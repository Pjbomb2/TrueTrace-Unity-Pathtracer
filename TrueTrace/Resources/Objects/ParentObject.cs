// #define DoLightMapping
// #define HardwareRT
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
        [HideInInspector] public ComputeBuffer TriBuffer;
        [HideInInspector] public ComputeBuffer BVHBuffer;
        [HideInInspector] public List<LightMapTriangle> LightMapTris;
        public string Name;
        [HideInInspector] public GraphicsBuffer[] VertexBuffers;
        [HideInInspector] public ComputeBuffer[] IndexBuffers;
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
        public List<MaterialData> _Materials;
        [HideInInspector] public int MatOffset;
        [HideInInspector] public int InstanceID;
        [HideInInspector] public StorableTransform[] CachedTransforms;
        [HideInInspector] public MeshDat CurMeshData;
        public int TotalObjects;
        [HideInInspector] public List<MeshTransformVertexs> TransformIndexes;
        [HideInInspector] public bool HasCompleted;
        [HideInInspector] public float TotEnergy;
        [HideInInspector] public int LightCount;
        [HideInInspector] public Transform ThisTransform;

        [HideInInspector] public int ConstructKernel;
        [HideInInspector] public int RefitLayerKernel;
        [HideInInspector] public int NodeUpdateKernel;
        [HideInInspector] public int NodeCompressKernel;
        [HideInInspector] public int NodeInitializerKernel;

        [HideInInspector] public int CompactedMeshData;

        [HideInInspector] public int InstanceReferences;
        [HideInInspector] public int GroupMatOffset;

        [HideInInspector] public bool NeedsToUpdate;

        public AssetManager Assets;

        public int TotalTriangles;
        public bool IsSkinnedGroup;

        private ComputeBuffer NodeBuffer;
        private ComputeBuffer AdvancedTriangleBuffer;
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

        [HideInInspector] public int NodeOffset;
        [HideInInspector] public int TriOffset;
        [HideInInspector] public List<BVHNode8DataFixed> SplitNodes;

        #if HardwareRT
            public List<int> HWRTIndex;
            public Renderer[] Renderers;
        #endif

        [System.Serializable]
        public struct StorableTransform
        {
            public Matrix4x4 WTL;
            public Vector3 Position;
        }

        [System.Serializable]
        public struct MeshTransformVertexs
        {
            public int VertexStart;
            public int VertexCount;
            public int IndexOffset;
        }

        [System.Serializable]
        public struct PerMatTextureData
        {
            public bool HasAlbedoMap;
            public bool HasNormalMap;
            public bool HasEmissiveMap;
            public bool HasMetallicMap;
            public bool HasRoughnessMap;
            public RayTracingObject MaterialObject;
            public int Offset;
            public float RoughnessRange;
            public float MetallicRange;
            public float IORRange;
            public float SpecTransRange;
            public bool IsCutout;
            public Color BaseColorValue;
            public Vector4 AlbedoTextureScale;
            public Vector4 NormalTextureScale;
            public Vector4 MetallicTextureScale;
            public Vector4 RoughnessTextureScale;
            public Vector4 EmissiveTextureScale;
        }
        public List<PerMatTextureData> MatTexData;

        public void CallUpdate()
        {
            if (!Assets.UpdateQue.Contains(this))
            {
                Assets.UpdateQue.Add(this);
            }
        }

        public void ClearAll()
        {

            Triangles = null;

            if (LightTriangles != null)
            {
                LightTriangles.Clear();
                LightTriangles.TrimExcess();
            }
            BVH = null;
            if (_Materials != null)
            {
                _Materials.Clear();
                _Materials.TrimExcess();
            }
            CurMeshData.Clear();
            TransformIndexes = null;
            MeshCountChanged = true;
            HasCompleted = false;
            LayerStack = null;
            if (NodePair != null)
            {
                NodePair.Clear();
                NodePair.TrimExcess();
            }
            ToBVHIndex = null;
            AggTriangles = null;
            AggNodes = null;
            ForwardStack = null;
            if (VertexBuffers != null)
            {
                for (int i = 0; i < VertexBuffers.Length; i++)
                {
                    VertexBuffers[i]?.Dispose();
                    IndexBuffers[i]?.Dispose();
                }
                NodeBuffer?.Dispose();
                AdvancedTriangleBuffer?.Dispose();
                StackBuffer?.Dispose();
                CWBVHIndicesBuffer?.Dispose();
                BVHDataBuffer?.Dispose();
                ToBVHIndexBuffer?.Dispose();
                for(int i2 = 0; i2 < WorkingBuffer.Length; i2++) WorkingBuffer[i2]?.Dispose();
            }
            if (TriBuffer != null)
            {
                TriBuffer?.Dispose();
                BVHBuffer?.Dispose();
            }
        }

        public void OnApplicationQuit()
        {
            if (VertexBuffers != null)
            {
                for (int i = 0; i < SkinnedMeshes.Length; i++)
                {
                    VertexBuffers[i].Dispose();
                    IndexBuffers[i].Dispose();
                    NodeBuffer.Dispose();
                    AdvancedTriangleBuffer.Dispose();
                    StackBuffer.Dispose();
                    CWBVHIndicesBuffer.Dispose();
                    BVHDataBuffer.Dispose();
                    ToBVHIndexBuffer.Dispose();
                    for(int i2 = 0; i2 < WorkingBuffer.Length; i2++) WorkingBuffer[i2].Dispose();
                }
            }
            if (TriBuffer != null)
            {
                TriBuffer.Dispose();
                BVHBuffer.Dispose();
            }
        }


        public void init()
        {
            this.gameObject.isStatic = false;
            InstanceID = this.GetInstanceID();
            Name = this.name;
            ThisTransform = this.transform;
            TransformIndexes = new List<MeshTransformVertexs>();
            _Materials = new List<MaterialData>();
            LightTriangles = new List<CudaLightTriangle>();
            MeshCountChanged = true;
            ObjectOrderHasChanged = false;
            HasCompleted = false;
            MeshRefit = Resources.Load<ComputeShader>("Utility/BVHRefitter");
            ConstructKernel = MeshRefit.FindKernel("Construct");
            RefitLayerKernel = MeshRefit.FindKernel("RefitLayer");
            NodeUpdateKernel = MeshRefit.FindKernel("NodeUpdate");
            NodeCompressKernel = MeshRefit.FindKernel("NodeCompress");
            NodeInitializerKernel = MeshRefit.FindKernel("NodeInitializer");
            Assets = GameObject.Find("Scene").GetComponent<AssetManager>();
            #if DoLightMapping
                LightMapTris = new List<LightMapTriangle>();
            #endif
        }
        private bool NeedsToResetBuffers = true;
        public void SetUpBuffers()
        {
            if (NeedsToResetBuffers)
            {
                if (TriBuffer != null) TriBuffer.Release();
                if (BVHBuffer != null) BVHBuffer.Release();
                TriBuffer = new ComputeBuffer(AggTriangles.Length, 88);
                BVHBuffer = new ComputeBuffer(AggNodes.Length, 80);
                TriBuffer.SetData(AggTriangles);
                BVHBuffer.SetData(AggNodes);
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

        public void CreateAtlas()
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
            MatTexData = new List<PerMatTextureData>();
            RoughnessTexChannelIndex = new List<int>();
            MetallicTexChannelIndex = new List<int>();
            Mesh mesh = new Mesh();
            PerMatTextureData CurrentTexDat = new PerMatTextureData();
            foreach (RayTracingObject obj in ChildObjects)
            {
                List<Material> DoneMats = new List<Material>();
                if (obj.GetComponent<MeshFilter>() != null)
                {
                    mesh = obj.GetComponent<MeshFilter>().sharedMesh;
                }
                else
                {
                    mesh = obj.GetComponent<SkinnedMeshRenderer>().sharedMesh;
                }
                obj.matfill();
                Material[] SharedMaterials = (obj.GetComponent<Renderer>() != null) ? obj.GetComponent<Renderer>().sharedMaterials : obj.GetComponent<SkinnedMeshRenderer>().sharedMaterials;
                int SharedMatLength = Mathf.Min(obj.Indexes.Length, SharedMaterials.Length);
                int Offset = 0;
                for (int i = 0; i < SharedMatLength; ++i)
                {
                    if (DoneMats.IndexOf(SharedMaterials[i]) != -1)
                    {
                        CurrentTexDat.Offset = DoneMats.IndexOf(SharedMaterials[i]);
                        Offset++;
                    }
                    else
                    {
                        CurrentTexDat.Offset = Offset;
                        Offset++;
                        DoneMats.Add(SharedMaterials[i]);
                    }
                    CurrentTexDat.MaterialObject = obj;
                    CurrentTexDat.HasRoughnessMap = false;
                    CurrentTexDat.HasAlbedoMap = false;
                    CurrentTexDat.HasNormalMap = false;
                    CurrentTexDat.HasEmissiveMap = false;
                    CurrentTexDat.HasMetallicMap = false;
                    CurrentTexDat.MetallicRange = -1;
                    CurrentTexDat.RoughnessRange = -1;
                    CurrentTexDat.IORRange = -1;
                    CurrentTexDat.SpecTransRange = -1;
                    CurrentTexDat.IsCutout = false;
                    RayObjectTextureIndex TempObj = new RayObjectTextureIndex();
                    TempObj.Obj = obj;
                    TempObj.ObjIndex = i;
                    int Index = AssetManager.ShaderNames.IndexOf(SharedMaterials[i].shader.name);
                    if (Index == -1)
                    {
                        Debug.Log("Adding Material To XML: " + SharedMaterials[i].shader.name);

                        if (SharedMaterials[i].mainTexture != null)
                        {
                            if (AlbedoTexs.Contains(SharedMaterials[i].mainTexture))
                            {
                                AlbedoIndexes[AlbedoTexs.IndexOf(SharedMaterials[i].mainTexture)].RayObjectList.Add(TempObj);
                            }
                            else
                            {
                                AlbedoIndexes.Add(new RayObjects());
                                AlbedoIndexes[AlbedoIndexes.Count - 1].RayObjectList.Add(TempObj);
                                AlbedoTexs.Add(SharedMaterials[i].mainTexture);
                            }
                        }
                        Assets.AddMaterial(SharedMaterials[i].shader);
                        Index = AssetManager.ShaderNames.IndexOf(SharedMaterials[i].shader.name);
                    }
                    MaterialShader RelevantMat = AssetManager.data.Material[Index];
                    if(!RelevantMat.MetallicRange.Equals("null")) {
                        CurrentTexDat.MetallicRange = SharedMaterials[i].GetFloat(RelevantMat.MetallicRange);
                    }
                    if(!RelevantMat.RoughnessRange.Equals("null")) {
                        CurrentTexDat.RoughnessRange = SharedMaterials[i].GetFloat(RelevantMat.RoughnessRange);
                    }
                    if(!RelevantMat.BaseColorValue.Equals("null")) {
                        CurrentTexDat.BaseColorValue = SharedMaterials[i].GetColor(RelevantMat.BaseColorValue);
                    } else {
                        CurrentTexDat.BaseColorValue = Color.white;
                    }
                    if(RelevantMat.IsGlass) {// || (SharedMaterials[i].GetFloat("_Mode") == 3)) {
                        CurrentTexDat.IORRange = 1.0f;
                        CurrentTexDat.SpecTransRange = 1f;
                    }
                    if(RelevantMat.IsCutout) {// || (SharedMaterials[i].GetFloat("_Mode") == 1)) {
                        CurrentTexDat.IsCutout = true;
                    }
                    if(RelevantMat.Name.Equals("Standard") && SharedMaterials[i].GetFloat("_Mode") == 1) CurrentTexDat.IsCutout = true;
                    if (SharedMaterials[i].GetTexture(RelevantMat.BaseColorTex) as Texture2D != null)
                    {
                        CurrentTexDat.AlbedoTextureScale = new Vector4(SharedMaterials[i].GetTextureScale(RelevantMat.BaseColorTex).x, SharedMaterials[i].GetTextureScale(RelevantMat.BaseColorTex).y, SharedMaterials[i].GetTextureOffset(RelevantMat.BaseColorTex).x, SharedMaterials[i].GetTextureOffset(RelevantMat.BaseColorTex).y);
                        if (AlbedoTexs.Contains(SharedMaterials[i].GetTexture(RelevantMat.BaseColorTex)))
                        {
                            AlbedoIndexes[AlbedoTexs.IndexOf(SharedMaterials[i].GetTexture(RelevantMat.BaseColorTex))].RayObjectList.Add(TempObj);
                        }
                        else
                        {
                            AlbedoIndexes.Add(new RayObjects());
                            AlbedoIndexes[AlbedoIndexes.Count - 1].RayObjectList.Add(TempObj);
                            AlbedoTexs.Add(SharedMaterials[i].GetTexture(RelevantMat.BaseColorTex));
                        }
                    }
                    if (!RelevantMat.NormalTex.Equals("null") && SharedMaterials[i].GetTexture(RelevantMat.NormalTex) as Texture2D != null)
                    {
                        CurrentTexDat.NormalTextureScale = new Vector4(SharedMaterials[i].GetTextureScale(RelevantMat.NormalTex).x, SharedMaterials[i].GetTextureScale(RelevantMat.NormalTex).y, SharedMaterials[i].GetTextureOffset(RelevantMat.NormalTex).x, SharedMaterials[i].GetTextureOffset(RelevantMat.NormalTex).y);
                        if (NormalTexs.Contains(SharedMaterials[i].GetTexture(RelevantMat.NormalTex)))
                        {
                            NormalIndexes[NormalTexs.IndexOf(SharedMaterials[i].GetTexture(RelevantMat.NormalTex))].RayObjectList.Add(TempObj);
                        }
                        else
                        {
                            NormalIndexes.Add(new RayObjects());
                            NormalIndexes[NormalIndexes.Count - 1].RayObjectList.Add(TempObj);
                            NormalTexs.Add(SharedMaterials[i].GetTexture(RelevantMat.NormalTex));
                        }
                    }
                    if (!RelevantMat.EmissionTex.Equals("null") && SharedMaterials[i].GetTexture(RelevantMat.EmissionTex) as Texture2D != null)
                    {
                        CurrentTexDat.EmissiveTextureScale = new Vector4(SharedMaterials[i].GetTextureScale(RelevantMat.EmissionTex).x, SharedMaterials[i].GetTextureScale(RelevantMat.EmissionTex).y, SharedMaterials[i].GetTextureOffset(RelevantMat.EmissionTex).x, SharedMaterials[i].GetTextureOffset(RelevantMat.EmissionTex).y);
                        CurrentTexDat.HasEmissiveMap = true;
                        if (EmissionTexs.Contains(SharedMaterials[i].GetTexture(RelevantMat.EmissionTex)))
                        {
                            EmissionIndexes[EmissionTexs.IndexOf(SharedMaterials[i].GetTexture(RelevantMat.EmissionTex))].RayObjectList.Add(TempObj);
                        }
                        else
                        {
                            EmissionIndexes.Add(new RayObjects());
                            EmissionIndexes[EmissionIndexes.Count - 1].RayObjectList.Add(TempObj);
                            EmissionTexs.Add(SharedMaterials[i].GetTexture(RelevantMat.EmissionTex));
                        }
                    }
                    if (!RelevantMat.MetallicTex.Equals("null") && SharedMaterials[i].GetTexture(RelevantMat.MetallicTex) as Texture2D != null)
                    {
                        CurrentTexDat.MetallicTextureScale = new Vector4(SharedMaterials[i].GetTextureScale(RelevantMat.MetallicTex).x, SharedMaterials[i].GetTextureScale(RelevantMat.MetallicTex).y, SharedMaterials[i].GetTextureOffset(RelevantMat.MetallicTex).x, SharedMaterials[i].GetTextureOffset(RelevantMat.MetallicTex).y);
                        if (MetallicTexs.Contains(SharedMaterials[i].GetTexture(RelevantMat.MetallicTex)))
                        {
                            MetallicIndexes[MetallicTexs.IndexOf(SharedMaterials[i].GetTexture(RelevantMat.MetallicTex))].RayObjectList.Add(TempObj);
                        }
                        else
                        {
                            MetallicIndexes.Add(new RayObjects());
                            MetallicIndexes[MetallicIndexes.Count - 1].RayObjectList.Add(TempObj);
                            MetallicTexs.Add(SharedMaterials[i].GetTexture(RelevantMat.MetallicTex));
                            MetallicTexChannelIndex.Add(RelevantMat.MetallicTexChannel);
                        }
                    }
                    if (!RelevantMat.RoughnessTex.Equals("null") && SharedMaterials[i].GetTexture(RelevantMat.RoughnessTex) as Texture2D != null)
                    {
                        CurrentTexDat.RoughnessTextureScale = new Vector4(SharedMaterials[i].GetTextureScale(RelevantMat.RoughnessTex).x, SharedMaterials[i].GetTextureScale(RelevantMat.RoughnessTex).y, SharedMaterials[i].GetTextureOffset(RelevantMat.RoughnessTex).x, SharedMaterials[i].GetTextureOffset(RelevantMat.RoughnessTex).y);
                        if (RoughnessTexs.Contains(SharedMaterials[i].GetTexture(RelevantMat.RoughnessTex)))
                        {
                            RoughnessIndexes[RoughnessTexs.IndexOf(SharedMaterials[i].GetTexture(RelevantMat.RoughnessTex))].RayObjectList.Add(TempObj);
                        }
                        else
                        {
                            RoughnessIndexes.Add(new RayObjects());
                            RoughnessIndexes[RoughnessIndexes.Count - 1].RayObjectList.Add(TempObj);
                            RoughnessTexs.Add(SharedMaterials[i].GetTexture(RelevantMat.RoughnessTex));
                            RoughnessTexChannelIndex.Add(RelevantMat.RoughnessTexChannel);
                        }
                    }

                    MatTexData.Add(CurrentTexDat);

                }
            }
            RayTracingObject PreviousObject = MatTexData[0].MaterialObject;
            int CurrentObjectOffset = -1;
            int CurMat = 0;
            foreach (PerMatTextureData Obj in MatTexData)
            {
                if (PreviousObject.Equals(Obj.MaterialObject))
                {
                    CurrentObjectOffset++;
                }
                else
                {
                    CurrentObjectOffset = 0;
                }
                RayTracingObject MaterialObject = Obj.MaterialObject;
                MaterialObject.JustCreated = MaterialObject.JustCreated || MaterialObject.FollowMaterial[CurrentObjectOffset];
                if(Obj.MetallicRange != -1 && MaterialObject.JustCreated) {
                    MaterialObject.Metallic[CurrentObjectOffset] = Obj.MetallicRange;
                }
                if(Obj.IsCutout) {
                    MaterialObject.MaterialOptions[CurrentObjectOffset] = RayTracingObject.Options.Cutout;
                }
                if(Obj.RoughnessRange != -1 && MaterialObject.JustCreated) {
                    MaterialObject.Roughness[CurrentObjectOffset] = Obj.RoughnessRange;
                }
                if(MaterialObject.SpecTrans[CurrentObjectOffset] == 0 && Obj.SpecTransRange != -1 && MaterialObject.JustCreated) {
                    MaterialObject.SpecTrans[CurrentObjectOffset] = Obj.SpecTransRange;
                    MaterialObject.IOR[CurrentObjectOffset] = Obj.IORRange;
                }
                if(MaterialObject.JustCreated && MaterialObject.BaseColor[CurrentObjectOffset].x == 1 && MaterialObject.BaseColor[CurrentObjectOffset].y == 1 && MaterialObject.BaseColor[CurrentObjectOffset].z == 1) {
                   MaterialObject.BaseColor[CurrentObjectOffset] = new Vector3(Obj.BaseColorValue.r, Obj.BaseColorValue.g, Obj.BaseColorValue.b);
                }
                if(MaterialObject.IOR[CurrentObjectOffset] == 0) {
                    MaterialObject.IOR[CurrentObjectOffset] = 1;
                }
                if(MaterialObject.JustCreated) MaterialObject.emmission[CurrentObjectOffset] = (Obj.HasEmissiveMap) ? 12 : 0;
                 if(CurrentObjectOffset == MaterialObject.BaseColor.Length - 1) MaterialObject.JustCreated = false;
                _Materials.Add(new MaterialData()
                {
                    BaseColor = MaterialObject.BaseColor[CurrentObjectOffset],
                    emmissive = MaterialObject.emmission[CurrentObjectOffset],
                    Roughness = MaterialObject.Roughness[CurrentObjectOffset],
                    MatType = (int)MaterialObject.MaterialOptions[CurrentObjectOffset],
                    EmissionColor = MaterialObject.EmissionColor[CurrentObjectOffset],
                    specTrans = MaterialObject.SpecTrans[CurrentObjectOffset],
                    AlbedoTextureScale = Obj.AlbedoTextureScale,
                    NormalTextureScale = Obj.NormalTextureScale,
                    MetallicTextureScale = Obj.MetallicTextureScale,
                    RoughnessTextureScale = Obj.RoughnessTextureScale,
                    EmissiveTextureScale = Obj.EmissiveTextureScale,
                });
                Obj.MaterialObject.Indexes[CurrentObjectOffset] = Obj.Offset;
                Obj.MaterialObject.MaterialIndex[CurrentObjectOffset] = CurMat;
                Obj.MaterialObject.LocalMaterialIndex[CurrentObjectOffset] = CurMat;
                PreviousObject = Obj.MaterialObject;
                CurMat++;
            }
            MatTexData.Clear();
        }

        public void LoadData()
        {
            NeedsToResetBuffers = true;
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
            if (this.gameObject.GetComponent<MeshFilter>() == null) for (int i = 0; i < this.transform.childCount; i++) if (this.transform.GetChild(i).gameObject.GetComponent<SkinnedMeshRenderer>() != null && this.transform.GetChild(i).gameObject.GetComponent<ParentObject>() == null && this.transform.GetChild(i).gameObject.activeInHierarchy) { IsSkinnedGroup = true; break; }

            if (!IsSkinnedGroup)
            {
                for (int i = 0; i < this.transform.childCount; i++)
                {
                    if (this.transform.GetChild(i).gameObject.GetComponent<RayTracingObject>() != null && this.transform.GetChild(i).gameObject.activeInHierarchy && this.transform.GetChild(i).gameObject.GetComponent<ParentObject>() == null)
                    {
                        this.transform.GetChild(i).gameObject.GetComponent<RayTracingObject>().matfill();
                        if(this.transform.GetChild(i).gameObject.GetComponent<RayTracingObject>() != null) {
                            TempObjectTransforms.Add(this.transform.GetChild(i));
                            TempObjects.Add(this.transform.GetChild(i).gameObject.GetComponent<RayTracingObject>());
                        }
                    }
                }
            }
            else
            {
                var TempObjects2 = GetComponentsInChildren<RayTracingObject>();
                TempObjects = new List<RayTracingObject>(TempObjects2);
                int TempObjectCount = TempObjects.Count;
                for (int i = TempObjectCount - 1; i >= 0; i--)
                {
                    if (TempObjects[i].gameObject.GetComponent<SkinnedMeshRenderer>() != null && TempObjects[i].gameObject.activeInHierarchy)
                    {
                        TempObjectTransforms.Add(TempObjects[i].gameObject.transform);
                    }
                    else
                    {
                        TempObjects.RemoveAt(i);
                    }
                }
            }
            if (this.gameObject.GetComponent<SkinnedMeshRenderer>() != null) IsSkinnedGroup = true;
            if (this.gameObject.GetComponent<RayTracingObject>() != null)
            {
                if (TempObjects == null) TempObjects = new List<RayTracingObject>();
                this.gameObject.GetComponent<RayTracingObject>().matfill();
                if(this.gameObject.GetComponent<RayTracingObject>() != null) {
                    TempObjects.Add(this.gameObject.GetComponent<RayTracingObject>());
                    TempObjectTransforms.Add(this.transform);
                }
            }
            if (TempObjects == null || TempObjects.Count == 0)
            {
                Debug.Log("NO RAYTRACINGOBJECT CHILDREN AT GAMEOBJECT: " + Name);
                DestroyImmediate(this);
                return;
            }

            Transform[] TempTransforms = TempObjectTransforms.ToArray();
            CachedTransforms = new StorableTransform[TempTransforms.Length];
            for (int i = 0; i < TempTransforms.Length; i++)
            {
                CachedTransforms[i].WTL = TempTransforms[i].worldToLocalMatrix;
                CachedTransforms[i].Position = TempTransforms[i].position;
            }
            TempObjectTransforms.Clear();
            TempObjectTransforms.Capacity = 0;
            ChildObjects = TempObjects.ToArray();
            TempObjects.Clear();
            TempObjects.Capacity = 0;
            TotalObjects = ChildObjects.Length;
            if (IsSkinnedGroup)
            {
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
            #if HardwareRT
                Renderers = new Renderer[TotalObjects];
            #endif
            for (int i = 0; i < TotalObjects; i++)
            {
                CurrentObject = ChildObjects[i];
                mesh = new Mesh();
                if (CurrentObject.GetComponent<MeshFilter>() != null)
                    mesh = CurrentObject.GetComponent<MeshFilter>().sharedMesh;
                else
                    mesh = CurrentObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;

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
                for (int i2 = 0; i2 < submeshcount; ++i2)
                {//Add together all the submeshes in the mesh to consider it as one object
                    int IndiceLength = (int)mesh.GetIndexCount(i2) / 3;
                    MatIndex = i2 + RepCount;
                    TotalIndexLength += IndiceLength;
                    var SubMesh = new int[IndiceLength];
                    System.Array.Fill(SubMesh, MatIndex);
                    CurMeshData.MatDat.AddRange(SubMesh);
                }
                if (IsSkinnedGroup)
                {
                    IndexCounts[i] = TotalIndexLength;
                    SkinnedMeshes[i] = ChildObjects[i].GetComponent<SkinnedMeshRenderer>();
                    SkinnedMeshes[i].updateWhenOffscreen = true;
                    TotalTriangles += TotalIndexLength;
                }
                TransformIndexes.Add(new MeshTransformVertexs()
                {
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

        unsafe public void DocumentNodes(int CurrentNode, int ParentNode, int NextNode, int NextBVH8Node, bool IsLeafRecur, int CurRecur)
        {
            NodeIndexPairData CurrentPair = NodePair[CurrentNode];
            MaxRecur = Mathf.Max(MaxRecur, CurRecur);
            CurrentPair.PreviousNode = ParentNode;
            CurrentPair.Node = CurrentNode;
            CurrentPair.RecursionCount = CurRecur;
            if (!IsLeafRecur)
            {
                ToBVHIndex[NextBVH8Node] = CurrentNode;
                CurrentPair.IsLeaf = 0;
                BVHNode8Data node = BVH.BVH8Nodes[NextBVH8Node];
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



        public struct TriangleData
        {
            public Vector3 V1, V2, V3;
            public Vector3 N1, N2, N3;
        }
        public TriangleData[] Tris1;
        public TriangleData[] Tris2;

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
                DocumentNodes(0, 0, 1, 0, false, 0);
                MaxRecur++;
                int NodeCount = NodePair.Count;
                ForwardStack = new Layer[NodeCount];
                for (int i = 0; i < NodePair.Count; i++)
                {
                    for (int i2 = 0; i2 < 8; i2++)
                    {
                        ForwardStack[i].Children[i2] = -1;
                        ForwardStack[i].Leaf[i2] = -1;
                    }
                }

                for (int i = 0; i < NodePair.Count; i++)
                {
                    if (NodePair[i].IsLeaf == 1)
                    {
                        int first_triangle = (byte)BVH.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] & 0b11111;
                        int NumBits = NumberOfSetBits((byte)BVH.BVH8Nodes[NodePair[i].BVHNode].meta[NodePair[i].InNodeOffset] >> 5);
                        ForwardStack[i].Children[NodePair[i].InNodeOffset] = NumBits + (int)BVH.BVH8Nodes[NodePair[i].BVHNode].base_index_triangle + first_triangle;
                        ForwardStack[i].Leaf[NodePair[i].InNodeOffset] = (int)BVH.BVH8Nodes[NodePair[i].BVHNode].base_index_triangle + first_triangle + 1;
                    }
                    else
                    {
                        ForwardStack[i].Children[NodePair[i].InNodeOffset] = i;
                        ForwardStack[i].Leaf[NodePair[i].InNodeOffset] = 0;
                    }
                    ForwardStack[NodePair[i].PreviousNode].Children[NodePair[i].InNodeOffset] = i;
                    ForwardStack[NodePair[i].PreviousNode].Leaf[NodePair[i].InNodeOffset] = 0;
                }

                LayerStack = new Layer2[MaxRecur];
                for (int i = 0; i < MaxRecur; i++)
                {
                    Layer2 TempSlab = new Layer2();
                    TempSlab.Slab = new List<int>();
                    LayerStack[i] = TempSlab;
                }
                for (int i = 0; i < NodePair.Count; i++)
                {
                    var TempLayer = LayerStack[NodePair[i].RecursionCount];
                    TempLayer.Slab.Add(i);
                    LayerStack[NodePair[i].RecursionCount] = TempLayer;
                }
                ConvertToSplitNodes();
            }
            for(int i = 0; i < LightTriangles.Count; i++) {
                var A = LightTriangles[i];
                A.TriIndex = CWBVHIndicesBufferInverted[A.TriIndex];
                LightTriangles[i] = A;
            }
        }

        unsafe private void ConvertToSplitNodes()
        {
            BVHNode8DataFixed NewNode = new BVHNode8DataFixed();
            SplitNodes = new List<BVHNode8DataFixed>();
            BVHNode8Data SourceNode;
            for (int i = 0; i < BVH.BVH8Nodes.Length; i++)
            {
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


        public void RefitMesh(ref ComputeBuffer RealizedAggNodes, ref ComputeBuffer RealizedTriBuffer, CommandBuffer cmd)
        {
            #if HardwareRT
                for(int i = 0; i < Renderers.Length; i++) {
                    Assets.AccelStruct.UpdateInstanceTransform(Renderers[i]);
                }
            #endif
            int KernelRatio = 256;

            AABB OverAABB = new AABB();
            tempAABB = new AABB();
            OverAABB.init();
            for (int i = 0; i < SkinnedMeshes.Length; i++)
            {
                Vector3 Scale = new Vector3(1, 1, 1);
                tempAABB.BBMax = SkinnedMeshes[i].bounds.center + Vector3.Scale(SkinnedMeshes[i].bounds.size, Scale) / 2.0f;
                tempAABB.BBMin = SkinnedMeshes[i].bounds.center - Vector3.Scale(SkinnedMeshes[i].bounds.size, Scale) / 2.0f;
                OverAABB.Extend(ref tempAABB);
            }

            aabb = OverAABB;
            if (!HasStarted)
            {
                UnityEngine.Profiling.Profiler.BeginSample("ReMesh Init");
                VertexBuffers = new GraphicsBuffer[SkinnedMeshes.Length];
                IndexBuffers = new ComputeBuffer[SkinnedMeshes.Length];
                NodeBuffer = new ComputeBuffer(NodePair.Count, 48);
                NodeBuffer.SetData(NodePair);
                AdvancedTriangleBuffer = new ComputeBuffer(TotalTriangles, 24);
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
                if (Hips == null && this.transform.childCount != 0 && this.transform.GetChild(0).childCount != 0 && this.transform.GetChild(0).GetChild(0) != null) Hips = this.transform.GetChild(0).GetChild(0);
                int MaxLength = 0;
                WorkingBuffer = new ComputeBuffer[LayerStack.Length];//(MaxLength, 4);
                for (int i = 0; i < LayerStack.Length; i++)
                {
                    WorkingBuffer[i] = new ComputeBuffer(LayerStack[i].Slab.Count, 4);
                    WorkingBuffer[i].SetData(LayerStack[i].Slab);
                    MaxLength = Mathf.Max(MaxLength, LayerStack[i].Slab.Count);
                }
                for (int i = 0; i < VertexBuffers.Length; i++)
                {
                    if (IndexBuffers[i] != null) IndexBuffers[i].Release();
                    int[] IndexBuffer = SkinnedMeshes[i].sharedMesh.triangles;
                    IndexBuffers[i] = new ComputeBuffer(IndexBuffer.Length, 4, ComputeBufferType.Raw);
                    IndexBuffers[i].SetData(IndexBuffer);
                }
                UnityEngine.Profiling.Profiler.EndSample();
            }
            else if (AllFull)
            {
                for (int i = 0; i < VertexBuffers.Length; i++)
                {
                    VertexBuffers[i].Dispose();
                    SkinnedMeshes[i].vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                    VertexBuffers[i] = SkinnedMeshes[i].GetVertexBuffer();
                }
                if (Hips == null) Hips = SkinnedMeshes[0].rootBone;//transform;
                if(Hips == null) return;
                UnityEngine.Profiling.Profiler.BeginSample("ReMesh Fill");
                cmd.SetComputeMatrixParam(MeshRefit, "Transform2", this.transform.localToWorldMatrix);
                cmd.SetComputeMatrixParam(MeshRefit, "Transform3", this.transform.worldToLocalMatrix);
                cmd.SetComputeVectorParam(MeshRefit, "Scale", this.transform.lossyScale);
                cmd.SetComputeVectorParam(MeshRefit, "OverallOffset", Hips.parent.localToWorldMatrix * Hips.localPosition);
                cmd.SetComputeVectorParam(MeshRefit, "ArmetureOffset", this.transform.localToWorldMatrix * Hips.parent.localPosition);
                cmd.SetComputeBufferParam(MeshRefit, RefitLayerKernel, "ReverseStack", StackBuffer);
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "AdvancedTriangles", AdvancedTriangleBuffer);
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "CudaTriArrayIN", TriBuffer);
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "CudaTriArray", RealizedTriBuffer);
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "CWBVHIndices", CWBVHIndicesBuffer);
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "AdvancedTriangles", AdvancedTriangleBuffer);
                cmd.SetComputeBufferParam(MeshRefit, NodeInitializerKernel, "AllNodes", NodeBuffer);
                cmd.SetComputeBufferParam(MeshRefit, RefitLayerKernel, "AllNodes", NodeBuffer);
                UnityEngine.Profiling.Profiler.EndSample();
                int CurVertOffset = 0;
                UnityEngine.Profiling.Profiler.BeginSample("ReMesh Aggregate");
                for (int i = 0; i < TotalObjects; i++)
                {
                    var SkinnedRootBone = SkinnedMeshes[i].rootBone;
                    int IndexCount = IndexCounts[i];
                    cmd.SetComputeIntParam(MeshRefit, "VertOffset", CurVertOffset);
                    cmd.SetComputeIntParam(MeshRefit, "gVertexCount", IndexCount);
                    cmd.SetComputeMatrixParam(MeshRefit, "Transform", SkinnedRootBone.worldToLocalMatrix);
                    cmd.SetComputeVectorParam(MeshRefit, "ArmetureScale", SkinnedRootBone.parent.lossyScale);
                    cmd.SetComputeVectorParam(MeshRefit, "Offset", SkinnedRootBone.position - Hips.position);
                    cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "bufVertices", VertexBuffers[i]);
                    cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "bufIndexes", IndexBuffers[i]);
                    cmd.DispatchCompute(MeshRefit, ConstructKernel, (int)Mathf.Ceil(IndexCount / (float)KernelRatio), 1, 1);
                    CurVertOffset += IndexCount;
                }
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("ReMesh NodeInit");
                cmd.SetComputeIntParam(MeshRefit, "NodeCount", NodePair.Count);
                cmd.DispatchCompute(MeshRefit, NodeInitializerKernel, (int)Mathf.Ceil(NodePair.Count / (float)KernelRatio), 1, 1);
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("ReMesh Layer Solve");
                cmd.SetComputeBufferParam(MeshRefit, RefitLayerKernel, "AdvancedTriangles", AdvancedTriangleBuffer);
                for (int i = MaxRecur - 1; i >= 0; i--)
                {
                    var NodeCount2 = LayerStack[i].Slab.Count;
                    cmd.SetComputeIntParam(MeshRefit, "NodeCount", NodeCount2);
                    cmd.SetComputeBufferParam(MeshRefit, RefitLayerKernel, "NodesToWork", WorkingBuffer[i]);
                    cmd.DispatchCompute(MeshRefit, RefitLayerKernel, (int)Mathf.Ceil(WorkingBuffer[i].count / (float)KernelRatio), 1, 1);
                }
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("ReMesh Solve");
                cmd.SetComputeIntParam(MeshRefit, "NodeCount", NodePair.Count);
                cmd.SetComputeBufferParam(MeshRefit, NodeUpdateKernel, "AllNodes", NodeBuffer);
                cmd.SetComputeBufferParam(MeshRefit, NodeUpdateKernel, "BVHNodes", BVHDataBuffer);
                cmd.SetComputeBufferParam(MeshRefit, NodeUpdateKernel, "ToBVHIndex", ToBVHIndexBuffer);
                cmd.DispatchCompute(MeshRefit, NodeUpdateKernel, (int)Mathf.Ceil(NodePair.Count / (float)KernelRatio), 1, 1);
                UnityEngine.Profiling.Profiler.EndSample();

                UnityEngine.Profiling.Profiler.BeginSample("ReMesh Compress");
                cmd.SetComputeIntParam(MeshRefit, "NodeCount", BVH.BVH8Nodes.Length);
                cmd.SetComputeIntParam(MeshRefit, "NodeOffset", NodeOffset);
                cmd.SetComputeBufferParam(MeshRefit, NodeCompressKernel, "BVHNodes", BVHDataBuffer);
                cmd.SetComputeBufferParam(MeshRefit, NodeCompressKernel, "AggNodes", RealizedAggNodes);
                cmd.DispatchCompute(MeshRefit, NodeCompressKernel, (int)Mathf.Ceil(NodePair.Count / (float)KernelRatio), 1, 1);
                UnityEngine.Profiling.Profiler.EndSample();
            }

            UnityEngine.Profiling.Profiler.BeginSample("ReMesh Buffer Refresh");
            if (!AllFull)
            {
                for (int i = 0; i < VertexBuffers.Length; i++)
                {
                    SkinnedMeshes[i].vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                    VertexBuffers[i] = SkinnedMeshes[i].GetVertexBuffer();
                }
                if (!((new List<GraphicsBuffer>(VertexBuffers)).Contains(null))) AllFull = true;
            }
            UnityEngine.Profiling.Profiler.EndSample();
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
        public Vector2 CalculateTriangleWidthAndHeight(Vector3 pointA, Vector3 pointB, Vector3 pointC)
        {
            // Get the vector from point A to point B
            Vector3 sideAB = pointB - pointA;
         
            // Get the vector from point B to point C
            Vector3 sideBC = pointC - pointB;
         
            // Get the vector from point A to point C
            Vector3 sideAC = pointC - pointA;
         
            // Calculate the width of the triangle by taking the magnitude of the cross product of vectors AB and AC
            float width = Vector3.Cross(sideAB, sideAC).magnitude;
         
            // Calculate the height of the triangle by taking the magnitude of the cross product of vectors AB and BC
            float height = Vector3.Cross(sideAB, sideBC).magnitude;
         
            return new Vector2(width, height);
        }

        public async Task BuildTotal()
        {
            Matrix4x4 ParentMat = CachedTransforms[0].WTL.inverse;
            Matrix4x4 ParentMatInv = CachedTransforms[0].WTL;
            Vector3 V1, V2, V3, Norm1, Norm2, Norm3, Tan1, Tan2, Tan3;
            PrimitiveData TempPrim = new PrimitiveData();
            float TotalEnergy = 0.0f;
            Triangles = new AABB[(TransformIndexes[TransformIndexes.Count - 1].VertexStart + TransformIndexes[TransformIndexes.Count - 1].VertexCount) / 3];
            AggTriangles = new CudaTriangle[(TransformIndexes[TransformIndexes.Count - 1].VertexStart + TransformIndexes[TransformIndexes.Count - 1].VertexCount) / 3];
            CudaTriangle TempTri = new CudaTriangle();
            int IllumTriCount = 0;
            for (int i = 0; i < TotalObjects; i++)
            {//Transforming so all child objects are in the same object space
                Matrix4x4 ChildMat = CachedTransforms[i + 1].WTL.inverse;
                Matrix4x4 TransMat = ParentMatInv * ChildMat;
                Vector3 Ofst = CachedTransforms[i + 1].WTL * CachedTransforms[i + 1].Position;
                Vector3 Ofst2 = ParentMatInv * CachedTransforms[0].Position;
                int IndexOffset = TransformIndexes[i].IndexOffset;
                for (int i3 = TransformIndexes[i].VertexStart; i3 < TransformIndexes[i].VertexStart + TransformIndexes[i].VertexCount; i3 += 3)
                {//Transforming child meshes into the space of their parent
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

                    TempTri.norm0 = CommonFunctions.PackOctahedral(TempPrim.Norm1);
                    TempTri.norm1 = CommonFunctions.PackOctahedral(TempPrim.Norm2);
                    TempTri.norm2 = CommonFunctions.PackOctahedral(TempPrim.Norm3);

                    TempTri.tan0 = CommonFunctions.PackOctahedral(TempPrim.Tan1);
                    TempTri.tan1 = CommonFunctions.PackOctahedral(TempPrim.Tan2);
                    TempTri.tan2 = CommonFunctions.PackOctahedral(TempPrim.Tan3);

                    TempTri.tex0 = TempPrim.tex1;
                    TempTri.texedge1 = TempPrim.tex2;
                    TempTri.texedge2 = TempPrim.tex3;

                    TempTri.MatDat = (uint)TempPrim.MatDat;
                    AggTriangles[i3 / 3] = TempTri;
                    Triangles[i3 / 3] = TempPrim.aabb;
                    #if DoLightMapping
                        LightMapTriangle TempLightMapTri = new LightMapTriangle();
                        
                        TempLightMapTri.pos0 = ParentMat * TempPrim.V1;

                        TempLightMapTri.posedge1 = ParentMat * TempPrim.V2 - ParentMat * TempPrim.V1;
                        TempLightMapTri.posedge2 = ParentMat * TempPrim.V3 - ParentMat * TempPrim.V1;
                        
                        TempLightMapTri.norm = ParentMat.MultiplyVector((TempPrim.Norm1 + TempPrim.Norm2 + TempPrim.Norm3).normalized);

                        TempLightMapTri.UV0 = CurMeshData.LightMapUvs[Index1];
                        TempLightMapTri.UV1 = CurMeshData.LightMapUvs[Index2];
                        TempLightMapTri.UV2 = CurMeshData.LightMapUvs[Index3];

                        TempLightMapTri.LightMapIndex = CurMeshData.LightMapTexIndexes[Index3];
                        TempLightMapTri.WH = CalculateTriangleWidthAndHeight(TempLightMapTri.pos0, TempLightMapTri.pos0 + TempLightMapTri.posedge2, TempLightMapTri.pos0 + TempLightMapTri.posedge1);
                        LightMapTris.Add(TempLightMapTri);
                    #endif
                    if (_Materials[TempPrim.MatDat].emmissive > 0.0f)
                    {
                        V1 = TempPrim.V1;
                        V2 = TempPrim.V2;
                        V3 = TempPrim.V3;
                        Vector3 Radiance = _Materials[TempPrim.MatDat].emmissive * _Materials[TempPrim.MatDat].BaseColor;
                        float radiance = luminance(Radiance.x, Radiance.y, Radiance.z);
                        float area = AreaOfTriangle(ParentMat * V1, ParentMat * V2, ParentMat * V3);
                        float e = radiance * area;
                        if(System.Double.IsNaN(area)) continue;
                        TotalEnergy += area;
                        TotEnergy += area;

                        LightTriangles.Add(new CudaLightTriangle()
                        {
                            TriIndex = i3 / 3,
                            Norm = ((TempPrim.Norm1 + TempPrim.Norm2 + TempPrim.Norm3) / 3.0f),
                            UV1 = TempPrim.tex1,
                            UV2 = TempPrim.tex2,
                            UV3 = TempPrim.tex3,
                            MatIndex = TempPrim.MatDat,
                            radiance = Radiance,
                            sumEnergy = TotalEnergy,
                            energy = area,
                            area = area
                        });
                        IllumTriCount++;
                    }
                }
            }
            CurMeshData.Clear();
            LightTriangles.Sort((s1, s2) => s1.energy.CompareTo(s2.energy));
            TotalEnergy = 0.0f;
            int LightTriCount = LightTriangles.Count;
            CudaLightTriangle TempTri2;
            for (int i = 0; i < LightTriCount; i++)
            {
                TempTri2 = LightTriangles[i];
                TotalEnergy += TempTri2.energy;
                TempTri2.sumEnergy = TotalEnergy;
                LightTriangles[i] = TempTri2;
            }
            LightCount = LightTriangles.Count;
            #if !HardwareRT
                ConstructAABB();
                Construct();

                {//Compile Triangles
                        CudaTriangle[] Vector3Array = new CudaTriangle[AggTriangles.Length];
                        System.Array.Copy(AggTriangles, Vector3Array, Vector3Array.Length);
                        for (int i = 0; i < AggTriangles.Length; i++)
                        {
                            AggTriangles[i] = Vector3Array[BVH.cwbvh_indices[i]];
                        }
                        Vector3Array = null;
                }

                AggNodes = new BVHNode8DataCompressed[BVH.BVH8Nodes.Length];
                CommonFunctions.Aggregate(ref AggNodes, ref BVH.BVH8Nodes);
            #else 
                if(IsSkinnedGroup) {
                    ConstructAABB();
                    Construct();

                    {//Compile Triangles
                        #if !HardwareRT
                            CudaTriangle[] Vector3Array = new CudaTriangle[AggTriangles.Length];
                            System.Array.Copy(AggTriangles, Vector3Array, Vector3Array.Length);
                            for (int i = 0; i < AggTriangles.Length; i++)
                            {
                                AggTriangles[i] = Vector3Array[BVH.cwbvh_indices[i]];
                            }
                            Vector3Array = null;
                        #endif
                    }

                    AggNodes = new BVHNode8DataCompressed[BVH.BVH8Nodes.Length];
                    CommonFunctions.Aggregate(ref AggNodes, ref BVH.BVH8Nodes);
                }else AggNodes = new BVHNode8DataCompressed[1];
            #endif
            MeshCountChanged = false;
            HasCompleted = true;
            NeedsToUpdate = false;

            Debug.Log(Name + " Has Completed Building with " + AggTriangles.Length + " triangles");
        }


        public void UpdateData()
        {
            #if !HardwareRT
                AggIndexCount = BVH.cwbvhindex_count;
                AggBVHNodeCount = BVH.cwbvhnode_count;
            #else 
                AggIndexCount = 1;
                AggBVHNodeCount = 1;
            #endif
            UpdateAABB(this.transform);
            SetUpBuffers();
            NeedsToResetBuffers = false;
        }
        public Vector3 center;
        public Vector3 extent;

        public void UpdateAABB(Transform transform)
        {//Update the Transformed AABB by getting the new Max/Min of the untransformed AABB after transforming it
            #if HardwareRT
                for(int i = 0; i < Renderers.Length; i++) {
                    Assets.AccelStruct.UpdateInstanceTransform(Renderers[i]);
                }
            #else
                Matrix4x4 Mat = transform.localToWorldMatrix;
                Vector3 new_center = CommonFunctions.transform_position(Mat, center);
                Vector3 new_extent = CommonFunctions.transform_direction(Mat, extent);

                aabb.BBMin = new_center - new_extent;
                aabb.BBMax = new_center + new_extent;
            #endif    
            transform.hasChanged = false;
        }

        private void ConstructAABB()
        {
            aabb_untransformed = new AABB();
            aabb_untransformed.init();
            for (int i = 0; i < Triangles.Length; i++)
            {
                aabb_untransformed.Extend(ref Triangles[i]);
            }
            center = 0.5f * (aabb_untransformed.BBMin + aabb_untransformed.BBMax);
            extent = 0.5f * (aabb_untransformed.BBMax - aabb_untransformed.BBMin);
        }

        private void OnEnable()
        {
            HasStarted = false;
            if (gameObject.scene.isLoaded)
            {
                if (this.GetComponentInParent<InstancedManager>() != null)
                {
                    this.GetComponentInParent<InstancedManager>().AddQue.Add(this);
                    this.GetComponentInParent<InstancedManager>().ParentCountHasChanged = true;
                }
                else
                {
                    this.GetComponentInParent<AssetManager>().AddQue.Add(this);
                    ExistsInQue = 3;
                    this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
                }
                HasCompleted = false;
            }
        }

        private void OnDisable()
        {
            HasStarted = false;
            if (gameObject.scene.isLoaded)
            {
                ClearAll();
                if (this.GetComponentInParent<InstancedManager>() != null)
                {
                    this.GetComponentInParent<InstancedManager>().RemoveQue.Add(this);
                    this.GetComponentInParent<InstancedManager>().ParentCountHasChanged = true;
                }
                else
                {
                    Assets.RemoveQue.Add(this);
                    Assets.ParentCountHasChanged = true;
                }
                HasCompleted = false;
            }
        }
    //     unsafe void RecurseDown(Vector3 ThisPoint, int NextBVH8Node, bool IsLeafRecur, int PrevBVH8Node)
    //     {
    //         if (!IsLeafRecur)
    //         {
    //             BVHNode8Data node = BVH.BVH8Nodes[NextBVH8Node];


    //             // Convert the combined uint back to a float vector
    //             Vector3 e = new Vector3(
    //                 System.BitConverter.ToSingle(System.BitConverter.GetBytes(node.e[0] << 23), 0),
    //                 System.BitConverter.ToSingle(System.BitConverter.GetBytes(node.e[1] << 23), 0),
    //                 System.BitConverter.ToSingle(System.BitConverter.GetBytes(node.e[2] << 23), 0)
    //             );

    //             for (int i = 0; i < 8; i++)
    //             {
    //                 float AABBPos1x = node.quantized_max_x[i] * e.x + node.p.x;
    //                 float AABBPos1y = node.quantized_max_y[i] * e.y + node.p.y;
    //                 float AABBPos1z = node.quantized_max_z[i] * e.z + node.p.z;
    //                 float AABBPos2x = node.quantized_min_x[i] * e.x + node.p.x;
    //                 float AABBPos2y = node.quantized_min_y[i] * e.y + node.p.y;
    //                 float AABBPos2z = node.quantized_min_z[i] * e.z + node.p.z;
    //                 bool IsLeaf = (node.meta[i] & 0b11111) < 24;
    //                 // Debug.Log(new Vector3(AABBPos1x, AABBPos1y, AABBPos1z) + ", " + new Vector3(AABBPos2x, AABBPos2y, AABBPos2z));
    //                 if(ThisPoint.x >= AABBPos2x && ThisPoint.y >= AABBPos2y && ThisPoint.z >= AABBPos2z && ThisPoint.x <= AABBPos1x && ThisPoint.y <= AABBPos1y && ThisPoint.z <= AABBPos1z) {
    //                     Gizmos.DrawWireCube((new Vector3(AABBPos1x, AABBPos1y, AABBPos1z) + new Vector3(AABBPos2x, AABBPos2y, AABBPos2z)) / 2.0f, (new Vector3(AABBPos1x, AABBPos1y, AABBPos1z) - new Vector3(AABBPos2x, AABBPos2y, AABBPos2z)));
    //                     if (IsLeaf)
    //                     {
    //                         Debug.Log(node.meta[i]  & 0b11111);
    //                         if(node.meta[i]  & 0b11111 < 23) {
    //                             List<TriangleData> Tris = new List<TriangleData>(Triangles);
    //                             Tris.Add()
    //                         }
    //                         RecurseDown(ThisPoint, -1, true, NextBVH8Node); 
    //                     }
    //                     else
    //                     {
    //                         Debug.Log()
    //                         int child_offset = (byte)node.meta[i] & 0b11111;
    //                         int child_index = (int)node.base_index_child + child_offset - 24;

    //                         RecurseDown(ThisPoint, child_index, false, NextBVH8Node);
    //                     }
    //                 }
    //             }
    //         }

    // }
                // void Start() {}
            // void OnDrawGizmos() {
            //     // Debug.Log("EEE");
            //     // RecurseDown(GameObject.Find("FoundPoint").transform.position, 0, false, 0);
            //     for(int i = 0; i < AggTriangles.Length; i++) {
            //         Vector3 V1 = this.transform.localToWorldMatrix * AggTriangles[i].pos0;
            //         Vector3 V2 = this.transform.localToWorldMatrix * (AggTriangles[i].pos0 + AggTriangles[i].posedge1);
            //         Vector3 V3 = this.transform.localToWorldMatrix * (AggTriangles[i].pos0 + AggTriangles[i].posedge2);
            //         Gizmos.DrawLine(V1, V2);
            //         Gizmos.DrawLine(V1, V3);
            //         Gizmos.DrawLine(V3, V2);

            //     }
            // }



        }






}