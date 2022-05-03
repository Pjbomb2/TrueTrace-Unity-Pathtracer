using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;

[System.Serializable]
public class AssetManager : MonoBehaviour {
    public Texture2D AlbedoAtlas;
    public Texture2D NormalAtlas;
    public Texture2D EmissiveAtlas;

    [HideInInspector] public List<MaterialData> _Materials;
    [HideInInspector] public List<CudaTriangle> aggregated_triangles;   
    [HideInInspector] public List<BVHNode8DataCompressed> BVH8Aggregated;
    [HideInInspector] public List<MyMeshDataCompacted> MyMeshesCompacted;
    [HideInInspector] public List<CudaLightTriangle> AggLightTriangles;
    [HideInInspector] public List<LightData> UnityLights;

    public List<ParentObject> RenderQue;
    public List<ParentObject> BuildQue;
    public List<ParentObject> AddQue;
    public List<ParentObject> RemoveQue;

    [HideInInspector] public List<Transform> LightTransforms;
    [HideInInspector] public List<Task> CurrentlyActiveTasks;

    [HideInInspector] public List<LightMeshData> LightMeshes;

    public AABB[] MeshAABBs;

    public bool ParentCountHasChanged;    
    private bool NeedsToUpdate = false;
    private bool ActuallyNeedsToUpdate = false;
    
    public int TLASSpace;
    public int LightTriCount;
    public int LightMeshCount;
    public int UnityLightCount;
    private int PrevLightCount;

    public BVH2Builder BVH2;

    public void ClearAll() {//My attempt at clearing memory
        ParentObject[] ChildrenObjects = this.GetComponentsInChildren<ParentObject>();
        foreach(ParentObject obj in ChildrenObjects)
            obj.ClearAll();
        _Materials.Clear();
        _Materials.TrimExcess();
        LightTransforms.Clear();
        LightTransforms.TrimExcess();
        LightMeshes.Clear();
        LightMeshes.TrimExcess();
        aggregated_triangles.Clear();
        aggregated_triangles.TrimExcess();
        BVH8Aggregated.Clear();
        BVH8Aggregated.TrimExcess();
        MyMeshesCompacted.Clear();
        MyMeshesCompacted.TrimExcess();
        AggLightTriangles.Clear();
        AggLightTriangles.TrimExcess();
        UnityLights.Clear();
        UnityLights.TrimExcess();
        DestroyImmediate(AlbedoAtlas);
        DestroyImmediate(NormalAtlas);
        DestroyImmediate(EmissiveAtlas);  
        NeedsToUpdate = true;
        ActuallyNeedsToUpdate = true;
    }



    private Vector4 ResizeRect(Vector4 In, Rect InRect) {
        Vector2 Difference = new Vector2(InRect.xMax - InRect.xMin, InRect.yMax - InRect.yMin);
        Vector2 Max = new Vector2(In.x, In.y) * Difference + new Vector2(InRect.xMin, InRect.yMin);
        Vector2 Min = new Vector2(In.z, In.w) * Difference + new Vector2(InRect.xMin, InRect.yMin);
        return new Vector4(Max.x, Max.y, Min.x, Min.y);
    }
    private void CreateAtlas() {//Creates texture atlas
        _Materials = new List<MaterialData>();
        List<Texture2D> AlbedoTexs = new List<Texture2D>();
        List<Texture2D> NormalTexs = new List<Texture2D>();
        List<Texture2D> EmissiveTexs = new List<Texture2D>();
        AlbedoAtlas = null;
        NormalAtlas = null;
        EmissiveAtlas = null;
        int AlbedoCount, NormalCount, EmissiveCount = 0;
        foreach(ParentObject Obj in RenderQue) {
            if(Obj.HasAlbedoAtlas) {
                AlbedoTexs.Add((Texture2D)Obj.AlbedoAtlas);
            }
            if(Obj.HasNormalAtlas) {
                NormalTexs.Add((Texture2D)Obj.NormalAtlas);
            }
            if(Obj.HasEmissiveAtlas) {
                EmissiveTexs.Add((Texture2D)Obj.EmissiveAtlas);
            }
        }
        Rect[] AlbedoRects, NormalRects, EmmissiveRects;
        if(AlbedoTexs.Count != 0) {
            AlbedoAtlas = new Texture2D(8192, 8192);
            AlbedoRects = AlbedoAtlas.PackTextures(AlbedoTexs.ToArray(), 2, 8192);
        } else {
            AlbedoAtlas = new Texture2D(1,1);
            AlbedoRects = new Rect[0];
        }
        if(NormalTexs.Count != 0) {
            NormalAtlas = new Texture2D(8192, 8192);
            NormalRects = NormalAtlas.PackTextures(NormalTexs.ToArray(), 2, 8192);
        } else {
            NormalAtlas = new Texture2D(1,1);
            NormalRects = new Rect[0];
        }
        if(EmissiveTexs.Count != 0) {
            EmissiveAtlas = new Texture2D(2048, 2048);
            EmmissiveRects = EmissiveAtlas.PackTextures(EmissiveTexs.ToArray(), 2, 2048);
        } else {
            EmissiveAtlas = new Texture2D(1,1);
            EmmissiveRects = new Rect[0];
        }
        AlbedoCount = NormalCount = EmissiveCount = 0;
         int MatCount = 0;
        foreach(ParentObject Obj in RenderQue) {
            foreach(RayTracingObject Obj2 in Obj.ChildObjects) {
                for(int i = 0; i < Obj2.MaterialIndex.Length; i++) {
                    Obj2.MaterialIndex[i] = Obj2.LocalMaterialIndex[i] + MatCount;
                }
            }
            foreach(MaterialData Mat in Obj._Materials) {
                MaterialData TempMat = Mat;
                if(TempMat.MatType == 1) {
                    TempMat.Roughness = Mathf.Max(TempMat.Roughness, 0.000001f);
                }
                if(TempMat.HasAlbedoTex == 1) {
                    TempMat.AlbedoTex = ResizeRect(TempMat.AlbedoTex, AlbedoRects[AlbedoCount]);
                }
                if(TempMat.HasNormalTex == 1) {
                    TempMat.NormalTex = ResizeRect(TempMat.NormalTex, NormalRects[NormalCount]);
                }
                if(TempMat.HasEmissiveTex == 1) {
                    TempMat.EmissiveTex = ResizeRect(TempMat.EmissiveTex, EmmissiveRects[EmissiveCount]);
                }
                _Materials.Add(TempMat);
                MatCount++;
            }
            if(Obj.HasAlbedoAtlas) AlbedoCount++;
            if(Obj.HasNormalAtlas) NormalCount++;
            if(Obj.HasEmissiveAtlas) EmissiveCount++;

        }
        AlbedoTexs.Clear();
        AlbedoTexs.TrimExcess();
        NormalTexs.Clear();
        NormalTexs.TrimExcess();
        EmissiveTexs.Clear();
        EmissiveTexs.TrimExcess();       
    }

    private void init() {
        MyMeshesCompacted = new List<MyMeshDataCompacted>();
        aggregated_triangles = new List<CudaTriangle>();
        BVH8Aggregated = new List<BVHNode8DataCompressed>();
        AggLightTriangles = new List<CudaLightTriangle>();
        CurrentlyActiveTasks = new List<Task>();
        UnityLights = new List<LightData>();
        LightMeshes = new List<LightMeshData>();
        LightTransforms = new List<Transform>();
        LightMeshCount = 0;
        UnityLightCount = 0;
        LightTriCount = 0;
    }

    private void UpdateRenderAndBuildQues() {
        NeedsToUpdate = false;
        int RenderQueCount = RenderQue.Count;
        for(int i = RenderQueCount - 1; i >= 0; i--) {//Demotes from Render Que to Build Que in case mesh has changed
            if(RenderQue[i].MeshCountChanged) {
                RenderQue[i].LoadData();
                BuildQue.Add(RenderQue[i]);
                var TempBuildQueCount = BuildQue.Count - 1;
                Task t1 = Task.Run(() => BuildQue[TempBuildQueCount].BuildTotal());
                CurrentlyActiveTasks.Add(t1);
                RenderQue.RemoveAt(i);
                NeedsToUpdate = true;
            }
        }
        int BuildQueCount = BuildQue.Count;
        for(int i = BuildQueCount - 1; i >= 0; i--) {//Promotes from Build Que to Render Que
            if(CurrentlyActiveTasks[i].IsFaulted) {//Fuck, something fucked up
                Debug.Log(CurrentlyActiveTasks[i].Exception);
            }
            if(CurrentlyActiveTasks[i].Status == TaskStatus.RanToCompletion) {
                RenderQue.Add(BuildQue[i]);
                CurrentlyActiveTasks.RemoveAt(i);
                BuildQue.RemoveAt(i);
                NeedsToUpdate = true;
            }
        }
        if(NeedsToUpdate) {
            MeshAABBs = new AABB[RenderQue.Count];
        }
    }


    public void EditorBuild() {
        init();
        AddQue = new List<ParentObject>();
        RemoveQue = new List<ParentObject>();
        RenderQue = new List<ParentObject>();
        CurrentlyActiveTasks = new List<Task>();
        BuildQue = new List<ParentObject>(GetComponentsInChildren<ParentObject>());
        for(int i = 0; i < BuildQue.Count; i++) {
            var CurrentRep = i;
            BuildQue[CurrentRep].LoadData();
            Task t1 = Task.Run(() => BuildQue[CurrentRep].BuildTotal());
            CurrentlyActiveTasks.Add(t1);
        }
        if(AggLightTriangles.Count == 0) {AggLightTriangles.Add(new CudaLightTriangle() {});}
    }

    public void BuildCombined() {
        init();
        AddQue = new List<ParentObject>();
        RemoveQue = new List<ParentObject>();
        RenderQue = new List<ParentObject>();
        CurrentlyActiveTasks = new List<Task>();
        BuildQue = new List<ParentObject>();
        List<ParentObject> TempQue = new List<ParentObject>(GetComponentsInChildren<ParentObject>());
        for(int i = 0; i < TempQue.Count; i++) {
            if(TempQue[i].HasCompleted && !TempQue[i].MeshCountChanged) {
                RenderQue.Add(TempQue[i]);
            } else {
                BuildQue.Add(TempQue[i]);
            }
        }
        for(int i = 0; i < BuildQue.Count; i++) {
            var CurrentRep = i;
            BuildQue[CurrentRep].LoadData();
            Task t1 = Task.Run(() => BuildQue[CurrentRep].BuildTotal());
            CurrentlyActiveTasks.Add(t1);
        }
        MeshAABBs = new AABB[RenderQue.Count];
        ParentCountHasChanged = true;
        if(AggLightTriangles.Count == 0) {AggLightTriangles.Add(new CudaLightTriangle() {});}
        if(RenderQue.Count != 0) {bool throwaway = UpdateTLAS();}
    }

    private void TempBuild() {
        UpdateRenderAndBuildQues();
        int ParentsLength = RenderQue.Count;
        TLASSpace = 2 * ParentsLength;
        int nodes = TLASSpace;
        int b = 0;
        int BVHOffset = 0;
        int MaterialOffset = 0;
        if(NeedsToUpdate || ActuallyNeedsToUpdate) {
            LightMeshCount = 0;
            LightMeshes.Clear();
            BVH8Aggregated.Clear();
            aggregated_triangles.Clear();
            AggLightTriangles.Clear();
            for(int i = 0; i < TLASSpace; i++) {
                BVH8Aggregated.Add(new BVHNode8DataCompressed() {});
            }
            float TotalEnergy = 0;
            LightTransforms.Clear();
            for(int i = 0; i < ParentsLength; i++) {
                RenderQue[i].UpdateData(ref b, ref nodes, ref BVHOffset, ref MaterialOffset);
                
                BVH8Aggregated.AddRange(RenderQue[i].AggNodes);
                aggregated_triangles.AddRange(RenderQue[i].AggTriangles);
                if(RenderQue[i].LightCount != 0) {
                    LightMeshCount++;
                    TotalEnergy += RenderQue[i].TotEnergy;
                    LightTransforms.Add(RenderQue[i].transform);
                    LightMeshes.Add(new LightMeshData() {
                        energy = RenderQue[i].TotEnergy,
                        TotalEnergy = TotalEnergy,
                        StartIndex = AggLightTriangles.Count,
                        IndexEnd = RenderQue[i].LightCount + AggLightTriangles.Count     
                      });
                    AggLightTriangles.AddRange(RenderQue[i].LightTriangles);
                }
            }
            if(LightMeshCount == 0) {LightMeshes.Add(new LightMeshData() {});}
            if(AggLightTriangles.Count == 0) {AggLightTriangles.Add(new CudaLightTriangle() {}); LightTriCount = 0;} else {LightTriCount = AggLightTriangles.Count;}
         CreateAtlas();
        }
    }

	void Aggregate(ref BVH8Builder BVH8) {//BVH aggregation/BVH compression
        for(int i = 0; i < BVH8.BVH8Nodes.Count; ++i) {
            BVHNode8Data TempNode = BVH8.BVH8Nodes[i];
            byte[] tempbyte = new byte[4];
            tempbyte[0] = TempNode.e[0];
            tempbyte[1] = TempNode.e[1];
            tempbyte[2] = TempNode.e[2];
            tempbyte[3] = TempNode.imask;
            byte[] metafirst = new byte[4];
            metafirst[0] = TempNode.meta[0];
            metafirst[1] = TempNode.meta[1];
            metafirst[2] = TempNode.meta[2];
            metafirst[3] = TempNode.meta[3];
            byte[] metasecond = new byte[4];
            metasecond[0] = TempNode.meta[4];
            metasecond[1] = TempNode.meta[5];
            metasecond[2] = TempNode.meta[6];
            metasecond[3] = TempNode.meta[7];
            byte[] minxfirst = new byte[4];
            minxfirst[0] = TempNode.quantized_min_x[0];
            minxfirst[1] = TempNode.quantized_min_x[1];
            minxfirst[2] = TempNode.quantized_min_x[2];
            minxfirst[3] = TempNode.quantized_min_x[3];
            byte[] minxsecond = new byte[4];
            minxsecond[0] = TempNode.quantized_min_x[4];
            minxsecond[1] = TempNode.quantized_min_x[5];
            minxsecond[2] = TempNode.quantized_min_x[6];
            minxsecond[3] = TempNode.quantized_min_x[7];
            byte[] maxxfirst = new byte[4];
            maxxfirst[0] = TempNode.quantized_max_x[0];
            maxxfirst[1] = TempNode.quantized_max_x[1];
            maxxfirst[2] = TempNode.quantized_max_x[2];
            maxxfirst[3] = TempNode.quantized_max_x[3];
            byte[] maxxsecond = new byte[4];
            maxxsecond[0] = TempNode.quantized_max_x[4];
            maxxsecond[1] = TempNode.quantized_max_x[5];
            maxxsecond[2] = TempNode.quantized_max_x[6];
            maxxsecond[3] = TempNode.quantized_max_x[7];

            byte[] minyfirst = new byte[4];
            minyfirst[0] = TempNode.quantized_min_y[0];
            minyfirst[1] = TempNode.quantized_min_y[1];
            minyfirst[2] = TempNode.quantized_min_y[2];
            minyfirst[3] = TempNode.quantized_min_y[3];
            byte[] minysecond = new byte[4];
            minysecond[0] = TempNode.quantized_min_y[4];
            minysecond[1] = TempNode.quantized_min_y[5];
            minysecond[2] = TempNode.quantized_min_y[6];
            minysecond[3] = TempNode.quantized_min_y[7];
            byte[] maxyfirst = new byte[4];
            maxyfirst[0] = TempNode.quantized_max_y[0];
            maxyfirst[1] = TempNode.quantized_max_y[1];
            maxyfirst[2] = TempNode.quantized_max_y[2];
            maxyfirst[3] = TempNode.quantized_max_y[3];
            byte[] maxysecond = new byte[4];
            maxysecond[0] = TempNode.quantized_max_y[4];
            maxysecond[1] = TempNode.quantized_max_y[5];
            maxysecond[2] = TempNode.quantized_max_y[6];
            maxysecond[3] = TempNode.quantized_max_y[7];

            byte[] minzfirst = new byte[4];
            minzfirst[0] = TempNode.quantized_min_z[0];
            minzfirst[1] = TempNode.quantized_min_z[1];
            minzfirst[2] = TempNode.quantized_min_z[2];
            minzfirst[3] = TempNode.quantized_min_z[3];
            byte[] minzsecond = new byte[4];
            minzsecond[0] = TempNode.quantized_min_z[4];
            minzsecond[1] = TempNode.quantized_min_z[5];
            minzsecond[2] = TempNode.quantized_min_z[6];
            minzsecond[3] = TempNode.quantized_min_z[7];
            byte[] maxzfirst = new byte[4];
            maxzfirst[0] = TempNode.quantized_max_z[0];
            maxzfirst[1] = TempNode.quantized_max_z[1];
            maxzfirst[2] = TempNode.quantized_max_z[2];
            maxzfirst[3] = TempNode.quantized_max_z[3];
            byte[] maxzsecond = new byte[4];
            maxzsecond[0] = TempNode.quantized_max_z[4];
            maxzsecond[1] = TempNode.quantized_max_z[5];
            maxzsecond[2] = TempNode.quantized_max_z[6];
            maxzsecond[3] = TempNode.quantized_max_z[7];
            BVHNode8DataCompressed TempNodeComp;
                TempNodeComp.node_0xyz = new Vector3(TempNode.p.x, TempNode.p.y, TempNode.p.z);
                TempNodeComp.node_0w = System.BitConverter.ToUInt32(tempbyte, 0);
                TempNodeComp.node_1x = TempNode.base_index_child;
                TempNodeComp.node_1y = TempNode.base_index_triangle;
                TempNodeComp.node_1z = System.BitConverter.ToUInt32(metafirst, 0);
                TempNodeComp.node_1w = System.BitConverter.ToUInt32(metasecond, 0);
                TempNodeComp.node_2x = System.BitConverter.ToUInt32(minxfirst, 0);
                TempNodeComp.node_2y = System.BitConverter.ToUInt32(minxsecond, 0);
                TempNodeComp.node_2z = System.BitConverter.ToUInt32(maxxfirst, 0);
                TempNodeComp.node_2w = System.BitConverter.ToUInt32(maxxsecond, 0);
                TempNodeComp.node_3x = System.BitConverter.ToUInt32(minyfirst, 0);
                TempNodeComp.node_3y = System.BitConverter.ToUInt32(minysecond, 0);
                TempNodeComp.node_3z = System.BitConverter.ToUInt32(maxyfirst, 0);
                TempNodeComp.node_3w = System.BitConverter.ToUInt32(maxysecond, 0);
                TempNodeComp.node_4x = System.BitConverter.ToUInt32(minzfirst, 0);
                TempNodeComp.node_4y = System.BitConverter.ToUInt32(minzsecond, 0);
                TempNodeComp.node_4z = System.BitConverter.ToUInt32(maxzfirst, 0);
                TempNodeComp.node_4w = System.BitConverter.ToUInt32(maxzsecond, 0);
                BVH8Aggregated[i] = TempNodeComp;
	    }
	}

    public bool UpdateTLAS() {	//Allows for objects to be moved in the scene or animated while playing 
        ActuallyNeedsToUpdate = false;
        bool LightsHaveUpdated = false;
        if(ParentCountHasChanged) {
            int AddQueCount = AddQue.Count;
            int RemoveQueCount = RemoveQue.Count;
            for(int i = AddQueCount - 1; i >= 0; i--) {
                var CurrentRep = BuildQue.Count;
                BuildQue.Add(AddQue[i]);
                BuildQue[CurrentRep].LoadData();
                Task t1 = Task.Run(() => BuildQue[CurrentRep].BuildTotal());
                CurrentlyActiveTasks.Add(t1);
                AddQue.RemoveAt(i);
            }
            for(int i = RemoveQueCount - 1; i >= 0; i--) {
                if(RenderQue.Contains(RemoveQue[i])) {
                    RenderQue.Remove(RemoveQue[i]);
                    RemoveQue.RemoveAt(i);
                }
            }
            MeshAABBs = new AABB[RenderQue.Count];
            TLASSpace = 2 * RenderQue.Count;
            ParentCountHasChanged = false;
            ActuallyNeedsToUpdate = true;
        }
        TempBuild();
        
        UnityLights.Clear();
        float TotalEnergy = (LightMeshes.Count != 0) ? LightMeshes[LightMeshes.Count - 1].TotalEnergy : 0.0f;
        UnityLightCount = 0;
        RayTracingMaster._rayTracingLights.Sort((s1,s2) => s1.Energy.CompareTo(s2.Energy));
        foreach(RayTracingLights RayLight in RayTracingMaster._rayTracingLights) {
            UnityLightCount++;
            RayLight.UpdateLight();
            TotalEnergy += RayLight.Energy;
            UnityLights.Add(new LightData() {
                Radiance = RayLight.Emission,
                Position = RayLight.Position,
                Direction = RayLight.Direction,
                energy = RayLight.Energy,
                TotalEnergy = TotalEnergy,
                Type = RayLight.Type,
                SpotAngle = RayLight.SpotAngle
                });
        }
        if(UnityLights.Count == 0) {UnityLights.Add(new LightData() {});}
        if(PrevLightCount != RayTracingMaster._rayTracingLights.Count) LightsHaveUpdated = true;
        PrevLightCount = RayTracingMaster._rayTracingLights.Count;

        MyMeshesCompacted.Clear();     
        int MeshDataCount =  RenderQue.Count;
        int aggregated_bvh_node_count = 2 * MeshDataCount;
        int AggNodeCount = aggregated_bvh_node_count;
        int AggTriCount = 0;
        int MatOffset = 0;
        for(int i = 0; i < MeshDataCount; i++) {
            RenderQue[i].UpdateAABB();
             MyMeshesCompacted.Add(new MyMeshDataCompacted() {
                mesh_data_bvh_offsets = aggregated_bvh_node_count,
                Transform =  RenderQue[i].transform.worldToLocalMatrix,
                Inverse = RenderQue[i].transform.worldToLocalMatrix.inverse,
                Center = RenderQue[i].transform.position,
                AggIndexCount = AggTriCount,
                AggNodeCount = AggNodeCount,
                MaterialOffset = MatOffset
              });
             MatOffset += RenderQue[i].MatOffset;
            MeshAABBs[i] = RenderQue[i].aabb;
            AggNodeCount += RenderQue[i].AggBVHNodeCount;//Can I replace this with just using aggregated_bvh_node_count below?
            AggTriCount += RenderQue[i].AggIndexCount;
            aggregated_bvh_node_count += RenderQue[i].BVH.cwbvhnode_count;
        }
        LightMeshData CurLightMesh;
        for(int i = 0; i < LightMeshCount; i++) {
            CurLightMesh = LightMeshes[i];
            CurLightMesh.Inverse = LightTransforms[i].worldToLocalMatrix.inverse;
            CurLightMesh.Center = LightTransforms[i].position;
            LightMeshes[i] = CurLightMesh;
        }
        BVH2 = new BVH2Builder(MeshAABBs);
        BVH8Builder TLASBVH8 = new BVH8Builder(BVH2, ref MyMeshesCompacted);
        Aggregate(ref TLASBVH8);
        return (NeedsToUpdate || ActuallyNeedsToUpdate || LightsHaveUpdated);//The issue is that all light triangle indices start at 0, and thus might not get correctly sorted for indices
	}

	public void UpdateMaterials() {//Allows for live updating of material properties of any object
        int ParentCount = RenderQue.Count;
        RayTracingObject CurrentMaterial;
		for(int i = 0; i < ParentCount; i++) {
            int ChildCount = RenderQue[i].ChildObjects.Length;
			for(int i2 = 0; i2 < ChildCount; i2++) {
                CurrentMaterial = RenderQue[i].ChildObjects[i2];
                int MaterialCount = CurrentMaterial.MaterialIndex.Length;
                for(int i3 = 0; i3 < MaterialCount; i3++) {
    				MaterialData TempMat = _Materials[CurrentMaterial.MaterialIndex[i3]];
                    if(TempMat.HasAlbedoTex != 1) {
    					TempMat.BaseColor = CurrentMaterial.BaseColor[i3];
    				}
    				TempMat.emmissive = CurrentMaterial.emmission[i3];
    				TempMat.Roughness = (CurrentMaterial.MatType[i3] != 1) ? CurrentMaterial.Roughness[i3] : Mathf.Max(CurrentMaterial.Roughness[i3], 0.000001f);
    				TempMat.eta = CurrentMaterial.eta[i3];
                    TempMat.MatType = CurrentMaterial.MatType[i3];
    				_Materials[CurrentMaterial.MaterialIndex[i3]] = TempMat;
                }
			}
		}
	}
    
}
