using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode][System.Serializable]
public class RayTracingLights : MonoBehaviour {
    public Vector3 Emission;
    public Vector3 Direction;
    public Vector3 Position;
    public int Type;
    [HideInInspector]
    public Light ThisLight;
    public Vector2 SpotAngle;
    public float Energy;
    public int ArrayIndex;
    public Transform ThisTransform;
    private bool HasChanged;
    public float ZAxisRotation;

    private float luminance(float r, float g, float b) {
        return 0.299f * r + 0.587f * g + 0.114f * b;
    }
    public void Start() {
        ThisLight = this.GetComponent<Light>();
        ThisTransform = this.transform;
        HasChanged = true;
    }
    void Awake() {
        ThisLight = this.GetComponent<Light>();
        ThisTransform = this.transform;
        HasChanged = true;
        ThisLight.shadows = LightShadows.None;
    }
    public void UpdateLight() {
        if(ThisTransform.hasChanged || HasChanged) {
            Position = ThisTransform.position;
            Direction = (ThisLight.type == LightType.Directional) ? -ThisTransform.forward : (ThisLight.type == LightType.Spot) ? Vector3.Normalize(ThisTransform.forward) : ThisTransform.forward;
            HasChanged = false;
            ZAxisRotation = ThisTransform.localEulerAngles.z;
            ThisTransform.hasChanged = false;
        }
        Color col = ThisLight.color; 
        Emission = new Vector3(col[0], col[1], col[2]) * ThisLight.intensity;
        Type = (ThisLight.type == LightType.Point) ? 0 : (ThisLight.type == LightType.Directional) ? 1 : (ThisLight.type == LightType.Spot) ? 2 : 3;
        if(ThisLight.type == LightType.Spot) {
            float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * ThisLight.innerSpotAngle);
            float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * ThisLight.spotAngle);
            float angleRangeInv = 1.0f / Mathf.Max(innerCos - outerCos, 0.001f);
            SpotAngle = new Vector2(angleRangeInv, -outerCos * angleRangeInv);
        } else if(ThisLight.type == LightType.Area) {
            SpotAngle = ThisLight.areaSize;
        } else {
            SpotAngle = new Vector2(0.0f, 0.0f);
        }
        Energy = luminance(Emission.x, Emission.y, Emission.z) * ((ThisLight.type == LightType.Area) ? (4.0f * SpotAngle.x * SpotAngle.y) : 1);
    }

    private void OnEnable() { 
        UpdateLight();           
        RayTracingMaster.RegisterObject(this);
    }

    private void OnDisable() {
        RayTracingMaster.UnregisterObject(this);
    }
}
