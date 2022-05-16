using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour {
    private ComputeShader RayTracingShader;
    private ComputeShader AtmosphereGeneratorShader;
    public Texture SkyboxTexture;
    
    private Camera _camera;
    private float _lastFieldOfView;
    private RenderTexture _target;
    private RenderTexture _converged;
    private RenderTexture _PosTex;
    private RenderTexture _Albedo;
    private RenderTexture _NormTex;

    private Denoiser Denoisers;
    private AtmosphereGenerator Atmo;

    private AssetManager Assets;

    private Material _addMaterial;
    private int _currentSample = 0;
    private List<Transform> _transformsToWatch = new List<Transform>();
    private static bool _meshObjectsNeedRebuilding = false;
    public static List<RayTracingLights> _rayTracingLights = new List<RayTracingLights>();
    private ComputeBuffer _meshObjectBuffer;
    private ComputeBuffer _MaterialDataBuffer;
    private ComputeBuffer _CompactedMeshData;
    private ComputeBuffer _RayBuffer1;
    private ComputeBuffer _RayBuffer2;
    private ComputeBuffer _ColorBuffer;
    private ComputeBuffer _BufferSizes;
    private ComputeBuffer _ShadowBuffer;
    private ComputeBuffer _LightTriangles;
    private ComputeBuffer _UnityLights;
    private ComputeBuffer _LightMeshes;
    [HideInInspector] public BufferSizeData[] BufferSizes;

    public float SunZOff;

    private int uFirstFrame = 1;
    [HideInInspector] public int bouncecount = 24;
    [HideInInspector] public float c_phiGlob = 0.1f;
    [HideInInspector] public float n_phiGlob = 0.1f;
    [HideInInspector] public float p_phiGlob = 0.1f;
    [HideInInspector] public bool UseSVGF = false;
    [HideInInspector] public bool UseAtrous = false;
    [HideInInspector] public bool UseRussianRoulette = true;
    [HideInInspector] public bool UseNEE = true;
    [HideInInspector] public bool DoTLASUpdates = true;
    [HideInInspector] public bool AllowConverge = true;
    [HideInInspector] public int SVGFAtrousKernelSizes = 6;
    [HideInInspector] public int AtrousKernelSizes = 6;
    [HideInInspector] public int LightTrianglesCount;
    [HideInInspector] public float SunDirFloat = 0.0f;
    private int threadGroupsX;
    private int threadGroupsY;
    private int threadGroupsX2;
    private int threadGroupsY2;
    private int GenKernel;
    private int TraceKernel;
    private int ShadeKernel;
    private int ShadowKernel;
    private int FinalizeKernel;
    private int FramesSinceStart;
    private Matrix4x4 PrevViewProjection;


    [System.Serializable]
    public struct BufferSizeData {
        public int tracerays;
        public int rays_retired;
        public int shade_rays;
        public int shadow_rays;
        public int retired_shadow_rays;
    }
    public bool HasStarted = false;
    void Start() {
        if(RayTracingShader == null) {RayTracingShader = Resources.Load<ComputeShader>("RayTracingShader");}
        if(AtmosphereGeneratorShader == null) {AtmosphereGeneratorShader = Resources.Load<ComputeShader>("Utility/AtmosphereLUTGenerator");}
        
        _meshObjectsNeedRebuilding = true;
        Assets = GameObject.Find("Scene").GetComponent<AssetManager>();
        Assets.BuildCombined();
        LightTrianglesCount = Assets.AggLightTriangles.Count;
        uFirstFrame = 1;
        FramesSinceStart = 0;
        threadGroupsX = Mathf.CeilToInt(Screen.width / 256.0f);
        threadGroupsY = Mathf.CeilToInt(Screen.height / 1.0f);
        threadGroupsX2 = Mathf.CeilToInt(Screen.width / 8.0f);
        threadGroupsY2 = Mathf.CeilToInt(Screen.height / 8.0f);
        GenKernel = RayTracingShader.FindKernel("Generate");
        TraceKernel = RayTracingShader.FindKernel("kernel_trace");
        ShadowKernel = RayTracingShader.FindKernel("kernel_shadow");
        ShadeKernel = RayTracingShader.FindKernel("kernel_shade");
        FinalizeKernel = RayTracingShader.FindKernel("kernel_finalize");

        Atmo = new AtmosphereGenerator(AtmosphereGeneratorShader, 6371.0f, 6403.0f);

        Denoisers = new Denoiser(_camera);
        HasStarted = true;
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
        _CompactedMeshData?.Release();
        _RayBuffer1?.Release();
        _RayBuffer2?.Release();
        _ColorBuffer?.Release();
        _BufferSizes?.Release();
        _ShadowBuffer?.Release();
        _LightTriangles?.Release();
        _UnityLights?.Release();
        _LightMeshes?.Release();
    }
    public static Vector3 SunDirection;
    private void Update() {
        SunDirection = new Vector3(Mathf.Cos(SunDirFloat), Mathf.Sin(SunDirFloat), SunZOff);

        RayTracingShader.SetVector("SunDir", SunDirection);
        if(!AllowConverge) {
            FramesSinceStart = 0;
        }

        if (_camera.fieldOfView != _lastFieldOfView) {
            FramesSinceStart = 0;
            _lastFieldOfView = _camera.fieldOfView;
        }

        foreach (Transform t in _transformsToWatch) {
            if (t.hasChanged) {
                FramesSinceStart = 0;
                t.hasChanged = false;
            }
        }
    }

    public static void RegisterObject(RayTracingLights obj) {//Adds meshes to list
        _rayTracingLights.Add(obj);
        _meshObjectsNeedRebuilding = true;
    }
    public static void UnregisterObject(RayTracingLights obj) {//Removes meshes from list
        _rayTracingLights.Remove(obj);
        _meshObjectsNeedRebuilding = true;
    }   

    private void CreateDynamicBuffer(ref ComputeBuffer TargetBuffer, int Stride) {
        if(TargetBuffer == null) TargetBuffer = new ComputeBuffer(Screen.width * Screen.height, Stride);
    }
    public void RebuildMeshObjectBuffers() {
        if(uFirstFrame != 1) {
            if(DoTLASUpdates) {
                if(Assets.UpdateTLAS()) {
                    CreateComputeBuffer(ref _CompactedMeshData, Assets.MyMeshesCompacted, 156);
                    CreateComputeBuffer(ref _LightTriangles, Assets.AggLightTriangles, 68);
                    CreateComputeBuffer(ref _MaterialDataBuffer, Assets._Materials, 96);
                    CreateComputeBuffer(ref _UnityLights, Assets.UnityLights, 56);
                    CreateComputeBuffer(ref _LightMeshes, Assets.LightMeshes, 92);
                    uFirstFrame = 1;
                } else {
                    _CompactedMeshData.SetData(Assets.MyMeshesCompacted);
                    if(Assets.LightMeshCount != 0) _LightMeshes.SetData(Assets.LightMeshes);
                    if(Assets.UnityLightCount != 0) _UnityLights.SetData(Assets.UnityLights);
                    Assets.UpdateMaterials();
                    _MaterialDataBuffer.SetData(Assets._Materials);
                }
            }
        }

        if (!_meshObjectsNeedRebuilding) return;

        _meshObjectsNeedRebuilding = false;
        FramesSinceStart = 0;

        CreateComputeBuffer(ref _UnityLights, Assets.UnityLights, 56);

        CreateComputeBuffer(ref _LightMeshes, Assets.LightMeshes, 92);
        
        CreateComputeBuffer(ref _MaterialDataBuffer, Assets._Materials, 96);
        CreateComputeBuffer(ref _CompactedMeshData, Assets.MyMeshesCompacted, 156);
        CreateComputeBuffer(ref _LightTriangles, Assets.AggLightTriangles, 68);

        CreateDynamicBuffer(ref _RayBuffer1, 48);
        if(_ShadowBuffer == null) _ShadowBuffer = new ComputeBuffer(Screen.width * Screen.height * 2, 52);
        CreateDynamicBuffer(ref _RayBuffer2, 48);
        CreateDynamicBuffer(ref _ColorBuffer, 36);
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
        _BufferSizes.SetData(BufferSizes);
        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
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
            RayTracingShader.SetInt("LightMeshCount", Assets.LightMeshCount);
            RayTracingShader.SetInt("unitylightcount", Assets.UnityLightCount);
            RayTracingShader.SetTexture(ShadeKernel, "_TextureAtlas", Assets.AlbedoAtlas);
            RayTracingShader.SetTexture(ShadeKernel, "_EmissiveAtlas", Assets.EmissiveAtlas);
            RayTracingShader.SetTexture(ShadeKernel, "_NormalAtlas", Assets.NormalAtlas);
            RayTracingShader.SetInt("lighttricount", Assets.LightTriCount);
            RayTracingShader.SetInt("screen_width", Screen.width);
            RayTracingShader.SetInt("screen_height", Screen.height);
            SetComputeBuffer(ShadeKernel, "_LightMeshes", _LightMeshes);
            SetComputeBuffer(ShadeKernel, "_Materials", _MaterialDataBuffer); 
            SetComputeBuffer(GenKernel, "GlobalRays1", _RayBuffer1);
            SetComputeBuffer(TraceKernel, "GlobalRays1", _RayBuffer1);
            SetComputeBuffer(ShadeKernel, "GlobalRays1", _RayBuffer1);

            SetComputeBuffer(ShadeKernel, "LightTriangles", _LightTriangles);  

            SetComputeBuffer(ShadowKernel, "ShadowRaysBuffer", _ShadowBuffer);
            SetComputeBuffer(ShadeKernel, "ShadowRaysBuffer", _ShadowBuffer);

            SetComputeBuffer(GenKernel, "GlobalRays2", _RayBuffer2);
            SetComputeBuffer(TraceKernel, "GlobalRays2", _RayBuffer2);
            SetComputeBuffer(ShadeKernel, "GlobalRays2", _RayBuffer2);
            
            SetComputeBuffer(GenKernel, "GlobalColors", _ColorBuffer);
            SetComputeBuffer(ShadowKernel, "GlobalColors", _ColorBuffer);
            SetComputeBuffer(ShadeKernel, "GlobalColors", _ColorBuffer);

            SetComputeBuffer(TraceKernel, "AggTris", Assets.AggTriBuffer);
            SetComputeBuffer(ShadowKernel, "AggTris", Assets.AggTriBuffer);
            SetComputeBuffer(ShadeKernel, "AggTris", Assets.AggTriBuffer);

            SetComputeBuffer(TraceKernel, "cwbvh_nodes", Assets.BVH8AggregatedBuffer);
            SetComputeBuffer(ShadowKernel, "cwbvh_nodes", Assets.BVH8AggregatedBuffer);
            
            SetComputeBuffer(ShadowKernel, "_MeshData", _CompactedMeshData);
            SetComputeBuffer(TraceKernel, "_MeshData", _CompactedMeshData);
            SetComputeBuffer(ShadeKernel, "_MeshData", _CompactedMeshData);

            SetComputeBuffer(ShadeKernel, "_UnityLights", _UnityLights);

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
                _Albedo.Release();
                _NormTex.Release();
            }

         CreateRenderTexture(ref _target, true);
         CreateRenderTexture(ref _NormTex, false);
         CreateRenderTexture(ref _converged, true);
         CreateRenderTexture(ref _PosTex, false);
         CreateRenderTexture(ref _Albedo, true);
            // Reset sampling
            _currentSample = 0;
        }
    }

    private void Render(RenderTexture destination) {
        float CurrentSample;
        InitRenderTexture();       
        RayTracingShader.SetTexture(ShadeKernel, "TempPosTex", _PosTex);
        RayTracingShader.SetTexture(ShadeKernel, "TempNormTex", _NormTex);
        RayTracingShader.SetTexture(ShadeKernel, "TempAlbedoTex", _Albedo);
        RayTracingShader.Dispatch(GenKernel, threadGroupsX, threadGroupsY, 1);
        for(int i = 0; i < bouncecount; i++) {
            var bouncebounce = i;
            RayTracingShader.SetInt("CurBounce", bouncebounce);
            RayTracingShader.Dispatch(TraceKernel, 768, 1, 1);
            RayTracingShader.Dispatch(ShadeKernel, threadGroupsX2, threadGroupsY2, 1);
            if(UseNEE) RayTracingShader.Dispatch(ShadowKernel, 768, 1, 1);
        }   
        //I could try to optimize it by using a structured buffer that contains normals, colors, and positions, making it float3, float3, float2(compressed normal) to conserve a single float
        if(!UseSVGF) {
            RayTracingShader.SetTexture(FinalizeKernel, "Result", _target);
            RayTracingShader.SetTexture(FinalizeKernel, "TempAlbedoTex", _Albedo);
            RayTracingShader.SetBuffer(FinalizeKernel, "GlobalColors", _ColorBuffer);
            RayTracingShader.Dispatch(FinalizeKernel, Mathf.CeilToInt(Screen.width / 16.0f), Mathf.CeilToInt(Screen.height / 16.0f), 1);
            CurrentSample = 1.0f / (FramesSinceStart + 1.0f);
        } else {
            Denoisers.ExecuteSVGF(_currentSample, SVGFAtrousKernelSizes, ref _ColorBuffer, ref _PosTex, ref _target, ref _Albedo, ref _NormTex);
            CurrentSample = 1;
        }

        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/Accumulate"));
        _addMaterial.SetFloat("_Sample", CurrentSample);
        Graphics.Blit(_target, _converged, _addMaterial);

        if(UseAtrous) {
            Denoisers.ExecuteAtrous(AtrousKernelSizes, n_phiGlob, p_phiGlob, c_phiGlob, ref _PosTex, ref _target, ref _Albedo, ref _converged, ref _NormTex);
        }

        Graphics.Blit((UseAtrous) ? _target : _converged, destination);
        _currentSample++; 
        FramesSinceStart++;  
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if(Assets.RenderQue.Count > 0) {
            RebuildMeshObjectBuffers();
            SetShaderParameters();
            Render(destination);
            uFirstFrame = 0;
        } else {
            bool throwawayBool = Assets.UpdateTLAS();
        }
    }

}