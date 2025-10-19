using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CommonVars;

namespace TrueTrace {
	[ExecuteInEditMode][System.Serializable]
	public class RayTracingObject : MonoBehaviour, ISerializationCallbackReceiver {
		public enum Options {Disney, Cutout, Fade};
		public enum BlendModes {Lerp, Add, Multiply};
		[SerializeField] public Options[] MaterialOptions;

		[SerializeField] public RayObjMat[] LocalMaterials;
		[SerializeField] public float[] KelvinTemp;
		[SerializeField] public bool[] UseKelvin;

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
		[SerializeField] public Vector4[] SecondaryTextureScaleOffset;
		[SerializeField] public Vector4[] NormalTexScaleOffset;
		[SerializeField] public float[] RotationNormal;
		[SerializeField] public float[] RotationSecondary;
		[SerializeField] public float[] RotationSecondaryDiffuse;
		[SerializeField] public float[] RotationSecondaryNormal;
		[SerializeField] public float[] Rotation;
		[SerializeField] public int[] Flags;
		[SerializeField] public float[] ColorBleed; 
		[SerializeField] public float[] AlbedoBlendFactor; 
		[SerializeField] public float[] SecondaryNormalTexBlend;
		[SerializeField] public float[] DetailNormalStrength;
		[SerializeField] public Vector4[] SecondaryNormalTexScaleOffset;
		[SerializeField] public bool DeleteObject = true;


	
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


		public void UpdateParentChain() {
			bool Fine = TryGetComponent<ParentObject>(out ParentObject ThisParent);
			ParentObject ParParent = null;
			if(transform.root != transform) Fine = transform.parent.TryGetComponent<ParentObject>(out ParParent) || Fine;
	    	if(gameObject.scene.isLoaded && Fine) {
	    		if(WasDeleted) return;
		    	if(ThisParent != null) {
		    		int IndexLength = LocalMaterialIndex.Length;
		    		for(int i = 0; i < IndexLength; i++) {
		    			MaterialData TempMat = ThisParent._Materials[LocalMaterialIndex[i]];
		    			TempMat.MatData = LocalMaterials[i];
		    			ThisParent._Materials[LocalMaterialIndex[i]] = TempMat;
		    		}

		    	} else if(ParParent != null) {
		    		int IndexLength = LocalMaterialIndex.Length;
		    		for(int i = 0; i < IndexLength; i++) {
		    			MaterialData TempMat = ParParent._Materials[LocalMaterialIndex[i]];
		    			TempMat.MatData = LocalMaterials[i];
		    			ParParent._Materials[LocalMaterialIndex[i]] = TempMat;
		    		}
	    		}
	    	}			
		}

		public void CallMaterialOverride(bool Save = true, bool CameFromEdit = false) {
			Material[] SharedMaterials;
			if(TryGetComponent<Renderer>(out Renderer TempRenderer)) {
				SharedMaterials = TempRenderer.sharedMaterials;
			} else SharedMaterials = GetComponent<SkinnedMeshRenderer>().sharedMaterials;
			int NamLen = Names.Length;
			DeleteObject = false;
			// this.hideFlags = HideFlags.None;
			if(Save) {
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
			}
			if(!CameFromEdit && gameObject.activeInHierarchy && AssetManager.Assets != null) AssetManager.Assets.MaterialsChanged.Add(this);
		}


		public void CallMaterialEdited(bool Do = false, bool DoSave = true) {
			bool WasEdited = false;
			if(Application.isPlaying) {
				if(gameObject.activeInHierarchy && AssetManager.Assets != null) {
					AssetManager.Assets.MaterialsChanged.Add(this);
					UpdateParentChain();
					WasEdited = true;
				}
			}
			System.Array.Fill(FollowMaterial, false);
			CallMaterialOverride(DoSave, WasEdited);
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

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
		TransmissionColor = null;
		BaseColor = null;
		MetallicRemap = null;
		RoughnessRemap = null;
		emission = null; 
		EmissionColor = null;
		Roughness = null;
		IOR = null;
		Metallic = null;
		SpecularTint = null;
		Sheen = null;
		SheenTint = null;
		ClearCoat = null;
		ClearCoatGloss = null;
		Anisotropic = null;
		Flatness = null;
		DiffTrans = null;
		SpecTrans = null;
		ScatterDist = null;
		Specular = null;
		AlphaCutoff = null;
		NormalStrength = null;
		Hue = null;
		Saturation = null;
		Brightness = null;
		Contrast = null;
		BlendColor = null;
		BlendFactor = null;
		MainTexScaleOffset = null;
		SecondaryAlbedoTexScaleOffset = null;
		SecondaryTextureScaleOffset = null;
		NormalTexScaleOffset = null;
		RotationNormal = null;
		RotationSecondary = null;
		RotationSecondaryDiffuse = null;
		RotationSecondaryNormal = null;
		Rotation = null;
		Flags = null;
		ColorBleed = null; 
		AlbedoBlendFactor = null; 
		SecondaryNormalTexBlend = null;
		DetailNormalStrength = null;
		SecondaryNormalTexScaleOffset = null;

    }
	    void ISerializationCallbackReceiver.OnAfterDeserialize()
	    {
	    	if(AlphaCutoff != null && AlphaCutoff.Length != 0) {
	    		if(LocalMaterials == null || LocalMaterials.Length == 0) {
	    			LocalMaterials = new RayObjMat[AlphaCutoff.Length];
	    		}
	    		for(int i = 0; i < AlphaCutoff.Length; i++) {
			        if(BaseColor != null && BaseColor.Length != 0)LocalMaterials[i].BaseColor = BaseColor[i];
			        if(emission != null && emission.Length != 0) LocalMaterials[i].emission = emission[i];
			        if(EmissionColor != null && EmissionColor.Length != 0) LocalMaterials[i].EmissionColor = EmissionColor[i];
			        if(Flags != null && Flags.Length != 0) LocalMaterials[i].Tag = Flags[i];
			        if(Roughness != null && Roughness.Length != 0) LocalMaterials[i].Roughness = Roughness[i];
			        if(MaterialOptions != null && MaterialOptions.Length != 0) LocalMaterials[i].MatType = (int)MaterialOptions[i];
			        if(TransmissionColor != null && TransmissionColor.Length != 0) LocalMaterials[i].TransmittanceColor = TransmissionColor[i];
			        if(IOR != null && IOR.Length != 0) LocalMaterials[i].IOR = IOR[i];
			        if(Metallic != null && Metallic.Length != 0) LocalMaterials[i].Metallic = Metallic[i];
			        if(Sheen != null && Sheen.Length != 0) LocalMaterials[i].Sheen = Sheen[i];
			        if(SheenTint != null && SheenTint.Length != 0) LocalMaterials[i].SheenTint = SheenTint[i];
			        if(SpecularTint != null && SpecularTint.Length != 0) LocalMaterials[i].SpecularTint = SpecularTint[i];
			        if(ClearCoat != null && ClearCoat.Length != 0) LocalMaterials[i].Clearcoat = ClearCoat[i];
			        if(ClearCoatGloss != null && ClearCoatGloss.Length != 0) LocalMaterials[i].ClearcoatGloss = ClearCoatGloss[i];
			        if(Anisotropic != null && Anisotropic.Length != 0) LocalMaterials[i].Anisotropic = Anisotropic[i];
			        if(Flatness != null && Flatness.Length != 0) LocalMaterials[i].Flatness = Flatness[i];
			        if(DiffTrans != null && DiffTrans.Length != 0) LocalMaterials[i].DiffTrans = DiffTrans[i];
			        if(SpecTrans != null && SpecTrans.Length != 0) LocalMaterials[i].SpecTrans = SpecTrans[i];
			        if(Specular != null && Specular.Length != 0) LocalMaterials[i].Specular = Specular[i];
			        if(ScatterDist != null && ScatterDist.Length != 0) LocalMaterials[i].ScatterDist = ScatterDist[i];
			        if(MetallicRemap != null && MetallicRemap.Length != 0) LocalMaterials[i].MetallicRemap = MetallicRemap[i];
			        if(RoughnessRemap != null && RoughnessRemap.Length != 0) LocalMaterials[i].RoughnessRemap = RoughnessRemap[i];
			        if(AlphaCutoff != null && AlphaCutoff.Length != 0) LocalMaterials[i].AlphaCutoff = AlphaCutoff[i];
			        if(NormalStrength != null && NormalStrength.Length != 0) LocalMaterials[i].NormalStrength = NormalStrength[i];
			        if(Hue != null && Hue.Length != 0) LocalMaterials[i].Hue = Hue[i];
			        if(Saturation != null && Saturation.Length != 0) LocalMaterials[i].Saturation = Saturation[i];
			        if(Contrast != null && Contrast.Length != 0) LocalMaterials[i].Contrast = Contrast[i];
			        if(Brightness != null && Brightness.Length != 0) LocalMaterials[i].Brightness = Brightness[i];
			        if(BlendColor != null && BlendColor.Length != 0) LocalMaterials[i].BlendColor = BlendColor[i];
			        if(BlendFactor != null && BlendFactor.Length != 0) LocalMaterials[i].BlendFactor = BlendFactor[i];
			        if(ColorBleed != null && ColorBleed.Length != 0) LocalMaterials[i].ColorBleed = ColorBleed[i];
			        if(AlbedoBlendFactor != null && AlbedoBlendFactor.Length != 0) LocalMaterials[i].AlbedoBlendFactor = AlbedoBlendFactor[i];
			        if(SecondaryNormalTexBlend != null && SecondaryNormalTexBlend.Length != 0) LocalMaterials[i].SecondaryNormalTexBlend = SecondaryNormalTexBlend[i];
			        if(DetailNormalStrength != null && DetailNormalStrength.Length != 0) LocalMaterials[i].DetailNormalStrength = DetailNormalStrength[i];

			 		if(MainTexScaleOffset != null && MainTexScaleOffset.Length != 0) LocalMaterials[i].TextureModifiers.MainTexScaleOffset = MainTexScaleOffset[i];
			        if(SecondaryTextureScaleOffset != null && SecondaryTextureScaleOffset.Length != 0) LocalMaterials[i].TextureModifiers.SecondaryTextureScaleOffset = SecondaryTextureScaleOffset[i];
			        if(NormalTexScaleOffset != null && NormalTexScaleOffset.Length != 0) LocalMaterials[i].TextureModifiers.NormalTexScaleOffset = NormalTexScaleOffset[i];
			        if(SecondaryAlbedoTexScaleOffset != null && SecondaryAlbedoTexScaleOffset.Length != 0) LocalMaterials[i].TextureModifiers.SecondaryAlbedoTexScaleOffset = SecondaryAlbedoTexScaleOffset[i];
			        if(SecondaryNormalTexScaleOffset != null && SecondaryNormalTexScaleOffset.Length != 0) LocalMaterials[i].TextureModifiers.SecondaryNormalTexScaleOffset = SecondaryNormalTexScaleOffset[i];
			        if(Rotation != null && Rotation.Length != 0) LocalMaterials[i].TextureModifiers.Rotation = Rotation[i];
			        if(RotationNormal != null && RotationNormal.Length != 0) LocalMaterials[i].TextureModifiers.RotationNormal = RotationNormal[i];
			        if(RotationSecondary != null && RotationSecondary.Length != 0) LocalMaterials[i].TextureModifiers.RotationSecondary = RotationSecondary[i];
			        if(RotationSecondaryDiffuse != null && RotationSecondaryDiffuse.Length != 0) LocalMaterials[i].TextureModifiers.RotationSecondaryDiffuse = RotationSecondaryDiffuse[i];
			        if(RotationSecondaryNormal != null && RotationSecondaryNormal.Length != 0) LocalMaterials[i].TextureModifiers.RotationSecondaryNormal = RotationSecondaryNormal[i];
	    		}
	    	}

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