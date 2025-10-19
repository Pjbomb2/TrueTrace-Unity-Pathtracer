#if UNITY_PIPELINE_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TrueTrace
{
    public class InjectPathTracingPass : ScriptableRendererFeature
    {
        [SerializeField] private RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        private URPTTPass m_PathTracingPass;
        private RayTracingMaster RayMaster;

        public override void Create()
        {
            RayMaster = GameObject.Find("Scene")?.GetComponent<RayTracingMaster>();
            m_PathTracingPass = new URPTTPass(renderPassEvent);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_PathTracingPass == null) {
                return;
            }

            if (RayMaster == null) {
                RayMaster = GameObject.Find("Scene")?.GetComponent<RayTracingMaster>();
                if (RayMaster == null) {
                    return;
                }
            }

            if (Application.isPlaying || RayMaster.HDRPorURPRenderInScene) {
                var camera = renderingData.cameraData.camera;
                camera.forceIntoRenderTexture = true;
                camera.depthTextureMode |= DepthTextureMode.Depth;

                renderer.EnqueuePass(m_PathTracingPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (m_PathTracingPass != null) {
                m_PathTracingPass.Cleanup();
                m_PathTracingPass = null;
            }
        }
    }
}
#endif