// #define HardwareRT
// #define HDRP
// #define DX11
#define UseSGTree
#define TTReflectionMotionVectors
// #define MultiMapScreenshot
// #define RasterizedDirect
// #define PhotonMapping
// #define TTDisplacement
// #define Fog
//Dont modify above, These have CPU-side toggles, which are available under "Functionality Settings" in the truetrace settings menu

//The defines below can be modified at any time by removing or adding "//" in front of each #define to tune your experience
// -- Trace-time modifications --
#define AdvancedAlphaMapped
#define IgnoreGlassShadow
// #define IgnoreGlassMain
// #define StainedGlassShadows
// #define ShadowGlassAttenuation
// #define IgnoreBackfacing
// #define FadeMapping
// #define AltFadeMapping

// -- Lights/Texture Modifications --
#define LBVH
#define AccurateEmissionTex
// #define PointFiltering
// #define UseTextureLOD
// #define DisableNormalMaps
// #define ClayMetalOverride
// #define IgnoreBackfacingEmissive

// -- Shading Modifications --
#define EONDiffuse
#define ClampRoughnessToBounce
// #define SkyboxMeshPortals
#define UseBRDFLights
#define DoubleBufferSGTree
// #define RadCache
// #define MoreAO
// #define YanusMode

// -- ReSTIR Modifiers --
#define ReSTIRAdvancedValidation
#define ReSTIRAdaptiveLighting
#define ReSTIRAdaptiveUnityLights
// #define ExpensiveAdaptiveTemporal
#define ReSTIRShadowBorderImprovement
// #define ReSTIRSampleReduction



//END OF MODIFIABLE DEFINES
//DEBUG VIEW DEFINES
#define DebugView DVNone
//Replace DVNone(^) with any of the DV Defines below(Make sure "UseDebugTexture" is ENABLED under the RayTracingMaster script on the Scene gameobject)

#define DVNone -1
#define DVMatID 0
#define DVMeshID 1
#define DVTriID 2
#define DVAlbID 3
#define DVBVHView 4
#define DVGIView 5
#define DVDepthView 6
#define DVRadCache 7
#define DVBVHViewAdvanced 8
#define DVGeomNorm 9
#define DVSurfNorm 10

#define DepthDivisor 1000.0f

#define FogScale 12.0f

#define JitterSize 1.0f


//Dont change the ones below
#define DisneyIndex 0
#define CutoutIndex 1
#define FadeIndex 2

#define POINTLIGHT 0
#define DIRECTIONALLIGHT 1
#define SPOTLIGHT 2
#define AREALIGHTQUAD 3
#define AREALIGHTDISK 4
#define TRILIGHT 5


#define MaxTraversalSamples 1000
#define NormalOffset 0.0001f
#define ShadowDistanceFudgeFactor 0.0001f



#define IsEmissionMask 0
#define BaseIsMap 1
#define ReplaceBase 2
#define UseSmoothness 3
#define InvertSmoothnessTexture 4
#define IsBackground 5
#define ShadowCaster 6
#define Invisible 7
#define BackgrounBleed 8
#define Thin 9
#define VertexColors 10
#define InvertAlpha 11
#define EnableCausticGeneration 12
#define DisableCausticRecieving 13

#define SampleAlbedo 0
#define SampleMetallic 1
#define SampleRoughness 2
#define SampleEmission 3
#define SampleNormal 4
#define SampleAlpha 5
#define SampleIES 6
#define SampleMatCap 7
#define SampleMatCapMask 8
#define SampleTerrainAlbedo 9
#define SampleSecondaryAlbedo 10
#define SampleSecondaryAlbedoMask 11
#define SampleDetailNormal 12
#define SampleDiffTrans 13
#define SampleDisplacement 14

#define BlendModeLerp 0
#define BlendModeAdd 1
#define BlendModeMult 2

bool GetFlag(int FlagVar, int flag) {
    return (((int)FlagVar >> flag) & (int)1) == 1;
}

int GetFlagStretch(int FlagVar, int LeftOffset, int Stride) {
    return ((FlagVar >> (32 - LeftOffset - Stride)) & ((1 << (Stride)) - 1));
}


