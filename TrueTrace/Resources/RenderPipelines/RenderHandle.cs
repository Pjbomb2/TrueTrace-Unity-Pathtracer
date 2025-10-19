using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace TrueTrace {
    public class RenderHandle : MonoBehaviour
    {
        RayTracingMaster RayMaster;
    #if RasterizedDirect
        Material acc2mat;
    #endif
        void Start()
        {
            if(GameObject.FindObjectsOfType<RayTracingMaster>().Length == 0) {RayMaster = null; return;}
            RayMaster = GameObject.FindObjectsOfType<RayTracingMaster>()[0];
            // gameObject.GetComponent<Camera>().renderingPath = RenderingPath.DeferredShading;
            RayMaster.TossCamera(gameObject.GetComponent<Camera>());
            RayMaster.Start2();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RayMaster = GameObject.FindObjectsOfType<RayTracingMaster>()[0];
            RayMaster.TossCamera(gameObject.GetComponent<Camera>());
            RayMaster.Start2();
        }
    #if RasterizedDirect
        [ImageEffectOpaque]
    #endif
        private void OnRenderImage(RenderTexture source, RenderTexture destination) {
            if(gameObject.GetComponent<Camera>() != Camera.current || (RayMaster == null && GameObject.FindObjectsOfType<RayTracingMaster>().Length == 0)) {Graphics.Blit(source, destination); return;}
            if(RayMaster == null) RayMaster = GameObject.FindObjectsOfType<RayTracingMaster>()[0];
            RayMaster.TossCamera(gameObject.GetComponent<Camera>());
            if(RayMaster == null) {
                Start();
            }
            if(RayMaster == null) return;
            RayTracingMaster._camera.depthTextureMode = DepthTextureMode.None;
            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "TrueTrace";
            RayMaster.RenderImage(destination, cmd);
    #if RasterizedDirect
           if (acc2mat == null)
                acc2mat = new Material(Shader.Find("Hidden/Acc2"));
                cmd.Blit(source, destination, acc2mat, 0);
    #endif
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            cmd.Release();
        }
    }
}