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
        [HideInInspector] public float LightEnergyScale = 1.0f;
        //emissive, alpha, metallic, roughness
        [System.NonSerialized] public Texture2D AlbedoAtlas;
        [System.NonSerialized] public RenderTexture HeightmapAtlas;
        [System.NonSerialized] public RenderTexture AlphaMapAtlas;
        [System.NonSerialized] public RenderTexture NormalAtlas;
        [System.NonSerialized] public RenderTexture EmissiveAtlas;
        [System.NonSerialized] public RenderTexture MetallicAtlas;
        [System.NonSerialized] public RenderTexture RoughnessAtlas;
        private RenderTexture AlphaAtlas;
        private RenderTexture TempTex;
        private RenderTexture s_Prop_EncodeBCn_Temp;
        private ComputeShader CopyShader;
        private ComputeShader Refitter;
        private int RefitLayer;
        private int NodeUpdater;
        private int NodeCompress;
        private int NodeInitializerKernel;

        [HideInInspector] public int AlbedoAtlasSize;

        [HideInInspector] public VideoObject VideoPlayerObject;
        [HideInInspector] public RenderTexture VideoTexture;

        [HideInInspector] public List<RayTracingObject> MaterialsChanged;
        [HideInInspector] public MaterialData[] _Materials;
        [HideInInspector] public ComputeBuffer BVH8AggregatedBuffer;
        [HideInInspector] public ComputeBuffer AggTriBuffer;
        [HideInInspector] public ComputeBuffer LightTriBuffer;
        [HideInInspector] public List<MyMeshDataCompacted> MyMeshesCompacted;
        [HideInInspector] public List<LightData> UnityLights;
        [HideInInspector] public InstancedManager InstanceData;
        [HideInInspector] public List<InstancedObject> Instances;
        [HideInInspector] public ComputeBuffer TLASCWBVHIndexes;

        [HideInInspector] public List<TerrainObject> Terrains;
        [HideInInspector] public List<TerrainDat> TerrainInfos;
        [HideInInspector] public ComputeBuffer TerrainBuffer;
        [HideInInspector] public bool DoHeightmap;

        private ComputeShader MeshFunctions;
        private int TriangleBufferKernel;
        private int NodeBufferKernel;
        private int LightBufferKernel;

        [HideInInspector] public List<ParentObject> RenderQue;
        [HideInInspector] public List<Transform> RenderTransforms;
        [HideInInspector] public List<ParentObject> BuildQue;
        [HideInInspector] public List<ParentObject> AddQue;
        [HideInInspector] public List<ParentObject> RemoveQue;
        [HideInInspector] public List<ParentObject> UpdateQue;

        [HideInInspector] public List<InstancedObject> InstanceRenderQue;
        [HideInInspector] public List<InstancedObject> InstanceUpdateQue;
        [HideInInspector] public List<Transform> InstanceRenderTransforms;
        [HideInInspector] public List<InstancedObject> InstanceBuildQue;
        [HideInInspector] public List<InstancedObject> InstanceAddQue;
        [HideInInspector] public List<InstancedObject> InstanceRemoveQue;

        private bool OnlyInstanceUpdated;
        [HideInInspector] public List<Transform> LightTransforms;
        [HideInInspector] public int MainDesiredRes = 16384;

        [HideInInspector] public int MatCount;

        [HideInInspector] public List<LightMeshData> LightMeshes;

        [HideInInspector] public AABB[] MeshAABBs;

        [HideInInspector] public bool ParentCountHasChanged;

        [HideInInspector] public int LightMeshCount;
        [HideInInspector] public int UnityLightCount;

        private int PrevLightCount;
        [HideInInspector] public BVH2Builder BVH2;

        [HideInInspector] public bool UseSkinning = true;
        [HideInInspector] public bool HasStart = false;
        [HideInInspector] public bool didstart = false;
        [HideInInspector] public bool ChildrenUpdated;

        [HideInInspector] public Vector3 SunDirection;

        private BVHNode8DataCompressed[] TempBVHArray;
        #if TTLightMapping
            public List<int> LightMapTexIndex;
            public LightMapData[] LightMaps;
        #endif

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
            CommonFunctions.DeepClean(ref _Materials);
            CommonFunctions.DeepClean(ref LightTransforms);
            CommonFunctions.DeepClean(ref LightMeshes);
            CommonFunctions.DeepClean(ref MyMeshesCompacted);
            CommonFunctions.DeepClean(ref UnityLights);

            DestroyImmediate(AlbedoAtlas);
            DestroyImmediate(NormalAtlas);
            DestroyImmediate(EmissiveAtlas);
            DestroyImmediate(MetallicAtlas);
            DestroyImmediate(RoughnessAtlas);
            DestroyImmediate(HeightmapAtlas);
            DestroyImmediate(AlphaMapAtlas);

            LightTriBuffer.ReleaseSafe();
            BVH8AggregatedBuffer.ReleaseSafe();
            AggTriBuffer.ReleaseSafe();

            TLASCWBVHIndexes.ReleaseSafe();

            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        public static AssetManager Assets;

    private void PackAndCompact(Dictionary<int, TexObj> DictTex, ref RenderTexture Atlas, ref RenderTexture AlphaAtlas, PackingRectangle[] Rects, int DesiredRes, int TexIndex, int ReadIndex = -1, List<int> TexChannelIndex = null) {
        Vector2 Scale = new Vector2(1,1);
        int TexCount = DictTex.Count;
        if(TexCount != 0) {
            PackingRectangle BoundRects;
            RectanglePacker.Pack(Rects, out BoundRects);
            if(TexIndex < 3) DesiredRes = (int)Mathf.Min(Mathf.Max(BoundRects.Width, BoundRects.Height), DesiredRes);
            Scale = new Vector2(Mathf.Min((float)DesiredRes / BoundRects.Width, 1), Mathf.Min((float)DesiredRes / BoundRects.Height, 1));
        } else {
            if(TexIndex != 6 && TexIndex != 7) DesiredRes = 1;
        }

        switch(TexIndex) {
            case 0://heightmap
                CreateRenderTexture(ref Atlas, DesiredRes, DesiredRes, RenderTextureFormat.RHalf, true);
            break;
            case 1://alphamap
                CreateRenderTexture(ref Atlas, DesiredRes, DesiredRes, RenderTextureFormat.ARGBHalf, true);
            break;
            case 2://normalmap
                CreateRenderTexture(ref Atlas, DesiredRes, DesiredRes, RenderTextureFormat.RG16, true);
            break;
            case 3://metallicmap
            case 4://roughnessmap
                CreateRenderTexture(ref Atlas, DesiredRes, DesiredRes, RenderTextureFormat.R8, true);
            break;
            case 5://EmissionMap
                CreateRenderTexture(ref Atlas, DesiredRes, DesiredRes, RenderTextureFormat.ARGB32, false);
            break;
            case 6://AlbedoMap
                CreateRenderTexture(ref Atlas, DesiredRes, DesiredRes, RenderTextureFormat.ARGB32, false);
                CreateRenderTexture(ref AlphaAtlas, DesiredRes, DesiredRes, RenderTextureFormat.R8, true);
            break;
            case 7://albedoalpha
                CreateRenderTexture(ref Atlas, DesiredRes, DesiredRes, RenderTextureFormat.ARGB32, false);
                CreateRenderTexture(ref AlphaAtlas, DesiredRes, DesiredRes, RenderTextureFormat.R8, true);
            break;
        }
        if(TexCount == 0) return;


        CopyShader.SetBool("IsNormalMap", TexIndex == 2);
        CopyShader.SetVector("Scale", Scale);
        CopyShader.SetVector("OutputSize", new Vector2(Atlas.width, Atlas.height));
        CopyShader.SetBool("IsHeightmap", TexIndex == 0);
        for (int i = 0; i < TexCount; i++) {
                PackingRectangle TempRect = Rects[i];
                int ID = TempRect.Id;
                CopyShader.SetVector("InputSize", new Vector2(TempRect.Width, TempRect.Height));
                CopyShader.SetVector("Offset", new Vector2(TempRect.X, TempRect.Y));
                TexObj SelectedTex = DictTex[ID];
                int ListLength = SelectedTex.TexObjList.Count;
                MaterialData MatDat;
                Vector4 RectSelect = new Vector4(0, 0, Mathf.Floor(TempRect.X * Scale.x) / Atlas.width, Mathf.Floor(TempRect.Y * Scale.y) / Atlas.height);
                RectSelect.x = RectSelect.z + Mathf.Floor((TempRect.Width - 1) * Scale.x) / Atlas.width;
                RectSelect.y = RectSelect.w + Mathf.Floor((TempRect.Height - 1) * Scale.y) / Atlas.height;

                if(TexIndex >= 2 && TexIndex < 7) {
                    for(int j = 0; j < ListLength; j++) {
                        // MatDat = _Materials[SelectedTex.TexObjList[j]];
                        switch (TexIndex) {
                            case 2: _Materials[SelectedTex.TexObjList[j]].NormalTex = RectSelect; break;
                            case 3: _Materials[SelectedTex.TexObjList[j]].MetallicTex = RectSelect; break;
                            case 4: _Materials[SelectedTex.TexObjList[j]].RoughnessTex = RectSelect; break;
                            case 5: _Materials[SelectedTex.TexObjList[j]].EmissiveTex = RectSelect; break;
                            case 6: _Materials[SelectedTex.TexObjList[j]].AlbedoTex = RectSelect; break;
                            default: break;
                        }
                        // _Materials[SelectedTex.TexObjList[j]] = MatDat;
                    }
                } else if(TexIndex < 2) {
                    TerrainDat TempTerrain = TerrainInfos[SelectedTex.TexObjList[0]];
                    if(TexIndex == 0) TempTerrain.HeightMap = RectSelect;
                    else TempTerrain.AlphaMap = RectSelect;
                    TerrainInfos[SelectedTex.TexObjList[0]] = TempTerrain;
                }
                switch(TexIndex) {
                    case 0:
                        CopyShader.SetInt("OutputRead", 0);
                        CopyShader.SetTexture(2, "AdditionTex", SelectedTex.Tex);
                        CopyShader.SetTexture(2, "ResultSingle", Atlas);
                        CopyShader.Dispatch(2, (int)Mathf.CeilToInt(TempRect.Width * Scale.x / 32.0f), (int)Mathf.CeilToInt(TempRect.Height * Scale.y / 32.0f), 1);
                    break;
                    case 1:
                        CopyShader.SetTexture(5, "_Source2", SelectedTex.Tex);
                        CopyShader.SetTexture(5, "_Target2", Atlas);
                        CopyShader.Dispatch(5, (int)Mathf.CeilToInt(TempRect.Width * Scale.x / 32.0f), (int)Mathf.CeilToInt(TempRect.Height * Scale.y / 32.0f), 1);
                    break;
                    case 2:
                        CopyShader.SetTexture(3, "AdditionTex", SelectedTex.Tex);
                        CopyShader.SetTexture(3, "ResultDouble", Atlas);
                        CopyShader.Dispatch(3, (int)Mathf.CeilToInt(TempRect.Width * Scale.x / 32.0f), (int)Mathf.CeilToInt(TempRect.Height * Scale.y / 32.0f), 1);
                    break;                    
                    case 3:
                    case 4:
                    case 7:
                        CopyShader.SetInt("OutputRead", ((TexChannelIndex == null) ? ReadIndex : SelectedTex.ReadIndex));
                        CopyShader.SetTexture(2, "AdditionTex", SelectedTex.Tex);
                        CopyShader.SetTexture(2, "ResultSingle", Atlas);
                        CopyShader.Dispatch(2, (int)Mathf.CeilToInt(TempRect.Width * Scale.x / 32.0f), (int)Mathf.CeilToInt(TempRect.Height * Scale.y / 32.0f), 1);
                    break;
                    case 5:
                    case 6:
                        CopyShader.SetTexture(0, "AdditionTex", SelectedTex.Tex);
                        CopyShader.SetTexture(0, "Result", Atlas);
                        CopyShader.Dispatch(0, (int)Mathf.CeilToInt(TempRect.Width * Scale.x / 32.0f), (int)Mathf.CeilToInt(TempRect.Height * Scale.y / 32.0f), 1);
                        CopyShader.SetInt("OutputRead", ((TexChannelIndex == null) ? ReadIndex : SelectedTex.ReadIndex));
                        CopyShader.SetTexture(2, "AdditionTex", SelectedTex.Tex);
                        CopyShader.SetTexture(2, "ResultSingle", AlphaAtlas);
                        CopyShader.Dispatch(2, (int)Mathf.CeilToInt(TempRect.Width * Scale.x / 32.0f), (int)Mathf.CeilToInt(TempRect.Height * Scale.y / 32.0f), 1);
                    break;
                }

            }
        }

        private void CreateRenderTexture(ref RenderTexture ThisTex, int Width, int Height, RenderTextureFormat Form, bool UseMip) {
            ThisTex = new RenderTexture(Width, Height, 0, Form, RenderTextureReadWrite.sRGB);
            ThisTex.enableRandomWrite = true;
            if(UseMip) {
                ThisTex.useMipMap = true;
                ThisTex.autoGenerateMips = false;
            } else ThisTex.useMipMap = false;
            ThisTex.Create();
        }

        private void KeyCheck(int MatIndex, Texture Tex, ref Dictionary<int, TexObj> DictTextures, ref List<PackingRectangle> PR, int ReadChannelIndex) {
            int index = Tex.GetInstanceID();

            if (DictTextures.TryGetValue(index, out var existingTexObj)) {
                existingTexObj.TexObjList.Add(MatIndex);
            } else {
                var newTexObj = new TexObj {
                    Tex = Tex,
                    ReadIndex = ReadChannelIndex
                };

                PR.Add(new PackingRectangle {
                    Width = (uint)Tex.width + 1,
                    Height = (uint)Tex.height + 1,
                    Id = index
                });

                newTexObj.TexObjList.Add(MatIndex);
                DictTextures.Add(index, newTexObj);
            }
        }
        private void CreateAtlas(int TotalMatCount) {//Creates texture atlas
            _Materials = new MaterialData[TotalMatCount];
            Dictionary<int, TexObj> HeightMapTextures = new Dictionary<int, TexObj>();
            Dictionary<int, TexObj> AlphaMapTextures = new Dictionary<int, TexObj>();
            List<PackingRectangle> HeightMapRect = new List<PackingRectangle>();
            List<PackingRectangle> AlphaMapRect = new List<PackingRectangle>();
            Dictionary<int, TexObj> AlbTextures = new Dictionary<int, TexObj>();
            Dictionary<int, TexObj> NormTextures = new Dictionary<int, TexObj>();
            Dictionary<int, TexObj> EmisTextures = new Dictionary<int, TexObj>();
            Dictionary<int, TexObj> MetTextures = new Dictionary<int, TexObj>();
            Dictionary<int, TexObj> RoughTextures = new Dictionary<int, TexObj>();
            List<PackingRectangle> AlbRect = new List<PackingRectangle>();
            List<PackingRectangle> NormRect = new List<PackingRectangle>();
            List<PackingRectangle> EmisRect = new List<PackingRectangle>();
            List<PackingRectangle> MetRect = new List<PackingRectangle>();
            List<PackingRectangle> RoughRect = new List<PackingRectangle>();
            MatCount = 0;
            int TerrainCount = Terrains.Count;
            TerrainInfos = new List<TerrainDat>();
            DoHeightmap = false;

            if (CopyShader == null) CopyShader = Resources.Load<ComputeShader>("Utility/CopyTextureShader");

            if(RenderQue.Count == 0) return;
            int CurCount = RenderQue[0].AlbedoTexs.Count;
            foreach (ParentObject Obj in RenderQue) {
                foreach (RayTracingObject Obj2 in Obj.ChildObjects) {
                    Obj2.MatOffset = MatCount;
                }
                MaterialsChanged.AddRange(Obj.ChildObjects);
                int ThisMatCount = Obj._Materials.Count;
                for(int i = 0; i < ThisMatCount; i++) {
                    MaterialData TempMat = Obj._Materials[i];
                    if(TempMat.AlbedoTex.w != 0) KeyCheck(MatCount, Obj.AlbedoTexs[(int)TempMat.AlbedoTex.w-1], ref AlbTextures, ref AlbRect, 0);
                    if(TempMat.NormalTex.w != 0) KeyCheck(MatCount, Obj.NormalTexs[(int)TempMat.NormalTex.w-1], ref NormTextures, ref NormRect, 0);
                    if(TempMat.EmissiveTex.w != 0) KeyCheck(MatCount, Obj.EmissionTexs[(int)TempMat.EmissiveTex.w-1], ref EmisTextures, ref EmisRect, 0);
                    if(TempMat.MetallicTex.w != 0) KeyCheck(MatCount, Obj.MetallicTexs[(int)TempMat.MetallicTex.w-1], ref MetTextures, ref MetRect, Obj.MetallicTexChannelIndex[(int)TempMat.MetallicTex.w-1]);
                    if(TempMat.RoughnessTex.w != 0) KeyCheck(MatCount, Obj.RoughnessTexs[(int)TempMat.RoughnessTex.w-1], ref RoughTextures, ref RoughRect, Obj.RoughnessTexChannelIndex[(int)TempMat.RoughnessTex.w-1]);
                    _Materials[MatCount] = TempMat;
                    MatCount++;
                }
            }
            foreach (ParentObject Obj in InstanceData.RenderQue) {
                foreach (RayTracingObject Obj2 in Obj.ChildObjects) {
                    Obj2.MatOffset = MatCount;
                }
                MaterialsChanged.AddRange(Obj.ChildObjects);
                int ThisMatCount = Obj._Materials.Count;
                for(int i = 0; i < ThisMatCount; i++) {
                    MaterialData TempMat = Obj._Materials[i];
                    if(TempMat.AlbedoTex.w != 0) KeyCheck(MatCount, Obj.AlbedoTexs[(int)TempMat.AlbedoTex.w-1], ref AlbTextures, ref AlbRect, 0);
                    if(TempMat.NormalTex.w != 0) KeyCheck(MatCount, Obj.NormalTexs[(int)TempMat.NormalTex.w-1], ref NormTextures, ref NormRect, 0);
                    if(TempMat.EmissiveTex.w != 0) KeyCheck(MatCount, Obj.EmissionTexs[(int)TempMat.EmissiveTex.w-1], ref EmisTextures, ref EmisRect, 0);
                    if(TempMat.MetallicTex.w != 0) KeyCheck(MatCount, Obj.MetallicTexs[(int)TempMat.MetallicTex.w-1], ref MetTextures, ref MetRect, Obj.MetallicTexChannelIndex[(int)TempMat.MetallicTex.w-1]);
                    if(TempMat.RoughnessTex.w != 0) KeyCheck(MatCount, Obj.RoughnessTexs[(int)TempMat.RoughnessTex.w-1], ref RoughTextures, ref RoughRect, Obj.RoughnessTexChannelIndex[(int)TempMat.RoughnessTex.w-1]);
                    _Materials[MatCount] = TempMat;
                    MatCount++;
                }
            }
            int TerrainMatCount = MatCount;
            if (TerrainCount != 0) {
                for (int j = 0; j < TerrainCount; j++) {
                    TerrainObject Obj2 = Terrains[j];
                    TerrainDat TempTerrain = new TerrainDat();
                    TempTerrain.PositionOffset = Obj2.transform.position;
                    TempTerrain.TerrainDim = Terrains[j].TerrainDim;
                    TempTerrain.HeightScale = Terrains[j].HeightScale;
                    TempTerrain.MatOffset = TerrainMatCount - MatCount;
                    int Index = Obj2.HeightMap.GetInstanceID();
                    var E = new TexObj();
                    {
                        HeightMapRect.Add(new PackingRectangle() {
                            Width = (uint)Obj2.HeightMap.width + 1,
                            Height = (uint)Obj2.HeightMap.height + 1,
                            Id = Index
                        });
                        E.Tex = Obj2.HeightMap;
                        E.ReadIndex = 0;
                        E.TexObjList.Add(j);
                        HeightMapTextures.Add(Index, E);
                    }
                    E = new TexObj();
                    Index = Obj2.AlphaMap.GetInstanceID();
                    {
                        AlphaMapRect.Add(new PackingRectangle() {
                            Width = (uint)Obj2.AlphaMap.width + 1,
                            Height = (uint)Obj2.AlphaMap.height + 1,
                            Id = Index
                        });
                        E.Tex = Obj2.AlphaMap;
                        E.ReadIndex = 0;
                        E.TexObjList.Add(j);
                        AlphaMapTextures.Add(Index, E);
                    }


                    int ThisMatCount = Obj2.Materials.Count;
                    for (int i = 0; i < ThisMatCount; i++) {
                        MaterialData TempMat = Obj2.Materials[i];
                        if(TempMat.AlbedoTex.w != 0) KeyCheck(TerrainMatCount + i, Obj2.AlbedoTexs[(int)TempMat.AlbedoTex.w-1], ref AlbTextures, ref AlbRect, 0);
                        if(TempMat.NormalTex.w != 0) KeyCheck(TerrainMatCount + i, Obj2.NormalTexs[(int)TempMat.NormalTex.w-1], ref NormTextures, ref NormRect, 0);
                        if(TempMat.MetallicTex.w != 0) KeyCheck(TerrainMatCount + i, Obj2.MaskTexs[(int)TempMat.MetallicTex.w-1], ref MetTextures, ref MetRect, 2);
                        if(TempMat.RoughnessTex.w != 0) KeyCheck(TerrainMatCount + i, Obj2.MaskTexs[(int)TempMat.RoughnessTex.w-1], ref RoughTextures, ref RoughRect, 1);

                        _Materials[TerrainMatCount + i] = TempMat;
                    }
                    TerrainMatCount += ThisMatCount;
                    TerrainInfos.Add(TempTerrain);
                }
            }


            if (!RenderQue.Any())
                return;

            if(NormalAtlas != null) NormalAtlas?.Release();
            if(RoughnessAtlas != null) RoughnessAtlas?.Release();
            if(MetallicAtlas != null) MetallicAtlas?.Release();
            if(EmissiveAtlas != null) EmissiveAtlas?.Release();

            if(AlbedoAtlas == null || AlbedoAtlas.width != MainDesiredRes) AlbedoAtlas = new Texture2D(MainDesiredRes,MainDesiredRes, TextureFormat.DXT5, false);
            PackAndCompact(AlbTextures, ref TempTex, ref AlphaAtlas, AlbRect.ToArray(), MainDesiredRes, 6, 3, null);
            int tempWidth = (TempTex.width + 3) / 4;
            int tempHeight = (TempTex.height + 3) / 4;
            var desc = new RenderTextureDescriptor
            {
                width = tempWidth,
                height = tempHeight,
                dimension = TextureDimension.Tex2D,
                enableRandomWrite = true,
                msaaSamples = 1,
                volumeDepth = 1
            };
            desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SInt;

            s_Prop_EncodeBCn_Temp = new RenderTexture(desc);
            CopyShader.SetTexture(4, "_Source", TempTex);
            CopyShader.SetTexture(4, "Alpha", AlphaAtlas);
            CopyShader.SetTexture(4, "_Target", s_Prop_EncodeBCn_Temp);
            CopyShader.Dispatch(4, (int)((tempWidth + 8 - 1) / 8), (int)((tempHeight + 8 - 1) / 8),1);
            Graphics.CopyTexture(s_Prop_EncodeBCn_Temp, 0, AlbedoAtlas, 0);
            TempTex.Release();
            s_Prop_EncodeBCn_Temp.Release();


            PackAndCompact(NormTextures, ref NormalAtlas, ref AlphaAtlas, NormRect.ToArray(), MainDesiredRes, 2);
            PackAndCompact(EmisTextures, ref EmissiveAtlas, ref AlphaAtlas, EmisRect.ToArray(), MainDesiredRes, 5);
            PackAndCompact(MetTextures, ref MetallicAtlas, ref AlphaAtlas, MetRect.ToArray(), AlbedoAtlas.width, 3, 0);
            PackAndCompact(RoughTextures, ref RoughnessAtlas, ref AlphaAtlas, RoughRect.ToArray(), AlbedoAtlas.width, 4, 0);
            PackAndCompact(HeightMapTextures, ref HeightmapAtlas, ref AlphaAtlas, HeightMapRect.ToArray(), 16384, 0);
            PackAndCompact(AlphaMapTextures, ref AlphaMapAtlas, ref AlphaAtlas, AlphaMapRect.ToArray(), 16384, 1);
            AlphaAtlas.Release();
            AlbedoAtlasSize = AlbedoAtlas.width;
            NormalAtlas.anisoLevel = 3;
            NormalAtlas.GenerateMips();
            MetallicAtlas.GenerateMips();
            RoughnessAtlas.GenerateMips();


            AlbTextures.Clear();
            AlbTextures.TrimExcess();
            NormTextures.Clear();
            NormTextures.TrimExcess();
            EmisTextures.Clear();
            EmisTextures.TrimExcess();
            MetTextures.Clear();
            MetTextures.TrimExcess();
            RoughTextures.Clear();
            RoughTextures.TrimExcess();
            HeightMapTextures.Clear();
            HeightMapTextures.TrimExcess();
            AlphaMapTextures.Clear();
            AlphaMapTextures.TrimExcess();

            AlbRect.Clear();
            AlbRect.TrimExcess();
            NormRect.Clear();
            NormRect.TrimExcess();
            EmisRect.Clear();
            EmisRect.TrimExcess();
            MetRect.Clear();
            MetRect.TrimExcess();
            RoughRect.Clear();
            RoughRect.TrimExcess();
            HeightMapRect.Clear();
            HeightMapRect.TrimExcess();
            AlphaMapRect.Clear();
            AlphaMapRect.TrimExcess();
            if (TerrainCount != 0) {
                if (TerrainBuffer != null) TerrainBuffer.Release();
                TerrainBuffer = new ComputeBuffer(TerrainCount, 56);
                TerrainBuffer.SetData(TerrainInfos);
            }

        }
        public void Start() {
            Assets = this;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if(this != null) gameObject.GetComponent<RayTracingMaster>().Start2();
        }

        public void ForceUpdateAtlas() {
            int throwaway = 0;
            // for(int i = 0; i < RenderQue.Count; i++) RenderQue[i].CreateAtlas(ref throwaway);
            // CreateAtlas();
        }

        public void OnApplicationQuit() {
            RunningTasks = 0;
            #if HardwareRT
                AccelStruct.Release();
            #endif

            LightTriBuffer.ReleaseSafe();
            BVH8AggregatedBuffer.ReleaseSafe();
            AggTriBuffer.ReleaseSafe();

            if(WorkingBuffer != null) for(int i = 0; i < WorkingBuffer.Length; i++) WorkingBuffer[i]?.Release();
            NodeBuffer.ReleaseSafe();
            StackBuffer.ReleaseSafe();
            ToBVHIndexBuffer.ReleaseSafe();
            BVHDataBuffer.ReleaseSafe();
            BVHBuffer.ReleaseSafe();
            BoxesBuffer.ReleaseSafe();
            TerrainBuffer.ReleaseSafe();
            TLASCWBVHIndexes.ReleaseSafe();
            TerrainBuffer.ReleaseSafe();
        }

        void OnDisable() {
            ClearAll();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }


        private TextAsset XMLObject;
        [HideInInspector] public static List<string> ShaderNames;
        [HideInInspector] public static Materials data = new Materials();
        [HideInInspector] public bool NeedsToUpdateXML;
        public void AddMaterial(Shader shader) {
            int ShaderPropertyCount = shader.GetPropertyCount();
            string NormalTex, EmissionTex, MetallicTex, RoughnessTex, MetallicRange, RoughnessRange, MetallicRemapMin, MetallicRemapMax, RoughnessRemapMin, RoughnessRemapMax;
            NormalTex = EmissionTex = MetallicTex = RoughnessTex = MetallicRange = RoughnessRange = MetallicRemapMin = MetallicRemapMax = RoughnessRemapMin = RoughnessRemapMax = "null";
            int MetallicIndex, RoughnessIndex;
            MetallicIndex = RoughnessIndex = 0;
            data.Material.Add(new MaterialShader() {
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
                UsesSmoothness = false,
                BaseColorValue = "null",
                MetallicRemapMin = MetallicRemapMin,
                MetallicRemapMax = MetallicRemapMax,
                RoughnessRemapMin = RoughnessRemapMin,
                RoughnessRemapMax = RoughnessRemapMax
            });
            ShaderNames.Add(shader.name);
            NeedsToUpdateXML = true;
        }
        public void UpdateMaterialDefinition() {
            XMLObject = Resources.Load<TextAsset>("Utility/MaterialMappings");
            #if UNITY_EDITOR
                if(XMLObject == null) {
                    Debug.Log("Missing Material Mappings XML");
                    return;
                }
            #endif
            ShaderNames = new List<string>();
            using (var A = new StringReader(XMLObject.text)) {
                var serializer = new XmlSerializer(typeof(Materials));
                data = serializer.Deserialize(A) as Materials;
            }
            foreach (var Mat in data.Material) {
                ShaderNames.Add(Mat.Name);
            }
        }

        private void init() {
            #if HardwareRT
                AccelStruct = new UnityEngine.Rendering.RayTracingAccelerationStructure();
            #endif
            if(AlbedoAtlas == null || AlbedoAtlas.width != MainDesiredRes) AlbedoAtlas = new Texture2D(MainDesiredRes,MainDesiredRes, TextureFormat.DXT5, false);
            UpdateMaterialDefinition();
            UnityEngine.Video.VideoPlayer[] VideoObjects = GameObject.FindObjectsOfType<UnityEngine.Video.VideoPlayer>();
            if (VideoTexture != null) VideoTexture.Release();
            if (VideoObjects.Length == 0) CreateRenderTexture(ref VideoTexture, 1, 1, RenderTextureFormat.ARGB32, false);
            else {
                GameObject VideoAttatchedObject = VideoObjects[0].gameObject;
                VideoPlayerObject = (VideoAttatchedObject.GetComponent<VideoObject>() == null) ? VideoAttatchedObject.AddComponent<VideoObject>() : VideoAttatchedObject.GetComponent<VideoObject>();
                VideoTexture = VideoPlayerObject.VideoTexture;
            }
            Refitter = Resources.Load<ComputeShader>("Utility/BVHRefitter");
            RefitLayer = Refitter.FindKernel("RefitBVHLayer");
            NodeUpdater = Refitter.FindKernel("NodeUpdate");
            NodeCompress = Refitter.FindKernel("NodeCompress");
            NodeInitializerKernel = Refitter.FindKernel("NodeInitializer");

            MaterialsChanged = new List<RayTracingObject>();
            InstanceData = GameObject.Find("InstancedStorage").GetComponent<InstancedManager>();
            Instances = new List<InstancedObject>();
            SunDirection = new Vector3(0, -1, 0);
            {
                AddQue = new List<ParentObject>();
                RemoveQue = new List<ParentObject>();
                RenderQue = new List<ParentObject>();
                RenderTransforms = new List<Transform>();
                UpdateQue = new List<ParentObject>();
                BuildQue = new List<ParentObject>();
            }
            {
                InstanceRenderQue = new List<InstancedObject>();
                InstanceUpdateQue = new List<InstancedObject>();
                InstanceRenderTransforms = new List<Transform>();
                InstanceBuildQue = new List<InstancedObject>();
                InstanceAddQue = new List<InstancedObject>();
                InstanceRemoveQue = new List<InstancedObject>();
            }
            MyMeshesCompacted = new List<MyMeshDataCompacted>();
            UnityLights = new List<LightData>();
            LightMeshes = new List<LightMeshData>();
            LightTransforms = new List<Transform>();
            LightMeshCount = 0;
            UnityLightCount = 0;
            TerrainInfos = new List<TerrainDat>();
            LightTriBuffer.ReleaseSafe();
            BVH8AggregatedBuffer.ReleaseSafe();
            AggTriBuffer.ReleaseSafe();
            MeshFunctions = Resources.Load<ComputeShader>("Utility/GeneralMeshFunctions");
            TriangleBufferKernel = MeshFunctions.FindKernel("CombineTriBuffers");
            NodeBufferKernel = MeshFunctions.FindKernel("CombineNodeBuffers");
            LightBufferKernel = MeshFunctions.FindKernel("CombineLightBuffers");
            if (TerrainBuffer != null) TerrainBuffer.Release();
            TerrainBuffer = new ComputeBuffer(1, 56);

            if (Terrains.Count != 0) for (int i = 0; i < Terrains.Count; i++) Terrains[i].Load();
        }

        private void UpdateRenderAndBuildQues() {
            ChildrenUpdated = false;
            OnlyInstanceUpdated = false;

            int QueCount = RemoveQue.Count;
            {//Main Object Data Handling
             // UnityEngine.Profiling.Profiler.BeginSample("Remove");
                for (int i = QueCount - 1; i >= 0; i--) {
                    switch(RemoveQue[i].ExistsInQue) {
                        case 0: {int Index = RenderQue.IndexOf(RemoveQue[i]); RenderQue.RemoveAt(Index); RenderTransforms.RemoveAt(Index);}; break;
                        case 1: BuildQue.Remove(RemoveQue[i]); break;
                        case 2: UpdateQue.Remove(RemoveQue[i]); break;
                        case 3: AddQue.Remove(RemoveQue[i]); break;
                    }
                    RemoveQue[i].ExistsInQue = -1;
                    ChildrenUpdated = true;
                }
                RemoveQue.Clear();
            // UnityEngine.Profiling.Profiler.EndSample();
             // UnityEngine.Profiling.Profiler.BeginSample("Update");
                QueCount = UpdateQue.Count;
                for (int i = QueCount - 1; i >= 0; i--) {
                    if(UpdateQue[i] == null) continue;
                    switch(UpdateQue[i].ExistsInQue) {
                        case 0: {int Index = RenderQue.IndexOf(UpdateQue[i]); RenderQue.RemoveAt(Index); RenderTransforms.RemoveAt(Index);}; break;
                        case 1: BuildQue.Remove(UpdateQue[i]); break;
                    }
                    UpdateQue[i].ExistsInQue = -1;
                    ChildrenUpdated = true;
                }
            // UnityEngine.Profiling.Profiler.EndSample();
             // UnityEngine.Profiling.Profiler.BeginSample("Add");
                QueCount = AddQue.Count;
                for (int i = QueCount - 1; i >= 0; i--) {
                    bool Contained = AddQue[i].ExistsInQue != 3;
                    if(!Contained || (AddQue[i].AsyncTask == null || AddQue[i].AsyncTask.Status != TaskStatus.RanToCompletion)) {
                        AddQue[i].Reset(1);
                        BuildQue.Add(AddQue[i]);
                        ChildrenUpdated = true;
                    }
                }
                AddQue.Clear();
            // UnityEngine.Profiling.Profiler.EndSample();
             // UnityEngine.Profiling.Profiler.BeginSample("Build");
                QueCount = BuildQue.Count;
                for (int i = QueCount - 1; i >= 0; i--) {//Promotes from Build Que to Render Que
                    if (BuildQue[i].AsyncTask.IsFaulted) {//Fuck, something fucked up
                        Debug.Log(BuildQue[i].AsyncTask.Exception + ", " + BuildQue[i].Name);
                        BuildQue[i].FailureCount++;
                        if(BuildQue[i].FailureCount > 6) {
                            BuildQue[i].ExistsInQue = -1;
                        } else {
                            BuildQue[i].ExistsInQue = 3;
                            AddQue.Add(BuildQue[i]);
                        }
                        BuildQue.RemoveAt(i);
                    } else {
                        if (BuildQue[i].AsyncTask.Status == TaskStatus.RanToCompletion) {
                            if (BuildQue[i].AggTriangles == null || BuildQue[i].AggNodes == null) {
                                BuildQue[i].ExistsInQue = 3;
                                AddQue.Add(BuildQue[i]);
                            } else {
                                BuildQue[i].SetUpBuffers();
                                BuildQue[i].ExistsInQue = 0;
                                RenderTransforms.Add(BuildQue[i].gameObject.transform);
                                RenderQue.Add(BuildQue[i]);
                                ChildrenUpdated = true;
                            }
                            BuildQue.RemoveAt(i);
                        }
                    }
                }
            // UnityEngine.Profiling.Profiler.EndSample();
             // UnityEngine.Profiling.Profiler.BeginSample("Update 2");
                QueCount = UpdateQue.Count;
                for (int i = QueCount - 1; i >= 0; i--) {//Demotes from Render Que to Build Que in case mesh has changed
                    if (UpdateQue[i] != null && UpdateQue[i].gameObject.activeInHierarchy) {
                        if (UpdateQue[i].ExistsInQue != 1) {
                            UpdateQue[i].Reset(1);
                            BuildQue.Add(UpdateQue[i]);
                        }
                        ChildrenUpdated = true;
                    }
                    UpdateQue.RemoveAt(i);
                }
            // UnityEngine.Profiling.Profiler.EndSample();
            }
            {//Instanced Models Data Handling
                InstanceData.UpdateRenderAndBuildQues(ref ChildrenUpdated);
             // UnityEngine.Profiling.Profiler.BeginSample("Asset Instance Remove");
                QueCount = InstanceUpdateQue.Count;
                for (int i = QueCount - 1; i >= 0; i--) {//Demotes from Render Que to Build Que in case mesh has changed
                    if(InstanceRenderQue.Contains(InstanceUpdateQue[i])) {InstanceRenderTransforms.RemoveAt(InstanceRenderQue.IndexOf(InstanceUpdateQue[i])); InstanceRenderQue.Remove(InstanceUpdateQue[i]);}
                    OnlyInstanceUpdated = true;
                }
                InstanceUpdateQue.Clear();
                QueCount = InstanceRemoveQue.Count;
                 for (int i = QueCount - 1; i >= 0; i--) {
                    switch(InstanceRemoveQue[i].ExistsInQue) {
                        default: Debug.Log("INSTANCES BROKE!"); break;
                        case 0: {InstanceRenderTransforms.RemoveAt(InstanceRenderQue.IndexOf(InstanceRemoveQue[i])); InstanceRenderQue.Remove(InstanceRemoveQue[i]);} break;
                        case 1: InstanceBuildQue.Remove(InstanceRemoveQue[i]); break;
                        case 3: InstanceAddQue.Remove(InstanceRemoveQue[i]); break;
                    }
                    InstanceRemoveQue[i].ExistsInQue = -1;
                    OnlyInstanceUpdated = true;
                }
                InstanceRemoveQue.Clear();
            // UnityEngine.Profiling.Profiler.EndSample();

             // UnityEngine.Profiling.Profiler.BeginSample("Asset Instance Add");
                QueCount = InstanceAddQue.Count;
                for (int i = QueCount - 1; i >= 0; i--) {
                    if (InstanceAddQue[i].InstanceParent != null && InstanceAddQue[i].InstanceParent.gameObject.activeInHierarchy) {
                        InstanceAddQue[i].ExistsInQue = 1;
                        InstanceBuildQue.Add(InstanceAddQue[i]);
                        InstanceAddQue.RemoveAt(i);
                    }
                }
            // UnityEngine.Profiling.Profiler.EndSample();
             // UnityEngine.Profiling.Profiler.BeginSample("Asset Instance Build");
                QueCount = InstanceBuildQue.Count;
                for (int i = QueCount - 1; i >= 0; i--) {//Promotes from Build Que to Render Que
                    if (InstanceBuildQue[i].InstanceParent.HasCompleted == true) {
                        InstanceBuildQue[i].ExistsInQue = 0;
                        InstanceRenderTransforms.Add(InstanceBuildQue[i].transform);
                        InstanceRenderQue.Add(InstanceBuildQue[i]);
                        InstanceBuildQue.RemoveAt(i);
                        OnlyInstanceUpdated = true;
                    }
                }
            // UnityEngine.Profiling.Profiler.EndSample();
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
        }

        #if TTLightMapping
            public void BuildCombined()
        #else
            public void EditorBuild()
        #endif
        {//Forces all to rebuild
            ClearAll();
            Terrains = new List<TerrainObject>();
            Terrains = new List<TerrainObject>(GetComponentsInChildren<TerrainObject>());
            init();
            AddQue = new List<ParentObject>();
            RemoveQue = new List<ParentObject>();
            RenderQue = new List<ParentObject>();
            RenderTransforms = new List<Transform>();
            BuildQue = new List<ParentObject>(GetComponentsInChildren<ParentObject>());
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
            didstart = false;
        }
        #if TTLightMapping
            public void EditorBuild()
        #else
            public void BuildCombined()
        #endif
        {//Only has unbuilt be built
            Terrains = new List<TerrainObject>(GetComponentsInChildren<TerrainObject>());
            init();
            List<ParentObject> TempQue = new List<ParentObject>(GameObject.FindObjectsOfType<ParentObject>());
            InstanceAddQue = new List<InstancedObject>(GetComponentsInChildren<InstancedObject>());
            InstanceData.BuildCombined();
            RunningTasks = 0;
            for (int i = 0; i < TempQue.Count; i++)
            {
                if(TempQue[i].transform.parent.name == "InstancedStorage") continue;
                if (TempQue[i].HasCompleted && !TempQue[i].NeedsToUpdate) {
                    TempQue[i].ExistsInQue = 0;
                    RenderQue.Add(TempQue[i]);
                    RenderTransforms.Add(TempQue[i].gameObject.transform);
                }
                else {TempQue[i].ExistsInQue = 1; BuildQue.Add(TempQue[i]); RunningTasks++;}
            }
            for (int i = 0; i < BuildQue.Count; i++)
            {
                var CurrentRep = i;
                BuildQue[CurrentRep].LoadData();
                BuildQue[CurrentRep].AsyncTask = Task.Run(() => {BuildQue[CurrentRep].BuildTotal(); RunningTasks--;});
            }
            ParentCountHasChanged = true;
            if (RenderQue.Count != 0) {CommandBuffer cmd = new CommandBuffer(); int throwaway = UpdateTLAS(cmd);  Graphics.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        cmd.Release();}
        }

        private void AccumulateData(CommandBuffer cmd)
        {
            // UnityEngine.Profiling.Profiler.BeginSample("Update Object Lists");
            UpdateRenderAndBuildQues();
            // UnityEngine.Profiling.Profiler.EndSample();

            int ParentsLength = RenderQue.Count;
            if (ChildrenUpdated || ParentCountHasChanged)
            {
                int CurNodeOffset = 2 * (ParentsLength + InstanceRenderQue.Count);
                int AggTriCount = 0;
                int AggNodeCount = CurNodeOffset;
                int LightTriCount = 0;
                int CurTriOffset = 0;
                int CurLightTriOffset = 0;
                TotalParentObjectSize = 0;
                LightMeshCount = 0;
                LightMeshes.Clear();
                LightTransforms.Clear();
                int TotalMatCount = 0;
                if (BVH8AggregatedBuffer != null)
                {
                    BVH8AggregatedBuffer.Release();
                    AggTriBuffer.Release();
                    LightTriBuffer.Release();
                }
                for (int i = 0; i < ParentsLength; i++)
                {
                    TotalMatCount += RenderQue[i]._Materials.Count;
                    AggNodeCount += RenderQue[i].AggNodes.Length;
                    AggTriCount += RenderQue[i].AggTriangles.Length;
                    LightTriCount += RenderQue[i].LightTriangles.Count;
                }
                for (int i = 0; i < InstanceData.RenderQue.Count; i++)
                {
                    TotalMatCount += InstanceData.RenderQue[i]._Materials.Count;
                    AggNodeCount += InstanceData.RenderQue[i].AggNodes.Length;
                    AggTriCount += InstanceData.RenderQue[i].AggTriangles.Length;
                    LightTriCount += InstanceData.RenderQue[i].LightTriangles.Count;
                }
                Debug.Log("Total Tri Count: " + AggTriCount);
                if(LightTriCount == 0) LightTriCount++;
                if (AggNodeCount != 0)
                {//Accumulate the BVH nodes and triangles for all normal models
                    #if TTLightMapping
                        LightMapTexIndex = new List<int>();
                        for(int i = 0; i < ParentsLength; i++) {
                            int Count2 = RenderQue[i].LightMapTexIndex.Count;
                            for(int j = 0; j < Count2; j++) {
                                if(!LightMapTexIndex.Contains(RenderQue[i].LightMapTexIndex[j])) LightMapTexIndex.Add(RenderQue[i].LightMapTexIndex[j]);
                            }
                        }
                        int LightMapCount = LightMapTexIndex.Count;
                        LightMaps = new LightMapData[LightMapCount];
                        for(int i = 0; i < LightMapCount; i++) {
                            LightMaps[i].LightMapIndex = i;
                            LightMaps[i].LightMapTris = new List<LightMapTriData>();
                        }
                        for(int i = 0; i < ParentsLength; i++) {
                            int Count2 = RenderQue[i].LightMapTexIndex.Count;
                            for(int j = 0; j < Count2; j++) {
                                Debug.Log(RenderQue[i].LightMapTris.Length);
                                LightMaps[RenderQue[i].LightMapTexIndex[j]].LightMapTris.AddRange(RenderQue[i].LightMapTris[j]);
                            }
                        }
                    #endif

                    BVH8AggregatedBuffer = new ComputeBuffer(AggNodeCount, 80);
                    AggTriBuffer = new ComputeBuffer(AggTriCount, 88);
                    LightTriBuffer = new ComputeBuffer(LightTriCount, 40);
                    MeshFunctions.SetBuffer(TriangleBufferKernel, "OutCudaTriArray", AggTriBuffer);
                    MeshFunctions.SetBuffer(NodeBufferKernel, "OutAggNodes", BVH8AggregatedBuffer);
                    MeshFunctions.SetBuffer(LightBufferKernel, "LightTrianglesOut", LightTriBuffer);
                    int MatOffset = 0;
                    for (int i = 0; i < ParentsLength; i++)
                    {
                        RenderQue[i].UpdateData();
                        // cmd.BeginSample("AccumBufferTri");
                        cmd.SetComputeIntParam(MeshFunctions, "Offset", CurTriOffset);
                        cmd.SetComputeIntParam(MeshFunctions, "Count", RenderQue[i].TriBuffer.count);
                        cmd.SetComputeBufferParam(MeshFunctions, TriangleBufferKernel, "InCudaTriArray", RenderQue[i].TriBuffer);
                        cmd.DispatchCompute(MeshFunctions, TriangleBufferKernel, (int)Mathf.Ceil(RenderQue[i].TriBuffer.count / 372.0f), 1, 1);
                        // cmd.EndSample("AccumBufferTri");

                        // cmd.BeginSample("AccumBufferNode");
                        cmd.SetComputeIntParam(MeshFunctions, "Offset", CurNodeOffset);
                        cmd.SetComputeIntParam(MeshFunctions, "Count", RenderQue[i].BVHBuffer.count);
                        cmd.SetComputeBufferParam(MeshFunctions, NodeBufferKernel, "InAggNodes", RenderQue[i].BVHBuffer);
                        cmd.DispatchCompute(MeshFunctions, NodeBufferKernel, (int)Mathf.Ceil(RenderQue[i].BVHBuffer.count / 372.0f), 1, 1);
                        // cmd.EndSample("AccumBufferNode");

                        if (RenderQue[i].LightTriangles.Count != 0)
                        {
                            cmd.SetComputeIntParam(MeshFunctions, "Offset", CurLightTriOffset);
                            cmd.SetComputeIntParam(MeshFunctions, "Count", RenderQue[i].LightTriBuffer.count);
                            cmd.SetComputeBufferParam(MeshFunctions, LightBufferKernel, "LightTrianglesIn", RenderQue[i].LightTriBuffer);
                            cmd.DispatchCompute(MeshFunctions, LightBufferKernel, (int)Mathf.Ceil(RenderQue[i].LightTriBuffer.count / 372.0f), 1, 1);
                            LightMeshCount++;
                            LightTransforms.Add(RenderTransforms[i]);
                            LightMeshes.Add(new LightMeshData()
                            {
                                StartIndex = CurLightTriOffset,
                                IndexEnd = RenderQue[i].LightTriangles.Count + CurLightTriOffset,
                                MatOffset = MatOffset,
                                LockedMeshIndex = i
                            });


                            RenderQue[i].LightTriOffset = CurLightTriOffset;
                            CurLightTriOffset += RenderQue[i].LightTriangles.Count;
                            TotalParentObjectSize += RenderQue[i].LightTriBuffer.count * RenderQue[i].LightTriBuffer.stride;
                        }
                        TotalParentObjectSize += RenderQue[i].TriBuffer.count * RenderQue[i].TriBuffer.stride;
                        TotalParentObjectSize += RenderQue[i].BVHBuffer.count * RenderQue[i].BVHBuffer.stride;
                        RenderQue[i].NodeOffset = CurNodeOffset;
                        RenderQue[i].TriOffset = CurTriOffset;
                        CurNodeOffset += RenderQue[i].AggNodes.Length;
                        CurTriOffset += RenderQue[i].AggTriangles.Length;
                        MatOffset += RenderQue[i]._Materials.Count;
                    }
                    int InstanceQueCount = InstanceData.RenderQue.Count;
                    for (int i = 0; i < InstanceQueCount; i++)
                    {//Accumulate the BVH nodes and triangles for all instanced models
                        InstanceData.RenderQue[i].UpdateData();
                        InstanceData.RenderQue[i].InstanceMeshIndex = i + ParentsLength;

                        // cmd.BeginSample("AccumBufferInstanceTri");
                        cmd.SetComputeIntParam(MeshFunctions, "Offset", CurTriOffset);
                        cmd.SetComputeIntParam(MeshFunctions, "Count", InstanceData.RenderQue[i].TriBuffer.count);
                        cmd.SetComputeBufferParam(MeshFunctions, TriangleBufferKernel, "InCudaTriArray", InstanceData.RenderQue[i].TriBuffer);
                        cmd.DispatchCompute(MeshFunctions, TriangleBufferKernel, (int)Mathf.Ceil(InstanceData.RenderQue[i].TriBuffer.count / 372.0f), 1, 1);
                        // cmd.EndSample("AccumBufferInstanceTri");

                        // cmd.BeginSample("AccumBufferInstanceNode");
                        cmd.SetComputeIntParam(MeshFunctions, "Offset", CurNodeOffset);
                        cmd.SetComputeIntParam(MeshFunctions, "Count", InstanceData.RenderQue[i].BVHBuffer.count);
                        cmd.SetComputeBufferParam(MeshFunctions, NodeBufferKernel, "InAggNodes", InstanceData.RenderQue[i].BVHBuffer);
                        cmd.DispatchCompute(MeshFunctions, NodeBufferKernel, (int)Mathf.Ceil(InstanceData.RenderQue[i].BVHBuffer.count / 372.0f), 1, 1);
                        // cmd.EndSample("AccumBufferInstanceNode");
                        if (InstanceData.RenderQue[i].LightTriangles.Count != 0)
                        {
                            cmd.SetComputeIntParam(MeshFunctions, "Offset", CurLightTriOffset);
                            cmd.SetComputeIntParam(MeshFunctions, "Count", InstanceData.RenderQue[i].LightTriBuffer.count);
                            cmd.SetComputeBufferParam(MeshFunctions, LightBufferKernel, "LightTrianglesIn", InstanceData.RenderQue[i].LightTriBuffer);
                            cmd.DispatchCompute(MeshFunctions, LightBufferKernel, (int)Mathf.Ceil(InstanceData.RenderQue[i].LightTriBuffer.count / 372.0f), 1, 1);
                            
                            InstanceData.RenderQue[i].LightTriOffset = CurLightTriOffset;
                            CurLightTriOffset += InstanceData.RenderQue[i].LightTriangles.Count;
                            InstanceData.RenderQue[i].LightEndIndex = CurLightTriOffset;
                        }

                        InstanceData.RenderQue[i].NodeOffset = CurNodeOffset;
                        InstanceData.RenderQue[i].TriOffset = CurTriOffset;
                        CurNodeOffset += InstanceData.RenderQue[i].AggNodes.Length;
                        CurTriOffset += InstanceData.RenderQue[i].AggTriangles.Length;

                    }
                    for (int i = 0; i < InstanceRenderQue.Count; i++)
                    {
                        if (InstanceRenderQue[i].InstanceParent.LightTriangles.Count != 0)
                        {
                            LightMeshCount++;
                            LightTransforms.Add(InstanceRenderTransforms[i]);
                            LightMeshes.Add(new LightMeshData()
                            {
                                LockedMeshIndex = i + ParentsLength,
                                StartIndex = InstanceRenderQue[i].InstanceParent.LightTriOffset,
                                IndexEnd = InstanceRenderQue[i].InstanceParent.LightEndIndex
                            });
                        }
                    }
                }

                if (LightMeshCount == 0) { LightMeshes.Add(new LightMeshData() { }); }


                if (!OnlyInstanceUpdated || _Materials.Length == 0) CreateAtlas(TotalMatCount);
            }
            ParentCountHasChanged = false;
            if (UseSkinning && didstart) {
                for (int i = 0; i < ParentsLength; i++) {//Refit BVH's of skinned meshes
                    if (RenderQue[i].IsSkinnedGroup || RenderQue[i].IsDeformable) {
                        RenderQue[i].RefitMesh(ref BVH8AggregatedBuffer, ref AggTriBuffer, ref LightTriBuffer, cmd);
                        if(i < MyMeshesCompacted.Count) {
                        MyMeshDataCompacted TempMesh2 = MyMeshesCompacted[i];
                        TempMesh2.Transform = RenderTransforms[i].worldToLocalMatrix;
                        MyMeshesCompacted[i] = TempMesh2;
                            MeshAABBs[i] = RenderQue[i].aabb;
                        }
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

        int MaxRecur = 0;
        int TempRecur = 0;
          private List<Vector3Int> IsLeafList;
    unsafe public void DocumentNodes(int CurrentNode, int ParentNode, int NextNode, int NextBVH8Node, bool IsLeafRecur, int CurRecur) {
        NodeIndexPairData CurrentPair = NodePair[CurrentNode];
        TempRecur = Mathf.Max(TempRecur, CurRecur);
        IsLeafList[CurrentNode] = new Vector3Int(IsLeafList[CurrentNode].x, CurRecur, ParentNode);
        if (!IsLeafRecur) {
            ToBVHIndex[NextBVH8Node] = CurrentNode;
            IsLeafList[CurrentNode] = new Vector3Int(0, IsLeafList[CurrentNode].y, IsLeafList[CurrentNode].z);
            BVHNode8Data node = TLASBVH8.BVH8Nodes[NextBVH8Node];
            NodeIndexPairData IndexPair = new NodeIndexPairData();
            IndexPair.AABB = new AABB();

            Vector3 e = Vector3.zero;
            for(int i = 0; i < 3; i++) e[i] = (float)(System.Convert.ToSingle((int)(System.Convert.ToUInt32(node.e[i]) << 23)));
            for (int i = 0; i < 8; i++) {
                IndexPair.InNodeOffset = i;
                float maxFactorX = node.quantized_max_x[i] * e.x;
                float maxFactorY = node.quantized_max_y[i] * e.y;
                float maxFactorZ = node.quantized_max_z[i] * e.z;
                float minFactorX = node.quantized_min_x[i] * e.x;
                float minFactorY = node.quantized_min_y[i] * e.y;
                float minFactorZ = node.quantized_min_z[i] * e.z;

                IndexPair.AABB.Create(new Vector3(maxFactorX, maxFactorY, maxFactorZ) + node.p, new Vector3(minFactorX, minFactorY, minFactorZ) + node.p);

                NextNode++;
                IndexPair.BVHNode = NextBVH8Node;
                NodePair.Add(IndexPair);
                IsLeafList.Add(new Vector3Int(0,0,0));
                if ((node.meta[i] & 0b11111) < 24) {
                    DocumentNodes(NodePair.Count - 1, CurrentNode, NextNode, -1, true, CurRecur + 1);
                } else {
                    int child_offset = (byte)node.meta[i] & 0b11111;
                    int child_index = (int)node.base_index_child + child_offset - 24;
                    DocumentNodes(NodePair.Count - 1, CurrentNode, NextNode, child_index, false, CurRecur + 1);
                }
            }
        } else IsLeafList[CurrentNode] = new Vector3Int(1, IsLeafList[CurrentNode].y, IsLeafList[CurrentNode].z);

        NodePair[CurrentNode] = CurrentPair;
    }
        List<BVHNode8DataFixed> SplitNodes;
        List<NodeIndexPairData> NodePair;
        Layer[] ForwardStack;
        Layer2[] LayerStack;
        int[] CWBVHIndicesBufferInverted;
        ComputeBuffer[] WorkingBuffer;
        ComputeBuffer NodeBuffer;
        ComputeBuffer StackBuffer;
        ComputeBuffer ToBVHIndexBuffer;
        ComputeBuffer BVHDataBuffer;
        ComputeBuffer BVHBuffer;

        unsafe public void ConstructNewTLAS() {
            #if HardwareRT
                int TotLength = 0;
                int MeshOffset = 0;
                SubMeshOffsets = new List<int>();
                MeshOffsets = new List<Vector2>();
                AccelStruct.ClearInstances();
                for(int i = 0; i < RenderQue.Count; i++) {
                    RenderQue[i].HWRTIndex = new List<int>();
                    foreach(var A in RenderQue[i].Renderers) {
                        MeshOffsets.Add(new Vector2(SubMeshOffsets.Count, i));
                        var B2 = A.gameObject;
                        Mesh mesh = ((B2.GetComponent<MeshFilter>() != null) ? B2.GetComponent<MeshFilter>().sharedMesh : B2.GetComponent<SkinnedMeshRenderer>().sharedMesh);
                        int SubMeshCount = mesh.subMeshCount;
                        for (int i2 = 0; i2 < SubMeshCount; ++i2)
                        {//Add together all the submeshes in the mesh to consider it as one object
                            SubMeshOffsets.Add(TotLength);
                            int IndiceLength = (int)mesh.GetIndexCount(i2) / 3;
                            TotLength += IndiceLength;
                        }
                        RayTracingSubMeshFlags[] B = new RayTracingSubMeshFlags[SubMeshCount];
                        for(int i2 = 0; i2 < SubMeshCount; i2++) {
                            B[i2] = RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.ClosestHitOnly;
                        }
                        AccelStruct.AddInstance(A, B, true, false, (uint)((B2.GetComponent<RayTracingObject>().SpecTrans[0] == 1) ? 0x2 : 0x1), (uint)MeshOffset);
                        MeshOffset++;
                    }
                }
            AccelStruct.Build();
            #else
                BVH2Builder BVH = new BVH2Builder(MeshAABBs);
                TLASBVH8 = new BVH8Builder(BVH);
                System.Array.Resize(ref TLASBVH8.BVH8Nodes, TLASBVH8.cwbvhnode_count);
                ToBVHIndex = new int[TLASBVH8.cwbvhnode_count];
                if (TempBVHArray == null || TLASBVH8.BVH8Nodes.Length != TempBVHArray.Length) TempBVHArray = new BVHNode8DataCompressed[TLASBVH8.BVH8Nodes.Length];
                CommonFunctions.Aggregate(ref TempBVHArray, ref TLASBVH8.BVH8Nodes);
                BVHNodeCount = TLASBVH8.BVH8Nodes.Length;
                NodePair = new List<NodeIndexPairData>();
                NodePair.Add(new NodeIndexPairData());
                IsLeafList = new List<Vector3Int>();
                IsLeafList.Add(new Vector3Int(0,0,0));
                DocumentNodes(0, 0, 1, 0, false, 0);
                TempRecur++;
                MaxRecur = TempRecur;
                int NodeCount = NodePair.Count;
                ForwardStack = new Layer[NodeCount];
                LayerStack = new Layer2[MaxRecur];
                Layer PresetLayer = new Layer();
                Layer2 TempSlab = new Layer2();
                TempSlab.Slab = new List<int>();

                for (int i = 0; i < MaxRecur; i++) LayerStack[i] = TempSlab;
                for(int i = 0; i < 8; i++) PresetLayer.Children[i] = 0;

                for (int i = 0; i < NodePair.Count; i++) {
                    ForwardStack[i] = PresetLayer;
                    if (IsLeafList[i].x == 1) {
                        int first_triangle = (byte)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] & 0b11111;
                        int NumBits = CommonFunctions.NumberOfSetBits((byte)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] >> 5);
                        ForwardStack[i].Children[NodePair[i].InNodeOffset] = NumBits + ((int)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].base_index_triangle + first_triangle) * 24 + 1;
                    } else {
                        ForwardStack[i].Children[NodePair[i].InNodeOffset] = (-i) - 1;
                    }
                    ForwardStack[IsLeafList[i].z].Children[NodePair[i].InNodeOffset] = (-i) - 1;
                    
                    var TempLayer = LayerStack[IsLeafList[i].y];
                    TempLayer.Slab.Add(i);
                    LayerStack[IsLeafList[i].y] = TempLayer;
                }
                CommonFunctions.ConvertToSplitNodes(TLASBVH8, ref SplitNodes);
                List<Layer2> TempStack = new List<Layer2>();
                for (int i = 0; i < LayerStack.Length; i++) if(LayerStack[i].Slab.Count != 0) TempStack.Add(LayerStack[i]);
                MaxRecur = TempStack.Count;

                if(WorkingBuffer != null) for(int i = 0; i < WorkingBuffer.Length; i++) WorkingBuffer[i]?.Release();
                if (NodeBuffer != null) NodeBuffer.Release();
                if (StackBuffer != null) StackBuffer.Release();
                if (ToBVHIndexBuffer != null) ToBVHIndexBuffer.Release();
                if (BVHDataBuffer != null) BVHDataBuffer.Release();
                if (BVHBuffer != null) BVHBuffer.Release();
                if (BoxesBuffer != null) BoxesBuffer.Release();
                if (TLASCWBVHIndexes != null) TLASCWBVHIndexes.Release();
                WorkingBuffer = new ComputeBuffer[LayerStack.Length];
                for (int i = 0; i < MaxRecur; i++) {
                    WorkingBuffer[i] = new ComputeBuffer(LayerStack[i].Slab.Count, 4);
                    WorkingBuffer[i].SetData(LayerStack[i].Slab);
                }
                NodeBuffer = new ComputeBuffer(NodePair.Count, 32);
                NodeBuffer.SetData(NodePair);
                StackBuffer = new ComputeBuffer(ForwardStack.Length, 32);
                StackBuffer.SetData(ForwardStack);
                ToBVHIndexBuffer = new ComputeBuffer(ToBVHIndex.Length, 4);
                ToBVHIndexBuffer.SetData(ToBVHIndex);
                BVHDataBuffer = new ComputeBuffer(TempBVHArray.Length, 260);
                BVHDataBuffer.SetData(SplitNodes);
                BVHBuffer = new ComputeBuffer(TempBVHArray.Length, 80);
                BVHBuffer.SetData(TempBVHArray);
                BoxesBuffer = new ComputeBuffer(MeshAABBs.Length, 24);
                TLASCWBVHIndexes = new ComputeBuffer(MeshAABBs.Length, 4);
                TLASCWBVHIndexes.SetData(TLASBVH8.cwbvh_indices);
                CurFrame = 0;
            #endif
        }
        Task TLASTask;
        unsafe async void CorrectRefit(AABB[] Boxes) {
            TempRecur = 0;
            BVH2Builder BVH = new BVH2Builder(Boxes);
            TLASBVH8 = new BVH8Builder(BVH);
                System.Array.Resize(ref TLASBVH8.BVH8Nodes, TLASBVH8.cwbvhnode_count);
                ToBVHIndex = new int[TLASBVH8.cwbvhnode_count];
                if (TempBVHArray == null || TLASBVH8.BVH8Nodes.Length != TempBVHArray.Length) TempBVHArray = new BVHNode8DataCompressed[TLASBVH8.BVH8Nodes.Length];
                CommonFunctions.Aggregate(ref TempBVHArray, ref TLASBVH8.BVH8Nodes);

                NodePair = new List<NodeIndexPairData>();
                NodePair.Add(new NodeIndexPairData());
                IsLeafList = new List<Vector3Int>();
                IsLeafList.Add(new Vector3Int(0,0,0));
                DocumentNodes(0, 0, 1, 0, false, 0);
                TempRecur++;
                int NodeCount = NodePair.Count;
                ForwardStack = new Layer[NodeCount];
                LayerStack = new Layer2[TempRecur];
                Layer PresetLayer = new Layer();
                Layer2 TempSlab = new Layer2();
                TempSlab.Slab = new List<int>();

                for (int i = 0; i < TempRecur; i++) LayerStack[i] = TempSlab;
                for(int i = 0; i < 8; i++) PresetLayer.Children[i] = 0;

                for (int i = 0; i < NodePair.Count; i++) {
                    ForwardStack[i] = PresetLayer;
                    if (IsLeafList[i].x == 1) {
                        int first_triangle = (byte)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] & 0b11111;
                        int NumBits = CommonFunctions.NumberOfSetBits((byte)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] >> 5);
                        ForwardStack[i].Children[NodePair[i].InNodeOffset] = NumBits + ((int)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].base_index_triangle + first_triangle) * 24 + 1;
                    } else {
                        ForwardStack[i].Children[NodePair[i].InNodeOffset] = (-i) - 1;
                    }
                    ForwardStack[IsLeafList[i].z].Children[NodePair[i].InNodeOffset] = (-i) - 1;
                    
                    var TempLayer = LayerStack[IsLeafList[i].y];
                    TempLayer.Slab.Add(i);
                    LayerStack[IsLeafList[i].y] = TempLayer;
                }
                CommonFunctions.ConvertToSplitNodes(TLASBVH8, ref SplitNodes);
                List<Layer2> TempStack = new List<Layer2>();
                for (int i = 0; i < LayerStack.Length; i++) if(LayerStack[i].Slab.Count != 0) TempStack.Add(LayerStack[i]);

                TempRecur = TempStack.Count;
                LayerStack = TempStack.ToArray();
            return;
        }
        ComputeBuffer BoxesBuffer;
        int CurFrame = 0;
        int BVHNodeCount = 0;
        public unsafe void RefitTLAS(AABB[] Boxes, CommandBuffer cmd)
        {
            #if !HardwareRT
            if(TLASTask == null) TLASTask = Task.Run(() => CorrectRefit(Boxes));

            CurFrame++;
             if(TLASTask.Status == TaskStatus.RanToCompletion && CurFrame % 25 == 24) {
                MaxRecur = TempRecur; 

                if(WorkingBuffer != null) for(int i = 0; i < WorkingBuffer.Length; i++) WorkingBuffer[i]?.Release();
                if (NodeBuffer != null) NodeBuffer.Release();
                if (StackBuffer != null) StackBuffer.Release();
                if (ToBVHIndexBuffer != null) ToBVHIndexBuffer.Release();
                if (BVHDataBuffer != null) BVHDataBuffer.Release();
                if (BVHBuffer != null) BVHBuffer.Release();
                if (BoxesBuffer != null) BoxesBuffer.Release();
                if (TLASCWBVHIndexes != null) TLASCWBVHIndexes.Release();
                WorkingBuffer = new ComputeBuffer[LayerStack.Length];
                for (int i = 0; i < MaxRecur; i++) {
                    WorkingBuffer[i] = new ComputeBuffer(LayerStack[i].Slab.Count, 4);
                    WorkingBuffer[i].SetData(LayerStack[i].Slab);
                }
                NodeBuffer = new ComputeBuffer(NodePair.Count, 32);
                NodeBuffer.SetData(NodePair);
                StackBuffer = new ComputeBuffer(ForwardStack.Length, 32);
                StackBuffer.SetData(ForwardStack);
                ToBVHIndexBuffer = new ComputeBuffer(ToBVHIndex.Length, 4);
                ToBVHIndexBuffer.SetData(ToBVHIndex);
                BVHDataBuffer = new ComputeBuffer(TempBVHArray.Length, 260);
                BVHDataBuffer.SetData(SplitNodes);
                BVHBuffer = new ComputeBuffer(TempBVHArray.Length, 80);
                BVHBuffer.SetData(TempBVHArray);
                BoxesBuffer = new ComputeBuffer(MeshAABBs.Length, 24);
                TLASCWBVHIndexes = new ComputeBuffer(MeshAABBs.Length, 4);
                TLASCWBVHIndexes.SetData(TLASBVH8.cwbvh_indices);
                 for (int i = 0; i < RenderQue.Count; i++) {
                    ParentObject TargetParent = RenderQue[i];
                    MeshAABBs[TargetParent.CompactedMeshData] = TargetParent.aabb;
                }
                Boxes = MeshAABBs;
                #if !HardwareRT
                    BVH8AggregatedBuffer.SetData(TempBVHArray, 0, 0, TempBVHArray.Length);
                #endif
                BVHNodeCount = TLASBVH8.BVH8Nodes.Length;

                TLASTask = Task.Run(() => CorrectRefit(Boxes));
            }
                // cmd.BeginSample("TLAS Refit Init");
                cmd.SetComputeIntParam(Refitter, "NodeCount", NodeBuffer.count);
                cmd.SetComputeBufferParam(Refitter, NodeInitializerKernel, "AllNodes", NodeBuffer);

                cmd.DispatchCompute(Refitter, NodeInitializerKernel, (int)Mathf.Ceil(NodeBuffer.count / (float)256), 1, 1);
                // cmd.EndSample("TLAS Refit Init");

                // cmd.BeginSample("TLAS Refit Refit");
                BoxesBuffer.SetData(Boxes);
                cmd.SetComputeBufferParam(Refitter, RefitLayer, "TLASCWBVHIndices", TLASCWBVHIndexes);
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
                // cmd.EndSample("TLAS Refit Refit");

                // cmd.BeginSample("TLAS Refit Node Update");
                cmd.SetComputeIntParam(Refitter, "NodeCount", NodeBuffer.count);
                cmd.SetComputeBufferParam(Refitter, NodeUpdater, "AllNodes", NodeBuffer);
                cmd.SetComputeBufferParam(Refitter, NodeUpdater, "BVHNodes", BVHDataBuffer);
                cmd.SetComputeBufferParam(Refitter, NodeUpdater, "ToBVHIndex", ToBVHIndexBuffer);
                cmd.DispatchCompute(Refitter, NodeUpdater, (int)Mathf.Ceil(NodeBuffer.count / (float)256), 1, 1);
                // cmd.EndSample("TLAS Refit Node Update");

                // cmd.BeginSample("TLAS Refit Node Compress");
                cmd.SetComputeIntParam(Refitter, "NodeCount", BVHNodeCount);
                cmd.SetComputeIntParam(Refitter, "NodeOffset", 0);
                cmd.SetComputeBufferParam(Refitter, NodeCompress, "BVHNodes", BVHDataBuffer);
                try {
                    cmd.SetComputeBufferParam(Refitter, NodeCompress, "AggNodes", BVH8AggregatedBuffer);
                } finally {}
                cmd.DispatchCompute(Refitter, NodeCompress, (int)Mathf.Ceil(NodeBuffer.count / (float)256), 1, 1);
                // cmd.EndSample("TLAS Refit Node Compress");

            #endif
        }

        private bool ChangedLastFrame = true;


        public int UpdateTLAS(CommandBuffer cmd)
        {  //Allows for objects to be moved in the scene or animated while playing 

            bool LightsHaveUpdated = false;
            AccumulateData(cmd);

                // UnityEngine.Profiling.Profiler.BeginSample("Lights Update");
            if (!didstart || PrevLightCount != RayTracingMaster._rayTracingLights.Count || UnityLights.Count == 0)
            {
                UnityLights.Clear();
                UnityLightCount = 0;
                foreach (RayTracingLights RayLight in RayTracingMaster._rayTracingLights) {
                    UnityLightCount++;
                    RayLight.UpdateLight();
                    if (RayLight.ThisLightData.Type == 1) SunDirection = RayLight.ThisLightData.Direction;
                    RayLight.ArrayIndex = UnityLightCount - 1;
                    RayLight.ThisLightData.Radiance *= LightEnergyScale;
                    UnityLights.Add(RayLight.ThisLightData);
                }
                if (UnityLights.Count == 0) { UnityLights.Add(new LightData() { }); }
                if (PrevLightCount != RayTracingMaster._rayTracingLights.Count) LightsHaveUpdated = true;
                PrevLightCount = RayTracingMaster._rayTracingLights.Count;
            } else {
                int LightCount = RayTracingMaster._rayTracingLights.Count;
                RayTracingLights RayLight;
                for (int i = 0; i < LightCount; i++) {
                    RayLight = RayTracingMaster._rayTracingLights[i];
                    RayLight.UpdateLight();
                    RayLight.ThisLightData.Radiance *= LightEnergyScale;
                    if (RayLight.ThisLightData.Type == 1) SunDirection = RayLight.ThisLightData.Direction;
                    try{UnityLights[RayLight.ArrayIndex] = RayLight.ThisLightData;} catch(System.Exception throwawayerror) {}
                }
            }
                // UnityEngine.Profiling.Profiler.EndSample();

            int MatOffset = 0;
            int MeshDataCount = RenderQue.Count;
            int aggregated_bvh_node_count = 2 * (MeshDataCount + InstanceRenderQue.Count);
            int AggNodeCount = aggregated_bvh_node_count;
            int AggTriCount = 0;
            if (ChildrenUpdated || !didstart || OnlyInstanceUpdated || MyMeshesCompacted.Count == 0)
            {
                MyMeshesCompacted.Clear();// = new MyMeshDataCompacted[MeshDataCount + InstanceRenderQue.Count];
                AggData[] Aggs = new AggData[InstanceData.RenderQue.Count];
                // UnityEngine.Profiling.Profiler.BeginSample("Remake Initial Data");
                for (int i = 0; i < MeshDataCount; i++) {
                    if (!RenderQue[i].IsSkinnedGroup && !RenderQue[i].IsDeformable) RenderQue[i].UpdateAABB(RenderTransforms[i]);
                    MyMeshesCompacted.Add(new MyMeshDataCompacted() {
                        mesh_data_bvh_offsets = aggregated_bvh_node_count,
                        Transform = RenderTransforms[i].worldToLocalMatrix,
                        AggIndexCount = AggTriCount,
                        AggNodeCount = AggNodeCount,
                        MaterialOffset = MatOffset,
                        LightTriCount = RenderQue[i].LightTriangles.Count
                    });
                    RenderQue[i].CompactedMeshData = i;
                    MatOffset += RenderQue[i].MatOffset;
                    MeshAABBs[i] = RenderQue[i].aabb;
                    AggNodeCount += RenderQue[i].AggBVHNodeCount;//Can I replace this with just using aggregated_bvh_node_count below?
                    AggTriCount += RenderQue[i].AggIndexCount;
                    #if !HardwareRT
                        aggregated_bvh_node_count += RenderQue[i].BVH.cwbvhnode_count;
                    #endif
                }
                // UnityEngine.Profiling.Profiler.EndSample();

                // UnityEngine.Profiling.Profiler.BeginSample("Remake Initial Instance Data A");
                int InstanceCount = InstanceData.RenderQue.Count;
                for (int i = 0; i < InstanceCount; i++)
                {
                    InstanceData.RenderQue[i].CompactedMeshData = i;
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
                // UnityEngine.Profiling.Profiler.EndSample();
                // UnityEngine.Profiling.Profiler.BeginSample("Remake Initial Instance Data B");
                InstanceCount = InstanceRenderQue.Count;
                int MeshCount = MeshDataCount;
                for (int i = 0; i < InstanceCount; i++)
                {
                    int Index = InstanceRenderQue[i].InstanceParent.CompactedMeshData;
                    MyMeshesCompacted.Add(new MyMeshDataCompacted() {
                        mesh_data_bvh_offsets = Aggs[Index].mesh_data_bvh_offsets,
                        Transform = InstanceRenderQue[i].transform.worldToLocalMatrix,
                        AggIndexCount = Aggs[Index].AggIndexCount,
                        AggNodeCount = Aggs[Index].AggNodeCount,
                        MaterialOffset = Aggs[Index].MaterialOffset,
                        LightTriCount = Aggs[Index].LightTriCount

                    });
                    InstanceRenderQue[i].CompactedMeshData = MeshCount + i;
                    AABB aabb = InstanceRenderQue[i].InstanceParent.aabb_untransformed;
                    CreateAABB(InstanceRenderQue[i].transform, ref aabb);
                    MeshAABBs[RenderQue.Count + i] = aabb;
                }
                // UnityEngine.Profiling.Profiler.EndSample();


                Debug.Log("Total Object Count: " + MeshAABBs.Length);
                // UnityEngine.Profiling.Profiler.BeginSample("Update Materials");
                UpdateMaterials();
                // UnityEngine.Profiling.Profiler.EndSample();
                ConstructNewTLAS();
                #if !HardwareRT
                    BVH8AggregatedBuffer.SetData(TempBVHArray, 0, 0, TempBVHArray.Length);
                #endif
            } else {
                // UnityEngine.Profiling.Profiler.BeginSample("Update Transforms");
                Transform TargetTransform;
                ParentObject TargetParent;
                int ClosedCount = 0;
                MyMeshDataCompacted TempMesh2;
                for (int i = 0; i < MeshDataCount; i++)
                {
                    if (RenderTransforms[i].hasChanged)
                    {
                        TargetParent = RenderQue[i];
                        // RenderTransforms[i].hasChanged = false;
                        if (TargetParent.IsSkinnedGroup || RenderQue[i].IsDeformable) continue;
                        TargetTransform = RenderTransforms[i];
                        TargetParent.UpdateAABB(TargetTransform);
                        TempMesh2 = MyMeshesCompacted[i];
                        TempMesh2.Transform = TargetTransform.worldToLocalMatrix;
                        MyMeshesCompacted[i] = TempMesh2;
                        #if HardwareRT
                            foreach(var a in TargetParent.Renderers) {
                                AccelStruct.UpdateInstanceTransform(a);
                            }
                        #endif
                        MeshAABBs[i] = TargetParent.aabb;
                    }
                }
                if(MeshDataCount != 1 && ClosedCount == MeshDataCount - 1) return 0;
                // UnityEngine.Profiling.Profiler.EndSample();

                // UnityEngine.Profiling.Profiler.BeginSample("Refit TLAS");
                int ListCount = InstanceRenderQue.Count;
                for (int i = 0; i < ListCount; i++)
                {
                    if (InstanceRenderTransforms[i].hasChanged || ChangedLastFrame)
                    {
                        TargetTransform = InstanceRenderTransforms[i];
                         MyMeshDataCompacted TempMesh = MyMeshesCompacted[InstanceRenderQue[i].CompactedMeshData];
                        TempMesh.Transform = TargetTransform.worldToLocalMatrix;
                        MyMeshesCompacted[InstanceRenderQue[i].CompactedMeshData] = TempMesh;
                        TargetTransform.hasChanged = false;
                        AABB aabb = InstanceRenderQue[i].InstanceParent.aabb_untransformed;
                        CreateAABB(TargetTransform, ref aabb);
                        MeshAABBs[InstanceRenderQue[i].CompactedMeshData] = aabb;
                    }
                }

                AggNodeCount = 0;
                // UnityEngine.Profiling.Profiler.EndSample();
                #if HardwareRT
                    AccelStruct.Build();
                #endif
                cmd.BeginSample("TLAS Refitting");
                RefitTLAS(MeshAABBs, cmd);
                cmd.EndSample("TLAS Refitting");
            }
            LightMeshData CurLightMesh;
            for (int i = 0; i < LightMeshCount; i++)
            {
                CurLightMesh = LightMeshes[i];
                CurLightMesh.Center = LightTransforms[i].position;
                LightMeshes[i] = CurLightMesh;
            }
            if (!didstart) didstart = true;

            AggNodeCount = 0;

            ChangedLastFrame = ChildrenUpdated;
            return (LightsHaveUpdated ? 2 : 0) + (ChildrenUpdated ? 1 : 0);//The issue is that all light triangle indices start at 0, and thus might not get correctly sorted for indices
        }

        public bool UpdateMaterials()
        {//Allows for live updating of material properties of any object
            bool HasChangedMaterials = false;
            int ParentCount = RenderQue.Count;
            RayTracingObject CurrentMaterial;
            MaterialData TempMat;
            int ChangedMaterialCount = MaterialsChanged.Count;
            int MatCount = _Materials.Length;
            for (int i = 0; i < ChangedMaterialCount; i++)
            {
                CurrentMaterial = MaterialsChanged[i];
                int MaterialCount = (int)Mathf.Min(CurrentMaterial.MaterialIndex.Length, CurrentMaterial.Indexes.Length);
                for (int i3 = 0; i3 < MaterialCount; i3++)
                {
                    int Index = CurrentMaterial.Indexes[i3];
                    if(CurrentMaterial.MaterialIndex[i3] + CurrentMaterial.MatOffset >= MatCount) continue;
                    TempMat = _Materials[CurrentMaterial.MaterialIndex[i3] + CurrentMaterial.MatOffset];
                    TempMat.BaseColor = CurrentMaterial.BaseColor[Index];
                    TempMat.emmissive = CurrentMaterial.emmission[Index];
                    TempMat.Roughness = ((int)CurrentMaterial.MaterialOptions[Index] != 1) ? CurrentMaterial.Roughness[Index] : Mathf.Max(CurrentMaterial.Roughness[Index], 0.000001f);
                    TempMat.TransmittanceColor = CurrentMaterial.TransmissionColor[Index];
                    TempMat.MatType = (int)CurrentMaterial.MaterialOptions[Index];
                    TempMat.EmissionColor = CurrentMaterial.EmissionColor[Index];
                    TempMat.Tag = (uint)(
                        ((CurrentMaterial.ReplaceBase[Index] ? 1 : 0) << 2) | 
                        ((CurrentMaterial.BaseIsMap[Index] ? 1 : 0) << 1) | 
                        ((CurrentMaterial.EmissionMask[Index] ? 0 : 1) << 0));
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
                    TempMat.scatterDistance = CurrentMaterial.ScatterDist[Index];
                    TempMat.IsSmoothness = CurrentMaterial.IsSmoothness[Index] ? 1 : 0;
                    TempMat.AlphaCutoff = CurrentMaterial.AlphaCutoff[Index];
                    TempMat.NormalStrength = CurrentMaterial.NormalStrength[Index];
                    TempMat.MetallicRemap = CurrentMaterial.MetallicRemap[Index];
                    TempMat.RoughnessRemap = CurrentMaterial.RoughnessRemap[Index];
                    if(CurrentMaterial.TilingChanged) {
                        string MatTile = AssetManager.data.Material[AssetManager.ShaderNames.IndexOf(CurrentMaterial.SharedMaterials[Index].shader.name)].BaseColorTex;
                        TempMat.AlbedoTextureScale = new Vector4(CurrentMaterial.SharedMaterials[Index].GetTextureScale(MatTile).x, CurrentMaterial.SharedMaterials[Index].GetTextureScale(MatTile).y, CurrentMaterial.SharedMaterials[Index].GetTextureOffset(MatTile).x, CurrentMaterial.SharedMaterials[Index].GetTextureOffset(MatTile).y);
                    }
                    _Materials[CurrentMaterial.MaterialIndex[i3] + CurrentMaterial.MatOffset] = TempMat;
                    #if HardwareRT
                        var A = CurrentMaterial.gameObject.GetComponent<Renderer>();
                        if(A != null) {
                            if(TempMat.specTrans == 1) AccelStruct.UpdateInstanceMask(A, 0x2);
                            else AccelStruct.UpdateInstanceMask(A, 0x1);
                        } else {
                            if(TempMat.specTrans == 1) AccelStruct.UpdateInstanceMask(CurrentMaterial.gameObject.GetComponent<SkinnedMeshRenderer>(), 0x2);
                            else AccelStruct.UpdateInstanceMask(CurrentMaterial.gameObject.GetComponent<SkinnedMeshRenderer>(), 0x1);
                        }
                    #endif
                    HasChangedMaterials = true;
                }
                CurrentMaterial.TilingChanged = false;
                CurrentMaterial.NeedsToUpdate = false;
            }
            MaterialsChanged.Clear();
            // UnityEngine.Profiling.Profiler.EndSample();
            return HasChangedMaterials;

        }
      

    }
}