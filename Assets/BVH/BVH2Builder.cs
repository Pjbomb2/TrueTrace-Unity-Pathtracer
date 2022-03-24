using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using CommonVars;

[System.Serializable]
public class BVH2Builder {

    public List<BVHNode2Data> BVH2Nodes;
    private List<int>[] DimensionedIndices;
    private bool[] indices_going_left;
    private List<Vector3> Centers;
    private List<int> temp;
    private PrimitiveData[] Primitives;
    public int PrimCount;
    private int ProgId;
    public int[] FinalIndices;

    public struct ObjectSplit {
        public int index;
        public float cost;
        public int dimension;
        public AABB aabb_left;
        public AABB aabb_right;
    }

    ObjectSplit partition_sah(int first_index, int index_count, ref float[] sah) {

        ObjectSplit split = new ObjectSplit();
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
                aabb_left.Extend(Primitives[CurIndex].BBMax, Primitives[CurIndex].BBMin);

                sah[i] = surface_area(aabb_left.BBMax, aabb_left.BBMin) * (float)i;
            }

            for(int i = index_count - 1; i > 0; i--) {
                CurIndex = DimensionedIndices[dimension][first_index + i];
                aabb_right.Extend(Primitives[CurIndex].BBMax, Primitives[CurIndex].BBMin);

                float cost = sah[i] + surface_area(aabb_right.BBMax, aabb_right.BBMin) * (float)(index_count - i);

                if(cost <= split.cost) {
                    split.cost = cost;
                    split.index = first_index + i;
                    split.dimension = dimension;
                    split.aabb_right = aabb_right;
                }
            }

            Assert.IsTrue(aabb_left.BBMax != new Vector3(float.MinValue, float.MinValue, float.MinValue));
            Assert.IsTrue(aabb_left.BBMin != new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
            Assert.IsTrue(aabb_right.BBMax != new Vector3(float.MinValue, float.MinValue, float.MinValue));
            Assert.IsTrue(aabb_right.BBMin != new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
        }

        for(int i = first_index; i < split.index; i++) {
            CurIndex = DimensionedIndices[split.dimension][i];
            split.aabb_left.Extend(Primitives[CurIndex].BBMax, Primitives[CurIndex].BBMin);
        }
        return split;
    }

    void BuildRecursive(int nodesi, ref int node_index, int first_index, int index_count) {
        BVHNode2Data node = BVH2Nodes[nodesi];
        if(index_count == 1) {
            node.first = first_index;
            node.count = (uint)index_count;
            BVH2Nodes[nodesi] = node;
            return;
        }
        
        float[] SAH = new float[index_count];
        
        ObjectSplit split = partition_sah(first_index, index_count, ref SAH);

        for(int i = first_index; i < split.index; i++) indices_going_left[DimensionedIndices[split.dimension][i]] = true;
        for(int i = split.index; i < first_index + index_count; i++) indices_going_left[DimensionedIndices[split.dimension][i]] = false;

        for(int dim = 0; dim < 3; dim++) {
            if(dim == split.dimension) continue;

            int left = 0;
            int right = split.index - first_index;
            temp = new List<int>();
            for(int i9 = 0; i9 < index_count; i9++) temp.Add(-1);

            for(int i = first_index; i < first_index + index_count; i++) {
                int index = DimensionedIndices[dim][i];

                bool goes_left = indices_going_left[index];
                if(goes_left) {
                    temp[left++] = index;
                } else {
                    temp[right++] = index;
                }
            }
      
            Assert.IsTrue(left == split.index - first_index);
            Assert.IsTrue(right == index_count);
            for(int index = 0; index < index_count; index++) {
                DimensionedIndices[dim][index + first_index] = temp[index];    
            }
        }
        node.left = node_index;
        node.count = 0;
        node.axis = (uint)split.dimension;

        BVHNode2Data TempNodeLeft = BVH2Nodes[node.left];
        TempNodeLeft.BBMax = split.aabb_left.BBMax;
        TempNodeLeft.BBMin = split.aabb_left.BBMin;
        BVH2Nodes[node.left] = TempNodeLeft;
        BVHNode2Data TempNodeRight = BVH2Nodes[node.left + 1];
        TempNodeRight.BBMax = split.aabb_right.BBMax;
        TempNodeRight.BBMin = split.aabb_right.BBMin;
        BVH2Nodes[node.left + 1] = TempNodeRight;

        node_index += 2;
        UnityEditor.Progress.Report(ProgId, (float)node_index / (float)(PrimCount * 2));
        int num_left = split.index - first_index;
        int num_right = first_index + index_count - split.index;

        BVH2Nodes[nodesi] = node;
        BuildRecursive(node.left, ref node_index,first_index,num_left);
        BuildRecursive(node.left + 1, ref node_index,first_index + num_left,num_right);
    }

    float surface_area(in Vector3 BBMax, in Vector3 BBMin) {
        Vector3 sizes = BBMax - BBMin;
        return 2.0f * ((sizes.x * sizes.y) + (sizes.x * sizes.z) + (sizes.y * sizes.z)); 
    }

    public BVH2Builder(List<PrimitiveData> Triangles, int progressId) {//Bottom Level Acceleration Structure Builder
        ProgId = progressId;
        PrimCount = Triangles.Count;
        DimensionedIndices = new List<int>[3];
        DimensionedIndices[0] = new List<int>(PrimCount);
        DimensionedIndices[1] = new List<int>(PrimCount);             
        DimensionedIndices[2] = new List<int>(PrimCount);
        Centers = new List<Vector3>(PrimCount);
        BVH2Nodes = new List<BVHNode2Data>(PrimCount);
        temp = new List<int>(); 
        indices_going_left = new bool[PrimCount];
        Primitives = Triangles.ToArray();
        AABB RootBB = new AABB();
        RootBB.init();
        for(int i = 0; i < PrimCount; i++) {
            DimensionedIndices[0].Add(i);
            DimensionedIndices[1].Add(i);
            DimensionedIndices[2].Add(i);
            indices_going_left[i] = false;
            Centers.Add(Primitives[i].Center);
            temp.Add(0);
            RootBB.Extend(Primitives[i].BBMax, Primitives[i].BBMin);
        }

        BVH2Nodes.Add(new BVHNode2Data() {
            BBMin = RootBB.BBMin,
            BBMax = RootBB.BBMax
        });

         for(int i = 1; i < PrimCount * 2; ++i) {
            BVH2Nodes.Add(new BVHNode2Data() {
                BBMin = new Vector3(float.MaxValue,float.MaxValue,float.MaxValue),
                BBMax = new Vector3(float.MinValue,float.MinValue,float.MinValue),
                count = 30,
                axis = 2
            });
         }

        DimensionedIndices[0].Sort((s1,s2) => Centers[s1].x.CompareTo(Centers[s2].x));
        DimensionedIndices[1].Sort((s1,s2) => Centers[s1].y.CompareTo(Centers[s2].y));
        DimensionedIndices[2].Sort((s1,s2) => Centers[s1].z.CompareTo(Centers[s2].z));

        int nodeIndex = 2;

        BuildRecursive(0, ref nodeIndex,0,PrimCount);

        Assert.IsTrue(nodeIndex <= 2 * PrimCount);
        int BVHNodeCount = BVH2Nodes.Count;
        BVHNode2Data TempNode;
        for(int i = 0; i < BVHNodeCount; ++i) {
            TempNode = BVH2Nodes[i];
            if(BVH2Nodes[i].count != 0) {
                TempNode.left = BVH2Nodes[i].first;
            } else {
                TempNode.first = BVH2Nodes[i].left;
            }

            BVH2Nodes[i] = TempNode;
        }
        FinalIndices = DimensionedIndices[0].ToArray();
        DimensionedIndices = null;

        UnityEditor.Progress.Report(ProgId, 1.0f);
    }
//28 ms


    public BVH2Builder(AABB[] MeshAABBs) {//Top Level Acceleration Structure
        int MeshCount = MeshAABBs.Length;
        DimensionedIndices = new List<int>[3];
        DimensionedIndices[0] = new List<int>(MeshCount);
        DimensionedIndices[1] = new List<int>(MeshCount);             
        DimensionedIndices[2] = new List<int>(MeshCount);
        Centers = new List<Vector3>();
        BVH2Nodes = new List<BVHNode2Data>();
        temp = new List<int>();
        Primitives = new PrimitiveData[MeshCount];
        indices_going_left = new bool[MeshCount];  
        AABB RootBB = new AABB();
        RootBB.init();
        for(int i = 0; i < MeshCount; i++) {//Treat Bottom Level BVH Root Nodes as triangles
            DimensionedIndices[0].Add(i);
            DimensionedIndices[1].Add(i);
            DimensionedIndices[2].Add(i);
            Primitives[i].BBMax = MeshAABBs[i].BBMax;
            Primitives[i].BBMin = MeshAABBs[i].BBMin;
            Primitives[i].Center = ((MeshAABBs[i].BBMax - MeshAABBs[i].BBMin)/2.0f + MeshAABBs[i].BBMin);

            Centers.Add(Primitives[i].Center);
            indices_going_left[i] = false;
            temp.Add(0);
            RootBB.Extend(Primitives[i].BBMax, Primitives[i].BBMin);
        }

        BVH2Nodes.Add(new BVHNode2Data() {
            BBMin = RootBB.BBMin,
            BBMax = RootBB.BBMax
        });

         for(int i = 1; i < MeshCount * 2; ++i) {
            BVH2Nodes.Add(new BVHNode2Data() {
                BBMin = new Vector3(float.MaxValue,float.MaxValue,float.MaxValue),
                BBMax = new Vector3(float.MinValue,float.MinValue,float.MinValue),
                count = 30,
                axis = 2
            });
         }

        DimensionedIndices[0].Sort((s1,s2) => Centers[s1].x.CompareTo(Centers[s2].x));
        DimensionedIndices[1].Sort((s1,s2) => Centers[s1].y.CompareTo(Centers[s2].y));
        DimensionedIndices[2].Sort((s1,s2) => Centers[s1].z.CompareTo(Centers[s2].z));

        int nodeIndex = 2;

        BuildRecursive(0, ref nodeIndex,0,MeshCount);

        Assert.IsTrue(nodeIndex <= 2 * MeshCount);
        int BVHNodeCount = BVH2Nodes.Count;
        BVHNode2Data TempNode;
        for(int i = 0; i < BVHNodeCount; ++i) {
            TempNode = BVH2Nodes[i];
            if(BVH2Nodes[i].count != 0) {
                TempNode.left = BVH2Nodes[i].first;
            } else {
                TempNode.first = BVH2Nodes[i].left;
            }
            BVH2Nodes[i] = TempNode;
        }
        FinalIndices = DimensionedIndices[0].ToArray();
        DimensionedIndices = null;
    }
}
