using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System; 
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace TrueTrace {
    public unsafe class LBVHGPU : MonoBehaviour
    {

        public NativeArray<float> CentersX;
        public NativeArray<float> CentersY;
        public NativeArray<float> CentersZ;
        public NativeArray<int> DimensionedIndicesArray;

        private int* DimensionedIndices;


        public LBVHGPU(LightBounds[] Tris) {//need to make sure incomming is transformed to world space already
            int PrimCount = Tris.Length;
            
            CentersX = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            CentersY = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            CentersZ = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            DimensionedIndicesArray = new NativeArray<int>(PrimCount * 3, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            DimensionedIndices = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(DimensionedIndicesArray);


            float* ptr = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(CentersX);
            float* ptr1 = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(CentersY);
            float* ptr2 = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(CentersZ);


            int[] FinalIndices = new int[PrimCount];

            for(int i = 0; i < PrimCount; i++) {
                FinalIndices[i] = i;
                ptr[i] = (Tris[i].b.BBMax.x - Tris[i].b.BBMin.x) / 2.0f + Tris[i].b.BBMin.x;
                ptr1[i] = (Tris[i].b.BBMax.y - Tris[i].b.BBMin.y) / 2.0f + Tris[i].b.BBMin.y;
                ptr2[i] = (Tris[i].b.BBMax.z - Tris[i].b.BBMin.z) / 2.0f + Tris[i].b.BBMin.z;
                // Union(ref nodes2[0].aabb, Tris[i]);
            }

            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = ptr[s1] - ptr[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            CentersX.Dispose();
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, 0, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = ptr1[s1] - ptr1[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            CentersY.Dispose();
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = ptr2[s1] - ptr2[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            CentersZ.Dispose();
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount * 2, PrimCount);
            CommonFunctions.DeepClean(ref FinalIndices);

            ComputeShader LGPUShader = Resources.Load<ComputeShader>("Builders/LightBVHBuilder");

        }

    }
}