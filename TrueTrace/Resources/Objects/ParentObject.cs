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
#if UNITY_PIPELINE_URP || UNITY_PIPELINE_HDRP
    [ExecuteInEditMode]
#endif
    public class ParentObject : MonoBehaviour
    {
    #if TTExtraVerbose && TTVerbose
        TTStopWatch MainWatch;
    #endif
        public float Distance(Vector3 a, Vector3 b) {
            a.x -= b.x;
            a.y -= b.y;
            a.z -= b.z;
            return (float)Math.Sqrt((double)(a.x * a.x + a.y * a.y + a.z * a.z));
        }
        public static Vector3 Scale(Vector3 a, Vector3 b) {
            a.x *= b.x;
            a.y *= b.y;
            a.z *= b.z;
            return a;
        }

        public void Normalize(ref Vector3 a) {
            float num = (float)Math.Sqrt((double)(a.x * a.x + a.y * a.y + a.z * a.z));
            if (num > 9.99999974737875E-06) {
                float inversed = 1 / num;
                a.x *= inversed;
                a.y *= inversed;
                a.z *= inversed;
            } else {
                a.x = 0;
                a.y = 0;
                a.z = 0;
            }
        }


        private float AreaOfTriangle(Vector3 pt1, Vector3 pt2, Vector3 pt3) {
            float a = Distance(pt1, pt2);
            float b = Distance(pt2, pt3);
            float c = Distance(pt3, pt1);
            float s = (a + b + c) / 2.0f;
            return Mathf.Sqrt(s * (s - a) * (s - b) * (s - c));
        }
        public PerInstanceData[] InstanceDatas;
#if HardwareRT
        public int[] RTAccelHandle;
        public int[] RTAccelSubmeshOffsets;
#endif
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
        private unsafe NativeArray<AABB> TrianglesArray;
        private unsafe AABB* Triangles;
        [HideInInspector] public CudaTriangle[] AggTriangles;
        [HideInInspector] public Vector3 ParentScale;
        [HideInInspector] public List<LightTriData> LightTriangles;
        [HideInInspector] private List<Vector3> LightTriNorms;//Test to see if I can get rrid of this alltogether and just calculate the normal based off cross...
        public BVH8Builder BVH;
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
        [HideInInspector] public Transform ThisTransform;

        [HideInInspector] public int ConstructKernel;
        [HideInInspector] public int RefitLayerKernel;
        [HideInInspector] public int UpdateGlobalBufferAABBKernel;
        [HideInInspector] public int LightBLASRefitKernel;
        [HideInInspector] public int LightBLASCopyKernel;

        [HideInInspector] public int CompactedMeshData;

        [HideInInspector] public int InstanceReferences;

        [HideInInspector] public bool NeedsToUpdate;

        public int FailureCount = 0;

        public int TotalTriangles;
        public bool IsSkinnedGroup;
        public bool HasLightTriangles;

        private ComputeBuffer AABBBuffer;
        private ComputeBuffer NodeParentAABBBuffer;

        private ComputeBuffer CWBVHIndicesBuffer;
        private ComputeBuffer[] WorkingBufferCWBVH;
        private ComputeBuffer[] WorkingBufferLightBVH;
        private BVH2Builder BVH2;

        [HideInInspector] private List<float> LuminanceWeights;

        [HideInInspector] public int MaxRecur = 0;

        [HideInInspector] public AABB tempAABB;
        
        public int GlobalNodeOffset;
        public int GlobalTriOffset;
        public int GlobalSkinnedOffset;
        public int GlobalLightTriOffset;
        public int GlobalLightNodeOffset;
        public int GlobalLightNodeSkinnedOffset;
        public int LocalTriCount;
        public int LocalNodeCount;
        public int LocalLightTriCount;
        public int LocalLightNodeCount;

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
            CommonFunctions.DeepClean(ref AggTriangles);
            CommonFunctions.DeepClean(ref AggNodes);
            CommonFunctions.DeepClean(ref LightTriNorms);
            CommonFunctions.DeepClean(ref LuminanceWeights);
            if(TrianglesArray.IsCreated) TrianglesArray.Dispose();
            if(BVH2 != null) BVH2.Dispose();
            BVH2 = null;
            if(BVH != null) BVH.Dispose();
            BVH = null;
            CurMeshData.Clear();
            HasCompleted = false;
            if (VertexBuffers != null) {
                for (int i = 0; i < VertexBuffers.Length; i++) {
                    VertexBuffers[i]?.Release();
                    IndexBuffers[i]?.Release();
                }
                AABBBuffer?.Release();
                NodeParentAABBBuffer?.Release();
                CWBVHIndicesBuffer?.Release();
                if(WorkingBufferCWBVH != null) for(int i2 = 0; i2 < WorkingBufferCWBVH.Length; i2++) WorkingBufferCWBVH[i2]?.Release();
                if(WorkingBufferLightBVH != null) for(int i2 = 0; i2 < WorkingBufferLightBVH.Length; i2++) WorkingBufferLightBVH[i2]?.Release();
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


            #if AccurateLightTris
                if(EmissionTexPixels != null) {
                    int EmissTexLeng = EmissionTexPixels.Count;
                    for(int i = 0; i < EmissTexLeng; i++) {
                        EmissionTexPixels[i].pixels.Dispose();
                    }
                    EmissionTexPixels = null;
                }
            #endif

        }

        public void Reset(int Que) {
            ExistsInQue = Que;
            QueInProgress = Que;
            LoadData();
            AsyncTask = Task.Run(() => {BuildTotal();});
        }

        public void OnApplicationQuit()
        {

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
            HasCompleted = false;
            MeshRefit = Resources.Load<ComputeShader>("Utility/BVHRefitter");
            ConstructKernel = MeshRefit.FindKernel("Construct");
            RefitLayerKernel = MeshRefit.FindKernel("RefitLayer");
            UpdateGlobalBufferAABBKernel = MeshRefit.FindKernel("UpdateGlobalBufferAABBKernel");
            LightBLASRefitKernel = MeshRefit.FindKernel("BLASLightTreeRefitKernel");
            LightBLASCopyKernel = MeshRefit.FindKernel("BLASCopyNodeDataKernel");
        }
        private bool NeedsToResetBuffers = true;
        public void SetUpBuffers() {
            if (NeedsToResetBuffers) {

                if (LightTriBuffer != null) LightTriBuffer.Release();
                if (TriBuffer != null) TriBuffer.Release();
                if (BVHBuffer != null) BVHBuffer.Release();
                if (LightTreeBuffer != null) LightTreeBuffer.Release();
                  if(AggTriangles != null) {
                    if(LightTriangles.Count == 0) {
                        CommonFunctions.CreateDynamicBuffer(ref LightTriBuffer, 1, CommonFunctions.GetStride<LightTriData>());
#if !DontUseSGTree
                        CommonFunctions.CreateDynamicBuffer(ref LightTreeBuffer, 1, CommonFunctions.GetStride<GaussianTreeNode>());
#else
                        CommonFunctions.CreateDynamicBuffer(ref LightTreeBuffer, 1, CommonFunctions.GetStride<CompactLightBVHData>());
#endif
                    } else {
                        CommonFunctions.CreateComputeBuffer(ref LightTriBuffer, LightTriangles);
#if !DontUseSGTree
                        CommonFunctions.CreateComputeBuffer(ref LightTreeBuffer, LBVH.SGTree);
#else
                        CommonFunctions.CreateComputeBuffer(ref LightTreeBuffer, LBVH.nodes);
#endif
                    }
                    CommonFunctions.CreateComputeBuffer(ref TriBuffer, AggTriangles);
                    CommonFunctions.CreateComputeBuffer(ref BVHBuffer, AggNodes);
                    LocalLightNodeCount = LightTreeBuffer.count;
                    LocalLightTriCount = LightTriBuffer.count;
                    LocalTriCount = TriBuffer.count;
                    LocalNodeCount = BVHBuffer.count;
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
        public List<Texture> DiffTransTexs;
        public List<int> DiffTransTexChannelIndex;
        public List<Texture> AlphaTexs;
        public List<int> AlphaTexChannelIndex;
        public List<Texture> MatCapMasks;
        public List<int> MatCapMaskChannelIndex;
        public List<Texture> MatCapTexs;
        public List<Texture> SecondaryAlbedoTexMasks;
        public List<int> SecondaryAlbedoTexMaskChannelIndex;
        public List<Texture> SecondaryAlbedoTexs;


        #if AccurateLightTris
            List<(Texture2D texture, NativeArray<Color32> pixels)> EmissionTexPixels;
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

                            var rawData = myTexture2D.GetRawTextureData<Color32>();
                            EmissionTexPixels.Add((myTexture2D, rawData));
                            // DestroyImmediate(myTexture2D);
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

        List<int> IndexOffsets;
        public void CreateAtlas(ref int VertCount, ref int IndexCount)
        {//Creates texture atlas
            #if AccurateLightTris
                if(EmissionTexPixels != null) {
                    int EmissTexLeng = EmissionTexPixels.Count;
                    for(int i = 0; i < EmissTexLeng; i++) {
                        EmissionTexPixels[i].pixels.Dispose();
                    }
                    EmissionTexPixels = null;
                }
                EmissionTexPixels = new List<(Texture2D texture, NativeArray<Color32> pixels)>();
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
            MemorySafeClear<Texture>(ref DiffTransTexs);
            MemorySafeClear<Texture>(ref MatCapTexs);
            MemorySafeClear<Texture>(ref MatCapMasks);
            MemorySafeClear<Texture>(ref SecondaryAlbedoTexs);
            MemorySafeClear<Texture>(ref SecondaryAlbedoTexMasks);
            MemorySafeClear<int>(ref RoughnessTexChannelIndex);
            MemorySafeClear<int>(ref MetallicTexChannelIndex);
            MemorySafeClear<int>(ref AlphaTexChannelIndex);
            MemorySafeClear<int>(ref DiffTransTexChannelIndex);
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

                obj.matfill();
                if(mesh == null) Debug.LogError("Missing Mesh: " + name);
                VertCount += mesh.vertexCount;
                int submeshcount = mesh.subMeshCount;
                int TempCount = 0;
                for(int i2 = 0; i2 < submeshcount; i2++) {
                    TempCount += (int)mesh.GetIndexCount(i2);
                }
                IndexCount += TempCount;
                IndexOffsets.Add(TempCount);

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
                                CurMat.Textures.AlbedoTex.x = AlbedoTexs.Count;
                            }
                        }
                        AssetManager.Assets.AddMaterial(SharedMaterials[i].shader);
                        Index = AssetManager.ShaderNames.IndexOf(SharedMaterials[i].shader.name);
                    }
                    MaterialShader RelevantMat = AssetManager.data.Material[Index];
                    if(!RelevantMat.MetallicRange.Equals("null") && JustCreated) obj.LocalMaterials[i].Metallic = SharedMaterials[i].GetFloat(RelevantMat.MetallicRange);
                    if(!RelevantMat.RoughnessRange.Equals("null") && JustCreated) obj.LocalMaterials[i].Roughness = (RelevantMat.UsesSmoothness ? (1.0f - SharedMaterials[i].GetFloat(RelevantMat.RoughnessRange)) : SharedMaterials[i].GetFloat(RelevantMat.RoughnessRange));
                    if(RelevantMat.MetallicRemapMin != null && !RelevantMat.MetallicRemapMin.Equals("null") && JustCreated) obj.LocalMaterials[i].MetallicRemap = new Vector2(SharedMaterials[i].GetFloat(RelevantMat.MetallicRemapMin), SharedMaterials[i].GetFloat(RelevantMat.MetallicRemapMax));
                    else if(JustCreated) obj.LocalMaterials[i].MetallicRemap = new Vector2(0, 1);
                    if(RelevantMat.RoughnessRemapMin != null && !RelevantMat.RoughnessRemapMin.Equals("null") && JustCreated) obj.LocalMaterials[i].RoughnessRemap = new Vector2(SharedMaterials[i].GetFloat(RelevantMat.RoughnessRemapMin), SharedMaterials[i].GetFloat(RelevantMat.RoughnessRemapMax));
                    else if(JustCreated) obj.LocalMaterials[i].RoughnessRemap = new Vector2(0, 1);
                    if(JustCreated || obj.LocalMaterials[i].DiffTransRemap.x == 0 && obj.LocalMaterials[i].DiffTransRemap.y == 0) obj.LocalMaterials[i].DiffTransRemap = new Vector2(0, 1);
                    if(!RelevantMat.BaseColorValue.Equals("null") && JustCreated) obj.LocalMaterials[i].BaseColor = (Vector3)((Vector4)SharedMaterials[i].GetColor(RelevantMat.BaseColorValue));
                    else if(JustCreated) obj.LocalMaterials[i].BaseColor = new Vector3(1,1,1);
                    if(RelevantMat.EmissionColorValue != null && !RelevantMat.EmissionColorValue.Equals("null") && JustCreated) obj.LocalMaterials[i].EmissionColor = (Vector3)((Vector4)SharedMaterials[i].GetColor(RelevantMat.EmissionColorValue));
                    else if(JustCreated) obj.LocalMaterials[i].EmissionColor = new Vector3(1,1,1);
                    if(RelevantMat.EmissionIntensityValue != null && !RelevantMat.EmissionIntensityValue.Equals("null") && JustCreated) obj.LocalMaterials[i].emission = (SharedMaterials[i].GetFloat(RelevantMat.EmissionIntensityValue));
                    else if(JustCreated) obj.LocalMaterials[i].emission = 0;
                    if(RelevantMat.IsGlass && JustCreated || (JustCreated && RelevantMat.Name.Equals("Standard") && SharedMaterials[i].GetFloat("_Mode") == 3)) obj.LocalMaterials[i].SpecTrans = 1f;
                    if(RelevantMat.IsCutout || (RelevantMat.Name.Equals("Standard") && SharedMaterials[i].GetFloat("_Mode") == 1)) obj.LocalMaterials[i].MatType = (int)RayTracingObject.Options.Cutout;

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
                                if(JustCreated) TextureParseScaleOffset(SharedMaterials[i], TexName, ref obj.LocalMaterials[i].TextureModifiers.SecondaryNormalTexScaleOffset);
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref SecondaryNormalTexs, ref TempIndex); 
                                CurMat.Textures.SecondaryNormalTex.x = TempIndex;
                            break;                            
                            case(TexturePurpose.SecondaryAlbedoTextureMask):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref SecondaryAlbedoTexMasks, ref TempIndex); 
                                CurMat.Textures.SecondaryAlbedoMask.x = TempIndex; 
                                if(Result == 1) SecondaryAlbedoTexMaskChannelIndex.Add(ReadIndex);
                            break;
                            case(TexturePurpose.SecondaryAlbedoTexture):
                                if(JustCreated) TextureParseScaleOffset(SharedMaterials[i], TexName, ref obj.LocalMaterials[i].TextureModifiers.SecondaryAlbedoTexScaleOffset);
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref SecondaryAlbedoTexs, ref TempIndex); 
                                CurMat.Textures.SecondaryAlbedoTex.x = TempIndex;
                            break;
                            case(TexturePurpose.MatCapTex):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref MatCapTexs, ref TempIndex); 
                                CurMat.Textures.MatCapTex.x = TempIndex;
                            break;
                            case(TexturePurpose.Albedo):
                                if(JustCreated) TextureParseScaleOffset(SharedMaterials[i], TexName, ref obj.LocalMaterials[i].TextureModifiers.MainTexScaleOffset);
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref AlbedoTexs, ref TempIndex); 
                                CurMat.Textures.AlbedoTex.x = TempIndex;
                            break;
                            case(TexturePurpose.Normal):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref NormalTexs, ref TempIndex); 
                                CurMat.Textures.NormalTex.x = TempIndex;
                            break;
                            case(TexturePurpose.Emission):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref EmissionTexs, ref TempIndex, true); 
                                CurMat.Textures.EmissiveTex.x = TempIndex; 
                                if(Result != 2 && JustCreated && obj.LocalMaterials[i].emission == 0) obj.LocalMaterials[i].emission = 12.0f;
                            break;
                            case(TexturePurpose.Metallic):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref MetallicTexs, ref TempIndex); 
                                CurMat.Textures.MetallicTex.x = TempIndex; 
                                if(Result == 1) MetallicTexChannelIndex.Add(ReadIndex);
                            break;
                            case(TexturePurpose.Roughness):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref RoughnessTexs, ref TempIndex); 
                                CurMat.Textures.RoughnessTex.x = TempIndex; 
                                if(Result == 1) RoughnessTexChannelIndex.Add(ReadIndex);
                            break;
                            case(TexturePurpose.Alpha):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref AlphaTexs, ref TempIndex); 
                                CurMat.Textures.AlphaTex.x = TempIndex; 
                                if(Result == 1) AlphaTexChannelIndex.Add(ReadIndex);
                            break;
                            case(TexturePurpose.DiffTransTex):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref DiffTransTexs, ref TempIndex); 
                                CurMat.Textures.DiffTransTex.x = TempIndex; 
                                if(Result == 1) DiffTransTexChannelIndex.Add(ReadIndex);
                            break;
                            case(TexturePurpose.MatCapMask):
                                Result = TextureParse(ref TempScale, SharedMaterials[i], TexName, ref MatCapMasks, ref TempIndex); 
                                CurMat.Textures.MatCapMask.x = TempIndex; 
                                if(Result == 1) MatCapMaskChannelIndex.Add(ReadIndex);
                            break;
                        }
                    }

                    if(obj.LocalMaterials[i].TextureModifiers.MainTexScaleOffset.x == 0) obj.LocalMaterials[i].TextureModifiers.MainTexScaleOffset = new Vector4(1,1,0,0);
                    if(obj.LocalMaterials[i].TextureModifiers.SecondaryTextureScaleOffset.x == 0) obj.LocalMaterials[i].TextureModifiers.SecondaryTextureScaleOffset = new Vector4(1,1,0,0);
                    if(obj.LocalMaterials[i].TextureModifiers.NormalTexScaleOffset.x == 0) obj.LocalMaterials[i].TextureModifiers.NormalTexScaleOffset = new Vector4(1,1,0,0);
                    if(obj.LocalMaterials[i].TextureModifiers.SecondaryAlbedoTexScaleOffset.x == 0) obj.LocalMaterials[i].TextureModifiers.SecondaryAlbedoTexScaleOffset = new Vector4(1,1,0,0);
                    if(obj.LocalMaterials[i].TextureModifiers.SecondaryNormalTexScaleOffset.x == 0) obj.LocalMaterials[i].TextureModifiers.SecondaryNormalTexScaleOffset = new Vector4(1,1,0,0);

                    if(JustCreated && obj.LocalMaterials[i].EmissionColor.x == 0 && obj.LocalMaterials[i].EmissionColor.y == 0 && obj.LocalMaterials[i].EmissionColor.z == 0) obj.LocalMaterials[i].EmissionColor = new Vector3(1,1,1);
                    if(JustCreated) obj.LocalMaterials[i].Tag = CommonFunctions.SetFlagVar(obj.LocalMaterials[i].Tag, CommonFunctions.Flags.UseSmoothness, RelevantMat.UsesSmoothness);
                    if(obj.LocalMaterials[i].Hue == 0 && obj.LocalMaterials[i].Saturation == 0 && obj.LocalMaterials[i].Contrast == 0 && obj.LocalMaterials[i].Brightness == 0 && obj.LocalMaterials[i].BlendColor == Vector3.zero) {
                        obj.LocalMaterials[i].Saturation = 1;
                        obj.LocalMaterials[i].Contrast = 1;
                        obj.LocalMaterials[i].Brightness = 1;
                    }
                    if(JustCreated || !CommonFunctions.GetFlag(obj.LocalMaterials[i].Tag, CommonFunctions.Flags.EnableCausticGeneration)) obj.LocalMaterials[i].CausticStrength = 1.0f;
                    if(JustCreated || (obj.LocalMaterials[i].ColorBleed == 0.0f && obj.LocalMaterials[i].EmissionColor.x == 0 && obj.LocalMaterials[i].EmissionColor.y == 0 && obj.LocalMaterials[i].EmissionColor.z == 0)) obj.LocalMaterials[i].ColorBleed = 1.0f;
                    CurMat.MatData = obj.LocalMaterials[i];
                    CurMat.MatData.BaseColor = (!obj.UseKelvin[i]) ? obj.LocalMaterials[i].BaseColor : new Vector3(Mathf.CorrelatedColorTemperatureToRGB(obj.KelvinTemp[i]).r, Mathf.CorrelatedColorTemperatureToRGB(obj.KelvinTemp[i]).g, Mathf.CorrelatedColorTemperatureToRGB(obj.KelvinTemp[i]).b);
                    if(i == obj.LocalMaterials.Length - 1) obj.JustCreated = false;
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
            CommonFunctions.DeepClean(ref LightTriNorms);
            CommonFunctions.DeepClean(ref CachedTransforms);
            CommonFunctions.DeepClean(ref TransformIndexes);
            CommonFunctions.DeepClean(ref LuminanceWeights);
            GlobalTriOffset = -1;
            GlobalNodeOffset = -1;
            GlobalLightTriOffset = -1;
            GlobalLightNodeOffset = -1;
            GlobalLightNodeSkinnedOffset = -1;
            HasLightTriangles = false;
            NeedsToResetBuffers = true;
            ClearAll();
            AllFull = false;
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
                            if(TempRayObj.enabled && !Target.TryGetComponent<ParentObject>(out ParentObject Paren2)) {
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
            int IndexCount = 0;
            IndexOffsets = new List<int>();
            CreateAtlas(ref VertCount, ref IndexCount);

            // LoadFile();
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

            CurMeshData.init(VertCount, IndexCount);

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
                int PreIndexLength = CurMeshData.CurIndexOffset;
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

                    NativeArray<int>.Copy(mesh.triangles, 0, CurMeshData.IndicesArray, CurMeshData.CurIndexOffset, IndexOffsets[i]);
                    CurMeshData.CurIndexOffset += IndexOffsets[i];
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
                            if(mesh.indexFormat == IndexFormat.UInt32) CurMeshData.Indices[CurMeshData.CurIndexOffset + i2] = ((int)BitConverter.ToUInt32(Data, (i2 * 4)));
                            else CurMeshData.Indices[CurMeshData.CurIndexOffset + i2] = ((int)BitConverter.ToUInt16(Data, (i2 * 2)));
                        }
                        CurMeshData.CurIndexOffset += DatCoun;
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
                    VertexCount = CurMeshData.CurIndexOffset - PreIndexLength,
                    IndexOffset = IndexOffset,
                    IndexOffsetEnd = CurMeshData.CurVertexOffset
                });
                RepCount += Mathf.Min(submeshcount, CurrentObject.Names.Length);
            }
        }

        public int TotalCounter = 0;
        public List<Layer2> WorkingSetCWBVH;
        unsafe public void DocumentNodes(int CurrentNode, int CurRecur) {
            MaxRecur = Mathf.Max(MaxRecur, CurRecur);
            BVHNode8Data node = BVH.BVH8Nodes[CurrentNode];
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

            int GetSplitCount(float priority, float totalPriority, int triangleCount)
            {
                float shareOfTris = priority / totalPriority * triangleCount;
                int splitCount = 1 + (int)(shareOfTris * 0.4f);

                return splitCount;
            }

            public float LargestExtent(ref Vector3 Sizes) {
                float Lorgest = Sizes.x;
                if(Sizes.y > Lorgest) Lorgest = Sizes.y;
                if(Sizes.z > Lorgest) Lorgest = Sizes.z;
                return Lorgest;
            }

            public float HalfArea(ref Vector3 d) {
                return (d.x + d.y) * d.z + d.x * d.y;
            }
            float Length(Vector3 a) {return (float)Math.Sqrt((double)(a.x * a.x + a.y * a.y + a.z * a.z));}


            float Priority(AABB triBox, CudaTriangle triangle)
            {
                Vector3 Sizes = triBox.BBMax - triBox.BBMin;
                return (float)System.Math.Pow(LargestExtent(ref Sizes) * (HalfArea(ref Sizes) * 2.0f - Length(Vector3.Cross(triangle.posedge1, triangle.posedge2)) * 0.5f), 1f / 3f);
            }
            public struct SplitData {
                public AABB box;
                public int splitsLeft;
            }


        unsafe public void Construct()
        {
            NativeArray<int> ReverseIndexesLightCounterArray = default;
            int* ReverseIndexesLightCounter = default;
#if TTTriSplitting && !HardwareRT
            if(!IsSkinnedGroup && !IsDeformable) {
#if TTExtraVerbose && TTVerbose
                MainWatch.Start();
#endif
                int Coun = AggTriangles.Length;
                int Splits = 0;
                Vector3 globalSize = aabb_untransformed.BBMax - aabb_untransformed.BBMin;

                CudaTriangle triangle;
                float totalPriority = 0.0f;
                NativeArray<float> PrioritiesArray = new NativeArray<float>(Coun, Unity.Collections.Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                float* Priorities = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(PrioritiesArray);

                for (int i = 0; i < Coun; i++) {
                    triangle = AggTriangles[i];
                    Priorities[i] = Priority(new AABB(triangle), triangle);
                    totalPriority += Priorities[i];
                }

                int referenceCount = 0;
                for (int i = 0; i < Coun; i++) {
                    int splitCount = GetSplitCount(Priorities[i], totalPriority, Coun);
                    referenceCount += splitCount;
                }
                PrioritiesArray.Dispose();

                Splits = referenceCount - Coun;
                TrianglesArray = new NativeArray<AABB>(referenceCount, Unity.Collections.Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                Triangles = (AABB*)NativeArrayUnsafeUtility.GetUnsafePtr(TrianglesArray);

                NativeArray<CudaTriangle> NewTrisArray = new NativeArray<CudaTriangle>(referenceCount, Unity.Collections.Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                CudaTriangle* NewTris = (CudaTriangle*)NativeArrayUnsafeUtility.GetUnsafePtr(NewTrisArray);

                int Coun2 = LightTriangles.Count;
                bool HasLightTriangles = Coun2 > 0;
                NativeArray<int> ReverseIndexesArray = default;
                int* ReverseIndexes = default;
                NativeArray<int> ReverseIndexesCounterArray = default;
                int* ReverseIndexesCounter = default;
                if(HasLightTriangles) {
                    ReverseIndexesLightCounterArray = new NativeArray<int>(Coun2, Unity.Collections.Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                    ReverseIndexesLightCounter = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(ReverseIndexesLightCounterArray);
                    ReverseIndexesArray = new NativeArray<int>(Coun, Unity.Collections.Allocator.Persistent, NativeArrayOptions.ClearMemory);
                    ReverseIndexes = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(ReverseIndexesArray);
                    ReverseIndexesCounterArray = new NativeArray<int>(Coun, Unity.Collections.Allocator.Persistent, NativeArrayOptions.ClearMemory);
                    ReverseIndexesCounter = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(ReverseIndexesCounterArray);
                }
                int counter = 0;

                SplitData[] stack = new SplitData[64];
                for (int i = 0; i < Coun; i++) {
                    triangle = AggTriangles[i];
                    AABB triBox = new AABB(triangle);

                    float priority = Priority(triBox, triangle);
                    int splitCount = GetSplitCount(priority, totalPriority, Coun);

                    int stackPtr = 0;
                    stack[stackPtr++] = new SplitData() {box = triBox, splitsLeft = splitCount};
                    while (stackPtr > 0) {
                        SplitData TempSplit = stack[--stackPtr];

                        if (TempSplit.splitsLeft == 1) {
                            Triangles[counter] = TempSplit.box;
                            NewTris[counter] = triangle;
                            if(HasLightTriangles) {
                                if(ReverseIndexes[i] == 0) ReverseIndexes[i] = counter + 1;
                                ReverseIndexesCounter[i]++;
                            }
                            counter++;
                            continue;
                        }

                        int splitAxis = TempSplit.box.LargestAxis();
                        float largestExtent = (TempSplit.box.BBMax - TempSplit.box.BBMin)[splitAxis];

                        float depth = (float)System.Math.Min(-1.0f, (float)System.Math.Floor((float)System.Math.Log(largestExtent / globalSize[splitAxis], 2)));
                        float cellSize = (float)System.Math.Pow(2f, depth) * globalSize[splitAxis];
                        
                        if (cellSize + 0.0001f >= largestExtent) {
                            cellSize *= 0.5f;
                        }

                        float midPos = (TempSplit.box.BBMin[splitAxis] + TempSplit.box.BBMax[splitAxis]) * 0.5f;
                        float splitPos = aabb_untransformed.BBMin[splitAxis] + (float)System.Math.Round((midPos - aabb_untransformed.BBMin[splitAxis]) / cellSize) * cellSize;
                        if (splitPos <= TempSplit.box.BBMin[splitAxis] || splitPos >= TempSplit.box.BBMax[splitAxis]) {
                            splitPos = midPos;
                        }

                        AABB[] lrBox = triangle.Split(splitAxis, splitPos);
                        lrBox[0].ShrinkToFit(TempSplit.box);
                        lrBox[1].ShrinkToFit(TempSplit.box);

                        float leftExtent = lrBox[0].LargestExtent(lrBox[0].LargestAxis());
                        float rightExtent = lrBox[1].LargestExtent(lrBox[1].LargestAxis());
                        
                        int leftCount = (int)(float)System.Math.Round(TempSplit.splitsLeft * (leftExtent / (leftExtent + rightExtent)));
                        leftCount = Mathf.Max(leftCount, 1);
                        leftCount = Mathf.Min(TempSplit.splitsLeft - 1, leftCount);

                        int rightCount = TempSplit.splitsLeft - leftCount;

                        SplitData A = new SplitData();
                        A.box = lrBox[0];
                        A.splitsLeft = leftCount;
                        SplitData B = new SplitData();
                        B.box = lrBox[1];
                        B.splitsLeft = rightCount;
                        stack[stackPtr++] = A;
                        stack[stackPtr++] = B;
                    }
                }
                AggTriangles = NewTrisArray.ToArray();
                NewTrisArray.Dispose();

                for(int i = 0; i < referenceCount; i++) {
                    Triangles[i].Validate(ParentScale);
                }
                if(HasLightTriangles) {
                    for(int i = 0; i < Coun2; i++) {
                        LightTriData TempTri = LightTriangles[i];
                        ReverseIndexesLightCounter[i] = ReverseIndexesCounter[TempTri.TriTarget];            
                        TempTri.TriTarget = (uint)ReverseIndexes[TempTri.TriTarget] - 1;
                        LightTriangles[i] = TempTri;    
                    }
                    ReverseIndexesArray.Dispose();
                    ReverseIndexesCounterArray.Dispose();
                }

#if TTExtraVerbose && TTVerbose
                MainWatch.Stop("Triangle Presplitting for " + Splits + " new triangles");
#endif
            }
#endif

            if(LightTriangles.Count > 0) {
#if TTExtraVerbose && TTVerbose
                MainWatch.Start();
#endif
#if TTTriSplitting && !HardwareRT
                LBVH = new LightBVHBuilder(LightTriangles, LightTriNorms, 0.1f, LuminanceWeights, ref AggTriangles, ReverseIndexesLightCounter, IsSkinnedGroup || IsDeformable);
                if(ReverseIndexesLightCounterArray != null && ReverseIndexesLightCounterArray.IsCreated) ReverseIndexesLightCounterArray.Dispose();
#else
                LBVH = new LightBVHBuilder(LightTriangles, LightTriNorms, 0.1f, LuminanceWeights, ref AggTriangles);
#endif
#if TTExtraVerbose && TTVerbose
                MainWatch.Stop("Light BVH for " + LightTriangles.Count + " Emissive triangles");
#endif
            }

            tempAABB = new AABB();
            MaxRecur = 0;
            int PrevLength = TrianglesArray.Length;
            if(BVH2 != null) BVH2.Dispose();
#if TTExtraVerbose && TTVerbose
            MainWatch.Start();
#endif
            BVH2 = new BVH2Builder(Triangles, TrianglesArray.Length);//Binary BVH Builder, and also the component that takes the longest to build
#if TTExtraVerbose && TTVerbose
            MainWatch.Stop("Binary BVH");
#endif
            TrianglesArray.Dispose();
            if(this.BVH != null) this.BVH.Dispose();
#if TTExtraVerbose && TTVerbose
            MainWatch.Start();
#endif
            this.BVH = new BVH8Builder(ref BVH2);
#if TTExtraVerbose && TTVerbose
            MainWatch.Stop("CWBVH Conversion");
#endif
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
                    // Debug.LogError("Allocated: " + BVH.PrevAlloc + ", Actual: " + BVH.cwbvhnode_count + ", Orig Tri Count: " + PrevLength);

            if (IsSkinnedGroup || IsDeformable) {
                WorkingSetCWBVH = new List<Layer2>();
                TotalCounter = 0;
                DocumentNodes(0, 0);
            }

            int LightTriLength = LightTriangles.Count;
            for(int i = 0; i < LightTriLength; i++) {
                LightTriData LT = LightTriangles[i];
                LT.TriTarget = (uint)BVH.cwbvh_indices[LT.TriTarget];
                LightTriangles[i] = LT;
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

        public void RefitMesh(ref ComputeBuffer RealizedAggNodes, ref ComputeBuffer RealizedTriBufferA, ref ComputeBuffer RealizedTriBufferB, ref ComputeBuffer RealizedLightTriBuffer, ComputeBuffer RealizedLightNodeBufferA, ComputeBuffer BoxesBuffer, int BoxesIndex, ComputeBuffer SkinnedMeshAggTriBufferPrev, CommandBuffer cmd)
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
                NodeParentAABBBuffer = new ComputeBuffer(TotalCounter, 24);
                AABBBuffer = new ComputeBuffer(TotalTriangles, 24);
                CWBVHIndicesBuffer = new ComputeBuffer(BVH.cwbvh_indices.Length, 4);
                CWBVHIndicesBuffer.SetData(BVH.cwbvh_indices);
                HasStarted = true;
                if(HasLightTriangles) {
                    Set = new List<int>[LBVH.MaxDepth];
                    WorkingBufferLightBVH = new ComputeBuffer[LBVH.MaxDepth];
                    for(int i = 0; i < LBVH.MaxDepth; i++) Set[i] = new List<int>();
                    Refit(0, 0);
                    for(int i = 0; i < LBVH.MaxDepth; i++) {
                        WorkingBufferLightBVH[i] = new ComputeBuffer(Set[i].Count, 4);
                        WorkingBufferLightBVH[i].SetData(Set[i]);
                    }
                }
                int WorkingLayerCount = WorkingSetCWBVH.Count;
                WorkingBufferCWBVH = new ComputeBuffer[WorkingLayerCount];
                for (int i = 0; i < WorkingLayerCount; i++) {
                    WorkingBufferCWBVH[i] = new ComputeBuffer(WorkingSetCWBVH[i].Slab.Count, 4);
                    WorkingBufferCWBVH[i].SetData(WorkingSetCWBVH[i].Slab);
                }
                int VertexBufferCount = VertexBuffers.Length;
                for (int i = 0; i < VertexBufferCount; i++) {
                    if (IndexBuffers[i] != null) IndexBuffers[i].Release();
                    int[] IndexBuffer = IsSkinnedGroup ? SkinnedMeshes[i].sharedMesh.triangles : DeformableMeshes[i].sharedMesh.triangles;
                    IndexBuffers[i] = new ComputeBuffer(IndexBuffer.Length, 4, ComputeBufferType.Raw);
                    IndexBuffers[i].SetData(IndexBuffer);
                }

            }
            else if (AllFull)
            {
                int VertLength = VertexBuffers.Length;
                for (int i = 0; i < VertLength; i++) {
                    VertexBuffers[i]?.Release();
                    if(IsSkinnedGroup) {
                        SkinnedMeshes[i].vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                        VertexBuffers[i] = SkinnedMeshes[i].GetVertexBuffer();
                    } else {
                        DeformableMeshes[i].sharedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                        VertexBuffers[i] = DeformableMeshes[i].sharedMesh.GetVertexBuffer(0);
                    }
                }
                cmd.SetComputeIntParam(MeshRefit, "TriBuffOffset", GlobalTriOffset);
                cmd.SetComputeIntParam(MeshRefit, "LightTriBuffOffset", GlobalLightTriOffset);
                cmd.SetComputeIntParam(MeshRefit, "SkinnedOffset", GlobalSkinnedOffset);
                if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ReMesh");
                if(HasLightTriangles) {
                    cmd.SetComputeBufferParam(MeshRefit, LightBLASRefitKernel, "LightTriangles", RealizedLightTriBuffer);
                    cmd.SetComputeBufferParam(MeshRefit, LightBLASRefitKernel, "CudaTriArrayINA", RealizedTriBufferA);
                }
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "Boxs", AABBBuffer);
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "CudaTriArrayA", RealizedTriBufferA);
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "SkinnedTriBuffer", SkinnedMeshAggTriBufferPrev);
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "CudaTriArrayB", RealizedTriBufferB);
                cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "CWBVHIndices", CWBVHIndicesBuffer);
                cmd.SetComputeBufferParam(MeshRefit, RefitLayerKernel, "NodeTotalBounds", NodeParentAABBBuffer);
                cmd.SetComputeBufferParam(MeshRefit, RefitLayerKernel, "Boxs", AABBBuffer);
                cmd.SetComputeBufferParam(MeshRefit, RefitLayerKernel, "AggNodes", RealizedAggNodes);
                int CurVertOffset = 0;
                if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ReMesh Accum");

                for (int i = 0; i < TotalObjects; i++)
                {
                    if(VertexBuffers[i] == null || !(VertexBuffers[i].IsValid())) {
                        AllFull = false;
                        return;
                    }
                    var SkinnedRootBone = IsSkinnedGroup ? SkinnedMeshes[i].rootBone : this.transform;
                    if(SkinnedRootBone == null && !IsDeformable) SkinnedRootBone = SkinnedMeshes[i].gameObject.transform;
                    int IndexCount = IndexCounts[i];

                    cmd.SetComputeIntParam(MeshRefit, "Stride", VertexBuffers[i].stride / 4);
                    if(IsSkinnedGroup) {
                        if(SkinnedRootBone != null) {
                            cmd.SetComputeMatrixParam(MeshRefit, "Transform", transform.worldToLocalMatrix * Matrix4x4.TRS(SkinnedRootBone.position, SkinnedRootBone.rotation, Vector3.one ));
                            cmd.SetComputeVectorParam(MeshRefit, "Offset", SkinnedRootBone.localPosition);
                        } else {
                            cmd.SetComputeMatrixParam(MeshRefit, "Transform", transform.worldToLocalMatrix * Matrix4x4.TRS(SkinnedMeshes[i].gameObject.transform.position, SkinnedMeshes[i].gameObject.transform.rotation, SkinnedMeshes[i].gameObject.transform.lossyScale ));
                            cmd.SetComputeVectorParam(MeshRefit, "Offset", SkinnedMeshes[i].gameObject.transform.localPosition);
                        }
                    } else {
                        cmd.SetComputeMatrixParam(MeshRefit, "Transform", Matrix4x4.identity);
                        cmd.SetComputeVectorParam(MeshRefit, "Offset", SkinnedRootBone.localPosition);
                    }
    
                    cmd.SetComputeIntParam(MeshRefit, "VertOffset", CurVertOffset);
                    cmd.SetComputeIntParam(MeshRefit, "gVertexCount", IndexCount);
                    cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "bufVertices", VertexBuffers[i]);
                    cmd.SetComputeBufferParam(MeshRefit, ConstructKernel, "bufIndexes", IndexBuffers[i]);
                    // if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ReMesh Accum " + i + " : " + IndexCount);
                    cmd.DispatchCompute(MeshRefit, ConstructKernel, (int)Mathf.Ceil(IndexCount / (float)KernelRatio), 1, 1);
                    // if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ReMesh Accum " + i + " : " + IndexCount);
                    CurVertOffset += IndexCount;
                }

                if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ReMesh Accum");


                if(LightTriangles.Count != 0) {
                    if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("LightRefitter");
                    var A1 = GlobalLightNodeOffset;
                    cmd.SetComputeIntParam(MeshRefit, "TotalNodeOffset", A1);
                    cmd.SetComputeMatrixParam(MeshRefit, "ToWorld", transform.localToWorldMatrix);
                    cmd.SetComputeBufferParam(MeshRefit, LightBLASRefitKernel, "LightNodesWrite", RealizedLightNodeBufferA);
                    cmd.SetComputeBufferParam(MeshRefit, LightBLASCopyKernel, "LightNodesWriteB", RealizedLightNodeBufferA);
                    cmd.SetComputeFloatParam(MeshRefit, "FloatMax", float.MaxValue);
                    if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("LightCopier");
                        cmd.SetComputeIntParam(MeshRefit, "TargetOffset", GlobalLightNodeSkinnedOffset);
                        cmd.SetComputeIntParam(MeshRefit, "Count", LocalLightNodeCount);
                        cmd.DispatchCompute(MeshRefit, LightBLASCopyKernel, (int)Mathf.Ceil(LocalLightNodeCount / (float)256.0f), 1, 1);
                    if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("LightCopier");


                    int ObjectOffset = GlobalLightNodeOffset;
                    for(int i = WorkingBufferLightBVH.Length - 1; i >= 0; i--) {
                        var ObjOffVar = ObjectOffset;
                        var SetCount = WorkingBufferLightBVH[i].count;
                        cmd.SetComputeIntParam(MeshRefit, "SetCount", SetCount);
                        cmd.SetComputeIntParam(MeshRefit, "ObjectOffset", ObjOffVar);
                        cmd.SetComputeBufferParam(MeshRefit, LightBLASRefitKernel, "WorkingSet", WorkingBufferLightBVH[i]);
                        cmd.DispatchCompute(MeshRefit, LightBLASRefitKernel, (int)Mathf.Ceil(SetCount / (float)256.0f), 1, 1);

                        ObjectOffset += SetCount;
                    }
                    if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("LightRefitter");

                }

                #if !HardwareRT
                    if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ReMesh Refit");
                    cmd.SetComputeIntParam(MeshRefit, "NodeOffset", GlobalNodeOffset);
                    for (int i = MaxRecur; i >= 0; i--) {
                        if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ReMesh Refit: " + i);
                        var NodeCount2 = WorkingBufferCWBVH[i].count;
                        cmd.SetComputeIntParam(MeshRefit, "NodeCount", NodeCount2);
                        cmd.SetComputeBufferParam(MeshRefit, RefitLayerKernel, "WorkingBuffer", WorkingBufferCWBVH[i]);
                        cmd.DispatchCompute(MeshRefit, RefitLayerKernel, (int)Mathf.Ceil(NodeCount2 / (float)128), 1, 1);
                        if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ReMesh Refit: " + i);
                    }
                    if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ReMesh Refit");
                    if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ReMesh Global AABB Update");
                        cmd.SetComputeIntParam(MeshRefit, "TLASBoxesInput", BoxesIndex);
                        cmd.SetComputeBufferParam(MeshRefit, UpdateGlobalBufferAABBKernel, "Boxs", BoxesBuffer);
                        cmd.SetComputeBufferParam(MeshRefit, UpdateGlobalBufferAABBKernel, "NodeTotalBounds", NodeParentAABBBuffer);
                        cmd.DispatchCompute(MeshRefit, UpdateGlobalBufferAABBKernel, 1, 1, 1);
                    if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ReMesh Global AABB Update");
                #endif
                if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ReMesh");
            }

            if (!AllFull) {
                int VertLength = VertexBuffers.Length;
                for (int i = 0; i < VertLength; i++)
                {
                    if(IsSkinnedGroup) {
                        SkinnedMeshes[i].vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                        VertexBuffers[i] = SkinnedMeshes[i].GetVertexBuffer();
                        if(!SkinnedMeshes[i].gameObject.activeInHierarchy) {
                            NeedsToUpdate = true;
                            if((QueInProgress == 0 || QueInProgress == 1) && AssetManager.Assets != null && AssetManager.Assets.UpdateQue != null && !AssetManager.Assets.UpdateQue.Contains(this)) {
                                QueInProgress = 2;
                                AssetManager.Assets.UpdateQue.Add(this);
                            }
                        }
                    } else {
                        DeformableMeshes[i].sharedMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                        VertexBuffers[i] = DeformableMeshes[i].sharedMesh.GetVertexBuffer(0);
                    }
                }
                if (!((new List<GraphicsBuffer>(VertexBuffers)).Contains(null))) AllFull = true;
            }
        }

        public void ForceUpdateSkinnedAABB(ComputeBuffer BoxesBuffer, int BoxesIndex, CommandBuffer cmd) {
            #if !HardwareRT
                if(RayTracingMaster.DoKernelProfiling) cmd.BeginSample("ReMesh Global AABB Update");
                    cmd.SetComputeIntParam(MeshRefit, "TLASBoxesInput", BoxesIndex);
                    cmd.SetComputeBufferParam(MeshRefit, UpdateGlobalBufferAABBKernel, "Boxs", BoxesBuffer);
                    cmd.SetComputeBufferParam(MeshRefit, UpdateGlobalBufferAABBKernel, "NodeTotalBounds", NodeParentAABBBuffer);
                    cmd.DispatchCompute(MeshRefit, UpdateGlobalBufferAABBKernel, 1, 1, 1);
                if(RayTracingMaster.DoKernelProfiling) cmd.EndSample("ReMesh Global AABB Update");
            #endif
        }


        private float luminance(float r, float g, float b) { return 0.299f * r + 0.587f * g + 0.114f * b; }
        private float luminance(Vector3 A) { return Vector3.Dot(new Vector3(0.299f, 0.587f, 0.114f), A);}


        public unsafe async Task BuildTotal() {
#if TTExtraVerbose && TTVerbose
            MainWatch = new TTStopWatch(Name);
#endif
            // if(HasCompleted) return;
            int IllumTriCount = 0;
            CudaTriangle TempTri = new CudaTriangle();
            Matrix4x4 ParentMatInv = CachedTransforms[0].WTL;
            Matrix4x4 ParentMat = CachedTransforms[0].WTL.inverse;
            Vector3 V1, V2, V3, Norm1, Tan1;
#if TTTriSplitting
    aabb_untransformed = new AABB();
    aabb_untransformed.init();
    if(IsSkinnedGroup || IsDeformable) {
#endif
            TrianglesArray = new NativeArray<AABB>((TransformIndexes[TransformIndexes.Count - 1].VertexStart + TransformIndexes[TransformIndexes.Count - 1].VertexCount) / 3, Unity.Collections.Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            Triangles = (AABB*)NativeArrayUnsafeUtility.GetUnsafePtr(TrianglesArray);
#if TTTriSplitting
    }
#endif
#if TTExtraVerbose && TTVerbose
            MainWatch.Start();
#endif
            AggTriangles = new CudaTriangle[(TransformIndexes[TransformIndexes.Count - 1].VertexStart + TransformIndexes[TransformIndexes.Count - 1].VertexCount) / 3];
            int OffsetReal = 0;
            aabb_untransformed = new AABB();
            aabb_untransformed.init();
            for (int i = 0; i < TotalObjects; i++) {//Transforming so all child objects are in the same object space
                Matrix4x4 ChildMat = CachedTransforms[i + 1].WTL.inverse;
                Matrix4x4 TransMat = ParentMatInv * ChildMat;
                Vector3 Ofst = CachedTransforms[i + 1].WTL * CachedTransforms[i + 1].Position;
                Vector3 Ofst2 = ParentMatInv * CachedTransforms[0].Position;
                int IndexOffset = TransformIndexes[i].IndexOffset;
                int IndexEnd = TransformIndexes[i].VertexStart + TransformIndexes[i].VertexCount;
                OffsetReal = TransformIndexes[i].VertexStart / 3;
                bool IsSingle = CachedTransforms[i + 1].WTL.inverse == ParentMat;
                float scalex = Length(ChildMat * new Vector3(1,0,0));
                float scaley = Length(ChildMat * new Vector3(0,1,0));
                float scalez = Length(ChildMat * new Vector3(0,0,1));
                float Leng = Length(new Vector3(scalex, scaley, scalez));
                scalex /= Leng;
                scaley /= Leng;
                scalez /= Leng;
                Vector3 ScaleFactor = IsSingle ? new Vector3(1,1,1) : new Vector3((float)System.Math.Pow(1.0f / scalex, 2.0f), (float)System.Math.Pow(1.0f / scaley, 2.0f), (float)System.Math.Pow(1.0f / scalez, 2.0f));
                int InitOff = TransformIndexes[i].IndexOffset;
                int IndEnd = TransformIndexes[i].IndexOffsetEnd;
                for (int i3 = InitOff; i3 < IndEnd; i3++) {
                    V1 = CurMeshData.Verticies[i3] + Ofst;
                    V1 = TransMat * V1;
                    CurMeshData.Verticies[i3] = V1 - Ofst2;

                    Tan1 = TransMat.MultiplyVector((Vector3)CurMeshData.Tangents[i3]);
                    Normalize(ref Tan1);
                    CurMeshData.TangentsArray.ReinterpretStore(i3, CommonFunctions.PackOctahedral(Tan1));

                    Norm1 = new Vector3(CurMeshData.Normals[i3].x * ScaleFactor.x, CurMeshData.Normals[i3].y * ScaleFactor.y, CurMeshData.Normals[i3].z * ScaleFactor.z);
                    Norm1 = TransMat.MultiplyVector(Norm1);
                    // Norm1 = TransMat * Scale(ScaleFactor, CurMeshData.Normals[i3]);
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
#if TTTriSplitting
    if(IsSkinnedGroup || IsDeformable) {
#endif
                    Triangles[OffsetReal].Create(V1, V2);
                    Triangles[OffsetReal].Extend(V3);
                    Triangles[OffsetReal].Validate(ParentScale);
                    aabb_untransformed.Extend(ref Triangles[OffsetReal]);
#if TTTriSplitting
    } else {
                    aabb_untransformed.Extend(V1);
                    aabb_untransformed.Extend(V2);
                    aabb_untransformed.Extend(V3);
    }
#endif
                    if (_Materials[(int)TempTri.MatDat].MatData.emission > 0.0f) {
                        bool IsValid = true;
                        #if AccurateLightTris
                            if(_Materials[(int)TempTri.MatDat].Textures.EmissiveTex.x != 0) {
                                int ThisIndex = _Materials[(int)TempTri.MatDat].Textures.EmissiveTex.x - 1;
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
                                    if(!(EmissionTexPixels[ThisIndex].pixels[UVIndex3].r < 0.01f && EmissionTexPixels[ThisIndex].pixels[UVIndex3].g < 0.01f && EmissionTexPixels[ThisIndex].pixels[UVIndex3].b < 0.01f)) FoundTrue = true;
                                    // else SecondaryBaseCol = new Vector3(EmissionTexPixels[ThisIndex].pixels[UVIndex3].r, EmissionTexPixels[ThisIndex].pixels[UVIndex3].g, EmissionTexPixels[ThisIndex].pixels[UVIndex3].b);
                                }
                                if(UVIndex2 < EmissionTexWidthHeight[ThisIndex].y * EmissionTexWidthHeight[ThisIndex].x){
                                    if(!(EmissionTexPixels[ThisIndex].pixels[UVIndex2].r < 0.01f && EmissionTexPixels[ThisIndex].pixels[UVIndex2].g < 0.01f && EmissionTexPixels[ThisIndex].pixels[UVIndex2].b < 0.01f)) FoundTrue = true;
                                    // else SecondaryBaseCol = new Vector3(EmissionTexPixels[ThisIndex].pixels[UVIndex3].r, EmissionTexPixels[ThisIndex].pixels[UVIndex3].g, EmissionTexPixels[ThisIndex].pixels[UVIndex3].b);
                                }
                                if(UVIndex1 < EmissionTexWidthHeight[ThisIndex].y * EmissionTexWidthHeight[ThisIndex].x){
                                    if(!(EmissionTexPixels[ThisIndex].pixels[UVIndex1].r < 0.01f && EmissionTexPixels[ThisIndex].pixels[UVIndex1].g < 0.01f && EmissionTexPixels[ThisIndex].pixels[UVIndex1].b < 0.01f)) FoundTrue = true;
                                    // else SecondaryBaseCol = new Vector3(EmissionTexPixels[ThisIndex].pixels[UVIndex3].r, EmissionTexPixels[ThisIndex].pixels[UVIndex3].g, EmissionTexPixels[ThisIndex].pixels[UVIndex3].b);
                                }
                                if(UVIndex0 < EmissionTexWidthHeight[ThisIndex].y * EmissionTexWidthHeight[ThisIndex].x){
                                    if(!(EmissionTexPixels[ThisIndex].pixels[UVIndex0].r < 0.01f && EmissionTexPixels[ThisIndex].pixels[UVIndex0].g < 0.01f && EmissionTexPixels[ThisIndex].pixels[UVIndex0].b < 0.01f)) FoundTrue = true;
                                    // else SecondaryBaseCol = new Vector3(EmissionTexPixels[ThisIndex].pixels[UVIndex3].r, EmissionTexPixels[ThisIndex].pixels[UVIndex3].g, EmissionTexPixels[ThisIndex].pixels[UVIndex3].b);
                                }
                                IsValid = FoundTrue;
                            
                            }
                        #endif
                        if(IsValid) {
                            Vector3 Radiance = _Materials[(int)TempTri.MatDat].MatData.emission * _Materials[(int)TempTri.MatDat].MatData.BaseColor;
                            float radiance = luminance(Radiance.x, Radiance.y, Radiance.z);
                            float area = AreaOfTriangle(V1, V2, V3);
                            if(System.Double.IsNaN(area)) continue;
                            if(area != 0 && radiance > 0) {
                                HasLightTriangles = true;
                                // float e = radiance * area;
                                LightTriNorms.Add(((CommonFunctions.UnpackOctahedral(TempTri.norm0) + CommonFunctions.UnpackOctahedral(TempTri.norm1) + CommonFunctions.UnpackOctahedral(TempTri.norm2)) / 3.0f).normalized);
                                LightTriangles.Add(new LightTriData() {
                                    TriTarget = (uint)(OffsetReal),
                                    SourceEnergy = Length(Radiance)
                                    });
                                LuminanceWeights.Add(_Materials[(int)TempTri.MatDat].MatData.emission);//Distance(Vector3.zero, _Materials[(int)TempTri.MatDat].emission * Scale(_Materials[(int)TempTri.MatDat].BaseColor, SecondaryBaseCol)));
                                AggTriangles[OffsetReal].IsEmissive = 1;
                                IllumTriCount++;
                            }
                        }
                    }
                    OffsetReal++;
                }
            }
#if TTExtraVerbose && TTVerbose
          MainWatch.Stop("Format Conversion for " + OffsetReal + " triangles");
#endif
            #if AccurateLightTris
                int EmissTexLeng = EmissionTexPixels.Count;
                for(int i = 0; i < EmissTexLeng; i++) {
                    EmissionTexPixels[i].pixels.Dispose();
                }
                EmissionTexPixels = null;
            #endif
            CurMeshData.Clear();
            aabb_untransformed.Validate(ParentScale);
            center = 0.5f * (aabb_untransformed.BBMin + aabb_untransformed.BBMax);
            extent = 0.5f * (aabb_untransformed.BBMax - aabb_untransformed.BBMin);
            #if !HardwareRT
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
                    Construct();
                    AggNodes = new BVHNode8DataCompressed[BVH.cwbvhnode_count];
                    CommonFunctions.Aggregate(ref AggNodes, BVH);
                    BVH.BVH8NodesArray.Dispose();
                } else {
                    AggNodes = new BVHNode8DataCompressed[1];
                    if(LightTriangles.Count > 0) LBVH = new LightBVHBuilder(LightTriangles, LightTriNorms, 0.1f, LuminanceWeights, ref AggTriangles);
                }
                if(TrianglesArray.IsCreated) TrianglesArray.Dispose();
            #endif
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
        public bool HasTransformChanged = false;
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
            HasTransformChanged = false;
        }

        private unsafe void ConstructAABB() {
            aabb_untransformed = new AABB();
            aabb_untransformed.init();
            int TriLength = TrianglesArray.Length;
            for (int i = 0; i < TriLength; i++) aabb_untransformed.Extend(ref Triangles[i]);
            center = 0.5f * (aabb_untransformed.BBMin + aabb_untransformed.BBMax);
            extent = 0.5f * (aabb_untransformed.BBMax - aabb_untransformed.BBMin);
        }
        public bool BotherToUpdate = true;
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
                        } else if(ExistsInQue == 3 && QueInProgress == -1) {
                            // AssetManager.Assets.AddQue.Add(this);
                            AssetManager.Assets.RemoveQue.Remove(this);
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
            if (BotherToUpdate && gameObject.scene.isLoaded) {
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
                    if(AssetManager.Assets != null && AssetManager.Assets.RemoveQue != null) {
                        if(!AssetManager.Assets.RemoveQue.Contains(this)) {
                            QueInProgress = -1;
                            AssetManager.Assets.RemoveQue.Add(this);
                        }
                        AssetManager.Assets.ParentCountHasChanged = true;
                    }
                }
                HasCompleted = false;
            }
        }

  }


}