using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System; 
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
namespace TrueTrace {
    // [System.Serializable]
    unsafe public class BVH2Builder {

        public BVHNode2Data* BVH2Nodes;
        public int* DimensionedIndices;
        public int* temp;
        public bool* indices_going_left;
        public int PrimCount;
        public int[] FinalIndices;
        private ObjectSplit split = new ObjectSplit();
        public float* SAH;
        public AABB* Primitives;
        
        // public BVHNodeVerbose[] VerboseNodes;


        public NativeArray<BVHNode2Data> BVH2NodesArray;
        public NativeArray<int> DimensionedIndicesArray;
        public NativeArray<int> tempArray;
        public NativeArray<bool> indices_going_left_array;
        public NativeArray<float> SAHArray;

        public void Dispose() {
            if(BVH2NodesArray.IsCreated) BVH2NodesArray.Dispose();
            if(DimensionedIndicesArray.IsCreated) DimensionedIndicesArray.Dispose();
            if(tempArray.IsCreated) tempArray.Dispose();
            if(indices_going_left_array.IsCreated) indices_going_left_array.Dispose();
            if(SAHArray.IsCreated) SAHArray.Dispose();
            
        }

        public struct ObjectSplit {
            public int index;
            public float cost;
            public int dimension;
            public AABB aabb_left;
            public AABB aabb_right;
        }

        AABB aabb_right;
        void partition_sah(int first_index, int index_count) {
            split.cost = float.MaxValue;
            split.index = -1;
            split.dimension = -1;
            split.aabb_left.init();
            split.aabb_right.init();

            int Offset;
            for(int dimension = 0; dimension < 3; dimension++) {
                aabb_right.init();
                Offset = PrimCount * dimension + first_index;
                for(int i = 1; i < index_count; i++) {
                    aabb_right.Extend(ref Primitives[DimensionedIndices[Offset + i - 1]]);

                    SAH[i] = surface_area(ref aabb_right) * (float)i;
                }
                aabb_right.init();

                for(int i = index_count - 1; i > 0; i--) {
                    aabb_right.Extend(ref Primitives[DimensionedIndices[Offset + i]]);

                    float cost = SAH[i] + surface_area(ref aabb_right) * (float)(index_count - i);

                    if(cost <= split.cost) {
                        split.cost = cost;
                        split.index = first_index + i;
                        split.dimension = dimension;
                        split.aabb_right = aabb_right;
                    }
                }
            }
            Offset = split.dimension * PrimCount;
            for(int i = first_index; i < split.index; i++) split.aabb_left.Extend(ref Primitives[DimensionedIndices[Offset + i]]);
        }
        void BuildRecursive(int nodesi, ref int node_index, int first_index, int index_count) {
            if(index_count == 1) {
                BVH2Nodes[nodesi].left = first_index;
                BVH2Nodes[nodesi].count = (uint)index_count;
                return;
            }
            
            partition_sah(first_index, index_count);
            int Offset = split.dimension * PrimCount;
            int IndexEnd = first_index + index_count;
            for(int i = first_index; i < IndexEnd; i++) indices_going_left[DimensionedIndices[Offset + i]] = i < split.index;

            for(int dim = 0; dim < 3; dim++) {
                if(dim == split.dimension) continue;

                int index;
                int left = 0;
                int right = split.index - first_index;
                Offset = dim * PrimCount;
                for(int i = first_index; i < IndexEnd; i++) {
                    index = DimensionedIndices[Offset + i];
                    temp[indices_going_left[index] ? (left++) : (right++)] = index;
                }
          
                NativeArray<int>.Copy(tempArray, 0, DimensionedIndicesArray, Offset + first_index, index_count);
            }
            BVH2Nodes[nodesi].left = node_index;

            BVH2Nodes[BVH2Nodes[nodesi].left].aabb = split.aabb_left;
            BVH2Nodes[BVH2Nodes[nodesi].left + 1].aabb = split.aabb_right;
            node_index += 2;
            int Index = split.index;
            BuildRecursive(BVH2Nodes[nodesi].left, ref node_index, first_index, Index - first_index);
            BuildRecursive(BVH2Nodes[nodesi].left + 1, ref node_index, Index, first_index + index_count - Index);
        }

        float surface_area(ref AABB aabb) {
            Vector3 d = new Vector3(aabb.BBMax.x - aabb.BBMin.x, aabb.BBMax.y - aabb.BBMin.y, aabb.BBMax.z - aabb.BBMin.z);
            return (d.x + d.y) * d.z + d.x * d.y; 
        }


        public unsafe BVH2Builder(AABB* Triangles, int PrimCount) {//Bottom Level Acceleration Structure Builder
            this.PrimCount = PrimCount;
            Primitives = Triangles;
            FinalIndices = new int[PrimCount];
            DimensionedIndicesArray = new NativeArray<int>(PrimCount * 3, Unity.Collections.Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            BVH2NodesArray = new NativeArray<BVHNode2Data>(PrimCount * 2, Unity.Collections.Allocator.Persistent, NativeArrayOptions.ClearMemory);
            BVH2Nodes = (BVHNode2Data*)NativeArrayUnsafeUtility.GetUnsafePtr(BVH2NodesArray);
            DimensionedIndices = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(DimensionedIndicesArray);
            SAHArray = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            SAH = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(SAHArray);
            BVH2Nodes[0].aabb.init();
            for(int i = 0; i < PrimCount; i++) {
                FinalIndices[i] = i;
                SAH[i] = (Triangles[i].BBMax.x - Triangles[i].BBMin.x) / 2.0f + Triangles[i].BBMin.x;
                BVH2Nodes[0].aabb.Extend(ref Triangles[i]);
            }
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, 0, PrimCount);
            for(int i = 0; i < PrimCount; i++) {FinalIndices[i] = i; SAH[i] = (Triangles[i].BBMax.y - Triangles[i].BBMin.y) / 2.0f + Triangles[i].BBMin.y;}
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount, PrimCount);
            for(int i = 0; i < PrimCount; i++) {FinalIndices[i] = i; SAH[i] = (Triangles[i].BBMax.z - Triangles[i].BBMin.z) / 2.0f + Triangles[i].BBMin.z;}
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount * 2, PrimCount);


            indices_going_left_array = new NativeArray<bool>(PrimCount, Unity.Collections.Allocator.Persistent, NativeArrayOptions.ClearMemory);
            indices_going_left = (bool*)NativeArrayUnsafeUtility.GetUnsafePtr(indices_going_left_array);
            tempArray = new NativeArray<int>(PrimCount, Unity.Collections.Allocator.Persistent, NativeArrayOptions.ClearMemory);
            temp = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(tempArray);
            aabb_right = new AABB();
            int nodeIndex = 2;
            BuildRecursive(0, ref nodeIndex,0,PrimCount);
            indices_going_left_array.Dispose();
            tempArray.Dispose();
            SAHArray.Dispose();
            int BVHNodeCount = BVH2NodesArray.Length;
            NativeArray<int>.Copy(DimensionedIndicesArray, 0, FinalIndices, 0, PrimCount);
            DimensionedIndicesArray.Dispose();
        }
        NativeArray<AABB> PrimAABBs;
        public unsafe BVH2Builder(AABB[] MeshAABBs) {//Top Level Acceleration Structure
            PrimCount = MeshAABBs.Length;
            FinalIndices = new int[PrimCount];
            DimensionedIndicesArray = new NativeArray<int>(PrimCount * 3, Unity.Collections.Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            SAHArray = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            SAH = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(SAHArray);
            BVH2NodesArray = new NativeArray<BVHNode2Data>(PrimCount * 2, Unity.Collections.Allocator.Persistent, NativeArrayOptions.ClearMemory);
            BVH2Nodes = (BVHNode2Data*)NativeArrayUnsafeUtility.GetUnsafePtr(BVH2NodesArray);
            DimensionedIndices = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(DimensionedIndicesArray);
            BVH2Nodes[0].aabb.init();
            for(int i = 0; i < PrimCount; i++) {//Treat Bottom Level BVH Root Nodes as triangles
                FinalIndices[i] = i;
                SAH[i] = ((MeshAABBs[i].BBMax.x - MeshAABBs[i].BBMin.x)/2.0f + MeshAABBs[i].BBMin.x);
                BVH2Nodes[0].aabb.Extend(ref MeshAABBs[i]);
            }
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, 0, PrimCount);
            for(int i = 0; i < PrimCount; i++) {FinalIndices[i] = i; SAH[i] = (MeshAABBs[i].BBMax.y - MeshAABBs[i].BBMin.y) / 2.0f + MeshAABBs[i].BBMin.y;}
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount, PrimCount);
            for(int i = 0; i < PrimCount; i++) {FinalIndices[i] = i; SAH[i] = (MeshAABBs[i].BBMax.z - MeshAABBs[i].BBMin.z) / 2.0f + MeshAABBs[i].BBMin.z;}
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount * 2, PrimCount);

            indices_going_left_array = new NativeArray<bool>(PrimCount, Unity.Collections.Allocator.Persistent, NativeArrayOptions.ClearMemory);
            indices_going_left = (bool*)NativeArrayUnsafeUtility.GetUnsafePtr(indices_going_left_array);
            PrimAABBs = new NativeArray<AABB>(MeshAABBs, Unity.Collections.Allocator.Persistent);
            Primitives = (AABB*)NativeArrayUnsafeUtility.GetUnsafePtr(PrimAABBs);
            tempArray = new NativeArray<int>(PrimCount, Unity.Collections.Allocator.Persistent, NativeArrayOptions.ClearMemory);
            temp = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(tempArray);
            aabb_right = new AABB();
            int nodeIndex = 2;
            BuildRecursive(0, ref nodeIndex,0,PrimCount);
            // indices_going_left_array.Dispose();
            // tempArray.Dispose();
            // SAHArray.Dispose();
            int BVHNodeCount = BVH2NodesArray.Length;
            NativeArray<int>.Copy(DimensionedIndicesArray, 0, FinalIndices, 0, PrimCount);
            // DimensionedIndicesArray.Dispose();
            // PrimAABBs.Dispose();

        }

        public void ClearAll() {
            tempArray.Dispose();
            SAHArray.Dispose();
            indices_going_left_array.Dispose();
            DimensionedIndicesArray.Dispose();
            PrimAABBs.Dispose();
            BVH2NodesArray.Dispose();
        }

        public unsafe void NoAllocRebuild(AABB[] MeshAABBs) {//Top Level Acceleration Structure
            PrimCount = MeshAABBs.Length;
            FinalIndices = new int[PrimCount];
            // DimensionedIndicesArray = new NativeArray<int>(PrimCount * 3, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            // SAHArray = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            SAH = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(SAHArray);
            // BVH2NodesArray = new NativeArray<BVHNode2Data>(PrimCount * 2, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            BVH2Nodes = (BVHNode2Data*)NativeArrayUnsafeUtility.GetUnsafePtr(BVH2NodesArray);
            DimensionedIndices = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(DimensionedIndicesArray);
            BVH2Nodes[0].aabb.init();
            for(int i = 0; i < PrimCount; i++) {//Treat Bottom Level BVH Root Nodes as triangles
                FinalIndices[i] = i;
                SAH[i] = ((MeshAABBs[i].BBMax.x - MeshAABBs[i].BBMin.x)/2.0f + MeshAABBs[i].BBMin.x);
                BVH2Nodes[i] = new BVHNode2Data();
                BVH2Nodes[i].aabb.init();
                BVH2Nodes[i + PrimCount] = new BVHNode2Data();
                BVH2Nodes[0].aabb.Extend(ref MeshAABBs[i]);
            }
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, 0, PrimCount);
            for(int i = 0; i < PrimCount; i++) {FinalIndices[i] = i; SAH[i] = (MeshAABBs[i].BBMax.y - MeshAABBs[i].BBMin.y) / 2.0f + MeshAABBs[i].BBMin.y;}
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount, PrimCount);
            for(int i = 0; i < PrimCount; i++) {FinalIndices[i] = i; SAH[i] = (MeshAABBs[i].BBMax.z - MeshAABBs[i].BBMin.z) / 2.0f + MeshAABBs[i].BBMin.z;}
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount * 2, PrimCount);

            // indices_going_left_array = new NativeArray<bool>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            indices_going_left = (bool*)NativeArrayUnsafeUtility.GetUnsafePtr(indices_going_left_array);
            NativeArray<AABB>.Copy(MeshAABBs, 0, PrimAABBs, 0, PrimCount);
            // NativeArray<AABB> PrimAABBs = new NativeArray<AABB>(MeshAABBs, Unity.Collections.Allocator.TempJob);
            Primitives = (AABB*)NativeArrayUnsafeUtility.GetUnsafePtr(PrimAABBs);
            // tempArray = new NativeArray<int>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            temp = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(tempArray);
            aabb_right = new AABB();
            int nodeIndex = 2;
            BuildRecursive(0, ref nodeIndex,0,PrimCount);

            int BVHNodeCount = BVH2NodesArray.Length;
            NativeArray<int>.Copy(DimensionedIndicesArray, 0, FinalIndices, 0, PrimCount);
            // DimensionedIndicesArray.Dispose();
            // PrimAABBs.Dispose();

        }

    }
}