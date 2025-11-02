using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using UnityEditor;


namespace TrueTrace {
    [ExecuteInEditMode][System.Serializable][RequireComponent(typeof(RayTracingObject))]
    public class TTMaterialAnimator : MonoBehaviour
    {
        [SerializeField] public bool Animate = true;
        [SerializeField] public RayObjMat SelectedMaterial = new RayObjMat();
        [HideInInspector] [SerializeField] public RayTracingObject TargetObject;
        [HideInInspector] [SerializeField] public int MatSubIndex = 0;
        [HideInInspector] [SerializeField] public int PrevMatSubIndex = 0;

        public void LateUpdate() {
            if(Application.isPlaying && Animate) {
                TargetObject.LocalMaterials[MatSubIndex] = SelectedMaterial;
                TargetObject.CallMaterialEdited(false, false);
            }
        }

        private void OnEnable() {
            TargetObject = this.gameObject.GetComponent<RayTracingObject>();
            SelectedMaterial = TargetObject.LocalMaterials[MatSubIndex];
        }

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TTMaterialAnimator))]
    public class TTMaterialAnimatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var t1 = (targets);
            int TargCount = t1.Length;
            var t =  t1[0] as TTMaterialAnimator;
            DrawDefaultInspector();
            var TheseNames = t.TargetObject.Names;
            t.MatSubIndex = EditorGUILayout.Popup("Selected Material:", t.MatSubIndex, TheseNames);
            if(t.PrevMatSubIndex != t.MatSubIndex) t.SelectedMaterial = t.TargetObject.LocalMaterials[t.MatSubIndex];
            t.PrevMatSubIndex = t.MatSubIndex;
        }
    }

#endif

}
