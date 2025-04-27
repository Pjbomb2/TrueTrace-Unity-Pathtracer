using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using UnityEditor;
#if UNITY_EDITOR
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace TrueTrace {
[ExecuteInEditMode][System.Serializable][RequireComponent(typeof(MeshRenderer))]
public class WaveV2 : MonoBehaviour
{
    public bool RunTemp = false;
    [SerializeField] public bool Run = true;
    [SerializeField] public int TextureDimensions = 512;
    [SerializeField] public float WaveDamper = 0.99f;
    [SerializeField] public float WaveDelta = 0.9f;
    [SerializeField] public float FakeWaterIntensity = 1.0f;
    [SerializeField] public float FakeWaterTiling = 1.0f;
    [SerializeField] public bool DoFakeWaves = false;
    [SerializeField] public bool DoRealWater = true;
    [SerializeField] public bool UseVertexNormals = false;
    [SerializeField] public int BlurPasses = 1;
    [SerializeField] public int SimulationPasses = 1;
    private GraphicsBuffer VertexBuffer;
    private ComputeBuffer IndexBuffer;
    private Camera VoxelizeCamera;
    public RenderTexture ComputeTextureA;
    private RenderTexture ComputeTextureB;
    private RenderTexture ComputeTextureC;
    private RenderTexture ComputeTextureD;
    private ComputeShader WaveShader;
    public bool ReInitialize = false;

    private Mesh RenderMesh;
    private bool ChildHasName(Transform TargetParent, string NameToFind) {
        int childCount = TargetParent.childCount;
        for(int i = 0; i < childCount; i++) {
            if(TargetParent.GetChild(i).gameObject.name.Equals(NameToFind)) return true;
        }
        return false;
    }
    private int GetChildWithName(Transform TargetParent, string NameToFind) {
        int childCount = TargetParent.childCount;
        for(int i = 0; i < childCount; i++) {
            if(TargetParent.GetChild(i).gameObject.name.Equals(NameToFind)) return i;
        }
        return -1;
    }
    public void OnDestroy() {
        if(!Application.isPlaying) {
            int childCount = this.transform.childCount;
            for(int i = childCount - 1; i >= 0; i--) {
                DestroyImmediate(this.transform.GetChild(i).gameObject);
            }
        }
    }
    public void OnEnable() {
        if (WaveShader == null) {WaveShader = Resources.Load<ComputeShader>("Utility/Water/WaveShaderV2"); }
        if(!ChildHasName(this.transform, "WaterPlaneParent")) {
            Vector3[] vertices;
            Vector2[] UVs;
            MeshRenderer TargetMesh = gameObject.GetComponent<MeshRenderer>();
            Material[] DebugMaterial = new Material[1];
            DebugMaterial[0] = new Material(Shader.Find("Standard"));
            GameObject ChildParentObject = new GameObject();
            GameObject ChildObject = new GameObject();
            LayerMask IgnoreMask = LayerMask.NameToLayer("IgnoreLayer");
            ChildObject.layer = IgnoreMask;
            this.gameObject.layer = IgnoreMask;
            ChildParentObject.name = "WaterPlaneParent";
            ChildObject.name = "WATERPLANE";
            ChildParentObject.transform.parent = this.transform;
            ChildObject.transform.parent = ChildParentObject.transform;
            ChildObject.AddComponent<MeshFilter>();
            ChildObject.AddComponent<MeshRenderer>();
            ChildObject.GetComponent<MeshRenderer>().materials = DebugMaterial;
            ChildObject.GetComponent<MeshRenderer>().rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.DynamicGeometry;
            GameObject CameraObject = new GameObject();
            CameraObject.name = "VoxelizeCamera";
            VoxelizeCamera = CameraObject.AddComponent<Camera>();
            VoxelizeCamera.transform.position = this.transform.position + new Vector3(0,1,0) * 10.0f;
            VoxelizeCamera.transform.parent = this.transform;
            VoxelizeCamera.transform.eulerAngles = new Vector3(90,0,0);

            VoxelizeCamera.cullingMask = VoxelizeCamera.cullingMask & ~(1 << IgnoreMask.value);

            VoxelizeCamera.enabled = false;
            VoxelizeCamera.orthographic = true;
            VoxelizeCamera.farClipPlane = 100.0f;
            // VoxelizeCamera.orthographicSize = 900.0f;

                Vector3 Iterator = (TargetMesh.bounds.max - TargetMesh.bounds.min);
                Iterator.x /= (float)(TextureDimensions);
                Iterator.z /= (float)(TextureDimensions);
                Vector3 MinAABB = TargetMesh.bounds.min;
                Vector3 MaxAABB = TargetMesh.bounds.max;
                vertices = new Vector3[(TextureDimensions + 1) * (TextureDimensions + 1)];
                UVs = new Vector2[(TextureDimensions + 1) * (TextureDimensions + 1)];
                for(int i = 0; i < TextureDimensions + 1; i++) {
                    for(int j = 0; j < TextureDimensions + 1; j++) {
                        Vector3 Min = MinAABB + new Vector3(i * Iterator.x, 0, j * Iterator.z);
                        Vector3 Max = MinAABB + new Vector3((i + 1) * Iterator.x, 0, (j + 1) * Iterator.z);
                        int Index = i + j * TextureDimensions;
                        vertices[i + j * (TextureDimensions + 1)] = Min;
                        UVs[i + j * (TextureDimensions + 1)] = new Vector2((float)i / (float)TextureDimensions, (float)j / (float)TextureDimensions);
                    }
                }

                int[] triangles = new int[TextureDimensions * TextureDimensions * 6];
                for (int ti = 0, vi = 0, y = 0; y < TextureDimensions; y++, vi++) {
                    for (int x = 0; x < TextureDimensions; x++, ti += 6, vi++) {
                        triangles[ti] = vi;
                        triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                        triangles[ti + 4] = triangles[ti + 1] = vi + TextureDimensions + 1;
                        triangles[ti + 5] = vi + TextureDimensions + 2;
                    }
                }

                RenderMesh = new Mesh();
                RenderMesh.name = "RenderMesh";
                RenderMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                RenderMesh.vertices = vertices;
                RenderMesh.triangles = triangles;
                RenderMesh.uv = UVs;
                RenderMesh.RecalculateNormals();   
                RenderMesh.RecalculateTangents();   
                RenderMesh.RecalculateBounds();
                RenderMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                ChildObject.GetComponent<MeshFilter>().mesh = RenderMesh;
                Vector3 Max2 = ChildObject.GetComponent<Renderer>().bounds.max + new Vector3(0,10,0);
                Vector3 Min2 = ChildObject.GetComponent<Renderer>().bounds.min - new Vector3(0,10,0);
                ChildObject.GetComponent<Renderer>().bounds = new Bounds((Max2 + Min2) / 2.0f, Max2 - Min2);
                VoxelizeCamera.orthographicSize = Mathf.Max(Max2.x - Min2.x, Max2.z - Min2.z) / 2.0f;


                ChildObject.AddComponent<RayTracingObject>();
                ChildParentObject.AddComponent<ParentObject>();
                ChildParentObject.GetComponent<ParentObject>().IsDeformable = true;
                RunTemp = true;
            }
            Run = true;
    }

    int IntIter = 0;
    float TimeInterval = 0;
    int VertexCount = 0;

    int NormalCalcKernel;
    int SanitizeWallKernel;
    int UpdateWallKernel;
    int SimulateKernel;
    int ApplyWaveKernel;
    int BlurNormsKernel;
    int ApplyNormsKernel;
    float PrevTime;

    public void ClearOutRenderTexture(RenderTexture renderTexture) {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = rt;
    }
        private RenderTexture DummyTex;
        Transform ChildTransform;
    public void FixedUpdate()
    {
        if(Application.isPlaying) {
            if(RunTemp || WaveShader == null) {
                PrevTime = 0;
                WaveShader = Resources.Load<ComputeShader>("Utility/Water/WaveShaderV2");
                NormalCalcKernel = WaveShader.FindKernel("CalcNorms");
                SanitizeWallKernel = WaveShader.FindKernel("SanitizeWalls");
                UpdateWallKernel = WaveShader.FindKernel("UpdateWalls");
                SimulateKernel = WaveShader.FindKernel("Simulate");
                ApplyWaveKernel = WaveShader.FindKernel("ApplyWaves");
                BlurNormsKernel = WaveShader.FindKernel("BlurNorms");
                ApplyNormsKernel = WaveShader.FindKernel("ApplyNorms");
                WaveShader.SetVector("TextureDimensions", new Vector2(TextureDimensions, TextureDimensions));
                

                VoxelizeCamera = this.gameObject.transform.GetChild(GetChildWithName(this.transform, "VoxelizeCamera")).gameObject.GetComponent<Camera>();
                CommonFunctions.CreateRenderTexture(ref ComputeTextureA, TextureDimensions, TextureDimensions, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref ComputeTextureB, TextureDimensions, TextureDimensions, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref ComputeTextureC, TextureDimensions, TextureDimensions, CommonFunctions.RTFull4);
                CommonFunctions.CreateRenderTexture(ref ComputeTextureD, TextureDimensions, TextureDimensions, CommonFunctions.RTFull4);
                ClearOutRenderTexture(ComputeTextureA);
                ClearOutRenderTexture(ComputeTextureB);
                ClearOutRenderTexture(ComputeTextureC);
                ClearOutRenderTexture(ComputeTextureD);
                GameObject ChildObject = this.transform.GetChild(GetChildWithName(this.transform, "WaterPlaneParent")).GetChild(0).gameObject;
                Vector3 Max2 = ChildObject.GetComponent<Renderer>().bounds.max + new Vector3(0,10,0);
                Vector3 Min2 = ChildObject.GetComponent<Renderer>().bounds.min - new Vector3(0,10,0);
                ChildObject.GetComponent<Renderer>().bounds = new Bounds((Max2 + Min2) / 2.0f, Max2 - Min2);
                ChildTransform = ChildObject.transform;
                RenderMesh = ChildObject.GetComponent<MeshFilter>().mesh;
                CommonFunctions.CreateComputeBuffer(ref IndexBuffer, RenderMesh.triangles);
                RenderMesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
                VertexBuffer = RenderMesh.GetVertexBuffer(0);
                VertexCount = RenderMesh.vertices.Length;
                IntIter = 0;
                TimeInterval = 0;
                RunTemp = false;      
                DummyTex = new RenderTexture(1500, 1500, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
                DummyTex.enableRandomWrite = true;
                DummyTex.Create();
                VoxelizeCamera.targetTexture = DummyTex;
                Shader.SetGlobalVector("RelativeScale", new Vector2(VoxelizeCamera.orthographicSize * 2.0f, VoxelizeCamera.orthographicSize * 2.0f));//new Vector3(Mathf.Floor(Camera.main.transform.position.x),Mathf.Floor(Camera.main.transform.position.y),Mathf.Floor(Camera.main.transform.position.z)));
                Shader.SetGlobalVector("VoxelizerTexDimensions", new Vector2(TextureDimensions, TextureDimensions));//new Vector3(Mathf.Floor(Camera.main.transform.position.x),Mathf.Floor(Camera.main.transform.position.y),Mathf.Floor(Camera.main.transform.position.z)));
            } else if(Run && ComputeTextureA != null) {
                int AA = IntIter % 2;
                // GameObject.Find("WATERPLANE").GetComponent<MeshRenderer>().sharedMaterials[0].mainTexture = AA == 0 ? ComputeTextureA : ComputeTextureB;
                WaveShader.SetMatrix("LocalToWorld", ChildTransform.localToWorldMatrix);
                Shader.SetGlobalFloat("PlaneDefaultY", this.gameObject.transform.position.y);
                Shader.SetGlobalFloat("WaveDeltaTime", WaveDelta);
                Shader.SetGlobalFloat("PressureDamper", WaveDamper);
                Shader.SetGlobalVector("ThisCamCenter", (this.transform.position));//new Vector3(Mathf.Floor(Camera.main.transform.position.x),Mathf.Floor(Camera.main.transform.position.y),Mathf.Floor(Camera.main.transform.position.z)));
                Shader.SetGlobalInt("DoFakeWaves", DoFakeWaves ? 1 : 0);
                Shader.SetGlobalInt("DoRealWater", DoRealWater ? 1 : 0);
                Shader.SetGlobalInt("UseVertexNormals", UseVertexNormals ? 1 : 0);
                Shader.SetGlobalFloat("FakeWaterIntensity", FakeWaterIntensity);
                Shader.SetGlobalFloat("FakeWaterTiling", FakeWaterTiling);
                WaveShader.SetFloat("deltaT", Time.deltaTime);
                WaveShader.SetFloat("PrevTime", PrevTime);
                TimeInterval += Time.deltaTime;
                WaveShader.SetFloat("Time", TimeInterval);

                {//Sanitize Walls
                    WaveShader.SetTexture(SanitizeWallKernel, "WaveTexA", AA == 0 ? ComputeTextureA : ComputeTextureB);
                    WaveShader.Dispatch(SanitizeWallKernel, Mathf.CeilToInt((float)TextureDimensions / 16.0f), Mathf.CeilToInt((float)TextureDimensions / 16.0f), 1);
                }        
                {//Voxelize Walls
                    Graphics.CopyTexture(AA == 0 ? ComputeTextureA : ComputeTextureA, 0, 0, ComputeTextureC, 0, 0);
                    Shader.SetGlobalTexture("TEMPTEXC", ComputeTextureC);
                    Graphics.SetRandomWriteTarget(1, AA == 0 ? ComputeTextureA : ComputeTextureB);
                    #if !UNITY_PIPELINE_HDRP
                        VoxelizeCamera.RenderWithShader(Shader.Find("Hidden/WaveV2Voxelize"), "");
                    #endif
                    Graphics.ClearRandomWriteTargets();
                }

                {//Update Walls
                    WaveShader.SetTexture(UpdateWallKernel, "WaveTexA", AA == 0 ? ComputeTextureA : ComputeTextureB);
                    WaveShader.SetTexture(UpdateWallKernel, "WaveTexB", AA == 1 ? ComputeTextureA : ComputeTextureB);
                    WaveShader.Dispatch(UpdateWallKernel, Mathf.CeilToInt((float)TextureDimensions / 16.0f), Mathf.CeilToInt((float)TextureDimensions / 16.0f), 1);
                }


                {//Simulate Water
                    if(DoRealWater) {
                        WaveShader.SetTexture(SimulateKernel, "WaveTexA", AA == 1 ? ComputeTextureA : ComputeTextureB);
                        WaveShader.SetTexture(SimulateKernel, "WaveTexB", AA == 0 ? ComputeTextureA : ComputeTextureB);
                        WaveShader.Dispatch(SimulateKernel, Mathf.CeilToInt((float)TextureDimensions / 16.0f), Mathf.CeilToInt((float)TextureDimensions / 16.0f), 1);
                        for(int i = 0; i < SimulationPasses - 1; i++) {
                            Graphics.CopyTexture(AA == 1 ? ComputeTextureA : ComputeTextureB, 0, 0, AA == 0 ? ComputeTextureA : ComputeTextureB, 0, 0);
                            WaveShader.Dispatch(SimulateKernel, Mathf.CeilToInt((float)TextureDimensions / 16.0f), Mathf.CeilToInt((float)TextureDimensions / 16.0f), 1);
                        }
                    }
                }


                {//Apply Wave Heightmap
                    WaveShader.SetInt("gVertexCount", VertexCount); 
                    WaveShader.SetBuffer(ApplyWaveKernel, "bufVertices", VertexBuffer);
                    WaveShader.SetTexture(ApplyWaveKernel, "WaveTexB", AA == 1 ? ComputeTextureA : ComputeTextureB);
                    WaveShader.Dispatch(ApplyWaveKernel, (int)Mathf.CeilToInt((float)VertexCount / (float)256), 1, 1);
                }

                {//Calculate Normals
                    WaveShader.SetInt("gVertexCount", IndexBuffer.count / 3); 
                    WaveShader.SetBuffer(NormalCalcKernel, "bufVertices", VertexBuffer);
                    WaveShader.SetTexture(NormalCalcKernel, "WaveTexA", ComputeTextureC);
                    WaveShader.SetBuffer(NormalCalcKernel, "IndexBuffer", IndexBuffer);
                    WaveShader.Dispatch(NormalCalcKernel, (int)Mathf.CeilToInt((float)(IndexBuffer.count / 3) / (float)256), 1, 1);
                }

                {//Blur Normals
                    for(int i = 0; i < BlurPasses; i++) {
                        int AA2 = i % 2;
                        WaveShader.SetTexture(BlurNormsKernel, "WaveTexA", AA2 == 0 ? ComputeTextureD : ComputeTextureC);
                        WaveShader.SetTexture(BlurNormsKernel, "WaveTexB", AA2 == 1 ? ComputeTextureD : ComputeTextureC);
                        WaveShader.Dispatch(BlurNormsKernel, Mathf.CeilToInt((float)TextureDimensions / 16.0f), Mathf.CeilToInt((float)TextureDimensions / 16.0f), 1);
                    }
                    if(BlurPasses % 2 == 1) Graphics.CopyTexture(ComputeTextureD, 0, 0, ComputeTextureC, 0, 0);
                }

                {//Apply Normals
                    WaveShader.SetInt("gVertexCount", VertexCount); 
                    WaveShader.SetBuffer(ApplyNormsKernel, "bufVertices", VertexBuffer);
                    WaveShader.SetTexture(ApplyNormsKernel, "WaveTexB", ComputeTextureC);
                    WaveShader.Dispatch(ApplyNormsKernel, (int)Mathf.CeilToInt((float)VertexCount / (float)256), 1, 1);
                }



                PrevTime = TimeInterval;

                IntIter++;
            }
        }
    }
    public void OnDrawGizmos() {
        if(ReInitialize) {
            ReInitialize = false;
        }
        // Gizmos.color = Color.green;
        // if(TargetMesh != null) {
        //     Gizmos.DrawWireCube(TargetMesh.bounds.center, TargetMesh.bounds.extents * 2.0f);
        // }
        // if(VoxelizedBounds != null) {
        //     Vector3 Iterator = (TargetMesh.bounds.max - TargetMesh.bounds.min);
        //     Iterator.x /= (float)(TextureDimensions);
        //     Iterator.z /= (float)(TextureDimensions);
        //     Vector3 MinAABB = TargetMesh.bounds.min;
        //     Vector3 MaxAABB = TargetMesh.bounds.max;
        //     for(int i = 0; i < TextureDimensions - 1; i++) {
        //         for(int j = 0; j < TextureDimensions - 1; j++) {
        //             int Index = i + j * TextureDimensions;
        //             if(VoxelizedBounds[Index].x == 1) {
        //                 Vector3 Min = MinAABB + new Vector3(i * Iterator.x, 0, j * Iterator.z);
        //                 Vector3 Max = MinAABB + new Vector3((i + 1) * Iterator.x, 0, (j + 1) * Iterator.z);
        //                 Gizmos.DrawWireCube((Max + Min) / 2.0f, Max - Min);
        //             }

        //         }
        //     }

        // }
    }
}

#if UNITY_EDITOR


    [CustomEditor(typeof(WaveV2))]
    public class WaveV2Editor : Editor
    {
        private VisualElement CreateVerticalBox(string Name) {
            VisualElement VertBox = new VisualElement();
            // VertBox.style.flexDirection = FlexDirection.Row;
            return VertBox;
        }

        private VisualElement CreateHorizontalBox(string Name) {
            VisualElement HorizBox = new VisualElement();
            HorizBox.style.flexDirection = FlexDirection.Row;
            return HorizBox;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var t1 = (targets);
            var t =  t1[0] as WaveV2;
            VisualElement MainContainer = CreateVerticalBox("Main Container");
                VisualElement RealWaveContainer = CreateVerticalBox("Real Wave Container");
                    Button ReinitializeButton = new Button(() => {
                        t.OnDestroy();
                        t.OnEnable();
                    }) {text = "Reinitialize"};
                    Toggle RunToggle = new Toggle() {value = !t.Run, text = "Pause Water"};
                        RunToggle.RegisterValueChangedCallback(evt => {t.Run = !evt.newValue;});

                    VisualElement WaterDimensionContainer = CreateHorizontalBox("Water Dimension Container");
                        Label WaterDimensionLabel = new Label("Water Plane Dimensions: ");
                            WaterDimensionLabel.style.width = 150;                      
                        IntegerField WaterDimensionField = new IntegerField() {value = t.TextureDimensions};
                            WaterDimensionField.RegisterValueChangedCallback(evt => {t.TextureDimensions = evt.newValue;});
                    WaterDimensionContainer.Add(WaterDimensionLabel); 
                    WaterDimensionContainer.Add(WaterDimensionField); 

                    VisualElement BlurPassesContainer = CreateHorizontalBox("Blur Pass Container");
                        Label BlurPassesLabel = new Label("Normal Blur Passes: ");
                            BlurPassesLabel.style.width = 150;                      
                        IntegerField BlurPassesField = new IntegerField() {value = t.BlurPasses};
                            BlurPassesField.RegisterValueChangedCallback(evt => {t.BlurPasses = evt.newValue;});
                    BlurPassesContainer.Add(BlurPassesLabel); 
                    BlurPassesContainer.Add(BlurPassesField); 
                    VisualElement SimPassesContainer = CreateHorizontalBox("Simulation Passes Container");
                        Label SimPassesLabel = new Label("Simulation Passes: ");
                            SimPassesLabel.style.width = 150;                      
                        IntegerField SimPassesField = new IntegerField() {value = t.SimulationPasses};
                            SimPassesField.RegisterValueChangedCallback(evt => {t.SimulationPasses = evt.newValue;});
                    SimPassesContainer.Add(SimPassesLabel); 
                    SimPassesContainer.Add(SimPassesField); 

                    Foldout RealWaterFoldout = new Foldout() {text = "Real Water"};
                        Toggle RealWaterToggle = new Toggle() {value = t.DoRealWater, text = "Enable Real Waves"};
                            RealWaterToggle.RegisterValueChangedCallback(evt => {t.DoRealWater = evt.newValue;});
                        VisualElement RealWaveDamperContainer = CreateHorizontalBox("Real Wave Damper Container");
                            Label RealWaveDamperLabel = new Label("Real Wave Dampening: ");
                                RealWaveDamperLabel.style.width = 150;
                            Slider RealWaveDamperSlider = new Slider() {value = t.WaveDamper, highValue = 0.9999f, lowValue = 0.9f};
                                RealWaveDamperSlider.style.width = 100;
                            FloatField RealWaveDamperField = new FloatField() {value = t.WaveDamper};
                                RealWaveDamperSlider.RegisterValueChangedCallback(evt => {t.WaveDamper = evt.newValue; RealWaveDamperField.value = evt.newValue;});
                                RealWaveDamperField.RegisterValueChangedCallback(evt => {t.WaveDamper = evt.newValue; RealWaveDamperSlider.value = evt.newValue;});
                        RealWaveDamperContainer.Add(RealWaveDamperLabel);
                        RealWaveDamperContainer.Add(RealWaveDamperSlider);
                        RealWaveDamperContainer.Add(RealWaveDamperField);
                        VisualElement RealWaveDeltaContainer = CreateHorizontalBox("Real Wave Delta Container");
                            Label RealWaveDeltaLabel = new Label("Real Wave Delta: ");
                                RealWaveDeltaLabel.style.width = 150;
                            Slider RealWaveDeltaSlider = new Slider() {value = t.WaveDelta, highValue = 1.25f, lowValue = 0.0f};
                                RealWaveDeltaSlider.style.width = 100;
                            FloatField RealWaveDeltaField = new FloatField() {value = t.WaveDelta};
                                RealWaveDeltaSlider.RegisterValueChangedCallback(evt => {t.WaveDelta = evt.newValue; RealWaveDeltaField.value = evt.newValue;});
                                RealWaveDeltaField.RegisterValueChangedCallback(evt => {t.WaveDelta = evt.newValue; RealWaveDeltaSlider.value = evt.newValue;});
                        RealWaveDeltaContainer.Add(RealWaveDeltaLabel);
                        RealWaveDeltaContainer.Add(RealWaveDeltaSlider);
                        RealWaveDeltaContainer.Add(RealWaveDeltaField);
                    RealWaterFoldout.Add(RealWaterToggle);
                    RealWaterFoldout.Add(RealWaveDamperContainer);
                    RealWaterFoldout.Add(RealWaveDeltaContainer);

                RealWaveContainer.Add(ReinitializeButton);
                RealWaveContainer.Add(RunToggle);
                RealWaveContainer.Add(WaterDimensionContainer);
                RealWaveContainer.Add(BlurPassesContainer);
                RealWaveContainer.Add(SimPassesContainer);
                RealWaveContainer.Add(RealWaterFoldout);


                VisualElement FakeWaveContainer = CreateVerticalBox("Fake Wave Container");
                    Foldout FakeWaterFoldout = new Foldout() {text = "Fake Water"};
                        Toggle FakeWaterToggle = new Toggle() {value = t.DoFakeWaves, text = "Enable Fake Waves"};
                            FakeWaterToggle.RegisterValueChangedCallback(evt => {t.DoFakeWaves = evt.newValue;});
                        Toggle FakeWaterVertexNormalsToggle = new Toggle() {value = t.UseVertexNormals, text = "Use Vertex Normals"};
                            FakeWaterVertexNormalsToggle.RegisterValueChangedCallback(evt => {t.UseVertexNormals = evt.newValue;});
                        VisualElement FakeWaterIntensityContainer = CreateHorizontalBox("Fake Water Intensity Container");
                            Label FakeWaterIntensityLabel = new Label("Fake Water Intensity: ");
                                FakeWaterIntensityLabel.style.width = 150;
                            FloatField FakeWaterIntensityField = new FloatField() {value = t.FakeWaterIntensity};
                                FakeWaterIntensityField.RegisterValueChangedCallback(evt => {t.FakeWaterIntensity = evt.newValue;});
                        FakeWaterIntensityContainer.Add(FakeWaterIntensityLabel);
                        FakeWaterIntensityContainer.Add(FakeWaterIntensityField);
                        VisualElement FakeWaterTilingContainer = CreateHorizontalBox("Fake Water Tiling Container");
                            Label FakeWaterTilingLabel = new Label("Fake Water Tiling: ");
                                FakeWaterTilingLabel.style.width = 150;
                            FloatField FakeWaterTilingField = new FloatField() {value = t.FakeWaterTiling};
                                FakeWaterTilingField.RegisterValueChangedCallback(evt => {t.FakeWaterTiling = evt.newValue;});
                        FakeWaterTilingContainer.Add(FakeWaterTilingLabel);
                        FakeWaterTilingContainer.Add(FakeWaterTilingField);

                    FakeWaterFoldout.Add(FakeWaterToggle);
                    FakeWaterFoldout.Add(FakeWaterVertexNormalsToggle);
                    FakeWaterFoldout.Add(FakeWaterIntensityContainer);
                    FakeWaterFoldout.Add(FakeWaterTilingContainer);
                FakeWaveContainer.Add(FakeWaterFoldout);
                VisualElement DebugContainer = CreateVerticalBox("Debug Container");
                    ObjectField DebugImageFieldA = new ObjectField();
                        DebugImageFieldA.objectType = typeof(Texture);
                        DebugImageFieldA.value = t.ComputeTextureA;
                DebugContainer.Add(DebugImageFieldA);

            MainContainer.Add(RealWaveContainer);
            MainContainer.Add(FakeWaveContainer);
            MainContainer.Add(DebugContainer);

            return MainContainer;
        }
    }

#endif
}