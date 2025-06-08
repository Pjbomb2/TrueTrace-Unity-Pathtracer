using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace DenoiserPlugin
{
public static partial class DlssSdk
{
public static class DlssCSharpBinding
{

public const int DLSS_INVALID_FEATURE_HANDLE = -1;

private const string DllName = "UnityDenoiserPlugin";

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct DLSSInitParams
{
    [MarshalAs(UnmanagedType.LPStr)]
    public string projectId;
    
    public NVSDK_NGX_EngineType engineType;
    
    [MarshalAs(UnmanagedType.LPStr)]
    public string engineVersion;
    
    [MarshalAs(UnmanagedType.LPWStr)]
    public string applicationDataPath;
    
    public NVSDK_NGX_Logging_Level loggingLevel;
}

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
public static extern NVSDK_NGX_Result DLSS_Init_with_ProjectID_D3D12(
    ref DLSSInitParams initParams
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
public static extern NVSDK_NGX_Result DLSS_Shutdown_D3D12();

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
public static extern NVSDK_NGX_Result DLSS_AllocateParameters_D3D12(
    out IntPtr ppOutParameters
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
public static extern NVSDK_NGX_Result DLSS_GetCapabilityParameters_D3D12(
    out IntPtr ppOutParameters
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
public static extern NVSDK_NGX_Result DLSS_DestroyParameters_D3D12(
    IntPtr pInParameters
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern void DLSS_Parameter_SetULL(
    IntPtr pParameters,
    [MarshalAs(UnmanagedType.LPStr)] string paramName,
    ulong value
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern void DLSS_Parameter_SetF(
    IntPtr pParameters,
    [MarshalAs(UnmanagedType.LPStr)] string paramName,
    float value
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern void DLSS_Parameter_SetD(
    IntPtr pParameters,
    [MarshalAs(UnmanagedType.LPStr)] string paramName,
    double value
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern void DLSS_Parameter_SetUI(
    IntPtr pParameters,
    [MarshalAs(UnmanagedType.LPStr)] string paramName,
    uint value
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern void DLSS_Parameter_SetI(
    IntPtr pParameters,
    [MarshalAs(UnmanagedType.LPStr)] string paramName,
    int value
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern void DLSS_Parameter_SetD3d11Resource(
    IntPtr pParameters,
    [MarshalAs(UnmanagedType.LPStr)] string paramName,
    IntPtr value // ID3D11Resource*
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern void DLSS_Parameter_SetD3d12Resource(
    IntPtr pParameters,
    [MarshalAs(UnmanagedType.LPStr)] string paramName,
    IntPtr value // ID3D12Resource*
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern void DLSS_Parameter_SetVoidPointer(
    IntPtr pParameters,
    [MarshalAs(UnmanagedType.LPStr)] string paramName,
    IntPtr value // void* maps to IntPtr
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern NVSDK_NGX_Result DLSS_Parameter_GetULL(
    IntPtr pParameters,
    [MarshalAs(UnmanagedType.LPStr)] string paramName,
    out ulong value
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern NVSDK_NGX_Result DLSS_Parameter_GetF(
    IntPtr pParameters,
    [MarshalAs(UnmanagedType.LPStr)] string paramName,
    out float value
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern NVSDK_NGX_Result DLSS_Parameter_GetD(
    IntPtr pParameters,
    [MarshalAs(UnmanagedType.LPStr)] string paramName,
    out double value
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern NVSDK_NGX_Result DLSS_Parameter_GetUI(
    IntPtr pParameters,
    [MarshalAs(UnmanagedType.LPStr)] string paramName,
    out uint value
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern NVSDK_NGX_Result DLSS_Parameter_GetI(
    IntPtr pParameters,
    [MarshalAs(UnmanagedType.LPStr)] string paramName,
    out int value
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern NVSDK_NGX_Result DLSS_Parameter_GetD3d11Resource(
    IntPtr pParameters,
    [MarshalAs(UnmanagedType.LPStr)] string paramName,
    out IntPtr value // ID3D11Resource**
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern NVSDK_NGX_Result DLSS_Parameter_GetD3d12Resource(
    IntPtr pParameters,
    [MarshalAs(UnmanagedType.LPStr)] string paramName,
    out IntPtr value // ID3D12Resource**
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern NVSDK_NGX_Result DLSS_Parameter_GetVoidPointer(
    IntPtr pParameters,
    [MarshalAs(UnmanagedType.LPStr)] string paramName,
    out IntPtr value // void** maps to out IntPtr
);

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
public static extern int DLSS_AllocateFeatureHandle();

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
public static extern int DLSS_FreeFeatureHandle(int handle);

[StructLayout(LayoutKind.Sequential)]
public struct DLSSCreateFeatureParams 
{
    public int handle;
    public NVSDK_NGX_Feature feature;
    public IntPtr parameters; 
};

[StructLayout(LayoutKind.Sequential)]
public struct DLSSEvaluateFeatureParams
{
    public int handle;
    public IntPtr parameters;
};

[StructLayout(LayoutKind.Sequential)]
public struct DLSSDestroyFeatureParams
{
    public int handle;
};

// Event IDs for C++ to distinguish commands
public const int EVENT_ID_CREATE_FEATURE = 0;
public const int EVENT_ID_EVALUATE_FEATURE = 1;
public const int EVENT_ID_DESTROY_FEATURE = 2;

[DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
public static extern IntPtr DLSS_UnityRenderEventFunc();

}
}
}
