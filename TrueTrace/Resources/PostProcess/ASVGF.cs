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
        public RenderTexture ASVGF_COLOR;
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


        public RenderTexture FDepth;

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

        public RenderTexture ReflectedRefractedTex;
        public RenderTexture ReflectedRefractedTexPrev;

        public RenderTexture DebugTex;


        public Camera camera;

        public int ScreenWidth;
        public int ScreenHeight;
        public ComputeShader shader;

        private int CopyData;
        private int CopyRadiance;
        public int Reproject;
        public int Gradient_Img;
        private int Gradient_Atrous;
        private int Temporal;
        private int Atrous_LF;
        private int Finalize;
        private int Atrous;
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
                ASVGF_COLOR.Release();
                ASVGF_GRAD_LF_PING.Release();
                ASVGF_GRAD_LF_PONG.Release();
                ASVGF_GRAD_HF_SPEC_PING.Release();
                ASVGF_GRAD_HF_SPEC_PONG.Release();
                FDepth.Release();
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
                DebugTex.Release();
                TEX_PT_COLOR_SPEC.Release();
                MetallicA.Release();
                MetallicB.Release();
                TEX_PT_NORMALS_A.Release();
                TEX_PT_NORMALS_B.Release();
                ReflectedRefractedTex.Release();
                ReflectedRefractedTexPrev.Release();
                AlbedoColorA.Release();
                AlbedoColorB.Release();
                LFVarianceA.Release();
                LFVarianceB.Release();
            }
            Initialized = false;
        }


        private void CreateComputeBuffer<T>(ref ComputeBuffer buffer, T[] data, int stride)
            where T : struct
        {
            // Do we already have a compute buffer?
            if (buffer != null)
            {
                // If no data or buffer doesn't match the given criteria, release it
                if (data.Length == 0 || buffer.count != data.Length || buffer.stride != stride)
                {
                    buffer.Release();
                    buffer = null;
                }
            }

            if (data.Length != 0)
            {
                // If the buffer has been released or wasn't there to
                // begin with, create it
                if (buffer == null)
                {
                    buffer = new ComputeBuffer(data.Length, stride);
                }
                // Set data on the buffer
                buffer.SetData(data);
            }
        }

        private void CreateRenderTexture(ref RenderTexture ThisTex)
        {
            ThisTex = new RenderTexture(ScreenWidth, ScreenHeight, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.useMipMap = false;
            ThisTex.Create();
        }

        private void CreateRenderTextureMask(ref RenderTexture ThisTex)
        {
            ThisTex = new RenderTexture(ScreenWidth, ScreenHeight, 0,
                RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.useMipMap = false;
            ThisTex.Create();
        }

        private void CreateRenderTextureGrad(ref RenderTexture ThisTex)
        {
            ThisTex = new RenderTexture(ScreenWidth / 3, ScreenHeight / 3, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.useMipMap = false;
            ThisTex.Create();
        }
        private void CreateRenderTextureGradDouble(ref RenderTexture ThisTex)
        {
            ThisTex = new RenderTexture(ScreenWidth / 3, ScreenHeight / 3, 0,
                RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.useMipMap = false;
            ThisTex.Create();
        }
        private void CreateRenderTextureGradInt(ref RenderTexture ThisTex)
        {
            ThisTex = new RenderTexture(ScreenWidth / 3, ScreenHeight / 3, 0,
                RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.useMipMap = false;
            ThisTex.Create();
        }
        private void CreateRenderTextureInt(ref RenderTexture ThisTex)
        {
            ThisTex = new RenderTexture(ScreenWidth, ScreenHeight, 0,
                RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }
        private void CreateRenderTextureInt2(ref RenderTexture ThisTex)
        {
            ThisTex = new RenderTexture(ScreenWidth, ScreenHeight, 0,
                RenderTextureFormat.RGInt, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }
        private void CreateRenderTextureDouble(ref RenderTexture ThisTex)
        {
            ThisTex = new RenderTexture(ScreenWidth, ScreenHeight, 0,
                RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }
        private void CreateRenderTextureGradSingle(ref RenderTexture ThisTex)
        {
            ThisTex = new RenderTexture(ScreenWidth / 3, ScreenHeight / 3, 0,
                RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }

        private void CreateRenderTextureSingle(ref RenderTexture ThisTex)
        {
            ThisTex = new RenderTexture(ScreenWidth, ScreenHeight, 0,
                RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }

        private void CreateRenderTextureSingleHalf(ref RenderTexture ThisTex)
        {
            ThisTex = new RenderTexture(ScreenWidth, ScreenHeight, 0,
                RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }

        public int iter;
        public void init(int ScreenWidth, int ScreenHeight, Camera camera)
        {
            this.ScreenWidth = ScreenWidth;
            this.ScreenHeight = ScreenHeight;
            this.camera = camera;
            iter = 0;
            if (shader == null) { shader = Resources.Load<ComputeShader>("PostProcess/ASVGF"); }
            CopyData = shader.FindKernel("CopyData");
            CopyRadiance = shader.FindKernel("CopyRadiance");
            Reproject = shader.FindKernel("Reproject");
            Gradient_Img = shader.FindKernel("Gradient_Img");
            Gradient_Atrous = shader.FindKernel("Gradient_Atrous");
            Temporal = shader.FindKernel("Temporal");
            Atrous_LF = shader.FindKernel("Atrous_LF");
            Finalize = shader.FindKernel("Finalize");
            Atrous = shader.FindKernel("Atrous");
            shader.SetInt("screen_width", ScreenWidth);
            shader.SetInt("screen_height", ScreenHeight);


            CreateRenderTextureMask(ref ASVGF_HIST_COLOR_HF);
            CreateRenderTextureInt2(ref TEX_PT_NORMALS_A);
            CreateRenderTextureInt2(ref TEX_PT_NORMALS_B);

            CreateRenderTextureGrad(ref ASVGF_ATROUS_PING_LF_SH);
            CreateRenderTextureGrad(ref ASVGF_ATROUS_PONG_LF_SH);
            CreateRenderTextureGradDouble(ref ASVGF_ATROUS_PING_LF_COCG);
            CreateRenderTextureGradDouble(ref ASVGF_ATROUS_PONG_LF_COCG);
            CreateRenderTexture(ref PT_LF1);
            CreateRenderTextureInt2(ref AlbedoColorA);
            CreateRenderTextureInt2(ref AlbedoColorB);
            CreateRenderTextureDouble(ref PT_LF2);
            CreateRenderTextureMask(ref ASVGF_ATROUS_PING_HF);
            CreateRenderTextureMask(ref ASVGF_ATROUS_PONG_HF);
            CreateRenderTextureDouble(ref ASVGF_ATROUS_PING_SPEC);
            CreateRenderTextureDouble(ref ASVGF_ATROUS_PONG_SPEC);
            CreateRenderTexture(ref ASVGF_COLOR);
            CreateRenderTextureDouble(ref ASVGF_FILTERED_SPEC_A);
            CreateRenderTextureDouble(ref ASVGF_FILTERED_SPEC_B);
            CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_SH_A);
            CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_SH_B);
            CreateRenderTextureDouble(ref ASVGF_HIST_COLOR_LF_COCG_A);
            CreateRenderTextureDouble(ref ASVGF_HIST_COLOR_LF_COCG_B);
            CreateRenderTextureGradSingle(ref ASVGF_GRAD_SMPL_POS_A);
            CreateRenderTextureGradSingle(ref ASVGF_GRAD_SMPL_POS_B);
            CreateRenderTextureMask(ref TEX_PT_COLOR_HF);
            CreateRenderTexture(ref DebugTex);
            CreateRenderTextureMask(ref TEX_PT_COLOR_SPEC);
            CommonFunctions.CreateRenderTexture(ref MetallicA, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref MetallicB, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ReflectedRefractedTex, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf1);
            CommonFunctions.CreateRenderTexture(ref ReflectedRefractedTexPrev, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf1);
            CommonFunctions.CreateRenderTexture(ref FDepth, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf1);
            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_MOMENTS_HF_B, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref ASVGF_HIST_MOMENTS_HF_A, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4);
            CommonFunctions.CreateRenderTexture(ref LFVarianceA, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf1);
            CommonFunctions.CreateRenderTexture(ref LFVarianceB, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf1);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PONG_MOMENTS, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_ATROUS_PING_MOMENTS, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_LF_PING, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_LF_PONG, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_HF_SPEC_PING, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref ASVGF_GRAD_HF_SPEC_PONG, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            Initialized = true;
        }

        Vector3 prevEuler;
        Vector3 PrevPos;
        public void DoRNG(ref RenderTexture RNGTex, ref RenderTexture RNGTexB, int CurFrame, ref ComputeBuffer GlobalRays, ref ComputeBuffer GlobalRaysB, RenderTexture TEX_PT_VIEW_DEPTH_B, CommandBuffer cmd, RenderTexture CorrectedDepthTex, RenderTexture PrimaryTriData, ComputeBuffer Meshes, ComputeBuffer Tris, bool UseBackupPointSelection)
        {
            bool EvenFrame = CurFrame % 2 == 0;
            Vector3 Euler = camera.transform.eulerAngles;
            shader.SetMatrix("viewprojection", camera.projectionMatrix * camera.worldToCameraMatrix);
            camera.transform.eulerAngles = prevEuler; 
            shader.SetMatrix("prevviewprojection", camera.projectionMatrix * camera.worldToCameraMatrix);
            camera.transform.eulerAngles = Euler; 
            prevEuler = Euler;
            shader.SetBool("UseBackupPointSelection", UseBackupPointSelection);
            shader.SetFloat("CameraDist", Vector3.Distance(camera.transform.position, PrevCamPos));
            shader.SetMatrix("_CameraToWorld", camera.cameraToWorldMatrix);
            shader.SetMatrix("_CameraInverseProjection", camera.projectionMatrix.inverse);
            shader.SetTextureFromGlobal(CopyRadiance, "NormalTex", "_CameraGBufferTexture2");
            shader.SetTextureFromGlobal(CopyRadiance, "MotionVectors", "_CameraMotionVectorsTexture");
            shader.SetFloat("FarPlane", camera.farClipPlane);
            cmd.BeginSample("ASVGF Copy Radiance Kernel");
            cmd.SetComputeTextureParam(shader, CopyRadiance, "FDepthWrite", FDepth);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "TEX_PT_NORMALS_AWrite", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, CopyRadiance, "TEX_PT_VIEW_DEPTH_B", TEX_PT_VIEW_DEPTH_B);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "Depth", CorrectedDepthTex);
            cmd.SetComputeIntParam(shader, "CurFrame", CurFrame);
            shader.SetVector("Forward", camera.transform.forward);
            cmd.SetComputeIntParam(shader, "iter", iter);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "RNGTexA", (EvenFrame) ? RNGTex : RNGTexB);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "RNGTexB", (!EvenFrame) ? RNGTex : RNGTexB);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "DebugTex", DebugTex);
            cmd.SetComputeBufferParam(shader, CopyRadiance, "RayB", (!EvenFrame) ? GlobalRays : GlobalRaysB);
            cmd.SetComputeBufferParam(shader, CopyRadiance, "GlobalRays", (EvenFrame) ? GlobalRays : GlobalRaysB);

            cmd.DispatchCompute(shader, CopyRadiance, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);
            cmd.EndSample("ASVGF Copy Radiance Kernel");
            cmd.BeginSample("ASVGF Reproject Gradients Kernel");

            shader.SetTextureFromGlobal(Reproject, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_VIEW_DEPTH_A", CorrectedDepthTex);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_VIEW_DEPTH_B", TEX_PT_VIEW_DEPTH_B);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_NORMALS_A", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
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
            cmd.SetComputeTextureParam(shader, Reproject, "DebugTex", DebugTex);
            cmd.SetComputeTextureParam(shader, Reproject, "MetallicAWrite", (EvenFrame ? MetallicA : MetallicB));
            cmd.SetComputeTextureParam(shader, Reproject, "MetallicB", (!EvenFrame ? MetallicA : MetallicB));
            cmd.SetComputeTextureParam(shader, Reproject, "PrimaryTriData", PrimaryTriData);
            cmd.SetComputeBufferParam(shader, Reproject, "RayB", (!EvenFrame) ? GlobalRays : GlobalRaysB);
            cmd.SetComputeBufferParam(shader, Reproject, "_MeshData", Meshes);
            cmd.SetComputeBufferParam(shader, Reproject, "AggTris", Tris);
            shader.SetVector("CamDiff", PrevPos - camera.transform.position);
            cmd.SetComputeBufferParam(shader, Reproject, "GlobalRays", (EvenFrame) ? GlobalRays : GlobalRaysB);



            cmd.DispatchCompute(shader, Reproject, Mathf.CeilToInt((ScreenWidth) / 24.0f), Mathf.CeilToInt((ScreenHeight) / 24.0f), 1);
            cmd.EndSample("ASVGF Reproject Gradients Kernel");
            PrevPos = camera.transform.position;
        }


        public void Do(ref ComputeBuffer _ColorBuffer, ref RenderTexture Albedo, ref RenderTexture Output, bool DiffRes, RenderTexture TEX_PT_VIEW_DEPTH_B, ComputeBuffer ScreenSpaceBuffer, CommandBuffer cmd, RenderTexture CorrectedDepthTex, int CurFrame, ref RenderTexture WorldPosData, int PartialRenderingFactor)
        {

            bool EvenFrame = CurFrame % 2 == 0;
            cmd.BeginSample("ASVGF Copy Data Kernel");
            int MaxIterations = 4;
            cmd.SetComputeIntParam(shader, "MaxIterations", MaxIterations);
            shader.SetBuffer(CopyData, "PerPixelRadiance", _ColorBuffer);
            shader.SetBuffer(CopyData, "ScreenSpaceInfo", ScreenSpaceBuffer);
            shader.SetTextureFromGlobal(CopyData, "MotionVectors", "_CameraMotionVectorsTexture");
            shader.SetInt("PartialRenderingFactor", PartialRenderingFactor);
            cmd.SetComputeTextureParam(shader, CopyData, "WorldPosData", WorldPosData);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_LF_SHWrite", PT_LF1);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_LF_COCGWrite", PT_LF2);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_HFWrite", TEX_PT_COLOR_HF);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_SPECWrite", TEX_PT_COLOR_SPEC);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_NORMALS_AWrite", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_NORMALS_B", (!EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            shader.SetTextureFromGlobal(CopyData, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");

            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_BASE_COLOR_A", Albedo);
            cmd.SetComputeTextureParam(shader, CopyData, "MetallicAWrite", (EvenFrame ? MetallicA : MetallicB));
            cmd.SetComputeTextureParam(shader, CopyData, "ReflectedRefractedTexWrite", (EvenFrame ? ReflectedRefractedTex : ReflectedRefractedTexPrev));
            cmd.SetComputeTextureParam(shader, CopyData, "ReflectedRefractedTexPrev", (!EvenFrame ? ReflectedRefractedTex : ReflectedRefractedTexPrev));

            cmd.SetComputeTextureParam(shader, CopyData, "DebugTex", DebugTex);
            cmd.SetComputeTextureParam(shader, CopyData, "AlbedoColorA", (EvenFrame ? AlbedoColorA : AlbedoColorB));
            cmd.SetComputeTextureParam(shader, CopyData, "AlbedoColorB", (!EvenFrame ? AlbedoColorA : AlbedoColorB));


            cmd.DispatchCompute(shader, CopyData, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);
            cmd.EndSample("ASVGF Copy Data Kernel");
            cmd.BeginSample("ASVGF Grad Compute Kernel");
            shader.SetTextureFromGlobal(Gradient_Img, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_ASVGF_GRAD_SMPL_POS_A", (EvenFrame ? ASVGF_GRAD_SMPL_POS_A : ASVGF_GRAD_SMPL_POS_B));
            cmd.SetComputeTextureParam(shader, Gradient_Img, "IMG_ASVGF_GRAD_LF_PING", ASVGF_GRAD_LF_PING);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "IMG_ASVGF_GRAD_HF_SPEC_PING", ASVGF_GRAD_HF_SPEC_PING);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_COLOR_HFWrite", TEX_PT_COLOR_HF);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_COLOR_SPECWrite", TEX_PT_COLOR_SPEC);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_COLOR_LF_SH", PT_LF1);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_ASVGF_HIST_COLOR_LF_SH_B", (!EvenFrame ? ASVGF_HIST_COLOR_LF_SH_A : ASVGF_HIST_COLOR_LF_SH_B));
            cmd.SetComputeTextureParam(shader, Gradient_Img, "DebugTex", DebugTex);


            cmd.DispatchCompute(shader, Gradient_Img, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);
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
                cmd.SetComputeTextureParam(shader, Gradient_Atrous, "DebugTex", DebugTex);

                cmd.DispatchCompute(shader, Gradient_Atrous, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);
            }
            cmd.EndSample("ASVGF Grad Atrous Kernels");

            cmd.BeginSample("ASVGF Temporal Kernel");
            shader.SetTextureFromGlobal(Temporal, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(shader, Temporal, "MetallicA", (EvenFrame ? MetallicA : MetallicB));
            cmd.SetComputeTextureParam(shader, Temporal, "ReflectedRefractedTex", (EvenFrame ? ReflectedRefractedTex : ReflectedRefractedTexPrev));
            cmd.SetComputeTextureParam(shader, Temporal, "ReflectedRefractedTexPrev", (!EvenFrame ? ReflectedRefractedTex : ReflectedRefractedTexPrev));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_VIEW_DEPTH_A", CorrectedDepthTex);
            cmd.SetComputeTextureParam(shader, Temporal, "FDepth", FDepth);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_VIEW_DEPTH_B", TEX_PT_VIEW_DEPTH_B);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_NORMALS_A", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_NORMALS_B", (!EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_COLOR_HF", TEX_PT_COLOR_HF);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_COLOR_LF_SH_B", (!EvenFrame ? ASVGF_HIST_COLOR_LF_SH_A : ASVGF_HIST_COLOR_LF_SH_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_COLOR_LF_COCG_B", (!EvenFrame ? ASVGF_HIST_COLOR_LF_COCG_A : ASVGF_HIST_COLOR_LF_COCG_B));
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_COLOR_HF", ASVGF_HIST_COLOR_HF);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_FILTERED_SPEC_B", (!EvenFrame ? ASVGF_FILTERED_SPEC_A : ASVGF_FILTERED_SPEC_B));
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_SPEC", (EvenFrame ? ASVGF_FILTERED_SPEC_A : ASVGF_FILTERED_SPEC_B));
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
            cmd.SetComputeTextureParam(shader, Temporal, "DebugTex", DebugTex);
            
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_ATROUS_PONG_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_ATROUS_PONG_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);
            
            cmd.DispatchCompute(shader, Temporal, Mathf.CeilToInt((ScreenWidth + 14) / 15.0f), Mathf.CeilToInt((ScreenHeight + 14) / 15.0f), 1);
            cmd.EndSample("ASVGF Temporal Kernel");

            shader.SetBool("DiffRes", DiffRes);
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
            cmd.SetComputeTextureParam(shader, Atrous_LF, "DebugTex", DebugTex);
            cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_HIST_MOMENTS_HF_A", (EvenFrame ? ASVGF_HIST_MOMENTS_HF_A : ASVGF_HIST_MOMENTS_HF_B));
            cmd.DispatchCompute(shader, Atrous_LF, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);
            cmd.EndSample("ASVGF Atrous LF: " + 0);


            for (int i = 0; i < MaxIterations; i++)
            {
                var e = i;
                cmd.BeginSample("ASVGF Atrous LF " + (e + 1));
                cmd.SetComputeIntParam(shader, "iteration", e + 1);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "FDepth", FDepth);
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
                cmd.SetComputeTextureParam(shader, Atrous_LF, "DebugTex", DebugTex);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_HIST_MOMENTS_HF_A", (EvenFrame ? ASVGF_HIST_MOMENTS_HF_A : ASVGF_HIST_MOMENTS_HF_B));
                cmd.DispatchCompute(shader, Atrous_LF, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);

                cmd.EndSample("ASVGF Atrous LF " + (e + 1));
                cmd.BeginSample("ASVGF Atrous " + e);

                cmd.SetComputeIntParam(shader, "spec_iteration", e);
                cmd.SetComputeTextureParam(shader, Atrous, "FDepth", FDepth);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_PT_NORMALS_A", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_PT_VIEW_DEPTH_A", CorrectedDepthTex);
                shader.SetTextureFromGlobal(Atrous, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
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
                cmd.SetComputeTextureParam(shader, Atrous, "DebugTex", DebugTex);
                cmd.SetComputeTextureParam(shader, Atrous, "MetallicA", (EvenFrame ? MetallicA : MetallicB));
                cmd.SetComputeTextureParam(shader, Atrous, "MetallicB", (!EvenFrame ? MetallicA : MetallicB));
                shader.SetTextureFromGlobal(Atrous, "DiffuseGBuffer", "_CameraGBufferTexture0");
                shader.SetTextureFromGlobal(Atrous, "SpecularGBuffer", "_CameraGBufferTexture1");
                shader.SetTextureFromGlobal(Atrous, "NormalTex", "_CameraGBufferTexture2");

                cmd.SetComputeTextureParam(shader, Atrous, "AlbedoColorB", (EvenFrame ? AlbedoColorA : AlbedoColorB));
                shader.SetBuffer(Atrous, "ScreenSpaceInfo", ScreenSpaceBuffer);
                cmd.DispatchCompute(shader, Atrous, Mathf.CeilToInt((ScreenWidth + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight + 15) / 16.0f), 1);
                cmd.EndSample("ASVGF Atrous " + e);

            }


            iter++;

            PrevCamPos = camera.transform.position;



        }

    }
}