using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CommonVars;

namespace TrueTrace {
    [System.Serializable]
    public class ASVGF
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
        public RenderTexture ASVGF_GRAD_SMPL_POS_A;
        public RenderTexture ASVGF_GRAD_SMPL_POS_B;
        public RenderTexture MetallicA;
        public RenderTexture MetallicB;
        public RenderTexture Quartiles;

        public RenderTexture ReflectedRefractedA;
        public RenderTexture ReflectedRefractedB;


        public RenderTexture LFVarianceA;
        public RenderTexture LFVarianceB;

        public RenderTexture TEX_PT_NORMALS_A;
        public RenderTexture TEX_PT_NORMALS_B;

        public RenderTexture AlbedoColorA;
        public RenderTexture AlbedoColorB;

        public RenderTexture PT_LF1;
        public RenderTexture PT_LF2;

        public RenderTexture TEX_PT_COLOR_HF;
        public RenderTexture TEX_PT_COLOR_SPEC;




        public int ScreenWidth;
        public int ScreenHeight;
        public int TargetWidth;
        public int TargetHeight;
        public ComputeShader shader;

        private int CopyData;
        public int Reproject;
        public int Gradient_Img;
        private int Gradient_Atrous;
        private int Temporal;
        private int Atrous_LF;
        private int Atrous;
        private int SpecCopy;
        private int CalcQuart;
        private Vector3 PrevCamPos;

        public void ClearAll()
        {
            if (ASVGF_HIST_COLOR_HF != null)
            {
                ASVGF_HIST_COLOR_HF.Release();
                ASVGF_ATROUS_PING_LF_SH.Release();
                ASVGF_ATROUS_PONG_LF_SH.Release();
                ASVGF_ATROUS_PING_LF_COCG.Release();
                ASVGF_ATROUS_PONG_LF_COCG.Release();
                ASVGF_ATROUS_PING_HF.Release();
                ASVGF_ATROUS_PONG_HF.Release();
                ASVGF_ATROUS_PONG_HF.Release();
                ASVGF_ATROUS_PING_SPEC.Release();
                ASVGF_ATROUS_PONG_SPEC.Release();
                ASVGF_ATROUS_PING_MOMENTS.Release();
                ASVGF_ATROUS_PONG_MOMENTS.Release();
                ASVGF_GRAD_LF_PING.Release();
                ASVGF_GRAD_LF_PONG.Release();
                ASVGF_GRAD_HF_SPEC_PING.Release();
                ASVGF_GRAD_HF_SPEC_PONG.Release();
                ASVGF_FILTERED_SPEC_A.Release();
                ASVGF_FILTERED_SPEC_B.Release();
                ASVGF_HIST_MOMENTS_HF_A.Release();
                ASVGF_HIST_MOMENTS_HF_B.Release();
                ASVGF_HIST_COLOR_LF_SH_A.Release();
                ASVGF_HIST_COLOR_LF_SH_B.Release();
                ASVGF_HIST_COLOR_LF_COCG_A.Release();
                ASVGF_HIST_COLOR_LF_COCG_B.Release();
                ASVGF_GRAD_SMPL_POS_A.Release();
                ASVGF_GRAD_SMPL_POS_B.Release();
                PT_LF1.Release();
                PT_LF2.Release();
                TEX_PT_COLOR_HF.Release();
                TEX_PT_COLOR_SPEC.Release();
                MetallicA.Release();
                MetallicB.Release();
                ReflectedRefractedA.Release();
                ReflectedRefractedB.Release();
                TEX_PT_NORMALS_A.Release();
                TEX_PT_NORMALS_B.Release();
                AlbedoColorA.Release();
                AlbedoColorB.Release();
                LFVarianceA.Release();
                LFVarianceB.Release();
                Quartiles.Release();
            }
            Initialized = false;
        }

        public int iter;
        public void init(int ScreenWidth, int ScreenHeight, int TargetWidth, int TargetHeight)
        {
            this.ScreenWidth = ScreenWidth;
            this.ScreenHeight = ScreenHeight;
            this.TargetWidth = TargetWidth;
            this.TargetHeight = TargetHeight;
            iter = 0;
            if (shader == null) { shader = Resources.Load<ComputeShader>("PostProcess/ASVGF/ASVGF"); }
            CopyData = shader.FindKernel("CopyData");
            Reproject = shader.FindKernel("Reproject");
            Gradient_Img = shader.FindKernel("Gradient_Img");
            Gradient_Atrous = shader.FindKernel("Gradient_Atrous");
            Temporal = shader.FindKernel("Temporal");
            Atrous_LF = shader.FindKernel("Atrous_LF");
            Atrous = shader.FindKernel("Atrous");
            SpecCopy = shader.FindKernel("TempCopyKernel");
            CalcQuart = shader.FindKernel("CalcPerc");
            shader.SetInt("screen_width", ScreenWidth);
            shader.SetInt("screen_height", ScreenHeight);

            shader.SetInt("TargetWidth", TargetWidth);
            shader.SetInt("TargetHeight", TargetHeight);


            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_COLOR_HF, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref TEX_PT_NORMALS_A, ScreenWidth, ScreenHeight, CommonFunctions.RTFull2);
            CommonFunctions.CreateRenderTexture(ref TEX_PT_NORMALS_B, ScreenWidth, ScreenHeight, CommonFunctions.RTFull2);

            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PING_LF_SH, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PONG_LF_SH, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTFull4);

            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PING_LF_COCG, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PONG_LF_COCG, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);

            CommonFunctions.CreateRenderTexture(ref PT_LF1, ScreenWidth, ScreenHeight, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref PT_LF2, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
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
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_SMPL_POS_A, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTFull1);
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_SMPL_POS_B, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTFull1);
            CommonFunctions.CreateRenderTexture(ref TEX_PT_COLOR_HF, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref TEX_PT_COLOR_SPEC, ScreenWidth, ScreenHeight, CommonFunctions.RTInt1);
            CommonFunctions.CreateRenderTexture(ref MetallicA, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref MetallicB, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ReflectedRefractedA, ScreenWidth, ScreenHeight, CommonFunctions.RTFull1);
            CommonFunctions.CreateRenderTexture(ref ReflectedRefractedB, ScreenWidth, ScreenHeight, CommonFunctions.RTFull1);
            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_MOMENTS_HF_B, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_MOMENTS_HF_A, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref LFVarianceA, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref LFVarianceB, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PONG_MOMENTS, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PING_MOMENTS, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_LF_PING, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_LF_PONG, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_HF_SPEC_PING, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_HF_SPEC_PONG, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref Quartiles, ScreenWidth / 8, ScreenHeight / 8, CommonFunctions.RTHalf2);
            Initialized = true;
        }

        Vector3 prevEuler;
        Vector3 PrevPos;
        public void DoRNG(ref RenderTexture RNGTex, ref RenderTexture RNGTexB, int CurFrame, ComputeBuffer GlobalRays, CommandBuffer cmd, RenderTexture PrimaryTriData, ComputeBuffer Meshes, ComputeBuffer Tris, bool UseBackupPointSelection, ComputeBuffer MeshIndexes, int ScreenWidth, int ScreenHeight, ComputeBuffer MeshesB, RenderTexture CorrectedDistanceTexA, RenderTexture CorrectedDistanceTexB, ComputeBuffer _ColorBuffer)
        {
            // this.ScreenWidth = ScreenWidth;
            // this.ScreenHeight = ScreenHeight;
            if (shader == null) { shader = Resources.Load<ComputeShader>("PostProcess/ASVGF/ASVGF"); }
            shader.SetInt("screen_width", ScreenWidth);
            shader.SetInt("screen_height", ScreenHeight);

            shader.SetInt("TargetWidth", ScreenWidth);
            shader.SetInt("TargetHeight", ScreenHeight);
            bool EvenFrame = CurFrame % 2 == 0;
            Vector3 Euler = RayTracingMaster._camera.transform.eulerAngles;
            Vector3 Pos = RayTracingMaster._camera.transform.position;
            shader.SetMatrix("viewprojection", RayTracingMaster._camera.projectionMatrix * RayTracingMaster._camera.worldToCameraMatrix);
            RayTracingMaster._camera.transform.eulerAngles = prevEuler; 
            RayTracingMaster._camera.transform.position = PrevPos; 
            shader.SetMatrix("prevviewprojection", RayTracingMaster._camera.projectionMatrix * RayTracingMaster._camera.worldToCameraMatrix);
            shader.SetMatrix("CamToWorldPrev", RayTracingMaster._camera.cameraToWorldMatrix);
            shader.SetMatrix("CamInvProjPrev", RayTracingMaster._camera.projectionMatrix.inverse);
            RayTracingMaster._camera.transform.eulerAngles = Euler; 
            RayTracingMaster._camera.transform.position = Pos; 
            prevEuler = Euler;
            PrevPos = Pos;
            shader.SetBool("UseBackupPointSelection", UseBackupPointSelection);
            shader.SetFloat("CameraDist", Vector3.Distance(RayTracingMaster._camera.transform.position, PrevCamPos));
            shader.SetMatrix("CamToWorld", RayTracingMaster._camera.cameraToWorldMatrix);
            shader.SetMatrix("CamInvProj", RayTracingMaster._camera.projectionMatrix.inverse);
            shader.SetVector("Forward", RayTracingMaster._camera.transform.forward);
            shader.SetFloat("FarPlane", RayTracingMaster._camera.farClipPlane);
            shader.SetFloat("NearPlane", RayTracingMaster._camera.nearClipPlane);
            shader.SetVector("PrevCamPos", PrevCamPos);
            shader.SetVector("CamPos", RayTracingMaster._camera.transform.position);

            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ASVGF Reproject Gradients Kernel");
            cmd.SetComputeIntParam(shader, "CurFrame", CurFrame);

            shader.SetTextureFromGlobal(Reproject, "TEX_PT_MOTION", "TTMotionVectorTexture");
            cmd.SetComputeTextureParam(shader, Reproject, "ReflRefracA", (EvenFrame ? ReflectedRefractedA : ReflectedRefractedB));
            cmd.SetComputeBufferParam(shader, Reproject, "GlobalColorsRead", _ColorBuffer);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_VIEW_DEPTH_A", EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_VIEW_DEPTH_B", !EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_NORMALS_B", (!EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, Reproject, "IMG_ASVGF_GRAD_SMPL_POS_A", (EvenFrame ? ASVGF_GRAD_SMPL_POS_A : ASVGF_GRAD_SMPL_POS_B));
            cmd.SetComputeTextureParam(shader, Reproject, "IMG_ASVGF_GRAD_HF_SPEC_PING", ASVGF_GRAD_HF_SPEC_PING);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_COLOR_HF", TEX_PT_COLOR_HF);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_COLOR_SPEC", TEX_PT_COLOR_SPEC);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_ASVGF_GRAD_SMPL_POS_B", (!EvenFrame ? ASVGF_GRAD_SMPL_POS_A : ASVGF_GRAD_SMPL_POS_B));
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_ASVGF_GRAD_SMPL_POS_A", (EvenFrame ? ASVGF_GRAD_SMPL_POS_A : ASVGF_GRAD_SMPL_POS_B));
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_COLOR_LF_SHWrite", PT_LF1);
            cmd.SetComputeTextureParam(shader, Reproject, "IMG_ASVGF_GRAD_LF_PING", ASVGF_GRAD_LF_PING);
            cmd.SetComputeTextureParam(shader, Reproject, "RNGTexA", (EvenFrame) ? RNGTex : RNGTexB);
            cmd.SetComputeTextureParam(shader, Reproject, "RNGTexB", (!EvenFrame) ? RNGTex : RNGTexB);
            cmd.SetComputeTextureParam(shader, Reproject, "MetallicAWrite", (EvenFrame ? MetallicA : MetallicB));
            cmd.SetComputeTextureParam(shader, Reproject, "ReflRefracB", (!EvenFrame ? ReflectedRefractedA : ReflectedRefractedB));
            cmd.SetComputeTextureParam(shader, Reproject, "MetallicB", (!EvenFrame ? MetallicA : MetallicB));
            cmd.SetComputeTextureParam(shader, Reproject, "PrimaryTriData", PrimaryTriData);
            cmd.SetComputeBufferParam(shader, Reproject, "_MeshData", Meshes);
            cmd.SetComputeBufferParam(shader, Reproject, "_MeshDataB", Meshes);
            cmd.SetComputeBufferParam(shader, Reproject, "AggTrisA", Tris);
            cmd.SetComputeBufferParam(shader, Reproject, "MeshIndexes", MeshIndexes);
            shader.SetVector("CamDiff", PrevPos - RayTracingMaster._camera.transform.position);
            cmd.SetComputeBufferParam(shader, Reproject, "GlobalRaysMini", GlobalRays);



            cmd.DispatchCompute(shader, Reproject, Mathf.CeilToInt((ScreenWidth) / 24.0f), Mathf.CeilToInt((ScreenHeight) / 24.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ASVGF Reproject Gradients Kernel");
            PrevPos = RayTracingMaster._camera.transform.position;
        }


        public void Do(ref ComputeBuffer _ColorBuffer, 
                        ref RenderTexture Output, 
                        float ResolutionRatio, 
                        RenderTexture ScreenSpaceInfo, 
                        CommandBuffer cmd, 
                        int CurFrame, 
                        ref RenderTexture WorldPosData, 
                        int PartialRenderingFactor, 
                        ComputeBuffer ExposureModifier, 
                        bool DoExposure, 
                        float IndirectBoost, 
                        RenderTexture RNGTex,
                        int UpscalerMethod, 
                        RenderTexture CorrectedDistanceTexA, 
                        RenderTexture CorrectedDistanceTexB,
                        RenderTexture PSRGbuffer)
        {

            RayTracingMaster._camera = RayTracingMaster._camera;
            bool EvenFrame = CurFrame % 2 == 0;
            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ASVGF Copy Data Kernel");
            int MaxIterations = 4;
            cmd.SetComputeIntParam(shader, "MaxIterations", MaxIterations);
            cmd.SetComputeIntParam(shader, "UpscalerMethod", UpscalerMethod);
            cmd.SetComputeFloatParam(shader, "ResRatio", ResolutionRatio);
            shader.SetBool("UseExposure", DoExposure);
            shader.SetFloat("IndirectBoost", IndirectBoost);
            shader.SetBuffer(CopyData, "GlobalColorsRead", _ColorBuffer);
            shader.SetBuffer(Atrous, "GlobalColorsRead", _ColorBuffer);
            shader.SetTexture(Atrous, "ReflRefracA", (EvenFrame ? ReflectedRefractedA : ReflectedRefractedB));
            shader.SetBuffer(CopyData, "ExposureBuffer", ExposureModifier);
            shader.SetBuffer(Atrous, "ExposureBuffer", ExposureModifier);
            shader.SetTexture(CopyData, "ScreenSpaceInfo", ScreenSpaceInfo);
            shader.SetTexture(Temporal, "ScreenSpaceInfo", ScreenSpaceInfo);
            shader.SetInt("PartialRenderingFactor", PartialRenderingFactor);
            cmd.SetComputeTextureParam(shader, CopyData, "WorldPosData", WorldPosData);
            cmd.SetComputeTextureParam(shader, Atrous, "WorldPosData", WorldPosData);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_LF_SHWrite", PT_LF1);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_LF_COCGWrite", PT_LF2);
            cmd.SetComputeTextureParam(shader, CopyData, "PSRGBuff", PSRGbuffer);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_HFWrite", TEX_PT_COLOR_HF);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_SPECWrite", TEX_PT_COLOR_SPEC);
            cmd.SetComputeTextureParam(shader, CopyData, "RNGTexB", RNGTex);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_NORMALS_AWrite", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_VIEW_DEPTH_B", !EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);
            cmd.SetComputeTextureParam(shader, CopyData, "WRITEDEPTHOVERRIDE", EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_NORMALS_B", (!EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, CopyData, "ReflRefracB", (!EvenFrame ? ReflectedRefractedA : ReflectedRefractedB));
            shader.SetTextureFromGlobal(CopyData, "TEX_PT_MOTION", "TTMotionVectorTexture");
            cmd.SetComputeTextureParam(shader, CopyData, "MetallicAWrite", (EvenFrame ? MetallicA : MetallicB));
            cmd.SetComputeTextureParam(shader, CopyData, "ReflRefracAWrite", (EvenFrame ? ReflectedRefractedA : ReflectedRefractedB));

            cmd.SetComputeTextureParam(shader, CopyData, "AlbedoColorA", (EvenFrame ? AlbedoColorA : AlbedoColorB));
            cmd.SetComputeTextureParam(shader, CopyData, "AlbedoColorB", (!EvenFrame ? AlbedoColorA : AlbedoColorB));

            cmd.DispatchCompute(shader, CopyData, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ASVGF Copy Data Kernel");

            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ASVGF Quart Kernel");
            cmd.SetComputeTextureParam(shader, CalcQuart, "TEX_PT_COLOR_SPECWrite", TEX_PT_COLOR_SPEC);
            cmd.SetComputeTextureParam(shader, CalcQuart, "TEX_PT_COLOR_LF_SH", PT_LF1);
            cmd.SetComputeTextureParam(shader, CalcQuart, "TEX_PT_COLOR_HF", TEX_PT_COLOR_HF);
            cmd.SetComputeTextureParam(shader, CalcQuart, "img_quartiles", Quartiles);
            cmd.DispatchCompute(shader, CalcQuart, Mathf.CeilToInt(ScreenWidth / 8.0f), Mathf.CeilToInt(ScreenHeight / 8.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ASVGF Quart Kernel");





            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ASVGF Grad Compute Kernel");
            shader.SetTextureFromGlobal(Gradient_Img, "TEX_PT_MOTION", "TTMotionVectorTexture");
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_ASVGF_GRAD_SMPL_POS_A", (EvenFrame ? ASVGF_GRAD_SMPL_POS_A : ASVGF_GRAD_SMPL_POS_B));
            cmd.SetComputeTextureParam(shader, Gradient_Img, "IMG_ASVGF_GRAD_LF_PING", ASVGF_GRAD_LF_PING);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "IMG_ASVGF_GRAD_HF_SPEC_PING", ASVGF_GRAD_HF_SPEC_PING);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_COLOR_HFWrite", TEX_PT_COLOR_HF);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_COLOR_SPECWrite", TEX_PT_COLOR_SPEC);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_COLOR_LF_SH", PT_LF1);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_ASVGF_HIST_COLOR_LF_SH_B", (!EvenFrame ? ASVGF_HIST_COLOR_LF_SH_A : ASVGF_HIST_COLOR_LF_SH_B));
            cmd.SetComputeTextureParam(shader, Gradient_Img, "AlbedoColorA", (EvenFrame ? AlbedoColorA : AlbedoColorB));
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_NORMALS_AWrite", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));


            cmd.DispatchCompute(shader, Gradient_Img, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ASVGF Grad Compute Kernel");

            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ASVGF Grad Atrous Kernels");
            for (int i = 0; i < 7; i++)
            {
                var e = i;
                bool Flip = e % 2 == 0;
                cmd.SetComputeIntParam(shader, "iteration", e);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "TEX_ASVGF_GRAD_LF_PING", Flip ? ASVGF_GRAD_LF_PING : ASVGF_GRAD_LF_PONG);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "TEX_ASVGF_GRAD_HF_SPEC_PING", Flip ? ASVGF_GRAD_HF_SPEC_PING : ASVGF_GRAD_HF_SPEC_PONG);

                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "IMG_ASVGF_GRAD_LF_PING", Flip ? ASVGF_GRAD_LF_PONG : ASVGF_GRAD_LF_PING);
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "IMG_ASVGF_GRAD_HF_SPEC_PING", Flip ? ASVGF_GRAD_HF_SPEC_PONG : ASVGF_GRAD_HF_SPEC_PING);

                cmd.DispatchCompute(shader, Gradient_Atrous, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);
            }
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ASVGF Grad Atrous Kernels");


            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ASVGF Temporal Kernel");
            shader.SetTextureFromGlobal(Temporal, "TEX_PT_MOTION", "TTMotionVectorTexture");
            cmd.SetComputeTextureParam(shader, Temporal, "quart_read", Quartiles);
            cmd.SetComputeTextureParam(shader, Temporal, "ReflRefracA", (EvenFrame ? ReflectedRefractedA : ReflectedRefractedB));
            cmd.SetComputeTextureParam(shader, Temporal, "ReflRefracB", (!EvenFrame ? ReflectedRefractedA : ReflectedRefractedB));
            cmd.SetComputeTextureParam(shader, Temporal, "MetallicA", (EvenFrame ? MetallicA : MetallicB));
            cmd.SetComputeTextureParam(shader, Temporal, "MetallicB", (!EvenFrame ? MetallicA : MetallicB));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_VIEW_DEPTH_A", EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_VIEW_DEPTH_B", !EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_NORMALS_A", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_NORMALS_B", (!EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_COLOR_HF", TEX_PT_COLOR_HF);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_COLOR_LF_SH_B", (!EvenFrame ? ASVGF_HIST_COLOR_LF_SH_A : ASVGF_HIST_COLOR_LF_SH_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_COLOR_LF_COCG_B", (!EvenFrame ? ASVGF_HIST_COLOR_LF_COCG_A : ASVGF_HIST_COLOR_LF_COCG_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_COLOR_HF", ASVGF_HIST_COLOR_HF);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_FILTERED_SPEC_B", (!EvenFrame ? ASVGF_FILTERED_SPEC_A : ASVGF_FILTERED_SPEC_B));
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_SPEC2", (EvenFrame ? ASVGF_FILTERED_SPEC_A : ASVGF_FILTERED_SPEC_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_MOMENTS_HF_B", (!EvenFrame ? ASVGF_HIST_MOMENTS_HF_A : ASVGF_HIST_MOMENTS_HF_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_COLOR_LF_SH", PT_LF1);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_COLOR_LF_COCG", PT_LF2);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_COLOR_SPEC", TEX_PT_COLOR_SPEC);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_HIST_MOMENTS_HF_A", (EvenFrame ? ASVGF_HIST_MOMENTS_HF_A : ASVGF_HIST_MOMENTS_HF_B));
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_HIST_COLOR_LF_SH_A", (EvenFrame ? ASVGF_HIST_COLOR_LF_SH_A : ASVGF_HIST_COLOR_LF_SH_B));
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_HIST_COLOR_LF_COCG_A", (EvenFrame ? ASVGF_HIST_COLOR_LF_COCG_A : ASVGF_HIST_COLOR_LF_COCG_B));
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_HF", ASVGF_ATROUS_PING_HF);
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_MOMENTS", ASVGF_ATROUS_PING_MOMENTS);
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PING_LF_SH);
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PING_LF_COCG);
            
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_ATROUS_PONG_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_ATROUS_PONG_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);
            
            cmd.DispatchCompute(shader, Temporal, Mathf.CeilToInt((ScreenWidth + 14) / 15.0f), Mathf.CeilToInt((ScreenHeight + 14) / 15.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ASVGF Temporal Kernel");

            shader.SetBool("DiffRes", ResolutionRatio != 1.0f);
            // cmd.CopyTexture((EvenFrame ? ASVGF_FILTERED_SPEC_A : ASVGF_FILTERED_SPEC_B), ASVGF_ATROUS_PING_SPEC);
            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("COPY A");
            cmd.SetComputeTextureParam(shader, SpecCopy, "TEX_ASVGF_FILTERED_SPEC_B", (EvenFrame ? ASVGF_FILTERED_SPEC_A : ASVGF_FILTERED_SPEC_B));
            cmd.SetComputeTextureParam(shader, SpecCopy, "IMG_ASVGF_ATROUS_PING_SPEC", ASVGF_ATROUS_PING_SPEC);
            cmd.DispatchCompute(shader, SpecCopy, Mathf.CeilToInt((ScreenWidth) / 32.0f), Mathf.CeilToInt((ScreenHeight) / 32.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("COPY A");

            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ASVGF Atrous LF: " + 0);
            cmd.SetComputeIntParam(shader, "iteration", 0);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_PT_NORMALS_A", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_PT_VIEW_DEPTH_A", EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);
            shader.SetTextureFromGlobal(Atrous_LF, "TEX_PT_MOTION", "TTMotionVectorTexture");

            cmd.SetComputeTextureParam(shader, Atrous_LF, "MetallicA", (EvenFrame ? MetallicA : MetallicB));
            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PING_LF_SH);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PING_LF_COCG);

            cmd.SetComputeTextureParam(shader, Atrous_LF, "LFVarianceA", (0 % 2 == 0 ? LFVarianceA : LFVarianceB));
            cmd.SetComputeTextureParam(shader, Atrous_LF, "LFVarianceB", (0 % 2 == 1 ? LFVarianceA : LFVarianceB));

            cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PONG_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PONG_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_HIST_MOMENTS_HF_A", (EvenFrame ? ASVGF_HIST_MOMENTS_HF_A : ASVGF_HIST_MOMENTS_HF_B));
            cmd.DispatchCompute(shader, Atrous_LF, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ASVGF Atrous LF: " + 0);

            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ASVGF Atrous " + 0);

                cmd.SetComputeIntParam(shader, "spec_iteration", 0);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_PT_NORMALS_A", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_PT_VIEW_DEPTH_A", EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);
                shader.SetTextureFromGlobal(Atrous, "TEX_PT_MOTION", "TTMotionVectorTexture");
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_HIST_MOMENTS_HF_A", (EvenFrame ? ASVGF_HIST_MOMENTS_HF_A : ASVGF_HIST_MOMENTS_HF_B));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_HIST_COLOR_LF_SH_A", (EvenFrame ? ASVGF_HIST_COLOR_LF_SH_A : ASVGF_HIST_COLOR_LF_SH_B));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_HIST_COLOR_LF_COCG_A", (EvenFrame ? ASVGF_HIST_COLOR_LF_COCG_A : ASVGF_HIST_COLOR_LF_COCG_B));


                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_HF", (0 == 1) ? ASVGF_HIST_COLOR_HF : ((0 % 2 == 0) ? ASVGF_ATROUS_PING_HF : ASVGF_ATROUS_PONG_HF));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_SPEC", ((0 % 2 == 0) ? ASVGF_ATROUS_PING_SPEC : ASVGF_ATROUS_PONG_SPEC));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_MOMENTS", ((0 % 2 == 0) ? ASVGF_ATROUS_PING_MOMENTS : ASVGF_ATROUS_PONG_MOMENTS));


                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PING_HF", (0 == 0) ? ASVGF_HIST_COLOR_HF : ((0 % 2 == 1) ? ASVGF_ATROUS_PING_HF : ASVGF_ATROUS_PONG_HF));
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PING_SPEC", ((0 % 2 == 1) ? ASVGF_ATROUS_PING_SPEC : ASVGF_ATROUS_PONG_SPEC));
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PING_MOMENTS", ((0 % 2 == 1) ? ASVGF_ATROUS_PING_MOMENTS : ASVGF_ATROUS_PONG_MOMENTS));


                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_COLOR", Output);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);
                cmd.SetComputeTextureParam(shader, Atrous, "MetallicA", (EvenFrame ? MetallicA : MetallicB));
                cmd.SetComputeTextureParam(shader, Atrous, "MetallicB", (!EvenFrame ? MetallicA : MetallicB));

                cmd.SetComputeTextureParam(shader, Atrous, "AlbedoColorB", (EvenFrame ? AlbedoColorA : AlbedoColorB));
                shader.SetTexture(Atrous, "ScreenSpaceInfo", ScreenSpaceInfo);
                cmd.DispatchCompute(shader, Atrous, Mathf.CeilToInt((ScreenWidth + 15) / 8.0f), Mathf.CeilToInt((ScreenHeight + 15) / 8.0f), 1);
                if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ASVGF Atrous " + 0);

            for(int i = 0; i < 6; i++) {
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
            }

            for (int i = 0; i < MaxIterations; i++)
            {
                var e = i;

                if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ASVGF Atrous " + (e + 1));

                cmd.SetComputeIntParam(shader, "spec_iteration", (e + 1));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_PT_NORMALS_A", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_PT_VIEW_DEPTH_A", EvenFrame ? CorrectedDistanceTexA : CorrectedDistanceTexB);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_HIST_MOMENTS_HF_A", (EvenFrame ? ASVGF_HIST_MOMENTS_HF_A : ASVGF_HIST_MOMENTS_HF_B));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_HIST_COLOR_LF_SH_A", (EvenFrame ? ASVGF_HIST_COLOR_LF_SH_A : ASVGF_HIST_COLOR_LF_SH_B));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_HIST_COLOR_LF_COCG_A", (EvenFrame ? ASVGF_HIST_COLOR_LF_COCG_A : ASVGF_HIST_COLOR_LF_COCG_B));


                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_HF", ((e + 1) == 1) ? ASVGF_HIST_COLOR_HF : (((e + 1) % 2 == 0) ? ASVGF_ATROUS_PING_HF : ASVGF_ATROUS_PONG_HF));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_SPEC", (((e + 1) % 2 == 0) ? ASVGF_ATROUS_PING_SPEC : ASVGF_ATROUS_PONG_SPEC));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_MOMENTS", (((e + 1) % 2 == 0) ? ASVGF_ATROUS_PING_MOMENTS : ASVGF_ATROUS_PONG_MOMENTS));


                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PING_HF", ((e + 1) == 0) ? ASVGF_HIST_COLOR_HF : (((e + 1) % 2 == 1) ? ASVGF_ATROUS_PING_HF : ASVGF_ATROUS_PONG_HF));
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PING_SPEC", (((e + 1) % 2 == 1) ? ASVGF_ATROUS_PING_SPEC : ASVGF_ATROUS_PONG_SPEC));
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PING_MOMENTS", (((e + 1) % 2 == 1) ? ASVGF_ATROUS_PING_MOMENTS : ASVGF_ATROUS_PONG_MOMENTS));


                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_COLOR", Output);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);
                cmd.SetComputeTextureParam(shader, Atrous, "MetallicA", (EvenFrame ? MetallicA : MetallicB));
                cmd.SetComputeTextureParam(shader, Atrous, "MetallicB", (!EvenFrame ? MetallicA : MetallicB));

                cmd.SetComputeTextureParam(shader, Atrous, "AlbedoColorB", (EvenFrame ? AlbedoColorA : AlbedoColorB));
                shader.SetTexture(Atrous, "ScreenSpaceInfo", ScreenSpaceInfo);
                cmd.DispatchCompute(shader, Atrous, Mathf.CeilToInt((ScreenWidth + 15) / 8.0f), Mathf.CeilToInt((ScreenHeight + 15) / 8.0f), 1);
                if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ASVGF Atrous " + (e + 1));
            }


            iter++;

            PrevCamPos = RayTracingMaster._camera.transform.position;



        }

    }
}