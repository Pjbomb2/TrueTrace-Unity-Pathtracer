using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonVars
{
    #pragma warning disable 4014

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
    public struct MeshDat
    {
        public List<int> Indices;
        public List<Vector3> Verticies;
        public List<Vector3> Normals;
        public List<Vector4> Tangents;
        public List<Vector2> UVs;
        public List<Color> Colors;
        #if TTLightMapping
            public List<Vector2> LightmapUVs;
        #endif
        public List<int> MatDat;

        public void SetUvZero(int Count) {
            for (int i = 0; i < Count; i++) UVs.Add(new Vector2(0.0f, 0.0f));
        }
        public void SetColorsZero(int Count) {
            Color TempCol = new Color(1,1,1,1);
            for (int i = 0; i < Count; i++) Colors.Add(TempCol);
        }
        #if TTLightMapping
            public void AddLightmapUVs(Vector2[] LightUVs, Vector4 ScaleOffset) {
                int Count = LightUVs.Length;
                for (int i = 0; i < Count; i++) LightmapUVs.Add(new Vector2(LightUVs[i].x * ScaleOffset.x + ScaleOffset.z, LightUVs[i].y * ScaleOffset.y + ScaleOffset.w));
            }
        #endif
        public void SetTansZero(int Count) {
            for (int i = 0; i < Count; i++) Tangents.Add(new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
        }
        public void init(int StartingSize) {
            this.Tangents = new List<Vector4>(StartingSize);
            this.MatDat = new List<int>(StartingSize / 3);
            this.UVs = new List<Vector2>(StartingSize);
            #if TTLightMapping
                this.LightmapUVs = new List<Vector2>(StartingSize);
            #endif
            this.Verticies = new List<Vector3>(StartingSize);
            this.Normals = new List<Vector3>(StartingSize);
            this.Indices = new List<int>(StartingSize);
            this.Colors = new List<Color>(StartingSize);
        }
        public void Clear() {
            if (Tangents != null) {
                CommonFunctions.DeepClean(ref Tangents);
                CommonFunctions.DeepClean(ref MatDat);
                CommonFunctions.DeepClean(ref UVs);
                #if TTLightMapping
                    CommonFunctions.DeepClean(ref LightmapUVs);
                #endif
                CommonFunctions.DeepClean(ref Verticies);
                CommonFunctions.DeepClean(ref Normals);
                CommonFunctions.DeepClean(ref Indices);
                CommonFunctions.DeepClean(ref Colors);
            }
        }
    }

    [System.Serializable]
    public struct MaterialData
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
        public Vector3 BaseColor;
        public float emission;
        public Vector3 EmissionColor;
        public int Tag;
        public float Roughness;
        public int MatType;
        public Vector3 TransmittanceColor;
        public float IOR;
        public float metallic;
        public float sheen;
        public float sheenTint;
        public float specularTint;
        public float clearcoat;
        public float clearcoatGloss;
        public float anisotropic;
        public float flatness;
        public float diffTrans;
        public float specTrans;
        public float Specular;
        public float scatterDistance;
        public Vector4 AlbedoTextureScale;
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
        public Vector2 SecondaryTextureScale;
        public Vector4 SecondaryAlbedoTexScaleOffset;
        public float Rotation;
        public float ColorBleed;
        public float AlbedoBlendFactor;
    }

    [System.Serializable]
    public struct BVHNode2Data
    {
        public AABB aabb;
        public int left;
        public uint count;
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
    public struct BVHNode8DataFixed
    {
        public uint px;
        public uint py;
        public uint pz;
        public uint e1;
        public uint e2;
        public uint e3;
        public uint imask;
        public uint base_index_child;
        public uint base_index_triangle;
        public uint meta1;
        public uint meta2;
        public uint meta3;
        public uint meta4;
        public uint meta5;
        public uint meta6;
        public uint meta7;
        public uint meta8;
        public uint quantized_min_x1;
        public uint quantized_min_x2;
        public uint quantized_min_x3;
        public uint quantized_min_x4;
        public uint quantized_min_x5;
        public uint quantized_min_x6;
        public uint quantized_min_x7;
        public uint quantized_min_x8;
        public uint quantized_max_x1;
        public uint quantized_max_x2;
        public uint quantized_max_x3;
        public uint quantized_max_x4;
        public uint quantized_max_x5;
        public uint quantized_max_x6;
        public uint quantized_max_x7;
        public uint quantized_max_x8;
        public uint quantized_min_y1;
        public uint quantized_min_y2;
        public uint quantized_min_y3;
        public uint quantized_min_y4;
        public uint quantized_min_y5;
        public uint quantized_min_y6;
        public uint quantized_min_y7;
        public uint quantized_min_y8;
        public uint quantized_max_y1;
        public uint quantized_max_y2;
        public uint quantized_max_y3;
        public uint quantized_max_y4;
        public uint quantized_max_y5;
        public uint quantized_max_y6;
        public uint quantized_max_y7;
        public uint quantized_max_y8;
        public uint quantized_min_z1;
        public uint quantized_min_z2;
        public uint quantized_min_z3;
        public uint quantized_min_z4;
        public uint quantized_min_z5;
        public uint quantized_min_z6;
        public uint quantized_min_z7;
        public uint quantized_min_z8;
        public uint quantized_max_z1;
        public uint quantized_max_z2;
        public uint quantized_max_z3;
        public uint quantized_max_z4;
        public uint quantized_max_z5;
        public uint quantized_max_z6;
        public uint quantized_max_z7;
        public uint quantized_max_z8;
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
    }

    [System.Serializable]
    public struct LightTriData
    {
        public Vector3 pos0;
        public Vector3 posedge1;
        public Vector3 posedge2;
        public uint TriTarget;
        public float SourceEnergy;
    }

    [System.Serializable]
    public struct LightBounds {
        public AABB b;
        public Vector3 w;
        public float phi;
        public float cosTheta_o;
        public float cosTheta_e;
        public int LightCount;
        public float Pad1;

        public LightBounds(AABB aabb, Vector3 W, float Phi, float cosTheta_o, float cosTheta_e, int lc, int p1) {
            b = aabb;
            w = W;
            phi = Phi;
            this.cosTheta_o = cosTheta_o;
            this.cosTheta_e = cosTheta_e;
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

    [System.Serializable]
    public struct CompactLightBVHData {
        public Vector3 BBMax;
        public Vector3 BBMin;
        public uint w;
        public float phi;
        public uint cosTheta_oe;
        public int left;
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
    public enum TexturePurpose {Albedo, Alpha, Normal, Emission, Metallic, Roughness, MatCapTex, MatCapMask, SecondaryAlbedoTexture, SecondaryAlbedoTextureMask};

    [System.Serializable]
    public class TexturePairs {
        public int Purpose;
        public int ReadIndex;//negative is the amount of components the destination contains plus 1, for use later with another idea I had
        public string TextureName;
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
        public Vector3 TransCol;
        public Vector3 BaseCol;
        public Vector2 MetRemap;
        public Vector2 RoughRemap;
        public float Emiss;
        public Vector3 EmissCol;
        public float Rough;
        public float IOR;
        public float Met;
        public float SpecTint;
        public float Sheen;
        public float SheenTint;
        public float Clearcoat;
        public float ClearcoatGloss;
        public float Anisotropic;
        public float Flatness;
        public float DiffTrans;
        public float SpecTrans;
        public bool FollowMat;
        public float ScatterDist;
        public float Spec;
        public float AlphaCutoff;
        public float NormStrength;
        public float Hue;
        public float Saturation;
        public float Brightness;
        public float Contrast;
        public Vector3 BlendColor;
        public float BlendFactor;
        public Vector4 MainTexScaleOffset;
        public Vector4 SecondaryAlbedoTexScaleOffset;
        public Vector2 SecondaryTextureScale;
        public float Rotation;
        public int Flags;
        public bool UseKelvin;
        public float KelvinTemp;
        public float ColorBleed;
        public float AlbedoBlendFactor;

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

        unsafe public static void Aggregate(ref BVHNode8DataCompressed[] AggNodes, ref BVHNode8Data[] BVH8Nodes)
        {//Compress the CWBVH
            BVHNode8DataCompressed TempBVHNode = new BVHNode8DataCompressed();
            for (int i = 0; i < BVH8Nodes.Length; ++i)
            {
                BVHNode8Data TempNode = BVH8Nodes[i];
                TempBVHNode.node_0x = System.BitConverter.ToUInt32(System.BitConverter.GetBytes(TempNode.p.x), 0);
                TempBVHNode.node_0y = System.BitConverter.ToUInt32(System.BitConverter.GetBytes(TempNode.p.y), 0);
                TempBVHNode.node_0z = System.BitConverter.ToUInt32(System.BitConverter.GetBytes(TempNode.p.z), 0);
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

        public unsafe static void ConvertToSplitNodes(TrueTrace.BVH8Builder BVH, ref List<BVHNode8DataFixed> SplitNodes)
        {
            BVHNode8DataFixed NewNode = new BVHNode8DataFixed();
            SplitNodes = new List<BVHNode8DataFixed>();
            BVHNode8Data SourceNode;
            for (int i = 0; i < BVH.BVH8Nodes.Length; i++)
            {
                SourceNode = BVH.BVH8Nodes[i];
                NewNode.px = System.BitConverter.ToUInt32(System.BitConverter.GetBytes(SourceNode.p.x), 0);
                NewNode.py = System.BitConverter.ToUInt32(System.BitConverter.GetBytes(SourceNode.p.y), 0);
                NewNode.pz = System.BitConverter.ToUInt32(System.BitConverter.GetBytes(SourceNode.p.z), 0);
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
            float Tot = absX + absY + Mathf.Abs(nor.z);

            Vector2 temp = new Vector2(absX / Tot, absY / Tot);
            if (nor.z < 0.0f)
            {
                temp = new Vector2(1.0f - temp.y, 1.0f - temp.x);
            }

            // Vector2 d = new Vector2(Mathf.Round(halfMaxUInt16 + temp.x * halfMaxUInt16), Mathf.Round(halfMaxUInt16 + temp.y * halfMaxUInt16));
            return (uint)(halfMaxUInt16 + temp.x * halfMaxUInt16 * sign.x) | ((uint)(halfMaxUInt16 + temp.y * halfMaxUInt16 * sign.y) << 16);
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


        public enum Flags {IsEmissionMask, BaseIsMap, ReplaceBase, UseSmoothness, InvertSmoothnessTexture, IsBackground, ShadowCaster, Invisible, BackgroundBleed, Thin};
        //0-9 Flags
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
            Vector3 va = new Vector3(Mathf.Max(0, v.r), Mathf.Max(0, v.g), Mathf.Max(0, v.b));
            float max_abs = Mathf.Max(va.x, Mathf.Max(va.y, va.z));
            if (max_abs == 0) return 0;

            float exponent = Mathf.Floor(Mathf.Log(max_abs, 2));

            uint result = (uint)(Mathf.Clamp(exponent + 20, 0, 31)) << 27;

            float scale = Mathf.Pow(2.0f, -exponent) * 256.0f;
            result |= (uint)(Mathf.Min(511, Mathf.Round(va.x * scale)));
            result |= (uint)(Mathf.Min(511, Mathf.Round(va.y * scale))) << 9;
            result |= (uint)(Mathf.Min(511, Mathf.Round(va.z * scale))) << 18;

            return result;
        }


    public static uint encodeQTangentUI32(Vector3 T, Vector3 B, Vector3 N){
        float determinant = T.x * ((B.y * N.z) - (N.y * B.z)) - B.x * ((T.y * N.z) - (N.y * T.z)) + N.x * ((T.y * B.z) - (B.y * T.z));

      float r = (determinant < 0.0f) ? -1.0f : 1.0f; // Reflection matrix handling 
      N *= r;
    // #if 0
    //   // When the input matrix is always a valid orthogonal tangent space matrix, we can simplify the quaternion calculation to just this:  
    //   vec4 q = vec4(B.z - T.y, N.x - T.z, T.y - B.x, 1.0 + T.x + B.y + N.z);
    // #else  
      // Otherwise we have to handle all other possible cases as well.
      float t = T.x + (B.y + N.z);
      Vector4 q;
      if(t > 2.9999999f){
        q = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
      }else if(t > 0.0000001f){
        float s = Mathf.Sqrt(1.0f + t) * 2.0f;
        q = new Vector4((B.z - N.y) / s, (N.x - T.z) / s, (T.y - B.x) / s, s * 0.25f);
      }else if((T.x > B.y) && (T.x > N.z)){
        float s = Mathf.Sqrt(1.0f + (T.x - (B.y + N.z))) * 2.0f;
        q = new Vector4(s * 0.25f, (B.x + T.y) / s, (N.x + T.z) / s, (B.z - T.y) / s);    
      }else if(B.y > N.z){
        float s = Mathf.Sqrt(1.0f + (B.y - (T.x + N.z))) * 2.0f;
        q = new Vector4((B.x + T.y) / s, (T.y + B.z) / s, (N.x - T.z) / s, s * 0.25f);
        q = new Vector4(q.x, q.w, q.y, q.z);
      }else{
        float s = Mathf.Sqrt(1.0f + (N.z - (T.x + B.y))) * 2.0f;
        q = new Vector4((N.x + T.z) / s, (T.y + B.z) / s, (T.y - B.x) / s, s * 0.25f); 
        q = new Vector4(q.x, q.y, q.w, q.z);
      }
    // #endif  
      q = q.normalized;
      Vector4 qAbs = new Vector4(Mathf.Abs(q.x), Mathf.Abs(q.y), Mathf.Abs(q.z), Mathf.Abs(q.w));
      int maxComponentIndex = (qAbs.x > qAbs.y) ? ((qAbs.x > qAbs.z) ? ((qAbs.x > qAbs.w) ? 0 : 3) : ((qAbs.z > qAbs.w) ? 2 : 3)) : ((qAbs.y > qAbs.z) ? ((qAbs.y > qAbs.w) ? 1 : 3) : ((qAbs.z > qAbs.w) ? 2 : 3)); 
      float Mult = ((q[maxComponentIndex] < 0.0f) ? -1.0f : 1.0f) * 1.4142135623730951f;
      switch(maxComponentIndex) {
        case 0:
            q = new Vector4(q.y, q.z, q.w, q.w);
        break;
        case 1:
            q = new Vector4(q.x, q.z, q.w, q.w);
        break;
        case 2:
            q = new Vector4(q.x, q.y, q.w, q.w);
        break;
        case 3:
        break;
      }
      q = new Vector4(q.x * Mult, q.y * Mult, q.z * Mult, q.w);
      return (((uint)(Mathf.Round(Mathf.Clamp(q.x * 511.0f, -511.0f, 511.0f) + 512.0f)) & 0x3ffu) << 0) | 
             (((uint)(Mathf.Round(Mathf.Clamp(q.y * 511.0f, -511.0f, 511.0f) + 512.0f)) & 0x3ffu) << 10) | 
             (((uint)(Mathf.Round(Mathf.Clamp(q.z * 255.0f, -255.0f, 255.0f) + 256.0f)) & 0x1ffu) << 20) |
             (((uint)(((Vector3.Dot(Vector3.Cross(T, N), B) * r) < 0.0f) ? 1u : 0u) & 0x1u) << 29) | 
             (((uint)(maxComponentIndex) & 0x3u) << 30);
    }

    public static void decodeQTangentUI32(uint v, ref Vector3 T, ref Vector3 N){
        Vector4 q = new Vector4(
            (((int)((v >> 0) & 0x3ffu) - 512) / 511.0f) * 0.7071067811865475f,
            (((int)((v >> 10) & 0x3ffu) - 512) / 511.0f) * 0.7071067811865475f,
            (((int)((v >> 20) & 0x1ffu) - 256) / 255.0f) * 0.7071067811865475f,
            0
            );
        q.w = Mathf.Sqrt(1.0f - Mathf.Clamp(Vector3.Dot(new Vector3(q.x, q.y, q.z), new Vector3(q.x, q.y, q.z)), 0.0f, 1.0f));
        switch((uint)((v >> 30) & 0x3u)) {
            case 0:
                q = new Vector4(q.w, q.x, q.y, q.z);
            break;
            case 1:
                q = new Vector4(q.x, q.w, q.y, q.z);
            break;
            case 2:
                q = new Vector4(q.x, q.y, q.w, q.z);
            break;
            case 3:
            break;
        }
        q = q.normalized;
        Vector3 t2 = new Vector3(q.x * 2.0f, q.y * 2.0f, q.z * 2.0f);
        Vector3 tx = t2 * q.x;
        Vector3 ty = t2 * q.y;
        Vector3 tz = t2 * q.w;
        T = (new Vector3(1.0f - (ty.y + (q.z * t2.z)), tx.y + tz.z, tx.z - tz.y)).normalized;
        N = (new Vector3(tx.z + tz.y, ty.z - tz.x, 1.0f - (tx.x + ty.y))).normalized;
        return;
    }

    }
}
