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
    [ExecuteInEditMode][System.Serializable][RequireComponent(typeof(Light))]
    public class RayTracingLights : MonoBehaviour {
        public enum LightTypes {Point, Directional, Spot, AreaRect, AreaDisk};
        [HideInInspector] public Light ThisLight;
        [SerializeField] public LightData ThisLightData = new LightData();
        [HideInInspector] public int ArrayIndex;
        [HideInInspector] public int IESIndex;
        [SerializeField] [Range(0,40)] public float ShadowSoftness = 0.0f;
        [SerializeField] public bool UseKelvin = false;
        [SerializeField] public bool IsMainSun = false;
        [SerializeField] [Range(1000.0f,20000.0f)] public float KelvinTemperature = 1000.0f;
        [SerializeField] public float Intensity;
        [SerializeField] public Vector3 Col;
        [SerializeField] public Vector4 IESTexScaleOffset;
        [SerializeField] public float SpotAngle;
        [SerializeField] public bool HasInitialized = false;
        public Texture2D IESProfile;
        public bool NeedsToUpdate = false;
        Transform LocalTransform;
#if UNITY_PIPELINE_HDRP
        public UnityEngine.Rendering.HighDefinition.HDAdditionalLightData LightDat;
#endif

        public void Start() {
            ThisLight = this.GetComponent<Light>();
#if UNITY_PIPELINE_HDRP
            LightDat = this.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
            LightDat.includeForRayTracing = false;
#endif
            LocalTransform = transform;
            if(!HasInitialized) {
                Init();  
                HasInitialized = true;
            }
        }
        void Awake() {
            ThisLight = this.GetComponent<Light>();
#if UNITY_PIPELINE_HDRP
            LightDat = this.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
            LightDat.includeForRayTracing = false;
#endif
            // ThisLight.shadows = LightShadows.None;
            LocalTransform = transform;
            if(!HasInitialized) {
                Init();  
                HasInitialized = true;
            }
        }
        public void Init() {
            ThisLight = this.GetComponent<Light>();
#if UNITY_PIPELINE_HDRP
            LightDat = this.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
            LightDat.includeForRayTracing = false;
#endif
            ThisLight.useColorTemperature = true;
            UnityEngine.Rendering.GraphicsSettings.lightsUseLinearIntensity = true;
                ThisLightData.Position = transform.position;
                ThisLightData.Direction = (ThisLight.type == LightType.Directional) ? -transform.forward : (ThisLight.type == LightType.Spot) ? Vector3.Normalize(transform.forward) : transform.forward;
                ThisLightData.ZAxisRotation = transform.localEulerAngles.z * 3.14159f / 180.0f;
            Color TempCol;
            if(UseKelvin) TempCol = Mathf.CorrelatedColorTemperatureToRGB(KelvinTemperature);
            else TempCol = ThisLight.color;


            Intensity = ThisLight.intensity;
            Col = new Vector3(TempCol[0], TempCol[1], TempCol[2]);

            ThisLightData.Radiance = new Vector3(TempCol[0], TempCol[1], TempCol[2]) * Intensity;
#if UNITY_2021 || UNITY_2022 || !UNITY_PIPELINE_HDRP
            ThisLightData.Type = (ThisLight.type == LightType.Point) ? 0 : (ThisLight.type == LightType.Directional) ? 1 : (ThisLight.type == LightType.Spot) ? 2 : (ThisLight.type == LightType.Rectangle) ? 3 : 4;
#else
            ThisLightData.Type = (ThisLight.type == LightType.Point) ? 0 : (ThisLight.type == LightType.Directional) ? 1 : (ThisLight.type == LightType.Spot) ? 2 : (ThisLight.type == LightType.Rectangle || ThisLight.type == LightType.Box) ? 3 : 4;
#endif
            SpotAngle = ThisLight.spotAngle;
            if(ThisLight.type == LightType.Spot) {
                float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * ThisLight.innerSpotAngle);
                float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * ThisLight.spotAngle);
                float angleRangeInv = 1.0f / Mathf.Max(innerCos - outerCos, 0.001f);
                ThisLightData.SpotAngle = new Vector2(angleRangeInv, -outerCos * angleRangeInv);
                ThisLightData.IESTexScale = new Vector4(1,1,0,0);
#if UNITY_2021 || UNITY_2022 || !UNITY_PIPELINE_HDRP
            } else if(ThisLight.type == LightType.Rectangle) {
                #if UNITY_EDITOR
                    ThisLightData.SpotAngle = ThisLight.areaSize;
                #endif
#else            
            } else if(ThisLight.type == LightType.Rectangle || ThisLight.type == LightType.Box) {
                #if UNITY_EDITOR
                    #if UNITY_PIPELINE_HDRP
                        ThisLightData.SpotAngle = new Vector2(LightDat.shapeWidth, LightDat.shapeHeight);
                    #else
                        ThisLightData.SpotAngle = ThisLight.areaSize;
                    #endif
                #endif
#endif
            } else if(ThisLight.type == LightType.Disc) {
                #if UNITY_EDITOR
                    ThisLightData.SpotAngle = ThisLight.areaSize;
                #endif
            } else {
                ThisLightData.SpotAngle = Vector2.zero;
            }
            ThisLightData.Softness = ShadowSoftness;
            HasInitialized = true;
            CallHasUpdated(true);
        }

        private void OnEnable() { 
#if UNITY_PIPELINE_HDRP
            LightDat = this.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
            LightDat.includeForRayTracing = false;
#endif
            if(!HasInitialized) {
                Init();  
                HasInitialized = true;
            }
            if(!RayTracingMaster._rayTracingLights.Contains(this)) RayTracingMaster.RegisterObject(this);
        }

        private void OnDisable() {
            RayTracingMaster.UnregisterObject(this);
        }

        Vector3 NewDir;
        public bool CallHasUpdated(bool Override = false) {
            // NewDir = (ThisLightData.Type == 1) ? -LocalTransform.forward : LocalTransform.forward;
            // if(LocalTransform.position != ThisLightData.Position || NewDir != ThisLightData.Direction) Override = true;
            // if(NeedsToUpdate || Override) {
            if(NeedsToUpdate || LocalTransform.hasChanged || Override) {
                ThisLightData.Position = LocalTransform.position;
                // ThisLightData.Direction = NewDir;
                Vector3 TempColVec = Col;
                if(UseKelvin) {
                    Color TempCol = Mathf.CorrelatedColorTemperatureToRGB(KelvinTemperature);
                    TempColVec = new Vector3(TempCol.r, TempCol.g, TempCol.b);
                }
                ThisLightData.Radiance = TempColVec * Intensity;
                if(ThisLightData.Type == 2) {
                    float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * 1.0f);
                    float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * SpotAngle);
                    float angleRangeInv = 1.0f / Mathf.Max(innerCos - outerCos, 0.001f);
                    ThisLightData.SpotAngle = new Vector2(angleRangeInv, -outerCos * angleRangeInv);
                    ThisLightData.IESTexScale = IESTexScaleOffset;
                }
                ThisLightData.Direction = (ThisLightData.Type == 1) ? -LocalTransform.forward : LocalTransform.forward;
                ThisLightData.Softness = ShadowSoftness;
                ThisLightData.ZAxisRotation = LocalTransform.localEulerAngles.z * 3.14159f / 180.0f;
                NeedsToUpdate = false;
                LocalTransform.hasChanged = false;
                return true;
            } else return false;
        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(RayTracingLights)), CanEditMultipleObjects]
    public class RayTracingLightsEditor : Editor
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


        public class FloatSliderPair {
            public VisualElement DynamicContainer;
            public Label DynamicLabel;
            public Slider DynamicSlider;
            public FloatField DynamicField;
        }
        FloatSliderPair CreatePairedFloatSlider(string Name, float LowValue, float HighValue, ref float InitialValue, float SliderWidth = 200) {
            FloatSliderPair NewPair = new FloatSliderPair();
            NewPair.DynamicContainer = CreateHorizontalBox(Name + " Container");
            NewPair.DynamicLabel = new Label(Name);
            NewPair.DynamicSlider = new Slider() {value = InitialValue, highValue = HighValue, lowValue = LowValue};
            NewPair.DynamicSlider.value = InitialValue;
            NewPair.DynamicField = new FloatField() {value = InitialValue};
            NewPair.DynamicSlider.style.width = SliderWidth;
            NewPair.DynamicContainer.Add(NewPair.DynamicLabel);
            NewPair.DynamicContainer.Add(NewPair.DynamicSlider);
            NewPair.DynamicContainer.Add(NewPair.DynamicField);
            return NewPair;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var t1 = (targets);
            int TargCount = t1.Length;
            var t =  t1[0] as RayTracingLights;
            bool HasUpdated = false;
            if(!t.HasInitialized) t.Init();
        #if UNITY_PIPELINE_HDRP
            if(t.LightDat == null) t.Init();
        #endif
            VisualElement MainContainer = CreateVerticalBox("Main Container");

                List<string> LightTypeSettings = new List<string>();
                LightTypeSettings.Add("Point");
                LightTypeSettings.Add("Directional");
                LightTypeSettings.Add("Spot");
                LightTypeSettings.Add("Area(Rectangle)");
                LightTypeSettings.Add("Area(Disk)");
                PopupField<string> LightTypeField = new PopupField<string>("<b>Light Type</b>");
                LightTypeField.ElementAt(0).style.minWidth = 65;
                LightTypeField.choices = LightTypeSettings;
                LightTypeField.index = t.ThisLightData.Type;
                LightTypeField.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {
                    (t1[i] as RayTracingLights).ThisLightData.Type = LightTypeField.index; 
                    (t1[i] as RayTracingLights).NeedsToUpdate = true;
                    switch(LightTypeField.index) {
                        case 0:
                            (t1[i] as RayTracingLights).ThisLight.type = LightType.Point;
                        break;
                        case 1:
                            (t1[i] as RayTracingLights).ThisLight.type = LightType.Directional;
                        break;
                        case 2:
                            (t1[i] as RayTracingLights).ThisLight.type = LightType.Spot;
                        break;
                        case 3:
                            (t1[i] as RayTracingLights).ThisLight.type = LightType.Rectangle;
                        break;
                        default:
                            (t1[i] as RayTracingLights).ThisLight.type = LightType.Disc;
                        break;
                    }
                }});
                MainContainer.Add(LightTypeField);

                VisualElement LightColorContainer = CreateHorizontalBox("Light Color Container");
                    Label LightColorLabel = new Label("Light Color: ");
                    ColorField LightColorField = new ColorField();
                    LightColorField.value = new Color(t.Col.x, t.Col.y, t.Col.z, 1);
                    LightColorField.style.width = 200;
                    LightColorField.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).Col = new Vector3(evt.newValue.r, evt.newValue.g, evt.newValue.b); (t1[i] as RayTracingLights).NeedsToUpdate = true;}});
                    LightColorContainer.Add(LightColorLabel);
                    LightColorContainer.Add(LightColorField);
                MainContainer.Add(LightColorContainer);

                VisualElement LightIntensityContainer = CreateHorizontalBox("Light Intensity Container");
                    // Label LightIntensityLabel = new Label("Light Intensity: ");
                    FloatField LightIntensityField = new FloatField() {label = "Intensity: ", value = t.Intensity};
                    LightIntensityField.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).ThisLight.intensity = evt.newValue; (t1[i] as RayTracingLights).Intensity = evt.newValue; (t1[i] as RayTracingLights).NeedsToUpdate = true;}});
                    // LightIntensityContainer.Add(LightIntensityLabel);
                    LightIntensityContainer.Add(LightIntensityField);
                MainContainer.Add(LightIntensityContainer);


                if(t.ThisLightData.Type == 2) {
                    FloatSliderPair SpotAngleSliderPair = CreatePairedFloatSlider("Spot Angle: ", 1, 179.0f, ref t.SpotAngle);
                        SpotAngleSliderPair.DynamicSlider.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).ThisLight.spotAngle = evt.newValue; (t1[i] as RayTracingLights).SpotAngle = evt.newValue; (t1[i] as RayTracingLights).NeedsToUpdate = true;} SpotAngleSliderPair.DynamicField.value = t.SpotAngle;});
                        SpotAngleSliderPair.DynamicField.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).ThisLight.spotAngle = evt.newValue; (t1[i] as RayTracingLights).SpotAngle = evt.newValue; (t1[i] as RayTracingLights).NeedsToUpdate = true;} SpotAngleSliderPair.DynamicSlider.value = t.SpotAngle;});
                    MainContainer.Add(SpotAngleSliderPair.DynamicContainer);                    
                } else if(t.ThisLightData.Type == 3) {
                    #if UNITY_EDITOR
                        VisualElement LightWidthContainer = CreateHorizontalBox("Light Width Container");
                            // Label LightWidthLabel = new Label("Width: ");
                            FloatField LightWidthField = new FloatField() {label = "Width: ", value = t.ThisLightData.SpotAngle.x};
#if !(UNITY_2021 || UNITY_2022) && UNITY_PIPELINE_HDRP
                            LightWidthField.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).LightDat.shapeWidth = evt.newValue; (t1[i] as RayTracingLights).ThisLightData.SpotAngle.x = evt.newValue; (t1[i] as RayTracingLights).NeedsToUpdate = true;}});
#else
                            LightWidthField.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).ThisLight.areaSize = new Vector2(evt.newValue, (t1[i] as RayTracingLights).ThisLight.areaSize.y); (t1[i] as RayTracingLights).ThisLightData.SpotAngle.x = evt.newValue; (t1[i] as RayTracingLights).NeedsToUpdate = true;}});
#endif
                            // LightWidthContainer.Add(LightWidthLabel);
                            LightWidthContainer.Add(LightWidthField);
                        MainContainer.Add(LightWidthContainer);

                        VisualElement LightHeightContainer = CreateHorizontalBox("Light Height Container");
                            // Label LightHeightLabel = new Label("Height: ");
                            FloatField LightHeightField = new FloatField() {label = "Height: ", value = t.ThisLightData.SpotAngle.y};
#if !(UNITY_2021 || UNITY_2022) && UNITY_PIPELINE_HDRP
                            LightHeightField.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).LightDat.shapeHeight = evt.newValue; (t1[i] as RayTracingLights).ThisLightData.SpotAngle.y = evt.newValue; (t1[i] as RayTracingLights).NeedsToUpdate = true;}});
#else
                            LightHeightField.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).ThisLight.areaSize = new Vector2((t1[i] as RayTracingLights).ThisLight.areaSize.x, evt.newValue); (t1[i] as RayTracingLights).ThisLightData.SpotAngle.y = evt.newValue; (t1[i] as RayTracingLights).NeedsToUpdate = true;}});
#endif
                            // LightHeightContainer.Add(LightHeightLabel);
                            LightHeightContainer.Add(LightHeightField);
                        MainContainer.Add(LightHeightContainer);
                    #endif
                } else if(t.ThisLightData.Type == 4) {
                    #if UNITY_EDITOR
                        VisualElement LightRadiusContainer = CreateHorizontalBox("Light Radius Container");
                            Label LightRadiusLabel = new Label("Radius: ");
                            FloatField LightRadiusField = new FloatField() {value = t.ThisLightData.SpotAngle.x};
                            LightRadiusField.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).ThisLight.areaSize = new Vector2(evt.newValue, (t1[i] as RayTracingLights).ThisLight.areaSize.y); (t1[i] as RayTracingLights).ThisLightData.SpotAngle.x = evt.newValue; (t1[i] as RayTracingLights).NeedsToUpdate = true;}});
                            LightRadiusContainer.Add(LightRadiusLabel);
                            LightRadiusContainer.Add(LightRadiusField);
                        MainContainer.Add(LightRadiusContainer);
                    #endif
                }       


                FloatSliderPair ShadowSoftnessSliderPair = CreatePairedFloatSlider(t.ThisLightData.Type == 3 ? "Shadow Sharpness" : "Shadow Softness", 0, 40, ref t.ShadowSoftness);
                    ShadowSoftnessSliderPair.DynamicSlider.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).ShadowSoftness = evt.newValue; (t1[i] as RayTracingLights).NeedsToUpdate = true;} ShadowSoftnessSliderPair.DynamicField.value = t.ShadowSoftness;});
                    ShadowSoftnessSliderPair.DynamicField.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).ShadowSoftness = evt.newValue; (t1[i] as RayTracingLights).NeedsToUpdate = true;} ShadowSoftnessSliderPair.DynamicSlider.value = t.ShadowSoftness;});
                MainContainer.Add(ShadowSoftnessSliderPair.DynamicContainer);


                Toggle UseKelvinToggle = new Toggle() {value = t.UseKelvin, text = "Use Kelvin For Color"};
                MainContainer.Add(UseKelvinToggle);
                VisualElement KelvinContainer = CreateVerticalBox("Kelvin Container");
                    FloatSliderPair KelvinTemperatureSliderPair = CreatePairedFloatSlider("Kelvin Temperature", 1000.0f, 20000.0f, ref t.KelvinTemperature);
                        KelvinTemperatureSliderPair.DynamicSlider.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).KelvinTemperature = evt.newValue; (t1[i] as RayTracingLights).NeedsToUpdate = true;} KelvinTemperatureSliderPair.DynamicField.value = t.KelvinTemperature;});
                        KelvinTemperatureSliderPair.DynamicField.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).KelvinTemperature = evt.newValue; (t1[i] as RayTracingLights).NeedsToUpdate = true;} KelvinTemperatureSliderPair.DynamicSlider.value = t.KelvinTemperature;});
                KelvinContainer.Add(KelvinTemperatureSliderPair.DynamicContainer);
                UseKelvinToggle.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).UseKelvin = evt.newValue; (t1[i] as RayTracingLights).NeedsToUpdate = true;} if(evt.newValue) MainContainer.Insert(MainContainer.IndexOf(UseKelvinToggle) + 1, KelvinContainer); else MainContainer.Remove(KelvinContainer);});
                if(t.UseKelvin) MainContainer.Add(KelvinContainer);

                Toggle IsMainSunToggle = new Toggle() {value = t.IsMainSun, text = "Is Sun"};
                if(t.ThisLight.type == LightType.Directional)
                    MainContainer.Add(IsMainSunToggle);
                IsMainSunToggle.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).IsMainSun = evt.newValue; (t1[i] as RayTracingLights).NeedsToUpdate = true;}});

                Vector2Field IESScaleField = new Vector2Field("IES Scale");
                Vector2Field IESOffsetField = new Vector2Field("IES Offset");
                IESScaleField.value = new Vector2(t.IESTexScaleOffset.x, t.IESTexScaleOffset.y);
                IESOffsetField.value = new Vector2(t.IESTexScaleOffset.z, t.IESTexScaleOffset.w);

                ObjectField IESField = new ObjectField("IES Texture");
                IESField.objectType = typeof(Texture2D);
                IESField.value = t.IESProfile;
                if(t.ThisLight.type == LightType.Spot) {
                    MainContainer.Add(IESField);
                    MainContainer.Add(IESScaleField);
                    MainContainer.Add(IESOffsetField);

                }
                IESField.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).IESProfile = evt.newValue as Texture2D; (t1[i] as RayTracingLights).NeedsToUpdate = true;}});
                IESScaleField.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).IESTexScaleOffset = new Vector4(evt.newValue.x, evt.newValue.y, (t1[i] as RayTracingLights).IESTexScaleOffset.z, (t1[i] as RayTracingLights).IESTexScaleOffset.w); (t1[i] as RayTracingLights).NeedsToUpdate = true;}});
                IESOffsetField.RegisterValueChangedCallback(evt => {for(int i = 0; i < TargCount; i++) {(t1[i] as RayTracingLights).IESTexScaleOffset = new Vector4((t1[i] as RayTracingLights).IESTexScaleOffset.x, (t1[i] as RayTracingLights).IESTexScaleOffset.y, evt.newValue.x, evt.newValue.y); (t1[i] as RayTracingLights).NeedsToUpdate = true;}});


            return MainContainer;
        }

    }
#endif
}
