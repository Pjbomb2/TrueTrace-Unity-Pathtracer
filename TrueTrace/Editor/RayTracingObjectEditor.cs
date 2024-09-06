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
        string PresetName = "Null";
        RayTracingObject ThisOBJ;
        int SaveIndex;

        public SavePopup(RayTracingObject ThisOBJ, int SaveIndex) {
            this.ThisOBJ = ThisOBJ;
            this.SaveIndex = SaveIndex;
        }
        public override Vector2 GetWindowSize()
        {
            return new Vector2(460, 50);
        }

        public override void OnGUI(Rect rect) {
            // Debug.Log("ONINSPECTORGUI");

            PresetName = GUILayout.TextField(PresetName, 32);
            
            if(GUILayout.Button("Save Preset")) {
                RayObjs PresetRays;
                int CopyIndex = -1;
                UnityEditor.AssetDatabase.Refresh();
                using (var A = new StringReader(Resources.Load<TextAsset>("Utility/MaterialPresets").text)) {
                    var serializer = new XmlSerializer(typeof(RayObjs));
                    PresetRays = serializer.Deserialize(A) as RayObjs;
                    int RayReadCount = PresetRays.RayObj.Count;
                    for(int i = 0; i < RayReadCount; i++) {
                        if(PresetRays.RayObj[i].MatName.Equals(PresetName)) {
                            CopyIndex = i;
                            break;
                        }
                    }
                }
                RayObjectDatas TempRay = new RayObjectDatas() {
                    ID = 0,
                    MatName = PresetName,
                    OptionID = (int)ThisOBJ.MaterialOptions[SaveIndex],
                    TransCol = ThisOBJ.TransmissionColor[SaveIndex],
                    BaseCol = ThisOBJ.BaseColor[SaveIndex],
                    MetRemap = ThisOBJ.MetallicRemap[SaveIndex],
                    RoughRemap = ThisOBJ.RoughnessRemap[SaveIndex],
                    Emiss = ThisOBJ.emmission[SaveIndex],
                    EmissCol = ThisOBJ.EmissionColor[SaveIndex],
                    Rough = ThisOBJ.Roughness[SaveIndex],
                    IOR = ThisOBJ.IOR[SaveIndex],
                    Met = ThisOBJ.Metallic[SaveIndex],
                    SpecTint = ThisOBJ.SpecularTint[SaveIndex],
                    Sheen = ThisOBJ.Sheen[SaveIndex],
                    SheenTint = ThisOBJ.SheenTint[SaveIndex],
                    Clearcoat = ThisOBJ.ClearCoat[SaveIndex],
                    ClearcoatGloss = ThisOBJ.ClearCoatGloss[SaveIndex],
                    Anisotropic = ThisOBJ.Anisotropic[SaveIndex],
                    Flatness = ThisOBJ.Flatness[SaveIndex],
                    DiffTrans = ThisOBJ.DiffTrans[SaveIndex],
                    SpecTrans = ThisOBJ.SpecTrans[SaveIndex],
                    FollowMat = ThisOBJ.FollowMaterial[SaveIndex],
                    ScatterDist = ThisOBJ.ScatterDist[SaveIndex],
                    Spec = ThisOBJ.Specular[SaveIndex],
                    AlphaCutoff = ThisOBJ.AlphaCutoff[SaveIndex],
                    NormStrength = ThisOBJ.NormalStrength[SaveIndex],
                    Hue = ThisOBJ.Hue[SaveIndex],
                    Brightness = ThisOBJ.Brightness[SaveIndex],
                    Contrast = ThisOBJ.Contrast[SaveIndex],
                    Saturation = ThisOBJ.Saturation[SaveIndex],
                    BlendColor = ThisOBJ.BlendColor[SaveIndex],
                    BlendFactor = ThisOBJ.BlendFactor[SaveIndex],
                    MainTexScaleOffset = ThisOBJ.MainTexScaleOffset[SaveIndex],
                    SecondaryTextureScale = ThisOBJ.SecondaryTextureScale[SaveIndex],
                    Rotation = ThisOBJ.Rotation[SaveIndex],
                    Flags = ThisOBJ.Flags[SaveIndex],
                    UseKelvin = ThisOBJ.UseKelvin[SaveIndex],
                    KelvinTemp = ThisOBJ.KelvinTemp[SaveIndex]
                };
                if(CopyIndex != -1) PresetRays.RayObj[CopyIndex] = TempRay;
                else PresetRays.RayObj.Add(TempRay);

                var materialPresetsPath = PathFinder.GetMaterialPresetsPath();
                using(StreamWriter writer = new StreamWriter(materialPresetsPath)) {
                    var serializer = new XmlSerializer(typeof(RayObjs));
                    serializer.Serialize(writer.BaseStream, PresetRays);
                    UnityEditor.AssetDatabase.Refresh();
                }
                this.editorWindow.Close();
            }


        }
    }
    public class LoadPopup : PopupWindowContent
    {
        Vector2 ScrollPosition;
        RayTracingObjectEditor SourceWindow;
        public LoadPopup(RayTracingObjectEditor editor) {
            this.SourceWindow = editor;
        }
        private void CallEditorFunction(RayObjectDatas RayObj) {
            if(SourceWindow != null) {
                SourceWindow.LoadFunction(RayObj);
            }
        }
        public override Vector2 GetWindowSize()
        {
            return new Vector2(460, 250);
        }

        public override void OnGUI(Rect rect) {
            RayObjs PresetRays;
            UnityEditor.AssetDatabase.Refresh();
            using (var A = new StringReader(Resources.Load<TextAsset>("Utility/MaterialPresets").text)) {
                var serializer = new XmlSerializer(typeof(RayObjs));
                PresetRays = serializer.Deserialize(A) as RayObjs;
            }
            int PresetLength = PresetRays.RayObj.Count;
            ScrollPosition = GUILayout.BeginScrollView(ScrollPosition, GUILayout.Width(460), GUILayout.Height(250));
            for(int i = 0; i < PresetLength; i++) {
                GUILayout.BeginHorizontal();
                    if(GUILayout.Button(PresetRays.RayObj[i].MatName)) {CallEditorFunction(PresetRays.RayObj[i]); this.editorWindow.Close();}
                    if(GUILayout.Button("Delete")) {
                        PresetRays.RayObj.RemoveAt(i);
                        var materialPresetPath = PathFinder.GetMaterialPresetsPath();
                        using(StreamWriter writer = new StreamWriter(materialPresetPath)) {
                            var serializer = new XmlSerializer(typeof(RayObjs));
                            serializer.Serialize(writer.BaseStream, PresetRays);
                            UnityEditor.AssetDatabase.Refresh();
                        }
                        OnGUI(new Rect(0,0,100,10));
                    }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
    }
    [CustomEditor(typeof(RayTracingObject))]
    public class RayTracingObjectEditor : Editor
    {

        int Selected = 0;
        string[] TheseNames;
        RayTracingObject t;
        void OnEnable()
        {
            (target as RayTracingObject).matfill();
        }

        void OnDestroy()
        {
            if(Application.isPlaying) {
                for(int i = 0; i < (target as RayTracingObject).Names.Length; i++) {
                    RayTracingMaster.WriteString((target as RayTracingObject), (target as RayTracingObject).Names[i]);
                }
            }
        }

        public void LoadFunction(RayObjectDatas RayObj) {
            t.MaterialOptions[Selected] = (RayTracingObject.Options)RayObj.OptionID;
            t.TransmissionColor[Selected] = RayObj.TransCol;
            t.BaseColor[Selected] = RayObj.BaseCol;
            t.MetallicRemap[Selected] = RayObj.MetRemap;
            t.RoughnessRemap[Selected] = RayObj.RoughRemap;
            t.emmission[Selected] = RayObj.Emiss;
            t.EmissionColor[Selected] = RayObj.EmissCol;
            t.Roughness[Selected] = RayObj.Rough;
            t.IOR[Selected] = RayObj.IOR;
            t.Metallic[Selected] = RayObj.Met;
            t.SpecularTint[Selected] = RayObj.SpecTint;
            t.Sheen[Selected] = RayObj.Sheen;
            t.SheenTint[Selected] = RayObj.SheenTint;
            t.ClearCoat[Selected] = RayObj.Clearcoat;
            t.ClearCoatGloss[Selected] = RayObj.ClearcoatGloss;
            t.Anisotropic[Selected] = RayObj.Anisotropic;
            t.Flatness[Selected] = RayObj.Flatness;
            t.DiffTrans[Selected] = RayObj.DiffTrans;
            t.SpecTrans[Selected] = RayObj.SpecTrans;
            t.FollowMaterial[Selected] = RayObj.FollowMat;
            t.ScatterDist[Selected] = RayObj.ScatterDist;
            t.Specular[Selected] = RayObj.Spec;
            t.AlphaCutoff[Selected] = RayObj.AlphaCutoff;
            t.NormalStrength[Selected] = RayObj.NormStrength;
            t.Hue[Selected] = RayObj.Hue;
            t.Brightness[Selected] = RayObj.Brightness;
            t.Contrast[Selected] = RayObj.Contrast;
            t.Saturation[Selected] = RayObj.Saturation;
            t.BlendColor[Selected] = RayObj.BlendColor;
            t.BlendFactor[Selected] = RayObj.BlendFactor;
            t.MainTexScaleOffset[Selected] = RayObj.MainTexScaleOffset;
            t.SecondaryTextureScale[Selected] = RayObj.SecondaryTextureScale;
            t.Rotation[Selected] = RayObj.Rotation;
            t.Flags[Selected] = RayObj.Flags;
            t.UseKelvin[Selected] = RayObj.UseKelvin;
            t.KelvinTemp[Selected] = RayObj.KelvinTemp;
            t.CallMaterialEdited(true);


            OnInspectorGUI();
        }
        public override void OnInspectorGUI() {
                
                var t1 = (targets);
                t =  t1[0] as RayTracingObject;
                TheseNames = t.Names;
                Selected = EditorGUILayout.Popup("Selected Material:", Selected, TheseNames);

                if(GUILayout.Button("Save Material Preset")) {
                    UnityEditor.PopupWindow.Show(new Rect(0,0,10,10), new SavePopup(t, Selected));
                }
                if(GUILayout.Button("Load Material Preset")) {
                    UnityEditor.PopupWindow.Show(new Rect(0,0,100,10), new LoadPopup(this));
                }


                EditorGUILayout.Space();
                EditorGUI.BeginChangeCheck();
                t.MaterialOptions[Selected] = (RayTracingObject.Options)EditorGUILayout.EnumPopup("MaterialType: ", t.MaterialOptions[Selected]);
                Color BaseCol = EditorGUILayout.ColorField("Base Color", new Color(t.BaseColor[Selected].x, t.BaseColor[Selected].y, t.BaseColor[Selected].z, 1));
                serializedObject.FindProperty("BaseColor").GetArrayElementAtIndex(Selected).vector3Value = new Vector3(BaseCol.r, BaseCol.g, BaseCol.b);
                serializedObject.FindProperty("emmission").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.FloatField("Emission: ", t.emmission[Selected]);
                Color EmissCol = EditorGUILayout.ColorField("Emission Color", new Color(t.EmissionColor[Selected].x, t.EmissionColor[Selected].y, t.EmissionColor[Selected].z, 1));
                serializedObject.FindProperty("EmissionColor").GetArrayElementAtIndex(Selected).vector3Value = new Vector3(EmissCol.r, EmissCol.g, EmissCol.b);
                int Flag = t.Flags[Selected];
                EditorGUILayout.BeginVertical();
                    EditorGUILayout.BeginHorizontal();
                        Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.IsEmissionMask, EditorGUILayout.ToggleLeft("Is Emission Map", Flag.GetFlag(CommonFunctions.Flags.IsEmissionMask), GUILayout.MaxWidth(200)));
                        Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.BaseIsMap, EditorGUILayout.ToggleLeft("Base Color Is Map", Flag.GetFlag(CommonFunctions.Flags.BaseIsMap), GUILayout.MaxWidth(200)));
                        Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.ReplaceBase, EditorGUILayout.ToggleLeft("Replace Base Color", Flag.GetFlag(CommonFunctions.Flags.ReplaceBase)));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                        Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.ShadowCaster, EditorGUILayout.ToggleLeft("Casts Shadows", Flag.GetFlag(CommonFunctions.Flags.ShadowCaster), GUILayout.MaxWidth(200)));
                        Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.UseSmoothness, EditorGUILayout.ToggleLeft("Use Smoothness", Flag.GetFlag(CommonFunctions.Flags.UseSmoothness), GUILayout.MaxWidth(200)));
                        Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.InvertSmoothnessTexture, EditorGUILayout.ToggleLeft("Invert Roughness Tex", Flag.GetFlag(CommonFunctions.Flags.InvertSmoothnessTexture)));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                        Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.Invisible, EditorGUILayout.ToggleLeft("Invisible", Flag.GetFlag(CommonFunctions.Flags.Invisible), GUILayout.MaxWidth(200)));
                        Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.Thin, EditorGUILayout.ToggleLeft("Thin", Flag.GetFlag(CommonFunctions.Flags.Thin), GUILayout.MaxWidth(200)));
                    EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                serializedObject.FindProperty("Metallic").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Metallic: ", t.Metallic[Selected], 0, 1);
                EditorGUILayout.MinMaxSlider("Metallic Remap: ", ref t.MetallicRemap[Selected].x, ref t.MetallicRemap[Selected].y, 0, 1);
                if(Flag.GetFlag(CommonFunctions.Flags.UseSmoothness)) serializedObject.FindProperty("Roughness").GetArrayElementAtIndex(Selected).floatValue = 1.0f - EditorGUILayout.Slider("Smoothness: ", 1.0f - t.Roughness[Selected], 0, 1);
                else serializedObject.FindProperty("Roughness").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Roughness: ", t.Roughness[Selected], 0, 1);
                EditorGUILayout.MinMaxSlider("Roughness Remap: ", ref t.RoughnessRemap[Selected].x, ref t.RoughnessRemap[Selected].y, 0, 1);
                serializedObject.FindProperty("IOR").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("IOR: ", t.IOR[Selected], 1, 10);
                serializedObject.FindProperty("Specular").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Specular: ", t.Specular[Selected], 0, 1);
                serializedObject.FindProperty("SpecularTint").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Specular Tint: ", t.SpecularTint[Selected], 0, 1);
                serializedObject.FindProperty("Sheen").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Sheen: ", t.Sheen[Selected], 0, 10);
                serializedObject.FindProperty("SheenTint").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Sheen Tint: ", t.SheenTint[Selected], 0, 1);
                serializedObject.FindProperty("ClearCoat").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Clearcoat: ", t.ClearCoat[Selected], 0, 1);
                serializedObject.FindProperty("ClearCoatGloss").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Clearcoat Gloss: ", t.ClearCoatGloss[Selected], 0, 1);
                serializedObject.FindProperty("Anisotropic").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Anisotropic: ", t.Anisotropic[Selected], 0, 1);
                serializedObject.FindProperty("SpecTrans").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("SpecTrans(Glass): ", t.SpecTrans[Selected], 0, 1);
                serializedObject.FindProperty("DiffTrans").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Diffuse Transmission: ", t.DiffTrans[Selected], 0, 1);
                serializedObject.FindProperty("TransmissionColor").GetArrayElementAtIndex(Selected).vector3Value = EditorGUILayout.Vector3Field("Transmission Color: ", t.TransmissionColor[Selected]);
                serializedObject.FindProperty("Flatness").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Flatness: ", t.Flatness[Selected], 0, 1);
                serializedObject.FindProperty("ScatterDist").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Scatter Distance: ", t.ScatterDist[Selected], 0, 5);
                serializedObject.FindProperty("AlphaCutoff").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Alpha Cutoff: ", t.AlphaCutoff[Selected], 0.01f, 1.0f);
                serializedObject.FindProperty("NormalStrength").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Normalmap Strength: ", t.NormalStrength[Selected], 0, 5.0f);
                
                EditorGUILayout.Space();

                serializedObject.FindProperty("Hue").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Hue Shift: ", t.Hue[Selected], 0, 1);
                serializedObject.FindProperty("Brightness").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Brightness: ", t.Brightness[Selected], 0, 5);
                serializedObject.FindProperty("Saturation").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Saturation: ", t.Saturation[Selected], 0, 2);
                serializedObject.FindProperty("Contrast").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Contrast: ", t.Contrast[Selected], 0, 2);
                Color BlendColor = EditorGUILayout.ColorField("Blend Color", new Color(t.BlendColor[Selected].x, t.BlendColor[Selected].y, t.BlendColor[Selected].z, 1));
                serializedObject.FindProperty("BlendColor").GetArrayElementAtIndex(Selected).vector3Value = new Vector3(BlendColor.r, BlendColor.g, BlendColor.b);
                serializedObject.FindProperty("BlendFactor").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Blend Factor: ", t.BlendFactor[Selected], 0, 1);
                serializedObject.FindProperty("MainTexScaleOffset").GetArrayElementAtIndex(Selected).vector4Value = EditorGUILayout.Vector4Field("MainTex Scale/Offset: ", t.MainTexScaleOffset[Selected]);
                serializedObject.FindProperty("SecondaryTextureScale").GetArrayElementAtIndex(Selected).vector2Value = EditorGUILayout.Vector2Field("SecondaryTex Scale: ", t.SecondaryTextureScale[Selected]);
                serializedObject.FindProperty("Rotation").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Texture Rotation: ", t.Rotation[Selected], 0, 1);
                serializedObject.FindProperty("Flags").GetArrayElementAtIndex(Selected).intValue = Flag;

                serializedObject.FindProperty("UseKelvin").GetArrayElementAtIndex(Selected).boolValue = EditorGUILayout.Toggle("Use Kelvin: ", t.UseKelvin[Selected]);
                serializedObject.FindProperty("KelvinTemp").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Kelvin Temperature: ", t.KelvinTemp[Selected], 0, 20000);


                bool MaterialWasChanged = false;
                if(EditorGUI.EndChangeCheck()) {
                    MaterialWasChanged = true;

                    for(int i = 0; i < t1.Length; i++) {
                        (t1[i] as RayTracingObject).CallMaterialEdited(true);
                    }
                }
                serializedObject.FindProperty("FollowMaterial").GetArrayElementAtIndex(Selected).boolValue = EditorGUILayout.Toggle("Link Mat To Unity Material: ", t.FollowMaterial[Selected]);
                serializedObject.ApplyModifiedProperties();

                if(MaterialWasChanged) {
                    string Name = TheseNames[Selected];
                    for(int i = 0; i < TheseNames.Length; i++) {
                        if(Selected == i) continue;
                        if(TheseNames[i].Equals(Name)) {
                            t.MaterialOptions[i] = t.MaterialOptions[Selected];
                            t.BaseColor[i] = t.BaseColor[Selected];
                            t.TransmissionColor[i] = t.TransmissionColor[Selected];
                            t.emmission[i] = t.emmission[Selected];
                            t.EmissionColor[i] = t.EmissionColor[Selected];
                            t.Roughness[i] = t.Roughness[Selected];
                            t.RoughnessRemap[i] = t.RoughnessRemap[Selected];
                            t.MetallicRemap[i] = t.MetallicRemap[Selected];
                            t.IOR[i] = t.IOR[Selected];
                            t.Metallic[i] = t.Metallic[Selected];
                            t.SpecularTint[i] = t.SpecularTint[Selected];
                            t.Sheen[i] = t.Sheen[Selected];
                            t.SheenTint[i] = t.SheenTint[Selected];
                            t.ClearCoat[i] = t.ClearCoat[Selected];
                            t.ClearCoatGloss[i] = t.ClearCoatGloss[Selected];
                            t.Anisotropic[i] = t.Anisotropic[Selected];
                            t.Flatness[i] = t.Flatness[Selected];
                            t.DiffTrans[i] = t.DiffTrans[Selected];
                            t.SpecTrans[i] = t.SpecTrans[Selected];
                            t.Hue[i] = t.Hue[Selected];
                            t.Brightness[i] = t.Brightness[Selected];
                            t.Saturation[i] = t.Saturation[Selected];
                            t.Contrast[i] = t.Contrast[Selected];
                            t.FollowMaterial[i] = t.FollowMaterial[Selected];
                            t.ScatterDist[i] = t.ScatterDist[Selected];
                            t.Specular[i] = t.Specular[Selected];
                            t.AlphaCutoff[i] = t.AlphaCutoff[Selected];
                            t.NormalStrength[i] = t.NormalStrength[Selected];
                            t.BlendColor[i] = t.BlendColor[Selected];
                            t.BlendFactor[i] = t.BlendFactor[Selected];
                            t.MainTexScaleOffset[i] = t.MainTexScaleOffset[Selected];
                            t.SecondaryTextureScale[i] = t.SecondaryTextureScale[Selected];
                            t.Rotation[i] = t.Rotation[Selected];
                            t.Flags[i] = Flag;
                            t.UseKelvin[i] = t.UseKelvin[Selected];
                            t.KelvinTemp[i] = t.KelvinTemp[Selected];
                            // Debug.Log(i);
                            t.CallMaterialEdited(true);
                        }
                    }
                }

                if(GUILayout.Button("Texture Scroll Changed")) {
                    t.CallTilingScrolled();
                }
                if(GUILayout.Button("Propogate To Materials")) {
                    RayTracingObject[] Objects = GameObject.FindObjectsOfType<RayTracingObject>();
                    string Name = t.Names[Selected];
                    foreach(var Obj in Objects) {
                        for(int i = 0; i < Obj.MaterialOptions.Length; i++) {
                            if(Obj.Names[i].Equals(Name)) {
                                Obj.MaterialOptions[i] = t.MaterialOptions[Selected];
                                Obj.BaseColor[i] = t.BaseColor[Selected];
                                Obj.TransmissionColor[i] = t.TransmissionColor[Selected];
                                Obj.emmission[i] = t.emmission[Selected];
                                Obj.EmissionColor[i] = t.EmissionColor[Selected];
                                Obj.Roughness[i] = t.Roughness[Selected];
                                Obj.RoughnessRemap[i] = t.RoughnessRemap[Selected];
                                Obj.MetallicRemap[i] = t.MetallicRemap[Selected];
                                Obj.IOR[i] = t.IOR[Selected];
                                Obj.Metallic[i] = t.Metallic[Selected];
                                Obj.SpecularTint[i] = t.SpecularTint[Selected];
                                Obj.Sheen[i] = t.Sheen[Selected];
                                Obj.SheenTint[i] = t.SheenTint[Selected];
                                Obj.ClearCoat[i] = t.ClearCoat[Selected];
                                Obj.ClearCoatGloss[i] = t.ClearCoatGloss[Selected];
                                Obj.Anisotropic[i] = t.Anisotropic[Selected];
                                Obj.Flatness[i] = t.Flatness[Selected];
                                Obj.DiffTrans[i] = t.DiffTrans[Selected];
                                Obj.SpecTrans[i] = t.SpecTrans[Selected];
                                Obj.Hue[i] = t.Hue[Selected];
                                Obj.Brightness[i] = t.Brightness[Selected];
                                Obj.Saturation[i] = t.Saturation[Selected];
                                Obj.Contrast[i] = t.Contrast[Selected];
                                Obj.FollowMaterial[i] = t.FollowMaterial[Selected];
                                Obj.ScatterDist[i] = t.ScatterDist[Selected];
                                Obj.Specular[i] = t.Specular[Selected];
                                Obj.AlphaCutoff[i] = t.AlphaCutoff[Selected];
                                Obj.NormalStrength[i] = t.NormalStrength[Selected];
                                Obj.BlendColor[i] = t.BlendColor[Selected];
                                Obj.BlendFactor[i] = t.BlendFactor[Selected];
                                Obj.MainTexScaleOffset[i] = t.MainTexScaleOffset[Selected];
                                Obj.SecondaryTextureScale[i] = t.SecondaryTextureScale[Selected];
                                Obj.Rotation[i] = t.Rotation[Selected];
                                Obj.Flags[i] = Flag;
                                Obj.UseKelvin[i] = t.UseKelvin[Selected];
                                Obj.KelvinTemp[i] = t.KelvinTemp[Selected];
                                Obj.CallMaterialEdited(true);
                            }
                        }
                    }
                    t.CallMaterialEdited();
                }

        }
    }
}
#endif