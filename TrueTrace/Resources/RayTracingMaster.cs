using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEditor;
#if UNITY_EDITOR
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif


namespace TrueTrace {
#if UNITY_PIPELINE_URP || UNITY_PIPELINE_HDRP
    [ExecuteInEditMode]
#endif
    public class RayTracingMaster : MonoBehaviour
    {
        public static RayTracingMaster RayMaster;
        [HideInInspector] public static Camera _camera;
        public static bool DoKernelProfiling = true;
        public bool EnableDebugTexture = false;
        [HideInInspector] public bool EnableDebugTexturePrev;
        [HideInInspector] [SerializeField] public string LocalTTSettingsName = "TTGlobalSettings";
        private bool OverriddenResolutionIsActive = false;
        public bool HDRPorURPRenderInScene = false;
        public bool OverrideAGXWithCustomTexture = false;
        [HideInInspector] public AtmosphereGenerator Atmo;
        [HideInInspector] public AssetManager Assets;
        private ReSTIRASVGF ReSTIRASVGFCode;
        private TTPostProcessing TTPostProc;
        public ASVGF ASVGFCode;
        public static bool ImageIsModified = false;
        #if UseOIDN
            private DenoiserPlugin.DenoiserPluginWrapper OIDNDenoiser;         
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
                    UseKelvin = OBJtoWrite.UseKelvin[Index],
                    KelvinTemp = OBJtoWrite.KelvinTemp[Index],
                    MatData = OBJtoWrite.LocalMaterials[Index]
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

#if EnablePhotonMapping
        public PhotonMapping PhotonMap;
        public RenderTexture FirstDiffuseThroughputTex;
        public RenderTexture FirstDiffusePosTex;
#endif

        private RenderTexture CorrectedDistanceTexA;
        private RenderTexture CorrectedDistanceTexB;

        private RenderTexture _target;
        private RenderTexture _converged;
        private RenderTexture _DebugTex;
        private RenderTexture _FinalTex;
        private RenderTexture _RandomNums;
        private RenderTexture _RandomNumsB;
        private RenderTexture _PrimaryTriangleInfoA;
        private RenderTexture _PrimaryTriangleInfoB;
#if MultiMapScreenshot
        [HideInInspector] public RenderTexture MultiMapMatIDTextureInitial;
        [HideInInspector] public RenderTexture MultiMapMatIDTexture;
        [HideInInspector] public RenderTexture MultiMapMeshIDTextureInitial;
        [HideInInspector] public RenderTexture MultiMapMeshIDTexture;
#endif
        private RenderTexture MVTexture;
        private RenderTexture GIReservoirA;
        private RenderTexture GIReservoirB;
        private RenderTexture GIReservoirC;


        private RenderTexture PSRGBuff;

        private RenderTexture GIWorldPosA;
        private RenderTexture GIWorldPosB;
        private RenderTexture GIWorldPosC;

        private RenderTexture GINEEPosA;
        private RenderTexture GINEEPosB;
        private RenderTexture GINEEPosC;

        private RenderTexture CDFX;
        private RenderTexture CDFY;

        private RenderTexture GradientsA;
        private RenderTexture GradientsB;
        public static TTSDFHandler OptionalSDFHandler;
        private bool HasSDFHandler = false;
        private bool ReSTIRInitialized = false;

        private bool OIDNGuideWrite;

        [HideInInspector] public RenderTexture ScreenSpaceInfo;
        [HideInInspector] public bool IsFocusing = false;
        [HideInInspector] public bool IsFocusingDelta = false;
        private RenderTexture ScreenSpaceInfoPrev;

        private ComputeBuffer _RayBuffer;
        private ComputeBuffer LightingBuffer;
        private ComputeBuffer _BufferSizes;
        private ComputeBuffer _ShadowBuffer;
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
        #endif

        private Texture3D ToneMapTex;
        private Texture3D ToneMapTex2;
        private Texture3D ToneMapTex3;
        [HideInInspector] public Texture3D AGXCustomTex;
        private Material _addMaterial;
        private Material _FireFlyMaterial;
        [HideInInspector] public int _currentSample = 0;
        private static bool _meshObjectsNeedRebuilding = false;
        public static List<RayTracingLights> _rayTracingLights = new List<RayTracingLights>();
        public static bool RTOShowBase = true;
        public static bool RTOShowEmission = false;
        public static bool RTOShowAdvanced = false;
        public static bool RTOShowColorModifiers = false;
        public static bool RTOShowTex = false;
        public static bool RTOShowFlag = false;


        [HideInInspector] public Vector2 CurrentHorizonalPatch;
        private float _lastFieldOfView;

        [HideInInspector] public int FramesSinceStart2;
        private BufferSizeData[] BufferSizes;
        [SerializeField] [HideInInspector] public static int SampleCount;

        private int uFirstFrame = 1;
        [HideInInspector] public static bool DoCheck = false;
        [HideInInspector] public bool PrevReSTIRGI = false;

        [HideInInspector] public bool DoPanorama = false;
        [HideInInspector] public bool DoChainedImages = false;

        [SerializeField] public TTSettings LocalTTSettings;

        public static bool SceneIsRunning = false;

        [HideInInspector] public Texture SkyboxTexture;
        [HideInInspector] public Texture SkyboxTextureB;
        private bool MeshOrderChanged = false;

        [HideInInspector] public int MainDirectionalLight = -1;
        [HideInInspector] public int AtmoNumLayers = 4;
        private float PrevResFactor;
        private int GenKernel;
        private int GenPanoramaKernel;
        private int TraceKernel;
        private int ShadowKernel;
        private int HeightmapKernel;
        private int HeightmapShadowKernel;
        private int ShadeKernel;
        private int FinalizeKernel;
        private int TransferKernel;
        private int ReSTIRGIKernel;
        private int ReSTIRGISpatialKernel;
        private int TTtoOIDNKernel;
        private int OIDNtoTTKernel;
        private int MVKernel;
        #if !DisableRadianceCache
            private int ResolveKernel;
            private int CompactKernel;
        #endif
        private int OverridenWidth = 1;
        private int OverridenHeight = 1;
        private int TargetWidth;
        private int TargetHeight;
        [HideInInspector] public int FramesSinceStart;
        [System.NonSerialized] public int SourceWidth;
        [System.NonSerialized] public int SourceHeight;
        private Vector3 PrevCamPosition;
        private bool PrevASVGF;


        [System.Serializable]
        public struct BufferSizeData
        {
            public int tracerays;
            public int shadow_rays;
            public int heightmap_rays;
            public int Heightmap_shadow_rays;
            public int TracedRays;
            public int TracedRaysShadow;
        }

        private void LoadInitialSettings() {//Loads settings from text file only in builds 
            if(AssetManager.Assets != null) {
                AssetManager.Assets.MainDesiredRes = LocalTTSettings.MainDesiredRes;
                AssetManager.Assets.UseSkinning = LocalTTSettings.UseSkinning;
            }
        }



        
        public void TossCamera(Camera camera) {
            _camera = camera; 
            if (!OverriddenResolutionIsActive) {                          
                CheckAndHandleResolutionChange(_camera.scaledPixelWidth, _camera.scaledPixelHeight);                
            }            
            OverriddenResolutionIsActive = false;
        }

        public void OverrideResolution(int ScreenWidth, int ScreenHeight) {
            CheckAndHandleResolutionChange(ScreenWidth, ScreenHeight);            
            OverriddenResolutionIsActive = true;
        }
        
        private void CheckAndHandleResolutionChange(int newWidth, int newHeight) {
            if (OverridenWidth != newWidth || OverridenHeight != newHeight) {
                SampleCount = 0;
                FramesSinceStart = 0;                   
                OverridenWidth = newWidth;
                OverridenHeight = newHeight;    
            }
        }
        unsafe public void Start2()
        {
            RayMaster = this;
            CurrentHorizonalPatch = new Vector2(0,1);
            LoadTT();
            ReSTIRInitialized = false;
            // LoadInitialSettings();//Build only
            Application.targetFrameRate = 165;
            ASVGFCode = new ASVGF();
            ReSTIRASVGFCode = new ReSTIRASVGF();
            ToneMapTex = Resources.Load<Texture3D>("Utility/ToneMapTex");
            ToneMapTex2 = Resources.Load<Texture3D>("Utility/AgXBC");
            ToneMapTex3 = Resources.Load<Texture3D>("Utility/AgXMHC");
            if (ShadingShader == null) {ShadingShader = Resources.Load<ComputeShader>("MainCompute/RayTracingShader"); }
            if (IntersectionShader == null) {IntersectionShader = Resources.Load<ComputeShader>("MainCompute/IntersectionKernels"); }
            if (GenerateShader == null) {GenerateShader = Resources.Load<ComputeShader>("MainCompute/RayGenKernels"); }
            if (ReSTIRGI == null) {ReSTIRGI = Resources.Load<ComputeShader>("MainCompute/ReSTIRGI"); }
            TargetWidth = 1;
            TargetHeight = 1;
            SourceWidth = 1;
#if EnablePhotonMapping
            if(PhotonMap == null || !PhotonMap.Initialized) {
                PhotonMap = new PhotonMapping();
                PhotonMap.Init();
            }
#endif
            SourceHeight = 1;
            PrevResFactor = LocalTTSettings.RenderScale;
            _meshObjectsNeedRebuilding = true;
            Assets = gameObject.GetComponent<AssetManager>();
            Assets.BuildCombined();
            uFirstFrame = 1;
            FramesSinceStart = 0;
            GenKernel = GenerateShader.FindKernel("Generate");
            GenPanoramaKernel = GenerateShader.FindKernel("GeneratePanorama");
            TraceKernel = IntersectionShader.FindKernel("kernel_trace");
            ShadowKernel = IntersectionShader.FindKernel("kernel_shadow");
            ShadeKernel = ShadingShader.FindKernel("kernel_shade");
            FinalizeKernel = ShadingShader.FindKernel("kernel_finalize");
            HeightmapShadowKernel = IntersectionShader.FindKernel("kernel_shadow_heightmap");
            HeightmapKernel = IntersectionShader.FindKernel("kernel_heightmap");
            TransferKernel = ShadingShader.FindKernel("TransferKernel");
            ReSTIRGIKernel = ReSTIRGI.FindKernel("ReSTIRGIKernel");
            ReSTIRGISpatialKernel = ReSTIRGI.FindKernel("ReSTIRGISpatial");
            TTtoOIDNKernel = ShadingShader.FindKernel("TTtoOIDNKernel");
            OIDNtoTTKernel = ShadingShader.FindKernel("OIDNtoTTKernel");
            MVKernel = ShadingShader.FindKernel("MVKernel");
            #if !DisableRadianceCache
                ResolveKernel = GenerateShader.FindKernel("CacheResolve");
                CompactKernel = GenerateShader.FindKernel("CacheCompact");
            #endif
            OIDNGuideWrite = false;
            ASVGFCode.Initialized = false;
            ReSTIRASVGFCode.Initialized = false;

            Atmo = new AtmosphereGenerator(6360, 6420, AtmoNumLayers);
            FramesSinceStart2 = 0;
            TTPostProc = new TTPostProcessing();
            TTPostProc.Initialized = false;
        }
        public void Awake() {
            RayMaster = this;
            LoadTT();
        }
        public void Start() {
            RayMaster = this;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            DoPanorama = false;
            DoChainedImages = false;
            LoadTT();
        }

        void Reset() {
            LoadTT();
        }
        public void LoadTT() {
            #if !LoadTTSettingsFromResources
                if(LocalTTSettings == null || (!LocalTTSettings.name.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name) && !ImageIsModified)) {
                    #if UNITY_EDITOR
                        UnityEngine.SceneManagement.Scene CurrentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                        string path = CurrentScene.path.Replace(".unity", "");
                        LocalTTSettings = UnityEditor.AssetDatabase.LoadAssetAtPath(path + ".asset", typeof(TTSettings)) as TTSettings;
                        if(LocalTTSettings == null) {
                            LocalTTSettings = ScriptableObject.CreateInstance<TTSettings>();
                            UnityEditor.AssetDatabase.CreateAsset(LocalTTSettings, path + ".asset");
                            UnityEditor.AssetDatabase.SaveAssets();
                        }
                    #else 
                        LocalTTSettings = Resources.Load<TTSettings>(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                        if(LocalTTSettings == null)
                            LocalTTSettings = ScriptableObject.CreateInstance<TTSettings>();
                    #endif
                }
                #if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(LocalTTSettings);
                #endif
                if(LocalTTSettings != null) {
                    LoadInitialSettings();
                }
            #else
                if(LocalTTSettings == null) {
                    LocalTTSettings = Resources.Load<TTSettings>("Utility/TTSettingsStorage/" + LocalTTSettingsName);
                    if(LocalTTSettings == null) {
                        #if UNITY_EDITOR
                            string path = "Assets/TrueTrace-Unity-Pathtracer/TrueTrace/Resources/Utility/TTSettingsStorage/" + LocalTTSettingsName;
                            LocalTTSettings = ScriptableObject.CreateInstance<TTSettings>();
                            UnityEditor.AssetDatabase.CreateAsset(LocalTTSettings, path + ".asset");
                            UnityEditor.AssetDatabase.SaveAssets();
                        #else 
                            LocalTTSettings = ScriptableObject.CreateInstance<TTSettings>();
                        #endif
                    }
                } else {
                    if(!LocalTTSettings.name.Equals(LocalTTSettingsName)) {
                        LocalTTSettingsName = LocalTTSettings.name;
                    }
                }
                #if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(LocalTTSettings);
                #endif
            #endif
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



        void OnSceneUnloaded(Scene scene) {
            ClearAll();
        }

        public void ClearAll() {
            DoCheck = true;
            ReSTIRInitialized = false;
            if(TTPostProc != null) TTPostProc.ClearAll();
            _RayBuffer.ReleaseSafe();
            LightingBuffer.ReleaseSafe();
            _BufferSizes.ReleaseSafe();
            _ShadowBuffer.ReleaseSafe();
            if(ASVGFCode != null) ASVGFCode.ClearAll();
            _RandomNumsB.ReleaseSafe();
            if(ReSTIRASVGFCode != null) ReSTIRASVGFCode.ClearAll();
            CurBounceInfoBuffer.ReleaseSafe();
            Atmo.Dispose();
            CDFX.ReleaseSafe();
            CDFY.ReleaseSafe();
            CDFTotalBuffer.ReleaseSafe();
            #if !DisableRadianceCache
                CacheBuffer.ReleaseSafe();
                VoxelDataBufferA.ReleaseSafe();
                VoxelDataBufferB.ReleaseSafe();
            #endif

            _RandomNums.ReleaseSafe();
            _target.ReleaseSafe();
            _converged.ReleaseSafe();
            _DebugTex.ReleaseSafe();
            _FinalTex.ReleaseSafe();
            GIReservoirA.ReleaseSafe();
            GIReservoirB.ReleaseSafe();
            GIReservoirC.ReleaseSafe();
            GINEEPosA.ReleaseSafe();
            GINEEPosB.ReleaseSafe();
            GINEEPosC.ReleaseSafe();
            GIWorldPosA.ReleaseSafe();
            GIWorldPosB.ReleaseSafe();
            GIWorldPosC.ReleaseSafe();
            _PrimaryTriangleInfoA.ReleaseSafe();
            _PrimaryTriangleInfoB.ReleaseSafe();
#if MultiMapScreenshot
            MultiMapMatIDTexture.ReleaseSafe();
            MultiMapMatIDTextureInitial.ReleaseSafe();
            MultiMapMeshIDTexture.ReleaseSafe();
            MultiMapMeshIDTextureInitial.ReleaseSafe();
#endif
            MVTexture.ReleaseSafe();
            ScreenSpaceInfo.ReleaseSafe();
            ScreenSpaceInfoPrev.ReleaseSafe();
            GradientsA.ReleaseSafe();
            GradientsB.ReleaseSafe();
            CorrectedDistanceTexA.ReleaseSafe();
            CorrectedDistanceTexB.ReleaseSafe();
#if EnablePhotonMapping
            FirstDiffuseThroughputTex.ReleaseSafe();
            FirstDiffusePosTex.ReleaseSafe();
            if(PhotonMap != null) PhotonMap.ClearAll();
#endif
            #if UseOIDN
                ColorBuffer.ReleaseSafe();
                OutputBuffer.ReleaseSafe();
                AlbedoBuffer.ReleaseSafe();
                NormalBuffer.ReleaseSafe();
                if(OIDNDenoiser != null) {
                    OIDNDenoiser.Dispose();
                    OIDNDenoiser = null;
                }
            #endif       
        }
        public void OnDisable() {
            ClearAll();
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void RunUpdate() {
            ShadingShader.SetVector("SunDir", Assets.SunDirection);
            if (!LocalTTSettings.Accumulate || IsFocusing || IsFocusingDelta) {
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
            if(DoKernelProfiling) cmd.BeginSample("Full Update");
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
            if(DoKernelProfiling) cmd.EndSample("Full Update");
            if (!_meshObjectsNeedRebuilding) return true;
            _meshObjectsNeedRebuilding = false;
            FramesSinceStart = 0;


            if(CurBounceInfoBuffer != null) CurBounceInfoBuffer.ReleaseSafe();
            CurBounceInfoBuffer = new ComputeBuffer(1, 12);
            if(_RayBuffer == null || _RayBuffer.count != SourceWidth * SourceHeight) {
                CommonFunctions.CreateDynamicBuffer(ref _RayBuffer, SourceWidth * SourceHeight * 2, 48);
                CommonFunctions.CreateDynamicBuffer(ref _ShadowBuffer, SourceWidth * SourceHeight, 48);
                CommonFunctions.CreateDynamicBuffer(ref LightingBuffer, SourceWidth * SourceHeight, 64);
            }
            return true;
        }

       
        private void SetMatrix(string Name, Matrix4x4 Mat) {
            ShadingShader.SetMatrix(Name, Mat);
            IntersectionShader.SetMatrix(Name, Mat);
            GenerateShader.SetMatrix(Name, Mat);
            ReSTIRGI.SetMatrix(Name, Mat);
#if EnablePhotonMapping
            PhotonMap.SPPMShader.SetMatrix(Name, Mat);
#endif
            if(HasSDFHandler) OptionalSDFHandler.GenShader.SetMatrix(Name, Mat);
        }

        private void SetVector(string Name, Vector3 IN, CommandBuffer cmd) {
            cmd.SetComputeVectorParam(ShadingShader, Name, IN);
            cmd.SetComputeVectorParam(IntersectionShader, Name, IN);
            cmd.SetComputeVectorParam(GenerateShader, Name, IN);
            cmd.SetComputeVectorParam(ReSTIRGI, Name, IN);
#if EnablePhotonMapping
            cmd.SetComputeVectorParam(PhotonMap.SPPMShader, Name, IN);
#endif
            if(HasSDFHandler) cmd.SetComputeVectorParam(OptionalSDFHandler.GenShader, Name, IN);
        }

        private void SetInt(string Name, int IN, CommandBuffer cmd) {
            cmd.SetComputeIntParam(ShadingShader, Name, IN);
            cmd.SetComputeIntParam(IntersectionShader, Name, IN);
            cmd.SetComputeIntParam(GenerateShader, Name, IN);
            cmd.SetComputeIntParam(ReSTIRGI, Name, IN);
#if EnablePhotonMapping
            cmd.SetComputeIntParam(PhotonMap.SPPMShader, Name, IN);
#endif
            if(HasSDFHandler) cmd.SetComputeIntParam(OptionalSDFHandler.GenShader, Name, IN);
        }

        private void SetFloat(string Name, float IN, CommandBuffer cmd) {
            cmd.SetComputeFloatParam(ShadingShader, Name, IN);
            cmd.SetComputeFloatParam(IntersectionShader, Name, IN);
            cmd.SetComputeFloatParam(GenerateShader, Name, IN);
            cmd.SetComputeFloatParam(ReSTIRGI, Name, IN);
#if EnablePhotonMapping
            cmd.SetComputeFloatParam(PhotonMap.SPPMShader, Name, IN);
#endif
            if(HasSDFHandler) cmd.SetComputeFloatParam(OptionalSDFHandler.GenShader, Name, IN);
        }

        private void SetBool(string Name, bool IN) {
            ShadingShader.SetBool(Name, IN);
            IntersectionShader.SetBool(Name, IN);
            GenerateShader.SetBool(Name, IN);
            ReSTIRGI.SetBool(Name, IN);
#if EnablePhotonMapping
            PhotonMap.SPPMShader.SetBool(Name, IN);
#endif
            if(HasSDFHandler) OptionalSDFHandler.GenShader.SetBool(Name, IN);
        }
        Matrix4x4 CamInvProjPrev;
        Matrix4x4 CamToWorldPrev;
        Vector3 PrevPos;

        [HideInInspector] public Vector2 projectionScale = Vector2.one;
        Matrix4x4 CalcProj(Camera cam) {
            float Aspect = OverridenWidth / (float)OverridenHeight;
            float YFOV = 1.0f / Mathf.Tan(cam.fieldOfView / (2.0f * (360.0f / (2.0f * 3.14159f))));
            float XFOV = YFOV / Aspect;
            Matrix4x4 TempProj = cam.projectionMatrix;
            TempProj[0,0] = XFOV * projectionScale.x;
            TempProj[1,1] = YFOV * projectionScale.y;
            return TempProj;
        }
        Matrix4x4 TTviewprojection;
        private Vector2 HDRIParams = Vector2.zero;
        private void SetShaderParameters(CommandBuffer cmd) {
            HasSDFHandler = (OptionalSDFHandler != null) && OptionalSDFHandler.enabled && OptionalSDFHandler.gameObject.activeInHierarchy;
            if(LocalTTSettings.RenderScale != 1.0f && LocalTTSettings.UpscalerMethod != 0) {
                _camera.renderingPath = RenderingPath.DeferredShading;
                _camera.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
            }
            if(LocalTTSettings.UseReSTIRGI && LocalTTSettings.DenoiserMethod == 1 && !ReSTIRASVGFCode.Initialized) ReSTIRASVGFCode.init(SourceWidth, SourceHeight);
            else if ((LocalTTSettings.DenoiserMethod != 1 || !LocalTTSettings.UseReSTIRGI) && ReSTIRASVGFCode.Initialized) ReSTIRASVGFCode.ClearAll();
            if (!LocalTTSettings.UseReSTIRGI && LocalTTSettings.DenoiserMethod == 1 && !ASVGFCode.Initialized) {
                ASVGFCode.init(SourceWidth, SourceHeight, OverridenWidth, OverridenHeight);
                CommonFunctions.CreateRenderTexture(ref _RandomNumsB, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
            } else if ((LocalTTSettings.DenoiserMethod != 1 || LocalTTSettings.UseReSTIRGI) && ASVGFCode.Initialized) {
                ASVGFCode.ClearAll();
                _RandomNumsB.ReleaseSafe();
            }
            if(TTPostProc.Initialized == false) TTPostProc.init(SourceWidth, SourceHeight);

            if(BufferSizes == null || BufferSizes.Length != LocalTTSettings.bouncecount + 2) BufferSizes = new BufferSizeData[LocalTTSettings.bouncecount + 2];
            for(int i = 0; i < LocalTTSettings.bouncecount + 2; i++) {
                BufferSizes[i].tracerays = 0;
                BufferSizes[i].shadow_rays = 0;
                BufferSizes[i].heightmap_rays = 0;
                BufferSizes[i].Heightmap_shadow_rays = 0;     
                BufferSizes[i].TracedRays = 0;     
                BufferSizes[i].TracedRaysShadow = 0;     
            }
            BufferSizes[0].tracerays = SourceWidth * SourceHeight;
            BufferSizes[0].heightmap_rays = SourceWidth * SourceHeight;
            if(_BufferSizes == null || !_BufferSizes.IsValid()) {
                _BufferSizes = new ComputeBuffer(LocalTTSettings.bouncecount + 2, 24);
            }
            if(_BufferSizes.count != LocalTTSettings.bouncecount + 2) {
                _BufferSizes.ReleaseSafe();
                _BufferSizes = new ComputeBuffer(LocalTTSettings.bouncecount + 2, 24);
            }
            cmd.SetBufferData(_BufferSizes, BufferSizes);
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
            Matrix4x4 ProjectionMatrix = CalcProj(_camera);

            CamInvProjPrev = ProjectionMatrix.inverse;
            CamToWorldPrev = _camera.cameraToWorldMatrix;
            TTviewprojection = ProjectionMatrix * _camera.worldToCameraMatrix;
            SetMatrix("viewprojection", ProjectionMatrix * _camera.worldToCameraMatrix);
            SetMatrix("prevviewprojection", EB.inverse * EA.inverse);
            SetMatrix("CamInvProj", ProjectionMatrix.inverse);
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
            SetVector("PrimaryBackgroundTintColor", LocalTTSettings.PrimaryBackgroundTintColor, cmd);
            SetVector("BackgroundColor", LocalTTSettings.SceneBackgroundColor, cmd);
            SetVector("SecondaryBackgroundColor", LocalTTSettings.SecondarySceneBackgroundColor, cmd);
            SetVector("ClayColor", LocalTTSettings.ClayColor, cmd);
            SetVector("GroundColor", LocalTTSettings.GroundColor, cmd);
            SetVector("Segment", CurrentHorizonalPatch, cmd);
            SetVector("HDRILongLat", LocalTTSettings.HDRILongLat, cmd);
            SetVector("HDRIScale", LocalTTSettings.HDRIScale, cmd);
            SetVector("MousePos", Input.mousePosition, cmd);
            SetVector("FogColor", LocalTTSettings.FogColor, cmd);
            if(LocalTTSettings.DenoiserMethod == 1 && !LocalTTSettings.UseReSTIRGI) ASVGFCode.shader.SetVector("CamDelta", E);
            if(LocalTTSettings.DenoiserMethod == 1 && LocalTTSettings.UseReSTIRGI) ReSTIRASVGFCode.shader.SetVector("CamDelta", E);

            Shader.SetGlobalInt("PartialRenderingFactor", LocalTTSettings.PartialRenderingFactor);
            SetFloat("FarPlane", _camera.farClipPlane, cmd);
            SetFloat("NearPlane", _camera.nearClipPlane, cmd);
            SetFloat("PrimaryBackgroundTint", LocalTTSettings.PrimaryBackgroundTint, cmd);
            SetFloat("PrimaryBackgroundContrast", LocalTTSettings.PrimaryBackgroundContrast, cmd);
            SetFloat("focal_distance", LocalTTSettings.DoFFocal, cmd);
            SetFloat("AperatureRadius", LocalTTSettings.DoFAperature * LocalTTSettings.DoFAperatureScale, cmd);
            SetFloat("IndirectBoost", LocalTTSettings.IndirectBoost, cmd);
            SetFloat("SkyDesaturate", LocalTTSettings.SkyDesaturate, cmd);
            SetFloat("SecondarySkyDesaturate", LocalTTSettings.SecondarySkyDesaturate, cmd);
            SetFloat("BackgroundIntensity", LocalTTSettings.BackgroundIntensity.x, cmd);
            SetFloat("SecondaryBackgroundIntensity", LocalTTSettings.BackgroundIntensity.y, cmd);
            SetFloat("LEMEnergyScale", LocalTTSettings.LEMEnergyScale, cmd);
            SetFloat("OIDNBlendRatio", LocalTTSettings.OIDNBlendRatio, cmd);
            SetFloat("FogDensity", LocalTTSettings.FogDensity, cmd);
            SetFloat("ScaleHeight", LocalTTSettings.FogHeight, cmd);
            SetFloat("OrthoSize", _camera.orthographicSize, cmd);
            SetFloat("LightEnergyScale", LocalTTSettings.LightEnergyScale, cmd);
            SetFloat("ClayMetalOverrideValue", LocalTTSettings.ClayMetalOverride, cmd);
            SetFloat("ClayRoughnessOverrideValue", LocalTTSettings.ClayRoughnessOverride, cmd);
            SetFloat("aoStrength", LocalTTSettings.aoStrength, cmd);
            SetFloat("aoRadius", LocalTTSettings.aoRadius, cmd);
            SetInt("LightTreePrimaryTLASOffset", AssetManager.Assets.LightTreePrimaryTLASOffset, cmd);
            SetInt("MainDirectionalLight", MainDirectionalLight, cmd);
            SetInt("NonInstanceCount", Assets.NonInstanceCount, cmd);
            SetInt("AlbedoAtlasSize", Assets.AlbedoAtlasSize, cmd);
            SetInt("LightMeshCount", Assets.LightMeshCount, cmd);
            SetInt("unitylightcount", Assets.UnityLightCount, cmd);
            SetInt("screen_width", SourceWidth, cmd);
            SetInt("screen_height", SourceHeight, cmd);
            SetInt("MaxBounce", LocalTTSettings.bouncecount, cmd);
            SetInt("frames_accumulated", _currentSample, cmd);
            SetInt("ReSTIRGITemporalMCap", LocalTTSettings.ReSTIRGITemporalMCap, cmd);
            SetInt("curframe", FramesSinceStart2, cmd);
            SetInt("TerrainCount", Assets.Terrains.Count, cmd);
            SetInt("RISCount", LocalTTSettings.RISCount, cmd);
            SetInt("BackgroundType", LocalTTSettings.BackgroundType, cmd);
            SetInt("SecondaryBackgroundType", LocalTTSettings.SecondaryBackgroundType, cmd);
            SetInt("MaterialCount", Assets.MatCount, cmd);
            SetInt("PartialRenderingFactor", LocalTTSettings.DoPartialRendering ? LocalTTSettings.PartialRenderingFactor : 1, cmd);
            SetInt("UpscalerMethod", LocalTTSettings.UpscalerMethod, cmd);

            SetBool("IsOrtho", _camera.orthographic);
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
            SetBool("UseASVGF", LocalTTSettings.DenoiserMethod == 1 && !LocalTTSettings.UseReSTIRGI);
            SetBool("UseASVGFAndReSTIR", LocalTTSettings.DenoiserMethod == 1 && LocalTTSettings.UseReSTIRGI);
            SetBool("TerrainExists", Assets.Terrains.Count != 0);
            SetBool("DoPartialRendering", LocalTTSettings.DoPartialRendering);
            SetBool("UseTransmittanceInNEE", LocalTTSettings.UseTransmittanceInNEE);
            OIDNGuideWrite = (FramesSinceStart == LocalTTSettings.OIDNFrameCount);
            SetBool("OIDNGuideWrite", OIDNGuideWrite && (LocalTTSettings.DenoiserMethod == 2 || LocalTTSettings.DenoiserMethod == 3));
            SetBool("DiffRes", LocalTTSettings.RenderScale != 1.0f);
            SetBool("DoPartialRendering", LocalTTSettings.DoPartialRendering);
            SetBool("DoExposure", LocalTTSettings.PPExposure);
            ShadingShader.SetBuffer(ShadeKernel, "Exposure", TTPostProc.ExposureBuffer);

            bool FlipFrame = (FramesSinceStart2 % 2 == 0);

            ReSTIRGI.SetTextureFromGlobal(ReSTIRGIKernel, "MotionVectors", "TTMotionVectorTexture");


            if (SkyboxTexture == null) SkyboxTexture = new Texture2D(1,1, TextureFormat.RGBA32, false);
            if (SkyboxTexture != null)
            {
                if(CDFTotalBuffer == null || !CDFTotalBuffer.IsValid()) {
                    CDFTotalBuffer = new ComputeBuffer(1, 4);
                }
                if((LocalTTSettings.BackgroundType == 1 || LocalTTSettings.SecondaryBackgroundType == 1) && (CDFX == null || SkyboxTexture != SkyboxTextureB)) {
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
                    CounterBuffer.ReleaseSafe();
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
            if((LocalTTSettings.BackgroundType == 1 || LocalTTSettings.SecondaryBackgroundType == 1)) SkyboxTextureB = SkyboxTexture;
            SetVector("HDRIParams", HDRIParams, cmd);
            bool UseBaseASVGF = LocalTTSettings.DenoiserMethod == 1 && !LocalTTSettings.UseReSTIRGI;

            if(UseBaseASVGF)
                GenerateShader.SetTexture(GenKernel, "RandomNums", (FramesSinceStart2 % 2 == 0) ? _RandomNums : _RandomNumsB);
            else
                GenerateShader.SetTexture(GenKernel, "RandomNums", _RandomNums);
            GenerateShader.SetComputeBuffer(GenKernel, "GlobalRays", _RayBuffer);
            if(UseBaseASVGF)
                GenerateShader.SetTexture(GenPanoramaKernel, "RandomNums", (FramesSinceStart2 % 2 == 0) ? _RandomNums : _RandomNumsB);
            else
                GenerateShader.SetTexture(GenPanoramaKernel, "RandomNums", _RandomNums);
            GenerateShader.SetComputeBuffer(GenPanoramaKernel, "GlobalRays", _RayBuffer);

            AssetManager.Assets.SetMeshTraceBuffers(IntersectionShader, TraceKernel);
            IntersectionShader.SetComputeBuffer(TraceKernel, "GlobalRays", _RayBuffer);
            IntersectionShader.SetComputeBuffer(TraceKernel, "GlobalColors", LightingBuffer);
            IntersectionShader.SetTexture(TraceKernel, "_PrimaryTriangleInfo", (FramesSinceStart2 % 2 == 0) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);


            AssetManager.Assets.SetMeshTraceBuffers(IntersectionShader, ShadowKernel);
            IntersectionShader.SetComputeBuffer(ShadowKernel, "ShadowRaysBuffer", _ShadowBuffer);
            IntersectionShader.SetComputeBuffer(ShadowKernel, "GlobalColors", LightingBuffer);
            if(LocalTTSettings.UseReSTIRGI && ReSTIRInitialized) {
                IntersectionShader.SetTexture(ShadowKernel, "NEEPosA", FlipFrame ? GINEEPosA : GINEEPosB);
                IntersectionShader.SetTexture(HeightmapShadowKernel, "NEEPosA", FlipFrame ? GINEEPosA : GINEEPosB);
            } else {
                IntersectionShader.SetTexture(ShadowKernel, "NEEPosA", GINEEPosA);
                IntersectionShader.SetTexture(HeightmapShadowKernel, "NEEPosA", GINEEPosA);
            }
            if(UseBaseASVGF)
                IntersectionShader.SetTexture(TraceKernel, "RandomNums", FlipFrame ? _RandomNums : _RandomNumsB);
            else
                IntersectionShader.SetTexture(TraceKernel, "RandomNums", _RandomNums);


            AssetManager.Assets.SetHeightmapTraceBuffers(IntersectionShader, HeightmapKernel);
            IntersectionShader.SetComputeBuffer(HeightmapKernel, "GlobalRays", _RayBuffer);
            IntersectionShader.SetTexture(HeightmapKernel, "_PrimaryTriangleInfo", (FramesSinceStart2 % 2 == 0) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);

            AssetManager.Assets.SetHeightmapTraceBuffers(IntersectionShader, HeightmapShadowKernel);
            IntersectionShader.SetComputeBuffer(HeightmapShadowKernel, "GlobalColors", LightingBuffer);
            IntersectionShader.SetComputeBuffer(HeightmapShadowKernel, "ShadowRaysBuffer", _ShadowBuffer);

            #if !DisableRadianceCache
                GenerateShader.SetComputeBuffer(ResolveKernel, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                GenerateShader.SetComputeBuffer(ResolveKernel, "VoxelDataBufferB", !FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                GenerateShader.SetComputeBuffer(CompactKernel, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);



                IntersectionShader.SetComputeBuffer(ShadowKernel, "CacheBuffer", CacheBuffer);
                IntersectionShader.SetComputeBuffer(ShadowKernel, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);

                IntersectionShader.SetComputeBuffer(HeightmapShadowKernel, "CacheBuffer", CacheBuffer);
                IntersectionShader.SetComputeBuffer(HeightmapShadowKernel, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                
                ShadingShader.SetComputeBuffer(ShadeKernel, "CacheBuffer", CacheBuffer);
                ShadingShader.SetComputeBuffer(ShadeKernel, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                ShadingShader.SetComputeBuffer(ShadeKernel, "VoxelDataBufferB", !FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                
                ShadingShader.SetComputeBuffer(FinalizeKernel, "CacheBuffer", CacheBuffer);
                ShadingShader.SetComputeBuffer(FinalizeKernel, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                ShadingShader.SetComputeBuffer(FinalizeKernel, "VoxelDataBufferB", !FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
            #endif

            AssetManager.Assets.SetMeshTraceBuffers(ShadingShader, MVKernel);
            ShadingShader.SetTexture(MVKernel, "PrimaryTriData", (FramesSinceStart2 % 2 == 0) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);
            ShadingShader.SetTexture(MVKernel, "PrimaryTriDataPrev", (FramesSinceStart2 % 2 == 1) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);
            ShadingShader.SetTexture(MVKernel, "MVTexture", MVTexture);
            ShadingShader.SetTexture(MVKernel, "CorrectedDistanceTex", (FramesSinceStart2 % 2 == 0) ? CorrectedDistanceTexA : CorrectedDistanceTexB);

            AssetManager.Assets.SetMeshTraceBuffers(ShadingShader, MVKernel+2);
            ShadingShader.SetTexture(MVKernel+2, "PrimaryTriData", (FramesSinceStart2 % 2 == 0) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);
            ShadingShader.SetTexture(MVKernel+2, "PrimaryTriDataPrev", (FramesSinceStart2 % 2 == 1) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);
            ShadingShader.SetTexture(MVKernel+2, "MVTexture", MVTexture);
            ShadingShader.SetTexture(MVKernel+2, "PSRGBuff", PSRGBuff);
            ShadingShader.SetTexture(MVKernel+2, "CorrectedDistanceTex", (FramesSinceStart2 % 2 == 0) ? CorrectedDistanceTexA : CorrectedDistanceTexB);

            ShadingShader.SetTexture(MVKernel + 1, "MVTexture", MVTexture);
            ShadingShader.SetTexture(MVKernel+1, "CorrectedDistanceTex", (FramesSinceStart2 % 2 == 0) ? CorrectedDistanceTexA : CorrectedDistanceTexB);
            ShadingShader.SetComputeBuffer(MVKernel, "GlobalColors", LightingBuffer);
            ShadingShader.SetComputeBuffer(MVKernel + 2, "GlobalColors", LightingBuffer);
            ShadingShader.SetTexture(MVKernel + 2, "ScreenSpaceInfo", FlipFrame ? ScreenSpaceInfo : ScreenSpaceInfoPrev);

            Atmo.AssignTextures(ShadingShader, ShadeKernel);
            AssetManager.Assets.SetLightData(ShadingShader, ShadeKernel);
            AssetManager.Assets.SetMeshTraceBuffers(ShadingShader, ShadeKernel);
            AssetManager.Assets.SetHeightmapTraceBuffers(ShadingShader, ShadeKernel);
            // ShadingShader.SetTexture(ShadeKernel, "CloudShapeTex", Atmo.CloudShapeTex);
            // ShadingShader.SetTexture(ShadeKernel, "CloudShapeDetailTex", Atmo.CloudShapeDetailTex);
            // ShadingShader.SetTexture(ShadeKernel, "localWeatherTexture", Atmo.WeatherTex);
            if(UseBaseASVGF)
                ShadingShader.SetTexture(ShadeKernel, "RandomNums", FlipFrame ? _RandomNums : _RandomNumsB);
            else
                ShadingShader.SetTexture(ShadeKernel, "RandomNums", _RandomNums);
            ShadingShader.SetTexture(ShadeKernel, "SingleComponentAtlas", Assets.SingleComponentAtlas);
            ShadingShader.SetTexture(ShadeKernel, "_EmissiveAtlas", Assets.EmissiveAtlas);
            ShadingShader.SetTexture(ShadeKernel, "_NormalAtlas", Assets.NormalAtlas);
            ShadingShader.SetTexture(ShadeKernel, "ScreenSpaceInfo", FlipFrame ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
            ShadingShader.SetComputeBuffer(ShadeKernel, "GlobalColors", LightingBuffer);
            ShadingShader.SetComputeBuffer(ShadeKernel, "GlobalRays", _RayBuffer);
            ShadingShader.SetComputeBuffer(ShadeKernel, "ShadowRaysBuffer", _ShadowBuffer);
            ShadingShader.SetTexture(ShadeKernel, "PSRGBuff", PSRGBuff);

#if MultiMapScreenshot
            ShadingShader.SetTexture(ShadeKernel, "MultiMapMatIDTexture", MultiMapMatIDTextureInitial);
            ShadingShader.SetTexture(ShadeKernel, "MultiMapMeshIDTexture", MultiMapMeshIDTextureInitial);
#endif
#if EnablePhotonMapping
            ShadingShader.SetTexture(ShadeKernel, "FirstDiffuseThroughputTex", FirstDiffuseThroughputTex);
            ShadingShader.SetTexture(ShadeKernel, "FirstDiffusePosTex", FirstDiffusePosTex);
#endif


            ShadingShader.SetBuffer(FinalizeKernel, "GlobalColors", LightingBuffer);
            ShadingShader.SetTexture(FinalizeKernel, "Result", _target);
            

            #if UseOIDN
                ShadingShader.SetBuffer(TTtoOIDNKernel, "AlbedoBuffer", AlbedoBuffer);
                ShadingShader.SetBuffer(TTtoOIDNKernel, "NormalBuffer", NormalBuffer);
                ShadingShader.SetTexture(TTtoOIDNKernel, "ScreenSpaceInfo", ScreenSpaceInfo);
                ShadingShader.SetComputeBuffer(TTtoOIDNKernel, "GlobalColors", LightingBuffer);
            #endif


            
            if(LocalTTSettings.UseReSTIRGI && ReSTIRInitialized) {
                AssetManager.Assets.SetMeshTraceBuffers(ReSTIRGI, ReSTIRGIKernel);
                ReSTIRGI.SetTexture(ReSTIRGIKernel, "ReservoirA", FlipFrame ? GIReservoirB : GIReservoirA);
                ReSTIRGI.SetTexture(ReSTIRGIKernel, "ReservoirB", !FlipFrame ? GIReservoirB : GIReservoirA);
                ReSTIRGI.SetTexture(ReSTIRGIKernel, "WorldPosC", GIWorldPosA);
                ReSTIRGI.SetTexture(ReSTIRGIKernel, "WorldPosA", FlipFrame ? GIWorldPosB : GIWorldPosC);
                ReSTIRGI.SetTexture(ReSTIRGIKernel, "WorldPosB", !FlipFrame ? GIWorldPosB : GIWorldPosC);
                ReSTIRGI.SetTexture(ReSTIRGIKernel, "NEEPosA", FlipFrame ? GINEEPosA : GINEEPosB);
                ReSTIRGI.SetTexture(ReSTIRGIKernel, "NEEPosB", !FlipFrame ? GINEEPosA : GINEEPosB);
                ReSTIRGI.SetTexture(ReSTIRGIKernel, "PrevScreenSpaceInfo", FlipFrame ? ScreenSpaceInfoPrev : ScreenSpaceInfo);
                ReSTIRGI.SetTexture(ReSTIRGIKernel, "RandomNums", _RandomNums);
                ReSTIRGI.SetTexture(ReSTIRGIKernel, "ScreenSpaceInfoRead", FlipFrame ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
                ReSTIRGI.SetTexture(ReSTIRGIKernel, "PrimaryTriData", (FramesSinceStart2 % 2 == 0) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);
                ReSTIRGI.SetTexture(ReSTIRGIKernel, "PrimaryTriDataPrev", (FramesSinceStart2 % 2 == 1) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);
                ReSTIRGI.SetComputeBuffer(ReSTIRGIKernel, "GlobalColors", LightingBuffer);
                ReSTIRGI.SetTexture(ReSTIRGIKernel, "Gradient", GradientsA);
                ReSTIRGI.SetTexture(ReSTIRGIKernel, "GradientWrite", GradientsB);

                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "Gradient", GradientsB);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "WorldPosB", FlipFrame ? GIWorldPosB : GIWorldPosC);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "NEEPosB", FlipFrame ? GINEEPosA : GINEEPosB);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "ReservoirB", FlipFrame ? GIReservoirB : GIReservoirA);
                ReSTIRGI.SetComputeBuffer(ReSTIRGISpatialKernel, "GlobalColors", LightingBuffer);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "ScreenSpaceInfoRead", FlipFrame ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "RandomNums", _RandomNums);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "PrimaryTriData", (FramesSinceStart2 % 2 == 0) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "WorldPosA", GIWorldPosA);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "NEEPosA", GINEEPosC);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel, "ReservoirA", GIReservoirC);

                AssetManager.Assets.SetLightData(ReSTIRGI, ReSTIRGISpatialKernel + 1);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel+1, "GradientWrite", GradientsA);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel+1, "Gradient", GradientsB);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel+1, "WorldPosB", GIWorldPosA);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel+1, "NEEPosB", GINEEPosC);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel+1, "ReservoirB", GIReservoirC);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel+1, "ScreenSpaceInfoRead", FlipFrame ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel+1, "RandomNums", _RandomNums);
                ReSTIRGI.SetTexture(ReSTIRGISpatialKernel+1, "PrimaryTriData", (FramesSinceStart2 % 2 == 0) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);
                ReSTIRGI.SetComputeBuffer(ReSTIRGISpatialKernel+1, "GlobalColors", LightingBuffer);


                AssetManager.Assets.SetMeshTraceBuffers(ReSTIRGI, ReSTIRGISpatialKernel);
                AssetManager.Assets.SetMeshTraceBuffers(ReSTIRGI, ReSTIRGISpatialKernel+1);
            }

            if(EnableDebugTexture) {
                if(!EnableDebugTexturePrev || _DebugTex == null || _DebugTex.width != SourceWidth) {
                    CommonFunctions.CreateRenderTexture(ref _DebugTex, SourceWidth, SourceHeight, CommonFunctions.RTFull4, RenderTextureReadWrite.sRGB);
                }
                Shader.SetGlobalTexture("_DebugTex", _DebugTex);
            } else if(EnableDebugTexturePrev) {
                _DebugTex.ReleaseSafe();                
            }
            EnableDebugTexturePrev = EnableDebugTexture;
        }

        private void ResetAllTextures() {
            // _camera.renderingPath = RenderingPath.DeferredShading;
            if(PrevResFactor != LocalTTSettings.RenderScale || TargetWidth != OverridenWidth || TargetHeight != OverridenHeight) {
                TargetWidth = OverridenWidth;
                TargetHeight = OverridenHeight;
                SourceWidth = (int)Mathf.Ceil((float)TargetWidth * LocalTTSettings.RenderScale);
                SourceHeight = (int)Mathf.Ceil((float)TargetHeight * LocalTTSettings.RenderScale);
                if (Mathf.Abs(SourceWidth - TargetWidth) < 2)
                {
                    SourceWidth = TargetWidth;
                    SourceHeight = TargetHeight;
                    LocalTTSettings.RenderScale = 1;
                }
                PrevResFactor = LocalTTSettings.RenderScale;
                if(LocalTTSettings.DenoiserMethod == 1 && LocalTTSettings.UseReSTIRGI) {ReSTIRASVGFCode.ClearAll(); ReSTIRASVGFCode.init(SourceWidth, SourceHeight);}
                if(LocalTTSettings.DenoiserMethod == 1 && !LocalTTSettings.UseReSTIRGI) {ASVGFCode.ClearAll(); _RandomNumsB.ReleaseSafe(); ASVGFCode.init(SourceWidth, SourceHeight, TargetWidth, TargetHeight); CommonFunctions.CreateRenderTexture(ref _RandomNumsB, SourceWidth, SourceHeight, CommonFunctions.RTFull4);}
                if(TTPostProc.Initialized) TTPostProc.ClearAll();
                TTPostProc.init(SourceWidth, SourceHeight);

                InitRenderTexture(true);
                CommonFunctions.CreateDynamicBuffer(ref _RayBuffer, SourceWidth * SourceHeight * 2, 48);
                CommonFunctions.CreateDynamicBuffer(ref _ShadowBuffer, SourceWidth * SourceHeight, 48);
                CommonFunctions.CreateDynamicBuffer(ref LightingBuffer, SourceWidth * SourceHeight, 64);
                #if !DisableRadianceCache
                    CommonFunctions.CreateDynamicBuffer(ref CacheBuffer, SourceWidth * SourceHeight, 48);
                    CommonFunctions.CreateDynamicBuffer(ref VoxelDataBufferA, 4 * 1024 * 1024 + 1024 * 1024, 16, ComputeBufferType.Raw);
                    CommonFunctions.CreateDynamicBuffer(ref VoxelDataBufferB, 4 * 1024 * 1024 + 1024 * 1024, 16, ComputeBufferType.Raw);

                    int[] EEEE = new int[4 * 1024 * 1024 * 4 + 1024 * 1024];
                    VoxelDataBufferA.SetData(EEEE);
                    VoxelDataBufferB.SetData(EEEE);
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

                    #if !DisableRadianceCache
                        CacheBuffer.ReleaseSafe();
                        VoxelDataBufferA.ReleaseSafe();
                        VoxelDataBufferB.ReleaseSafe();
                    #endif

                    _RayBuffer.ReleaseSafe();
                    _ShadowBuffer.ReleaseSafe();
                    LightingBuffer.ReleaseSafe();
                    _RandomNums.ReleaseSafe();
                    _target.ReleaseSafe();
                    _converged.ReleaseSafe();
                    _DebugTex.ReleaseSafe();
                    _FinalTex.ReleaseSafe();
                    GINEEPosC.ReleaseSafe();
                    GIWorldPosA.ReleaseSafe();
                    ReSTIRInitialized = false;
                    GIReservoirA.ReleaseSafe();
                    GIReservoirB.ReleaseSafe();
                    GIReservoirC.ReleaseSafe();
                    GINEEPosA.ReleaseSafe();
                    GINEEPosB.ReleaseSafe();
                    GIWorldPosB.ReleaseSafe();
                    GIWorldPosC.ReleaseSafe();
                    PSRGBuff.ReleaseSafe();
                    _PrimaryTriangleInfoA.ReleaseSafe();
                    _PrimaryTriangleInfoB.ReleaseSafe();
#if EnablePhotonMapping
                    FirstDiffuseThroughputTex.ReleaseSafe();
                    FirstDiffusePosTex.ReleaseSafe();
#endif
                    MVTexture.ReleaseSafe();
                    ScreenSpaceInfo.ReleaseSafe();
                    ScreenSpaceInfoPrev.ReleaseSafe();
                    GradientsA.ReleaseSafe();
                    GradientsB.ReleaseSafe();
                    CorrectedDistanceTexA.Release();
                    CorrectedDistanceTexB.Release();
                    #if UseOIDN
                        ColorBuffer.ReleaseSafe();
                        OutputBuffer.ReleaseSafe();
                        AlbedoBuffer.ReleaseSafe();
                        NormalBuffer.ReleaseSafe();
                        if(OIDNDenoiser != null) {
                            OIDNDenoiser.Dispose();
                            OIDNDenoiser = null;
                        }
                    #endif
                }

                #if UseOIDN
                    DenoiserPlugin.DenoiserConfig cfg = new DenoiserPlugin.DenoiserConfig() {
                        imageWidth = SourceWidth,
                        imageHeight = SourceHeight,
                        guideAlbedo = 1,
                        guideNormal = 1,
                        temporalMode = 0,
                        cleanAux = 1,
                        prefilterAux = 1
                    };
                    
                    DenoiserPlugin.DenoiserType denoiserType;
                    switch (LocalTTSettings.DenoiserMethod) {
                        case 2:
                            denoiserType = DenoiserPlugin.DenoiserType.OIDN;
                            break;
                        case 3:
                            denoiserType = DenoiserPlugin.DenoiserType.OptiX;
                            break;
                        default:
                            denoiserType = DenoiserPlugin.DenoiserType.OIDN;
                            break;
                    }                   
                    OIDNDenoiser = new DenoiserPlugin.DenoiserPluginWrapper(denoiserType, cfg);
                    
                    ColorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, SourceWidth * SourceHeight, 12);
                    OutputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, SourceWidth * SourceHeight, 12);
                    AlbedoBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, SourceWidth * SourceHeight, 12);
                    NormalBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, SourceWidth * SourceHeight, 12);
                #endif
#if EnablePhotonMapping
                CommonFunctions.CreateRenderTexture(ref FirstDiffuseThroughputTex, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref FirstDiffusePosTex, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
#endif
                CommonFunctions.CreateRenderTexture(ref _RandomNums, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref _FinalTex, TargetWidth, TargetHeight, CommonFunctions.RTFull4, RenderTextureReadWrite.sRGB, true);
                CommonFunctions.CreateRenderTexture(ref _target, SourceWidth, SourceHeight, CommonFunctions.RTHalf4, RenderTextureReadWrite.sRGB);
                CommonFunctions.CreateRenderTexture(ref _converged, SourceWidth, SourceHeight, CommonFunctions.RTFull4, RenderTextureReadWrite.sRGB);
                if(LocalTTSettings.UseReSTIRGI) {
                    CommonFunctions.CreateRenderTextureArray(ref GIReservoirA, SourceWidth, SourceHeight, 2, CommonFunctions.RTFull4);
                    CommonFunctions.CreateRenderTextureArray(ref GIReservoirB, SourceWidth, SourceHeight, 2, CommonFunctions.RTFull4);
                    CommonFunctions.CreateRenderTextureArray(ref GIReservoirC, SourceWidth, SourceHeight, 2, CommonFunctions.RTFull4);
                    CommonFunctions.CreateRenderTexture(ref GINEEPosB, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                    CommonFunctions.CreateRenderTexture(ref GINEEPosC, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                    CommonFunctions.CreateRenderTexture(ref GIWorldPosB, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                    CommonFunctions.CreateRenderTexture(ref GIWorldPosC, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                    CommonFunctions.CreateRenderTexture(ref GradientsA, SourceWidth, SourceHeight, CommonFunctions.RTHalf2);
                    CommonFunctions.CreateRenderTexture(ref GradientsB, SourceWidth, SourceHeight, CommonFunctions.RTHalf2);
                    CommonFunctions.CreateRenderTexture(ref GINEEPosA, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                    ReSTIRInitialized = true;
                } else {
                    CommonFunctions.CreateRenderTexture(ref GINEEPosA, 1, 1, CommonFunctions.RTFull4);
                }
                CommonFunctions.CreateRenderTexture(ref GIWorldPosA, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref _PrimaryTriangleInfoA, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref _PrimaryTriangleInfoB, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
#if MultiMapScreenshot
                CommonFunctions.CreateRenderTexture(ref MultiMapMatIDTexture, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref MultiMapMatIDTextureInitial, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref MultiMapMeshIDTexture, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref MultiMapMeshIDTextureInitial, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
#endif

                CommonFunctions.CreateRenderTexture(ref MVTexture, SourceWidth, SourceHeight, CommonFunctions.RTHalf2);
                CommonFunctions.CreateRenderTexture(ref ScreenSpaceInfo, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref ScreenSpaceInfoPrev, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref CorrectedDistanceTexA, SourceWidth, SourceHeight, CommonFunctions.RTHalf1);
                CommonFunctions.CreateRenderTexture(ref CorrectedDistanceTexB, SourceWidth, SourceHeight, CommonFunctions.RTHalf1);
                // Reset sampling
                _currentSample = 0;
                uFirstFrame = 1;
                FramesSinceStart = 0;
                FramesSinceStart2 = 0;
            }
            if(LocalTTSettings.UseReSTIRGI && !ReSTIRInitialized) {
                GINEEPosA.ReleaseSafe();
                CommonFunctions.CreateRenderTexture(ref GINEEPosA, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref GradientsA, SourceWidth, SourceHeight, CommonFunctions.RTHalf2);
                CommonFunctions.CreateRenderTexture(ref GradientsB, SourceWidth, SourceHeight, CommonFunctions.RTHalf2);
                CommonFunctions.CreateRenderTextureArray(ref GIReservoirA, SourceWidth, SourceHeight, 2, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTextureArray(ref GIReservoirB, SourceWidth, SourceHeight, 2, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTextureArray(ref GIReservoirC, SourceWidth, SourceHeight, 2, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref GINEEPosB, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref GINEEPosC, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref GIWorldPosB, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref GIWorldPosC, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                ReSTIRInitialized = true;
            }
            CommonFunctions.CreateRenderTexture(ref PSRGBuff, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
            if(!LocalTTSettings.UseReSTIRGI && ReSTIRInitialized) {
                GIReservoirA.ReleaseSafe();
                GIReservoirB.ReleaseSafe();
                GIReservoirC.ReleaseSafe();
                GINEEPosA.ReleaseSafe();
                GINEEPosB.ReleaseSafe();
                GIWorldPosB.ReleaseSafe();
                GIWorldPosC.ReleaseSafe();
                GradientsA.ReleaseSafe();
                GradientsB.ReleaseSafe();
                CommonFunctions.CreateRenderTexture(ref GINEEPosA, 1, 1, CommonFunctions.RTFull4);
                ReSTIRInitialized = false;
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
            if (LocalTTSettings.DenoiserMethod == 1 && !LocalTTSettings.UseReSTIRGI) {
            if(DoKernelProfiling) cmd.BeginSample("TTMV");
                cmd.DispatchCompute(ShadingShader, MVKernel+1, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                cmd.DispatchCompute(ShadingShader, MVKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
            if(DoKernelProfiling) cmd.EndSample("TTMV");
                if(DoKernelProfiling) cmd.BeginSample("ASVGF Reproject Pass");
                    // AssetManager.Assets.SetMeshTraceBuffers(ASVGFCode.shader, 1);
                    ASVGFCode.shader.SetTexture(1, "ScreenSpaceInfoWrite", (FramesSinceStart2 % 2 == 0) ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
                    ASVGFCode.DoRNG(ref _RandomNums, ref _RandomNumsB, FramesSinceStart2, _RayBuffer, cmd, (FramesSinceStart2 % 2 == 1) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB, (LocalTTSettings.DoTLASUpdates && (FramesSinceStart2 % 2 == 0)) ? AssetManager.Assets.MeshDataBufferA : AssetManager.Assets.MeshDataBufferB, Assets.AggTriBufferA, MeshOrderChanged, Assets.TLASCWBVHIndexes, SourceWidth, SourceHeight, (LocalTTSettings.DoTLASUpdates && (FramesSinceStart2 % 2 == 1)) ? AssetManager.Assets.MeshDataBufferA : AssetManager.Assets.MeshDataBufferB, CorrectedDistanceTexA, CorrectedDistanceTexB, LightingBuffer);
                if(DoKernelProfiling) cmd.EndSample("ASVGF Reproject Pass");
            }

            SetInt("CurBounce", 0, cmd);
            if(LocalTTSettings.DenoiserMethod != 1 || LocalTTSettings.UseReSTIRGI) {
                if(DoKernelProfiling) cmd.BeginSample("Primary Ray Generation");
                   cmd.DispatchCompute(GenerateShader, (DoChainedImages ? GenPanoramaKernel : GenKernel), Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                if(DoKernelProfiling) cmd.EndSample("Primary Ray Generation");
            }
            if(HasSDFHandler) OptionalSDFHandler.Run(cmd, (FramesSinceStart2 % 2 == 0) ? _RandomNums : _RandomNumsB, _RayBuffer, SourceWidth, SourceHeight);
        }


        private void Render(RenderTexture destination, CommandBuffer cmd)
        {



            if(LocalTTSettings.MaxSampCount > SampleCount) {
                #if !DisableRadianceCache
                    if(DoKernelProfiling) cmd.BeginSample("RadCacheClear");
                        cmd.DispatchCompute(GenerateShader, ResolveKernel, Mathf.CeilToInt((4.0f * 1024.0f * 1024.0f) / 256.0f), 1, 1);
                    if(DoKernelProfiling) cmd.EndSample("RadCacheClear");

                    if(DoKernelProfiling) cmd.BeginSample("RadCacheCompact");
                        cmd.DispatchCompute(GenerateShader, CompactKernel, Mathf.CeilToInt((4.0f * 1024.0f * 1024.0f) / 256.0f), 1, 1);
                    if(DoKernelProfiling) cmd.EndSample("RadCacheCompact");
                #endif
                TTPostProc.ValidateInit(LocalTTSettings.PPBloom, LocalTTSettings.PPTAA, SourceWidth != TargetWidth, LocalTTSettings.UpscalerMethod == 2, LocalTTSettings.DoSharpen, LocalTTSettings.PPFXAA, LocalTTSettings.DoChromaAber || LocalTTSettings.DoBCS || LocalTTSettings.DoVignette);
                float CurrentSample;
                
                GenerateRays(cmd);

                    if(DoKernelProfiling) cmd.BeginSample("Pathtracing Kernels");

                    for (int i = 0; i <= LocalTTSettings.bouncecount; i++) {
                        if(DoKernelProfiling) cmd.BeginSample("Bounce: " + i);
                            var bouncebounce = i;
                            if(bouncebounce == 1) {
                                cmd.SetComputeTextureParam(IntersectionShader, TraceKernel, "_PrimaryTriangleInfo", GIWorldPosA);
                                cmd.SetComputeTextureParam(IntersectionShader, HeightmapKernel, "_PrimaryTriangleInfo", GIWorldPosA);
                            }
                            SetInt("CurBounce", bouncebounce, cmd);
                            if(DoKernelProfiling) cmd.BeginSample("Transfer Kernel: " + i);
                            cmd.SetComputeIntParam(ShadingShader, "Type", 0);
                            cmd.DispatchCompute(ShadingShader, TransferKernel, 1, 1, 1);
                            if(DoKernelProfiling) cmd.EndSample("Transfer Kernel: " + i);

                            if(DoKernelProfiling) cmd.BeginSample("Trace Kernel: " + i);
                            #if DX11Only
                                cmd.DispatchCompute(IntersectionShader, TraceKernel, Mathf.CeilToInt((SourceHeight * SourceWidth) / 64.0f), 1, 1);
                            #else
                                cmd.DispatchCompute(IntersectionShader, TraceKernel, CurBounceInfoBuffer, 0);//784 is 28^2
                            #endif
                            if(DoKernelProfiling) cmd.EndSample("Trace Kernel: " + i);


                            if (Assets.Terrains.Count != 0) {
                                if(DoKernelProfiling) cmd.BeginSample("HeightMap Trace Kernel: " + i);
                                #if DX11Only
                                    cmd.DispatchCompute(IntersectionShader, HeightmapKernel, Mathf.CeilToInt((SourceHeight * SourceWidth) / 64.0f), 1, 1);
                                #else
                                    cmd.DispatchCompute(IntersectionShader, HeightmapKernel, CurBounceInfoBuffer, 0);//784 is 28^2
                                #endif
                                if(DoKernelProfiling) cmd.EndSample("HeightMap Trace Kernel: " + i);
                            }

                            if(DoKernelProfiling) cmd.BeginSample("Shading Kernel: " + i);
                            #if DX11Only
                                cmd.DispatchCompute(ShadingShader, ShadeKernel, Mathf.CeilToInt((SourceHeight * SourceWidth) / 64.0f), 1, 1);
                            #else
                                cmd.DispatchCompute(ShadingShader, ShadeKernel, CurBounceInfoBuffer, 0);
                            #endif
                            if(DoKernelProfiling) cmd.EndSample("Shading Kernel: " + i);



                            if(DoKernelProfiling) cmd.BeginSample("Transfer Kernel 2: " + i);
                            cmd.SetComputeIntParam(ShadingShader, "Type", 1);
                            cmd.DispatchCompute(ShadingShader, TransferKernel, 1, 1, 1);
                            if(DoKernelProfiling) cmd.EndSample("Transfer Kernel 2: " + i);
                            if (LocalTTSettings.UseNEE) {
                                if(DoKernelProfiling) cmd.BeginSample("Shadow Kernel: " + i);
                                #if DX11Only
                                    cmd.DispatchCompute(IntersectionShader, ShadowKernel, Mathf.CeilToInt((SourceHeight * SourceWidth) / 64.0f), 1, 1);
                                #else
                                    cmd.DispatchCompute(IntersectionShader, ShadowKernel, CurBounceInfoBuffer, 0);
                                #endif
                                if(DoKernelProfiling) cmd.EndSample("Shadow Kernel: " + i);
                            }
                            if (LocalTTSettings.UseNEE && Assets.Terrains.Count != 0) {
                                if(DoKernelProfiling) cmd.BeginSample("Heightmap Shadow Kernel: " + i);
                                #if DX11Only
                                    cmd.DispatchCompute(IntersectionShader, HeightmapShadowKernel, Mathf.CeilToInt((SourceHeight * SourceWidth) / 64.0f), 1, 1);
                                #else
                                    cmd.DispatchCompute(IntersectionShader, HeightmapShadowKernel, CurBounceInfoBuffer, 0);
                                #endif
                                if(DoKernelProfiling) cmd.EndSample("Heightmap Shadow Kernel: " + i);
                            }
                        if(DoKernelProfiling) cmd.EndSample("Bounce: " + i);

                    }
                if(DoKernelProfiling) cmd.EndSample("Pathtracing Kernels");

            if(DoKernelProfiling) cmd.BeginSample("TTMV2");
                cmd.DispatchCompute(ShadingShader, MVKernel+2, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
            if(DoKernelProfiling) cmd.EndSample("TTMV2");
#if EnablePhotonMapping
                if(Assets.UnityLightCount != 0) {
                    PhotonMap.Generate(cmd, LocalTTSettings.PhotonMapRadiusCoverage, TTviewprojection, _camera.transform.position, _camera.transform.forward, (FramesSinceStart2 % 2 == 0) ? CorrectedDistanceTexA : CorrectedDistanceTexB, LocalTTSettings.CausticIntensityMultiplier, LocalTTSettings.PhotonGuidingRatio);
                    PhotonMap.Collect(cmd, 
                        ref FirstDiffuseThroughputTex, 
                        ref FirstDiffusePosTex,
                        ref _target,
                        ref LightingBuffer,
                        SourceWidth,
                        SourceHeight);
                }
#endif
                if (LocalTTSettings.UseReSTIRGI) {
                    SetInt("CurBounce", 0, cmd);
                    if(DoKernelProfiling) cmd.BeginSample("ReSTIRGI Temporal Kernel");
                    cmd.DispatchCompute(ReSTIRGI, ReSTIRGIKernel, Mathf.CeilToInt(SourceWidth / 8.0f), Mathf.CeilToInt(SourceHeight / 8.0f), 1);
                    if(DoKernelProfiling) cmd.EndSample("ReSTIRGI Temporal Kernel");
                bool FlipFrame = (FramesSinceStart2 % 2 == 0);

                    if(DoKernelProfiling) cmd.BeginSample("ReSTIRGI Extra Spatial Kernel");
                    SetInt("CurPass", 0, cmd);

                    cmd.DispatchCompute(ReSTIRGI, ReSTIRGISpatialKernel, Mathf.CeilToInt(SourceWidth / 8.0f), Mathf.CeilToInt(SourceHeight / 8.0f), 1);
                    if(DoKernelProfiling) cmd.EndSample("ReSTIRGI Extra Spatial Kernel");

                    if(DoKernelProfiling) cmd.BeginSample("ReSTIRGI Extra Spatial Kernel 1");
                    SetInt("frames_accumulated", _currentSample * 2, cmd);
                    SetInt("CurPass", 1, cmd);
                    cmd.DispatchCompute(ReSTIRGI, ReSTIRGISpatialKernel+1, Mathf.CeilToInt(SourceWidth / 8.0f), Mathf.CeilToInt(SourceHeight / 8.0f), 1);
                    if(DoKernelProfiling) cmd.EndSample("ReSTIRGI Extra Spatial Kernel 1");

                    cmd.Blit(GradientsA, GradientsB);
                }

                if (!(!LocalTTSettings.UseReSTIRGI && LocalTTSettings.DenoiserMethod == 1) && !(LocalTTSettings.UseReSTIRGI && LocalTTSettings.DenoiserMethod == 1))
                {
                    if(DoKernelProfiling) cmd.BeginSample("Finalize Kernel");
                    cmd.DispatchCompute(ShadingShader, FinalizeKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);


                    CurrentSample = 1.0f / (FramesSinceStart + 1.0f);
                    SampleCount++;

                    if (_addMaterial == null)
                        _addMaterial = new Material(Shader.Find("Hidden/Accumulate"));
                    cmd.SetGlobalFloat("_Sample", CurrentSample);
                    cmd.Blit(_target, _converged, _addMaterial);
#if MultiMapScreenshot
                    cmd.Blit(MultiMapMatIDTextureInitial, MultiMapMatIDTexture, _addMaterial);
                    cmd.Blit(MultiMapMeshIDTextureInitial, MultiMapMeshIDTexture, _addMaterial);
#endif

                    if(DoKernelProfiling) cmd.EndSample("Finalize Kernel");
                } else if(!(LocalTTSettings.UseReSTIRGI && LocalTTSettings.DenoiserMethod == 1)) {
                    if(DoKernelProfiling) cmd.BeginSample("ASVGF");
                    SampleCount = 0;
                    ASVGFCode.Do(ref LightingBuffer, 
                                    ref _converged, 
                                    LocalTTSettings.RenderScale, 
                                    ((FramesSinceStart2 % 2 == 0) ? ScreenSpaceInfo : ScreenSpaceInfoPrev), 
                                    cmd, 
                                    FramesSinceStart2, 
                                    ref GIWorldPosA, 
                                    LocalTTSettings.DoPartialRendering ? LocalTTSettings.PartialRenderingFactor : 1, 
                                    TTPostProc.ExposureBuffer, 
                                    LocalTTSettings.PPExposure, 
                                    LocalTTSettings.IndirectBoost, 
                                    (FramesSinceStart2 % 2 == 0) ? _RandomNums : _RandomNumsB, 
                                    LocalTTSettings.UpscalerMethod, 
                                    CorrectedDistanceTexA, 
                                    CorrectedDistanceTexB,
                                    PSRGBuff);
                    CurrentSample = 1;
                    if(DoKernelProfiling) cmd.EndSample("ASVGF");
                } else if(LocalTTSettings.UseReSTIRGI && LocalTTSettings.DenoiserMethod == 1) {
                    if(DoKernelProfiling) cmd.BeginSample("ReSTIR ASVGF");
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
                                        GradientsA,
                                        (FramesSinceStart2 % 2 == 0) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB, 
                                        (LocalTTSettings.DoTLASUpdates && (FramesSinceStart2 % 2 == 0)) ? AssetManager.Assets.MeshDataBufferA : AssetManager.Assets.MeshDataBufferB, 
                                        Assets.AggTriBufferA, 
                                        LocalTTSettings.UpscalerMethod, 
                                        CorrectedDistanceTexA, 
                                        CorrectedDistanceTexB,
                                        PSRGBuff);
                    CurrentSample = 1;
                    if(DoKernelProfiling) cmd.EndSample("ReSTIR ASVGF");
                }
                if(DoKernelProfiling) cmd.BeginSample("Firefly Blit");
                if (_FireFlyMaterial == null)
                    _FireFlyMaterial = new Material(Shader.Find("Hidden/FireFlyPass"));
                if(LocalTTSettings.DoFirefly && SampleCount > LocalTTSettings.FireflyFrameCount && (SampleCount - LocalTTSettings.FireflyFrameCount) % LocalTTSettings.FireflyFrameInterval == 0) {
                    _FireFlyMaterial.SetFloat("_Strength", LocalTTSettings.FireflyStrength);
                    _FireFlyMaterial.SetFloat("_Offset", LocalTTSettings.FireflyOffset);

                    cmd.Blit(_converged, _target, _FireFlyMaterial);
                    cmd.Blit(_target, _converged);
                }
                if(DoKernelProfiling) cmd.EndSample("Firefly Blit");
                if (SourceWidth != TargetWidth) {
                    switch(LocalTTSettings.UpscalerMethod) {
                        case 0://Bilinear
                            cmd.Blit(_converged, _FinalTex);
                        break;
                        case 1://GSR
                            TTPostProc.ExecuteUpsample(ref _converged, ref _FinalTex, FramesSinceStart2, _currentSample, cmd, ((FramesSinceStart2 % 2 == 0) ? ScreenSpaceInfo : ScreenSpaceInfoPrev));
                        break;
                        case 2://TAAU
                            TTPostProc.ExecuteTAAU(ref _FinalTex, ref _converged, cmd, FramesSinceStart2);
                        break;
                    }
                }
                else cmd.CopyTexture(_converged, 0, 0, _FinalTex, 0, 0);
            } else {
                cmd.CopyTexture(_converged, 0, 0, _FinalTex, 0, 0);
            }


            if(DoKernelProfiling) cmd.BeginSample("Post Processing");

            if (LocalTTSettings.PPExposure) {
                _FinalTex.GenerateMips();
                TTPostProc.ExecuteAutoExpose(ref _FinalTex, LocalTTSettings.Exposure, cmd, LocalTTSettings.ExposureAuto);
            }

            #if UseOIDN
                if((LocalTTSettings.DenoiserMethod == 2 || LocalTTSettings.DenoiserMethod == 3) && SampleCount > LocalTTSettings.OIDNFrameCount) {
                    cmd.SetComputeBufferParam(ShadingShader, TTtoOIDNKernel, "OutputBuffer", ColorBuffer);
                    ShadingShader.SetTexture(TTtoOIDNKernel, "Result", _FinalTex);
                    cmd.DispatchCompute(ShadingShader, TTtoOIDNKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);

                    OIDNDenoiser.Render(cmd, ColorBuffer, OutputBuffer, AlbedoBuffer, NormalBuffer);

                    cmd.SetComputeBufferParam(ShadingShader, OIDNtoTTKernel, "OutputBuffer", OutputBuffer);
                    ShadingShader.SetTexture(OIDNtoTTKernel, "Result", _FinalTex);
                    if(SampleCount > LocalTTSettings.OIDNFrameCount+1 || LocalTTSettings.OIDNFrameCount == 0)
                    cmd.DispatchCompute(ShadingShader, OIDNtoTTKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);            
                }
            #endif

            if (LocalTTSettings.PPBloom) {
                if(LocalTTSettings.ConvBloom) TTPostProc.ExecuteConvBloom(ref _FinalTex, LocalTTSettings.ConvStrength, LocalTTSettings.ConvBloomThreshold, LocalTTSettings.ConvBloomSize, LocalTTSettings.ConvBloomDistExp, LocalTTSettings.ConvBloomDistExpClampMin, LocalTTSettings.ConvBloomDistExpScale, cmd);
                else TTPostProc.ExecuteBloom(ref _FinalTex, LocalTTSettings.BloomStrength, cmd);
            }
            if(LocalTTSettings.DoChromaAber || LocalTTSettings.DoBCS || LocalTTSettings.DoVignette) TTPostProc.ExecuteCombinedPP(ref _FinalTex, cmd, LocalTTSettings.DoBCS, LocalTTSettings.DoVignette, LocalTTSettings.DoChromaAber, LocalTTSettings.Contrast, LocalTTSettings.Saturation, LocalTTSettings.ChromaDistort, LocalTTSettings.innerVignette, LocalTTSettings.outerVignette, LocalTTSettings.strengthVignette, LocalTTSettings.curveVignette, LocalTTSettings.ColorVignette);
            if(LocalTTSettings.PPToneMap) TTPostProc.ExecuteToneMap(ref _FinalTex, cmd, ref ToneMapTex, LocalTTSettings.ToneMapper == 7 ? AGXCustomTex : (LocalTTSettings.ToneMapper == 6 ? ToneMapTex3 : ToneMapTex2), LocalTTSettings.ToneMapper);
            if (LocalTTSettings.PPTAA) TTPostProc.ExecuteTAA(ref _FinalTex, _currentSample, cmd);
            if (LocalTTSettings.PPFXAA) TTPostProc.ExecuteFXAA(ref _FinalTex, cmd);
            if (LocalTTSettings.DoSharpen) TTPostProc.ExecuteSharpen(ref _FinalTex, LocalTTSettings.Sharpness, cmd);

            cmd.Blit(_FinalTex, destination);
            if(EnableDebugTexture) ClearOutRenderTexture(_DebugTex);
            if(DoKernelProfiling) cmd.EndSample("Post Processing");
            if(LocalTTSettings.MaxSampCount > SampleCount) {
                _currentSample++;
                FramesSinceStart++;
                FramesSinceStart2++;
                PrevCamPosition = _camera.transform.position;
                PrevASVGF = LocalTTSettings.DenoiserMethod == 1;
                PrevReSTIRGI = LocalTTSettings.UseReSTIRGI;
            }
        }
        public void RenderImage(RenderTexture destination, CommandBuffer cmd)
        {
            if(HDRPorURPRenderInScene && TTPostProc == null) {
                // AssetManager.Assets = GameObject.Find("Scene").GetComponent<AssetManager>();
                Start2();
            }
            if (SceneIsRunning && Assets != null && Assets.RenderQue.Count > 0)
            {
                ResetAllTextures();
                RunUpdate();
                if(RebuildMeshObjectBuffers(cmd)) {
                    InitRenderTexture();
                    Shader.SetGlobalTexture("TTMotionVectorTexture", MVTexture);
                    for(int i = 0; i < LocalTTSettings.SamplesPerFrame; i++) {
                        SetShaderParameters(cmd);
                        Render(destination, cmd);
                    // else cmd.Blit(_FinalTex, destination);
                    }
                }
                uFirstFrame = 0;
            }
            else
            {
                try { int throwawayBool = AssetManager.Assets.UpdateTLAS(cmd); _meshObjectsNeedRebuilding = true;} catch (System.IndexOutOfRangeException) { }
            }
            SceneIsRunning = true;
        }
        // public void OnDrawGizmos() {
        //     if(Application.isPlaying)
        //         PhotonMap.OnDrawGizmos();
        // }

    }


#if UNITY_EDITOR
    [CustomEditor(typeof(RayTracingMaster))]
    public class RayTracingMasterEditor : Editor
    {
        private VisualElement CreateVerticalBox(string Name) {
            VisualElement VertBox = new VisualElement();
            // VertBox.style.flexDirection = FlexDirection.Row;
            return VertBox;
        }

        private VisualElement CreateHorizontalBox(string Name) {
            VisualElement HorizBox = new VisualElement();
            HorizBox.style.flexDirection = FlexDirection.Row;
            return HorizBox;
        }


        public class FloatSliderPair {
            public VisualElement DynamicContainer;
            public Label DynamicLabel;
            public Slider DynamicSlider;
            public FloatField DynamicField;
        }
        FloatSliderPair CreatePairedFloatSlider(string Name, float LowValue, float HighValue, ref float InitialValue, float SliderWidth = 200) {
            FloatSliderPair NewPair = new FloatSliderPair();
            NewPair.DynamicContainer = CreateHorizontalBox(Name + " Container");
            NewPair.DynamicLabel = new Label(Name);
            NewPair.DynamicSlider = new Slider() {value = InitialValue, highValue = HighValue, lowValue = LowValue};
            NewPair.DynamicField = new FloatField() {value = InitialValue};
            NewPair.DynamicSlider.style.width = SliderWidth;
            NewPair.DynamicContainer.Add(NewPair.DynamicLabel);
            NewPair.DynamicContainer.Add(NewPair.DynamicSlider);
            NewPair.DynamicContainer.Add(NewPair.DynamicField);
            return NewPair;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var t1 = (targets);
            int TargCount = t1.Length;
            var t =  t1[0] as RayTracingMaster;
            VisualElement MainContainer = CreateVerticalBox("Main Container");
                Toggle RenderInSceneToggle = new Toggle() {value = t.HDRPorURPRenderInScene, text = "Render in Scene View(HDRP/URP ONLY)"};
                RenderInSceneToggle.RegisterValueChangedCallback(evt => {t.HDRPorURPRenderInScene = evt.newValue;});
                MainContainer.Add(RenderInSceneToggle);

                ObjectField LocalTTSettingsField = new ObjectField("Local TT Settings Override");
                LocalTTSettingsField.objectType = typeof(TTSettings);
                LocalTTSettingsField.value = t.LocalTTSettings;
                LocalTTSettingsField.RegisterValueChangedCallback(evt => {t.LocalTTSettings = evt.newValue as TTSettings;});
                MainContainer.Add(LocalTTSettingsField);
                
                IntegerField SamplesPerFrameField = new IntegerField("Samples per Frame");
                SamplesPerFrameField.value = t.LocalTTSettings.SamplesPerFrame;
                SamplesPerFrameField.RegisterValueChangedCallback(evt => {t.LocalTTSettings.SamplesPerFrame = evt.newValue;});
                MainContainer.Add(SamplesPerFrameField);

#if EnablePhotonMapping
                 VisualElement SpacerA = new VisualElement();
                 SpacerA.style.height = 10;
                MainContainer.Add(SpacerA);

                FloatField DirectionalLightCoverageRadiusField = new FloatField("Photon Mapping Radius Coverage");
                DirectionalLightCoverageRadiusField.value = t.LocalTTSettings.PhotonMapRadiusCoverage;
                DirectionalLightCoverageRadiusField.RegisterValueChangedCallback(evt => {t.LocalTTSettings.PhotonMapRadiusCoverage = evt.newValue;});
                MainContainer.Add(DirectionalLightCoverageRadiusField);

                IntegerField PerLightGuidingResolutionField = new IntegerField("Photon Mapping Per-Light Guiding Resolution");
                PerLightGuidingResolutionField.value = t.LocalTTSettings.PhotonGuidingPerLightGuidingResolution;
                PerLightGuidingResolutionField.RegisterValueChangedCallback(evt => {t.LocalTTSettings.PhotonGuidingPerLightGuidingResolution = evt.newValue;});
                MainContainer.Add(PerLightGuidingResolutionField);

                IntegerField TotalPhotonsField = new IntegerField("Photon Mapping Total Photons Per Frame");
                TotalPhotonsField.value = t.LocalTTSettings.PhotonGuidingTotalPhotonsPerFrame;
                TotalPhotonsField.RegisterValueChangedCallback(evt => {t.LocalTTSettings.PhotonGuidingTotalPhotonsPerFrame = evt.newValue;});
                MainContainer.Add(TotalPhotonsField);

                FloatField CausticIntensityMultiplierField = new FloatField("Caustic Intensity Multiplier");
                CausticIntensityMultiplierField.value = t.LocalTTSettings.CausticIntensityMultiplier;
                CausticIntensityMultiplierField.RegisterValueChangedCallback(evt => {t.LocalTTSettings.CausticIntensityMultiplier = evt.newValue;});
                MainContainer.Add(CausticIntensityMultiplierField);

                FloatField PhotonRatioField = new FloatField("Guiding vs Guided Photon Ratio");
                PhotonRatioField.value = t.LocalTTSettings.PhotonGuidingRatio;
                PhotonRatioField.RegisterValueChangedCallback(evt => {t.LocalTTSettings.PhotonGuidingRatio = Mathf.Clamp(evt.newValue, 0.001f, 0.999f);});
                MainContainer.Add(PhotonRatioField);
                
                VisualElement SpacerB = new VisualElement();
                SpacerB.style.height = 10;
                MainContainer.Add(SpacerB);
#endif

                Toggle DebugTexToggle = new Toggle() {value = t.EnableDebugTexture, text = "Enable Debug Texture"};
                DebugTexToggle.RegisterValueChangedCallback(evt => {t.EnableDebugTexture = evt.newValue;});
                MainContainer.Add(DebugTexToggle);

                // if(t.LocalTTSettings.ToneMapper == 7) {
                    ObjectField OverrideAGX = new ObjectField("Custom AGX Tonemap Texture");
                    OverrideAGX.objectType = typeof(Texture3D);
                    OverrideAGX.value = t.AGXCustomTex;
                    OverrideAGX.RegisterValueChangedCallback(evt => {t.AGXCustomTex = evt.newValue as Texture3D;});
                    MainContainer.Add(OverrideAGX);
                // }

            return MainContainer;
        }

    }
#endif


}
