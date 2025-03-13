#if UNITY_PIPELINE_URP
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

    public class URPTTPass : ScriptableRenderPass {
        TrueTrace.RayTracingMaster RayMaster;
        RenderTexture MainTex;
        #if UNITY_2021
            RenderTargetIdentifier m_CameraColorTarget;
        #else
            RTHandle m_CameraColorTarget;
        #endif
        ScriptableRenderer renderer;

        public void SetTarget(ScriptableRenderer Renderer) {
            renderer = Renderer;
        }
        private void CreateRenderTexture(ref RenderTexture ThisTex, Camera cam) {
            ThisTex = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
            renderingData.cameraData.camera.depthTextureMode = DepthTextureMode.Depth;
            #if UNITY_2021
                m_CameraColorTarget = renderer.cameraColorTarget;
            #else
                m_CameraColorTarget = renderer.cameraColorTargetHandle;
            #endif

            if(MainTex == null) CreateRenderTexture(ref MainTex, renderingData.cameraData.camera);

            ConfigureTarget(m_CameraColorTarget);

        }

        public URPTTPass(RenderPassEvent rpEvent) {
            RayMaster = GameObject.Find("Scene").GetComponent<TrueTrace.RayTracingMaster>();
            RayMaster.Start2();
            renderPassEvent = rpEvent;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "TrueTrace";
            
#if !TTCustomMotionVectors
            renderingData.cameraData.camera.depthTextureMode = DepthTextureMode.MotionVectors | DepthTextureMode.Depth;
            var motionVectors = Shader.GetGlobalTexture("_MotionVectorTexture");
            Shader.SetGlobalTexture("_CameraMotionVectorsTexture", motionVectors);
#else
            renderingData.cameraData.camera.depthTextureMode = DepthTextureMode.Depth;
#endif
            if(TrueTrace.RayTracingMaster._camera.renderingPath == RenderingPath.DeferredShading) {
                Shader.SetGlobalTexture("_CameraGBufferTexture2", Shader.GetGlobalTexture("_GBuffer2"));
                Shader.SetGlobalTexture("_CameraGBufferTexture0", Shader.GetGlobalTexture("_GBuffer0"));
                Shader.SetGlobalTexture("_CameraGBufferTexture1", Shader.GetGlobalTexture("_GBuffer1"));
            }
            RayMaster.TossCamera(renderingData.cameraData.camera);
            RayMaster.RenderImage(MainTex, cmd);
            cmd.Blit(MainTex, m_CameraColorTarget);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            cmd.Release();
        }
    }   




#endif