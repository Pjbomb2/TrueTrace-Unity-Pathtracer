using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;


[System.Serializable]
unsafe public class OctreeBuilder
{

    private Vector3 Size;
    public Vector3 Center;
    public Vector3 BBMaxTop;
    public Vector3 BBMinTop;
    public Vector3 OrigOrigSize;
    public CompressedOctreeNode[] CompressedOctree;
    public Voxel[] TopVox;

    public List<OctreeNode> Octree = new List<OctreeNode>();
    public List<Voxel> OrderedVoxels;
    private Vector3 GetPosition(int Index) {
        Vector3 location = new Vector3(0,0,0);
        location.x = (float)Index % Size.x;
        location.y = (float)(((Index - location.x) / Size.x) % Size.y);
        location.z = (float)(((Index - location.x - Size.x * location.y) / (Size.y * Size.x)));
        return location + new Vector3(0.5f, 0.5f, 0.5f);
    }

    Vector3 GetPositionOrig(int Index) {
        Vector3 location;
        location.x = (float)Index % OrigOrigSize.x;
        location.y = (float)(((Index - location.x) / OrigOrigSize.x) % OrigOrigSize.y);
        location.z = (float)(((Index - location.x - OrigOrigSize.x * location.y) / (OrigOrigSize.y * OrigOrigSize.x)));
        return location + new Vector3(0.5f,0.5f,0.5f);
    }
    public List<Voxel>LastNodes;
public int MaxDepth = 12;
public int MaxRecursions = 0;
    public int TotalDepth;
    public void RecursiveBuild(Voxel[] ChildrenIn, Vector3 BBMax, Vector3 BBMin, int PrevNode, int PrevOffset, int CurDepth, ref OctreeNode PrevNode2) {
        TotalDepth = Mathf.Max(CurDepth, TotalDepth);
        PrevNode2.ChildNode[PrevOffset] = Octree.Count - 1;

        Octree.Add(new OctreeNode());
        var CurrentCount = Octree.Count;
        var TempNode = new OctreeNode();
        TempNode.BBMax = BBMax;
        TempNode.BBMin = BBMin;
        List<Voxel>[] ChildrenNodes = new List<Voxel>[8];
        ChildrenNodes[0] = new List<Voxel>();
        ChildrenNodes[1] = new List<Voxel>();
        ChildrenNodes[2] = new List<Voxel>();
        ChildrenNodes[3] = new List<Voxel>();
        ChildrenNodes[4] = new List<Voxel>();
        ChildrenNodes[5] = new List<Voxel>();
        ChildrenNodes[6] = new List<Voxel>();
        ChildrenNodes[7] = new List<Voxel>();
        Vector3[] Max = new Vector3[8];
        Vector3[] Min = new Vector3[8];
        Vector3 Center = (BBMax - BBMin) / 2;
        Vector3 Axis = (BBMax - BBMin) / 2;
        TempNode.Center = Axis + BBMin;
        Axis += BBMin;
        TempNode.Extent = (BBMax - BBMin);
        BBMaxTop = BBMax;
        BBMinTop = BBMin;
        Center = Axis;
        Vector3 Extents = BBMax - Center;

        Min[0] = Axis - Extents; Max[0] = Axis;//Bottom Left
        Min[6] = Axis; Max[6] = Axis + Extents;//Top Right
        Min[3] = new Vector3(BBMin.x, Axis.y, BBMin.z); Max[3] = new Vector3(Axis.x, Axis.y + Extents.y, Axis.z);
        Min[5] = new Vector3(Axis.x, BBMin.y, Axis.z); Max[5] = new Vector3(Axis.x + Extents.x, Axis.y, Axis.z + Extents.z);
        Min[4] = new Vector3(BBMin.x, BBMin.y, Axis.z); Max[4] = new Vector3(Axis.x, Axis.y, BBMax.z);
        Min[7] = new Vector3(BBMin.x, Axis.y, Axis.z); Max[7] = new Vector3(Axis.x, BBMax.y, BBMax.z);
        Min[1] = new Vector3(Axis.x, BBMin.y, BBMin.z); Max[1] = new Vector3(BBMax.x, Axis.y, Axis.z);
        Min[2] = new Vector3(Axis.x, Axis.y, BBMin.z); Max[2] = new Vector3(BBMax.x, BBMax.y, Axis.z);
        for(int i = 0; i < ChildrenIn.Length; i++) {
            Vector3 Position = GetPosition(ChildrenIn[i].Index);
            if(Position.x < Axis.x && Position.y < Axis.y && Position.z < Axis.z) {ChildrenNodes[0].Add(ChildrenIn[i]); continue;}//0
            if(Position.x >= Axis.x && Position.y < Axis.y && Position.z < Axis.z) {ChildrenNodes[1].Add(ChildrenIn[i]); continue;}//6
            if(Position.x >= Axis.x && Position.y >= Axis.y && Position.z < Axis.z) {ChildrenNodes[2].Add(ChildrenIn[i]); continue;}//7
            if(Position.x < Axis.x && Position.y >= Axis.y && Position.z < Axis.z) {ChildrenNodes[3].Add(ChildrenIn[i]); continue;}//2
            if(Position.x < Axis.x && Position.y < Axis.y && Position.z >= Axis.z) {ChildrenNodes[4].Add(ChildrenIn[i]); continue;}//4
            if(Position.x >= Axis.x && Position.y < Axis.y && Position.z >= Axis.z) {ChildrenNodes[5].Add(ChildrenIn[i]); continue;}//3
            if(Position.x >= Axis.x && Position.y >= Axis.y && Position.z >= Axis.z) {ChildrenNodes[6].Add(ChildrenIn[i]); continue;}//1
            if(Position.x < Axis.x && Position.y >= Axis.y && Position.z >= Axis.z) {ChildrenNodes[7].Add(ChildrenIn[i]); continue;}//5
        }


        for(int i = 0; i < 8; i++) {
            if(ChildrenNodes[i].Count <= 1 || CurDepth > MaxDepth) {
                TempNode.IsChild[i] = true;
                TempNode.ChildNode[i] = (ChildrenNodes[i].Count == 0 || CurDepth > MaxDepth) ? -1 : ChildrenNodes[i][0].InArrayIndex;
            } else {
                TempNode.IsChild[i] = false;
                RecursiveBuild(ChildrenNodes[i].ToArray(), new Vector3((int)Max[i].x, (int)Max[i].y, (int)Max[i].z), new Vector3((int)Min[i].x, (int)Min[i].y, (int)Min[i].z), CurrentCount - 1, i, CurDepth + 1, ref TempNode);
            }
        }
        TempNode.InArrayIndex = CurrentCount - 2;
        Octree[CurrentCount - 1] = TempNode;
    }

    Vector3 Decode(int Index, int Size) {
    Vector3 location;
    location.x = (float)Index % Size;
    location.y = (float)(((Index - location.x) / Size) % Size);
    location.z = (float)(((Index - location.x - Size * location.y) / (Size * Size)));
    return location;
}


    public List<OctreeNode> OrderedList;
    public int node_count;
    public int index_count;

    public void NaiveConstruct(Voxel[] Voxels, int LargestAxis, Vector3 OrigionalSize) {
        TopVox = Voxels;
        OrderedVoxels = new List<Voxel>();
        Size = OrigionalSize;
        MaxRecursions = 0;
        Octree = new List<OctreeNode>();
        OrigOrigSize = OrigionalSize;
        OctreeNode TempNode = new OctreeNode();
        OrigionalSize = new Vector3(LargestAxis,LargestAxis,LargestAxis);
        Octree.Add(TempNode);
        RecursiveBuild(Voxels, OrigionalSize, new Vector3(0,0,0), 0, 0, 0, ref TempNode);
        Octree.RemoveAt(0);

        for(int i = 0; i < Octree.Count; i++) {//Ordering the voxels
            var TempVox = Octree[i];
            for(int i2 = 0; i2 < 8; i2++) {
                if(TempVox.IsChild[i2] && TempVox.ChildNode[i2] != -1) {
                    OrderedVoxels.Add(TopVox[TempVox.ChildNode[i2]]);
                    TempVox.ChildNode[i2] = OrderedVoxels.Count - 1;
                }
            }
            Octree[i] = TempVox;
        }

        OrderedList = new List<OctreeNode>();
        List<OctreeNode> WorkGroup = new List<OctreeNode>();
        List<int> OldIndex = new List<int>();
        OctreeNode TempOrderedNode;
        WorkGroup.Add(Octree[0]);
        OrderedList.Add(Octree[0]);
        OldIndex.Add(0); int Reps = 0;
        OctreeNode[] OrderedOctree = new OctreeNode[Octree.Count];
        while(WorkGroup.Count != 0 && Reps < 2400000) {//Re-organizing the octree so that the 8 child node come after the parent node every time
            Reps++;
            TempNode = WorkGroup[0];
            int Index = OldIndex[0];
            WorkGroup.RemoveAt(0);
            OldIndex.RemoveAt(0);
            for(int i = 0; i < 8; i++) {
                if(!TempNode.IsChild[i] && !(TempNode.ChildNode[i] == -1)) {
                    OrderedList.Add(Octree[TempNode.ChildNode[i]]);
                    OldIndex.Add(OrderedList.Count - 1);
                    WorkGroup.Add(Octree[TempNode.ChildNode[i]]);
                    TempNode.ChildNode[i] = OrderedList.Count - 1;
                }
            }
            OrderedList[Index] = TempNode;

        }

        Octree = new List<OctreeNode>(OrderedList);
        OrderedList.Clear();

        OrigionalSize += new Vector3(1,1,1);
        CompressedOctree = new CompressedOctreeNode[Octree.Count];
        OctreeNode CurOctreeNode;
        for(int i = 0; i < CompressedOctree.Length; i++) {//Compressing the Octree Stage 1
            CurOctreeNode = Octree[i];
            int node_triangle_count = 0;
            int node_internal_count = 0;
            CompressedOctree[i].base_index_child = (uint)node_count;
            int index = 99999999;
            for(int i2 = 0; i2 < 8; i2++) {
                if(CurOctreeNode.IsChild[i2] && CurOctreeNode.ChildNode[i2] != -1) {
                    index = Mathf.Min(index, CurOctreeNode.ChildNode[i2]);
                }
            }
            CompressedOctree[i].base_index_triangle = (uint)((index == 99999999) ? 0 : index);
            CompressedOctree[i].Max = (uint)(CurOctreeNode.BBMax.x + OrigionalSize.x * CurOctreeNode.BBMax.y + OrigionalSize.x * OrigionalSize.y * CurOctreeNode.BBMax.z);
            CompressedOctree[i].Min = (uint)(CurOctreeNode.BBMin.x + OrigionalSize.x * CurOctreeNode.BBMin.y + OrigionalSize.x * OrigionalSize.y * CurOctreeNode.BBMin.z);
            for(int i2 = 0; i2 < 8; i2++) {
                if(CurOctreeNode.ChildNode[i2] != -1) {
                    if(CurOctreeNode.IsChild[i2]) {
                        CompressedOctree[i].meta[i2] |= System.Convert.ToByte(1 << (0 + 5));
                        CompressedOctree[i].meta[i2] |= System.Convert.ToByte(node_triangle_count);
                        index_count++;
                        node_triangle_count++;
                    } else {
                        CompressedOctree[i].meta[i2] = System.Convert.ToByte((node_internal_count + 24) | 0b00100000);
                        CompressedOctree[i].imask |= System.Convert.ToByte(1 << node_internal_count);
                        node_internal_count++;
                        node_count++;
                    }
                } else {
                    CompressedOctree[i].meta[i2] = System.Convert.ToByte((uint)255);
                }
            }
        }
    }
}
