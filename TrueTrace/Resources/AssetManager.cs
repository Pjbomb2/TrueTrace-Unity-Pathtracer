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
#if UNITY_PIPELINE_URP || UNITY_PIPELINE_HDRP
    [ExecuteInEditMode]
#endif
    public class AssetManager : MonoBehaviour
    {//This handels all the data

        private BindlessArray bindlessTextures;
        public int BindlessTextureCount;
        public static AssetManager Assets;
        public int TotalParentObjectSize;
        [HideInInspector] public Texture2D IESAtlas;
        [HideInInspector] public Texture2D AlbedoAtlas;
        [HideInInspector] public Texture2D NormalAtlas;
        [HideInInspector] public Texture2D SingleComponentAtlas;
        [HideInInspector] public Texture2D EmissiveAtlas;
        [HideInInspector] public Texture2D AlphaAtlas;
        [HideInInspector] public RenderTexture HeightmapAtlas;
        [HideInInspector] public RenderTexture AlphaMapAtlas;
        private RenderTexture TempTex;
        private ComputeShader CopyShader;
        private ComputeShader Refitter;
        private ComputeShader MeshFunctions;
        private int RefitLayer;
        private int RefitCopy;
        private int LightTLASRefitKernel;

        private int TriangleBufferKernel;
        private int NodeBufferKernel;
        private int LightBufferKernel;
        private int LightTreeNodeBufferKernel;
        private int LightTreeNodeBufferSkinnedKernel;


        [HideInInspector] public LightBVHTransform[] LightBVHTransforms;
        [HideInInspector] public GaussianTreeNode[] SGTree;
        [HideInInspector] public int LightTreePrimaryTLASOffset;


        [HideInInspector] public List<RayTracingObject> MaterialsChanged;
        [HideInInspector] public MaterialData[] _Materials;
        [HideInInspector] public IntersectionMatData[] IntersectionMats;
        [HideInInspector] public ComputeBuffer BVH8AggregatedBuffer;
        [HideInInspector] public ComputeBuffer AggTriBufferA;
        [HideInInspector] public ComputeBuffer SkinnedMeshAggTriBufferPrev;
        [HideInInspector] public ComputeBuffer AggTriBufferB;
        [HideInInspector] public ComputeBuffer LightTriBuffer;
        [HideInInspector] public ComputeBuffer LightTreeBufferA;
        [HideInInspector] public List<MyMeshDataCompacted> MyMeshesCompacted;
        [HideInInspector] public List<LightData> UnityLights;
        [HideInInspector] public InstancedManager InstanceData;
        [HideInInspector] public List<InstancedObject> Instances;
        [HideInInspector] public ComputeBuffer TLASCWBVHIndexes;
        [HideInInspector] private ComputeBuffer[] WorkingBuffer;
        [HideInInspector] private ComputeBuffer[] LBVHWorkingSet;
        [HideInInspector] public List<TerrainObject> Terrains;
        [HideInInspector] public List<TerrainDat> TerrainInfos;
        [HideInInspector] public ComputeBuffer TerrainBuffer;

        [HideInInspector] private ComputeBuffer MaterialBuffer;
        [HideInInspector] private ComputeBuffer IntersectionMaterialBuffer;
        [HideInInspector] public ComputeBuffer MeshDataBufferA;
        [HideInInspector] public ComputeBuffer MeshDataBufferB;
        [HideInInspector] private ComputeBuffer LightMeshBuffer;
        [HideInInspector] private ComputeBuffer UnityLightBuffer;

        #if HardwareRT
            private ComputeBuffer MeshIndexOffsets;
            private ComputeBuffer SubMeshOffsetsBuffer;
        #endif

        public void SetMeshTraceBuffers(ComputeShader ThisShader, int Kernel) {
            #if !HardwareRT
                ThisShader.SetComputeBuffer(Kernel, "TLASBVH8Indices", TLASCWBVHIndexes);
            #endif
            ThisShader.SetComputeBuffer(Kernel, "AggTrisA", AggTriBufferA);
            ThisShader.SetComputeBuffer(Kernel, "SkinnedMeshTriBufferPrev", SkinnedMeshAggTriBufferPrev);
            ThisShader.SetComputeBuffer(Kernel, "AggTrisB", AggTriBufferB);
            ThisShader.SetComputeBuffer(Kernel, "cwbvh_nodes", BVH8AggregatedBuffer);
            ThisShader.SetComputeBuffer(Kernel, "_MeshData", (RayMaster.LocalTTSettings.DoTLASUpdates && (RayMaster.FramesSinceStart2 % 2 == 0)) ? MeshDataBufferA : MeshDataBufferB);
            ThisShader.SetComputeBuffer(Kernel, "_MeshDataPrev", (RayMaster.LocalTTSettings.DoTLASUpdates && (RayMaster.FramesSinceStart2 % 2 == 1)) ? MeshDataBufferA : MeshDataBufferB);
            ThisShader.SetComputeBuffer(Kernel, "_Materials", MaterialBuffer);
            ThisShader.SetComputeBuffer(Kernel, "_IntersectionMaterials", IntersectionMaterialBuffer);
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
            ThisShader.SetComputeBuffer(Kernel, "_IntersectionMaterials", IntersectionMaterialBuffer);
            ThisShader.SetTexture(Kernel, "Heightmap", HeightmapAtlas);
            ThisShader.SetTexture(Kernel, "TerrainAlphaMap", AlphaMapAtlas);
        }

        public void SetLightData(ComputeShader ThisShader, int Kernel) {
            ThisShader.SetComputeBuffer(Kernel, "_UnityLights", UnityLightBuffer);
            ThisShader.SetComputeBuffer(Kernel, "LightTriangles", LightTriBuffer);
            ThisShader.SetComputeBuffer(Kernel, "_LightMeshes", LightMeshBuffer);
            ThisShader.SetComputeBuffer(Kernel, "SGTree", LightTreeBufferA);
            ThisShader.SetTexture(Kernel, "Heightmap", HeightmapAtlas);
        }


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

        [HideInInspector] public List<Transform> LightTransforms;


        [HideInInspector] public List<LightMeshData> LightMeshes;

        [HideInInspector] public AABB[] MeshAABBs;
        [HideInInspector] public AABB[] TransformedAABBs;
        [HideInInspector] public LightBounds[] LightAABBs;
        [HideInInspector] public GaussianTreeNode[] SGTreeNodes;



        [HideInInspector] public BVH2Builder BVH2;
        [HideInInspector] public BVH8Builder TLASBVH8;
        private BVHNode8DataCompressed[] TempBVHArray;

        [HideInInspector] public bool ParentCountHasChanged;
        [HideInInspector] public bool UseSkinning = true;
        [HideInInspector] public bool HasStart = false;
        [HideInInspector] public bool didstart = false;
        [HideInInspector] public bool ChildrenUpdated;
        private bool OnlyInstanceUpdated;

        [HideInInspector] public Vector3 SunDirection;

        [HideInInspector] public int LightMeshCount;
        [HideInInspector] public int UnityLightCount;
        [HideInInspector] public int MainDesiredRes = 16384;
        [HideInInspector] public int MatCount;
        [HideInInspector] public int AlbedoAtlasSize;
        [HideInInspector] public int IESAtlasSize;



        private int PrevLightCount;

        [SerializeField] public int RunningTasks;
        #if HardwareRT
            [HideInInspector] public List<Vector2> MeshOffsets;
            [HideInInspector] public List<int> SubMeshOffsets;
            [HideInInspector] public UnityEngine.Rendering.RayTracingAccelerationStructure AccelStruct;
        #endif
        public void ClearAll()
        {//My attempt at clearing memory
            // RunningTasks = 0;
            ParentObject[] ChildrenObjects = GameObject.FindObjectsOfType<ParentObject>();
            foreach (ParentObject obj in ChildrenObjects) {
                if(obj.gameObject.transform.root.name == "InstancedStorage") continue;
                obj.ClearAll();
            }
            CommonFunctions.DeepClean(ref _Materials);
            CommonFunctions.DeepClean(ref IntersectionMats);
            CommonFunctions.DeepClean(ref SGTreeNodes);
            CommonFunctions.DeepClean(ref LightTransforms);
            CommonFunctions.DeepClean(ref LightMeshes);
            CommonFunctions.DeepClean(ref MyMeshesCompacted);
            CommonFunctions.DeepClean(ref UnityLights);

            if(TLASBVH8 != null) {
                CommonFunctions.DeepClean(ref TLASBVH8.cwbvh_indices);
                if(TLASBVH8.BVH8NodesArray.IsCreated) TLASBVH8.BVH8NodesArray.Dispose();
                if(TLASBVH8.costArray.IsCreated) TLASBVH8.costArray.Dispose();
                if(TLASBVH8.decisionsArray.IsCreated) TLASBVH8.decisionsArray.Dispose();
            }

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
            LightTreeBufferA.ReleaseSafe();
            // LightTreeBufferB.ReleaseSafe();
            BVH8AggregatedBuffer.ReleaseSafe();
            AggTriBufferA.ReleaseSafe();
            SkinnedMeshAggTriBufferPrev.ReleaseSafe();
            AggTriBufferB.ReleaseSafe();
            WorkingBuffer.ReleaseSafe();
            LBVHWorkingSet.ReleaseSafe();
            BoxesBuffer.ReleaseSafe();
            NodeParentAABBBuffer.ReleaseSafe();
            TerrainBuffer.ReleaseSafe();
            TLASCWBVHIndexes.ReleaseSafe();

            MaterialBuffer.ReleaseSafe();
            IntersectionMaterialBuffer.ReleaseSafe();
            MeshDataBufferA.ReleaseSafe();
            MeshDataBufferB.ReleaseSafe();
            LightMeshBuffer.ReleaseSafe();
            UnityLightBuffer.ReleaseSafe();

            TLASCWBVHIndexes.ReleaseSafe();

            #if HardwareRT
                MeshIndexOffsets?.Release();
                AccelStruct?.Release();
                SubMeshOffsetsBuffer?.Release();
            #endif



            if(BVH != null) {
                BVH.ClearAll();
                BVH = null;
            }
            if(LBVH != null) {
                LBVH.Dispose();
                LBVH = null;
            }

            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

    Vector2Int PackRect(Vector4 ThisRect) {
        int A = (int)Mathf.Ceil(ThisRect.x * 16384.0f) | ((int)Mathf.Ceil(ThisRect.y * 16384.0f) << 15);
        int B = (int)Mathf.Ceil(ThisRect.z * 16384.0f) | ((int)Mathf.Ceil(ThisRect.w * 16384.0f) << 15);
        return new Vector2Int(A, B);
    }
    private void PackAndCompactBindless(Dictionary<int, TexObj> DictTex, int[] Rects, int TexIndex, int ReadIndex = -1) {
        int TexCount = DictTex.Count;
        if(TexCount != 0) {
            for (int i = 0; i < TexCount; i++) {
                if(BindlessTextureCount > 2046) {
                    Debug.LogError("TOO MANY TEXTURES, REPORT BACK TO DEVELOPER");
                    return;
                } else BindlessTextureCount++;
                TexObj SelectedTex = DictTex[Rects[i]];
                int ListLength = SelectedTex.TexObjList.Count;
                var bindlessIdx = bindlessTextures.AppendRaw(SelectedTex.Tex);

                for(int j = 0; j < ListLength; j++) {
                        Vector2Int VectoredTexIndex = new Vector2Int(BindlessTextureCount, SelectedTex.TexObjList[j].z);
                        switch (SelectedTex.TexObjList[j].y) {
                            case 0: _Materials[SelectedTex.TexObjList[j].x].Textures.AlbedoTex = VectoredTexIndex; break;
                            case 1: _Materials[SelectedTex.TexObjList[j].x].Textures.NormalTex = VectoredTexIndex; break;
                            case 2: _Materials[SelectedTex.TexObjList[j].x].Textures.EmissiveTex = VectoredTexIndex; break;
                            case 3: _Materials[SelectedTex.TexObjList[j].x].Textures.AlphaTex = VectoredTexIndex; break;
                            case 4: _Materials[SelectedTex.TexObjList[j].x].Textures.MetallicTex = VectoredTexIndex; break;
                            case 5: _Materials[SelectedTex.TexObjList[j].x].Textures.RoughnessTex = VectoredTexIndex; break;
                            case 6: _Materials[SelectedTex.TexObjList[j].x].Textures.MatCapMask = VectoredTexIndex; break;
                            case 7: _Materials[SelectedTex.TexObjList[j].x].Textures.MatCapTex = VectoredTexIndex; break;
                            case 9: _Materials[SelectedTex.TexObjList[j].x].Textures.SecondaryAlbedoTex = VectoredTexIndex; break;
                            case 10: _Materials[SelectedTex.TexObjList[j].x].Textures.SecondaryAlbedoMask = VectoredTexIndex; break;
                            case 11: _Materials[SelectedTex.TexObjList[j].x].Textures.SecondaryNormalTex = VectoredTexIndex; break;
                            case 12: _Materials[SelectedTex.TexObjList[j].x].Textures.DiffTransTex = VectoredTexIndex; break;
                            default: break;
                        }
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
        // DesiredRes = 1024;
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
                case 12://DiffTransMap
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
                                if(TempRect.TexType == 1) _Materials[SelectedTex.TexObjList[j].x].Textures.NormalTex = PackRect(RectSelect); 
                                else if(TempRect.TexType == 11) _Materials[SelectedTex.TexObjList[j].x].Textures.SecondaryNormalTex = PackRect(RectSelect); 
                            break;
                            case 4: 
                                if(TempRect.TexType == 4) _Materials[SelectedTex.TexObjList[j].x].Textures.MetallicTex = PackRect(RectSelect); 
                                else if(TempRect.TexType == 5)  _Materials[SelectedTex.TexObjList[j].x].Textures.RoughnessTex = PackRect(RectSelect);
                                else if(TempRect.TexType == 6)  _Materials[SelectedTex.TexObjList[j].x].Textures.MatCapMask = PackRect(RectSelect);
                                else if(TempRect.TexType == 10)  _Materials[SelectedTex.TexObjList[j].x].Textures.SecondaryAlbedoMask = PackRect(RectSelect);
                                else if(TempRect.TexType == 12) _Materials[SelectedTex.TexObjList[j].x].Textures.DiffTransTex = PackRect(RectSelect); 
                            break;
                            case 5: 
                                _Materials[SelectedTex.TexObjList[j].x].Textures.EmissiveTex = PackRect(RectSelect); 
                            break;
                            case 6: 
                                if(TempRect.TexType == 0) _Materials[SelectedTex.TexObjList[j].x].Textures.AlbedoTex = PackRect(RectSelect); 
                                else if(TempRect.TexType == 7) _Materials[SelectedTex.TexObjList[j].x].Textures.MatCapTex = PackRect(RectSelect); 
                                else if(TempRect.TexType == 9) _Materials[SelectedTex.TexObjList[j].x].Textures.SecondaryAlbedoTex = PackRect(RectSelect); 
                            break;
                            case 7:
                                _Materials[SelectedTex.TexObjList[j].x].Textures.AlphaTex = PackRect(RectSelect);
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
                    case 12:
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

        private void KeyCheck(int MatIndex, Texture Tex, ref Dictionary<int, TexObj> DictTextures, ref List<int> PR, int ReadChannelIndex, int TexType) {
            int index = Tex.GetInstanceID();
            if (DictTextures.TryGetValue(index, out var existingTexObj)) {
                existingTexObj.TexObjList.Add(new Vector3Int(MatIndex, TexType, ReadChannelIndex));
            } else {
                var newTexObj = new TexObj {
                    Tex = Tex,
                    ReadIndex = ReadChannelIndex
                };

                PR.Add(index);

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
                // MaterialsChanged.AddRange(Obj.ChildObjects);
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
                List<int> BindlessRect = new List<int>();
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



            if (CopyShader == null) CopyShader = Resources.Load<ComputeShader>("Utility/CopyTextureShader");

            if(RenderQue.Count == 0) return;

            foreach (ParentObject Obj in RenderQue) {
                foreach (RayTracingObject Obj2 in Obj.ChildObjects) {
                    Obj2.MatOffset = MatCount;
                }
                // MaterialsChanged.AddRange(Obj.ChildObjects);
                int ThisMatCount = Obj._Materials.Count;
                for(int i = 0; i < ThisMatCount; i++) {
                    MaterialData TempMat = Obj._Materials[i];
                    #if !DX11Only && !UseAtlas
                        if(TempMat.Textures.AlbedoTex.x != 0) KeyCheck(MatCount, Obj.AlbedoTexs[(int)TempMat.Textures.AlbedoTex.x-1], ref BindlessDict, ref BindlessRect, 4, 0);
                        if(TempMat.Textures.NormalTex.x != 0) KeyCheck(MatCount, Obj.NormalTexs[(int)TempMat.Textures.NormalTex.x-1], ref BindlessDict, ref BindlessRect, 4, 1);
                        if(TempMat.Textures.SecondaryNormalTex.x != 0) KeyCheck(MatCount, Obj.SecondaryNormalTexs[(int)TempMat.Textures.SecondaryNormalTex.x-1], ref BindlessDict, ref BindlessRect, 4, 11);
                        if(TempMat.Textures.EmissiveTex.x != 0) KeyCheck(MatCount, Obj.EmissionTexs[(int)TempMat.Textures.EmissiveTex.x-1], ref BindlessDict, ref BindlessRect, 4, 2);
                        if(TempMat.Textures.AlphaTex.x != 0) KeyCheck(MatCount, Obj.AlphaTexs[(int)TempMat.Textures.AlphaTex.x-1], ref BindlessDict, ref BindlessRect, Obj.AlphaTexChannelIndex[(int)TempMat.Textures.AlphaTex.x-1], 3);
                        if(TempMat.Textures.MetallicTex.x != 0) KeyCheck(MatCount, Obj.MetallicTexs[(int)TempMat.Textures.MetallicTex.x-1], ref BindlessDict, ref BindlessRect, Obj.MetallicTexChannelIndex[(int)TempMat.Textures.MetallicTex.x-1], 4);
                        if(TempMat.Textures.RoughnessTex.x != 0) KeyCheck(MatCount, Obj.RoughnessTexs[(int)TempMat.Textures.RoughnessTex.x-1], ref BindlessDict, ref BindlessRect, Obj.RoughnessTexChannelIndex[(int)TempMat.Textures.RoughnessTex.x-1], 5);
                        if(TempMat.Textures.MatCapMask.x != 0) KeyCheck(MatCount, Obj.MatCapMasks[(int)TempMat.Textures.MatCapMask.x-1], ref BindlessDict, ref BindlessRect, Obj.MatCapMaskChannelIndex[(int)TempMat.Textures.MatCapMask.x-1], 6);
                        if(TempMat.Textures.MatCapTex.x != 0) KeyCheck(MatCount, Obj.MatCapTexs[(int)TempMat.Textures.MatCapTex.x-1], ref BindlessDict, ref BindlessRect, 4, 7);
                        if(TempMat.Textures.SecondaryAlbedoTex.x != 0) KeyCheck(MatCount, Obj.SecondaryAlbedoTexs[(int)TempMat.Textures.SecondaryAlbedoTex.x-1], ref BindlessDict, ref BindlessRect, 4, 9);
                        if(TempMat.Textures.SecondaryAlbedoMask.x != 0) KeyCheck(MatCount, Obj.SecondaryAlbedoTexMasks[(int)TempMat.Textures.SecondaryAlbedoMask.x-1], ref BindlessDict, ref BindlessRect, Obj.SecondaryAlbedoTexMaskChannelIndex[(int)TempMat.Textures.SecondaryAlbedoMask.x-1], 10);
                        if(TempMat.Textures.DiffTransTex.x != 0) KeyCheck(MatCount, Obj.DiffTransTexs[(int)TempMat.Textures.DiffTransTex.x-1], ref BindlessDict, ref BindlessRect, Obj.DiffTransTexChannelIndex[(int)TempMat.Textures.DiffTransTex.x-1], 12);
                    #else
                        if(TempMat.Textures.AlbedoTex.x != 0) KeyCheck(MatCount, Obj.AlbedoTexs[(int)TempMat.Textures.AlbedoTex.x-1], ref AlbTextures, ref AlbRect, 0, 0);
                        if(TempMat.Textures.NormalTex.x != 0) KeyCheck(MatCount, Obj.NormalTexs[(int)TempMat.Textures.NormalTex.x-1], ref NormTextures, ref NormRect, 0, 1);
                        if(TempMat.Textures.SecondaryNormalTex.x != 0) KeyCheck(MatCount, Obj.SecondaryNormalTexs[(int)TempMat.Textures.SecondaryNormalTex.x-1], ref NormTextures, ref NormRect, 0, 11);
                        if(TempMat.Textures.EmissiveTex.x != 0) KeyCheck(MatCount, Obj.EmissionTexs[(int)TempMat.Textures.EmissiveTex.x-1], ref EmisTextures, ref EmisRect, 0, 2);
                        if(TempMat.Textures.AlphaTex.x != 0) KeyCheck(MatCount, Obj.AlphaTexs[(int)TempMat.Textures.AlphaTex.x-1], ref AlphTextures, ref AlphRect, Obj.AlphaTexChannelIndex[(int)TempMat.Textures.AlphaTex.x-1], 3);
                        if(TempMat.Textures.MetallicTex.x != 0) KeyCheck(MatCount, Obj.MetallicTexs[(int)TempMat.Textures.MetallicTex.x-1], ref SingleComponentTexture, ref SingleComponentRect, Obj.MetallicTexChannelIndex[(int)TempMat.Textures.MetallicTex.x-1], 4);
                        if(TempMat.Textures.RoughnessTex.x != 0) KeyCheck(MatCount, Obj.RoughnessTexs[(int)TempMat.Textures.RoughnessTex.x-1], ref SingleComponentTexture, ref SingleComponentRect, Obj.RoughnessTexChannelIndex[(int)TempMat.Textures.RoughnessTex.x-1], 5);
                        if(TempMat.Textures.MatCapMask.x != 0) KeyCheck(MatCount, Obj.MatCapMasks[(int)TempMat.Textures.MatCapMask.x-1], ref SingleComponentTexture, ref SingleComponentRect, Obj.MatCapMaskChannelIndex[(int)TempMat.Textures.MatCapMask.x-1], 6);
                        if(TempMat.Textures.MatCapTex.x != 0) KeyCheck(MatCount, Obj.MatCapTexs[(int)TempMat.Textures.MatCapTex.x-1], ref AlbTextures, ref AlbRect, 0, 7);
                        if(TempMat.Textures.SecondaryAlbedoTex.x != 0) KeyCheck(MatCount, Obj.SecondaryAlbedoTexs[(int)TempMat.Textures.SecondaryAlbedoTex.x-1], ref AlbTextures, ref AlbRect, 0, 9);
                        if(TempMat.Textures.SecondaryAlbedoMask.x != 0) KeyCheck(MatCount, Obj.SecondaryAlbedoTexMasks[(int)TempMat.Textures.SecondaryAlbedoMask.x-1], ref SingleComponentTexture, ref SingleComponentRect, Obj.SecondaryAlbedoTexMaskChannelIndex[(int)TempMat.Textures.SecondaryAlbedoMask.x-1], 10);
                        if(TempMat.Textures.DiffTransTex.x != 0) KeyCheck(MatCount, Obj.DiffTransTexs[(int)TempMat.Textures.DiffTransTex.x-1], ref SingleComponentTexture, ref SingleComponentRect, Obj.DiffTransTexChannelIndex[(int)TempMat.Textures.DiffTransTex.x-1], 12);
                    #endif
                    _Materials[MatCount] = TempMat;
                    MatCount++;
                }
            }
            foreach (ParentObject Obj in InstanceData.RenderQue) {
                foreach (RayTracingObject Obj2 in Obj.ChildObjects) {
                    Obj2.MatOffset = MatCount;
                }
                // MaterialsChanged.AddRange(Obj.ChildObjects);
                int ThisMatCount = Obj._Materials.Count;
                for(int i = 0; i < ThisMatCount; i++) {
                    MaterialData TempMat = Obj._Materials[i];
                    #if !DX11Only && !UseAtlas
                        if(TempMat.Textures.AlbedoTex.x != 0) KeyCheck(MatCount, Obj.AlbedoTexs[(int)TempMat.Textures.AlbedoTex.x-1], ref BindlessDict, ref BindlessRect, 4, 0);
                        if(TempMat.Textures.NormalTex.x != 0) KeyCheck(MatCount, Obj.NormalTexs[(int)TempMat.Textures.NormalTex.x-1], ref BindlessDict, ref BindlessRect, 4, 1);
                        if(TempMat.Textures.SecondaryNormalTex.x != 0) KeyCheck(MatCount, Obj.SecondaryNormalTexs[(int)TempMat.Textures.SecondaryNormalTex.x-1], ref BindlessDict, ref BindlessRect, 4, 11);
                        if(TempMat.Textures.EmissiveTex.x != 0) KeyCheck(MatCount, Obj.EmissionTexs[(int)TempMat.Textures.EmissiveTex.x-1], ref BindlessDict, ref BindlessRect, 4, 2);
                        if(TempMat.Textures.AlphaTex.x != 0) KeyCheck(MatCount, Obj.AlphaTexs[(int)TempMat.Textures.AlphaTex.x-1], ref BindlessDict, ref BindlessRect, Obj.AlphaTexChannelIndex[(int)TempMat.Textures.AlphaTex.x-1], 3);
                        if(TempMat.Textures.MetallicTex.x != 0) KeyCheck(MatCount, Obj.MetallicTexs[(int)TempMat.Textures.MetallicTex.x-1], ref BindlessDict, ref BindlessRect, Obj.MetallicTexChannelIndex[(int)TempMat.Textures.MetallicTex.x-1], 4);
                        if(TempMat.Textures.RoughnessTex.x != 0) KeyCheck(MatCount, Obj.RoughnessTexs[(int)TempMat.Textures.RoughnessTex.x-1], ref BindlessDict, ref BindlessRect, Obj.RoughnessTexChannelIndex[(int)TempMat.Textures.RoughnessTex.x-1], 5);
                        if(TempMat.Textures.MatCapMask.x != 0) KeyCheck(MatCount, Obj.MatCapMasks[(int)TempMat.Textures.MatCapMask.x-1], ref BindlessDict, ref BindlessRect, Obj.MatCapMaskChannelIndex[(int)TempMat.Textures.MatCapMask.x-1], 6);
                        if(TempMat.Textures.MatCapTex.x != 0) KeyCheck(MatCount, Obj.MatCapTexs[(int)TempMat.Textures.MatCapTex.x-1], ref BindlessDict, ref BindlessRect, 4, 7);
                        if(TempMat.Textures.SecondaryAlbedoTex.x != 0) KeyCheck(MatCount, Obj.SecondaryAlbedoTexs[(int)TempMat.Textures.SecondaryAlbedoTex.x-1], ref BindlessDict, ref BindlessRect, 4, 9);
                        if(TempMat.Textures.SecondaryAlbedoMask.x != 0) KeyCheck(MatCount, Obj.SecondaryAlbedoTexMasks[(int)TempMat.Textures.SecondaryAlbedoMask.x-1], ref BindlessDict, ref BindlessRect, Obj.SecondaryAlbedoTexMaskChannelIndex[(int)TempMat.Textures.SecondaryAlbedoMask.x-1], 10);
                        if(TempMat.Textures.DiffTransTex.x != 0) KeyCheck(MatCount, Obj.DiffTransTexs[(int)TempMat.Textures.DiffTransTex.x-1], ref BindlessDict, ref BindlessRect, Obj.DiffTransTexChannelIndex[(int)TempMat.Textures.DiffTransTex.x-1], 12);
                    #else
                        if(TempMat.Textures.AlbedoTex.x != 0) KeyCheck(MatCount, Obj.AlbedoTexs[(int)TempMat.Textures.AlbedoTex.x-1], ref AlbTextures, ref AlbRect, 0, 0);
                        if(TempMat.Textures.NormalTex.x != 0) KeyCheck(MatCount, Obj.NormalTexs[(int)TempMat.Textures.NormalTex.x-1], ref NormTextures, ref NormRect, 0, 1);
                        if(TempMat.Textures.SecondaryNormalTex.x != 0) KeyCheck(MatCount, Obj.SecondaryNormalTexs[(int)TempMat.Textures.SecondaryNormalTex.x-1], ref NormTextures, ref NormRect, 0, 11);
                        if(TempMat.Textures.EmissiveTex.x != 0) KeyCheck(MatCount, Obj.EmissionTexs[(int)TempMat.Textures.EmissiveTex.x-1], ref EmisTextures, ref EmisRect, 0, 2);
                        if(TempMat.Textures.AlphaTex.x != 0) KeyCheck(MatCount, Obj.AlphaTexs[(int)TempMat.Textures.AlphaTex.x-1], ref AlphTextures, ref AlphRect, Obj.AlphaTexChannelIndex[(int)TempMat.Textures.AlphaTex.x-1], 3);
                        if(TempMat.Textures.MetallicTex.x != 0) KeyCheck(MatCount, Obj.MetallicTexs[(int)TempMat.Textures.MetallicTex.x-1], ref SingleComponentTexture, ref SingleComponentRect, Obj.MetallicTexChannelIndex[(int)TempMat.Textures.MetallicTex.x-1], 4);
                        if(TempMat.Textures.RoughnessTex.x != 0) KeyCheck(MatCount, Obj.RoughnessTexs[(int)TempMat.Textures.RoughnessTex.x-1], ref SingleComponentTexture, ref SingleComponentRect, Obj.RoughnessTexChannelIndex[(int)TempMat.Textures.RoughnessTex.x-1], 5);
                        if(TempMat.Textures.MatCapMask.x != 0) KeyCheck(MatCount, Obj.MatCapMasks[(int)TempMat.Textures.MatCapMask.x-1], ref SingleComponentTexture, ref SingleComponentRect, Obj.MatCapMaskChannelIndex[(int)TempMat.Textures.MatCapMask.x-1], 6);
                        if(TempMat.Textures.MatCapTex.x != 0) KeyCheck(MatCount, Obj.MatCapTexs[(int)TempMat.Textures.MatCapTex.x-1], ref AlbTextures, ref AlbRect, 0, 7);
                        if(TempMat.Textures.SecondaryAlbedoTex.x != 0) KeyCheck(MatCount, Obj.SecondaryAlbedoTexs[(int)TempMat.Textures.SecondaryAlbedoTex.x-1], ref AlbTextures, ref AlbRect, 0, 9);
                        if(TempMat.Textures.SecondaryAlbedoMask.x != 0) KeyCheck(MatCount, Obj.SecondaryAlbedoTexMasks[(int)TempMat.Textures.SecondaryAlbedoMask.x-1], ref SingleComponentTexture, ref SingleComponentRect, Obj.SecondaryAlbedoTexMaskChannelIndex[(int)TempMat.Textures.SecondaryAlbedoMask.x-1], 10);
                        if(TempMat.Textures.DiffTransTex.x != 0) KeyCheck(MatCount, Obj.DiffTransTexs[(int)TempMat.Textures.DiffTransTex.x-1], ref SingleComponentTexture, ref SingleComponentRect, Obj.DiffTransTexChannelIndex[(int)TempMat.Textures.DiffTransTex.x-1], 12);
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
                            if(TempMat.Textures.AlbedoTex.x != 0) KeyCheck(TerrainMatCount + i, Obj2.AlbedoTexs[(int)TempMat.Textures.AlbedoTex.x-1], ref BindlessDict, ref BindlessRect, 4, 0);
                            if(TempMat.Textures.NormalTex.x != 0) KeyCheck(TerrainMatCount + i, Obj2.NormalTexs[(int)TempMat.Textures.NormalTex.x-1], ref BindlessDict, ref BindlessRect, 4, 1);
                            if(TempMat.Textures.MetallicTex.x != 0) KeyCheck(TerrainMatCount + i, Obj2.MaskTexs[(int)TempMat.Textures.MetallicTex.x-1], ref BindlessDict, ref BindlessRect, 2, 4);
                            if(TempMat.Textures.RoughnessTex.x != 0) KeyCheck(TerrainMatCount + i, Obj2.MaskTexs[(int)TempMat.Textures.RoughnessTex.x-1], ref BindlessDict, ref BindlessRect, 1, 5);
                        #else
                            if(TempMat.Textures.AlbedoTex.x != 0) KeyCheck(TerrainMatCount + i, Obj2.AlbedoTexs[(int)TempMat.Textures.AlbedoTex.x-1], ref AlbTextures, ref AlbRect, 0, 0);
                            if(TempMat.Textures.NormalTex.x != 0) KeyCheck(TerrainMatCount + i, Obj2.NormalTexs[(int)TempMat.Textures.NormalTex.x-1], ref NormTextures, ref NormRect, 0, 1);
                            if(TempMat.Textures.MetallicTex.x != 0) KeyCheck(TerrainMatCount + i, Obj2.MaskTexs[(int)TempMat.Textures.MetallicTex.x-1], ref SingleComponentTexture, ref SingleComponentRect, 2, 4);
                            if(TempMat.Textures.RoughnessTex.x != 0) KeyCheck(TerrainMatCount + i, Obj2.MaskTexs[(int)TempMat.Textures.RoughnessTex.x-1], ref SingleComponentTexture, ref SingleComponentRect, 1, 5);
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
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        void OnSceneUnloaded(Scene scene) {
            ClearAll();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if(this != null) gameObject.GetComponent<RayTracingMaster>().Start2();
        }

        public void ForceUpdateAtlas() {
            int throwaway = 0;
            int throwaway2 = 0;
            CommandBuffer tempcmd = new CommandBuffer();
            for(int i = 0; i < RenderQue.Count; i++) RenderQue[i].CreateAtlas(ref throwaway, ref throwaway2);
            CreateAtlas(throwaway, tempcmd);
            Graphics.ExecuteCommandBuffer(tempcmd);
            tempcmd.Clear();
            tempcmd.Release();
        }

        public void OnApplicationQuit() {
            RunningTasks = 0;
            ClearAll();
        }

        void OnDisable() {
            ClearAll();
            bindlessTextures?.Dispose();
            bindlessTextures = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
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
                RoughnessRemapMax = "null",
                EmissionIntensityValue = "null",
                EmissionColorValue = "null"
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
            LightTLASRefitKernel = Refitter.FindKernel("TLASLightBVHRefitKernel");
            RefitLayer = Refitter.FindKernel("RefitBVHLayer");
            
            RefitCopy = Refitter.FindKernel("BLASCopyNodeDataKernel");

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
            LightTreeBufferA.ReleaseSafe();
            // LightTreeBufferB.ReleaseSafe();
            BVH8AggregatedBuffer.ReleaseSafe();
            AggTriBufferA.ReleaseSafe();
            SkinnedMeshAggTriBufferPrev.ReleaseSafe();
            AggTriBufferB.ReleaseSafe();
            MeshFunctions = Resources.Load<ComputeShader>("Utility/GeneralMeshFunctions");
            TriangleBufferKernel = MeshFunctions.FindKernel("CombineTriBuffers");
            NodeBufferKernel = MeshFunctions.FindKernel("CombineNodeBuffers");
            LightBufferKernel = MeshFunctions.FindKernel("CombineLightBuffers");
            LightTreeNodeBufferKernel = MeshFunctions.FindKernel("CombineSGTreeNodes");

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
#if TTVerbose
                        Debug.LogError(BuildQue[i].AsyncTask.Exception + ", " + BuildQue[i].Name);
#endif
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
                        default: Debug.LogError("Report this to the developer"); break;
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


            if (OnlyInstanceUpdated && !ChildrenUpdated) ChildrenUpdated = true;
            else OnlyInstanceUpdated = false;

            if (ChildrenUpdated || ParentCountHasChanged) {
                MeshAABBs = new AABB[RenderQue.Count + InstanceRenderQue.Count];
                TransformedAABBs = new AABB[RenderQue.Count + InstanceRenderQue.Count];
            }
        }

        public void EditorBuild()
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
            BuildQue = new List<ParentObject>(GameObject.FindObjectsOfType<ParentObject>());
            List<ParentObject> TempQue = new List<ParentObject>(InstanceData.GetComponentsInChildren<ParentObject>());
            int QueCount = TempQue.Count;
            for(int i = 0; i < QueCount; i++) {
                int Index = BuildQue.IndexOf(TempQue[i]);
                if(Index != -1) BuildQue.RemoveAt(Index);
            }
            RunningTasks = 0;
            InstanceData.EditorBuild();
            QueCount = BuildQue.Count;
            for(int i = QueCount - 1; i >= 0; i--) {
                if(!BuildQue[i].enabled) BuildQue.RemoveAt(i);
            }

            QueCount = BuildQue.Count;
            RunningTasks = QueCount;
            for (int i = 0; i < QueCount; i++)
            {
                var CurrentRep = i;
                BuildQue[CurrentRep].LoadData();
                // BuildQue[CurrentRep].BuildTotal();
                BuildQue[CurrentRep].AsyncTask = Task.Run(() => { BuildQue[CurrentRep].BuildTotal(); RunningTasks--; });
                BuildQue[CurrentRep].ExistsInQue = 1;
                BuildQue[CurrentRep].QueInProgress = 1;
            }
            didstart = false;
        }
        
        public void BuildCombined()
        {//Only has unbuilt be built
            Assets = this;
            if(!Application.isPlaying) ClearAll();
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

        int LightNodeCount = 0;
        private void AccumulateData(CommandBuffer cmd)
        {
            // UnityEngine.Profiling.Profiler.BeginSample("Update Object Lists");
            UpdateRenderAndBuildQues();
            // UnityEngine.Profiling.Profiler.EndSample();

            int ParentsLength = RenderQue.Count;
            if (ChildrenUpdated || ParentCountHasChanged || MaterialBuffer == null)
            {
                int CurNodeOffset = 2 * (ParentsLength + InstanceRenderQue.Count);
                int AggTriCount = 0;
                int AggNodeCount = CurNodeOffset;
                int LightTriCount = 0;
                int CurTriOffset = 0;
                int CurLightTriOffset = 0;
                int CurSGNodeOffset = 0;
                int CurSGNodeSkinnedOffset = 0;
                TotalParentObjectSize = 0;
                LightMeshCount = 0;
                LightMeshes.Clear();
                LightTransforms.Clear();
                int TotalMatCount = 0;
                int AggSGTreeNodeCount = 0;
                int AggSGTreeSKINNEDNodeCount = 0;
                int SkinnedMeshTriCount = 0;
                int SkinnedMeshTriOffset = 0;

                for(int i = 0; i < ParentsLength; i++) {
                    if (RenderQue[i].HasLightTriangles) {
                        if(RenderQue[i].IsSkinnedGroup) {
                            AggSGTreeSKINNEDNodeCount += RenderQue[i].LBVH.NodeCount;
                        } 
                        LightMeshCount++; 
                        AggSGTreeNodeCount += RenderQue[i].LBVH.NodeCount;
                    }
                }
                int InstanceQueCount = InstanceRenderQue.Count;
                for (int i = 0; i < InstanceQueCount; i++) {if (InstanceRenderQue[i].InstanceParent.HasLightTriangles) LightMeshCount++;}


                
                if (BVH8AggregatedBuffer != null)
                {
#if StrictMemoryReduction
                    BVH8AggregatedBuffer.Release();
                    AggTriBufferA.Release();
                    SkinnedMeshAggTriBufferPrev.ReleaseSafe();
                    AggTriBufferB.Release();
                    LightTriBuffer.Release();
                    if(LightTreeBufferA != null) LightTreeBufferA.Release();
                    // if(LightTreeBufferB != null) LightTreeBufferB.Release();
#endif
                }
                for (int i = 0; i < ParentsLength; i++)
                {
                    TotalMatCount += RenderQue[i]._Materials.Count;
                    AggNodeCount += RenderQue[i].AggNodes.Length;
                    AggTriCount += RenderQue[i].AggTriangles.Length;
                    LightTriCount += RenderQue[i].LightTriangles.Count;
                    if(RenderQue[i].IsSkinnedGroup || RenderQue[i].IsDeformable) SkinnedMeshTriCount += RenderQue[i].AggTriangles.Length;
                }
                InstanceQueCount = InstanceData.RenderQue.Count;
                for (int i = 0; i < InstanceQueCount; i++)
                {
                    TotalMatCount += InstanceData.RenderQue[i]._Materials.Count;
                    AggNodeCount += InstanceData.RenderQue[i].AggNodes.Length;
                    AggTriCount += InstanceData.RenderQue[i].AggTriangles.Length;
                    LightTriCount += InstanceData.RenderQue[i].LightTriangles.Count;
                    if (InstanceData.RenderQue[i].LightTriangles.Count != 0) AggSGTreeNodeCount += InstanceData.RenderQue[i].LBVH.NodeCount;
                }

                CurSGNodeOffset = 2 * (LightMeshCount) * 2;
                LightTreePrimaryTLASOffset = 2 * (LightMeshCount);
                AggSGTreeNodeCount += CurSGNodeOffset;
#if TTVerbose
                Debug.Log("Light Tri Count: " + LightTriCount);
                Debug.Log("Total Tri Count: " + AggTriCount);
#endif
                if(LightTriCount == 0) {LightTriCount++; AggSGTreeNodeCount++;}
                if(AggSGTreeSKINNEDNodeCount == 0) AggSGTreeSKINNEDNodeCount = 1;
                if (AggNodeCount != 0)
                {//Accumulate the BVH nodes and triangles for all normal models
                    if(MeshFunctions == null) MeshFunctions = Resources.Load<ComputeShader>("Utility/GeneralMeshFunctions");
                    LightAABBs = new LightBounds[LightMeshCount];
                    LightNodeCount = LightMeshCount * 2;
                    #if !DontUseSGTree
                        SGTreeNodes = new GaussianTreeNode[LightMeshCount];
                        SGTree = new GaussianTreeNode[LightMeshCount * 2];
                    #endif
                    bool ResizedTriArray = false;
                    bool ResizedBVHArray = false;
                    bool ResizedLightTriArray = false;
                    bool ResizedLightBVHArray = false;
#if StrictMemoryReduction
                    ResizedLightBVHArray = true;
                    ResizedLightTriArray = true;
                    ResizedTriArray = true;
                    ResizedBVHArray = true;

                    CommonFunctions.CreateDynamicBuffer(ref BVH8AggregatedBuffer, AggNodeCount, 80);
                    CommonFunctions.CreateDynamicBuffer(ref AggTriBufferA, AggTriCount, CommonFunctions.GetStride<CudaTriangleA>());
                    CommonFunctions.CreateDynamicBuffer(ref SkinnedMeshAggTriBufferPrev, (int)Mathf.Max(SkinnedMeshTriCount,1), 36);
                    CommonFunctions.CreateDynamicBuffer(ref AggTriBufferB, AggTriCount, CommonFunctions.GetStride<CudaTriangleB>());
                    CommonFunctions.CreateDynamicBuffer(ref LightTriBuffer, LightTriCount, CommonFunctions.GetStride<LightTriData>());
    #if !DontUseSGTree
                    CommonFunctions.CreateDynamicBuffer(ref LightTreeBufferA, AggSGTreeNodeCount + AggSGTreeSKINNEDNodeCount, CommonFunctions.GetStride<GaussianTreeNode>());
    #else
                    CommonFunctions.CreateDynamicBuffer(ref LightTreeBufferA, AggSGTreeNodeCount + AggSGTreeSKINNEDNodeCount, CommonFunctions.GetStride<CompactLightBVHData>());
    #endif
#else
                    if(BVH8AggregatedBuffer == null || !BVH8AggregatedBuffer.IsValid() || AggNodeCount > BVH8AggregatedBuffer.count) {CommonFunctions.CreateDynamicBuffer(ref BVH8AggregatedBuffer, AggNodeCount, 80); ResizedBVHArray = true;}
                    if(AggTriBufferA == null || !AggTriBufferA.IsValid() || AggTriCount > AggTriBufferA.count) {CommonFunctions.CreateDynamicBuffer(ref AggTriBufferA, AggTriCount, CommonFunctions.GetStride<CudaTriangleA>()); ResizedTriArray = true;}
                    if(SkinnedMeshAggTriBufferPrev == null || !SkinnedMeshAggTriBufferPrev.IsValid() || (int)Mathf.Max(SkinnedMeshTriCount,1) > SkinnedMeshAggTriBufferPrev.count) CommonFunctions.CreateDynamicBuffer(ref SkinnedMeshAggTriBufferPrev, (int)Mathf.Max(SkinnedMeshTriCount,1), 36);
                    if(AggTriBufferB == null || !AggTriBufferB.IsValid() || AggTriCount > AggTriBufferB.count) CommonFunctions.CreateDynamicBuffer(ref AggTriBufferB, AggTriCount, CommonFunctions.GetStride<CudaTriangleB>());
                    if(LightTriBuffer == null || !LightTriBuffer.IsValid() || LightTriCount > LightTriBuffer.count) {CommonFunctions.CreateDynamicBuffer(ref LightTriBuffer, LightTriCount, CommonFunctions.GetStride<LightTriData>()); ResizedLightTriArray = true;}
    #if !DontUseSGTree
                    if(LightTreeBufferA == null || !LightTreeBufferA.IsValid() || AggSGTreeNodeCount + AggSGTreeSKINNEDNodeCount > LightTreeBufferA.count) {CommonFunctions.CreateDynamicBuffer(ref LightTreeBufferA, AggSGTreeNodeCount + AggSGTreeSKINNEDNodeCount, CommonFunctions.GetStride<GaussianTreeNode>()); ResizedLightBVHArray = true;}
    #else
                    if(LightTreeBufferA == null || !LightTreeBufferA.IsValid() || AggSGTreeNodeCount + AggSGTreeSKINNEDNodeCount > LightTreeBufferA.count) {CommonFunctions.CreateDynamicBuffer(ref LightTreeBufferA, AggSGTreeNodeCount + AggSGTreeSKINNEDNodeCount, CommonFunctions.GetStride<CompactLightBVHData>()); ResizedLightBVHArray = true;}
    #endif
#endif
                    MeshFunctions.SetBuffer(TriangleBufferKernel, "OutCudaTriArrayA", AggTriBufferA);
                    MeshFunctions.SetBuffer(TriangleBufferKernel, "OutCudaTriArrayB", AggTriBufferB);
                    MeshFunctions.SetBuffer(NodeBufferKernel, "OutAggNodes", BVH8AggregatedBuffer);
                    MeshFunctions.SetBuffer(LightBufferKernel, "LightTrianglesOut", LightTriBuffer);
                    
                    CurSGNodeSkinnedOffset = AggSGTreeNodeCount;


                    int MatOffset = 0;
                    int CurLightMesh = 0;
                    for (int i = 0; i < ParentsLength; i++)
                    {
                        RenderQue[i].UpdateData();
                        int TempI = i;
                        if(ResizedTriArray || RenderQue[i].GlobalTriOffset != CurTriOffset) {
                            int TempOffset = CurTriOffset;
                            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("AccumBufferTri");
                            cmd.SetComputeIntParam(MeshFunctions, "Offset", TempOffset);
                            cmd.SetComputeIntParam(MeshFunctions, "Count", RenderQue[TempI].LocalTriCount);
                            cmd.SetComputeBufferParam(MeshFunctions, TriangleBufferKernel, "InCudaTriArray", RenderQue[TempI].TriBuffer);
                            cmd.DispatchCompute(MeshFunctions, TriangleBufferKernel, (int)Mathf.Ceil(RenderQue[TempI].LocalTriCount / 372.0f), 1, 1);
                            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("AccumBufferTri");
                        }
                        if(ResizedBVHArray || RenderQue[i].GlobalNodeOffset != CurNodeOffset) {
                            int TempOffset = CurNodeOffset;
                            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("AccumBufferNode");
                            cmd.SetComputeIntParam(MeshFunctions, "Offset", TempOffset);
                            cmd.SetComputeIntParam(MeshFunctions, "Count", RenderQue[TempI].LocalNodeCount);
                            cmd.SetComputeBufferParam(MeshFunctions, NodeBufferKernel, "InAggNodes", RenderQue[TempI].BVHBuffer);
                            cmd.DispatchCompute(MeshFunctions, NodeBufferKernel, (int)Mathf.Ceil(RenderQue[TempI].LocalNodeCount / 372.0f), 1, 1);
                            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("AccumBufferNode");
                        }

                        if (RenderQue[i].HasLightTriangles) {
                            if(ResizedLightTriArray || RenderQue[i].GlobalLightTriOffset != CurLightTriOffset) {
                                int TempOffset = CurLightTriOffset;
                                cmd.SetComputeIntParam(MeshFunctions, "Offset", TempOffset);
                                cmd.SetComputeIntParam(MeshFunctions, "Count", RenderQue[TempI].LocalLightTriCount);
                                cmd.SetComputeBufferParam(MeshFunctions, LightBufferKernel, "LightTrianglesIn", RenderQue[TempI].LightTriBuffer);
                                cmd.DispatchCompute(MeshFunctions, LightBufferKernel, (int)Mathf.Ceil(RenderQue[TempI].LocalLightTriCount / 372.0f), 1, 1);
                            }
                            if(ResizedLightBVHArray || RenderQue[i].GlobalLightNodeOffset != CurSGNodeOffset) {
                                int TempOffset = CurSGNodeOffset;
                                cmd.SetComputeIntParam(MeshFunctions, "Offset", TempOffset);
                                cmd.SetComputeIntParam(MeshFunctions, "Count", RenderQue[TempI].LocalLightNodeCount);
                                cmd.SetComputeBufferParam(MeshFunctions, LightTreeNodeBufferKernel, "LightNodesOut", LightTreeBufferA);
                                cmd.SetComputeBufferParam(MeshFunctions, LightTreeNodeBufferKernel, "LightNodesIn", RenderQue[TempI].LightTreeBuffer);
                                cmd.DispatchCompute(MeshFunctions, LightTreeNodeBufferKernel, (int)Mathf.Ceil(RenderQue[TempI].LocalLightNodeCount / 372.0f), 1, 1);
                            }
                            if(RenderQue[TempI].IsSkinnedGroup && (ResizedLightBVHArray || RenderQue[i].GlobalLightNodeSkinnedOffset != CurSGNodeSkinnedOffset)) {
                                int TempOffset = CurSGNodeSkinnedOffset;
                                cmd.SetComputeIntParam(MeshFunctions, "Offset", TempOffset);
                                cmd.SetComputeIntParam(MeshFunctions, "Count", RenderQue[TempI].LocalLightNodeCount);
                                cmd.SetComputeBufferParam(MeshFunctions, LightTreeNodeBufferKernel, "LightNodesOut", LightTreeBufferA);
                                cmd.SetComputeBufferParam(MeshFunctions, LightTreeNodeBufferKernel, "LightNodesIn", RenderQue[TempI].LightTreeBuffer);
                                cmd.DispatchCompute(MeshFunctions, LightTreeNodeBufferKernel, (int)Mathf.Ceil(RenderQue[TempI].LocalLightNodeCount / 372.0f), 1, 1);
                            }
                                                        
                            LightTransforms.Add(RenderTransforms[i]);
                            LightMeshes.Add(new LightMeshData() {
                                StartIndex = CurLightTriOffset,
                                IndexEnd = RenderQue[i].LightTriangles.Count + CurLightTriOffset,
                                MatOffset = MatOffset,
                                LockedMeshIndex = i
                            });
                            try {
#if !DontUseSGTree
                                SGTreeNodes[CurLightMesh] = RenderQue[i].LBVH.SGTree[0];
#endif
                                LightAABBs[CurLightMesh] = RenderQue[i].LBVH.ParentBound.aabb;
                            } catch(System.Exception e) {Debug.Log("BROKEN FUCKER: " + RenderQue[i].Name + ", " + e);}

                            if(RenderQue[i].IsSkinnedGroup) {
                                RenderQue[i].GlobalLightNodeSkinnedOffset = CurSGNodeSkinnedOffset;
                                CurSGNodeSkinnedOffset += RenderQue[i].LocalLightNodeCount;
                            }
                            RenderQue[i].GlobalLightTriOffset = CurLightTriOffset;
                            RenderQue[i].GlobalLightNodeOffset = CurSGNodeOffset;
                            CurSGNodeOffset += RenderQue[i].LocalLightNodeCount;
                            CurLightTriOffset += RenderQue[i].LocalLightTriCount;
                            TotalParentObjectSize += RenderQue[i].LocalLightTriCount * RenderQue[i].LightTriBuffer.stride;
                            CurLightMesh++;
                        }
                        TotalParentObjectSize += RenderQue[i].LocalTriCount * RenderQue[i].TriBuffer.stride;
                        TotalParentObjectSize += RenderQue[i].LocalNodeCount * RenderQue[i].BVHBuffer.stride;
                        RenderQue[i].GlobalNodeOffset = CurNodeOffset;
                        RenderQue[i].GlobalTriOffset = CurTriOffset;
                        if(RenderQue[i].IsSkinnedGroup || RenderQue[i].IsDeformable) {
                            RenderQue[i].GlobalSkinnedOffset = SkinnedMeshTriOffset;
                            SkinnedMeshTriOffset += RenderQue[i].LocalTriCount;
                        } else RenderQue[i].GlobalSkinnedOffset = -1;
                        CurNodeOffset += RenderQue[i].LocalNodeCount;
                        CurTriOffset += RenderQue[i].LocalTriCount;
                        MatOffset += RenderQue[i]._Materials.Count;
                    }
                    InstanceQueCount = InstanceData.RenderQue.Count;
                    for (int i = 0; i < InstanceQueCount; i++)
                    {//Accumulate the BVH nodes and triangles for all instanced models
                        InstanceData.RenderQue[i].UpdateData();
                        InstanceData.RenderQue[i].InstanceMeshIndex = i + ParentsLength;

                        if(ResizedTriArray || InstanceData.RenderQue[i].GlobalTriOffset != CurTriOffset) {
                            int TempOffset = CurTriOffset;
                            int TempI = i;
                            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("AccumBufferInstanceTri");
                            cmd.SetComputeIntParam(MeshFunctions, "Offset", TempOffset);
                            cmd.SetComputeIntParam(MeshFunctions, "Count", InstanceData.RenderQue[TempI].LocalTriCount);
                            cmd.SetComputeBufferParam(MeshFunctions, TriangleBufferKernel, "InCudaTriArray", InstanceData.RenderQue[TempI].TriBuffer);
                            cmd.DispatchCompute(MeshFunctions, TriangleBufferKernel, (int)Mathf.Ceil(InstanceData.RenderQue[TempI].LocalTriCount / 372.0f), 1, 1);
                            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("AccumBufferInstanceTri");
                        }
                        if(ResizedBVHArray || InstanceData.RenderQue[i].GlobalNodeOffset != CurNodeOffset) {
                            int TempOffset = CurNodeOffset;
                            int TempI = i;
                            if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("AccumBufferInstanceNode");
                            cmd.SetComputeIntParam(MeshFunctions, "Offset", TempOffset);
                            cmd.SetComputeIntParam(MeshFunctions, "Count", InstanceData.RenderQue[TempI].LocalNodeCount);
                            cmd.SetComputeBufferParam(MeshFunctions, NodeBufferKernel, "InAggNodes", InstanceData.RenderQue[TempI].BVHBuffer);
                            cmd.DispatchCompute(MeshFunctions, NodeBufferKernel, (int)Mathf.Ceil(InstanceData.RenderQue[TempI].LocalNodeCount / 372.0f), 1, 1);
                            if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("AccumBufferInstanceNode");
                        }
                        if (InstanceData.RenderQue[i].HasLightTriangles) {
                            if(ResizedLightTriArray || InstanceData.RenderQue[i].GlobalLightTriOffset != CurLightTriOffset) {
                                int TempOffset = CurLightTriOffset;
                                int TempI = i;
                                cmd.SetComputeIntParam(MeshFunctions, "Offset", TempOffset);
                                cmd.SetComputeIntParam(MeshFunctions, "Count", InstanceData.RenderQue[TempI].LocalLightTriCount);
                                cmd.SetComputeBufferParam(MeshFunctions, LightBufferKernel, "LightTrianglesIn", InstanceData.RenderQue[TempI].LightTriBuffer);
                                cmd.DispatchCompute(MeshFunctions, LightBufferKernel, (int)Mathf.Ceil(InstanceData.RenderQue[TempI].LocalLightTriCount / 372.0f), 1, 1);
                            }

                            if(ResizedLightBVHArray || InstanceData.RenderQue[i].GlobalLightNodeOffset != CurSGNodeOffset) {
                                int TempOffset = CurSGNodeOffset;
                                int TempI = i;
                                cmd.SetComputeIntParam(MeshFunctions, "Offset", TempOffset);
                                cmd.SetComputeIntParam(MeshFunctions, "Count", InstanceData.RenderQue[TempI].LocalLightNodeCount);
                                cmd.SetComputeBufferParam(MeshFunctions, LightTreeNodeBufferKernel, "LightNodesIn", InstanceData.RenderQue[TempI].LightTreeBuffer);
                                cmd.DispatchCompute(MeshFunctions, LightTreeNodeBufferKernel, (int)Mathf.Ceil(InstanceData.RenderQue[TempI].LocalLightNodeCount / 372.0f), 1, 1);
                            }

                            InstanceData.RenderQue[i].GlobalLightTriOffset = CurLightTriOffset;
                            CurLightTriOffset += InstanceData.RenderQue[i].LocalLightTriCount;
                            InstanceData.RenderQue[i].LightEndIndex = CurLightTriOffset;
                            InstanceData.RenderQue[i].GlobalLightNodeOffset = CurSGNodeOffset;
                            CurSGNodeOffset += InstanceData.RenderQue[i].LocalLightNodeCount;
                        }

                        InstanceData.RenderQue[i].GlobalNodeOffset = CurNodeOffset;
                        InstanceData.RenderQue[i].GlobalSkinnedOffset = -1;
                        InstanceData.RenderQue[i].GlobalTriOffset = CurTriOffset;
                        CurNodeOffset += InstanceData.RenderQue[i].LocalNodeCount;
                        CurTriOffset += InstanceData.RenderQue[i].LocalTriCount;
                        MatOffset += InstanceData.RenderQue[i]._Materials.Count;

                    }
                    InstanceQueCount = InstanceRenderQue.Count;
                    for (int i = 0; i < InstanceQueCount; i++) {
                        if (InstanceRenderQue[i].InstanceParent.HasLightTriangles) {
                            InstanceRenderQue[i].LightIndex = LightMeshes.Count;
                            LightTransforms.Add(InstanceRenderTransforms[i]);
                            LightMeshes.Add(new LightMeshData() {
                                LockedMeshIndex = i + ParentsLength,
                                StartIndex = InstanceRenderQue[i].InstanceParent.GlobalLightTriOffset,
                                IndexEnd = InstanceRenderQue[i].InstanceParent.LightEndIndex
                            });
#if !DontUseSGTree
                            SGTreeNodes[CurLightMesh] = InstanceRenderQue[i].InstanceParent.LBVH.SGTree[0];
#endif
                            LightAABBs[CurLightMesh] = InstanceRenderQue[i].InstanceParent.LBVH.ParentBound.aabb;
                            CurLightMesh++;
                        }
                    }
                }

                if (LightMeshCount == 0) { LightMeshes.Add(new LightMeshData() { }); }

                if (!OnlyInstanceUpdated || (_Materials == null || _Materials.Length == 0) || (ChildrenUpdated && ParentCountHasChanged && OnlyInstanceUpdated)) {
                    CreateAtlas(TotalMatCount, cmd);
                    int MatLeng = _Materials.Length;
                    IntersectionMats = new IntersectionMatData[MatLeng];
                    for(int i = 0; i < MatLeng; i++) {
                        IntersectionMats[i].AlphaTex = _Materials[i].Textures.AlphaTex;
                        IntersectionMats[i].AlbedoTex = _Materials[i].Textures.AlbedoTex;
                        IntersectionMats[i].Tag = _Materials[i].MatData.Tag;
                        IntersectionMats[i].MatType = _Materials[i].MatData.MatType;
                        IntersectionMats[i].specTrans = _Materials[i].MatData.SpecTrans;
                        IntersectionMats[i].AlphaCutoff = _Materials[i].MatData.AlphaCutoff;
                        IntersectionMats[i].AlbedoTexScale = _Materials[i].MatData.TextureModifiers.MainTexScaleOffset;
                        IntersectionMats[i].surfaceColor = _Materials[i].MatData.BaseColor;
                        IntersectionMats[i].Rotation = _Materials[i].MatData.TextureModifiers.Rotation;
                        IntersectionMats[i].scatterDistance = _Materials[i].MatData.ScatterDist;
                    }

#if StrictMemoryReduction
                    CommonFunctions.CreateComputeBuffer(ref IntersectionMaterialBuffer, IntersectionMats);
                    CommonFunctions.CreateComputeBuffer(ref MaterialBuffer, _Materials);
#else
                    if(IntersectionMaterialBuffer == null || !IntersectionMaterialBuffer.IsValid() || IntersectionMats.Length > IntersectionMaterialBuffer.count) CommonFunctions.CreateComputeBuffer(ref IntersectionMaterialBuffer, IntersectionMats);
                    else IntersectionMaterialBuffer.SetData(IntersectionMats, 0, 0, IntersectionMats.Length);
                    if(MaterialBuffer == null || !MaterialBuffer.IsValid() || MatLeng > MaterialBuffer.count) CommonFunctions.CreateComputeBuffer(ref MaterialBuffer, _Materials);
                    else MaterialBuffer.SetData(_Materials, 0, 0, MatLeng);
#endif
                }
            }
            ParentCountHasChanged = false;
            {//BINDLESS-TEST this spot is guarenteed to run once per frame, be very close to the begining of the commandbuffer, and is guarenteed to have the AlbedoArray filled
            #if !DX11Only && !UseAtlas
                Shader.SetGlobalTexture("_BindlessTextures", Texture2D.whiteTexture);
                if(bindlessTextures != null) bindlessTextures.UpdateDescriptors();
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


        int MaxRecur = 0;
        int TempRecur = 0;
        [HideInInspector] public int TotalCounter = 0;
        [HideInInspector] public List<Layer2> WorkingSetCWBVH;
        unsafe public void DocumentNodes(int CurrentNode, int CurRecur) {
            TempRecur = Mathf.Max(TempRecur, CurRecur);
            BVHNode8Data node = TLASBVH8.BVH8Nodes[CurrentNode];
            if(WorkingSetCWBVH.Count <= CurRecur) {
                Layer2 TempLayer = new Layer2();
                TempLayer.Slab = new List<int>();
                WorkingSetCWBVH.Add(TempLayer);
            }
            WorkingSetCWBVH[CurRecur].Slab.Add(CurrentNode);
            TotalCounter++;
            for (int i = 0; i < 8; i++) {
                if(CommonFunctions.NumberOfSetBits(node.meta[i] >> 5) == 0) continue;
                if ((node.meta[i] & 0b11111) < 24) {
                    continue;
                } else {
                    int child_offset = (byte)node.meta[i] & 0b11111;
                    int child_index = (int)node.base_index_child + child_offset - 24;
                    DocumentNodes(child_index, CurRecur + 1);
                }
            }
        }



        public int NonInstanceCount = 0;
        Dictionary<ParentObject, List<InstancedObject>> InstanceIndexes;

        BVH2Builder BVH;
        unsafe public void ConstructNewTLAS() {

            #if HardwareRT
                int TotLength = 0;
                int MeshOffset = 0;
                SubMeshOffsets = new List<int>();
                MeshOffsets = new List<Vector2>();
                AccelStruct.ClearInstances();
                int RendCount = RenderQue.Count;
                for(int i = 0; i < RendCount; i++) {
                    foreach(var A in RenderQue[i].Renderers) {
                        MeshOffsets.Add(new Vector2(SubMeshOffsets.Count, i));
                        var B2 = A.gameObject;
                        Mesh mesh = null;
                        if(B2.TryGetComponent<MeshFilter>(out MeshFilter TempFilter)) mesh = TempFilter.sharedMesh;
                        else if(B2.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer TempSkin)) mesh = TempSkin.sharedMesh;
                        int SubMeshCount = mesh.subMeshCount;
                        for (int i2 = 0; i2 < SubMeshCount; ++i2) {//Add together all the submeshes in the mesh to consider it as one object
                            SubMeshOffsets.Add(TotLength);
                            int IndiceLength = (int)mesh.GetIndexCount(i2) / 3;
                            TotLength += IndiceLength;
                        }
                        RayTracingSubMeshFlags[] B = new RayTracingSubMeshFlags[SubMeshCount];
                        for(int i2 = 0; i2 < SubMeshCount; i2++)
                            B[i2] = RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.ClosestHitOnly;
                        if(B2.TryGetComponent<RayTracingObject>(out RayTracingObject TempObj))
                            AccelStruct.AddInstance(A, B, true, false, (uint)((TempObj.LocalMaterials[0].SpecTrans == 1) ? 0x2 : 0x1), (uint)MeshOffset);
                        MeshOffset++;
                    }
                }

                NonInstanceCount = MeshOffset;

                int ExteriorCount = RendCount;

                int RendQueCount = InstanceData.RenderQue.Count;
                for(int i = 0; i < RendQueCount; i++) {
                    Mesh mesh = new Mesh();
                    if(InstanceData.RenderQue[i].TryGetComponent<MeshFilter>(out MeshFilter TempFilter)) mesh = TempFilter.sharedMesh;
                    else if(InstanceData.RenderQue[i].TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer TempSkin)) mesh = TempSkin.sharedMesh;
                    int SubMeshCount = mesh.subMeshCount;
                    InstanceData.RenderQue[i].RTAccelHandle = new int[SubMeshCount];
                    InstanceData.RenderQue[i].RTAccelSubmeshOffsets = new int[SubMeshCount];
                    if (InstanceIndexes.TryGetValue(InstanceData.RenderQue[i], out List<InstancedObject> ExistingList)) {
                        int ExistingCount = ExistingList.Count;
                        InstanceData.RenderQue[i].InstanceDatas = new PerInstanceData[ExistingList.Count];
                        
                        for(int j = 0; j < ExistingCount; j++) {
                            InstanceData.RenderQue[i].InstanceDatas[j].objectToWorld = ExistingList[j].transform.localToWorldMatrix;
                            InstanceData.RenderQue[i].InstanceDatas[j].renderingLayerMask = 0xFF;
                            InstanceData.RenderQue[i].InstanceDatas[j].CustomInstanceID = (uint)j;
                        }
                        for(int i2 = 0; i2 < SubMeshCount; i2++) {
                            for(int j = 0; j < ExistingCount; j++)
                                MeshOffsets.Add(new Vector2(SubMeshOffsets.Count + i2, ExteriorCount + j));
                            RayTracingMeshInstanceConfig TempConfig = new RayTracingMeshInstanceConfig();
                            TempConfig.subMeshFlags = RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.ClosestHitOnly;
                            TempConfig.mesh = mesh;
                            TempConfig.subMeshIndex = (uint)i2;
                            InstanceData.RenderQue[i].TryGetComponent<MeshRenderer>(out MeshRenderer TempRend);
                            TempConfig.material = TempRend.sharedMaterials[Mathf.Min(i2, TempRend.sharedMaterials.Length - 1)];
                            TempConfig.material.enableInstancing = true;

                            InstanceData.RenderQue[i].RTAccelHandle[i2] = AccelStruct.AddInstances(TempConfig, InstanceData.RenderQue[i].InstanceDatas, -1, 0, (uint)MeshOffset + (uint)ExistingList.Count * (uint)i2);
                            InstanceData.RenderQue[i].RTAccelSubmeshOffsets[i2] = MeshOffset + ExistingList.Count * i2;

                        }
                        MeshOffset += ExistingList.Count * SubMeshCount;
                        ExteriorCount += ExistingList.Count;// * SubMeshCount;

                    }
                    for (int i2 = 0; i2 < SubMeshCount; ++i2) {//Add together all the submeshes in the mesh to consider it as one object
                        SubMeshOffsets.Add(TotLength);
                        int IndiceLength = (int)mesh.GetIndexCount(i2) / 3;
                        TotLength += IndiceLength;
                    }

                }
            AccelStruct.Build();
            #else
                if(BVH != null) BVH.ClearAll();
                BVH = new BVH2Builder(TransformedAABBs);
                if(TLASBVH8 != null) {
                    CommonFunctions.DeepClean(ref TLASBVH8.cwbvh_indices);
                    if(TLASBVH8.BVH8NodesArray.IsCreated) TLASBVH8.BVH8NodesArray.Dispose();
                    if(TLASBVH8.costArray.IsCreated) TLASBVH8.costArray.Dispose();
                    if(TLASBVH8.decisionsArray.IsCreated) TLASBVH8.decisionsArray.Dispose();
                }
                TLASBVH8 = new BVH8Builder(BVH);
                // System.Array.Resize(ref TLASBVH8.BVH8Nodes, TLASBVH8.cwbvhnode_count);
                TempBVHArray = new BVHNode8DataCompressed[TLASBVH8.BVH8NodesArray.Length];
                CommonFunctions.Aggregate(ref TempBVHArray, TLASBVH8);
                BVHNodeCount = TLASBVH8.cwbvhnode_count;
                WorkingSetCWBVH = new List<Layer2>();
                TotalCounter = 0;
                TempRecur = 0;
                DocumentNodes(0, 0);
                MaxRecur = TempRecur;

                WorkingBuffer.ReleaseSafe();
                BoxesBuffer.ReleaseSafe();
                NodeParentAABBBuffer.ReleaseSafe();
                TLASCWBVHIndexes.ReleaseSafe();
                int LayerLength = WorkingSetCWBVH.Count;
                WorkingBuffer = new ComputeBuffer[LayerLength];
                for (int i = 0; i < LayerLength; i++) {
                    WorkingBuffer[i] = new ComputeBuffer(WorkingSetCWBVH[i].Slab.Count, 4);
                    WorkingBuffer[i].SetData(WorkingSetCWBVH[i].Slab);
                }
                NodeParentAABBBuffer = new ComputeBuffer(TotalCounter, 24);
                BoxesBuffer = new ComputeBuffer(MeshAABBs.Length, 24);
                BoxesBuffer.SetData(MeshAABBs);
                TLASCWBVHIndexes = new ComputeBuffer(MeshAABBs.Length, 4);
                TLASCWBVHIndexes.SetData(TLASBVH8.cwbvh_indices);
                CurFrame = 0;
                TLASTask = null;
            #endif

            LBVHTLASTask = null;

        }
        Task TLASTask;
        unsafe async void CorrectRefit(AABB[] Boxes) {
            TempRecur = 0;
            BVH.NoAllocRebuild(Boxes);
            TLASBVH8.NoAllocRebuild(BVH);
            // System.Array.Resize(ref TLASBVH8.BVH8Nodes, TLASBVH8.cwbvhnode_count);
            // if (TempBVHArray == null || TLASBVH8.cwbvhnode_count != TempBVHArray.Length) TempBVHArray = new BVHNode8DataCompressed[TLASBVH8.cwbvhnode_count];
            CommonFunctions.Aggregate(ref TempBVHArray, TLASBVH8);
            if(WorkingSetCWBVH == null) WorkingSetCWBVH = new List<Layer2>();
            else WorkingSetCWBVH.Clear();
            TotalCounter = 0;
            DocumentNodes(0, 0);
            return;
        }

        Task LBVHTLASTask;
        LightBVHBuilder LBVH;
        unsafe async void CorrectRefitLBVH() {
            LBVH.NoAllocRebuild(LightAABBs, ref SGTree, LightBVHTransforms, SGTreeNodes);
        }
        ComputeBuffer BoxesBuffer;
        ComputeBuffer NodeParentAABBBuffer;
        int CurFrame = 0;
        int BVHNodeCount = 0;

        public unsafe void RefitTLAS(AABB[] Boxes, CommandBuffer cmd, bool ReadyToRefit)
        {
            CurFrame++;
            bool Resettled = false;
            if(LightAABBs != null && LightAABBs.Length != 0 && LBVHTLASTask == null) LBVHTLASTask = Task.Run(() => CorrectRefitLBVH());
            #if !HardwareRT
                if(TLASTask == null) TLASTask = Task.Run(() => CorrectRefit(TransformedAABBs));

                 if(ReadyToRefit) {
                    MaxRecur = TempRecur; 

                    if(WorkingBuffer != null) for(int i = 0; i < WorkingBuffer.Length; i++) WorkingBuffer[i]?.Release();
                    if (NodeParentAABBBuffer != null) NodeParentAABBBuffer.Release();
                    if (BoxesBuffer != null) BoxesBuffer.Release();
                    if (TLASCWBVHIndexes != null) TLASCWBVHIndexes.Release();
                    int WorkingLayerCount = WorkingSetCWBVH.Count;
                    WorkingBuffer = new ComputeBuffer[WorkingLayerCount];
                    for (int i = 0; i < WorkingLayerCount; i++) {
                        WorkingBuffer[i] = new ComputeBuffer(WorkingSetCWBVH[i].Slab.Count, 4);
                        WorkingBuffer[i].SetData(WorkingSetCWBVH[i].Slab);
                    }
                    BVHNodeCount = TLASBVH8.cwbvhnode_count;
                    BoxesBuffer = new ComputeBuffer(MeshAABBs.Length, 24);
                    NodeParentAABBBuffer = new ComputeBuffer(TotalCounter, 24);
                    TLASCWBVHIndexes = new ComputeBuffer(MeshAABBs.Length, 4);
                    TLASCWBVHIndexes.SetData(TLASBVH8.cwbvh_indices);
                    int RendCount = RenderQue.Count;
                    #if !HardwareRT
                        BVH8AggregatedBuffer.SetData(TempBVHArray, 0, 0, BVHNodeCount);
                    #endif

                    TLASTask = Task.Run(() => CorrectRefit(TransformedAABBs));
                    cmd.SetBufferData(BoxesBuffer, MeshAABBs);
                    if (didstart) {
                        int ParentsLength = RenderQue.Count;
                        for (int i = 0; i < ParentsLength; i++) {//Refit BVH's of skinned meshes
                            if (RenderQue[i].IsSkinnedGroup || RenderQue[i].IsDeformable) {
                                RenderQue[i].ForceUpdateSkinnedAABB(BoxesBuffer, i, cmd);
                            }
                        }
                    }
                }
                if(RayMaster.FramesSinceStart2 > 1) {
                    if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("TLAS Refit Refit");
                    cmd.SetComputeBufferParam(Refitter, RefitLayer, "_MeshData", RayMaster.FramesSinceStart2 % 2 == 0 ? MeshDataBufferA : MeshDataBufferB);
                    cmd.SetComputeBufferParam(Refitter, RefitLayer, "AggNodes", BVH8AggregatedBuffer);
                    cmd.SetComputeBufferParam(Refitter, RefitLayer, "TLASCWBVHIndices", TLASCWBVHIndexes);
                    cmd.SetComputeBufferParam(Refitter, RefitLayer, "Boxs", BoxesBuffer);
                    cmd.SetComputeBufferParam(Refitter, RefitLayer, "NodeTotalBounds", NodeParentAABBBuffer);
                    for (int i = MaxRecur; i >= 0; i--) {
                        var NodeCount2 = WorkingBuffer[i].count;
                        cmd.SetComputeIntParam(Refitter, "NodeCount", NodeCount2);
                        cmd.SetComputeBufferParam(Refitter, RefitLayer, "WorkingBuffer", WorkingBuffer[i]);
                        cmd.DispatchCompute(Refitter, RefitLayer, (int)Mathf.Ceil(NodeCount2 / (float)128), 1, 1);
                    }
                    if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("TLAS Refit Refit");
                }
            #endif

             if(LightAABBs != null && LightAABBs.Length != 0 && LBVHTLASTask.Status == TaskStatus.RanToCompletion && CurFrame % 25 == 24) {
                CommonFunctions.DeepClean(ref LBVHLeaves);
                CommonFunctions.DeepClean(ref ParentList);
                    LBVHLeaves = new List<int>();
                    ParentList = new Vector2Int[LightNodeCount];
                    if(LightNodeCount != 0)
                    Refit2(0,0);
                    int LBVHLeaveCount = LBVHLeaves.Count;
                    for(int i = 0; i < LBVHLeaveCount; i++) {
#if !DontUseSGTree
                        int Index = LightMeshes[-(SGTree[LBVHLeaves[i]].left+1)].LockedMeshIndex;
#else
                        int Index = LightMeshes[-(LBVH.nodes[LBVHLeaves[i]].left+1)].LockedMeshIndex;
#endif
                    MyMeshDataCompacted TempDat =  MyMeshesCompacted[Index];
                    TempDat.PathFlags = CalcBitField(LBVHLeaves[i]);
                    MyMeshesCompacted[Index] = TempDat;
                }
                if(LightMeshCount > 0) {
                    int RendQueCount = RenderQue.Count;
                    for (int i = 0; i < LightMeshCount; i++) {
                        Matrix4x4 Mat = LightTransforms[i].localToWorldMatrix;
                        LightBVHTransforms[i].Transform = Mat;
                        int Index = LightMeshes[i].LockedMeshIndex;
                        if(Index < RendQueCount) {
                            LightBVHTransforms[i].SolidOffset = RenderQue[Index].GlobalLightNodeOffset; 
                            LightAABBs[i].b = RenderQue[Index].LBVH.ParentBound.aabb.b;
                            LightAABBs[i].b.TransformAABB(Mat);
                            LightAABBs[i].w = (Mat * RenderQue[Index].LBVH.ParentBound.aabb.w).normalized;
                        } else {
                            Index -= RendQueCount;
                            LightBVHTransforms[i].SolidOffset = InstanceRenderQue[Index].InstanceParent.GlobalLightNodeOffset; 
                            LightAABBs[i].b = InstanceRenderQue[Index].InstanceParent.LBVH.ParentBound.aabb.b;
                            LightAABBs[i].b.TransformAABB(Mat);
                            LightAABBs[i].w = (Mat * InstanceRenderQue[Index].InstanceParent.LBVH.ParentBound.aabb.w).normalized;
                        }
                    }
                    if(LBVHWorkingSet != null) for(int i = 0; i < LBVHWorkingSet.Length; i++) LBVHWorkingSet[i].ReleaseSafe();
                    LBVHWorkingSet = new ComputeBuffer[LBVH.MaxDepth];
                    for(int i = 0; i < LBVH.MaxDepth; i++) {
                        LBVHWorkingSet[i] = new ComputeBuffer(LBVH.MainSet[i].Count, 4);
                        LBVHWorkingSet[i].SetData(LBVH.MainSet[i]);
                    }
                    //kernel 8
                    cmd.SetComputeBufferParam(Refitter, RefitCopy, "LightNodesWriteB", LightTreeBufferA);
                    cmd.SetComputeIntParam(Refitter, "TargetOffset", LightTreePrimaryTLASOffset);
                    cmd.SetComputeIntParam(Refitter, "TotalNodeOffset", 0);

                    Resettled = true;
                    cmd.SetComputeIntParam(Refitter, "Count", LightNodeCount);
                    cmd.DispatchCompute(Refitter, RefitCopy, (int)Mathf.Ceil(LightNodeCount / (float)256.0f), 1, 1);
#if !DontUseSGTree
                    cmd.SetBufferData(LightTreeBufferA, SGTree, 0, 0, LightNodeCount);
#else
                    cmd.SetBufferData(LightTreeBufferA, LBVH.nodes, 0, 0, LightNodeCount);
#endif
                }
                LBVHTLASTask = Task.Run(() => CorrectRefitLBVH());
            }

                if(LightAABBs != null && LightAABBs.Length != 0) {
                    if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("LightRefitter");
                    cmd.SetComputeIntParam(Refitter, "LightTreePrimaryTLASOffset", LightTreePrimaryTLASOffset);
                    cmd.SetComputeIntParam(Refitter, "Resettled", Resettled ? 1 : 0);
                    cmd.SetComputeBufferParam(Refitter, LightTLASRefitKernel, "_MeshData", RayMaster.FramesSinceStart2 % 2 == 0 ? MeshDataBufferA : MeshDataBufferB);
                    cmd.SetComputeBufferParam(Refitter, LightTLASRefitKernel, "_LightMeshes", LightMeshBuffer);
                    cmd.SetComputeBufferParam(Refitter, LightTLASRefitKernel, "LightNodesWrite", LightTreeBufferA);
                    int ObjectOffset = 0;
                    for(int i = LBVHWorkingSet.Length - 1; i >= 0; i--) {
                        var ObjOffVar = ObjectOffset;
                        var SetCount = LBVHWorkingSet[i].count;
                        cmd.SetComputeIntParam(Refitter, "SetCount", SetCount);
                        cmd.SetComputeIntParam(Refitter, "ObjectOffset", ObjOffVar);
                        cmd.SetComputeBufferParam(Refitter, LightTLASRefitKernel, "WorkingSet", LBVHWorkingSet[i]);
                        cmd.DispatchCompute(Refitter, LightTLASRefitKernel, (int)Mathf.Ceil(SetCount / (float)256.0f), 1, 1);

                        ObjectOffset += SetCount;
                    }
                    if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("LightRefitter");
                }
        }

        public Vector2Int[] ParentList;
        public List<int> LBVHLeaves;

        public uint CalcBitField(int Index) {
            int Parent = ParentList[Index].x;
#if !DontUseSGTree
            bool IsLeft = SGTree[Parent].left == Index;
#else
            bool IsLeft = LBVH.nodes[Parent].left == Index;
#endif
            uint Flag = (IsLeft ? 0u : 1u) << ParentList[Index].y;
            if(ParentList[Index].y == 0) return Flag;
            return Flag | CalcBitField(ParentList[Index].x);
        }

        private void Refit2(int Depth, int CurrentIndex) {
#if !DontUseSGTree
            if(SGTree[CurrentIndex].left < 0) {
                LBVHLeaves.Add(CurrentIndex);
                return;
            }
            ParentList[SGTree[CurrentIndex].left] = new Vector2Int(CurrentIndex, Depth);
            ParentList[SGTree[CurrentIndex].left + 1] = new Vector2Int(CurrentIndex, Depth);
            Refit2(Depth + 1, SGTree[CurrentIndex].left);
            Refit2(Depth + 1, SGTree[CurrentIndex].left + 1);
#else
            if(LBVH.nodes[CurrentIndex].left < 0) {
                LBVHLeaves.Add(CurrentIndex);
                return;
            }
            ParentList[LBVH.nodes[CurrentIndex].left] = new Vector2Int(CurrentIndex, Depth);
            ParentList[LBVH.nodes[CurrentIndex].left + 1] = new Vector2Int(CurrentIndex, Depth);
            Refit2(Depth + 1, LBVH.nodes[CurrentIndex].left);
            Refit2(Depth + 1, LBVH.nodes[CurrentIndex].left + 1);
#endif
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
                RayMaster.MainDirectionalLight = -1;
                foreach (RayTracingLights RayLight in RayTracingMaster._rayTracingLights) {
                    UnityLightCount++;
                    if (RayLight.ThisLightData.Type == 1) {
                        if(RayLight.IsMainSun || RayMaster.MainDirectionalLight == -1) {
                            RayMaster.MainDirectionalLight = UnityLightCount - 1;
                            SunDirection = RayLight.ThisLightData.Direction;
                        }
                    }
                    RayLight.ArrayIndex = UnityLightCount - 1;
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
                RayMaster.MainDirectionalLight = -1;
                for (int i = 0; i < LightCount; i++) {
                    RayLight = RayTracingMaster._rayTracingLights[i];
                    if(RayLight.ThisLightData.Type == 1) {
                        if(RayLight.IsMainSun || RayMaster.MainDirectionalLight == -1) {
                            RayMaster.MainDirectionalLight = i;
                            SunDirection = RayLight.ThisLightData.Direction;
                        }
                    }
                    if(RayLight.CallHasUpdated()) {
                        RayTracingMaster.SampleCount = 0;
                        RayMaster.FramesSinceStart = 0;
                        UnityLights[RayLight.ArrayIndex] = RayLight.ThisLightData;
                        UnityLightBuffer.SetData(UnityLights, RayLight.ArrayIndex, RayLight.ArrayIndex, 1);
                    }
                }
            }

            int MatOffset = 0;
            int MeshDataCount = RenderQue.Count;
            int aggregated_bvh_node_count = 2 * (MeshDataCount + InstanceRenderQue.Count);
            int AggNodeCount = aggregated_bvh_node_count;
            int AggTriCount = 0;
            int AggSkinnedOffset = 0;
            bool HasChangedMaterials = false;
            if(MeshAABBs.Length == 0) return -1;
            if (ChildrenUpdated || !didstart || OnlyInstanceUpdated || MyMeshesCompacted.Count == 0) {

                if (UseSkinning && didstart) {
                    ParentObject TempParent;
                    int CompactedCount = MyMeshesCompacted.Count;
                    for (int i = 0; i < MeshDataCount; i++) {//Refit BVH's of skinned meshes
                        TempParent = RenderQue[i];
                        if (TempParent.IsSkinnedGroup || TempParent.IsDeformable) {
                            TempParent.RefitMesh(ref BVH8AggregatedBuffer, ref AggTriBufferA, ref AggTriBufferB, ref LightTriBuffer, LightTreeBufferA, BoxesBuffer, i, SkinnedMeshAggTriBufferPrev, cmd);
                            if(i < CompactedCount) {
                                MyMeshDataCompacted TempMesh2 = MyMeshesCompacted[i];
                                TempMesh2.Transform = RenderTransforms[i].worldToLocalMatrix;
                                MyMeshesCompacted[i] = TempMesh2;
                                MeshAABBs[i] = TempParent.aabb_untransformed;
                                TransformedAABBs[i] = TempParent.aabb;
                            }
                        }
                    }
                }

                
                MyMeshesCompacted.Clear();
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
                        LightNodeOffset = RenderQue[i].GlobalLightNodeOffset,
                        LightNodeSkinnedOffset = RenderQue[i].GlobalLightNodeSkinnedOffset,
                        SkinnedOffset = RenderQue[i].GlobalSkinnedOffset
                    });
                    RenderQue[i].CompactedMeshData = i;
                    MatOffset += RenderQue[i].MatOffset;
                    MeshAABBs[i] = RenderQue[i].aabb_untransformed;
                    TransformedAABBs[i] = RenderQue[i].aabb;
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
                        LightBVHTransforms[i].SolidOffset = RenderQue[Index].GlobalLightNodeOffset; 
                        LightAABBs[i].b = RenderQue[Index].LBVH.ParentBound.aabb.b;
                        LightAABBs[i].b.TransformAABB(Mat);
                        LightAABBs[i].w = (Mat * RenderQue[Index].LBVH.ParentBound.aabb.w).normalized;
                    } else {
                        Index -= RendQueCount;
                        LightBVHTransforms[i].SolidOffset = InstanceRenderQue[Index].InstanceParent.GlobalLightNodeOffset; 
                        LightAABBs[i].b = InstanceRenderQue[Index].InstanceParent.LBVH.ParentBound.aabb.b;
                        LightAABBs[i].b.TransformAABB(Mat);
                        LightAABBs[i].w = (Mat * InstanceRenderQue[Index].InstanceParent.LBVH.ParentBound.aabb.w).normalized;
                    }
                }
                if(LightMeshCount > 0) {
                    if(LBVH != null) LBVH.Dispose();
                    LBVH = new LightBVHBuilder(LightAABBs, ref SGTree, LightBVHTransforms, SGTreeNodes);

                    LBVHWorkingSet.ReleaseSafe();
                    LBVHWorkingSet = new ComputeBuffer[LBVH.MaxDepth];
                    for(int i = 0; i < LBVH.MaxDepth; i++) {
                        LBVHWorkingSet[i] = new ComputeBuffer(LBVH.MainSet[i].Count, 4);
                        LBVHWorkingSet[i].SetData(LBVH.MainSet[i]);
                    }

#if !DontUseSGTree
                    LightTreeBufferA.SetData(SGTree, 0, 0, LightNodeCount);
#else
                    LightTreeBufferA.SetData(LBVH.nodes, 0, 0, LightNodeCount);
#endif
                }

                // UnityEngine.Profiling.Profiler.EndSample();
                InstanceIndexes = new Dictionary<ParentObject, List<InstancedObject>>();
                int InstanceCount = InstanceRenderQue.Count;
                for(int i = 0; i < InstanceCount; i++) {
                    if (InstanceIndexes.TryGetValue(InstanceRenderQue[i].InstanceParent, out List<InstancedObject> ExistingList)) {
                        ExistingList.Add(InstanceRenderQue[i]);
                    } else {
                        List<InstancedObject> TempList = new List<InstancedObject>();
                        TempList.Add(InstanceRenderQue[i]);
                        InstanceIndexes.Add(InstanceRenderQue[i].InstanceParent, TempList);
                    }
                }

                // UnityEngine.Profiling.Profiler.BeginSample("Remake Initial Instance Data A");
                InstanceCount = InstanceData.RenderQue.Count;
                for (int i = 0; i < InstanceCount; i++) {
                    InstanceData.RenderQue[i].CompactedMeshData = i;
                    Aggs[i].AggIndexCount = AggTriCount;
                    Aggs[i].AggNodeCount = AggNodeCount;
                    Aggs[i].MaterialOffset = MatOffset;
                    Aggs[i].mesh_data_bvh_offsets = aggregated_bvh_node_count;
                    Aggs[i].LightTriCount = InstanceData.RenderQue[i].LightTriangles.Count;
                    Aggs[i].LightNodeOffset = InstanceData.RenderQue[i].GlobalLightNodeOffset;
                    MatOffset += InstanceData.RenderQue[i].MatOffset;
                    AggNodeCount += InstanceData.RenderQue[i].AggBVHNodeCount;//Can I replace this with just using aggregated_bvh_node_count below?
                    AggTriCount += InstanceData.RenderQue[i].AggIndexCount;
                    #if !HardwareRT
                        aggregated_bvh_node_count += InstanceData.RenderQue[i].BVH.cwbvhnode_count;
                    #endif
                }
                // UnityEngine.Profiling.Profiler.EndSample();
                // UnityEngine.Profiling.Profiler.BeginSample("Remake Initial Instance Data B");
                int MeshCount = MeshDataCount;
                int RenderCountOffset = RenderQue.Count;
                #if HardwareRT
                    int TempCount = 0;
                    InstanceCount = InstanceData.RenderQue.Count;
                    for(int i = 0; i < InstanceCount; i++) {
                        if(InstanceIndexes.TryGetValue(InstanceData.RenderQue[i], out List<InstancedObject> TempList)) {
                            int ListCount = TempList.Count;
                            for(int j = 0; j < ListCount; j++) {
                                int Index = TempList[j].InstanceParent.CompactedMeshData;
                                MyMeshesCompacted.Add(new MyMeshDataCompacted() {
                                    mesh_data_bvh_offsets = Aggs[Index].mesh_data_bvh_offsets,
                                    Transform = TempList[j].transform.worldToLocalMatrix,
                                    AggIndexCount = Aggs[Index].AggIndexCount,
                                    AggNodeCount = Aggs[Index].AggNodeCount,
                                    MaterialOffset = Aggs[Index].MaterialOffset,
                                    LightTriCount = Aggs[Index].LightTriCount,
                                    LightNodeOffset = Aggs[Index].LightNodeOffset,
                                    LightNodeSkinnedOffset = -1,
                                    SkinnedOffset = -1
                                });
                                if(TempList[j].LightIndex != -1) {
                                    LightMeshData TempDat = LightMeshes[TempList[j].LightIndex];
                                    TempDat.LockedMeshIndex = MyMeshesCompacted.Count - 1;
                                    LightMeshes[TempList[j].LightIndex] = TempDat;
                                }
                                TempList[j].CompactedMeshData = MeshCount + j;
                                MeshAABBs[RenderCountOffset + TempCount] = TempList[j].InstanceParent.aabb_untransformed;      
                                AABB aabb = TempList[j].InstanceParent.aabb_untransformed;
                                aabb.TransformAABB(TempList[j].transform.localToWorldMatrix);                  
                                TransformedAABBs[RenderCountOffset + TempCount] = aabb;                        
                                TempCount++;
                            }
                            MeshCount += TempList.Count;
                        }
                    }
                #else
                    InstanceCount = InstanceRenderQue.Count;
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
                            LightNodeOffset = Aggs[Index].LightNodeOffset,
                            LightNodeSkinnedOffset = -1,
                            SkinnedOffset = -1

                        });
                        InstanceRenderQue[i].CompactedMeshData = MeshCount + i;

                        MeshAABBs[RenderCountOffset + i] = InstanceRenderQue[i].InstanceParent.aabb_untransformed;
                        AABB aabb = InstanceRenderQue[i].InstanceParent.aabb_untransformed;
                        aabb.TransformAABB(InstanceRenderQue[i].transform.localToWorldMatrix);                  
                        TransformedAABBs[RenderCountOffset + i] = aabb;
                    }
                #endif
                // UnityEngine.Profiling.Profiler.EndSample();


                CommonFunctions.CreateComputeBuffer(ref LightMeshBuffer, LightMeshes);
#if TTVerbose
                Debug.Log("Total Object Count: " + MeshAABBs.Length);
#endif
                // UnityEngine.Profiling.Profiler.BeginSample("Update Materials");
                HasChangedMaterials = UpdateMaterials();
                // UnityEngine.Profiling.Profiler.EndSample();
                ConstructNewTLAS();
                #if !HardwareRT
                    BVH8AggregatedBuffer.SetData(TempBVHArray, 0, 0, BVHNodeCount);
                #endif
                    CommonFunctions.DeepClean(ref LBVHLeaves);
                    CommonFunctions.DeepClean(ref ParentList);
                    LBVHLeaves = new List<int>();
                    ParentList = new Vector2Int[LightNodeCount];
                    if(LightNodeCount != 0)
                        Refit2(0,0);
                    int LBVHLeaveCount = LBVHLeaves.Count;
                    for(int i = 0; i < LBVHLeaveCount; i++) {
#if !DontUseSGTree
                        int Index = LightMeshes[-(SGTree[LBVHLeaves[i]].left+1)].LockedMeshIndex;
#else
                        int Index = LightMeshes[-(LBVH.nodes[LBVHLeaves[i]].left+1)].LockedMeshIndex;
#endif
                        MyMeshDataCompacted TempDat =  MyMeshesCompacted[Index];
                        TempDat.PathFlags = CalcBitField(LBVHLeaves[i]);
                        MyMeshesCompacted[Index] = TempDat;
                    }
                // Refit3(0,0,0);
                CommonFunctions.CreateComputeBuffer(ref MeshDataBufferA, MyMeshesCompacted);
                CommonFunctions.CreateComputeBuffer(ref MeshDataBufferB, MyMeshesCompacted);
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
                bool ReadyToRefit = TLASTask != null && TLASTask.Status == TaskStatus.RanToCompletion && CurFrame % 25 == 24;
                int CompactedCount = MyMeshesCompacted.Count;
                for (int i = 0; i < MeshDataCount; i++) {
                    TargetParent = RenderQue[i];
                    TargetTransform = RenderTransforms[i];
                    if (TargetParent.IsSkinnedGroup || TargetParent.IsDeformable) {
                        if (UseSkinning && didstart) {
                            TargetParent.RefitMesh(ref BVH8AggregatedBuffer, ref AggTriBufferA, ref AggTriBufferB, ref LightTriBuffer, LightTreeBufferA, BoxesBuffer, i, SkinnedMeshAggTriBufferPrev, cmd);
                            if(i < CompactedCount) {
                                TempMesh2 = MyMeshesCompacted[i];
                                TempMesh2.Transform = TargetTransform.worldToLocalMatrix;
                                MyMeshesCompacted[i] = TempMesh2;
                                MeshAABBs[i] = TargetParent.aabb_untransformed;
                                TransformedAABBs[i] = TargetParent.aabb;
                            }
                        }
                        continue;
                    }


                    if (TargetTransform.hasChanged || (ReadyToRefit && TargetParent.HasTransformChanged)) {
                        TargetParent.HasTransformChanged = true;
                        TargetTransform = TargetTransform;
                        TargetTransform.hasChanged = false;
                        TempMesh2 = MyMeshesCompacted[i];
                        TempMesh2.Transform = TargetTransform.worldToLocalMatrix;
                        MyMeshesCompacted[i] = TempMesh2;
                        #if HardwareRT
                            foreach(var a in TargetParent.Renderers)
                                AccelStruct.UpdateInstanceTransform(a);
                        #endif
                        if(ReadyToRefit) {
                            TargetParent.UpdateAABB(TargetTransform);
                            MeshAABBs[i] = TargetParent.aabb_untransformed;
                            TransformedAABBs[i] = TargetParent.aabb;
                        }
                    }
                }
                if(MeshDataCount != 1 && ClosedCount == MeshDataCount - 1) return 0;
                // UnityEngine.Profiling.Profiler.EndSample();

                int RendQueCount = RenderQue.Count;
                for (int i = 0; i < LightMeshCount; i++) LightBVHTransforms[i].Transform = LightTransforms[i].localToWorldMatrix;

                // UnityEngine.Profiling.Profiler.BeginSample("Refit TLAS");
                int ListCount = InstanceRenderQue.Count;
                List<ParentObject> ObjsToUpdate = new List<ParentObject>();

                // bool AnyHasChanged = false;
                LightMeshData CurLightMesh;
                if (LightMeshBuffer != null && LightMeshBuffer.IsValid() && LightMeshCount != 0 && LightMeshCount == LightMeshBuffer.count) {
                    for (int i = 0; i < LightMeshCount; i++) {
                        if(LightTransforms[i].hasChanged) {
                            CurLightMesh = LightMeshes[i];
                            CurLightMesh.Center = LightTransforms[i].position;
                            LightMeshes[i] = CurLightMesh;
                            LightMeshBuffer.SetData(LightMeshes, i, i, 1);
                        }
                    }
                } else {
                    CommonFunctions.CreateComputeBuffer(ref LightMeshBuffer, LightMeshes);
                }

                for (int i = 0; i < ListCount; i++) {
                    if (InstanceRenderTransforms[i].hasChanged || ChangedLastFrame) {
                        TargetTransform = InstanceRenderTransforms[i];
                         MyMeshDataCompacted TempMesh = MyMeshesCompacted[InstanceRenderQue[i].CompactedMeshData];
                        TempMesh.Transform = TargetTransform.worldToLocalMatrix;
                        MyMeshesCompacted[InstanceRenderQue[i].CompactedMeshData] = TempMesh;
                    #if !HardwareRT
                        TargetTransform.hasChanged = false;
                    #endif
                        MeshAABBs[InstanceRenderQue[i].CompactedMeshData] = InstanceRenderQue[i].InstanceParent.aabb_untransformed;
                        AABB aabb = InstanceRenderQue[i].InstanceParent.aabb_untransformed;
                        aabb.TransformAABB(TargetTransform.localToWorldMatrix);                  
                        TransformedAABBs[InstanceRenderQue[i].CompactedMeshData] = aabb;
                        if(!ObjsToUpdate.Contains(InstanceRenderQue[i].InstanceParent)) ObjsToUpdate.Add(InstanceRenderQue[i].InstanceParent);
                    }
                }

                #if HardwareRT
                    int UpdateCount = ObjsToUpdate.Count;
                    for(int i = 0; i < UpdateCount; i++) {
                        if (InstanceIndexes.TryGetValue(ObjsToUpdate[i], out List<InstancedObject> ExistingList)) {

                            Mesh mesh = new Mesh();
                            if(ObjsToUpdate[i].TryGetComponent<MeshFilter>(out MeshFilter TempFilter)) mesh = TempFilter.sharedMesh;
                            else if(ObjsToUpdate[i].TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer TempSkin)) mesh = TempSkin.sharedMesh;
                            int SubMeshCount = mesh.subMeshCount;
                            for(int i2 = 0; i2 < SubMeshCount; i2++) AccelStruct.RemoveInstance(ObjsToUpdate[i].RTAccelHandle[i2]);


                            int ExCount = ExistingList.Count;
                            for(int j = 0; j < ExCount; j++) {
                                if(ExistingList[j].transform.hasChanged) {
                                    ExistingList[j].transform.hasChanged = false;
                                    ObjsToUpdate[i].InstanceDatas[j].objectToWorld = ExistingList[j].transform.localToWorldMatrix;
                                    ObjsToUpdate[i].InstanceDatas[j].renderingLayerMask = 0xFF;
                                    ObjsToUpdate[i].InstanceDatas[j].CustomInstanceID = (uint)j;
                                }
                            }


                            for(int i2 = 0; i2 < SubMeshCount; i2++) {

                                RayTracingMeshInstanceConfig TempConfig = new RayTracingMeshInstanceConfig();
                                TempConfig.subMeshFlags = RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.ClosestHitOnly;
                                TempConfig.mesh = mesh;
                                TempConfig.subMeshIndex = (uint)i2;
                                ObjsToUpdate[i].TryGetComponent<MeshRenderer>(out MeshRenderer TempRend);
                                TempConfig.material = TempRend.sharedMaterials[Mathf.Min(i2, TempRend.sharedMaterials.Length - 1)];

                                ObjsToUpdate[i].RTAccelHandle[i2] = AccelStruct.AddInstances(TempConfig, ObjsToUpdate[i].InstanceDatas, -1, 0, (uint)ObjsToUpdate[i].RTAccelSubmeshOffsets[i2]);
                            }
                        }

                    }
                    AccelStruct.Build();
                #endif
                // UnityEngine.Profiling.Profiler.EndSample();
                AggNodeCount = 0;
                HasChangedMaterials = UpdateMaterials();
                 // cmd.SetBufferData(MeshDataBufferA, MyMeshesCompacted);
                cmd.BeginSample("MeshBufferSetter");
                if(RayMaster.FramesSinceStart2 % 2 == 0) cmd.SetBufferData(MeshDataBufferA, MyMeshesCompacted);
                else cmd.SetBufferData(MeshDataBufferB, MyMeshesCompacted);
                cmd.EndSample("MeshBufferSetter");
                if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("TLAS Refitting");
                RefitTLAS(MeshAABBs, cmd, ReadyToRefit);
                if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("TLAS Refitting");
            }


            if(HasChangedMaterials) MaterialBuffer.SetData(_Materials);
            if(HasChangedMaterials) IntersectionMaterialBuffer.SetData(IntersectionMats);
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
                    TempMat.MatData = CurrentMaterial.LocalMaterials[Index];
                    TempMat.MatData.BaseColor = (!CurrentMaterial.UseKelvin[Index]) ? CurrentMaterial.LocalMaterials[Index].BaseColor : new Vector3(Mathf.CorrelatedColorTemperatureToRGB(CurrentMaterial.KelvinTemp[Index]).r, Mathf.CorrelatedColorTemperatureToRGB(CurrentMaterial.KelvinTemp[Index]).g, Mathf.CorrelatedColorTemperatureToRGB(CurrentMaterial.KelvinTemp[Index]).b);
                    TempMat.MatData.Roughness = ((int)CurrentMaterial.LocalMaterials[Index].MatType != 1) ? CurrentMaterial.LocalMaterials[Index].Roughness : Mathf.Max(CurrentMaterial.LocalMaterials[Index].Roughness, 0.000001f);
                    if(CurrentMaterial.InvisibleOverride) TempMat.MatData.Tag = CommonFunctions.SetFlagVar(CurrentMaterial.LocalMaterials[Index].Tag, CommonFunctions.Flags.Invisible, true);
                    else TempMat.MatData.Tag = CurrentMaterial.LocalMaterials[Index].Tag;



                        IntersectionMats[CurrentMaterial.MaterialIndex[i3] + CurrentMaterial.MatOffset].AlphaTex =        TempMat.Textures.AlphaTex;
                        IntersectionMats[CurrentMaterial.MaterialIndex[i3] + CurrentMaterial.MatOffset].AlbedoTex =       TempMat.Textures.AlbedoTex;
                        IntersectionMats[CurrentMaterial.MaterialIndex[i3] + CurrentMaterial.MatOffset].Tag =             TempMat.MatData.Tag;
                        IntersectionMats[CurrentMaterial.MaterialIndex[i3] + CurrentMaterial.MatOffset].MatType =         TempMat.MatData.MatType;
                        IntersectionMats[CurrentMaterial.MaterialIndex[i3] + CurrentMaterial.MatOffset].specTrans =       TempMat.MatData.SpecTrans;
                        IntersectionMats[CurrentMaterial.MaterialIndex[i3] + CurrentMaterial.MatOffset].AlphaCutoff =     TempMat.MatData.AlphaCutoff;
                        IntersectionMats[CurrentMaterial.MaterialIndex[i3] + CurrentMaterial.MatOffset].AlbedoTexScale =  TempMat.MatData.TextureModifiers.MainTexScaleOffset;
                        IntersectionMats[CurrentMaterial.MaterialIndex[i3] + CurrentMaterial.MatOffset].Rotation =        TempMat.MatData.TextureModifiers.Rotation;
                        IntersectionMats[CurrentMaterial.MaterialIndex[i3] + CurrentMaterial.MatOffset].surfaceColor =        TempMat.MatData.BaseColor;
                        IntersectionMats[CurrentMaterial.MaterialIndex[i3] + CurrentMaterial.MatOffset].scatterDistance =        TempMat.MatData.ScatterDist;
                    if(RayMaster.LocalTTSettings.MatChangeResetsAccum) {
                        RayTracingMaster.SampleCount = 0;
                        RayMaster.FramesSinceStart = 0;
                    }
                    _Materials[CurrentMaterial.MaterialIndex[i3] + CurrentMaterial.MatOffset] = TempMat;
                    #if HardwareRT
                        var A = CurrentMaterial.gameObject.GetComponent<Renderer>();
                        if(A != null) {
                            if(TempMat.MatData.SpecTrans == 1) AccelStruct.UpdateInstanceMask(A, 0x2);
                            else AccelStruct.UpdateInstanceMask(A, 0x1);
                        } else {
                            if(TempMat.MatData.SpecTrans == 1) AccelStruct.UpdateInstanceMask(CurrentMaterial.gameObject.GetComponent<SkinnedMeshRenderer>(), 0x2);
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

        // public void Update() {

        //         int Coun1 = InstanceData.RenderQue.Count;
        //         for(int i = 0; i < Coun1; i++) {
        //             if (InstanceIndexes.TryGetValue(InstanceData.RenderQue[i], out List<InstancedObject> ExistingList)) {
        //                 int Coun2 = ExistingList.Count;
        //                 Mesh mesh = new Mesh();
        //                 if(InstanceData.RenderQue[i].TryGetComponent<MeshFilter>(out MeshFilter TempFilter)) mesh = TempFilter.sharedMesh;
        //                 else if(InstanceData.RenderQue[i].TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer TempSkin)) mesh = TempSkin.sharedMesh;
        //                 Matrix4x4[] TransformList = new Matrix4x4[Coun2];
        //                 InstanceData.RenderQue[i].TryGetComponent<MeshRenderer>(out MeshRenderer TempRend);
        //                 TempRend.sharedMaterials[0].enableInstancing = true;
        //                 RenderParams rendp = new RenderParams(TempRend.sharedMaterials[0]);
        //                 for(int i2 = 0; i2 < Coun2; i2++) {
        //                     TransformList[i2] = ExistingList[i2].transform.localToWorldMatrix;
        //                 }
        //                 Graphics.RenderMeshInstanced(rendp, mesh, 0, TransformList);
        //             }
        //         }
        // }

        // private int DrawCount;
        // // public GaussianTreeNode[] LightTree;
        // private void Refit(int Depth, int CurrentIndex, bool HasHitTLAS, int node_offset, Matrix4x4 Transform, Vector3 PrevPos) {
        //     try{
        //         if((PublicLightData[CurrentIndex].left < 0) || Depth > 20 || PublicLightData[CurrentIndex].phi == 0) return;
        //     } catch(System.Exception E) {
        //         Debug.LogError(CurrentIndex);
        //         return;
        //     }
        //     int LeftIndex = PublicLightData[CurrentIndex].left;
        //         Vector3 BBMax;
        //         Vector3 BBMin;
        //        //  if(HasHitTLAS) {
        //        //      Vector3 new_center = CommonFunctions.transform_position(Transform, (PublicLightData[CurrentIndex].BBMax + PublicLightData[CurrentIndex].BBMin) / 2.0f);
        //        //      Vector3 new_extent = CommonFunctions.transform_direction(Transform, (PublicLightData[CurrentIndex].BBMax - PublicLightData[CurrentIndex].BBMin) / 2.0f);

        //        //      BBMin = new_center - new_extent;
        //        //      BBMax = new_center + new_extent;
        //        //      Gizmos.color = Color.green;
        //        //      Gizmos.DrawWireCube((BBMax + BBMin) / 2.0f, BBMax - BBMin);
        //        //      // Gizmos.color = Color.yellow;
        //        //      // Gizmos.DrawLine((BBMax + BBMin) / 2.0f, PrevPos);
        //        //     return;
        //        // } else {
        //             BBMax = PublicLightData[CurrentIndex].BBMax;
        //             BBMin = PublicLightData[CurrentIndex].BBMin;
        //             Gizmos.color = Color.red;
        //             if(LeftIndex >= 0) Gizmos.DrawWireCube((PublicLightData[CurrentIndex].BBMax + PublicLightData[CurrentIndex].BBMin) / 2.0f, PublicLightData[CurrentIndex].BBMax - PublicLightData[CurrentIndex].BBMin);
        //        // }
        //     HasHitTLAS = HasHitTLAS || LeftIndex < 0;
        //         // float Radius = Vector3.Distance(Pos, CommonFunctions.ToVector3(Transform * CommonFunctions.ToVector4(LightTree[CurrentIndex].S.Center + new Vector3(LightTree[CurrentIndex].S.Radius, 0, 0), 1)));
        //         // Gizmos.DrawWireSphere(Pos, Radius);            
        //     if(LeftIndex < 0) {
        //         // int MeshIndex = -(LeftIndex+1);
        //         // node_offset = MyMeshesCompacted[LightMeshes[MeshIndex].LockedMeshIndex].LightNodeOffset;
        //         // LeftIndex = 0;
        //         return;
        //         // int MeshIndex = LightMeshes[-(LeftIndex+1)].LockedMeshIndex;
        //         // node_offset = MyMeshesCompacted[MeshIndex].LightNodeOffset;
        //         // Transform = MyMeshesCompacted[LightMeshes[-(LeftIndex+1)].LockedMeshIndex].Transform.inverse;
        //         // HasHitTLAS = true;
        //         // LeftIndex = 0;
        //     } else {
        //         // DrawCount++;
        //         // if(CurrentIndex >= node_offset + LeftIndex) Debug.Log("EEE " + LeftIndex + ", " + node_offset);
        //     }


        //     Refit(Depth + 1, LeftIndex + node_offset, HasHitTLAS, node_offset, Transform, (BBMax + BBMin) / 2.0f);
        //     Refit(Depth + 1, LeftIndex + node_offset + 1, HasHitTLAS, node_offset, Transform, (BBMax + BBMin) / 2.0f);
        // }

        // // public float VarTest = 1.0f;
        // // public GaussianTreeNode[] LightTree2;
        // public void OnDrawGizmos() {
        //     if(Application.isPlaying) {
        //         DrawCount = 0;
        //         // int Count = LightTreeBufferB.count;
        //         // LightTree = new GaussianTreeNode[Count];
        //         // LightTree2 = new GaussianTreeNode[Count];
        //         // LightTreeBuffer2.GetData(LightTree);
        //         // LightTreeBufferA.GetData(LightTree);
        //         Refit(0, 0, false, 0, Matrix4x4.identity, Vector3.zero);
        //         // Debug.Log(DrawCount);
        //         // for(int i = 2; i < Count; i++) {
        //         //     Vector3 Pos = CommonFunctions.ToVector3(LightBVHTransforms[0].Transform * CommonFunctions.ToVector4(LightTree[i].S.Center, 1));
        //         //     float Radius = Vector3.Distance(Pos, CommonFunctions.ToVector3(LightBVHTransforms[0].Transform * CommonFunctions.ToVector4(LightTree[i].S.Center + new Vector3(LightTree[i].S.Radius, 0, 0), 1)));
        //         //     // if(LightTree[i].variance < LightTree[i].S.Radius * VarTest) Gizmos.DrawWireSphere(Pos, Radius);
        //         //     Gizmos.DrawWireSphere(Pos, Radius);

        //         // }
        //     }
        // }








    }
}