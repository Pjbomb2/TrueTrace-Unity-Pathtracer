using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;


[System.Serializable]
public class ParentObject : MonoBehaviour {

[HideInInspector] public ComputeBuffer TriBuffer;
[HideInInspector] public ComputeBuffer BVHBuffer;
[HideInInspector] public List<int> ToIllumTriBuffer;
public string Name;
[HideInInspector] public GraphicsBuffer[] VertexBuffers;
[HideInInspector] public GraphicsBuffer[] IndexBuffers;
[HideInInspector] public int[] CWBVHIndicesBufferInverted;
[HideInInspector] public RayTracingObject[] ChildObjects;
[HideInInspector] public bool MeshCountChanged;
private AABB[] Triangles;
[HideInInspector] public CudaTriangle[] AggTriangles;
[HideInInspector] public Vector3 ParentScale;
[HideInInspector] public List<CudaLightTriangle> LightTriangles;
[HideInInspector] public BVH8Builder BVH;
[HideInInspector] public SkinnedMeshRenderer[] SkinnedMeshes;
[HideInInspector] public int[] IndexCounts;
[HideInInspector] public ComputeShader MeshRefit;
[HideInInspector] public bool HasStarted;
[HideInInspector] public BVHNode8DataCompressed[] AggNodes;
public AABB aabb_untransformed;
public AABB aabb;
[HideInInspector] public bool AllFull;
public Transform Hips;
[HideInInspector] public int AggIndexCount;
[HideInInspector] public int AggBVHNodeCount;
[HideInInspector] public int StaticBVHOffset;
[HideInInspector] public bool ObjectOrderHasChanged;
[HideInInspector] public List<MaterialData> _Materials;
[HideInInspector] public int MatOffset;
[HideInInspector] public int InstanceID;
[HideInInspector] public StorableTransform[] CachedTransforms;
public MeshDat CurMeshData;
public int TotalObjects;
[HideInInspector] public List<MeshTransformVertexs> TransformIndexes;
[HideInInspector] public bool HasCompleted;
[HideInInspector] public float TotEnergy;
[HideInInspector] public int LightCount;

[HideInInspector] public int ConstructKernel;
[HideInInspector] public int RefitLayerKernel;
[HideInInspector] public int NodeUpdateKernel;
[HideInInspector] public int NodeCompressKernel;
[HideInInspector] public int NodeInitializerKernel;

[HideInInspector]public int InstanceReferences;

[HideInInspector]public bool NeedsToUpdate;

public int TotalTriangles;
[HideInInspector]public bool IsSkinnedGroup;

private ComputeBuffer NodeBuffer;
private ComputeBuffer AdvancedTriangleBuffer;
private ComputeBuffer StackBuffer;
private ComputeBuffer VertexBufferOut;

private ComputeBuffer BVHDataBuffer;
private ComputeBuffer ToBVHIndexBuffer;
private ComputeBuffer CWBVHIndicesBuffer;

private ComputeBuffer WorkingBuffer;
public int AtlasSize;

[HideInInspector] public Layer[] ForwardStack;
[HideInInspector] public Layer2[] LayerStack;
private bool started = false;

[HideInInspector] public List<NodeIndexPairData> NodePair;
[HideInInspector] public int MaxRecur = 0;
[HideInInspector] public int[] ToBVHIndex;

[HideInInspector] public AABB tempAABB;

[HideInInspector] public int NodeOffset;
[HideInInspector] public int TriOffset;
[HideInInspector] public List<BVHNode8DataFixed> SplitNodes;

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

[System.Serializable]
public struct PerMatTextureData {
    public bool HasAlbedoMap;
    public bool HasNormalMap;
    public bool HasEmissiveMap;
    public bool HasMetallicMap;
    public bool HasRoughnessMap;
    public RayTracingObject MaterialObject;
    public int Offset;
}
public List<PerMatTextureData> MatTexData;


public void ClearAll() {

    Triangles = null;

    if(LightTriangles != null) {
        LightTriangles.Clear();
        LightTriangles.TrimExcess();
    }
    BVH = null;
    if(_Materials != null) {
        _Materials.Clear();
        _Materials.TrimExcess();
    }
    CurMeshData.Clear();
    TransformIndexes = null;
    MeshCountChanged = true;
    HasCompleted = false;
    LayerStack = null;
    if(NodePair != null) {
        NodePair.Clear();
        NodePair.TrimExcess();
    }
    ToBVHIndex = null;
    AggTriangles = null;
    AggNodes = null;
    ForwardStack = null;
    if(VertexBuffers != null) {
        for(int i = 0; i < SkinnedMeshes.Length; i++) {
            VertexBuffers[i].Dispose();
            IndexBuffers[i].Dispose();
            NodeBuffer.Dispose();
            AdvancedTriangleBuffer.Dispose();
            VertexBufferOut.Dispose();
            StackBuffer.Dispose();
            CWBVHIndicesBuffer.Dispose();
            BVHDataBuffer.Dispose();
            ToBVHIndexBuffer.Dispose();
            WorkingBuffer.Dispose();
        }
    }
    if(TriBuffer != null) {
        TriBuffer.Dispose();
        BVHBuffer.Dispose();
    }
}

public void OnApplicationQuit() {
    if(VertexBuffers != null) {
        for(int i = 0; i < SkinnedMeshes.Length; i++) {
            VertexBuffers[i].Dispose();
            IndexBuffers[i].Dispose();
            NodeBuffer.Dispose();
            AdvancedTriangleBuffer.Dispose();
            VertexBufferOut.Dispose();
            StackBuffer.Dispose();
            CWBVHIndicesBuffer.Dispose();
            BVHDataBuffer.Dispose();
            ToBVHIndexBuffer.Dispose();
            WorkingBuffer.Dispose();
        }
    }
    if(TriBuffer != null) {
        TriBuffer.Dispose();
        BVHBuffer.Dispose();
    }
}


public void init() {
    ToIllumTriBuffer = new List<int>();
    InstanceID = this.GetInstanceID();
    Name = this.name;
    TransformIndexes = new List<MeshTransformVertexs>();
    _Materials = new List<MaterialData>();
    LightTriangles = new List<CudaLightTriangle>();
    MeshCountChanged = true;
    ObjectOrderHasChanged = false;
    HasCompleted = false;
    MeshRefit =  Resources.Load<ComputeShader>("BVH/BVHRefitter");
    ConstructKernel = MeshRefit.FindKernel("Construct");
    RefitLayerKernel = MeshRefit.FindKernel("RefitLayer");
    NodeUpdateKernel = MeshRefit.FindKernel("NodeUpdate");
    NodeCompressKernel = MeshRefit.FindKernel("NodeCompress");
    NodeInitializerKernel = MeshRefit.FindKernel("NodeInitializer");
    AtlasSize = 8192;
}

public void SetUpBuffers() {
    if(TriBuffer != null) {
        TriBuffer.Release();
        BVHBuffer.Release();
    }
    TriBuffer = new ComputeBuffer(AggTriangles.Length, 136);
    BVHBuffer = new ComputeBuffer(AggNodes.Length, 80);
    TriBuffer.SetData(AggTriangles);
    BVHBuffer.SetData(AggNodes);
}

public void Release() {
    if(TriBuffer != null) TriBuffer.Dispose();
    if(BVHBuffer != null) BVHBuffer.Dispose();
}


public struct objtextureindices {
    public int textureindexstart;
    public int textureindexcount;
}

public List<Texture> AlbedoTexs;
public List<RayObjects> AlbedoIndexes;
public List<Texture> NormalTexs;
public List<RayObjects> NormalIndexes;
public List<Texture> MetallicTexs;
public List<RayObjects> MetallicIndexes;
public List<Texture> RoughnessTexs;
public List<RayObjects> RoughnessIndexes;
public List<Texture> EmissionTexs;
public List<RayObjects> EmissionIndexes;

public void CreateAtlas() {//Creates texture atlas
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
    MatTexData = new List<PerMatTextureData>();

    Mesh mesh = new Mesh();
    PerMatTextureData CurrentTexDat = new PerMatTextureData();
    foreach(RayTracingObject obj in ChildObjects) {
        List<Material> DoneMats = new List<Material>();
        obj.matfill();
        if(obj.GetComponent<MeshFilter>() != null) { 
            mesh = obj.GetComponent<MeshFilter>().sharedMesh;
        }else {
            mesh = obj.GetComponent<SkinnedMeshRenderer>().sharedMesh;
        }
        Material[] SharedMaterials = (obj.GetComponent<Renderer>() != null) ? obj.GetComponent<Renderer>().sharedMaterials : obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials;       
        int SharedMatLength = obj.Indexes.Length;
                    List<string> PropertyNames = new List<string>();
        int Offset = 0;
        for(int i = 0; i < SharedMatLength; ++i) {
            if(DoneMats.IndexOf(SharedMaterials[i]) != -1) {
                CurrentTexDat.Offset = DoneMats.IndexOf(SharedMaterials[i]);
                Offset++;
            } else {
                CurrentTexDat.Offset = Offset;
                Offset++;
                DoneMats.Add(SharedMaterials[i]);
            }
            CurrentTexDat.MaterialObject = obj;
            SharedMaterials[i].GetTexturePropertyNames(PropertyNames);
                    CurrentTexDat.HasRoughnessMap = false;
                    CurrentTexDat.HasAlbedoMap = false;
                    CurrentTexDat.HasNormalMap = false;
                    CurrentTexDat.HasEmissiveMap = false;
                    CurrentTexDat.HasMetallicMap = false;
                    RayObjectTextureIndex TempObj = new RayObjectTextureIndex();
                    TempObj.Obj = obj;
                    TempObj.ObjIndex = i;
                    if(PropertyNames.Contains("_Albedo1")) {
                        if(SharedMaterials[i].GetTexture("_Albedo1") != null) {
                            if(AlbedoTexs.Contains(SharedMaterials[i].GetTexture("_Albedo1"))) {
                                AlbedoIndexes[AlbedoTexs.IndexOf(SharedMaterials[i].GetTexture("_Albedo1"))].RayObjectList.Add(TempObj);
                            } else {
                                AlbedoIndexes.Add(new RayObjects());
                                AlbedoIndexes[AlbedoIndexes.Count - 1].RayObjectList.Add(TempObj);
                                AlbedoTexs.Add(SharedMaterials[i].GetTexture("_Albedo1"));
                            }
                        }
                        if(SharedMaterials[i].GetTexture("_Metallic1") != null) {
                            if(MetallicTexs.Contains(SharedMaterials[i].GetTexture("_Metallic1"))) {
                                MetallicIndexes[MetallicTexs.IndexOf(SharedMaterials[i].GetTexture("_Metallic1"))].RayObjectList.Add(TempObj);
                            } else {
                                MetallicIndexes.Add(new RayObjects());
                                MetallicIndexes[MetallicIndexes.Count - 1].RayObjectList.Add(TempObj);
                                MetallicTexs.Add(SharedMaterials[i].GetTexture("_Metallic1"));
                            }
                        }
                        if(SharedMaterials[i].GetTexture("_Normal1") != null) {
                            if(NormalTexs.Contains(SharedMaterials[i].GetTexture("_Normal1"))) {
                                NormalIndexes[NormalTexs.IndexOf(SharedMaterials[i].GetTexture("_Normal1"))].RayObjectList.Add(TempObj);
                            } else {
                                NormalIndexes.Add(new RayObjects());
                                NormalIndexes[NormalIndexes.Count - 1].RayObjectList.Add(TempObj);
                                NormalTexs.Add(SharedMaterials[i].GetTexture("_Normal1"));
                            }
                        }
                    } else { 
                    if(PropertyNames.Contains("_Albedo")) {
                        if(SharedMaterials[i].GetTexture("_Albedo") != null) {
                            if(AlbedoTexs.Contains(SharedMaterials[i].GetTexture("_Albedo"))) {
                                AlbedoIndexes[AlbedoTexs.IndexOf(SharedMaterials[i].GetTexture("_Albedo"))].RayObjectList.Add(TempObj);
                            } else {
                                AlbedoIndexes.Add(new RayObjects());
                                AlbedoIndexes[AlbedoIndexes.Count - 1].RayObjectList.Add(TempObj);
                                AlbedoTexs.Add(SharedMaterials[i].GetTexture("_Albedo"));
                            }
                        }                        
                    } else {
                        if(SharedMaterials[i].mainTexture != null) {
                            if(AlbedoTexs.Contains(SharedMaterials[i].mainTexture)) {
                                AlbedoIndexes[AlbedoTexs.IndexOf(SharedMaterials[i].mainTexture)].RayObjectList.Add(TempObj);
                            } else {
                                AlbedoIndexes.Add(new RayObjects());
                                AlbedoIndexes[AlbedoIndexes.Count - 1].RayObjectList.Add(TempObj);
                                AlbedoTexs.Add(SharedMaterials[i].mainTexture);
                            }
                        }
                    }
                    if(PropertyNames.Contains("_Normals")) {
                        if(SharedMaterials[i].GetTexture("_Normals") != null) {
                            if(NormalTexs.Contains(SharedMaterials[i].GetTexture("_Normals"))) {
                                NormalIndexes[NormalTexs.IndexOf(SharedMaterials[i].GetTexture("_Normals"))].RayObjectList.Add(TempObj);
                            } else {
                                NormalIndexes.Add(new RayObjects());
                                NormalIndexes[NormalIndexes.Count - 1].RayObjectList.Add(TempObj);
                                NormalTexs.Add(SharedMaterials[i].GetTexture("_Normals"));
                            }
                        }                        
                    } else {
                        if(SharedMaterials[i].GetTexture("_BumpMap") != null) {
                            if(NormalTexs.Contains(SharedMaterials[i].GetTexture("_BumpMap"))) {
                                NormalIndexes[NormalTexs.IndexOf(SharedMaterials[i].GetTexture("_BumpMap"))].RayObjectList.Add(TempObj);
                            } else {
                                NormalIndexes.Add(new RayObjects());
                                NormalIndexes[NormalIndexes.Count - 1].RayObjectList.Add(TempObj);
                                NormalTexs.Add(SharedMaterials[i].GetTexture("_BumpMap"));
                            }
                        }
                    }
                    if(SharedMaterials[i].GetTexture("_EmissionMap") != null) {
                        if(EmissionTexs.Contains(SharedMaterials[i].GetTexture("_EmissionMap"))) {
                            EmissionIndexes[EmissionTexs.IndexOf(SharedMaterials[i].GetTexture("_EmissionMap"))].RayObjectList.Add(TempObj);
                        } else {
                            EmissionIndexes.Add(new RayObjects());
                            EmissionIndexes[EmissionIndexes.Count - 1].RayObjectList.Add(TempObj);
                            EmissionTexs.Add(SharedMaterials[i].GetTexture("_EmissionMap"));
                        }
                    }
                    if(!PropertyNames.Contains("_Metallic")) {
                        if(SharedMaterials[i].GetTexture("_MetallicGlossMap") != null) {
                            if(MetallicTexs.Contains(SharedMaterials[i].GetTexture("_MetallicGlossMap"))) {
                                MetallicIndexes[MetallicTexs.IndexOf(SharedMaterials[i].GetTexture("_MetallicGlossMap"))].RayObjectList.Add(TempObj);
                            } else {
                                MetallicIndexes.Add(new RayObjects());
                                MetallicIndexes[MetallicIndexes.Count - 1].RayObjectList.Add(TempObj);
                                MetallicTexs.Add(SharedMaterials[i].GetTexture("_MetallicGlossMap"));
                            }
                        }
                    } else {
                        if(SharedMaterials[i].GetTexture("_Metallic") != null) {
                            if(MetallicTexs.Contains(SharedMaterials[i].GetTexture("_Metallic"))) {
                                MetallicIndexes[MetallicTexs.IndexOf(SharedMaterials[i].GetTexture("_Metallic"))].RayObjectList.Add(TempObj);
                            } else {
                                MetallicIndexes.Add(new RayObjects());
                                MetallicIndexes[MetallicIndexes.Count - 1].RayObjectList.Add(TempObj);
                                MetallicTexs.Add(SharedMaterials[i].GetTexture("_Metallic"));
                            }
                        }
                    }
                    if(SharedMaterials[i].GetTexture("_OcclusionMap") != null) {
                        if(RoughnessTexs.Contains(SharedMaterials[i].GetTexture("_OcclusionMap"))) {
                            RoughnessIndexes[RoughnessTexs.IndexOf(SharedMaterials[i].GetTexture("_OcclusionMap"))].RayObjectList.Add(TempObj);
                        } else {
                            RoughnessIndexes.Add(new RayObjects());
                            RoughnessIndexes[RoughnessIndexes.Count - 1].RayObjectList.Add(TempObj);
                            RoughnessTexs.Add(SharedMaterials[i].GetTexture("_OcclusionMap"));
                        }
                    }
                }

            MatTexData.Add(CurrentTexDat);

        }
    }
    RayTracingObject PreviousObject = MatTexData[0].MaterialObject;
    int CurrentObjectOffset = -1;
    int CurMat = 0;
    foreach(PerMatTextureData Obj in MatTexData) {
        if(PreviousObject.Equals(Obj.MaterialObject)) {
            CurrentObjectOffset++;
        } else {
            CurrentObjectOffset = 0;
        }
        _Materials.Add(new MaterialData() {
            BaseColor = Obj.MaterialObject.BaseColor[CurrentObjectOffset],
            emmissive = Obj.MaterialObject.emmission[CurrentObjectOffset],
            Roughness = Obj.MaterialObject.Roughness[CurrentObjectOffset],
            MatType = (int)Obj.MaterialObject.MaterialOptions[CurrentObjectOffset],
            EmissionColor = Obj.MaterialObject.EmissionColor[CurrentObjectOffset]
        });
        // Debug.Log(Obj.MaterialObject.Indexes.Length + ", " + Obj.MaterialObject.MaterialIndex.Length);
        Obj.MaterialObject.Indexes[CurrentObjectOffset] = Obj.Offset;
        Obj.MaterialObject.MaterialIndex[CurrentObjectOffset] = CurMat;
        Obj.MaterialObject.LocalMaterialIndex[CurrentObjectOffset] = CurMat;
        PreviousObject = Obj.MaterialObject;
        CurMat++;
    }
    MatTexData.Clear();
}

public void LoadData() {
    ClearAll();
    AllFull = false;
    TotEnergy = 0;
    LightCount = 0;
    init();
    CurMeshData = new MeshDat();
    CurMeshData.init();
    ParentScale = this.transform.lossyScale;
    List<RayTracingObject> TempObjects = new List<RayTracingObject>();
    List<Transform> TempObjectTransforms = new List<Transform>();
    TempObjectTransforms.Add(this.transform);
    IsSkinnedGroup = false;
    for(int i = 0; i < this.transform.childCount; i++) if(this.transform.GetChild(i).gameObject.GetComponent<SkinnedMeshRenderer>() != null && this.transform.GetChild(i).gameObject.activeInHierarchy ) {IsSkinnedGroup = true; break;}
    
    if(!IsSkinnedGroup) {
        for(int i = 0; i < this.transform.childCount; i++) {
            if(this.transform.GetChild(i).gameObject.GetComponent<RayTracingObject>() != null && this.transform.GetChild(i).gameObject.activeInHierarchy && this.transform.GetChild(i).gameObject.GetComponent<ParentObject>() == null) {
                TempObjectTransforms.Add(this.transform.GetChild(i));
                TempObjects.Add(this.transform.GetChild(i).gameObject.GetComponent<RayTracingObject>());
            }
        }
    } else {
        var TempObjects2 = GetComponentsInChildren<RayTracingObject>();
        TempObjects = new List<RayTracingObject>(TempObjects2);
        int TempObjectCount = TempObjects.Count;
        for(int i = TempObjectCount - 1; i >= 0; i--) {
            if(TempObjects[i].gameObject.GetComponent<SkinnedMeshRenderer>() != null) {
                TempObjectTransforms.Add(TempObjects[i].gameObject.transform);
            } else {
                TempObjects.RemoveAt(i);
            }
        }
    }
    if(this.gameObject.GetComponent<RayTracingObject>() != null) {
        TempObjects = new List<RayTracingObject>();
        TempObjects.Add(this.gameObject.GetComponent<RayTracingObject>());
        TempObjectTransforms.Add(this.transform);
    } else if(TempObjects == null || TempObjects.Count == 0) {
        Debug.Log("NO RAYTRACINGOBJECT CHILDREN AT GAMEOBJECT: " + Name);
    }
    
    Transform[] TempTransforms = TempObjectTransforms.ToArray();
    CachedTransforms = new StorableTransform[TempTransforms.Length];
    for(int i = 0; i < TempTransforms.Length; i++) {
        CachedTransforms[i].WTL = TempTransforms[i].worldToLocalMatrix;
        CachedTransforms[i].Position = TempTransforms[i].position;
    }
    TempObjectTransforms.Clear();
    TempObjectTransforms.Capacity = 0;
    ChildObjects = TempObjects.ToArray();
    TempObjects.Clear();
    TempObjects.Capacity = 0;
    TotalObjects = ChildObjects.Length;
    if(IsSkinnedGroup) {
        HasStarted = false;
        SkinnedMeshes = new SkinnedMeshRenderer[TotalObjects];
        IndexCounts = new int[TotalObjects];
    }
    CreateAtlas();
    int submeshcount;
    Mesh mesh;
    RayTracingObject CurrentObject;
    int MatIndex = 0;
    int RepCount = 0;
    this.MatOffset = _Materials.Count;
    for(int i = 0; i < TotalObjects; i++) {
        CurrentObject = ChildObjects[i];
            mesh = new Mesh();
            if(CurrentObject.GetComponent<MeshFilter>() != null)
                mesh = CurrentObject.GetComponent<MeshFilter>().sharedMesh;
            else
                CurrentObject.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh, true);

            submeshcount = mesh.subMeshCount;
            
            var Tans = new List<Vector4>();
            mesh.GetTangents(Tans);
            if(Tans.Count != 0) CurMeshData.Tangents.AddRange(Tans);
            else CurMeshData.SetTansZero(mesh.vertexCount);
            var Norms = new List<Vector3>();
            mesh.GetNormals(Norms);
            CurMeshData.Normals.AddRange(Norms);
            int IndexOffset = CurMeshData.Verticies.Count;
            CurMeshData.Verticies.AddRange(mesh.vertices);
            int MeshUvLength = mesh.uv.Length;
            
            if(MeshUvLength == mesh.vertexCount) CurMeshData.UVs.AddRange(mesh.uv);
            else CurMeshData.SetUvZero(mesh.vertexCount);

            int PreIndexLength = CurMeshData.Indices.Count;
            CurMeshData.Indices.AddRange(mesh.triangles);  
            int TotalIndexLength = 0;
            for(int i2 = 0; i2 < submeshcount; ++i2) {//Add together all the submeshes in the mesh to consider it as one object
                int IndiceLength = (int)mesh.GetIndexCount(i2) / 3;
                TotalIndexLength += IndiceLength;
                MatIndex = i2 + RepCount;
                var SubMesh = new int[IndiceLength];
                System.Array.Fill(SubMesh, MatIndex);
                CurMeshData.MatDat.AddRange(SubMesh);
            }
            if(IsSkinnedGroup) {
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



int NumberOfSetBits(int i)
{
    i = i - ((i >> 1) & 0x55555555);
    i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
    return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
}

unsafe public void DocumentNodes(int CurrentNode, int ParentNode, int NextNode, int NextBVH8Node, bool IsLeafRecur, int CurRecur) {
    NodeIndexPairData CurrentPair = NodePair[CurrentNode];
    MaxRecur = Mathf.Max(MaxRecur, CurRecur);
    CurrentPair.PreviousNode = ParentNode;
    CurrentPair.Node = CurrentNode;
    CurrentPair.RecursionCount = CurRecur;
    if(!IsLeafRecur) {
        ToBVHIndex[NextBVH8Node] = CurrentNode;
        CurrentPair.IsLeaf = 0;
        BVHNode8Data node = BVH.BVH8Nodes[NextBVH8Node];
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



public struct TriangleData {
    public Vector3 V1, V2, V3;
    public Vector3 N1, N2, N3;
}
public TriangleData[] Tris1;
public TriangleData[] Tris2;

unsafe public void Construct() {
    tempAABB = new AABB();
    MaxRecur = 0;
    started = false;
    BVH2Builder BVH2 = new BVH2Builder(ref Triangles);//Binary BVH Builder, and also the component that takes the longest to build
    this.BVH = new BVH8Builder(ref BVH2);
    BVH2 = null;
    System.Array.Resize(ref BVH.BVH8Nodes, BVH.cwbvhnode_count);
    ToBVHIndex = new int[BVH.cwbvhnode_count];
    
    if(IsSkinnedGroup) {
        CWBVHIndicesBufferInverted = new int[BVH.cwbvh_indices.Length];
        int CWBVHIndicesBufferCount = CWBVHIndicesBufferInverted.Length;
        for(int i = 0; i < CWBVHIndicesBufferCount; i++) CWBVHIndicesBufferInverted[BVH.cwbvh_indices[i]] = i;
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
                int first_triangle = (byte)BVH.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] & 0b11111;
                int NumBits = NumberOfSetBits((byte)BVH.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] >> 5);
                ForwardStack[i].Children[NodePair[i].InNodeOffset] = NumBits;
                ForwardStack[i].Leaf[NodePair[i].InNodeOffset] = (int)BVH.BVH8Nodes[NodePair[i].BVHNode].base_index_triangle + first_triangle + 1; 
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
        ConvertToSplitNodes();
    }
}

unsafe private void ConvertToSplitNodes() {
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

public int OffsetFirst;


public void RefitMesh(ref ComputeBuffer RealizedAggNodes) {
    int KernelRatio = 256;

    AABB OverAABB = new AABB();
    tempAABB = new AABB();
    OverAABB.init();
    for(int i = 0; i < SkinnedMeshes.Length; i++) {
        Vector3 Scale = new Vector3(1,1,1);//new Vector3(Mathf.Sqrt(this.transform.lossyScale.x), Mathf.Sqrt(this.transform.lossyScale.y), Mathf.Sqrt(this.transform.lossyScale.z));
        tempAABB.BBMax = SkinnedMeshes[i].bounds.center + Vector3.Scale(SkinnedMeshes[i].bounds.size, Scale) / 2.0f;
        tempAABB.BBMin = SkinnedMeshes[i].bounds.center - Vector3.Scale(SkinnedMeshes[i].bounds.size, Scale) / 2.0f;
        OverAABB.Extend(ref tempAABB);
    }

    aabb = OverAABB;
    if(!HasStarted) {
        UnityEngine.Profiling.Profiler.BeginSample("ReMesh Init");
        VertexBuffers = new GraphicsBuffer[SkinnedMeshes.Length];
        IndexBuffers = new GraphicsBuffer[SkinnedMeshes.Length];
        NodeBuffer = new ComputeBuffer(NodePair.Count, 48);
        NodeBuffer.SetData(NodePair);
        AdvancedTriangleBuffer = new ComputeBuffer(TotalTriangles, 96);
        VertexBufferOut = new ComputeBuffer(TotalTriangles, 72);
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
        if(Hips == null && this.transform.GetChild(0).GetChild(0) != null) Hips = this.transform.GetChild(0).GetChild(0);
        int MaxLength = 0;
        for(int i = 0; i < LayerStack.Length; i++) {
            MaxLength = Mathf.Max(MaxLength, LayerStack[i].Slab.Count);
        }
        WorkingBuffer = new ComputeBuffer(MaxLength, 4);
        UnityEngine.Profiling.Profiler.EndSample();
    } else if(AllFull) {
        if(Hips == null) Hips = this.transform;
        UnityEngine.Profiling.Profiler.BeginSample("ReMesh Fill");
        MeshRefit.SetMatrix("Transform2", this.transform.localToWorldMatrix);  
        MeshRefit.SetMatrix("Transform3", this.transform.worldToLocalMatrix);      
        MeshRefit.SetVector("Scale", this.transform.lossyScale);
        MeshRefit.SetVector("OverallOffset", Hips.parent.localToWorldMatrix * Hips.localPosition);
        MeshRefit.SetVector("ArmetureOffset", this.transform.localToWorldMatrix * Hips.parent.localPosition);
        MeshRefit.SetBuffer(ConstructKernel, "VertexsOut", VertexBufferOut);
        MeshRefit.SetBuffer(RefitLayerKernel, "ReverseStack", StackBuffer);
        MeshRefit.SetBuffer(ConstructKernel, "AdvancedTriangles", AdvancedTriangleBuffer);
        MeshRefit.SetBuffer(ConstructKernel, "CudaTriArray", TriBuffer);
        MeshRefit.SetBuffer(ConstructKernel, "CWBVHIndices", CWBVHIndicesBuffer);
        MeshRefit.SetBuffer(ConstructKernel, "VertexsIn", VertexBufferOut);
        MeshRefit.SetBuffer(ConstructKernel, "AdvancedTriangles", AdvancedTriangleBuffer);
        MeshRefit.SetBuffer(NodeInitializerKernel, "AllNodes", NodeBuffer);
        MeshRefit.SetBuffer(RefitLayerKernel, "AllNodes", NodeBuffer);
        for(int i = 0; i < VertexBuffers.Length; i++) {
            VertexBuffers[i].Dispose();
            IndexBuffers[i].Dispose();
            SkinnedMeshes[i].vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            VertexBuffers[i] = SkinnedMeshes[i].GetVertexBuffer();
            SkinnedMeshes[i].sharedMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            IndexBuffers[i] = SkinnedMeshes[i].sharedMesh.GetIndexBuffer();
        }
        UnityEngine.Profiling.Profiler.EndSample();
        int CurVertOffset = 0;
        UnityEngine.Profiling.Profiler.BeginSample("ReMesh Aggregate");
        for(int i = 0; i < TotalObjects; i++) {
            var SkinnedRootBone = SkinnedMeshes[i].rootBone;
            int IndexCount = IndexCounts[i];
            MeshRefit.SetInt("VertOffset", CurVertOffset);
            MeshRefit.SetInt("gVertexCount", IndexCount);
            MeshRefit.SetMatrix("Transform", SkinnedRootBone.worldToLocalMatrix);
            MeshRefit.SetVector("ArmetureScale", SkinnedRootBone.parent.lossyScale);
            MeshRefit.SetVector("Offset", SkinnedRootBone.position - Hips.position);
            MeshRefit.SetBuffer(ConstructKernel, "bufVertices", VertexBuffers[i]);
            MeshRefit.SetBuffer(ConstructKernel, "bufIndexes", IndexBuffers[i]);
            MeshRefit.Dispatch(ConstructKernel, (int)Mathf.Ceil(IndexCount / (float)KernelRatio),1,1);
            CurVertOffset += IndexCount;
        }
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("ReMesh NodeInit");
        MeshRefit.SetInt("NodeCount", NodePair.Count);
        MeshRefit.Dispatch(NodeInitializerKernel, (int)Mathf.Ceil(NodePair.Count / (float)KernelRatio), 1, 1);
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("ReMesh Layer Solve");
        MeshRefit.SetBuffer(RefitLayerKernel, "AdvancedTriangles", AdvancedTriangleBuffer);
        for(int i = MaxRecur - 1; i >= 0; i--) {
            var NodeCount2 = LayerStack[i].Slab.Count;
            WorkingBuffer.SetData(LayerStack[i].Slab, 0, 0, NodeCount2);
            MeshRefit.SetInt("NodeCount", NodeCount2);
            MeshRefit.SetBuffer(RefitLayerKernel, "NodesToWork", WorkingBuffer);        
            MeshRefit.Dispatch(RefitLayerKernel, (int)Mathf.Ceil(WorkingBuffer.count / (float)KernelRatio), 1, 1);
        }
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("ReMesh Solve");
        MeshRefit.SetInt("NodeCount", NodePair.Count);
        MeshRefit.SetBuffer(NodeUpdateKernel, "AllNodes", NodeBuffer);
        MeshRefit.SetBuffer(NodeUpdateKernel, "BVHNodes", BVHDataBuffer);
        MeshRefit.SetBuffer(NodeUpdateKernel, "ToBVHIndex", ToBVHIndexBuffer);
        MeshRefit.Dispatch(NodeUpdateKernel, (int)Mathf.Ceil(NodePair.Count / (float)KernelRatio), 1, 1);
        UnityEngine.Profiling.Profiler.EndSample();

        UnityEngine.Profiling.Profiler.BeginSample("ReMesh Compress");
        MeshRefit.SetInt("NodeCount", BVH.BVH8Nodes.Length);
        MeshRefit.SetInt("NodeOffset", NodeOffset);
        MeshRefit.SetBuffer(NodeCompressKernel, "BVHNodes", BVHDataBuffer);
        MeshRefit.SetBuffer(NodeCompressKernel, "AggNodes", RealizedAggNodes);
        MeshRefit.Dispatch(NodeCompressKernel, (int)Mathf.Ceil(NodePair.Count / (float)KernelRatio), 1, 1);
        UnityEngine.Profiling.Profiler.EndSample();
    }  

    UnityEngine.Profiling.Profiler.BeginSample("ReMesh Buffer Refresh");
    if(!AllFull) {
        for(int i = 0; i < VertexBuffers.Length; i++) {
            SkinnedMeshes[i].vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            VertexBuffers[i] = SkinnedMeshes[i].GetVertexBuffer();
            SkinnedMeshes[i].sharedMesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            IndexBuffers[i] = SkinnedMeshes[i].sharedMesh.GetIndexBuffer();
        }
        if(!((new List<GraphicsBuffer>(VertexBuffers)).Contains(null)) && !((new List<GraphicsBuffer>(IndexBuffers)).Contains(null))) AllFull = true;
    } 
    UnityEngine.Profiling.Profiler.EndSample();
}


private float AreaOfTriangle(Vector3 pt1, Vector3 pt2, Vector3 pt3) {
    float a = Vector3.Distance(pt1, pt2);
    float b = Vector3.Distance(pt2, pt3);
    float c = Vector3.Distance(pt3, pt1);
    float s = (a + b + c) / 2.0f;
    return Mathf.Sqrt(s * (s-a) * (s-b) * (s-c));
}
private float luminance(float r, float g, float b) {return 0.299f * r + 0.587f * g + 0.114f * b;}


public async Task BuildTotal() {
    Matrix4x4 ParentMat = CachedTransforms[0].WTL.inverse;
    Matrix4x4 ParentMatInv = CachedTransforms[0].WTL;
    Vector3 V1, V2, V3, Norm1, Norm2, Norm3, Tan1, Tan2, Tan3;
    PrimitiveData TempPrim = new PrimitiveData();
    float TotalEnergy = 0.0f;
    Triangles = new AABB[(TransformIndexes[TransformIndexes.Count - 1].VertexStart + TransformIndexes[TransformIndexes.Count - 1].VertexCount) / 3];
    AggTriangles = new CudaTriangle[(TransformIndexes[TransformIndexes.Count - 1].VertexStart + TransformIndexes[TransformIndexes.Count - 1].VertexCount) / 3];
    CudaTriangle TempTri = new CudaTriangle();
    int IllumTriCount = 0;
    for(int i = 0; i < TotalObjects; i++) {//Transforming so all child objects are in the same object space
        Matrix4x4 ChildMat = CachedTransforms[i + 1].WTL.inverse;
        Matrix4x4 TransMat = ParentMatInv * ChildMat;
        Vector3 Ofst = CachedTransforms[i + 1].WTL * CachedTransforms[i + 1].Position;
        Vector3 Ofst2 = ParentMatInv * CachedTransforms[0].Position;
        int IndexOffset = TransformIndexes[i].IndexOffset;
        for(int i3 = TransformIndexes[i].VertexStart; i3 < TransformIndexes[i].VertexStart + TransformIndexes[i].VertexCount; i3 += 3) {//Transforming child meshes into the space of their parent
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
            TempPrim.V1 = V1;
            TempPrim.V2 = V2;
            TempPrim.V3 = V3;
            Norm1 = ChildMat * CurMeshData.Normals[Index1];
            Norm2 = ChildMat * CurMeshData.Normals[Index2];
            Norm3 = ChildMat * CurMeshData.Normals[Index3];

            Tan1 = ChildMat * (Vector3)CurMeshData.Tangents[Index1];
            Tan2 = ChildMat * (Vector3)CurMeshData.Tangents[Index2];
            Tan3 = ChildMat * (Vector3)CurMeshData.Tangents[Index3];

            TempPrim.Norm1 = ParentMatInv * Norm1;
            TempPrim.Norm2 = ParentMatInv * Norm2;
            TempPrim.Norm3 = ParentMatInv * Norm3;

            TempPrim.Tan1 = ParentMatInv * Tan1;
            TempPrim.Tan2 = ParentMatInv * Tan2;
            TempPrim.Tan3 = ParentMatInv * Tan3;
            
            TempPrim.tex1 = CurMeshData.UVs[Index1];
            TempPrim.tex2 = CurMeshData.UVs[Index2];
            TempPrim.tex3 = CurMeshData.UVs[Index3];

            TempPrim.MatDat = CurMeshData.MatDat[i3 / 3];

            TempPrim.Reconstruct(ParentScale);
            TempTri.pos0 = TempPrim.V1;

            TempTri.posedge1 = TempPrim.V2 - TempPrim.V1;
            TempTri.posedge2 = TempPrim.V3 - TempPrim.V1;

            TempTri.norm0 = TempPrim.Norm1;
            TempTri.normedge1 = TempPrim.Norm2 - TempPrim.Norm1;
            TempTri.normedge2 = TempPrim.Norm3 - TempPrim.Norm1;

            TempTri.tan0 = TempPrim.Tan1;
            TempTri.tanedge1 = TempPrim.Tan2 - TempPrim.Tan1;
            TempTri.tanedge2 = TempPrim.Tan3 - TempPrim.Tan1;

            TempTri.tex0 = TempPrim.tex1;
            TempTri.texedge1 = TempPrim.tex2;
            TempTri.texedge2 = TempPrim.tex3;

            TempTri.MatDat = (uint)TempPrim.MatDat;
                AggTriangles[i3 / 3] = TempTri;
                Triangles[i3 / 3] = TempPrim.aabb;
            if(_Materials[TempPrim.MatDat].emmissive > 0.0f) {
                V1 = TempPrim.V1;
                V2 = TempPrim.V2;
                V3 = TempPrim.V3;
                Vector3 Radiance = _Materials[TempPrim.MatDat].emmissive * _Materials[TempPrim.MatDat].BaseColor;
                float radiance = luminance(Radiance.x, Radiance.y, Radiance.z);
                float area = AreaOfTriangle(ParentMat * V1, ParentMat * V2, ParentMat * V3);
                float e = radiance * area;
                TotalEnergy += area;
                TotEnergy += area;

                LightTriangles.Add(new CudaLightTriangle() {
                    pos0 = V1,
                    posedge1 = (V2 - V1),
                    posedge2 = (V3 - V1),
                    Norm = ((TempPrim.Norm1 + TempPrim.Norm2 + TempPrim.Norm3) / 3.0f),
                    radiance = Radiance,
                    sumEnergy = TotalEnergy,
                    energy = area,
                    area = area
                    });
                IllumTriCount++;
            }
            ToIllumTriBuffer.Add(IllumTriCount);
        }  
    }
    LightTriangles.Sort((s1,s2) => s1.energy.CompareTo(s2.energy));
    TotalEnergy = 0.0f;
    int LightTriCount = LightTriangles.Count;
    CudaLightTriangle TempTri2;
    for(int i = 0; i < LightTriCount; i++) {
        TempTri2 = LightTriangles[i];
        TotalEnergy += TempTri2.energy;
        TempTri2.sumEnergy = TotalEnergy;
        LightTriangles[i] = TempTri2; 
    }
    LightCount = LightTriangles.Count;
    ConstructAABB();
    Construct();

    {//Compile Triangles
        CudaTriangle[] TempArray = new CudaTriangle[AggTriangles.Length];
        System.Array.Copy(AggTriangles, TempArray, TempArray.Length);
        CudaTriangle Temp;
        for(int i = 0; i < AggTriangles.Length; i++) {
            AggTriangles[i] = TempArray[BVH.cwbvh_indices[i]];
        }
        TempArray = null;
    }

    AggNodes = new BVHNode8DataCompressed[BVH.BVH8Nodes.Length];
    CommonFunctions.Aggregate(ref AggNodes, ref BVH.BVH8Nodes);
    MeshCountChanged = false;
    HasCompleted = true;
    NeedsToUpdate = false;
    Debug.Log(Name + " Has Completed Building with " + AggTriangles.Length + " triangles");
}


public void UpdateData() {
    AggIndexCount = BVH.cwbvhindex_count;
    AggBVHNodeCount = BVH.cwbvhnode_count;
    UpdateAABB(this.transform);
    SetUpBuffers();
}

public void UpdateAABB(Transform transform) {//Update the Transformed AABB by getting the new Max/Min of the untransformed AABB after transforming it
    Vector3 center = 0.5f * (aabb_untransformed.BBMin + aabb_untransformed.BBMax);
    Vector3 extent = 0.5f * (aabb_untransformed.BBMax - aabb_untransformed.BBMin);

    Vector3 new_center = CommonFunctions.transform_position (transform.worldToLocalMatrix.inverse, center);
    Vector3 new_extent = CommonFunctions.transform_direction(CommonFunctions.abs(transform.worldToLocalMatrix.inverse), extent);

    aabb.BBMin = new_center - new_extent;
    aabb.BBMax = new_center + new_extent;
}

private void ConstructAABB() {
    aabb_untransformed = new AABB();
    aabb_untransformed.init();
    for(int i = 0; i < Triangles.Length; i++) {
        aabb_untransformed.Extend(ref Triangles[i]);
    }
}

private void OnEnable() {
    HasStarted = false;
    if(gameObject.scene.isLoaded) {
        if(this.GetComponentInParent<InstancedManager>() != null) {
            this.GetComponentInParent<InstancedManager>().AddQue.Add(this);
            this.GetComponentInParent<InstancedManager>().ParentCountHasChanged = true;            
        } else {
            this.GetComponentInParent<AssetManager>().AddQue.Add(this);
            this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
        }
        HasCompleted = false;
    }
}

private void OnDisable() {
    HasStarted = false;
    if(gameObject.scene.isLoaded) {
        ClearAll();
        if(this.GetComponentInParent<InstancedManager>() != null) {
            this.GetComponentInParent<InstancedManager>().RemoveQue.Add(this);
            this.GetComponentInParent<InstancedManager>().ParentCountHasChanged = true;            
        } else {
            this.GetComponentInParent<AssetManager>().RemoveQue.Add(this);
            this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
        }
        HasCompleted = false;
    }
}




}
