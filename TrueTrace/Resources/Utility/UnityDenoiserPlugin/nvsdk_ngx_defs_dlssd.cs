using System;
using System.Runtime.InteropServices;

namespace DenoiserPlugin
{
public static partial class DlssSdk
{

public enum NVSDK_NGX_DLSS_Denoise_Mode
{
    NVSDK_NGX_DLSS_Denoise_Mode_Off = 0,
    NVSDK_NGX_DLSS_Denoise_Mode_DLUnified = 1, // DL based unified upscaler
}

public enum NVSDK_NGX_DLSS_Roughness_Mode
{
    NVSDK_NGX_DLSS_Roughness_Mode_Unpacked = 0, // Read roughness separately 
    NVSDK_NGX_DLSS_Roughness_Mode_Packed = 1, // Read roughness from normals.w
}

public enum NVSDK_NGX_DLSS_Depth_Type
{
    NVSDK_NGX_DLSS_Depth_Type_Linear = 0, // Linear Depth
    NVSDK_NGX_DLSS_Depth_Type_HW = 1,     // HW Depth
}

public enum NVSDK_NGX_RayReconstruction_Hint_Render_Preset
{
    NVSDK_NGX_RayReconstruction_Hint_Render_Preset_Default,     // default behavior, may or may not change after OTA
    NVSDK_NGX_RayReconstruction_Hint_Render_Preset_A,
    NVSDK_NGX_RayReconstruction_Hint_Render_Preset_B,
    NVSDK_NGX_RayReconstruction_Hint_Render_Preset_C,
    NVSDK_NGX_RayReconstruction_Hint_Render_Preset_D,
    NVSDK_NGX_RayReconstruction_Hint_Render_Preset_E,           // do not use, reverts to default behavior
    NVSDK_NGX_RayReconstruction_Hint_Render_Preset_F,           // do not use, reverts to default behavior
    NVSDK_NGX_RayReconstruction_Hint_Render_Preset_G,           // do not use, reverts to default behavior
    NVSDK_NGX_RayReconstruction_Hint_Render_Preset_H,           // do not use, reverts to default behavior
    NVSDK_NGX_RayReconstruction_Hint_Render_Preset_I,           // do not use, reverts to default behavior
    NVSDK_NGX_RayReconstruction_Hint_Render_Preset_J,           // do not use, reverts to default behavior
    NVSDK_NGX_RayReconstruction_Hint_Render_Preset_K,           // do not use, reverts to default behavior
    NVSDK_NGX_RayReconstruction_Hint_Render_Preset_L,           // do not use, reverts to default behavior
    NVSDK_NGX_RayReconstruction_Hint_Render_Preset_M,           // do not use, reverts to default behavior
    NVSDK_NGX_RayReconstruction_Hint_Render_Preset_N,           // do not use, reverts to default behavior
    NVSDK_NGX_RayReconstruction_Hint_Render_Preset_O,           // do not use, reverts to default behavior
}

public const string NVSDK_NGX_Parameter_DLSS_Denoise_Mode = "DLSS.Denoise.Mode";
public const string NVSDK_NGX_Parameter_DLSS_Roughness_Mode = "DLSS.Roughness.Mode";
public const string NVSDK_NGX_Parameter_DiffuseAlbedo = "DLSS.Input.DiffuseAlbedo";
public const string NVSDK_NGX_Parameter_SpecularAlbedo = "DLSS.Input.SpecularAlbedo";
public const string NVSDK_NGX_Parameter_DLSS_Input_DiffuseAlbedo_Subrect_Base_X = "DLSS.Input.DiffuseAlbedo.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSS_Input_DiffuseAlbedo_Subrect_Base_Y = "DLSS.Input.DiffuseAlbedo.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSS_Input_SpecularAlbedo_Subrect_Base_X = "DLSS.Input.SpecularAlbedo.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSS_Input_SpecularAlbedo_Subrect_Base_Y = "DLSS.Input.SpecularAlbedo.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSS_Input_Normals_Subrect_Base_X = "DLSS.Input.Normals.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSS_Input_Normals_Subrect_Base_Y = "DLSS.Input.Normals.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSS_Input_Roughness_Subrect_Base_X = "DLSS.Input.Roughness.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSS_Input_Roughness_Subrect_Base_Y = "DLSS.Input.Roughness.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_ViewToClipMatrix = "ViewToClipMatrix";
public const string NVSDK_NGX_Parameter_GBuffer_Emissive = "GBuffer.Emissive";
public const string NVSDK_NGX_Parameter_Use_Folded_Network = "DLSS.Use.Folded.Network";
public const string NVSDK_NGX_Parameter_Diffuse_Ray_Direction = "Diffuse.Ray.Direction";
public const string NVSDK_NGX_Parameter_DLSS_WORLD_TO_VIEW_MATRIX = "WorldToViewMatrix";
public const string NVSDK_NGX_Parameter_DLSS_VIEW_TO_CLIP_MATRIX = "ViewToClipMatrix";
public const string NVSDK_NGX_Parameter_Use_HW_Depth = "DLSS.Use.HW.Depth";
public const string NVSDK_NGX_Parameter_DLSSD_ReflectedAlbedo = "DLSSD.ReflectedAlbedo";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeParticles = "DLSSD.ColorBeforeParticles";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterParticles = "DLSSD.ColorAfterParticles";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeTransparency = "DLSSD.ColorBeforeTransparency";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterTransparency = "DLSSD.ColorAfterTransparency";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeFog = "DLSSD.ColorBeforeFog";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterFog = "DLSSD.ColorAfterFog";
public const string NVSDK_NGX_Parameter_DLSSD_ScreenSpaceSubsurfaceScatteringGuide = "DLSSD.ScreenSpaceSubsurfaceScatteringGuide";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeScreenSpaceSubsurfaceScattering = "DLSSD.ColorBeforeScreenSpaceSubsurfaceScattering";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterScreenSpaceSubsurfaceScattering = "DLSSD.ColorAfterScreenSpaceSubsurfaceScattering";
public const string NVSDK_NGX_Parameter_DLSSD_ScreenSpaceRefractionGuide = "DLSSD.ScreenSpaceRefractionGuide";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeScreenSpaceRefraction = "DLSSD.ColorBeforeScreenSpaceRefraction";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterScreenSpaceRefraction = "DLSSD.ColorAfterScreenSpaceRefraction";
public const string NVSDK_NGX_Parameter_DLSSD_DepthOfFieldGuide = "DLSSD.DepthOfFieldGuide";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeDepthOfField = "DLSSD.ColorBeforeDepthOfField";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterDepthOfField = "DLSSD.ColorAfterDepthOfField";
public const string NVSDK_NGX_Parameter_DLSSD_DiffuseHitDistance = "DLSSD.DiffuseHitDistance";
public const string NVSDK_NGX_Parameter_DLSSD_SpecularHitDistance = "DLSSD.SpecularHitDistance";
public const string NVSDK_NGX_Parameter_DLSSD_DiffuseRayDirection = "DLSSD.DiffuseRayDirection";
public const string NVSDK_NGX_Parameter_DLSSD_SpecularRayDirection = "DLSSD.SpecularRayDirection";
public const string NVSDK_NGX_Parameter_DLSSD_DiffuseRayDirectionHitDistance = "DLSSD.DiffuseRayDirectionHitDistance";
public const string NVSDK_NGX_Parameter_DLSSD_SpecularRayDirectionHitDistance = "DLSSD.SpecularRayDirectionHitDistance";
public const string NVSDK_NGX_Parameter_DLSSD_Alpha = "DLSSD.Alpha";
public const string NVSDK_NGX_Parameter_DLSSD_OutputAlpha = "DLSSD.OutputAlpha";
public const string NVSDK_NGX_Parameter_DLSSD_Alpha_Subrect_Base_X = "DLSSD.Alpha.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_Alpha_Subrect_Base_Y = "DLSSD.Alpha.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_OutputAlpha_Subrect_Base_X = "DLSSD.OutputAlpha.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_OutputAlpha_Subrect_Base_Y = "DLSSD.OutputAlpha.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_ReflectedAlbedo_Subrect_Base_X = "DLSSD.ReflectedAlbedo.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_ReflectedAlbedo_Subrect_Base_Y = "DLSSD.ReflectedAlbedo.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeParticles_Subrect_Base_X = "DLSSD.ColorBeforeParticles.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeParticles_Subrect_Base_Y = "DLSSD.ColorBeforeParticles.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterParticles_Subrect_Base_X = "DLSSD.ColorAfterParticles.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterParticles_Subrect_Base_Y = "DLSSD.ColorAfterParticles.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeTransparency_Subrect_Base_X = "DLSSD.ColorBeforeTransparency.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeTransparency_Subrect_Base_Y = "DLSSD.ColorBeforeTransparency.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterTransparency_Subrect_Base_X = "DLSSD.ColorAfterTransparency.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterTransparency_Subrect_Base_Y = "DLSSD.ColorAfterTransparency.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeFog_Subrect_Base_X = "DLSSD.ColorBeforeFog.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeFog_Subrect_Base_Y = "DLSSD.ColorBeforeFog.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterFog_Subrect_Base_X = "DLSSD.ColorAfterFog.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterFog_Subrect_Base_Y = "DLSSD.ColorAfterFog.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_ScreenSpaceSubsurfaceScatteringGuide_Subrect_Base_X = "DLSSD.ScreenSpaceSubsurfaceScatteringGuide.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_ScreenSpaceSubsurfaceScatteringGuide_Subrect_Base_Y = "DLSSD.ScreenSpaceSubsurfaceScatteringGuide.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeScreenSpaceSubsurfaceScattering_Subrect_Base_X = "DLSSD.ColorBeforeScreenSpaceSubsurfaceScattering.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeScreenSpaceSubsurfaceScattering_Subrect_Base_Y = "DLSSD.ColorBeforeScreenSpaceSubsurfaceScattering.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterScreenSpaceSubsurfaceScattering_Subrect_Base_X = "DLSSD.ColorAfterScreenSpaceSubsurfaceScattering.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterScreenSpaceSubsurfaceScattering_Subrect_Base_Y = "DLSSD.ColorAfterScreenSpaceSubsurfaceScattering.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_ScreenSpaceRefractionGuide_Subrect_Base_X = "DLSSD.ScreenSpaceRefractionGuide.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_ScreenSpaceRefractionGuide_Subrect_Base_Y = "DLSSD.ScreenSpaceRefractionGuide.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeScreenSpaceRefraction_Subrect_Base_X = "DLSSD.ColorBeforeScreenSpaceRefraction.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeScreenSpaceRefraction_Subrect_Base_Y = "DLSSD.ColorBeforeScreenSpaceRefraction.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterScreenSpaceRefraction_Subrect_Base_X = "DLSSD.ColorAfterScreenSpaceRefraction.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterScreenSpaceRefraction_Subrect_Base_Y = "DLSSD.ColorAfterScreenSpaceRefraction.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_DepthOfFieldGuide_Subrect_Base_X = "DLSSD.DepthOfFieldGuide.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_DepthOfFieldGuide_Subrect_Base_Y = "DLSSD.DepthOfFieldGuide.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeDepthOfField_Subrect_Base_X = "DLSSD.ColorBeforeDepthOfField.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_ColorBeforeDepthOfField_Subrect_Base_Y = "DLSSD.ColorBeforeDepthOfField.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterDepthOfField_Subrect_Base_X = "DLSSD.ColorAfterDepthOfField.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_ColorAfterDepthOfField_Subrect_Base_Y = "DLSSD.ColorAfterDepthOfField.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_DiffuseHitDistance_Subrect_Base_X = "DLSSD.DiffuseHitDistance.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_DiffuseHitDistance_Subrect_Base_Y = "DLSSD.DiffuseHitDistance.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_SpecularHitDistance_Subrect_Base_X = "DLSSD.SpecularHitDistance.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_SpecularHitDistance_Subrect_Base_Y = "DLSSD.SpecularHitDistance.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_DiffuseRayDirection_Subrect_Base_X = "DLSSD.DiffuseRayDirection.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_DiffuseRayDirection_Subrect_Base_Y = "DLSSD.DiffuseRayDirection.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_SpecularRayDirection_Subrect_Base_X = "DLSSD.SpecularRayDirection.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_SpecularRayDirection_Subrect_Base_Y = "DLSSD.SpecularRayDirection.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_DiffuseRayDirectionHitDistance_Subrect_Base_X = "DLSSD.DiffuseRayDirectionHitDistance.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_DiffuseRayDirectionHitDistance_Subrect_Base_Y = "DLSSD.DiffuseRayDirectionHitDistance.Subrect.Base.Y";
public const string NVSDK_NGX_Parameter_DLSSD_SpecularRayDirectionHitDistance_Subrect_Base_X = "DLSSD.SpecularRayDirectionHitDistance.Subrect.Base.X";
public const string NVSDK_NGX_Parameter_DLSSD_SpecularRayDirectionHitDistance_Subrect_Base_Y = "DLSSD.SpecularRayDirectionHitDistance.Subrect.Base.Y";

public const string NVSDK_NGX_Parameter_SuperSamplingDenoising_Available = "SuperSamplingDenoising.Available";
public const string NVSDK_NGX_Parameter_SuperSamplingDenoising_NeedsUpdatedDriver = "SuperSamplingDenoising.NeedsUpdatedDriver";
public const string NVSDK_NGX_Parameter_SuperSamplingDenoising_MinDriverVersionMajor = "SuperSamplingDenoising.MinDriverVersionMajor";
public const string NVSDK_NGX_Parameter_SuperSamplingDenoising_MinDriverVersionMinor = "SuperSamplingDenoising.MinDriverVersionMinor";
public const string NVSDK_NGX_Parameter_SuperSamplingDenoising_FeatureInitResult = "SuperSamplingDenoising.FeatureInitResult";
public const string NVSDK_NGX_Parameter_DLSSDOptimalSettingsCallback = "DLSSDOptimalSettingsCallback";
public const string NVSDK_NGX_Parameter_DLSSDGetStatsCallback = "DLSSDGetStatsCallback";

public const string NVSDK_NGX_Parameter_RayReconstruction_Hint_Render_Preset_DLAA = "RayReconstruction.Hint.Render.Preset.DLAA";
public const string NVSDK_NGX_Parameter_RayReconstruction_Hint_Render_Preset_Quality = "RayReconstruction.Hint.Render.Preset.Quality";
public const string NVSDK_NGX_Parameter_RayReconstruction_Hint_Render_Preset_Balanced = "RayReconstruction.Hint.Render.Preset.Balanced";
public const string NVSDK_NGX_Parameter_RayReconstruction_Hint_Render_Preset_Performance = "RayReconstruction.Hint.Render.Preset.Performance";
public const string NVSDK_NGX_Parameter_RayReconstruction_Hint_Render_Preset_UltraPerformance = "RayReconstruction.Hint.Render.Preset.UltraPerformance";
public const string NVSDK_NGX_Parameter_RayReconstruction_Hint_Render_Preset_UltraQuality = "RayReconstruction.Hint.Render.Preset.UltraQuality";

}
} 