using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrueTrace {
    public class TTRenderInterupt : MonoBehaviour
    {
        RayTracingMaster RayMaster;
        public void Initialize(Camera Cam) {
            if(GameObject.FindObjectsOfType<RayTracingMaster>().Length == 0) {RayMaster = null; return;}
            RayMaster = GameObject.FindObjectsOfType<RayTracingMaster>()[0];
            Cam.renderingPath = RenderingPath.DeferredShading;
            Cam.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
            RayMaster.TossCamera(Cam);
            RayMaster.Start2();

        }

        public void RenderToRenderTexture(RenderTexture RefTex, Camera TargCam) {
            if(RayMaster == null) RayMaster = GameObject.FindObjectsOfType<RayTracingMaster>()[0];
            RayMaster.TossCamera(TargCam);
            RayMaster.OverrideResolution(RefTex.width, RefTex.height);
            if(RayMaster == null) return;
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "TrueTrace";
            RayMaster.RenderImage(RefTex, cmd);
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            cmd.Release();
        }
    }
}