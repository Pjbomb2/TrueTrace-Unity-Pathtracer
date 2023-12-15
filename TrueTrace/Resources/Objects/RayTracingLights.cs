using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;

namespace TrueTrace {
    [ExecuteInEditMode][System.Serializable]
    public class RayTracingLights : MonoBehaviour {
        [HideInInspector] public Light ThisLight;
        [HideInInspector] public LightData ThisLightData;
        [HideInInspector] public int ArrayIndex;
        [HideInInspector] private bool HasChanged;
        [Range(0,10)] public float ShadowSoftness = 0.0f;

        public void Start() {
            ThisLightData = new LightData();
            ThisLight = this.GetComponent<Light>();
            HasChanged = true;
        }
        void Awake() {
            ThisLightData = new LightData();
            ThisLight = this.GetComponent<Light>();
            HasChanged = true;
            // ThisLight.shadows = LightShadows.None;
        }
        public void UpdateLight() {
            if(transform.hasChanged || HasChanged) {
                ThisLightData.Position = transform.position;
                ThisLightData.Direction = (ThisLight.type == LightType.Directional) ? -transform.forward : (ThisLight.type == LightType.Spot) ? Vector3.Normalize(transform.forward) : transform.forward;
                HasChanged = false;
                ThisLightData.ZAxisRotation = transform.localEulerAngles.z * 3.14159f / 180.0f;
                transform.hasChanged = false;
            }
            Color col = ThisLight.color; 
            ThisLightData.Radiance = new Vector3(col[0], col[1], col[2]) * ThisLight.intensity;
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
            RayTracingMaster.RegisterObject(this);
        }

        private void OnDisable() {
            RayTracingMaster.UnregisterObject(this);
        }
    }
}
