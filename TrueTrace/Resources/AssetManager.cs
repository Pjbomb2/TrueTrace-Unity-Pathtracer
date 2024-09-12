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
using Meetem.Bindless;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
#pragma warning disable 4014
namespace TrueTrace {
    [System.Serializable]
    public class AssetManager : MonoBehaviour
    {//This handels all the data

        private BindlessArray bindlessTextures;
        public static AssetManager Assets;
        public int TotalParentObjectSize;
        //emissive, alpha, metallic, roughness
        public int BindlessTextureCount;
        [HideInInspector] public Texture2D IESAtlas;
        [HideInInspector] public Texture2D AlbedoAtlas;
        [HideInInspector] public Texture2D NormalAtlas;
        [HideInInspector] public Texture2D SingleComponentAtlas;
        [HideInInspector] public Texture2D EmissiveAtlas;
        [HideInInspector] public Texture2D AlphaAtlas;
        [HideInInspector] public RenderTexture HeightmapAtlas;
        [HideInInspector] public RenderTexture AlphaMapAtlas;
        private RenderTexture TempTex;
        private RenderTexture s_Prop_EncodeBCn_Temp;
        private ComputeShader CopyShader;
        private ComputeShader Refitter;
        private ComputeShader LightRefitter;
        private int RefitLayer;
        private int NodeUpdater;
        private int NodeCompress;
        private int NodeInitializerKernel;

        private int LightBVHKernel;


        [HideInInspector] public int AlbedoAtlasSize;
        [HideInInspector] public int IESAtlasSize;


        [HideInInspector] public List<RayTracingObject> MaterialsChanged;
        [HideInInspector] public MaterialData[] _Materials;
        [HideInInspector] public ComputeBuffer BVH8AggregatedBuffer;
        [HideInInspector] public ComputeBuffer AggTriBuffer;
        [HideInInspector] public ComputeBuffer LightTriBuffer;
        [HideInInspector] public ComputeBuffer LightNodeBuffer;
        [HideInInspector] public List<MyMeshDataCompacted> MyMeshesCompacted;
        [HideInInspector] public List<LightData> UnityLights;
        [HideInInspector] public InstancedManager InstanceData;
        [HideInInspector] public List<InstancedObject> Instances;
        [HideInInspector] public ComputeBuffer TLASCWBVHIndexes;

        [HideInInspector] public List<TerrainObject> Terrains;
        [HideInInspector] public List<TerrainDat> TerrainInfos;
        [HideInInspector] public ComputeBuffer TerrainBuffer;
        [HideInInspector] public bool DoHeightmap;

        [HideInInspector] public ComputeBuffer MaterialBuffer;
        [HideInInspector] public ComputeBuffer MeshDataBuffer;
        [HideInInspector] public ComputeBuffer LightMeshBuffer;
        [HideInInspector] public ComputeBuffer UnityLightBuffer;

        #if HardwareRT
            private ComputeBuffer MeshIndexOffsets;
            private ComputeBuffer SubMeshOffsetsBuffer;
        #endif

        public void SetMeshTraceBuffers(ComputeShader ThisShader, int Kernel) {
            ThisShader.SetComputeBuffer(Kernel, "TLASBVH8Indices", TLASCWBVHIndexes);
            ThisShader.SetComputeBuffer(Kernel, "AggTris", AggTriBuffer);
            ThisShader.SetComputeBuffer(Kernel, "cwbvh_nodes", BVH8AggregatedBuffer);
            ThisShader.SetComputeBuffer(Kernel, "_MeshData", MeshDataBuffer);
            ThisShader.SetComputeBuffer(Kernel, "_Materials", MaterialBuffer);
            ThisShader.SetTexture(Kernel, "_AlphaAtlas", AlphaAtlas);
            ThisShader.SetTexture(Kernel, "_IESAtlas", IESAtlas);
            ThisShader.SetTexture(Kernel, "_TextureAtlas", AlbedoAtlas);
            //BINDLESS-TEST Assign the albedo array here
            #if HardwareRT
                ThisShader.SetRayTracingAccelerationStructure(Kernel, "myAccelerationStructure", AccelStruct);
                ThisShader.SetBuffer(Kernel, "MeshOffsets", MeshIndexOffsets);
                ThisShader.SetBuffer(Kernel, "SubMeshOffsets", SubMeshOffsetsBuffer);
            #endif
        }

        public void SetHeightmapTraceBuffers(ComputeShader ThisShader, int Kernel) {
            ThisShader.SetComputeBuffer(Kernel, "Terrains", TerrainBuffer);
            ThisShader.SetComputeBuffer(Kernel, "_Materials", MaterialBuffer);
            ThisShader.SetTexture(Kernel, "Heightmap", HeightmapAtlas);
            ThisShader.SetTexture(Kernel, "TerrainAlphaMap", AlphaMapAtlas);
        }

        public void SetLightData(ComputeShader ThisShader, int Kernel) {
            ThisShader.SetComputeBuffer(Kernel, "_UnityLights", UnityLightBuffer);
            ThisShader.SetComputeBuffer(Kernel, "LightNodes", LightNodeBuffer);
            ThisShader.SetComputeBuffer(Kernel, "LightTriangles", LightTriBuffer);
            ThisShader.SetComputeBuffer(Kernel, "_LightMeshes", LightMeshBuffer);
            ThisShader.SetTexture(Kernel, "Heightmap", HeightmapAtlas);
        }

        private ComputeShader MeshFunctions;
        private int TriangleBufferKernel;
        private int NodeBufferKernel;
        private int LightBufferKernel;
        private int LightNodeBufferKernel;

        [HideInInspector] public List<Transform> RenderTransforms;
        [HideInInspector] public List<ParentObject> RenderQue;
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
        [HideInInspector] public LightBounds[] LightAABBs;

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

            DestroyImmediate(IESAtlas);
            DestroyImmediate(AlbedoAtlas);
            DestroyImmediate(NormalAtlas);
            DestroyImmediate(SingleComponentAtlas);
            DestroyImmediate(AlphaAtlas);
            DestroyImmediate(EmissiveAtlas);
            DestroyImmediate(HeightmapAtlas);
            DestroyImmediate(AlphaMapAtlas);

            TerrainBuffer.ReleaseSafe();
            LightTriBuffer.ReleaseSafe();
            LightNodeBuffer.ReleaseSafe();
            BVH8AggregatedBuffer.ReleaseSafe();
            AggTriBuffer.ReleaseSafe();

            MaterialBuffer.ReleaseSafe();
            MeshDataBuffer.ReleaseSafe();
            LightMeshBuffer.ReleaseSafe();
            UnityLightBuffer.ReleaseSafe();

            TLASCWBVHIndexes.ReleaseSafe();

            #if HardwareRT
                MeshIndexOffsets?.Release();
                SubMeshOffsetsBuffer?.Release();
            #endif

            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

    Vector2Int PackRect(Vector4 ThisRect) {
        int A = (int)Mathf.Ceil(ThisRect.x * 16384.0f) | ((int)Mathf.Ceil(ThisRect.y * 16384.0f) << 15);
        int B = (int)Mathf.Ceil(ThisRect.z * 16384.0f) | ((int)Mathf.Ceil(ThisRect.w * 16384.0f) << 15);
        return new Vector2Int(A, B);
    }
    private void PackAndCompactBindless(Dictionary<int, TexObj> DictTex, PackingRectangle[] Rects, int TexIndex, int ReadIndex = -1) {
        int TexCount = DictTex.Count;
        if(TexCount != 0) {
            for (int i = 0; i < TexCount; i++) {
                PackingRectangle TempRect = Rects[i];
                int ID = TempRect.Id;
                TexObj SelectedTex = DictTex[ID];
                int ListLength = SelectedTex.TexObjList.Count;
                var bindlessIdx = bindlessTextures.AppendRaw(SelectedTex.Tex);

                for(int j = 0; j < ListLength; j++) {
                        Vector2Int VectoredTexIndex = new Vector2Int(BindlessTextureCount + 1, SelectedTex.TexObjList[j].z);
                        switch (SelectedTex.TexObjList[j].y) {
                            case 0: _Materials[SelectedTex.TexObjList[j].x].AlbedoTex = VectoredTexIndex; break;
                            case 1: _Materials[SelectedTex.TexObjList[j].x].NormalTex = VectoredTexIndex; break;
                            case 2: _Materials[SelectedTex.TexObjList[j].x].EmissiveTex = VectoredTexIndex; break;
                            case 3: _Materials[SelectedTex.TexObjList[j].x].AlphaTex = VectoredTexIndex; break;
                            case 4: _Materials[SelectedTex.TexObjList[j].x].MetallicTex = VectoredTexIndex; break;
                            case 5: _Materials[SelectedTex.TexObjList[j].x].RoughnessTex = VectoredTexIndex; break;
                            case 6: _Materials[SelectedTex.TexObjList[j].x].MatCapMask = VectoredTexIndex; break;
                            case 7: _Materials[SelectedTex.TexObjList[j].x].MatCapTex = VectoredTexIndex; break;
                            default: break;
                        }
                }
                BindlessTextureCount++;
                if(BindlessTextureCount > 2046) {
                    Debug.LogError("TOO MANY TEXTURES, REPORT BACK TO DEVELOPER");
                    return;
                }
            }
        }

    }


    private void PackAndCompact(Dictionary<int, TexObj> DictTex, ref RenderTexture Atlas, PackingRectangle[] Rects, int DesiredRes, int TexIndex, int ReadIndex = -1) {
        Vector2 Scale = new Vector2(1,1);
        int TexCount = DictTex.Count;
        if(TexCount != 0) {
            PackingRectangle BoundRects;
            RectanglePacker.Pack(Rects, out BoundRects);
            DesiredRes = (int)Mathf.Min((Mathf.Floor((float)Mathf.Max(BoundRects.Width, BoundRects.Height) / 4.0f)) * 4, DesiredRes);
            Scale = new Vector2((Mathf.Min((float)DesiredRes / BoundRects.Width, 1) * 16384.0f / 4.0f) * 4.0f / 16384.0f, (Mathf.Min((float)DesiredRes / BoundRects.Height, 1) * 16384.0f / 4.0f) * 4.0f / 16384.0f);
        } else {
            DesiredRes = 4;
        }
        if(Atlas == null || Atlas.width != DesiredRes) {
            if(Atlas != null) Atlas.ReleaseSafe();
            int tempWidth = (DesiredRes + 3) / 4;
            int tempHeight = (DesiredRes + 3) / 4;
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
            switch(TexIndex) {
                case 0://heightmap
                    CreateRenderTexture(ref Atlas, DesiredRes, DesiredRes, RenderTextureFormat.RFloat, false);
                break;
                case 1://alphamap
                    CreateRenderTexture(ref Atlas, DesiredRes, DesiredRes, RenderTextureFormat.ARGBHalf, false);
                break;
                case 2://normalmap
                    Atlas = new RenderTexture(desc);

                    if(NormalAtlas != null && NormalAtlas.width != DesiredRes) {
                        DestroyImmediate(NormalAtlas);
                        NormalAtlas = new Texture2D(DesiredRes,DesiredRes, TextureFormat.BC5, 1, false);
                    }
                break;
                case 3://metallicmap
                case 4://roughnessmap
                    desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32_SInt;
                    Atlas = new RenderTexture(desc);

                    if(SingleComponentAtlas != null && SingleComponentAtlas.width != DesiredRes) {
                        DestroyImmediate(SingleComponentAtlas);
                        SingleComponentAtlas = new Texture2D(DesiredRes,DesiredRes, TextureFormat.BC4, 1, false);
                    }
                break;
                case 7://AlphaMap
                    desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32_SInt;
                    Atlas = new RenderTexture(desc);
                    if(AlphaAtlas != null && AlphaAtlas.width != DesiredRes) {
                        DestroyImmediate(AlphaAtlas);
                        AlphaAtlas = new Texture2D(DesiredRes,DesiredRes, TextureFormat.BC4, 1, false);
                    }
                break;
                case 5://EmissionMap
                    Atlas = new RenderTexture(desc);
                    if(EmissiveAtlas != null && EmissiveAtlas.width != DesiredRes) {
                        DestroyImmediate(EmissiveAtlas);
                        EmissiveAtlas = new Texture2D(DesiredRes,DesiredRes, TextureFormat.BC6H, false);
                    }
                break;
                case 6://AlbedoMap
                    Atlas = new RenderTexture(desc);
                    AlbedoAtlasSize = DesiredRes;
                    if(AlbedoAtlas != null && AlbedoAtlas.width != DesiredRes) {
                        DestroyImmediate(AlbedoAtlas);
                        AlbedoAtlas = new Texture2D(DesiredRes,DesiredRes, TextureFormat.BC6H, false);
                    }
                break;
                case 8://IESMap
                    desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32_SInt;
                    Atlas = new RenderTexture(desc);
                    IESAtlasSize = DesiredRes;
                    if(IESAtlas != null && IESAtlas.width != DesiredRes) {
                        DestroyImmediate(IESAtlas);
                        IESAtlas = new Texture2D(DesiredRes,DesiredRes, TextureFormat.BC4, 1, false);
                    }
                break;
            }
        }
        if(TexCount == 0) return;

        CopyShader.SetBool("ForceLossless", false);
        CopyShader.SetBool("IsHeightmap", TexIndex == 0);
        for (int i = 0; i < TexCount; i++) {
                PackingRectangle TempRect = Rects[i];
                int ID = TempRect.Id;
                CopyShader.SetVector("InputSize", new Vector2(Mathf.Ceil((TempRect.Width) * Scale.x / 4.0f) * 4.0f, Mathf.Ceil((TempRect.Height) * Scale.y / 4.0f) * 4.0f));
                CopyShader.SetVector("Offset", new Vector2(Mathf.Ceil(TempRect.X * Scale.x / 4.0f) * 4.0f, Mathf.Ceil(TempRect.Y * Scale.y / 4.0f) * 4.0f));
                TexObj SelectedTex = DictTex[ID];
                int ListLength = SelectedTex.TexObjList.Count;
                Vector4 RectSelect = new Vector4(0, 0, (Mathf.Ceil(TempRect.X * Scale.x / 4.0f) * 4.0f) / DesiredRes, (Mathf.Ceil(TempRect.Y * Scale.y / 4.0f) * 4.0f) / DesiredRes);
                RectSelect.x = (Mathf.Ceil((TempRect.X * Scale.x + (TempRect.Width) * Scale.x) / 4.0f) * 4.0f) / DesiredRes;
                RectSelect.y = (Mathf.Ceil((TempRect.Y * Scale.y + (TempRect.Height) * Scale.y) / 4.0f) * 4.0f) / DesiredRes;

                if(TexIndex >= 2) {
                    for(int j = 0; j < ListLength; j++) {
                        switch (TexIndex) {
                            case 2: 
                                _Materials[SelectedTex.TexObjList[j].x].NormalTex = PackRect(RectSelect); 
                            break;
                            case 4: 
                                if(TempRect.TexType == 4) _Materials[SelectedTex.TexObjList[j].x].MetallicTex = PackRect(RectSelect); 
                                else if(TempRect.TexType == 5)  _Materials[SelectedTex.TexObjList[j].x].RoughnessTex = PackRect(RectSelect);
                                else if(TempRect.TexType == 6)  _Materials[SelectedTex.TexObjList[j].x].MatCapMask = PackRect(RectSelect);
                            break;
                            case 5: 
                                _Materials[SelectedTex.TexObjList[j].x].EmissiveTex = PackRect(RectSelect); 
                            break;
                            case 6: 
                                if(TempRect.TexType == 0) _Materials[SelectedTex.TexObjList[j].x].AlbedoTex = PackRect(RectSelect); 
                                else if(TempRect.TexType == 7) _Materials[SelectedTex.TexObjList[j].x].MatCapTex = PackRect(RectSelect); 
                            break;
                            case 7:
                                _Materials[SelectedTex.TexObjList[j].x].AlphaTex = PackRect(RectSelect);
                            break;
                            case 8: 
                                LightData TempLight = UnityLights[SelectedTex.TexObjList[j].x];
                                TempLight.IESTex = PackRect(RectSelect); 
                                RayTracingMaster._rayTracingLights[SelectedTex.TexObjList[j].x].ThisLightData.IESTex = PackRect(RectSelect); 
                                UnityLights[SelectedTex.TexObjList[j].x] = TempLight;
                                break;
                            default: break;
                        }
                    }
                } else if(TexIndex < 2) {
                    TerrainDat TempTerrain = TerrainInfos[SelectedTex.TexObjList[0].x];
                    if(TexIndex == 0) TempTerrain.HeightMap = RectSelect;
                    else TempTerrain.AlphaMap = RectSelect;
                    TerrainInfos[SelectedTex.TexObjList[0].x] = TempTerrain;
                }
                    Vector2 RCPSize = new Vector2(1.0f / (float)(TempRect.Width2 * Scale.x), 1.0f / (float)(TempRect.Height2 * Scale.y));
                CopyShader.SetInt("OutputRead", ((ReadIndex != -1) ? ReadIndex : SelectedTex.ReadIndex));
                        CopyShader.SetVector("TextureSizeRcp", RCPSize);
                switch(TexIndex) {
                    case 0://heightmap
                        CopyShader.SetTexture(0, "InputTex", SelectedTex.Tex);
                        CopyShader.SetTexture(0, "ResultSingle", Atlas);
                        CopyShader.Dispatch(0, (int)Mathf.CeilToInt(TempRect.Width * Scale.x / 32.0f), (int)Mathf.CeilToInt(TempRect.Height * Scale.y / 32.0f), 1);
                    break;
                    case 3://metallic
                    case 4://roughness
                    case 7://alpha
                    case 8://IES
                        CopyShader.SetTexture(6, "SingleInput", SelectedTex.Tex);
                        CopyShader.SetTexture(6, "SingleOutput", Atlas);
                        CopyShader.Dispatch(6, (int)Mathf.CeilToInt(TempRect.Width * Scale.x / 4.0f), (int)Mathf.CeilToInt(TempRect.Height * Scale.y / 4.0f), 1);
                    break;
                    case 1://alphamap(uncompressed)
                        CopyShader.SetBool("ForceLossless", true);
                        CopyShader.SetTexture(2, "InputTex", SelectedTex.Tex);
                        CopyShader.SetTexture(2, "ResultFull", Atlas);
                        CopyShader.Dispatch(2, (int)Mathf.CeilToInt(TempRect.Width * Scale.x / 32.0f), (int)Mathf.CeilToInt(TempRect.Height * Scale.y / 32.0f), 1);
                    break;
                    case 2:
                        CopyShader.SetTexture(5, "NormSource", SelectedTex.Tex);
                        CopyShader.SetTexture(5, "NormTarget", Atlas);
                        CopyShader.Dispatch(5, (int)Mathf.CeilToInt(TempRect.Width * Scale.x / 4.0f), (int)Mathf.CeilToInt(TempRect.Height * Scale.y / 4.0f), 1);
                    break;  
                    case 5:
                    case 6:
                        CopyShader.SetTexture(4, "_Source", SelectedTex.Tex);

                        CopyShader.SetTexture(4, "_Target", Atlas);
                        CopyShader.Dispatch(4, (int)Mathf.CeilToInt(TempRect.Width * Scale.x / 4.0f), (int)Mathf.CeilToInt(TempRect.Height * Scale.y / 4.0f), 1);
                    break;
                }

            }
        }

        private void CreateRenderTexture(ref RenderTexture ThisTex, int Width, int Height, RenderTextureFormat Form, bool UseMip, RenderTextureReadWrite RendRead = RenderTextureReadWrite.sRGB) {
            ThisTex = new RenderTexture(Width, Height, 0, Form, RendRead);
            ThisTex.enableRandomWrite = true;
            if(UseMip) {
                ThisTex.useMipMap = true;
                ThisTex.autoGenerateMips = false;
            } else ThisTex.useMipMap = false;
            ThisTex.Create();
        }

        private void KeyCheck(int MatIndex, Texture Tex, ref Dictionary<int, TexObj> DictTextures, ref List<PackingRectangle> PR, int ReadChannelIndex, int TexType) {
            #if !DX11Only && !UseAtlas
                int index = Tex.GetInstanceID();
            #else
                int index = System.HashCode.Combine(Tex.GetInstanceID(), TexType);
            #endif
            if (DictTextures.TryGetValue(index, out var existingTexObj)) {
                existingTexObj.TexObjList.Add(new Vector3Int(MatIndex, TexType, ReadChannelIndex));
            } else {
                var newTexObj = new TexObj {
                    Tex = Tex,
                    ReadIndex = ReadChannelIndex
                };

                PR.Add(new PackingRectangle {
                    Width = (uint)Mathf.Ceil(((float)Tex.width) / 4.0f) * 4,
                    Height = (uint)Mathf.Ceil(((float)Tex.height) / 4.0f) * 4,
                    Width2 = (uint)(Tex.width),
                    Height2 = (uint)(Tex.height),
                    Id = index,
                    TexType = TexType
                });

                newTexObj.TexObjList.Add(new Vector3Int(MatIndex, TexType, ReadChannelIndex));
                DictTextures.Add(index, newTexObj);
            }
        }

        private void CreateAtlasIES() {//Creates texture atlas
            Dictionary<int, TexObj> IESMapTextures = new Dictionary<int, TexObj>();
            List<PackingRectangle> IESMapRect = new List<PackingRectangle>();
            if (CopyShader == null) CopyShader = Resources.Load<ComputeShader>("Utility/CopyTextureShader");
            if(RenderQue.Count == 0) return;

            int IESLightCount = 0;
            foreach (RayTracingLights Obj in RayTracingMaster._rayTracingLights) {
                if(Obj.ThisLight.type == LightType.Spot && Obj.IESProfile != null) KeyCheck(IESLightCount, Obj.IESProfile, ref IESMapTextures, ref IESMapRect, 0, 8);
                IESLightCount++;
            }
            PackAndCompact(IESMapTextures, ref TempTex, IESMapRect.ToArray(), MainDesiredRes, 8);
            Graphics.CopyTexture(TempTex, 0, IESAtlas, 0);
            TempTex.Release();
            TempTex = null;
            IESMapTextures.Clear();
            IESMapTextures.TrimExcess();
            IESMapRect.TrimExcess();
            IESMapRect.Clear();
        }

        private void CreateAtlas(int TotalMatCount, CommandBuffer cmd) {//Creates texture atlas
            TotalMatCount = 0;
            foreach (ParentObject Obj in RenderQue) {
                MaterialsChanged.AddRange(Obj.ChildObjects);
                TotalMatCount += Obj._Materials.Count;
            }
            foreach (ParentObject Obj in InstanceData.RenderQue) {
                TotalMatCount += Obj._Materials.Count;
            }
            int TerrainCount = Terrains.Count;
             if (TerrainCount != 0) {
                for (int j = 0; j < TerrainCount; j++) {
                    TotalMatCount += Terrains[j].Materials.Count;
               }
            }
            BindlessTextureCount = 0;
            Dictionary<int, TexObj> HeightMapTextures = new Dictionary<int, TexObj>();
            Dictionary<int, TexObj> AlphaMapTextures = new Dictionary<int, TexObj>();
            List<PackingRectangle> HeightMapRect = new List<PackingRectangle>();
            List<PackingRectangle> AlphaMapRect = new List<PackingRectangle>();
            #if !DX11Only && !UseAtlas
                if(bindlessTextures == null) bindlessTextures = new BindlessArray();
                bindlessTextures.Clear();
                Dictionary<int, TexObj> BindlessDict = new Dictionary<int, TexObj>();
                List<PackingRectangle> BindlessRect = new List<PackingRectangle>();
            #else
                Dictionary<int, TexObj> AlbTextures = new Dictionary<int, TexObj>();
                Dictionary<int, TexObj> NormTextures = new Dictionary<int, TexObj>();
                Dictionary<int, TexObj> EmisTextures = new Dictionary<int, TexObj>();
                Dictionary<int, TexObj> SingleComponentTexture = new Dictionary<int, TexObj>();
                Dictionary<int, TexObj> AlphTextures = new Dictionary<int, TexObj>();
                List<PackingRectangle> AlbRect = new List<PackingRectangle>();
                List<PackingRectangle> NormRect = new List<PackingRectangle>();
                List<PackingRectangle> EmisRect = new List<PackingRectangle>();
                List<PackingRectangle> SingleComponentRect = new List<PackingRectangle>();
                List<PackingRectangle> AlphRect = new List<PackingRectangle>();
            #endif
            _Materials = new MaterialData[TotalMatCount];

            MatCount = 0;
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
                    #if !DX11Only && !UseAtlas
                        if(TempMat.AlbedoTex.x != 0) KeyCheck(MatCount, Obj.AlbedoTexs[(int)TempMat.AlbedoTex.x-1], ref BindlessDict, ref BindlessRect, 4, 0);
                        if(TempMat.NormalTex.x != 0) KeyCheck(MatCount, Obj.NormalTexs[(int)TempMat.NormalTex.x-1], ref BindlessDict, ref BindlessRect, 4, 1);
                        if(TempMat.EmissiveTex.x != 0) KeyCheck(MatCount, Obj.EmissionTexs[(int)TempMat.EmissiveTex.x-1], ref BindlessDict, ref BindlessRect, 4, 2);
                        if(TempMat.AlphaTex.x != 0) KeyCheck(MatCount, Obj.AlphaTexs[(int)TempMat.AlphaTex.x-1], ref BindlessDict, ref BindlessRect, Obj.AlphaTexChannelIndex[(int)TempMat.AlphaTex.x-1], 3);
                        if(TempMat.MetallicTex.x != 0) KeyCheck(MatCount, Obj.MetallicTexs[(int)TempMat.MetallicTex.x-1], ref BindlessDict, ref BindlessRect, Obj.MetallicTexChannelIndex[(int)TempMat.MetallicTex.x-1], 4);
                        if(TempMat.RoughnessTex.x != 0) KeyCheck(MatCount, Obj.RoughnessTexs[(int)TempMat.RoughnessTex.x-1], ref BindlessDict, ref BindlessRect, Obj.RoughnessTexChannelIndex[(int)TempMat.RoughnessTex.x-1], 5);
                        if(TempMat.MatCapMask.x != 0) KeyCheck(MatCount, Obj.MatCapMasks[(int)TempMat.MatCapMask.x-1], ref BindlessDict, ref BindlessRect, Obj.MatCapMaskChannelIndex[(int)TempMat.MatCapMask.x-1], 6);
                        if(TempMat.MatCapTex.x != 0) KeyCheck(MatCount, Obj.MatCapTexs[(int)TempMat.MatCapTex.x-1], ref BindlessDict, ref BindlessRect, 4, 7);
                    #else
                        if(TempMat.AlbedoTex.x != 0) KeyCheck(MatCount, Obj.AlbedoTexs[(int)TempMat.AlbedoTex.x-1], ref AlbTextures, ref AlbRect, 0, 0);
                        if(TempMat.NormalTex.x != 0) KeyCheck(MatCount, Obj.NormalTexs[(int)TempMat.NormalTex.x-1], ref NormTextures, ref NormRect, 0, 1);
                        if(TempMat.EmissiveTex.x != 0) KeyCheck(MatCount, Obj.EmissionTexs[(int)TempMat.EmissiveTex.x-1], ref EmisTextures, ref EmisRect, 0, 2);
                        if(TempMat.AlphaTex.x != 0) KeyCheck(MatCount, Obj.AlphaTexs[(int)TempMat.AlphaTex.x-1], ref AlphTextures, ref AlphRect, Obj.AlphaTexChannelIndex[(int)TempMat.AlphaTex.x-1], 3);
                        if(TempMat.MetallicTex.x != 0) KeyCheck(MatCount, Obj.MetallicTexs[(int)TempMat.MetallicTex.x-1], ref SingleComponentTexture, ref SingleComponentRect, Obj.MetallicTexChannelIndex[(int)TempMat.MetallicTex.x-1], 4);
                        if(TempMat.RoughnessTex.x != 0) KeyCheck(MatCount, Obj.RoughnessTexs[(int)TempMat.RoughnessTex.x-1], ref SingleComponentTexture, ref SingleComponentRect, Obj.RoughnessTexChannelIndex[(int)TempMat.RoughnessTex.x-1], 5);
                        if(TempMat.MatCapMask.x != 0) KeyCheck(MatCount, Obj.MatCapMasks[(int)TempMat.MatCapMask.x-1], ref SingleComponentTexture, ref SingleComponentRect, Obj.MatCapMaskChannelIndex[(int)TempMat.MatCapMask.x-1], 6);
                        if(TempMat.MatCapTex.x != 0) KeyCheck(MatCount, Obj.MatCapTexs[(int)TempMat.MatCapTex.x-1], ref AlbTextures, ref AlbRect, 0, 7);
                    #endif
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
                    #if !DX11Only && !UseAtlas
                        if(TempMat.AlbedoTex.x != 0) KeyCheck(MatCount, Obj.AlbedoTexs[(int)TempMat.AlbedoTex.x-1], ref BindlessDict, ref BindlessRect, 4, 0);
                        if(TempMat.NormalTex.x != 0) KeyCheck(MatCount, Obj.NormalTexs[(int)TempMat.NormalTex.x-1], ref BindlessDict, ref BindlessRect, 4, 1);
                        if(TempMat.EmissiveTex.x != 0) KeyCheck(MatCount, Obj.EmissionTexs[(int)TempMat.EmissiveTex.x-1], ref BindlessDict, ref BindlessRect, 4, 2);
                        if(TempMat.AlphaTex.x != 0) KeyCheck(MatCount, Obj.AlphaTexs[(int)TempMat.AlphaTex.x-1], ref BindlessDict, ref BindlessRect, Obj.AlphaTexChannelIndex[(int)TempMat.AlphaTex.x-1], 3);
                        if(TempMat.MetallicTex.x != 0) KeyCheck(MatCount, Obj.MetallicTexs[(int)TempMat.MetallicTex.x-1], ref BindlessDict, ref BindlessRect, Obj.MetallicTexChannelIndex[(int)TempMat.MetallicTex.x-1], 4);
                        if(TempMat.RoughnessTex.x != 0) KeyCheck(MatCount, Obj.RoughnessTexs[(int)TempMat.RoughnessTex.x-1], ref BindlessDict, ref BindlessRect, Obj.RoughnessTexChannelIndex[(int)TempMat.RoughnessTex.x-1], 5);
                        if(TempMat.MatCapMask.x != 0) KeyCheck(MatCount, Obj.MatCapMasks[(int)TempMat.MatCapMask.x-1], ref BindlessDict, ref BindlessRect, Obj.MatCapMaskChannelIndex[(int)TempMat.MatCapMask.x-1], 6);
                        if(TempMat.MatCapTex.x != 0) KeyCheck(MatCount, Obj.MatCapTexs[(int)TempMat.MatCapTex.x-1], ref BindlessDict, ref BindlessRect, 4, 7);
                    #else
                        if(TempMat.AlbedoTex.x != 0) KeyCheck(MatCount, Obj.AlbedoTexs[(int)TempMat.AlbedoTex.x-1], ref AlbTextures, ref AlbRect, 0, 0);
                        if(TempMat.NormalTex.x != 0) KeyCheck(MatCount, Obj.NormalTexs[(int)TempMat.NormalTex.x-1], ref NormTextures, ref NormRect, 0, 1);
                        if(TempMat.EmissiveTex.x != 0) KeyCheck(MatCount, Obj.EmissionTexs[(int)TempMat.EmissiveTex.x-1], ref EmisTextures, ref EmisRect, 0, 2);
                        if(TempMat.AlphaTex.x != 0) KeyCheck(MatCount, Obj.AlphaTexs[(int)TempMat.AlphaTex.x-1], ref AlphTextures, ref AlphRect, Obj.AlphaTexChannelIndex[(int)TempMat.AlphaTex.x-1], 3);
                        if(TempMat.MetallicTex.x != 0) KeyCheck(MatCount, Obj.MetallicTexs[(int)TempMat.MetallicTex.x-1], ref SingleComponentTexture, ref SingleComponentRect, Obj.MetallicTexChannelIndex[(int)TempMat.MetallicTex.x-1], 4);
                        if(TempMat.RoughnessTex.x != 0) KeyCheck(MatCount, Obj.RoughnessTexs[(int)TempMat.RoughnessTex.x-1], ref SingleComponentTexture, ref SingleComponentRect, Obj.RoughnessTexChannelIndex[(int)TempMat.RoughnessTex.x-1], 5);
                        if(TempMat.MatCapMask.x != 0) KeyCheck(MatCount, Obj.MatCapMasks[(int)TempMat.MatCapMask.x-1], ref SingleComponentTexture, ref SingleComponentRect, Obj.MatCapMaskChannelIndex[(int)TempMat.MatCapMask.x-1], 6);
                        if(TempMat.MatCapTex.x != 0) KeyCheck(MatCount, Obj.MatCapTexs[(int)TempMat.MatCapTex.x-1], ref AlbTextures, ref AlbRect, 0, 7);
                    #endif
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
                    TempTerrain.TerrainDimX = Terrains[j].TerrainDimX;
                    TempTerrain.TerrainDimY = Terrains[j].TerrainDimY;
                    TempTerrain.HeightScale = Terrains[j].HeightScale;
                    TempTerrain.MatOffset = TerrainMatCount - MatCount;
                    int Index = Obj2.HeightMap.GetInstanceID();
                    var E = new TexObj();
                    {
                        HeightMapRect.Add(new PackingRectangle() {
                            Width = (uint)Mathf.Ceil(((float)Obj2.HeightMap.width) / 4.0f) * 4,
                            Height = (uint)Mathf.Ceil(((float)Obj2.HeightMap.height) / 4.0f) * 4,
                            Width2 = (uint)Obj2.HeightMap.width,
                            Height2 = (uint)Obj2.HeightMap.height,
                            Id = Index,
                            TexType = 99999
                        });
                        E.Tex = Obj2.HeightMap;
                        E.ReadIndex = 0;
                        E.TexObjList.Add(new Vector3Int(j, 9, 0));
                        HeightMapTextures.Add(Index, E);
                    }
                    E = new TexObj();
                    Index = Obj2.AlphaMap.GetInstanceID();
                    {
                        AlphaMapRect.Add(new PackingRectangle() {
                            Width = (uint)Mathf.Ceil(((float)Obj2.AlphaMap.width) / 4.0f) * 4,
                            Height = (uint)Mathf.Ceil(((float)Obj2.AlphaMap.height) / 4.0f) * 4,
                            Width2 = (uint)Obj2.AlphaMap.width,
                            Height2 = (uint)Obj2.AlphaMap.height,
                            Id = Index,
                            TexType = 9999
                        });
                        E.Tex = Obj2.AlphaMap;
                        E.ReadIndex = 0;
                        E.TexObjList.Add(new Vector3Int(j, 9, 0));
                        AlphaMapTextures.Add(Index, E);
                    }

                    int ThisMatCount = Obj2.Materials.Count;
                    for (int i = 0; i < ThisMatCount; i++) {
                        MaterialData TempMat = Obj2.Materials[i];
                        #if !DX11Only && !UseAtlas
                            if(TempMat.AlbedoTex.x != 0) KeyCheck(TerrainMatCount + i, Obj2.AlbedoTexs[(int)TempMat.AlbedoTex.x-1], ref BindlessDict, ref BindlessRect, 4, 0);
                            if(TempMat.NormalTex.x != 0) KeyCheck(TerrainMatCount + i, Obj2.NormalTexs[(int)TempMat.NormalTex.x-1], ref BindlessDict, ref BindlessRect, 4, 1);
                            if(TempMat.MetallicTex.x != 0) KeyCheck(TerrainMatCount + i, Obj2.MaskTexs[(int)TempMat.MetallicTex.x-1], ref BindlessDict, ref BindlessRect, 2, 4);
                            if(TempMat.RoughnessTex.x != 0) KeyCheck(TerrainMatCount + i, Obj2.MaskTexs[(int)TempMat.RoughnessTex.x-1], ref BindlessDict, ref BindlessRect, 1, 5);
                        #else
                            if(TempMat.AlbedoTex.x != 0) KeyCheck(TerrainMatCount + i, Obj2.AlbedoTexs[(int)TempMat.AlbedoTex.x-1], ref AlbTextures, ref AlbRect, 0, 0);
                            if(TempMat.NormalTex.x != 0) KeyCheck(TerrainMatCount + i, Obj2.NormalTexs[(int)TempMat.NormalTex.x-1], ref NormTextures, ref NormRect, 0, 1);
                            if(TempMat.MetallicTex.x != 0) KeyCheck(TerrainMatCount + i, Obj2.MaskTexs[(int)TempMat.MetallicTex.x-1], ref SingleComponentTexture, ref SingleComponentRect, 2, 4);
                            if(TempMat.RoughnessTex.x != 0) KeyCheck(TerrainMatCount + i, Obj2.MaskTexs[(int)TempMat.RoughnessTex.x-1], ref SingleComponentTexture, ref SingleComponentRect, 1, 5);
                        #endif

                        _Materials[TerrainMatCount + i] = TempMat;
                    }
                    TerrainMatCount += ThisMatCount;
                    TerrainInfos.Add(TempTerrain);
                }
            }


            if (!RenderQue.Any())
                return;

            #if !DX11Only && !UseAtlas
                PackAndCompactBindless(BindlessDict, BindlessRect.ToArray(), 6);
            #else
                PackAndCompact(AlbTextures, ref TempTex, AlbRect.ToArray(), MainDesiredRes, 6, 3);
                Graphics.CopyTexture(TempTex, 0, AlbedoAtlas, 0);
                TempTex.Release();
                TempTex = null;


                PackAndCompact(NormTextures, ref TempTex, NormRect.ToArray(), MainDesiredRes, 2);
                Graphics.CopyTexture(TempTex, 0, NormalAtlas, 0);
                TempTex.Release();
                TempTex = null;

                PackAndCompact(SingleComponentTexture, ref TempTex, SingleComponentRect.ToArray(), MainDesiredRes, 4);
                Graphics.CopyTexture(TempTex, 0, SingleComponentAtlas, 0);
                TempTex.Release();
                TempTex = null;

                PackAndCompact(EmisTextures, ref TempTex, EmisRect.ToArray(), MainDesiredRes, 5);
                Graphics.CopyTexture(TempTex, 0, EmissiveAtlas, 0);
                TempTex.Release();
                TempTex = null;

                PackAndCompact(AlphTextures, ref TempTex, AlphRect.ToArray(), MainDesiredRes, 7);
                Graphics.CopyTexture(TempTex, 0, AlphaAtlas, 0);
                TempTex.Release();
                TempTex = null;
            #endif

            PackAndCompact(HeightMapTextures, ref HeightmapAtlas, HeightMapRect.ToArray(), 16384, 0, 0);
            PackAndCompact(AlphaMapTextures, ref AlphaMapAtlas, AlphaMapRect.ToArray(), 16384, 1);
            
            #if !DX11Only && !UseAtlas
                BindlessDict.Clear();
                BindlessDict.TrimExcess();
                BindlessRect.Clear();
                BindlessRect.TrimExcess();
            #else
                AlbTextures.Clear();
                AlbTextures.TrimExcess();
                NormTextures.Clear();
                NormTextures.TrimExcess();
                EmisTextures.Clear();
                EmisTextures.TrimExcess();
                SingleComponentTexture.Clear();
                SingleComponentTexture.TrimExcess();
                AlphTextures.Clear();
                AlphTextures.TrimExcess();

                AlbRect.Clear();
                AlbRect.TrimExcess();
                NormRect.Clear();
                NormRect.TrimExcess();
                EmisRect.Clear();
                EmisRect.TrimExcess();
                SingleComponentRect.Clear();
                SingleComponentRect.TrimExcess();
                AlphRect.Clear();
                AlphRect.TrimExcess();
            #endif

            HeightMapTextures.Clear();
            HeightMapTextures.TrimExcess();
            AlphaMapTextures.Clear();
            AlphaMapTextures.TrimExcess();

            HeightMapRect.Clear();
            HeightMapRect.TrimExcess();
            AlphaMapRect.Clear();
            AlphaMapRect.TrimExcess();
            if (TerrainBuffer != null) TerrainBuffer.Release();
            if (TerrainCount != 0) {
                TerrainBuffer = new ComputeBuffer(TerrainCount, 60);
                TerrainBuffer.SetData(TerrainInfos);
            } else {
                TerrainBuffer = new ComputeBuffer(1, 60);
            }

        }
        public void Awake() {
            Assets = this;
            bindlessTextures = new BindlessArray();
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
            // CreateAtlas(throwaway);
        }

        public void OnApplicationQuit() {
            RunningTasks = 0;
            #if HardwareRT
                AccelStruct.Release();
            #endif

            LightTriBuffer.ReleaseSafe();
            LightNodeBuffer.ReleaseSafe();
            BVH8AggregatedBuffer.ReleaseSafe();
            AggTriBuffer.ReleaseSafe();

            if(WorkingBuffer != null) for(int i = 0; i < WorkingBuffer.Length; i++) WorkingBuffer[i]?.Release();
            NodeBuffer.ReleaseSafe();
            StackBuffer.ReleaseSafe();
            ToBVHIndexBuffer.ReleaseSafe();
            BVHDataBuffer.ReleaseSafe();
            BoxesBuffer.ReleaseSafe();
            TerrainBuffer.ReleaseSafe();
            TLASCWBVHIndexes.ReleaseSafe();
            TerrainBuffer.ReleaseSafe();
        }

        void OnDisable() {
            ClearAll();
            bindlessTextures?.Dispose();
            bindlessTextures = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }


        private TextAsset XMLObject;
        [HideInInspector] public static List<string> ShaderNames;
        [HideInInspector] public static Materials data = new Materials();
        [HideInInspector] public bool NeedsToUpdateXML;
        public void AddMaterial(Shader shader) {
            int ShaderPropertyCount = shader.GetPropertyCount();
            List<TexturePairs> TexturePairList = new List<TexturePairs>();
            TexturePairList.Add(new TexturePairs() {
                Purpose = (int)TexturePurpose.Albedo,
                ReadIndex = -4,
                TextureName = "_MainTex"
            });
            TexturePairList.Add(new TexturePairs() {
                Purpose = (int)TexturePurpose.Alpha,
                ReadIndex = 3,
                TextureName = "_MainTex"
            });
            data.Material.Add(new MaterialShader() {
                Name = shader.name,
                AvailableTextures = TexturePairList,
                MetallicRange = "null",
                RoughnessRange = "null",
                IsGlass = false,
                IsCutout = false,
                UsesSmoothness = false,
                BaseColorValue = "null",
                MetallicRemapMin = "null",
                MetallicRemapMax = "null",
                RoughnessRemapMin = "null",
                RoughnessRemapMax = "null"
            });
            ShaderNames.Add(shader.name);
            NeedsToUpdateXML = true;
        }
        public void UpdateMaterialDefinition() {
            XMLObject = Resources.Load<TextAsset>("Utility/MaterialMappings");
            #if UNITY_EDITOR
                if(XMLObject == null) {
                    Debug.LogError("Missing Material Mappings XML");
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
            if(AlbedoAtlas == null) AlbedoAtlas = new Texture2D(4,4, TextureFormat.BC6H, false);
            if(IESAtlas == null) IESAtlas = new Texture2D(4,4, TextureFormat.BC4, 1, false);
            if(EmissiveAtlas == null) EmissiveAtlas = new Texture2D(4,4, TextureFormat.BC6H, false);
            if(NormalAtlas == null) NormalAtlas = new Texture2D(4,4, TextureFormat.BC5, 1, false);
            if(SingleComponentAtlas == null) SingleComponentAtlas = new Texture2D(4,4, TextureFormat.BC4, 1, false);
            if(AlphaAtlas == null) AlphaAtlas = new Texture2D(4,4, TextureFormat.BC4, 1, false);
            
            UpdateMaterialDefinition();
            Refitter = Resources.Load<ComputeShader>("Utility/BVHRefitter");
            LightRefitter = Resources.Load<ComputeShader>("Utility/LightBVHRefitter");
            LightBVHKernel = LightRefitter.FindKernel("RefitKernel");
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
            LightNodeBuffer.ReleaseSafe();
            BVH8AggregatedBuffer.ReleaseSafe();
            AggTriBuffer.ReleaseSafe();
            MeshFunctions = Resources.Load<ComputeShader>("Utility/GeneralMeshFunctions");
            TriangleBufferKernel = MeshFunctions.FindKernel("CombineTriBuffers");
            NodeBufferKernel = MeshFunctions.FindKernel("CombineNodeBuffers");
            LightBufferKernel = MeshFunctions.FindKernel("CombineLightBuffers");
            LightNodeBufferKernel = MeshFunctions.FindKernel("CombineLightNodes");

            if (Terrains.Count != 0) for (int i = 0; i < Terrains.Count; i++) Terrains[i].Load();
        }

        private void UpdateRenderAndBuildQues() {
            ChildrenUpdated = false;
            OnlyInstanceUpdated = false;

            int QueCount = RemoveQue.Count;
            {//Main Object Data Handling
                for (int i = QueCount - 1; i >= 0; i--) {
                    switch(RemoveQue[i].ExistsInQue) {
                        case 0: {int Index = RenderQue.IndexOf(RemoveQue[i]); if(Index != -1) {RenderQue.RemoveAt(Index); RenderTransforms.RemoveAt(Index);}}; break;
                        case 1: BuildQue.Remove(RemoveQue[i]); break;
                        case 2: UpdateQue.Remove(RemoveQue[i]); break;
                        case 3: AddQue.Remove(RemoveQue[i]); break;
                    }
                    RemoveQue[i].ExistsInQue = -1;
                    RemoveQue[i].QueInProgress = -1;
                    ChildrenUpdated = true;
                }
                RemoveQue.Clear();
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
                QueCount = BuildQue.Count;
                for (int i = QueCount - 1; i >= 0; i--) {//Promotes from Build Que to Render Que
                    if (BuildQue[i].AsyncTask.IsFaulted) {//Fuck, something fucked up
                        Debug.LogError(BuildQue[i].AsyncTask.Exception + ", " + BuildQue[i].Name);
                        BuildQue[i].FailureCount++;
                        BuildQue[i].ClearAll();
                        if(BuildQue[i].FailureCount > 6) {
                            BuildQue[i].ExistsInQue = -1;
                            BuildQue[i].QueInProgress = -1;
                        } else {
                            BuildQue[i].ExistsInQue = 3;
                            BuildQue[i].QueInProgress = 3;
                            AddQue.Add(BuildQue[i]);
                        }
                        BuildQue.RemoveAt(i);
                    } else {
                        if (BuildQue[i].AsyncTask.Status == TaskStatus.RanToCompletion) {
                            if (BuildQue[i].AggTriangles == null || BuildQue[i].AggNodes == null) {
                                BuildQue[i].FailureCount++;
                                BuildQue[i].ClearAll();
                                if(BuildQue[i].FailureCount > 6) {
                                    BuildQue[i].ExistsInQue = -1;
                                    BuildQue[i].QueInProgress = -1;
                                } else {
                                    BuildQue[i].ExistsInQue = 3;
                                    BuildQue[i].QueInProgress = 3;
                                    AddQue.Add(BuildQue[i]);
                                }
                            } else {
                                BuildQue[i].SetUpBuffers();
                                BuildQue[i].ExistsInQue = 0;
                                BuildQue[i].QueInProgress = 0;
                                RenderTransforms.Add(BuildQue[i].gameObject.transform);
                                RenderQue.Add(BuildQue[i]);
                                ChildrenUpdated = true;
                            }
                            BuildQue.RemoveAt(i);
                        }
                    }
                }
                QueCount = UpdateQue.Count;
                for (int i = QueCount - 1; i >= 0; i--) {//Demotes from Render Que to Build Que in case mesh has changed
                    if (UpdateQue[i] != null && UpdateQue[i].gameObject.activeInHierarchy) {
                        if (UpdateQue[i].ExistsInQue == 0) {
                            int Index = RenderQue.IndexOf(UpdateQue[i]); 
                            RenderQue.RemoveAt(Index); 
                            RenderTransforms.RemoveAt(Index);
                            UpdateQue[i].Reset(1);
                            BuildQue.Add(UpdateQue[i]);
                        } else if(UpdateQue[i].ExistsInQue == 1) {
                            int Index = BuildQue.IndexOf(UpdateQue[i]); 
                            BuildQue.RemoveAt(Index); 
                            UpdateQue[i].Reset(1);
                            BuildQue.Add(UpdateQue[i]);
                        } else if (UpdateQue[i].ExistsInQue == -1) {
                            UpdateQue[i].Reset(1);
                            BuildQue.Add(UpdateQue[i]);
                        }
                        ChildrenUpdated = true;
                    }
                    UpdateQue.RemoveAt(i);
                }
            }
            {//Instanced Models Data Handling
                InstanceData.UpdateRenderAndBuildQues(ref ChildrenUpdated);
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

                QueCount = InstanceAddQue.Count;
                for (int i = QueCount - 1; i >= 0; i--) {
                    if (InstanceAddQue[i].InstanceParent != null && InstanceAddQue[i].InstanceParent.gameObject.activeInHierarchy) {
                        InstanceAddQue[i].ExistsInQue = 1;
                        InstanceBuildQue.Add(InstanceAddQue[i]);
                        InstanceAddQue.RemoveAt(i);
                    }
                }
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
            Assets = this;
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
            int QueCount = BuildQue.Count;
            for(int i = QueCount - 1; i >= 0; i--) {
                if(!BuildQue[i].enabled) BuildQue.RemoveAt(i);
            }

            QueCount = BuildQue.Count;
            RunningTasks = QueCount;
            for (int i = 0; i < QueCount; i++)
            {
                var CurrentRep = i;
                BuildQue[CurrentRep].LoadData();
                BuildQue[CurrentRep].AsyncTask = Task.Run(() => { BuildQue[CurrentRep].BuildTotal(); RunningTasks--; });
                BuildQue[CurrentRep].ExistsInQue = 1;
                BuildQue[CurrentRep].QueInProgress = 1;
            }
            didstart = false;
        }
        #if TTLightMapping
            public void EditorBuild()
        #else
            public void BuildCombined()
        #endif
        {//Only has unbuilt be built
            Assets = this;
            Terrains = new List<TerrainObject>(GetComponentsInChildren<TerrainObject>());
            init();
            List<ParentObject> TempQue = new List<ParentObject>(GameObject.FindObjectsOfType<ParentObject>());
            InstanceAddQue = new List<InstancedObject>(GetComponentsInChildren<InstancedObject>());
            InstanceData.BuildCombined();
            RunningTasks = 0;
            int QueCount = TempQue.Count;
            for(int i = QueCount - 1; i >= 0; i--) {
                if(!TempQue[i].enabled) TempQue.RemoveAt(i);
            }

            QueCount = TempQue.Count;

            for (int i = 0; i < QueCount; i++)
            {
                if(TempQue[i].transform.parent != null && TempQue[i].transform.parent.name == "InstancedStorage") continue;
                if (TempQue[i].HasCompleted && !TempQue[i].NeedsToUpdate) {
                    TempQue[i].ExistsInQue = 0;
                    TempQue[i].QueInProgress = 0;
                    RenderQue.Add(TempQue[i]);
                    RenderTransforms.Add(TempQue[i].gameObject.transform);
                }
                else {TempQue[i].ExistsInQue = 1; TempQue[i].QueInProgress = 1; BuildQue.Add(TempQue[i]); RunningTasks++;}
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
                int CurLightNodeOffset = 0;
                TotalParentObjectSize = 0;
                LightMeshCount = 0;
                LightMeshes.Clear();
                LightTransforms.Clear();
                int TotalMatCount = 0;
                int AggLightNodeCount = 0;
                for(int i = 0; i < ParentsLength; i++) if (RenderQue[i].LightTriangles.Count != 0) {LightMeshCount++; AggLightNodeCount += RenderQue[i].LBVH.nodes.Length;}
                for (int i = 0; i < InstanceRenderQue.Count; i++) {if (InstanceRenderQue[i].InstanceParent.LightTriangles.Count != 0) LightMeshCount++;}


                
                if (BVH8AggregatedBuffer != null)
                {
                    BVH8AggregatedBuffer.Release();
                    AggTriBuffer.Release();
                    LightTriBuffer.Release();
                    if(LightNodeBuffer == null) LightNodeBuffer.Release();
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
                    if (InstanceData.RenderQue[i].LightTriangles.Count != 0) AggLightNodeCount += InstanceData.RenderQue[i].LBVH.nodes.Length;
                }

                CurLightNodeOffset = 2 * (LightMeshCount);
                AggLightNodeCount += CurLightNodeOffset;
                Debug.Log("Light Tri Count: " + LightTriCount);
                Debug.Log("Total Tri Count: " + AggTriCount);
                if(LightTriCount == 0) {LightTriCount++; AggLightNodeCount++;}
                if (AggNodeCount != 0)
                {//Accumulate the BVH nodes and triangles for all normal models
                    LightAABBs = new LightBounds[LightMeshCount];

                    CommonFunctions.CreateDynamicBuffer(ref BVH8AggregatedBuffer, AggNodeCount, 80);
                    CommonFunctions.CreateDynamicBuffer(ref AggTriBuffer, AggTriCount, 88);
                    CommonFunctions.CreateDynamicBuffer(ref LightTriBuffer, LightTriCount, 44);
                    CommonFunctions.CreateDynamicBuffer(ref LightNodeBuffer, AggLightNodeCount, 40);
                    MeshFunctions.SetBuffer(TriangleBufferKernel, "OutCudaTriArray", AggTriBuffer);
                    MeshFunctions.SetBuffer(NodeBufferKernel, "OutAggNodes", BVH8AggregatedBuffer);
                    MeshFunctions.SetBuffer(LightBufferKernel, "LightTrianglesOut", LightTriBuffer);
                    MeshFunctions.SetBuffer(LightNodeBufferKernel, "LightNodesOut", LightNodeBuffer);
                    int MatOffset = 0;
                    int CurLightMesh = 0;
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

                            cmd.SetComputeIntParam(MeshFunctions, "Offset", CurLightNodeOffset);
                            cmd.SetComputeIntParam(MeshFunctions, "Count", RenderQue[i].LightNodeBuffer.count);
                            cmd.SetComputeBufferParam(MeshFunctions, LightNodeBufferKernel, "LightNodesIn", RenderQue[i].LightNodeBuffer);
                            cmd.DispatchCompute(MeshFunctions, LightNodeBufferKernel, (int)Mathf.Ceil(RenderQue[i].LightNodeBuffer.count / 372.0f), 1, 1);

                            LightTransforms.Add(RenderTransforms[i]);
                            LightMeshes.Add(new LightMeshData()
                            {
                                StartIndex = CurLightTriOffset,
                                IndexEnd = RenderQue[i].LightTriangles.Count + CurLightTriOffset,
                                MatOffset = MatOffset,
                                LockedMeshIndex = i
                            });
                            try
                            {
                                LightAABBs[CurLightMesh].phi = RenderQue[i].LBVH.ParentBound.aabb.phi;
                                LightAABBs[CurLightMesh].cosTheta_o = RenderQue[i].LBVH.ParentBound.aabb.cosTheta_o;
                                LightAABBs[CurLightMesh].cosTheta_e = RenderQue[i].LBVH.ParentBound.aabb.cosTheta_e;
                            } catch(System.Exception e) {Debug.Log("BROKEN FUCKER: " + RenderQue[i].Name + ", " + e);}


                            RenderQue[i].LightTriOffset = CurLightTriOffset;
                            RenderQue[i].LightNodeOffset = CurLightNodeOffset;
                            CurLightNodeOffset += RenderQue[i].LBVH.nodes.Length;
                            CurLightTriOffset += RenderQue[i].LightTriangles.Count;
                            TotalParentObjectSize += RenderQue[i].LightTriBuffer.count * RenderQue[i].LightTriBuffer.stride;
                            CurLightMesh++;
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
                            
                            cmd.SetComputeIntParam(MeshFunctions, "Offset", CurLightNodeOffset);
                            cmd.SetComputeIntParam(MeshFunctions, "Count", InstanceData.RenderQue[i].LightNodeBuffer.count);
                            cmd.SetComputeBufferParam(MeshFunctions, LightNodeBufferKernel, "LightNodesIn", InstanceData.RenderQue[i].LightNodeBuffer);
                            cmd.DispatchCompute(MeshFunctions, LightNodeBufferKernel, (int)Mathf.Ceil(InstanceData.RenderQue[i].LightNodeBuffer.count / 372.0f), 1, 1);


                            InstanceData.RenderQue[i].LightTriOffset = CurLightTriOffset;
                            CurLightTriOffset += InstanceData.RenderQue[i].LightTriangles.Count;
                            InstanceData.RenderQue[i].LightEndIndex = CurLightTriOffset;
                            InstanceData.RenderQue[i].LightNodeOffset = CurLightNodeOffset;
                            CurLightNodeOffset += InstanceData.RenderQue[i].LBVH.nodes.Length;
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
                            LightTransforms.Add(InstanceRenderTransforms[i]);
                            LightMeshes.Add(new LightMeshData()
                            {
                                LockedMeshIndex = i + ParentsLength,
                                StartIndex = InstanceRenderQue[i].InstanceParent.LightTriOffset,
                                IndexEnd = InstanceRenderQue[i].InstanceParent.LightEndIndex
                            });

                            LightAABBs[CurLightMesh].phi = InstanceRenderQue[i].InstanceParent.LBVH.ParentBound.aabb.phi;
                            LightAABBs[CurLightMesh].cosTheta_o = InstanceRenderQue[i].InstanceParent.LBVH.ParentBound.aabb.cosTheta_o;
                            LightAABBs[CurLightMesh].cosTheta_e = InstanceRenderQue[i].InstanceParent.LBVH.ParentBound.aabb.cosTheta_e;
                            CurLightMesh++;
                        }
                    }
                }

                if (LightMeshCount == 0) { LightMeshes.Add(new LightMeshData() { }); }


                if (!OnlyInstanceUpdated || _Materials.Length == 0) {
                    CreateAtlas(TotalMatCount, cmd);
                    // MaterialBuffer.ReleaseSafe();
                    CommonFunctions.CreateComputeBuffer(ref MaterialBuffer, _Materials);
                }
            }
            ParentCountHasChanged = false;
            if (UseSkinning && didstart) {
                for (int i = 0; i < ParentsLength; i++) {//Refit BVH's of skinned meshes
                    if (RenderQue[i].IsSkinnedGroup || RenderQue[i].IsDeformable) {
                        RenderQue[i].RefitMesh(ref BVH8AggregatedBuffer, ref AggTriBuffer, ref LightTriBuffer, ref LightNodeBuffer, cmd);
                        if(i < MyMeshesCompacted.Count) {
                            MyMeshDataCompacted TempMesh2 = MyMeshesCompacted[i];
                            TempMesh2.Transform = RenderTransforms[i].worldToLocalMatrix;
                            MyMeshesCompacted[i] = TempMesh2;
                            MeshAABBs[i] = RenderQue[i].aabb;
                        }
                    }
                }
            }
            {//BINDLESS-TEST this spot is guarenteed to run once per frame, be very close to the begining of the commandbuffer, and is guarenteed to have the AlbedoArray filled
            #if !DX11Only && !UseAtlas
                Shader.SetGlobalTexture("_BindlessTextures", Texture2D.whiteTexture);
                bindlessTextures.UpdateDescriptors();
            #endif
            }

        }

        public struct AggData {
            public int AggIndexCount;
            public int AggNodeCount;
            public int MaterialOffset;
            public int mesh_data_bvh_offsets;
            public int LightTriCount;
            public int LightNodeOffset;
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
        ComputeBuffer LightBVHTransformsBuffer;

        [System.Serializable]
        public struct LightBVHTransform {
            public Matrix4x4 Transform;
            public int SolidOffset;
        }
        [HideInInspector] public LightBVHTransform[] LightBVHTransforms;

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
                        Mesh mesh = new Mesh();
                        if(B2.TryGetComponent<MeshFilter>(out MeshFilter TempFilter)) mesh = TempFilter.sharedMesh;
                        else if(B2.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer TempSkin)) mesh = TempSkin.sharedMesh;
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
                        if(B2.TryGetComponent<RayTracingObject>(out RayTracingObject TempObj))
                            AccelStruct.AddInstance(A, B, true, false, (uint)((TempObj.SpecTrans[0] == 1) ? 0x2 : 0x1), (uint)MeshOffset);
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

                for (int i = 0; i < MaxRecur; i++) {LayerStack[i] = new Layer2(); LayerStack[i].Slab = new List<int>();}
                for(int i = 0; i < 8; i++) PresetLayer.Children[i] = 0;
                for (int i = 0; i < NodeCount; i++) {
                    ForwardStack[i] = PresetLayer;
                    if (IsLeafList[i].x == 1) {
                        int first_triangle = (byte)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] & 0b11111;
                        int NumBits = CommonFunctions.NumberOfSetBits((byte)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] >> 5);
                        ForwardStack[i].Children[NodePair[i].InNodeOffset] = NumBits + ((int)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].base_index_triangle + first_triangle) * 24 + 1;
                    } 
                    else ForwardStack[i].Children[NodePair[i].InNodeOffset] = (-i) - 1;
                    ForwardStack[IsLeafList[i].z].Children[NodePair[i].InNodeOffset] = (-i) - 1;
                    
                    var TempLayer = LayerStack[IsLeafList[i].y];
                    TempLayer.Slab.Add(i);
                    LayerStack[IsLeafList[i].y] = TempLayer;
                }
                CommonFunctions.ConvertToSplitNodes(TLASBVH8, ref SplitNodes);
                List<Layer2> TempStack = new List<Layer2>();
                for (int i = 0; i < LayerStack.Length; i++) if(LayerStack[i].Slab.Count != 0) TempStack.Add(LayerStack[i]);
                MaxRecur = TempStack.Count;

                if(WorkingBuffer != null) for(int i = 0; i < WorkingBuffer.Length; i++) WorkingBuffer[i].ReleaseSafe();
                NodeBuffer.ReleaseSafe();
                StackBuffer.ReleaseSafe();
                ToBVHIndexBuffer.ReleaseSafe();
                BVHDataBuffer.ReleaseSafe();
                BoxesBuffer.ReleaseSafe();
                TLASCWBVHIndexes.ReleaseSafe();
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
                BoxesBuffer = new ComputeBuffer(MeshAABBs.Length, 24);
                TLASCWBVHIndexes = new ComputeBuffer(MeshAABBs.Length, 4);
                TLASCWBVHIndexes.SetData(TLASBVH8.cwbvh_indices);
                CurFrame = 0;
                TLASTask = null;
            #endif
            LightBVHTransformsBuffer.ReleaseSafe();
            if(LightBVHTransforms.Length != 0) {
                LightBVHTransformsBuffer = new ComputeBuffer(LightBVHTransforms.Length, 68);
                LightBVHTransformsBuffer.SetData(LightBVHTransforms);
            }
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

                for (int i = 0; i < TempRecur; i++) {LayerStack[i] = new Layer2(); LayerStack[i].Slab = new List<int>();}
                for(int i = 0; i < 8; i++) PresetLayer.Children[i] = 0;
                for (int i = 0; i < NodeCount; i++) {
                    ForwardStack[i] = PresetLayer;
                    if (IsLeafList[i].x == 1) {
                        int first_triangle = (byte)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] & 0b11111;
                        int NumBits = CommonFunctions.NumberOfSetBits((byte)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] >> 5);
                        ForwardStack[i].Children[NodePair[i].InNodeOffset] = NumBits + ((int)TLASBVH8.BVH8Nodes[NodePair[i].BVHNode].base_index_triangle + first_triangle) * 24 + 1;
                    } 
                    else ForwardStack[i].Children[NodePair[i].InNodeOffset] = (-i) - 1;
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
        ComputeBuffer LightBoxesBuffer;
        int CurFrame = 0;
        int BVHNodeCount = 0;
        LightBVHBuilder LBVH;

        public unsafe void RefitTLAS(AABB[] Boxes, CommandBuffer cmd)
        {
            CurFrame++;
            #if !HardwareRT
            if(TLASTask == null) TLASTask = Task.Run(() => CorrectRefit(Boxes));

             if(TLASTask.Status == TaskStatus.RanToCompletion && CurFrame % 25 == 24) {
                MaxRecur = TempRecur; 
                if(LightMeshCount > 0) {
                    if(LBVH != null && LBVH.WorkingSet != null) {
                        int Coun = LBVH.WorkingSet.Length - 1;
                        for(int i = Coun; i >= 0; i--) {
                            LBVH.WorkingSet[i].ReleaseSafe();
                        }
                    }
                    LBVH = new LightBVHBuilder(LightAABBs);
                    LightNodeBuffer.SetData(LBVH.nodes, 0, 0, LBVH.nodes.Length);
                }
                if(WorkingBuffer != null) for(int i = 0; i < WorkingBuffer.Length; i++) WorkingBuffer[i]?.Release();
                if (NodeBuffer != null) NodeBuffer.Release();
                if (StackBuffer != null) StackBuffer.Release();
                if (ToBVHIndexBuffer != null) ToBVHIndexBuffer.Release();
                if (BVHDataBuffer != null) BVHDataBuffer.Release();
                if (BoxesBuffer != null) BoxesBuffer.Release();
                if (TLASCWBVHIndexes != null) TLASCWBVHIndexes.Release();
                if (LightBVHTransformsBuffer != null) LightBVHTransformsBuffer.Release();
                WorkingBuffer = new ComputeBuffer[LayerStack.Length];
                for (int i = 0; i < MaxRecur; i++) {
                    WorkingBuffer[i] = new ComputeBuffer(LayerStack[i].Slab.Count, 4);
                    WorkingBuffer[i].SetData(LayerStack[i].Slab);
                }
                if(LightBVHTransforms.Length != 0) {
                    LightBVHTransformsBuffer = new ComputeBuffer(LightBVHTransforms.Length, 68);
                    LightBVHTransformsBuffer.SetData(LightBVHTransforms);
                }
                NodeBuffer = new ComputeBuffer(NodePair.Count, 32);
                NodeBuffer.SetData(NodePair);
                StackBuffer = new ComputeBuffer(ForwardStack.Length, 32);
                StackBuffer.SetData(ForwardStack);
                ToBVHIndexBuffer = new ComputeBuffer(ToBVHIndex.Length, 4);
                ToBVHIndexBuffer.SetData(ToBVHIndex);
                BVHDataBuffer = new ComputeBuffer(TempBVHArray.Length, 260);
                BVHDataBuffer.SetData(SplitNodes);
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
                for (int i = MaxRecur - 1; i >= 0; i--)
                {
                    var NodeCount2 = WorkingBuffer[i].count;
                    cmd.SetComputeIntParam(Refitter, "NodeCount", NodeCount2);
                    cmd.SetComputeBufferParam(Refitter, RefitLayer, "WorkingBuffer", WorkingBuffer[i]);
                    cmd.DispatchCompute(Refitter, RefitLayer, (int)Mathf.Ceil(NodeCount2 / (float)256), 1, 1);
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
            #else

             if(CurFrame % 25 == 24) {
                if(LightMeshCount > 0) {
                    LBVH = new LightBVHBuilder(LightAABBs);
                    LightNodeBuffer.SetData(LBVH.nodes, 0, 0, LBVH.nodes.Length);
                }
                LightBVHTransformsBuffer.ReleaseSafe();
                if(LightBVHTransforms.Length != 0) {
                    LightBVHTransformsBuffer = new ComputeBuffer(LightBVHTransforms.Length, 68);
                    LightBVHTransformsBuffer.SetData(LightBVHTransforms);
                }
            }

            #endif
                if(LightAABBs != null && LightAABBs.Length != 0) {
                    LightBVHTransformsBuffer.SetData(LightBVHTransforms);
                    if(LightBoxesBuffer == null || LightBoxesBuffer.count != LightAABBs.Length) LightBoxesBuffer = new ComputeBuffer(LightAABBs.Length, 56);
                    LightBoxesBuffer.SetData(LightAABBs);
                    cmd.BeginSample("LightRefitter");
                    cmd.SetComputeBufferParam(LightRefitter, LightBVHKernel, "Transfers", LightBVHTransformsBuffer);
                    cmd.SetComputeBufferParam(LightRefitter, LightBVHKernel, "LightNodesWrite", LightNodeBuffer);
                    cmd.SetComputeBufferParam(LightRefitter, LightBVHKernel, "LightBounds", LightBoxesBuffer);
                    int ObjectOffset = 0;
                    for(int i = LBVH.WorkingSet.Length - 1; i >= 0; i--) {
                        var ObjOffVar = ObjectOffset;
                        var SetCount = LBVH.WorkingSet[i].count;
                        cmd.SetComputeIntParam(LightRefitter, "SetCount", SetCount);
                        cmd.SetComputeIntParam(LightRefitter, "ObjectOffset", ObjOffVar);
                        cmd.SetComputeBufferParam(LightRefitter, LightBVHKernel, "WorkingSet", LBVH.WorkingSet[i]);
                        cmd.DispatchCompute(LightRefitter, LightBVHKernel, (int)Mathf.Ceil(SetCount / (float)256.0f), 1, 1);

                        ObjectOffset += SetCount;
                    }
                    cmd.EndSample("LightRefitter");
                }
        }

        private bool ChangedLastFrame = true;
        private RayTracingMaster RayMaster;
        public int UpdateTLAS(CommandBuffer cmd)
        {  //Allows for objects to be moved in the scene or animated while playing 
            if(RayMaster == null) TryGetComponent<RayTracingMaster>(out RayMaster);
            bool LightsHaveUpdated = false;
            AccumulateData(cmd);
            if (!didstart || PrevLightCount != RayTracingMaster._rayTracingLights.Count || UnityLights.Count == 0)
            {
                UnityLights.Clear();
                UnityLightCount = 0;
                foreach (RayTracingLights RayLight in RayTracingMaster._rayTracingLights) {
                    UnityLightCount++;
                    RayLight.UpdateLight();
                    if (RayLight.ThisLightData.Type == 1) SunDirection = RayLight.ThisLightData.Direction;
                    RayLight.ArrayIndex = UnityLightCount - 1;
                    RayLight.ThisLightData.Radiance *= RayMaster.LocalTTSettings.LightEnergyScale;
                    RayLight.ThisLightData.IESTex = new Vector2Int(-1, 0);
                    UnityLights.Add(RayLight.ThisLightData);
                }
                if (UnityLights.Count == 0) { UnityLights.Add(new LightData() { }); }
                if (PrevLightCount != RayTracingMaster._rayTracingLights.Count) LightsHaveUpdated = true;
                PrevLightCount = RayTracingMaster._rayTracingLights.Count;
                // UnityLightBuffer.ReleaseSafe();
                CreateAtlasIES();
                CommonFunctions.CreateComputeBuffer(ref UnityLightBuffer, UnityLights);
            } else {
                int LightCount = RayTracingMaster._rayTracingLights.Count;
                RayTracingLights RayLight;
                for (int i = 0; i < LightCount; i++) {
                    RayLight = RayTracingMaster._rayTracingLights[i];
                    RayLight.UpdateLight();
                    RayLight.ThisLightData.Radiance *= RayMaster.LocalTTSettings.LightEnergyScale;
                    if (RayLight.ThisLightData.Type == 1) SunDirection = RayLight.ThisLightData.Direction;
                    UnityLights[RayLight.ArrayIndex] = RayLight.ThisLightData;
                }
                cmd.SetBufferData(UnityLightBuffer, UnityLights);
            }

                // UnityEngine.Profiling.Profiler.BeginSample("Lights Update");
                // UnityEngine.Profiling.Profiler.EndSample();

            int MatOffset = 0;
            int MeshDataCount = RenderQue.Count;
            int aggregated_bvh_node_count = 2 * (MeshDataCount + InstanceRenderQue.Count);
            int AggNodeCount = aggregated_bvh_node_count;
            int AggTriCount = 0;
            bool HasChangedMaterials = false;
            if(MeshAABBs.Length == 0) return -1;
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
                        LightTriCount = RenderQue[i].LightTriangles.Count,
                        LightNodeOffset = RenderQue[i].LightNodeOffset
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
                int RendQueCount = RenderQue.Count;
                LightBVHTransforms = new LightBVHTransform[LightMeshCount];
                for (int i = 0; i < LightMeshCount; i++)
                {
                    Matrix4x4 Mat = LightTransforms[i].localToWorldMatrix;
                    LightBVHTransforms[i].Transform = Mat;
                    int Index = LightMeshes[i].LockedMeshIndex;
                    if(Index < RendQueCount) {
                        LightBVHTransforms[i].SolidOffset = RenderQue[Index].LightNodeOffset; 
                        Vector3 new_center = CommonFunctions.transform_position(Mat, (RenderQue[Index].LBVH.ParentBound.aabb.b.BBMax + RenderQue[Index].LBVH.ParentBound.aabb.b.BBMin) / 2.0f);
                        Vector3 new_extent = CommonFunctions.transform_direction(Mat, (RenderQue[Index].LBVH.ParentBound.aabb.b.BBMax - RenderQue[Index].LBVH.ParentBound.aabb.b.BBMin) / 2.0f);

                        LightAABBs[i].b.BBMin = new_center - new_extent;
                        LightAABBs[i].b.BBMax = new_center + new_extent;
                        LightAABBs[i].w = (Mat * RenderQue[Index].LBVH.ParentBound.aabb.w).normalized;
                    } else {
                        Index -= RendQueCount;
                        LightBVHTransforms[i].SolidOffset = InstanceRenderQue[Index].InstanceParent.LightNodeOffset; 
                        Vector3 new_center = CommonFunctions.transform_position(Mat, (InstanceRenderQue[Index].InstanceParent.LBVH.ParentBound.aabb.b.BBMax + InstanceRenderQue[Index].InstanceParent.LBVH.ParentBound.aabb.b.BBMin) / 2.0f);
                        Vector3 new_extent = CommonFunctions.transform_direction(Mat, (InstanceRenderQue[Index].InstanceParent.LBVH.ParentBound.aabb.b.BBMax - InstanceRenderQue[Index].InstanceParent.LBVH.ParentBound.aabb.b.BBMin) / 2.0f);

                        LightAABBs[i].b.BBMin = new_center - new_extent;
                        LightAABBs[i].b.BBMax = new_center + new_extent;
                        LightAABBs[i].w = (Mat * InstanceRenderQue[Index].InstanceParent.LBVH.ParentBound.aabb.w).normalized;
                    }
                }
                if(LightMeshCount > 0) {
                    LBVH = new LightBVHBuilder(LightAABBs);
                    LightNodeBuffer.SetData(LBVH.nodes, 0, 0, LBVH.nodes.Length);
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
                    Aggs[i].LightNodeOffset = InstanceData.RenderQue[i].LightNodeOffset;
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
                        LightTriCount = Aggs[Index].LightTriCount,
                        LightNodeOffset = Aggs[Index].LightNodeOffset

                    });
                    InstanceRenderQue[i].CompactedMeshData = MeshCount + i;
                    AABB aabb = InstanceRenderQue[i].InstanceParent.aabb_untransformed;
                    CreateAABB(InstanceRenderQue[i].transform, ref aabb);
                    MeshAABBs[RenderQue.Count + i] = aabb;
                }
                // UnityEngine.Profiling.Profiler.EndSample();


                Debug.Log("Total Object Count: " + MeshAABBs.Length);
                // UnityEngine.Profiling.Profiler.BeginSample("Update Materials");
                HasChangedMaterials = UpdateMaterials();
                // UnityEngine.Profiling.Profiler.EndSample();
                ConstructNewTLAS();
                #if !HardwareRT
                    BVH8AggregatedBuffer.SetData(TempBVHArray, 0, 0, TempBVHArray.Length);
                #endif
                CommonFunctions.CreateComputeBuffer(ref MeshDataBuffer, MyMeshesCompacted);
                #if HardwareRT
                    CommonFunctions.CreateComputeBuffer(ref MeshIndexOffsets, MeshOffsets);
                    CommonFunctions.CreateComputeBuffer(ref SubMeshOffsetsBuffer, SubMeshOffsets);
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

                int RendQueCount = RenderQue.Count;
                for (int i = 0; i < LightMeshCount; i++) LightBVHTransforms[i].Transform = LightTransforms[i].localToWorldMatrix;

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
                HasChangedMaterials = UpdateMaterials();
                MeshDataBuffer.SetData(MyMeshesCompacted);
            }
            if(HasChangedMaterials) MaterialBuffer.SetData(_Materials);
            LightMeshData CurLightMesh;
            for (int i = 0; i < LightMeshCount; i++)
            {
                CurLightMesh = LightMeshes[i];
                CurLightMesh.Center = LightTransforms[i].position;
                LightMeshes[i] = CurLightMesh;
            }
            if (LightMeshBuffer != null && LightMeshBuffer.IsValid() && LightMeshCount != 0 && LightMeshCount == LightMeshBuffer.count) LightMeshBuffer.SetData(LightMeshes);
            else {
                CommonFunctions.CreateComputeBuffer(ref LightMeshBuffer, LightMeshes);
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
                    TempMat.BaseColor = (!CurrentMaterial.UseKelvin[Index]) ? CurrentMaterial.BaseColor[Index] : new Vector3(Mathf.CorrelatedColorTemperatureToRGB(CurrentMaterial.KelvinTemp[Index]).r, Mathf.CorrelatedColorTemperatureToRGB(CurrentMaterial.KelvinTemp[Index]).g, Mathf.CorrelatedColorTemperatureToRGB(CurrentMaterial.KelvinTemp[Index]).b);
                    TempMat.emission = CurrentMaterial.emission[Index];
                    TempMat.Roughness = ((int)CurrentMaterial.MaterialOptions[Index] != 1) ? CurrentMaterial.Roughness[Index] : Mathf.Max(CurrentMaterial.Roughness[Index], 0.000001f);
                    TempMat.TransmittanceColor = CurrentMaterial.TransmissionColor[Index];
                    TempMat.MatType = (int)CurrentMaterial.MaterialOptions[Index];
                    TempMat.EmissionColor = CurrentMaterial.EmissionColor[Index];
                    TempMat.Tag = CurrentMaterial.Flags[Index];

                    TempMat.metallic = CurrentMaterial.Metallic[Index];
                    TempMat.specularTint = CurrentMaterial.SpecularTint[Index];
                    TempMat.sheen = CurrentMaterial.Sheen[Index];
                    TempMat.sheenTint = CurrentMaterial.SheenTint[Index];
                    TempMat.clearcoat = CurrentMaterial.ClearCoat[Index];
                    TempMat.IOR = CurrentMaterial.IOR[Index];
                    TempMat.clearcoatGloss = CurrentMaterial.ClearCoatGloss[Index];
                    TempMat.specTrans = CurrentMaterial.SpecTrans[Index];
                    TempMat.anisotropic = CurrentMaterial.Anisotropic[Index];
                    TempMat.diffTrans = CurrentMaterial.DiffTrans[Index];
                    TempMat.flatness = CurrentMaterial.Flatness[Index];
                    TempMat.Specular = CurrentMaterial.Specular[Index];
                    TempMat.scatterDistance = CurrentMaterial.ScatterDist[Index];
                    TempMat.AlphaCutoff = CurrentMaterial.AlphaCutoff[Index];
                    TempMat.NormalStrength = CurrentMaterial.NormalStrength[Index];
                    TempMat.MetallicRemap = CurrentMaterial.MetallicRemap[Index];
                    TempMat.RoughnessRemap = CurrentMaterial.RoughnessRemap[Index];
                    TempMat.Hue = CurrentMaterial.Hue[Index];
                    TempMat.Contrast = CurrentMaterial.Contrast[Index];
                    TempMat.Brightness = CurrentMaterial.Brightness[Index];
                    TempMat.Saturation = CurrentMaterial.Saturation[Index];
                    TempMat.BlendColor = CurrentMaterial.BlendColor[Index];
                    TempMat.BlendFactor = CurrentMaterial.BlendFactor[Index];
                    TempMat.AlbedoTextureScale = CurrentMaterial.MainTexScaleOffset[Index];
                    TempMat.SecondaryTextureScale = CurrentMaterial.SecondaryTextureScale[Index];
                    TempMat.Rotation = CurrentMaterial.Rotation[Index] * 3.14159f;
                    TempMat.ColorBleed = CurrentMaterial.ColorBleed[Index];
                    if(RayMaster.LocalTTSettings.MatChangeResetsAccum) {
                        RayMaster.SampleCount = 0;
                        RayMaster.FramesSinceStart = 0;
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