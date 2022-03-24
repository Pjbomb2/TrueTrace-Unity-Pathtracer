using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;

public class AssetManager : MonoBehaviour {
    public Texture2D atlas;
	[HideInInspector]
    public Mesh_Data[] mesh_datas;
    [HideInInspector]
	public List<MaterialData> _Materials;
    [HideInInspector]
    public List<CudaTriangle> aggregated_triangles;
    [HideInInspector]
    public RayTracingObject[] RelevantMaterials;   
    [HideInInspector]
    public List<BVHNode8DataCompressed> BVH8Aggregated;
    [HideInInspector]
    public List<MyMeshDataCompacted> MyMeshesCompacted;
    [HideInInspector]
    public List<ProgReportData> ProgIds;
    [HideInInspector]
    public List<RayTracingObject>[] NodeGroups;
    [HideInInspector]
    public List<CudaLightTriangle> AggLightTriangles;
    [HideInInspector]
    public List<int> LightTriangleIndices;
    [HideInInspector]
    public AABB[] MeshAABBs;
    [HideInInspector]
    public int TLASSpace;
    
    private List<Task> tasks;
    private List<BVHNode8DataCompressed> BVH8StaticAggregated;


    public struct objtextureindices {
        public int textureindexstart;
        public int textureindexcount;
    }

    private void CreateAtlas() {//Creates texture atlas
        _Materials = new List<MaterialData>();
        List<objtextureindices> ObjectTextures = new List<objtextureindices>();
        List<Texture2D> TempTexs = new List<Texture2D>();
        List<int> texindex = new List<int>();
        int texcount = 0;
        foreach(RayTracingObject obj in RayTracingMaster._rayTracingObjects) {
            int texturestart = texindex.Count;
            Mesh mesh = new Mesh();
            if(obj.GetComponent<MeshFilter>() != null) { 
                mesh = obj.GetComponent<MeshFilter>().sharedMesh;
            }else {
                obj.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh);
            }
            for(int i = 0; i < mesh.subMeshCount; ++i) {
                if((obj.GetComponent<Renderer>() != null) ? obj.GetComponent<Renderer>().sharedMaterials[i].mainTexture : obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials[i].mainTexture != null) {
                    texindex.Add(i);
                    TempTexs.Add((Texture2D)obj.GetComponent<Renderer>().sharedMaterials[i].mainTexture);
                    texcount += 1;
                }
            }
            ObjectTextures.Add(new objtextureindices() {
                textureindexcount = texindex.Count - texturestart,
                textureindexstart = texturestart
            });
        }
        atlas = new Texture2D(8192, 8192);
        Rect[] rects = atlas.PackTextures(TempTexs.ToArray(), 2, 8192);
        TempTexs.Clear();
        int curobj2 = 0;
        texcount = 0;
        int curMat = 0;
        foreach (RayTracingObject obj in RayTracingMaster._rayTracingObjects) {
            Mesh mesh = new Mesh();
            if(obj.GetComponent<MeshFilter>() != null) { 
                mesh = obj.GetComponent<MeshFilter>().sharedMesh;
            }else {
                obj.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh);
            }
            int SubMeshCount = mesh.subMeshCount; 
            for(int CurSubMesh = 0; CurSubMesh < SubMeshCount; ++CurSubMesh) {
                int contains = 0;
                int num = 0;
                
                for(int CurTexIndex = ObjectTextures[curobj2].textureindexstart; CurTexIndex < ObjectTextures[curobj2].textureindexcount + ObjectTextures[curobj2].textureindexstart; ++CurTexIndex) {
                    if(texindex[CurTexIndex] == CurSubMesh) {
                        contains = 1;
                        num = CurTexIndex;
                        break;
                    }
                }
                RayTracingObject TargetObject = obj;
                TargetObject.MaterialIndex[CurSubMesh] = curMat;
                curMat++;
                int MatType = TargetObject.MatType[CurSubMesh];
                if(MatType == 0 && TargetObject.eta[CurSubMesh].x != 0.0) {
                    MatType = 1;
                    TargetObject.MatType[CurSubMesh] = 1;
                }
                _Materials.Add(new MaterialData() {
                    BaseColor = TargetObject.BaseColor[CurSubMesh],
                    TexMax = new Vector2((contains == 1) ? rects[num].xMax : 0, (contains == 1) ? rects[num].yMax : 0),
                    TexMin = new Vector2((contains == 1) ? rects[num].xMin : 0, (contains == 1) ? rects[num].yMin : 0),
                    emmissive = TargetObject.emmission[CurSubMesh],
                    Roughness = TargetObject.Roughness[CurSubMesh],
                    HasTextures = (contains == 1) ? 1 : 0,
                    MatType = MatType,
                    eta = TargetObject.eta[CurSubMesh]
                });
                if(contains == 1)
                    texcount += 1;
            }
            curobj2++;
        }
    }

    private void writetime(string section, System.TimeSpan ts) {//Allows me to print how long something took
        // Format and display the TimeSpan value.
        string elapsedTime = System.String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
            ts.Hours, ts.Minutes, ts.Seconds,
            ts.Milliseconds / 10);
        Debug.Log(section + elapsedTime);
    }

    private void init() {
        ProgIds = new List<ProgReportData>();
        MyMeshesCompacted = new List<MyMeshDataCompacted>();
        RelevantMaterials = new RayTracingObject[RayTracingMaster._rayTracingObjects.Count];
        BVH8StaticAggregated = new List<BVHNode8DataCompressed>();
        aggregated_triangles = new List<CudaTriangle>();
        BVH8Aggregated = new List<BVHNode8DataCompressed>();
        AggLightTriangles = new List<CudaLightTriangle>();
        LightTriangleIndices = new List<int>();
    }

    System.TimeSpan ts;
    System.Diagnostics.Stopwatch GlobalStopWatch = new System.Diagnostics.Stopwatch();

    public async Task RunThreads() {
        tasks = new List<Task>();
        int MeshLength = mesh_datas.Length;
        for(int i = 0; i < MeshLength; i++) {
            var currentRep = i;
            Task t1 = Task.Run(() => mesh_datas[currentRep].Construct());
            tasks.Add(t1);
        }
        Task.WaitAll(tasks.ToArray());
        int aggregated_bvh_node_count = 2 * MeshLength;
        int aggregated_index_count = 0;
        CudaTriangle TempTri = new CudaTriangle();
            for(int i = 0; i < MeshLength; i++) {
                BVHNode8Data TempNode2;
                int CWBVHNodeCount = mesh_datas[i].BVH.cwbvhnode_count;
                for(int n = 0; n < CWBVHNodeCount; n++) {
                    TempNode2 = mesh_datas[i].BVH.BVH8Nodes[n];
                    TempNode2.base_index_triangle += (uint)aggregated_index_count;
                    TempNode2.base_index_child += (uint)aggregated_bvh_node_count;
                    mesh_datas[i].BVH.BVH8Nodes[n] = TempNode2;
                }
                mesh_datas[i].Aggregate();
                mesh_datas[i].BVH.BVH8Nodes.Clear();
                mesh_datas[i].BVH.BVH8Nodes.Capacity = 0;
                BVH8StaticAggregated.AddRange(mesh_datas[i].Aggregated_BVH_Nodes);
                int TriCount = mesh_datas[i].triangles.Count;
                PrimitiveData triangle;
                for(int i2 = 0; i2 < TriCount; ++i2) {//This constructs the list of triangles that actually get sent to the GPU
                    triangle = mesh_datas[i].triangles[mesh_datas[i].BVH.cwbvh_indices[i2]];
                    TempTri.pos0 = triangle.V1;

                    TempTri.posedge1 = triangle.V2 - triangle.V1;
                    TempTri.posedge2 = triangle.V3 - triangle.V1;

                    TempTri.norm0 = triangle.Norm1;
                    TempTri.normedge1 = triangle.Norm2 - triangle.Norm1;
                    TempTri.normedge2 = triangle.Norm3 - triangle.Norm1;

                    TempTri.tex0 = triangle.tex1;
                    TempTri.texedge1 = triangle.tex2;
                    TempTri.texedge2 = triangle.tex3;

                    TempTri.MatDat = (uint)triangle.MatDat;
                    aggregated_triangles.Add(TempTri);
                 }

                aggregated_bvh_node_count += mesh_datas[i].BVH.cwbvhnode_count;
                aggregated_index_count += mesh_datas[i].BVH.cwbvhindex_count;
            }
           
            Task.Run(() => UpdateTLASAsync());//Async functions dont like to call non async functions, so it needs its own TLAS builder seperate from the one the RayTracingMaster uses
    }

    private async void UpdateTLASAsync() { 
        int aggregated_bvh_node_count = 2 * mesh_datas.Length;
        MyMeshesCompacted.Clear();
        int MeshDataCount = mesh_datas.Length;
        MeshAABBs = new AABB[MeshDataCount];
        LightTriangleIndices = new List<int>();
        for(int i = 0; i < MeshDataCount; i++) {
             MyMeshesCompacted.Add(new MyMeshDataCompacted() {
                mesh_data_bvh_offsets = aggregated_bvh_node_count,
                Transform =  mesh_datas[i].CachedTransform.inverse,
                Inverse = mesh_datas[i].CachedTransform,
                Center = mesh_datas[i].CachedPosition
              });
            MeshAABBs[i] = mesh_datas[i].aabb;
            aggregated_bvh_node_count += mesh_datas[i].BVH.cwbvhnode_count;
            AggLightTriangles.AddRange(mesh_datas[i].LightTriangles);
        }
        if(AggLightTriangles.Count == 0) {AggLightTriangles.Add(new CudaLightTriangle() {});}//Fallback so we still initialize the list for the compute shader
        AggLightTriangles.Sort((s1, s2) => s1.sumEnergy.CompareTo(s2.sumEnergy));
        BVH8Builder TLASBVH8 = new BVH8Builder(new BVH2Builder(MeshAABBs), ref MyMeshesCompacted);

       for(int i = 0; i < TLASBVH8.cwbvh_indices.Count; i++) {
            LightTriangleIndices.Add(i);
        }
        LightTriangleIndices.Sort((s1, s2) => TLASBVH8.cwbvh_indices[s1].CompareTo(TLASBVH8.cwbvh_indices[s2]));

        TLASSpace = TLASBVH8.BVH8Nodes.Count;
        for(int i = 0; i < TLASSpace; i++) {
            BVH8Aggregated.Add(new BVHNode8DataCompressed() {});
        }
        Aggregate(ref TLASBVH8);
        BVH8Aggregated.AddRange(BVH8StaticAggregated);
        BVH8StaticAggregated = null;
        GlobalStopWatch.Stop(); ts = GlobalStopWatch.Elapsed; writetime("Total Construction Time: ", ts);//Tells me how long total construction has taken
    }



  private float AreaOfTriangle(Vector3 pt1, Vector3 pt2, Vector3 pt3) {
    float a = Vector3.Distance(pt1, pt2);
    float b = Vector3.Distance(pt2, pt3);
    float c = Vector3.Distance(pt3, pt1);
    float s = (a + b + c) / 2.0f;
    return Mathf.Sqrt(s * (s-a) * (s-b) * (s-c));
  }

    public void BuildCombined() {//Replaced other build function with this, as this can do the job of both and do it better
        GlobalStopWatch = System.Diagnostics.Stopwatch.StartNew();
        List<int> AloneNodes = new List<int>();
        int TotalObjectGroups = 0;
        init();
        int MatRep = 0;
        for(int i = 0; i < RayTracingMaster._rayTracingObjects.Count; i++) {
            RelevantMaterials[MatRep] = RayTracingMaster._rayTracingObjects[i];
            MatRep++;
            if(RayTracingMaster._rayTracingObjects[i].ObjectGroup != -1)
                TotalObjectGroups = Mathf.Max(TotalObjectGroups, RayTracingMaster._rayTracingObjects[i].ObjectGroup + 1);
            else 
                AloneNodes.Add(0);
        }
        CreateAtlas();
        int PreAloneNodeLength = TotalObjectGroups;
        TotalObjectGroups += AloneNodes.Count;
        NodeGroups = new List<RayTracingObject>[TotalObjectGroups];
        for(int i = 0; i < TotalObjectGroups; i++) {
            NodeGroups[i] = new List<RayTracingObject>();
        }
        int AloneCounter = 0;
        foreach(RayTracingObject obj in RayTracingMaster._rayTracingObjects) {
            int ObjectGroup = obj.ObjectGroup;
            if(ObjectGroup == -1) {
                NodeGroups[AloneCounter + PreAloneNodeLength].Add(obj);
                AloneCounter++;
            } else {
                if(obj.IsParent) {
                    NodeGroups[ObjectGroup].Insert(0, obj);
                } else {
                    NodeGroups[ObjectGroup].Add(obj);
                }
            }
        }

        MeshDat CurMeshData;
        Mesh mesh = new Mesh();
        RayTracingObject ParentObject, ChildObject;
        int submeshcount = 0;
        int RepCount = 0;
        Vector3 V1, V2, V3, Norm1, Norm2, Norm3;
        int MatIndex = 0;
        PrimitiveData TempPrim = new PrimitiveData();
        mesh_datas = new Mesh_Data[TotalObjectGroups];
        float totalEnergy = 0;
        int totPrims = 0;
        Mesh_Data CurentMesh;
        CurMeshData = new MeshDat();
        CurMeshData.init();
        for(int i = 0; i < TotalObjectGroups; i++) {
            ParentObject = NodeGroups[i][0];
            mesh = new Mesh();
            if(ParentObject.GetComponent<MeshFilter>() != null) {//Allows us to also render posed skinned meshes 
                mesh = ParentObject.GetComponent<MeshFilter>().sharedMesh;
            }else {
                ParentObject.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh, true);
            }
            submeshcount = mesh.subMeshCount;
            CurMeshData.Clear();
            mesh.GetNormals(CurMeshData.Normals);
            CurMeshData.Verticies.AddRange(mesh.vertices);
            if(mesh.uv.Length != 0) {
                CurMeshData.UVs.AddRange(mesh.uv);
            } else {
                Debug.Log("NO UV's: " + ParentObject.gameObject.name);
                CurMeshData.SetUvZero();
            }
            for(int i3 = 0; i3 < submeshcount; ++i3) {//Add together all the submeshes in the mesh to consider it as one object
                int PrevLength = CurMeshData.Indices.Count;
                MatIndex = ParentObject.MaterialIndex[i3];
                CurMeshData.Indices.AddRange(mesh.GetIndices(i3));
                int IndiceLength = (CurMeshData.Indices.Count - PrevLength) / 3;
                for(int i4 = 0; i4 < IndiceLength; ++i4) {
                    CurMeshData.MatDat.Add(MatIndex);
                }
            }
            RepCount += submeshcount;
            Matrix4x4 ParentMat = ParentObject.transform.worldToLocalMatrix.inverse;
            Matrix4x4 ParentMatInv = ParentObject.transform.worldToLocalMatrix;
            int progressId = UnityEditor.Progress.Start(ParentObject.name, "", UnityEditor.Progress.Options.Managed, -1);
            CurentMesh = new Mesh_Data(ParentObject.name, ref CurMeshData, progressId, _Materials, ParentObject.gameObject.transform);
            for(int i2 = 0; i2 < CurentMesh.triangles.Count; ++i2) {
                TempPrim = CurentMesh.triangles[i2];
                Vector3 TempVert1 = TempPrim.V1;
                Vector3 TempVert2 = TempPrim.V2;
                Vector3 TempVert3 = TempPrim.V3;
                
                TempPrim.Reconstruct();
                if(_Materials[TempPrim.MatDat].emmissive > 0.0f) {
                    V1 = TempPrim.V1;
                    V2 = TempPrim.V2;
                    V3 = TempPrim.V3;
                    float radiance = _Materials[TempPrim.MatDat].emmissive * _Materials[TempPrim.MatDat].BaseColor.x + 
                                    _Materials[TempPrim.MatDat].emmissive * _Materials[TempPrim.MatDat].BaseColor.y +
                                    _Materials[TempPrim.MatDat].emmissive * _Materials[TempPrim.MatDat].BaseColor.z;
                    float area = AreaOfTriangle(ParentMat * V1, ParentMat * V2, ParentMat * V3);
                    float e = radiance * area;
                    totalEnergy += e;

                    CurentMesh.LightTriangles.Add(new CudaLightTriangle() {
                        pos0 = V1,
                        posedge1 = V2 - V1,
                        posedge2 = V3 - V1,
                        Norm = (TempPrim.Norm1 + TempPrim.Norm2 + TempPrim.Norm3) / 3.0f,
                        radiance = _Materials[TempPrim.MatDat].emmissive * _Materials[TempPrim.MatDat].BaseColor,
                        sumEnergy = totalEnergy,
                        energy = e,
                        area = area,
                        MeshIndexTie = (uint)i
                        });
                }
            }

            for(int i2 = 1; i2 < NodeGroups[i].Count; i2++) {
                ChildObject = NodeGroups[i][i2];
                mesh = new Mesh();
                if(ChildObject.GetComponent<MeshFilter>() != null) { 
                    mesh = ChildObject.GetComponent<MeshFilter>().sharedMesh;
                }else {
                    ChildObject.GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh, true);
                }
                submeshcount = mesh.subMeshCount;
                CurMeshData.Clear();
                mesh.GetNormals(CurMeshData.Normals);
                CurMeshData.Verticies.AddRange(mesh.vertices);
                if(mesh.uv.Length != 0) {
                    CurMeshData.UVs.AddRange(mesh.uv);
                } else {
                    Debug.Log("NO UV's: " + ChildObject.gameObject.name);
                    CurMeshData.SetUvZero();
                }
                for(int i3 = 0; i3 < submeshcount; ++i3) {//Add together all the submeshes in the mesh to consider it as one object
                    int PrevLength = CurMeshData.Indices.Count;
                    CurMeshData.Indices.AddRange(mesh.GetIndices(i3));
                    int IndiceLength = (CurMeshData.Indices.Count - PrevLength) / 3;
                    MatIndex = ChildObject.MaterialIndex[i3];
                    for(int i4 = 0; i4 < IndiceLength; ++i4) {
                        CurMeshData.MatDat.Add(MatIndex);
                    }
                }
                RepCount += submeshcount;
                Matrix4x4 ChildMat = ChildObject.transform.worldToLocalMatrix.inverse;
                Matrix4x4 TransMat = ParentMatInv * ChildMat;
                Vector3 Ofst = ChildObject.transform.worldToLocalMatrix * ChildObject.transform.position;
                Vector3 Ofst2 = ParentMatInv * ParentObject.transform.position;
                for(int i3 = 0; i3 < CurMeshData.Indices.Count; i3 += 3) {//Transforming child meshes into the space of their parent
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
                    TempPrim.Norm1 = ParentMatInv * Norm1;
                    TempPrim.Norm2 = ParentMatInv * Norm2;
                    TempPrim.Norm3 = ParentMatInv * Norm3;
                    
                    TempPrim.tex1 = CurMeshData.UVs[Index1];
                    TempPrim.tex2 = CurMeshData.UVs[Index2];
                    TempPrim.tex3 = CurMeshData.UVs[Index3];

                    TempPrim.MatDat = CurMeshData.MatDat[i3 / 3];

                    TempPrim.Reconstruct();
                    CurentMesh.triangles.Add(TempPrim);
                    if(_Materials[TempPrim.MatDat].emmissive > 0.0f) {
                        V1 = TempPrim.V1;
                        V2 = TempPrim.V2;
                        V3 = TempPrim.V3;
                        float radiance = _Materials[TempPrim.MatDat].emmissive * _Materials[TempPrim.MatDat].BaseColor.x + 
                                        _Materials[TempPrim.MatDat].emmissive * _Materials[TempPrim.MatDat].BaseColor.y +
                                        _Materials[TempPrim.MatDat].emmissive * _Materials[TempPrim.MatDat].BaseColor.z;
                        float area = AreaOfTriangle(ParentMat * V1, ParentMat * V2, ParentMat * V3);
                        float e = radiance * area;
                        totalEnergy += e;

                        CurentMesh.LightTriangles.Add(new CudaLightTriangle() {
                            pos0 = V1,
                            posedge1 = V2 - V1,
                            posedge2 = V3 - V1,
                            Norm = (TempPrim.Norm1 + TempPrim.Norm2 + TempPrim.Norm3) / 3.0f,
                            radiance = _Materials[TempPrim.MatDat].emmissive * _Materials[TempPrim.MatDat].BaseColor,
                            sumEnergy = totalEnergy,
                            energy = e,
                            area = area,
                            MeshIndexTie = (uint)i
                            });
                    }
                }
            }
            Debug.Log("Parent Object: " + ParentObject.name);
            ProgReportData TempProgDat = new ProgReportData();
            TempProgDat.init(progressId, ParentObject.name, CurentMesh.triangles.Count);
            ProgIds.Add(TempProgDat);
            totPrims += CurentMesh.triangles.Count;
            CurentMesh.ConstructAABB();
            CurentMesh.UpdateAABB();
            mesh_datas[i] = CurentMesh;
        }

        NodeGroups = null;
        CurentMesh = null;

        Task t1 = Task.Run(() => RunThreads());
        Debug.Log("TOTAL TRIANGLES: " + totPrims);
    }

	void Aggregate(ref BVH8Builder BVH8) {//BVH aggregation/BVH compression
        for(int i = 0; i < BVH8.BVH8Nodes.Count; ++i) {
            BVHNode8Data TempNode = BVH8.BVH8Nodes[i];
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
            BVHNode8DataCompressed TempNodeComp;
                TempNodeComp.node_0xyz = new Vector3(TempNode.p.x, TempNode.p.y, TempNode.p.z);
                TempNodeComp.node_0w = System.BitConverter.ToUInt32(tempbyte, 0);
                TempNodeComp.node_1x = TempNode.base_index_child;
                TempNodeComp.node_1y = TempNode.base_index_triangle;
                TempNodeComp.node_1z = System.BitConverter.ToUInt32(metafirst, 0);
                TempNodeComp.node_1w = System.BitConverter.ToUInt32(metasecond, 0);
                TempNodeComp.node_2x = System.BitConverter.ToUInt32(minxfirst, 0);
                TempNodeComp.node_2y = System.BitConverter.ToUInt32(minxsecond, 0);
                TempNodeComp.node_2z = System.BitConverter.ToUInt32(maxxfirst, 0);
                TempNodeComp.node_2w = System.BitConverter.ToUInt32(maxxsecond, 0);
                TempNodeComp.node_3x = System.BitConverter.ToUInt32(minyfirst, 0);
                TempNodeComp.node_3y = System.BitConverter.ToUInt32(minysecond, 0);
                TempNodeComp.node_3z = System.BitConverter.ToUInt32(maxyfirst, 0);
                TempNodeComp.node_3w = System.BitConverter.ToUInt32(maxysecond, 0);
                TempNodeComp.node_4x = System.BitConverter.ToUInt32(minzfirst, 0);
                TempNodeComp.node_4y = System.BitConverter.ToUInt32(minzsecond, 0);
                TempNodeComp.node_4z = System.BitConverter.ToUInt32(maxzfirst, 0);
                TempNodeComp.node_4w = System.BitConverter.ToUInt32(maxzsecond, 0);
                BVH8Aggregated[i] = TempNodeComp;
	    }
	}

    public void UpdateTLAS() {	//Allows for objects to be moved in the scene or animated while playing 

        MyMeshesCompacted.Clear();     
        int MeshDataCount =  mesh_datas.Length;
        int aggregated_bvh_node_count = 2 * MeshDataCount;
        for(int i = 0; i < MeshDataCount; i++) {
             MyMeshesCompacted.Add(new MyMeshDataCompacted() {
                mesh_data_bvh_offsets = aggregated_bvh_node_count,
                Transform =  mesh_datas[i].Transform.transform.worldToLocalMatrix,
                Inverse = mesh_datas[i].Transform.transform.worldToLocalMatrix.inverse,
                Center = mesh_datas[i].Transform.transform.position
              });
             mesh_datas[i].UpdateAABB();
            MeshAABBs[i] = mesh_datas[i].aabb;
            aggregated_bvh_node_count += mesh_datas[i].BVH.cwbvhnode_count;
        }
        BVH8Builder TLASBVH8 = new BVH8Builder(new BVH2Builder(MeshAABBs), ref MyMeshesCompacted);
       LightTriangleIndices = new List<int>();
       for(int i = 0; i < TLASBVH8.cwbvh_indices.Count; i++) {
            LightTriangleIndices.Add(i);
        }
        LightTriangleIndices.Sort((s1, s2) => TLASBVH8.cwbvh_indices[s1].CompareTo(TLASBVH8.cwbvh_indices[s2]));
        Aggregate(ref TLASBVH8);
	}

	public void UpdateMaterials() {//Allows for live updating of material properties of any object
		int curmat = 0;
        int RayObjectCount = RayTracingMaster._rayTracingObjects.Count;
		for(int i = 0; i < RayObjectCount; i++) {
            int MaterialCount = RelevantMaterials[i].MatType.Length;
			for(int i2 = 0; i2 < MaterialCount; i2++) {
				MaterialData TempMat = _Materials[curmat];
				if(TempMat.HasTextures == 0) {
					TempMat.BaseColor = RelevantMaterials[i].BaseColor[i2];
				}
				TempMat.emmissive = RelevantMaterials[i].emmission[i2];
				TempMat.Roughness = RelevantMaterials[i].Roughness[i2];
				TempMat.eta = RelevantMaterials[i].eta[i2];
                TempMat.MatType = RelevantMaterials[i].MatType[i2];
				_Materials[curmat] = TempMat;
				curmat++;
			}
		}
	}


    
}
