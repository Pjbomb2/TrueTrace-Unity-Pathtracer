using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.Assertions;
using System; 
using CommonVars;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace TrueTrace {
    [System.Serializable]
    unsafe public class BVH8Builder {



        float Dot(ref Vector3 A, ref Vector3 B) {return A.x * B.x + A.y * B.y + A.z * B.z;}
        float Dot(Vector3 A) {return A.x * A.x + A.y * A.y + A.z * A.z;}
        float Length(Vector3 a) {return (float)Math.Sqrt((double)(a.x * a.x + a.y * a.y + a.z * a.z));}
        float Length(ref Vector3 a) {return (float)Math.Sqrt((double)(a.x * a.x + a.y * a.y + a.z * a.z));}

        public float Distance(Vector3 a, Vector3 b)
        {
            a.x -= b.x;
            a.y -= b.y;
            a.z -= b.z;
            return Length(ref a);
        }

        public void Normalize(ref Vector3 a)
        {
            float num = Length(ref a);
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

        public float* cost;
        public Decision* decisions;
        public int[] cwbvh_indices;
        public BVHNode8Data* BVH8Nodes;
        public int cwbvhindex_count;
        public int cwbvhnode_count;
        public BVHNode2Data* nodes;


        public NativeArray<float> costArray;
        public NativeArray<Decision> decisionsArray;
        public NativeArray<BVHNode8Data> BVH8NodesArray;
        public BVHNode8Data ZeroedData = new BVHNode8Data();
        
        public struct Decision {
            public int Type;//Types: 0 is LEAF, 1 is INTERNAL, 2 is DISTRIBUTE
            public int dist_left;
            public int dist_right;
        }
        void FillArrayFast(ref int[] Target) {
            Target[0] = -1;
            Target[1] = -1;
            Target[2] = -1;
            Target[3] = -1;
            Target[4] = -1;
            Target[5] = -1;
            Target[6] = -1;
            Target[7] = -1;
        }
        int calculate_cost(int node_index) {
            Decision dectemp;
            BVHNode2Data node = nodes[node_index];
            node_index *= 7;
            int num_primitives;

            if(node.count > 0) {
                num_primitives = (int)node.count;
                //SAH Cost
                float cost_leaf = surface_area(ref node.aabb) * (float)num_primitives;
                for(int i = 0; i < 7; i++) {
                    cost[node_index + i] = cost_leaf;
                    decisions[node_index + i].Type = 0;
                }
            } else {
                num_primitives = 
                    calculate_cost(node.left) +
                    calculate_cost(node.left + 1);
                        {
                            float cost_leaf = num_primitives <= 3 ? (float)num_primitives * surface_area(ref node.aabb) : float.MaxValue;

                            float cost_distribute = float.MaxValue;

                            int dist_left = -1;
                            int dist_right = -1;

                            for(int k = 0; k < 7; k++) {
                                float c = cost[node.left * 7 + k] + cost[(node.left + 1) * 7 + 6 - k];
                        
                                if(c < cost_distribute) {
                                    cost_distribute = c;

                                    dist_left = k;
                                    dist_right = 6 - k;
                                }
                            }

                            float cost_internal = cost_distribute + surface_area(ref node.aabb);
                            if(cost_leaf < cost_internal) {
                                cost[node_index] = cost_leaf;

                                decisions[node_index].Type = 0;
                            } else {
                                cost[node_index] = cost_internal;

                                decisions[node_index].Type = 1;
                            }

                            decisions[node_index].dist_left = dist_left;
                            decisions[node_index].dist_right = dist_right;
                        }

                        for(int i = 1; i < 7; i++) {
                            float cost_distribute = cost[node_index + i - 1];
                            int dist_left = -1;
                            int dist_right = -1;

                            for(int k = 0; k < i; k++) {
                                float c = cost[node.left * 7 + k] + cost[(node.left + 1) * 7 + i - k - 1];

                                    if(c < cost_distribute) {
                                        cost_distribute = c;

                                        dist_left = k;
                                        dist_right = i - k - 1;
                                    }
                            }

                            cost[node_index + i] = cost_distribute;

                            if(dist_left != -1) {
                                decisions[node_index + i].Type = 2;
                                decisions[node_index + i].dist_left = dist_left;
                                decisions[node_index + i].dist_right = dist_right;
                            } else {
                                decisions[node_index + i] = decisions[node_index + i - 1];
                            }
                        }
            }
            return num_primitives;
        }


        void get_children(int node_index, int i, ref int child_count, ref int[] children) {

            if(nodes[node_index].count > 0) {
                children[child_count++] = node_index;
                return;
            }

            int dist_left = decisions[node_index * 7 + i].dist_left;
            int dist_right = decisions[node_index * 7 + i].dist_right;


            if(decisions[nodes[node_index].left * 7 + dist_left].Type == 2) {
                get_children(nodes[node_index].left, dist_left, ref child_count, ref children);
            } else {
                children[child_count++] = nodes[node_index].left;
            }

            if(decisions[(nodes[node_index].left + 1) * 7 + dist_right].Type == 2) {
                get_children(nodes[node_index].left + 1, dist_right, ref child_count, ref children);   
            } else {
                children[child_count++] = nodes[node_index].left + 1;
            }
        }
        float[,] cost2;
        float[,] costpreset;
        int[] children_copy;
        int[] assignment;
        bool[] slot_filled;
        void order_children(int node_index, ref int[] children, int child_count) {
            Vector3 p = (nodes[node_index].aabb.BBMax + nodes[node_index].aabb.BBMin) / 2.0f;

            cost2 = costpreset;//try moving these out into public variables, since they can be the same array, as they get overwritten every time, just need their values initialized to 0?
            FillArrayFast(ref assignment);
            System.Array.Fill(slot_filled, false);  
            Vector3 Temp;
            Vector3 direction;
            for(int c = 0; c < child_count; c++) {
                for(int s = 0; s < 8; s++) {
                    direction = new Vector3(
                        (((s >> 2) & 1) == 1) ? -1.0f : 1.0f,
                        (((s >> 1) & 1) == 1) ? -1.0f : 1.0f,
                        (((s >> 0) & 1) == 1) ? -1.0f : 1.0f);
                    Temp = (nodes[children[c]].aabb.BBMax + nodes[children[c]].aabb.BBMin) / 2.0f - p;

                    cost2[c,s] = Dot(ref Temp, ref direction);
                }
            }


            while(true) {
                float min_cost = float.MaxValue;

                int min_slot = -1;
                int min_index = -1;

                for(int c = 0; c < child_count; c++) {
                    if(assignment[c] == -1) {
                        for(int s = 0; s < 8; s++) {
                            if(!slot_filled[s] && cost2[c,s] < min_cost) {
                                min_cost = cost2[c,s];

                                min_slot = s;
                                min_index = c;
                            }
                        }
                    }
                }

                if(min_slot == -1) break;

                slot_filled[min_slot] = true;
                assignment[min_index] = min_slot;
            }

            
            System.Array.Copy(children, children_copy, 8);

            FillArrayFast(ref children);

            for(int i = 0; i < child_count; i++) {
                children[assignment[i]] = children_copy[i];
            }
        }

        uint count_primitives(int node_index, ref int[] indices) {

            uint NodeCount = nodes[node_index].count;
            if(NodeCount > 0) {
                for(uint i = 0; i < NodeCount; i++)
                    cwbvh_indices[cwbvhindex_count++] = indices[nodes[node_index].left + i];
                return NodeCount;
            }
            return count_primitives(nodes[node_index].left, ref indices) + count_primitives(nodes[node_index].left + 1, ref indices);
        }
        unsafe float NextPowerOfTwoFloatUnsafe(float x)
        {
            if (x <= 0f) return 0f;

            int bits = *(int*)&x;
            int exponent = ((bits >> 23) & 0xFF) - 127;
            bool isExact = (bits & 0x7FFFFF) == 0;

            int power = isExact ? exponent : exponent + 1;
            int resultBits = (power + 127) << 23;
            return *(float*)&resultBits;
        }


    unsafe void collapse(ref int[] indices_bvh, int node_index_cwbvh, int node_index_bvh) {
          BVHNode8Data node = BVH8Nodes[node_index_cwbvh];
          AABB aabb = nodes[node_index_bvh].aabb;
          
          node.p = aabb.BBMin;

          int Nq = 8;
          float denom = 1.0f / (float)((1 << Nq) - 1);

          Vector3 size = (aabb.BBMax - aabb.BBMin) * denom;
        Vector3 e = new Vector3(
          NextPowerOfTwoFloatUnsafe(size.x),
          NextPowerOfTwoFloatUnsafe(size.y),
          NextPowerOfTwoFloatUnsafe(size.z)
          );

        Vector3 one_over_e = new Vector3(1.0f / e.x, 1.0f / e.y, 1.0f / e.z);

        node.e[0] = (byte)((*(uint*)&e.x) >> 23);
        node.e[1] = (byte)((*(uint*)&e.y) >> 23);
        node.e[2] = (byte)((*(uint*)&e.z) >> 23);

        int child_count = 0;
        int[] children = {-1, -1, -1, -1, -1, -1, -1, -1};

        get_children(node_index_bvh, 0, ref child_count, ref children);

        order_children(node_index_bvh, ref children, child_count);

        node.imask = 0;

        node.base_index_child = (uint)cwbvhnode_count;
        node.base_index_triangle = (uint)cwbvhindex_count;

        int node_internal_count = 0;
        uint node_triangle_count = 0;
        AABB child_aabb;
        for(int i = 0; i < 8; i++) {
            int child_index = children[i];
            if(child_index == -1) continue;

            child_aabb = nodes[child_index].aabb;

            float min_x = (child_aabb.BBMin.x - node.p.x) * one_over_e.x;
            float min_y = (child_aabb.BBMin.y - node.p.y) * one_over_e.y;
            float min_z = (child_aabb.BBMin.z - node.p.z) * one_over_e.z;

            float max_x = (child_aabb.BBMax.x - node.p.x) * one_over_e.x;
            float max_y = (child_aabb.BBMax.y - node.p.y) * one_over_e.y;
            float max_z = (child_aabb.BBMax.z - node.p.z) * one_over_e.z;

            node.quantized_min_x[i] = (byte)(uint)min_x;
            node.quantized_min_y[i] = (byte)(uint)min_y;
            node.quantized_min_z[i] = (byte)(uint)min_z;

            node.quantized_max_x[i] = (byte)(uint)(max_x + 0.9999999f);
            node.quantized_max_y[i] = (byte)(uint)(max_y + 0.9999999f);
            node.quantized_max_z[i] = (byte)(uint)(max_z + 0.9999999f);


            int type = decisions[child_index * 7].Type;
            if(type == 0) {
                uint triangle_count = count_primitives(child_index, ref indices_bvh);
                for(int j = 0; j < triangle_count; j++)
                    node.meta[i] |= (byte)(1 << (j + 5));
                node.meta[i] |= (byte)node_triangle_count;
                node_triangle_count += triangle_count;
            } else if(type == 1) {
                node.meta[i] = (byte)((node_internal_count + 24) | 0b0010_0000);
                node.imask |= (byte)(1 << node_internal_count);
                cwbvhnode_count++;
                node_internal_count++;
            }
        }

        BVH8Nodes[node_index_cwbvh] = node;
        for(int i = 0; i < 8; i++) {
            int child_index = children[i];
            if(child_index == -1) continue;
    
            if(decisions[child_index * 7].Type == 1) {
                collapse(ref indices_bvh, (int)node.base_index_child + ((byte)node.meta[i] & 31) - 24, child_index);
            }
        }
    }

        float surface_area(ref AABB aabb) {
            Vector3 sizes = aabb.BBMax - aabb.BBMin;
            return 2.0f * ((sizes.x * sizes.y) + (sizes.x * sizes.z) + (sizes.y * sizes.z)); 
        }



        public BVH8Builder() {}//null constructor
        
        public void Dispose() {
            if(decisionsArray.IsCreated) decisionsArray.Dispose();
            if(costArray.IsCreated) costArray.Dispose();
            if(BVH8NodesArray.IsCreated) BVH8NodesArray.Dispose();
            CommonFunctions.DeepClean(ref cwbvh_indices);

        }
        // public int PrevAlloc = 0;
        public BVH8Builder(ref BVH2Builder BVH2) {//Bottom Level CWBVH Builder
            int BVH2NodesCount = BVH2.BVH2NodesArray.Length;
            int BVH2IndicesCount = BVH2.FinalIndices.Length;
            cost2 = new float[8,8];
            costpreset = new float[8,8];
            costArray = new NativeArray<float>(BVH2NodesCount * 7,  Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            decisionsArray = new NativeArray<Decision>(BVH2NodesCount * 7,  Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            cost = (float*)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(costArray);
            decisions = (Decision*)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(decisionsArray);
            
            
            nodes = BVH2.BVH2Nodes;
            assignment = new int[8];
            slot_filled = new bool[8];
            children_copy = new int[8];
            cwbvhindex_count = 0;
            cwbvhnode_count = 1;
            calculate_cost(0);
            cwbvh_indices = new int[BVH2IndicesCount];
            BVH8NodesArray = new NativeArray<BVHNode8Data>(BVH2NodesCount, Unity.Collections.Allocator.Persistent, NativeArrayOptions.ClearMemory);
            BVH8Nodes = (BVHNode8Data*)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(BVH8NodesArray);
            collapse(ref BVH2.FinalIndices, 0, 0);
            BVH2.BVH2NodesArray.Dispose();
            costArray.Dispose();
            decisionsArray.Dispose();

        }

        public BVH8Builder(BVH2Builder BVH2) {//Top Level CWBVH Builder
            int BVH2NodesCount = BVH2.BVH2NodesArray.Length;
            int BVH2IndicesCount = BVH2.FinalIndices.Length;
            cost2 = new float[8,8];
            costpreset = new float[8,8];
            costArray = new NativeArray<float>(BVH2NodesCount * 7,  Unity.Collections.Allocator.Persistent, NativeArrayOptions.ClearMemory);
            decisionsArray = new NativeArray<Decision>(BVH2NodesCount * 7,  Unity.Collections.Allocator.Persistent, NativeArrayOptions.ClearMemory);
            cost = (float*)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(costArray);
            decisions = (Decision*)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(decisionsArray);
            
            nodes = BVH2.BVH2Nodes;
            children_copy = new int[8];
            assignment = new int[8];
            slot_filled = new bool[8];

            cwbvhindex_count = 0;
            cwbvhnode_count = 1;

            calculate_cost(0);

            cwbvh_indices = new int[BVH2IndicesCount];
            BVH8NodesArray = new NativeArray<BVHNode8Data>(BVH2NodesCount, Unity.Collections.Allocator.Persistent, NativeArrayOptions.ClearMemory);
            BVH8Nodes = (BVHNode8Data*)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(BVH8NodesArray);

            collapse(ref BVH2.FinalIndices, 0, 0);

        }
        public void ClearAll() {
            if(costArray.IsCreated) costArray.Dispose();
            if(decisionsArray.IsCreated) decisionsArray.Dispose();            
            if(BVH8NodesArray.IsCreated) BVH8NodesArray.Dispose();            
        }


        public void NoAllocRebuild(BVH2Builder BVH2) {//Top Level CWBVH Builder
            int BVH2NodesCount = BVH2.BVH2NodesArray.Length;
            int BVH2IndicesCount = BVH2.FinalIndices.Length;
            for(int i = 0; i < 8; i++) {
                for(int j = 0; j < 8; j++) {
                    cost2[i,j] = 0;
                    costpreset[i,j] = 0;
                }
                children_copy[i] = 0;
                assignment[i] = 0;
                slot_filled[i] = false;
            }
            cost = (float*)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(costArray);
            decisions = (Decision*)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(decisionsArray);
            for(int i = 0; i < BVH2NodesCount; i++)
                BVH8Nodes[i] = ZeroedData;
            
            nodes = BVH2.BVH2Nodes;

            cwbvhindex_count = 0;
            cwbvhnode_count = 1;

            calculate_cost(0);

            collapse(ref BVH2.FinalIndices, 0, 0);
        }
    }
}