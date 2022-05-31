using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Denoiser {
    private ComputeShader SVGF;
    private ComputeShader AtrousDenoiser;
    private ComputeShader Bloom;

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
    private RenderTexture Intermediate;
    private RenderTexture TempTex;
    private RenderTexture SuperIntermediate;

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
    private int AtrousKernel;
    private int AtrousCopyKernel;
    private int AtrousFinalizeKernel;
    
    private int BloomDownsampleKernel;
    private int BloomLowPassKernel;
    private int BloomUpsampleKernel;

    private int ComputeHistogramKernel;
    private int CalcAverageKernel;
    private int ToneMapKernel;

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
        if(SVGF == null) {SVGF = Resources.Load<ComputeShader>("Denoiser/SVGF");}
        if(AtrousDenoiser == null) {AtrousDenoiser = Resources.Load<ComputeShader>("Denoiser/Atrous");}
        if(Bloom == null) {Bloom = Resources.Load<ComputeShader>("Utility/Bloom");}

        VarianceKernel = SVGF.FindKernel("kernel_variance");
        CopyKernel = SVGF.FindKernel("kernel_copy");
        ReprojectKernel = SVGF.FindKernel("kernel_reproject");
        FinalizeKernel = SVGF.FindKernel("kernel_finalize");
        SVGFAtrousKernel = SVGF.FindKernel("kernel_atrous");
        TAAKernel = SVGF.FindKernel("kernel_taa");
        TAAFinalizeKernel = SVGF.FindKernel("kernel_taa_finalize");
        AtrousKernel = AtrousDenoiser.FindKernel("Atrous");
        AtrousCopyKernel = AtrousDenoiser.FindKernel("kernel_copy");
        AtrousFinalizeKernel = AtrousDenoiser.FindKernel("kernel_finalize");

        BloomDownsampleKernel = Bloom.FindKernel("Downsample");
        BloomLowPassKernel = Bloom.FindKernel("LowPass");
        BloomUpsampleKernel = Bloom.FindKernel("Upsample");

        SVGF.SetInt("screen_width", Screen.width);
        SVGF.SetInt("screen_height", Screen.height);

        Bloom.SetInt("screen_width", Screen.width);
        Bloom.SetInt("screen_width", Screen.height);

        AtrousDenoiser.SetInt("screen_width", Screen.width);
        AtrousDenoiser.SetInt("screen_height", Screen.height);


        threadGroupsX = Mathf.CeilToInt(Screen.width / 16.0f);
        threadGroupsY = Mathf.CeilToInt(Screen.height / 16.0f);

        threadGroupsX2 = Mathf.CeilToInt(Screen.width / 8.0f);
        threadGroupsY2 = Mathf.CeilToInt(Screen.height / 8.0f);

        InitRenderTexture();
    }

    public void ExecuteSVGF(int CurrentSamples, int AtrousKernelSize, ref ComputeBuffer _ColorBuffer, ref RenderTexture _PosTex, ref RenderTexture _target, ref RenderTexture _Albedo, ref RenderTexture _NormTex) {
        InitRenderTexture();
        Matrix4x4 viewprojmatrix = _camera.projectionMatrix * _camera.worldToCameraMatrix;
        var PrevMatrix = PrevViewProjection;
        SVGF.SetMatrix("viewprojection", viewprojmatrix);
        SVGF.SetMatrix("prevviewprojection", PrevMatrix);
        SVGF.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        SVGF.SetInt("Samples_Accumulated", CurrentSamples);
        PrevViewProjection = viewprojmatrix;

        SVGF.SetInt("AtrousIterations", AtrousKernelSize);
        bool OddAtrousIteration = (AtrousKernelSize % 2 == 1);

        SVGF.SetBuffer(CopyKernel, "PerPixelRadiance", _ColorBuffer);
        SVGF.SetTexture(CopyKernel, "PosTex", _PosTex);
        SVGF.SetTexture(CopyKernel, "RWHistoryNormalAndDepth", _HistoryNormalDepth);
        SVGF.SetTexture(CopyKernel, "RWNormalAndDepth", _NormalDepth);
        SVGF.SetTexture(CopyKernel, "PrevPosTex", _PrevPosTex);
        SVGF.SetTexture(CopyKernel, "RWScreenPosPrev", _ScreenPosPrev);
        SVGF.SetTexture(CopyKernel, "ColorDirectOut", _ColorDirectOut);
        SVGF.SetTexture(CopyKernel, "ColorIndirectOut", _ColorIndirectOut);
        SVGF.SetTexture(CopyKernel, "_CameraNormalDepthTex", _NormTex);
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

        SVGF.SetTexture(SVGFAtrousKernel, "NormalAndDepth", _NormalDepth);
        SVGF.SetTexture(SVGFAtrousKernel, "HistoryDirectTex", _HistoryDirect);
        SVGF.SetTexture(SVGFAtrousKernel, "HistoryIndirectTex", _HistoryIndirect);
        for (int i = 0; i < AtrousKernelSize; i++) {
            int step_size = 1 << i;
            bool UseFlipped = (i % 2 == 1);
            SVGF.SetTexture(SVGFAtrousKernel, "ColorDirectOut", (UseFlipped) ? _ColorDirectIn : _ColorDirectOut);
            SVGF.SetTexture(SVGFAtrousKernel, "ColorIndirectOut", (UseFlipped) ? _ColorIndirectIn : _ColorIndirectOut);
            SVGF.SetTexture(SVGFAtrousKernel, "ColorDirectIn", (UseFlipped) ? _ColorDirectOut : _ColorDirectIn);
            SVGF.SetTexture(SVGFAtrousKernel, "ColorIndirectIn", (UseFlipped) ? _ColorIndirectOut : _ColorIndirectIn);
            var step2 = step_size;
            SVGF.SetInt("step_size", step2);
            SVGF.Dispatch(SVGFAtrousKernel, threadGroupsX, threadGroupsY, 1);
        }
        SVGF.SetBuffer(FinalizeKernel, "PerPixelRadiance", _ColorBuffer);
        SVGF.SetTexture(FinalizeKernel, "ColorDirectIn", (OddAtrousIteration) ? _ColorDirectOut : _ColorDirectIn);
        SVGF.SetTexture(FinalizeKernel, "ColorDirectOut", (OddAtrousIteration) ? _ColorDirectIn : _ColorDirectOut);
        SVGF.SetTexture(FinalizeKernel, "NormalAndDepth", _NormalDepth);
        SVGF.SetTexture(FinalizeKernel, "ColorIndirectIn", (OddAtrousIteration) ? _ColorIndirectOut : _ColorIndirectIn);
        SVGF.SetTexture(FinalizeKernel, "HistoryDirectTex", _HistoryDirect);
        SVGF.SetTexture(FinalizeKernel, "HistoryIndirectTex", _HistoryIndirect);
        SVGF.SetTexture(FinalizeKernel, "HistoryMomentTex", _HistoryMoment);
        SVGF.SetTexture(FinalizeKernel, "RWHistoryNormalAndDepth", _HistoryNormalDepth);
        SVGF.SetTexture(FinalizeKernel, "Result", _target);
        SVGF.SetTexture(FinalizeKernel, "HistoryTex", _History);
        SVGF.SetTexture(FinalizeKernel, "_Albedo", _Albedo);
        SVGF.SetTexture(FinalizeKernel, "FrameBufferMoment", _FrameMoment);
        SVGF.Dispatch(FinalizeKernel, threadGroupsX, threadGroupsY, 1);

        SVGF.SetTexture(TAAKernel, "Result", (OddAtrousIteration) ? _ColorDirectIn : _ColorDirectOut);
        SVGF.SetTexture(TAAKernel, "ScreenPosPrev", _ScreenPosPrev);
        SVGF.SetTexture(TAAKernel, "TAAPrev", _TAAPrev);
        SVGF.SetTexture(TAAKernel, "ColorDirectOut", (OddAtrousIteration) ? _ColorDirectOut : _ColorDirectIn);
        SVGF.Dispatch(TAAKernel, threadGroupsX, threadGroupsY, 1);

        SVGF.SetTexture(TAAFinalizeKernel, "TAAPrev", _TAAPrev);
        SVGF.SetTexture(TAAFinalizeKernel, "Result", _target);
        SVGF.SetTexture(TAAFinalizeKernel, "ColorDirectIn", (OddAtrousIteration) ? _ColorDirectOut : _ColorDirectIn);
        SVGF.Dispatch(TAAFinalizeKernel, threadGroupsX, threadGroupsY, 1);

        Graphics.CopyTexture(_PosTex, _PrevPosTex);

    }

    public void ExecuteAtrous(int AtrousKernelSize, float n_phi, float p_phi, float c_phi, ref RenderTexture _PosTex, ref RenderTexture _target, ref RenderTexture _Albedo, ref RenderTexture _converged, ref RenderTexture _NormTex) {
        InitRenderTexture();
        Matrix4x4 viewprojmatrix = _camera.projectionMatrix * _camera.worldToCameraMatrix;
        AtrousDenoiser.SetMatrix("viewprojection", viewprojmatrix);
        AtrousDenoiser.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        AtrousDenoiser.SetTexture(AtrousCopyKernel, "PosTex", _PosTex);
        AtrousDenoiser.SetTexture(AtrousCopyKernel, "RWNormalAndDepth", _NormalDepth);
        AtrousDenoiser.SetTexture(AtrousCopyKernel, "_CameraNormalDepthTex", _NormTex);
        AtrousDenoiser.Dispatch(AtrousCopyKernel, threadGroupsX, threadGroupsY, 1);

        Graphics.CopyTexture(_converged, _ColorDirectIn);
            AtrousDenoiser.SetFloat("n_phi", n_phi);
            AtrousDenoiser.SetFloat("p_phi", p_phi);
            AtrousDenoiser.SetInt("KernelSize", AtrousKernelSize);
            AtrousDenoiser.SetTexture(AtrousKernel, "PosTex", _PosTex);
            AtrousDenoiser.SetTexture(AtrousKernel, "NormalAndDepth", _NormalDepth);
            int CurrentIteration = 0;
            for(int i = 1; i <= AtrousKernelSize; i *= 2) {
                var step_size = i;
                var c_phi2 = c_phi;
                bool UseFlipped = (CurrentIteration % 2 == 1);
                CurrentIteration++;
                AtrousDenoiser.SetTexture(AtrousKernel, "ResultIn", (UseFlipped) ? _ColorDirectOut : _ColorDirectIn);
                AtrousDenoiser.SetTexture(AtrousKernel, "Result", (UseFlipped) ? _ColorDirectIn : _ColorDirectOut);
                AtrousDenoiser.SetFloat("c_phi", c_phi2);
                AtrousDenoiser.SetInt("step_width", step_size);
                AtrousDenoiser.Dispatch(AtrousKernel, threadGroupsX, threadGroupsY, 1);
                c_phi /= 2.0f;
            }
            AtrousDenoiser.SetTexture(AtrousFinalizeKernel, "ResultIn", (CurrentIteration % 2 == 1) ? _ColorDirectIn : _ColorDirectOut);
            AtrousDenoiser.SetTexture(AtrousFinalizeKernel, "_Albedo", _Albedo);
            AtrousDenoiser.SetTexture(AtrousFinalizeKernel, "Result", _target);
            AtrousDenoiser.Dispatch(AtrousFinalizeKernel, threadGroupsX, threadGroupsY, 1);

    }


    public void ExecuteBloom(ref RenderTexture _target, ref RenderTexture _converged) {//need to fix this so it doesnt create new textures every time
        int CurrentWidth = Screen.width;
        int CurrentHeight = Screen.height;
        int TargetWidth = Screen.width / 2;
        int TargetHeight = Screen.height / 2;

        Bloom.SetInt("screen_width", CurrentWidth);
        Bloom.SetInt("screen_height", CurrentHeight);
        Bloom.SetInt("TargetWidth", TargetWidth);
        Bloom.SetInt("TargetHeight", TargetHeight);
        Intermediate = RenderTexture.GetTemporary(TargetWidth, TargetHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        Intermediate.enableRandomWrite = true;
        Bloom.SetTexture(BloomLowPassKernel, "InputTex", _converged);
        Bloom.SetTexture(BloomLowPassKernel, "OutputTex", Intermediate);
        Bloom.Dispatch(BloomLowPassKernel, (int)Mathf.Ceil(TargetWidth / 16.0f), (int)Mathf.Ceil(TargetHeight / 16.0f), 1);
        
        
        for(int i = 0; i < 5; i++) {
        CurrentWidth = TargetWidth;
        CurrentHeight = TargetHeight;
        TargetWidth /= 2;
        TargetHeight /= 2;    
        Bloom.SetInt("screen_width", CurrentWidth);
        Bloom.SetInt("screen_height", CurrentHeight);
        Bloom.SetInt("TargetWidth", TargetWidth);
        Bloom.SetInt("TargetHeight", TargetHeight);
        TempTex = RenderTexture.GetTemporary(TargetWidth, TargetHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        TempTex.enableRandomWrite = true;
        Bloom.SetTexture(BloomDownsampleKernel, "InputTex", Intermediate);
        Bloom.SetTexture(BloomDownsampleKernel, "OutputTex", TempTex);
        Bloom.Dispatch(BloomDownsampleKernel, (int)Mathf.Ceil(TargetWidth / 16.0f), (int)Mathf.Ceil(TargetHeight / 16.0f), 1);
        Intermediate.Release();
        Intermediate = RenderTexture.GetTemporary(TargetWidth, TargetHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        Graphics.CopyTexture(TempTex, Intermediate);
        TempTex.Release();
        }
        SuperIntermediate = RenderTexture.GetTemporary(TargetWidth, TargetHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        Bloom.SetTexture(BloomUpsampleKernel, "OrigTex", _converged);
        Bloom.SetBool("IsFinal", false);
        for(int i = 0; i < 5; i++) {
        CurrentWidth = TargetWidth;
        CurrentHeight = TargetHeight;
        TargetWidth *= 2;
        TargetHeight *= 2;    
        Bloom.SetInt("screen_width", CurrentWidth);
        Bloom.SetInt("screen_height", CurrentHeight);
        Bloom.SetInt("TargetWidth", TargetWidth);
        Bloom.SetInt("TargetHeight", TargetHeight);
        TempTex = RenderTexture.GetTemporary(TargetWidth, TargetHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        TempTex.enableRandomWrite = true;
        
        SuperIntermediate = RenderTexture.GetTemporary(CurrentWidth, CurrentHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        Graphics.CopyTexture(Intermediate, SuperIntermediate);
        
        Bloom.SetTexture(BloomUpsampleKernel, "OrigTex", SuperIntermediate);
        Bloom.SetTexture(BloomUpsampleKernel, "InputTex", Intermediate);
        Bloom.SetTexture(BloomUpsampleKernel, "OutputTex", TempTex);
        Bloom.Dispatch(BloomUpsampleKernel, (int)Mathf.Ceil(TargetWidth / 16.0f), (int)Mathf.Ceil(TargetHeight / 16.0f), 1);
        Intermediate.Release();
        SuperIntermediate.Release();
        Intermediate = RenderTexture.GetTemporary(TargetWidth, TargetHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        Graphics.CopyTexture(TempTex, Intermediate);
        TempTex.Release();
        }
        SuperIntermediate = RenderTexture.GetTemporary(TargetWidth, TargetHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        Graphics.CopyTexture(Intermediate, SuperIntermediate);
        
        Bloom.SetTexture(BloomUpsampleKernel, "OrigTex", _converged);

        CurrentWidth = TargetWidth;
        CurrentHeight = TargetHeight;   
        Bloom.SetInt("screen_width", CurrentWidth);
        Bloom.SetInt("screen_height", CurrentHeight);
        Bloom.SetInt("TargetWidth", Screen.width);
        Bloom.SetInt("TargetHeight", Screen.height);
        Bloom.SetBool("IsFinal", true);

        Bloom.SetTexture(BloomUpsampleKernel, "InputTex", Intermediate);
        Bloom.SetTexture(BloomUpsampleKernel, "OutputTex", _target);
        Bloom.Dispatch(BloomUpsampleKernel, (int)Mathf.Ceil(Screen.width / 16.0f), (int)Mathf.Ceil(Screen.height / 16.0f), 1);
        SuperIntermediate.Release();
        Intermediate.Release();


    }

}
