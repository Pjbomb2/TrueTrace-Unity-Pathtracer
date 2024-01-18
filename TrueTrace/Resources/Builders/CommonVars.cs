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
        public List<Vector2> LightmapUVs;
        public List<int> MatDat;

        public void SetUvZero(int Count) {
            for (int i = 0; i < Count; i++) UVs.Add(new Vector2(0.0f, 0.0f));
        }
        public void AddLightmapUVs(Vector2[] LightUVs, Vector4 ScaleOffset) {
            int Count = LightUVs.Length;
            for (int i = 0; i < Count; i++) LightmapUVs.Add(new Vector2(LightUVs[i].x * ScaleOffset.x + ScaleOffset.z, LightUVs[i].y * ScaleOffset.y + ScaleOffset.w));
        }
        public void SetTansZero(int Count) {
            for (int i = 0; i < Count; i++) Tangents.Add(new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
        }
        public void init(int StartingSize) {
            this.Tangents = new List<Vector4>(StartingSize);
            this.MatDat = new List<int>(StartingSize / 3);
            this.UVs = new List<Vector2>(StartingSize);
            this.LightmapUVs = new List<Vector2>(StartingSize);
            this.Verticies = new List<Vector3>(StartingSize);
            this.Normals = new List<Vector3>(StartingSize);
            this.Indices = new List<int>(StartingSize);
        }
        public void Clear() {
            if (Tangents != null) {
                CommonFunctions.DeepClean(ref Tangents);
                CommonFunctions.DeepClean(ref MatDat);
                CommonFunctions.DeepClean(ref UVs);
                CommonFunctions.DeepClean(ref LightmapUVs);
                CommonFunctions.DeepClean(ref Verticies);
                CommonFunctions.DeepClean(ref Normals);
                CommonFunctions.DeepClean(ref Indices);
            }
        }
    }


    [System.Serializable]
    public struct MaterialData
    {
        public Vector4 AlbedoTex;
        public Vector4 NormalTex;
        public Vector4 EmissiveTex;
        public Vector4 MetallicTex;
        public Vector4 RoughnessTex;
        public Vector3 BaseColor;
        public float emmissive;
        public Vector3 EmissionColor;
        public uint Tag;
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
        public int Thin;
        public float Specular;
        public float scatterDistance;
        public int IsSmoothness;
        public Vector4 AlbedoTextureScale;
        public Vector2 MetallicRemap;
        public Vector2 RoughnessRemap;
        public float AlphaCutoff;
    }

    [System.Serializable]
    public struct BVHNode2Data
    {
        public AABB aabb;
        public int left;
        public int first;
        public uint count;
        public uint axis;
    }

    [System.Serializable]
    public struct TerrainDat
    {
        public Vector3 PositionOffset;
        public float HeightScale;
        public float TerrainDim;
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
        public Vector3 p;
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
    }

    [System.Serializable]
    public struct LightTriData
    {
        public Vector3 pos0;
        public Vector3 posedge1;
        public Vector3 posedge2;
        public uint TriTarget;
    }

    [System.Serializable]
    public struct AABB
    {
        public Vector3 BBMax;
        public Vector3 BBMin;

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

        public void init()
        {
            BBMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            BBMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        }
    }

    [System.Serializable]
    public struct LightAABB
    {
        public Vector3 BBMax;
        public Vector3 BBMin;
        public float flux;
        public Vector3 coneDirection;
        public float cosConeAngle;

        public void Create(AABB aabb, float flux, Vector3 Dir, float cosConeAngle)
        {
            this.flux = flux;
            coneDirection = Dir;
            this.cosConeAngle = cosConeAngle;
            this.BBMax = aabb.BBMax;//new Vector3(System.Math.Max(A.x, B.x), System.Math.Max(A.y, B.y), System.Math.Max(A.z, B.z));
            this.BBMin = aabb.BBMin;//new Vector3(System.Math.Min(A.x, B.x), System.Math.Min(A.y, B.y), System.Math.Min(A.z, B.z));
        }
        public float ComputeVolume() {
            return System.Math.Max((BBMax.x - BBMin.x) * (BBMax.y - BBMin.y) * (BBMax.z - BBMin.z),0.00001f);
        }

        public float ComputeSurfaceArea() {
            Vector3 sizes = BBMax - BBMin;
            return 2.0f * ((sizes.x * sizes.y) + (sizes.x * sizes.z) + (sizes.y * sizes.z)); 
        }


        public void Extend(ref LightAABB aabb)
        {
            flux += aabb.flux;
            coneDirection += aabb.coneDirection;
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

        public void init()
        {
            cosConeAngle = 1.0f;
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
        public Vector3 node_0xyz;
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

        public Vector2 tex0;
        public Vector2 texedge1;
        public Vector2 texedge2;

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
        public List<int> TexObjList = new List<int>();
    }   


    [System.Serializable]
    public class MaterialShader
    {
        public string Name;
        public string BaseColorTex;
        public string NormalTex;
        public string EmissionTex;
        public string MetallicTex;
        public int MetallicTexChannel;
        public string MetallicRange;
        public string RoughnessTex;
        public int RoughnessTexChannel;
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

        public static void CreateDynamicBuffer(ref ComputeBuffer TargetBuffer, int Count, int Stride)
        {
            if (TargetBuffer != null) TargetBuffer?.Dispose();
            TargetBuffer = new ComputeBuffer(Count, Stride);
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
                TempBVHNode.node_0xyz = new Vector3(TempNode.p.x, TempNode.p.y, TempNode.p.z);
                TempBVHNode.node_0w = (TempNode.e[0] | (TempNode.e[1] << 8) | (TempNode.e[2] << 16) | (TempNode.imask << 24));
                TempBVHNode.node_1x = TempNode.base_index_child;
                TempBVHNode.node_1y = TempNode.base_index_triangle;
                TempBVHNode.node_1z = (uint)(TempNode.meta[0] | (TempNode.meta[1] << 8) | (TempNode.meta[2] << 16) | (TempNode.meta[3] << 24));
                TempBVHNode.node_1w = (uint)(TempNode.meta[4] | (TempNode.meta[5] << 8) | (TempNode.meta[6] << 16) | (TempNode.meta[7] << 24));
                TempBVHNode.node_2x = (uint)(TempNode.quantized_min_x[0] | (TempNode.quantized_min_x[1] << 8) | (TempNode.quantized_min_x[2] << 16) | (TempNode.quantized_min_x[3] << 24));
                TempBVHNode.node_2y = (uint)(TempNode.quantized_min_x[4] | (TempNode.quantized_min_x[5] << 8) | (TempNode.quantized_min_x[6] << 16) | (TempNode.quantized_min_x[7] << 24));
                TempBVHNode.node_2z = (uint)(TempNode.quantized_max_x[0] | (TempNode.quantized_max_x[1] << 8) | (TempNode.quantized_max_x[2] << 16) | (TempNode.quantized_max_x[3] << 24));
                TempBVHNode.node_2w = (uint)(TempNode.quantized_max_x[4] | (TempNode.quantized_max_x[5] << 8) | (TempNode.quantized_max_x[6] << 16) | (TempNode.quantized_max_x[7] << 24));
                TempBVHNode.node_3x = (uint)(TempNode.quantized_min_y[0] | (TempNode.quantized_min_y[1] << 8) | (TempNode.quantized_min_y[2] << 16) | (TempNode.quantized_min_y[3] << 24));
                TempBVHNode.node_3y = (uint)(TempNode.quantized_min_y[4] | (TempNode.quantized_min_y[5] << 8) | (TempNode.quantized_min_y[6] << 16) | (TempNode.quantized_min_y[7] << 24));
                TempBVHNode.node_3z = (uint)(TempNode.quantized_max_y[0] | (TempNode.quantized_max_y[1] << 8) | (TempNode.quantized_max_y[2] << 16) | (TempNode.quantized_max_y[3] << 24));
                TempBVHNode.node_3w = (uint)(TempNode.quantized_max_y[4] | (TempNode.quantized_max_y[5] << 8) | (TempNode.quantized_max_y[6] << 16) | (TempNode.quantized_max_y[7] << 24));
                TempBVHNode.node_4x = (uint)(TempNode.quantized_min_z[0] | (TempNode.quantized_min_z[1] << 8) | (TempNode.quantized_min_z[2] << 16) | (TempNode.quantized_min_z[3] << 24));
                TempBVHNode.node_4y = (uint)(TempNode.quantized_min_z[4] | (TempNode.quantized_min_z[5] << 8) | (TempNode.quantized_min_z[6] << 16) | (TempNode.quantized_min_z[7] << 24));
                TempBVHNode.node_4z = (uint)(TempNode.quantized_max_z[0] | (TempNode.quantized_max_z[1] << 8) | (TempNode.quantized_max_z[2] << 16) | (TempNode.quantized_max_z[3] << 24));
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
                if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride) {
                    buffer.Release();
                    buffer = null;
                }
            }
            if (data.Count != 0) {
                if (buffer == null) buffer = new ComputeBuffer(data.Count, stride);
                buffer.SetData(data);
            }
        }
        public static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, T[] data)
            where T : struct
        {
            int stride = System.Runtime.InteropServices.Marshal.SizeOf<T>();
            if (buffer != null) {
                if (data.Length == 0 || buffer.count != data.Length || buffer.stride != stride) {
                    buffer.Release();
                    buffer = null;
                }
            }
            if (data.Length != 0) {
                if (buffer == null) buffer = new ComputeBuffer(data.Length, stride);
                buffer.SetData(data);
            }
        }




    }



}
