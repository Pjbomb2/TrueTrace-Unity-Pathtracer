using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BSDFMaterial
{
    public string ObjName;
    public string MatName;
    public string MatType;
    public Vector3 SurfaceColor;
    public bool HasTexture;
    public Texture2D AlbedoTexture;
    public float Linear_Roughness;
    public Vector3 eta;
    public Vector3 k;
    public float int_IOR;
    public int MatIndex;
    public float emissive = 0.0f;
    public bool HasBeenModified = false;
}
