#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TrueTrace {
    [CustomEditor(typeof(RayTracingObject)), CanEditMultipleObjects]
    public class RayTracingObjectEditor : Editor
    {

        int Selected = 0;
        string[] TheseNames;
        void OnEnable()
        {
            (target as RayTracingObject).matfill();
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
                serializedObject.FindProperty("EmissionMask").GetArrayElementAtIndex(Selected).boolValue = EditorGUILayout.Toggle("Is Emission Map: ", t.EmissionMask[Selected]);
                serializedObject.FindProperty("BaseIsMap").GetArrayElementAtIndex(Selected).boolValue = EditorGUILayout.Toggle("Base Color Is Map: ", t.BaseIsMap[Selected]);
                serializedObject.FindProperty("ReplaceBase").GetArrayElementAtIndex(Selected).boolValue = EditorGUILayout.Toggle("Replace Base Color: ", t.ReplaceBase[Selected]);
                serializedObject.FindProperty("Roughness").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Roughness: ", t.Roughness[Selected], 0, 1);
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
                serializedObject.FindProperty("Thin").GetArrayElementAtIndex(Selected).intValue = EditorGUILayout.Toggle("Surface Is Thin", t.Thin[Selected] == 1 ? true : false) ? 1 : 0;
                serializedObject.FindProperty("DiffTrans").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Diffuse Transmission: ", t.DiffTrans[Selected], 0, 1);
                serializedObject.FindProperty("TransmissionColor").GetArrayElementAtIndex(Selected).vector3Value = EditorGUILayout.Vector3Field("Transmission Color: ", t.TransmissionColor[Selected]);
                serializedObject.FindProperty("Flatness").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Flatness: ", t.Flatness[Selected], 0, 1);
                serializedObject.FindProperty("ScatterDist").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Scatter Distance: ", t.ScatterDist[Selected], 0, 5);
                serializedObject.FindProperty("AlphaCutoff").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Alpha Cutoff: ", t.AlphaCutoff[Selected], 0.01f, 1.0f);
                serializedObject.FindProperty("NormalStrength").GetArrayElementAtIndex(Selected).floatValue = EditorGUILayout.Slider("Normalmap Strength: ", t.NormalStrength[Selected], 0, 5.0f);

                if(EditorGUI.EndChangeCheck()) {
                    for(int i = 0; i < t1.Length; i++) {
                        (t1[i] as RayTracingObject).CallMaterialEdited();
                    }
                }

                serializedObject.FindProperty("FollowMaterial").GetArrayElementAtIndex(Selected).boolValue = EditorGUILayout.Toggle("Link Mat To Unity Material: ", t.FollowMaterial[Selected]);
                serializedObject.ApplyModifiedProperties();
                if(Application.isPlaying && GUILayout.Button("Save Properties")) {
                    List<CommonVars.RayObjectData> RayDat = new List<CommonVars.RayObjectData>();
                    for(int i = 0; i < t.TransmissionColor.Length; i++) {
                        RayDat.Add(new CommonVars.RayObjectData() {
                            TransmittanceColor = t.TransmissionColor[i],
                            BaseColor = t.BaseColor[i],
                            MetallicRemap = t.MetallicRemap[i],
                            RoughnessRemap = t.RoughnessRemap[i],
                            emmission = t.emmission[i],
                            EmissionColor = t.EmissionColor[i],
                            EmissionMask = t.EmissionMask[i],
                            BaseIsMap = t.BaseIsMap[i],
                            ReplaceBase = t.ReplaceBase[i],
                            Roughness = t.Roughness[i],
                            IOR = t.IOR[i],
                            Metallic = t.Metallic[i],
                            SpecularTint = t.SpecularTint[i],
                            Sheen = t.Sheen[i],
                            SheenTint = t.SheenTint[i],
                            ClearCoat = t.ClearCoat[i],
                            ClearCoatGloss = t.ClearCoatGloss[i],
                            Anisotropic = t.Anisotropic[i],
                            Flatness = t.Flatness[i],
                            DiffTrans = t.DiffTrans[i],
                            SpecTrans = t.SpecTrans[i],
                            Thin = t.Thin[i],
                            FollowMaterial = t.FollowMaterial[i],
                            ScatterDist = t.ScatterDist[i],
                            Specular = t.Specular[i],
                            AlphaCutoff = t.AlphaCutoff[i],
                            IsSmoothness = t.IsSmoothness[i],
                            NormalStrength = t.NormalStrength[i]
                        });
                    }
                    if(!EditModeFunctions.RayObjects.ContainsKey(t.GetInstanceID())) {
                        EditModeFunctions.RayObjects.Add(t.GetInstanceID(), new CommonVars.RayObject() {RayObj = t.gameObject, RayData = RayDat});
                    } else {
                        EditModeFunctions.RayObjects[t.GetInstanceID()] = new CommonVars.RayObject() {RayObj = t.gameObject, RayData = RayDat};
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
                                Obj.EmissionMask[i] = t.EmissionMask[Selected];
                                Obj.BaseIsMap[i] = t.BaseIsMap[Selected];
                                Obj.ReplaceBase[i] = t.ReplaceBase[Selected];
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
                                Obj.Thin[i] = t.Thin[Selected];
                                Obj.FollowMaterial[i] = t.FollowMaterial[Selected];
                                Obj.ScatterDist[i] = t.ScatterDist[Selected];
                                Obj.Specular[i] = t.Specular[Selected];
                                Obj.IsSmoothness[i] = t.IsSmoothness[Selected];
                                Obj.AlphaCutoff[i] = t.AlphaCutoff[Selected];
                                Obj.CallMaterialEdited();
                            }
                        }
                    }
                    t.CallMaterialEdited();
                }

        }
    }
}
#endif