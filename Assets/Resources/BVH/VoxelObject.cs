using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
 using UnityEditor;
 using System.IO;
 using CommonVars;
using CommonFunctions;

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

VoxLoader Vox;
    unsafe public void LoadData() {
        Debug.Log("VoxelStart");
        HasCompleted = false;
        Octree = new OctreeBuilder();
        init();
        Name = VoxelRef.name;
        Vox = new VoxLoader("Object");//AssetDatabase.GetAssetPath(VoxelRef));
       
        //12 minutes
    }
    public int MaxRecur = 0;

    private Vector3 GetPosition(int Index) {
        Vector3 location = new Vector3(0,0,0);
        location.x = (float)Index % Size.x;
        location.y = (float)(((Index - location.x) / Size.x) % Size.y);
        location.z = (float)(((Index - location.x - Size.x * location.y) / (Size.y * Size.x)));
        return location;
    }

    unsafe public async void BuildOctree() {
        Voxels = new List<Voxel>();
        for(int i = 0; i < Vox.parts.Count; i++) {
            Size = Vox.parts[i].size;
            for(int i2 = 0; i2 < Vox.parts[i].voxels.Count; i2++) {
                    Voxels.Add(new Voxel() {
                        Index = (int)(Vox.parts[i].voxels[i2].x + Size.x * Vox.parts[i].voxels[i2].y + Size.x * Size.y * Vox.parts[i].voxels[i2].z),
                        Material = (int)Vox.parts[i].voxels[i2].w
                    });
            }
        }
        Debug.Log(Voxels.Count + " Voxels Loaded");
        aabb_untransformed = new AABB();
        aabb_untransformed.BBMax = Size;
        aabb_untransformed.BBMin = new Vector3(0,0,0);

        for(int i = 0; i < Vox.MaterialCount; i++) {
            if(Vox.palette[Vox.CurrentMaterials[i]].x == 15 && Vox.palette[Vox.CurrentMaterials[i]].y == 169 && Vox.palette[Vox.CurrentMaterials[i]].z == 189) Debug.Log("EEEEEEE: " + i);
            Vector3 BaseColor = (Vector3)Vox.palette[Vox.CurrentMaterials[i]] / 255.0f;
            if(BaseColor.Equals(new Vector3(0,0,0))) BaseColor = new Vector3(0.1f,0.1f,0.1f);
            _Materials.Add(new MaterialData() {
                BaseColor = BaseColor,
                MatType = (Vox.palette[Vox.CurrentMaterials[i]].w != 255) ? 2 : 0,
                eta = (Vox.palette[Vox.CurrentMaterials[i]].w != 255) ? new Vector3(1.33f,0,0) : new Vector3(0,0,0)
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
        GPUOctree = new GPUOctreeNode[Octree.CompressedOctree.Length + Octree.OrderedVoxels.Count];
        GPUVoxels = new GPUVoxel[Octree.OrderedVoxels.Count];

        GPUVoxel TempVoxel = new GPUVoxel();
        for(int i = 0; i < GPUVoxels.Length; i++) {
            GPUVoxels[i].Index = Octree.OrderedVoxels[i].Index;
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
        for(int i  = Octree.CompressedOctree.Length; i < Octree.CompressedOctree.Length + Voxels.Count; i++) {
            GPUOctree[i].node_1x = (uint)Octree.OrderedVoxels[i - Octree.CompressedOctree.Length].Material;
            GPUOctree[i].Center = GetPosition(Octree.OrderedVoxels[i - Octree.CompressedOctree.Length].Index);
        }
        LargestAxis = A;
        HasCompleted = true;
        Debug.Log("Voxel Object " + Name + " Completed With " + Voxels.Count + " Voxels With Depth Of " + Octree.TotalDepth);
    }

private Vector3 transform_position(Matrix4x4 matrix, Vector3 position) {
    return new Vector3(
        matrix[0, 0] * position.x + matrix[0, 1] * position.y + matrix[0, 2] * position.z + matrix[0, 3],
        matrix[1, 0] * position.x + matrix[1, 1] * position.y + matrix[1, 2] * position.z + matrix[1, 3],
        matrix[2, 0] * position.x + matrix[2, 1] * position.y + matrix[2, 2] * position.z + matrix[2, 3]
    );
}
private Vector3 transform_direction(Matrix4x4 matrix, Vector3 direction) {
    return new Vector3(
        matrix[0, 0] * direction.x + matrix[0, 1] * direction.y + matrix[0, 2] * direction.z,
        matrix[1, 0] * direction.x + matrix[1, 1] * direction.y + matrix[1, 2] * direction.z,
        matrix[2, 0] * direction.x + matrix[2, 1] * direction.y + matrix[2, 2] * direction.z
    );
}
private Matrix4x4 abs(Matrix4x4 matrix) {
    Matrix4x4 result = new Matrix4x4();
    for (int i = 0; i < 4; i++) {
        for (int i2 = 0; i2 < 4; i2++) result[i,i2] = Mathf.Abs(matrix[i,i2]);
    }
    return result;
}


public void UpdateAABB() {//Update the Transformed AABB by getting the new Max/Min of the untransformed AABB after transforming it
    Vector3 center = 0.5f * (aabb_untransformed.BBMin + aabb_untransformed.BBMax);
    Vector3 extent = 0.5f * (aabb_untransformed.BBMax - aabb_untransformed.BBMin);

    Vector3 new_center = transform_position (this.transform.worldToLocalMatrix.inverse, center);
    Vector3 new_extent = transform_direction(abs(this.transform.worldToLocalMatrix.inverse), extent);

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
