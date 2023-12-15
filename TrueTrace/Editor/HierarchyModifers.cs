#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 using UnityEditor;
using CommonVars;
 using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace TrueTrace {
    public class HierarchyModifers : EditorWindow {
        [MenuItem("TrueTrace/Hierarchy Modifers")]
        public static void ShowWindow() {
            GetWindow<HierarchyModifers>("Hierarchy Modifers");
        }

             public struct ParentData {
                public Transform This;
                public List<ParentData> Children;
             }

             private ParentData GrabChildren2(Transform Parent) {
                ParentData Parents = new ParentData();
                Parents.Children = new List<ParentData>();
                Parents.This = Parent;
                int ChildCount = Parent.childCount;
                for(int i = 0; i < ChildCount; i++) {
                   if(Parent.GetChild(i).gameObject.activeInHierarchy) Parents.Children.Add(GrabChildren2(Parent.GetChild(i)));

                }
                return Parents;
             }

             private bool TraverseFirstLevel(ParentData Parent) {
                int ChildLength = Parent.Children.Count;
                for(int i = 0; i < ChildLength; i++) {
                   if(Parent.Children[i].This.gameObject.GetComponent<MeshFilter>() != null) {
                      return false;
                   }
                }
                return true;
             }

             private void ReduceChildren(ParentData Parent) {
                int ChildLength = Parent.Children.Count;
                for(int i = 0; i < ChildLength; i++) {
                   if(Parent.Children[i].This.gameObject.GetComponent<ParentObject>() != null) {
                      if(TraverseFirstLevel(Parent.Children[i])) {
                         DestroyImmediate(Parent.Children[i].This.gameObject.GetComponent<ParentObject>());
                      }
                   }
                   ReduceChildren(Parent.Children[i]);
                }
             }

             private void SolveChildren(ParentData Parent) {
                int ChildLength = Parent.Children.Count;
                for(int i = 0; i < ChildLength; i++) {
                   SolveChildren(Parent.Children[i]);
                }
                if(((Parent.This.gameObject.GetComponent<MeshFilter>() != null && Parent.This.gameObject.GetComponent<MeshFilter>().sharedMesh != null) || (Parent.This.gameObject.GetComponent<SkinnedMeshRenderer>() != null && Parent.This.gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh != null)) && Parent.This.gameObject.GetComponent<InstancedObject>() == null) {
                   if(Parent.This.gameObject.GetComponent<RayTracingObject>() == null) {
                         if((Parent.This.gameObject.GetComponent<MeshRenderer>() != null && Parent.This.gameObject.GetComponent<MeshRenderer>().sharedMaterials != null && Parent.This.gameObject.GetComponent<MeshRenderer>().sharedMaterials.Length != 0) || (Parent.This.gameObject.GetComponent<SkinnedMeshRenderer>() != null && Parent.This.gameObject.GetComponent<SkinnedMeshRenderer>().sharedMaterials != null && Parent.This.gameObject.GetComponent<SkinnedMeshRenderer>().sharedMaterials.Length != 0)) {
                            Parent.This.gameObject.AddComponent<RayTracingObject>();
                         }

                      }
                }
                int RayTracingObjectChildCount = 0;
                bool HasSkinnedMeshAsChild = false;
                bool HasNormalMeshAsChild = false;
                for(int i = 0; i < ChildLength; i++) {
                   if(Parent.Children[i].This.gameObject.GetComponent<RayTracingObject>() != null && Parent.Children[i].This.gameObject.GetComponent<ParentObject>() == null) RayTracingObjectChildCount++;
                   if(Parent.Children[i].This.gameObject.GetComponent<MeshFilter>() != null && Parent.Children[i].This.gameObject.GetComponent<ParentObject>() == null) HasNormalMeshAsChild = true;
                   if(Parent.Children[i].This.gameObject.GetComponent<SkinnedMeshRenderer>() != null && Parent.Children[i].This.gameObject.GetComponent<ParentObject>() == null) HasSkinnedMeshAsChild = true;
                   if(Parent.Children[i].This.gameObject.GetComponent<Light>() != null && Parent.Children[i].This.gameObject.GetComponent<RayTracingLights>() == null) Parent.Children[i].This.gameObject.AddComponent<RayTracingLights>(); 
                }
                bool ReductionNeeded = false;
                for(int i = 0; i < ChildLength; i++) {
                   if(Parent.Children[i].This.gameObject.GetComponent<SkinnedMeshRenderer>() != null && Parent.This.gameObject.GetComponent<MeshFilter>() == null) {
                      ReductionNeeded = true;
                   }
                }
                if(ReductionNeeded) ReduceChildren(Parent);
                if(RayTracingObjectChildCount > 0) {
                   if(Parent.This.gameObject.GetComponent<AssetManager>() == null) {
                      if(Parent.This.gameObject.GetComponent<ParentObject>() == null) {
                         Parent.This.gameObject.AddComponent<ParentObject>();
                      }
                   }
                   else {
                      for(int i = 0; i < ChildLength; i++) {
                         if(Parent.Children[i].This.gameObject.GetComponent<RayTracingObject>() != null && Parent.Children[i].This.gameObject.GetComponent<ParentObject>() == null) Parent.Children[i].This.gameObject.AddComponent<ParentObject>();
                      }               
                   }
                } else {
                   for(int i = 0; i < ChildLength; i++) {
                      if(Parent.Children[i].This.gameObject.GetComponent<RayTracingObject>() != null && Parent.Children[i].This.gameObject.GetComponent<ParentObject>() == null && Parent.This.gameObject.GetComponent<ParentObject>() == null) Parent.This.gameObject.AddComponent<ParentObject>();
                   }
                }
                if(HasNormalMeshAsChild && HasSkinnedMeshAsChild) {
                   for(int i = 0; i < ChildLength; i++) {
                      if(Parent.Children[i].This.gameObject.GetComponent<SkinnedMeshRenderer>() != null && Parent.Children[i].This.gameObject.GetComponent<ParentObject>() == null) {
                         Parent.Children[i].This.gameObject.AddComponent<ParentObject>();
                      }
                   }  
                }


             }
            ObjectField TargetObject;
             private void UndoInstances() {
                GameObject Obj = TargetObject.value as GameObject;
                if(Obj == null) return;

                var E = Obj.GetComponentsInChildren<InstancedObject>();
                foreach(var a in E) {
                    DestroyImmediate(a);
                }
                ParentData SourceParent = GrabChildren2(Obj.transform);

                SolveChildren(SourceParent);
             }

                      List<Transform> ChildObjects;
         private void GrabChildren(Transform Parent) {
            ChildObjects.Add(Parent);
            int ChildCount = Parent.childCount;
            for(int i = 0; i < ChildCount; i++) {
               if(Parent.GetChild(i).gameObject.activeInHierarchy) GrabChildren(Parent.GetChild(i));
            }
         }


             private void MakeStatic() {
                GameObject Obj = TargetObject.value as GameObject;
            Transform[] AllObjects = Obj.GetComponentsInChildren<Transform>();//("Untagged");
            foreach(Transform obj in AllObjects) {
               
               if(PrefabUtility.IsAnyPrefabInstanceRoot(obj.gameObject)) PrefabUtility.UnpackPrefabInstance(obj.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            ChildObjects = new List<Transform>();
            Transform Source = Obj.transform;
            int ChildrenLeft = Source.childCount;
            Transform Parent;
            if(GameObject.Find("Static Objects") == null) {
               GameObject TempObject = new GameObject("Static Objects", typeof(ParentObject));
               Parent = TempObject.transform;
            }
            else Parent = GameObject.Find("Static Objects").transform;
            Parent.parent = Source;
            int CurrentChild = 0;
            while(CurrentChild < ChildrenLeft) {
               Transform CurrentObject = Source.GetChild(CurrentChild++);
               if(CurrentObject.gameObject.activeInHierarchy && !CurrentObject.gameObject.name.Equals("Static Objects")) GrabChildren(CurrentObject); 
            }
            CurrentChild = 0;
            ChildrenLeft = Parent.childCount;
            while(CurrentChild < ChildrenLeft) {
               Transform CurrentObject = Parent.GetChild(CurrentChild++);
               if(CurrentObject.gameObject.activeInHierarchy && !CurrentObject.gameObject.name.Equals("Static Objects")) GrabChildren(CurrentObject); 
            }
            int ChildCount = ChildObjects.Count;
            for(int i = ChildCount - 1; i >= 0; i--) {
               if(ChildObjects[i].GetComponent<ParentObject>() != null) {
                  DestroyImmediate(ChildObjects[i].GetComponent<ParentObject>());
               }
               if(ChildObjects[i].GetComponent<Light>() != null) {
                  continue;
               } else if(ChildObjects[i].GetComponent<MeshFilter>() != null || ChildObjects[i].GetComponent<Terrain>() != null) {
                  ChildObjects[i].parent = Parent;
               } else if(ChildObjects[i].GetComponent<InstancedObject>() != null) {
                  ChildObjects[i].parent = Source;
               } else {
                  ChildObjects[i].parent = null;
               }
            }

         }

            public void CreateGUI() {
                TargetObject = new ObjectField();
                TargetObject.objectType = typeof(GameObject);

               Button ReplaceInstanceButton = new Button(() => UndoInstances()) {text = "Undo Instances"};

               Button MakeStaticButton = new Button(() => MakeStatic()) {text = "Make Static"};


               rootVisualElement.Add(TargetObject);
               rootVisualElement.Add(ReplaceInstanceButton);
               rootVisualElement.Add(MakeStaticButton);

           }






    }
}
#endif