using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CommonVars;

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

        public RenderTexture NormA;
        public RenderTexture NormB;

        public RenderTexture MomA;
        public RenderTexture MomB;


        public RenderTexture DepthA;
        public RenderTexture DepthB;


        public RenderTexture BlurHints;

        public RenderTexture GradA;
        public RenderTexture GradB;


        private int CopyColorKernel;
        private int MainBlurKernel;
        private int TemporalFastKernel;
        private int TemporalSlowKernel;
        private int SSAOKernel;
        private int GradKernel;


        Camera camera;
        int ScreenHeight;
        int ScreenWidth;

        public void init(int ScreenWidth, int ScreenHeight)
        {
            this.ScreenWidth = ScreenWidth;
            this.ScreenHeight = ScreenHeight;

            if (shader == null) { shader = Resources.Load<ComputeShader>("PostProcess/ReCurDenoiser/ReCur"); }

            CopyColorKernel = shader.FindKernel("ColorKernel");
            MainBlurKernel = shader.FindKernel("BlurKernel");
            TemporalFastKernel = shader.FindKernel("temporal");
            TemporalSlowKernel = shader.FindKernel("secondarytemporal");
            SSAOKernel = shader.FindKernel("SSAO");
            GradKernel = shader.FindKernel("Gradient_Atrous");


            CommonFunctions.CreateRenderTexture(ref MomA, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref MomB, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref DepthA, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf1);
            CommonFunctions.CreateRenderTexture(ref DepthB, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf1);
            CommonFunctions.CreateRenderTexture(ref SSAOTexA, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf1);
            CommonFunctions.CreateRenderTexture(ref SSAOTexB, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf1);
            CommonFunctions.CreateRenderTexture(ref BlurHints, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf1);
            CommonFunctions.CreateRenderTexture(ref HFA, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4,RenderTextureReadWrite.Linear);
            CommonFunctions.CreateRenderTexture(ref HFB, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4,RenderTextureReadWrite.Linear);
            CommonFunctions.CreateRenderTexture(ref HFPrev, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4,RenderTextureReadWrite.Linear);
            CommonFunctions.CreateRenderTexture(ref HFLAA, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4,RenderTextureReadWrite.Linear);
            CommonFunctions.CreateRenderTexture(ref HFLAB, ScreenWidth, ScreenHeight, CommonFunctions.RTHalf4,RenderTextureReadWrite.Linear);
            CommonFunctions.CreateRenderTexture(ref NormA, ScreenWidth, ScreenHeight, CommonFunctions.RTFull2);
            CommonFunctions.CreateRenderTexture(ref NormB, ScreenWidth, ScreenHeight, CommonFunctions.RTFull2);
            CommonFunctions.CreateRenderTexture(ref GradA, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            CommonFunctions.CreateRenderTexture(ref GradB, ScreenWidth / 3, ScreenHeight / 3, CommonFunctions.RTHalf2);
            shader.SetInt("screen_width", ScreenWidth);
            shader.SetInt("screen_height", ScreenHeight);
        }

        public void ClearAll() {
            if(MomA != null) {
                MomA.Release();
                MomB.Release();
                DepthA.Release();
                DepthB.Release();
                SSAOTexA.Release();
                SSAOTexB.Release();
                BlurHints.Release();
                HFA.Release();
                HFB.Release();
                HFPrev.Release();
                HFLAA.Release();
                HFLAB.Release();
                NormA.Release();
                NormB.Release();
                GradA.Release();
                GradB.Release();
            }
        }



        Vector3 PrevCamPos = Vector3.zero;
        public void Do(ref RenderTexture Output,
                        ref RenderTexture Albedo, 
                        ref ComputeBuffer _ColorBuffer,  
                            RenderTexture ScreenSpaceInfo, 
                            RenderTexture TEX_PT_VIEW_DEPTH_B, 
                            RenderTexture CorrectedDepthTex, 
                            RenderTexture ReSTIRGIA, 
                            RenderTexture ReSTIRGIB, 
                            RenderTexture WorldPosData,
                            CommandBuffer cmd, 
                            int CurFrame, 
                            bool UseReSTIRGI, 
                            float ScaleMultiplier, 
                            float BlurRadius, 
                            int PartialRenderingFactor,
                            float IndirectBoost,
                            RenderTexture Gradients) {

            camera = RayTracingMaster._camera;
            shader.SetFloat("ViewFrustrumTop", camera.projectionMatrix.decomposeProjection.top);
            shader.SetFloat("ViewFrustrumBottom", camera.projectionMatrix.decomposeProjection.bottom);
            shader.SetFloat("FarPlane", camera.farClipPlane);
            
            if(UseReSTIRGI) {
                cmd.SetComputeIntParam(shader, "iteration", 0);
                cmd.SetComputeTextureParam(shader, GradKernel, "GradB", Gradients);
                cmd.SetComputeTextureParam(shader, GradKernel, "GradA", GradA);
                cmd.DispatchCompute(shader, GradKernel, Mathf.CeilToInt(ScreenWidth / 3 / 16.0f), Mathf.CeilToInt(ScreenHeight / 3 / 16.0f), 1);

                cmd.SetComputeIntParam(shader, "iteration", 1);
                cmd.SetComputeTextureParam(shader, GradKernel, "GradB", GradA);
                cmd.SetComputeTextureParam(shader, GradKernel, "GradA", GradB);
                cmd.DispatchCompute(shader, GradKernel, Mathf.CeilToInt(ScreenWidth / 3 / 16.0f), Mathf.CeilToInt(ScreenHeight / 3 / 16.0f), 1);

                cmd.SetComputeIntParam(shader, "iteration", 2);
                cmd.SetComputeTextureParam(shader, GradKernel, "GradB", GradB);
                cmd.SetComputeTextureParam(shader, GradKernel, "GradA", GradA);
                cmd.DispatchCompute(shader, GradKernel, Mathf.CeilToInt(ScreenWidth / 3 / 16.0f), Mathf.CeilToInt(ScreenHeight / 3 / 16.0f), 1);

            }


            bool DoUpscale = ScaleMultiplier != 1;
            shader.SetFloat("CameraDist", Vector3.Distance(camera.transform.position, PrevCamPos));
            shader.SetFloat("IndirectBoost", IndirectBoost);
            shader.SetFloat("gBlurRadius", BlurRadius * ScaleMultiplier);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "Albedo", Albedo);
            cmd.SetComputeTextureParam(shader, CopyColorKernel, "Albedo", Albedo);
            cmd.SetComputeTextureParam(shader, CopyColorKernel, "WorldPosData", WorldPosData);
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "Albedo", Albedo);
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "SSAORead", SSAOTexB);
            shader.SetInt("CurFrame", CurFrame);
            shader.SetInt("PartialRenderingFactor", PartialRenderingFactor);
            shader.SetBool("DoUpscale", DoUpscale);
            bool EvenFrame = CurFrame % 2 == 0;
            cmd.BeginSample("ReCur Copy");
            cmd.SetComputeIntParam(shader, "PassNum", 0);
            shader.SetBuffer(CopyColorKernel, "PerPixelRadiance", _ColorBuffer);
            cmd.SetComputeTextureParam(shader, CopyColorKernel, "ScreenSpaceInfo", ScreenSpaceInfo);
            cmd.SetComputeTextureParam(shader, CopyColorKernel, "HintsWrite", BlurHints);
            cmd.SetComputeTextureParam(shader, CopyColorKernel, "HFA",  EvenFrame ? HFA : HFPrev);
            cmd.SetComputeTextureParam(shader, CopyColorKernel, "NormA", EvenFrame ? NormA : NormB);
            cmd.SetComputeTextureParam(shader, CopyColorKernel, "CurDepthWrite", EvenFrame ? DepthA : DepthB);
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
            cmd.SetComputeIntParam(shader, "PassNum", 1);
            shader.SetTextureFromGlobal(TemporalFastKernel, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
            shader.SetBool("UseReSTIRGI", UseReSTIRGI);
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "ReservoirDataA", ReSTIRGIA);
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "ReservoirDataB", ReSTIRGIB);
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "NormB", (!EvenFrame) ? NormA : NormB);//not an error in order, I use NormB here as NormA because I can samplelevel NormB but not NormA
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "NormA", (EvenFrame) ? NormA : NormB);
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "CurDepth", EvenFrame ? DepthA : DepthB);
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "PrevDepth", (!EvenFrame) ? DepthA : DepthB);
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "HFA", EvenFrame ? HFA : HFPrev);
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "HFPrev", (!EvenFrame) ? HFA : HFPrev);
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "MomentsA", (EvenFrame) ? MomA : MomB);
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "MomentsB", (!EvenFrame) ? MomA : MomB);
            cmd.SetComputeTextureParam(shader, TemporalFastKernel, "Gradients", GradA);
            cmd.DispatchCompute(shader, TemporalFastKernel, Mathf.CeilToInt(ScreenWidth / 8.0f), Mathf.CeilToInt(ScreenHeight / 8.0f), 1);
            cmd.EndSample("ReCur Fast Temporal");

            cmd.BeginSample("ReCur Blit");
            cmd.Blit(EvenFrame ? HFA : HFPrev, (!EvenFrame) ? HFA : HFPrev);
            cmd.Blit(EvenFrame ? MomA : MomB, (!EvenFrame) ? MomA : MomB);
            cmd.EndSample("ReCur Blit");

            cmd.BeginSample("ReCur Main Blur");
            cmd.SetComputeIntParam(shader, "PassNum", 2);
            shader.SetTextureFromGlobal(MainBlurKernel, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "NormB", (EvenFrame) ? NormA : NormB);
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "CurDepth", EvenFrame ? DepthA : DepthB);
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "PrevDepth", (!EvenFrame) ? DepthA : DepthB);
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "MomentsA", (EvenFrame) ? MomA : MomB);
            cmd.SetComputeTextureParam(shader, MainBlurKernel, "MomentsB", (!EvenFrame) ? MomA : MomB);
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

            cmd.BeginSample("ReCur Slow Temporal");
            cmd.SetComputeIntParam(shader, "PassNum", 4);
            shader.SetTextureFromGlobal(TemporalSlowKernel, "TEX_PT_MOTION", "_CameraMotionVectorsTexture");
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "ReservoirDataA", ReSTIRGIA);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "ReservoirDataB", ReSTIRGIB);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "NormB", (!EvenFrame) ? NormA : NormB);//not an error in order, I use NormB here as NormA because I can samplelevel NormB but not NormA
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "NormA", (EvenFrame) ? NormA : NormB);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "HFA", (EvenFrame) ? HFLAA : HFLAB);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "HFPrev", (!EvenFrame) ? HFLAA : HFLAB);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "HFB", HFB);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "CurDepth", EvenFrame ? DepthA : DepthB);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "PrevDepth", (!EvenFrame) ? DepthA : DepthB);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "Output", Output);
            cmd.SetComputeTextureParam(shader, TemporalSlowKernel, "Gradients", GradA);
            cmd.DispatchCompute(shader, TemporalSlowKernel, Mathf.CeilToInt(ScreenWidth / 16.0f), Mathf.CeilToInt(ScreenHeight / 16.0f), 1);
            cmd.EndSample("ReCur Slow Temporal");
            PrevCamPos = camera.transform.position;

        }
    }
}