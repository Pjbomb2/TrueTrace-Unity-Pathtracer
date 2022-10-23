using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[ExecuteInEditMode][System.Serializable]
public class RayTracingObject : MonoBehaviour {
	public enum Options {Diffuse, Disney, Glossy, Cutout, Volumetric, SubSurfaceScattering, DiffuseTransmission, Plastic};
	public Options[] MaterialOptions;
	public Vector3[] TransmissionColor, BaseColor;
	public float[] emmission; 
	public Vector3[] EmissionColor;
	public float[] Roughness;
	public float[] IOR;


	public float[] Metallic;
	public float[] SpecularTint;
	public float[] Sheen;
	public float[] SheenTint;
	public float[] ClearCoat;
	public float[] ClearCoatGloss;
	public float[] Anisotropic;
	public float[] Flatness;
	public float[] DiffTrans;
	public float[] SpecTrans;
	public int[] Thin;

	[HideInInspector] public int[] MaterialIndex;
	[HideInInspector] public int[] LocalMaterialIndex;
	public void matfill() {
		 Mesh mesh = new Mesh();
		 			int SubMeshCount;
		 if(GetComponent<MeshFilter>() != null) { 
		 	mesh = GetComponent<MeshFilter>().sharedMesh;
		 	SubMeshCount = (GetComponent<MeshRenderer>().sharedMaterials).Length;
	 	} else {
	 		GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh);
			SubMeshCount = (GetComponent<SkinnedMeshRenderer>().sharedMaterials).Length;
	 	}
	 	if(mesh == null) {
	 		DestroyImmediate(this);
	 	}
			if(EmissionColor == null || EmissionColor.Length != SubMeshCount) EmissionColor = new Vector3[SubMeshCount];
		
			if(Thin == null || Thin.Length != SubMeshCount) {
				TransmissionColor = new Vector3[SubMeshCount];
				IOR = new float[SubMeshCount];
				Metallic = new float[SubMeshCount];
				SpecularTint = new float[SubMeshCount];
				Sheen = new float[SubMeshCount];
				SheenTint = new float[SubMeshCount];
				ClearCoat = new float[SubMeshCount];
				ClearCoatGloss = new float[SubMeshCount];
				Anisotropic = new float[SubMeshCount];
				Flatness = new float[SubMeshCount];
				DiffTrans = new float[SubMeshCount];
				SpecTrans = new float[SubMeshCount];
				Thin = new int[SubMeshCount];
			}


		try {
			if(emmission == null || emmission.Length != mesh.subMeshCount) {
			MaterialOptions = new Options[SubMeshCount];
			LocalMaterialIndex = new int[SubMeshCount];
			emmission = new float[SubMeshCount];
			Roughness = new float[SubMeshCount];
			System.Array.Fill(IOR, 1);
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
					MaterialOptions[i] = Options.Disney;
					IOR[i] = 1.33f;
					SpecTrans[i] = 1;

				}
				if(SharedMaterials[i].GetFloat("_Mode") == 1.0f) {
					MaterialOptions[i] = Options.Cutout;
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
		TransmissionColor = null;
		MaterialOptions = null;
		BaseColor = null;
	}
	
    private void OnEnable() {
    	// if(this.gameObject.GetComponent<SkinnedMeshRenderer>() != null) this.gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
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