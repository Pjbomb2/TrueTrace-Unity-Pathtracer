using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Denoiser {
    private ComputeShader SVGF;
    private ComputeShader AtrousDenoiser;
    private ComputeShader Bloom;
    private ComputeShader AutoExpose;
    private ComputeShader TAA;
    private ComputeShader Upscaler;
    private ComputeShader ToneMapper;
    private ComputeShader TAAU;
    private ComputeShader SpecularDenoiser;


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
    private RenderTexture SuperIntermediate;
    private RenderTexture PrevDepthTex;
    private RenderTexture PrevOutputTex;
    private RenderTexture PrevUpscalerTAA;
    private RenderTexture UpScalerLightingDataTexture;

    private RenderTexture SpecularIn;
    private RenderTexture SpecularOut;




    private RenderTexture TAAA;
    private RenderTexture TAAB;
    private RenderTexture[] BloomSamples;



    private ComputeBuffer A;
    public ComputeBuffer B;

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
    private int AtrousKernel;
    private int AtrousCopyKernel;
    private int AtrousFinalizeKernel;

    private int BloomDownsampleKernel;
    private int BloomLowPassKernel;
    private int BloomUpsampleKernel;

    private int ComputeHistogramKernel;
    private int CalcAverageKernel;
    private int ToneMapKernel;

    private int AutoExposeKernel;
    private int AutoExposeFinalizeKernel;

    private int TAAKernel;
    private int TAAFinalizeKernel;
    private int TAAPrepareKernel;

    private int TAAUKernel;
    private int TAAUCopyKernel;

    private int SpecularCopyKernel;
    private int SpecularSpatialKernel;




    private int UpsampleKernel;

    private int SourceWidth;
    private int SourceHeight;
    private int[] BloomWidths;
    private int[] BloomHeights;


    private void CreateRenderTexture(ref RenderTexture ThisTex, bool SRGB) {
        if(SRGB) {
        ThisTex = new RenderTexture(SourceWidth, SourceHeight, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        } else {
        ThisTex = new RenderTexture(SourceWidth, SourceHeight, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        }
        ThisTex.enableRandomWrite = true;
        ThisTex.useMipMap = false;
        ThisTex.Create();
    }


    private void CreateRenderTexture(ref RenderTexture ThisTex, bool SRGB, int Width, int Height) {
        if(SRGB) {
        ThisTex = new RenderTexture(Width, Height, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        } else {
        ThisTex = new RenderTexture(Width, Height, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        }
        ThisTex.enableRandomWrite = true;
        ThisTex.useMipMap = false;
        ThisTex.Create();
    }

    private void CreateRenderTexture2(ref RenderTexture ThisTex, bool SRGB) {
        if(SRGB) {
        ThisTex = new RenderTexture(Screen.width, Screen.height, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
        } else {
        ThisTex = new RenderTexture(Screen.width, Screen.height, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        }
        ThisTex.enableRandomWrite = true;
        ThisTex.useMipMap = false;
        ThisTex.Create();
    }
    private void CreateRenderTextureInt(ref RenderTexture ThisTex) {
        ThisTex = new RenderTexture(SourceWidth, SourceHeight, 0,
            RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
        ThisTex.enableRandomWrite = true;
        ThisTex.Create();
    }
    private void CreateRenderTextureDouble(ref RenderTexture ThisTex) {
        ThisTex = new RenderTexture(SourceWidth, SourceHeight, 0,
            RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);
        ThisTex.enableRandomWrite = true;
        ThisTex.Create();
    }

    private void CreateRenderTextureSingle(ref RenderTexture ThisTex) {
        ThisTex = new RenderTexture(SourceWidth, SourceHeight, 0,
            RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
        ThisTex.enableRandomWrite = true;
        ThisTex.Create();
    }

    public bool SVGFInitialized;
    public void ClearSVGF() {
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
        SVGFInitialized = false;
    }
    public void InitSVGF() {
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
         CreateRenderTextureInt(ref _History);
         SVGFInitialized = true;
    }
    private void InitRenderTexture() {
        if (_ColorDirectIn == null || _ColorDirectIn.width != SourceWidth || _ColorDirectIn.height != SourceHeight) {
            // Release render texture if we already have one
            if (_ColorDirectIn != null) {
                _TAAPrev.Release();
                PrevOutputTex.Release();
                PrevUpscalerTAA.Release();
                TAAA.Release();
                TAAB.Release();
                // SpecularIn.Release();
                // SpecularOut.Release();
            }

         CreateRenderTexture2(ref _TAAPrev, false);
         CreateRenderTexture2(ref PrevDepthTex, false);
         CreateRenderTexture2(ref PrevOutputTex, false);
         CreateRenderTexture2(ref PrevUpscalerTAA, false);
         CreateRenderTexture2(ref UpScalerLightingDataTexture, false);
         CreateRenderTexture2(ref TAAA, false);
         CreateRenderTexture2(ref TAAB, false);
         // CreateRenderTexture(ref SpecularIn, false);
         // CreateRenderTexture(ref SpecularOut, false);
         BloomSamples = new RenderTexture[8];
         int BloomWidth = Screen.width / 2;
         int BloomHeight = Screen.height / 2;
         BloomWidths = new int[8];
         BloomHeights = new int[8];
         for(int i = 0; i < 8; i++) {
            CreateRenderTexture(ref BloomSamples[i], false, BloomWidth, BloomHeight);
            BloomWidths[i] = BloomWidth;
            BloomHeights[i] = BloomHeight;
            BloomWidth /= 2;
            BloomHeight /= 2;
         }
        }
    }
    
    public Denoiser(Camera Cam, int SourceWidth, int SourceHeight) {
        this.SourceWidth = SourceWidth;
        this.SourceHeight = SourceHeight;
        _camera = Cam;
        SVGFInitialized = false;

        if(SVGF == null) {SVGF = Resources.Load<ComputeShader>("PostProcess/SVGF");}
        if(AtrousDenoiser == null) {AtrousDenoiser = Resources.Load<ComputeShader>("PostProcess/Atrous");}
        if(AutoExpose == null) {AutoExpose = Resources.Load<ComputeShader>("PostProcess/AutoExpose");}
        if(Bloom == null) {Bloom = Resources.Load<ComputeShader>("PostProcess/Bloom");}
        if(TAA == null) {TAA = Resources.Load<ComputeShader>("PostProcess/TAA");}
        if(Upscaler == null) {Upscaler = Resources.Load<ComputeShader>("PostProcess/Upscaler");}
        if(ToneMapper == null) {ToneMapper = Resources.Load<ComputeShader>("PostProcess/ToneMap");}
        if(TAAU == null) {TAAU = Resources.Load<ComputeShader>("PostProcess/TAAU");}
        if(SpecularDenoiser == null) {SpecularDenoiser = Resources.Load<ComputeShader>("PostProcess/SpecularDenoiser");}


        VarianceKernel = SVGF.FindKernel("kernel_variance");
        CopyKernel = SVGF.FindKernel("kernel_copy");
        ReprojectKernel = SVGF.FindKernel("kernel_reproject");
        FinalizeKernel = SVGF.FindKernel("kernel_finalize");
        SVGFAtrousKernel = SVGF.FindKernel("kernel_atrous");
        AtrousKernel = AtrousDenoiser.FindKernel("Atrous");
        AtrousCopyKernel = AtrousDenoiser.FindKernel("kernel_copy");
        AtrousFinalizeKernel = AtrousDenoiser.FindKernel("kernel_finalize");

        TAAUKernel = TAAU.FindKernel("TAAU");
        TAAUCopyKernel = TAAU.FindKernel("Copy");


        BloomDownsampleKernel = Bloom.FindKernel("Downsample");
        BloomLowPassKernel = Bloom.FindKernel("LowPass");
        BloomUpsampleKernel = Bloom.FindKernel("Upsample");

        TAAKernel = TAA.FindKernel("kernel_taa");
        TAAFinalizeKernel = TAA.FindKernel("kernel_taa_finalize");
        TAAPrepareKernel = TAA.FindKernel("kernel_taa_prepare");
        
        UpsampleKernel = Upscaler.FindKernel("kernel_upsample");

        SpecularCopyKernel = SpecularDenoiser.FindKernel("Copy");
        SpecularSpatialKernel = SpecularDenoiser.FindKernel("Spatial");


        AutoExposeKernel = AutoExpose.FindKernel("AutoExpose");
        AutoExposeFinalizeKernel = AutoExpose.FindKernel("AutoExposeFinalize");
        List<float> TestBuffer = new List<float>();
        TestBuffer.Add(1);
        if(A == null) {A = new ComputeBuffer(1, sizeof(float)); A.SetData(TestBuffer);}
        SVGF.SetInt("screen_width", SourceWidth);
        SVGF.SetInt("screen_height", SourceHeight);
        SVGF.SetInt("TargetWidth", Screen.width);
        SVGF.SetInt("TargetHeight", Screen.height);

        Bloom.SetInt("screen_width", Screen.width);
        Bloom.SetInt("screen_width", Screen.height);

        AtrousDenoiser.SetInt("screen_width", Screen.width);
        AtrousDenoiser.SetInt("screen_height", Screen.height);

        AutoExpose.SetInt("screen_width", Screen.width);
        AutoExpose.SetInt("screen_height", Screen.height);
        AutoExpose.SetBuffer(AutoExposeKernel, "A", A);
        AutoExpose.SetBuffer(AutoExposeFinalizeKernel, "A", A);

        TAA.SetInt("screen_width", Screen.width);
        TAA.SetInt("screen_height", Screen.height);

        SpecularDenoiser.SetInt("screen_width", Screen.width);
        SpecularDenoiser.SetInt("screen_height", Screen.height);

        threadGroupsX = Mathf.CeilToInt(SourceWidth / 16.0f);
        threadGroupsY = Mathf.CeilToInt(SourceHeight / 16.0f);

        threadGroupsX2 = Mathf.CeilToInt(Screen.width / 16.0f);
        threadGroupsY2 = Mathf.CeilToInt(Screen.height / 16.0f);


        InitRenderTexture();
    }

    public void ExecuteSVGF(int CurrentSamples, int AtrousKernelSize, ref ComputeBuffer _ColorBuffer, ref RenderTexture _target, ref RenderTexture _Albedo, ref RenderTexture _NormTex, bool DiffRes, ref RenderTexture PrevDepthTexMain, ref RenderTexture PrevNormalTex) {

        InitRenderTexture();
        SVGF.SetBool("DiffRes", DiffRes);
        Matrix4x4 viewprojmatrix = _camera.projectionMatrix * _camera.worldToCameraMatrix;
        var PrevMatrix = PrevViewProjection;
        SVGF.SetMatrix("viewprojection", viewprojmatrix);
        SVGF.SetMatrix("prevviewprojection", PrevMatrix);
        SVGF.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        SVGF.SetInt("Samples_Accumulated", CurrentSamples);
        PrevViewProjection = viewprojmatrix;
        SVGF.SetInt("AtrousIterations", AtrousKernelSize);
        bool OddAtrousIteration = (AtrousKernelSize % 2 == 1);
        UnityEngine.Profiling.Profiler.BeginSample("SVGFCopy");
        SVGF.SetBuffer(CopyKernel, "PerPixelRadiance", _ColorBuffer);
        SVGF.SetTexture(CopyKernel, "RWHistoryNormalAndDepth", _HistoryNormalDepth);
        SVGF.SetTexture(CopyKernel, "RWNormalAndDepth", _NormalDepth);
        SVGF.SetTexture(CopyKernel, "RWScreenPosPrev", _ScreenPosPrev);
        SVGF.SetTexture(CopyKernel, "ColorDirectOut", _ColorDirectOut);
        SVGF.SetTexture(CopyKernel, "_Albedo", _Albedo);
        SVGF.SetTexture(CopyKernel, "ColorIndirectOut", _ColorIndirectOut);
        SVGF.SetFloat("FarPlane", _camera.farClipPlane);
        SVGF.SetTextureFromGlobal(CopyKernel, "MotionVectors", "_CameraMotionVectorsTexture");
        SVGF.SetTextureFromGlobal(CopyKernel, "DepthTex", "_CameraDepthTexture");
        SVGF.SetTextureFromGlobal(CopyKernel, "PrevDepthTex", "_LastCameraDepthTexture");
        SVGF.SetTexture(CopyKernel, "_CameraNormalDepthTex", _NormTex);
        SVGF.SetTexture(CopyKernel, "PrevDepthTexMain", PrevDepthTexMain);
        SVGF.SetTextureFromGlobal(CopyKernel, "NormalTex", "_CameraGBufferTexture2");
        SVGF.SetTexture(CopyKernel, "PrevNormTex", PrevNormalTex);
        SVGF.Dispatch(CopyKernel, threadGroupsX, threadGroupsY, 1);
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("SVGFReproject");
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
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("SVGFVariance");
        SVGF.SetTexture(VarianceKernel, "ColorDirectOut", _ColorDirectOut);
        SVGF.SetTexture(VarianceKernel, "ColorIndirectOut", _ColorIndirectOut);
        SVGF.SetTexture(VarianceKernel, "ColorDirectIn", _ColorDirectIn);
        SVGF.SetTexture(VarianceKernel, "ColorIndirectIn", _ColorIndirectIn);
        SVGF.SetTexture(VarianceKernel, "NormalAndDepth", _NormalDepth);
        SVGF.SetTexture(VarianceKernel, "FrameBufferMoment", _FrameMoment);
        SVGF.SetTexture(VarianceKernel, "HistoryTex", _History);
        SVGF.Dispatch(VarianceKernel, threadGroupsX, threadGroupsY, 1);
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("SVGFAtrous");
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
        UnityEngine.Profiling.Profiler.EndSample();
        // DenoiseSpecular(ref _ColorBuffer, ref _NormTex, ref DirectionRoughnessTex);
        UnityEngine.Profiling.Profiler.BeginSample("SVGFFinalize");
        SVGF.SetBuffer(FinalizeKernel, "PerPixelRadiance", _ColorBuffer);
        SVGF.SetTexture(FinalizeKernel, "ColorDirectIn", (OddAtrousIteration) ? _ColorDirectOut : _ColorDirectIn);
        SVGF.SetTexture(FinalizeKernel, "ColorDirectOut", (OddAtrousIteration) ? _ColorDirectIn : _ColorDirectOut);
        SVGF.SetTexture(FinalizeKernel, "NormalAndDepth", _NormalDepth);
        SVGF.SetTexture(FinalizeKernel, "ColorIndirectIn", (OddAtrousIteration) ? _ColorIndirectOut : _ColorIndirectIn);
        SVGF.SetTexture(FinalizeKernel, "HistoryDirectTex", _HistoryDirect);
        // SVGF.SetTexture(FinalizeKernel, "Specular", SpecularOut);
        SVGF.SetTexture(FinalizeKernel, "HistoryIndirectTex", _HistoryIndirect);
        SVGF.SetTexture(FinalizeKernel, "HistoryMomentTex", _HistoryMoment);
        SVGF.SetTexture(FinalizeKernel, "RWHistoryNormalAndDepth", _HistoryNormalDepth);
        SVGF.SetTexture(FinalizeKernel, "Result", _target);
        SVGF.SetTexture(FinalizeKernel, "HistoryTex", _History);
        SVGF.SetTexture(FinalizeKernel, "_Albedo", _Albedo);
        
        SVGF.SetTexture(FinalizeKernel, "FrameBufferMoment", _FrameMoment);
        SVGF.Dispatch(FinalizeKernel, threadGroupsX, threadGroupsY, 1);
        UnityEngine.Profiling.Profiler.EndSample();

    }




    public void DenoiseSpecular(ref ComputeBuffer ColorBuffer, ref RenderTexture Normals, ref RenderTexture DirectionRoughnessTex) {
        SpecularDenoiser.SetBuffer(SpecularCopyKernel, "PerPixelRadiance", ColorBuffer);
        SpecularDenoiser.SetTexture(SpecularCopyKernel, "SpecularOut", SpecularOut);
        SpecularDenoiser.Dispatch(SpecularCopyKernel, threadGroupsX, threadGroupsY, 1);
            SpecularDenoiser.SetMatrix("UNITY_MAXTRIX_VP", _camera.projectionMatrix * _camera.worldToCameraMatrix);

        for(int i = 0; i < 8; i++) {
            var tempstep = i << 1;
            SpecularDenoiser.SetInt("step_size", tempstep);
            SpecularDenoiser.SetTextureFromGlobal(SpecularSpatialKernel, "DepthTex", "_CameraDepthTexture");
            SpecularDenoiser.SetTexture(SpecularSpatialKernel, "Normals", Normals);
            SpecularDenoiser.SetTexture(SpecularSpatialKernel, "DirectionRoughness", DirectionRoughnessTex);
            SpecularDenoiser.SetTexture(SpecularSpatialKernel, "SpecularIn", (i % 2 == 0) ? SpecularOut : SpecularIn);
            SpecularDenoiser.SetTexture(SpecularSpatialKernel, "SpecularOut", (i % 2 == 0) ? SpecularIn : SpecularOut);
            SpecularDenoiser.Dispatch(SpecularSpatialKernel, threadGroupsX, threadGroupsY, 1);
        }
    }




    public void ExecuteAtrous(int AtrousKernelSize, float n_phi, float p_phi, float c_phi, ref RenderTexture _PosTex, ref RenderTexture _target, ref RenderTexture _converged, ref RenderTexture _Albedo, ref RenderTexture _NormTex, int curframe) {
        InitRenderTexture();
        Matrix4x4 viewprojmatrix = _camera.projectionMatrix * _camera.worldToCameraMatrix;
        AtrousDenoiser.SetMatrix("viewprojection", viewprojmatrix);
        AtrousDenoiser.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        AtrousDenoiser.SetTexture(AtrousCopyKernel, "PosTex", _PosTex);
        AtrousDenoiser.SetTexture(AtrousCopyKernel, "RWNormalAndDepth", _NormalDepth);
        AtrousDenoiser.SetTexture(AtrousCopyKernel, "_CameraNormalDepthTex", _NormTex);
        AtrousDenoiser.SetTextureFromGlobal(AtrousCopyKernel, "NormalTex", "_CameraGBufferTexture2");
        AtrousDenoiser.Dispatch(AtrousCopyKernel, threadGroupsX2, threadGroupsY2, 1);

        Graphics.CopyTexture(_converged, 0, 0, _ColorDirectIn, 0, 0);
            AtrousDenoiser.SetFloat("n_phi", n_phi);
            AtrousDenoiser.SetFloat("p_phi", p_phi);
            AtrousDenoiser.SetInt("KernelSize", AtrousKernelSize);
            AtrousDenoiser.SetFloat("CurFrame", curframe);
            AtrousDenoiser.SetTexture(AtrousKernel, "PosTex", _PosTex);
            AtrousDenoiser.SetTexture(AtrousKernel, "NormalAndDepth", _NormalDepth);
            int CurrentIteration = 0;
            AtrousDenoiser.SetTexture(AtrousKernel, "_Albedo", _Albedo);

            for(int i = 1; i <= AtrousKernelSize; i++) {
                var step_size = i;
                var c_phi2 = c_phi;
                bool UseFlipped = (CurrentIteration % 2 == 1);
                CurrentIteration++;
                AtrousDenoiser.SetTexture(AtrousKernel, "ResultIn", (UseFlipped) ? _ColorDirectOut : _ColorDirectIn);
                AtrousDenoiser.SetTexture(AtrousKernel, "Result", (UseFlipped) ? _ColorDirectIn : _ColorDirectOut);
                AtrousDenoiser.SetFloat("c_phi", c_phi2);
                AtrousDenoiser.SetInt("step_width", step_size);
                AtrousDenoiser.Dispatch(AtrousKernel, threadGroupsX2, threadGroupsY2, 1);
                c_phi /= 2.0f;
            }
            AtrousDenoiser.SetTexture(AtrousFinalizeKernel, "ResultIn", AtrousKernelSize == 0 ? _target : (CurrentIteration % 2 == 1) ? _ColorDirectIn : _ColorDirectOut);
            AtrousDenoiser.SetTexture(AtrousFinalizeKernel, "_Albedo", _Albedo);
            AtrousDenoiser.SetTexture(AtrousFinalizeKernel, "Result", _target);
            AtrousDenoiser.Dispatch(AtrousFinalizeKernel, threadGroupsX2, threadGroupsY2, 1);
    }

    public void ExecuteBloom(ref RenderTexture _target, ref RenderTexture _converged, float BloomStrength) {//need to fix this so it doesnt create new textures every time

        Bloom.SetFloat("strength", BloomStrength);
        Bloom.SetInt("screen_width", Screen.width);
        Bloom.SetInt("screen_height", Screen.height);
            Bloom.SetInt("TargetWidth", BloomWidths[0]);
            Bloom.SetInt("TargetHeight", BloomHeights[0]);
        Bloom.SetTexture(BloomLowPassKernel, "InputTex", _converged);
        Bloom.SetTexture(BloomLowPassKernel, "OutputTex", BloomSamples[0]);
        Bloom.Dispatch(BloomLowPassKernel, (int)Mathf.Ceil(BloomWidths[0] / 16.0f), (int)Mathf.Ceil(BloomHeights[0] / 16.0f), 1);
        for(int i = 1; i < 6; i++) {
            // Debug.Log(BloomWidths[i]);
            Bloom.SetInt("TargetWidth", BloomWidths[i]);
            Bloom.SetInt("TargetHeight", BloomHeights[i]);
            Bloom.SetInt("screen_width", BloomWidths[i - 1]);
            Bloom.SetInt("screen_height", BloomHeights[i - 1]);
            Bloom.SetTexture(BloomDownsampleKernel, "InputTex", BloomSamples[i - 1]);
            Bloom.SetTexture(BloomDownsampleKernel, "OutputTex", BloomSamples[i]);
            Bloom.Dispatch(BloomDownsampleKernel, (int)Mathf.Ceil(BloomWidths[i - 1] / 16.0f), (int)Mathf.Ceil(BloomHeights[i - 1] / 16.0f), 1);
        }
            Bloom.SetBool("IsFinal", false);

        for(int i = 5; i > 0; i--) {
            Bloom.SetInt("TargetWidth", BloomWidths[i - 1]);
            Bloom.SetInt("TargetHeight", BloomHeights[i - 1]);
            Bloom.SetInt("screen_width", BloomWidths[i]);
            Bloom.SetInt("screen_height", BloomHeights[i]);
            Bloom.SetTexture(BloomUpsampleKernel, "InputTex", BloomSamples[i]);
            Bloom.SetTexture(BloomUpsampleKernel, "OutputTex", BloomSamples[i - 1]);
            Bloom.SetTexture(BloomUpsampleKernel, "OrigTex", BloomSamples[i - 1]);

            Bloom.Dispatch(BloomUpsampleKernel, (int)Mathf.Ceil(BloomWidths[i - 1] / 16.0f), (int)Mathf.Ceil(BloomHeights[i - 1] / 16.0f), 1);
        }
        Bloom.SetInt("TargetWidth", Screen.width);
        Bloom.SetInt("TargetHeight", Screen.height);
        Bloom.SetInt("screen_width", BloomWidths[0]);
        Bloom.SetInt("screen_height", BloomHeights[0]);
        Bloom.SetBool("IsFinal", true);
        Bloom.SetTexture(BloomUpsampleKernel, "OrigTex", _converged);
        Bloom.SetTexture(BloomUpsampleKernel, "InputTex", BloomSamples[0]);
        Bloom.SetTexture(BloomUpsampleKernel, "OutputTex", _target);
        Bloom.Dispatch(BloomUpsampleKernel, (int)Mathf.Ceil(Screen.width / 16.0f), (int)Mathf.Ceil(Screen.height / 16.0f), 1);



    }


    public void ExecuteAutoExpose(ref RenderTexture _target, ref RenderTexture _converged, float Exposure) {//need to fix this so it doesnt create new textures every time
        AutoExpose.SetTexture(AutoExposeKernel, "InTex", _converged);
        AutoExpose.SetFloat("Exposure", Exposure);
        AutoExpose.Dispatch(AutoExposeKernel, 1, 1, 1);
        AutoExpose.SetTexture(AutoExposeFinalizeKernel, "InTex", _converged);
        AutoExpose.SetTexture(AutoExposeFinalizeKernel, "OutTex", _target);
        AutoExpose.Dispatch(AutoExposeFinalizeKernel, threadGroupsX2, threadGroupsY2, 1);


    }

    public void ExecuteTAA(ref RenderTexture Input, ref RenderTexture _Final, int CurrentSamples) {//need to fix this so it doesnt create new textures every time
        
        TAA.SetInt("Samples_Accumulated", CurrentSamples);

        RenderTexture TempTex = RenderTexture.GetTemporary(Input.descriptor);
        RenderTexture TempTex2 = RenderTexture.GetTemporary(Input.descriptor);

        UnityEngine.Profiling.Profiler.BeginSample("TAAKernel Prepare");
        TAA.SetFloat("FarPlane", _camera.farClipPlane);
        TAA.SetTextureFromGlobal(TAAPrepareKernel, "MotionVectors", "_CameraMotionVectorsTexture");
        TAA.SetTextureFromGlobal(TAAPrepareKernel, "DepthTex", "_CameraDepthTexture");
        TAA.SetTexture(TAAPrepareKernel, "ColorIn", Input);
        TAA.SetTexture(TAAPrepareKernel, "ColorOut", TempTex);
        TAA.Dispatch(TAAPrepareKernel, threadGroupsX2, threadGroupsY2, 1);
        UnityEngine.Profiling.Profiler.EndSample();


        UnityEngine.Profiling.Profiler.BeginSample("TAAKernel");
        TAA.SetTexture(TAAKernel, "ColorIn", TempTex);
        TAA.SetTextureFromGlobal(TAAKernel, "MotionVectors", "_CameraMotionVectorsTexture");
        TAA.SetTexture(TAAKernel, "TAAPrev", _TAAPrev);
        TAA.SetTexture(TAAKernel, "ColorOut", TempTex2);
        TAA.Dispatch(TAAKernel, threadGroupsX2, threadGroupsY2, 1);
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("TAAFinalize");
        TAA.SetTexture(TAAFinalizeKernel, "TAAPrev", _TAAPrev);
        TAA.SetTexture(TAAFinalizeKernel, "ColorOut", _Final);
        TAA.SetTexture(TAAFinalizeKernel, "ColorIn", TempTex2);
        TAA.Dispatch(TAAFinalizeKernel, threadGroupsX2, threadGroupsY2, 1);
        UnityEngine.Profiling.Profiler.EndSample();

        RenderTexture.ReleaseTemporary(TempTex);
        RenderTexture.ReleaseTemporary(TempTex2);
    }

    Matrix4x4 PreviousCameraMatrix;
    Matrix4x4 PreviousCameraInverseMatrix;
    Matrix4x4 PrevProjInv;

    public void ExecuteUpsample(ref RenderTexture Input, ref RenderTexture Output, ref RenderTexture OrigPos, int curframe, int cursample) {//need to fix this so it doesnt create new textures every time
        UnityEngine.Profiling.Profiler.BeginSample("Upscale");
        Upscaler.SetInt("curframe", curframe);
        Upscaler.SetInt("cursam", cursample);
        Upscaler.SetInt("source_width", Input.width);
        Upscaler.SetInt("source_height", Input.height);
        Upscaler.SetInt("target_width", Output.width);
        Upscaler.SetInt("target_height", Output.height);
        Upscaler.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        Upscaler.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        Upscaler.SetMatrix("_PrevCameraToWorld", PreviousCameraMatrix);
        Upscaler.SetMatrix("_PrevCameraInverseProjection", PreviousCameraInverseMatrix);
        Upscaler.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        Upscaler.SetMatrix("_PrevCameraInverseProjection", PrevProjInv);
        Upscaler.SetVector("Forward", _camera.transform.forward);
        Upscaler.SetTexture(UpsampleKernel, "PosTex", OrigPos);
        Upscaler.SetVector("CamPos", _camera.transform.position);
        Upscaler.SetMatrix("ViewProjectionMatrix",  _camera.projectionMatrix * _camera.worldToCameraMatrix);
        Upscaler.SetFloat("FarPlane", _camera.farClipPlane);
        Upscaler.SetTextureFromGlobal(UpsampleKernel, "Albedo", "_CameraGBufferTexture0");
        Upscaler.SetTextureFromGlobal(UpsampleKernel, "DepthTex", "_CameraDepthTexture");
       // Upscaler.SetTextureFromGlobal(UpsampleKernel, "PrevDepthTex", "_LastCameraDepthTexture");
        Upscaler.SetTexture(UpsampleKernel, "PrevDepthTex", PrevDepthTex);
        Upscaler.SetTextureFromGlobal(UpsampleKernel, "NormalTex", "_CameraGBufferTexture2");
        Upscaler.SetTextureFromGlobal(UpsampleKernel, "MotionVectors", "_CameraMotionVectorsTexture");
        RenderTexture TempTex = RenderTexture.GetTemporary(Output.descriptor);
        RenderTexture TempTex2 = RenderTexture.GetTemporary(Output.descriptor);
        RenderTexture TempTex3 = RenderTexture.GetTemporary(Output.descriptor);
        Upscaler.SetTexture(UpsampleKernel, "Input", Input);
        Upscaler.SetTexture(UpsampleKernel, "Output", UpScalerLightingDataTexture);
        Upscaler.SetTexture(UpsampleKernel, "FinalOutput", Output);
        Upscaler.SetTexture(UpsampleKernel, "PrevOutput", PrevOutputTex);
        Upscaler.SetTexture(UpsampleKernel, "PrevDepthTexWrite", TempTex);
        Upscaler.SetTexture(UpsampleKernel, "TAAPrev", PrevUpscalerTAA);
        Upscaler.SetTexture(UpsampleKernel, "TAAPrevWrite", TempTex3);
        Upscaler.Dispatch(UpsampleKernel, threadGroupsX2, threadGroupsY2, 1);
        UnityEngine.Profiling.Profiler.EndSample();
        Graphics.CopyTexture(TempTex, PrevDepthTex);
        Graphics.CopyTexture(UpScalerLightingDataTexture, PrevOutputTex);
        Graphics.CopyTexture(TempTex3, PrevUpscalerTAA);
        RenderTexture.ReleaseTemporary(TempTex);
        RenderTexture.ReleaseTemporary(TempTex2);
        RenderTexture.ReleaseTemporary(TempTex3);
        PreviousCameraMatrix = _camera.cameraToWorldMatrix;
        PreviousCameraInverseMatrix = _camera.projectionMatrix.inverse;
//PrevDepthTex = Shader.GetGlobalTexture("_LastCameraDepthTexture");
        PrevProjInv = _camera.projectionMatrix.inverse;
    }


    public void ExecuteToneMap(ref RenderTexture Output) {//need to fix this so it doesnt create new textures every time
        ToneMapper.SetInt("width", Output.width);
        ToneMapper.SetInt("height", Output.height);
        ToneMapper.SetTexture(0, "Result", Output);
        ToneMapper.Dispatch(0,threadGroupsX2,threadGroupsY2,1);
    }

    public void ExecuteTAAU(ref RenderTexture Output, ref RenderTexture Input) {//need to fix this so it doesnt create new textures every time
        TAAU.SetInt("source_width", SourceWidth);
        TAAU.SetInt("source_height", SourceHeight);
        TAAU.SetInt("target_width", Output.width);
        TAAU.SetInt("target_height", Output.height);
        TAAU.SetTexture(TAAUKernel, "IMG_ASVGF_TAA_A", TAAA);
        TAAU.SetTexture(TAAUKernel, "TEX_ASVGF_TAA_B", TAAB);
        TAAU.SetTexture(TAAUKernel, "TEX_FLAT_COLOR", Input);
        TAAU.SetTexture(TAAUKernel, "IMG_TAA_OUTPUT", Output);
        TAAU.SetTextureFromGlobal(TAAUKernel, "Albedo", "_CameraGBufferTexture0");
        TAAU.SetTextureFromGlobal(TAAUKernel, "TEX_FLAT_MOTION", "_CameraMotionVectorsTexture");
        TAAU.Dispatch(TAAUKernel,threadGroupsX2,threadGroupsY2,1);
        Graphics.CopyTexture(TAAA, TAAB);
    }

}


