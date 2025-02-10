using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CommonVars;
    
[System.Serializable]
public class GPULightBVHBuilder
{
    public ComputeShader GPUBVHShader;

    ComputeBuffer DimensionedIndices;
    ComputeBuffer SplitDataBuffer;
    ComputeBuffer ReadbackDataA;
    ComputeBuffer ReadbackDataB;
    ComputeBuffer WorkGroup;
    ComputeBuffer TempIndices;
    ComputeBuffer DispatchBuffer;
    ComputeBuffer LayerStridesBuffer;
    ComputeBuffer SingleParentBoundBuffer;
    public ComputeBuffer AABBBuffer;
    ComputeBuffer DebugBuffer;
    public CompactLightBVHData[] ParentBoundArray;

#if !DontUseSGTree
    public ComputeBuffer NodesBuffer;
#endif

    protected LocalKeyword SORT_LARGE;
    protected LocalKeyword SORT_MED;
    protected LocalKeyword SORT_SMALL;

    public uint[] DebugData;
    int PrimCount;
    public GPULightBVHBuilder(int TriCount) {
        PrimCount = TriCount;
        if (GPUBVHShader == null) {GPUBVHShader = Resources.Load<ComputeShader>("Builders/GPUBVHBuilders/GPULightBVH/GPULightBVHBuilder"); }
        
        CommonFunctions.CreateDynamicBuffer(ref DimensionedIndices, TriCount * 3, 4);
        CommonFunctions.CreateDynamicBuffer(ref SplitDataBuffer, (TriCount * 2 + 1), 12);
        CommonFunctions.CreateDynamicBuffer(ref ReadbackDataA, (TriCount * 2 + 1), 16);
        CommonFunctions.CreateDynamicBuffer(ref ReadbackDataB, (TriCount * 2 + 1), 16);
        CommonFunctions.CreateDynamicBuffer(ref WorkGroup, 4, 4);
        CommonFunctions.CreateDynamicBuffer(ref TempIndices, TriCount * 3, 4);
        CommonFunctions.CreateDynamicBuffer(ref DispatchBuffer, 16, 4);
        CommonFunctions.CreateDynamicBuffer(ref DebugBuffer, TriCount, 4);
        CommonFunctions.CreateDynamicBuffer(ref LayerStridesBuffer, 100, 16);
    #if !DontUseSGTree
        CommonFunctions.CreateDynamicBuffer(ref NodesBuffer, TriCount * 2, CommonFunctions.GetStride<CompactLightBVHData>());
    #endif
        CommonFunctions.CreateDynamicBuffer(ref SingleParentBoundBuffer, 1, CommonFunctions.GetStride<CompactLightBVHData>());
        CommonFunctions.CreateDynamicBuffer(ref AABBBuffer, TriCount, CommonFunctions.GetStride<CompactLightBVHData>());
        SORT_LARGE = new LocalKeyword(GPUBVHShader, "SORT_LARGE");
        SORT_MED = new LocalKeyword(GPUBVHShader, "SORT_MED");
        SORT_SMALL = new LocalKeyword(GPUBVHShader, "SORT_SMALL");
    }

#if !DontUseSGTree
    public unsafe void Run(ComputeBuffer LightTriBuffer, ComputeBuffer SGTreeBuffer, ComputeBuffer TransfBuffer, int KeyCount) {
#else
    public unsafe void Run(ComputeBuffer LightTriBuffer, ComputeBuffer NodesBuffer, ComputeBuffer TransfBuffer, int KeyCount) {
#endif
        if (GPUBVHShader == null) {GPUBVHShader = Resources.Load<ComputeShader>("Builders/GPUBVHBuilders/GPULightBVH/GPULightBVHBuilder"); }

        uint[] TempDispatchInit = new uint[4 * 4];
        System.Array.Fill(TempDispatchInit, (uint)1);
        DispatchBuffer.SetData(TempDispatchInit);

        int[] WorkGroupInit = new int[4];
        WorkGroupInit[3] = 2;
        WorkGroup.SetData(WorkGroupInit);


        Vector4[] RefitInit = new Vector4[100];
        System.Array.Fill(RefitInit, new Vector4((int)Mathf.Pow(2, 30), -(int)Mathf.Pow(2, 30), 0, 0));
        LayerStridesBuffer.SetData(RefitInit);

            // int[] DimensionedIndicesArray = new int[KeyCount * 3];
            // float[] CentersX = new float[KeyCount];
            // float[] CentersY = new float[KeyCount];
            // float[] CentersZ = new float[KeyCount];
            // int[] FinalIndices = new int[KeyCount];
            // for(int i = 0; i < KeyCount; i++) {
            //     FinalIndices[i] = i;
            //     CentersX[i] = (Triangles[i].BBMax.x - Triangles[i].BBMin.x) / 2.0f + Triangles[i].BBMin.x;
            //     CentersY[i] = (Triangles[i].BBMax.y - Triangles[i].BBMin.y) / 2.0f + Triangles[i].BBMin.y;
            //     CentersZ[i] = (Triangles[i].BBMax.z - Triangles[i].BBMin.z) / 2.0f + Triangles[i].BBMin.z;
            // }

            // Debug.Log("LIGHT COUNT: " + KeyCount);
            // System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersX[s1] - CentersX[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            // System.Array.Copy(FinalIndices, 0, DimensionedIndicesArray, 0, KeyCount);
            // for(int i = 0; i < KeyCount; i++) FinalIndices[i] = i;
            // System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersY[s1] - CentersY[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            // System.Array.Copy(FinalIndices, 0, DimensionedIndicesArray, KeyCount, KeyCount);



            // for(int i = 0; i < KeyCount; i++) FinalIndices[i] = i;
            // System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersZ[s1] - CentersZ[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            // System.Array.Copy(FinalIndices, 0, DimensionedIndicesArray, KeyCount * 2, KeyCount);

            // DimensionedIndices.SetData(DimensionedIndicesArray);

        int PropogateKernel = GPUBVHShader.FindKernel("Propogate");
        int SortKernel = GPUBVHShader.FindKernel("Sort");
        int SplitKernel = GPUBVHShader.FindKernel("SplitKernel");
        int TransferKernel = GPUBVHShader.FindKernel("Transfer");
        int AABBCopyKernel = GPUBVHShader.FindKernel("InitializeKernel");
        int AABBReductionKernel = GPUBVHShader.FindKernel("NodeBufferInitialization");
        int PostProcKernel = GPUBVHShader.FindKernel("PostProcKernel");
        int SGTreeKernel = GPUBVHShader.FindKernel("SGTreeKernel");

        // DebugData = new uint[KeyCount];
        // DebugBuffer.SetData(DebugData);

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "GPUSort";
        cmd.EnableKeyword( GPUBVHShader, SORT_LARGE);
        cmd.DisableKeyword(GPUBVHShader, SORT_MED);
        cmd.DisableKeyword(GPUBVHShader, SORT_SMALL);
        cmd.SetComputeIntParam(GPUBVHShader, "PrimCount", KeyCount);

        cmd.BeginSample("GPU BVH ToWorld");
        cmd.SetComputeBufferParam(GPUBVHShader, 1, "TrianglesWrite", AABBBuffer);
        cmd.SetComputeBufferParam(GPUBVHShader, 1, "Nodes", NodesBuffer);
        cmd.SetComputeBufferParam(GPUBVHShader, 1, "InputAABBBuffer", LightTriBuffer);
        cmd.SetComputeBufferParam(GPUBVHShader, 1, "Transfers", TransfBuffer);
        cmd.DispatchCompute(GPUBVHShader, 1, Mathf.CeilToInt((float)KeyCount / 1024.0f), 1, 1);
        cmd.EndSample("GPU BVH ToWorld");


        cmd.SetComputeBufferParam(GPUBVHShader, 0, "DimensionedIndices", DimensionedIndices);
        cmd.SetComputeBufferParam(GPUBVHShader, 0, "IndB", TempIndices);
        cmd.BeginSample("OneSweep X");
            OneSweep.Instance.Sort(cmd, AABBBuffer, TempIndices, 0, true, KeyCount);
        cmd.EndSample("OneSweep X");
        cmd.BeginSample("GPUSort X");
            cmd.SetComputeIntParam(GPUBVHShader, "CurRun", 0);
            cmd.DispatchCompute(GPUBVHShader, 0, Mathf.CeilToInt((float)KeyCount / 1024.0f), 1, 1);
        cmd.EndSample("GPUSort X");
        cmd.BeginSample("OneSweep Y");
            OneSweep.Instance.Sort(cmd, AABBBuffer, TempIndices, 1, true, KeyCount);
        cmd.EndSample("OneSweep Y");
        cmd.BeginSample("GPUSort Y");
            cmd.SetComputeIntParam(GPUBVHShader, "CurRun", 1);
            cmd.DispatchCompute(GPUBVHShader, 0, Mathf.CeilToInt((float)KeyCount / 1024.0f), 1, 1);
        cmd.EndSample("GPUSort Y");
        cmd.BeginSample("OneSweep Z");
            OneSweep.Instance.Sort(cmd, AABBBuffer, TempIndices, 2, true, KeyCount);
        cmd.EndSample("OneSweep Z");
        cmd.BeginSample("GPUSort Z");
            cmd.SetComputeIntParam(GPUBVHShader, "CurRun", 2);
            cmd.DispatchCompute(GPUBVHShader, 0, Mathf.CeilToInt((float)KeyCount / 1024.0f), 1, 1);
        cmd.EndSample("GPUSort Z");

        cmd.BeginSample("GPU BVH Initialize");
            cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "SplitWrite", SplitDataBuffer);
            cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "TempIndexes", TempIndices);
            cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "ReadBackWrite", ReadbackDataA);
            cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "ReadBackWrite2", ReadbackDataB);
            cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "Nodes", NodesBuffer);
            cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "Triangles", AABBBuffer);
            cmd.DispatchCompute(GPUBVHShader, AABBCopyKernel, Mathf.CeilToInt((float)KeyCount / 512.0f), 1, 1);
        cmd.EndSample("GPU BVH Initialize");

        cmd.BeginSample("GPU Parrallel Reduce");
        int SetCount = KeyCount;
        cmd.SetComputeBufferParam(GPUBVHShader, AABBReductionKernel, "Nodes", NodesBuffer);
        int Count = Mathf.CeilToInt(Mathf.Log(KeyCount, 2));
            for(int i = 0; i <= Count; i++) {
                var aa = SetCount;
                cmd.SetComputeIntParam(GPUBVHShader, "SetCount", aa);
                cmd.BeginSample("GPU Parrallel Reduce " + aa);
                    cmd.DispatchCompute(GPUBVHShader, AABBReductionKernel, Mathf.CeilToInt((float)aa / 256.0f), 1, 1);
                cmd.EndSample("GPU Parrallel Reduce " + aa);
                SetCount = Mathf.CeilToInt((float)SetCount / 2.0f);
            }
        cmd.EndSample("GPU Parrallel Reduce");

        // cmd.SetComputeBufferParam(GPUBVHShader, PostProcKernel, "Nodes", NodesBuffer);
        // cmd.SetComputeBufferParam(GPUBVHShader, PostProcKernel, "SingularNodeBuffer", SingleParentBoundBuffer);
        // cmd.DispatchCompute(GPUBVHShader, PostProcKernel, 1, 1, 1);


        



        cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "LayerStridesBuffer", LayerStridesBuffer);
        cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "LayerStridesBuffer", LayerStridesBuffer);
        // cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "DebugBuffer", DebugBuffer);
        cmd.SetComputeBufferParam(GPUBVHShader, TransferKernel, "LayerStridesBuffer", LayerStridesBuffer);

        cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "Nodes", NodesBuffer);
        cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "Nodes", NodesBuffer);

        cmd.SetComputeBufferParam(GPUBVHShader, SortKernel, "TempIndexes", TempIndices);
        cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "TempIndexes", TempIndices);

        cmd.SetComputeBufferParam(GPUBVHShader, SortKernel, "DimensionedIndices", DimensionedIndices);
        cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "DimensionedIndices", DimensionedIndices);

        cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "Triangles", AABBBuffer);
        // cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "DebugBuffer", DebugBuffer);
        
        cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "WorkGroup", WorkGroup);
        cmd.SetComputeBufferParam(GPUBVHShader, SortKernel, "WorkGroup", WorkGroup);
        cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "WorkGroup", WorkGroup);
        cmd.SetComputeBufferParam(GPUBVHShader, TransferKernel, "WorkGroup", WorkGroup);
        
        cmd.SetComputeBufferParam(GPUBVHShader, TransferKernel, "DispatchSize", DispatchBuffer);

        cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "SplitRead", SplitDataBuffer);
        cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "SplitWrite", SplitDataBuffer);

        cmd.BeginSample("GPUBVHBuild");
            for(int i = 0; i <= 25; i++) {
                int ii = i;
                bool DoFlip = i % 2 == 0;

                if(ii == 2) {
                    cmd.DisableKeyword(GPUBVHShader, SORT_LARGE);
                    cmd.EnableKeyword(GPUBVHShader, SORT_MED);
                    cmd.DisableKeyword(GPUBVHShader, SORT_SMALL);
                }
                if(ii == 8) {
                    cmd.DisableKeyword(GPUBVHShader, SORT_LARGE);
                    cmd.DisableKeyword(GPUBVHShader, SORT_MED);
                    cmd.EnableKeyword(GPUBVHShader, SORT_SMALL);
                }

                cmd.SetComputeIntParam(GPUBVHShader, "CurRun", ii);
                
                cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "ReadBackRead", DoFlip ? ReadbackDataA : ReadbackDataB);
                cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "ReadBackRead", DoFlip ? ReadbackDataA : ReadbackDataB);
                cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "ReadBackWrite", !DoFlip ? ReadbackDataA : ReadbackDataB);
                cmd.SetComputeBufferParam(GPUBVHShader, SortKernel, "ReadBackWrite", !DoFlip ? ReadbackDataA : ReadbackDataB);


                cmd.BeginSample("Split Layer " + i);
                cmd.DispatchCompute(GPUBVHShader, SplitKernel, DispatchBuffer, 32);
                cmd.EndSample("Split Layer " + i);

                cmd.BeginSample("Propogate Layer " + i);
                cmd.DispatchCompute(GPUBVHShader, PropogateKernel, DispatchBuffer, 0);
                cmd.EndSample("Propogate Layer " + i);

                cmd.BeginSample("Transfer Layer " + i);
                cmd.DispatchCompute(GPUBVHShader, TransferKernel, 1, 1, 1);
                cmd.EndSample("Transfer Layer " + i);

                cmd.BeginSample("Sort Layer " + i);
                cmd.DispatchCompute(GPUBVHShader, SortKernel, DispatchBuffer, 16);
                cmd.EndSample("Sort Layer " + i);

            }
#if !DontUseSGTree
            cmd.SetComputeBufferParam(GPUBVHShader, SGTreeKernel, "LayerStridesBuffer", LayerStridesBuffer);
            cmd.SetComputeBufferParam(GPUBVHShader, SGTreeKernel, "Transfers", TransfBuffer);
            cmd.SetComputeBufferParam(GPUBVHShader, SGTreeKernel, "NodesRead", NodesBuffer);
            cmd.SetComputeBufferParam(GPUBVHShader, SGTreeKernel, "SGNodesBuffer", SGTreeBuffer);
	        cmd.SetComputeBufferParam(GPUBVHShader, SGTreeKernel, "InputAABBBuffer", LightTriBuffer);
            cmd.SetComputeBufferParam(GPUBVHShader, SGTreeKernel+1, "LayerStridesBuffer", LayerStridesBuffer);
            cmd.SetComputeBufferParam(GPUBVHShader, SGTreeKernel+1, "DispatchSize", DispatchBuffer);
            for(int i = 25; i >= 0; i--) {
                var ii = i;
                cmd.SetComputeIntParam(GPUBVHShader, "CurRun", ii);
    
                cmd.BeginSample("Transfer Layer SG " + i);
                    cmd.DispatchCompute(GPUBVHShader, SGTreeKernel+1, 1, 1, 1);
                cmd.EndSample("Transfer Layer SG " + i);

                cmd.BeginSample("SG Layer " + i);
                    cmd.DispatchCompute(GPUBVHShader, SGTreeKernel, DispatchBuffer, 0);
                cmd.EndSample("SG Layer " + i);
            }
#endif
        cmd.EndSample("GPUBVHBuild");
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        cmd.Release();

        // DispatchBuffer.GetData(TempDispatchInit);
        // string TempString = "";
        // for(int i = 0; i < 16; i++) {
        // 	TempString += TempDispatchInit[i] + ", ";
        // }
        // Debug.Log(TempString);

        // int[] TempRefDat = new int[100 * 4];
        // LayerStridesBuffer.GetData(TempRefDat);
        // for(int i = 0; i < 20; i++) {
        //     string CurString = "" + i + ": ";
        //     for(int j = 0; j < 4; j++) {
        //         CurString += TempRefDat[i * 4 + j] + ", ";
        //     }
        //     Debug.Log(CurString);
        // }
        // DebugBuffer.GetData(DebugData);
        // Debug.LogError(KeyCount);
        // for(int i = 0; i < KeyCount; i++) {
        //     if(DebugData[i] != 1) Debug.LogError(i + ": " + DebugData[i]); 
        // }
        // ParentBoundArray = new CompactLightBVHData[1];
        // SingleParentBoundBuffer.GetData(ParentBoundArray);
        // SingleParentBoundBuffer.Release();
    }







#if !DontUseSGTree
    public unsafe void RunLocal(ComputeBuffer InputAABBBuffer, ComputeBuffer SGTreeBuffer, ComputeBuffer TriBuffer, ComputeBuffer TransfBuffer, int KeyCount) {
#else
    public unsafe void RunLocal(ComputeBuffer InputAABBBuffer, ComputeBuffer NodesBuffer, ComputeBuffer TriBuffer, int KeyCount) {
#endif
        if (GPUBVHShader == null) {GPUBVHShader = Resources.Load<ComputeShader>("Builders/GPUBVHBuilders/GPULightBVH/GPULightBVHBuilder"); }

        uint[] TempDispatchInit = new uint[4 * 4];
        System.Array.Fill(TempDispatchInit, (uint)1);
        DispatchBuffer.SetData(TempDispatchInit);

        int[] WorkGroupInit = new int[4];
        WorkGroupInit[3] = 2;
        WorkGroup.SetData(WorkGroupInit);


        Vector4[] RefitInit = new Vector4[50];
        System.Array.Fill(RefitInit, new Vector4((int)Mathf.Pow(2, 30), -(int)Mathf.Pow(2, 30), 0, 0));
        LayerStridesBuffer.SetData(RefitInit);

            // int[] DimensionedIndicesArray = new int[KeyCount * 3];
            // float[] CentersX = new float[KeyCount];
            // float[] CentersY = new float[KeyCount];
            // float[] CentersZ = new float[KeyCount];
            // int[] FinalIndices = new int[KeyCount];
            // for(int i = 0; i < KeyCount; i++) {
            //     FinalIndices[i] = i;
            //     CentersX[i] = (Triangles[i].BBMax.x - Triangles[i].BBMin.x) / 2.0f + Triangles[i].BBMin.x;
            //     CentersY[i] = (Triangles[i].BBMax.y - Triangles[i].BBMin.y) / 2.0f + Triangles[i].BBMin.y;
            //     CentersZ[i] = (Triangles[i].BBMax.z - Triangles[i].BBMin.z) / 2.0f + Triangles[i].BBMin.z;
            // }

            // Debug.Log("LIGHT COUNT: " + KeyCount);
            // System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersX[s1] - CentersX[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            // System.Array.Copy(FinalIndices, 0, DimensionedIndicesArray, 0, KeyCount);
            // for(int i = 0; i < KeyCount; i++) FinalIndices[i] = i;
            // System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersY[s1] - CentersY[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            // System.Array.Copy(FinalIndices, 0, DimensionedIndicesArray, KeyCount, KeyCount);



            // for(int i = 0; i < KeyCount; i++) FinalIndices[i] = i;
            // System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersZ[s1] - CentersZ[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            // System.Array.Copy(FinalIndices, 0, DimensionedIndicesArray, KeyCount * 2, KeyCount);

            // DimensionedIndices.SetData(DimensionedIndicesArray);

        int PropogateKernel = GPUBVHShader.FindKernel("Propogate");
        int SortKernel = GPUBVHShader.FindKernel("Sort");
        int SplitKernel = GPUBVHShader.FindKernel("SplitKernel");
        int TransferKernel = GPUBVHShader.FindKernel("Transfer");
        int AABBCopyKernel = GPUBVHShader.FindKernel("InitializeKernel");
        int AABBReductionKernel = GPUBVHShader.FindKernel("NodeBufferInitialization");
        int PostProcKernel = GPUBVHShader.FindKernel("PostProcKernel");
        int SGTreeKernel = GPUBVHShader.FindKernel("SGTreeKernel");


        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "GPUSort";
        cmd.EnableKeyword( GPUBVHShader, SORT_LARGE);
        cmd.DisableKeyword(GPUBVHShader, SORT_MED);
        cmd.DisableKeyword(GPUBVHShader, SORT_SMALL);
        cmd.SetComputeIntParam(GPUBVHShader, "PrimCount", KeyCount);

        cmd.BeginSample("GPU BVH ToWorld");
        cmd.SetComputeBufferParam(GPUBVHShader, 2, "TrianglesWrite", AABBBuffer);
        cmd.SetComputeBufferParam(GPUBVHShader, 2, "Nodes", NodesBuffer);
        cmd.SetComputeBufferParam(GPUBVHShader, 2, "InputAABBBuffer", InputAABBBuffer);
        cmd.DispatchCompute(GPUBVHShader, 2, Mathf.CeilToInt((float)KeyCount / 1024.0f), 1, 1);
        cmd.EndSample("GPU BVH ToWorld");


        cmd.SetComputeBufferParam(GPUBVHShader, 0, "DimensionedIndices", DimensionedIndices);
        cmd.SetComputeBufferParam(GPUBVHShader, 0, "IndB", TempIndices);
        cmd.BeginSample("OneSweep X");
            OneSweep.Instance.Sort(cmd, AABBBuffer, TempIndices, 0, true, KeyCount);
        cmd.EndSample("OneSweep X");
        cmd.BeginSample("GPUSort X");
            cmd.SetComputeIntParam(GPUBVHShader, "CurRun", 0);
            cmd.DispatchCompute(GPUBVHShader, 0, Mathf.CeilToInt((float)KeyCount / 1024.0f), 1, 1);
        cmd.EndSample("GPUSort X");
        cmd.BeginSample("OneSweep Y");
            OneSweep.Instance.Sort(cmd, AABBBuffer, TempIndices, 1, true, KeyCount);
        cmd.EndSample("OneSweep Y");
        cmd.BeginSample("GPUSort Y");
            cmd.SetComputeIntParam(GPUBVHShader, "CurRun", 1);
            cmd.DispatchCompute(GPUBVHShader, 0, Mathf.CeilToInt((float)KeyCount / 1024.0f), 1, 1);
        cmd.EndSample("GPUSort Y");
        cmd.BeginSample("OneSweep Z");
            OneSweep.Instance.Sort(cmd, AABBBuffer, TempIndices, 2, true, KeyCount);
        cmd.EndSample("OneSweep Z");
        cmd.BeginSample("GPUSort Z");
            cmd.SetComputeIntParam(GPUBVHShader, "CurRun", 2);
            cmd.DispatchCompute(GPUBVHShader, 0, Mathf.CeilToInt((float)KeyCount / 1024.0f), 1, 1);
        cmd.EndSample("GPUSort Z");

        cmd.BeginSample("GPU BVH Initialize");
            cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "SplitWrite", SplitDataBuffer);
            cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "TempIndexes", TempIndices);
            cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "ReadBackWrite", ReadbackDataA);
            cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "ReadBackWrite2", ReadbackDataB);
            cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "Nodes", NodesBuffer);
            cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "Triangles", AABBBuffer);
            cmd.DispatchCompute(GPUBVHShader, AABBCopyKernel, Mathf.CeilToInt((float)KeyCount / 512.0f), 1, 1);
        cmd.EndSample("GPU BVH Initialize");

        cmd.BeginSample("GPU Parrallel Reduce");
        int SetCount = KeyCount;
        cmd.SetComputeBufferParam(GPUBVHShader, AABBReductionKernel, "Nodes", NodesBuffer);
        int Count = Mathf.CeilToInt(Mathf.Log(KeyCount, 2));
            for(int i = 0; i <= Count; i++) {
                var aa = SetCount;
                cmd.SetComputeIntParam(GPUBVHShader, "SetCount", aa);
                cmd.BeginSample("GPU Parrallel Reduce " + aa);
                    cmd.DispatchCompute(GPUBVHShader, AABBReductionKernel, Mathf.CeilToInt((float)aa / 256.0f), 1, 1);
                cmd.EndSample("GPU Parrallel Reduce " + aa);
                SetCount = Mathf.CeilToInt((float)SetCount / 2.0f);
            }
        cmd.EndSample("GPU Parrallel Reduce");




        



        cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "LayerStridesBuffer", LayerStridesBuffer);
        cmd.SetComputeBufferParam(GPUBVHShader, TransferKernel, "LayerStridesBuffer", LayerStridesBuffer);

        cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "Nodes", NodesBuffer);
        cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "Nodes", NodesBuffer);

        cmd.SetComputeBufferParam(GPUBVHShader, SortKernel, "TempIndexes", TempIndices);
        cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "TempIndexes", TempIndices);

        cmd.SetComputeBufferParam(GPUBVHShader, SortKernel, "DimensionedIndices", DimensionedIndices);
        cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "DimensionedIndices", DimensionedIndices);

        cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "Triangles", AABBBuffer);
        cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "DebugBuffer", DebugBuffer);
        
        cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "WorkGroup", WorkGroup);
        cmd.SetComputeBufferParam(GPUBVHShader, SortKernel, "WorkGroup", WorkGroup);
        cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "WorkGroup", WorkGroup);
        cmd.SetComputeBufferParam(GPUBVHShader, TransferKernel, "WorkGroup", WorkGroup);
        
        cmd.SetComputeBufferParam(GPUBVHShader, TransferKernel, "DispatchSize", DispatchBuffer);

        cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "SplitRead", SplitDataBuffer);
        cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "SplitWrite", SplitDataBuffer);

        cmd.BeginSample("GPUBVHBuild");
            for(int i = 0; i <= 50; i++) {
                int ii = i;
                bool DoFlip = i % 2 == 0;

                if(ii == 2) {
                    cmd.DisableKeyword(GPUBVHShader, SORT_LARGE);
                    cmd.EnableKeyword(GPUBVHShader, SORT_MED);
                    cmd.DisableKeyword(GPUBVHShader, SORT_SMALL);
                }
                if(ii == 8) {
                    cmd.DisableKeyword(GPUBVHShader, SORT_LARGE);
                    cmd.DisableKeyword(GPUBVHShader, SORT_MED);
                    cmd.EnableKeyword(GPUBVHShader, SORT_SMALL);
                }

                cmd.SetComputeIntParam(GPUBVHShader, "CurRun", ii);
                
                cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "ReadBackRead", DoFlip ? ReadbackDataA : ReadbackDataB);
                cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "ReadBackRead", DoFlip ? ReadbackDataA : ReadbackDataB);
                cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "ReadBackWrite", !DoFlip ? ReadbackDataA : ReadbackDataB);
                cmd.SetComputeBufferParam(GPUBVHShader, SortKernel, "ReadBackWrite", !DoFlip ? ReadbackDataA : ReadbackDataB);


                cmd.BeginSample("Split Layer " + i);
                cmd.DispatchCompute(GPUBVHShader, SplitKernel, DispatchBuffer, 32);
                cmd.EndSample("Split Layer " + i);

                cmd.BeginSample("Propogate Layer " + i);
                cmd.DispatchCompute(GPUBVHShader, PropogateKernel, DispatchBuffer, 0);
                cmd.EndSample("Propogate Layer " + i);

                cmd.BeginSample("Transfer Layer " + i);
                cmd.DispatchCompute(GPUBVHShader, TransferKernel, 1, 1, 1);
                cmd.EndSample("Transfer Layer " + i);

                cmd.BeginSample("Sort Layer " + i);
                cmd.DispatchCompute(GPUBVHShader, SortKernel, DispatchBuffer, 16);
                cmd.EndSample("Sort Layer " + i);

            }
        cmd.SetComputeBufferParam(GPUBVHShader, PostProcKernel, "Nodes", NodesBuffer);
        cmd.SetComputeBufferParam(GPUBVHShader, PostProcKernel, "SingularNodeBuffer", SingleParentBoundBuffer);
        cmd.DispatchCompute(GPUBVHShader, PostProcKernel, 1, 1, 1);
#if !DontUseSGTree
            cmd.SetComputeBufferParam(GPUBVHShader, SGTreeKernel, "LayerStridesBuffer", LayerStridesBuffer);
            cmd.SetComputeBufferParam(GPUBVHShader, SGTreeKernel, "Transfers", TransfBuffer);
            cmd.SetComputeBufferParam(GPUBVHShader, SGTreeKernel, "NodesRead", NodesBuffer);
            cmd.SetComputeBufferParam(GPUBVHShader, SGTreeKernel, "SGNodesBuffer", SGTreeBuffer);
            cmd.SetComputeBufferParam(GPUBVHShader, SGTreeKernel+1, "LayerStridesBuffer", LayerStridesBuffer);
            cmd.SetComputeBufferParam(GPUBVHShader, SGTreeKernel+1, "DispatchSize", DispatchBuffer);
            for(int i = 50; i >= 0; i--) {
                var ii = i;
                cmd.SetComputeIntParam(GPUBVHShader, "CurRun", ii);
    
                cmd.BeginSample("Transfer Layer SG " + i);
                    cmd.DispatchCompute(GPUBVHShader, SGTreeKernel+1, 1, 1, 1);
                cmd.EndSample("Transfer Layer SG " + i);

                cmd.BeginSample("SG Layer " + i);
                    cmd.DispatchCompute(GPUBVHShader, SGTreeKernel, DispatchBuffer, 0);
                cmd.EndSample("SG Layer " + i);
            }
#endif
        cmd.EndSample("GPUBVHBuild");
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        cmd.Release();
        // int[] TempRefDat = new int[50 * 4];
        // LayerStridesBuffer.GetData(TempRefDat);
        // for(int i = 0; i < 50; i++) {
        //     string CurString = "" + i + ": ";
        //     for(int j = 0; j < 4; j++) {
        //         CurString += TempRefDat[i * 4 + j] + ", ";
        //     }
        //     Debug.Log(CurString);
        // }
        // DebugBuffer.GetData(DebugData);
        // Debug.LogError(KeyCount);
        // for(int i = 0; i < KeyCount; i++) {
        //     if(DebugData[i] != 1) Debug.LogError(i + ": " + DebugData[i]); 
        // }
        ParentBoundArray = new CompactLightBVHData[1];
        SingleParentBoundBuffer.GetData(ParentBoundArray);
        SingleParentBoundBuffer.Release();
    }


    public void Dispose() {
        SplitDataBuffer?.Release();
        ReadbackDataA?.Release();
        ReadbackDataB?.Release();
        WorkGroup?.Release();
        TempIndices?.Release();
        DispatchBuffer?.Release();
        LayerStridesBuffer?.Release();
#if !DontUseSGTree
        NodesBuffer?.Release();
#endif
        AABBBuffer?.Release();
    }
}
