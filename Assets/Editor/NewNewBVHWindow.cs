using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 using UnityEditor;
using CommonVars;
 using System.Xml;
 using System.IO;
 using UnityEngine.UIElements;
 using UnityEngine.Profiling;
using UnityEditor.UIElements;

public class EditModeFunctions : EditorWindow {
     [MenuItem("PathTracer/Pathtracer Settings")]
     public static void ShowWindow() {
         GetWindow<EditModeFunctions>("Pathtracing Settings");
     }

     public Toggle NEEToggle;
     public Button BVHBuild;
     public Button ScreenShotButton;
     public Button StaticButton;
     public Button ClearButton;
     public Button QuickStartButton;
     public Button ForceInstancesButton;

     // public FloatField BounceField;
     // public FloatField ResField;

     public RayTracingMaster RayMaster;
     public AssetManager Assets;

      public int BounceCount = 24;
      public float RenderRes = 1;
      public bool NEE = false;
      public bool Accumulate = true;
      public bool RR = true;
      public bool Moving = true;
      public bool Volumetrics = false;
      public float VolumDens = 0;
      public bool MeshSkin = true;
      public bool Bloom = false;
      public float BloomStrength = 0;
      public bool DoF = false;
      public float DoFAperature = 0;
      public float DoFFocal = 0;
      public bool Exposure = false;
      public bool ReSTIR = false;
      public bool SampleRegen = false;
      public bool Precompute = true;
      public bool ReSTIRTemporal = true;
      public int InitSampleCount = 32;
      public bool ReSTIRSpatial = true;
      public int ReSTIRSpatialSampleCount = 5;
      public int ReSTIRMCap = 32;
      public bool ReSTIRGI = false;
      public bool SampleValid = false;
      public int UpdateRate = 9;
      public bool GITemporal = true;
      public int GITemporalMCap = 12;
      public bool GISpatial = true;
      public int GISpatialSampleCount = 6;
      public bool SpatialStabalizer = false;
      public bool TAA = false;
      public bool SVGF = false;
      public bool SVGFAlternate = true;
      public int SVGFSize = 4;
      public bool ASVGF = false;
      public int ASVGFSize = 4;
      public bool ToneMap = true;
      public bool TAAU = true;
      public int AtmoScatter = 4;
      public bool ShowFPS = true;

      List<List<GameObject>> Objects;
      List<Mesh> SourceMeshes;

      private void OnStartAsyncCombined() {
         EditorUtility.SetDirty(GameObject.Find("Scene").GetComponent<AssetManager>());
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
            if(ChildObjects[i].GetComponent<ParentObject>() != null || ChildObjects[i].GetComponent<InstancedObject>() != null) {
               continue;
            }
            if(ChildObjects[i].GetComponent<RayTracingObject>() != null) {
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
               InstancedParent.AddComponent<ParentObject>();
               for(int i2 = Count - 1; i2 >= 0; i2--) {
                  DestroyImmediate(Objects[i][i2].GetComponent<RayTracingObject>());
                  Objects[i][i2].AddComponent<InstancedObject>();
                  Objects[i][i2].GetComponent<InstancedObject>().InstanceParent = InstancedParent.GetComponent<ParentObject>();;
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
            } else if(ChildObjects[i].GetComponent<MeshFilter>() != null) {
               ChildObjects[i].parent = Parent;
            } else if(ChildObjects[i].GetComponent<InstancedObject>() != null) {
               ChildObjects[i].parent = Source;
            } else {
               ChildObjects[i].parent = null;
            }
         }

      }

      private void QuickStart() {
         List<Transform> Children = new List<Transform>();
            for(int i = 0; i < Assets.transform.childCount; i++) {
                  Children.Add(Assets.transform.GetChild(i));   
               }
            List<Transform> Parents = new List<Transform>();
            while(Children.Count != 0) {
               Transform Child = Children[Children.Count - 1];//its much faster to read and remove from the end than the beginning
               Children.RemoveAt(Children.Count - 1);
               if(Child.GetComponent<InstancedObject>() != null) continue;
               if(Child.GetComponent<Light>() != null && Child.GetComponent<RayTracingLights>() == null) Child.gameObject.AddComponent<RayTracingLights>(); 
               if(!Child.gameObject.activeInHierarchy && !(Child.GetComponent<SkinnedMeshRenderer>() != null || Child.GetComponent<MeshFilter>() != null)) continue;
               if(Child.parent.GetComponent<ParentObject>() == null) {
                  if((Child.GetComponent<SkinnedMeshRenderer>() != null || Child.GetComponent<MeshFilter>() != null) && Child.GetComponent<ParentObject>() == null) {
                     if(Child.GetComponent<SkinnedMeshRenderer>() != null) {
                        bool AlreadyIsParented = false;
                        for(int i2 = 0; i2 < Parents.Count; i2++) {
                           if(Child.IsChildOf(Parents[i2])) AlreadyIsParented = true;                        
                        }
                        if(!AlreadyIsParented) {
                           Parents.Add(Child);
                           Child.gameObject.AddComponent<ParentObject>();
                        }
                     } else {
                        Child.gameObject.AddComponent<ParentObject>(); 
                     }
                  }
                  if((Child.GetComponent<SkinnedMeshRenderer>() != null || Child.GetComponent<MeshFilter>() != null)  && Child.GetComponent<RayTracingObject>() == null) {
                     Child.gameObject.AddComponent<RayTracingObject>(); 
                  }
               } else {
                  if((Child.GetComponent<SkinnedMeshRenderer>() != null || Child.GetComponent<MeshFilter>() != null)  && Child.GetComponent<RayTracingObject>() == null) {
                     Child.gameObject.AddComponent<RayTracingObject>();
                  }
               }

               bool HasChildrenMesh = false;
               for(int i = 0; i < Child.childCount; i++) {
                  Children.Add(Child.GetChild(i));   
                  if((Child.GetChild(i).GetComponent<MeshFilter>() != null) && Child.GetComponent<ParentObject>() == null && Child.GetChild(i).GetComponent<ParentObject>() == null) {
                     Child.gameObject.AddComponent<ParentObject>();
                           Debug.Log(Child.gameObject.name);
                  }
                  if((Child.GetChild(i).GetComponent<SkinnedMeshRenderer>() != null) && Child.GetComponent<ParentObject>() == null) {
                     bool AlreadyIsParented = false;
                     for(int i2 = 0; i2 < Parents.Count; i2++) {
                        if(Child.IsChildOf(Parents[i2])) AlreadyIsParented = true;                        
                     }
                     if(!AlreadyIsParented) {
                        Parents.Add(Child);
                        Child.gameObject.AddComponent<ParentObject>();
                     }
                  } else if(Child.GetComponent<ParentObject>() != null) {
                     Parents.Add(Child);
                  }
               }
            }
            Parents.Clear();
            Parents.TrimExcess();
            Children.TrimExcess();
      }

      public void OnFocus() {
        if(RayMaster == null) {
            Camera TempCam = Camera.main;
            if(TempCam.GetComponent<RayTracingMaster>() == null) TempCam.gameObject.AddComponent(typeof(RayTracingMaster));
            if(TempCam.GetComponent<FlyCamera>() == null) TempCam.gameObject.AddComponent(typeof(FlyCamera));
            RayMaster = Camera.main.GetComponent<RayTracingMaster>();
         }
        if(Assets == null) {
            if(GameObject.Find("Scene") == null) {
               List<GameObject> Objects = new List<GameObject>();
               UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(Objects);
               GameObject SceneObject = new GameObject("Scene", typeof(AssetManager));
               foreach(GameObject Obj in Objects) {
                  if(!Obj.Equals(Camera.main.gameObject)) {
                     Obj.transform.SetParent(SceneObject.transform);
                  }
               }
               Assets = GameObject.Find("Scene").GetComponent<AssetManager>();
               QuickStart();
            }
            if(GameObject.Find("InstancedStorage") == null) {
               GameObject InstanceObject = new GameObject("InstancedStorage", typeof(InstancedManager));
            }

            Assets = GameObject.Find("Scene").GetComponent<AssetManager>();

        }
      }

     public void CreateGUI() {
         OnFocus();
        RayMaster.bouncecount = BounceCount;
        RayMaster.RenderScale = RenderRes;
        RayMaster.UseRussianRoulette = RR;
        RayMaster.DoTLASUpdates = Moving;
        RayMaster.AllowConverge = Accumulate;
        RayMaster.UseNEE = NEE;
        RayMaster.AllowVolumetrics = Volumetrics;
        RayMaster.VolumeDensity = VolumDens;
        Assets.UseSkinning = MeshSkin;
        RayMaster.AllowBloom = Bloom;
        RayMaster.BloomStrength = BloomStrength * 128.0f;
        RayMaster.AllowDoF = DoF;
        RayMaster.DoFAperature = DoFAperature;
        RayMaster.DoFFocal = DoFFocal * 60.0f;
        RayMaster.AllowAutoExpose = Exposure;
        RayMaster.AllowReSTIR = ReSTIR;
        RayMaster.AllowReSTIRRegeneration = SampleRegen;
        RayMaster.AllowReSTIRPrecomputedSamples = Precompute;
        RayMaster.AllowReSTIRTemporal = ReSTIRTemporal;
        RayMaster.RISSampleCount = InitSampleCount;
        RayMaster.AllowReSTIRSpatial = ReSTIRSpatial;
        RayMaster.SpatialSamples = ReSTIRSpatialSampleCount;
        RayMaster.SpatialMCap = ReSTIRMCap;
        RayMaster.UseReSTIRGI = ReSTIRGI;
        RayMaster.UseReSTIRGITemporal = GITemporal;
        RayMaster.UseReSTIRGISpatial = GISpatial;
        RayMaster.DoReSTIRGIConnectionValidation = SampleValid;
        RayMaster.ReSTIRGIUpdateRate = UpdateRate;
        RayMaster.ReSTIRGITemporalMCap = GITemporalMCap;
        RayMaster.ReSTIRGISpatialCount = GISpatialSampleCount;
        RayMaster.ReSTIRGISpatialStabalizer = SpatialStabalizer;
        RayMaster.AllowTAA = TAA;
        RayMaster.UseSVGF = SVGF;
        RayMaster.AlternateSVGF = SVGFAlternate;
        RayMaster.SVGFAtrousKernelSizes = SVGFSize;
        RayMaster.UseASVGF = ASVGF;
        RayMaster.MaxIterations = ASVGFSize;
        RayMaster.AllowToneMap = ToneMap;
        RayMaster.UseTAAU = TAAU;
        RayMaster.AtmoNumLayers = AtmoScatter;





        BVHBuild = new Button(() => OnStartAsyncCombined()) {text = "Build Aggregated BVH"};
        BVHBuild.style.minWidth = 145;
        ScreenShotButton = new Button(() => {
            string dirPath = Application.dataPath + "/../Assets/ScreenShots";
            if(!System.IO.Directory.Exists(dirPath)) {
               Debug.Log("No Folder Named ScreenShots in Assets Folder.  Please Create One");
            } else {
               ScreenCapture.CaptureScreenshot(dirPath + "/" + System.DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ", " + RayMaster.SampleCount + " Samples.png");
               UnityEditor.AssetDatabase.Refresh();
            }
        }) {text = "Take Screenshot"};
        ScreenShotButton.style.minWidth = 100;
        StaticButton = new Button(() => OptimizeForStatic()) {text = "Make All Static"};
        StaticButton.style.minWidth = 105;
        
        ClearButton = new Button(() => {
            EditorUtility.SetDirty(Assets);
            Assets.ClearAll();
            InstancedManager Instanced = GameObject.Find("InstancedStorage").GetComponent<InstancedManager>();
            EditorUtility.SetDirty(Instanced);
            Instanced.ClearAll();
        }) {text = "Clear Parent Data"};
        ClearButton.style.minWidth = 145;
        QuickStartButton = new Button(() => QuickStart()) {text = "Quick Start"};
        QuickStartButton.style.minWidth = 111;
        ForceInstancesButton = new Button(() => ConstructInstances()) {text = "Force Instances"};

        Box ButtonField1 = new Box();
        ButtonField1.style.flexDirection = FlexDirection.Row;
        ButtonField1.Add(BVHBuild);
        ButtonField1.Add(ScreenShotButton);
        ButtonField1.Add(StaticButton);
        rootVisualElement.Add(ButtonField1);

        Box ButtonField2 = new Box();
        ButtonField2.style.flexDirection = FlexDirection.Row;
        ButtonField2.Add(ClearButton);
        ButtonField2.Add(QuickStartButton);
        ButtonField2.Add(ForceInstancesButton);
        rootVisualElement.Add(ButtonField2);

        Box TopEnclosingBox = new Box();
            TopEnclosingBox.style.flexDirection = FlexDirection.Row;
            FloatField BounceField = new FloatField() {value = BounceCount, label = "Max Bounces"};
            BounceField.ElementAt(0).style.minWidth = 75;
            BounceField.ElementAt(1).style.width = 25;
            BounceField.style.paddingRight = 40;
            TopEnclosingBox.Add(BounceField);
            BounceField.RegisterValueChangedCallback(evt => {BounceCount = (int)evt.newValue; RayMaster.bouncecount = BounceCount;});        
            FloatField ResField = new FloatField("Render Scale") {value = RenderRes};
            ResField.ElementAt(0).style.minWidth = 75;
            ResField.ElementAt(1).style.width = 25;
            TopEnclosingBox.Add(ResField);
            ResField.RegisterValueChangedCallback(evt => {RenderRes = evt.newValue; RayMaster.RenderScale = RenderRes;});        
        rootVisualElement.Add(TopEnclosingBox);

        Toggle RRToggle = new Toggle() {value = RR, text = "Use Russian Roulette"};
        rootVisualElement.Add(RRToggle);
        RRToggle.RegisterValueChangedCallback(evt => {RR = evt.newValue; RayMaster.UseRussianRoulette = RR;});

        Toggle MovingToggle = new Toggle() {value = Moving, text = "Enable Object Moving"};
        rootVisualElement.Add(MovingToggle);
        MovingToggle.RegisterValueChangedCallback(evt => {Moving = evt.newValue; RayMaster.DoTLASUpdates = Moving;});

        Toggle AccumToggle = new Toggle() {value = Accumulate, text = "Allow Image Accumulation"};
        rootVisualElement.Add(AccumToggle);
        AccumToggle.RegisterValueChangedCallback(evt => {Accumulate = evt.newValue; RayMaster.AllowConverge = Accumulate;});

        NEEToggle = new Toggle() {value = NEE, text = "Use Next Event Estimation"};
        rootVisualElement.Add(NEEToggle);
        NEEToggle.RegisterValueChangedCallback(evt => {NEE = evt.newValue; RayMaster.UseNEE = NEE;});

        VisualElement VolumetricBox = new VisualElement();
            Label VolumetricLabel = new Label("Volumetric Density");
            Slider VolumetricSlider = new Slider() {value = VolumDens, highValue = 1, lowValue = 0};
            VolumetricSlider.style.width = 100;
            Toggle VolumetricToggle = new Toggle() {value = Volumetrics, text = "Allow Volumetrics"};
            rootVisualElement.Add(VolumetricToggle);
            VolumetricToggle.RegisterValueChangedCallback(evt => {Volumetrics = evt.newValue; RayMaster.AllowVolumetrics = Volumetrics; if(evt.newValue) rootVisualElement.Insert(rootVisualElement.IndexOf(VolumetricToggle) + 1, VolumetricBox); else rootVisualElement.Remove(VolumetricBox);});        
            VolumetricSlider.RegisterValueChangedCallback(evt => {VolumDens = evt.newValue; RayMaster.VolumeDensity = VolumDens;});
            VolumetricBox.Add(VolumetricLabel);
            VolumetricBox.Add(VolumetricSlider);
            VolumetricBox.style.flexDirection = FlexDirection.Row;
        if(Volumetrics) rootVisualElement.Add(VolumetricBox);
        
    
        Toggle SkinToggle = new Toggle() {value = MeshSkin, text = "Allow Mesh Skinning"};
        rootVisualElement.Add(SkinToggle);
        SkinToggle.RegisterValueChangedCallback(evt => {MeshSkin = evt.newValue; Assets.UseSkinning = MeshSkin;});

        Toggle BloomToggle = new Toggle() {value = Bloom, text = "Enable Bloom"};
        VisualElement BloomBox = new VisualElement();
            Label BloomLabel = new Label("Bloom Strength");
            Slider BloomSlider = new Slider() {value = BloomStrength, highValue = 1.0f, lowValue = 0};
            BloomSlider.style.width = 100;
            BloomToggle.RegisterValueChangedCallback(evt => {Bloom = evt.newValue; RayMaster.AllowBloom = Bloom; if(evt.newValue) rootVisualElement.Insert(rootVisualElement.IndexOf(BloomToggle) + 1, BloomBox); else rootVisualElement.Remove(BloomBox);});        
            BloomSlider.RegisterValueChangedCallback(evt => {BloomStrength = evt.newValue; RayMaster.BloomStrength = BloomStrength * 128.0f;});
            rootVisualElement.Add(BloomToggle);
            BloomBox.style.flexDirection = FlexDirection.Row;
            BloomBox.Add(BloomLabel);
            BloomBox.Add(BloomSlider);
        if(Bloom) rootVisualElement.Add(BloomBox);

        Label AperatureLabel = new Label("Aperature Size");
        Slider AperatureSlider = new Slider() {value = DoFAperature, highValue = 1, lowValue = 0};
        AperatureSlider.style.width = 100;
        Label FocalLabel = new Label("Focal Length");
        Slider FocalSlider = new Slider() {value = DoFFocal, highValue = 1, lowValue = 0};
        FocalSlider.style.width = 100;
        Box AperatureBox = new Box();
        AperatureBox.Add(AperatureLabel);
        AperatureBox.Add(AperatureSlider);
        AperatureBox.style.flexDirection = FlexDirection.Row;
        Box FocalBox = new Box();
        FocalBox.Add(FocalLabel);
        FocalBox.Add(FocalSlider);
        FocalBox.style.flexDirection = FlexDirection.Row;

        Toggle DoFToggle = new Toggle() {value = DoF, text = "Enable DoF"};
        VisualElement DoFFoldout = new VisualElement();
        DoFFoldout.Add(AperatureBox);
        DoFFoldout.Add(FocalBox);
        rootVisualElement.Add(DoFToggle);
        DoFToggle.RegisterValueChangedCallback(evt => {DoF = evt.newValue; RayMaster.AllowDoF = DoF;if(evt.newValue) rootVisualElement.Insert(rootVisualElement.IndexOf(DoFToggle) + 1, DoFFoldout); else rootVisualElement.Remove(DoFFoldout);});        
        AperatureSlider.RegisterValueChangedCallback(evt => {DoFAperature = evt.newValue; RayMaster.DoFAperature = DoFAperature;});
        FocalSlider.RegisterValueChangedCallback(evt => {DoFFocal = evt.newValue; RayMaster.DoFFocal = DoFFocal * 60.0f;});
        if(DoF) rootVisualElement.Add(DoFFoldout);
        Toggle ExposureToggle = new Toggle() {value = Exposure, text = "Enable Auto Exposure"};
        rootVisualElement.Add(ExposureToggle);
        ExposureToggle.RegisterValueChangedCallback(evt => {Exposure = evt.newValue; RayMaster.AllowAutoExpose = Exposure;});



        VisualElement ReSTIRFoldout = new VisualElement() {};
        Toggle ReSTIRToggle = new Toggle() {value = ReSTIR, text = "Use ReSTIR"};
        Box EnclosingReSTIR = new Box();
            Box ReSTIRInitialBox = new Box();
                ReSTIRInitialBox.style.flexDirection = FlexDirection.Row;
                Toggle SampleRegenToggle = new Toggle() {value = SampleRegen, text = "Allow Sample Regeneration"};
                Toggle PrecomputeToggle = new Toggle() {value = Precompute, text = "Allow Sample Precomputation"};
                Label InitSampleCountLabel = new Label("Initial Sample Count");
                FloatField InitSampleCountField = new FloatField() {value = InitSampleCount};
                SampleRegenToggle.RegisterValueChangedCallback(evt => {SampleRegen = evt.newValue; RayMaster.AllowReSTIRRegeneration = SampleRegen;});
                PrecomputeToggle.RegisterValueChangedCallback(evt => {Precompute = evt.newValue; RayMaster.AllowReSTIRPrecomputedSamples = Precompute;});
                InitSampleCountField.RegisterValueChangedCallback(evt => {InitSampleCount = (int)evt.newValue; RayMaster.RISSampleCount = InitSampleCount;});
                ReSTIRInitialBox.Add(SampleRegenToggle);
                ReSTIRInitialBox.Add(PrecomputeToggle);
                ReSTIRInitialBox.Add(InitSampleCountField);
                ReSTIRInitialBox.Add(InitSampleCountLabel);
            EnclosingReSTIR.Add(ReSTIRInitialBox);
            Toggle ReSTIRTemporalToggle = new Toggle() {value = ReSTIRTemporal, text = "Allow ReSTIR Temporal"};
            EnclosingReSTIR.Add(ReSTIRTemporalToggle);
            ReSTIRTemporalToggle.RegisterValueChangedCallback(evt => {ReSTIRTemporal = evt.newValue; RayMaster.AllowReSTIRTemporal = ReSTIRTemporal;});
            Box ReSTIRSpatialBox = new Box();
                ReSTIRSpatialBox.style.flexDirection = FlexDirection.Row;
                Toggle ReSTIRSpatialToggle = new Toggle() {value = ReSTIRSpatial, text = "Allow ReSTIR Spatial"};
                Label ReSTIRSpatialSampleCountLabel = new Label("Spatial Sample Count");
                FloatField ReSTIRSpatialSampleCountField = new FloatField() {value = ReSTIRSpatialSampleCount};
                Label ReSTIRSpatialMCapLabel = new Label("Spatial M Cap");
                FloatField ReSTIRSpatialMCapField = new FloatField() {value = ReSTIRMCap};
                ReSTIRSpatialToggle.RegisterValueChangedCallback(evt => {ReSTIRSpatial = evt.newValue; RayMaster.AllowReSTIRSpatial = ReSTIRSpatial;});
                ReSTIRSpatialSampleCountField.RegisterValueChangedCallback(evt => {ReSTIRSpatialSampleCount = (int)evt.newValue; RayMaster.SpatialSamples = ReSTIRSpatialSampleCount;});
                ReSTIRSpatialMCapField.RegisterValueChangedCallback(evt => {ReSTIRMCap = (int)evt.newValue; RayMaster.SpatialMCap = ReSTIRMCap;});
                ReSTIRSpatialBox.Add(ReSTIRSpatialToggle);
                ReSTIRSpatialBox.Add(ReSTIRSpatialSampleCountLabel);
                ReSTIRSpatialBox.Add(ReSTIRSpatialSampleCountField);
                ReSTIRSpatialBox.Add(ReSTIRSpatialMCapLabel);
                ReSTIRSpatialBox.Add(ReSTIRSpatialMCapField);
            EnclosingReSTIR.Add(ReSTIRSpatialBox);
        ReSTIRFoldout.Add(EnclosingReSTIR);
        rootVisualElement.Add(ReSTIRToggle);
        ReSTIRToggle.RegisterValueChangedCallback(evt => {ReSTIR = evt.newValue; RayMaster.AllowReSTIR = ReSTIR;if(evt.newValue) rootVisualElement.Insert(rootVisualElement.IndexOf(ReSTIRToggle) + 1, ReSTIRFoldout); else rootVisualElement.Remove(ReSTIRFoldout);});
        if(ReSTIR) rootVisualElement.Add(ReSTIRFoldout);

        Toggle GIToggle = new Toggle() {value = ReSTIRGI, text = "Use ReSTIR GI"};
        VisualElement GIFoldout = new VisualElement() {};
        Box EnclosingGI = new Box();
            Box TopGI = new Box();
                TopGI.style.flexDirection = FlexDirection.Row;
                Toggle SampleValidToggle = new Toggle() {value = SampleValid, text = "Do Sample Connection Validation"};
                Label GIUpdateRateLabel = new Label("ReSTIR GI Update Rate(0 is off)");
                FloatField GIUpdateRateField = new FloatField() {value = UpdateRate};
                SampleValidToggle.RegisterValueChangedCallback(evt => {SampleValid = evt.newValue; RayMaster.DoReSTIRGIConnectionValidation = SampleValid;});
                GIUpdateRateField.RegisterValueChangedCallback(evt => {UpdateRate = (int)evt.newValue; RayMaster.ReSTIRGIUpdateRate = UpdateRate;});
                TopGI.Add(SampleValidToggle);
                TopGI.Add(GIUpdateRateField);
                TopGI.Add(GIUpdateRateLabel);
            EnclosingGI.Add(TopGI);
            Box TemporalGI = new Box();
                TemporalGI.style.flexDirection = FlexDirection.Row;
                Toggle TemporalGIToggle = new Toggle() {value = GITemporal, text = "Enable Temporal"};
                Label TemporalGIMCapLabel = new Label("Temporal M Cap(0 is off)");
                FloatField TeporalGIMCapField = new FloatField() {value = GITemporalMCap};
                TemporalGIToggle.RegisterValueChangedCallback(evt => {GITemporal = evt.newValue; RayMaster.UseReSTIRGITemporal = GITemporal;});
                TeporalGIMCapField.RegisterValueChangedCallback(evt => {GITemporalMCap = (int)evt.newValue; RayMaster.ReSTIRGITemporalMCap = GITemporalMCap;});
                TemporalGI.Add(TemporalGIToggle);
                TemporalGI.Add(TeporalGIMCapField);
                TemporalGI.Add(TemporalGIMCapLabel);
            EnclosingGI.Add(TemporalGI);
            Box SpatialGI = new Box();
                SpatialGI.style.flexDirection = FlexDirection.Row;
                Toggle SpatialGIToggle = new Toggle() {value = GISpatial, text = "Enable Spatial"};
                Label SpatialGISampleCountLabel = new Label("Spatial Sample Count");
                FloatField SpatialGISampleCountField = new FloatField() {value = GISpatialSampleCount};
                Toggle StabalizerToggle = new Toggle() {value = SpatialStabalizer, text = "Enable Spatial Stabalizer"};
                SpatialGIToggle.RegisterValueChangedCallback(evt => {GISpatial = evt.newValue; RayMaster.UseReSTIRGISpatial = GISpatial;});
                SpatialGISampleCountField.RegisterValueChangedCallback(evt => {GISpatialSampleCount = (int)evt.newValue; RayMaster.ReSTIRGISpatialCount = GISpatialSampleCount;});
                StabalizerToggle.RegisterValueChangedCallback(evt => {SpatialStabalizer = evt.newValue; RayMaster.ReSTIRGISpatialStabalizer = SpatialStabalizer;});
                SpatialGI.Add(SpatialGIToggle);
                SpatialGI.Add(SpatialGISampleCountField);
                SpatialGI.Add(SpatialGISampleCountLabel);
                SpatialGI.Add(StabalizerToggle);
            EnclosingGI.Add(SpatialGI);
        GIFoldout.Add(EnclosingGI);
        rootVisualElement.Add(GIToggle);
        GIToggle.RegisterValueChangedCallback(evt => {ReSTIRGI = evt.newValue; RayMaster.UseReSTIRGI = ReSTIRGI;if(evt.newValue) rootVisualElement.Insert(rootVisualElement.IndexOf(GIToggle) + 1, GIFoldout); else rootVisualElement.Remove(GIFoldout);});
        if(ReSTIRGI) rootVisualElement.Add(GIFoldout);
    
        Toggle TAAToggle = new Toggle() {value = TAA, text = "Enable TAA"};
        rootVisualElement.Add(TAAToggle);
        TAAToggle.RegisterValueChangedCallback(evt => {TAA = evt.newValue; RayMaster.AllowTAA = TAA;});

        Toggle SVGFToggle = new Toggle() {value = SVGF, text = "Enable SVGF"};
        VisualElement SVGFFoldout = new VisualElement() {};
            SVGFFoldout.style.flexDirection = FlexDirection.Row;
            FloatField SVGFSizeField = new FloatField("SVGF Atrous Kernel Size") {value = SVGFSize};
            SVGFSizeField.RegisterValueChangedCallback(evt => {SVGFSize = (int)evt.newValue; RayMaster.SVGFAtrousKernelSizes = SVGFSize;});
            Toggle SVGFAlternateToggle = new Toggle() {value = SVGFAlternate, text = "Use Alternate SVGF"};
            SVGFAlternateToggle.RegisterValueChangedCallback(evt => {SVGFAlternate = evt.newValue; RayMaster.AlternateSVGF = SVGFAlternate;});
            SVGFFoldout.Add(SVGFAlternateToggle);
            SVGFFoldout.Add(SVGFSizeField);
        rootVisualElement.Add(SVGFToggle);
        SVGFToggle.RegisterValueChangedCallback(evt => {SVGF = evt.newValue; RayMaster.UseSVGF = SVGF;if(evt.newValue) rootVisualElement.Insert(rootVisualElement.IndexOf(SVGFToggle) + 1, SVGFFoldout); else rootVisualElement.Remove(SVGFFoldout);});
        if(SVGF) rootVisualElement.Add(SVGFFoldout);

        Toggle ASVGFToggle = new Toggle() {value = ASVGF, text = "Enable A-SVGF"};
        VisualElement ASVGFFoldout = new VisualElement() {};
            ASVGFFoldout.style.flexDirection = FlexDirection.Row;
            FloatField ASVGFSizeField = new FloatField("ASVGF Atrous Kernel Size") {value = ASVGFSize};
            ASVGFSizeField.RegisterValueChangedCallback(evt => {ASVGFSize = (int)evt.newValue; ASVGFSize = Mathf.Max(ASVGFSize, 4); ASVGFSize = Mathf.Min(ASVGFSize, 6); RayMaster.MaxIterations = ASVGFSize;});
            ASVGFFoldout.Add(ASVGFSizeField);
        rootVisualElement.Add(ASVGFToggle);
        ASVGFToggle.RegisterValueChangedCallback(evt => {ASVGF = evt.newValue; RayMaster.UseASVGF = ASVGF;if(evt.newValue) rootVisualElement.Insert(rootVisualElement.IndexOf(ASVGFToggle) + 1, ASVGFFoldout); else rootVisualElement.Remove(ASVGFFoldout);});
        if(ASVGF) rootVisualElement.Add(ASVGFFoldout);

        Toggle ToneMapToggle = new Toggle() {value = ToneMap, text = "Enable Tonemapping"};
        rootVisualElement.Add(ToneMapToggle);
        ToneMapToggle.RegisterValueChangedCallback(evt => {ToneMap = evt.newValue; RayMaster.AllowToneMap = ToneMap;});

        Toggle TAAUToggle = new Toggle() {value = TAAU, text = "Enable TAAU"};
        rootVisualElement.Add(TAAUToggle);
        TAAUToggle.RegisterValueChangedCallback(evt => {TAAU = evt.newValue; RayMaster.UseTAAU = TAAU;});

        VisualElement AtmoBox = new VisualElement();
            AtmoBox.style.flexDirection = FlexDirection.Row;
            FloatField AtmoScatterField = new FloatField("Atmospheric Scattering Samples") {value = AtmoScatter};
            AtmoScatterField.RegisterValueChangedCallback(evt => {AtmoScatter = (int)evt.newValue; RayMaster.AtmoNumLayers = AtmoScatter;});
            AtmoBox.Add(AtmoScatterField);
        rootVisualElement.Add(AtmoBox);

        Toggle SampleShowToggle = new Toggle() {value = ShowFPS, text = "Show Sample Count"};
        SerializedObject so = new SerializedObject(RayMaster);
        VisualElement SampleCountBox = new VisualElement();
            SampleCountBox.style.flexDirection = FlexDirection.Row;
            IntegerField SampleCountField = new IntegerField("Current Sample Count") {bindingPath = "SampleCount"};
            SampleCountField.Bind(so);
            SampleCountBox.Add(SampleCountField);
        rootVisualElement.Add(SampleShowToggle);
        SampleShowToggle.RegisterValueChangedCallback(evt => {ShowFPS = evt.newValue; if(evt.newValue) rootVisualElement.Insert(rootVisualElement.IndexOf(SampleShowToggle) + 1, SampleCountBox); else rootVisualElement.Remove(SampleCountBox);});
        if(ShowFPS) rootVisualElement.Add(SampleCountBox);

     }

}
