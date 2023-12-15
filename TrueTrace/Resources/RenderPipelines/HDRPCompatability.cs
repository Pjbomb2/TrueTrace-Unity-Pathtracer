#if UNITY_PIPELINE_HDRP
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

public class HDRPCompatability : CustomPass
{
    RenderTexture MainTex;
    TrueTrace.RayTracingMaster RayMaster;
    Material MainMat;
    private void CreateRenderTexture(ref RenderTexture ThisTex, Camera cam)
    {
        ThisTex = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        ThisTex.enableRandomWrite = true;
        ThisTex.Create();
    }

    // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
    // When empty this render pass will render to the active camera render target.
    // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
    // The render pipeline will ensure target setup and clearing happens in an performance manner.
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        if(GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>().Length == 0) {RayMaster = null; return;}
        RayMaster = GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>()[0];
        Shader.SetGlobalTexture("_CameraGBufferTexture2", Shader.GetGlobalTexture("_GBufferTexture2"));
        RayMaster.Start2();
        if(RayMaster.ShadingShader == null) {
            GameObject.Find("Scene").GetComponent<TrueTrace.AssetManager>().Start();
        }
    }

    protected override void Execute(CustomPassContext ctx)
    {
        if(Application.isPlaying) {
            if(RayMaster == null) {
                if(GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>().Length == 0) {RayMaster = null; return;}
                RayMaster = GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>()[0];
            }
            if(MainTex == null) CreateRenderTexture(ref MainTex, ctx.hdCamera.camera);
            ctx.hdCamera.camera.renderingPath = RenderingPath.DeferredShading;
            ctx.hdCamera.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;
            RayMaster.TossCamera(ctx.hdCamera.camera);
            Shader.SetGlobalTexture("_CameraGBufferTexture2", Shader.GetGlobalTexture("_GBufferTexture2"));
            Shader.SetGlobalTexture("_CameraGBufferTexture0", Shader.GetGlobalTexture("_GBufferTexture0"));
            Shader.SetGlobalTexture("_CameraGBufferTexture1", Shader.GetGlobalTexture("_GBufferTexture1"));
            RayMaster.RenderImage(MainTex, ctx.cmd);
            ctx.propertyBlock.SetTexture("_MainTex", MainTex);
            ctx.cmd.Blit(MainTex, ctx.cameraColorBuffer);
         }
    }

    protected override void Cleanup()
    {
        RayMaster.OnDisable();
        GameObject.Find("Scene").GetComponent<TrueTrace.AssetManager>().ClearAll();
        // Cleanup code
    }
}
#endif