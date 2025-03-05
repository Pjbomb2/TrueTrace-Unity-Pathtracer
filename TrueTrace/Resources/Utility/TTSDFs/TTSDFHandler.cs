using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using CommonVars;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif


namespace TrueTrace {
    [ExecuteInEditMode][System.Serializable][RequireComponent(typeof(MeshRenderer))]
    public class TTSDFHandler : MonoBehaviour
    {
        public enum SDFShapes {Sphere, Box};
        public enum SDFOperations {Union, Subtraction, Xor, Intersection};

        [System.Serializable]
        public struct SDFVar {
            public Vector3 A;
            public Vector3 B;
            public int Type;
            public int Operation;
            public float Smoothness;
            public Quaternion Transform;

            public SDFVar(SDFVar A2) {
                this = A2;
            }
        }
        [SerializeField] public SDFVar[] SDFs;
        [SerializeField] public bool IntersectBackFacing = false;
        [SerializeField] public int MaxStepCount = 1000;
        [SerializeField] public float MinValidSize = 0.01f;
        [SerializeField] public float FramesSinceStart = 0.0f;
        
        public ComputeShader GenShader;
        public ComputeBuffer SDFBuffer;
        public MeshRenderer Rend;
        private Material SDFMat;

        private int GenKernel;

        public void Start() {
            Init();
            RayTracingMaster.OptionalSDFHandler = this;
            // if(Rend == null) {
            //     if(TryGetComponent<MeshRenderer>(out Rend)) {
            //         // Rend.enabled = false;
            //     }
            // }

        }

        public void Init() {
            if(SDFs != null) {
                CommonFunctions.CreateComputeBuffer<SDFVar>(ref SDFBuffer, SDFs);
            }        
            if(SDFMat == null) {
                SDFMat = new Material(Shader.Find("Unlit/TTSDFShader"));
                if(TryGetComponent<MeshRenderer>(out Rend)) {
                    Rend.materials = new Material[] {SDFMat};
                }
            }    
            if(GenShader == null) {GenShader = Resources.Load<ComputeShader>("Utility/TTSDFs/TTSDFRayGen"); }
            GenKernel = GenShader.FindKernel("Generate");
        }

        public void Run(CommandBuffer cmd, RenderTexture _RandomNums, ComputeBuffer _RayBuffer, int SourceWidth, int SourceHeight) {
            FramesSinceStart++;
            if(GenShader != null && SDFs != null) {
                CommonFunctions.CreateComputeBuffer<SDFVar>(ref SDFBuffer, SDFs);
                SDFMat.SetInteger("SDFCount", SDFs.Length);
                SDFMat.SetBuffer("SDFs", SDFBuffer);
                SDFMat.SetVector("Scale", transform.localScale);

                AssetManager.Assets.SetMeshTraceBuffers(GenShader, GenKernel);
                GenShader.SetTexture(GenKernel, "RandomNums", _RandomNums);
                GenShader.SetComputeBuffer(GenKernel, "GlobalRays", _RayBuffer);

                GenShader.SetComputeBuffer(GenKernel, "SDFs", SDFBuffer);
                GenShader.SetInt("SDFCount", SDFBuffer.count);
                GenShader.SetBool("DoBackfacing", IntersectBackFacing);
                GenShader.SetFloat("Scale", Mathf.Max(Mathf.Max(transform.localScale.x, transform.localScale.y), transform.localScale.z));
                GenShader.SetInt("MaxStepCount", MaxStepCount);
                GenShader.SetFloat("MinStepSize", MinValidSize);
                GenShader.SetFloat("CustFramesSinceStart", FramesSinceStart);

                cmd.BeginSample("Primary Ray Generation");
                    cmd.DispatchCompute(GenShader, GenKernel, Mathf.CeilToInt(SourceWidth / 16.0f), Mathf.CeilToInt(SourceHeight / 16.0f), 1);
                cmd.EndSample("Primary Ray Generation");
            }
        }
        public void OnEnable() {
            if(SDFs == null) {
                SDFs = new SDFVar[1];
                SDFs[0].Transform = Quaternion.identity;
                SDFs[0].B = Vector3.one;

            }
            this.transform.localScale = new Vector3(2048.0f, 2048.0f, 2048.0f);
            this.transform.position = Vector3.zero;
            SDFMat = new Material(Shader.Find("Unlit/TTSDFShader"));
            if(TryGetComponent<MeshRenderer>(out Rend)) {
                Rend.materials = new Material[] {SDFMat};
            }
        }
        public void UpdateDraw() {
            if(SDFs != null && SDFMat != null) {
                CommonFunctions.CreateComputeBuffer<SDFVar>(ref SDFBuffer, SDFs);
                SDFMat.SetInteger("SDFCount", SDFs.Length);
                SDFMat.SetBuffer("SDFs", SDFBuffer);
                SDFMat.SetVector("Scale", transform.localScale);
            }
        }

        public void OnDrawGizmos() {
            if(Application.isPlaying) {
                int Count = SDFs.Length;
                for(int i = 0; i < Count; i++) {
                    if(SDFs[i].Type == 0) {
                        Gizmos.DrawWireSphere(SDFs[i].A, SDFs[i].B.x);
                    } else {
                        Gizmos.DrawWireCube(SDFs[i].A, SDFs[i].B * 2.0f);
                    }
                }
            }
        }
    }

#if UNITY_EDITOR
    // A tiny custom editor for ExampleScript component
    [CustomEditor(typeof(TTSDFHandler))]
    public class TTSDFHandlerEditor : Editor
    {


        private VisualElement CreateHorizontalBox(string Name) {
            VisualElement HorizBox = new VisualElement();
            HorizBox.style.flexDirection = FlexDirection.Row;
            return HorizBox;
        }
        ScrollView MatHierarchyView;
        VisualElement SideWindow;

        private void Refresh(TTSDFHandler t) {
            MatHierarchyView.Clear();
            int Count = t.SDFs.Length;
            for(int i = 0; i < Count; i++) {
                int Selected = i;
                VisualElement SideBySideWindow = CreateHorizontalBox("SideBox " + i);
                    Button ThisButton = new Button(() => {
                        SideWindow.Clear();
                        Vector3Field CenterField = new Vector3Field("Position: ");
                        CenterField.value = t.SDFs[Selected].A;
                        CenterField.RegisterValueChangedCallback(evt => {t.SDFs[Selected].A = evt.newValue;t.UpdateDraw();});

                        Vector3Field ScaleField = new Vector3Field("Scale: ");
                        ScaleField.value = t.SDFs[Selected].B;
                        ScaleField.RegisterValueChangedCallback(evt => {t.SDFs[Selected].B = evt.newValue;t.UpdateDraw();});

                        EnumField ShapeField = new EnumField("SDF Type: ", (TTSDFHandler.SDFShapes)t.SDFs[Selected].Type);
                        ShapeField.RegisterValueChangedCallback(evt => {t.SDFs[Selected].Type = (int)(TTSDFHandler.SDFShapes)(evt.newValue);t.UpdateDraw();});

                        EnumField OpField = new EnumField("SDF Operation: ", (TTSDFHandler.SDFOperations)t.SDFs[Selected].Operation);
                        OpField.RegisterValueChangedCallback(evt => {t.SDFs[Selected].Operation = (int)(TTSDFHandler.SDFOperations)(evt.newValue); t.UpdateDraw();});


                        Slider SmoothnessSlider = new Slider() {label = "Smoothness: ", value = t.SDFs[Selected].Smoothness, highValue = 1.0f, lowValue = 0.0f};
                        SmoothnessSlider.showInputField = true;        
                        SmoothnessSlider.style.width = 200;
                        SmoothnessSlider.ElementAt(0).style.minWidth = 65;
                        SmoothnessSlider.RegisterValueChangedCallback(evt => {t.SDFs[Selected].Smoothness = evt.newValue;});

                        Button ResetRotationButton = new Button(() => {
                            t.SDFs[Selected].Transform = Quaternion.identity;
                        });
                        ResetRotationButton.text = "Reset Rotation";

                        SideWindow.Add(CenterField);
                        SideWindow.Add(ScaleField);
                        SideWindow.Add(ShapeField);
                        SideWindow.Add(OpField);
                        SideWindow.Add(SmoothnessSlider);
                        SideWindow.Add(ResetRotationButton);
                    });

                    ThisButton.text = "SDF " + i;

                    Button DeleteButton = new Button(() => {
                        SideWindow.Clear();
                        List<TTSDFHandler.SDFVar> Temp = new List<TTSDFHandler.SDFVar>(t.SDFs);
                        Temp.RemoveAt(Selected);
                        t.SDFs = Temp.ToArray();  
                        Refresh(t);
                    });
                    DeleteButton.text = "Delete";

                    Button ShiftUpButton = new Button(() => {
                        SideWindow.Clear();
                        if(Selected != 0) {
                            TTSDFHandler.SDFVar Temp = new TTSDFHandler.SDFVar(t.SDFs[Selected]);
                            t.SDFs[Selected] = t.SDFs[Selected - 1];
                            t.SDFs[Selected - 1] = Temp;
                            Refresh(t);
                        }
                    });
                    ShiftUpButton.text = "Shift Up";

                    Button ShiftDownButton = new Button(() => {
                        SideWindow.Clear();
                        if(Selected <= Count - 2) {
                            TTSDFHandler.SDFVar Temp = new TTSDFHandler.SDFVar(t.SDFs[Selected]);
                            t.SDFs[Selected] = t.SDFs[Selected + 1];
                            t.SDFs[Selected + 1] = Temp;
                            Refresh(t);
                        }
                    });
                    ShiftDownButton.text = "Shift Down";


                SideBySideWindow.Add(ThisButton);
                SideBySideWindow.Add(DeleteButton);
                SideBySideWindow.Add(ShiftUpButton);
                SideBySideWindow.Add(ShiftDownButton);

                    
                MatHierarchyView.Add(SideBySideWindow);
            }

            Button AddButton = new Button(() => {
                List<TTSDFHandler.SDFVar> Temp = new List<TTSDFHandler.SDFVar>(t.SDFs);
                Temp.Add(new TTSDFHandler.SDFVar() {
                    Transform = Quaternion.identity,
                    B = Vector3.one
                });
                t.SDFs = Temp.ToArray();
                Refresh(t);

            });
            AddButton.text = "Add SDF";
            MatHierarchyView.Add(AddButton);

        }

        // int Selected = 0;
        public override VisualElement CreateInspectorGUI() {
            VisualElement rootVisualElement = new VisualElement();


            TwoPaneSplitView NewView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);

            NewView.style.minHeight = 1000;

            MatHierarchyView = new ScrollView();

            SideWindow = new VisualElement();
            var t = target as TTSDFHandler;
            int Count = t.SDFs.Length;
            for(int i = 0; i < Count; i++) {
                int Selected = i;
                VisualElement SideBySideWindow = CreateHorizontalBox("SideBox " + i);
                    Button ThisButton = new Button(() => {
                        SideWindow.Clear();
                        Vector3Field CenterField = new Vector3Field("Position: ");
                        CenterField.value = t.SDFs[Selected].A;
                        CenterField.RegisterValueChangedCallback(evt => {t.SDFs[Selected].A = evt.newValue;t.UpdateDraw();});

                        Vector3Field ScaleField = new Vector3Field("Scale: ");
                        ScaleField.value = t.SDFs[Selected].B;
                        ScaleField.RegisterValueChangedCallback(evt => {t.SDFs[Selected].B = evt.newValue;t.UpdateDraw();});

                        EnumField ShapeField = new EnumField("SDF Type: ", (TTSDFHandler.SDFShapes)t.SDFs[Selected].Type);
                        ShapeField.RegisterValueChangedCallback(evt => {t.SDFs[Selected].Type = (int)(TTSDFHandler.SDFShapes)(evt.newValue);t.UpdateDraw();});

                        EnumField OpField = new EnumField("SDF Operation: ", (TTSDFHandler.SDFOperations)t.SDFs[Selected].Operation);
                        OpField.RegisterValueChangedCallback(evt => {t.SDFs[Selected].Operation = (int)(TTSDFHandler.SDFOperations)(evt.newValue); t.UpdateDraw();});

                        Slider SmoothnessSlider = new Slider() {label = "Smoothness: ", value = t.SDFs[Selected].Smoothness, highValue = 1.0f, lowValue = 0.0f};
                        SmoothnessSlider.showInputField = true;        
                        SmoothnessSlider.style.width = 200;
                        SmoothnessSlider.ElementAt(0).style.minWidth = 65;
                        SmoothnessSlider.RegisterValueChangedCallback(evt => {t.SDFs[Selected].Smoothness = evt.newValue;});

                        Button ResetRotationButton = new Button(() => {
                            t.SDFs[Selected].Transform = Quaternion.identity;
                        });
                        ResetRotationButton.text = "Reset Rotation";

                        SideWindow.Add(CenterField);
                        SideWindow.Add(ScaleField);
                        SideWindow.Add(ShapeField);
                        SideWindow.Add(OpField);
                        SideWindow.Add(SmoothnessSlider);
                        SideWindow.Add(ResetRotationButton);
                    });

                    ThisButton.text = "SDF " + i;

                    Button DeleteButton = new Button(() => {
                        SideWindow.Clear();
                        List<TTSDFHandler.SDFVar> Temp = new List<TTSDFHandler.SDFVar>(t.SDFs);
                        Temp.RemoveAt(Selected);
                        t.SDFs = Temp.ToArray();  
                        Refresh(t);
                    });
                    DeleteButton.text = "Delete";

                    Button ShiftUpButton = new Button(() => {
                        SideWindow.Clear();
                        if(Selected != 0) {
                            TTSDFHandler.SDFVar Temp = new TTSDFHandler.SDFVar(t.SDFs[Selected]);
                            t.SDFs[Selected] = t.SDFs[Selected - 1];
                            t.SDFs[Selected - 1] = Temp;
                        }
                        Refresh(t);
                    });
                    ShiftUpButton.text = "Shift Up";

                    Button ShiftDownButton = new Button(() => {
                        SideWindow.Clear();
                        if(Selected <= Count - 2) {
                            TTSDFHandler.SDFVar Temp = new TTSDFHandler.SDFVar(t.SDFs[Selected]);
                            t.SDFs[Selected] = t.SDFs[Selected + 1];
                            t.SDFs[Selected + 1] = Temp;
                        }
                        Refresh(t);
                    });
                    ShiftDownButton.text = "Shift Down";


                SideBySideWindow.Add(ThisButton);
                SideBySideWindow.Add(DeleteButton);
                SideBySideWindow.Add(ShiftUpButton);
                SideBySideWindow.Add(ShiftDownButton);

                    
                MatHierarchyView.Add(SideBySideWindow);
            }

            Button AddButton = new Button(() => {
                List<TTSDFHandler.SDFVar> Temp = new List<TTSDFHandler.SDFVar>(t.SDFs);
                Temp.Add(new TTSDFHandler.SDFVar() {
                    Transform = Quaternion.identity,
                    B = Vector3.one
                });
                t.SDFs = Temp.ToArray();
                Refresh(t);

            });
            AddButton.text = "Add SDF";
            MatHierarchyView.Add(AddButton);


            NewView.Add(MatHierarchyView);
            NewView.Add(SideWindow);




            Button ResetSphereButton = new Button(() => {
                t.FramesSinceStart = 0.0f;
            });
            ResetSphereButton.text = "Reset Sphere";


            rootVisualElement.Add(ResetSphereButton);
            rootVisualElement.Add(NewView);
            Toggle IntersectBackFacingToggle = new Toggle() {value = t.IntersectBackFacing, text = "Do Backfacing"};
            IntersectBackFacingToggle.RegisterValueChangedCallback(evt => {t.IntersectBackFacing = evt.newValue;});
            rootVisualElement.Add(IntersectBackFacingToggle);

            IntegerField MaxStepCountField = new IntegerField() {value = t.MaxStepCount, label = "Max Step Count"};
            MaxStepCountField.RegisterValueChangedCallback(evt => {t.MaxStepCount = evt.newValue;});

            Slider MinStepSizeSlider = new Slider() {label = "Min Step Size: ", value = t.MinValidSize, highValue = 0.01f, lowValue = 0.00001f};
            MinStepSizeSlider.showInputField = true;        
            MinStepSizeSlider.style.width = 200;
            MinStepSizeSlider.ElementAt(0).style.minWidth = 65;
            MinStepSizeSlider.RegisterValueChangedCallback(evt => {t.MinValidSize = evt.newValue;});

            rootVisualElement.Add(MaxStepCountField);
            rootVisualElement.Add(MinStepSizeSlider);
            return rootVisualElement;

        }
        // Custom in-scene UI for when ExampleScript
        // component is selected.
        public void OnSceneGUI()
        {
            var t = target as TTSDFHandler;
            int Count = t.SDFs.Length;
            Transform TempTransform = t.transform;
            for(int i = 0; i < Count; i++) {
                t.SDFs[i].Transform = Quaternion.Inverse(Handles.RotationHandle(Quaternion.Inverse(t.SDFs[i].Transform), t.SDFs[i].A));
                
                t.SDFs[i].A = Handles.PositionHandle(t.SDFs[i].A + t.transform.position, Quaternion.identity);
                t.SDFs[i].B = Handles.ScaleHandle(t.SDFs[i].B, t.SDFs[i].A, Quaternion.identity, 1.0f);      
                t.SDFs[i].A -= t.transform.position;              
            }
            t.UpdateDraw();

        }
    }
#endif


}