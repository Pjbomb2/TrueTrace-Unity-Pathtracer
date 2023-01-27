using System.Collections.Generic;
using UnityEngine;

namespace TrueTrace {
    [System.Serializable]
    public class SVGF2
    {
        public RenderTexture NormDepthTex;
        public RenderTexture PrevNormDepthTex;
        public RenderTexture PrevHistoryTex;
        public RenderTexture HistoryTex;
        public RenderTexture PrevMoments;
        public RenderTexture Moments;
        public RenderTexture InBetweenTex;
        public RenderTexture InBetweenTex2;
        public RenderTexture ReprojectTex;
        Material Modulate;
        Material Pack;
        Material Atrous;
        Material Reproject;
        Material Filter;
        int TargetWidth;
        int TargetHeight;
        int SourceWidth;
        int SourceHeight;
        int CurrentFrame;
        RenderBuffer[] mrt;

        private void CreateRenderTexture(ref RenderTexture ThisTex, bool SRGB, bool Res) {
            if(SRGB) {
            ThisTex = new RenderTexture((Res) ? TargetWidth : SourceWidth, (Res) ? TargetHeight : SourceHeight, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            } else {
            ThisTex = new RenderTexture((Res) ? TargetWidth : SourceWidth, (Res) ? TargetHeight : SourceHeight, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            }
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }
        private Camera _camera;
        // Start is called before the first frame update
        public bool Initialized;
        public void ClearSVGF() {
            NormDepthTex.Release();
            PrevNormDepthTex.Release();
            PrevHistoryTex.Release();
            PrevMoments.Release();
            InBetweenTex2.Release();
            InBetweenTex.Release();
            HistoryTex.Release();
            Moments.Release();
            ReprojectTex.Release();
            Initialized = false;
        }
        public void InitSVGF() {
            CreateRenderTexture(ref NormDepthTex, false, false);
            CreateRenderTexture(ref PrevNormDepthTex, false, false);
            CreateRenderTexture(ref PrevHistoryTex, false, false);
            CreateRenderTexture(ref PrevMoments, false, false);
            CreateRenderTexture(ref InBetweenTex, false, false);
            CreateRenderTexture(ref InBetweenTex2, false, false);
            CreateRenderTexture(ref HistoryTex, false, false);
            CreateRenderTexture(ref Moments, false, false);
            CreateRenderTexture(ref ReprojectTex, false, false);
            Initialized = true;
        }
        public void Start(int Width, int Height, Camera camera)
        {
            Initialized = false;
            _camera = camera;
            CurrentFrame = 0;
            TargetHeight = Height;
            TargetWidth = Width;
            SourceHeight = Height;
            SourceWidth = Width;
            Pack = new Material(Shader.Find("Hidden/SVGFPack"));
            Modulate = new Material(Shader.Find("Hidden/SVGFModulate"));
            Atrous = new Material(Shader.Find("Hidden/SVGFAtrous"));
            Reproject = new Material(Shader.Find("Hidden/SVGFReproject"));
            Filter = new Material(Shader.Find("Hidden/SVGFFilterMoments"));
            mrt = new RenderBuffer[3];
        }
        public void Denoise(ref RenderTexture Output, RenderTexture Input, RenderTexture AlbedoTex, int SVGFAtrousKernelSizes) {
            Pack.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
            Pack.SetVector("Forward", _camera.transform.forward);
            Graphics.Blit(null, (CurrentFrame%2 == 0) ? NormDepthTex : PrevNormDepthTex, Pack);
            
            Reproject.SetTexture("gColor", Input);
            Reproject.SetTexture("gAlbedo", AlbedoTex);
            Reproject.SetTexture("gPrevIllum", (SVGFAtrousKernelSizes%2 == 1) ? InBetweenTex : InBetweenTex2);
            Reproject.SetTexture("gPrevMoments", (CurrentFrame%2 == 0) ? PrevMoments : Moments);
            Reproject.SetTexture("gLinearZAndNormal", (CurrentFrame%2 == 0) ? NormDepthTex : PrevNormDepthTex);
            Reproject.SetTexture("gPrevLinearZAndNormal", (CurrentFrame%2 == 0) ? PrevNormDepthTex : NormDepthTex);
            Reproject.SetTexture("gPrevHistoryLength", (CurrentFrame%2 == 0) ? PrevHistoryTex : HistoryTex);
            Reproject.SetFloat("gAlpha", 0.05f);
            Reproject.SetFloat("gMomentsAlpha", 0.2f);
            mrt[0] = ReprojectTex.colorBuffer;
            mrt[1] = ((CurrentFrame%2 == 0) ? Moments : PrevMoments).colorBuffer;
            mrt[2] = ((CurrentFrame%2 == 0) ? HistoryTex : PrevHistoryTex).colorBuffer;
            Graphics.SetRenderTarget(mrt, ReprojectTex.depthBuffer);
            Graphics.Blit(null, Reproject, 0);

            Filter.SetTexture("gIllumination", ReprojectTex);
            Filter.SetTexture("gMoments", (CurrentFrame%2 == 0) ? Moments : PrevMoments);
            Filter.SetTexture("gHistoryLength", (CurrentFrame%2 == 0) ? HistoryTex : PrevHistoryTex);
            Filter.SetTexture("gLinearZAndNormal", (CurrentFrame%2 == 0) ? NormDepthTex : PrevNormDepthTex);
            Filter.SetFloat("gPhiColor", 10.0f);
            Filter.SetFloat("gPhiNormal", 128.0f);
            Graphics.Blit(null, InBetweenTex2, Filter);

            Atrous.SetTexture("gLinearZAndNormal", (CurrentFrame%2 == 0) ? NormDepthTex : PrevNormDepthTex);
            Atrous.SetTexture("gHistoryLength", (CurrentFrame%2 == 0) ? HistoryTex : PrevHistoryTex);
            Atrous.SetTexture("gAlbedo", AlbedoTex);
            Atrous.SetFloat("gPhiColor", 10.0f);
            Atrous.SetFloat("gPhiNormal", 128.0f);
            for(int i = 0; i < SVGFAtrousKernelSizes; i++) {
                int Temp = 1 << i;
                Atrous.SetTexture("gIllumination", (i%2 == 0) ? InBetweenTex2 : InBetweenTex);
                Atrous.SetInt("gStepSize", Temp);
                Graphics.Blit(null, (i%2 == 0) ? InBetweenTex : InBetweenTex2, Atrous);
            }
            Modulate.SetTexture("gAlbedo", AlbedoTex);
            Modulate.SetTexture("gIllumination", (SVGFAtrousKernelSizes%2 == 1) ? InBetweenTex : InBetweenTex2);
            Graphics.Blit(null, Output, Modulate);
            CurrentFrame++;
        }
    }
}