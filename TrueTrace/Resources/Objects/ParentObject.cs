using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

#pragma warning disable 1998



namespace TrueTrace {
    [System.Serializable]
    public class ParentObject : MonoBehaviour
    {

        public float Distance(Vector3 a, Vector3 b)
        {
            a.x -= b.x;
            a.y -= b.y;
            a.z -= b.z;
            return (float)Math.Sqrt(a.x * (double)a.x + a.y * (double)a.y + a.z * (double)a.z);
        }
        public static Vector3 Scale(Vector3 a, Vector3 b)
        {
            a.x *= b.x;
            a.y *= b.y;
            a.z *= b.z;
            return a;
        }

        public void Normalize(ref Vector3 a)
        {
            float num = (float)Math.Sqrt(a.x * (double)a.x + a.y * (double)a.y + a.z * (double)a.z);
            if (num > 9.99999974737875E-06)
            {
                float inversed = 1 / num;
                a.x *= inversed;
                a.y *= inversed;
                a.z *= inversed;
            }
            else
            {
                a.x = 0;
                a.y = 0;
                a.z = 0;
            }
        }



        public PerInstanceData[] InstanceDatas;
        public int[] RTAccelHandle;
        public int[] RTAccelSubmeshOffsets;
        public LightBVHBuilder LBVH;
        public Task AsyncTask;
        public int ExistsInQue = -1;
        public int QueInProgress = -1;
        public bool RenderImposters = false;
        public bool IsDeformable = false;
        [HideInInspector] public ComputeBuffer LightTriBuffer;
        [HideInInspector] public ComputeBuffer LightTreeBuffer;
        [HideInInspector] public ComputeBuffer TriBuffer;
        [HideInInspector] public ComputeBuffer BVHBuffer;
        public string Name;
        [HideInInspector] public GraphicsBuffer[] VertexBuffers;
        [HideInInspector] public ComputeBuffer[] IndexBuffers;
        [HideInInspector] public List<RayTracingObject> ChildObjects;
        [HideInInspector] public bool MeshCountChanged;
        private unsafe NativeArray<AABB> TrianglesArray;
        private unsafe AABB* Triangles;
        [HideInInspector] public CudaTriangle[] AggTriangles;
        [HideInInspector] public Vector3 ParentScale;
        [HideInInspector] public List<LightTriData> LightTriangles;
        [HideInInspector] private List<Vector3> LightTriNorms;//Test to see if I can get rrid of this alltogether and just calculate the normal based off cross...
        [HideInInspector] public BVH8Builder BVH;
        [HideInInspector] public SkinnedMeshRenderer[] SkinnedMeshes;
        [HideInInspector] public MeshFilter[] DeformableMeshes;
        [HideInInspector] public int[] IndexCounts;
        [HideInInspector] public ComputeShader MeshRefit;
        [HideInInspector] public bool HasStarted;
        [HideInInspector] public BVHNode8DataCompressed[] AggNodes;
        [HideInInspector] public int InstanceMeshIndex;
        [HideInInspector] public int LightEndIndex;
        [HideInInspector] public AABB aabb_untransformed;
        [HideInInspector] public AABB aabb;
        [HideInInspector] public bool AllFull;
        [HideInInspector] public int AggIndexCount;
        [HideInInspector] public int AggBVHNodeCount;
        [HideInInspector] public List<MaterialData> _Materials;
        [HideInInspector] public int MatOffset;
        [HideInInspector] public StorableTransform[] CachedTransforms;
        [HideInInspector] private MeshDat CurMeshData;
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
        [HideInInspector] public int LightBLASRefitKernel;

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
        private BVH2Builder BVH2;

        [HideInInspector] public Layer[] ForwardStack;
        [HideInInspector] public Layer2[] LayerStack;
        [HideInInspector] public List<NodeIndexPairData> NodePair;
        [HideInInspector] private List<float> LuminanceWeights;

        [HideInInspector] public int MaxRecur = 0;
        [HideInInspector] public int[] ToBVHIndex;

        [HideInInspector] public AABB tempAABB;

        public int NodeOffset;
        public int TriOffset;
        public int LightTriOffset;
        public int LightNodeOffset;
        [HideInInspector] public List<BVHNode8DataFixed> SplitNodes;

        #if HardwareRT
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
            public int IndexOffsetEnd;
        }

        public void CallUpdate() {
            if ((QueInProgress == 0 || QueInProgress == 1) && !AssetManager.Assets.UpdateQue.Contains(this)) AssetManager.Assets.UpdateQue.Add(this);
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
            CommonFunctions.DeepClean(ref LuminanceWeights);
            if(TrianglesArray.IsCreated) TrianglesArray.Dispose();
            if(BVH2 != null) {
                BVH2.Dispose();
            }
            BVH2 = null;
            if(BVH != null) {
                BVH.Dispose();
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
                LightTreeBuffer?.ReleaseSafe();
                TriBuffer?.Release();
                BVHBuffer?.Release();
            }
            if(LBVH != null) {
                LBVH.ClearAll();
            }
        }

        public void Reset(int Que) {
            ExistsInQue = Que;
            QueInProgress = Que;
            LoadData();
            AsyncTask = Task.Run(() => {BuildTotal();});
        }

        public void OnApplicationQuit()
        {
            if (VertexBuffers != null) {
                int Leng = 0;
                if(SkinnedMeshes != null) Leng = SkinnedMeshes.Length;
                if(DeformableMeshes != null) Leng = Mathf.Max(Leng, DeformableMeshes.Length);
                for (int i = 0; i < Leng; i++) {
                    if(VertexBuffers != null && VertexBuffers[i] != null) VertexBuffers[i].Release();
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
                LightTreeBuffer.ReleaseSafe();
                TriBuffer.Release();
                BVHBuffer.Release();
            }
            ClearAll();
        }


        public void init()
        {
            if(this == null || this.gameObject == null) return;
            this.gameObject.isStatic = false;
            {
                if (this.GetComponentInParent<InstancedManager>() != null) {
                    var Instances = FindObjectsOfType(typeof(InstancedObject)) as InstancedObject[];
                    int Count = Instances.Length;
                    for(int i = 0; i < Count; i++) if(Instances[i].InstanceParent == this) Instances[i].OnParentClear();
                }
            }
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
            ConstructKernel = MeshRefit.FindKernel("Construct");
            TransferKernel = MeshRefit.FindKernel("TransferKernel");
            RefitLayerKernel = MeshRefit.FindKernel("RefitLayer");
            NodeUpdateKernel = MeshRefit.FindKernel("NodeUpdate");
            NodeCompressKernel = MeshRefit.FindKernel("NodeCompress");
            NodeInitializerKernel = MeshRefit.FindKernel("NodeInitializer");
#if !DontUseSGTree
            LightBLASRefitKernel = MeshRefit.FindKernel("BLASSGTreeRefitKernel");
#else
            LightBLASRefitKernel = MeshRefit.FindKernel("BLASLightRefitKernel");
#endif
        }
        private bool NeedsToResetBuffers = true;
        public void SetUpBuffers()
        {
            if (NeedsToResetBuffers)
            {
                if (LightTriBuffer != null) LightTriBuffer.Release();
                if (TriBuffer != null) TriBuffer.Release();
                if (BVHBuffer != null) BVHBuffer.Release();
                if (LightTreeBuffer != null) LightTreeBuffer.Release();
                  if(AggTriangles != null) {
                    if(LightTriangles.Count == 0) {
                        LightTriBuffer = new ComputeBuffer(1, CommonFunctions.GetStride<LightTriData>());
#if !DontUseSGTree
                        LightTreeBuffer = new ComputeBuffer(1, CommonFunctions.GetStride<GaussianTreeNode>());
#else
                        LightTreeBuffer = new ComputeBuffer(1, CommonFunctions.GetStride<CompactLightBVHData>());
#endif
                    } else {
                        LightTriBuffer = new ComputeBuffer(Mathf.Max(LightTriangles.Count,1), CommonFunctions.GetStride<LightTriData>());
#if !DontUseSGTree
                        LightTreeBuffer = new ComputeBuffer(Mathf.Max(LBVH.SGTree.Length,1), CommonFunctions.GetStride<GaussianTreeNode>());
#else
                        LightTreeBuffer = new ComputeBuffer(Mathf.Max(LBVH.nodes.Length,1), CommonFunctions.GetStride<CompactLightBVHData>());
#endif
                    }
                    TriBuffer = new ComputeBuffer(AggTriangles.Length, CommonFunctions.GetStride<CudaTriangle>());
                    BVHBuffer = new ComputeBuffer(AggNodes.Length, 80);
                    if(HasLightTriangles) {
                        LightTriBuffer.SetData(LightTriangles);
#if !DontUseSGTree
                        LightTreeBuffer.SetData(LBVH.SGTree);
#else
                        LightTreeBuffer.SetData(LBVH.nodes);
#endif

                    }
                    TriBuffer.SetData(AggTriangles);
                    BVHBuffer.SetData(AggNodes);
                }
            }
        }

        public List<Texture> AlbedoTexs;
        public List<Texture> NormalTexs;
        public List<Texture> SecondaryNormalTexs;
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
        public List<Texture> SecondaryAlbedoTexMasks;
        public List<int> SecondaryAlbedoTexMaskChannelIndex;
        public List<Texture> SecondaryAlbedoTexs;


        #if AccurateLightTris
            List<Color[]> EmissionTexPixels;
            List<Vector2> EmissionTexWidthHeight;
        #endif

        private void TextureParseScaleOffset(Material Mat, string TexName, ref Vector4 ScaleOffset) {
            if (Mat.HasProperty(TexName) && Mat.GetTexture(TexName) != null) {
                Vector2 Offset = Mat.GetTextureOffset(TexName);
                Vector2 Scale = Mat.GetTextureScale(TexName);
                ScaleOffset = new Vector4(Scale.x, Scale.y, Offset.x, Offset.y);
            }
        }
        
        private void TextureParseScale(Material Mat, string TexName, ref Vector2 Scale) {
            if (Mat.HasProperty(TexName) && Mat.GetTexture(TexName) != null) {
                Scale = Mat.GetTextureScale(TexName);
            }
        }

        private void TextureParseOffset(Material Mat, string TexName, ref Vector2 Offset) {
            if (Mat.HasProperty(TexName) && Mat.GetTexture(TexName) != null) {
                Offset = Mat.GetTextureOffset(TexName);
            }
        }

        private int TextureParse(ref Vector4 RefMat, Material Mat, string TexName, ref List<Texture> Texs, ref int TextureIndex, bool IsEmission = false) {
            TextureIndex = 0;
            if (Mat.HasProperty(TexName) && Mat.GetTexture(TexName) != null) {
                if(RefMat.x == 0) RefMat = new Vector4(Mat.GetTextureScale(TexName).x, Mat.GetTextureScale(TexName).y, Mat.GetTextureOffset(TexName).x, Mat.GetTextureOffset(TexName).y);
                Texture Tex = Mat.GetTexture(TexName);
                TextureIndex = Texs.IndexOf(Tex) + 1;
                if (TextureIndex != 0) {
                    return 0;
                } else {
                    #if AccurateLightTris
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

        private void MemorySafeClear<T>(ref List<T> TargList) {
            if(TargList != null) TargList.Clear();
            else TargList = new List<T>();
        }

        public void CreateAtlas(ref int VertCount)
        {//Creates texture atlas
            #if AccurateLightTris
                EmissionTexPixels = new List<Color[]>();
                EmissionTexWidthHeight = new List<Vector2>();
            #endif
            _Materials.Clear();
            MemorySafeClear<Texture>(ref AlbedoTexs);
            MemorySafeClear<Texture>(ref NormalTexs);
            MemorySafeClear<Texture>(ref SecondaryNormalTexs);
            MemorySafeClear<Texture>(ref MetallicTexs);
            MemorySafeClear<Texture>(ref RoughnessTexs);
            MemorySafeClear<Texture>(ref EmissionTexs);
            MemorySafeClear<Texture>(ref AlphaTexs);
            MemorySafeClear<Texture>(ref MatCapTexs);
            MemorySafeClear<Texture>(ref MatCapMasks);
            MemorySafeClear<Texture>(ref SecondaryAlbedoTexs);
            MemorySafeClear<Texture>(ref SecondaryAlbedoTexMasks);
            MemorySafeClear<int>(ref RoughnessTexChannelIndex);
            MemorySafeClear<int>(ref MetallicTexChannelIndex);
            MemorySafeClear<int>(ref AlphaTexChannelIndex);
            MemorySafeClear<int>(ref MatCapMaskChannelIndex);
            MemorySafeClear<int>(ref SecondaryAlbedoTexMaskChannelIndex);
            int CurMatIndex = 0;
            Mesh mesh;
            RayObjectTextureIndex TempObj = new RayObjectTextureIndex();
            List<Material> DoneMats = new List<Material>();
            Material[] SharedMaterials;// = new Material[1];
            foreach (RayTracingObject obj in ChildObjects) {
                if(obj == null) Debug.LogError("Report this to the developer!");
                DoneMats.Clear();
                if (obj.TryGetComponent<MeshFilter>(out MeshFilter TempMesh)) mesh = TempMesh.sharedMesh;
                else if(obj.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer TempSkin)) mesh = TempSkin.sharedMesh;
                else mesh = null;

                if(mesh == null) Debug.LogError("Missing Mesh: " + name);
                obj.matfill();
                VertCount += mesh.vertexCount;
                if(obj.TryGetComponent<Renderer>(out Renderer TempRend)) SharedMaterials = TempRend.sharedMaterials;
                else if(obj.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer TempSkinRend)) SharedMaterials = TempSkinRend.sharedMaterials;
                else SharedMaterials = new Material[1];
                int SharedMatLength = Mathf.Min(obj.Indexes.Length, SharedMaterials.Length);
                int Offset = 0;

                for (int i = 0; i < SharedMatLength; ++i) {
                    bool JustCreated = obj.JustCreated && obj.FollowMaterial[i] || obj.FollowMaterial[i];
                    MaterialData CurMat = new MaterialData();
                    bool GotSentBack = DoneMats.IndexOf(SharedMaterials[i]) != -1;
                    if (GotSentBack) Offset = DoneMats.IndexOf(SharedMaterials[i]);
                    else DoneMats.Add(SharedMaterials[i]);

                    TempObj.Obj = obj;
                    TempObj.ObjIndex = i;
                    int Index = AssetManager.ShaderNames.IndexOf(SharedMaterials[i].shader.name);
                    if (Index == -1) {
#if TTVerbose
                        Debug.Log("Adding Material To XML: " + SharedMaterials[i].shader.name);
#endif
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
                    if(!RelevantMat.RoughnessRange.Equals("null") && JustCreated) obj.Roughness[i] = (RelevantMat.UsesSmoothness ? (1.0f - SharedMaterials[i].GetFloat(RelevantMat.RoughnessRange)) : SharedMaterials[i].GetFloat(RelevantMat.RoughnessRange));
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
                        int ReadIndex = RelevantMat.AvailableTextures[i2].ReadIndex;
                        int TexPurpose = RelevantMat.AvailableTextures[i2].Purpose;
                        TexturePairs CurrentPair = RelevantMat.AvailableTextures[i2];
                        if(CurrentPair.Fallback != null) {
                            do {
                                if(SharedMaterials[i].HasProperty(CurrentPair.TextureName) && SharedMaterials[i].GetTexture(CurrentPair.TextureName) != null) {
                                    TexName = CurrentPair.TextureName;
                                    ReadIndex = CurrentPair.ReadIndex;
                                    break;
                                }
                                CurrentPair = CurrentPair.Fallback;
                            } while(CurrentPair != null);
                        }
                        switch((TexturePurpose)TexPurpose) {
                            case(TexturePurpose.SecondaryNormalTexture):
                                if(JustCreated) TextureParseScaleOffset(SharedMaterials[i], TexName, ref obj.SecondaryNormalTexScaleOffset[i]);
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref SecondaryNormalTexs, ref TempIndex); 
                                CurMat.SecondaryNormalTex.x = TempIndex;
                            break;                            
                            case(TexturePurpose.SecondaryAlbedoTextureMask):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref SecondaryAlbedoTexMasks, ref TempIndex); 
                                CurMat.SecondaryAlbedoMask.x = TempIndex; 
                                if(Result == 1) SecondaryAlbedoTexMaskChannelIndex.Add(ReadIndex);
                            break;
                            case(TexturePurpose.SecondaryAlbedoTexture):
                                if(JustCreated) TextureParseScaleOffset(SharedMaterials[i], TexName, ref obj.SecondaryAlbedoTexScaleOffset[i]);
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref SecondaryAlbedoTexs, ref TempIndex); 
                                CurMat.SecondaryAlbedoTex.x = TempIndex;
                            break;
                            case(TexturePurpose.MatCapTex):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref MatCapTexs, ref TempIndex); 
                                CurMat.MatCapTex.x = TempIndex;
                            break;
                            case(TexturePurpose.Albedo):
                                if(JustCreated) TextureParseScaleOffset(SharedMaterials[i], TexName, ref obj.MainTexScaleOffset[i]);
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
                                if(Result != 2 && JustCreated) obj.emission[i] = 12.0f;
                            break;
                            case(TexturePurpose.Metallic):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref MetallicTexs, ref TempIndex); 
                                CurMat.MetallicTex.x = TempIndex; 
                                if(Result == 1) MetallicTexChannelIndex.Add(ReadIndex);
                            break;
                            case(TexturePurpose.Roughness):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref RoughnessTexs, ref TempIndex); 
                                CurMat.RoughnessTex.x = TempIndex; 
                                if(Result == 1) RoughnessTexChannelIndex.Add(ReadIndex);
                            break;
                            case(TexturePurpose.Alpha):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref AlphaTexs, ref TempIndex); 
                                CurMat.AlphaTex.x = TempIndex; 
                                if(Result == 1) AlphaTexChannelIndex.Add(ReadIndex);
                            break;
                            case(TexturePurpose.MatCapMask):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref MatCapMasks, ref TempIndex); 
                                CurMat.MatCapMask.x = TempIndex; 
                                if(Result == 1) MatCapMaskChannelIndex.Add(ReadIndex);
                            break;
                        }
                    }

                    if(JustCreated) {
                        CurMat.SecondaryNormalTexScaleOffset = TempScale;
                        CurMat.SecondaryAlbedoTexScaleOffset = TempScale;
                        CurMat.AlbedoTextureScale = TempScale;
                        CurMat.SecondaryTextureScaleOffset = TempScale;
                        CurMat.NormalTexScaleOffset = TempScale;
                    }

                    if(JustCreated && obj.EmissionColor[i].x == 0 && obj.EmissionColor[i].y == 0 && obj.EmissionColor[i].z == 0) obj.EmissionColor[i] = new Vector3(1,1,1);
                    CurMat.MetallicRemap = obj.MetallicRemap[i];
                    CurMat.RoughnessRemap = obj.RoughnessRemap[i];
                    CurMat.BaseColor = (!obj.UseKelvin[i]) ? obj.BaseColor[i] : new Vector3(Mathf.CorrelatedColorTemperatureToRGB(obj.KelvinTemp[i]).r, Mathf.CorrelatedColorTemperatureToRGB(obj.KelvinTemp[i]).g, Mathf.CorrelatedColorTemperatureToRGB(obj.KelvinTemp[i]).b);
                    CurMat.BaseColor = obj.BaseColor[i];
                    CurMat.emission = obj.emission[i];
                    CurMat.Roughness = obj.Roughness[i];
                    CurMat.specTrans = obj.SpecTrans[i];
                    CurMat.EmissionColor = obj.EmissionColor[i];
                    CurMat.ColorBleed = obj.ColorBleed[i];
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
        public struct PerDataOffsets {
            public int TotalOffset;
            public int TotalStride;
            public int PosStride;
            public int NormStride;
            public int TanStride;
            public int ColStride;
            public int UVStride;
        }
        public List<Transform> ChildObjectTransforms;
        public unsafe void LoadData() {
            // TTStopWatch TempWatch = new TTStopWatch("StopWatch For: " + gameObject.name);
            // TempWatch.Start();
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
                                    if(TempRenderer.enabled){
                                        if(TempRenderer.rayTracingMode ==  UnityEngine.Experimental.Rendering.RayTracingMode.DynamicGeometry) {
                                            IsDeformable = true;
                                        }
                                        ChildObjectTransforms.Add(ChildTransf[i]);
                                    }
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
            if(TryGetComponent<RayTracingObject>(out RayTracingObject TempObj2)) {
                if(TryGetComponent<MeshRenderer>(out MeshRenderer TempRenderer)) {
                    if(TempRenderer.rayTracingMode ==  UnityEngine.Experimental.Rendering.RayTracingMode.DynamicGeometry) {
                        IsDeformable = true;
                    }
                }
                ChildObjects.Add(TempObj2);
            }
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

            // LoadFile();
            // MeshCountChanged = false;
            // HasCompleted = true;
            // NeedsToUpdate = false;
            // Debug.Log(Name + " Has Completed Building with " + AggTriangles.Length + " triangles");
            // FailureCount = 0;
            // return;
            
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
                #if HardwareRT
                    if(CurrentObject.TryGetComponent<Renderer>(out Renderers[i])) {}
                #endif
                int CurVertCount = mesh.vertexCount;
                int PreIndexLength = CurMeshData.Indices.Count;
                int IndexOffset = CurMeshData.CurVertexOffset;
                if((!Application.isPlaying) || mesh.isReadable) {
                    int VertCount2 = mesh.vertexCount;
                    var Tans = new List<Vector4>(VertCount2);
                    mesh.GetTangents(Tans);
                    if (Tans.Count != 0) NativeArray<Vector4>.Copy(Tans.ToArray(), 0, CurMeshData.TangentsArray, CurMeshData.CurVertexOffset, CurVertCount);

                    var Colors = new List<Color>(VertCount2);
                    mesh.GetColors(Colors);
                    if (Colors.Count != 0) NativeArray<Color>.Copy(Colors.ToArray(), 0, CurMeshData.ColorsArray, CurMeshData.CurVertexOffset, CurVertCount);

                    var Norms = new List<Vector3>(VertCount2);
                    mesh.GetNormals(Norms);
                    NativeArray<Vector3>.Copy(Norms.ToArray(), 0, CurMeshData.NormalsArray, CurMeshData.CurVertexOffset, CurVertCount);

                    mesh.GetVertices(Norms);
                    NativeArray<Vector3>.Copy(Norms.ToArray(), 0, CurMeshData.VerticiesArray, CurMeshData.CurVertexOffset, CurVertCount);

                    int MeshUvLength = mesh.uv.Length;
                    if (MeshUvLength == CurVertCount) NativeArray<Vector2>.Copy(mesh.uv, 0, CurMeshData.UVsArray, CurMeshData.CurVertexOffset, CurVertCount);

                    CurMeshData.Indices.AddRange(mesh.triangles);
                } else {
                    Debug.LogWarning("Object " + gameObject.name + " Is Using the GPU Mesh loading. Consider making this mesh read/writeable if possible, as GPU Loading takes additional RAM");
                    mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                    UnityEngine.Rendering.VertexAttributeDescriptor[] Attributes = mesh.GetVertexAttributes();
                    int VertBufCount = mesh.vertexBufferCount;
                    List<Vector3Int>[] BufferIndexers = new List<Vector3Int>[VertBufCount];
                    for(int i2 = 0; i2 < VertBufCount; i2++) BufferIndexers[i2] = new List<Vector3Int>();
                    VertexAttribute[] AttsToCheck = {VertexAttribute.Position, VertexAttribute.Tangent, VertexAttribute.Normal, VertexAttribute.TexCoord0, VertexAttribute.Color};
                    bool[] HasAttribute = new bool[AttsToCheck.Length];
                    int AttToCheckLength = AttsToCheck.Length;
                    int MaxStream = -1;
                    for(int i2 = 0; i2 < AttToCheckLength; i2++) {
                        int BufferIndex = mesh.GetVertexAttributeStream(AttsToCheck[i2]);
                        MaxStream = Mathf.Max(MaxStream, BufferIndex);
                        if(BufferIndex != -1) {
                            HasAttribute[i2] = true;
                            BufferIndexers[BufferIndex].Add(new Vector3Int(i2, mesh.GetVertexAttributeOffset(AttsToCheck[i2]) / 4, mesh.GetVertexAttributeDimension(AttsToCheck[i2])));
                        }
                    }
                    int TotVertCount = mesh.vertexCount;
                    for(int i2 = 0; i2 < MaxStream+1; i2++) {
                        int TempIndex = i2;
                        if(BufferIndexers[i2].Count != 0) {
                            GraphicsBuffer MeshBuffer = mesh.GetVertexBuffer(TempIndex);
                            Action<AsyncGPUReadbackRequest> checkOutput = (AsyncGPUReadbackRequest rq) => {
                                NativeArray<float> Data = (rq.GetData<float>());//May want to convert this to a pointer
                                int TotalStride = MeshBuffer.stride / 4;
                                int VertCount = Data.Length / TotalStride;//mesh.vertexCount;
                                int VertOff = -1;
                                int UVOff = -1;
                                int TanOff = -1;
                                int NormOff = -1;
                                int ColOff = -1;
                                int TempCoun = BufferIndexers[TempIndex].Count;
                                for(int i3 = 0; i3 < TempCoun; i3++) {
                                    switch(BufferIndexers[TempIndex][i3].x) {
                                        case 0: VertOff = BufferIndexers[TempIndex][i3].y; break;
                                        case 1: TanOff = BufferIndexers[TempIndex][i3].y; break;
                                        case 2: NormOff = BufferIndexers[TempIndex][i3].y; break;
                                        case 3: UVOff = BufferIndexers[TempIndex][i3].y; break;
                                        case 4: ColOff = BufferIndexers[TempIndex][i3].y; break;
                                    }
                                }
                                for(int i3 = 0; i3 < VertCount; i3++) {
                                    int Index = i3 * TotalStride;
                                    if(VertOff != -1) CurMeshData.Verticies[CurMeshData.CurVertexOffset + i3] = (new Vector3(Data[Index + VertOff], Data[Index + VertOff + 1], Data[Index + VertOff + 2]));
                                    if(NormOff != -1) CurMeshData.Normals[CurMeshData.CurVertexOffset + i3] = (new Vector3(Data[Index + NormOff], Data[Index + NormOff + 1], Data[Index + NormOff + 2]));
                                    if(TanOff != -1) CurMeshData.Tangents[CurMeshData.CurVertexOffset + i3] = (new Vector4(Data[Index + TanOff], Data[Index + TanOff + 1], Data[Index + TanOff + 2], Data[Index + TanOff + 3]));
                                    if(UVOff != -1) CurMeshData.UVs[CurMeshData.CurVertexOffset + i3] = (new Vector2(Data[Index + UVOff], Data[Index + UVOff + 1]));
                                }

                                Data.Dispose();
                                MeshBuffer.Release();
                            };
                            AsyncGPUReadback.Request(MeshBuffer, checkOutput);
                        }
                    }




                    mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
                    GraphicsBuffer indexesBuffer = mesh.GetIndexBuffer();
                    int tot = indexesBuffer.stride * indexesBuffer.count;
                    int Stride = indexesBuffer.stride / (mesh.indexFormat == IndexFormat.UInt32 ? 4 : 2);
                    Action<AsyncGPUReadbackRequest> checkOutput2 = (AsyncGPUReadbackRequest rq) => {
                        var indexesData = (rq.GetData<byte>());
                        byte[] Data = indexesData.ToArray();
                        int DatCoun = 0;
                        for(int i2 = 0; i2 < submeshcount; i2++) {
                            DatCoun += (int)mesh.GetIndexCount(i2);
                        }
                        for(int i2 = 0; i2 < DatCoun; i2++) {
                            if(mesh.indexFormat == IndexFormat.UInt32) CurMeshData.Indices.Add((int)BitConverter.ToUInt32(Data, (i2 * 4)));
                            else CurMeshData.Indices.Add((int)BitConverter.ToUInt16(Data, (i2 * 2)));
                        }
                        indexesData.Dispose();
                        indexesBuffer.Release();
                    };
                    AsyncGPUReadback.Request(indexesBuffer, checkOutput2);



                AsyncGPUReadback.WaitAllRequests();

                }
                CurMeshData.CurVertexOffset += CurVertCount;
                int TotalIndexLength = 0;
                for (int i2 = 0; i2 < submeshcount; ++i2) {//Add together all the submeshes in the mesh to consider it as one object
                    int IndiceLength = (int)mesh.GetIndexCount(i2) / 3;
                    MatIndex = Mathf.Min(i2, CurrentObject.Names.Length-1) + RepCount;
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
                    IndexOffset = IndexOffset,
                    IndexOffsetEnd = CurMeshData.CurVertexOffset
                });
                RepCount += Mathf.Min(submeshcount, CurrentObject.Names.Length);
            }
            // TempWatch.Stop("Object Mesh Loading");

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
            TrianglesArray.Dispose();
            this.BVH = new BVH8Builder(ref BVH2);
            CommonFunctions.DeepClean(ref BVH2.FinalIndices);
            BVH2.Dispose();
            BVH2 = null;

            int CWBVHIndicesBufferCount = BVH.cwbvh_indices.Length;
            #if !HardwareRT
                NativeArray<int> InvertedBufferArray = new NativeArray<int>(BVH.cwbvh_indices, Unity.Collections.Allocator.TempJob);
                int* InvertedBuffer = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(InvertedBufferArray);
                for (int i = 0; i < CWBVHIndicesBufferCount; i++) BVH.cwbvh_indices[InvertedBuffer[i]] = i;
                InvertedBufferArray.Dispose();
            #else
                for (int i = 0; i < CWBVHIndicesBufferCount; i++) BVH.cwbvh_indices[i] = i;
            #endif
            if (IsSkinnedGroup || IsDeformable)
            {
                ToBVHIndex = new int[BVH.cwbvhnode_count];
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
                LT.TriTarget = (uint)BVH.cwbvh_indices[LT.TriTarget];
                LightTriangles[i] = LT;
            }


            if(LightTriangles.Count > 0) {
                LBVH = new LightBVHBuilder(LightTriangles, LightTriNorms, 0.1f, LuminanceWeights);

            }


        }


        public List<int>[] Set;
        private void Refit(int Depth, int CurrentIndex) {
#if DontUseSGTree
            if((2.0f * ((float)(LBVH.nodes[CurrentIndex].cosTheta_oe >> 16) / 32767.0f) - 1.0f) == 0) return;
#endif
            Set[Depth].Add(CurrentIndex);
#if !DontUseSGTree
            if(LBVH.SGTree[CurrentIndex].left < 0) return;
            Refit(Depth + 1, LBVH.SGTree[CurrentIndex].left);
            Refit(Depth + 1, LBVH.SGTree[CurrentIndex].left + 1);
#else 
            if(LBVH.nodes[CurrentIndex].left < 0) return;
            Refit(Depth + 1, LBVH.nodes[CurrentIndex].left);
            Refit(Depth + 1, LBVH.nodes[CurrentIndex].left + 1);
#endif
        }



        public void RefitMesh(ref ComputeBuffer RealizedAggNodes, ref ComputeBuffer RealizedTriBufferA, ref ComputeBuffer RealizedTriBufferB, ref ComputeBuffer RealizedLightTriBuffer, ComputeBuffer RealizedLightNodeBuffer, CommandBuffer cmd)
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
                CWBVHIndicesBuffer.SetData(BVH.cwbvh_indices);
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
                if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ReMesh");
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
                if(HasLightTriangles) cmd.SetComputeBufferParam(MeshRefit, LightBLASRefitKernel, "LightTriangles", RealizedLightTriBuffer);
                cmd.SetComputeBufferParam(MeshRefit, RefitLayerKernel, "ReverseStack", StackBuffer);
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "Boxs", AABBBuffer);
                cmd.SetComputeBufferParam(MeshRefit, TransferKernel, "LightTrianglesOut", RealizedLightTriBuffer);
                cmd.SetComputeBufferParam(MeshRefit, TransferKernel, "CudaTriArrayINA", RealizedTriBufferA);
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "CudaTriArrayA", RealizedTriBufferA);
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "CudaTriArrayB", RealizedTriBufferB);
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
                if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ReMesh Accum");

                for (int i = 0; i < TotalObjects; i++)
                {
                    if(VertexBuffers[i] == null || !(VertexBuffers[i].IsValid())) {
                        AllFull = false;
                        return;
                    }
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

                if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ReMesh Accum");

                if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ReMesh Light Transfer");
                if(LightTriangles.Count != 0) {
                    cmd.SetComputeIntParam(MeshRefit, "gVertexCount", LightTriangles.Count);
                    cmd.DispatchCompute(MeshRefit, TransferKernel, (int)Mathf.Ceil(LightTriangles.Count / (float)KernelRatio), 1, 1);
                }
                if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ReMesh Light Transfer");

                if(LightTriangles.Count != 0) {
                    if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("LightRefitter");
                    cmd.SetComputeIntParam(MeshRefit, "TotalNodeOffset", LightNodeOffset);
                    cmd.SetComputeMatrixParam(MeshRefit, "ToWorld", transform.localToWorldMatrix);
#if !DontUseSGTree
                    cmd.SetComputeBufferParam(MeshRefit, LightBLASRefitKernel, "SGTreeWrite", RealizedLightNodeBuffer);
#else
                    cmd.SetComputeBufferParam(MeshRefit, LightBLASRefitKernel, "LightNodesWrite", RealizedLightNodeBuffer);
#endif
                    cmd.SetComputeFloatParam(MeshRefit, "FloatMax", float.MaxValue);
                    int ObjectOffset = LightNodeOffset;
                    for(int i = WorkingSet.Length - 1; i >= 0; i--) {
                        var ObjOffVar = ObjectOffset;
                        var SetCount = WorkingSet[i].count;
                        cmd.SetComputeIntParam(MeshRefit, "SetCount", SetCount);
                        cmd.SetComputeIntParam(MeshRefit, "ObjectOffset", ObjOffVar);
                        cmd.SetComputeBufferParam(MeshRefit, LightBLASRefitKernel, "WorkingSet", WorkingSet[i]);
                        cmd.DispatchCompute(MeshRefit, LightBLASRefitKernel, (int)Mathf.Ceil(SetCount / (float)256.0f), 1, 1);

                        ObjectOffset += SetCount;
                    }
                    if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("LightRefitter");
                }

                #if !HardwareRT
                    if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ReMesh Init");
                    cmd.SetComputeIntParam(MeshRefit, "NodeCount", NodePair.Count);
                    cmd.DispatchCompute(MeshRefit, NodeInitializerKernel, (int)Mathf.Ceil(NodePair.Count / (float)KernelRatio), 1, 1);
                    if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ReMesh Init");

                    if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ReMesh Refit");
                    for (int i = MaxRecur - 1; i >= 0; i--) {
                        var NodeCount2 = WorkingBuffer[i].count;
                        cmd.SetComputeIntParam(MeshRefit, "NodeCount", NodeCount2);
                        cmd.SetComputeBufferParam(MeshRefit, RefitLayerKernel, "WorkingBuffer", WorkingBuffer[i]);
                        cmd.DispatchCompute(MeshRefit, RefitLayerKernel, (int)Mathf.Ceil(NodeCount2 / (float)KernelRatio), 1, 1);
                    }
                    if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ReMesh Refit");

                    if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ReMesh Update");
                    cmd.SetComputeIntParam(MeshRefit, "NodeCount", NodePair.Count);
                    cmd.DispatchCompute(MeshRefit, NodeUpdateKernel, (int)Mathf.Ceil(NodePair.Count / (float)KernelRatio), 1, 1);
                    if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ReMesh Update");

                    if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ReMesh Compress");
                    cmd.SetComputeIntParam(MeshRefit, "NodeCount", BVH.cwbvhnode_count);
                    cmd.SetComputeIntParam(MeshRefit, "NodeOffset", NodeOffset);
                    cmd.DispatchCompute(MeshRefit, NodeCompressKernel, (int)Mathf.Ceil(NodePair.Count / (float)KernelRatio), 1, 1);
                    if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ReMesh Compress");
                #endif
                if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ReMesh");
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
            float a = Distance(pt1, pt2);
            float b = Distance(pt2, pt3);
            float c = Distance(pt3, pt1);
            float s = (a + b + c) / 2.0f;
            return Mathf.Sqrt(s * (s - a) * (s - b) * (s - c));
        }
        private float luminance(float r, float g, float b) { return 0.299f * r + 0.587f * g + 0.114f * b; }
        private float luminance(Vector3 A) { return Vector3.Dot(new Vector3(0.299f, 0.587f, 0.114f), A);}

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
            // if(HasCompleted) return;
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
                Matrix4x4 ChildMat = CachedTransforms[i + 1].WTL.inverse;
                Matrix4x4 TransMat = ParentMatInv * ChildMat;
                Vector3 Ofst = CachedTransforms[i + 1].WTL * CachedTransforms[i + 1].Position;
                Vector3 Ofst2 = ParentMatInv * CachedTransforms[0].Position;
                int IndexOffset = TransformIndexes[i].IndexOffset;
                int IndexEnd = TransformIndexes[i].VertexStart + TransformIndexes[i].VertexCount;
                OffsetReal = TransformIndexes[i].VertexStart / 3;
                bool IsSingle = CachedTransforms[i + 1].WTL.inverse == ParentMat;
                float scalex = Distance(ChildMat * new Vector3(1,0,0), new Vector3(0,0,0));
                float scaley = Distance(ChildMat * new Vector3(0,1,0), new Vector3(0,0,0));
                float scalez = Distance(ChildMat * new Vector3(0,0,1), new Vector3(0,0,0));
                float Leng = Distance(new Vector3(scalex, scaley, scalez), new Vector3(0,0,0));
                scalex /= Leng;
                scaley /= Leng;
                scalez /= Leng;
                Vector3 ScaleFactor = IsSingle ? new Vector3(1,1,1) : new Vector3(Mathf.Pow(1.0f / scalex, 2.0f), Mathf.Pow(1.0f / scaley, 2.0f), Mathf.Pow(1.0f / scalez, 2.0f));
                int InitOff = TransformIndexes[i].IndexOffset;
                int IndEnd = TransformIndexes[i].IndexOffsetEnd;
                for (int i3 = TransformIndexes[i].IndexOffset; i3 < IndEnd; i3++) {
                    int TruOff = i3 - InitOff;
                    V1 = CurMeshData.Verticies[i3] + Ofst;
                    V1 = TransMat * V1;
                    CurMeshData.Verticies[i3] = V1 - Ofst2;

                    Tan1 = TransMat * (Vector3)CurMeshData.Tangents[i3];
                    Normalize(ref Tan1);
                    CurMeshData.TangentsArray.ReinterpretStore(i3, CommonFunctions.PackOctahedral(Tan1));

                    Norm1 = TransMat * Scale(ScaleFactor, CurMeshData.Normals[i3]);
                    Normalize(ref Norm1);
                    CurMeshData.NormalsArray.ReinterpretStore(i3, CommonFunctions.PackOctahedral(Norm1));
                    
                    CurMeshData.ColorsArray.ReinterpretStore(i3, CommonFunctions.packRGBE(CurMeshData.Colors[i3]));
                    
                    CurMeshData.UVsArray.ReinterpretStore(i3, ((uint)Mathf.FloatToHalf(CurMeshData.UVs[i3].x) << 16) | Mathf.FloatToHalf(CurMeshData.UVs[i3].y));
                }


                for (int i3 = TransformIndexes[i].VertexStart; i3 < IndexEnd; i3 += 3) {//Transforming child meshes into the space of their parent
                    int Index1 = CurMeshData.Indices[i3] + IndexOffset;
                    int Index2 = CurMeshData.Indices[i3 + 2] + IndexOffset;
                    int Index3 = CurMeshData.Indices[i3 + 1] + IndexOffset;

                    V1 = CurMeshData.Verticies[Index1];
                    V2 = CurMeshData.Verticies[Index2];
                    V3 = CurMeshData.Verticies[Index3];

                    TempTri.tex0 = CurMeshData.UVsArray.ReinterpretLoad<uint>(Index1);
                    TempTri.texedge1 = CurMeshData.UVsArray.ReinterpretLoad<uint>(Index2);
                    TempTri.texedge2 = CurMeshData.UVsArray.ReinterpretLoad<uint>(Index3);

                    TempTri.pos0 = V1;
                    TempTri.posedge1 = V2 - V1;
                    TempTri.posedge2 = V3 - V1;

                    TempTri.norm0 = CurMeshData.NormalsArray.ReinterpretLoad<uint>(Index1);
                    TempTri.norm1 = CurMeshData.NormalsArray.ReinterpretLoad<uint>(Index2);
                    TempTri.norm2 = CurMeshData.NormalsArray.ReinterpretLoad<uint>(Index3);

                    TempTri.tan0 = CurMeshData.TangentsArray.ReinterpretLoad<uint>(Index1);
                    TempTri.tan1 = CurMeshData.TangentsArray.ReinterpretLoad<uint>(Index2);
                    TempTri.tan2 = CurMeshData.TangentsArray.ReinterpretLoad<uint>(Index3);
                    
                    TempTri.VertColA = CurMeshData.ColorsArray.ReinterpretLoad<uint>(Index1);
                    TempTri.VertColB = CurMeshData.ColorsArray.ReinterpretLoad<uint>(Index2);
                    TempTri.VertColC = CurMeshData.ColorsArray.ReinterpretLoad<uint>(Index3);

                    TempTri.MatDat = (uint)CurMeshData.MatDat[OffsetReal];
                    TempTri.IsEmissive = 0;
                    AggTriangles[OffsetReal] = TempTri;
                    Triangles[OffsetReal].Create(V1, V2);
                    Triangles[OffsetReal].Extend(V3);
                    Triangles[OffsetReal].Validate(ParentScale);

                    if (_Materials[(int)TempTri.MatDat].emission > 0.0f) {
                        bool IsValid = true;
                        Vector3 SecondaryBaseCol = Vector3.one;
                        #if AccurateLightTris
                            if(_Materials[(int)TempTri.MatDat].EmissiveTex.x != 0) {
                                int ThisIndex = _Materials[(int)TempTri.MatDat].EmissiveTex.x - 1;
                                Vector2 UVV = (new Vector2(Mathf.HalfToFloat((ushort)(TempTri.tex0 >> 16)), Mathf.HalfToFloat((ushort)(TempTri.tex0 & 0xFFFF))) + 
                                                new Vector2(Mathf.HalfToFloat((ushort)(TempTri.texedge1 >> 16)), Mathf.HalfToFloat((ushort)(TempTri.texedge1 & 0xFFFF))) + 
                                                new Vector2(Mathf.HalfToFloat((ushort)(TempTri.texedge2 >> 16)), Mathf.HalfToFloat((ushort)(TempTri.texedge2 & 0xFFFF)))) / 3.0f;
                                int UVIndex3 = (int)Mathf.Max((Mathf.Floor(UVV.y * (EmissionTexWidthHeight[ThisIndex].y)) * EmissionTexWidthHeight[ThisIndex].x + Mathf.Floor(UVV.x * EmissionTexWidthHeight[ThisIndex].x)),0);
                                UVV = new Vector2(Mathf.HalfToFloat((ushort)(TempTri.tex0 >> 16)), Mathf.HalfToFloat((ushort)(TempTri.tex0 & 0xFFFF))); 
                                int UVIndex2 = (int)Mathf.Max((Mathf.Floor(UVV.y * (EmissionTexWidthHeight[ThisIndex].y)) * EmissionTexWidthHeight[ThisIndex].x + Mathf.Floor(UVV.x * EmissionTexWidthHeight[ThisIndex].x)),0);
                                UVV = new Vector2(Mathf.HalfToFloat((ushort)(TempTri.texedge1 >> 16)), Mathf.HalfToFloat((ushort)(TempTri.texedge1 & 0xFFFF))); 
                                int UVIndex1 = (int)Mathf.Max((Mathf.Floor(UVV.y * (EmissionTexWidthHeight[ThisIndex].y)) * EmissionTexWidthHeight[ThisIndex].x + Mathf.Floor(UVV.x * EmissionTexWidthHeight[ThisIndex].x)),0);
                                UVV = new Vector2(Mathf.HalfToFloat((ushort)(TempTri.texedge2 >> 16)), Mathf.HalfToFloat((ushort)(TempTri.texedge2 & 0xFFFF))); 
                                int UVIndex0 = (int)Mathf.Max((Mathf.Floor(UVV.y * (EmissionTexWidthHeight[ThisIndex].y)) * EmissionTexWidthHeight[ThisIndex].x + Mathf.Floor(UVV.x * EmissionTexWidthHeight[ThisIndex].x)),0);
                                bool FoundTrue = false;
                                if(UVIndex3 < EmissionTexWidthHeight[ThisIndex].y * EmissionTexWidthHeight[ThisIndex].x){
                                    if(!(EmissionTexPixels[ThisIndex][UVIndex3].r < 0.01f && EmissionTexPixels[ThisIndex][UVIndex3].g < 0.01f && EmissionTexPixels[ThisIndex][UVIndex3].b < 0.01f)) FoundTrue = true;
                                    // else SecondaryBaseCol = new Vector3(EmissionTexPixels[ThisIndex][UVIndex3].r, EmissionTexPixels[ThisIndex][UVIndex3].g, EmissionTexPixels[ThisIndex][UVIndex3].b);
                                }
                                if(UVIndex2 < EmissionTexWidthHeight[ThisIndex].y * EmissionTexWidthHeight[ThisIndex].x){
                                    if(!(EmissionTexPixels[ThisIndex][UVIndex2].r < 0.01f && EmissionTexPixels[ThisIndex][UVIndex2].g < 0.01f && EmissionTexPixels[ThisIndex][UVIndex2].b < 0.01f)) FoundTrue = true;
                                    // else SecondaryBaseCol = new Vector3(EmissionTexPixels[ThisIndex][UVIndex3].r, EmissionTexPixels[ThisIndex][UVIndex3].g, EmissionTexPixels[ThisIndex][UVIndex3].b);
                                }
                                if(UVIndex1 < EmissionTexWidthHeight[ThisIndex].y * EmissionTexWidthHeight[ThisIndex].x){
                                    if(!(EmissionTexPixels[ThisIndex][UVIndex1].r < 0.01f && EmissionTexPixels[ThisIndex][UVIndex1].g < 0.01f && EmissionTexPixels[ThisIndex][UVIndex1].b < 0.01f)) FoundTrue = true;
                                    // else SecondaryBaseCol = new Vector3(EmissionTexPixels[ThisIndex][UVIndex3].r, EmissionTexPixels[ThisIndex][UVIndex3].g, EmissionTexPixels[ThisIndex][UVIndex3].b);
                                }
                                if(UVIndex0 < EmissionTexWidthHeight[ThisIndex].y * EmissionTexWidthHeight[ThisIndex].x){
                                    if(!(EmissionTexPixels[ThisIndex][UVIndex0].r < 0.01f && EmissionTexPixels[ThisIndex][UVIndex0].g < 0.01f && EmissionTexPixels[ThisIndex][UVIndex0].b < 0.01f)) FoundTrue = true;
                                    // else SecondaryBaseCol = new Vector3(EmissionTexPixels[ThisIndex][UVIndex3].r, EmissionTexPixels[ThisIndex][UVIndex3].g, EmissionTexPixels[ThisIndex][UVIndex3].b);
                                }
                                IsValid = FoundTrue;
                            
                            }
                        #endif
                        if(IsValid && _Materials[(int)TempTri.MatDat].emission > 1) {
                            Vector3 Radiance = _Materials[(int)TempTri.MatDat].emission * _Materials[(int)TempTri.MatDat].BaseColor;
                            float radiance = luminance(Radiance.x, Radiance.y, Radiance.z);
                            float area = AreaOfTriangle(ParentMat * V1, ParentMat * V2, ParentMat * V3);
                            if(area != 0) {
                                HasLightTriangles = true;
                                float e = radiance * area;
                                if(System.Double.IsNaN(area)) continue;
                                TotEnergy += area;
                                LightTriNorms.Add(((CommonFunctions.UnpackOctahedral(TempTri.norm0) + CommonFunctions.UnpackOctahedral(TempTri.norm1) + CommonFunctions.UnpackOctahedral(TempTri.norm2)) / 3.0f).normalized);
                                LightTriangles.Add(new LightTriData() {
                                    pos0 = TempTri.pos0,
                                    posedge1 = TempTri.posedge1,
                                    posedge2 = TempTri.posedge2,
                                    TriTarget = (uint)(OffsetReal),
                                    SourceEnergy = Distance(Vector3.zero, _Materials[(int)TempTri.MatDat].emission * Scale(_Materials[(int)TempTri.MatDat].BaseColor, SecondaryBaseCol))
                                    });
                                LuminanceWeights.Add(_Materials[(int)TempTri.MatDat].emission);//Distance(Vector3.zero, _Materials[(int)TempTri.MatDat].emission * Scale(_Materials[(int)TempTri.MatDat].BaseColor, SecondaryBaseCol)));
                                AggTriangles[OffsetReal].IsEmissive = 1;
                                IllumTriCount++;
                            }
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
                    for (int i = 0; i < TriLength; i++) AggTriangles[BVH.cwbvh_indices[i]] = VecPointer[i];
                    Vector3Array.Dispose();
                }
                AggNodes = new BVHNode8DataCompressed[BVH.cwbvhnode_count];
                CommonFunctions.Aggregate(ref AggNodes, BVH);
                BVH.BVH8NodesArray.Dispose();
            #else 
                if(IsSkinnedGroup || IsDeformable) {
                    ConstructAABB();
                    Construct();
                    AggNodes = new BVHNode8DataCompressed[BVH.cwbvhnode_count];
                    CommonFunctions.Aggregate(ref AggNodes, BVH);
                    BVH.BVH8NodesArray.Dispose();
                } else {
                    AggNodes = new BVHNode8DataCompressed[1];
                    if(LightTriangles.Count > 0) LBVH = new LightBVHBuilder(LightTriangles, LightTriNorms, 0.1f, LuminanceWeights);
                }
            #endif
            MeshCountChanged = false;
            HasCompleted = true;
            NeedsToUpdate = false;
#if TTVerbose
            Debug.Log(Name + " Has Completed Building with " + AggTriangles.Length + " triangles");
#endif
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
                    var Instances = FindObjectsOfType(typeof(InstancedObject)) as InstancedObject[];
                    int Count = Instances.Length;
                    for(int i = 0; i < Count; i++) {
                        if(Instances[i].InstanceParent == this) Instances[i].OnParentClear();
                    }
                }
                else {
                    if(AssetManager.Assets != null) {
                        if(!AssetManager.Assets.RemoveQue.Contains(this)) {
                            if(QueInProgress == 2) {
                                AssetManager.Assets.UpdateQue.Remove(this);
                            }
                            AssetManager.Assets.AddQue.Add(this);
                            QueInProgress = 3;
                            ExistsInQue = 3;
                        }
                        AssetManager.Assets.ParentCountHasChanged = true;
                    }
                }
                HasCompleted = false;
            }
        }

        private void OnDisable() {
            HasStarted = false;
            FailureCount = 0;
            ClearAll();
            if (gameObject.scene.isLoaded) {
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
                    if(!AssetManager.Assets.RemoveQue.Contains(this)) {
                        QueInProgress = -1;
                        AssetManager.Assets.RemoveQue.Add(this);
                    }
                    AssetManager.Assets.ParentCountHasChanged = true;
                }
                HasCompleted = false;
            }
        }

        // public void SaveToFile() {
        //     // ParentData LoadedFile = new ParentData();
        //     // LoadedFile.SGTree = LBVH.SGTree;
        //     // LoadedFile.AggTriangles = AggTriangles;
        //     // LoadedFile.AggNodes = AggNodes;
        //     // LoadedFile.LightTriangles = LightTriangles;
        //     // string json = JsonUtility.ToJson(LoadedFile);
        //     using(var stream = File.Open((Application.dataPath.Replace("/Assets", "")) + "/SavedFiles/" + gameObject.name + ".txt", FileMode.Create, FileAccess.Write)) {
        //         using (var writer = new BinaryWriter(stream))
        //         {
        //             int TargetLength = (LBVH == null || LBVH.SGTree == null) ? 0 : LBVH.SGTree.Length;
        //             writer.Write(TargetLength);
        //             GaussianTreeNode SGNode;
        //             for(int i = 0; i < TargetLength; i++) {
        //                 SGNode = LBVH.SGTree[i];
        //                 writer.Write(SGNode.S.Center.x);
        //                 writer.Write(SGNode.S.Center.y);
        //                 writer.Write(SGNode.S.Center.z);
        //                 writer.Write(SGNode.S.Radius);
        //                 writer.Write(SGNode.axis.x);
        //                 writer.Write(SGNode.axis.y);
        //                 writer.Write(SGNode.axis.z);
        //                 writer.Write(SGNode.variance);
        //                 writer.Write(SGNode.sharpness);
        //                 writer.Write(SGNode.intensity);
        //                 writer.Write(SGNode.left);
        //             }


        //             TargetLength = LightTriangles.Count;
        //             writer.Write(TargetLength);
        //             LightTriData LightTri;
        //             for(int i = 0; i < TargetLength; i++) {
        //                 LightTri = LightTriangles[i];
        //                 writer.Write(LightTri.pos0.x);
        //                 writer.Write(LightTri.pos0.y);
        //                 writer.Write(LightTri.pos0.z);
        //                 writer.Write(LightTri.posedge1.x);
        //                 writer.Write(LightTri.posedge1.y);
        //                 writer.Write(LightTri.posedge1.z);
        //                 writer.Write(LightTri.posedge2.x);
        //                 writer.Write(LightTri.posedge2.y);
        //                 writer.Write(LightTri.posedge2.z);
        //                 writer.Write(LightTri.TriTarget);
        //                 writer.Write(LightTri.SourceEnergy);
        //             }


        //             TargetLength = AggTriangles.Length;
        //             writer.Write(TargetLength);
        //             CudaTriangle triangle;
        //             for(int i = 0; i < TargetLength; i++) {
        //                 triangle = AggTriangles[i];
        //                 writer.Write(triangle.pos0.x);
        //                 writer.Write(triangle.pos0.y);
        //                 writer.Write(triangle.pos0.z);

        //                 writer.Write(triangle.posedge1.x);
        //                 writer.Write(triangle.posedge1.y);
        //                 writer.Write(triangle.posedge1.z);

        //                 writer.Write(triangle.posedge2.x);
        //                 writer.Write(triangle.posedge2.y);
        //                 writer.Write(triangle.posedge2.z);

        //                 writer.Write(triangle.norm0);
        //                 writer.Write(triangle.norm1);
        //                 writer.Write(triangle.norm2);

        //                 writer.Write(triangle.tan0);
        //                 writer.Write(triangle.tan1);
        //                 writer.Write(triangle.tan2);

        //                 writer.Write(triangle.tex0);
        //                 writer.Write(triangle.texedge1);
        //                 writer.Write(triangle.texedge2);

        //                 writer.Write(triangle.VertColA);
        //                 writer.Write(triangle.VertColB);
        //                 writer.Write(triangle.VertColC);

        //                 writer.Write(triangle.MatDat);
        //             }
        //             TargetLength = AggNodes.Length;
        //             writer.Write(TargetLength);
        //             BVHNode8DataCompressed CurNode;
        //             for(int i = 0; i < TargetLength; i++) {
        //                 CurNode = AggNodes[i];
        //                 writer.Write(CurNode.node_0x);
        //                 writer.Write(CurNode.node_0y);
        //                 writer.Write(CurNode.node_0z);
        //                 writer.Write(CurNode.node_0w);
        //                 writer.Write(CurNode.node_1x);
        //                 writer.Write(CurNode.node_1y);
        //                 writer.Write(CurNode.node_1z);
        //                 writer.Write(CurNode.node_1w);
        //                 writer.Write(CurNode.node_2x);
        //                 writer.Write(CurNode.node_2y);
        //                 writer.Write(CurNode.node_2z);
        //                 writer.Write(CurNode.node_2w);
        //                 writer.Write(CurNode.node_3x);
        //                 writer.Write(CurNode.node_3y);
        //                 writer.Write(CurNode.node_3z);
        //                 writer.Write(CurNode.node_3w);
        //                 writer.Write(CurNode.node_4x);
        //                 writer.Write(CurNode.node_4y);
        //                 writer.Write(CurNode.node_4z);
        //                 writer.Write(CurNode.node_4w);
        //             }

        //             writer.Write(LBVH.ParentBound.aabb.b.BBMax.x);
        //             writer.Write(LBVH.ParentBound.aabb.b.BBMax.y);
        //             writer.Write(LBVH.ParentBound.aabb.b.BBMax.z);
        //             writer.Write(LBVH.ParentBound.aabb.b.BBMin.x);
        //             writer.Write(LBVH.ParentBound.aabb.b.BBMin.y);
        //             writer.Write(LBVH.ParentBound.aabb.b.BBMin.z);
        //             writer.Write(LBVH.ParentBound.aabb.w.x);
        //             writer.Write(LBVH.ParentBound.aabb.w.y);
        //             writer.Write(LBVH.ParentBound.aabb.w.z);
        //             writer.Write(LBVH.ParentBound.aabb.phi);
        //             writer.Write(LBVH.ParentBound.aabb.cosTheta_o);
        //             writer.Write(LBVH.ParentBound.aabb.cosTheta_e);
        //             writer.Write(LBVH.ParentBound.aabb.LightCount);
        //             writer.Write(LBVH.ParentBound.aabb.Pad1);
        //             writer.Write(LBVH.ParentBound.left);
        //             writer.Write(LBVH.ParentBound.isLeaf);

        //         }
        //     }
        // }
        // public void LoadFile() {
        //     using(var stream = File.Open((Application.dataPath.Replace("/Assets", "")) + "/SavedFiles/" + gameObject.name + ".txt", FileMode.Open, FileAccess.Read)) {
        //         using(BinaryReader reader = new BinaryReader(stream)) {
        //             int CurCount = reader.ReadInt32();
        //             LBVH = new LightBVHBuilder();
        //             LBVH.SGTree = new GaussianTreeNode[CurCount];
        //             for(int i = 0; i < CurCount; i++) {
        //                 LBVH.SGTree[i] = new GaussianTreeNode
        //                 {
        //                     // Read BoundingSphere
        //                     S = new CommonVars.BoundingSphere
        //                     {
        //                         Center = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
        //                         Radius = reader.ReadSingle()
        //                     },

        //                     // Read other fields
        //                     axis = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
        //                     variance = reader.ReadSingle(),
        //                     sharpness = reader.ReadSingle(),
        //                     intensity = reader.ReadSingle(),
        //                     left = reader.ReadInt32()
        //                 };
        //             }
        //             CurCount = reader.ReadInt32();
        //             LightTriangles = new List<LightTriData>(CurCount);
        //             if(CurCount != 0) HasLightTriangles = true;
        //             for(int i = 0; i < CurCount; i++) {
        //                 LightTriangles.Add(new LightTriData() {
        //                     pos0 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
        //                     posedge1 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
        //                     posedge2 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),                            
        //                     TriTarget = reader.ReadUInt32(),
        //                     SourceEnergy = reader.ReadSingle()
        //                 });
        //             }
        //             CurCount = reader.ReadInt32();
        //             AggTriangles = new CudaTriangle[CurCount];
        //             for(int i = 0; i < CurCount; i++) {
        //                 AggTriangles[i] = new CudaTriangle
        //                 {
        //                     // Read Vector3 fields
        //                     pos0 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
        //                     posedge1 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
        //                     posedge2 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),

        //                     // Read uint fields
        //                     norm0 = reader.ReadUInt32(),
        //                     norm1 = reader.ReadUInt32(),
        //                     norm2 = reader.ReadUInt32(),

        //                     tan0 = reader.ReadUInt32(),
        //                     tan1 = reader.ReadUInt32(),
        //                     tan2 = reader.ReadUInt32(),

        //                     tex0 = reader.ReadUInt32(),
        //                     texedge1 = reader.ReadUInt32(),
        //                     texedge2 = reader.ReadUInt32(),

        //                     VertColA = reader.ReadUInt32(),
        //                     VertColB = reader.ReadUInt32(),
        //                     VertColC = reader.ReadUInt32(),

        //                     MatDat = reader.ReadUInt32()
        //                 };
        //             }

        //             CurCount = reader.ReadInt32();
        //             AggNodes = new BVHNode8DataCompressed[CurCount];
        //             for(int i = 0; i < CurCount; i++) {
        //                 AggNodes[i] = new BVHNode8DataCompressed
        //                 {
        //                     node_0x = reader.ReadUInt32(),
        //                     node_0y = reader.ReadUInt32(),
        //                     node_0z = reader.ReadUInt32(),
        //                     node_0w = reader.ReadUInt32(),
        //                     node_1x = reader.ReadUInt32(),
        //                     node_1y = reader.ReadUInt32(),
        //                     node_1z = reader.ReadUInt32(),
        //                     node_1w = reader.ReadUInt32(),
        //                     node_2x = reader.ReadUInt32(),
        //                     node_2y = reader.ReadUInt32(),
        //                     node_2z = reader.ReadUInt32(),
        //                     node_2w = reader.ReadUInt32(),
        //                     node_3x = reader.ReadUInt32(),
        //                     node_3y = reader.ReadUInt32(),
        //                     node_3z = reader.ReadUInt32(),
        //                     node_3w = reader.ReadUInt32(),
        //                     node_4x = reader.ReadUInt32(),
        //                     node_4y = reader.ReadUInt32(),
        //                     node_4z = reader.ReadUInt32(),
        //                     node_4w = reader.ReadUInt32()
        //                 };
        //             }
        //             LBVH.ParentBound = new NodeBounds {
        //                 aabb = new LightBounds {
        //                     b = new AABB {
        //                         BBMax = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
        //                         BBMin = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
        //                     },
        //                     w = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
        //                     phi = reader.ReadSingle(),
        //                     cosTheta_o = reader.ReadSingle(),
        //                     cosTheta_e = reader.ReadSingle(),
        //                     LightCount = reader.ReadInt32(),
        //                     Pad1 = reader.ReadSingle()
        //                 },
        //                 left = reader.ReadInt32(),
        //                 isLeaf = reader.ReadInt32()
        //             };

        //             // var formatter = new BinaryFormatter();
        //             // ParentData LoadedFile = (ParentData)formatter.Deserialize(stream);
                    
        //             // AggTriangles = LoadedFile.AggTriangles;
        //             // AggNodes = LoadedFile.AggNodes;
        //             BVH = new BVH8Builder();
        //             BVH.cwbvhnode_count = AggNodes.Length;
        //             BVH.cwbvhindex_count = AggTriangles.Length;
        //         }
        //     }
        // }

        // public void OnDrawGizmos() {

        //     if(LightTriangles != null) {
        //         int LightTriCount = LightTriangles.Count;
        //         for(int i = 0; i < LightTriCount; i++) {
        //             Vector3 Pos0 = CommonFunctions.ToVector3(this.transform.localToWorldMatrix * CommonFunctions.ToVector4(LightTriangles[i].pos0, 1));
        //             Vector3 Pos1 = CommonFunctions.ToVector3(this.transform.localToWorldMatrix * CommonFunctions.ToVector4(LightTriangles[i].pos0 + LightTriangles[i].posedge1, 1));
        //             Vector3 Pos2 = CommonFunctions.ToVector3(this.transform.localToWorldMatrix * CommonFunctions.ToVector4(LightTriangles[i].pos0 + LightTriangles[i].posedge2, 1));
        //                 Gizmos.DrawLine(Pos0, Pos1);
        //                 Gizmos.DrawLine(Pos0, Pos2);
        //                 Gizmos.DrawLine(Pos2, Pos1);
        //         }
        //     }
        // }



        // public void OnDrawGizmos() {
        //     if(BVH2 != null && BVH2.VerboseNodes != null) {
        //         int Count = BVH2.VerboseNodes.Length;
        //         for(int i = 0; i < Count; i++) {
        //             if(BVH2.VerboseNodes[i].count == 0) {
        //                 Vector3 BBMax = CommonFunctions.ToVector3(this.transform.localToWorldMatrix * CommonFunctions.ToVector4(BVH2.VerboseNodes[i].aabb.BBMax, 1));
        //                 Vector3 BBMin = CommonFunctions.ToVector3(this.transform.localToWorldMatrix * CommonFunctions.ToVector4(BVH2.VerboseNodes[i].aabb.BBMin, 1));
        //                 Gizmos.DrawWireCube((BBMax + BBMin) / 2.0f, BBMax - BBMin);
        //             }
        //         }
        //     }
        // }

        // public void OnDrawGizmos() {
        //     if(LBVH != null && LBVH.SGTree != null) {
        //         int Count = LBVH.SGTree.Length;
        //         for(int i = 0; i < Count; i++) {
        //             Vector3 Pos = CommonFunctions.ToVector3(this.transform.localToWorldMatrix * CommonFunctions.ToVector4(LBVH.SGTree[i].S.Center, 1));
        //             float Radius = Distance(Pos, CommonFunctions.ToVector3(this.transform.localToWorldMatrix * CommonFunctions.ToVector4(LBVH.SGTree[i].S.Center + new Vector3(LBVH.SGTree[i].S.Radius, 0, 0), 1)));
        //             // if(LightTree[i].variance < LightTree[i].S.Radius * VarTest) Gizmos.DrawWireSphere(Pos, Radius);
        //             Gizmos.DrawWireSphere(Pos, Radius);

        //         }
        //     }
        // }
    }


    [System.Serializable]
    public class ParentData {
        public GaussianTreeNode[] SGTree;
        public List<LightTriData> LightTriangles;
        public CudaTriangle[] AggTriangles;
        public BVHNode8DataCompressed[] AggNodes;
    }



}