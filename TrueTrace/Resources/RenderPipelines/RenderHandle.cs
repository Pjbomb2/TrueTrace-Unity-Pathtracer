using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class RenderHandle : MonoBehaviour
{
    TrueTrace.RayTracingMaster RayMaster;
#if RasterizedDirect
    Material acc2mat;
#endif
    void Start()
    {
        if(GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>().Length == 0) {RayMaster = null; return;}
        RayMaster = GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>()[0];
        // gameObject.GetComponent<Camera>().renderingPath = RenderingPath.DeferredShading;
        RayMaster.TossCamera(gameObject.GetComponent<Camera>());
        RayMaster.Start2();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RayMaster = GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>()[0];
        RayMaster.TossCamera(gameObject.GetComponent<Camera>());
        RayMaster.Start2();
    }
#if RasterizedDirect
    [ImageEffectOpaque]
#endif
    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if(gameObject.GetComponent<Camera>() != Camera.current || (RayMaster == null && GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>().Length == 0)) {Graphics.Blit(source, destination); return;}
        if(RayMaster == null) RayMaster = GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>()[0];
        RayMaster.TossCamera(gameObject.GetComponent<Camera>());
        if(RayMaster == null) {
            Start();
        }
        if(RayMaster == null) return;
#if !TTCustomMotionVectors
        TrueTrace.RayTracingMaster._camera.depthTextureMode |= DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
#else
        TrueTrace.RayTracingMaster._camera.depthTextureMode = DepthTextureMode.None;
#endif
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
