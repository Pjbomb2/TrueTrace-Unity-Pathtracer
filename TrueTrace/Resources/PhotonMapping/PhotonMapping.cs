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
        private ComputeBuffer DebugBuffer;
        private ComputeBuffer DebugCounter;
        public RenderTexture mpCausticPosBucket;
        public RenderTexture mpCausticDirBucket;
        public RenderTexture mpCausticFluxBucket;
        // public ComputeBuffer[] PhotonDensityMap;
        // public RenderTexture DensityDebugTex;

        public RenderTexture CDFX;
        public RenderTexture CDFY;
        public RenderTexture[] EquirectVisibilityTex;

        private ComputeShader CDFCompute;
        private ComputeBuffer CDFTotalBuffer;
        public int PerLightVisSize = 128;
        public bool ReInit = false;

        int FramesSinceStart = 0;
        uint NumPhotons = 2000000;
        public int analyticPhotons = 2000000;
        float mAnalyticInvPdf;
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
        int PrevLightCount;
        int GenKernel;
        int CollectKernel;
        

        int PhotonDensityMapClearKernel;
        int PhotonDensityMapSortKernel;

        public void ClearAll() {
            mpCausticHashPhotonCounter.ReleaseSafe();
            mpCausticPosBucket.ReleaseSafe();
            AABBBuffA.ReleaseSafe();
            // if(PhotonDensityMap != null) {
            //     PhotonDensityMap[0].ReleaseSafe();
            //     PhotonDensityMap[1].ReleaseSafe();
            // }
            DebugBuffer.ReleaseSafe();
            mpCausticDirBucket.ReleaseSafe();
            mpCausticFluxBucket.ReleaseSafe();
            EquirectVisibilityTex.ReleaseSafe();
            CDFX.ReleaseSafe();
            CDFY.ReleaseSafe();
        }

        public void Init() {
            PerLightVisSize = RayTracingMaster.RayMaster.LocalTTSettings.PhotonGuidingPerLightGuidingResolution;
            analyticPhotons = RayTracingMaster.RayMaster.LocalTTSettings.PhotonGuidingTotalPhotonsPerFrame;
            if (SPPMShader == null) {SPPMShader = Resources.Load<ComputeShader>("PhotonMapping/SPPM"); }
                CDFCompute = Resources.Load<ComputeShader>("Utility/CDFCreator");
            if(AssetManager.Assets == null || AssetManager.Assets.UnityLightCount == 0) return;
            ClearAll();
            ReInit = false;
            FramesSinceStart = 0;
            if (SPPMShader == null) {SPPMShader = Resources.Load<ComputeShader>("PhotonMapping/SPPM"); }
            GenKernel = SPPMShader.FindKernel("kernel_gen");
            CollectKernel = SPPMShader.FindKernel("kernel_collect");
            PhotonDensityMapClearKernel = SPPMShader.FindKernel("kernel_PhotonDensityClear");
            PhotonDensityMapSortKernel = SPPMShader.FindKernel("kernel_PhotonDensitySort");
            Initialized = true;

            if(CDFTotalBuffer == null || !CDFTotalBuffer.IsValid()) {
                CDFTotalBuffer = new ComputeBuffer(1, 4);
            }
                // PhotonDensityMap = new ComputeBuffer[2];
                // PhotonDensityMap[0] = new ComputeBuffer(AssetManager.Assets.UnityLightCount, 20);
                // PhotonDensityMap[1] = new ComputeBuffer(AssetManager.Assets.UnityLightCount, 20);
                // CommonFunctions.CreateRenderTextureArray2(ref PhotonDensityMap, AssetManager.Assets.UnityLightCount, 1, 2, CommonFunctions.RTFull4);
                // CommonFunctions.CreateRenderTexture(ref DensityDebugTex, AssetManager.Assets.UnityLightCount, 32, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTextureArray2(ref EquirectVisibilityTex, PerLightVisSize * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)), PerLightVisSize / 2 * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)), 2, CommonFunctions.RTFull4);
                CDFX.ReleaseSafe();
                CDFY.ReleaseSafe();
                CommonFunctions.CreateRenderTexture(ref CDFX, PerLightVisSize * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)), PerLightVisSize / 2 * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)), CommonFunctions.RTFull1);
                CommonFunctions.CreateRenderTexture(ref CDFY, PerLightVisSize / 2 * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)), 1, CommonFunctions.RTFull1);
                CounterBuffer = new ComputeBuffer(1, 4);
                CDFCompute.SetTexture(0, "Tex", EquirectVisibilityTex[0]);
                CDFCompute.SetTexture(0, "CDFX", CDFX);
                CDFCompute.SetTexture(0, "CDFY", CDFY);
                CDFCompute.SetInt("w", PerLightVisSize * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)));
                CDFCompute.SetInt("h", PerLightVisSize / 2 * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)));
                CDFCompute.SetBuffer(0, "CounterBuffer", CounterBuffer);
                CDFCompute.SetBuffer(0, "TotalBuff", CDFTotalBuffer);


            analyticPhotons += (AssetManager.Assets.UnityLightCount) - (analyticPhotons % (AssetManager.Assets.UnityLightCount));

            NumPhotons = (uint)analyticPhotons;

            mAnalyticInvPdf = (((float)NumPhotons) * ((float)AssetManager.Assets.UnityLightCount)) / ((float)analyticPhotons);

            uint blockSizeSq = blockSize * blockSize;
            uint xPhotons = (uint)((float)NumPhotons / (float)mMaxDispatchY) + 1;
            xPhotons += (xPhotons % blockSize == 0 && analyticPhotons > 0) ? blockSize : (blockSize - (xPhotons % blockSize));

            if(AssetManager.Assets.UnityLightCount > 0) {
                uint numCurrentLight = 0;
                uint step = (uint)((float)analyticPhotons / (float)(AssetManager.Assets.UnityLightCount));
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
            CommonFunctions.CreateRenderTexture(ref mpCausticFluxBucket, (int)Width, (int)Height, CommonFunctions.RTFull4);
            CommonFunctions.CreateDynamicBuffer(ref mpCausticHashPhotonCounter, (int)mNumBuckets, 4);
            CommonFunctions.CreateDynamicBuffer(ref AABBBuffA, 2, 40);
            CommonFunctions.CreateDynamicBuffer(ref DebugBuffer, 1000, 12);
            CommonFunctions.CreateDynamicBuffer(ref DebugCounter, 1, 4);


        }

        public void Generate(CommandBuffer cmd, float DirectionalLightCoverageRadius, Matrix4x4 TTviewprojection, Vector3 TTCamPos, Vector3 TTCamForward, RenderTexture DistanceTex, float IntensityMultiplier, float PhotonGuidingRatio) {
            if (SPPMShader == null) {SPPMShader = Resources.Load<ComputeShader>("PhotonMapping/SPPM"); }
                CDFCompute = Resources.Load<ComputeShader>("Utility/CDFCreator");
            if(AssetManager.Assets == null || AssetManager.Assets.UnityLightCount == 0) return;
            if(PrevLightCount != AssetManager.Assets.UnityLightCount || ReInit || AABBBuffA == null || !AABBBuffA.IsValid() || mpCausticHashPhotonCounter == null || !mpCausticHashPhotonCounter.IsValid() || EquirectVisibilityTex == null || EquirectVisibilityTex[0] == null || EquirectVisibilityTex[1] == null) Init();
            PrevLightCount = AssetManager.Assets.UnityLightCount;
            FramesSinceStart++;
            bool FlipFrame = (FramesSinceStart % 2) == 0;
            // Vector3[] TempDat = new Vector3[1000];
            // uint[] TempDat2 = new uint[1];

            // cmd.SetBufferData(DebugBuffer, TempDat);
            // cmd.SetBufferData(DebugCounter, TempDat2);
            cmd.SetComputeIntParam(SPPMShader, "PhotonFrames", FramesSinceStart);
            cmd.SetComputeIntParam(SPPMShader, "MaxBounce", (int)12);
            cmd.SetComputeIntParam(SPPMShader, "CurBounce", (int)1);
            cmd.SetComputeIntParam(SPPMShader, "screen_width", (int)Width);
            cmd.SetComputeIntParam(SPPMShader, "mPGDDispatchX", (int)mPGDDispatchX);
            cmd.SetComputeIntParam(SPPMShader, "mMaxDispatchY", (int)mMaxDispatchY);
            cmd.SetComputeIntParam(SPPMShader, "screen_height", (int)Height);
            cmd.SetComputeIntParam(SPPMShader, "mNumBuckets", (int)mNumBuckets);
            cmd.SetComputeIntParam(SPPMShader, "analyticPhotons", analyticPhotons);
            cmd.SetComputeFloatParam(SPPMShader, "CDFWIDTH", (float)PerLightVisSize);
            cmd.SetComputeFloatParam(SPPMShader, "PhotonGuidingRatio", PhotonGuidingRatio);
            cmd.SetComputeVectorParam(SPPMShader, "TTCamPos", TTCamPos);
            cmd.SetComputeVectorParam(SPPMShader, "TTForward", TTCamForward);
            cmd.SetComputeMatrixParam(SPPMShader, "TTviewprojection", TTviewprojection);
            cmd.SetComputeFloatParam(SPPMShader, "DirectionalLightCoverageRadius", DirectionalLightCoverageRadius);
            cmd.SetComputeFloatParam(SPPMShader, "IntensityMultiplier", IntensityMultiplier);

            cmd.SetComputeTextureParam(SPPMShader, 0, "gHashBucketPos", mpCausticPosBucket);
            cmd.SetComputeTextureParam(SPPMShader, 0, "gHashBucketFlux", mpCausticFluxBucket);
            cmd.SetComputeTextureParam(SPPMShader, 0, "gHashBucketDir", mpCausticDirBucket);
            cmd.SetComputeBufferParam(SPPMShader, 0, "gHashCounter", mpCausticHashPhotonCounter);
            cmd.SetComputeBufferParam(SPPMShader, 0, "AABBBuff", AABBBuffA);
            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("SPPM Init A");
            cmd.DispatchCompute(SPPMShader, 0, Mathf.CeilToInt((float)Width / 16.0f), Mathf.CeilToInt((float)Height / 16.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("SPPM Init A");
            cmd.SetComputeBufferParam(SPPMShader, 1, "gHashCounter", mpCausticHashPhotonCounter);
            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("SPPM Init B");
            cmd.DispatchCompute(SPPMShader, 1, Mathf.CeilToInt((float)mNumBuckets / 1024.0f), 1, 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("SPPM Init B");


            // cmd.SetComputeBufferParam(SPPMShader, PhotonDensityMapClearKernel, "PhotonDensityMapB", !FlipFrame ? PhotonDensityMap[0] : PhotonDensityMap[1]);//WRITE
            // cmd.SetComputeTextureParam(SPPMShader, PhotonDensityMapClearKernel, "DensityDebugTex", DensityDebugTex);//WRITE
            // cmd.DispatchCompute(SPPMShader, PhotonDensityMapClearKernel, Mathf.CeilToInt(((float)AssetManager.Assets.UnityLightCount) / 16.0f), 1, 1);



            AssetManager.Assets.SetLightData(SPPMShader, GenKernel);
            AssetManager.Assets.SetMeshTraceBuffers(SPPMShader, GenKernel);
            AssetManager.Assets.SetHeightmapTraceBuffers(SPPMShader, GenKernel);
            cmd.SetComputeTextureParam(SPPMShader, GenKernel, "gHashBucketPos", mpCausticPosBucket);
            cmd.SetComputeTextureParam(SPPMShader, GenKernel, "gHashBucketFlux", mpCausticFluxBucket);
            // cmd.SetComputeBufferParam(SPPMShader, GenKernel, "PhotonDensityMapA", FlipFrame ? PhotonDensityMap[0] : PhotonDensityMap[1]);//READ
            // cmd.SetComputeBufferParam(SPPMShader, GenKernel, "PhotonDensityMapB", !FlipFrame ? PhotonDensityMap[0] : PhotonDensityMap[1]);//READ
            cmd.SetComputeTextureParam(SPPMShader, GenKernel, "VisTexA", FlipFrame ? EquirectVisibilityTex[0] : EquirectVisibilityTex[1]);//READ
            cmd.SetComputeTextureParam(SPPMShader, GenKernel, "VisTexB", !FlipFrame ? EquirectVisibilityTex[0] : EquirectVisibilityTex[1]);//WRITE
            cmd.SetComputeTextureParam(SPPMShader, GenKernel, "gHashBucketDir", mpCausticDirBucket);
            cmd.SetComputeBufferParam(SPPMShader, GenKernel, "AABBBuff", AABBBuffA);
            cmd.SetComputeBufferParam(SPPMShader, GenKernel, "DebugBuffer", DebugBuffer);
            cmd.SetComputeBufferParam(SPPMShader, GenKernel, "DebugCounter", DebugCounter);
            cmd.SetComputeBufferParam(SPPMShader, GenKernel, "gHashCounter", mpCausticHashPhotonCounter);
            cmd.SetComputeTextureParam(SPPMShader, GenKernel, "DistTex", DistanceTex);
            cmd.SetComputeTextureParam(SPPMShader, GenKernel, "CDFX", CDFX);
            cmd.SetComputeTextureParam(SPPMShader, GenKernel, "CDFY", CDFY);
            cmd.SetComputeBufferParam(SPPMShader, GenKernel, "TotSum", CDFTotalBuffer);
            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("SPPM Gen");
            cmd.DispatchCompute(SPPMShader, GenKernel, Mathf.CeilToInt((float)mPGDDispatchX / 32.0f), Mathf.CeilToInt((float)mMaxDispatchY / 32.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("SPPM Gen");


            // cmd.SetComputeBufferParam(SPPMShader, PhotonDensityMapSortKernel, "PhotonDensityMapB", !FlipFrame ? PhotonDensityMap[0] : PhotonDensityMap[1]);//READ
            // cmd.SetComputeTextureParam(SPPMShader, PhotonDensityMapSortKernel, "DensityDebugTex", DensityDebugTex);//WRITE
            // cmd.DispatchCompute(SPPMShader, PhotonDensityMapSortKernel, 1, 1, 1);


            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("SPPM Init C");
            cmd.SetComputeTextureParam(SPPMShader, 2, "VisTexB", !FlipFrame ? EquirectVisibilityTex[0] : EquirectVisibilityTex[1]);
            cmd.SetComputeTextureParam(SPPMShader, 2, "CDFXWRITE", CDFX);
            cmd.SetComputeBufferParam(SPPMShader, 2, "AABBBuff", AABBBuffA);
            cmd.SetComputeTextureParam(SPPMShader, 2, "CDFYWRITE", CDFY);
            cmd.DispatchCompute(SPPMShader, 2, PerLightVisSize / 32 * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)), PerLightVisSize / 64 * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("SPPM Init C");


            if(CDFTotalBuffer == null || !CDFTotalBuffer.IsValid()) {
                CDFTotalBuffer = new ComputeBuffer(1, 4);
            }

            if(CounterBuffer == null || !CounterBuffer.IsValid()) {
                CounterBuffer = new ComputeBuffer(1, 4);
            }

            if(EquirectVisibilityTex == null || EquirectVisibilityTex[0] == null || EquirectVisibilityTex[1] == null) CommonFunctions.CreateRenderTextureArray2(ref EquirectVisibilityTex, PerLightVisSize * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)), PerLightVisSize / 2 * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)), 2, CommonFunctions.RTFull2);
                CDFCompute = Resources.Load<ComputeShader>("Utility/CDFCreator");
            if(CDFX == null) {
                CDFX.ReleaseSafe();
                CDFY.ReleaseSafe();
                CommonFunctions.CreateRenderTexture(ref CDFX, PerLightVisSize * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)), PerLightVisSize / 2 * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)), CommonFunctions.RTFull1);
                CommonFunctions.CreateRenderTexture(ref CDFY, PerLightVisSize / 2 * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)), 1, CommonFunctions.RTFull1);

            }
                CDFCompute.SetTexture(0, "Tex", EquirectVisibilityTex[0]);
                CDFCompute.SetTexture(0, "CDFX", CDFX);
                CDFCompute.SetTexture(0, "CDFY", CDFY);
                CDFCompute.SetInt("w", PerLightVisSize * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)));
                CDFCompute.SetInt("h", PerLightVisSize / 2 * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)));
                CDFCompute.SetBuffer(0, "CounterBuffer", CounterBuffer);
                CDFCompute.SetBuffer(0, "TotalBuff", CDFTotalBuffer);



            float[] CDFTotalInit = new float[1];
            cmd.SetBufferData(CDFTotalBuffer, CDFTotalInit);
            int[] CounterInit = new int[1];
            CounterBuffer.SetData(CounterInit);
            cmd.SetComputeTextureParam(CDFCompute, 0, "Tex", !FlipFrame ? EquirectVisibilityTex[0] : EquirectVisibilityTex[1]);
            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("SPPM CDF");
            cmd.DispatchCompute(CDFCompute, 0, 1, PerLightVisSize / 2 * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("SPPM CDF");

            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("SPPM Init D");
            cmd.SetComputeTextureParam(SPPMShader, 3, "VisTexB", FlipFrame ? EquirectVisibilityTex[0] : EquirectVisibilityTex[1]);
            cmd.DispatchCompute(SPPMShader, 3, PerLightVisSize / 32 * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)), PerLightVisSize / 64 * Mathf.CeilToInt(Mathf.Sqrt((float)AssetManager.Assets.UnityLightCount)), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("SPPM Init D");

        }

        public void Collect(CommandBuffer cmd, 
            ref RenderTexture Throughput, 
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

            cmd.SetComputeTextureParam(SPPMShader, CollectKernel, "gHashBucketPos", mpCausticPosBucket);
            cmd.SetComputeTextureParam(SPPMShader, CollectKernel, "gHashBucketFlux", mpCausticFluxBucket);
            cmd.SetComputeTextureParam(SPPMShader, CollectKernel, "gHashBucketDir", mpCausticDirBucket);
            cmd.SetComputeTextureParam(SPPMShader, CollectKernel, "Result", Result);
            cmd.SetComputeBufferParam(SPPMShader, CollectKernel, "AABBBuff", AABBBuffA);
            cmd.SetComputeTextureParam(SPPMShader, CollectKernel, "ThroughputTex", Throughput);
            cmd.SetComputeTextureParam(SPPMShader, CollectKernel, "PrimaryTriangleInfo", PrimaryTriInfo);
            cmd.SetComputeBufferParam(SPPMShader, CollectKernel, "gHashCounter", mpCausticHashPhotonCounter);
            cmd.SetComputeBufferParam(SPPMShader, CollectKernel, "GlobalColors", ColBuffer);
            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("SPPM Collect");
            cmd.DispatchCompute(SPPMShader, CollectKernel, Mathf.CeilToInt((float)screen_width / 16.0f), Mathf.CeilToInt((float)screen_height / 16.0f), 1);
            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("SPPM Collect");
        }
        Vector3[] DebugData = new Vector3[1000];
        int[] DebugData2 = new int[1];
        public void OnDrawGizmos() {
            DebugBuffer.GetData(DebugData);
            DebugCounter.GetData(DebugData2);
            DebugData2[0] = Mathf.Min(DebugData2[0], 1000);
            for(int i = 0; i < DebugData2[0]; i++) {
                if(DebugData[i] != Vector3.zero) Gizmos.DrawSphere(DebugData[i], 0.01f);
            }
        }

    }
}