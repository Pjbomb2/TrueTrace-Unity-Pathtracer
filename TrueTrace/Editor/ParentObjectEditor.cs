#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CommonVars;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace TrueTrace {
    [CustomEditor(typeof(ParentObject))]
    public class ParentObjectEditor : Editor
    {
        public override bool RequiresConstantRepaint() => false;
        public override VisualElement CreateInspectorGUI() {
            return new VisualElement();
        }
    }
}
#endif