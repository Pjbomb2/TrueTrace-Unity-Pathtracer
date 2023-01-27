using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TrueTrace {
	[ExecuteInEditMode][System.Serializable]
	public class RayTracingObject : MonoBehaviour {
		public enum Options {Diffuse, Disney, Cutout, Volumetric, Video};
		public Options[] MaterialOptions;
		[SerializeField] public Vector3[] TransmissionColor, BaseColor;
		[SerializeField] public float[] emmission; 
		[SerializeField] public Vector3[] EmissionColor;
		[SerializeField] public float[] Roughness;
		[SerializeField] public float[] IOR;
		[SerializeField] public float[] Metallic;
		[SerializeField] public float[] SpecularTint;
		[SerializeField] public float[] Sheen;
		[SerializeField] public float[] SheenTint;
		[SerializeField] public float[] ClearCoat;
		[SerializeField] public float[] ClearCoatGloss;
		[SerializeField] public float[] Anisotropic;
		[SerializeField] public float[] Flatness;
		[SerializeField] public float[] DiffTrans;
		[SerializeField] public float[] SpecTrans;
		[SerializeField] public int[] Thin;
		public string[] Names;
		[SerializeField] public float[] Specular;
		[SerializeField] public int Selected;
		public int[] Indexes;
		public bool NeedsToUpdate;
		[SerializeField] public bool IsReady = false;

		[HideInInspector] public int[] MaterialIndex;
		[HideInInspector] public int[] LocalMaterialIndex;
		AssetManager Assets;
		[HideInInspector] public bool JustCreated = true;
		private bool WasDeleted = false;
		public void CallMaterialEdited() {
			if(gameObject.activeInHierarchy && Assets != null) Assets.MaterialsChanged.Add(this);
		}

		public void matfill() {
			WasDeleted = false;
			Assets = GameObject.Find("Scene").GetComponent<AssetManager>();
			 Mesh mesh = new Mesh();
			 int SubMeshCount;
			 if(GetComponent<MeshFilter>() != null) { 
			 	mesh = GetComponent<MeshFilter>().sharedMesh;
			 	SubMeshCount = (GetComponent<MeshRenderer>().sharedMaterials).Length;
		 	} else {
		 		if(this.GetComponent<SkinnedMeshRenderer>() == null) DestroyImmediate(this);
		 		GetComponent<SkinnedMeshRenderer>().BakeMesh(mesh);
				SubMeshCount = (GetComponent<SkinnedMeshRenderer>().sharedMaterials).Length;
				this.transform.localScale = new Vector3(1,1,1);
				this.transform.position = new Vector3(0,0,0);
				this.transform.eulerAngles = new Vector3(0,0,0);
		 	}
		 	if(mesh == null) {
		 		DestroyImmediate(this);
		 		WasDeleted = true;
		 		return;
		 	}
			SubMeshCount = Mathf.Min(mesh.subMeshCount, SubMeshCount);
		 	if(SubMeshCount == 0) {
		 		DestroyImmediate(this);
		 		WasDeleted = true;
		 		return;
		 	}
		 	Material[] SharedMaterials = (GetComponent<Renderer>() != null) ? GetComponent<Renderer>().sharedMaterials : GetComponent<SkinnedMeshRenderer>().sharedMaterials;
			List<string> PropertyNames = new List<string>();
		 	if(Indexes == null || Indexes.Length != mesh.subMeshCount) Indexes = new int[Mathf.Max(mesh.subMeshCount, SubMeshCount)];
		 	if(Specular == null || Specular.Length != SubMeshCount) Specular = new float[SubMeshCount];
			try {
				if(Names == null || Names.Length == 0) {
					Names = new string[SubMeshCount];
					TransmissionColor = new Vector3[SubMeshCount];
					EmissionColor = new Vector3[SubMeshCount];				
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
					MaterialOptions = new Options[SubMeshCount];
					LocalMaterialIndex = new int[SubMeshCount];
					emmission = new float[SubMeshCount];
					Roughness = new float[SubMeshCount];
					System.Array.Fill(IOR, 1);
					BaseColor = new Vector3[SubMeshCount];
					MaterialIndex = new int[SubMeshCount];
					for(int i = 0; i < SubMeshCount; i++) {
						MaterialOptions[i] = Options.Disney;
						Names[i] = SharedMaterials[i].name;
						BaseColor[i] = new Vector3(1,1,1);
					}
				}
			} catch(System.Exception e) {
				Debug.Log("ERROR AT: " + this.gameObject.name + ": " + e);
			}
			IsReady = true;
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
	    		if(WasDeleted) return;
		    	this.transform.parent.GetComponent<ParentObject>().NeedsToUpdate = true;
				if(Assets != null && Assets.UpdateQue != null && !Assets.UpdateQue.Contains(this.transform.parent.GetComponent<ParentObject>())) Assets.UpdateQue.Add(this.transform.parent.GetComponent<ParentObject>());
	    	} else if(gameObject.scene.isLoaded && this.transform.GetComponent<ParentObject>() != null) {
	    		matfill();
	    		if(WasDeleted) return;
				if(Assets != null && Assets.UpdateQue != null && !Assets.UpdateQue.Contains(this.transform.parent.GetComponent<ParentObject>())) Assets.UpdateQue.Add(this.transform.parent.GetComponent<ParentObject>());
		    	this.transform.GetComponent<ParentObject>().NeedsToUpdate = true;
	    	}
	    }

	    private void OnDisable() {
	    	if(gameObject.scene.isLoaded && this.transform.parent.GetComponent<ParentObject>() != null) {
	    		this.transform.parent.GetComponent<ParentObject>().NeedsToUpdate = true;
	    		if(Assets != null && Assets.UpdateQue != null && !Assets.UpdateQue.Contains(this.transform.parent.GetComponent<ParentObject>())) Assets.UpdateQue.Add(this.transform.parent.GetComponent<ParentObject>());
	    	} else if(gameObject.scene.isLoaded && this.transform.GetComponent<ParentObject>() != null) {
		    	this.transform.GetComponent<ParentObject>().NeedsToUpdate = true;
	    	}
	    }

	}
}