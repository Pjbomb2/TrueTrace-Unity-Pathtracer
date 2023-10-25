using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class RenderHandle : MonoBehaviour
{
    TrueTrace.RayTracingMaster RayMaster;
    Material OverlayMaterial;
    RenderTexture GITex;
    RenderTexture ParticleTex;
    private void CreateRenderTexture(ref RenderTexture ThisTex)
    {
        ThisTex?.Release();
        ThisTex = new RenderTexture(gameObject.GetComponent<Camera>().pixelWidth, gameObject.GetComponent<Camera>().pixelHeight, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        ThisTex.enableRandomWrite = true;
        ThisTex.Create();
    }
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
        if(gameObject.GetComponent<Camera>() != Camera.current) return;
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
