using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;


[System.Serializable]
public class ParentObject : MonoBehaviour {

    public string Name;
    public Texture2D AlbedoAtlas;
    public Texture2D NormalAtlas;
    public Texture2D EmissiveAtlas;
    [HideInInspector] public RayTracingObject[] ChildObjects;
    public bool MeshCountChanged;
    [HideInInspector] public List<PrimitiveData> Triangles;
    [HideInInspector] public List<CudaTriangle> AggTriangles;
    [HideInInspector] public List<CudaLightTriangle> LightTriangles;
    [HideInInspector] public BVH8Builder BVH;
    [HideInInspector] public List<BVHNode8DataCompressed> AggNodes;
    public AABB aabb_untransformed;
    public AABB aabb;
    public int AggIndexCount;
    public int AggBVHNodeCount;
    public int StaticBVHOffset;
    public bool ObjectOrderHasChanged;
    public List<MaterialData> _Materials;
    public int MatOffset;
    public int InstanceID;
    [HideInInspector] public StorableTransform[] CachedTransforms;
    public MeshDat CurMeshData;
    public int TotalObjects;
    [HideInInspector] public List<MeshTransformVertexs> TransformIndexes;
    public bool HasCompleted;
    public float TotEnergy;
    public int LightCount;

    public bool HasAlbedoAtlas;
    public bool HasNormalAtlas;
    public bool HasEmissiveAtlas;

    [System.Serializable]
    public struct StorableTransform {
        public Matrix4x4 WTL;
        public Vector3 Position;
    }

    [System.Serializable]
    public struct MeshTransformVertexs {
        public int VertexStart;
        public int VertexCount;
    }

    [System.Serializable]
    public struct PerMatTextureData {
        public bool HasAlbedoMap;
        public bool HasNormalMap;
        public bool HasEmissiveMap;
        public RayTracingObject MaterialObject;
    }
    public List<PerMatTextureData> MatTexData;


    public void ClearAll() {
        Debug.Log("CLEAR");
        Triangles.Clear();
        Triangles.TrimExcess();
        AggTriangles.Clear();
        AggTriangles.TrimExcess();
        LightTriangles.Clear();
        LightTriangles.TrimExcess();
        BVH = null;
        AggNodes.Clear();
        AggNodes.TrimExcess();
        _Materials.Clear();
        _Materials.TrimExcess();
        CurMeshData.Clear();
        TransformIndexes = null;
        MeshCountChanged = true;
        HasCompleted = false;
        DestroyImmediate(AlbedoAtlas);
        DestroyImmediate(NormalAtlas);
        DestroyImmediate(EmissiveAtlas); 
    }


    public void init() {
        InstanceID = this.GetInstanceID();
        Name = this.name;
        TransformIndexes = new List<MeshTransformVertexs>();
        _Materials = new List<MaterialData>();
        Triangles = new List<PrimitiveData>();
        AggTriangles = new List<CudaTriangle>();
        LightTriangles = new List<CudaLightTriangle>();
        AggNodes = new List<BVHNode8DataCompressed>();
        MeshCountChanged = true;
        ObjectOrderHasChanged = false;
        HasCompleted = false;
    }

    public struct objtextureindices {
        public int textureindexstart;
        public int textureindexcount;
    }

    private void CreateAtlas() {//Creates texture atlas
        MatTexData = new List<PerMatTextureData>();
        _Materials.Clear();
        List<Texture2D> AlbedoTexs = new List<Texture2D>();
        List<Texture2D> NormalTexs = new List<Texture2D>();
        List<Texture2D> EmissiveTexs = new List<Texture2D>();
        AlbedoAtlas = null;
        NormalAtlas = null;
        EmissiveAtlas = null;
        Mesh mesh = new Mesh();
        PerMatTextureData CurrentTexDat = new PerMatTextureData();
        foreach(RayTracingObject obj in ChildObjects) {
            if(obj.GetComponent<MeshFilter>() != null) { 
                mesh = obj.GetComponent<MeshFilter>().sharedMesh;
            }else {
                mesh = obj.GetComponent<SkinnedMeshRenderer>().sharedMesh;
            }
            Material[] SharedMaterials = (obj.GetComponent<Renderer>() != null) ? obj.GetComponent<Renderer>().sharedMaterials : obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials;       
            int SharedMatLength = SharedMaterials.Length;
            for(int i = 0; i < SharedMatLength; ++i) {
                CurrentTexDat.MaterialObject = obj;
                if(SharedMaterials[Mathf.Min(i, SharedMatLength - 1)] == null) {
                    i--;
                    SharedMatLength--;
                }
                if(SharedMaterials[i].mainTexture != null) {
                    AlbedoTexs.Add((Texture2D)SharedMaterials[i].mainTexture);
                    CurrentTexDat.HasAlbedoMap = true;
                } else {
                    CurrentTexDat.HasAlbedoMap = false;
                }
                if(SharedMaterials[i].GetTexture("_BumpMap") != null) {
                    NormalTexs.Add((Texture2D)SharedMaterials[i].GetTexture("_BumpMap"));
                    CurrentTexDat.HasNormalMap = true;
                } else {
                    CurrentTexDat.HasNormalMap = false;
                }
                if(SharedMaterials[i].GetTexture("_EmissionMap") != null) {
                    EmissiveTexs.Add((Texture2D)SharedMaterials[i].GetTexture("_EmissionMap"));
                    CurrentTexDat.HasEmissiveMap = true;
                } else {
                    CurrentTexDat.HasEmissiveMap = false;
                }
                MatTexData.Add(CurrentTexDat);


            }
        }
        int AlbedoCount = 0;
        int NormalCount = 0;
        int EmissiveCount = 0;
        Rect[] AlbedoRects;
        Rect[] NormalRects;
        Rect[] EmmissiveRects;
        if(AlbedoTexs.Count != 0) {
            AlbedoAtlas = new Texture2D(8192, 8192);
            AlbedoRects = AlbedoAtlas.PackTextures(AlbedoTexs.ToArray(), 2, 8192);
            HasAlbedoAtlas = true;
        } else {
            HasAlbedoAtlas = false;
            AlbedoAtlas = new Texture2D(1,1);
            AlbedoRects = new Rect[0];
        }
        if(NormalTexs.Count != 0) {
            NormalAtlas = new Texture2D(8192, 8192);
            NormalRects = NormalAtlas.PackTextures(NormalTexs.ToArray(), 0, 8192);
            HasNormalAtlas = true;
        } else {
            HasNormalAtlas = false;
            NormalAtlas = new Texture2D(1,1);
            NormalRects = new Rect[0];
        }
        if(EmissiveTexs.Count != 0) {
            EmissiveAtlas = new Texture2D(2048, 2048);
            EmmissiveRects = EmissiveAtlas.PackTextures(EmissiveTexs.ToArray(), 2, 2048);
            HasEmissiveAtlas = true;
        } else {
            HasEmissiveAtlas = false;
            EmissiveAtlas = new Texture2D(1,1);
            EmmissiveRects = new Rect[0];
        }
        AlbedoTexs.Clear();
        AlbedoTexs.TrimExcess();
        NormalTexs.Clear();
        NormalTexs.TrimExcess();
        EmissiveTexs.Clear();
        EmissiveTexs.TrimExcess();
        RayTracingObject PreviousObject = MatTexData[0].MaterialObject;
        int CurrentObjectOffset = -1;
        int CurrentMat = 0;
        Vector4 AlbedoTex = new Vector4(0,0,0,0);
        Vector4 NormalTex = new Vector4(0,0,0,0);
        Vector4 EmissiveTex = new Vector4(0,0,0,0);
        int CurMat = 0;
        foreach(PerMatTextureData Obj in MatTexData) {
            if(PreviousObject.Equals(Obj.MaterialObject)) {
                CurrentObjectOffset++;
            } else {
                CurrentObjectOffset = 0;
            }
            if(Obj.HasAlbedoMap) {
                AlbedoTex = new Vector4(AlbedoRects[AlbedoCount].xMax, AlbedoRects[AlbedoCount].yMax, AlbedoRects[AlbedoCount].xMin, AlbedoRects[AlbedoCount].yMin);
                AlbedoCount++;
            }
            if(Obj.HasNormalMap) {
                NormalTex = new Vector4(NormalRects[NormalCount].xMax, NormalRects[NormalCount].yMax, NormalRects[NormalCount].xMin, NormalRects[NormalCount].yMin);
                NormalCount++;
            }
            if(Obj.HasEmissiveMap) {
                EmissiveTex = new Vector4(EmmissiveRects[EmissiveCount].xMax, EmmissiveRects[EmissiveCount].yMax, EmmissiveRects[EmissiveCount].xMin, EmmissiveRects[EmissiveCount].yMin);
                EmissiveCount++;
            }
            _Materials.Add(new MaterialData() {
                AlbedoTex = AlbedoTex,
                NormalTex = NormalTex,
                EmissiveTex = EmissiveTex,
                HasAlbedoTex = (Obj.HasAlbedoMap) ? 1 : 0,
                HasNormalTex = (Obj.HasNormalMap) ? 1 : 0,
                HasEmissiveTex = (Obj.HasEmissiveMap) ? 1 : 0,
                BaseColor = Obj.MaterialObject.BaseColor[CurrentObjectOffset],
                emmissive = Obj.MaterialObject.emmission[CurrentObjectOffset],
                Roughness = Obj.MaterialObject.Roughness[CurrentObjectOffset],
                MatType = Obj.MaterialObject.MatType[CurrentObjectOffset],
                eta = Obj.MaterialObject.eta[CurrentObjectOffset]
            });
            Obj.MaterialObject.MaterialIndex[CurrentObjectOffset] = CurMat;
            Obj.MaterialObject.LocalMaterialIndex[CurrentObjectOffset] = CurMat;
            PreviousObject = Obj.MaterialObject;
            CurMat++;
        }

    }


    public void LoadData() {
        TotEnergy = 0;
        LightCount = 0;
        init();
        Triangles.Clear();
        CurMeshData = new MeshDat();
        CurMeshData.init();
        List<RayTracingObject> TempObjects = new List<RayTracingObject>();
        List<Transform> TempObjectTransforms = new List<Transform>();
        TempObjectTransforms.Add(this.transform);
        for(int i = 0; i < this.transform.childCount; i++) {
            if(this.transform.GetChild(i).gameObject.GetComponent<RayTracingObject>() != null && this.transform.GetChild(i).gameObject.activeInHierarchy) {
                TempObjectTransforms.Add(this.transform.GetChild(i));
                TempObjects.Add(this.transform.GetChild(i).gameObject.GetComponent<RayTracingObject>());
            }
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
                if(CurrentObject.GetComponent<MeshFilter>() != null) { 
                    mesh = CurrentObject.GetComponent<MeshFilter>().sharedMesh;
                }else {
                    CurrentObject.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh, true);
                }
                submeshcount = mesh.subMeshCount;
                
                List<Vector4> Tans = new List<Vector4>();
                mesh.GetTangents(Tans);
                for(int i2 = 0; i2 < Tans.Count; i2++) {
                    CurMeshData.Tangents.Add(new Vector3(Tans[i2].x, Tans[i2].y, Tans[i2].z));
                }
            
                List<Vector3> Norms = new List<Vector3>();
                mesh.GetNormals(Norms);
                CurMeshData.Normals.AddRange(Norms);
                int IndexOffset = CurMeshData.Verticies.Count;
                CurMeshData.Verticies.AddRange(mesh.vertices);
                int MeshUvLength = mesh.uv.Length;
                if(MeshUvLength != 0) {
                    CurMeshData.UVs.AddRange(mesh.uv);
                } else {
                    CurMeshData.SetUvZero(MeshUvLength);
                }
                int PreIndexLength = CurMeshData.Indices.Count;
                for(int i2 = 0; i2 < submeshcount; ++i2) {//Add together all the submeshes in the mesh to consider it as one object
                    int PrevLength = CurMeshData.Indices.Count;
                    List<int> NewIndexes = new List<int>(mesh.GetIndices(i2));
                    int NewIndexLength = NewIndexes.Count;
                    for(int i3 = 0; i3 < NewIndexLength; i3++) {
                        CurMeshData.Indices.Add(NewIndexes[i3] + IndexOffset);    
                    }
                    int IndiceLength = (CurMeshData.Indices.Count - PrevLength) / 3;
                    MatIndex = i2 + RepCount;
                    for(int i3 = 0; i3 < IndiceLength; ++i3) {
                        CurMeshData.MatDat.Add(MatIndex);
                    }
                }
                TransformIndexes.Add(new MeshTransformVertexs() {
                    VertexStart = PreIndexLength,
                    VertexCount = CurMeshData.Indices.Count - PreIndexLength
                });
                RepCount += submeshcount;
            }
  }



    public void Construct() {
        BVH2Builder BVH2 = new BVH2Builder(Triangles);//Binary BVH Builder, and also the component that takes the longest to build
        this.BVH = new BVH8Builder(ref BVH2);
        BVH2 = null;
        BVH.BVH8Nodes.RemoveRange(BVH.cwbvhnode_count, BVH.BVH8Nodes.Count - BVH.cwbvhnode_count);
        BVH.BVH8Nodes.Capacity = BVH.BVH8Nodes.Count;
    }


  private float AreaOfTriangle(Vector3 pt1, Vector3 pt2, Vector3 pt3) {
    float a = Vector3.Distance(pt1, pt2);
    float b = Vector3.Distance(pt2, pt3);
    float c = Vector3.Distance(pt3, pt1);
    float s = (a + b + c) / 2.0f;
    return Mathf.Sqrt(s * (s-a) * (s-b) * (s-c));
  }


  public async Task BuildTotal() {
        Matrix4x4 ParentMat = CachedTransforms[0].WTL.inverse;
        Matrix4x4 ParentMatInv = CachedTransforms[0].WTL;
        Vector3 V1, V2, V3, Norm1, Norm2, Norm3, Tan1, Tan2, Tan3;
        PrimitiveData TempPrim = new PrimitiveData();
        float TotalEnergy = 0.0f;
    for(int i = 0; i < TotalObjects; i++) {

        Matrix4x4 ChildMat = CachedTransforms[i + 1].WTL.inverse;
        Matrix4x4 TransMat = ParentMatInv * ChildMat;
        Vector3 Ofst = CachedTransforms[i + 1].WTL * CachedTransforms[i + 1].Position;
        Vector3 Ofst2 = ParentMatInv * CachedTransforms[0].Position;
            for(int i3 = TransformIndexes[i].VertexStart; i3 < TransformIndexes[i].VertexStart + TransformIndexes[i].VertexCount; i3 += 3) {//Transforming child meshes into the space of their parent
                int Index1 = CurMeshData.Indices[i3];
                int Index2 = CurMeshData.Indices[i3 + 2];
                int Index3 = CurMeshData.Indices[i3 + 1];
                V1 = CurMeshData.Verticies[Index1] + Ofst;
                V2 = CurMeshData.Verticies[Index2] + Ofst;
                V3 = CurMeshData.Verticies[Index3] + Ofst;
                V1 = TransMat * V1;
                V2 = TransMat * V2;
                V3 = TransMat * V3;
                TempPrim.V1 = V1 - Ofst2;
                TempPrim.V2 = V2 - Ofst2;
                TempPrim.V3 = V3 - Ofst2;
                Norm1 = ChildMat * CurMeshData.Normals[Index1];
                Norm2 = ChildMat * CurMeshData.Normals[Index2];
                Norm3 = ChildMat * CurMeshData.Normals[Index3];

                Tan1 = ChildMat * CurMeshData.Tangents[Index1];
                Tan2 = ChildMat * CurMeshData.Tangents[Index2];
                Tan3 = ChildMat * CurMeshData.Tangents[Index3];


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

                TempPrim.Reconstruct();
                Triangles.Add(TempPrim);
                if(_Materials[TempPrim.MatDat].emmissive > 0.0f) {
                    V1 = TempPrim.V1;
                    V2 = TempPrim.V2;
                    V3 = TempPrim.V3;
                    float radiance = _Materials[TempPrim.MatDat].emmissive * _Materials[TempPrim.MatDat].BaseColor.x + 
                                    _Materials[TempPrim.MatDat].emmissive * _Materials[TempPrim.MatDat].BaseColor.y +
                                    _Materials[TempPrim.MatDat].emmissive * _Materials[TempPrim.MatDat].BaseColor.z;
                    float area = AreaOfTriangle(ParentMat * V1, ParentMat * V2, ParentMat * V3);
                    float e = radiance * area;
                    TotalEnergy += e;
                    TotEnergy += e;

                    LightTriangles.Add(new CudaLightTriangle() {
                        pos0 = V1,
                        posedge1 = V2 - V1,
                        posedge2 = V3 - V1,
                        Norm = (TempPrim.Norm1 + TempPrim.Norm2 + TempPrim.Norm3) / 3.0f,
                        radiance = _Materials[TempPrim.MatDat].emmissive * _Materials[TempPrim.MatDat].BaseColor,
                        sumEnergy = TotalEnergy,
                        energy = e,
                        area = area
                        });
                }
            }
        
    }
    LightCount = LightTriangles.Count;
    ConstructAABB();
    Construct();
    CompileTriangles();
    Aggregate();
    HasCompleted = true;
    Debug.Log(Name + " Has Completed Building with " + AggTriangles.Count + " triangles");
  }


    public void UpdateData(ref int a, ref int b, ref int ReturnOffset, ref int MaterialOffset) {
            MaterialOffset += MatOffset;
            MatOffset = _Materials.Count;
            b += BVH.cwbvhnode_count;
            a += BVH.cwbvhindex_count;
            AggIndexCount = BVH.cwbvhindex_count;
            AggBVHNodeCount = BVH.cwbvhnode_count;
            ReturnOffset += StaticBVHOffset;
            UpdateAABB();
    }

    public void CompileTriangles() {
        AggTriangles.Clear();
        CudaTriangle TempTri = new CudaTriangle();
                int TriCount = Triangles.Count;
                PrimitiveData triangle;
                for(int i2 = 0; i2 < TriCount; ++i2) {//This constructs the list of triangles that actually get sent to the GPU
                    triangle = Triangles[BVH.cwbvh_indices[i2]];
                    TempTri.pos0 = triangle.V1;

                    TempTri.posedge1 = triangle.V2 - triangle.V1;
                    TempTri.posedge2 = triangle.V3 - triangle.V1;

                    TempTri.norm0 = triangle.Norm1;
                    TempTri.normedge1 = triangle.Norm2 - triangle.Norm1;
                    TempTri.normedge2 = triangle.Norm3 - triangle.Norm1;

                    TempTri.tan0 = triangle.Tan1;
                    TempTri.tanedge1 = triangle.Tan2 - triangle.Tan1;
                    TempTri.tanedge2 = triangle.Tan3 - triangle.Tan1;

                    TempTri.tex0 = triangle.tex1;
                    TempTri.texedge1 = triangle.tex2;
                    TempTri.texedge2 = triangle.tex3;

                    TempTri.MatDat = (uint)triangle.MatDat;
                    AggTriangles.Add(TempTri);
                 }
           
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

    public void UpdateAABB() {//Update the Transformed AABB by getting the new Max/Min of the untransformed AABB after transforming it
        Vector3 center = 0.5f * (aabb_untransformed.BBMin + aabb_untransformed.BBMax);
        Vector3 extent = 0.5f * (aabb_untransformed.BBMax - aabb_untransformed.BBMin);

        Vector3 new_center = transform_position (this.transform.worldToLocalMatrix.inverse, center);
        Vector3 new_extent = transform_direction(abs(this.transform.worldToLocalMatrix.inverse), extent);

        aabb.BBMin = new_center - new_extent;
        aabb.BBMax = new_center + new_extent;
    }

    private void ConstructAABB() {
        aabb_untransformed = new AABB();
        aabb_untransformed.init();
        for(int i = 0; i < Triangles.Count; i++) {
            aabb_untransformed.Extend(Triangles[i].BBMax, Triangles[i].BBMin);
        }
    }



public void Aggregate() {//Compress the CWBVH
        AggNodes = new List<BVHNode8DataCompressed>();
        for(int i = 0; i < BVH.BVH8Nodes.Count; ++i) {
            BVHNode8Data TempNode = BVH.BVH8Nodes[i];
            byte[] tempbyte = new byte[4];
            tempbyte[0] = TempNode.e[0];
            tempbyte[1] = TempNode.e[1];
            tempbyte[2] = TempNode.e[2];
            tempbyte[3] = TempNode.imask;
            byte[] metafirst = new byte[4];
            metafirst[0] = TempNode.meta[0];
            metafirst[1] = TempNode.meta[1];
            metafirst[2] = TempNode.meta[2];
            metafirst[3] = TempNode.meta[3];
            byte[] metasecond = new byte[4];
            metasecond[0] = TempNode.meta[4];
            metasecond[1] = TempNode.meta[5];
            metasecond[2] = TempNode.meta[6];
            metasecond[3] = TempNode.meta[7];
            byte[] minxfirst = new byte[4];
            minxfirst[0] = TempNode.quantized_min_x[0];
            minxfirst[1] = TempNode.quantized_min_x[1];
            minxfirst[2] = TempNode.quantized_min_x[2];
            minxfirst[3] = TempNode.quantized_min_x[3];
            byte[] minxsecond = new byte[4];
            minxsecond[0] = TempNode.quantized_min_x[4];
            minxsecond[1] = TempNode.quantized_min_x[5];
            minxsecond[2] = TempNode.quantized_min_x[6];
            minxsecond[3] = TempNode.quantized_min_x[7];
            byte[] maxxfirst = new byte[4];
            maxxfirst[0] = TempNode.quantized_max_x[0];
            maxxfirst[1] = TempNode.quantized_max_x[1];
            maxxfirst[2] = TempNode.quantized_max_x[2];
            maxxfirst[3] = TempNode.quantized_max_x[3];
            byte[] maxxsecond = new byte[4];
            maxxsecond[0] = TempNode.quantized_max_x[4];
            maxxsecond[1] = TempNode.quantized_max_x[5];
            maxxsecond[2] = TempNode.quantized_max_x[6];
            maxxsecond[3] = TempNode.quantized_max_x[7];

            byte[] minyfirst = new byte[4];
            minyfirst[0] = TempNode.quantized_min_y[0];
            minyfirst[1] = TempNode.quantized_min_y[1];
            minyfirst[2] = TempNode.quantized_min_y[2];
            minyfirst[3] = TempNode.quantized_min_y[3];
            byte[] minysecond = new byte[4];
            minysecond[0] = TempNode.quantized_min_y[4];
            minysecond[1] = TempNode.quantized_min_y[5];
            minysecond[2] = TempNode.quantized_min_y[6];
            minysecond[3] = TempNode.quantized_min_y[7];
            byte[] maxyfirst = new byte[4];
            maxyfirst[0] = TempNode.quantized_max_y[0];
            maxyfirst[1] = TempNode.quantized_max_y[1];
            maxyfirst[2] = TempNode.quantized_max_y[2];
            maxyfirst[3] = TempNode.quantized_max_y[3];
            byte[] maxysecond = new byte[4];
            maxysecond[0] = TempNode.quantized_max_y[4];
            maxysecond[1] = TempNode.quantized_max_y[5];
            maxysecond[2] = TempNode.quantized_max_y[6];
            maxysecond[3] = TempNode.quantized_max_y[7];

            byte[] minzfirst = new byte[4];
            minzfirst[0] = TempNode.quantized_min_z[0];
            minzfirst[1] = TempNode.quantized_min_z[1];
            minzfirst[2] = TempNode.quantized_min_z[2];
            minzfirst[3] = TempNode.quantized_min_z[3];
            byte[] minzsecond = new byte[4];
            minzsecond[0] = TempNode.quantized_min_z[4];
            minzsecond[1] = TempNode.quantized_min_z[5];
            minzsecond[2] = TempNode.quantized_min_z[6];
            minzsecond[3] = TempNode.quantized_min_z[7];
            byte[] maxzfirst = new byte[4];
            maxzfirst[0] = TempNode.quantized_max_z[0];
            maxzfirst[1] = TempNode.quantized_max_z[1];
            maxzfirst[2] = TempNode.quantized_max_z[2];
            maxzfirst[3] = TempNode.quantized_max_z[3];
            byte[] maxzsecond = new byte[4];
            maxzsecond[0] = TempNode.quantized_max_z[4];
            maxzsecond[1] = TempNode.quantized_max_z[5];
            maxzsecond[2] = TempNode.quantized_max_z[6];
            maxzsecond[3] = TempNode.quantized_max_z[7];
            AggNodes.Add(new BVHNode8DataCompressed() {
                node_0xyz = new Vector3(TempNode.p.x, TempNode.p.y, TempNode.p.z),
                node_0w = System.BitConverter.ToUInt32(tempbyte, 0),
                node_1x = TempNode.base_index_child,
                node_1y = TempNode.base_index_triangle,
                node_1z = System.BitConverter.ToUInt32(metafirst, 0),
                node_1w = System.BitConverter.ToUInt32(metasecond, 0),
                node_2x = System.BitConverter.ToUInt32(minxfirst, 0),
                node_2y = System.BitConverter.ToUInt32(minxsecond, 0),
                node_2z = System.BitConverter.ToUInt32(maxxfirst, 0),
                node_2w = System.BitConverter.ToUInt32(maxxsecond, 0),
                node_3x = System.BitConverter.ToUInt32(minyfirst, 0),
                node_3y = System.BitConverter.ToUInt32(minysecond, 0),
                node_3z = System.BitConverter.ToUInt32(maxyfirst, 0),
                node_3w = System.BitConverter.ToUInt32(maxysecond, 0),
                node_4x = System.BitConverter.ToUInt32(minzfirst, 0),
                node_4y = System.BitConverter.ToUInt32(minzsecond, 0),
                node_4z = System.BitConverter.ToUInt32(maxzfirst, 0),
                node_4w = System.BitConverter.ToUInt32(maxzsecond, 0)

            });
        }
        MeshCountChanged = false;
    }


    private void OnEnable() {
        if(gameObject.scene.isLoaded) {
            this.GetComponentInParent<AssetManager>().AddQue.Add(this);
            this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
            HasCompleted = false;
        }
    }

    private void OnDisable() {
        if(gameObject.scene.isLoaded) {
            this.GetComponentInParent<AssetManager>().RemoveQue.Add(this);
            this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
            HasCompleted = false;
        }
    }

}
