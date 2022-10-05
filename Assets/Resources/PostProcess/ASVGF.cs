using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ASVGF
{

    public RenderTexture ASVGF_HIST_COLOR_HF;
    public RenderTexture TEX_PT_VIEW_DEPTH_A;
    public RenderTexture TEX_PT_VIEW_DEPTH_B;
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
    public RenderTexture PT_GEO_NORMAL;
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
    public RenderTexture TEX_PT_NORMAL_B;
    public RenderTexture RNGTexB;

    public RenderTexture PT_LF1;
    public RenderTexture PT_LF2;

    public RenderTexture TEX_PT_COLOR_HF;

    public RenderTexture DebugTex;

    public ComputeBuffer RayA;
    public ComputeBuffer RayB;

    public Camera camera;

    public int ScreenWidth;
    public int ScreenHeight;
    public ComputeShader shader;

    private int CopyData;
    private int CopyRadiance;
    private int Reproject;
    private int Gradient_Img;
    private int Gradient_Atrous;
    private int Temporal;
    private int Atrous_LF;
    private int Finalize;
    private int Atrous;

    public void ClearAll() {
        RayA?.Release();
        RayB?.Release();
        ASVGF_HIST_COLOR_HF.Release();
        TEX_PT_VIEW_DEPTH_A.Release();
        TEX_PT_VIEW_DEPTH_B.Release();
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
        PT_GEO_NORMAL.Release();
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
    }

    
    private void CreateComputeBuffer<T>(ref ComputeBuffer buffer, T[] data, int stride)
        where T : struct
    {
        // Do we already have a compute buffer?
        if (buffer != null) {
            // If no data or buffer doesn't match the given criteria, release it
            if (data.Length == 0 || buffer.count != data.Length || buffer.stride != stride) {
                buffer.Release();
                buffer = null;
            }
        }

        if (data.Length != 0) {
            // If the buffer has been released or wasn't there to
            // begin with, create it
            if (buffer == null) {
                buffer = new ComputeBuffer(data.Length, stride);
            }
            // Set data on the buffer
            buffer.SetData(data);
        }
    }

    private void CreateRenderTexture(ref RenderTexture ThisTex) {
        ThisTex = new RenderTexture(ScreenWidth, ScreenHeight, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        ThisTex.enableRandomWrite = true;
        ThisTex.useMipMap = false;
        ThisTex.Create();
    }

    private void CreateRenderTextureGrad(ref RenderTexture ThisTex) {
        ThisTex = new RenderTexture(ScreenWidth / 3, ScreenHeight / 3, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        ThisTex.enableRandomWrite = true;
        ThisTex.useMipMap = false;
        ThisTex.Create();
    }

    private int iter;
    public void init(int ScreenWidth, int ScreenHeight, Camera camera) {
        this.ScreenWidth = ScreenWidth;
        this.ScreenHeight = ScreenHeight;
        this.camera = camera;
        iter = 0;
        if(shader == null) {shader = Resources.Load<ComputeShader>("PostProcess/ASVGF");} 
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

        RayA = new ComputeBuffer(ScreenWidth * ScreenHeight, 36);
        RayB = new ComputeBuffer(ScreenWidth * ScreenHeight, 36);

        CreateRenderTexture(ref ASVGF_HIST_COLOR_HF);
        CreateRenderTexture(ref TEX_PT_VIEW_DEPTH_A);
        CreateRenderTexture(ref TEX_PT_VIEW_DEPTH_B);
        CreateRenderTexture(ref TEX_PT_NORMAL_A);
        CreateRenderTexture(ref TEX_PT_NORMAL_B);


        CreateRenderTextureGrad(ref ASVGF_ATROUS_PING_LF_SH);
        CreateRenderTextureGrad(ref ASVGF_ATROUS_PONG_LF_SH);
        CreateRenderTextureGrad(ref ASVGF_ATROUS_PING_LF_COCG);
        CreateRenderTextureGrad(ref ASVGF_ATROUS_PONG_LF_COCG);
        CreateRenderTexture(ref PT_LF1);
        CreateRenderTexture(ref PT_LF2);
        CreateRenderTexture(ref ASVGF_ATROUS_PING_HF);
        CreateRenderTexture(ref ASVGF_ATROUS_PONG_HF);
        CreateRenderTexture(ref ASVGF_ATROUS_PING_SPEC);
        CreateRenderTexture(ref ASVGF_ATROUS_PONG_SPEC);
        CreateRenderTexture(ref ASVGF_ATROUS_PING_MOMENTS);
        CreateRenderTexture(ref ASVGF_ATROUS_PONG_MOMENTS);
        CreateRenderTexture(ref ASVGF_COLOR);
        CreateRenderTextureGrad(ref ASVGF_GRAD_LF_PING);
        CreateRenderTextureGrad(ref ASVGF_GRAD_LF_PONG);
        CreateRenderTextureGrad(ref ASVGF_GRAD_HF_SPEC_PING);
        CreateRenderTextureGrad(ref ASVGF_GRAD_HF_SPEC_PONG);
        CreateRenderTexture(ref PT_VIEW_DIRECTION);
        CreateRenderTexture(ref PT_GEO_NORMAL);
        CreateRenderTexture(ref ASVGF_FILTERED_SPEC_A);
        CreateRenderTexture(ref ASVGF_FILTERED_SPEC_B);
        CreateRenderTexture(ref ASVGF_HIST_MOMENTS_HF_A);
        CreateRenderTexture(ref ASVGF_HIST_MOMENTS_HF_B);
        CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_SH_A);
        CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_SH_B);
        CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_COCG_A);
        CreateRenderTexture(ref ASVGF_HIST_COLOR_LF_COCG_B);
        CreateRenderTextureGrad(ref ASVGF_GRAD_SMPL_POS_A);
        CreateRenderTextureGrad(ref ASVGF_GRAD_SMPL_POS_B);
        CreateRenderTexture(ref TEX_PT_MOTION);
        CreateRenderTexture(ref TEX_PT_COLOR_HF);
        CreateRenderTexture(ref DebugTex);
        CreateRenderTexture(ref RNGTexB);
    }

    public void DoRNG(ref RenderTexture RNGTex, int CurFrame, ref ComputeBuffer GlobalRays) {
UnityEngine.Profiling.Profiler.BeginSample("Init RNG");
        shader.SetMatrix("_CameraToWorld", camera.cameraToWorldMatrix);
        shader.SetMatrix("_CameraInverseProjection", camera.projectionMatrix.inverse);
        shader.SetTextureFromGlobal(CopyRadiance, "NormalTex", "_CameraGBufferTexture2");
        shader.SetTextureFromGlobal(CopyRadiance, "MotionVectors", "_CameraMotionVectorsTexture");
        shader.SetTextureFromGlobal(CopyRadiance, "Depth", "_CameraDepthTexture");
        shader.SetFloat("FarPlane", camera.farClipPlane);
        shader.SetTexture(CopyRadiance, "TEX_PT_MOTION", TEX_PT_MOTION);
        shader.SetTexture(CopyRadiance, "TEX_PT_VIEW_DEPTH_A", TEX_PT_VIEW_DEPTH_A);
        shader.SetTexture(CopyRadiance, "TEX_PT_NORMAL_A", TEX_PT_NORMAL_A);
        shader.SetTexture(CopyRadiance, "TEX_PT_VIEW_DEPTH_B", TEX_PT_VIEW_DEPTH_B);
        shader.SetInt("CurFrame", CurFrame);
        shader.SetInt("iter", iter);
        shader.SetTexture(CopyRadiance, "RNGTexA", RNGTex);
        shader.SetTexture(CopyRadiance, "RNGTexB", RNGTexB);
        shader.SetTexture(CopyRadiance, "DebugTex", DebugTex);
        shader.SetBuffer(CopyRadiance, "RayA", RayA);
        shader.SetBuffer(CopyRadiance, "RayB", RayB);
        shader.SetBuffer(CopyRadiance, "GlobalRays", GlobalRays);
        
        shader.Dispatch(CopyRadiance, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight /16.0f), 1);
 UnityEngine.Profiling.Profiler.EndSample();

UnityEngine.Profiling.Profiler.BeginSample("Grad Reproject");

        shader.SetTexture(Reproject, "TEX_PT_MOTION", TEX_PT_MOTION);
        shader.SetTexture(Reproject, "TEX_PT_VIEW_DEPTH_A", TEX_PT_VIEW_DEPTH_A);
        shader.SetTexture(Reproject, "TEX_PT_VIEW_DEPTH_B", TEX_PT_VIEW_DEPTH_B);        
        shader.SetTexture(Reproject, "TEX_PT_GEO_NORMAL_A", TEX_PT_NORMAL_A);
        shader.SetTexture(Reproject, "TEX_PT_NORMAL_A", TEX_PT_NORMAL_A);
        shader.SetTexture(Reproject, "TEX_PT_NORMAL_B", TEX_PT_NORMAL_B);
        shader.SetTexture(Reproject, "TEX_PT_GEO_NORMAL_B", TEX_PT_NORMAL_B);
        shader.SetTexture(Reproject, "IMG_ASVGF_GRAD_SMPL_POS_A", ASVGF_GRAD_SMPL_POS_A);
        shader.SetTexture(Reproject, "IMG_ASVGF_GRAD_HF_SPEC_PING", ASVGF_GRAD_HF_SPEC_PING);
        shader.SetTexture(Reproject, "TEX_PT_COLOR_HF", TEX_PT_COLOR_HF);
        shader.SetTexture(Reproject, "TEX_PT_COLOR_SPEC", ASVGF_ATROUS_PING_SPEC);
        shader.SetTexture(Reproject, "TEX_ASVGF_GRAD_SMPL_POS_B", ASVGF_GRAD_SMPL_POS_B);
        shader.SetTexture(Reproject, "TEX_ASVGF_GRAD_SMPL_POS_A", ASVGF_GRAD_SMPL_POS_A);
        shader.SetTexture(Reproject, "RNGTexA", RNGTex);
        shader.SetTexture(Reproject, "RNGTexB", RNGTexB);
        shader.SetTexture(Reproject, "DebugTex", DebugTex);
        shader.SetBuffer(Reproject, "RayA", RayA);
        shader.SetBuffer(Reproject, "RayB", RayB);
        shader.SetBuffer(Reproject, "GlobalRays", GlobalRays);



        shader.Dispatch(Reproject, Mathf.CeilToInt((ScreenWidth) / 24.0f), Mathf.CeilToInt((ScreenHeight) / 24.0f), 1);
 UnityEngine.Profiling.Profiler.EndSample();

    }


    public void Do(ref ComputeBuffer _ColorBuffer, ref RenderTexture NormalTex, ref RenderTexture Albedo, ref RenderTexture Output, ref RenderTexture RNGTex, ref ComputeBuffer SHBuff, int MaxIterations, bool DiffRes) {
UnityEngine.Profiling.Profiler.BeginSample("Init Colors");

shader.SetInt("MaxIterations", MaxIterations);
        shader.SetBuffer(CopyData, "PerPixelRadiance", _ColorBuffer);
        shader.SetBuffer(CopyData, "SHStruct", SHBuff);
        shader.SetTextureFromGlobal(CopyData, "MotionVectors", "_CameraMotionVectorsTexture");
        shader.SetTextureFromGlobal(CopyData, "Depth", "_CameraDepthTexture");
        shader.SetTexture(CopyData, "TEX_PT_COLOR_LF_SH", PT_LF1);
        shader.SetTexture(CopyData, "TEX_PT_COLOR_LF_COCG", PT_LF2);
        shader.SetTexture(CopyData, "TEX_PT_COLOR_HF", TEX_PT_COLOR_HF);
        shader.SetTexture(CopyData, "TEX_PT_COLOR_SPEC", ASVGF_ATROUS_PING_SPEC);
        shader.SetTexture(CopyData, "Normal", NormalTex);
        shader.SetTexture(CopyData, "TEX_PT_MOTION", TEX_PT_MOTION);

        shader.SetTexture(CopyData, "TEX_PT_NORMAL_A", TEX_PT_NORMAL_A);
        shader.SetTexture(CopyData, "DebugTex", DebugTex);


        shader.Dispatch(CopyData, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);
 UnityEngine.Profiling.Profiler.EndSample();

UnityEngine.Profiling.Profiler.BeginSample("Grad IMG");

        shader.SetTexture(Gradient_Img, "TEX_PT_MOTION", TEX_PT_MOTION);
        shader.SetTexture(Gradient_Img, "TEX_ASVGF_GRAD_SMPL_POS_A", ASVGF_GRAD_SMPL_POS_A);
        shader.SetTexture(Gradient_Img, "IMG_ASVGF_GRAD_LF_PING", ASVGF_GRAD_LF_PING);
        shader.SetTexture(Gradient_Img, "IMG_ASVGF_GRAD_HF_SPEC_PING", ASVGF_GRAD_HF_SPEC_PING);
        shader.SetTexture(Gradient_Img, "TEX_PT_COLOR_HF", TEX_PT_COLOR_HF);
        shader.SetTexture(Gradient_Img, "TEX_PT_COLOR_SPEC", ASVGF_ATROUS_PING_SPEC);
        shader.SetTexture(Gradient_Img, "TEX_PT_COLOR_LF_SH", PT_LF1);
        shader.SetTexture(Gradient_Img, "TEX_ASVGF_HIST_COLOR_LF_SH_B", ASVGF_HIST_COLOR_LF_SH_B);
        shader.SetTexture(Gradient_Img, "DebugTex", DebugTex);

        
        shader.Dispatch(Gradient_Img, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);
 UnityEngine.Profiling.Profiler.EndSample();
UnityEngine.Profiling.Profiler.BeginSample("Grad Atrous");

        for(int i = 0; i < 7; i++) {
            var e = i;
            shader.SetInt("iteration", e);
            shader.SetTexture(Gradient_Atrous, "TEX_ASVGF_GRAD_LF_PING", ASVGF_GRAD_LF_PING);
            shader.SetTexture(Gradient_Atrous, "TEX_ASVGF_GRAD_HF_SPEC_PING", ASVGF_GRAD_HF_SPEC_PING);
            shader.SetTexture(Gradient_Atrous, "TEX_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
            shader.SetTexture(Gradient_Atrous, "TEX_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);


            shader.SetTexture(Gradient_Atrous, "IMG_ASVGF_GRAD_LF_PING", ASVGF_GRAD_LF_PING);
            shader.SetTexture(Gradient_Atrous, "IMG_ASVGF_GRAD_HF_SPEC_PING", ASVGF_GRAD_HF_SPEC_PING);
            shader.SetTexture(Gradient_Atrous, "IMG_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
            shader.SetTexture(Gradient_Atrous, "IMG_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);
            shader.SetTexture(Gradient_Atrous, "DebugTex", DebugTex);

            shader.Dispatch(Gradient_Atrous, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);
        }
 UnityEngine.Profiling.Profiler.EndSample();
UnityEngine.Profiling.Profiler.BeginSample("Temporal");

        shader.SetTexture(Temporal, "TEX_PT_VIEW_DEPTH_A", TEX_PT_VIEW_DEPTH_A);
        shader.SetTexture(Temporal, "TEX_PT_VIEW_DEPTH_B", TEX_PT_VIEW_DEPTH_B);
        shader.SetTexture(Temporal, "TEX_PT_NORMAL_A", TEX_PT_NORMAL_A);
        shader.SetTexture(Temporal, "TEX_PT_NORMAL_B", TEX_PT_NORMAL_B);
        shader.SetTexture(Temporal, "TEX_PT_COLOR_HF", TEX_PT_COLOR_HF);
        shader.SetTexture(Temporal, "TEX_PT_MOTION", TEX_PT_MOTION);
        shader.SetTexture(Temporal, "TEX_ASVGF_HIST_COLOR_LF_SH_B", ASVGF_HIST_COLOR_LF_SH_B);
        shader.SetTexture(Temporal, "TEX_ASVGF_HIST_COLOR_LF_COCG_B", ASVGF_HIST_COLOR_LF_COCG_B);
        shader.SetTexture(Temporal, "TEX_ASVGF_HIST_COLOR_HF", ASVGF_HIST_COLOR_HF);
        shader.SetTexture(Temporal, "TEX_ASVGF_FILTERED_SPEC_B", ASVGF_FILTERED_SPEC_B);
        shader.SetTexture(Temporal, "TEX_ASVGF_HIST_MOMENTS_HF_B", ASVGF_HIST_MOMENTS_HF_B);
        shader.SetTexture(Temporal, "TEX_PT_COLOR_LF_SH", PT_LF1);
        shader.SetTexture(Temporal, "TEX_PT_COLOR_LF_COCG", PT_LF2);
        shader.SetTexture(Temporal, "TEX_PT_COLOR_SPEC", ASVGF_ATROUS_PING_SPEC);
        shader.SetTexture(Temporal, "TEX_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
        shader.SetTexture(Temporal, "TEX_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);
        shader.SetTexture(Temporal, "IMG_ASVGF_HIST_MOMENTS_HF_A", ASVGF_HIST_MOMENTS_HF_A);
        shader.SetTexture(Temporal, "IMG_ASVGF_HIST_COLOR_LF_SH_A", ASVGF_HIST_COLOR_LF_SH_A);
        shader.SetTexture(Temporal, "IMG_ASVGF_HIST_COLOR_LF_COCG_A", ASVGF_HIST_COLOR_LF_COCG_A);
        shader.SetTexture(Temporal, "IMG_ASVGF_ATROUS_PING_HF", ASVGF_ATROUS_PING_HF);
        shader.SetTexture(Temporal, "IMG_ASVGF_ATROUS_PING_SPEC", ASVGF_ATROUS_PING_SPEC);
        shader.SetTexture(Temporal, "IMG_ASVGF_ATROUS_PING_MOMENTS", ASVGF_ATROUS_PING_MOMENTS);
        shader.SetTexture(Temporal, "IMG_ASVGF_FILTERED_SPEC_A", ASVGF_FILTERED_SPEC_A);
        shader.SetTexture(Temporal, "IMG_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PING_LF_SH);
        shader.SetTexture(Temporal, "IMG_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PING_LF_COCG);
        shader.SetTexture(Temporal, "DebugTex", DebugTex);
        shader.Dispatch(Temporal, Mathf.CeilToInt((ScreenWidth + 14) / 15.0f), Mathf.CeilToInt((ScreenHeight + 14) / 15.0f), 1);
 UnityEngine.Profiling.Profiler.EndSample();
UnityEngine.Profiling.Profiler.BeginSample("Atrous");
shader.SetBool("DiffRes", DiffRes);

        for(int i = 0; i < MaxIterations; i++) {
            var e = i;
            shader.SetInt("iteration", e);
            shader.SetTexture(Atrous_LF, "TEX_PT_GEO_NORMAL_A", TEX_PT_NORMAL_A);
            shader.SetTexture(Atrous_LF, "TEX_PT_VIEW_DEPTH_A", TEX_PT_VIEW_DEPTH_A);
            shader.SetTexture(Atrous_LF, "TEX_PT_MOTION", TEX_PT_MOTION);

            shader.SetTexture(Atrous_LF, "TEX_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PING_LF_SH);
            shader.SetTexture(Atrous_LF, "TEX_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PING_LF_COCG);
            shader.SetTexture(Atrous_LF, "TEX_ASVGF_ATROUS_PONG_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
            shader.SetTexture(Atrous_LF, "TEX_ASVGF_ATROUS_PONG_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);

            shader.SetTexture(Atrous_LF, "IMG_ASVGF_ATROUS_PONG_LF_SH", ASVGF_ATROUS_PONG_LF_SH);
            shader.SetTexture(Atrous_LF, "IMG_ASVGF_ATROUS_PONG_LF_COCG", ASVGF_ATROUS_PONG_LF_COCG);
            shader.SetTexture(Atrous_LF, "IMG_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PING_LF_SH);
            shader.SetTexture(Atrous_LF, "IMG_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PING_LF_COCG);
            shader.SetTexture(Atrous_LF, "DebugTex", DebugTex);
            shader.Dispatch(Atrous_LF, Mathf.CeilToInt((ScreenWidth / 3.0f + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight / 3.0f + 15) / 16.0f), 1);

            shader.SetInt("spec_iteration", e);
            shader.SetTexture(Atrous, "TEX_PT_NORMAL_A", TEX_PT_NORMAL_A);
            shader.SetTexture(Atrous, "TEX_PT_VIEW_DEPTH_A", TEX_PT_VIEW_DEPTH_A);
            shader.SetTexture(Atrous, "TEX_PT_MOTION", TEX_PT_MOTION);
            shader.SetTexture(Atrous, "TEX_ASVGF_HIST_MOMENTS_HF_A", ASVGF_HIST_MOMENTS_HF_A);
            shader.SetTexture(Atrous, "TEX_PT_GEO_NORMAL_A", TEX_PT_NORMAL_A);
            shader.SetTexture(Atrous, "TEX_ASVGF_HIST_COLOR_LF_SH_A", ASVGF_HIST_COLOR_LF_SH_A);
            shader.SetTexture(Atrous, "TEX_ASVGF_HIST_COLOR_LF_COCG_A", ASVGF_HIST_COLOR_LF_COCG_A);
            shader.SetTexture(Atrous, "TEX_ASVGF_ATROUS_PING_HF", ASVGF_ATROUS_PING_HF);
            shader.SetTexture(Atrous, "TEX_ASVGF_ATROUS_PING_SPEC", ASVGF_ATROUS_PING_SPEC);
            shader.SetTexture(Atrous, "TEX_ASVGF_ATROUS_PING_MOMENTS", ASVGF_ATROUS_PING_MOMENTS);
            shader.SetTexture(Atrous, "TEX_ASVGF_HIST_COLOR_HF", ASVGF_HIST_COLOR_HF);
            shader.SetTexture(Atrous, "TEX_ASVGF_ATROUS_PONG_SPEC", ASVGF_ATROUS_PONG_SPEC);
            shader.SetTexture(Atrous, "TEX_ASVGF_ATROUS_PONG_MOMENTS", ASVGF_ATROUS_PONG_MOMENTS);
            shader.SetTexture(Atrous, "TEX_ASVGF_ATROUS_PONG_HF", ASVGF_ATROUS_PONG_HF);
            shader.SetTexture(Atrous, "IMG_ASVGF_HIST_COLOR_HF", ASVGF_HIST_COLOR_HF);
            shader.SetTexture(Atrous, "IMG_ASVGF_ATROUS_PONG_SPEC", ASVGF_ATROUS_PONG_SPEC);
            shader.SetTexture(Atrous, "IMG_ASVGF_ATROUS_PONG_MOMENTS", ASVGF_ATROUS_PONG_MOMENTS);
            shader.SetTexture(Atrous, "IMG_ASVGF_ATROUS_PING_HF", ASVGF_ATROUS_PING_HF);
            shader.SetTexture(Atrous, "IMG_ASVGF_ATROUS_PING_SPEC", ASVGF_ATROUS_PING_SPEC);
            shader.SetTexture(Atrous, "IMG_ASVGF_ATROUS_PING_MOMENTS", ASVGF_ATROUS_PING_MOMENTS);
            shader.SetTexture(Atrous, "IMG_ASVGF_ATROUS_PONG_HF", ASVGF_ATROUS_PONG_HF);
            shader.SetTexture(Atrous, "IMG_ASVGF_ATROUS_PONG_SPEC", ASVGF_ATROUS_PONG_SPEC);
            shader.SetTexture(Atrous, "IMG_ASVGF_ATROUS_PONG_MOMENTS", ASVGF_ATROUS_PONG_MOMENTS);
            shader.SetTexture(Atrous, "TEX_ASVGF_ATROUS_PING_LF_SH", ASVGF_ATROUS_PING_LF_SH);
            shader.SetTexture(Atrous, "TEX_ASVGF_ATROUS_PING_LF_COCG", ASVGF_ATROUS_PING_LF_COCG);
            shader.SetTexture(Atrous, "TEX_PT_BASE_COLOR_A", Albedo);
            shader.SetTexture(Atrous, "IMG_ASVGF_COLOR", Output);
            shader.SetTexture(Atrous, "TEX_ASVGF_GRAD_LF_PONG", ASVGF_GRAD_LF_PONG);
            shader.SetTexture(Atrous, "TEX_ASVGF_GRAD_HF_SPEC_PONG", ASVGF_GRAD_HF_SPEC_PONG);
            shader.SetTexture(Atrous, "DebugTex", DebugTex);
            shader.Dispatch(Atrous, Mathf.CeilToInt((ScreenWidth + 15) / 16.0f), Mathf.CeilToInt((ScreenHeight + 15) / 16.0f), 1);  

        }
 UnityEngine.Profiling.Profiler.EndSample();
UnityEngine.Profiling.Profiler.BeginSample("Finalize");

        shader.SetTexture(Finalize, "IMG_PT_NORMAL_A", TEX_PT_NORMAL_A);
        shader.SetTexture(Finalize, "IMG_PT_NORMAL_B", TEX_PT_NORMAL_B);

        shader.SetTexture(Finalize, "TEX_PT_VIEW_DEPTH_A", TEX_PT_VIEW_DEPTH_A);
        shader.SetTexture(Finalize, "TEX_PT_VIEW_DEPTH_B", TEX_PT_VIEW_DEPTH_B);

        shader.SetTexture(Finalize, "TEX_ASVGF_HIST_MOMENTS_HF_B", ASVGF_HIST_MOMENTS_HF_B);
        shader.SetTexture(Finalize, "TEX_ASVGF_HIST_MOMENTS_HF_A", ASVGF_HIST_MOMENTS_HF_A);

        shader.SetTexture(Finalize, "IMG_ASVGF_HIST_COLOR_LF_SH_A", ASVGF_HIST_COLOR_LF_SH_A);
        shader.SetTexture(Finalize, "IMG_ASVGF_HIST_COLOR_LF_SH_B", ASVGF_HIST_COLOR_LF_SH_B);

        shader.SetTexture(Finalize, "IMG_ASVGF_HIST_COLOR_LF_COCG_A", ASVGF_HIST_COLOR_LF_COCG_A);
        shader.SetTexture(Finalize, "IMG_ASVGF_HIST_COLOR_LF_COCG_B", ASVGF_HIST_COLOR_LF_COCG_B);

        shader.SetTexture(Finalize, "TEX_ASVGF_GRAD_SMPL_POS_A", ASVGF_GRAD_SMPL_POS_A);
        shader.SetTexture(Finalize, "TEX_ASVGF_GRAD_SMPL_POS_B", ASVGF_GRAD_SMPL_POS_B);

        shader.SetTexture(Finalize, "RNGTexB", RNGTexB);
        shader.SetTexture(Finalize, "RNGTexA", RNGTex);

        shader.SetTexture(Finalize, "DebugTex", DebugTex);


        shader.SetBuffer(Finalize, "RayA", RayA);
        shader.SetBuffer(Finalize, "RayB", RayB);


        shader.Dispatch(Finalize, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);
 UnityEngine.Profiling.Profiler.EndSample();


        iter++;



    } 

}
