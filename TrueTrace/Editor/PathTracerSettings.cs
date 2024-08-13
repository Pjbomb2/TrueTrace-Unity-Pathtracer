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

namespace TrueTrace {
   public class EditModeFunctions : EditorWindow {
        [MenuItem("TrueTrace/TrueTrace Settings")]
        public static void ShowWindow() {
            GetWindow<EditModeFunctions>("TrueTrace Settings");
        }

        public Toggle NEEToggle;
        public Toggle DingToggle;
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
        private AudioClip DingNoise;
         [SerializeField] public Camera SelectedCamera;
         [SerializeField] public bool ClayMode = false;
         [SerializeField] public Vector3 ClayColor = new Vector3(0.5f,0.5f,0.5f);
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
         [SerializeField] public int UpdateRate = 7;
         [SerializeField] public bool GITemporal = true;
         [SerializeField] public int GITemporalMCap = 12;
         [SerializeField] public bool GISpatial = true;
         [SerializeField] public int GISpatialSampleCount = 24;
         [SerializeField] public bool TAA = false;
         [SerializeField] public bool ToneMap = false;
         [SerializeField] public int ToneMapIndex = 0;
         [SerializeField] public bool TAAU = true;
         [SerializeField] public int AtmoScatter = 4;
         [SerializeField] public bool ShowFPS = true;
         [SerializeField] public float Exposure = 0;
         [SerializeField] public int AtlasSize = 16384;
         [SerializeField] public bool DoPartialRendering = false;
         [SerializeField] public int PartialRenderingFactor = 1;
         [SerializeField] public bool DoFirefly = false;
         [SerializeField] public bool ImprovedPrimaryHit = false;
         [SerializeField] public float ReSTIRGISpatialRadius = 50;
         [SerializeField] public int RISCount = 5;
         [SerializeField] public int DenoiserSelection = 0;
         [SerializeField] public Color SceneBackgroundColor = new Color(1,1,1,1);
         [SerializeField] public Color SecondarySceneBackgroundColor = new Color(1,1,1,1);
         [SerializeField] public Vector2 BackgroundIntensity = Vector2.one;
         [SerializeField] public float LightEnergyScale = 1;
         [SerializeField] public float LEMEnergyScale = 1;
         [SerializeField] public float IndirectBoost = 1;
         [SerializeField] public int BackgroundType = 0;
         [SerializeField] public int SecondaryBackgroundType = 0;
         [SerializeField] public bool DoDing = true;
         [SerializeField] public bool DoSaving = true;
         [SerializeField] public float SunDesaturate = 0;
         [SerializeField] public float SkyDesaturate = 0;
         [SerializeField] public int FireflyFrameCount = 0;
         [SerializeField] public float FireflyStrength = 1.0f;
         [SerializeField] public float FireflyOffset = 0;
         [SerializeField] public int OIDNFrameCount = 0;
         [SerializeField] public bool UseOIDN = false;
         [SerializeField] public bool DoSharpen = false;
         [SerializeField] public float Sharpness = 1.0f;
         [SerializeField] public Vector2 HDRILongLat = Vector2.zero;
         [SerializeField] public Vector2 HDRIScale = Vector2.one;


         public void SetGlobalDefines(string DefineToSet, bool SetValue) {
            if(File.Exists(Application.dataPath + "/TrueTrace/Resources/GlobalDefines.cginc")) {
               string[] GlobalDefines = System.IO.File.ReadAllLines(Application.dataPath + "/TrueTrace/Resources/GlobalDefines.cginc");
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
               GlobalDefines[Index] = GlobalDefines[Index].Replace("// ", "");
               if(!SetValue) GlobalDefines[Index] = "// " + GlobalDefines[Index];

               System.IO.File.WriteAllLines(Application.dataPath + "/TrueTrace/Resources/GlobalDefines.cginc", GlobalDefines);
               AssetDatabase.Refresh();
            } else {Debug.Log("No GlobalDefinesFile");}
         }


         void OnEnable() {
            EditorSceneManager.activeSceneChangedInEditMode += EvaluateScene;
            EditorSceneManager.sceneSaving += SaveScene;
            EditorSceneManager.activeSceneChanged += ChangedActiveScene;
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
            GameObject.Find("Scene").GetComponent<AssetManager>().EditorBuild();
         }


         List<Transform> ChildObjects;
         private void GrabChildren(Transform Parent) {
            ChildObjects.Add(Parent);
            int ChildCount = Parent.childCount;
            for(int i = 0; i < ChildCount; i++) {
               if(Parent.GetChild(i).gameObject.activeInHierarchy) GrabChildren(Parent.GetChild(i));
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
                     DestroyImmediate(Objects[i][i2].GetComponent<RayTracingObject>());
                     if(InstancedParent.GetComponent<ParentObject>()) DestroyImmediate(Objects[i][i2].GetComponent<ParentObject>());
                     Objects[i][i2].AddComponent<InstancedObject>();
                     Objects[i][i2].GetComponent<InstancedObject>().InstanceParent = InstancedParent.GetComponent<ParentObject>();
                  }
               }
            }
         }

         private void OptimizeForStatic() {
            GameObject[] AllObjects = GameObject.FindObjectsOfType<GameObject>();//("Untagged");
            foreach(GameObject obj in AllObjects) {
               
               if(PrefabUtility.IsAnyPrefabInstanceRoot(obj)) PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
            foreach(GameObject obj in AllObjects) {
               
               if(obj.name.Contains("LOD1") || obj.name.Contains("LOD2")) DestroyImmediate(obj);
            }

            ChildObjects = new List<Transform>();
            Transform Source = GameObject.Find("Scene").transform;
            if(GameObject.Find("Terrain") != null) GameObject.Find("Terrain").transform.parent = Source;
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
                        if(MeshRend.sharedMaterials != null && MeshRend.sharedMaterials.Length != 0)
                          Parent.This.gameObject.AddComponent<RayTracingObject>();
                     } else if(Parent.This.gameObject.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer SkinRend)) {
                        if(SkinRend.sharedMaterials != null && SkinRend.sharedMaterials.Length != 0)
                           Parent.This.gameObject.AddComponent<RayTracingObject>();
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
                     Parent.This.gameObject.AddComponent<ParentObject>();
                  }
               }
               else {
                  for(int i = 0; i < ChildLength; i++) {
                     if(Parent.Children[i].This.gameObject.TryGetComponent<RayTracingObject>(out RayTracingObject TempObj2) && !Parent.Children[i].This.gameObject.TryGetComponent<ParentObject>(out ParentObject TempParent2)) Parent.Children[i].This.gameObject.AddComponent<ParentObject>();
                  }               
               }
            } else {
               for(int i = 0; i < ChildLength; i++) {
                  if(Parent.Children[i].This.gameObject.TryGetComponent<RayTracingObject>(out RayTracingObject TempObj2) && !Parent.Children[i].This.gameObject.TryGetComponent<ParentObject>(out ParentObject TempParent3) && !Parent.This.gameObject.TryGetComponent<ParentObject>(out ParentObject TempParent2)) Parent.This.gameObject.AddComponent<ParentObject>();
               }
            }
            if(HasNormalMeshAsChild && HasSkinnedMeshAsChild) {
               for(int i = 0; i < ChildLength; i++) {
                  if(!Parent.Children[i].This.gameObject.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer TempSkin3) && !Parent.Children[i].This.gameObject.TryGetComponent<ParentObject>(out ParentObject TempParent3)) {
                     Parent.Children[i].This.gameObject.AddComponent<ParentObject>();
                  }
               }  
            }


         }
         // public struct FlagObjects {
         //    public List<FlagObjects> Children;
         //    public GameObject Obj;
         //    public bool HasSkinnedChild;
         //    public bool HasSkinnedSelf;
         //    public bool HasNormChild;
         //    public bool HasNormSelf;
         //    public bool HasPO;
         //    public bool HasRTO;
         //    public bool ChainedImportance;
         //    public bool AlreadyHandled;
         //    public bool IsEmpty;
         // }
         // public List<FlagObjects> Hierarchy;       

         // FlagObjects Prepare(Transform Source) {
         //    FlagObjects SourceObj = new FlagObjects();
         //    SourceObj.Children = new List<FlagObjects>();
         //    SourceObj.Obj = Source.gameObject;
         //    if(Source.gameObject.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer TempSkin)) {
         //       SourceObj.HasSkinnedSelf = true;
         //       SourceObj.ChainedImportance = true;
         //    }
         //    if(Source.gameObject.TryGetComponent<MeshRenderer>(out MeshRenderer TempRend)) {
         //       SourceObj.HasNormSelf = true;
         //       SourceObj.ChainedImportance = true;
         //    }
         //    SourceObj.IsEmpty = !Source.ChainedImportance;

         //    if(Source.gameObject.TryGetComponent<RayTracingObject>(out RayTracingObject TempRTO)) SourceObj.HasRTO = true;
         //    if(Source.gameObject.TryGetComponent<ParentObject>(out ParentObject TempPO)) SourceObj.HasPO = true;

         //    int TransformCount = Source.transform.childCount;
         //    for(int i = 0; i < TransformCount; i++) {
         //       if(Source.GetChild(i).gameObject.activeInHierarchy) {
         //          FlagObjects TempFlag = Prepare(Source.GetChild(i));
         //          if(TempFlag.HasSkinnedSelf) SourceObj.HasSkinnedChild = true;
         //          if(TempFlag.HasNormSelf) SourceObj.HasNormChild = true;
         //          if(TempFlag.ChainedImportance) SourceObj.ChainedImportance = true;
         //          SourceObj.Children.Add(TempFlag);
         //       }
         //    }
         //    return SourceObj;
         // }
         // void Prune(ref FlagObjects Source) {
         //    if(Source.Children == null) return;
         //    int ChildCount = Source.Children.Count;
         //    for(int i = ChildCount - 1; i >= 0; i--) {
         //       if(Source.Children[i].ChainedImportance) Prune(ref Source.Children[i]);
         //       else Source.Children.RemoveAt(i);
         //    }
         // }

         // int Check(ref FlagObjects Source, FlagObjects Child) {
         //    if(!(Child.HasPO && (Child.HasRTO || Child.HasSkinnedChild || Child.HasNormChild))) Source.ChildNeedsHandling = true;  
         // }
         // void Solve(ref FlagObjects Source) {
         //    int ChildCount = Source.Children.Count;
         //    for(int i = 0; i < ChildCount; i++) {
         //       Solve(ref Source.Children[i]);
         //    }

         //    if(Source.HasNormSelf || Source.HasSkinnedSelf) {//check for material count not zero and mesh exists?
         //       if(!Source.HasRTO) {
         //          Source.Obj.AddComponent<RayTracingObject>();
         //          Source.HasRTO = true;
         //       }
         //    }

         //    bool ChildrenSafe = true;
         //    bool UnhandledContainsSkinned = false;
         //    bool UnhandledContainsNormal = false;
         //    List<int> UnhandledChildrenIndexes = new List<int>();
         //    for(int i = 0; i < ChildCount && ChildrenSafe; i++) {
         //       if(!Source.Children[i].AlreadyHandled) {
         //          UnhandledChildrenIndexes.Add(i); 
         //          UnhandledContainsSkinned = UnhandledContainsSkinned || Source.Children[i].HasSkinnedSelf;
         //          UnhandledContainsNormal = UnhandledContainsNormal || Source.Children[i].HasNormSelf;
         //       }
         //    }
         //    int UnhandledCount = UnhandledChildrenIndexes.Count;
         //    if(!Source.AlreadyHandled) {
         //       if(UnhandledContainsSkinned && !Source.HasNormSelf) {
         //          Source.Obj.AddComponent<ParentObject>();
         //          Source.AlreadyHandled = true;     
         //          for(int i = 0; i < UnhandledCount; i++) {
         //             int Index = UnhandledChildrenIndexes[i];
         //             if(Source.Children[Index].HasSkinnedSelf) {
         //                Source.Children[Index].AlreadyHandled = true;
         //             } else if(Source.Children[Index].HasNormSelf) {
         //                Source.Children[Index].Obj.AddComponent<ParentObject>();
         //                Source.Children[Index].AlreadyHandled = true;
         //             }
         //          }
         //       } else if(UnhandledContainsSkinned && Source.HasNormSelf) {

         //       }
         //    }


         private void QuickStart() {
            // RayTracingObject[] TempObjects = GameObject.FindObjectsOfType<RayTracingObject>();
            // foreach(var a in TempObjects) {
            //    DestroyImmediate(a);
            // }         
            // ParentObject[] TempObjects2 = GameObject.FindObjectsOfType<ParentObject>();
            // foreach(var a in TempObjects2) {
            //    DestroyImmediate(a);
            // }

            var LightObjects = GameObject.FindObjectsOfType<Light>(true);
            foreach(var LightObj in LightObjects) {
               if(LightObj.gameObject.GetComponent<RayTracingLights>() == null) LightObj.gameObject.AddComponent<RayTracingLights>(); 
            }

            // FlagsObjects RootFlag = Prepare(Assets.transform);
            // Prune(ref RootFlag);

            ParentData SourceParent = GrabChildren2(Assets.transform);

            SolveChildren(SourceParent);


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
                  foreach(GameObject Obj in Objects) {
                     if(Obj.GetComponent<Camera>() == null && !Obj.name.Equals("InstancedStorage")) {
                        Obj.transform.SetParent(SceneObject.transform);
                     }
                  }
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
Toggle TAAUToggle;
FloatField AtmoScatterField;
Toggle GIToggle;
FloatField GIUpdateRateField;
FloatField TeporalGIMCapField;
FloatField ReSTIRGISpatialRadiusField;
Toggle TemporalGIToggle;
Toggle SpatialGIToggle;
Toggle TAAToggle;
Toggle DoPartialRenderingToggle;
Toggle DoFireflyToggle;
Toggle SampleValidToggle;
Toggle IndirectClampingToggle;
FloatField RISCountField;
IntegerField OIDNFrameField;
FloatField FocalSlider;


private void StandardSet() {
         BounceCount = 7;
         RenderRes = 1;
         NEE = true;
         Accumulate = true;
         RR = true;
         Moving = true;
         MeshSkin = true;
         Bloom = false;
         BloomStrength = 0.5f;
         DoF = false;
         DoFAperature = 0.1f;
         DoFAperatureScale = 1.0f;
         DoFFocal = 0.1f;
         DoExposure = false;
         ExposureAuto = false;
         ReSTIRGI = false;
         SampleValid = false;
         UpdateRate = 7;
         GITemporal = true;
         GITemporalMCap = 488;
         GISpatial = true;
         GISpatialSampleCount = 12;
         TAA = false;
         ToneMap = false;
         TAAU = true;
         AtmoScatter = 4;
         ShowFPS = true;
         Exposure = 0;
         AtlasSize = 16384;
         DoPartialRendering = false;
         DoFirefly = false;
         ReSTIRGISpatialRadius = 30;
         RISCount = 12;
}



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

         rootVisualElement.Remove(RearrangeElement);
         CreateGUI(); 
         rootVisualElement.Add(MainSource); 
         Assets.UpdateMaterialDefinition();
      }

      VisualElement HardSettingsMenu;
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

         InputHDRIField = new ObjectField();
         InputHDRIField.objectType = typeof(Texture);
         InputHDRIField.label = "Drag your skybox here ->";
         InputHDRIField.value = RayMaster.SkyboxTexture;
         InputHDRIField.RegisterValueChangedCallback(evt => {RayMaster.SkyboxTexture = evt.newValue as Texture; SecondaryInputHDRIField.value = evt.newValue;});
         SecondaryInputHDRIField = new ObjectField();
         SecondaryInputHDRIField.objectType = typeof(Texture);
         SecondaryInputHDRIField.label = "Drag your skybox here ->";
         SecondaryInputHDRIField.value = RayMaster.SkyboxTexture;
         SecondaryInputHDRIField.RegisterValueChangedCallback(evt => {RayMaster.SkyboxTexture = evt.newValue as Texture; InputHDRIField.value = evt.newValue;});
         BackgroundColorField = new ColorField();
         BackgroundColorField.value = SceneBackgroundColor;
         BackgroundColorField.style.width = 150;
         BackgroundColorField.RegisterValueChangedCallback(evt => {SceneBackgroundColor = evt.newValue; RayMaster.LocalTTSettings.SceneBackgroundColor = new Vector3(SceneBackgroundColor.r,SceneBackgroundColor.g,SceneBackgroundColor.b);});
         SecondaryBackgroundColorField = new ColorField();
         SecondaryBackgroundColorField.value = SecondarySceneBackgroundColor;
         SecondaryBackgroundColorField.style.width = 150;
         SecondaryBackgroundColorField.RegisterValueChangedCallback(evt => {SecondarySceneBackgroundColor = evt.newValue; RayMaster.LocalTTSettings.SecondarySceneBackgroundColor = new Vector3(SecondarySceneBackgroundColor.r,SecondarySceneBackgroundColor.g,SecondarySceneBackgroundColor.b);});

         BackgroundSettingsField = new PopupField<string>("Background Type");
         BackgroundSettingsField.choices = BackgroundSettings;
         BackgroundSettingsField.index = BackgroundType;
         BackgroundSettingsField.style.width = 550;
         switch(BackgroundSettingsField.index) {
            case 0:
               BackgroundSettingsField.Add(BlankElement);
            break;
            case 1:
               BackgroundSettingsField.Add(InputHDRIField);
            break;
            case 2:
               BackgroundSettingsField.Add(BackgroundColorField);
            break;
         }

         SceneSettingsMenu.Add(BackgroundSettingsField);
         BackgroundSettingsField.RegisterValueChangedCallback(evt => {
            int Prev = RayMaster.LocalTTSettings.BackgroundType;
            BackgroundType = BackgroundSettingsField.index;
            RayMaster.LocalTTSettings.BackgroundType = BackgroundType;
            switch(BackgroundSettingsField.index) {
               case 0:
                  BackgroundSettingsField.Add(BlankElement);
               break;
               case 1:
                  BackgroundSettingsField.Add(InputHDRIField);
               break;
               case 2:
                  BackgroundSettingsField.Add(BackgroundColorField);
               break;
            }
            if(Prev != RayMaster.LocalTTSettings.BackgroundType) BackgroundSettingsField.RemoveAt(2);

            });

         SecondaryBackgroundSettingsField = new PopupField<string>("Secondary Background Type");
         SecondaryBackgroundSettingsField.choices = BackgroundSettings;
         SecondaryBackgroundSettingsField.index = SecondaryBackgroundType;
         SecondaryBackgroundSettingsField.style.width = 550;
         switch(SecondaryBackgroundSettingsField.index) {
            case 0:
               SecondaryBackgroundSettingsField.Add(SecondaryBlankElement);
            break;
            case 1:
               SecondaryBackgroundSettingsField.Add(SecondaryInputHDRIField);
            break;
            case 2:
               SecondaryBackgroundSettingsField.Add(SecondaryBackgroundColorField);
            break;
         }

         SceneSettingsMenu.Add(SecondaryBackgroundSettingsField);
         SecondaryBackgroundSettingsField.RegisterValueChangedCallback(evt => {
            int Prev2 = RayMaster.LocalTTSettings.SecondaryBackgroundType;
            SecondaryBackgroundType = SecondaryBackgroundSettingsField.index;
            RayMaster.LocalTTSettings.SecondaryBackgroundType = SecondaryBackgroundType;
            switch(SecondaryBackgroundSettingsField.index) {
               case 0:
                  SecondaryBackgroundSettingsField.Add(SecondaryBlankElement);
               break;
               case 1:
                  SecondaryBackgroundSettingsField.Add(SecondaryInputHDRIField);
               break;
               case 2:
                  SecondaryBackgroundSettingsField.Add(SecondaryBackgroundColorField);
               break;
            }
            if(Prev2 != RayMaster.LocalTTSettings.SecondaryBackgroundType) SecondaryBackgroundSettingsField.RemoveAt(2);

            });
      


      BackgroundIntensityField = new FloatField() {value = BackgroundIntensity.x, label = "Primary Background Intensity"};
      BackgroundIntensityField.RegisterValueChangedCallback(evt => {BackgroundIntensity = new Vector2(evt.newValue, BackgroundIntensity.y); RayMaster.LocalTTSettings.BackgroundIntensity = BackgroundIntensity;});
      BackgroundIntensityField.style.maxWidth = 345;
      SceneSettingsMenu.Add(BackgroundIntensityField);

      FloatField SecondaryBackgroundIntensityField = new FloatField() {value = BackgroundIntensity.y, label = "Secondary Background Intensity"};
      SecondaryBackgroundIntensityField.RegisterValueChangedCallback(evt => {BackgroundIntensity = new Vector2(BackgroundIntensity.x, evt.newValue); RayMaster.LocalTTSettings.BackgroundIntensity = BackgroundIntensity;});
      SecondaryBackgroundIntensityField.style.maxWidth = 345;
      SceneSettingsMenu.Add(SecondaryBackgroundIntensityField);

      UnityLightModifierField = new FloatField() {value = LightEnergyScale, label = "Unity Light Intensity Modifier"};
      UnityLightModifierField.RegisterValueChangedCallback(evt => {LightEnergyScale = evt.newValue; Assets.LightEnergyScale = LightEnergyScale;});
      UnityLightModifierField.style.maxWidth = 345;
      SceneSettingsMenu.Add(UnityLightModifierField);

      FloatField LEMLightModifierField = new FloatField() {value = LEMEnergyScale, label = "LEM Light Intensity Modifier"};
      LEMLightModifierField.RegisterValueChangedCallback(evt => {LEMEnergyScale = evt.newValue; RayMaster.LocalTTSettings.LEMEnergyScale = LEMEnergyScale;});
      LEMLightModifierField.style.maxWidth = 345;
      SceneSettingsMenu.Add(LEMLightModifierField);

      IndirectBoostField = new FloatField() {value = IndirectBoost, label = "Indirect Lighting Boost"};
      IndirectBoostField.RegisterValueChangedCallback(evt => {IndirectBoost = evt.newValue; RayMaster.LocalTTSettings.IndirectBoost = IndirectBoost;});
      IndirectBoostField.style.maxWidth = 345;
      SceneSettingsMenu.Add(IndirectBoostField);

      Slider SunDesatSlider = new Slider() {label = "SunDesat: ", value = SunDesaturate, highValue = 1.0f, lowValue = 0.0f};
      SunDesatSlider.RegisterValueChangedCallback(evt => {SunDesaturate = evt.newValue; RayMaster.LocalTTSettings.SunDesaturate = SunDesaturate;});
      SceneSettingsMenu.Add(SunDesatSlider);
      SunDesatSlider.style.maxWidth = 345;

      Slider SkyDesatSlider = new Slider() {label = "SkyDesat: ", value = SkyDesaturate, highValue = 1.0f, lowValue = 0.0f};
      SkyDesatSlider.RegisterValueChangedCallback(evt => {SkyDesaturate = evt.newValue; RayMaster.LocalTTSettings.SkyDesaturate = SkyDesaturate;});
      SceneSettingsMenu.Add(SkyDesatSlider);
      SkyDesatSlider.style.maxWidth = 345;




      VisualElement HDRILongElement = new VisualElement();
         HDRILongElement.style.flexDirection = FlexDirection.Row;
         Slider HDRILongSlider = new Slider() {label = "HDRI Horizontal Offset: ", value = HDRILongLat.x, highValue = 360.0f, lowValue = 0.0f};
         HDRILongSlider.style.minWidth = 345;
         HDRILongSlider.style.maxWidth = 345;
         FloatField HDRILongField = new FloatField() {value = HDRILongLat.x};
         HDRILongField.style.maxWidth = 345;
         HDRILongElement.Add(HDRILongSlider);
         HDRILongElement.Add(HDRILongField);
      HDRILongSlider.RegisterValueChangedCallback(evt => {HDRILongLat = new Vector2(evt.newValue, HDRILongLat.y); HDRILongField.value = HDRILongLat.x; RayMaster.LocalTTSettings.HDRILongLat = HDRILongLat;});
      HDRILongField.RegisterValueChangedCallback(evt => {HDRILongLat = new Vector2(evt.newValue, HDRILongLat.y); HDRILongSlider.value = HDRILongLat.x; RayMaster.LocalTTSettings.HDRILongLat = HDRILongLat;});
      SceneSettingsMenu.Add(HDRILongElement);


      VisualElement HDRILatElement = new VisualElement();
         HDRILatElement.style.flexDirection = FlexDirection.Row;
         Slider HDRILatSlider = new Slider() {label = "HDRI Vertical Offset: ", value = HDRILongLat.y, highValue = 360.0f, lowValue = 0.0f};
         HDRILatSlider.style.minWidth = 345;
         HDRILatSlider.style.maxWidth = 345;
         FloatField HDRILatField = new FloatField() {value = HDRILongLat.y};
         HDRILatField.style.maxWidth = 345;
         HDRILatElement.Add(HDRILatSlider);
         HDRILatElement.Add(HDRILatField);
      HDRILatSlider.RegisterValueChangedCallback(evt => {HDRILongLat = new Vector2(HDRILongLat.x, evt.newValue); HDRILatField.value = HDRILongLat.y; RayMaster.LocalTTSettings.HDRILongLat = HDRILongLat;});
      HDRILatField.RegisterValueChangedCallback(evt => {HDRILongLat = new Vector2(HDRILongLat.x, evt.newValue); HDRILatSlider.value = HDRILongLat.y; RayMaster.LocalTTSettings.HDRILongLat = HDRILongLat;});
      SceneSettingsMenu.Add(HDRILatElement);

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
      SceneSettingsMenu.Add(HDRIScaleElement);



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
                                       MatCapMask
                                       };

      VisualElement MaterialPairingMenu;
      ObjectField InputMaterialField;
      PopupField<string> BaseColorField;
      PopupField<string> BaseColorTextureField;
      PopupField<string> NormalTextureField;
      PopupField<string> EmissionTextureField;
      PopupField<string> MetallicRangeField;
      PopupField<string> MetallicTextureField;
      PopupField<string> MetallicChannelField;
      PopupField<string> RoughnessRangeField;
      PopupField<string> RoughnessTextureField;
      PopupField<string> RoughnessChannelField;
      PopupField<string> MetallicRemapMinField;
      PopupField<string> MetallicRemapMaxField;
      PopupField<string> RoughnessRemapMinField;
      PopupField<string> RoughnessRemapMaxField;
      Toggle GlassToggle;
      Toggle CutoutToggle;
      Toggle SmoothnessToggle;
      MaterialShader MatShader;
      int Index;
      List<string> TextureProperties;
      List<string> ColorProperties;
      List<string> FloatProperties;
      List<string> RangeProperties;
      DialogueNode OutputNode;
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
            switch((int)AvailableIndexes[i].PropertyIndex) {
               case((int)Properties.AlbedoColor):
                  MatShader.BaseColorValue = ColorProperties[VerboseColorProperties.IndexOf(AvailableIndexes[i].title)];
               break;
               case((int)Properties.AlbedoTexture):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.Albedo,
                     ReadIndex = -4,
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)]
                  });  
               break;
               case((int)Properties.NormalTexture):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.Normal,
                     ReadIndex = -3,
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)]
                  });  
               break;
               case((int)Properties.EmissionTexture):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.Emission,
                     ReadIndex = -4,
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)]
                  });  
               break;
               case((int)Properties.MetallicTexture):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.Metallic,
                     ReadIndex = ChannelProperties.IndexOf(AvailableIndexes[i].GUID),
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)]
                  });  
               break;
               case((int)Properties.MetallicSlider):
                  MatShader.MetallicRange = RangeProperties[VerboseRangeProperties.IndexOf(AvailableIndexes[i].title)];
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
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)]
                  });  
               break;
               case((int)Properties.RoughnessSlider):
                  MatShader.RoughnessRange = RangeProperties[VerboseRangeProperties.IndexOf(AvailableIndexes[i].title)];
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
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)]
                  });  
               break;
               case((int)Properties.MatCapTexture):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.MatCapTex,
                     ReadIndex = -4,
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)]
                  });  
               break;
               case((int)Properties.MatCapMask):
                  MatShader.AvailableTextures.Add(new TexturePairs() {
                     Purpose = (int)TexturePurpose.MatCapMask,
                     ReadIndex = ChannelProperties.IndexOf(AvailableIndexes[i].GUID),
                     TextureName = TextureProperties[VerboseTextureProperties.IndexOf(AvailableIndexes[i].title)]
                  });  
               break;
            }
         }


      

         MatShader.IsGlass = GlassToggle.value;
         MatShader.IsCutout = CutoutToggle.value;
         MatShader.UsesSmoothness = SmoothnessToggle.value;
         AssetManager.data.Material[Index] = MatShader;
         using(StreamWriter writer = new StreamWriter(Application.dataPath + "/TrueTrace/Resources/Utility/MaterialMappings.xml")) {
            var serializer = new XmlSerializer(typeof(Materials));
            serializer.Serialize(writer.BaseStream, AssetManager.data);
            AssetDatabase.Refresh();
            
               Assets.UpdateMaterialDefinition();
         }
      }
      List<string> ChannelProperties;
      List<string> VerboseRangeProperties;
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
               if(InputElement != -1) {
                  PopupField<string> ChannelField = new PopupField<string>("Read Channel");
                  ChannelField.choices = ChannelProperties;               
                  ChannelField.index = InputElement;
                  dialogueNode.GUID = ChannelProperties[ChannelField.index];
                  dialogueNode.inputContainer.Add(ChannelField);
                  ChannelField.RegisterValueChangedCallback(evt => {dialogueNode.GUID = ChannelProperties[ChannelField.index];});
               }
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
           }else if(T == typeof(float)) {
               if(!IsRange) {
                  DropField.choices = RangeProperties;
                  DropField.index = (int)Mathf.Max(RangeProperties.IndexOf(InitialValue),0);
                  dialogueNode.title = VerboseRangeProperties[DropField.index];
                  DropField.RegisterValueChangedCallback(evt => {dialogueNode.title = VerboseRangeProperties[DropField.index];});
               } else {
                  DropField.choices = FloatProperties;
                  DropField.index = (int)Mathf.Max(FloatProperties.IndexOf(InitialValue),0);
                  dialogueNode.title = VerboseFloatProperties[DropField.index];
                  DropField.RegisterValueChangedCallback(evt => {dialogueNode.title = VerboseFloatProperties[DropField.index];});
               }
           }

           dialogueNode.inputContainer.Add(DropField);
           dialogueNode.RefreshExpandedState();
           dialogueNode.RefreshPorts();
           dialogueNode.SetPosition(new Rect(Pos, new Vector2(50, 100)));

           return dialogueNode;
      }



      private DialogueGraphView _graphView;
      void AddAssetsToMenu() {


         Shader shader = (InputMaterialField.value as Material).shader;
         RangeProperties = new List<string>();
         FloatProperties = new List<string>();
         ColorProperties = new List<string>();
         TextureProperties = new List<string>();
         ChannelProperties = new List<string>();
         VerboseRangeProperties = new List<string>();
         VerboseFloatProperties = new List<string>();
         VerboseColorProperties = new List<string>();
         VerboseTextureProperties = new List<string>();
         int PropCount = shader.GetPropertyCount();
         ColorProperties.Add("null");
         RangeProperties.Add("null");
         FloatProperties.Add("null");
         TextureProperties.Add("null");
         ChannelProperties.Add("R");
         ChannelProperties.Add("G");
         ChannelProperties.Add("B");
         ChannelProperties.Add("A");

         VerboseColorProperties.Add("null");
         VerboseRangeProperties.Add("null");
         VerboseFloatProperties.Add("null");
         VerboseTextureProperties.Add("null");
         for(int i = 0; i < PropCount; i++) {
            if(shader.GetPropertyType(i) == ShaderPropertyType.Texture) {TextureProperties.Add(shader.GetPropertyName(i)); VerboseTextureProperties.Add(shader.GetPropertyDescription(i));}
            if(shader.GetPropertyType(i) == ShaderPropertyType.Color) {ColorProperties.Add(shader.GetPropertyName(i)); VerboseColorProperties.Add(shader.GetPropertyDescription(i));}
            if(shader.GetPropertyType(i) == ShaderPropertyType.Range) {RangeProperties.Add(shader.GetPropertyName(i)); VerboseRangeProperties.Add(shader.GetPropertyDescription(i));}
            if(shader.GetPropertyType(i) == ShaderPropertyType.Float) {FloatProperties.Add(shader.GetPropertyName(i)); VerboseFloatProperties.Add(shader.GetPropertyDescription(i));}
         }
         MatShader = AssetManager.data.Material.Find((s1) => s1.Name.Equals(shader.name));
         Index = AssetManager.data.Material.IndexOf(MatShader);
         if(Index == -1) {
            if(Assets != null && Assets.NeedsToUpdateXML) {
               Assets.AddMaterial(shader);
               using(StreamWriter writer = new StreamWriter(Application.dataPath + "/TrueTrace/Resources/Utility/MaterialMappings.xml")) {
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
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "Metallic Texture(Single Component)"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(float), Port.Capacity.Single, "Metallic Range"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(float), Port.Capacity.Single, "Metallic Min"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(float), Port.Capacity.Single, "Metallic Max"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "Roughness Texture(Single Component)"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(float), Port.Capacity.Single, "Roughness Range"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(float), Port.Capacity.Single, "Roughness Min"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(float), Port.Capacity.Single, "Roughness Max"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "Alpha Texture(Single Component)"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "MatCap Texture"));
         OutputNode.inputContainer.Add(_graphView.GeneratePort(OutputNode, Direction.Input, typeof(Texture), Port.Capacity.Single, "MatCap Mask(Single Component)"));

         _graphView.AddElement(OutputNode);
         Vector2 Pos = new Vector2(30, 10);
         List<int> AvailableTexturesPurposes = new List<int>();
         for(int i = 0; i < MatShader.AvailableTextures.Count; i++) {
            AvailableTexturesPurposes.Add(MatShader.AvailableTextures[i].Purpose);
         }
         for(int i = 0; i < MatShader.AvailableTextures.Count; i++) {
            DialogueNode ThisNode = new DialogueNode();
            Edge ThisEdge = new Edge();
            switch((int)MatShader.AvailableTextures[i].Purpose) {
               case((int)TexturePurpose.MatCapMask):
                  Pos.y = 1140;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, MatShader.AvailableTextures[i].TextureName, MatShader.AvailableTextures[i].ReadIndex);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.MatCapMask] as Port);
               break;
               case((int)TexturePurpose.MatCapTex):
                  Pos.y = 1060;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, MatShader.AvailableTextures[i].TextureName);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.MatCapTexture] as Port);
               break;
               case((int)TexturePurpose.Alpha):
                  Pos.y = 980;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, MatShader.AvailableTextures[i].TextureName, MatShader.AvailableTextures[i].ReadIndex);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.AlphaTexture] as Port);
               break;
               case((int)TexturePurpose.Metallic):
                  Pos.y = 340;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, MatShader.AvailableTextures[i].TextureName, MatShader.AvailableTextures[i].ReadIndex);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.MetallicTexture] as Port);
               break;
               case((int)TexturePurpose.Roughness):
                  Pos.y = 660;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, MatShader.AvailableTextures[i].TextureName, MatShader.AvailableTextures[i].ReadIndex);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.RoughnessTexture] as Port);
               break;
               case((int)TexturePurpose.Albedo):
                  Pos.y = 100;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, MatShader.AvailableTextures[i].TextureName);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.AlbedoTexture] as Port);
               break;
               case((int)TexturePurpose.Normal):
                  Pos.y = 180;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, MatShader.AvailableTextures[i].TextureName);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.NormalTexture] as Port);
               break;
               case((int)TexturePurpose.Emission):
                  Pos.y = 260;
                  ThisNode = CreateInputNode("Texture", typeof(Texture), Pos, MatShader.AvailableTextures[i].TextureName);
                  ThisEdge = (ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.EmissionTexture] as Port);
               break;
            }
            _graphView.AddElement(ThisNode);
            _graphView.AddElement(ThisEdge);
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
            Index = RangeProperties.IndexOf(MatShader.MetallicRange);
            if(Index > 0) {
               DialogueNode ThisNode = new DialogueNode();
               Pos.y = 420;
                ThisNode = CreateInputNode("Float", typeof(float), Pos, MatShader.MetallicRange);
               _graphView.AddElement(ThisNode);
               _graphView.AddElement((ThisNode.outputContainer[0] as Port).ConnectTo(OutputNode.inputContainer[(int)Properties.MetallicSlider] as Port));
            }

            Index = RangeProperties.IndexOf(MatShader.RoughnessRange);
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
        var PartialTextureNodeButton = new Button(() => {_graphView.AddElement(CreateInputNode("Texture", typeof(Texture), new Vector2(0,0), "null", 0));}) {text = "Texture(Single Component)"};
        var FloatNodeButton = new Button(() => {_graphView.AddElement(CreateInputNode("Float", typeof(float), new Vector2(0,0), "null"));}) {text = "Float"};
        var ColorNodeButton = new Button(() => {_graphView.AddElement(CreateInputNode("Color", typeof(Color), new Vector2(0,0), "null"));}) {text = "Color"};
        var RangeNodeButton = new Button(() => {_graphView.AddElement(CreateInputNode("MinMax", typeof(float), new Vector2(0,0), "null", -1, true));}) {text = "Range"};

        toolbar.Add(TextureNodeButton);//partial textures will be defined as according to the output they are connected to 
        toolbar.Add(PartialTextureNodeButton);//partial textures will be defined as according to the output they are connected to 
        toolbar.Add(FloatNodeButton);
        toolbar.Add(ColorNodeButton);
        toolbar.Add(RangeNodeButton);
        MaterialPairingMenu.Add(toolbar);
        MaterialPairingMenu.Add(Spacer);

        
      }

      List<string> definesList;

       private void RemoveDefine(string define)
       {
           definesList = GetDefines();
           if (definesList.Contains(define))
           {
               definesList.Remove(define);
           }
       }

       private void AddDefine(string define)
       {
           definesList = GetDefines();
           if (!definesList.Contains(define))
           {
               definesList.Add(define);
           }
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
            int MatCount = RayObjs[i].MaterialOptions.Length;
            for(int i2 = 0; i2 < MatCount; i2++) {
               if(RayObjs[i].MaterialOptions[i2] == RayTracingObject.Options.Cutout) 
                  RayObjs[i].MaterialOptions[i2] = RayTracingObject.Options.Disney;
               else if(RayObjs[i].MaterialOptions[i2] != RayTracingObject.Options.Disney) 
                  RayObjs[i].MaterialOptions[i2] = RayTracingObject.Options.Disney;
            }
         }

      }

      void AddHardSettingsToMenu() {
         definesList = GetDefines();
         SetGlobalDefines("HardwareRT", definesList.Contains("HardwareRT"));
         SetGlobalDefines("UseBindless", !(definesList.Contains("UseAtlas")));
         if(definesList.Contains("DisableRadianceCache")) SetGlobalDefines("RadianceCache", false);
         SetGlobalDefines("DX11", definesList.Contains("DX11Only"));
         Toggle HardwareRTToggle = new Toggle() {value = (definesList.Contains("HardwareRT")), text = "Enable RT Cores (Requires Unity 2023+)"};
         HardwareRTToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) {AddDefine("HardwareRT"); SetGlobalDefines("HardwareRT", true);} else {RemoveDefine("HardwareRT"); SetGlobalDefines("HardwareRT", false);}SetDefines();});
         HardSettingsMenu.Add(HardwareRTToggle);

         Toggle BindlessToggle = new Toggle() {value = (definesList.Contains("UseAtlas")), text = "Disable Bindless Textures"};
         BindlessToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) {AddDefine("UseAtlas"); SetGlobalDefines("UseBindless", false);} else {RemoveDefine("UseAtlas"); SetGlobalDefines("UseBindless", true);} SetDefines();});
         HardSettingsMenu.Add(BindlessToggle);

         Toggle NonAccurateLightTriToggle = new Toggle() {value = (definesList.Contains("AccurateLightTris")), text = "Enable Emissive Texture Aware Light BVH"};
         NonAccurateLightTriToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) AddDefine("AccurateLightTris"); else RemoveDefine("AccurateLightTris"); SetDefines();});
         HardSettingsMenu.Add(NonAccurateLightTriToggle);
         
         VisualElement ClayColorBox = new VisualElement();

         Toggle ClayModeToggle = new Toggle() {value = ClayMode, text = "Use ClayMode"};
         ClayModeToggle.RegisterValueChangedCallback(evt => {ClayMode = evt.newValue; RayMaster.LocalTTSettings.ClayMode = ClayMode; if(evt.newValue) HardSettingsMenu.Insert(HardSettingsMenu.IndexOf(ClayModeToggle) + 1, ClayColorBox); else HardSettingsMenu.Remove(ClayColorBox);});
         HardSettingsMenu.Add(ClayModeToggle);

         ColorField ClayColorField = new ColorField();
         ClayColorField.label = "Clay Color: ";
         ClayColorField.value = new Color(ClayColor.x, ClayColor.y, ClayColor.z, 1.0f);
         ClayColorField.style.width = 250;
         ClayColorField.RegisterValueChangedCallback(evt => {ClayColor = new Vector3(evt.newValue.r, evt.newValue.g, evt.newValue.b); RayMaster.LocalTTSettings.ClayColor = ClayColor;});
         ClayColorBox.Add(ClayColorField);
         if(ClayMode) HardSettingsMenu.Add(ClayColorBox);

         ColorField GroundColorField = new ColorField();
         GroundColorField.label = "Ground Color: ";
         GroundColorField.value = new Color(GroundColor.x, GroundColor.y, GroundColor.z, 1.0f);
         GroundColorField.style.width = 250;
         GroundColorField.RegisterValueChangedCallback(evt => {GroundColor = new Vector3(evt.newValue.r, evt.newValue.g, evt.newValue.b); RayMaster.LocalTTSettings.GroundColor = GroundColor;});
         HardSettingsMenu.Add(GroundColorField);

         Toggle OIDNToggle = new Toggle() {value = (definesList.Contains("UseOIDN")), text = "Enable OIDN(Does NOT work with DX11 Only)"};
         OIDNToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) AddDefine("UseOIDN"); else RemoveDefine("UseOIDN"); SetDefines();});
         HardSettingsMenu.Add(OIDNToggle);

         Toggle RadCacheToggle = new Toggle() {value = (definesList.Contains("DisableRadianceCache")), text = "Disable Radiance Cache"};
         RadCacheToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) {SetGlobalDefines("RadianceCache", false); AddDefine("DisableRadianceCache");} else {SetGlobalDefines("RadianceCache", true); RemoveDefine("DisableRadianceCache");} SetDefines();});
         HardSettingsMenu.Add(RadCacheToggle);


         // Toggle LightmappingToggle = new Toggle() {value = (definesList.Contains("TTLightMapping")), text = "Use TrueTrace as a LightMapper"};
         // LightmappingToggle.RegisterValueChangedCallback(evt => {if(evt.newValue) definesList.Add("TTLightMapping"); else RemoveDefine("TTLightMapping"); SetDefines();});
         // HardSettingsMenu.Add(LightmappingToggle);
         Toggle DX11Toggle = new Toggle() {value = (definesList.Contains("DX11Only")), text = "Use DX11"};
         DX11Toggle.RegisterValueChangedCallback(evt => {if(evt.newValue) {BindlessToggle.value = true; 
                                                                           OIDNToggle.value = false; 
                                                                           RemoveDefine("UseOIDN"); 
                                                                           AddDefine("UseAtlas"); 
                                                                           AddDefine("DX11Only"); 
                                                                           SetGlobalDefines("DX11", true); 
                                                                           SetGlobalDefines("UseBindless", false);} else {RemoveDefine("DX11Only"); SetGlobalDefines("DX11", false);}SetDefines();});
         HardSettingsMenu.Add(DX11Toggle);


         Button RemoveTrueTraceButton = new Button(() => RemoveTrueTrace()) {text = "Remove TrueTrace Scripts From Scene"};
         HardSettingsMenu.Add(RemoveTrueTraceButton);
         DingToggle = new Toggle() {value = DoDing, text = "Play Ding When Build Finishes"};
         DingToggle.RegisterValueChangedCallback(evt => {DoDing = evt.newValue; RayTracingMaster.DoDing = DoDing;});
         HardSettingsMenu.Add(DingToggle);
         Toggle DoSavingToggle = new Toggle() {value = DoSaving, text = "Enable RayTacingObject Saving"};
         DoSavingToggle.RegisterValueChangedCallback(evt => {DoSaving = evt.newValue; RayTracingMaster.DoSaving = DoSaving;});
         HardSettingsMenu.Add(DoSavingToggle);
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
         HardSettingsMenu.Add(ScreenShotBox);

         Button CorrectMatOptionsButton = new Button(() => FixRayObjects()) {text = "Correct Mat Options"};
         HardSettingsMenu.Add(CorrectMatOptionsButton);

      }

      void AddHierarchyOptionsToMenu() {
         ObjectField SelectiveAutoAssignField = new ObjectField();
         SelectiveAutoAssignField.objectType = typeof(GameObject);
         SelectiveAutoAssignField.label = "Selective Auto Assign Scripts";
         Button SelectiveAutoAssignButton = new Button(() => {
            ParentData SourceParent = GrabChildren2((SelectiveAutoAssignField.value as GameObject).transform);
            SolveChildren(SourceParent);
            }) {text = "Selective Auto Assign"};
         HierarchyOptionsMenu.Add(SelectiveAutoAssignField);
         HierarchyOptionsMenu.Add(SelectiveAutoAssignButton);
      
         ForceInstancesButton = new Button(() => {if(!Application.isPlaying) ConstructInstances(); else Debug.Log("Cant Do This In Editor");}) {text = "Force Instances"};
         HierarchyOptionsMenu.Add(ForceInstancesButton);

         StaticButton = new Button(() => {if(!Application.isPlaying) OptimizeForStatic(); else Debug.Log("Cant Do This In Editor");}) {text = "Make All Static"};
         StaticButton.style.minWidth = 105;
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
         currenttexture.ReadPixels(new Rect(RayMaster.SourceWidth / 2, RayMaster.SourceHeight / 2 - 1, 1, 1), 0, 0);
         currenttexture.Apply();
          RenderTexture.active = old_rt;
         var CenterDistance = currenttexture.GetPixelData<Vector4>(0);
         FocalSlider.value = CenterDistance[0].z + RayTracingMaster._camera.nearClipPlane;
         Destroy(currenttexture);
         CenterDistance.Dispose();
      }

         void EvaluateScene(Scene Current, Scene Next) {
            rootVisualElement.Clear();
            MainSource.Clear();
            CreateGUI();
         }

         void SaveScene(Scene Current, string ThrowawayString) {
            EditorUtility.SetDirty(Assets);
            Assets.ClearAll();
            InstancedManager Instanced = GameObject.Find("InstancedStorage").GetComponent<InstancedManager>();
            EditorUtility.SetDirty(Instanced);
            Instanced.ClearAll();
            Cleared = true;
         }

         private void TakeScreenshot() {
            ScreenCapture.CaptureScreenshot(PlayerPrefs.GetString("ScreenShotPath") + "/" + System.DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ", " + RayMaster.SampleCount + " Samples.png");
            UnityEditor.AssetDatabase.Refresh();
         }
         bool HasNoMore = false;


void AddResolution(int width, int height, string label)
    {
        Type gameViewSize = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
        Type gameViewSizes = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        Type gameViewSizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
        Type generic = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizes);
        MethodInfo getGroup = gameViewSizes.GetMethod("GetGroup");
        object instance = generic.GetProperty("instance").GetValue(null, null);         
        object group = getGroup.Invoke(instance, new object[] { (int)GameViewSizeGroupType.Standalone });       
        Type[] types = new Type[] { gameViewSizeType, typeof(int), typeof(int), typeof(string)};
        ConstructorInfo constructorInfo = gameViewSize.GetConstructor(types);
        object entry = constructorInfo.Invoke(new object[] { 1, width, height, label });
        MethodInfo addCustomSize = getGroup.ReturnType.GetMethod("AddCustomSize");
        addCustomSize.Invoke(group, new object[] { entry });
    }
 void RemoveResolution(int index)
    {
        Type gameViewSizes = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        Type generic = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizes);
        MethodInfo getGroup = gameViewSizes.GetMethod("GetGroup");
        object instance = generic.GetProperty("instance").GetValue(null, null);
        object group = getGroup.Invoke(instance, new object[] { (int)GameViewSizeGroupType.Standalone });
        MethodInfo removeCustomSize = getGroup.ReturnType.GetMethod("RemoveCustomSize");
        removeCustomSize.Invoke(group, new object[] { index });
    }

    int GetCount()
    {
        Type gameViewSizes = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        Type generic = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizes);
        MethodInfo getGroup = gameViewSizes.GetMethod("GetGroup");
        object instance = generic.GetProperty("instance").GetValue(null, null);
        PropertyInfo currentGroupType = instance.GetType().GetProperty("currentGroupType");
        GameViewSizeGroupType groupType = (GameViewSizeGroupType)(int)currentGroupType.GetValue(instance, null);
        object group = getGroup.Invoke(instance, new object[] { (int)groupType });
        MethodInfo getBuiltinCount = group.GetType().GetMethod("GetBuiltinCount");
        MethodInfo getCustomCount = group.GetType().GetMethod("GetCustomCount");
        return (int)getBuiltinCount.Invoke(group, null) + (int)getCustomCount.Invoke(group, null);   
    }

    void SetResolution(int index)
    {
        Type gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        PropertyInfo selectedSizeIndex = gameView.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        EditorWindow window = EditorWindow.GetWindow(gameView);
        selectedSizeIndex.SetValue(window, index, null);
    }
        public void CreateGUI() {
            HasNoMore = false;
            if(!PlayerPrefs.HasKey("ScreenShotPath")) {
               PlayerPrefs.SetString("ScreenShotPath",  Application.dataPath + "/ScreenShots");
            }
            if(!System.IO.Directory.Exists(PlayerPrefs.GetString("ScreenShotPath"))) {
               AssetDatabase.CreateFolder("Assets", "ScreenShots");
               PlayerPrefs.SetString("ScreenShotPath",  Application.dataPath + "/ScreenShots");
            }

            DingNoise = Resources.Load("Utility/DING", typeof(AudioClip)) as AudioClip;
            OnFocus();
            MainSource = new VisualElement();
            HierarchyOptionsMenu = new VisualElement();
            MaterialPairingMenu = new VisualElement();
            SceneSettingsMenu = new VisualElement();
            HardSettingsMenu = new VisualElement();
            InputMaterialField = new ObjectField();
            InputMaterialField.objectType = typeof(Material);
            InputMaterialField.label = "Drag a material with the desired shader here ->";
            InputMaterialField.RegisterValueChangedCallback(evt => {MaterialPairingMenu.Clear(); MaterialPairingMenu.Add(InputMaterialField); AddAssetsToMenu();});
            MaterialPairingMenu.Add(InputMaterialField);
            toolbar = new Toolbar();
            rootVisualElement.Add(toolbar);
            if(Assets == null) {
               RearrangeElement = new VisualElement();
               Button RearrangeButton = new Button(() => {UnityEditor.PopupWindow.Show(new Rect(0,0,10,10), new PopupWarningWindow());}) {text="Arrange Hierarchy"};
               RearrangeElement.Add(RearrangeButton);
               rootVisualElement.Add(RearrangeElement);
               return;
            } else {
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
            Button MainSourceButton = new Button(() => {rootVisualElement.Clear(); rootVisualElement.Add(toolbar); rootVisualElement.Add(MainSource); MaterialPairingMenu.Clear();});
            Button MaterialPairButton = new Button(() => {rootVisualElement.Clear(); rootVisualElement.Add(toolbar); InputMaterialField.value = null; MaterialPairingMenu.Add(InputMaterialField); rootVisualElement.Add(MaterialPairingMenu);});
            Button SceneSettingsButton = new Button(() => {rootVisualElement.Clear(); rootVisualElement.Add(toolbar); rootVisualElement.Add(SceneSettingsMenu);});
            Button HardSettingsButton = new Button(() => {rootVisualElement.Clear(); rootVisualElement.Add(toolbar); rootVisualElement.Add(HardSettingsMenu);});
            Button HierarchyOptionsButton = new Button(() => {rootVisualElement.Clear(); rootVisualElement.Add(toolbar); rootVisualElement.Add(HierarchyOptionsMenu);});
            toolbar.Add(MainSourceButton);
            toolbar.Add(MaterialPairButton);
            toolbar.Add(SceneSettingsButton);
            toolbar.Add(HardSettingsButton);
            toolbar.Add(HierarchyOptionsButton);
            MainSourceButton.text = "Main Options";
            MaterialPairButton.text = "Material Pair Options";
            SceneSettingsButton.text = "Scene Settings";
            HardSettingsButton.text = "Functionality Settings";
            HierarchyOptionsButton.text = "Hierarchy Options";

            if(RayMaster != null && Assets != null) {
            AddNormalSettings();
           Assets.UseSkinning = MeshSkin;
           RayMaster.AtmoNumLayers = AtmoScatter;
           Assets.MainDesiredRes = AtlasSize;
           Assets.LightEnergyScale = LightEnergyScale;
           RayMaster.LoadTT();
           LEMEnergyScale = RayMaster.LocalTTSettings.LEMEnergyScale;
           RayTracingMaster.DoSaving = DoSaving;
           RayTracingMaster.DoDing = DoDing;
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
           UpdateRate = RayMaster.LocalTTSettings.ReSTIRGIUpdateRate;
           GITemporalMCap = RayMaster.LocalTTSettings.ReSTIRGITemporalMCap;
           GISpatialSampleCount = RayMaster.LocalTTSettings.ReSTIRGISpatialCount;
           TAA = RayMaster.LocalTTSettings.PPTAA;
           ToneMap = RayMaster.LocalTTSettings.PPToneMap;
           ToneMapIndex = RayMaster.LocalTTSettings.ToneMapper;
           TAAU = RayMaster.LocalTTSettings.UseTAAU;
           DoPartialRendering = RayMaster.LocalTTSettings.DoPartialRendering;
           PartialRenderingFactor = RayMaster.LocalTTSettings.PartialRenderingFactor;
           DoFirefly = RayMaster.LocalTTSettings.DoFirefly;
           ReSTIRGISpatialRadius = RayMaster.LocalTTSettings.ReSTIRGISpatialRadius;
           RISCount = RayMaster.LocalTTSettings.RISCount;
           ImprovedPrimaryHit = RayMaster.LocalTTSettings.ImprovedPrimaryHit;
           ClayMode = RayMaster.LocalTTSettings.ClayMode;
           SceneBackgroundColor = new Color(RayMaster.LocalTTSettings.SceneBackgroundColor.x, RayMaster.LocalTTSettings.SceneBackgroundColor.y, RayMaster.LocalTTSettings.SceneBackgroundColor.z, 1);
           SecondarySceneBackgroundColor = new Color(RayMaster.LocalTTSettings.SecondarySceneBackgroundColor.x, RayMaster.LocalTTSettings.SecondarySceneBackgroundColor.y, RayMaster.LocalTTSettings.SecondarySceneBackgroundColor.z, 1);
           BackgroundIntensity = RayMaster.LocalTTSettings.BackgroundIntensity;
           IndirectBoost = RayMaster.LocalTTSettings.IndirectBoost;
           BackgroundType = RayMaster.LocalTTSettings.BackgroundType;
           SecondaryBackgroundType = RayMaster.LocalTTSettings.SecondaryBackgroundType;
           SunDesaturate = RayMaster.LocalTTSettings.SunDesaturate;
           SkyDesaturate = RayMaster.LocalTTSettings.SkyDesaturate;
           FireflyFrameCount = RayMaster.LocalTTSettings.FireflyFrameCount;
           OIDNFrameCount = RayMaster.LocalTTSettings.OIDNFrameCount;
         }

           AddHardSettingsToMenu();
           AddHierarchyOptionsToMenu();
           BVHBuild = new Button(() => OnStartAsyncCombined()) {text = "Build Aggregated BVH"};
           BVHBuild.style.minWidth = 145;
           ScreenShotButton = new Button(() => TakeScreenshot()) {text = "Take Screenshot"};
           ScreenShotButton.style.minWidth = 100;

           Button PanoramaButton = new Button(() => {
            var TempPan = GameObject.Find("Scene").GetComponent<PanoramaDoer>();
            // if(TempPan != null && ((float)TempPan.FinalAtlasSize.x / (float)RayTracingMaster._camera.pixelWidth) != Mathf.Ceil((float)TempPan.FinalAtlasSize.x / (float)RayTracingMaster._camera.pixelWidth)) {
               // Debug.LogError("You need to set the resolution width to evenly divide the width (" + TempPan.FinalAtlasSize.x + "), and the height to " + TempPan.FinalAtlasSize.y);
            // } else {
               if(TempPan != null) {
                  AddResolution(Mathf.CeilToInt((float)TempPan.FinalAtlasSize.x / (float)TempPan.HorizontalSegments), TempPan.FinalAtlasSize.y, "TempPanoramaSize");
                  SetResolution(GetCount() - 1);
                  TempPan.Init();
                  RayMaster.DoPanorama = true;
                  RayMaster.DoChainedImages = true;
               } else {
                  Debug.LogError("You need to add the PanoramaDoer to the Scene Gameobject");
               }
            // }
            }) {text = "Create Panorama"};
           PanoramaButton.style.minWidth = 105;

           Button ChainedImageButton = new Button(() => {
            var TempPan = GameObject.Find("Scene").GetComponent<PanoramaDoer>();
               if(TempPan != null) {
                  AddResolution(TempPan.FinalAtlasSize.x, TempPan.FinalAtlasSize.y, "TempPanoramaSize");
                  SetResolution(GetCount() - 1);
                  TempPan.HorizontalSegments = 1;
                  TempPan.Init();
                  RayMaster.DoPanorama = false;
                  RayMaster.DoChainedImages = true;
               } else {
                  Debug.LogError("You need to add the PanoramaDoer to the Scene Gameobject");
               }
            }) {text = "Create Qued Screenshots"};
           ChainedImageButton.style.minWidth = 105;

           
           ClearButton = new Button(() => {
            if(!Application.isPlaying) {
               EditorUtility.SetDirty(Assets);
               Assets.ClearAll();
               InstancedManager Instanced = GameObject.Find("InstancedStorage").GetComponent<InstancedManager>();
               EditorUtility.SetDirty(Instanced);
               Instanced.ClearAll();
               Cleared = true;
           } else Debug.Log("Cant Do This In Editor");}) {text = "Clear Parent Data"};
           ClearButton.style.minWidth = 145;
           QuickStartButton = new Button(() => QuickStart()) {text = "Auto Assign Scripts"};
           QuickStartButton.style.minWidth = 111;

           IntegerField AtlasField = new IntegerField() {value = AtlasSize, label = "Atlas Size"};
           AtlasField.isDelayed = true;
           AtlasField.RegisterValueChangedCallback(evt => {if(!Application.isPlaying) {AtlasSize = evt.newValue; AtlasSize = Mathf.Min(AtlasSize, 16384); AtlasSize = Mathf.Max(AtlasSize, 32); AtlasField.value = AtlasSize; Assets.MainDesiredRes = AtlasSize;} else AtlasField.value = AtlasSize;});
               AtlasField.ElementAt(0).style.minWidth = 65;
               AtlasField.ElementAt(1).style.width = 45;

           Box ButtonField1 = new Box();
           ButtonField1.style.flexDirection = FlexDirection.Row;
           ButtonField1.Add(BVHBuild);
           ButtonField1.Add(ScreenShotButton);
           ButtonField1.Add(PanoramaButton);
           MainSource.Add(ButtonField1);

           Box ButtonField2 = new Box();
           ButtonField2.style.flexDirection = FlexDirection.Row;
           ButtonField2.Add(ClearButton);
           ButtonField2.Add(QuickStartButton);
           ButtonField2.Add(ChainedImageButton);
           MainSource.Add(ButtonField2);

           Box TopEnclosingBox = new Box();
               TopEnclosingBox.style.flexDirection = FlexDirection.Row;
               FloatField BounceField = new FloatField() {value = (int)BounceCount, label = "Max Bounces"};
               BounceField.ElementAt(0).style.minWidth = 75;
               BounceField.ElementAt(1).style.width = 25;
               BounceField.style.paddingRight = 40;
               TopEnclosingBox.Add(BounceField);
               BounceField.RegisterValueChangedCallback(evt => {BounceCount = (int)evt.newValue; RayMaster.LocalTTSettings.bouncecount = BounceCount;});        
               ResField = new FloatField("Internal Resolution Ratio") {value = RenderRes};
               ResField.ElementAt(0).style.minWidth = 75;
               ResField.ElementAt(1).style.width = 35;
               TopEnclosingBox.Add(ResField);
               ResField.RegisterValueChangedCallback(evt => {RenderRes = evt.newValue; RenderRes = Mathf.Max(RenderRes, 0.1f); RenderRes = Mathf.Min(RenderRes, 1.0f); RayMaster.LocalTTSettings.RenderScale = RenderRes;});        
               TopEnclosingBox.Add(AtlasField);
           MainSource.Add(TopEnclosingBox);

           RRToggle = new Toggle() {value = RR, text = "Use Russian Roulette"};
           MainSource.Add(RRToggle);
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
            RISCountField = new FloatField() {value = RISCount};
            RISCountField.RegisterValueChangedCallback(evt => {RISCount = (int)evt.newValue; RayMaster.LocalTTSettings.RISCount = RISCount;});
            NEEBox.style.flexDirection = FlexDirection.Row;
            NEEBox.Add(RISLabel);
            NEEBox.Add(RISCountField);
           NEEToggle.RegisterValueChangedCallback(evt => {NEE = evt.newValue; RayMaster.LocalTTSettings.UseNEE = NEE; if(evt.newValue) MainSource.Insert(MainSource.IndexOf(NEEToggle) + 1, NEEBox);else MainSource.Remove(NEEBox);});
            if(NEEToggle.value) {
               MainSource.Add(NEEBox);
            }
       

           SkinToggle = new Toggle() {value = MeshSkin, text = "Allow Mesh Skinning"};
           MainSource.Add(SkinToggle);
           SkinToggle.RegisterValueChangedCallback(evt => {MeshSkin = evt.newValue; Assets.UseSkinning = MeshSkin;});

            List<string> DenoiserSettings = new List<string>();
            DenoiserSettings.Add("None");
            DenoiserSettings.Add("ASVGF");
            #if UseOIDN
               DenoiserSettings.Add("OIDN");
            #endif
            PopupField<string> DenoiserField = new PopupField<string>("<b>Denoiser</b>");
            DenoiserField.ElementAt(0).style.minWidth = 55;
            DenoiserField.style.width = 275;
            DenoiserField.choices = DenoiserSettings;
            DenoiserField.index = DenoiserSelection;
            DenoiserField.style.flexDirection = FlexDirection.Row;
            DenoiserField.RegisterValueChangedCallback(evt => {
               DenoiserSelection = DenoiserField.index;
               RayMaster.LocalTTSettings.UseASVGF = false;
               RayMaster.LocalTTSettings.UseOIDN = false;
               if(DenoiserField.Contains(OIDNFrameField)) DenoiserField.Remove(OIDNFrameField);
               switch(DenoiserSelection) {
                  case 0:
                  break;
                  case 1:
                     RayMaster.LocalTTSettings.UseASVGF = true;
                  break;
                  case 2:
                     #if UseOIDN
                        RayMaster.LocalTTSettings.UseOIDN = true;
                        OIDNFrameField = new IntegerField("Frames Before OIDN") {value = OIDNFrameCount};
                        OIDNFrameField.ElementAt(0).style.minWidth = 95;
                        OIDNFrameField.RegisterValueChangedCallback(evt => {OIDNFrameCount = (int)evt.newValue; RayMaster.LocalTTSettings.OIDNFrameCount = OIDNFrameCount;});
                        DenoiserField.Add(OIDNFrameField);
                     #else 
                        RayMaster.LocalTTSettings.UseOIDN = false;
                        DenoiserField.index = 0;
                        DenoiserSelection = 0;
                     #endif
                  break;

               } 
            });
            if(DenoiserField.Contains(OIDNFrameField)) DenoiserField.Remove(OIDNFrameField);
            DenoiserSelection = DenoiserField.index;
            if(RayMaster.LocalTTSettings.UseASVGF) {
               DenoiserSelection = 1;
            } else if(RayMaster.LocalTTSettings.UseOIDN) {
               DenoiserSelection = 2;
            }
            switch(DenoiserSelection) {
               case 0:
               break;
               case 1:
                  RayMaster.LocalTTSettings.UseASVGF = true;
               break;
               case 2:
                  #if UseOIDN
                     RayMaster.LocalTTSettings.UseOIDN = true;
                     OIDNFrameField = new IntegerField("Frames Before OIDN") {value = OIDNFrameCount};
                     OIDNFrameField.ElementAt(0).style.minWidth = 95;
                     OIDNFrameField.RegisterValueChangedCallback(evt => {OIDNFrameCount = (int)evt.newValue; RayMaster.LocalTTSettings.OIDNFrameCount = OIDNFrameCount;});
                     DenoiserField.Add(OIDNFrameField);
                  #else 
                     RayMaster.LocalTTSettings.UseOIDN = false;
                     DenoiserField.index = 0;
                     DenoiserSelection = 0;
                  #endif
               break;
            } 

            MainSource.Add(DenoiserField);




         BloomToggle = new Toggle() {value = Bloom, text = "Enable Bloom"};
           VisualElement BloomBox = new VisualElement();
               Label BloomLabel = new Label("Bloom Strength");
               Slider BloomSlider = new Slider() {value = BloomStrength, highValue = 0.9999f, lowValue = 0.25f};
               BloomSlider.style.width = 100;
               BloomToggle.RegisterValueChangedCallback(evt => {Bloom = evt.newValue; RayMaster.LocalTTSettings.PPBloom = Bloom; if(evt.newValue) MainSource.Insert(MainSource.IndexOf(BloomToggle) + 1, BloomBox); else MainSource.Remove(BloomBox);});        
               BloomSlider.RegisterValueChangedCallback(evt => {BloomStrength = evt.newValue; RayMaster.LocalTTSettings.BloomStrength = BloomStrength;});
               MainSource.Add(BloomToggle);
               BloomBox.style.flexDirection = FlexDirection.Row;
               BloomBox.Add(BloomLabel);
               BloomBox.Add(BloomSlider);
           if(Bloom) MainSource.Add(BloomBox);



            VisualElement SharpenFoldout = new VisualElement() {};
               Slider SharpnessSlider = new Slider() {label = "Sharpness: ", value = Sharpness, highValue = 1.0f, lowValue = 0.0f};
               SharpnessSlider.style.width = 200;
               SharpnessSlider.RegisterValueChangedCallback(evt => {Sharpness = evt.newValue; RayMaster.LocalTTSettings.Sharpness = Sharpness;});
               SharpenFoldout.Add(SharpnessSlider);
            SharpnessSlider.ElementAt(0).style.minWidth = 65;


            Toggle SharpenToggle = new Toggle() {value = DoSharpen, text = "Use Sharpness Filter"};
            MainSource.Add(SharpenToggle);
            SharpenToggle.RegisterValueChangedCallback(evt => {DoSharpen = evt.newValue; RayMaster.LocalTTSettings.DoSharpen = DoSharpen; if(evt.newValue) MainSource.Insert(MainSource.IndexOf(SharpenToggle) + 1, SharpenFoldout); else MainSource.Remove(SharpenFoldout);});
            if(DoSharpen) MainSource.Add(SharpenFoldout);



           Label AperatureLabel = new Label("Aperature Size");
           Slider AperatureSlider = new Slider() {value = DoFAperature, highValue = 1, lowValue = 0};
           AperatureSlider.style.width = 250;
           FloatField AperatureScaleField = new FloatField() {value = DoFAperatureScale, label = "Aperature Scale"};
           AperatureScaleField.ElementAt(0).style.minWidth = 65;
           AperatureScaleField.RegisterValueChangedCallback(evt => {DoFAperatureScale = evt.newValue; DoFAperatureScale = Mathf.Max(DoFAperatureScale, 0.0001f); RayMaster.LocalTTSettings.DoFAperatureScale = DoFAperatureScale; AperatureScaleField.value = DoFAperatureScale;});
           Label FocalLabel = new Label("Focal Length");
           FocalSlider = new FloatField() {value = DoFFocal};
           FocalSlider.style.width = 150;
           Button AutofocusButton = new Button(() => GetFocalLength()) {text = "Autofocus DoF"};
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
           FocalBox.Add(AutofocusButton);
           FocalBox.style.flexDirection = FlexDirection.Row;

           Toggle DoFToggle = new Toggle() {value = DoF, text = "Enable DoF"};
           VisualElement DoFFoldout = new VisualElement();
           DoFFoldout.Add(AperatureBox);
           DoFFoldout.Add(FocalBox);
           MainSource.Add(DoFToggle);
           DoFToggle.RegisterValueChangedCallback(evt => {DoF = evt.newValue; RayMaster.LocalTTSettings.PPDoF = DoF;if(evt.newValue) MainSource.Insert(MainSource.IndexOf(DoFToggle) + 1, DoFFoldout); else MainSource.Remove(DoFFoldout);});        
           AperatureSlider.RegisterValueChangedCallback(evt => {DoFAperature = evt.newValue; RayMaster.LocalTTSettings.DoFAperature = DoFAperature;});
           FocalSlider.RegisterValueChangedCallback(evt => {DoFFocal = Mathf.Max(0.001f, evt.newValue); RayMaster.LocalTTSettings.DoFFocal = DoFFocal;});
           if(DoF) MainSource.Add(DoFFoldout);
           
           Toggle DoExposureToggle = new Toggle() {value = DoExposure, text = "Enable Auto/Manual Exposure"};
           MainSource.Add(DoExposureToggle);
           VisualElement ExposureElement = new VisualElement();
               ExposureElement.style.flexDirection = FlexDirection.Row;
               Label ExposureLabel = new Label("Exposure");
               Slider ExposureSlider = new Slider() {value = Exposure, highValue = 50.0f, lowValue = 0};
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
           DoExposureToggle.RegisterValueChangedCallback(evt => {DoExposure = evt.newValue; RayMaster.LocalTTSettings.PPExposure = DoExposure;if(evt.newValue) MainSource.Insert(MainSource.IndexOf(DoExposureToggle) + 1, ExposureElement); else MainSource.Remove(ExposureElement);});
           ExposureSlider.RegisterValueChangedCallback(evt => {Exposure = evt.newValue; ExposureField.value = Exposure; RayMaster.LocalTTSettings.Exposure = Exposure;});
           ExposureField.RegisterValueChangedCallback(evt => {Exposure = evt.newValue; ExposureSlider.value = Exposure; RayMaster.LocalTTSettings.Exposure = Exposure;});
            if(DoExposure) MainSource.Add(ExposureElement);

           GIToggle = new Toggle() {value = ReSTIRGI, text = "Use ReSTIR GI"};
           VisualElement GIFoldout = new VisualElement() {};
           Box EnclosingGI = new Box();
               Box TopGI = new Box();
                   TopGI.style.flexDirection = FlexDirection.Row;
                   SampleValidToggle = new Toggle() {value = SampleValid, text = "Do Sample Connection Validation"};
                   SampleValidToggle.tooltip = "Confirms samples are mutually visable, reduces performance but improves indirect shadow quality";
                   Label GIUpdateRateLabel = new Label("Update Rate(0 is off)");
                   GIUpdateRateLabel.tooltip = "How often a pixel should validate its entire path, good for quickly changing lighting";
                   GIUpdateRateField = new FloatField() {value = UpdateRate};
                   SampleValidToggle.RegisterValueChangedCallback(evt => {SampleValid = evt.newValue; RayMaster.LocalTTSettings.DoReSTIRGIConnectionValidation = SampleValid;});
                   GIUpdateRateField.RegisterValueChangedCallback(evt => {UpdateRate = (int)evt.newValue; RayMaster.LocalTTSettings.ReSTIRGIUpdateRate = UpdateRate;});
                   TopGI.Add(SampleValidToggle);
                   TopGI.Add(GIUpdateRateField);
                   TopGI.Add(GIUpdateRateLabel);
               EnclosingGI.Add(TopGI);
               Box TemporalGI = new Box();
                   TemporalGI.style.flexDirection = FlexDirection.Row;
                   TemporalGIToggle = new Toggle() {value = GITemporal, text = "Enable Temporal"};
                   
                   Label TemporalGIMCapLabel = new Label("Temporal M Cap(0 is off)");
                   TemporalGIMCapLabel.tooltip = "Controls how long a sample is valid for, lower numbers update more quickly but have more noise, good for quickly changing scenes/lighting";
                   TeporalGIMCapField = new FloatField() {value = GITemporalMCap};
                   TemporalGIToggle.RegisterValueChangedCallback(evt => {GITemporal = evt.newValue; RayMaster.LocalTTSettings.UseReSTIRGITemporal = GITemporal;});
                   TeporalGIMCapField.RegisterValueChangedCallback(evt => {GITemporalMCap = (int)evt.newValue; RayMaster.LocalTTSettings.ReSTIRGITemporalMCap = GITemporalMCap;});
                   TemporalGI.Add(TemporalGIToggle);
                   TemporalGI.Add(TeporalGIMCapField);
                   TemporalGI.Add(TemporalGIMCapLabel);
               EnclosingGI.Add(TemporalGI);
               Box SpatialGI = new Box();
                   SpatialGI.style.flexDirection = FlexDirection.Row;
                   SpatialGIToggle = new Toggle() {value = GISpatial, text = "Enable Spatial"};
                   Label SpatialGISampleCountLabel = new Label("Spatial Sample Count");
                   Label ReSTIRGISpatialRadiusLabel = new Label("Minimum Spatial Radius");
                   SpatialGISampleCountLabel.tooltip = "How many neighbors are sampled, tradeoff between performance and quality";
                   FloatField SpatialGISampleCountField = new FloatField() {value = GISpatialSampleCount};
                   FloatField ReSTIRGISpatialRadiusField = new FloatField() {value = ReSTIRGISpatialRadius};
                   SpatialGIToggle.RegisterValueChangedCallback(evt => {GISpatial = evt.newValue; RayMaster.LocalTTSettings.UseReSTIRGISpatial = GISpatial;});
                   SpatialGISampleCountField.RegisterValueChangedCallback(evt => {GISpatialSampleCount = (int)evt.newValue; RayMaster.LocalTTSettings.ReSTIRGISpatialCount = GISpatialSampleCount;});
                   ReSTIRGISpatialRadiusField.RegisterValueChangedCallback(evt => {ReSTIRGISpatialRadius = (int)evt.newValue; RayMaster.LocalTTSettings.ReSTIRGISpatialRadius = ReSTIRGISpatialRadius;});
                   SpatialGI.Add(SpatialGIToggle);
                   SpatialGI.Add(SpatialGISampleCountField);
                   SpatialGI.Add(SpatialGISampleCountLabel);
                   SpatialGI.Add(ReSTIRGISpatialRadiusField);
                   SpatialGI.Add(ReSTIRGISpatialRadiusLabel);
               EnclosingGI.Add(SpatialGI);
           GIFoldout.Add(EnclosingGI);
           MainSource.Add(GIToggle);
           GIToggle.RegisterValueChangedCallback(evt => {ReSTIRGI = evt.newValue; RayMaster.LocalTTSettings.UseReSTIRGI = ReSTIRGI;if(evt.newValue) MainSource.Insert(MainSource.IndexOf(GIToggle) + 1, GIFoldout); else MainSource.Remove(GIFoldout);});
           if(ReSTIRGI) MainSource.Add(GIFoldout);
       

           TAAToggle = new Toggle() {value = TAA, text = "Enable TAA"};
           MainSource.Add(TAAToggle);
           TAAToggle.RegisterValueChangedCallback(evt => {TAA = evt.newValue; RayMaster.LocalTTSettings.PPTAA = TAA;});

           
            List<string> TonemapSettings = new List<string>();
            TonemapSettings.Add("TonyMcToneFace");
            TonemapSettings.Add("ACES Filmic");
            TonemapSettings.Add("Uchimura");
            TonemapSettings.Add("Reinhard");
            TonemapSettings.Add("Uncharted 2");
            TonemapSettings.Add("AgX");
            PopupField<string> ToneMapField = new PopupField<string>("<b>Tonemapper</b>");
            ToneMapField.ElementAt(0).style.minWidth = 65;
            ToneMapField.choices = TonemapSettings;
            ToneMapField.index = ToneMapIndex;
            ToneMapField.RegisterValueChangedCallback(evt => {ToneMapIndex = ToneMapField.index; RayMaster.LocalTTSettings.ToneMapper = ToneMapIndex;});

           Toggle ToneMapToggle = new Toggle() {value = ToneMap, text = "Enable Tonemapping"};
            VisualElement ToneMapFoldout = new VisualElement() {};
               ToneMapFoldout.style.flexDirection = FlexDirection.Row;
               ToneMapFoldout.Add(ToneMapField);
           MainSource.Add(ToneMapToggle);
           ToneMapToggle.RegisterValueChangedCallback(evt => {ToneMap = evt.newValue; RayMaster.LocalTTSettings.PPToneMap = ToneMap; if(evt.newValue) MainSource.Insert(MainSource.IndexOf(ToneMapToggle) + 1, ToneMapFoldout); else MainSource.Remove(ToneMapFoldout);});
           if(ToneMap) MainSource.Add(ToneMapFoldout);


           TAAUToggle = new Toggle() {value = TAAU, text = "Enable TAAU"};
           TAAUToggle.tooltip = "On = Temporal Anti Aliasing Upscaling; Off = Semi Custom Upscaler, performs slightly differently";
           MainSource.Add(TAAUToggle);
           TAAUToggle.RegisterValueChangedCallback(evt => {TAAU = evt.newValue; RayMaster.LocalTTSettings.UseTAAU = TAAU;});



            VisualElement PartialRenderingFoldout = new VisualElement() {};
               PartialRenderingFoldout.style.flexDirection = FlexDirection.Row;
               IntegerField PartialRenderingField = new IntegerField() {value = PartialRenderingFactor, label = "Partial Factor"};
               PartialRenderingField.ElementAt(0).style.minWidth = 65;
               PartialRenderingField.RegisterValueChangedCallback(evt => {PartialRenderingFactor = evt.newValue; PartialRenderingFactor = Mathf.Max(PartialRenderingFactor, 1); RayMaster.LocalTTSettings.PartialRenderingFactor = PartialRenderingFactor;});
               PartialRenderingFoldout.Add(PartialRenderingField);
           DoPartialRenderingToggle = new Toggle() {value = DoPartialRendering, text = "Use Partial Rendering"};
           MainSource.Add(DoPartialRenderingToggle);
           DoPartialRenderingToggle.RegisterValueChangedCallback(evt => {DoPartialRendering = evt.newValue; RayMaster.LocalTTSettings.DoPartialRendering = DoPartialRendering;if(evt.newValue) MainSource.Insert(MainSource.IndexOf(DoPartialRenderingToggle) + 1, PartialRenderingFoldout); else MainSource.Remove(PartialRenderingFoldout);});
           if(DoPartialRendering) MainSource.Add(PartialRenderingFoldout);

            VisualElement FireflyFoldout = new VisualElement() {};
               IntegerField FireflyFrameCountField = new IntegerField() {value = FireflyFrameCount, label = "Frames Before Anti-Firefly"};
               FireflyFrameCountField.ElementAt(0).style.minWidth = 65;
               FireflyFrameCountField.RegisterValueChangedCallback(evt => {FireflyFrameCount = evt.newValue; RayMaster.LocalTTSettings.FireflyFrameCount = FireflyFrameCount;});
               FireflyFrameCountField.style.maxWidth = 345;
               FireflyFoldout.Add(FireflyFrameCountField);
         
               Slider FireflyStrengthSlider = new Slider() {label = "Anti Firefly Strength: ", value = FireflyStrength, highValue = 1.0f, lowValue = 0.0f};
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
           MainSource.Add(ImprovedPrimaryHitToggle);


           VisualElement AtmoBox = new VisualElement();
               AtmoBox.style.flexDirection = FlexDirection.Row;
               AtmoScatterField = new FloatField("Atmospheric Scattering Samples") {value = AtmoScatter};
               AtmoScatterField.RegisterValueChangedCallback(evt => {AtmoScatter = (int)evt.newValue; RayMaster.AtmoNumLayers = AtmoScatter;});
               AtmoBox.Add(AtmoScatterField);
           MainSource.Add(AtmoBox);



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
               RemainingObjectsField.RegisterValueChangedCallback(evt => {if(evt.newValue == 0) {ReadyBox.style.backgroundColor = Color.green; if(!Cleared) PlayClip(DingNoise);} else ReadyBox.style.backgroundColor = Color.red;});
               Label ReadyLabel = new Label("All Objects Built");
               ReadyLabel.style.color = Color.black;
               ReadyBox.style.alignItems = Align.Center;
               ReadyBox.Add(ReadyLabel);
               EnclosingBox.Add(RemainingObjectsLabel);
               EnclosingBox.Add(RemainingObjectsField);
               EnclosingBox.Add(ReadyBox);
            MainSource.Add(EnclosingBox);

        }

         public GameObject getOBJ(int id)
         {
             Dictionary<int, GameObject> m_instanceMap = new Dictionary<int, GameObject>();
             //record instance map

             m_instanceMap.Clear();
             List<GameObject> gos = new List<GameObject>();
             foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)))
             {
                 if (gos.Contains(go))
                 {
                     continue;
                 }
                 gos.Add(go);
                 m_instanceMap[go.GetInstanceID()] = go;
             }

             if (m_instanceMap.ContainsKey(id))
             {
                 return m_instanceMap[id];
             }
             else
             {
                 return null;
             }
         }

        void Update() {
            if(!Application.isPlaying) {
               if(RayTracingMaster.DoCheck && DoSaving) {
                  try{
                      UnityEditor.AssetDatabase.Refresh();
                     using (var A = new StringReader(Resources.Load<TextAsset>("Utility/SaveFile").text)) {
                         var serializer = new XmlSerializer(typeof(RayObjs));
                         RayObjs rayreads = serializer.Deserialize(A) as RayObjs;
                         int RayReadCount = rayreads.RayObj.Count;
                         for(int i = 0; i < RayReadCount; i++) {
                           RayObjectDatas Ray = rayreads.RayObj[i];
                           GameObject TempObj = getOBJ(Ray.ID);
                           RayTracingObject TempRTO = TempObj.GetComponent<RayTracingObject>();
                           int NameIndex = (new List<string>(TempRTO.Names)).IndexOf(Ray.MatName);
                           if(NameIndex == -1) {
                              Debug.Log("EEEE");
                           } else {
                              TempRTO.MaterialOptions[NameIndex] = (RayTracingObject.Options)Ray.OptionID;
                              TempRTO.TransmissionColor[NameIndex] = Ray.TransCol;
                              TempRTO.BaseColor[NameIndex] = Ray.BaseCol;
                              TempRTO.MetallicRemap[NameIndex] = Ray.MetRemap;
                              TempRTO.RoughnessRemap[NameIndex] = Ray.RoughRemap;
                              TempRTO.emmission[NameIndex] = Ray.Emiss;
                              TempRTO.EmissionColor[NameIndex] = Ray.EmissCol;
                              TempRTO.Roughness[NameIndex] = Ray.Rough;
                              TempRTO.IOR[NameIndex] = Ray.IOR;
                              TempRTO.Metallic[NameIndex] = Ray.Met;
                              TempRTO.SpecularTint[NameIndex] = Ray.SpecTint;
                              TempRTO.Sheen[NameIndex] = Ray.Sheen;
                              TempRTO.SheenTint[NameIndex] = Ray.SheenTint;
                              TempRTO.ClearCoat[NameIndex] = Ray.Clearcoat;
                              TempRTO.ClearCoatGloss[NameIndex] = Ray.ClearcoatGloss;
                              TempRTO.Anisotropic[NameIndex] = Ray.Anisotropic;
                              TempRTO.Flatness[NameIndex] = Ray.Flatness;
                              TempRTO.DiffTrans[NameIndex] = Ray.DiffTrans;
                              TempRTO.SpecTrans[NameIndex] = Ray.SpecTrans;
                              TempRTO.FollowMaterial[NameIndex] = Ray.FollowMat;
                              TempRTO.ScatterDist[NameIndex] = Ray.ScatterDist;
                              TempRTO.Specular[NameIndex] = Ray.Spec;
                              TempRTO.AlphaCutoff[NameIndex] = Ray.AlphaCutoff;
                              TempRTO.NormalStrength[NameIndex] = Ray.NormStrength;
                              TempRTO.Hue[NameIndex] = Ray.Hue;
                              TempRTO.Brightness[NameIndex] = Ray.Brightness;
                              TempRTO.Contrast[NameIndex] = Ray.Contrast;
                              TempRTO.Saturation[NameIndex] = Ray.Saturation;
                              TempRTO.BlendColor[NameIndex] = Ray.BlendColor;
                              TempRTO.BlendFactor[NameIndex] = Ray.BlendFactor;
                              TempRTO.MainTexScaleOffset[NameIndex] = Ray.MainTexScaleOffset;
                              TempRTO.SecondaryTextureScale[NameIndex] = Ray.SecondaryTextureScale;
                              TempRTO.Rotation[NameIndex] = Ray.Rotation;
                              TempRTO.Flags[NameIndex] = Ray.Flags;
                              TempRTO.CallMaterialEdited();
                           }

                         }
                      }
                  } catch(System.Exception e) {HasNoMore = true;};
                  using(StreamWriter writer = new StreamWriter(Application.dataPath + "/TrueTrace/Resources/Utility/SaveFile.xml")) {
                      var serializer = new XmlSerializer(typeof(RayObjs));
                      serializer.Serialize(writer.BaseStream, new RayObjs());
                      UnityEditor.AssetDatabase.Refresh();
                  }
               }
               RayTracingMaster.DoCheck = false;
            }

            if(Assets != null && Instancer != null && Assets.RunningTasks != null && Instancer.RunningTasks != null) RemainingObjectsField.value = Assets.RunningTasks + Instancer.RunningTasks;
            if(RayMaster != null) SampleCountField.value = RayMaster.SampleCount;
            
            if(Assets != null && Assets.NeedsToUpdateXML) {
                using(StreamWriter writer = new StreamWriter(Application.dataPath + "/TrueTrace/Resources/Utility/MaterialMappings.xml")) {
                  var serializer = new XmlSerializer(typeof(Materials));
                  serializer.Serialize(writer.BaseStream, AssetManager.data);
               }
               Assets.NeedsToUpdateXML = false;
            }
            Cleared = false;
        }

      public static void PlayClip(AudioClip clip, int startSample = 0, bool loop = false)
      {
         float TimeElapsed = BuildWatch.GetSeconds();
         BuildWatch.Stop();
         if(RayTracingMaster.DoDing && TimeElapsed > 15.0f) {
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
        
            System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            MethodInfo method = audioUtilClass.GetMethod(
               "PlayPreviewClip",
               BindingFlags.Static | BindingFlags.Public,
               null,
               new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
               null
            );
            method.Invoke(
               null,
               new object[] { clip, startSample, loop }
            );
         }
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
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
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