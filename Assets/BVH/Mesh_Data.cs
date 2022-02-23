using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;

[System.Serializable]
public class Mesh_Data {
    public string Name;
    public AABB aabb_untransformed;
    public AABB aabb;
    public Vector3 Position;
    public Matrix4x4 Transform;
    public List<PrimitiveData> triangles;
    public BVH8Builder BVH;
    public List<BVHNode8DataCompressed> Aggregated_BVH_Nodes;
    public int ObjectGroup;
    public int progressId = 0;

    public Mesh_Data(string Name, List<PrimitiveData> triangles, int progressId) {
        this.Name = Name;
        this.triangles = triangles;
        this.progressId = progressId;
    }

    public async Task Construct() {
        BVH2Builder BVH2 = new BVH2Builder(this.triangles, progressId);//Binary BVH Builder, and also the component that takes the longest to build
        this.BVH = new BVH8Builder(BVH2);
        BVH.BVH8Nodes.RemoveRange(BVH.cwbvhnode_count, BVH.BVH8Nodes.Count - BVH.cwbvhnode_count);
    }

    public void UpdateAABB() {//Update the Transformed AABB by getting the new Max/Min of the untransformed AABB after transforming it
        Bounds TempBound = new Bounds();
        TempBound.max = aabb_untransformed.BBMax;
        TempBound.min = aabb_untransformed.BBMin;
        List<Vector3> Points = new List<Vector3>();
            Points.Add(Transform * (new Vector3(TempBound.center.x + TempBound.extents.x, TempBound.center.y + TempBound.extents.y, TempBound.center.z + TempBound.extents.z)));
            Points.Add(Transform * (new Vector3(TempBound.center.x + TempBound.extents.x, TempBound.center.y + TempBound.extents.y, TempBound.center.z - TempBound.extents.z)));
            Points.Add(Transform * (new Vector3(TempBound.center.x + TempBound.extents.x, TempBound.center.y - TempBound.extents.y, TempBound.center.z + TempBound.extents.z)));
            Points.Add(Transform * (new Vector3(TempBound.center.x + TempBound.extents.x, TempBound.center.y - TempBound.extents.y, TempBound.center.z - TempBound.extents.z)));
            Points.Add(Transform * (new Vector3(TempBound.center.x - TempBound.extents.x, TempBound.center.y + TempBound.extents.y, TempBound.center.z + TempBound.extents.z)));
            Points.Add(Transform * (new Vector3(TempBound.center.x - TempBound.extents.x, TempBound.center.y + TempBound.extents.y, TempBound.center.z - TempBound.extents.z)));
            Points.Add(Transform * (new Vector3(TempBound.center.x - TempBound.extents.x, TempBound.center.y - TempBound.extents.y, TempBound.center.z + TempBound.extents.z)));
            Points.Add(Transform * (new Vector3(TempBound.center.x - TempBound.extents.x, TempBound.center.y - TempBound.extents.y, TempBound.center.z - TempBound.extents.z)));
            
            Vector3 BBMAX = Points[0] + Position;
            Vector3 BBMIN = Points[0] + Position;
            for(int i2 = 1; i2 < Points.Count; ++i2) {
                BBMAX = Vector3.Max(BBMAX, (Points[i2] + Position));
                BBMIN = Vector3.Min(BBMIN, (Points[i2] + Position));
            }
        aabb.BBMax = BBMAX;
        aabb.BBMin = BBMIN;
    }

    public void ConstructAABB() {
        aabb_untransformed = new AABB();
        aabb_untransformed.init();
        for(int i = 0; i < triangles.Count; i++) {
            aabb_untransformed.Extend(triangles[i].BBMax, triangles[i].BBMin);
        }
    }


    public void Aggregate() {//Compress the CWBVH
        Aggregated_BVH_Nodes = new List<BVHNode8DataCompressed>();
        for(int i = 0; i < BVH.BVH8Nodes.Count; ++i) {
            BVHNode8Data TempNode = BVH.BVH8Nodes[i];
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
            Aggregated_BVH_Nodes.Add(new BVHNode8DataCompressed() {
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

}
