using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[ExecuteInEditMode][System.Serializable]
public class RayTracingObject : MonoBehaviour {
	public enum Options {Diffuse, Metallic, Glass, Glossy, Unused, Volumetric, SubSurfaceScattering, DiffuseTransmission, Plastic};
	public Options[] MaterialOptions;
	public float[] emmission, Roughness;
	public Vector3[] eta, BaseColor;
	public int[] MaterialIndex;
	public int[] LocalMaterialIndex;

	public void matfill() {
		 Mesh mesh = new Mesh();
		 if(GetComponent<MeshFilter>() != null) { 
		 	mesh = GetComponent<MeshFilter>().sharedMesh;
	 	} else {
	 		GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh);
	 	}

		if(emmission == null || emmission.Length != mesh.subMeshCount) {
			int SubMeshCount = mesh.subMeshCount;
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
				if(SharedMaterials[i].GetTexture("_EmissionMap") != null) {
					//emmission[i] = 12.0f;
						//Color Col = ((Texture2D)SharedMaterials[i].GetTexture("_EmissionMap")).GetPixel(8,8,0);
						//BaseColor[i] = new Vector3(Col.r, Col.g, Col.b);
						EmissionColored = true;
				}
				if(SharedMaterials[i].GetFloat("_Mode") == 3.0f) {
					MaterialOptions[i] = Options.Glass;
					eta[i].x = 1.33f;
				}
				if(!EmissionColored) BaseColor[i] = (SharedMaterials[i].mainTexture == null) ? ((SharedMaterials[i].HasProperty("_Color")) ? new Vector3(SharedMaterials[i].color.r, SharedMaterials[i].color.g, SharedMaterials[i].color.b) : new Vector3(0.78f, 0.14f, 0.69f)) : new Vector3(0.78f, 0.14f, 0.69f);
			}
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