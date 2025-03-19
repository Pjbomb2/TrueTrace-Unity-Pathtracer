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

		[SerializeField] public RayObjMat[] LocalMaterials;
		[SerializeField] public float[] KelvinTemp;
		[SerializeField] public bool[] UseKelvin;
	
		public int[] Indexes;
		public bool NeedsToUpdate;
		[SerializeField] public bool IsReady = false;
		[SerializeField] public bool InvisibleOverride = false;
		[SerializeField] public bool[] FollowMaterial;
		[SerializeField] public Material[] SharedMaterials;
		[SerializeField] public string[] Names;

		[HideInInspector] public int[] MaterialIndex;
		[HideInInspector] public int[] LocalMaterialIndex;
		[HideInInspector] public bool JustCreated = true;
		[HideInInspector] public bool TilingChanged = false;
		[HideInInspector] public int MatOffset = 0;
		private bool WasDeleted = false;

		public void CallMaterialOverride() {
			Material[] SharedMaterials = (GetComponent<Renderer>() != null) ? GetComponent<Renderer>().sharedMaterials : GetComponent<SkinnedMeshRenderer>().sharedMaterials;
			int NamLen = Names.Length;
			for(int i = 0; i < NamLen; i++) {
				Names[i] = SharedMaterials[i].name;
				 if(FollowMaterial[i]) {
					int Index = AssetManager.ShaderNames.IndexOf(SharedMaterials[i].shader.name);
					 if (Index == -1) {
					 	Debug.Log("Material Not Added");
					 	return;
					 }
					 MaterialShader RelevantMat = AssetManager.data.Material[Index];
					if(!RelevantMat.MetallicRange.Equals("null")) LocalMaterials[i].Metallic = SharedMaterials[i].GetFloat(RelevantMat.MetallicRange);
                    if(!RelevantMat.RoughnessRange.Equals("null")) LocalMaterials[i].Roughness = SharedMaterials[i].GetFloat(RelevantMat.RoughnessRange);
                    if(!RelevantMat.BaseColorValue.Equals("null")) LocalMaterials[i].BaseColor = new Vector3(SharedMaterials[i].GetColor(RelevantMat.BaseColorValue).r, SharedMaterials[i].GetColor(RelevantMat.BaseColorValue).g, SharedMaterials[i].GetColor(RelevantMat.BaseColorValue).b);
                    else LocalMaterials[i].BaseColor = Vector3.one;
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
					int NamLength = Names.Length;
					for(int i = 0; i < NamLength; i++) {
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

		private int[] Index;
		public void matfill() {
			TilingChanged = false;
			WasDeleted = false;
			#if UNITY_EDITOR
				UnityEditor.GameObjectUtility.SetStaticEditorFlags(gameObject, UnityEditor.GameObjectUtility.GetStaticEditorFlags(gameObject) & ~UnityEditor.StaticEditorFlags.BatchingStatic);
			#endif
			 Mesh mesh = null;
			 if(TryGetComponent<MeshRenderer>(out MeshRenderer MeshRend)) { 
			 	if(TryGetComponent<MeshFilter>(out MeshFilter MeshFilt)) { 
			 		mesh = MeshFilt.sharedMesh;
			 	}
			 	if(!MeshRend.enabled) mesh = null;
			 	SubMeshCount = (MeshRend.sharedMaterials).Length;
			 	if(TryGetComponent<Renderer>(out Renderer Rend)) SharedMaterials = Rend.sharedMaterials;
		 	} else if(TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer SkinnedRend)) {
                mesh = SkinnedRend.sharedMesh;		 		
			 	if(!SkinnedRend.enabled) mesh = null;
				SubMeshCount = (SkinnedRend.sharedMaterials).Length;
		 		SharedMaterials = SkinnedRend.sharedMaterials;
		 	} else {
		 		DestroyImmediate(this);
		 		WasDeleted = true;
		 		return;
		 	}
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
		 	int MatLength = SharedMaterials.Length;
			for(int i = 0; i < MatLength; i++) {	
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
			if(Index == null || Index.Length != SubMeshCount) Index = new int[SubMeshCount];
			else {for(int i = 0; i < SubMeshCount; i++) Index[i] = 0;}
			
			bool NeedsRedo = InitializeArrayWithIndex(ref Names, "", SharedMaterials, ref Index);
			InitializeArray<int>(ref Indexes, 0, Index, NeedsRedo, Mathf.Max(mesh.subMeshCount, SubMeshCount));
			InitializeArray<bool>(ref FollowMaterial, true, Index, NeedsRedo);
			InitializeArray<int>(ref LocalMaterialIndex, 0, Index, NeedsRedo);
			InitializeArray<int>(ref MaterialIndex, 0, Index, NeedsRedo);
			InitializeArray<bool>(ref UseKelvin, false, Index, NeedsRedo);
			InitializeArray<float>(ref KelvinTemp, 0, Index, NeedsRedo);
			InitializeArray<RayObjMat>(ref LocalMaterials, CommonFunctions.ZeroConstructorMat(), Index, NeedsRedo);


			IsReady = true;
			mesh = null;
		}

		public void ResetData() {
			LocalMaterials = null;
			// Roughness = null;
			// TransmissionColor = null;
			// MaterialOptions = null;
			// BaseColor = null;
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