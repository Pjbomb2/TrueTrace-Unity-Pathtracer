// #define DoLightMapping\
// #define HardwareRT
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;
using System.Threading;
using RectpackSharp;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
#pragma warning disable 4014

namespace TrueTrace {
    [System.Serializable]
    public class AssetManager : MonoBehaviour
    {//This handels all the data
        public int TotalParentObjectSize;
        public float LightEnergyScale = 1.0f;
        public Texture2D AlbedoAtlas;
        public RenderTexture NormalAtlas;
        public RenderTexture EmissiveAtlas;
        public RenderTexture AlphaAtlas;
        public RenderTexture MetallicAtlas;
        public RenderTexture RoughnessAtlas;
        [HideInInspector] public RenderTexture TempTex;
        private ComputeShader Refitter;
        private int RefitLayer;
        private int NodeUpdater;
        private int NodeCompress;
        private int NodeInitializerKernel;
        [HideInInspector] public List<LightMapTriangle> LightMapTris;
        [HideInInspector] static public Camera SelectedCamera;

        [HideInInspector] public VideoObject VideoPlayerObject;
        [HideInInspector] public RenderTexture VideoTexture;

        private Material Copymaterial;
        private ComputeShader CopyShader;

        [HideInInspector] public List<RayTracingObject> MaterialsChanged;
        [HideInInspector] public List<MaterialData> _Materials;
        [HideInInspector] public ComputeBuffer BVH8AggregatedBuffer;
        [HideInInspector] public ComputeBuffer AggTriBuffer;
        [HideInInspector] public BVHNode8DataCompressed[] VoxelTLAS;
        [HideInInspector] public List<MyMeshDataCompacted> MyMeshesCompacted;
        [HideInInspector] public List<CudaLightTriangle> AggLightTriangles;
        public List<LightData> UnityLights;
        [HideInInspector] public List<Light> Lights;
        [HideInInspector] public InstancedManager InstanceData;
        [HideInInspector] public List<InstancedObject> Instances;

        [HideInInspector] public List<TerrainObject> Terrains;
        [HideInInspector] public List<TerrainDat> TerrainInfos;
        [HideInInspector] public ComputeBuffer TerrainBuffer;
        [HideInInspector] public Texture2D HeightMap;
        [HideInInspector] public Texture2D AlphaMap;
        [HideInInspector] public bool DoHeightmap;


        private ComputeShader MeshFunctions;
        private int TriangleBufferKernel;
        private int NodeBufferKernel;

        [HideInInspector] public List<ParentObject> RenderQue;
        [HideInInspector] public List<ParentObject> BuildQue;
        [HideInInspector] public List<ParentObject> AddQue;
        [HideInInspector] public List<ParentObject> RemoveQue;
        [HideInInspector] public List<ParentObject> UpdateQue;

        [HideInInspector] public List<InstancedObject> InstanceRenderQue;
        [HideInInspector] public List<InstancedObject> InstanceBuildQue;
        [HideInInspector] public List<InstancedObject> InstanceAddQue;
        [HideInInspector] public List<InstancedObject> InstanceRemoveQue;

        [HideInInspector] public List<VoxelObject> VoxelBuildQue;
        [HideInInspector] public List<VoxelObject> VoxelRenderQue;
        [HideInInspector] public List<VoxelObject> VoxelAddQue;
        [HideInInspector] public List<VoxelObject> VoxelRemoveQue;
        private bool OnlyInstanceUpdated;
        [HideInInspector] public List<Transform> LightTransforms;
        [HideInInspector] public List<Task> CurrentlyActiveVoxelTasks;
        [HideInInspector] public int DesiredRes = 16300;

        [HideInInspector] public int MatCount;

        [HideInInspector] public int NormalSize;
        [HideInInspector] public int EmissiveSize;

        [HideInInspector] public List<LightMeshData> LightMeshes;

        [HideInInspector] public AABB[] MeshAABBs;
        [HideInInspector] public AABB[] UnsortedAABBs;
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
        #if HardwareRT
            public List<Vector2> MeshOffsets;
            public List<int> SubMeshOffsets;
            public UnityEngine.Rendering.RayTracingAccelerationStructure AccelStruct;
        #endif
        public void ClearAll()
        {//My attempt at clearing memory
            RunningTasks = 0;
            ParentObject[] ChildrenObjects = this.GetComponentsInChildren<ParentObject>();
            foreach (ParentObject obj in ChildrenObjects)
                obj.ClearAll();
            VoxelObject[] ChildVoxelObjects = this.GetComponentsInChildren<VoxelObject>();
            foreach (VoxelObject obj in ChildVoxelObjects)
                obj.ClearAll();
            if (_Materials != null)
            {
                _Materials.Clear();
                _Materials.TrimExcess();
            }
            if (LightTransforms != null)
            {
                LightTransforms.Clear();
                LightTransforms.TrimExcess();
            }
            if (LightMeshes != null)
            {
                LightMeshes.Clear();
                LightMeshes.TrimExcess();
            }
            if (MyMeshesCompacted != null)
            {
                MyMeshesCompacted.Clear();
                MyMeshesCompacted.TrimExcess();
            }
            if (AggLightTriangles != null)
            {
                AggLightTriangles.Clear();
                AggLightTriangles.TrimExcess();
            }
            if (UnityLights != null)
            {
                UnityLights.Clear();
                UnityLights.TrimExcess();
            }

            DestroyImmediate(AlbedoAtlas);
            DestroyImmediate(NormalAtlas);
            DestroyImmediate(EmissiveAtlas);
            DestroyImmediate(AlphaAtlas);
            DestroyImmediate(MetallicAtlas);
            DestroyImmediate(RoughnessAtlas);
            if (BVH8AggregatedBuffer != null)
            {
                BVH8AggregatedBuffer.Release();
                BVH8AggregatedBuffer = null;
                AggTriBuffer.Release();
                AggTriBuffer = null;
            }


            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        private List<Texture2D> AlbedoTexs;
        private List<RayObjects> AlbedoIndexes;
        private List<Texture2D> NormalTexs;
        private List<RayObjects> NormalIndexes;
        private List<Texture2D> MetallicTexs;
        private List<RayObjects> MetallicIndexes;
        private List<int> MetallicTexChannelIndex;
        private List<Texture2D> RoughnessTexs;
        private List<RayObjects> RoughnessIndexes;
        private List<int> RoughnessTexChannelIndex;
        private List<Texture2D> EmissiveTexs;
        private List<RayObjects> EmissiveIndexes;

        private void AddTextures(ref List<Texture2D> Texs, ref List<RayObjects> Indexes, ref List<RayObjects> ObjIndexes, ref List<Texture> ObjTexs)
        {
            int NewLength = ObjTexs.Count;
            int PrevLength = Texs.Count;
            for (int i = 0; i < NewLength; i++)
            {
                int Index = Texs.IndexOf((Texture2D)ObjTexs[i], 0, PrevLength);
                if (Index == -1)
                {
                    Texs.Add((Texture2D)ObjTexs[i]);
                    var E = new RayObjects();
                    E.RayObjectList = new List<RayObjectTextureIndex>(ObjIndexes[i].RayObjectList);
                    Indexes.Add(E);
                }
                else
                {
                    Indexes[Index].RayObjectList.AddRange(ObjIndexes[i].RayObjectList);
                }
            }
        }
        private void AddTexturesSingle(ref List<Texture2D> Texs, ref List<RayObjects> Indexes, ref List<RayObjects> ObjIndexes, ref List<Texture> ObjTexs, ref List<int> ReadIndex, List<int> ObjReadIndex)
        {
            int NewLength = ObjTexs.Count;
            int PrevLength = Texs.Count;
            for (int i = 0; i < NewLength; i++)
            {
                int Index = Texs.IndexOf((Texture2D)ObjTexs[i], 0, PrevLength);
                if (Index == -1)
                {
                    Texs.Add((Texture2D)ObjTexs[i]);
                    ReadIndex.Add(ObjReadIndex[i]);
                    var E = new RayObjects();
                    E.RayObjectList = new List<RayObjectTextureIndex>(ObjIndexes[i].RayObjectList);
                    Indexes.Add(E);
                }
                else
                {
                    Indexes[Index].RayObjectList.AddRange(ObjIndexes[i].RayObjectList);
                }
            }
        }

        private void ModifyTextureBounds(ref Rect[] Rects, int TexLength, ref List<RayObjects> Indexes, int TargetTex)
        {
            int TerrainIndexOffset = 0;
            for (int i = 0; i < TexLength; i++)
            {
                int SecondaryLength = Indexes[i].RayObjectList.Count;
                for (int i2 = 0; i2 < SecondaryLength; i2++)
                {
                    MaterialData TempMat = Indexes[i].RayObjectList[i2].Obj == null ?
                    _Materials[Indexes[i].RayObjectList[i2].Terrain.MaterialIndex[Indexes[i].RayObjectList[i2].ObjIndex] + MatCount + TerrainIndexOffset]
                    : _Materials[Indexes[i].RayObjectList[i2].Obj.MaterialIndex[Indexes[i].RayObjectList[i2].ObjIndex]];
                    switch (TargetTex)
                    {
                        case 0:
                            TempMat.AlbedoTex = new Vector4(Rects[i].xMax, Rects[i].yMax, Rects[i].xMin, Rects[i].yMin);
                            break;
                        case 1:
                            TempMat.NormalTex = new Vector4(Rects[i].xMax, Rects[i].yMax, Rects[i].xMin, Rects[i].yMin);
                            break;
                        case 2:
                            TempMat.MetallicTex = new Vector4(Rects[i].xMax, Rects[i].yMax, Rects[i].xMin, Rects[i].yMin);
                            break;
                        case 3:
                            TempMat.RoughnessTex = new Vector4(Rects[i].xMax, Rects[i].yMax, Rects[i].xMin, Rects[i].yMin);
                            break;
                        case 4:
                            TempMat.EmissiveTex = new Vector4(Rects[i].xMax, Rects[i].yMax, Rects[i].xMin, Rects[i].yMin);
                            break;
                        default:
                            Debug.Log("Materials Broke");
                            break;
                    }
                    _Materials[Indexes[i].RayObjectList[i2].Obj == null ? (Indexes[i].RayObjectList[i2].Terrain.MaterialIndex[Indexes[i].RayObjectList[i2].ObjIndex] + MatCount + TerrainIndexOffset) : Indexes[i].RayObjectList[i2].Obj.MaterialIndex[Indexes[i].RayObjectList[i2].ObjIndex]] = TempMat;
                }
            }
        }

        private void ConstructAtlas(List<Texture2D> Texs, ref RenderTexture Atlas, out Rect[] Rects, int DesiredRes, bool IsNormalMap)
        {
            PackingRectangle[] rectangles = new PackingRectangle[Texs.Count];
            for (int i = 0; i < Texs.Count; i++)
            {
                rectangles[i].Width = (uint)Texs[i].width;
                rectangles[i].Height = (uint)Texs[i].height;
                rectangles[i].Id = i;
            }
            PackingRectangle BoundRects;
            RectanglePacker.Pack(rectangles, out BoundRects);
            DesiredRes = (int)Mathf.Min(Mathf.Max(BoundRects.Width, BoundRects.Height), DesiredRes);
            CreateRenderTexture(ref Atlas, DesiredRes, DesiredRes);
            Rects = new Rect[Texs.Count];
            for (int i = 0; i < Texs.Count; i++)
            {
                Rects[rectangles[i].Id].width = rectangles[i].Width;
                Rects[rectangles[i].Id].height = rectangles[i].Height;
                Rects[rectangles[i].Id].x = rectangles[i].X;
                Rects[rectangles[i].Id].y = rectangles[i].Y;
            }

            Vector2 Scale = new Vector2(Mathf.Min((float)DesiredRes / BoundRects.Width, 1), Mathf.Min((float)DesiredRes / BoundRects.Height, 1));
            CopyShader.SetBool("IsNormalMap", IsNormalMap);
            for (int i = 0; i < Texs.Count; i++)
            {
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

        private void ConstructAtlasSingle(List<Texture2D> Texs, ref RenderTexture Atlas, out Rect[] Rects, int DesiredRes, bool IsNormalMap, int ReadIndex, List<int> TexChannelIndex)
        {
            PackingRectangle[] rectangles = new PackingRectangle[Texs.Count];
            for (int i = 0; i < Texs.Count; i++)
            {
                rectangles[i].Width = (uint)Texs[i].width;
                rectangles[i].Height = (uint)Texs[i].height;
                rectangles[i].Id = i;
            }
            PackingRectangle BoundRects;
            RectanglePacker.Pack(rectangles, out BoundRects);
            if(IsNormalMap) {
                DesiredRes = (int)Mathf.Min(Mathf.Max(BoundRects.Width, BoundRects.Height), DesiredRes);
                CreateRenderTextureDouble(ref Atlas, DesiredRes, DesiredRes);
            } else CreateRenderTextureSingle(ref Atlas, DesiredRes, DesiredRes);
            Rects = new Rect[Texs.Count];
            for (int i = 0; i < Texs.Count; i++)
            {
                Rects[rectangles[i].Id].width = rectangles[i].Width;
                Rects[rectangles[i].Id].height = rectangles[i].Height;
                Rects[rectangles[i].Id].x = rectangles[i].X;
                Rects[rectangles[i].Id].y = rectangles[i].Y;
            }

            Vector2 Scale = new Vector2(Mathf.Min((float)DesiredRes / BoundRects.Width, 1), Mathf.Min((float)DesiredRes / BoundRects.Height, 1));
            CopyShader.SetBool("IsNormalMap", IsNormalMap);
            for (int i = 0; i < Texs.Count; i++)
            {
                CopyShader.SetInt("OutputRead", ((TexChannelIndex == null) ? ReadIndex : TexChannelIndex[i]));
                CopyShader.SetVector("InputSize", new Vector2(Rects[i].width, Rects[i].height));
                CopyShader.SetVector("OutputSize", new Vector2(Atlas.width, Atlas.height));
                CopyShader.SetVector("Scale", Scale);
                CopyShader.SetVector("Offset", new Vector2(Rects[i].x, Rects[i].y));
                if(!IsNormalMap) {
                    CopyShader.SetTexture(2, "AdditionTex", Texs[i]);
                    CopyShader.SetTexture(2, "ResultSingle", Atlas);
                    CopyShader.Dispatch(2, (int)Mathf.CeilToInt(Rects[i].width * Scale.x / 32.0f), (int)Mathf.CeilToInt(Rects[i].height * Scale.y / 32.0f), 1);
                } else {
                    CopyShader.SetTexture(3, "AdditionTex", Texs[i]);
                    CopyShader.SetTexture(3, "ResultDouble", Atlas);
                    CopyShader.Dispatch(3, (int)Mathf.CeilToInt(Rects[i].width * Scale.x / 32.0f), (int)Mathf.CeilToInt(Rects[i].height * Scale.y / 32.0f), 1);
                }
                Rects[i].width = Mathf.Floor(Rects[i].width * Scale.x) / Atlas.width;
                Rects[i].x = Mathf.Floor(Rects[i].x * Scale.x) / Atlas.width;
                Rects[i].height = Mathf.Floor(Rects[i].height * Scale.y) / Atlas.height;
                Rects[i].y = Mathf.Floor(Rects[i].y * Scale.y) / Atlas.height;
            }
        }

        private void AddToAtlas(List<Texture2D> Texs, ref RenderTexture Atlas, out Rect[] Rects, int DesiredRes, int ReadIndex, int WriteIndex, List<int> TexChannelIndex)
        {
            PackingRectangle[] rectangles = new PackingRectangle[Texs.Count];
            for (int i = 0; i < Texs.Count; i++)
            {
                rectangles[i].Width = (uint)Texs[i].width;
                rectangles[i].Height = (uint)Texs[i].height;
                rectangles[i].Id = i;
            }
            PackingRectangle BoundRects;
            RectanglePacker.Pack(rectangles, out BoundRects);
            if (Atlas.width == 1) CreateRenderTexture(ref Atlas, DesiredRes, DesiredRes);
            Rects = new Rect[Texs.Count];
            for (int i = 0; i < Texs.Count; i++)
            {
                Rects[rectangles[i].Id].width = rectangles[i].Width;
                Rects[rectangles[i].Id].height = rectangles[i].Height;
                Rects[rectangles[i].Id].x = rectangles[i].X;
                Rects[rectangles[i].Id].y = rectangles[i].Y;
            }

            Vector2 Scale = new Vector2(Mathf.Min((float)DesiredRes / BoundRects.Width, 1), Mathf.Min((float)DesiredRes / BoundRects.Height, 1));
            CopyShader.SetInt("OutputWrite", WriteIndex);
            for (int i = 0; i < Texs.Count; i++)
            {
                CopyShader.SetInt("OutputRead", ((TexChannelIndex == null) ? ReadIndex : TexChannelIndex[i]));
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

        private void CreateRenderTexture(ref RenderTexture ThisTex, int Width, int Height)
        {
            ThisTex = new RenderTexture(Width, Height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            ThisTex.enableRandomWrite = true;
            ThisTex.useMipMap = false;
            // ThisTex.autoGenerateMips = false;
            ThisTex.Create();
        }

        private void CreateRenderTextureSingle(ref RenderTexture ThisTex, int Width, int Height)
        {
            ThisTex = new RenderTexture(Width, Height, 0,
                RenderTextureFormat.R8, RenderTextureReadWrite.sRGB);
            ThisTex.enableRandomWrite = true;
            ThisTex.useMipMap = true;
            ThisTex.autoGenerateMips = false;
            ThisTex.Create();
        }

        private void CreateRenderTextureDouble(ref RenderTexture ThisTex, int Width, int Height)
        {
            ThisTex = new RenderTexture(Width, Height, 0,
                RenderTextureFormat.RG16, RenderTextureReadWrite.sRGB);
            ThisTex.enableRandomWrite = true;
            ThisTex.useMipMap = true;
            ThisTex.autoGenerateMips = false;
            ThisTex.Create();
        }
        private void CreateAtlas()
        {//Creates texture atlas

            _Materials = new List<MaterialData>();
            AlbedoIndexes = new List<RayObjects>();
            AlbedoTexs = new List<Texture2D>();
            NormalTexs = new List<Texture2D>();
            NormalIndexes = new List<RayObjects>();
            MetallicTexs = new List<Texture2D>();
            MetallicIndexes = new List<RayObjects>();
            MetallicTexChannelIndex = new List<int>();
            RoughnessTexs = new List<Texture2D>();
            RoughnessIndexes = new List<RayObjects>();
            RoughnessTexChannelIndex = new List<int>();
            EmissiveTexs = new List<Texture2D>();
            EmissiveIndexes = new List<RayObjects>();
            List<Texture2D> HeightMaps = new List<Texture2D>();
            List<Texture2D> AlphaMaps = new List<Texture2D>();

            int TerrainCount = Terrains.Count;
            int MaterialOffset = 0;
            List<Vector2> Sizes = new List<Vector2>();
            TerrainInfos = new List<TerrainDat>();
            DoHeightmap = false;
            for (int i = 0; i < TerrainCount; i++)
            {
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
            if (TerrainCount != 0)
            {
                DoHeightmap = true;
                Rect[] AlphaRects;
                List<Rect> HeightRects = new List<Rect>();
                float MinX = 0;
                float MinY = 0;
                for (int i = 0; i < Sizes.Count; i++)
                {
                    MinX = Mathf.Max(Sizes[i].x, MinX);
                    MinY += Sizes[i].y * 2;
                }
                int Size = (int)Mathf.Min((MinY) / Mathf.Ceil(Mathf.Sqrt(Sizes.Count)), 16380);

                Texture2D.GenerateAtlas(Sizes.ToArray(), 0, Size, HeightRects);
                HeightMap = new Texture2D(Size, Size, UnityEngine.Experimental.Rendering.GraphicsFormat.R16_UNorm, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
                Color32[] Colors = HeightMap.GetPixels32(0);
                System.Array.Fill(Colors, Color.black);
                HeightMap.SetPixels32(Colors);
                HeightMap.Apply();
                AlphaRects = AlphaMap.PackTextures(AlphaMaps.ToArray(), 1, 16380, true);
                for (int i = 0; i < TerrainCount; i++)
                {
                    Graphics.CopyTexture(HeightMaps[i], 0, 0, 0, 0, HeightMaps[i].width, HeightMaps[i].height, HeightMap, 0, 0, (int)HeightRects[i].xMin, (int)HeightRects[i].yMin);
                    TerrainDat TempTerrain = TerrainInfos[i];
                    TempTerrain.HeightMap = new Vector4(HeightRects[i].xMax / Size, HeightRects[i].yMax / Size, HeightRects[i].xMin / Size, HeightRects[i].yMin / Size);
                    TempTerrain.AlphaMap = new Vector4(AlphaRects[i].xMax, AlphaRects[i].yMax, AlphaRects[i].xMin, AlphaRects[i].yMin);
                    TerrainInfos[i] = TempTerrain;
                }
            }
            if (!RenderQue.Any())
                return;
            int CurCount = RenderQue[0].AlbedoTexs.Count;
            foreach (ParentObject Obj in RenderQue)
            {
                AddTextures(ref AlbedoTexs, ref AlbedoIndexes, ref Obj.AlbedoIndexes, ref Obj.AlbedoTexs);
                AddTextures(ref NormalTexs, ref NormalIndexes, ref Obj.NormalIndexes, ref Obj.NormalTexs);
                AddTexturesSingle(ref MetallicTexs, ref MetallicIndexes, ref Obj.MetallicIndexes, ref Obj.MetallicTexs, ref MetallicTexChannelIndex, Obj.MetallicTexChannelIndex);
                AddTexturesSingle(ref RoughnessTexs, ref RoughnessIndexes, ref Obj.RoughnessIndexes, ref Obj.RoughnessTexs, ref RoughnessTexChannelIndex, Obj.RoughnessTexChannelIndex);
                AddTextures(ref EmissiveTexs, ref EmissiveIndexes, ref Obj.EmissionIndexes, ref Obj.EmissionTexs);
            }
            foreach (ParentObject Obj in InstanceData.RenderQue)
            {
                AddTextures(ref AlbedoTexs, ref AlbedoIndexes, ref Obj.AlbedoIndexes, ref Obj.AlbedoTexs);
                AddTextures(ref NormalTexs, ref NormalIndexes, ref Obj.NormalIndexes, ref Obj.NormalTexs);
                AddTexturesSingle(ref MetallicTexs, ref MetallicIndexes, ref Obj.MetallicIndexes, ref Obj.MetallicTexs, ref MetallicTexChannelIndex, Obj.MetallicTexChannelIndex);
                AddTexturesSingle(ref RoughnessTexs, ref RoughnessIndexes, ref Obj.RoughnessIndexes, ref Obj.RoughnessTexs, ref RoughnessTexChannelIndex, Obj.RoughnessTexChannelIndex);
                AddTextures(ref EmissiveTexs, ref EmissiveIndexes, ref Obj.EmissionIndexes, ref Obj.EmissionTexs);
            }

            if (TerrainCount != 0)
            {
                for (int i = 0; i < TerrainCount; i++)
                {
                    AddTextures(ref AlbedoTexs, ref AlbedoIndexes, ref Terrains[i].AlbedoIndexes, ref Terrains[i].AlbedoTexs);
                    AddTextures(ref NormalTexs, ref NormalIndexes, ref Terrains[i].NormalIndexes, ref Terrains[i].NormalTexs);
                    AddTextures(ref MetallicTexs, ref MetallicIndexes, ref Terrains[i].MetallicIndexes, ref Terrains[i].MetallicTexs);
                }
            }

            Rect[] AlbedoRects, NormalRects, EmissiveRects, MetallicRects, RoughnessRects;
            if (CopyShader == null) CopyShader = Resources.Load<ComputeShader>("Utility/CopyTextureShader");
            if (NormalAtlas != null) NormalAtlas.Release();
            if (AlphaAtlas != null) AlphaAtlas.Release();
            if (RoughnessAtlas != null) RoughnessAtlas.Release();
            if (MetallicAtlas != null) MetallicAtlas.Release();
            if (EmissiveAtlas != null) EmissiveAtlas.Release();
            if (AlbedoTexs.Count != 0) ConstructAtlas(AlbedoTexs, ref TempTex, out AlbedoRects, DesiredRes, false);
            else { AlbedoRects = new Rect[0]; CreateRenderTexture(ref TempTex, 1, 1); }
            Graphics.CopyTexture(TempTex, 0, 0, 0, 0, TempTex.width, TempTex.height, AlbedoAtlas, 0, 0, 0, 0);
            TempTex.Release();
            if (NormalTexs.Count != 0) ConstructAtlasSingle(NormalTexs, ref NormalAtlas, out NormalRects, DesiredRes, true, 0, null);
            else { NormalRects = new Rect[0]; CreateRenderTextureSingle(ref NormalAtlas, 1, 1); }
            if (EmissiveTexs.Count != 0) ConstructAtlas(EmissiveTexs, ref EmissiveAtlas, out EmissiveRects, DesiredRes, false);
            else { EmissiveRects = new Rect[0]; CreateRenderTexture(ref EmissiveAtlas, 1, 1); }
            if (MetallicTexs.Count != 0) ConstructAtlasSingle(MetallicTexs, ref MetallicAtlas, out MetallicRects, AlbedoAtlas.width, false, 0, MetallicTexChannelIndex);
            else { MetallicRects = new Rect[0]; CreateRenderTextureSingle(ref MetallicAtlas, 1, 1);}
            if (AlbedoTexs.Count != 0) ConstructAtlasSingle(AlbedoTexs, ref AlphaAtlas, out AlbedoRects, AlbedoAtlas.width, false, 3, null);
            else { AlbedoRects = new Rect[0]; CreateRenderTextureSingle(ref AlphaAtlas, 1, 1);}
            if (RoughnessTexs.Count != 0) ConstructAtlasSingle(RoughnessTexs, ref RoughnessAtlas, out RoughnessRects, AlbedoAtlas.width, false, 0, RoughnessTexChannelIndex);
            else { RoughnessRects = new Rect[0]; CreateRenderTextureSingle(ref RoughnessAtlas, 1, 1);}
            // AlbedoAtlas.GenerateMips();
            MetallicAtlas.GenerateMips();
            RoughnessAtlas.GenerateMips();
            // AlbedoAtlas.GenerateMips();
            MatCount = 0;

            foreach (ParentObject Obj in RenderQue)
            {
                foreach (RayTracingObject Obj2 in Obj.ChildObjects)
                {
                    MaterialsChanged.Add(Obj2);
                    for (int i = 0; i < Obj2.MaterialIndex.Length; i++)
                    {
                        Obj2.MaterialIndex[i] = Obj2.LocalMaterialIndex[i] + MatCount;
                    }
                }
                _Materials.AddRange(Obj._Materials);
                MatCount += Obj._Materials.Count;
            }
            foreach (ParentObject Obj in InstanceData.RenderQue)
            {
                foreach (RayTracingObject Obj2 in Obj.ChildObjects)
                {
                    MaterialsChanged.Add(Obj2);
                    for (int i = 0; i < Obj2.MaterialIndex.Length; i++)
                    {
                        Obj2.MaterialIndex[i] = Obj2.LocalMaterialIndex[i] + MatCount;
                    }
                }
                _Materials.AddRange(Obj._Materials);
                MatCount += Obj._Materials.Count;
            }
            int TerrainMaterials = 0;
            if (TerrainCount != 0)
            {
                foreach (TerrainObject Obj2 in Terrains)
                {
                    for (int i = 0; i < Obj2.MaterialIndex.Length; i++)
                    {
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

            if (TerrainCount != 0)
            {
                if (TerrainBuffer != null) TerrainBuffer.Release();
                TerrainBuffer = new ComputeBuffer(TerrainCount, 56);
                TerrainBuffer.SetData(TerrainInfos);
            }

            foreach (VoxelObject Vox in VoxelRenderQue)
            {
                foreach (MaterialData Mat in Vox._Materials)
                {
                    _Materials.Add(Mat);
                }
            }
            NormalSize = NormalAtlas.width;
            EmissiveSize = EmissiveAtlas.width;

        }
        public static AssetManager Assets;
        public void Start() {
            Assets = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            gameObject.GetComponent<RayTracingMaster>().Start2();
        }

        public void ForceUpdateAtlas() {
            for(int i = 0; i < RenderQue.Count; i++) {
                RenderQue[i].CreateAtlas();
            }
            CreateAtlas();
        }

        public void OnApplicationQuit()
        {
            RunningTasks = 0;
            #if HardwareRT
                AccelStruct.Release();
            #endif
            if (BVH8AggregatedBuffer != null)
            {
                BVH8AggregatedBuffer.Release();
                BVH8AggregatedBuffer = null;
                AggTriBuffer.Release();
                AggTriBuffer = null;
            }
            for(int i = 0; i < WorkingBuffer.Length; i++) WorkingBuffer[i]?.Release();
            if (NodeBuffer != null) NodeBuffer.Release();
            if (StackBuffer != null) StackBuffer.Release();
            if (ToBVHIndexBuffer != null) ToBVHIndexBuffer.Release();
            if (BVHDataBuffer != null) BVHDataBuffer.Release();
            if (BVHBuffer != null) BVHBuffer.Release();
            if (BoxesBuffer != null) BoxesBuffer.Release();
            if (TerrainBuffer != null) TerrainBuffer.Release();
        }


        private TextAsset XMLObject;
        [HideInInspector] public static List<string> ShaderNames;
        [HideInInspector] public static Materials data = new Materials();
        [HideInInspector] public bool NeedsToUpdateXML;
        public void AddMaterial(Shader shader)
        {
            int ShaderPropertyCount = shader.GetPropertyCount();
            string NormalTex = "null";
            string EmissionTex = "null";
            string MetallicTex = "null";
            int MetallicIndex = 0;
            string RoughnessTex = "null";
            int RoughnessIndex = 0;
            string MetallicRange = "null";
            string RoughnessRange = "null";
            data.Material.Add(new MaterialShader()
            {
                Name = shader.name,
                BaseColorTex = "_MainTex",
                NormalTex = NormalTex,
                EmissionTex = EmissionTex,
                MetallicTex = MetallicTex,
                MetallicTexChannel = MetallicIndex,
                MetallicRange = MetallicRange,
                RoughnessTex = RoughnessTex,
                RoughnessTexChannel = RoughnessIndex,
                RoughnessRange = RoughnessRange,
                IsGlass = false,
                IsCutout = false,
                BaseColorValue = "null"
            });
            ShaderNames.Add(shader.name);
            NeedsToUpdateXML = true;
        }
        public void UpdateMaterialDefinition()
        {
                XMLObject = Resources.Load<TextAsset>("Utility/MaterialMappings");
         #if UNITY_EDITOR
                    if(XMLObject == null) {
                        XMLObject = new TextAsset();
                        var g = UnityEditor.AssetDatabase.FindAssets ( $"t:Script {nameof(AssetManager)}" );
                        var Path = UnityEditor.AssetDatabase.GUIDToAssetPath ( g [ 0 ] );
                        Path = Path.Replace("AssetManager.cs", "Utility/MaterialMappings.xml").Replace("Assets", Application.dataPath);
                        using(var A = File.OpenWrite(Path)) {
                            byte[] info = new System.Text.UTF8Encoding(true).GetBytes("<?xml version=\"1.0\"?>\n");
                            A.Write(info, 0, info.Length);
                            info = new System.Text.UTF8Encoding(true).GetBytes("<Materials xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\n");
                            A.Write(info, 0, info.Length);
                            info = new System.Text.UTF8Encoding(true).GetBytes("</Materials>\n");
                            A.Write(info, 0, info.Length);
                        }//
                        UnityEditor.AssetDatabase.Refresh();
                        XMLObject = Resources.Load<TextAsset>("Utility/MaterialMappings");

                    }
                #endif
            ShaderNames = new List<string>();
            using (var A = new StringReader(XMLObject.text))
            {
                var serializer = new XmlSerializer(typeof(Materials));

                data = serializer.Deserialize(A) as Materials;
            }
            foreach (var Mat in data.Material)
            {
                ShaderNames.Add(Mat.Name);
            }
        }

        void OnDisable() {
            ClearAll();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void init()
        {
            #if HardwareRT
                AccelStruct = new UnityEngine.Rendering.RayTracingAccelerationStructure();
            #endif
            if(AlbedoAtlas == null || AlbedoAtlas.width != DesiredRes) AlbedoAtlas = new Texture2D(DesiredRes,DesiredRes, TextureFormat.RGBA32, false);
            UpdateMaterialDefinition();
            UnityEngine.Video.VideoPlayer[] VideoObjects = GameObject.FindObjectsOfType<UnityEngine.Video.VideoPlayer>();
            if (VideoTexture != null) VideoTexture.Release();
            if (VideoObjects.Length == 0)
            {
                CreateRenderTexture(ref VideoTexture, 1, 1);
            }
            else
            {
                GameObject VideoAttatchedObject = VideoObjects[0].gameObject;
                VideoPlayerObject = (VideoAttatchedObject.GetComponent<VideoObject>() == null) ? VideoAttatchedObject.AddComponent<VideoObject>() : VideoAttatchedObject.GetComponent<VideoObject>();
                VideoTexture = VideoPlayerObject.VideoTexture;
            }
            #if DoLightMapping
                LightMapTris = new List<LightMapTriangle>();
            #endif
            Refitter = Resources.Load<ComputeShader>("Utility/BVHRefitter");
            RefitLayer = Refitter.FindKernel("RefitBVHLayer");
            NodeUpdater = Refitter.FindKernel("NodeUpdate");
            NodeCompress = Refitter.FindKernel("NodeCompress");
            NodeInitializerKernel = Refitter.FindKernel("NodeInitializer");

            MaterialsChanged = new List<RayTracingObject>();
            InstanceData = GameObject.Find("InstancedStorage").GetComponent<InstancedManager>();
            Instances = new List<InstancedObject>();
            SunDirection = new Vector3(0, -1, 0);
            AddQue = new List<ParentObject>();
            RemoveQue = new List<ParentObject>();
            RenderQue = new List<ParentObject>();
            UpdateQue = new List<ParentObject>();
            InstanceRenderQue = new List<InstancedObject>();
            InstanceBuildQue = new List<InstancedObject>();
            InstanceAddQue = new List<InstancedObject>();
            InstanceRemoveQue = new List<InstancedObject>();
            VoxelAddQue = new List<VoxelObject>();
            VoxelRemoveQue = new List<VoxelObject>();
            VoxelRenderQue = new List<VoxelObject>();
            VoxelBuildQue = new List<VoxelObject>();
            CurrentlyActiveVoxelTasks = new List<Task>();
            BuildQue = new List<ParentObject>();
            MyMeshesCompacted = new List<MyMeshDataCompacted>();
            AggLightTriangles = new List<CudaLightTriangle>();
            UnityLights = new List<LightData>();
            LightMeshes = new List<LightMeshData>();
            LightTransforms = new List<Transform>();
            Lights = new List<Light>();
            LightMeshCount = 0;
            UnityLightCount = 0;
            LightTriCount = 0;
            TerrainInfos = new List<TerrainDat>();
            if (BVH8AggregatedBuffer != null)
            {
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
            if (TerrainBuffer != null) TerrainBuffer.Release();
            TerrainBuffer = new ComputeBuffer(1, 56);

            if (Terrains.Count != 0) for (int i = 0; i < Terrains.Count; i++) Terrains[i].Load();
        }

        private void UpdateRenderAndBuildQues()
        {
            ChildrenUpdated = false;
            OnlyInstanceUpdated = false;

            int AddQueCount = 0;
            int RemoveQueCount = RemoveQue.Count - 1;
            int RenderQueCount = 0;
            int BuildQueCount = 0;
            {//Main Object Data Handling
             UnityEngine.Profiling.Profiler.BeginSample("Remove");
                for (int i = RemoveQueCount; i >= 0; i--)
                {
                    switch(RemoveQue[i].ExistsInQue) {
                        case 0:
                            RenderQue.Remove(RemoveQue[i]);
                        break;
                        case 1:
                            BuildQue.Remove(RemoveQue[i]);
                        break;
                        case 2:
                            UpdateQue.Remove(RemoveQue[i]);
                        break;
                        case 3:
                            AddQue.Remove(RemoveQue[i]);
                        break;
                    }
                    RemoveQue[i].ExistsInQue = -1;
                    ChildrenUpdated = true;
                }
                RenderQueCount = UpdateQue.Count - 1;
                for (int i = RenderQueCount; i >= 0; i--)
                {
                    if(UpdateQue[i] == null) continue;
                    switch(UpdateQue[i].ExistsInQue) {
                        case 0:
                            RenderQue.Remove(UpdateQue[i]);
                        break;
                        case 1:
                            BuildQue.Remove(UpdateQue[i]);
                        break;
                    }
                    UpdateQue[i].ExistsInQue = -1;
                    ChildrenUpdated = true;
                }
                RemoveQue.Clear();
                AddQueCount = AddQue.Count - 1;
                for (int i = AddQueCount; i >= 0; i--)
                {
                    var CurrentRep = BuildQue.Count;
                    bool Contained = AddQue[i].ExistsInQue != 3;

                    if(!Contained || (AddQue[i].AsyncTask == null || AddQue[i].AsyncTask.Status != TaskStatus.RanToCompletion)) {
                        AddQue[i].ExistsInQue = 1;
                        BuildQue.Add(AddQue[i]);
                        AddQue[i].LoadData();
                        AddQue[i].AsyncTask = Task.Run(() => BuildQue[CurrentRep].BuildTotal());
                        ChildrenUpdated = true;
                    }
                }
                AddQue.Clear();
                BuildQueCount = BuildQue.Count - 1;
                RenderQueCount = UpdateQue.Count - 1;
                for (int i = BuildQueCount; i >= 0; i--)
                {//Promotes from Build Que to Render Que
                    if (BuildQue[i].AsyncTask.IsFaulted)
                    {//Fuck, something fucked up
                        Debug.Log(BuildQue[i].AsyncTask.Exception + ", " + BuildQue[i].Name);
                        BuildQue[i].ExistsInQue = 3;
                        AddQue.Add(BuildQue[i]);
                        BuildQue.RemoveAt(i);
                    }
                    else
                    {
                        if (BuildQue[i].AsyncTask.Status == TaskStatus.RanToCompletion)
                        {
                            if (BuildQue[i].AggTriangles == null || BuildQue[i].AggNodes == null)
                            {
                                BuildQue[i].ExistsInQue = 3;
                                AddQue.Add(BuildQue[i]);
                                BuildQue.RemoveAt(i);
                            }
                            else
                            {
                                BuildQue[i].SetUpBuffers();
                                BuildQue[i].ExistsInQue = 0;
                                RenderQue.Add(BuildQue[i]);
                                BuildQue.RemoveAt(i);
                                ChildrenUpdated = true;
                            }
                        }
                    }
                }
            UnityEngine.Profiling.Profiler.EndSample();
            }
            for (int i = RenderQueCount; i >= 0; i--)
            {//Demotes from Render Que to Build Que in case mesh has changed
                if (UpdateQue[i] != null && UpdateQue[i].gameObject.activeInHierarchy)
                {
                    UpdateQue[i].ClearAll();
                    UpdateQue[i].LoadData();
                    if (UpdateQue[i].ExistsInQue != 1)
                    {
                        UpdateQue[i].ExistsInQue = 1;
                        BuildQue.Add(UpdateQue[i]);
                        var TempBuildQueCount = BuildQue.Count - 1;
                        UpdateQue[i].AsyncTask = Task.Run(() => BuildQue[TempBuildQueCount].BuildTotal());
                    }
                    UpdateQue.RemoveAt(i);
                    ChildrenUpdated = true;
                }
                else
                {
                    UpdateQue.RemoveAt(i);
                }
            }
            AddQueCount = InstanceAddQue.Count - 1;
            RemoveQueCount = InstanceRemoveQue.Count - 1;
            {//Instanced Models Data Handling
                InstanceData.UpdateRenderAndBuildQues(ref ChildrenUpdated);
                for (int i = AddQueCount; i >= 0; i--)
                {
                    if (InstanceAddQue[i].InstanceParent != null)
                    {
                        InstanceBuildQue.Add(InstanceAddQue[i]);
                        InstanceAddQue.RemoveAt(i);
                    }
                }
                for (int i = RemoveQueCount; i >= 0; i--)
                {
                    if (InstanceRenderQue.Contains(InstanceRemoveQue[i]))
                        InstanceRenderQue.Remove(InstanceRemoveQue[i]);
                    else if (InstanceBuildQue.Contains(InstanceRemoveQue[i]))
                    {
                        InstanceBuildQue.Remove(InstanceRemoveQue[i]);
                    }
                    else
                        Debug.Log("REMOVE QUE NOT FOUND");
                    OnlyInstanceUpdated = true;
                    InstanceRemoveQue.RemoveAt(i);
                }
                RenderQueCount = InstanceRenderQue.Count - 1;
                BuildQueCount = InstanceBuildQue.Count - 1;
                for (int i = RenderQueCount; i >= 0; i--)
                {//Demotes from Render Que to Build Que in case mesh has changed
                    // InstanceRenderQue[i].UpdateInstance();
                    ParentObject InstanceParent = InstanceRenderQue[i].InstanceParent;
                    if (InstanceParent == null || InstanceParent.gameObject.activeInHierarchy == false || InstanceParent.HasCompleted == false)
                    {
                        InstanceBuildQue.Add(InstanceRenderQue[i]);
                        InstanceRenderQue.RemoveAt(i);
                        OnlyInstanceUpdated = true;
                    }
                }
                for (int i = BuildQueCount; i >= 0; i--)
                {//Promotes from Build Que to Render Que
                    if (InstanceBuildQue[i].InstanceParent.HasCompleted == true)
                    {
                        InstanceRenderQue.Add(InstanceBuildQue[i]);
                        InstanceBuildQue.RemoveAt(i);
                        OnlyInstanceUpdated = true;
                    }
                }
            }
            AddQueCount = VoxelAddQue.Count - 1;
            RemoveQueCount = VoxelRemoveQue.Count - 1;
            {//Voxel Que Data Handling
                for (int i = AddQueCount; i >= 0; i--)
                {
                    var CurrentRep = VoxelBuildQue.Count;
                    VoxelBuildQue.Add(VoxelAddQue[i]);
                    VoxelAddQue.RemoveAt(i);
                    VoxelBuildQue[CurrentRep].LoadData();
                    CurrentlyActiveVoxelTasks.Add(Task.Run(() => VoxelBuildQue[CurrentRep].BuildOctree()));
                    ChildrenUpdated = true;
                }
                for (int i = RemoveQueCount; i >= 0; i--)
                {
                    if (VoxelRenderQue.Contains(VoxelRemoveQue[i]))
                        VoxelRenderQue.Remove(VoxelRemoveQue[i]);
                    else if (VoxelBuildQue.Contains(VoxelRemoveQue[i]))
                    {
                        CurrentlyActiveVoxelTasks.RemoveAt(VoxelBuildQue.IndexOf(VoxelRemoveQue[i]));
                        VoxelBuildQue.Remove(VoxelRemoveQue[i]);
                    }
                    else
                        Debug.Log("REMOVE QUE NOT FOUND");
                    ChildrenUpdated = true;
                    VoxelRemoveQue.RemoveAt(i);
                }
                BuildQueCount = VoxelBuildQue.Count - 1;
                for (int i = BuildQueCount; i >= 0; i--)
                {//Promotes from Build Que to Render Que
                    if (CurrentlyActiveVoxelTasks[i].IsFaulted) Debug.Log(CurrentlyActiveVoxelTasks[i].Exception);//Fuck, something fucked up
                    if (CurrentlyActiveVoxelTasks[i].Status == TaskStatus.RanToCompletion)
                    {
                        VoxelRenderQue.Add(VoxelBuildQue[i]);
                        VoxelBuildQue.RemoveAt(i);
                        CurrentlyActiveVoxelTasks.RemoveAt(i);
                        ChildrenUpdated = true;
                    }
                }
            }


            if (OnlyInstanceUpdated && !ChildrenUpdated)
            {
                ChildrenUpdated = true;
            }
            else
            {
                OnlyInstanceUpdated = false;
            }
            if (ChildrenUpdated || ParentCountHasChanged) MeshAABBs = new AABB[RenderQue.Count + InstanceRenderQue.Count];
            if (ChildrenUpdated || ParentCountHasChanged) UnsortedAABBs = new AABB[RenderQue.Count + InstanceRenderQue.Count];
            if (ChildrenUpdated || ParentCountHasChanged) VoxelAABBs = new AABB[VoxelRenderQue.Count];
        }
        public void EditorBuild()
        {//Forces all to rebuild
            ClearAll();
            Terrains = new List<TerrainObject>();
            Terrains = new List<TerrainObject>(GetComponentsInChildren<TerrainObject>());
            init();
            AddQue = new List<ParentObject>();
            RemoveQue = new List<ParentObject>();
            RenderQue = new List<ParentObject>();
            CurrentlyActiveVoxelTasks = new List<Task>();
            BuildQue = new List<ParentObject>(GetComponentsInChildren<ParentObject>());
            VoxelBuildQue = new List<VoxelObject>(GetComponentsInChildren<VoxelObject>());
            RunningTasks = 0;
            InstanceData.EditorBuild();
            for (int i = 0; i < BuildQue.Count; i++)
            {
                var CurrentRep = i;
                BuildQue[CurrentRep].LoadData();
                BuildQue[CurrentRep].AsyncTask = Task.Run(() => { BuildQue[CurrentRep].BuildTotal(); RunningTasks--; });
                BuildQue[CurrentRep].ExistsInQue = 1;
                RunningTasks++;
            }
            for (int i = 0; i < VoxelBuildQue.Count; i++)
            {
                var CurrentRep = i;
                VoxelBuildQue[CurrentRep].LoadData();
                //VoxelBuildQue[CurrentRep].BuildOctree();
                Task t1 = Task.Run(() => VoxelBuildQue[CurrentRep].BuildOctree());
                CurrentlyActiveVoxelTasks.Add(t1);
            }
            if (AggLightTriangles.Count == 0) { AggLightTriangles.Add(new CudaLightTriangle() { }); }
            didstart = false;
        }
        public void BuildCombined()
        {//Only has unbuilt be built
            HasToDo = false;
            Terrains = new List<TerrainObject>(GetComponentsInChildren<TerrainObject>());
            init();
            CurrentlyActiveVoxelTasks = new List<Task>();
            List<ParentObject> TempQue = new List<ParentObject>(GetComponentsInChildren<ParentObject>());
            InstanceAddQue = new List<InstancedObject>(GetComponentsInChildren<InstancedObject>());
            InstanceData.BuildCombined();
            List<VoxelObject> TempVoxelQue = new List<VoxelObject>(GetComponentsInChildren<VoxelObject>());
            RunningTasks = 0;
            int TotLength = 0;
            int MeshOffset = 0;
            for (int i = 0; i < TempQue.Count; i++)
            {
                if (TempQue[i].HasCompleted && !TempQue[i].NeedsToUpdate) {
                    TempQue[i].ExistsInQue = 0;
                    RenderQue.Add(TempQue[i]);
                }
                else {TempQue[i].ExistsInQue = 1; BuildQue.Add(TempQue[i]); RunningTasks++;}
            }
            for (int i = 0; i < TempVoxelQue.Count; i++)
            {
                if (TempVoxelQue[i].HasCompleted) VoxelRenderQue.Add(TempVoxelQue[i]);
                else VoxelBuildQue.Add(TempVoxelQue[i]);
            }
            for (int i = 0; i < BuildQue.Count; i++)
            {
                var CurrentRep = i;
                BuildQue[CurrentRep].LoadData();
                BuildQue[CurrentRep].AsyncTask = Task.Run(() => {BuildQue[CurrentRep].BuildTotal(); RunningTasks--;});
            }
            for (int i = 0; i < VoxelBuildQue.Count; i++)
            {
                var CurrentRep = i;
                VoxelBuildQue[CurrentRep].LoadData();
                CurrentlyActiveVoxelTasks.Add(Task.Run(() => VoxelBuildQue[CurrentRep].BuildOctree()));
            }
            ParentCountHasChanged = true;
            if (AggLightTriangles.Count == 0) { AggLightTriangles.Add(new CudaLightTriangle() { }); }
            if (RenderQue.Count != 0) {CommandBuffer cmd = new CommandBuffer(); bool throwaway = UpdateTLAS(cmd);  Graphics.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        cmd.Release();}
        }
        [HideInInspector] public bool HasToDo;

        private void AccumulateData(CommandBuffer cmd)
        {
            UnityEngine.Profiling.Profiler.BeginSample("Update Object Lists");
            UpdateRenderAndBuildQues();
            UnityEngine.Profiling.Profiler.EndSample();

            int ParentsLength = RenderQue.Count;
            TLASSpace = 2 * (ParentsLength + InstanceRenderQue.Count);
            int nodes = TLASSpace;
            if (ChildrenUpdated || ParentCountHasChanged)
            {
                TotalParentObjectSize = 0;
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
                if (BVH8AggregatedBuffer != null)
                {
                    BVH8AggregatedBuffer.Release();
                    AggTriBuffer.Release();
                }
                for (int i = 0; i < ParentsLength; i++)
                {
                    AggNodeCount += RenderQue[i].AggNodes.Length;
                    AggTriCount += RenderQue[i].AggTriangles.Length;
                }
                for (int i = 0; i < InstanceData.RenderQue.Count; i++)
                {
                    AggNodeCount += InstanceData.RenderQue[i].AggNodes.Length;
                    AggTriCount += InstanceData.RenderQue[i].AggTriangles.Length;
                }
                Debug.Log("Total Tri Count: " + AggTriCount);
                if (AggNodeCount != 0)
                {//Accumulate the BVH nodes and triangles for all normal models
                    BVH8AggregatedBuffer = new ComputeBuffer(AggNodeCount, 80);
                    AggTriBuffer = new ComputeBuffer(AggTriCount, 88);
                    MeshFunctions.SetBuffer(TriangleBufferKernel, "OutCudaTriArray", AggTriBuffer);
                    MeshFunctions.SetBuffer(NodeBufferKernel, "OutAggNodes", BVH8AggregatedBuffer);
                    int MatOffset = 0;
                    for (int i = 0; i < ParentsLength; i++)
                    {
                        RenderQue[i].UpdateData();

                        MeshFunctions.SetInt("Offset", CurTriOffset);
                        MeshFunctions.SetInt("Count", RenderQue[i].TriBuffer.count);
                        MeshFunctions.SetBuffer(TriangleBufferKernel, "InCudaTriArray", RenderQue[i].TriBuffer);
                        MeshFunctions.Dispatch(TriangleBufferKernel, (int)Mathf.Ceil(RenderQue[i].TriBuffer.count / 248.0f), 1, 1);

                        MeshFunctions.SetInt("Offset", CurNodeOffset);
                        MeshFunctions.SetInt("Count", RenderQue[i].BVHBuffer.count);
                        MeshFunctions.SetBuffer(NodeBufferKernel, "InAggNodes", RenderQue[i].BVHBuffer);
                        MeshFunctions.Dispatch(NodeBufferKernel, (int)Mathf.Ceil(RenderQue[i].BVHBuffer.count / 248.0f), 1, 1);
                        TotalParentObjectSize += RenderQue[i].TriBuffer.count * RenderQue[i].TriBuffer.stride;
                        TotalParentObjectSize += RenderQue[i].BVHBuffer.count * RenderQue[i].BVHBuffer.stride;
                        RenderQue[i].NodeOffset = CurNodeOffset;
                        RenderQue[i].TriOffset = CurTriOffset;
                        CurNodeOffset += RenderQue[i].AggNodes.Length;
                        CurTriOffset += RenderQue[i].AggTriangles.Length;
                        if (RenderQue[i].LightTriangles.Count != 0)
                        {
                            LightMeshCount++;
                            CDF += RenderQue[i].TotEnergy;
                            LightTransforms.Add(RenderQue[i].transform);
                            LightMeshes.Add(new LightMeshData()
                            {
                                energy = RenderQue[i].TotEnergy,
                                CDF = CDF,
                                StartIndex = AggLightTriangles.Count,
                                IndexEnd = RenderQue[i].LightTriangles.Count + AggLightTriangles.Count,
                                MatOffset = MatOffset,
                                OrigionalMesh = i,
                                LockedMeshIndex = i
                            });
                            AggLightTriangles.AddRange(RenderQue[i].LightTriangles);
                        }
                        #if DoLightMapping
                            LightMapTris.AddRange(RenderQue[i].LightMapTris);
                        #endif
                        MatOffset += RenderQue[i]._Materials.Count;
                    }
                    #if DoLightMapping
                        LightMapTris.Sort((s1, s2) => s1.LightMapIndex.CompareTo(s2.LightMapIndex));
                        LightMapTris = LightMapTris.Distinct().ToList();
                    #endif
                    for (int i = 0; i < InstanceData.RenderQue.Count; i++)
                    {//Accumulate the BVH nodes and triangles for all instanced models
                        InstanceData.RenderQue[i].UpdateData();

                        MeshFunctions.SetInt("Offset", CurTriOffset);
                        MeshFunctions.SetInt("Count", InstanceData.RenderQue[i].TriBuffer.count);
                        MeshFunctions.SetBuffer(TriangleBufferKernel, "InCudaTriArray", InstanceData.RenderQue[i].TriBuffer);
                        MeshFunctions.Dispatch(TriangleBufferKernel, (int)Mathf.Ceil(InstanceData.RenderQue[i].TriBuffer.count / 248.0f), 1, 1);

                        MeshFunctions.SetInt("Offset", CurNodeOffset);
                        MeshFunctions.SetInt("Count", InstanceData.RenderQue[i].BVHBuffer.count);
                        MeshFunctions.SetBuffer(NodeBufferKernel, "InAggNodes", InstanceData.RenderQue[i].BVHBuffer);
                        MeshFunctions.Dispatch(NodeBufferKernel, (int)Mathf.Ceil(InstanceData.RenderQue[i].BVHBuffer.count / 248.0f), 1, 1);

                        InstanceData.RenderQue[i].NodeOffset = CurNodeOffset;
                        InstanceData.RenderQue[i].TriOffset = CurTriOffset;
                        CurNodeOffset += InstanceData.RenderQue[i].AggNodes.Length;
                        CurTriOffset += InstanceData.RenderQue[i].AggTriangles.Length;
                    }
                    for (int i = 0; i < InstanceRenderQue.Count; i++)
                    {
                        if (InstanceRenderQue[i].InstanceParent.LightCount != 0)
                        {
                            LightMeshCount++;
                            CDF += InstanceRenderQue[i].InstanceParent.TotEnergy;
                            LightTransforms.Add(InstanceRenderQue[i].transform);
                            LightMeshes.Add(new LightMeshData()
                            {
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
                for (int i = 0; i < VoxelRenderQue.Count; i++)
                {
                    //   Debug.Log("SIZE: " + VoxelRenderQue[i].BrickmapTraverse.Count);
                    GPUBrickmap.AddRange(VoxelRenderQue[i].BrickmapTraverse);
                }

                if (GPUBrickmap.Count == 0)
                {
                    UseVoxels = false;
                    GPUBrickmap.Add(0);
                }
                else
                {
                    UseVoxels = true;
                }
                if (LightMeshCount == 0) { LightMeshes.Add(new LightMeshData() { }); }
                if (AggLightTriangles.Count == 0) { AggLightTriangles.Add(new CudaLightTriangle() { }); LightTriCount = 0; } else { LightTriCount = AggLightTriangles.Count; }


                if (!OnlyInstanceUpdated || _Materials.Count == 0) CreateAtlas();
            }
            ParentCountHasChanged = false;
            if (UseSkinning && didstart)
            {
                for (int i = 0; i < ParentsLength; i++)
                {//Refit BVH's of skinned meshes
                    if (RenderQue[i].IsSkinnedGroup)//this can be optimized to operate directly on the triangle buffer instead of needing to copy it
                    {
                        cmd.SetComputeIntParam(RenderQue[i].MeshRefit, "TriBuffOffset", RenderQue[i].TriOffset);
                        RenderQue[i].RefitMesh(ref BVH8AggregatedBuffer, ref AggTriBuffer, cmd);
                    }
                }
            }
        }

        public struct AggData
        {
            public int AggIndexCount;
            public int AggNodeCount;
            public int MaterialOffset;
            public int mesh_data_bvh_offsets;
            public int LightTriCount;
        }


        public void CreateAABB(Transform transform, ref AABB aabb)
        {//Update the Transformed AABB by getting the new Max/Min of the untransformed AABB after transforming it
            Vector3 center = 0.5f * (aabb.BBMin + aabb.BBMax);
            Vector3 extent = 0.5f * (aabb.BBMax - aabb.BBMin);
            Matrix4x4 Mat = transform.localToWorldMatrix;
            Vector3 new_center = CommonFunctions.transform_position(Mat, center);
            Vector3 new_extent = CommonFunctions.transform_direction(Mat, extent);

            aabb.BBMin = new_center - new_extent;
            aabb.BBMax = new_center + new_extent;
        }

        AABB tempAABB;
        int[] ToBVHIndex;
        [HideInInspector] public BVH8Builder TLASBVH8;

        int TempRecur = 0;
        unsafe public void DocumentNodes(int CurrentNode, int ParentNode, int NextNode, int NextBVH8Node, bool IsLeafRecur, int CurRecur)
        {
            NodeIndexPairData CurrentPair = NodePair[CurrentNode];
            TempRecur = Mathf.Max(TempRecur, CurRecur);
            CurrentPair.PreviousNode = ParentNode;
            CurrentPair.Node = CurrentNode;
            CurrentPair.RecursionCount = CurRecur;
            if (!IsLeafRecur)
            {
                ToBVHIndex[NextBVH8Node] = CurrentNode;
                CurrentPair.IsLeaf = 0;
                BVHNode8Data node = TLASBVH8.BVH8Nodes[NextBVH8Node];
                NodeIndexPairData IndexPair = new NodeIndexPairData();

                IndexPair.AABB = new AABB();
                float ex = (float)System.Convert.ToSingle((int)(System.Convert.ToUInt32(node.e[0]) << 23));
                float ey = (float)System.Convert.ToSingle((int)(System.Convert.ToUInt32(node.e[1]) << 23));
                float ez = (float)System.Convert.ToSingle((int)(System.Convert.ToUInt32(node.e[2]) << 23));
                Vector3 e = new Vector3(ex, ey, ez);
                for (int i = 0; i < 8; i++)
                {
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
                    if (IsLeaf)
                    {
                        IndexPair.BVHNode = NextBVH8Node;
                        NodePair.Add(IndexPair);
                        NextNode++;
                        DocumentNodes(NodePair.Count - 1, CurrentNode, NextNode, -1, true, CurRecur + 1);
                    }
                    else
                    {
                        int child_offset = (byte)node.meta[i] & 0b11111;
                        int child_index = (int)node.base_index_child + child_offset - 24;

                        IndexPair.BVHNode = NextBVH8Node;
                        NodePair.Add(IndexPair);
                        NextNode++;
                        DocumentNodes(NodePair.Count - 1, CurrentNode, NextNode, child_index, false, CurRecur + 1);
                    }
                }
            }
            else
            {
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
        ComputeBuffer[] WorkingBuffer;
        ComputeBuffer NodeBuffer;
        ComputeBuffer StackBuffer;
        ComputeBuffer ToBVHIndexBuffer;
        ComputeBuffer BVHDataBuffer;
        ComputeBuffer BVHBuffer;

        unsafe public void ConstructNewTLAS()
        {

            #if HardwareRT
                int TotLength = 0;
                int MeshOffset = 0;
                SubMeshOffsets = new List<int>();
                MeshOffsets = new List<Vector2>();

                for(int i = 0; i < RenderQue.Count; i++) {
                    RenderQue[i].HWRTIndex = new List<int>();
                    foreach(var A in RenderQue[i].Renderers) {
                        MeshOffsets.Add(new Vector2(SubMeshOffsets.Count, i));
                        Mesh mesh = ((A.gameObject.GetComponent<MeshFilter>() != null) ? A.gameObject.GetComponent<MeshFilter>().sharedMesh : A.gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh);
                        for (int i2 = 0; i2 < mesh.subMeshCount; ++i2)
                        {//Add together all the submeshes in the mesh to consider it as one object
                            SubMeshOffsets.Add(TotLength);
                            int IndiceLength = (int)mesh.GetIndexCount(i2) / 3;
                            TotLength += IndiceLength;
                        }
                        RayTracingSubMeshFlags[] B = new RayTracingSubMeshFlags[mesh.subMeshCount];
                        for(int i2 = 0; i2 < B.Length; i2++) {
                            B[i2] = RayTracingSubMeshFlags.Enabled;
                        }
                        AccelStruct.AddInstance(A, B, true, false, 0xff, (uint)MeshOffset);
                        MeshOffset++;
                    }
                }
            AccelStruct.Build();
            #else
                TLASTask = null;
                BVH2Builder BVH = new BVH2Builder(MeshAABBs);
                TLASBVH8 = new BVH8Builder(BVH, ref MyMeshesCompacted);
                for (int i = 0; i < TLASBVH8.cwbvh_indices.Length; i++) {
                    if (TLASBVH8.cwbvh_indices[i] >= RenderQue.Count) {
                        InstanceRenderQue[TLASBVH8.cwbvh_indices[i] - RenderQue.Count].CompactedMeshData = i;
                    }
                    else RenderQue[TLASBVH8.cwbvh_indices[i]].CompactedMeshData = i;
                }
                System.Array.Resize(ref TLASBVH8.BVH8Nodes, TLASBVH8.cwbvhnode_count);
                ToBVHIndex = new int[TLASBVH8.cwbvhnode_count];
                if (TempBVHArray == null || TLASBVH8.BVH8Nodes.Length != TempBVHArray.Length) TempBVHArray = new BVHNode8DataCompressed[TLASBVH8.BVH8Nodes.Length];
                CommonFunctions.Aggregate(ref TempBVHArray, ref TLASBVH8.BVH8Nodes);
                BVHNodeCount = TLASBVH8.BVH8Nodes.Length;
                CWBVHIndicesBufferInverted = new int[TLASBVH8.cwbvh_indices.Length];
                int CWBVHIndicesBufferCount = CWBVHIndicesBufferInverted.Length;
                for (int i = 0; i < CWBVHIndicesBufferCount; i++) CWBVHIndicesBufferInverted[TLASBVH8.cwbvh_indices[i]] = i;
                NodePair = new List<NodeIndexPairData>();
                NodePair.Add(new NodeIndexPairData());
                DocumentNodes(0, 0, 1, 0, false, 0);
                TempRecur++;
                MaxRecur = TempRecur;
                int NodeCount = NodePair.Count;
                ForwardStack = new Layer[NodeCount];
                for (int i = 0; i < NodePair.Count; i++) {
                    for (int i2 = 0; i2 < 8; i2++) {
                        ForwardStack[i].Children[i2] = -1;
                        ForwardStack[i].Leaf[i2] = -1;
                    }
                }

                for (int i = 0; i < NodePair.Count; i++) {
                    if (NodePair[i].IsLeaf == 1) {
                        int first_triangle = (byte)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] & 0b11111;
                        int NumBits = CommonFunctions.NumberOfSetBits((byte)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] >> 5);
                        ForwardStack[i].Children[NodePair[i].InNodeOffset] = NumBits;
                        ForwardStack[i].Leaf[NodePair[i].InNodeOffset] = (int)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].base_index_triangle + first_triangle + 1;
                    }
                    else {
                        ForwardStack[i].Children[NodePair[i].InNodeOffset] = i;
                        ForwardStack[i].Leaf[NodePair[i].InNodeOffset] = 0;
                    }
                    ForwardStack[NodePair[i].PreviousNode].Children[NodePair[i].InNodeOffset] = i;
                    ForwardStack[NodePair[i].PreviousNode].Leaf[NodePair[i].InNodeOffset] = 0;
                }

                LayerStack = new Layer2[MaxRecur];
                for (int i = 0; i < MaxRecur; i++) {
                    Layer2 TempSlab = new Layer2();
                    TempSlab.Slab = new List<int>();
                    LayerStack[i] = TempSlab;
                }
                for (int i = 0; i < NodePair.Count; i++) {
                    var TempLayer = LayerStack[NodePair[i].RecursionCount];
                    TempLayer.Slab.Add(i);
                    LayerStack[NodePair[i].RecursionCount] = TempLayer;
                }
                CommonFunctions.ConvertToSplitNodes(TLASBVH8, ref SplitNodes);
                int MaxLength = 0;
                List<Layer2> TempStack = new List<Layer2>();
                for (int i = 0; i < LayerStack.Length; i++) 
                {  
                    if(LayerStack[i].Slab.Count != 0) {
                        TempStack.Add(LayerStack[i]);
                    }
                }
                MaxRecur = TempStack.Count;
                LayerStack = TempStack.ToArray();

                if(WorkingBuffer != null) for(int i = 0; i < WorkingBuffer.Length; i++) WorkingBuffer[i]?.Release();
                if (NodeBuffer != null) NodeBuffer.Release();
                if (StackBuffer != null) StackBuffer.Release();
                if (ToBVHIndexBuffer != null) ToBVHIndexBuffer.Release();
                if (BVHDataBuffer != null) BVHDataBuffer.Release();
                if (BVHBuffer != null) BVHBuffer.Release();
                if (BoxesBuffer != null) BoxesBuffer.Release();
                WorkingBuffer = new ComputeBuffer[LayerStack.Length];
                for (int i = 0; i < MaxRecur; i++)
                {
                    WorkingBuffer[i] = new ComputeBuffer(LayerStack[i].Slab.Count, 4);
                    WorkingBuffer[i].SetData(LayerStack[i].Slab);
                }
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
            #endif
        }
        List<MyMeshDataCompacted> BackupMeshesCompacted;
        List<MyMeshDataCompacted> OGBackupMeshesCompacted;
        Task TLASTask;
        unsafe async void CorrectRefit(AABB[] Boxes) {
            TempRecur = 0;
            BackupMeshesCompacted = new List<MyMeshDataCompacted>(OGBackupMeshesCompacted);
            BVH2Builder BVH = new BVH2Builder(Boxes);
            TLASBVH8 = new BVH8Builder(BVH, ref BackupMeshesCompacted);
 System.Array.Resize(ref TLASBVH8.BVH8Nodes, TLASBVH8.cwbvhnode_count);
                ToBVHIndex = new int[TLASBVH8.cwbvhnode_count];
                if (TempBVHArray == null || TLASBVH8.BVH8Nodes.Length != TempBVHArray.Length) TempBVHArray = new BVHNode8DataCompressed[TLASBVH8.BVH8Nodes.Length];
                CommonFunctions.Aggregate(ref TempBVHArray, ref TLASBVH8.BVH8Nodes);

                CWBVHIndicesBufferInverted = new int[TLASBVH8.cwbvh_indices.Length];
                int CWBVHIndicesBufferCount = CWBVHIndicesBufferInverted.Length;
                for (int i = 0; i < CWBVHIndicesBufferCount; i++) CWBVHIndicesBufferInverted[TLASBVH8.cwbvh_indices[i]] = i;
                NodePair = new List<NodeIndexPairData>();
                NodePair.Add(new NodeIndexPairData());
                DocumentNodes(0, 0, 1, 0, false, 0);
                TempRecur++;
                int NodeCount = NodePair.Count;
                ForwardStack = new Layer[NodeCount];
                for (int i = 0; i < NodePair.Count; i++) {
                    for (int i2 = 0; i2 < 8; i2++) {
                        ForwardStack[i].Children[i2] = -1;
                        ForwardStack[i].Leaf[i2] = -1;
                    }
                }

                for (int i = 0; i < NodePair.Count; i++) {
                    if (NodePair[i].IsLeaf == 1) {
                        int first_triangle = (byte)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] & 0b11111;
                        int NumBits = CommonFunctions.NumberOfSetBits((byte)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] >> 5);
                        ForwardStack[i].Children[NodePair[i].InNodeOffset] = NumBits;
                        ForwardStack[i].Leaf[NodePair[i].InNodeOffset] = (int)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].base_index_triangle + first_triangle + 1;
                    }
                    else {
                        ForwardStack[i].Children[NodePair[i].InNodeOffset] = i;
                        ForwardStack[i].Leaf[NodePair[i].InNodeOffset] = 0;
                    }
                    ForwardStack[NodePair[i].PreviousNode].Children[NodePair[i].InNodeOffset] = i;
                    ForwardStack[NodePair[i].PreviousNode].Leaf[NodePair[i].InNodeOffset] = 0;
                }
                LayerStack = new Layer2[TempRecur];
                for (int i = 0; i < TempRecur; i++) {
                    Layer2 TempSlab = new Layer2();
                    TempSlab.Slab = new List<int>();
                    LayerStack[i] = TempSlab;
                }
                for (int i = 0; i < NodePair.Count; i++) {
                    var TempLayer = LayerStack[NodePair[i].RecursionCount];
                    TempLayer.Slab.Add(i);
                    LayerStack[NodePair[i].RecursionCount] = TempLayer;
                }
                CommonFunctions.ConvertToSplitNodes(TLASBVH8, ref SplitNodes);
                int MaxLength = 0;
                List<Layer2> TempStack = new List<Layer2>();
                for (int i = 0; i < LayerStack.Length; i++) 
                {  
                    if(LayerStack[i].Slab.Count != 0) {
                        TempStack.Add(LayerStack[i]);
                    }
                }
                TempRecur = TempStack.Count;
                LayerStack = TempStack.ToArray();
            return;
        }
        ComputeBuffer BoxesBuffer;
        int CurFrame = 0;
        int BVHNodeCount = 0;
        public unsafe void RefitTLAS(AABB[] Boxes, CommandBuffer cmd, AABB[] Boxes2)
        {
            #if !HardwareRT
            if(TLASTask == null) {
                TLASTask = Task.Run(() => CorrectRefit(Boxes2));
            }
            CurFrame++;
             if(TLASTask.Status == TaskStatus.RanToCompletion && CurFrame % 25 == 24) {
                MaxRecur = TempRecur; 
                List<MyMeshDataCompacted> TempCompressed = new List<MyMeshDataCompacted>(MyMeshesCompacted);
                MyMeshesCompacted.Clear();
                int MaxVal = 0;
                for(int i = 0; i < TLASBVH8.cwbvh_indices.Length; ++i) {
                    MyMeshesCompacted.Add(TempCompressed[((TLASBVH8.cwbvh_indices[i] >= RenderQue.Count) ? InstanceRenderQue[TLASBVH8.cwbvh_indices[i] - RenderQue.Count].CompactedMeshData : RenderQue[TLASBVH8.cwbvh_indices[i]].CompactedMeshData)]);
                    MaxVal = Mathf.Max(TLASBVH8.cwbvh_indices[i], MaxVal);
                }
                for(int i = MaxVal + 1; i < TempCompressed.Count; i++) {
                    MyMeshesCompacted.Add(TempCompressed[RenderQue[i].CompactedMeshData]);
                }
                TempCompressed.Clear();
                for (int i = 0; i < TLASBVH8.cwbvh_indices.Length; i++) {
                    if (TLASBVH8.cwbvh_indices[i] >= RenderQue.Count) {
                        InstanceRenderQue[TLASBVH8.cwbvh_indices[i] - RenderQue.Count].CompactedMeshData = i;
                    }
                    else RenderQue[TLASBVH8.cwbvh_indices[i]].CompactedMeshData = i;
                }

                if(WorkingBuffer != null) for(int i = 0; i < WorkingBuffer.Length; i++) WorkingBuffer[i]?.Release();
                if (NodeBuffer != null) NodeBuffer.Release();
                if (StackBuffer != null) StackBuffer.Release();
                if (ToBVHIndexBuffer != null) ToBVHIndexBuffer.Release();
                if (BVHDataBuffer != null) BVHDataBuffer.Release();
                if (BVHBuffer != null) BVHBuffer.Release();
                if (BoxesBuffer != null) BoxesBuffer.Release();
                WorkingBuffer = new ComputeBuffer[LayerStack.Length];
                for (int i = 0; i < MaxRecur; i++)
                {
                    WorkingBuffer[i] = new ComputeBuffer(LayerStack[i].Slab.Count, 4);
                    WorkingBuffer[i].SetData(LayerStack[i].Slab);
                }
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
                 for (int i = 0; i < RenderQue.Count; i++)
                {
                    ParentObject TargetParent = RenderQue[i];
                    MeshAABBs[TargetParent.CompactedMeshData] = TargetParent.aabb;
                }
                Boxes = MeshAABBs;
                #if !HardwareRT
                    BVH8AggregatedBuffer.SetData(TempBVHArray, 0, 0, TempBVHArray.Length);
                #endif
                    BVHNodeCount = TLASBVH8.BVH8Nodes.Length;
                TLASTask = Task.Run(() => CorrectRefit(Boxes2));
                // Debug.Log("EEE");
            }
                cmd.SetComputeIntParam(Refitter, "NodeCount", NodeBuffer.count);
                cmd.SetComputeBufferParam(Refitter, NodeInitializerKernel, "AllNodes", NodeBuffer);

                cmd.DispatchCompute(Refitter, NodeInitializerKernel, (int)Mathf.Ceil(NodeBuffer.count / (float)256), 1, 1);

                BoxesBuffer.SetData(Boxes);
                cmd.SetComputeBufferParam(Refitter, RefitLayer, "Boxs", BoxesBuffer);
                cmd.SetComputeBufferParam(Refitter, RefitLayer, "ReverseStack", StackBuffer);
                cmd.SetComputeBufferParam(Refitter, RefitLayer, "AllNodes", NodeBuffer);
                cmd.SetComputeBufferParam(Refitter, NodeInitializerKernel, "AllNodes", NodeBuffer);
                for (int i = MaxRecur - 1; i >= 0; i--)
                {
                    var NodeCount2 = WorkingBuffer[i].count;
                    cmd.SetComputeIntParam(Refitter, "NodeCount", NodeCount2);
                    cmd.SetComputeBufferParam(Refitter, RefitLayer, "NodesToWork", WorkingBuffer[i]);
                    cmd.DispatchCompute(Refitter, RefitLayer, (int)Mathf.Ceil(WorkingBuffer[i].count / (float)256), 1, 1);
                }
                cmd.SetComputeIntParam(Refitter, "NodeCount", NodeBuffer.count);
                cmd.SetComputeBufferParam(Refitter, NodeUpdater, "AllNodes", NodeBuffer);
                cmd.SetComputeBufferParam(Refitter, NodeUpdater, "BVHNodes", BVHDataBuffer);
                cmd.SetComputeBufferParam(Refitter, NodeUpdater, "ToBVHIndex", ToBVHIndexBuffer);
                cmd.DispatchCompute(Refitter, NodeUpdater, (int)Mathf.Ceil(NodeBuffer.count / (float)256), 1, 1);

                cmd.SetComputeIntParam(Refitter, "NodeCount", BVHNodeCount);
                cmd.SetComputeIntParam(Refitter, "NodeOffset", 0);
                cmd.SetComputeBufferParam(Refitter, NodeCompress, "BVHNodes", BVHDataBuffer);
                try {
                    cmd.SetComputeBufferParam(Refitter, NodeCompress, "AggNodes", BVH8AggregatedBuffer);
                } catch(System.Exception E) {
                }
                cmd.DispatchCompute(Refitter, NodeCompress, (int)Mathf.Ceil(NodeBuffer.count / (float)256), 1, 1);
            #endif
        }

        int PreVoxelMeshCount;

        private bool ChangedLastFrame = true;


        public bool UpdateTLAS(CommandBuffer cmd)
        {  //Allows for objects to be moved in the scene or animated while playing 

            bool LightsHaveUpdated = false;
            AccumulateData(cmd);

            float CDF = 0;//(LightMeshes.Count != 0) ? LightMeshes[LightMeshes.Count - 1].CDF : 0.0f;
            if (!didstart || PrevLightCount != RayTracingMaster._rayTracingLights.Count || UnityLights.Count == 0)
            {
                UnityLights.Clear();
                UnityLightCount = 0;
                foreach (RayTracingLights RayLight in RayTracingMaster._rayTracingLights)
                {
                    UnityLightCount++;
                    RayLight.UpdateLight();
                    CDF += RayLight.Energy * LightEnergyScale;
                    if (RayLight.Type == 1) SunDirection = RayLight.Direction;
                    RayLight.ArrayIndex = UnityLights.Count;
                    UnityLights.Add(new LightData()
                    {
                        Radiance = RayLight.Emission * LightEnergyScale,
                        Position = RayLight.Position,
                        Direction = RayLight.Direction,
                        energy = RayLight.Energy * LightEnergyScale,
                        CDF = CDF,
                        Type = RayLight.Type,
                        SpotAngle = RayLight.SpotAngle,
                        ZAxisRotation = RayLight.ZAxisRotation
                    });
                }
                if (UnityLights.Count == 0) { UnityLights.Add(new LightData() { }); }
                if (PrevLightCount != RayTracingMaster._rayTracingLights.Count) LightsHaveUpdated = true;
                PrevLightCount = RayTracingMaster._rayTracingLights.Count;
            }
            else
            {
                int LightCount = RayTracingMaster._rayTracingLights.Count;
                LightData UnityLight;
                RayTracingLights RayLight;
                for (int i = 0; i < LightCount; i++)
                {
                    RayLight = RayTracingMaster._rayTracingLights[i];
                    CDF += RayLight.Energy * LightEnergyScale;
                    if (RayLight.Type == 1) SunDirection = RayLight.Direction;
                    RayLight.UpdateLight();
                    UnityLight = UnityLights[RayLight.ArrayIndex];
                    UnityLight.Radiance = RayLight.Emission * LightEnergyScale;
                    UnityLight.Position = RayLight.Position;
                    UnityLight.Direction = RayLight.Direction;
                    UnityLight.energy = RayLight.Energy * LightEnergyScale;
                    UnityLight.CDF = CDF;
                    UnityLight.Type = RayLight.Type;
                    UnityLight.SpotAngle = RayLight.SpotAngle;
                    UnityLight.ZAxisRotation = RayLight.ZAxisRotation;
                    UnityLights[RayLight.ArrayIndex] = UnityLight;
                }
            }

            int MatOffset = 0;
            int MeshDataCount = RenderQue.Count;
            int aggregated_bvh_node_count = 2 * (MeshDataCount + InstanceRenderQue.Count);
            int AggNodeCount = aggregated_bvh_node_count;
            int AggTriCount = 0;
            if (ChildrenUpdated || !didstart || OnlyInstanceUpdated || MyMeshesCompacted.Count == 0)
            {
                MyMeshesCompacted.Clear();
                AggData[] Aggs = new AggData[InstanceData.RenderQue.Count];
                for (int i = 0; i < MeshDataCount; i++)
                {
                    RenderQue[i].UpdateAABB(RenderQue[i].transform);
                    MyMeshesCompacted.Add(new MyMeshDataCompacted()
                    {
                        mesh_data_bvh_offsets = aggregated_bvh_node_count,
                        Transform = RenderQue[i].transform.worldToLocalMatrix,
                        Inverse = RenderQue[i].transform.localToWorldMatrix,
                        AggIndexCount = AggTriCount,
                        AggNodeCount = AggNodeCount,
                        MaterialOffset = MatOffset,
                        IsVoxel = 0,
                        SizeX = 0,
                        SizeY = 0,
                        SizeZ = 0,
                        LightTriCount = RenderQue[i].LightTriangles.Count,
                        LightPDF = RenderQue[i].TotEnergy
                    });
                    RenderQue[i].CompactedMeshData = MyMeshesCompacted.Count - 1;
                    MatOffset += RenderQue[i].MatOffset;
                    MeshAABBs[i] = RenderQue[i].aabb;
                    UnsortedAABBs[i] = RenderQue[i].aabb;
                    AggNodeCount += RenderQue[i].AggBVHNodeCount;//Can I replace this with just using aggregated_bvh_node_count below?
                    AggTriCount += RenderQue[i].AggIndexCount;
                    #if !HardwareRT
                        aggregated_bvh_node_count += RenderQue[i].BVH.cwbvhnode_count;
                    #endif
                }
                for (int i = 0; i < InstanceData.RenderQue.Count; i++)
                {
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
                for (int i = 0; i < InstanceRenderQue.Count; i++)
                {
                    int Index = InstanceData.RenderQue.IndexOf(InstanceRenderQue[i].InstanceParent);
                    MyMeshesCompacted.Add(new MyMeshDataCompacted()
                    {
                        mesh_data_bvh_offsets = Aggs[Index].mesh_data_bvh_offsets,
                        Transform = InstanceRenderQue[i].transform.worldToLocalMatrix,
                        Inverse = InstanceRenderQue[i].transform.localToWorldMatrix,
                        AggIndexCount = Aggs[Index].AggIndexCount,
                        AggNodeCount = Aggs[Index].AggNodeCount,
                        MaterialOffset = Aggs[Index].MaterialOffset,
                        IsVoxel = 0,
                        SizeX = 0,
                        SizeY = 0,
                        SizeZ = 0,
                        LightTriCount = Aggs[Index].LightTriCount
                    });
                    InstanceRenderQue[i].CompactedMeshData = MyMeshesCompacted.Count - 1;
                    AABB aabb = InstanceRenderQue[i].InstanceParent.aabb_untransformed;
                    CreateAABB(InstanceRenderQue[i].transform, ref aabb);
                    MeshAABBs[RenderQue.Count + i] = aabb;
                    UnsortedAABBs[RenderQue.Count + i] = aabb;
                }
                PreVoxelMeshCount = MyMeshesCompacted.Count;
                List<MyMeshDataCompacted> TempMeshCompacted = new List<MyMeshDataCompacted>();
                for (int i = 0; i < VoxelRenderQue.Count; i++)
                {
                    TempMeshCompacted.Add(new MyMeshDataCompacted()
                    {
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
                if (VoxelRenderQue.Count != 0)
                {
                    BVH2Builder BVH2 = new BVH2Builder(VoxelAABBs);
                    BVH8Builder TLASBVH8 = new BVH8Builder(BVH2, ref TempMeshCompacted);
                    MyMeshesCompacted.AddRange(TempMeshCompacted);
                    if (VoxelTLAS == null || VoxelTLAS.Length != TLASBVH8.BVH8Nodes.Length) VoxelTLAS = new BVHNode8DataCompressed[TLASBVH8.BVH8Nodes.Length];
                    CommonFunctions.Aggregate(ref VoxelTLAS, ref TLASBVH8.BVH8Nodes);
                }
                else
                {
                    if (VoxelTLAS == null || VoxelTLAS.Length != 1) VoxelTLAS = new BVHNode8DataCompressed[1];
                }


                Debug.Log("Total Object Count: " + MeshAABBs.Length);
                UpdateMaterials();
                OGBackupMeshesCompacted = new List<MyMeshDataCompacted>(MyMeshesCompacted);
            }
            else
            {
                UnityEngine.Profiling.Profiler.BeginSample("Refit TLAS");
                UnityEngine.Profiling.Profiler.BeginSample("Update Transforms");
                Transform transform;
                ParentObject TargetParent;
                int ClosedCount = 0;
                MyMeshDataCompacted TempMesh2;
                for (int i = 0; i < MeshDataCount; i++)
                {
                    TargetParent = RenderQue[i];
                    transform = TargetParent.ThisTransform;
                    // if (transform.hasChanged || TargetParent.IsSkinnedGroup)
                    // {
                        if (!TargetParent.IsSkinnedGroup) TargetParent.UpdateAABB(transform);
                        TempMesh2 = MyMeshesCompacted[TargetParent.CompactedMeshData];
                        TempMesh2.Transform = transform.worldToLocalMatrix;
                        TempMesh2.Inverse = transform.localToWorldMatrix;
                        MyMeshesCompacted[TargetParent.CompactedMeshData] = TempMesh2;
                        transform.hasChanged = false;
                        OGBackupMeshesCompacted[i] = MyMeshesCompacted[TargetParent.CompactedMeshData];
                    // }
                    MeshAABBs[TargetParent.CompactedMeshData] = TargetParent.aabb;
                    UnsortedAABBs[i] = TargetParent.aabb;
                }
                if(MeshDataCount != 1 && ClosedCount == MeshDataCount - 1) return false;
                UnityEngine.Profiling.Profiler.EndSample();

                for (int i = 0; i < InstanceRenderQue.Count; i++)
                {
                    transform = InstanceRenderQue[i].transform;
                    if (transform.hasChanged || ChangedLastFrame)
                    {
                        MyMeshDataCompacted TempMesh = MyMeshesCompacted[InstanceRenderQue[i].CompactedMeshData];
                        TempMesh.Transform = transform.worldToLocalMatrix;
                        TempMesh.Inverse = transform.localToWorldMatrix;
                        MyMeshesCompacted[InstanceRenderQue[i].CompactedMeshData] = TempMesh;
                        transform.hasChanged = false;
                        AABB aabb = InstanceRenderQue[i].InstanceParent.aabb_untransformed;
                        CreateAABB(transform, ref aabb);
                        OGBackupMeshesCompacted[i + RenderQue.Count] = MyMeshesCompacted[InstanceRenderQue[i].CompactedMeshData];
                        MeshAABBs[InstanceRenderQue[i].CompactedMeshData] = aabb;
                    }
                }
                for (int i = 0; i < InstanceData.RenderQue.Count; i++)
                {
                    MatOffset += InstanceData.RenderQue[i]._Materials.Count;
                }
                AggNodeCount = 0;
                for (int i = 0; i < VoxelRenderQue.Count; i++)
                {
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
                if (VoxelRenderQue.Count != 0)
                {
                    BVH2Builder BVH22 = new BVH2Builder(VoxelAABBs);
                    List<MyMeshDataCompacted> TempMeshCompacted = new List<MyMeshDataCompacted>();
                    for (int i = PreVoxelMeshCount; i < MyMeshesCompacted.Count; i++)
                    {
                        TempMeshCompacted.Add(MyMeshesCompacted[i]);
                    }
                    BVH8Builder TLASBVH82 = new BVH8Builder(BVH22, ref TempMeshCompacted);
                    for (int i = PreVoxelMeshCount; i < MyMeshesCompacted.Count; i++)
                    {
                        MyMeshesCompacted[i] = TempMeshCompacted[i - PreVoxelMeshCount];
                    }
                    if (VoxelTLAS == null || VoxelTLAS.Length != TLASBVH82.BVH8Nodes.Length) VoxelTLAS = new BVHNode8DataCompressed[TLASBVH82.BVH8Nodes.Length];
                    CommonFunctions.Aggregate(ref VoxelTLAS, ref TLASBVH82.BVH8Nodes);
                }
                else
                {
                    if (VoxelTLAS == null || VoxelTLAS.Length != 1) VoxelTLAS = new BVHNode8DataCompressed[1];
                }
                UnityEngine.Profiling.Profiler.EndSample();
                #if HardwareRT
                    AccelStruct.Build();
                #endif
            }
            LightMeshData CurLightMesh;
            for (int i = 0; i < LightMeshCount; i++)
            {
                CurLightMesh = LightMeshes[i];
                CurLightMesh.Inverse = LightTransforms[i].localToWorldMatrix;
                CurLightMesh.Center = LightTransforms[i].position;
                CurLightMesh.OrigionalMesh = RenderQue[CurLightMesh.LockedMeshIndex].CompactedMeshData;
                LightMeshes[i] = CurLightMesh;
            }
            if (ChildrenUpdated || !didstart || OnlyInstanceUpdated)
            {
                ConstructNewTLAS();
                #if !HardwareRT
                    BVH8AggregatedBuffer.SetData(TempBVHArray, 0, 0, TempBVHArray.Length);
                #endif
            }
            else
            {
                UnityEngine.Profiling.Profiler.BeginSample("TLAS Refitting");
                RefitTLAS(MeshAABBs, cmd, UnsortedAABBs);
                UnityEngine.Profiling.Profiler.EndSample();
            }
            if (!didstart) didstart = true;

            VoxOffset = PreVoxelMeshCount;
            AggNodeCount = 0;
            if (VoxelRenderQue.Count != 0)
                UseVoxels = true;
            else
                UseVoxels = false;

            ChangedLastFrame = ChildrenUpdated;
            return (LightsHaveUpdated || ChildrenUpdated);//The issue is that all light triangle indices start at 0, and thus might not get correctly sorted for indices
        }

        public void UpdateMaterials()
        {//Allows for live updating of material properties of any object
            int ParentCount = RenderQue.Count;
            RayTracingObject CurrentMaterial;
            MaterialData TempMat;
            int ChangedMaterialCount = MaterialsChanged.Count;
            int MatCount = _Materials.Count;
            for (int i = 0; i < ChangedMaterialCount; i++)
            {
                CurrentMaterial = MaterialsChanged[i];
                int MaterialCount = (int)Mathf.Min(CurrentMaterial.MaterialIndex.Length, CurrentMaterial.Indexes.Length);
                for (int i3 = 0; i3 < MaterialCount; i3++)
                {
                    int Index = CurrentMaterial.Indexes[i3];
                    if(CurrentMaterial.MaterialIndex[i3] >= MatCount) continue;
                    TempMat = _Materials[CurrentMaterial.MaterialIndex[i3]];
                    TempMat.BaseColor = CurrentMaterial.BaseColor[Index];
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
                    TempMat.relativeIOR = CurrentMaterial.IOR[Index];
                    TempMat.Thin = CurrentMaterial.Thin[Index];
                    TempMat.clearcoatGloss = CurrentMaterial.ClearCoatGloss[Index];
                    TempMat.specTrans = CurrentMaterial.SpecTrans[Index];
                    TempMat.anisotropic = CurrentMaterial.Anisotropic[Index];
                    TempMat.diffTrans = CurrentMaterial.DiffTrans[Index];
                    TempMat.flatness = CurrentMaterial.Flatness[Index];
                    TempMat.Specular = CurrentMaterial.Specular[Index];
                    TempMat.scatterDistance = CurrentMaterial.ScatterDist[Index];
                    _Materials[CurrentMaterial.MaterialIndex[i3]] = TempMat;
                }
                CurrentMaterial.NeedsToUpdate = false;
            }
            MaterialsChanged.Clear();

            ParentCount = InstanceData.RenderQue.Count;
            for (int i = 0; i < ParentCount; i++)
            {
                int ChildCount = InstanceData.RenderQue[i].ChildObjects.Length;
                for (int i2 = 0; i2 < ChildCount; i2++)
                {
                    CurrentMaterial = InstanceData.RenderQue[i].ChildObjects[i2];
                    if (CurrentMaterial.NeedsToUpdate || !didstart)
                    {
                        int MaterialCount = (int)Mathf.Min(CurrentMaterial.MaterialIndex.Length, CurrentMaterial.Indexes.Length);
                        for (int i3 = 0; i3 < MaterialCount; i3++)
                        {
                            int Index = CurrentMaterial.Indexes[i3];
                            TempMat = _Materials[CurrentMaterial.MaterialIndex[i3]];
                            TempMat.BaseColor = CurrentMaterial.BaseColor[Index];
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
                            TempMat.relativeIOR = CurrentMaterial.IOR[Index];
                            TempMat.Thin = CurrentMaterial.Thin[Index];
                            TempMat.clearcoatGloss = CurrentMaterial.ClearCoatGloss[Index];
                            TempMat.specTrans = CurrentMaterial.SpecTrans[Index];
                            TempMat.anisotropic = CurrentMaterial.Anisotropic[Index];
                            TempMat.diffTrans = CurrentMaterial.DiffTrans[Index];
                            TempMat.flatness = CurrentMaterial.Flatness[Index];
                            TempMat.Specular = CurrentMaterial.Specular[Index];
                            TempMat.scatterDistance = CurrentMaterial.ScatterDist[Index];
                            _Materials[CurrentMaterial.MaterialIndex[i3]] = TempMat;
                        }
                    }
                }
            }
        }
    }
}