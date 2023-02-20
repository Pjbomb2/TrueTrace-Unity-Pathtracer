using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace TrueTrace {
    public class VideoObject : MonoBehaviour
    {
        [HideInInspector] public RenderTexture VideoTexture;
        private VideoPlayer VideoPlayerObject;
        private void CreateRenderTexture(ref RenderTexture ThisTex)
        {
            ThisTex = new RenderTexture((int)1920, (int)1080, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }

        void Awake()
        {
            VideoPlayerObject = gameObject.GetComponent<VideoPlayer>();
            CreateRenderTexture(ref VideoTexture);
            VideoPlayerObject.targetTexture = VideoTexture;

        }

        void OnApplicationQuit()
        {
            if (VideoTexture != null) VideoTexture.Release();
        }
    }
}