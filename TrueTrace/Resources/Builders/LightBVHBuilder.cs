using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System; 
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace TrueTrace {
    [System.Serializable]
    public unsafe class LightBVHBuilder {
        void OnDestroy() {
            Debug.Log("EEE");
        }
        public NodeBounds ParentBound;


        private float luminance(float r, float g, float b) { return 0.299f * r + 0.587f * g + 0.114f * b; }

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



        public bool IsEmpty(ref LightBounds Cone) {return Cone.w == Vector3.zero;}



        private void RotateRodrigues(ref Vector3 v, float angle, ref Vector3 axis, float axisLenSq) {
            float invLen = 1f / (float)System.Math.Sqrt((double)axisLenSq);
            axis = axis * invLen;

            float cos = (float)System.Math.Cos((double)angle);
            float sin = (float)System.Math.Sin((double)angle);

            float dot = Dot(ref axis, ref v);
            Vector3 cross = Vector3.Cross(axis, v);

            v = v * cos + cross * sin + axis * dot * (1f - cos);
        }
        private float AngleBetween(ref Vector3 v1, ref Vector3 v2) {
            if(Dot(ref v1, ref v2) < 0) return 3.14159f - 2.0f * (float)System.Math.Asin(Length(v1 + v2) * 0.5f);
            else return 2.0f * (float)System.Math.Asin(Length(v2 - v1) * 0.5f);
        }
        public void UnionCone(ref LightBounds A, ref LightBounds B) {
            if(A.w.x == 0 && A.w.y == 0 && A.w.z == 0) {A.w = B.w; A.cosTheta_o = B.cosTheta_o; return;}
            if(B.w.x == 0 && B.w.y == 0 && B.w.z == 0) return;

            float theta_a = (float)System.Math.Acos((double)A.cosTheta_o);
            float theta_b = (float)System.Math.Acos((double)B.cosTheta_o);
            float theta_d = AngleBetween(ref A.w, ref B.w);
            float TempVar = 3.14159f;
            if(theta_d + theta_b < TempVar) TempVar = theta_d + theta_b;
            if(TempVar <= theta_a) return;
            TempVar = 3.14159f;
            if(theta_d + theta_a < TempVar) TempVar = theta_d + theta_a;
            if(TempVar <= theta_b) {A.w = B.w; A.cosTheta_o = B.cosTheta_o; return;}

            float theta_o = (theta_a + theta_d + theta_b) * 0.5f;
            if(theta_o >= 3.14159f) {A.w = Vector3.zero; A.cosTheta_o = -1; return;}

            float theta_r = theta_o - theta_a;
            Vector3 wr = Vector3.Cross(A.w, B.w);
            float AxisLength = Dot(ref wr, ref wr);
            if(AxisLength == 0) {A.w = Vector3.zero; A.cosTheta_o = -1; return;}
            RotateRodrigues(ref A.w, theta_r, ref wr, AxisLength);
            A.cosTheta_o = (float)System.Math.Cos((double)theta_o);
            // if(A.cosTheta_o > 1) A.cosTheta_o = 1;
            // else if(A.cosTheta_o < -1) A.cosTheta_o = -1;
        }



    

        private void Union(ref LightBounds A, ref LightBounds B) {
            if(A.phi == 0) {A = B; return;}
            if(B.phi == 0) return;
            UnionCone(ref A, ref B);
            if(B.cosTheta_e < A.cosTheta_e) A.cosTheta_e = B.cosTheta_e;
            A.b.Extend(ref B.b);
            A.phi += B.phi;
            A.LightCount += B.LightCount;
        }

        float surface_area(ref AABB aabb) {
            Vector3 d = new Vector3(aabb.BBMax.x - aabb.BBMin.x, aabb.BBMax.y - aabb.BBMin.y, aabb.BBMax.z - aabb.BBMin.z);
            return (d.x + d.y) * d.z + d.x * d.y; 
        }

        private float EvaluateCost(ref LightBounds b, float Kr) {
            float theta_o = (float)System.Math.Acos(b.cosTheta_o);
            float theta_e = (float)System.Math.Acos(b.cosTheta_e);
            float theta_w = 3.14159f;
            if(theta_o + theta_e < theta_w) theta_w = theta_o + theta_e;
            float sinTheta_o = (float)System.Math.Sqrt(1.0f - b.cosTheta_o * b.cosTheta_o);
            float M_omega = 2.0f * 3.14159f * (1.0f - b.cosTheta_o) +
                            3.14159f / 2.0f *
                                (2.0f * theta_w * sinTheta_o -(float)System.Math.Cos(theta_o - 2.0f * theta_w) -
                                 2.0f * theta_o * sinTheta_o + b.cosTheta_o);

            
            int LightCoun = b.LightCount;
            if(LightCoun < 1) LightCoun = 1;
            return b.phi * M_omega * Kr * surface_area(ref b.b) / (float)LightCoun;
        }

        // private float InlinedUnion()

        // {
        //     if(A.phi == 0) {A = B;}
        //     else if(B.phi != 0) {
        //         if(A.w.x == 0 && A.w.y == 0 && A.w.z == 0) {A.w = B.w; A.cosTheta_o = B.cosTheta_o;}
        //         else if(!(B.w.x == 0 && B.w.y == 0 && B.w.z == 0)) {
        //             float theta_a = (float)System.Math.Acos((double)A.cosTheta_o);
        //             float theta_b = (float)System.Math.Acos((double)B.cosTheta_o);
        //             float theta_d;
        //             if(Dot(ref A.w, ref B.w) < 0) theta_d = 3.14159f - 2.0f * (float)System.Math.Asin(Length(A.w + B.w) * 0.5f);
        //             else theta_d = 2.0f * (float)System.Math.Asin(Length(B.w - A.w) * 0.5f);

        //             float TempVar = 3.14159f;
        //             if(theta_d + theta_b < TempVar) TempVar = theta_d + theta_b;
        //             if(TempVar > theta_a) {
        //                 TempVar = 3.14159f;
        //                 if(theta_d + theta_a < TempVar) TempVar = theta_d + theta_a;
        //                 if(TempVar <= theta_b) {A.w = B.w; A.cosTheta_o = B.cosTheta_o;}
        //                 else {
        //                     float theta_o = (theta_a + theta_d + theta_b) * 0.5f;
        //                     if(theta_o >= 3.14159f) {A.w = Vector3.zero; A.cosTheta_o = -1;}
        //                     else {
        //                         float theta_r = theta_o - theta_a;
        //                         Vector3 wr = Vector3.Cross(A.w, B.w);
        //                         float AxisLength = Dot(ref wr, ref wr);
        //                         if(AxisLength == 0) {A.w = Vector3.zero; A.cosTheta_o = -1;}
        //                         else {
        //                             float invLen = 1f / (float)System.Math.Sqrt((double)AxisLength);
        //                             wr = wr * invLen;

        //                             float cos = (float)System.Math.Cos((double)theta_r);
        //                             float sin = (float)System.Math.Sin((double)theta_r);

        //                             float dot = Dot(ref wr, ref A.w);
        //                             Vector3 cross = Vector3.Cross(wr, A.w);

        //                             A.w = A.w * cos + cross * sin + axis * dot * (1f - cos);
        //                             A.cosTheta_o = (float)System.Math.Cos((double)theta_o);
        //                         }
        //                     }
        //                 }
        //             }
        //         }

        //         if(B.cosTheta_e < A.cosTheta_e) A.cosTheta_e = B.cosTheta_e;
        //         A.b.Extend(ref B.b);
        //         A.phi += B.phi;
        //         A.LightCount += B.LightCount;
        //     }

        // }
        private float Dat = Mathf.PI;
        float theta_o;
        private float InlinedUnionCost(ref LightBounds A, ref LightBounds B, float Kr)

        {
            float Cos = Dat;
            if(A.phi == 0) {A = B; Cos = (float)System.Math.Acos((double)A.cosTheta_o);}
            else if(B.phi != 0) {
                if(A.w.x == 0 && A.w.y == 0 && A.w.z == 0) {A.w = B.w; A.cosTheta_o = B.cosTheta_o; Cos = (float)System.Math.Acos((double)A.cosTheta_o);}
                else if(B.w.x == 0 && B.w.y == 0 && B.w.z == 0) Cos = (float)System.Math.Acos((double)A.cosTheta_o);
                else {
                    float theta_a = (float)System.Math.Acos((double)A.cosTheta_o);
                    float theta_b = (float)System.Math.Acos((double)B.cosTheta_o);
                    float theta_d;
                    if(Dot(ref A.w, ref B.w) < 0) theta_d = 3.14159f - 2.0f * (float)System.Math.Asin(Length(A.w + B.w) * 0.5f);
                    else theta_d = 2.0f * (float)System.Math.Asin(Length(B.w - A.w) * 0.5f);

                    float TempVar = 3.14159f;
                    if(theta_d + theta_b < TempVar) TempVar = theta_d + theta_b;
                    if(TempVar <= theta_a) Cos = theta_a;
                    else {
                        TempVar = 3.14159f;
                        if(theta_d + theta_a < TempVar) TempVar = theta_d + theta_a;
                        if(TempVar <= theta_b) {A.w = B.w; A.cosTheta_o = B.cosTheta_o; Cos = theta_b;}
                        else {
                            theta_o = (theta_a + theta_d + theta_b) * 0.5f;
                            if(theta_o >= 3.14159f) {A.w = Vector3.zero; A.cosTheta_o = -1;}
                            else {
                                float theta_r = theta_o - theta_a;
                                Vector3 wr = Vector3.Cross(A.w, B.w);
                                float AxisLength = Dot(ref wr, ref wr);
                                if(AxisLength == 0) {A.w = Vector3.zero; A.cosTheta_o = -1;}
                                else {
                                    float invLen = 1f / (float)System.Math.Sqrt((double)AxisLength);
                                    wr = wr * invLen;

                                    float cos = (float)System.Math.Cos((double)theta_r);
                                    float sin = (float)System.Math.Sin((double)theta_r);

                                    float dot = Dot(ref wr, ref A.w);
                                    Vector3 cross = Vector3.Cross(wr, A.w);

                                    A.w = A.w * cos + cross * sin + wr * dot * (1f - cos);
                                    Cos = theta_o;
                                    A.cosTheta_o = (float)System.Math.Cos((double)theta_o);
                                }
                            }
                        }
                    }
                }

                if(B.cosTheta_e < A.cosTheta_e) A.cosTheta_e = B.cosTheta_e;
                A.b.Extend(ref B.b);
                A.phi += B.phi;
                A.LightCount += B.LightCount;
            }

            theta_o = Cos;
            float theta_e = (float)System.Math.Acos(A.cosTheta_e);
            float theta_w = 3.14159f;
            if(theta_o + theta_e < theta_w) theta_w = theta_o + theta_e;
            float sinTheta_o = (float)System.Math.Sqrt(1.0f - A.cosTheta_o * A.cosTheta_o);
            float M_omega = 2.0f * 3.14159f * (1.0f - A.cosTheta_o) +
                            3.14159f / 2.0f *
                                (2.0f * theta_w * sinTheta_o -(float)System.Math.Cos(theta_o - 2.0f * theta_w) -
                                 2.0f * theta_o * sinTheta_o + A.cosTheta_o);

            
            int LightCoun = A.LightCount;
            if(LightCoun < 1) LightCoun = 1;
            return A.phi * M_omega * Kr * surface_area(ref A.b) / (float)LightCoun;

        }


        private LightBounds* LightTris;
        private NodeBounds* nodes2;
#if DontUseSGTree
        public CompactLightBVHData[] nodes;
#else
        private CompactLightBVHData[] nodes;
        public GaussianTreeNode[] SGTree;
#endif
        private int* DimensionedIndices;
        public int PrimCount;
        private float* SAH;
        private bool* indices_going_left;
        private int* temp;
        public int[] FinalIndices;

        public NativeArray<LightBounds> LightTrisArray;
        public NativeArray<NodeBounds> nodes2Array;
        public NativeArray<int> DimensionedIndicesArray;
        public NativeArray<int> tempArray;
        public NativeArray<bool> indices_going_left_array;
        public NativeArray<float> SAHArray;

        public struct ObjectSplit {
            public int index;
            public float cost;
            public int dimension;
            public LightBounds aabb_left;
            public LightBounds aabb_right;
        }
        private ObjectSplit split = new ObjectSplit();


        LightBounds aabb_left;
        LightBounds aabb_right;
        void partition_sah(int first_index, int index_count, AABB parentBounds) {
            split.cost = float.MaxValue;
            split.index = -1;
            split.dimension = -1;

            aabb_left.Clear();
            aabb_right.Clear();

            int Offset;
            Vector3 Diagonal = new Vector3(parentBounds.BBMax.x - parentBounds.BBMin.x, parentBounds.BBMax.y - parentBounds.BBMin.y, parentBounds.BBMax.z - parentBounds.BBMin.z);
            float Kr1 = Diagonal.x;
            if(Kr1 < Diagonal.y) Kr1 = Diagonal.y;
            if(Kr1 < Diagonal.z) Kr1 = Diagonal.z;
            for(int dimension = 0; dimension < 3; dimension++) {
                float Kr = Kr1 / Diagonal[dimension];
                Offset = PrimCount * dimension + first_index;
                aabb_left = LightTris[DimensionedIndices[Offset]];
                SAH[1] = EvaluateCost(ref aabb_left, Kr);
                for(int i = 2; i < index_count; i++) {
                    // Union(ref aabb_left, ref LightTris[DimensionedIndices[Offset + i - 1]]);

                    SAH[i] = InlinedUnionCost(ref aabb_left, ref LightTris[DimensionedIndices[Offset + i - 1]], Kr) * (float)i;
                    // SAH[i] = EvaluateCost(ref aabb_left, Kr) * (float)i;
                }

                {
                    aabb_right = LightTris[DimensionedIndices[Offset + index_count - 1]];
                    float cost = SAH[index_count - 1] + EvaluateCost(ref aabb_right, Kr) * (float)(index_count - (index_count - 1));

                    if(cost != 0)
                    if(cost <= split.cost) {
                        split.cost = cost;
                        split.index = first_index + index_count - 1;
                        split.dimension = dimension;
                        split.aabb_right = aabb_right;
                    }
                }


                for(int i = index_count - 2; i > 0; i--) {
                    // Union(ref aabb_right, ref LightTris[DimensionedIndices[Offset + i]]);

                    // float LeftCost = EvaluateCost(ref aabb_right, Kr) * (float)(index_count - i);
                    float LeftCost = InlinedUnionCost(ref aabb_right, ref LightTris[DimensionedIndices[Offset + i]], Kr) * (float)(index_count - i);
                    if(LeftCost >= split.cost) break;
                    float cost = SAH[i] + LeftCost;

                    if(cost != 0)
                    if(cost <= split.cost) {
                        split.cost = cost;
                        split.index = first_index + i;
                        split.dimension = dimension;
                        split.aabb_right = aabb_right;
                    }
                }
            }
            if(split.cost == float.MaxValue) split.dimension = 0;
            Offset = split.dimension * PrimCount;
            if(split.cost == float.MaxValue) {
                split.index = first_index + (index_count) / 2;
                for(int i = split.index; i < index_count + first_index; i++) {
                    Union(ref split.aabb_right, ref LightTris[DimensionedIndices[Offset + i]]);
                }
            }
            split.aabb_left = LightTris[DimensionedIndices[Offset + first_index]];
            for(int i = first_index + 1; i < split.index; i++) Union(ref split.aabb_left, ref LightTris[DimensionedIndices[Offset + i]]);
        }
        public int MaxDepth;
        void BuildRecursive(int nodesi, ref int node_index, int first_index, int index_count, int Depth) {
            if(index_count == 1) {
                nodes2[nodesi].left = first_index;
                nodes2[nodesi].isLeaf = 1;
                MaxDepth = System.Math.Max(Depth, MaxDepth);
                return;
            }
            partition_sah(first_index, index_count, nodes2[nodesi].aabb.b);
            int Offset = split.dimension * PrimCount;
            int IndexEnd = first_index + index_count;
            for(int i = first_index; i < IndexEnd; i++) indices_going_left[DimensionedIndices[Offset + i]] = i < split.index;

            for(int dim = 0; dim < 3; dim++) {
                if(dim == split.dimension) continue;

                int index;
                int left = 0;
                int right = split.index - first_index;
                Offset = dim * PrimCount;
                for(int i = first_index; i < IndexEnd; i++) {
                    index = DimensionedIndices[Offset + i];
                    temp[indices_going_left[index] ? (left++) : (right++)] = index;
                }
          
                NativeArray<int>.Copy(tempArray, 0, DimensionedIndicesArray, Offset + first_index, index_count);
            }
            nodes2[nodesi].left = node_index;

            nodes2[nodes2[nodesi].left].aabb = split.aabb_left;
            nodes2[nodes2[nodesi].left + 1].aabb = split.aabb_right;
            node_index += 2;
            int Index = split.index;
            BuildRecursive(nodes2[nodesi].left, ref node_index,first_index,Index - first_index, Depth + 1);
            BuildRecursive(nodes2[nodesi].left + 1, ref node_index,first_index + Index - first_index,first_index + index_count - Index, Depth + 1);
        }
        public ComputeBuffer[] WorkingSet;

  
        private float AreaOfTriangle(Vector3 pt1, Vector3 pt2, Vector3 pt3)
        {
            float a = Distance(pt1, pt2);
            float b = Distance(pt2, pt3);
            float c = Distance(pt3, pt1);
            float s = (a + b + c) / 2.0f;
            return (float)System.Math.Sqrt((double)(s * (s - a) * (s - b) * (s - c)));
        }

        public List<int>[] MainSet;
        public List<int>[] Set;
        void Refit(int Depth, int CurrentIndex) {
            if(nodes2[CurrentIndex].aabb.cosTheta_e == 0) return;
            MainSet[Depth].Add(CurrentIndex);
            if(nodes2[CurrentIndex].isLeaf == 1) return;
            Refit(Depth + 1, nodes2[CurrentIndex].left);
            Refit(Depth + 1, nodes2[CurrentIndex].left + 1);
        }

        private void Refit2(int Depth, int CurrentIndex) {
            if((2.0f * ((float)(nodes[CurrentIndex].cosTheta_oe >> 16) / 32767.0f) - 1.0f) == 0) return;
            Set[Depth].Add(CurrentIndex);
            if(nodes[CurrentIndex].left < 0) return;
            Refit2(Depth + 1, nodes[CurrentIndex].left);
            Refit2(Depth + 1, nodes[CurrentIndex].left + 1);
        }

        float expm1_over_x(float x)
        {
            float u = Mathf.Exp(x);

            if (u == 1.0f)
            {
                return 1.0f;
            }

            float y = u - 1.0f;

            if (Mathf.Abs(x) < 1.0f)
            {
                return y / Mathf.Log(u);
            }

            return y / x;
        }

        float SGIntegral(float sharpness)
        {
            return 4.0f * Mathf.PI * expm1_over_x(-2.0f * sharpness);
        }

        // Estimation of vMF sharpness (i.e., SG sharpness) from the average of directions in R^3.
        // [Banerjee et al. 2005 "Clustering on the Unit Hypersphere using von Mises-Fisher Distributions"]
        float VMFAxisLengthToSharpness(float axisLength)
        {
            return axisLength * (3.0f - axisLength * axisLength) / (1.0f - axisLength * axisLength);
        }

        // Inverse of VMFAxisLengthToSharpness.
        float VMFSharpnessToAxisLength(float sharpness)
        {
            // Solve x^3 - sx^2 - 3x + s = 0, where s = sharpness.
            // For x in [0, 1] and s in [0, infty), this equation has only a single solution.
            // [Xu and Wang 2015 "Realtime Rendering Glossy to Glossy Reflections in Screen Space"]
            // We solve this cubic equation in a numerically stable manner.
            // [Peters, C. 2016 "How to solve a cubic equation, revisited" https://momentsingraphics.de/CubicRoots.html]
            float a = sharpness / 3.0f;
            float b = a * a * a;
            float c = Mathf.Sqrt(1.0f + 3.0f * (a * a) * (1.0f + a * a));
            float theta = Mathf.Atan2(c, b) / 3.0f;
            float d = -2.0f * Mathf.Sin(Mathf.PI / 6.0f - theta); // = sin(theta) * sqrt(3) - cos(theta).
            return (sharpness > 33554432.0f) ? 1.0f : Mathf.Sqrt(1.0f + a * a) * d + a;
        }



        public unsafe LightBVHBuilder(List<LightTriData> Tris, List<Vector3> Norms, float phi, List<float> LuminanceWeights) {//need to make sure incomming is transformed to world space already
            PrimCount = Tris.Count;          
            MaxDepth = 0;
            DimensionedIndicesArray = new NativeArray<int>(PrimCount * 3, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            
            nodes2Array = new NativeArray<NodeBounds>(PrimCount * 2, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            nodes2 = (NodeBounds*)NativeArrayUnsafeUtility.GetUnsafePtr(nodes2Array);
            
            LightTrisArray = new NativeArray<LightBounds>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            LightTris = (LightBounds*)NativeArrayUnsafeUtility.GetUnsafePtr(LightTrisArray);

            SAHArray = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            SAH = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(SAHArray);

            DimensionedIndices = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(DimensionedIndicesArray);

            FinalIndices = new int[PrimCount];

            AABB TriAABB = new AABB();
            Vector3 Scaler = new Vector3(0.0001f,0.0001f,0.0001f);
            float Precomputed = (float)System.Math.Cos(3.14159f / 2.0f);
            for(int i = 0; i < PrimCount; i++) {
                TriAABB.init();
                TriAABB.Extend(Tris[i].pos0);
                TriAABB.Extend(Tris[i].pos0 + Tris[i].posedge1);
                TriAABB.Extend(Tris[i].pos0 + Tris[i].posedge2);
                TriAABB.Validate(Scaler);
                DirectionCone tricone = new DirectionCone(-Norms[i], 1);
                float ThisPhi = AreaOfTriangle(Tris[i].pos0, Tris[i].pos0 + Tris[i].posedge1, Tris[i].pos0 + Tris[i].posedge2) * LuminanceWeights[i];
                LightBounds TempBound = new LightBounds(TriAABB, tricone.W, ThisPhi, tricone.cosTheta,Precomputed, 1, 0);
                LightTris[i] = TempBound;
                FinalIndices[i] = i;
                SAH[i] = (TriAABB.BBMax.x + TriAABB.BBMin.x) * 0.5f;
                Union(ref nodes2[0].aabb, ref TempBound);
            }

            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, 0, PrimCount);
            for(int i = 0; i < PrimCount; i++) {FinalIndices[i] = i; SAH[i] = (LightTris[i].b.BBMax.y + LightTris[i].b.BBMin.y) * 0.5f;}
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount, PrimCount);
            for(int i = 0; i < PrimCount; i++) {FinalIndices[i] = i; SAH[i] = (LightTris[i].b.BBMax.z + LightTris[i].b.BBMin.z) * 0.5f;}
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount * 2, PrimCount);
            CommonFunctions.DeepClean(ref FinalIndices);


            aabb_left = new LightBounds();
            aabb_right = new LightBounds();
            tempArray = new NativeArray<int>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            temp = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(tempArray);
            indices_going_left_array = new NativeArray<bool>(PrimCount, Unity.Collections.Allocator.TempJob, NativeArrayOptions.ClearMemory);
            indices_going_left = (bool*)NativeArrayUnsafeUtility.GetUnsafePtr(indices_going_left_array);

            int nodeIndex = 2;
            BuildRecursive(0, ref nodeIndex,0,PrimCount, 1);
            if(indices_going_left_array.IsCreated) indices_going_left_array.Dispose();
            if(tempArray.IsCreated) tempArray.Dispose();
            if(SAHArray.IsCreated) SAHArray.Dispose();
            if(LightTrisArray.IsCreated) LightTrisArray.Dispose();

            for(int i = 0; i < PrimCount * 2; i++) {
                if(nodes2[i].isLeaf == 1) {
                    nodes2[i].left = DimensionedIndices[nodes2[i].left];
                }
            }
            DimensionedIndicesArray.Dispose();
            nodes = new CompactLightBVHData[PrimCount * 2];
            for(int i = 0; i < PrimCount * 2; i++) {
                CompactLightBVHData TempNode = new CompactLightBVHData();
                TempNode.BBMax = nodes2[i].aabb.b.BBMax;
                TempNode.BBMin = nodes2[i].aabb.b.BBMin;
                TempNode.w = CommonFunctions.PackOctahedral(nodes2[i].aabb.w);
                TempNode.phi = nodes2[i].aabb.phi;
                TempNode.cosTheta_oe = ((uint)Mathf.Floor(32767.0f * ((nodes2[i].aabb.cosTheta_o + 1.0f) / 2.0f))) | ((uint)Mathf.Floor(32767.0f * ((nodes2[i].aabb.cosTheta_e + 1.0f) / 2.0f)) << 16);
                if(nodes2[i].isLeaf == 1) {
                    TempNode.left = (-nodes2[i].left) - 1;
                } else {
                    TempNode.left = nodes2[i].left;
                }
                nodes[i] = TempNode;
            }
            ParentBound = nodes2[0];
            nodes2Array.Dispose();
#if !DontUseSGTree
            {
                SGTree = new GaussianTreeNode[nodes.Length];
                Set = new List<int>[MaxDepth];
                for(int i = 0; i < MaxDepth; i++) Set[i] = new List<int>();
                Refit2(0, 0);
                GaussianTreeNode TempNode = new GaussianTreeNode();
                for(int i = MaxDepth - 1; i >= 0; i--) {
                    int SetCount = Set[i].Count;
                    for(int j = 0; j < SetCount; j++) {
                        int WriteIndex = Set[i][j];
                        CompactLightBVHData LBVHNode = nodes[WriteIndex];
                        Vector3 V;
                        Vector3 mean;
                        float variance;
                        float intensity;
                        float radius;
                        if(LBVHNode.left < 0) {
                            LightTriData ThisLight = Tris[-(LBVHNode.left+1)];

                            float area = AreaOfTriangle(ThisLight.pos0, ThisLight.pos0 + ThisLight.posedge1, ThisLight.pos0 + ThisLight.posedge2);

                            intensity = ThisLight.SourceEnergy * area;
                            V = 0.5f * -Norms[-(LBVHNode.left+1)];//(Vector3.Cross(ThisLight.posedge1.normalized, ThisLight.posedge2.normalized).normalized);
                            mean = (ThisLight.pos0 + (ThisLight.pos0 + ThisLight.posedge1) + (ThisLight.pos0 + ThisLight.posedge2)) / 3.0f;
                            variance = (Vector3.Dot(ThisLight.posedge1, ThisLight.posedge1) + Vector3.Dot(ThisLight.posedge2, ThisLight.posedge2) - Vector3.Dot(ThisLight.posedge1, ThisLight.posedge2)) / 18.0f;
                            radius = Mathf.Max(Mathf.Max(Distance(mean, ThisLight.pos0), Distance(mean, ThisLight.pos0 + ThisLight.posedge1)), Distance(mean, ThisLight.pos0 + ThisLight.posedge2));
                        } else {
                            GaussianTreeNode LeftNode = SGTree[nodes[WriteIndex].left];    
                            GaussianTreeNode RightNode = SGTree[nodes[WriteIndex].left + 1];

                            float phi_left = LeftNode.intensity;    
                            float phi_right = RightNode.intensity;    
                            float w_left = phi_left / (phi_left + phi_right);
                            float w_right = phi_right / (phi_left + phi_right);
                            
                            V = w_left * LeftNode.axis * VMFSharpnessToAxisLength(LeftNode.sharpness) + w_right * RightNode.axis * VMFSharpnessToAxisLength(RightNode.sharpness);

                            mean = w_left * LeftNode.S.Center + w_right * RightNode.S.Center;
                            variance = w_left * LeftNode.variance + w_right * RightNode.variance + w_left * w_right * Vector3.Dot(LeftNode.S.Center - RightNode.S.Center, LeftNode.S.Center - RightNode.S.Center);

                            intensity = LeftNode.intensity + RightNode.intensity;
                            radius = Mathf.Max(Distance(mean, LeftNode.S.Center) + LeftNode.S.Radius, Distance(mean, RightNode.S.Center) + RightNode.S.Radius);
                        }
                        float AxisLength = Distance(Vector3.zero, V);
                        if(AxisLength == 0) V = new Vector3(0,1,0);
                        else V /= AxisLength;
                        TempNode.sharpness = Mathf.Min(VMFAxisLengthToSharpness(Mathf.Clamp(AxisLength, 0.0f, 1.0f)), 2199023255552.0f);// ((3.0f * Distance(Vector3.zero, V) - Mathf.Pow(Distance(Vector3.zero, V), 3))) / (1.0f - Mathf.Pow(Distance(Vector3.zero, V), 2));
                        TempNode.axis = V;
                        TempNode.S.Center = mean;
                        TempNode.variance = variance;
                        TempNode.intensity = intensity;
                        TempNode.S.Radius = radius;

                        TempNode.left = LBVHNode.left;
                        SGTree[WriteIndex] = TempNode;
                    }
                }
            }

            CommonFunctions.DeepClean(ref nodes);
#endif
        }
        public void Dispose() {
            ClearAll();
            
            if(WorkingSet != null) {
                int SetLength = WorkingSet.Length;
                for(int i = 0; i < SetLength; i++) {
                    if(WorkingSet[i] != null) WorkingSet[i].Release();
                }
            }
        }


        public LightBVHBuilder(LightBounds[] Tris,ref GaussianTreeNode[] SGTree, LightBVHTransform[] LightBVHTransforms, GaussianTreeNode[] SGTreeNodes) {//need to make sure incomming is transformed to world space already
            PrimCount = Tris.Length;          
            MaxDepth = 0;
            
            LightTrisArray = new NativeArray<LightBounds>(Tris, Unity.Collections.Allocator.Persistent);
            LightTris = (LightBounds*)NativeArrayUnsafeUtility.GetUnsafePtr(LightTrisArray);
            SAHArray = new NativeArray<float>(PrimCount, Unity.Collections.Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            DimensionedIndicesArray = new NativeArray<int>(PrimCount * 3, Unity.Collections.Allocator.Persistent, NativeArrayOptions.UninitializedMemory);


            nodes2Array = new NativeArray<NodeBounds>(PrimCount * 2, Unity.Collections.Allocator.Persistent, NativeArrayOptions.ClearMemory);
            nodes2 = (NodeBounds*)NativeArrayUnsafeUtility.GetUnsafePtr(nodes2Array);
            DimensionedIndices = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(DimensionedIndicesArray);
            SAH = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(SAHArray);


            FinalIndices = new int[PrimCount];

            for(int i = 0; i < PrimCount; i++) {
                FinalIndices[i] = i;
                SAH[i] = (LightTris[i].b.BBMax.x + LightTris[i].b.BBMin.x) * 0.5f;
                Union(ref nodes2[0].aabb, ref LightTris[i]);
            }

            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, 0, PrimCount);
            for(int i = 0; i < PrimCount; i++) {FinalIndices[i] = i; SAH[i] = (LightTris[i].b.BBMax.y + LightTris[i].b.BBMin.y) * 0.5f;}
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount, PrimCount);
            for(int i = 0; i < PrimCount; i++) {FinalIndices[i] = i; SAH[i] = (LightTris[i].b.BBMax.z + LightTris[i].b.BBMin.z) * 0.5f;}
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount * 2, PrimCount);


            aabb_left = new LightBounds();
            aabb_right = new LightBounds();

            indices_going_left_array = new NativeArray<bool>(PrimCount, Unity.Collections.Allocator.Persistent, NativeArrayOptions.ClearMemory);
            indices_going_left = (bool*)NativeArrayUnsafeUtility.GetUnsafePtr(indices_going_left_array);
            tempArray = new NativeArray<int>(PrimCount, Unity.Collections.Allocator.Persistent, NativeArrayOptions.ClearMemory);
            temp = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(tempArray);

            int nodeIndex = 2;
            BuildRecursive(0, ref nodeIndex,0,PrimCount, 1);

            for(int i = 0; i < PrimCount * 2; i++) {
                if(nodes2[i].isLeaf == 1) {
                    nodes2[i].left = DimensionedIndices[nodes2[i].left];
                }
            }
            MainSet = new List<int>[MaxDepth];
            for(int i = 0; i < MaxDepth; i++) MainSet[i] = new List<int>();
            Refit(0, 0);
            nodes = new CompactLightBVHData[PrimCount * 2];
            for(int i = 0; i < PrimCount * 2; i++) {
                CompactLightBVHData TempNode = new CompactLightBVHData();
                TempNode.BBMax = nodes2[i].aabb.b.BBMax;
                TempNode.BBMin = nodes2[i].aabb.b.BBMin;
                TempNode.w = CommonFunctions.PackOctahedral(nodes2[i].aabb.w);
                TempNode.phi = nodes2[i].aabb.phi;
                TempNode.cosTheta_oe = ((uint)Mathf.Floor(32767.0f * ((nodes2[i].aabb.cosTheta_o + 1.0f) / 2.0f))) | ((uint)Mathf.Floor(32767.0f * ((nodes2[i].aabb.cosTheta_e + 1.0f) / 2.0f)) << 16);
                if(nodes2[i].isLeaf == 1) {
                    TempNode.left = (-nodes2[i].left) - 1;
                } else {
                    TempNode.left = nodes2[i].left;
                }
                nodes[i] = TempNode;
            }

#if !DontUseSGTree
            {
                Set = new List<int>[MaxDepth];
                for(int i = 0; i < MaxDepth; i++) Set[i] = new List<int>();
                Refit2(0, 0);
                GaussianTreeNode TempNode = new GaussianTreeNode();
                for(int i = MaxDepth - 1; i >= 0; i--) {
                    int SetCount = Set[i].Count;
                    for(int j = 0; j < SetCount; j++) {
                        int WriteIndex = Set[i][j];
                        CompactLightBVHData LBVHNode = nodes[WriteIndex];
                        Vector3 V;
                        Vector3 mean;
                        float variance;
                        float intensity;
                        float radius;
                        if(LBVHNode.left < 0) {
                            TempNode = SGTreeNodes[-(LBVHNode.left+1)];
                            Vector3 ExtendedCenter = CommonFunctions.ToVector3(LightBVHTransforms[-(LBVHNode.left+1)].Transform * CommonFunctions.ToVector4(TempNode.S.Center + new Vector3(TempNode.S.Radius, 0, 0), 1));
                            Vector3 center = CommonFunctions.ToVector3(LightBVHTransforms[-(LBVHNode.left+1)].Transform * CommonFunctions.ToVector4(TempNode.S.Center, 1));
                            Vector3 Axis = CommonFunctions.ToVector3(LightBVHTransforms[-(LBVHNode.left+1)].Transform * CommonFunctions.ToVector4(TempNode.axis, 0));
                            float Scale = Distance(center, ExtendedCenter) / TempNode.S.Radius;
                            TempNode.sharpness = Mathf.Min(VMFAxisLengthToSharpness(Mathf.Clamp(VMFSharpnessToAxisLength(TempNode.sharpness), 0.0f, 1.0f)), 2199023255552.0f);// ((3.0f * Distance(Vector3.zero, V) - Mathf.Pow(Distance(Vector3.zero, V), 3))) / (1.0f - Mathf.Pow(Distance(Vector3.zero, V), 2));
                            TempNode.axis = Axis;
                            TempNode.S.Center = center;
                            TempNode.variance *= Scale;
                            TempNode.S.Radius *= Scale;
                            TempNode.intensity *= Scale * Scale;
                        } else {
                            GaussianTreeNode LeftNode = SGTree[nodes[WriteIndex].left];    
                            GaussianTreeNode RightNode = SGTree[nodes[WriteIndex].left + 1];

                            float phi_left = LeftNode.intensity;    
                            float phi_right = RightNode.intensity;    
                            float w_left = phi_left / (phi_left + phi_right);
                            float w_right = phi_right / (phi_left + phi_right);
                            
                            V = w_left * LeftNode.axis * VMFSharpnessToAxisLength(LeftNode.sharpness) + w_right * RightNode.axis * VMFSharpnessToAxisLength(RightNode.sharpness);
                            // V = w_left * LeftNode.axis + w_right * RightNode.axis;//may be wrong, paper uses BAR_V(BAR_axis here), not just normalized V/axis

                            mean = w_left * LeftNode.S.Center + w_right * RightNode.S.Center;
                            variance = w_left * LeftNode.variance + w_right * RightNode.variance + w_left * w_right * Vector3.Dot(LeftNode.S.Center - RightNode.S.Center, LeftNode.S.Center - RightNode.S.Center);

                            intensity = LeftNode.intensity + RightNode.intensity;
                            radius = Mathf.Max(Distance(mean, LeftNode.S.Center) + LeftNode.S.Radius, Distance(mean, RightNode.S.Center) + RightNode.S.Radius);

                            float AxisLength = Distance(Vector3.zero, V);
                            if(AxisLength == 0) V = new Vector3(0,1,0);
                            else V /= AxisLength;
                            TempNode.sharpness = Mathf.Min(VMFAxisLengthToSharpness(Mathf.Clamp(AxisLength, 0.0f, 1.0f)), 2199023255552.0f);// ((3.0f * Distance(Vector3.zero, V) - Mathf.Pow(Distance(Vector3.zero, V), 3))) / (1.0f - Mathf.Pow(Distance(Vector3.zero, V), 2));

                            TempNode.axis = V;
                            TempNode.S.Center = mean;
                            TempNode.variance = variance;
                            TempNode.intensity = intensity;
                            TempNode.S.Radius = radius;
                        }

                        TempNode.left = LBVHNode.left;
                        SGTree[WriteIndex] = TempNode;
                    }
                }
            }
#endif

        }

        public NodeBounds ZeroBound = new NodeBounds();
        public CompactLightBVHData ZeroBound2 = new CompactLightBVHData();


   public void NoAllocRebuild(LightBounds[] Tris,ref GaussianTreeNode[] SGTree, LightBVHTransform[] LightBVHTransforms, GaussianTreeNode[] SGTreeNodes) {//need to make sure incomming is transformed to world space already
            PrimCount = Tris.Length;          
            MaxDepth = 0;
            NativeArray<LightBounds>.Copy(Tris, 0, LightTrisArray, 0, PrimCount);
            LightTris = (LightBounds*)NativeArrayUnsafeUtility.GetUnsafePtr(LightTrisArray);


            nodes2 = (NodeBounds*)NativeArrayUnsafeUtility.GetUnsafePtr(nodes2Array);
            DimensionedIndices = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(DimensionedIndicesArray);
            SAH = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(SAHArray);



            for(int i = 0; i < PrimCount; i++) {
                indices_going_left[i] = false;
                temp[i] = 0;
                nodes[i] = ZeroBound2;
                nodes[i + PrimCount] = ZeroBound2;
                nodes2[i] = ZeroBound;
                nodes2[i + PrimCount] = ZeroBound;
                FinalIndices[i] = i;
                SAH[i] = (LightTris[i].b.BBMax.x + LightTris[i].b.BBMin.x) * 0.5f;
                Union(ref nodes2[0].aabb, ref LightTris[i]);
            }

            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, 0, PrimCount);
            for(int i = 0; i < PrimCount; i++) {FinalIndices[i] = i; SAH[i] = (LightTris[i].b.BBMax.y + LightTris[i].b.BBMin.y) * 0.5f;}
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount, PrimCount);
            for(int i = 0; i < PrimCount; i++) {FinalIndices[i] = i; SAH[i] = (LightTris[i].b.BBMax.z + LightTris[i].b.BBMin.z) * 0.5f;}
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = SAH[s1] - SAH[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            NativeArray<int>.Copy(FinalIndices, 0, DimensionedIndicesArray, PrimCount * 2, PrimCount);


            aabb_left = new LightBounds();
            aabb_right = new LightBounds();

            indices_going_left = (bool*)NativeArrayUnsafeUtility.GetUnsafePtr(indices_going_left_array);
            temp = (int*)NativeArrayUnsafeUtility.GetUnsafePtr(tempArray);

            int nodeIndex = 2;
            BuildRecursive(0, ref nodeIndex,0,PrimCount, 1);

            for(int i = 0; i < PrimCount * 2; i++) {
                if(nodes2[i].isLeaf == 1) {
                    nodes2[i].left = DimensionedIndices[nodes2[i].left];
                }
            }
            MainSet = new List<int>[MaxDepth];
            for(int i = 0; i < MaxDepth; i++) MainSet[i] = new List<int>();
            Refit(0, 0);
            for(int i = 0; i < PrimCount * 2; i++) {
                CompactLightBVHData TempNode = new CompactLightBVHData();
                TempNode.BBMax = nodes2[i].aabb.b.BBMax;
                TempNode.BBMin = nodes2[i].aabb.b.BBMin;
                TempNode.w = CommonFunctions.PackOctahedral(nodes2[i].aabb.w);
                TempNode.phi = nodes2[i].aabb.phi;
                TempNode.cosTheta_oe = ((uint)Mathf.Floor(32767.0f * ((nodes2[i].aabb.cosTheta_o + 1.0f) / 2.0f))) | ((uint)Mathf.Floor(32767.0f * ((nodes2[i].aabb.cosTheta_e + 1.0f) / 2.0f)) << 16);
                if(nodes2[i].isLeaf == 1) {
                    TempNode.left = (-nodes2[i].left) - 1;
                } else {
                    TempNode.left = nodes2[i].left;
                }
                nodes[i] = TempNode;
            }

#if !DontUseSGTree
            {
                Set = new List<int>[MaxDepth];
                for(int i = 0; i < MaxDepth; i++) Set[i] = new List<int>();
                Refit2(0, 0);
                GaussianTreeNode TempNode = new GaussianTreeNode();
                for(int i = MaxDepth - 1; i >= 0; i--) {
                    int SetCount = Set[i].Count;
                    for(int j = 0; j < SetCount; j++) {
                        int WriteIndex = Set[i][j];
                        CompactLightBVHData LBVHNode = nodes[WriteIndex];
                        Vector3 V;
                        Vector3 mean;
                        float variance;
                        float intensity;
                        float radius;
                        if(LBVHNode.left < 0) {
                            TempNode = SGTreeNodes[-(LBVHNode.left+1)];
                            Vector3 ExtendedCenter = CommonFunctions.ToVector3(LightBVHTransforms[-(LBVHNode.left+1)].Transform * CommonFunctions.ToVector4(TempNode.S.Center + new Vector3(TempNode.S.Radius, 0, 0), 1));
                            Vector3 center = CommonFunctions.ToVector3(LightBVHTransforms[-(LBVHNode.left+1)].Transform * CommonFunctions.ToVector4(TempNode.S.Center, 1));
                            Vector3 Axis = CommonFunctions.ToVector3(LightBVHTransforms[-(LBVHNode.left+1)].Transform * CommonFunctions.ToVector4(TempNode.axis, 0));
                            float Scale = Distance(center, ExtendedCenter) / TempNode.S.Radius;
                            TempNode.sharpness = Mathf.Min(VMFAxisLengthToSharpness(Mathf.Clamp(VMFSharpnessToAxisLength(TempNode.sharpness), 0.0f, 1.0f)), 2199023255552.0f);// ((3.0f * Distance(Vector3.zero, V) - Mathf.Pow(Distance(Vector3.zero, V), 3))) / (1.0f - Mathf.Pow(Distance(Vector3.zero, V), 2));
                            TempNode.axis = Axis;
                            TempNode.S.Center = center;
                            TempNode.variance *= Scale;
                            TempNode.S.Radius *= Scale;
                            TempNode.intensity *= Scale * Scale;
                        } else {
                            GaussianTreeNode LeftNode = SGTree[nodes[WriteIndex].left];    
                            GaussianTreeNode RightNode = SGTree[nodes[WriteIndex].left + 1];

                            float phi_left = LeftNode.intensity;    
                            float phi_right = RightNode.intensity;    
                            float w_left = phi_left / (phi_left + phi_right);
                            float w_right = phi_right / (phi_left + phi_right);
                            
                            V = w_left * LeftNode.axis * VMFSharpnessToAxisLength(LeftNode.sharpness) + w_right * RightNode.axis * VMFSharpnessToAxisLength(RightNode.sharpness);
                            // V = w_left * LeftNode.axis + w_right * RightNode.axis;//may be wrong, paper uses BAR_V(BAR_axis here), not just normalized V/axis

                            mean = w_left * LeftNode.S.Center + w_right * RightNode.S.Center;
                            variance = w_left * LeftNode.variance + w_right * RightNode.variance + w_left * w_right * Vector3.Dot(LeftNode.S.Center - RightNode.S.Center, LeftNode.S.Center - RightNode.S.Center);

                            intensity = LeftNode.intensity + RightNode.intensity;
                            radius = Mathf.Max(Distance(mean, LeftNode.S.Center) + LeftNode.S.Radius, Distance(mean, RightNode.S.Center) + RightNode.S.Radius);

                            float AxisLength = Distance(Vector3.zero, V);
                            if(AxisLength == 0) V = new Vector3(0,1,0);
                            else V /= AxisLength;
                            TempNode.sharpness = Mathf.Min(VMFAxisLengthToSharpness(Mathf.Clamp(AxisLength, 0.0f, 1.0f)), 2199023255552.0f);// ((3.0f * Distance(Vector3.zero, V) - Mathf.Pow(Distance(Vector3.zero, V), 3))) / (1.0f - Mathf.Pow(Distance(Vector3.zero, V), 2));

                            TempNode.axis = V;
                            TempNode.S.Center = mean;
                            TempNode.variance = variance;
                            TempNode.intensity = intensity;
                            TempNode.S.Radius = radius;
                        }

                        TempNode.left = LBVHNode.left;
                        SGTree[WriteIndex] = TempNode;
                    }
                }
            }
#endif

        }



        public void ClearAll() {
            if(LightTrisArray.IsCreated) LightTrisArray.Dispose();
            if(indices_going_left_array.IsCreated) indices_going_left_array.Dispose();
            if(tempArray.IsCreated) tempArray.Dispose();
            if(SAHArray.IsCreated) SAHArray.Dispose();
            if(DimensionedIndicesArray.IsCreated) DimensionedIndicesArray.Dispose();
            if(nodes2Array.IsCreated) nodes2Array.Dispose();
            // LightTrisArray.Dispose();
            // // nodesArray.Dispose();
            // SAHArray.Dispose();
            // indices_going_left_array.Dispose();
            // tempArray.Dispose();
            CommonFunctions.DeepClean(ref FinalIndices);
            CommonFunctions.DeepClean(ref nodes);
#if !DontUseSGTree
            CommonFunctions.DeepClean(ref SGTree);
#endif
        }
    }
}