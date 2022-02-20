using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class RayTracingObject : MonoBehaviour {
	public float[] emmission, Roughness;
	public Vector3[] eta, BaseColor;
	public int[] MatType;
	public int Dynamic;

	public void matfill() {
		if(emmission == null || emmission.Length != this.GetComponent<MeshFilter>().sharedMesh.subMeshCount) {
			int SubMeshCount = GetComponent<MeshFilter>().sharedMesh.subMeshCount;
			emmission = new float[SubMeshCount];
			Roughness = new float[SubMeshCount];
			eta = new Vector3[SubMeshCount];
			MatType = new int[SubMeshCount];
			BaseColor = new Vector3[SubMeshCount];
			for(int i = 0 ; i < SubMeshCount; i++) {
				BaseColor[i] = (GetComponent<Renderer>().sharedMaterials[i].mainTexture == null) ? ((GetComponent<Renderer>().sharedMaterials[i].HasProperty("_Color")) ? new Vector3(GetComponent<Renderer>().sharedMaterials[i].color.r, GetComponent<Renderer>().sharedMaterials[i].color.g, GetComponent<Renderer>().sharedMaterials[i].color.b) : new Vector3(0.78f, 0.14f, 0.69f)) : new Vector3(0.78f, 0.14f, 0.69f);
			}
			Dynamic = 0;
		}
	}

	public void ResetData() {
		emmission = null;
		Roughness = null;
		eta = null;
		Dynamic = 0;
		MatType = null;
		BaseColor = null;
	}
	
    private void OnEnable() {
        RayTracingMaster.RegisterObject(this);
    }

    private void OnDisable() {
        RayTracingMaster.UnregisterObject(this);
    }
}