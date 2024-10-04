// #define HardwareRT
// #define HDRP
// #define DX11
#define UseBindless
//Dont modify above, CPU code will do automatically
#define AdvancedAlphaMapped
#define ExtraSampleValidation
#define IgnoreGlassShadow
// #define IgnoreGlassMain
// #define FadeMapping
// #define PointFiltering
#define StainedGlassShadows
// #define IgnoreBackfacing
// #define WhiteLights
#define LBVH
// #define FasterLightSampling
#define AccurateEmissionTex
#define RadianceCache
// #define ImprovedRadCacheSpecularResponse
// #define HighSpeedRadCache
#define IndirectRetraceWeighting
#define TrueBlack
// #define AdvancedRadCacheAlt
// #define UseTextureLOD
// #define vMFDiffuse
#define AdvancedBackground

//END OF DEFINES
//DEBUG VIEW DEFINES
#define DebugView DVNone
//Replace DVNone(^) with any of the DV Defines below

#define DVNone -1
#define DVMatID 0
#define DVMeshID 1
#define DVTriID 2
#define DVAlbID 3
#define DVBVHView 4
#define DVRadCache 5
#define DVGIView 6

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

#define NormalOffset 0.0001f

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

#define BlendModeLerp 0
#define BlendModeAdd 1
#define BlendModeMult 2

bool GetFlag(int FlagVar, int flag) {
    return (((int)FlagVar >> flag) & (int)1) == 1;
}

int GetFlagStretch(int FlagVar, int LeftOffset, int Stride) {
    return ((FlagVar >> (32 - LeftOffset - Stride)) & ((1 << (Stride)) - 1));
}

#define MaxTraversalSamples 1000
