#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CommonVars;

namespace TrueTrace {
    [CustomEditor(typeof(RayTracingObject))]
    public class RayTracingObjectEditor : Editor
    {

        int Selected = 0;
        string[] TheseNames;
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

        public override void OnInspectorGUI() {
                var t1 = (targets);
                var t =  t1[0] as RayTracingObject;
                TheseNames = t.Names;
                Selected = EditorGUILayout.Popup("Selected Material:", Selected, TheseNames);

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
                        // Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.IsBackground, EditorGUILayout.ToggleLeft("Is Background Object", Flag.GetFlag(CommonFunctions.Flags.IsBackground), GUILayout.MaxWidth(200)));
                        // Flag = CommonFunctions.SetFlagVar(Flag, CommonFunctions.Flags.BackgroundBleed, EditorGUILayout.ToggleLeft("Background Bleed", Flag.GetFlag(CommonFunctions.Flags.BackgroundBleed)));
                    EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                if(Flag.GetFlag(CommonFunctions.Flags.UseSmoothness)) serializedObject.FindProperty("Roughness").GetArrayElementAtIndex(Selected).floatValue = 1.0f - EditorGUILayout.Slider("Smoothness: ", 1.0f - t.Roughness[Selected], 0, 1);
                else serializedObject.FindProperty("Roughness").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Roughness: ", t.Roughness[Selected], 0, 1);
                EditorGUILayout.MinMaxSlider("Roughness Remap: ", ref t.RoughnessRemap[Selected].x, ref t.RoughnessRemap[Selected].y, 0, 1);
                serializedObject.FindProperty("Metallic").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Metallic: ", t.Metallic[Selected], 0, 1);
                EditorGUILayout.MinMaxSlider("Metallic Remap: ", ref t.MetallicRemap[Selected].x, ref t.MetallicRemap[Selected].y, 0, 1);
                serializedObject.FindProperty("IOR").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("IOR: ", t.IOR[Selected], 0, 10);
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

                bool EEE = false;
                if(EditorGUI.EndChangeCheck()) {
                    EEE = true;

                    for(int i = 0; i < t1.Length; i++) {
                        (t1[i] as RayTracingObject).CallMaterialEdited();
                    }
                }
                serializedObject.FindProperty("FollowMaterial").GetArrayElementAtIndex(Selected).boolValue = EditorGUILayout.Toggle("Link Mat To Unity Material: ", t.FollowMaterial[Selected]);
                serializedObject.ApplyModifiedProperties();

                if(EEE) {
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