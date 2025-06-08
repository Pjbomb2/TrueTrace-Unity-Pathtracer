using System;
using System.Runtime.InteropServices;

namespace DenoiserPlugin
{
public static partial class DlssSdk
{

//  Version Notes:
//      Version 0x0000014:
//          * Added a logging callback that the app may pass in on init
//          * Added ability for the app to override the logging level
//      Version 0x0000015:
//          * Support multiple GPUs (bug 3270533)
public const uint NVSDK_NGX_VERSION_API_MACRO = 0x0000015;  // NGX_VERSION_DOT 1.5.0

// AppId typedef unsigned long long AppId; // In C++. In C#, use ulong directly or define a specific type if widely used.

public enum NVSDK_NGX_DLSS_Hint_Render_Preset
{
    NVSDK_NGX_DLSS_Hint_Render_Preset_Default,     // default behavior, may or may not change after OTA
    NVSDK_NGX_DLSS_Hint_Render_Preset_A,
    NVSDK_NGX_DLSS_Hint_Render_Preset_B,
    NVSDK_NGX_DLSS_Hint_Render_Preset_C,
    NVSDK_NGX_DLSS_Hint_Render_Preset_D,
    NVSDK_NGX_DLSS_Hint_Render_Preset_E,
    NVSDK_NGX_DLSS_Hint_Render_Preset_F,
    NVSDK_NGX_DLSS_Hint_Render_Preset_G,           // do not use, reverts to default behavior
    NVSDK_NGX_DLSS_Hint_Render_Preset_H_Reserved,  // do not use, reverts to default behavior
    NVSDK_NGX_DLSS_Hint_Render_Preset_I_Reserved,  // do not use, reverts to default behavior
    NVSDK_NGX_DLSS_Hint_Render_Preset_J,
    NVSDK_NGX_DLSS_Hint_Render_Preset_K,
    NVSDK_NGX_DLSS_Hint_Render_Preset_L,           // do not use, reverts to default behavior
    NVSDK_NGX_DLSS_Hint_Render_Preset_M,           // do not use, reverts to default behavior
    NVSDK_NGX_DLSS_Hint_Render_Preset_N,           // do not use, reverts to default behavior
    NVSDK_NGX_DLSS_Hint_Render_Preset_O,           // do not use, reverts to default behavior
}

// typedef struct NVSDK_NGX_FeatureCommonInfo_Internal NVSDK_NGX_FeatureCommonInfo_Internal; // Excluded

public enum NVSDK_NGX_Version : uint
{ 
    NVSDK_NGX_Version_API = NVSDK_NGX_VERSION_API_MACRO
}

public enum NVSDK_NGX_Result : int
{
    NVSDK_NGX_Result_Success = 0x1,

    NVSDK_NGX_Result_Fail = unchecked((int)0xBAD00000),

    // Feature is not supported on current hardware
    NVSDK_NGX_Result_FAIL_FeatureNotSupported = NVSDK_NGX_Result_Fail | 1,

    // Platform error - for example - check d3d12 debug layer log for more information
    NVSDK_NGX_Result_FAIL_PlatformError = NVSDK_NGX_Result_Fail | 2,

    // Feature with given parameters already exists
    NVSDK_NGX_Result_FAIL_FeatureAlreadyExists = NVSDK_NGX_Result_Fail | 3,

    // Feature with provided handle does not exist
    NVSDK_NGX_Result_FAIL_FeatureNotFound = NVSDK_NGX_Result_Fail | 4,

    // Invalid parameter was provided
    NVSDK_NGX_Result_FAIL_InvalidParameter = NVSDK_NGX_Result_Fail | 5,

    // Provided buffer is too small, please use size provided by NVSDK_NGX_GetScratchBufferSize
    NVSDK_NGX_Result_FAIL_ScratchBufferTooSmall = NVSDK_NGX_Result_Fail | 6,

    // SDK was not initialized properly
    NVSDK_NGX_Result_FAIL_NotInitialized = NVSDK_NGX_Result_Fail | 7,

    //  Unsupported format used for input/output buffers
    NVSDK_NGX_Result_FAIL_UnsupportedInputFormat = NVSDK_NGX_Result_Fail | 8,

    // Feature input/output needs RW access (UAV) (d3d11/d3d12 specific)
    NVSDK_NGX_Result_FAIL_RWFlagMissing = NVSDK_NGX_Result_Fail | 9,

    // Feature was created with specific input but none is provided at evaluation
    NVSDK_NGX_Result_FAIL_MissingInput = NVSDK_NGX_Result_Fail | 10,

    // Feature is not available on the system
    NVSDK_NGX_Result_FAIL_UnableToInitializeFeature = NVSDK_NGX_Result_Fail | 11,

    // NGX system libraries are old and need an update
    NVSDK_NGX_Result_FAIL_OutOfDate = NVSDK_NGX_Result_Fail | 12,

    // Feature requires more GPU memory than it is available on system
    NVSDK_NGX_Result_FAIL_OutOfGPUMemory = NVSDK_NGX_Result_Fail | 13,

    // Format used in input buffer(s) is not supported by feature
    NVSDK_NGX_Result_FAIL_UnsupportedFormat = NVSDK_NGX_Result_Fail | 14,

    // Path provided in InApplicationDataPath cannot be written to
    NVSDK_NGX_Result_FAIL_UnableToWriteToAppDataPath = NVSDK_NGX_Result_Fail | 15,

    // Unsupported parameter was provided (e.g. specific scaling factor is unsupported)
    NVSDK_NGX_Result_FAIL_UnsupportedParameter = NVSDK_NGX_Result_Fail | 16,

    // The feature or application was denied (contact NVIDIA for further details)
    NVSDK_NGX_Result_FAIL_Denied = NVSDK_NGX_Result_Fail | 17,

    // The feature or functionality is not implemented
    NVSDK_NGX_Result_FAIL_NotImplemented = NVSDK_NGX_Result_Fail | 18,
}

public static bool NVSDK_NGX_SUCCEED(NVSDK_NGX_Result value) => (int)((int)value & 0xFFF00000) != (int)NVSDK_NGX_Result.NVSDK_NGX_Result_Fail;
public static bool NVSDK_NGX_FAILED(NVSDK_NGX_Result value) => (int)((int)value & 0xFFF00000) == (int)NVSDK_NGX_Result.NVSDK_NGX_Result_Fail;

public enum NVSDK_NGX_Feature
{
    NVSDK_NGX_Feature_Reserved0             = 0,

    NVSDK_NGX_Feature_SuperSampling         = 1,

    NVSDK_NGX_Feature_InPainting            = 2,

    NVSDK_NGX_Feature_ImageSuperResolution  = 3,

    NVSDK_NGX_Feature_SlowMotion            = 4,

    NVSDK_NGX_Feature_VideoSuperResolution  = 5,

    NVSDK_NGX_Feature_Reserved1             = 6,

    NVSDK_NGX_Feature_Reserved2             = 7,

    NVSDK_NGX_Feature_Reserved3             = 8,

    NVSDK_NGX_Feature_ImageSignalProcessing = 9,

    NVSDK_NGX_Feature_DeepResolve           = 10,

    NVSDK_NGX_Feature_FrameGeneration       = 11,

    NVSDK_NGX_Feature_DeepDVC               = 12,

    NVSDK_NGX_Feature_RayReconstruction     = 13,

    NVSDK_NGX_Feature_Reserved14            = 14,

    NVSDK_NGX_Feature_Reserved15            = 15,

    NVSDK_NGX_Feature_Reserved16            = 16,

    // New features go here
    NVSDK_NGX_Feature_Count,

    // These members are not strictly NGX features, but are
    // components of the NGX system, and it may sometimes
    // be useful to identify them using the same enum
    NVSDK_NGX_Feature_Reserved_SDK          = 32764,

    NVSDK_NGX_Feature_Reserved_Core         = 32765,

    NVSDK_NGX_Feature_Reserved_Unknown      = 32766
}

//TODO create grayscale format (R32F?)
public enum NVSDK_NGX_Buffer_Format
{
    NVSDK_NGX_Buffer_Format_Unknown,
    NVSDK_NGX_Buffer_Format_RGB8UI,
    NVSDK_NGX_Buffer_Format_RGB16F,
    NVSDK_NGX_Buffer_Format_RGB32F,
    NVSDK_NGX_Buffer_Format_RGBA8UI,
    NVSDK_NGX_Buffer_Format_RGBA16F,
    NVSDK_NGX_Buffer_Format_RGBA32F,
}

public enum NVSDK_NGX_PerfQuality_Value
{
    NVSDK_NGX_PerfQuality_Value_MaxPerf,
    NVSDK_NGX_PerfQuality_Value_Balanced,
    NVSDK_NGX_PerfQuality_Value_MaxQuality,
    // Extended PerfQuality modes
    NVSDK_NGX_PerfQuality_Value_UltraPerformance,
    NVSDK_NGX_PerfQuality_Value_UltraQuality,
    NVSDK_NGX_PerfQuality_Value_DLAA,
}

public enum NVSDK_NGX_RTX_Value
{
    NVSDK_NGX_RTX_Value_Off,
    NVSDK_NGX_RTX_Value_On,
}

public enum NVSDK_NGX_DLSS_Mode
{
    NVSDK_NGX_DLSS_Mode_Off,        // use existing in-engine AA + upscale solution
    NVSDK_NGX_DLSS_Mode_DLSS_DLISP,
    NVSDK_NGX_DLSS_Mode_DLISP_Only, // use existing in-engine AA solution
    NVSDK_NGX_DLSS_Mode_DLSS,       // DLSS will apply AA and upsample at the same time
}

[StructLayout(LayoutKind.Sequential)]
public struct NVSDK_NGX_Handle 
{ 
    public uint Id; 
}

public enum NVSDK_NGX_GPU_Arch
{
    NVSDK_NGX_GPU_Arch_NotSupported = 0,

    // Match NvAPI's NV_GPU_ARCHITECTURE_ID values for GV100 and TU100 for
    // backwards compatibility with snippets built against NvAPI
    NVSDK_NGX_GPU_Arch_Volta        = 0x0140,
    NVSDK_NGX_GPU_Arch_Turing       = 0x0160,
    NVSDK_NGX_GPU_Arch_Ampere       = 0x0170,
    NVSDK_NGX_GPU_Arch_Ada          = 0x0190,
    NVSDK_NGX_GPU_Arch_Hopper       = 0x0180,
    NVSDK_NGX_GPU_Arch_Blackwell    = 0x01A0,
    NVSDK_NGX_GPU_Arch_Blackwell2   = 0x01B0,

    // Presumably something newer
    NVSDK_NGX_GPU_Arch_Unknown      = 0x7FFFFFF
}

[Flags] // DLSS Feature Flags are bit flags
public enum NVSDK_NGX_DLSS_Feature_Flags : uint // Explicitly uint as C++ shifts into MSB of 32-bit int
{
    NVSDK_NGX_DLSS_Feature_Flags_IsInvalid      = 1u << 31, // Use 'u' for unsigned literal
    NVSDK_NGX_DLSS_Feature_Flags_None           = 0,
    NVSDK_NGX_DLSS_Feature_Flags_IsHDR          = 1u << 0,
    NVSDK_NGX_DLSS_Feature_Flags_MVLowRes       = 1u << 1,
    NVSDK_NGX_DLSS_Feature_Flags_MVJittered     = 1u << 2,
    NVSDK_NGX_DLSS_Feature_Flags_DepthInverted  = 1u << 3,
    NVSDK_NGX_DLSS_Feature_Flags_Reserved_0     = 1u << 4,
    NVSDK_NGX_DLSS_Feature_Flags_DoSharpening   = 1u << 5,
    NVSDK_NGX_DLSS_Feature_Flags_AutoExposure   = 1u << 6,
    NVSDK_NGX_DLSS_Feature_Flags_AlphaUpscaling = 1u << 7,
}

public enum NVSDK_NGX_ToneMapperType
{
    NVSDK_NGX_TONEMAPPER_STRING = 0,
    NVSDK_NGX_TONEMAPPER_REINHARD,
    NVSDK_NGX_TONEMAPPER_ONEOVERLUMA,
    NVSDK_NGX_TONEMAPPER_ACES,
    NVSDK_NGX_TONEMAPPERTYPE_NUM
}

public enum NVSDK_NGX_GBufferType
{
    NVSDK_NGX_GBUFFER_ALBEDO = 0,
    NVSDK_NGX_GBUFFER_ROUGHNESS,
    NVSDK_NGX_GBUFFER_METALLIC,
    NVSDK_NGX_GBUFFER_SPECULAR,
    NVSDK_NGX_GBUFFER_SUBSURFACE,
    NVSDK_NGX_GBUFFER_NORMALS,
    NVSDK_NGX_GBUFFER_SHADINGMODELID,  /* unique identifier for drawn object or how the object is drawn */
    NVSDK_NGX_GBUFFER_MATERIALID, /* unique identifier for material */
    NVSDK_NGX_GBUFFER_SPECULAR_ALBEDO,
    NVSDK_NGX_GBUFFER_INDIRECT_ALBEDO,
    NVSDK_NGX_GBUFFER_SPECULAR_MVEC,
    NVSDK_NGX_GBUFFER_DISOCCL_MASK,
    NVSDK_NGX_GBUFFER_EMISSIVE,
    NVSDK_NGX_GBUFFERTYPE_NUM = 16
}

[StructLayout(LayoutKind.Sequential)]
public struct NVSDK_NGX_Coordinates
{
    public uint X;
    public uint Y;
}

[StructLayout(LayoutKind.Sequential)]
public struct NVSDK_NGX_Dimensions
{
    public uint Width;
    public uint Height;
}

[StructLayout(LayoutKind.Sequential)]
public struct NVSDK_NGX_PrecisionInfo
{
    // 1 if and only if the associated resource buffer is considered low-precision
    public uint IsLowPrecision;

    // Bias and Scale values, such that `hi = lo * Scale + Bias`
    public float Bias;
    public float Scale;
}

// typedef struct NVSDK_NGX_PathListInfo -> Excluded
// typedef NVSDK_NGX_AppLogCallback -> Excluded
// typedef struct NVSDK_NGX_LoggingInfo -> Excluded
// typedef struct NVSDK_NGX_FeatureCommonInfo -> Excluded

public enum NVSDK_NGX_Logging_Level
{
    NVSDK_NGX_LOGGING_LEVEL_OFF = 0,
    NVSDK_NGX_LOGGING_LEVEL_ON = 1,
    NVSDK_NGX_LOGGING_LEVEL_VERBOSE = 2
}

public enum NVSDK_NGX_Resource_VK_Type
{
    NVSDK_NGX_RESOURCE_VK_TYPE_VK_IMAGEVIEW,
    NVSDK_NGX_RESOURCE_VK_TYPE_VK_BUFFER
}

public enum NVSDK_NGX_Opt_Level
{
    NVSDK_NGX_OPT_LEVEL_UNDEFINED = 0,
    NVSDK_NGX_OPT_LEVEL_DEBUG = 20,
    NVSDK_NGX_OPT_LEVEL_DEVELOP = 30,
    NVSDK_NGX_OPT_LEVEL_RELEASE = 40
}

public enum NVSDK_NGX_EngineType
{
    NVSDK_NGX_ENGINE_TYPE_CUSTOM = 0,
    NVSDK_NGX_ENGINE_TYPE_UNREAL,
    NVSDK_NGX_ENGINE_TYPE_UNITY,
    NVSDK_NGX_ENGINE_TYPE_OMNIVERSE,
    NVSDK_NGX_ENGINE_COUNT
}

[Flags] // Feature Support Result are bit flags
public enum NVSDK_NGX_Feature_Support_Result
{
    NVSDK_NGX_FeatureSupportResult_Supported = 0,
    NVSDK_NGX_FeatureSupportResult_CheckNotPresent = 1,
    NVSDK_NGX_FeatureSupportResult_DriverVersionUnsupported = 2,
    NVSDK_NGX_FeatureSupportResult_AdapterUnsupported = 4,
    NVSDK_NGX_FeatureSupportResult_OSVersionBelowMinimumSupported = 8,
    NVSDK_NGX_FeatureSupportResult_NotImplemented = 16
}

public enum NVSDK_NGX_Application_Identifier_Type
{
    NVSDK_NGX_Application_Identifier_Type_Application_Id = 0,
    NVSDK_NGX_Application_Identifier_Type_Project_Id = 1,
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct NVSDK_NGX_ProjectIdDescription
{
    [MarshalAs(UnmanagedType.LPStr)]
    public string ProjectId;
    public NVSDK_NGX_EngineType EngineType;
    [MarshalAs(UnmanagedType.LPStr)]
    public string EngineVersion;
}

[StructLayout(LayoutKind.Explicit)]
public struct NVSDK_NGX_Application_Identifier_Union
{
    [FieldOffset(0)]
    public NVSDK_NGX_ProjectIdDescription ProjectDesc;
    [FieldOffset(0)]
    public ulong ApplicationId;
}

[StructLayout(LayoutKind.Sequential)]
public struct NVSDK_NGX_Application_Identifier
{
    public NVSDK_NGX_Application_Identifier_Type IdentifierType;
    public NVSDK_NGX_Application_Identifier_Union v;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] // For ApplicationDataPath
public struct NVSDK_NGX_FeatureDiscoveryInfo
{
    public NVSDK_NGX_Version SDKVersion;
    public NVSDK_NGX_Feature FeatureID;
    public NVSDK_NGX_Application_Identifier Identifier;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string ApplicationDataPath;
    public IntPtr FeatureInfo; // const NVSDK_NGX_FeatureCommonInfo* FeatureInfo; // Excluded type, so IntPtr
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)] // For MinOSVersion
public struct NVSDK_NGX_FeatureRequirement
{
    // Bitfield of bit shifted values specified in NVSDK_NGX_Feature_Support_Result. 0 if Feature is Supported.
    public NVSDK_NGX_Feature_Support_Result FeatureSupported;
	
    // Returned HW Architecture value corresponding to NV_GPU_ARCHITECTURE_ID values defined in NvAPI GPU Framework.
    public uint MinHWArchitecture;

    // Value corresponding to minimum OS version required for NGX Feature Support
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
    public string MinOSVersion;
}

// Read-only parameters provided by NGX
public const string NVSDK_NGX_EParameter_Reserved00 = "#\x00";
public const string NVSDK_NGX_EParameter_SuperSampling_Available = "#\x01";
public const string NVSDK_NGX_EParameter_InPainting_Available = "#\x02";
public const string NVSDK_NGX_EParameter_ImageSuperResolution_Available = "#\x03";
public const string NVSDK_NGX_EParameter_SlowMotion_Available = "#\x04";
public const string NVSDK_NGX_EParameter_VideoSuperResolution_Available = "#\x05";
public const string NVSDK_NGX_EParameter_Reserved06 = "#\x06";
public const string NVSDK_NGX_EParameter_Reserved07 = "#\x07";
public const string NVSDK_NGX_EParameter_Reserved08 = "#\x08";
public const string NVSDK_NGX_EParameter_ImageSignalProcessing_Available = "#\x09";
public const string NVSDK_NGX_EParameter_ImageSuperResolution_ScaleFactor_2_1 = "#\x0a";
public const string NVSDK_NGX_EParameter_ImageSuperResolution_ScaleFactor_3_1 = "#\x0b";
public const string NVSDK_NGX_EParameter_ImageSuperResolution_ScaleFactor_3_2 = "#\x0c";
public const string NVSDK_NGX_EParameter_ImageSuperResolution_ScaleFactor_4_3 = "#\x0d";
public const string NVSDK_NGX_EParameter_NumFrames = "#\x0e";
public const string NVSDK_NGX_EParameter_Scale = "#\x0f";
public const string NVSDK_NGX_EParameter_Width = "#\x10";
public const string NVSDK_NGX_EParameter_Height = "#\x11";
public const string NVSDK_NGX_EParameter_OutWidth = "#\x12";
public const string NVSDK_NGX_EParameter_OutHeight = "#\x13";
public const string NVSDK_NGX_EParameter_Sharpness = "#\x14";
public const string NVSDK_NGX_EParameter_Scratch = "#\x15";
public const string NVSDK_NGX_EParameter_Scratch_SizeInBytes = "#\x16";
public const string NVSDK_NGX_EParameter_EvaluationNode = "#\x17"; // valid since API 0x13 (replaced a deprecated param)
public const string NVSDK_NGX_EParameter_Input1 = "#\x18";
public const string NVSDK_NGX_EParameter_Input1_Format = "#\x19";
public const string NVSDK_NGX_EParameter_Input1_SizeInBytes = "#\x1a";
public const string NVSDK_NGX_EParameter_Input2 = "#\x1b";
public const string NVSDK_NGX_EParameter_Input2_Format = "#\x1c";
public const string NVSDK_NGX_EParameter_Input2_SizeInBytes = "#\x1d";
public const string NVSDK_NGX_EParameter_Color = "#\x1e";
public const string NVSDK_NGX_EParameter_Color_Format = "#\x1f";
public const string NVSDK_NGX_EParameter_Color_SizeInBytes = "#\x20";
public const string NVSDK_NGX_EParameter_Albedo = "#\x21";
public const string NVSDK_NGX_EParameter_Output = "#\x22";
public const string NVSDK_NGX_EParameter_Output_Format = "#\x23";
public const string NVSDK_NGX_EParameter_Output_SizeInBytes = "#\x24";
public const string NVSDK_NGX_EParameter_Reset = "#\x25";
public const string NVSDK_NGX_EParameter_BlendFactor = "#\x26";
public const string NVSDK_NGX_EParameter_MotionVectors = "#\x27";
public const string NVSDK_NGX_EParameter_Rect_X = "#\x28";
public const string NVSDK_NGX_EParameter_Rect_Y = "#\x29";
public const string NVSDK_NGX_EParameter_Rect_W = "#\x2a";
public const string NVSDK_NGX_EParameter_Rect_H = "#\x2b";
public const string NVSDK_NGX_EParameter_MV_Scale_X = "#\x2c";
public const string NVSDK_NGX_EParameter_MV_Scale_Y = "#\x2d";
public const string NVSDK_NGX_EParameter_Model = "#\x2e";
public const string NVSDK_NGX_EParameter_Format = "#\x2f";
public const string NVSDK_NGX_EParameter_SizeInBytes = "#\x30";
public const string NVSDK_NGX_EParameter_ResourceAllocCallback = "#\x31";
public const string NVSDK_NGX_EParameter_BufferAllocCallback = "#\x32";
public const string NVSDK_NGX_EParameter_Tex2DAllocCallback = "#\x33";
public const string NVSDK_NGX_EParameter_ResourceReleaseCallback = "#\x34";
public const string NVSDK_NGX_EParameter_CreationNodeMask = "#\x35";
public const string NVSDK_NGX_EParameter_VisibilityNodeMask = "#\x36";
public const string NVSDK_NGX_EParameter_PreviousOutput = "#\x37";
public const string NVSDK_NGX_EParameter_MV_Offset_X = "#\x38";
public const string NVSDK_NGX_EParameter_MV_Offset_Y = "#\x39";
public const string NVSDK_NGX_EParameter_Hint_UseFireflySwatter = "#\x3a";
public const string NVSDK_NGX_EParameter_Resource_Width = "#\x3b";
public const string NVSDK_NGX_EParameter_Resource_Height = "#\x3c";
public const string NVSDK_NGX_EParameter_Depth = "#\x3d";
public const string NVSDK_NGX_EParameter_DLSSOptimalSettingsCallback = "#\x3e";
public const string NVSDK_NGX_EParameter_PerfQualityValue = "#\x3f";
public const string NVSDK_NGX_EParameter_RTXValue = "#\x40";
public const string NVSDK_NGX_EParameter_DLSSMode = "#\x41";
public const string NVSDK_NGX_EParameter_DeepResolve_Available = "#\x42";
public const string NVSDK_NGX_EParameter_Deprecated_43 = "#\x43";
public const string NVSDK_NGX_EParameter_OptLevel = "#\x44";
public const string NVSDK_NGX_EParameter_IsDevSnippetBranch = "#\x45";
public const string NVSDK_NGX_EParameter_DeepDVC_Available = "#\x46";
public const string NVSDK_NGX_EParameter_Graphics_API = "#\x47";
public const string NVSDK_NGX_EParameter_Reserved_48 = "#\x48";
public const string NVSDK_NGX_EParameter_Reserved_49 = "#\x49";

public const string NVSDK_NGX_Parameter_OptLevel = "Snippet.OptLevel";
public const string NVSDK_NGX_Parameter_IsDevSnippetBranch = "Snippet.IsDevBranch";
public const string NVSDK_NGX_Parameter_SuperSampling_ScaleFactor = "SuperSampling.ScaleFactor";
public const string NVSDK_NGX_Parameter_ImageSignalProcessing_ScaleFactor = "ImageSignalProcessing.ScaleFactor";
public const string NVSDK_NGX_Parameter_SuperSampling_Available = "SuperSampling.Available";
public const string NVSDK_NGX_Parameter_InPainting_Available = "InPainting.Available";
public const string NVSDK_NGX_Parameter_ImageSuperResolution_Available = "ImageSuperResolution.Available";
public const string NVSDK_NGX_Parameter_SlowMotion_Available = "SlowMotion.Available";
public const string NVSDK_NGX_Parameter_VideoSuperResolution_Available = "VideoSuperResolution.Available";
public const string NVSDK_NGX_Parameter_ImageSignalProcessing_Available = "ImageSignalProcessing.Available";
public const string NVSDK_NGX_Parameter_DeepResolve_Available = "DeepResolve.Available";
public const string NVSDK_NGX_Parameter_SuperSampling_NeedsUpdatedDriver = "SuperSampling.NeedsUpdatedDriver";
public const string NVSDK_NGX_Parameter_InPainting_NeedsUpdatedDriver = "InPainting.NeedsUpdatedDriver";
public const string NVSDK_NGX_Parameter_ImageSuperResolution_NeedsUpdatedDriver = "ImageSuperResolution.NeedsUpdatedDriver";
public const string NVSDK_NGX_Parameter_SlowMotion_NeedsUpdatedDriver = "SlowMotion.NeedsUpdatedDriver";
public const string NVSDK_NGX_Parameter_VideoSuperResolution_NeedsUpdatedDriver = "VideoSuperResolution.NeedsUpdatedDriver";
public const string NVSDK_NGX_Parameter_ImageSignalProcessing_NeedsUpdatedDriver = "ImageSignalProcessing.NeedsUpdatedDriver";
public const string NVSDK_NGX_Parameter_DeepResolve_NeedsUpdatedDriver = "DeepResolve.NeedsUpdatedDriver";
public const string NVSDK_NGX_Parameter_FrameInterpolation_NeedsUpdatedDriver = "FrameInterpolation.NeedsUpdatedDriver";
public const string NVSDK_NGX_Parameter_SuperSampling_MinDriverVersionMajor = "SuperSampling.MinDriverVersionMajor";
public const string NVSDK_NGX_Parameter_InPainting_MinDriverVersionMajor = "InPainting.MinDriverVersionMajor";
public const string NVSDK_NGX_Parameter_ImageSuperResolution_MinDriverVersionMajor = "ImageSuperResolution.MinDriverVersionMajor";
public const string NVSDK_NGX_Parameter_SlowMotion_MinDriverVersionMajor = "SlowMotion.MinDriverVersionMajor";
public const string NVSDK_NGX_Parameter_VideoSuperResolution_MinDriverVersionMajor = "VideoSuperResolution.MinDriverVersionMajor";
public const string NVSDK_NGX_Parameter_ImageSignalProcessing_MinDriverVersionMajor = "ImageSignalProcessing.MinDriverVersionMajor";
public const string NVSDK_NGX_Parameter_DeepResolve_MinDriverVersionMajor = "DeepResolve.MinDriverVersionMajor";
public const string NVSDK_NGX_Parameter_FrameInterpolation_MinDriverVersionMajor = "FrameInterpolation.MinDriverVersionMajor";
public const string NVSDK_NGX_Parameter_SuperSampling_MinDriverVersionMinor = "SuperSampling.MinDriverVersionMinor";
public const string NVSDK_NGX_Parameter_InPainting_MinDriverVersionMinor = "InPainting.MinDriverVersionMinor";
public const string NVSDK_NGX_Parameter_ImageSuperResolution_MinDriverVersionMinor = "ImageSuperResolution.MinDriverVersionMinor";
public const string NVSDK_NGX_Parameter_SlowMotion_MinDriverVersionMinor = "SlowMotion.MinDriverVersionMinor";
public const string NVSDK_NGX_Parameter_VideoSuperResolution_MinDriverVersionMinor = "VideoSuperResolution.MinDriverVersionMinor";
public const string NVSDK_NGX_Parameter_ImageSignalProcessing_MinDriverVersionMinor = "ImageSignalProcessing.MinDriverVersionMinor";
public const string NVSDK_NGX_Parameter_DeepResolve_MinDriverVersionMinor = "DeepResolve.MinDriverVersionMinor";
public const string NVSDK_NGX_Parameter_SuperSampling_FeatureInitResult = "SuperSampling.FeatureInitResult";
public const string NVSDK_NGX_Parameter_InPainting_FeatureInitResult = "InPainting.FeatureInitResult";
public const string NVSDK_NGX_Parameter_ImageSuperResolution_FeatureInitResult = "ImageSuperResolution.FeatureInitResult";
public const string NVSDK_NGX_Parameter_SlowMotion_FeatureInitResult = "SlowMotion.FeatureInitResult";
public const string NVSDK_NGX_Parameter_VideoSuperResolution_FeatureInitResult = "VideoSuperResolution.FeatureInitResult";
public const string NVSDK_NGX_Parameter_ImageSignalProcessing_FeatureInitResult = "ImageSignalProcessing.FeatureInitResult";
public const string NVSDK_NGX_Parameter_DeepResolve_FeatureInitResult = "DeepResolve.FeatureInitResult";
public const string NVSDK_NGX_Parameter_FrameInterpolation_FeatureInitResult = "FrameInterpolation.FeatureInitResult";
public const string NVSDK_NGX_Parameter_ImageSuperResolution_ScaleFactor_2_1 = "ImageSuperResolution.ScaleFactor.2.1";
public const string NVSDK_NGX_Parameter_ImageSuperResolution_ScaleFactor_3_1 = "ImageSuperResolution.ScaleFactor.3.1";
public const string NVSDK_NGX_Parameter_ImageSuperResolution_ScaleFactor_3_2 = "ImageSuperResolution.ScaleFactor.3.2";
public const string NVSDK_NGX_Parameter_ImageSuperResolution_ScaleFactor_4_3 = "ImageSuperResolution.ScaleFactor.4.3";
public const string NVSDK_NGX_Parameter_NumFrames = "NumFrames";
public const string NVSDK_NGX_Parameter_Scale = "Scale";
public const string NVSDK_NGX_Parameter_Width = "Width";
public const string NVSDK_NGX_Parameter_Height = "Height";
public const string NVSDK_NGX_Parameter_OutWidth = "OutWidth";
public const string NVSDK_NGX_Parameter_OutHeight = "OutHeight";
public const string NVSDK_NGX_Parameter_Sharpness = "Sharpness";
public const string NVSDK_NGX_Parameter_Scratch = "Scratch";
public const string NVSDK_NGX_Parameter_Scratch_SizeInBytes = "Scratch.SizeInBytes";
public const string NVSDK_NGX_Parameter_Input1 = "Input1";
public const string NVSDK_NGX_Parameter_Input1_Format = "Input1.Format";
public const string NVSDK_NGX_Parameter_Input1_SizeInBytes = "Input1.SizeInBytes";
public const string NVSDK_NGX_Parameter_Input2 = "Input2";
public const string NVSDK_NGX_Parameter_Input2_Format = "Input2.Format";
public const string NVSDK_NGX_Parameter_Input2_SizeInBytes = "Input2.SizeInBytes";
public const string NVSDK_NGX_Parameter_Color = "Color";
public const string NVSDK_NGX_Parameter_Color_Format = "Color.Format";
public const string NVSDK_NGX_Parameter_Color_SizeInBytes = "Color.SizeInBytes";
public const string NVSDK_NGX_Parameter_FI_Color1 = "Color1";
public const string NVSDK_NGX_Parameter_FI_Color2 = "Color2";
public const string NVSDK_NGX_Parameter_Albedo = "Albedo";
public const string NVSDK_NGX_Parameter_Output = "Output";
public const string NVSDK_NGX_Parameter_Output_Format = "Output.Format";
public const string NVSDK_NGX_Parameter_Output_SizeInBytes = "Output.SizeInBytes";
public const string NVSDK_NGX_Parameter_FI_Output1 = "Output1";
public const string NVSDK_NGX_Parameter_FI_Output2 = "Output2";
public const string NVSDK_NGX_Parameter_FI_Output3 = "Output3";
public const string NVSDK_NGX_Parameter_Reset = "Reset";
public const string NVSDK_NGX_Parameter_BlendFactor = "BlendFactor";
public const string NVSDK_NGX_Parameter_MotionVectors = "MotionVectors";
public const string NVSDK_NGX_Parameter_FI_MotionVectors1 = "MotionVectors1";
public const string NVSDK_NGX_Parameter_FI_MotionVectors2 = "MotionVectors2";
public const string NVSDK_NGX_Parameter_Rect_X = "Rect.X";
public const string NVSDK_NGX_Parameter_Rect_Y = "Rect.Y";
public const string NVSDK_NGX_Parameter_Rect_W = "Rect.W";
public const string NVSDK_NGX_Parameter_Rect_H = "Rect.H";
public const string NVSDK_NGX_Parameter_OutRect_X = "OutRect.X";
public const string NVSDK_NGX_Parameter_OutRect_Y = "OutRect.Y";
public const string NVSDK_NGX_Parameter_OutRect_W = "OutRect.W";
public const string NVSDK_NGX_Parameter_OutRect_H = "OutRect.H";
public const string NVSDK_NGX_Parameter_MV_Scale_X = "MV.Scale.X";
public const string NVSDK_NGX_Parameter_MV_Scale_Y = "MV.Scale.Y";
public const string NVSDK_NGX_Parameter_Model = "Model";
public const string NVSDK_NGX_Parameter_Format = "Format";
public const string NVSDK_NGX_Parameter_SizeInBytes = "SizeInBytes";
public const string NVSDK_NGX_Parameter_ResourceAllocCallback = "ResourceAllocCallback";
public const string NVSDK_NGX_Parameter_BufferAllocCallback = "BufferAllocCallback";
public const string NVSDK_NGX_Parameter_Tex2DAllocCallback = "Tex2DAllocCallback";
public const string NVSDK_NGX_Parameter_ResourceReleaseCallback = "ResourceReleaseCallback";
public const string NVSDK_NGX_Parameter_CreationNodeMask = "CreationNodeMask";
public const string NVSDK_NGX_Parameter_VisibilityNodeMask = "VisibilityNodeMask";
public const string NVSDK_NGX_Parameter_MV_Offset_X = "MV.Offset.X";
public const string NVSDK_NGX_Parameter_MV_Offset_Y = "MV.Offset.Y";
public const string NVSDK_NGX_Parameter_Hint_UseFireflySwatter = "Hint.UseFireflySwatter";
public const string NVSDK_NGX_Parameter_Resource_Width = "ResourceWidth";
public const string NVSDK_NGX_Parameter_Resource_Height = "ResourceHeight";
public const string NVSDK_NGX_Parameter_Resource_OutWidth = "ResourceOutWidth";
public const string NVSDK_NGX_Parameter_Resource_OutHeight = "ResourceOutHeight";
public const string NVSDK_NGX_Parameter_Depth = "Depth";
public const string NVSDK_NGX_Parameter_FI_Depth1 = "Depth1";
public const string NVSDK_NGX_Parameter_FI_Depth2 = "Depth2";
public const string NVSDK_NGX_Parameter_DLSSOptimalSettingsCallback = "DLSSOptimalSettingsCallback";
public const string NVSDK_NGX_Parameter_DLSSGetStatsCallback = "DLSSGetStatsCallback";
public const string NVSDK_NGX_Parameter_PerfQualityValue = "PerfQualityValue";
public const string NVSDK_NGX_Parameter_RTXValue = "RTXValue";
public const string NVSDK_NGX_Parameter_DLSSMode = "DLSSMode";
public const string NVSDK_NGX_Parameter_FI_Mode = "FIMode";
public const string NVSDK_NGX_Parameter_FI_OF_Preset = "FIOFPreset";
public const string NVSDK_NGX_Parameter_FI_OF_GridSize = "FIOFGridSize";
public const string NVSDK_NGX_Parameter_Jitter_Offset_X = "Jitter.Offset.X";
public const string NVSDK_NGX_Parameter_Jitter_Offset_Y = "Jitter.Offset.Y";
public const string NVSDK_NGX_Parameter_Denoise = "Denoise";
public const string NVSDK_NGX_Parameter_TransparencyMask = "TransparencyMask";
public const string NVSDK_NGX_Parameter_ExposureTexture = "ExposureTexture"; // a 1x1 texture containing the final exposure scale
public const string NVSDK_NGX_Parameter_DLSS_Feature_Create_Flags = "DLSS.Feature.Create.Flags";
public const string NVSDK_NGX_Parameter_DLSS_Checkerboard_Jitter_Hack = "DLSS.Checkerboard.Jitter.Hack";
public const string NVSDK_NGX_Parameter_GBuffer_Normals = "GBuffer.Normals";
public const string NVSDK_NGX_Parameter_GBuffer_Albedo = "GBuffer.Albedo";
public const string NVSDK_NGX_Parameter_GBuffer_Roughness = "GBuffer.Roughness";
public const string NVSDK_NGX_Parameter_GBuffer_DiffuseAlbedo = "GBuffer.DiffuseAlbedo";
public const string NVSDK_NGX_Parameter_GBuffer_SpecularAlbedo = "GBuffer.SpecularAlbedo";
public const string NVSDK_NGX_Parameter_GBuffer_IndirectAlbedo = "GBuffer.IndirectAlbedo";
public const string NVSDK_NGX_Parameter_GBuffer_SpecularMvec = "GBuffer.SpecularMvec";
public const string NVSDK_NGX_Parameter_GBuffer_DisocclusionMask = "GBuffer.DisocclusionMask";
public const string NVSDK_NGX_Parameter_GBuffer_Metallic = "GBuffer.Metallic";
public const string NVSDK_NGX_Parameter_GBuffer_Specular = "GBuffer.Specular";
public const string NVSDK_NGX_Parameter_GBuffer_Subsurface = "GBuffer.Subsurface";
public const string NVSDK_NGX_Parameter_GBuffer_ShadingModelId = "GBuffer.ShadingModelId";
public const string NVSDK_NGX_Parameter_GBuffer_MaterialId = "GBuffer.MaterialId";
public const string NVSDK_NGX_Parameter_GBuffer_Atrrib_8 = "GBuffer.Attrib.8"; // Note: C++ typo was Atrrib, kept for consistency
public const string NVSDK_NGX_Parameter_GBuffer_Atrrib_9 = "GBuffer.Attrib.9";
public const string NVSDK_NGX_Parameter_GBuffer_Atrrib_10 = "GBuffer.Attrib.10";
public const string NVSDK_NGX_Parameter_GBuffer_Atrrib_11 = "GBuffer.Attrib.11";
public const string NVSDK_NGX_Parameter_GBuffer_Atrrib_12 = "GBuffer.Attrib.12";
public const string NVSDK_NGX_Parameter_GBuffer_Atrrib_13 = "GBuffer.Attrib.13";
public const string NVSDK_NGX_Parameter_GBuffer_Atrrib_14 = "GBuffer.Attrib.14";
public const string NVSDK_NGX_Parameter_GBuffer_Atrrib_15 = "GBuffer.Attrib.15";
public const string NVSDK_NGX_Parameter_TonemapperType = "TonemapperType";
public const string NVSDK_NGX_Parameter_FreeMemOnReleaseFeature = "FreeMemOnReleaseFeature";
public const string NVSDK_NGX_Parameter_MotionVectors3D = "MotionVectors3D";
public const string NVSDK_NGX_Parameter_IsParticleMask = "IsParticleMask";
public const string NVSDK_NGX_Parameter_AnimatedTextureMask = "AnimatedTextureMask";
public const string NVSDK_NGX_Parameter_DepthHighRes = "DepthHighRes";
public const string NVSDK_NGX_Parameter_Position_ViewSpace = "Position.ViewSpace";
public const string NVSDK_NGX_Parameter_FrameTimeDeltaInMsec = "FrameTimeDeltaInMsec";
public const string NVSDK_NGX_Parameter_RayTracingHitDistance = "RayTracingHitDistance";
public const string NVSDK_NGX_Parameter_MotionVectorsReflection = "MotionVectorsReflection";
public const string NVSDK_NGX_Parameter_DLSS_Enable_Output_Subrects = "DLSS.Enable.Output.Subrects";
public const string NVSDK_NGX_Parameter_DLSS_Input_Color_Subrect_Base_X = "DLSS.Input.Color.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSS_Input_Color_Subrect_Base_Y = "DLSS.Input.Color.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSS_Input_Depth_Subrect_Base_X = "DLSS.Input.Depth.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSS_Input_Depth_Subrect_Base_Y = "DLSS.Input.Depth.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSS_Input_MV_SubrectBase_X = "DLSS.Input.MV.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSS_Input_MV_SubrectBase_Y = "DLSS.Input.MV.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSS_Input_Translucency_SubrectBase_X = "DLSS.Input.Translucency.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSS_Input_Translucency_SubrectBase_Y = "DLSS.Input.Translucency.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSS_Output_Subrect_Base_X = "DLSS.Output.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSS_Output_Subrect_Base_Y = "DLSS.Output.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSS_Render_Subrect_Dimensions_Width = "DLSS.Render.Subrect.Dimensions.Width";
public const string NVSDK_NGX_Parameter_DLSS_Render_Subrect_Dimensions_Height = "DLSS.Render.Subrect.Dimensions.Height";
public const string NVSDK_NGX_Parameter_DLSS_Pre_Exposure = "DLSS.Pre.Exposure";
public const string NVSDK_NGX_Parameter_DLSS_Exposure_Scale = "DLSS.Exposure.Scale";
public const string NVSDK_NGX_Parameter_DLSS_Input_Bias_Current_Color_Mask = "DLSS.Input.Bias.Current.Color.Mask";
public const string NVSDK_NGX_Parameter_DLSS_Input_Bias_Current_Color_SubrectBase_X = "DLSS.Input.Bias.Current.Color.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSS_Input_Bias_Current_Color_SubrectBase_Y = "DLSS.Input.Bias.Current.Color.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSS_Indicator_Invert_Y_Axis = "DLSS.Indicator.Invert.Y.Axis";
public const string NVSDK_NGX_Parameter_DLSS_Indicator_Invert_X_Axis = "DLSS.Indicator.Invert.X.Axis";
public const string NVSDK_NGX_Parameter_DLSS_INV_VIEW_PROJECTION_MATRIX = "InvViewProjectionMatrix";
public const string NVSDK_NGX_Parameter_DLSS_CLIP_TO_PREV_CLIP_MATRIX = "ClipToPrevClipMatrix";

public const string NVSDK_NGX_Parameter_DLSS_TransparencyLayer = "DLSS.TransparencyLayer";
public const string NVSDK_NGX_Parameter_DLSS_TransparencyLayer_Subrect_Base_X = "DLSS.TransparencyLayer.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSS_TransparencyLayer_Subrect_Base_Y = "DLSS.TransparencyLayer.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSS_TransparencyLayerOpacity = "DLSS.TransparencyLayerOpacity";
public const string NVSDK_NGX_Parameter_DLSS_TransparencyLayerOpacity_Subrect_Base_X = "DLSS.TransparencyLayerOpacity.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSS_TransparencyLayerOpacity_Subrect_Base_Y = "DLSS.TransparencyLayerOpacity.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSS_TransparencyLayerMvecs = "DLSS.TransparencyLayerMvecs";
public const string NVSDK_NGX_Parameter_DLSS_TransparencyLayerMvecs_Subrect_Base_X = "DLSS.TransparencyLayerMvecs.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSS_TransparencyLayerMvecs_Subrect_Base_Y = "DLSS.TransparencyLayerMvecs.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSS_DisocclusionMask = "DLSS.DisocclusionMask";
public const string NVSDK_NGX_Parameter_DLSS_DisocclusionMask_Subrect_Base_X = "DLSS.DisocclusionMask.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSS_DisocclusionMask_Subrect_Base_Y = "DLSS.DisocclusionMask.Subrect.Base.Y";

public const string NVSDK_NGX_Parameter_DLSS_Get_Dynamic_Max_Render_Width = "DLSS.Get.Dynamic.Max.Render.Width";
public const string NVSDK_NGX_Parameter_DLSS_Get_Dynamic_Max_Render_Height = "DLSS.Get.Dynamic.Max.Render.Height";
public const string NVSDK_NGX_Parameter_DLSS_Get_Dynamic_Min_Render_Width = "DLSS.Get.Dynamic.Min.Render.Width";
public const string NVSDK_NGX_Parameter_DLSS_Get_Dynamic_Min_Render_Height = "DLSS.Get.Dynamic.Min.Render.Height";

public const string NVSDK_NGX_Parameter_DLSS_Hint_Render_Preset_DLAA = "DLSS.Hint.Render.Preset.DLAA";
public const string NVSDK_NGX_Parameter_DLSS_Hint_Render_Preset_Quality = "DLSS.Hint.Render.Preset.Quality";
public const string NVSDK_NGX_Parameter_DLSS_Hint_Render_Preset_Balanced = "DLSS.Hint.Render.Preset.Balanced";
public const string NVSDK_NGX_Parameter_DLSS_Hint_Render_Preset_Performance = "DLSS.Hint.Render.Preset.Performance";
public const string NVSDK_NGX_Parameter_DLSS_Hint_Render_Preset_UltraPerformance = "DLSS.Hint.Render.Preset.UltraPerformance";
public const string NVSDK_NGX_Parameter_DLSS_Hint_Render_Preset_UltraQuality = "DLSS.Hint.Render.Preset.UltraQuality";

} 
}