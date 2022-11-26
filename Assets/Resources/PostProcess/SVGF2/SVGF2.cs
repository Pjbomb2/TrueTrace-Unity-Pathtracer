using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class SVGF2
{
    public RenderTexture NormDepthTex;
    public RenderTexture PrevNormDepthTex;
    public RenderTexture PrevHistoryTex;
    public RenderTexture HistoryTex;
    public RenderTexture PrevMoments;
    public RenderTexture Moments;
    public RenderTexture PrevIllum;
    public RenderTexture InBetweenTex;
    public RenderTexture InBetweenTex2;
    Material Modulate;
    Material Pack;
    Material Atrous;
    Material Reproject;
    Material Filter;
    int TargetWidth;
    int TargetHeight;
    int SourceWidth;
    int SourceHeight;

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
    // Start is called before the first frame update
    public void Start(int Width, int Height)
    {
        TargetHeight = Height;
        TargetWidth = Width;
        SourceHeight = Height;
        SourceWidth = Width;
        CreateRenderTexture(ref NormDepthTex, false, false);
        CreateRenderTexture(ref PrevNormDepthTex, false, false);
        CreateRenderTexture(ref PrevHistoryTex, false, false);
        CreateRenderTexture(ref PrevMoments, false, false);
        CreateRenderTexture(ref PrevIllum, false, false);
        CreateRenderTexture(ref InBetweenTex, false, false);
        CreateRenderTexture(ref InBetweenTex2, false, false);
        CreateRenderTexture(ref HistoryTex, false, false);
        CreateRenderTexture(ref Moments, false, false);
        Pack = new Material(Shader.Find("Hidden/SVGFPack"));
        Modulate = new Material(Shader.Find("Hidden/SVGFModulate"));
        Atrous = new Material(Shader.Find("Hidden/SVGFAtrous"));
        Reproject = new Material(Shader.Find("Hidden/SVGFReproject"));
        Filter = new Material(Shader.Find("Hidden/SVGFFilterMoments"));
        
    }
    public void Denoise(ref RenderTexture Output, RenderTexture Input, RenderTexture AlbedoTex, int SVGFAtrousKernelSizes) {
        Graphics.Blit(null, NormDepthTex, Pack);
        Graphics.CopyTexture(NormDepthTex, Output);
        
        Reproject.SetTexture("gColor", Input);
        Reproject.SetTexture("gAlbedo", AlbedoTex);
        Reproject.SetTexture("gPrevIllum", PrevIllum);
        Reproject.SetTexture("gPrevMoments", PrevMoments);
        Reproject.SetTexture("gLinearZAndNormal", NormDepthTex);
        Reproject.SetTexture("gPrevLinearZAndNormal", PrevNormDepthTex);
        Reproject.SetTexture("gPrevHistoryLength", PrevHistoryTex);
        Reproject.SetFloat("gAlpha", 0.05f);
        Reproject.SetFloat("gMomentsAlpha", 0.2f);
        var mrt = new RenderBuffer[3];
        mrt[0] = InBetweenTex.colorBuffer;
        mrt[1] = Moments.colorBuffer;
        mrt[2] = HistoryTex.colorBuffer;
        Graphics.SetRenderTarget(mrt, InBetweenTex.depthBuffer);
        Graphics.Blit(null, Reproject, 0);

        Filter.SetTexture("gIllumination", InBetweenTex);
        Filter.SetTexture("gMoments", Moments);
        Filter.SetTexture("gHistoryLength", HistoryTex);
        Filter.SetTexture("gLinearZAndNormal", NormDepthTex);
        Filter.SetFloat("gPhiColor", 10.0f);
        Filter.SetFloat("gPhiNormal", 128.0f);
        Graphics.Blit(null, InBetweenTex2, Filter);
            
        for(int i = 0; i < SVGFAtrousKernelSizes; i++) {
            int Temp = 1 << i;

            Atrous.SetTexture("gLinearZAndNormal", NormDepthTex);
            Atrous.SetTexture("gHistoryLength", HistoryTex);
            Atrous.SetTexture("gAlbedo", AlbedoTex);
            Atrous.SetFloat("gPhiColor", 10.0f);
            Atrous.SetFloat("gPhiNormal", 128.0f);
            Atrous.SetTexture("gIllumination", (i%2 == 0) ? InBetweenTex2 : InBetweenTex);
            Atrous.SetInt("gStepSize", Temp);
            Graphics.Blit(null, (i%2 == 0) ? InBetweenTex : InBetweenTex2, Atrous);
        }

        Modulate.SetTexture("gAlbedo", AlbedoTex);
        Modulate.SetTexture("gIllumination", InBetweenTex);
        Graphics.Blit(null, Output, Modulate);

        Graphics.CopyTexture(InBetweenTex, PrevIllum);
        Graphics.CopyTexture(NormDepthTex, PrevNormDepthTex);
        Graphics.CopyTexture(HistoryTex, PrevHistoryTex);
        Graphics.CopyTexture(Moments, PrevMoments);




    }
}
