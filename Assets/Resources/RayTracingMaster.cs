using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour {
    public ComputeShader RayTracingShader;
    public ComputeShader Denoiser;
    public Texture SkyboxTexture;
    
    private Camera _camera;
    private float _lastFieldOfView;
    private RenderTexture _target;
    private RenderTexture _converged;
    private RenderTexture _NormTex;
    private RenderTexture _PosTex;
    private RenderTexture _Intermediate;
    private RenderTexture _Albedo;

    private Material _addMaterial;
    private int _currentSample = 0;
    private List<Transform> _transformsToWatch = new List<Transform>();
    private static bool _meshObjectsNeedRebuilding = false;
    public static List<RayTracingObject> _rayTracingObjects = new List<RayTracingObject>();
    private ComputeBuffer _meshObjectBuffer;
    private ComputeBuffer _MaterialDataBuffer;
    private ComputeBuffer _AggTriBuffer;
    private ComputeBuffer _AggBVHBuffer;
    private ComputeBuffer _AggCWBVHBuffer;
    private ComputeBuffer _CompactedMeshData;
    private ComputeBuffer _RayBuffer1;
    private ComputeBuffer _RayBuffer2;
    private ComputeBuffer _ColorBuffer;
    private ComputeBuffer _BufferSizes;
    public BufferSizeData[] BufferSizes;

    private int uFirstFrame = 1;
    public int bouncecount = 24;
    public float c_phiGlob;
    public float n_phiGlob;
    public float p_phiGlob;
    public bool UseAtrous = false;
    public bool UseRussianRoulette = true;
    public int AtrousKernelSizes = 1;
    private int threadGroupsX;
    private int threadGroupsY;

    [System.Serializable]
    public struct BufferSizeData {
        public int tracerays;
        public int rays_retired;
        public int shade_rays;
    }
    void Start() {
        uFirstFrame = 1;
        threadGroupsX = Mathf.CeilToInt(Screen.width / 256.0f);
        threadGroupsY = Mathf.CeilToInt(Screen.height / 1.0f);
        //AssetManager Assets = Camera.main.GetComponent<AssetManager>();//Enable these two to rebuild the BVH on start
        //Assets.Begin();
    }

    private void Awake() {
        _camera = GetComponent<Camera>();
        _transformsToWatch.Add(transform);
    }

    private void OnEnable() {
        _currentSample = 0;
    }

    private void OnDisable() {
        _meshObjectBuffer?.Release();
        _MaterialDataBuffer?.Release();
        _AggTriBuffer?.Release();
        _AggCWBVHBuffer?.Release();
        _CompactedMeshData?.Release();
        _RayBuffer1?.Release();
        _RayBuffer2?.Release();
        _ColorBuffer?.Release();
        _BufferSizes?.Release();
    }

    private void Update() {

        if (_camera.fieldOfView != _lastFieldOfView) {
            _currentSample = 0;
            _lastFieldOfView = _camera.fieldOfView;
        }

        foreach (Transform t in _transformsToWatch) {
            if (t.hasChanged) {
                _currentSample = 0;
                t.hasChanged = false;
            }
        }
    }

    public static void RegisterObject(RayTracingObject obj) {//Adds meshes to list
        obj.matfill();//Ensure that the new object has proper initial data
        _rayTracingObjects.Add(obj);
        _meshObjectsNeedRebuilding = true;
    }
    public static void UnregisterObject(RayTracingObject obj) {//Removes meshes from list
        _rayTracingObjects.Remove(obj);
        _meshObjectsNeedRebuilding = true;
    }   

    private void CreateDynamicBuffer(ref ComputeBuffer TargetBuffer, int Stride) {
        if(TargetBuffer == null) TargetBuffer = new ComputeBuffer(Screen.width * Screen.height, Stride);
    }

    public void RebuildMeshObjectBuffers() {
        AssetManager Assets = Camera.main.GetComponent<AssetManager>();
        if(uFirstFrame != 1) {
            Assets.UpdateTLAS();
            _CompactedMeshData.SetData(Assets.MyMeshesCompacted);
            _AggCWBVHBuffer.SetData(Assets.BVH8Aggregated);
            Assets.UpdateMaterials();
            _MaterialDataBuffer.SetData(Assets._Materials);
        }

        if (!_meshObjectsNeedRebuilding) return;

        _meshObjectsNeedRebuilding = false;
        _currentSample = 0;

        CreateComputeBuffer(ref _MaterialDataBuffer, Assets._Materials, 56);
        CreateComputeBuffer(ref _AggTriBuffer, Assets.aggregated_triangles, 100);
        CreateComputeBuffer(ref _AggCWBVHBuffer, Assets.BVH8Aggregated, 80);
        CreateComputeBuffer(ref _CompactedMeshData, Assets.MyMeshesCompacted, 68);

        CreateDynamicBuffer(ref _RayBuffer1, 48);
        CreateDynamicBuffer(ref _RayBuffer2, 48);
        CreateDynamicBuffer(ref _ColorBuffer, 12);
        CreateDynamicBuffer(ref _BufferSizes, 12);
    }


    private void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
        where T : struct
    {
        // Do we already have a compute buffer?
        if (buffer != null) {
            // If no data or buffer doesn't match the given criteria, release it
            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride) {
                buffer.Release();
                buffer = null;
            }
        }

        if (data.Count != 0) {
            // If the buffer has been released or wasn't there to
            // begin with, create it
            if (buffer == null) {
                buffer = new ComputeBuffer(data.Count, stride);
            }
            // Set data on the buffer
            buffer.SetData(data);
        }
    }

    private void SetComputeBuffer(int kernel, string name, ComputeBuffer buffer) {
        if (buffer != null) RayTracingShader.SetBuffer(kernel, name, buffer);
    }

    private void SetShaderParameters() {
        BufferSizes = new BufferSizeData[bouncecount];
        BufferSizes[0].tracerays = Screen.width * Screen.height;
        _BufferSizes.SetData(BufferSizes, 0, 0, bouncecount);
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetInt("MaxBounce", bouncecount - 1);
        RayTracingShader.SetInt("frames_accumulated", _currentSample);
        SetComputeBuffer(0, "BufferSizes", _BufferSizes);
        SetComputeBuffer(1, "BufferSizes", _BufferSizes);
        SetComputeBuffer(2, "BufferSizes", _BufferSizes);  
        RayTracingShader.SetBool("UseRussianRoulette", UseRussianRoulette);  
        if(uFirstFrame == 1) {
            AssetManager Assets = Camera.main.GetComponent<AssetManager>();
            if(SkyboxTexture != null) {
                RayTracingShader.SetTexture(2, "_SkyboxTexture", SkyboxTexture);
            }
            RayTracingShader.SetTexture(2, "_TextureAtlas", Assets.atlas);
            RayTracingShader.SetInt("screen_width", Screen.width);
            RayTracingShader.SetInt("screen_height", Screen.height);
            Denoiser.SetInt("screen_width", Screen.width);
            Denoiser.SetInt("screen_height", Screen.height);
            SetComputeBuffer(2, "_Materials", _MaterialDataBuffer);
            
            SetComputeBuffer(0, "GlobalRays1", _RayBuffer1);
            SetComputeBuffer(1  , "GlobalRays1", _RayBuffer1);
            SetComputeBuffer(2  , "GlobalRays1", _RayBuffer1);

            SetComputeBuffer(0, "GlobalRays2", _RayBuffer2);
            SetComputeBuffer(1  , "GlobalRays2", _RayBuffer2);
            SetComputeBuffer(2  , "GlobalRays2", _RayBuffer2);
            
            SetComputeBuffer(0  , "GlobalColors", _ColorBuffer);
            SetComputeBuffer(2  , "GlobalColors", _ColorBuffer);

            SetComputeBuffer(1, "AggTris", _AggTriBuffer);
            SetComputeBuffer(2, "AggTris", _AggTriBuffer);

            SetComputeBuffer(1, "cwbvh_nodes", _AggCWBVHBuffer);
            
            SetComputeBuffer(1, "_MeshData", _CompactedMeshData);
            SetComputeBuffer(2, "_MeshData", _CompactedMeshData);
        }

    }

    private void CreateRenderTexture(ref RenderTexture ThisTex) {
        ThisTex = new RenderTexture(Screen.width, Screen.height, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        ThisTex.enableRandomWrite = true;
        ThisTex.Create();
    }

    private void InitRenderTexture() {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height) {
            // Release render texture if we already have one
            if (_target != null) {
                _target.Release();
                _converged.Release();
                _NormTex.Release();
                _PosTex.Release();
                _Intermediate.Release();
                _Albedo.Release();
            }

         CreateRenderTexture(ref _target);
         CreateRenderTexture(ref _converged);
         CreateRenderTexture(ref _NormTex);
         CreateRenderTexture(ref _PosTex);
         CreateRenderTexture(ref _Intermediate);
         CreateRenderTexture(ref _Albedo);
            // Reset sampling
            _currentSample = 0;
        }
    }


    private void Render(RenderTexture destination) {

        InitRenderTexture();
        int GenKernel = RayTracingShader.FindKernel("Generate");
        int TraceKernel = RayTracingShader.FindKernel("kernel_trace");
        int ShadeKernel = RayTracingShader.FindKernel("kernel_shade");
        RayTracingShader.SetTexture(ShadeKernel, "Result", _target);
        RayTracingShader.SetTexture(ShadeKernel, "TempNormTex", _NormTex);
        RayTracingShader.SetTexture(ShadeKernel, "TempPosTex", _PosTex);
        RayTracingShader.SetTexture(ShadeKernel, "TempAlbedoTex", _Albedo);

        RayTracingShader.Dispatch(GenKernel, threadGroupsX, threadGroupsY, 1);
        for(int i = 0; i < bouncecount; i++) {
            RayTracingShader.SetInt("CurBounce", i);
            RayTracingShader.Dispatch(TraceKernel, 16, 16, 1);
            RayTracingShader.Dispatch(ShadeKernel, threadGroupsX, threadGroupsY, 1);
        }   

        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/Accumulate"));
        _addMaterial.SetFloat("_Sample", _currentSample);
        Graphics.Blit(_target, _converged, _addMaterial);
        
        Graphics.CopyTexture(_converged, _target);
        
        if(UseAtrous) {
            int AtrousKernel = Denoiser.FindKernel("Atrous");
            Graphics.CopyTexture(_target, _Intermediate);
            Denoiser.SetFloat("n_phi", n_phiGlob);
            Denoiser.SetFloat("p_phi", p_phiGlob);
            Denoiser.SetInt("KernelSize", AtrousKernelSizes);
            Denoiser.SetTexture(AtrousKernel, "NormTex", _NormTex);
            Denoiser.SetTexture(AtrousKernel, "PosTex", _PosTex);
            Denoiser.SetTexture(AtrousKernel, "AlbedoTex", _Albedo);
            float c_phi = c_phiGlob;
            for(int i = 1; i <= AtrousKernelSizes; i *= 2) {
                Denoiser.SetTexture(AtrousKernel, "ResultIn", _Intermediate);
                Denoiser.SetTexture(AtrousKernel, "Result", _target);
                Denoiser.SetFloat("c_phi", c_phi);
                Denoiser.SetInt("step_width", i);
                Denoiser.Dispatch(AtrousKernel, threadGroupsX, threadGroupsY, 1);
                Graphics.CopyTexture(_target, _Intermediate);
                c_phi /= 2.0f;
            }
        }
        
        Graphics.Blit(_target, destination);
        _currentSample++;   
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        RebuildMeshObjectBuffers();
        SetShaderParameters();
        Render(destination);
        uFirstFrame = 0;
    }

}