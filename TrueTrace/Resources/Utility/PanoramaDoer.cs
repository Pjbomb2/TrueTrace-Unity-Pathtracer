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
        }
        public void OnDisable() {
            if(RayMaster.DoChainedImages) RemoveResolution(GetCount() - 1);
        }
        private void FinalizePanorama() {
            Color[] FinalAtlasData = new Color[FinalAtlasSize.x * FinalAtlasSize.y];

            for(int iter = 0; iter < HorizontalSegments; iter++) {
                int width = TexArray[iter].width;
                int height = TexArray[iter].height;
                Color[] CurrentData = TexArray[iter].GetPixels(0);
                int XOffset = iter * Mathf.CeilToInt((float)FinalAtlasSize.x / (float)HorizontalSegments);
                // int YOffset = iter * Mathf.CeilToInt(5000.0f / 5000.0f);
                for(int i = 0; i < width; i++) {
                    for(int j = 0; j < height; j++) {
                        int IndexChild = i + j * width;
                        int IndexFinal = (i + XOffset) + (j) * FinalAtlasSize.x;
                        FinalAtlasData[IndexFinal] = new Color(CurrentData[IndexChild].r, CurrentData[IndexChild].g, CurrentData[IndexChild].b, 1); 
                    }
                }
                DestroyImmediate(TexArray[iter]);
            }
            Texture2D FinalAtlas = new Texture2D(FinalAtlasSize.x, FinalAtlasSize.y);
            FinalAtlas.SetPixels(FinalAtlasData, 0);
            FinalAtlas.Apply();
            System.IO.File.WriteAllBytes(PlayerPrefs.GetString("ScreenShotPath") + "/" + System.DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".png", FinalAtlas.EncodeToPNG()); 
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
                RayMaster.CurrentHorizonalPatch = new Vector2((float)CurrentSegment / (float)HorizontalSegments, (float)(CurrentSegment + 1) / (float)HorizontalSegments);
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
                        FinalizePanorama();
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
            StartCoroutine(RecordFrame());
        }
    }
}
#endif