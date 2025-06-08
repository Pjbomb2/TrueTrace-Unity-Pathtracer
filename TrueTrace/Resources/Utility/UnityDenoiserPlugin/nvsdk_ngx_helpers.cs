using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace DenoiserPlugin
{
public static partial class DlssSdk
{

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate NVSDK_NGX_Result PFN_NVSDK_NGX_DLSS_GetStatsCallback(IntPtr InParams);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate NVSDK_NGX_Result PFN_NVSDK_NGX_DLSS_GetOptimalSettingsCallback(IntPtr InParams);

public static NVSDK_NGX_Result NGX_DLSS_GET_STATS_2(
    IntPtr pInParams, // NVSDK_NGX_Parameter*
    out ulong pVRAMAllocatedBytes,
    out uint pOptLevel,
    out uint IsDevSnippetBranch)
{
    pVRAMAllocatedBytes = 0;
    pOptLevel = 0;
    IsDevSnippetBranch = 0;

    IntPtr callbackPtr;
    DLSS_Parameter_GetVoidPointer(pInParams, NVSDK_NGX_Parameter_DLSSGetStatsCallback, out callbackPtr);
    if (callbackPtr == IntPtr.Zero)
    {
        // Possible reasons for this:
        // - Installed DLSS is out of date and does not support the feature we need
        // - You used NVSDK_NGX_AllocateParameters() for creating InParams. Try using NVSDK_NGX_GetCapabilityParameters() instead
        return NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_OutOfDate;
    }

    NVSDK_NGX_Result Res = NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
    var PFNCallback = Marshal.GetDelegateForFunctionPointer<PFN_NVSDK_NGX_DLSS_GetStatsCallback>(callbackPtr);
    Res = PFNCallback(pInParams);
    if (NVSDK_NGX_FAILED(Res))
    {
        return Res;
    }
    DLSS_Parameter_GetULL(pInParams, NVSDK_NGX_Parameter_SizeInBytes, out pVRAMAllocatedBytes);
    DLSS_Parameter_GetUI(pInParams, NVSDK_NGX_EParameter_OptLevel, out pOptLevel);
    DLSS_Parameter_GetUI(pInParams, NVSDK_NGX_EParameter_IsDevSnippetBranch, out IsDevSnippetBranch);
    return Res;
}

public static NVSDK_NGX_Result NGX_DLSS_GET_STATS_1(
    IntPtr pInParams, // NVSDK_NGX_Parameter*
    out ulong pVRAMAllocatedBytes,
    out uint pOptLevel)
{
    uint dummy = 0;
    return NGX_DLSS_GET_STATS_2(pInParams, out pVRAMAllocatedBytes, out pOptLevel, out dummy);
}

public static NVSDK_NGX_Result NGX_DLSS_GET_STATS(
    IntPtr pInParams, // NVSDK_NGX_Parameter*
    out ulong pVRAMAllocatedBytes)
{
    uint dummy = 0;
    return NGX_DLSS_GET_STATS_2(pInParams, out pVRAMAllocatedBytes, out dummy, out dummy);
}

public static NVSDK_NGX_Result NGX_DLSS_GET_OPTIMAL_SETTINGS(
    IntPtr pInParams, // NVSDK_NGX_Parameter*
    uint InUserSelectedWidth,
    uint InUserSelectedHeight,
    NVSDK_NGX_PerfQuality_Value InPerfQualityValue,
    out uint pOutRenderOptimalWidth,
    out uint pOutRenderOptimalHeight,
    out uint pOutRenderMaxWidth,
    out uint pOutRenderMaxHeight,
    out uint pOutRenderMinWidth,
    out uint pOutRenderMinHeight,
    out float pOutSharpness)
{
    pOutRenderOptimalWidth = 0;
    pOutRenderOptimalHeight = 0;
    pOutRenderMaxWidth = 0;
    pOutRenderMaxHeight = 0;
    pOutRenderMinWidth = 0;
    pOutRenderMinHeight = 0;
    pOutSharpness = 0.0f;

    IntPtr callbackPtr;
    DLSS_Parameter_GetVoidPointer(pInParams, NVSDK_NGX_Parameter_DLSSOptimalSettingsCallback, out callbackPtr);
    if (callbackPtr == IntPtr.Zero)
    {
        // Possible reasons for this:
        // - Installed DLSS is out of date and does not support the feature we need
        // - You used NVSDK_NGX_AllocateParameters() for creating InParams. Try using NVSDK_NGX_GetCapabilityParameters() instead
        return NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_OutOfDate;
    }

    // These are selections made by user in UI
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_Width, InUserSelectedWidth);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_Height, InUserSelectedHeight);
    DLSS_Parameter_SetI(pInParams, NVSDK_NGX_Parameter_PerfQualityValue, (int)InPerfQualityValue);
    DLSS_Parameter_SetI(pInParams, NVSDK_NGX_Parameter_RTXValue, 0); // Some older DLSS dlls still expect this value to be set

    NVSDK_NGX_Result Res = NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
    var PFNCallback = Marshal.GetDelegateForFunctionPointer<PFN_NVSDK_NGX_DLSS_GetOptimalSettingsCallback>(callbackPtr);
    Res = PFNCallback(pInParams);
    if (NVSDK_NGX_FAILED(Res))
    {
        return Res;
    }
    DLSS_Parameter_GetUI(pInParams, NVSDK_NGX_Parameter_OutWidth, out pOutRenderOptimalWidth);
    DLSS_Parameter_GetUI(pInParams, NVSDK_NGX_Parameter_OutHeight, out pOutRenderOptimalHeight);
    // If we have an older DLSS Dll those might need to be set to the optimal dimensions instead
    pOutRenderMaxWidth = pOutRenderOptimalWidth;
    pOutRenderMaxHeight = pOutRenderOptimalHeight;
    pOutRenderMinWidth = pOutRenderOptimalWidth;
    pOutRenderMinHeight = pOutRenderOptimalHeight;
    DLSS_Parameter_GetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Get_Dynamic_Max_Render_Width, out pOutRenderMaxWidth);
    DLSS_Parameter_GetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Get_Dynamic_Max_Render_Height, out pOutRenderMaxHeight);
    DLSS_Parameter_GetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Get_Dynamic_Min_Render_Width, out pOutRenderMinWidth);
    DLSS_Parameter_GetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Get_Dynamic_Min_Render_Height, out pOutRenderMinHeight);
    DLSS_Parameter_GetF(pInParams, NVSDK_NGX_Parameter_Sharpness, out pOutSharpness);
    return Res;
}

public struct NVSDK_NGX_Feature_Create_Params
{
    public uint InWidth;
    public uint InHeight;
    public uint InTargetWidth;
    public uint InTargetHeight;
    /*** OPTIONAL ***/
    public NVSDK_NGX_PerfQuality_Value InPerfQualityValue;
};

public struct NVSDK_NGX_DLSS_Create_Params
{
    public NVSDK_NGX_Feature_Create_Params Feature;
    /*** OPTIONAL ***/
    public int     InFeatureCreateFlags;
    public bool    InEnableOutputSubrects;
};

[StructLayout(LayoutKind.Sequential)]
public struct NVSDK_NGX_D3D12_Feature_Eval_Params
{
    public IntPtr pInColor; // ID3D12Resource*
    public IntPtr pInOutput; // ID3D12Resource*
    public float InSharpness; // OPTIONAL for DLSS
}

// typedef struct NVSDK_NGX_D3D12_GBuffer
public class NVSDK_NGX_D3D12_GBuffer
{
    public IntPtr[] pInAttrib = new IntPtr[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFERTYPE_NUM];
}

public class NVSDK_NGX_D3D12_DLSS_Eval_Params
{
    public NVSDK_NGX_D3D12_Feature_Eval_Params Feature;
    public IntPtr pInDepth; // ID3D12Resource*
    public IntPtr pInMotionVectors; // ID3D12Resource*
    public float InJitterOffsetX; // Jitter offset must be in input/render pixel space
    public float InJitterOffsetY;
    public NVSDK_NGX_Dimensions InRenderSubrectDimensions;
    /*** OPTIONAL - leave to 0/0.0f if unused ***/
    public int InReset; // Set to 1 when scene changes completely (new level etc)
    public float InMVScaleX; // If MVs need custom scaling to convert to pixel space
    public float InMVScaleY;
    public IntPtr pInTransparencyMask; // ID3D12Resource* - Unused/Reserved for future use
    public IntPtr pInExposureTexture; // ID3D12Resource*
    public IntPtr pInBiasCurrentColorMask; // ID3D12Resource*
    public NVSDK_NGX_Coordinates InColorSubrectBase;
    public NVSDK_NGX_Coordinates InDepthSubrectBase;
    public NVSDK_NGX_Coordinates InMVSubrectBase;
    public NVSDK_NGX_Coordinates InTranslucencySubrectBase;
    public NVSDK_NGX_Coordinates InBiasCurrentColorSubrectBase;
    public NVSDK_NGX_Coordinates InOutputSubrectBase;
    public float InPreExposure;
    public float InExposureScale;
    public int InIndicatorInvertXAxis;
    public int InIndicatorInvertYAxis;
    /*** OPTIONAL - only for research purposes ***/
    public NVSDK_NGX_D3D12_GBuffer GBufferSurface = new ();
    public NVSDK_NGX_ToneMapperType InToneMapperType;
    public IntPtr pInMotionVectors3D; // ID3D12Resource*
    public IntPtr pInIsParticleMask; // ID3D12Resource* - to identify which pixels contains particles, essentially that are not drawn as part of base pass
    public IntPtr pInAnimatedTextureMask; // ID3D12Resource* - a binary mask covering pixels occupied by animated textures
    public IntPtr pInDepthHighRes; // ID3D12Resource*
    public IntPtr pInPositionViewSpace; // ID3D12Resource*
    public float InFrameTimeDeltaInMsec; // helps in determining the amount to denoise or anti-alias based on the speed of the object from motion vector magnitudes and fps as determined by this delta
    public IntPtr pInRayTracingHitDistance; // ID3D12Resource* - for each effect - approximation to the amount of noise in a ray-traced color
    public IntPtr pInMotionVectorsReflections; // ID3D12Resource* - motion vectors of reflected objects like for mirrored surfaces
}

public static void NGX_D3D12_CREATE_DLSS_EXT(
    CommandBuffer pInCmdList, // ID3D12GraphicsCommandList*
    uint InCreationNodeMask,
    uint InVisibilityNodeMask,
    out int ppOutHandle, // NVSDK_NGX_Handle**
    IntPtr pInParams, // NVSDK_NGX_Parameter*
    ref NVSDK_NGX_DLSS_Create_Params pInDlssCreateParams)
{
    ppOutHandle = -1;

    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_CreationNodeMask, InCreationNodeMask);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_VisibilityNodeMask, InVisibilityNodeMask);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_Width, pInDlssCreateParams.Feature.InWidth);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_Height, pInDlssCreateParams.Feature.InHeight);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_OutWidth, pInDlssCreateParams.Feature.InTargetWidth);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_OutHeight, pInDlssCreateParams.Feature.InTargetHeight);
    DLSS_Parameter_SetI(pInParams, NVSDK_NGX_Parameter_PerfQualityValue, (int)pInDlssCreateParams.Feature.InPerfQualityValue);
    DLSS_Parameter_SetI(pInParams, NVSDK_NGX_Parameter_DLSS_Feature_Create_Flags, pInDlssCreateParams.InFeatureCreateFlags);
    DLSS_Parameter_SetI(pInParams, NVSDK_NGX_Parameter_DLSS_Enable_Output_Subrects, pInDlssCreateParams.InEnableOutputSubrects ? 1 : 0);

    ppOutHandle = DLSS_CreateFeature(pInCmdList, NVSDK_NGX_Feature.NVSDK_NGX_Feature_SuperSampling, pInParams);
}

public static void NGX_D3D12_EVALUATE_DLSS_EXT(
    CommandBuffer pInCmdList,
    int pInHandle,
    IntPtr pInParams,
    ref NVSDK_NGX_D3D12_DLSS_Eval_Params pInDlssEvalParams)
{
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_Color, pInDlssEvalParams.Feature.pInColor);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_Output, pInDlssEvalParams.Feature.pInOutput);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_Depth, pInDlssEvalParams.pInDepth);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_MotionVectors, pInDlssEvalParams.pInMotionVectors);
    DLSS_Parameter_SetF(pInParams, NVSDK_NGX_Parameter_Jitter_Offset_X, pInDlssEvalParams.InJitterOffsetX);
    DLSS_Parameter_SetF(pInParams, NVSDK_NGX_Parameter_Jitter_Offset_Y, pInDlssEvalParams.InJitterOffsetY);
    DLSS_Parameter_SetF(pInParams, NVSDK_NGX_Parameter_Sharpness, pInDlssEvalParams.Feature.InSharpness);
    DLSS_Parameter_SetI(pInParams, NVSDK_NGX_Parameter_Reset, pInDlssEvalParams.InReset);
    DLSS_Parameter_SetF(pInParams, NVSDK_NGX_Parameter_MV_Scale_X, pInDlssEvalParams.InMVScaleX == 0.0f ? 1.0f : pInDlssEvalParams.InMVScaleX);
    DLSS_Parameter_SetF(pInParams, NVSDK_NGX_Parameter_MV_Scale_Y, pInDlssEvalParams.InMVScaleY == 0.0f ? 1.0f : pInDlssEvalParams.InMVScaleY);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_TransparencyMask, pInDlssEvalParams.pInTransparencyMask);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_ExposureTexture, pInDlssEvalParams.pInExposureTexture);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Bias_Current_Color_Mask, pInDlssEvalParams.pInBiasCurrentColorMask);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Albedo, pInDlssEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_ALBEDO]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Roughness, pInDlssEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_ROUGHNESS]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Metallic, pInDlssEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_METALLIC]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Specular, pInDlssEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_SPECULAR]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Subsurface, pInDlssEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_SUBSURFACE]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Normals, pInDlssEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_NORMALS]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_ShadingModelId, pInDlssEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_SHADINGMODELID]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_MaterialId, pInDlssEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_MATERIALID]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Atrrib_8, pInDlssEvalParams.GBufferSurface.pInAttrib[8]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Atrrib_9, pInDlssEvalParams.GBufferSurface.pInAttrib[9]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Atrrib_10, pInDlssEvalParams.GBufferSurface.pInAttrib[10]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Atrrib_11, pInDlssEvalParams.GBufferSurface.pInAttrib[11]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Atrrib_12, pInDlssEvalParams.GBufferSurface.pInAttrib[12]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Atrrib_13, pInDlssEvalParams.GBufferSurface.pInAttrib[13]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Atrrib_14, pInDlssEvalParams.GBufferSurface.pInAttrib[14]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Atrrib_15, pInDlssEvalParams.GBufferSurface.pInAttrib[15]);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_TonemapperType, (uint)pInDlssEvalParams.InToneMapperType);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_MotionVectors3D, pInDlssEvalParams.pInMotionVectors3D);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_IsParticleMask, pInDlssEvalParams.pInIsParticleMask);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_AnimatedTextureMask, pInDlssEvalParams.pInAnimatedTextureMask);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DepthHighRes, pInDlssEvalParams.pInDepthHighRes);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_Position_ViewSpace, pInDlssEvalParams.pInPositionViewSpace);
    DLSS_Parameter_SetF(pInParams, NVSDK_NGX_Parameter_FrameTimeDeltaInMsec, pInDlssEvalParams.InFrameTimeDeltaInMsec);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_RayTracingHitDistance, pInDlssEvalParams.pInRayTracingHitDistance);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_MotionVectorsReflection, pInDlssEvalParams.pInMotionVectorsReflections);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Color_Subrect_Base_X, pInDlssEvalParams.InColorSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Color_Subrect_Base_Y, pInDlssEvalParams.InColorSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Depth_Subrect_Base_X, pInDlssEvalParams.InDepthSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Depth_Subrect_Base_Y, pInDlssEvalParams.InDepthSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_MV_SubrectBase_X, pInDlssEvalParams.InMVSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_MV_SubrectBase_Y, pInDlssEvalParams.InMVSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Translucency_SubrectBase_X, pInDlssEvalParams.InTranslucencySubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Translucency_SubrectBase_Y, pInDlssEvalParams.InTranslucencySubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Bias_Current_Color_SubrectBase_X, pInDlssEvalParams.InBiasCurrentColorSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Bias_Current_Color_SubrectBase_Y, pInDlssEvalParams.InBiasCurrentColorSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Output_Subrect_Base_X, pInDlssEvalParams.InOutputSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Output_Subrect_Base_Y, pInDlssEvalParams.InOutputSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Render_Subrect_Dimensions_Width, pInDlssEvalParams.InRenderSubrectDimensions.Width);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Render_Subrect_Dimensions_Height, pInDlssEvalParams.InRenderSubrectDimensions.Height);
    DLSS_Parameter_SetF(pInParams, NVSDK_NGX_Parameter_DLSS_Pre_Exposure, pInDlssEvalParams.InPreExposure == 0.0f ? 1.0f : pInDlssEvalParams.InPreExposure);
    DLSS_Parameter_SetF(pInParams, NVSDK_NGX_Parameter_DLSS_Exposure_Scale, pInDlssEvalParams.InExposureScale == 0.0f ? 1.0f : pInDlssEvalParams.InExposureScale);
    DLSS_Parameter_SetI(pInParams, NVSDK_NGX_Parameter_DLSS_Indicator_Invert_X_Axis, pInDlssEvalParams.InIndicatorInvertXAxis);
    DLSS_Parameter_SetI(pInParams, NVSDK_NGX_Parameter_DLSS_Indicator_Invert_Y_Axis, pInDlssEvalParams.InIndicatorInvertYAxis);

    DLSS_EvaluateFeature(pInCmdList, pInHandle, pInParams);
}

}
} 