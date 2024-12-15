#if UNITY_PIPELINE_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Universal
{
    [ExecuteInEditMode]
    public class InjectPathTracingPass : MonoBehaviour
    {
        public URPTTPass m_PathTracingPass = null;
        TrueTrace.RayTracingMaster RayMaster;
        private void OnEnable() {
            RenderPipelineManager.beginCameraRendering += InjectPass;
        }

        private void OnDisable() {
            RenderPipelineManager.beginCameraRendering -= InjectPass;
        }

        private void CreateRenderPass() {
            m_PathTracingPass = new URPTTPass(RenderPassEvent.BeforeRenderingPostProcessing);
        }
        private void InjectPass(ScriptableRenderContext renderContext, Camera currCamera) {
            if (m_PathTracingPass == null) CreateRenderPass();
            if(RayMaster == null) RayMaster = GameObject.Find("Scene").GetComponent<TrueTrace.RayTracingMaster>();
             
            if (Application.isPlaying || RayMaster.HDRPorURPRenderInScene) {
                currCamera.depthTextureMode |= (DepthTextureMode.MotionVectors | DepthTextureMode.Depth);
                var data = currCamera.GetUniversalAdditionalCameraData();
                m_PathTracingPass.SetTarget(data.scriptableRenderer);
                data.scriptableRenderer.EnqueuePass(m_PathTracingPass);
                currCamera.forceIntoRenderTexture = true;
            }
        }
    }
}
#endif