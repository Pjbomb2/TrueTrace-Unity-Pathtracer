using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonVars {

    [System.Serializable]
    public struct LightData {
        public Vector3 Radiance;
        public Vector3 Position;
        public Vector3 Direction;
        public float energy;
        public float TotalEnergy;
        public int Type;
        public Vector2 SpotAngle;
    }



    [System.Serializable]
    public struct MeshDat {
        public List<int> Indices;
        public List<Vector3> Verticies;
        public List<Vector3> Normals;
        public List<Vector2> UVs;
        public List<int> MatDat;

        public void SetUvZero(int Count) {
            for(int i = 0; i < Count; i++) {
                UVs.Add(new Vector2(0.0f, 0.0f));
            }
        }
        public void init() {
            this.MatDat = new List<int>();
            this.UVs = new List<Vector2>();
            this.Verticies = new List<Vector3>();
            this.Normals = new List<Vector3>();
            this.Indices = new List<int>();
        }
        public void Clear() {
            this.MatDat.Clear();
            this.UVs.Clear();
            this.Verticies.Clear();
            this.Normals.Clear();
            this.Indices.Clear();
        }
    }

    [System.Serializable]
    public struct MaterialData {
        public Vector3 BaseColor;
        public Vector2 TexMax;
        public Vector2 TexMin;
        public float emmissive;
        public float Roughness;
        public int HasTextures;
        public int MatType;
        public Vector3 eta;
    }

    [System.Serializable]
    public struct BVHNode2Data {
        public Vector3 BBMax;
        public Vector3 BBMin;
        public int left;    
        public int first;
        public uint count;
        public uint axis;
    }
    
    [System.Serializable]
    public struct BVHNode8Data {
        public Vector3 p;
        public byte[] e;
        public byte imask;    
        public uint base_index_child;
        public uint base_index_triangle;
        public byte[] meta;
        public byte[] quantized_min_x;
        public byte[] quantized_max_x;
        public byte[] quantized_min_y;
        public byte[] quantized_max_y;
        public byte[] quantized_min_z;
        public byte[] quantized_max_z;
    }

    [System.Serializable]
    public struct PrimitiveData {
        public Vector3 BBMin;
        public Vector3 BBMax;
        public Vector3 Center;
        public Vector3 V1;
        public Vector3 V2;
        public Vector3 V3;
        public Vector3 Norm1;
        public Vector3 Norm2;
        public Vector3 Norm3;
        public Vector2 tex1;
        public Vector2 tex2;
        public Vector2 tex3;
        public int MatDat;

        public void Reconstruct() {
            BBMin = Vector3.Min(Vector3.Min(V1,V2),V3);
            BBMax = Vector3.Max(Vector3.Max(V1,V2),V3);
            for(int i2 = 0; i2 < 3; i2++) {
                if(BBMax[i2] - BBMin[i2] < 0.001f) {
                    BBMin[i2] -= 0.001f;
                    BBMax[i2] += 0.001f;
                }
            }
            Center = (V1 + V2 + V3) / 3.0f;
        }
    }

    public struct ProgReportData {
        public int Id;
        public string Name;
        public int TriCount;
        public void init(int Id, string Name, int TriCount) {
            this.Id = Id;
            this.Name = Name;
            this.TriCount = TriCount;
        }
    }
    
    [System.Serializable]
    public struct MyMeshDataCompacted {
        public Matrix4x4 Transform;
        public Matrix4x4 Inverse;
        public Vector3 Center;
        public int AggIndexCount;
        public int AggNodeCount;
        public int MaterialOffset;
        public int mesh_data_bvh_offsets;
        //I do have the space to store 1 more int and 1 more other value to align to 128 bits
    }

    [System.Serializable]
    public struct AABB {
        public Vector3 BBMax;
        public Vector3 BBMin;

        public void Extend(in Vector3 InMax, in Vector3 InMin) {
            this.BBMax = new Vector3(Mathf.Max(BBMax.x, InMax.x), Mathf.Max(BBMax.y, InMax.y), Mathf.Max(BBMax.z, InMax.z));
            this.BBMin = new Vector3(Mathf.Min(BBMin.x, InMin.x), Mathf.Min(BBMin.y, InMin.y), Mathf.Min(BBMin.z, InMin.z));
        }
        public void init() {
            BBMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            BBMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        }
    }

    [System.Serializable]
    public struct BVHNode8DataCompressed {
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
    public struct CudaTriangle {
        public Vector3 pos0;
        public Vector3 posedge1;
        public Vector3 posedge2;

        public Vector3 norm0;
        public Vector3 normedge1;
        public Vector3 normedge2;

        public Vector2 tex0;
        public Vector2 texedge1;
        public Vector2 texedge2;

        public uint MatDat;
    }

    [System.Serializable]
    public struct CudaLightTriangle {
        public Vector3 pos0;
        public Vector3 posedge1;
        public Vector3 posedge2;
        public Vector3 Norm;

        public Vector3 radiance;
        public float sumEnergy;
        public float energy;
        public float area;
    }

    [System.Serializable]
    public struct LightMeshData {
        public Matrix4x4 Inverse;
        public Vector3 Center;
        public float energy;
        public float TotalEnergy;
        public int StartIndex;
        public int IndexEnd;
    }
    
}
