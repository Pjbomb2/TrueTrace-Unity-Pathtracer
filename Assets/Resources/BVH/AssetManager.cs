using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;
using System.Threading;
using RectpackSharp;

[System.Serializable]
public class AssetManager : MonoBehaviour {//This handels all the data
    public RenderTexture AlbedoAtlas;
    public RenderTexture NormalAtlas;
    public RenderTexture EmissiveAtlas;
    public RenderTexture MetalAlphaRoughnessAtlas;
    public List<Vector4> VoxelPositions;
    private ComputeShader Refitter;
    private int RefitLayer;
    private int NodeUpdater;
    private int NodeCompress;
    private int NodeInitializerKernel;

    private Material Copymaterial;
    private ComputeShader CopyShader;

    public List<RayTracingObject> MaterialsChanged;
    public List<MaterialData> _Materials;
    [HideInInspector] public ComputeBuffer BVH8AggregatedBuffer;
    [HideInInspector] public ComputeBuffer AggTriBuffer;
    public BVHNode8DataCompressed[] VoxelTLAS;
    public List<MyMeshDataCompacted> MyMeshesCompacted;
     public List<CudaLightTriangle> AggLightTriangles;
     public List<int> ToIllumTriBuffer;
    [HideInInspector] public List<LightData> UnityLights;
    public List<Light> Lights;
    public InstancedManager InstanceData;
    public List<InstancedObject> Instances;

    public List<TerrainObject> Terrains;
    public List<TerrainDat> TerrainInfos;
    public ComputeBuffer TerrainBuffer;
    public Texture2D HeightMap;
    public Texture2D AlphaMap;


    public ComputeShader MeshFunctions;
    private int TriangleBufferKernel;
    private int NodeBufferKernel;

    public List<ParentObject> RenderQue;
    public List<ParentObject> BuildQue;
    public List<ParentObject> AddQue;
    public List<ParentObject> RemoveQue;
    public List<ParentObject> UpdateQue;

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
    [HideInInspector] public int DesiredRes = 4096;

    public int MatCount;

    public List<LightMeshData> LightMeshes;

    [HideInInspector] public AABB[] MeshAABBs;
    [HideInInspector] public AABB[] VoxelAABBs;

    [HideInInspector] public bool ParentCountHasChanged;    
    
    [HideInInspector] public int TLASSpace;
    [HideInInspector] public int LightTriCount;
    [HideInInspector] public int LightMeshCount;
    [HideInInspector] public int UnityLightCount;
    
    [HideInInspector] public List<GPUOctreeNode> GPUOctree;
    [HideInInspector] public List<int> GPUBrickmap;
    [HideInInspector] public int VoxOffset;
    private int PrevLightCount;
    [HideInInspector] public BVH2Builder BVH2;

    [HideInInspector] public bool UseSkinning = true;
    [HideInInspector] public bool HasStart = false;
    [HideInInspector] public bool didstart = false;
    [HideInInspector] public bool UseVoxels;
    [HideInInspector] public bool ChildrenUpdated;

    [HideInInspector] public Vector3 SunDirection;

    private BVHNode8DataCompressed[] TempBVHArray;

    [SerializeField] public int RunningTasks;
    public void ClearAll() {//My attempt at clearing memory
        ParentObject[] ChildrenObjects = this.GetComponentsInChildren<ParentObject>();
        foreach(ParentObject obj in ChildrenObjects)
            obj.ClearAll();
        VoxelObject[] ChildVoxelObjects = this.GetComponentsInChildren<VoxelObject>();
        foreach(VoxelObject obj in ChildVoxelObjects)
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
        DestroyImmediate(MetalAlphaRoughnessAtlas);  
        if(BVH8AggregatedBuffer != null) {
            BVH8AggregatedBuffer.Release();
            BVH8AggregatedBuffer = null;    
            AggTriBuffer.Release();
            AggTriBuffer = null;    
        }


        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    private List<Texture2D> AlbedoTexs;
    public List<RayObjects> AlbedoIndexes;
    private List<Texture2D> NormalTexs;
    private List<RayObjects> NormalIndexes;
    private List<Texture2D> MetallicTexs;
    private List<RayObjects> MetallicIndexes;
    private List<Texture2D> RoughnessTexs;
    private List<RayObjects> RoughnessIndexes;
    private List<Texture2D> EmissiveTexs;
    private List<RayObjects> EmissiveIndexes;

    private void AddTextures(ref List<Texture2D> Texs, ref List<RayObjects> Indexes, ref List<RayObjects> ObjIndexes, ref List<Texture> ObjTexs) {
        int NewLength = ObjTexs.Count;
        int PrevLength = Texs.Count;
        for(int i = 0; i < NewLength; i++) {
            int Index = Texs.IndexOf((Texture2D)ObjTexs[i], 0, PrevLength);
            if(Index == -1) {
                Texs.Add((Texture2D)ObjTexs[i]);
                Indexes.Add(ObjIndexes[i]);
            } else {
                Indexes[Index].RayObjectList.AddRange(ObjIndexes[i].RayObjectList);
            }
        }
    }

    private void ModifyTextureBounds(ref Rect[] Rects, int TexLength, ref List<RayObjects> Indexes, int TargetTex) {
            int TerrainIndexOffset = 0;
            for(int i = 0; i < TexLength; i++) {
                int SecondaryLength = Indexes[i].RayObjectList.Count;
                for(int i2 = 0; i2 < SecondaryLength; i2++) {
                    MaterialData TempMat = Indexes[i].RayObjectList[i2].Obj == null ? 
                    _Materials[Indexes[i].RayObjectList[i2].Terrain.MaterialIndex[Indexes[i].RayObjectList[i2].ObjIndex] + MatCount + TerrainIndexOffset]
                    : _Materials[Indexes[i].RayObjectList[i2].Obj.MaterialIndex[Indexes[i].RayObjectList[i2].ObjIndex]];
                    switch(TargetTex) {
                        case 0: 
                            TempMat.AlbedoTex = new Vector4(Rects[i].xMax, Rects[i].yMax, Rects[i].xMin, Rects[i].yMin);
                            TempMat.HasAlbedoTex = 1;
                        break;
                        case 1: 
                            TempMat.NormalTex = new Vector4(Rects[i].xMax, Rects[i].yMax, Rects[i].xMin, Rects[i].yMin);
                            TempMat.HasNormalTex = 1;
                        break;
                        case 2: 
                            TempMat.MetallicTex = new Vector4(Rects[i].xMax, Rects[i].yMax, Rects[i].xMin, Rects[i].yMin);
                            TempMat.HasMetallicTex = 1;
                        break;
                        case 3: 
                            TempMat.RoughnessTex = new Vector4(Rects[i].xMax, Rects[i].yMax, Rects[i].xMin, Rects[i].yMin);
                            TempMat.HasRoughnessTex = 1;
                        break;
                        case 4: 
                            TempMat.EmissiveTex = new Vector4(Rects[i].xMax, Rects[i].yMax, Rects[i].xMin, Rects[i].yMin);
                            TempMat.HasEmissiveTex = 1;
                        break;
                        default:
                            Debug.Log("EEEEE");
                        break;
                    }
                    _Materials[Indexes[i].RayObjectList[i2].Obj == null ? (Indexes[i].RayObjectList[i2].Terrain.MaterialIndex[Indexes[i].RayObjectList[i2].ObjIndex] + MatCount + TerrainIndexOffset) : Indexes[i].RayObjectList[i2].Obj.MaterialIndex[Indexes[i].RayObjectList[i2].ObjIndex]] = TempMat;
                }
            }
    }

    private void ConstructAtlas(List<Texture2D> Texs, ref RenderTexture Atlas, out Rect[] Rects, int DesiredRes, bool IsNormalMap) {
            PackingRectangle[] rectangles = new PackingRectangle[Texs.Count];
            for(int i = 0; i < Texs.Count; i++) {
                rectangles[i].Width = (uint)Texs[i].width;
                rectangles[i].Height = (uint)Texs[i].height;
                rectangles[i].Id = i;
            }   
            PackingRectangle BoundRects;
            RectanglePacker.Pack(rectangles, out BoundRects);
            DesiredRes = (int)Mathf.Min(Mathf.Max(BoundRects.Width, BoundRects.Height), DesiredRes);
            CreateRenderTexture(ref Atlas, DesiredRes, DesiredRes);
            Rects = new Rect[Texs.Count];
            for(int i = 0; i < Texs.Count; i++) {
                Rects[rectangles[i].Id].width = rectangles[i].Width;
                Rects[rectangles[i].Id].height = rectangles[i].Height;
                Rects[rectangles[i].Id].x = rectangles[i].X;
                Rects[rectangles[i].Id].y = rectangles[i].Y;
            }   

            Vector2 Scale = new Vector2(Mathf.Min((float)DesiredRes / BoundRects.Width, 1),Mathf.Min((float)DesiredRes / BoundRects.Height, 1));
            CopyShader.SetBool("IsNormalMap", IsNormalMap);
            for(int i = 0; i < Texs.Count; i++) {
                CopyShader.SetVector("InputSize", new Vector2(Rects[i].width, Rects[i].height));
                CopyShader.SetVector("OutputSize", new Vector2(Atlas.width, Atlas.height));
                CopyShader.SetVector("Scale", Scale);
                CopyShader.SetVector("Offset", new Vector2(Rects[i].x, Rects[i].y));
                CopyShader.SetTexture(0, "AdditionTex", Texs[i]);
                CopyShader.SetTexture(0, "Result", Atlas);
                CopyShader.Dispatch(0, (int)Mathf.CeilToInt(Rects[i].width * Scale.x / 32.0f), (int)Mathf.CeilToInt(Rects[i].height * Scale.y / 32.0f), 1);

                Rects[i].width = Mathf.Floor(Rects[i].width * Scale.x) / Atlas.width;
                Rects[i].x = Mathf.Floor(Rects[i].x * Scale.x) / Atlas.width;
                Rects[i].height = Mathf.Floor(Rects[i].height * Scale.y) / Atlas.height;
                Rects[i].y = Mathf.Floor(Rects[i].y * Scale.y) / Atlas.height;
            }
    }

    private void AddToAtlas(List<Texture2D> Texs, ref RenderTexture Atlas, out Rect[] Rects, int DesiredRes, int ReadIndex, int WriteIndex) {
            PackingRectangle[] rectangles = new PackingRectangle[Texs.Count];
            for(int i = 0; i < Texs.Count; i++) {
                rectangles[i].Width = (uint)Texs[i].width;
                rectangles[i].Height = (uint)Texs[i].height;
                rectangles[i].Id = i;
            }   
            PackingRectangle BoundRects;
            RectanglePacker.Pack(rectangles, out BoundRects);
            if(Atlas.width == 1) CreateRenderTexture(ref Atlas, DesiredRes, DesiredRes);
            Rects = new Rect[Texs.Count];
            for(int i = 0; i < Texs.Count; i++) {
                Rects[rectangles[i].Id].width = rectangles[i].Width;
                Rects[rectangles[i].Id].height = rectangles[i].Height;
                Rects[rectangles[i].Id].x = rectangles[i].X;
                Rects[rectangles[i].Id].y = rectangles[i].Y;
            }   

            Vector2 Scale = new Vector2(Mathf.Min((float)DesiredRes / BoundRects.Width, 1),Mathf.Min((float)DesiredRes / BoundRects.Height, 1));
            CopyShader.SetInt("OutputRead", ReadIndex);
            CopyShader.SetInt("OutputWrite", WriteIndex);
            for(int i = 0; i < Texs.Count; i++) {
                CopyShader.SetVector("InputSize", new Vector2(Rects[i].width, Rects[i].height));
                CopyShader.SetVector("OutputSize", new Vector2(Atlas.width, Atlas.height));
                CopyShader.SetVector("Scale", Scale);
                CopyShader.SetVector("Offset", new Vector2(Rects[i].x, Rects[i].y));
                CopyShader.SetTexture(1, "AdditionTex", Texs[i]);
                CopyShader.SetTexture(1, "Result", Atlas);
                CopyShader.Dispatch(1, (int)Mathf.CeilToInt(Rects[i].width * Scale.x / 32.0f), (int)Mathf.CeilToInt(Rects[i].height * Scale.y / 32.0f), 1);

                Rects[i].width = Mathf.Floor(Rects[i].width * Scale.x) / Atlas.width;
                Rects[i].x = Mathf.Floor(Rects[i].x * Scale.x) / Atlas.width;
                Rects[i].height = Mathf.Floor(Rects[i].height * Scale.y) / Atlas.height;
                Rects[i].y = Mathf.Floor(Rects[i].y * Scale.y) / Atlas.height;
            }
    }

    private void CreateRenderTexture(ref RenderTexture ThisTex, int Width, int Height) {
        ThisTex = new RenderTexture(Width, Height, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        ThisTex.enableRandomWrite = true;
        ThisTex.Create();
    }
   private void CreateAtlas() {//Creates texture atlas

        _Materials = new List<MaterialData>();
        AlbedoIndexes = new List<RayObjects>();
        AlbedoTexs = new List<Texture2D>();
        NormalTexs = new List<Texture2D>();
        NormalIndexes = new List<RayObjects>();
        MetallicTexs = new List<Texture2D>();
        MetallicIndexes = new List<RayObjects>();
        RoughnessTexs = new List<Texture2D>();
        RoughnessIndexes = new List<RayObjects>();
        EmissiveTexs = new List<Texture2D>();
        EmissiveIndexes = new List<RayObjects>();
        List<Texture2D> HeightMaps = new List<Texture2D>();
        List<Texture2D> AlphaMaps = new List<Texture2D>();

        int TerrainCount = Terrains.Count;
        int MaterialOffset = 0;
        List<Vector2> Sizes = new List<Vector2>();
        TerrainInfos = new List<TerrainDat>();
        for(int i = 0; i < TerrainCount; i++) {
            TerrainDat TempTerrain = new TerrainDat();
            TempTerrain.PositionOffset = Terrains[i].transform.position;
            AlphaMaps.Add(Terrains[i].AlphaMap);
            HeightMaps.Add(Terrains[i].HeightMap);
            Sizes.Add(new Vector2(Terrains[i].HeightMap.width, Terrains[i].HeightMap.height));
            TempTerrain.TerrainDim = Terrains[i].TerrainDim;
            TempTerrain.HeightScale = Terrains[i].HeightScale;
            TempTerrain.MatOffset = MaterialOffset;
            MaterialOffset += Terrains[i].Materials.Count;
            TerrainInfos.Add(TempTerrain);
        }
        if(TerrainCount != 0) {
            Rect[] AlphaRects;
            List<Rect> HeightRects = new List<Rect>();
            float MinX = 0;
            float MinY = 0;
            for(int i = 0;i < Sizes.Count; i++) {
                MinX = Mathf.Max(Sizes[i].x, MinX);
                MinY += Sizes[i].y * 2;
            } 
            int Size = (int)Mathf.Min((MinY) / Mathf.Ceil(Mathf.Sqrt(Sizes.Count)), 16380);

            Texture2D.GenerateAtlas(Sizes.ToArray(), 0, Size, HeightRects);
            HeightMap = new Texture2D(Size, Size,  UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UNorm, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
            Color32[] Colors = HeightMap.GetPixels32(0);
            System.Array.Fill(Colors, Color.black);
            HeightMap.SetPixels32(Colors);
            HeightMap.Apply();
            AlphaRects = AlphaMap.PackTextures(AlphaMaps.ToArray(), 1, 16380, true);
            for(int i = 0; i < TerrainCount; i++) {
                Graphics.CopyTexture(HeightMaps[i], 0, 0, 0, 0, HeightMaps[i].width, HeightMaps[i].height, HeightMap, 0, 0, (int)HeightRects[i].xMin, (int)HeightRects[i].yMin);
                TerrainDat TempTerrain = TerrainInfos[i];
                TempTerrain.HeightMap = new Vector4(HeightRects[i].xMax / Size, HeightRects[i].yMax / Size, HeightRects[i].xMin / Size, HeightRects[i].yMin / Size);
                TempTerrain.AlphaMap = new Vector4(AlphaRects[i].xMax, AlphaRects[i].yMax, AlphaRects[i].xMin, AlphaRects[i].yMin);
                TerrainInfos[i] = TempTerrain;
            }
        }
        int CurCount = RenderQue[0].AlbedoTexs.Count;
        int CurLength;
        foreach(ParentObject Obj in RenderQue) {
            AddTextures(ref AlbedoTexs, ref AlbedoIndexes, ref Obj.AlbedoIndexes, ref Obj.AlbedoTexs);
            AddTextures(ref NormalTexs, ref NormalIndexes, ref Obj.NormalIndexes, ref Obj.NormalTexs);
            AddTextures(ref MetallicTexs, ref MetallicIndexes, ref Obj.MetallicIndexes, ref Obj.MetallicTexs);
            AddTextures(ref RoughnessTexs, ref RoughnessIndexes, ref Obj.RoughnessIndexes, ref Obj.RoughnessTexs);
            AddTextures(ref EmissiveTexs, ref EmissiveIndexes, ref Obj.EmissionIndexes, ref Obj.EmissionTexs);
        }
        foreach(ParentObject Obj in InstanceData.RenderQue) {
            AddTextures(ref AlbedoTexs, ref AlbedoIndexes, ref Obj.AlbedoIndexes, ref Obj.AlbedoTexs);
            AddTextures(ref NormalTexs, ref NormalIndexes, ref Obj.NormalIndexes, ref Obj.NormalTexs);
            AddTextures(ref MetallicTexs, ref MetallicIndexes, ref Obj.MetallicIndexes, ref Obj.MetallicTexs);
            AddTextures(ref RoughnessTexs, ref RoughnessIndexes, ref Obj.RoughnessIndexes, ref Obj.RoughnessTexs);
            AddTextures(ref EmissiveTexs, ref EmissiveIndexes, ref Obj.EmissionIndexes, ref Obj.EmissionTexs);
        }

        if(TerrainCount != 0) {
            for(int i = 0; i < TerrainCount; i++) {
                AddTextures(ref AlbedoTexs, ref AlbedoIndexes, ref Terrains[i].AlbedoIndexes, ref Terrains[i].AlbedoTexs);
                AddTextures(ref NormalTexs, ref NormalIndexes, ref Terrains[i].NormalIndexes, ref Terrains[i].NormalTexs);
                AddTextures(ref MetallicTexs, ref MetallicIndexes, ref Terrains[i].MetallicIndexes, ref Terrains[i].MetallicTexs);
            }
        }

        Rect[] AlbedoRects, NormalRects, EmissiveRects, MetallicRects, RoughnessRects;
        if(CopyShader == null) CopyShader = Resources.Load<ComputeShader>("Utility/CopyTextureShader");

        if(AlbedoAtlas != null) AlbedoAtlas.Release();
        if(NormalAtlas != null) NormalAtlas.Release();
        if(MetalAlphaRoughnessAtlas != null) MetalAlphaRoughnessAtlas.Release();
        if(EmissiveAtlas != null) EmissiveAtlas.Release();
        if(AlbedoTexs.Count != 0) ConstructAtlas(AlbedoTexs, ref AlbedoAtlas, out AlbedoRects, DesiredRes, false);
        else {AlbedoRects = new Rect[0]; CreateRenderTexture(ref AlbedoAtlas, 1, 1);}
        if(NormalTexs.Count != 0) ConstructAtlas(NormalTexs, ref NormalAtlas, out NormalRects, DesiredRes, true);
        else {NormalRects = new Rect[0]; CreateRenderTexture(ref NormalAtlas, 1, 1);}
        if(EmissiveTexs.Count != 0) ConstructAtlas(EmissiveTexs, ref EmissiveAtlas, out EmissiveRects, DesiredRes, false);
        else {EmissiveRects = new Rect[0]; CreateRenderTexture(ref EmissiveAtlas, 1, 1);}
        CreateRenderTexture(ref MetalAlphaRoughnessAtlas, 1, 1);
        if(MetallicTexs.Count != 0) AddToAtlas(MetallicTexs, ref MetalAlphaRoughnessAtlas, out MetallicRects, AlbedoAtlas.width, 0, 0);
        else {MetallicRects = new Rect[0];}
        if(AlbedoTexs.Count != 0) AddToAtlas(AlbedoTexs, ref MetalAlphaRoughnessAtlas, out AlbedoRects, AlbedoAtlas.width, 3, 1);
        else {AlbedoRects = new Rect[0];}
        if(RoughnessTexs.Count != 0) AddToAtlas(RoughnessTexs, ref MetalAlphaRoughnessAtlas, out RoughnessRects, AlbedoAtlas.width, 0, 2);
        else {RoughnessRects = new Rect[0];}

        MatCount = 0;

        foreach(ParentObject Obj in RenderQue) {
            foreach(RayTracingObject Obj2 in Obj.ChildObjects) {
                MaterialsChanged.Add(Obj2);
                for(int i = 0; i < Obj2.MaterialIndex.Length; i++) {
                    Obj2.MaterialIndex[i] = Obj2.LocalMaterialIndex[i] + MatCount;
                }
            }
            _Materials.AddRange(Obj._Materials);
            MatCount += Obj._Materials.Count;
        }
        foreach(ParentObject Obj in InstanceData.RenderQue) {
            foreach(RayTracingObject Obj2 in Obj.ChildObjects) {
                MaterialsChanged.Add(Obj2);
                for(int i = 0; i < Obj2.MaterialIndex.Length; i++) {
                    Obj2.MaterialIndex[i] = Obj2.LocalMaterialIndex[i] + MatCount;
                }
            }
            _Materials.AddRange(Obj._Materials);
            MatCount += Obj._Materials.Count;
        }
        int TerrainMaterials = 0;
        if(TerrainCount != 0) {
            foreach(TerrainObject Obj2 in Terrains) {
                for(int i = 0; i < Obj2.MaterialIndex.Length; i++) {
                    Obj2.MaterialIndex[i] += TerrainMaterials;
                }
                _Materials.AddRange(Obj2.Materials);
                TerrainMaterials += Obj2.Materials.Count;
            }
        }

        ModifyTextureBounds(ref AlbedoRects, AlbedoTexs.Count, ref AlbedoIndexes, 0);
        ModifyTextureBounds(ref NormalRects, NormalTexs.Count, ref NormalIndexes, 1);
        ModifyTextureBounds(ref MetallicRects, MetallicTexs.Count, ref MetallicIndexes, 2);
        ModifyTextureBounds(ref RoughnessRects, RoughnessTexs.Count, ref RoughnessIndexes, 3);
        ModifyTextureBounds(ref EmissiveRects, EmissiveTexs.Count, ref EmissiveIndexes, 4);

        AlbedoTexs.Clear();
        AlbedoTexs.TrimExcess();
        NormalTexs.Clear();
        NormalTexs.TrimExcess();
        MetallicTexs.Clear();
        MetallicTexs.TrimExcess();
        RoughnessTexs.Clear();
        RoughnessTexs.TrimExcess();
        EmissiveTexs.Clear();
        EmissiveTexs.TrimExcess();

        if(TerrainCount != 0) {
            if(TerrainBuffer != null) TerrainBuffer.Release();
            TerrainBuffer = new ComputeBuffer(TerrainCount, 56);
            TerrainBuffer.SetData(TerrainInfos);
        }

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
        if(WorkingBuffer != null) WorkingBuffer.Release();
        if(NodeBuffer != null) NodeBuffer.Release();
        if(StackBuffer != null) StackBuffer.Release();
        if(ToBVHIndexBuffer != null) ToBVHIndexBuffer.Release();
        if(BVHDataBuffer != null) BVHDataBuffer.Release();
        if(BVHBuffer != null) BVHBuffer.Release();
        if(BoxesBuffer != null) BoxesBuffer.Release();
        if(TerrainBuffer != null) TerrainBuffer.Release();
    }

    private void init() {
        Refitter =  Resources.Load<ComputeShader>("BVH/BVHRefitter");
        RefitLayer = Refitter.FindKernel("RefitBVHLayer");
        NodeUpdater = Refitter.FindKernel("NodeUpdate");
        NodeCompress = Refitter.FindKernel("NodeCompress");
        NodeInitializerKernel = Refitter.FindKernel("NodeInitializer");

        MaterialsChanged = new List<RayTracingObject>();
        InstanceData = GameObject.Find("InstancedStorage").GetComponent<InstancedManager>();
        Instances = new List<InstancedObject>();
        ToIllumTriBuffer = new List<int>();
        SunDirection = new Vector3(0,-1,0);
        AddQue = new List<ParentObject>();
        RemoveQue = new List<ParentObject>();
        RenderQue = new List<ParentObject>();
        UpdateQue = new List<ParentObject>();
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
        TerrainInfos = new List<TerrainDat>();
        if(BVH8AggregatedBuffer != null) {
            BVH8AggregatedBuffer.Release();
            BVH8AggregatedBuffer = null;    
            AggTriBuffer.Release();
            AggTriBuffer = null;    
        }
        MeshFunctions = Resources.Load<ComputeShader>("Utility/GeneralMeshFunctions");
        TriangleBufferKernel = MeshFunctions.FindKernel("CombineTriBuffers");
        NodeBufferKernel = MeshFunctions.FindKernel("CombineNodeBuffers");
        AlphaMap = Texture2D.blackTexture;
        HeightMap = Texture2D.blackTexture;
        if(TerrainBuffer != null) TerrainBuffer.Release();
        TerrainBuffer = new ComputeBuffer(1, 56);

        if(Terrains.Count != 0) for(int i = 0; i < Terrains.Count; i++) Terrains[i].Load();
    }

    private void UpdateRenderAndBuildQues() {
        ChildrenUpdated = false;
        OnlyInstanceUpdated = false;

        int AddQueCount = AddQue.Count - 1;
        int RemoveQueCount = RemoveQue.Count - 1;
        int RenderQueCount = 0;
        int BuildQueCount = 0;
        {//Main Object Data Handling
            for(int i = RemoveQueCount; i >= 0; i--) {
                if(RenderQue.Contains(RemoveQue[i]))
                    RenderQue.Remove(RemoveQue[i]);
                if(BuildQue.Contains(RemoveQue[i])) {
                    CurrentlyActiveTasks.RemoveAt(BuildQue.IndexOf(RemoveQue[i]));
                    BuildQue.Remove(RemoveQue[i]);
                } 
                if(UpdateQue.Contains(RemoveQue[i])) {
                    UpdateQue.Remove(RemoveQue[i]);
                } 
                if(AddQue.Contains(RemoveQue[i])) {
                    AddQue.Remove(RemoveQue[i]);
                }
                ChildrenUpdated = true;
                RemoveQue.RemoveAt(i); 
            }
            for(int i = AddQueCount; i >= 0; i--) {
                var CurrentRep = BuildQue.Count;
                BuildQue.Add(AddQue[i]);
                AddQue.RemoveAt(i);
                BuildQue[CurrentRep].LoadData();
                CurrentlyActiveTasks.Add(Task.Run(() => BuildQue[CurrentRep].BuildTotal()));
                ChildrenUpdated = true;
            }
            BuildQueCount = BuildQue.Count - 1;
            RenderQueCount = UpdateQue.Count - 1;
            for(int i = BuildQueCount; i >= 0; i--) {//Promotes from Build Que to Render Que
                if(CurrentlyActiveTasks[i].IsFaulted) {//Fuck, something fucked up
                    Debug.Log(CurrentlyActiveTasks[i].Exception + ", " + BuildQue[i].Name);
                    CurrentlyActiveTasks.RemoveAt(i);
                    AddQue.Add(BuildQue[i]);
                    BuildQue.RemoveAt(i);

                } else {
                    if(CurrentlyActiveTasks[i].Status == TaskStatus.RanToCompletion) {
                        if(BuildQue[i].AggTriangles == null) {
                            CurrentlyActiveTasks.RemoveAt(i);
                            AddQue.Add(BuildQue[i]);
                            BuildQue.RemoveAt(i);
                        } else {
                            BuildQue[i].SetUpBuffers();
                            RenderQue.Add(BuildQue[i]);
                            BuildQue.RemoveAt(i);
                            CurrentlyActiveTasks.RemoveAt(i);
                            ChildrenUpdated = true;
                        }
                    }
                }
            }
        }
        for(int i = RenderQueCount; i >= 0; i--) {//Demotes from Render Que to Build Que in case mesh has changed
            if(UpdateQue[i] != null && UpdateQue[i].gameObject.activeInHierarchy) {
                UpdateQue[i].ClearAll();
                UpdateQue[i].LoadData();
                if(!BuildQue.Contains(UpdateQue[i])) {
                    BuildQue.Add(UpdateQue[i]);
                    if(RenderQue.Contains(UpdateQue[i])) RenderQue.Remove(UpdateQue[i]);
                    var TempBuildQueCount = BuildQue.Count - 1;
                    CurrentlyActiveTasks.Add(Task.Run(() => BuildQue[TempBuildQueCount].BuildTotal()));
                }
                UpdateQue.RemoveAt(i);
                ChildrenUpdated = true;
            } else {
                UpdateQue.RemoveAt(i);
            }
        }
        AddQueCount = InstanceAddQue.Count - 1;
        RemoveQueCount = InstanceRemoveQue.Count - 1;
        {//Instanced Models Data Handling
            InstanceData.UpdateRenderAndBuildQues(ref ChildrenUpdated);
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
            BuildQueCount = InstanceBuildQue.Count - 1;
            for(int i = RenderQueCount; i >= 0; i--) {//Demotes from Render Que to Build Que in case mesh has changed
                // InstanceRenderQue[i].UpdateInstance();
                ParentObject InstanceParent = InstanceRenderQue[i].InstanceParent;
                if(InstanceParent == null || InstanceParent.gameObject.activeInHierarchy == false || InstanceParent.HasCompleted == false) {
                    InstanceBuildQue.Add(InstanceRenderQue[i]);
                    InstanceRenderQue.RemoveAt(i);
                    OnlyInstanceUpdated = true;
                }
            }
            for(int i = BuildQueCount; i >= 0; i--) {//Promotes from Build Que to Render Que
                if(InstanceBuildQue[i].InstanceParent.HasCompleted == true) {
                    InstanceRenderQue.Add(InstanceBuildQue[i]);
                    InstanceBuildQue.RemoveAt(i);
                    OnlyInstanceUpdated = true;
                }
            }
        }
        AddQueCount = VoxelAddQue.Count - 1;
        RemoveQueCount = VoxelRemoveQue.Count - 1;
        {//Voxel Que Data Handling
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
        }


        if(OnlyInstanceUpdated && !ChildrenUpdated) {
            ChildrenUpdated = true;
        } else {
            OnlyInstanceUpdated = false;
        }
        if(ChildrenUpdated || ParentCountHasChanged) MeshAABBs = new AABB[RenderQue.Count + InstanceRenderQue.Count];
        if(ChildrenUpdated || ParentCountHasChanged) VoxelAABBs = new AABB[VoxelRenderQue.Count];
    }
    public void EditorBuild() {//Forces all to rebuild
        ClearAll();
        Terrains = new List<TerrainObject>();
        Terrains = new List<TerrainObject>(GetComponentsInChildren<TerrainObject>());
        init();
        AddQue = new List<ParentObject>();
        RemoveQue = new List<ParentObject>();
        RenderQue = new List<ParentObject>();
        CurrentlyActiveTasks = new List<Task>();
        CurrentlyActiveVoxelTasks = new List<Task>();
        BuildQue = new List<ParentObject>(GetComponentsInChildren<ParentObject>());
        VoxelBuildQue = new List<VoxelObject>(GetComponentsInChildren<VoxelObject>());
        RunningTasks = 0;
        InstanceData.EditorBuild();
        for(int i = 0; i < BuildQue.Count; i++) {
            var CurrentRep = i;
            BuildQue[CurrentRep].LoadData();
            Task t1 = Task.Run(() => {BuildQue[CurrentRep].BuildTotal(); RunningTasks--;});
            RunningTasks++;
            CurrentlyActiveTasks.Add(t1);
        }
        for(int i = 0; i < VoxelBuildQue.Count; i++) {
            var CurrentRep = i;
            VoxelBuildQue[CurrentRep].LoadData();
        //VoxelBuildQue[CurrentRep].BuildOctree();
            Task t1 = Task.Run(() => VoxelBuildQue[CurrentRep].BuildOctree());
            CurrentlyActiveVoxelTasks.Add(t1);
        }
        if(AggLightTriangles.Count == 0) {AggLightTriangles.Add(new CudaLightTriangle() {});}
        didstart = false;
    }
    public void BuildCombined() {//Only has unbuilt be built
        HasToDo = false;
        Terrains = new List<TerrainObject>(GetComponentsInChildren<TerrainObject>());
        init();
        CurrentlyActiveVoxelTasks = new List<Task>();
        List<ParentObject> TempQue = new List<ParentObject>(GetComponentsInChildren<ParentObject>());
        InstanceAddQue = new List<InstancedObject>(GetComponentsInChildren<InstancedObject>());
        InstanceData.BuildCombined();
        List<VoxelObject> TempVoxelQue = new List<VoxelObject>(GetComponentsInChildren<VoxelObject>());
        for(int i = 0; i < TempQue.Count; i++) {
            if(TempQue[i].HasCompleted && !TempQue[i].NeedsToUpdate) RenderQue.Add(TempQue[i]);
            else BuildQue.Add(TempQue[i]);
        }
        for(int i = 0; i < TempVoxelQue.Count; i++) {
            if(TempVoxelQue[i].HasCompleted) VoxelRenderQue.Add(TempVoxelQue[i]);
            else VoxelBuildQue.Add(TempVoxelQue[i]);
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
        ParentCountHasChanged = true;
        if(AggLightTriangles.Count == 0) {AggLightTriangles.Add(new CudaLightTriangle() {});}
        if(RenderQue.Count != 0) {bool throwaway = UpdateTLAS();}
    }
    public bool HasToDo;

    private void AccumulateData() {
        UnityEngine.Profiling.Profiler.BeginSample("Update Object Lists");
        UpdateRenderAndBuildQues();
        UnityEngine.Profiling.Profiler.EndSample();

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
            Debug.Log("Total Tri Count: " + AggTriCount);
            if(AggNodeCount != 0) {//Accumulate the BVH nodes and triangles for all normal models
                BVH8AggregatedBuffer = new ComputeBuffer(AggNodeCount, 80);
                AggTriBuffer = new ComputeBuffer(AggTriCount, 136);
                MeshFunctions.SetBuffer(TriangleBufferKernel, "OutCudaTriArray", AggTriBuffer);
                MeshFunctions.SetBuffer(NodeBufferKernel, "OutAggNodes", BVH8AggregatedBuffer);
                for(int i = 0; i < ParentsLength; i++) {
                    RenderQue[i].UpdateData();

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

                    if(RenderQue[i].LightTriangles.Count != 0) {
                        LightMeshCount++;
                        CDF += RenderQue[i].TotEnergy;
                        LightTransforms.Add(RenderQue[i].transform);
                        LightMeshes.Add(new LightMeshData() {
                            energy = RenderQue[i].TotEnergy,
                            CDF = CDF,
                            StartIndex = AggLightTriangles.Count,
                            IndexEnd = RenderQue[i].LightTriangles.Count + AggLightTriangles.Count     
                          });
                        AggLightTriangles.AddRange(RenderQue[i].LightTriangles);
                    }
                }
                for(int i = 0; i < InstanceData.RenderQue.Count; i++) {//Accumulate the BVH nodes and triangles for all instanced models
                    InstanceData.RenderQue[i].UpdateData();
                    
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
            GPUBrickmap = new List<int>();
            for(int i = 0; i < VoxelRenderQue.Count; i++) {
             //   Debug.Log("SIZE: " + VoxelRenderQue[i].BrickmapTraverse.Count);
                GPUBrickmap.AddRange(VoxelRenderQue[i].BrickmapTraverse);
            }
            
            if(GPUBrickmap.Count == 0) {
                UseVoxels = false;
                GPUBrickmap.Add(0);
            } else {
                UseVoxels = true;
            }
            if(LightMeshCount == 0) {LightMeshes.Add(new LightMeshData() {});}
            if(AggLightTriangles.Count == 0) {AggLightTriangles.Add(new CudaLightTriangle() {}); LightTriCount = 0;} else {LightTriCount = AggLightTriangles.Count;}


           if(!OnlyInstanceUpdated || _Materials.Count == 0) CreateAtlas();
        }
        ParentCountHasChanged = false;
        if(UseSkinning && didstart) { 
            for(int i = 0; i < ParentsLength; i++) {//Refit BVH's of skinned meshes
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

    public struct AggData {
        public int AggIndexCount;
        public int AggNodeCount;
        public int MaterialOffset;
        public int mesh_data_bvh_offsets;
        public int LightTriCount;
    }


public void CreateAABB(Transform transform, ref AABB aabb) {//Update the Transformed AABB by getting the new Max/Min of the untransformed AABB after transforming it
    Vector3 center = 0.5f * (aabb.BBMin + aabb.BBMax);
    Vector3 extent = 0.5f * (aabb.BBMax - aabb.BBMin);
    Matrix4x4 Mat = transform.localToWorldMatrix;
    Vector3 new_center = CommonFunctions.transform_position (Mat, center);
    CommonFunctions.abs(ref Mat);
    Vector3 new_extent = CommonFunctions.transform_direction(Mat, extent);

    aabb.BBMin = new_center - new_extent;
    aabb.BBMax = new_center + new_extent;
}

AABB tempAABB;
int[] ToBVHIndex;
public BVH8Builder TLASBVH8;


unsafe public void DocumentNodes(int CurrentNode, int ParentNode, int NextNode, int NextBVH8Node, bool IsLeafRecur, int CurRecur) {
    NodeIndexPairData CurrentPair = NodePair[CurrentNode];
    MaxRecur = Mathf.Max(MaxRecur, CurRecur);
    CurrentPair.PreviousNode = ParentNode;
    CurrentPair.Node = CurrentNode;
    CurrentPair.RecursionCount = CurRecur;
    if(!IsLeafRecur) {
        ToBVHIndex[NextBVH8Node] = CurrentNode;
        CurrentPair.IsLeaf = 0;
        BVHNode8Data node = TLASBVH8.BVH8Nodes[NextBVH8Node];
        NodeIndexPairData IndexPair = new NodeIndexPairData();

        IndexPair.AABB = new AABB();
        float ex = (float)System.Convert.ToSingle((int)(System.Convert.ToUInt32(node.e[0]) << 23));
        float ey = (float)System.Convert.ToSingle((int)(System.Convert.ToUInt32(node.e[1]) << 23));
        float ez = (float)System.Convert.ToSingle((int)(System.Convert.ToUInt32(node.e[2]) << 23));
        Vector3 e = new Vector3(ex, ey, ez);
        for(int i = 0; i < 8; i++) {
            IndexPair.InNodeOffset = i;
            float AABBPos1x = node.quantized_max_x[i] * e.x + node.p.x;
            float AABBPos1y = node.quantized_max_y[i] * e.y + node.p.y;
            float AABBPos1z = node.quantized_max_z[i] * e.z + node.p.z;
            float AABBPos2x = node.quantized_min_x[i] * e.x + node.p.x;
            float AABBPos2y = node.quantized_min_y[i] * e.y + node.p.y;
            float AABBPos2z = node.quantized_min_z[i] * e.z + node.p.z;
            tempAABB.BBMax = new Vector3(AABBPos1x, AABBPos1y, AABBPos1z);
            tempAABB.BBMin = new Vector3(AABBPos2x, AABBPos2y, AABBPos2z);
            IndexPair.AABB.init();
            IndexPair.AABB.Extend(ref tempAABB);
            IndexPair.InNodeOffset = i;
            bool IsLeaf = (node.meta[i] & 0b11111) < 24;
            if(IsLeaf) {
                IndexPair.BVHNode = NextBVH8Node;
                NodePair.Add(IndexPair);
                NextNode++;
                DocumentNodes(NodePair.Count - 1, CurrentNode, NextNode, -1, true, CurRecur + 1);
            } else {
                int child_offset = (byte)node.meta[i] & 0b11111;
                int child_index  = (int)node.base_index_child + child_offset - 24;
                
                IndexPair.BVHNode = NextBVH8Node;
                NodePair.Add(IndexPair);
                NextNode++;
                DocumentNodes(NodePair.Count - 1, CurrentNode, NextNode, child_index, false, CurRecur + 1);
            }
        }
    } else {
        CurrentPair.IsLeaf = 1;
    }
    NodePair[CurrentNode] = CurrentPair;
}
List<BVHNode8DataFixed> SplitNodes; 
List<NodeIndexPairData> NodePair;
Layer[] ForwardStack;
Layer2[] LayerStack;
int MaxRecur = 0;
int[] CWBVHIndicesBufferInverted;
ComputeBuffer WorkingBuffer;
ComputeBuffer NodeBuffer;
ComputeBuffer StackBuffer;
ComputeBuffer ToBVHIndexBuffer;
ComputeBuffer BVHDataBuffer;
ComputeBuffer BVHBuffer;

int NumberOfSetBits(int i)
{
    i = i - ((i >> 1) & 0x55555555);
    i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
    return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
}
unsafe public void ConstructNewTLAS() {

        BVH2Builder BVH = new BVH2Builder(MeshAABBs);
        TLASBVH8 = new BVH8Builder(BVH, ref MyMeshesCompacted);
        for(int i = 0; i < TLASBVH8.cwbvh_indices.Length; i++) {
            if(TLASBVH8.cwbvh_indices[i] >= RenderQue.Count) {
                InstanceRenderQue[TLASBVH8.cwbvh_indices[i] - RenderQue.Count].CompactedMeshData = i;
            } else RenderQue[TLASBVH8.cwbvh_indices[i]].CompactedMeshData = i;
        }         
        System.Array.Resize(ref TLASBVH8.BVH8Nodes, TLASBVH8.cwbvhnode_count);
        ToBVHIndex = new int[TLASBVH8.cwbvhnode_count];
        if(TempBVHArray == null || TLASBVH8.BVH8Nodes.Length != TempBVHArray.Length) TempBVHArray = new BVHNode8DataCompressed[TLASBVH8.BVH8Nodes.Length];
        CommonFunctions.Aggregate(ref TempBVHArray, ref TLASBVH8.BVH8Nodes);

            CWBVHIndicesBufferInverted = new int[TLASBVH8.cwbvh_indices.Length];
        int CWBVHIndicesBufferCount = CWBVHIndicesBufferInverted.Length;
        for(int i = 0; i < CWBVHIndicesBufferCount; i++) CWBVHIndicesBufferInverted[TLASBVH8.cwbvh_indices[i]] = i;
        NodePair = new List<NodeIndexPairData>();
        NodePair.Add(new NodeIndexPairData());
        DocumentNodes(0, 0, 1, 0, false, 0);
        MaxRecur++;
        int NodeCount = NodePair.Count;
        ForwardStack = new Layer[NodeCount];
        for(int i = 0; i < NodePair.Count; i++) {
            for(int i2 = 0; i2 < 8; i2++) {
                ForwardStack[i].Children[i2] = -1;
                ForwardStack[i].Leaf[i2] = -1;
            }
        }

        for(int i = 0; i < NodePair.Count; i++) {
            if(NodePair[i].IsLeaf == 1) {
                int first_triangle = (byte)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] & 0b11111;
                int NumBits = NumberOfSetBits((byte)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] >> 5);
                ForwardStack[i].Children[NodePair[i].InNodeOffset] = NumBits;
                ForwardStack[i].Leaf[NodePair[i].InNodeOffset] = (int)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].base_index_triangle + first_triangle + 1; 
            } else {
                ForwardStack[i].Children[NodePair[i].InNodeOffset] = i;
                ForwardStack[i].Leaf[NodePair[i].InNodeOffset] = 0;
            }
            ForwardStack[NodePair[i].PreviousNode].Children[NodePair[i].InNodeOffset] = i;
            ForwardStack[NodePair[i].PreviousNode].Leaf[NodePair[i].InNodeOffset] = 0;
        }

        LayerStack = new Layer2[MaxRecur];
        for(int i = 0; i < MaxRecur; i++) {
            Layer2 TempSlab = new Layer2();
            TempSlab.Slab = new List<int>();
            LayerStack[i] = TempSlab;
        }
        for(int i = 0; i < NodePair.Count; i++) {
            var TempLayer = LayerStack[NodePair[i].RecursionCount];
            TempLayer.Slab.Add(i);
            LayerStack[NodePair[i].RecursionCount] = TempLayer;
        }
        ConvertToSplitNodes(TLASBVH8);
        int MaxLength = 0;
        for(int i = 0; i < LayerStack.Length; i++) {
            MaxLength = Mathf.Max(MaxLength, LayerStack[i].Slab.Count);
        }

        if(WorkingBuffer != null) WorkingBuffer.Release();
        if(NodeBuffer != null) NodeBuffer.Release();
        if(StackBuffer != null) StackBuffer.Release();
        if(ToBVHIndexBuffer != null) ToBVHIndexBuffer.Release();
        if(BVHDataBuffer != null) BVHDataBuffer.Release();
        if(BVHBuffer != null) BVHBuffer.Release();
        if(BoxesBuffer != null) BoxesBuffer.Release();
        WorkingBuffer = new ComputeBuffer(MaxLength, 4);
        NodeBuffer = new ComputeBuffer(NodePair.Count, 48);
        NodeBuffer.SetData(NodePair);
        StackBuffer = new ComputeBuffer(ForwardStack.Length, 64);
        StackBuffer.SetData(ForwardStack);
        ToBVHIndexBuffer = new ComputeBuffer(ToBVHIndex.Length, 4);
        ToBVHIndexBuffer.SetData(ToBVHIndex);     
        BVHDataBuffer = new ComputeBuffer(TempBVHArray.Length, 260);
        BVHDataBuffer.SetData(SplitNodes); 
        BVHBuffer = new ComputeBuffer(TempBVHArray.Length, 80);
        BVHBuffer.SetData(TempBVHArray);
        BoxesBuffer = new ComputeBuffer(MeshAABBs.Length, 24);
    }

    unsafe private void ConvertToSplitNodes(BVH8Builder BVH) {
    BVHNode8DataFixed NewNode = new BVHNode8DataFixed();
    SplitNodes = new List<BVHNode8DataFixed>();
    BVHNode8Data SourceNode;
    for(int i = 0; i < BVH.BVH8Nodes.Length; i++) {
        SourceNode = BVH.BVH8Nodes[i];
        NewNode.p = SourceNode.p;
        NewNode.e1 = SourceNode.e[0];
        NewNode.e2 = SourceNode.e[1];
        NewNode.e3 = SourceNode.e[2];
        NewNode.imask = SourceNode.imask;
        NewNode.base_index_child = SourceNode.base_index_child;
        NewNode.base_index_triangle = SourceNode.base_index_triangle;
        NewNode.meta1 = (uint)SourceNode.meta[0];
        NewNode.meta2 = (uint)SourceNode.meta[1];
        NewNode.meta3 = (uint)SourceNode.meta[2];
        NewNode.meta4 = (uint)SourceNode.meta[3];
        NewNode.meta5 = (uint)SourceNode.meta[4];
        NewNode.meta6 = (uint)SourceNode.meta[5];
        NewNode.meta7 = (uint)SourceNode.meta[6];
        NewNode.meta8 = (uint)SourceNode.meta[7];
        NewNode.quantized_min_x1 = (uint)SourceNode.quantized_min_x[0];
        NewNode.quantized_min_x2 = (uint)SourceNode.quantized_min_x[1];
        NewNode.quantized_min_x3 = (uint)SourceNode.quantized_min_x[2];
        NewNode.quantized_min_x4 = (uint)SourceNode.quantized_min_x[3];
        NewNode.quantized_min_x5 = (uint)SourceNode.quantized_min_x[4];
        NewNode.quantized_min_x6 = (uint)SourceNode.quantized_min_x[5];
        NewNode.quantized_min_x7 = (uint)SourceNode.quantized_min_x[6];
        NewNode.quantized_min_x8 = (uint)SourceNode.quantized_min_x[7];
        NewNode.quantized_max_x1 = (uint)SourceNode.quantized_max_x[0];
        NewNode.quantized_max_x2 = (uint)SourceNode.quantized_max_x[1];
        NewNode.quantized_max_x3 = (uint)SourceNode.quantized_max_x[2];
        NewNode.quantized_max_x4 = (uint)SourceNode.quantized_max_x[3];
        NewNode.quantized_max_x5 = (uint)SourceNode.quantized_max_x[4];
        NewNode.quantized_max_x6 = (uint)SourceNode.quantized_max_x[5];
        NewNode.quantized_max_x7 = (uint)SourceNode.quantized_max_x[6];
        NewNode.quantized_max_x8 = (uint)SourceNode.quantized_max_x[7];

        NewNode.quantized_min_y1 = (uint)SourceNode.quantized_min_y[0];
        NewNode.quantized_min_y2 = (uint)SourceNode.quantized_min_y[1];
        NewNode.quantized_min_y3 = (uint)SourceNode.quantized_min_y[2];
        NewNode.quantized_min_y4 = (uint)SourceNode.quantized_min_y[3];
        NewNode.quantized_min_y5 = (uint)SourceNode.quantized_min_y[4];
        NewNode.quantized_min_y6 = (uint)SourceNode.quantized_min_y[5];
        NewNode.quantized_min_y7 = (uint)SourceNode.quantized_min_y[6];
        NewNode.quantized_min_y8 = (uint)SourceNode.quantized_min_y[7];
        NewNode.quantized_max_y1 = (uint)SourceNode.quantized_max_y[0];
        NewNode.quantized_max_y2 = (uint)SourceNode.quantized_max_y[1];
        NewNode.quantized_max_y3 = (uint)SourceNode.quantized_max_y[2];
        NewNode.quantized_max_y4 = (uint)SourceNode.quantized_max_y[3];
        NewNode.quantized_max_y5 = (uint)SourceNode.quantized_max_y[4];
        NewNode.quantized_max_y6 = (uint)SourceNode.quantized_max_y[5];
        NewNode.quantized_max_y7 = (uint)SourceNode.quantized_max_y[6];
        NewNode.quantized_max_y8 = (uint)SourceNode.quantized_max_y[7];

        NewNode.quantized_min_z1 = (uint)SourceNode.quantized_min_z[0];
        NewNode.quantized_min_z2 = (uint)SourceNode.quantized_min_z[1];
        NewNode.quantized_min_z3 = (uint)SourceNode.quantized_min_z[2];
        NewNode.quantized_min_z4 = (uint)SourceNode.quantized_min_z[3];
        NewNode.quantized_min_z5 = (uint)SourceNode.quantized_min_z[4];
        NewNode.quantized_min_z6 = (uint)SourceNode.quantized_min_z[5];
        NewNode.quantized_min_z7 = (uint)SourceNode.quantized_min_z[6];
        NewNode.quantized_min_z8 = (uint)SourceNode.quantized_min_z[7];
        NewNode.quantized_max_z1 = (uint)SourceNode.quantized_max_z[0];
        NewNode.quantized_max_z2 = (uint)SourceNode.quantized_max_z[1];
        NewNode.quantized_max_z3 = (uint)SourceNode.quantized_max_z[2];
        NewNode.quantized_max_z4 = (uint)SourceNode.quantized_max_z[3];
        NewNode.quantized_max_z5 = (uint)SourceNode.quantized_max_z[4];
        NewNode.quantized_max_z6 = (uint)SourceNode.quantized_max_z[5];
        NewNode.quantized_max_z7 = (uint)SourceNode.quantized_max_z[6];
        NewNode.quantized_max_z8 = (uint)SourceNode.quantized_max_z[7];

        SplitNodes.Add(NewNode);
    }
}

ComputeBuffer BoxesBuffer;
public void RefitTLAS(AABB[] Boxes) {
        Refitter.SetInt("NodeCount", NodePair.Count);
        Refitter.SetBuffer(NodeInitializerKernel, "AllNodes", NodeBuffer);

        Refitter.Dispatch(NodeInitializerKernel, (int)Mathf.Ceil(NodePair.Count / (float)256), 1, 1);
        
        BoxesBuffer.SetData(Boxes);
        Refitter.SetBuffer(RefitLayer, "Boxs", BoxesBuffer);
        Refitter.SetBuffer(RefitLayer, "ReverseStack", StackBuffer);
        Refitter.SetBuffer(RefitLayer, "AllNodes", NodeBuffer);
        Refitter.SetBuffer(NodeInitializerKernel, "AllNodes", NodeBuffer);
        for(int i = MaxRecur - 1; i >= 0; i--) {
            var NodeCount2 = LayerStack[i].Slab.Count;
            WorkingBuffer.SetData(LayerStack[i].Slab, 0, 0, NodeCount2);
            Refitter.SetInt("NodeCount", NodeCount2);
            Refitter.SetBuffer(RefitLayer, "NodesToWork", WorkingBuffer);        
            Refitter.Dispatch(RefitLayer, (int)Mathf.Ceil(WorkingBuffer.count / (float)256), 1, 1);
        }
        Refitter.SetInt("NodeCount", NodePair.Count);
        Refitter.SetBuffer(NodeUpdater, "AllNodes", NodeBuffer);
        Refitter.SetBuffer(NodeUpdater, "BVHNodes", BVHDataBuffer);
        Refitter.SetBuffer(NodeUpdater, "ToBVHIndex", ToBVHIndexBuffer);
        Refitter.Dispatch(NodeUpdater, (int)Mathf.Ceil(NodePair.Count / (float)256), 1, 1);

        Refitter.SetInt("NodeCount", TLASBVH8.BVH8Nodes.Length);
        Refitter.SetInt("NodeOffset", 0);
        Refitter.SetBuffer(NodeCompress, "BVHNodes", BVHDataBuffer);
        Refitter.SetBuffer(NodeCompress, "AggNodes", BVH8AggregatedBuffer);
        Refitter.Dispatch(NodeCompress, (int)Mathf.Ceil(NodePair.Count / (float)256), 1, 1);
}

int PreVoxelMeshCount;

private bool ChangedLastFrame = true;


    public bool UpdateTLAS() {  //Allows for objects to be moved in the scene or animated while playing 
        
        bool LightsHaveUpdated = false;
        AccumulateData();

        float CDF = 0.0f;//(LightMeshes.Count != 0) ? LightMeshes[LightMeshes.Count - 1].CDF : 0.0f;
        if(!didstart || PrevLightCount != RayTracingMaster._rayTracingLights.Count) {
            UnityLights.Clear();
            UnityLightCount = 0;
            foreach(RayTracingLights RayLight in RayTracingMaster._rayTracingLights) {
                UnityLightCount++;
                RayLight.UpdateLight();
                CDF += RayLight.Energy;
                if(RayLight.Type == 1) SunDirection = RayLight.Direction;
                RayLight.ArrayIndex = UnityLights.Count;
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
        } else {
            int LightCount = RayTracingMaster._rayTracingLights.Count;
            LightData UnityLight;
            RayTracingLights RayLight;
            for(int i = 0; i < LightCount; i++) {
                RayLight = RayTracingMaster._rayTracingLights[i];
                CDF += RayLight.Energy;
                if(RayLight.Type == 1) SunDirection = RayLight.Direction;
                RayLight.UpdateLight();
                UnityLight = UnityLights[RayLight.ArrayIndex];
                UnityLight.Radiance = RayLight.Emission;
                UnityLight.Position = RayLight.Position;
                UnityLight.Direction = RayLight.Direction;
                UnityLight.energy = RayLight.Energy;
                UnityLight.CDF = CDF;
                UnityLight.Type = RayLight.Type;
                UnityLight.SpotAngle = RayLight.SpotAngle;
                UnityLights[RayLight.ArrayIndex] = UnityLight;
            }
        }
       
            int MatOffset = 0;
            int MeshDataCount =  RenderQue.Count;
            int aggregated_bvh_node_count = 2 * (MeshDataCount + InstanceRenderQue.Count);
            int AggNodeCount = aggregated_bvh_node_count;
            int AggTriCount = 0;
        if(ChildrenUpdated || !didstart || OnlyInstanceUpdated) {
            MyMeshesCompacted.Clear();     
            AggData[] Aggs = new AggData[InstanceData.RenderQue.Count];
            for(int i = 0; i < MeshDataCount; i++) {
                RenderQue[i].UpdateAABB(RenderQue[i].transform);
                 MyMeshesCompacted.Add(new MyMeshDataCompacted() {
                    mesh_data_bvh_offsets = aggregated_bvh_node_count,
                    Transform = RenderQue[i].transform.worldToLocalMatrix,
                    Inverse = RenderQue[i].transform.localToWorldMatrix,
                    AggIndexCount = AggTriCount,
                    AggNodeCount = AggNodeCount,
                    MaterialOffset = MatOffset,
                    IsVoxel = 0, SizeX = 0, SizeY = 0, SizeZ = 0,
                    LightTriCount = RenderQue[i].LightTriangles.Count,
                    LightPDF = RenderQue[i].TotEnergy
                });
                RenderQue[i].CompactedMeshData = MyMeshesCompacted.Count - 1;
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
                Aggs[i].LightTriCount = InstanceData.RenderQue[i].LightTriangles.Count;
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
                    Inverse = InstanceRenderQue[i].transform.localToWorldMatrix,
                    AggIndexCount = Aggs[Index].AggIndexCount,
                    AggNodeCount = Aggs[Index].AggNodeCount,
                    MaterialOffset = Aggs[Index].MaterialOffset,
                    IsVoxel = 0, SizeX = 0, SizeY = 0, SizeZ = 0,
                    LightTriCount = Aggs[Index].LightTriCount
                  });
                 InstanceRenderQue[i].CompactedMeshData = MyMeshesCompacted.Count - 1;
                 AABB aabb = InstanceRenderQue[i].InstanceParent.aabb_untransformed;
                 CreateAABB(InstanceRenderQue[i].transform, ref aabb);
                MeshAABBs[RenderQue.Count + i] = aabb;
            }
            PreVoxelMeshCount = MyMeshesCompacted.Count;
            List<MyMeshDataCompacted> TempMeshCompacted = new List<MyMeshDataCompacted>();
            for(int i = 0; i < VoxelRenderQue.Count; i++) {
                 TempMeshCompacted.Add(new MyMeshDataCompacted() {
                    mesh_data_bvh_offsets = 0,
                    Transform = VoxelRenderQue[i].transform.worldToLocalMatrix,
                    Inverse = VoxelRenderQue[i].transform.worldToLocalMatrix.inverse,
                    AggIndexCount = AggTriCount,
                    AggNodeCount = AggNodeCount,
                    MaterialOffset = MatOffset,
                    IsVoxel = 1,
                    SizeX = (int)VoxelRenderQue[i].Builder.FinalSize,
                    SizeY = (int)VoxelRenderQue[i].Builder.VoxelSize,
                    SizeZ = (int)VoxelRenderQue[i].Builder.BrickSize
                  });
                 AggTriCount += VoxelRenderQue[i].BrickmapTraverse.Count;
                 AggNodeCount += VoxelRenderQue[i].BrickmapTraverse.Count;
                 MatOffset += VoxelRenderQue[i]._Materials.Count;
                 VoxelRenderQue[i].UpdateAABB();
                 VoxelAABBs[i] = VoxelRenderQue[i].aabb;

            }
            if(VoxelRenderQue.Count != 0) {
                BVH2Builder BVH2 = new BVH2Builder(VoxelAABBs);
                BVH8Builder TLASBVH8 = new BVH8Builder(BVH2, ref TempMeshCompacted);
                MyMeshesCompacted.AddRange(TempMeshCompacted);
                if(VoxelTLAS == null || VoxelTLAS.Length != TLASBVH8.BVH8Nodes.Length) VoxelTLAS = new BVHNode8DataCompressed[TLASBVH8.BVH8Nodes.Length];
                CommonFunctions.Aggregate(ref VoxelTLAS, ref TLASBVH8.BVH8Nodes);
            } else {
                if(VoxelTLAS == null || VoxelTLAS.Length != 1) VoxelTLAS = new BVHNode8DataCompressed[1];
            }


            Debug.Log("Total Object Count: " + MeshAABBs.Length);
            UpdateMaterials();
        } else {
            UnityEngine.Profiling.Profiler.BeginSample("Refit TLAS");
            UnityEngine.Profiling.Profiler.BeginSample("Update Transforms");
            Transform transform;
            ParentObject TargetParent;
            for(int i = 0; i < MeshDataCount; i++) {
                TargetParent = RenderQue[i];
                transform = TargetParent.ThisTransform;
                if(transform.hasChanged) {
                    if(!TargetParent.IsSkinnedGroup) TargetParent.UpdateAABB(transform);            
                    MyMeshDataCompacted TempMesh = MyMeshesCompacted[TargetParent.CompactedMeshData];
                    TempMesh.Transform = transform.worldToLocalMatrix;
                    TempMesh.Inverse = transform.localToWorldMatrix;
                    MyMeshesCompacted[TargetParent.CompactedMeshData] = TempMesh;
                    transform.hasChanged = false;
                }
                MeshAABBs[TargetParent.CompactedMeshData] = TargetParent.aabb;
                MatOffset += TargetParent.MatOffset;
                AggNodeCount += TargetParent.AggBVHNodeCount;//Can I replace this with just using aggregated_bvh_node_count below?
                AggTriCount += TargetParent.AggIndexCount;
                aggregated_bvh_node_count += TargetParent.BVH.cwbvhnode_count;
            }
            UnityEngine.Profiling.Profiler.EndSample();

            for(int i = 0; i < InstanceRenderQue.Count; i++) {
                transform = InstanceRenderQue[i].transform;
                if(transform.hasChanged || ChangedLastFrame) {
                    MyMeshDataCompacted TempMesh = MyMeshesCompacted[InstanceRenderQue[i].CompactedMeshData];
                    TempMesh.Transform = transform.worldToLocalMatrix;
                    TempMesh.Inverse = transform.localToWorldMatrix;
                    MyMeshesCompacted[InstanceRenderQue[i].CompactedMeshData] = TempMesh;
                    transform.hasChanged = false;
                    AABB aabb = InstanceRenderQue[i].InstanceParent.aabb_untransformed;
                    CreateAABB(transform, ref aabb);
                    MeshAABBs[InstanceRenderQue[i].CompactedMeshData] = aabb;
                }
             }
            for(int i = 0; i < InstanceData.RenderQue.Count; i++) {
                MatOffset += InstanceData.RenderQue[i]._Materials.Count;
            }
             AggNodeCount = 0;
            for(int i = 0; i < VoxelRenderQue.Count; i++) {
                MyMeshDataCompacted TempMesh = MyMeshesCompacted[i + PreVoxelMeshCount];
                    // if(VoxelRenderQue[i].transform.hasChanged) {
                        TempMesh.Transform = VoxelRenderQue[i].transform.worldToLocalMatrix;
                        TempMesh.Inverse = VoxelRenderQue[i].transform.localToWorldMatrix;
                        TempMesh.AggIndexCount = AggTriCount;
                        TempMesh.AggNodeCount = AggNodeCount;
                        TempMesh.MaterialOffset = MatOffset;
                        TempMesh.IsVoxel = 1;
                        TempMesh.SizeX = (int)VoxelRenderQue[i].Builder.FinalSize;
                        TempMesh.SizeY = (int)VoxelRenderQue[i].Builder.VoxelSize;
                        TempMesh.SizeZ = (int)VoxelRenderQue[i].Builder.BrickSize;
                        VoxelRenderQue[i].transform.hasChanged = false;
                    // }
                    MyMeshesCompacted[i + PreVoxelMeshCount] = TempMesh;
                 AggTriCount += VoxelRenderQue[i].BrickmapTraverse.Count;
                 AggNodeCount += VoxelRenderQue[i].BrickmapTraverse.Count;
                 MatOffset += VoxelRenderQue[i]._Materials.Count;
                 VoxelRenderQue[i].UpdateAABB();
                 VoxelAABBs[i] = VoxelRenderQue[i].aabb;

            }
            if(VoxelRenderQue.Count != 0) {
                BVH2Builder BVH22 = new BVH2Builder(VoxelAABBs);
                List<MyMeshDataCompacted> TempMeshCompacted = new List<MyMeshDataCompacted>();
                for(int i = PreVoxelMeshCount; i < MyMeshesCompacted.Count; i++) {
                    TempMeshCompacted.Add(MyMeshesCompacted[i]);
                }
                BVH8Builder TLASBVH82 = new BVH8Builder(BVH22, ref TempMeshCompacted);
                for(int i = PreVoxelMeshCount; i < MyMeshesCompacted.Count; i++) {
                    MyMeshesCompacted[i] = TempMeshCompacted[i - PreVoxelMeshCount];
                }
                if(VoxelTLAS == null || VoxelTLAS.Length != TLASBVH82.BVH8Nodes.Length) VoxelTLAS = new BVHNode8DataCompressed[TLASBVH82.BVH8Nodes.Length];
                CommonFunctions.Aggregate(ref VoxelTLAS, ref TLASBVH82.BVH8Nodes);
            } else {
                if(VoxelTLAS == null || VoxelTLAS.Length != 1) VoxelTLAS = new BVHNode8DataCompressed[1];
            }
            UnityEngine.Profiling.Profiler.EndSample();

        }
        LightMeshData CurLightMesh;
        for(int i = 0; i < LightMeshCount; i++) {
            CurLightMesh = LightMeshes[i];
            CurLightMesh.Inverse = LightTransforms[i].localToWorldMatrix;
            CurLightMesh.Center = LightTransforms[i].position;
            LightMeshes[i] = CurLightMesh;
        }
        if(ChildrenUpdated || !didstart || OnlyInstanceUpdated) {
            ConstructNewTLAS();
            BVH8AggregatedBuffer.SetData(TempBVHArray,0,0,TempBVHArray.Length);
        } else {
            UnityEngine.Profiling.Profiler.BeginSample("TLAS Refitting");
            RefitTLAS(MeshAABBs);
            UnityEngine.Profiling.Profiler.EndSample();
        }
        if(!didstart) didstart = true;
        
        VoxOffset = PreVoxelMeshCount;
        AggNodeCount = 0;
        if(VoxelRenderQue.Count != 0) 
            UseVoxels = true;
        else 
            UseVoxels = false;

            ChangedLastFrame = ChildrenUpdated;
        return (LightsHaveUpdated || ChildrenUpdated);//The issue is that all light triangle indices start at 0, and thus might not get correctly sorted for indices
    }

    public void UpdateMaterials() {//Allows for live updating of material properties of any object
        int ParentCount = RenderQue.Count;
        RayTracingObject CurrentMaterial;
        MaterialData TempMat;
        int ChangedMaterialCount = MaterialsChanged.Count;
        for(int i = 0; i < ChangedMaterialCount; i++) {
            CurrentMaterial = MaterialsChanged[i];
            int MaterialCount = CurrentMaterial.MaterialIndex.Length;
            for(int i3 = 0; i3 < MaterialCount; i3++) {
                int Index = CurrentMaterial.Indexes[i3];
                TempMat = _Materials[CurrentMaterial.MaterialIndex[i3]];
                if(TempMat.HasAlbedoTex != 1)  TempMat.BaseColor = CurrentMaterial.BaseColor[Index];
                TempMat.emmissive = CurrentMaterial.emmission[Index];
                TempMat.Roughness = ((int)CurrentMaterial.MaterialOptions[Index] != 1) ? CurrentMaterial.Roughness[Index] : Mathf.Max(CurrentMaterial.Roughness[Index], 0.000001f);
                TempMat.TransmittanceColor = CurrentMaterial.TransmissionColor[Index];
                TempMat.MatType = (int)CurrentMaterial.MaterialOptions[Index];
                TempMat.EmissionColor = CurrentMaterial.EmissionColor[Index];
                TempMat.metallic = CurrentMaterial.Metallic[Index];
                TempMat.specularTint = CurrentMaterial.SpecularTint[Index];
                TempMat.sheen = CurrentMaterial.Sheen[Index];
                TempMat.sheenTint = CurrentMaterial.SheenTint[Index];
                TempMat.clearcoat = CurrentMaterial.ClearCoat[Index];
                TempMat.IOR = CurrentMaterial.IOR[Index];
                TempMat.Thin = CurrentMaterial.Thin[Index];
                TempMat.clearcoatGloss = CurrentMaterial.ClearCoatGloss[Index];
                TempMat.specTrans = CurrentMaterial.SpecTrans[Index];
                TempMat.anisotropic = CurrentMaterial.Anisotropic[Index];
                TempMat.diffTrans = CurrentMaterial.DiffTrans[Index];
                TempMat.flatness = CurrentMaterial.Flatness[Index];
                TempMat.Specular = CurrentMaterial.Specular[Index];
                _Materials[CurrentMaterial.MaterialIndex[i3]] = TempMat;
            }
            CurrentMaterial.NeedsToUpdate = false;
        }
        MaterialsChanged.Clear();

        ParentCount = InstanceData.RenderQue.Count;
        for(int i = 0; i < ParentCount; i++) {
            int ChildCount = InstanceData.RenderQue[i].ChildObjects.Length;
            for(int i2 = 0; i2 < ChildCount; i2++) {
                CurrentMaterial = InstanceData.RenderQue[i].ChildObjects[i2];
                if(CurrentMaterial.NeedsToUpdate || !didstart) {
                    int MaterialCount = CurrentMaterial.MaterialIndex.Length;
                    for(int i3 = 0; i3 < MaterialCount; i3++) {
                        int Index = CurrentMaterial.Indexes[i3];
                        TempMat = _Materials[CurrentMaterial.MaterialIndex[i3]];
                        if(TempMat.HasAlbedoTex != 1)  TempMat.BaseColor = CurrentMaterial.BaseColor[Index];
                        TempMat.emmissive = CurrentMaterial.emmission[Index];
                        TempMat.Roughness = ((int)CurrentMaterial.MaterialOptions[Index] != 1) ? CurrentMaterial.Roughness[Index] : Mathf.Max(CurrentMaterial.Roughness[Index], 0.000001f);
                        TempMat.TransmittanceColor = CurrentMaterial.TransmissionColor[Index];
                        TempMat.MatType = (int)CurrentMaterial.MaterialOptions[Index];
                        TempMat.EmissionColor = CurrentMaterial.EmissionColor[Index];
                        TempMat.metallic = CurrentMaterial.Metallic[Index];
                        TempMat.specularTint = CurrentMaterial.SpecularTint[Index];
                        TempMat.sheen = CurrentMaterial.Sheen[Index];
                        TempMat.sheenTint = CurrentMaterial.SheenTint[Index];
                        TempMat.clearcoat = CurrentMaterial.ClearCoat[Index];
                        TempMat.IOR = CurrentMaterial.IOR[Index];
                        TempMat.Thin = CurrentMaterial.Thin[Index];
                        TempMat.clearcoatGloss = CurrentMaterial.ClearCoatGloss[Index];
                        TempMat.specTrans = CurrentMaterial.SpecTrans[Index];
                        TempMat.anisotropic = CurrentMaterial.Anisotropic[Index];
                        TempMat.diffTrans = CurrentMaterial.DiffTrans[Index];
                        TempMat.flatness = CurrentMaterial.Flatness[Index];
                        TempMat.Specular = CurrentMaterial.Specular[Index];
                        _Materials[CurrentMaterial.MaterialIndex[i3]] = TempMat;
                    }
                }
            }
        }
    }



}
