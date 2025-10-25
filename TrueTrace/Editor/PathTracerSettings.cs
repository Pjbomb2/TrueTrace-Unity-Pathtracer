#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
 using UnityEditor;
using CommonVars;
 using UnityEngine.UIElements;
using UnityEditor.UIElements;
 using System.Xml;
 using System.IO;
 using UnityEngine.Profiling;
using System.Xml.Serialization;
using UnityEngine.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShortcutManagement;

namespace TrueTrace {
   public class EditModeFunctions : EditorWindow {
        [MenuItem("TrueTrace/TrueTrace Settings")]
        public static void ShowWindow() {
            GetWindow<EditModeFunctions>("TrueTrace Settings");
        }

        public Toggle NEEToggle;
        public Button BVHBuild;
        public Button ScreenShotButton;
        public Button StaticButton;
        public Button ClearButton;
        public Button QuickStartButton;
        public Button ForceInstancesButton;
        public RayTracingMaster RayMaster;
        public AssetManager Assets;
        public InstancedManager Instancer;
        private bool Cleared;
         [SerializeField] public Camera SelectedCamera;
         [SerializeField] public bool ClayMode = false;
         [SerializeField] public Vector3 ClayColor = new Vector3(0.5f,0.5f,0.5f);
         [SerializeField] public float ClayMetalOverride = 0.0f;
         [SerializeField] public float ClayRoughnessOverride = 0.0f;
         [SerializeField] public bool DoClayMetalRoughOverride = false;

         [SerializeField] public int MaxSampCount = 99999999;
         [SerializeField] public Vector3 GroundColor = new Vector3(0.1f,0.1f,0.1f);
         [SerializeField] public int BounceCount = 7;
         [SerializeField] public float RenderRes = 1;
         [SerializeField] public bool NEE = true;
         [SerializeField] public bool Accumulate = true;
         [SerializeField] public bool RR = true;
         [SerializeField] public bool Moving = true;
         [SerializeField] public bool MeshSkin = true;
         [SerializeField] public bool Bloom = false;
         [SerializeField] public float BloomStrength = 0.5f;
         [SerializeField] public bool DoF = false;
         [SerializeField] public float DoFAperature = 0.1f;
         [SerializeField] public float DoFAperatureScale = 1.0f;
         [SerializeField] public float DoFFocal = 0.1f;
         [SerializeField] public bool DoExposure = false;
         [SerializeField] public bool ExposureAuto = false;
         [SerializeField] public bool ReSTIRGI = false;
         [SerializeField] public bool SampleValid = false;
         [SerializeField] public bool GITemporal = true;
         [SerializeField] public int GITemporalMCap = 20;
         [SerializeField] public bool GISpatial = true;
         [SerializeField] public bool TAA = false;
         [SerializeField] public bool FXAA = false;
         [SerializeField] public bool ToneMap = false;
         [SerializeField] public int ToneMapIndex = 0;
         [SerializeField] public int UpscalerMethod = 0;
         [SerializeField] public int AtmoScatter = 4;
         [SerializeField] public bool ShowFPS = true;
         [SerializeField] public float Exposure = 0;
         [SerializeField] public int AtlasSize = 16384;
         [SerializeField] public bool DoPartialRendering = false;
         [SerializeField] public int PartialRenderingFactor = 1;
         [SerializeField] public bool DoFirefly = false;
         [SerializeField] public bool ImprovedPrimaryHit = true;
         [SerializeField] public int RISCount = 5;
         [SerializeField] public int DenoiserMethod = 0;
         [SerializeField] public Color SceneBackgroundColor = new Color(1,1,1,1);
         [SerializeField] public Color PrimaryBackgroundTintColor = new Color(1,1,1,1);
         [SerializeField] public Color SecondarySceneBackgroundColor = new Color(1,1,1,1);
         [SerializeField] public Color FogColor = new Color(0.6f,0.6f,0.6f,1);
         [SerializeField] public Vector2 BackgroundIntensity = Vector2.one;
         [SerializeField] public float PrimaryBackgroundTint = 0.0f;
         [SerializeField] public float PrimaryBackgroundContrast = 1.0f;
         [SerializeField] public float LightEnergyScale = 1;
         [SerializeField] public float LEMEnergyScale = 1;
         [SerializeField] public float IndirectBoost = 1;
         [SerializeField] public int BackgroundType = 0;
         [SerializeField] public int SecondaryBackgroundType = 0;
         [SerializeField] public bool DoSaving = true;
         [SerializeField] public float SkyDesaturate = 0;
         [SerializeField] public float SecondarySkyDesaturate = 0;
         [SerializeField] public int FireflyFrameCount = 0;
         [SerializeField] public int FireflyFrameInterval = 1;
         [SerializeField] public float FireflyStrength = 1.0f;
         [SerializeField] public float FireflyOffset = 0;
         [SerializeField] public int OIDNFrameCount = 0;
         [SerializeField] public bool DoSharpen = false;
         [SerializeField] public float Sharpness = 1.0f;
         [SerializeField] public Vector2 HDRILongLat = Vector2.zero;
         [SerializeField] public Vector2 HDRIScale = Vector2.one;
         [SerializeField] public bool UseTransmittanceInNEE = true;
         [SerializeField] public bool MatChangeResetsAccum = false;
         [SerializeField] public float OIDNBlendRatio = 1.0f;
         [SerializeField] public float FogDensity = 0.0002f;
         [SerializeField] public float FogHeight = 80.0f;
         [SerializeField] public bool ConvBloom = false;
         [SerializeField] public float ConvStrength = 1.37f;
         [SerializeField] public float ConvBloomThreshold = 13.23f;
         [SerializeField] public Vector2 ConvBloomSize = Vector2.one;
         [SerializeField] public float ConvBloomDistExp = 0;
         [SerializeField] public float ConvBloomDistExpClampMin = 1;
         [SerializeField] public float ConvBloomDistExpScale = 1;
         [SerializeField] public bool DoChromaAber = false;
         [SerializeField] public float ChromaDistort = 0.3f;
         [SerializeField] public bool DoBCS = false;
         [SerializeField] public float Saturation = 1.0f;
         [SerializeField] public float Contrast = 1.0f;
         [SerializeField] public bool DoVignette = false;
         [SerializeField] public float innerVignette = 0.5f;
         [SerializeField] public float outerVignette = 1.2f;
         [SerializeField] public float strengthVignette = 0.8f;
         [SerializeField] public float curveVignette = 0.5f;
         [SerializeField] public Color ColorVignette = Color.black;
         [SerializeField] public bool ShowPostProcessMenu = true;
         [SerializeField] public float aoStrength = 1.0f;
         [SerializeField] public float aoRadius = 2.0f;



         public bool GetGlobalDefine(string DefineToGet) {
            string globalDefinesPath = TTPathFinder.GetGlobalDefinesPath();

            if(File.Exists(globalDefinesPath)) {
               string[] GlobalDefines = System.IO.File.ReadAllLines(globalDefinesPath);
               int Index = -1;
               for(int i = 0; i < GlobalDefines.Length; i++) {
                  if(GlobalDefines[i].Equals("//END OF DEFINES")) break;
                  string TempString = GlobalDefines[i].Replace("#define ", "");
                  TempString = TempString.Replace("// ", "");
                  if(TempString.Equals(DefineToGet)) {
                     Index = i;
                     break;
                  }
               }
               if(Index == -1) {
                  Debug.Log("Cant find define \"" + DefineToGet + "\"");
                  return false;
               }
               bool CachedValue = true;
               if(GlobalDefines[Index].Contains("// ")) CachedValue = false;
               return CachedValue;
            } else {Debug.Log("No GlobalDefinesFile");}
            return false;
         }


         public void SetGlobalDefines(string DefineToSet, bool SetValue) {
            string globalDefinesPath = TTPathFinder.GetGlobalDefinesPath();

            if(File.Exists(globalDefinesPath)) {
               string[] GlobalDefines = System.IO.File.ReadAllLines(globalDefinesPath);
               int Index = -1;
               for(int i = 0; i < GlobalDefines.Length; i++) {
                  if(GlobalDefines[i].Equals("//END OF DEFINES")) break;
                  string TempString = GlobalDefines[i].Replace("#define ", "");
                  TempString = TempString.Replace("// ", "");
                  if(TempString.Equals(DefineToSet)) {
                     Index = i;
                     break;
                  }
               }
               if(Index == -1) {
                  Debug.Log("Cant find define \"" + DefineToSet + "\"");
                  return;
               }
               bool CachedValue = true;
               if(GlobalDefines[Index].Contains("// ")) CachedValue = false;
               if(CachedValue != SetValue) {
                  GlobalDefines[Index] = GlobalDefines[Index].Replace("// ", "");
                  if(!SetValue) GlobalDefines[Index] = "// " + GlobalDefines[Index];

                  System.IO.File.WriteAllLines(globalDefinesPath, GlobalDefines);
                  AssetDatabase.Refresh();
               }
            } else {Debug.Log("No GlobalDefinesFile");}
         }


         void OnEnable() {
            EditorSceneManager.activeSceneChangedInEditMode += EvaluateScene;
            EditorSceneManager.sceneSaving += SaveScene;
            EditorSceneManager.activeSceneChanged += ChangedActiveScene;
            EditorSceneManager.sceneSaved += SaveScenePost;
            if(EditorPrefs.GetString("EditModeFunctions", JsonUtility.ToJson(this, false)) != null) {
               var data = EditorPrefs.GetString("EditModeFunctions", JsonUtility.ToJson(this, false));
               JsonUtility.FromJsonOverwrite(data, this);
            }
         }

         private void ChangedActiveScene(Scene current, Scene next)
         {
            CreateGUI();
         }

         void OnDisable() {
            var data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString("EditModeFunctions", data);
         }


         List<List<GameObject>> Objects;
         List<Mesh> SourceMeshes;
         private static TTStopWatch BuildWatch = new TTStopWatch("Total Scene Build Time");
         private void OnStartAsyncCombined() {
            EditorUtility.SetDirty(GameObject.Find("Scene").GetComponent<AssetManager>());
            BuildWatch.Start();
            GameObject.Find("Scene").GetComponent<AssetManager>().ClearAll();
            GameObject.Find("Scene").GetComponent<AssetManager>().EditorBuild();
         }


         List<Transform> ChildObjects;
         private void GrabChildren(Transform Parent, bool IgnoreSkinned = false) {
            ChildObjects.Add(Parent);
            int ChildCount = Parent.childCount;
            for(int i = 0; i < ChildCount; i++) {
               if(Parent.GetChild(i).gameObject.activeInHierarchy) {
                  if(IgnoreSkinned) {
                     if(Parent.GetChild(i).gameObject.TryGetComponent<ParentObject>(out ParentObject TempObj)) {
                        if(TempObj.IsSkinnedGroup) continue;
                     }
                  }
                  GrabChildren(Parent.GetChild(i), IgnoreSkinned);
               }
            }
         }


         private void ConstructInstances() {
            SourceMeshes = new List<Mesh>();
            Objects = new List<List<GameObject>>();
            ChildObjects = new List<Transform>();
            Transform Source = GameObject.Find("Scene").transform;
            Transform InstanceStorage = GameObject.Find("InstancedStorage").transform;
            int ChildrenLeft = Source.childCount;
            int CurrentChild = 0;
            while(CurrentChild < ChildrenLeft) {
               Transform CurrentObject = Source.GetChild(CurrentChild++);
               if(CurrentObject.gameObject.activeInHierarchy) GrabChildren(CurrentObject); 
            }

            int ChildCount = ChildObjects.Count;
            for(int i = ChildCount - 1; i >= 0; i--) {
               if(ChildObjects[i].GetComponent<InstancedObject>() != null) {
                  continue;
               }
               if(ChildObjects[i].GetComponent<RayTracingObject>() != null && ChildObjects[i].GetComponent<MeshFilter>() != null) {
                     var mesh = ChildObjects[i].GetComponent<MeshFilter>().sharedMesh;
                     if(SourceMeshes.Contains(mesh)) {
                        int Index = SourceMeshes.IndexOf(mesh);
                        Objects[Index].Add(ChildObjects[i].gameObject);
                     } else {
                        SourceMeshes.Add(mesh);
                        Objects.Add(new List<GameObject>());
                        Objects[Objects.Count - 1].Add(ChildObjects[i].gameObject);
                     }
               }
            }
            int UniqueMeshCounts = SourceMeshes.Count;
            for(int i = 0; i < UniqueMeshCounts; i++) {
               if(Objects[i].Count > 1) {
                  int Count = Objects[i].Count;
                  GameObject InstancedParent = Instantiate(Objects[i][0], new Vector3(0,-100,0), Quaternion.identity, InstanceStorage);
                  if(InstancedParent.GetComponent<ParentObject>() == null) InstancedParent.AddComponent<ParentObject>();
                  for(int i2 = Count - 1; i2 >= 0; i2--) {
                     DestroyImmediate(Objects[i][i2].GetComponent<MeshFilter>());
                     DestroyImmediate(Objects[i][i2].GetComponent<MeshRenderer>());
                     DestroyImmediate(Objects[i][i2].GetComponent<RayTracingObject>());
                     if(InstancedParent.GetComponent<ParentObject>()) DestroyImmediate(Objects[i][i2].GetComponent<ParentObject>());
                     (Objects[i][i2].AddComponent<InstancedObject>()).InstanceParent = InstancedParent.GetComponent<ParentObject>();
                     // Objects[i][i2].GetComponent<InstancedObject>().InstanceParent = InstancedParent.GetComponent<ParentObject>();
                  }
               }
            }
         }


         private void ConstructInstancesSelective(Mesh SelectedMesh) {
            SourceMeshes = new List<Mesh>();
            SourceMeshes.Add(SelectedMesh);
            Objects = new List<List<GameObject>>();
            Objects.Add(new List<GameObject>());
            ChildObjects = new List<Transform>();
            Transform Source = GameObject.Find("Scene").transform;
            Transform InstanceStorage = GameObject.Find("InstancedStorage").transform;
            int ChildrenLeft = Source.childCount;
            int CurrentChild = 0;
            while(CurrentChild < ChildrenLeft) {
               Transform CurrentObject = Source.GetChild(CurrentChild++);
               if(CurrentObject.gameObject.activeInHierarchy) GrabChildren(CurrentObject); 
            }

            int ChildCount = ChildObjects.Count;
            for(int i = ChildCount - 1; i >= 0; i--) {
               if(ChildObjects[i].GetComponent<InstancedObject>() != null) {
                  continue;
               }
               if(ChildObjects[i].GetComponent<RayTracingObject>() != null && ChildObjects[i].GetComponent<MeshFilter>() != null) {
                     var mesh = ChildObjects[i].GetComponent<MeshFilter>().sharedMesh;
                     if(SourceMeshes.Contains(mesh)) {
                        int Index = SourceMeshes.IndexOf(mesh);
                        Objects[Index].Add(ChildObjects[i].gameObject);
                     } else {
                        // SourceMeshes.Add(mesh);
                        // Objects.Add(new List<GameObject>());
                        // Objects[Objects.Count - 1].Add(ChildObjects[i].gameObject);
                     }
               }
            }
            int UniqueMeshCounts = SourceMeshes.Count;
            for(int i = 0; i < UniqueMeshCounts; i++) {
               if(Objects[i].Count > 1) {
                  int Count = Objects[i].Count;
                  GameObject InstancedParent = Instantiate(Objects[i][0], new Vector3(0,-100,0), Quaternion.identity, InstanceStorage);
                  if(InstancedParent.GetComponent<ParentObject>() == null) InstancedParent.AddComponent<ParentObject>();
                  for(int i2 = Count - 1; i2 >= 0; i2--) {
                     DestroyImmediate(Objects[i][i2].GetComponent<MeshFilter>());
                     DestroyImmediate(Objects[i][i2].GetComponent<MeshRenderer>());
                     DestroyImmediate(Objects[i][i2].GetComponent<RayTracingObject>());
                     if(InstancedParent.GetComponent<ParentObject>()) DestroyImmediate(Objects[i][i2].GetComponent<ParentObject>());
                     (Objects[i][i2].AddComponent<InstancedObject>()).InstanceParent = InstancedParent.GetComponent<ParentObject>();
                     // Objects[i][i2].GetComponent<InstancedObject>().InstanceParent = InstancedParent.GetComponent<ParentObject>();
                  }
               }
            }
         }

         private void OptimizeForStatic() {
            GameObject[] AllObjects = GameObject.FindObjectsOfType<GameObject>();//("Untagged");
            foreach(GameObject obj in AllObjects) {
               
               try{if(PrefabUtility.IsAnyPrefabInstanceRoot(obj)) PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);} catch(System.Exception e) {}
            }
            foreach(GameObject obj in AllObjects) {
               
               if(obj.name.Contains("LOD1") || obj.name.Contains("LOD2")) DestroyImmediate(obj);
            }

            ChildObjects = new List<Transform>();
            Transform Source = GameObject.Find("Scene").transform;
            if(GameObject.Find("Terrain") != null) GameObject.Find("Terrain").transform.parent = Source;
            int ChildrenLeft = Source.childCount;
            Transform Parent;
            Transform LeftoverParent;
            if(GameObject.Find("Leftover Objects") == null) {
               GameObject TempObject2 = new GameObject("Leftover Objects");
               LeftoverParent = TempObject2.transform;
            } else LeftoverParent = GameObject.Find("Leftover Objects").transform;
            LeftoverParent.parent = Source;
            if(GameObject.Find("Static Objects") == null) {
               GameObject TempObject = new GameObject("Static Objects", typeof(ParentObject));
               Parent = TempObject.transform;
            } else Parent = GameObject.Find("Static Objects").transform;
            Parent.parent = Source;
            int CurrentChild = 0;
            while(CurrentChild < ChildrenLeft) {
               Transform CurrentObject = Source.GetChild(CurrentChild++);
               if(CurrentObject.gameObject.activeInHierarchy && !CurrentObject.gameObject.name.Equals("Static Objects")) GrabChildren(CurrentObject, true); 
            }
            CurrentChild = 0;
            ChildrenLeft = Parent.childCount;
            while(CurrentChild < ChildrenLeft) {
               Transform CurrentObject = Parent.GetChild(CurrentChild++);
               if(CurrentObject.gameObject.activeInHierarchy && !CurrentObject.gameObject.name.Equals("Static Objects")) GrabChildren(CurrentObject, true); 
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
                  ChildObjects[i].parent = LeftoverParent;
               }
            }

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
               if(Parent.GetChild(i).gameObject.activeInHierarchy && !(Parent.GetChild(i).gameObject.name.Contains("LOD") && !Parent.GetChild(i).gameObject.name.Contains("LOD0"))) Parents.Children.Add(GrabChildren2(Parent.GetChild(i)));

            }
            return Parents;
         }

         private bool TraverseFirstLevel(ParentData Parent) {
            int ChildLength = Parent.Children.Count;
            for(int i = 0; i < ChildLength; i++) {
               if(Parent.Children[i].This.gameObject.TryGetComponent<MeshFilter>(out MeshFilter TempFilt)) {
                  return false;
               }
            }
            return true;
         }

         private void ReduceChildren(ParentData Parent) {
            int ChildLength = Parent.Children.Count;
            for(int i = 0; i < ChildLength; i++) {
               if(Parent.Children[i].This.gameObject.TryGetComponent<ParentObject>(out ParentObject TempParent)) {
                  if(TraverseFirstLevel(Parent.Children[i])) {
                     DestroyImmediate(TempParent);
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
            bool Fine0 = Parent.This.gameObject.TryGetComponent<MeshFilter>(out MeshFilter TempFilt);
            bool Fine2 = Fine0;
            if(Fine0) Fine0 = Fine0 && TempFilt.sharedMesh != null;
            bool Fine1 = Parent.This.gameObject.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer TempSkin);
            if(Fine1) Fine1 = Fine1 && TempSkin.sharedMesh != null;
            if((Fine0 || Fine1) && !Parent.This.gameObject.TryGetComponent<InstancedObject>(out InstancedObject InstObj)) {
               if(!Parent.This.gameObject.TryGetComponent<RayTracingObject>(out RayTracingObject TempRayObj)) {
                     if(Parent.This.gameObject.TryGetComponent<MeshRenderer>(out MeshRenderer MeshRend)) {
                        if(MeshRend.sharedMaterials != null && MeshRend.sharedMaterials.Length != 0) {
                          var TempOBJ = Parent.This.gameObject.AddComponent<RayTracingObject>();
                          // TempOBJ.hideFlags = HideFlags.DontSave;
                       }
                     } else if(Parent.This.gameObject.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer SkinRend)) {
                        if(SkinRend.sharedMaterials != null && SkinRend.sharedMaterials.Length != 0) {
                           var TempOBJ = Parent.This.gameObject.AddComponent<RayTracingObject>();
                           // TempOBJ.hideFlags = HideFlags.DontSave;
                        }
                     }

                  }
            }
            int RayTracingObjectChildCount = 0;
            bool HasSkinnedMeshAsChild = false;
            bool HasNormalMeshAsChild = false;
            for(int i = 0; i < ChildLength; i++) {
               bool FoundParent = Parent.Children[i].This.gameObject.TryGetComponent<ParentObject>(out ParentObject TempParent);
               if(Parent.Children[i].This.gameObject.TryGetComponent<RayTracingObject>(out RayTracingObject TempRayObj2) && !FoundParent) RayTracingObjectChildCount++;
               if(Parent.Children[i].This.gameObject.TryGetComponent<MeshFilter>(out MeshFilter TempFilt2) && !FoundParent) HasNormalMeshAsChild = true;
               if(Parent.Children[i].This.gameObject.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer SkinRend2) && !FoundParent) HasSkinnedMeshAsChild = true;
            }
            bool ReductionNeeded = false;
            for(int i = 0; i < ChildLength; i++) {
               if(Parent.Children[i].This.gameObject.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer TempSkin2) && !Fine2) {
                  ReductionNeeded = true;
                  break;
               }
            }
            if(ReductionNeeded) ReduceChildren(Parent);
            if(RayTracingObjectChildCount > 0) {
               if(!Parent.This.gameObject.TryGetComponent<AssetManager>(out AssetManager TempAsset)) {
                  if(!Parent.This.gameObject.TryGetComponent<ParentObject>(out ParentObject TempParent2)) {
                     var TempOBJ = Parent.This.gameObject.AddComponent<ParentObject>();
                     // TempOBJ.hideFlags = HideFlags.DontSave;
                  
                  }
               }
               else {
                  for(int i = 0; i < ChildLength; i++) {
                     if(Parent.Children[i].This.gameObject.TryGetComponent<RayTracingObject>(out RayTracingObject TempObj2) && !Parent.Children[i].This.gameObject.TryGetComponent<ParentObject>(out ParentObject TempParent2)) {
                        var TempOBJ = Parent.Children[i].This.gameObject.AddComponent<ParentObject>();
                        // TempOBJ.hideFlags = HideFlags.DontSave;
                     }
                  }               
               }
            } else {
               if(ChildLength == 0 && Parent.This.root == Parent.This && Parent.This.gameObject.TryGetComponent<RayTracingObject>(out RayTracingObject TempObj4) && !Parent.This.gameObject.TryGetComponent<ParentObject>(out ParentObject TempParent5)) {
                  var TempOBJ = Parent.This.gameObject.AddComponent<ParentObject>();
                  // TempOBJ.hideFlags = HideFlags.DontSave;
               }
               for(int i = 0; i < ChildLength; i++) {
                  if(Parent.Children[i].This.gameObject.TryGetComponent<RayTracingObject>(out RayTracingObject TempObj2) && !Parent.Children[i].This.gameObject.TryGetComponent<ParentObject>(out ParentObject TempParent3) && !Parent.This.gameObject.TryGetComponent<ParentObject>(out ParentObject TempParent2)) {
                     var TempOBJ = Parent.This.gameObject.AddComponent<ParentObject>();
                     // TempOBJ.hideFlags = HideFlags.DontSave;
                  }
               }
            }
            if(HasNormalMeshAsChild && HasSkinnedMeshAsChild) {
               for(int i = 0; i < ChildLength; i++) {
                  if(!Parent.Children[i].This.gameObject.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer TempSkin3) && !Parent.Children[i].This.gameObject.TryGetComponent<ParentObject>(out ParentObject TempParent3)) {
                     var TempOBJ = Parent.Children[i].This.gameObject.AddComponent<ParentObject>();
                     // TempOBJ.hideFlags = HideFlags.DontSave;
                  }
               }  
            }


         }

         public void QuickStart() {
            // ParentObject[] TempObjects2 = GameObject.FindObjectsOfType<ParentObject>();
            // foreach(var a in TempObjects2) {
            //    DestroyImmediate(a);
            // }
            // RayTracingObject[] TempObjects = GameObject.FindObjectsOfType<RayTracingObject>();
            // foreach(var a in TempObjects) {
            //    // a.gameObject.AddComponent<ParentObject>();
            //    // DestroyImmediate(a);
            //    // if(a.gameObject.name.Contains("LOD") && !a.gameObject.name.Contains("LOD0")) DestroyImmediate(a);
            // }         

            var LightObjects = GameObject.FindObjectsOfType<Light>(true);
            foreach(var LightObj in LightObjects) {
               if(LightObj.gameObject.GetComponent<RayTracingLights>() == null) LightObj.gameObject.AddComponent<RayTracingLights>(); 
            }

            // FlagsObjects RootFlag = Prepare(Assets.transform);
            // Prune(ref RootFlag);

            GameObject[] RootObjs = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            int RootLength = RootObjs.Length;
            for(int i = 0; i < RootLength; i++) {
               ParentData SourceParent = GrabChildren2(RootObjs[i].transform);

               SolveChildren(SourceParent);
            }


               Terrain[] Terrains = GameObject.FindObjectsOfType<Terrain>();
               foreach(var TerrainComponent in Terrains) {
                  if(TerrainComponent.gameObject.GetComponentInParent<AssetManager>() == null) {
                     TerrainComponent.gameObject.transform.parent = Assets.transform;
                  }
                  if(TerrainComponent.gameObject.GetComponent<TerrainObject>() == null) TerrainComponent.gameObject.AddComponent<TerrainObject>();
               }
         }
      IntegerField RemainingObjectsField;
      IntegerField SampleCountField;
      private void ReArrangeHierarchy() {
            if(Camera.main != null) {
               if(Camera.main.gameObject.GetComponent<RayTracingMaster>() != null) {
                  DestroyImmediate(Camera.main.gameObject.GetComponent<RayTracingMaster>());
                  Camera.main.gameObject.AddComponent<RenderHandle>();
               }
               if(Camera.main.gameObject.GetComponent<RenderHandle>() == null) Camera.main.gameObject.AddComponent<RenderHandle>();

            }
         if(GameObject.Find("Scene") == null) {
                  List<GameObject> Objects = new List<GameObject>();
                  UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(Objects);
                  GameObject SceneObject = new GameObject("Scene", typeof(AssetManager));
                  // foreach(GameObject Obj in Objects) {
                  //    if(Obj.GetComponent<Camera>() == null && !Obj.name.Equals("InstancedStorage")) {
                  //       Obj.transform.SetParent(SceneObject.transform);
                  //    }
                  // }
                  Assets = GameObject.Find("Scene").GetComponent<AssetManager>();
                  QuickStart();
               }
            if(Instancer == null && GameObject.Find("InstancedStorage") != null) Instancer = GameObject.Find("InstancedStorage").GetComponent<InstancedManager>();
            if(GameObject.Find("InstancedStorage") == null) {
               GameObject InstanceObject = new GameObject("InstancedStorage", typeof(InstancedManager));
            }
            OnFocus();
      }

         public void OnFocus() {
           if(Assets == null) {
               if( GameObject.Find("Scene") != null) {
                  EnsureInitializedGlobalDefines();
                  Assets = GameObject.Find("Scene").GetComponent<AssetManager>();
                  if(Assets == null) {
                     Assets = GameObject.Find("Scene").AddComponent<AssetManager>();
                  }
               }
           }
           if(RayMaster == null) {
               if(GameObject.Find("Scene") != null) {
                  if(GameObject.Find("Scene").GetComponent<RayTracingMaster>() == null) GameObject.Find("Scene").AddComponent(typeof(RayTracingMaster));
                  RayMaster = GameObject.Find("Scene").GetComponent<RayTracingMaster>();
                  RayMaster.LoadTT();
               }
            }
            if(RayMaster == null) return;
            if(Instancer == null && GameObject.Find("InstancedStorage") != null) Instancer = GameObject.Find("InstancedStorage").GetComponent<InstancedManager>();
            if(GameObject.Find("InstancedStorage") == null) {
               GameObject InstanceObject = new GameObject("InstancedStorage", typeof(InstancedManager));
            }
         }
Button LowSettings;
Toggle RRToggle;
Toggle MovingToggle;
Toggle AccumToggle;
Toggle SkinToggle;
Toggle BloomToggle;        
[Delayed] FloatField ResField;
IntegerField AtmoScatterField;
Toggle GIToggle;
IntegerField TemporalGIMCapField;
Toggle TemporalGIToggle;
Toggle SpatialGIToggle;
Toggle TAAToggle;
Toggle FXAAToggle;
Toggle DoPartialRenderingToggle;
Toggle DoFireflyToggle;
Toggle SampleValidToggle;
Toggle IndirectClampingToggle;
IntegerField RISCountField;
IntegerField OIDNFrameField;
FloatField FocalSlider;
PopupField<string> UpscalerField;


VisualElement MainSource;
VisualElement RearrangeElement;
Toolbar toolbar;
      void OnGUI() {
         Event e = Event.current;
         if(e.commandName == "ConfirmedButton") ConfirmPopup();  
      }
      public void ConfirmPopup() {
         ReArrangeHierarchy();  
         OnFocus(); 
         if(Camera.main != null && Camera.main.gameObject.GetComponent<FlyCamera>() == null) Camera.main.gameObject.AddComponent<FlyCamera>();

         MainSource.Remove(RearrangeElement);
         CreateGUI(); 
         rootVisualElement.Add(MainSource); 
         Assets.UpdateMaterialDefinition();
      }

      VisualElement HardSettingsMenu;
      VisualElement PostProcessingMenu;
      VisualElement SceneSettingsMenu;
      PopupField<string> BackgroundSettingsField;
      PopupField<string> SecondaryBackgroundSettingsField;
      ObjectField InputHDRIField;
      ObjectField SecondaryInputHDRIField;
      ColorField BackgroundColorField;
      ColorField SecondaryBackgroundColorField;
      FloatField BackgroundIntensityField;
      FloatField UnityLightModifierField;
      FloatField IndirectBoostField;


      void AddNormalSettings() {
         List<string> BackgroundSettings = new List<string>();
         BackgroundSettings.Add("Atmosphere");
         BackgroundSettings.Add("HDRI");
         BackgroundSettings.Add("Solid Color");

         VisualElement BlankElement = new VisualElement();
         VisualElement SecondaryBlankElement = new VisualElement();



         Foldout PrimaryBackgroundFoldout = new Foldout() {text = "Primary Background"};
            PrimaryBackgroundFoldout.tooltip = "Sky used for direct lighting";
        { 
            {

               InputHDRIField = new ObjectField();
               InputHDRIField.objectType = typeof(Texture);
               InputHDRIField.label = "Drag your skybox here ->";
               InputHDRIField.value = RayMaster.SkyboxTexture;
               InputHDRIField.RegisterValueChangedCallback(evt => {RayMaster.SkyboxTexture = evt.newValue as Texture; SecondaryInputHDRIField.value = evt.newValue;});

               BackgroundColorField = new ColorField();
               BackgroundColorField.value = SceneBackgroundColor;
               BackgroundColorField.style.width = 150;
               BackgroundColorField.RegisterValueChangedCallback(evt => {SceneBackgroundColor = evt.newValue; RayMaster.LocalTTSettings.SceneBackgroundColor = new Vector3(SceneBackgroundColor.r,SceneBackgroundColor.g,SceneBackgroundColor.b);});

               BackgroundSettingsField = new PopupField<string>("Background Type");
                  BackgroundSettingsField.choices = BackgroundSettings;
                  BackgroundSettingsField.index = BackgroundType;
                  BackgroundSettingsField.style.width = 550;
                  switch(BackgroundSettingsField.index) {
                     case 0: BackgroundSettingsField.Add(BlankElement); break;
                     case 1: BackgroundSettingsField.Add(InputHDRIField); break;
                     case 2: BackgroundSettingsField.Add(BackgroundColorField); break;
                  }

                  BackgroundSettingsField.RegisterValueChangedCallback(evt => {
                     int Prev = RayMaster.LocalTTSettings.BackgroundType;
                     BackgroundType = BackgroundSettingsField.index;
                     RayMaster.LocalTTSettings.BackgroundType = BackgroundType;
                     switch(BackgroundSettingsField.index) {
                        case 0: BackgroundSettingsField.Add(BlankElement); break;
                        case 1: BackgroundSettingsField.Add(InputHDRIField); break;
                        case 2: BackgroundSettingsField.Add(BackgroundColorField); break;
                     }
                     if(Prev != RayMaster.LocalTTSettings.BackgroundType) BackgroundSettingsField.RemoveAt(2);

                  });
               }
               Slider SkyDesatSlider = new Slider() {label = "SkyDesat: ", value = SkyDesaturate, highValue = 1.0f, lowValue = 0.0f};
               SkyDesatSlider.value = SkyDesaturate;
               SkyDesatSlider.RegisterValueChangedCallback(evt => {SkyDesaturate = evt.newValue; RayMaster.LocalTTSettings.SkyDesaturate = SkyDesaturate;});
               SkyDesatSlider.style.maxWidth = 345;

               BackgroundIntensityField = new FloatField() {value = BackgroundIntensity.x, label = "Primary Background Intensity"};
               BackgroundIntensityField.RegisterValueChangedCallback(evt => {BackgroundIntensity = new Vector2(evt.newValue, BackgroundIntensity.y); RayMaster.LocalTTSettings.BackgroundIntensity = BackgroundIntensity;});
               BackgroundIntensityField.style.maxWidth = 345;

               Slider PrimaryBackgroundContrastSlider = new Slider() {label = "Background Contrast: ", value = PrimaryBackgroundContrast, highValue = 2.0f, lowValue = 0.0f};
               PrimaryBackgroundContrastSlider.value = PrimaryBackgroundContrast;
               PrimaryBackgroundContrastSlider.RegisterValueChangedCallback(evt => {PrimaryBackgroundContrast = evt.newValue; RayMaster.LocalTTSettings.PrimaryBackgroundContrast = PrimaryBackgroundContrast;});
               PrimaryBackgroundContrastSlider.style.maxWidth = 345;
               PrimaryBackgroundContrastSlider.showInputField = true;
               
               ColorField PrimaryBackgroundTintColorField = new ColorField() {label = "Tint Color"};
               PrimaryBackgroundTintColorField.value = PrimaryBackgroundTintColor;
               PrimaryBackgroundTintColorField.style.width = 250;
               PrimaryBackgroundTintColorField.RegisterValueChangedCallback(evt => {PrimaryBackgroundTintColor = evt.newValue; RayMaster.LocalTTSettings.PrimaryBackgroundTintColor = new Vector3(PrimaryBackgroundTintColor.r,PrimaryBackgroundTintColor.g,PrimaryBackgroundTintColor.b);});

               Slider PrimaryBackgroundTintSlider = new Slider() {label = "Tint Strength: ", value = PrimaryBackgroundTint, highValue = 1.0f, lowValue = 0.0f};
               PrimaryBackgroundTintSlider.value = PrimaryBackgroundTint;
               PrimaryBackgroundTintSlider.RegisterValueChangedCallback(evt => {PrimaryBackgroundTint = evt.newValue; RayMaster.LocalTTSettings.PrimaryBackgroundTint = PrimaryBackgroundTint;});
               PrimaryBackgroundTintSlider.style.maxWidth = 345;

            PrimaryBackgroundFoldout.Add(BackgroundSettingsField);
            PrimaryBackgroundFoldout.Add(BackgroundIntensityField);
            PrimaryBackgroundFoldout.Add(SkyDesatSlider);
            PrimaryBackgroundFoldout.Add(PrimaryBackgroundContrastSlider);
            PrimaryBackgroundFoldout.Add(PrimaryBackgroundTintColorField);
            PrimaryBackgroundFoldout.Add(PrimaryBackgroundTintSlider);
        }


         Foldout SecondaryBackgroundFoldout = new Foldout() {text = "Secondary Background"};
            SecondaryBackgroundFoldout.tooltip = "Sky used for indirect lighting";
         {
            {
               SecondaryInputHDRIField = new ObjectField();
               SecondaryInputHDRIField.objectType = typeof(Texture);
               SecondaryInputHDRIField.label = "Drag your skybox here ->";
               SecondaryInputHDRIField.value = RayMaster.SkyboxTexture;
               SecondaryInputHDRIField.RegisterValueChangedCallback(evt => {RayMaster.SkyboxTexture = evt.newValue as Texture; InputHDRIField.value = evt.newValue;});
               
               SecondaryBackgroundColorField = new ColorField();
               SecondaryBackgroundColorField.value = SecondarySceneBackgroundColor;
               SecondaryBackgroundColorField.style.width = 150;
               SecondaryBackgroundColorField.RegisterValueChangedCallback(evt => {SecondarySceneBackgroundColor = evt.newValue; RayMaster.LocalTTSettings.SecondarySceneBackgroundColor = new Vector3(SecondarySceneBackgroundColor.r,SecondarySceneBackgroundColor.g,SecondarySceneBackgroundColor.b);});

               SecondaryBackgroundSettingsField = new PopupField<string>("Secondary Background Type");
               SecondaryBackgroundSettingsField.choices = BackgroundSettings;
               SecondaryBackgroundSettingsField.index = SecondaryBackgroundType;
               SecondaryBackgroundSettingsField.style.width = 550;
               switch(SecondaryBackgroundSettingsField.index) {
                  case 0: SecondaryBackgroundSettingsField.Add(SecondaryBlankElement); break;
                  case 1: SecondaryBackgroundSettingsField.Add(SecondaryInputHDRIField); break;
                  case 2: SecondaryBackgroundSettingsField.Add(SecondaryBackgroundColorField); break;
               }

               SecondaryBackgroundSettingsField.RegisterValueChangedCallback(evt => {
                  int Prev2 = RayMaster.LocalTTSettings.SecondaryBackgroundType;
                  SecondaryBackgroundType = SecondaryBackgroundSettingsField.index;
                  RayMaster.LocalTTSettings.SecondaryBackgroundType = SecondaryBackgroundType;
                  switch(SecondaryBackgroundSettingsField.index) {
                     case 0: SecondaryBackgroundSettingsField.Add(SecondaryBlankElement); break;
                     case 1: SecondaryBackgroundSettingsField.Add(SecondaryInputHDRIField); break;
                     case 2: SecondaryBackgroundSettingsField.Add(SecondaryBackgroundColorField); break;
                  }
                  if(Prev2 != RayMaster.LocalTTSettings.SecondaryBackgroundType) SecondaryBackgroundSettingsField.RemoveAt(2);
               });
            }
            FloatField SecondaryBackgroundIntensityField = new FloatField() {value = BackgroundIntensity.y, label = "Secondary Background Intensity"};
            SecondaryBackgroundIntensityField.RegisterValueChangedCallback(evt => {BackgroundIntensity = new Vector2(BackgroundIntensity.x, evt.newValue); RayMaster.LocalTTSettings.BackgroundIntensity = BackgroundIntensity;});
            SecondaryBackgroundIntensityField.style.maxWidth = 345;
            
            Slider SecondarySkyDesatSlider = new Slider() {label = "Secondary SkyDesat: ", value = SecondarySkyDesaturate, highValue = 1.0f, lowValue = 0.0f};
            SecondarySkyDesatSlider.value = SecondarySkyDesaturate;
            SecondarySkyDesatSlider.RegisterValueChangedCallback(evt => {SecondarySkyDesaturate = evt.newValue; RayMaster.LocalTTSettings.SecondarySkyDesaturate = SecondarySkyDesaturate;});
            SecondarySkyDesatSlider.style.maxWidth = 345;

            SecondaryBackgroundFoldout.Add(SecondaryBackgroundSettingsField);
            SecondaryBackgroundFoldout.Add(SecondaryBackgroundIntensityField);
            SecondaryBackgroundFoldout.Add(SecondarySkyDesatSlider);
         }

      





      UnityLightModifierField = new FloatField() {value = LightEnergyScale, label = "Unity Light Intensity Modifier"};
         UnityLightModifierField.tooltip = "Global emission multiplier for spot/point/direction/area lights";
      UnityLightModifierField.RegisterValueChangedCallback(evt => {LightEnergyScale = evt.newValue; RayMaster.LocalTTSettings.LightEnergyScale = LightEnergyScale;});
      UnityLightModifierField.style.maxWidth = 345;

      FloatField LEMLightModifierField = new FloatField() {value = LEMEnergyScale, label = "LEM Light Intensity Modifier"};
         LEMLightModifierField.tooltip = "Global emission multiplier for emissive meshes";
      LEMLightModifierField.RegisterValueChangedCallback(evt => {LEMEnergyScale = evt.newValue; RayMaster.LocalTTSettings.LEMEnergyScale = LEMEnergyScale;});
      LEMLightModifierField.style.maxWidth = 345;

      IndirectBoostField = new FloatField() {value = IndirectBoost, label = "Indirect Lighting Boost"};
         IndirectBoostField.tooltip = "Global multiplier for indirection lighting strength";
      IndirectBoostField.RegisterValueChangedCallback(evt => {IndirectBoost = evt.newValue; RayMaster.LocalTTSettings.IndirectBoost = IndirectBoost;});
      IndirectBoostField.style.maxWidth = 345;

      ColorField GroundColorField = new ColorField();
         GroundColorField.tooltip = "Ground color when using the procedural atmosphere";
      GroundColorField.label = "Ground Color: ";
      GroundColorField.value = new Color(GroundColor.x, GroundColor.y, GroundColor.z, 1.0f);
      GroundColorField.style.width = 250;
      GroundColorField.RegisterValueChangedCallback(evt => {GroundColor = new Vector3(evt.newValue.r, evt.newValue.g, evt.newValue.b); RayMaster.LocalTTSettings.GroundColor = GroundColor;});


      Toggle TransmittanceInNEEToggle = new Toggle() {value = RayMaster.LocalTTSettings.UseTransmittanceInNEE, text = "Apply Sky Transmittance to Sun"};
         TransmittanceInNEEToggle.tooltip = "Determins if the color of the sun is automatically adjusted based on its position in the sky(like getting more red towards the horizon)";
      TransmittanceInNEEToggle.RegisterValueChangedCallback(evt => {UseTransmittanceInNEE = evt.newValue; RayMaster.LocalTTSettings.UseTransmittanceInNEE = UseTransmittanceInNEE;});

      SceneSettingsMenu.Add(PrimaryBackgroundFoldout);
      SceneSettingsMenu.Add(SecondaryBackgroundFoldout);
      SceneSettingsMenu.Add(UnityLightModifierField);
      SceneSettingsMenu.Add(LEMLightModifierField);
      SceneSettingsMenu.Add(IndirectBoostField);


      VisualElement HDRILongElement = new VisualElement();
         HDRILongElement.style.flexDirection = FlexDirection.Row;
         Slider HDRILongSlider = new Slider() {label = "HDRI Horizontal Offset: ", value = HDRILongLat.x, highValue = 360.0f, lowValue = 0.0f};
         HDRILongSlider.value = HDRILongLat.x;
         HDRILongSlider.style.minWidth = 345;
         HDRILongSlider.style.maxWidth = 345;
         FloatField HDRILongField = new FloatField() {value = HDRILongLat.x};
         HDRILongField.style.maxWidth = 345;
         HDRILongElement.Add(HDRILongSlider);
         HDRILongElement.Add(HDRILongField);
      HDRILongSlider.RegisterValueChangedCallback(evt => {HDRILongLat = new Vector2(evt.newValue, HDRILongLat.y); HDRILongField.value = HDRILongLat.x; RayMaster.LocalTTSettings.HDRILongLat = HDRILongLat;});
      HDRILongField.RegisterValueChangedCallback(evt => {HDRILongLat = new Vector2(evt.newValue, HDRILongLat.y); HDRILongSlider.value = HDRILongLat.x; RayMaster.LocalTTSettings.HDRILongLat = HDRILongLat;});
      #if TTAdvancedSettings
         SceneSettingsMenu.Add(HDRILongElement);
      #endif

      VisualElement HDRILatElement = new VisualElement();
         HDRILatElement.style.flexDirection = FlexDirection.Row;
         Slider HDRILatSlider = new Slider() {label = "HDRI Vertical Offset: ", value = HDRILongLat.y, highValue = 360.0f, lowValue = 0.0f};
         HDRILatSlider.value = HDRILongLat.y;
         HDRILatSlider.style.minWidth = 345;
         HDRILatSlider.style.maxWidth = 345;
         FloatField HDRILatField = new FloatField() {value = HDRILongLat.y};
         HDRILatField.style.maxWidth = 345;
         HDRILatElement.Add(HDRILatSlider);
         HDRILatElement.Add(HDRILatField);
      HDRILatSlider.RegisterValueChangedCallback(evt => {HDRILongLat = new Vector2(HDRILongLat.x, evt.newValue); HDRILatField.value = HDRILongLat.y; RayMaster.LocalTTSettings.HDRILongLat = HDRILongLat;});
      HDRILatField.RegisterValueChangedCallback(evt => {HDRILongLat = new Vector2(HDRILongLat.x, evt.newValue); HDRILatSlider.value = HDRILongLat.y; RayMaster.LocalTTSettings.HDRILongLat = HDRILongLat;});
      #if TTAdvancedSettings
         SceneSettingsMenu.Add(HDRILatElement);
      #endif
     VisualElement HDRIScaleElement = new VisualElement();
         HDRIScaleElement.style.flexDirection = FlexDirection.Row;
         FloatField HDRIXScale = new FloatField() {label = "HDRI Scaling X: ", value = HDRIScale.x};
         HDRIXScale.style.minWidth = 200;
         HDRIXScale.style.maxWidth = 200;
         HDRIXScale.ElementAt(0).style.minWidth = 65;
         HDRIXScale.ElementAt(1).style.width = 45;
         FloatField HDRIYScale = new FloatField() {label = "Y: ", value = HDRIScale.y};
         HDRIYScale.style.maxWidth = 200;
         HDRIYScale.ElementAt(0).style.minWidth = 65;
         HDRIYScale.ElementAt(1).style.width = 45;
         HDRIScaleElement.Add(HDRIXScale);
         HDRIScaleElement.Add(HDRIYScale);
      HDRIXScale.RegisterValueChangedCallback(evt => {HDRIScale = new Vector2(evt.newValue, HDRIScale.y); RayMaster.LocalTTSettings.HDRIScale = HDRIScale;});
      HDRIYScale.RegisterValueChangedCallback(evt => {HDRIScale = new Vector2(HDRIScale.x, evt.newValue); RayMaster.LocalTTSettings.HDRIScale = HDRIScale;});
      #if TTAdvancedSettings
         SceneSettingsMenu.Add(HDRIScaleElement);
         SceneSettingsMenu.Add(TransmittanceInNEEToggle);
         SceneSettingsMenu.Add(GroundColorField);
      #endif



      }

      VisualElement HierarchyOptionsMenu;


      private enum Properties {AlbedoColor, 
                                       AlbedoTexture, 
                                       NormalTexture,
                                       EmissionTexture,
                                       MetallicTexture,
                                       MetallicSlider,
                                       MetallicMin,
                                       MetallicMax,
                                       RoughnessTexture,
                                       RoughnessSlider,
                                       RoughnessMin,
                                       RoughnessMax,
                                       AlphaTexture,
                                       MatCapTexture,
                                       MatCapMask,
                                       SecondaryAlbedoTexture,
                                       SecondaryAlbedoTextureMask,
                                       SecondaryNormalTexture,
                                       DiffTransTexture,
                                       EmissionColor,
                                       EmissionIntensity,
                                       MatCapColor,
                                       };

      VisualElement MaterialPairingMenu;
      ObjectField InputMaterialField;
      Toggle GlassToggle;
      Toggle CutoutToggle;
      Toggle SmoothnessToggle;
      MaterialShader MatShader;
      int Index;
      List<string> TextureProperties;
      List<string> ColorProperties;
      List<string> FloatProperties;
      DialogueNode OutputNode;
      TexturePairs ConnectChildren(ref List<TexturePairs> TargetList, int CurrentIndex) {
         if(CurrentIndex > 0) {
            return new TexturePairs() {
                     Purpose = TargetList[CurrentIndex].Purpose,
                     ReadIndex = TargetList[CurrentIndex].ReadIndex,
                     TextureName = TargetList[CurrentIndex].TextureName,
                     Fallback = ConnectChildren(ref TargetList, CurrentIndex - 1)
                  };                   
         } else {
            return new TexturePairs() {
                     Purpose = TargetList[CurrentIndex].Purpose,
                     ReadIndex = TargetList[CurrentIndex].ReadIndex,
                     TextureName = TargetList[CurrentIndex].TextureName,
                     Fallback = null
                  };                    
         }
      }
      void ConfirmMats() {
         
         MatShader.AvailableTextures = new List<TexturePairs>();
         List<int> AvailableEdges = new List<int>();
         List<DialogueNode> AvailableIndexes = new List<DialogueNode>();
         for(int i = 0; i < OutputNode.inputContainer.childCount; i++) {
            if((OutputNode.inputContainer[i] as Port).connected) {
               DialogueNode TempNode = ((((OutputNode.inputContainer[i] as Port).connections.ToList())[0].output as Port).node as DialogueNode);
               TempNode.PropertyIndex = i;
               AvailableIndexes.Add(TempNode);
            }
         }


         for(int i = 0; i < AvailableIndexes.Count; i++) {

            int Prop = (int)AvailableIndexes[i].PropertyIndex;
            List<TexturePairs> FallbackNodes = new List<TexturePairs>();
            TexturePairs FallbackNode = null;
            if(Prop != (int)Properties.MatCapColor && Prop != (int)Properties.EmissionColor && Prop != (int)Properties.EmissionIntensity && Prop != (int)Properties.AlbedoColor && Prop != (int)Properties.MetallicSlider && Prop != (int)Properties.MetallicMin && Prop != (int)Properties.MetallicMax && Prop != (int)Properties.RoughnessSlider && Prop != (int)Properties.RoughnessMin && Prop != (int)Properties.RoughnessMax) {
               if((AvailableIndexes[i].inputContainer[0] as Port).connections.ToList().Count != 0) {
                  int Purpose = 0;
                  int ReadIndex = 0;
                  switch(Prop) {
                     case((int)Properties.AlbedoTexture):Purpose = (int)TexturePurpose.Albedo;break;
                     case((int)Properties.NormalTexture):Purpose = (int)TexturePurpose.Normal;break;
                     case((int)Properties.EmissionTexture):Purpose = (int)TexturePurpose.Emission;break;
                     case((int)Properties.MetallicTexture):Purpose = (int)TexturePurpose.Metallic;break;
                     case((int)Properties.RoughnessTexture):Purpose = (int)TexturePurpose.Roughness;break;
                     case((int)Properties.AlphaTexture):Purpose = (int)TexturePurpose.Alpha;break;
                     case((int)Properties.MatCapTexture):Purpose = (int)TexturePurpose.MatCapTex;break;
                     case((int)Properties.MatCapMask):Purpose = (int)TexturePurpose.MatCapMask;break;
                     case((int)Properties.SecondaryAlbedoTexture):Purpose = (int)TexturePurpose.SecondaryAlbedoTexture;break;
                     case((int)Properties.SecondaryAlbedoTextureMask):Purpose = (int)TexturePurpose.SecondaryAlbedoTextureMask;break;
                     case((int)Properties.SecondaryNormalTexture):Purpose = (int)TexturePurpose.SecondaryNormalTexture;break;
                     case((int)Properties.DiffTransTexture):Purpose = (int)TexturePurpose.DiffTransTex;break;
                  }



                  DialogueNode CurrentNode = ((((AvailableIndexes[i].inputContainer[0] as Port).connections.ToList())[0].output as Port).node as DialogueNode);
                  switch(Prop) {
                     case((int)Properties.AlbedoTexture):ReadIndex = -4;break;
                     case((int)Properties.NormalTexture):ReadIndex = -3;break;
                     case((int)Properties.EmissionTexture):ReadIndex = -4;break;
                     case((int)Properties.MetallicTexture):ReadIndex = ChannelProperties.IndexOf(CurrentNode.GUID);  break;
                     case((int)Properties.RoughnessTexture):ReadIndex = ChannelProperties.IndexOf(CurrentNode.GUID);  break;
                     case((int)Properties.AlphaTexture):ReadIndex = ChannelProperties.IndexOf(CurrentNode.GUID);break;
                     case((int)Properties.MatCapTexture):ReadIndex = -4;break;
                     case((int)Properties.MatCapMask):ReadIndex = ChannelProperties.IndexOf(CurrentNode.GUID);break;
                     case((int)Properties.SecondaryAlbedoTexture):ReadIndex = -4;break;
                     case((int)Properties.SecondaryAlbedoTextureMask):ReadIndex = ChannelProperties.IndexOf(CurrentNode.GUID);break;
                     case((int)Properties.SecondaryNormalTexture):ReadIndex = -3;break;
                     case((int)Properties.DiffTransTexture):ReadIndex = ChannelProperties.IndexOf(CurrentNode.GUID);  break;
                  }
                  FallbackNodes.Add(new TexturePairs() {
                     Purpose = Purpose,
                     ReadIndex = ReadIndex,
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(CurrentNode.title)]
                  });                    
                  while((CurrentNode.inputContainer[0] as Port).connections.ToList().Count != 0) {
                     CurrentNode = ((((CurrentNode.inputContainer[0] as Port).connections.ToList())[0].output as Port).node as DialogueNode);
                     switch(Prop) {
                        case((int)Properties.AlbedoTexture):ReadIndex = -4;break;
                        case((int)Properties.NormalTexture):ReadIndex = -3;break;
                        case((int)Properties.EmissionTexture):ReadIndex = -4;break;
                        case((int)Properties.MetallicTexture):ReadIndex = ChannelProperties.IndexOf(CurrentNode.GUID);  break;
                        case((int)Properties.RoughnessTexture):ReadIndex = ChannelProperties.IndexOf(CurrentNode.GUID);  break;
                        case((int)Properties.AlphaTexture):ReadIndex = ChannelProperties.IndexOf(CurrentNode.GUID);break;
                        case((int)Properties.MatCapTexture):ReadIndex = -4;break;
                        case((int)Properties.MatCapMask):ReadIndex = ChannelProperties.IndexOf(CurrentNode.GUID);break;
                        case((int)Properties.SecondaryAlbedoTexture):ReadIndex = -4;break;
                        case((int)Properties.SecondaryAlbedoTextureMask):ReadIndex = ChannelProperties.IndexOf(CurrentNode.GUID);break;
                        case((int)Properties.SecondaryNormalTexture):ReadIndex = -3;break;
                        case((int)Properties.DiffTransTexture):ReadIndex = ChannelProperties.IndexOf(CurrentNode.GUID);  break;
                     }
                     FallbackNodes.Add(new TexturePairs() {
                        Purpose = Purpose,
                        ReadIndex = ReadIndex,
                        TextureName = TextureProperties[VerboseTextureProperties.IndexOf(CurrentNode.title)]
                     });                    
                  }
                  int RecurseCount = FallbackNodes.Count;
                  FallbackNode = ConnectChildren(ref FallbackNodes, RecurseCount - 1);
               } 
            }

            switch(Prop) {
               case((int)Properties.MatCapColor):
                  MatShader.MatCapColorValue = ColorProperties[VerboseColorProperties.IndexOf(AvailableIndexes[i].title)];
               break;
               case((int)Properties.EmissionColor):
                  MatShader.EmissionColorValue = ColorProperties[VerboseColorProperties.IndexOf(AvailableIndexes[i].title)];
               break;
               case((int)Properties.EmissionIntensity):
                  MatShader.EmissionIntensityValue = FloatProperties[VerboseFloatProperties.IndexOf(AvailableIndexes[i].title)];
               break;
               case((int)Properties.AlbedoColor):
                  MatShader.BaseColorValue = ColorProperties[VerboseColorProperties.IndexOf(AvailableIndexes[i].title)];
               break;
               case((int)Properties.AlbedoTexture):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.Albedo,
                     ReadIndex = -4,
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)],
                     Fallback = FallbackNode
                  });  
               break;
               case((int)Properties.NormalTexture):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.Normal,
                     ReadIndex = -3,
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)],
                     Fallback = FallbackNode
                  });  
               break;
               case((int)Properties.EmissionTexture):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.Emission,
                     ReadIndex = -4,
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)],
                     Fallback = FallbackNode
                  });  
               break;
               case((int)Properties.MetallicTexture):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.Metallic,
                     ReadIndex = ChannelProperties.IndexOf(AvailableIndexes[i].GUID),
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)],
                     Fallback = FallbackNode
                  });  
               break;
               case((int)Properties.DiffTransTexture):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.DiffTransTex,
                     ReadIndex = ChannelProperties.IndexOf(AvailableIndexes[i].GUID),
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)],
                     Fallback = FallbackNode
                  });  
               break;
               case((int)Properties.MetallicSlider):
                  MatShader.MetallicRange = FloatProperties[VerboseFloatProperties.IndexOf(AvailableIndexes[i].title)];
               break;
               case((int)Properties.MetallicMin):
                  MatShader.MetallicRemapMin = FloatProperties[VerboseFloatProperties.IndexOf(AvailableIndexes[i].title)];
               break;
               case((int)Properties.MetallicMax):
                  MatShader.MetallicRemapMax = FloatProperties[VerboseFloatProperties.IndexOf(AvailableIndexes[i].title)];
               break;
               case((int)Properties.RoughnessTexture):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.Roughness,
                     ReadIndex = ChannelProperties.IndexOf(AvailableIndexes[i].GUID),
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)],
                     Fallback = FallbackNode
                  });  
               break;
               case((int)Properties.RoughnessSlider):
                  MatShader.RoughnessRange = FloatProperties[VerboseFloatProperties.IndexOf(AvailableIndexes[i].title)];
               break;
               case((int)Properties.RoughnessMin):
                  MatShader.RoughnessRemapMin = FloatProperties[VerboseFloatProperties.IndexOf(AvailableIndexes[i].title)];
               break;
               case((int)Properties.RoughnessMax):
                  MatShader.RoughnessRemapMax = FloatProperties[VerboseFloatProperties.IndexOf(AvailableIndexes[i].title)];
               break;
               case((int)Properties.AlphaTexture):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.Alpha,
                     ReadIndex = ChannelProperties.IndexOf(AvailableIndexes[i].GUID),
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)],
                     Fallback = FallbackNode
                  });  
               break;
               case((int)Properties.MatCapTexture):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.MatCapTex,
                     ReadIndex = -4,
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)],
                     Fallback = FallbackNode
                  });  
               break;
               case((int)Properties.MatCapMask):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.MatCapMask,
                     ReadIndex = ChannelProperties.IndexOf(AvailableIndexes[i].GUID),
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)],
                     Fallback = FallbackNode
                  });  
               break;
               case((int)Properties.SecondaryAlbedoTexture):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.SecondaryAlbedoTexture,
                     ReadIndex = -4,
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)],
                     Fallback = FallbackNode
                  });  
               break;
               case((int)Properties.SecondaryAlbedoTextureMask):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.SecondaryAlbedoTextureMask,
                     ReadIndex = ChannelProperties.IndexOf(AvailableIndexes[i].GUID),
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)],
                     Fallback = FallbackNode
                  });  
               break;
               case((int)Properties.SecondaryNormalTexture):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.SecondaryNormalTexture,
                     ReadIndex = -3,
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)],
                     Fallback = FallbackNode
                  });  
               break;
            }
         }


      

         MatShader.IsGlass = GlassToggle.value;
         MatShader.IsCutout = CutoutToggle.value;
         MatShader.UsesSmoothness = SmoothnessToggle.value;
         AssetManager.data.Material[Index] = MatShader;
         string materialMappingsPath = TTPathFinder.GetMaterialMappingsPath();
         using(StreamWriter writer = new StreamWriter(materialMappingsPath)) {
               // int AssetCount = AssetManager.data.Material.Count;
               // int Counter = 0;
               // for(int i = AssetCount - 1; i >= 0; i--) {//143
               //    if(AssetManager.data.Material[i].Name.Contains("Hidden/.poiyomi/")) {AssetManager.data.Material.RemoveAt(i); Counter++;}
               // }
               // Debug.Log("Removed: " + Counter);
            var serializer = new XmlSerializer(typeof(Materials));
            serializer.Serialize(writer.BaseStream, AssetManager.data);
            AssetDatabase.Refresh();
            
               Assets.UpdateMaterialDefinition();
         }
      }
      List<string> ChannelProperties;
      List<string> VerboseFloatProperties;
      List<string> VerboseColorProperties;
      List<string> VerboseTextureProperties;
      public DialogueNode CreateInputNode(string PropertyID, System.Type T, Vector2 Pos, string InitialValue = "null", int InputElement = -1, bool IsRange = false, Texture SampleTex = null) 
      {
           var dialogueNode = new DialogueNode() {
               title = PropertyID,
               DialogueText = PropertyID,
               GUID = PropertyID
           };

            PopupField<string> DropField = new PopupField<string>(PropertyID);
           var inputPort = _graphView.GeneratePort(dialogueNode, Direction.Output, T, Port.Capacity.Multi);
           inputPort.portName = PropertyID;
           dialogueNode.outputContainer.Add(inputPort);
           if(T == typeof(Texture)) {
           var FallbackPort = _graphView.GeneratePort(dialogueNode, Direction.Input, T, Port.Capacity.Multi);
           FallbackPort.portName = PropertyID;
           dialogueNode.inputContainer.Add(FallbackPort);
           dialogueNode.inputContainer.Add(DropField);
               // if(InputElement != -1) {
                  PopupField<string> ChannelField = new PopupField<string>("Read Channel");
                  ChannelField.choices = ChannelProperties;               
                  ChannelField.index = Mathf.Max(InputElement, 0);
                  dialogueNode.GUID = ChannelProperties[ChannelField.index];
                  dialogueNode.inputContainer.Add(ChannelField);
                  ChannelField.visible = InputElement != -1;

                  // Debug.Log(dialogueNode.inputContainer.childCount);
                  ChannelField.RegisterValueChangedCallback(evt => {dialogueNode.GUID = ChannelProperties[ChannelField.index];});
               // }
               DropField.choices = TextureProperties;
               DropField.index = (int)Mathf.Max(TextureProperties.IndexOf(InitialValue),0);
               dialogueNode.title = VerboseTextureProperties[DropField.index];
               Image TexPreview = new Image();
               if(DropField.index > 0) TexPreview.image = (InputMaterialField.value as Material).GetTexture(TextureProperties[DropField.index]);
               TexPreview.style.width = 100;
               TexPreview.style.height = 100;
               dialogueNode.outputContainer.Add(TexPreview);
               DropField.RegisterValueChangedCallback(evt => {dialogueNode.title = VerboseTextureProperties[DropField.index]; TexPreview.image = (InputMaterialField.value as Material).GetTexture(TextureProperties[DropField.index]);});
           } else if(T == typeof(Color)) {
               DropField.choices = ColorProperties;
               DropField.index = (int)Mathf.Max(ColorProperties.IndexOf(InitialValue),0);
               dialogueNode.title = VerboseColorProperties[DropField.index];
               DropField.RegisterValueChangedCallback(evt => {dialogueNode.title = VerboseColorProperties[DropField.index];});
               dialogueNode.inputContainer.Add(DropField);
           }else if(T == typeof(float)) {
                  DropField.choices = FloatProperties;
                  DropField.index = (int)Mathf.Max(FloatProperties.IndexOf(InitialValue),0);
                  dialogueNode.title = VerboseFloatProperties[DropField.index];
                  DropField.RegisterValueChangedCallback(evt => {dialogueNode.title = VerboseFloatProperties[DropField.index];});
               dialogueNode.inputContainer.Add(DropField);
           }

           dialogueNode.RefreshExpandedState();
           dialogueNode.RefreshPorts();
           dialogueNode.SetPosition(new Rect(Pos, new Vector2(50, 100)));

           return dialogueNode;
      }

     private int CalcLevenshteinDistance2() {
        string s = "AAAA";
        string t = "AAAA";
         // Special cases
         if (s == t) return 0;
         if (s.Length == 0) return t.Length;
         if (t.Length == 0) return s.Length;
         // Initialize the distance matrix
         int[, ] distance = new int[s.Length + 1, t.Length + 1];
         for (int i = 0; i <= s.Length; i++) distance[i, 0] = i;
         for (int j = 0; j <= t.Length; j++) distance[0, j] = j;
         // Calculate the distance
         for (int i = 1; i <= s.Length; i++) {
             for (int j = 1; j <= t.Length; j++) {
                 int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                 distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + cost);
             }
         }
         // Return the distance
         return distance[s.Length, t.Length];
     }


    private int CalcLevenshteinDistance() {
        string InputVal = "AAAA";
        string ComparativeVal = "AAAA";
        if(InputVal.Equals(ComparativeVal)) return 0;
        int InputLength = InputVal.Length;
        int ComparativeLength = ComparativeVal.Length;
        if(InputLength == 0) return ComparativeLength;
        if(ComparativeLength == 0) return InputLength;

        int[] Distances = new int[(InputLength + 1) * (ComparativeLength + 1)];
        for (int i = 0; i <= InputLength; i++) Distances[i] = i;
        for (int j = 0; j <= ComparativeLength; j++) Distances[j * (InputLength + 1)] = j;
        // Calculate the distance
        for (int i = 1; i <= InputLength; i++) {
            for (int j = 1; j <= ComparativeLength; j++) {
                int cost = (InputVal[i - 1] == ComparativeVal[j - 1]) ? 0 : 1;
                Distances[i + j * (InputLength + 1)] = Math.Min(Math.Min(Distances[i + j * (InputLength + 1) - 1] + 1, Distances[i + (j - 1) * (InputLength + 1)] + 1), Distances[i + (j - 1) * (InputLength + 1) - 1] + cost);
            }
        }
        // Return the distance
        return Distances[InputLength + ComparativeLength * (InputLength + 1)];


    }


      private DialogueGraphView _graphView;
      void AddAssetsToMenu() {


         Shader shader = (InputMaterialField.value as Material).shader;
         FloatProperties = new List<string>();
         ColorProperties = new List<string>();
         TextureProperties = new List<string>();
         ChannelProperties = new List<string>();
         VerboseFloatProperties = new List<string>();
         VerboseColorProperties = new List<string>();
         VerboseTextureProperties = new List<string>();
         int PropCount = shader.GetPropertyCount();
         ColorProperties.Add("null");
         FloatProperties.Add("null");
         TextureProperties.Add("null");
         ChannelProperties.Add("R");
         ChannelProperties.Add("G");
         ChannelProperties.Add("B");
         ChannelProperties.Add("A");

         VerboseColorProperties.Add("null");
         VerboseFloatProperties.Add("null");
         VerboseTextureProperties.Add("null");
         for(int i = 0; i < PropCount; i++) {
            if(shader.GetPropertyType(i) == ShaderPropertyType.Texture) {TextureProperties.Add(shader.GetPropertyName(i)); VerboseTextureProperties.Add(shader.GetPropertyName(i));}
            if(shader.GetPropertyType(i) == ShaderPropertyType.Color) {ColorProperties.Add(shader.GetPropertyName(i)); VerboseColorProperties.Add(shader.GetPropertyDescription(i));}
            if(shader.GetPropertyType(i) == ShaderPropertyType.Float || shader.GetPropertyType(i) == ShaderPropertyType.Range) {FloatProperties.Add(shader.GetPropertyName(i)); VerboseFloatProperties.Add(shader.GetPropertyDescription(i));}
         }
         MatShader = AssetManager.data.Material.Find((s1) => s1.Name.Equals(shader.name));
         Index = AssetManager.data.Material.IndexOf(MatShader);
         if(Index == -1) {
            if(Assets != null && Assets.NeedsToUpdateXML) {
               Assets.AddMaterial(shader);
               string materialMappingsPath = TTPathFinder.GetMaterialMappingsPath();
               using(StreamWriter writer = new StreamWriter(materialMappingsPath)) {
                  var serializer = new XmlSerializer(typeof(Materials));
                  serializer.Serialize(writer.BaseStream, AssetManager.data);
                  AssetDatabase.Refresh();
               }
               Assets.UpdateMaterialDefinition();
               Index = AssetManager.data.Material.IndexOf(MatShader);
            }
         }

        
         GlassToggle = new Toggle() {value = MatShader.IsGlass, text = "Force Glass On All Objects With This Material"};
         MaterialPairingMenu.Add(GlassToggle);
         CutoutToggle = new Toggle() {value = MatShader.IsCutout, text = "Force All Objects With This Material To Be Cutout"};
         MaterialPairingMenu.Add(CutoutToggle);
         SmoothnessToggle = new Toggle() {value = MatShader.UsesSmoothness, text = "Does this Material use Smoothness(True) or Roughness(False)"};
         MaterialPairingMenu.Add(SmoothnessToggle);

         Button ConfirmMaterialButton = new Button(() => ConfirmMats()) {text = "Apply Material Links"};
         MaterialPairingMenu.Add(ConfirmMaterialButton);


         _graphView = new DialogueGraphView
         {
            name = "Dialogue Graph"
         };
         _graphView.SetupZoom(0.1f, 5.0f);
         VisualElement Spacer = new VisualElement();

         Spacer.style.width = 1800;
         Spacer.style.height = 1800;

         Spacer.Add(_graphView);
         _graphView.StretchToParentSize();
         




         OutputNode = new DialogueNode() {
            title = "Output",
            DialogueText = "Output",
            GUID = "Output"
         };

         

         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Color), Port.Capacity.Single, "Base Color"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "Base Texture"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "Normal Texture"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "Emission Texture"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "Metallic Texture"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(float), Port.Capacity.Single, "Metallic Range"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(float), Port.Capacity.Single, "Metallic Min"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(float), Port.Capacity.Single, "Metallic Max"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "Roughness Texture"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(float), Port.Capacity.Single, "Roughness Range"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(float), Port.Capacity.Single, "Roughness Min"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(float), Port.Capacity.Single, "Roughness Max"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "Alpha Texture"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "MatCap Texture"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "MatCap Mask"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "Secondary Base Texture"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "Secondary Base Texture Mask"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "Detail Normal Texture"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "DiffTrans Texture"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Color), Port.Capacity.Single, "Emission Color"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(float), Port.Capacity.Single, "Emission Intensity"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(float), Port.Capacity.Single, "MatCap Color"));

         _graphView.AddElement(OutputNode);
         Vector2 Pos = new Vector2(30, 10);
         List<int> AvailableTexturesPurposes = new List<int>();
         for(int i = 0; i < MatShader.AvailableTextures.Count; i++) {
            AvailableTexturesPurposes.Add(MatShader.AvailableTextures[i].Purpose);
         }
         for(int i = 0; i < MatShader.AvailableTextures.Count; i++) {
            Pos.x = 30;
            DialogueNode ThisNode = new DialogueNode();
            Edge ThisEdge = new Edge();
            TexturePairs CurrentPair = MatShader.AvailableTextures[i];
            switch((int)CurrentPair.Purpose) {
               case((int)TexturePurpose.SecondaryNormalTexture):
                  Pos.y = 1380;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.SecondaryNormalTexture] as Port);
               break;
               case((int)TexturePurpose.SecondaryAlbedoTextureMask):
                  Pos.y = 1300;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName, CurrentPair.ReadIndex);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.SecondaryAlbedoTextureMask] as Port);
               break;
               case((int)TexturePurpose.SecondaryAlbedoTexture):
                  Pos.y = 1220;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.SecondaryAlbedoTexture] as Port);
               break;
               case((int)TexturePurpose.MatCapMask):
                  Pos.y = 1140;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName, CurrentPair.ReadIndex);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.MatCapMask] as Port);
               break;
               case((int)TexturePurpose.MatCapTex):
                  Pos.y = 1060;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.MatCapTexture] as Port);
               break;
               case((int)TexturePurpose.Alpha):
                  Pos.y = 980;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName, CurrentPair.ReadIndex);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.AlphaTexture] as Port);
               break;
               case((int)TexturePurpose.Metallic):
                  Pos.y = 340;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName, CurrentPair.ReadIndex);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.MetallicTexture] as Port);
               break;
               case((int)TexturePurpose.Roughness):
                  Pos.y = 660;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName, CurrentPair.ReadIndex);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.RoughnessTexture] as Port);
               break;
               case((int)TexturePurpose.Albedo):
                  Pos.y = 100;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.AlbedoTexture] as Port);
               break;
               case((int)TexturePurpose.Normal):
                  Pos.y = 180;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.NormalTexture] as Port);
               break;
               case((int)TexturePurpose.Emission):
                  Pos.y = 260;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.EmissionTexture] as Port);
               break;
               case((int)TexturePurpose.DiffTransTex):
                  Pos.y = 1460;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName, CurrentPair.ReadIndex);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.DiffTransTexture] as Port);
               break;
            }
            _graphView.AddElement(ThisNode);
            _graphView.AddElement(ThisEdge);
            if(CurrentPair.Fallback != null) {
               do {
                  DialogueNode PrevNode = ThisNode;
                  CurrentPair = CurrentPair.Fallback;
                  Pos.x -= 600;
                  switch((int)CurrentPair.Purpose) {
                     case((int)TexturePurpose.SecondaryNormalTexture):
                        Pos.y = 1380;
                        ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName);
                     break;
                     case((int)TexturePurpose.SecondaryAlbedoTextureMask):
                        Pos.y = 1300;
                        ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName, CurrentPair.ReadIndex);
                     break;
                     case((int)TexturePurpose.SecondaryAlbedoTexture):
                        Pos.y = 1220;
                        ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName);
                     break;
                     case((int)TexturePurpose.MatCapMask):
                        Pos.y = 1140;
                        ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName, CurrentPair.ReadIndex);
                     break;
                     case((int)TexturePurpose.MatCapTex):
                        Pos.y = 1060;
                        ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName);
                     break;
                     case((int)TexturePurpose.Alpha):
                        Pos.y = 980;
                        ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName, CurrentPair.ReadIndex);
                     break;
                     case((int)TexturePurpose.Metallic):
                        Pos.y = 340;
                        ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName, CurrentPair.ReadIndex);
                     break;
                     case((int)TexturePurpose.DiffTransTex):
                        Pos.y = 1460;
                        ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName, CurrentPair.ReadIndex);
                     break;
                     case((int)TexturePurpose.Roughness):
                        Pos.y = 660;
                        ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName, CurrentPair.ReadIndex);
                     break;
                     case((int)TexturePurpose.Albedo):
                        Pos.y = 100;
                        ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName);
                     break;
                     case((int)TexturePurpose.Normal):
                        Pos.y = 180;
                        ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName);
                     break;
                     case((int)TexturePurpose.Emission):
                        Pos.y = 260;
                        ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, CurrentPair.TextureName);
                     break;
                  }
                        ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(PrevNode.inputContainer[0] as Port);

                  _graphView.AddElement(ThisNode);
                  _graphView.AddElement(ThisEdge);
               }  while(CurrentPair.Fallback != null);
            }
         }

         {
            int Index = ColorProperties.IndexOf(MatShader.BaseColorValue);
            if(Index > 0) {
               DialogueNode ThisNode = new DialogueNode();
                  Pos.y = 20;
                ThisNode = CreateInputNode("Color", typeof(Color), Pos, MatShader.BaseColorValue);
               _graphView.AddElement(ThisNode);
               _graphView.AddElement((ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.AlbedoColor] as Port));
            }
            Index = ColorProperties.IndexOf(MatShader.EmissionColorValue);
            if(Index > 0) {
               DialogueNode ThisNode = new DialogueNode();
                  Pos.y = 1540;
                ThisNode = CreateInputNode("Color", typeof(Color), Pos, MatShader.EmissionColorValue);
               _graphView.AddElement(ThisNode);
               _graphView.AddElement((ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.EmissionColor] as Port));
            }
            Index = ColorProperties.IndexOf(MatShader.MatCapColorValue);
            if(Index > 0) {
               DialogueNode ThisNode = new DialogueNode();
                  Pos.y = 1700;
                ThisNode = CreateInputNode("MatCap Color", typeof(Color), Pos, MatShader.MatCapColorValue);
               _graphView.AddElement(ThisNode);
               _graphView.AddElement((ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.MatCapColor] as Port));
            }
            Index = FloatProperties.IndexOf(MatShader.EmissionIntensityValue);
            if(Index > 0) {
               DialogueNode ThisNode = new DialogueNode();
               Pos.y = 1620;
                ThisNode = CreateInputNode("Float", typeof(float), Pos, MatShader.EmissionIntensityValue);
               _graphView.AddElement(ThisNode);
               _graphView.AddElement((ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.EmissionIntensity] as Port));
            }

            Index = FloatProperties.IndexOf(MatShader.MetallicRange);
            if(Index > 0) {
               DialogueNode ThisNode = new DialogueNode();
               Pos.y = 420;
                ThisNode = CreateInputNode("Float", typeof(float), Pos, MatShader.MetallicRange);
               _graphView.AddElement(ThisNode);
               _graphView.AddElement((ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.MetallicSlider] as Port));
            }

            Index = FloatProperties.IndexOf(MatShader.RoughnessRange);
            if(Index > 0) {
               DialogueNode ThisNode = new DialogueNode();
                  Pos.y = 740;
                ThisNode = CreateInputNode("Float", typeof(float), Pos, MatShader.RoughnessRange);
               _graphView.AddElement(ThisNode);
               _graphView.AddElement((ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.RoughnessSlider] as Port));
            }

            Index = FloatProperties.IndexOf(MatShader.MetallicRemapMin);
            if(Index > 0) {
               DialogueNode ThisNode = new DialogueNode();
                  Pos.y = 500;
                ThisNode = CreateInputNode("Float", typeof(float), Pos, MatShader.MetallicRemapMin, -1, true);
               _graphView.AddElement(ThisNode);
               _graphView.AddElement((ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.MetallicMin] as Port));
            }
            Index = FloatProperties.IndexOf(MatShader.MetallicRemapMax);
            if(Index > 0) {
               DialogueNode ThisNode = new DialogueNode();
                  Pos.y = 580;
                ThisNode = CreateInputNode("Float", typeof(float), Pos, MatShader.MetallicRemapMax, -1, true);
               _graphView.AddElement(ThisNode);
               _graphView.AddElement((ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.MetallicMax] as Port));
            }
            Index = FloatProperties.IndexOf(MatShader.RoughnessRemapMin);
            if(Index > 0) {
               DialogueNode ThisNode = new DialogueNode();
                  Pos.y = 820;
                ThisNode = CreateInputNode("Float", typeof(float), Pos, MatShader.RoughnessRemapMin, -1, true);
               _graphView.AddElement(ThisNode);
               _graphView.AddElement((ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.RoughnessMin] as Port));
            }
            Index = FloatProperties.IndexOf(MatShader.RoughnessRemapMax);
            if(Index > 0) {
               DialogueNode ThisNode = new DialogueNode();
                ThisNode = CreateInputNode("Float", typeof(float), Pos, MatShader.RoughnessRemapMax, -1, true);
               _graphView.AddElement(ThisNode);
               _graphView.AddElement((ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.RoughnessMax] as Port));
               Pos.y = 900;
            }


         }

         OutputNode.RefreshExpandedState();
         OutputNode.RefreshPorts();
         OutputNode.SetPosition(new Rect(new Vector2(500, 50), new Vector2(150,200)));

        var toolbar = new Toolbar();


        var TextureNodeButton = new Button(() => {_graphView.AddElement(CreateInputNode("Texture", typeof(Texture), new Vector2(0,0), "null"));}) {text = "Texture"};
        var FloatNodeButton = new Button(() => {_graphView.AddElement(CreateInputNode("Float", typeof(float), new Vector2(0,0), "null"));}) {text = "Float"};
        var ColorNodeButton = new Button(() => {_graphView.AddElement(CreateInputNode("Color", typeof(Color), new Vector2(0,0), "null"));}) {text = "Color"};

        toolbar.Add(TextureNodeButton);//partial textures will be defined as according to the output they are connected to 
        toolbar.Add(FloatNodeButton);
        toolbar.Add(ColorNodeButton);
        MaterialPairingMenu.Add(toolbar);
        MaterialPairingMenu.Add(Spacer);

        
      }

      List<string> definesList;

       private void RemoveDefine(string define) {
           definesList = GetDefines();
           if (definesList.Contains(define))
               definesList.Remove(define);
            SetDefines();
       }

       private void AddDefine(string define) {
           definesList = GetDefines();
           if (!definesList.Contains(define))
               definesList.Add(define);
            SetDefines();
       }
    
      private List<string> GetDefines() {
         var target = EditorUserBuildSettings.activeBuildTarget;
         var group = BuildPipeline.GetBuildTargetGroup(target);
         var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
         var defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
         return defines.Split(';').ToList();
      }
    
      private void SetDefines() {
         var target = EditorUserBuildSettings.activeBuildTarget;
         var group = BuildPipeline.GetBuildTargetGroup(target);
         var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
         var defines = string.Join(";", definesList.ToArray());
         PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);
      }

      private void RemoveTrueTrace() {
         GameObject SceneObject = GameObject.Find("Scene");
         int ChildCount = SceneObject.transform.childCount;
         for(int i = ChildCount - 1; i >= 0; i--) {
            SceneObject.transform.GetChild(i).parent = null;
         }
         RayTracingObject[] TempObjects = GameObject.FindObjectsOfType<RayTracingObject>();
         foreach(var a in TempObjects) DestroyImmediate(a);
         ParentObject[] TempObjects2 = GameObject.FindObjectsOfType<ParentObject>();
         foreach(var a in TempObjects2) DestroyImmediate(a);
         RayTracingLights[] TempObjects3 = GameObject.FindObjectsOfType<RayTracingLights>();
         foreach(var a in TempObjects3) DestroyImmediate(a);
         InstancedObject[] TempObjects4 = GameObject.FindObjectsOfType<InstancedObject>();
         foreach(var a in TempObjects4) DestroyImmediate(a);
         GameObject InstanceObject = GameObject.Find("InstancedStorage");
         DestroyImmediate(InstanceObject);
         DestroyImmediate(SceneObject);
         RenderHandle[] TempObjects5 = GameObject.FindObjectsOfType<RenderHandle>();
         foreach(var a in TempObjects5) DestroyImmediate(a);
         FlyCamera[] TempObjects6 = GameObject.FindObjectsOfType<FlyCamera>();
         foreach(var a in TempObjects6) DestroyImmediate(a);

      }

      void FixRayObjects() {
         List<RayTracingObject> RayObjs = new List<RayTracingObject>();
         foreach (var go in Resources.FindObjectsOfTypeAll<RayTracingObject>()) {
            RayObjs.Add(go);
         }
         int RayObjCount = RayObjs.Count;
         for(int i = 0; i < RayObjCount; i++) {
            int MatCount = RayObjs[i].LocalMaterials.Length;
            for(int i2 = 0; i2 < MatCount; i2++) {
               if(RayObjs[i].LocalMaterials[i2].MatType == 1) {
                  RayObjs[i].LocalMaterials[i2].MatType = 0;
                  if(Application.isPlaying)
                     RayObjs[i].CallMaterialEdited(true);
               }
               else if(RayObjs[i].LocalMaterials[i2].MatType != 0) 
                  RayObjs[i].LocalMaterials[i2].MatType = 0;
            }
         }

      }

      Toggle BindlessToggle;
      Toggle HardwareRTToggle;
      Toggle GaussianTreeToggle;
      Toggle OIDNToggle;
      Toggle MaterialHelperToggle;
      Toggle DX11Toggle;
      Toggle TriangleSplittingToggle;
      Toggle PhotonMappingToggle;

      private Toggle CustomToggle(string Label, string TargetDefine, string tooltip = "", VisualElement ToggleableContainer = null, VisualElement ParentContainer = null) {
            Toggle CustToggle = new Toggle() {value = GetGlobalDefine(TargetDefine), text = Label};
               CustToggle.tooltip = tooltip;
            if(ToggleableContainer != null) {
               CustToggle.RegisterValueChangedCallback(evt => {SetGlobalDefines(TargetDefine, evt.newValue); if(evt.newValue) {ParentContainer.Insert(ParentContainer.IndexOf(CustToggle) + 1, ToggleableContainer);} else ParentContainer.Remove(ToggleableContainer);});
               // GIToggle.RegisterValueChangedCallback(evt => {ReSTIRGI = evt.newValue; RayMaster.LocalTTSettings.UseReSTIRGI = ReSTIRGI;if(evt.newValue) MainSource.Insert(MainSource.IndexOf(GIToggle) + 1, GIFoldout); else MainSource.Remove(GIFoldout);});
               if(CustToggle.value) ParentContainer.Add(ToggleableContainer);
            } else {
               CustToggle.RegisterValueChangedCallback(evt => {SetGlobalDefines(TargetDefine, evt.newValue);});
            }
         return CustToggle;
      }

      void EnsureInitializedGlobalDefines() {
         definesList = GetDefines();
         if(SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D12) {
            if(!definesList.Contains("DX11Only")) {
               ActiveDX11Overrides(); 
               definesList = GetDefines();
            }
         } else if(definesList.Contains("DX11Only")) {
            RemoveDefine("DX11Only"); 
            RemoveDefine("UseAtlas"); 

         }



         SetGlobalDefines("PhotonMapping", definesList.Contains("EnablePhotonMapping"));
         SetGlobalDefines("HardwareRT", definesList.Contains("HardwareRT"));
         // SetGlobalDefines("TTCustomMotionVectors", !definesList.Contains("TTDisableCustomMotionVectors"));
         SetGlobalDefines("UseSGTree", !(definesList.Contains("DontUseSGTree")));
         SetGlobalDefines("MultiMapScreenshot", definesList.Contains("MultiMapScreenshot"));
         if(definesList.Contains("DisableRadianceCache")) SetGlobalDefines("RadCache", false);
         SetGlobalDefines("DX11", definesList.Contains("DX11Only"));
         SetGlobalDefines("RasterizedDirect", definesList.Contains("RasterizedDirect"));
      }


      void ActiveDX11Overrides() {
         // BindlessToggle.value = true; 
         RemoveDefine("UseOIDN"); 
         RemoveDefine("EnablePhotonMapping"); 
         AddDefine("UseAtlas"); 
         AddDefine("DX11Only"); 
         SetGlobalDefines("PhotonMapping", false); 
         SetGlobalDefines("DX11", true); 
         RemoveDefine("HardwareRT"); 
         SetGlobalDefines("HardwareRT", false);
      }

      void AddHardSettingsToMenu() {
         Button RemoveTrueTraceButton = new Button(() => RemoveTrueTrace()) {text = "Remove TrueTrace Scripts From Scene"};
         
         Label NonPlayLabel = new Label("-- THESE CANT BE MODIFIED ON THE FLY/DURING PLAY --");
         
         VisualElement NonPlayContainer = new VisualElement();
         NonPlayContainer.style.paddingLeft = 10;
         EnsureInitializedGlobalDefines();

            HardwareRTToggle = new Toggle() {value = (definesList.Contains("HardwareRT")), text = "Enable RT Cores (Requires Unity 2023+)"};
            HardwareRTToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) {RemoveDefine("TTTriSplitting"); TriangleSplittingToggle.SetEnabled(false); AddDefine("HardwareRT"); SetGlobalDefines("HardwareRT", true);} else {TriangleSplittingToggle.SetEnabled(true); RemoveDefine("HardwareRT"); SetGlobalDefines("HardwareRT", false);}});



            GaussianTreeToggle = new Toggle() {value = (definesList.Contains("DontUseSGTree")), text = "Use Old Light BVH instead of Gaussian Tree"};
               GaussianTreeToggle.tooltip = "Gaussian tree is more expensive, but samples on metallic surfaces a LOT better";
            GaussianTreeToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) {AddDefine("DontUseSGTree"); SetGlobalDefines("UseSGTree", false);} else {RemoveDefine("DontUseSGTree"); SetGlobalDefines("UseSGTree", true);}});


            // BindlessToggle = new Toggle() {value = (definesList.Contains("UseAtlas")), text = "Disable Bindless Textures"};
            //    BindlessToggle.tooltip = "Uses Atlas fallback, which increases VRAM/RAM use, and scales down texture resolution when needed";
            // BindlessToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) {AddDefine("UseAtlas"); SetGlobalDefines("UseBindless", false);} else {RemoveDefine("UseAtlas"); SetGlobalDefines("UseBindless", true);}});

            // Toggle CustomMotionVectorToggle = new Toggle() {value = (!definesList.Contains("TTDisableCustomMotionVectors")), text = "Enable TrueTrace Motion Vectors"};
            //    CustomMotionVectorToggle.tooltip = "Removes the need for rasterized rendering(except when upscaling with TAAU), allowing you to turn it off in your camera for extra performance";
            // CustomMotionVectorToggle.RegisterValueChangedCallback(evt => {if(!evt.newValue) {AddDefine("TTDisableCustomMotionVectors"); SetGlobalDefines("TTCustomMotionVectors", false);} else {RemoveDefine("TTDisableCustomMotionVectors"); SetGlobalDefines("TTCustomMotionVectors", true);}});

            // Toggle ReflectionMotionVectorToggle = new Toggle() {value = (definesList.Contains("TTReflectionMotionVectors")), text = "Accurate Mirror Motion Vectors(Experiemental)"};
            //    ReflectionMotionVectorToggle.tooltip = "A better way to calculate motion vectors for reflections in mirrors and such for ASVGF, requires \"RemoveRasterizationRequirement\"";
            // ReflectionMotionVectorToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) {AddDefine("TTReflectionMotionVectors"); SetGlobalDefines("TTReflectionMotionVectors", true);} else {RemoveDefine("TTReflectionMotionVectors"); SetGlobalDefines("TTReflectionMotionVectors", false);}});

            Toggle RasterizedDirectToggle = new Toggle() {value = (definesList.Contains("RasterizedDirect")), text = "Use Rasterized Lighting for Direct"};
               RasterizedDirectToggle.tooltip = "Removes the need for rasterized rendering(except when upscaling with TAAU), allowing you to turn it off in your camera for extra performance";
            RasterizedDirectToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) {AddDefine("RasterizedDirect"); SetGlobalDefines("RasterizedDirect", true);} else {RemoveDefine("RasterizedDirect"); SetGlobalDefines("RasterizedDirect", false);}});

            Toggle NonAccurateLightTriToggle = new Toggle() {value = (definesList.Contains("AccurateLightTris")), text = "Enable Emissive Texture Aware Light BVH"};
               NonAccurateLightTriToggle.tooltip = "Uses more ram(rarely it can use a LOT), but allows for much better emissive mesh sampling if you make heavy use of emission masks or textures";
            NonAccurateLightTriToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) AddDefine("AccurateLightTris"); else RemoveDefine("AccurateLightTris");});

            Toggle LoadTTSettingsFromResourcesToggle = new Toggle() {value = (definesList.Contains("LoadTTSettingsFromResources")), text = "Load TTSettings from Global File"};
               LoadTTSettingsFromResourcesToggle.tooltip = "Replaces the per-scene TTSettings file with the one that is declared in the RayTracingMaster's inspector window";
            LoadTTSettingsFromResourcesToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) AddDefine("LoadTTSettingsFromResources"); else RemoveDefine("LoadTTSettingsFromResources");});

            TriangleSplittingToggle = new Toggle() {value = (definesList.Contains("TTTriSplitting")), text = "Enable Triangle Pre-Splitting(Leave this ON when Hardware RT is OFF)"};
               TriangleSplittingToggle.tooltip = "Enables Triangle Splitting for SWRT";
            TriangleSplittingToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) AddDefine("TTTriSplitting"); else RemoveDefine("TTTriSplitting");});

            Toggle VerboseToggle = new Toggle() {value = (definesList.Contains("TTVerbose")), text = "Enable Verbose Logging"};
               VerboseToggle.tooltip = "More data";
            VerboseToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) AddDefine("TTVerbose"); else RemoveDefine("TTVerbose");});

            Toggle ExtraVerboseToggle = new Toggle() {value = (definesList.Contains("TTExtraVerbose")), text = "Enable Extra Verbose Logging"};
               ExtraVerboseToggle.tooltip = "Even MORE data";
            ExtraVerboseToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) AddDefine("TTExtraVerbose"); else RemoveDefine("TTExtraVerbose");});

            Toggle MultiMapScreenshotToggle = new Toggle() {value = (definesList.Contains("MultiMapScreenshot")), text = "Save Multiple Maps on Screenshot"};
               MultiMapScreenshotToggle.tooltip = "Save Mat ID and Mesh ID as seperate images when taking a screenshot";
            MultiMapScreenshotToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) {AddDefine("MultiMapScreenshot"); SetGlobalDefines("MultiMapScreenshot", true);} else {RemoveDefine("MultiMapScreenshot"); SetGlobalDefines("MultiMapScreenshot", false);}});

            Toggle TTAdvancedSettingsToggle = new Toggle() {value = (definesList.Contains("TTAdvancedSettings")), text = "Display Advanced Settings"};
               TTAdvancedSettingsToggle.tooltip = "Enables more advanced settings to be displayed";
            TTAdvancedSettingsToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) {AddDefine("TTAdvancedSettings");} else {RemoveDefine("TTAdvancedSettings");}});

            PhotonMappingToggle = new Toggle() {value = (definesList.Contains("EnablePhotonMapping")), text = "Enable Photon Mapped Caustics"};
               PhotonMappingToggle.tooltip = "Enable Photon Mapping for Caustics";
            PhotonMappingToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) {AddDefine("EnablePhotonMapping"); SetGlobalDefines("PhotonMapping", true);} else {RemoveDefine("EnablePhotonMapping"); SetGlobalDefines("PhotonMapping", false);}});


            Toggle TTIncrementRenderCounterToggle = new Toggle() {value = (definesList.Contains("TTIncrementRenderCounter")), text = "Add Render Counter To File"};
            TTIncrementRenderCounterToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) {AddDefine("TTIncrementRenderCounter");} else {RemoveDefine("TTIncrementRenderCounter");}});


            Toggle RemoveScriptsDuringSaveToggle = new Toggle() {value = (definesList.Contains("RemoveScriptsDuringSave")), text = "Remove TT Scripts During Save"};
               RemoveScriptsDuringSaveToggle.tooltip = "Removes all ParentObject and unmodified RayTracingObject scripts during scene save, and adds them back after(Helps with version control)";
            RemoveScriptsDuringSaveToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) {AddDefine("RemoveScriptsDuringSave");} else {RemoveDefine("RemoveScriptsDuringSave");}});


            VisualElement ClayColorBox = new VisualElement();





            Toggle ClayModeToggle = new Toggle() {value = ClayMode, text = "Use ClayMode"};
               ClayModeToggle.tooltip = "Disables normal mapping, and forces a constant albedo color, good for seeing light propogation";
            ClayModeToggle.RegisterValueChangedCallback(evt => {ClayMode = evt.newValue; RayMaster.LocalTTSettings.ClayMode = ClayMode; if(evt.newValue) HardSettingsMenu.Insert(HardSettingsMenu.IndexOf(ClayModeToggle) + 1, ClayColorBox); else HardSettingsMenu.Remove(ClayColorBox);});

            ColorField ClayColorField = new ColorField();
            ClayColorField.label = "Clay Color: ";
            ClayColorField.value = new Color(ClayColor.x, ClayColor.y, ClayColor.z, 1.0f);
            ClayColorField.style.width = 250;
            ClayColorField.RegisterValueChangedCallback(evt => {ClayColor = new Vector3(evt.newValue.r, evt.newValue.g, evt.newValue.b); RayMaster.LocalTTSettings.ClayColor = ClayColor;});
            ClayColorBox.Add(ClayColorField);
            ClayColorBox.Add(CustomToggle("Clay Metal Override", "ClayMetalOverride", "Allows you to override metallic/roughness in in clay mode(blame Yanus)"));
            
            FloatSliderPair ClayMetallicContainer = CreatePairedFloatSlider("Metallilc Override", 0, 1, ref ClayMetalOverride);
            ClayMetallicContainer.DynamicSlider.RegisterValueChangedCallback(evt => {ClayMetalOverride = evt.newValue; ClayMetallicContainer.DynamicField.value = ClayMetalOverride; RayMaster.LocalTTSettings.ClayMetalOverride = ClayMetalOverride;});
            ClayMetallicContainer.DynamicField.RegisterValueChangedCallback(evt => {ClayMetalOverride = evt.newValue; ClayMetallicContainer.DynamicSlider.value = ClayMetalOverride; RayMaster.LocalTTSettings.ClayMetalOverride = ClayMetalOverride;});
            ClayColorBox.Add(ClayMetallicContainer.DynamicContainer);

            FloatSliderPair ClayRoughnessContainer = CreatePairedFloatSlider("Roughness Override", 0, 1, ref ClayRoughnessOverride);
            ClayRoughnessContainer.DynamicSlider.RegisterValueChangedCallback(evt => {ClayRoughnessOverride = evt.newValue; ClayRoughnessContainer.DynamicField.value = ClayRoughnessOverride; RayMaster.LocalTTSettings.ClayRoughnessOverride = ClayRoughnessOverride;});
            ClayRoughnessContainer.DynamicField.RegisterValueChangedCallback(evt => {ClayRoughnessOverride = evt.newValue; ClayRoughnessContainer.DynamicSlider.value = ClayRoughnessOverride; RayMaster.LocalTTSettings.ClayRoughnessOverride = ClayRoughnessOverride;});
            ClayColorBox.Add(ClayRoughnessContainer.DynamicContainer);



            IntegerField MaxSampField = new IntegerField() {value = MaxSampCount, label = "Maximum Sample Count"};
               MaxSampField.tooltip = "Truetrace will render up to this sample count, then idle";
            MaxSampField.style.width = 300;
            MaxSampField.RegisterValueChangedCallback(evt => {MaxSampCount = evt.newValue; MaxSampCount = Mathf.Min(Mathf.Max(MaxSampCount, 0), 99999999); MaxSampField.value = MaxSampCount; RayMaster.LocalTTSettings.MaxSampCount = MaxSampCount;});


            OIDNToggle = new Toggle() {value = (definesList.Contains("UseOIDN")), text = "Enable OIDN(DX12 ONLY)"};
               OIDNToggle.tooltip = "Allows access to the OIDN denoiser in the main menus \"Denoiser\" field";
            OIDNToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) AddDefine("UseOIDN"); else RemoveDefine("UseOIDN");});


            Toggle RadCacheToggle = new Toggle() {value = (definesList.Contains("DisableRadianceCache")), text = "FULLY Disable Radiance Cache"};
               RadCacheToggle.tooltip = "Prevents use of the radcache entirely while this is enabled, as it frees up all resources/ram/vram the radiance cache uses";
            RadCacheToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) {SetGlobalDefines("RadCache", false); AddDefine("DisableRadianceCache");} else {SetGlobalDefines("RadCache", true); RemoveDefine("DisableRadianceCache");}});

            Toggle StrictMemoryReductionToggle = new Toggle() {value = (definesList.Contains("StrictMemoryReduction")), text = "Enable Strict Memory Reduction(read tooltip)"};
               StrictMemoryReductionToggle.tooltip = "Automatically shrinks buffer sizes for storing meshes when able to(takes more performance)";
            StrictMemoryReductionToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) AddDefine("StrictMemoryReduction"); else RemoveDefine("StrictMemoryReduction");});



            // DX11Toggle = new Toggle() {value = (definesList.Contains("DX11Only")), text = "Use DX11"};
            if(Application.isPlaying) {
               PhotonMappingToggle.SetEnabled(false);
               HardwareRTToggle.SetEnabled(false);
               // CustomMotionVectorToggle.SetEnabled(false);
               // ReflectionMotionVectorToggle.SetEnabled(false);
               RasterizedDirectToggle.SetEnabled(false);
               // BindlessToggle.SetEnabled(false);
               GaussianTreeToggle.SetEnabled(false);
               OIDNToggle.SetEnabled(false);
               RadCacheToggle.SetEnabled(false);
               NonAccurateLightTriToggle.SetEnabled(false);
               LoadTTSettingsFromResourcesToggle.SetEnabled(false);
               VerboseToggle.SetEnabled(false);
               ExtraVerboseToggle.SetEnabled(false);
               TriangleSplittingToggle.SetEnabled(false);
               StrictMemoryReductionToggle.SetEnabled(false);
               MultiMapScreenshotToggle.SetEnabled(false);
               TTAdvancedSettingsToggle.SetEnabled(false);
               RemoveScriptsDuringSaveToggle.SetEnabled(false);
               TTIncrementRenderCounterToggle.SetEnabled(false);
               // DX11Toggle.SetEnabled(false);
            } else {
               PhotonMappingToggle.SetEnabled(true);
               HardwareRTToggle.SetEnabled(true);
               // CustomMotionVectorToggle.SetEnabled(true);
               // ReflectionMotionVectorToggle.SetEnabled(true);
               RasterizedDirectToggle.SetEnabled(true);
               // BindlessToggle.SetEnabled(true);
               GaussianTreeToggle.SetEnabled(true);
               OIDNToggle.SetEnabled(true);
               RadCacheToggle.SetEnabled(true);
               NonAccurateLightTriToggle.SetEnabled(true);
               LoadTTSettingsFromResourcesToggle.SetEnabled(true);
               VerboseToggle.SetEnabled(true);
               ExtraVerboseToggle.SetEnabled(true);
               TriangleSplittingToggle.SetEnabled(true);
               StrictMemoryReductionToggle.SetEnabled(true);
               MultiMapScreenshotToggle.SetEnabled(true);
               TTAdvancedSettingsToggle.SetEnabled(true);
               RemoveScriptsDuringSaveToggle.SetEnabled(true);
               TTIncrementRenderCounterToggle.SetEnabled(true);
               // DX11Toggle.SetEnabled(true);
            }

            if(definesList.Contains("HardwareRT")) {
               if(definesList.Contains("TTTriSplitting")) {
                  RemoveDefine("TTTriSplitting");
               }
               TriangleSplittingToggle.SetEnabled(false);
            } else if(!Application.isPlaying) {
               TriangleSplittingToggle.SetEnabled(true);
            }
            if(SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D12) {
               if(!definesList.Contains("DX11Only")) {
                  ActiveDX11Overrides(); 
               }
               // BindlessToggle.SetEnabled(false);
               HardwareRTToggle.SetEnabled(false);
               PhotonMappingToggle.SetEnabled(false);
               OIDNToggle.SetEnabled(false);
            }


            // DX11Toggle.RegisterValueChangedCallback(evt => {
            //    if(evt.newValue) {
            //       ActiveDX11Overrides(); 
            //       // BindlessToggle.SetEnabled(false);
            //       HardwareRTToggle.SetEnabled(false);
            //       PhotonMappingToggle.SetEnabled(false);
            //       OIDNToggle.SetEnabled(false);
            //    } else {
            //       if(SystemInfo.graphicsDeviceType != GraphicsDeviceType.Direct3D12) {
            //          Debug.LogError("DX12 Not Found, Forcing DX11"); 
            //          DX11Toggle.value = true;
            //       } else {
            //          OIDNToggle.SetEnabled(true);
            //          HardwareRTToggle.SetEnabled(true);
            //          PhotonMappingToggle.SetEnabled(true);
            //          // BindlessToggle.SetEnabled(true);
            //          RemoveDefine("DX11Only"); 
            //          SetGlobalDefines("DX11", false); 
            //       } 
            //    }
            // });

         NonPlayContainer.Add(HardwareRTToggle);
         // NonPlayContainer.Add(BindlessToggle);
         #if TTAdvancedSettings
            NonPlayContainer.Add(GaussianTreeToggle);
         #endif
         // NonPlayContainer.Add(DX11Toggle);
         NonPlayContainer.Add(OIDNToggle);
         #if TTAdvancedSettings
            NonPlayContainer.Add(RadCacheToggle);
         #endif
         // NonPlayContainer.Add(CustomMotionVectorToggle);
         // NonPlayContainer.Add(ReflectionMotionVectorToggle);
         #if TTAdvancedSettings
            NonPlayContainer.Add(RasterizedDirectToggle);
         #endif
         NonPlayContainer.Add(NonAccurateLightTriToggle);
         #if TTAdvancedSettings
            NonPlayContainer.Add(LoadTTSettingsFromResourcesToggle);
            NonPlayContainer.Add(VerboseToggle);
            #if TTVerbose
               NonPlayContainer.Add(ExtraVerboseToggle);
            #endif
         #endif
         NonPlayContainer.Add(TriangleSplittingToggle);
         #if TTAdvancedSettings
            NonPlayContainer.Add(StrictMemoryReductionToggle);
            NonPlayContainer.Add(MultiMapScreenshotToggle);
         #endif
         NonPlayContainer.Add(PhotonMappingToggle);
         #if TTAdvancedSettings
            NonPlayContainer.Add(RemoveScriptsDuringSaveToggle);
            NonPlayContainer.Add(TTIncrementRenderCounterToggle);
         #endif
         NonPlayContainer.Add(TTAdvancedSettingsToggle);
         NonPlayContainer.Add(new Label("-------------"));

         Label PlayLabel = new Label("-- THESE CAN BE MODIFIED ON THE FLY/DURING PLAY --");
         
         VisualElement PlayContainer = new VisualElement();
         PlayContainer.style.paddingLeft = 10;

         VisualElement AOContainer = new VisualElement();
            AOContainer.style.paddingLeft = 20;
            FloatField AORadiusField = new FloatField() {label = "AO Radius", value = aoRadius};
               AORadiusField.RegisterValueChangedCallback(evt => {aoRadius = evt.newValue; RayMaster.LocalTTSettings.aoRadius = aoRadius;});
               AORadiusField.ElementAt(0).style.minWidth = 75;
               AORadiusField.style.width = 120;
            FloatField AOStrengthField = new FloatField() {label = "AO Strength", value = aoStrength};
               AOStrengthField.RegisterValueChangedCallback(evt => {aoStrength = evt.newValue; RayMaster.LocalTTSettings.aoStrength = aoStrength;});
               AOStrengthField.ElementAt(0).style.minWidth = 75;
               AOStrengthField.style.width = 120;
         AOContainer.Add(AORadiusField);
         AOContainer.Add(AOStrengthField);

         PlayContainer.Add(CustomToggle("Fade Mapping", "FadeMapping", "Allows for fade mapping"));
         PlayContainer.Add(CustomToggle("Stained Glass", "StainedGlassShadows", "Simulates colored glass coloring shadow rays - Stained glass effect"));
         PlayContainer.Add(CustomToggle("Ignore Backfacing Triangles", "IgnoreBackfacing", "Backfacing triangles wont get rendered"));
         #if TTAdvancedSettings
            PlayContainer.Add(CustomToggle("Use Light BVH", "LBVH", "Quick toggle to switch between the active light tree(Gaussian tree or light bvh), and simple RIS, like the default unity lights use"));
         #endif
         PlayContainer.Add(CustomToggle("Quick RadCache Toggle", "RadCache", "Quick toggle for the radiance cache, does NOT affect memory used by the radiance cache, unlike the toggle above"));
         #if TTAdvancedSettings
            PlayContainer.Add(CustomToggle("Use Texture LOD", "UseTextureLOD", "DX12 Only - Uses a higher texture LOD for each bounce, which can help performance"));
            PlayContainer.Add(CustomToggle("Double Buffer Light Tree", "DoubleBufferSGTree", "Enables double buffering of the light tree, allowing for stable moving emissive objects with ASVGF, but hurts performance"));
            PlayContainer.Add(CustomToggle("Use BSDF Lights", "UseBRDFLights", "Toggle for BSDF lights, Turning off can help with fireflies"));
            PlayContainer.Add(CustomToggle("Use Advanced Background", "AdvancedBackground"));
         #endif
         PlayContainer.Add(CustomToggle("More AO", "MoreAO", "If you want yet more AO", AOContainer, PlayContainer));


         VisualElement FogContainer = new VisualElement();
            Slider FogSlider = new Slider() {label = "Fog Density: ", value = FogDensity, highValue = 0.2f, lowValue = 0.000000001f};
               FogSlider.value = FogDensity;
               FogSlider.showInputField = true;        
               FogSlider.style.width = 400;
               FogSlider.ElementAt(0).style.minWidth = 65;
               FogSlider.RegisterValueChangedCallback(evt => {FogDensity = FogSlider.value; RayMaster.LocalTTSettings.FogDensity = FogDensity;});        

            Slider FogHeightSlider = new Slider() {label = "Fog Height: ", value = FogHeight, highValue = 80.0f, lowValue = 0.00001f};
               FogHeightSlider.value = FogHeight;
               FogHeightSlider.showInputField = true;        
               FogHeightSlider.style.width = 400;
               FogHeightSlider.ElementAt(0).style.minWidth = 65;
               FogHeightSlider.value = FogHeight;
               FogHeightSlider.RegisterValueChangedCallback(evt => {FogHeight = FogHeightSlider.value; RayMaster.LocalTTSettings.FogHeight = FogHeight;});        
            
            ColorField FogColorField = new ColorField();
               FogColorField.value = FogColor;
               FogColorField.style.width = 150;
               FogColorField.RegisterValueChangedCallback(evt => {FogColor = evt.newValue; RayMaster.LocalTTSettings.FogColor = new Vector3(FogColor.r,FogColor.g,FogColor.b);});
         FogContainer.Add(FogSlider);
         FogContainer.Add(FogHeightSlider);
         FogContainer.Add(FogColorField);
         PlayContainer.Add(CustomToggle("Multiscatter Fog", "Fog", "Not realtime, as I have no denoiser for it yet", FogContainer, PlayContainer));



         PlayContainer.Add(new Label("-------------"));




         MaterialHelperToggle = new Toggle() {value = !(definesList.Contains("HIDEMATERIALREATIONS")), text = "Show Material Helper Lines"};
         MaterialHelperToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) RemoveDefine("HIDEMATERIALREATIONS"); else AddDefine("HIDEMATERIALREATIONS");});


         Toggle DoSavingToggle = new Toggle() {value = DoSaving, text = "Enable RayTracingObject Saving"};
            DoSavingToggle.tooltip = "Allows saving any changes to your truetrace materials made during play mode";
         DoSavingToggle.RegisterValueChangedCallback(evt => {DoSaving = evt.newValue; RayTracingMaster.DoSaving = DoSaving;});
         Toggle MatChangeResetsAccumToggle = new Toggle() {value = MatChangeResetsAccum, text = "Material Change Resets Accumulation"};
         MatChangeResetsAccumToggle.RegisterValueChangedCallback(evt => {MatChangeResetsAccum = evt.newValue; RayMaster.LocalTTSettings.MatChangeResetsAccum = MatChangeResetsAccum;});
         VisualElement ScreenShotBox = new VisualElement();
         ScreenShotBox.style.flexDirection = FlexDirection.Row;
         Label PathLabel = new Label() {text = "Screenshot Path: "};
         PathLabel.style.color = Color.black;
         if(System.IO.Directory.Exists(PlayerPrefs.GetString("ScreenShotPath"))) PathLabel.style.backgroundColor = Color.green;
         else PathLabel.style.backgroundColor = Color.red;
         TextField AbsolutePath = new TextField();
         AbsolutePath.value = PlayerPrefs.GetString("ScreenShotPath");
         AbsolutePath.RegisterValueChangedCallback(evt => {if(System.IO.Directory.Exists(evt.newValue)) {PathLabel.style.backgroundColor = Color.green;} else {PathLabel.style.backgroundColor = Color.red;} PlayerPrefs.SetString("ScreenShotPath", evt.newValue);});
         ScreenShotBox.Add(PathLabel);
         ScreenShotBox.Add(AbsolutePath);


         VisualElement PanoramaBox = new VisualElement();
         PanoramaBox.style.flexDirection = FlexDirection.Row;
         Label PanoramaLabel = new Label() {text = "Panorama Path: "};
         PanoramaLabel.style.color = Color.black;
         if(System.IO.Directory.Exists(PlayerPrefs.GetString("PanoramaPath"))) PanoramaLabel.style.backgroundColor = Color.green;
         else PanoramaLabel.style.backgroundColor = Color.red;
         TextField PanoramaAbsolutePath = new TextField();
         PanoramaAbsolutePath.value = PlayerPrefs.GetString("PanoramaPath");
         PanoramaAbsolutePath.RegisterValueChangedCallback(evt => {if(System.IO.Directory.Exists(evt.newValue)) {PanoramaLabel.style.backgroundColor = Color.green;} else {PanoramaLabel.style.backgroundColor = Color.red;} PlayerPrefs.SetString("PanoramaPath", evt.newValue);});
         PanoramaBox.Add(PanoramaLabel);
         PanoramaBox.Add(PanoramaAbsolutePath);

         VisualElement TurnTableBox = new VisualElement();
         TurnTableBox.style.flexDirection = FlexDirection.Row;
         Label TurnTableLabel = new Label() {text = "Turn Table Path: "};
         TurnTableLabel.style.color = Color.black;
         if(System.IO.Directory.Exists(PlayerPrefs.GetString("TurnTablePath"))) TurnTableLabel.style.backgroundColor = Color.green;
         else TurnTableLabel.style.backgroundColor = Color.red;
         TextField TurnTableAbsolutePath = new TextField();
         TurnTableAbsolutePath.value = PlayerPrefs.GetString("TurnTablePath");
         TurnTableAbsolutePath.RegisterValueChangedCallback(evt => {if(System.IO.Directory.Exists(evt.newValue)) {TurnTableLabel.style.backgroundColor = Color.green;} else {TurnTableLabel.style.backgroundColor = Color.red;} PlayerPrefs.SetString("TurnTablePath", evt.newValue);});
         TurnTableBox.Add(TurnTableLabel);
         TurnTableBox.Add(TurnTableAbsolutePath);


         VisualElement TimelineBox = new VisualElement();
         TimelineBox.style.flexDirection = FlexDirection.Row;
         Label TimelineLabel = new Label() {text = "Timeline Path: "};
         TimelineLabel.style.color = Color.black;
         if(System.IO.Directory.Exists(PlayerPrefs.GetString("TimelinePath"))) TimelineLabel.style.backgroundColor = Color.green;
         else TimelineLabel.style.backgroundColor = Color.red;
         TextField TimelineAbsolutePath = new TextField();
         TimelineAbsolutePath.value = PlayerPrefs.GetString("TimelinePath");
         TimelineAbsolutePath.RegisterValueChangedCallback(evt => {if(System.IO.Directory.Exists(evt.newValue)) {TimelineLabel.style.backgroundColor = Color.green;} else {TimelineLabel.style.backgroundColor = Color.red;} PlayerPrefs.SetString("TimelinePath", evt.newValue);});
         TimelineBox.Add(TimelineLabel);
         TimelineBox.Add(TimelineAbsolutePath);


         VisualElement CounterBox = new VisualElement();
         CounterBox.style.flexDirection = FlexDirection.Row;
         Label CounterLabel = new Label() {text = "Counter Path: "};
         CounterLabel.style.color = Color.black;
         if(System.IO.Directory.Exists(PlayerPrefs.GetString("CounterPath"))) CounterLabel.style.backgroundColor = Color.green;
         else CounterLabel.style.backgroundColor = Color.red;
         TextField CounterAbsolutePath = new TextField();
         CounterAbsolutePath.value = PlayerPrefs.GetString("CounterPath");
         CounterAbsolutePath.RegisterValueChangedCallback(evt => {if(System.IO.Directory.Exists(evt.newValue)) {CounterLabel.style.backgroundColor = Color.green;} else {CounterLabel.style.backgroundColor = Color.red;} PlayerPrefs.SetString("CounterPath", evt.newValue);});
         CounterBox.Add(CounterLabel);
         CounterBox.Add(CounterAbsolutePath);


         Button CorrectMatOptionsButton = new Button(() => FixRayObjects()) {text = "(Debug Button)Correct Mat Options"};


            // List<string> DebugSettings = new List<string>();
            // DebugSettings.Add("None");
            // DebugSettings.Add("Material ID");
            // DebugSettings.Add("Mesh ID");
            // DebugSettings.Add("Triangle ID");
            // DebugSettings.Add("Albedo ID");
            // DebugSettings.Add("BVH View");
            // DebugSettings.Add("Radiance Cache");
            // DebugSettings.Add("GI View");
            // DebugSettings.Add("Depth View");
            // PopupField<string> DebugSettingsField = new PopupField<string>("<b>Debug Views</b>");
            // DebugSettingsField.ElementAt(0).style.minWidth = 65;
            // DebugSettingsField.choices = DebugSettings;
            // DebugSettingsField.index = ToneMapIndex;
            // DebugSettingsField.RegisterValueChangedCallback(evt => {ToneMapIndex = ToneMapField.index; RayMaster.LocalTTSettings.ToneMapper = ToneMapIndex;});




         HardSettingsMenu.Add(NonPlayLabel);
         HardSettingsMenu.Add(NonPlayContainer);
         HardSettingsMenu.Add(PlayLabel);
         HardSettingsMenu.Add(PlayContainer);
         HardSettingsMenu.Add(ClayModeToggle);
         #if TTAdvancedSettings
            HardSettingsMenu.Add(MaterialHelperToggle);
            HardSettingsMenu.Add(MatChangeResetsAccumToggle);
         #endif
         if(ClayMode) HardSettingsMenu.Add(ClayColorBox);
         VisualElement Spacer = new VisualElement();
         Spacer.style.height = 10;
         HardSettingsMenu.Add(Spacer);
         HardSettingsMenu.Add(DoSavingToggle);
         HardSettingsMenu.Add(MaxSampField);
         HardSettingsMenu.Add(ScreenShotBox);
         HardSettingsMenu.Add(PanoramaBox);
         HardSettingsMenu.Add(TurnTableBox);
         HardSettingsMenu.Add(TimelineBox);
         HardSettingsMenu.Add(CounterBox);
         // HardSettingsMenu.Add(CorrectMatOptionsButton);
         HardSettingsMenu.Add(RemoveTrueTraceButton);
         






         // HardSettingsMenu.Add(CorrectMatOptionsButton);
      }
      public class FloatSliderPair {
         public VisualElement DynamicContainer;
         public Label DynamicLabel;
         public Slider DynamicSlider;
         public FloatField DynamicField;
      }
      FloatSliderPair CreatePairedFloatSlider(string Name, float LowValue, float HighValue, ref float InitialValue, float SliderWidth = 100) {
         FloatSliderPair NewPair = new FloatSliderPair();
         NewPair.DynamicContainer = CreateHorizontalBox(Name + " Container");
            NewPair.DynamicLabel = new Label(Name);
            NewPair.DynamicSlider = new Slider() {value = InitialValue, highValue = HighValue, lowValue = LowValue};
            NewPair.DynamicSlider.value = InitialValue;
            NewPair.DynamicField = new FloatField() {value = InitialValue};
            NewPair.DynamicSlider.style.width = SliderWidth;
         NewPair.DynamicContainer.Add(NewPair.DynamicLabel);
         NewPair.DynamicContainer.Add(NewPair.DynamicSlider);
         NewPair.DynamicContainer.Add(NewPair.DynamicField);
         return NewPair;
      }

      void AddPostProcessingToMenu() {

         List<string> TonemapSettings = new List<string>();
         TonemapSettings.Add("TonyMcToneFace");
         TonemapSettings.Add("ACES Filmic");
         TonemapSettings.Add("Uchimura");
         TonemapSettings.Add("Reinhard");
         TonemapSettings.Add("Uncharted 2");
         TonemapSettings.Add("AgX BC");
         TonemapSettings.Add("AgX MHC");
         TonemapSettings.Add("AgX Custom");
         TonemapSettings.Add("PBR Neutral");
         PopupField<string> ToneMapField = new PopupField<string>("<b>Tonemapper</b>");
         ToneMapField.ElementAt(0).style.minWidth = 65;
         ToneMapField.choices = TonemapSettings;
         ToneMapField.index = ToneMapIndex;
         ToneMapField.RegisterValueChangedCallback(evt => {ToneMapIndex = ToneMapField.index; RayMaster.LocalTTSettings.ToneMapper = ToneMapIndex;});

         Toggle ToneMapToggle = new Toggle() {value = ToneMap, text = "Enable Tonemapping"};
         VisualElement ToneMapFoldout = new VisualElement() {};
            ToneMapFoldout.style.flexDirection = FlexDirection.Row;
            ToneMapFoldout.Add(ToneMapField);
         PostProcessingMenu.Add(ToneMapToggle);
         ToneMapToggle.RegisterValueChangedCallback(evt => {ToneMap = evt.newValue; RayMaster.LocalTTSettings.PPToneMap = ToneMap; if(evt.newValue) PostProcessingMenu.Insert(PostProcessingMenu.IndexOf(ToneMapToggle) + 1, ToneMapFoldout); else PostProcessingMenu.Remove(ToneMapFoldout);});
         if(ToneMap) PostProcessingMenu.Add(ToneMapFoldout);

         VisualElement SharpenFoldout = new VisualElement() {};
            Slider SharpnessSlider = new Slider() {label = "Sharpness: ", value = Sharpness, highValue = 1.0f, lowValue = 0.0f};
            SharpnessSlider.value = Sharpness;
            SharpnessSlider.style.width = 200;
            SharpnessSlider.RegisterValueChangedCallback(evt => {Sharpness = evt.newValue; RayMaster.LocalTTSettings.Sharpness = Sharpness;});
            SharpenFoldout.Add(SharpnessSlider);
         SharpnessSlider.ElementAt(0).style.minWidth = 65;


         Toggle SharpenToggle = new Toggle() {value = DoSharpen, text = "Use Sharpness Filter"};
         PostProcessingMenu.Add(SharpenToggle);
         SharpenToggle.RegisterValueChangedCallback(evt => {DoSharpen = evt.newValue; RayMaster.LocalTTSettings.DoSharpen = DoSharpen; if(evt.newValue) PostProcessingMenu.Insert(PostProcessingMenu.IndexOf(SharpenToggle) + 1, SharpenFoldout); else PostProcessingMenu.Remove(SharpenFoldout);});
         if(DoSharpen) PostProcessingMenu.Add(SharpenFoldout);



         BloomToggle = new Toggle() {value = Bloom, text = "Enable Bloom"};
            VisualElement BloomBox = new VisualElement();
               Toggle ConvBloomToggle = new Toggle {value = ConvBloom, text = "Convolutional Bloom"};
                  VisualElement StandardBloomBox = new VisualElement();
                     StandardBloomBox.style.flexDirection = FlexDirection.Row;
                     Label BloomLabel = new Label("Bloom Strength");
                     Slider BloomSlider = new Slider() {value = BloomStrength, highValue = 0.9999f, lowValue = 0.25f};
                        BloomSlider.value = BloomStrength;
                        BloomSlider.style.width = 100;
                        BloomSlider.RegisterValueChangedCallback(evt => {BloomStrength = evt.newValue; RayMaster.LocalTTSettings.BloomStrength = BloomStrength;});
                  StandardBloomBox.Add(BloomLabel);
                  StandardBloomBox.Add(BloomSlider);
                  VisualElement ConvBloomBox = new VisualElement();
                     Slider ConvBloomStrengthSlider = new Slider() {label = "Convolution Bloom Strength", value = ConvStrength, highValue = 10.0f, lowValue = 0.0f};
                        ConvBloomStrengthSlider.value = ConvStrength;
                        ConvBloomStrengthSlider.style.width = 400;
                        ConvBloomStrengthSlider.RegisterValueChangedCallback(evt => {ConvStrength = evt.newValue; RayMaster.LocalTTSettings.ConvStrength = ConvStrength;});
                     Slider ConvBloomThresholdSlider = new Slider() {label = "Convolution Bloom Threshold", value = ConvBloomThreshold, highValue = 20.0f, lowValue = 0.0f};
                        ConvBloomThresholdSlider.value = ConvBloomThreshold;
                        ConvBloomThresholdSlider.style.width = 400;
                        ConvBloomThresholdSlider.RegisterValueChangedCallback(evt => {ConvBloomThreshold = evt.newValue; RayMaster.LocalTTSettings.ConvBloomThreshold = ConvBloomThreshold;});
                     Vector2Field ConvBloomSizeField = new Vector2Field() {label = "Convolution Bloom Size", value = ConvBloomSize};
                        ConvBloomSizeField.RegisterValueChangedCallback(evt => {ConvBloomSize = evt.newValue; RayMaster.LocalTTSettings.ConvBloomSize = ConvBloomSize;});
                     VisualElement ConvDistExpBox = CreateHorizontalBox("Convolution Distance Container");
                        FloatField ConvBloomDistExpField = new FloatField() {label = "Convolution Bloom Dist Exp", value = ConvBloomDistExp};
                           ConvBloomDistExpField.RegisterValueChangedCallback(evt => {ConvBloomDistExp = evt.newValue; RayMaster.LocalTTSettings.ConvBloomDistExp = ConvBloomDistExp;});
                        FloatField ConvBloomDistExpClampMinField = new FloatField() {label = "Convolution Bloom Dist Exp Clamp Min", value = ConvBloomDistExpClampMin};
                           ConvBloomDistExpClampMinField.RegisterValueChangedCallback(evt => {ConvBloomDistExpClampMin = evt.newValue; RayMaster.LocalTTSettings.ConvBloomDistExpClampMin = ConvBloomDistExpClampMin;});
                        FloatField ConvBloomDistExpScaleField = new FloatField() {label = "Convolution Bloom Dist Exp Scale", value = ConvBloomDistExpScale};
                           ConvBloomDistExpScaleField.RegisterValueChangedCallback(evt => {ConvBloomDistExpScale = evt.newValue; RayMaster.LocalTTSettings.ConvBloomDistExpScale = ConvBloomDistExpScale;});
                     ConvDistExpBox.Add(ConvBloomDistExpField);
                     ConvDistExpBox.Add(ConvBloomDistExpClampMinField);
                     ConvDistExpBox.Add(ConvBloomDistExpScaleField);


                  ConvBloomBox.Add(ConvBloomStrengthSlider);
                  ConvBloomBox.Add(ConvBloomThresholdSlider);
                  ConvBloomBox.Add(ConvBloomSizeField);
                  ConvBloomBox.Add(ConvDistExpBox);
               BloomBox.Add(ConvBloomToggle);
               if(ConvBloom) BloomBox.Add(ConvBloomBox);
               else BloomBox.Add(StandardBloomBox);
               ConvBloomToggle.RegisterValueChangedCallback(evt => {ConvBloom = evt.newValue; RayMaster.LocalTTSettings.ConvBloom = ConvBloom; if(evt.newValue) {BloomBox.Remove(StandardBloomBox); BloomBox.Insert(BloomBox.IndexOf(ConvBloomToggle) + 1, ConvBloomBox); } else {BloomBox.Remove(ConvBloomBox); BloomBox.Insert(BloomBox.IndexOf(ConvBloomToggle) + 1, StandardBloomBox); }});        
            BloomToggle.RegisterValueChangedCallback(evt => {Bloom = evt.newValue; RayMaster.LocalTTSettings.PPBloom = Bloom; if(evt.newValue) PostProcessingMenu.Insert(PostProcessingMenu.IndexOf(BloomToggle) + 1, BloomBox); else PostProcessingMenu.Remove(BloomBox);});        
         PostProcessingMenu.Add(BloomToggle);
         if(Bloom) {
            PostProcessingMenu.Add(BloomBox);

         }



           Label AperatureLabel = new Label("Aperature Size");
           AperatureSlider = new Slider() {value = DoFAperature, highValue = 1, lowValue = 0};
           AperatureSlider.value = DoFAperature;
           AperatureSlider.style.width = 250;
           FloatField AperatureScaleField = new FloatField() {value = DoFAperatureScale, label = "Aperature Scale"};
           AperatureScaleField.ElementAt(0).style.minWidth = 65;
           AperatureScaleField.RegisterValueChangedCallback(evt => {DoFAperatureScale = evt.newValue; DoFAperatureScale = Mathf.Max(DoFAperatureScale, 0.0001f); RayMaster.LocalTTSettings.DoFAperatureScale = DoFAperatureScale; AperatureScaleField.value = DoFAperatureScale;});
           Label FocalLabel = new Label("Focal Length");
           FocalSlider = new FloatField() {value = DoFFocal};
           FocalSlider.value = DoFFocal;
           FocalSlider.style.width = 150;
           Label AutoFocLabel = new Label("Hold Middle Mouse + Left Control in game to focus");
           // Button AutofocusButton = new Button(() => {IsFocusing = true;}) {text = "Autofocus DoF"};
           // AutofocusButton.RegisterCallback<MouseEnterEvent>(evt => {});
           // AutofocusButton.RegisterCallback<MouseLeaveEvent>(evt => {});


           Box AperatureBox = new Box();
           AperatureBox.Add(AperatureLabel);
           AperatureBox.Add(AperatureSlider);
           AperatureBox.Add(AperatureScaleField);
           AperatureBox.style.flexDirection = FlexDirection.Row;
           Box FocalBox = new Box();
           FocalBox.Add(FocalLabel);
           FocalBox.Add(FocalSlider);
           FocalBox.Add(AutoFocLabel);
           // FocalBox.Add(AutofocusButton);
           FocalBox.style.flexDirection = FlexDirection.Row;

           Toggle DoFToggle = new Toggle() {value = DoF, text = "Enable DoF"};
           VisualElement DoFFoldout = new VisualElement();
           DoFFoldout.Add(AperatureBox);
           DoFFoldout.Add(FocalBox);
           PostProcessingMenu.Add(DoFToggle);
           DoFToggle.RegisterValueChangedCallback(evt => {DoF = evt.newValue; RayMaster.LocalTTSettings.PPDoF = DoF;if(evt.newValue) PostProcessingMenu.Insert(PostProcessingMenu.IndexOf(DoFToggle) + 1, DoFFoldout); else PostProcessingMenu.Remove(DoFFoldout);});        
           AperatureSlider.RegisterValueChangedCallback(evt => {DoFAperature = evt.newValue; RayMaster.LocalTTSettings.DoFAperature = DoFAperature;});
           FocalSlider.RegisterValueChangedCallback(evt => {DoFFocal = Mathf.Max(0.001f, evt.newValue); RayMaster.LocalTTSettings.DoFFocal = DoFFocal;});
           if(DoF) PostProcessingMenu.Add(DoFFoldout);
           
           Toggle DoExposureToggle = new Toggle() {value = DoExposure, text = "Enable Auto/Manual Exposure"};
           PostProcessingMenu.Add(DoExposureToggle);
           VisualElement ExposureElement = new VisualElement();
               ExposureElement.style.flexDirection = FlexDirection.Row;
               Label ExposureLabel = new Label("Exposure");
               Slider ExposureSlider = new Slider() {value = Exposure, highValue = 50.0f, lowValue = 0};
               ExposureSlider.value = Exposure;
               FloatField ExposureField = new FloatField() {value = Exposure};
               Toggle ExposureAutoToggle = new Toggle() {value = ExposureAuto, text = "Auto(On)/Manual(Off)"};
               ExposureAutoToggle.RegisterValueChangedCallback(evt => {ExposureAuto = evt.newValue; RayMaster.LocalTTSettings.ExposureAuto = ExposureAuto;});
               DoExposureToggle.tooltip = "Slide to the left for Auto";
               ExposureSlider.tooltip = "Slide to the left for Auto";
               ExposureLabel.tooltip = "Slide to the left for Auto";
               ExposureSlider.style.width = 100;
               ExposureElement.Add(ExposureLabel);
               ExposureElement.Add(ExposureSlider);
               ExposureElement.Add(ExposureField);
               ExposureElement.Add(ExposureAutoToggle);
           DoExposureToggle.RegisterValueChangedCallback(evt => {DoExposure = evt.newValue; RayMaster.LocalTTSettings.PPExposure = DoExposure;if(evt.newValue) PostProcessingMenu.Insert(PostProcessingMenu.IndexOf(DoExposureToggle) + 1, ExposureElement); else PostProcessingMenu.Remove(ExposureElement);});
           ExposureSlider.RegisterValueChangedCallback(evt => {Exposure = evt.newValue; ExposureField.value = Exposure; RayMaster.LocalTTSettings.Exposure = Exposure;});
           ExposureField.RegisterValueChangedCallback(evt => {Exposure = evt.newValue; ExposureSlider.value = Exposure; RayMaster.LocalTTSettings.Exposure = Exposure;});
            if(DoExposure) PostProcessingMenu.Add(ExposureElement);

           TAAToggle = new Toggle() {value = TAA, text = "Enable TAA"};
           PostProcessingMenu.Add(TAAToggle);
           TAAToggle.RegisterValueChangedCallback(evt => {TAA = evt.newValue; RayMaster.LocalTTSettings.PPTAA = TAA;});

           FXAAToggle = new Toggle() {value = FXAA, text = "Enable FXAA"};
           PostProcessingMenu.Add(FXAAToggle);
           FXAAToggle.RegisterValueChangedCallback(evt => {FXAA = evt.newValue; RayMaster.LocalTTSettings.PPFXAA = FXAA;});


            Toggle BCSToggle = new Toggle() {value = DoBCS, text = "Enable Saturation Adjustment"};
            VisualElement BCSContainer = new VisualElement();
               VisualElement SaturationContainer = CreateHorizontalBox();
                  Label SaturationLabel = new Label("Saturation");
                  Slider SaturationSlider = new Slider() {value = Saturation, highValue = 2.0f, lowValue = 0};
                  SaturationSlider.value = Saturation;
                  FloatField SaturationField = new FloatField() {value = Saturation};
                  SaturationSlider.style.width = 100;
                  SaturationSlider.RegisterValueChangedCallback(evt => {Saturation = evt.newValue; SaturationField.value = Saturation; RayMaster.LocalTTSettings.Saturation = Saturation;});
                  SaturationField.RegisterValueChangedCallback(evt => {Saturation = evt.newValue; SaturationSlider.value = Saturation; RayMaster.LocalTTSettings.Saturation = Saturation;});
               SaturationContainer.Add(SaturationLabel);
               SaturationContainer.Add(SaturationSlider);
               SaturationContainer.Add(SaturationField);

               // VisualElement ContrastContainer = CreateHorizontalBox();
               //    Label ContrastLabel = new Label("Contrast");
               //    Slider ContrastSlider = new Slider() {value = Contrast, highValue = 2.0f, lowValue = 0};
               //    FloatField ContrastField = new FloatField() {value = Contrast};
               //    ContrastSlider.style.width = 100;
               //    ContrastSlider.RegisterValueChangedCallback(evt => {Contrast = evt.newValue; ContrastField.value = Contrast; RayMaster.LocalTTSettings.Contrast = Contrast;});
               //    ContrastField.RegisterValueChangedCallback(evt => {Contrast = evt.newValue; ContrastSlider.value = Contrast; RayMaster.LocalTTSettings.Contrast = Contrast;});
               // ContrastContainer.Add(ContrastLabel);
               // ContrastContainer.Add(ContrastSlider);
               // ContrastContainer.Add(ContrastField);
            BCSContainer.Add(SaturationContainer);
            // BCSContainer.Add(ContrastContainer);

            PostProcessingMenu.Add(BCSToggle);
            if(DoBCS) PostProcessingMenu.Add(BCSContainer);
            BCSToggle.RegisterValueChangedCallback(evt => {DoBCS = evt.newValue; RayMaster.LocalTTSettings.DoBCS = DoBCS; if(evt.newValue) PostProcessingMenu.Insert(PostProcessingMenu.IndexOf(BCSToggle) + 1, BCSContainer); else PostProcessingMenu.Remove(BCSContainer);});


            Toggle DoVignetteToggle = new Toggle() {value = DoVignette, text = "Enable Vignette"};
            PostProcessingMenu.Add(DoVignetteToggle);
            VisualElement VignetteContainer = new VisualElement();
               FloatSliderPair VignetteStrengthContainer = CreatePairedFloatSlider("Vignette Strength", 0, 4, ref strengthVignette);
               VignetteStrengthContainer.DynamicSlider.RegisterValueChangedCallback(evt => {strengthVignette = evt.newValue; VignetteStrengthContainer.DynamicField.value = strengthVignette; RayMaster.LocalTTSettings.strengthVignette = strengthVignette;});
               VignetteStrengthContainer.DynamicField.RegisterValueChangedCallback(evt => {strengthVignette = evt.newValue; VignetteStrengthContainer.DynamicSlider.value = strengthVignette; RayMaster.LocalTTSettings.strengthVignette = strengthVignette;});

               FloatSliderPair innerVignetteContainer = CreatePairedFloatSlider("Vignette Inner", 0, 4, ref innerVignette);
               innerVignetteContainer.DynamicSlider.RegisterValueChangedCallback(evt => {innerVignette = evt.newValue; innerVignetteContainer.DynamicField.value =  innerVignette; RayMaster.LocalTTSettings.innerVignette = innerVignette;});
               innerVignetteContainer.DynamicField.RegisterValueChangedCallback(evt =>  {innerVignette = evt.newValue; innerVignetteContainer.DynamicSlider.value = innerVignette; RayMaster.LocalTTSettings.innerVignette = innerVignette;});

               FloatSliderPair outerVignetteContainer = CreatePairedFloatSlider("Vignette Outer", 0, 4, ref outerVignette);
               outerVignetteContainer.DynamicSlider.RegisterValueChangedCallback(evt => {outerVignette = evt.newValue; outerVignetteContainer.DynamicField.value =  outerVignette; RayMaster.LocalTTSettings.outerVignette = outerVignette;});
               outerVignetteContainer.DynamicField.RegisterValueChangedCallback(evt =>  {outerVignette = evt.newValue; outerVignetteContainer.DynamicSlider.value = outerVignette; RayMaster.LocalTTSettings.outerVignette = outerVignette;});

               FloatSliderPair curveVignetteContainer = CreatePairedFloatSlider("Vignette Curve", 0, 4, ref curveVignette);
               curveVignetteContainer.DynamicSlider.RegisterValueChangedCallback(evt => {curveVignette = evt.newValue; curveVignetteContainer.DynamicField.value =  curveVignette; RayMaster.LocalTTSettings.curveVignette = curveVignette;});
               curveVignetteContainer.DynamicField.RegisterValueChangedCallback(evt =>  {curveVignette = evt.newValue; curveVignetteContainer.DynamicSlider.value = curveVignette; RayMaster.LocalTTSettings.curveVignette = curveVignette;});

               ColorField VignetteColorField = new ColorField();
               VignetteColorField.value = ColorVignette;
               VignetteColorField.style.width = 150;
               VignetteColorField.RegisterValueChangedCallback(evt => {ColorVignette = evt.newValue; RayMaster.LocalTTSettings.ColorVignette = new Vector3(ColorVignette.r,ColorVignette.g,ColorVignette.b);});


            VignetteContainer.Add(innerVignetteContainer.DynamicContainer);
            VignetteContainer.Add(outerVignetteContainer.DynamicContainer);
            VignetteContainer.Add(VignetteStrengthContainer.DynamicContainer);
            VignetteContainer.Add(curveVignetteContainer.DynamicContainer);
            VignetteContainer.Add(VignetteColorField);

            DoVignetteToggle.RegisterValueChangedCallback(evt => {DoVignette = evt.newValue; RayMaster.LocalTTSettings.DoVignette = DoVignette;if(evt.newValue) PostProcessingMenu.Insert(PostProcessingMenu.IndexOf(DoVignetteToggle) + 1, VignetteContainer); else PostProcessingMenu.Remove(VignetteContainer);});
            if(DoVignette) PostProcessingMenu.Add(VignetteContainer);



            Toggle DoChromaAberToggle = new Toggle() {value = DoChromaAber, text = "Enable Chomatic Aberation"};
            PostProcessingMenu.Add(DoChromaAberToggle);
            VisualElement ChromaAberContainer = CreateHorizontalBox();
               Label ChromaAberLabel = new Label("Chromatic Aberation Strength");
               Slider ChromaAberSlider = new Slider() {value = ChromaDistort, highValue = 7.0f, lowValue = 0};
               ChromaAberSlider.value = ChromaDistort;
               FloatField ChromaAberField = new FloatField() {value = ChromaDistort};
               ChromaAberSlider.RegisterValueChangedCallback(evt => {ChromaDistort = evt.newValue; ChromaAberField.value = ChromaDistort; RayMaster.LocalTTSettings.ChromaDistort = ChromaDistort;});
               ChromaAberField.RegisterValueChangedCallback(evt =>  {ChromaDistort = evt.newValue; ChromaAberSlider.value = ChromaDistort; RayMaster.LocalTTSettings.ChromaDistort = ChromaDistort;});
            ChromaAberSlider.style.width = 100;
            ChromaAberContainer.Add(ChromaAberLabel);
            ChromaAberContainer.Add(ChromaAberSlider);
            ChromaAberContainer.Add(ChromaAberField);

            DoChromaAberToggle.RegisterValueChangedCallback(evt => {DoChromaAber = evt.newValue; RayMaster.LocalTTSettings.DoChromaAber = DoChromaAber;if(evt.newValue) PostProcessingMenu.Insert(PostProcessingMenu.IndexOf(DoChromaAberToggle) + 1, ChromaAberContainer); else PostProcessingMenu.Remove(ChromaAberContainer);});
            if(DoChromaAber) PostProcessingMenu.Add(ChromaAberContainer);



      }


      private VisualElement CreateHorizontalBox(string Name = "") {
         VisualElement HorizBox = new VisualElement();
         HorizBox.style.flexDirection = FlexDirection.Row;
         return HorizBox;
      }


      ObjectField SelectiveField;

       private void UndoInstances() {
          GameObject Obj = SelectiveField.value as GameObject;
          if(Obj == null) return;
          if(!Obj.scene.IsValid()) {
            GameObject[] InstanceQues = PrefabUtility.FindAllInstancesOfPrefab(SelectiveField.value as GameObject);
            int QueCount = InstanceQues.Length;
            ParentObject OrigPObj = null;
            RayTracingObject OrigRObj = null;
            string OrigName = "";
            if(QueCount > 0) {
               InstancedObject[] TempVar = InstanceQues[0].GetComponentsInChildren<InstancedObject>();
               if(TempVar != null && TempVar.Length != 0) {
                  if(TempVar[0].InstanceParent != null) {
                     OrigPObj = TempVar[0].InstanceParent.GetComponent<ParentObject>();
                     OrigRObj = TempVar[0].InstanceParent.GetComponent<RayTracingObject>();
                     OrigName = TempVar[0].InstanceParent.gameObject.name;
                  }
               }
            }
            for(int i = 0; i < QueCount; i++) {
               InstancedObject[] TempVar = InstanceQues[i].GetComponentsInChildren<InstancedObject>();
               if(TempVar != null && TempVar.Length != 0) {
                  PrefabUtility.RevertPrefabInstance(InstanceQues[i],  InteractionMode.AutomatedAction);
                  for(int i2 = 0; i2 < InstanceQues[i].transform.childCount; i2++) {
                     if(OrigName.Contains(InstanceQues[i].transform.GetChild(i2).gameObject.name)) {
                        InstanceQues[i].transform.GetChild(i2).gameObject.AddComponent<RayTracingObject>(OrigRObj);
                        InstanceQues[i].transform.GetChild(i2).gameObject.AddComponent<ParentObject>();
                     }
                  }
               }
            }
            DestroyImmediate(OrigPObj.gameObject);
          } else {
             var E = Obj.GetComponentsInChildren<InstancedObject>();
             foreach(var a in E) {
                  GameObject TempOBJ = GameObject.Instantiate(a.InstanceParent.gameObject);
                  TempOBJ.transform.parent = a.gameObject.transform.parent;
                  TempOBJ.transform.position = a.gameObject.transform.position;
                  TempOBJ.transform.rotation = a.gameObject.transform.rotation;
                 DestroyImmediate(a.gameObject);
             }
             ParentData SourceParent = GrabChildren2(Obj.transform);

             SolveChildren(SourceParent);
          }
       }

       VisualElement MakeSpacer(int Height = 30) {
         VisualElement TempSpace = new VisualElement();
         TempSpace.style.minHeight = 30;
         TempSpace.style.maxHeight = 30;
         return TempSpace;
       }

      void AddHierarchyOptionsToMenu() {
         SelectiveField = new ObjectField();
         SelectiveField.objectType = typeof(GameObject);
         SelectiveField.label = "Selected Object";
         Button SelectiveAutoAssignButton = new Button(() => {
            ParentData SourceParent = GrabChildren2((SelectiveField.value as GameObject).transform);
            SolveChildren(SourceParent);
            }) {text = "Selective Auto Assign"};
         Button ReplaceInstanceButton = new Button(() => UndoInstances()) {text = "Undo Selective Instances"};

         
      
         ForceInstancesButton = new Button(() => {if(!Application.isPlaying) ConstructInstances(); else Debug.Log("Cant Do This In Editor");}) {text = "Force All Instances"};
         Button ForceInstancesSelectiveButton = new Button(() => {if(!Application.isPlaying) {if((SelectiveField.value as GameObject).TryGetComponent<MeshFilter>(out MeshFilter TempFilter)) ConstructInstancesSelective(TempFilter.sharedMesh); else Debug.Log("Missing Valid Object With Mesh");} else Debug.Log("Cant Do This In Editor");}) {text = "Force Selected Mesh Into Instances"};

         StaticButton = new Button(() => {if(!Application.isPlaying) OptimizeForStatic(); else Debug.Log("Cant Do This In Editor");}) {text = "Make All Static"};
         StaticButton.style.minWidth = 105;

         HierarchyOptionsMenu.Add(SelectiveField);
         HierarchyOptionsMenu.Add(ForceInstancesSelectiveButton);
         HierarchyOptionsMenu.Add(ReplaceInstanceButton);
         
         HierarchyOptionsMenu.Add(MakeSpacer());

         HierarchyOptionsMenu.Add(SelectiveAutoAssignButton);
         HierarchyOptionsMenu.Add(ForceInstancesButton);
         HierarchyOptionsMenu.Add(StaticButton);
      }

      public struct CustomGBufferData {
         public uint GeomNorm;
         public uint SurfNorm;
         public float t;
         public uint MatIndex;
      }
      private void GetFocalLength() {
         var old_rt = RenderTexture.active;
         RenderTexture.active = RayMaster.ScreenSpaceInfo;
         Texture2D currenttexture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
         #if UNITY_PIPELINE_HDRP
            currenttexture.ReadPixels(new Rect(Input.mousePosition.x, Input.mousePosition.y, 1, 1), 0, 0);
         #else
            currenttexture.ReadPixels(new Rect(Input.mousePosition.x, RayMaster.SourceHeight - Input.mousePosition.y, 1, 1), 0, 0);
         #endif
         currenttexture.Apply();
          RenderTexture.active = old_rt;
         var CenterDistance = currenttexture.GetPixelData<Vector4>(0);
         FocalSlider.value = CenterDistance[0].z + RayTracingMaster._camera.nearClipPlane;
         Destroy(currenttexture);
         CenterDistance.Dispose();
      }

         void EvaluateScene(Scene Current, Scene Next) {
            rootVisualElement.Clear();
            if(MainSource != null) MainSource.Clear();
            CreateGUI();
         }

         void SaveScene(Scene Current, string ThrowawayString) {
#if RemoveScriptsDuringSave
            ParentObject[] TempParents = GameObject.FindObjectsOfType<ParentObject>();
            foreach(var obj in TempParents) {
               obj.BotherToUpdate = false;
               DestroyImmediate(obj);
            }
            RayTracingObject[] TempRayObjs = GameObject.FindObjectsOfType<RayTracingObject>();
            foreach(var obj in TempRayObjs) {
               if(obj.DeleteObject) {
                  DestroyImmediate(obj);
               }
            }
#endif
            if(Assets != null) {
               EditorUtility.SetDirty(Assets);
               Assets.ClearAll();
               AssetManager.Assets = null;
            }
            InstancedManager Instanced = GameObject.Find("InstancedStorage").GetComponent<InstancedManager>();
            if(Instanced != null) {
               EditorUtility.SetDirty(Instanced);
               Instanced.ClearAll();
            }


            Cleared = true;
         }

         void SaveScenePost(Scene Current) {
#if RemoveScriptsDuringSave
            QuickStart();
            GameObject.Find("Scene").GetComponent<AssetManager>().EditorBuild();
#endif
         }

         // [Shortcut("TrueTrace/ScreenShot", KeyCode.None, ShortcutModifiers.Action)]
         // private static void TakeScreenShotHotkey() {
         //    if(Application.isPlaying) {
         //       TakeScreenshot();
         //    }
         // }

         // [Shortcut("TrueTrace/RebuildBVH", KeyCode.None, ShortcutModifiers.Action)]
         // private static void RebuildBVHHotkey() {
         //    EditorUtility.SetDirty(GameObject.Find("Scene").GetComponent<AssetManager>());
         //    BuildWatch.Start();
         //    GameObject.Find("Scene").GetComponent<AssetManager>().EditorBuild();
         // }

      public static void SaveTexture (RenderTexture RefTex, string Path) {
         Texture2D tex = new Texture2D(RefTex.width, RefTex.height, TextureFormat.RGBAFloat, false);
         RenderTexture.active = RefTex;
         tex.ReadPixels(new Rect(0, 0, RefTex.width, RefTex.height), 0, 0);
         tex.Apply();
         Color[] ArrayA = tex.GetPixels(0);
         int Coun = ArrayA.Length;
         for(int i = 0; i < Coun; i++) {
            ArrayA[i].a = 1.0f;
         }
         tex.SetPixels(ArrayA, 0);
         tex.Apply();
         byte[] bytes = tex.EncodeToPNG();
         System.IO.File.WriteAllBytes(Path, bytes);
      }

      public static void IncrementRenderCounter() {
         #if TTIncrementRenderCounter
            string Path = PlayerPrefs.GetString("CounterPath") + "/TTStats.txt";
            List<string> RenderStatData = new List<string>();
            DateTime CurrentDate = DateTime.Now;
            string FormattedDate = CurrentDate.ToString("yyyy-MM-dd");
            if(File.Exists(Path)) {
               using (StreamReader sr = new StreamReader(Path)) {
                   string Line;
                   while((Line = sr.ReadLine()) != null) {
                     RenderStatData.Add(Line);
                   }
               }            
            }
            int DateIndex = RenderStatData.IndexOf("Renders For: " + FormattedDate);
            if(DateIndex == -1) {
               RenderStatData.Add("Renders For: " + FormattedDate);
               RenderStatData.Add("1");
            } else {
               RenderStatData[DateIndex + 1] = "" + (int.Parse(RenderStatData[DateIndex + 1]) + 1);
            }
            using (StreamWriter sw = new StreamWriter(Path)) {
               int Coun = RenderStatData.Count;
               for(int i = 0; i < Coun; i++) {
                  sw.WriteLine(RenderStatData[i]);
               }
            }
         #endif
      }

         public static void TakeScreenshot() {
            IncrementRenderCounter();
           string SegmentNumber = "";
           string FilePath = "";
           int TempSeg = 1;
            do {
               SegmentNumber = "";
               FilePath = "";
               int TempTempSeg = TempSeg;
              int[] NumSegments = new int[3];
              for(int i = 0; i < 3; i++) {
                  NumSegments[i] = ((TempTempSeg) % 10);
                  TempTempSeg /= 10;
              }
              for(int i = 0; i < 3; i++) {
                  SegmentNumber += NumSegments[2 - i];
              }
              TempSeg++;

               FilePath = PlayerPrefs.GetString("ScreenShotPath") + "/" + SceneManager.GetActiveScene().name.Replace(" ", "") + "_" + RayTracingMaster._camera.name + "_" + SegmentNumber + ".png";
            } while(System.IO.File.Exists(FilePath));
           

            ScreenCapture.CaptureScreenshot(FilePath);
            #if MultiMapScreenshot
               SaveTexture(RayTracingMaster.RayMaster.MultiMapMatIDTexture, FilePath.Replace(".png", "") + "_MatID.png");
               SaveTexture(RayTracingMaster.RayMaster.MultiMapMeshIDTexture, FilePath.Replace(".png", "") + "_MeshID.png");
            #endif
            
            // ScreenCapture.CaptureScreenshot(PlayerPrefs.GetString("ScreenShotPath") + "/" + System.DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ", " + RayTracingMaster.SampleCount + " Samples.png");
            UnityEditor.AssetDatabase.Refresh();
         }
         bool HasNoMore = false;
Slider AperatureSlider;


        public void CreateGUI() {
            EnsureInitializedGlobalDefines();

            HasNoMore = false;
            string BasePath = Application.dataPath.Replace("/Assets", "");
            if(!System.IO.Directory.Exists(BasePath + "/TrueTrace")) {
               System.IO.Directory.CreateDirectory(BasePath + "/TrueTrace");
            }
            if(!PlayerPrefs.HasKey("ScreenShotPath")) {
               PlayerPrefs.SetString("ScreenShotPath",  BasePath + "/TrueTrace/ScreenShots");
            }
            if(!PlayerPrefs.HasKey("PanoramaPath")) {
               PlayerPrefs.SetString("PanoramaPath",  BasePath + "/TrueTrace/ScreenShots");
            }
            if(!PlayerPrefs.HasKey("TimelinePath")) {
               PlayerPrefs.SetString("TimelinePath",  BasePath + "/TrueTrace/TimelineFrames");
            }
            if(!PlayerPrefs.HasKey("CounterPath")) {
               PlayerPrefs.SetString("CounterPath",  Application.persistentDataPath);
            }
            if(!PlayerPrefs.HasKey("TurnTablePath")) {
               PlayerPrefs.SetString("TurnTablePath",  BasePath + "/TrueTrace/TurnTables");
            }
            if(!System.IO.Directory.Exists(PlayerPrefs.GetString("TurnTablePath"))) {
               System.IO.Directory.CreateDirectory(BasePath + "/TrueTrace/TurnTables");
               PlayerPrefs.SetString("TurnTablePath",  BasePath + "/TrueTrace/TurnTables");
            }
            if(!System.IO.Directory.Exists(PlayerPrefs.GetString("ScreenShotPath"))) {
               System.IO.Directory.CreateDirectory(BasePath + "/TrueTrace/ScreenShots");
               PlayerPrefs.SetString("ScreenShotPath",  BasePath + "/TrueTrace/ScreenShots");
            }
            if(!System.IO.Directory.Exists(PlayerPrefs.GetString("PanoramaPath"))) {
               System.IO.Directory.CreateDirectory(BasePath + "/TrueTrace/ScreenShots");
               PlayerPrefs.SetString("PanoramaPath",  BasePath + "/TrueTrace/ScreenShots");
            }
            if(!System.IO.Directory.Exists(PlayerPrefs.GetString("TimelinePath"))) {
               System.IO.Directory.CreateDirectory(BasePath + "/TrueTrace/TimelineFrames");
               PlayerPrefs.SetString("TimelinePath",  BasePath + "/TrueTrace/TimelineFrames");
            }

            OnFocus();
            MainSource = new VisualElement();
            MainSource.style.justifyContent = Justify.FlexStart; // Align items to the start
            MainSource.style.alignItems = Align.FlexStart; // Align content to start of the container
            HierarchyOptionsMenu = new VisualElement();
            MaterialPairingMenu = new VisualElement();
            SceneSettingsMenu = new VisualElement();
            HardSettingsMenu = new VisualElement();
            PostProcessingMenu = new VisualElement();
            InputMaterialField = new ObjectField();
            InputMaterialField.objectType = typeof(Material);
            InputMaterialField.label = "Drag a material with the desired shader here ->";
            InputMaterialField.RegisterValueChangedCallback(evt => {MaterialPairingMenu.Clear(); MaterialPairingMenu.Add(InputMaterialField); AddAssetsToMenu();});
            MaterialPairingMenu.Add(InputMaterialField);
            toolbar = new Toolbar();
            rootVisualElement.Add(toolbar);
            Button MainSourceButton = new Button(() => {rootVisualElement.Clear(); rootVisualElement.Add(toolbar); rootVisualElement.Add(MainSource); MaterialPairingMenu.Clear();});
            Button MaterialPairButton = new Button(() => {rootVisualElement.Clear(); rootVisualElement.Add(toolbar); InputMaterialField.value = null; MaterialPairingMenu.Add(InputMaterialField); rootVisualElement.Add(MaterialPairingMenu);});
            Button SceneSettingsButton = new Button(() => {rootVisualElement.Clear(); rootVisualElement.Add(toolbar); rootVisualElement.Add(SceneSettingsMenu);});
            Button HardSettingsButton = new Button(() => {rootVisualElement.Clear(); rootVisualElement.Add(toolbar); HardSettingsMenu.Clear(); AddHardSettingsToMenu(); rootVisualElement.Add(HardSettingsMenu);});
            Button HierarchyOptionsButton = new Button(() => {rootVisualElement.Clear(); rootVisualElement.Add(toolbar); rootVisualElement.Add(HierarchyOptionsMenu);});
            MainSourceButton.text = "Main Options";
            MaterialPairButton.text = "Material Pair Options";
            SceneSettingsButton.text = "Scene Settings";
            HardSettingsButton.text = "Functionality Settings";
            HierarchyOptionsButton.text = "Hierarchy Options";
            if(Assets == null) {
               toolbar.Add(MainSourceButton);
               toolbar.Add(HardSettingsButton);
               RearrangeElement = new VisualElement();
               Button RearrangeButton = new Button(() => {UnityEditor.PopupWindow.Show(new Rect(0,0,10,10), new PopupWarningWindow());}) {text="Arrange Hierarchy"};
               RearrangeElement.Add(RearrangeButton);
               MainSource.Add(RearrangeElement);
               rootVisualElement.Add(MainSource);
               return;
            } else {
               toolbar.Add(MainSourceButton);
               toolbar.Add(MaterialPairButton);
               toolbar.Add(SceneSettingsButton);
               toolbar.Add(HardSettingsButton);
               toolbar.Add(HierarchyOptionsButton);
               {rootVisualElement.Clear(); rootVisualElement.Add(toolbar); rootVisualElement.Add(MainSource); MaterialPairingMenu.Clear();}
               Assets.UpdateMaterialDefinition();


               #if UNITY_PIPELINE_HDRP
                  GameObject NewObject = GameObject.Find("HDRPPASS");
                  
                  if(NewObject == null) {
                      NewObject = new GameObject();
                      NewObject.name = "HDRPPASS";
                      NewObject.AddComponent<UnityEngine.Rendering.HighDefinition.CustomPassVolume>();
                      var A = NewObject.GetComponent<UnityEngine.Rendering.HighDefinition.CustomPassVolume>();
                      A.injectionPoint = UnityEngine.Rendering.HighDefinition.CustomPassInjectionPoint.BeforePostProcess;
                      A.customPasses.Add(new HDRPCompatability());
                  }
               #endif
            }

            if(RayMaster != null && Assets != null) {
            AddNormalSettings();
           Assets.UseSkinning = MeshSkin;
           RayMaster.AtmoNumLayers = AtmoScatter;
           Assets.MainDesiredRes = AtlasSize;
           if(RayMaster.LocalTTSettings == null) RayMaster.LoadTT();
           LightEnergyScale = RayMaster.LocalTTSettings.LightEnergyScale;
           LEMEnergyScale = RayMaster.LocalTTSettings.LEMEnergyScale;
           RayTracingMaster.DoSaving = DoSaving;
           MatChangeResetsAccum = RayMaster.LocalTTSettings.MatChangeResetsAccum;
           UseTransmittanceInNEE = RayMaster.LocalTTSettings.UseTransmittanceInNEE;
           BounceCount = RayMaster.LocalTTSettings.bouncecount;
           RenderRes = RayMaster.LocalTTSettings.RenderScale;
           RR = RayMaster.LocalTTSettings.UseRussianRoulette;
           Moving = RayMaster.LocalTTSettings.DoTLASUpdates;
           Accumulate = RayMaster.LocalTTSettings.Accumulate;
           NEE = RayMaster.LocalTTSettings.UseNEE;
           Bloom = RayMaster.LocalTTSettings.PPBloom;
           DoSharpen = RayMaster.LocalTTSettings.DoSharpen;
           Sharpness = RayMaster.LocalTTSettings.Sharpness;
           HDRILongLat = RayMaster.LocalTTSettings.HDRILongLat;
           BloomStrength = RayMaster.LocalTTSettings.BloomStrength;
           DoF = RayMaster.LocalTTSettings.PPDoF;
           ClayMetalOverride = RayMaster.LocalTTSettings.ClayMetalOverride;
           ClayRoughnessOverride = RayMaster.LocalTTSettings.ClayRoughnessOverride;
           ClayColor = RayMaster.LocalTTSettings.ClayColor;
           GroundColor = RayMaster.LocalTTSettings.GroundColor;
           DoFAperature = RayMaster.LocalTTSettings.DoFAperature;
           DoFAperatureScale = RayMaster.LocalTTSettings.DoFAperatureScale;
           DoFFocal = RayMaster.LocalTTSettings.DoFFocal;
           DoExposure = RayMaster.LocalTTSettings.PPExposure;
           ExposureAuto = RayMaster.LocalTTSettings.ExposureAuto;
           Exposure = RayMaster.LocalTTSettings.Exposure;
           ReSTIRGI = RayMaster.LocalTTSettings.UseReSTIRGI;
           GITemporal = RayMaster.LocalTTSettings.UseReSTIRGITemporal;
           GISpatial = RayMaster.LocalTTSettings.UseReSTIRGISpatial;
           SampleValid = RayMaster.LocalTTSettings.DoReSTIRGIConnectionValidation;
           GITemporalMCap = RayMaster.LocalTTSettings.ReSTIRGITemporalMCap;
           TAA = RayMaster.LocalTTSettings.PPTAA;
           FXAA = RayMaster.LocalTTSettings.PPFXAA;
           ToneMap = RayMaster.LocalTTSettings.PPToneMap;
           ToneMapIndex = RayMaster.LocalTTSettings.ToneMapper;
           UpscalerMethod = RayMaster.LocalTTSettings.UpscalerMethod;
           DoPartialRendering = RayMaster.LocalTTSettings.DoPartialRendering;
           PartialRenderingFactor = RayMaster.LocalTTSettings.PartialRenderingFactor;
           DoFirefly = RayMaster.LocalTTSettings.DoFirefly;
           RISCount = RayMaster.LocalTTSettings.RISCount;
           ImprovedPrimaryHit = RayMaster.LocalTTSettings.ImprovedPrimaryHit;
           ClayMode = RayMaster.LocalTTSettings.ClayMode;
           SceneBackgroundColor = new Color(RayMaster.LocalTTSettings.SceneBackgroundColor.x, RayMaster.LocalTTSettings.SceneBackgroundColor.y, RayMaster.LocalTTSettings.SceneBackgroundColor.z, 1);
           SecondarySceneBackgroundColor = new Color(RayMaster.LocalTTSettings.SecondarySceneBackgroundColor.x, RayMaster.LocalTTSettings.SecondarySceneBackgroundColor.y, RayMaster.LocalTTSettings.SecondarySceneBackgroundColor.z, 1);
           BackgroundIntensity = RayMaster.LocalTTSettings.BackgroundIntensity;
           PrimaryBackgroundTint = RayMaster.LocalTTSettings.PrimaryBackgroundTint;
           PrimaryBackgroundTintColor = new Color(RayMaster.LocalTTSettings.PrimaryBackgroundTintColor.x, RayMaster.LocalTTSettings.PrimaryBackgroundTintColor.y, RayMaster.LocalTTSettings.PrimaryBackgroundTintColor.z, 1);
           PrimaryBackgroundContrast = RayMaster.LocalTTSettings.PrimaryBackgroundContrast;
           IndirectBoost = RayMaster.LocalTTSettings.IndirectBoost;
           BackgroundType = RayMaster.LocalTTSettings.BackgroundType;
           SecondaryBackgroundType = RayMaster.LocalTTSettings.SecondaryBackgroundType;
           SkyDesaturate = RayMaster.LocalTTSettings.SkyDesaturate;
           SecondarySkyDesaturate = RayMaster.LocalTTSettings.SecondarySkyDesaturate;
           FireflyFrameCount = RayMaster.LocalTTSettings.FireflyFrameCount;
           FireflyFrameInterval = RayMaster.LocalTTSettings.FireflyFrameInterval;
           OIDNFrameCount = RayMaster.LocalTTSettings.OIDNFrameCount;
           RayMaster.IsFocusing = false;
           ConvBloom = RayMaster.LocalTTSettings.ConvBloom;
           ConvStrength = RayMaster.LocalTTSettings.ConvStrength;
           ConvBloomThreshold = RayMaster.LocalTTSettings.ConvBloomThreshold;
           ConvBloomSize = RayMaster.LocalTTSettings.ConvBloomSize;
           ConvBloomDistExp = RayMaster.LocalTTSettings.ConvBloomDistExp;
           ConvBloomDistExpClampMin = RayMaster.LocalTTSettings.ConvBloomDistExpClampMin;
           ConvBloomDistExpScale = RayMaster.LocalTTSettings.ConvBloomDistExpScale;
           DoBCS = RayMaster.LocalTTSettings.DoBCS;
           Saturation = RayMaster.LocalTTSettings.Saturation;
           Contrast = RayMaster.LocalTTSettings.Contrast;
           DoVignette = RayMaster.LocalTTSettings.DoVignette;
           innerVignette = RayMaster.LocalTTSettings.innerVignette;
           outerVignette = RayMaster.LocalTTSettings.outerVignette;
           strengthVignette = RayMaster.LocalTTSettings.strengthVignette;
           curveVignette = RayMaster.LocalTTSettings.curveVignette;
           ColorVignette = new Color(RayMaster.LocalTTSettings.ColorVignette.x,RayMaster.LocalTTSettings.ColorVignette.y,RayMaster.LocalTTSettings.ColorVignette.z,1);
           FogDensity = RayMaster.LocalTTSettings.FogDensity;
           FogHeight = RayMaster.LocalTTSettings.FogHeight;
           DoChromaAber = RayMaster.LocalTTSettings.DoChromaAber;
         }

           // AddHardSettingsToMenu();
           AddHierarchyOptionsToMenu();
           BVHBuild = new Button(() => OnStartAsyncCombined()) {text = "Build Aggregated BVH"};
           BVHBuild.style.minWidth = 145;
           ScreenShotButton = new Button(() => TakeScreenshot()) {text = "Take Screenshot"};
           ScreenShotButton.style.minWidth = 100;



           
           ClearButton = new Button(() => {
            if(!Application.isPlaying) {
               EditorUtility.SetDirty(Assets);
               Assets.ClearAll();
               InstancedManager Instanced = GameObject.Find("InstancedStorage").GetComponent<InstancedManager>();
               EditorUtility.SetDirty(Instanced);
               Instanced.ClearAll();
               Cleared = true;
               // Assets.RunningTasks = 0;
           } else Debug.Log("Cant Do This In Editor");}) {text = "Clear Parent Data"};
           ClearButton.style.minWidth = 145;
           QuickStartButton = new Button(() => QuickStart()) {text = "Auto Assign Scripts"};
           QuickStartButton.style.minWidth = 111;

           VisualElement SpacerElement = new VisualElement();
           SpacerElement.style.minWidth = 145;


           // IntegerField AtlasField = new IntegerField() {value = AtlasSize, label = "Atlas Size"};
           // AtlasField.isDelayed = true;
           // AtlasField.RegisterValueChangedCallback(evt => {if(!Application.isPlaying) {AtlasSize = evt.newValue; AtlasSize = Mathf.Min(AtlasSize, 16384); AtlasSize = Mathf.Max(AtlasSize, 32); AtlasField.value = AtlasSize; Assets.MainDesiredRes = AtlasSize;} else AtlasField.value = AtlasSize;});
           //     AtlasField.ElementAt(0).style.minWidth = 65;
           //     AtlasField.ElementAt(1).style.width = 45;

           Box ButtonField1 = new Box();
           ButtonField1.style.flexDirection = FlexDirection.Row;
           ButtonField1.Add(BVHBuild);
           ButtonField1.Add(ScreenShotButton);
           // ButtonField1.Add(ClearButton);
           MainSource.Add(ButtonField1);

           Box ButtonField2 = new Box();
           ButtonField2.style.flexDirection = FlexDirection.Row;
           // ButtonField2.Add(SpacerElement);
           ButtonField2.Add(ClearButton);
           ButtonField2.Add(QuickStartButton);
           MainSource.Add(ButtonField2);

           Box TopEnclosingBox = new Box();
               TopEnclosingBox.style.flexDirection = FlexDirection.Row;
               IntegerField BounceField = new IntegerField() {value = (int)BounceCount, label = "Max Bounces"};
               BounceField.ElementAt(0).style.minWidth = 75;
               BounceField.ElementAt(1).style.width = 25;
               BounceField.style.paddingRight = 40;
               TopEnclosingBox.Add(BounceField);
               BounceField.RegisterValueChangedCallback(evt => {BounceCount = (int)evt.newValue; RayMaster.LocalTTSettings.bouncecount = BounceCount;});        
               ResField = new FloatField("Internal Resolution Ratio") {value = RenderRes};
               ResField.ElementAt(0).style.minWidth = 75;
               ResField.ElementAt(1).style.width = 35;
               TopEnclosingBox.Add(ResField);
               ResField.RegisterCallback<FocusOutEvent>(evt => {
                                                            ResField.value = Mathf.Max(Mathf.Min(ResField.value, 1.0f), 0.1f);
                                                            RenderRes = ResField.value; 
                                                            RayMaster.LocalTTSettings.RenderScale = RenderRes; 
                                                            if(MainSource.Children().Contains(UpscalerField)) {
                                                               if(RenderRes == 1.0f) MainSource.Remove(UpscalerField);
                                                            } else if(RenderRes != 1.0f) MainSource.Insert(MainSource.IndexOf(DoPartialRenderingToggle), UpscalerField);
                                                         });        
               // TopEnclosingBox.Add(AtlasField);
           MainSource.Add(TopEnclosingBox);

           RRToggle = new Toggle() {value = RR, text = "Use Russian Roulette"};
            #if TTAdvancedSettings
               MainSource.Add(RRToggle);
            #endif
           RRToggle.RegisterValueChangedCallback(evt => {RR = evt.newValue; RayMaster.LocalTTSettings.UseRussianRoulette = RR;});

           MovingToggle = new Toggle() {value = Moving, text = "Enable Object Moving"};
           MovingToggle.tooltip = "Enables realtime updating of materials and object positions, laggy to leave on for scenes with high ParentObject counts";
           MainSource.Add(MovingToggle);
           MovingToggle.RegisterValueChangedCallback(evt => {Moving = evt.newValue; RayMaster.LocalTTSettings.DoTLASUpdates = Moving;});

           AccumToggle = new Toggle() {value = Accumulate, text = "Allow Image Accumulation"};
           MainSource.Add(AccumToggle);
           AccumToggle.RegisterValueChangedCallback(evt => {Accumulate = evt.newValue; RayMaster.LocalTTSettings.Accumulate = Accumulate;});

           NEEToggle = new Toggle() {value = NEE, text = "Use Next Event Estimation"};
           MainSource.Add(NEEToggle);
           VisualElement NEEBox = new VisualElement();
            Label RISLabel = new Label("RIS Count");
            RISCountField = new IntegerField() {value = RISCount};
            RISCountField.RegisterValueChangedCallback(evt => {RISCount = (int)evt.newValue; RayMaster.LocalTTSettings.RISCount = RISCount;});
            NEEBox.style.flexDirection = FlexDirection.Row;
            NEEBox.Add(RISLabel);
            NEEBox.Add(RISCountField);
           NEEToggle.RegisterValueChangedCallback(evt => {NEE = evt.newValue; RayMaster.LocalTTSettings.UseNEE = NEE; if(evt.newValue) MainSource.Insert(MainSource.IndexOf(NEEToggle) + 1, NEEBox);else MainSource.Remove(NEEBox);});
            #if TTAdvancedSettings
               if(NEEToggle.value) {
                  MainSource.Add(NEEBox);
               }
            #endif
       

           SkinToggle = new Toggle() {value = MeshSkin, text = "Allow Mesh Skinning"};
            #if TTAdvancedSettings
               MainSource.Add(SkinToggle);
            #endif
           SkinToggle.RegisterValueChangedCallback(evt => {MeshSkin = evt.newValue; Assets.UseSkinning = MeshSkin;});

            List<string> DenoiserSettings = new List<string>();
            DenoiserSettings.Add("None");
            DenoiserSettings.Add("ASVGF");
            #if UseOIDN
               DenoiserSettings.Add("OIDN");
            DenoiserSettings.Add("OptiX");
            #endif
            PopupField<string> DenoiserField = new PopupField<string>("<b>Denoiser</b>");
            VisualElement DenoiserExtrasContainer = CreateHorizontalBox("Denoiser Extra Info Container");
            #if !UseOIDN
               RayMaster.LocalTTSettings.DenoiserMethod = Mathf.Min(RayMaster.LocalTTSettings.DenoiserMethod, 1);
            #endif
            DenoiserMethod = RayMaster.LocalTTSettings.DenoiserMethod;
            DenoiserField.ElementAt(0).style.minWidth = 55;
            DenoiserField.ElementAt(1).style.minWidth = 75;
            DenoiserField.ElementAt(1).style.maxWidth = 75;
            DenoiserField.style.width = 450;
            DenoiserField.choices = DenoiserSettings;
            DenoiserField.index = DenoiserMethod;
            DenoiserField.style.flexDirection = FlexDirection.Row;
            DenoiserExtrasContainer.Clear();
            DenoiserField.RegisterValueChangedCallback(evt => {
               #if !UseOIDN
                  DenoiserField.index = Mathf.Min(DenoiserField.index, 1);
               #endif
               DenoiserMethod = DenoiserField.index;
               RayMaster.LocalTTSettings.DenoiserMethod = DenoiserMethod;
               DenoiserExtrasContainer.Clear();
               if(DenoiserMethod == 2 || DenoiserMethod == 3) {
                  OIDNFrameField = new IntegerField("Frame Delay") {value = OIDNFrameCount};
                  OIDNFrameField.ElementAt(0).style.minWidth = 65;
                  OIDNFrameField.RegisterValueChangedCallback(evt => {OIDNFrameCount = (int)evt.newValue; RayMaster.LocalTTSettings.OIDNFrameCount = OIDNFrameCount;});


                  Slider OIDNBlendRatioSlider = new Slider() {label = "Blend Ratio: ", value = OIDNBlendRatio, highValue = 1.0f, lowValue = 0.0f};
                  OIDNBlendRatioSlider.value = OIDNBlendRatio;
                  OIDNBlendRatioSlider.showInputField = true;        
                  OIDNBlendRatioSlider.style.width = 200;
                  OIDNBlendRatioSlider.ElementAt(0).style.minWidth = 65;
                  OIDNBlendRatioSlider.RegisterValueChangedCallback(evt => {OIDNBlendRatio = evt.newValue; RayMaster.LocalTTSettings.OIDNBlendRatio = OIDNBlendRatio;});

                  DenoiserExtrasContainer.Add(OIDNFrameField);
                  DenoiserExtrasContainer.Add(OIDNBlendRatioSlider);
               }
            });
            if(DenoiserMethod == 2 || DenoiserMethod == 3) {
               OIDNFrameField = new IntegerField("Frame Delay") {value = OIDNFrameCount};
               OIDNFrameField.ElementAt(0).style.minWidth = 65;
               OIDNFrameField.RegisterValueChangedCallback(evt => {OIDNFrameCount = (int)evt.newValue; RayMaster.LocalTTSettings.OIDNFrameCount = OIDNFrameCount;});
   

               Slider OIDNBlendRatioSlider = new Slider() {label = "Blend Ratio: ", value = OIDNBlendRatio, highValue = 1.0f, lowValue = 0.0f};
               OIDNBlendRatioSlider.value = OIDNBlendRatio;
               OIDNBlendRatioSlider.showInputField = true;        
               OIDNBlendRatioSlider.style.width = 200;
               OIDNBlendRatioSlider.ElementAt(0).style.minWidth = 65;
               OIDNBlendRatioSlider.RegisterValueChangedCallback(evt => {OIDNBlendRatio = evt.newValue; RayMaster.LocalTTSettings.OIDNBlendRatio = OIDNBlendRatio;});



               DenoiserExtrasContainer.Add(OIDNFrameField);
               DenoiserExtrasContainer.Add(OIDNBlendRatioSlider);
            } 
            DenoiserField.Add(DenoiserExtrasContainer);

            MainSource.Add(DenoiserField);













           GIToggle = new Toggle() {value = ReSTIRGI, text = "Use ReSTIR GI"};
           VisualElement GIFoldout = new VisualElement() {};
           Box EnclosingGI = new Box();
               Box TopGI = new Box();
                   TopGI.style.flexDirection = FlexDirection.Row;
                   SampleValidToggle = new Toggle() {value = SampleValid, text = "Do Sample Connection Validation"};
                   SampleValidToggle.tooltip = "Confirms samples are mutually visable, reduces performance but improves indirect shadow quality";
                   SampleValidToggle.RegisterValueChangedCallback(evt => {SampleValid = evt.newValue; RayMaster.LocalTTSettings.DoReSTIRGIConnectionValidation = SampleValid;});
                   TopGI.Add(SampleValidToggle);
               EnclosingGI.Add(TopGI);
               Box TemporalGI = new Box();
                   TemporalGI.style.flexDirection = FlexDirection.Row;
                   TemporalGIToggle = new Toggle() {value = GITemporal, text = "Enable Temporal"};
                   
                   Label TemporalGIMCapLabel = new Label("Temporal M Cap(0 is off)");
                   TemporalGIMCapLabel.tooltip = "Controls how long a sample is valid for, lower numbers update more quickly but have more noise, good for quickly changing scenes/lighting";
                   TemporalGIMCapField = new IntegerField() {value = GITemporalMCap};
                   TemporalGIToggle.RegisterValueChangedCallback(evt => {GITemporal = evt.newValue; RayMaster.LocalTTSettings.UseReSTIRGITemporal = GITemporal;});
                   TemporalGIMCapField.RegisterValueChangedCallback(evt => {TemporalGIMCapField.value = Mathf.Min(Mathf.Max(evt.newValue, 1), 60); GITemporalMCap = (int)TemporalGIMCapField.value; RayMaster.LocalTTSettings.ReSTIRGITemporalMCap = GITemporalMCap;});
                   TemporalGI.Add(TemporalGIToggle);
                   TemporalGI.Add(TemporalGIMCapField);
                   TemporalGI.Add(TemporalGIMCapLabel);
               EnclosingGI.Add(TemporalGI);
               Box SpatialGI = new Box();
                   SpatialGI.style.flexDirection = FlexDirection.Row;
                   SpatialGIToggle = new Toggle() {value = GISpatial, text = "Enable Spatial"};
                   SpatialGIToggle.RegisterValueChangedCallback(evt => {GISpatial = evt.newValue; RayMaster.LocalTTSettings.UseReSTIRGISpatial = GISpatial;});
                   SpatialGI.Add(SpatialGIToggle);
               EnclosingGI.Add(SpatialGI);
           GIFoldout.Add(EnclosingGI);
           MainSource.Add(GIToggle);
           GIToggle.RegisterValueChangedCallback(evt => {ReSTIRGI = evt.newValue; RayMaster.LocalTTSettings.UseReSTIRGI = ReSTIRGI;if(evt.newValue) MainSource.Insert(MainSource.IndexOf(GIToggle) + 1, GIFoldout); else MainSource.Remove(GIFoldout);});
           if(ReSTIRGI) MainSource.Add(GIFoldout);
      

         



            List<string> UpscalerSettings = new List<string>();
            UpscalerSettings.Add("Bilinear");
            UpscalerSettings.Add("GSR");
            UpscalerSettings.Add("TAAU");
            UpscalerField = new PopupField<string>("<b>Upscaler</b>");
            UpscalerField.ElementAt(0).style.minWidth = 55;
            UpscalerField.ElementAt(1).style.minWidth = 75;
            UpscalerField.ElementAt(1).style.maxWidth = 75;
            UpscalerField.style.width = 275;
            UpscalerField.choices = UpscalerSettings;
            UpscalerField.index = UpscalerMethod;
            UpscalerField.style.flexDirection = FlexDirection.Row;
            UpscalerField.RegisterValueChangedCallback(evt => {
               UpscalerMethod = UpscalerField.index;
               RayMaster.LocalTTSettings.UpscalerMethod = UpscalerMethod; 
            });
            if(RenderRes != 1.0f) MainSource.Add(UpscalerField);



            VisualElement PartialRenderingFoldout = new VisualElement() {};
               PartialRenderingFoldout.style.flexDirection = FlexDirection.Row;
               IntegerField PartialRenderingField = new IntegerField() {value = PartialRenderingFactor, label = "Partial Factor"};
               PartialRenderingField.ElementAt(0).style.minWidth = 65;
               PartialRenderingField.RegisterValueChangedCallback(evt => {PartialRenderingField.value = Mathf.Max(2, evt.newValue); PartialRenderingFactor = PartialRenderingField.value; RayMaster.LocalTTSettings.PartialRenderingFactor = PartialRenderingFactor;});
               PartialRenderingFoldout.Add(PartialRenderingField);
           DoPartialRenderingToggle = new Toggle() {value = DoPartialRendering, text = "Use Partial Rendering"};
            #if TTAdvancedSettings
              MainSource.Add(DoPartialRenderingToggle);
            #endif
           DoPartialRenderingToggle.RegisterValueChangedCallback(evt => {DoPartialRendering = evt.newValue; RayMaster.LocalTTSettings.DoPartialRendering = DoPartialRendering;if(evt.newValue) MainSource.Insert(MainSource.IndexOf(DoPartialRenderingToggle) + 1, PartialRenderingFoldout); else MainSource.Remove(PartialRenderingFoldout);});
           if(DoPartialRendering) MainSource.Add(PartialRenderingFoldout);

            VisualElement FireflyFoldout = new VisualElement() {};
            FireflyFoldout.style.minWidth = 300;
               IntegerField FireflyFrameCountField = new IntegerField() {value = FireflyFrameCount, label = "Frames Before Anti-Firefly"};
               FireflyFrameCountField.ElementAt(0).style.minWidth = 65;
               FireflyFrameCountField.RegisterValueChangedCallback(evt => {FireflyFrameCount = evt.newValue; RayMaster.LocalTTSettings.FireflyFrameCount = FireflyFrameCount;});
               FireflyFrameCountField.style.maxWidth = 345;
               FireflyFoldout.Add(FireflyFrameCountField);

               IntegerField FireflyFrameIntervalField = new IntegerField() {value = FireflyFrameInterval, label = "Anti-Firefly Frame Interval"};
               FireflyFrameIntervalField.ElementAt(0).style.minWidth = 65;
               FireflyFrameIntervalField.RegisterValueChangedCallback(evt => {FireflyFrameIntervalField.value = Mathf.Max(evt.newValue, 1); FireflyFrameInterval = Mathf.Max(evt.newValue, 1); RayMaster.LocalTTSettings.FireflyFrameInterval = FireflyFrameInterval;});
               FireflyFrameIntervalField.style.maxWidth = 345;
               FireflyFoldout.Add(FireflyFrameIntervalField);
         
               Slider FireflyStrengthSlider = new Slider() {label = "Anti Firefly Strength: ", value = FireflyStrength, highValue = 1.0f, lowValue = 0.0f};
               FireflyStrengthSlider.value = FireflyStrength;
               FireflyStrengthSlider.RegisterValueChangedCallback(evt => {FireflyStrength = evt.newValue; RayMaster.LocalTTSettings.FireflyStrength = FireflyStrength;});
               FireflyStrengthSlider.style.maxWidth = 345;
               FireflyFoldout.Add(FireflyStrengthSlider);

               FloatField FireflyOffsetField = new FloatField("Firefly Minimum Offset") {value = FireflyOffset};
               FireflyOffsetField.RegisterValueChangedCallback(evt => {FireflyOffset = (int)evt.newValue; RayMaster.LocalTTSettings.FireflyOffset = FireflyOffset;});
               FireflyOffsetField.style.maxWidth = 345;
               FireflyFoldout.Add(FireflyOffsetField);


           DoFireflyToggle = new Toggle() {value = DoFirefly, text = "Enable AntiFirefly"};
           MainSource.Add(DoFireflyToggle);
           DoFireflyToggle.RegisterValueChangedCallback(evt => {DoFirefly = evt.newValue; RayMaster.LocalTTSettings.DoFirefly = DoFirefly; if(evt.newValue) MainSource.Insert(MainSource.IndexOf(DoFireflyToggle) + 1, FireflyFoldout); else MainSource.Remove(FireflyFoldout);});
           if(DoFirefly) MainSource.Add(FireflyFoldout);

           Toggle ImprovedPrimaryHitToggle = new Toggle() {value = ImprovedPrimaryHit, text = "RR Ignores Primary Hit"};
           ImprovedPrimaryHitToggle.RegisterValueChangedCallback(evt => {ImprovedPrimaryHit = evt.newValue; RayMaster.LocalTTSettings.ImprovedPrimaryHit = ImprovedPrimaryHit;});
            #if TTAdvancedSettings
               MainSource.Add(ImprovedPrimaryHitToggle);
            #endif


           VisualElement AtmoBox = new VisualElement();
               AtmoBox.style.flexDirection = FlexDirection.Row;
               AtmoScatterField = new IntegerField("Atmospheric Scattering Samples") {value = AtmoScatter};
               AtmoScatterField.RegisterValueChangedCallback(evt => {AtmoScatterField.value = Mathf.Max(evt.newValue, 1); AtmoScatter = AtmoScatterField.value; RayMaster.AtmoNumLayers = AtmoScatter;});
               AtmoBox.Add(AtmoScatterField);
            #if TTAdvancedSettings
               MainSource.Add(AtmoBox);
            #endif


           Toggle SampleShowToggle = new Toggle() {value = ShowFPS, text = "Show Sample Count"};
           // SerializedObject so = new SerializedObject(RayMaster);
           VisualElement SampleCountBox = new VisualElement();
               SampleCountBox.style.flexDirection = FlexDirection.Row;
               SampleCountField = new IntegerField("Current Sample Count") {};
               // SampleCountField.Bind(so);
               SampleCountBox.Add(SampleCountField);
           MainSource.Add(SampleShowToggle);
           SampleShowToggle.RegisterValueChangedCallback(evt => {ShowFPS = evt.newValue; if(evt.newValue) MainSource.Insert(MainSource.IndexOf(SampleShowToggle) + 1, SampleCountBox); else MainSource.Remove(SampleCountBox);});
           if(ShowFPS) MainSource.Add(SampleCountBox);


            AddPostProcessingToMenu();
            Foldout PostProcessingFoldout = new Foldout() {text = "Post Processing"};
               PostProcessingFoldout.Add(PostProcessingMenu);
            MainSource.Add(PostProcessingFoldout);

           Rect WindowRect = MainSource.layout;
           Box EnclosingBox = new Box();
               try {
                  EnclosingBox.style.position = Position.Absolute;
               } finally {}
               EnclosingBox.style.top = 70;
               EnclosingBox.style.width = 110;
               EnclosingBox.style.height = 55;
               EnclosingBox.style.left = 200;
               Label RemainingObjectsLabel = new Label("Remaining Objects");
               // RemainingObjectsLabel.style.color = Color.white;
               RemainingObjectsField = new IntegerField() {};
               Box ReadyBox = new Box();
               ReadyBox.style.height = 18;
               ReadyBox.style.backgroundColor = Color.green;
               RemainingObjectsField.RegisterValueChangedCallback(evt => {if(evt.newValue == 0) {ReadyBox.style.backgroundColor = Color.green;} else ReadyBox.style.backgroundColor = Color.red;});
               Label ReadyLabel = new Label("All Objects Built");
               ReadyLabel.style.color = Color.black;
               ReadyBox.style.alignItems = Align.Center;
               ReadyBox.Add(ReadyLabel);
               EnclosingBox.Add(RemainingObjectsLabel);
               EnclosingBox.Add(RemainingObjectsField);
               EnclosingBox.Add(ReadyBox);
            MainSource.Add(EnclosingBox);

        }


        RayCastMaterialSelector TempTest;
        int AFrame = -1;
        int FramesSinceDOF = 0;
        string PrevLocTTSettingsName = "";
        void Update() {
            if(AFrame != -1) {
               RayTracingObjectEditor[] editors = (RayTracingObjectEditor[])Resources.FindObjectsOfTypeAll(typeof(RayTracingObjectEditor));
               if (editors.Length > 0) {
                  editors[0].SetSelected(AFrame);
               }
               AFrame = -1;
            }
            if(Application.isFocused && Input.GetMouseButton(2) && !Input.GetKey(KeyCode.LeftControl)) {
               if(Input.mousePosition.x >= 0 && Input.mousePosition.x < RayMaster.SourceWidth && Input.mousePosition.y >= 0 && Input.mousePosition.y < RayMaster.SourceHeight) {
                  if(TempTest == null) TempTest = new RayCastMaterialSelector();
                  AFrame = TempTest.CastRay(RayTracingMaster._camera, RayMaster.SourceWidth, RayMaster.SourceHeight);
               }
            }
            if(RayMaster != null) {
               if(Application.isPlaying && RayTracingMaster.RayMaster != null && RayTracingMaster.RayMaster.LocalTTSettings != null) {
                  if(!(PrevLocTTSettingsName.Equals(RayTracingMaster.RayMaster.LocalTTSettings.name))) {
                     rootVisualElement.Clear();
                     MainSource.Clear();
                     CreateGUI();
                  }
                  PrevLocTTSettingsName = RayTracingMaster.RayMaster.LocalTTSettings.name;
               }


                  // Debug.Log(Input.GetAxis("Mouse ScrollWheel"));
               if(RayMaster.LocalTTSettings.PPDoF && ((Input.GetAxis("Mouse ScrollWheel") != 0 && Input.GetKey(KeyCode.LeftControl)))) {
                  RayMaster.IsFocusingDelta = true;
                  RayMaster.LocalTTSettings.DoFAperature += Input.GetAxis("Mouse ScrollWheel") * 0.1f;
                  RayMaster.LocalTTSettings.DoFAperature = Mathf.Clamp(RayMaster.LocalTTSettings.DoFAperature, 0.0001f, 1);
                  DoFAperature = RayMaster.LocalTTSettings.DoFAperature;
                  AperatureSlider.value = RayMaster.LocalTTSettings.DoFAperature;
               } else {
                  RayMaster.IsFocusingDelta = false;
               }

               if(RayMaster.LocalTTSettings.PPDoF && (FramesSinceDOF < 3 || (Input.GetMouseButton(2) && Input.GetKey(KeyCode.LeftControl)))) {
                  FramesSinceDOF++;
                  if(Input.mousePosition.x >= 0 && Input.mousePosition.x < RayMaster.SourceWidth && Input.mousePosition.y >= 0 && Input.mousePosition.y < RayMaster.SourceHeight) {
                     if((Input.GetMouseButton(2) && Input.GetKey(KeyCode.LeftControl))) FramesSinceDOF = 0;
                     RayMaster.IsFocusing = true;
                     GetFocalLength();
                  } else {
                     RayMaster.IsFocusing = false;
                     FramesSinceDOF = 3;
                  }
               } else {
                  RayMaster.IsFocusing = false;
                  FramesSinceDOF = 3;
               }
            }
            if(!Application.isPlaying) {
               if(RayTracingMaster.DoCheck && DoSaving) {
                  try{
                      UnityEditor.AssetDatabase.Refresh();
                        using (var A = new StringReader(Resources.Load<TextAsset>("Utility/SaveFile").text)) {
                           var serializer = new XmlSerializer(typeof(RayObjs));
                           RayObjs rayreads = serializer.Deserialize(A) as RayObjs;
                           int RayReadCount = rayreads.RayObj.Count;
                           RayTracingObject TempRTO;
                           Dictionary<int, GameObject> m_instanceMap = new Dictionary<int, GameObject>();
                           //record instance map

                           m_instanceMap.Clear();
                           List<GameObject> gos = new List<GameObject>();
                           var Candidates = Resources.FindObjectsOfTypeAll(typeof(GameObject));
                           foreach (GameObject go in Candidates) {
                              if (gos.Contains(go))
                                 continue;
                              gos.Add(go);
                              m_instanceMap[go.GetInstanceID()] = go;
                           }
                           for(int i = 0; i < RayReadCount; i++) {
                              RayObjectDatas Ray = rayreads.RayObj[i];
                              if(m_instanceMap.ContainsKey(Ray.ID)) {
                                 if(m_instanceMap[Ray.ID].TryGetComponent(out TempRTO)) {
                                    int NameIndex = (new List<string>(TempRTO.Names)).IndexOf(Ray.MatName);
                                    if(NameIndex == -1) {
                                       Debug.Log("Missing material marked for update");
                                    } else {
                                       EditorUtility.SetDirty(TempRTO);
                                       TempRTO.LocalMaterials[NameIndex] = Ray.MatData;
                                       TempRTO.UseKelvin[NameIndex] = Ray.UseKelvin;
                                       TempRTO.KelvinTemp[NameIndex] = Ray.KelvinTemp;
                                       TempRTO.CallMaterialEdited();
                                    }
                                 }
                              }

                         }
                      }
                  } catch(System.Exception e) {HasNoMore = true;};
                  string saveFilePath = TTPathFinder.GetSaveFilePath();
                  using(StreamWriter writer = new StreamWriter(saveFilePath)) {
                      var serializer = new XmlSerializer(typeof(RayObjs));
                      serializer.Serialize(writer.BaseStream, new RayObjs());
                      UnityEditor.AssetDatabase.Refresh();
                  }
               }
               RayTracingMaster.DoCheck = false;
            }

            if(Assets != null && Instancer != null && RemainingObjectsField != null) RemainingObjectsField.value = Assets.RunningTasks + Instancer.RunningTasks;
            if(RayTracingMaster.RayMaster != null) SampleCountField.value = RayTracingMaster.SampleCount;
            
            if(Assets != null && Assets.NeedsToUpdateXML) {
               string materialMappingsPath = TTPathFinder.GetMaterialMappingsPath();
                using(StreamWriter writer = new StreamWriter(materialMappingsPath)) {
                  var serializer = new XmlSerializer(typeof(Materials));
                  serializer.Serialize(writer.BaseStream, AssetManager.data);
               }
               Assets.NeedsToUpdateXML = false;
            }
            Cleared = false;
        }

      }


    public class PopupWarningWindow : PopupWindowContent
    {
        public override Vector2 GetWindowSize()
        {
            return new Vector2(460, 50);
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.Label("This will Re-arrange your hierarchy and remove Static flags from objects", EditorStyles.boldLabel);
            if(GUILayout.Button("Proceed")) EditorWindow.GetWindow<EditModeFunctions>().ConfirmPopup();
        }
    }

public class DialogueNode : Node
{
    public string GUID;

    public string DialogueText;

    public bool EntryPoint = false;

    public int PropertyIndex;
}


public class DialogueGraphView : GraphView
{

    public DialogueGraphView() {
      graphViewChanged = OnGraphViewChanged;
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
    }

    private bool CheckIfSingleComp(string PortName) {
      return PortName.Contains("Mask") || PortName.Equals("Alpha Texture") || PortName.Equals("Metallic Texture") || PortName.Equals("Roughness Texture") || PortName.Equals("DiffTrans Texture");
    }
    private UnityEditor.Experimental.GraphView.GraphViewChange OnGraphViewChanged(UnityEditor.Experimental.GraphView.GraphViewChange graphViewChange) {
      if(graphViewChange.edgesToCreate != null)
      foreach(var A in graphViewChange.edgesToCreate) {
         if(A.output != null) {
            Port OutPort = A.output;
            if(OutPort.node != null) {
               Node OutNode = OutPort.node;
               if(OutNode.inputContainer.childCount >= 2) {
                  try {
                     Port InPort = A.input;
                     Node InNode = InPort.node;
                     if(CheckIfSingleComp(InPort.portName))
                        ((PopupField<string>)OutNode.inputContainer[2]).visible = true;
                     else
                        ((PopupField<string>)OutNode.inputContainer[2]).visible = false;
                  } catch (System.Exception E) {
                     Debug.LogError(E);
                  }

               }

            }
         }
         // if(OutNode.inputContainer.ElementAt(2) == typeof(PopupField)) 
         // Debug.Log(OutNode.inputContainer.ElementAt(2));
         // InNode.inputContainer[2].
      }
      return graphViewChange;
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapater) {
        var compatablePorts = new List<Port>();

        ports.ForEach(port => {
        if(startPort != port && startPort.node != port.node)
            compatablePorts.Add(port);

        });
        return compatablePorts;
    }
    public Port GeneratePort(DialogueNode node, Direction portDirection, System.Type T, Port.Capacity capacity = Port.Capacity.Single, string Name = "") {
        var NodePort = node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, T);
        NodePort.portName = Name;
        return NodePort;
    }
}  



}



#endif