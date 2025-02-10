// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Rendering;
// using CommonVars;
    
// public class GPUBVHBuilder
// {
//     public ComputeShader GPUBVHShader;
//     public ComputeBuffer NodesBuffer;

//     ComputeBuffer SplitDataBuffer;
//     ComputeBuffer ReadbackDataA;
//     ComputeBuffer ReadbackDataB;
//     ComputeBuffer WorkGroup;
//     ComputeBuffer TempIndices;
//     ComputeBuffer DispatchBuffer;
//     ComputeBuffer LayerStridesBuffer;

//     protected LocalKeyword SORT_LARGE;
//     protected LocalKeyword SORT_MED;
//     protected LocalKeyword SORT_SMALL;

//     public uint[] DebugData;
//     int PrimCount;
//     public GPUBVHBuilder(int TriCount) {
//         PrimCount = TriCount;
//         if (GPUBVHShader == null) {GPUBVHShader = Resources.Load<ComputeShader>("Builders/GPUBVHBuilders/GPUBVH/GPUBVHBuilder"); }
        
//         CommonFunctions.CreateDynamicBuffer(ref NodesBuffer, TriCount * 2, 32);
//         CommonFunctions.CreateDynamicBuffer(ref SplitDataBuffer, (PrimCount * 2 + 1), 12);
//         CommonFunctions.CreateDynamicBuffer(ref ReadbackDataA, (PrimCount * 2 + 1), 16);
//         CommonFunctions.CreateDynamicBuffer(ref ReadbackDataB, (PrimCount * 2 + 1), 16);
//         CommonFunctions.CreateDynamicBuffer(ref WorkGroup, 4, 4);
//         CommonFunctions.CreateDynamicBuffer(ref TempIndices, TriCount * 3, 4);
//         CommonFunctions.CreateDynamicBuffer(ref DispatchBuffer, 16, 4);
//         CommonFunctions.CreateDynamicBuffer(ref LayerStridesBuffer, 100, 16);
//         SORT_LARGE = new LocalKeyword(GPUBVHShader, "SORT_LARGE");
//         SORT_MED = new LocalKeyword(GPUBVHShader, "SORT_MED");
//         SORT_SMALL = new LocalKeyword(GPUBVHShader, "SORT_SMALL");


//     }

//     public unsafe void Run(AABB* Triangles,  ComputeBuffer AABBBuffer, ComputeBuffer DimensionedIndices, int KeyCount) {
//         if (GPUBVHShader == null) {GPUBVHShader = Resources.Load<ComputeShader>("Builders/GPUBVHBuilders/GPUBVH/GPUBVHBuilder"); }


//         uint[] TempDispatchInit = new uint[4 * 4];
//         System.Array.Fill(TempDispatchInit, (uint)1);
//         DispatchBuffer.SetData(TempDispatchInit);

//         int[] WorkGroupInit = new int[4];
//         WorkGroupInit[3] = 2;
//         WorkGroup.SetData(WorkGroupInit);


//         Vector4[] RefitInit = new Vector4[100];
//         System.Array.Fill(RefitInit, new Vector4((int)Mathf.Pow(2, 30), -(int)Mathf.Pow(2, 30), 0, 0));
//         LayerStridesBuffer.SetData(RefitInit);

//             // int[] DimensionedIndicesArray = new int[PrimCount * 3];
//             // float[] CentersX = new float[PrimCount];
//             // float[] CentersY = new float[PrimCount];
//             // float[] CentersZ = new float[PrimCount];
//             // int[] FinalIndices = new int[PrimCount];
//             // for(int i = 0; i < PrimCount; i++) {
//             //     FinalIndices[i] = i;
//             //     CentersX[i] = (Triangles[i].BBMax.x - Triangles[i].BBMin.x) / 2.0f + Triangles[i].BBMin.x;
//             //     CentersY[i] = (Triangles[i].BBMax.y - Triangles[i].BBMin.y) / 2.0f + Triangles[i].BBMin.y;
//             //     CentersZ[i] = (Triangles[i].BBMax.z - Triangles[i].BBMin.z) / 2.0f + Triangles[i].BBMin.z;
//             // }

//             // Debug.Log("LIGHT COUNT: " + PrimCount);
//             // System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersX[s1] - CentersX[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
//             // System.Array.Copy(FinalIndices, 0, DimensionedIndicesArray, 0, PrimCount);
//             // for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
//             // System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersY[s1] - CentersY[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
//             // System.Array.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount, PrimCount);



//             // for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
//             // System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersZ[s1] - CentersZ[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
//             // System.Array.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount * 2, PrimCount);

//             // DimensionedIndices.SetData(DimensionedIndicesArray);

//         int PropogateKernel = GPUBVHShader.FindKernel("Propogate");
//         int SortKernel = GPUBVHShader.FindKernel("Sort");
//         int SplitKernel = GPUBVHShader.FindKernel("SplitKernel");
//         int TransferKernel = GPUBVHShader.FindKernel("Transfer");
//         int AABBCopyKernel = GPUBVHShader.FindKernel("InitializeKernel");
//         int AABBReductionKernel = GPUBVHShader.FindKernel("NodeBufferInitialization");



//         CommandBuffer cmd = new CommandBuffer();
//         cmd.name = "GPUSort";
//         cmd.EnableKeyword( GPUBVHShader, SORT_LARGE);
//         cmd.DisableKeyword(GPUBVHShader, SORT_MED);
//         cmd.DisableKeyword(GPUBVHShader, SORT_SMALL);

//         cmd.SetComputeIntParam(GPUBVHShader, "PrimCount", PrimCount);
//         cmd.SetComputeBufferParam(GPUBVHShader, 0, "DimensionedIndices", DimensionedIndices);
//         cmd.SetComputeBufferParam(GPUBVHShader, 0, "IndB", TempIndices);
//         cmd.BeginSample("GPUSort X");
//             OneSweep.Instance.Sort(cmd, AABBBuffer, TempIndices, 0, false, KeyCount);
//             cmd.SetComputeIntParam(GPUBVHShader, "CurRun", 0);
//             cmd.DispatchCompute(GPUBVHShader, 0, Mathf.CeilToInt((float)PrimCount / 1024.0f), 1, 1);
//         cmd.EndSample("GPUSort X");
//         cmd.BeginSample("GPUSort Y");
//             OneSweep.Instance.Sort(cmd, AABBBuffer, TempIndices, 1, false, KeyCount);
//             cmd.SetComputeIntParam(GPUBVHShader, "CurRun", 1);
//             cmd.DispatchCompute(GPUBVHShader, 0, Mathf.CeilToInt((float)PrimCount / 1024.0f), 1, 1);
//         cmd.EndSample("GPUSort Y");
//         cmd.BeginSample("GPUSort Z");
//             OneSweep.Instance.Sort(cmd, AABBBuffer, TempIndices, 2, false, KeyCount);
//             cmd.SetComputeIntParam(GPUBVHShader, "CurRun", 2);
//             cmd.DispatchCompute(GPUBVHShader, 0, Mathf.CeilToInt((float)PrimCount / 1024.0f), 1, 1);
//         cmd.EndSample("GPUSort Z");

//         cmd.BeginSample("GPU BVH Initialize");
//             cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "SplitWrite", SplitDataBuffer);
//             cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "TempIndexes", TempIndices);
//             cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "ReadBackWrite", ReadbackDataA);
//             cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "ReadBackWrite2", ReadbackDataB);
//             cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "Nodes", NodesBuffer);
//             cmd.SetComputeBufferParam(GPUBVHShader, AABBCopyKernel, "Triangles", AABBBuffer);
//             cmd.DispatchCompute(GPUBVHShader, AABBCopyKernel, Mathf.CeilToInt((float)PrimCount / 512.0f), 1, 1);
//         cmd.EndSample("GPU BVH Initialize");

//         cmd.BeginSample("GPU Parrallel Reduce");
//         int SetCount = PrimCount;
//         cmd.SetComputeBufferParam(GPUBVHShader, AABBReductionKernel, "Nodes", NodesBuffer);
//         int Coun = Mathf.CeilToInt(Mathf.Log(PrimCount, 2));
//             for(int i = 0; i <= Coun; i++) {
//                 var aa = SetCount;
//                 cmd.SetComputeIntParam(GPUBVHShader, "SetCount", aa);
//                 cmd.BeginSample("GPU Parrallel Reduce " + aa);
//                     cmd.DispatchCompute(GPUBVHShader, AABBReductionKernel, Mathf.CeilToInt((float)aa / 512.0f), 1, 1);
//                 cmd.EndSample("GPU Parrallel Reduce " + aa);
//                 SetCount = Mathf.CeilToInt((float)SetCount / 2.0f);
//             }
//         cmd.EndSample("GPU Parrallel Reduce");



        



//         cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "LayerStridesBuffer", LayerStridesBuffer);
//         cmd.SetComputeBufferParam(GPUBVHShader, TransferKernel, "LayerStridesBuffer", LayerStridesBuffer);

//         cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "Nodes", NodesBuffer);
//         cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "Nodes", NodesBuffer);

//         cmd.SetComputeBufferParam(GPUBVHShader, SortKernel, "TempIndexes", TempIndices);
//         cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "TempIndexes", TempIndices);

//         cmd.SetComputeBufferParam(GPUBVHShader, SortKernel, "DimensionedIndices", DimensionedIndices);
//         cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "DimensionedIndices", DimensionedIndices);

//         cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "Triangles", AABBBuffer);
        
//         cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "WorkGroup", WorkGroup);
//         cmd.SetComputeBufferParam(GPUBVHShader, SortKernel, "WorkGroup", WorkGroup);
//         cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "WorkGroup", WorkGroup);
//         cmd.SetComputeBufferParam(GPUBVHShader, TransferKernel, "WorkGroup", WorkGroup);
        
//         cmd.SetComputeBufferParam(GPUBVHShader, TransferKernel, "DispatchSize", DispatchBuffer);

//         cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "SplitRead", SplitDataBuffer);
//         cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "SplitWrite", SplitDataBuffer);

//         cmd.BeginSample("GPUBVHBuild");
//             for(int i = 0; i < 100; i++) {
//                 int ii = i;
//                 bool DoFlip = i % 2 == 0;

//                 if(ii == 2) {
//                     cmd.DisableKeyword(GPUBVHShader, SORT_LARGE);
//                     cmd.EnableKeyword(GPUBVHShader, SORT_MED);
//                     cmd.DisableKeyword(GPUBVHShader, SORT_SMALL);
//                 }
//                 if(ii == 8) {
//                     cmd.DisableKeyword(GPUBVHShader, SORT_LARGE);
//                     cmd.DisableKeyword(GPUBVHShader, SORT_MED);
//                     cmd.EnableKeyword(GPUBVHShader, SORT_SMALL);
//                 }


//                 cmd.SetComputeIntParam(GPUBVHShader, "CurRun", ii);
                
//                 cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "ReadBackRead", DoFlip ? ReadbackDataA : ReadbackDataB);
//                 cmd.SetComputeBufferParam(GPUBVHShader, SplitKernel, "ReadBackRead", DoFlip ? ReadbackDataA : ReadbackDataB);
//                 cmd.SetComputeBufferParam(GPUBVHShader, PropogateKernel, "ReadBackWrite", !DoFlip ? ReadbackDataA : ReadbackDataB);
//                 cmd.SetComputeBufferParam(GPUBVHShader, SortKernel, "ReadBackWrite", !DoFlip ? ReadbackDataA : ReadbackDataB);


//                 cmd.BeginSample("Split Layer " + i);
//                 cmd.DispatchCompute(GPUBVHShader, SplitKernel, DispatchBuffer, 32);
//                 cmd.EndSample("Split Layer " + i);

//                 cmd.BeginSample("Propogate Layer " + i);
//                 cmd.DispatchCompute(GPUBVHShader, PropogateKernel, DispatchBuffer, 0);
//                 cmd.EndSample("Propogate Layer " + i);

//                 cmd.BeginSample("Transfer Layer " + i);
//                 cmd.DispatchCompute(GPUBVHShader, TransferKernel, 1, 1, 1);
//                 cmd.EndSample("Transfer Layer " + i);

//                 cmd.BeginSample("Sort Layer " + i);
//                 cmd.DispatchCompute(GPUBVHShader, SortKernel, DispatchBuffer, 16);
//                 cmd.EndSample("Sort Layer " + i);

//             }
//         cmd.EndSample("GPUBVHBuild");
//         Graphics.ExecuteCommandBuffer(cmd);
//         cmd.Clear();
//         cmd.Release();
//     }

//     public void Dispose() {
//         SplitDataBuffer?.Release();
//         ReadbackDataA?.Release();
//         ReadbackDataB?.Release();
//         WorkGroup?.Release();
//         TempIndices?.Release();
//         DispatchBuffer?.Release();
//         LayerStridesBuffer?.Release();
//         NodesBuffer?.Release();
//     }
// }
