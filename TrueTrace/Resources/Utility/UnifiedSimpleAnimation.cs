using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace TrueTrace {
    public class UnifiedSimpleAnimation : MonoBehaviour
    {
        public enum Options {X, Y, Z};
        [SerializeField] public bool RunTransforms = true;
        [SerializeField] public bool HasUpdatedToRun = false;
        [SerializeField] public Vector3 InitialPosition;
        [SerializeField] public Vector3 Forward;

        [SerializeField] public bool DoRotation = false;
        [SerializeField] public bool RotationRespectsTransform = false;
        [SerializeField] public float RotationSpeed = 32.0f;
        [SerializeField] public Options RotationAxis = Options.Y;

        [SerializeField] public bool DoTranslation = false;
        [SerializeField] public bool TranslationRespectsTransform = false;
        [SerializeField] public float TranslationSpeed = 1.0f;
        [SerializeField] public float TranslationDistance = 1.0f;
        [SerializeField] public Options TranslationAxis = Options.X;


        private float TimeSinceStart;
        public void Start() {
            TimeSinceStart = 0;
            InitialPosition = transform.position;
            Forward = transform.forward;
        }
        public void Update() {
            if(RunTransforms != HasUpdatedToRun) {
                if(!RunTransforms) {
                    transform.position = InitialPosition;
                    transform.forward = Forward;
                } else {
                    TimeSinceStart = 0;
                    InitialPosition = transform.position;
                    Forward = transform.forward;
                }
            } else {
                if(RunTransforms) {
                    if(DoRotation) {
                        if(RotationRespectsTransform) transform.Rotate((RotationAxis == Options.Z ? Vector3.forward : (RotationAxis == Options.Y ? Vector3.up : Vector3.right)) * (RotationSpeed * Time.deltaTime));
                        else transform.Rotate((RotationAxis == Options.Z ? new Vector3(0,0,1) : (RotationAxis == Options.Y ? new Vector3(0,1,0) : new Vector3(1,0,0))) * (RotationSpeed * Time.deltaTime), Space.World);
                    }
                    if(DoTranslation) {//Translation
                        if(TranslationRespectsTransform) transform.position = InitialPosition + ((TranslationAxis == Options.X ? transform.right : (TranslationAxis == Options.Y ? transform.up : transform.forward)) * TranslationDistance * 4.0f * Mathf.Cos((TimeSinceStart += (TranslationSpeed * Time.deltaTime))));
                        else transform.position = InitialPosition + ((TranslationAxis == Options.X ? new Vector3(1,0,0) : (TranslationAxis == Options.Y ? new Vector3(0,1,0) : new Vector3(0,0,1))) * TranslationDistance * 4.0f * Mathf.Cos((TimeSinceStart += (TranslationSpeed * Time.deltaTime))));
                    }
                }
            }
            HasUpdatedToRun = RunTransforms;
        }

        #if UNITY_EDITOR
            public void OnDrawGizmosSelected() {
                Vector3 Offset;
                if(TranslationRespectsTransform) Offset = ((TranslationAxis == Options.X ? transform.right : (TranslationAxis == Options.Y ? transform.up : transform.forward)) * TranslationDistance * 4.0f);
                else Offset = ((TranslationAxis == Options.X ? new Vector3(1,0,0) : (TranslationAxis == Options.Y ? new Vector3(0,1,0) : new Vector3(0,0,1))) * TranslationDistance * 4.0f);
                Gizmos.DrawLine(InitialPosition + Offset, InitialPosition - Offset);
            }
        #endif
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(UnifiedSimpleAnimation))]
    public class UnifiedSimpleAnimationEditor : Editor
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

        public override VisualElement CreateInspectorGUI() {
            var t1 = (targets);
            var t =  t1[0] as UnifiedSimpleAnimation;
            VisualElement MainContainer = CreateVerticalBox("Main Container");
                Toggle RunTransformsToggle = new Toggle() {value = t.RunTransforms, text = "Run Animations"};
                    RunTransformsToggle.RegisterValueChangedCallback(evt => {t.RunTransforms = evt.newValue;});

                
                Foldout RotationFoldout = new Foldout() {text = "Rotation"};
                    Toggle DoRotationToggle = new Toggle() {value = t.DoRotation, text = "Run Rotation"};
                        DoRotationToggle.RegisterValueChangedCallback(evt => {t.DoRotation = evt.newValue;});
                    VisualElement RotationSpeedContainer = CreateHorizontalBox("Rotation Speed Container");
                        Label RotationSpeedLabel = new Label("Rotation Speed: ");
                            RotationSpeedLabel.style.width = 150;
                        FloatField RotationSpeedField = new FloatField() {value = t.RotationSpeed};
                            RotationSpeedField.RegisterValueChangedCallback(evt => {t.RotationSpeed = evt.newValue;});
                    RotationSpeedContainer.Add(RotationSpeedLabel);
                    RotationSpeedContainer.Add(RotationSpeedField);

                    Toggle RotationRespectsTransformToggle = new Toggle() {value = t.RotationRespectsTransform, text = "Rotation Respects Transform"};
                        RotationRespectsTransformToggle.RegisterValueChangedCallback(evt => {t.RotationRespectsTransform = evt.newValue;});
                    VisualElement RotationAxisContainer = CreateHorizontalBox("Rotation Axis Container");
                        Label RotationAxisLabel = new Label("Rotation Axis: ");
                            RotationAxisLabel.style.width = 150;
                        EnumField RotationAxisEnum = new EnumField(t.RotationAxis);
                            RotationAxisEnum.style.width = 35;
                            RotationAxisEnum.RegisterValueChangedCallback(evt => {t.RotationAxis = (UnifiedSimpleAnimation.Options)evt.newValue;});
                    RotationAxisContainer.Add(RotationAxisLabel);
                    RotationAxisContainer.Add(RotationAxisEnum);
                RotationFoldout.Add(DoRotationToggle);
                RotationFoldout.Add(RotationRespectsTransformToggle);
                RotationFoldout.Add(RotationAxisContainer);
                RotationFoldout.Add(RotationSpeedContainer);

                Foldout TranslationFoldout = new Foldout() {text = "Translation"};
                    Toggle DoTranslationToggle = new Toggle() {value = t.DoTranslation, text = "Run Translation"};
                        DoTranslationToggle.RegisterValueChangedCallback(evt => {t.DoTranslation = evt.newValue;});
                    VisualElement TranslationSpeedContainer = CreateHorizontalBox("Translation Speed Container");
                        Label TranslationSpeedLabel = new Label("Translation Speed: ");
                            TranslationSpeedLabel.style.width = 150;
                        FloatField TranslationSpeedField = new FloatField() {value = t.TranslationSpeed};
                            TranslationSpeedField.RegisterValueChangedCallback(evt => {t.TranslationSpeed = evt.newValue;});
                    TranslationSpeedContainer.Add(TranslationSpeedLabel);
                    TranslationSpeedContainer.Add(TranslationSpeedField);

                    VisualElement TranslationDistanceContainer = CreateHorizontalBox("Translation Distance Container");
                        Label TranslationDistanceLabel = new Label("Translation Distance: ");
                            TranslationDistanceLabel.style.width = 150;
                        FloatField TranslationDistanceField = new FloatField() {value = t.TranslationDistance};
                            TranslationDistanceField.RegisterValueChangedCallback(evt => {t.TranslationDistance = evt.newValue;});
                    TranslationDistanceContainer.Add(TranslationDistanceLabel);
                    TranslationDistanceContainer.Add(TranslationDistanceField);

                    Toggle TranslationRespectsTransformToggle = new Toggle() {value = t.TranslationRespectsTransform, text = "Translation Respects Transform"};
                        TranslationRespectsTransformToggle.RegisterValueChangedCallback(evt => {t.TranslationRespectsTransform = evt.newValue;});
                    VisualElement TranslationAxisContainer = CreateHorizontalBox("Translation Axis Container");
                        Label TranslationAxisLabel = new Label("Translation Axis: ");
                            TranslationAxisLabel.style.width = 150;
                        EnumField TranslationAxisEnum = new EnumField(t.TranslationAxis);
                            TranslationAxisEnum.style.width = 35;
                            TranslationAxisEnum.RegisterValueChangedCallback(evt => {t.TranslationAxis = (UnifiedSimpleAnimation.Options)evt.newValue;});
                    TranslationAxisContainer.Add(TranslationAxisLabel);
                    TranslationAxisContainer.Add(TranslationAxisEnum);
                TranslationFoldout.Add(DoTranslationToggle);
                TranslationFoldout.Add(TranslationRespectsTransformToggle);
                TranslationFoldout.Add(TranslationAxisContainer);
                TranslationFoldout.Add(TranslationSpeedContainer);
                TranslationFoldout.Add(TranslationDistanceContainer);



            MainContainer.Add(RunTransformsToggle);
            MainContainer.Add(RotationFoldout);
            MainContainer.Add(TranslationFoldout);



            return MainContainer;
        }
    }
    #endif
}