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
        // if(OverlayMaterial == null) OverlayMaterial = new Material(Shader.Find("Hidden/OverlayShader"));
        // CreateRenderTexture(ref GITex);
        // CreateRenderTexture(ref ParticleTex);
        RayMaster = GameObject.Find("Scene").GetComponent<TrueTrace.RayTracingMaster>();
        RayMaster.TossCamera(gameObject.GetComponent<Camera>());
        RayMaster.Start2();
    
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RayMaster = GameObject.Find("Scene").GetComponent<TrueTrace.RayTracingMaster>();
        RayMaster.TossCamera(gameObject.GetComponent<Camera>());
        RayMaster.Start2();
    }
    // [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if(RayMaster == null) {
            Start();
        }
        // RayMaster.ParticleCamera.targetTexture = ParticleTex;
        // RayMaster.ParticleCamera.Render();

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "TrueTrace";
        RayMaster.RenderImage(destination, cmd);
        // cmd.Blit(destination, GITex);
        // OverlayMaterial.SetTexture("GITexture", GITex);
        // cmd.Blit(source, destination, OverlayMaterial);
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        cmd.Release();
    }
}
