using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrueTrace {

    [CreateAssetMenu(fileName = "TTSettings", menuName = "TrueTrace/Settings", order = 1)]
    [System.Serializable] public class TTSettings : ScriptableObject {
        [SerializeField] public string SceneName = "TestA";
        [SerializeField] public float IndirectBoost = 1.0f;
        [SerializeField] public int bouncecount = 12;
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
        [SerializeField] public bool UseASVGF = false;
        [SerializeField] public bool UseTAAU = true;
        [SerializeField] public int ReSTIRGIUpdateRate = 7;
        [SerializeField] public bool UseReSTIRGITemporal = true;
        [SerializeField] public bool UseReSTIRGISpatial = true;
        [SerializeField] public bool UseReSTIRGI = false;
        [SerializeField] public int ReSTIRGISpatialCount = 24;
        [SerializeField] public float ReSTIRGISpatialRadius = 50.0f;
        [SerializeField] public int ReSTIRGITemporalMCap = 4;
        [SerializeField] public bool DoReSTIRGIConnectionValidation = true;
        [SerializeField] public float Exposure = 1.0f;
        [SerializeField] public bool DoPartialRendering = false;
        [SerializeField] public int PartialRenderingFactor = 1;
        [SerializeField] public bool DoFirefly = false;
        [SerializeField] public bool ImprovedPrimaryHit = false;
        [SerializeField] public int RISCount = 12;
        [SerializeField] public int ToneMapper = 0;
        [SerializeField] public float SkyDesaturate = 0.0f;
        [SerializeField] public Vector3 ClayColor = new Vector3(0.5f, 0.5f, 0.5f);
        [SerializeField] public Vector3 GroundColor = new Vector3(0.1f, 0.1f, 0.1f);
        [SerializeField] public int FireflyFrameCount = 0;
        [SerializeField] public float FireflyStrength = 1.0f;
        [SerializeField] public float FireflyOffset = 0.0f;
        [SerializeField] public int OIDNFrameCount = 0;
        [SerializeField] public bool UseOIDN = false;
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
        [SerializeField] public bool MatChangeResetsAccum = false;
    }
}