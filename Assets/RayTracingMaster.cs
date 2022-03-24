using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour {
    public ComputeShader RayTracingShader;
    public ComputeShader Denoiser;
    public ComputeShader AtmosphereGeneratorShader;
    public Texture SkyboxTexture;
    
    private Camera _camera;
    private float _lastFieldOfView;
    private RenderTexture _target;
    private RenderTexture _converged;
    private RenderTexture _PosTex;
    private RenderTexture _Intermediate;
    private RenderTexture _Albedo;

    private AtmosphereGenerator Atmo;

    private AssetManager Assets;

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
    private ComputeBuffer _ShadowBuffer;
    private ComputeBuffer _LightTriangles;
    private ComputeBuffer _LightTriangleIndices;
    [HideInInspector] public BufferSizeData[] BufferSizes;

    private int uFirstFrame = 1;
    [HideInInspector] public int bouncecount = 24;
    [HideInInspector] public float c_phiGlob;
    [HideInInspector] public float n_phiGlob;
    [HideInInspector] public float p_phiGlob;
    [HideInInspector] public bool UseAtrous = false;
    [HideInInspector] public bool UseRussianRoulette = true;
    [HideInInspector] public bool UseNEE = false;
    [HideInInspector] public bool DoTLASUpdates = true;
    [HideInInspector] public bool AllowConverge = true;
    [HideInInspector] public int AtrousKernelSizes = 1;
    [HideInInspector] public int LightTrianglesCount;
    [HideInInspector] public float SunDirFloat = 0.0f;
    private int threadGroupsX;
    private int threadGroupsY;
    private int GenKernel;
    private int TraceKernel;
    private int ShadeKernel;
    private int ShadowKernel;


    [System.Serializable]
    public struct BufferSizeData {
        public int tracerays;
        public int rays_retired;
        public int shade_rays;
        public int shadow_rays;
        public int retired_shadow_rays;
    }
    void Start() {
        Assets = Camera.main.GetComponent<AssetManager>();
        LightTrianglesCount = Assets.AggLightTriangles.Count;
        uFirstFrame = 1;
        threadGroupsX = Mathf.CeilToInt(Screen.width / 256.0f);
        threadGroupsY = Mathf.CeilToInt(Screen.height / 1.0f);
        GenKernel = RayTracingShader.FindKernel("Generate");
        TraceKernel = RayTracingShader.FindKernel("kernel_trace");
        ShadowKernel = RayTracingShader.FindKernel("kernel_shadow");
        ShadeKernel = RayTracingShader.FindKernel("kernel_shade");

        Atmo = new AtmosphereGenerator(AtmosphereGeneratorShader, 6371.0f, 6403.0f);
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
        _ShadowBuffer?.Release();
        _LightTriangles?.Release();
        _LightTriangleIndices?.Release();
    }

    private void Update() {
        RayTracingShader.SetVector("SunDir", new Vector3(Mathf.Cos(SunDirFloat), Mathf.Sin(SunDirFloat), 0.0001f));
        if(!AllowConverge) {
            _currentSample = 0;
        }

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
        if(uFirstFrame != 1) {
            if(DoTLASUpdates) {
                Assets.UpdateTLAS();
                _CompactedMeshData.SetData(Assets.MyMeshesCompacted);
                _AggCWBVHBuffer.SetData(Assets.BVH8Aggregated, 0, 0, Assets.TLASSpace);
                _LightTriangleIndices.SetData(Assets.LightTriangleIndices);
            }
            Assets.UpdateMaterials();
            _MaterialDataBuffer.SetData(Assets._Materials);
        }

        if (!_meshObjectsNeedRebuilding) return;

        _meshObjectsNeedRebuilding = false;
        _currentSample = 0;

        CreateComputeBuffer(ref _MaterialDataBuffer, Assets._Materials, 56);
        CreateComputeBuffer(ref _AggTriBuffer, Assets.aggregated_triangles, 100);
        CreateComputeBuffer(ref _AggCWBVHBuffer, Assets.BVH8Aggregated, 80);
        CreateComputeBuffer(ref _CompactedMeshData, Assets.MyMeshesCompacted, 144);
        CreateComputeBuffer(ref _LightTriangles, Assets.AggLightTriangles, 76);
        CreateComputeBuffer(ref _LightTriangleIndices, Assets.LightTriangleIndices, 4);

        CreateDynamicBuffer(ref _RayBuffer1, 48);
        CreateDynamicBuffer(ref _ShadowBuffer, 44);
        CreateDynamicBuffer(ref _RayBuffer2, 48);
        CreateDynamicBuffer(ref _ColorBuffer, 32);
        CreateDynamicBuffer(ref _BufferSizes, 20);
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
        Denoiser.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetMatrix("ViewMatrix", _camera.worldToCameraMatrix);
        RayTracingShader.SetMatrix("InverseViewMatrix", _camera.worldToCameraMatrix.inverse);
        RayTracingShader.SetInt("MaxBounce", bouncecount - 1);
        RayTracingShader.SetInt("frames_accumulated", _currentSample);
        SetComputeBuffer(GenKernel, "BufferSizes", _BufferSizes);
        SetComputeBuffer(TraceKernel, "BufferSizes", _BufferSizes);
        SetComputeBuffer(ShadowKernel, "BufferSizes", _BufferSizes); 
        SetComputeBuffer(ShadeKernel, "BufferSizes", _BufferSizes);  
        RayTracingShader.SetBool("UseRussianRoulette", UseRussianRoulette);  
        RayTracingShader.SetBool("UseNEE", UseNEE);  
        if(uFirstFrame == 1) {
            if(SkyboxTexture != null) {
                RayTracingShader.SetTexture(ShadeKernel, "_SkyboxTexture", SkyboxTexture);
            }
            RayTracingShader.SetTexture(ShadeKernel, "_TextureAtlas", Assets.atlas);
            RayTracingShader.SetInt("lighttricount", LightTrianglesCount);
            RayTracingShader.SetInt("screen_width", Screen.width);
            RayTracingShader.SetInt("screen_height", Screen.height);
            Denoiser.SetInt("screen_width", Screen.width);
            Denoiser.SetInt("screen_height", Screen.height);
            SetComputeBuffer(ShadeKernel, "_Materials", _MaterialDataBuffer); 
            SetComputeBuffer(GenKernel, "GlobalRays1", _RayBuffer1);
            SetComputeBuffer(TraceKernel, "GlobalRays1", _RayBuffer1);
            SetComputeBuffer(ShadeKernel, "GlobalRays1", _RayBuffer1);

            SetComputeBuffer(ShadeKernel, "LightTriangles", _LightTriangles); 
            SetComputeBuffer(ShadeKernel, "LightTriangleIndices", _LightTriangleIndices); 

            SetComputeBuffer(ShadowKernel, "ShadowRaysBuffer", _ShadowBuffer);
            SetComputeBuffer(ShadeKernel, "ShadowRaysBuffer", _ShadowBuffer);

            SetComputeBuffer(GenKernel, "GlobalRays2", _RayBuffer2);
            SetComputeBuffer(TraceKernel, "GlobalRays2", _RayBuffer2);
            SetComputeBuffer(ShadeKernel, "GlobalRays2", _RayBuffer2);
            
            SetComputeBuffer(GenKernel, "GlobalColors", _ColorBuffer);
            SetComputeBuffer(ShadowKernel, "GlobalColors", _ColorBuffer);
            SetComputeBuffer(ShadeKernel, "GlobalColors", _ColorBuffer);

            SetComputeBuffer(TraceKernel, "AggTris", _AggTriBuffer);
            SetComputeBuffer(ShadowKernel, "AggTris", _AggTriBuffer);
            SetComputeBuffer(ShadeKernel, "AggTris", _AggTriBuffer);

            SetComputeBuffer(TraceKernel, "cwbvh_nodes", _AggCWBVHBuffer);
            SetComputeBuffer(ShadowKernel, "cwbvh_nodes", _AggCWBVHBuffer);
            
            SetComputeBuffer(ShadowKernel, "_MeshData", _CompactedMeshData);
            SetComputeBuffer(TraceKernel, "_MeshData", _CompactedMeshData);
            SetComputeBuffer(ShadeKernel, "_MeshData", _CompactedMeshData);

            RayTracingShader.SetFloat("sun_angular_radius", 0.05f);
            RayTracingShader.SetTexture(ShadeKernel, "ScatterTex", Atmo._RayleighTex);
            RayTracingShader.SetTexture(ShadeKernel, "MieTex", Atmo._MieTex);
        }

    }

    private void CreateRenderTexture(ref RenderTexture ThisTex, bool SRGB) {
        if(SRGB) {
        ThisTex = new RenderTexture(Screen.width, Screen.height, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        } else {
        ThisTex = new RenderTexture(Screen.width, Screen.height, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        }
        ThisTex.enableRandomWrite = true;
        ThisTex.Create();
    }

    private void InitRenderTexture() {
        if (_target == null || _target.width != Screen.width || _target.height != Screen.height) {
            // Release render texture if we already have one
            if (_target != null) {
                _target.Release();
                _converged.Release();
                _PosTex.Release();
                _Intermediate.Release();
                _Albedo.Release();
            }

         CreateRenderTexture(ref _target, true);
         CreateRenderTexture(ref _converged, true);
         CreateRenderTexture(ref _PosTex, false);
         CreateRenderTexture(ref _Intermediate, true);
         CreateRenderTexture(ref _Albedo, true);
            // Reset sampling
            _currentSample = 0;
        }
    }

    private void Render(RenderTexture destination) {
         _camera.depthTextureMode |= DepthTextureMode.DepthNormals | DepthTextureMode.MotionVectors | DepthTextureMode.Depth;
        
        InitRenderTexture();
        _target.Release();
        RayTracingShader.SetTexture(ShadeKernel, "Result", _target);        
        RayTracingShader.SetTexture(ShadeKernel, "TempPosTex", _PosTex);
        RayTracingShader.SetTexture(ShadeKernel, "TempAlbedoTex", _Albedo);
        RayTracingShader.Dispatch(GenKernel, threadGroupsX, threadGroupsY, 1);
        for(int i = 0; i < bouncecount; i++) {
            RayTracingShader.SetInt("CurBounce", i);
            RayTracingShader.Dispatch(TraceKernel, 16, 16, 1);
            RayTracingShader.Dispatch(ShadeKernel, threadGroupsX, threadGroupsY, 1);
            if(UseNEE) RayTracingShader.Dispatch(ShadowKernel, 16, 16, 1);
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
            Denoiser.SetTextureFromGlobal(AtrousKernel, "NormTex", "_CameraDepthNormalsTexture");
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