using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class RenderHandle : MonoBehaviour
{
    TrueTrace.RayTracingMaster RayMaster;

    void Start()
    {
        if(GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>().Length == 0) {RayMaster = null; return;}
        RayMaster = GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>()[0];
        gameObject.GetComponent<Camera>().renderingPath = RenderingPath.DeferredShading;
        gameObject.GetComponent<Camera>().depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;
        RayMaster.TossCamera(gameObject.GetComponent<Camera>());
        RayMaster.Start2();
    
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RayMaster = GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>()[0];
        RayMaster.TossCamera(gameObject.GetComponent<Camera>());
        RayMaster.Start2();
    }
    // [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if(gameObject.GetComponent<Camera>() != Camera.current || (RayMaster == null && GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>().Length == 0)) {Graphics.Blit(source, destination); return;}
        if(RayMaster == null) RayMaster = GameObject.FindObjectsOfType<TrueTrace.RayTracingMaster>()[0];
        RayMaster.TossCamera(gameObject.GetComponent<Camera>());
        if(RayMaster == null) {
            Start();
        }
        if(RayMaster == null) return;
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "TrueTrace";
        RayMaster.RenderImage(destination, cmd);
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        cmd.Release();
    }
}
