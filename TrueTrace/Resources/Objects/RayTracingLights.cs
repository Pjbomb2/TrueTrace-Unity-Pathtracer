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
        [HideInInspector] public Light ThisLight;
        [HideInInspector] public LightData ThisLightData;
        [HideInInspector] public int ArrayIndex;
        [HideInInspector] public int IESIndex;
        [SerializeField] [Range(0,40)] public float ShadowSoftness = 0.0f;
        [SerializeField] public bool UseKelvin = false;
        [SerializeField] public bool IsMainSun = false;
        [SerializeField] [Range(1000.0f,20000.0f)] public float KelvinTemperature = 1000.0f;
        public Texture2D IESProfile;

        Vector3 PrevCol;
        float prevInten;

        public void Start() {
            ThisLightData = new LightData();
            ThisLight = this.GetComponent<Light>();
        }
        void Awake() {
            ThisLightData = new LightData();
            ThisLight = this.GetComponent<Light>();
            ThisLight.shadows = LightShadows.None;
        }
        public bool UpdateLight(bool OverrideTransform) {
            bool HasChanged = false;
            ThisLight.useColorTemperature = true;
            UnityEngine.Rendering.GraphicsSettings.lightsUseLinearIntensity = true;
            // if(transform.hasChanged || OverrideTransform) {
                if(ThisLightData.Position != transform.position) HasChanged = true;
                if(!HasChanged && ThisLightData.Direction != ((ThisLight.type == LightType.Directional) ? -transform.forward : (ThisLight.type == LightType.Spot) ? Vector3.Normalize(transform.forward) : transform.forward)) HasChanged = true;
                if(!HasChanged && Mathf.Abs(ThisLightData.ZAxisRotation - transform.localEulerAngles.z * 3.14159f / 180.0f) > 0.00001f) HasChanged = true;
                ThisLightData.Position = transform.position;
                ThisLightData.Direction = (ThisLight.type == LightType.Directional) ? -transform.forward : (ThisLight.type == LightType.Spot) ? Vector3.Normalize(transform.forward) : transform.forward;
                ThisLightData.ZAxisRotation = transform.localEulerAngles.z * 3.14159f / 180.0f;
                transform.hasChanged = false;
            // }
            Color TempCol;
            if(UseKelvin) TempCol = Mathf.CorrelatedColorTemperatureToRGB(KelvinTemperature);
            else TempCol = ThisLight.color;

            if(!HasChanged && (PrevCol != new Vector3(TempCol[0], TempCol[1], TempCol[2]) || (prevInten != ThisLight.intensity))) HasChanged = true;
            if(!HasChanged && ThisLightData.Type != ((ThisLight.type == LightType.Point) ? 0 : (ThisLight.type == LightType.Directional) ? 1 : (ThisLight.type == LightType.Spot) ? 2 : (ThisLight.type == LightType.Rectangle) ? 3 : 4)) HasChanged = true;

            prevInten = ThisLight.intensity;
            PrevCol = new Vector3(TempCol[0], TempCol[1], TempCol[2]);

            ThisLightData.Radiance = new Vector3(TempCol[0], TempCol[1], TempCol[2]) * ThisLight.intensity;
            ThisLightData.Type = (ThisLight.type == LightType.Point) ? 0 : (ThisLight.type == LightType.Directional) ? 1 : (ThisLight.type == LightType.Spot) ? 2 : (ThisLight.type == LightType.Rectangle) ? 3 : 4;
            if(ThisLight.type == LightType.Spot) {
                float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * ThisLight.innerSpotAngle);
                float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * ThisLight.spotAngle);
                float angleRangeInv = 1.0f / Mathf.Max(innerCos - outerCos, 0.001f);
                if(!HasChanged && ThisLightData.SpotAngle != new Vector2(angleRangeInv, -outerCos * angleRangeInv)) HasChanged = true;
                ThisLightData.SpotAngle = new Vector2(angleRangeInv, -outerCos * angleRangeInv);
            } else if(ThisLight.type == LightType.Rectangle) {
                #if UNITY_EDITOR
                    if(!HasChanged && ThisLightData.SpotAngle != ThisLight.areaSize) HasChanged = true;
                    ThisLightData.SpotAngle = ThisLight.areaSize;
                #endif
            } else if(ThisLight.type == LightType.Disc) {
                #if UNITY_EDITOR
                    if(!HasChanged && ThisLightData.SpotAngle != ThisLight.areaSize) HasChanged = true;
                    ThisLightData.SpotAngle = ThisLight.areaSize;
                #endif
            } else {
                ThisLightData.SpotAngle = Vector2.zero;
            }
            if(!HasChanged && ThisLightData.Softness != ShadowSoftness) HasChanged = true;
            ThisLightData.Softness = ShadowSoftness;
            return HasChanged;
        }

        private void OnEnable() { 
            UpdateLight(true);  
            if(!RayTracingMaster._rayTracingLights.Contains(this)) RayTracingMaster.RegisterObject(this);
        }

        private void OnDisable() {
            RayTracingMaster.UnregisterObject(this);
        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(RayTracingLights))]
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
        FloatSliderPair CreatePairedFloatSlider(string Name, float LowValue, float HighValue, ref float InitialValue, float SliderWidth = 100) {
            FloatSliderPair NewPair = new FloatSliderPair();
            NewPair.DynamicContainer = CreateHorizontalBox(Name + " Container");
            NewPair.DynamicLabel = new Label(Name);
            NewPair.DynamicSlider = new Slider() {value = InitialValue, highValue = HighValue, lowValue = LowValue};
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
            var t =  t1[0] as RayTracingLights;
            VisualElement MainContainer = CreateVerticalBox("Main Container");
                FloatSliderPair ShadowSoftnessSliderPair = CreatePairedFloatSlider("Shadow Softness", 0, 40, ref t.ShadowSoftness);
                    ShadowSoftnessSliderPair.DynamicSlider.RegisterValueChangedCallback(evt => {t.ShadowSoftness = evt.newValue; ShadowSoftnessSliderPair.DynamicField.value = t.ShadowSoftness;});
                    ShadowSoftnessSliderPair.DynamicField.RegisterValueChangedCallback(evt => {t.ShadowSoftness = evt.newValue; ShadowSoftnessSliderPair.DynamicSlider.value = t.ShadowSoftness;});
                MainContainer.Add(ShadowSoftnessSliderPair.DynamicContainer);


                Toggle UseKelvinToggle = new Toggle() {value = t.UseKelvin, text = "Use Kelvin For Color"};
                MainContainer.Add(UseKelvinToggle);
                VisualElement KelvinContainer = CreateVerticalBox("Kelvin Container");
                    FloatSliderPair KelvinTemperatureSliderPair = CreatePairedFloatSlider("Kelvin Temperature", 1000.0f, 20000.0f, ref t.KelvinTemperature);
                        KelvinTemperatureSliderPair.DynamicSlider.RegisterValueChangedCallback(evt => {t.KelvinTemperature = evt.newValue; KelvinTemperatureSliderPair.DynamicField.value = t.KelvinTemperature;});
                        KelvinTemperatureSliderPair.DynamicField.RegisterValueChangedCallback(evt => {t.KelvinTemperature = evt.newValue; KelvinTemperatureSliderPair.DynamicSlider.value = t.KelvinTemperature;});
                KelvinContainer.Add(KelvinTemperatureSliderPair.DynamicContainer);
                UseKelvinToggle.RegisterValueChangedCallback(evt => {t.UseKelvin = evt.newValue; if(evt.newValue) MainContainer.Insert(MainContainer.IndexOf(UseKelvinToggle) + 1, KelvinContainer); else MainContainer.Remove(KelvinContainer);});
                if(t.UseKelvin) MainContainer.Add(KelvinContainer);

                Toggle IsMainSunToggle = new Toggle() {value = t.IsMainSun, text = "Is Sun"};
                if(t.ThisLight.type == LightType.Directional)
                    MainContainer.Add(IsMainSunToggle);
                IsMainSunToggle.RegisterValueChangedCallback(evt => {t.IsMainSun = evt.newValue;});

                ObjectField IESField = new ObjectField("IES Texture");
                IESField.objectType = typeof(Texture2D);
                IESField.value = t.IESProfile;
                if(t.ThisLight.type == LightType.Spot)
                    MainContainer.Add(IESField);
                IESField.RegisterValueChangedCallback(evt => {t.IESProfile = evt.newValue as Texture2D;});


            return MainContainer;
        }

    }
#endif
}
