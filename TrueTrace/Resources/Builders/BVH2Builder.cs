using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using CommonVars;

namespace TrueTrace {

    public class BVH2Builder {

        public BVHNode2Data[] BVH2Nodes;
        private List<int>[] DimensionedIndices;
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

        ObjectSplit partition_sah(int first_index, int index_count, ref float[] sah, ref AABB[] Primitives) {

            split.cost = float.MaxValue;
            split.index = -1;
            split.dimension = -1;
            split.aabb_left.init();
            split.aabb_right.init();
            int CurIndex;

            AABB aabb_left = new AABB();
            AABB aabb_right = new AABB();
            for(int dimension = 0; dimension < 3; dimension++) {

                aabb_left.init();
                aabb_right.init();

                for(int i = 1; i < index_count; i++) {
                    CurIndex = DimensionedIndices[dimension][first_index + i - 1];
                    aabb_left.Extend(ref Primitives[CurIndex]);

                    sah[i] = surface_area(ref aabb_left) * (float)i;
                }

                for(int i = index_count - 1; i > 0; i--) {
                    CurIndex = DimensionedIndices[dimension][first_index + i];
                    aabb_right.Extend(ref Primitives[CurIndex]);

                    float cost = sah[i] + surface_area(ref aabb_right) * (float)(index_count - i);

                    if(cost <= split.cost) {
                        split.cost = cost;
                        split.index = first_index + i;
                        split.dimension = dimension;
                        split.aabb_right = aabb_right;
                    }
                }
            }
            int sindex = split.index;
            int sd = split.dimension;
            for(int i = first_index; i < sindex; i++) {
                CurIndex = DimensionedIndices[sd][i];
                split.aabb_left.Extend(ref Primitives[CurIndex]);
            }
            return split;
        }
        void BuildRecursive(int nodesi, ref int node_index, int first_index, int index_count, ref AABB[] Primitives) {
            if(index_count == 1) {
                BVH2Nodes[nodesi].first = first_index;
                BVH2Nodes[nodesi].left = first_index;
                BVH2Nodes[nodesi].count = (uint)index_count;
                return;
            }
            
            ObjectSplit split = partition_sah(first_index, index_count, ref SAH, ref Primitives);
            int EndIndex = first_index + index_count;
            int sd = split.dimension;
            for(int i = first_index; i < EndIndex; i++) indices_going_left[DimensionedIndices[sd][i]] = i < split.index;

            for(int dim = 0; dim < 3; dim++) {
                if(dim == sd) continue;

                int left = 0;
                int right = split.index - first_index;

                for(int i = first_index; i < EndIndex; i++) {
                    int index = DimensionedIndices[dim][i];

                    bool goes_left = indices_going_left[index];
                    if(goes_left) {
                        temp[left++] = index + 1;
                    } else {
                        temp[right++] = index + 1;
                    }
                }
          

                for(int index = 0; index < index_count; index++) {
                    DimensionedIndices[dim][index + first_index] = temp[index] - 1;    
                }
            }
            BVH2Nodes[nodesi].left = node_index;
            BVH2Nodes[nodesi].first = node_index;
            BVH2Nodes[nodesi].count = 0;
            BVH2Nodes[nodesi].axis = (uint)sd;

            BVH2Nodes[BVH2Nodes[nodesi].left].aabb = split.aabb_left;
            BVH2Nodes[BVH2Nodes[nodesi].left + 1].aabb = split.aabb_right;
            node_index += 2;
            int num_left = split.index - first_index;
            int num_right = EndIndex - split.index;

            BuildRecursive(BVH2Nodes[nodesi].left, ref node_index,first_index,num_left, ref Primitives);
            BuildRecursive(BVH2Nodes[nodesi].left + 1, ref node_index,first_index + num_left,num_right, ref Primitives);
        }

        float surface_area(ref AABB aabb) {
            Vector3 sizes = aabb.BBMax - aabb.BBMin;
            return 2.0f * ((sizes.x * sizes.y) + (sizes.x * sizes.z) + (sizes.y * sizes.z)); 
        }

        public BVH2Builder(ref AABB[] Triangles) {//Bottom Level Acceleration Structure Builder
            PrimCount = Triangles.Length;
            DimensionedIndices = new List<int>[3];
            DimensionedIndices[0] = new List<int>(PrimCount);
            DimensionedIndices[1] = new List<int>(PrimCount);             
            DimensionedIndices[2] = new List<int>(PrimCount);
            BVH2Nodes = new BVHNode2Data[PrimCount * 2];
            temp = new int[PrimCount]; 
            indices_going_left = new bool[PrimCount];
            AABB RootBB = new AABB();
            RootBB.init();
            float[] CentersX = new float[PrimCount];
            float[] CentersY = new float[PrimCount];
            float[] CentersZ = new float[PrimCount];
            Vector3 Center;
            for(int i = 0; i < PrimCount; i++) {
                DimensionedIndices[0].Add(i);
                DimensionedIndices[1].Add(i);
                DimensionedIndices[2].Add(i);
                indices_going_left[i] = false;
                CentersX[i] = (Triangles[i].BBMax.x - Triangles[i].BBMin.x) / 2.0f + Triangles[i].BBMin.x;
                CentersY[i] = (Triangles[i].BBMax.y - Triangles[i].BBMin.y) / 2.0f + Triangles[i].BBMin.y;
                CentersZ[i] = (Triangles[i].BBMax.z - Triangles[i].BBMin.z) / 2.0f + Triangles[i].BBMin.z;
                RootBB.Extend(ref Triangles[i]);
            }

            BVH2Nodes[0].aabb = RootBB;
            AABB tempAABB = new AABB();
            tempAABB.init();

            DimensionedIndices[0].Sort((s1,s2) => CentersX[s1].CompareTo(CentersX[s2]));
            DimensionedIndices[1].Sort((s1,s2) => CentersY[s1].CompareTo(CentersY[s2]));
            DimensionedIndices[2].Sort((s1,s2) => CentersZ[s1].CompareTo(CentersZ[s2]));

            int nodeIndex = 2;
            SAH = new float[PrimCount];
            BuildRecursive(0, ref nodeIndex,0,PrimCount, ref Triangles);
            Assert.IsTrue(nodeIndex <= 2 * PrimCount);
            int BVHNodeCount = BVH2Nodes.Length;
            FinalIndices = DimensionedIndices[0].ToArray();
            DimensionedIndices = null;

        }

        public BVH2Builder(AABB[] MeshAABBs) {//Top Level Acceleration Structure
            int MeshCount = MeshAABBs.Length;
            DimensionedIndices = new List<int>[3];
            DimensionedIndices[0] = new List<int>(MeshCount);
            DimensionedIndices[1] = new List<int>(MeshCount);             
            DimensionedIndices[2] = new List<int>(MeshCount);
            BVH2Nodes = new BVHNode2Data[MeshCount * 2];
            temp = new int[MeshCount];
            AABB[] Primitives = new AABB[MeshCount];
            indices_going_left = new bool[MeshCount];  
            AABB RootBB = new AABB();
            RootBB.init();
            float[] CentersX = new float[MeshCount];
            float[] CentersY = new float[MeshCount];
            float[] CentersZ = new float[MeshCount];
            Vector3 Center;
            for(int i = 0; i < MeshCount; i++) {//Treat Bottom Level BVH Root Nodes as triangles
                DimensionedIndices[0].Add(i);
                DimensionedIndices[1].Add(i);
                DimensionedIndices[2].Add(i);
                Primitives[i].BBMax = MeshAABBs[i].BBMax;
                Primitives[i].BBMin = MeshAABBs[i].BBMin;
                Center = ((MeshAABBs[i].BBMax - MeshAABBs[i].BBMin)/2.0f + MeshAABBs[i].BBMin);
                Primitives[i].BBMax = MeshAABBs[i].BBMax;
                Primitives[i].BBMin = MeshAABBs[i].BBMin;

                CentersX[i] = Center.x;
                CentersY[i] = Center.y;
                CentersZ[i] = Center.z;
                indices_going_left[i] = false;
                RootBB.Extend(ref Primitives[i]);
            }

            BVH2Nodes[0].aabb = RootBB;


            DimensionedIndices[0].Sort((s1,s2) => CentersX[s1].CompareTo(CentersX[s2]));
            DimensionedIndices[1].Sort((s1,s2) => CentersY[s1].CompareTo(CentersY[s2]));
            DimensionedIndices[2].Sort((s1,s2) => CentersZ[s1].CompareTo(CentersZ[s2]));

            int nodeIndex = 2;
            SAH = new float[MeshCount];
            BuildRecursive(0, ref nodeIndex,0,MeshCount, ref Primitives);

            Assert.IsTrue(nodeIndex <= 2 * MeshCount);
            int BVHNodeCount = BVH2Nodes.Length;
            FinalIndices = DimensionedIndices[0].ToArray();
            DimensionedIndices = null;
        }
    }
}