#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CommonVars;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace TrueTrace {
    public class SavePopup : PopupWindowContent
    {
       RayObjFolderMaster PresetMaster; 
        string PresetName = "Null";
        RayTracingObject ThisOBJ;
        int SaveIndex;

        public SavePopup(RayTracingObject ThisOBJ, int SaveIndex) {
            this.ThisOBJ = ThisOBJ;
            this.SaveIndex = SaveIndex;
            UpdateList();
        }
        public override Vector2 GetWindowSize()
        {
            return new Vector2(460, 300);
        }
        Vector2 ScrollPosition;
        string FolderName = "";
        int CopyIndex;
        int FolderIndex;

        void UpdateList() {
            UnityEditor.AssetDatabase.Refresh();   
            using (var A = new StringReader(Resources.Load<TextAsset>("Utility/MaterialPresets").text)) {
                var serializer = new XmlSerializer(typeof(RayObjFolderMaster));
                PresetMaster = serializer.Deserialize(A) as RayObjFolderMaster;
            }
        }
        void Init(Rect rect) {
            UpdateList();
            OnGUI(new Rect(0,0,100,10));
        }


        public override void OnGUI(Rect rect) {
            // Debug.Log("ONINSPECTORGUI");

            GUILayout.BeginHorizontal();
                GUILayout.Label("Preset Name: ");
                PresetName = GUILayout.TextField(PresetName, 32);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
                GUILayout.Label("Folder Name: ");
                FolderName = GUILayout.TextField(FolderName, 32);
            GUILayout.EndHorizontal();

            CopyIndex = -1;
            FolderIndex = -1;
            
            {
                int FolderCount = PresetMaster.PresetFolders.Count;
                GUILayout.BeginScrollView(ScrollPosition, GUILayout.Width(100), GUILayout.Height(200));
                for(int i = 0; i < FolderCount; i++) {
                    if(!PresetMaster.PresetFolders[i].FolderName.Equals("COPYPASTEBUFFER")) {
                        if(GUILayout.Button(PresetMaster.PresetFolders[i].FolderName)) {
                            FolderName = PresetMaster.PresetFolders[i].FolderName;
                            FolderIndex = i;
                        }                  
                    }
                }
                GUILayout.EndScrollView();
            }


            
            if(GUILayout.Button("Save Preset")) {
                int FolderCount = PresetMaster.PresetFolders.Count;
                for(int j = 0; j < FolderCount; j++) {
                     if(PresetMaster.PresetFolders[j].FolderName.Equals(FolderName)) {
                        FolderIndex = j;
                        break;
                     }
                }
                if(FolderIndex == -1) {
                    PresetMaster.PresetFolders.Add(new RayObjFolder() {
                        FolderName = FolderName,
                        ContainedPresets = new List<RayObjectDatas>()
                    });
                    FolderIndex = PresetMaster.PresetFolders.Count - 1;
                }
                int RayReadCount = PresetMaster.PresetFolders[FolderIndex].ContainedPresets.Count;
                for(int j = 0; j < RayReadCount; j++) {
                    if(PresetMaster.PresetFolders[FolderIndex].ContainedPresets[j].MatName.Equals(PresetName)) {
                        CopyIndex = j;
                        break;
                    }
                }

                Material TempMat = ThisOBJ.SharedMaterials[SaveIndex];
                int MatIndex = AssetManager.ShaderNames.IndexOf(TempMat.shader.name);
                MaterialShader RelevantMat;
                string AlbedoGUID = "null";
                string MetallicGUID = "null";
                string RoughnessGUID = "null";
                string EmissionGUID = "null";
                string AlphaGUID = "null";
                string MatCapGUID = "null";
                string MatcapMaskGUID = "null";
                string SecondaryAlbedoGUID = "null";
                string SecondaryAlbedoMaskGUID = "null";
                string NormalGUID = "null";
                if(MatIndex != -1) {
                    RelevantMat = AssetManager.data.Material[MatIndex];
                    int TexCount = RelevantMat.AvailableTextures.Count;
                    for(int i2 = 0; i2 < TexCount; i2++) {
                        string TexName = RelevantMat.AvailableTextures[i2].TextureName;
                        if(TempMat.HasProperty(TexName) && TempMat.GetTexture(TexName) != null) {
                            switch((TexturePurpose)RelevantMat.AvailableTextures[i2].Purpose) {
                                case(TexturePurpose.SecondaryAlbedoTextureMask):
                                    SecondaryAlbedoMaskGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.SecondaryAlbedoTexture):
                                    SecondaryAlbedoGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.MatCapTex):
                                    MatCapGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.Normal):
                                    NormalGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.Emission):
                                    EmissionGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.Metallic):
                                    MetallicGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.Roughness):
                                    RoughnessGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.Alpha):
                                    AlphaGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.MatCapMask):
                                    MatcapMaskGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.Albedo):
                                    AlbedoGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                            }
                        }
                    }
                }
                else Debug.LogError("FUCK: " + ThisOBJ.SharedMaterials[SaveIndex].shader.name);





                RayObjectDatas TempRay = new RayObjectDatas() {
                    ID = 0,
                    MatName = PresetName,
                    MatData = ThisOBJ.LocalMaterials[SaveIndex],
                    
                    UseKelvin = ThisOBJ.UseKelvin[SaveIndex],
                    KelvinTemp = ThisOBJ.KelvinTemp[SaveIndex],

                    AlbedoGUID = AlbedoGUID,
                    MetallicGUID = MetallicGUID,
                    RoughnessGUID = RoughnessGUID,
                    EmissionGUID = EmissionGUID,
                    NormalGUID = NormalGUID,
                    AlphaGUID = AlphaGUID,
                    MatCapGUID = MatCapGUID,
                    MatcapMaskGUID = MatcapMaskGUID,
                    SecondaryAlbedoGUID = SecondaryAlbedoGUID,
                    SecondaryAlbedoMaskGUID = SecondaryAlbedoMaskGUID,

                    ShaderName = TempMat.shader.name
                };
                if(CopyIndex != -1) PresetMaster.PresetFolders[FolderIndex].ContainedPresets[CopyIndex] = TempRay;
                else PresetMaster.PresetFolders[FolderIndex].ContainedPresets.Add(TempRay);
                string materialPresetsPath = TTPathFinder.GetMaterialPresetsPath();
                using(StreamWriter writer = new StreamWriter(materialPresetsPath)) {
                    var serializer = new XmlSerializer(typeof(RayObjFolderMaster));
                    serializer.Serialize(writer.BaseStream, PresetMaster);
                }
                this.editorWindow.Close();
            }


        }
    }
    public class LoadPopup : PopupWindowContent
    {
        Vector2 ScrollPosition;
        RayTracingObjectEditor SourceWindow;
        RayObjFolderMaster PresetMaster;
        public LoadPopup(RayTracingObjectEditor editor) {
            this.SourceWindow = editor;
            UpdateList();
        }
        private void CallEditorFunction(RayObjectDatas RayObj, bool LoadTextures) {
            if(SourceWindow != null) {
                SourceWindow.LoadFunction(RayObj, LoadTextures);
            }
        }
        public override Vector2 GetWindowSize()
        {
            return new Vector2(460, 250);
        }
        bool[] FoldoutBool;
        void UpdateList() {
            UnityEditor.AssetDatabase.Refresh();
            using (var A = new StringReader(Resources.Load<TextAsset>("Utility/MaterialPresets").text)) {
                var serializer = new XmlSerializer(typeof(RayObjFolderMaster));
                PresetMaster = serializer.Deserialize(A) as RayObjFolderMaster;
            }
        }
        void Init(Rect rect) {
            UpdateList();
            OnGUI(new Rect(0,0,100,10));
        }
        public override void OnGUI(Rect rect) {
            int FolderLength = PresetMaster.PresetFolders.Count;
            if(FoldoutBool == null) FoldoutBool = new bool[FolderLength];
            ScrollPosition = GUILayout.BeginScrollView(ScrollPosition, GUILayout.Width(460), GUILayout.Height(250));
            string materialPresetsPath = TTPathFinder.GetMaterialPresetsPath();
            for(int j = 0; j < FolderLength; j++) {
                if(!PresetMaster.PresetFolders[j].FolderName.Equals("COPYPASTEBUFFER")) {
                    GUILayout.BeginHorizontal();
                        FoldoutBool[j] = EditorGUILayout.Foldout(FoldoutBool[j], PresetMaster.PresetFolders[j].FolderName);
                        if(GUILayout.Button("Delete")) {
                            PresetMaster.PresetFolders.RemoveAt(j);
                            using(StreamWriter writer = new StreamWriter(materialPresetsPath)) {
                                var serializer = new XmlSerializer(typeof(RayObjFolderMaster));
                                serializer.Serialize(writer.BaseStream, PresetMaster);
                                UnityEditor.AssetDatabase.Refresh();
                            }
                            Init(new Rect(0,0,100,10));   
                        }
                    GUILayout.EndHorizontal();
                    if(FoldoutBool[j]) {
                         if (Selection.activeTransform) {
                            int PresetLength = PresetMaster.PresetFolders[j].ContainedPresets.Count;
                            for(int i = 0; i < PresetLength; i++) {
                                GUILayout.BeginHorizontal();
                                    if(GUILayout.Button(PresetMaster.PresetFolders[j].ContainedPresets[i].MatName)) {CallEditorFunction(PresetMaster.PresetFolders[j].ContainedPresets[i], true); this.editorWindow.Close();}
                                    if(GUILayout.Button("Load Without Textures")) {CallEditorFunction(PresetMaster.PresetFolders[j].ContainedPresets[i], false); this.editorWindow.Close();}
                                    if(GUILayout.Button("Delete")) {
                                        PresetMaster.PresetFolders[j].ContainedPresets.RemoveAt(i);
                                        using(StreamWriter writer = new StreamWriter(materialPresetsPath)) {
                                            var serializer = new XmlSerializer(typeof(RayObjFolderMaster));
                                            serializer.Serialize(writer.BaseStream, PresetMaster);
                                            UnityEditor.AssetDatabase.Refresh();
                                        }
                                        Init(new Rect(0,0,100,10));
                                    }
                                GUILayout.EndHorizontal();
                            }
                        }
                    }
                }
            }
            GUILayout.EndScrollView();
        }
    }

    [CustomEditor(typeof(RayTracingObject))]
    public class RayTracingObjectEditor : Editor
    {
        int Selected = 0;
        public void SetSelected(int A) {
            Selected = A;
            Repaint();
        }

        string[] TheseNames;
        RayTracingObject t;
        void OnEnable()
        {
            (target as RayTracingObject).matfill();
        }

        public void LoadFunction(RayObjectDatas RayObj, bool LoadTextures) {
            t.LocalMaterials[Selected] = RayObj.MatData;
            t.UseKelvin[Selected] = RayObj.UseKelvin;
            t.KelvinTemp[Selected] = RayObj.KelvinTemp;

            if(LoadTextures) {
                Material TempMat = t.SharedMaterials[Selected];
                int MatIndex = AssetManager.ShaderNames.IndexOf(TempMat.shader.name);
                MaterialShader RelevantMat;
                if(MatIndex != -1) {
                    // if(TempMat.shader.name.Equals(RayObj.ShaderName)) {
                        RelevantMat = AssetManager.data.Material[MatIndex];
                        int TexCount = RelevantMat.AvailableTextures.Count;
                        for(int i2 = 0; i2 < TexCount; i2++) {
                            string TexName = RelevantMat.AvailableTextures[i2].TextureName;
                            switch((TexturePurpose)RelevantMat.AvailableTextures[i2].Purpose) {
                                case(TexturePurpose.Albedo):
                                    if (TempMat.HasProperty(TexName)) {
                                        if(!(RayObj.AlbedoGUID.Equals("null"))) {
                                            Texture2D TextureAsset = (AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(RayObj.AlbedoGUID), typeof(Texture)) as Texture2D);
                                            if(TextureAsset != null) TempMat.SetTexture(TexName, TextureAsset);
                                            else Debug.LogError("Missing Texture Asset At " + AssetDatabase.GUIDToAssetPath(RayObj.AlbedoGUID));
                                        } else TempMat.SetTexture(TexName, null);
                                    }
                                break;
                                case(TexturePurpose.SecondaryAlbedoTextureMask):
                                    if (TempMat.HasProperty(TexName)) {
                                        if(!(RayObj.SecondaryAlbedoMaskGUID.Equals("null"))) {
                                            Texture2D TextureAsset = (AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(RayObj.SecondaryAlbedoMaskGUID), typeof(Texture)) as Texture2D);
                                            if(TextureAsset != null) TempMat.SetTexture(TexName, TextureAsset);
                                            else Debug.LogError("Missing Texture Asset At " + AssetDatabase.GUIDToAssetPath(RayObj.SecondaryAlbedoMaskGUID));
                                        } else TempMat.SetTexture(TexName, null);
                                    }
                                break;
                                case(TexturePurpose.SecondaryAlbedoTexture):
                                    if (TempMat.HasProperty(TexName)) {
                                        if(!(RayObj.SecondaryAlbedoGUID.Equals("null"))) {
                                            Texture2D TextureAsset = (AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(RayObj.SecondaryAlbedoGUID), typeof(Texture)) as Texture2D);
                                            if(TextureAsset != null) TempMat.SetTexture(TexName, TextureAsset);
                                            else Debug.LogError("Missing Texture Asset At " + AssetDatabase.GUIDToAssetPath(RayObj.SecondaryAlbedoGUID));
                                        } else TempMat.SetTexture(TexName, null);
                                    }
                                break;
                                case(TexturePurpose.MatCapTex):
                                    if (TempMat.HasProperty(TexName)) {
                                        if(!(RayObj.MatCapGUID.Equals("null"))) {
                                            Texture2D TextureAsset = (AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(RayObj.MatCapGUID), typeof(Texture)) as Texture2D);
                                            if(TextureAsset != null) TempMat.SetTexture(TexName, TextureAsset);
                                            else Debug.LogError("Missing Texture Asset At " + AssetDatabase.GUIDToAssetPath(RayObj.MatCapGUID));
                                        } else TempMat.SetTexture(TexName, null);
                                    }
                                break;
                                case(TexturePurpose.Normal):
                                    if (TempMat.HasProperty(TexName)) {
                                        if(!(RayObj.NormalGUID.Equals("null"))) {
                                            Texture2D TextureAsset = (AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(RayObj.NormalGUID), typeof(Texture)) as Texture2D);
                                            if(TextureAsset != null) TempMat.SetTexture(TexName, TextureAsset);
                                            else Debug.LogError("Missing Texture Asset At " + AssetDatabase.GUIDToAssetPath(RayObj.NormalGUID));
                                        } else TempMat.SetTexture(TexName, null);
                                    }
                                break;
                                case(TexturePurpose.Emission):
                                    if (TempMat.HasProperty(TexName)) {
                                        if(!(RayObj.EmissionGUID.Equals("null"))) {
                                            Texture2D TextureAsset = (AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(RayObj.EmissionGUID), typeof(Texture)) as Texture2D);
                                            if(TextureAsset != null) TempMat.SetTexture(TexName, TextureAsset);
                                            else Debug.LogError("Missing Texture Asset At " + AssetDatabase.GUIDToAssetPath(RayObj.EmissionGUID));
                                        } else TempMat.SetTexture(TexName, null);
                                    }
                                break;
                                case(TexturePurpose.Metallic):
                                    if (TempMat.HasProperty(TexName)) {
                                        if(!(RayObj.MetallicGUID.Equals("null"))) {
                                            Texture2D TextureAsset = (AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(RayObj.MetallicGUID), typeof(Texture)) as Texture2D);
                                            if(TextureAsset != null) TempMat.SetTexture(TexName, TextureAsset);
                                            else Debug.LogError("Missing Texture Asset At " + AssetDatabase.GUIDToAssetPath(RayObj.MetallicGUID));
                                        } else TempMat.SetTexture(TexName, null);
                                    }
                                break;
                                case(TexturePurpose.Roughness):
                                    if (TempMat.HasProperty(TexName)) {
                                        if(!(RayObj.RoughnessGUID.Equals("null"))) {
                                            Texture2D TextureAsset = (AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(RayObj.RoughnessGUID), typeof(Texture)) as Texture2D);
                                            if(TextureAsset != null) TempMat.SetTexture(TexName, TextureAsset);
                                            else Debug.LogError("Missing Texture Asset At " + AssetDatabase.GUIDToAssetPath(RayObj.RoughnessGUID));
                                        } else TempMat.SetTexture(TexName, null);
                                    }
                                break;
                                case(TexturePurpose.Alpha):
                                    if (TempMat.HasProperty(TexName)) {
                                        if(!(RayObj.AlphaGUID.Equals("null"))) {
                                            Texture2D TextureAsset = (AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(RayObj.AlphaGUID), typeof(Texture)) as Texture2D);
                                            if(TextureAsset != null) TempMat.SetTexture(TexName, TextureAsset);
                                            else Debug.LogError("Missing Texture Asset At " + AssetDatabase.GUIDToAssetPath(RayObj.AlphaGUID));
                                        } else TempMat.SetTexture(TexName, null);
                                    }
                                break;
                                case(TexturePurpose.MatCapMask):
                                    if (TempMat.HasProperty(TexName)) {
                                        if(!(RayObj.MatcapMaskGUID.Equals("null"))) {
                                            Texture2D TextureAsset = (AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(RayObj.MatcapMaskGUID), typeof(Texture)) as Texture2D);
                                            if(TextureAsset != null) TempMat.SetTexture(TexName, TextureAsset);
                                            else Debug.LogError("Missing Texture Asset At " + AssetDatabase.GUIDToAssetPath(RayObj.MatcapMaskGUID));
                                        } else TempMat.SetTexture(TexName, null);
                                    }
                                break;
                            }
                        }
                    // }
                } else Debug.LogError("FUCK: " + TempMat.shader.name);
                // AssetManager.Assets.ForceUpdateAtlas();
            }
            // else Debug.LogError("FUCK: " + ThisOBJ.SharedMaterials[SaveIndex].shader.name);

            t.CallMaterialEdited(true);


            // OnInspectorGUI();
        }

        public void CopyFunction(RayTracingObject ThisOBJ, int SaveIndex) {
                RayObjFolderMaster PresetMaster;
                int CopyIndex = -1;
                int FolderIndex = -1;
                string FolderName = "COPYPASTEBUFFER";
                string PresetName = "COPYPASTEBUFFER";
                UnityEditor.AssetDatabase.Refresh();
                using (var A = new StringReader(Resources.Load<TextAsset>("Utility/MaterialPresets").text)) {
                    var serializer = new XmlSerializer(typeof(RayObjFolderMaster));
                    PresetMaster = serializer.Deserialize(A) as RayObjFolderMaster;
                    int RayReadCount = PresetMaster.PresetFolders.Count;
                    for(int i = 0; i < RayReadCount; i++) {
                        if(PresetMaster.PresetFolders[i].FolderName.Equals(FolderName)) {
                            FolderIndex = i;
                            int PresetCount = PresetMaster.PresetFolders[i].ContainedPresets.Count;
                            for(int j = 0; j < PresetCount; j++) {
                                if(PresetMaster.PresetFolders[i].ContainedPresets[j].MatName.Equals(PresetName)) {
                                    CopyIndex = j;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }

                Material TempMat = ThisOBJ.SharedMaterials[SaveIndex];
                int MatIndex = AssetManager.ShaderNames.IndexOf(TempMat.shader.name);
                MaterialShader RelevantMat;
                string AlbedoGUID = "null";
                string MetallicGUID = "null";
                string RoughnessGUID = "null";
                string EmissionGUID = "null";
                string AlphaGUID = "null";
                string MatCapGUID = "null";
                string MatcapMaskGUID = "null";
                string SecondaryAlbedoGUID = "null";
                string SecondaryAlbedoMaskGUID = "null";
                string NormalGUID = "null";
                if(MatIndex != -1) {
                    RelevantMat = AssetManager.data.Material[MatIndex];
                    int TexCount = RelevantMat.AvailableTextures.Count;
                    for(int i2 = 0; i2 < TexCount; i2++) {
                        string TexName = RelevantMat.AvailableTextures[i2].TextureName;
                        if(TempMat.HasProperty(TexName) && TempMat.GetTexture(TexName) != null) {
                            switch((TexturePurpose)RelevantMat.AvailableTextures[i2].Purpose) {
                                case(TexturePurpose.SecondaryAlbedoTextureMask):
                                    SecondaryAlbedoMaskGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.SecondaryAlbedoTexture):
                                    SecondaryAlbedoGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.MatCapTex):
                                    MatCapGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.Normal):
                                    NormalGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.Emission):
                                    EmissionGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.Metallic):
                                    MetallicGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.Roughness):
                                    RoughnessGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.Alpha):
                                    AlphaGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.MatCapMask):
                                    MatcapMaskGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                                case(TexturePurpose.Albedo):
                                    AlbedoGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(TempMat.GetTexture(TexName)));
                                break;
                            }
                        }
                    }
                }
                else Debug.LogError("FUCK: " + ThisOBJ.SharedMaterials[SaveIndex].shader.name);



                RayObjectDatas TempRay = new RayObjectDatas() {
                    ID = 0,
                    MatName = PresetName,
                    MatData = ThisOBJ.LocalMaterials[SaveIndex],

                    UseKelvin = ThisOBJ.UseKelvin[SaveIndex],
                    KelvinTemp = ThisOBJ.KelvinTemp[SaveIndex],

                    AlbedoGUID = AlbedoGUID,
                    MetallicGUID = MetallicGUID,
                    RoughnessGUID = RoughnessGUID,
                    EmissionGUID = EmissionGUID,
                    NormalGUID = NormalGUID,
                    AlphaGUID = AlphaGUID,
                    MatCapGUID = MatCapGUID,
                    MatcapMaskGUID = MatcapMaskGUID,
                    SecondaryAlbedoGUID = SecondaryAlbedoGUID,
                    SecondaryAlbedoMaskGUID = SecondaryAlbedoMaskGUID,

                    ShaderName = TempMat.shader.name
                };
                if(CopyIndex != -1) PresetMaster.PresetFolders[FolderIndex].ContainedPresets[CopyIndex] = TempRay;
                else PresetMaster.PresetFolders[FolderIndex].ContainedPresets.Add(TempRay);
                string materialPresetsPath = TTPathFinder.GetMaterialPresetsPath();
                using(StreamWriter writer = new StreamWriter(materialPresetsPath)) {
                    var serializer = new XmlSerializer(typeof(RayObjFolderMaster));
                    serializer.Serialize(writer.BaseStream, PresetMaster);
                    UnityEditor.AssetDatabase.Refresh();
                }
        }
        public void PasteFunction() {
            RayObjFolderMaster PresetMaster;
            UnityEditor.AssetDatabase.Refresh();
            using (var A = new StringReader(Resources.Load<TextAsset>("Utility/MaterialPresets").text)) {
                var serializer = new XmlSerializer(typeof(RayObjFolderMaster));
                PresetMaster = serializer.Deserialize(A) as RayObjFolderMaster;
            }
            int Index = -1;
            int FolderIndex = -1;


            int FolderCount = PresetMaster.PresetFolders.Count;
            for(int i = 0; i < FolderCount; i++) {
                if(PresetMaster.PresetFolders[i].FolderName.Equals("COPYPASTEBUFFER")) {
                    int PresetCount = PresetMaster.PresetFolders[i].ContainedPresets.Count;
                    for(int j = 0; j < PresetCount; j++) {
                        if(PresetMaster.PresetFolders[i].ContainedPresets[j].MatName.Equals("COPYPASTEBUFFER")) {
                            Index = j;
                            break;
                        }
                    }
                    FolderIndex = i;
                    break;
                }
            }
            if(Index == -1) return;
            LoadFunction(PresetMaster.PresetFolders[FolderIndex].ContainedPresets[Index], true);
        }

        Dictionary<string, List<string>> DictionaryLinks;
        Dictionary<string, Rect> ConnectionSources;
        List<string> ConnectionSourceNames;

        private void DrawConnections(Rect A, Rect B)
        {
            Handles.BeginGUI();
            int HorizontalOffset = 30;
            Handles.DrawLine(new Vector3(A.xMin, A.center.y),
                             new Vector3(A.xMin - HorizontalOffset, A.center.y));

            // Draw a line from Specular to Roughness
            Handles.DrawLine(new Vector3(A.xMin - HorizontalOffset, A.center.y),
                             new Vector3(A.xMin - HorizontalOffset, B.center.y));
                             
            Handles.DrawLine(new Vector3(A.xMin - HorizontalOffset, B.center.y),
                             new Vector3(B.xMin, B.center.y));

            Handles.EndGUI();
        }
        private void DrawHighlighter(Rect A) {
            Handles.BeginGUI();
            
            Vector3[] Verts = new Vector3[4];
            Verts[0] = new Vector3(A.xMin, A.yMin);
            Verts[1] = new Vector3(A.xMin, A.yMax);
            Verts[2] = new Vector3(A.xMax, A.yMax);
            Verts[3] = new Vector3(A.xMax, A.yMin);
            Handles.DrawSolidRectangleWithOutline(Verts, new Color(0,0,0,0), new Color(1,0,0,1));

            Handles.EndGUI();
        }

        public override void OnInspectorGUI() {
            if(DictionaryLinks == null) {
                DictionaryLinks = new Dictionary<string, List<string>>();
                DictionaryLinks.Add("BaseColor", new List<string> {
                    "ColorModifiersContainer",
                });                
                DictionaryLinks.Add("Metallic", new List<string> {
                    "Roughness",
                    "Smoothness",
                    "RoughnessRemap",
                    "FlagsContainer",
                    "Anisotropic",
                    "BaseColor",
                });
                DictionaryLinks.Add("Roughness", new List<string> {
                    "Smoothness",
                    "RoughnessRemap",
                    "FlagsContainer",
                    "Thin",
                    "Flatness",
                });
                DictionaryLinks.Add("Emission", new List<string> {
                    "BaseContainer",
                    "EmissionColor",
                    "BaseColor",
                });
                DictionaryLinks.Add("Specular", new List<string> {
                    "Roughness",
                    "Smoothness",
                    "FlagsContainer",
                    "RoughnessRemap",
                    "SpecularTint",
                    "Anisotropic",
                    "IOR"
                });
                DictionaryLinks.Add("ClearCoat", new List<string> {
                    "ClearCoatGloss",
                });
                DictionaryLinks.Add("Sheen", new List<string> {
                    "SheenTint",
                });
                DictionaryLinks.Add("IOR", new List<string> {
                });
                DictionaryLinks.Add("SpecTrans", new List<string> {
                    "BaseContainer",
                    "BaseColor",
                    "ScatterDist",
                    "FlagsContainer",
                    "Thin",
                    "IOR",
                    "Smoothness",
                    "Roughness",
                    "RoughnessRemap",
                    "Anisotropic",
                });
                DictionaryLinks.Add("DiffTrans", new List<string> {
                    "BaseContainer",
                    "FlagsContainer",
                    "BaseColor",
                    "ScatterDist",
                    "Thin",
                    "Smoothness",
                    "Roughness",
                    "TransmissionColor",
                });

            }
                ConnectionSources = new Dictionary<string, Rect>();
                ConnectionSourceNames = new List<string>();
                GUIStyle FoldoutStyle = new GUIStyle(EditorStyles.foldoutHeader);
                GUIStyle FoldoutStyleBolded = new GUIStyle(EditorStyles.foldoutHeader);
                GUIStyle LabelStyleBolded = new GUIStyle(EditorStyles.label);
                FoldoutStyle.fontSize = 15;
                FoldoutStyleBolded.fontSize = 20;
                FoldoutStyleBolded.fontStyle = FontStyle.Bold;
                LabelStyleBolded.fontSize = 20;
                LabelStyleBolded.fontStyle = FontStyle.Bold;
                // FoldoutStyle.fontStyle = FontStyle.Italic;
                var t1 = (targets);
                t =  t1[0] as RayTracingObject;
                TheseNames = t.Names;
                Selected = EditorGUILayout.Popup("Selected Material:", Selected, TheseNames);

                EditorGUILayout.BeginHorizontal();
                    if(GUILayout.Button("Save Material Preset"))
                        UnityEditor.PopupWindow.Show(new Rect(0,0,10,10), new SavePopup(t, Selected));
                    
                    if(GUILayout.Button("Load Material Preset"))
                        UnityEditor.PopupWindow.Show(new Rect(0,0,100,10), new LoadPopup(this));
                EditorGUILayout.EndHorizontal();
                bool QuickPasted = false;
                EditorGUILayout.BeginHorizontal();
                    if(GUILayout.Button("Quick Copy"))
                        CopyFunction(t, Selected);

                    if(GUILayout.Button("Quick Paste")) {
                        PasteFunction();
                        QuickPasted = true;
                    }
                EditorGUILayout.EndHorizontal();




                EditorGUILayout.Space();
                EditorGUI.BeginChangeCheck();
                t.LocalMaterials[Selected].MatType = (int)(RayTracingObject.Options)EditorGUILayout.EnumPopup("Material Type: ", (RayTracingObject.Options)t.LocalMaterials[Selected].MatType);
                int Flag = t.LocalMaterials[Selected].Tag;
               

                RayTracingMaster.RTOShowBase = EditorGUILayout.BeginFoldoutHeaderGroup(RayTracingMaster.RTOShowBase, "Basic Settings", FoldoutStyle);
                ConnectionSources.Add("BaseContainer", GUILayoutUtility.GetLastRect()); // Store position
                ConnectionSourceNames.Add("BaseContainer");
                if(RayTracingMaster.RTOShowBase) {
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space(12.0f, false);
                        EditorGUILayout.BeginVertical();
                            Color BaseCol = EditorGUILayout.ColorField("Base Color", new Color(t.LocalMaterials[Selected].BaseColor.x, t.LocalMaterials[Selected].BaseColor.y, t.LocalMaterials[Selected].BaseColor.z, 1));
                            ConnectionSources.Add("BaseColor", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("BaseColor");
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("BaseColor").vector3Value = new Vector3(BaseCol.r, BaseCol.g, BaseCol.b);

                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("Metallic").floatValue = EditorGUILayout.Slider("Metallic: ", t.LocalMaterials[Selected].Metallic, 0, 1);
                            ConnectionSources.Add("Metallic", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("Metallic");
                            EditorGUILayout.MinMaxSlider("Metallic Remap: ", ref t.LocalMaterials[Selected].MetallicRemap.x, ref t.LocalMaterials[Selected].MetallicRemap.y, 0, 1);
                            ConnectionSources.Add("MetallicRemap", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("MetallicRemap");

                            if(Flag.GetFlag(CommonFunctions.Flags.UseSmoothness)) serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("Roughness").floatValue = 1.0f - EditorGUILayout.Slider("Smoothness: ", 1.0f - t.LocalMaterials[Selected].Roughness, 0, 1);
                            else serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("Roughness").floatValue = EditorGUILayout.Slider("Roughness: ", t.LocalMaterials[Selected].Roughness, 0, 1);
                            ConnectionSources.Add("Roughness", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("Roughness");
                            EditorGUILayout.MinMaxSlider("Roughness Remap: ", ref t.LocalMaterials[Selected].RoughnessRemap.x, ref t.LocalMaterials[Selected].RoughnessRemap.y, 0, 1);
                            ConnectionSources.Add("RoughnessRemap", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("RoughnessRemap");

                            EditorGUILayout.Space();
                            if(t.LocalMaterials[Selected].MatType == 1 || t.LocalMaterials[Selected].MatType == 2) {
                                serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("AlphaCutoff").floatValue = EditorGUILayout.Slider("Alpha Cutoff: ", t.LocalMaterials[Selected].AlphaCutoff, 0.01f, 1.0f);
                                ConnectionSources.Add("AlphaCutoff", GUILayoutUtility.GetLastRect()); // Store position
                                ConnectionSourceNames.Add("AlphaCutoff");
                            }
                
                        EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                EditorGUILayout.Space();
                RayTracingMaster.RTOShowTex = EditorGUILayout.BeginFoldoutHeaderGroup(RayTracingMaster.RTOShowTex, "Textures", FoldoutStyle);
                ConnectionSources.Add("TexturesContainer", GUILayoutUtility.GetLastRect()); // Store position
                ConnectionSourceNames.Add("TexturesContainer");
                if(RayTracingMaster.RTOShowTex) {
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space(12.0f, false);
                        EditorGUILayout.BeginVertical();
                            EditorGUILayout.BeginVertical();
                                GUILayout.Label("Primary Diffuse", LabelStyleBolded);
                                serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("TextureModifiers").FindPropertyRelative("MainTexScaleOffset").vector4Value = EditorGUILayout.Vector4Field("Scale/Offset: ", t.LocalMaterials[Selected].TextureModifiers.MainTexScaleOffset);
                                serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("TextureModifiers").FindPropertyRelative("Rotation").floatValue = EditorGUILayout.Slider("Rotation: ", t.LocalMaterials[Selected].TextureModifiers.Rotation, 0, 360);
                            EditorGUILayout.EndVertical();
            
                            EditorGUILayout.Space();
                            EditorGUILayout.Space();
                            EditorGUILayout.BeginVertical();
                                GUILayout.Label("Secondary Diffuse", LabelStyleBolded);
                                serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("TextureModifiers").FindPropertyRelative("SecondaryAlbedoTexScaleOffset").vector4Value = EditorGUILayout.Vector4Field("Scale/Offset: ", t.LocalMaterials[Selected].TextureModifiers.SecondaryAlbedoTexScaleOffset);
                                serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("TextureModifiers").FindPropertyRelative("RotationSecondaryDiffuse").floatValue = EditorGUILayout.Slider("Rotation: ", t.LocalMaterials[Selected].TextureModifiers.RotationSecondaryDiffuse, 0, 360);
                                Flag = CommonFunctions.SetFlagStretch(Flag, 1, 3, (int)((RayTracingObject.BlendModes)EditorGUILayout.EnumPopup("Blend Mode: ", (RayTracingObject.BlendModes)Flag.GetFlagStretch(1, 3))));
                                serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("AlbedoBlendFactor").floatValue = EditorGUILayout.Slider("Blend Factor: ", t.LocalMaterials[Selected].AlbedoBlendFactor, 0, 1);
                            EditorGUILayout.EndVertical();

                            EditorGUILayout.Space();
                            EditorGUILayout.Space();
                            EditorGUILayout.BeginVertical();
                                GUILayout.Label("Normal Map", LabelStyleBolded);
                                serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("TextureModifiers").FindPropertyRelative("NormalTexScaleOffset").vector4Value = EditorGUILayout.Vector4Field("Scale/Offset: ", t.LocalMaterials[Selected].TextureModifiers.NormalTexScaleOffset);
                                serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("TextureModifiers").FindPropertyRelative("RotationNormal").floatValue = EditorGUILayout.Slider("Rotation: ", t.LocalMaterials[Selected].TextureModifiers.RotationNormal, 0, 360);
                                serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("NormalStrength").floatValue = EditorGUILayout.Slider("Strength: ", t.LocalMaterials[Selected].NormalStrength, 0, 20.0f);
                                ConnectionSources.Add("NormalStrength", GUILayoutUtility.GetLastRect()); // Store position
                                ConnectionSourceNames.Add("NormalStrength");
                            EditorGUILayout.EndVertical();

                            EditorGUILayout.Space();
                            EditorGUILayout.Space();
                            EditorGUILayout.BeginVertical();
                                GUILayout.Label("Secondary Normal Map", LabelStyleBolded);
                                serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("TextureModifiers").FindPropertyRelative("SecondaryNormalTexScaleOffset").vector4Value = EditorGUILayout.Vector4Field("Scale/Offset: ", t.LocalMaterials[Selected].TextureModifiers.SecondaryNormalTexScaleOffset);
                                serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("TextureModifiers").FindPropertyRelative("RotationSecondaryNormal").floatValue = EditorGUILayout.Slider("Rotation: ", t.LocalMaterials[Selected].TextureModifiers.RotationSecondaryNormal, 0, 360);
                                serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("DetailNormalStrength").floatValue = EditorGUILayout.Slider("Strength: ", t.LocalMaterials[Selected].DetailNormalStrength, 0, 20.0f);
                                ConnectionSources.Add("DetailNormalStrength", GUILayoutUtility.GetLastRect()); // Store position
                                ConnectionSourceNames.Add("DetailNormalStrength");
                                serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("SecondaryNormalTexBlend").floatValue = EditorGUILayout.Slider("Blend Factor: ", t.LocalMaterials[Selected].SecondaryNormalTexBlend, 0, 1);
                            EditorGUILayout.EndVertical();

                            EditorGUILayout.Space();
                            EditorGUILayout.Space();
                            EditorGUILayout.BeginVertical();
                                GUILayout.Label("Misc Maps", LabelStyleBolded);
                                serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("TextureModifiers").FindPropertyRelative("SecondaryTextureScaleOffset").vector4Value = EditorGUILayout.Vector4Field("Scale/Offset: ", t.LocalMaterials[Selected].TextureModifiers.SecondaryTextureScaleOffset);
                                serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("TextureModifiers").FindPropertyRelative("RotationSecondary").floatValue = EditorGUILayout.Slider("Rotation: ", t.LocalMaterials[Selected].TextureModifiers.RotationSecondary, 0, 360);
                            EditorGUILayout.EndVertical();





                        EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();


                EditorGUILayout.Space();
                RayTracingMaster.RTOShowEmission = EditorGUILayout.BeginFoldoutHeaderGroup(RayTracingMaster.RTOShowEmission, "Emission Settings", FoldoutStyle);
                ConnectionSources.Add("EmissionContainer", GUILayoutUtility.GetLastRect()); // Store position
                ConnectionSourceNames.Add("EmissionContainer");
                if(RayTracingMaster.RTOShowEmission) {
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space(12.0f, false);
                        EditorGUILayout.BeginVertical();
                            EditorGUILayout.BeginHorizontal();
                                Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.IsEmissionMask, EditorGUILayout.ToggleLeft("Is Emission Map", Flag.GetFlag(CommonFunctions.Flags.IsEmissionMask), GUILayout.MaxWidth(135)));
                                Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.BaseIsMap, EditorGUILayout.ToggleLeft("Base Color Is Map", Flag.GetFlag(CommonFunctions.Flags.BaseIsMap), GUILayout.MaxWidth(135)));
                                Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.ReplaceBase, EditorGUILayout.ToggleLeft("Replace Base Color", Flag.GetFlag(CommonFunctions.Flags.ReplaceBase), GUILayout.MaxWidth(135)));
                            EditorGUILayout.EndHorizontal();

                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("emission").floatValue = EditorGUILayout.FloatField("Emission: ", t.LocalMaterials[Selected].emission);
                            ConnectionSources.Add("Emission", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("Emission");
                            Color EmissCol = EditorGUILayout.ColorField("Emission Color", new Color(t.LocalMaterials[Selected].EmissionColor.x, t.LocalMaterials[Selected].EmissionColor.y, t.LocalMaterials[Selected].EmissionColor.z, 1));
                            ConnectionSources.Add("EmissionColor", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("EmissionColor");
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("EmissionColor").vector3Value = new Vector3(EmissCol.r, EmissCol.g, EmissCol.b);
                
                        EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();




                EditorGUILayout.Space();
                RayTracingMaster.RTOShowAdvanced = EditorGUILayout.BeginFoldoutHeaderGroup(RayTracingMaster.RTOShowAdvanced, "Advanced Settings", FoldoutStyle);
                ConnectionSources.Add("AdvancedContainer", GUILayoutUtility.GetLastRect()); // Store position
                ConnectionSourceNames.Add("AdvancedContainer");
                if(RayTracingMaster.RTOShowAdvanced) {
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space(12.0f, false);
                        EditorGUILayout.BeginVertical();

                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("Specular").floatValue = EditorGUILayout.Slider("Specular: ", t.LocalMaterials[Selected].Specular, 0, 1);
                            ConnectionSources.Add("Specular", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("Specular");
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("SpecularTint").floatValue = EditorGUILayout.Slider("Specular Tint: ", t.LocalMaterials[Selected].SpecularTint, 0, 1);
                            ConnectionSources.Add("SpecularTint", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("SpecularTint");
                            EditorGUILayout.Space();
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("Clearcoat").floatValue = EditorGUILayout.Slider("Clearcoat: ", t.LocalMaterials[Selected].Clearcoat, 0, 1);
                            ConnectionSources.Add("ClearCoat", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("ClearCoat");
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("ClearcoatGloss").floatValue = EditorGUILayout.Slider("Clearcoat Gloss: ", t.LocalMaterials[Selected].ClearcoatGloss, 0, 1);
                            ConnectionSources.Add("ClearCoatGloss", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("ClearCoatGloss");
                            EditorGUILayout.Space();
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("Sheen").floatValue = EditorGUILayout.Slider("Sheen: ", t.LocalMaterials[Selected].Sheen, 0, 100);
                            ConnectionSources.Add("Sheen", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("Sheen");
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("SheenTint").floatValue = EditorGUILayout.Slider("Sheen Tint: ", t.LocalMaterials[Selected].SheenTint, 0, 1);
                            ConnectionSources.Add("SheenTint", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("SheenTint");
                            EditorGUILayout.Space();                        
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("Anisotropic").floatValue = EditorGUILayout.Slider("Anisotropic: ", t.LocalMaterials[Selected].Anisotropic, 0, 1);
                            ConnectionSources.Add("Anisotropic", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("Anisotropic");
                            EditorGUILayout.Space();                        
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("AnisotropicRotation").floatValue = EditorGUILayout.Slider("Anisotropic Rotation: ", t.LocalMaterials[Selected].AnisotropicRotation, 0, 180);
                            EditorGUILayout.Space();
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("IOR").floatValue = EditorGUILayout.Slider("IOR: ", t.LocalMaterials[Selected].IOR, 1, 10);
                            ConnectionSources.Add("IOR", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("IOR");
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("SpecTrans").floatValue = EditorGUILayout.Slider("Glass: ", t.LocalMaterials[Selected].SpecTrans, 0, 1);
                            ConnectionSources.Add("SpecTrans", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("SpecTrans");
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("ScatterDist").floatValue = EditorGUILayout.Slider("Scatter Distance: ", t.LocalMaterials[Selected].ScatterDist, 0, 5);
                            ConnectionSources.Add("ScatterDist", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("ScatterDist");
                            EditorGUILayout.Space();
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("DiffTrans").floatValue = EditorGUILayout.Slider("Diffuse Transmission: ", t.LocalMaterials[Selected].DiffTrans, 0, 1);
                            ConnectionSources.Add("DiffTrans", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("DiffTrans");
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("TransmittanceColor").vector3Value = EditorGUILayout.Vector3Field("Transmission Color: ", t.LocalMaterials[Selected].TransmittanceColor);
                            ConnectionSources.Add("TransmissionColor", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("TransmissionColor");
                            EditorGUILayout.Space();
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("Flatness").floatValue = EditorGUILayout.Slider("Flatness: ", t.LocalMaterials[Selected].Flatness, 0, 1);
                            ConnectionSources.Add("Flatness", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("Flatness");

                        EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                EditorGUILayout.Space();
                RayTracingMaster.RTOShowColorModifiers = EditorGUILayout.BeginFoldoutHeaderGroup(RayTracingMaster.RTOShowColorModifiers, "Color Modifiers", FoldoutStyle);
                ConnectionSources.Add("ColorModifiersContainer", GUILayoutUtility.GetLastRect()); // Store position
                ConnectionSourceNames.Add("ColorModifiersContainer");
                if(RayTracingMaster.RTOShowColorModifiers) {
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space(12.0f, false);
                        EditorGUILayout.BeginVertical();
                    
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("Hue").floatValue = EditorGUILayout.Slider("Hue Shift: ", t.LocalMaterials[Selected].Hue, 0, 1);
                            ConnectionSources.Add("Hue", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("Hue");
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("Brightness").floatValue = EditorGUILayout.Slider("Brightness: ", t.LocalMaterials[Selected].Brightness, 0, 5);
                            ConnectionSources.Add("Brightness", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("Brightness");
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("Saturation").floatValue = EditorGUILayout.Slider("Saturation: ", t.LocalMaterials[Selected].Saturation, 0, 5);
                            ConnectionSources.Add("Saturation", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("Saturation");
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("Contrast").floatValue = EditorGUILayout.Slider("Contrast: ", t.LocalMaterials[Selected].Contrast, 0, 2);
                            ConnectionSources.Add("Contrast", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("Contrast");
                            Color BlendColor = EditorGUILayout.ColorField("Blend Color", new Color(t.LocalMaterials[Selected].BlendColor.x, t.LocalMaterials[Selected].BlendColor.y, t.LocalMaterials[Selected].BlendColor.z, 1));
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("BlendColor").vector3Value = new Vector3(BlendColor.r, BlendColor.g, BlendColor.b);
                            ConnectionSources.Add("BlendColor", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("BlendColor");
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("BlendFactor").floatValue = EditorGUILayout.Slider("Blend Factor: ", t.LocalMaterials[Selected].BlendFactor, 0, 1);      
                            ConnectionSources.Add("BlendFactor", GUILayoutUtility.GetLastRect()); // Store position
                            ConnectionSourceNames.Add("BlendFactor");

                        EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();

                EditorGUILayout.Space();
                RayTracingMaster.RTOShowFlag = EditorGUILayout.BeginFoldoutHeaderGroup(RayTracingMaster.RTOShowFlag, "Flags", FoldoutStyle);
                ConnectionSources.Add("FlagsContainer", GUILayoutUtility.GetLastRect()); // Store position
                ConnectionSourceNames.Add("FlagsContainer");
                if(RayTracingMaster.RTOShowFlag) {
                    EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Space(12.0f, false);
                        EditorGUILayout.BeginVertical();
                            EditorGUILayout.BeginHorizontal();
                                Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.UseSmoothness, EditorGUILayout.ToggleLeft("Use Smoothness", Flag.GetFlag(CommonFunctions.Flags.UseSmoothness), GUILayout.MaxWidth(135)));
                                ConnectionSources.Add("Smoothness", GUILayoutUtility.GetLastRect()); // Store position
                                ConnectionSourceNames.Add("Smoothness");
                                Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.InvertSmoothnessTexture, EditorGUILayout.ToggleLeft("Invert Roughness Tex", Flag.GetFlag(CommonFunctions.Flags.InvertSmoothnessTexture), GUILayout.MaxWidth(135)));
                                Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.ShadowCaster, EditorGUILayout.ToggleLeft("Casts Shadows", Flag.GetFlag(CommonFunctions.Flags.ShadowCaster), GUILayout.MaxWidth(135)));
                                Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.UseVertexColors, EditorGUILayout.ToggleLeft("Vertex Colors", Flag.GetFlag(CommonFunctions.Flags.UseVertexColors), GUILayout.MaxWidth(135)));
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                                Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.Thin, EditorGUILayout.ToggleLeft("Thin", Flag.GetFlag(CommonFunctions.Flags.Thin), GUILayout.MaxWidth(135)));
                                ConnectionSources.Add("Thin", GUILayoutUtility.GetLastRect()); // Store position
                                ConnectionSourceNames.Add("Thin");
                                Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.Invisible, EditorGUILayout.ToggleLeft("Invisible", Flag.GetFlag(CommonFunctions.Flags.Invisible), GUILayout.MaxWidth(135)));
                                Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.IsBackground, EditorGUILayout.ToggleLeft("Is Background", Flag.GetFlag(CommonFunctions.Flags.IsBackground), GUILayout.MaxWidth(135)));
                            EditorGUILayout.EndHorizontal();

                            EditorGUILayout.Space();
                            serializedObject.FindProperty("UseKelvin").GetArrayElementAtIndex(Selected).boolValue = EditorGUILayout.Toggle("Use Kelvin: ", t.UseKelvin[Selected]);
                            if(t.UseKelvin[Selected]) serializedObject.FindProperty("KelvinTemp").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Kelvin Temperature: ", t.KelvinTemp[Selected], 0, 20000);
                            EditorGUILayout.Space();
                            serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("ColorBleed").floatValue = EditorGUILayout.Slider("ColorBleed: ", t.LocalMaterials[Selected].ColorBleed, 0, 1.0f);
                            EditorGUILayout.Space();
                        EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndFoldoutHeaderGroup();



             







                serializedObject.FindProperty("LocalMaterials").GetArrayElementAtIndex(Selected).FindPropertyRelative("Tag").intValue = Flag;


                bool A = EditorGUILayout.ToggleLeft("Override All Local \"Invisible\" Flags", t.InvisibleOverride, GUILayout.MaxWidth(225));
                serializedObject.FindProperty("InvisibleOverride").boolValue = A;

                bool MaterialWasChanged = false;
                if(EditorGUI.EndChangeCheck()) {
                    MaterialWasChanged = true;
                }
                serializedObject.FindProperty("FollowMaterial").GetArrayElementAtIndex(Selected).boolValue = EditorGUILayout.ToggleLeft("Link Mat To Unity Material", t.FollowMaterial[Selected]);
                if(!QuickPasted) serializedObject.ApplyModifiedProperties();

                if(MaterialWasChanged) {
                    string Name = TheseNames[Selected];
                    if(MaterialWasChanged) {
                        for(int i = 0; i < t1.Length; i++) {
                            (t1[i] as RayTracingObject).CallMaterialEdited(true);
                        }
                    }
                    for(int i = 0; i < TheseNames.Length; i++) {
                        if(Selected == i) continue;
                        if(TheseNames[i].Equals(Name)) {
                            t.LocalMaterials[i] = t.LocalMaterials[Selected];
                            t.UseKelvin[i] = t.UseKelvin[Selected];
                            t.KelvinTemp[i] = t.KelvinTemp[Selected];
                            t.CallMaterialEdited(true);
                        }
                    }
                }

                if(GUILayout.Button("Propogate Material")) {
                    RayTracingObject[] Objects = GameObject.FindObjectsOfType<RayTracingObject>();
                    string Name = t.Names[Selected];
                    foreach(var Obj in Objects) {
                        for(int i = 0; i < Obj.LocalMaterials.Length; i++) {
                            if(Obj.Names[i].Equals(Name)) {
                                Obj.LocalMaterials[i] = t.LocalMaterials[Selected];
                                t.UseKelvin[i] = t.UseKelvin[Selected];
                                t.KelvinTemp[i] = t.KelvinTemp[Selected];
                                Obj.CallMaterialEdited(true);
                            }
                        }
                    }
                    t.CallMaterialEdited();
                }

            #if !HIDEMATERIALREATIONS
                int LinkSourceCount = ConnectionSourceNames.Count;
                for(int i = 0; i < LinkSourceCount; i++) {
                    if(ConnectionSources.TryGetValue(ConnectionSourceNames[i], out Rect ContainingRect)) {
                        if(ContainingRect.Contains(Event.current.mousePosition)) {
                            DrawHighlighter(ContainingRect);
                            if(DictionaryLinks.TryGetValue(ConnectionSourceNames[i], out List<string> B)) {
                                if(B != null) {
                                    int LinkCount = B.Count;
                                    for(int i2 = 0; i2 < LinkCount; i2++) {
                                        if(ConnectionSources.TryGetValue(B[i2], out Rect C)) {
                                            DrawConnections(ContainingRect, C);
                                        }
                                    }

                                }
                            }                
                            break;
                        }
                    }
                }
            #endif

        }
    }
}
#endif