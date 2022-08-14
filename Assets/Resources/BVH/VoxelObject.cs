using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using UnityEditor;
using System.IO;
using CommonVars;

[System.Serializable]
public class VoxelObject : MonoBehaviour
{
    public AABB aabb;
    public AABB aabb_untransformed;
    public List<Voxel> Voxels;
    public Vector3 Size;
    public List<MaterialData> _Materials;
    public string Name;

    public OctreeBuilder Octree;

    public GPUOctreeNode[] GPUOctree;
    public GPUVoxel[] GPUVoxels;
    public bool HasCompleted;
    public Object VoxelRef;
    public string CachedString;
    private VoxLoader Vox;

    public void init() {
        Voxels = new List<Voxel>();
        _Materials = new List<MaterialData>();
    }
    public int LargestAxis;

    uint Encode(uint b1,uint b2,uint b3,uint b4) {
            return ((uint)b1      ) |
                   ((uint)b2 <<  8) |
                   ((uint)b3 << 16) |
                   ((uint)b4 << 24);
    }

    unsafe public void LoadData() {
        HasCompleted = false;
        Octree = new OctreeBuilder();
        init();
        Name = VoxelRef.name;
        #if UNITY_EDITOR
        CachedString = AssetDatabase.GetAssetPath(VoxelRef);
        #endif
        Vox = new VoxLoader(CachedString);
       
    }
    public int MaxRecur = 0;

    private Vector3 GetPosition(uint Index) {
        Vector3 location = new Vector3(0,0,0);
        location.x = Mathf.Floor((float)(Index % (uint)Size.x));
        location.y = Mathf.Floor((float)(((Index - (uint)location.x) / (uint)Size.x) % (uint)Size.y));
        location.z = Mathf.Floor(((float)(((Index - (uint)location.x - (uint)Size.x * (uint)location.y) / ((uint)Size.y * (uint)Size.x)))));
        return location;
    }
public Vector3 BBMin;
public Vector3 BBMax;
    unsafe public async void BuildOctree() {
        BBMax = new Vector3(-9999.0f,-9999.0f,-9999.0f);
        BBMin = new Vector3(9999.0f,9999.0f,9999.0f);
        for(int i = 0; i < Vox.VoxelObjects.Count; i++) {
            BBMax = Vector3.Max(BBMax, Vox.VoxelObjects[i].Size / 2.0f + Vox.VoxelObjects[i].Translation);
            BBMin = Vector3.Min(BBMin, -Vox.VoxelObjects[i].Size / 2.0f + Vox.VoxelObjects[i].Translation);
        }
        Vector3 GlobalOffset = BBMin;
        BBMax -= BBMin;
        BBMin -= BBMin;
        BBMax = new Vector3(Mathf.Ceil(BBMax.x),Mathf.Ceil(BBMax.y),Mathf.Ceil(BBMax.z));
        Size = BBMax - BBMin;
        Voxels = new List<Voxel>();
        bool[] Occupied = new bool[(uint)((uint)Size.x * (uint)Size.y * (uint)Size.z)];
        int MaterialCount = 0;
        List<int> Materials = new List<int>();
        int VoxCount = 0;
        Size = BBMax - BBMin;
        BBMax = Size;
        for(int i = 0; i < Vox.VoxelObjects.Count; i++) {
            Vector3 Temp = Vox.VoxelObjects[i].Size / 2.0f - (new Vector3(((Vox.VoxelObjects[i].Size.x % 2 == 1) ? 1 : 0.5f),((Vox.VoxelObjects[i].Size.y % 2 == 1) ? 1 : 0.5f),((Vox.VoxelObjects[i].Size.z % 2 == 1) ? 1 : 0.5f)));
            Matrix4x4 rotation = Vox.VoxelObjects[i].Rotation;
            uint VoxLength = (uint)Vox.VoxelObjects[i].Size.x * (uint)Vox.VoxelObjects[i].Size.y * (uint)Vox.VoxelObjects[i].Size.z;
            for(uint i2 = 0; i2 < VoxLength; i2++) {
                if(Vox.VoxelObjects[i].colors[i2] != 0) {
                    Vector3 location = new Vector3(0,0,0);
                    location.x = (float)((uint)i2 % (uint)Vox.VoxelObjects[i].Size.x);
                    location.y = (float)((((uint)i2 - (uint)location.x) / (uint)Vox.VoxelObjects[i].Size.x) % (uint)Vox.VoxelObjects[i].Size.y);
                    location.z = (float)((((uint)i2 - (uint)location.x - (uint)Vox.VoxelObjects[i].Size.x * (uint)location.y) / ((uint)Vox.VoxelObjects[i].Size.y * (uint)Vox.VoxelObjects[i].Size.x)));
                    location -= Temp;
                    location = rotation * location;
                    location += Vox.VoxelObjects[i].Translation - GlobalOffset;
                    uint Index = (uint)((uint)location.x + (uint)Size.x * (uint)location.y + (uint)Size.x * (uint)Size.y * (uint)location.z);
                    try{
                        if(!Occupied[Index]) {
                            Occupied[Index] = true;
                            if(!Materials.Contains(Vox.VoxelObjects[i].colors[i2])) {
                                Materials.Add((int)Vox.VoxelObjects[i].colors[i2]);
                            }
                            Voxels.Add(new Voxel() {
                                Index = Index,
                                Material = Materials.IndexOf((int)Vox.VoxelObjects[i].colors[i2]),
                            });
                        }
                    }catch(System.Exception e) {
                        Debug.LogError("Overall Model Too Big, Please split into multiple models");
                        return;
                    }
                }
            }
        }
        Debug.Log(Voxels.Count + " Voxels Loaded");
        aabb_untransformed = new AABB();
        aabb_untransformed.BBMax = Size;
        aabb_untransformed.BBMin = new Vector3(0,0,0);

        for(int i = 0; i < Materials.Count; i++) {
            Vector3 BaseColor = new Vector3(Vox.palette[Materials[i]].r, Vox.palette[Materials[i]].g, Vox.palette[Materials[i]].b);
            if(BaseColor.Equals(new Vector3(0,0,0))) BaseColor = new Vector3(0.1f,0.1f,0.1f);
            _Materials.Add(new MaterialData() {
                BaseColor = BaseColor,
                MatType = 0,
                eta = new Vector3(0,0,0)
                });
        }
        LargestAxis = (int)Mathf.Max(Mathf.Max(Size.x, Size.y), Size.z);
        for(int i = 0; i < Voxels.Count; i++) {
            var TempVox = Voxels[i];
            TempVox.InArrayIndex = i;
            Voxels[i] = TempVox;
        }
        int A = 1;
        LargestAxis =(int)Mathf.Max(Mathf.Max(Size.x, Size.y), Size.z);
        while(A < LargestAxis) {
            A *= 2;
        }
        Octree.NaiveConstruct(Voxels.ToArray(), A, Size);
        GPUOctree = new GPUOctreeNode[Octree.CompressedOctree.Length + Voxels.Count];
        GPUVoxels = new GPUVoxel[Octree.OrderedVoxels.Count];
        GPUVoxel TempVoxel = new GPUVoxel();
        for(int i = 0; i < GPUVoxels.Length; i++) {
            GPUVoxels[i].Index = (int)Octree.OrderedVoxels[i].Index;
            GPUVoxels[i].Material = Octree.OrderedVoxels[i].Material;
        }

        GPUOctreeNode TempBVHNode = new GPUOctreeNode();
        for(int i = 0; i < Octree.CompressedOctree.Length; ++i) {//Could I store the entire voxel inside the first node? I dont need to send it then, if its just a material index
            CompressedOctreeNode TempNode = Octree.CompressedOctree[i];
            TempBVHNode.node_1x = TempNode.base_index_child;
            TempBVHNode.node_1y = TempNode.base_index_triangle + (uint)Octree.CompressedOctree.Length;
            TempBVHNode.Meta1 = Encode(TempNode.meta[0], TempNode.meta[1], TempNode.meta[2], TempNode.meta[3]);
            TempBVHNode.Meta2 = Encode(TempNode.meta[4], TempNode.meta[5], TempNode.meta[6], TempNode.meta[7]);
            TempBVHNode.Center = Octree.Octree[i].Center;
            GPUOctree[i] = TempBVHNode;
        }
        int OctreeLength = Octree.CompressedOctree.Length;
        int VoxelLength = OctreeLength + Voxels.Count;
        for(int i  = OctreeLength; i < VoxelLength; i++) {
            GPUOctree[i].node_1x = (uint)Octree.OrderedVoxels[i - OctreeLength].Material;
            GPUOctree[i].Center = GetPosition(Octree.OrderedVoxels[i - OctreeLength].Index);
        }
        LargestAxis = A;
        HasCompleted = true;
        Debug.Log("Voxel Object " + Name + " Completed With " + Voxels.Count + " Voxels With Depth Of " + Octree.TotalDepth);
    }


    public void UpdateAABB() {//Update the Transformed AABB by getting the new Max/Min of the untransformed AABB after transforming it
        Vector3 center = 0.5f * (aabb_untransformed.BBMin + aabb_untransformed.BBMax);
        Vector3 extent = 0.5f * (aabb_untransformed.BBMax - aabb_untransformed.BBMin);

        Vector3 new_center = CommonFunctions.transform_position (this.transform.worldToLocalMatrix.inverse, center);
        Vector3 new_extent = CommonFunctions.transform_direction(CommonFunctions.abs(this.transform.worldToLocalMatrix.inverse), extent);

        aabb.BBMin = new_center - new_extent;
        aabb.BBMax = new_center + new_extent;
    }

    private void OnEnable() {
        if(gameObject.scene.isLoaded) {
            LoadData();
            this.GetComponentInParent<AssetManager>().VoxelAddQue.Add(this);
            this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
        }
    }

    private void OnDisable() {
        if(gameObject.scene.isLoaded) {
            Debug.Log("EEE");
            this.GetComponentInParent<AssetManager>().VoxelRemoveQue.Add(this);
            this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
        }
    }

 

}
