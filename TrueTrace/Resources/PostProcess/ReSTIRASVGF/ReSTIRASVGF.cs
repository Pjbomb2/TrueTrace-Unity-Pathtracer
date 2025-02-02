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
        private RenderTexture ASVGF_HIST_COLOR_HF;
        private RenderTexture ASVGF_ATROUS_PING_LF_SH;
        private RenderTexture ASVGF_ATROUS_PONG_LF_SH;
        private RenderTexture ASVGF_ATROUS_PING_LF_COCG;
        private RenderTexture ASVGF_ATROUS_PONG_LF_COCG;
        private RenderTexture ASVGF_ATROUS_PING_HF;
        private RenderTexture ASVGF_ATROUS_PONG_HF;
        private RenderTexture ASVGF_ATROUS_PING_SPEC;
        private RenderTexture ASVGF_ATROUS_PONG_SPEC;
        private RenderTexture ASVGF_ATROUS_PING_MOMENTS;
        private RenderTexture ASVGF_ATROUS_PONG_MOMENTS;
        private RenderTexture ASVGF_GRAD_LF_PING;
        private RenderTexture ASVGF_GRAD_LF_PONG;
        private RenderTexture ASVGF_GRAD_HF_SPEC_PONG;
        private RenderTexture ASVGF_FILTERED_SPEC_A;
        private RenderTexture ASVGF_FILTERED_SPEC_B;
        private RenderTexture ASVGF_HIST_MOMENTS_HF_A;
        private RenderTexture ASVGF_HIST_MOMENTS_HF_B;
        private RenderTexture ASVGF_HIST_COLOR_LF_SH_A;
        private RenderTexture ASVGF_HIST_COLOR_LF_SH_B;
        private RenderTexture ASVGF_HIST_COLOR_LF_COCG_A;
        private RenderTexture ASVGF_HIST_COLOR_LF_COCG_B;
        private RenderTexture MetallicA;
        private RenderTexture MetallicB;
        private RenderTexture InputLFSH;
        private RenderTexture InputLFCOCG;

        private RenderTexture TEX_PT_COLOR_SPEC;


        private RenderTexture LFVarianceB;
        private RenderTexture LFVarianceA;

        private RenderTexture TEX_PT_NORMALS_A;
        private RenderTexture TEX_PT_NORMALS_B;

        private RenderTexture AlbedoColorA;
        private RenderTexture AlbedoColorB;


        private RenderTexture ReflectedRefractedTex;
        private RenderTexture ReflectedRefractedTexPrev;

        public RenderTexture CorrectedDistanceTexA;
        public RenderTexture CorrectedDistanceTexB;

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
        private int DistanceCorrection;
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
                CommonFunctions.ReleaseSafe(CorrectedDistanceTexA);
                CommonFunctions.ReleaseSafe(CorrectedDistanceTexB);
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
            DistanceCorrection = shader.FindKernel("DistanceCorrectionKernel");
            shader.SetInt("screen_width", ScreenWidth);
            shader.SetInt("screen_height", ScreenHeight);


            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_COLOR_HF, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref TEX_PT_NORMALS_A, ScreenWidth, ScreenHeight, CommonFunctions.RTFull2);
            CommonFunctions.CreateRenderTexture(ref TEX_PT_NORMALS_B, ScreenWidth, ScreenHeight, CommonFunctions.RTFull2);

            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PING_LF_SH, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PONG_LF_SH, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTFull4);

            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PING_LF_COCG, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PONG_LF_COCG, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);

            CommonFunctions.CreateRenderTexture(ref AlbedoColorA, ScreenWidth, ScreenHeight, CommonFunctions.RTInt2);
            CommonFunctions.CreateRenderTexture(ref AlbedoColorB, ScreenWidth, ScreenHeight, CommonFunctions.RTInt2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PING_HF, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PONG_HF, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PING_SPEC, ScreenWidth, ScreenHeight, CommonFunctions.RTFull1);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PONG_SPEC, ScreenWidth, ScreenHeight, CommonFunctions.RTFull1);
            CommonFunctions.CreateRenderTexture(ref ASVGF_FILTERED_SPEC_A, ScreenWidth, ScreenHeight, CommonFunctions.RTFull2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_FILTERED_SPEC_B, ScreenWidth, ScreenHeight, CommonFunctions.RTFull2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_SH_A, ScreenWidth, ScreenHeight, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_SH_B, ScreenWidth, ScreenHeight, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_COCG_A, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_COCG_B, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref MetallicA, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref MetallicB, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ReflectedRefractedTex, ScreenWidth, ScreenHeight, CommonFunctions.RTFull1);
            CommonFunctions.CreateRenderTexture(ref ReflectedRefractedTexPrev, ScreenWidth, ScreenHeight, CommonFunctions.RTFull1);
            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_MOMENTS_HF_B, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_MOMENTS_HF_A, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref LFVarianceA, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTFull1);
            CommonFunctions.CreateRenderTexture(ref LFVarianceB, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTFull1);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PONG_MOMENTS, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PING_MOMENTS, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_LF_PING, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_LF_PONG, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_HF_SPEC_PONG, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref InputLFSH, ScreenWidth, ScreenHeight, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref InputLFCOCG, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref TEX_PT_COLOR_SPEC, ScreenWidth, ScreenHeight, CommonFunctions.RTFull1);
            CommonFunctions.CreateRenderTexture(ref CorrectedDistanceTexA, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref CorrectedDistanceTexB, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            Initialized = true;
        }

        Vector3 prevEuler;
        Vector3 PrevPos;
        public void Do(ref ComputeBuffer _ColorBuffer,  
                        ref RenderTexture Output, 
                        float ResolutionRatio, 
                        RenderTexture ScreenSpaceInfo, 
                        RenderTexture ScreenSpaceInfoPrev, 
                        CommandBuffer cmd, 
                        int CurFrame, 
                        ref RenderTexture SecondaryTriData, 
                        int PartialRenderingFactor, 
                        ComputeBuffer ExposureModifier, 
                        bool DoExposure, 
                        float IndirectBoost, 
                        RenderTexture Gradients,
                        RenderTexture PrimaryTriData,
                        ComputeBuffer MeshData,
                        ComputeBuffer TriData,
                        int UpscalerMethod)
        {

            camera = RayTracingMaster._camera;
            bool EvenFrame = CurFrame % 2 == 0;
            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("Dist Correct Kernel");
            cmd.SetComputeIntParam(shader, "UpscalerMethod", UpscalerMethod);
            Vector3 Euler = camera.transform.eulerAngles;
            shader.SetMatrix("viewprojection", camera.projectionMatrix * camera.worldToCameraMatrix);
            camera.transform.eulerAngles = prevEuler; 
            shader.SetMatrix("prevviewprojection", camera.projectionMatrix * camera.worldToCameraMatrix);
            camera.transform.eulerAngles = Euler; 
            prevEuler = Euler;
            shader.SetVector("Forward", camera.transform.forward);
            shader.SetFloat("FarPlane", camera.farClipPlane);
            shader.SetFloat("NearPlane", camera.nearClipPlane);
            shader.SetMatrix("CamToWorld", camera.cameraToWorldMatrix);
            shader.SetMatrix("CamInvProj", camera.projectionMatrix.inverse);
            shader.SetTextureFromGlobal(DistanceCorrection, "Depth", "_CameraDepthTexture");
            cmd.SetComputeTextureParam(shader, DistanceCorrection, "TEX_PT_VIEW_DEPTH_AWRITE", EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);
            cmd.DispatchCompute(shader, DistanceCorrection, Mathf.CeilToInt((ScreenWidth) / 32.0f), Mathf.CeilToInt((ScreenHeight) / 32.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("Dist Correct Kernel");
            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ASVGF Copy Data Kernel");
            cmd.SetComputeIntParam(shader, "MaxIterations", 4);
            cmd.SetComputeFloatParam(shader, "ResRatio", ResolutionRatio);
            shader.SetBool("UseExposure", DoExposure);
            shader.SetFloat("IndirectBoost", IndirectBoost);
            shader.SetBuffer(CopyData, "GlobalColorsRead", _ColorBuffer);
            shader.SetBuffer(CopyData, "ExposureBuffer", ExposureModifier);
            shader.SetBuffer(Atrous, "ExposureBuffer", ExposureModifier);
            shader.SetTexture(CopyData, "ScreenSpaceInfo", ScreenSpaceInfo);
            shader.SetTexture(CopyData, "ScreenSpaceInfoPrev", ScreenSpaceInfoPrev);
            shader.SetTextureFromGlobal(CopyData, "MotionVectors", "_CameraMotionVectorsTexture");
            shader.SetInt("PartialRenderingFactor", PartialRenderingFactor);
            shader.SetBuffer(CopyData, "AggTrisA", TriData);
            shader.SetBuffer(CopyData, "_MeshData", MeshData);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_LF_SHWrite", InputLFSH);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_LF_COCGWrite", InputLFCOCG);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_HFWrite", ASVGF_ATROUS_PING_HF);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_SPECWrite", TEX_PT_COLOR_SPEC);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_NORMALS_AWrite", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_VIEW_DEPTH_B", !EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_NORMALS_B", (!EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, CopyData, "SecondaryTriData", SecondaryTriData);
            cmd.SetComputeTextureParam(shader, CopyData, "PrimaryTriData", PrimaryTriData);
            shader.SetTextureFromGlobal(CopyData, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");

            cmd.SetComputeTextureParam(shader, CopyData, "MetallicAWrite", (EvenFrame ? MetallicA : MetallicB));
            cmd.SetComputeTextureParam(shader, CopyData, "ReflectedRefractedTexWrite", (EvenFrame ? ReflectedRefractedTex : ReflectedRefractedTexPrev));
            cmd.SetComputeTextureParam(shader, CopyData, "ReflectedRefractedTexPrev", (!EvenFrame ? ReflectedRefractedTex : ReflectedRefractedTexPrev));

            cmd.SetComputeTextureParam(shader, CopyData, "AlbedoColorA", (EvenFrame ? AlbedoColorA : AlbedoColorB));
            cmd.SetComputeTextureParam(shader, CopyData, "AlbedoColorB", (!EvenFrame ? AlbedoColorA : AlbedoColorB));


            cmd.DispatchCompute(shader, CopyData, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ASVGF Copy Data Kernel");
            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ASVGF Grad Compute Kernel");
            shader.SetTextureFromGlobal(Gradient_Img, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(shader, Gradient_Img, "IMG_ASVGF_GRAD_LF_PING", ASVGF_GRAD_LF_PING);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_COLOR_LF_SH", InputLFSH);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_ASVGF_HIST_COLOR_LF_SH_B", (!EvenFrame ? ASVGF_HIST_COLOR_LF_SH_A : ASVGF_HIST_COLOR_LF_SH_B));
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_VIEW_DEPTH_AWRITE", EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "AlbedoColorA", (EvenFrame ? AlbedoColorA : AlbedoColorB));
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_NORMALS_AWrite", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, Gradient_Img, "ReflectedRefractedTexWrite", (EvenFrame ? ReflectedRefractedTex : ReflectedRefractedTexPrev));


            cmd.DispatchCompute(shader, Gradient_Img, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ASVGF Grad Compute Kernel");

            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ASVGF Grad Atrous Kernels");
            for (int i = 0; i < 7; i++)
            {
                var e = i;
                cmd.SetComputeIntParam(shader, "iteration", e);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "TEX_ASVGF_GRAD_LF_PING", ASVGF_GRAD_LF_PING);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "TEX_ASVGF_GRAD_HF_SPEC_PING", Gradients);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "TEX_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "TEX_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);


                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "IMG_ASVGF_GRAD_LF_PING", ASVGF_GRAD_LF_PING);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "IMG_ASVGF_GRAD_HF_SPEC_PING", Gradients);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "IMG_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "IMG_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);

                cmd.DispatchCompute(shader, Gradient_Atrous, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);
            }
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ASVGF Grad Atrous Kernels");

            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ASVGF Temporal Kernel");
            shader.SetTextureFromGlobal(Temporal, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(shader, Temporal, "MetallicA", (EvenFrame ? MetallicA : MetallicB));
            cmd.SetComputeTextureParam(shader, Temporal, "ReflectedRefractedTex", (EvenFrame ? ReflectedRefractedTex : ReflectedRefractedTexPrev));
            cmd.SetComputeTextureParam(shader, Temporal, "ReflectedRefractedTexPrev", (!EvenFrame ? ReflectedRefractedTex : ReflectedRefractedTexPrev));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_VIEW_DEPTH_A", EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_VIEW_DEPTH_B", !EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_NORMALS_A", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_NORMALS_B", (!EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_COLOR_LF_SH_B", (!EvenFrame ? ASVGF_HIST_COLOR_LF_SH_A : ASVGF_HIST_COLOR_LF_SH_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_COLOR_LF_COCG_B", (!EvenFrame ? ASVGF_HIST_COLOR_LF_COCG_A : ASVGF_HIST_COLOR_LF_COCG_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_COLOR_HF", ASVGF_HIST_COLOR_HF);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_FILTERED_SPEC_B", (!EvenFrame ? ASVGF_FILTERED_SPEC_A : ASVGF_FILTERED_SPEC_B));
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_SPEC2", (EvenFrame ? ASVGF_FILTERED_SPEC_A : ASVGF_FILTERED_SPEC_B));
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
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ASVGF Temporal Kernel");

            shader.SetBool("DiffRes", ResolutionRatio != 1.0f);
            // cmd.CopyTexture((EvenFrame ? ASVGF_FILTERED_SPEC_A : ASVGF_FILTERED_SPEC_B), ASVGF_ATROUS_PING_SPEC);
            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("COPY A");
            cmd.SetComputeTextureParam(shader, DistanceCorrection + 1, "TEX_ASVGF_FILTERED_SPEC_B", (EvenFrame ? ASVGF_FILTERED_SPEC_A : ASVGF_FILTERED_SPEC_B));
            cmd.SetComputeTextureParam(shader, DistanceCorrection + 1, "IMG_ASVGF_ATROUS_PING_SPEC", ASVGF_ATROUS_PING_SPEC);
            cmd.DispatchCompute(shader, DistanceCorrection + 1, Mathf.CeilToInt((ScreenWidth) / 32.0f), Mathf.CeilToInt((ScreenHeight) / 32.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("COPY A");

            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ASVGF Atrous LF: " + 0);
            cmd.SetComputeIntParam(shader, "iteration", 0);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_PT_NORMALS_A", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_PT_VIEW_DEPTH_A", EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);
            shader.SetTextureFromGlobal(Atrous_LF, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");


            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PING_LF_SH);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PING_LF_COCG);

            cmd.SetComputeTextureParam(shader, Atrous_LF, "LFVarianceA", (0 % 2 == 0 ? LFVarianceA : LFVarianceB));
            cmd.SetComputeTextureParam(shader, Atrous_LF, "LFVarianceB", (0 % 2 == 1 ? LFVarianceA : LFVarianceB));

            cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PONG_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PONG_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_HIST_MOMENTS_HF_A", (EvenFrame ? ASVGF_HIST_MOMENTS_HF_A : ASVGF_HIST_MOMENTS_HF_B));
            cmd.DispatchCompute(shader, Atrous_LF, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ASVGF Atrous LF: " + 0);


            for (int i = 0; i < 4; i++)
            {
                var e = i;
                if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ASVGF Atrous LF " + (e + 1));
                cmd.SetComputeIntParam(shader, "iteration", e + 1);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_PT_NORMALS_A", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_PT_VIEW_DEPTH_A", EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);

                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PING_LF_SH", (e % 2 == 0) ? ASVGF_ATROUS_PONG_LF_SH : ASVGF_ATROUS_PING_LF_SH);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PING_LF_COCG", (e % 2 == 0) ? ASVGF_ATROUS_PONG_LF_COCG : ASVGF_ATROUS_PING_LF_COCG);

                cmd.SetComputeTextureParam(shader, Atrous_LF, "LFVarianceA", ((e + 1) % 2 == 0 ? LFVarianceA : LFVarianceB));
                cmd.SetComputeTextureParam(shader, Atrous_LF, "LFVarianceB", ((e + 1) % 2 == 1 ? LFVarianceA : LFVarianceB));

                cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PONG_LF_SH", (e % 2 == 1) ? ASVGF_ATROUS_PONG_LF_SH : ASVGF_ATROUS_PING_LF_SH);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PONG_LF_COCG", (e % 2 == 1) ? ASVGF_ATROUS_PONG_LF_COCG : ASVGF_ATROUS_PING_LF_COCG);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_HIST_MOMENTS_HF_A", (EvenFrame ? ASVGF_HIST_MOMENTS_HF_A : ASVGF_HIST_MOMENTS_HF_B));
                cmd.DispatchCompute(shader, Atrous_LF, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);

                if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ASVGF Atrous LF " + (e + 1));
                if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ASVGF Atrous " + e);

                cmd.SetComputeIntParam(shader, "spec_iteration", e);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_PT_NORMALS_A", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_PT_VIEW_DEPTH_A", EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);
                shader.SetTextureFromGlobal(Atrous, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_HIST_MOMENTS_HF_A", (EvenFrame ? ASVGF_HIST_MOMENTS_HF_A : ASVGF_HIST_MOMENTS_HF_B));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_HIST_COLOR_LF_SH_A", (EvenFrame ? ASVGF_HIST_COLOR_LF_SH_A : ASVGF_HIST_COLOR_LF_SH_B));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_HIST_COLOR_LF_COCG_A", (EvenFrame ? ASVGF_HIST_COLOR_LF_COCG_A : ASVGF_HIST_COLOR_LF_COCG_B));

                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_HF", (e == 1) ? ASVGF_HIST_COLOR_HF : ((e % 2 == 0) ? ASVGF_ATROUS_PING_HF : ASVGF_ATROUS_PONG_HF));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_SPEC", ((e % 2 == 0) ? ASVGF_ATROUS_PING_SPEC : ASVGF_ATROUS_PONG_SPEC));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_MOMENTS", ((e % 2 == 0) ? ASVGF_ATROUS_PING_MOMENTS : ASVGF_ATROUS_PONG_MOMENTS));


                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PING_HF", (e == 0) ? ASVGF_HIST_COLOR_HF : ((e % 2 == 1) ? ASVGF_ATROUS_PING_HF : ASVGF_ATROUS_PONG_HF));
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PING_SPEC", ((e % 2 == 1) ? ASVGF_ATROUS_PING_SPEC : ASVGF_ATROUS_PONG_SPEC));
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PING_MOMENTS", ((e % 2 == 1) ? ASVGF_ATROUS_PING_MOMENTS : ASVGF_ATROUS_PONG_MOMENTS));

                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);
                cmd.SetComputeBufferParam(shader, Atrous, "GlobalColorsRead", _ColorBuffer);
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
                if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ASVGF Atrous " + e);

            }


            iter++;

            PrevCamPos = camera.transform.position;



        }

    }
}