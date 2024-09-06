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
        [SerializeField] [Range(1000.0f,20000.0f)] public float KelvinTemperature = 1000.0f;
        public Texture2D IESProfile;



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
            Color TempCol;
            if(UseKelvin) TempCol = Mathf.CorrelatedColorTemperatureToRGB(KelvinTemperature);
            else TempCol = ThisLight.color;

            ThisLightData.Radiance = new Vector3(TempCol[0], TempCol[1], TempCol[2]) * ThisLight.intensity;
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
