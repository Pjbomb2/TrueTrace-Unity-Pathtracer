using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using Binding = DenoiserPlugin.DlssSdk.DlssCSharpBinding;

namespace DenoiserPlugin
{
public static partial class DlssSdk
{

public const int DLSS_INVALID_FEATURE_HANDLE = Binding.DLSS_INVALID_FEATURE_HANDLE;

// Reference counter to track initialization
private static int s_dlssInitializationCount = 0;

// Cached feature availability results
private static bool s_SuperSamplingAvailable = false;
private static bool s_RayReconstructionAvailable = false;

// Ring buffer for c# and c++ interop
private static RingBufferAllocator _commandDataAllocator;
private const int CommandDataBufferSize = 2 * 1024 * 1024; // 2MB, adjust as needed

public static NVSDK_NGX_Result DLSS_Init()
{
    // Increment the reference counter
    s_dlssInitializationCount++;
    
    // Only initialize if this is the first call
    if (s_dlssInitializationCount == 1)
    {   
        // We're using DirectX 12 in Unity
        var initParams = new Binding.DLSSInitParams();
        initParams.projectId = "";
        initParams.engineType = NVSDK_NGX_EngineType.NVSDK_NGX_ENGINE_TYPE_UNITY;
        initParams.engineVersion = Application.version;
        initParams.applicationDataPath = "";
        initParams.loggingLevel = NVSDK_NGX_Logging_Level.NVSDK_NGX_LOGGING_LEVEL_VERBOSE;
        NVSDK_NGX_Result result = Binding.DLSS_Init_with_ProjectID_D3D12(ref initParams);
        LogDlssResult(result, "DLSS_Init");

        // Query and cache feature availability
        if (NVSDK_NGX_SUCCEED(result))
        {
            QueryFeatureAvailability();
        }

        return result;
    }
    
    return NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
}

public static NVSDK_NGX_Result DLSS_Shutdown()
{
    // Check if already shut down
    if (s_dlssInitializationCount <= 0)
    {
        s_dlssInitializationCount = 0;
        return NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
    }
    
    // Decrement the reference counter
    s_dlssInitializationCount--;
    
    // Only shutdown if this is the last reference
    if (s_dlssInitializationCount == 0)
    {
        // Reset cached availability
        s_SuperSamplingAvailable = false;
        s_RayReconstructionAvailable = false;
        
        // Dispose allocator
        _commandDataAllocator?.Dispose();
        _commandDataAllocator = null;
        
        var result = Binding.DLSS_Shutdown_D3D12();
        LogDlssResult(result, "DLSS_Shutdown");
        return result;
    }
    
    return NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
}

public static bool DLSS_IsSuperSamplingAvailable()
{
    return s_SuperSamplingAvailable;
}

public static bool DLSS_IsRayReconstructionAvailable()
{
    return s_RayReconstructionAvailable;
}

public static int DLSS_CreateFeature(CommandBuffer cmd, NVSDK_NGX_Feature feature, IntPtr parameters)
{
    int handle = Binding.DLSS_AllocateFeatureHandle();
    if (handle == DLSS_INVALID_FEATURE_HANDLE)
    {
        Debug.LogError("[CommandBufferExtensions] Failed to allocate feature handle from C++.");
        return DLSS_INVALID_FEATURE_HANDLE;
    }

    var commandParams = new Binding.DLSSCreateFeatureParams
    {
        handle = handle,
        feature = feature,
        parameters = parameters 
    };

    IntPtr absolutePointerToData = Allocator.Allocate(commandParams); // This is now an absolute pointer

    if (absolutePointerToData == IntPtr.Zero) // Check against IntPtr.Zero
    {
        Debug.LogError("[CommandBufferExtensions] Failed to allocate space in ring buffer for CreateDLSSFeature (returned absolute pointer was Zero).");
        Binding.DLSS_FreeFeatureHandle(handle);
        return DLSS_INVALID_FEATURE_HANDLE; 
    }
    
    cmd.IssuePluginEventAndData(Binding.DLSS_UnityRenderEventFunc(), Binding.EVENT_ID_CREATE_FEATURE, absolutePointerToData);
    return handle;
}

public static void DLSS_EvaluateFeature(CommandBuffer cmd, int handle, IntPtr parameters)
{
    var commandParams = new Binding.DLSSEvaluateFeatureParams
    {
        handle = handle,
        parameters = parameters
    };

    IntPtr absolutePointerToData = Allocator.Allocate(commandParams);

    if (absolutePointerToData == IntPtr.Zero)
    {
        Debug.LogError("[CommandBufferExtensions] Failed to allocate space in ring buffer for EvaluateDLSSFeature (returned absolute pointer was Zero).");
        return;
    }

    cmd.IssuePluginEventAndData(Binding.DLSS_UnityRenderEventFunc(), Binding.EVENT_ID_EVALUATE_FEATURE, absolutePointerToData);
}

public static void DLSS_DestroyFeature(CommandBuffer cmd, int handle)
{ 
    var commandParams = new Binding.DLSSDestroyFeatureParams
    {
        handle = handle
    };

    IntPtr absolutePointerToData = Allocator.Allocate(commandParams);

    if (absolutePointerToData == IntPtr.Zero)
    {
        Debug.LogError("[CommandBufferExtensions] Failed to allocate space in ring buffer for DestroyDLSSFeature (returned absolute pointer was Zero).");
        return;
    }

    cmd.IssuePluginEventAndData(Binding.DLSS_UnityRenderEventFunc(), Binding.EVENT_ID_DESTROY_FEATURE, absolutePointerToData);
}

public static NVSDK_NGX_Result DLSS_AllocateParameters_D3D12(out IntPtr ppOutParameters)
{
    var result = Binding.DLSS_AllocateParameters_D3D12(out ppOutParameters);
    LogDlssResult(result, "DLSS_AllocateParameters_D3D12");
    return result;
}

public static NVSDK_NGX_Result DLSS_GetCapabilityParameters_D3D12(out IntPtr ppOutParameters)
{
    var result = Binding.DLSS_GetCapabilityParameters_D3D12(out ppOutParameters);
    LogDlssResult(result, "DLSS_GetCapabilityParameters_D3D12");
    return result;
}

public static NVSDK_NGX_Result DLSS_DestroyParameters_D3D12(IntPtr pInParameters)
{
    var result = Binding.DLSS_DestroyParameters_D3D12(pInParameters);
    LogDlssResult(result, "DLSS_DestroyParameters_D3D12");
    return result;
}

public static NVSDK_NGX_Result DLSS_Parameter_SetULL(IntPtr pParameters, string paramName, ulong value)
{
    Binding.DLSS_Parameter_SetULL(pParameters, paramName, value);
    return NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
}

public static NVSDK_NGX_Result DLSS_Parameter_GetULL(IntPtr pParameters, string paramName, out ulong value)
{
    var result = Binding.DLSS_Parameter_GetULL(pParameters, paramName, out value);
    LogDlssResult(result, "DLSS_Parameter_GetULL");
    return result;
}

public static NVSDK_NGX_Result DLSS_Parameter_SetF(IntPtr pParameters, string paramName, float value)
{
    Binding.DLSS_Parameter_SetF(pParameters, paramName, value);
    return NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
}

public static NVSDK_NGX_Result DLSS_Parameter_GetF(IntPtr pParameters, string paramName, out float value)
{
    var result = Binding.DLSS_Parameter_GetF(pParameters, paramName, out value);
    LogDlssResult(result, "DLSS_Parameter_GetF");
    return result;
}

public static NVSDK_NGX_Result DLSS_Parameter_SetD(IntPtr pParameters, string paramName, double value)
{
    Binding.DLSS_Parameter_SetD(pParameters, paramName, value);
    return NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
}

public static NVSDK_NGX_Result DLSS_Parameter_GetD(IntPtr pParameters, string paramName, out double value)
{
    var result = Binding.DLSS_Parameter_GetD(pParameters, paramName, out value);
    LogDlssResult(result, "DLSS_Parameter_GetD");
    return result;
}

public static NVSDK_NGX_Result DLSS_Parameter_SetUI(IntPtr pParameters, string paramName, uint value)
{
    Binding.DLSS_Parameter_SetUI(pParameters, paramName, value);
    return NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
}

public static NVSDK_NGX_Result DLSS_Parameter_GetUI(IntPtr pParameters, string paramName, out uint value)
{
    var result = Binding.DLSS_Parameter_GetUI(pParameters, paramName, out value);
    LogDlssResult(result, "DLSS_Parameter_GetUI");
    return result;
}

public static NVSDK_NGX_Result DLSS_Parameter_SetI(IntPtr pParameters, string paramName, int value)
{
    Binding.DLSS_Parameter_SetI(pParameters, paramName, value);
    return NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
}

public static NVSDK_NGX_Result DLSS_Parameter_GetI(IntPtr pParameters, string paramName, out int value)
{
    var result = Binding.DLSS_Parameter_GetI(pParameters, paramName, out value);
    LogDlssResult(result, "DLSS_Parameter_GetI");
    return result;
}

public static NVSDK_NGX_Result DLSS_Parameter_SetD3d11Resource(IntPtr pParameters, string paramName, IntPtr value)
{
    Binding.DLSS_Parameter_SetD3d11Resource(pParameters, paramName, value);
    return NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
}

public static NVSDK_NGX_Result DLSS_Parameter_GetD3d11Resource(IntPtr pParameters, string paramName, out IntPtr value)
{
    var result = Binding.DLSS_Parameter_GetD3d11Resource(pParameters, paramName, out value);
    LogDlssResult(result, "DLSS_Parameter_GetD3d11Resource");
    return result;
}

public static NVSDK_NGX_Result DLSS_Parameter_SetD3d12Resource(IntPtr pParameters, string paramName, IntPtr value)
{
    Binding.DLSS_Parameter_SetD3d12Resource(pParameters, paramName, value);
    return NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
}

public static NVSDK_NGX_Result DLSS_Parameter_GetD3d12Resource(IntPtr pParameters, string paramName, out IntPtr value)
{
    var result = Binding.DLSS_Parameter_GetD3d12Resource(pParameters, paramName, out value);
    LogDlssResult(result, "DLSS_Parameter_GetD3d12Resource");
    return result;
}

public static NVSDK_NGX_Result DLSS_Parameter_SetVoidPointer(IntPtr pParameters, string paramName, IntPtr value)
{
    Binding.DLSS_Parameter_SetVoidPointer(pParameters, paramName, value);
    return NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
}

public static NVSDK_NGX_Result DLSS_Parameter_GetVoidPointer(IntPtr pParameters, string paramName, out IntPtr value)
{
    var result = Binding.DLSS_Parameter_GetVoidPointer(pParameters, paramName, out value);
    LogDlssResult(result, "DLSS_Parameter_GetVoidPointer");
    return result;
}

public static NVSDK_NGX_Result DLSS_Parameter_SetMatrix4x4(IntPtr pParameters, string paramName, Matrix4x4 value)
{
    // Allocate contiguous memory for 16 floats
    IntPtr ptr = Allocator.AllocateArray<float>(16);
    if (ptr == IntPtr.Zero)
    {
        return NVSDK_NGX_Result.NVSDK_NGX_Result_Fail;
    }

    // Manually fill matrix data
    unsafe
    {
        float* floatPtr = (float*)ptr.ToPointer();
        floatPtr[0] = value.m00; floatPtr[1] = value.m01; floatPtr[2] = value.m02; floatPtr[3] = value.m03;
        floatPtr[4] = value.m10; floatPtr[5] = value.m11; floatPtr[6] = value.m12; floatPtr[7] = value.m13;
        floatPtr[8] = value.m20; floatPtr[9] = value.m21; floatPtr[10] = value.m22; floatPtr[11] = value.m23;
        floatPtr[12] = value.m30; floatPtr[13] = value.m31; floatPtr[14] = value.m32; floatPtr[15] = value.m33;
    }

    Binding.DLSS_Parameter_SetVoidPointer(pParameters, paramName, ptr);
    return NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
}

public static NVSDK_NGX_Result DLSS_Parameter_GetMatrix4x4(IntPtr pParameters, string paramName, out Matrix4x4 value)
{
    value = Matrix4x4.zero;
    
    // Get pointer to matrix data
    NVSDK_NGX_Result result = Binding.DLSS_Parameter_GetVoidPointer(pParameters, paramName, out IntPtr ptr);
    if (!NVSDK_NGX_SUCCEED(result) || ptr == IntPtr.Zero)
    {
        return result;
    }

    // Read 16 float values from pointer and construct Matrix4x4
    unsafe
    {
        float* floatPtr = (float*)ptr.ToPointer();
        value = new Matrix4x4(
            new Vector4(floatPtr[0], floatPtr[1], floatPtr[2], floatPtr[3]),   // First column
            new Vector4(floatPtr[4], floatPtr[5], floatPtr[6], floatPtr[7]),   // Second column
            new Vector4(floatPtr[8], floatPtr[9], floatPtr[10], floatPtr[11]), // Third column
            new Vector4(floatPtr[12], floatPtr[13], floatPtr[14], floatPtr[15]) // Fourth column
        );
    }

    return NVSDK_NGX_Result.NVSDK_NGX_Result_Success;
}

// Static property for lazy initialization of the allocator
private static RingBufferAllocator Allocator
{
    get
    {
        if (_commandDataAllocator == null)
        {
            _commandDataAllocator = new RingBufferAllocator(CommandDataBufferSize);
        }
        return _commandDataAllocator;
    }
}

private static void QueryFeatureAvailability()
{
    if (NVSDK_NGX_SUCCEED(Binding.DLSS_GetCapabilityParameters_D3D12(out IntPtr pCapabilityParameters)))
    {
        // Query SuperSampling availability
        if (NVSDK_NGX_SUCCEED(Binding.DLSS_Parameter_GetI(pCapabilityParameters, NVSDK_NGX_Parameter_SuperSampling_Available, out int superSamplingSupport)))
        {
            s_SuperSamplingAvailable = superSamplingSupport != 0;
        }
        
        // Query RayReconstruction (SuperSamplingDenoising) availability
        if (NVSDK_NGX_SUCCEED(Binding.DLSS_Parameter_GetI(pCapabilityParameters, NVSDK_NGX_Parameter_SuperSamplingDenoising_Available, out int rayReconstructionSupport)))
        {
            s_RayReconstructionAvailable = rayReconstructionSupport != 0;
        }
        
        Binding.DLSS_DestroyParameters_D3D12(pCapabilityParameters);
    }
}

// Helper function to log DLSS errors
private static void LogDlssResult(NVSDK_NGX_Result result, string functionName)
{
    if (!NVSDK_NGX_SUCCEED(result))
    {
        System.Text.StringBuilder oss = new System.Text.StringBuilder();
        oss.Append("[DLSS] ").Append(functionName).Append(" failed with error code: ").Append(result);
        
        // Add more specific information based on error code
        switch (result)
        {
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_FeatureNotSupported:
            oss.Append(" - Feature not supported on current hardware");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_PlatformError:
            oss.Append(" - Platform error, check D3D12 debug layer for more info");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_FeatureAlreadyExists:
            oss.Append(" - Feature with given parameters already exists");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_FeatureNotFound:
            oss.Append(" - Feature with provided handle does not exist");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_InvalidParameter:
            oss.Append(" - Invalid parameter was provided");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_ScratchBufferTooSmall:
            oss.Append(" - Provided buffer is too small");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_NotInitialized:
            oss.Append(" - SDK was not initialized properly");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_UnsupportedInputFormat:
            oss.Append(" - Unsupported format used for input/output buffers");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_RWFlagMissing:
            oss.Append(" - Feature input/output needs RW access (UAV)");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_MissingInput:
            oss.Append(" - Feature was created with specific input but none is provided at evaluation");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_UnableToInitializeFeature:
            oss.Append(" - Feature is not available on the system");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_OutOfDate:
            oss.Append(" - NGX system libraries are old and need an update");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_OutOfGPUMemory:
            oss.Append(" - Feature requires more GPU memory than is available");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_UnsupportedFormat:
            oss.Append(" - Format used in input buffer(s) is not supported by feature");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_UnableToWriteToAppDataPath:
            oss.Append(" - Path provided in InApplicationDataPath cannot be written to");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_UnsupportedParameter:
            oss.Append(" - Unsupported parameter was provided");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_Denied:
            oss.Append(" - The feature or application was denied (contact NVIDIA for details)");
            break;
        case NVSDK_NGX_Result.NVSDK_NGX_Result_FAIL_NotImplemented:
            oss.Append(" - The feature or functionality is not implemented");
            break;
        default:
            oss.Append(" - Unknown error");
            break;
        }
        
        Debug.LogError(oss.ToString());
    }
}
}
}

