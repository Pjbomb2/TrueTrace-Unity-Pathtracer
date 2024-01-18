using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CommonVars;

namespace TrueTrace {
    [System.Serializable]
    public class Denoiser
    {
        public bool Initialized = false;
        private ComputeShader SVGF;
        private ComputeShader Bloom;
        private ComputeShader AutoExpose;
        private ComputeShader TAA;
        private ComputeShader Upscaler;
        private ComputeShader ToneMapper;
        private ComputeShader TAAU;
        bool BloomInitialized = false;
        bool TAAInitialized = false;
        bool UpscalerInitialized = false;
        bool TAAUInitialized = false;


        public RenderTexture DirectA;
        public RenderTexture DirectB;
        public RenderTexture DirectC;

        public RenderTexture IndirectA;
        public RenderTexture IndirectB;
        public RenderTexture IndirectC;


        public RenderTexture MomentA;
        public RenderTexture MomentB;
        public RenderTexture HistoryTex;














        public RenderTexture _TAAPrev;
        public RenderTexture Intermediate;
        public RenderTexture SuperIntermediate;
        public RenderTexture PrevDepthTex;
        public RenderTexture PrevOutputTex;
        private RenderTexture PrevUpscalerTAA;
        private RenderTexture UpScalerLightingDataTexture;
        private RenderTexture PrevNormalUpscalerTex;
        private RenderTexture PrevNormalUpscalerTexWrite;

        private RenderTexture TempTexTAA;
        private RenderTexture TempTexTAA2;

        private RenderTexture TempTexUpscaler;
        private RenderTexture TempTexUpscaler2;





        private RenderTexture TAAA;
        private RenderTexture TAAB;
        public RenderTexture[] BloomSamplesUp;
        public RenderTexture[] BloomSamplesDown;
        public RenderTexture BloomIntermediate;

        private int ToneMapLuminanceKernel;
        private int ToneMapExposureWeightKernel;
        private int ToneMapBlendKernel;
        private int ToneMapBlendLapLaceKernel;
        private int ToneMapCombineKernel;




        public ComputeBuffer ExposureBuffer;

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

        public bool SVGFInitialized;
        public void ClearSVGF()
        {
            DirectA.ReleaseSafe();
            DirectB.ReleaseSafe();
            DirectC.ReleaseSafe();
            IndirectA.ReleaseSafe();
            IndirectB.ReleaseSafe();
            IndirectC.ReleaseSafe();
            MomentA.ReleaseSafe();
            MomentB.ReleaseSafe();
            HistoryTex.ReleaseSafe();

            SVGFInitialized = false;
        }
        public void InitSVGF(int SourceWidth, int SourceHeight)
        {

            this.SourceWidth = SourceWidth;
            this.SourceHeight = SourceHeight;
            CommonFunctions.CreateRenderTexture(ref DirectA, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref DirectB, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref DirectC, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref IndirectA, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref IndirectB, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref IndirectC, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref MomentA, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref MomentB, SourceWidth, SourceHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref HistoryTex, SourceWidth, SourceHeight, CommonFunctions.RTHalf1);
            SVGFInitialized = true;
        }
        private void InitRenderTexture(bool Force = false)
        {
            if(Force) {
                    PrevOutputTex.ReleaseSafe();
                    PrevUpscalerTAA.ReleaseSafe();
                    TAAA.ReleaseSafe();
                    TAAB.ReleaseSafe();
                    _TAAPrev.ReleaseSafe();
                    TempTexTAA.ReleaseSafe();
                    TempTexTAA2.ReleaseSafe();
                    TempTexUpscaler.ReleaseSafe();
                    TempTexUpscaler2.ReleaseSafe();
                    PrevNormalUpscalerTex.ReleaseSafe();
                    PrevNormalUpscalerTexWrite.ReleaseSafe();
                BloomInitialized = false;
                TAAInitialized = false;
                UpscalerInitialized = false;
            }
            
        }
        public void ClearAll() {
            ClearSVGF();
            _TAAPrev.ReleaseSafe();
            Intermediate.ReleaseSafe();
            SuperIntermediate.ReleaseSafe();
            PrevDepthTex.ReleaseSafe();
            PrevOutputTex.ReleaseSafe();
            PrevUpscalerTAA.ReleaseSafe();
            UpScalerLightingDataTexture.ReleaseSafe();
            PrevNormalUpscalerTex.ReleaseSafe();
            PrevNormalUpscalerTexWrite.ReleaseSafe();

            TempTexTAA.ReleaseSafe();
            TempTexTAA2.ReleaseSafe();
            TempTexUpscaler.ReleaseSafe();
            TempTexUpscaler2.ReleaseSafe();

            TAAA.ReleaseSafe();
            TAAB.ReleaseSafe();

            ExposureBuffer.ReleaseSafe();
            BloomIntermediate.ReleaseSafe();
            if(BloomSamplesUp != null) for(int i = 0; i < BloomSamplesUp.Length; i++) BloomSamplesUp[i].ReleaseSafe();
            if(BloomSamplesDown != null) for(int i = 0; i < BloomSamplesDown.Length; i++) BloomSamplesDown[i].ReleaseSafe();

        }

        void OnApplicationQuit()
        {
            ClearAll();
        }

        public void init(int SourceWidth, int SourceHeight)
        {
            this.SourceWidth = SourceWidth;
            this.SourceHeight = SourceHeight;
            SVGFInitialized = false;

            _camera = RayTracingMaster._camera;
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
            ExposureBuffer?.Release(); ExposureBuffer = new ComputeBuffer(1, sizeof(float)); ExposureBuffer.SetData(TestBuffer);
            SVGF.SetInt("screen_width", SourceWidth);
            SVGF.SetInt("screen_height", SourceHeight);
            SVGF.SetInt("TargetWidth", Screen.width);
            SVGF.SetInt("TargetHeight", Screen.height);

            Bloom.SetInt("screen_width", Screen.width);
            Bloom.SetInt("screen_width", Screen.height);

            AutoExpose.SetInt("screen_width", Screen.width);
            AutoExpose.SetInt("screen_height", Screen.height);
            AutoExpose.SetBuffer(AutoExposeKernel, "A", ExposureBuffer);
            AutoExpose.SetBuffer(AutoExposeFinalizeKernel, "A", ExposureBuffer);

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

            BloomInitialized = false;
            TAAInitialized = false;
            UpscalerInitialized = false;
            InitRenderTexture();
            Initialized = true;
        }

        public void Reinit(int SourceWidth, int SourceHeight)
        {
            this.SourceWidth = SourceWidth;
            this.SourceHeight = SourceHeight;
            _camera = RayTracingMaster._camera;

            List<float> TestBuffer = new List<float>();
            TestBuffer.Add(1);
            ExposureBuffer?.Release(); ExposureBuffer = new ComputeBuffer(1, sizeof(float)); ExposureBuffer.SetData(TestBuffer);
            SVGF.SetInt("screen_width", SourceWidth);
            SVGF.SetInt("screen_height", SourceHeight);
            SVGF.SetInt("TargetWidth", Screen.width);
            SVGF.SetInt("TargetHeight", Screen.height);

            Bloom.SetInt("screen_width", Screen.width);
            Bloom.SetInt("screen_width", Screen.height);

            AutoExpose.SetInt("screen_width", Screen.width);
            AutoExpose.SetInt("screen_height", Screen.height);
            AutoExpose.SetBuffer(AutoExposeKernel, "A", ExposureBuffer);
            AutoExpose.SetBuffer(AutoExposeFinalizeKernel, "A", ExposureBuffer);

            TAA.SetInt("screen_width", Screen.width);
            TAA.SetInt("screen_height", Screen.height);

            threadGroupsX = Mathf.CeilToInt(SourceWidth / 16.0f);
            threadGroupsY = Mathf.CeilToInt(SourceHeight / 16.0f);

            threadGroupsX2 = Mathf.CeilToInt(Screen.width / 16.0f);
            threadGroupsY2 = Mathf.CeilToInt(Screen.height / 16.0f);


            InitRenderTexture(true);
        }

        public void ValidateInit(bool BloomInit, bool TAAInit, bool IsUpscaling, bool UseTAAU) {
            if(!BloomInit) {
                if(BloomInitialized) {
                    BloomIntermediate.ReleaseSafe();
                    if(BloomSamplesUp != null) for(int i = 0; i < BloomSamplesUp.Length; i++) BloomSamplesUp[i].ReleaseSafe();
                    if(BloomSamplesDown != null) for(int i = 0; i < BloomSamplesDown.Length; i++) BloomSamplesDown[i].ReleaseSafe();
                    BloomInitialized = false;
                }
            }
            if(!TAAInit) {
                if(TAAInitialized) {
                    _TAAPrev.ReleaseSafe();
                    TempTexTAA.ReleaseSafe();
                    TempTexTAA2.ReleaseSafe();
                    TAAInitialized = false;
                }
            }
            if(!IsUpscaling) {
                if(!UseTAAU) {
                    if(UpscalerInitialized) {
                        PrevNormalUpscalerTex.ReleaseSafe();
                        PrevNormalUpscalerTexWrite.ReleaseSafe();
                        PrevDepthTex.ReleaseSafe();
                        UpScalerLightingDataTexture.ReleaseSafe();
                        PrevOutputTex.ReleaseSafe();
                        TempTexUpscaler.ReleaseSafe();
                        TempTexUpscaler2.ReleaseSafe();
                        PrevUpscalerTAA.ReleaseSafe();
                        UpscalerInitialized = false;
                    }
                } else {
                    if(TAAUInitialized) {
                        TAAA.ReleaseSafe();
                        TAAB.ReleaseSafe();
                        TAAUInitialized = false;
                    }
                }
            }
        }

        public void ExecuteSVGF(int CurrentSamples, 
                                int AtrousKernelSize, 
                                ref ComputeBuffer _ColorBuffer, 
                                ref RenderTexture _target, 
                                ref RenderTexture _Albedo, 
                                RenderTexture ScreenSpaceInfo, 
                                RenderTexture PrevScreenSpaceInfo, 
                                RenderTexture WorldPosData, 
                                bool DiffRes, 
                                bool UseReSTIRGI,
                                CommandBuffer cmd) 
        {
            SVGF.SetBool("UseReSTIRGI", UseReSTIRGI);
            InitRenderTexture();
            SVGF.SetBool("DiffRes", DiffRes);
            bool OddAtrousIteration = (AtrousKernelSize % 2 == 1);
            bool Flipper = CurrentSamples % 2 == 0;
            SVGF.SetBuffer(CopyKernel, "PerPixelRadiance", _ColorBuffer);
            cmd.SetComputeIntParam(SVGF, "AtrousIterations", AtrousKernelSize);
            cmd.SetComputeTextureParam(SVGF, CopyKernel, "TempAlbedoTex", _Albedo);
            cmd.SetComputeTextureParam(SVGF, CopyKernel, "WorldPosData", WorldPosData);
            cmd.SetComputeTextureParam(SVGF, CopyKernel, "IndirectA", IndirectA);
            cmd.SetComputeTextureParam(SVGF, CopyKernel, "DirectA", DirectA);
            cmd.BeginSample("SVGF Copy Kernel");
            cmd.DispatchCompute(SVGF, CopyKernel, threadGroupsX, threadGroupsY, 1);
            cmd.EndSample("SVGF Copy Kernel");




            SVGF.SetTextureFromGlobal(ReprojectKernel, "MotionVectors", "_CameraMotionVectorsTexture");

            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "DirectA", DirectA);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "DirectB", DirectB);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "IndirectA", IndirectA);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "IndirectB", IndirectB);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "MomentA", Flipper ? MomentA : MomentB);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "MomentB", Flipper ? MomentB : MomentA);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "HistoryTex", HistoryTex);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "ScreenSpaceInfo", ScreenSpaceInfo);
            cmd.SetComputeTextureParam(SVGF, ReprojectKernel, "PrevScreenSpaceInfo", PrevScreenSpaceInfo);
            cmd.BeginSample("SVGF Reproject Kernel");
            cmd.DispatchCompute(SVGF, ReprojectKernel, threadGroupsX, threadGroupsY, 1);
            cmd.EndSample("SVGF Reproject Kernel");  

            cmd.SetComputeTextureParam(SVGF, VarianceKernel, "ScreenSpaceInfo", ScreenSpaceInfo);
            cmd.SetComputeTextureParam(SVGF, VarianceKernel, "HistoryTex", HistoryTex);
            cmd.SetComputeTextureParam(SVGF, VarianceKernel, "DirectA", DirectB);
            cmd.SetComputeTextureParam(SVGF, VarianceKernel, "DirectB", DirectA);
            cmd.SetComputeTextureParam(SVGF, VarianceKernel, "IndirectA", IndirectB);
            cmd.SetComputeTextureParam(SVGF, VarianceKernel, "IndirectB", IndirectA);//Current frame is stored in BTex, previous frame is erased
            cmd.SetComputeTextureParam(SVGF, VarianceKernel, "MomentA", Flipper ? MomentB : MomentA);//Current frame is stored in BTex, previous frame is erased
            cmd.SetComputeTextureParam(SVGF, VarianceKernel, "MomentB", Flipper ? MomentA : MomentB);//Need to be flipflopping moment
            cmd.BeginSample("SVGF Variance Kernel");
            cmd.DispatchCompute(SVGF, VarianceKernel, threadGroupsX, threadGroupsY, 1);
            cmd.EndSample("SVGF Variance Kernel");            


            cmd.SetComputeTextureParam(SVGF, SVGFAtrousKernel, "ScreenSpaceInfo", ScreenSpaceInfo);

            for (int i = 0; i < AtrousKernelSize; i++)
            {
                int step_size = 1 << i;
                bool UseFlipped = (i % 2 == 0);
                cmd.SetComputeTextureParam(SVGF, SVGFAtrousKernel, "DirectA", (UseFlipped) ? DirectC : DirectA);
                cmd.SetComputeTextureParam(SVGF, SVGFAtrousKernel, "DirectB", (UseFlipped) ? DirectA : DirectC);
                cmd.SetComputeTextureParam(SVGF, SVGFAtrousKernel, "IndirectA", (UseFlipped) ? IndirectC : IndirectA);
                cmd.SetComputeTextureParam(SVGF, SVGFAtrousKernel, "IndirectB", (UseFlipped) ? IndirectA : IndirectC);
                var step2 = step_size;
                cmd.SetComputeIntParam(SVGF, "step_size", step2);
                cmd.BeginSample("SVGF Atrous Kernel: " + i);
                cmd.DispatchCompute(SVGF, SVGFAtrousKernel, threadGroupsX, threadGroupsY, 1);
                cmd.EndSample("SVGF Atrous Kernel: " + i);
                if(step2 == 1) {
                    cmd.CopyTexture(DirectC, DirectB);
                    cmd.CopyTexture(IndirectC, IndirectB);
                }
            }

            SVGF.SetBuffer(FinalizeKernel, "PerPixelRadiance", _ColorBuffer);
            SVGF.SetTextureFromGlobal(FinalizeKernel, "DiffuseGBuffer", "_CameraGBufferTexture0");
            SVGF.SetTextureFromGlobal(FinalizeKernel, "SpecularGBuffer", "_CameraGBufferTexture1");
            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "DirectB", (OddAtrousIteration) ? DirectC : DirectA);
            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "IndirectB", (OddAtrousIteration) ? IndirectC : IndirectA);
            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "Result", _target);
            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "WorldPosData", WorldPosData);
            cmd.SetComputeTextureParam(SVGF, FinalizeKernel, "TempAlbedoTex", _Albedo);
            cmd.BeginSample("SVGF Finalize Kernel");
            cmd.DispatchCompute(SVGF, FinalizeKernel, threadGroupsX, threadGroupsY, 1);
            cmd.EndSample("SVGF Finalize Kernel");

        }


        private void InitBloom() {
            BloomSamplesUp = new RenderTexture[8];
            BloomSamplesDown = new RenderTexture[8];
            int BloomWidth = Screen.width / 2;
            int BloomHeight = Screen.height / 2;
            BloomWidths = new int[8];
            BloomHeights = new int[8];
            for (int i = 0; i < 8; i++)
            {
                CommonFunctions.CreateRenderTexture(ref BloomSamplesUp[i], BloomWidth, BloomHeight, CommonFunctions.RTHalf4);
                CommonFunctions.CreateRenderTexture(ref BloomSamplesDown[i], BloomWidth, BloomHeight, CommonFunctions.RTHalf4);
                BloomWidths[i] = BloomWidth;
                BloomHeights[i] = BloomHeight;
                BloomWidth /= 2;
                BloomHeight /= 2;
            }
            CommonFunctions.CreateRenderTexture(ref BloomIntermediate, Screen.width, Screen.height, CommonFunctions.RTFull4);
            BloomInitialized = true;
        }
        public void ExecuteBloom(ref RenderTexture _converged, float BloomStrength, CommandBuffer cmd)
        {//need to fix this so it doesnt create new textures every time
            if(!BloomInitialized) InitBloom();
            cmd.BeginSample("Bloom");
            Bloom.SetFloat("strength", BloomStrength);
            cmd.SetComputeIntParam(Bloom, "screen_width", Screen.width);
            cmd.SetComputeIntParam(Bloom, "screen_height", Screen.height);
            cmd.SetComputeIntParam(Bloom, "TargetWidth", BloomWidths[0]);
            cmd.SetComputeIntParam(Bloom, "TargetHeight", BloomHeights[0]);
            cmd.SetComputeTextureParam(Bloom, BloomLowPassKernel, "InputTex", _converged);
            cmd.SetComputeTextureParam(Bloom, BloomLowPassKernel, "OutputTex", BloomSamplesDown[0]);
            cmd.DispatchCompute(Bloom, BloomLowPassKernel, (int)Mathf.Ceil(BloomWidths[0] / 16.0f), (int)Mathf.Ceil(BloomHeights[0] / 16.0f), 1);
            for (int i = 1; i < 6; i++)
            {
                // Debug.Log(BloomWidths[i]);
                cmd.SetComputeIntParam(Bloom, "TargetWidth", BloomWidths[i]);
                cmd.SetComputeIntParam(Bloom, "TargetHeight", BloomHeights[i]);
                cmd.SetComputeIntParam(Bloom, "screen_width", BloomWidths[i - 1]);
                cmd.SetComputeIntParam(Bloom, "screen_height", BloomHeights[i - 1]);
                cmd.SetComputeTextureParam(Bloom, BloomDownsampleKernel, "InputTex", BloomSamplesDown[i - 1]);
                cmd.SetComputeTextureParam(Bloom, BloomDownsampleKernel, "OutputTex", BloomSamplesDown[i]);
                cmd.DispatchCompute(Bloom, BloomDownsampleKernel, (int)Mathf.Ceil(BloomWidths[i - 1] / 16.0f), (int)Mathf.Ceil(BloomHeights[i - 1] / 16.0f), 1);
            }
            Bloom.SetBool("IsFinal", false);

            for (int i = 5; i > 0; i--)
            {
                cmd.SetComputeIntParam(Bloom, "TargetWidth", BloomWidths[i - 1]);
                cmd.SetComputeIntParam(Bloom, "TargetHeight", BloomHeights[i - 1]);
                cmd.SetComputeIntParam(Bloom, "screen_width", BloomWidths[i]);
                cmd.SetComputeIntParam(Bloom, "screen_height", BloomHeights[i]);
                cmd.SetComputeTextureParam(Bloom, BloomUpsampleKernel, "InputTex", BloomSamplesDown[i]);
                cmd.SetComputeTextureParam(Bloom, BloomUpsampleKernel, "OutputTex", BloomSamplesUp[i - 1]);
                cmd.SetComputeTextureParam(Bloom, BloomUpsampleKernel, "OrigTex", BloomSamplesDown[i - 1]);

                cmd.DispatchCompute(Bloom, BloomUpsampleKernel, (int)Mathf.Ceil(BloomWidths[i - 1] / 16.0f), (int)Mathf.Ceil(BloomHeights[i - 1] / 16.0f), 1);
            }
            cmd.Blit(_converged, BloomIntermediate);
            cmd.SetComputeIntParam(Bloom, "TargetWidth", Screen.width);
            cmd.SetComputeIntParam(Bloom, "TargetHeight", Screen.height);
            cmd.SetComputeIntParam(Bloom, "screen_width", BloomWidths[0]);
            cmd.SetComputeIntParam(Bloom, "screen_height", BloomHeights[0]);
            Bloom.SetBool("IsFinal", true);
            cmd.SetComputeTextureParam(Bloom, BloomUpsampleKernel, "OrigTex", BloomIntermediate);
            cmd.SetComputeTextureParam(Bloom, BloomUpsampleKernel, "InputTex", BloomSamplesUp[0]);
            cmd.SetComputeTextureParam(Bloom, BloomUpsampleKernel, "OutputTex", _converged);
            cmd.DispatchCompute(Bloom, BloomUpsampleKernel, (int)Mathf.Ceil(Screen.width / 16.0f), (int)Mathf.Ceil(Screen.height / 16.0f), 1);
            cmd.EndSample("Bloom");



        }


        public void ExecuteAutoExpose(ref RenderTexture _converged, float Exposure, CommandBuffer cmd, bool ExposureAuto)
        {//need to fix this so it doesnt create new textures every time
            cmd.BeginSample("Auto Exposure");
            if(ExposureBuffer == null) {
                List<float> TestBuffer = new List<float>();
                TestBuffer.Add(1);
                ExposureBuffer.ReleaseSafe(); ExposureBuffer = new ComputeBuffer(1, sizeof(float)); ExposureBuffer.SetData(TestBuffer);
                AutoExpose.SetBuffer(AutoExposeKernel, "A", ExposureBuffer);
                AutoExpose.SetBuffer(AutoExposeFinalizeKernel, "A", ExposureBuffer);
            }
            cmd.SetComputeTextureParam(AutoExpose, AutoExposeKernel, "InTex", _converged);
            AutoExpose.SetFloat("Exposure", Exposure);
            AutoExpose.SetBool("ExposureAuto", ExposureAuto);
            AutoExpose.SetFloat("frame_time", Time.deltaTime);
            cmd.DispatchCompute(AutoExpose, AutoExposeKernel, 1, 1, 1);
            cmd.SetComputeTextureParam(AutoExpose, AutoExposeFinalizeKernel, "OutTex", _converged);
            cmd.DispatchCompute(AutoExpose, AutoExposeFinalizeKernel, threadGroupsX2, threadGroupsY2, 1);
            cmd.EndSample("Auto Exposure");


        }
        private void InitializeTAA() {
            CommonFunctions.CreateRenderTexture(ref TempTexTAA, Screen.width, Screen.height, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref TempTexTAA2, Screen.width, Screen.height, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref _TAAPrev, Screen.width, Screen.height, CommonFunctions.RTHalf4);
            TAAInitialized = true;
        }
        public void ExecuteTAA(ref RenderTexture _Final, int CurrentSamples, CommandBuffer cmd)
        {//need to fix this so it doesnt create new textures every time

            if(!TAAInitialized) InitializeTAA();
            cmd.SetComputeIntParam(TAA,"Samples_Accumulated", CurrentSamples);

            TAA.SetFloat("FarPlane", _camera.farClipPlane);
            TAA.SetTextureFromGlobal(TAAPrepareKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            TAA.SetTextureFromGlobal(TAAPrepareKernel, "DepthTex", "_CameraDepthTexture");
            cmd.SetComputeTextureParam(TAA, TAAPrepareKernel, "ColorIn", _Final);
            cmd.SetComputeTextureParam(TAA, TAAPrepareKernel, "ColorOut", TempTexTAA);
            cmd.BeginSample("TAA Prepare Kernel");
            cmd.DispatchCompute(TAA, TAAPrepareKernel, threadGroupsX2, threadGroupsY2, 1);
            cmd.EndSample("TAA Prepare Kernel");


            cmd.SetComputeTextureParam(TAA, TAAKernel, "ColorIn", TempTexTAA);
            TAA.SetTextureFromGlobal(TAAKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(TAA, TAAKernel, "TAAPrev", _TAAPrev);
            cmd.SetComputeTextureParam(TAA, TAAKernel, "TAAPrevRead", _TAAPrev);
            cmd.SetComputeTextureParam(TAA, TAAKernel, "ColorOut", TempTexTAA2);
            cmd.BeginSample("TAA Main Kernel");
            cmd.DispatchCompute(TAA, TAAKernel, threadGroupsX2, threadGroupsY2, 1);
            cmd.EndSample("TAA Main Kernel");

            cmd.SetComputeTextureParam(TAA, TAAFinalizeKernel, "TAAPrev", _TAAPrev);
            cmd.SetComputeTextureParam(TAA, TAAFinalizeKernel, "ColorOut", _Final);
            cmd.SetComputeTextureParam(TAA, TAAFinalizeKernel, "ColorIn", TempTexTAA2);
            cmd.BeginSample("TAA Finalize Kernel");
            cmd.DispatchCompute(TAA, TAAFinalizeKernel, threadGroupsX2, threadGroupsY2, 1);
            cmd.EndSample("TAA Finalize Kernel");
        }

        Matrix4x4 PreviousCameraMatrix;
        Matrix4x4 PreviousCameraInverseMatrix;
        Matrix4x4 PrevProjInv;
        private void InitializeUpsampler() {
            CommonFunctions.CreateRenderTexture(ref PrevNormalUpscalerTex, Screen.width, Screen.height, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref PrevNormalUpscalerTexWrite, Screen.width, Screen.height, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref PrevDepthTex, Screen.width, Screen.height, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref UpScalerLightingDataTexture, Screen.width, Screen.height, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref PrevOutputTex, Screen.width, Screen.height, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref TempTexUpscaler, Screen.width, Screen.height, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref TempTexUpscaler2, Screen.width, Screen.height, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref PrevUpscalerTAA, Screen.width, Screen.height, CommonFunctions.RTHalf4);
            UpscalerInitialized = true;
        }
        public void ExecuteUpsample(ref RenderTexture Input, ref RenderTexture Output, int curframe, int cursample, ref RenderTexture ThroughputTex, CommandBuffer cmd, RenderTexture ScreenSpaceInfo)
        {//need to fix this so it doesnt create new textures every time
            if(!UpscalerInitialized) InitializeUpsampler();
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
            Upscaler.SetVector("CamPos", _camera.transform.position);
            Upscaler.SetMatrix("ViewProjectionMatrix", _camera.projectionMatrix * _camera.worldToCameraMatrix);
            Upscaler.SetFloat("FarPlane", _camera.farClipPlane);
            Upscaler.SetInt("CurFrame", curframe);
            Upscaler.SetTextureFromGlobal(UpsampleKernel, "Albedo", "_CameraGBufferTexture0");
            Upscaler.SetTextureFromGlobal(UpsampleKernel, "Albedo2", "_CameraGBufferTexture1");
            Upscaler.SetTextureFromGlobal(UpsampleKernel, "DepthTex", "_CameraDepthTexture");
            // Upscaler.SetTextureFromGlobal(UpsampleKernel, "PrevDepthTex", "_LastCameraDepthTexture");
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "PrevDepthTex", PrevDepthTex);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "ThroughputTex", ThroughputTex);
            Upscaler.SetTextureFromGlobal(UpsampleKernel, "NormalTex", "_CameraGBufferTexture2");
            Upscaler.SetTextureFromGlobal(UpsampleKernel, "MotionVectors", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "SmallerGBuffer", ScreenSpaceInfo);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "Input", Input);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "Output", UpScalerLightingDataTexture);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "FinalOutput", Output);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "PrevOutput", PrevOutputTex);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "PrevDepthTexWrite", TempTexUpscaler);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "TAAPrev", PrevUpscalerTAA);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel, "TAAPrevWrite", TempTexUpscaler2);
            cmd.BeginSample("Upsample Main Kernel");
            cmd.DispatchCompute(Upscaler, UpsampleKernel, threadGroupsX2, threadGroupsY2, 1);
            cmd.EndSample("Upsample Main Kernel");


            Upscaler.SetTextureFromGlobal(UpsampleKernel + 1, "Albedo", "_CameraGBufferTexture0");
            Upscaler.SetTextureFromGlobal(UpsampleKernel + 1, "Albedo2", "_CameraGBufferTexture1");
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel + 1, "Input", UpScalerLightingDataTexture);
            cmd.SetComputeTextureParam(Upscaler, UpsampleKernel + 1, "FinalOutput", Output);
            cmd.BeginSample("Upsample Blur Kernel");
            cmd.DispatchCompute(Upscaler, UpsampleKernel + 1, threadGroupsX2, threadGroupsY2, 1);
            cmd.EndSample("Upsample Blur Kernel");
            

            cmd.CopyTexture(TempTexUpscaler, PrevDepthTex);
            cmd.CopyTexture(UpScalerLightingDataTexture, PrevOutputTex);
            cmd.CopyTexture(TempTexUpscaler2, PrevUpscalerTAA);
            cmd.CopyTexture(PrevNormalUpscalerTexWrite, PrevNormalUpscalerTex);
            PreviousCameraMatrix = _camera.cameraToWorldMatrix;
            PreviousCameraInverseMatrix = _camera.projectionMatrix.inverse;
            //PrevDepthTex = Shader.GetGlobalTexture("_LastCameraDepthTexture");
            PrevProjInv = _camera.projectionMatrix.inverse;
        }


        public void ExecuteToneMap(ref RenderTexture Output, CommandBuffer cmd, ref Texture3D LUT, int ToneMapSelection)
        {//need to fix this so it doesnt create new textures every time
            cmd.BeginSample("ToneMap");
            cmd.SetComputeIntParam(ToneMapper,"ToneMapSelection", ToneMapSelection);
            cmd.SetComputeIntParam(ToneMapper,"width", Output.width);
            cmd.SetComputeIntParam(ToneMapper,"height", Output.height);
            cmd.SetComputeIntParam(ToneMapper,"ScreenWidth", Output.width);
            cmd.SetComputeIntParam(ToneMapper,"ScreenHeight", Output.height);
            cmd.SetComputeTextureParam(ToneMapper, 0, "Result", Output);
            cmd.SetComputeTextureParam(ToneMapper, 0, "LUT", LUT);
            cmd.DispatchCompute(ToneMapper, 0, threadGroupsX2, threadGroupsY2, 1);
            cmd.EndSample("ToneMap");
        }
        private void InitializeTAAU() {
            CommonFunctions.CreateRenderTexture(ref TAAA, Screen.width, Screen.height, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref TAAB, Screen.width, Screen.height, CommonFunctions.RTHalf4);
            TAAUInitialized = true;
        }
        public void ExecuteTAAU(ref RenderTexture Output, ref RenderTexture Input, ref RenderTexture ThroughputTex, CommandBuffer cmd, int CurFrame, RenderTexture CorrectedDepthTex)
        {//need to fix this so it doesnt create new textures every time
            if(!TAAUInitialized) InitializeTAAU();
            cmd.BeginSample("TAAU");
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
            cmd.EndSample("TAAU");
        }

    }
}

