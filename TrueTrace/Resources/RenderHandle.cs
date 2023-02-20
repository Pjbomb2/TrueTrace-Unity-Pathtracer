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

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if(RayMaster == null) {
            Start();
        }
        CommandBuffer cmd = new CommandBuffer();
        RayMaster.RenderImage(destination, cmd);
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        cmd.Release();
    }
}
