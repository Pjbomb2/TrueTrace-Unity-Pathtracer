#if UNITY_PIPELINE_URP
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class URPCompatability : ScriptableRendererFeature
{
    RenderTexture MainTex;
    TrueTrace.RayTracingMaster RayMaster;
    URPCompatabilityPass Pass;
    private void CreateRenderTexture(ref RenderTexture ThisTex)
    {
        ThisTex = new RenderTexture(Camera.main.pixelWidth, Camera.main.pixelHeight, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        ThisTex.enableRandomWrite = true;
        ThisTex.Create();
    }
    public override void Create() {
        CreateRenderTexture(ref MainTex);
        RayMaster = GameObject.Find("Scene").GetComponent<TrueTrace.RayTracingMaster>();
        RayMaster.TossCamera(Camera.main.gameObject.GetComponent<Camera>());
        RayMaster.Start2();
        RayMaster.Assets.Start();
        Pass = new URPCompatabilityPass(RayMaster, MainTex);
    }



    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if(Application.isPlaying) {
            renderingData.cameraData.camera.depthTextureMode |= (DepthTextureMode.MotionVectors | DepthTextureMode.Depth);
            Pass.ConfigureInput(ScriptableRenderPassInput.Color);
            Pass.SetTarget(renderer);
            Pass.ConfigureInput(ScriptableRenderPassInput.Motion);
            renderer.EnqueuePass(Pass);
          }
    }

    class URPCompatabilityPass : ScriptableRenderPass
    {
        TrueTrace.RayTracingMaster RayMaster;
        RenderTexture MainTex;
        RenderTargetIdentifier m_CameraColorTarget;
        ScriptableRenderer renderer;

    public void SetTarget(ScriptableRenderer Renderer)
    {
        renderer = Renderer;
    }
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // renderingData.cameraData.camera.depthTextureMode = DepthTextureMode.MotionVectors | DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
            m_CameraColorTarget = renderer.cameraColorTarget;
            ConfigureTarget(m_CameraColorTarget);
            var motionVectors = Shader.GetGlobalTexture("_MotionVectorTexture");
            Shader.SetGlobalTexture("_CameraMotionVectorsTexture", motionVectors);
        }
        public URPCompatabilityPass(TrueTrace.RayTracingMaster Master, RenderTexture maintex)
        {
            MainTex = maintex;
            RayMaster = Master;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(name: "LensFlarePass");
            Shader.SetGlobalTexture("_CameraGBufferTexture2", Shader.GetGlobalTexture("_GBuffer2"));
            Shader.SetGlobalTexture("_CameraGBufferTexture0", Shader.GetGlobalTexture("_GBuffer0"));
            Shader.SetGlobalTexture("_CameraGBufferTexture1", Shader.GetGlobalTexture("_GBuffer1"));
            RayMaster.RenderImage(MainTex, cmd);
            cmd.Blit(MainTex, m_CameraColorTarget);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }   



}
#endif