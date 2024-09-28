using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TrueTrace {
    public class RayTracingMaster : MonoBehaviour
    {
        [HideInInspector] public static Camera _camera;
        
        [HideInInspector] public AtmosphereGenerator Atmo;
        [HideInInspector] public AssetManager Assets;
        private ReSTIRASVGF ReSTIRASVGFCode;
        private TTPostProcessing TTPostProc;
        private ASVGF ASVGFCode;
        private bool Abandon = false;
        #if UseOIDN
            private UnityDenoiserPlugin.DenoiserPluginWrapper OIDNDenoiser;
        #endif
        [HideInInspector] public static bool DoSaving = true;
        public static RayObjs raywrites = new RayObjs();
        public static void WriteString(RayTracingObject OBJtoWrite, string NameIndex) {
            if(DoSaving) {
                if(OBJtoWrite == null || OBJtoWrite.gameObject == null) return;
                int ID = OBJtoWrite.gameObject.GetInstanceID();
                int Index = (new List<string>(OBJtoWrite.Names)).IndexOf(NameIndex);
                RayObjectDatas TempOBJ = raywrites.RayObj.Find((s1) => (s1.MatName.Equals(NameIndex) && s1.ID.Equals(ID)));
                int WriteID = raywrites.RayObj.IndexOf(TempOBJ);
                RayObjectDatas DataToWrite = new RayObjectDatas() {
                    ID = ID,
                    MatName = NameIndex,
                    OptionID = (int)OBJtoWrite.MaterialOptions[Index],
                    TransCol = OBJtoWrite.TransmissionColor[Index],
                    BaseCol = OBJtoWrite.BaseColor[Index],
                    MetRemap = OBJtoWrite.MetallicRemap[Index],
                    RoughRemap = OBJtoWrite.RoughnessRemap[Index],
                    Emiss = OBJtoWrite.emission[Index],
                    EmissCol = OBJtoWrite.EmissionColor[Index],
                    Rough = OBJtoWrite.Roughness[Index],
                    IOR = OBJtoWrite.IOR[Index],
                    Met = OBJtoWrite.Metallic[Index],
                    SpecTint = OBJtoWrite.SpecularTint[Index],
                    Sheen = OBJtoWrite.Sheen[Index],
                    SheenTint = OBJtoWrite.SheenTint[Index],
                    Clearcoat = OBJtoWrite.ClearCoat[Index],
                    ClearcoatGloss = OBJtoWrite.ClearCoatGloss[Index],
                    Anisotropic = OBJtoWrite.Anisotropic[Index],
                    Flatness = OBJtoWrite.Flatness[Index],
                    DiffTrans = OBJtoWrite.DiffTrans[Index],
                    SpecTrans = OBJtoWrite.SpecTrans[Index],
                    FollowMat = OBJtoWrite.FollowMaterial[Index],
                    ScatterDist = OBJtoWrite.ScatterDist[Index],
                    Spec = OBJtoWrite.Specular[Index],
                    AlphaCutoff = OBJtoWrite.AlphaCutoff[Index],
                    NormStrength = OBJtoWrite.NormalStrength[Index],
                    Hue = OBJtoWrite.Hue[Index],
                    Brightness = OBJtoWrite.Brightness[Index],
                    Contrast = OBJtoWrite.Contrast[Index],
                    Saturation = OBJtoWrite.Saturation[Index],
                    BlendColor = OBJtoWrite.BlendColor[Index],
                    BlendFactor = OBJtoWrite.BlendFactor[Index],
                    MainTexScaleOffset = OBJtoWrite.MainTexScaleOffset[Index],
                    SecondaryAlbedoTexScaleOffset = OBJtoWrite.SecondaryAlbedoTexScaleOffset[Index],
                    SecondaryTextureScale = OBJtoWrite.SecondaryTextureScale[Index],
                    Rotation = OBJtoWrite.Rotation[Index],
                    Flags = OBJtoWrite.Flags[Index],
                    UseKelvin = OBJtoWrite.UseKelvin[Index],
                    KelvinTemp = OBJtoWrite.KelvinTemp[Index],
                    ColorBleed = OBJtoWrite.ColorBleed[Index],
                    AlbedoBlendFactor = OBJtoWrite.AlbedoBlendFactor[Index]
                };
                if(WriteID == -1) {
                    raywrites.RayObj.Add(DataToWrite);
                } else {
                    raywrites.RayObj[WriteID] = DataToWrite;
                }
            }
        }

        [HideInInspector] public ComputeShader ShadingShader;
        private ComputeShader IntersectionShader;
        private ComputeShader GenerateShader;
        private ComputeShader ReSTIRGI;
        private ComputeShader CDFCompute;

        private RenderTexture _target;
        private RenderTexture _converged;
        private RenderTexture _DebugTex;
        private RenderTexture _FinalTex;
        private RenderTexture _RandomNums;
        private RenderTexture _RandomNumsB;
        private RenderTexture _PrimaryTriangleInfo;

        private RenderTexture GIReservoirA;
        private RenderTexture GIReservoirB;

        private RenderTexture GIWorldPosA;
        private RenderTexture GIWorldPosB;
        private RenderTexture GIWorldPosC;

        private RenderTexture GINEEPosA;
        private RenderTexture GINEEPosB;

        private RenderTexture CDFX;
        private RenderTexture CDFY;

        private RenderTexture Gradients;


        private bool OIDNGuideWrite;

        [HideInInspector] public RenderTexture ScreenSpaceInfo;
        [HideInInspector] public bool IsFocusing = false;
        private RenderTexture ScreenSpaceInfoPrev;

        private ComputeBuffer _RayBuffer;
        private ComputeBuffer LightingBuffer;
        private ComputeBuffer PrevLightingBufferA;
        private ComputeBuffer PrevLightingBufferB;
        private ComputeBuffer _BufferSizes;
        private ComputeBuffer _ShadowBuffer;
        private ComputeBuffer RaysBuffer;
        private ComputeBuffer RaysBufferB;
        private ComputeBuffer CurBounceInfoBuffer;
        private ComputeBuffer CDFTotalBuffer;

        #if UseOIDN
            private GraphicsBuffer ColorBuffer;
            private GraphicsBuffer OutputBuffer;
            private GraphicsBuffer AlbedoBuffer;
            private GraphicsBuffer NormalBuffer;
        #endif
        #if !DisableRadianceCache
            private ComputeBuffer CacheBuffer;
            private ComputeBuffer VoxelDataBufferA;
            private ComputeBuffer VoxelDataBufferB;
            private ComputeBuffer HashBuffer;
            private ComputeBuffer HashBufferB;
        #endif

        private Texture3D ToneMapTex;
        private Texture3D ToneMapTex2;
        private Material _addMaterial;
        private Material _FireFlyMaterial;
        [HideInInspector] public int _currentSample = 0;
        private static bool _meshObjectsNeedRebuilding = false;
        public static List<RayTracingLights> _rayTracingLights = new List<RayTracingLights>();
        public static bool RTOShowBase = true;
        public static bool RTOShowEmission = false;
        public static bool RTOShowAdvanced = false;
        public static bool RTOShowColorModifiers = false;
        public static bool RTOShowMisc = false;


        [HideInInspector] public Vector2 CurrentHorizonalPatch;
        private float _lastFieldOfView;

        [HideInInspector] public int FramesSinceStart2;
        private BufferSizeData[] BufferSizes;
        [SerializeField] [HideInInspector] public int SampleCount;

        private int uFirstFrame = 1;
        [HideInInspector] public static bool DoDing = true;
        [HideInInspector] public static bool DoCheck = false;
        [HideInInspector] public bool PrevReSTIRGI = false;

        [HideInInspector] public bool DoPanorama = false;
        [HideInInspector] public bool DoChainedImages = false;
        [HideInInspector] [SerializeField] public TTSettings LocalTTSettings;

        public static bool SceneIsRunning = false;

        [HideInInspector] public Texture SkyboxTexture;
        private bool MeshOrderChanged = false;


        [HideInInspector] public int AtmoNumLayers = 4;
        private float PrevResFactor;
        private int GenKernel;
        private int GenPanoramaKernel;
        private int GenASVGFKernel;
        private int TraceKernel;
        private int ShadowKernel;
        private int HeightmapKernel;
        private int HeightmapShadowKernel;
        private int ShadeKernel;
        private int FinalizeKernel;
        private int GIReTraceKernel;
        private int TransferKernel;
        private int ReSTIRGIKernel;
        private int ReSTIRGISpatialKernel;
        private int TTtoOIDNKernel;
        private int OIDNtoTTKernel;
        private int TTtoOIDNKernelPanorama;
        #if !DisableRadianceCache
            private int ResolveKernel;
        #endif
        private int TargetWidth;
        private int TargetHeight;
        [HideInInspector] public int FramesSinceStart;
        [System.NonSerialized] public int SourceWidth;
        [System.NonSerialized] public int SourceHeight;
        private Vector3 PrevCamPosition;
        private bool PrevASVGF;
        private Matrix4x4 PrevViewProjection;


        [System.Serializable]
        public struct BufferSizeData
        {
            public int tracerays;
            public int shadow_rays;
            public int heightmap_rays;
            public int Heightmap_shadow_rays;
        }

        private void LoadInitialSettings() {//Loads settings from text file only in builds 
            if(AssetManager.Assets != null) {
                AssetManager.Assets.MainDesiredRes = LocalTTSettings.MainDesiredRes;
                AssetManager.Assets.UseSkinning = LocalTTSettings.UseSkinning;
            }
        }


        public void TossCamera(Camera camera) {
            _camera = camera;
        }
        unsafe public void Start2()
        {
            CurrentHorizonalPatch = new Vector2(0,1);
            LoadTT();
            // LoadInitialSettings();//Build only
            Application.targetFrameRate = 165;
            ASVGFCode = new ASVGF();
            ReSTIRASVGFCode = new ReSTIRASVGF();
            ToneMapTex = Resources.Load<Texture3D>("Utility/ToneMapTex");
            ToneMapTex2 = Resources.Load<Texture3D>("Utility/AgXBC");
            if (ShadingShader == null) {ShadingShader = Resources.Load<ComputeShader>("MainCompute/RayTracingShader"); }
            if (IntersectionShader == null) {IntersectionShader = Resources.Load<ComputeShader>("MainCompute/IntersectionKernels"); }
            if (GenerateShader == null) {GenerateShader = Resources.Load<ComputeShader>("MainCompute/RayGenKernels"); }
            if (ReSTIRGI == null) {ReSTIRGI = Resources.Load<ComputeShader>("MainCompute/ReSTIRGI"); }
            TargetWidth = 1;
            TargetHeight = 1;
            SourceWidth = 1;
            SourceHeight = 1;
            PrevResFactor = LocalTTSettings.RenderScale;
            _meshObjectsNeedRebuilding = true;
            Assets = gameObject.GetComponent<AssetManager>();
            Assets.BuildCombined();
            uFirstFrame = 1;
            FramesSinceStart = 0;
            GenKernel = GenerateShader.FindKernel("Generate");
            GenPanoramaKernel = GenerateShader.FindKernel("GeneratePanorama");
            GenASVGFKernel = GenerateShader.FindKernel("GenerateASVGF");
            TraceKernel = IntersectionShader.FindKernel("kernel_trace");
            ShadowKernel = IntersectionShader.FindKernel("kernel_shadow");
            ShadeKernel = ShadingShader.FindKernel("kernel_shade");
            FinalizeKernel = ShadingShader.FindKernel("kernel_finalize");
            HeightmapShadowKernel = IntersectionShader.FindKernel("kernel_shadow_heightmap");
            HeightmapKernel = IntersectionShader.FindKernel("kernel_heightmap");
            GIReTraceKernel = GenerateShader.FindKernel("GIReTraceKernel");
            TransferKernel = ShadingShader.FindKernel("TransferKernel");
            ReSTIRGIKernel = ReSTIRGI.FindKernel("ReSTIRGIKernel");
            ReSTIRGISpatialKernel = ReSTIRGI.FindKernel("ReSTIRGISpatial");
            TTtoOIDNKernel = ShadingShader.FindKernel("TTtoOIDNKernel");
            OIDNtoTTKernel = ShadingShader.FindKernel("OIDNtoTTKernel");
            TTtoOIDNKernelPanorama = ShadingShader.FindKernel("TTtoOIDNKernelPanorama");
            OIDNGuideWrite = false;
            #if !DisableRadianceCache
                ResolveKernel = GenerateShader.FindKernel("CacheResolve");
            #endif
            ASVGFCode.Initialized = false;
            ReSTIRASVGFCode.Initialized = false;

            Atmo = new AtmosphereGenerator(6360, 6420, AtmoNumLayers);
            FramesSinceStart2 = 0;
            TTPostProc = new TTPostProcessing();
            TTPostProc.Initialized = false;
        }
        public void Awake() {
            LoadTT();
        }
        public void Start() {
            DoPanorama = false;
            DoChainedImages = false;
            LoadTT();
        }

        void Reset() {
            LoadTT();
        }
        public void LoadTT() {
            if(LocalTTSettings == null || !LocalTTSettings.name.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)) {
                #if UNITY_EDITOR
                    UnityEngine.SceneManagement.Scene CurrentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    string path = CurrentScene.path.Replace(".unity", "");
                    LocalTTSettings = UnityEditor.AssetDatabase.LoadAssetAtPath(path + ".asset", typeof(TTSettings)) as TTSettings;
                    if(LocalTTSettings == null) {
                        LocalTTSettings = ScriptableObject.CreateInstance<TTSettings>();
                        UnityEditor.AssetDatabase.CreateAsset(LocalTTSettings, path + ".asset");
                        UnityEditor.AssetDatabase.SaveAssets();
                    }
                #endif
            }
            #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(LocalTTSettings);
            #endif
            if(LocalTTSettings != null) {
                LoadInitialSettings();
            }
        }

        private void OnEnable() {
            _currentSample = 0;
            LoadTT();
        }

        void OnDestroy() {
            #if UNITY_EDITOR
            string saveFilePath = TTPathFinder.GetSaveFilePath();
                using(StreamWriter writer = new StreamWriter(saveFilePath)) {
                    var serializer = new XmlSerializer(typeof(RayObjs));
                    serializer.Serialize(writer.BaseStream, raywrites);
                    UnityEditor.AssetDatabase.Refresh();
                }
            #endif
        }
        public void OnDisable() {
            DoCheck = true;
            _RayBuffer?.Release();
            LightingBuffer?.Release();
            PrevLightingBufferA?.Release();
            PrevLightingBufferB?.Release();
            _BufferSizes?.Release();
            _ShadowBuffer?.Release();
            RaysBuffer?.Release();
            RaysBufferB?.Release();
            #if UseOIDN
                ColorBuffer?.Release();
                OutputBuffer?.Release();
                AlbedoBuffer?.Release();
                NormalBuffer?.Release();
                if(OIDNDenoiser != null) {
                    OIDNDenoiser.Dispose();
                    OIDNDenoiser = null;
                }
            #endif
            if(ASVGFCode != null) ASVGFCode.ClearAll();
            if(ReSTIRASVGFCode != null) ReSTIRASVGFCode.ClearAll();
            CurBounceInfoBuffer?.Release();
            TTPostProc.ClearAll();
            CDFX.ReleaseSafe();
            CDFY.ReleaseSafe();
            CDFTotalBuffer.ReleaseSafe();
            #if !DisableRadianceCache
                CacheBuffer.ReleaseSafe();
                VoxelDataBufferA.ReleaseSafe();
                VoxelDataBufferB.ReleaseSafe();
                HashBuffer.ReleaseSafe();
                HashBufferB.ReleaseSafe();
            #endif
        }

        private void RunUpdate() {
            ShadingShader.SetVector("SunDir", Assets.SunDirection);
            if (!LocalTTSettings.Accumulate || IsFocusing) {
                SampleCount = 0;
                FramesSinceStart = 0;
            }

            if (_camera.fieldOfView != _lastFieldOfView) {
                FramesSinceStart = 0;
                _lastFieldOfView = _camera.fieldOfView;
            }

            if (_camera.transform.hasChanged) {
                SampleCount = 0;
                FramesSinceStart = 0;
                _camera.transform.hasChanged = false;
            }
        }

        public static void RegisterObject(RayTracingLights obj) {//Adds meshes to list
            _rayTracingLights.Add(obj);
        }
        public static void UnregisterObject(RayTracingLights obj) {//Removes meshes from list
            _rayTracingLights.Remove(obj);
        }

        public bool RebuildMeshObjectBuffers(CommandBuffer cmd)
        {
            cmd.BeginSample("Full Update");
            if (uFirstFrame != 1) {
                if (LocalTTSettings.DoTLASUpdates) {
                    int UpdateFlags = Assets.UpdateTLAS(cmd);
                    if (UpdateFlags == 1 || UpdateFlags == 3) {
                        MeshOrderChanged = true;
                        uFirstFrame = 1;
                    } else if(UpdateFlags == 2) {
                        MeshOrderChanged = false;
                    } else if(UpdateFlags != -1) {
                        MeshOrderChanged = false;
                    } else return false;
                }
            }
            cmd.EndSample("Full Update");
            if (!_meshObjectsNeedRebuilding) return true;
            _meshObjectsNeedRebuilding = false;
            FramesSinceStart = 0;


            if(CurBounceInfoBuffer != null) CurBounceInfoBuffer.Release();
            CurBounceInfoBuffer = new ComputeBuffer(1, 12);
            if(_RayBuffer == null || _RayBuffer.count != SourceWidth * SourceHeight) {
                CommonFunctions.CreateDynamicBuffer(ref _RayBuffer, SourceWidth * SourceHeight * 2, 48);
                CommonFunctions.CreateDynamicBuffer(ref _ShadowBuffer, SourceWidth * SourceHeight, 48);
                CommonFunctions.CreateDynamicBuffer(ref LightingBuffer, SourceWidth * SourceHeight, 64);
                CommonFunctions.CreateDynamicBuffer(ref PrevLightingBufferA, SourceWidth * SourceHeight, 64);
                CommonFunctions.CreateDynamicBuffer(ref PrevLightingBufferB, SourceWidth * SourceHeight, 64);
                CommonFunctions.CreateDynamicBuffer(ref RaysBuffer, SourceWidth * SourceHeight, 24);
                CommonFunctions.CreateDynamicBuffer(ref RaysBufferB, SourceWidth * SourceHeight, 24);
            }
            GenerateShader.SetBuffer(GenASVGFKernel, "Rays", RaysBuffer);
            return true;
        }

       
        private void SetMatrix(string Name, Matrix4x4 Mat) {
            ShadingShader.SetMatrix(Name, Mat);
            IntersectionShader.SetMatrix(Name, Mat);
            GenerateShader.SetMatrix(Name, Mat);
            ReSTIRGI.SetMatrix(Name, Mat);
        }

        private void SetVector(string Name, Vector3 IN, CommandBuffer cmd) {
            cmd.SetComputeVectorParam(ShadingShader, Name, IN);
            cmd.SetComputeVectorParam(IntersectionShader, Name, IN);
            cmd.SetComputeVectorParam(GenerateShader, Name, IN);
            cmd.SetComputeVectorParam(ReSTIRGI, Name, IN);
        }

        private void SetInt(string Name, int IN, CommandBuffer cmd) {
            cmd.SetComputeIntParam(ShadingShader, Name, IN);
            cmd.SetComputeIntParam(IntersectionShader, Name, IN);
            cmd.SetComputeIntParam(GenerateShader, Name, IN);
            cmd.SetComputeIntParam(ReSTIRGI, Name, IN);
        }

        private void SetFloat(string Name, float IN, CommandBuffer cmd) {
            cmd.SetComputeFloatParam(ShadingShader, Name, IN);
            cmd.SetComputeFloatParam(IntersectionShader, Name, IN);
            cmd.SetComputeFloatParam(GenerateShader, Name, IN);
            cmd.SetComputeFloatParam(ReSTIRGI, Name, IN);
        }

        private void SetBool(string Name, bool IN) {
            ShadingShader.SetBool(Name, IN);
            IntersectionShader.SetBool(Name, IN);
            GenerateShader.SetBool(Name, IN);
            ReSTIRGI.SetBool(Name, IN);
        }
        Matrix4x4 CamInvProjPrev;
        Matrix4x4 CamToWorldPrev;
        Vector3 PrevPos;
        private Vector2 HDRIParams = Vector2.zero;
        private void SetShaderParameters(CommandBuffer cmd)
        {
            if(LocalTTSettings.RenderScale != 1.0f) _camera.renderingPath = RenderingPath.DeferredShading;
            _camera.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
            if(LocalTTSettings.UseReSTIRGI && LocalTTSettings.UseASVGF && !ReSTIRASVGFCode.Initialized) ReSTIRASVGFCode.init(SourceWidth, SourceHeight);
            else if ((!LocalTTSettings.UseASVGF || !LocalTTSettings.UseReSTIRGI) && ReSTIRASVGFCode.Initialized) ReSTIRASVGFCode.ClearAll();
            if (!LocalTTSettings.UseReSTIRGI && LocalTTSettings.UseASVGF && !ASVGFCode.Initialized) ASVGFCode.init(SourceWidth, SourceHeight);
            else if ((!LocalTTSettings.UseASVGF || LocalTTSettings.UseReSTIRGI) && ASVGFCode.Initialized) ASVGFCode.ClearAll();
            if(TTPostProc.Initialized == false) TTPostProc.init(SourceWidth, SourceHeight);

            BufferSizes = new BufferSizeData[LocalTTSettings.bouncecount + 1];
            BufferSizes[0].tracerays = SourceWidth * SourceHeight;
            BufferSizes[0].heightmap_rays = SourceWidth * SourceHeight;
            if(_BufferSizes == null) {
                _BufferSizes = new ComputeBuffer(LocalTTSettings.bouncecount + 1, 16);
            }
            if(_BufferSizes.count != LocalTTSettings.bouncecount + 1) {
                _BufferSizes.Release();
                _BufferSizes = new ComputeBuffer(LocalTTSettings.bouncecount + 1, 16);
            }
            _BufferSizes.SetData(BufferSizes);
            ShadingShader.SetBuffer(ShadeKernel, "BufferSizes", _BufferSizes);
            ShadingShader.SetBuffer(TransferKernel, "BufferSizes", _BufferSizes);
            IntersectionShader.SetBuffer(TraceKernel, "BufferSizes", _BufferSizes);
            IntersectionShader.SetBuffer(ShadowKernel, "BufferSizes", _BufferSizes);
            IntersectionShader.SetBuffer(HeightmapShadowKernel, "BufferSizes", _BufferSizes);
            IntersectionShader.SetBuffer(HeightmapKernel, "BufferSizes", _BufferSizes);
            ShadingShader.SetComputeBuffer(TransferKernel, "BufferData", CurBounceInfoBuffer);
            ShadingShader.SetComputeBuffer(ShadeKernel, "BufferData", CurBounceInfoBuffer);

            var EA = CamToWorldPrev;
            var EB = CamInvProjPrev;
            CamInvProjPrev = _camera.projectionMatrix.inverse;
            CamToWorldPrev = _camera.cameraToWorldMatrix;
            SetMatrix("CamInvProj", _camera.projectionMatrix.inverse);
            SetMatrix("CamToWorld", _camera.cameraToWorldMatrix);
            SetMatrix("CamInvProjPrev", EB);
            SetMatrix("CamToWorldPrev", EA);
            SetMatrix("ViewMatrix", _camera.worldToCameraMatrix);
            var E = _camera.transform.position - PrevPos;
            SetVector("Up", _camera.transform.up, cmd);
            SetVector("Right", _camera.transform.right, cmd);
            SetVector("Forward", _camera.transform.forward, cmd);
            SetVector("CamPos", _camera.transform.position, cmd);
            Vector3 Temp22 = PrevPos;
            PrevPos = _camera.transform.position;
            SetVector("PrevCamPos", Temp22, cmd);
            SetVector("CamDelta", E, cmd);
            SetVector("BackgroundColor", LocalTTSettings.SceneBackgroundColor, cmd);
            SetVector("SecondaryBackgroundColor", LocalTTSettings.SecondarySceneBackgroundColor, cmd);
            SetVector("ClayColor", LocalTTSettings.ClayColor, cmd);
            SetVector("GroundColor", LocalTTSettings.GroundColor, cmd);
            SetVector("Segment", CurrentHorizonalPatch, cmd);
            SetVector("HDRILongLat", LocalTTSettings.HDRILongLat, cmd);
            SetVector("HDRIScale", LocalTTSettings.HDRIScale, cmd);
            SetVector("MousePos", Input.mousePosition, cmd);
            if(LocalTTSettings.UseASVGF && !LocalTTSettings.UseReSTIRGI) ASVGFCode.shader.SetVector("CamDelta", E);
            if(LocalTTSettings.UseASVGF && LocalTTSettings.UseReSTIRGI) ReSTIRASVGFCode.shader.SetVector("CamDelta", E);

            Shader.SetGlobalInt("PartialRenderingFactor", LocalTTSettings.PartialRenderingFactor);
            SetFloat("FarPlane", _camera.farClipPlane, cmd);
            SetFloat("NearPlane", _camera.nearClipPlane, cmd);
            SetFloat("focal_distance", LocalTTSettings.DoFFocal, cmd);
            SetFloat("AperatureRadius", LocalTTSettings.DoFAperature * LocalTTSettings.DoFAperatureScale, cmd);
            SetFloat("IndirectBoost", LocalTTSettings.IndirectBoost, cmd);
            SetFloat("GISpatialRadius", LocalTTSettings.ReSTIRGISpatialRadius, cmd);
            SetFloat("SkyDesaturate", LocalTTSettings.SkyDesaturate, cmd);
            SetFloat("SecondarySkyDesaturate", LocalTTSettings.SecondarySkyDesaturate, cmd);
            SetFloat("BackgroundIntensity", LocalTTSettings.BackgroundIntensity.x, cmd);
            SetFloat("SecondaryBackgroundIntensity", LocalTTSettings.BackgroundIntensity.y, cmd);
            SetFloat("LEMEnergyScale", LocalTTSettings.LEMEnergyScale, cmd);
            SetFloat("OIDNBlendRatio", LocalTTSettings.OIDNBlendRatio, cmd);

            SetInt("AlbedoAtlasSize", Assets.AlbedoAtlasSize, cmd);
            SetInt("LightMeshCount", Assets.LightMeshCount, cmd);
            SetInt("unitylightcount", Assets.UnityLightCount, cmd);
            SetInt("screen_width", SourceWidth, cmd);
            SetInt("screen_height", SourceHeight, cmd);
            SetInt("MaxBounce", LocalTTSettings.bouncecount - 1, cmd);
            SetInt("frames_accumulated", _currentSample, cmd);
            SetInt("ReSTIRGISpatialCount", LocalTTSettings.ReSTIRGISpatialCount, cmd);
            SetInt("ReSTIRGITemporalMCap", LocalTTSettings.ReSTIRGITemporalMCap, cmd);
            SetInt("curframe", FramesSinceStart2, cmd);
            SetInt("TerrainCount", Assets.Terrains.Count, cmd);
            SetInt("ReSTIRGIUpdateRate", LocalTTSettings.UseReSTIRGI ? LocalTTSettings.ReSTIRGIUpdateRate : 0, cmd);
            SetInt("RISCount", LocalTTSettings.RISCount, cmd);
            SetInt("BackgroundType", LocalTTSettings.BackgroundType, cmd);
            SetInt("SecondaryBackgroundType", LocalTTSettings.SecondaryBackgroundType, cmd);
            SetInt("MaterialCount", Assets.MatCount, cmd);
            SetInt("PartialRenderingFactor", LocalTTSettings.DoPartialRendering ? LocalTTSettings.PartialRenderingFactor : 1, cmd);

            SetBool("IsFocusing", IsFocusing);
            SetBool("DoPanorama", DoPanorama);
            SetBool("ClayMode", LocalTTSettings.ClayMode);
            SetBool("ImprovedPrimaryHit", LocalTTSettings.ImprovedPrimaryHit);
            SetBool("UseRussianRoulette", LocalTTSettings.UseRussianRoulette);
            SetBool("UseNEE", LocalTTSettings.UseNEE);
            SetBool("UseDoF", LocalTTSettings.PPDoF);
            SetBool("UseReSTIRGI", LocalTTSettings.UseReSTIRGI);
            SetBool("UseReSTIRGITemporal", LocalTTSettings.UseReSTIRGITemporal);
            SetBool("UseReSTIRGISpatial", LocalTTSettings.UseReSTIRGISpatial);
            SetBool("DoReSTIRGIConnectionValidation", LocalTTSettings.DoReSTIRGIConnectionValidation);
            SetBool("UseASVGF", LocalTTSettings.UseASVGF && !LocalTTSettings.UseReSTIRGI);
            SetBool("TerrainExists", Assets.Terrains.Count != 0);
            SetBool("DoPartialRendering", LocalTTSettings.DoPartialRendering);
            SetBool("UseTransmittanceInNEE", LocalTTSettings.UseTransmittanceInNEE);
            OIDNGuideWrite = (FramesSinceStart == LocalTTSettings.OIDNFrameCount);
            SetBool("OIDNGuideWrite", OIDNGuideWrite && LocalTTSettings.UseOIDN);
            SetBool("DiffRes", LocalTTSettings.RenderScale != 1.0f);
            SetBool("DoPartialRendering", LocalTTSettings.DoPartialRendering);
            SetBool("DoExposure", LocalTTSettings.PPExposure);
            ShadingShader.SetBuffer(ShadeKernel, "Exposure", TTPostProc.ExposureBuffer);

            bool FlipFrame = (FramesSinceStart2 % 2 == 0);


            GenerateShader.SetTextureFromGlobal(GIReTraceKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            ReSTIRGI.SetTextureFromGlobal(ReSTIRGIKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            ReSTIRGI.SetTextureFromGlobal(ReSTIRGISpatialKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            ShadingShader.SetTextureFromGlobal(ShadeKernel, "MotionVectors", "_CameraMotionVectorsTexture");

            ShadingShader.SetTextureFromGlobal(FinalizeKernel, "DiffuseGBuffer", "_CameraGBufferTexture0");
            ShadingShader.SetTextureFromGlobal(FinalizeKernel, "SpecularGBuffer", "_CameraGBufferTexture1");


            if (SkyboxTexture == null) SkyboxTexture = new Texture2D(1,1, TextureFormat.RGBA32, false);
            if (SkyboxTexture != null)
            {
                if(CDFTotalBuffer == null) {
                    CDFTotalBuffer = new ComputeBuffer(1, 4);
                }
                if((LocalTTSettings.BackgroundType == 1 || LocalTTSettings.SecondaryBackgroundType == 1) && CDFX == null) {
                    CDFX.ReleaseSafe();
                    CDFY.ReleaseSafe();
                    CDFCompute = Resources.Load<ComputeShader>("Utility/CDFCreator");
                    CommonFunctions.CreateRenderTexture(ref CDFX, SkyboxTexture.width, SkyboxTexture.height, CommonFunctions.RTFull1);
                    CommonFunctions.CreateRenderTexture(ref CDFY, SkyboxTexture.height, 1, CommonFunctions.RTFull1);
                    float[] CDFTotalInit = new float[1];
                    CDFTotalBuffer.SetData(CDFTotalInit);
                    ComputeBuffer CounterBuffer = new ComputeBuffer(1, 4);
                    int[] CounterInit = new int[1];
                    CounterBuffer.SetData(CounterInit);
                    CDFCompute.SetTexture(0, "Tex", SkyboxTexture);
                    CDFCompute.SetTexture(0, "CDFX", CDFX);
                    CDFCompute.SetTexture(0, "CDFY", CDFY);
                    CDFCompute.SetInt("w", SkyboxTexture.width);
                    CDFCompute.SetInt("h", SkyboxTexture.height);
                    CDFCompute.SetBuffer(0, "CounterBuffer", CounterBuffer);
                    CDFCompute.SetBuffer(0, "TotalBuff", CDFTotalBuffer);
                    CDFCompute.Dispatch(0, 1, SkyboxTexture.height, 1);
                    CounterBuffer.Release();
                    HDRIParams = new Vector2(SkyboxTexture.width, SkyboxTexture.height);
                    ShadingShader.SetTexture(ShadeKernel, "CDFX", CDFX);
                    ShadingShader.SetTexture(ShadeKernel, "CDFY", CDFY);
                }
                if(LocalTTSettings.BackgroundType != 1 && LocalTTSettings.SecondaryBackgroundType != 1) {
                    ShadingShader.SetTexture(ShadeKernel, "CDFX", SkyboxTexture);
                    ShadingShader.SetTexture(ShadeKernel, "CDFY", SkyboxTexture);                    
                } else {
                    ShadingShader.SetTexture(ShadeKernel, "CDFX", CDFX);
                    ShadingShader.SetTexture(ShadeKernel, "CDFY", CDFY);

                }
                ShadingShader.SetBuffer(ShadeKernel, "TotSum", CDFTotalBuffer);
                ShadingShader.SetTexture(ShadeKernel, "_SkyboxTexture", SkyboxTexture);
            }
            SetVector("HDRIParams", HDRIParams, cmd);

            GenerateShader.SetBuffer(GenASVGFKernel, "Rays", (FramesSinceStart2 % 2 == 0) ? RaysBuffer : RaysBufferB);
            GenerateShader.SetTexture(GenASVGFKernel, "RandomNums", FlipFrame ? _RandomNums : _RandomNumsB);
            GenerateShader.SetComputeBuffer(GenASVGFKernel, "GlobalRays", _RayBuffer);
            GenerateShader.SetComputeBuffer(GenASVGFKernel, "GlobalColors", LightingBuffer);
            GenerateShader.SetTexture(GenASVGFKernel, "WorldPosA", GIWorldPosA);
            GenerateShader.SetTexture(GenASVGFKernel, "NEEPosA", FlipFrame ? GINEEPosA : GINEEPosB);
            GenerateShader.SetComputeBuffer(GenASVGFKernel, "GlobalRays", _RayBuffer);
            GenerateShader.SetComputeBuffer(GenASVGFKernel, "GlobalColors", LightingBuffer);


            GenerateShader.SetTexture(GenKernel, "RandomNums", (FramesSinceStart2 % 2 == 0) ? _RandomNums : _RandomNumsB);
            GenerateShader.SetComputeBuffer(GenKernel, "GlobalRays", _RayBuffer);
            GenerateShader.SetComputeBuffer(GenKernel, "GlobalColors", LightingBuffer);            
            GenerateShader.SetTexture(GenPanoramaKernel, "RandomNums", (FramesSinceStart2 % 2 == 0) ? _RandomNums : _RandomNumsB);
            GenerateShader.SetComputeBuffer(GenPanoramaKernel, "GlobalRays", _RayBuffer);
            GenerateShader.SetComputeBuffer(GenPanoramaKernel, "GlobalColors", LightingBuffer);            

            AssetManager.Assets.SetMeshTraceBuffers(IntersectionShader, TraceKernel);
            IntersectionShader.SetComputeBuffer(TraceKernel, "GlobalRays", _RayBuffer);
            IntersectionShader.SetComputeBuffer(TraceKernel, "GlobalColors", LightingBuffer);
            IntersectionShader.SetTexture(TraceKernel, "_PrimaryTriangleInfo", _PrimaryTriangleInfo);


            AssetManager.Assets.SetMeshTraceBuffers(IntersectionShader, ShadowKernel);
            IntersectionShader.SetComputeBuffer(ShadowKernel, "ShadowRaysBuffer", _ShadowBuffer);
            IntersectionShader.SetComputeBuffer(ShadowKernel, "GlobalColors", LightingBuffer);
            IntersectionShader.SetTexture(ShadowKernel, "NEEPosA", FlipFrame ? GINEEPosA : GINEEPosB);
            IntersectionShader.SetTexture(TraceKernel, "RandomNums", FlipFrame ? _RandomNums : _RandomNumsB);


            AssetManager.Assets.SetHeightmapTraceBuffers(IntersectionShader, HeightmapKernel);
            IntersectionShader.SetComputeBuffer(HeightmapKernel, "GlobalRays", _RayBuffer);
            IntersectionShader.SetTexture(HeightmapKernel, "_PrimaryTriangleInfo", _PrimaryTriangleInfo);

            AssetManager.Assets.SetHeightmapTraceBuffers(IntersectionShader, HeightmapShadowKernel);
            IntersectionShader.SetComputeBuffer(HeightmapShadowKernel, "GlobalColors", LightingBuffer);
            IntersectionShader.SetComputeBuffer(HeightmapShadowKernel, "ShadowRaysBuffer", _ShadowBuffer);
            IntersectionShader.SetTexture(HeightmapShadowKernel, "NEEPosA", FlipFrame ? GINEEPosA : GINEEPosB);


            #if !DisableRadianceCache
                GenerateShader.SetComputeBuffer(ResolveKernel, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                GenerateShader.SetComputeBuffer(ResolveKernel, "VoxelDataBufferB", !FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                GenerateShader.SetComputeBuffer(ResolveKernel, "HashEntriesBuffer", HashBuffer);
                GenerateShader.SetComputeBuffer(ResolveKernel, "HashEntriesBufferB", HashBufferB);
                GenerateShader.SetComputeBuffer(ResolveKernel + 1, "HashEntriesBuffer", HashBuffer);
                GenerateShader.SetComputeBuffer(ResolveKernel + 1, "HashEntriesBufferA2", HashBufferB);
                GenerateShader.SetComputeBuffer(ResolveKernel + 2, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                // GenerateShader.SetComputeBuffer(ResolveKernel + 2, "HashEntriesBufferB", HashBuffer);
                // GenerateShader.SetComputeBuffer(ResolveKernel + 2, "HashEntriesBuffer", HashBufferB);


                IntersectionShader.SetComputeBuffer(ShadowKernel, "CacheBuffer", CacheBuffer);
                IntersectionShader.SetComputeBuffer(HeightmapShadowKernel, "CacheBuffer", CacheBuffer);
                GenerateShader.SetComputeBuffer(GIReTraceKernel, "CacheBuffer", CacheBuffer);
                GenerateShader.SetComputeBuffer(GenASVGFKernel, "CacheBuffer", CacheBuffer);
                GenerateShader.SetComputeBuffer(GenASVGFKernel, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                GenerateShader.SetComputeBuffer(GenKernel, "CacheBuffer", CacheBuffer);
                GenerateShader.SetComputeBuffer(GenKernel, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);

                GenerateShader.SetComputeBuffer(GenPanoramaKernel, "CacheBuffer", CacheBuffer);
                GenerateShader.SetComputeBuffer(GenPanoramaKernel, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                
                ShadingShader.SetComputeBuffer(ShadeKernel, "CacheBuffer", CacheBuffer);
                ShadingShader.SetComputeBuffer(ShadeKernel, "HashEntriesBuffer", HashBuffer);
                ShadingShader.SetComputeBuffer(ShadeKernel, "HashEntriesBufferB", HashBufferB);
                ShadingShader.SetComputeBuffer(ShadeKernel, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                ShadingShader.SetComputeBuffer(ShadeKernel, "VoxelDataBufferB", !FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                
                ShadingShader.SetComputeBuffer(FinalizeKernel, "CacheBuffer", CacheBuffer);
                ShadingShader.SetComputeBuffer(FinalizeKernel, "HashEntriesBufferB", HashBuffer);
                ShadingShader.SetComputeBuffer(FinalizeKernel, "VoxelDataBufferA", !FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                ShadingShader.SetComputeBuffer(FinalizeKernel, "VoxelDataBufferB", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
            #endif

            Atmo.AssignTextures(ShadingShader, ShadeKernel);
            AssetManager.Assets.SetLightData(ShadingShader, ShadeKernel);
            AssetManager.Assets.SetMeshTraceBuffers(ShadingShader, ShadeKernel);
            AssetManager.Assets.SetHeightmapTraceBuffers(ShadingShader, ShadeKernel);
            ShadingShader.SetTexture(ShadeKernel, "WorldPosB", !FlipFrame ? GIWorldPosB : GIWorldPosC);
            ShadingShader.SetTexture(ShadeKernel, "RandomNums", FlipFrame ? _RandomNums : _RandomNumsB);
            ShadingShader.SetTexture(ShadeKernel, "SingleComponentAtlas", Assets.SingleComponentAtlas);
            ShadingShader.SetTexture(ShadeKernel, "_EmissiveAtlas", Assets.EmissiveAtlas);
            ShadingShader.SetTexture(ShadeKernel, "_NormalAtlas", Assets.NormalAtlas);
            ShadingShader.SetTexture(ShadeKernel, "ScreenSpaceInfo", FlipFrame ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
            ShadingShader.SetComputeBuffer(ShadeKernel, "GlobalColors", LightingBuffer);
            ShadingShader.SetComputeBuffer(ShadeKernel, "GlobalRays", _RayBuffer);
            ShadingShader.SetComputeBuffer(ShadeKernel, "ShadowRaysBuffer", _ShadowBuffer);


            ShadingShader.SetBuffer(FinalizeKernel, "GlobalColors", LightingBuffer);
            ShadingShader.SetTexture(FinalizeKernel, "Result", _target);

            #if UseOIDN
                ShadingShader.SetBuffer(TTtoOIDNKernel, "AlbedoBuffer", AlbedoBuffer);
                ShadingShader.SetBuffer(TTtoOIDNKernel, "NormalBuffer", NormalBuffer);
                ShadingShader.SetTexture(TTtoOIDNKernel, "ScreenSpaceInfo", ScreenSpaceInfo);
                ShadingShader.SetComputeBuffer(TTtoOIDNKernel, "GlobalColors", LightingBuffer);
                ShadingShader.SetBuffer(TTtoOIDNKernelPanorama, "AlbedoBuffer", AlbedoBuffer);
                ShadingShader.SetBuffer(TTtoOIDNKernelPanorama, "NormalBuffer", NormalBuffer);
                ShadingShader.SetTexture(TTtoOIDNKernelPanorama, "ScreenSpaceInfo", ScreenSpaceInfo);
                ShadingShader.SetComputeBuffer(TTtoOIDNKernelPanorama, "GlobalColors", LightingBuffer);
            #endif

            GenerateShader.SetComputeBuffer(GIReTraceKernel, "GlobalRays", _RayBuffer);
            GenerateShader.SetComputeBuffer(GIReTraceKernel, "GlobalColors", LightingBuffer);
            GenerateShader.SetTexture(GIReTraceKernel, "WorldPosA", GIWorldPosA);
            GenerateShader.SetTexture(GIReTraceKernel, "NEEPosA", FlipFrame ? GINEEPosA : GINEEPosB);
            GenerateShader.SetComputeBuffer(GIReTraceKernel, "PrevGlobalColorsA", FlipFrame ? PrevLightingBufferA : PrevLightingBufferB);
            GenerateShader.SetBuffer(GIReTraceKernel, "Rays", FlipFrame ? RaysBuffer : RaysBufferB);
            GenerateShader.SetTexture(GIReTraceKernel, "RandomNumsWrite", FlipFrame ? _RandomNums : _RandomNumsB);
            GenerateShader.SetTexture(GIReTraceKernel, "ReservoirA", !FlipFrame ? GIReservoirB : GIReservoirA);
            GenerateShader.SetTexture(GIReTraceKernel, "RandomNums", !FlipFrame ? _RandomNums : _RandomNumsB);
            GenerateShader.SetTexture(GIReTraceKernel, "ScreenSpaceInfo", !FlipFrame ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
            

            AssetManager.Assets.SetMeshTraceBuffers(ReSTIRGI, ReSTIRGIKernel);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "ReservoirA", FlipFrame ? GIReservoirB : GIReservoirA);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "ReservoirB", !FlipFrame ? GIReservoirB : GIReservoirA);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "WorldPosC", GIWorldPosA);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "WorldPosA", FlipFrame ? GIWorldPosB : GIWorldPosC);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "WorldPosB", !FlipFrame ? GIWorldPosB : GIWorldPosC);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "NEEPosA", FlipFrame ? GINEEPosA : GINEEPosB);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "NEEPosB", !FlipFrame ? GINEEPosA : GINEEPosB);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "PrevScreenSpaceInfo", FlipFrame ? ScreenSpaceInfoPrev : ScreenSpaceInfo);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "RandomNums", FlipFrame ? _RandomNums : _RandomNumsB);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "ScreenSpaceInfoRead", FlipFrame ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "GradientWrite", Gradients);
            ReSTIRGI.SetTexture(ReSTIRGIKernel, "PrimaryTriData", _PrimaryTriangleInfo);
            ReSTIRGI.SetComputeBuffer(ReSTIRGIKernel, "PrevGlobalColorsA", FlipFrame ? PrevLightingBufferA : PrevLightingBufferB);
            ReSTIRGI.SetComputeBuffer(ReSTIRGIKernel, "PrevGlobalColorsB", FlipFrame ? PrevLightingBufferB : PrevLightingBufferA);
            ReSTIRGI.SetComputeBuffer(ReSTIRGIKernel, "GlobalColors", LightingBuffer);

            ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "WorldPosC", GIWorldPosA);
            ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "WorldPosB", FlipFrame ? GIWorldPosB : GIWorldPosC);
            ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "NEEPosB", FlipFrame ? GINEEPosA : GINEEPosB);
            ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "ReservoirB", FlipFrame ? GIReservoirB : GIReservoirA);
            ReSTIRGI.SetComputeBuffer(ReSTIRGISpatialKernel, "PrevGlobalColorsA", FlipFrame ? PrevLightingBufferB : PrevLightingBufferA);
            ReSTIRGI.SetComputeBuffer(ReSTIRGISpatialKernel, "GlobalColors", LightingBuffer);
            ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "ScreenSpaceInfoRead", FlipFrame ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
            ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "RandomNums", FlipFrame ? _RandomNums : _RandomNumsB);
            ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "PrimaryTriData", _PrimaryTriangleInfo);




            AssetManager.Assets.SetMeshTraceBuffers(ReSTIRGI, ReSTIRGISpatialKernel);

            Shader.SetGlobalTexture("_DebugTex", _DebugTex);
        }

        private void ResetAllTextures() {
            // _camera.renderingPath = RenderingPath.DeferredShading;
            _camera.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
            if(PrevResFactor != LocalTTSettings.RenderScale || TargetWidth != _camera.scaledPixelWidth) {
                TargetWidth = _camera.scaledPixelWidth;
                TargetHeight = _camera.scaledPixelHeight;
                SourceWidth = (int)Mathf.Ceil((float)TargetWidth * LocalTTSettings.RenderScale);
                SourceHeight = (int)Mathf.Ceil((float)TargetHeight * LocalTTSettings.RenderScale);
                if (Mathf.Abs(SourceWidth - TargetWidth) < 2)
                {
                    SourceWidth = TargetWidth;
                    SourceHeight = TargetHeight;
                    LocalTTSettings.RenderScale = 1;
                }
                PrevResFactor = LocalTTSettings.RenderScale;
                if(LocalTTSettings.UseASVGF && LocalTTSettings.UseReSTIRGI) {ReSTIRASVGFCode.ClearAll(); ReSTIRASVGFCode.init(SourceWidth, SourceHeight);}
                if (LocalTTSettings.UseASVGF && !LocalTTSettings.UseReSTIRGI) {ASVGFCode.ClearAll(); ASVGFCode.init(SourceWidth, SourceHeight);}
                if(TTPostProc.Initialized) TTPostProc.ClearAll();
                TTPostProc.init(SourceWidth, SourceHeight);

                InitRenderTexture(true);
                CommonFunctions.CreateDynamicBuffer(ref _RayBuffer, SourceWidth * SourceHeight * 2, 48);
                CommonFunctions.CreateDynamicBuffer(ref _ShadowBuffer, SourceWidth * SourceHeight, 48);
                CommonFunctions.CreateDynamicBuffer(ref LightingBuffer, SourceWidth * SourceHeight, 64);
                CommonFunctions.CreateDynamicBuffer(ref PrevLightingBufferA, SourceWidth * SourceHeight, 64);
                CommonFunctions.CreateDynamicBuffer(ref PrevLightingBufferB, SourceWidth * SourceHeight, 64);
                CommonFunctions.CreateDynamicBuffer(ref RaysBuffer, SourceWidth * SourceHeight, 24);
                CommonFunctions.CreateDynamicBuffer(ref RaysBufferB, SourceWidth * SourceHeight, 24);
                CommonFunctions.CreateRenderTexture(ref _RandomNums, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref _RandomNumsB, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                #if !DisableRadianceCache
                    CommonFunctions.CreateDynamicBuffer(ref CacheBuffer, SourceWidth * SourceHeight, 48);
                    CommonFunctions.CreateDynamicBuffer(ref VoxelDataBufferA, 4 * 1024 * 1024, 16, ComputeBufferType.Raw);
                    CommonFunctions.CreateDynamicBuffer(ref VoxelDataBufferB, 4 * 1024 * 1024, 16, ComputeBufferType.Raw);
                    CommonFunctions.CreateDynamicBuffer(ref HashBuffer, 4 * 1024 * 1024, 12);
                    CommonFunctions.CreateDynamicBuffer(ref HashBufferB, 4 * 1024 * 1024, 12);

                    int[] EEE = new int[4 * 1024 * 1024 * 3];
                    HashBuffer.SetData(EEE);
                    HashBufferB.SetData(EEE);
                    int[] EEEE = new int[4 * 1024 * 1024 * 4];
                    VoxelDataBufferB.SetData(EEEE);
                    VoxelDataBufferA.SetData(EEEE);
                #endif
            }
            PrevResFactor = LocalTTSettings.RenderScale;
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
                    _DebugTex.Release();
                    _FinalTex.Release();
                    GIReservoirA.Release();
                    GIReservoirB.Release();
                    GINEEPosA.Release();
                    GINEEPosB.Release();
                    GIWorldPosA.Release();
                    GIWorldPosB.Release();
                    GIWorldPosC.Release();
                    _PrimaryTriangleInfo.Release();
                    ScreenSpaceInfo.Release();
                    ScreenSpaceInfoPrev.Release();
                    Gradients.Release();
                    _RandomNums.Release();
                    _RandomNumsB.Release();
                    #if UseOIDN
                        ColorBuffer.Release();
                        OutputBuffer.Release();
                        AlbedoBuffer.Release();
                        NormalBuffer.Release();
                        OIDNDenoiser.Dispose();
                    #endif
                }

                #if UseOIDN
                    UnityDenoiserPlugin.DenoiserConfig cfg = new UnityDenoiserPlugin.DenoiserConfig() {
                        imageWidth = SourceWidth,
                        imageHeight = SourceHeight,
                        guideAlbedo = 1,
                        guideNormal = 1,
                        temporalMode = 0,
                        cleanAux = 1,
                        prefilterAux = 0
                    };
                    OIDNDenoiser = new UnityDenoiserPlugin.DenoiserPluginWrapper(UnityDenoiserPlugin.DenoiserType.OIDN, cfg);
                    ColorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, SourceWidth * SourceHeight, 12);
                    OutputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, SourceWidth * SourceHeight, 12);
                    AlbedoBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, SourceWidth * SourceHeight, 12);
                    NormalBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, SourceWidth * SourceHeight, 12);
                #endif

                CommonFunctions.CreateRenderTexture(ref _RandomNums, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref _RandomNumsB, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref _DebugTex, SourceWidth, SourceHeight, CommonFunctions.RTFull4, RenderTextureReadWrite.sRGB);
                CommonFunctions.CreateRenderTexture(ref _FinalTex, TargetWidth, TargetHeight, CommonFunctions.RTFull4, RenderTextureReadWrite.sRGB, true);
                CommonFunctions.CreateRenderTexture(ref _target, SourceWidth, SourceHeight, CommonFunctions.RTHalf4, RenderTextureReadWrite.sRGB);
                CommonFunctions.CreateRenderTexture(ref _converged, SourceWidth, SourceHeight, CommonFunctions.RTFull4, RenderTextureReadWrite.sRGB);
                CommonFunctions.CreateRenderTexture(ref GIReservoirA, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
                CommonFunctions.CreateRenderTexture(ref GIReservoirB, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
                CommonFunctions.CreateRenderTexture(ref GINEEPosA, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
                CommonFunctions.CreateRenderTexture(ref GINEEPosB, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
                CommonFunctions.CreateRenderTexture(ref GIWorldPosA, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref GIWorldPosB, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref GIWorldPosC, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref _PrimaryTriangleInfo, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref ScreenSpaceInfo, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref ScreenSpaceInfoPrev, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref Gradients, SourceWidth / 3, SourceHeight / 3, CommonFunctions.RTHalf2);

                #if TTLightMapping
                    CommonFunctions.CreateRenderTexture(ref LightWorldIndex, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                #endif
                // Reset sampling
                _currentSample = 0;
                uFirstFrame = 1;
                FramesSinceStart = 0;
                FramesSinceStart2 = 0;
            }
        }
        public void ClearOutRenderTexture(RenderTexture renderTexture)
        {
            RenderTexture rt = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = rt;
        }

        
        private void GenerateRays(CommandBuffer cmd) {
            if (LocalTTSettings.UseASVGF && !LocalTTSettings.UseReSTIRGI) {
                cmd.BeginSample("ASVGF Reproject Pass");
                ASVGFCode.shader.SetTexture(1, "ScreenSpaceInfoWrite", (FramesSinceStart2 % 2 == 0) ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
                ASVGFCode.DoRNG(ref _RandomNums, ref _RandomNumsB, FramesSinceStart2, ref RaysBuffer, ref RaysBufferB, cmd, _PrimaryTriangleInfo, AssetManager.Assets.MeshDataBuffer, Assets.AggTriBuffer, MeshOrderChanged, Assets.TLASCWBVHIndexes);
                cmd.EndSample("ASVGF Reproject Pass");
            }

            SetInt("CurBounce", 0, cmd);
            if(LocalTTSettings.UseReSTIRGI && LocalTTSettings.ReSTIRGIUpdateRate != 0) {
                cmd.BeginSample("ReSTIR GI Reproject");
                cmd.DispatchCompute(GenerateShader, GIReTraceKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                cmd.EndSample("ReSTIR GI Reproject");
            } else {
                cmd.BeginSample("Primary Ray Generation");
                cmd.DispatchCompute(GenerateShader, (DoChainedImages ? GenPanoramaKernel : ((LocalTTSettings.UseASVGF && !LocalTTSettings.UseReSTIRGI) ? GenASVGFKernel : GenKernel)), Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                cmd.EndSample("Primary Ray Generation");
            }
        }

        private void Render(RenderTexture destination, CommandBuffer cmd)
        {
            #if !DisableRadianceCache
                cmd.BeginSample("RadCacheClear");
                    cmd.DispatchCompute(GenerateShader, ResolveKernel + 2, Mathf.CeilToInt((4.0f * 1024.0f * 1024.0f) / 256.0f), 1, 1);
                cmd.EndSample("RadCacheClear");
            #endif
            TTPostProc.ValidateInit(LocalTTSettings.PPBloom, LocalTTSettings.PPTAA, SourceWidth != TargetWidth, LocalTTSettings.UseTAAU, LocalTTSettings.DoSharpen, LocalTTSettings.PPFXAA);
            float CurrentSample;
            
            GenerateRays(cmd);

                cmd.BeginSample("Pathtracing Kernels");

                for (int i = 0; i < LocalTTSettings.bouncecount; i++) {
                    cmd.BeginSample("Bounce: " + i);
                        var bouncebounce = i;
                        if(bouncebounce == 1) {
                            cmd.SetComputeTextureParam(IntersectionShader, TraceKernel, "_PrimaryTriangleInfo", GIWorldPosA);
                            cmd.SetComputeTextureParam(IntersectionShader, HeightmapKernel, "_PrimaryTriangleInfo", GIWorldPosA);
                        }
                        SetInt("CurBounce", bouncebounce, cmd);
                        cmd.BeginSample("Transfer Kernel: " + i);
                        cmd.SetComputeIntParam(ShadingShader, "Type", 0);
                        cmd.DispatchCompute(ShadingShader, TransferKernel, 1, 1, 1);
                        cmd.EndSample("Transfer Kernel: " + i);

                        cmd.BeginSample("Trace Kernel: " + i);
                        #if DX11Only
                            cmd.DispatchCompute(IntersectionShader, TraceKernel, Mathf.CeilToInt((SourceHeight * SourceWidth) / 64.0f), 1, 1);
                        #else
                            cmd.DispatchCompute(IntersectionShader, TraceKernel, CurBounceInfoBuffer, 0);//784 is 28^2
                        #endif
                        cmd.EndSample("Trace Kernel: " + i);


                        if (Assets.Terrains.Count != 0) {
                            cmd.BeginSample("HeightMap Trace Kernel: " + i);
                            #if DX11Only
                                cmd.DispatchCompute(IntersectionShader, HeightmapKernel, Mathf.CeilToInt((SourceHeight * SourceWidth) / 64.0f), 1, 1);
                            #else
                                cmd.DispatchCompute(IntersectionShader, HeightmapKernel, CurBounceInfoBuffer, 0);//784 is 28^2
                            #endif
                            cmd.EndSample("HeightMap Trace Kernel: " + i);
                        }

                        cmd.BeginSample("Shading Kernel: " + i);
                        #if DX11Only
                            cmd.DispatchCompute(ShadingShader, ShadeKernel, Mathf.CeilToInt((SourceHeight * SourceWidth) / 64.0f), 1, 1);
                        #else
                            cmd.DispatchCompute(ShadingShader, ShadeKernel, CurBounceInfoBuffer, 0);
                        #endif
                        cmd.EndSample("Shading Kernel: " + i);



                        cmd.BeginSample("Transfer Kernel 2: " + i);
                        cmd.SetComputeIntParam(ShadingShader, "Type", 1);
                        cmd.DispatchCompute(ShadingShader, TransferKernel, 1, 1, 1);
                        cmd.EndSample("Transfer Kernel 2: " + i);
                        if (LocalTTSettings.UseNEE) {
                            cmd.BeginSample("Shadow Kernel: " + i);
                            #if DX11Only
                                cmd.DispatchCompute(IntersectionShader, ShadowKernel, Mathf.CeilToInt((SourceHeight * SourceWidth) / 64.0f), 1, 1);
                            #else
                                cmd.DispatchCompute(IntersectionShader, ShadowKernel, CurBounceInfoBuffer, 0);
                            #endif
                            cmd.EndSample("Shadow Kernel: " + i);
                        }
                        if (LocalTTSettings.UseNEE && Assets.Terrains.Count != 0) {
                            cmd.BeginSample("Heightmap Shadow Kernel: " + i);
                            #if DX11Only
                                cmd.DispatchCompute(IntersectionShader, HeightmapShadowKernel, Mathf.CeilToInt((SourceHeight * SourceWidth) / 64.0f), 1, 1);
                            #else
                                cmd.DispatchCompute(IntersectionShader, HeightmapShadowKernel, CurBounceInfoBuffer, 0);
                            #endif
                            cmd.EndSample("Heightmap Shadow Kernel: " + i);
                        }
                    cmd.EndSample("Bounce: " + i);

                }
            cmd.EndSample("Pathtracing Kernels");

            #if !DisableRadianceCache
                cmd.BeginSample("RadCache Resolve Kernel");
                    cmd.DispatchCompute(GenerateShader, ResolveKernel, Mathf.CeilToInt((4.0f * 1024.0f * 1024.0f) / 256.0f), 1, 1);
                cmd.EndSample("RadCache Resolve Kernel");    
                cmd.BeginSample("RadCache Copy Kernel");
                    cmd.DispatchCompute(GenerateShader, ResolveKernel + 1, Mathf.CeilToInt((4.0f * 1024.0f * 1024.0f) / 256.0f), 1, 1);
                cmd.EndSample("RadCache Copy Kernel");
            #endif


            if (LocalTTSettings.UseReSTIRGI) {
                SetInt("CurBounce", 0, cmd);
                cmd.BeginSample("ReSTIRGI Temporal Kernel");
                cmd.DispatchCompute(ReSTIRGI, ReSTIRGIKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                cmd.EndSample("ReSTIRGI Temporal Kernel");

                cmd.BeginSample("ReSTIRGI Extra Spatial Kernel");
                cmd.DispatchCompute(ReSTIRGI, ReSTIRGISpatialKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                cmd.EndSample("ReSTIRGI Extra Spatial Kernel");
            }

            if (!(!LocalTTSettings.UseReSTIRGI && LocalTTSettings.UseASVGF) && !(LocalTTSettings.UseReSTIRGI && LocalTTSettings.UseASVGF))
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
            } else if(!(LocalTTSettings.UseReSTIRGI && LocalTTSettings.UseASVGF)) {
                cmd.BeginSample("ASVGF");
                SampleCount = 0;
                ASVGFCode.Do(ref LightingBuffer, ref _converged, LocalTTSettings.RenderScale, ((FramesSinceStart2 % 2 == 0) ? ScreenSpaceInfo : ScreenSpaceInfoPrev), cmd, FramesSinceStart2, ref GIWorldPosA, LocalTTSettings.DoPartialRendering ? LocalTTSettings.PartialRenderingFactor : 1, TTPostProc.ExposureBuffer, LocalTTSettings.PPExposure, LocalTTSettings.IndirectBoost, (FramesSinceStart2 % 2 == 0) ? _RandomNums : _RandomNumsB);
                CurrentSample = 1;
                cmd.EndSample("ASVGF");
            } else if(LocalTTSettings.UseReSTIRGI && LocalTTSettings.UseASVGF) {
                cmd.BeginSample("ReSTIR ASVGF");
                SampleCount = 0;
                ReSTIRASVGFCode.Do(ref LightingBuffer,
                                    ref _converged, 
                                    LocalTTSettings.RenderScale, 
                                    ((FramesSinceStart2 % 2 == 0) ? ScreenSpaceInfo : ScreenSpaceInfoPrev), 
                                    ((FramesSinceStart2 % 2 == 1) ? ScreenSpaceInfo : ScreenSpaceInfoPrev), 
                                    cmd, 
                                    FramesSinceStart2, 
                                    ref GIWorldPosA, 
                                    LocalTTSettings.DoPartialRendering ? LocalTTSettings.PartialRenderingFactor : 1, 
                                    TTPostProc.ExposureBuffer, 
                                    LocalTTSettings.PPExposure, 
                                    LocalTTSettings.IndirectBoost, 
                                    Gradients,
                                    _PrimaryTriangleInfo, 
                                    AssetManager.Assets.MeshDataBuffer, 
                                    Assets.AggTriBuffer);
                CurrentSample = 1;
                cmd.EndSample("ReSTIR ASVGF");
            }
            cmd.BeginSample("Firefly Blit");
            if (_FireFlyMaterial == null)
                _FireFlyMaterial = new Material(Shader.Find("Hidden/FireFlyPass"));
            if(LocalTTSettings.DoFirefly && SampleCount > LocalTTSettings.FireflyFrameCount && (SampleCount - LocalTTSettings.FireflyFrameCount) % LocalTTSettings.FireflyFrameInterval == 0) {
                _FireFlyMaterial.SetFloat("_Strength", LocalTTSettings.FireflyStrength);
                _FireFlyMaterial.SetFloat("_Offset", LocalTTSettings.FireflyOffset);

                cmd.Blit(_converged, _target, _FireFlyMaterial);
                cmd.Blit(_target, _converged);
            }
            cmd.EndSample("Firefly Blit");


            cmd.BeginSample("Post Processing");
            if (SourceWidth != TargetWidth) {
                if (LocalTTSettings.UseTAAU) TTPostProc.ExecuteTAAU(ref _FinalTex, ref _converged, cmd, FramesSinceStart2);
                else TTPostProc.ExecuteUpsample(ref _converged, ref _FinalTex, FramesSinceStart2, _currentSample, cmd, ((FramesSinceStart2 % 2 == 0) ? ScreenSpaceInfo : ScreenSpaceInfoPrev));//This is a postprocessing pass, but im treating it like its not one, need to move it to after the accumulation
            }
            else cmd.CopyTexture(_converged, 0, 0, _FinalTex, 0, 0);

            if (LocalTTSettings.PPExposure) {
                _FinalTex.GenerateMips();
                TTPostProc.ExecuteAutoExpose(ref _FinalTex, LocalTTSettings.Exposure, cmd, LocalTTSettings.ExposureAuto);
            }

            #if UseOIDN
                if(LocalTTSettings.UseOIDN && SampleCount > LocalTTSettings.OIDNFrameCount) {
                    if(DoChainedImages) {
                        cmd.SetComputeBufferParam(ShadingShader, TTtoOIDNKernelPanorama, "OutputBuffer", ColorBuffer);
                        ShadingShader.SetTexture(TTtoOIDNKernelPanorama, "Result", _FinalTex);
                        cmd.DispatchCompute(ShadingShader, TTtoOIDNKernelPanorama, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                    } else {
                        cmd.SetComputeBufferParam(ShadingShader, TTtoOIDNKernel, "OutputBuffer", ColorBuffer);
                        ShadingShader.SetTexture(TTtoOIDNKernel, "Result", _FinalTex);
                        cmd.DispatchCompute(ShadingShader, TTtoOIDNKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                    }

                    OIDNDenoiser.Render(cmd, ColorBuffer, OutputBuffer, AlbedoBuffer, NormalBuffer);

                    cmd.SetComputeBufferParam(ShadingShader, OIDNtoTTKernel, "OutputBuffer", OutputBuffer);
                    ShadingShader.SetTexture(OIDNtoTTKernel, "Result", _FinalTex);
                    cmd.DispatchCompute(ShadingShader, OIDNtoTTKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);            
                }
            #endif

            if (LocalTTSettings.PPBloom) {
                if(LocalTTSettings.ConvBloom) TTPostProc.ExecuteConvBloom(ref _FinalTex, LocalTTSettings.ConvStrength, LocalTTSettings.ConvBloomThreshold, LocalTTSettings.ConvBloomSize, LocalTTSettings.ConvBloomDistExp, LocalTTSettings.ConvBloomDistExpClampMin, LocalTTSettings.ConvBloomDistExpScale, cmd);
                else TTPostProc.ExecuteBloom(ref _FinalTex, LocalTTSettings.BloomStrength, cmd);
            }
            if(LocalTTSettings.PPToneMap) TTPostProc.ExecuteToneMap(ref _FinalTex, cmd, ref ToneMapTex, ref ToneMapTex2, LocalTTSettings.ToneMapper);
            if (LocalTTSettings.PPTAA) TTPostProc.ExecuteTAA(ref _FinalTex, _currentSample, cmd);
            if (LocalTTSettings.PPFXAA) TTPostProc.ExecuteFXAA(ref _FinalTex, cmd);
            if (LocalTTSettings.DoSharpen) TTPostProc.ExecuteSharpen(ref _FinalTex, LocalTTSettings.Sharpness, cmd);
            cmd.Blit(_FinalTex, destination);
            ClearOutRenderTexture(_DebugTex);
            cmd.EndSample("Post Processing");
            _currentSample++;
            FramesSinceStart++;
            FramesSinceStart2++;
            PrevCamPosition = _camera.transform.position;
            PrevASVGF = LocalTTSettings.UseASVGF;
            PrevReSTIRGI = LocalTTSettings.UseReSTIRGI;
        }

        public void RenderImage(RenderTexture destination, CommandBuffer cmd)
        {
            Abandon = false;
            _camera.renderingPath = RenderingPath.DeferredShading;
            if (SceneIsRunning && Assets != null && Assets.RenderQue.Count > 0)
            {
                ResetAllTextures();
                RunUpdate();
                if(RebuildMeshObjectBuffers(cmd)) {
                    InitRenderTexture();
                    SetShaderParameters(cmd);
                    Render(destination, cmd);
                }
                uFirstFrame = 0;
            }
            else
            {
                try { int throwawayBool = AssetManager.Assets.UpdateTLAS(cmd); _meshObjectsNeedRebuilding = true;} catch (System.IndexOutOfRangeException) { }
            }
            SceneIsRunning = true;
        }

    }
}
