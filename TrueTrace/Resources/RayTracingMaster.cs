using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace TrueTrace {
    public class RayTracingMaster : MonoBehaviour
    {
        [HideInInspector] public ComputeShader ShadingShader;
        private ComputeShader IntersectionShader;
        private ComputeShader GenerateShader;
        private ComputeShader ReSTIRGI;

        [HideInInspector] public static Camera _camera;
        private float _lastFieldOfView;
        private RenderTexture _target;
        private RenderTexture _converged;
        private RenderTexture _Albedo;
        private RenderTexture _IntermediateTex;
        private RenderTexture _DebugTex;
        private RenderTexture _FinalTex;
        private RenderTexture CorrectedDistanceTex;
        private RenderTexture CorrectedDistanceTexB;
        private RenderTexture _RandomNums;
        private RenderTexture _RandomNumsB;
        private RenderTexture _PrimaryTriangleInfo;

        private RenderTexture GIReservoirA;
        private RenderTexture GIReservoirB;
        private RenderTexture GIReservoirC;

        private RenderTexture GIWorldPosA;
        private RenderTexture GIWorldPosB;
        private RenderTexture GIWorldPosC;

        private RenderTexture GINEEPosA;
        private RenderTexture GINEEPosB;
        private RenderTexture GINEEPosC;


        private RenderTexture Gradients;


        [HideInInspector] public RenderTexture ScreenSpaceInfo;
        private RenderTexture ScreenSpaceInfoPrev;



        private ReCurDenoiser ReCurDen;
        private Denoiser Denoisers;
        private ASVGF ASVGFCode;
        private ReSTIRASVGF ReSTIRASVGFCode;

        [HideInInspector] public AtmosphereGenerator Atmo;
        [HideInInspector] public AssetManager Assets;
        private Texture3D ToneMapTex;
        private Material _addMaterial;
        private Material _FireFlyMaterial;
        private int _currentSample = 0;
        [HideInInspector] public List<Transform> _transformsToWatch = new List<Transform>();
        private static bool _meshObjectsNeedRebuilding = false;
        public static List<RayTracingLights> _rayTracingLights = new List<RayTracingLights>();
        private ComputeBuffer _MaterialDataBuffer;
        private ComputeBuffer _CompactedMeshData;
        private ComputeBuffer _RayBuffer;
        private ComputeBuffer LightingBuffer;
        private ComputeBuffer PrevLightingBufferA;
        private ComputeBuffer PrevLightingBufferB;
        private ComputeBuffer _BufferSizes;
        private ComputeBuffer _ShadowBuffer;
        private ComputeBuffer _UnityLights;
        private ComputeBuffer _LightMeshes;
        private ComputeBuffer RaysBuffer;
        private ComputeBuffer RaysBufferB;
        private ComputeBuffer CurBounceInfoBuffer;
        #if HardwareRT
            private ComputeBuffer MeshIndexOffsets;
            private ComputeBuffer SubMeshOffsetsBuffer;
        #endif


        private int FramesSinceStart2;
        private BufferSizeData[] BufferSizes;
        [SerializeField]
        [HideInInspector] public int SampleCount;

        private int uFirstFrame = 1;
        [HideInInspector] public float IndirectBoost = 1;
        [HideInInspector] public int bouncecount = 24;
        [HideInInspector] public bool UseSVGF = false;
        [HideInInspector] public bool UseRussianRoulette = true;
        [HideInInspector] public bool UseNEE = true;
        [HideInInspector] public bool DoTLASUpdates = true;
        [HideInInspector] public bool AllowConverge = true;
        [HideInInspector] public bool AllowBloom = false;
        [HideInInspector] public bool AllowDoF = false;
        [HideInInspector] public bool AllowAutoExpose = false;
        [HideInInspector] public bool AllowToneMap = true;
        [HideInInspector] public bool AllowTAA = false;
        [HideInInspector] public float DoFAperature = 0.2f;
        [HideInInspector] public float DoFFocal = 0.2f;
        [HideInInspector] public float RenderScale = 1.0f;
        [HideInInspector] public float BloomStrength = 32.0f;
        [HideInInspector] public float MinSpatialSize = 10.0f;
        [HideInInspector] public bool UseASVGF = false;
        [HideInInspector] public bool UseReCur = false;
        [HideInInspector] public bool UseTAAU = true;
        [HideInInspector] public bool DoExposureAuto = false;
        [HideInInspector] public int ReSTIRGIUpdateRate = 0;
        [HideInInspector] public bool UseReSTIRGITemporal = false;
        [HideInInspector] public bool UseReSTIRGISpatial = false;
        [HideInInspector] public bool UseReSTIRGI = false;
        [HideInInspector] public int ReSTIRGISpatialCount = 5;
        [HideInInspector] public int ReSTIRGITemporalMCap = 0;
        [HideInInspector] public bool DoReSTIRGIConnectionValidation = false;
        [HideInInspector] public float Exposure = 1;
        [HideInInspector] public float ReCurBlurRadius = 30.0f;
        [HideInInspector] public bool PrevReSTIRGI = false;
        [HideInInspector] public bool DoPartialRendering = false;
        [HideInInspector] public int PartialRenderingFactor = 1;
        [HideInInspector] public bool DoFirefly = false;
        [HideInInspector] public bool ImprovedPrimaryHit = false;
        [HideInInspector] public bool ReSTIRDenoiser = false;
        [HideInInspector] public int RISCount = 5;
        [HideInInspector] public int ToneMapper = 0;
        public static bool SceneIsRunning = false;

        [HideInInspector] public int BackgroundType = 0;
        [HideInInspector] public Vector3 SceneBackgroundColor = Vector3.one;
        [HideInInspector] public Texture SkyboxTexture;
        [HideInInspector] public float BackgroundIntensity = 1;
        private bool MeshOrderChanged = false;



        [HideInInspector] public int SVGFAtrousKernelSizes = 6;
        [HideInInspector] public int AtmoNumLayers = 4;
        private float PrevResFactor;
        private int GenKernel;
        private int GenASVGFKernel;
        private int TraceKernel;
        private int ShadeKernel;
        private int ShadowKernel;
        private int FinalizeKernel;
        private int HeightmapShadowKernel;
        private int FramesSinceStart;
        private int HeightmapKernel;
        private int GIReTraceKernel;
        private int TransferKernel;
        private int CorrectedDistanceKernel;
        private int ReSTIRCorectKernel;
        private int ReSTIRGIKernel;
        private int TargetWidth;
        private int TargetHeight;
        [System.NonSerialized] public int SourceWidth;
        [System.NonSerialized] public int SourceHeight;
        private Vector3 PrevCamPosition;
        private bool PrevASVGF;
        private bool PrevReCur;
        private Matrix4x4 PrevViewProjection;


        [System.Serializable]
        public struct BufferSizeData
        {
            public int tracerays;
            public int shadow_rays;
            public int heightmap_rays;
            public int Heightmap_shadow_rays;
        }
        [HideInInspector] public bool HasStarted = false;


        public void TossCamera(Camera camera) {
            _camera = camera;
            _transformsToWatch.Clear();
            _transformsToWatch.Add(_camera.transform);
        }
        unsafe public void Start2()
        {
            Application.targetFrameRate = 165;
            ASVGFCode = new ASVGF();
            ReCurDen = new ReCurDenoiser();
            ReSTIRASVGFCode = new ReSTIRASVGF();
            ToneMapTex = Resources.Load<Texture3D>("Utility/ToneMapTex");
            if (ShadingShader == null) {ShadingShader = Resources.Load<ComputeShader>("MainCompute/RayTracingShader"); }
            if (IntersectionShader == null) {IntersectionShader = Resources.Load<ComputeShader>("MainCompute/IntersectionKernels"); }
            if (GenerateShader == null) {GenerateShader = Resources.Load<ComputeShader>("MainCompute/RayGenKernels"); }
            if (ReSTIRGI == null) {ReSTIRGI = Resources.Load<ComputeShader>("MainCompute/ReSTIRGI"); }
            TargetWidth = 1;// _camera.scaledPixelWidth;
            TargetHeight = 1;//_camera.scaledPixelHeight;
            SourceWidth = 1;//(int)Mathf.Ceil((float)TargetWidth * RenderScale);
            SourceHeight = 1;//(int)Mathf.Ceil((float)TargetHeight * RenderScale);
            if (Mathf.Abs(SourceWidth - TargetWidth) < 2)
            {
                SourceWidth = TargetWidth;
                SourceHeight = TargetHeight;
                RenderScale = 1;
            }
            PrevResFactor = RenderScale;
            _meshObjectsNeedRebuilding = true;
            Assets = gameObject.GetComponent<AssetManager>();
            Assets.BuildCombined();
            uFirstFrame = 1;
            FramesSinceStart = 0;
            GenKernel = GenerateShader.FindKernel("Generate");
            GenASVGFKernel = GenerateShader.FindKernel("GenerateASVGF");
            TraceKernel = IntersectionShader.FindKernel("kernel_trace");
            ShadowKernel = IntersectionShader.FindKernel("kernel_shadow");
            ShadeKernel = ShadingShader.FindKernel("kernel_shade");
            FinalizeKernel = ShadingShader.FindKernel("kernel_finalize");
            HeightmapShadowKernel = IntersectionShader.FindKernel("kernel_shadow_heightmap");
            HeightmapKernel = IntersectionShader.FindKernel("kernel_heightmap");
            GIReTraceKernel = ShadingShader.FindKernel("GIReTraceKernel");
            TransferKernel = ShadingShader.FindKernel("TransferKernel");
            CorrectedDistanceKernel = ShadingShader.FindKernel("DepthCopyKernel");
            ReSTIRCorectKernel = ShadingShader.FindKernel("ReSTIRCorectKernel");
            ReSTIRGIKernel = ReSTIRGI.FindKernel("ReSTIRGIKernel");

            ASVGFCode.Initialized = false;
            ReSTIRASVGFCode.Initialized = false;

            Atmo = new AtmosphereGenerator(6360, 6420, AtmoNumLayers);
            FramesSinceStart2 = 0;
            Denoisers = new Denoiser();
            Denoisers.Initialized = false;
            HasStarted = true;
        }

        private void OnEnable()
        {
            _currentSample = 0;
        }
        public void OnDisable()
        {
            _MaterialDataBuffer?.Release();
            _CompactedMeshData?.Release();
            _RayBuffer?.Release();
            LightingBuffer?.Release();
            PrevLightingBufferA?.Release();
            PrevLightingBufferB?.Release();
            _BufferSizes?.Release();
            _ShadowBuffer?.Release();
            _UnityLights?.Release();
            _LightMeshes?.Release();
            RaysBuffer?.Release();
            RaysBufferB?.Release();
            if(ASVGFCode != null) ASVGFCode.ClearAll();
            if(ReSTIRASVGFCode != null) ReSTIRASVGFCode.ClearAll();
            if(ReCurDen != null) ReCurDen.ClearAll();
            if (RaysBuffer != null) RaysBuffer.Release();
            if (RaysBufferB != null) RaysBufferB.Release();
            #if HardwareRT
                MeshIndexOffsets?.Release();
                SubMeshOffsetsBuffer?.Release();
            #endif
            CurBounceInfoBuffer?.Release();
        }
        public static Vector3 SunDirection;
        private void RunUpdate()
        {
            SunDirection = Assets.SunDirection;

            ShadingShader.SetVector("SunDir", SunDirection);
            if (!AllowConverge)
            {
                SampleCount = 0;
                FramesSinceStart = 0;
            }

            if (_camera.fieldOfView != _lastFieldOfView)
            {
                FramesSinceStart = 0;
                _lastFieldOfView = _camera.fieldOfView;
            }

            foreach (Transform t in _transformsToWatch)
            {
                if (t.hasChanged)
                {
                    SampleCount = 0;
                    FramesSinceStart = 0;
                    t.hasChanged = false;
                }
            }
        }

        public static void RegisterObject(RayTracingLights obj)
        {//Adds meshes to list
            _rayTracingLights.Add(obj);
            // _meshObjectsNeedRebuilding = true;
        }
        public static void UnregisterObject(RayTracingLights obj)
        {//Removes meshes from list
            _rayTracingLights.Remove(obj);
            // _meshObjectsNeedRebuilding = true;
        }

        public void RebuildMeshObjectBuffers(CommandBuffer cmd)
        {
            cmd.BeginSample("Full Update");
            if (uFirstFrame != 1)
            {
                if (DoTLASUpdates)
                {
                    int UpdateFlags = Assets.UpdateTLAS(cmd);
                    if (UpdateFlags == 1 || UpdateFlags == 3)
                    {
                        MeshOrderChanged = true;
                        CommonFunctions.CreateComputeBuffer(ref _CompactedMeshData, Assets.MyMeshesCompacted);
                        CommonFunctions.CreateComputeBuffer(ref _MaterialDataBuffer, Assets._Materials);
                        CommonFunctions.CreateComputeBuffer(ref _UnityLights, Assets.UnityLights);
                        CommonFunctions.CreateComputeBuffer(ref _LightMeshes, Assets.LightMeshes);
                        #if HardwareRT
                            CommonFunctions.CreateComputeBuffer(ref MeshIndexOffsets, Assets.MeshOffsets);
                            CommonFunctions.CreateComputeBuffer(ref SubMeshOffsetsBuffer, Assets.SubMeshOffsets);
                        #endif
                        uFirstFrame = 1;
                    }
                    else if(UpdateFlags == 2) {
                        MeshOrderChanged = false;
                        cmd.SetBufferData(_CompactedMeshData, Assets.MyMeshesCompacted);
                        CommonFunctions.CreateComputeBuffer(ref _UnityLights, Assets.UnityLights);
                        if (Assets.LightMeshCount != 0) cmd.SetBufferData(_LightMeshes, Assets.LightMeshes);
                        _MaterialDataBuffer.SetData(Assets._Materials);
                    } else {
                        MeshOrderChanged = false;
                        cmd.BeginSample("Update Materials");
                        cmd.SetBufferData(_CompactedMeshData, Assets.MyMeshesCompacted);
                        if (Assets.LightMeshCount != 0) cmd.SetBufferData(_LightMeshes, Assets.LightMeshes);
                        if (Assets.UnityLightCount != 0) cmd.SetBufferData(_UnityLights, Assets.UnityLights);
                        if(Assets.UpdateMaterials()) _MaterialDataBuffer.SetData(Assets._Materials);
                        cmd.EndSample("Update Materials");
                    }
                }
            }
            cmd.EndSample("Full Update");
            if (!_meshObjectsNeedRebuilding) return;
            _meshObjectsNeedRebuilding = false;
            FramesSinceStart = 0;
            CommonFunctions.CreateComputeBuffer(ref _UnityLights, Assets.UnityLights);
            CommonFunctions.CreateComputeBuffer(ref _LightMeshes, Assets.LightMeshes);

            CommonFunctions.CreateComputeBuffer(ref _MaterialDataBuffer, Assets._Materials);
            CommonFunctions.CreateComputeBuffer(ref _CompactedMeshData, Assets.MyMeshesCompacted);

            #if HardwareRT
                CommonFunctions.CreateComputeBuffer(ref MeshIndexOffsets, Assets.MeshOffsets);
                CommonFunctions.CreateComputeBuffer(ref SubMeshOffsetsBuffer, Assets.SubMeshOffsets);
            #endif

            if(CurBounceInfoBuffer != null) CurBounceInfoBuffer.Release();
            CurBounceInfoBuffer = new ComputeBuffer(1, 12);
            CommonFunctions.CreateDynamicBuffer(ref _RayBuffer, SourceWidth * SourceHeight, 40);
            CommonFunctions.CreateDynamicBuffer(ref _ShadowBuffer, SourceWidth * SourceHeight, 40);
            CommonFunctions.CreateDynamicBuffer(ref LightingBuffer, SourceWidth * SourceHeight, 48);
            CommonFunctions.CreateDynamicBuffer(ref PrevLightingBufferA, SourceWidth * SourceHeight, 48);
            CommonFunctions.CreateDynamicBuffer(ref PrevLightingBufferB, SourceWidth * SourceHeight, 48);
            CommonFunctions.CreateDynamicBuffer(ref RaysBuffer, SourceWidth * SourceHeight, 24);
            CommonFunctions.CreateDynamicBuffer(ref RaysBufferB, SourceWidth * SourceHeight, 24);
            GenerateShader.SetBuffer(GenASVGFKernel, "Rays", RaysBuffer);
        }

       
        private void SetMatrix(string Name, Matrix4x4 Mat) {
            ShadingShader.SetMatrix(Name, Mat);
            IntersectionShader.SetMatrix(Name, Mat);
            GenerateShader.SetMatrix(Name, Mat);
            ReSTIRGI.SetMatrix(Name, Mat);
        }

        private void SetVector(string Name, Vector3 IN) {
            ShadingShader.SetVector(Name, IN);
            IntersectionShader.SetVector(Name, IN);
            GenerateShader.SetVector(Name, IN);
            ReSTIRGI.SetVector(Name, IN);
        }

        private void SetInt(string Name, int IN, CommandBuffer cmd) {
            cmd.SetComputeIntParam(ShadingShader, Name, IN);
            cmd.SetComputeIntParam(IntersectionShader, Name, IN);
            cmd.SetComputeIntParam(GenerateShader, Name, IN);
            cmd.SetComputeIntParam(ReSTIRGI, Name, IN);
        }

        private void SetFloat(string Name, float IN) {
            ShadingShader.SetFloat(Name, IN);
            IntersectionShader.SetFloat(Name, IN);
            GenerateShader.SetFloat(Name, IN);
            ReSTIRGI.SetFloat(Name, IN);
        }

        private void SetBool(string Name, bool IN) {
            ShadingShader.SetBool(Name, IN);
            IntersectionShader.SetBool(Name, IN);
            GenerateShader.SetBool(Name, IN);
            ReSTIRGI.SetBool(Name, IN);
        }

        Matrix4x4 prevView;
        Matrix4x4 PrevCamToWorld;
        Matrix4x4 PrevCamInvProj;
        Vector3 PrevPos;
        private void SetShaderParameters(CommandBuffer cmd)
        {
            if(RenderScale != 1.0f) _camera.renderingPath = RenderingPath.DeferredShading;
            _camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;
            if(UseReSTIRGI && ReSTIRDenoiser && !ReSTIRASVGFCode.Initialized) ReSTIRASVGFCode.init(SourceWidth, SourceHeight);
            else if ((!ReSTIRDenoiser || !UseReSTIRGI) && ReSTIRASVGFCode.Initialized) ReSTIRASVGFCode.ClearAll();
            if (UseASVGF && !ASVGFCode.Initialized) ASVGFCode.init(SourceWidth, SourceHeight);
            else if (!UseASVGF && ASVGFCode.Initialized) ASVGFCode.ClearAll();
            if (UseSVGF && !Denoisers.SVGFInitialized && !UseASVGF) Denoisers.InitSVGF(SourceWidth, SourceHeight);
            else if ((UseASVGF || !UseSVGF) && Denoisers.SVGFInitialized) Denoisers.ClearSVGF();
            if (!UseReCur && PrevReCur) ReCurDen.ClearAll();
            else if(!PrevReCur && UseReCur) ReCurDen.init(SourceWidth, SourceHeight);
            if(Denoisers.Initialized == false) Denoisers.init(SourceWidth, SourceHeight);

            BufferSizes = new BufferSizeData[bouncecount + 1];
            BufferSizes[0].tracerays = SourceWidth * SourceHeight;
            BufferSizes[0].heightmap_rays = SourceWidth * SourceHeight;
            if(_BufferSizes == null) {
                _BufferSizes = new ComputeBuffer(bouncecount + 1, 16);
            }
            if(_BufferSizes.count != bouncecount + 1) {
                _BufferSizes.Release();
                _BufferSizes = new ComputeBuffer(bouncecount + 1, 16);
            }
            _BufferSizes.SetData(BufferSizes);
            GenerateShader.SetComputeBuffer(GenKernel, "BufferSizes", _BufferSizes);
            GenerateShader.SetComputeBuffer(GenASVGFKernel, "BufferSizes", _BufferSizes);
            IntersectionShader.SetComputeBuffer(TraceKernel, "BufferSizes", _BufferSizes);
            IntersectionShader.SetComputeBuffer(ShadowKernel, "BufferSizes", _BufferSizes);
            ShadingShader.SetComputeBuffer(ShadeKernel, "BufferSizes", _BufferSizes);
            ShadingShader.SetComputeBuffer(TransferKernel, "BufferSizes", _BufferSizes);
            ShadingShader.SetComputeBuffer(TransferKernel, "BufferData", CurBounceInfoBuffer);
            ShadingShader.SetComputeBuffer(ShadeKernel, "BufferData", CurBounceInfoBuffer);
            IntersectionShader.SetComputeBuffer(HeightmapShadowKernel, "BufferSizes", _BufferSizes);
            IntersectionShader.SetComputeBuffer(HeightmapKernel, "BufferSizes", _BufferSizes);

            SetMatrix("CamInvProj", _camera.projectionMatrix.inverse);
            SetMatrix("CamToWorld", _camera.cameraToWorldMatrix);
            SetMatrix("PrevCamInvProj", PrevCamInvProj);
            SetMatrix("PrevCamToWorld", PrevCamToWorld);
            SetMatrix("ViewMatrix", _camera.worldToCameraMatrix);
            var E = _camera.transform.position - PrevPos;
            SetVector("Up", _camera.transform.up);
            SetVector("Right", _camera.transform.right);
            SetVector("Forward", _camera.transform.forward);
            SetVector("CamPos", _camera.transform.position);
            SetVector("CamDelta", E);
            SetVector("BackgroundColor", SceneBackgroundColor);
            if(UseASVGF) ASVGFCode.shader.SetVector("CamDelta", E);
            if(ReSTIRDenoiser && UseReSTIRGI) ReSTIRASVGFCode.shader.SetVector("CamDelta", E);

            Shader.SetGlobalInt("PartialRenderingFactor", PartialRenderingFactor);
            SetFloat("FarPlane", _camera.farClipPlane);
            SetFloat("focal_distance", DoFFocal);
            SetFloat("AperatureRadius", DoFAperature);
            SetFloat("sun_angular_radius", 0.1f);
            SetFloat("IndirectBoost", IndirectBoost);
            SetFloat("fps", 1.0f / Time.smoothDeltaTime);
            SetFloat("GISpatialRadius", MinSpatialSize);


            // ShadingShader.SetTexture(ShadeKernel, "_ShapeTexture", Atmo.CloudTex1);
            // ShadingShader.SetTexture(ShadeKernel, "_DetailTexture", Atmo.CloudTex2);
            // ShadingShader.SetTexture(ShadeKernel, "_WeatherTexture", WeatherTex);
            // ShadingShader.SetTexture(ShadeKernel, "_CurlNoise", CurlNoiseTex);

            SetInt("LightMeshCount", Assets.LightMeshCount, cmd);
            SetInt("unitylightcount", Assets.UnityLightCount, cmd);
            SetInt("screen_width", SourceWidth, cmd);
            SetInt("screen_height", SourceHeight, cmd);
            SetInt("MaxBounce", bouncecount - 1, cmd);
            SetInt("frames_accumulated", _currentSample, cmd);
            SetInt("ReSTIRGISpatialCount", ReSTIRGISpatialCount, cmd);
            SetInt("ReSTIRGITemporalMCap", ReSTIRGITemporalMCap, cmd);
            SetInt("curframe", FramesSinceStart2, cmd);
            SetInt("TerrainCount", Assets.Terrains.Count, cmd);
            SetInt("ReSTIRGIUpdateRate", UseReSTIRGI ? ReSTIRGIUpdateRate : 0, cmd);
            SetInt("TargetWidth", TargetWidth, cmd);
            SetInt("TargetHeight", TargetHeight, cmd);
            SetInt("RISCount", RISCount, cmd);
            SetInt("NormalSize", Assets.NormalSize, cmd);
            SetInt("EmissiveSize", Assets.EmissiveSize, cmd);
            SetInt("BackgroundType", BackgroundType, cmd);
            SetInt("MaterialCount", Assets.MatCount, cmd);
            SetInt("PartialRenderingFactor", PartialRenderingFactor, cmd);
            SetFloat("AtlasSize", Assets.DesiredRes);
            SetFloat("BackgroundIntensity", BackgroundIntensity);
            SetInt("AlbedoAtlasSize", Assets.AlbedoAtlasSize, cmd);
            SetInt("PartialRenderingFactor", DoPartialRendering ? PartialRenderingFactor : 1, cmd);

            SetBool("UseReCur", UseReCur);
            SetBool("ImprovedPrimaryHit", ImprovedPrimaryHit);
            SetBool("UseRussianRoulette", UseRussianRoulette);
            SetBool("UseNEE", UseNEE);
            SetBool("UseDoF", AllowDoF);
            SetBool("UseReSTIRGI", UseReSTIRGI);
            SetBool("UseReSTIRGITemporal", UseReSTIRGITemporal);
            SetBool("UseReSTIRGISpatial", UseReSTIRGISpatial);
            SetBool("DoReSTIRGIConnectionValidation", DoReSTIRGIConnectionValidation);
            SetBool("UseASVGF", UseASVGF);
            var C = UseASVGF != PrevASVGF || UseReSTIRGI != PrevReSTIRGI;
            SetBool("AbandonSamples", C);
            SetBool("TerrainExists", Assets.Terrains.Count != 0);
            SetBool("DoPartialRendering", DoPartialRendering);

            SetBool("DoPartialRendering", DoPartialRendering);
            SetBool("ChangedExposure", AllowAutoExpose);
            SetBool("DoHeightmap", Assets.DoHeightmap);
            if(AllowAutoExpose) {
                SetBool("DoExposure", true);
                ShadingShader.SetBuffer(ShadeKernel, "Exposure", Denoisers.ExposureBuffer);
            } else {
                SetBool("DoExposure", false);
                ShadingShader.SetBuffer(ShadeKernel, "Exposure", Denoisers.ExposureBuffer);                
            }

            var Temp = prevView;
            PrevPos = _camera.transform.position;
            ShadingShader.SetMatrix("prevviewmatrix", Temp);
            prevView = _camera.worldToCameraMatrix;

            bool FlipFrame = (FramesSinceStart2 % 2 == 0);


            ShadingShader.SetTextureFromGlobal(CorrectedDistanceKernel, "Depth", "_CameraDepthTexture");
            ShadingShader.SetTextureFromGlobal(GIReTraceKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            ReSTIRGI.SetTextureFromGlobal(ReSTIRGIKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            ShadingShader.SetTextureFromGlobal(ShadeKernel, "MotionVectors", "_CameraMotionVectorsTexture");

            ShadingShader.SetTextureFromGlobal(FinalizeKernel, "DiffuseGBuffer", "_CameraGBufferTexture0");
            ShadingShader.SetTextureFromGlobal(FinalizeKernel, "SpecularGBuffer", "_CameraGBufferTexture1");

            IntersectionShader.SetComputeBuffer(HeightmapKernel, "Terrains", Assets.TerrainBuffer);

            SetBool("DiffRes", RenderScale != 1.0f);
            #if HardwareRT                
                IntersectionShader.SetRayTracingAccelerationStructure(TraceKernel, "myAccelerationStructure", Assets.AccelStruct);
                IntersectionShader.SetRayTracingAccelerationStructure(ShadowKernel, "myAccelerationStructure", Assets.AccelStruct);
                ReSTIRGI.SetRayTracingAccelerationStructure(ReSTIRGIKernel, "myAccelerationStructure", Assets.AccelStruct);
                ShadingShader.SetBuffer(ShadeKernel, "MeshOffsets", MeshIndexOffsets);
                ReSTIRGI.SetBuffer(ReSTIRGIKernel, "MeshOffsets", MeshIndexOffsets);
                IntersectionShader.SetBuffer(TraceKernel, "MeshOffsets", MeshIndexOffsets);
                IntersectionShader.SetBuffer(ShadowKernel, "MeshOffsets", MeshIndexOffsets);
                IntersectionShader.SetBuffer(TraceKernel, "SubMeshOffsets", SubMeshOffsetsBuffer);
                ReSTIRGI.SetBuffer(ReSTIRGIKernel, "SubMeshOffsets", SubMeshOffsetsBuffer);
                IntersectionShader.SetBuffer(ShadowKernel, "SubMeshOffsets", SubMeshOffsetsBuffer);
                ShadingShader.SetBuffer(ShadeKernel, "SubMeshOffsets", SubMeshOffsetsBuffer);
            #endif

            if (SkyboxTexture == null) SkyboxTexture = new Cubemap(1, TextureFormat.RGBA32, false);
            if (SkyboxTexture != null)
            {
                ShadingShader.SetTexture(ShadeKernel, "_SkyboxTexture", SkyboxTexture);
            }
            IntersectionShader.SetComputeBuffer(HeightmapKernel, "GlobalRays", _RayBuffer);
            IntersectionShader.SetTexture(HeightmapKernel, "Heightmap", Assets.HeightmapAtlas);
            IntersectionShader.SetTexture(HeightmapKernel, "_PrimaryTriangleInfo", _PrimaryTriangleInfo);


            ShadingShader.SetTexture(CorrectedDistanceKernel, "CorrectedDepthTex", FlipFrame ? CorrectedDistanceTex : CorrectedDistanceTexB);

            if (_RandomNums == null) CommonFunctions.CreateRenderTexture(ref _RandomNums, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
            if (_RandomNumsB == null) CommonFunctions.CreateRenderTexture(ref _RandomNumsB, SourceWidth, SourceHeight, CommonFunctions.RTFull4);

            GenerateShader.SetComputeBuffer(GenASVGFKernel, "GlobalRays", _RayBuffer);
            GenerateShader.SetComputeBuffer(GenASVGFKernel, "GlobalColors", LightingBuffer);
            GenerateShader.SetBuffer(GenASVGFKernel, "Rays", (FramesSinceStart2 % 2 == 0) ? RaysBuffer : RaysBufferB);
            GenerateShader.SetTexture(GenASVGFKernel, "RandomNums", FlipFrame ? _RandomNums : _RandomNumsB);
            GenerateShader.SetTexture(GenASVGFKernel, "WorldPosA", GIWorldPosA);
            GenerateShader.SetTexture(GenASVGFKernel, "NEEPosA", GINEEPosA);

            GenerateShader.SetTexture(GenKernel, "CorrectedDepthTex", FlipFrame ? CorrectedDistanceTex : CorrectedDistanceTexB);
            GenerateShader.SetTexture(GenKernel, "RandomNums", (FramesSinceStart2 % 2 == 0) ? _RandomNums : _RandomNumsB);
            GenerateShader.SetComputeBuffer(GenKernel, "GlobalRays", _RayBuffer);
            GenerateShader.SetComputeBuffer(GenKernel, "GlobalColors", LightingBuffer);

            IntersectionShader.SetComputeBuffer(TraceKernel, "TLASBVH8Indices", Assets.TLASCWBVHIndexes);
            IntersectionShader.SetComputeBuffer(TraceKernel, "GlobalRays", _RayBuffer);
            IntersectionShader.SetComputeBuffer(TraceKernel, "AggTris", Assets.AggTriBuffer);
            IntersectionShader.SetComputeBuffer(TraceKernel, "cwbvh_nodes", Assets.BVH8AggregatedBuffer);
            IntersectionShader.SetComputeBuffer(TraceKernel, "_MeshData", _CompactedMeshData);
            IntersectionShader.SetComputeBuffer(TraceKernel, "_Materials", _MaterialDataBuffer);
            IntersectionShader.SetTexture(TraceKernel, "_TextureAtlas", Assets.AlbedoAtlas);
            IntersectionShader.SetTexture(TraceKernel, "VideoTex", Assets.VideoTexture);
            IntersectionShader.SetTexture(TraceKernel, "_PrimaryTriangleInfo", _PrimaryTriangleInfo);



            IntersectionShader.SetComputeBuffer(ShadowKernel, "TLASBVH8Indices", Assets.TLASCWBVHIndexes);
            IntersectionShader.SetComputeBuffer(ShadowKernel, "_MeshData", _CompactedMeshData);
            IntersectionShader.SetComputeBuffer(ShadowKernel, "cwbvh_nodes", Assets.BVH8AggregatedBuffer);
            IntersectionShader.SetComputeBuffer(ShadowKernel, "AggTris", Assets.AggTriBuffer);
            IntersectionShader.SetComputeBuffer(ShadowKernel, "ShadowRaysBuffer", _ShadowBuffer);
            IntersectionShader.SetComputeBuffer(ShadowKernel, "_Materials", _MaterialDataBuffer);
            IntersectionShader.SetComputeBuffer(ShadowKernel, "GlobalColors", LightingBuffer);
            IntersectionShader.SetTexture(ShadowKernel, "_TextureAtlas", Assets.AlbedoAtlas);
            IntersectionShader.SetTexture(ShadowKernel, "VideoTex", Assets.VideoTexture);
            IntersectionShader.SetTexture(ShadowKernel, "NEEPosA", GINEEPosA);


            IntersectionShader.SetComputeBuffer(HeightmapShadowKernel, "ShadowRaysBuffer", _ShadowBuffer);
            IntersectionShader.SetComputeBuffer(HeightmapShadowKernel, "GlobalColors", LightingBuffer);
            IntersectionShader.SetComputeBuffer(HeightmapShadowKernel, "Terrains", Assets.TerrainBuffer);
            IntersectionShader.SetTexture(HeightmapShadowKernel, "Heightmap", Assets.HeightmapAtlas);


            ShadingShader.SetTexture(ShadeKernel, "WorldPosA", GIWorldPosA);
            ShadingShader.SetTexture(ShadeKernel, "WorldPosB", !FlipFrame ? GIWorldPosB : GIWorldPosC);
            ShadingShader.SetTexture(ShadeKernel, "NEEPosA", GINEEPosA);
            ShadingShader.SetTexture(ShadeKernel, "Heightmap", Assets.HeightmapAtlas);
            ShadingShader.SetTexture(ShadeKernel, "TerrainAlphaMap", Assets.AlphaMapAtlas);
            ShadingShader.SetTexture(ShadeKernel, "RandomNums", FlipFrame ? _RandomNums : _RandomNumsB);
            ShadingShader.SetTexture(ShadeKernel, "MetallicTex", Assets.MetallicAtlas);
            ShadingShader.SetTexture(ShadeKernel, "RoughnessTex", Assets.RoughnessAtlas);
            ShadingShader.SetTexture(ShadeKernel, "_TextureAtlas", Assets.AlbedoAtlas);
            ShadingShader.SetTexture(ShadeKernel, "_EmissiveAtlas", Assets.EmissiveAtlas);
            ShadingShader.SetTexture(ShadeKernel, "_NormalAtlas", Assets.NormalAtlas);
            ShadingShader.SetTexture(ShadeKernel, "scattering_texture", Atmo.MultiScatterTex);
            ShadingShader.SetTexture(ShadeKernel, "TransmittanceTex", Atmo._TransmittanceLUT);
            ShadingShader.SetTexture(ShadeKernel, "TempAlbedoTex", _Albedo);
            ShadingShader.SetTexture(ShadeKernel, "VideoTex", Assets.VideoTexture);
            ShadingShader.SetTexture(ShadeKernel, "ScreenSpaceInfo", FlipFrame ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
            ShadingShader.SetComputeBuffer(ShadeKernel, "Terrains", Assets.TerrainBuffer);
            ShadingShader.SetComputeBuffer(ShadeKernel, "_LightMeshes", _LightMeshes);
            ShadingShader.SetComputeBuffer(ShadeKernel, "_Materials", _MaterialDataBuffer);
            ShadingShader.SetComputeBuffer(ShadeKernel, "GlobalRays", _RayBuffer);
            ShadingShader.SetComputeBuffer(ShadeKernel, "LightTriangles", Assets.LightTriBuffer);
            ShadingShader.SetComputeBuffer(ShadeKernel, "ShadowRaysBuffer", _ShadowBuffer);
            ShadingShader.SetComputeBuffer(ShadeKernel, "AggTris", Assets.AggTriBuffer);
            ShadingShader.SetComputeBuffer(ShadeKernel, "GlobalColors", LightingBuffer);
            ShadingShader.SetComputeBuffer(ShadeKernel, "_MeshData", _CompactedMeshData);
            ShadingShader.SetComputeBuffer(ShadeKernel, "_UnityLights", _UnityLights);


            ShadingShader.SetBuffer(FinalizeKernel, "GlobalColors", LightingBuffer);
            ShadingShader.SetTexture(FinalizeKernel, "Result", _target);
            ShadingShader.SetTexture(FinalizeKernel, "TempAlbedoTex", _Albedo);

            ShadingShader.SetTexture(ReSTIRCorectKernel, "ReservoirA", FlipFrame ? GIReservoirB : GIReservoirC);
            ShadingShader.SetComputeBuffer(ReSTIRCorectKernel, "PrevGlobalColorsB", FlipFrame ? PrevLightingBufferB : PrevLightingBufferA);

            ShadingShader.SetComputeBuffer(GIReTraceKernel, "PrevGlobalColorsA", FlipFrame ? PrevLightingBufferA : PrevLightingBufferB);
            ShadingShader.SetBuffer(GIReTraceKernel, "Rays", FlipFrame ? RaysBuffer : RaysBufferB);
            ShadingShader.SetTexture(GIReTraceKernel, "RandomNumsWrite", FlipFrame ? _RandomNums : _RandomNumsB);
            ShadingShader.SetTexture(GIReTraceKernel, "ReservoirA", !FlipFrame ? GIReservoirB : GIReservoirC);
            ShadingShader.SetTexture(GIReTraceKernel, "RandomNums", FlipFrame ? _RandomNums : _RandomNumsB);


            ReSTIRGI.SetComputeBuffer(ReSTIRGIKernel, "TLASBVH8Indices", Assets.TLASCWBVHIndexes);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "ReservoirC", GIReservoirA);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "ReservoirA", FlipFrame ? GIReservoirB : GIReservoirC);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "ReservoirB", !FlipFrame ? GIReservoirB : GIReservoirC);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "WorldPosC", GIWorldPosA);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "WorldPosA", FlipFrame ? GIWorldPosB : GIWorldPosC);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "WorldPosB", !FlipFrame ? GIWorldPosB : GIWorldPosC);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "NEEPosC", GINEEPosA);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "NEEPosA", FlipFrame ? GINEEPosB : GINEEPosC);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "NEEPosB", !FlipFrame ? GINEEPosB : GINEEPosC);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "TempAlbedoTex", _Albedo);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "_TextureAtlas", Assets.AlbedoAtlas);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "RandomNums", FlipFrame ? _RandomNums : _RandomNumsB);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "PrevScreenSpaceInfo", FlipFrame ? ScreenSpaceInfoPrev : ScreenSpaceInfo);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "ScreenSpaceInfoRead", FlipFrame ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "GradientWrite", Gradients);
            ReSTIRGI.SetComputeBuffer(ReSTIRGIKernel, "PrevGlobalColorsA", FlipFrame ? PrevLightingBufferA : PrevLightingBufferB);
            ReSTIRGI.SetComputeBuffer(ReSTIRGIKernel, "PrevGlobalColorsB", FlipFrame ? PrevLightingBufferB : PrevLightingBufferA);
            ReSTIRGI.SetComputeBuffer(ReSTIRGIKernel, "_Materials", _MaterialDataBuffer);
            ReSTIRGI.SetComputeBuffer(ReSTIRGIKernel, "GlobalColors", LightingBuffer);
            ReSTIRGI.SetComputeBuffer(ReSTIRGIKernel, "AggTris", Assets.AggTriBuffer);
            ReSTIRGI.SetComputeBuffer(ReSTIRGIKernel, "cwbvh_nodes", Assets.BVH8AggregatedBuffer);
            ReSTIRGI.SetComputeBuffer(ReSTIRGIKernel, "_MeshData", _CompactedMeshData);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "PrimaryTriData", _PrimaryTriangleInfo);

            GenerateShader.SetTexture(GenKernel, "_DebugTex", _DebugTex);
            ShadingShader.SetTexture(ShadeKernel, "_DebugTex", _DebugTex);
            IntersectionShader.SetTexture(TraceKernel, "_DebugTex", _DebugTex);
            ShadingShader.SetTexture(FinalizeKernel, "_DebugTex", _DebugTex);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "_DebugTex", _DebugTex);
            IntersectionShader.SetTexture(HeightmapKernel, "_DebugTex", _DebugTex);
            IntersectionShader.SetTexture(ShadowKernel, "_DebugTex", _DebugTex);

            ReSTIRGI.SetTexture(ReSTIRGIKernel, "_DebugTex", _DebugTex);
        }

        private void ResetAllTextures() {
            // _camera.renderingPath = RenderingPath.DeferredShading;
            _camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;
            if(PrevResFactor != RenderScale || TargetWidth != _camera.scaledPixelWidth) {
                TargetWidth = _camera.scaledPixelWidth;
                TargetHeight = _camera.scaledPixelHeight;
                SourceWidth = (int)Mathf.Ceil((float)TargetWidth * RenderScale);
                SourceHeight = (int)Mathf.Ceil((float)TargetHeight * RenderScale);
                if (Mathf.Abs(SourceWidth - TargetWidth) < 2)
                {
                    SourceWidth = TargetWidth;
                    SourceHeight = TargetHeight;
                    RenderScale = 1;
                }
                PrevResFactor = RenderScale;
                if(ReSTIRDenoiser && UseReSTIRGI) {ReSTIRASVGFCode.ClearAll(); ReSTIRASVGFCode.init(SourceWidth, SourceHeight);}
                if (UseASVGF) {ASVGFCode.ClearAll(); ASVGFCode.init(SourceWidth, SourceHeight);}
                if (UseSVGF && !UseASVGF) {Denoisers.ClearSVGF(); Denoisers.InitSVGF(SourceWidth, SourceHeight);}
                if(UseReCur) {ReCurDen.ClearAll(); ReCurDen.init(SourceWidth, SourceHeight);}
                if(Denoisers.Initialized) Denoisers.ClearAll();
                Denoisers.init(SourceWidth, SourceHeight);

                InitRenderTexture(true);
                CommonFunctions.CreateDynamicBuffer(ref _RayBuffer, SourceWidth * SourceHeight, 40);
                CommonFunctions.CreateDynamicBuffer(ref _ShadowBuffer, SourceWidth * SourceHeight, 40);
                CommonFunctions.CreateDynamicBuffer(ref LightingBuffer, SourceWidth * SourceHeight, 48);
                CommonFunctions.CreateDynamicBuffer(ref PrevLightingBufferA, SourceWidth * SourceHeight, 48);
                CommonFunctions.CreateDynamicBuffer(ref PrevLightingBufferB, SourceWidth * SourceHeight, 48);
                CommonFunctions.CreateDynamicBuffer(ref RaysBuffer, SourceWidth * SourceHeight, 24);
                CommonFunctions.CreateDynamicBuffer(ref RaysBufferB, SourceWidth * SourceHeight, 24);
                CommonFunctions.CreateRenderTexture(ref _RandomNums, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref _RandomNumsB, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
            }
            PrevResFactor = RenderScale;
        }


        private void InitRenderTexture(bool ForceReset = false)
        {
            if (ForceReset || _target == null || _target.width != SourceWidth || _target.height != SourceHeight)
            {
                // Release render texture if we already have one
                if (_target != null)
                {
                    _target.Release();
                    _converged.Release();
                    _Albedo.Release();
                    _IntermediateTex.Release();
                    _DebugTex.Release();
                    _FinalTex.Release();
                    CorrectedDistanceTex.Release();
                    CorrectedDistanceTexB.Release();
                    GIReservoirA.Release();
                    GIReservoirB.Release();
                    GIReservoirC.Release();
                    Gradients.Release();
                }
                CommonFunctions.CreateRenderTexture(ref _DebugTex, SourceWidth, SourceHeight, CommonFunctions.RTHalf4, RenderTextureReadWrite.sRGB);
                CommonFunctions.CreateRenderTexture(ref _FinalTex, TargetWidth, TargetHeight, CommonFunctions.RTFull4, RenderTextureReadWrite.sRGB, true);
                CommonFunctions.CreateRenderTexture(ref _IntermediateTex, TargetWidth, TargetHeight, CommonFunctions.RTFull4, RenderTextureReadWrite.sRGB, true);
                CommonFunctions.CreateRenderTexture(ref _target, SourceWidth, SourceHeight, CommonFunctions.RTFull4, RenderTextureReadWrite.sRGB);
                CommonFunctions.CreateRenderTexture(ref _converged, SourceWidth, SourceHeight, CommonFunctions.RTFull4, RenderTextureReadWrite.sRGB);
                CommonFunctions.CreateRenderTexture(ref _Albedo, SourceWidth, SourceHeight, CommonFunctions.RTHalf4, RenderTextureReadWrite.sRGB);
                CommonFunctions.CreateRenderTexture(ref CorrectedDistanceTex, SourceWidth, SourceHeight, CommonFunctions.RTHalf2);
                CommonFunctions.CreateRenderTexture(ref CorrectedDistanceTexB, SourceWidth, SourceHeight, CommonFunctions.RTHalf2);
                CommonFunctions.CreateRenderTexture(ref GIReservoirA, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
                CommonFunctions.CreateRenderTexture(ref GIReservoirB, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
                CommonFunctions.CreateRenderTexture(ref GIReservoirC, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
                CommonFunctions.CreateRenderTexture(ref GINEEPosA, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
                CommonFunctions.CreateRenderTexture(ref GINEEPosB, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
                CommonFunctions.CreateRenderTexture(ref GINEEPosC, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
                CommonFunctions.CreateRenderTexture(ref GIWorldPosA, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref GIWorldPosB, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref GIWorldPosC, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref _PrimaryTriangleInfo, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref ScreenSpaceInfo, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref ScreenSpaceInfoPrev, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref Gradients, SourceWidth / 3, SourceHeight / 3, CommonFunctions.RTHalf2);
                // Reset sampling
                _currentSample = 0;
            }
        }
        public void ClearOutRenderTexture(RenderTexture renderTexture)
        {
            RenderTexture rt = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = rt;
        }

        private void Render(RenderTexture destination, CommandBuffer cmd)
        {
            float CurrentSample;
            cmd.BeginSample("Linearize and Copy Depth");
            cmd.DispatchCompute(ShadingShader, CorrectedDistanceKernel, Mathf.CeilToInt(SourceWidth / 32.0f), Mathf.CeilToInt(SourceHeight / 32.0f), 1);
            cmd.EndSample("Linearize and Copy Depth");
            
            if (UseASVGF) {
                cmd.BeginSample("ASVGF Reproject Pass");
                ASVGFCode.shader.SetBool("ReSTIRGI", UseReSTIRGI);
                ASVGFCode.DoRNG(ref _RandomNums, ref _RandomNumsB, FramesSinceStart2, ref RaysBuffer, ref RaysBufferB, (FramesSinceStart2 % 2 == 1) ? CorrectedDistanceTex : CorrectedDistanceTexB, cmd, (FramesSinceStart2 % 2 == 0) ? CorrectedDistanceTex : CorrectedDistanceTexB, _PrimaryTriangleInfo, _CompactedMeshData, Assets.AggTriBuffer, MeshOrderChanged, Assets.TLASCWBVHIndexes);
                GenerateShader.SetBuffer(GenASVGFKernel, "Rays", (FramesSinceStart2 % 2 == 0) ? RaysBuffer : RaysBufferB);
                ASVGFCode.shader.SetTexture(1, "ScreenSpaceInfoWrite", (FramesSinceStart2 % 2 == 0) ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
                cmd.EndSample("ASVGF Reproject Pass");
            }

            if(UseReSTIRGI && ReSTIRGIUpdateRate != 0) {
                cmd.BeginSample("ReSTIR GI Reproject");
                cmd.DispatchCompute(ShadingShader, GIReTraceKernel, Mathf.CeilToInt(SourceWidth / 24.0f), Mathf.CeilToInt(SourceHeight / 24.0f), 1);
                cmd.EndSample("ReSTIR GI Reproject");
            }

            cmd.BeginSample("Pathtracing Kernels");
                cmd.BeginSample("Primary Ray Generation");
                SetInt("CurBounce", 0, cmd);
                cmd.DispatchCompute(GenerateShader, (UseASVGF || (UseReSTIRGI && ReSTIRGIUpdateRate != 0)) ? GenASVGFKernel : GenKernel, Mathf.CeilToInt(SourceWidth / 256.0f), SourceHeight, 1);
                cmd.EndSample("Primary Ray Generation");

                for (int i = 0; i < bouncecount; i++) {
                    var bouncebounce = i;
                    SetInt("CurBounce", bouncebounce, cmd);
                    cmd.BeginSample("Transfer Kernel: " + i);
                    cmd.DispatchCompute(ShadingShader, TransferKernel, 1, 1, 1);
                    cmd.EndSample("Transfer Kernel: " + i);

                    cmd.BeginSample("Trace Kernel: " + i);
                    cmd.DispatchCompute(IntersectionShader, TraceKernel, 784, 1, 1);//784 is 28^2
                    cmd.EndSample("Trace Kernel: " + i);

                    if (Assets.Terrains.Count != 0) {
                        cmd.BeginSample("HeightMap Trace Kernel: " + i);
                        cmd.DispatchCompute(IntersectionShader, HeightmapKernel, 784, 1, 1);
                        cmd.EndSample("HeightMap Trace Kernel: " + i);
                    }

                    cmd.BeginSample("Shading Kernel: " + i);
                    #if DX11Only
                        cmd.DispatchCompute(ShadingShader, ShadeKernel, Mathf.CeilToInt((SourceHeight * SourceWidth) / 64.0f), 1, 1);
                    #else
                        cmd.DispatchCompute(ShadingShader, ShadeKernel, CurBounceInfoBuffer, 0);
                    #endif
                    cmd.EndSample("Shading Kernel: " + i);
                    if (UseNEE) {
                        cmd.BeginSample("Shadow Kernel: " + i);
                        cmd.DispatchCompute(IntersectionShader, ShadowKernel, 784, 1, 1);
                        cmd.EndSample("Shadow Kernel: " + i);
                    }
                    if (UseNEE && Assets.Terrains.Count != 0) {
                        cmd.BeginSample("Heightmap Shadow Kernel: " + i);
                        cmd.DispatchCompute(IntersectionShader, HeightmapShadowKernel, 784, 1, 1);
                        cmd.EndSample("Heightmap Shadow Kernel: " + i);
                    }
                }
            cmd.EndSample("Pathtracing Kernels");


            if (UseReSTIRGI) {
                SetInt("CurBounce", 0, cmd);
                cmd.BeginSample("ReSTIRGI Temporal Kernel");
                cmd.DispatchCompute(ReSTIRGI, ReSTIRGIKernel, Mathf.CeilToInt(SourceWidth / 12.0f), Mathf.CeilToInt(SourceHeight / 12.0f), 1);
                cmd.EndSample("ReSTIRGI Temporal Kernel");
                cmd.BeginSample("ReSTIRGI CorrectionKernel");
                cmd.DispatchCompute(ShadingShader, ReSTIRCorectKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                cmd.EndSample("ReSTIRGI CorrectionKernel");

            }

            if (!UseSVGF && !UseASVGF && !UseReCur && !(UseReSTIRGI && ReSTIRDenoiser))
            {
                cmd.BeginSample("Finalize Kernel");
                cmd.DispatchCompute(ShadingShader, FinalizeKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                CurrentSample = 1.0f / (FramesSinceStart + 1.0f);
                SampleCount++;

                if (_addMaterial == null)
                    _addMaterial = new Material(Shader.Find("Hidden/Accumulate"));
                _addMaterial.SetFloat("_Sample", CurrentSample);
                cmd.Blit(_target, _converged, _addMaterial);
                cmd.EndSample("Finalize Kernel");
            }
            else if (!UseASVGF && !UseReCur && !(UseReSTIRGI && ReSTIRDenoiser))
            {
                cmd.BeginSample("SVGF");
                SampleCount = 0;
                Denoisers.ExecuteSVGF(FramesSinceStart2, 
                                    SVGFAtrousKernelSizes, 
                                    ref LightingBuffer, 
                                    ref _converged, 
                                    ref _Albedo, 
                                    ((FramesSinceStart2 % 2 == 0) ? ScreenSpaceInfo : ScreenSpaceInfoPrev),
                                    ((FramesSinceStart2 % 2 == 1) ? ScreenSpaceInfo : ScreenSpaceInfoPrev),
                                    GIWorldPosA,
                                    RenderScale != 1.0f, 
                                    UseReSTIRGI,
                                    cmd);
                CurrentSample = 1;
                cmd.EndSample("SVGF");
            }
            else if(!UseReCur && !(UseReSTIRGI && ReSTIRDenoiser))
            {
                cmd.BeginSample("ASVGF");
                SampleCount = 0;
                ASVGFCode.Do(ref LightingBuffer, ref _Albedo, ref _converged, RenderScale, (FramesSinceStart2 % 2 == 1) ? CorrectedDistanceTex : CorrectedDistanceTexB, ((FramesSinceStart2 % 2 == 0) ? ScreenSpaceInfo : ScreenSpaceInfoPrev), cmd, (FramesSinceStart2 % 2 == 0) ? CorrectedDistanceTex : CorrectedDistanceTexB, FramesSinceStart2, ref GIWorldPosA, DoPartialRendering ? PartialRenderingFactor : 1, Denoisers.ExposureBuffer, AllowAutoExpose, IndirectBoost, (FramesSinceStart2 % 2 == 0) ? _RandomNums : _RandomNumsB);
                CurrentSample = 1;
                cmd.EndSample("ASVGF");
            } else if(UseReSTIRGI && ReSTIRDenoiser && !UseReCur) {
                cmd.BeginSample("ReSTIR ASVGF");
                SampleCount = 0;
                ReSTIRASVGFCode.Do(ref LightingBuffer, 
                                    ref _Albedo, 
                                    ref _converged, 
                                    RenderScale, 
                                    (FramesSinceStart2 % 2 == 1) ? CorrectedDistanceTex : CorrectedDistanceTexB, 
                                    ((FramesSinceStart2 % 2 == 0) ? ScreenSpaceInfo : ScreenSpaceInfoPrev), 
                                    cmd, 
                                    (FramesSinceStart2 % 2 == 0) ? CorrectedDistanceTex : CorrectedDistanceTexB, 
                                    FramesSinceStart2, 
                                    ref GIWorldPosA, 
                                    DoPartialRendering ? PartialRenderingFactor : 1, 
                                    Denoisers.ExposureBuffer, 
                                    AllowAutoExpose, 
                                    IndirectBoost, 
                                    Gradients);
                CurrentSample = 1;
                cmd.EndSample("ReSTIR ASVGF");
            } else {
                cmd.BeginSample("ReCur");
                SampleCount = 0;
                ReCurDen.Do(ref _converged, ref _Albedo, ref LightingBuffer, ((FramesSinceStart2 % 2 == 0) ? ScreenSpaceInfo : ScreenSpaceInfoPrev), CorrectedDistanceTexB, CorrectedDistanceTex, ((FramesSinceStart2 % 2 == 0) ? GIReservoirB : GIReservoirC), ((FramesSinceStart2 % 2 == 1) ? GIReservoirB : GIReservoirC), GIWorldPosA, cmd, FramesSinceStart2, UseReSTIRGI, RenderScale, ReCurBlurRadius, DoPartialRendering ? PartialRenderingFactor : 1, IndirectBoost, Gradients);
                CurrentSample = 1.0f;
                cmd.EndSample("ReCur");
            }
            cmd.BeginSample("Firefly Blit");
            if (_FireFlyMaterial == null)
                _FireFlyMaterial = new Material(Shader.Find("Hidden/FireFlyPass"));
            if(DoFirefly) cmd.Blit(_converged, _target, _FireFlyMaterial);
            if(DoFirefly) cmd.Blit(_target, _converged);
            cmd.EndSample("Firefly Blit");


            cmd.BeginSample("Post Processing");
            if (SourceWidth != TargetWidth)
            {
                if (UseTAAU) Denoisers.ExecuteTAAU(ref _FinalTex, ref _converged, ref _Albedo, cmd, FramesSinceStart2, (FramesSinceStart2 % 2 == 0) ? CorrectedDistanceTex : CorrectedDistanceTexB);
                else Denoisers.ExecuteUpsample(ref _converged, ref _FinalTex, FramesSinceStart2, _currentSample, ref _Albedo, cmd, ((FramesSinceStart2 % 2 == 0) ? ScreenSpaceInfo : ScreenSpaceInfoPrev));//This is a postprocessing pass, but im treating it like its not one, need to move it to after the accumulation
            }
            else
            {
                cmd.CopyTexture(_converged, 0, 0, _FinalTex, 0, 0);
            }

            if (AllowAutoExpose)
            {
                _FinalTex.GenerateMips();
                Denoisers.ExecuteAutoExpose(ref _FinalTex, Exposure, cmd, DoExposureAuto);
            }
            if (AllowBloom) Denoisers.ExecuteBloom(ref _FinalTex, BloomStrength, cmd);
            if(AllowToneMap) Denoisers.ExecuteToneMap(ref _FinalTex, cmd, ref ToneMapTex, ToneMapper);
            if (AllowTAA) Denoisers.ExecuteTAA(ref _FinalTex, _currentSample, cmd);

            cmd.Blit(_FinalTex, destination);
            ClearOutRenderTexture(_DebugTex);
            cmd.EndSample("Post Processing");
            _currentSample++;
            FramesSinceStart++;
            FramesSinceStart2++;
            PrevCamPosition = _camera.transform.position;
            PrevASVGF = UseASVGF;
            PrevReCur = UseReCur;
            PrevReSTIRGI = UseReSTIRGI;
            PrevCamInvProj = _camera.projectionMatrix.inverse;
            PrevCamToWorld = _camera.cameraToWorldMatrix;
        }

        public void RenderImage(RenderTexture destination, CommandBuffer cmd)
        {
            _camera.renderingPath = RenderingPath.DeferredShading;
            if(RenderScale != 1.0f) {
                _camera.renderingPath = RenderingPath.DeferredShading;
                // return;
            }
            if (SceneIsRunning && Assets != null && Assets.RenderQue.Count > 0)
            {
                ResetAllTextures();
                RunUpdate();
                RebuildMeshObjectBuffers(cmd);
                InitRenderTexture();
                SetShaderParameters(cmd);
                Render(destination, cmd);
                uFirstFrame = 0;
            }
            else
            {
                try { int throwawayBool = Assets.UpdateTLAS(cmd); } catch (System.IndexOutOfRangeException) { }
            }
            SceneIsRunning = true;
        }
    }
}
