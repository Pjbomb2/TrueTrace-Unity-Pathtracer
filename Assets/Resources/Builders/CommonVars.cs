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
        public float energy;
        public float CDF;
        public int Type;
        public Vector2 SpotAngle;
        public float ZAxisRotation;
    }

    [System.Serializable]
    public struct VolumetricVoxelDat
    {
        public float Density;
        public int Index;
    }


    [System.Serializable]
    public struct TriNodePairData
    {
        public int TriIndex;
        public int NodeIndex;
    }

    [System.Serializable]
    public struct Voxel
    {
        public uint Index;
        public int Material;
        public int InArrayIndex;
    }

    [System.Serializable]
    public struct VolumetricVoxelData
    {
        public float Density;
        public int InArrayIndex;
        public int Index;
        public Vector3 Location;
    }

    [System.Serializable]
    public struct Brick
    {
        public int StartingIndex;
        public int IndexCount;
        public Vector3 BBMax;
        public Vector3 BBMin;
        public int Depth;
    }

    [System.Serializable]
    public struct NewBrick
    {
        public int StartingIndex;
        public Vector3 BBMax;
        public Vector3 BBMin;
        public List<Voxel> BrickVoxels;
    }
    [System.Serializable]
    public struct NewVolumeBrick
    {
        public int StartingIndex;
        public Vector3 BBMax;
        public Vector3 BBMin;
        public List<VolumetricVoxelDat> BrickVoxels;
        public float Density;
    }
    [System.Serializable]
    public struct GPUBrick
    {
        public int StartingIndex;
    }
    [System.Serializable]
    public struct GPUBrick2
    {
        public int StartingIndex;
        public Vector3 BBMax;
        public Vector3 BBMin;
    }




    [System.Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public unsafe struct OctreeNode
    {
        [System.Runtime.InteropServices.FieldOffset(0)] public fixed int ChildNode[8];
        [System.Runtime.InteropServices.FieldOffset(32)] public fixed bool IsChild[8];
        [System.Runtime.InteropServices.FieldOffset(40)] public Vector3 BBMax;
        [System.Runtime.InteropServices.FieldOffset(52)] public Vector3 BBMin;
        [System.Runtime.InteropServices.FieldOffset(64)] public Vector3 Center;
        [System.Runtime.InteropServices.FieldOffset(76)] public Vector3 Extent;
        [System.Runtime.InteropServices.FieldOffset(88)] public int InArrayIndex;
    }

    [System.Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    public unsafe struct VolumetricOctreeNode
    {
        [System.Runtime.InteropServices.FieldOffset(0)] public fixed int ChildNode[8];
        [System.Runtime.InteropServices.FieldOffset(32)] public fixed bool IsChild[8];
        [System.Runtime.InteropServices.FieldOffset(40)] public Vector3 BBMax;
        [System.Runtime.InteropServices.FieldOffset(52)] public Vector3 BBMin;
        [System.Runtime.InteropServices.FieldOffset(64)] public Vector3 Center;
        [System.Runtime.InteropServices.FieldOffset(76)] public Vector3 Extent;
        [System.Runtime.InteropServices.FieldOffset(88)] public int InArrayIndex;
        [System.Runtime.InteropServices.FieldOffset(92)] public float Density;

    }

    [System.Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    unsafe public struct CompressedOctreeNode
    {
        [System.Runtime.InteropServices.FieldOffset(0)] public uint imask;
        [System.Runtime.InteropServices.FieldOffset(4)] public uint base_index_child;
        [System.Runtime.InteropServices.FieldOffset(8)] public uint base_index_triangle;
        [System.Runtime.InteropServices.FieldOffset(12)] public uint Max;
        [System.Runtime.InteropServices.FieldOffset(16)] public uint Min;
        [System.Runtime.InteropServices.FieldOffset(20)] public fixed byte meta[8];//might be able to pack in material data as well so I could have it ignore glass!

    }

    [System.Serializable]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    unsafe public struct VolumetricCompressedOctreeNode
    {
        [System.Runtime.InteropServices.FieldOffset(0)] public uint imask;
        [System.Runtime.InteropServices.FieldOffset(4)] public uint base_index_child;
        [System.Runtime.InteropServices.FieldOffset(8)] public uint base_index_triangle;
        [System.Runtime.InteropServices.FieldOffset(12)] public uint Max;
        [System.Runtime.InteropServices.FieldOffset(16)] public uint Min;
        [System.Runtime.InteropServices.FieldOffset(20)] public float Density;
        [System.Runtime.InteropServices.FieldOffset(24)] public fixed byte meta[8];//might be able to pack in material data as well so I could have it ignore glass!

    }

    [System.Serializable]
    unsafe public struct GPUOctreeNode
    {
        public uint node_1x;
        public uint node_1y;
        public uint Meta1;
        public uint Meta2;
        public Vector3 Center;
    }

    [System.Serializable]
    unsafe public struct VolumetricGPUOctreeNode
    {
        public uint node_1x;
        public uint node_1y;
        public uint Meta1;
        public uint Meta2;
        public float Density;
        public Vector3 Center;

    }

    [System.Serializable]
    public struct GPUVoxel
    {
        public int Index;
        public int Material;
    }


    [System.Serializable]
    public struct MeshDat
    {
        public List<int> Indices;
        public List<Vector3> Verticies;
        public List<Vector3> Normals;
        public List<Vector4> Tangents;
        public List<Vector2> UVs;
        public List<int> MatDat;

        public void SetUvZero(int Count)
        {
            for (int i = 0; i < Count; i++)
            {
                UVs.Add(new Vector2(0.0f, 0.0f));
            }
        }
        public void SetTansZero(int Count)
        {
            for (int i = 0; i < Count; i++)
            {
                Tangents.Add(new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
            }
        }
        public void init()
        {
            this.Tangents = new List<Vector4>();
            this.MatDat = new List<int>();
            this.UVs = new List<Vector2>();
            this.Verticies = new List<Vector3>();
            this.Normals = new List<Vector3>();
            this.Indices = new List<int>();
        }
        public void Clear()
        {
            if (Tangents != null)
            {
                this.Tangents.Clear();
                this.MatDat.Clear();
                this.UVs.Clear();
                this.Verticies.Clear();
                this.Normals.Clear();
                this.Indices.Clear();
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
        public int HasAlbedoTex;
        public int HasNormalTex;
        public int HasEmissiveTex;
        public int HasMetallicTex;
        public int HasRoughnessTex;
        public Vector3 BaseColor;
        public float emmissive;
        public Vector3 EmissionColor;
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


    public struct VoxelObjectData
    {
        public byte[] colors;
        public Vector3 Size;
        public Vector3 Translation;
        public Matrix4x4 Rotation;
    }

    [System.Serializable]
    public struct PrimitiveData
    {
        public AABB aabb;
        public Vector3 Center;
        public Vector3 V1;
        public Vector3 V2;
        public Vector3 V3;
        public Vector3 Norm1;
        public Vector3 Norm2;
        public Vector3 Norm3;
        public Vector3 Tan1;
        public Vector3 Tan2;
        public Vector3 Tan3;
        public Vector2 tex1;
        public Vector2 tex2;
        public Vector2 tex3;
        public int MatDat;

        public void Reconstruct()
        {
            Vector3 BBMin = Vector3.Min(Vector3.Min(V1, V2), V3);
            Vector3 BBMax = Vector3.Max(Vector3.Max(V1, V2), V3);
            for (int i2 = 0; i2 < 3; i2++)
            {
                if (BBMax[i2] - BBMin[i2] < 0.001f)
                {
                    BBMin[i2] -= 0.001f;
                    BBMax[i2] += 0.001f;
                }
            }
            Center = (V1 + V2 + V3) / 3.0f;
            aabb.BBMax = BBMax;
            aabb.BBMin = BBMin;
        }
        public void Reconstruct(Vector3 Scale)
        {
            Vector3 BBMin = Vector3.Min(Vector3.Min(V1, V2), V3);
            Vector3 BBMax = Vector3.Max(Vector3.Max(V1, V2), V3);
            for (int i2 = 0; i2 < 3; i2++)
            {
                if (BBMax[i2] - BBMin[i2] < 0.001f / Scale[i2])
                {
                    BBMin[i2] -= 0.001f / Scale[i2];
                    BBMax[i2] += 0.001f / Scale[i2];
                }
            }
            Center = (V1 + V2 + V3) / 3.0f;
            aabb.BBMax = BBMax;
            aabb.BBMin = BBMin;
        }
    }

    public struct ProgReportData
    {
        public int Id;
        public string Name;
        public int TriCount;
        public void init(int Id, string Name, int TriCount)
        {
            this.Id = Id;
            this.Name = Name;
            this.TriCount = TriCount;
        }
    }

    [System.Serializable]
    public struct MyMeshDataCompacted
    {
        public Matrix4x4 Transform;
        public Matrix4x4 Inverse;
        public int AggIndexCount;
        public int AggNodeCount;
        public int MaterialOffset;
        public int mesh_data_bvh_offsets;
        public uint IsVoxel;
        public int SizeX;
        public int SizeY;
        public int SizeZ;
        public int LightTriCount;
        public float LightPDF;
        //I do have the space to store 1 more int and 1 more other value to align to 128 bits
    }

    [System.Serializable]
    public struct AABB
    {
        public Vector3 BBMax;
        public Vector3 BBMin;

        public void Extend(ref AABB aabb)
        {
            this.BBMax = new Vector3(Mathf.Max(BBMax.x, aabb.BBMax.x), Mathf.Max(BBMax.y, aabb.BBMax.y), Mathf.Max(BBMax.z, aabb.BBMax.z));
            this.BBMin = new Vector3(Mathf.Min(BBMin.x, aabb.BBMin.x), Mathf.Min(BBMin.y, aabb.BBMin.y), Mathf.Min(BBMin.z, aabb.BBMin.z));
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
        public int PreviousNode;
        public int BVHNode;
        public int Node;
        public CommonVars.AABB AABB;
        public int InNodeOffset;
        public int IsLeaf;
        public int RecursionCount;
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

        public Vector3 norm0;
        public Vector3 normedge1;
        public Vector3 normedge2;

        public Vector3 tan0;
        public Vector3 tanedge1;
        public Vector3 tanedge2;

        public Vector2 tex0;
        public Vector2 texedge1;
        public Vector2 texedge2;

        public uint MatDat;
    }

    [System.Serializable]
    public struct CudaLightTriangle
    {
        public Vector3 pos0;
        public Vector3 posedge1;
        public Vector3 posedge2;
        public Vector3 Norm;
        public Vector2 UV1;
        public Vector2 UV2;
        public Vector2 UV3;
        public int MatIndex;
        public Vector3 radiance;
        public float sumEnergy;
        public float energy;
        public float area;
    }

    [System.Serializable]
    public struct LightMeshData
    {
        public Matrix4x4 Inverse;
        public Vector3 Center;
        public float energy;
        public float CDF;
        public int StartIndex;
        public int IndexEnd;
        public int MatOffset;
    }

    [System.Serializable]
    public struct Layer
    {
        unsafe public fixed int Children[8];
        unsafe public fixed int Leaf[8];

    }


    [System.Serializable]
    public struct SplitLayer
    {
        public int Child1;
        public int Child2;
        public int Child3;
        public int Child4;
        public int Child5;
        public int Child6;
        public int Child7;
        public int Child8;


        public int Leaf1;
        public int Leaf2;
        public int Leaf3;
        public int Leaf4;
        public int Leaf5;
        public int Leaf6;
        public int Leaf7;
        public int Leaf8;
    }

    [System.Serializable]
    public struct Layer2
    {
        public List<int> Slab;
    }

    [System.Serializable]
    public struct RayObjectTextureIndex
    {
        public RayTracingObject Obj;
        public TerrainObject Terrain;
        public int ObjIndex;
    }

    [System.Serializable]
    public class RayObjects
    {
        public List<RayObjectTextureIndex> RayObjectList = new List<RayObjectTextureIndex>();
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
        public string RoughnessTex;
        public int RoughnessTexChannel;
    }
    [System.Serializable]
    public class Materials
    {
        [System.Xml.Serialization.XmlElement("MaterialShader")]
        public List<MaterialShader> Material = new List<MaterialShader>();
    }

    public static class CommonFunctions
    {
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
                matrix[0, 0] * direction.x + matrix[0, 1] * direction.y + matrix[0, 2] * direction.z,
                matrix[1, 0] * direction.x + matrix[1, 1] * direction.y + matrix[1, 2] * direction.z,
                matrix[2, 0] * direction.x + matrix[2, 1] * direction.y + matrix[2, 2] * direction.z
            );
        }
        public static void abs(ref Matrix4x4 matrix)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int i2 = 0; i2 < 4; i2++) matrix[i, i2] = Mathf.Abs(matrix[i, i2]);
            }
        }

    }



}
