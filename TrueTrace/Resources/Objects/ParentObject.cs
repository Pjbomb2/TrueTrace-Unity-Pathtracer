using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;
using UnityEngine.Rendering;
#pragma warning disable 1998



namespace TrueTrace {
    [System.Serializable]
    public class ParentObject : MonoBehaviour
    {
        public Task AsyncTask;
        public int ExistsInQue = -1;
        [HideInInspector] public ComputeBuffer LightTriBuffer;
        [HideInInspector] public ComputeBuffer TriBuffer;
        [HideInInspector] public ComputeBuffer BVHBuffer;
        public string Name;
        [HideInInspector] public GraphicsBuffer[] VertexBuffers;
        [HideInInspector] public ComputeBuffer[] IndexBuffers;
        [HideInInspector] public int[] CWBVHIndicesBufferInverted;
        [HideInInspector] public List<RayTracingObject> ChildObjects;
        [HideInInspector] public bool MeshCountChanged;
        private AABB[] Triangles;
        [HideInInspector] public CudaTriangle[] AggTriangles;
        [HideInInspector] public Vector3 ParentScale;
        [HideInInspector] public List<LightTriData> LightTriangles;
        [HideInInspector] public BVH8Builder BVH;
        [HideInInspector] public SkinnedMeshRenderer[] SkinnedMeshes;
        [HideInInspector] public int[] IndexCounts;
        [HideInInspector] public ComputeShader MeshRefit;
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

        public AssetManager Assets;
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

        [HideInInspector] public Layer[] ForwardStack;
        [HideInInspector] public Layer2[] LayerStack;
        [HideInInspector] public List<NodeIndexPairData> NodePair;

        [HideInInspector] public int MaxRecur = 0;
        [HideInInspector] public int[] ToBVHIndex;

        [HideInInspector] public AABB tempAABB;

        public int NodeOffset;
        public int TriOffset;
        public int LightTriOffset;
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
            if (!Assets.UpdateQue.Contains(this)) Assets.UpdateQue.Add(this);
        }  

        public void ClearAll() {
            CommonFunctions.DeepClean(ref Triangles);
            CommonFunctions.DeepClean(ref _Materials);
            CommonFunctions.DeepClean(ref LightTriangles);
            CommonFunctions.DeepClean(ref TransformIndexes);
            CommonFunctions.DeepClean(ref LayerStack);
            CommonFunctions.DeepClean(ref NodePair);
            CommonFunctions.DeepClean(ref ToBVHIndex);
            CommonFunctions.DeepClean(ref AggTriangles);
            CommonFunctions.DeepClean(ref AggNodes);
            CommonFunctions.DeepClean(ref ForwardStack);
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
            }
            if (TriBuffer != null)
            {
                LightTriBuffer?.Release();
                TriBuffer?.Release();
                BVHBuffer?.Release();
            }
        }

        public void Reset(int Que) {
            ExistsInQue = Que;
            ClearAll();
            LoadData();
            AsyncTask = Task.Run(() => BuildTotal());
        }

        public void OnApplicationQuit()
        {
            if (VertexBuffers != null) {
                for (int i = 0; i < SkinnedMeshes.Length; i++) {
                    VertexBuffers[i].Release();
                    IndexBuffers[i].Release();
                    NodeBuffer.Release();
                    AABBBuffer.Release();
                    StackBuffer.Release();
                    CWBVHIndicesBuffer.Release();
                    BVHDataBuffer.Release();
                    ToBVHIndexBuffer.Release();
                    if(WorkingBuffer != null) for(int i2 = 0; i2 < WorkingBuffer.Length; i2++) WorkingBuffer[i2].Release();
                }
            }
            if (TriBuffer != null) {
                LightTriBuffer.Release();
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
            MeshCountChanged = true;
            HasCompleted = false;
            MeshRefit = Resources.Load<ComputeShader>("Utility/BVHRefitter");
            ConstructKernel = MeshRefit.FindKernel("Construct");
            TransferKernel = MeshRefit.FindKernel("TransferKernel");
            RefitLayerKernel = MeshRefit.FindKernel("RefitLayer");
            NodeUpdateKernel = MeshRefit.FindKernel("NodeUpdate");
            NodeCompressKernel = MeshRefit.FindKernel("NodeCompress");
            NodeInitializerKernel = MeshRefit.FindKernel("NodeInitializer");
            Assets = GameObject.Find("Scene").GetComponent<AssetManager>();
        }
        private bool NeedsToResetBuffers = true;
        public void SetUpBuffers()
        {
            if (NeedsToResetBuffers)
            {
                if (LightTriBuffer != null) LightTriBuffer.Release();
                if (TriBuffer != null) TriBuffer.Release();
                if (BVHBuffer != null) BVHBuffer.Release();
                  if(AggTriangles != null) {
                    LightTriBuffer = new ComputeBuffer(Mathf.Max(LightTriangles.Count,1), 40);
                    TriBuffer = new ComputeBuffer(AggTriangles.Length, 88);
                    BVHBuffer = new ComputeBuffer(AggNodes.Length, 80);
                    if(HasLightTriangles) LightTriBuffer.SetData(LightTriangles);
                    TriBuffer.SetData(AggTriangles);
                    BVHBuffer.SetData(AggNodes);
                }
            }
        }

        public struct objtextureindices
        {
            public int textureindexstart;
            public int textureindexcount;
        }

        public List<Texture> AlbedoTexs;
        public List<RayObjects> AlbedoIndexes;
        public List<Texture> NormalTexs;
        public List<RayObjects> NormalIndexes;
        public List<Texture> MetallicTexs;
        public List<RayObjects> MetallicIndexes;
        public List<int> MetallicTexChannelIndex;
        public List<Texture> RoughnessTexs;
        public List<RayObjects> RoughnessIndexes;
        public List<int> RoughnessTexChannelIndex;
        public List<Texture> EmissionTexs;
        public List<RayObjects> EmissionIndexes;


        private int TextureParse(ref Vector4 RefMat, RayObjectTextureIndex TexIndex, Material Mat, string TexName, ref List<Texture> Texs, ref List<RayObjects> Indexes) {
            if (Mat.HasProperty(TexName) && Mat.GetTexture(TexName) as Texture2D != null) {
                if(RefMat.x == 0) RefMat = new Vector4(Mat.GetTextureScale(TexName).x, Mat.GetTextureScale(TexName).y, Mat.GetTextureOffset(TexName).x, Mat.GetTextureOffset(TexName).y);
                if (Texs.Contains(Mat.GetTexture(TexName))) {
                    Indexes[Texs.IndexOf(Mat.GetTexture(TexName))].RayObjectList.Add(TexIndex);
                    return 0;
                }
                else {
                    Indexes.Add(new RayObjects());
                    Indexes[Indexes.Count - 1].RayObjectList.Add(TexIndex);
                    Texs.Add(Mat.GetTexture(TexName));
                    return 1;
                }
            }
            return 2;
        }

        public void CreateAtlas(ref int VertCount)
        {//Creates texture atlas

            _Materials.Clear();
            AlbedoTexs = new List<Texture>();
            AlbedoIndexes = new List<RayObjects>();
            NormalTexs = new List<Texture>();
            NormalIndexes = new List<RayObjects>();
            MetallicTexs = new List<Texture>();
            MetallicIndexes = new List<RayObjects>();
            RoughnessTexs = new List<Texture>();
            RoughnessIndexes = new List<RayObjects>();
            EmissionTexs = new List<Texture>();
            EmissionIndexes = new List<RayObjects>();
            RoughnessTexChannelIndex = new List<int>();
            MetallicTexChannelIndex = new List<int>();
            int CurMatIndex = 0;
            Mesh mesh;
            Vector4 Throwaway = Vector3.zero;
            foreach (RayTracingObject obj in ChildObjects) {
                if(obj == null) Debug.Log("WTF");
                List<Material> DoneMats = new List<Material>();
                if (obj.GetComponent<MeshFilter>() != null) mesh = obj.GetComponent<MeshFilter>().sharedMesh;
                else mesh = obj.GetComponent<SkinnedMeshRenderer>().sharedMesh;
                if(mesh == null) Debug.Log("Missing Mesh: " + name);
                obj.matfill();
                VertCount += mesh.vertexCount;
                Material[] SharedMaterials = (obj.GetComponent<Renderer>() != null) ? obj.GetComponent<Renderer>().sharedMaterials : obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials;
                int SharedMatLength = Mathf.Min(obj.Indexes.Length, SharedMaterials.Length);
                int Offset = 0;

                for (int i = 0; i < SharedMatLength; ++i) {
                    bool JustCreated = obj.JustCreated || obj.FollowMaterial[i];
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
                            if (AlbedoTexs.Contains(SharedMaterials[i].mainTexture)) {
                                AlbedoIndexes[AlbedoTexs.IndexOf(SharedMaterials[i].mainTexture)].RayObjectList.Add(TempObj);
                            } else {
                                AlbedoIndexes.Add(new RayObjects());
                                AlbedoIndexes[AlbedoIndexes.Count - 1].RayObjectList.Add(TempObj);
                                AlbedoTexs.Add(SharedMaterials[i].mainTexture);
                            }
                        }
                        Assets.AddMaterial(SharedMaterials[i].shader);
                        Index = AssetManager.ShaderNames.IndexOf(SharedMaterials[i].shader.name);
                    }
                    MaterialShader RelevantMat = AssetManager.data.Material[Index];
                    if(!RelevantMat.MetallicRange.Equals("null") && JustCreated) obj.Metallic[i] = SharedMaterials[i].GetFloat(RelevantMat.MetallicRange);
                    if(!RelevantMat.RoughnessRange.Equals("null") && JustCreated) obj.Roughness[i] = SharedMaterials[i].GetFloat(RelevantMat.RoughnessRange);
                    if(RelevantMat.MetallicRemapMin != null && !RelevantMat.MetallicRemapMin.Equals("null")) obj.MetallicRemap[i] = new Vector2(SharedMaterials[i].GetFloat(RelevantMat.MetallicRemapMin), SharedMaterials[i].GetFloat(RelevantMat.MetallicRemapMax));
                    else obj.MetallicRemap[i] = new Vector2(0, 1);
                    if(RelevantMat.RoughnessRemapMin != null && !RelevantMat.RoughnessRemapMin.Equals("null")) obj.RoughnessRemap[i] = new Vector2(SharedMaterials[i].GetFloat(RelevantMat.RoughnessRemapMin), SharedMaterials[i].GetFloat(RelevantMat.RoughnessRemapMax));
                    else obj.RoughnessRemap[i] = new Vector2(0, 1);
                    if(!RelevantMat.BaseColorValue.Equals("null") && JustCreated) obj.BaseColor[i] = (Vector3)((Vector4)SharedMaterials[i].GetColor(RelevantMat.BaseColorValue));
                    else if(JustCreated) obj.BaseColor[i] = new Vector3(1,1,1);
                    if(RelevantMat.IsGlass && JustCreated || (JustCreated && RelevantMat.Name.Equals("Standard") && SharedMaterials[i].GetFloat("_Mode") == 3)) obj.SpecTrans[i] = 1f;
                    if(RelevantMat.IsCutout || (RelevantMat.Name.Equals("Standard") && SharedMaterials[i].GetFloat("_Mode") == 1)) obj.MaterialOptions[i] = RayTracingObject.Options.Cutout;

                    int Result = TextureParse(ref CurMat.AlbedoTextureScale, TempObj, SharedMaterials[i], RelevantMat.BaseColorTex, ref AlbedoTexs, ref AlbedoIndexes);
                    if(!RelevantMat.NormalTex.Equals("null")) {Result = TextureParse(ref CurMat.AlbedoTextureScale, TempObj, SharedMaterials[i], RelevantMat.NormalTex, ref NormalTexs, ref NormalIndexes);}
                    if(!RelevantMat.EmissionTex.Equals("null")) {Result = TextureParse(ref CurMat.AlbedoTextureScale, TempObj, SharedMaterials[i], RelevantMat.EmissionTex, ref EmissionTexs, ref EmissionIndexes); if(Result != 2 && JustCreated) obj.emmission[i] = 12.0f;}
                    if(!RelevantMat.MetallicTex.Equals("null")) {Result = TextureParse(ref CurMat.AlbedoTextureScale, TempObj, SharedMaterials[i], RelevantMat.MetallicTex, ref MetallicTexs, ref MetallicIndexes); if(Result == 1) MetallicTexChannelIndex.Add(RelevantMat.MetallicTexChannel);}
                    if(!RelevantMat.RoughnessTex.Equals("null")) {Result = TextureParse(ref CurMat.AlbedoTextureScale, TempObj, SharedMaterials[i], RelevantMat.RoughnessTex, ref RoughnessTexs, ref RoughnessIndexes); if(Result == 1) RoughnessTexChannelIndex.Add(RelevantMat.RoughnessTexChannel);}

                    if(JustCreated && obj.EmissionColor[i].x == 0 && obj.EmissionColor[i].y == 0 && obj.EmissionColor[i].z == 0) obj.EmissionColor[i] = new Vector3(1,1,1);
                    CurMat.MetallicRemap = obj.MetallicRemap[i];
                    CurMat.RoughnessRemap = obj.RoughnessRemap[i];
                    CurMat.BaseColor = obj.BaseColor[i];
                    CurMat.emmissive = obj.emmission[i];
                    CurMat.Roughness = obj.Roughness[i];
                    CurMat.specTrans = obj.SpecTrans[i];
                    CurMat.EmissionColor = obj.EmissionColor[i];
                    CurMat.MatType = (int)obj.MaterialOptions[i];
                    obj.IsSmoothness[i] = RelevantMat.UsesSmoothness;
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
            ParentScale = this.transform.lossyScale;
            ParentScale = new Vector3(0.001f / ParentScale.x, 0.001f / ParentScale.y, 0.001f / ParentScale.z);
            ChildObjects = new List<RayTracingObject>();
            ChildObjectTransforms = new List<Transform>();
            ChildObjectTransforms.Add(this.transform);
            IsSkinnedGroup = this.gameObject.GetComponent<SkinnedMeshRenderer>() != null;
            for (int i = 0; i < this.transform.childCount; i++) if (this.GetComponent<MeshFilter>() == null && this.transform.GetChild(i).gameObject.GetComponent<SkinnedMeshRenderer>() != null && this.transform.GetChild(i).gameObject.GetComponent<ParentObject>() == null && this.transform.GetChild(i).gameObject.activeInHierarchy) { IsSkinnedGroup = true; break; }
            if(IsSkinnedGroup) {
                var Temp = this.GetComponentsInChildren<SkinnedMeshRenderer>();
                for(int i = 0; i < Temp.Length; i++) {
                    if(FailureCount > 2 && Application.isPlaying && !Temp[i].sharedMesh.isReadable) continue;
                    GameObject Target = Temp[i].gameObject;
                    if(Target.GetComponent<RayTracingObject>() != null && Target.GetComponent<ParentObject>() == null && Target.activeInHierarchy) {
                        Target.GetComponent<RayTracingObject>().matfill();
                        if(Target.GetComponent<RayTracingObject>() != null) {
                            ChildObjectTransforms.Add(Target.transform);
                        }
                    }
                }
            } else {
                for (int i = 0; i < this.transform.childCount; i++) {
                    GameObject Target = this.transform.GetChild(i).gameObject;
                    if(Application.isPlaying && Target.GetComponent<RayTracingObject>() != null && Target.GetComponent<MeshFilter>() != null && (!Target.GetComponent<MeshFilter>().sharedMesh.isReadable && FailureCount > 2)) continue;
                    if(Target.GetComponent<RayTracingObject>() != null && Target.GetComponent<ParentObject>() == null && Target.activeInHierarchy) {
                        Target.GetComponent<RayTracingObject>().matfill();
                        if(Target.GetComponent<RayTracingObject>() != null) {
                            ChildObjectTransforms.Add(Target.transform);
                        }
                    }
                }
            }
            for(int i = 0; i < ChildObjectTransforms.Count; i++) {
                RayTracingObject Target = ChildObjectTransforms[i].gameObject.GetComponent<RayTracingObject>();
                if(Target != null && ChildObjectTransforms[i] != this.transform) ChildObjects.Add(Target);
            }
            if(gameObject.GetComponent<RayTracingObject>() != null) ChildObjects.Add(gameObject.GetComponent<RayTracingObject>());
            CachedTransforms = new StorableTransform[ChildObjects.Count + 1];
            CachedTransforms[0].WTL = gameObject.transform.worldToLocalMatrix;
            CachedTransforms[0].Position = gameObject.transform.position;
            for (int i = 0; i < CachedTransforms.Length - 1; i++) {
                CachedTransforms[i + 1].WTL = ChildObjects[i].gameObject.transform.worldToLocalMatrix;
                CachedTransforms[i + 1].Position = ChildObjects[i].gameObject.transform.position;
            }
            if (ChildObjects == null || ChildObjects.Count == 0) {
                Debug.Log("NO RAYTRACINGOBJECT CHILDREN AT GAMEOBJECT: " + Name);
                DestroyImmediate(this);
                return;
            }
            TotalObjects = ChildObjects.Count;
            if (IsSkinnedGroup) {
                HasStarted = false;
                SkinnedMeshes = new SkinnedMeshRenderer[TotalObjects];
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

            for (int i = 0; i < TotalObjects; i++) {
                CurrentObject = ChildObjects[i];
                if (CurrentObject.GetComponent<MeshFilter>() != null) mesh = CurrentObject.GetComponent<MeshFilter>().sharedMesh;
                else CurrentObject.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh);

                submeshcount = mesh.subMeshCount;
                #if HardwareRT
                    Renderers[i] = CurrentObject.GetComponent<Renderer>();
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

                int PreIndexLength = CurMeshData.Indices.Count;
                CurMeshData.Indices.AddRange(mesh.triangles);
                int TotalIndexLength = 0;

                for (int i2 = 0; i2 < submeshcount; ++i2) {//Add together all the submeshes in the mesh to consider it as one object
                    int IndiceLength = (int)mesh.GetIndexCount(i2) / 3;
                    MatIndex = i2 + RepCount;
                    TotalIndexLength += IndiceLength;
                    var SubMesh = new int[IndiceLength];
                    System.Array.Fill(SubMesh, MatIndex);
                    CurMeshData.MatDat.AddRange(SubMesh);
                }
                if (IsSkinnedGroup) {
                    IndexCounts[i] = TotalIndexLength;
                    SkinnedMeshes[i] = ChildObjects[i].GetComponent<SkinnedMeshRenderer>();
                    SkinnedMeshes[i].updateWhenOffscreen = true;
                    TotalTriangles += TotalIndexLength;
                }
                TransformIndexes.Add(new MeshTransformVertexs() {
                    VertexStart = PreIndexLength,
                    VertexCount = CurMeshData.Indices.Count - PreIndexLength,
                    IndexOffset = IndexOffset
                });
                RepCount += submeshcount;
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
            BVH2Builder BVH2 = new BVH2Builder(ref Triangles);//Binary BVH Builder, and also the component that takes the longest to build
            this.BVH = new BVH8Builder(ref BVH2);
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
            if (IsSkinnedGroup)
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

                for (int i = 0; i < MaxRecur; i++) LayerStack[i] = TempSlab;
                for(int i = 0; i < 8; i++) PresetLayer.Leaf[i] = PresetLayer.Children[i] = -1;

                for (int i = 0; i < NodePair.Count; i++) {
                    ForwardStack[i] = PresetLayer;
                    if (IsLeafList[i].x == 1) {
                        int first_triangle = (byte)BVH.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] & 0b11111;
                        int NumBits = CommonFunctions.NumberOfSetBits((byte)BVH.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] >> 5);
                        ForwardStack[i].Children[NodePair[i].InNodeOffset] = NumBits + (int)BVH.BVH8Nodes[NodePair[i].BVHNode].base_index_triangle + first_triangle;
                        ForwardStack[i].Leaf[NodePair[i].InNodeOffset] = (int)BVH.BVH8Nodes[NodePair[i].BVHNode].base_index_triangle + first_triangle + 1;
                    } else {
                        ForwardStack[i].Children[NodePair[i].InNodeOffset] = i;
                        ForwardStack[i].Leaf[NodePair[i].InNodeOffset] = 0;
                    }
                    ForwardStack[IsLeafList[i].z].Children[NodePair[i].InNodeOffset] = i;
                    ForwardStack[IsLeafList[i].z].Leaf[NodePair[i].InNodeOffset] = 0;
                    
                    var TempLayer = LayerStack[IsLeafList[i].y];
                    TempLayer.Slab.Add(i);
                    LayerStack[IsLeafList[i].y] = TempLayer;
                }
                for(int i = 0; i < LayerStack.Length; i++) {
                    LayerStack[i].Slab.Sort((s1, s2) => (s1).CompareTo((s2)));
                }
                CommonFunctions.ConvertToSplitNodes(BVH, ref SplitNodes);
            }
            for(int i = 0; i < LightTriangles.Count; i++) {
                LightTriData LT = LightTriangles[i];
                LT.TriTarget = (uint)CWBVHIndicesBufferInverted[LT.TriTarget];
                LightTriangles[i] = LT;
            }


        }
        
        public void RefitMesh(ref ComputeBuffer RealizedAggNodes, ref ComputeBuffer RealizedTriBuffer, ref ComputeBuffer RealizedLightTriBuffer, CommandBuffer cmd)
        {
            #if HardwareRT
                for(int i = 0; i < Renderers.Length; i++) Assets.AccelStruct.UpdateInstanceTransform(Renderers[i]);
            #endif
            int KernelRatio = 256;

            AABB OverAABB = new AABB();
            tempAABB = new AABB();
            OverAABB.init();
            for (int i = 0; i < SkinnedMeshes.Length; i++) {
                tempAABB.BBMax = SkinnedMeshes[i].bounds.center + SkinnedMeshes[i].bounds.size / 2.0f;
                tempAABB.BBMin = SkinnedMeshes[i].bounds.center - SkinnedMeshes[i].bounds.size / 2.0f;
                OverAABB.Extend(ref tempAABB);
            }

            aabb = OverAABB;
            if (!HasStarted) {
                VertexBuffers = new GraphicsBuffer[SkinnedMeshes.Length];
                IndexBuffers = new ComputeBuffer[SkinnedMeshes.Length];
                NodeBuffer = new ComputeBuffer(NodePair.Count, 32);
                NodeBuffer.SetData(NodePair);
                AABBBuffer = new ComputeBuffer(TotalTriangles, 24);
                StackBuffer = new ComputeBuffer(ForwardStack.Length, 64);
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
                int MaxLength = 0;
                WorkingBuffer = new ComputeBuffer[LayerStack.Length];
                for (int i = 0; i < LayerStack.Length; i++) {
                    WorkingBuffer[i] = new ComputeBuffer(LayerStack[i].Slab.Count, 4);
                    WorkingBuffer[i].SetData(LayerStack[i].Slab);
                    MaxLength = Mathf.Max(MaxLength, LayerStack[i].Slab.Count);
                }
                for (int i = 0; i < VertexBuffers.Length; i++) {
                    if (IndexBuffers[i] != null) IndexBuffers[i].Release();
                    int[] IndexBuffer = SkinnedMeshes[i].sharedMesh.triangles;
                    IndexBuffers[i] = new ComputeBuffer(IndexBuffer.Length, 4, ComputeBufferType.Raw);
                    IndexBuffers[i].SetData(IndexBuffer);
                }
            }
            else if (AllFull)
            {
                cmd.BeginSample("ReMesh");
                for (int i = 0; i < VertexBuffers.Length; i++) {
                    VertexBuffers[i].Release();
                    SkinnedMeshes[i].vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                    VertexBuffers[i] = SkinnedMeshes[i].GetVertexBuffer();
                }
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
                    var SkinnedRootBone = SkinnedMeshes[i].rootBone;
                    if(SkinnedRootBone == null) continue;
                    int IndexCount = IndexCounts[i];

                    cmd.SetComputeIntParam(MeshRefit, "Stride", VertexBuffers[i].stride / 4);
                    cmd.SetComputeMatrixParam(MeshRefit, "Transform", transform.worldToLocalMatrix * Matrix4x4.TRS(SkinnedRootBone.position, SkinnedRootBone.rotation, Vector3.one ));
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


                cmd.BeginSample("ReMesh Init");
                cmd.SetComputeIntParam(MeshRefit, "NodeCount", NodePair.Count);
                cmd.DispatchCompute(MeshRefit, NodeInitializerKernel, (int)Mathf.Ceil(NodePair.Count / (float)KernelRatio), 1, 1);
                cmd.EndSample("ReMesh Init");

                cmd.BeginSample("ReMesh Refit");
                for (int i = MaxRecur - 1; i >= 0; i--) {
                    var NodeCount2 = LayerStack[i].Slab.Count;
                    cmd.SetComputeIntParam(MeshRefit, "NodeCount", NodeCount2);
                    cmd.SetComputeBufferParam(MeshRefit, RefitLayerKernel, "NodesToWork", WorkingBuffer[i]);
                    cmd.DispatchCompute(MeshRefit, RefitLayerKernel, (int)Mathf.Ceil(WorkingBuffer[i].count / (float)KernelRatio), 1, 1);
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
                cmd.EndSample("ReMesh");
            }

            if (!AllFull) {
                for (int i = 0; i < VertexBuffers.Length; i++)
                {
                    SkinnedMeshes[i].vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                    VertexBuffers[i] = SkinnedMeshes[i].GetVertexBuffer();
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

        public async ValueTask BuildTotal() {
            int IllumTriCount = 0;
            CudaTriangle TempTri = new CudaTriangle();
            Matrix4x4 ParentMatInv = CachedTransforms[0].WTL;
            Matrix4x4 ParentMat = CachedTransforms[0].WTL.inverse;
            Vector3 V1, V2, V3, Norm1, Norm2, Norm3, Tan1, Tan2, Tan3;
            Triangles = new AABB[(TransformIndexes[TransformIndexes.Count - 1].VertexStart + TransformIndexes[TransformIndexes.Count - 1].VertexCount) / 3];
            AggTriangles = new CudaTriangle[(TransformIndexes[TransformIndexes.Count - 1].VertexStart + TransformIndexes[TransformIndexes.Count - 1].VertexCount) / 3];
            int OffsetReal;

            for (int i = 0; i < TotalObjects; i++) {//Transforming so all child objects are in the same object space
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
                // float scaley = length(mul(Inverse, float3(0,1,0)));
                // float scalez = length(mul(Inverse, float3(0,0,1)));
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
                        HasLightTriangles = true;
                        Vector3 Radiance = _Materials[(int)TempTri.MatDat].emmissive * _Materials[(int)TempTri.MatDat].BaseColor;
                        float radiance = luminance(Radiance.x, Radiance.y, Radiance.z);
                        float area = AreaOfTriangle(ParentMat * V1, ParentMat * V2, ParentMat * V3);
                        float e = radiance * area;
                        if(System.Double.IsNaN(area)) continue;
                        TotEnergy += area;

                        LightTriangles.Add(new LightTriData() {
                            pos0 = TempTri.pos0,
                            posedge1 = TempTri.posedge1,
                            posedge2 = TempTri.posedge2,
                            TriTarget = (uint)(OffsetReal)
                            });
                        IllumTriCount++;
                    }
                    OffsetReal++;
                }
            }

            CurMeshData.Clear();
            #if !HardwareRT
                ConstructAABB();
                Construct();
                {//Compile Triangles
                    CudaTriangle[] Vector3Array = new CudaTriangle[AggTriangles.Length];
                    System.Array.Copy(AggTriangles, Vector3Array, Vector3Array.Length);
                    for (int i = 0; i < AggTriangles.Length; i++) AggTriangles[i] = Vector3Array[BVH.cwbvh_indices[i]];
                    Vector3Array = null;
                }
                AggNodes = new BVHNode8DataCompressed[BVH.BVH8Nodes.Length];
                CommonFunctions.Aggregate(ref AggNodes, ref BVH.BVH8Nodes);
            #else 
                if(IsSkinnedGroup) {
                    ConstructAABB();
                    Construct();
                    AggNodes = new BVHNode8DataCompressed[BVH.BVH8Nodes.Length];
                    CommonFunctions.Aggregate(ref AggNodes, ref BVH.BVH8Nodes);
                }else AggNodes = new BVHNode8DataCompressed[1];
            #endif
            MeshCountChanged = false;
            HasCompleted = true;
            NeedsToUpdate = false;

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
                for(int i = 0; i < Renderers.Length; i++) Assets.AccelStruct.UpdateInstanceTransform(Renderers[i]);
            #else
                Matrix4x4 Mat = transform.localToWorldMatrix;
                Vector3 new_center = CommonFunctions.transform_position(Mat, center);
                Vector3 new_extent = CommonFunctions.transform_direction(Mat, extent);

                aabb.BBMin = new_center - new_extent;
                aabb.BBMax = new_center + new_extent;
            #endif    
            transform.hasChanged = false;
        }

        private void ConstructAABB() {
            aabb_untransformed = new AABB();
            aabb_untransformed.init();
            for (int i = 0; i < Triangles.Length; i++) aabb_untransformed.Extend(ref Triangles[i]);
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
                    GameObject.Find("Scene").GetComponent<AssetManager>().AddQue.Add(this);
                    ExistsInQue = 3;
                    GameObject.Find("Scene").GetComponent<AssetManager>().ParentCountHasChanged = true;
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
                    GameObject.FindObjectsOfType<AssetManager>()[0].RemoveQue.Add(this);
                    GameObject.FindObjectsOfType<AssetManager>()[0].ParentCountHasChanged = true;
                }
                HasCompleted = false;
            }
        }

    }






}