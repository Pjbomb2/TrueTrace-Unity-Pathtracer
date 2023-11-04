using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using CommonVars;

namespace TrueTrace {
    [System.Serializable]
    unsafe public class BVH8Builder {

        private float[] cost;
        private Decision[] decisions;
        public int[] cwbvh_indices;
        public BVHNode8Data[] BVH8Nodes;
        public int cwbvhindex_count;
        public int cwbvhnode_count;

        public struct Decision {
            public int Type;//Types: 0 is LEAF, 1 is INTERNAL, 2 is DISTRIBUTE
            public int dist_left;
            public int dist_right;
        }

        int calculate_cost(int node_index, ref BVHNode2Data[] nodes) {
            Decision dectemp;
            BVHNode2Data node = nodes[node_index];
            int num_primitives;

            if(node.count > 0) {
                num_primitives = (int)node.count;
                Assert.IsTrue(num_primitives == 1);
                if(num_primitives != 1) {
                    Debug.Log("BVH NODE GREATER THAN 1 TRI AT NODE: " + node_index);
                    return -1;
                }
                //SAH Cost
                float cost_leaf = surface_area(ref node.aabb) * (float)num_primitives;
                for(int i = 0; i < 7; i++) {
                    cost[node_index * 7 + i] = cost_leaf;
                    dectemp = decisions[node_index * 7 + i];
                    dectemp.Type = 0;
                    decisions[node_index * 7 + i] = dectemp;
                }
            } else {
                num_primitives = 
                    calculate_cost(node.left, ref nodes) +
                    calculate_cost(node.left + 1, ref nodes);
                        {
                            float cost_leaf = num_primitives <= 3 ? (float)num_primitives * surface_area(ref node.aabb) : float.MaxValue;

                            float cost_distribute = float.MaxValue;

                            int dist_left = -1;
                            int dist_right = -1;

                            for(int k = 0; k < 7; k++) {
                                float c = 
                                    cost[node.left * 7 + k] + 
                                    cost[(node.left + 1) * 7 + 6 - k];
                        
                                if(c < cost_distribute) {
                                    cost_distribute = c;

                                    dist_left = k;
                                    dist_right = 6 - k;
                                }
                            }

                            float cost_internal = cost_distribute + surface_area(ref node.aabb);
                            if(cost_leaf < cost_internal) {
                                cost[node_index * 7] = cost_leaf;

                                dectemp = decisions[node_index * 7];
                                dectemp.Type = 0;
                                decisions[node_index * 7] = dectemp;
                            } else {
                                cost[node_index * 7] = cost_internal;

                                dectemp = decisions[node_index * 7];
                                dectemp.Type = 1;
                                decisions[node_index * 7] = dectemp;   
                            }

                            dectemp = decisions[node_index * 7];
                            dectemp.dist_left = dist_left;
                            dectemp.dist_right = dist_right;
                            decisions[node_index * 7] = dectemp;
                        }

                        for(int i = 1; i < 7; i++) {
                            float cost_distribute = cost[node_index * 7 + i - 1];
                            int dist_left = -1;
                            int dist_right = -1;

                            for(int k = 0; k < i; k++) {
                                float c = 
                                    cost[node.left * 7 + k] + 
                                    cost[(node.left + 1) * 7 + i - k - 1];

                                    if(c < cost_distribute) {
                                        cost_distribute = c;

                                        dist_left = k;
                                        dist_right = i - k - 1;
                                    }
                            }

                            cost[node_index * 7 + i] = cost_distribute;

                            if(dist_left != -1) {
                                dectemp = decisions[node_index * 7 + i];
                                dectemp.Type = 2;
                                dectemp.dist_left = dist_left;
                                dectemp.dist_right = dist_right;
                                decisions[node_index * 7 + i] = dectemp;
                            } else {
                                decisions[node_index * 7 + i] = decisions[node_index * 7 + i - 1];
                            }
                        }
            }
            return num_primitives;
        }


        void get_children(int node_index, ref BVHNode2Data[] nodes, int i, ref int child_count, ref int[] children) {

            if(nodes[node_index].count > 0) {
                children[child_count++] = node_index;
                return;
            }

            int dist_left = decisions[node_index * 7 + i].dist_left;
            int dist_right = decisions[node_index * 7 + i].dist_right;

            Assert.IsTrue(dist_left >= 0 && dist_left < 7);
            Assert.IsTrue(dist_right >= 0 && dist_right < 7); 

            Assert.IsTrue(child_count < 8);

            if(decisions[nodes[node_index].left * 7 + dist_left].Type == 2) {
                get_children(nodes[node_index].left, ref nodes, dist_left, ref child_count, ref children);
            } else {
                children[child_count++] = nodes[node_index].left;
            }

            if(decisions[(nodes[node_index].left + 1) * 7 + dist_right].Type == 2) {
                get_children(nodes[node_index].left + 1, ref nodes, dist_right, ref child_count, ref children);   
            } else {
                children[child_count++] = nodes[node_index].left + 1;
            }
        }
        float[,] cost2;
        float[,] costpreset;
        void order_children(int node_index, ref BVHNode2Data[] nodes, ref int[] children, int child_count) {
            Vector3 p = (nodes[node_index].aabb.BBMax + nodes[node_index].aabb.BBMin) / 2.0f;

            cost2 = costpreset;//try moving these out into public variables, since they can be the same array, as they get overwritten every time, just need their values initialized to 0?

            for(int c = 0; c < child_count; c++) {
                for(int s = 0; s < 8; s++) {
                    Vector3 direction = new Vector3(
                        (((s >> 2) & 1) == 1) ? -1.0f : 1.0f,
                        (((s >> 1) & 1) == 1) ? -1.0f : 1.0f,
                        (((s >> 0) & 1) == 1) ? -1.0f : 1.0f);
                    cost2[c,s] = Vector3.Dot((nodes[children[c]].aabb.BBMax + nodes[children[c]].aabb.BBMin) / 2.0f - p, direction);
                }
            }

            int[] assignment = {-1, -1, -1, -1, -1, -1, -1, -1};
            bool[] slot_filled = new bool[8];

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

            int[] children_copy = new int[8];
            System.Array.Copy(children, children_copy, 8);

            for(int i = 0; i < 8; i++) children[i] = -1;

            for(int i = 0; i < child_count; i++) {
                Assert.IsTrue(assignment[i] != -1);
                Assert.IsTrue(children_copy[i] != -1);

                children[assignment[i]] = children_copy[i];
            }
        }

        int count_primitives(int node_index, ref BVHNode2Data[] nodes, ref int[] indices) {

            if(nodes[node_index].count > 0) {
                Assert.IsTrue(nodes[node_index].count == 1);
                for(int i = 0; i < nodes[node_index].count; i++) {
                    cwbvh_indices[cwbvhindex_count++] = indices[nodes[node_index].first + i];
                }
                return (int)nodes[node_index].count;
            }
            return count_primitives(nodes[node_index].left, ref nodes, ref indices) + count_primitives(nodes[node_index].left + 1, ref nodes, ref indices);
        }


        void collapse(ref BVHNode2Data[] nodes_bvh, ref int[] indices_bvh, int node_index_cwbvh, int node_index_bvh) {
          BVHNode8Data node = BVH8Nodes[node_index_cwbvh];
          AABB aabb = new AABB();
          aabb = nodes_bvh[node_index_bvh].aabb;
          
          node.p = aabb.BBMin;

          int Nq = 8;
          float denom = 1.0f / (float)((1 << Nq) - 1);

          Vector3 e = new Vector3(
            Mathf.Pow(2,Mathf.Ceil(Mathf.Log((aabb.BBMax.x - aabb.BBMin.x) * denom, 2))),
            Mathf.Pow(2,Mathf.Ceil(Mathf.Log((aabb.BBMax.y - aabb.BBMin.y) * denom, 2))),
            Mathf.Pow(2,Mathf.Ceil(Mathf.Log((aabb.BBMax.z - aabb.BBMin.z) * denom, 2)))
            );

          Vector3 one_over_e = new Vector3(1.0f / e.x, 1.0f / e.y, 1.0f / e.z);

          uint u_ex = 0;
          uint u_ey = 0;
          uint u_ez = 0;

          u_ex = System.BitConverter.ToUInt32(System.BitConverter.GetBytes(e.x), 0);
          u_ey = System.BitConverter.ToUInt32(System.BitConverter.GetBytes(e.y), 0);
          u_ez = System.BitConverter.ToUInt32(System.BitConverter.GetBytes(e.z), 0);

          Assert.IsTrue((u_ex & 0b10000000011111111111111111111111) == 0);
          Assert.IsTrue((u_ey & 0b10000000011111111111111111111111) == 0);
          Assert.IsTrue((u_ez & 0b10000000011111111111111111111111) == 0);

          node.e[0] = System.Convert.ToByte(u_ex >> 23);
          node.e[1] = System.Convert.ToByte(u_ey >> 23);
          node.e[2] = System.Convert.ToByte(u_ez >> 23);
          


          int child_count = 0;
          int[] children = {-1, -1, -1, -1, -1, -1, -1, -1};

          get_children(node_index_bvh, ref nodes_bvh, 0, ref child_count, ref children);

          Assert.IsTrue(child_count <= 8);

          order_children(node_index_bvh, ref nodes_bvh, ref children, child_count);

          node.imask = 0;

          node.base_index_child = (uint)cwbvhnode_count;
          node.base_index_triangle = (uint)cwbvhindex_count;

          int node_internal_count = 0;
          int node_triangle_count = 0;

          AABB child_aabb = new AABB();
          for(int i = 0; i < 8; i++) {
            int child_index = children[i];
            if(child_index == -1) continue;

            child_aabb = nodes_bvh[child_index].aabb;

            node.quantized_min_x[i] = (byte)(uint)Mathf.Floor((child_aabb.BBMin.x - node.p.x) * one_over_e.x);
            node.quantized_min_y[i] = (byte)(uint)Mathf.Floor((child_aabb.BBMin.y - node.p.y) * one_over_e.y);
            node.quantized_min_z[i] = (byte)(uint)Mathf.Floor((child_aabb.BBMin.z - node.p.z) * one_over_e.z);

            node.quantized_max_x[i] = (byte)(uint)Mathf.Ceil((child_aabb.BBMax.x - node.p.x) * one_over_e.x);
            node.quantized_max_y[i] = (byte)(uint)Mathf.Ceil((child_aabb.BBMax.y - node.p.y) * one_over_e.y);
            node.quantized_max_z[i] = (byte)(uint)Mathf.Ceil((child_aabb.BBMax.z - node.p.z) * one_over_e.z);
          switch(decisions[child_index * 7].Type) {
            case 0: {
                int triangle_count = count_primitives(child_index, ref nodes_bvh, ref indices_bvh);
                Assert.IsTrue(triangle_count > 0 && triangle_count <= 3);

                for(int j = 0; j < triangle_count; j++) {
                    node.meta[i] |= System.Convert.ToByte(1 << (j + 5));
                }
                node.meta[i] |= System.Convert.ToByte(node_triangle_count);
                node_triangle_count += triangle_count;
                Assert.IsTrue(node_triangle_count <= 24);
                break;
            }
            case 1: {
                node.meta[i] = System.Convert.ToByte((node_internal_count + 24) | 0b00100000);

                node.imask |= System.Convert.ToByte(1 << node_internal_count);
                cwbvhnode_count++;
                node_internal_count++;
                break;
            }
            default: {
                Assert.IsTrue(false);
                break;
            }
          }
          }

          Assert.IsTrue(node.base_index_child + node_internal_count == cwbvhnode_count);
          Assert.IsTrue(node.base_index_triangle + node_triangle_count == cwbvhindex_count);
          BVH8Nodes[node_index_cwbvh] = node;
          for(int i = 0; i < 8; i++) {
            int child_index = children[i];
            if(child_index == -1) continue;

            if(decisions[child_index * 7].Type == 1) {
                collapse(ref nodes_bvh, ref indices_bvh, (int)node.base_index_child + ((byte)node.meta[i] & 31) - 24, child_index);
            }
          }
        }

        float surface_area(ref AABB aabb) {
            Vector3 sizes = aabb.BBMax - aabb.BBMin;
            return 2.0f * ((sizes.x * sizes.y) + (sizes.x * sizes.z) + (sizes.y * sizes.z)); 
        }

        public BVH8Builder(ref BVH2Builder BVH2) {//Bottom Level CWBVH Builder
            int BVH2NodesCount = BVH2.BVH2Nodes.Length;
            int BVH2IndicesCount = BVH2.FinalIndices.Length;
            cost2 = new float[8,8];
            costpreset = new float[8,8];
            cost = new float[BVH2NodesCount * 7];
            decisions = new Decision[BVH2NodesCount * 7];
            BVH8Nodes = new BVHNode8Data[BVH2NodesCount];
            cwbvh_indices = new int[BVH2IndicesCount];

            cwbvhindex_count = 0;
            cwbvhnode_count = 1;
            calculate_cost(0, ref BVH2.BVH2Nodes);

            collapse(ref BVH2.BVH2Nodes, ref BVH2.FinalIndices, 0, 0);
            decisions = null;
        }

        public BVH8Builder(BVH2Builder BVH2) {//Top Level CWBVH Builder
            int BVH2NodesCount = BVH2.BVH2Nodes.Length;
            int BVH2IndicesCount = BVH2.FinalIndices.Length;
            cost2 = new float[8,8];
            costpreset = new float[8,8];
            cost = new float[BVH2NodesCount * 7];
            decisions = new Decision[BVH2NodesCount * 7];
            BVH8Nodes = new BVHNode8Data[BVH2NodesCount];
            cwbvh_indices = new int[BVH2IndicesCount];


            cwbvhindex_count = 0;
            cwbvhnode_count = 1;

            calculate_cost(0, ref BVH2.BVH2Nodes);

            collapse(ref BVH2.BVH2Nodes, ref BVH2.FinalIndices, 0, 0);

            decisions = null;
        }
    }
}