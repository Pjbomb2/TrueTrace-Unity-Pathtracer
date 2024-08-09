#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CommonVars;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace TrueTrace {
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
}
#endif