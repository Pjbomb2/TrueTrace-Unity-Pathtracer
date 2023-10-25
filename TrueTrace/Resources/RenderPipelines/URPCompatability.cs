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
    bool SceneIsSetup = false;
    private void CreateRenderTexture(ref RenderTexture ThisTex, Camera cam)
    {
        ThisTex = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        ThisTex.enableRandomWrite = true;
        ThisTex.Create();
    }
    public override void Create() {
        if(GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>().Length == 0) {RayMaster = null; return;}
        RayMaster = GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>()[0];
        RayMaster.Start2();
        RayMaster.Assets.Start();
        Pass = new URPCompatabilityPass(RayMaster, MainTex);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if(Application.isPlaying) {
            if(RayMaster == null) {
                if(GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>().Length == 0) {RayMaster = null; return;}
                RayMaster = GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>()[0];
            }
            RayMaster.TossCamera(renderingData.cameraData.camera);
            if(MainTex == null) CreateRenderTexture(ref MainTex, renderingData.cameraData.camera);
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
        #if UNITY_2021
            RenderTargetIdentifier m_CameraColorTarget;
        #else
            RTHandle m_CameraColorTarget;
        #endif
        ScriptableRenderer renderer;

    public void SetTarget(ScriptableRenderer Renderer)
    {
        renderer = Renderer;
    }
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // renderingData.cameraData.camera.depthTextureMode = DepthTextureMode.MotionVectors | DepthTextureMode.Depth | DepthTextureMode.DepthNormals;
            #if UNITY_2021
                m_CameraColorTarget = renderer.cameraColorTarget;
            #else
                m_CameraColorTarget = renderer.cameraColorTargetHandle;
            #endif

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