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
	public int ObjectGroup;

	public void matfill() {
		if(emmission == null || emmission.Length != this.GetComponent<MeshFilter>().sharedMesh.subMeshCount) {
			int SubMeshCount = GetComponent<MeshFilter>().sharedMesh.subMeshCount;
			emmission = new float[SubMeshCount];
			Roughness = new float[SubMeshCount];
			eta = new Vector3[SubMeshCount];
			MatType = new int[SubMeshCount];
			BaseColor = new Vector3[SubMeshCount];
			Material[] SharedMaterials = GetComponent<Renderer>().sharedMaterials;
			for(int i = 0; i < SubMeshCount; i++) {
				if(SharedMaterials[i].GetFloat("_Mode") == 3.0f) {
					MatType[i] = 2;
					eta[i].x = 1.33f;
				}
				BaseColor[i] = (SharedMaterials[i].mainTexture == null) ? ((SharedMaterials[i].HasProperty("_Color")) ? new Vector3(SharedMaterials[i].color.r, SharedMaterials[i].color.g, SharedMaterials[i].color.b) : new Vector3(0.78f, 0.14f, 0.69f)) : new Vector3(0.78f, 0.14f, 0.69f);
			}
			ObjectGroup = -1;
		}
	}

	public void ResetData() {
		emmission = null;
		Roughness = null;
		eta = null;
		ObjectGroup = -1;
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