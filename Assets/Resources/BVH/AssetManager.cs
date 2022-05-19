using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;
using System.Threading;


[System.Serializable]
public class AssetManager : MonoBehaviour {
    public Texture2D AlbedoAtlas;
    public Texture2D NormalAtlas;
    public Texture2D EmissiveAtlas;

    [HideInInspector] public List<MaterialData> _Materials;
    [HideInInspector] public ComputeBuffer BVH8AggregatedBuffer;
    [HideInInspector] public ComputeBuffer AggTriBuffer;
    [HideInInspector] public List<MyMeshDataCompacted> MyMeshesCompacted;
    [HideInInspector] public List<CudaLightTriangle> AggLightTriangles;
    [HideInInspector] public List<LightData> UnityLights;

    public ComputeShader MeshFunctions;
    private int TriangleBufferKernel;
    private int NodeBufferKernel;

    public List<ParentObject> RenderQue;
    public List<ParentObject> BuildQue;
    public List<ParentObject> AddQue;
    public List<ParentObject> RemoveQue;

    [HideInInspector] public List<Transform> LightTransforms;
    [HideInInspector] public List<Task> CurrentlyActiveTasks;

    [HideInInspector] public List<LightMeshData> LightMeshes;

    [HideInInspector] public AABB[] MeshAABBs;

    [HideInInspector] public bool ParentCountHasChanged;    
    private bool NeedsToUpdate = false;
    private bool ActuallyNeedsToUpdate = false;
    
    [HideInInspector] public int TLASSpace;
    [HideInInspector] public int LightTriCount;
    [HideInInspector] public int LightMeshCount;
    [HideInInspector] public int UnityLightCount;
    private int PrevLightCount;

    [HideInInspector] public BVH2Builder BVH2;

    [HideInInspector] public bool UseSkinning = true;
    [HideInInspector] public bool HasStart = false;
    [HideInInspector] public bool didstart = false;

    public void ClearAll() {//My attempt at clearing memory
        ParentObject[] ChildrenObjects = this.GetComponentsInChildren<ParentObject>();
        foreach(ParentObject obj in ChildrenObjects)
            obj.ClearAll();
        if(_Materials != null) {
            _Materials.Clear();
            _Materials.TrimExcess();
        }
        if(LightTransforms != null) {
            LightTransforms.Clear();
            LightTransforms.TrimExcess();
        }
        if(LightMeshes != null) {
            LightMeshes.Clear();
            LightMeshes.TrimExcess();
        }
        if(MyMeshesCompacted != null) {
            MyMeshesCompacted.Clear();
            MyMeshesCompacted.TrimExcess();
        }
        if(AggLightTriangles != null) {
            AggLightTriangles.Clear();
            AggLightTriangles.TrimExcess();
        }
        if(UnityLights != null) {
            UnityLights.Clear();
            UnityLights.TrimExcess();
        }
        DestroyImmediate(AlbedoAtlas);
        DestroyImmediate(NormalAtlas);
        DestroyImmediate(EmissiveAtlas);  
        NeedsToUpdate = true;
        ActuallyNeedsToUpdate = true;
        if(BVH8AggregatedBuffer != null) {
            BVH8AggregatedBuffer.Release();
            BVH8AggregatedBuffer = null;    
            AggTriBuffer.Release();
            AggTriBuffer = null;    
        }

        Resources.UnloadUnusedAssets();
        System.GC.Collect();
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
            AlbedoAtlas = new Texture2D(1, 1);
            AlbedoRects = AlbedoAtlas.PackTextures(AlbedoTexs.ToArray(), 2, 16384);
        } else {
            AlbedoAtlas = new Texture2D(1,1);
            AlbedoRects = new Rect[0];
        }
        if(NormalTexs.Count != 0) {
            NormalAtlas = new Texture2D(1, 1);
            NormalRects = NormalAtlas.PackTextures(NormalTexs.ToArray(), 2, 16384);
        } else {
            NormalAtlas = new Texture2D(1,1);
            NormalRects = new Rect[0];
        }
        if(EmissiveTexs.Count != 0) {
            EmissiveAtlas = new Texture2D(1, 1);
            EmmissiveRects = EmissiveAtlas.PackTextures(EmissiveTexs.ToArray(), 2, 16384);
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
        AggLightTriangles = new List<CudaLightTriangle>();
        CurrentlyActiveTasks = new List<Task>();
        UnityLights = new List<LightData>();
        LightMeshes = new List<LightMeshData>();
        LightTransforms = new List<Transform>();
        LightMeshCount = 0;
        UnityLightCount = 0;
        LightTriCount = 0;
        if(BVH8AggregatedBuffer != null) {
            BVH8AggregatedBuffer.Release();
            BVH8AggregatedBuffer = null;    
            AggTriBuffer.Release();
            AggTriBuffer = null;    
        }
        MeshFunctions = Resources.Load<ComputeShader>("Utility/GeneralMeshFunctions");
        TriangleBufferKernel = MeshFunctions.FindKernel("CombineTriBuffers");
        NodeBufferKernel = MeshFunctions.FindKernel("CombineNodeBuffers");
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
                RenderQue[RenderQue.Count - 1].SetUpBuffers();
                NeedsToUpdate = true;
            }
        }
        if(NeedsToUpdate) {
            MeshAABBs = new AABB[RenderQue.Count];
        }
    }

    public void EditorBuild() {
        ClearAll();
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
        didstart = false;
    }

    public void BuildCombined() {
        HasToDo = false;
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
    public bool HasToDo;

    private void TempBuild() {
        UpdateRenderAndBuildQues();
        int ParentsLength = RenderQue.Count;
        TLASSpace = 2 * ParentsLength;
        int nodes = TLASSpace;
        int b = 0;
        int BVHOffset = 0;
        int MaterialOffset = 0;
        if(NeedsToUpdate || ActuallyNeedsToUpdate) {
            HasToDo = false;
            LightMeshCount = 0;
            LightMeshes.Clear();
            AggLightTriangles.Clear();
            int AggTriCount = 0;
            int AggNodeCount = TLASSpace;//removed - 1
            for(int i = 0; i < ParentsLength; i++) {
                AggNodeCount += RenderQue[i].AggNodes.Length;
                AggTriCount += RenderQue[i].AggTriangles.Length;
            }
            float TotalEnergy = 0;
            LightTransforms.Clear();
            if(BVH8AggregatedBuffer != null) {
                BVH8AggregatedBuffer.Release();
                BVH8AggregatedBuffer = null;    
                AggTriBuffer.Release();
                AggTriBuffer = null;    
            }
            BVH8AggregatedBuffer = new ComputeBuffer(AggNodeCount, 80);
            AggTriBuffer = new ComputeBuffer(AggTriCount, 136);
            MeshFunctions.SetBuffer(TriangleBufferKernel, "OutCudaTriArray", AggTriBuffer);
            MeshFunctions.SetBuffer(NodeBufferKernel, "OutAggNodes", BVH8AggregatedBuffer);
            int CurNodeOffset = TLASSpace;
            int CurTriOffset = 0;
            for(int i = 0; i < ParentsLength; i++) {
                RenderQue[i].SetUpBuffers();
                RenderQue[i].UpdateData(ref b, ref nodes, ref BVHOffset, ref MaterialOffset);
                MeshFunctions.SetInt("Offset", CurTriOffset);
                MeshFunctions.SetInt("Count", RenderQue[i].TriBuffer.count);
                MeshFunctions.SetBuffer(TriangleBufferKernel, "InCudaTriArray", RenderQue[i].TriBuffer);
                MeshFunctions.Dispatch(TriangleBufferKernel, (int)Mathf.Ceil(RenderQue[i].TriBuffer.count / 248.0f), 1, 1);

                MeshFunctions.SetInt("Offset", CurNodeOffset);
                MeshFunctions.SetInt("Count", RenderQue[i].BVHBuffer.count);
                MeshFunctions.SetBuffer(NodeBufferKernel, "InAggNodes", RenderQue[i].BVHBuffer);
                MeshFunctions.Dispatch(NodeBufferKernel, (int)Mathf.Ceil(RenderQue[i].BVHBuffer.count / 248.0f), 1, 1);

                RenderQue[i].NodeOffset = CurNodeOffset;
                RenderQue[i].TriOffset = CurTriOffset;
                CurNodeOffset += RenderQue[i].AggNodes.Length;
                CurTriOffset += RenderQue[i].AggTriangles.Length;

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
           NeedsToUpdate = true;
           ActuallyNeedsToUpdate = true;
           CreateAtlas();
        }
        if(UseSkinning && didstart) { 
            for(int i = 0; i < ParentsLength; i++) {
                if(RenderQue[i].IsSkinnedGroup) {
                    RenderQue[i].RefitMesh();
                    MeshFunctions.SetInt("Offset", RenderQue[i].TriOffset);
                    MeshFunctions.SetInt("Count", RenderQue[i].TriBuffer.count);
                    MeshFunctions.SetBuffer(TriangleBufferKernel, "InCudaTriArray", RenderQue[i].TriBuffer);
                    MeshFunctions.Dispatch(TriangleBufferKernel, (int)Mathf.Ceil(RenderQue[i].TriBuffer.count / 248.0f), 1, 1);
                    MeshFunctions.SetInt("Offset", RenderQue[i].NodeOffset);
                    MeshFunctions.SetInt("Count", RenderQue[i].BVHBuffer.count);
                    MeshFunctions.SetBuffer(NodeBufferKernel, "InAggNodes", RenderQue[i].BVHBuffer);
                    MeshFunctions.Dispatch(NodeBufferKernel, (int)Mathf.Ceil(RenderQue[i].BVHBuffer.count / 248.0f), 1, 1);
                }
            }
        }
    }

    void Aggregate(ref BVH8Builder BVH8) {//BVH aggregation/BVH compression
        List<BVHNode8DataCompressed> TempBVHArray = new List<BVHNode8DataCompressed>();
        for(int i = 0; i < BVH8.BVH8Nodes.Length; ++i) {
            BVHNode8Data TempNode = BVH8.BVH8Nodes[i];
            uint tempbyte = (TempNode.e[0] | (TempNode.e[1] << 8) | (TempNode.e[2] << 16) | (TempNode.imask << 24));
            uint metafirst = (TempNode.meta[0] | (TempNode.meta[1] << 8) | (TempNode.meta[2] << 16) | (TempNode.meta[3] << 24));
            uint metasecond = (TempNode.meta[4] | (TempNode.meta[5] << 8) | (TempNode.meta[6] << 16) | (TempNode.meta[7] << 24));
            uint minxfirst = (TempNode.quantized_min_x[0] | (TempNode.quantized_min_x[1] << 8) | (TempNode.quantized_min_x[2] << 16) | (TempNode.quantized_min_x[3] << 24));
            uint minxsecond = (TempNode.quantized_min_x[4] | (TempNode.quantized_min_x[5] << 8) | (TempNode.quantized_min_x[6] << 16) | (TempNode.quantized_min_x[7] << 24));
            uint maxxfirst = (TempNode.quantized_max_x[0] | (TempNode.quantized_max_x[1] << 8) | (TempNode.quantized_max_x[2] << 16) | (TempNode.quantized_max_x[3] << 24));
            uint maxxsecond = (TempNode.quantized_max_x[4] | (TempNode.quantized_max_x[5] << 8) | (TempNode.quantized_max_x[6] << 16) | (TempNode.quantized_max_x[7] << 24));
            uint minyfirst = (TempNode.quantized_min_y[0] | (TempNode.quantized_min_y[1] << 8) | (TempNode.quantized_min_y[2] << 16) | (TempNode.quantized_min_y[3] << 24));
            uint minysecond = (TempNode.quantized_min_y[4] | (TempNode.quantized_min_y[5] << 8) | (TempNode.quantized_min_y[6] << 16) | (TempNode.quantized_min_y[7] << 24));
            uint maxyfirst = (TempNode.quantized_max_y[0] | (TempNode.quantized_max_y[1] << 8) | (TempNode.quantized_max_y[2] << 16) | (TempNode.quantized_max_y[3] << 24));
            uint maxysecond = (TempNode.quantized_max_y[4] | (TempNode.quantized_max_y[5] << 8) | (TempNode.quantized_max_y[6] << 16) | (TempNode.quantized_max_y[7] << 24));
            uint minzfirst = (TempNode.quantized_min_z[0] | (TempNode.quantized_min_z[1] << 8) | (TempNode.quantized_min_z[2] << 16) | (TempNode.quantized_min_z[3] << 24));
            uint minzsecond = (TempNode.quantized_min_z[4] | (TempNode.quantized_min_z[5] << 8) | (TempNode.quantized_min_z[6] << 16) | (TempNode.quantized_min_z[7] << 24));
            uint maxzfirst = (TempNode.quantized_max_z[0] | (TempNode.quantized_max_z[1] << 8) | (TempNode.quantized_max_z[2] << 16) | (TempNode.quantized_max_z[3] << 24));
            uint maxzsecond = (TempNode.quantized_max_z[4] | (TempNode.quantized_max_z[5] << 8) | (TempNode.quantized_max_z[6] << 16) | (TempNode.quantized_max_z[7] << 24));

            TempBVHArray.Add(new BVHNode8DataCompressed() {
                node_0xyz = new Vector3(TempNode.p.x, TempNode.p.y, TempNode.p.z),
                node_0w = tempbyte,
                node_1x = TempNode.base_index_child,
                node_1y = TempNode.base_index_triangle,
                node_1z = metafirst,
                node_1w = metasecond,
                node_2x = minxfirst,
                node_2y = minxsecond,
                node_2z = maxxfirst,
                node_2w = maxxsecond,
                node_3x = minyfirst,
                node_3y = minysecond,
                node_3z = maxyfirst,
                node_3w = maxysecond,
                node_4x = minzfirst,
                node_4y = minzsecond,
                node_4z = maxzfirst,
                node_4w = maxzsecond

            });
        }
        BVH8AggregatedBuffer.SetData(TempBVHArray,0,0,TempBVHArray.Count);
    }

    public bool UpdateTLAS() {  //Allows for objects to be moved in the scene or animated while playing 
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
        if(!didstart) {
            Thread.Sleep(10);
        }
        TempBuild();
        if(!didstart) {
            didstart = true;
        }
       
        
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
            if(!RenderQue[i].IsSkinnedGroup) RenderQue[i].UpdateAABB();
             MyMeshesCompacted.Add(new MyMeshDataCompacted() {
                mesh_data_bvh_offsets = aggregated_bvh_node_count,
                Transform = RenderQue[i].transform.worldToLocalMatrix,
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
