using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[ExecuteInEditMode][System.Serializable]
public class RayTracingObject : MonoBehaviour {
	public enum Options {Diffuse, Metallic, Glass, Glossy, Unused, Volumetric, SubSurfaceScattering, DiffuseTransmission, Plastic, Disney};
	public Options[] MaterialOptions;
	public float[] emmission; 
	public Vector3[] EmissionColor;
	public float[] Roughness;
	public Vector3[] eta, BaseColor;
	public int[] MaterialIndex;
	public int[] LocalMaterialIndex;


	public float[] Metallic;
	public float[] SubSurface;
	public float[] SpecularTint;
	public float[] Sheen;
	public float[] SheenTint;
	public float[] ClearCoat;
	public float[] ClearCoatRoughness;
	public float[] SpecTrans;
	public float[] IOR;
	public Vector3[] Extinction;

	public void matfill() {
		 Mesh mesh = new Mesh();
		 if(GetComponent<MeshFilter>() != null) { 
		 	mesh = GetComponent<MeshFilter>().sharedMesh;
	 	} else {
	 		GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh);
	 	}
			int SubMeshCount = mesh.subMeshCount;
			if(EmissionColor == null || EmissionColor.Length != SubMeshCount) EmissionColor = new Vector3[SubMeshCount];
		
			if(Metallic == null || Metallic.Length != mesh.subMeshCount) {
				Metallic = new float[SubMeshCount];
				SubSurface = new float[SubMeshCount];
				SpecularTint = new float[SubMeshCount];
				Sheen = new float[SubMeshCount];
				SheenTint = new float[SubMeshCount];
				ClearCoat = new float[SubMeshCount];
				ClearCoatRoughness = new float[SubMeshCount];
				SpecTrans = new float[SubMeshCount];
				IOR = new float[SubMeshCount];
				Extinction = new Vector3[SubMeshCount];
			}


		try {
			if(emmission == null || emmission.Length != mesh.subMeshCount) {
			MaterialOptions = new Options[SubMeshCount];
			LocalMaterialIndex = new int[mesh.subMeshCount];
			emmission = new float[SubMeshCount];
			Roughness = new float[SubMeshCount];
			eta = new Vector3[SubMeshCount];
			BaseColor = new Vector3[SubMeshCount];
			MaterialIndex = new int[SubMeshCount];
			Material[] SharedMaterials = (GetComponent<Renderer>() != null) ? GetComponent<Renderer>().sharedMaterials : GetComponent<SkinnedMeshRenderer>().sharedMaterials;
			for(int i = 0; i < SubMeshCount; i++) {
				MaterialOptions[i] = Options.Diffuse;
				bool EmissionColored = false;
				if(SharedMaterials[i].GetTexture("_EmissionMap") != null && SharedMaterials[i].GetTexture("_EmissionMap").width < 32) {
					emmission[i] = 4.0f;
					if(SharedMaterials[i].GetTexture("_EmissionMap") != null) {
						Color Col = ((Texture2D)SharedMaterials[i].GetTexture("_EmissionMap")).GetPixel(8,8,0);
						BaseColor[i] = new Vector3(Col.r, Col.g, Col.b);
					} else {
						Color Col = SharedMaterials[i].GetColor("_EmissionColor");
						EmissionColor[i] = new Vector3(Col.r, Col.g, Col.b).normalized;
					}
				}
				if(SharedMaterials[i].GetFloat("_Mode") == 3.0f) {
					MaterialOptions[i] = Options.Glass;
					eta[i].x = 1.33f;
				}
				if(!EmissionColored) BaseColor[i] = (SharedMaterials[i].mainTexture == null) ? ((SharedMaterials[i].HasProperty("_Color")) ? new Vector3(SharedMaterials[i].color.r, SharedMaterials[i].color.g, SharedMaterials[i].color.b) : new Vector3(0.78f, 0.14f, 0.69f)) : new Vector3(0.78f, 0.14f, 0.69f);
			}
		}
		} catch(System.Exception e) {
			Debug.Log("ERROR AT: " + this.gameObject.name);
		}
		mesh = null;
	}

	public void ResetData() {
		emmission = null;
		Roughness = null;
		eta = null;
		MaterialOptions = null;
		BaseColor = null;
	}
	
    private void OnEnable() {
    	if(gameObject.scene.isLoaded && this.transform.parent.GetComponent<ParentObject>() != null) {
    		matfill();
	    	this.transform.parent.GetComponent<ParentObject>().NeedsToUpdate = true;
    	} else if(gameObject.scene.isLoaded && this.transform.GetComponent<ParentObject>() != null) {
    		matfill();
	    	this.transform.GetComponent<ParentObject>().NeedsToUpdate = true;
    	}
    }

    private void OnDisable() {
    	if(gameObject.scene.isLoaded && this.transform.parent.GetComponent<ParentObject>() != null) {
    		this.transform.parent.GetComponent<ParentObject>().NeedsToUpdate = true;
    	} else if(gameObject.scene.isLoaded && this.transform.GetComponent<ParentObject>() != null) {
	    	this.transform.GetComponent<ParentObject>().NeedsToUpdate = true;
    	}
    }
}