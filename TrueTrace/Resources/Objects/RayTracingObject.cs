using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CommonVars;

namespace TrueTrace {
	[ExecuteInEditMode][System.Serializable]
	public class RayTracingObject : MonoBehaviour {
		public enum Options {Disney, Cutout, Fade};
		public enum BlendModes {Lerp, Add, Multiply};
		[SerializeField] public Options[] MaterialOptions;
		[SerializeField] public Vector3[] TransmissionColor, BaseColor;
		[SerializeField] public Vector2[] MetallicRemap, RoughnessRemap;
		[SerializeField] public float[] emission; 
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
		[SerializeField] public bool[] FollowMaterial;
		[SerializeField] public float[] ScatterDist;
		[SerializeField] public float[] Specular;
		[SerializeField] public float[] AlphaCutoff;
		[SerializeField] public float[] NormalStrength;
		[SerializeField] public float[] Hue;
		[SerializeField] public float[] Saturation;
		[SerializeField] public float[] Brightness;
		[SerializeField] public float[] Contrast;
		[SerializeField] public Vector3[] BlendColor;
		[SerializeField] public float[] BlendFactor;
		[SerializeField] public Vector4[] MainTexScaleOffset;
		[SerializeField] public Vector4[] SecondaryAlbedoTexScaleOffset;
		[SerializeField] public Vector2[] SecondaryTextureScale;
		[SerializeField] public float[] Rotation;
		[SerializeField] public int[] Flags;
		[SerializeField] public bool[] UseKelvin;
		[SerializeField] public float[] KelvinTemp;
		[SerializeField] public float[] ColorBleed; 
		[SerializeField] public float[] AlbedoBlendFactor; 
		[SerializeField] public Material[] SharedMaterials;
		[SerializeField] public string[] Names;
		[SerializeField] public int Selected;
		public int[] Indexes;
		public bool NeedsToUpdate;
		[SerializeField] public bool IsReady = false;

		[HideInInspector] public int[] MaterialIndex;
		[HideInInspector] public int[] LocalMaterialIndex;
		[HideInInspector] public bool JustCreated = true;
		[HideInInspector] public bool TilingChanged = false;
		[HideInInspector] public int MatOffset = 0;
		private bool WasDeleted = false;

		public void CallMaterialOverride() {
			Material[] SharedMaterials = (GetComponent<Renderer>() != null) ? GetComponent<Renderer>().sharedMaterials : GetComponent<SkinnedMeshRenderer>().sharedMaterials;
			for(int i = 0; i < Names.Length; i++) {
				Names[i] = SharedMaterials[i].name;
				 if(FollowMaterial[i]) {
					int Index = AssetManager.ShaderNames.IndexOf(SharedMaterials[i].shader.name);
					 if (Index == -1) {
					 	Debug.Log("Material Not Added");
					 	return;
					 }
					 MaterialShader RelevantMat = AssetManager.data.Material[Index];
					if(!RelevantMat.MetallicRange.Equals("null")) Metallic[i] = SharedMaterials[i].GetFloat(RelevantMat.MetallicRange);
                    if(!RelevantMat.RoughnessRange.Equals("null")) Roughness[i] = SharedMaterials[i].GetFloat(RelevantMat.RoughnessRange);
                    if(!RelevantMat.BaseColorValue.Equals("null")) BaseColor[i] = new Vector3(SharedMaterials[i].GetColor(RelevantMat.BaseColorValue).r, SharedMaterials[i].GetColor(RelevantMat.BaseColorValue).g, SharedMaterials[i].GetColor(RelevantMat.BaseColorValue).b);
                    else BaseColor[i] = Vector3.one;
				 }

			}
			if(gameObject.activeInHierarchy && AssetManager.Assets != null) AssetManager.Assets.MaterialsChanged.Add(this);
		}


		public void CallMaterialEdited(bool Do = false) {
			if(Application.isPlaying) {
				if(gameObject.activeInHierarchy && AssetManager.Assets != null) AssetManager.Assets.MaterialsChanged.Add(this);
			}
			System.Array.Fill(FollowMaterial, false);
			CallMaterialOverride();
			#if UNITY_EDITOR
				if(Application.isPlaying && Do) {
					for(int i = 0; i < Names.Length; i++) {
						RayTracingMaster.WriteString(this, Names[i]);
					}
				}
			#endif
		}

		public void CallTilingScrolled() {
			TilingChanged = true;
			CallMaterialEdited();
		}

		int SubMeshCount;
		private void InitializeArray<T>(ref T[] ExsArray, T FillVar, int[] Index, bool NeedsRedo, int ExtraCount = -1) {
			if(ExtraCount == -1) ExtraCount = SubMeshCount;
			if(ExsArray == null || ExsArray.Length != ExtraCount || NeedsRedo) {
				if(ExsArray == null) {
					ExsArray = new T[ExtraCount];
					System.Array.Fill(ExsArray, FillVar);
				} else {
					int PrevLength = ExsArray.Length;
					if(ExtraCount != PrevLength || NeedsRedo) {
						T[] PrevArray = new T[PrevLength];
						System.Array.Copy(ExsArray, PrevArray, PrevLength);
						ExsArray = new T[ExtraCount];
						System.Array.Fill(ExsArray, FillVar);
						for(int i = 0; i < PrevLength; i++) {
							if(Index[i] != -1) ExsArray[Index[i]] = PrevArray[i];
						}

					}
				}
			}
		}

		private bool InitializeArrayWithIndex(ref string[] ExsArray, string FillVar, Material[] SharedMaterials, ref int[] Index, int ExtraCount = -1) {
			if(ExtraCount == -1) ExtraCount = SubMeshCount;
			bool NeedsRedo = false;
			if(ExsArray != null && ExsArray.Length == ExtraCount) {
				for(int i = 0; i < ExtraCount; i++) {
					if(!ExsArray[i].Equals(SharedMaterials[i].name)) {
						NeedsRedo = true;
						break;
					}
				}
			}
			if(ExsArray == null || ExsArray.Length != ExtraCount || NeedsRedo) {
				if(ExsArray == null) {
					ExsArray = new string[ExtraCount];
					System.Array.Fill(ExsArray, FillVar);
					for(int i = 0; i < ExtraCount; i++) {ExsArray[i] = SharedMaterials[i].name;}
				} else {
					int PrevLength = ExsArray.Length;
					string[] OrigExsArray = new string[PrevLength];
					System.Array.Copy(ExsArray, OrigExsArray, PrevLength);
					if(ExtraCount != PrevLength || NeedsRedo) {
						ExsArray = new string[ExtraCount];
						System.Array.Fill(ExsArray, FillVar);
						Index = new int[PrevLength];
						System.Array.Fill(Index, -1);
						for(int i = 0; i < ExtraCount; i++) ExsArray[i] = SharedMaterials[i].name; 
						for(int i = 0; i < ExtraCount; i++) {
							int ArrayCount = OrigExsArray.Length;
							for(int j = 0; j < PrevLength; j++) {
								if(ExsArray[i].Equals(OrigExsArray[j])) {
									Index[j] = i;
									break;
								}

							}

						}
					}
				}
			}
			return NeedsRedo;
		}


		public void matfill() {
			TilingChanged = false;
			WasDeleted = false;
			#if UNITY_EDITOR
				UnityEditor.GameObjectUtility.SetStaticEditorFlags(gameObject, UnityEditor.GameObjectUtility.GetStaticEditorFlags(gameObject) & ~UnityEditor.StaticEditorFlags.BatchingStatic);
			#endif
			 Mesh mesh = new Mesh();
			 if(TryGetComponent<MeshRenderer>(out MeshRenderer MeshRend)) { 
			 	if(TryGetComponent<MeshFilter>(out MeshFilter MeshFilt)) { 
			 		mesh = MeshFilt.sharedMesh;
			 	}
			 	if(!MeshRend.enabled) mesh = null;
			 	SubMeshCount = (MeshRend.sharedMaterials).Length;
			 	if(TryGetComponent<Renderer>(out Renderer Rend)) SharedMaterials = Rend.sharedMaterials;
		 	} else if(TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer SkinnedRend)) {
		 		SkinnedRend.BakeMesh(mesh);
			 	if(!SkinnedRend.enabled) mesh = null;
				SubMeshCount = (SkinnedRend.sharedMaterials).Length;
		 		SharedMaterials = SkinnedRend.sharedMaterials;
		 	} else {
		 		DestroyImmediate(this);
		 		WasDeleted = true;
		 		return;
		 	}
// (Application.isPlaying && !mesh.isReadable) || 
		 	if(mesh == null || SharedMaterials == null || SharedMaterials.Length == 0 || mesh.GetTopology(0) != MeshTopology.Triangles || mesh.vertexCount == 0) {
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
			for(int i = 0; i < SharedMaterials.Length; i++) {	
				if(SharedMaterials[i] == null || SubMeshCount == 0) {
					Debug.LogError("GameObject " + this.name + " is Missing a Material and will NOT be Included");
					DestroyImmediate(this);
					WasDeleted = true;
					return;
				}
				if(SharedMaterials[i].shader.name.Contains("InternalErrorShader")) {
					SharedMaterials[i].shader = Shader.Find("Standard");
				}
			}
			int[] Index = new int[SubMeshCount];
			bool NeedsRedo = InitializeArrayWithIndex(ref Names, "", SharedMaterials, ref Index);
			InitializeArray<float>(ref Rotation, 0, Index, NeedsRedo);
			InitializeArray<int>(ref Flags, 0, Index, NeedsRedo);

			InitializeArray<Vector2>(ref SecondaryTextureScale, new Vector2(1,1), Index, NeedsRedo);
			InitializeArray<Vector4>(ref MainTexScaleOffset, new Vector4(1,1,0,0), Index, NeedsRedo);
			InitializeArray<Vector4>(ref SecondaryAlbedoTexScaleOffset, new Vector4(1,1,0,0), Index, NeedsRedo);
			InitializeArray<Vector3>(ref BlendColor, new Vector3(1,1,1), Index, NeedsRedo);
			InitializeArray<float>(ref BlendFactor, 0, Index, NeedsRedo);
			InitializeArray<float>(ref Hue, 0, Index, NeedsRedo);
			InitializeArray<float>(ref Saturation, 1, Index, NeedsRedo);
			InitializeArray<float>(ref Brightness, 1, Index, NeedsRedo);
			InitializeArray<float>(ref Contrast, 1, Index, NeedsRedo);
			InitializeArray<Vector2>(ref MetallicRemap, new Vector2(0,1), Index, NeedsRedo);
			InitializeArray<Vector2>(ref RoughnessRemap, new Vector2(0,1), Index, NeedsRedo);
			InitializeArray<float>(ref NormalStrength, 1, Index, NeedsRedo);
			InitializeArray<float>(ref AlphaCutoff, 0.1f, Index, NeedsRedo);
			InitializeArray<float>(ref ScatterDist, 0.1f, Index, NeedsRedo);
			InitializeArray<int>(ref Indexes, 0, Index, NeedsRedo, Mathf.Max(mesh.subMeshCount, SubMeshCount));
			InitializeArray<float>(ref Specular, 0, Index, NeedsRedo);
			InitializeArray<bool>(ref FollowMaterial, true, Index, NeedsRedo);
			InitializeArray<Vector3>(ref TransmissionColor, new Vector3(1,1,1), Index, NeedsRedo);
			InitializeArray<Vector3>(ref EmissionColor, new Vector3(1,1,1), Index, NeedsRedo);
			InitializeArray<float>(ref IOR, 1.0f, Index, NeedsRedo);
			InitializeArray<float>(ref Metallic, 0.0f, Index, NeedsRedo);
			InitializeArray<float>(ref SpecularTint, 0.0f, Index, NeedsRedo);
			InitializeArray<float>(ref Sheen, 0.0f, Index, NeedsRedo);
			InitializeArray<float>(ref SheenTint, 0.0f, Index, NeedsRedo);
			InitializeArray<float>(ref ClearCoat, 0.0f, Index, NeedsRedo);
			InitializeArray<float>(ref ClearCoatGloss, 0.0f, Index, NeedsRedo);
			InitializeArray<float>(ref Anisotropic, 0.0f, Index, NeedsRedo);
			InitializeArray<float>(ref Flatness, 0.0f, Index, NeedsRedo);
			InitializeArray<float>(ref DiffTrans, 0.0f, Index, NeedsRedo);
			InitializeArray<float>(ref SpecTrans, 0.0f, Index, NeedsRedo);
			InitializeArray<Options>(ref MaterialOptions, Options.Disney, Index, NeedsRedo);
			InitializeArray<int>(ref LocalMaterialIndex, 0, Index, NeedsRedo);
			InitializeArray<float>(ref emission, 0, Index, NeedsRedo);
			InitializeArray<float>(ref Roughness, 0, Index, NeedsRedo);
			InitializeArray<Vector3>(ref BaseColor, new Vector3(1,1,1), Index, NeedsRedo);
			InitializeArray<int>(ref MaterialIndex, 0, Index, NeedsRedo);
			InitializeArray<bool>(ref UseKelvin, false, Index, NeedsRedo);
			InitializeArray<float>(ref KelvinTemp, 0, Index, NeedsRedo);
			InitializeArray<float>(ref ColorBleed, 1, Index, NeedsRedo);
			InitializeArray<float>(ref AlbedoBlendFactor, 1, Index, NeedsRedo);

			IsReady = true;
			mesh = null;
		}

		public void ResetData() {
			emission = null;
			Roughness = null;
			TransmissionColor = null;
			MaterialOptions = null;
			BaseColor = null;
		}
		public void ForceUpdateParent() {
			bool Fine = TryGetComponent<ParentObject>(out ParentObject ThisParent);
			Fine = transform.parent.TryGetComponent<ParentObject>(out ParentObject ParParent) || Fine;
	    	if(gameObject.scene.isLoaded && Fine) {
	    		matfill();
	    		if(WasDeleted) return;
		    	if(ThisParent != null) {
		    		ThisParent.NeedsToUpdate = true;
					if((ThisParent.QueInProgress == 0 || ThisParent.QueInProgress == 1) && AssetManager.Assets != null && AssetManager.Assets.UpdateQue != null && !AssetManager.Assets.UpdateQue.Contains(ThisParent)) {
						ThisParent.QueInProgress = 2;
						AssetManager.Assets.UpdateQue.Add(ThisParent);
					}
		    	} else if(ParParent != null) {
		    		ParParent.NeedsToUpdate = true;
					if((ParParent.QueInProgress == 0 || ParParent.QueInProgress == 1) && AssetManager.Assets != null && AssetManager.Assets.UpdateQue != null && !AssetManager.Assets.UpdateQue.Contains(ParParent)) {
						ParParent.QueInProgress = 2;
						AssetManager.Assets.UpdateQue.Add(ParParent);
					}
	    		}
	    	}			
		}
	    private void OnEnable() {
			bool Fine = TryGetComponent<ParentObject>(out ParentObject ThisParent);
			if(transform.parent != null) Fine = transform.parent.TryGetComponent<ParentObject>(out ParentObject ParParent) || Fine;
	    	if(gameObject.scene.isLoaded && Fine) {
	    		matfill();
	    		if(WasDeleted) return;
		    	if(ThisParent != null) {
		    		ThisParent.NeedsToUpdate = true;
		    		if(ThisParent.enabled) {
						if((ThisParent.QueInProgress == 0 || ThisParent.QueInProgress == 1) && AssetManager.Assets != null && AssetManager.Assets.UpdateQue != null && !AssetManager.Assets.UpdateQue.Contains(ThisParent)) {
							ThisParent.QueInProgress = 2;
							AssetManager.Assets.UpdateQue.Add(ThisParent);
						}
					} else {
						ThisParent.enabled = true;
					}
		    	} else if(transform.parent.TryGetComponent<ParentObject>(out ParentObject ParParent)) {
		    		ParParent.NeedsToUpdate = true;
		    		if(ParParent.enabled) {
						if((ParParent.QueInProgress == 0 || ParParent.QueInProgress == 1) && AssetManager.Assets != null && AssetManager.Assets.UpdateQue != null && !AssetManager.Assets.UpdateQue.Contains(ParParent)) {
							ParParent.QueInProgress = 2;
							AssetManager.Assets.UpdateQue.Add(ParParent);
						}
					} else {
						ParParent.enabled = true;
					}
	    		}
	    	}
	    }

	    private void OnDisable() {
	    	if(transform.parent != null && gameObject.scene.isLoaded && transform.parent.TryGetComponent<ParentObject>(out ParentObject ParParent)) {
	    		ParParent.NeedsToUpdate = true;
	    		if((ParParent.QueInProgress == 0 || ParParent.QueInProgress == 1) && AssetManager.Assets != null && AssetManager.Assets.UpdateQue != null && !AssetManager.Assets.UpdateQue.Contains(ParParent)) {
					ParParent.QueInProgress = 2;
	    			AssetManager.Assets.UpdateQue.Add(ParParent);
	    		}
	    	} else if(gameObject.scene.isLoaded && TryGetComponent<ParentObject>(out ParentObject ThisParent)) {
		    	ThisParent.NeedsToUpdate = true;
	    	}
	    }

	}
}