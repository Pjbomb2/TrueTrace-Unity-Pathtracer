using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RayTracingObject))]
[CanEditMultipleObjects]
public class RayTracingObjectEditor : Editor
{
    SerializedProperty Names;
    int Selected = 0;
    string[] TheseNames;
    void OnEnable()
    {
        Names = serializedObject.FindProperty("Names");
        TheseNames = new string[Names.arraySize];
        (target as RayTracingObject).matfill();
    }

    public override void OnInspectorGUI() {
         Names = serializedObject.FindProperty("Names");
        for(int i = 0; i < Names.arraySize; i++) {
            TheseNames[i] = Names.GetArrayElementAtIndex(i).stringValue;
        }
        Selected = EditorGUILayout.Popup("Selected Material:", Selected, TheseNames);
        EditorGUILayout.Space();
        var t = (target as RayTracingObject);
        t.MaterialOptions[Selected] = (RayTracingObject.Options)EditorGUILayout.EnumPopup("MaterialType: ", t.MaterialOptions[Selected]);
        t.BaseColor[Selected] = EditorGUILayout.Vector3Field("Base Color: ", t.BaseColor[Selected]);
        t.emmission[Selected] = EditorGUILayout.FloatField("Emission: ", t.emmission[Selected]);
        t.EmissionColor[Selected] = EditorGUILayout.Vector3Field("Emission Color: ", t.EmissionColor[Selected]);
        t.Roughness[Selected] = EditorGUILayout.Slider("Roughness: ", t.Roughness[Selected], 0, 1);
        t.IOR[Selected] = EditorGUILayout.Slider("IOR: ", t.IOR[Selected], 0, 10);
        t.Metallic[Selected] = EditorGUILayout.Slider("Metallic: ", t.Metallic[Selected], 0, 1);
        t.SpecularTint[Selected] = EditorGUILayout.Slider("Specular Tint: ", t.SpecularTint[Selected], 0, 1);
        t.Sheen[Selected] = EditorGUILayout.Slider("Sheen: ", t.Sheen[Selected], 0, 10);
        t.SheenTint[Selected] = EditorGUILayout.Slider("Sheen Tint: ", t.SheenTint[Selected], 0, 1);
        t.ClearCoat[Selected] = EditorGUILayout.Slider("Clearcoat: ", t.ClearCoat[Selected], 0, 1);
        t.ClearCoatGloss[Selected] = EditorGUILayout.Slider("Clearcoat Gloss: ", t.ClearCoatGloss[Selected], 0, 1);
        t.Anisotropic[Selected] = EditorGUILayout.Slider("Anisotropic: ", t.Anisotropic[Selected], 0, 1);
        t.SpecTrans[Selected] = EditorGUILayout.Slider("Specular Transmission: ", t.SpecTrans[Selected], 0, 1);
        t.DiffTrans[Selected] = EditorGUILayout.Slider("Diffuse Transmission: ", t.DiffTrans[Selected], 0, 1);
        t.TransmissionColor[Selected] = EditorGUILayout.Vector3Field("Transmission Color: ", t.TransmissionColor[Selected]);
        t.Flatness[Selected] = EditorGUILayout.Slider("Flatness: ", t.Flatness[Selected], 0, 1);
        t.Thin[Selected] = EditorGUILayout.IntField("Thin: ", t.Thin[Selected]);

    }

}
