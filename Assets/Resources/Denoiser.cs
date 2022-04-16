using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Denoiser {
    private ComputeShader SVGF;

    private RenderTexture _ColorDirectIn;
    private RenderTexture _ColorIndirectIn;
    private RenderTexture _ColorDirectOut;
    private RenderTexture _ColorIndirectOut;
    private RenderTexture _PrevPosTex;
    private RenderTexture _ScreenPosPrev;
    private RenderTexture _HistoryDirect;
    private RenderTexture _HistoryIndirect;
    private RenderTexture _HistoryMoment;
    private RenderTexture _HistoryNormalDepth;
    private RenderTexture _NormalDepth;
    private RenderTexture _FrameMoment;
    private RenderTexture _History;
    private RenderTexture _TAAPrev;

    private int ScreenWidth;
    private int ScreenHeight;

    private Camera _camera;
    private Matrix4x4 PrevViewProjection;

    private int threadGroupsX;
    private int threadGroupsY;

    private int threadGroupsX2;
    private int threadGroupsY2;

    private int VarianceKernel;
    private int CopyKernel;
    private int ReprojectKernel;
    private int FinalizeKernel;
    private int SVGFAtrousKernel;
    private int TAAKernel;
    private int TAAFinalizeKernel;

    private void CreateRenderTexture(ref RenderTexture ThisTex, bool SRGB) {
        if(SRGB) {
        ThisTex = new RenderTexture(Screen.width, Screen.height, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        } else {
        ThisTex = new RenderTexture(Screen.width, Screen.height, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        }
        ThisTex.enableRandomWrite = true;
        ThisTex.Create();
    }


    private void InitRenderTexture() {
        if (_ColorDirectIn == null || _ColorDirectIn.width != Screen.width || _ColorDirectIn.height != Screen.height) {
            // Release render texture if we already have one
            if (_ColorDirectIn != null) {
                _ColorDirectIn.Release();
                _ColorIndirectIn.Release();
                _ColorDirectOut.Release();
                _ColorIndirectOut.Release();
                _ScreenPosPrev.Release();
                _PrevPosTex.Release();
                _HistoryDirect.Release();
                _HistoryIndirect.Release();
                _HistoryMoment.Release();
                _HistoryNormalDepth.Release();
                _NormalDepth.Release();
                _FrameMoment.Release();
                _History.Release();
                _TAAPrev.Release();
            }

         CreateRenderTexture(ref _ColorDirectIn, false);
         CreateRenderTexture(ref _ColorIndirectIn, false);
         CreateRenderTexture(ref _ColorDirectOut, false);
         CreateRenderTexture(ref _ColorIndirectOut, false);
         CreateRenderTexture(ref _ScreenPosPrev, false);
         CreateRenderTexture(ref _PrevPosTex, false);
         CreateRenderTexture(ref _HistoryDirect, false);
         CreateRenderTexture(ref _HistoryIndirect, false);
         CreateRenderTexture(ref _HistoryMoment, false);
         CreateRenderTexture(ref _HistoryNormalDepth, false);
         CreateRenderTexture(ref _NormalDepth, false);
         CreateRenderTexture(ref _FrameMoment, false);
         CreateRenderTexture(ref _History, false);
         CreateRenderTexture(ref _TAAPrev, false);
        }
    }

    public Denoiser(Camera Cam) {
        _camera = Cam;
        if(SVGF == null) {SVGF = Resources.Load<ComputeShader>("SVGF");}

        VarianceKernel = SVGF.FindKernel("kernel_variance");
        CopyKernel = SVGF.FindKernel("kernel_copy");
        ReprojectKernel = SVGF.FindKernel("kernel_reproject");
        FinalizeKernel = SVGF.FindKernel("kernel_finalize");
        SVGFAtrousKernel = SVGF.FindKernel("kernel_atrous");
        TAAKernel = SVGF.FindKernel("kernel_taa");
        TAAFinalizeKernel = SVGF.FindKernel("kernel_taa_finalize");

        SVGF.SetInt("screen_width", Screen.width);
        SVGF.SetInt("screen_height", Screen.height);

        threadGroupsX = Mathf.CeilToInt(Screen.width / 16.0f);
        threadGroupsY = Mathf.CeilToInt(Screen.height / 16.0f);

        threadGroupsX2 = Mathf.CeilToInt(Screen.width / 8.0f);
        threadGroupsY2 = Mathf.CeilToInt(Screen.height / 8.0f);

        InitRenderTexture();
    }

    public void ExecuteSVGF(int CurrentSamples, int AtrousKernelSize, ref ComputeBuffer _ColorBuffer, ref RenderTexture _PosTex, ref RenderTexture _target, ref RenderTexture _Albedo) {
        Matrix4x4 viewprojmatrix = _camera.projectionMatrix * _camera.worldToCameraMatrix;
        var PrevMatrix = PrevViewProjection;
        SVGF.SetMatrix("viewprojection", viewprojmatrix);
        SVGF.SetMatrix("prevviewprojection", PrevMatrix);
        SVGF.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        SVGF.SetInt("Samples_Accumulated", CurrentSamples);
        PrevViewProjection = viewprojmatrix;

        SVGF.SetInt("AtrousIterations", AtrousKernelSize);

        SVGF.SetBuffer(CopyKernel, "PerPixelRadiance", _ColorBuffer);
        SVGF.SetTexture(CopyKernel, "PosTex", _PosTex);
        SVGF.SetTexture(CopyKernel, "RWHistoryNormalAndDepth", _HistoryNormalDepth);
        SVGF.SetTexture(CopyKernel, "RWNormalAndDepth", _NormalDepth);
        SVGF.SetTexture(CopyKernel, "PrevPosTex", _PrevPosTex);
        SVGF.SetTexture(CopyKernel, "RWScreenPosPrev", _ScreenPosPrev);
        SVGF.SetTexture(CopyKernel, "ColorDirectOut", _ColorDirectOut);
        SVGF.SetTexture(CopyKernel, "ColorIndirectOut", _ColorIndirectOut);
        SVGF.SetTextureFromGlobal(CopyKernel, "_CameraNormalDepthTex", "_CameraDepthNormalsTexture");
        SVGF.Dispatch(CopyKernel, threadGroupsX, threadGroupsY, 1);

        SVGF.SetTexture(ReprojectKernel, "NormalAndDepth", _NormalDepth);
        SVGF.SetTexture(ReprojectKernel, "HistoryNormalAndDepth", _HistoryNormalDepth);
        SVGF.SetTexture(ReprojectKernel, "HistoryDirectTex", _HistoryDirect);
        SVGF.SetTexture(ReprojectKernel, "HistoryIndirectTex", _HistoryIndirect);
        SVGF.SetTexture(ReprojectKernel, "HistoryMomentTex", _HistoryMoment);
        SVGF.SetTexture(ReprojectKernel, "HistoryTex", _History);
        SVGF.SetTexture(ReprojectKernel, "ColorDirectIn", _ColorDirectOut);
        SVGF.SetTexture(ReprojectKernel, "ColorIndirectIn", _ColorIndirectOut);
        SVGF.SetTexture(ReprojectKernel, "ScreenPosPrev", _ScreenPosPrev);
        SVGF.SetTexture(ReprojectKernel, "ColorDirectOut", _ColorDirectIn);
        SVGF.SetTexture(ReprojectKernel, "ColorIndirectOut", _ColorIndirectIn);
        SVGF.SetTexture(ReprojectKernel, "FrameBufferMoment", _FrameMoment);
        SVGF.Dispatch(ReprojectKernel, threadGroupsX, threadGroupsY, 1);

        SVGF.SetTexture(VarianceKernel, "ColorDirectOut", _ColorDirectOut);
        SVGF.SetTexture(VarianceKernel, "ColorIndirectOut", _ColorIndirectOut);
        SVGF.SetTexture(VarianceKernel, "ColorDirectIn", _ColorDirectIn);
        SVGF.SetTexture(VarianceKernel, "ColorIndirectIn", _ColorIndirectIn);
        SVGF.SetTexture(VarianceKernel, "NormalAndDepth", _NormalDepth);
        SVGF.SetTexture(VarianceKernel, "FrameBufferMoment", _FrameMoment);
        SVGF.SetTexture(VarianceKernel, "HistoryTex", _History);
        SVGF.Dispatch(VarianceKernel, threadGroupsX, threadGroupsY, 1);
        Graphics.CopyTexture(_ColorDirectOut, _ColorDirectIn);
        Graphics.CopyTexture(_ColorIndirectOut, _ColorIndirectIn);

        for (int i = 0; i < AtrousKernelSize; i++) {
            int step_size = 1 << i;
            SVGF.SetTexture(SVGFAtrousKernel, "ColorDirectOut", _ColorDirectOut);
            SVGF.SetTexture(SVGFAtrousKernel, "ColorIndirectOut", _ColorIndirectOut);
            SVGF.SetTexture(SVGFAtrousKernel, "ColorDirectIn", _ColorDirectIn);
            SVGF.SetTexture(SVGFAtrousKernel, "ColorIndirectIn", _ColorIndirectIn);
            SVGF.SetTexture(SVGFAtrousKernel, "NormalAndDepth", _NormalDepth);
            SVGF.SetTexture(SVGFAtrousKernel, "HistoryDirectTex", _HistoryDirect);
            SVGF.SetTexture(SVGFAtrousKernel, "HistoryIndirectTex", _HistoryIndirect);
            var step = step_size;
            SVGF.SetInt("step_size", step);
            SVGF.Dispatch(SVGFAtrousKernel, threadGroupsX, threadGroupsY, 1);
            Graphics.CopyTexture(_ColorDirectOut, _ColorDirectIn);
            Graphics.CopyTexture(_ColorIndirectOut, _ColorIndirectIn);
        }

        SVGF.SetTexture(FinalizeKernel, "ColorDirectIn", _ColorDirectIn);
        SVGF.SetTexture(FinalizeKernel, "ColorDirectOut", _ColorDirectOut);
        SVGF.SetTexture(FinalizeKernel, "NormalAndDepth", _NormalDepth);
        SVGF.SetTexture(FinalizeKernel, "ColorIndirectIn", _ColorIndirectIn);
        SVGF.SetTexture(FinalizeKernel, "HistoryDirectTex", _HistoryDirect);
        SVGF.SetTexture(FinalizeKernel, "HistoryIndirectTex", _HistoryIndirect);
        SVGF.SetTexture(FinalizeKernel, "HistoryMomentTex", _HistoryMoment);
        SVGF.SetTexture(FinalizeKernel, "RWHistoryNormalAndDepth", _HistoryNormalDepth);
        SVGF.SetTexture(FinalizeKernel, "Result", _target);
        SVGF.SetTexture(FinalizeKernel, "HistoryTex", _History);
        SVGF.SetTexture(FinalizeKernel, "_Albedo", _Albedo);
        SVGF.SetTexture(FinalizeKernel, "FrameBufferMoment", _FrameMoment);
        SVGF.Dispatch(FinalizeKernel, threadGroupsX, threadGroupsY, 1);

        SVGF.SetTexture(TAAKernel, "Result", _ColorDirectOut);
        SVGF.SetTexture(TAAKernel, "ScreenPosPrev", _ScreenPosPrev);
        SVGF.SetTexture(TAAKernel, "TAAPrev", _TAAPrev);
        SVGF.SetTexture(TAAKernel, "ColorDirectOut", _ColorDirectIn);
        SVGF.Dispatch(TAAKernel, threadGroupsX, threadGroupsY, 1);

        SVGF.SetTexture(TAAFinalizeKernel, "TAAPrev", _TAAPrev);
        SVGF.SetTexture(TAAFinalizeKernel, "Result", _target);
        SVGF.SetTexture(TAAFinalizeKernel, "ColorDirectIn", _ColorDirectIn);
        SVGF.Dispatch(TAAFinalizeKernel, threadGroupsX, threadGroupsY, 1);

        Graphics.CopyTexture(_PosTex, _PrevPosTex);

    }


}
