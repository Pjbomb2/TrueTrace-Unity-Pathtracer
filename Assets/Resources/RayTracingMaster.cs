using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour {
    private ComputeShader RayTracingShader;
    private ComputeShader AtmosphereGeneratorShader;
    public Texture SkyboxTexture;
    
    private Camera _camera;
    private float _lastFieldOfView;
    public RenderTexture _target;
    private RenderTexture _converged;
    private RenderTexture _PosTex;
    private RenderTexture _Albedo;
    private RenderTexture _NormTex;
    private RenderTexture _IntermediateTex;
    private RenderTexture _DebugTex;
    private RenderTexture _FinalTex;
    private RenderTexture PrevDistanceTex;
    private RenderTexture MaskTex;
    private RenderTexture PrevNormalTex;
    private RenderTexture PrevPosTex;

    public RenderTexture _RandomNums;

    public Denoiser Denoisers;
    public AtmosphereGenerator Atmo;

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
    private ComputeBuffer _BrickmapBuffer;
    private ComputeBuffer _VoxelTLAS;
    private ComputeBuffer _CurrentReservoir;
    private ComputeBuffer _PreviousReservoir;
    private ComputeBuffer _PrecomputedBlocks;
    private ComputeBuffer _IllumTriBuffer;
    private ComputeBuffer _VoxelPositionBuffer;
    private ComputeBuffer _SHBuffer;
    private ComputeBuffer RaysBuffer;
    private ComputeBuffer GIReservoirCurrent;
    private ComputeBuffer GIReservoirPrevious;
    private ComputeBuffer MatModifierBuffer;
    private ComputeBuffer MatModifierBufferPrev;
    private bool PreviousAtrous;


    public ASVGF ASVGFCode;

    private int FramesSinceStart2;
    [HideInInspector] public BufferSizeData[] BufferSizes;
    [SerializeField]
    public int SampleCount;

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
    [HideInInspector] public bool AllowVolumetrics = false;
    [HideInInspector] public bool DoTLASUpdates = true;
    [HideInInspector] public bool AllowConverge = true;
    [HideInInspector] public bool AllowBloom = false;
    [HideInInspector] public bool AllowDoF = false;
    [HideInInspector] public bool AllowAutoExpose = false;
    [HideInInspector] public bool AllowReSTIR = false;
    [HideInInspector] public bool AllowReSTIRTemporal = false;
    [HideInInspector] public bool AllowReSTIRSpatial = false;
    [HideInInspector] public bool AllowReSTIRRegeneration = false;
    [HideInInspector] public bool AllowReSTIRPrecomputedSamples = false;
    [HideInInspector] public bool AllowToneMap = true;
    [HideInInspector] public bool AllowTAA = false;
    [HideInInspector] public float DoFAperature = 0.2f;
    [HideInInspector] public float DoFFocal = 0.2f;
    [HideInInspector] public float RenderScale = 1.0f;
    [HideInInspector] public float BloomStrength = 32.0f;
    [HideInInspector] public int RISSampleCount = 32;
    [HideInInspector] public int SpatialSamples = 5;
    [HideInInspector] public bool UseASVGF = false;
    [HideInInspector] public bool UseTAAU = true;
    [HideInInspector] public int SpatialMCap = 32;
    [HideInInspector] public int MaxIterations = 4;
    [HideInInspector] public bool UseReSTIRGITemporal = true;
    [HideInInspector] public bool UseReSTIRGISpatial = true;
    [HideInInspector] public bool UseReSTIRGI = false;
    [HideInInspector] public int ReSTIRGISpatialCount = 5;
    [HideInInspector] public int ReSTIRGIUpdateRate = 0;
    [HideInInspector] public int ReSTIRGITemporalMCap = 0;
    [HideInInspector] public bool DoReSTIRGIConnectionValidation = true;
    [HideInInspector] public bool ReSTIRGISpatialStabalizer = false;

    public bool DoVoxels = false;

    [HideInInspector] public int SVGFAtrousKernelSizes = 6;
    [HideInInspector] public int AtrousKernelSizes = 6;
    [HideInInspector] public int LightTrianglesCount;
    [HideInInspector] public float SunDirFloat = 0.0f;
    [HideInInspector] public float VolumeDensity = 0.0001f;
    [HideInInspector] public int AtmoNumLayers = 4;
    [HideInInspector] public bool AlternateSVGF = true;
    private int threadGroupsX;
    private int threadGroupsY;
    private int threadGroupsX2;
    private int threadGroupsY2;
    private int threadGroupsX3;
    private int threadGroupsY3;
    private int GenKernel;
    private int GenASVGFKernel;
    private int TraceKernel;
    private int ShadeKernel;
    private int ShadowKernel;
    private int FinalizeKernel;
    private int BrickmapKernel;
    private int BrickmapShadowKernel;
    private int ReservoirKernel;
    private int ReservoirPrecomputeKernel;
    private int FramesSinceStart;
    private int GIReSTIRKernel;
    private int GIForwardProjectKernel;
    private int ValidateReSTIRGIKernel;
    private Matrix4x4 PrevViewProjection;
    private int TargetWidth;
    private int TargetHeight;
    private int SourceWidth;
    private int SourceHeight;
    private Vector3 PrevCamPosition;
    private bool PrevASVGF;


    [System.Serializable]
    public struct BufferSizeData {
        public int tracerays;
        public int rays_retired;
        public int brickmap_rays_retired;
        public int shade_rays;
        public int shadow_rays;
        public int retired_shadow_rays;
        public int retired_brickmap_shadow_rays;
    }
    public bool HasStarted = false;
        public SVGF2 SecondSVGF;
    // Denoiser2 OIDN = new Denoiser2();
    unsafe void Start() {
        // OIDN.Start();
        SecondSVGF = new SVGF2();
        Application.targetFrameRate = 165;
        ASVGFCode = new ASVGF();
        if(RayTracingShader == null) {RayTracingShader = Resources.Load<ComputeShader>("RayTracingShader");}
        if(AtmosphereGeneratorShader == null) {AtmosphereGeneratorShader = Resources.Load<ComputeShader>("Utility/AtmosphereLUTGenerator");}
        SourceWidth = (int)Mathf.Ceil((float)Screen.width * RenderScale);
        SourceHeight = (int)Mathf.Ceil((float)Screen.height * RenderScale);
        TargetWidth = Screen.width;
        TargetHeight = Screen.height;
        if(Mathf.Abs(SourceWidth - TargetWidth) < 2) {
            SourceWidth = TargetWidth;
            SourceHeight = TargetHeight;
        }
        ASVGFCode.init(SourceWidth, SourceHeight, _camera);   
        SecondSVGF.Start(TargetWidth, TargetHeight);
        
        _meshObjectsNeedRebuilding = true;
        Assets = GameObject.Find("Scene").GetComponent<AssetManager>();
        Assets.BuildCombined();
        LightTrianglesCount = Assets.AggLightTriangles.Count;
        uFirstFrame = 1;
        FramesSinceStart = 0;
        threadGroupsX = Mathf.CeilToInt(SourceWidth / 256.0f);
        threadGroupsY = Mathf.CeilToInt(SourceHeight / 1.0f);
        threadGroupsX2 = Mathf.CeilToInt(SourceWidth / 8.0f);
        threadGroupsY2 = Mathf.CeilToInt(SourceHeight / 8.0f);
        threadGroupsX3 = Mathf.CeilToInt(SourceWidth / 16.0f);
        threadGroupsY3 = Mathf.CeilToInt(SourceHeight / 16.0f);
        GenKernel = RayTracingShader.FindKernel("Generate");
        GenASVGFKernel = RayTracingShader.FindKernel("GenerateASVGF");
        TraceKernel = RayTracingShader.FindKernel("kernel_trace");
        ShadowKernel = RayTracingShader.FindKernel("kernel_shadow");
        ShadeKernel = RayTracingShader.FindKernel("kernel_shade");
        FinalizeKernel = RayTracingShader.FindKernel("kernel_finalize");
        BrickmapKernel = RayTracingShader.FindKernel("kernel_brickmap_trace");
        BrickmapShadowKernel = RayTracingShader.FindKernel("kernel_shadow_brickmap");
        ReservoirKernel = RayTracingShader.FindKernel("kernel_reservoir");
        ReservoirPrecomputeKernel = RayTracingShader.FindKernel("kernel_reservoir_precompute");
        GIReSTIRKernel = RayTracingShader.FindKernel("kernel_GI_Reserviour");    
        GIForwardProjectKernel = RayTracingShader.FindKernel("forwardprojectionkernel");    
        ValidateReSTIRGIKernel = RayTracingShader.FindKernel("ValidateGI");    

        Atmo = new AtmosphereGenerator(AtmosphereGeneratorShader, 6360000.0f / 1000.0f, 6420000.0f / 1000.0f, AtmoNumLayers);
        FramesSinceStart2 = 0;

        Denoisers = new Denoiser(_camera, SourceWidth, SourceHeight);
        HasStarted = true;
        _camera.renderingPath = RenderingPath.DeferredShading;
        _camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

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
        _BrickmapBuffer?.Release();
        _VoxelTLAS?.Release();
        _CurrentReservoir?.Release();
        _PreviousReservoir?.Release();
        _PrecomputedBlocks?.Release();
        _IllumTriBuffer?.Release();
        _BrickmapBuffer?.Release();
        _VoxelPositionBuffer?.Release();
        _SHBuffer?.Release();
        RaysBuffer?.Release();
        ASVGFCode.ClearAll();
        GIReservoirCurrent.Release();
        GIReservoirPrevious.Release();
        MatModifierBuffer.Release();
        MatModifierBufferPrev.Release();
    }
    public static Vector3 SunDirection;
    private void Update() {
        SunDirection = Assets.SunDirection;

        RayTracingShader.SetVector("SunDir", SunDirection);
        if(!AllowConverge || (PreviousAtrous != UseAtrous)) {
            SampleCount = 0;
            FramesSinceStart = 0;
        }

        if (_camera.fieldOfView != _lastFieldOfView) {
            FramesSinceStart = 0;
            _lastFieldOfView = _camera.fieldOfView;
        }

        foreach (Transform t in _transformsToWatch) {
            if (t.hasChanged) {
                SampleCount = 0;
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
        if(TargetBuffer == null) TargetBuffer = new ComputeBuffer(SourceWidth * SourceHeight, Stride);
    }
    public void RebuildMeshObjectBuffers() {
        if(uFirstFrame != 1) {
            if(DoTLASUpdates) {
                if(Assets.UpdateTLAS()) {
                    CreateComputeBuffer(ref _CompactedMeshData, Assets.MyMeshesCompacted, 180);
                    CreateComputeBuffer(ref _LightTriangles, Assets.AggLightTriangles, 72);
                    CreateComputeBuffer(ref _MaterialDataBuffer, Assets._Materials, 200);
                    CreateComputeBuffer(ref _UnityLights, Assets.UnityLights, 56);
                    CreateComputeBuffer(ref _LightMeshes, Assets.LightMeshes, 92);
                    CreateComputeBuffer(ref _VoxelTLAS, Assets.VoxelTLAS, 80);
                    CreateComputeBuffer(ref _BrickmapBuffer, Assets.GPUBrickmap, 4);
                    uFirstFrame = 1;
                } else {
                    _CompactedMeshData.SetData(Assets.MyMeshesCompacted);
                    if(Assets.LightMeshCount != 0) _LightMeshes.SetData(Assets.LightMeshes);
                    if(Assets.UnityLightCount != 0) _UnityLights.SetData(Assets.UnityLights);
                    Assets.UpdateMaterials();
                    _MaterialDataBuffer.SetData(Assets._Materials);
                    if(Assets.UseVoxels) _VoxelTLAS.SetData(Assets.VoxelTLAS);
                }
            }
        }

        if (!_meshObjectsNeedRebuilding) return;

        _meshObjectsNeedRebuilding = false;
        FramesSinceStart = 0;
        CreateComputeBuffer(ref _BrickmapBuffer, Assets.GPUBrickmap, 4);
        CreateComputeBuffer(ref _UnityLights, Assets.UnityLights, 56);
        CreateComputeBuffer(ref _VoxelTLAS, Assets.VoxelTLAS, 80);
        CreateComputeBuffer(ref _LightMeshes, Assets.LightMeshes, 92);
        CreateComputeBuffer(ref _VoxelPositionBuffer, Assets.VoxelPositions, 16);
        
        CreateComputeBuffer(ref _MaterialDataBuffer, Assets._Materials, 200);
        CreateComputeBuffer(ref _CompactedMeshData, Assets.MyMeshesCompacted, 180);
        CreateComputeBuffer(ref _LightTriangles, Assets.AggLightTriangles, 72);
        CreateComputeBuffer(ref _IllumTriBuffer, Assets.ToIllumTriBuffer, 4);

        CreateDynamicBuffer(ref _RayBuffer1, 56);
        if(_ShadowBuffer == null) _ShadowBuffer = new ComputeBuffer(SourceWidth * SourceHeight * 2, 52);
        CreateDynamicBuffer(ref _RayBuffer2, 56);
        CreateDynamicBuffer(ref _ColorBuffer, 52);
        CreateDynamicBuffer(ref MatModifierBuffer, 32);
        CreateDynamicBuffer(ref MatModifierBufferPrev, 32);
        CreateDynamicBuffer(ref _BufferSizes, 28);
        CreateDynamicBuffer(ref _CurrentReservoir, 92);
        CreateDynamicBuffer(ref _PreviousReservoir, 92);
        CreateDynamicBuffer(ref GIReservoirCurrent, 92);
        CreateDynamicBuffer(ref GIReservoirPrevious, 92);
        CreateDynamicBuffer(ref _SHBuffer, 24);
        RaysBuffer = new ComputeBuffer(SourceWidth * SourceHeight, 36);
        RayTracingShader.SetBuffer(GenASVGFKernel, "Rays", RaysBuffer);
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
    private void CreateComputeBuffer<T>(ref ComputeBuffer buffer, T[] data, int stride)
        where T : struct
    {
        // Do we already have a compute buffer?
        if (buffer != null) {
            // If no data or buffer doesn't match the given criteria, release it
            if (data.Length == 0 || buffer.count != data.Length || buffer.stride != stride) {
                buffer.Release();
                buffer = null;
            }
        }

        if (data.Length != 0) {
            // If the buffer has been released or wasn't there to
            // begin with, create it
            if (buffer == null) {
                buffer = new ComputeBuffer(data.Length, stride);
            }
            // Set data on the buffer
            buffer.SetData(data);
        }
    }

    private void SetComputeBuffer(int kernel, string name, ComputeBuffer buffer) {
        if (buffer != null) RayTracingShader.SetBuffer(kernel, name, buffer);
    }

    private void SetShaderParameters() {
        Matrix4x4 viewprojmatrix = _camera.projectionMatrix * _camera.worldToCameraMatrix;
        var PrevMatrix = PrevViewProjection;
        BufferSizes = new BufferSizeData[bouncecount];
        BufferSizes[0].tracerays = SourceWidth * SourceHeight;
        _BufferSizes.SetData(BufferSizes);
        SetComputeBuffer(GenKernel, "BufferSizes", _BufferSizes);
        SetComputeBuffer(GenASVGFKernel, "BufferSizes", _BufferSizes);
        SetComputeBuffer(TraceKernel, "BufferSizes", _BufferSizes);
        SetComputeBuffer(ShadowKernel, "BufferSizes", _BufferSizes); 
        SetComputeBuffer(ShadeKernel, "BufferSizes", _BufferSizes);  
        SetComputeBuffer(BrickmapKernel, "BufferSizes", _BufferSizes);
        SetComputeBuffer(BrickmapShadowKernel, "BufferSizes", _BufferSizes);

        RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        RayTracingShader.SetMatrix("viewprojection", viewprojmatrix);
        RayTracingShader.SetMatrix("prevviewprojection", PrevMatrix);
        RayTracingShader.SetMatrix("ViewMatrix", _camera.worldToCameraMatrix);
        RayTracingShader.SetMatrix("InverseViewMatrix", _camera.worldToCameraMatrix.inverse);

        RayTracingShader.SetVector("CamDir", _camera.transform.forward);
        RayTracingShader.SetVector("Up", _camera.transform.up);
        RayTracingShader.SetVector("Right", _camera.transform.right);
        RayTracingShader.SetVector("Forward", _camera.transform.forward);
        RayTracingShader.SetVector("camPos", _camera.transform.position);
        RayTracingShader.SetVector("CamPos", _camera.transform.position);
        RayTracingShader.SetVector("PrevCamPos", PrevCamPosition);

        RayTracingShader.SetFloat("VolumeDensity", VolumeDensity * VolumeDensity);
        RayTracingShader.SetFloat("FarPlane", _camera.farClipPlane);
        RayTracingShader.SetFloat("VolumeDensity", VolumeDensity * VolumeDensity);
        RayTracingShader.SetFloat("focal_distance", DoFFocal);
        RayTracingShader.SetFloat("AperatureRadius", DoFAperature);
        RayTracingShader.SetFloat("sun_angular_radius", 0.1f);

        RayTracingShader.SetInt("MaxBounce", bouncecount - 1);
        RayTracingShader.SetInt("frames_accumulated", _currentSample);
        RayTracingShader.SetInt("VoxelOffset", Assets.VoxOffset);
        RayTracingShader.SetInt("lightsamplecount", RISSampleCount);
        RayTracingShader.SetInt("spatialsamplecount", SpatialSamples);
        RayTracingShader.SetInt("ReSTIRGISpatialCount", ReSTIRGISpatialCount);
        RayTracingShader.SetInt("SpatialMCap", SpatialMCap);
        RayTracingShader.SetInt("ReSTIRGIUpdateRate", ReSTIRGIUpdateRate);
        RayTracingShader.SetInt("ReSTIRGITemporalMCap", ReSTIRGITemporalMCap);
        RayTracingShader.SetInt("curframe", FramesSinceStart2); 


        RayTracingShader.SetBool("UseRussianRoulette", UseRussianRoulette);  
        RayTracingShader.SetBool("UseSpatial", AllowReSTIRSpatial);  
        RayTracingShader.SetBool("UseNEE", UseNEE);  
        RayTracingShader.SetBool("DoVoxels", Assets.UseVoxels);  
        RayTracingShader.SetBool("AllowVolumetrics", AllowVolumetrics); 
        RayTracingShader.SetBool("UseDoF", AllowDoF);
        RayTracingShader.SetBool("UseReSTIRGI", UseReSTIRGI);
        RayTracingShader.SetBool("UseReSTIRGITemporal", UseReSTIRGITemporal);
        RayTracingShader.SetBool("UseReSTIRGISpatial", UseReSTIRGISpatial);
        RayTracingShader.SetBool("DoReSTIRGIConnectionValidation", DoReSTIRGIConnectionValidation);
        RayTracingShader.SetBool("UseRestir", AllowReSTIR); 
        RayTracingShader.SetBool("UseRestirTemporal", AllowReSTIRTemporal); 
        RayTracingShader.SetBool("UseRestirPrecomputedSamples", AllowReSTIRPrecomputedSamples); 
        RayTracingShader.SetBool("UseASVGF", UseASVGF); 
        RayTracingShader.SetBool("UseAtrous", UseAtrous);
        RayTracingShader.SetBool("AbandonSamples", UseASVGF != PrevASVGF);
        RayTracingShader.SetBool("SpatialStabalizer", ReSTIRGISpatialStabalizer);

        SetComputeBuffer(BrickmapShadowKernel, "CurrentReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirCurrent : GIReservoirPrevious));
        SetComputeBuffer(GIReSTIRKernel, "CurrentReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirCurrent : GIReservoirPrevious));
        SetComputeBuffer(GIReSTIRKernel, "PreviousReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirPrevious : GIReservoirCurrent));
        SetComputeBuffer(ShadeKernel, "CurrentReservoirGI",((FramesSinceStart2 % 2 == 0) ? GIReservoirCurrent : GIReservoirPrevious));
        SetComputeBuffer(ShadeKernel, "PreviousReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirPrevious : GIReservoirCurrent));   
        SetComputeBuffer(FinalizeKernel, "CurrentReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirCurrent : GIReservoirPrevious));
        SetComputeBuffer(FinalizeKernel, "PreviousReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirPrevious : GIReservoirCurrent)); 
        SetComputeBuffer(GenKernel, "CurrentReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirCurrent : GIReservoirPrevious));
        SetComputeBuffer(GenKernel, "PreviousReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirPrevious : GIReservoirCurrent));  
        SetComputeBuffer(ValidateReSTIRGIKernel, "CurrentReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirCurrent : GIReservoirPrevious));
        SetComputeBuffer(ValidateReSTIRGIKernel, "PreviousReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirPrevious : GIReservoirCurrent));       
        SetComputeBuffer(GIForwardProjectKernel, "CurrentReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirPrevious : GIReservoirCurrent));  

        SetComputeBuffer(BrickmapShadowKernel, "CurrentReservoir", ((FramesSinceStart2 % 2 == 0) ? _CurrentReservoir : _PreviousReservoir));
        SetComputeBuffer(ReservoirKernel, "CurrentReservoir", (FramesSinceStart2 % 2 == 0) ? _CurrentReservoir : _PreviousReservoir);
        SetComputeBuffer(ReservoirKernel, "PreviousReservoir", (FramesSinceStart2 % 2 == 0) ? _PreviousReservoir : _CurrentReservoir);
        SetComputeBuffer(ShadeKernel, "CurrentReservoir", ((FramesSinceStart2 % 2 == 0) ? _CurrentReservoir : _PreviousReservoir));
        SetComputeBuffer(ShadeKernel, "PreviousReservoir", (FramesSinceStart2 % 2 == 0) ? _PreviousReservoir : _CurrentReservoir);     

        RayTracingShader.SetTextureFromGlobal(FinalizeKernel, "Depth", "_CameraDepthTexture");              
        RayTracingShader.SetTextureFromGlobal(FinalizeKernel, "NormalTex", "_CameraGBufferTexture2");
        RayTracingShader.SetTextureFromGlobal(ReservoirKernel, "MotionVectors", "_CameraMotionVectorsTexture");
        RayTracingShader.SetTextureFromGlobal(GIReSTIRKernel, "Depth", "_CameraDepthTexture");              
        RayTracingShader.SetTextureFromGlobal(GIReSTIRKernel, "MotionVectors", "_CameraMotionVectorsTexture");
        RayTracingShader.SetTextureFromGlobal(GIReSTIRKernel, "NormalTex", "_CameraGBufferTexture2");
        RayTracingShader.SetTextureFromGlobal(GIForwardProjectKernel, "MotionVectors", "_CameraMotionVectorsTexture");
        RayTracingShader.SetTextureFromGlobal(ValidateReSTIRGIKernel, "MotionVectors", "_CameraMotionVectorsTexture");
        RayTracingShader.SetTextureFromGlobal(ShadeKernel, "MotionVectors", "_CameraMotionVectorsTexture");
        RayTracingShader.SetTextureFromGlobal(GenKernel, "MotionVectors", "_CameraMotionVectorsTexture");
        RayTracingShader.SetTextureFromGlobal(GenASVGFKernel, "MotionVectors", "_CameraMotionVectorsTexture");

        SetComputeBuffer(ShadeKernel, "MatModifiers", ((FramesSinceStart2 % 2 == 0) ? MatModifierBuffer : MatModifierBufferPrev));
        SetComputeBuffer(FinalizeKernel, "MatModifiers", ((FramesSinceStart2 % 2 == 0) ? MatModifierBuffer : MatModifierBufferPrev));
        SetComputeBuffer(FinalizeKernel, "MatModifiersPrev", ((FramesSinceStart2 % 2 == 0) ? MatModifierBufferPrev : MatModifierBuffer));
        SetComputeBuffer(GIReSTIRKernel, "MatModifiersPrev", ((FramesSinceStart2 % 2 == 0) ? MatModifierBufferPrev : MatModifierBuffer));

        PrevViewProjection = viewprojmatrix;
        if(uFirstFrame == 1) {
            if(SkyboxTexture != null) {
                RayTracingShader.SetTexture(ShadeKernel, "_SkyboxTexture", SkyboxTexture);
            }
            
            RayTracingShader.SetBool("DiffRes", RenderScale != 1.0f);
            
            RayTracingShader.SetInt("LightMeshCount", Assets.LightMeshCount);
            RayTracingShader.SetInt("unitylightcount", Assets.UnityLightCount);
            RayTracingShader.SetInt("lighttricount", Assets.LightTriCount); 
            RayTracingShader.SetInt("screen_width", SourceWidth);
            RayTracingShader.SetInt("screen_height", SourceHeight);

            CreateRenderTexture(ref _RandomNums, false, false);

            RayTracingShader.SetTexture(GenASVGFKernel, "RandomNums", _RandomNums);
            SetComputeBuffer(GenASVGFKernel, "SH", _SHBuffer);
            SetComputeBuffer(GenASVGFKernel, "GlobalRays1", _RayBuffer1);
            SetComputeBuffer(GenASVGFKernel, "GlobalRays2", _RayBuffer2);
            SetComputeBuffer(GenASVGFKernel, "GlobalColors", _ColorBuffer);

            RayTracingShader.SetTexture(GenKernel, "RandomNums", _RandomNums);
            SetComputeBuffer(GenKernel, "GlobalRays1", _RayBuffer1);
            SetComputeBuffer(GenKernel, "GlobalRays2", _RayBuffer2);
            SetComputeBuffer(GenKernel, "GlobalColors", _ColorBuffer);            

            SetComputeBuffer(TraceKernel, "GlobalRays1", _RayBuffer1);
            SetComputeBuffer(TraceKernel, "GlobalRays2", _RayBuffer2);
            SetComputeBuffer(TraceKernel, "AggTris", Assets.AggTriBuffer);
            SetComputeBuffer(TraceKernel, "cwbvh_nodes", Assets.BVH8AggregatedBuffer);
            SetComputeBuffer(TraceKernel, "_MeshData", _CompactedMeshData);

            SetComputeBuffer(ShadowKernel, "_MeshData", _CompactedMeshData);
            SetComputeBuffer(ShadowKernel, "cwbvh_nodes", Assets.BVH8AggregatedBuffer);
            SetComputeBuffer(ShadowKernel, "AggTris", Assets.AggTriBuffer);
            SetComputeBuffer(ShadowKernel, "ShadowRaysBuffer", _ShadowBuffer);

            SetComputeBuffer(BrickmapKernel, "BrickMap", _BrickmapBuffer);
            SetComputeBuffer(BrickmapKernel, "VoxelTLAS", _VoxelTLAS); 
            SetComputeBuffer(BrickmapKernel, "GlobalRays2", _RayBuffer2);
            SetComputeBuffer(BrickmapKernel, "_MeshData", _CompactedMeshData);

            SetComputeBuffer(BrickmapShadowKernel, "SH", _SHBuffer);
            SetComputeBuffer(BrickmapShadowKernel, "BrickMap", _BrickmapBuffer);
            SetComputeBuffer(BrickmapShadowKernel, "VoxelTLAS", _VoxelTLAS); 
            SetComputeBuffer(BrickmapShadowKernel, "ShadowRaysBuffer", _ShadowBuffer);
            SetComputeBuffer(BrickmapShadowKernel, "GlobalColors", _ColorBuffer);
            SetComputeBuffer(BrickmapShadowKernel, "_MeshData", _CompactedMeshData);
            RayTracingShader.SetBuffer(BrickmapShadowKernel, "Rays", RaysBuffer);

            RayTracingShader.SetTexture(ShadeKernel, "RandomNums", _RandomNums);
            RayTracingShader.SetTexture(ShadeKernel, "_TextureAtlas", Assets.AlbedoAtlas);
            RayTracingShader.SetTexture(ShadeKernel, "_EmissiveAtlas", Assets.EmissiveAtlas);
            RayTracingShader.SetTexture(ShadeKernel, "_NormalAtlas", Assets.NormalAtlas);
            RayTracingShader.SetTexture(ShadeKernel, "_MetallicAtlas", Assets.MetallicAtlas);
            RayTracingShader.SetTexture(ShadeKernel, "_RoughnessAtlas", Assets.RoughnessAtlas);
            RayTracingShader.SetTexture(ShadeKernel, "RenderMaskTex", MaskTex);
            RayTracingShader.SetTexture(ShadeKernel, "scattering_texture", Atmo.MultiScatterTex);
            RayTracingShader.SetTexture(ShadeKernel, "TransmittanceTex", Atmo._TransmittanceLUT);
            RayTracingShader.SetTexture(ShadeKernel, "ScatterTex", Atmo._RayleighTex);
            RayTracingShader.SetTexture(ShadeKernel, "MieTex", Atmo._MieTex);
            RayTracingShader.SetTexture(ShadeKernel, "TempAlbedoTex", _Albedo);
            RayTracingShader.SetTexture(ShadeKernel, "TempNormTex", _NormTex);
            RayTracingShader.SetTexture(ShadeKernel, "TempPosTex", _PosTex);
            SetComputeBuffer(ShadeKernel, "SH", _SHBuffer);
            SetComputeBuffer(ShadeKernel, "_LightMeshes", _LightMeshes);
            SetComputeBuffer(ShadeKernel, "_Materials", _MaterialDataBuffer);             
            SetComputeBuffer(ShadeKernel, "GlobalRays1", _RayBuffer1);
            SetComputeBuffer(ShadeKernel, "GlobalRays2", _RayBuffer2);
            SetComputeBuffer(ShadeKernel, "LightTriangles", _LightTriangles);  
            SetComputeBuffer(ShadeKernel, "ShadowRaysBuffer", _ShadowBuffer);
            SetComputeBuffer(ShadeKernel, "cwbvh_nodes", Assets.BVH8AggregatedBuffer);
            SetComputeBuffer(ShadeKernel, "AggTris", Assets.AggTriBuffer);
            SetComputeBuffer(ShadeKernel, "GlobalColors", _ColorBuffer);
            SetComputeBuffer(ShadeKernel, "_MeshData", _CompactedMeshData);
            SetComputeBuffer(ShadeKernel, "_UnityLights", _UnityLights);
            RayTracingShader.SetBuffer(ShadeKernel, "Rays", RaysBuffer);

            RayTracingShader.SetTexture(FinalizeKernel, "RandomNums", _RandomNums);
            RayTracingShader.SetTexture(FinalizeKernel, "TempPosTex", _PosTex);
            RayTracingShader.SetTexture(FinalizeKernel, "PrevDepthTex", PrevDistanceTex);
            RayTracingShader.SetTexture(FinalizeKernel, "Result", _target);
            RayTracingShader.SetTexture(FinalizeKernel, "AlbedoTex", _Albedo);
            RayTracingShader.SetTexture(GIReSTIRKernel, "TempAlbedoTex", _Albedo);
            RayTracingShader.SetTexture(FinalizeKernel, "PrevNormalTex", PrevNormalTex);
            RayTracingShader.SetBuffer(FinalizeKernel, "GlobalColors", _ColorBuffer);

            RayTracingShader.SetTexture(ReservoirKernel, "RandomNums", _RandomNums);
            RayTracingShader.SetTexture(ReservoirKernel, "TempPosTex", _PosTex);
            RayTracingShader.SetTexture(ReservoirKernel, "AlbedoTex", _Albedo);
            RayTracingShader.SetTexture(ReservoirKernel, "TempNormTex", _NormTex);
            SetComputeBuffer(ReservoirKernel, "CurrentReservoir", _CurrentReservoir);
            SetComputeBuffer(ReservoirKernel, "PreviousReservoir", _PreviousReservoir);
            SetComputeBuffer(ReservoirKernel, "_LightMeshes", _LightMeshes);
            SetComputeBuffer(ReservoirKernel, "LightTriangles", _LightTriangles);
            SetComputeBuffer(ReservoirKernel, "AggTris", Assets.AggTriBuffer);
            SetComputeBuffer(ReservoirKernel, "cwbvh_nodes", Assets.BVH8AggregatedBuffer);
            SetComputeBuffer(ReservoirKernel, "_MeshData", _CompactedMeshData);
            SetComputeBuffer(ReservoirKernel, "_UnityLights", _UnityLights);

            RayTracingShader.SetTexture(ReservoirPrecomputeKernel, "RandomNums", _RandomNums);


            RayTracingShader.SetTexture(GIReSTIRKernel, "RandomNums", _RandomNums);
            RayTracingShader.SetTexture(GIReSTIRKernel, "RenderMaskTex", MaskTex);
            RayTracingShader.SetTexture(GIReSTIRKernel, "PrevPosTex", PrevPosTex);
            RayTracingShader.SetTexture(GIReSTIRKernel, "TempPosTex", _PosTex);
            RayTracingShader.SetTexture(GIReSTIRKernel, "TempNormTex", _NormTex);
            RayTracingShader.SetTexture(GIReSTIRKernel, "PrevDepthTex", PrevDistanceTex);
            RayTracingShader.SetTexture(GIReSTIRKernel, "PrevNormalTex", PrevNormalTex);
            SetComputeBuffer(GIReSTIRKernel, "_Materials", _MaterialDataBuffer);   
            SetComputeBuffer(GIReSTIRKernel, "GlobalColors", _ColorBuffer);
            SetComputeBuffer(GIReSTIRKernel, "AggTris", Assets.AggTriBuffer);
            SetComputeBuffer(GIReSTIRKernel, "cwbvh_nodes", Assets.BVH8AggregatedBuffer);
            SetComputeBuffer(GIReSTIRKernel, "_MeshData", _CompactedMeshData);
            SetComputeBuffer(GIReSTIRKernel, "SH", _SHBuffer);
            SetComputeBuffer(GIReSTIRKernel, "MatModifiers", MatModifierBuffer);

            RayTracingShader.SetTexture(GIForwardProjectKernel, "RenderMaskTex", MaskTex);
            RayTracingShader.SetTexture(GIForwardProjectKernel, "RandomNums", _RandomNums);
            SetComputeBuffer(GIForwardProjectKernel, "Rays", RaysBuffer);  

            RayTracingShader.SetTexture(ValidateReSTIRGIKernel, "RandomNums", _RandomNums);
            SetComputeBuffer(ValidateReSTIRGIKernel, "GlobalColors", _ColorBuffer);

            RayTracingShader.SetTexture(GenKernel, "_DebugTex", _DebugTex);
            RayTracingShader.SetTexture(ShadeKernel, "_DebugTex", _DebugTex);
            RayTracingShader.SetTexture(ReservoirKernel, "_DebugTex", _DebugTex);
            RayTracingShader.SetTexture(BrickmapKernel, "_DebugTex", _DebugTex);
            RayTracingShader.SetTexture(TraceKernel, "_DebugTex", _DebugTex);
            RayTracingShader.SetTexture(FinalizeKernel, "_DebugTex", _DebugTex);
            RayTracingShader.SetTexture(GIReSTIRKernel, "_DebugTex", _DebugTex);
            RayTracingShader.SetTexture(ValidateReSTIRGIKernel, "_DebugTex", _DebugTex);

        if(_PrecomputedBlocks == null) _PrecomputedBlocks = new ComputeBuffer(128 * 512, 56);
            SetComputeBuffer(ReservoirPrecomputeKernel, "WriteBlocks", _PrecomputedBlocks);
            SetComputeBuffer(ReservoirPrecomputeKernel, "_LightMeshes", _LightMeshes);
            SetComputeBuffer(ReservoirPrecomputeKernel, "LightTriangles", _LightTriangles); 
            SetComputeBuffer(ReservoirPrecomputeKernel, "_UnityLights", _UnityLights);
            RayTracingShader.Dispatch(ReservoirPrecomputeKernel, 128, 1, 1);
            SetComputeBuffer(ReservoirKernel, "Blocks", _PrecomputedBlocks);
        } else if(AllowReSTIRRegeneration) {
            SetComputeBuffer(ReservoirPrecomputeKernel, "WriteBlocks", _PrecomputedBlocks);
            SetComputeBuffer(ReservoirPrecomputeKernel, "_LightMeshes", _LightMeshes);
            SetComputeBuffer(ReservoirPrecomputeKernel, "LightTriangles", _LightTriangles); 
            SetComputeBuffer(ReservoirPrecomputeKernel, "_UnityLights", _UnityLights);
            RayTracingShader.Dispatch(ReservoirPrecomputeKernel, 128, 1, 1);
            SetComputeBuffer(ReservoirKernel, "Blocks", _PrecomputedBlocks);
        }

    }

    private void CreateRenderTexture(ref RenderTexture ThisTex, bool SRGB, bool Res) {
        if(SRGB) {
        ThisTex = new RenderTexture((Res) ? TargetWidth : SourceWidth, (Res) ? TargetHeight : SourceHeight, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        } else {
        ThisTex = new RenderTexture((Res) ? TargetWidth : SourceWidth, (Res) ? TargetHeight : SourceHeight, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        }
        ThisTex.enableRandomWrite = true;
        ThisTex.Create();
    }

    private void CreateRenderTextureMask(ref RenderTexture ThisTex, bool Res) {
        ThisTex = new RenderTexture((Res) ? TargetWidth : SourceWidth, (Res) ? TargetHeight : SourceHeight, 0,
            RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
        ThisTex.enableRandomWrite = true;
        ThisTex.Create();
    }

    private void CreateRenderTexture(ref RenderTexture ThisTex, bool SRGB, bool istarget, bool Res) {
        if(SRGB) {
        ThisTex = new RenderTexture((Res) ? TargetWidth : SourceWidth, (Res) ? TargetHeight : SourceHeight, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        } else {
        ThisTex = new RenderTexture((Res) ? TargetWidth : SourceWidth, (Res) ? TargetHeight : SourceHeight, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        }
        if(istarget) {
            ThisTex.useMipMap = true;
            ThisTex.autoGenerateMips = false;
        }
        ThisTex.enableRandomWrite = true;
        ThisTex.Create();
    }
    private void CreateRenderTextureSingle(ref RenderTexture ThisTex, bool Res) {
        ThisTex = new RenderTexture((Res) ? TargetWidth : SourceWidth, (Res) ? TargetHeight : SourceHeight, 0,
            RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        ThisTex.enableRandomWrite = true;
        ThisTex.Create();
    }
    private void CreateRenderTextureDouble(ref RenderTexture ThisTex, bool Res) {
        ThisTex = new RenderTexture((Res) ? TargetWidth : SourceWidth, (Res) ? TargetHeight : SourceHeight, 0,
            RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);
        ThisTex.enableRandomWrite = true;
        ThisTex.Create();
    }

    private void InitRenderTexture() {
        if (_target == null || _target.width != SourceWidth || _target.height != SourceHeight) {
            // Release render texture if we already have one
            if (_target != null) {
                _target.Release();
                _converged.Release();
                _PosTex.Release();
                _Albedo.Release();
                _NormTex.Release();
                _IntermediateTex.Release();
                _DebugTex.Release();
                PrevDistanceTex.Release();
                MaskTex.Release();
                PrevNormalTex.Release();
                PrevPosTex.Release();
            }

         CreateRenderTexture(ref _DebugTex, true, false);
         CreateRenderTexture(ref _FinalTex, true, true);
         CreateRenderTexture(ref _IntermediateTex, true, true, true);
         CreateRenderTexture(ref _target, true, false);
         CreateRenderTextureMask(ref _NormTex, false);
         CreateRenderTexture(ref _converged, true, false);
         CreateRenderTexture(ref _PosTex, false, false);
         CreateRenderTexture(ref PrevPosTex, false, false);
         CreateRenderTexture(ref _Albedo, true, false);
         CreateRenderTextureSingle(ref PrevDistanceTex, false);
         CreateRenderTexture(ref PrevNormalTex, false, false);
         CreateRenderTextureMask(ref MaskTex, false);
            // Reset sampling
            _currentSample = 0;
        }
    }
    public void ClearOutRenderTexture(RenderTexture renderTexture) {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = rt;
    }

    private void Render(RenderTexture destination) {
        float CurrentSample;
        UnityEngine.Profiling.Profiler.BeginSample("Init Compute Shaders"); 

        UnityEngine.Profiling.Profiler.EndSample();
        
        if(UseASVGF) {
            ASVGFCode.DoRNG(ref _RandomNums, FramesSinceStart2, ref RaysBuffer, ref PrevDistanceTex, ref PrevNormalTex);
            RayTracingShader.SetBuffer(GenASVGFKernel, "Rays", RaysBuffer);
        }


        UnityEngine.Profiling.Profiler.BeginSample("ReSTIRGI Reproject Pass"); 
        if(ReSTIRGIUpdateRate != 0 && UseReSTIRGI) RayTracingShader.Dispatch(GIForwardProjectKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
        UnityEngine.Profiling.Profiler.EndSample();



        UnityEngine.Profiling.Profiler.BeginSample("First Bounce Gen");
        RayTracingShader.SetInt("CurBounce", 0);
        RayTracingShader.Dispatch((UseASVGF || (UseReSTIRGI && ReSTIRGIUpdateRate != 0)) ? GenASVGFKernel : GenKernel, threadGroupsX, threadGroupsY, 1);
        UnityEngine.Profiling.Profiler.EndSample();
        
        UnityEngine.Profiling.Profiler.BeginSample("First Bounce Trace");
        RayTracingShader.Dispatch(TraceKernel, 768, 1, 1);
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("First Trace For Brickmap");
        if(Assets.UseVoxels) RayTracingShader.Dispatch(BrickmapKernel, 768, 1, 1);
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("Rest Of Trace");
        if(AllowReSTIR) {//Required to be down here so that prevpostex is correct
            UnityEngine.Profiling.Profiler.BeginSample("ReSTIR Reproject");
            RayTracingShader.Dispatch(ReservoirKernel, Mathf.CeilToInt(SourceWidth / 8.0f), Mathf.CeilToInt(SourceHeight / 8.0f), 1);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        for(int i = 0; i < bouncecount; i++) {
            UnityEngine.Profiling.Profiler.BeginSample("TraceKernel For Bounce: " + i);
            var bouncebounce = i;
            RayTracingShader.SetInt("CurBounce", bouncebounce);
            if(i != 0) RayTracingShader.Dispatch(TraceKernel, 768, 1, 1);
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TraceKernel For Brickmap: " + i);
            if(Assets.UseVoxels && i != 0) RayTracingShader.Dispatch(BrickmapKernel, 768, 1, 1);
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("Shade Kernel: " + i);
            RayTracingShader.Dispatch(ShadeKernel, threadGroupsX2, threadGroupsY2, 1);
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("Shadow Kernel: " + i);
            if(UseNEE || AllowVolumetrics) RayTracingShader.Dispatch(ShadowKernel, 768, 1, 1);
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("Brickmap Shadow Kernel: " + i);
            if(UseNEE || AllowVolumetrics) RayTracingShader.Dispatch(BrickmapShadowKernel, 768, 1, 1);
            UnityEngine.Profiling.Profiler.EndSample();
        }
        UnityEngine.Profiling.Profiler.EndSample();

        if(ReSTIRGIUpdateRate != 0 && UseReSTIRGI) RayTracingShader.Dispatch(ValidateReSTIRGIKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);

        UnityEngine.Profiling.Profiler.BeginSample("ReSTIRGI Pass"); 
        if(UseReSTIRGI) RayTracingShader.Dispatch(GIReSTIRKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
        UnityEngine.Profiling.Profiler.EndSample();


        UnityEngine.Profiling.Profiler.BeginSample("Finalize");
        if(!UseSVGF && !UseASVGF) {
            RayTracingShader.Dispatch(FinalizeKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
            CurrentSample = 1.0f / (FramesSinceStart + 1.0f);
            SampleCount++;
        } else if(!UseASVGF) {
            SampleCount = 0;
            if(AlternateSVGF) {
                RayTracingShader.Dispatch(FinalizeKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                Graphics.CopyTexture(_target, 0, 0, _IntermediateTex, 0, 0);
                SecondSVGF.Denoise(ref _target, _IntermediateTex, _Albedo, SVGFAtrousKernelSizes);
            } else Denoisers.ExecuteSVGF(_currentSample, SVGFAtrousKernelSizes, ref _ColorBuffer, ref _target, ref _Albedo, ref _NormTex, RenderScale != 1.0f, ref PrevDistanceTex, ref PrevNormalTex);
            CurrentSample = 1;
        } else {
            SampleCount = 0;
            ASVGFCode.Do(ref _ColorBuffer, ref _NormTex, ref _Albedo, ref _target, ref _RandomNums, ref _SHBuffer, MaxIterations, RenderScale != 1.0f, ref PrevDistanceTex, ref PrevNormalTex);
            CurrentSample = 1;
        }

        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/Accumulate"));
        _addMaterial.SetFloat("_Sample", CurrentSample);
        Graphics.Blit(_target, _converged, _addMaterial);
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("Post Processing");
        if(SourceWidth != Screen.width) {
            if(UseTAAU) Denoisers.ExecuteTAAU(ref _FinalTex, ref _converged);
            else Denoisers.ExecuteUpsample(ref _converged, ref _FinalTex, ref _PosTex, FramesSinceStart2, _currentSample);//This is a postprocessing pass, but im treating it like its not one, need to move it to after the accumulation
            Graphics.CopyTexture(_FinalTex,0,0,_IntermediateTex,0,0);
        } else {
            Graphics.CopyTexture(_converged, _FinalTex);
            if(UseAtrous || AllowAutoExpose || AllowBloom || AllowTAA) {
                Graphics.CopyTexture(_FinalTex,0,0,_IntermediateTex,0,0);
            }
        }

        if(UseAtrous) {
            Denoisers.ExecuteAtrous(AtrousKernelSizes, n_phiGlob, p_phiGlob, c_phiGlob, ref _PosTex, ref _FinalTex, ref _IntermediateTex, ref _Albedo, ref _NormTex, FramesSinceStart2);
            Graphics.CopyTexture(_FinalTex, 0, 0, _IntermediateTex, 0, 0);
        }
        if(AllowAutoExpose) {
            _IntermediateTex.GenerateMips();
            Denoisers.ExecuteAutoExpose(ref _FinalTex, ref _IntermediateTex);
            Graphics.CopyTexture(_FinalTex, 0, 0, _IntermediateTex, 0, 0);
        }
        if(AllowBloom) {
            Denoisers.ExecuteBloom(ref _FinalTex, ref _IntermediateTex, BloomStrength);
            Graphics.CopyTexture(_FinalTex, 0, 0, _IntermediateTex, 0, 0);
        }
        if(AllowTAA) {
            Denoisers.ExecuteTAA(ref _FinalTex, ref _IntermediateTex, _currentSample);
        }
        
        if(UseAtrous || AllowAutoExpose || AllowBloom || AllowTAA) Graphics.CopyTexture(_IntermediateTex, 0, 0, _FinalTex, 0, 0);
        
        if(AllowToneMap) Denoisers.ExecuteToneMap(ref _FinalTex);
        // if(UseTAAU) {
        //     Graphics.CopyTexture(_FinalTex, 0, 0, _IntermediateTex, 0, 0);
        //     OIDN.OnRenderImage2(_IntermediateTex, ref _FinalTex);
        //     }
        // if(UseTAAU) SecondSVGF.Denoise(ref _FinalTex, _IntermediateTex, _Albedo);
        Graphics.Blit(_FinalTex, destination);
        Graphics.CopyTexture(_PosTex, PrevPosTex);
        ClearOutRenderTexture(_DebugTex);
        UnityEngine.Profiling.Profiler.EndSample();
         _currentSample++; 
        FramesSinceStart++;  
        FramesSinceStart2++;
        PreviousAtrous = UseAtrous;
        PrevCamPosition = _camera.transform.position;
        PrevASVGF = UseASVGF;
        // _BufferSizes.GetData(BufferSizes);
        // float average = 0;
        // int PixAcccounted = 0;
        // for(int i = bouncecount - 1; i >= 0; i--) {
        //     average += (BufferSizes[i].rays_retired - PixAcccounted) * (i + 1) / (float)(Screen.width * Screen.height);
        //     PixAcccounted += (BufferSizes[i].rays_retired - PixAcccounted);
        // }
        // Debug.Log((average));
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if(Assets.RenderQue.Count > 0) {
            RebuildMeshObjectBuffers();
            InitRenderTexture();     
            SetShaderParameters();
            Render(destination);
            uFirstFrame = 0;
        } else {
            try {bool throwawayBool = Assets.UpdateTLAS();} catch(System.IndexOutOfRangeException){}
        }
    }

}
