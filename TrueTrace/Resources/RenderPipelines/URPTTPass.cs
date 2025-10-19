#if UNITY_PIPELINE_URP
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Experimental.Rendering;

namespace TrueTrace {
    public class URPTTPass : ScriptableRenderPass {
        RayTracingMaster RayMaster;
        RenderTexture MainTex;

        private class PassData {
            public TextureHandle mainTexHandle;
            public TextureHandle cameraColorHandle;
        }

        private void CreateRenderTexture(ref RenderTexture ThisTex, Camera cam) {
            ThisTex = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }

        public URPTTPass(RenderPassEvent rpEvent) {
            if(RayMaster == null && GameObject.Find("Scene") != null) {
                RayMaster = GameObject.Find("Scene").GetComponent<RayTracingMaster>();
            // if(Application.isPlaying) {
                RayMaster.Start2();
            // }
            }
            renderPassEvent = rpEvent;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            RayMaster = GameObject.Find("Scene")?.GetComponent<RayTracingMaster>();
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var camera = cameraData.camera;

            if(camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView) return;
            
            if (resourceData.isActiveTargetBackBuffer) {
                return;
            }

            TextureHandle cameraColorHandle = resourceData.activeColorTexture;

            bool needsResize = MainTex == null || 
                               MainTex.width != camera.pixelWidth || 
                               MainTex.height != camera.pixelHeight;
            if (needsResize) {
                if (MainTex != null) {
                    MainTex.Release();
                }
                CreateRenderTexture(ref MainTex, camera);
            }

            TextureHandle mainTexHandle = renderGraph.ImportTexture(RTHandles.Alloc(MainTex));

            // if(RayTracingMaster.RayMaster.LocalTTSettings.RenderScale != 1.0f && RayTracingMaster.RayMaster.LocalTTSettings.UpscalerMethod != 0) {
            //     Shader.SetGlobalTexture("_CameraGBufferTexture2", Shader.GetGlobalTexture("_GBuffer2"));
            //     Shader.SetGlobalTexture("_CameraGBufferTexture0", Shader.GetGlobalTexture("_GBuffer0"));
            //     Shader.SetGlobalTexture("_CameraGBufferTexture1", Shader.GetGlobalTexture("_GBuffer1"));
            // }

            using (var builder = renderGraph.AddUnsafePass<PassData>("TrueTrace", out var passData)) {
                passData.mainTexHandle = mainTexHandle;
                passData.cameraColorHandle = cameraColorHandle;

                builder.UseTexture(passData.mainTexHandle, AccessFlags.ReadWrite);
                builder.UseTexture(passData.cameraColorHandle, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) => {
                    CommandBuffer nativeCmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

                    RayMaster.TossCamera(camera);

                    RayMaster.RenderImage(MainTex, nativeCmd);

                    ctx.cmd.SetRenderTarget(data.cameraColorHandle);
                    Blitter.BlitTexture(nativeCmd, data.mainTexHandle, new Vector4(1f, 1f, 0f, 0f), 0, false);
                });
            }
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