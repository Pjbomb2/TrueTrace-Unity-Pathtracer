using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CommonVars;

namespace TrueTrace {
    [System.Serializable]
    public class PhotonMapping
    {

        public ComputeShader SPPMShader;
        ComputeBuffer mpCausticHashPhotonCounter;
        ComputeBuffer AABBBuffA;
        ComputeBuffer AABBBuffB;
        public RenderTexture mpCausticPosBucket;
        public RenderTexture mpCausticDirBucket;
        public RenderTexture RndNumWrt;
        public RenderTexture mpCausticFluxBucket;
        public RenderTexture ValidDir;

        public RenderTexture CDFX;
        public RenderTexture CDFY;
        public RenderTexture[] EquirectVisibilityTex;

        private ComputeShader CDFCompute;
        private ComputeBuffer CDFTotalBuffer;
        private int VisTexWidth = 1024;

        int FramesSinceStart = 0;
        uint NumPhotons = 500000;
        int analyticPhotons = 500000;
        float mAnalyticInvPdf;
        int NumAnalyticLights = 1;
        uint blockSize = 16;
        uint mMaxDispatchY = 512;
        uint mPGDDispatchX;
        uint mBucketFixedYExtend = 512;
        uint mNumBucketBits = 18;
        uint Width;
        uint Height;
        uint mNumBuckets;
        public bool Initialized = false;

        ComputeBuffer CounterBuffer;

        int GenKernel;
        int CollectKernel;

        public void ClearAll() {
            mpCausticHashPhotonCounter.ReleaseSafe();
            mpCausticPosBucket.ReleaseSafe();
            RndNumWrt.ReleaseSafe();
            AABBBuffA.ReleaseSafe();
            AABBBuffB.ReleaseSafe();
            mpCausticDirBucket.ReleaseSafe();
            mpCausticFluxBucket.ReleaseSafe();
            EquirectVisibilityTex.ReleaseSafe();
            CDFX.ReleaseSafe();
            CDFY.ReleaseSafe();
        }

        public void Init() {
            VisTexWidth = 1024;
            FramesSinceStart = 0;
            if (SPPMShader == null) {SPPMShader = Resources.Load<ComputeShader>("PhotonMapping/SPPM"); }
            GenKernel = SPPMShader.FindKernel("kernel_gen");
            CollectKernel = SPPMShader.FindKernel("kernel_collect");
            Initialized = true;

            if(CDFTotalBuffer == null || !CDFTotalBuffer.IsValid()) {
                CDFTotalBuffer = new ComputeBuffer(1, 4);
            }
            if(CDFX == null) {
                CommonFunctions.CreateRenderTextureArray2(ref EquirectVisibilityTex, VisTexWidth, VisTexWidth / 2, 2, CommonFunctions.RTFull2);
                CDFX.ReleaseSafe();
                CDFY.ReleaseSafe();
                CDFCompute = Resources.Load<ComputeShader>("Utility/CDFCreator");
                CommonFunctions.CreateRenderTexture(ref CDFX, VisTexWidth, VisTexWidth / 2, CommonFunctions.RTFull1);
                CommonFunctions.CreateRenderTexture(ref CDFY, VisTexWidth / 2, 1, CommonFunctions.RTFull1);
                CounterBuffer = new ComputeBuffer(1, 4);
                CDFCompute.SetTexture(1, "Tex2", EquirectVisibilityTex[0]);
                CDFCompute.SetTexture(1, "CDFX", CDFX);
                CDFCompute.SetTexture(1, "CDFY", CDFY);
                CDFCompute.SetInt("w", VisTexWidth);
                CDFCompute.SetInt("h", VisTexWidth / 2);
                CDFCompute.SetBuffer(1, "CounterBuffer", CounterBuffer);
                CDFCompute.SetBuffer(1, "TotalBuff", CDFTotalBuffer);

            }

            analyticPhotons += NumAnalyticLights - (analyticPhotons % NumAnalyticLights);

            NumPhotons = (uint)analyticPhotons;

            mAnalyticInvPdf = (((float)NumPhotons) * ((float)NumAnalyticLights)) / ((float)analyticPhotons);

            uint blockSizeSq = blockSize * blockSize;
            uint xPhotons = (uint)((float)NumPhotons / (float)mMaxDispatchY) + 1;
            xPhotons += (xPhotons % blockSize == 0 && analyticPhotons > 0) ? blockSize : (blockSize - (xPhotons % blockSize));

            if(NumAnalyticLights > 0) {
                uint numCurrentLight = 0;
                uint step = (uint)((float)analyticPhotons / (float)NumAnalyticLights);
                bool stop = false;
                for(uint i = 0; i <= analyticPhotons / blockSizeSq; i++) {
                    if(stop) break;
                    for(uint y = 0; y < blockSize; y++) {
                        if(stop) break;
                        for(uint x = 0; x < blockSize; x++) {
                            if(numCurrentLight >= analyticPhotons) {
                                stop = true;
                                break;
                            }
                            uint idxx = (i * i) % xPhotons;
                            uint idxy = ((i * i) / xPhotons) * blockSize;
                            idxx += x;
                            idxy += y;
                            int lightIdx = (int)((float)numCurrentLight / (float)step) + 1;
                            numCurrentLight++;
                        }
                    }
                }
            }
            mPGDDispatchX = xPhotons;
            NumPhotons = mPGDDispatchX * mMaxDispatchY;

            mNumBuckets = (uint)(1 << (int)mNumBucketBits);
            //Get xy extend
            Width = mNumBuckets % mBucketFixedYExtend == 0 ? mNumBuckets / mBucketFixedYExtend : (mNumBuckets / mBucketFixedYExtend) + 1;
            Height = mBucketFixedYExtend;
            CommonFunctions.CreateRenderTexture(ref mpCausticPosBucket, (int)Width, (int)Height, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref mpCausticDirBucket, (int)Width, (int)Height, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref RndNumWrt, (int)Width, (int)Height, CommonFunctions.RTFull4);
            CommonFunctions.CreateRenderTexture(ref mpCausticFluxBucket, (int)Width, (int)Height, CommonFunctions.RTFull4);
            CommonFunctions.CreateDynamicBuffer(ref mpCausticHashPhotonCounter, (int)mNumBuckets, 4);
            CommonFunctions.CreateDynamicBuffer(ref AABBBuffA, 2, 40);


        }

        public void Generate(CommandBuffer cmd) {
            if(AABBBuffA == null || !AABBBuffA.IsValid() || mpCausticHashPhotonCounter == null || !mpCausticHashPhotonCounter.IsValid() || EquirectVisibilityTex == null || EquirectVisibilityTex[0] == null || EquirectVisibilityTex[1] == null) Init();
            FramesSinceStart++;
            bool FlipFrame = (FramesSinceStart % 2) == 0;
            cmd.SetComputeIntParam(SPPMShader, "PhotonFrames", FramesSinceStart);
            cmd.SetComputeIntParam(SPPMShader, "TEMPTESTA", 1);
            cmd.SetComputeIntParam(SPPMShader, "MaxBounce", (int)12);
            cmd.SetComputeIntParam(SPPMShader, "CurBounce", (int)1);
            cmd.SetComputeIntParam(SPPMShader, "screen_width", (int)Width);
            cmd.SetComputeIntParam(SPPMShader, "mPGDDispatchX", (int)mPGDDispatchX);
            cmd.SetComputeIntParam(SPPMShader, "mMaxDispatchY", (int)mMaxDispatchY);
            cmd.SetComputeIntParam(SPPMShader, "screen_height", (int)Height);
            cmd.SetComputeIntParam(SPPMShader, "mNumBuckets", (int)mNumBuckets);

            cmd.SetComputeTextureParam(SPPMShader, 0, "gHashBucketPos", mpCausticPosBucket);
            cmd.SetComputeTextureParam(SPPMShader, 0, "gHashBucketFlux", mpCausticFluxBucket);
            cmd.SetComputeTextureParam(SPPMShader, 0, "gHashBucketDir", mpCausticDirBucket);
            cmd.SetComputeTextureParam(SPPMShader, 0, "RandomNumsWrite", RndNumWrt);
            cmd.SetComputeBufferParam(SPPMShader, 0, "gHashCounter", mpCausticHashPhotonCounter);
            cmd.SetComputeBufferParam(SPPMShader, 0, "AABBBuff", AABBBuffA);
            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("SPPM Init A");
            cmd.DispatchCompute(SPPMShader, 0, Mathf.CeilToInt((float)Width / 16.0f), Mathf.CeilToInt((float)Height / 16.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("SPPM Init A");
            cmd.SetComputeBufferParam(SPPMShader, 1, "gHashCounter", mpCausticHashPhotonCounter);
            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("SPPM Init B");
            cmd.DispatchCompute(SPPMShader, 1, Mathf.CeilToInt((float)mNumBuckets / 1024.0f), 1, 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("SPPM Init B");




            AssetManager.Assets.SetLightData(SPPMShader, GenKernel);
            AssetManager.Assets.SetMeshTraceBuffers(SPPMShader, GenKernel);
            AssetManager.Assets.SetHeightmapTraceBuffers(SPPMShader, GenKernel);
            cmd.SetComputeTextureParam(SPPMShader, GenKernel, "gHashBucketPos", mpCausticPosBucket);
            cmd.SetComputeTextureParam(SPPMShader, GenKernel, "gHashBucketFlux", mpCausticFluxBucket);
            cmd.SetComputeTextureParam(SPPMShader, GenKernel, "RandomNumsWrite", RndNumWrt);
            cmd.SetComputeTextureParam(SPPMShader, GenKernel, "VisTexA", FlipFrame ? EquirectVisibilityTex[0] : EquirectVisibilityTex[1]);//READ
            cmd.SetComputeTextureParam(SPPMShader, GenKernel, "VisTexB", !FlipFrame ? EquirectVisibilityTex[0] : EquirectVisibilityTex[1]);//WRITE
            cmd.SetComputeTextureParam(SPPMShader, GenKernel, "gHashBucketDir", mpCausticDirBucket);
            cmd.SetComputeBufferParam(SPPMShader, GenKernel, "AABBBuff", AABBBuffA);
            cmd.SetComputeBufferParam(SPPMShader, GenKernel, "gHashCounter", mpCausticHashPhotonCounter);
            cmd.SetComputeTextureParam(SPPMShader, GenKernel, "CDFX", CDFX);
            cmd.SetComputeTextureParam(SPPMShader, GenKernel, "CDFY", CDFY);
            cmd.SetComputeBufferParam(SPPMShader, GenKernel, "TotSum", CDFTotalBuffer);
            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("SPPM Gen");
            cmd.DispatchCompute(SPPMShader, GenKernel, Mathf.CeilToInt((float)mPGDDispatchX / 32.0f), Mathf.CeilToInt((float)mMaxDispatchY / 32.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("SPPM Gen");


            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("SPPM Init C");
            cmd.SetComputeTextureParam(SPPMShader, 2, "VisTexB", !FlipFrame ? EquirectVisibilityTex[0] : EquirectVisibilityTex[1]);
            cmd.SetComputeTextureParam(SPPMShader, 2, "CDFXWRITE", CDFX);
            cmd.SetComputeTextureParam(SPPMShader, 2, "CDFYWRITE", CDFY);
            cmd.DispatchCompute(SPPMShader, 2, VisTexWidth / 32, VisTexWidth / 64, 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("SPPM Init C");


            if(CDFTotalBuffer == null || !CDFTotalBuffer.IsValid()) {
                CDFTotalBuffer = new ComputeBuffer(1, 4);
            }

            if(CounterBuffer == null || !CounterBuffer.IsValid()) {
                CounterBuffer = new ComputeBuffer(1, 4);
            }

            if(EquirectVisibilityTex == null || EquirectVisibilityTex[0] == null || EquirectVisibilityTex[1] == null) CommonFunctions.CreateRenderTextureArray2(ref EquirectVisibilityTex, VisTexWidth, VisTexWidth / 2, 2, CommonFunctions.RTFull2);
                CDFCompute = Resources.Load<ComputeShader>("Utility/CDFCreator");
            if(CDFX == null) {
                CDFX.ReleaseSafe();
                CDFY.ReleaseSafe();
                CommonFunctions.CreateRenderTexture(ref CDFX, VisTexWidth, VisTexWidth / 2, CommonFunctions.RTFull1);
                CommonFunctions.CreateRenderTexture(ref CDFY, VisTexWidth / 2, 1, CommonFunctions.RTFull1);

            }
                CDFCompute.SetTexture(1, "Tex2", EquirectVisibilityTex[0]);
                CDFCompute.SetTexture(1, "CDFX", CDFX);
                CDFCompute.SetTexture(1, "CDFY", CDFY);
                CDFCompute.SetInt("w", VisTexWidth);
                CDFCompute.SetInt("h", VisTexWidth / 2);
                CDFCompute.SetBuffer(1, "CounterBuffer", CounterBuffer);
                CDFCompute.SetBuffer(1, "TotalBuff", CDFTotalBuffer);


            float[] CDFTotalInit = new float[1];
            CDFTotalBuffer.SetData(CDFTotalInit);
            int[] CounterInit = new int[1];
            CounterBuffer.SetData(CounterInit);
            cmd.SetComputeTextureParam(CDFCompute, 1, "Tex2", !FlipFrame ? EquirectVisibilityTex[0] : EquirectVisibilityTex[1]);
            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("SPPM CDF");
            cmd.DispatchCompute(CDFCompute, 1, 1, VisTexWidth / 2, 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("SPPM CDF");

            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("SPPM Init D");
            cmd.SetComputeTextureParam(SPPMShader, 3, "VisTexB", FlipFrame ? EquirectVisibilityTex[0] : EquirectVisibilityTex[1]);
            cmd.DispatchCompute(SPPMShader, 3, VisTexWidth / 32, VisTexWidth / 64, 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("SPPM Init D");

        }

        public void Collect(CommandBuffer cmd, 
            ref RenderTexture Throughput, 
            ref RenderTexture ViewDir, 
            ref RenderTexture PrimaryTriInfo, 
            ref RenderTexture Result, 
            ref ComputeBuffer ColBuffer, 
            int screen_width, 
            int screen_height) {
            AssetManager.Assets.SetLightData(SPPMShader, CollectKernel);
            AssetManager.Assets.SetMeshTraceBuffers(SPPMShader, CollectKernel);
            AssetManager.Assets.SetHeightmapTraceBuffers(SPPMShader, CollectKernel);
            cmd.SetComputeIntParam(SPPMShader, "Width", (int)Width);
            cmd.SetComputeIntParam(SPPMShader, "screen_width", (int)screen_width);
            cmd.SetComputeIntParam(SPPMShader, "mPGDDispatchX", (int)mPGDDispatchX);
            cmd.SetComputeIntParam(SPPMShader, "mMaxDispatchY", (int)mMaxDispatchY);
            cmd.SetComputeIntParam(SPPMShader, "Height", (int)Height);
            cmd.SetComputeIntParam(SPPMShader, "screen_height", (int)screen_height);
            cmd.SetComputeIntParam(SPPMShader, "mNumBuckets", (int)mNumBuckets);

            cmd.SetComputeTextureParam(SPPMShader, CollectKernel, "RandomNumsWrite", RndNumWrt);
            cmd.SetComputeTextureParam(SPPMShader, CollectKernel, "gHashBucketPos", mpCausticPosBucket);
            cmd.SetComputeTextureParam(SPPMShader, CollectKernel, "gHashBucketFlux", mpCausticFluxBucket);
            cmd.SetComputeTextureParam(SPPMShader, CollectKernel, "gHashBucketDir", mpCausticDirBucket);
            cmd.SetComputeTextureParam(SPPMShader, CollectKernel, "Result", Result);
            cmd.SetComputeTextureParam(SPPMShader, CollectKernel, "ThroughputTex", Throughput);
            cmd.SetComputeTextureParam(SPPMShader, CollectKernel, "PrimaryTriangleInfo", PrimaryTriInfo);
            cmd.SetComputeTextureParam(SPPMShader, CollectKernel, "ViewDirTex", ViewDir);
            cmd.SetComputeBufferParam(SPPMShader, CollectKernel, "gHashCounter", mpCausticHashPhotonCounter);
            cmd.SetComputeBufferParam(SPPMShader, CollectKernel, "GlobalColors", ColBuffer);
            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("SPPM Collect");
            cmd.DispatchCompute(SPPMShader, CollectKernel, Mathf.CeilToInt((float)screen_width / 16.0f), Mathf.CeilToInt((float)screen_height / 16.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("SPPM Collect");
        }

    }
}