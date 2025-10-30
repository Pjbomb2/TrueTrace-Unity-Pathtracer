using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonVars
{
    #pragma warning disable 4014

    [System.Serializable]
    public struct BoundingSphere {
       public Vector3 Center;
       public float Radius;
       
       public void init() {
        Center = Vector3.zero;
        Radius = 0.0f;
       }
       public void Validate(float padding) {
        Radius = Mathf.Max(Radius, padding);
       }

       public void Extend(Vector3 A) {
        Radius = Mathf.Max(Radius, Vector3.Distance(Center, A));
       }
    }

    [System.Serializable]
    public struct LightBVHTransform {
        public Matrix4x4 Transform;
        public int SolidOffset;
    }

    [System.Serializable]
    public struct GaussianTreeNode {
        public BoundingSphere S;
        public uint axis;
        public float variance;
        public float sharpness;
        public float intensity;
        public int left;
    }


    [System.Serializable]
    public struct LightData
    {
        public Vector3 Radiance;
        public Vector3 Position;
        public Vector3 Direction;
        public int Type;
        public Vector2 SpotAngle;
        public float ZAxisRotation;
        public float Softness;
        public Vector2Int IESTex;
        public Vector4 IESTexScale;
    }

    [System.Serializable]
    public struct LightMapTriData
    {
        public Vector3 pos0;
        public Vector3 posedge1;
        public Vector3 posedge2;
        public Vector2 LMUV0;
        public Vector2 LMUV1;
        public Vector2 LMUV2;
        public uint Norm1;
        public uint Norm2;
        public uint Norm3;
    }

    [System.Serializable]
    public struct LightMapData
    {
        public int LightMapIndex;
        public List<LightMapTriData> LightMapTris;
    }

    [System.Serializable]
    public unsafe struct MeshDat
    {
        
        public int CurVertexOffset;
        public Vector3* Verticies;
        public Vector3* Normals;
        public Vector4* Tangents;
        public Vector2* UVs;
        public Color* Colors;
        public int* Indices;
        public Unity.Collections.NativeArray<Vector3> VerticiesArray;
        public Unity.Collections.NativeArray<Vector3> NormalsArray;
        public Unity.Collections.NativeArray<Vector4> TangentsArray;
        public Unity.Collections.NativeArray<Vector2> UVsArray;
        public Unity.Collections.NativeArray<Color> ColorsArray;
        public List<int> MatDat;
        public Unity.Collections.NativeArray<int> IndicesArray;
        public int CurIndexOffset;

        public void SetUvZero(int Count) {
            // for (int i = 0; i < Count; i++) UVs.Add(new Vector2(0.0f, 0.0f));
        }
        public void SetColorsZero(int Count) {
            // Color TempCol = new Color(1,1,1,1);
            // for (int i = 0; i < Count; i++) Colors.Add(TempCol);
        }
        public void SetTansZero(int Count) {
            // for (int i = 0; i < Count; i++) Tangents.Add(new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
        }
        public void init(int StartingSize, int StartingIndexSize) {
            UVsArray = new Unity.Collections.NativeArray<Vector2>(StartingSize, Unity.Collections.Allocator.Persistent, Unity.Collections.NativeArrayOptions.ClearMemory);
            UVs = (Vector2*)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(UVsArray);
            VerticiesArray = new Unity.Collections.NativeArray<Vector3>(StartingSize, Unity.Collections.Allocator.Persistent, Unity.Collections.NativeArrayOptions.UninitializedMemory);
            Verticies = (Vector3*)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(VerticiesArray);
            NormalsArray = new Unity.Collections.NativeArray<Vector3>(StartingSize, Unity.Collections.Allocator.Persistent, Unity.Collections.NativeArrayOptions.UninitializedMemory);
            Normals = (Vector3*)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(NormalsArray);
            TangentsArray = new Unity.Collections.NativeArray<Vector4>(StartingSize, Unity.Collections.Allocator.Persistent, Unity.Collections.NativeArrayOptions.ClearMemory);
            Tangents = (Vector4*)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(TangentsArray);
            ColorsArray = new Unity.Collections.NativeArray<Color>(StartingSize, Unity.Collections.Allocator.Persistent, Unity.Collections.NativeArrayOptions.ClearMemory);
            Colors = (Color*)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(ColorsArray);
            IndicesArray = new Unity.Collections.NativeArray<int>(StartingIndexSize, Unity.Collections.Allocator.Persistent, Unity.Collections.NativeArrayOptions.ClearMemory);
            Indices = (int*)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(IndicesArray);
            MatDat = new List<int>(StartingSize / 3);
            CurVertexOffset = 0;
            CurIndexOffset = 0;
        }
        public void Clear() {
            if (TangentsArray != null) {
                if(NormalsArray.IsCreated) NormalsArray.Dispose();
                if(ColorsArray.IsCreated) ColorsArray.Dispose();
                if(UVsArray.IsCreated) UVsArray.Dispose();
                if(VerticiesArray.IsCreated) VerticiesArray.Dispose();
                if(TangentsArray.IsCreated) TangentsArray.Dispose();
                if(IndicesArray.IsCreated) IndicesArray.Dispose();
                CommonFunctions.DeepClean(ref MatDat);
            }
        }
    }

    [System.Serializable]
    public struct PerInstanceData {
        public Matrix4x4 objectToWorld; // We must specify object-to-world transformation for each instance
        public uint renderingLayerMask;
        public uint CustomInstanceID;
    }

    [System.Serializable]
    public struct IntersectionMatData {//56
        public Vector2Int AlphaTex;//80
        public Vector2Int AlbedoTex;//80
        public int Tag;
        public int MatType;//Can pack into tag
        public float specTrans;
        public float AlphaCutoff;
        public Vector4 AlbedoTexScale;
        public Vector3 surfaceColor;
        public float Rotation;
        public float scatterDistance;
    };

    [System.Serializable]
    public struct MatTextureData
    {
        public Vector2Int AlbedoTex;
        public Vector2Int NormalTex;
        public Vector2Int EmissiveTex;
        public Vector2Int MetallicTex;
        public Vector2Int RoughnessTex;
        public Vector2Int AlphaTex;
        public Vector2Int MatCapMask;
        public Vector2Int MatCapTex;
        public Vector2Int SecondaryAlbedoTex;
        public Vector2Int SecondaryAlbedoMask;
        public Vector2Int SecondaryNormalTex;
        public Vector2Int DiffTransTex;
    }
    [System.Serializable]
    public struct MatTextureModifierData
    {
        public Vector4 MainTexScaleOffset;
        public Vector4 SecondaryTextureScaleOffset;
        public Vector4 NormalTexScaleOffset;
        public Vector4 SecondaryAlbedoTexScaleOffset;
        public Vector4 SecondaryNormalTexScaleOffset;
        public float Rotation;
        public float RotationNormal;
        public float RotationSecondary;
        public float RotationSecondaryDiffuse;
        public float RotationSecondaryNormal;
        public MatTextureModifierData(int a = 0) {
            MainTexScaleOffset = new Vector4(1,1,0,0);
            SecondaryTextureScaleOffset = new Vector4(1,1,0,0);
            NormalTexScaleOffset = new Vector4(1,1,0,0);
            SecondaryAlbedoTexScaleOffset = new Vector4(1,1,0,0);
            SecondaryNormalTexScaleOffset = new Vector4(1,1,0,0);
            Rotation = 0;
            RotationNormal = 0;
            RotationSecondary = 0;
            RotationSecondaryDiffuse = 0;
            RotationSecondaryNormal = 0;
        }
    }

    [System.Serializable]
    public struct RayObjMat
    {
        public MatTextureModifierData TextureModifiers;
        public Vector3 BaseColor;
        public float emission;
        public Vector3 EmissionColor;
        public int Tag;
        public float Roughness;
        public int MatType;
        public Vector3 TransmittanceColor;
        public float IOR;
        public float Metallic;
        public float Sheen;
        public float SheenTint;
        public float SpecularTint;
        public float Clearcoat;
        public float ClearcoatGloss;
        public float Anisotropic;
        public float AnisotropicRotation;
        public float Flatness;
        public float DiffTrans;
        public float SpecTrans;
        public float Specular;
        public float ScatterDist;
        public Vector2 MetallicRemap;
        public Vector2 RoughnessRemap;
        public float AlphaCutoff;
        public float NormalStrength;
        public float Hue;
        public float Saturation;
        public float Contrast;
        public float Brightness;
        public Vector3 BlendColor;
        public float BlendFactor;
        public float ColorBleed;
        public float AlbedoBlendFactor;
        public float SecondaryNormalTexBlend;
        public float DetailNormalStrength;
        public Vector2 DiffTransRemap;
        public Vector3 MatCapColor;
        public float CausticStrength;
    }

    [System.Serializable]
    public struct MaterialData
    {
        public MatTextureData Textures;
        public RayObjMat MatData;
        public MaterialData(int a = 0) {
            Textures = new MatTextureData();
            MatData = new RayObjMat();
        }
    }

    [System.Serializable]
    public struct RayObjectTextureIndex
    {
        public TrueTrace.RayTracingObject Obj;
        public TrueTrace.TerrainObject Terrain;
        public int ObjIndex;
    }

    [System.Serializable]
    public class RayObjects
    {
        public List<RayObjectTextureIndex> RayObjectList = new List<RayObjectTextureIndex>();
    }   

    [System.Serializable]
    public class TexObj
    {
        public Texture Tex;
        public int ReadIndex;
        public List<Vector3Int> TexObjList = new List<Vector3Int>();
    }   
    public enum TexturePurpose {Albedo, Alpha, Normal, Emission, Metallic, Roughness, MatCapTex, MatCapMask, SecondaryAlbedoTexture, SecondaryAlbedoTextureMask, SecondaryNormalTexture, DiffTransTex};

    [System.Serializable]
    public class TexturePairs {
        public int Purpose;
        public int ReadIndex;//negative is the amount of components the destination contains plus 1, for use later with another idea I had
        public string TextureName;
        public TexturePairs Fallback;
    }

    [System.Serializable]
    public class MaterialShader
    {
        public string Name;
        public List<TexturePairs> AvailableTextures;
        public string MetallicRange;
        public string RoughnessRange;
        public bool IsGlass;
        public bool IsCutout;
        public bool UsesSmoothness;
        public string BaseColorValue;
        public string MetallicRemapMin;
        public string MetallicRemapMax;
        public string RoughnessRemapMin;
        public string RoughnessRemapMax;
        public string EmissionColorValue;
        public string EmissionIntensityValue;
        public string MatCapColorValue;
    }
    [System.Serializable]
    public class Materials
    {
        [System.Xml.Serialization.XmlElement("MaterialShader")]
        public List<MaterialShader> Material = new List<MaterialShader>();
    }


    [System.Serializable]
    public class RayObjectDatas
    {
        public int ID;
        public string MatName;
        public int OptionID;
        public RayObjMat MatData;

        public bool UseKelvin;
        public float KelvinTemp;

        public string AlbedoGUID;
        public string MetallicGUID;
        public string RoughnessGUID;
        public string EmissionGUID;
        public string AlphaGUID;
        public string MatCapGUID;
        public string MatcapMaskGUID;
        public string SecondaryAlbedoGUID;
        public string SecondaryAlbedoMaskGUID;
        public string NormalGUID;
        public string DiffTransGUID;
        public string ShaderName;
    }
    [System.Serializable]
    public class RayObjs
    {
        [System.Xml.Serialization.XmlElement("RayObjectDatas")]
        public List<RayObjectDatas> RayObj = new List<RayObjectDatas>();
    }
    [System.Serializable]
    public class RayObjFolder
    {
        public string FolderName = "";
        public List<RayObjectDatas> ContainedPresets = new List<RayObjectDatas>();
    }

    [System.Serializable]
    public class RayObjFolderMaster
    {
        [System.Xml.Serialization.XmlElement("RayObjFolder")]
        public List<RayObjFolder> PresetFolders = new List<RayObjFolder>();
    }





    [System.Serializable]
    public struct BVHNode2Data
    {
        public AABB aabb;
        public int left;
        public uint count;
    }

    [System.Serializable]
    public struct BVHNodeVerbose
    {
        public AABB aabb;
        public uint left;
        public uint right;
        public uint parent;
        public uint count;
        public uint triCount;

        public bool isLeaf() {
            return count != 0;
        }
    }

    [System.Serializable]
    public struct TerrainDat
    {
        public Vector3 PositionOffset;
        public float HeightScale;
        public float TerrainDimX;
        public float TerrainDimY;
        public Vector4 AlphaMap;
        public Vector4 HeightMap;
        public int MatOffset;
    }

    [System.Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    unsafe public struct BVHNode8Data
    {
        [System.Runtime.InteropServices.FieldOffset(0)] public fixed uint e[3];
        [System.Runtime.InteropServices.FieldOffset(12)] public uint imask;
        [System.Runtime.InteropServices.FieldOffset(16)] public uint base_index_child;
        [System.Runtime.InteropServices.FieldOffset(20)] public uint base_index_triangle;
        [System.Runtime.InteropServices.FieldOffset(24)] public fixed byte meta[8];
        [System.Runtime.InteropServices.FieldOffset(32)] public fixed byte quantized_min_x[8];
        [System.Runtime.InteropServices.FieldOffset(40)] public fixed byte quantized_max_x[8];
        [System.Runtime.InteropServices.FieldOffset(48)] public fixed byte quantized_min_y[8];
        [System.Runtime.InteropServices.FieldOffset(56)] public fixed byte quantized_max_y[8];
        [System.Runtime.InteropServices.FieldOffset(64)] public fixed byte quantized_min_z[8];
        [System.Runtime.InteropServices.FieldOffset(72)] public fixed byte quantized_max_z[8];
        [System.Runtime.InteropServices.FieldOffset(84)] public Vector3 p;
    }


    [System.Serializable]
    public struct MyMeshDataCompacted
    {
        public Matrix4x4 Transform;
        public int AggIndexCount;
        public int AggNodeCount;
        public int MaterialOffset;
        public int mesh_data_bvh_offsets;
        public int LightTriCount;
        public int LightNodeOffset;
        public int LightNodeSkinnedOffset;
        public uint PathFlags;
        public int SkinnedOffset;
    }

    [System.Serializable]
    public struct LightTriData
    {
        public uint TriTarget;
        public float SourceEnergy;
    }

    [System.Serializable]
    public struct LightBounds {
        public AABB b;
        public Vector3 w;
        public float phi;
        public float Theta_o;
        public float Theta_e;
        public int LightCount;
        public float Pad1;
        public void Clear() {
            b.init();
            w = Vector3.zero;
            phi = 0;
            Theta_e = 0;
            Theta_o = 0;
            LightCount = 0;
            Pad1 = 0;
        }

        public LightBounds(AABB aabb, Vector3 W, float Phi, float Theta_o, float Theta_e, int lc, int p1) {
            b = aabb;
            w = W;
            phi = Phi;
            this.Theta_o = Theta_o;
            this.Theta_e = Theta_e;
            LightCount = lc;
            Pad1 = p1;
        }
    }

        [System.Serializable]
        public struct NodeBounds {
            public LightBounds aabb;
            public int left;
            public int isLeaf;
        }

        public struct DirectionCone {
            public Vector3 W;
            public float cosTheta;

            public DirectionCone(Vector3 w, float cosTheta) {
                W = w;
                this.cosTheta = cosTheta;
            }
        }

    [System.Serializable]
    public struct CompactLightBVHData {
        public Vector3 BBMax;
        public Vector3 BBMin;
        public uint w;
        public float phi;
        public uint cosTheta_oe;
        public int left;

        public CompactLightBVHData(Vector3 BBMax, Vector3 BBMin, uint W, float Phi, uint cosTheta_oe, int left) {
            this.BBMax = BBMax;
            this.BBMin = BBMin;
            w = W;
            phi = Phi;
            this.cosTheta_oe = cosTheta_oe;
            this.left = left;
        }
    };

    [System.Serializable]
    public struct AABB
    {
        public Vector3 BBMax;
        public Vector3 BBMin;


        public AABB(int a = 0) { 
            BBMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            BBMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        }

        public AABB(CudaTriangle Tri) { 
            BBMax = Vector3.Max(Vector3.Max(Tri.pos0, Tri.pos0 + Tri.posedge1), Tri.pos0 + Tri.posedge2);
            BBMin = Vector3.Min(Vector3.Min(Tri.pos0, Tri.pos0 + Tri.posedge1), Tri.pos0 + Tri.posedge2);
            // this.Validate(new Vector3(0.1f,0.1f,0.1f));
        }
        public void TransformAABB(Matrix4x4 Mat) { 
            Vector3 center = 0.5f * (BBMin + BBMax);
            Vector3 extent = 0.5f * (BBMax - BBMin);
            Vector3 new_center = CommonFunctions.transform_position(Mat, center);
            Vector3 new_extent = CommonFunctions.transform_direction(Mat, extent);

            BBMin = new_center - new_extent;
            BBMax = new_center + new_extent;
        }
        public int LargestAxis() {
            Vector3 Sizes = BBMax - BBMin;
            int Lorge = 0;
            float Lorgest = Sizes.x;
            if(Sizes.y > Lorgest) {
                Lorgest = Sizes.y;
                Lorge = 1;
            }
            if(Sizes.z > Lorgest) {
                Lorge = 2;
            }
            return Lorge;
        }

        public float LargestExtent(int Axis) {
            Vector3 Sizes = BBMax - BBMin;
            return Sizes[Axis];
        }

        public void ShrinkToFit(AABB SideAABB) {
            BBMax = Vector3.Min(BBMax, SideAABB.BBMax);
            BBMin = Vector3.Max(BBMin, SideAABB.BBMin);
        }
        public void Create(Vector3 A, Vector3 B)
        {
            this.BBMax = A;//new Vector3(System.Math.Max(A.x, B.x), System.Math.Max(A.y, B.y), System.Math.Max(A.z, B.z));
            this.BBMin = A;//new Vector3(System.Math.Min(A.x, B.x), System.Math.Min(A.y, B.y), System.Math.Min(A.z, B.z));
            Extend(B);
        }
        public float ComputeVolume() {
            return System.Math.Max((BBMax.x - BBMin.x) * (BBMax.y - BBMin.y) * (BBMax.z - BBMin.z),0.00001f);
        }

        public float ComputeSurfaceArea() {
            Vector3 sizes = BBMax - BBMin;
            return 2.0f * ((sizes.x * sizes.y) + (sizes.x * sizes.z) + (sizes.y * sizes.z)); 
        }

        public Vector3 Diagonal() {
            return BBMax - BBMin;
        }

        public float HalfArea() {
            Vector3 d = BBMax - BBMin;
            return (d.x + d.y) * d.z + d.x * d.y;
        }

        public AABB Intersect(ref AABB aabb) {
            AABB NewAABB = new AABB();
            NewAABB.BBMax = new Vector3(Mathf.Min(BBMax.x, aabb.BBMax.x), Mathf.Min(BBMax.y, aabb.BBMax.y), Mathf.Min(BBMax.z, aabb.BBMax.z));
            NewAABB.BBMin = new Vector3(Mathf.Max(BBMin.x, aabb.BBMin.x), Mathf.Max(BBMin.y, aabb.BBMin.y), Mathf.Max(BBMin.z, aabb.BBMin.z));
            return NewAABB;
        }

        public void Extend(ref AABB aabb)
        {

            if (aabb.BBMin.x < BBMin.x)
                BBMin.x = aabb.BBMin.x;
            if (aabb.BBMin.y < BBMin.y)
                BBMin.y = aabb.BBMin.y;
            if (aabb.BBMin.z < BBMin.z)
                BBMin.z = aabb.BBMin.z;

            if (aabb.BBMax.x > BBMax.x)
                BBMax.x = aabb.BBMax.x;
            if (aabb.BBMax.y > BBMax.y)
                BBMax.y = aabb.BBMax.y;
            if (aabb.BBMax.z > BBMax.z)
                BBMax.z = aabb.BBMax.z;
        }

        public AABB Union(AABB aabb)
        {
            AABB ResultAABB;
            ResultAABB.BBMax = BBMax;
            ResultAABB.BBMin = BBMin;
            if (aabb.BBMin.x < BBMin.x)
                ResultAABB.BBMin.x = aabb.BBMin.x;
            if (aabb.BBMin.y < BBMin.y)
                ResultAABB.BBMin.y = aabb.BBMin.y;
            if (aabb.BBMin.z < BBMin.z)
                ResultAABB.BBMin.z = aabb.BBMin.z;

            if (aabb.BBMax.x > BBMax.x)
                ResultAABB.BBMax.x = aabb.BBMax.x;
            if (aabb.BBMax.y > BBMax.y)
                ResultAABB.BBMax.y = aabb.BBMax.y;
            if (aabb.BBMax.z > BBMax.z)
                ResultAABB.BBMax.z = aabb.BBMax.z;
            return ResultAABB;
        }

        public void Extend(Vector3 P)
        {

            if (P.x < BBMin.x)
                BBMin.x = P.x;
            if(P.x > BBMax.x)
                BBMax.x = P.x;
            if (P.y < BBMin.y)
                BBMin.y = P.y;
            if(P.y > BBMax.y)
                BBMax.y = P.y;
            if (P.z < BBMin.z)
                BBMin.z = P.z;
            if(P.z > BBMax.z)
                BBMax.z = P.z;
        }

        public bool IsInside(Vector3 P)
        {

            if (P.x < BBMin.x)
                return true;
            if(P.x > BBMax.x)
                return true;
            if (P.y < BBMin.y)
                return true;
            if(P.y > BBMax.y)
                return true;
            if (P.z < BBMin.z)
                return true;
            if(P.z > BBMax.z)
                return true;
            return false;
        }

        public void Validate(Vector3 Scale)
        {
            for (int i2 = 0; i2 < 3; i2++)
            {
                if (BBMax[i2] - BBMin[i2] < Scale[i2])
                {
                    BBMin[i2] -= Scale[i2];
                    BBMax[i2] += Scale[i2];
                }
            }
        }

        public bool IsValid()
        {
            for (int i2 = 0; i2 < 3; i2++)
            {
                if (BBMax[i2] < BBMin[i2])
                {
                    return false;
                }
            }
            return true;
        }

        public void init()
        {
            BBMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            BBMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        }
    }


    [System.Serializable]
    public struct NodeIndexPairData
    {
        public CommonVars.AABB AABB;
        public int BVHNode;
        public int InNodeOffset;
    }

    [System.Serializable]
    public struct BVHNode8DataCompressed
    {
        public uint node_0x;
        public uint node_0y;
        public uint node_0z;
        public uint node_0w;
        public uint node_1x;
        public uint node_1y;
        public uint node_1z;
        public uint node_1w;
        public uint node_2x;
        public uint node_2y;
        public uint node_2z;
        public uint node_2w;
        public uint node_3x;
        public uint node_3y;
        public uint node_3z;
        public uint node_3w;
        public uint node_4x;
        public uint node_4y;
        public uint node_4z;
        public uint node_4w;
    }

    [System.Serializable]
    public struct CudaTriangle
    {
        public Vector3 pos0;
        public Vector3 posedge1;
        public Vector3 posedge2;

        public uint norm0;
        public uint norm1;
        public uint norm2;

        public uint tan0;
        public uint tan1;
        public uint tan2;

        public uint tex0;
        public uint texedge1;
        public uint texedge2;

        public uint VertColA;
        public uint VertColB;
        public uint VertColC;

        public uint MatDat;
        public uint IsEmissive;

        Vector3 split_edge(int axis, float position, Vector3 a, Vector3 b) {
            float t = (position - a[axis]) / (b[axis] - a[axis]);
            return a + t * (b - a);
        }
        public AABB[] Split(int axis, float position) {
            Vector3[] p = { pos0, pos0 + posedge1, pos0 + posedge2 };
            AABB left  = new AABB();
            left.init();
            AABB right = new AABB();
            right.init();
            bool q0 = p[0][axis] <= position;
            bool q1 = p[1][axis] <= position;
            bool q2 = p[2][axis] <= position;
            if (q0) left.Extend(p[0]);
            else    right.Extend(p[0]);
            if (q1) left.Extend(p[1]);
            else    right.Extend(p[1]);
            if (q2) left.Extend(p[2]);
            else    right.Extend(p[2]);
            if (q0 ^ q1) {
                Vector3 m = split_edge(axis, position, p[0], p[1]);
                left.Extend(m);
                right.Extend(m);
            }
            if (q1 ^ q2) {
                Vector3 m = split_edge(axis, position, p[1], p[2]);
                left.Extend(m);
                right.Extend(m);
            }
            if (q2 ^ q0) {
                Vector3 m = split_edge(axis, position, p[2], p[0]);
                left.Extend(m);
                right.Extend(m);
            }
            AABB[] RetAABB = new AABB[2];
            // left.Validate(new Vector3(0.0001f,0.0001f,0.0001f));
            // right.Validate(new Vector3(0.0001f,0.0001f,0.0001f));
            RetAABB[0] = left;
            RetAABB[1] = right;
            return RetAABB;
        }

    }


    [System.Serializable]
    public struct CudaTriangleA
    {
        public Vector3 pos0;
        public Vector3 posedge1;
        public Vector3 posedge2;

        public uint tex0;
        public uint texedge1;
        public uint texedge2;

        public uint MatDat;
    }


    [System.Serializable]
    public struct CudaTriangleB
    {
        public uint norm0;
        public uint norm1;
        public uint norm2;

        public uint tan0;
        public uint tan1;
        public uint tan2;
     
        public uint VertColA;
        public uint VertColB;
        public uint VertColC;
        
        public uint IsEmissive;
    }

    [System.Serializable]
    public struct LightMeshData
    {
        public Vector3 Center;
        public int StartIndex;
        public int IndexEnd;
        public int MatOffset;
        public int LockedMeshIndex;
    }

    [System.Serializable]
    public struct Layer
    {
        unsafe public fixed int Children[8];
    }

    [System.Serializable]
    public struct Layer2
    {
        public List<int> Slab;
    }





    public struct TTStopWatch {//stopwatch stuff
        public string Name;
        private System.Diagnostics.Stopwatch SW;
        
        public TTStopWatch(string Name) {
            this.Name = Name;
            SW = new System.Diagnostics.Stopwatch();
        }
        public void Start() {
            SW = System.Diagnostics.Stopwatch.StartNew();
        }
        public void Pause() {
            SW.Stop();
        }
        public void Resume() {
            SW.Start();
        }
        public float GetSeconds() {
            SW.Stop();
            System.TimeSpan ts = SW.Elapsed;
            return ts.Minutes * 60.0f + ts.Seconds;
        }
        public void Stop(string OptionalString = "") {
            SW.Stop();
            System.TimeSpan ts = SW.Elapsed;
            if(OptionalString.Equals("")) {
                Debug.Log(Name + ": " + System.String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10));
            } else {
                Debug.Log(Name + ", " + OptionalString + ": " + System.String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10));
            }
        }
    }


    public static class CommonFunctions
    {

        public static Vector4 ToVector4(Vector3 A, float B) {
            return new Vector4(A.x, A.y, A.z, B);
        }
        public static Vector3 ToVector3(Vector4 A) {
            return new Vector3(A.x, A.y, A.z);
        }

        public static void DeepClean<T>(ref List<T> A) {
            if(A != null) {
                A.Clear();
                A.TrimExcess();
                A = null;
            }
        }
        public static void DeepClean<T>(ref T[] A) {
            A = null;
        }

        public static void CreateDynamicBuffer(ref ComputeBuffer TargetBuffer, int Count, int Stride, ComputeBufferType ComputeType = ComputeBufferType.Structured, ComputeBufferMode ComputeMode = ComputeBufferMode.Immutable)
        {
            if (TargetBuffer != null) TargetBuffer?.Dispose();
            TargetBuffer = new ComputeBuffer(Count, Stride, ComputeType, ComputeMode);
        }
        public static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
            where T : struct
        {
            // Do we already have a compute buffer?
            if (buffer != null)
            {
                // If no data or buffer doesn't match the given criteria, release it
                if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
                {
                    buffer.Release();
                    buffer = null;
                }
            }

            if (data.Count != 0)
            {
                // If the buffer has been released or wasn't there to
                // begin with, create it
                if (buffer == null)
                {
                    buffer = new ComputeBuffer(data.Count, stride);
                }
                // Set data on the buffer
                buffer.SetData(data);
            }
        }

        unsafe public static void Aggregate(ref BVHNode8DataCompressed[] AggNodes, TrueTrace.BVH8Builder BVH)
        {//Compress the CWBVH
            BVHNode8DataCompressed TempBVHNode = new BVHNode8DataCompressed();
            int BVHLength = BVH.cwbvhnode_count;
            for (int i = 0; i < BVHLength; ++i)
            {
                BVHNode8Data TempNode = BVH.BVH8Nodes[i];
                TempBVHNode.node_0x = *(uint*)&TempNode.p.x;
                TempBVHNode.node_0y = *(uint*)&TempNode.p.y;
                TempBVHNode.node_0z = *(uint*)&TempNode.p.z;
                TempBVHNode.node_0w = (TempNode.e[0] | (TempNode.e[1] << 8) | (TempNode.e[2] << 16) | (TempNode.imask << 24));
                TempBVHNode.node_1x = TempNode.base_index_child;
                TempBVHNode.node_1y = TempNode.base_index_triangle;
                TempBVHNode.node_1z = (uint)(TempNode.meta[0] | (TempNode.meta[1] << 8) | (TempNode.meta[2] << 16) | (TempNode.meta[3] << 24));
                TempBVHNode.node_1w = (uint)(TempNode.meta[4] | (TempNode.meta[5] << 8) | (TempNode.meta[6] << 16) | (TempNode.meta[7] << 24));
                TempBVHNode.node_2x = (uint)(TempNode.quantized_min_x[0] | (TempNode.quantized_min_x[1] << 8) | (TempNode.quantized_min_x[2] << 16) | (TempNode.quantized_min_x[3] << 24));
                TempBVHNode.node_2z = (uint)(TempNode.quantized_min_x[4] | (TempNode.quantized_min_x[5] << 8) | (TempNode.quantized_min_x[6] << 16) | (TempNode.quantized_min_x[7] << 24));
                TempBVHNode.node_2y = (uint)(TempNode.quantized_max_x[0] | (TempNode.quantized_max_x[1] << 8) | (TempNode.quantized_max_x[2] << 16) | (TempNode.quantized_max_x[3] << 24));
                TempBVHNode.node_2w = (uint)(TempNode.quantized_max_x[4] | (TempNode.quantized_max_x[5] << 8) | (TempNode.quantized_max_x[6] << 16) | (TempNode.quantized_max_x[7] << 24));
                TempBVHNode.node_3x = (uint)(TempNode.quantized_min_y[0] | (TempNode.quantized_min_y[1] << 8) | (TempNode.quantized_min_y[2] << 16) | (TempNode.quantized_min_y[3] << 24));
                TempBVHNode.node_3z = (uint)(TempNode.quantized_min_y[4] | (TempNode.quantized_min_y[5] << 8) | (TempNode.quantized_min_y[6] << 16) | (TempNode.quantized_min_y[7] << 24));
                TempBVHNode.node_3y = (uint)(TempNode.quantized_max_y[0] | (TempNode.quantized_max_y[1] << 8) | (TempNode.quantized_max_y[2] << 16) | (TempNode.quantized_max_y[3] << 24));
                TempBVHNode.node_3w = (uint)(TempNode.quantized_max_y[4] | (TempNode.quantized_max_y[5] << 8) | (TempNode.quantized_max_y[6] << 16) | (TempNode.quantized_max_y[7] << 24));
                TempBVHNode.node_4x = (uint)(TempNode.quantized_min_z[0] | (TempNode.quantized_min_z[1] << 8) | (TempNode.quantized_min_z[2] << 16) | (TempNode.quantized_min_z[3] << 24));
                TempBVHNode.node_4z = (uint)(TempNode.quantized_min_z[4] | (TempNode.quantized_min_z[5] << 8) | (TempNode.quantized_min_z[6] << 16) | (TempNode.quantized_min_z[7] << 24));
                TempBVHNode.node_4y = (uint)(TempNode.quantized_max_z[0] | (TempNode.quantized_max_z[1] << 8) | (TempNode.quantized_max_z[2] << 16) | (TempNode.quantized_max_z[3] << 24));
                TempBVHNode.node_4w = (uint)(TempNode.quantized_max_z[4] | (TempNode.quantized_max_z[5] << 8) | (TempNode.quantized_max_z[6] << 16) | (TempNode.quantized_max_z[7] << 24));
                AggNodes[i] = TempBVHNode;
            }
        }

        public static int NumberOfSetBits(int i)
        {
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        //Better Bounding Box Transformation by Zuex(I got it from Zuen)
        public static Vector3 transform_position(Matrix4x4 matrix, Vector3 position)
        {
            return new Vector3(
                matrix[0, 0] * position.x + matrix[0, 1] * position.y + matrix[0, 2] * position.z + matrix[0, 3],
                matrix[1, 0] * position.x + matrix[1, 1] * position.y + matrix[1, 2] * position.z + matrix[1, 3],
                matrix[2, 0] * position.x + matrix[2, 1] * position.y + matrix[2, 2] * position.z + matrix[2, 3]
            );
        }
        public static Vector3 transform_direction(Matrix4x4 matrix, Vector3 direction)
        {
            return new Vector3(
                Mathf.Abs(matrix[0, 0]) * direction.x + Mathf.Abs(matrix[0, 1]) * direction.y + Mathf.Abs(matrix[0, 2]) * direction.z,
                Mathf.Abs(matrix[1, 0]) * direction.x + Mathf.Abs(matrix[1, 1]) * direction.y + Mathf.Abs(matrix[1, 2]) * direction.z,
                Mathf.Abs(matrix[2, 0]) * direction.x + Mathf.Abs(matrix[2, 1]) * direction.y + Mathf.Abs(matrix[2, 2]) * direction.z
            );
        }
        public static void abs(ref Matrix4x4 matrix)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int i2 = 0; i2 < 4; i2++) matrix[i, i2] = Mathf.Abs(matrix[i, i2]);
            }
        }
        public static void SetComputeBuffer(this ComputeShader Shader, int kernel, string name, ComputeBuffer buffer)
        {
            if (buffer != null) Shader.SetBuffer(kernel, name, buffer);
        }
        public static void ReleaseSafe(this RenderTexture Tex)
        {
            if (Tex != null) Tex.Release();
        }
        public static void ReleaseSafe(this RenderTexture[] Tex)
        {
            if (Tex != null) {
                int TexLength = Tex.Length;
                for(int i = 0; i < TexLength; i++) {
                    if(Tex[i] != null) Tex[i].Release();
                }
            }
        }
        public static void ReleaseSafe(this ComputeBuffer Buff)
        {
            if (Buff != null) {Buff.Release(); Buff = null;}
        }

        public static void ReleaseSafe(this ComputeBuffer[] Buff)
        {
            if (Buff != null) {
                int BuffLength = Buff.Length;
                for(int i = 0; i < BuffLength; i++)
                    if(Buff[i] != null) Buff[i].Release();
            }
        }

        public static void ReleaseSafe(this GraphicsBuffer Buff)
        {
            if (Buff != null) {Buff.Release(); Buff = null;}
        }

        static Vector2 msign(Vector2 v)
        {
            return new Vector2((v.x >= 0.0f) ? 1.0f : -1.0f, (v.y >= 0.0f) ? 1.0f : -1.0f);
        }

        public static uint PackOctahedral(Vector3 nor)
        {
            const float halfMaxUInt16 = 32767.5f;

            Vector2 sign = new Vector2((nor.x >= 0.0f) ? 1.0f : -1.0f, (nor.y >= 0.0f) ? 1.0f : -1.0f);
            float absX = nor.x * sign.x;
            float absY = nor.y * sign.y;
            float Tot = absX + absY + (float)System.Math.Abs(nor.z);

            Vector2 temp = new Vector2(absX / Tot, absY / Tot);
            if (nor.z < 0.0f)
            {
                temp = new Vector2(1.0f - temp.y, 1.0f - temp.x);
            }

            // Vector2 d = new Vector2(Mathf.Round(halfMaxUInt16 + temp.x * halfMaxUInt16), Mathf.Round(halfMaxUInt16 + temp.y * halfMaxUInt16));
            return (uint)(halfMaxUInt16 + temp.x * halfMaxUInt16 * sign.x) | ((uint)(halfMaxUInt16 + temp.y * halfMaxUInt16 * sign.y) << 16);
        }


        public static Vector3 UnpackOctahedral(uint data) {
            uint ivx = (uint)(data) & 65535u; 
            uint ivy = (uint)(data>>16 ) & 65535u; 
            Vector2 v = new Vector2(ivx/32767.5f, ivy/32767.5f) - Vector2.one;
            Vector3 nor = new Vector3(v.x, v.y, 1.0f - Mathf.Abs(v.x) - Mathf.Abs(v.y)); // Rune Stubbe's version,
            float t = Mathf.Max(-nor.z,0.0f);                     // much faster than original
            nor.x += (nor.x >= 0) ? -t : t;
            nor.y += (nor.y >= 0) ? -t : t;
            return nor.normalized;
        }

        public static readonly RenderTextureFormat RTFull4 = RenderTextureFormat.ARGBFloat;
        public static readonly RenderTextureFormat RTInt1 = RenderTextureFormat.RInt;
        public static readonly RenderTextureFormat RTInt2 = RenderTextureFormat.RGInt;
        public static readonly RenderTextureFormat RTHalf4 = RenderTextureFormat.ARGBHalf;
        public static readonly RenderTextureFormat RTHalf1 = RenderTextureFormat.RHalf;
        public static readonly RenderTextureFormat RTFull2 = RenderTextureFormat.RGFloat;
        public static readonly RenderTextureFormat RTFull1 = RenderTextureFormat.RFloat;
        public static readonly RenderTextureFormat RTHalf2 = RenderTextureFormat.RGHalf;

        public static void CreateRenderTexture(ref RenderTexture ThisTex, 
                                                    int Width, int Height, 
                                                    RenderTextureFormat Form, 
                                                    RenderTextureReadWrite RendRead = RenderTextureReadWrite.Linear, 
                                                    bool UseMip = false) {
            if(ThisTex != null) ThisTex?.Release();
            ThisTex = new RenderTexture(Width, Height, 0,
                Form, RendRead);
            if (UseMip) {
                ThisTex.useMipMap = true;
                ThisTex.autoGenerateMips = false;
            }
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }

        public static void CreateRenderTextureArray2(ref RenderTexture[] ThisTex, 
                                                    int Width, int Height, int Depth,
                                                    RenderTextureFormat Form, 
                                                    RenderTextureReadWrite RendRead = RenderTextureReadWrite.Linear, 
                                                    bool UseMip = false) {
            ThisTex = new RenderTexture[2];
            for(int i = 0; i < Depth; i++) {
                if(ThisTex[i] != null) ThisTex[i]?.Release();
                ThisTex[i] = new RenderTexture(Width, Height, 0,
                    Form, RendRead);
                if (UseMip) {
                    ThisTex[i].useMipMap = true;
                    ThisTex[i].autoGenerateMips = false;
                }
                ThisTex[i].enableRandomWrite = true;
                ThisTex[i].Create();
            }
        }


        public static void CreateRenderTexture3D(ref RenderTexture ThisTex, 
                                                    int Width, int Height, int Depth, 
                                                    RenderTextureFormat Form, 
                                                    RenderTextureReadWrite RendRead = RenderTextureReadWrite.Linear, 
                                                    bool UseMip = false) {
            if(ThisTex != null) ThisTex?.Release();
            ThisTex = new RenderTexture(Width, Height, 0, Form, RendRead);
            ThisTex.volumeDepth = Depth;
            ThisTex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;

            if (UseMip) {
                ThisTex.useMipMap = true;
                ThisTex.autoGenerateMips = false;
            }
            ThisTex.enableRandomWrite = true;
            ThisTex.Create();
        }

        public static void CreateRenderTextureArray(ref RenderTexture ThisTexArray, 
                                                    int Width, int Height, int Depth,
                                                    RenderTextureFormat Form, 
                                                    RenderTextureReadWrite RendRead = RenderTextureReadWrite.Linear) {
            if(ThisTexArray != null) ThisTexArray?.Release();
            ThisTexArray = new RenderTexture(Width, Height, 0,
                Form, RendRead);
            
            ThisTexArray.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
            ThisTexArray.enableRandomWrite = true;
            ThisTexArray.volumeDepth = Depth;
            ThisTexArray.Create();
        }

        public static int GetStride<T>() 
            where T : struct
        {
            return System.Runtime.InteropServices.Marshal.SizeOf<T>();
        }

        public static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data)
            where T : struct
        {
            int stride = System.Runtime.InteropServices.Marshal.SizeOf<T>();
            if (buffer != null) {
                if (data == null || data.Count == 0 || !buffer.IsValid() || buffer.count != data.Count || buffer.stride != stride) {
                    buffer.Release();
                    buffer = null;
                }
            }
            if (data != null && data.Count != 0) {
                if (buffer == null) buffer = new ComputeBuffer(data.Count, stride);
                buffer.SetData(data);
            } else {
                if (buffer == null) buffer = new ComputeBuffer(1, stride);
            }
        }
        public static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, T[] data)
            where T : struct
        {
            int stride = System.Runtime.InteropServices.Marshal.SizeOf<T>();
            if (buffer != null) {
                if (data == null || data.Length == 0 || !buffer.IsValid() || buffer.count != data.Length || buffer.stride != stride) {
                    buffer.Release();
                    buffer = null;
                }
            }
            if (data != null && data.Length != 0) {
                if (buffer == null) buffer = new ComputeBuffer(data.Length, stride);
                buffer.SetData(data);
            } else {
                if (buffer == null) buffer = new ComputeBuffer(1, stride);

            }
        }


        public enum Flags {IsEmissionMask, BaseIsMap, ReplaceBase, UseSmoothness, InvertSmoothnessTexture, IsBackground, ShadowCaster, Invisible, BackgroundBleed, Thin, UseVertexColors, InvertAlpha, EnableCausticGeneration, DisableCausticRecieving};
        //0-13 Flags
        //28-30 SecondaryAlbedoStride

        public static int SetFlagStretch(this int FlagVar, int LeftOffset, int Stride, int Setter) {
            return (FlagVar & ~(((1 << (Stride + 1)) - 1) << (int)(32 - LeftOffset - Stride))) | ((Setter & ((1 << (Stride)) - 1)) << (int)(32 - LeftOffset - Stride));
        }

        public static int GetFlagStretch(this int FlagVar, int LeftOffset, int Stride) {
            return ((FlagVar >> (32 - LeftOffset - Stride)) & ((1 << (Stride)) - 1));
        }

        public static void SetFlag(this int FlagVar, Flags flag, bool Setter) {
            FlagVar = (FlagVar & ~(1 << (int)flag)) | ((Setter ? 1 : 0) << (int)flag);
        }
        
        public static int SetFlagVar(int FlagVar, Flags flag, bool Setter) {
            return (FlagVar & ~(1 << (int)flag)) | ((Setter ? 1 : 0) << (int)flag);
        }

        public static bool GetFlag(this int FlagVar, Flags flag) {
            return (((int)FlagVar >> (int)flag) & (int)1) == 1;
        }

        public static uint packRGBE(Color v)
        {
            Vector3 va = new Vector3(v.r, v.g, v.b);
            float max_abs = va.x;//, Mathf.Max(va.y, va.z));
            if(max_abs < va.y) max_abs = va.y;
            if(max_abs < va.z) max_abs = va.z;
            if (max_abs == 0) return 0;

            float exponent = (float)System.Math.Floor(System.Math.Log(max_abs, 2));

            uint result = (uint)(System.Math.Clamp(exponent + 20, 0, 31)) << 27;

            float scale = (float)System.Math.Pow(2.0f, -exponent) * 256.0f;
            result |= (uint)(System.Math.Min(511, System.Math.Round(va.x * scale)));
            result |= (uint)(System.Math.Min(511, System.Math.Round(va.y * scale))) << 9;
            result |= (uint)(System.Math.Min(511, System.Math.Round(va.z * scale))) << 18;

            return result;
        }

        public static RayObjMat ZeroConstructorMat() {
            RayObjMat NewMat = new RayObjMat();
            NewMat.TextureModifiers = new MatTextureModifierData();
            
            NewMat.TextureModifiers.MainTexScaleOffset = new Vector4(1,1,0,0);
            NewMat.TextureModifiers.SecondaryTextureScaleOffset = new Vector4(1,1,0,0);
            NewMat.TextureModifiers.NormalTexScaleOffset = new Vector4(1,1,0,0);
            NewMat.TextureModifiers.SecondaryAlbedoTexScaleOffset = new Vector4(1,1,0,0);
            NewMat.TextureModifiers.SecondaryNormalTexScaleOffset = new Vector4(1,1,0,0);
            NewMat.TextureModifiers.Rotation = 0;
            NewMat.TextureModifiers.RotationNormal = 0;
            NewMat.TextureModifiers.RotationSecondary = 0;
            NewMat.TextureModifiers.RotationSecondaryDiffuse = 0;
            NewMat.TextureModifiers.RotationSecondaryNormal = 0;

            NewMat.BaseColor = Vector3.one;
            NewMat.emission = 0;
            NewMat.EmissionColor = Vector3.one;
            NewMat.Tag = 0;
            NewMat.Roughness = 0;
            NewMat.MatType = 0;
            NewMat.TransmittanceColor = Vector3.one;
            NewMat.IOR = 1;
            NewMat.Metallic = 0;
            NewMat.Sheen = 0;
            NewMat.SheenTint = 0;
            NewMat.SpecularTint = 0;
            NewMat.Clearcoat = 0;
            NewMat.ClearcoatGloss = 0;
            NewMat.Anisotropic = 0;
            NewMat.AnisotropicRotation = 0;
            NewMat.Flatness = 0;
            NewMat.DiffTrans = 0;
            NewMat.SpecTrans = 0;
            NewMat.Specular = 0;
            NewMat.ScatterDist = 1;
            NewMat.MetallicRemap = new Vector2(0,1);
            NewMat.RoughnessRemap = new Vector2(0,1);
            NewMat.AlphaCutoff = 0.1f;
            NewMat.NormalStrength = 1;
            NewMat.Hue = 0;
            NewMat.Saturation = 1;
            NewMat.Contrast = 1;
            NewMat.Brightness = 1;
            NewMat.BlendColor = Vector3.one;
            NewMat.BlendFactor = 0;
            NewMat.ColorBleed = 1;
            NewMat.AlbedoBlendFactor = 0;
            NewMat.SecondaryNormalTexBlend = 0;
            NewMat.DetailNormalStrength = 1;
            NewMat.DiffTransRemap = new Vector2(0,1);
            NewMat.MatCapColor = Vector3.one;
            NewMat.CausticStrength = 1;
            return NewMat;
        }


        #if UNITY_EDITOR
           public static T GetCopyOf2<T>(this Component comp, T other) where T : Component
          {
             System.Type type = comp.GetType();
             if (type != other.GetType()) return null; // type mis-match
             System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.DeclaredOnly;
             System.Reflection.PropertyInfo[] pinfos = type.GetProperties(flags);
             foreach (var pinfo in pinfos) {
                if (pinfo.CanWrite) {
                   try {
                      pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                   }
                   catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
             }
             System.Reflection.FieldInfo[] finfos = type.GetFields(flags);
             foreach (var finfo in finfos) {
                finfo.SetValue(comp, finfo.GetValue(other));
             }
             return comp as T;
          }
            public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component
            {
                return go.AddComponent<T>().GetCopyOf2(toAdd) as T;
            }

        #endif


    }
}
