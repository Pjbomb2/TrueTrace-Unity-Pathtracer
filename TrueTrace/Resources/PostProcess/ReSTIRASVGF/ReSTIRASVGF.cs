using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CommonVars;

namespace TrueTrace {
    [System.Serializable]
    public class ReSTIRASVGF
    {
        public bool Initialized = false;
        public RenderTexture ASVGF_HIST_COLOR_HF;
        public RenderTexture ASVGF_ATROUS_PING_LF_SH;
        public RenderTexture ASVGF_ATROUS_PONG_LF_SH;
        public RenderTexture ASVGF_ATROUS_PING_LF_COCG;
        public RenderTexture ASVGF_ATROUS_PONG_LF_COCG;
        public RenderTexture ASVGF_ATROUS_PING_HF;
        public RenderTexture ASVGF_ATROUS_PONG_HF;
        public RenderTexture ASVGF_ATROUS_PING_SPEC;
        public RenderTexture ASVGF_ATROUS_PONG_SPEC;
        public RenderTexture ASVGF_ATROUS_PING_MOMENTS;
        public RenderTexture ASVGF_ATROUS_PONG_MOMENTS;
        public RenderTexture ASVGF_GRAD_LF_PING;
        public RenderTexture ASVGF_GRAD_LF_PONG;
        public RenderTexture ASVGF_GRAD_HF_SPEC_PING;
        public RenderTexture ASVGF_GRAD_HF_SPEC_PONG;
        public RenderTexture ASVGF_FILTERED_SPEC_A;
        public RenderTexture ASVGF_FILTERED_SPEC_B;
        public RenderTexture ASVGF_HIST_MOMENTS_HF_A;
        public RenderTexture ASVGF_HIST_MOMENTS_HF_B;
        public RenderTexture ASVGF_HIST_COLOR_LF_SH_A;
        public RenderTexture ASVGF_HIST_COLOR_LF_SH_B;
        public RenderTexture ASVGF_HIST_COLOR_LF_COCG_A;
        public RenderTexture ASVGF_HIST_COLOR_LF_COCG_B;
        public RenderTexture MetallicA;
        public RenderTexture MetallicB;
        public RenderTexture InputLFSH;
        public RenderTexture InputLFCOCG;

        public RenderTexture TEX_PT_COLOR_SPEC;


        public RenderTexture LFVarianceA;
        public RenderTexture LFVarianceB;

        public RenderTexture TEX_PT_NORMALS_A;
        public RenderTexture TEX_PT_NORMALS_B;

        public RenderTexture AlbedoColorA;
        public RenderTexture AlbedoColorB;


        public RenderTexture ReflectedRefractedTex;
        public RenderTexture ReflectedRefractedTexPrev;


        public Camera camera;

        public int ScreenWidth;
        public int ScreenHeight;
        public ComputeShader shader;

        private int CopyData;
        private int Gradient_Atrous;
        private int Gradient_Img;
        private int Temporal;
        private int Atrous_LF;
        private int Atrous;
        private Vector3 PrevCamPos;

        public void ClearAll()
        {
                CommonFunctions.ReleaseSafe(ASVGF_HIST_COLOR_HF);
                CommonFunctions.ReleaseSafe(ASVGF_ATROUS_PING_LF_SH);
                CommonFunctions.ReleaseSafe(ASVGF_ATROUS_PONG_LF_SH);
                CommonFunctions.ReleaseSafe(ASVGF_ATROUS_PING_LF_COCG);
                CommonFunctions.ReleaseSafe(ASVGF_ATROUS_PONG_LF_COCG);
                CommonFunctions.ReleaseSafe(ASVGF_ATROUS_PING_HF);
                CommonFunctions.ReleaseSafe(ASVGF_ATROUS_PONG_HF);
                CommonFunctions.ReleaseSafe(ASVGF_ATROUS_PING_SPEC);
                CommonFunctions.ReleaseSafe(ASVGF_ATROUS_PONG_SPEC);
                CommonFunctions.ReleaseSafe(ASVGF_ATROUS_PING_MOMENTS);
                CommonFunctions.ReleaseSafe(ASVGF_ATROUS_PONG_MOMENTS);
                CommonFunctions.ReleaseSafe(ASVGF_GRAD_LF_PING);
                CommonFunctions.ReleaseSafe(ASVGF_GRAD_LF_PONG);
                CommonFunctions.ReleaseSafe(ASVGF_GRAD_HF_SPEC_PING);
                CommonFunctions.ReleaseSafe(ASVGF_GRAD_HF_SPEC_PONG);
                CommonFunctions.ReleaseSafe(ASVGF_FILTERED_SPEC_A);
                CommonFunctions.ReleaseSafe(ASVGF_FILTERED_SPEC_B);
                CommonFunctions.ReleaseSafe(ASVGF_HIST_MOMENTS_HF_A);
                CommonFunctions.ReleaseSafe(ASVGF_HIST_MOMENTS_HF_B);
                CommonFunctions.ReleaseSafe(ASVGF_HIST_COLOR_LF_SH_A);
                CommonFunctions.ReleaseSafe(ASVGF_HIST_COLOR_LF_SH_B);
                CommonFunctions.ReleaseSafe(ASVGF_HIST_COLOR_LF_COCG_A);
                CommonFunctions.ReleaseSafe(ASVGF_HIST_COLOR_LF_COCG_B);
                CommonFunctions.ReleaseSafe(MetallicA);
                CommonFunctions.ReleaseSafe(MetallicB);
                CommonFunctions.ReleaseSafe(TEX_PT_NORMALS_A);
                CommonFunctions.ReleaseSafe(TEX_PT_NORMALS_B);
                CommonFunctions.ReleaseSafe(ReflectedRefractedTex);
                CommonFunctions.ReleaseSafe(ReflectedRefractedTexPrev);
                CommonFunctions.ReleaseSafe(AlbedoColorA);
                CommonFunctions.ReleaseSafe(AlbedoColorB);
                CommonFunctions.ReleaseSafe(LFVarianceA);
                CommonFunctions.ReleaseSafe(LFVarianceB);
                CommonFunctions.ReleaseSafe(InputLFSH);
                CommonFunctions.ReleaseSafe(InputLFCOCG);
                CommonFunctions.ReleaseSafe(TEX_PT_COLOR_SPEC);
            Initialized = false;
        }

        public int iter;
        public void init(int ScreenWidth, int ScreenHeight)
        {
            this.ScreenWidth = ScreenWidth;
            this.ScreenHeight = ScreenHeight;
            iter = 0;
            if (shader == null) { shader = Resources.Load<ComputeShader>("PostProcess/ReSTIRASVGF/ReSTIRASVGF"); }
            Gradient_Atrous = shader.FindKernel("Gradient_Atrous");
            Gradient_Img = shader.FindKernel("Gradient_Img");
            Atrous_LF = shader.FindKernel("Atrous_LF");
            Temporal = shader.FindKernel("Temporal");
            CopyData = shader.FindKernel("CopyData");
            Atrous = shader.FindKernel("Atrous");
            shader.SetInt("screen_width", ScreenWidth);
            shader.SetInt("screen_height", ScreenHeight);


            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_COLOR_HF, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref TEX_PT_NORMALS_A, ScreenWidth, ScreenHeight, CommonFunctions.RTInt2);
            CommonFunctions.CreateRenderTexture(ref TEX_PT_NORMALS_B, ScreenWidth, ScreenHeight, CommonFunctions.RTInt2);

            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PING_LF_SH, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PONG_LF_SH, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTFull4);

            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PING_LF_COCG, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTFull2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PONG_LF_COCG, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTFull2);

            CommonFunctions.CreateRenderTexture(ref AlbedoColorA, ScreenWidth, ScreenHeight, CommonFunctions.RTInt2);
            CommonFunctions.CreateRenderTexture(ref AlbedoColorB, ScreenWidth, ScreenHeight, CommonFunctions.RTInt2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PING_HF, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PONG_HF, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PING_SPEC, ScreenWidth, ScreenHeight, CommonFunctions.RTFull2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PONG_SPEC, ScreenWidth, ScreenHeight, CommonFunctions.RTFull2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_FILTERED_SPEC_A, ScreenWidth, ScreenHeight, CommonFunctions.RTFull2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_FILTERED_SPEC_B, ScreenWidth, ScreenHeight, CommonFunctions.RTFull2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_SH_A, ScreenWidth, ScreenHeight, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_SH_B, ScreenWidth, ScreenHeight, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_COCG_A, ScreenWidth, ScreenHeight, CommonFunctions.RTFull2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_COCG_B, ScreenWidth, ScreenHeight, CommonFunctions.RTFull2);
            CommonFunctions.CreateRenderTexture(ref MetallicA, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref MetallicB, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ReflectedRefractedTex, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ReflectedRefractedTexPrev, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_MOMENTS_HF_B, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_MOMENTS_HF_A, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref LFVarianceA, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTFull1);
            CommonFunctions.CreateRenderTexture(ref LFVarianceB, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTFull1);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PONG_MOMENTS, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PING_MOMENTS, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_LF_PING, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_LF_PONG, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_HF_SPEC_PING, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_HF_SPEC_PONG, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref InputLFSH, ScreenWidth, ScreenHeight, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref InputLFCOCG, ScreenWidth, ScreenHeight, CommonFunctions.RTFull2);
            CommonFunctions.CreateRenderTexture(ref TEX_PT_COLOR_SPEC, ScreenWidth, ScreenHeight, CommonFunctions.RTFull1);
            Initialized = true;
        }

        Vector3 prevEuler;
        Vector3 PrevPos;


        public void Do(ref ComputeBuffer _ColorBuffer, 
                        ref RenderTexture Albedo, 
                        ref RenderTexture Output, 
                        float ResolutionRatio, 
                        RenderTexture TEX_PT_VIEW_DEPTH_B, 
                        RenderTexture ScreenSpaceInfo, 
                        RenderTexture ScreenSpaceInfoPrev, 
                        CommandBuffer cmd, 
                        RenderTexture CorrectedDepthTex, 
                        int CurFrame, 
                        ref RenderTexture WorldPosData, 
                        int PartialRenderingFactor, 
                        ComputeBuffer ExposureModifier, 
                        bool DoExposure, 
                        float IndirectBoost, 
                        RenderTexture Gradients)
        {

            camera = RayTracingMaster._camera;
            bool EvenFrame = CurFrame % 2 == 0;
            cmd.BeginSample("ASVGF Copy Data Kernel");
            cmd.SetComputeIntParam(shader, "MaxIterations", 4);
            cmd.SetComputeFloatParam(shader, "ResRatio", ResolutionRatio);
            shader.SetBool("UseExposure", DoExposure);
            shader.SetFloat("IndirectBoost", IndirectBoost);
            shader.SetBuffer(CopyData, "PerPixelRadiance", _ColorBuffer);
            shader.SetBuffer(CopyData, "ExposureBuffer", ExposureModifier);
            shader.SetBuffer(Atrous, "ExposureBuffer", ExposureModifier);
            shader.SetTexture(CopyData, "ScreenSpaceInfo", ScreenSpaceInfo);
            shader.SetTexture(CopyData, "ScreenSpaceInfoPrev", ScreenSpaceInfoPrev);
            shader.SetTextureFromGlobal(CopyData, "MotionVectors", "_CameraMotionVectorsTexture");
            shader.SetInt("PartialRenderingFactor", PartialRenderingFactor);
            cmd.SetComputeTextureParam(shader, CopyData, "WorldPosData", WorldPosData);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_LF_SHWrite", InputLFSH);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_LF_COCGWrite", InputLFCOCG);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_HFWrite", ASVGF_ATROUS_PING_HF);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_SPECWrite", TEX_PT_COLOR_SPEC);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_NORMALS_AWrite", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_VIEW_DEPTH_B", TEX_PT_VIEW_DEPTH_B);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_NORMALS_B", (!EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            shader.SetTextureFromGlobal(CopyData, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");

            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_BASE_COLOR_A", Albedo);
            cmd.SetComputeTextureParam(shader, CopyData, "MetallicAWrite", (EvenFrame ? MetallicA : MetallicB));
            cmd.SetComputeTextureParam(shader, CopyData, "ReflectedRefractedTexWrite", (EvenFrame ? ReflectedRefractedTex : ReflectedRefractedTexPrev));
            cmd.SetComputeTextureParam(shader, CopyData, "ReflectedRefractedTexPrev", (!EvenFrame ? ReflectedRefractedTex : ReflectedRefractedTexPrev));

            cmd.SetComputeTextureParam(shader, CopyData, "AlbedoColorA", (EvenFrame ? AlbedoColorA : AlbedoColorB));
            cmd.SetComputeTextureParam(shader, CopyData, "AlbedoColorB", (!EvenFrame ? AlbedoColorA : AlbedoColorB));


            cmd.DispatchCompute(shader, CopyData, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);
            cmd.EndSample("ASVGF Copy Data Kernel");
            cmd.BeginSample("ASVGF Grad Compute Kernel");
            shader.SetTextureFromGlobal(Gradient_Img, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(shader, Gradient_Img, "IMG_ASVGF_GRAD_LF_PING", ASVGF_GRAD_LF_PING);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "IMG_ASVGF_GRAD_HF_SPEC_PING", ASVGF_GRAD_HF_SPEC_PING);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_COLOR_LF_SH", InputLFSH);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_ASVGF_HIST_COLOR_LF_SH_B", (!EvenFrame ? ASVGF_HIST_COLOR_LF_SH_A : ASVGF_HIST_COLOR_LF_SH_B));
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_VIEW_DEPTH_AWRITE", CorrectedDepthTex);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "AlbedoColorA", (EvenFrame ? AlbedoColorA : AlbedoColorB));
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_NORMALS_AWrite", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, Gradient_Img, "ReflectedRefractedTexWrite", (EvenFrame ? ReflectedRefractedTex : ReflectedRefractedTexPrev));


            cmd.DispatchCompute(shader, Gradient_Img, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);
            cmd.CopyTexture(Gradients, ASVGF_GRAD_HF_SPEC_PING);
            cmd.EndSample("ASVGF Grad Compute Kernel");

            cmd.BeginSample("ASVGF Grad Atrous Kernels");
            for (int i = 0; i < 7; i++)
            {
                var e = i;
                cmd.SetComputeIntParam(shader, "iteration", e);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "TEX_ASVGF_GRAD_LF_PING", ASVGF_GRAD_LF_PING);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "TEX_ASVGF_GRAD_HF_SPEC_PING", ASVGF_GRAD_HF_SPEC_PING);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "TEX_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "TEX_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);


                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "IMG_ASVGF_GRAD_LF_PING", ASVGF_GRAD_LF_PING);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "IMG_ASVGF_GRAD_HF_SPEC_PING", ASVGF_GRAD_HF_SPEC_PING);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "IMG_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "IMG_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);

                cmd.DispatchCompute(shader, Gradient_Atrous, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);
            }
            cmd.EndSample("ASVGF Grad Atrous Kernels");

            cmd.BeginSample("ASVGF Temporal Kernel");
            shader.SetTextureFromGlobal(Temporal, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(shader, Temporal, "MetallicA", (EvenFrame ? MetallicA : MetallicB));
            cmd.SetComputeTextureParam(shader, Temporal, "ReflectedRefractedTex", (EvenFrame ? ReflectedRefractedTex : ReflectedRefractedTexPrev));
            cmd.SetComputeTextureParam(shader, Temporal, "ReflectedRefractedTexPrev", (!EvenFrame ? ReflectedRefractedTex : ReflectedRefractedTexPrev));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_VIEW_DEPTH_A", CorrectedDepthTex);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_VIEW_DEPTH_B", TEX_PT_VIEW_DEPTH_B);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_NORMALS_A", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_NORMALS_B", (!EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_COLOR_LF_SH_B", (!EvenFrame ? ASVGF_HIST_COLOR_LF_SH_A : ASVGF_HIST_COLOR_LF_SH_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_COLOR_LF_COCG_B", (!EvenFrame ? ASVGF_HIST_COLOR_LF_COCG_A : ASVGF_HIST_COLOR_LF_COCG_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_COLOR_HF", ASVGF_HIST_COLOR_HF);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_FILTERED_SPEC_B", (!EvenFrame ? ASVGF_FILTERED_SPEC_A : ASVGF_FILTERED_SPEC_B));
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_SPEC", (EvenFrame ? ASVGF_FILTERED_SPEC_A : ASVGF_FILTERED_SPEC_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_COLOR_SPEC", TEX_PT_COLOR_SPEC);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_MOMENTS_HF_B", (!EvenFrame ? ASVGF_HIST_MOMENTS_HF_A : ASVGF_HIST_MOMENTS_HF_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_HIST_MOMENTS_HF_A", (EvenFrame ? ASVGF_HIST_MOMENTS_HF_A : ASVGF_HIST_MOMENTS_HF_B));
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_HIST_COLOR_LF_SH_A", (EvenFrame ? ASVGF_HIST_COLOR_LF_SH_A : ASVGF_HIST_COLOR_LF_SH_B));
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_HIST_COLOR_LF_COCG_A", (EvenFrame ? ASVGF_HIST_COLOR_LF_COCG_A : ASVGF_HIST_COLOR_LF_COCG_B));
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_HF", ASVGF_ATROUS_PING_HF);
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_MOMENTS", ASVGF_ATROUS_PING_MOMENTS);
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PING_LF_SH);
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PING_LF_COCG);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_COLOR_LF_SH", InputLFSH);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_COLOR_LF_COCG", InputLFCOCG);
            
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_ATROUS_PONG_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_ATROUS_PONG_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);
            
            cmd.DispatchCompute(shader, Temporal, Mathf.CeilToInt((ScreenWidth + 14) / 15.0f), Mathf.CeilToInt((ScreenHeight + 14) / 15.0f), 1);
            cmd.EndSample("ASVGF Temporal Kernel");

            shader.SetBool("DiffRes", ResolutionRatio != 1.0f);
            cmd.CopyTexture((EvenFrame ? ASVGF_FILTERED_SPEC_A : ASVGF_FILTERED_SPEC_B), ASVGF_ATROUS_PING_SPEC);
            cmd.BeginSample("ASVGF Atrous LF: " + 0);
            cmd.SetComputeIntParam(shader, "iteration", 0);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_PT_NORMALS_A", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_PT_VIEW_DEPTH_A", CorrectedDepthTex);
            shader.SetTextureFromGlobal(Atrous_LF, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");


            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PING_LF_SH);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PING_LF_COCG);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PONG_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PONG_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);

            cmd.SetComputeTextureParam(shader, Atrous_LF, "LFVarianceA", (0 % 2 == 0 ? LFVarianceA : LFVarianceB));
            cmd.SetComputeTextureParam(shader, Atrous_LF, "LFVarianceB", (0 % 2 == 1 ? LFVarianceA : LFVarianceB));

            cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PONG_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PONG_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PING_LF_SH);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PING_LF_COCG);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_HIST_MOMENTS_HF_A", (EvenFrame ? ASVGF_HIST_MOMENTS_HF_A : ASVGF_HIST_MOMENTS_HF_B));
            cmd.DispatchCompute(shader, Atrous_LF, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);
            cmd.EndSample("ASVGF Atrous LF: " + 0);


            for (int i = 0; i < 4; i++)
            {
                var e = i;
                cmd.BeginSample("ASVGF Atrous LF " + (e + 1));
                cmd.SetComputeIntParam(shader, "iteration", e + 1);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_PT_NORMALS_A", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_PT_VIEW_DEPTH_A", CorrectedDepthTex);

                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PING_LF_SH);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PING_LF_COCG);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PONG_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PONG_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);

                cmd.SetComputeTextureParam(shader, Atrous_LF, "LFVarianceA", ((e + 1) % 2 == 0 ? LFVarianceA : LFVarianceB));
                cmd.SetComputeTextureParam(shader, Atrous_LF, "LFVarianceB", ((e + 1) % 2 == 1 ? LFVarianceA : LFVarianceB));

                cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PONG_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PONG_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PING_LF_SH);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PING_LF_COCG);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_HIST_MOMENTS_HF_A", (EvenFrame ? ASVGF_HIST_MOMENTS_HF_A : ASVGF_HIST_MOMENTS_HF_B));
                cmd.DispatchCompute(shader, Atrous_LF, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);

                cmd.EndSample("ASVGF Atrous LF " + (e + 1));
                cmd.BeginSample("ASVGF Atrous " + e);

                cmd.SetComputeIntParam(shader, "spec_iteration", e);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_PT_NORMALS_A", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_PT_VIEW_DEPTH_A", CorrectedDepthTex);
                shader.SetTextureFromGlobal(Atrous, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_HIST_MOMENTS_HF_A", (EvenFrame ? ASVGF_HIST_MOMENTS_HF_A : ASVGF_HIST_MOMENTS_HF_B));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_HIST_COLOR_LF_SH_A", (EvenFrame ? ASVGF_HIST_COLOR_LF_SH_A : ASVGF_HIST_COLOR_LF_SH_B));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_HIST_COLOR_LF_COCG_A", (EvenFrame ? ASVGF_HIST_COLOR_LF_COCG_A : ASVGF_HIST_COLOR_LF_COCG_B));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_HF", ASVGF_ATROUS_PING_HF);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_SPEC", ASVGF_ATROUS_PING_SPEC);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_MOMENTS", ASVGF_ATROUS_PING_MOMENTS);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_HIST_COLOR_HF", ASVGF_HIST_COLOR_HF);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PONG_SPEC", ASVGF_ATROUS_PONG_SPEC);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PONG_MOMENTS", ASVGF_ATROUS_PONG_MOMENTS);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PONG_HF", ASVGF_ATROUS_PONG_HF);
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_HIST_COLOR_HF", ASVGF_HIST_COLOR_HF);
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PONG_SPEC", ASVGF_ATROUS_PONG_SPEC);
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PONG_MOMENTS", ASVGF_ATROUS_PONG_MOMENTS);
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PING_HF", ASVGF_ATROUS_PING_HF);
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PING_SPEC", ASVGF_ATROUS_PING_SPEC);
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PING_MOMENTS", ASVGF_ATROUS_PING_MOMENTS);
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PONG_HF", ASVGF_ATROUS_PONG_HF);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_PT_BASE_COLOR_A", Albedo);
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_COLOR", Output);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);
                cmd.SetComputeTextureParam(shader, Atrous, "MetallicA", (EvenFrame ? MetallicA : MetallicB));
                cmd.SetComputeTextureParam(shader, Atrous, "MetallicB", (!EvenFrame ? MetallicA : MetallicB));
                shader.SetTextureFromGlobal(Atrous, "DiffuseGBuffer", "_CameraGBufferTexture0");
                shader.SetTextureFromGlobal(Atrous, "SpecularGBuffer", "_CameraGBufferTexture1");
                shader.SetTextureFromGlobal(Atrous, "NormalTex", "_CameraGBufferTexture2");

                cmd.SetComputeTextureParam(shader, Atrous, "AlbedoColorB", (EvenFrame ? AlbedoColorA : AlbedoColorB));
                shader.SetTexture(Atrous, "ScreenSpaceInfo", ScreenSpaceInfo);
                cmd.DispatchCompute(shader, Atrous, Mathf.CeilToInt((ScreenWidth + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight + 15) / 16.0f), 1);
                cmd.EndSample("ASVGF Atrous " + e);

            }


            iter++;

            PrevCamPos = camera.transform.position;



        }

    }
}