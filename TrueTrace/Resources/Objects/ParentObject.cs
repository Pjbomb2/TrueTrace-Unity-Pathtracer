using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#pragma warning disable 1998



namespace TrueTrace {
    [System.Serializable]
    public class ParentObject : MonoBehaviour
    {
        public LightBVHBuilder LBVH;
        public Task AsyncTask;
        public int ExistsInQue = -1;
        public bool IsDeformable = false;
        [HideInInspector] public ComputeBuffer LightTriBuffer;
        [HideInInspector] public ComputeBuffer LightNodeBuffer;
        [HideInInspector] public ComputeBuffer TriBuffer;
        [HideInInspector] public ComputeBuffer BVHBuffer;
        public string Name;
        [HideInInspector] public GraphicsBuffer[] VertexBuffers;
        [HideInInspector] public ComputeBuffer[] IndexBuffers;
        [HideInInspector] public int[] CWBVHIndicesBufferInverted;
        [HideInInspector] public List<RayTracingObject> ChildObjects;
        [HideInInspector] public bool MeshCountChanged;
        private unsafe NativeArray<AABB> TrianglesArray;
        private unsafe AABB* Triangles;
        [HideInInspector] public CudaTriangle[] AggTriangles;
        [HideInInspector] public Vector3 ParentScale;
        [HideInInspector] public List<LightTriData> LightTriangles;
        [HideInInspector] public List<Vector3> LightTriNorms;
        [HideInInspector] public BVH8Builder BVH;
        [HideInInspector] public SkinnedMeshRenderer[] SkinnedMeshes;
        [HideInInspector] public MeshFilter[] DeformableMeshes;
        [HideInInspector] public int[] IndexCounts;
        [HideInInspector] public ComputeShader MeshRefit;
        [HideInInspector] public ComputeShader LightMeshRefit;
        [HideInInspector] public bool HasStarted;
        [HideInInspector] public BVHNode8DataCompressed[] AggNodes;
        [HideInInspector] public int InstanceMeshIndex;
        [HideInInspector] public int LightEndIndex;
        public AABB aabb_untransformed;
        public AABB aabb;
        [HideInInspector] public bool AllFull;
        [HideInInspector] public int AggIndexCount;
        [HideInInspector] public int AggBVHNodeCount;
        public List<MaterialData> _Materials;
        [HideInInspector] public int MatOffset;
        [HideInInspector] public StorableTransform[] CachedTransforms;
        [HideInInspector] public MeshDat CurMeshData;
        public int TotalObjects;
        [HideInInspector] public List<MeshTransformVertexs> TransformIndexes;
        [HideInInspector] public bool HasCompleted;
        [HideInInspector] public float TotEnergy;
        [HideInInspector] public Transform ThisTransform;

        [HideInInspector] public int ConstructKernel;
        [HideInInspector] public int TransferKernel;
        [HideInInspector] public int RefitLayerKernel;
        [HideInInspector] public int NodeUpdateKernel;
        [HideInInspector] public int NodeCompressKernel;
        [HideInInspector] public int NodeInitializerKernel;

        [HideInInspector] public int CompactedMeshData;

        [HideInInspector] public int InstanceReferences;

        [HideInInspector] public bool NeedsToUpdate;

        public int FailureCount = 0;

        public int TotalTriangles;
        public bool IsSkinnedGroup;
        public bool HasLightTriangles;

        private ComputeBuffer NodeBuffer;
        private ComputeBuffer AABBBuffer;
        private ComputeBuffer StackBuffer;

        private ComputeBuffer BVHDataBuffer;
        private ComputeBuffer ToBVHIndexBuffer;
        private ComputeBuffer CWBVHIndicesBuffer;
        private ComputeBuffer[] WorkingBuffer;
        private ComputeBuffer[] WorkingSet;
        public BVH2Builder BVH2;

        #if TTLightMapping
            public List<LightMapTriData>[] LightMapTris;
            public List<int> LightMapTexIndex;
            public List<int> PerRendererIndex;
        #endif

        [HideInInspector] public Layer[] ForwardStack;
        [HideInInspector] public Layer2[] LayerStack;
        [HideInInspector] public List<NodeIndexPairData> NodePair;
        [HideInInspector] public List<float> LuminanceWeights;

        [HideInInspector] public int MaxRecur = 0;
        [HideInInspector] public int[] ToBVHIndex;

        [HideInInspector] public AABB tempAABB;

        public int NodeOffset;
        public int TriOffset;
        public int LightTriOffset;
        public int LightNodeOffset;
        [HideInInspector] public List<BVHNode8DataFixed> SplitNodes;

        #if HardwareRT
            public List<int> HWRTIndex;
            public Renderer[] Renderers;
        #endif

        [System.Serializable]
        public struct StorableTransform {
            public Matrix4x4 WTL;
            public Vector3 Position;
        }

        [System.Serializable]
        public struct MeshTransformVertexs {
            public int VertexStart;
            public int VertexCount;
            public int IndexOffset;
        }

        public void CallUpdate() {
            if (!AssetManager.Assets.UpdateQue.Contains(this)) AssetManager.Assets.UpdateQue.Add(this);
        }  

        public void ClearAll() {
            CommonFunctions.DeepClean(ref _Materials);
            CommonFunctions.DeepClean(ref LightTriangles);
            CommonFunctions.DeepClean(ref TransformIndexes);
            CommonFunctions.DeepClean(ref LayerStack);
            CommonFunctions.DeepClean(ref NodePair);
            CommonFunctions.DeepClean(ref ToBVHIndex);
            CommonFunctions.DeepClean(ref AggTriangles);
            CommonFunctions.DeepClean(ref AggNodes);
            CommonFunctions.DeepClean(ref ForwardStack);
            CommonFunctions.DeepClean(ref LightTriNorms);
            CommonFunctions.DeepClean(ref CWBVHIndicesBufferInverted);
            CommonFunctions.DeepClean(ref CWBVHIndicesBufferInverted);
            CommonFunctions.DeepClean(ref LuminanceWeights);
            if(TrianglesArray.IsCreated) TrianglesArray.Dispose();
            if(BVH2 != null) {
                if(BVH2.BVH2NodesArray.IsCreated) BVH2.BVH2NodesArray.Dispose();
                if(BVH2.DimensionedIndicesArray.IsCreated) BVH2.DimensionedIndicesArray.Dispose();
                if(BVH2.tempArray.IsCreated) BVH2.tempArray.Dispose();
                if(BVH2.indices_going_left_array.IsCreated) BVH2.indices_going_left_array.Dispose();
                if(BVH2.CentersX.IsCreated) BVH2.CentersX.Dispose();
                if(BVH2.CentersY.IsCreated) BVH2.CentersY.Dispose();
                if(BVH2.CentersZ.IsCreated) BVH2.CentersZ.Dispose();
                if(BVH2.SAHArray.IsCreated) BVH2.SAHArray.Dispose();
            }
            if(BVH != null) {
                if(BVH.costArray.IsCreated) BVH.costArray.Dispose();
                if(BVH.decisionsArray.IsCreated) BVH.decisionsArray.Dispose();
            }
            BVH = null;
            CurMeshData.Clear();
            MeshCountChanged = true;
            HasCompleted = false;
            if (VertexBuffers != null)
            {
                for (int i = 0; i < VertexBuffers.Length; i++)
                {
                    VertexBuffers[i]?.Release();
                    IndexBuffers[i]?.Release();
                }
                NodeBuffer?.Release();
                AABBBuffer?.Release();
                StackBuffer?.Release();
                CWBVHIndicesBuffer?.Release();
                BVHDataBuffer?.Release();
                ToBVHIndexBuffer?.Release();
                if(WorkingBuffer != null) for(int i2 = 0; i2 < WorkingBuffer.Length; i2++) WorkingBuffer[i2]?.Release();
                if(WorkingSet != null) for(int i2 = 0; i2 < WorkingSet.Length; i2++) WorkingSet[i2]?.Release();
            }
            if (TriBuffer != null)
            {
                LightTriBuffer?.Release();
                LightNodeBuffer?.Release();
                TriBuffer?.Release();
                BVHBuffer?.Release();
            }
            if(LBVH != null) {
                LBVH.ClearAll();
            }
        }

        public void Reset(int Que) {
            ExistsInQue = Que;
            LoadData();
            AsyncTask = Task.Run(() => {BuildTotal();});
        }

        public void OnApplicationQuit()
        {
            if (VertexBuffers != null) {
                for (int i = 0; i < Mathf.Max(SkinnedMeshes.Length, DeformableMeshes.Length); i++) {
                    VertexBuffers[i].Release();
                    IndexBuffers[i].Release();
                    NodeBuffer.Release();
                    AABBBuffer.Release();
                    StackBuffer.Release();
                    CWBVHIndicesBuffer.Release();
                    BVHDataBuffer.Release();
                    ToBVHIndexBuffer.Release();
                    if(WorkingBuffer != null) for(int i2 = 0; i2 < WorkingBuffer.Length; i2++) WorkingBuffer[i2].Release();
                    if(WorkingSet != null) for(int i2 = 0; i2 < WorkingSet.Length; i2++) WorkingSet[i2]?.Release();
                }
            }
            if (TriBuffer != null) {
                LightTriBuffer.Release();
                LightNodeBuffer.Release();
                TriBuffer.Release();
                BVHBuffer.Release();
            }
        }


        public void init()
        {
            if(this == null || this.gameObject == null) return;
            this.gameObject.isStatic = false;
            Name = this.name;
            ThisTransform = this.transform;
            TransformIndexes = new List<MeshTransformVertexs>();
            _Materials = new List<MaterialData>();
            LightTriangles = new List<LightTriData>();
            LightTriNorms = new List<Vector3>();
            LuminanceWeights = new List<float>();
            MeshCountChanged = true;
            HasCompleted = false;
            MeshRefit = Resources.Load<ComputeShader>("Utility/BVHRefitter");
            LightMeshRefit = Resources.Load<ComputeShader>("Utility/LightBVHRefitter");
            ConstructKernel = MeshRefit.FindKernel("Construct");
            TransferKernel = MeshRefit.FindKernel("TransferKernel");
            RefitLayerKernel = MeshRefit.FindKernel("RefitLayer");
            NodeUpdateKernel = MeshRefit.FindKernel("NodeUpdate");
            NodeCompressKernel = MeshRefit.FindKernel("NodeCompress");
            NodeInitializerKernel = MeshRefit.FindKernel("NodeInitializer");
        }
        private bool NeedsToResetBuffers = true;
        public void SetUpBuffers()
        {
            if (NeedsToResetBuffers)
            {
                if (LightTriBuffer != null) LightTriBuffer.Release();
                if (LightNodeBuffer != null) LightNodeBuffer.Release();
                if (TriBuffer != null) TriBuffer.Release();
                if (BVHBuffer != null) BVHBuffer.Release();
                  if(AggTriangles != null) {
                    if(LightTriangles.Count == 0) {
                        LightTriBuffer = new ComputeBuffer(1, 44);
                        LightNodeBuffer = new ComputeBuffer(1, 40);
                    } else {
                        LightTriBuffer = new ComputeBuffer(Mathf.Max(LightTriangles.Count,1), 44);
                        LightNodeBuffer = new ComputeBuffer(Mathf.Max(LBVH.nodes.Length,1), 40);
                    }
                    TriBuffer = new ComputeBuffer(AggTriangles.Length, 88);
                    BVHBuffer = new ComputeBuffer(AggNodes.Length, 80);
                    if(HasLightTriangles) {
                        LightTriBuffer.SetData(LightTriangles);
                        LightNodeBuffer.SetData(LBVH.nodes);
                    }
                    TriBuffer.SetData(AggTriangles);
                    BVHBuffer.SetData(AggNodes);
                }
            }
        }

        public List<Texture> AlbedoTexs;
        public List<Texture> NormalTexs;
        public List<Texture> MetallicTexs;
        public List<int> MetallicTexChannelIndex;
        public List<Texture> RoughnessTexs;
        public List<int> RoughnessTexChannelIndex;
        public List<Texture> EmissionTexs;
        public List<Texture> AlphaTexs;
        public List<int> AlphaTexChannelIndex;
        public List<Texture> MatCapMasks;
        public List<int> MatCapMaskChannelIndex;
        public List<Texture> MatCapTexs;

        #if !NonAccurateLightTris
            List<Color[]> EmissionTexPixels;
            List<Vector2> EmissionTexWidthHeight;
        #endif
        
        private int TextureParse(ref Vector4 RefMat, Material Mat, string TexName, ref List<Texture> Texs, ref int TextureIndex, bool IsEmission = false) {
            TextureIndex = 0;
            if (Mat.HasProperty(TexName) && Mat.GetTexture(TexName) != null) {
                if(RefMat.x == 0) RefMat = new Vector4(Mat.mainTextureScale.x, Mat.mainTextureScale.y, Mat.mainTextureOffset.x, Mat.mainTextureOffset.y);
                Texture Tex = Mat.GetTexture(TexName);
                TextureIndex = Texs.IndexOf(Tex) + 1;
                if (TextureIndex != 0) {
                    return 0;
                } else {
                    #if !NonAccurateLightTris
                        if(IsEmission) {
                            RenderTexture tmp = RenderTexture.GetTemporary( 
                                Tex.width,
                                Tex.height,
                                0,
                                RenderTextureFormat.Default,
                                RenderTextureReadWrite.Linear);

                            Graphics.Blit(Tex, tmp);

                            RenderTexture previous = RenderTexture.active;
                            RenderTexture.active = tmp;
                            Texture2D myTexture2D = new Texture2D(Tex.width, Tex.height);
                            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                            myTexture2D.Apply();
                            RenderTexture.active = previous;
                            RenderTexture.ReleaseTemporary(tmp);

                            EmissionTexPixels.Add(myTexture2D.GetPixels(0));
                            DestroyImmediate(myTexture2D);
                            EmissionTexWidthHeight.Add(new Vector2(Tex.width, Tex.height));
                        }
                    #endif
                    Texs.Add(Tex);
                    TextureIndex = Texs.Count;
                    return 1;
                }
            }
            return 2;
        }

        public void CreateAtlas(ref int VertCount)
        {//Creates texture atlas
            #if !NonAccurateLightTris
                EmissionTexPixels = new List<Color[]>();
                EmissionTexWidthHeight = new List<Vector2>();
            #endif
            _Materials.Clear();
            AlbedoTexs = new List<Texture>();
            NormalTexs = new List<Texture>();
            MetallicTexs = new List<Texture>();
            RoughnessTexs = new List<Texture>();
            EmissionTexs = new List<Texture>();
            AlphaTexs = new List<Texture>();
            MatCapTexs = new List<Texture>();
            MatCapMasks = new List<Texture>();
            RoughnessTexChannelIndex = new List<int>();
            MetallicTexChannelIndex = new List<int>();
            AlphaTexChannelIndex = new List<int>();
            MatCapMaskChannelIndex = new List<int>();
            int CurMatIndex = 0;
            Mesh mesh;
            Vector4 Throwaway = Vector3.zero;
            foreach (RayTracingObject obj in ChildObjects) {
                if(obj == null) Debug.Log("WTF");
                List<Material> DoneMats = new List<Material>();
                if (obj.TryGetComponent<MeshFilter>(out MeshFilter TempMesh)) mesh = TempMesh.sharedMesh;
                else if(obj.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer TempSkin)) mesh = TempSkin.sharedMesh;
                else mesh = null;

                if(mesh == null) Debug.Log("Missing Mesh: " + name);
                obj.matfill();
                VertCount += mesh.vertexCount;
                Material[] SharedMaterials = new Material[1];
                if(obj.TryGetComponent<Renderer>(out Renderer TempRend)) SharedMaterials = TempRend.sharedMaterials;
                else if(obj.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer TempSkinRend)) SharedMaterials = TempSkinRend.sharedMaterials;
                int SharedMatLength = Mathf.Min(obj.Indexes.Length, SharedMaterials.Length);
                int Offset = 0;

                for (int i = 0; i < SharedMatLength; ++i) {
                    bool JustCreated = obj.JustCreated && obj.FollowMaterial[i] || obj.FollowMaterial[i];
                    MaterialData CurMat = new MaterialData();
                    bool GotSentBack = DoneMats.IndexOf(SharedMaterials[i]) != -1;
                    if (GotSentBack) Offset = DoneMats.IndexOf(SharedMaterials[i]);
                    else DoneMats.Add(SharedMaterials[i]);

                    RayObjectTextureIndex TempObj = new RayObjectTextureIndex();
                    TempObj.Obj = obj;
                    TempObj.ObjIndex = i;
                    int Index = AssetManager.ShaderNames.IndexOf(SharedMaterials[i].shader.name);
                    if (Index == -1) {
                        Debug.Log("Adding Material To XML: " + SharedMaterials[i].shader.name);

                        if (SharedMaterials[i].mainTexture != null) {
                            if (!AlbedoTexs.Contains(SharedMaterials[i].mainTexture)) {
                                AlbedoTexs.Add(SharedMaterials[i].mainTexture);
                                CurMat.AlbedoTex.x = AlbedoTexs.Count;
                            }
                        }
                        AssetManager.Assets.AddMaterial(SharedMaterials[i].shader);
                        Index = AssetManager.ShaderNames.IndexOf(SharedMaterials[i].shader.name);
                    }
                    MaterialShader RelevantMat = AssetManager.data.Material[Index];
                    if(!RelevantMat.MetallicRange.Equals("null") && JustCreated) obj.Metallic[i] = SharedMaterials[i].GetFloat(RelevantMat.MetallicRange);
                    if(!RelevantMat.RoughnessRange.Equals("null") && JustCreated) obj.Roughness[i] = SharedMaterials[i].GetFloat(RelevantMat.RoughnessRange);
                    if(RelevantMat.MetallicRemapMin != null && !RelevantMat.MetallicRemapMin.Equals("null") && JustCreated) obj.MetallicRemap[i] = new Vector2(SharedMaterials[i].GetFloat(RelevantMat.MetallicRemapMin), SharedMaterials[i].GetFloat(RelevantMat.MetallicRemapMax));
                    else if(JustCreated) obj.MetallicRemap[i] = new Vector2(0, 1);
                    if(RelevantMat.RoughnessRemapMin != null && !RelevantMat.RoughnessRemapMin.Equals("null") && JustCreated) obj.RoughnessRemap[i] = new Vector2(SharedMaterials[i].GetFloat(RelevantMat.RoughnessRemapMin), SharedMaterials[i].GetFloat(RelevantMat.RoughnessRemapMax));
                    else if(JustCreated) obj.RoughnessRemap[i] = new Vector2(0, 1);
                    if(!RelevantMat.BaseColorValue.Equals("null") && JustCreated) obj.BaseColor[i] = (Vector3)((Vector4)SharedMaterials[i].GetColor(RelevantMat.BaseColorValue));
                    else if(JustCreated) obj.BaseColor[i] = new Vector3(1,1,1);
                    if(RelevantMat.IsGlass && JustCreated || (JustCreated && RelevantMat.Name.Equals("Standard") && SharedMaterials[i].GetFloat("_Mode") == 3)) obj.SpecTrans[i] = 1f;
                    if(RelevantMat.IsCutout || (RelevantMat.Name.Equals("Standard") && SharedMaterials[i].GetFloat("_Mode") == 1)) obj.MaterialOptions[i] = RayTracingObject.Options.Cutout;

                    int TempIndex = 0;
                    Vector4 TempScale = new Vector4(1,1,0,0);
                    int Result;
                    int TexCount = RelevantMat.AvailableTextures.Count;
                    for(int i2 = 0; i2 < TexCount; i2++) {
                        string TexName = RelevantMat.AvailableTextures[i2].TextureName;
                        switch((TexturePurpose)RelevantMat.AvailableTextures[i2].Purpose) {
                            case(TexturePurpose.MatCapTex):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref MatCapTexs, ref TempIndex); 
                                CurMat.MatCapTex.x = TempIndex;
                            break;
                            case(TexturePurpose.Albedo):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref AlbedoTexs, ref TempIndex); 
                                CurMat.AlbedoTex.x = TempIndex;
                            break;
                            case(TexturePurpose.Normal):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref NormalTexs, ref TempIndex); 
                                CurMat.NormalTex.x = TempIndex;
                            break;
                            case(TexturePurpose.Emission):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref EmissionTexs, ref TempIndex, true); 
                                CurMat.EmissiveTex.x = TempIndex; 
                                if(Result != 2 && JustCreated) obj.emmission[i] = 12.0f;
                            break;
                            case(TexturePurpose.Metallic):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref MetallicTexs, ref TempIndex); 
                                CurMat.MetallicTex.x = TempIndex; 
                                if(Result == 1) MetallicTexChannelIndex.Add(RelevantMat.AvailableTextures[i2].ReadIndex);
                            break;
                            case(TexturePurpose.Roughness):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref RoughnessTexs, ref TempIndex); 
                                CurMat.RoughnessTex.x = TempIndex; 
                                if(Result == 1) RoughnessTexChannelIndex.Add(RelevantMat.AvailableTextures[i2].ReadIndex);
                            break;
                            case(TexturePurpose.Alpha):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref AlphaTexs, ref TempIndex); 
                                CurMat.AlphaTex.x = TempIndex; 
                                if(Result == 1) AlphaTexChannelIndex.Add(RelevantMat.AvailableTextures[i2].ReadIndex);
                            break;
                            case(TexturePurpose.MatCapMask):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref MatCapMasks, ref TempIndex); 
                                CurMat.MatCapMask.x = TempIndex; 
                                if(Result == 1) MatCapMaskChannelIndex.Add(RelevantMat.AvailableTextures[i2].ReadIndex);
                            break;
                        }
                    }

                    if(JustCreated) {
                        CurMat.AlbedoTextureScale = TempScale;
                        CurMat.SecondaryTextureScale = new Vector2(TempScale.x, TempScale.y);
                    }

                    if(JustCreated && obj.EmissionColor[i].x == 0 && obj.EmissionColor[i].y == 0 && obj.EmissionColor[i].z == 0) obj.EmissionColor[i] = new Vector3(1,1,1);
                    CurMat.MetallicRemap = obj.MetallicRemap[i];
                    CurMat.RoughnessRemap = obj.RoughnessRemap[i];
                    CurMat.BaseColor = obj.BaseColor[i];
                    CurMat.emmissive = obj.emmission[i];
                    CurMat.Roughness = obj.Roughness[i];
                    CurMat.specTrans = obj.SpecTrans[i];
                    CurMat.EmissionColor = obj.EmissionColor[i];
                    CurMat.MatType = (int)obj.MaterialOptions[i];
                    if(JustCreated) obj.Flags[i] = CommonFunctions.SetFlagVar(obj.Flags[i], CommonFunctions.Flags.UseSmoothness, RelevantMat.UsesSmoothness);
                    if(i == obj.BaseColor.Length - 1) obj.JustCreated = false;
                    obj.Indexes[i] = Offset;
                    obj.MaterialIndex[i] = CurMatIndex;
                    obj.LocalMaterialIndex[i] = CurMatIndex;
                    CurMatIndex++;
                    _Materials.Add(CurMat);
                    if(GotSentBack) Offset = DoneMats.Count;
                    Offset++;
                }
            }
        }

        public List<Transform> ChildObjectTransforms;
        public void LoadData() {
            HasLightTriangles = false;
            NeedsToResetBuffers = true;
            ClearAll();
            AllFull = false;
            TotEnergy = 0;
            init();
            CurMeshData = new MeshDat();
            if(this == null) return;
            Transform transf = transform;
            ParentScale = transf.lossyScale;
            ParentScale = new Vector3(Mathf.Abs(0.001f / ParentScale.x), Mathf.Abs(0.001f / ParentScale.y), 0.001f / Mathf.Abs(ParentScale.z));
            ChildObjects = new List<RayTracingObject>();
            ChildObjectTransforms = new List<Transform>();
            ChildObjectTransforms.Add(transf);
            int transfchildLength = transf.childCount;
            Transform[] ChildTransf = new Transform[transfchildLength];
            for(int i = 0; i < transfchildLength; i++) ChildTransf[i] = transf.GetChild(i);
            MeshFilter tempfilter = GetComponent<MeshFilter>();
            SkinnedMeshRenderer temprend = GetComponent<SkinnedMeshRenderer>();
            IsSkinnedGroup = temprend != null;
            if(tempfilter == null) for (int i = 0; i < transfchildLength; i++) if (ChildTransf[i].gameObject.activeInHierarchy && ChildTransf[i].gameObject.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer Skin) && !ChildTransf[i].gameObject.TryGetComponent<ParentObject>(out ParentObject Paren)) { IsSkinnedGroup = true; break; }
            if(IsSkinnedGroup) {
                var Temp = GetComponentsInChildren<SkinnedMeshRenderer>(false);
                int TempLength = Temp.Length;
                for(int i = 0; i < TempLength; i++) {
                    if(FailureCount > 2 && Application.isPlaying && !Temp[i].sharedMesh.isReadable) continue;
                    GameObject Target = Temp[i].gameObject;
                    if(Target.activeInHierarchy) {
                        if(Target.TryGetComponent<RayTracingObject>(out RayTracingObject TempRayObj)) {
                            if(!Target.TryGetComponent<ParentObject>(out ParentObject Paren2)) {
                                TempRayObj.matfill();
                                if(Target.TryGetComponent<RayTracingObject>(out RayTracingObject TempRayObj2)) {
                                    ChildObjectTransforms.Add(Target.transform);
                                }
                            }
                        }
                    }
                }
            } else {
                for (int i = 0; i < transfchildLength; i++) {
                    GameObject Target = ChildTransf[i].gameObject;
                    if(Target.TryGetComponent<RayTracingObject>(out RayTracingObject TempObj)) {
                        if(Application.isPlaying) {
                            if(Target.TryGetComponent<MeshFilter>(out MeshFilter TempFilter)) {
                                if((FailureCount > 2 && !TempFilter.sharedMesh.isReadable)) continue;
                            }
                        } 
                        if(Target.activeInHierarchy && !Target.TryGetComponent<ParentObject>(out ParentObject ThrowawayObj)) {
                            TempObj.matfill();
                            if(Target.TryGetComponent<RayTracingObject>(out RayTracingObject TempRayObj2)) {
                                if(Target.TryGetComponent<MeshRenderer>(out MeshRenderer TempRenderer)) {
                                    if(TempRenderer.enabled)
                                        ChildObjectTransforms.Add(ChildTransf[i]);
                                }
                            }
                        }
                    }
                }
            }
            int ChildCount = ChildObjectTransforms.Count;
            for(int i = 0; i < ChildCount; i++) {
                if(ChildObjectTransforms[i].gameObject.TryGetComponent<RayTracingObject>(out RayTracingObject Target))
                    if(ChildObjectTransforms[i] != this.transform) ChildObjects.Add(Target);
            }
            if(TryGetComponent<RayTracingObject>(out RayTracingObject TempObj2)) ChildObjects.Add(TempObj2);
            TotalObjects = ChildObjects.Count;
            CachedTransforms = new StorableTransform[TotalObjects + 1];
            CachedTransforms[0].WTL = transf.worldToLocalMatrix;
            CachedTransforms[0].Position = transf.position;
            for (int i = 0; i < TotalObjects; i++) {
                CachedTransforms[i + 1].WTL = ChildObjects[i].gameObject.transform.worldToLocalMatrix;
                CachedTransforms[i + 1].Position = ChildObjects[i].gameObject.transform.position;
            }
            if (ChildObjects == null || ChildObjects.Count == 0) {
                Debug.Log("NO RAYTRACINGOBJECT CHILDREN AT GAMEOBJECT: " + Name);
                this.enabled = false;
                return;
            }
            TotalObjects = ChildObjects.Count;
            if (IsSkinnedGroup) {
                HasStarted = false;
                SkinnedMeshes = new SkinnedMeshRenderer[TotalObjects];
                IndexCounts = new int[TotalObjects];
            } else if(IsDeformable) {
                HasStarted = false;
                DeformableMeshes = new MeshFilter[TotalObjects];
                IndexCounts = new int[TotalObjects];
            }

            int VertCount = 0;
            CreateAtlas(ref VertCount);

            int submeshcount;
            Mesh mesh = new Mesh();
            RayTracingObject CurrentObject;
            int MatIndex = 0;
            int RepCount = 0;
            TotalTriangles = 0;
            this.MatOffset = _Materials.Count;
            #if HardwareRT
                Renderers = new Renderer[TotalObjects];
            #endif

            CurMeshData.init(VertCount);
            #if TTLightMapping
                LightMapTexIndex = new List<int>();
                PerRendererIndex = new List<int>();
                for (int i = 0; i < TotalObjects; i++) {
                    CurrentObject = ChildObjects[i];
                    if(!LightMapTexIndex.Contains(CurrentObject.GetComponent<Renderer>().lightmapIndex)) LightMapTexIndex.Add(CurrentObject.GetComponent<Renderer>().lightmapIndex);

                }
                int LightMapCount = LightMapTexIndex.Count;
                LightMapTris = new List<LightMapTriData>[LightMapCount];
                for(int i = 0; i < LightMapCount; i++) {
                    LightMapTris[i] = new List<LightMapTriData>();
                }
            #endif

            for (int i = 0; i < TotalObjects; i++) {
                CurrentObject = ChildObjects[i];
                if (CurrentObject.TryGetComponent<MeshFilter>(out MeshFilter TempFilter)) {
                    mesh = TempFilter.sharedMesh;
                    if(IsDeformable)
                      DeformableMeshes[i] = TempFilter;
                } else if(CurrentObject.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer TempRend)) {
                    TempRend.BakeMesh(mesh);
                    if (IsSkinnedGroup)
                        SkinnedMeshes[i] = TempRend;
                }
                submeshcount = mesh.subMeshCount;
                #if TTLightMapping
                    PerRendererIndex.Add(CurrentObject.GetComponent<Renderer>().lightmapIndex);
                #endif
                #if HardwareRT
                    if(CurrentObject.TryGetComponent<Renderer>(out Renderers[i])) {}
                #endif

                var Tans = new List<Vector4>();
                mesh.GetTangents(Tans);
                if (Tans.Count != 0) CurMeshData.Tangents.AddRange(Tans);
                else CurMeshData.SetTansZero(mesh.vertexCount);

                var Norms = new List<Vector3>();
                mesh.GetNormals(Norms);
                CurMeshData.Normals.AddRange(Norms);

                int IndexOffset = CurMeshData.Verticies.Count;
                CurMeshData.Verticies.AddRange(mesh.vertices);

                int MeshUvLength = mesh.uv.Length;
                if (MeshUvLength == mesh.vertexCount) CurMeshData.UVs.AddRange(mesh.uv);
                else CurMeshData.SetUvZero(mesh.vertexCount);
                // if(mesh.uv2.Length != mesh.vertexCount) Debug.Log("FUCKED: " + CurrentObject.name);
                #if TTLightMapping
                    CurMeshData.AddLightmapUVs(mesh.uv2, CurrentObject.GetComponent<Renderer>().lightmapScaleOffset);
                #endif

                int PreIndexLength = CurMeshData.Indices.Count;
                CurMeshData.Indices.AddRange(mesh.triangles);
                int TotalIndexLength = 0;

                for (int i2 = 0; i2 < submeshcount; ++i2) {//Add together all the submeshes in the mesh to consider it as one object
                    int IndiceLength = (int)mesh.GetIndexCount(i2) / 3;
                    MatIndex = Mathf.Min(i2, CurrentObject.Names.Length) + RepCount;
                    TotalIndexLength += IndiceLength;
                    var SubMesh = new int[IndiceLength];
                    System.Array.Fill(SubMesh, MatIndex);
                    CurMeshData.MatDat.AddRange(SubMesh);
                }
                if (IsSkinnedGroup) {
                    IndexCounts[i] = TotalIndexLength;
                    SkinnedMeshes[i].updateWhenOffscreen = true;
                    TotalTriangles += TotalIndexLength;
                } else if(IsDeformable) {
                    IndexCounts[i] = TotalIndexLength;
                    TotalTriangles += TotalIndexLength;
                }
                TransformIndexes.Add(new MeshTransformVertexs() {
                    VertexStart = PreIndexLength,
                    VertexCount = CurMeshData.Indices.Count - PreIndexLength,
                    IndexOffset = IndexOffset
                });
                RepCount += Mathf.Min(submeshcount, CurrentObject.Names.Length);
            }
        }

    private List<Vector3Int> IsLeafList;
    unsafe public void DocumentNodes(int CurrentNode, int ParentNode, int NextNode, int NextBVH8Node, bool IsLeafRecur, int CurRecur) {
        NodeIndexPairData CurrentPair = NodePair[CurrentNode];
        MaxRecur = Mathf.Max(MaxRecur, CurRecur);
        IsLeafList[CurrentNode] = new Vector3Int(IsLeafList[CurrentNode].x, CurRecur, ParentNode);
        if (!IsLeafRecur) {
            ToBVHIndex[NextBVH8Node] = CurrentNode;
            IsLeafList[CurrentNode] = new Vector3Int(0, IsLeafList[CurrentNode].y, IsLeafList[CurrentNode].z);
            BVHNode8Data node = BVH.BVH8Nodes[NextBVH8Node];
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
        } else IsLeafList[CurrentNode] = new Vector3Int(1, IsLeafList[CurrentNode].y,IsLeafList[CurrentNode].z);

        NodePair[CurrentNode] = CurrentPair;
    }

        unsafe public void Construct()
        {

            tempAABB = new AABB();
            MaxRecur = 0;
            BVH2 = new BVH2Builder(Triangles, TrianglesArray.Length);//Binary BVH Builder, and also the component that takes the longest to build
            this.BVH = new BVH8Builder(ref BVH2);
            BVH2.BVH2NodesArray.Dispose();
            BVH2 = null;
            System.Array.Resize(ref BVH.BVH8Nodes, BVH.cwbvhnode_count);
            ToBVHIndex = new int[BVH.cwbvhnode_count];

            CWBVHIndicesBufferInverted = new int[BVH.cwbvh_indices.Length];
            int CWBVHIndicesBufferCount = CWBVHIndicesBufferInverted.Length;
            #if !HardwareRT
                for (int i = 0; i < CWBVHIndicesBufferCount; i++) CWBVHIndicesBufferInverted[BVH.cwbvh_indices[i]] = i;
            #else
                for (int i = 0; i < CWBVHIndicesBufferCount; i++) CWBVHIndicesBufferInverted[i] = i;
            #endif
            if (IsSkinnedGroup || IsDeformable)
            {
                NodePair = new List<NodeIndexPairData>();
                NodePair.Add(new NodeIndexPairData());
                IsLeafList = new List<Vector3Int>();
                IsLeafList.Add(new Vector3Int(0,0,0));
                DocumentNodes(0, 0, 1, 0, false, 0);
                MaxRecur++;
                int NodeCount = NodePair.Count;
                ForwardStack = new Layer[NodeCount];
                LayerStack = new Layer2[MaxRecur];
                Layer PresetLayer = new Layer();
                Layer2 TempSlab = new Layer2();
                TempSlab.Slab = new List<int>();

                for (int i = 0; i < MaxRecur; i++) {LayerStack[i] = new Layer2(); LayerStack[i].Slab = new List<int>();}
                for(int i = 0; i < 8; i++) PresetLayer.Children[i] = 0;

                for (int i = 0; i < NodePair.Count; i++) {
                    ForwardStack[i] = PresetLayer;
                    if (IsLeafList[i].x == 1) {
                        int first_triangle = (byte)BVH.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] & 0b11111;
                        int NumBits = CommonFunctions.NumberOfSetBits((byte)BVH.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] >> 5);
                        ForwardStack[i].Children[NodePair[i].InNodeOffset] = NumBits + ((int)BVH.BVH8Nodes[NodePair[i].BVHNode].base_index_triangle + first_triangle) * 24 + 1;
                    } else {
                        ForwardStack[i].Children[NodePair[i].InNodeOffset] = (-i) - 1;
                    }
                    ForwardStack[IsLeafList[i].z].Children[NodePair[i].InNodeOffset] = (-i) - 1;
                    
                    var TempLayer = LayerStack[IsLeafList[i].y];
                    TempLayer.Slab.Add(i);
                    LayerStack[IsLeafList[i].y] = TempLayer;
                }
                CommonFunctions.ConvertToSplitNodes(BVH, ref SplitNodes);
            }
            int LightTriLength = LightTriangles.Count;
            for(int i = 0; i < LightTriLength; i++) {
                LightTriData LT = LightTriangles[i];
                LT.TriTarget = (uint)CWBVHIndicesBufferInverted[LT.TriTarget];
                LightTriangles[i] = LT;
            }
            if(LightTriangles.Count > 0) LBVH = new LightBVHBuilder(LightTriangles, LightTriNorms, 0.1f, LuminanceWeights);


        }

        public List<int>[] Set;
        private void Refit(int Depth, int CurrentIndex) {
            if((2.0f * ((float)(LBVH.nodes[CurrentIndex].cosTheta_oe >> 16) / 32767.0f) - 1.0f) == 0) return;
            Set[Depth].Add(CurrentIndex);
            if(LBVH.nodes[CurrentIndex].left < 0) return;
            Refit(Depth + 1, LBVH.nodes[CurrentIndex].left);
            Refit(Depth + 1, LBVH.nodes[CurrentIndex].left + 1);
        }

        public void RefitMesh(ref ComputeBuffer RealizedAggNodes, ref ComputeBuffer RealizedTriBuffer, ref ComputeBuffer RealizedLightTriBuffer, ref ComputeBuffer RealizedLightNodeBuffer, CommandBuffer cmd)
        {
            #if HardwareRT
                for(int i = 0; i < Renderers.Length; i++) AssetManager.Assets.AccelStruct.UpdateInstanceTransform(Renderers[i]);
            #endif
            int KernelRatio = 256;

            AABB OverAABB = new AABB();
            tempAABB = new AABB();
            OverAABB.init();
            if(IsSkinnedGroup) {
                for (int i = 0; i < SkinnedMeshes.Length; i++) {
                    tempAABB.BBMax = SkinnedMeshes[i].bounds.center + SkinnedMeshes[i].bounds.size / 2.0f;
                    tempAABB.BBMin = SkinnedMeshes[i].bounds.center - SkinnedMeshes[i].bounds.size / 2.0f;
                    OverAABB.Extend(ref tempAABB);
                }
            } else {
                for (int i = 0; i < DeformableMeshes.Length; i++) {
                    tempAABB.BBMax = DeformableMeshes[i].gameObject.GetComponent<Renderer>().bounds.center + DeformableMeshes[i].gameObject.GetComponent<Renderer>().bounds.size / 2.0f;
                    tempAABB.BBMin = DeformableMeshes[i].gameObject.GetComponent<Renderer>().bounds.center - DeformableMeshes[i].gameObject.GetComponent<Renderer>().bounds.size / 2.0f;
                    OverAABB.Extend(ref tempAABB);
                }
            }

            aabb = OverAABB;
            if (!HasStarted) {
                if(IsSkinnedGroup) {                
                    VertexBuffers = new GraphicsBuffer[SkinnedMeshes.Length];
                    IndexBuffers = new ComputeBuffer[SkinnedMeshes.Length];
                } else {
                    VertexBuffers = new GraphicsBuffer[DeformableMeshes.Length];
                    IndexBuffers = new ComputeBuffer[DeformableMeshes.Length];
                }
                NodeBuffer = new ComputeBuffer(NodePair.Count, 32);
                NodeBuffer.SetData(NodePair);
                AABBBuffer = new ComputeBuffer(TotalTriangles, 24);
                StackBuffer = new ComputeBuffer(ForwardStack.Length, 32);
                StackBuffer.SetData(ForwardStack);
                CWBVHIndicesBuffer = new ComputeBuffer(BVH.cwbvh_indices.Length, 4);
                CWBVHIndicesBuffer.SetData(CWBVHIndicesBufferInverted);
                BVHDataBuffer = new ComputeBuffer(AggNodes.Length, 260);
                BVHDataBuffer.SetData(SplitNodes);
                SplitNodes.Clear();
                SplitNodes.TrimExcess();
                ToBVHIndexBuffer = new ComputeBuffer(ToBVHIndex.Length, 4);
                ToBVHIndexBuffer.SetData(ToBVHIndex);
                HasStarted = true;
                WorkingBuffer = new ComputeBuffer[LayerStack.Length];
                if(HasLightTriangles) {
                    Set = new List<int>[LBVH.MaxDepth];
                    WorkingSet = new ComputeBuffer[LBVH.MaxDepth];
                    for(int i = 0; i < LBVH.MaxDepth; i++) Set[i] = new List<int>();
                    Refit(0, 0);
                    for(int i = 0; i < LBVH.MaxDepth; i++) {
                        WorkingSet[i] = new ComputeBuffer(Set[i].Count, 4);
                        WorkingSet[i].SetData(Set[i]);
                    }
                }
                for (int i = 0; i < LayerStack.Length; i++) {
                    WorkingBuffer[i] = new ComputeBuffer(LayerStack[i].Slab.Count, 4);
                    WorkingBuffer[i].SetData(LayerStack[i].Slab);
                }
                for (int i = 0; i < VertexBuffers.Length; i++) {
                    if (IndexBuffers[i] != null) IndexBuffers[i].Release();
                    int[] IndexBuffer = IsSkinnedGroup ? SkinnedMeshes[i].sharedMesh.triangles : DeformableMeshes[i].sharedMesh.triangles;
                    IndexBuffers[i] = new ComputeBuffer(IndexBuffer.Length, 4, ComputeBufferType.Raw);
                    IndexBuffers[i].SetData(IndexBuffer);
                }
            }
            else if (AllFull)
            {
                cmd.SetComputeIntParam(MeshRefit, "TriBuffOffset", TriOffset);
                cmd.SetComputeIntParam(MeshRefit, "LightTriBuffOffset", LightTriOffset);
                cmd.SetComputeIntParam(LightMeshRefit, "LightTriBuffOffset", LightTriOffset);
                cmd.BeginSample("ReMesh");
                for (int i = 0; i < VertexBuffers.Length; i++) {
                    VertexBuffers[i].Release();
                    if(IsSkinnedGroup) {
                        SkinnedMeshes[i].vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                        VertexBuffers[i] = SkinnedMeshes[i].GetVertexBuffer();
                    } else {
                        DeformableMeshes[i].sharedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                        VertexBuffers[i] = DeformableMeshes[i].sharedMesh.GetVertexBuffer(0);
                    }
                }
                if(HasLightTriangles) cmd.SetComputeBufferParam(LightMeshRefit, 1, "LightTriangles", RealizedLightTriBuffer);
                cmd.SetComputeBufferParam(MeshRefit, RefitLayerKernel, "ReverseStack", StackBuffer);
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "Boxs", AABBBuffer);
                cmd.SetComputeBufferParam(MeshRefit, TransferKernel, "LightTrianglesOut", RealizedLightTriBuffer);
                cmd.SetComputeBufferParam(MeshRefit, TransferKernel, "CudaTriArrayIN", RealizedTriBuffer);
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "CudaTriArrayIN", TriBuffer);
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "CudaTriArray", RealizedTriBuffer);
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "CWBVHIndices", CWBVHIndicesBuffer);
                cmd.SetComputeBufferParam(MeshRefit, NodeInitializerKernel, "AllNodes", NodeBuffer);
                cmd.SetComputeBufferParam(MeshRefit, RefitLayerKernel, "AllNodes", NodeBuffer);
                cmd.SetComputeBufferParam(MeshRefit, RefitLayerKernel, "Boxs", AABBBuffer);
                cmd.SetComputeBufferParam(MeshRefit, NodeUpdateKernel, "AllNodes", NodeBuffer);
                cmd.SetComputeBufferParam(MeshRefit, NodeUpdateKernel, "BVHNodes", BVHDataBuffer);
                cmd.SetComputeBufferParam(MeshRefit, NodeUpdateKernel, "ToBVHIndex", ToBVHIndexBuffer);
                cmd.SetComputeBufferParam(MeshRefit, NodeCompressKernel, "BVHNodes", BVHDataBuffer);
                cmd.SetComputeBufferParam(MeshRefit, NodeCompressKernel, "AggNodes", RealizedAggNodes);
                int CurVertOffset = 0;
                cmd.BeginSample("ReMesh Accum");

                for (int i = 0; i < TotalObjects; i++)
                {
                    var SkinnedRootBone = IsSkinnedGroup ? SkinnedMeshes[i].rootBone : this.transform;
                    if(SkinnedRootBone == null) SkinnedRootBone = SkinnedMeshes[i].gameObject.transform;
                    int IndexCount = IndexCounts[i];

                    cmd.SetComputeIntParam(MeshRefit, "Stride", VertexBuffers[i].stride / 4);
                    if(IsSkinnedGroup) {
                        cmd.SetComputeMatrixParam(MeshRefit, "Transform", transform.worldToLocalMatrix * Matrix4x4.TRS(SkinnedRootBone.position, SkinnedRootBone.rotation, Vector3.one ));
                    } else {
                        cmd.SetComputeMatrixParam(MeshRefit, "Transform", Matrix4x4.identity);
                    }
                    cmd.SetComputeVectorParam(MeshRefit, "Offset", SkinnedRootBone.localPosition);
    
                    cmd.SetComputeIntParam(MeshRefit, "VertOffset", CurVertOffset);
                    cmd.SetComputeIntParam(MeshRefit, "gVertexCount", IndexCount);
                    cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "bufVertices", VertexBuffers[i]);
                    cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "bufIndexes", IndexBuffers[i]);
                    cmd.DispatchCompute(MeshRefit, ConstructKernel, (int)Mathf.Ceil(IndexCount / (float)KernelRatio), 1, 1);
                    CurVertOffset += IndexCount;
                }

                cmd.EndSample("ReMesh Accum");

                cmd.BeginSample("ReMesh Light Transfer");
                if(LightTriangles.Count != 0) {
                    cmd.SetComputeIntParam(MeshRefit, "gVertexCount", LightTriangles.Count);
                    cmd.DispatchCompute(MeshRefit, TransferKernel, (int)Mathf.Ceil(LightTriangles.Count / (float)KernelRatio), 1, 1);
                }
                cmd.EndSample("ReMesh Light Transfer");

                if(LightTriangles.Count != 0) {
                    cmd.BeginSample("LightRefitter");
                    cmd.SetComputeMatrixParam(LightMeshRefit, "ToWorld", transform.localToWorldMatrix);
                    cmd.SetComputeIntParam(LightMeshRefit, "TotalNodeOffset", LightNodeOffset);
                    cmd.SetComputeBufferParam(LightMeshRefit, 1, "LightNodes", RealizedLightNodeBuffer);
                    cmd.SetComputeFloatParam(LightMeshRefit, "FloatMax", float.MaxValue);
                    int ObjectOffset = LightNodeOffset;
                    for(int i = WorkingSet.Length - 1; i >= 0; i--) {
                        var ObjOffVar = ObjectOffset;
                        var SetCount = WorkingSet[i].count;
                        cmd.SetComputeIntParam(LightMeshRefit, "SetCount", SetCount);
                        cmd.SetComputeIntParam(LightMeshRefit, "ObjectOffset", ObjOffVar);
                        cmd.SetComputeBufferParam(LightMeshRefit, 1, "WorkingSet", WorkingSet[i]);
                        cmd.DispatchCompute(LightMeshRefit, 1, (int)Mathf.Ceil(SetCount / (float)256.0f), 1, 1);

                        ObjectOffset += SetCount;
                    }
                    cmd.EndSample("LightRefitter");
                }

                #if HardwareRT
                #else
                    cmd.BeginSample("ReMesh Init");
                    cmd.SetComputeIntParam(MeshRefit, "NodeCount", NodePair.Count);
                    cmd.DispatchCompute(MeshRefit, NodeInitializerKernel, (int)Mathf.Ceil(NodePair.Count / (float)KernelRatio), 1, 1);
                    cmd.EndSample("ReMesh Init");

                    cmd.BeginSample("ReMesh Refit");
                    for (int i = MaxRecur - 1; i >= 0; i--) {
                        var NodeCount2 = WorkingBuffer[i].count;
                        cmd.SetComputeIntParam(MeshRefit, "NodeCount", NodeCount2);
                        cmd.SetComputeBufferParam(MeshRefit, RefitLayerKernel, "WorkingBuffer", WorkingBuffer[i]);
                        cmd.DispatchCompute(MeshRefit, RefitLayerKernel, (int)Mathf.Ceil(NodeCount2 / (float)KernelRatio), 1, 1);
                    }
                    cmd.EndSample("ReMesh Refit");

                    cmd.BeginSample("ReMesh Update");
                    cmd.SetComputeIntParam(MeshRefit, "NodeCount", NodePair.Count);
                    cmd.DispatchCompute(MeshRefit, NodeUpdateKernel, (int)Mathf.Ceil(NodePair.Count / (float)KernelRatio), 1, 1);
                    cmd.EndSample("ReMesh Update");

                    cmd.BeginSample("ReMesh Compress");
                    cmd.SetComputeIntParam(MeshRefit, "NodeCount", BVH.BVH8Nodes.Length);
                    cmd.SetComputeIntParam(MeshRefit, "NodeOffset", NodeOffset);
                    cmd.DispatchCompute(MeshRefit, NodeCompressKernel, (int)Mathf.Ceil(NodePair.Count / (float)KernelRatio), 1, 1);
                    cmd.EndSample("ReMesh Compress");
                #endif
                cmd.EndSample("ReMesh");
            }

            if (!AllFull) {
                for (int i = 0; i < VertexBuffers.Length; i++)
                {
                    if(IsSkinnedGroup) {
                        SkinnedMeshes[i].vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                        VertexBuffers[i] = SkinnedMeshes[i].GetVertexBuffer();
                    } else {
                        DeformableMeshes[i].sharedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                        VertexBuffers[i] = DeformableMeshes[i].sharedMesh.GetVertexBuffer(0);
                    }
                }
                if (!((new List<GraphicsBuffer>(VertexBuffers)).Contains(null))) AllFull = true;
            }
        }


        private float AreaOfTriangle(Vector3 pt1, Vector3 pt2, Vector3 pt3)
        {
            float a = Vector3.Distance(pt1, pt2);
            float b = Vector3.Distance(pt2, pt3);
            float c = Vector3.Distance(pt3, pt1);
            float s = (a + b + c) / 2.0f;
            return Mathf.Sqrt(s * (s - a) * (s - b) * (s - c));
        }
        private float luminance(float r, float g, float b) { return 0.299f * r + 0.587f * g + 0.114f * b; }

        // Calculate the width and height of a triangle in 3D space
        public Vector2 CalculateTriangleWidthAndHeight(Vector3 pointA, Vector3 pointB, Vector3 pointC) {
            Vector3 sideAB = pointB - pointA;
            Vector3 sideBC = pointC - pointB;
            Vector3 sideAC = pointC - pointA;
            float width = Vector3.Cross(sideAB, sideAC).magnitude;
            float height = Vector3.Cross(sideAB, sideBC).magnitude;
            return new Vector2(width, height);
        }

        public unsafe async Task BuildTotal() {
            int IllumTriCount = 0;
            CudaTriangle TempTri = new CudaTriangle();
            Matrix4x4 ParentMatInv = CachedTransforms[0].WTL;
            Matrix4x4 ParentMat = CachedTransforms[0].WTL.inverse;
            Vector3 V1, V2, V3, Norm1, Norm2, Norm3, Tan1, Tan2, Tan3;
            TrianglesArray = new NativeArray<AABB>((TransformIndexes[TransformIndexes.Count - 1].VertexStart + TransformIndexes[TransformIndexes.Count - 1].VertexCount) / 3, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            Triangles = (AABB*)NativeArrayUnsafeUtility.GetUnsafePtr(TrianglesArray);
            AggTriangles = new CudaTriangle[(TransformIndexes[TransformIndexes.Count - 1].VertexStart + TransformIndexes[TransformIndexes.Count - 1].VertexCount) / 3];
            int OffsetReal;
            for (int i = 0; i < TotalObjects; i++) {//Transforming so all child objects are in the same object space
                #if TTLightMapping
                    int LightMapRendererIndex = LightMapTexIndex.IndexOf(PerRendererIndex[i]);
                #endif
                Matrix4x4 ChildMat = CachedTransforms[i + 1].WTL.inverse;
                Matrix4x4 TransMat = ParentMatInv * ChildMat;
                Vector3 Ofst = CachedTransforms[i + 1].WTL * CachedTransforms[i + 1].Position;
                Vector3 Ofst2 = ParentMatInv * CachedTransforms[0].Position;
                int IndexOffset = TransformIndexes[i].IndexOffset;
                int IndexEnd = TransformIndexes[i].VertexStart + TransformIndexes[i].VertexCount;
                OffsetReal = TransformIndexes[i].VertexStart / 3;
                bool IsSingle = CachedTransforms[i + 1].WTL.inverse == ParentMat;
                float scalex = Vector3.Distance(ChildMat * new Vector3(1,0,0), new Vector3(0,0,0));
                float scaley = Vector3.Distance(ChildMat * new Vector3(0,1,0), new Vector3(0,0,0));
                float scalez = Vector3.Distance(ChildMat * new Vector3(0,0,1), new Vector3(0,0,0));
                Vector3 Scale = IsSingle ? new Vector3(1,1,1) : new Vector3(Mathf.Pow(1.0f / scalex, 2.0f), Mathf.Pow(1.0f / scaley, 2.0f), Mathf.Pow(1.0f / scalez, 2.0f));
                for (int i3 = TransformIndexes[i].VertexStart; i3 < IndexEnd; i3 += 3) {//Transforming child meshes into the space of their parent
                    int Index1 = CurMeshData.Indices[i3] + IndexOffset;
                    int Index2 = CurMeshData.Indices[i3 + 2] + IndexOffset;
                    int Index3 = CurMeshData.Indices[i3 + 1] + IndexOffset;

                    V1 = CurMeshData.Verticies[Index1] + Ofst;
                    V2 = CurMeshData.Verticies[Index2] + Ofst;
                    V3 = CurMeshData.Verticies[Index3] + Ofst;
                    V1 = TransMat * V1;
                    V2 = TransMat * V2;
                    V3 = TransMat * V3;
                    V1 = V1 - Ofst2;
                    V2 = V2 - Ofst2;
                    V3 = V3 - Ofst2;

                    Tan1 = TransMat * (Vector3)CurMeshData.Tangents[Index1];
                    Tan2 = TransMat * (Vector3)CurMeshData.Tangents[Index2];
                    Tan3 = TransMat * (Vector3)CurMeshData.Tangents[Index3];
                   
                    Norm1 = TransMat * Vector3.Scale(Scale, CurMeshData.Normals[Index1]);
                    Norm2 = TransMat * Vector3.Scale(Scale, CurMeshData.Normals[Index2]);
                    Norm3 = TransMat * Vector3.Scale(Scale, CurMeshData.Normals[Index3]);

                    #if TTLightMapping
                        LightMapTris[LightMapRendererIndex].Add(new LightMapTriData() {
                            pos0 = ParentMat * V1,
                            posedge1 = ParentMat * (V2 - V1),
                            posedge2 = ParentMat * (V3 - V1),
                            LMUV0 = CurMeshData.LightmapUVs[Index1],
                            LMUV1 = CurMeshData.LightmapUVs[Index2],
                            LMUV2 = CurMeshData.LightmapUVs[Index3],
                            Norm1 = CommonFunctions.PackOctahedral((Vector3)(ParentMat * Norm1).normalized),
                            Norm2 = CommonFunctions.PackOctahedral((Vector3)(ParentMat * Norm2).normalized),
                            Norm3 = CommonFunctions.PackOctahedral((Vector3)(ParentMat * Norm3).normalized)
                        });
                    #endif


                    TempTri.tex0 = CurMeshData.UVs[Index1];
                    TempTri.texedge1 = CurMeshData.UVs[Index2];
                    TempTri.texedge2 = CurMeshData.UVs[Index3];

                    TempTri.pos0 = V1;
                    TempTri.posedge1 = V2 - V1;
                    TempTri.posedge2 = V3 - V1;
                    TempTri.norm0 = CommonFunctions.PackOctahedral(Norm1.normalized);
                    TempTri.norm1 = CommonFunctions.PackOctahedral(Norm2.normalized);
                    TempTri.norm2 = CommonFunctions.PackOctahedral(Norm3.normalized);

                    TempTri.tan0 = CommonFunctions.PackOctahedral(Tan1.normalized);
                    TempTri.tan1 = CommonFunctions.PackOctahedral(Tan2.normalized);
                    TempTri.tan2 = CommonFunctions.PackOctahedral(Tan3.normalized);

                    TempTri.MatDat = (uint)CurMeshData.MatDat[OffsetReal];
                    AggTriangles[OffsetReal] = TempTri;
                    Triangles[OffsetReal].Create(V1, V2);
                    Triangles[OffsetReal].Extend(V3);
                    Triangles[OffsetReal].Validate(ParentScale);

                    if (_Materials[(int)TempTri.MatDat].emmissive > 0.0f) {
                        bool IsValid = true;
                        #if !NonAccurateLightTris
                            if(_Materials[(int)TempTri.MatDat].EmissiveTex.x != 0) {
                                int ThisIndex = _Materials[(int)TempTri.MatDat].EmissiveTex.x - 1;
                                Vector2 UVV = (TempTri.tex0 + TempTri.texedge1 + TempTri.texedge2) / 3.0f;
                                int UVIndex3 = (int)Mathf.Max((Mathf.Floor(UVV.y * (EmissionTexWidthHeight[ThisIndex].y)) * EmissionTexWidthHeight[ThisIndex].x + Mathf.Floor(UVV.x * EmissionTexWidthHeight[ThisIndex].x)),0);
                                if(UVIndex3 < EmissionTexWidthHeight[ThisIndex].y * EmissionTexWidthHeight[ThisIndex].x)
                                    if(EmissionTexPixels[ThisIndex][UVIndex3].r < 0.1f && EmissionTexPixels[ThisIndex][UVIndex3].g < 0.1f && EmissionTexPixels[ThisIndex][UVIndex3].b < 0.1f) IsValid = false;
                            
                            }
                        #endif
                        if(IsValid) {
                            HasLightTriangles = true;
                            LuminanceWeights.Add(_Materials[(int)TempTri.MatDat].emmissive);
                            Vector3 Radiance = _Materials[(int)TempTri.MatDat].emmissive * _Materials[(int)TempTri.MatDat].BaseColor;
                            float radiance = luminance(Radiance.x, Radiance.y, Radiance.z);
                            float area = AreaOfTriangle(ParentMat * V1, ParentMat * V2, ParentMat * V3);
                            float e = radiance * area;
                            if(System.Double.IsNaN(area)) continue;
                            TotEnergy += area;
                            LightTriNorms.Add(((Norm1.normalized + Norm2.normalized + Norm3.normalized) / 3.0f).normalized);
                            LightTriangles.Add(new LightTriData() {
                                pos0 = TempTri.pos0,
                                posedge1 = TempTri.posedge1,
                                posedge2 = TempTri.posedge2,
                                TriTarget = (uint)(OffsetReal),
                                SourceEnergy = radiance
                                });
                            IllumTriCount++;
                        }
                    }
                    OffsetReal++;
                }
            }
            CurMeshData.Clear();
            #if !HardwareRT
                ConstructAABB();
                Construct();
                {//Compile Triangles
                    int TriLength = AggTriangles.Length;
                    NativeArray<CudaTriangle> Vector3Array = new NativeArray<CudaTriangle>(AggTriangles, Unity.Collections.Allocator.TempJob);
                    CudaTriangle* VecPointer = (CudaTriangle*)NativeArrayUnsafeUtility.GetUnsafePtr(Vector3Array);
                    for (int i = 0; i < TriLength; i++) AggTriangles[i] = VecPointer[BVH.cwbvh_indices[i]];
                    Vector3Array.Dispose();
                }
                AggNodes = new BVHNode8DataCompressed[BVH.BVH8Nodes.Length];
                CommonFunctions.Aggregate(ref AggNodes, ref BVH.BVH8Nodes);
            #else 
                if(IsSkinnedGroup || IsDeformable) {
                    ConstructAABB();
                    Construct();
                    AggNodes = new BVHNode8DataCompressed[BVH.BVH8Nodes.Length];
                    CommonFunctions.Aggregate(ref AggNodes, ref BVH.BVH8Nodes);
                } else {
                    AggNodes = new BVHNode8DataCompressed[1];
                    if(LightTriangles.Count > 0) LBVH = new LightBVHBuilder(LightTriangles, LightTriNorms, 0.1f, LuminanceWeights);
                }
            #endif
            MeshCountChanged = false;
            HasCompleted = true;
            NeedsToUpdate = false;
            TrianglesArray.Dispose();
            Debug.Log(Name + " Has Completed Building with " + AggTriangles.Length + " triangles");
            FailureCount = 0;
            
        }

        public void UpdateData() {
            #if !HardwareRT
                AggIndexCount = BVH.cwbvhindex_count;
                AggBVHNodeCount = BVH.cwbvhnode_count;
            #else 
                AggIndexCount = AggTriangles.Length;
                AggBVHNodeCount = 1;
            #endif
            UpdateAABB(this.transform);
            SetUpBuffers();
            NeedsToResetBuffers = false;
        }
        public Vector3 center;
        public Vector3 extent;

        public void UpdateAABB(Transform transform) {//Update the Transformed AABB by getting the new Max/Min of the untransformed AABB after transforming it
            #if HardwareRT
                for(int i = 0; i < Renderers.Length; i++) AssetManager.Assets.AccelStruct.UpdateInstanceTransform(Renderers[i]);
            #else
                Matrix4x4 Mat = transform.localToWorldMatrix;
                Vector3 new_center = CommonFunctions.transform_position(Mat, center);
                Vector3 new_extent = CommonFunctions.transform_direction(Mat, extent);

                aabb.BBMin = new_center - new_extent;
                aabb.BBMax = new_center + new_extent;
            #endif    
            transform.hasChanged = false;
        }

        private unsafe void ConstructAABB() {
            aabb_untransformed = new AABB();
            aabb_untransformed.init();
            int TriLength = TrianglesArray.Length;
            for (int i = 0; i < TriLength; i++) aabb_untransformed.Extend(ref Triangles[i]);
            center = 0.5f * (aabb_untransformed.BBMin + aabb_untransformed.BBMax);
            extent = 0.5f * (aabb_untransformed.BBMax - aabb_untransformed.BBMin);
        }

        private void OnEnable() {
            HasStarted = false;
            FailureCount = 0;
            if (gameObject.scene.isLoaded) {
                if (this.GetComponentInParent<InstancedManager>() != null) {
                    this.GetComponentInParent<InstancedManager>().AddQue.Add(this);
                    this.GetComponentInParent<InstancedManager>().ParentCountHasChanged = true;
                }
                else {
                    if(!AssetManager.Assets.RemoveQue.Contains(this)) {
                        AssetManager.Assets.AddQue.Add(this);
                        ExistsInQue = 3;
                    }
                    AssetManager.Assets.ParentCountHasChanged = true;
                }
                HasCompleted = false;
            }
        }

        private void OnDisable() {
            HasStarted = false;
            FailureCount = 0;
            if (gameObject.scene.isLoaded) {
                ClearAll();
                if (this.GetComponentInParent<InstancedManager>() != null) {
                    this.GetComponentInParent<InstancedManager>().RemoveQue.Add(this);
                    this.GetComponentInParent<InstancedManager>().ParentCountHasChanged = true;
                    var Instances = FindObjectsOfType(typeof(InstancedObject)) as InstancedObject[];
                    int Count = Instances.Length;
                    for(int i = 0; i < Count; i++) {
                        if(Instances[i].InstanceParent == this) Instances[i].OnParentClear();
                    }
                }
                else {
                    if(!AssetManager.Assets.RemoveQue.Contains(this)) AssetManager.Assets.RemoveQue.Add(this);
                    AssetManager.Assets.ParentCountHasChanged = true;
                }
                HasCompleted = false;
            }
        }


        // public void OnDrawGizmos() {

        //     int LightTriCount = LightTriangles.Count;
        //     for(int i = 0; i < LightTriCount; i++) {
        //         Vector3 Pos0 = this.transform.localToWorldMatrix * LightTriangles[i].pos0;
        //         Vector3 Pos1 = this.transform.localToWorldMatrix * (LightTriangles[i].pos0 + LightTriangles[i].posedge1);
        //         Vector3 Pos2 = this.transform.localToWorldMatrix * (LightTriangles[i].pos0 + LightTriangles[i].posedge2);
        //             Gizmos.DrawLine(Pos0, Pos1);
        //             Gizmos.DrawLine(Pos0, Pos2);
        //             Gizmos.DrawLine(Pos2, Pos1);
        //     }
        // }

    }






}