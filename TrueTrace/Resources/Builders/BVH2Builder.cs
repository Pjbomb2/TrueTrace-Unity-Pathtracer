using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System; 
namespace TrueTrace {

    public class BVH2Builder {

        public BVHNode2Data[] BVH2Nodes;
        private int[] DimensionedIndices;
        private bool[] indices_going_left;
        private int[] temp;
        public int PrimCount;
        public int[] FinalIndices;
        private ObjectSplit split = new ObjectSplit();
        private float[] SAH;
        
        public struct ObjectSplit {
            public int index;
            public float cost;
            public int dimension;
            public AABB aabb_left;
            public AABB aabb_right;
        }

        ObjectSplit partition_sah(int first_index, int index_count, ref AABB[] Primitives) {
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
            return split;
        }
        void BuildRecursive(int nodesi, ref int node_index, int first_index, int index_count, ref AABB[] Primitives) {
            if(index_count == 1) {
                BVH2Nodes[nodesi].left = first_index;
                BVH2Nodes[nodesi].count = (uint)index_count;
                return;
            }
            
            ObjectSplit split = partition_sah(first_index, index_count, ref Primitives);
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
          
                System.Array.Copy(temp, 0, DimensionedIndices, Offset + first_index, index_count);
            }
            BVH2Nodes[nodesi].left = node_index;

            BVH2Nodes[BVH2Nodes[nodesi].left].aabb = split.aabb_left;
            BVH2Nodes[BVH2Nodes[nodesi].left + 1].aabb = split.aabb_right;
            node_index += 2;

            BuildRecursive(BVH2Nodes[nodesi].left, ref node_index, first_index, split.index - first_index, ref Primitives);
            BuildRecursive(BVH2Nodes[nodesi].left + 1, ref node_index, split.index, first_index + index_count - split.index, ref Primitives);
        }

        float surface_area(ref AABB aabb) {
            Vector3 sizes = aabb.BBMax - aabb.BBMin;
            return 2.0f * ((sizes.x * sizes.y) + (sizes.x * sizes.z) + (sizes.y * sizes.z)); 
        }

        public BVH2Builder(ref AABB[] Triangles) {//Bottom Level Acceleration Structure Builder
            PrimCount = Triangles.Length;
            temp = new int[PrimCount]; 
            SAH = new float[PrimCount];
            FinalIndices = new int[PrimCount];
            float[] CentersX = new float[PrimCount];
            float[] CentersY = new float[PrimCount];
            float[] CentersZ = new float[PrimCount];
            indices_going_left = new bool[PrimCount];
            DimensionedIndices = new int[3 * PrimCount];
            BVH2Nodes = new BVHNode2Data[PrimCount * 2];
            BVH2Nodes[0].aabb.init();
            for(int i = 0; i < PrimCount; i++) {
                FinalIndices[i] = i;
                CentersX[i] = (Triangles[i].BBMax.x - Triangles[i].BBMin.x) / 2.0f + Triangles[i].BBMin.x;
                CentersY[i] = (Triangles[i].BBMax.y - Triangles[i].BBMin.y) / 2.0f + Triangles[i].BBMin.y;
                CentersZ[i] = (Triangles[i].BBMax.z - Triangles[i].BBMin.z) / 2.0f + Triangles[i].BBMin.z;
                BVH2Nodes[0].aabb.Extend(ref Triangles[i]);
            }
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersX[s1] - CentersX[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            System.Array.Copy(FinalIndices, 0, DimensionedIndices, 0, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersY[s1] - CentersY[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            System.Array.Copy(FinalIndices, 0, DimensionedIndices, PrimCount, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersZ[s1] - CentersZ[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            System.Array.Copy(FinalIndices, 0, DimensionedIndices, PrimCount * 2, PrimCount);
            int nodeIndex = 2;
            BuildRecursive(0, ref nodeIndex,0,PrimCount, ref Triangles);
            int BVHNodeCount = BVH2Nodes.Length;
            System.Array.Copy(DimensionedIndices, 0, FinalIndices, 0, PrimCount);
            DimensionedIndices = null;
        }

        public BVH2Builder(AABB[] MeshAABBs) {//Top Level Acceleration Structure
            PrimCount = MeshAABBs.Length;
            temp = new int[PrimCount];
            SAH = new float[PrimCount];
            FinalIndices = new int[PrimCount];
            float[] CentersX = new float[PrimCount];
            float[] CentersY = new float[PrimCount];
            float[] CentersZ = new float[PrimCount];
            indices_going_left = new bool[PrimCount];  
            DimensionedIndices = new int[3 * PrimCount];
            BVH2Nodes = new BVHNode2Data[PrimCount * 2];
            BVH2Nodes[0].aabb.init();
            for(int i = 0; i < PrimCount; i++) {//Treat Bottom Level BVH Root Nodes as triangles
                FinalIndices[i] = i;
                CentersX[i] = ((MeshAABBs[i].BBMax.x - MeshAABBs[i].BBMin.x)/2.0f + MeshAABBs[i].BBMin.x);
                CentersY[i] = ((MeshAABBs[i].BBMax.y - MeshAABBs[i].BBMin.y)/2.0f + MeshAABBs[i].BBMin.y);
                CentersZ[i] = ((MeshAABBs[i].BBMax.z - MeshAABBs[i].BBMin.z)/2.0f + MeshAABBs[i].BBMin.z);
                BVH2Nodes[0].aabb.Extend(ref MeshAABBs[i]);
            }
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersX[s1] - CentersX[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            System.Array.Copy(FinalIndices, 0, DimensionedIndices, 0, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersY[s1] - CentersY[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            System.Array.Copy(FinalIndices, 0, DimensionedIndices, PrimCount, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersZ[s1] - CentersZ[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            System.Array.Copy(FinalIndices, 0, DimensionedIndices, PrimCount * 2, PrimCount);

            int nodeIndex = 2;
            BuildRecursive(0, ref nodeIndex,0,PrimCount, ref MeshAABBs);
            int BVHNodeCount = BVH2Nodes.Length;
            System.Array.Copy(DimensionedIndices, 0, FinalIndices, 0, PrimCount);
            DimensionedIndices = null;
        }
    }
}