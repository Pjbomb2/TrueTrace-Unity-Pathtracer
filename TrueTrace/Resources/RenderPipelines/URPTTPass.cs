#if UNITY_PIPELINE_URP
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace TrueTrace {
    public class URPTTPass : ScriptableRenderPass {
        RayTracingMaster RayMaster;
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

            if(MainTex == null || MainTex.width != renderingData.cameraData.camera.pixelWidth || MainTex.height != renderingData.cameraData.camera.pixelHeight) {
                if(MainTex != null) {
                    MainTex.Release();
                    Object.DestroyImmediate(MainTex);
                }
                CreateRenderTexture(ref MainTex, renderingData.cameraData.camera);
            }
            ConfigureTarget(m_CameraColorTarget);

        }

        public URPTTPass(RenderPassEvent rpEvent) {
            RayMaster = GameObject.Find("Scene").GetComponent<RayTracingMaster>();
            if(Application.isPlaying) {
                RayMaster.Start2();
            }
            renderPassEvent = rpEvent;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "TrueTrace";
            
#if TTDisableCustomMotionVectors
            renderingData.cameraData.camera.depthTextureMode = DepthTextureMode.MotionVectors | DepthTextureMode.Depth;
            var motionVectors = Shader.GetGlobalTexture("_MotionVectorTexture");
            Shader.SetGlobalTexture("_CameraMotionVectorsTexture", motionVectors);
#else
            renderingData.cameraData.camera.depthTextureMode = DepthTextureMode.None;
#endif
            RayMaster.TossCamera(renderingData.cameraData.camera);
            if(RayTracingMaster.RayMaster.LocalTTSettings.RenderScale != 1.0f && RayTracingMaster.RayMaster.LocalTTSettings.UpscalerMethod != 0) {
                Shader.SetGlobalTexture("_CameraGBufferTexture2", Shader.GetGlobalTexture("_GBuffer2"));
                Shader.SetGlobalTexture("_CameraGBufferTexture0", Shader.GetGlobalTexture("_GBuffer0"));
                Shader.SetGlobalTexture("_CameraGBufferTexture1", Shader.GetGlobalTexture("_GBuffer1"));
            }
            RayMaster.RenderImage(MainTex, cmd);
            cmd.Blit(MainTex, m_CameraColorTarget);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            cmd.Release();
        }

        public void Cleanup() {
            if(MainTex != null) {
                MainTex.Release();
                Object.DestroyImmediate(MainTex);
                MainTex = null;
            }
        }
    }   
}




#endif