using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrueTrace {
    [System.Serializable]
    public class Denoiser
    {
        private ComputeShader SVGF;
        private ComputeShader Bloom;
        private ComputeShader AutoExpose;
        private ComputeShader TAA;
        private ComputeShader Upscaler;
        private ComputeShader ToneMapper;
        private ComputeShader TAAU;


        public RenderTexture _ColorDirectIn;
        public RenderTexture _ColorIndirectIn;
        public RenderTexture _ColorDirectOut;
        public RenderTexture _ColorIndirectOut;
        public RenderTexture _PrevPosTex;
        public RenderTexture _ScreenPosPrev;
        public RenderTexture _HistoryDirect;
        public RenderTexture _HistoryIndirect;
        public RenderTexture _HistoryMoment;
        public RenderTexture _HistoryNormalDepth;
        public RenderTexture _NormalDepth;
        public RenderTexture _FrameMoment;
        public RenderTexture _History;
        public RenderTexture _TAAPrev;
        public RenderTexture Intermediate;
        public RenderTexture SuperIntermediate;
        public RenderTexture PrevDepthTex;
        public RenderTexture PrevOutputTex;
        private RenderTexture PrevUpscalerTAA;
        private RenderTexture UpScalerLightingDataTexture;
        private RenderTexture PrevNormalUpscalerTex;
        private RenderTexture PrevNormalUpscalerTexWrite;
        private RenderTexture PrevWorldPosWrite;
        private RenderTexture PrevWorldPos;

        private RenderTexture TempTex;
        private RenderTexture TempTex2;






        private RenderTexture TAAA;
        private RenderTexture TAAB;
        public RenderTexture[] BloomSamples;

        public RenderTexture[] mips;
        public RenderTexture[] mipsWeights;
        public RenderTexture[] mipsAssemble;

        private int ToneMapLuminanceKernel;
        private int ToneMapExposureWeightKernel;
        private int ToneMapBlendKernel;
        private int ToneMapBlendLapLaceKernel;
        private int ToneMapCombineKernel;




        public ComputeBuffer A;

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




        private int UpsampleKernel;

        private int SourceWidth;
        private int SourceHeight;
        private int[] BloomWidths;
        private int[] BloomHeights;


        private void CreateRenderTexture(ref RenderTexture ThisTex, bool SRGB)
        {
            ThisTex?.Release();
            if (SRGB)
            {
                ThisTex = new RenderTexture(SourceWidth, SourceHeight, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            }
            else
            {
                ThisTex = new RenderTexture(SourceWidth, SourceHeight, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            }
            ThisTex.enableRandomWrite = true;
            ThisTex.useMipMap = false;
            ThisTex.Create();
        }


        private void CreateRenderTexture(ref RenderTexture ThisTex, bool SRGB, int Width, int Height)
        {
            ThisTex?.Release();
            if (SRGB)
            {
                ThisTex = new RenderTexture(Width, Height, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            }
            else
            {
                ThisTex = new RenderTexture(Width, Height, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            }
            ThisTex.enableRandomWrite = true;
            ThisTex.useMipMap = false;
            ThisTex.Create();
        }

        private void CreateRenderTexture2(ref RenderTexture ThisTex, bool SRGB)
        {
            ThisTex?.Release();
            if (SRGB)
            {
                ThisTex = new RenderTexture(Screen.width, Screen.height, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.sRGB);
            }
            else
            {
                ThisTex = new RenderTexture(Screen.width, Screen.height, 0,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            }
            ThisTex.enableRandomWrite = true;
            ThisTex.useMipMap = false;
            ThisTex.Create();
        }
        private void CreateRenderTextureInt(ref RenderTexture ThisTex)
        {
            ThisTex?.Release();
            ThisTex = new RenderTexture(SourceWidth, SourceHeight, 0,
                RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }
        private void CreateRenderTextureDouble(ref RenderTexture ThisTex)
        {
            ThisTex?.Release();
            ThisTex = new RenderTexture(SourceWidth, SourceHeight, 0,
                RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }

        private void CreateRenderTextureSingle(ref RenderTexture ThisTex)
        {
            ThisTex?.Release();
            ThisTex = new RenderTexture(SourceWidth, SourceHeight, 0,
                RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }

        public bool SVGFInitialized;
        public void ClearSVGF()
        {
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
        public void InitSVGF()
        {
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
        private void InitRenderTexture(bool Force = false)
        {
            if (Force || _ColorDirectIn == null || _ColorDirectIn.width != SourceWidth || _ColorDirectIn.height != SourceHeight)
            {
                // Release render texture if we already have one
                if (_ColorDirectIn != null)
                {
                    _TAAPrev.Release();
                    PrevOutputTex.Release();
                    PrevUpscalerTAA.Release();
                    TAAA.Release();
                    TAAB.Release();
                    TempTex.Release();
                    TempTex2.Release();
                    PrevNormalUpscalerTex.Release();
                    PrevNormalUpscalerTexWrite.Release();
                }

                CreateRenderTexture2(ref _TAAPrev, false);
                CreateRenderTexture2(ref PrevDepthTex, false);
                CreateRenderTexture2(ref PrevOutputTex, false);
                CreateRenderTexture2(ref PrevUpscalerTAA, false);
                CreateRenderTexture2(ref UpScalerLightingDataTexture, false);
                CreateRenderTexture2(ref TAAA, false);
                CreateRenderTexture2(ref TAAB, false);
                CreateRenderTexture2(ref TempTex, false);
                CreateRenderTexture2(ref TempTex2, false);
                CreateRenderTexture2(ref PrevNormalUpscalerTex, false);
                CreateRenderTexture2(ref PrevNormalUpscalerTexWrite, false);
                CreateRenderTexture2(ref PrevWorldPos, false);
                CreateRenderTexture2(ref PrevWorldPosWrite, false);
                BloomSamples = new RenderTexture[8];
                int BloomWidth = Screen.width / 2;
                int BloomHeight = Screen.height / 2;
                BloomWidths = new int[8];
                BloomHeights = new int[8];
                for (int i = 0; i < 8; i++)
                {
                    CreateRenderTexture(ref BloomSamples[i], false, BloomWidth, BloomHeight);
                    BloomWidths[i] = BloomWidth;
                    BloomHeights[i] = BloomHeight;
                    BloomWidth /= 2;
                    BloomHeight /= 2;
                }
            }
        }
        void OnApplicationQuit()
        {
            if (A != null) A.Release();
            for(int i = 0; i < BloomSamples.Length; i++) {
                BloomSamples[i].Release();
            }
        }

        public Denoiser(Camera Cam, int SourceWidth, int SourceHeight)
        {
            this.SourceWidth = SourceWidth;
            this.SourceHeight = SourceHeight;
            _camera = Cam;
            SVGFInitialized = false;

            if (SVGF == null) { SVGF = Resources.Load<ComputeShader>("PostProcess/Compute/SVGF"); }
            if (AutoExpose == null) { AutoExpose = Resources.Load<ComputeShader>("PostProcess/Compute/AutoExpose"); }
            if (Bloom == null) { Bloom = Resources.Load<ComputeShader>("PostProcess/Compute/Bloom"); }
            if (TAA == null) { TAA = Resources.Load<ComputeShader>("PostProcess/Compute/TAA"); }
            if (Upscaler == null) { Upscaler = Resources.Load<ComputeShader>("PostProcess/Compute/Upscaler"); }
            if (ToneMapper == null) { ToneMapper = Resources.Load<ComputeShader>("PostProcess/Compute/ToneMap"); }
            if (TAAU == null) { TAAU = Resources.Load<ComputeShader>("PostProcess/Compute/TAAU"); }


            VarianceKernel = SVGF.FindKernel("kernel_variance");
            CopyKernel = SVGF.FindKernel("kernel_copy");
            ReprojectKernel = SVGF.FindKernel("kernel_reproject");
            FinalizeKernel = SVGF.FindKernel("kernel_finalize");
            SVGFAtrousKernel = SVGF.FindKernel("kernel_atrous");

            TAAUKernel = TAAU.FindKernel("TAAU");
            TAAUCopyKernel = TAAU.FindKernel("Copy");


            BloomDownsampleKernel = Bloom.FindKernel("Downsample");
            BloomLowPassKernel = Bloom.FindKernel("LowPass");
            BloomUpsampleKernel = Bloom.FindKernel("Upsample");

            TAAKernel = TAA.FindKernel("kernel_taa");
            TAAFinalizeKernel = TAA.FindKernel("kernel_taa_finalize");
            TAAPrepareKernel = TAA.FindKernel("kernel_taa_prepare");

            UpsampleKernel = Upscaler.FindKernel("kernel_upsample");

            AutoExposeKernel = AutoExpose.FindKernel("AutoExpose");
            AutoExposeFinalizeKernel = AutoExpose.FindKernel("AutoExposeFinalize");
            List<float> TestBuffer = new List<float>();
            TestBuffer.Add(1);
            A?.Dispose(); A = new ComputeBuffer(1, sizeof(float)); A.SetData(TestBuffer);
            SVGF.SetInt("screen_width", SourceWidth);
            SVGF.SetInt("screen_height", SourceHeight);
            SVGF.SetInt("TargetWidth", Screen.width);
            SVGF.SetInt("TargetHeight", Screen.height);

            Bloom.SetInt("screen_width", Screen.width);
            Bloom.SetInt("screen_width", Screen.height);

            AutoExpose.SetInt("screen_width", Screen.width);
            AutoExpose.SetInt("screen_height", Screen.height);
            AutoExpose.SetBuffer(AutoExposeKernel, "A", A);
            AutoExpose.SetBuffer(AutoExposeFinalizeKernel, "A", A);

            TAA.SetInt("screen_width", Screen.width);
            TAA.SetInt("screen_height", Screen.height);

            ToneMapLuminanceKernel = ToneMapper.FindKernel("LuminanceShader");
            ToneMapExposureWeightKernel = ToneMapper.FindKernel("ExposureWeightShader");
            ToneMapBlendKernel = ToneMapper.FindKernel("BlendShader");
            ToneMapBlendLapLaceKernel = ToneMapper.FindKernel("BlendLapLaceShader");
            ToneMapCombineKernel = ToneMapper.FindKernel("FinalCombine");

            threadGroupsX = Mathf.CeilToInt(SourceWidth / 16.0f);
            threadGroupsY = Mathf.CeilToInt(SourceHeight / 16.0f);

            threadGroupsX2 = Mathf.CeilToInt(Screen.width / 16.0f);
            threadGroupsY2 = Mathf.CeilToInt(Screen.height / 16.0f);


            InitRenderTexture();
        }

        public void Reinit(Camera Cam, int SourceWidth, int SourceHeight)
        {
            this.SourceWidth = SourceWidth;
            this.SourceHeight = SourceHeight;
            _camera = Cam;

            List<float> TestBuffer = new List<float>();
            TestBuffer.Add(1);
            A?.Dispose(); A = new ComputeBuffer(1, sizeof(float)); A.SetData(TestBuffer);
            SVGF.SetInt("screen_width", SourceWidth);
            SVGF.SetInt("screen_height", SourceHeight);
            SVGF.SetInt("TargetWidth", Screen.width);
            SVGF.SetInt("TargetHeight", Screen.height);

            Bloom.SetInt("screen_width", Screen.width);
            Bloom.SetInt("screen_width", Screen.height);

            AutoExpose.SetInt("screen_width", Screen.width);
            AutoExpose.SetInt("screen_height", Screen.height);
            AutoExpose.SetBuffer(AutoExposeKernel, "A", A);
            AutoExpose.SetBuffer(AutoExposeFinalizeKernel, "A", A);

            TAA.SetInt("screen_width", Screen.width);
            TAA.SetInt("screen_height", Screen.height);

            threadGroupsX = Mathf.CeilToInt(SourceWidth / 16.0f);
            threadGroupsY = Mathf.CeilToInt(SourceHeight / 16.0f);

            threadGroupsX2 = Mathf.CeilToInt(Screen.width / 16.0f);
            threadGroupsY2 = Mathf.CeilToInt(Screen.height / 16.0f);


            InitRenderTexture(true);
        }

        public void ExecuteSVGF(int CurrentSamples, int AtrousKernelSize, ref ComputeBuffer _ColorBuffer, ref RenderTexture _target, ref RenderTexture _Albedo, ref RenderTexture _NormTex, bool DiffRes, RenderTexture PrevDepthTexMain, CommandBuffer cmd, RenderTexture CorrectedDepthTex, bool UseReSTIRGI)
        {
            SVGF.SetBool("UseReSTIRGI", UseReSTIRGI);
            InitRenderTexture();
            SVGF.SetBool("DiffRes", DiffRes);
            Matrix4x4 viewprojmatrix = _camera.projectionMatrix * _camera.worldToCameraMatrix;
            var PrevMatrix = PrevViewProjection;
            SVGF.SetMatrix("viewprojection", viewprojmatrix);
            SVGF.SetMatrix("prevviewprojection", PrevMatrix);
            SVGF.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
            cmd.SetComputeIntParam(SVGF, "Samples_Accumulated", CurrentSamples);
            SVGF.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
            SVGF.SetVector("Forward", _camera.transform.forward);
            PrevViewProjection = viewprojmatrix;
            cmd.SetComputeIntParam(SVGF, "AtrousIterations", AtrousKernelSize);
            bool OddAtrousIteration = (AtrousKernelSize % 2 == 1);
            UnityEngine.Profiling.Profiler.BeginSample("SVGFCopy");
            SVGF.SetBuffer(CopyKernel, "PerPixelRadiance", _ColorBuffer);
            cmd.SetComputeTextureParam(SVGF, CopyKernel, "RWHistoryNormalAndDepth", _HistoryNormalDepth);
            cmd.SetComputeTextureParam(SVGF, CopyKernel, "RWNormalAndDepth", _NormalDepth);
            cmd.SetComputeTextureParam(SVGF, CopyKernel, "RWScreenPosPrev", _ScreenPosPrev);
            cmd.SetComputeTextureParam(SVGF, CopyKernel, "ColorDirectOut", _ColorDirectOut);
            cmd.SetComputeTextureParam(SVGF, CopyKernel, "_Albedo", _Albedo);
            cmd.SetComputeTextureParam(SVGF, CopyKernel, "ColorIndirectOut", _ColorIndirectOut);
            SVGF.SetFloat("FarPlane", _camera.farClipPlane);
            SVGF.SetTextureFromGlobal(CopyKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(SVGF,CopyKernel, "DepthTex", CorrectedDepthTex);
            cmd.SetComputeTextureParam(SVGF,CopyKernel, "PrevDepthTex", PrevDepthTexMain);
            cmd.SetComputeTextureParam(SVGF, CopyKernel, "_CameraNormalDepthTex", _NormTex);
            SVGF.SetTextureFromGlobal(CopyKernel, "NormalTex", "_CameraGBufferTexture2");
             cmd.BeginSample("SVGF Copy");
            cmd.DispatchCompute(SVGF, CopyKernel, threadGroupsX, threadGroupsY, 1);
             cmd.EndSample("SVGF Copy");
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("SVGFReproject");
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "NormalAndDepth", _NormalDepth);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "HistoryNormalAndDepth", _HistoryNormalDepth);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "HistoryDirectTex", _HistoryDirect);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "HistoryIndirectTex", _HistoryIndirect);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "HistoryMomentTex", _HistoryMoment);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "HistoryTex", _History);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "ColorDirectIn", _ColorDirectOut);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "ColorIndirectIn", _ColorIndirectOut);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "ScreenPosPrev", _ScreenPosPrev);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "ColorDirectOut", _ColorDirectIn);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "ColorIndirectOut", _ColorIndirectIn);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "FrameBufferMoment", _FrameMoment);
             cmd.BeginSample("SVGF Reproject");
            cmd.DispatchCompute(SVGF, ReprojectKernel, threadGroupsX, threadGroupsY, 1);
             cmd.EndSample("SVGF Reproject");
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("SVGFVariance");
            cmd.SetComputeTextureParam(SVGF, VarianceKernel, "ColorDirectOut", _ColorDirectOut);
            cmd.SetComputeTextureParam(SVGF, VarianceKernel, "ColorIndirectOut", _ColorIndirectOut);
            cmd.SetComputeTextureParam(SVGF, VarianceKernel, "ColorDirectIn", _ColorDirectIn);
            cmd.SetComputeTextureParam(SVGF, VarianceKernel, "ColorIndirectIn", _ColorIndirectIn);
            cmd.SetComputeTextureParam(SVGF, VarianceKernel, "NormalAndDepth", _NormalDepth);
            cmd.SetComputeTextureParam(SVGF, VarianceKernel, "FrameBufferMoment", _FrameMoment);
            cmd.SetComputeTextureParam(SVGF, VarianceKernel, "HistoryTex", _History);
             cmd.BeginSample("SVGF Var");
            cmd.DispatchCompute(SVGF, VarianceKernel, threadGroupsX, threadGroupsY, 1);
             cmd.EndSample("SVGF Var");
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("SVGFAtrous");
            cmd.SetComputeTextureParam(SVGF, SVGFAtrousKernel, "NormalAndDepth", _NormalDepth);
            cmd.SetComputeTextureParam(SVGF, SVGFAtrousKernel, "HistoryDirectTex", _HistoryDirect);
            cmd.SetComputeTextureParam(SVGF, SVGFAtrousKernel, "HistoryIndirectTex", _HistoryIndirect);
            for (int i = 0; i < AtrousKernelSize; i++)
            {
                int step_size = 1 << i;
                bool UseFlipped = (i % 2 == 1);
                cmd.SetComputeTextureParam(SVGF, SVGFAtrousKernel, "ColorDirectOut", (UseFlipped) ? _ColorDirectIn : _ColorDirectOut);
                cmd.SetComputeTextureParam(SVGF, SVGFAtrousKernel, "ColorIndirectOut", (UseFlipped) ? _ColorIndirectIn : _ColorIndirectOut);
                cmd.SetComputeTextureParam(SVGF, SVGFAtrousKernel, "ColorDirectIn", (UseFlipped) ? _ColorDirectOut : _ColorDirectIn);
                cmd.SetComputeTextureParam(SVGF, SVGFAtrousKernel, "ColorIndirectIn", (UseFlipped) ? _ColorIndirectOut : _ColorIndirectIn);
                var step2 = step_size;
                cmd.SetComputeIntParam(SVGF, "step_size", step2);
                cmd.SetComputeIntParam(SVGF, "iteration", i);
                cmd.BeginSample("SVGF Atrous: " + i);
                cmd.DispatchCompute(SVGF, SVGFAtrousKernel, threadGroupsX, threadGroupsY, 1);
                cmd.EndSample("SVGF Atrous: " + i);
            }
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("SVGFFinalize");
            SVGF.SetBuffer(FinalizeKernel, "PerPixelRadiance", _ColorBuffer);
            SVGF.SetTextureFromGlobal(FinalizeKernel, "DiffuseGBuffer", "_CameraGBufferTexture0");
            SVGF.SetTextureFromGlobal(FinalizeKernel, "SpecularGBuffer", "_CameraGBufferTexture1");
            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "ColorDirectIn", (OddAtrousIteration) ? _ColorDirectOut : _ColorDirectIn);
            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "ColorDirectOut", (OddAtrousIteration) ? _ColorDirectIn : _ColorDirectOut);
            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "NormalAndDepth", _NormalDepth);
            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "ColorIndirectIn", (OddAtrousIteration) ? _ColorIndirectOut : _ColorIndirectIn);
            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "HistoryDirectTex", _HistoryDirect);
            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "HistoryIndirectTex", _HistoryIndirect);
            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "HistoryMomentTex", _HistoryMoment);
            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "RWHistoryNormalAndDepth", _HistoryNormalDepth);
            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "Result", _target);
            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "HistoryTex", _History);
            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "_Albedo", _Albedo);

            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "FrameBufferMoment", _FrameMoment);
                cmd.BeginSample("SVGF Finalize");
            cmd.DispatchCompute(SVGF, FinalizeKernel, threadGroupsX, threadGroupsY, 1);
                cmd.EndSample("SVGF Finalize");
            UnityEngine.Profiling.Profiler.EndSample();
            cmd.CopyTexture(CorrectedDepthTex, PrevDepthTexMain);

        }

        public void ExecuteBloom(ref RenderTexture _target, ref RenderTexture _converged, float BloomStrength, CommandBuffer cmd)
        {//need to fix this so it doesnt create new textures every time

            Bloom.SetFloat("strength", BloomStrength);
            cmd.SetComputeIntParam(Bloom, "screen_width", Screen.width);
            cmd.SetComputeIntParam(Bloom, "screen_height", Screen.height);
            cmd.SetComputeIntParam(Bloom, "TargetWidth", BloomWidths[0]);
            cmd.SetComputeIntParam(Bloom, "TargetHeight", BloomHeights[0]);
            cmd.SetComputeTextureParam(Bloom, BloomLowPassKernel, "InputTex", _converged);
            cmd.SetComputeTextureParam(Bloom, BloomLowPassKernel, "OutputTex", BloomSamples[0]);
            cmd.DispatchCompute(Bloom, BloomLowPassKernel, (int)Mathf.Ceil(BloomWidths[0] / 16.0f), (int)Mathf.Ceil(BloomHeights[0] / 16.0f), 1);
            for (int i = 1; i < 6; i++)
            {
                // Debug.Log(BloomWidths[i]);
                cmd.SetComputeIntParam(Bloom, "TargetWidth", BloomWidths[i]);
                cmd.SetComputeIntParam(Bloom, "TargetHeight", BloomHeights[i]);
                cmd.SetComputeIntParam(Bloom, "screen_width", BloomWidths[i - 1]);
                cmd.SetComputeIntParam(Bloom, "screen_height", BloomHeights[i - 1]);
                cmd.SetComputeTextureParam(Bloom, BloomDownsampleKernel, "InputTex", BloomSamples[i - 1]);
                cmd.SetComputeTextureParam(Bloom, BloomDownsampleKernel, "OutputTex", BloomSamples[i]);
                cmd.DispatchCompute(Bloom, BloomDownsampleKernel, (int)Mathf.Ceil(BloomWidths[i - 1] / 16.0f), (int)Mathf.Ceil(BloomHeights[i - 1] / 16.0f), 1);
            }
            Bloom.SetBool("IsFinal", false);

            for (int i = 5; i > 0; i--)
            {
                cmd.SetComputeIntParam(Bloom, "TargetWidth", BloomWidths[i - 1]);
                cmd.SetComputeIntParam(Bloom, "TargetHeight", BloomHeights[i - 1]);
                cmd.SetComputeIntParam(Bloom, "screen_width", BloomWidths[i]);
                cmd.SetComputeIntParam(Bloom, "screen_height", BloomHeights[i]);
                cmd.SetComputeTextureParam(Bloom, BloomUpsampleKernel, "InputTex", BloomSamples[i]);
                cmd.SetComputeTextureParam(Bloom, BloomUpsampleKernel, "OutputTex", BloomSamples[i - 1]);
                cmd.SetComputeTextureParam(Bloom, BloomUpsampleKernel, "OrigTex", BloomSamples[i - 1]);

                cmd.DispatchCompute(Bloom, BloomUpsampleKernel, (int)Mathf.Ceil(BloomWidths[i - 1] / 16.0f), (int)Mathf.Ceil(BloomHeights[i - 1] / 16.0f), 1);
            }
            cmd.SetComputeIntParam(Bloom, "TargetWidth", Screen.width);
            cmd.SetComputeIntParam(Bloom, "TargetHeight", Screen.height);
            cmd.SetComputeIntParam(Bloom, "screen_width", BloomWidths[0]);
            cmd.SetComputeIntParam(Bloom, "screen_height", BloomHeights[0]);
            Bloom.SetBool("IsFinal", true);
            cmd.SetComputeTextureParam(Bloom, BloomUpsampleKernel, "OrigTex", _converged);
            cmd.SetComputeTextureParam(Bloom, BloomUpsampleKernel, "InputTex", BloomSamples[0]);
            cmd.SetComputeTextureParam(Bloom, BloomUpsampleKernel, "OutputTex", _target);
            cmd.DispatchCompute(Bloom, BloomUpsampleKernel, (int)Mathf.Ceil(Screen.width / 16.0f), (int)Mathf.Ceil(Screen.height / 16.0f), 1);



        }


        public void ExecuteAutoExpose(ref RenderTexture _target, ref RenderTexture _converged, float Exposure, CommandBuffer cmd)
        {//need to fix this so it doesnt create new textures every time
            cmd.SetComputeTextureParam(AutoExpose, AutoExposeKernel, "InTex", _converged);
            AutoExpose.SetFloat("Exposure", Exposure);
            AutoExpose.SetFloat("frame_time", Time.deltaTime);
            cmd.DispatchCompute(AutoExpose, AutoExposeKernel, 1, 1, 1);
            cmd.SetComputeTextureParam(AutoExpose, AutoExposeFinalizeKernel, "InTex", _converged);
            cmd.SetComputeTextureParam(AutoExpose, AutoExposeFinalizeKernel, "OutTex", _target);
            cmd.DispatchCompute(AutoExpose, AutoExposeFinalizeKernel, threadGroupsX2, threadGroupsY2, 1);


        }

        public void ExecuteTAA(ref RenderTexture Input, ref RenderTexture _Final, int CurrentSamples, CommandBuffer cmd)
        {//need to fix this so it doesnt create new textures every time

            cmd.SetComputeIntParam(TAA,"Samples_Accumulated", CurrentSamples);

            UnityEngine.Profiling.Profiler.BeginSample("TAAKernel Prepare");
            TAA.SetFloat("FarPlane", _camera.farClipPlane);
            TAA.SetTextureFromGlobal(TAAPrepareKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            TAA.SetTextureFromGlobal(TAAPrepareKernel, "DepthTex", "_CameraDepthTexture");
            cmd.SetComputeTextureParam(TAA, TAAPrepareKernel, "ColorIn", Input);
            cmd.SetComputeTextureParam(TAA, TAAPrepareKernel, "ColorOut", TempTex);
            cmd.DispatchCompute(TAA, TAAPrepareKernel, threadGroupsX2, threadGroupsY2, 1);
            UnityEngine.Profiling.Profiler.EndSample();


            UnityEngine.Profiling.Profiler.BeginSample("TAAKernel");
            cmd.SetComputeTextureParam(TAA, TAAKernel, "ColorIn", TempTex);
            TAA.SetTextureFromGlobal(TAAKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(TAA, TAAKernel, "TAAPrev", _TAAPrev);
            cmd.SetComputeTextureParam(TAA, TAAKernel, "ColorOut", TempTex2);
            cmd.DispatchCompute(TAA, TAAKernel, threadGroupsX2, threadGroupsY2, 1);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TAAFinalize");
            cmd.SetComputeTextureParam(TAA, TAAFinalizeKernel, "TAAPrev", _TAAPrev);
            cmd.SetComputeTextureParam(TAA, TAAFinalizeKernel, "ColorOut", _Final);
            cmd.SetComputeTextureParam(TAA, TAAFinalizeKernel, "ColorIn", TempTex2);
            cmd.DispatchCompute(TAA, TAAFinalizeKernel, threadGroupsX2, threadGroupsY2, 1);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        Matrix4x4 PreviousCameraMatrix;
        Matrix4x4 PreviousCameraInverseMatrix;
        Matrix4x4 PrevProjInv;

        public void ExecuteUpsample(ref RenderTexture Input, ref RenderTexture Output, int curframe, int cursample, ref RenderTexture ThroughputTex, CommandBuffer cmd)
        {//need to fix this so it doesnt create new textures every time
            UnityEngine.Profiling.Profiler.BeginSample("Upscale");
            cmd.SetComputeIntParam(Upscaler,"curframe", curframe);
            cmd.SetComputeIntParam(Upscaler,"cursam", cursample);
            cmd.SetComputeIntParam(Upscaler,"source_width", Input.width);
            cmd.SetComputeIntParam(Upscaler,"source_height", Input.height);
            cmd.SetComputeIntParam(Upscaler,"target_width", Output.width);
            cmd.SetComputeIntParam(Upscaler,"target_height", Output.height);
            Upscaler.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
            Upscaler.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
            Upscaler.SetMatrix("_PrevCameraToWorld", PreviousCameraMatrix);
            Upscaler.SetMatrix("_PrevCameraInverseProjection", PreviousCameraInverseMatrix);
            Upscaler.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
            Upscaler.SetMatrix("_PrevCameraInverseProjection", PrevProjInv);
            Upscaler.SetVector("Forward", _camera.transform.forward);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "PrevNormalTex", PrevNormalUpscalerTex);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "PrevNormalTexWrite", PrevNormalUpscalerTexWrite);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "PrevWorldPos", PrevWorldPos);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "PrevWorldPosWrite", PrevWorldPosWrite);
            Upscaler.SetVector("CamPos", _camera.transform.position);
            Upscaler.SetMatrix("ViewProjectionMatrix", _camera.projectionMatrix * _camera.worldToCameraMatrix);
            Upscaler.SetFloat("FarPlane", _camera.farClipPlane);
            Upscaler.SetTextureFromGlobal(UpsampleKernel, "Albedo", "_CameraGBufferTexture0");
            Upscaler.SetTextureFromGlobal(UpsampleKernel, "Albedo2", "_CameraGBufferTexture1");
            Upscaler.SetTextureFromGlobal(UpsampleKernel, "DepthTex", "_CameraDepthTexture");
            // Upscaler.SetTextureFromGlobal(UpsampleKernel, "PrevDepthTex", "_LastCameraDepthTexture");
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "PrevDepthTex", PrevDepthTex);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "ThroughputTex", ThroughputTex);
            Upscaler.SetTextureFromGlobal(UpsampleKernel, "NormalTex", "_CameraGBufferTexture2");
            Upscaler.SetTextureFromGlobal(UpsampleKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "Input", Input);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "Output", UpScalerLightingDataTexture);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "FinalOutput", Output);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "PrevOutput", PrevOutputTex);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "PrevDepthTexWrite", TempTex);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "TAAPrev", PrevUpscalerTAA);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "TAAPrevWrite", TempTex2);
            cmd.DispatchCompute(Upscaler, UpsampleKernel, threadGroupsX2, threadGroupsY2, 1);
            UnityEngine.Profiling.Profiler.EndSample();
            cmd.CopyTexture(TempTex, PrevDepthTex);
            cmd.CopyTexture(UpScalerLightingDataTexture, PrevOutputTex);
            cmd.CopyTexture(TempTex2, PrevUpscalerTAA);
            cmd.CopyTexture(PrevNormalUpscalerTexWrite, PrevNormalUpscalerTex);
            cmd.CopyTexture(PrevWorldPosWrite, PrevWorldPos);
            PreviousCameraMatrix = _camera.cameraToWorldMatrix;
            PreviousCameraInverseMatrix = _camera.projectionMatrix.inverse;
            //PrevDepthTex = Shader.GetGlobalTexture("_LastCameraDepthTexture");
            PrevProjInv = _camera.projectionMatrix.inverse;
        }


        public void ExecuteToneMap(ref RenderTexture Output, CommandBuffer cmd, ref Texture3D LUT)
        {//need to fix this so it doesnt create new textures every time
            cmd.SetComputeIntParam(ToneMapper,"width", Output.width);
            cmd.SetComputeIntParam(ToneMapper,"height", Output.height);
            cmd.SetComputeIntParam(ToneMapper,"ScreenWidth", Output.width);
            cmd.SetComputeIntParam(ToneMapper,"ScreenHeight", Output.height);
            cmd.SetComputeTextureParam(ToneMapper, 0, "Result", Output);
            cmd.SetComputeTextureParam(ToneMapper, 0, "LUT", LUT);
            cmd.DispatchCompute(ToneMapper, 0, threadGroupsX2, threadGroupsY2, 1);

            // cmd.// SetComputeTextureParam(ToneMapper, ToneMapLuminanceKernel, "Result", Output);
            // cmd.// SetComputeTextureParam(ToneMapper, ToneMapLuminanceKernel, "LuminanceTex", mips[0]);
            // // ToneMapper.Dispatch(ToneMapLuminanceKernel, (int)Mathf.Ceil(mips[0].width / 16.0f), (int)Mathf.Ceil(mips[0].height / 16.0f), 1);
            // cmd.// SetComputeTextureParam(ToneMapper, ToneMapExposureWeightKernel, "DiffuseTex", mips[0]);
            // cmd.// SetComputeTextureParam(ToneMapper, ToneMapExposureWeightKernel, "Result", mipsWeights[0]);
            // // ToneMapper.Dispatch(ToneMapExposureWeightKernel, (int)Mathf.Ceil(mips[0].width / 16.0f), (int)Mathf.Ceil(mips[0].height / 16.0f), 1);
            // // for (int i = 0; i < mips.Length - 1; i++)
            // // {
            // //     cmd.SetComputeIntParam(Bloom, "TargetWidth", mips[i + 1].width);
            // //     cmd.SetComputeIntParam(Bloom, "TargetHeight", mips[i + 1].height);
            // //     cmd.SetComputeIntParam(Bloom, "screen_width", mips[i].width);
            // //     cmd.SetComputeIntParam(Bloom, "screen_height", mips[i].height);
            // cmd.//     SetComputeTextureParam(Bloom, BloomDownsampleKernel, "InputTex", mips[i]);
            // cmd.//     SetComputeTextureParam(Bloom, BloomDownsampleKernel, "OutputTex", mips[i + 1]);
            // //     cmd.DispatchCompute(Bloom, BloomDownsampleKernel, (int)Mathf.Ceil(mips[i + 1].width / 16.0f), (int)Mathf.Ceil(mips[i + 1].height / 16.0f), 1);
            // cmd.//     SetComputeTextureParam(Bloom, BloomDownsampleKernel, "InputTex", mipsWeights[i]);
            // cmd.//     SetComputeTextureParam(Bloom, BloomDownsampleKernel, "OutputTex", mipsWeights[i + 1]);
            // //     cmd.DispatchCompute(Bloom, BloomDownsampleKernel, (int)Mathf.Ceil(mips[i + 1].width / 16.0f), (int)Mathf.Ceil(mips[i + 1].height / 16.0f), 1);
            // // }
            // cmd.// SetComputeTextureParam(ToneMapper, ToneMapBlendKernel, "tWeights", mipsWeights[mipsWeights.Length - 1]);
            // cmd.// SetComputeTextureParam(ToneMapper, ToneMapBlendKernel, "tExposures", mips[mipsWeights.Length - 1]);
            // cmd.// SetComputeTextureParam(ToneMapper, ToneMapBlendKernel, "Result", mipsAssemble[mipsWeights.Length - 1]);
            // // ToneMapper.Dispatch(ToneMapBlendKernel, (int)Mathf.Ceil(mips[mipsWeights.Length - 1].width / 16.0f), (int)Mathf.Ceil(mips[mipsWeights.Length - 1].height / 16.0f), 1);
            // // for (int i = mips.Length - 1; i > 0; i--) {
            // //     cmd.SetComputeIntParam(ToneMapper,"ScreenWidth", mips[i - 1].width);
            // //     cmd.SetComputeIntParam(ToneMapper,"ScreenHeight", mips[i - 1].height);
            // //     // Blend the finer levels - Laplacians.
            // cmd.//     SetComputeTextureParam(ToneMapper, ToneMapBlendLapLaceKernel, "tExposures", mips[i - 1]);
            // cmd.//     SetComputeTextureParam(ToneMapper, ToneMapBlendLapLaceKernel, "tExposuresCoarser", mips[i]);
            // cmd.//     SetComputeTextureParam(ToneMapper, ToneMapBlendLapLaceKernel, "tWeights", mipsWeights[i - 1]);
            // cmd.//     SetComputeTextureParam(ToneMapper, ToneMapBlendLapLaceKernel, "tAccumSoFar", mipsAssemble[i]);
            // cmd.//     SetComputeTextureParam(ToneMapper, ToneMapBlendLapLaceKernel, "Result", mipsAssemble[i - 1]);
            // //     ToneMapper.Dispatch(ToneMapBlendLapLaceKernel, (int)Mathf.Ceil(mips[i - 1].width / 16.0f), (int)Mathf.Ceil(mips[i - 1].height / 16.0f), 1);
            // // }
            // //     cmd.SetComputeIntParam(ToneMapper,"ScreenWidth", mips[0].width);
            // //     cmd.SetComputeIntParam(ToneMapper,"ScreenHeight", mips[0].height);
            // cmd.// SetComputeTextureParam(ToneMapper, ToneMapCombineKernel, "tOrigionalMip", mips[1]);
            // cmd.// SetComputeTextureParam(ToneMapper, ToneMapCombineKernel, "tOrigional", Output);
            // cmd.// SetComputeTextureParam(ToneMapper, ToneMapCombineKernel, "DiffuseTex", mipsAssemble[0]);
            // cmd.// SetComputeTextureParam(ToneMapper, ToneMapCombineKernel, "Result", Output);
            // ToneMapper.Dispatch(ToneMapCombineKernel, (int)Mathf.Ceil(Output.width / 16.0f), (int)Mathf.Ceil(Output.height / 16.0f), 1);
        }

        public void ExecuteTAAU(ref RenderTexture Output, ref RenderTexture Input, ref RenderTexture ThroughputTex, CommandBuffer cmd, int CurFrame, RenderTexture CorrectedDepthTex)
        {//need to fix this so it doesnt create new textures every time
            bool IsEven = CurFrame % 2 == 0;
            cmd.SetComputeIntParam(TAAU,"source_width", SourceWidth);
            cmd.SetComputeIntParam(TAAU,"source_height", SourceHeight);
            cmd.SetComputeIntParam(TAAU,"target_width", Output.width);
            cmd.SetComputeIntParam(TAAU,"target_height", Output.height);
            cmd.SetComputeIntParam(TAAU,"CurFrame", CurFrame);
            cmd.SetComputeTextureParam(TAAU, TAAUKernel, "IMG_ASVGF_TAA_A", IsEven ? TAAA : TAAB);
            cmd.SetComputeTextureParam(TAAU, TAAUKernel, "TEX_ASVGF_TAA_B", !IsEven ? TAAA : TAAB);
            cmd.SetComputeTextureParam(TAAU, TAAUKernel, "TEX_FLAT_COLOR", Input);
            cmd.SetComputeTextureParam(TAAU, TAAUKernel, "ThroughputTex", ThroughputTex);
            cmd.SetComputeTextureParam(TAAU, TAAUKernel, "IMG_TAA_OUTPUT", Output);
            cmd.SetComputeTextureParam(TAAU, TAAUKernel, "CorrectedDepthTex", CorrectedDepthTex);
            TAAU.SetTextureFromGlobal(TAAUKernel, "Albedo", "_CameraGBufferTexture0");
            TAAU.SetTextureFromGlobal(TAAUKernel, "Albedo2", "_CameraGBufferTexture1");
            TAAU.SetTextureFromGlobal(TAAUKernel, "TEX_FLAT_MOTION", "_CameraMotionVectorsTexture");
            cmd.DispatchCompute(TAAU, TAAUKernel, threadGroupsX2, threadGroupsY2, 1);
        }

    }
}

