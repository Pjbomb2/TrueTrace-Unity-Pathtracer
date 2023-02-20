using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using UnityEditor;
using System.IO;
using CommonVars;
#pragma warning disable 1998

namespace TrueTrace {
    [System.Serializable]
    public class VoxelObject : MonoBehaviour
    {
        public AABB aabb;
        public AABB aabb_untransformed;
        public List<Voxel> Voxels;
        public Vector3 Size;
        public List<MaterialData> _Materials;
        public string Name;

        public bool HasCompleted;
        public Object VoxelRef;
        public string CachedString;
        private VoxLoader Vox;
        public BrickMapBuilder Builder;
        public void init()
        {
            Voxels = new List<Voxel>();
            _Materials = new List<MaterialData>();
        }
        public int LargestAxis;

        public void ClearAll()
        {
            Voxels = null;
            _Materials = null;
            Builder.OrderedVoxels = null;
            Builder.BrickMap = null;
        }

        unsafe public void LoadData()
        {
            Builder = new BrickMapBuilder();
            HasCompleted = false;
            init();
            Name = VoxelRef.name;
    #if UNITY_EDITOR
            CachedString = AssetDatabase.GetAssetPath(VoxelRef);
    #endif
            Vox = new VoxLoader(CachedString);

        }

        public Vector3 BBMin;
        public Vector3 BBMax;
        public List<int> BrickmapTraverse;
        unsafe public async void BuildOctree()
        {
            BBMax = new Vector3(-9999.0f, -9999.0f, -9999.0f);
            BBMin = new Vector3(9999.0f, 9999.0f, 9999.0f);
            for (int i = 0; i < Vox.VoxelObjects.Count; i++)
            {
                BBMax = Vector3.Max(BBMax, Vox.VoxelObjects[i].Size / 2.0f + Vox.VoxelObjects[i].Translation);
                BBMin = Vector3.Min(BBMin, -Vox.VoxelObjects[i].Size / 2.0f + Vox.VoxelObjects[i].Translation);
            }
            Vector3 GlobalOffset = BBMin;
            BBMax -= BBMin;
            BBMin -= BBMin;
            BBMax = new Vector3(Mathf.Ceil(BBMax.x), Mathf.Ceil(BBMax.y), Mathf.Ceil(BBMax.z));
            Size = BBMax - BBMin;
            Voxels = new List<Voxel>();
            bool[] Occupied = new bool[(uint)((uint)Size.x * (uint)Size.y * (uint)Size.z)];
            List<int> Materials = new List<int>();
            Size = BBMax - BBMin;
            BBMax = Size;
            //  Debug.Log("A");
            for (int i = 0; i < Vox.VoxelObjects.Count; i++)
            {
                Vector3 Temp = Vox.VoxelObjects[i].Size / 2.0f - (new Vector3(((Vox.VoxelObjects[i].Size.x % 2 == 1) ? 1 : 0.5f), ((Vox.VoxelObjects[i].Size.y % 2 == 1) ? 1 : 0.5f), ((Vox.VoxelObjects[i].Size.z % 2 == 1) ? 1 : 0.5f)));
                Matrix4x4 rotation = Vox.VoxelObjects[i].Rotation;
                uint VoxLength = (uint)Vox.VoxelObjects[i].Size.x * (uint)Vox.VoxelObjects[i].Size.y * (uint)Vox.VoxelObjects[i].Size.z;
                Vector3 Translation = Vox.VoxelObjects[i].Translation - new Vector3(1, 1, 1);
                Vector3 ThisSize = Vox.VoxelObjects[i].Size;
                var Colors = Vox.VoxelObjects[i].colors;
                for (uint i2 = 0; i2 < VoxLength; i2++)
                {
                    if (Colors[i2] != 0)
                    {
                        Vector3 location = new Vector3(0, 0, 0);
                        location.x = (float)((uint)i2 % (uint)ThisSize.x);
                        location.y = (float)((((uint)i2 - (uint)location.x) / (uint)ThisSize.x) % (uint)ThisSize.y);
                        location.z = (float)((((uint)i2 - (uint)location.x - (uint)ThisSize.x * (uint)location.y) / ((uint)ThisSize.y * (uint)ThisSize.x)));
                        location -= Temp;
                        location = rotation * location;
                        location += Translation - GlobalOffset;
                        location = new Vector3(Mathf.Ceil(location.x), Mathf.Ceil(location.y), Mathf.Ceil(location.z));
                        uint Index = (uint)((uint)location.x + (uint)Size.x * (uint)location.y + (uint)Size.x * (uint)Size.y * (uint)location.z);
                        try
                        {
                            if (!Occupied[Index])
                            {
                                Occupied[Index] = true;
                                if (!Materials.Contains(Colors[i2]))
                                {
                                    Materials.Add((int)Colors[i2]);
                                }
                                Voxels.Add(new Voxel()
                                {
                                    Index = Index,
                                    Material = Materials.IndexOf((int)Colors[i2]),
                                });
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError("Overall Model Too Big, Please split into multiple files: " + e);
                            return;
                        }
                    }
                }
            }
            aabb_untransformed = new AABB();
            for (int i = 0; i < Materials.Count; i++)
            {
                Vector3 BaseColor = new Vector3(Vox.palette[Materials[i]].r, Vox.palette[Materials[i]].g, Vox.palette[Materials[i]].b);
                if (BaseColor.Equals(new Vector3(0, 0, 0))) BaseColor = new Vector3(0.1f, 0.1f, 0.1f);
                _Materials.Add(new MaterialData()
                {
                    BaseColor = BaseColor,
                    MatType = 0
                });
            }
            LargestAxis = (int)Mathf.Max(Mathf.Max(Size.x, Size.y), Size.z);
            for (int i = 0; i < Voxels.Count; i++)
            {
                var TempVox = Voxels[i];
                TempVox.InArrayIndex = i;
                Voxels[i] = TempVox;
            }
            int A = 1;
            LargestAxis = (int)Mathf.Max(Mathf.Max(Size.x, Size.y), Size.z);
            while (A < LargestAxis)
            {
                A *= 2;
            }
            Builder.NaiveConstruct(Voxels.ToArray(), A, Size);
            aabb_untransformed.BBMax = new Vector3(Builder.FinalSize, Builder.FinalSize, Builder.FinalSize);
            aabb_untransformed.BBMin = new Vector3(0, 0, 0);
            BrickmapTraverse = new List<int>();
            for (int i = 0; i < Builder.BrickMap.Count; i++)
            {
                BrickmapTraverse.Add(Builder.BrickMap[i].StartingIndex);
            }
            for (int i = 0; i < Builder.OrderedVoxels.Count; i++)
            {
                BrickmapTraverse.Add(-Builder.OrderedVoxels[i] - 2);
            }

            LargestAxis = A;
            HasCompleted = true;
            Debug.Log("Voxel Object " + Name + " Completed With " + Builder.OrderedVoxels.Count + " Voxels With Depth Of " + Builder.TotalDepth);
        }


        public void UpdateAABB()
        {//Update the Transformed AABB by getting the new Max/Min of the untransformed AABB after transforming it
            Vector3 center = 0.5f * (aabb_untransformed.BBMin + aabb_untransformed.BBMax);
            Vector3 extent = 0.5f * (aabb_untransformed.BBMax - aabb_untransformed.BBMin);
            Matrix4x4 Mat = this.transform.localToWorldMatrix;
            Vector3 new_center = CommonFunctions.transform_position(Mat, center);
            CommonFunctions.abs(ref Mat);
            Vector3 new_extent = CommonFunctions.transform_direction(Mat, extent);

            aabb.BBMin = new_center - new_extent;
            aabb.BBMax = new_center + new_extent;
        }

        private void OnEnable()
        {
            if (gameObject.scene.isLoaded)
            {
                LoadData();
                this.GetComponentInParent<AssetManager>().VoxelAddQue.Add(this);
                this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
            }
        }

        private void OnDisable()
        {
            if (gameObject.scene.isLoaded)
            {
                Debug.Log("EEE");
                this.GetComponentInParent<AssetManager>().VoxelRemoveQue.Add(this);
                this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
            }
        }


        private Vector3 GetPosition2(uint Index)
        {
            Vector3 location = new Vector3(0, 0, 0);
            location.x = Mathf.Floor((float)(Index % (uint)8));
            location.y = Mathf.Floor((float)(((Index - (uint)location.x) / (uint)8) % (uint)8));
            location.z = Mathf.Floor(((float)(((Index - (uint)location.x - (uint)8 * (uint)location.y) / ((uint)8 * (uint)8)))));
            return location;
        }


    }
}