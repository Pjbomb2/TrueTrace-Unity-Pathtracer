using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace DenoiserPlugin
{
public static partial class DlssSdk
{

public static NVSDK_NGX_Result NGX_DLSSD_GET_STATS_2(
    IntPtr pInParams, // NVSDK_NGX_Parameter*
    out ulong pVRAMAllocatedBytes, 
    out uint pOptLevel,
    out uint IsDevSnippetBranch)
{
    pVRAMAllocatedBytes = 0;
    pOptLevel = 0;
    IsDevSnippetBranch = 0;

    IntPtr Callback = IntPtr.Zero;
    DLSS_Parameter_GetVoidPointer(pInParams, NVSDK_NGX_Parameter_DLSSDGetStatsCallback, out Callback);
    if (Callback == IntPtr.Zero)
    {
        // Possible reasons for this:
        // - Installed DLSS is out of date and does not support the feature we need
        // - You used NVSDK_NGX_AllocateParameters() for creating InParams. Try using NVSDK_NGX_GetCapabilityParameters() instead
        return NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_OutOfDate;
    }

    NVSDK_NGX_Result Res = NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
    PFN_NVSDK_NGX_DLSS_GetStatsCallback PFNCallback = Marshal.GetDelegateForFunctionPointer<PFN_NVSDK_NGX_DLSS_GetStatsCallback>(Callback);
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

public static NVSDK_NGX_Result NGX_DLSSD_GET_STATS_1(
    IntPtr pInParams,               // NVSDK_NGX_Parameter*
    out ulong pVRAMAllocatedBytes,
    out uint pOptLevel)
{
    uint dummy = 0;
    return NGX_DLSSD_GET_STATS_2(pInParams, out pVRAMAllocatedBytes, out pOptLevel, out dummy);
}

public static NVSDK_NGX_Result NGX_DLSSD_GET_STATS(
    IntPtr pInParams,               // NVSDK_NGX_Parameter*
    out ulong pVRAMAllocatedBytes)
{
    uint dummy = 0;
    return NGX_DLSSD_GET_STATS_2(pInParams, out pVRAMAllocatedBytes, out dummy, out dummy);
}

public static NVSDK_NGX_Result NGX_DLSSD_GET_OPTIMAL_SETTINGS(
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

    IntPtr Callback = IntPtr.Zero;
    DLSS_Parameter_GetVoidPointer(pInParams, NVSDK_NGX_Parameter_DLSSDOptimalSettingsCallback, out Callback);
    if (Callback == IntPtr.Zero)
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
    DLSS_Parameter_SetI(pInParams, NVSDK_NGX_Parameter_RTXValue, 0); // false - Some older DLSS dlls still expect this value to be set

    NVSDK_NGX_Result Res = NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
    PFN_NVSDK_NGX_DLSS_GetOptimalSettingsCallback PFNCallback = Marshal.GetDelegateForFunctionPointer<PFN_NVSDK_NGX_DLSS_GetOptimalSettingsCallback>(Callback);
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

// typedef struct NVSDK_NGX_DLSSD_Create_Params (inferred from usage)
public class NVSDK_NGX_DLSSD_Create_Params
{
    public uint InWidth;
    public uint InHeight;
    public uint InTargetWidth;
    public uint InTargetHeight;
    public NVSDK_NGX_PerfQuality_Value InPerfQualityValue;
    public int InFeatureCreateFlags; // NVSDK_NGX_DLSS_Feature_Create_Flags bitmask
    public int InEnableOutputSubrects; // Boolean as int (0 or 1)
    public NVSDK_NGX_DLSS_Roughness_Mode InRoughnessMode;
    public NVSDK_NGX_DLSS_Depth_Type InUseHWDepth;
}

// typedef struct NVSDK_NGX_D3D12_DLSSD_Eval_Params
public class NVSDK_NGX_D3D12_DLSSD_Eval_Params
{
    public IntPtr pInDiffuseAlbedo; // ID3D12Resource*
    public IntPtr pInSpecularAlbedo; // ID3D12Resource*
    public IntPtr pInNormals; // ID3D12Resource*
    public IntPtr pInRoughness; // ID3D12Resource*

    public IntPtr pInColor; // ID3D12Resource*
    public IntPtr pInAlpha; // ID3D12Resource*
    public IntPtr pInOutput; // ID3D12Resource*
    public IntPtr pInOutputAlpha; // ID3D12Resource*
    public IntPtr pInDepth; // ID3D12Resource*
    public IntPtr pInMotionVectors; // ID3D12Resource*
    public float InJitterOffsetX;     /* Jitter offset must be in input/render pixel space */
    public float InJitterOffsetY;
    public NVSDK_NGX_Dimensions InRenderSubrectDimensions;
    /*** OPTIONAL - leave to 0/0.0f if unused ***/
    public int InReset;             /* Set to 1 when scene changes completely (new level etc) */
    public float InMVScaleX;          /* If MVs need custom scaling to convert to pixel space */
    public float InMVScaleY;
    public IntPtr pInTransparencyMask; /* Unused/Reserved for future use */ // ID3D12Resource*
    public IntPtr pInExposureTexture; // ID3D12Resource*
    public IntPtr pInBiasCurrentColorMask; // ID3D12Resource*
    public NVSDK_NGX_Coordinates InAlphaSubrectBase;
    public NVSDK_NGX_Coordinates InOutputAlphaSubrectBase;
    public NVSDK_NGX_Coordinates InDiffuseAlbedoSubrectBase;
    public NVSDK_NGX_Coordinates InSpecularAlbedoSubrectBase;
    public NVSDK_NGX_Coordinates InNormalsSubrectBase;
    public NVSDK_NGX_Coordinates InRoughnessSubrectBase;
    public NVSDK_NGX_Coordinates InColorSubrectBase;
    public NVSDK_NGX_Coordinates InDepthSubrectBase;
    public NVSDK_NGX_Coordinates InMVSubrectBase;
    public NVSDK_NGX_Coordinates InTranslucencySubrectBase;
    public NVSDK_NGX_Coordinates InBiasCurrentColorSubrectBase;
    public NVSDK_NGX_Coordinates InOutputSubrectBase;

    public IntPtr pInReflectedAlbedo; // ID3D12Resource*
    public IntPtr pInColorBeforeParticles; // ID3D12Resource*
    public IntPtr pInColorAfterParticles; // ID3D12Resource*
    public IntPtr pInColorBeforeTransparency; // ID3D12Resource*
    public IntPtr pInColorAfterTransparency; // ID3D12Resource*
    public IntPtr pInColorBeforeFog; // ID3D12Resource*
    public IntPtr pInColorAfterFog; // ID3D12Resource*
    public IntPtr pInScreenSpaceSubsurfaceScatteringGuide; // ID3D12Resource*
    public IntPtr pInColorBeforeScreenSpaceSubsurfaceScattering; // ID3D12Resource*
    public IntPtr pInColorAfterScreenSpaceSubsurfaceScattering; // ID3D12Resource*
    public IntPtr pInScreenSpaceRefractionGuide; // ID3D12Resource*
    public IntPtr pInColorBeforeScreenSpaceRefraction; // ID3D12Resource*
    public IntPtr pInColorAfterScreenSpaceRefraction; // ID3D12Resource*
    public IntPtr pInDepthOfFieldGuide; // ID3D12Resource*
    public IntPtr pInColorBeforeDepthOfField; // ID3D12Resource*
    public IntPtr pInColorAfterDepthOfField; // ID3D12Resource*
    public IntPtr pInDiffuseHitDistance; // ID3D12Resource*
    public IntPtr pInSpecularHitDistance; // ID3D12Resource*
    public IntPtr pInDiffuseRayDirection; // ID3D12Resource*
    public IntPtr pInSpecularRayDirection; // ID3D12Resource*
    public IntPtr pInDiffuseRayDirectionHitDistance; // ID3D12Resource*
    public IntPtr pInSpecularRayDirectionHitDistance; // ID3D12Resource*
    public NVSDK_NGX_Coordinates InReflectedAlbedoSubrectBase;
    public NVSDK_NGX_Coordinates InColorBeforeParticlesSubrectBase;
    public NVSDK_NGX_Coordinates InColorAfterParticlesSubrectBase;
    public NVSDK_NGX_Coordinates InColorBeforeTransparencySubrectBase;
    public NVSDK_NGX_Coordinates InColorAfterTransparencySubrectBase;
    public NVSDK_NGX_Coordinates InColorBeforeFogSubrectBase;
    public NVSDK_NGX_Coordinates InColorAfterFogSubrectBase;
    public NVSDK_NGX_Coordinates InScreenSpaceSubsurfaceScatteringGuideSubrectBase;
    public NVSDK_NGX_Coordinates InScreenSpaceRefractionGuideSubrectBase;
    public NVSDK_NGX_Coordinates InDepthOfFieldGuideSubrectBase;
    public NVSDK_NGX_Coordinates InDiffuseHitDistanceSubrectBase;
    public NVSDK_NGX_Coordinates InSpecularHitDistanceSubrectBase;
    public NVSDK_NGX_Coordinates InDiffuseRayDirectionSubrectBase;
    public NVSDK_NGX_Coordinates InSpecularRayDirectionSubrectBase;
    public NVSDK_NGX_Coordinates InDiffuseRayDirectionHitDistanceSubrectBase;
    public NVSDK_NGX_Coordinates InSpecularRayDirectionHitDistanceSubrectBase;
    public NVSDK_NGX_Coordinates InColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase;
    public NVSDK_NGX_Coordinates InColorAfterScreenSpaceSubsurfaceScatteringSubrectBase;
    public NVSDK_NGX_Coordinates InColorBeforeScreenSpaceRefractionSubrectBase;
    public NVSDK_NGX_Coordinates InColorAfterScreenSpaceRefractionSubrectBase;
    public NVSDK_NGX_Coordinates InColorBeforeDepthOfFieldSubrectBase;
    public NVSDK_NGX_Coordinates InColorAfterDepthOfFieldSubtectBase;
    public Matrix4x4 InWorldToViewMatrix;
    public Matrix4x4 InViewToClipMatrix;

    public float InPreExposure;
    public float InExposureScale;
    public int InIndicatorInvertXAxis;
    public int InIndicatorInvertYAxis;
    /*** OPTIONAL - only for research purposes ***/
    public NVSDK_NGX_D3D12_GBuffer GBufferSurface = new ();
    public NVSDK_NGX_ToneMapperType InToneMapperType;
    public IntPtr pInMotionVectors3D; // ID3D12Resource*
    public IntPtr pInIsParticleMask; /* to identify which pixels contains particles, essentially that are not drawn as part of base pass */ // ID3D12Resource*
    public IntPtr pInAnimatedTextureMask; /* a binary mask covering pixels occupied by animated textures */ // ID3D12Resource*
    public IntPtr pInDepthHighRes; // ID3D12Resource*
    public IntPtr pInPositionViewSpace; // ID3D12Resource*
    public float InFrameTimeDeltaInMsec; /* helps in determining the amount to denoise or anti-alias based on the speed of the object from motion vector magnitudes and fps as determined by this delta */
    public IntPtr pInRayTracingHitDistance; /* for each effect - approximation to the amount of noise in a ray-traced color */ // ID3D12Resource*
    public IntPtr pInMotionVectorsReflections; /* motion vectors of reflected objects like for mirrored surfaces */ // ID3D12Resource*
    public IntPtr pInTransparencyLayer; /* optional input res particle layer */ // ID3D12Resource*
    public NVSDK_NGX_Coordinates InTransparencyLayerSubrectBase;
    public IntPtr pInTransparencyLayerOpacity; /* optional input res particle opacity layer */ // ID3D12Resource*
    public NVSDK_NGX_Coordinates InTransparencyLayerOpacitySubrectBase;
    public IntPtr pInTransparencyLayerMvecs; /* optional input res transparency layer mvecs */ // ID3D12Resource*
    public NVSDK_NGX_Coordinates InTransparencyLayerMvecsSubrectBase;
    public IntPtr pInDisocclusionMask; /* optional input res disocclusion mask */ // ID3D12Resource*
    public NVSDK_NGX_Coordinates InDisocclusionMaskSubrectBase;

}

public static void NGX_D3D12_CREATE_DLSSD_EXT(
    CommandBuffer pInCmdList,
    uint InCreationNodeMask, 
    uint InVisibilityNodeMask,
    out int ppOutHandle,
    IntPtr pInParams, // NVSDK_NGX_Parameter*
    ref NVSDK_NGX_DLSSD_Create_Params pInDlssDCreateParams)
{
    ppOutHandle = -1;

    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_CreationNodeMask, InCreationNodeMask);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_VisibilityNodeMask, InVisibilityNodeMask);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_Width, pInDlssDCreateParams.InWidth);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_Height, pInDlssDCreateParams.InHeight);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_OutWidth, pInDlssDCreateParams.InTargetWidth);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_OutHeight, pInDlssDCreateParams.InTargetHeight);
    DLSS_Parameter_SetI(pInParams, NVSDK_NGX_Parameter_PerfQualityValue, (int)pInDlssDCreateParams.InPerfQualityValue);
    DLSS_Parameter_SetI(pInParams, NVSDK_NGX_Parameter_DLSS_Feature_Create_Flags, pInDlssDCreateParams.InFeatureCreateFlags);
    DLSS_Parameter_SetI(pInParams, NVSDK_NGX_Parameter_DLSS_Enable_Output_Subrects, pInDlssDCreateParams.InEnableOutputSubrects != 0 ? 1 : 0);
    DLSS_Parameter_SetI(pInParams, NVSDK_NGX_Parameter_DLSS_Denoise_Mode, (int)NVSDK_NGX_DLSS_Denoise_Mode.NVSDK_NGX_DLSS_Denoise_Mode_DLUnified);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Roughness_Mode, (uint)pInDlssDCreateParams.InRoughnessMode);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_Use_HW_Depth, (uint)pInDlssDCreateParams.InUseHWDepth);
    
    ppOutHandle = DLSS_CreateFeature(pInCmdList, NVSDK_NGX_Feature.NVSDK_NGX_Feature_RayReconstruction, pInParams);
}

public static void NGX_D3D12_EVALUATE_DLSSD_EXT(
    CommandBuffer pInCmdList,
    int pInHandle,
    IntPtr pInParams, // NVSDK_NGX_Parameter*
    ref NVSDK_NGX_D3D12_DLSSD_Eval_Params pInDlssDEvalParams)
{
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_Color, pInDlssDEvalParams.pInColor);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_Output, pInDlssDEvalParams.pInOutput);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_Depth, pInDlssDEvalParams.pInDepth);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_MotionVectors, pInDlssDEvalParams.pInMotionVectors);
    DLSS_Parameter_SetF(pInParams, NVSDK_NGX_Parameter_Jitter_Offset_X, pInDlssDEvalParams.InJitterOffsetX);
    DLSS_Parameter_SetF(pInParams, NVSDK_NGX_Parameter_Jitter_Offset_Y, pInDlssDEvalParams.InJitterOffsetY);
    DLSS_Parameter_SetI(pInParams, NVSDK_NGX_Parameter_Reset, pInDlssDEvalParams.InReset);
    DLSS_Parameter_SetF(pInParams, NVSDK_NGX_Parameter_MV_Scale_X, pInDlssDEvalParams.InMVScaleX == 0.0f ? 1.0f : pInDlssDEvalParams.InMVScaleX);
    DLSS_Parameter_SetF(pInParams, NVSDK_NGX_Parameter_MV_Scale_Y, pInDlssDEvalParams.InMVScaleY == 0.0f ? 1.0f : pInDlssDEvalParams.InMVScaleY);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_TransparencyMask, pInDlssDEvalParams.pInTransparencyMask);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_ExposureTexture, pInDlssDEvalParams.pInExposureTexture);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Bias_Current_Color_Mask, pInDlssDEvalParams.pInBiasCurrentColorMask);

    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Albedo, pInDlssDEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_ALBEDO]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Roughness, pInDlssDEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_ROUGHNESS]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Metallic, pInDlssDEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_METALLIC]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Specular, pInDlssDEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_SPECULAR]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Subsurface, pInDlssDEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_SUBSURFACE]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Normals, pInDlssDEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_NORMALS]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_ShadingModelId, pInDlssDEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_SHADINGMODELID]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_MaterialId, pInDlssDEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_MATERIALID]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Atrrib_8, pInDlssDEvalParams.GBufferSurface.pInAttrib[8]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Atrrib_9, pInDlssDEvalParams.GBufferSurface.pInAttrib[9]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Atrrib_11, pInDlssDEvalParams.GBufferSurface.pInAttrib[11]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Atrrib_12, pInDlssDEvalParams.GBufferSurface.pInAttrib[12]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Atrrib_13, pInDlssDEvalParams.GBufferSurface.pInAttrib[13]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Atrrib_14, pInDlssDEvalParams.GBufferSurface.pInAttrib[14]);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Atrrib_15, pInDlssDEvalParams.GBufferSurface.pInAttrib[15]);

    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_TonemapperType, (uint)pInDlssDEvalParams.InToneMapperType);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_MotionVectors3D, pInDlssDEvalParams.pInMotionVectors3D);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_IsParticleMask, pInDlssDEvalParams.pInIsParticleMask);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_AnimatedTextureMask, pInDlssDEvalParams.pInAnimatedTextureMask);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DepthHighRes, pInDlssDEvalParams.pInDepthHighRes);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_Position_ViewSpace, pInDlssDEvalParams.pInPositionViewSpace);
    DLSS_Parameter_SetF(pInParams, NVSDK_NGX_Parameter_FrameTimeDeltaInMsec, pInDlssDEvalParams.InFrameTimeDeltaInMsec);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_RayTracingHitDistance, pInDlssDEvalParams.pInRayTracingHitDistance);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_SpecularMvec, pInDlssDEvalParams.pInMotionVectorsReflections);

    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Color_Subrect_Base_X, pInDlssDEvalParams.InColorSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Color_Subrect_Base_Y, pInDlssDEvalParams.InColorSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Depth_Subrect_Base_X, pInDlssDEvalParams.InDepthSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Depth_Subrect_Base_Y, pInDlssDEvalParams.InDepthSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_MV_SubrectBase_X, pInDlssDEvalParams.InMVSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_MV_SubrectBase_Y, pInDlssDEvalParams.InMVSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Translucency_SubrectBase_X, pInDlssDEvalParams.InTranslucencySubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Translucency_SubrectBase_Y, pInDlssDEvalParams.InTranslucencySubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Bias_Current_Color_SubrectBase_X, pInDlssDEvalParams.InBiasCurrentColorSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Bias_Current_Color_SubrectBase_Y, pInDlssDEvalParams.InBiasCurrentColorSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Output_Subrect_Base_X, pInDlssDEvalParams.InOutputSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Output_Subrect_Base_Y, pInDlssDEvalParams.InOutputSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Render_Subrect_Dimensions_Width, pInDlssDEvalParams.InRenderSubrectDimensions.Width);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Render_Subrect_Dimensions_Height, pInDlssDEvalParams.InRenderSubrectDimensions.Height);
    DLSS_Parameter_SetF(pInParams, NVSDK_NGX_Parameter_DLSS_Pre_Exposure, pInDlssDEvalParams.InPreExposure == 0.0f ? 1.0f : pInDlssDEvalParams.InPreExposure);
    DLSS_Parameter_SetF(pInParams, NVSDK_NGX_Parameter_DLSS_Exposure_Scale, pInDlssDEvalParams.InExposureScale == 0.0f ? 1.0f : pInDlssDEvalParams.InExposureScale);
    DLSS_Parameter_SetI(pInParams, NVSDK_NGX_Parameter_DLSS_Indicator_Invert_X_Axis, pInDlssDEvalParams.InIndicatorInvertXAxis);
    DLSS_Parameter_SetI(pInParams, NVSDK_NGX_Parameter_DLSS_Indicator_Invert_Y_Axis, pInDlssDEvalParams.InIndicatorInvertYAxis);

    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Emissive, pInDlssDEvalParams.GBufferSurface.pInAttrib[(int)NVSDK_NGX_GBufferType.NVSDK_NGX_GBUFFER_EMISSIVE]);

    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DiffuseAlbedo, pInDlssDEvalParams.pInDiffuseAlbedo);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_SpecularAlbedo, pInDlssDEvalParams.pInSpecularAlbedo);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_DiffuseAlbedo_Subrect_Base_X, pInDlssDEvalParams.InDiffuseAlbedoSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_DiffuseAlbedo_Subrect_Base_Y, pInDlssDEvalParams.InDiffuseAlbedoSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_SpecularAlbedo_Subrect_Base_X, pInDlssDEvalParams.InSpecularAlbedoSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_SpecularAlbedo_Subrect_Base_Y, pInDlssDEvalParams.InSpecularAlbedoSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Normals_Subrect_Base_X, pInDlssDEvalParams.InNormalsSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Normals_Subrect_Base_Y, pInDlssDEvalParams.InNormalsSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Roughness_Subrect_Base_X, pInDlssDEvalParams.InRoughnessSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_Input_Roughness_Subrect_Base_Y, pInDlssDEvalParams.InRoughnessSubrectBase.Y);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Normals, pInDlssDEvalParams.pInNormals);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_GBuffer_Roughness, pInDlssDEvalParams.pInRoughness);

    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_Alpha, pInDlssDEvalParams.pInAlpha);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_OutputAlpha, pInDlssDEvalParams.pInOutputAlpha);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_ReflectedAlbedo, pInDlssDEvalParams.pInReflectedAlbedo);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeParticles, pInDlssDEvalParams.pInColorBeforeParticles);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterParticles, pInDlssDEvalParams.pInColorAfterParticles);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeTransparency, pInDlssDEvalParams.pInColorBeforeTransparency);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterTransparency, pInDlssDEvalParams.pInColorAfterTransparency);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeFog, pInDlssDEvalParams.pInColorBeforeFog);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterFog, pInDlssDEvalParams.pInColorAfterFog);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_ScreenSpaceSubsurfaceScatteringGuide, pInDlssDEvalParams.pInScreenSpaceSubsurfaceScatteringGuide);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeScreenSpaceSubsurfaceScattering, pInDlssDEvalParams.pInColorBeforeScreenSpaceSubsurfaceScattering);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterScreenSpaceSubsurfaceScattering, pInDlssDEvalParams.pInColorAfterScreenSpaceSubsurfaceScattering);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_ScreenSpaceRefractionGuide, pInDlssDEvalParams.pInScreenSpaceRefractionGuide);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeScreenSpaceRefraction, pInDlssDEvalParams.pInColorBeforeScreenSpaceRefraction);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterScreenSpaceRefraction, pInDlssDEvalParams.pInColorAfterScreenSpaceRefraction);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_DepthOfFieldGuide, pInDlssDEvalParams.pInDepthOfFieldGuide);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeDepthOfField, pInDlssDEvalParams.pInColorBeforeDepthOfField);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterDepthOfField, pInDlssDEvalParams.pInColorAfterDepthOfField);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_DiffuseHitDistance, pInDlssDEvalParams.pInDiffuseHitDistance);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_SpecularHitDistance, pInDlssDEvalParams.pInSpecularHitDistance);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_DiffuseRayDirection, pInDlssDEvalParams.pInDiffuseRayDirection);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_SpecularRayDirection, pInDlssDEvalParams.pInSpecularRayDirection);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_DiffuseRayDirectionHitDistance, pInDlssDEvalParams.pInDiffuseRayDirectionHitDistance);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSSD_SpecularRayDirectionHitDistance, pInDlssDEvalParams.pInSpecularRayDirectionHitDistance);

    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_Alpha_Subrect_Base_X, pInDlssDEvalParams.InAlphaSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_Alpha_Subrect_Base_Y, pInDlssDEvalParams.InAlphaSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_OutputAlpha_Subrect_Base_X, pInDlssDEvalParams.InOutputAlphaSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_OutputAlpha_Subrect_Base_Y, pInDlssDEvalParams.InOutputAlphaSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ReflectedAlbedo_Subrect_Base_X, pInDlssDEvalParams.InReflectedAlbedoSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ReflectedAlbedo_Subrect_Base_Y, pInDlssDEvalParams.InReflectedAlbedoSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterParticles_Subrect_Base_X, pInDlssDEvalParams.InColorAfterParticlesSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterParticles_Subrect_Base_Y, pInDlssDEvalParams.InColorAfterParticlesSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeParticles_Subrect_Base_X, pInDlssDEvalParams.InColorBeforeParticlesSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeParticles_Subrect_Base_Y, pInDlssDEvalParams.InColorBeforeParticlesSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeTransparency_Subrect_Base_X, pInDlssDEvalParams.InColorBeforeTransparencySubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeTransparency_Subrect_Base_Y, pInDlssDEvalParams.InColorBeforeTransparencySubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterTransparency_Subrect_Base_X, pInDlssDEvalParams.InColorAfterTransparencySubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterTransparency_Subrect_Base_Y, pInDlssDEvalParams.InColorAfterTransparencySubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterFog_Subrect_Base_X, pInDlssDEvalParams.InColorAfterFogSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterFog_Subrect_Base_Y, pInDlssDEvalParams.InColorAfterFogSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeFog_Subrect_Base_X, pInDlssDEvalParams.InColorBeforeFogSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeFog_Subrect_Base_Y, pInDlssDEvalParams.InColorBeforeFogSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ScreenSpaceSubsurfaceScatteringGuide_Subrect_Base_X, pInDlssDEvalParams.InScreenSpaceSubsurfaceScatteringGuideSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ScreenSpaceSubsurfaceScatteringGuide_Subrect_Base_Y, pInDlssDEvalParams.InScreenSpaceSubsurfaceScatteringGuideSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeScreenSpaceSubsurfaceScattering_Subrect_Base_X, pInDlssDEvalParams.InColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeScreenSpaceSubsurfaceScattering_Subrect_Base_Y, pInDlssDEvalParams.InColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterScreenSpaceSubsurfaceScattering_Subrect_Base_X, pInDlssDEvalParams.InColorAfterScreenSpaceSubsurfaceScatteringSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterScreenSpaceSubsurfaceScattering_Subrect_Base_Y, pInDlssDEvalParams.InColorAfterScreenSpaceSubsurfaceScatteringSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ScreenSpaceRefractionGuide_Subrect_Base_X, pInDlssDEvalParams.InScreenSpaceRefractionGuideSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ScreenSpaceRefractionGuide_Subrect_Base_Y, pInDlssDEvalParams.InScreenSpaceRefractionGuideSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeScreenSpaceRefraction_Subrect_Base_X, pInDlssDEvalParams.InColorBeforeScreenSpaceRefractionSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeScreenSpaceRefraction_Subrect_Base_Y, pInDlssDEvalParams.InColorBeforeScreenSpaceRefractionSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterScreenSpaceRefraction_Subrect_Base_X, pInDlssDEvalParams.InColorAfterScreenSpaceRefractionSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterScreenSpaceRefraction_Subrect_Base_Y, pInDlssDEvalParams.InColorAfterScreenSpaceRefractionSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_DepthOfFieldGuide_Subrect_Base_X, pInDlssDEvalParams.InDepthOfFieldGuideSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_DepthOfFieldGuide_Subrect_Base_Y, pInDlssDEvalParams.InDepthOfFieldGuideSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeDepthOfField_Subrect_Base_X, pInDlssDEvalParams.InColorBeforeDepthOfFieldSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorBeforeDepthOfField_Subrect_Base_Y, pInDlssDEvalParams.InColorBeforeDepthOfFieldSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterDepthOfField_Subrect_Base_X, pInDlssDEvalParams.InColorAfterDepthOfFieldSubtectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_ColorAfterDepthOfField_Subrect_Base_Y, pInDlssDEvalParams.InColorAfterDepthOfFieldSubtectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_DiffuseHitDistance_Subrect_Base_X, pInDlssDEvalParams.InDiffuseHitDistanceSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_DiffuseHitDistance_Subrect_Base_Y, pInDlssDEvalParams.InDiffuseHitDistanceSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_SpecularHitDistance_Subrect_Base_X, pInDlssDEvalParams.InSpecularHitDistanceSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_SpecularHitDistance_Subrect_Base_Y, pInDlssDEvalParams.InSpecularHitDistanceSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_DiffuseRayDirection_Subrect_Base_X, pInDlssDEvalParams.InDiffuseRayDirectionSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_DiffuseRayDirection_Subrect_Base_Y, pInDlssDEvalParams.InDiffuseRayDirectionSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_SpecularRayDirection_Subrect_Base_X, pInDlssDEvalParams.InSpecularRayDirectionSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_SpecularRayDirection_Subrect_Base_Y, pInDlssDEvalParams.InSpecularRayDirectionSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_DiffuseRayDirectionHitDistance_Subrect_Base_X, pInDlssDEvalParams.InDiffuseRayDirectionHitDistanceSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_DiffuseRayDirectionHitDistance_Subrect_Base_Y, pInDlssDEvalParams.InDiffuseRayDirectionHitDistanceSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_SpecularRayDirectionHitDistance_Subrect_Base_X, pInDlssDEvalParams.InSpecularRayDirectionHitDistanceSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSSD_SpecularRayDirectionHitDistance_Subrect_Base_Y, pInDlssDEvalParams.InSpecularRayDirectionHitDistanceSubrectBase.Y);    
    DLSS_Parameter_SetMatrix4x4(pInParams, NVSDK_NGX_Parameter_DLSS_WORLD_TO_VIEW_MATRIX, pInDlssDEvalParams.InWorldToViewMatrix);
    DLSS_Parameter_SetMatrix4x4(pInParams, NVSDK_NGX_Parameter_DLSS_VIEW_TO_CLIP_MATRIX, pInDlssDEvalParams.InViewToClipMatrix);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSS_TransparencyLayer, pInDlssDEvalParams.pInTransparencyLayer);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSS_TransparencyLayerOpacity, pInDlssDEvalParams.pInTransparencyLayerOpacity);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSS_TransparencyLayerMvecs, pInDlssDEvalParams.pInTransparencyLayerMvecs);
    DLSS_Parameter_SetD3d12Resource(pInParams, NVSDK_NGX_Parameter_DLSS_DisocclusionMask, pInDlssDEvalParams.pInDisocclusionMask);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_TransparencyLayer_Subrect_Base_X, pInDlssDEvalParams.InTransparencyLayerSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_TransparencyLayer_Subrect_Base_Y, pInDlssDEvalParams.InTransparencyLayerSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_TransparencyLayerOpacity_Subrect_Base_X, pInDlssDEvalParams.InTransparencyLayerOpacitySubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_TransparencyLayerOpacity_Subrect_Base_Y, pInDlssDEvalParams.InTransparencyLayerOpacitySubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_TransparencyLayerMvecs_Subrect_Base_X, pInDlssDEvalParams.InTransparencyLayerMvecsSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_TransparencyLayerMvecs_Subrect_Base_Y, pInDlssDEvalParams.InTransparencyLayerMvecsSubrectBase.Y);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_DisocclusionMask_Subrect_Base_X, pInDlssDEvalParams.InDisocclusionMaskSubrectBase.X);
    DLSS_Parameter_SetUI(pInParams, NVSDK_NGX_Parameter_DLSS_DisocclusionMask_Subrect_Base_Y, pInDlssDEvalParams.InDisocclusionMaskSubrectBase.Y);

    DLSS_EvaluateFeature(pInCmdList, pInHandle, pInParams);
}
}
}
