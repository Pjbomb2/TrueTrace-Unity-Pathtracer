using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;

namespace TrueTrace {
    [ExecuteInEditMode][System.Serializable][RequireComponent(typeof(Light))]
    public class RayTracingLights : MonoBehaviour {
        [HideInInspector] public Light ThisLight;
        [HideInInspector] public LightData ThisLightData;
        [HideInInspector] public int ArrayIndex;
        [HideInInspector] public int IESIndex;
        [Range(0,40)] public float ShadowSoftness = 0.0f;
        [SerializeField] public bool UseKelvin = false;
        [SerializeField] public bool DebugTest = false;
        [SerializeField] [Range(1000.0f,20000.0f)] public float KelvinTemperature = 1000.0f;
        public Texture2D IESProfile;


        private Vector3 GetRgbFromTemperature(float temperature) {
            temperature = Mathf.Clamp(temperature, 1000, 40000);

            temperature /= 100.0f;

            float red, green, blue;

            if (temperature <= 66) red = 255;
            else {
                red = (329.698727446f * (Mathf.Pow(temperature - 60, -0.1332047592f)));
                red = Mathf.Clamp(red, 0, 255);
            }

            if (temperature <= 66) green = (int)(99.4708025861f * Mathf.Log(temperature) - 161.1195681661f);
            else green = (288.1221695283f * (Mathf.Pow(temperature - 60, -0.0755148492f)));

            green = Mathf.Clamp(green, 0, 255);

            if (temperature >= 66) blue = 255;
            else if (temperature <= 19) blue = 0;
            else {
                blue = (int)(138.5177312231f * Mathf.Log(temperature - 10) - 305.0447927307f);
                blue = Mathf.Clamp(blue, 0, 255);
            }

            return new Vector3(red, green, blue) / 255.0f;
        }


        public void Start() {
            ThisLightData = new LightData();
            ThisLight = this.GetComponent<Light>();
        }
        void Awake() {
            ThisLightData = new LightData();
            ThisLight = this.GetComponent<Light>();
            // ThisLight.shadows = LightShadows.None;
        }
        public void UpdateLight() {
            ThisLight.useColorTemperature = true;
            UnityEngine.Rendering.GraphicsSettings.lightsUseLinearIntensity = true;
            // if(transform.hasChanged || HasChanged) {
                ThisLightData.Position = transform.position;
                ThisLightData.Direction = (ThisLight.type == LightType.Directional) ? -transform.forward : (ThisLight.type == LightType.Spot) ? Vector3.Normalize(transform.forward) : transform.forward;
                ThisLightData.ZAxisRotation = transform.localEulerAngles.z * 3.14159f / 180.0f;
                transform.hasChanged = false;
            // }
            if(UseKelvin) {
                if(DebugTest) {
                    Color TempCol = Mathf.CorrelatedColorTemperatureToRGB(KelvinTemperature);
                    ThisLightData.Radiance = new Vector3(TempCol[0], TempCol[1], TempCol[2]) * ThisLight.intensity;
                } else {
                    ThisLightData.Radiance = GetRgbFromTemperature(KelvinTemperature) * ThisLight.intensity;
                }
            } else {
                Color col = ThisLight.color;
                ThisLightData.Radiance = new Vector3(col[0], col[1], col[2]) * ThisLight.intensity;
            }
            ThisLightData.Type = (ThisLight.type == LightType.Point) ? 0 : (ThisLight.type == LightType.Directional) ? 1 : (ThisLight.type == LightType.Spot) ? 2 : (ThisLight.type == LightType.Rectangle) ? 3 : 4;
            if(ThisLight.type == LightType.Spot) {
                float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * ThisLight.innerSpotAngle);
                float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * ThisLight.spotAngle);
                float angleRangeInv = 1.0f / Mathf.Max(innerCos - outerCos, 0.001f);
                ThisLightData.SpotAngle = new Vector2(angleRangeInv, -outerCos * angleRangeInv);
            } else if(ThisLight.type == LightType.Rectangle) {
                #if UNITY_EDITOR
                    ThisLightData.SpotAngle = ThisLight.areaSize;
                #endif
            } else if(ThisLight.type == LightType.Disc) {
                #if UNITY_EDITOR
                    ThisLightData.SpotAngle = ThisLight.areaSize;
                #endif
            } else {
                ThisLightData.SpotAngle = new Vector2(0.0f, 0.0f);
            }
            ThisLightData.Softness = ShadowSoftness;
        }

        private void OnEnable() { 
            UpdateLight();  
            if(!RayTracingMaster._rayTracingLights.Contains(this)) RayTracingMaster.RegisterObject(this);
        }

        private void OnDisable() {
            RayTracingMaster.UnregisterObject(this);
        }
    }
}
