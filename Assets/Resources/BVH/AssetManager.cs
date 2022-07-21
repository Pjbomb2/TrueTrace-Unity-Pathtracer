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
    public Texture2D MetallicAtlas;
    public Texture2D RoughnessAtlas;


    public List<MaterialData> _Materials;
    [HideInInspector] public ComputeBuffer BVH8AggregatedBuffer;
    [HideInInspector] public ComputeBuffer AggTriBuffer;
    public List<BVHNode8DataCompressed> VoxelTLAS;
    public List<MyMeshDataCompacted> MyMeshesCompacted;
     public List<CudaLightTriangle> AggLightTriangles;
     public List<int> ToIllumTriBuffer;
    [HideInInspector] public List<LightData> UnityLights;
    public List<Light> Lights;
    public InstancedManager InstanceData;
    public List<InstancedObject> Instances;

    public ComputeShader MeshFunctions;
    private int TriangleBufferKernel;
    private int NodeBufferKernel;

    public List<ParentObject> RenderQue;
    public List<ParentObject> BuildQue;
    public List<ParentObject> AddQue;
    public List<ParentObject> RemoveQue;
    public List<InstancedObject> InstanceRenderQue;
    public List<InstancedObject> InstanceBuildQue;
    public List<InstancedObject> InstanceAddQue;
    public List<InstancedObject> InstanceRemoveQue;
    public List<VoxelObject> VoxelBuildQue;
    public List<VoxelObject> VoxelRenderQue;
    public List<VoxelObject> VoxelAddQue;
    public List<VoxelObject> VoxelRemoveQue;
    private bool OnlyInstanceUpdated;
    [HideInInspector] public List<Transform> LightTransforms;
    [HideInInspector] public List<Task> CurrentlyActiveTasks;
    [HideInInspector] public List<Task> CurrentlyActiveVoxelTasks;

    public List<LightMeshData> LightMeshes;

    [HideInInspector] public AABB[] MeshAABBs;
    [HideInInspector] public AABB[] VoxelAABBs;

    [HideInInspector] public bool ParentCountHasChanged;    
    
    [HideInInspector] public int TLASSpace;
    [HideInInspector] public int LightTriCount;
    [HideInInspector] public int LightMeshCount;
    [HideInInspector] public int UnityLightCount;
    public List<GPUOctreeNode> GPUOctree;
    public int VoxOffset;
    private int PrevLightCount;

    [HideInInspector] public BVH2Builder BVH2;

    [HideInInspector] public bool UseSkinning = true;
    [HideInInspector] public bool HasStart = false;
    [HideInInspector] public bool didstart = false;
    public bool UseVoxels;
    public bool ChildrenUpdated;

    public Vector3 SunDirection;

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
        List<Texture2D> MetallicTexs = new List<Texture2D>();
        List<Texture2D> RoughnessTexs = new List<Texture2D>();
        AlbedoAtlas = new Texture2D(1,1);
        NormalAtlas = new Texture2D(1, 1);
        EmissiveAtlas = new Texture2D(1, 1);
        MetallicAtlas = new Texture2D(1, 1);
        RoughnessAtlas = new Texture2D(1, 1);
        int AlbedoCount, NormalCount, EmissiveCount, MetallicCount, RoughnessCount = 0;
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
            if(Obj.HasMetallicAtlas) {
                MetallicTexs.Add((Texture2D)Obj.MetallicAtlas);
            }
            if(Obj.HasRoughnessAtlas) {
                RoughnessTexs.Add((Texture2D)Obj.RoughnessAtlas);
            }
        }
        foreach(ParentObject Obj in InstanceData.RenderQue) {
            if(Obj.HasAlbedoAtlas) {
                AlbedoTexs.Add((Texture2D)Obj.AlbedoAtlas);
            }
            if(Obj.HasNormalAtlas) {
                NormalTexs.Add((Texture2D)Obj.NormalAtlas);
            }
            if(Obj.HasEmissiveAtlas) {
                EmissiveTexs.Add((Texture2D)Obj.EmissiveAtlas);
            }
            if(Obj.HasMetallicAtlas) {
                MetallicTexs.Add((Texture2D)Obj.MetallicAtlas);
            }
            if(Obj.HasRoughnessAtlas) {
                RoughnessTexs.Add((Texture2D)Obj.RoughnessAtlas);
            }
        }
        Rect[] AlbedoRects, NormalRects, EmmissiveRects, MetallicRects, RoughnessRects;
        if(AlbedoTexs.Count != 0) {
            AlbedoRects = AlbedoAtlas.PackTextures(AlbedoTexs.ToArray(), 1, Mathf.Min((int)Mathf.Ceil(Mathf.Sqrt(RenderQue.Count)) * RenderQue[0].AtlasSize, 16392), true);//4096);
        } else {
            AlbedoRects = new Rect[0];
        }
        if(NormalTexs.Count != 0) {
            NormalRects = NormalAtlas.PackTextures(NormalTexs.ToArray(), 1, Mathf.Min((int)Mathf.Ceil(Mathf.Sqrt(RenderQue.Count)) * RenderQue[0].AtlasSize, 16392), true);//4096);
        } else {
            NormalRects = new Rect[0];
        }
        if(EmissiveTexs.Count != 0) {
            EmmissiveRects = EmissiveAtlas.PackTextures(EmissiveTexs.ToArray(), 1, Mathf.Min((int)Mathf.Ceil(Mathf.Sqrt(RenderQue.Count)) * RenderQue[0].AtlasSize, 16392), true);//4096);
        } else {
            EmmissiveRects = new Rect[0];
        }
        if(MetallicTexs.Count != 0) {
            MetallicRects = MetallicAtlas.PackTextures(MetallicTexs.ToArray(), 1, Mathf.Min((int)Mathf.Ceil(Mathf.Sqrt(RenderQue.Count)) * RenderQue[0].AtlasSize, 16392), true);//4096);
        } else {
            MetallicRects = new Rect[0];
        }
        if(RoughnessTexs.Count != 0) {
            RoughnessRects = RoughnessAtlas.PackTextures(RoughnessTexs.ToArray(), 1, Mathf.Min((int)Mathf.Ceil(Mathf.Sqrt(RenderQue.Count)) * RenderQue[0].AtlasSize, 16392), true);//4096);
        } else {
            RoughnessRects = new Rect[0];
        }
        AlbedoCount = NormalCount = EmissiveCount = MetallicCount = RoughnessCount =  0;
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
                if(TempMat.HasMetallicTex == 1) {
                    TempMat.MetallicTex = ResizeRect(TempMat.MetallicTex, MetallicRects[MetallicCount]);
                }
                if(TempMat.HasRoughnessTex == 1) {
                    TempMat.RoughnessTex = ResizeRect(TempMat.RoughnessTex, RoughnessRects[RoughnessCount]);
                }
                _Materials.Add(TempMat);
                MatCount++;
            }
            if(Obj.HasAlbedoAtlas) AlbedoCount++;
            if(Obj.HasNormalAtlas) NormalCount++;
            if(Obj.HasEmissiveAtlas) EmissiveCount++;
            if(Obj.HasMetallicAtlas) MetallicCount++;
            if(Obj.HasRoughnessAtlas) RoughnessCount++;

        }
        foreach(ParentObject Obj in InstanceData.RenderQue) {
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
                if(TempMat.HasMetallicTex == 1) {
                    TempMat.MetallicTex = ResizeRect(TempMat.MetallicTex, MetallicRects[MetallicCount]);
                }
                if(TempMat.HasRoughnessTex == 1) {
                    TempMat.RoughnessTex = ResizeRect(TempMat.RoughnessTex, RoughnessRects[RoughnessCount]);
                }
                _Materials.Add(TempMat);
                MatCount++;
            }
            if(Obj.HasAlbedoAtlas) AlbedoCount++;
            if(Obj.HasNormalAtlas) NormalCount++;
            if(Obj.HasEmissiveAtlas) EmissiveCount++;
            if(Obj.HasMetallicAtlas) MetallicCount++;
            if(Obj.HasRoughnessAtlas) RoughnessCount++;

        }
        AlbedoTexs.Clear();
        AlbedoTexs.TrimExcess();
        NormalTexs.Clear();
        NormalTexs.TrimExcess();
        EmissiveTexs.Clear();
        EmissiveTexs.TrimExcess(); 
        MetallicTexs.Clear();
        MetallicTexs.TrimExcess(); 
        RoughnessTexs.Clear();
        RoughnessTexs.TrimExcess(); 
        foreach(VoxelObject Vox in VoxelRenderQue) {
            foreach(MaterialData Mat in Vox._Materials) {
                _Materials.Add(Mat);
            }
        } 

    }
    public void OnApplicationQuit() {
        if(BVH8AggregatedBuffer != null) {
            BVH8AggregatedBuffer.Release();
            BVH8AggregatedBuffer = null;    
            AggTriBuffer.Release();
            AggTriBuffer = null;    
        }
    }

    private void init() {
        InstanceData = GameObject.Find("InstancedStorage").GetComponent<InstancedManager>();
        Instances = new List<InstancedObject>();
        ToIllumTriBuffer = new List<int>();
        SunDirection = new Vector3(0,-1,0);
        AddQue = new List<ParentObject>();
        RemoveQue = new List<ParentObject>();
        RenderQue = new List<ParentObject>();
        VoxelAddQue = new List<VoxelObject>();
        VoxelRemoveQue = new List<VoxelObject>();
        VoxelRenderQue = new List<VoxelObject>();
        VoxelBuildQue = new List<VoxelObject>();
        CurrentlyActiveTasks = new List<Task>();
        CurrentlyActiveVoxelTasks = new List<Task>();
        BuildQue = new List<ParentObject>();
        MyMeshesCompacted = new List<MyMeshDataCompacted>();
        AggLightTriangles = new List<CudaLightTriangle>();
        CurrentlyActiveTasks = new List<Task>();
        UnityLights = new List<LightData>();
        LightMeshes = new List<LightMeshData>();
        LightTransforms = new List<Transform>();
        Lights = new List<Light>();
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
        ChildrenUpdated = false;
        OnlyInstanceUpdated = false;

        int AddQueCount = AddQue.Count - 1;
        int RemoveQueCount = RemoveQue.Count - 1;
        for(int i = AddQueCount; i >= 0; i--) {
            var CurrentRep = BuildQue.Count;
            BuildQue.Add(AddQue[i]);
            AddQue.RemoveAt(i);
            BuildQue[CurrentRep].LoadData();
            CurrentlyActiveTasks.Add(Task.Run(() => BuildQue[CurrentRep].BuildTotal()));
            ChildrenUpdated = true;
        }
        for(int i = RemoveQueCount; i >= 0; i--) {
            if(RenderQue.Contains(RemoveQue[i]))
                RenderQue.Remove(RemoveQue[i]);
            else if(BuildQue.Contains(RemoveQue[i])) {
                CurrentlyActiveTasks.RemoveAt(BuildQue.IndexOf(RemoveQue[i]));
                BuildQue.Remove(RemoveQue[i]);
            } else
                Debug.Log("REMOVE QUE NOT FOUND");
            ChildrenUpdated = true;
            RemoveQue.RemoveAt(i); 
        }
        int RenderQueCount = RenderQue.Count - 1;
        for(int i = RenderQueCount; i >= 0; i--) {//Demotes from Render Que to Build Que in case mesh has changed
            if(RenderQue[i].NeedsToUpdate) {
                RenderQue[i].ClearAll();
                RenderQue[i].LoadData();
                BuildQue.Add(RenderQue[i]);
                RenderQue.RemoveAt(i);
                var TempBuildQueCount = BuildQue.Count - 1;
                CurrentlyActiveTasks.Add(Task.Run(() => BuildQue[TempBuildQueCount].BuildTotal()));
                ChildrenUpdated = true;
            }
        }
        int BuildQueCount = BuildQue.Count - 1;
        for(int i = BuildQueCount; i >= 0; i--) {//Promotes from Build Que to Render Que
            if(CurrentlyActiveTasks[i].IsFaulted) Debug.Log(CurrentlyActiveTasks[i].Exception + ", " + BuildQue[i].Name);//Fuck, something fucked up
            if(CurrentlyActiveTasks[i].Status == TaskStatus.RanToCompletion) {
                BuildQue[i].SetUpBuffers();
                RenderQue.Add(BuildQue[i]);
                BuildQue.RemoveAt(i);
                CurrentlyActiveTasks.RemoveAt(i);
                ChildrenUpdated = true;
            }
        }
        InstanceData.UpdateRenderAndBuildQues(ref ChildrenUpdated);
        AddQueCount = InstanceAddQue.Count - 1;
        RemoveQueCount = InstanceRemoveQue.Count - 1;
        for(int i = AddQueCount; i >= 0; i--) {
            if(InstanceAddQue[i].InstanceParent != null) {
                InstanceBuildQue.Add(InstanceAddQue[i]);
                InstanceAddQue.RemoveAt(i);
            }
        }
        for(int i = RemoveQueCount; i >= 0; i--) {
            if(InstanceRenderQue.Contains(InstanceRemoveQue[i]))
                InstanceRenderQue.Remove(InstanceRemoveQue[i]);
            else if(InstanceBuildQue.Contains(InstanceRemoveQue[i])) {
                InstanceBuildQue.Remove(InstanceRemoveQue[i]);
            } else
                Debug.Log("REMOVE QUE NOT FOUND");
            OnlyInstanceUpdated = true;
            InstanceRemoveQue.RemoveAt(i); 
        }
        RenderQueCount = InstanceRenderQue.Count - 1;
        for(int i = RenderQueCount; i >= 0; i--) {//Demotes from Render Que to Build Que in case mesh has changed
            InstanceRenderQue[i].UpdateInstance();
            if(InstanceRenderQue[i].InstanceParent == null || InstanceRenderQue[i].InstanceParent.gameObject.activeInHierarchy == false || InstanceRenderQue[i].InstanceParent.HasCompleted == false) {
                InstanceBuildQue.Add(InstanceRenderQue[i]);
                InstanceRenderQue.RemoveAt(i);
                OnlyInstanceUpdated = true;
            }
        }
        BuildQueCount = InstanceBuildQue.Count - 1;
        for(int i = BuildQueCount; i >= 0; i--) {//Promotes from Build Que to Render Que
            if(InstanceBuildQue[i].InstanceParent.HasCompleted == true) {
                InstanceRenderQue.Add(InstanceBuildQue[i]);
                InstanceBuildQue.RemoveAt(i);
                OnlyInstanceUpdated = true;
            }
        }

        AddQueCount = VoxelAddQue.Count - 1;
        RemoveQueCount = VoxelRemoveQue.Count - 1;
        for(int i = AddQueCount; i >= 0; i--) {
            var CurrentRep = VoxelBuildQue.Count;
            VoxelBuildQue.Add(VoxelAddQue[i]);
            VoxelAddQue.RemoveAt(i);
            VoxelBuildQue[CurrentRep].LoadData();
            CurrentlyActiveVoxelTasks.Add(Task.Run(() => VoxelBuildQue[CurrentRep].BuildOctree()));
            ChildrenUpdated = true;
        }
        for(int i = RemoveQueCount; i >= 0; i--) {
            if(VoxelRenderQue.Contains(VoxelRemoveQue[i]))
                VoxelRenderQue.Remove(VoxelRemoveQue[i]);
            else if(VoxelBuildQue.Contains(VoxelRemoveQue[i])) {
                CurrentlyActiveVoxelTasks.RemoveAt(VoxelBuildQue.IndexOf(VoxelRemoveQue[i]));
                VoxelBuildQue.Remove(VoxelRemoveQue[i]);
            } else
                Debug.Log("REMOVE QUE NOT FOUND");
            ChildrenUpdated = true;
            VoxelRemoveQue.RemoveAt(i); 
        }



        BuildQueCount = VoxelBuildQue.Count - 1;
        for(int i = BuildQueCount; i >= 0; i--) {//Promotes from Build Que to Render Que
            if(CurrentlyActiveVoxelTasks[i].IsFaulted) Debug.Log(CurrentlyActiveVoxelTasks[i].Exception);//Fuck, something fucked up
            if(CurrentlyActiveVoxelTasks[i].Status == TaskStatus.RanToCompletion) {
                VoxelRenderQue.Add(VoxelBuildQue[i]);
                VoxelBuildQue.RemoveAt(i);
                CurrentlyActiveVoxelTasks.RemoveAt(i);
                ChildrenUpdated = true;
            }
        }
        if(OnlyInstanceUpdated && !ChildrenUpdated) {
            ChildrenUpdated = true;
        } else {
            OnlyInstanceUpdated = false;
        }
        if(ChildrenUpdated || ParentCountHasChanged) MeshAABBs = new AABB[RenderQue.Count + InstanceRenderQue.Count];
        if(ChildrenUpdated || ParentCountHasChanged) VoxelAABBs = new AABB[VoxelRenderQue.Count];
    }
    public void EditorBuild() {
        ClearAll();
        init();
        AddQue = new List<ParentObject>();
        RemoveQue = new List<ParentObject>();
        RenderQue = new List<ParentObject>();
        CurrentlyActiveTasks = new List<Task>();
        CurrentlyActiveVoxelTasks = new List<Task>();
        BuildQue = new List<ParentObject>(GetComponentsInChildren<ParentObject>());
        //InstanceAddQue = new List<InstancedObject>(GetComponentsInChildren<InstancedObject>());
        VoxelBuildQue = new List<VoxelObject>(GetComponentsInChildren<VoxelObject>());
        InstanceData.EditorBuild();
        for(int i = 0; i < BuildQue.Count; i++) {
            var CurrentRep = i;
            BuildQue[CurrentRep].LoadData();
            Task t1 = Task.Run(() => BuildQue[CurrentRep].BuildTotal());
            CurrentlyActiveTasks.Add(t1);
        }
        for(int i = 0; i < VoxelBuildQue.Count; i++) {
            var CurrentRep = i;
            VoxelBuildQue[CurrentRep].LoadData();
            Task t1 = Task.Run(() => VoxelBuildQue[CurrentRep].BuildOctree());
            CurrentlyActiveVoxelTasks.Add(t1);
        }
        if(AggLightTriangles.Count == 0) {AggLightTriangles.Add(new CudaLightTriangle() {});}
        didstart = false;
    }
    int Counter;
    public void BuildCombined() {
        Counter = 0;
        HasToDo = false;
        init();
        CurrentlyActiveVoxelTasks = new List<Task>();
        List<ParentObject> TempQue = new List<ParentObject>(GetComponentsInChildren<ParentObject>());
        InstanceAddQue = new List<InstancedObject>(GetComponentsInChildren<InstancedObject>());
        InstanceData.BuildCombined();
        List<VoxelObject> TempVoxelQue = new List<VoxelObject>(GetComponentsInChildren<VoxelObject>());
        for(int i = 0; i < TempQue.Count; i++) {
            if(TempQue[i].HasCompleted && !TempQue[i].NeedsToUpdate)
                RenderQue.Add(TempQue[i]);
            else
                BuildQue.Add(TempQue[i]);
        }
        for(int i = 0; i < TempVoxelQue.Count; i++) {
            if(TempVoxelQue[i].HasCompleted)
                VoxelRenderQue.Add(TempVoxelQue[i]);
            else
                VoxelBuildQue.Add(TempVoxelQue[i]);
        }
        for(int i = 0; i < BuildQue.Count; i++) {
            var CurrentRep = i;
            BuildQue[CurrentRep].LoadData();
            CurrentlyActiveTasks.Add(Task.Run(() => BuildQue[CurrentRep].BuildTotal()));
        }
        for(int i = 0; i < VoxelBuildQue.Count; i++) {
            var CurrentRep = i;
            VoxelBuildQue[CurrentRep].LoadData();
            CurrentlyActiveVoxelTasks.Add(Task.Run(() => VoxelBuildQue[CurrentRep].BuildOctree()));
        }
        MeshAABBs = new AABB[RenderQue.Count + InstanceRenderQue.Count];
        VoxelAABBs = new AABB[VoxelRenderQue.Count];
        ParentCountHasChanged = true;
        if(AggLightTriangles.Count == 0) {AggLightTriangles.Add(new CudaLightTriangle() {});}
        if(RenderQue.Count != 0) {bool throwaway = UpdateTLAS();}
    }
    public bool HasToDo;

    private void TempBuild() {
        UpdateRenderAndBuildQues();
        int ParentsLength = RenderQue.Count;
        TLASSpace = 2 * (ParentsLength + InstanceRenderQue.Count);
        int nodes = TLASSpace;
        int BVHOffset = 0;
        int MaterialOffset = 0;
        if(ChildrenUpdated || ParentCountHasChanged) {
            HasToDo = false;
            int CurNodeOffset = TLASSpace;
            int CurTriOffset = 0;
            LightMeshCount = 0;
            LightMeshes.Clear();
            AggLightTriangles.Clear();
            LightTransforms.Clear();
            float CDF = 0;
            int AggTriCount = 0;
            int AggNodeCount = TLASSpace;
            if(BVH8AggregatedBuffer != null) {
                BVH8AggregatedBuffer.Release();    
                AggTriBuffer.Release();  
            }
            for(int i = 0; i < ParentsLength; i++) {
                AggNodeCount += RenderQue[i].AggNodes.Length;
                AggTriCount += RenderQue[i].AggTriangles.Length;
            }
            for(int i = 0; i < InstanceData.RenderQue.Count; i++) {
                AggNodeCount += InstanceData.RenderQue[i].AggNodes.Length;
                AggTriCount += InstanceData.RenderQue[i].AggTriangles.Length;
            }
            if(AggNodeCount != 0) {
                BVH8AggregatedBuffer = new ComputeBuffer(AggNodeCount, 80);
                AggTriBuffer = new ComputeBuffer(AggTriCount, 136);
                MeshFunctions.SetBuffer(TriangleBufferKernel, "OutCudaTriArray", AggTriBuffer);
                MeshFunctions.SetBuffer(NodeBufferKernel, "OutAggNodes", BVH8AggregatedBuffer);
                for(int i = 0; i < ParentsLength; i++) {
                    RenderQue[i].UpdateData(ref BVHOffset, ref MaterialOffset);
                    RenderQue[i].SetUpBuffers();
                    MeshFunctions.SetInt("Offset", CurTriOffset);
                    MeshFunctions.SetInt("Count", RenderQue[i].TriBuffer.count);
                    MeshFunctions.SetBuffer(TriangleBufferKernel, "InCudaTriArray", RenderQue[i].TriBuffer);
                    MeshFunctions.Dispatch(TriangleBufferKernel, (int)Mathf.Ceil(RenderQue[i].TriBuffer.count / 248.0f), 1, 1);

                    MeshFunctions.SetInt("Offset", CurNodeOffset);
                    MeshFunctions.SetInt("Count", RenderQue[i].BVHBuffer.count);
                    MeshFunctions.SetBuffer(NodeBufferKernel, "InAggNodes", RenderQue[i].BVHBuffer);
                    MeshFunctions.Dispatch(NodeBufferKernel, (int)Mathf.Ceil(RenderQue[i].BVHBuffer.count / 248.0f), 1, 1);
                    if(!RenderQue[i].IsSkinnedGroup) RenderQue[i].Release();
                    ToIllumTriBuffer.AddRange(RenderQue[i].ToIllumTriBuffer);

                    RenderQue[i].NodeOffset = CurNodeOffset;
                    RenderQue[i].TriOffset = CurTriOffset;
                    CurNodeOffset += RenderQue[i].AggNodes.Length;
                    CurTriOffset += RenderQue[i].AggTriangles.Length;

                    if(RenderQue[i].LightCount != 0) {
                        LightMeshCount++;
                        CDF += RenderQue[i].TotEnergy;
                        LightTransforms.Add(RenderQue[i].transform);
                        LightMeshes.Add(new LightMeshData() {
                            energy = RenderQue[i].TotEnergy,
                            CDF = CDF,
                            StartIndex = AggLightTriangles.Count,
                            IndexEnd = RenderQue[i].LightCount + AggLightTriangles.Count     
                          });
                        AggLightTriangles.AddRange(RenderQue[i].LightTriangles);
                    }
                }
                for(int i = 0; i < InstanceData.RenderQue.Count; i++) {
                    InstanceData.RenderQue[i].UpdateData(ref BVHOffset, ref MaterialOffset);
                    InstanceData.RenderQue[i].SetUpBuffers();
                    MeshFunctions.SetInt("Offset", CurTriOffset);
                    MeshFunctions.SetInt("Count", InstanceData.RenderQue[i].TriBuffer.count);
                    MeshFunctions.SetBuffer(TriangleBufferKernel, "InCudaTriArray", InstanceData.RenderQue[i].TriBuffer);
                    MeshFunctions.Dispatch(TriangleBufferKernel, (int)Mathf.Ceil(InstanceData.RenderQue[i].TriBuffer.count / 248.0f), 1, 1);

                    MeshFunctions.SetInt("Offset", CurNodeOffset);
                    MeshFunctions.SetInt("Count", InstanceData.RenderQue[i].BVHBuffer.count);
                    MeshFunctions.SetBuffer(NodeBufferKernel, "InAggNodes", InstanceData.RenderQue[i].BVHBuffer);
                    MeshFunctions.Dispatch(NodeBufferKernel, (int)Mathf.Ceil(InstanceData.RenderQue[i].BVHBuffer.count / 248.0f), 1, 1);
                    if(!InstanceData.RenderQue[i].IsSkinnedGroup) InstanceData.RenderQue[i].Release();
                    ToIllumTriBuffer.AddRange(InstanceData.RenderQue[i].ToIllumTriBuffer);

                    InstanceData.RenderQue[i].NodeOffset = CurNodeOffset;
                    InstanceData.RenderQue[i].TriOffset = CurTriOffset;
                    CurNodeOffset += InstanceData.RenderQue[i].AggNodes.Length;
                    CurTriOffset += InstanceData.RenderQue[i].AggTriangles.Length;
                }
                for(int i = 0; i < InstanceRenderQue.Count; i++) {
                    if(InstanceRenderQue[i].InstanceParent.LightCount != 0) {
                        LightMeshCount++;
                        CDF += InstanceRenderQue[i].InstanceParent.TotEnergy;
                        LightTransforms.Add(InstanceRenderQue[i].transform);
                        LightMeshes.Add(new LightMeshData() {
                            energy = InstanceRenderQue[i].InstanceParent.TotEnergy,
                            CDF = CDF,
                            StartIndex = AggLightTriangles.Count,
                            IndexEnd = InstanceRenderQue[i].InstanceParent.LightCount + AggLightTriangles.Count     
                          });
                        AggLightTriangles.AddRange(InstanceRenderQue[i].InstanceParent.LightTriangles);
                    }
                }
            }
            GPUOctree = new List<GPUOctreeNode>();
            for(int i = 0; i < VoxelRenderQue.Count; i++) {
                GPUOctree.AddRange(VoxelRenderQue[i].GPUOctree);
            }
            if(GPUOctree.Count == 0) {
                UseVoxels = false;
                GPUOctree.Add(new GPUOctreeNode());
            } else {
                UseVoxels = true;
            }
            if(LightMeshCount == 0) {LightMeshes.Add(new LightMeshData() {});}
            if(AggLightTriangles.Count == 0) {AggLightTriangles.Add(new CudaLightTriangle() {}); LightTriCount = 0;} else {LightTriCount = AggLightTriangles.Count;}


           if(!OnlyInstanceUpdated || _Materials.Count == 0) CreateAtlas();
        }
        ParentCountHasChanged = false;
        if(UseSkinning && didstart) { 
            for(int i = 0; i < ParentsLength; i++) {
                if(RenderQue[i].IsSkinnedGroup) {
                    RenderQue[i].RefitMesh(ref BVH8AggregatedBuffer);
                    MeshFunctions.SetInt("Offset", RenderQue[i].TriOffset);
                    MeshFunctions.SetInt("Count", RenderQue[i].TriBuffer.count);
                    MeshFunctions.SetBuffer(TriangleBufferKernel, "InCudaTriArray", RenderQue[i].TriBuffer);
                    MeshFunctions.Dispatch(TriangleBufferKernel, (int)Mathf.Ceil(RenderQue[i].TriBuffer.count / 248.0f), 1, 1);
                }
            }
        }
    }

    unsafe void Aggregate(ref BVH8Builder BVH8) {//BVH aggregation/BVH compression
        List<BVHNode8DataCompressed> TempBVHArray = new List<BVHNode8DataCompressed>();
        for(int i = 0; i < BVH8.BVH8Nodes.Length; ++i) {
            BVHNode8Data TempNode = BVH8.BVH8Nodes[i];

            TempBVHArray.Add(new BVHNode8DataCompressed() {
                node_0xyz = new Vector3(TempNode.p.x, TempNode.p.y, TempNode.p.z),
                node_0w = (TempNode.e[0] | (TempNode.e[1] << 8) | (TempNode.e[2] << 16) | (TempNode.imask << 24)),
                node_1x = TempNode.base_index_child,
                node_1y = TempNode.base_index_triangle,
                node_1z = (uint)(TempNode.meta[0] | (TempNode.meta[1] << 8) | (TempNode.meta[2] << 16) | (TempNode.meta[3] << 24)),
                node_1w = (uint)(TempNode.meta[4] | (TempNode.meta[5] << 8) | (TempNode.meta[6] << 16) | (TempNode.meta[7] << 24)),
                node_2x = (uint)(TempNode.quantized_min_x[0] | (TempNode.quantized_min_x[1] << 8) | (TempNode.quantized_min_x[2] << 16) | (TempNode.quantized_min_x[3] << 24)),
                node_2y = (uint)(TempNode.quantized_min_x[4] | (TempNode.quantized_min_x[5] << 8) | (TempNode.quantized_min_x[6] << 16) | (TempNode.quantized_min_x[7] << 24)),
                node_2z = (uint)(TempNode.quantized_max_x[0] | (TempNode.quantized_max_x[1] << 8) | (TempNode.quantized_max_x[2] << 16) | (TempNode.quantized_max_x[3] << 24)),
                node_2w = (uint)(TempNode.quantized_max_x[4] | (TempNode.quantized_max_x[5] << 8) | (TempNode.quantized_max_x[6] << 16) | (TempNode.quantized_max_x[7] << 24)),
                node_3x = (uint)(TempNode.quantized_min_y[0] | (TempNode.quantized_min_y[1] << 8) | (TempNode.quantized_min_y[2] << 16) | (TempNode.quantized_min_y[3] << 24)),
                node_3y = (uint)(TempNode.quantized_min_y[4] | (TempNode.quantized_min_y[5] << 8) | (TempNode.quantized_min_y[6] << 16) | (TempNode.quantized_min_y[7] << 24)),
                node_3z = (uint)(TempNode.quantized_max_y[0] | (TempNode.quantized_max_y[1] << 8) | (TempNode.quantized_max_y[2] << 16) | (TempNode.quantized_max_y[3] << 24)),
                node_3w = (uint)(TempNode.quantized_max_y[4] | (TempNode.quantized_max_y[5] << 8) | (TempNode.quantized_max_y[6] << 16) | (TempNode.quantized_max_y[7] << 24)),
                node_4x = (uint)(TempNode.quantized_min_z[0] | (TempNode.quantized_min_z[1] << 8) | (TempNode.quantized_min_z[2] << 16) | (TempNode.quantized_min_z[3] << 24)),
                node_4y = (uint)(TempNode.quantized_min_z[4] | (TempNode.quantized_min_z[5] << 8) | (TempNode.quantized_min_z[6] << 16) | (TempNode.quantized_min_z[7] << 24)),
                node_4z = (uint)(TempNode.quantized_max_z[0] | (TempNode.quantized_max_z[1] << 8) | (TempNode.quantized_max_z[2] << 16) | (TempNode.quantized_max_z[3] << 24)),
                node_4w = (uint)(TempNode.quantized_max_z[4] | (TempNode.quantized_max_z[5] << 8) | (TempNode.quantized_max_z[6] << 16) | (TempNode.quantized_max_z[7] << 24))

            });
        }
        BVH8AggregatedBuffer.SetData(TempBVHArray,0,0,TempBVHArray.Count);
    }

    unsafe void AggregateTLAS(ref BVH8Builder BVH8) {//BVH aggregation/BVH compression
        VoxelTLAS = new List<BVHNode8DataCompressed>();
        for(int i = 0; i < BVH8.BVH8Nodes.Length; ++i) {
            BVHNode8Data TempNode = BVH8.BVH8Nodes[i];

            VoxelTLAS.Add(new BVHNode8DataCompressed() {
                node_0xyz = new Vector3(TempNode.p.x, TempNode.p.y, TempNode.p.z),
                node_0w = (TempNode.e[0] | (TempNode.e[1] << 8) | (TempNode.e[2] << 16) | (TempNode.imask << 24)),
                node_1x = TempNode.base_index_child,
                node_1y = TempNode.base_index_triangle,
                node_1z = (uint)(TempNode.meta[0] | (TempNode.meta[1] << 8) | (TempNode.meta[2] << 16) | (TempNode.meta[3] << 24)),
                node_1w = (uint)(TempNode.meta[4] | (TempNode.meta[5] << 8) | (TempNode.meta[6] << 16) | (TempNode.meta[7] << 24)),
                node_2x = (uint)(TempNode.quantized_min_x[0] | (TempNode.quantized_min_x[1] << 8) | (TempNode.quantized_min_x[2] << 16) | (TempNode.quantized_min_x[3] << 24)),
                node_2y = (uint)(TempNode.quantized_min_x[4] | (TempNode.quantized_min_x[5] << 8) | (TempNode.quantized_min_x[6] << 16) | (TempNode.quantized_min_x[7] << 24)),
                node_2z = (uint)(TempNode.quantized_max_x[0] | (TempNode.quantized_max_x[1] << 8) | (TempNode.quantized_max_x[2] << 16) | (TempNode.quantized_max_x[3] << 24)),
                node_2w = (uint)(TempNode.quantized_max_x[4] | (TempNode.quantized_max_x[5] << 8) | (TempNode.quantized_max_x[6] << 16) | (TempNode.quantized_max_x[7] << 24)),
                node_3x = (uint)(TempNode.quantized_min_y[0] | (TempNode.quantized_min_y[1] << 8) | (TempNode.quantized_min_y[2] << 16) | (TempNode.quantized_min_y[3] << 24)),
                node_3y = (uint)(TempNode.quantized_min_y[4] | (TempNode.quantized_min_y[5] << 8) | (TempNode.quantized_min_y[6] << 16) | (TempNode.quantized_min_y[7] << 24)),
                node_3z = (uint)(TempNode.quantized_max_y[0] | (TempNode.quantized_max_y[1] << 8) | (TempNode.quantized_max_y[2] << 16) | (TempNode.quantized_max_y[3] << 24)),
                node_3w = (uint)(TempNode.quantized_max_y[4] | (TempNode.quantized_max_y[5] << 8) | (TempNode.quantized_max_y[6] << 16) | (TempNode.quantized_max_y[7] << 24)),
                node_4x = (uint)(TempNode.quantized_min_z[0] | (TempNode.quantized_min_z[1] << 8) | (TempNode.quantized_min_z[2] << 16) | (TempNode.quantized_min_z[3] << 24)),
                node_4y = (uint)(TempNode.quantized_min_z[4] | (TempNode.quantized_min_z[5] << 8) | (TempNode.quantized_min_z[6] << 16) | (TempNode.quantized_min_z[7] << 24)),
                node_4z = (uint)(TempNode.quantized_max_z[0] | (TempNode.quantized_max_z[1] << 8) | (TempNode.quantized_max_z[2] << 16) | (TempNode.quantized_max_z[3] << 24)),
                node_4w = (uint)(TempNode.quantized_max_z[4] | (TempNode.quantized_max_z[5] << 8) | (TempNode.quantized_max_z[6] << 16) | (TempNode.quantized_max_z[7] << 24))

            });
        }

    }
    public struct AggData {
        public int AggIndexCount;
        public int AggNodeCount;
        public int MaterialOffset;
        public int mesh_data_bvh_offsets;
    }

//Better Bounding Box Transformation by Zuex(I got it from Zuen)
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


public void CreateAABB(Transform transform, ref AABB aabb) {//Update the Transformed AABB by getting the new Max/Min of the untransformed AABB after transforming it
    Vector3 center = 0.5f * (aabb.BBMin + aabb.BBMax);
    Vector3 extent = 0.5f * (aabb.BBMax - aabb.BBMin);

    Vector3 new_center = transform_position (transform.worldToLocalMatrix.inverse, center);
    Vector3 new_extent = transform_direction(abs(transform.worldToLocalMatrix.inverse), extent);

    aabb.BBMin = new_center - new_extent;
    aabb.BBMax = new_center + new_extent;
}

    public bool UpdateTLAS() {  //Allows for objects to be moved in the scene or animated while playing 
        
        bool LightsHaveUpdated = false;
        TempBuild();
        if(!didstart) {
            didstart = true;

        }

        UnityLights.Clear();
        float CDF = 0.0f;//(LightMeshes.Count != 0) ? LightMeshes[LightMeshes.Count - 1].CDF : 0.0f;
        UnityLightCount = 0;
        foreach(RayTracingLights RayLight in RayTracingMaster._rayTracingLights) {
            UnityLightCount++;
            RayLight.UpdateLight();
            CDF += RayLight.Energy;
            if(RayLight.Type == 1) SunDirection = RayLight.Direction;
            UnityLights.Add(new LightData() {
                Radiance = RayLight.Emission,
                Position = RayLight.Position,
                Direction = RayLight.Direction,
                energy = RayLight.Energy,
                CDF = CDF,
                Type = RayLight.Type,
                SpotAngle = RayLight.SpotAngle
                });
        }
        if(UnityLights.Count == 0) {UnityLights.Add(new LightData() {});}
        if(PrevLightCount != RayTracingMaster._rayTracingLights.Count) LightsHaveUpdated = true;
        PrevLightCount = RayTracingMaster._rayTracingLights.Count;
       
        MyMeshesCompacted.Clear();     
        int MeshDataCount =  RenderQue.Count;
        AggData[] Aggs = new AggData[InstanceData.RenderQue.Count];
        int aggregated_bvh_node_count = 2 * (MeshDataCount + InstanceRenderQue.Count);
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
                MaterialOffset = MatOffset,
                IsVoxel = 0,
                SizeX = 0,
                SizeY = 0,
                SizeZ = 0,
              });
             MatOffset += RenderQue[i].MatOffset;
            MeshAABBs[i] = RenderQue[i].aabb;
            AggNodeCount += RenderQue[i].AggBVHNodeCount;//Can I replace this with just using aggregated_bvh_node_count below?
            AggTriCount += RenderQue[i].AggIndexCount;
            aggregated_bvh_node_count += RenderQue[i].BVH.cwbvhnode_count;
        }
        for(int i = 0; i < InstanceData.RenderQue.Count; i++) {
             Aggs[i].AggIndexCount = AggTriCount;
             Aggs[i].AggNodeCount = AggNodeCount;
             Aggs[i].MaterialOffset = MatOffset;
             Aggs[i].mesh_data_bvh_offsets = aggregated_bvh_node_count;
            MatOffset += InstanceData.RenderQue[i].MatOffset;
            AggNodeCount += InstanceData.RenderQue[i].AggBVHNodeCount;//Can I replace this with just using aggregated_bvh_node_count below?
            AggTriCount += InstanceData.RenderQue[i].AggIndexCount;
            aggregated_bvh_node_count += InstanceData.RenderQue[i].BVH.cwbvhnode_count;            
        }
        for(int i = 0; i < InstanceRenderQue.Count; i++) {
             int Index = InstanceData.RenderQue.IndexOf(InstanceRenderQue[i].InstanceParent);
             MyMeshesCompacted.Add(new MyMeshDataCompacted() {
                mesh_data_bvh_offsets = Aggs[Index].mesh_data_bvh_offsets,
                Transform = InstanceRenderQue[i].transform.worldToLocalMatrix,
                Inverse = InstanceRenderQue[i].transform.worldToLocalMatrix.inverse,
                Center = InstanceRenderQue[i].transform.position,
                AggIndexCount = Aggs[Index].AggIndexCount,
                AggNodeCount = Aggs[Index].AggNodeCount,
                MaterialOffset = Aggs[Index].MaterialOffset,
                IsVoxel = 0,
                SizeX = 0,
                SizeY = 0,
                SizeZ = 0,
              });
             AABB aabb = InstanceRenderQue[i].InstanceParent.aabb_untransformed;
             CreateAABB(InstanceRenderQue[i].transform, ref aabb);
            MeshAABBs[RenderQue.Count + i] = aabb;
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
        VoxOffset = MyMeshesCompacted.Count;
        AggNodeCount = 0;
        if(VoxelRenderQue.Count != 0) UseVoxels = true;
        else {
            UseVoxels = false;
        } 
        List<MyMeshDataCompacted> TempMeshCompacted = new List<MyMeshDataCompacted>();
        for(int i = 0; i < VoxelRenderQue.Count; i++) {
             TempMeshCompacted.Add(new MyMeshDataCompacted() {
                mesh_data_bvh_offsets = 0,
                Transform = VoxelRenderQue[i].transform.worldToLocalMatrix,
                Inverse = VoxelRenderQue[i].transform.worldToLocalMatrix.inverse,
                Center = VoxelRenderQue[i].transform.position,
                AggIndexCount = AggTriCount,
                AggNodeCount = AggNodeCount,
                MaterialOffset = MatOffset,
                IsVoxel = 1,
                SizeX = (int)VoxelRenderQue[i].Size.x,
                SizeY = (int)VoxelRenderQue[i].Size.y,
                SizeZ = (int)VoxelRenderQue[i].Size.z,
                MaxAxis = (int)VoxelRenderQue[i].LargestAxis
              });
             AggTriCount += VoxelRenderQue[i].GPUOctree.Length;
             AggNodeCount += VoxelRenderQue[i].GPUOctree.Length;
             MatOffset += VoxelRenderQue[i]._Materials.Count;
             VoxelRenderQue[i].UpdateAABB();
             VoxelAABBs[i] = VoxelRenderQue[i].aabb;

        }
        if(VoxelRenderQue.Count != 0) {
            BVH2 = new BVH2Builder(VoxelAABBs);
            TLASBVH8 = new BVH8Builder(BVH2, ref TempMeshCompacted);
            MyMeshesCompacted.AddRange(TempMeshCompacted);
            AggregateTLAS(ref TLASBVH8);
        } else {
            VoxelTLAS = new List<BVHNode8DataCompressed>();
            VoxelTLAS.Add(new BVHNode8DataCompressed());
        }

        return (LightsHaveUpdated || ChildrenUpdated);//The issue is that all light triangle indices start at 0, and thus might not get correctly sorted for indices
    }

    public void UpdateMaterials() {//Allows for live updating of material properties of any object
        int ParentCount = RenderQue.Count;
        RayTracingObject CurrentMaterial;
        MaterialData TempMat;
        for(int i = 0; i < ParentCount; i++) {
            int ChildCount = RenderQue[i].ChildObjects.Length;
            for(int i2 = 0; i2 < ChildCount; i2++) {
                CurrentMaterial = RenderQue[i].ChildObjects[i2];
                int MaterialCount = CurrentMaterial.MaterialIndex.Length;
                for(int i3 = 0; i3 < MaterialCount; i3++) {
                    TempMat = _Materials[CurrentMaterial.MaterialIndex[i3]];
                    if(TempMat.HasAlbedoTex != 1) {
                        TempMat.BaseColor = CurrentMaterial.BaseColor[i3];
                    }
                    TempMat.emmissive = CurrentMaterial.emmission[i3];
                    TempMat.Roughness = ((int)CurrentMaterial.MaterialOptions[i3] != 1) ? CurrentMaterial.Roughness[i3] : Mathf.Max(CurrentMaterial.Roughness[i3], 0.000001f);
                    TempMat.eta = CurrentMaterial.eta[i3];
                    TempMat.MatType = (int)CurrentMaterial.MaterialOptions[i3];
                    TempMat.EmissionColor = CurrentMaterial.EmissionColor[i3];
                    _Materials[CurrentMaterial.MaterialIndex[i3]] = TempMat;
                }
            }
        }
        ParentCount = InstanceData.RenderQue.Count;
        for(int i = 0; i < ParentCount; i++) {
            int ChildCount = InstanceData.RenderQue[i].ChildObjects.Length;
            for(int i2 = 0; i2 < ChildCount; i2++) {
                CurrentMaterial = InstanceData.RenderQue[i].ChildObjects[i2];
                int MaterialCount = CurrentMaterial.MaterialIndex.Length;
                for(int i3 = 0; i3 < MaterialCount; i3++) {
                    TempMat = _Materials[CurrentMaterial.MaterialIndex[i3]];
                    if(TempMat.HasAlbedoTex != 1) {
                        TempMat.BaseColor = CurrentMaterial.BaseColor[i3];
                    }
                    TempMat.emmissive = CurrentMaterial.emmission[i3];
                    TempMat.Roughness = ((int)CurrentMaterial.MaterialOptions[i3] != 1) ? CurrentMaterial.Roughness[i3] : Mathf.Max(CurrentMaterial.Roughness[i3], 0.000001f);
                    TempMat.eta = CurrentMaterial.eta[i3];
                    TempMat.MatType = (int)CurrentMaterial.MaterialOptions[i3];
                    TempMat.EmissionColor = CurrentMaterial.EmissionColor[i3];
                    _Materials[CurrentMaterial.MaterialIndex[i3]] = TempMat;
                }
            }
        }
    }

     /*    int Time = 0;
         public void OnDrawGizmos() {
             Time += 1;
             if(Time > 125) Time = 0;
         for(int i = 0; i < 1024; i++) {
         //    Debug.Log(RayTracingMaster.TempData[i].Misc.y);
            Vector3 Point = LightMeshes[(int)RayTracingMaster.TempData[i].Misc.x].Inverse * RayTracingMaster.TempData[i].Pos;
            Gizmos.DrawSphere(Point + LightMeshes[(int)RayTracingMaster.TempData[i].Misc.x].Center, 0.05f);
             CudaLightTriangle TempTri = AggLightTriangles[(int)RayTracingMaster.TempData[i].Misc.y];
             Vector3 V1 = TempTri.pos0;
             Vector3 V2 = V1 + TempTri.posedge1;
             Vector3 V3 = V1 + TempTri.posedge2;
            V1 = LightMeshes[(int)RayTracingMaster.TempData[i].Misc.x].Inverse * V1;
            V2 = LightMeshes[(int)RayTracingMaster.TempData[i].Misc.x].Inverse * V2;
            V3 = LightMeshes[(int)RayTracingMaster.TempData[i].Misc.x].Inverse * V3;
            V1 += LightMeshes[(int)RayTracingMaster.TempData[i].Misc.x].Center;
            V2 += LightMeshes[(int)RayTracingMaster.TempData[i].Misc.x].Center;
            V3 += LightMeshes[(int)RayTracingMaster.TempData[i].Misc.x].Center;
             Gizmos.DrawLine(V1, V2);
             Gizmos.DrawLine(V1, V3);
             Gizmos.DrawLine(V3, V2);
         }
     }*/


}
