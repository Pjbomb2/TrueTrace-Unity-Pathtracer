using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;

public class AssetManager : MonoBehaviour {
    public Texture2D atlas;
	public Mesh_Data[] mesh_datas;

	public List<MaterialData> _Materials;
    [HideInInspector]
    public List<CudaTriangle> aggregated_triangles;
    [HideInInspector]
    public Transform[] RelevantTransforms;
    [HideInInspector]
    public RayTracingObject[] RelevantMaterials;   
    [HideInInspector]
    public List<BVHNode8DataCompressed> BVH8StaticAggregated;
    [HideInInspector]
    public List<BVHNode8DataCompressed> BVH8Aggregated;
    [HideInInspector]
    public List<MyMeshDataCompacted> MyMeshesCompacted;
    [HideInInspector]
    public List<ProgReportData> ProgIds;
    
    private List<Task> tasks;

    public struct objtextureindices {
        public int textureindexstart;
        public int textureindexcount;
    }

    public void CreateAtlas() {//Creates texture atlas
        _Materials.Clear();
        List<objtextureindices> ObjectTextures = new List<objtextureindices>();
        List<Texture2D> TempTexs = new List<Texture2D>();
        List<int> texindex = new List<int>();
        int texcount = 0;
        foreach(RayTracingObject obj in RayTracingMaster._rayTracingObjects) {
            int texturestart = texindex.Count;
            for(int i = 0; i < obj.GetComponent<MeshFilter>().sharedMesh.subMeshCount; ++i) {
                if(obj.GetComponent<Renderer>().sharedMaterials[i].mainTexture != null) {
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
        foreach (RayTracingObject obj in RayTracingMaster._rayTracingObjects) {
            int SubMeshCount = obj.GetComponent<MeshFilter>().sharedMesh.subMeshCount; 
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
                RayTracingObject TargetObject = obj.GetComponent<RayTracingObject>();
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

        System.TimeSpan ts;
        System.Diagnostics.Stopwatch GlobalStopWatch = new System.Diagnostics.Stopwatch();
        System.Diagnostics.Stopwatch LocalStopWatch = new System.Diagnostics.Stopwatch();
	public void Begin() {
        int ObjCount = RayTracingMaster._rayTracingObjects.Count;
        ProgIds = new List<ProgReportData>();
        GlobalStopWatch = System.Diagnostics.Stopwatch.StartNew();
		MyMeshesCompacted = new List<MyMeshDataCompacted>();
		mesh_datas = new Mesh_Data[ObjCount];
		RelevantTransforms = new Transform[ObjCount];
		RelevantMaterials = new RayTracingObject[ObjCount];
        BVH8StaticAggregated = new List<BVHNode8DataCompressed>();
        aggregated_triangles = new List<CudaTriangle>();
		int MatRep = 0;
        int RepCount = 0;
        int Reps = 0;
        int totPrims = 0;
		foreach(RayTracingObject obj in RayTracingMaster._rayTracingObjects) {
			RelevantMaterials[MatRep] = obj.GetComponent<RayTracingObject>();
			MatRep++;
		}
		CreateAtlas();
		foreach(RayTracingObject obj in RayTracingMaster._rayTracingObjects) {
            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
			RelevantTransforms[Reps] = obj.GetComponent<Transform>();
            List<Vector3> Verticies = new List<Vector3>(mesh.vertices);
            List<PrimitiveData> Primitives = new List<PrimitiveData>();
            int submeshcount = mesh.subMeshCount;
            List<int> MatDat = new List<int>();
            List<int> Indices = new List<int>();
            List<Vector2> Uv = new List<Vector2>();
            List<Vector3> Normals = new List<Vector3>();
            mesh.GetNormals(Normals);

            if(mesh.uv.Length != 0) {
                Uv.AddRange(mesh.uv);
            } else {
                for(int i = 0; i < Verticies.Count; ++i) {//Fallback incase the mesh has no UV's
                    Uv.Add(new Vector2(0.0f,0.0f));
                }
            }
            for(int i = 0; i < submeshcount; ++i) {//Add together all the submeshes in the mesh to consider it as one object
                int PrevLength = Indices.Count;
                Indices.AddRange(mesh.GetIndices(i));
                int IndiceLength = (Indices.Count - PrevLength) / 3;
                for(int i2 = 0; i2 < IndiceLength; ++i2) {
                    MatDat.Add(i + RepCount);
                }
             }

             RepCount += submeshcount;
             AABB RootBB = new AABB();
             RootBB.init();
             Matrix4x4 Transform = RelevantTransforms[Reps].transform.worldToLocalMatrix;

             for(int i = 0; i < Indices.Count; i += 3) {//Create my own primitive from each mesh triangle
	            PrimitiveData TempPrim = new PrimitiveData();
                TempPrim.V1 = Verticies[Indices[i]];
                TempPrim.V2 = Verticies[Indices[i+2]];
                TempPrim.V3 = Verticies[Indices[i+1]];
                TempPrim.Norm1 = Vector3.Normalize(Normals[Indices[i]]);
                TempPrim.Norm2 = Vector3.Normalize(Normals[Indices[i+2]]);
                TempPrim.Norm3 = Vector3.Normalize(Normals[Indices[i+1]]);
                TempPrim.tex1 = Uv[Indices[i]];
                TempPrim.tex2 = Uv[Indices[i+2]];
                TempPrim.tex3 = Uv[Indices[i+1]];
                TempPrim.MatDat = MatDat[i / 3];
                TempPrim.Reconstruct();
                RootBB.Extend(TempPrim.BBMax, TempPrim.BBMin);
                Primitives.Add(TempPrim);
             }

            totPrims += Primitives.Count;
            string objName = obj.name;
            int progressId = UnityEditor.Progress.Start(objName, "", UnityEditor.Progress.Options.Managed, -1);//These progress bits allow me to display estimated time left/progress bars for each mesh construction
            mesh_datas[Reps] = new Mesh_Data(objName, Primitives, progressId);
            ProgReportData TempProgDat = new ProgReportData();
            TempProgDat.init(progressId, objName, Primitives.Count);
            ProgIds.Add(TempProgDat);
            mesh_datas[Reps].Transform = Transform.inverse;
            mesh_datas[Reps].Position = RelevantTransforms[Reps].transform.position;
            mesh_datas[Reps].ConstructAABB();
			Reps++;
        }

        Task t1 = Task.Run(() => RunThreads());//Multithreads so that many BVH's can be constructed at once, and makes it so that it doesnt freeze unity
        Debug.Log("TOTAL TRIANGLES: " + totPrims);
    }

    public async Task RunThreads() {
        tasks = new List<Task>();
        for(int i = 0; i < mesh_datas.Length; i++) {
            var currentRep = i;
            Task t1 = Task.Run(() => mesh_datas[currentRep].Construct());
            tasks.Add(t1);
        }
        Task.WaitAll(tasks.ToArray());

        int aggregated_bvh_node_count = 2 * mesh_datas.Length;
        int aggregated_index_count = 0;
            for(int i = 0; i < mesh_datas.Length; i++) {
                for(int n = 0; n < mesh_datas[i].BVH.cwbvhnode_count; n++) {
                    BVHNode8Data TempNode2 = mesh_datas[i].BVH.BVH8Nodes[n];
                    TempNode2.base_index_triangle += (uint)aggregated_index_count;
                    TempNode2.base_index_child += (uint)aggregated_bvh_node_count;
                    mesh_datas[i].BVH.BVH8Nodes[n] = TempNode2;
                }
                mesh_datas[i].Aggregate();
                BVH8StaticAggregated.AddRange(mesh_datas[i].Aggregated_BVH_Nodes);

                for(int i2 = 0; i2 < mesh_datas[i].triangles.Count; ++i2) {//This constructs the list of triangles that actually get sent to the GPU
                    int index = mesh_datas[i].BVH.cwbvh_indices[i2];
                    PrimitiveData triangle = mesh_datas[i].triangles[index];
                    CudaTriangle TempTri = new CudaTriangle();
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

 private void UpdateTLASAsync() { 
    BVH8Aggregated = new List<BVHNode8DataCompressed>();
    int aggregated_bvh_node_count = 2 * mesh_datas.Length;
    MyMeshesCompacted.Clear();
    for(int i = 0; i < mesh_datas.Length; i++) {
         MyMeshesCompacted.Add(new MyMeshDataCompacted() {
            mesh_data_bvh_offsets = aggregated_bvh_node_count,
            Transform =  mesh_datas[i].Transform.inverse
          });
         mesh_datas[i].UpdateAABB();
        aggregated_bvh_node_count += mesh_datas[i].BVH.cwbvhnode_count;
    }
    BVH2Builder BVH2 = new BVH2Builder(mesh_datas);//Build binary BVH
    BVH8Builder BVH8 = new BVH8Builder(BVH2, ref MyMeshesCompacted);//Build Compressed Wide BVH

    Aggregate(BVH8);//Aggregate the BVH to get sent to the GPU(compressing it)
    BVH8Aggregated.AddRange(BVH8StaticAggregated);
    GlobalStopWatch.Stop(); ts = GlobalStopWatch.Elapsed; writetime("Total Construction Time: ", ts);//Tells me how long total construction has taken
}


    public void BuildCombined() {//Seperate function! It aggregates all non dynamic meshes(user defined which are dynamic) into 1 mesh to build a BVH from 
                                 //results in performance improvements but can greatly increase build times
        int ObjCount = RayTracingMaster._rayTracingObjects.Count;
        ProgIds = new List<ProgReportData>();
        GlobalStopWatch = System.Diagnostics.Stopwatch.StartNew();
        MyMeshesCompacted = new List<MyMeshDataCompacted>();
		mesh_datas = new Mesh_Data[ObjCount];
		Transform[] RelevantTransformsOverall = new Transform[ObjCount];
        RelevantTransforms = new Transform[ObjCount];
		RelevantMaterials = new RayTracingObject[ObjCount];
		List<Mesh_Data> StaticMeshes = new List<Mesh_Data>();
        List<Mesh_Data> DynamicMeshes = new List<Mesh_Data>();
		List<RayTracingObject> RelevantStatics = new List<RayTracingObject>();
		List<RayTracingObject> RelevantDynamics = new List<RayTracingObject>();;
        BVH8StaticAggregated = new List<BVHNode8DataCompressed>();
        aggregated_triangles = new List<CudaTriangle>();
        int MatRep = 0;
        int RepCount = 0;
        int Reps = 0;
        int totPrims = 0;
		foreach(RayTracingObject obj in RayTracingMaster._rayTracingObjects) {
			RelevantMaterials[MatRep] = obj.GetComponent<RayTracingObject>();
			MatRep++;
		}
		CreateAtlas();
		foreach(RayTracingObject obj in RayTracingMaster._rayTracingObjects) {
            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
			RelevantTransformsOverall[Reps] = obj.GetComponent<Transform>();
            List<Vector3> Verticies = new List<Vector3>(mesh.vertices);
            List<PrimitiveData> Primitives = new List<PrimitiveData>();
            int submeshcount = mesh.subMeshCount;
            List<int> MatDat = new List<int>();
            List<int> Indices = new List<int>();
            List<Vector2> Uv = new List<Vector2>();
            List<Vector3> Normals = new List<Vector3>();
            mesh.GetNormals(Normals);
            if(mesh.uv.Length != 0) {
                Uv.AddRange(mesh.uv);
            } else {
                for(int i = 0; i < Verticies.Count; ++i) {
                    Uv.Add(new Vector2(0.0f,0.0f));
                }
            }
            for(int i = 0; i < submeshcount; ++i) {
                int PrevLength = Indices.Count;
                Indices.AddRange(mesh.GetIndices(i));
                int IndiceLength = (Indices.Count - PrevLength) / 3;
                for(int i2 = 0; i2 < IndiceLength; ++i2) {
                    MatDat.Add(i + RepCount);
                }
             }
             RepCount += submeshcount;
             AABB RootBB = new AABB();
             RootBB.init();
             Matrix4x4 Transform = RelevantTransformsOverall[Reps].transform.worldToLocalMatrix;

             for(int i = 0; i < Indices.Count; i += 3) {
                PrimitiveData TempPrim = new PrimitiveData();
                TempPrim.V1 = Verticies[Indices[i]];
                TempPrim.V2 = Verticies[Indices[i+2]];
                TempPrim.V3 = Verticies[Indices[i+1]];
                TempPrim.Norm1 = Vector3.Normalize(Normals[Indices[i]]);
                TempPrim.Norm2 = Vector3.Normalize(Normals[Indices[i+2]]);
                TempPrim.Norm3 = Vector3.Normalize(Normals[Indices[i+1]]);
                TempPrim.tex1 = Uv[Indices[i]];
                TempPrim.tex2 = Uv[Indices[i+2]];
                TempPrim.tex3 = Uv[Indices[i+1]];
                TempPrim.MatDat = MatDat[i / 3];
                TempPrim.Reconstruct();
                RootBB.Extend(TempPrim.BBMax, TempPrim.BBMin);
                Primitives.Add(TempPrim);
             }

             totPrims += Primitives.Count;

             mesh_datas[Reps] = new Mesh_Data(obj.name, Primitives, -1);

            mesh_datas[Reps].Transform = Transform.inverse;
            mesh_datas[Reps].Position = RelevantTransformsOverall[Reps].transform.position;
            mesh_datas[Reps].ConstructAABB();
            mesh_datas[Reps].isStatic = (obj.GetComponent<RayTracingObject>().Dynamic == 1) ? false : true;
            mesh_datas[Reps].UpdateAABB();

             if(mesh_datas[Reps].isStatic) {
				StaticMeshes.Add(mesh_datas[Reps]);
				RelevantStatics.Add(obj);
			} else {
				DynamicMeshes.Add(mesh_datas[Reps]);
				RelevantDynamics.Add(obj);
			}
			Reps++;
            }

            RelevantTransforms = new Transform[RelevantDynamics.Count + 1];
            int StaticCount = StaticMeshes.Count;
            Mesh_Data RootMesh = StaticMeshes[0];
            RelevantTransforms[0] = RelevantStatics[0].transform;
            for(int i = StaticCount - 1; i > 0; i--) {
			List<PrimitiveData> TempPrims = new List<PrimitiveData>();
                Matrix4x4 TransMat = RootMesh.Transform.inverse * StaticMeshes[i].Transform;
                Matrix4x4 TransMat2 = StaticMeshes[i].Transform.inverse * RootMesh.Transform;
                Vector3 Ofst = StaticMeshes[i].Transform.inverse * StaticMeshes[i].Position;
                Vector3 Ofst2 = RootMesh.Transform.inverse * RootMesh.Position;
                for(int i2 = 0; i2 < StaticMeshes[i].triangles.Count; ++i2) {//transforms every meshes triangles so they can be combined into 1 mesh without 1 transform
                	PrimitiveData TempTempPrim = StaticMeshes[i].triangles[i2];
					Vector3 TempVert1 = TempTempPrim.V1 + Ofst;
					Vector3 TempVert2 = TempTempPrim.V2 + Ofst;
					Vector3 TempVert3 = TempTempPrim.V3 + Ofst;
                	TempVert1 = TransMat * TempVert1;
                	TempVert2 = TransMat * TempVert2;
                	TempVert3 = TransMat * TempVert3;
                	
                	TempTempPrim.V1 = TempVert1 - Ofst2;
                	TempTempPrim.V2 = TempVert2 - Ofst2;
                	TempTempPrim.V3 = TempVert3 - Ofst2;

                    TempTempPrim.Norm1 = TransMat * TempTempPrim.Norm1;
                    TempTempPrim.Norm2 = TransMat * TempTempPrim.Norm2;
                    TempTempPrim.Norm3 = TransMat * TempTempPrim.Norm3;
                    
                    TempTempPrim.Reconstruct();
                    
                    TempPrims.Add(TempTempPrim);
                }
             RootMesh.triangles.AddRange(TempPrims);
            }
			mesh_datas = new Mesh_Data[RelevantDynamics.Count + 1];
            RootMesh.ConstructAABB();
            RootMesh.UpdateAABB();
            RootMesh.Name = "Aggregated Mesh";
            int progressId = UnityEditor.Progress.Start(RootMesh.Name, "", UnityEditor.Progress.Options.Managed, -1);
            RootMesh.progressId = progressId;
			mesh_datas[0] = RootMesh;
            ProgReportData TempProgDat = new ProgReportData();
            TempProgDat.init(progressId, RootMesh.Name, RootMesh.triangles.Count);
            ProgIds.Add(TempProgDat);

             for(int i = 0; i < DynamicMeshes.Count; i++) {
                Mesh_Data CurMesh = DynamicMeshes[i];
                progressId = UnityEditor.Progress.Start(CurMesh.Name, "", UnityEditor.Progress.Options.Managed, -1);
                CurMesh.progressId = progressId;
                TempProgDat = new ProgReportData();
                TempProgDat.init(progressId, CurMesh.Name, CurMesh.triangles.Count);
                ProgIds.Add(TempProgDat);
                mesh_datas[i + 1] = CurMesh;
                RelevantTransforms[i + 1] = RelevantDynamics[i].transform;

             }
            Task t1 = Task.Run(() => RunThreads());
            Debug.Log("TOTAL TRIANGLES: " + totPrims);

    }








	void Aggregate(BVH8Builder BVH8) {//BVH aggregation/BVH compression
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
	        BVH8Aggregated.Add(new BVHNode8DataCompressed() {
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
	}

    public void UpdateTLAS() {	//Allows for objects to be moved in the scene or animated while playing
    	BVH8Aggregated = new List<BVHNode8DataCompressed>();
		int aggregated_bvh_node_count = 2 * mesh_datas.Length;
		MyMeshesCompacted.Clear();

		for(int i = 0; i < mesh_datas.Length; i++) {
        	mesh_datas[i].Transform = RelevantTransforms[i].transform.worldToLocalMatrix.inverse;
        	mesh_datas[i].Position = RelevantTransforms[i].transform.position;
             MyMeshesCompacted.Add(new MyMeshDataCompacted() {
                mesh_data_bvh_offsets = aggregated_bvh_node_count,
                Transform =  RelevantTransforms[i].transform.worldToLocalMatrix
              });
             mesh_datas[i].UpdateAABB();

			aggregated_bvh_node_count += mesh_datas[i].BVH.cwbvhnode_count;
        }
       	BVH2Builder BVH2 = new BVH2Builder(mesh_datas);
       	BVH8Builder BVH8 = new BVH8Builder(BVH2, ref MyMeshesCompacted);

       	Aggregate(BVH8);
       	BVH8Aggregated.AddRange(BVH8StaticAggregated);
	}

	public void UpdateMaterials() {//Allows for live updating of material properties of any object
		int curmat = 0;
		for(int i = 0; i < RayTracingMaster._rayTracingObjects.Count; i++) {
			for(int i2 = 0; i2 < RelevantMaterials[i].MatType.Length; i2++) {
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
