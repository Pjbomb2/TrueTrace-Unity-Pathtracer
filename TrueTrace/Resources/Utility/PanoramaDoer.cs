#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
 using UnityEngine.Profiling;
using System.Reflection;

namespace TrueTrace {
    public class PanoramaDoer : MonoBehaviour
    {
        public int Padding = 32;
        float PaddingHalfValue = 0;
        public bool DoPanorama = true;
        RayTracingMaster RayMaster;
        public Camera[] Cameras;
        public float TimeBetweenSegments = 10f;
        public int MaxSamples = 10000;
        private int CurrentCamera = 0;
        public Vector2Int FinalAtlasSize = new Vector2Int(10000, 5000);
        private bool PrevPanorama = false;
        private float waitedTime = 0;
        public int HorizontalSegments = 10;
        private int CurrentSegment = 0;
        private Texture2D[] TexArray; 

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


    void SetResolution(int index)
    {
        Type gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        PropertyInfo selectedSizeIndex = gameView.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        EditorWindow window = EditorWindow.GetWindow(gameView);
        selectedSizeIndex.SetValue(window, index, null);
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

        public void Start() {
            RayMaster = GameObject.Find("Scene").GetComponent<RayTracingMaster>();
            // HorizontalSegments = Mathf.CeilToInt(FinalAtlasSize.x / (float)Screen.width); 
            Application.runInBackground = true;
            RayMaster.DoPanorama = false;
            RayMaster.DoChainedImages = false;
            if(DoPanorama) {
                AddResolution(Mathf.CeilToInt((float)FinalAtlasSize.x / (float)HorizontalSegments) + Padding, FinalAtlasSize.y, "TempPanoramaSize");
                SetResolution(GetCount() - 1);
            } else {
                AddResolution(FinalAtlasSize.x, FinalAtlasSize.y, "TempPanoramaSize");
                SetResolution(GetCount() - 1);
                HorizontalSegments = 1;
            }
            Init();
            RayMaster.DoPanorama = DoPanorama;
            RayMaster.DoChainedImages = true;
        }
        public void OnDisable() {
            if(RayMaster.DoChainedImages) RemoveResolution(GetCount() - 1);
        }
        private void FinalizePanorama(Camera cam) {
            Color[] FinalAtlasData = new Color[FinalAtlasSize.x * FinalAtlasSize.y];

            for(int iter = 0; iter < HorizontalSegments; iter++) {
                int width = TexArray[iter].width - Padding;
                int height = TexArray[iter].height;
                Color[] CurrentData = TexArray[iter].GetPixels(0);
                int XOffset = iter * Mathf.CeilToInt((float)FinalAtlasSize.x / (float)HorizontalSegments);
                // int YOffset = iter * Mathf.CeilToInt(5000.0f / 5000.0f);
                for(int i = 0; i < width + Padding; i++) {
                    for(int j = 0; j < height; j++) {
                        int IndexChild = i + j * (width + Padding);
                        int IndexFinal = (i + XOffset - (Padding / 2)) + (j) * FinalAtlasSize.x;
                        if(i >= Padding / 2 && iter != HorizontalSegments - 1) FinalAtlasData[IndexFinal] = new Color(CurrentData[IndexChild].r, CurrentData[IndexChild].g, CurrentData[IndexChild].b, 1); 
                        else if(iter != 0 && iter != HorizontalSegments - 1) {
                            float Ratio = 1;
                            if(i < Padding / 2) {
                                Ratio = 1.0f - ((float)i / (float)(Padding / 2));
                            }
                            FinalAtlasData[IndexFinal] = new Color((FinalAtlasData[IndexFinal].r * Ratio + CurrentData[IndexChild].r * (1.0f - Ratio)), (FinalAtlasData[IndexFinal].g * Ratio + CurrentData[IndexChild].g * (1.0f - Ratio)), (FinalAtlasData[IndexFinal].b * (Ratio) + CurrentData[IndexChild].b * (1.0f - Ratio)), 1); 
                        } else if(iter == HorizontalSegments - 1 && i < width + (Padding / 2)) {
                            FinalAtlasData[IndexFinal] = new Color(CurrentData[IndexChild].r, CurrentData[IndexChild].g, CurrentData[IndexChild].b, 1); 
                        }
                    }
                }
                DestroyImmediate(TexArray[iter]);
            }
            Texture2D FinalAtlas = new Texture2D(FinalAtlasSize.x, FinalAtlasSize.y);
            FinalAtlas.SetPixels(FinalAtlasData, 0);
            FinalAtlas.Apply();
            System.IO.File.WriteAllBytes(PlayerPrefs.GetString("PanoramaPath") + "/" + cam.gameObject.name + ".png", FinalAtlas.EncodeToPNG()); 
        }
        public void Init() {
            RayMaster.SampleCount = 0;
            RayMaster.FramesSinceStart = 0;          
            RayMaster._currentSample = 0;
            TexArray = new Texture2D[HorizontalSegments];
            PrevPanorama = true;
            CurrentCamera = 0;
            if(Cameras == null || Cameras.Length == 0) {
                Cameras = new Camera[1];
                if(RayTracingMaster._camera != null)
                    Cameras[0] = RayTracingMaster._camera;
            }
            if(!(Cameras == null || Cameras.Length == 0)) {
                Cameras[0].gameObject.SetActive(true);
                for(int i = 1; i < Cameras.Length; i++) Cameras[i].gameObject.SetActive(false);
                Camera[] AllCameras = GameObject.FindObjectsOfType<Camera>();
                for(int i = 0; i < AllCameras.Length; i++) if(!Cameras[0].Equals(AllCameras[i])) AllCameras[i].gameObject.SetActive(false);
            }

        }

        IEnumerator RecordFrame()
        {
            yield return new WaitForEndOfFrame();
            if(RayMaster.DoChainedImages && TexArray != null) {
                if(PrevPanorama) {
                    PrevPanorama = false;
                    RayMaster.SampleCount = 0;
                    RayMaster.FramesSinceStart = 0;                    
                }
                PaddingHalfValue = (Padding / 2.0f) / (float)FinalAtlasSize.x;
                RayMaster.CurrentHorizonalPatch = new Vector2((float)CurrentSegment / (float)HorizontalSegments - PaddingHalfValue, (float)(CurrentSegment + 1) / (float)HorizontalSegments + PaddingHalfValue);
                waitedTime += Time.deltaTime;
                if (RayMaster.FramesSinceStart >= MaxSamples || waitedTime >= TimeBetweenSegments) {
                    waitedTime = 0;
                    if(!System.IO.Directory.Exists(Application.dataPath.Replace("/Assets", "") + "/TempPanoramas")) {
                        System.IO.Directory.CreateDirectory(Application.dataPath.Replace("/Assets", "") + "/TempPanoramas");
                    }
                    ScreenCapture.CaptureScreenshot(Application.dataPath.Replace("/Assets", "") + "/TempPanoramas/" + CurrentSegment + ".png");
                    TexArray[CurrentSegment] = ScreenCapture.CaptureScreenshotAsTexture();
                    CurrentSegment++;
                    RayMaster.SampleCount = 0;
                    RayMaster.FramesSinceStart = 0;
                    RayMaster._currentSample = 0;
                    PrevPanorama = true;
                    if(CurrentSegment == HorizontalSegments) {
                        CurrentSegment = 0;
                        waitedTime = 0;
                        FinalizePanorama(Cameras[CurrentCamera]);
                        CurrentCamera++;
                        if(CurrentCamera < Cameras.Length) {
                            Cameras[CurrentCamera - 1].gameObject.SetActive(false);
                            Cameras[CurrentCamera].gameObject.SetActive(true);
                            RayMaster.TossCamera(Cameras[CurrentCamera]);
                        } else {                            
                            RemoveResolution(GetCount() - 1);
                            RayMaster.DoPanorama = false;
                            RayMaster.DoChainedImages = false;
                            Application.runInBackground = false;
                            EditorApplication.isPlaying = false;
                        }

                    }
                }
            }
        }
        public void LateUpdate()
        {
            RayMaster.DoPanorama = DoPanorama;
            RayMaster.DoChainedImages = true;
            StartCoroutine(RecordFrame());
        }
    }
}
#endif