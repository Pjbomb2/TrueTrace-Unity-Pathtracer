using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;

namespace TrueTrace {
    [System.Serializable]
    public class LightBVHBuilder {
        void OnDestroy() {
            Debug.Log("EEE");
        }
        public NodeBounds ParentBound;
        public struct DirectionCone {
            public Vector3 W;
            public float cosTheta;

            public DirectionCone(Vector3 w, float cosTheta) {
                W = w;
                this.cosTheta = cosTheta;
            }
        }

        private float luminance(float r, float g, float b) { return 0.299f * r + 0.587f * g + 0.114f * b; }
        TTStopWatch TTWatch;

        float Dot(ref Vector3 A, ref Vector3 B) {
            return A.x * B.x + A.y * B.y + A.z * B.z;
        }
        float Dot(Vector3 A) {
            return A.x * A.x + A.y * A.y + A.z * A.z;
        }
        float Length(Vector3 A) {
            return (float)System.Math.Sqrt(Dot(A));
        }

        public bool IsEmpty(DirectionCone Cone) {return Cone.cosTheta == float.MaxValue;}
        public bool IsEmpty(ref LightBounds Cone) {return Cone.w == Vector3.zero;}
        public DirectionCone EntireSphere() {return new DirectionCone(new Vector3(0,0,1), -1);}
        public DirectionCone BoundSubtendedDirections(AABB aabb, Vector3 p) {
            float diameter = aabb.IsInside(p) ? Dot(aabb.BBMax - p) : 0;
            Vector3 pCenter = (aabb.BBMax + aabb.BBMin) / 2.0f;
            float sin2ThetaMax = Dot(p - pCenter);
            if(sin2ThetaMax < diameter) return  EntireSphere();
            Vector3 w = (pCenter - p).normalized;
            sin2ThetaMax = diameter / sin2ThetaMax;
            float cosThetaMax = Mathf.Sqrt(1.0f - sin2ThetaMax);
            return new DirectionCone(w, cosThetaMax);
        }

        private float AngleBetween(Vector3 v1, Vector3 v2) {
            if(Dot(ref v1, ref v2) < 0) return 3.14159f - 2.0f * (float)System.Math.Asin(Length(v1 + v2) / 2.0f);
            else return 2.0f * (float)System.Math.Asin(Length(v2 - v1) / 2.0f);
        }

        private Matrix4x4 Rotate(float sinTheta, float cosTheta, Vector3 axis) {
            Vector3 a = (axis).normalized;
            Matrix4x4 m = Matrix4x4.identity;
            m[0,0] = a.x * a.x + (1 - a.x * a.x) * cosTheta;
            m[0,1] = a.x * a.y * (1 - cosTheta) - a.z * sinTheta;
            m[0,2] = a.x * a.z * (1 - cosTheta) + a.y * sinTheta;
            m[0,3] = 0;

            m[1,0] = a.x * a.y * (1 - cosTheta) + a.z * sinTheta;
            m[1,1] = a.y * a.y + (1 - a.y * a.y) * cosTheta;
            m[1,2] = a.y * a.z * (1 - cosTheta) - a.x * sinTheta;
            m[1,3] = 0;

            m[2,0] = a.x * a.z * (1 - cosTheta) - a.y * sinTheta;
            m[2,1] = a.y * a.z * (1 - cosTheta) + a.x * sinTheta;
            m[2,2] = a.z * a.z + (1 - a.z * a.z) * cosTheta;
            m[2,3] = 0;

            return m * m.transpose;
        }

        private Matrix4x4 Rotate(float Theta, Vector3 axis) {

            return Rotate((float)System.Math.Sin(Theta),(float)System.Math.Cos(Theta), axis);
        }

        public void UnionCone(ref LightBounds A, ref LightBounds B) {
            if(IsEmpty(ref A)) {A.w = B.w; A.cosTheta_o = B.cosTheta_o; return;}
            if(IsEmpty(ref B)) return;

            float theta_a = (float)System.Math.Acos(Mathf.Clamp(A.cosTheta_o,-1.0f, 1.0f));
            float theta_b = (float)System.Math.Acos(Mathf.Clamp(B.cosTheta_o,-1.0f, 1.0f));
            float theta_d = AngleBetween(A.w, B.w);
            if(System.Math.Min(theta_d + theta_b, 3.14159f) <= theta_a) return;
            if(System.Math.Min(theta_d + theta_a, 3.14159f) <= theta_b) {A.w = B.w; A.cosTheta_o = B.cosTheta_o; return;}

            float theta_o = (theta_a + theta_d + theta_b) / 2.0f;
            if(theta_o >= 3.14159f) {A.w = new Vector3(0,0,0); A.cosTheta_o = -1; return;}

            float theta_r = theta_o - theta_a;
            Vector3 wr = Vector3.Cross(A.w, B.w);
            if(Vector3.Dot(wr, wr) == 0) {A.w = new Vector3(0,0,0); A.cosTheta_o = -1; return;}
            A.w = Rotate(theta_r, wr) * A.w;
            A.cosTheta_o =(float)System.Math.Cos(theta_o);
        }



    

        private void Union(ref LightBounds A, LightBounds B) {
            if(A.phi == 0) {A = B; return;}
            if(B.phi == 0) return;
            UnionCone(ref A, ref B);
            A.cosTheta_e = System.Math.Min(A.cosTheta_e, B.cosTheta_e);
            A.b.Extend(ref B.b);
            A.phi += B.phi;
            A.LightCount += B.LightCount;
        }

        private float surface_area(AABB aabb) {
            Vector3 sizes = aabb.BBMax - aabb.BBMin;
            return 2.0f * ((sizes.x * sizes.y) + (sizes.x * sizes.z) + (sizes.y * sizes.z)); 
        }

        private float EvaluateCost(ref LightBounds b, float Kr, int dim) {
            float theta_o = (float)System.Math.Acos(b.cosTheta_o);
            float theta_e = (float)System.Math.Acos(b.cosTheta_e);
            float theta_w = System.Math.Min(theta_o + theta_e, 3.14159f);
            float sinTheta_o = Mathf.Sqrt(1.0f - b.cosTheta_o * b.cosTheta_o);
            float M_omega = 2.0f * 3.14159f * (1.0f - b.cosTheta_o) +
                            3.14159f / 2.0f *
                                (2.0f * theta_w * sinTheta_o -(float)System.Math.Cos(theta_o - 2.0f * theta_w) -
                                 2.0f * theta_o * sinTheta_o + b.cosTheta_o);


            return b.phi * M_omega * Kr * surface_area(b.b) / (float)Mathf.Max(b.LightCount, 1);
        }

        private LightBounds[] LightTris;
        private NodeBounds[] nodes2;
        private CompactLightBVHData[] nodes;
        public GaussianTreeNode[] SGTree;

        private int[] DimensionedIndices;

        public struct ObjectSplit {
            public int index;
            public float cost;
            public int dimension;
            public LightBounds aabb_left;
            public LightBounds aabb_right;
        }
        private ObjectSplit split = new ObjectSplit();

        public int PrimCount;
        private float[] SAH;
        private bool[] indices_going_left;
        private int[] temp;
        public int[] FinalIndices;

        ObjectSplit partition_sah(int first_index, int index_count, AABB parentBounds) {
            split.cost = float.MaxValue;
            split.index = -1;
            split.dimension = -1;

            LightBounds aabb_left = new LightBounds();
            LightBounds aabb_right = new LightBounds();
            int Offset;
            Vector3 Diagonal = parentBounds.BBMax - parentBounds.BBMin;
            float Kr1 = System.Math.Max(System.Math.Max(Diagonal.x, Diagonal.y), Diagonal.z);
            for(int dimension = 0; dimension < 3; dimension++) {
                float Kr = Kr1 / Diagonal[dimension];
                Offset = PrimCount * dimension + first_index;
                aabb_left = LightTris[DimensionedIndices[Offset]];
                SAH[1] = EvaluateCost(ref aabb_left, Kr, dimension);
                for(int i = 2; i < index_count; i++) {
                    Union(ref aabb_left, LightTris[DimensionedIndices[Offset + i - 1]]);

                    SAH[i] = EvaluateCost(ref aabb_left, Kr, dimension) * (float)i;
                }

                {
                    aabb_right = LightTris[DimensionedIndices[Offset + index_count - 1]];
                    float cost = SAH[index_count - 1] + EvaluateCost(ref aabb_right, Kr, dimension) * (float)(index_count - (index_count - 1));

                    if(cost != 0)
                    if(cost <= split.cost) {
                        split.cost = cost;
                        split.index = first_index + index_count - 1;
                        split.dimension = dimension;
                        split.aabb_right = aabb_right;
                    }
                }


                for(int i = index_count - 2; i > 0; i--) {
                    Union(ref aabb_right, LightTris[DimensionedIndices[Offset + i]]);

                    float cost = SAH[i] + EvaluateCost(ref aabb_right, Kr, dimension) * (float)(index_count - i);

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
                    Union(ref split.aabb_right, LightTris[DimensionedIndices[Offset + i]]);
                }
            }
            split.aabb_left = LightTris[DimensionedIndices[Offset + first_index]];
            for(int i = first_index + 1; i < split.index; i++) Union(ref split.aabb_left, LightTris[DimensionedIndices[Offset + i]]);
            return split;
        }
        public int MaxDepth;
        void BuildRecursive(int nodesi, ref int node_index, int first_index, int index_count, int Depth) {
            if(index_count == 1) {
                nodes2[nodesi].left = first_index;
                nodes2[nodesi].isLeaf = 1;
                MaxDepth = System.Math.Max(Depth, MaxDepth);
                return;
            }
            ObjectSplit split = partition_sah(first_index, index_count, nodes2[nodesi].aabb.b);
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
          
                System.Array.Copy(temp, 0, DimensionedIndices, Offset + first_index, index_count);
            }
            nodes2[nodesi].left = node_index;

            nodes2[nodes2[nodesi].left].aabb = split.aabb_left;
            nodes2[nodes2[nodesi].left + 1].aabb = split.aabb_right;
            node_index += 2;

            BuildRecursive(nodes2[nodesi].left, ref node_index,first_index,split.index - first_index, Depth + 1);
            BuildRecursive(nodes2[nodesi].left + 1, ref node_index,first_index + split.index - first_index,first_index + index_count - split.index, Depth + 1);
        }
        public ComputeBuffer[] WorkingSet;

  
        private float AreaOfTriangle(Vector3 pt1, Vector3 pt2, Vector3 pt3)
        {
            float a = Vector3.Distance(pt1, pt2);
            float b = Vector3.Distance(pt2, pt3);
            float c = Vector3.Distance(pt3, pt1);
            float s = (a + b + c) / 2.0f;
            return Mathf.Sqrt(s * (s - a) * (s - b) * (s - c));
        }

        public List<int>[] Set;
        void Refit(int Depth, int CurrentIndex) {
            if(nodes2[CurrentIndex].aabb.cosTheta_e == 0) return;
            Set[Depth].Add(CurrentIndex);
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

  
        public LightBVHBuilder(List<LightTriData> Tris, List<Vector3> Norms, float phi, List<float> LuminanceWeights) {//need to make sure incomming is transformed to world space already
            TTWatch = new TTStopWatch("LBVH FOR TRI COUNT: " + Tris.Count);
            TTWatch.Start();
            PrimCount = Tris.Count;          
            MaxDepth = 0;
            LightTris = new LightBounds[PrimCount];  
            nodes2 = new NodeBounds[PrimCount * 2];    
            SAH = new float[PrimCount];
            indices_going_left = new bool[PrimCount];
            temp = new int[PrimCount];
            DimensionedIndices = new int[PrimCount * 3];   
            FinalIndices = new int[PrimCount];
            float[] CentersX = new float[PrimCount];
            float[] CentersY = new float[PrimCount];
            float[] CentersZ = new float[PrimCount];     
            for(int i = 0; i < PrimCount * 2; i++) nodes2[i] = new NodeBounds();
            for(int i = 0; i < PrimCount; i++) {
                AABB TriAABB = new AABB();
                TriAABB.init();
                TriAABB.Extend(Tris[i].pos0);
                TriAABB.Extend(Tris[i].pos0 + Tris[i].posedge1);
                TriAABB.Extend(Tris[i].pos0 + Tris[i].posedge2);
                TriAABB.Validate(new Vector3(0.0001f,0.0001f,0.0001f));
                DirectionCone tricone = new DirectionCone(-Norms[i], 1);
                float ThisPhi = AreaOfTriangle(Tris[i].pos0, Tris[i].pos0 + Tris[i].posedge1, Tris[i].pos0 + Tris[i].posedge2) * LuminanceWeights[i];
                LightBounds TempBound = new LightBounds(TriAABB, tricone.W, ThisPhi, tricone.cosTheta,(float)System.Math.Cos(3.14159f / 2.0f), 1, 0);
                LightTris[i] = TempBound;
                FinalIndices[i] = i;
                CentersX[i] = (TriAABB.BBMax.x - TriAABB.BBMin.x) / 2.0f + TriAABB.BBMin.x;
                CentersY[i] = (TriAABB.BBMax.y - TriAABB.BBMin.y) / 2.0f + TriAABB.BBMin.y;
                CentersZ[i] = (TriAABB.BBMax.z - TriAABB.BBMin.z) / 2.0f + TriAABB.BBMin.z;
                Union(ref nodes2[0].aabb, TempBound);
            }

            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersX[s1] - CentersX[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            System.Array.Copy(FinalIndices, 0, DimensionedIndices, 0, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersY[s1] - CentersY[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            System.Array.Copy(FinalIndices, 0, DimensionedIndices, PrimCount, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersZ[s1] - CentersZ[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            System.Array.Copy(FinalIndices, 0, DimensionedIndices, PrimCount * 2, PrimCount);

            int nodeIndex = 2;
            BuildRecursive(0, ref nodeIndex,0,PrimCount, 1);
            // NodeBounds[] TempNodes = new NodeBounds[nodeIndex];
            // for(int i = 0; i < nodeIndex; i++) {
                // TempNodes[i] = nodes2[i];
            // }
            // nodes2 = new NodeBounds[nodeIndex];
            // nodes2 = TempNodes;
            for(int i = 0; i < PrimCount * 2; i++) {
                if(nodes2[i].isLeaf == 1) {
                    nodes2[i].left = DimensionedIndices[nodes2[i].left];
                }
            }
            nodes = new CompactLightBVHData[nodes2.Length];
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
            CommonFunctions.DeepClean(ref nodes2);
            CommonFunctions.DeepClean(ref LightTris);

            TTWatch.Stop("LBVH");
            TTWatch.Start();
            {
                SGTree = new GaussianTreeNode[nodes.Length];
                Set = new List<int>[MaxDepth];
                for(int i = 0; i < MaxDepth; i++) Set[i] = new List<int>();
                Refit2(0, 0);
                for(int i = MaxDepth - 1; i >= 0; i--) {
                    int SetCount = Set[i].Count;
                    for(int j = 0; j < SetCount; j++) {
                        GaussianTreeNode TempNode = new GaussianTreeNode();
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
                            radius = Mathf.Max(Mathf.Max(Vector3.Distance(mean, ThisLight.pos0), Vector3.Distance(mean, ThisLight.pos0 + ThisLight.posedge1)), Vector3.Distance(mean, ThisLight.pos0 + ThisLight.posedge2));
                        } else {
                            GaussianTreeNode LeftNode = SGTree[nodes[WriteIndex].left];    
                            GaussianTreeNode RightNode = SGTree[nodes[WriteIndex].left + 1];

                            float phi_left = LeftNode.intensity;    
                            float phi_right = RightNode.intensity;    
                            float w_left = phi_left / (phi_left + phi_right);
                            float w_right = phi_right / (phi_left + phi_right);
                            
                            V = w_left * LeftNode.axis + w_right * RightNode.axis;//may be wrong, paper uses BAR_V(BAR_axis here), not just normalized V/axis

                            mean = w_left * LeftNode.S.Center + w_right * RightNode.S.Center;
                            variance = w_left * LeftNode.variance + w_right * RightNode.variance + w_left * w_right * Vector3.Dot(LeftNode.S.Center - RightNode.S.Center, LeftNode.S.Center - RightNode.S.Center);

                            intensity = LeftNode.intensity + RightNode.intensity;
                            radius = Mathf.Max(Vector3.Distance(mean, LeftNode.S.Center) + LeftNode.S.Radius, Vector3.Distance(mean, RightNode.S.Center) + RightNode.S.Radius);
                        }
                        TempNode.sharpness = ((3.0f * Vector3.Distance(Vector3.zero, V) - Mathf.Pow(Vector3.Distance(Vector3.zero, V), 3))) / (1.0f - Mathf.Pow(Vector3.Distance(Vector3.zero, V), 2));
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
            TTWatch.Stop("GAUSSIAN TREE");


            CommonFunctions.DeepClean(ref nodes);

        }



        public LightBVHBuilder(LightBounds[] Tris,ref GaussianTreeNode[] SGTree, LightBVHTransform[] LightBVHTransforms, GaussianTreeNode[] SGTreeNodes) {//need to make sure incomming is transformed to world space already
            PrimCount = Tris.Length;          
            MaxDepth = 0;
            LightTris = Tris;  
            nodes2 = new NodeBounds[PrimCount * 2];    
            SAH = new float[PrimCount];
            indices_going_left = new bool[PrimCount];
            temp = new int[PrimCount];
            DimensionedIndices = new int[PrimCount * 3];   
            FinalIndices = new int[PrimCount];
            float[] CentersX = new float[PrimCount];
            float[] CentersY = new float[PrimCount];
            float[] CentersZ = new float[PrimCount];     
            for(int i = 0; i < PrimCount * 2; i++) nodes2[i] = new NodeBounds();
            for(int i = 0; i < PrimCount; i++) {
                FinalIndices[i] = i;
                CentersX[i] = (Tris[i].b.BBMax.x - Tris[i].b.BBMin.x) / 2.0f + Tris[i].b.BBMin.x;
                CentersY[i] = (Tris[i].b.BBMax.y - Tris[i].b.BBMin.y) / 2.0f + Tris[i].b.BBMin.y;
                CentersZ[i] = (Tris[i].b.BBMax.z - Tris[i].b.BBMin.z) / 2.0f + Tris[i].b.BBMin.z;
                Union(ref nodes2[0].aabb, Tris[i]);
            }

            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersX[s1] - CentersX[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            System.Array.Copy(FinalIndices, 0, DimensionedIndices, 0, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersY[s1] - CentersY[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            System.Array.Copy(FinalIndices, 0, DimensionedIndices, PrimCount, PrimCount);
            for(int i = 0; i < PrimCount; i++) FinalIndices[i] = i;
            System.Array.Sort(FinalIndices, (s1,s2) => {var sign = CentersZ[s1] - CentersZ[s2]; return sign < 0 ? -1 : (sign == 0 ? 0 : 1);});
            System.Array.Copy(FinalIndices, 0, DimensionedIndices, PrimCount * 2, PrimCount);

            int nodeIndex = 2;
            BuildRecursive(0, ref nodeIndex,0,PrimCount, 1);
            for(int i = 0; i < PrimCount * 2; i++) {
                if(nodes2[i].isLeaf == 1) {
                    nodes2[i].left = DimensionedIndices[nodes2[i].left];
                    // nodes2[i].left = Offsets[nodes2[i].left];
                }
            }
            Set = new List<int>[MaxDepth];
            WorkingSet = new ComputeBuffer[MaxDepth];
            for(int i = 0; i < MaxDepth; i++) Set[i] = new List<int>();
            Refit(0, 0);
            for(int i = 0; i < MaxDepth; i++) {
                WorkingSet[i] = new ComputeBuffer(Set[i].Count, 4);
                WorkingSet[i].SetData(Set[i]);
            }
            nodes = new CompactLightBVHData[nodes2.Length];
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
            CommonFunctions.DeepClean(ref nodes2);


            {
                for(int i = 0; i < MaxDepth; i++) Set[i] = new List<int>();
                Refit2(0, 0);
                for(int i = MaxDepth - 1; i >= 0; i--) {
                    int SetCount = Set[i].Count;
                    for(int j = 0; j < SetCount; j++) {
                        GaussianTreeNode TempNode = new GaussianTreeNode();
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
                            float Scale = Vector3.Distance(center, ExtendedCenter) / TempNode.S.Radius;
                            TempNode.axis = Axis;
                            TempNode.S.Center = center;
                            TempNode.variance *= Scale;
                            TempNode.S.Radius *= Scale;
                            TempNode.intensity *= Scale;
                            // ThisLight.posedge1 = CommonFunctions.ToVector3(this.transform.localToWorldMatrix * CommonFunctions.ToVector4(ThisLight.posedge1, 1));
                            // ThisLight.posedge2 = CommonFunctions.ToVector3(this.transform.localToWorldMatrix * CommonFunctions.ToVector4(ThisLight.posedge2, 1));

                            

                            // intensity = new Vector3(ThisLight.SourceEnergy, ThisLight.SourceEnergy, ThisLight.SourceEnergy) * area;
                            // V = 0.5f * (Vector3.Cross(ThisLight.posedge1.normalized, ThisLight.posedge2.normalized).normalized);
                            // mean = (ThisLight.pos0 + (ThisLight.pos0 + ThisLight.posedge1) + (ThisLight.pos0 + ThisLight.posedge2)) / 3.0f;
                            // variance = (Vector3.Dot(ThisLight.posedge1, ThisLight.posedge1) + Vector3.Dot(ThisLight.posedge2, ThisLight.posedge2) - Vector3.Dot(ThisLight.posedge1, ThisLight.posedge2)) / 18.0f;
                            // radius = Mathf.Max(Mathf.Max(Vector3.Distance(mean, ThisLight.pos0), Vector3.Distance(mean, ThisLight.pos0 + ThisLight.posedge1)), Vector3.Distance(mean, ThisLight.pos0 + ThisLight.posedge2));
                        } else {
                            GaussianTreeNode LeftNode = SGTree[nodes[WriteIndex].left];    
                            GaussianTreeNode RightNode = SGTree[nodes[WriteIndex].left + 1];

                            float phi_left = LeftNode.intensity;    
                            float phi_right = RightNode.intensity;    
                            float w_left = phi_left / (phi_left + phi_right);
                            float w_right = phi_right / (phi_left + phi_right);
                            
                            V = w_left * LeftNode.axis + w_right * RightNode.axis;//may be wrong, paper uses BAR_V(BAR_axis here), not just normalized V/axis

                            mean = w_left * LeftNode.S.Center + w_right * RightNode.S.Center;
                            variance = w_left * LeftNode.variance + w_right * RightNode.variance + w_left * w_right * Vector3.Dot(LeftNode.S.Center - RightNode.S.Center, LeftNode.S.Center - RightNode.S.Center);

                            intensity = LeftNode.intensity + RightNode.intensity;
                            radius = Mathf.Max(Vector3.Distance(mean, LeftNode.S.Center) + LeftNode.S.Radius, Vector3.Distance(mean, RightNode.S.Center) + RightNode.S.Radius);
                            TempNode.sharpness = ((3.0f * Vector3.Distance(Vector3.zero, V) - Mathf.Pow(Vector3.Distance(Vector3.zero, V), 3))) / (1.0f - Mathf.Pow(Vector3.Distance(Vector3.zero, V), 2));
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
            CommonFunctions.DeepClean(ref nodes);


        }

        public void ClearAll() {
            CommonFunctions.DeepClean(ref LightTris);
            CommonFunctions.DeepClean(ref nodes2);
            CommonFunctions.DeepClean(ref nodes);
            CommonFunctions.DeepClean(ref SAH);
            CommonFunctions.DeepClean(ref indices_going_left);
            CommonFunctions.DeepClean(ref temp);
            CommonFunctions.DeepClean(ref DimensionedIndices);
            CommonFunctions.DeepClean(ref FinalIndices);
            CommonFunctions.DeepClean(ref SGTree);
        }
    }
}