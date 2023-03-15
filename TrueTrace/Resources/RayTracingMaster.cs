// #define DoLightMapping
// #define HardwareRT
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace TrueTrace {
    public class RayTracingMaster : MonoBehaviour
    {
        private ComputeShader RayTracingShader;
        private ComputeShader IntersectionShader;
        private ComputeShader GenerateShader;
        private ComputeShader AtmosphereGeneratorShader;
        public Texture SkyboxTexture;
        // private Texture2D LightMapTex;
        // private Object LightMapDat;
        private Camera _camera;
        private float _lastFieldOfView;
        private RenderTexture _target;
        private RenderTexture _converged;
        private RenderTexture _Albedo;
        private RenderTexture _NormTex;
        private RenderTexture _IntermediateTex;
        private RenderTexture _DebugTex;
        private RenderTexture _FinalTex;
        private RenderTexture PrevDistanceTex;
        private RenderTexture CorrectedDistanceTex;
        private RenderTexture _RandomNums;

        private Denoiser Denoisers;
        private AtmosphereGenerator Atmo;
        [HideInInspector] public AssetManager Assets;

        private Material _addMaterial;
        private Material _FireFlyMaterial;
        private int _currentSample = 0;
        [HideInInspector] public List<Transform> _transformsToWatch = new List<Transform>();
        private static bool _meshObjectsNeedRebuilding = false;
        public static List<RayTracingLights> _rayTracingLights = new List<RayTracingLights>();
        private ComputeBuffer _MaterialDataBuffer;
        private ComputeBuffer _CompactedMeshData;
        private ComputeBuffer _RayBuffer1;
        private ComputeBuffer _ColorBuffer;
        private ComputeBuffer _ColorBuffer2;
        private ComputeBuffer _ColorBuffer3;
        private ComputeBuffer _BufferSizes;
        private ComputeBuffer _ShadowBuffer;
        private ComputeBuffer _LightTriangles;
        private ComputeBuffer _UnityLights;
        private ComputeBuffer _LightMeshes;
        private ComputeBuffer _BrickmapBuffer;
        private ComputeBuffer _VoxelTLAS;
        private ComputeBuffer RaysBuffer;
        private ComputeBuffer GIReservoirCurrent;
        private ComputeBuffer GIReservoirPrevious;
        private ComputeBuffer MatModifierBuffer;
        private ComputeBuffer MatModifierBufferPrev;
        private ComputeBuffer CurBounceInfoBuffer;
        #if HardwareRT
            private ComputeBuffer MeshIndexOffsets;
            private ComputeBuffer SubMeshOffsetsBuffer;
        #endif
        // private ComputeBuffer LightMapTriBuffer;

        private ASVGF ASVGFCode;

        private int FramesSinceStart2;
        private BufferSizeData[] BufferSizes;
        [SerializeField]
        public int SampleCount;

        private int uFirstFrame = 1;
        public float IndirectBoost = 1;
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
        [HideInInspector] public int ReSTIRGIUpdateRate = 0;
        [HideInInspector] public bool UseASVGF = false;
        [HideInInspector] public bool UseTAAU = true;
        [HideInInspector] public bool UseReSTIRGITemporal = true;
        [HideInInspector] public bool UseReSTIRGISpatial = true;
        [HideInInspector] public bool UseReSTIRGI = false;
        [HideInInspector] public bool ReSTIRGIPermutedSamples = false;
        [HideInInspector] public int ReSTIRGISpatialCount = 5;
        [HideInInspector] public int ReSTIRGITemporalMCap = 0;
        [HideInInspector] public bool DoReSTIRGIConnectionValidation = true;
        [HideInInspector] public bool ReSTIRGISpatialStabalizer = false;
        [HideInInspector] public float Exposure = 1;
        [HideInInspector] public bool PrevReSTIRGI;
        [HideInInspector] public bool DoWRS = false;
        [HideInInspector] public bool DoCheckerboarding = false;
        [HideInInspector] public bool DoFirefly = false;
        [HideInInspector] public bool DoIndirectClamping = true;
        [HideInInspector] public int RISCount = 5;

        [HideInInspector] public bool DoVoxels = false;

        [HideInInspector] public int SVGFAtrousKernelSizes = 6;
        [HideInInspector] public int LightTrianglesCount;
        [HideInInspector] public int AtmoNumLayers = 4;
        [HideInInspector] public bool UseAlteredPipeline = false;
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
        private int FramesSinceStart;
        private int GIReSTIRKernel;
        private int HeightmapKernel;
        private int LightMapGenKernel;
        private int GIReTraceKernel;
        private int TransferKernel;
        private int CorrectedDistanceKernel;
        private Matrix4x4 PrevViewProjection;
        private int TargetWidth;
        private int TargetHeight;
        private int SourceWidth;
        private int SourceHeight;
        private Vector3 PrevCamPosition;
        private bool PrevASVGF;


        [System.Serializable]
        public struct BufferSizeData
        {
            public int tracerays;
            public int rays_retired;
            public int brickmap_rays_retired;
            public int shade_rays;
            public int shadow_rays;
            public int retired_shadow_rays;
            public int retired_brickmap_shadow_rays;
            public int heightmap_rays_retired;
        }
        [HideInInspector] public bool HasStarted = false;


        public void TossCamera(Camera camera) {
            _camera = camera;
            AssetManager.SelectedCamera = camera;
            _transformsToWatch.Clear();
            _transformsToWatch.Add(_camera.transform);
        }
        unsafe public void Start2()
        {

            Application.targetFrameRate = 165;
            ASVGFCode = new ASVGF();
            if (RayTracingShader == null) {RayTracingShader = Resources.Load<ComputeShader>("MainCompute/RayTracingShader"); }
            if (IntersectionShader == null) {IntersectionShader = Resources.Load<ComputeShader>("MainCompute/IntersectionKernels"); }
            if (GenerateShader == null) {GenerateShader = Resources.Load<ComputeShader>("MainCompute/RayGenKernels"); }
            if (AtmosphereGeneratorShader == null) { AtmosphereGeneratorShader = Resources.Load<ComputeShader>("Utility/AtmosphereLUTGenerator"); }
            TargetWidth = Screen.width;
            TargetHeight = Screen.height;
            SourceWidth = (int)Mathf.Ceil((float)TargetWidth * RenderScale);
            SourceHeight = (int)Mathf.Ceil((float)TargetHeight * RenderScale);
            if (Mathf.Abs(SourceWidth - TargetWidth) < 2)
            {
                SourceWidth = TargetWidth;
                SourceHeight = TargetHeight;
                RenderScale = 1;
            }

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
            GenKernel = GenerateShader.FindKernel("Generate");
            GenASVGFKernel = GenerateShader.FindKernel("GenerateASVGF");
            TraceKernel = IntersectionShader.FindKernel("kernel_trace");
            ShadowKernel = IntersectionShader.FindKernel("kernel_shadow");
            ShadeKernel = RayTracingShader.FindKernel("kernel_shade");
            FinalizeKernel = RayTracingShader.FindKernel("kernel_finalize");
            BrickmapKernel = IntersectionShader.FindKernel("kernel_brickmap_trace");
            BrickmapShadowKernel = IntersectionShader.FindKernel("kernel_shadow_brickmap");
            GIReSTIRKernel = RayTracingShader.FindKernel("kernel_GI_Reserviour");
            HeightmapKernel = IntersectionShader.FindKernel("kernel_heightmap");
            LightMapGenKernel = GenerateShader.FindKernel("GenerateLightMaps");
            GIReTraceKernel = RayTracingShader.FindKernel("GIReTraceKernel");
            TransferKernel = RayTracingShader.FindKernel("TransferKernel");
            CorrectedDistanceKernel = RayTracingShader.FindKernel("DepthCopyKernel");

            ASVGFCode.Initialized = false;

            Atmo = new AtmosphereGenerator(AtmosphereGeneratorShader, 6360000.0f / 1000.0f, 6420000.0f / 1000.0f, AtmoNumLayers);
            FramesSinceStart2 = 0;

            Denoisers = new Denoiser(_camera, SourceWidth, SourceHeight);
            HasStarted = true;
            _camera.renderingPath = RenderingPath.DeferredShading;
            _camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;
        }

        private void OnEnable()
        {
            _currentSample = 0;
        }
        public void OnDisable()
        {
            _MaterialDataBuffer?.Release();
            _CompactedMeshData?.Release();
            _RayBuffer1?.Release();
            _ColorBuffer?.Release();
            _ColorBuffer2?.Release();
            _ColorBuffer3?.Release();
            _BufferSizes?.Release();
            _ShadowBuffer?.Release();
            _LightTriangles?.Release();
            _UnityLights?.Release();
            _LightMeshes?.Release();
            _BrickmapBuffer?.Release();
            _VoxelTLAS?.Release();
            _BrickmapBuffer?.Release();
            RaysBuffer?.Release();
            ASVGFCode.ClearAll();
            if (RaysBuffer != null) RaysBuffer.Release();
            if(GIReservoirCurrent != null) GIReservoirCurrent.Release();
            if(GIReservoirPrevious != null) GIReservoirPrevious.Release();
            if(MatModifierBuffer != null) MatModifierBuffer.Release();
            if(MatModifierBufferPrev != null) MatModifierBufferPrev.Release();
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

            RayTracingShader.SetVector("SunDir", SunDirection);
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
            _meshObjectsNeedRebuilding = true;
        }
        public static void UnregisterObject(RayTracingLights obj)
        {//Removes meshes from list
            _rayTracingLights.Remove(obj);
            _meshObjectsNeedRebuilding = true;
        }

        private void CreateDynamicBuffer(ref ComputeBuffer TargetBuffer, int Stride)
        {
            if (TargetBuffer == null) TargetBuffer = new ComputeBuffer(SourceWidth * SourceHeight, Stride);
        }
        public void RebuildMeshObjectBuffers(CommandBuffer cmd)
        {
            UnityEngine.Rendering.GlobalKeyword HDRP_ON = UnityEngine.Rendering.GlobalKeyword.Create("HDRP_ON");
            GenerateShader.EnableKeyword("HDRP_ON");
            RayTracingShader.EnableKeyword("HDRP_ON");
            cmd.BeginSample("Full Update");
            if (uFirstFrame != 1)
            {
                if (DoTLASUpdates)
                {
                    if (Assets.UpdateTLAS(cmd))
                    {
                        CreateComputeBuffer(ref _CompactedMeshData, Assets.MyMeshesCompacted, 168);
                        CreateComputeBuffer(ref _LightTriangles, Assets.AggLightTriangles, 100);
                        CreateComputeBuffer(ref _MaterialDataBuffer, Assets._Materials, 216);
                        CreateComputeBuffer(ref _UnityLights, Assets.UnityLights, 60);
                        CreateComputeBuffer(ref _LightMeshes, Assets.LightMeshes, 96);
                        CreateComputeBuffer(ref _VoxelTLAS, Assets.VoxelTLAS, 80);
                        CreateComputeBuffer(ref _BrickmapBuffer, Assets.GPUBrickmap, 4);
                        // CreateComputeBuffer(ref LightMapTriBuffer, Assets.LightMapTris, 84);
                        #if HardwareRT
                            CreateComputeBuffer(ref MeshIndexOffsets, Assets.MeshOffsets, 8);
                            CreateComputeBuffer(ref SubMeshOffsetsBuffer, Assets.SubMeshOffsets, 4);
                        #endif
                        uFirstFrame = 1;
                    }
                    else
                    {
                        cmd.BeginSample("Update Materials");
                        cmd.SetBufferData(_CompactedMeshData, Assets.MyMeshesCompacted);
                        if (Assets.LightMeshCount != 0) _LightMeshes.SetData(Assets.LightMeshes);
                        if (Assets.UnityLightCount != 0) _UnityLights.SetData(Assets.UnityLights);
                        Assets.UpdateMaterials();
                        _MaterialDataBuffer.SetData(Assets._Materials);
                        if (Assets.UseVoxels) _VoxelTLAS.SetData(Assets.VoxelTLAS);
                        cmd.EndSample("Update Materials");
                    }
                }
            }
            cmd.EndSample("Full Update");

            if (!_meshObjectsNeedRebuilding) return;

            _meshObjectsNeedRebuilding = false;
            FramesSinceStart = 0;
            // CreateComputeBuffer(ref LightMapTriBuffer, Assets.LightMapTris, 84);
            CreateComputeBuffer(ref _BrickmapBuffer, Assets.GPUBrickmap, 4);
            CreateComputeBuffer(ref _UnityLights, Assets.UnityLights, 60);
            CreateComputeBuffer(ref _VoxelTLAS, Assets.VoxelTLAS, 80);
            CreateComputeBuffer(ref _LightMeshes, Assets.LightMeshes, 96);

            CreateComputeBuffer(ref _MaterialDataBuffer, Assets._Materials, 216);
            CreateComputeBuffer(ref _CompactedMeshData, Assets.MyMeshesCompacted, 168);
            CreateComputeBuffer(ref _LightTriangles, Assets.AggLightTriangles, 100);

            #if HardwareRT
                CreateComputeBuffer(ref MeshIndexOffsets, Assets.MeshOffsets, 8);
                CreateComputeBuffer(ref SubMeshOffsetsBuffer, Assets.SubMeshOffsets, 4);
            #endif

            CreateDynamicBuffer(ref _RayBuffer1, 56);
            if (_ShadowBuffer == null) _ShadowBuffer = new ComputeBuffer(SourceWidth * SourceHeight, 64);
            CreateDynamicBuffer(ref _ColorBuffer, 48);
            CreateDynamicBuffer(ref _ColorBuffer2, 48);
            CreateDynamicBuffer(ref _ColorBuffer3, 48);
            CreateDynamicBuffer(ref MatModifierBuffer, 76);
            CreateDynamicBuffer(ref MatModifierBufferPrev, 76);
            CreateDynamicBuffer(ref GIReservoirCurrent, 60);
            CreateDynamicBuffer(ref GIReservoirPrevious, 60);
            CurBounceInfoBuffer = new ComputeBuffer(8, 12);
            RaysBuffer = new ComputeBuffer(SourceWidth * SourceHeight, 36);
            GenerateShader.SetBuffer(GenASVGFKernel, "Rays", RaysBuffer);
        }


        private void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
            where T : struct
        {
            // Do we already have a compute buffer?
            if (buffer != null)
            {
                // If no data or buffer doesn't match the given criteria, release it
                if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
                {
                    buffer.Release();
                    buffer = null;
                }
            }

            if (data.Count != 0)
            {
                // If the buffer has been released or wasn't there to
                // begin with, create it
                if (buffer == null)
                {
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
            if (buffer != null)
            {
                // If no data or buffer doesn't match the given criteria, release it
                if (data.Length == 0 || buffer.count != data.Length || buffer.stride != stride)
                {
                    buffer.Release();
                    buffer = null;
                }
            }

            if (data.Length != 0)
            {
                // If the buffer has been released or wasn't there to
                // begin with, create it
                if (buffer == null)
                {
                    buffer = new ComputeBuffer(data.Length, stride);
                }
                // Set data on the buffer
                buffer.SetData(data);
            }
        }
        private void SetMatrix(string Name, Matrix4x4 Mat) {
            RayTracingShader.SetMatrix(Name, Mat);
            IntersectionShader.SetMatrix(Name, Mat);
            GenerateShader.SetMatrix(Name, Mat);
        }

        private void SetVector(string Name, Vector3 Mat) {
            RayTracingShader.SetVector(Name, Mat);
            IntersectionShader.SetVector(Name, Mat);
            GenerateShader.SetVector(Name, Mat);
        }

        private void SetInt(string Name, int Mat, CommandBuffer cmd) {
            cmd.SetComputeIntParam(RayTracingShader, Name, Mat);
            cmd.SetComputeIntParam(IntersectionShader, Name, Mat);
            cmd.SetComputeIntParam(GenerateShader, Name, Mat);
        }

        private void SetFloat(string Name, float Mat) {
            RayTracingShader.SetFloat(Name, Mat);
            IntersectionShader.SetFloat(Name, Mat);
            GenerateShader.SetFloat(Name, Mat);
        }

        private void SetBool(string Name, bool Mat) {
            RayTracingShader.SetBool(Name, Mat);
            IntersectionShader.SetBool(Name, Mat);
            GenerateShader.SetBool(Name, Mat);
        }

        Matrix4x4 prevView;
        private void SetShaderParameters(CommandBuffer cmd)
        {
            if (UseASVGF && !ASVGFCode.Initialized) ASVGFCode.init(SourceWidth, SourceHeight, _camera);
            else if (!UseASVGF && ASVGFCode.Initialized) ASVGFCode.ClearAll();
            if (UseSVGF && !Denoisers.SVGFInitialized && !UseASVGF) Denoisers.InitSVGF();
            else if ((UseASVGF || !UseSVGF) && Denoisers.SVGFInitialized) Denoisers.ClearSVGF();

            Matrix4x4 viewprojmatrix = _camera.projectionMatrix * _camera.worldToCameraMatrix;
            var PrevMatrix = PrevViewProjection;
            BufferSizes = new BufferSizeData[bouncecount + 1];
            BufferSizes[0].tracerays = 0;
            if(_BufferSizes == null) {
                _BufferSizes = new ComputeBuffer(bouncecount + 1, 32);
            }
            if(_BufferSizes.count != bouncecount + 1) {
                _BufferSizes.Release();
                _BufferSizes = new ComputeBuffer(bouncecount + 1, 32);
            }
            _BufferSizes.SetData(BufferSizes);
            GenerateShader.SetComputeBuffer(GenKernel, "BufferSizes", _BufferSizes);
            GenerateShader.SetComputeBuffer(GenASVGFKernel, "BufferSizes", _BufferSizes);
            IntersectionShader.SetComputeBuffer(TraceKernel, "BufferSizes", _BufferSizes);
            IntersectionShader.SetComputeBuffer(ShadowKernel, "BufferSizes", _BufferSizes);
            RayTracingShader.SetComputeBuffer(ShadeKernel, "BufferSizes", _BufferSizes);
            RayTracingShader.SetComputeBuffer(TransferKernel, "BufferSizes", _BufferSizes);
            RayTracingShader.SetComputeBuffer(TransferKernel, "BufferData", CurBounceInfoBuffer);
            IntersectionShader.SetComputeBuffer(BrickmapKernel, "BufferSizes", _BufferSizes);
            IntersectionShader.SetComputeBuffer(BrickmapShadowKernel, "BufferSizes", _BufferSizes);
            IntersectionShader.SetComputeBuffer(HeightmapKernel, "BufferSizes", _BufferSizes);

            SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
            SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
            SetMatrix("_CameraProjection", _camera.projectionMatrix);
            SetMatrix("viewprojection", viewprojmatrix);
            SetMatrix("prevviewprojection", PrevMatrix);
            SetMatrix("ViewMatrix", _camera.worldToCameraMatrix);
            SetMatrix("InverseViewMatrix", _camera.worldToCameraMatrix.inverse);
            SetMatrix("inverseview", _camera.worldToCameraMatrix.inverse);

            SetVector("CamDir", _camera.transform.forward);
            SetVector("Up", _camera.transform.up);
            SetVector("Right", _camera.transform.right);
            SetVector("Forward", _camera.transform.forward);
            SetVector("camPos", _camera.transform.position);
            SetVector("CamPos", _camera.transform.position);
            SetVector("PrevCamPos", PrevCamPosition);

            SetFloat("FarPlane", _camera.farClipPlane);
            SetFloat("focal_distance", DoFFocal);
            SetFloat("AperatureRadius", DoFAperature);
            SetFloat("MinSpatialSize", MinSpatialSize);
            SetFloat("sun_angular_radius", 0.1f);
            SetFloat("IndirectBoost", IndirectBoost);

            SetInt("MaxBounce", bouncecount - 1, cmd);
            SetInt("frames_accumulated", _currentSample, cmd);
            SetInt("VoxelOffset", Assets.VoxOffset, cmd);
            SetInt("ReSTIRGISpatialCount", ReSTIRGISpatialCount, cmd);
            SetInt("ReSTIRGITemporalMCap", ReSTIRGITemporalMCap, cmd);
            SetInt("curframe", FramesSinceStart2, cmd);
            SetInt("TerrainCount", Assets.Terrains.Count, cmd);
            SetInt("ReSTIRGIUpdateRate", UseReSTIRGI ? ReSTIRGIUpdateRate : 0, cmd);
            SetInt("TargetWidth", TargetWidth, cmd);
            SetInt("TargetHeight", TargetHeight, cmd);
            SetInt("RISCount", RISCount, cmd);


            SetBool("UseRussianRoulette", UseRussianRoulette);
            SetBool("UseNEE", UseNEE);
            SetBool("DoVoxels", Assets.UseVoxels);
            SetBool("UseDoF", AllowDoF);
            SetBool("UseReSTIRGI", UseReSTIRGI);
            SetBool("UseReSTIRGITemporal", UseReSTIRGITemporal);
            SetBool("UseReSTIRGISpatial", UseReSTIRGISpatial);
            SetBool("DoReSTIRGIConnectionValidation", DoReSTIRGIConnectionValidation);
            SetBool("UseASVGF", UseASVGF);
            SetBool("AbandonSamples", UseASVGF != PrevASVGF || UseReSTIRGI != PrevReSTIRGI);
            SetBool("SpatialStabalizer", ReSTIRGISpatialStabalizer);
            SetBool("UsePermutatedSamples", ReSTIRGIPermutedSamples);
            SetBool("TerrainExists", Assets.Terrains.Count != 0);
            SetBool("DoWRS", DoWRS);
            SetBool("UseAlteredPipeline", UseAlteredPipeline);
            SetBool("DoCheckerboarding", DoCheckerboarding);
            SetBool("ChangedExposure", AllowAutoExpose);
            SetBool("DoHeightmap", Assets.DoHeightmap);
            SetBool("DoIndirectClamping", DoIndirectClamping);
            if(AllowAutoExpose) {
                float[] A = new float[1];
                Denoisers.A.GetData(A);
                RayTracingShader.SetFloat("A", A[0]);
            } else {
                RayTracingShader.SetFloat("A", 1);                
            }

            IntersectionShader.SetComputeBuffer(BrickmapShadowKernel, "CurrentReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirCurrent : GIReservoirPrevious));
            IntersectionShader.SetComputeBuffer(ShadowKernel, "CurrentReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirCurrent : GIReservoirPrevious));
            RayTracingShader.SetComputeBuffer(GIReSTIRKernel, "CurrentReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirCurrent : GIReservoirPrevious));
            RayTracingShader.SetComputeBuffer(GIReTraceKernel, "CurrentReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirPrevious : GIReservoirCurrent));
            RayTracingShader.SetComputeBuffer(GIReSTIRKernel, "PreviousReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirPrevious : GIReservoirCurrent));
            RayTracingShader.SetComputeBuffer(ShadeKernel, "CurrentReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirCurrent : GIReservoirPrevious));
            RayTracingShader.SetComputeBuffer(ShadeKernel, "PreviousReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirPrevious : GIReservoirCurrent));
            RayTracingShader.SetComputeBuffer(FinalizeKernel, "CurrentReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirCurrent : GIReservoirPrevious));
            RayTracingShader.SetComputeBuffer(FinalizeKernel, "PreviousReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirPrevious : GIReservoirCurrent));
            GenerateShader.SetComputeBuffer(GenKernel, "CurrentReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirCurrent : GIReservoirPrevious));
            GenerateShader.SetComputeBuffer(GenKernel, "PreviousReservoirGI", ((FramesSinceStart2 % 2 == 0) ? GIReservoirPrevious : GIReservoirCurrent));
            
            RayTracingShader.SetTextureFromGlobal(FinalizeKernel, "Depth", "_CameraDepthTexture");
            RayTracingShader.SetTextureFromGlobal(CorrectedDistanceKernel, "Depth", "_CameraDepthTexture");
            RayTracingShader.SetTextureFromGlobal(FinalizeKernel, "NormalTex", "_CameraGBufferTexture2");
            RayTracingShader.SetTextureFromGlobal(GIReTraceKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            RayTracingShader.SetTextureFromGlobal(GIReSTIRKernel, "Depth", "_CameraDepthTexture");
            RayTracingShader.SetTextureFromGlobal(GIReSTIRKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            RayTracingShader.SetTextureFromGlobal(GIReSTIRKernel, "NormalTex", "_CameraGBufferTexture2");
            RayTracingShader.SetTextureFromGlobal(ShadeKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            GenerateShader.SetTextureFromGlobal(GenKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            GenerateShader.SetTextureFromGlobal(GenKernel, "Depth", "_CameraDepthTexture");
            GenerateShader.SetTextureFromGlobal(GenASVGFKernel, "MotionVectors", "_CameraMotionVectorsTexture");

            RayTracingShader.SetComputeBuffer(ShadeKernel, "ScreenSpaceInfo", ((FramesSinceStart2 % 2 == 0) ? MatModifierBuffer : MatModifierBufferPrev));
            RayTracingShader.SetComputeBuffer(FinalizeKernel, "ScreenSpaceInfo", ((FramesSinceStart2 % 2 == 0) ? MatModifierBuffer : MatModifierBufferPrev));
            RayTracingShader.SetComputeBuffer(FinalizeKernel, "MatModifiersPrev", ((FramesSinceStart2 % 2 == 0) ? MatModifierBufferPrev : MatModifierBuffer));
            RayTracingShader.SetComputeBuffer(GIReSTIRKernel, "MatModifiersPrev", ((FramesSinceStart2 % 2 == 0) ? MatModifierBufferPrev : MatModifierBuffer));
            RayTracingShader.SetComputeBuffer(GIReSTIRKernel, "ScreenSpaceInfo", ((FramesSinceStart2 % 2 == 0) ? MatModifierBuffer : MatModifierBufferPrev));
            IntersectionShader.SetComputeBuffer(BrickmapShadowKernel, "ScreenSpaceInfo", ((FramesSinceStart2 % 2 == 0) ? MatModifierBuffer : MatModifierBufferPrev));
            PrevViewProjection = viewprojmatrix;
            IntersectionShader.SetComputeBuffer(HeightmapKernel, "Terrains", Assets.TerrainBuffer);
            RayTracingShader.SetComputeBuffer(ShadeKernel, "Terrains", Assets.TerrainBuffer);
            IntersectionShader.SetComputeBuffer(BrickmapShadowKernel, "Terrains", Assets.TerrainBuffer);
            var Temp = prevView;
            RayTracingShader.SetMatrix("prevviewmatrix", Temp);
            prevView = _camera.worldToCameraMatrix;
                RayTracingShader.SetComputeBuffer(GIReSTIRKernel, "GlobalColors2", (FramesSinceStart2 % 2 == 0) ? _ColorBuffer2 : _ColorBuffer3);
                RayTracingShader.SetComputeBuffer(GIReSTIRKernel, "GlobalColors3", (FramesSinceStart2 % 2 == 0) ? _ColorBuffer3 : _ColorBuffer2);
            #if DoLightMapping
                SetBool("DiffRes", true);
                GenerateShader.SetBuffer(LightMapGenKernel, "LightMapTris", LightMapTriBuffer);
            #else
                SetBool("DiffRes", RenderScale != 1.0f);
            #endif
            #if HardwareRT
                IntersectionShader.SetRayTracingAccelerationStructure(TraceKernel, "myAccelerationStructure", Assets.AccelStruct);
                IntersectionShader.SetRayTracingAccelerationStructure(2, "myAccelerationStructure", Assets.AccelStruct);
            #endif
            if (true)
            {
                if (SkyboxTexture != null)
                {
                    RayTracingShader.SetTexture(ShadeKernel, "_SkyboxTexture", SkyboxTexture);
                }
                IntersectionShader.SetComputeBuffer(HeightmapKernel, "GlobalRays1", _RayBuffer1);
                IntersectionShader.SetTexture(HeightmapKernel, "Heightmap", Assets.HeightMap);
                RayTracingShader.SetTexture(ShadeKernel, "Heightmap", Assets.HeightMap);
                IntersectionShader.SetTexture(BrickmapShadowKernel, "Heightmap", Assets.HeightMap);
                RayTracingShader.SetTexture(ShadeKernel, "TerrainAlphaMap", Assets.AlphaMap);
                SetInt("MaterialCount", Assets.MatCount, cmd);

                RayTracingShader.SetTexture(CorrectedDistanceKernel, "CorrectedDepthTex", CorrectedDistanceTex);

                SetInt("LightMeshCount", Assets.LightMeshCount, cmd);
                SetInt("unitylightcount", Assets.UnityLightCount, cmd);
                SetInt("lighttricount", Assets.LightTriCount, cmd);
                SetInt("screen_width", SourceWidth, cmd);
                SetInt("screen_height", SourceHeight, cmd);

                if (_RandomNums == null) CreateRenderTexture(ref _RandomNums, false, false);

                GenerateShader.SetTexture(GenASVGFKernel, "RandomNums", _RandomNums);
                GenerateShader.SetComputeBuffer(GenASVGFKernel, "GlobalRays1", _RayBuffer1);
                GenerateShader.SetComputeBuffer(GenASVGFKernel, "GlobalColors", _ColorBuffer);
                GenerateShader.SetComputeBuffer(GenASVGFKernel, "BufferSizes", _BufferSizes);

                IntersectionShader.SetComputeBuffer(ShadowKernel, "GlobalColors", _ColorBuffer);

                GenerateShader.SetComputeBuffer(3, "GlobalColors", _ColorBuffer);

                GenerateShader.SetTexture(GenKernel, "RandomNums", _RandomNums);
                GenerateShader.SetComputeBuffer(GenKernel, "GlobalRays1", _RayBuffer1);
                GenerateShader.SetComputeBuffer(GenKernel, "GlobalColors", _ColorBuffer);
                GenerateShader.SetComputeBuffer(GenKernel, "BufferSizes", _BufferSizes);

                GenerateShader.SetComputeBuffer(LightMapGenKernel, "GlobalRays1", _RayBuffer1);
                GenerateShader.SetComputeBuffer(LightMapGenKernel, "GlobalColors", _ColorBuffer);
                GenerateShader.SetComputeBuffer(LightMapGenKernel, "BufferSizes", _BufferSizes);

                IntersectionShader.SetComputeBuffer(TraceKernel, "GlobalRays1", _RayBuffer1);
                IntersectionShader.SetComputeBuffer(TraceKernel, "AggTris", Assets.AggTriBuffer);
                IntersectionShader.SetComputeBuffer(TraceKernel, "cwbvh_nodes", Assets.BVH8AggregatedBuffer);
                IntersectionShader.SetComputeBuffer(TraceKernel, "_MeshData", _CompactedMeshData);
                IntersectionShader.SetComputeBuffer(TraceKernel, "_Materials", _MaterialDataBuffer);
                IntersectionShader.SetTexture(TraceKernel, "AlphaAtlas", Assets.AlphaAtlas);

                IntersectionShader.SetComputeBuffer(ShadowKernel, "_MeshData", _CompactedMeshData);
                IntersectionShader.SetComputeBuffer(ShadowKernel, "cwbvh_nodes", Assets.BVH8AggregatedBuffer);
                IntersectionShader.SetComputeBuffer(ShadowKernel, "AggTris", Assets.AggTriBuffer);
                IntersectionShader.SetComputeBuffer(ShadowKernel, "ShadowRaysBuffer", _ShadowBuffer);
                IntersectionShader.SetComputeBuffer(ShadowKernel, "_Materials", _MaterialDataBuffer);
                IntersectionShader.SetTexture(ShadowKernel, "AlphaAtlas", Assets.AlphaAtlas);

                IntersectionShader.SetComputeBuffer(BrickmapKernel, "BrickMap", _BrickmapBuffer);
                IntersectionShader.SetComputeBuffer(BrickmapKernel, "VoxelTLAS", _VoxelTLAS);
                IntersectionShader.SetComputeBuffer(BrickmapKernel, "_MeshData", _CompactedMeshData);

                IntersectionShader.SetComputeBuffer(BrickmapShadowKernel, "BrickMap", _BrickmapBuffer);
                IntersectionShader.SetComputeBuffer(BrickmapShadowKernel, "VoxelTLAS", _VoxelTLAS);
                IntersectionShader.SetComputeBuffer(BrickmapShadowKernel, "ShadowRaysBuffer", _ShadowBuffer);
                IntersectionShader.SetComputeBuffer(BrickmapShadowKernel, "GlobalColors", _ColorBuffer);
                IntersectionShader.SetComputeBuffer(BrickmapShadowKernel, "_MeshData", _CompactedMeshData);
                IntersectionShader.SetBuffer(BrickmapShadowKernel, "Rays", RaysBuffer);
                RayTracingShader.SetBuffer(GIReTraceKernel, "Rays", RaysBuffer);

                #if HardwareRT
                    RayTracingShader.SetBuffer(ShadeKernel, "MeshOffsets", MeshIndexOffsets);
                    RayTracingShader.SetBuffer(ShadeKernel, "SubMeshOffsets", SubMeshOffsetsBuffer);
                #endif
                RayTracingShader.SetTexture(ShadeKernel, "MetallicTex", Assets.MetallicAtlas);
                RayTracingShader.SetTexture(ShadeKernel, "RoughnessTex", Assets.RoughnessAtlas);
                RayTracingShader.SetTexture(ShadeKernel, "RandomNums", _RandomNums);
                RayTracingShader.SetTexture(GIReTraceKernel, "RandomNumsWrite", _RandomNums);
                RayTracingShader.SetTexture(GIReTraceKernel, "RandomNums", _RandomNums);
                RayTracingShader.SetTexture(ShadeKernel, "_TextureAtlas", Assets.AlbedoAtlas);
                RayTracingShader.SetTexture(ShadeKernel, "_EmissiveAtlas", Assets.EmissiveAtlas);
                RayTracingShader.SetTexture(ShadeKernel, "_NormalAtlas", Assets.NormalAtlas);
                RayTracingShader.SetTexture(ShadeKernel, "AlphaAtlas", Assets.AlphaAtlas);
                RayTracingShader.SetTexture(ShadeKernel, "scattering_texture", Atmo.MultiScatterTex);
                RayTracingShader.SetTexture(ShadeKernel, "TransmittanceTex", Atmo._TransmittanceLUT);
                RayTracingShader.SetTexture(ShadeKernel, "ScatterTex", Atmo._RayleighTex);
                RayTracingShader.SetTexture(ShadeKernel, "MieTex", Atmo._MieTex);
                RayTracingShader.SetTexture(ShadeKernel, "TempAlbedoTex", _Albedo);
                RayTracingShader.SetTexture(ShadeKernel, "TempNormTex", _NormTex);
                RayTracingShader.SetTexture(ShadeKernel, "VideoTex", Assets.VideoTexture);
                RayTracingShader.SetTexture(GIReSTIRKernel, "VideoTex", Assets.VideoTexture);
                IntersectionShader.SetTexture(0, "VideoTex", Assets.VideoTexture);
                IntersectionShader.SetTexture(ShadowKernel, "VideoTex", Assets.VideoTexture);
                RayTracingShader.SetComputeBuffer(ShadeKernel, "_LightMeshes", _LightMeshes);
                RayTracingShader.SetComputeBuffer(ShadeKernel, "_Materials", _MaterialDataBuffer);
                RayTracingShader.SetComputeBuffer(ShadeKernel, "GlobalRays1", _RayBuffer1);
                RayTracingShader.SetComputeBuffer(ShadeKernel, "LightTriangles", _LightTriangles);
                RayTracingShader.SetComputeBuffer(ShadeKernel, "ShadowRaysBuffer", _ShadowBuffer);
                RayTracingShader.SetComputeBuffer(ShadeKernel, "cwbvh_nodes", Assets.BVH8AggregatedBuffer);
                RayTracingShader.SetComputeBuffer(ShadeKernel, "AggTris", Assets.AggTriBuffer);
                RayTracingShader.SetComputeBuffer(ShadeKernel, "GlobalColors", _ColorBuffer);
                RayTracingShader.SetComputeBuffer(ShadeKernel, "_MeshData", _CompactedMeshData);
                RayTracingShader.SetComputeBuffer(ShadeKernel, "_UnityLights", _UnityLights);
                RayTracingShader.SetBuffer(ShadeKernel, "Rays", RaysBuffer);

                RayTracingShader.SetTexture(FinalizeKernel, "RandomNums", _RandomNums);
                RayTracingShader.SetTexture(FinalizeKernel, "PrevDepthTex", PrevDistanceTex);
                RayTracingShader.SetTexture(FinalizeKernel, "Result", _target);
                RayTracingShader.SetTexture(FinalizeKernel, "AlbedoTex", _Albedo);
                RayTracingShader.SetBuffer(FinalizeKernel, "GlobalColors", _ColorBuffer);




                RayTracingShader.SetTexture(GIReSTIRKernel, "TempAlbedoTex", _Albedo);
                RayTracingShader.SetTexture(GIReSTIRKernel, "AlphaAtlas", Assets.AlphaAtlas);
                RayTracingShader.SetTexture(GIReSTIRKernel, "RandomNums", _RandomNums);
                RayTracingShader.SetTexture(GIReSTIRKernel, "TempNormTex", _NormTex);
                RayTracingShader.SetTexture(GIReSTIRKernel, "PrevDepthTex", PrevDistanceTex);
                RayTracingShader.SetTexture(GIReSTIRKernel, "AlbedoTexRead", _Albedo);
                RayTracingShader.SetComputeBuffer(GIReSTIRKernel, "_Materials", _MaterialDataBuffer);
                RayTracingShader.SetComputeBuffer(GIReSTIRKernel, "GlobalColors", _ColorBuffer);
                RayTracingShader.SetComputeBuffer(GIReSTIRKernel, "AggTris", Assets.AggTriBuffer);
                RayTracingShader.SetComputeBuffer(GIReSTIRKernel, "cwbvh_nodes", Assets.BVH8AggregatedBuffer);
                RayTracingShader.SetComputeBuffer(GIReSTIRKernel, "_MeshData", _CompactedMeshData);
                RayTracingShader.SetTexture(GIReSTIRKernel, "RandomNums", _RandomNums);

                GenerateShader.SetTexture(LightMapGenKernel, "RandomNums", _RandomNums);

                GenerateShader.SetTexture(GenKernel, "_DebugTex", _DebugTex);
                GenerateShader.SetTexture(LightMapGenKernel, "_DebugTex", _DebugTex);
                RayTracingShader.SetTexture(ShadeKernel, "_DebugTex", _DebugTex);
                IntersectionShader.SetTexture(BrickmapKernel, "_DebugTex", _DebugTex);
                IntersectionShader.SetTexture(TraceKernel, "_DebugTex", _DebugTex);
                RayTracingShader.SetTexture(FinalizeKernel, "_DebugTex", _DebugTex);
                RayTracingShader.SetTexture(GIReSTIRKernel, "_DebugTex", _DebugTex);
                IntersectionShader.SetTexture(HeightmapKernel, "_DebugTex", _DebugTex);
            }

        }

        private void CreateRenderTexture(ref RenderTexture ThisTex, bool SRGB, bool Res)
        {
            if (SRGB)
            {
                ThisTex = new RenderTexture((Res) ? TargetWidth : SourceWidth, (Res) ? TargetHeight : SourceHeight, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            }
            else
            {
                ThisTex = new RenderTexture((Res) ? TargetWidth : SourceWidth, (Res) ? TargetHeight : SourceHeight, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            }
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }

        private void CreateRenderTextureMask(ref RenderTexture ThisTex, bool Res)
        {
            ThisTex = new RenderTexture((Res) ? TargetWidth : SourceWidth, (Res) ? TargetHeight : SourceHeight, 0,
                RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }

        private void CreateRenderTexture(ref RenderTexture ThisTex, bool SRGB, bool istarget, bool Res)
        {
            if (SRGB)
            {
                ThisTex = new RenderTexture((Res) ? TargetWidth : SourceWidth, (Res) ? TargetHeight : SourceHeight, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            }
            else
            {
                ThisTex = new RenderTexture((Res) ? TargetWidth : SourceWidth, (Res) ? TargetHeight : SourceHeight, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            }
            if (istarget)
            {
                ThisTex.useMipMap = true;
                ThisTex.autoGenerateMips = false;
            }
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }
        private void CreateRenderTextureSingle(ref RenderTexture ThisTex, bool Res)
        {
            ThisTex = new RenderTexture((Res) ? TargetWidth : SourceWidth, (Res) ? TargetHeight : SourceHeight, 0,
                RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }
        private void CreateRenderTextureDouble(ref RenderTexture ThisTex, bool Res)
        {
            ThisTex = new RenderTexture((Res) ? TargetWidth : SourceWidth, (Res) ? TargetHeight : SourceHeight, 0,
                RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }

        private void InitRenderTexture()
        {
            if (_target == null || _target.width != SourceWidth || _target.height != SourceHeight)
            {
                // Release render texture if we already have one
                if (_target != null)
                {
                    _target.Release();
                    _converged.Release();
                    _Albedo.Release();
                    _NormTex.Release();
                    _IntermediateTex.Release();
                    _DebugTex.Release();
                    PrevDistanceTex.Release();
                    _FinalTex.Release();
                    CorrectedDistanceTex.Release();
                }

                CreateRenderTexture(ref _DebugTex, true, false);
                CreateRenderTexture(ref _FinalTex, true, true);
                CreateRenderTexture(ref _IntermediateTex, true, true, true);
                CreateRenderTexture(ref _target, true, false);
                CreateRenderTextureMask(ref _NormTex, false);
                CreateRenderTexture(ref _converged, true, false);
                CreateRenderTexture(ref _Albedo, true, false);
                CreateRenderTextureSingle(ref PrevDistanceTex, false);
                CreateRenderTextureSingle(ref CorrectedDistanceTex, false);
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
        #if DoLightMapping
            public Texture2D TempTex;
            public Texture2D DirMap;
        #endif

        private void Render(RenderTexture destination, CommandBuffer cmd)
        {
            float CurrentSample;
            cmd.BeginSample("Depth Correct And Copy");
            cmd.DispatchCompute(RayTracingShader, CorrectedDistanceKernel, Mathf.CeilToInt(SourceWidth / 32.0f), Mathf.CeilToInt(SourceHeight / 32.0f), 1);
            cmd.EndSample("Depth Correct And Copy");
            
            if (UseASVGF)
            {
                cmd.BeginSample("ASVGF Reproject Pass");
                ASVGFCode.shader.SetBool("ReSTIRGI", UseReSTIRGI);
                ASVGFCode.DoRNG(ref _RandomNums, FramesSinceStart2, ref RaysBuffer, ref PrevDistanceTex, cmd, ref CorrectedDistanceTex);
                GenerateShader.SetBuffer(GenASVGFKernel, "Rays", RaysBuffer);
                cmd.EndSample("ASVGF Reproject Pass");
            }

            if(UseReSTIRGI && ReSTIRGIUpdateRate != 0) {
                cmd.BeginSample("ReSTIR GI Reproject");
                cmd.DispatchCompute(RayTracingShader, GIReTraceKernel, Mathf.CeilToInt(SourceWidth / 12.0f), Mathf.CeilToInt(SourceHeight / 12.0f), 1);
                cmd.EndSample("ReSTIR GI Reproject");
            }

            cmd.BeginSample("First Bounce Gen");
            SetInt("CurBounce", 0, cmd);
            #if DoLightMapping
                cmd.DispatchCompute(GenerateShader, 3, threadGroupsX, threadGroupsY, 1);
                cmd.DispatchCompute(GenerateShader, LightMapGenKernel, Assets.LightMapTris.Count, 1, 1);
            #else
                cmd.DispatchCompute(GenerateShader, (UseASVGF || (UseReSTIRGI && ReSTIRGIUpdateRate != 0)) ? GenASVGFKernel : GenKernel, threadGroupsX, threadGroupsY, 1);
            #endif
            cmd.EndSample("First Bounce Gen");

            cmd.BeginSample("First Bounce Trace");
            cmd.DispatchCompute(IntersectionShader, TraceKernel, 768, 1, 1);
            cmd.EndSample("First Bounce Trace");

            cmd.BeginSample("First Trace For Brickmap");
            if (Assets.UseVoxels) cmd.DispatchCompute(IntersectionShader, BrickmapKernel, 768, 1, 1);
            cmd.EndSample("First Trace For Brickmap");

            cmd.BeginSample("Rest Of Trace");

            for (int i = 0; i < bouncecount; i++)
            {
                cmd.BeginSample("TraceKernel For Bounce: " + i);
                var bouncebounce = i;
                SetInt("CurBounce", bouncebounce, cmd);
                cmd.DispatchCompute(RayTracingShader, TransferKernel, 1, 1, 1);
                if (i != 0) cmd.DispatchCompute(IntersectionShader, TraceKernel, 768, 1, 1);
                cmd.EndSample("TraceKernel For Bounce: " + i);
                cmd.BeginSample("TraceKernel For Brickmap: " + i);
                if (Assets.UseVoxels && i != 0) cmd.DispatchCompute(IntersectionShader, BrickmapKernel, 768, 1, 1);
                cmd.EndSample("TraceKernel For Brickmap: " + i);

                if (Assets.Terrains.Count != 0) cmd.DispatchCompute(IntersectionShader, HeightmapKernel, 768, 1, 1);

                cmd.BeginSample("Shade Kernel: " + i);
                cmd.DispatchCompute(RayTracingShader, ShadeKernel, CurBounceInfoBuffer, 0);
                cmd.EndSample("Shade Kernel: " + i);
                cmd.BeginSample("Shadow Kernel: " + i);
                if (UseNEE) cmd.DispatchCompute(IntersectionShader, ShadowKernel, 768, 1, 1);
                cmd.EndSample("Shadow Kernel: " + i);
                cmd.BeginSample("Brickmap Shadow Kernel: " + i);
                if (UseNEE && (Assets.UseVoxels || Assets.DoHeightmap)) cmd.DispatchCompute(IntersectionShader, BrickmapShadowKernel, 768, 1, 1);
                cmd.EndSample("Brickmap Shadow Kernel: " + i);
            }
            cmd.EndSample("Rest Of Trace");


            cmd.BeginSample("ReSTIRGI Pass");
            SetInt("CurBounce", 0, cmd);
            if (UseReSTIRGI) cmd.DispatchCompute(RayTracingShader, GIReSTIRKernel, Mathf.CeilToInt(SourceWidth / 12.0f), Mathf.CeilToInt(SourceHeight / 12.0f), 1);
            cmd.EndSample("ReSTIRGI Pass");


            if (!UseSVGF && !UseASVGF)
            {
                cmd.BeginSample("Finalize");
                cmd.DispatchCompute(RayTracingShader, FinalizeKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                CurrentSample = 1.0f / (FramesSinceStart + 1.0f);
                SampleCount++;
                cmd.EndSample("Finalize");
            }
            else if (!UseASVGF)
            {
                SampleCount = 0;

                Denoisers.ExecuteSVGF(_currentSample, SVGFAtrousKernelSizes, ref _ColorBuffer, ref _target, ref _Albedo, ref _NormTex, RenderScale != 1.0f, ref PrevDistanceTex, cmd, ref CorrectedDistanceTex);
                CurrentSample = 1;
            }
            else
            {
                SampleCount = 0;
                ASVGFCode.Do(ref _ColorBuffer, ref _NormTex, ref _Albedo, ref _target, ref _RandomNums, RenderScale != 1.0f, ref PrevDistanceTex, ((FramesSinceStart2 % 2 == 0) ? MatModifierBuffer : MatModifierBufferPrev), cmd, ref CorrectedDistanceTex, ref RaysBuffer);
                CurrentSample = 1;
            }
            cmd.BeginSample("Final Blit");
            if (_FireFlyMaterial == null)
                _FireFlyMaterial = new Material(Shader.Find("Hidden/FireFlyPass"));
            if (_addMaterial == null)
                _addMaterial = new Material(Shader.Find("Hidden/Accumulate"));
            _addMaterial.SetFloat("_Sample", CurrentSample);
            cmd.Blit(_target, _converged, _addMaterial);

            if(DoFirefly) cmd.Blit(_converged, _target, _FireFlyMaterial);
            if(DoFirefly) cmd.Blit(_target, _converged);
            cmd.EndSample("Final Blit");

            cmd.BeginSample("Post Processing");
            if (SourceWidth != TargetWidth)
            {
                if (UseTAAU) Denoisers.ExecuteTAAU(ref _FinalTex, ref _converged, ref _Albedo, cmd);
                else Denoisers.ExecuteUpsample(ref _converged, ref _FinalTex, FramesSinceStart2, _currentSample, ref _Albedo, cmd);//This is a postprocessing pass, but im treating it like its not one, need to move it to after the accumulation
                cmd.CopyTexture(_FinalTex, 0, 0, _IntermediateTex, 0, 0);
            }
            else
            {
                cmd.CopyTexture(_converged, _FinalTex);
                if (AllowAutoExpose || AllowBloom || AllowTAA)
                {
                    cmd.CopyTexture(_FinalTex, 0, 0, _IntermediateTex, 0, 0);
                }
            }

            if (AllowAutoExpose)
            {
                _IntermediateTex.GenerateMips();
                Denoisers.ExecuteAutoExpose(ref _FinalTex, ref _IntermediateTex, Exposure, cmd);
                cmd.CopyTexture(_FinalTex, 0, 0, _IntermediateTex, 0, 0);
            }
            if (AllowBloom)
            {
                Denoisers.ExecuteBloom(ref _FinalTex, ref _IntermediateTex, BloomStrength, cmd);
                cmd.CopyTexture(_FinalTex, 0, 0, _IntermediateTex, 0, 0);
            }
            if (AllowTAA)
            {
                Denoisers.ExecuteTAA(ref _FinalTex, ref _IntermediateTex, _currentSample, cmd);
            }

            if (AllowAutoExpose || AllowBloom || AllowTAA) cmd.CopyTexture(_IntermediateTex, 0, 0, _FinalTex, 0, 0);

            if(AllowToneMap) Denoisers.ExecuteToneMap(ref _FinalTex, cmd);

            cmd.Blit(_FinalTex, destination);
            ClearOutRenderTexture(_DebugTex);
            cmd.EndSample("Post Processing");
            _currentSample++;
            FramesSinceStart++;
            FramesSinceStart2++;
            PrevCamPosition = _camera.transform.position;
            PrevASVGF = UseASVGF;
            PrevReSTIRGI = UseReSTIRGI;
            #if DoLightMapping
                if(SampleCount > 1000) {
                    var tempRenderTexture = new RenderTexture(SourceWidth, SourceHeight, 0,
                        RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
                    TempTex = new Texture2D(_FinalTex.width, _FinalTex.height);
                    cmd.ConvertTexture(_FinalTex, TempTex);
                    RenderTexture.active = _FinalTex;
                    _FinalTex.GenerateMips();
                    TempTex.ReadPixels(new Rect(0f,0f,TempTex.width, TempTex.height), 0, 0, true); //true if you use mipmaps
                    TempTex.Apply(false, false); //make it readabole
                    LightmapData[] TempDat = new LightmapData[1];
                    TempDat[0] = new LightmapData();
                    TempDat[0].lightmapColor = TempTex;
                    TempDat[0].lightmapDir = DirMap;

                    LightmapSettings.lightmaps = TempDat;
                    SampleCount = 0;
                }
            #endif
        }

        public void RenderImage(RenderTexture destination, CommandBuffer cmd)
        {
            if (Assets != null && Assets.RenderQue.Count > 0)
            {
                RunUpdate();
                RebuildMeshObjectBuffers(cmd);
                InitRenderTexture();
                SetShaderParameters(cmd);
                Render(destination, cmd);
                uFirstFrame = 0;
            }
            else
            {
                try { bool throwawayBool = Assets.UpdateTLAS(cmd); } catch (System.IndexOutOfRangeException) { }
            }




        }
    }
}
