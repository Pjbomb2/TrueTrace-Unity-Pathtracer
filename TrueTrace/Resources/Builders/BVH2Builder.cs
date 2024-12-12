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
        private int* DimensionedIndices;
        private int* temp;
        private bool* indices_going_left;
        public int PrimCount;
        public int[] FinalIndices;
        private ObjectSplit split = new ObjectSplit();
        private float* SAH;
        private AABB* Primitives;
        
        // public BVHNodeVerbose[] VerboseNodes;


        public NativeArray<BVHNode2Data> BVH2NodesArray;
        public NativeArray<int> DimensionedIndicesArray;
        public NativeArray<int> tempArray;
        public NativeArray<bool> indices_going_left_array;
        public NativeArray<float> CentersX;
        public NativeArray<float> CentersY;
        public NativeArray<float> CentersZ;
        public NativeArray<float> SAHArray;

        public struct ObjectSplit {
            public int index;
            public float cost;
            public int dimension;
            public AABB aabb_left;
            public AABB aabb_right;
        }

        void partition_sah(int first_index, int index_count) {
            split.cost = float.MaxValue;
            split.index = -1;
            split.dimension = -1;
            split.aabb_left.init();
            split.aabb_right.init();

            AABB aabb_left = new AABB();
            AABB aabb_right = new AABB();
            int Offset;
            for(int dimension = 0; dimension < 3; dimension++) {
                aabb_left.init();
                aabb_right.init();
                Offset = PrimCount * dimension + first_index;
                for(int i = 1; i < index_count; i++) {
                    aabb_left.Extend(ref Primitives[DimensionedIndices[Offset + i - 1]]);

                    SAH[i] = surface_area(ref aabb_left) * (float)i;
                }

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
            Vector3 sizes = aabb.BBMax - aabb.BBMin;
            return 2.0f * ((sizes.x * sizes.y) + (sizes.x * sizes.z) + (sizes.y * sizes.z)); 
        }

        // uint usedVerboseNodes = 0;

        // void RefitUp(ref BVHNodeVerbose[] TempVerboseNodes, int ChildIndex, int ParentIndex, int CurDepth) {
        //     TempVerboseNodes[ChildIndex].aabb = BVH2Nodes[ChildIndex].aabb;
        //     TempVerboseNodes[ChildIndex].count = BVH2Nodes[ChildIndex].count;
        //     TempVerboseNodes[ChildIndex].left = (uint)BVH2Nodes[ChildIndex].left;
        //     TempVerboseNodes[ChildIndex].right = (uint)BVH2Nodes[ChildIndex].left + 1;
        //     TempVerboseNodes[ChildIndex].parent = (uint)ParentIndex;
        //     if(CurDepth != 0 && (BVH2Nodes[ChildIndex].count != 0 || (BVH2Nodes[ChildIndex].left == 0))) return;
        //     RefitUp(ref TempVerboseNodes, BVH2Nodes[ChildIndex].left, ChildIndex, CurDepth + 1);
        //     RefitUp(ref TempVerboseNodes, BVH2Nodes[ChildIndex].left + 1, ChildIndex, CurDepth + 1);
        // }

        // void RefitUpVerbose(ref BVHNodeVerbose[] verbose, uint nodeIdx)
        // {
        //     while (nodeIdx != 0xffffffff)
        //     {
        //         BVHNodeVerbose node = verbose[nodeIdx];
        //         BVHNodeVerbose left = verbose[node.left];
        //         BVHNodeVerbose right = verbose[node.right];
        //         node.aabb.BBMin = Vector3.Min( left.aabb.BBMin, right.aabb.BBMin );
        //         node.aabb.BBMax = Vector3.Max( left.aabb.BBMax, right.aabb.BBMax );
        //         verbose[nodeIdx] = node;
        //         nodeIdx = node.parent;
        //     }
        // }

        // uint[] taskNode = new uint[512];
        // float[] taskCi = new float[512];
        // float[] taskInvCi = new float[512];
        // uint FindBestNewPosition(ref BVHNodeVerbose[] verbose, uint Lid )
        // {
        //     BVHNodeVerbose L = verbose[Lid];
        //     float SA_L = surface_area(ref L.aabb);
        //     // reinsert L into BVH
        //     uint tasks = 1;
        //     uint Xbest = 0;
        //     float Cbest = 1e30f;
        //     float epsilon = 1e-10f;
        //     taskNode[0] = 0; /* root */ 
        //     taskCi[0] = 0; 
        //     taskInvCi[0] = 1 / epsilon;
        //     AABB tempAABB = new AABB();
        //     while (tasks > 0)
        //     {
        //         // 'pop' task with createst taskInvCi
        //         float maxInvCi = 0;
        //         uint bestTask = 0;
        //         for (uint j = 0; j < tasks; j++) {
        //             if (taskInvCi[j] > maxInvCi) {
        //                 maxInvCi = taskInvCi[j];
        //                 bestTask = j;
        //             }
        //         }
        //         uint Xid = taskNode[bestTask];
        //         float CiLX = taskCi[bestTask];
        //         taskNode[bestTask] = taskNode[--tasks];
        //         taskCi[bestTask] = taskCi[tasks];
        //         taskInvCi[bestTask] = taskInvCi[tasks];
        //         // execute task
        //         BVHNodeVerbose X = verbose[Xid];
        //         if (CiLX + SA_L >= Cbest) break;
        //         tempAABB.Create(Vector3.Max(L.aabb.BBMax, X.aabb.BBMax), Vector3.Min(L.aabb.BBMin, X.aabb.BBMin));
        //         float CdLX = surface_area(ref tempAABB);
        //         float CLX = CiLX + CdLX;
        //         if (CLX < Cbest) {
        //             Cbest = CLX;
        //             Xbest = Xid;
        //         }
        //         float Ci = CLX - surface_area(ref X.aabb);
        //         if (Ci + SA_L < Cbest) {
        //             if (!X.isLeaf())
        //             {
        //                 taskNode[tasks] = X.left;
        //                 taskCi[tasks] = Ci;
        //                 taskInvCi[tasks++] = 1.0f / (Ci + epsilon);
        //                 taskNode[tasks] = X.right;
        //                 taskCi[tasks] = Ci;
        //                 taskInvCi[tasks++] = 1.0f / (Ci + epsilon);
        //             }
        //         }
        //     }
        //     return Xbest;
        // }

        // void ReinsertNodeVerbose(ref BVHNodeVerbose[] verbose, uint Lid, uint Nid, uint origin )
        // {
        //     uint Xbest = FindBestNewPosition(ref verbose, Lid );
        //     if (verbose[Xbest].parent == 0) Xbest = origin;
        //     uint X1 = verbose[Xbest].parent;
        //     BVHNodeVerbose N = verbose[Nid];
        //     N.left = Xbest;
        //     N.right = Lid;
        //     N.aabb.BBMin = Vector3.Min( verbose[Xbest].aabb.BBMin, verbose[Lid].aabb.BBMin );
        //     N.aabb.BBMax = Vector3.Max( verbose[Xbest].aabb.BBMax, verbose[Lid].aabb.BBMax );
        //     verbose[Nid] = N;
        //     verbose[Nid].parent = X1;
        //     if (verbose[X1].left == Xbest) verbose[X1].left = Nid; else verbose[X1].right = Nid;
        //     verbose[Lid].parent = Nid;
        //     verbose[Xbest].parent = verbose[Lid].parent;

        //     RefitUpVerbose(ref verbose, Nid );
        // }

        // void Optimize(ref BVHNodeVerbose[] verbose, uint node_index, ref uint seed) {
        //     uint Nid = 0;
        //     uint valid = 0;
        //     do
        //     {
        //         seed ^= seed << 13;
        //         seed ^= seed >> 17;
        //         seed ^= seed << 5; // xor32
        //         valid = 1;
        //         Nid = 2 + seed % (node_index - 2);
        //         if (verbose[Nid].parent == 0 || verbose[Nid].isLeaf()) valid = 0;
        //         if (valid != 0) if (verbose[verbose[Nid].parent].parent == 0) valid = 0;
        //     } while (valid == 0);
        //     // snip it loose
        //     BVHNodeVerbose N = verbose[Nid];
        //     BVHNodeVerbose P = verbose[N.parent];
        //     uint Pid = N.parent, X1 = P.parent;
        //     uint X2 = P.left == Nid ? P.right : P.left;
        //     if (verbose[X1].left == Pid) verbose[X1].left = X2;
        //     else /* verbose[X1].right == Pid */ verbose[X1].right = X2;
        //     verbose[X2].parent = X1;
        //     uint L = N.left;
        //     uint R = N.right;
        //     // fix affected node bounds
        //     verbose[Nid] = N;
        //     verbose[N.parent] = P;
        //     RefitUpVerbose(ref verbose, X1 );
        //     ReinsertNodeVerbose(ref verbose, L, Pid, X1 );
        //     ReinsertNodeVerbose(ref verbose, R, Nid, X1 );
        // }


        // float SAHCost(ref BVHNodeVerbose[] verbose, uint nodeIdx = 0 )
        // {
        //     // Determine the SAH cost of the tree. This provides an indication
        //     // of the quality of the BVH: Lower is better.
        //     BVHNodeVerbose n = verbose[nodeIdx];
        //     if (n.isLeaf()) return 2.0f * surface_area(ref n.aabb) * n.triCount;
        //     float cost = 3.0f * surface_area(ref n.aabb) + SAHCost(ref verbose, n.left) + SAHCost(ref verbose, n.right);
        //     return nodeIdx == 0 ? (cost / surface_area(ref n.aabb)) : cost;
        // }

        // int GetTriCount(ref BVHNodeVerbose[] verbose, uint Index) {
        //     if(verbose[Index].count == 1) return 1;
        //     else if(verbose[Index].left == 0) return 0;
        //     else return GetTriCount(ref verbose, verbose[Index].left) + GetTriCount(ref verbose, verbose[Index].right);
        // }

        public unsafe BVH2Builder(AABB* Triangles, int PrimCount) {//Bottom Level Acceleration Structure Builder
            this.PrimCount = PrimCount;
            Primitives = Triangles;
            FinalIndices = new int[PrimCount];
            SAHArray = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            DimensionedIndicesArray = new NativeArray<int>(PrimCount * 3, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            CentersX = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            CentersY = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            BVH2NodesArray = new NativeArray<BVHNode2Data>(PrimCount * 2, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            BVH2Nodes = (BVHNode2Data*)NativeArrayUnsafeUtility.GetUnsafePtr(BVH2NodesArray);
            float* ptr = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(CentersX);
            float* ptr1 = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(CentersY);
            DimensionedIndices = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(DimensionedIndicesArray);
            SAH = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(SAHArray);
            BVH2Nodes[0].aabb.init();
            for(int i = 0; i < PrimCount; i++) {
                FinalIndices[i] = i;
                ptr[i] = (Triangles[i].BBMax.x - Triangles[i].BBMin.x) / 2.0f + Triangles[i].BBMin.x;
                ptr1[i] = (Triangles[i].BBMax.y - Triangles[i].BBMin.y) / 2.0f + Triangles[i].BBMin.y;
                SAH[i] = (Triangles[i].BBMax.z - Triangles[i].BBMin.z) / 2.0f + Triangles[i].BBMin.z;
                BVH2Nodes[0].aabb.Extend(ref Triangles[i]);
            }
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = ptr[s1] - ptr[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            CentersX.Dispose();
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, 0, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = ptr1[s1] - ptr1[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            CentersY.Dispose();
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount * 2, PrimCount);


            indices_going_left_array = new NativeArray<bool>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            indices_going_left = (bool*)NativeArrayUnsafeUtility.GetUnsafePtr(indices_going_left_array);
            tempArray = new NativeArray<int>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            temp = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(tempArray);
            int nodeIndex = 2;
            BuildRecursive(0, ref nodeIndex,0,PrimCount);
            indices_going_left_array.Dispose();
            tempArray.Dispose();
            SAHArray.Dispose();
            int BVHNodeCount = BVH2NodesArray.Length;
            NativeArray<int>.Copy(DimensionedIndicesArray, 0, FinalIndices, 0, PrimCount);
            DimensionedIndicesArray.Dispose();


            // VerboseNodes = new BVHNodeVerbose[BVH2NodesArray.Length];
            // usedVerboseNodes = (uint)BVH2NodesArray.Length;


            // uint nodeIdx = 0;
            // uint parent = 0xffffffff;
            // uint[] stack = new uint[128];
            // uint stackPtr = 0;
            // while (true)
            // {
            //     BVHNode2Data node = BVH2Nodes[nodeIdx];
            //     VerboseNodes[nodeIdx].aabb.BBMin = node.aabb.BBMin;
            //     VerboseNodes[nodeIdx].aabb.BBMax = node.aabb.BBMax;
            //     VerboseNodes[nodeIdx].count = node.count;
            //     VerboseNodes[nodeIdx].parent = parent;
            //     if (node.count != 0)
            //     {
            //         VerboseNodes[nodeIdx].left = (uint)node.left;
            //         if (stackPtr == 0) break;
            //         nodeIdx = stack[--stackPtr];
            //         parent = stack[--stackPtr];
            //     }
            //     else
            //     {
            //         VerboseNodes[nodeIdx].left = (uint)node.left;
            //         VerboseNodes[nodeIdx].right = (uint)node.left + 1;
            //         stack[stackPtr++] = nodeIdx;
            //         stack[stackPtr++] = (uint)node.left + 1;
            //         parent = nodeIdx;
            //         nodeIdx = (uint)node.left;
            //     }
            // }


            // // RefitUp(ref VerboseNodes, 0, 0, 0);
            // VerboseNodes[0].parent = 0xffffffff;
            // uint seed = 0x12345678;
            // for(int i = 0; i < 1000000; i++) Optimize(ref VerboseNodes, (uint)nodeIndex, ref seed);

            // int VerbCount = VerboseNodes.Length;
            // for(int i = 0; i < VerbCount; i++) {
            //     VerboseNodes[i].triCount = (uint)GetTriCount(ref VerboseNodes, (uint)i);
            // }

            // // Debug.Log("SAH: " + SAHCost(ref VerboseNodes, 0));

            // uint srcNodeIdx = 0;
            // uint dstNodeIdx = 0;
            // uint newNodePtr = 2;
            // uint[] srcStack = new uint[64];
            // uint[] dstStack = new uint[64];
            // stackPtr = 0;

            // while(true) {
            //     BVHNodeVerbose srcNode = VerboseNodes[srcNodeIdx];
            //     BVH2Nodes[dstNodeIdx].aabb.BBMin = srcNode.aabb.BBMin;
            //     BVH2Nodes[dstNodeIdx].aabb.BBMax = srcNode.aabb.BBMax;
            //     if (srcNode.isLeaf())
            //     {
            //         BVH2Nodes[dstNodeIdx].left = (int)srcNode.left;
            //         BVH2Nodes[dstNodeIdx].count = srcNode.count;
            //         if (stackPtr == 0) break;
            //         srcNodeIdx = srcStack[--stackPtr];
            //         dstNodeIdx = dstStack[stackPtr];
            //     }
            //     else
            //     {
            //         BVH2Nodes[dstNodeIdx].left = (int)newNodePtr;
            //         BVH2Nodes[dstNodeIdx].count = srcNode.count;
            //         uint srcRightIdx = srcNode.right;
            //         srcNodeIdx = srcNode.left;
            //         dstNodeIdx = newNodePtr++;
            //         srcStack[stackPtr] = srcRightIdx;
            //         dstStack[stackPtr++] = newNodePtr++;
            //     }
            // }

            // Debug.Log(seed);

            // int LEN = VerboseNodes.Length;
            // for(int i = 0; i < LEN; i++) {
            //     // if(BVH2Nodes[i].left != (int)VerboseNodes[i].left) Debug.Log("EEE");
            //     if((int)VerboseNodes[i].right != (int)VerboseNodes[i].left + 1 && (int)VerboseNodes[i].left != 0) Debug.Log("AAAA " + i + "; " + (int)VerboseNodes[i].left + "; " + (int)VerboseNodes[i].right);
            //     BVH2Nodes[i].aabb = VerboseNodes[i].aabb;
            //     BVH2Nodes[i].left = (int)VerboseNodes[i].left;
            //     BVH2Nodes[i].count = VerboseNodes[i].count;
            // }
        }

        public unsafe BVH2Builder(AABB[] MeshAABBs) {//Top Level Acceleration Structure
            PrimCount = MeshAABBs.Length;
            FinalIndices = new int[PrimCount];
            DimensionedIndicesArray = new NativeArray<int>(PrimCount * 3, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            CentersX = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            CentersY = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            SAHArray = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            BVH2NodesArray = new NativeArray<BVHNode2Data>(PrimCount * 2, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            BVH2Nodes = (BVHNode2Data*)NativeArrayUnsafeUtility.GetUnsafePtr(BVH2NodesArray);
            float* ptr = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(CentersX);
            float* ptr1 = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(CentersY);
            SAH = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(SAHArray);
            DimensionedIndices = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(DimensionedIndicesArray);
            BVH2Nodes[0].aabb.init();
            for(int i = 0; i < PrimCount; i++) {//Treat Bottom Level BVH Root Nodes as triangles
                FinalIndices[i] = i;
                ptr[i] = ((MeshAABBs[i].BBMax.x - MeshAABBs[i].BBMin.x)/2.0f + MeshAABBs[i].BBMin.x);
                ptr1[i] = ((MeshAABBs[i].BBMax.y - MeshAABBs[i].BBMin.y)/2.0f + MeshAABBs[i].BBMin.y);
                SAH[i] = ((MeshAABBs[i].BBMax.z - MeshAABBs[i].BBMin.z)/2.0f + MeshAABBs[i].BBMin.z);
                BVH2Nodes[0].aabb.Extend(ref MeshAABBs[i]);
            }
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = ptr[s1] - ptr[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            CentersX.Dispose();
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, 0, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = ptr1[s1] - ptr1[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            CentersY.Dispose();
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount * 2, PrimCount);

            indices_going_left_array = new NativeArray<bool>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            indices_going_left = (bool*)NativeArrayUnsafeUtility.GetUnsafePtr(indices_going_left_array);
            NativeArray<AABB> PrimAABBs = new NativeArray<AABB>(MeshAABBs, Unity.Collections.Allocator.TempJob);
            Primitives = (AABB*)NativeArrayUnsafeUtility.GetUnsafePtr(PrimAABBs);
            tempArray = new NativeArray<int>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            temp = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(tempArray);
            int nodeIndex = 2;
            BuildRecursive(0, ref nodeIndex,0,PrimCount);
            indices_going_left_array.Dispose();
            tempArray.Dispose();
            SAHArray.Dispose();
            int BVHNodeCount = BVH2NodesArray.Length;
            NativeArray<int>.Copy(DimensionedIndicesArray, 0, FinalIndices, 0, PrimCount);
            DimensionedIndicesArray.Dispose();
            PrimAABBs.Dispose();

        }

    




    }
}