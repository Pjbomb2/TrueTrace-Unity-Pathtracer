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
        public static RayTracingMaster RayMaster;
        [HideInInspector] public static Camera _camera;
        public static bool DoKernelProfiling = true;
        [HideInInspector] [SerializeField] public string LocalTTSettingsName = "TTGlobalSettings";
        private bool OverriddenResolutionIsActive = false;
        public bool HDRPorURPRenderInScene = false;
        [HideInInspector] public AtmosphereGenerator Atmo;
        [HideInInspector] public AssetManager Assets;
        private ReSTIRASVGF ReSTIRASVGFCode;
        private TTPostProcessing TTPostProc;
        private ASVGF ASVGFCode;
        public static bool ImageIsModified = false;
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
                    DetailNormalStrength = OBJtoWrite.DetailNormalStrength[Index],
                    Hue = OBJtoWrite.Hue[Index],
                    Brightness = OBJtoWrite.Brightness[Index],
                    Contrast = OBJtoWrite.Contrast[Index],
                    Saturation = OBJtoWrite.Saturation[Index],
                    BlendColor = OBJtoWrite.BlendColor[Index],
                    BlendFactor = OBJtoWrite.BlendFactor[Index],
                    MainTexScaleOffset = OBJtoWrite.MainTexScaleOffset[Index],
                    SecondaryAlbedoTexScaleOffset = OBJtoWrite.SecondaryAlbedoTexScaleOffset[Index],
                    SecondaryTextureScaleOffset = OBJtoWrite.SecondaryTextureScaleOffset[Index],
                    NormalTexScaleOffset = OBJtoWrite.NormalTexScaleOffset[Index],
                    RotationNormal = OBJtoWrite.RotationNormal[Index],
                    RotationSecondary = OBJtoWrite.RotationSecondary[Index],
                    RotationSecondaryDiffuse = OBJtoWrite.RotationSecondaryDiffuse[Index],
                    RotationSecondaryNormal = OBJtoWrite.RotationSecondaryNormal[Index],
                    Rotation = OBJtoWrite.Rotation[Index],
                    Flags = OBJtoWrite.Flags[Index],
                    UseKelvin = OBJtoWrite.UseKelvin[Index],
                    KelvinTemp = OBJtoWrite.KelvinTemp[Index],
                    ColorBleed = OBJtoWrite.ColorBleed[Index],
                    SecondaryNormalTexBlend = OBJtoWrite.SecondaryNormalTexBlend[Index],
                    SecondaryNormalTexScaleOffset = OBJtoWrite.SecondaryNormalTexScaleOffset[Index],
                    AlbedoBlendFactor = OBJtoWrite.AlbedoBlendFactor[Index]
                };
                if(WriteID == -1) {
                    raywrites.RayObj.Add(DataToWrite);
                } else {
                    raywrites.RayObj[WriteID] = DataToWrite;
                }
            }
        }

        // [HideInInspector] public ComputeShader ShadingShader;
        private ComputeShader CDFCompute;

        private RenderTexture _target;
        private RenderTexture _converged;
        private RenderTexture _DebugTex;
        private RenderTexture _FinalTex;
        private RenderTexture _RandomNums;
        private RenderTexture _RandomNumsB;
        private RenderTexture _PrimaryTriangleInfoA;
        private RenderTexture _PrimaryTriangleInfoB;

        // public RenderTexture TTMotionVectors;

        private RenderTexture GIReservoirA;
        private RenderTexture GIReservoirB;
        private RenderTexture GIReservoirC;

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
            private ComputeBuffer HashBufferA;
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
        private bool MeshOrderChanged = false;

        [HideInInspector] public int MainDirectionalLight = -1;
        [HideInInspector] public int AtmoNumLayers = 4;
        private float PrevResFactor;
        private string GenKernel;
        private string GenPanoramaKernel;
        private string TraceKernel;
        private string ShadowKernel;
        private string HeightmapKernel;
        private string HeightmapShadowKernel;
        private string ShadeKernel;
        private string FinalizeKernel;
        private string GIReTraceKernel;
        private string TransferKernel;
        private string ReSTIRGIKernel;
        private string ReSTIRGISpatialKernel;
        private string TTtoOIDNKernel;
        private string OIDNtoTTKernel;
        private string TTtoOIDNKernelPanorama;
        private string MVKernel;
        #if !DisableRadianceCache
            private string ResolveKernel;
            private string CompactKernel;
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
        private Matrix4x4 PrevViewProjection;


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
            ShaderStuff.DX12ShadersInitialize(Application.dataPath + "/TrueTrace-Unity-Pathtracer/TrueTrace/Resources/shaderjsons/shaderjsons");
            // LoadInitialSettings();//Build only
            Application.targetFrameRate = 165;
            ASVGFCode = new ASVGF();
            ReSTIRASVGFCode = new ReSTIRASVGF();
            ToneMapTex = Resources.Load<Texture3D>("Utility/ToneMapTex");
            ToneMapTex2 = Resources.Load<Texture3D>("Utility/AgXBC");
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
            GenKernel = "Generate";
            GenPanoramaKernel = "GeneratePanorama";
            TraceKernel = "kernel_trace";
            ShadowKernel = "kernel_shadow";
            ShadeKernel = "kernel_shade";
            FinalizeKernel = "kernel_finalize";
            HeightmapShadowKernel = "kernel_shadow_heightmap";
            HeightmapKernel = "kernel_heightmap";
            GIReTraceKernel = "GIReTraceKernel";
            TransferKernel = "TransferKernel";
            ReSTIRGIKernel = "ReSTIRGIKernel";
            ReSTIRGISpatialKernel = "ReSTIRGISpatial";
            TTtoOIDNKernel = "TTtoOIDNKernel";
            OIDNtoTTKernel = "OIDNtoTTKernel";
            TTtoOIDNKernelPanorama = "TTtoOIDNKernelPanorama";
            MVKernel = "MVKernel";
            #if !DisableRadianceCache
                ResolveKernel = "CacheResolve";
                CompactKernel = "CacheCompact";
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
            if(TTPostProc != null) TTPostProc.ClearAll();
            _RayBuffer.ReleaseSafe();
            LightingBuffer.ReleaseSafe();
            _BufferSizes.ReleaseSafe();
            _ShadowBuffer.ReleaseSafe();
            if(ASVGFCode != null) ASVGFCode.ClearAll();
            if(ReSTIRASVGFCode != null) ReSTIRASVGFCode.ClearAll();
            CurBounceInfoBuffer.ReleaseSafe();
            Atmo.Dispose();
            TTPostProc.ClearAll();
            CDFX.ReleaseSafe();
            CDFY.ReleaseSafe();
            CDFTotalBuffer.ReleaseSafe();
            #if !DisableRadianceCache
                CacheBuffer.ReleaseSafe();
                VoxelDataBufferA.ReleaseSafe();
                VoxelDataBufferB.ReleaseSafe();
                HashBufferA.ReleaseSafe();
                HashBufferB.ReleaseSafe();
            #endif

            _RandomNums.ReleaseSafe();
            _RandomNumsB.ReleaseSafe();
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
            ScreenSpaceInfo.ReleaseSafe();
            ScreenSpaceInfoPrev.ReleaseSafe();
            GradientsA.ReleaseSafe();
            GradientsB.ReleaseSafe();
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
            // cmd.SetMatrix(Name, Mat);
            // cmd.SetMatrix(Name, Mat);
            // GenerateShader.SetMatrix(Name, Mat);
            // ReSTIRGI.SetMatrix(Name, Mat);
            // if(HasSDFHandler) OptionalSDFHandler.GenShader.SetMatrix(Name, Mat);
        }

        private void SetVector(string Name, Vector3 IN, CommandBuffer cmd) {
            cmd.SetVector("RayTracingShader", Name, IN);
            cmd.SetVector("IntersectionKernels", Name, IN);
            cmd.SetVector("RayGenKernels", Name, IN);
            cmd.SetVector("ReSTIRGI", Name, IN);
            // if(HasSDFHandler) cmd.SetComputeVectorParam(OptionalSDFHandler.GenShader, Name, IN);
        }

        private void SetInt(string Name, int IN, CommandBuffer cmd) {
            cmd.SetInt("RayTracingShader", Name, IN);
            cmd.SetInt("IntersectionKernels", Name, IN);
            cmd.SetInt("RayGenKernels", Name, IN);
            cmd.SetInt("ReSTIRGI", Name, IN);
            // if(HasSDFHandler) cmd.SetComputeIntParam(OptionalSDFHandler.GenShader, Name, IN);
        }

        private void SetFloat(string Name, float IN, CommandBuffer cmd) {
            cmd.SetFloat("RayTracingShader", Name, IN);
            cmd.SetFloat("IntersectionKernels", Name, IN);
            cmd.SetFloat("RayGenKernels", Name, IN);
            cmd.SetFloat("ReSTIRGI", Name, IN);
            // if(HasSDFHandler) cmd.SetComputeFloatParam(OptionalSDFHandler.GenShader, Name, IN);
        }

        private void SetBool(string Name, bool IN, CommandBuffer cmd) {
            cmd.SetBool("RayTracingShader", Name, IN);
            cmd.SetBool("IntersectionKernels", Name, IN);
            cmd.SetBool("RayGenKernels", Name, IN);
            cmd.SetBool("ReSTIRGI", Name, IN);
            // if(HasSDFHandler) OptionalSDFHandler.GenShader.SetBool(Name, IN);
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

        private Vector2 HDRIParams = Vector2.zero;
        private void SetShaderParameters(CommandBuffer cmd) {
            HasSDFHandler = (OptionalSDFHandler != null) && OptionalSDFHandler.enabled && OptionalSDFHandler.gameObject.activeInHierarchy;
            if(LocalTTSettings.RenderScale != 1.0f) _camera.renderingPath = RenderingPath.DeferredShading;
            _camera.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
            if(LocalTTSettings.UseReSTIRGI && LocalTTSettings.DenoiserMethod == 1 && !ReSTIRASVGFCode.Initialized) ReSTIRASVGFCode.init(SourceWidth, SourceHeight);
            else if ((LocalTTSettings.DenoiserMethod != 1 || !LocalTTSettings.UseReSTIRGI) && ReSTIRASVGFCode.Initialized) ReSTIRASVGFCode.ClearAll();
            if (!LocalTTSettings.UseReSTIRGI && LocalTTSettings.DenoiserMethod == 1 && !ASVGFCode.Initialized) ASVGFCode.init(SourceWidth, SourceHeight, OverridenWidth, OverridenHeight);
            else if ((LocalTTSettings.DenoiserMethod != 1 || LocalTTSettings.UseReSTIRGI) && ASVGFCode.Initialized) ASVGFCode.ClearAll();
            if(TTPostProc.Initialized == false) TTPostProc.init(SourceWidth, SourceHeight);

            if(BufferSizes == null || BufferSizes.Length != LocalTTSettings.bouncecount + 1) BufferSizes = new BufferSizeData[LocalTTSettings.bouncecount + 1];
            for(int i = 0; i < LocalTTSettings.bouncecount + 1; i++) {
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
                _BufferSizes = new ComputeBuffer(LocalTTSettings.bouncecount + 1, 24);
            }
            if(_BufferSizes.count != LocalTTSettings.bouncecount + 1) {
                _BufferSizes.ReleaseSafe();
                _BufferSizes = new ComputeBuffer(LocalTTSettings.bouncecount + 1, 24);
            }
            _BufferSizes.SetData(BufferSizes);
            cmd.SetBuffer("kernel_shade", "BufferSizes", _BufferSizes);
            cmd.SetBuffer("TransferKernel", "BufferSizes", _BufferSizes);
            cmd.SetBuffer(TraceKernel, "BufferSizes", _BufferSizes);
            cmd.SetBuffer(ShadowKernel, "BufferSizes", _BufferSizes);
            cmd.SetBuffer(HeightmapShadowKernel, "BufferSizes", _BufferSizes);
            cmd.SetBuffer(HeightmapKernel, "BufferSizes", _BufferSizes);



            var EA = CamToWorldPrev;
            var EB = CamInvProjPrev;
            Matrix4x4 ProjectionMatrix = CalcProj(_camera);

            CamInvProjPrev = ProjectionMatrix.inverse;
            CamToWorldPrev = _camera.cameraToWorldMatrix;
            SetMatrix("viewprojection", ProjectionMatrix * _camera.worldToCameraMatrix);
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
            SetVector("SunDir", Assets.SunDirection, cmd);
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
            SetFloat("GISpatialRadius", LocalTTSettings.ReSTIRGISpatialRadius, cmd);
            SetFloat("SkyDesaturate", LocalTTSettings.SkyDesaturate, cmd);
            SetFloat("SecondarySkyDesaturate", LocalTTSettings.SecondarySkyDesaturate, cmd);
            SetFloat("BackgroundIntensity", LocalTTSettings.BackgroundIntensity.x, cmd);
            SetFloat("SecondaryBackgroundIntensity", LocalTTSettings.BackgroundIntensity.y, cmd);
            SetFloat("LEMEnergyScale", LocalTTSettings.LEMEnergyScale, cmd);
            SetFloat("OIDNBlendRatio", LocalTTSettings.OIDNBlendRatio, cmd);
            SetFloat("FogDensity", LocalTTSettings.FogDensity, cmd);
            SetFloat("ScaleHeight", LocalTTSettings.FogHeight, cmd);
            SetFloat("OrthoSize", _camera.orthographicSize, cmd);

            SetInt("MainDirectionalLight", MainDirectionalLight, cmd);
            SetInt("NonInstanceCount", Assets.NonInstanceCount, cmd);
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
            SetInt("RISCount", LocalTTSettings.RISCount, cmd);
            SetInt("BackgroundType", LocalTTSettings.BackgroundType, cmd);
            SetInt("SecondaryBackgroundType", LocalTTSettings.SecondaryBackgroundType, cmd);
            SetInt("MaterialCount", Assets.MatCount, cmd);
            SetInt("PartialRenderingFactor", LocalTTSettings.DoPartialRendering ? LocalTTSettings.PartialRenderingFactor : 1, cmd);
            SetInt("UpscalerMethod", LocalTTSettings.UpscalerMethod, cmd);
            SetInt("ReSTIRGIUpdateRate", LocalTTSettings.UseReSTIRGI ? LocalTTSettings.ReSTIRGIUpdateRate : 0, cmd);

            SetBool("IsOrtho", _camera.orthographic, cmd);
            SetBool("IsFocusing", IsFocusing, cmd);
            SetBool("DoPanorama", DoPanorama, cmd);
            SetBool("ClayMode", LocalTTSettings.ClayMode, cmd);
            SetBool("ImprovedPrimaryHit", LocalTTSettings.ImprovedPrimaryHit, cmd);
            SetBool("UseRussianRoulette", LocalTTSettings.UseRussianRoulette, cmd);
            SetBool("UseNEE", LocalTTSettings.UseNEE, cmd);
            SetBool("UseDoF", LocalTTSettings.PPDoF, cmd);
            SetBool("UseReSTIRGI", LocalTTSettings.UseReSTIRGI, cmd);
            SetBool("UseReSTIRGITemporal", LocalTTSettings.UseReSTIRGITemporal, cmd);
            SetBool("UseReSTIRGISpatial", LocalTTSettings.UseReSTIRGISpatial, cmd);
            SetBool("DoReSTIRGIConnectionValidation", LocalTTSettings.DoReSTIRGIConnectionValidation, cmd);
            SetBool("UseASVGF", LocalTTSettings.DenoiserMethod == 1 && !LocalTTSettings.UseReSTIRGI, cmd);
            SetBool("UseASVGFAndReSTIR", LocalTTSettings.DenoiserMethod == 1 && LocalTTSettings.UseReSTIRGI, cmd);
            SetBool("TerrainExists", Assets.Terrains.Count != 0, cmd);
            SetBool("DoPartialRendering", LocalTTSettings.DoPartialRendering, cmd);
            SetBool("UseTransmittanceInNEE", LocalTTSettings.UseTransmittanceInNEE, cmd);
            OIDNGuideWrite = (FramesSinceStart == LocalTTSettings.OIDNFrameCount);
            SetBool("OIDNGuideWrite", OIDNGuideWrite && (LocalTTSettings.DenoiserMethod == 2 || LocalTTSettings.DenoiserMethod == 3), cmd);
            SetBool("DiffRes", LocalTTSettings.RenderScale != 1.0f, cmd);
            SetBool("DoPartialRendering", LocalTTSettings.DoPartialRendering, cmd);
            SetBool("DoExposure", LocalTTSettings.PPExposure, cmd);
            cmd.SetBuffer(ShadeKernel, "Exposure", TTPostProc.ExposureBuffer);

            bool FlipFrame = (FramesSinceStart2 % 2 == 0);


            // GenerateShader.SetTextureFromGlobal(GIReTraceKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            // ReSTIRGI.SetTextureFromGlobal(ReSTIRGIKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            // ReSTIRGI.SetTextureFromGlobal(ReSTIRGISpatialKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            // cmd.SetTextureFromGlobal(ShadeKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            cmd.SetTexture(ShadeKernel, "CDFY", Texture2D.whiteTexture.GetNativeTexturePtr());
            


            if (SkyboxTexture == null) SkyboxTexture = new Texture2D(1,1, TextureFormat.RGBA32, false);
            if (SkyboxTexture != null)
            {
                if(CDFTotalBuffer == null || !CDFTotalBuffer.IsValid()) {
                    CDFTotalBuffer = new ComputeBuffer(1, 4);
                }
                // if((LocalTTSettings.BackgroundType == 1 || LocalTTSettings.SecondaryBackgroundType == 1) && CDFX == null) {
                //     CDFX.ReleaseSafe();
                //     CDFY.ReleaseSafe();
                //     CDFCompute = Resources.Load<ComputeShader>("Utility/CDFCreator");
                //     CommonFunctions.CreateRenderTexture(ref CDFX, SkyboxTexture.width, SkyboxTexture.height, CommonFunctions.RTFull1);
                //     CommonFunctions.CreateRenderTexture(ref CDFY, SkyboxTexture.height, 1, CommonFunctions.RTFull1);
                //     float[] CDFTotalInit = new float[1];
                //     CDFTotalBuffer.SetData(CDFTotalInit);
                //     ComputeBuffer CounterBuffer = new ComputeBuffer(1, 4);
                //     int[] CounterInit = new int[1];
                //     CounterBuffer.SetData(CounterInit);
                //     CDFCompute.SetTexture(0, "Tex", SkyboxTexture);
                //     CDFCompute.SetTexture(0, "CDFX", CDFX);
                //     CDFCompute.SetTexture(0, "CDFY", CDFY);
                //     CDFCompute.SetInt("w", SkyboxTexture.width);
                //     CDFCompute.SetInt("h", SkyboxTexture.height);
                //     CDFCompute.SetBuffer(0, "CounterBuffer", CounterBuffer);
                //     CDFCompute.SetBuffer(0, "TotalBuff", CDFTotalBuffer);
                //     CDFCompute.Dispatch(0, 1, SkyboxTexture.height, 1);
                //     CounterBuffer.ReleaseSafe();
                //     HDRIParams = new Vector2(SkyboxTexture.width, SkyboxTexture.height);
                //     cmd.SetTexture(ShadeKernel, "CDFX", CDFX);
                //     cmd.SetTexture(ShadeKernel, "CDFY", CDFY);
                // }
                // if(LocalTTSettings.BackgroundType != 1 && LocalTTSettings.SecondaryBackgroundType != 1) {
                    cmd.SetTexture(ShadeKernel, "CDFX", SkyboxTexture.GetNativeTexturePtr());
                    cmd.SetTexture(ShadeKernel, "CDFY", SkyboxTexture.GetNativeTexturePtr());
                // } else {
                    // cmd.SetTexture(ShadeKernel, "CDFX", CDFX);
                    // cmd.SetTexture(ShadeKernel, "CDFY", CDFY);

                // }
                cmd.SetBuffer(ShadeKernel, "TotSum", CDFTotalBuffer);
                cmd.SetTexture(ShadeKernel, "_SkyboxTexture", SkyboxTexture.GetNativeTexturePtr());
            }
            SetVector("HDRIParams", HDRIParams, cmd);

            if(!_camera.orthographic) {
                // cmd.SetTextureFromGlobal(FinalizeKernel, "DiffuseGBuffer", "_CameraGBufferTexture0");
                // cmd.SetTextureFromGlobal(FinalizeKernel, "SpecularGBuffer", "_CameraGBufferTexture1");
            } else {
                
            }

            cmd.SetTexture(GenKernel, "RandomNums", ((FramesSinceStart2 % 2 == 0) ? _RandomNums : _RandomNumsB).GetNativeTexturePtr());
            cmd.SetBuffer(HeightmapKernel, "GlobalRays", _RayBuffer);
            cmd.SetBuffer(GenPanoramaKernel, "GlobalRays", _RayBuffer);
            cmd.SetTexture(GenPanoramaKernel, "RandomNums", ((FramesSinceStart2 % 2 == 0) ? _RandomNums : _RandomNumsB).GetNativeTexturePtr());

            AssetManager.Assets.SetMeshTraceBuffers(TraceKernel, cmd);
            cmd.SetBuffer(TraceKernel, "GlobalRays", _RayBuffer);
            cmd.SetBuffer(TraceKernel, "GlobalColors", LightingBuffer);
            cmd.SetTexture(TraceKernel, "_PrimaryTriangleInfo", (FramesSinceStart2 % 2 == 0) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);


            AssetManager.Assets.SetMeshTraceBuffers(ShadowKernel, cmd);
            cmd.SetComputeBuffer(ShadowKernel, "ShadowRaysBuffer", _ShadowBuffer);
            cmd.SetComputeBuffer(ShadowKernel, "GlobalColors", LightingBuffer);
            cmd.SetTexture(ShadowKernel, "NEEPosA", FlipFrame ? GINEEPosA : GINEEPosB);
            cmd.SetTexture(TraceKernel, "RandomNums", FlipFrame ? _RandomNums : _RandomNumsB);


            AssetManager.Assets.SetHeightmapTraceBuffers(HeightmapKernel, cmd);
            cmd.SetComputeBuffer(HeightmapKernel, "GlobalRays", _RayBuffer);
            cmd.SetTexture(HeightmapKernel, "_PrimaryTriangleInfo", (FramesSinceStart2 % 2 == 0) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);

            AssetManager.Assets.SetHeightmapTraceBuffers(HeightmapShadowKernel, cmd);
            cmd.SetComputeBuffer(HeightmapShadowKernel, "GlobalColors", LightingBuffer);
            cmd.SetComputeBuffer(HeightmapShadowKernel, "ShadowRaysBuffer", _ShadowBuffer);
            cmd.SetTexture(HeightmapShadowKernel, "NEEPosA", FlipFrame ? GINEEPosA : GINEEPosB);

            #if !DisableRadianceCache
                cmd.SetComputeBuffer(ResolveKernel, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                cmd.SetComputeBuffer(ResolveKernel, "VoxelDataBufferB", !FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                cmd.SetComputeBuffer(ResolveKernel, "HashEntriesBufferA", FlipFrame ? HashBufferA : HashBufferB);
                cmd.SetComputeBuffer(ResolveKernel, "HashEntriesBufferB", !FlipFrame ? HashBufferA : HashBufferB);
                cmd.SetComputeBuffer(CompactKernel, "HashEntriesBufferA", FlipFrame ? HashBufferA : HashBufferB);



                cmd.SetComputeBuffer(ShadowKernel, "CacheBuffer", CacheBuffer);
                cmd.SetComputeBuffer(ShadowKernel, "HashEntriesBufferA", FlipFrame ? HashBufferA : HashBufferB);
                cmd.SetComputeBuffer(ShadowKernel, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);

                cmd.SetComputeBuffer(HeightmapShadowKernel, "CacheBuffer", CacheBuffer);
                cmd.SetComputeBuffer(HeightmapShadowKernel, "HashEntriesBufferA", FlipFrame ? HashBufferA : HashBufferB);
                cmd.SetComputeBuffer(HeightmapShadowKernel, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                
                cmd.SetComputeBuffer(ShadeKernel, "CacheBuffer", CacheBuffer);
                cmd.SetComputeBuffer(ShadeKernel, "HashEntriesBufferA", FlipFrame ? HashBufferA : HashBufferB);
                cmd.SetComputeBuffer(ShadeKernel, "HashEntriesBufferB", !FlipFrame ? HashBufferA : HashBufferB);
                cmd.SetComputeBuffer(ShadeKernel, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                cmd.SetComputeBuffer(ShadeKernel, "VoxelDataBufferB", !FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                
                cmd.SetComputeBuffer(FinalizeKernel, "CacheBuffer", CacheBuffer);
                cmd.SetComputeBuffer(FinalizeKernel, "HashEntriesBufferA", FlipFrame ? HashBufferA : HashBufferB);
                cmd.SetComputeBuffer(FinalizeKernel, "HashEntriesBufferB", !FlipFrame ? HashBufferA : HashBufferB);
                cmd.SetComputeBuffer(FinalizeKernel, "VoxelDataBufferA", FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
                cmd.SetComputeBuffer(FinalizeKernel, "VoxelDataBufferB", !FlipFrame ? VoxelDataBufferA : VoxelDataBufferB);
            #endif



            Atmo.AssignTextures(ShadeKernel, cmd);
            AssetManager.Assets.SetLightData(ShadeKernel, cmd);
            AssetManager.Assets.SetMeshTraceBuffers(ShadeKernel, cmd);
            AssetManager.Assets.SetHeightmapTraceBuffers(ShadeKernel, cmd);
            // cmd.SetTexture(ShadeKernel, "CloudShapeTex", Atmo.CloudShapeTex);
            // cmd.SetTexture(ShadeKernel, "CloudShapeDetailTex", Atmo.CloudShapeDetailTex);
            // cmd.SetTexture(ShadeKernel, "localWeatherTexture", Atmo.WeatherTex);
            cmd.SetTexture(ShadeKernel, "WorldPosB", !FlipFrame ? GIWorldPosB : GIWorldPosC);
            cmd.SetTexture(ShadeKernel, "RandomNums", FlipFrame ? _RandomNums : _RandomNumsB);
            cmd.SetTexture(ShadeKernel, "SingleComponentAtlas", Assets.SingleComponentAtlas);
            cmd.SetTexture(ShadeKernel, "_EmissiveAtlas", Assets.EmissiveAtlas);
            cmd.SetTexture(ShadeKernel, "_NormalAtlas", Assets.NormalAtlas);
            cmd.SetTexture(ShadeKernel, "ScreenSpaceInfo", FlipFrame ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
            cmd.SetComputeBuffer(ShadeKernel, "GlobalColors", LightingBuffer);
            cmd.SetComputeBuffer(ShadeKernel, "GlobalRays", _RayBuffer);
            cmd.SetComputeBuffer(ShadeKernel, "ShadowRaysBuffer", _ShadowBuffer);


            cmd.SetBuffer(FinalizeKernel, "GlobalColors", LightingBuffer);
            cmd.SetTexture(FinalizeKernel, "Result", _target);
            

            // AssetManager.Assets.SetMeshTraceBuffers(ShadingShader, MVKernel);
            // cmd.SetTexture(MVKernel, "PrimaryTriData", FlipFrame ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);
            // cmd.SetTexture(MVKernel, "PrimaryTriDataPrev", !FlipFrame ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);
            // cmd.SetTexture(MVKernel, "Result", _target);

            #if UseOIDN
                cmd.SetBuffer(TTtoOIDNKernel, "AlbedoBuffer", AlbedoBuffer);
                cmd.SetBuffer(TTtoOIDNKernel, "NormalBuffer", NormalBuffer);
                cmd.SetTexture(TTtoOIDNKernel, "ScreenSpaceInfo", ScreenSpaceInfo);
                cmd.SetComputeBuffer(TTtoOIDNKernel, "GlobalColors", LightingBuffer);
                cmd.SetBuffer(TTtoOIDNKernelPanorama, "AlbedoBuffer", AlbedoBuffer);
                cmd.SetBuffer(TTtoOIDNKernelPanorama, "NormalBuffer", NormalBuffer);
                cmd.SetTexture(TTtoOIDNKernelPanorama, "ScreenSpaceInfo", ScreenSpaceInfo);
                cmd.SetComputeBuffer(TTtoOIDNKernelPanorama, "GlobalColors", LightingBuffer);
            #endif

            cmd.SetComputeBuffer(GIReTraceKernel, "GlobalRays", _RayBuffer);
            cmd.SetTexture(GIReTraceKernel, "RandomNumsWrite", FlipFrame ? _RandomNums : _RandomNumsB);
            cmd.SetTexture(GIReTraceKernel, "ReservoirA", !FlipFrame ? GIReservoirB : GIReservoirA);
            cmd.SetTexture(GIReTraceKernel, "RandomNums", !FlipFrame ? _RandomNums : _RandomNumsB);

            

            AssetManager.Assets.SetMeshTraceBuffers(ReSTIRGIKernel, cmd);
            cmd.SetTexture(ReSTIRGIKernel, "ReservoirA", FlipFrame ? GIReservoirB : GIReservoirA);
            cmd.SetTexture(ReSTIRGIKernel, "ReservoirB", !FlipFrame ? GIReservoirB : GIReservoirA);
            cmd.SetTexture(ReSTIRGIKernel, "WorldPosC", GIWorldPosA);
            cmd.SetTexture(ReSTIRGIKernel, "WorldPosA", FlipFrame ? GIWorldPosB : GIWorldPosC);
            cmd.SetTexture(ReSTIRGIKernel, "WorldPosB", !FlipFrame ? GIWorldPosB : GIWorldPosC);
            cmd.SetTexture(ReSTIRGIKernel, "NEEPosA", FlipFrame ? GINEEPosA : GINEEPosB);
            cmd.SetTexture(ReSTIRGIKernel, "NEEPosB", !FlipFrame ? GINEEPosA : GINEEPosB);
            cmd.SetTexture(ReSTIRGIKernel, "PrevScreenSpaceInfo", FlipFrame ? ScreenSpaceInfoPrev : ScreenSpaceInfo);
            cmd.SetTexture(ReSTIRGIKernel, "RandomNums", FlipFrame ? _RandomNums : _RandomNumsB);
            cmd.SetTexture(ReSTIRGIKernel, "ScreenSpaceInfoRead", FlipFrame ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
            cmd.SetTexture(ReSTIRGIKernel, "PrimaryTriData", (FramesSinceStart2 % 2 == 0) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);
            cmd.SetTexture(ReSTIRGIKernel, "PrimaryTriDataPrev", (FramesSinceStart2 % 2 == 1) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);
            cmd.SetComputeBuffer(ReSTIRGIKernel, "GlobalColors", LightingBuffer);
            cmd.SetTexture(ReSTIRGIKernel, "Gradient", GradientsA);
            cmd.SetTexture(ReSTIRGIKernel, "GradientWrite", GradientsB);

            cmd.SetTexture(ReSTIRGISpatialKernel, "Gradient", GradientsB);
            cmd.SetTexture(ReSTIRGISpatialKernel, "WorldPosB", FlipFrame ? GIWorldPosB : GIWorldPosC);
            cmd.SetTexture(ReSTIRGISpatialKernel, "NEEPosB", FlipFrame ? GINEEPosA : GINEEPosB);
            cmd.SetTexture(ReSTIRGISpatialKernel, "ReservoirB", FlipFrame ? GIReservoirB : GIReservoirA);
            cmd.SetComputeBuffer(ReSTIRGISpatialKernel, "GlobalColors", LightingBuffer);
            cmd.SetTexture(ReSTIRGISpatialKernel, "ScreenSpaceInfoRead", FlipFrame ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
            cmd.SetTexture(ReSTIRGISpatialKernel, "RandomNums", FlipFrame ? _RandomNums : _RandomNumsB);
            cmd.SetTexture(ReSTIRGISpatialKernel, "PrimaryTriData", (FramesSinceStart2 % 2 == 0) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);
            cmd.SetTexture(ReSTIRGISpatialKernel, "WorldPosA", GIWorldPosA);
            cmd.SetTexture(ReSTIRGISpatialKernel, "NEEPosA", GINEEPosC);
            cmd.SetTexture(ReSTIRGISpatialKernel, "ReservoirA", GIReservoirC);

            cmd.SetTexture(ReSTIRGISpatialKernel+"2", "GradientWrite", GradientsA);
            cmd.SetTexture(ReSTIRGISpatialKernel+"2", "Gradient", GradientsB);
            cmd.SetTexture(ReSTIRGISpatialKernel+"2", "WorldPosB", GIWorldPosA);
            cmd.SetTexture(ReSTIRGISpatialKernel+"2", "NEEPosB", GINEEPosC);
            cmd.SetTexture(ReSTIRGISpatialKernel+"2", "ReservoirB", GIReservoirC);
            cmd.SetTexture(ReSTIRGISpatialKernel+"2", "ScreenSpaceInfoRead", FlipFrame ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
            cmd.SetTexture(ReSTIRGISpatialKernel+"2", "RandomNums", FlipFrame ? _RandomNums : _RandomNumsB);
            cmd.SetTexture(ReSTIRGISpatialKernel+"2", "PrimaryTriData", (FramesSinceStart2 % 2 == 0) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB);
            cmd.SetComputeBuffer(ReSTIRGISpatialKernel+"2", "GlobalColors", LightingBuffer);


            AssetManager.Assets.SetMeshTraceBuffers(ReSTIRGISpatialKernel, cmd);
            AssetManager.Assets.SetMeshTraceBuffers(ReSTIRGISpatialKernel+"2", cmd);

            Shader.SetGlobalTexture("_DebugTex", _DebugTex);
        }

        private void ResetAllTextures() {
            // _camera.renderingPath = RenderingPath.DeferredShading;
            _camera.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
            if(PrevResFactor != LocalTTSettings.RenderScale || TargetWidth != OverridenWidth) {
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
                if(LocalTTSettings.DenoiserMethod == 1 && !LocalTTSettings.UseReSTIRGI) {ASVGFCode.ClearAll(); ASVGFCode.init(SourceWidth, SourceHeight, TargetWidth, TargetHeight);}
                if(TTPostProc.Initialized) TTPostProc.ClearAll();
                TTPostProc.init(SourceWidth, SourceHeight);

                InitRenderTexture(true);
                CommonFunctions.CreateDynamicBuffer(ref _RayBuffer, SourceWidth * SourceHeight * 2, 48);
                CommonFunctions.CreateDynamicBuffer(ref _ShadowBuffer, SourceWidth * SourceHeight, 48);
                CommonFunctions.CreateDynamicBuffer(ref LightingBuffer, SourceWidth * SourceHeight, 64);
                #if !DisableRadianceCache
                    CommonFunctions.CreateDynamicBuffer(ref CacheBuffer, SourceWidth * SourceHeight, 48);
                    CommonFunctions.CreateDynamicBuffer(ref VoxelDataBufferA, 4 * 1024 * 1024, 16, ComputeBufferType.Raw);
                    CommonFunctions.CreateDynamicBuffer(ref VoxelDataBufferB, 4 * 1024 * 1024, 16, ComputeBufferType.Raw);
                    CommonFunctions.CreateDynamicBuffer(ref HashBufferA, 4 * 1024 * 1024, 4);
                    CommonFunctions.CreateDynamicBuffer(ref HashBufferB, 4 * 1024 * 1024, 4);

                    int[] EEE = new int[4 * 1024 * 1024];
                    HashBufferA.SetData(EEE);
                    HashBufferB.SetData(EEE);
                    int[] EEEE = new int[4 * 1024 * 1024 * 4];
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
                        HashBufferA.ReleaseSafe();
                        HashBufferB.ReleaseSafe();
                    #endif

                    _RayBuffer.ReleaseSafe();
                    _ShadowBuffer.ReleaseSafe();
                    LightingBuffer.ReleaseSafe();
                    _RandomNums.ReleaseSafe();
                    _RandomNumsB.ReleaseSafe();
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
                    ScreenSpaceInfo.ReleaseSafe();
                    ScreenSpaceInfoPrev.ReleaseSafe();
                    GradientsA.ReleaseSafe();
                    GradientsB.ReleaseSafe();
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
                    UnityDenoiserPlugin.DenoiserConfig cfg = new UnityDenoiserPlugin.DenoiserConfig() {
                        imageWidth = SourceWidth,
                        imageHeight = SourceHeight,
                        guideAlbedo = 1,
                        guideNormal = 1,
                        temporalMode = 0,
                        cleanAux = 1,
                        prefilterAux = 1
                    };
					
					UnityDenoiserPlugin.DenoiserType denoiserType;
					switch (LocalTTSettings.DenoiserMethod) {
						case 2:
							denoiserType = UnityDenoiserPlugin.DenoiserType.OIDN;
							break;
						case 3:
							denoiserType = UnityDenoiserPlugin.DenoiserType.OptiX;
							break;
						default:
							denoiserType = UnityDenoiserPlugin.DenoiserType.OIDN;
							break;
					}					
                    OIDNDenoiser = new UnityDenoiserPlugin.DenoiserPluginWrapper(denoiserType, cfg);
					
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
                CommonFunctions.CreateRenderTextureArray(ref GIReservoirA, SourceWidth, SourceHeight, 2, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTextureArray(ref GIReservoirB, SourceWidth, SourceHeight, 2, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTextureArray(ref GIReservoirC, SourceWidth, SourceHeight, 2, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref GINEEPosA, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
                CommonFunctions.CreateRenderTexture(ref GINEEPosB, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
                CommonFunctions.CreateRenderTexture(ref GINEEPosC, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
                CommonFunctions.CreateRenderTexture(ref GIWorldPosA, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref GIWorldPosB, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref GIWorldPosC, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref _PrimaryTriangleInfoA, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref _PrimaryTriangleInfoB, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref ScreenSpaceInfo, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref ScreenSpaceInfoPrev, SourceWidth, SourceHeight, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref GradientsA, SourceWidth, SourceHeight, CommonFunctions.RTHalf2);
                CommonFunctions.CreateRenderTexture(ref GradientsB, SourceWidth, SourceHeight, CommonFunctions.RTHalf2);
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
            if (LocalTTSettings.DenoiserMethod == 1 && !LocalTTSettings.UseReSTIRGI) {
                if(DoKernelProfiling) cmd.BeginSample("ASVGF Reproject Pass");
                    ASVGFCode.shader.SetTexture(1, "ScreenSpaceInfoWrite", (FramesSinceStart2 % 2 == 0) ? ScreenSpaceInfo : ScreenSpaceInfoPrev);
                    ASVGFCode.DoRNG(ref _RandomNums, ref _RandomNumsB, FramesSinceStart2, _RayBuffer, cmd, (FramesSinceStart2 % 2 == 1) ? _PrimaryTriangleInfoA : _PrimaryTriangleInfoB, (LocalTTSettings.DoTLASUpdates && (FramesSinceStart2 % 2 == 0)) ? AssetManager.Assets.MeshDataBufferA : AssetManager.Assets.MeshDataBufferB, Assets.AggTriBufferA, MeshOrderChanged, Assets.TLASCWBVHIndexes);
                if(DoKernelProfiling) cmd.EndSample("ASVGF Reproject Pass");
            }

            SetInt("CurBounce", 0, cmd);
            if(LocalTTSettings.UseReSTIRGI && LocalTTSettings.ReSTIRGIUpdateRate != 0) {
                if(DoKernelProfiling) cmd.BeginSample("ReSTIR GI Reproject");
                cmd.DispatchCompute(GIReTraceKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                if(DoKernelProfiling) cmd.EndSample("ReSTIR GI Reproject");
            } else if(LocalTTSettings.DenoiserMethod != 1 || LocalTTSettings.UseReSTIRGI) {
                if(DoKernelProfiling) cmd.BeginSample("Primary Ray Generation");
                   cmd.DispatchCompute((DoChainedImages ? GenPanoramaKernel : GenKernel), Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                if(DoKernelProfiling) cmd.EndSample("Primary Ray Generation");
            }
        }

        private void Render(RenderTexture destination, CommandBuffer cmd)
        {
            #if !DisableRadianceCache
                if(DoKernelProfiling) cmd.BeginSample("RadCacheClear");
                    cmd.DispatchCompute(ResolveKernel, Mathf.CeilToInt((4.0f * 1024.0f * 1024.0f) / 256.0f), 1, 1);
                if(DoKernelProfiling) cmd.EndSample("RadCacheClear");

                if(DoKernelProfiling) cmd.BeginSample("RadCacheCompact");
                    cmd.DispatchCompute(CompactKernel, Mathf.CeilToInt((4.0f * 1024.0f * 1024.0f) / 256.0f), 1, 1);
                if(DoKernelProfiling) cmd.EndSample("RadCacheCompact");
            #endif
            TTPostProc.ValidateInit(LocalTTSettings.PPBloom, LocalTTSettings.PPTAA, SourceWidth != TargetWidth, LocalTTSettings.UpscalerMethod == 2, LocalTTSettings.DoSharpen, LocalTTSettings.PPFXAA, LocalTTSettings.DoChromaAber || LocalTTSettings.DoBCS || LocalTTSettings.DoVignette);
            float CurrentSample;
            
            GenerateRays(cmd);

                if(DoKernelProfiling) cmd.BeginSample("Pathtracing Kernels");

                for (int i = 0; i < LocalTTSettings.bouncecount; i++) {
                    if(DoKernelProfiling) cmd.BeginSample("Bounce: " + i);
                        var bouncebounce = i;
                        if(bouncebounce == 1) {
                            cmd.SetTexture(TraceKernel, "_PrimaryTriangleInfo", GIWorldPosA);
                            cmd.SetTexture(HeightmapKernel, "_PrimaryTriangleInfo", GIWorldPosA);
                        }
                        SetInt("CurBounce", bouncebounce, cmd);
                        if(DoKernelProfiling) cmd.BeginSample("Transfer Kernel: " + i);
                        cmd.SetInt("RayTracingShader", "Type", 0);
                        cmd.DispatchCompute(TransferKernel, 1, 1, 1);
                        if(DoKernelProfiling) cmd.EndSample("Transfer Kernel: " + i);

                        if(DoKernelProfiling) cmd.BeginSample("Trace Kernel: " + i);
                        #if DX11Only
                            cmd.DispatchCompute(TraceKernel, Mathf.CeilToInt((SourceHeight * SourceWidth) / 64.0f), 1, 1);
                        #else
                            #if HardwareRT
                                cmd.DispatchCompute(TraceKernel, CurBounceInfoBuffer, 0);//784 is 28^2
                            #else
                                cmd.DispatchCompute(TraceKernel, 32, 32, 1);
                            #endif
                        #endif
                        if(DoKernelProfiling) cmd.EndSample("Trace Kernel: " + i);


                        if (Assets.Terrains.Count != 0) {
                            if(DoKernelProfiling) cmd.BeginSample("HeightMap Trace Kernel: " + i);
                            #if DX11Only
                                cmd.DispatchCompute(HeightmapKernel, Mathf.CeilToInt((SourceHeight * SourceWidth) / 64.0f), 1, 1);
                            #else
                                cmd.DispatchCompute(HeightmapKernel, CurBounceInfoBuffer, 0);//784 is 28^2
                            #endif
                            if(DoKernelProfiling) cmd.EndSample("HeightMap Trace Kernel: " + i);
                        }

                        if(DoKernelProfiling) cmd.BeginSample("Shading Kernel: " + i);
                        #if DX11Only
                            cmd.DispatchCompute(ShadeKernel, Mathf.CeilToInt((SourceHeight * SourceWidth) / 64.0f), 1, 1);
                        #else
                            cmd.DispatchCompute(ShadeKernel, CurBounceInfoBuffer, 0);
                        #endif
                        if(DoKernelProfiling) cmd.EndSample("Shading Kernel: " + i);



                        if(DoKernelProfiling) cmd.BeginSample("Transfer Kernel 2: " + i);
                        cmd.SetInt("RayTracingShader", "Type", 1);
                        cmd.DispatchCompute(TransferKernel, 1, 1, 1);
                        if(DoKernelProfiling) cmd.EndSample("Transfer Kernel 2: " + i);
                        if (LocalTTSettings.UseNEE) {
                            if(DoKernelProfiling) cmd.BeginSample("Shadow Kernel: " + i);
                            #if DX11Only
                                cmd.DispatchCompute(ShadowKernel, Mathf.CeilToInt((SourceHeight * SourceWidth) / 64.0f), 1, 1);
                            #else
                                #if HardwareRT
                                    cmd.DispatchCompute(ShadowKernel, CurBounceInfoBuffer, 0);
                                #else
                                    cmd.DispatchCompute(ShadowKernel, 32, 32, 1);
                                #endif
                            #endif
                            if(DoKernelProfiling) cmd.EndSample("Shadow Kernel: " + i);
                        }
                        if (LocalTTSettings.UseNEE && Assets.Terrains.Count != 0) {
                            if(DoKernelProfiling) cmd.BeginSample("Heightmap Shadow Kernel: " + i);
                            #if DX11Only
                                cmd.DispatchCompute(HeightmapShadowKernel, Mathf.CeilToInt((SourceHeight * SourceWidth) / 64.0f), 1, 1);
                            #else
                                cmd.DispatchCompute(HeightmapShadowKernel, CurBounceInfoBuffer, 0);
                            #endif
                            if(DoKernelProfiling) cmd.EndSample("Heightmap Shadow Kernel: " + i);
                        }
                    if(DoKernelProfiling) cmd.EndSample("Bounce: " + i);

                }
            if(DoKernelProfiling) cmd.EndSample("Pathtracing Kernels");


            if (LocalTTSettings.UseReSTIRGI) {
                SetInt("CurBounce", 0, cmd);
                if(DoKernelProfiling) cmd.BeginSample("ReSTIRGI Temporal Kernel");
                cmd.DispatchCompute(ReSTIRGIKernel, Mathf.CeilToInt(SourceWidth / 8.0f), Mathf.CeilToInt(SourceHeight / 8.0f), 1);
                if(DoKernelProfiling) cmd.EndSample("ReSTIRGI Temporal Kernel");
            bool FlipFrame = (FramesSinceStart2 % 2 == 0);

                if(DoKernelProfiling) cmd.BeginSample("ReSTIRGI Extra Spatial Kernel");
                SetInt("CurPass", 0, cmd);

                cmd.DispatchCompute(ReSTIRGISpatialKernel, Mathf.CeilToInt(SourceWidth / 8.0f), Mathf.CeilToInt(SourceHeight / 8.0f), 1);
                if(DoKernelProfiling) cmd.EndSample("ReSTIRGI Extra Spatial Kernel");

                if(DoKernelProfiling) cmd.BeginSample("ReSTIRGI Extra Spatial Kernel 1");
                SetInt("frames_accumulated", _currentSample * 2, cmd);
                SetInt("CurPass", 1, cmd);
                cmd.DispatchCompute(ReSTIRGISpatialKernel+"2", Mathf.CeilToInt(SourceWidth / 8.0f), Mathf.CeilToInt(SourceHeight / 8.0f), 1);
                if(DoKernelProfiling) cmd.EndSample("ReSTIRGI Extra Spatial Kernel 1");

                cmd.Blit(GradientsA, GradientsB);
            }

            if (!(!LocalTTSettings.UseReSTIRGI && LocalTTSettings.DenoiserMethod == 1) && !(LocalTTSettings.UseReSTIRGI && LocalTTSettings.DenoiserMethod == 1))
            {
                if(DoKernelProfiling) cmd.BeginSample("Finalize Kernel");
                cmd.DispatchCompute(FinalizeKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                CurrentSample = 1.0f / (FramesSinceStart + 1.0f);
                SampleCount++;

                if (_addMaterial == null)
                    _addMaterial = new Material(Shader.Find("Hidden/Accumulate"));
                _addMaterial.SetFloat("_Sample", CurrentSample);
                cmd.Blit(_target, _converged, _addMaterial);
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
                                LocalTTSettings.UpscalerMethod);
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
                                    LocalTTSettings.UpscalerMethod);
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


            if(DoKernelProfiling) cmd.BeginSample("Post Processing");
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

            if (LocalTTSettings.PPExposure) {
                _FinalTex.GenerateMips();
                TTPostProc.ExecuteAutoExpose(ref _FinalTex, LocalTTSettings.Exposure, cmd, LocalTTSettings.ExposureAuto);
            }

            #if UseOIDN
                if((LocalTTSettings.DenoiserMethod == 2 || LocalTTSettings.DenoiserMethod == 3) && SampleCount > LocalTTSettings.OIDNFrameCount) {
                    if(DoChainedImages) {
                        cmd.SetComputeBufferParam(ShadingShader, TTtoOIDNKernelPanorama, "OutputBuffer", ColorBuffer);
                        cmd.SetTexture(TTtoOIDNKernelPanorama, "Result", _FinalTex);
                        cmd.DispatchCompute(ShadingShader, TTtoOIDNKernelPanorama, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                    } else {
                        cmd.SetComputeBufferParam(ShadingShader, TTtoOIDNKernel, "OutputBuffer", ColorBuffer);
                        cmd.SetTexture(TTtoOIDNKernel, "Result", _FinalTex);
                        cmd.DispatchCompute(ShadingShader, TTtoOIDNKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                    }

                    OIDNDenoiser.Render(cmd, ColorBuffer, OutputBuffer, AlbedoBuffer, NormalBuffer);

                    cmd.SetComputeBufferParam(ShadingShader, OIDNtoTTKernel, "OutputBuffer", OutputBuffer);
                    cmd.SetTexture(OIDNtoTTKernel, "Result", _FinalTex);
                    if(SampleCount > LocalTTSettings.OIDNFrameCount+1 || LocalTTSettings.OIDNFrameCount == 0)
                    cmd.DispatchCompute(ShadingShader, OIDNtoTTKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);            
                }
            #endif

            if (LocalTTSettings.PPBloom) {
                if(LocalTTSettings.ConvBloom) TTPostProc.ExecuteConvBloom(ref _FinalTex, LocalTTSettings.ConvStrength, LocalTTSettings.ConvBloomThreshold, LocalTTSettings.ConvBloomSize, LocalTTSettings.ConvBloomDistExp, LocalTTSettings.ConvBloomDistExpClampMin, LocalTTSettings.ConvBloomDistExpScale, cmd);
                else TTPostProc.ExecuteBloom(ref _FinalTex, LocalTTSettings.BloomStrength, cmd);
            }
            if(LocalTTSettings.DoChromaAber || LocalTTSettings.DoBCS || LocalTTSettings.DoVignette) TTPostProc.ExecuteCombinedPP(ref _FinalTex, cmd, LocalTTSettings.DoBCS, LocalTTSettings.DoVignette, LocalTTSettings.DoChromaAber, LocalTTSettings.Contrast, LocalTTSettings.Saturation, LocalTTSettings.ChromaDistort, LocalTTSettings.innerVignette, LocalTTSettings.outerVignette, LocalTTSettings.strengthVignette, LocalTTSettings.curveVignette, LocalTTSettings.ColorVignette);
            if(LocalTTSettings.PPToneMap) TTPostProc.ExecuteToneMap(ref _FinalTex, cmd, ref ToneMapTex, ref ToneMapTex2, LocalTTSettings.ToneMapper);
            if (LocalTTSettings.PPTAA) TTPostProc.ExecuteTAA(ref _FinalTex, _currentSample, cmd);
            if (LocalTTSettings.PPFXAA) TTPostProc.ExecuteFXAA(ref _FinalTex, cmd);
            if (LocalTTSettings.DoSharpen) TTPostProc.ExecuteSharpen(ref _FinalTex, LocalTTSettings.Sharpness, cmd);
            cmd.Blit(_FinalTex, destination);
            ClearOutRenderTexture(_DebugTex);
            if(DoKernelProfiling) cmd.EndSample("Post Processing");
            _currentSample++;
            FramesSinceStart++;
            FramesSinceStart2++;
            PrevCamPosition = _camera.transform.position;
            PrevASVGF = LocalTTSettings.DenoiserMethod == 1;
            PrevReSTIRGI = LocalTTSettings.UseReSTIRGI;
        }

        public void RenderImage(RenderTexture destination, CommandBuffer cmd)
        {
            _camera.renderingPath = RenderingPath.DeferredShading;
            if (SceneIsRunning && Assets != null && Assets.RenderQue.Count > 0)
            {
                ResetAllTextures();
                RunUpdate();
                if(RebuildMeshObjectBuffers(cmd)) {
                    InitRenderTexture();
                    SetShaderParameters(cmd);
                    if(LocalTTSettings.MaxSampCount > SampleCount) Render(destination, cmd);
                    else cmd.Blit(_FinalTex, destination);
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
