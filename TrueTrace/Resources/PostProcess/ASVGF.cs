using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrueTrace {
    [System.Serializable]
    public class ASVGF
    {
        public bool Initialized = false;
        public RenderTexture ASVGF_HIST_COLOR_HF;
        public RenderTexture TEX_PT_VIEW_DEPTH_A;
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
        public RenderTexture PT_VIEW_DIRECTION;
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
        public RenderTexture IMG_ASVGF_COLOR;
        public RenderTexture RNGTexB;
        public RenderTexture MetallicA;
        public RenderTexture MetallicB;
        public RenderTexture PrevIndirectLum;

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

        public ComputeBuffer RayB;

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
                RayB?.Release();
                ASVGF_HIST_COLOR_HF.Release();
                TEX_PT_VIEW_DEPTH_A.Release();
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
                PT_VIEW_DIRECTION.Release();
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
                RNGTexB.Release();
                PT_LF1.Release();
                PT_LF2.Release();
                TEX_PT_COLOR_HF.Release();
                DebugTex.Release();
                TEX_PT_COLOR_SPEC.Release();
                IMG_ASVGF_COLOR.Release();
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
                PrevIndirectLum.Release();
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

            RayB = new ComputeBuffer(ScreenWidth * ScreenHeight, 36);

            CreateRenderTextureMask(ref ASVGF_HIST_COLOR_HF);
            CreateRenderTextureSingle(ref TEX_PT_VIEW_DEPTH_A);
            CreateRenderTextureInt2(ref TEX_PT_NORMALS_A);
            CreateRenderTextureInt2(ref TEX_PT_NORMALS_B);

            CreateRenderTextureGradSingle(ref PrevIndirectLum);

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
            CreateRenderTextureDouble(ref ASVGF_ATROUS_PING_MOMENTS);
            CreateRenderTextureDouble(ref ASVGF_ATROUS_PONG_MOMENTS);
            CreateRenderTexture(ref ASVGF_COLOR);
            CreateRenderTextureGradDouble(ref ASVGF_GRAD_LF_PING);
            CreateRenderTextureGradDouble(ref ASVGF_GRAD_LF_PONG);
            CreateRenderTextureGradDouble(ref ASVGF_GRAD_HF_SPEC_PING);
            CreateRenderTextureGradDouble(ref ASVGF_GRAD_HF_SPEC_PONG);
            CreateRenderTexture(ref PT_VIEW_DIRECTION);
            CreateRenderTextureDouble(ref ASVGF_FILTERED_SPEC_A);
            CreateRenderTextureDouble(ref ASVGF_FILTERED_SPEC_B);
            CreateRenderTexture(ref ASVGF_HIST_MOMENTS_HF_A);
            CreateRenderTexture(ref ASVGF_HIST_MOMENTS_HF_B);
            CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_SH_A);
            CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_SH_B);
            CreateRenderTextureDouble(ref ASVGF_HIST_COLOR_LF_COCG_A);
            CreateRenderTextureDouble(ref ASVGF_HIST_COLOR_LF_COCG_B);
            CreateRenderTextureGradSingle(ref ASVGF_GRAD_SMPL_POS_A);
            CreateRenderTextureGradSingle(ref ASVGF_GRAD_SMPL_POS_B);
            CreateRenderTextureGradSingle(ref LFVarianceA);
            CreateRenderTextureGradSingle(ref LFVarianceB);
            CreateRenderTextureSingle(ref FDepth);
            CreateRenderTextureMask(ref TEX_PT_COLOR_HF);
            CreateRenderTexture(ref DebugTex);
            CreateRenderTexture(ref RNGTexB);
            CreateRenderTextureMask(ref TEX_PT_COLOR_SPEC);
            CreateRenderTexture(ref IMG_ASVGF_COLOR);
            CreateRenderTextureDouble(ref MetallicA);
            CreateRenderTextureDouble(ref MetallicB);
            CreateRenderTextureMask(ref ReflectedRefractedTex);
            CreateRenderTextureMask(ref ReflectedRefractedTexPrev);
            Initialized = true;
        }

        Vector3 prevEuler;
        Vector3 PrevPos;
        public void DoRNG(ref RenderTexture RNGTex, int CurFrame, ref ComputeBuffer GlobalRays, ref RenderTexture TEX_PT_VIEW_DEPTH_B, CommandBuffer cmd, ref RenderTexture CorrectedDepthTex)
        {
            bool EvenFrame = CurFrame % 2 == 0;
            Vector3 Euler = Camera.main.transform.eulerAngles;
            shader.SetMatrix("viewprojection", Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix);
            Camera.main.transform.eulerAngles = prevEuler; 
            shader.SetMatrix("prevviewprojection", Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix);
            Camera.main.transform.eulerAngles = Euler; 
            prevEuler = Euler;
            shader.SetFloat("CameraDist", Vector3.Distance(camera.transform.position, PrevCamPos));
            UnityEngine.Profiling.Profiler.BeginSample("Init RNG");
            shader.SetMatrix("_CameraToWorld", camera.cameraToWorldMatrix);
            shader.SetMatrix("_CameraInverseProjection", camera.projectionMatrix.inverse);
            shader.SetTextureFromGlobal(CopyRadiance, "NormalTex", "_CameraGBufferTexture2");
            shader.SetTextureFromGlobal(CopyRadiance, "MotionVectors", "_CameraMotionVectorsTexture");
            shader.SetFloat("FarPlane", camera.farClipPlane);
            cmd.BeginSample("ASVGF CopyRadiance");
            cmd.SetComputeTextureParam(shader, CopyRadiance, "FDepthWrite", FDepth);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "TEX_PT_NORMALS_AWrite", (EvenFrame ? TEX_PT_NORMALS_A : TEX_PT_NORMALS_B));
            cmd.SetComputeTextureParam(shader, CopyRadiance, "TEX_PT_VIEW_DEPTH_B", TEX_PT_VIEW_DEPTH_B);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "Depth", CorrectedDepthTex);
            cmd.SetComputeIntParam(shader, "CurFrame", CurFrame);
            shader.SetVector("Forward", camera.transform.forward);
            cmd.SetComputeIntParam(shader, "iter", iter);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "RNGTexA", RNGTex);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "RNGTexB", RNGTexB);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "DebugTex", DebugTex);
            cmd.SetComputeBufferParam(shader, CopyRadiance, "RayB", RayB);
            cmd.SetComputeBufferParam(shader, CopyRadiance, "GlobalRays", GlobalRays);

            cmd.DispatchCompute(shader, CopyRadiance, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);
            UnityEngine.Profiling.Profiler.EndSample();
            cmd.EndSample("ASVGF CopyRadiance");
            cmd.BeginSample("ASVGF Reproject");
            UnityEngine.Profiling.Profiler.BeginSample("Grad Reproject");

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
            cmd.SetComputeTextureParam(shader, Reproject, "RNGTexA", RNGTex);
            cmd.SetComputeTextureParam(shader, Reproject, "RNGTexB", RNGTexB);
            cmd.SetComputeTextureParam(shader, Reproject, "PrevIndirectLum", PrevIndirectLum);
            cmd.SetComputeTextureParam(shader, Reproject, "DebugTex", DebugTex);
            cmd.SetComputeTextureParam(shader, Reproject, "MetallicAWrite", (EvenFrame ? MetallicA : MetallicB));
            cmd.SetComputeTextureParam(shader, Reproject, "MetallicB", (!EvenFrame ? MetallicA : MetallicB));
            cmd.SetComputeBufferParam(shader, Reproject, "RayB", RayB);
            shader.SetVector("CamDiff", PrevPos - camera.transform.position);
            cmd.SetComputeBufferParam(shader, Reproject, "GlobalRays", GlobalRays);



            cmd.DispatchCompute(shader, Reproject, Mathf.CeilToInt((ScreenWidth) / 24.0f), Mathf.CeilToInt((ScreenHeight) / 24.0f), 1);
            UnityEngine.Profiling.Profiler.EndSample();
            cmd.EndSample("ASVGF Reproject");
            PrevPos = camera.transform.position;
        }


        public void Do(ref ComputeBuffer _ColorBuffer, ref RenderTexture NormalTex, ref RenderTexture Albedo, ref RenderTexture Output, ref RenderTexture RNGTex, bool DiffRes, ref RenderTexture TEX_PT_VIEW_DEPTH_B, ComputeBuffer ScreenSpaceBuffer, CommandBuffer cmd, ref RenderTexture CorrectedDepthTex, ref ComputeBuffer GlobalRays, int CurFrame)
        {
            cmd.BeginSample("ASVGF");

            bool EvenFrame = CurFrame % 2 == 0;
            UnityEngine.Profiling.Profiler.BeginSample("Init Colors");
            cmd.BeginSample("ASVGF CopyData");
            int MaxIterations = 4;
            cmd.SetComputeIntParam(shader, "MaxIterations", MaxIterations);
            shader.SetBuffer(CopyData, "PerPixelRadiance", _ColorBuffer);
            shader.SetBuffer(CopyData, "ScreenSpaceInfo", ScreenSpaceBuffer);
            shader.SetTextureFromGlobal(CopyData, "MotionVectors", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_LF_SHWrite", PT_LF1);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_LF_COCGWrite", PT_LF2);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_HFWrite", TEX_PT_COLOR_HF);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_SPECWrite", TEX_PT_COLOR_SPEC);
            cmd.SetComputeTextureParam(shader, CopyData, "Normal", NormalTex);
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
            UnityEngine.Profiling.Profiler.EndSample();
            cmd.EndSample("ASVGF CopyData");
            UnityEngine.Profiling.Profiler.BeginSample("Grad IMG");
            cmd.BeginSample("ASVGF GradIMG");
            shader.SetTextureFromGlobal(Gradient_Img, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_ASVGF_GRAD_SMPL_POS_A", (EvenFrame ? ASVGF_GRAD_SMPL_POS_A : ASVGF_GRAD_SMPL_POS_B));
            cmd.SetComputeTextureParam(shader, Gradient_Img, "IMG_ASVGF_GRAD_LF_PING", ASVGF_GRAD_LF_PING);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "IMG_ASVGF_GRAD_HF_SPEC_PING", ASVGF_GRAD_HF_SPEC_PING);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_COLOR_HFWrite", TEX_PT_COLOR_HF);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_COLOR_SPECWrite", TEX_PT_COLOR_SPEC);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_COLOR_LF_SH", PT_LF1);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_ASVGF_HIST_COLOR_LF_SH_B", (!EvenFrame ? ASVGF_HIST_COLOR_LF_SH_A : ASVGF_HIST_COLOR_LF_SH_B));
            cmd.SetComputeTextureParam(shader, Gradient_Img, "DebugTex", DebugTex);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "PrevIndirectLum", PrevIndirectLum);


            cmd.DispatchCompute(shader, Gradient_Img, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);
            cmd.EndSample("ASVGF GradIMG");
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("Grad Atrous");

            cmd.BeginSample("ASVGF Grad Atrous");
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
            cmd.EndSample("ASVGF Grad Atrous");
            cmd.BeginSample("ASVGF Temporal");

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("Temporal");

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
            cmd.DispatchCompute(shader, Temporal, Mathf.CeilToInt((ScreenWidth + 14) / 15.0f), Mathf.CeilToInt((ScreenHeight + 14) / 15.0f), 1);
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("Atrous");
            shader.SetBool("DiffRes", DiffRes);
            cmd.CopyTexture((EvenFrame ? ASVGF_FILTERED_SPEC_A : ASVGF_FILTERED_SPEC_B), ASVGF_ATROUS_PING_SPEC);

            cmd.BeginSample("ASVGF Atrous LF " + 0);
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
            cmd.EndSample("ASVGF Atrous LF " + 0);


            cmd.EndSample("ASVGF Temporal");
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
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_COLOR", IMG_ASVGF_COLOR);
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
            cmd.BeginSample("ASVGF Finalize");
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("Finalize2");

            cmd.SetComputeTextureParam(shader, Finalize, "TEX_PT_VIEW_DEPTH_A", CorrectedDepthTex);
            cmd.SetComputeTextureParam(shader, Finalize, "TEX_PT_VIEW_DEPTH_BWrite", TEX_PT_VIEW_DEPTH_B);

            cmd.SetComputeTextureParam(shader, Finalize, "RNGTexBWrite", RNGTexB);
            cmd.SetComputeTextureParam(shader, Finalize, "RNGTexA", RNGTex);

            shader.SetBuffer(Finalize, "GlobalRays", GlobalRays);
            shader.SetBuffer(Finalize, "RayB", RayB);

            cmd.SetComputeTextureParam(shader, Finalize, "IMG_ASVGF_COLOR", IMG_ASVGF_COLOR);
            cmd.SetComputeTextureParam(shader, Finalize, "Output", Output);

            cmd.DispatchCompute(shader, Finalize, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);

            cmd.EndSample("ASVGF Finalize");
            cmd.EndSample("ASVGF");

            // cmd.CopyTexture(DebugTex, Output);
            UnityEngine.Profiling.Profiler.EndSample();


            iter++;

            PrevCamPos = camera.transform.position;



        }

    }
}