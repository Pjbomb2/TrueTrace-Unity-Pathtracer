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
        public RenderTexture TEX_PT_MOTION;
        public RenderTexture PT_GEO_NORMAL_A;
        public RenderTexture PT_GEO_NORMAL_B;
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
        public RenderTexture TEX_PT_NORMAL_A;
        public RenderTexture IMG_ASVGF_COLOR;
        public RenderTexture RNGTexB;
        public RenderTexture MetallicA;
        public RenderTexture MetallicB;

        public RenderTexture PT_LF1;
        public RenderTexture PT_LF2;

        public RenderTexture TEX_PT_COLOR_HF;
        public RenderTexture TEX_PT_COLOR_SPEC;

        public RenderTexture DebugTex;

        public ComputeBuffer RayA;
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
        private int Finalize2;
        private int Atrous;
        private Vector3 PrevCamPos;

        public void ClearAll()
        {
            if (ASVGF_HIST_COLOR_HF != null)
            {
                RayA?.Release();
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
                TEX_PT_MOTION.Release();
                PT_GEO_NORMAL_A.Release();
                PT_GEO_NORMAL_B.Release();
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
            Finalize2 = shader.FindKernel("Finalize2");
            Atrous = shader.FindKernel("Atrous");
            shader.SetInt("screen_width", ScreenWidth);
            shader.SetInt("screen_height", ScreenHeight);

            RayA = new ComputeBuffer(ScreenWidth * ScreenHeight, 36);
            RayB = new ComputeBuffer(ScreenWidth * ScreenHeight, 36);

            CreateRenderTexture(ref ASVGF_HIST_COLOR_HF);
            CreateRenderTextureSingle(ref TEX_PT_VIEW_DEPTH_A);
            CreateRenderTextureInt(ref TEX_PT_NORMAL_A);


            CreateRenderTextureGrad(ref ASVGF_ATROUS_PING_LF_SH);
            CreateRenderTextureGrad(ref ASVGF_ATROUS_PONG_LF_SH);
            CreateRenderTextureGradDouble(ref ASVGF_ATROUS_PING_LF_COCG);
            CreateRenderTextureGradDouble(ref ASVGF_ATROUS_PONG_LF_COCG);
            CreateRenderTexture(ref PT_LF1);
            CreateRenderTextureDouble(ref PT_LF2);
            CreateRenderTexture(ref ASVGF_ATROUS_PING_HF);
            CreateRenderTexture(ref ASVGF_ATROUS_PONG_HF);
            CreateRenderTexture(ref ASVGF_ATROUS_PING_SPEC);
            CreateRenderTexture(ref ASVGF_ATROUS_PONG_SPEC);
            CreateRenderTextureDouble(ref ASVGF_ATROUS_PING_MOMENTS);
            CreateRenderTextureDouble(ref ASVGF_ATROUS_PONG_MOMENTS);
            CreateRenderTexture(ref ASVGF_COLOR);
            CreateRenderTextureGradDouble(ref ASVGF_GRAD_LF_PING);
            CreateRenderTextureGradDouble(ref ASVGF_GRAD_LF_PONG);
            CreateRenderTextureGradDouble(ref ASVGF_GRAD_HF_SPEC_PING);
            CreateRenderTextureGradDouble(ref ASVGF_GRAD_HF_SPEC_PONG);
            CreateRenderTexture(ref PT_VIEW_DIRECTION);
            CreateRenderTextureInt(ref PT_GEO_NORMAL_A);
            CreateRenderTextureInt(ref PT_GEO_NORMAL_B);
            CreateRenderTexture(ref ASVGF_FILTERED_SPEC_A);
            CreateRenderTexture(ref ASVGF_FILTERED_SPEC_B);
            CreateRenderTexture(ref ASVGF_HIST_MOMENTS_HF_A);
            CreateRenderTexture(ref ASVGF_HIST_MOMENTS_HF_B);
            CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_SH_A);
            CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_SH_B);
            CreateRenderTextureDouble(ref ASVGF_HIST_COLOR_LF_COCG_A);
            CreateRenderTextureDouble(ref ASVGF_HIST_COLOR_LF_COCG_B);
            CreateRenderTextureGradSingle(ref ASVGF_GRAD_SMPL_POS_A);
            CreateRenderTextureGradSingle(ref ASVGF_GRAD_SMPL_POS_B);
            CreateRenderTexture(ref TEX_PT_MOTION);
            CreateRenderTexture(ref TEX_PT_COLOR_HF);
            CreateRenderTexture(ref DebugTex);
            CreateRenderTexture(ref RNGTexB);
            CreateRenderTexture(ref TEX_PT_COLOR_SPEC);
            CreateRenderTexture(ref IMG_ASVGF_COLOR);
            CreateRenderTextureDouble(ref MetallicA);
            CreateRenderTextureDouble(ref MetallicB);
            Initialized = true;
        }

        public void DoRNG(ref RenderTexture RNGTex, int CurFrame, ref ComputeBuffer GlobalRays, ref RenderTexture TEX_PT_VIEW_DEPTH_B, ref RenderTexture TEX_PT_NORMAL_B, CommandBuffer cmd)
        {
            shader.SetFloat("CameraDist", Vector3.Distance(camera.transform.position, PrevCamPos));
            UnityEngine.Profiling.Profiler.BeginSample("Init RNG");
            shader.SetMatrix("_CameraToWorld", camera.cameraToWorldMatrix);
            shader.SetMatrix("_CameraInverseProjection", camera.projectionMatrix.inverse);
            shader.SetTextureFromGlobal(CopyRadiance, "NormalTex", "_CameraGBufferTexture2");
            shader.SetTextureFromGlobal(CopyRadiance, "MotionVectors", "_CameraMotionVectorsTexture");
            shader.SetTextureFromGlobal(CopyRadiance, "Depth", "_CameraDepthTexture");
            shader.SetFloat("FarPlane", camera.farClipPlane);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "TEX_PT_MOTIONWrite", TEX_PT_MOTION);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "TEX_PT_VIEW_DEPTH_AWrite", TEX_PT_VIEW_DEPTH_A);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "TEX_PT_NORMAL_AWrite", TEX_PT_NORMAL_A);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "TEX_PT_VIEW_DEPTH_B", TEX_PT_VIEW_DEPTH_B);
            cmd.SetComputeIntParam(shader, "CurFrame", CurFrame);
            shader.SetVector("Forward", camera.transform.forward);
            cmd.SetComputeIntParam(shader, "iter", iter);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "RNGTexA", RNGTex);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "RNGTexB", RNGTexB);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "DebugTex", DebugTex);
            shader.SetBuffer(CopyRadiance, "RayA", RayA);
            shader.SetBuffer(CopyRadiance, "RayB", RayB);
            shader.SetBuffer(CopyRadiance, "GlobalRays", GlobalRays);
            cmd.SetComputeTextureParam(shader, CopyRadiance, "TEX_PT_GEO_NORMAL_AWrite", PT_GEO_NORMAL_A);

            cmd.DispatchCompute(shader, CopyRadiance, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("Grad Reproject");

            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_MOTION", TEX_PT_MOTION);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_VIEW_DEPTH_A", TEX_PT_VIEW_DEPTH_A);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_VIEW_DEPTH_B", TEX_PT_VIEW_DEPTH_B);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_GEO_NORMAL_A", PT_GEO_NORMAL_A);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_NORMAL_A", TEX_PT_NORMAL_A);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_NORMAL_B", TEX_PT_NORMAL_B);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_GEO_NORMAL_B", PT_GEO_NORMAL_B);
            cmd.SetComputeTextureParam(shader, Reproject, "IMG_ASVGF_GRAD_SMPL_POS_A", ASVGF_GRAD_SMPL_POS_A);
            cmd.SetComputeTextureParam(shader, Reproject, "IMG_ASVGF_GRAD_HF_SPEC_PING", ASVGF_GRAD_HF_SPEC_PING);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_COLOR_HF", TEX_PT_COLOR_HF);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_PT_COLOR_SPEC", TEX_PT_COLOR_SPEC);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_ASVGF_GRAD_SMPL_POS_B", ASVGF_GRAD_SMPL_POS_B);
            cmd.SetComputeTextureParam(shader, Reproject, "TEX_ASVGF_GRAD_SMPL_POS_A", ASVGF_GRAD_SMPL_POS_A);
            cmd.SetComputeTextureParam(shader, Reproject, "RNGTexA", RNGTex);
            cmd.SetComputeTextureParam(shader, Reproject, "RNGTexB", RNGTexB);
            cmd.SetComputeTextureParam(shader, Reproject, "DebugTex", DebugTex);
            cmd.SetComputeTextureParam(shader, Reproject, "MetallicAWrite", MetallicA);
            cmd.SetComputeTextureParam(shader, Reproject, "MetallicB", MetallicB);
            shader.SetBuffer(Reproject, "RayA", RayA);
            shader.SetBuffer(Reproject, "RayB", RayB);
            shader.SetBuffer(Reproject, "GlobalRays", GlobalRays);




            cmd.DispatchCompute(shader, Reproject, Mathf.CeilToInt((ScreenWidth) / 24.0f), Mathf.CeilToInt((ScreenHeight) / 24.0f), 1);
            UnityEngine.Profiling.Profiler.EndSample();

        }


        public void Do(ref ComputeBuffer _ColorBuffer, ref RenderTexture NormalTex, ref RenderTexture Albedo, ref RenderTexture Output, ref RenderTexture RNGTex, ref ComputeBuffer SHBuff, int MaxIterations, bool DiffRes, ref RenderTexture TEX_PT_VIEW_DEPTH_B, ref RenderTexture TEX_PT_NORMAL_B, ComputeBuffer ScreenSpaceBuffer, CommandBuffer cmd)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Init Colors");

            cmd.SetComputeIntParam(shader, "MaxIterations", MaxIterations);
            shader.SetBuffer(CopyData, "PerPixelRadiance", _ColorBuffer);
            shader.SetBuffer(CopyData, "SHStruct", SHBuff);
            shader.SetBuffer(CopyData, "ScreenSpaceInfo", ScreenSpaceBuffer);
            shader.SetTextureFromGlobal(CopyData, "MotionVectors", "_CameraMotionVectorsTexture");
            shader.SetTextureFromGlobal(CopyData, "Depth", "_CameraDepthTexture");
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_LF_SHWrite", PT_LF1);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_LF_COCGWrite", PT_LF2);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_HFWrite", TEX_PT_COLOR_HF);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_COLOR_SPECWrite", TEX_PT_COLOR_SPEC);
            cmd.SetComputeTextureParam(shader, CopyData, "Normal", NormalTex);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_MOTION", TEX_PT_MOTION);
            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_BASE_COLOR_A", Albedo);
            cmd.SetComputeTextureParam(shader, CopyData, "MetallicAWrite", MetallicA);

            cmd.SetComputeTextureParam(shader, CopyData, "TEX_PT_GEO_NORMAL_AWrite", PT_GEO_NORMAL_A);
            cmd.SetComputeTextureParam(shader, CopyData, "DebugTex", DebugTex);


            cmd.DispatchCompute(shader, CopyData, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("Grad IMG");

            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_MOTION", TEX_PT_MOTION);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_ASVGF_GRAD_SMPL_POS_A", ASVGF_GRAD_SMPL_POS_A);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "IMG_ASVGF_GRAD_LF_PING", ASVGF_GRAD_LF_PING);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "IMG_ASVGF_GRAD_HF_SPEC_PING", ASVGF_GRAD_HF_SPEC_PING);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_COLOR_HF", TEX_PT_COLOR_HF);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_COLOR_SPEC", TEX_PT_COLOR_SPEC);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_PT_COLOR_LF_SH", PT_LF1);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "TEX_ASVGF_HIST_COLOR_LF_SH_B", ASVGF_HIST_COLOR_LF_SH_B);
            cmd.SetComputeTextureParam(shader, Gradient_Img, "DebugTex", DebugTex);


            cmd.DispatchCompute(shader, Gradient_Img, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("Grad Atrous");

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
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("Temporal");

            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_VIEW_DEPTH_A", TEX_PT_VIEW_DEPTH_A);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_VIEW_DEPTH_B", TEX_PT_VIEW_DEPTH_B);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_NORMAL_A", TEX_PT_NORMAL_A);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_NORMAL_B", TEX_PT_NORMAL_B);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_GEO_NORMAL_A", PT_GEO_NORMAL_A);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_GEO_NORMAL_B", PT_GEO_NORMAL_B);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_COLOR_HF", TEX_PT_COLOR_HF);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_MOTION", TEX_PT_MOTION);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_COLOR_LF_SH_B", ASVGF_HIST_COLOR_LF_SH_B);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_COLOR_LF_COCG_B", ASVGF_HIST_COLOR_LF_COCG_B);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_COLOR_HF", ASVGF_HIST_COLOR_HF);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_FILTERED_SPEC_B", ASVGF_FILTERED_SPEC_B);
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_SPEC", ASVGF_FILTERED_SPEC_A);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_HIST_MOMENTS_HF_B", ASVGF_HIST_MOMENTS_HF_B);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_COLOR_LF_SH", PT_LF1);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_COLOR_LF_COCG", PT_LF2);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_PT_COLOR_SPEC", TEX_PT_COLOR_SPEC);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
            cmd.SetComputeTextureParam(shader, Temporal, "TEX_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_HIST_MOMENTS_HF_A", ASVGF_HIST_MOMENTS_HF_A);
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_HIST_COLOR_LF_SH_A", ASVGF_HIST_COLOR_LF_SH_A);
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_HIST_COLOR_LF_COCG_A", ASVGF_HIST_COLOR_LF_COCG_A);
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_HF", ASVGF_ATROUS_PING_HF);
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_MOMENTS", ASVGF_ATROUS_PING_MOMENTS);
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PING_LF_SH);
            cmd.SetComputeTextureParam(shader, Temporal, "IMG_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PING_LF_COCG);
            cmd.SetComputeTextureParam(shader, Temporal, "DebugTex", DebugTex);
            cmd.DispatchCompute(shader, Temporal, Mathf.CeilToInt((ScreenWidth + 14) / 15.0f), Mathf.CeilToInt((ScreenHeight + 14) / 15.0f), 1);
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("Atrous");
            shader.SetBool("DiffRes", DiffRes);
            cmd.CopyTexture(ASVGF_FILTERED_SPEC_A, ASVGF_ATROUS_PING_SPEC);

            for (int i = 0; i < MaxIterations; i++)
            {
                var e = i;
                cmd.SetComputeIntParam(shader, "iteration", e);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_PT_GEO_NORMAL_A", PT_GEO_NORMAL_A);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_PT_VIEW_DEPTH_A", TEX_PT_VIEW_DEPTH_A);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_PT_MOTION", TEX_PT_MOTION);

                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PING_LF_SH);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PING_LF_COCG);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PONG_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "TEX_ASVGF_ATROUS_PONG_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);

                cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PONG_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PONG_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PING_LF_SH);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "IMG_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PING_LF_COCG);
                cmd.SetComputeTextureParam(shader, Atrous_LF, "DebugTex", DebugTex);
                cmd.DispatchCompute(shader, Atrous_LF, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);

                cmd.SetComputeIntParam(shader, "spec_iteration", e);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_PT_NORMAL_A", PT_GEO_NORMAL_A);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_PT_VIEW_DEPTH_A", TEX_PT_VIEW_DEPTH_A);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_PT_MOTION", TEX_PT_MOTION);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_HIST_MOMENTS_HF_A", ASVGF_HIST_MOMENTS_HF_A);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_PT_GEO_NORMAL_A", PT_GEO_NORMAL_A);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_HIST_COLOR_LF_SH_A", ASVGF_HIST_COLOR_LF_SH_A);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_HIST_COLOR_LF_COCG_A", ASVGF_HIST_COLOR_LF_COCG_A);
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
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PONG_SPEC", ASVGF_ATROUS_PONG_SPEC);
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_ATROUS_PONG_MOMENTS", ASVGF_ATROUS_PONG_MOMENTS);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PING_LF_SH);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PING_LF_COCG);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_PT_BASE_COLOR_A", Albedo);
                cmd.SetComputeTextureParam(shader, Atrous, "IMG_ASVGF_COLOR", IMG_ASVGF_COLOR);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
                cmd.SetComputeTextureParam(shader, Atrous, "TEX_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);
                cmd.SetComputeTextureParam(shader, Atrous, "DebugTex", DebugTex);
                cmd.SetComputeTextureParam(shader, Atrous, "MetallicA", MetallicA);
                cmd.SetComputeTextureParam(shader, Atrous, "MetallicB", MetallicB);
                cmd.DispatchCompute(shader, Atrous, Mathf.CeilToInt((ScreenWidth + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight + 15) / 16.0f), 1);

            }
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("Finalize");

            cmd.SetComputeTextureParam(shader, Finalize, "IMG_PT_NORMAL_A", TEX_PT_NORMAL_A);
            cmd.SetComputeTextureParam(shader, Finalize, "IMG_PT_NORMAL_B", TEX_PT_NORMAL_B);

            cmd.SetComputeTextureParam(shader, Finalize, "TEX_PT_VIEW_DEPTH_A", TEX_PT_VIEW_DEPTH_A);
            cmd.SetComputeTextureParam(shader, Finalize, "TEX_PT_VIEW_DEPTH_BWrite", TEX_PT_VIEW_DEPTH_B);

            cmd.SetComputeTextureParam(shader, Finalize, "TEX_ASVGF_HIST_MOMENTS_HF_BWrite", ASVGF_HIST_MOMENTS_HF_B);
            cmd.SetComputeTextureParam(shader, Finalize, "TEX_ASVGF_HIST_MOMENTS_HF_A", ASVGF_HIST_MOMENTS_HF_A);

            cmd.SetComputeTextureParam(shader, Finalize, "TEX_ASVGF_HIST_COLOR_LF_SH_A", ASVGF_HIST_COLOR_LF_SH_A);
            cmd.SetComputeTextureParam(shader, Finalize, "IMG_ASVGF_HIST_COLOR_LF_SH_B", ASVGF_HIST_COLOR_LF_SH_B);

            cmd.SetComputeTextureParam(shader, Finalize, "TEX_ASVGF_HIST_COLOR_LF_COCG_A", ASVGF_HIST_COLOR_LF_COCG_A);
            cmd.SetComputeTextureParam(shader, Finalize, "IMG_ASVGF_HIST_COLOR_LF_COCG_B", ASVGF_HIST_COLOR_LF_COCG_B);

            cmd.SetComputeTextureParam(shader, Finalize, "TEX_ASVGF_GRAD_SMPL_POS_A", ASVGF_GRAD_SMPL_POS_A);
            cmd.SetComputeTextureParam(shader, Finalize, "TEX_ASVGF_GRAD_SMPL_POS_BWrite", ASVGF_GRAD_SMPL_POS_B);

            cmd.SetComputeTextureParam(shader, Finalize, "DebugTex", DebugTex);

            cmd.SetComputeTextureParam(shader, Finalize, "MetallicA", MetallicA);
            cmd.SetComputeTextureParam(shader, Finalize, "MetallicB", MetallicB);

            cmd.SetComputeTextureParam(shader, Finalize, "TEX_PT_GEO_NORMAL_BWrite", PT_GEO_NORMAL_B);
            cmd.SetComputeTextureParam(shader, Finalize, "TEX_PT_GEO_NORMAL_A", PT_GEO_NORMAL_A);


            cmd.DispatchCompute(shader, Finalize, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("Finalize2");

            cmd.SetComputeTextureParam(shader, Finalize2, "RNGTexBWrite", RNGTexB);
            cmd.SetComputeTextureParam(shader, Finalize2, "RNGTexA", RNGTex);

            shader.SetBuffer(Finalize2, "RayA", RayA);
            shader.SetBuffer(Finalize2, "RayB", RayB);

            cmd.SetComputeTextureParam(shader, Finalize2, "IMG_ASVGF_COLOR", IMG_ASVGF_COLOR);
            cmd.SetComputeTextureParam(shader, Finalize2, "Output", Output);

            cmd.SetComputeTextureParam(shader, Finalize2, "TEX_ASVGF_FILTERED_SPEC_BWrite", ASVGF_FILTERED_SPEC_B);
            cmd.SetComputeTextureParam(shader, Finalize2, "IMG_ASVGF_FILTERED_SPEC_A", ASVGF_FILTERED_SPEC_A);

            cmd.DispatchCompute(shader, Finalize2, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);


            // cmd.CopyTexture(DebugTex, Output);
            UnityEngine.Profiling.Profiler.EndSample();


            iter++;

            PrevCamPos = camera.transform.position;



        }

    }
}