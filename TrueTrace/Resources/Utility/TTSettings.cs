using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TrueTrace {

    [CreateAssetMenu(fileName = "TTSettings", menuName = "TrueTrace/Settings", order = 1)]
    [System.Serializable] public class TTSettings : ScriptableObject {
        [SerializeField] public string SceneName = "TestA";
        [SerializeField] public float IndirectBoost = 1.0f;
        [SerializeField] public int bouncecount = 10;
        [SerializeField] public bool ClayMode = false;
        [SerializeField] public bool UseRussianRoulette = true;
        [SerializeField] public bool UseNEE = true;
        [SerializeField] public bool DoTLASUpdates = true;
        [SerializeField] public bool Accumulate = true;
        [SerializeField] public bool PPBloom = false;
        [SerializeField] public float BloomStrength = 0.5f;
        [SerializeField] public bool PPDoF = false;
        [SerializeField] public float DoFAperature = 0.2f;
        [SerializeField] public float DoFAperatureScale = 1.0f;
        [SerializeField] public float DoFFocal = 0.2f;
        [SerializeField] public bool PPExposure = false;
        [SerializeField] public bool ExposureAuto = false;
        [SerializeField] public bool PPToneMap = true;
        [SerializeField] public bool PPTAA = false;
        [SerializeField] public float RenderScale = 1.0f;
        [SerializeField] public int DenoiserMethod = 0;
        [SerializeField] public int UpscalerMethod = 0;
        [SerializeField] public bool UseReSTIRGITemporal = true;
        [SerializeField] public bool UseReSTIRGISpatial = true;
        [SerializeField] public bool UseReSTIRGI = false;
        [SerializeField] public int ReSTIRGITemporalMCap = 4;
        [SerializeField] public bool DoReSTIRGIConnectionValidation = true;
        [SerializeField] public float Exposure = 1.0f;
        [SerializeField] public bool DoPartialRendering = false;
        [SerializeField] public int PartialRenderingFactor = 1;
        [SerializeField] public bool DoFirefly = false;
        [SerializeField] public bool ImprovedPrimaryHit = true;
        [SerializeField] public int RISCount = 12;
        [SerializeField] public int ToneMapper = 0;
        [SerializeField] public float SkyDesaturate = 0.0f;
        [SerializeField] public Vector3 ClayColor = new Vector3(0.5f, 0.5f, 0.5f);
        [SerializeField] public float ClayMetalOverride = 0.0f;
        [SerializeField] public float ClayRoughnessOverride = 0.0f;
        [SerializeField] public bool DoClayMetalRoughOverride = false;
        [SerializeField] public Vector3 GroundColor = new Vector3(0.1f, 0.1f, 0.1f);
        [SerializeField] public int FireflyFrameCount = 0;
        [SerializeField] public int FireflyFrameInterval = 1;
        [SerializeField] public float FireflyStrength = 1.0f;
        [SerializeField] public float FireflyOffset = 0.0f;
        [SerializeField] public int OIDNFrameCount = 0;
        [SerializeField] public bool DoSharpen = false;
        [SerializeField] public float Sharpness = 1.0f;
        [SerializeField] public int MainDesiredRes = 16384;
        [SerializeField] public bool UseSkinning = true;
        [SerializeField] public float LightEnergyScale = 1.0f;
        [SerializeField] public int BackgroundType = 0;
        [SerializeField] public Vector2 BackgroundIntensity = Vector2.one;
        [SerializeField] public Vector3 SceneBackgroundColor = Vector3.one;
        [SerializeField] public int SecondaryBackgroundType = 0;
        [SerializeField] public Vector3 SecondarySceneBackgroundColor = Vector3.one;
        [SerializeField] public Vector2 HDRILongLat = Vector2.zero;
        [SerializeField] public Vector2 HDRIScale = Vector2.one;
        [SerializeField] public float LEMEnergyScale = 1.0f;
        [SerializeField] public bool UseTransmittanceInNEE = true;
        [SerializeField] public float SecondarySkyDesaturate = 0.0f;
        [SerializeField] public bool MatChangeResetsAccum = true;
        [SerializeField] public bool PPFXAA = false;
        [SerializeField] public float OIDNBlendRatio = 1.0f;
        [SerializeField] public bool ConvBloom = false;
        [SerializeField] public float ConvStrength = 1.37f;
        [SerializeField] public float ConvBloomThreshold = 13.23f;
        [SerializeField] public Vector2 ConvBloomSize = Vector2.one;
        [SerializeField] public float ConvBloomDistExp = 0;
        [SerializeField] public float ConvBloomDistExpClampMin = 1;
        [SerializeField] public float ConvBloomDistExpScale = 1;
        [SerializeField] public Vector3 PrimaryBackgroundTintColor = new Vector3(1.0f, 1.0f, 1.0f);
        [SerializeField] public float PrimaryBackgroundTint = 0;
        [SerializeField] public float PrimaryBackgroundContrast = 1;
        [SerializeField] public float FogDensity = 0.0002f;
        [SerializeField] public float FogHeight = 80.0f;
        [SerializeField] public Vector3 FogColor = new Vector3(0.6f, 0.6f, 0.6f);
        [SerializeField] public int MaxSampCount = 99999999;
        [SerializeField] public bool DoChromaAber = false;
        [SerializeField] public float ChromaDistort = 0.3f;
        [SerializeField] public bool DoBCS = false;
        [SerializeField] public float Saturation = 1.0f;
        [SerializeField] public float Contrast = 1.0f;
        [SerializeField] public bool DoVignette = false;
        [SerializeField] public float innerVignette = 0.5f;
        [SerializeField] public float outerVignette = 1.2f;
        [SerializeField] public float strengthVignette = 0.8f;
        [SerializeField] public float curveVignette = 0.5f;
        [SerializeField] public Vector3 ColorVignette = Vector3.zero;
        [SerializeField] public float aoStrength = 1.0f;
        [SerializeField] public float aoRadius = 2.0f;
        [SerializeField] public string CurrentTargetFile = null;
        [SerializeField] public int SamplesPerFrame = 1;
        [SerializeField] public float PhotonMapRadiusCoverage = 8;
        [SerializeField] public float CausticIntensityMultiplier = 1;
        [SerializeField] public float PhotonGuidingRatio = 0.15f;
        [SerializeField] public int PhotonGuidingPerLightGuidingResolution = 128;
        [SerializeField] public int PhotonGuidingTotalPhotonsPerFrame = 2000000;

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TTSettings))]
    public class TTSettingsEditor : Editor 
    {
        // SerializedProperty lookAtPoint;
        
        void OnEnable()
        {
            // lookAtPoint = serializedObject.FindProperty("lookAtPoint");
        }

        public override void OnInspectorGUI()
        {

            base.OnInspectorGUI();
            var script = (TTSettings)target;

            if(GUILayout.Button("Set For Modification", GUILayout.Height(40))) {
                TTInterface.SetTTSettings(script);
            }
            // serializedObject.Update();
            // EditorGUILayout.PropertyField(lookAtPoint);
            // serializedObject.ApplyModifiedProperties();
        }
    }


#endif
}