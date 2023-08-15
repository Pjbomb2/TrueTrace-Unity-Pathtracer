using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TrueTrace {
    [System.Serializable]
    public class ReCurDenoiser
    {

        ComputeShader shader;
        public RenderTexture HFA;
        public RenderTexture HFB;
        public RenderTexture HFPrev;
        public RenderTexture SSAOTexA;
        public RenderTexture SSAOTexB;

        public RenderTexture HFLAA;
        public RenderTexture HFLAB;//High Frequency Long Accumulation

        public RenderTexture DebugTex;
        public RenderTexture NormA;
        public RenderTexture NormB;

        public RenderTexture BlurHints;

        private int CopyColorKernel;
        private int MainBlurKernel;
        private int TemporalFastKernel;
        private int TemporalSlowKernel;
        private int SSAOKernel;


        Camera camera;
        int ScreenHeight;
        int ScreenWidth;

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
                RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.useMipMap = false;
            ThisTex.Create();
        }
        private void CreateRenderTexture4(ref RenderTexture ThisTex)
        {
            ThisTex = new RenderTexture(ScreenWidth, ScreenHeight, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.useMipMap = false;
            ThisTex.Create();
        }
        private void CreateRenderTextureDouble(ref RenderTexture ThisTex)
        {
            ThisTex = new RenderTexture(ScreenWidth, ScreenHeight, 0,
                RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.useMipMap = false;
            ThisTex.Create();
        }

        private void CreateRenderTextureSingle(ref RenderTexture ThisTex)
        {
            ThisTex = new RenderTexture(ScreenWidth, ScreenHeight, 0,
                RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.useMipMap = false;
            ThisTex.Create();
        }

        private void CreateRenderTexture2(ref RenderTexture ThisTex)
        {
            ThisTex = new RenderTexture(ScreenWidth, ScreenHeight, 0,
                RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            ThisTex.enableRandomWrite = true;
            ThisTex.useMipMap = true;
            ThisTex.autoGenerateMips = false;
            ThisTex.Create();
        }

        public void init(int ScreenWidth, int ScreenHeight, Camera camera)
        {
            this.ScreenWidth = ScreenWidth;
            this.ScreenHeight = ScreenHeight;
            this.camera = camera;

            if (shader == null) { shader = Resources.Load<ComputeShader>("PostProcess/ReCurDenoiser/ReCur"); }

            CopyColorKernel = shader.FindKernel("ColorKernel");
            MainBlurKernel = shader.FindKernel("BlurKernel");
            TemporalFastKernel = shader.FindKernel("temporal");
            TemporalSlowKernel = shader.FindKernel("secondarytemporal");
            SSAOKernel = shader.FindKernel("SSAO");


            CreateRenderTextureSingle(ref SSAOTexA);
            CreateRenderTextureSingle(ref SSAOTexB);
            CreateRenderTexture2(ref HFA);
            CreateRenderTexture2(ref HFB);
            CreateRenderTexture2(ref HFPrev);
            CreateRenderTexture2(ref HFLAA);
            CreateRenderTexture2(ref HFLAB);
            CreateRenderTextureDouble(ref NormA);
            CreateRenderTextureDouble(ref NormB);
            CreateRenderTextureSingle(ref BlurHints);
            CreateRenderTexture4(ref DebugTex);
            shader.SetInt("screen_width", ScreenWidth);
            shader.SetInt("screen_height", ScreenHeight);
        }
        Vector3 PrevCamPos = Vector3.zero;
        public void Do(ref ComputeBuffer _ColorBuffer, ref RenderTexture Output, RenderTexture TEX_PT_VIEW_DEPTH_B, RenderTexture CorrectedDepthTex, int CurFrame, CommandBuffer cmd, ComputeBuffer ScreenSpaceBuffer, ComputeBuffer ReSTIRBuffer, ComputeBuffer PrevReSTIRBuffer, bool UseReSTIRGI, ref RenderTexture Albedo, float ScaleMultiplier, float BlurRadius) {
            bool DoUpscale = ScaleMultiplier != 1;
            shader.SetFloat("CameraDist", Vector3.Distance(camera.transform.position, PrevCamPos));
            shader.SetFloat("gBlurRadius", BlurRadius * ScaleMultiplier);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "Albedo", Albedo);
            cmd.SetComputeTextureParam(shader, CopyColorKernel, "Albedo", Albedo);
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "Albedo", Albedo);
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "SSAORead", SSAOTexB);
            cmd.BeginSample("ReCur");
            shader.SetInt("CurFrame", CurFrame);
            shader.SetBool("DoUpscale", DoUpscale);
            bool EvenFrame = CurFrame % 2 == 0;
            cmd.BeginSample("ReCur Copy");
            cmd.SetComputeIntParam(shader, "PassNum", 0);
            shader.SetBuffer(CopyColorKernel, "PerPixelRadiance", _ColorBuffer);
            cmd.SetComputeBufferParam(shader, CopyColorKernel, "ScreenSpaceInfo", ScreenSpaceBuffer);
            cmd.SetComputeTextureParam(shader, CopyColorKernel, "HintsWrite", BlurHints);
            cmd.SetComputeTextureParam(shader, CopyColorKernel, "HFA",  EvenFrame ? HFA : HFPrev);
            cmd.SetComputeTextureParam(shader, CopyColorKernel, "NormA", EvenFrame ? NormA : NormB);
            cmd.DispatchCompute(shader, CopyColorKernel, Mathf.CeilToInt(ScreenWidth / 32.0f), Mathf.CeilToInt(ScreenHeight / 32.0f), 1);
            cmd.EndSample("ReCur Copy");
            
            cmd.BeginSample("ReCur SSAO");
                cmd.BeginSample("ReCur SSAO Create");
                shader.SetMatrix("CameraToWorld", camera.cameraToWorldMatrix);
                shader.SetMatrix("ViewProj", camera.projectionMatrix * camera.worldToCameraMatrix);
                shader.SetMatrix("CamInvProj", camera.projectionMatrix.inverse);
                cmd.SetComputeTextureParam(shader, SSAOKernel, "NormB", EvenFrame ? NormA : NormB);
                cmd.SetComputeTextureParam(shader, SSAOKernel, "CurDepth", EvenFrame ? CorrectedDepthTex : TEX_PT_VIEW_DEPTH_B);
                cmd.SetComputeTextureParam(shader, SSAOKernel, "SSAOWrite", SSAOTexA);
                cmd.DispatchCompute(shader, SSAOKernel, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);
                cmd.EndSample("ReCur SSAO Create");
                cmd.BeginSample("ReCur SSAO Filter");
                cmd.SetComputeTextureParam(shader, SSAOKernel + 1, "NormB", EvenFrame ? NormA : NormB);
                cmd.SetComputeTextureParam(shader, SSAOKernel + 1, "CurDepth", EvenFrame ? CorrectedDepthTex : TEX_PT_VIEW_DEPTH_B);
                cmd.SetComputeTextureParam(shader, SSAOKernel + 1, "SSAOWrite", SSAOTexB);
                cmd.SetComputeTextureParam(shader, SSAOKernel + 1, "SSAORead", SSAOTexA);
                cmd.DispatchCompute(shader, SSAOKernel + 1, Mathf.CeilToInt(ScreenWidth / 8.0f), Mathf.CeilToInt(ScreenHeight / 8.0f), 1);
                cmd.EndSample("ReCur SSAO Filter");
            cmd.EndSample("ReCur SSAO");

            cmd.SetComputeTextureParam(shader, MainBlurKernel, "HintsRead", BlurHints);

            cmd.BeginSample("ReCur Fast Temporal");
            if(EvenFrame) {
                HFPrev.GenerateMips();
            } else {
                HFA.GenerateMips();
            }
            cmd.SetComputeIntParam(shader, "PassNum", 1);
            shader.SetTextureFromGlobal(TemporalFastKernel, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
            shader.SetBool("UseReSTIRGI", UseReSTIRGI);
            cmd.SetComputeBufferParam(shader, TemporalFastKernel, "CurrentReservoirGI", ReSTIRBuffer);
            cmd.SetComputeBufferParam(shader, TemporalFastKernel, "PrevReservoirGI", PrevReSTIRBuffer);
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "DebugTex", DebugTex);
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "NormB", (!EvenFrame) ? NormA : NormB);//not an error in order, I use NormB here as NormA because I can samplelevel NormB but not NormA
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "NormA", (EvenFrame) ? NormA : NormB);
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "CurDepth", EvenFrame ? CorrectedDepthTex : TEX_PT_VIEW_DEPTH_B);
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "PrevDepth", (!EvenFrame) ? CorrectedDepthTex : TEX_PT_VIEW_DEPTH_B);
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "HFA", EvenFrame ? HFA : HFPrev);
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "HFPrev", (!EvenFrame) ? HFA : HFPrev);
            cmd.DispatchCompute(shader, TemporalFastKernel, Mathf.CeilToInt(ScreenWidth / 8.0f), Mathf.CeilToInt(ScreenHeight / 8.0f), 1);
            cmd.EndSample("ReCur Fast Temporal");

            cmd.BeginSample("ReCur Blit");
            cmd.Blit(EvenFrame ? HFA : HFPrev, (!EvenFrame) ? HFA : HFPrev);
            cmd.EndSample("ReCur Blit");

            cmd.BeginSample("ReCur Main Blur");
            cmd.SetComputeIntParam(shader, "PassNum", 2);
            shader.SetTextureFromGlobal(MainBlurKernel, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "DebugTex", DebugTex);
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "NormB", (EvenFrame) ? NormA : NormB);
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "CurDepth", EvenFrame ? CorrectedDepthTex : TEX_PT_VIEW_DEPTH_B);
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "PrevDepth", (!EvenFrame) ? CorrectedDepthTex : TEX_PT_VIEW_DEPTH_B);
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "HFA", EvenFrame ? HFA : HFPrev);
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "HFB", (!EvenFrame) ? HFA : HFPrev);
            cmd.DispatchCompute(shader, MainBlurKernel, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);
            cmd.EndSample("ReCur Main Blur");

            cmd.BeginSample("ReCur Output Blur");
            cmd.SetComputeIntParam(shader, "PassNum", 13);
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "HFA", HFB);
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "HFB", EvenFrame ? HFA : HFPrev);
            cmd.DispatchCompute(shader, MainBlurKernel, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);
            cmd.EndSample("ReCur Output Blur");

            // cmd.BeginSample("ReCur Edge Clean Blur");
            // cmd.SetComputeIntParam(shader, "PassNum", 14);
            // cmd.SetComputeTextureParam(shader, MainBlurKernel, "HFA", (!EvenFrame) ? HFA : HFPrev);
            // cmd.SetComputeTextureParam(shader, MainBlurKernel, "HFB", HFB);
            // cmd.DispatchCompute(shader, MainBlurKernel, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);
            // cmd.EndSample("ReCur Edge Clean Blur");

            if(EvenFrame) {
                HFLAB.GenerateMips();
            } else {
                HFLAA.GenerateMips();
            }

            cmd.BeginSample("ReCur Slow Temporal");
            cmd.SetComputeIntParam(shader, "PassNum", 4);
            shader.SetTextureFromGlobal(TemporalSlowKernel, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "NormB", (!EvenFrame) ? NormA : NormB);//not an error in order, I use NormB here as NormA because I can samplelevel NormB but not NormA
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "NormA", (EvenFrame) ? NormA : NormB);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "DebugTex", DebugTex);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "HFA", (EvenFrame) ? HFLAA : HFLAB);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "HFPrev", (!EvenFrame) ? HFLAA : HFLAB);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "HFB", HFB);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "CurDepth", EvenFrame ? CorrectedDepthTex : TEX_PT_VIEW_DEPTH_B);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "PrevDepth", (!EvenFrame) ? CorrectedDepthTex : TEX_PT_VIEW_DEPTH_B);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "Output", Output);
            cmd.DispatchCompute(shader, TemporalSlowKernel, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);
            cmd.EndSample("ReCur Slow Temporal");
            PrevCamPos = camera.transform.position;

            cmd.EndSample("ReCur");
        }
    }
}