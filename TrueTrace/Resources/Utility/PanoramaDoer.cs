using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
namespace TrueTrace {
    public class PanoramaDoer : MonoBehaviour
    {
        RayTracingMaster RayMaster;
        public float TimeBetweenSegments = 10f;
        public int MaxSamples = 10000;
        private bool PrevPanorama = false;
        private float waitedTime = 0;
        private int HorizontalSegments = 10;
        private int CurrentSegment = 0;
        private Texture2D[] TexArray; 
        public void Start() {
            RayMaster = GameObject.Find("Scene").GetComponent<RayTracingMaster>();
            HorizontalSegments = Mathf.CeilToInt(10000.0f / (float)Screen.width); 
            Application.runInBackground = true;
            RayMaster.DoPanorama = false;
        }
        private void FinalizePanorama() {
            Color[] FinalAtlasData = new Color[10000 * 5000];

            for(int iter = 0; iter < HorizontalSegments; iter++) {
                int width = TexArray[iter].width;
                int height = TexArray[iter].height;
                Color[] CurrentData = TexArray[iter].GetPixels(0);
                int XOffset = iter * Mathf.CeilToInt(10000.0f / (float)HorizontalSegments);
                // int YOffset = iter * Mathf.CeilToInt(5000.0f / 5000.0f);
                for(int i = 0; i < width; i++) {
                    for(int j = 0; j < height; j++) {
                        int IndexChild = i + j * width;
                        int IndexFinal = (i + XOffset) + (j) * 10000;
                        FinalAtlasData[IndexFinal] = CurrentData[IndexChild]; 
                    }
                }
                DestroyImmediate(TexArray[iter]);
            }
            TexArray = null;
            Texture2D FinalAtlas = new Texture2D(10000, 5000);
            FinalAtlas.SetPixels(FinalAtlasData, 0);
            FinalAtlas.Apply();
            System.IO.File.WriteAllBytes(PlayerPrefs.GetString("ScreenShotPath") + "/" + System.DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".png", FinalAtlas.EncodeToPNG()); 

            Application.runInBackground = false;
            EditorApplication.isPlaying = false;
        }
        public void Init() {
            RayMaster.SampleCount = 0;
            RayMaster.FramesSinceStart = 0;          
            RayMaster._currentSample = 0;
            TexArray = new Texture2D[HorizontalSegments];
            PrevPanorama = true;
        }

        IEnumerator RecordFrame()
        {
            yield return new WaitForEndOfFrame();
            if(RayMaster.DoPanorama && TexArray != null) {
                if(PrevPanorama) {
                    PrevPanorama = false;
                    RayMaster.SampleCount = 0;
                    RayMaster.FramesSinceStart = 0;                    
                }
                RayMaster.CurrentHorizonalPatch = new Vector2((float)CurrentSegment / (float)HorizontalSegments, (float)(CurrentSegment + 1) / (float)HorizontalSegments);
                waitedTime += Time.deltaTime;
                if (RayMaster.FramesSinceStart >= MaxSamples || waitedTime >= TimeBetweenSegments) {
                    waitedTime = 0;
                    if(!System.IO.Directory.Exists(Application.dataPath + "/TempPanoramas")) {
                       AssetDatabase.CreateFolder("Assets", "TempPanoramas");
                    }
                    ScreenCapture.CaptureScreenshot(Application.dataPath + "/TempPanoramas/" + CurrentSegment + ".png");
                    TexArray[CurrentSegment] = ScreenCapture.CaptureScreenshotAsTexture();
                    CurrentSegment++;
                    RayMaster.SampleCount = 0;
                    RayMaster.FramesSinceStart = 0;
                    RayMaster._currentSample = 0;
                    PrevPanorama = true;
                    if(CurrentSegment == HorizontalSegments) {
                        CurrentSegment = 0;
                        waitedTime = 0;
                        RayMaster.DoPanorama = false;
                        FinalizePanorama();
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