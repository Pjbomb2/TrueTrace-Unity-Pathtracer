#if UNITY_PIPELINE_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TrueTrace
{
    [ExecuteInEditMode]
    public class InjectPathTracingPass : MonoBehaviour
    {
        public URPTTPass m_PathTracingPass = null;
        RayTracingMaster RayMaster;
        private void OnEnable() {
            RenderPipelineManager.beginCameraRendering += InjectPass;
        }

        private void OnDisable() {
            RenderPipelineManager.beginCameraRendering -= InjectPass;
            CleanupPass();
        }
        private void OnDestroy() {
            CleanupPass();
        }
        private void CleanupPass() {
            if(m_PathTracingPass != null) {
                m_PathTracingPass.Cleanup();
                m_PathTracingPass = null;
            }
        }

        private void CreateRenderPass() {
            CleanupPass();
            m_PathTracingPass = new URPTTPass(RenderPassEvent.BeforeRenderingPostProcessing);
        }
        private void InjectPass(ScriptableRenderContext renderContext, Camera currCamera) {
            if (m_PathTracingPass == null) CreateRenderPass();
            if(RayMaster == null) RayMaster = GameObject.Find("Scene").GetComponent<RayTracingMaster>();
             
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