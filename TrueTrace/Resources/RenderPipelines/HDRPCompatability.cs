#if UNITY_PIPELINE_HDRP
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
namespace TrueTrace {
    public class HDRPCompatability : CustomPass
    {
        RenderTexture MainTex;
        RayTracingMaster RayMaster;
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
            if(GameObject.FindObjectsOfType<RayTracingMaster>().Length == 0) {RayMaster = null; return;}
            RayMaster = GameObject.FindObjectsOfType<RayTracingMaster>()[0];
            RayMaster.Start2();
            if(RayMaster.ShadingShader == null) {
                GameObject.Find("Scene").GetComponent<AssetManager>().Start();
            }
        }

        protected override void Execute(CustomPassContext ctx)
        {
            if(RayMaster == null) {
                if(GameObject.FindObjectsOfType<RayTracingMaster>().Length == 0) {RayMaster = null; return;}
                RayMaster = GameObject.FindObjectsOfType<RayTracingMaster>()[0];
            }
            if((Application.isPlaying && Camera.current == null) || RayMaster.HDRPorURPRenderInScene) {
                if(MainTex == null || MainTex.width != ctx.hdCamera.camera.pixelWidth) {
                    if(MainTex != null) MainTex?.Release();
                    CreateRenderTexture(ref MainTex, ctx.hdCamera.camera);
                }
                ctx.hdCamera.camera.renderingPath = RenderingPath.DeferredShading;
                RayMaster.TossCamera(ctx.hdCamera.camera);
                if(RayTracingMaster._camera.renderingPath == RenderingPath.DeferredShading) {
                    Shader.SetGlobalTexture("_CameraGBufferTexture2", Shader.GetGlobalTexture("_GBufferTexture2"));
                    Shader.SetGlobalTexture("_CameraGBufferTexture0", Shader.GetGlobalTexture("_GBufferTexture0"));
                    Shader.SetGlobalTexture("_CameraGBufferTexture1", Shader.GetGlobalTexture("_GBufferTexture1"));
                }
                ctx.hdCamera.camera.depthTextureMode = DepthTextureMode.None;
                ctx.cmd.BeginSample("TrueTrace");
                RayMaster.RenderImage(MainTex, ctx.cmd);
                ctx.propertyBlock.SetTexture("_MainTex", MainTex);

                ctx.cmd.Blit(MainTex, ctx.cameraColorBuffer, new Vector2((float)ctx.cameraColorBuffer.referenceSize.x / (float) ctx.hdCamera.camera.pixelWidth, (float)ctx.cameraColorBuffer.referenceSize.y / (float) ctx.hdCamera.camera.pixelHeight), Vector2.zero);

                ctx.cmd.EndSample("TrueTrace");
             }
        }

        protected override void Cleanup()
        {
            if(RayMaster != null) RayMaster.OnDisable();
            if(GameObject.Find("Scene") != null) GameObject.Find("Scene").GetComponent<AssetManager>().ClearAll();
            // Cleanup code
        }
    }
}
#endif