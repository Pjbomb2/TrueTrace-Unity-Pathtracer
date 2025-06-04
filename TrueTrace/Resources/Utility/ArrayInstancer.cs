using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_EDITOR
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace TrueTrace {
    public class ArrayInstancer : MonoBehaviour
    {
        public GameObject TargetObject;
        public Vector2Int Resolution = new Vector2Int(1,1);
        public Vector2 Spacing = Vector2.one;
        public Vector3 ScaleRandomizeLower = Vector3.one;
        public Vector3 ScaleRandomizeUpper = Vector3.one;

        public Vector3 RotationRandomizeLower = Vector3.zero;
        public Vector3 RotationRandomizeUpper = Vector3.zero;


        public float DiskRadiusPositionRandomize = 0;
        public int Seed = 3223;
        public bool InstanceOverSurface = false;

        public void CreateInstancesARRAY() {
            Random.InitState(Seed);
            Vector3 BasePosition = this.transform.position;
            if(TargetObject != null) {
                for(int i = 0; i < Resolution.x; i++) {
                    for(int j = 0; j < Resolution.y; j++) {
                        Vector2 UnitRandom = Random.insideUnitCircle * DiskRadiusPositionRandomize;
                        GameObject TempOBJ = GameObject.Instantiate(TargetObject);
                        TempOBJ.transform.parent = TargetObject.transform.parent;

                        Vector3 position = BasePosition + new Vector3((float)i * Spacing.x + UnitRandom.x, 0, (float)j * Spacing.y + UnitRandom.y);
                    if(InstanceOverSurface) {
                        Ray ray = new Ray(position, new Vector3(0,-1,0));
                        RaycastHit hit = new RaycastHit();
                        if (Physics.Raycast(ray, out hit, 1000.0f)) {
                            TempOBJ.transform.position = hit.point;                        
                            TempOBJ.transform.eulerAngles = Quaternion.FromToRotation(Vector3.up, hit.normal).eulerAngles;
                        }
                    } else {
                        TempOBJ.transform.position = position;

                    }
                    TempOBJ.transform.eulerAngles += Vector3.Scale(new Vector3(Random.value * 2.0f - 1.0f, Random.value * 2.0f - 1.0f, Random.value * 2.0f - 1.0f), RotationRandomizeUpper - RotationRandomizeLower) + RotationRandomizeLower;
                    TempOBJ.transform.localScale = Vector3.Scale(new Vector3(Random.value, Random.value, Random.value), ScaleRandomizeUpper - ScaleRandomizeLower) + ScaleRandomizeLower;

                    }
                }
            }
        }

        public void OnDrawGizmos() {
            Random.InitState(Seed);
            Vector3 BasePosition = this.transform.position;
            Matrix4x4 PrevMat = Gizmos.matrix;
            Quaternion TempQuant = new Quaternion();
            for(int i = 0; i < Resolution.x; i++) {
                for(int j = 0; j < Resolution.y; j++) {
                    Vector2 UnitRandom = Random.insideUnitCircle * DiskRadiusPositionRandomize;
                    Vector3 position = BasePosition + new Vector3((float)i * Spacing.x + UnitRandom.x, 0, (float)j * Spacing.y + UnitRandom.y);
                    if(InstanceOverSurface) {
                        Ray ray = new Ray(position, new Vector3(0,-1,0));
                        RaycastHit hit = new RaycastHit();
                        if (Physics.Raycast(ray, out hit, 1000.0f)) {
                            Gizmos.matrix = PrevMat;
                            Gizmos.DrawLine(hit.point, position);
                            TempQuant = Quaternion.FromToRotation(transform.up, hit.normal);                        
                        }
                        TempQuant.eulerAngles = TempQuant.eulerAngles + Vector3.Scale(new Vector3(Random.value * 2.0f - 1.0f, Random.value * 2.0f - 1.0f, Random.value * 2.0f - 1.0f), RotationRandomizeUpper - RotationRandomizeLower) + RotationRandomizeLower;
                        Gizmos.matrix = Matrix4x4.TRS(hit.point, TempQuant, Vector3.one);
                        Gizmos.DrawCube(Vector3.zero, Vector3.Scale(new Vector3(Random.value, Random.value, Random.value), ScaleRandomizeUpper - ScaleRandomizeLower) + ScaleRandomizeLower);
                    } else {
                        TempQuant.eulerAngles = Quaternion.identity.eulerAngles + Vector3.Scale(new Vector3(Random.value * 2.0f - 1.0f, Random.value * 2.0f - 1.0f, Random.value * 2.0f - 1.0f), RotationRandomizeUpper - RotationRandomizeLower) + RotationRandomizeLower;
                        Gizmos.matrix = Matrix4x4.TRS(position, TempQuant, Vector3.one);
                        Gizmos.DrawCube(Vector3.zero, Vector3.Scale(new Vector3(Random.value, Random.value, Random.value), ScaleRandomizeUpper - ScaleRandomizeLower) + ScaleRandomizeLower);
                    }
                }
            }
            Gizmos.matrix = PrevMat;
        }

    }


    #if UNITY_EDITOR


    namespace TrueTrace {
        [CustomEditor(typeof(ArrayInstancer))]
        public class ArrayInstancerEditor : Editor
        {
            private VisualElement CreateVerticalBox(string Name) {
                VisualElement VertBox = new VisualElement();
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
                var t =  t1[0] as ArrayInstancer;
                VisualElement RootElement = CreateVerticalBox("Root Container");
                    Button InstanceToArrayButton = new Button(() => {
                        t.CreateInstancesARRAY();
                    }) {text = "Generate Instances(ARRAY)"};

                    VisualElement ObjectContainer = CreateHorizontalBox("Object Container");
                        Label TargetObjectLabel = new Label("Target Object: ");
                            TargetObjectLabel.style.width = 200;
                        ObjectField TargetObjectField = new ObjectField();
                            TargetObjectField.objectType = typeof(GameObject);
                            TargetObjectField.RegisterValueChangedCallback(evt => {t.TargetObject = (GameObject)evt.newValue;});
                    ObjectContainer.Add(TargetObjectLabel);
                    ObjectContainer.Add(TargetObjectField);

                    VisualElement ResolutionContainer = CreateHorizontalBox("Instance Resolution Container");
                        Label ResolutionLabel = new Label("Horizontal/Vertical Resolution: ");
                            ResolutionLabel.style.width = 200;
                        Vector2IntField ResolutionField = new Vector2IntField() {value = t.Resolution};
                            ResolutionField.RegisterValueChangedCallback(evt => {t.Resolution = evt.newValue;});
                    ResolutionContainer.Add(ResolutionLabel);
                    ResolutionContainer.Add(ResolutionField);

                    VisualElement SpacingContainer = CreateHorizontalBox("Instance Spacing Container");
                        Label SpacingLabel = new Label("Horizontal/Vertical Spacing: ");
                            SpacingLabel.style.width = 200;
                        Vector2Field SpacingField = new Vector2Field() {value = t.Spacing};
                            SpacingField.RegisterValueChangedCallback(evt => {t.Spacing = evt.newValue;});
                    SpacingContainer.Add(SpacingLabel);
                    SpacingContainer.Add(SpacingField);

                    VisualElement InstanceScaleContainer = CreateVerticalBox("Instace Scale Container");
                        VisualElement LowerScaleContainer = CreateHorizontalBox("Instance Lower Scale Container");
                            Label LowerScaleLabel = new Label("Scale Spacing(LowerBound): ");
                                LowerScaleLabel.style.width = 200;
                            Vector3Field LowerScaleField = new Vector3Field() {value = t.ScaleRandomizeLower};
                                LowerScaleField.RegisterValueChangedCallback(evt => {t.ScaleRandomizeLower = evt.newValue;});
                        LowerScaleContainer.Add(LowerScaleLabel);
                        LowerScaleContainer.Add(LowerScaleField);
                        VisualElement UpperScaleContainer = CreateHorizontalBox("Instance Upper Scale Container");
                            Label UpperScaleLabel = new Label("Scale Spacing(UpperBound): ");
                                UpperScaleLabel.style.width = 200;
                            Vector3Field UpperScaleField = new Vector3Field() {value = t.ScaleRandomizeUpper};
                                UpperScaleField.RegisterValueChangedCallback(evt => {t.ScaleRandomizeUpper = evt.newValue;});
                        UpperScaleContainer.Add(UpperScaleLabel);
                        UpperScaleContainer.Add(UpperScaleField);
                    InstanceScaleContainer.Add(LowerScaleContainer);
                    InstanceScaleContainer.Add(UpperScaleContainer);


                    VisualElement InstanceRotationContainer = CreateVerticalBox("Instace Rotation Container");
                        VisualElement LowerRotationContainer = CreateHorizontalBox("Instance Lower Rotation Container");
                            Label LowerRotationLabel = new Label("Rotation Spacing(LowerBound): ");
                                LowerRotationLabel.style.width = 200;
                            Vector3Field LowerRotationField = new Vector3Field() {value = t.RotationRandomizeLower};
                                LowerRotationField.RegisterValueChangedCallback(evt => {t.RotationRandomizeLower = evt.newValue;});
                        LowerRotationContainer.Add(LowerRotationLabel);
                        LowerRotationContainer.Add(LowerRotationField);
                        VisualElement UpperRotationContainer = CreateHorizontalBox("Instance Upper Rotation Container");
                            Label UpperRotationLabel = new Label("Rotation Spacing(UpperBound): ");
                                UpperRotationLabel.style.width = 200;
                            Vector3Field UpperRotationField = new Vector3Field() {value = t.RotationRandomizeUpper};
                                UpperRotationField.RegisterValueChangedCallback(evt => {t.RotationRandomizeUpper = evt.newValue;});
                        UpperRotationContainer.Add(UpperRotationLabel);
                        UpperRotationContainer.Add(UpperRotationField);
                    InstanceRotationContainer.Add(LowerRotationContainer);
                    InstanceRotationContainer.Add(UpperRotationContainer);

                    VisualElement DiskPositionRandomizationContainer = CreateHorizontalBox("Disk Position Randomization Container");
                        Label DiskPositionRandomizationLabel = new Label("Disk Randomization Radius: ");
                            DiskPositionRandomizationLabel.style.width = 200;
                        FloatField DiskPositionRandomizationField = new FloatField() {value = t.DiskRadiusPositionRandomize};
                            DiskPositionRandomizationField.RegisterValueChangedCallback(evt => {t.DiskRadiusPositionRandomize = evt.newValue;});
                    DiskPositionRandomizationContainer.Add(DiskPositionRandomizationLabel);
                    DiskPositionRandomizationContainer.Add(DiskPositionRandomizationField);

                    VisualElement SeedContainer = CreateHorizontalBox("Seed Container");
                        Label SeedLabel = new Label("Seed: ");
                            SeedLabel.style.width = 200;
                        IntegerField SeedField = new IntegerField() {value = t.Seed};
                            SeedField.RegisterValueChangedCallback(evt => {t.Seed = evt.newValue;});
                    SeedContainer.Add(SeedLabel);
                    SeedContainer.Add(SeedField);

                    VisualElement IOSContainer = CreateHorizontalBox("IOS Container");
                        Label IOSLabel = new Label("Instance Over Surface: ");
                            IOSLabel.style.width = 200;
                        Toggle IOSField = new Toggle() {value = t.InstanceOverSurface};
                            IOSField.RegisterValueChangedCallback(evt => {t.InstanceOverSurface = evt.newValue;});
                    IOSContainer.Add(IOSLabel);
                    IOSContainer.Add(IOSField);


                RootElement.Add(InstanceToArrayButton);
                RootElement.Add(ObjectContainer);
                {
                    VisualElement SpacerElement = new VisualElement();
                    SpacerElement.style.height = 10;
                    RootElement.Add(SpacerElement);
                }
                RootElement.Add(ResolutionContainer);
                {
                    VisualElement SpacerElement = new VisualElement();
                    SpacerElement.style.height = 10;
                    RootElement.Add(SpacerElement);
                }
                RootElement.Add(SpacingContainer);
                {
                    VisualElement SpacerElement = new VisualElement();
                    SpacerElement.style.height = 10;
                    RootElement.Add(SpacerElement);
                }
                RootElement.Add(InstanceScaleContainer);
                {
                    VisualElement SpacerElement = new VisualElement();
                    SpacerElement.style.height = 10;
                    RootElement.Add(SpacerElement);
                }
                RootElement.Add(InstanceRotationContainer);
                {
                    VisualElement SpacerElement = new VisualElement();
                    SpacerElement.style.height = 10;
                    RootElement.Add(SpacerElement);
                }
                RootElement.Add(DiskPositionRandomizationContainer);
                {
                    VisualElement SpacerElement = new VisualElement();
                    SpacerElement.style.height = 10;
                    RootElement.Add(SpacerElement);
                }
                RootElement.Add(SeedContainer);
                RootElement.Add(IOSContainer);
                return RootElement;
            }
        }
    }
#endif
}