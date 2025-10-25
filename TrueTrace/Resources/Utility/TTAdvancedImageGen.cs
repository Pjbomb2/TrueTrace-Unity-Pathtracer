#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace TrueTrace {
    [System.Serializable]
    public class TTAdvancedImageGen : MonoBehaviour
    {
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
      public static Texture2D ConvertTexture (RenderTexture RefTex) {
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
         return tex;
      }


      public static void IncrementRenderCounter() {
        #if TTIncrementRenderCounter
         string Path = PlayerPrefs.GetString("CounterPath") + "/TTStats.txt";
         List<string> RenderStatData = new List<string>();
         DateTime CurrentDate = DateTime.Now;
         string FormattedDate = CurrentDate.ToString("yyyy-MM-dd");
         if(System.IO.File.Exists(Path)) {
            using (System.IO.StreamReader sr = new System.IO.StreamReader(Path)) {
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
         using (System.IO.StreamWriter sw = new System.IO.StreamWriter(Path)) {
            int Coun = RenderStatData.Count;
            for(int i = 0; i < Coun; i++) {
               sw.WriteLine(RenderStatData[i]);
            }
         }
        #endif
      }
        public enum ImageGenType {NULL, Panorama, LargeScreenShot, TurnTable, TimedScreenShot, TimelineShooter};
        [SerializeField] public ImageGenType SelectedFunctionality = ImageGenType.NULL;
        private int CurrentResIndex;
        private TTSettings InitialSettings;
        [SerializeField] public int SamplesBetweenShots = 1000;
        [SerializeField] public bool ResetSampCountAfterShot = false;
        [Serializable]
        public class CameraListData {
            public Camera TargCam;
            public TTSettings CamSettings;
#if MultiMapScreenshot
            public bool SaveImgMap;
#endif
            public bool ActivateCam;
            public UnityEngine.Playables.PlayableDirector OptionalDirector;
        }
        [SerializeField] public List<CameraListData> CameraList;

        [Serializable]
        public class TurnTableData {
            private int CurrentSegment = 0;
            private int CurrentCamera = 0;
            private float waitedTime = 0;
            private bool PrevImage = false;
            public bool Running = false;
            public List<CameraListData> CameraList;
            [System.Serializable]
            public struct CamData {
                public float TimeBetweenSegments;
                public int MaxSamples;
                public int HorizontalResolution;
                public Vector3 Center;
                public float Distance;
                [Range(-89.9f, 89.9f)]public float Pitch;
            }
            public List<CamData> CamSettings;
            public TurnTableData(ref List<CameraListData> CamList) {
                CamSettings = new List<CamData>();
                CameraList = CamList;
            }
            public void SetInitialSettings(List<CameraListData> CamList) {
                CameraList = CamList;
                TTInterface.SetTTSettings(CameraList[0].CamSettings);
            }
            public IEnumerator RecordFrame()
            {
                yield return new WaitForEndOfFrame();
                    if(PrevImage) {
                        PrevImage = false;
                        RayTracingMaster.SampleCount = 0;
                        RayTracingMaster.RayMaster.FramesSinceStart = 0;                    
                    }
                    waitedTime += Time.deltaTime;
                    if(!Running) {
                        Running = true;
                    }
                    if (!CameraList[CurrentCamera].ActivateCam || RayTracingMaster.RayMaster.FramesSinceStart >= CamSettings[CurrentCamera].MaxSamples || waitedTime >= CamSettings[CurrentCamera].TimeBetweenSegments) {
                        if(CameraList[CurrentCamera].ActivateCam) {
                            waitedTime = 0;
                            string SegmentNumber = "";
                            int TempSeg = CurrentSegment;
                            int[] NumSegments = new int[3];
                            for(int i = 0; i < 3; i++) {
                                NumSegments[i] = ((TempSeg) % 10);
                                TempSeg /= 10;
                            }
                            for(int i = 0; i < 3; i++) {
                                SegmentNumber += NumSegments[2 - i];
                                if(i < 2) {
                                    SegmentNumber += "_";
                                }
                            }
                            if(!System.IO.Directory.Exists(PlayerPrefs.GetString("TurnTablePath") + "/" + CameraList[CurrentCamera].TargCam.gameObject.name.Replace(" ", ""))) {
                                System.IO.Directory.CreateDirectory(PlayerPrefs.GetString("TurnTablePath") + "/" + CameraList[CurrentCamera].TargCam.gameObject.name.Replace(" ", ""));
                            }
                            string Path = PlayerPrefs.GetString("TurnTablePath") + "/" + CameraList[CurrentCamera].TargCam.gameObject.name.Replace(" ", "") + "/" + CameraList[CurrentCamera].TargCam.gameObject.name + "." + SegmentNumber + ".png";
                            ScreenCapture.CaptureScreenshot(Path);
                            IncrementRenderCounter();
                            #if MultiMapScreenshot
                                if(CameraList[CurrentCamera].SaveImgMap) {
                                   SaveTexture(RayTracingMaster.RayMaster.MultiMapMatIDTexture, Path.Replace(".png", "") + "_MatID.png");
                                   SaveTexture(RayTracingMaster.RayMaster.MultiMapMeshIDTexture, Path.Replace(".png", "") + "_MeshID.png");
                                }
                            #endif
                            CurrentSegment++;
                            CameraList[CurrentCamera].TargCam.gameObject.transform.RotateAround(CamSettings[CurrentCamera].Center, Vector3.up, (360.0f / (float)CamSettings[CurrentCamera].HorizontalResolution));
                            RayTracingMaster.SampleCount = 0;
                            RayTracingMaster.RayMaster.FramesSinceStart = 0;
                            RayTracingMaster.RayMaster._currentSample = 0;
                            PrevImage = true;
                        }
                        if(CameraList[CurrentCamera].ActivateCam || CurrentSegment == CamSettings[CurrentCamera].HorizontalResolution) {
                            CurrentSegment = 0;
                            waitedTime = 0;
                            CurrentCamera++;
                            if(CurrentCamera < CamSettings.Count) {
                                CameraList[CurrentCamera - 1].TargCam.gameObject.SetActive(false);
                                CameraList[CurrentCamera].TargCam.gameObject.SetActive(true);
                                RayTracingMaster.RayMaster.TossCamera(CameraList[CurrentCamera].TargCam);
                                TTInterface.SetTTSettings(CameraList[CurrentCamera].CamSettings);
                            } else {                            
                                Application.runInBackground = false;
                                EditorApplication.isPlaying = false;
                            }

                        }

                    }
            }

        }

        [SerializeField] public TurnTableData TurnTableSettings;


        [Serializable]
        public class TimelineShooterData {
            private int CurrentCamera = 0;
            private int CurrentFrame = 0;
            private bool IsBetweenFrame;
            private bool IsRendering = false;
            private bool FinishedRendering = false;
            public List<CameraListData> CameraList;
            [System.Serializable]
            public struct CamData {
                public int SamplesPerShot;
                public int StartFrame;
                public int EndFrame;
                public int TotalFrames;
            }
            public List<CamData> CamSettings;
            public TimelineShooterData(ref List<CameraListData> CamList) {
                CamSettings = new List<CamData>();
                CameraList = CamList;
            }
            public void SetInitialSettings(List<CameraListData> CamList) {
                Application.runInBackground = true;
                CameraList = CamList;
                foreach(var A in CameraList) {
                    A.OptionalDirector.timeUpdateMode = UnityEngine.Playables.DirectorUpdateMode.Manual;
                    A.OptionalDirector.playOnAwake = false;
                }
                CurrentFrame = CamSettings[0].StartFrame;
                TTInterface.SetTTSettings(CameraList[0].CamSettings);
            }


            public IEnumerator RecordFrame()
            {
                yield return new WaitForEndOfFrame();
                float _animTotalDuration = (float)CameraList[CurrentCamera].OptionalDirector.duration;
                float _animCurrentTime = ((float)CurrentFrame / (float)CamSettings[CurrentCamera].TotalFrames) * _animTotalDuration;
                CameraList[CurrentCamera].OptionalDirector.time = _animCurrentTime;
                CameraList[CurrentCamera].OptionalDirector.Evaluate();
                if((RayTracingMaster.SampleCount >= CamSettings[CurrentCamera].SamplesPerShot)) {
                    string Path = PlayerPrefs.GetString("TimelinePath") + "/" + RayTracingMaster._camera.name + "_" + CurrentFrame + ".png";
                    ScreenCapture.CaptureScreenshot(Path);
                    IncrementRenderCounter();
                    #if MultiMapScreenshot
                        if(CameraList[CurrentCamera].SaveImgMap) {
                           SaveTexture(RayTracingMaster.RayMaster.MultiMapMatIDTexture, Path.Replace(".png", "") + "_MatID.png");
                           SaveTexture(RayTracingMaster.RayMaster.MultiMapMeshIDTexture, Path.Replace(".png", "") + "_MeshID.png");
                       }
                    #endif
                    UnityEditor.AssetDatabase.Refresh();
                    RayTracingMaster.SampleCount = 0;
                    RayTracingMaster.RayMaster.FramesSinceStart = 0;
                    IsRendering = false;
                    CurrentFrame++;
                }
                if(!CameraList[CurrentCamera].ActivateCam || CurrentFrame > CamSettings[CurrentCamera].EndFrame) {
                    CurrentCamera++;
                    if(CurrentCamera < CamSettings.Count) {
                        CameraList[CurrentCamera - 1].TargCam.gameObject.SetActive(false);
                        CameraList[CurrentCamera].TargCam.gameObject.SetActive(true);
                        RayTracingMaster.RayMaster.TossCamera(CameraList[CurrentCamera].TargCam);
                        TTInterface.SetTTSettings(CameraList[CurrentCamera].CamSettings);
                        CurrentFrame = CamSettings[CurrentCamera].StartFrame;
                    } else {                            
                        Application.runInBackground = false;
                        EditorApplication.isPlaying = false;
                    }
                }
            }



        }
        [SerializeField] public TimelineShooterData TimelineSettings;

        [Serializable]
        public class SlicedImageData {
            public Vector2Int FinalAtlasSize;
            public int Padding;
            public float TimeBetweenSegments;
            public int MaxSamples;
            public int HorizontalSegments;
            public Texture2D[] TexArray; 
            #if MultiMapScreenshot
                public Texture2D[] MatTexArray; 
                public Texture2D[] MeshTexArray; 
            #endif
            public bool PrevPanorama = false;
            private float waitedTime = 0;
            private int CurrentCamera = 0;
            private int CurrentSegment = 0;
            public List<CameraListData> CameraList;
            public SlicedImageData(ref List<CameraListData> CamList) {
                CameraList = CamList;
                FinalAtlasSize = new Vector2Int(10000, 5000);
                Padding = 32;
                TimeBetweenSegments = 10f;
                MaxSamples = 10000;
                HorizontalSegments = 10;
            }

            public IEnumerator RecordFrame()
            {
                yield return new WaitForEndOfFrame();
                if(TexArray != null) {
                    if(PrevPanorama) {
                        PrevPanorama = false;
                        RayTracingMaster.SampleCount = 0;
                        RayTracingMaster.RayMaster.FramesSinceStart = 0;                    
                    }
                    float PaddingHalfValue = (Padding / 2.0f) / (float)FinalAtlasSize.x;
                    RayTracingMaster.RayMaster.CurrentHorizonalPatch = new Vector2((float)CurrentSegment / (float)HorizontalSegments - PaddingHalfValue, (float)(CurrentSegment + 1) / (float)HorizontalSegments + PaddingHalfValue);
                    waitedTime += Time.deltaTime;
                    if (!CameraList[CurrentCamera].ActivateCam || RayTracingMaster.RayMaster.FramesSinceStart >= MaxSamples || waitedTime >= TimeBetweenSegments) {
                        if(CameraList[CurrentCamera].ActivateCam) {
                            waitedTime = 0;
                            if(!System.IO.Directory.Exists(Application.dataPath.Replace("/Assets", "") + "/TrueTrace/TempPanoramas")) {
                                System.IO.Directory.CreateDirectory(Application.dataPath.Replace("/Assets", "") + "/TrueTrace/TempPanoramas");
                            }
                            ScreenCapture.CaptureScreenshot(Application.dataPath.Replace("/Assets", "") + "/TrueTrace/TempPanoramas/" + CurrentSegment + ".png");
                            TexArray[CurrentSegment] = ScreenCapture.CaptureScreenshotAsTexture();
                            #if MultiMapScreenshot
                                if(CameraList[CurrentCamera].SaveImgMap) {
                                    MatTexArray[CurrentSegment] = ConvertTexture(RayTracingMaster.RayMaster.MultiMapMatIDTexture);
                                    MeshTexArray[CurrentSegment] = ConvertTexture(RayTracingMaster.RayMaster.MultiMapMeshIDTexture);
                                }
                            #endif


                            CurrentSegment++;
                            RayTracingMaster.SampleCount = 0;
                            RayTracingMaster.RayMaster.FramesSinceStart = 0;
                            RayTracingMaster.RayMaster._currentSample = 0;
                            PrevPanorama = true;
                            if(CurrentSegment == HorizontalSegments) {
                                CurrentSegment = 0;
                                waitedTime = 0;
                            #if MultiMapScreenshot
                                StitchSlices(CameraList[CurrentCamera].TargCam, CameraList[CurrentCamera].SaveImgMap);
                            #else
                                StitchSlices(CameraList[CurrentCamera].TargCam);
                            #endif
                                CurrentCamera++;
                                if(CurrentCamera < CameraList.Count) {
                                    CameraList[CurrentCamera - 1].TargCam.gameObject.SetActive(false);
                                    CameraList[CurrentCamera].TargCam.gameObject.SetActive(true);
                                    RayTracingMaster.RayMaster.TossCamera(CameraList[CurrentCamera].TargCam);
                                    TTInterface.SetTTSettings(CameraList[CurrentCamera].CamSettings);
                                } else {                            
                                    // RemoveResolution(GetCount() - 1);
                                    RayTracingMaster.RayMaster.DoPanorama = false;
                                    RayTracingMaster.RayMaster.DoChainedImages = false;
                                    Application.runInBackground = false;
                                    EditorApplication.isPlaying = false;
                                }
                            }
                        } else {
                            CurrentCamera++;
                            if(CurrentCamera < CameraList.Count) {
                                CameraList[CurrentCamera - 1].TargCam.gameObject.SetActive(false);
                                CameraList[CurrentCamera].TargCam.gameObject.SetActive(true);
                                RayTracingMaster.RayMaster.TossCamera(CameraList[CurrentCamera].TargCam);
                                TTInterface.SetTTSettings(CameraList[CurrentCamera].CamSettings);
                            } else {                            
                                // RemoveResolution(GetCount() - 1);
                                RayTracingMaster.RayMaster.DoPanorama = false;
                                RayTracingMaster.RayMaster.DoChainedImages = false;
                                Application.runInBackground = false;
                                EditorApplication.isPlaying = false;
                            }
                        }

                    }
                }
            }
            public void SetInitialSettings(List<CameraListData> CamList) {
                CameraList = CamList;
                TTInterface.SetTTSettings(CameraList[0].CamSettings == null ? RayTracingMaster.RayMaster.LocalTTSettings : CameraList[0].CamSettings);
            }
        #if MultiMapScreenshot
            public void StitchSlices(Camera cam, bool DoMapping) {
        #else
            public void StitchSlices(Camera cam) {
        #endif 
                IncrementRenderCounter();
                Color[] FinalAtlasData = new Color[FinalAtlasSize.x * FinalAtlasSize.y];
                #if MultiMapScreenshot
                    Color[] FinalAtlasMatData = new Color[1];
                    Color[] FinalAtlasMeshData = new Color[1];
                    if(DoMapping) {
                        FinalAtlasMatData = new Color[FinalAtlasSize.x * FinalAtlasSize.y];
                        FinalAtlasMeshData = new Color[FinalAtlasSize.x * FinalAtlasSize.y];
                    }
                #endif
                for(int iter = 0; iter < HorizontalSegments; iter++) {
                    int width = TexArray[iter].width - Padding;
                    int height = TexArray[iter].height;

                    #if MultiMapScreenshot
                        Color[] CurrentMatData = new Color[1];
                        Color[] CurrentMeshData = new Color[1];
                        if(DoMapping) {
                            CurrentMatData = MatTexArray[iter].GetPixels(0);
                            CurrentMeshData = MeshTexArray[iter].GetPixels(0);
                        }
                    #endif


                    Color[] CurrentData = TexArray[iter].GetPixels(0);
                    int XOffset = iter * Mathf.CeilToInt((float)FinalAtlasSize.x / (float)HorizontalSegments);
                    // int YOffset = iter * Mathf.CeilToInt(5000.0f / 5000.0f);
                    for(int i = 0; i < width + Padding; i++) {
                        for(int j = 0; j < height; j++) {
                            int IndexChild = i + j * (width + Padding);
                            int IndexFinal = (i + XOffset - (Padding / 2)) + (j) * FinalAtlasSize.x;
                            if(i >= Padding / 2 && iter != HorizontalSegments - 1) {
                                FinalAtlasData[IndexFinal] = new Color(CurrentData[IndexChild].r, CurrentData[IndexChild].g, CurrentData[IndexChild].b, 1); 
                    #if MultiMapScreenshot
                                if(DoMapping) {
                                    FinalAtlasMatData[IndexFinal] = new Color(CurrentMatData[IndexChild].r, CurrentMatData[IndexChild].g, CurrentMatData[IndexChild].b, 1); 
                                    FinalAtlasMeshData[IndexFinal] = new Color(CurrentMeshData[IndexChild].r, CurrentMeshData[IndexChild].g, CurrentMeshData[IndexChild].b, 1); 
                                }
                    #endif
                            } else if(iter != 0 && iter != HorizontalSegments - 1) {
                                float Ratio = 1;
                                if(i < Padding / 2) {
                                    Ratio = 1.0f - ((float)i / (float)(Padding / 2));
                                }
                                FinalAtlasData[IndexFinal] = new Color((FinalAtlasData[IndexFinal].r * Ratio + CurrentData[IndexChild].r * (1.0f - Ratio)), (FinalAtlasData[IndexFinal].g * Ratio + CurrentData[IndexChild].g * (1.0f - Ratio)), (FinalAtlasData[IndexFinal].b * (Ratio) + CurrentData[IndexChild].b * (1.0f - Ratio)), 1); 
                    #if MultiMapScreenshot
                                if(DoMapping) {
                                    FinalAtlasMatData[IndexFinal] = new Color((FinalAtlasMatData[IndexFinal].r * Ratio + CurrentMatData[IndexChild].r * (1.0f - Ratio)), (FinalAtlasMatData[IndexFinal].g * Ratio + CurrentMatData[IndexChild].g * (1.0f - Ratio)), (FinalAtlasMatData[IndexFinal].b * (Ratio) + CurrentMatData[IndexChild].b * (1.0f - Ratio)), 1); 
                                    FinalAtlasMeshData[IndexFinal] = new Color((FinalAtlasMeshData[IndexFinal].r * Ratio + CurrentMeshData[IndexChild].r * (1.0f - Ratio)), (FinalAtlasMeshData[IndexFinal].g * Ratio + CurrentMeshData[IndexChild].g * (1.0f - Ratio)), (FinalAtlasMeshData[IndexFinal].b * (Ratio) + CurrentMeshData[IndexChild].b * (1.0f - Ratio)), 1); 
                                }
                    #endif
                            } else if(iter == HorizontalSegments - 1 && i < width + (Padding / 2)) {
                                FinalAtlasData[IndexFinal] = new Color(CurrentData[IndexChild].r, CurrentData[IndexChild].g, CurrentData[IndexChild].b, 1); 
                    #if MultiMapScreenshot
                                if(DoMapping) {
                                    FinalAtlasMatData[IndexFinal] = new Color(CurrentMatData[IndexChild].r, CurrentMatData[IndexChild].g, CurrentMatData[IndexChild].b, 1); 
                                    FinalAtlasMeshData[IndexFinal] = new Color(CurrentMeshData[IndexChild].r, CurrentMeshData[IndexChild].g, CurrentMeshData[IndexChild].b, 1); 
                                }
                    #endif
                            }
                        }
                    }
                    DestroyImmediate(TexArray[iter]);
                    #if MultiMapScreenshot
                        if(DoMapping) {
                            DestroyImmediate(MatTexArray[iter]);
                            DestroyImmediate(MeshTexArray[iter]);
                        }
                    #endif
                }
                Texture2D FinalAtlas = new Texture2D(FinalAtlasSize.x, FinalAtlasSize.y);
                FinalAtlas.SetPixels(FinalAtlasData, 0);
                FinalAtlas.Apply();

                #if MultiMapScreenshot
                    Texture2D FinalMatAtlas = new Texture2D(1,1);
                    Texture2D FinalMeshAtlas = new Texture2D(1,1);
                    if(DoMapping) {
                        FinalMatAtlas = new Texture2D(FinalAtlasSize.x, FinalAtlasSize.y);
                        FinalMatAtlas.SetPixels(FinalAtlasMatData, 0);
                        FinalMatAtlas.Apply();

                        FinalMeshAtlas = new Texture2D(FinalAtlasSize.x, FinalAtlasSize.y);
                        FinalMeshAtlas.SetPixels(FinalAtlasMeshData, 0);
                        FinalMeshAtlas.Apply();
                    }
                #endif
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

                   FilePath = PlayerPrefs.GetString("PanoramaPath") + "/" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Replace(" ", "") + "_" + RayTracingMaster._camera.name + "_" + SegmentNumber + ".png";
                } while(System.IO.File.Exists(FilePath));
               

                System.IO.File.WriteAllBytes(FilePath, FinalAtlas.EncodeToPNG());


                #if MultiMapScreenshot
                    if(DoMapping) {
                        System.IO.File.WriteAllBytes(FilePath.Replace(".png", "") + "_MatID.png", FinalMatAtlas.EncodeToPNG());
                        System.IO.File.WriteAllBytes(FilePath.Replace(".png", "") + "_MeshID.png", FinalMeshAtlas.EncodeToPNG());
                    }

                #endif

                // System.IO.File.WriteAllBytes(PlayerPrefs.GetString("PanoramaPath") + "/" + cam.gameObject.name + ".png", FinalAtlas.EncodeToPNG()); 
            }

        }
        [SerializeField] public SlicedImageData SlicedImageSettings;

       
        Matrix4x4 CalcProj(Camera cam) {
            float Aspect = SlicedImageSettings.FinalAtlasSize.x / (float)SlicedImageSettings.FinalAtlasSize.y;
            float YFOV = 1.0f / Mathf.Tan(cam.fieldOfView / (2.0f * (360.0f / (2.0f * 3.14159f))));
            float XFOV = YFOV / Aspect;
            Matrix4x4 TempProj = cam.projectionMatrix;
            TempProj[0,0] = XFOV;
            TempProj[1,1] = YFOV;
            return TempProj;
        }

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

    int GetResolution()
    {
        Type gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        PropertyInfo selectedSizeIndex = gameView.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        EditorWindow window = EditorWindow.GetWindow(gameView);
        return (int)selectedSizeIndex.GetValue(window);
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

        private void InitCameras() {
            RayTracingMaster.SampleCount = 0;
            RayTracingMaster.RayMaster.FramesSinceStart = 0;          
            RayTracingMaster.RayMaster._currentSample = 0;
            // if(Cameras == null || Cameras.Length == 0) {
            //     Cameras = new Camera[1];
            //     if(RayTracingMaster._camera != null)
            //         Cameras[0] = RayTracingMaster._camera;
            // }
            // if(/!(Cameras == null || Cameras.Length == 0)) {
                Camera[] AllCameras = GameObject.FindObjectsOfType<Camera>();
                for(int i = 0; i < AllCameras.Length; i++) {
                    if(SelectedFunctionality == ImageGenType.LargeScreenShot) {
                        AllCameras[i].projectionMatrix = CalcProj(AllCameras[i]);
                    }
                    AllCameras[i].gameObject.SetActive(false);

                }
                CameraList[0].TargCam.gameObject.SetActive(true);
            // }
        }

        public void OnDisable() {
            switch(SelectedFunctionality) {
                default:
                break;
                case(ImageGenType.LargeScreenShot):
                case(ImageGenType.Panorama):
                    SetResolution(CurrentResIndex);
                    RemoveResolution(GetCount() - 1);
                break;
            }            
            TTInterface.SetTTSettings(InitialSettings);
            RayTracingMaster.ImageIsModified = false;
        }

        public void Init() {
            switch(SelectedFunctionality) {
                default:
                break;
                case(ImageGenType.LargeScreenShot):
                case(ImageGenType.Panorama):
                    SlicedImageSettings = new SlicedImageData(ref CameraList);
                break;
                case(ImageGenType.TurnTable):
                    TurnTableSettings = new TurnTableData(ref CameraList);
                break;
                case(ImageGenType.TimelineShooter):
                    TimelineSettings = new TimelineShooterData(ref CameraList);
                break;
            }            
        }
        bool HasStarted = false;
        public void Start() {
            InitialSettings = RayTracingMaster.RayMaster.LocalTTSettings;
            InitCameras();
            switch(SelectedFunctionality) {
                default:
                break;
                case(ImageGenType.Panorama):
                    Application.runInBackground = true;
                    RayTracingMaster.RayMaster.DoPanorama = true;
                    RayTracingMaster.RayMaster.DoChainedImages = true;
                    CurrentResIndex = GetResolution();
                    AddResolution(Mathf.CeilToInt((float)SlicedImageSettings.FinalAtlasSize.x / (float)SlicedImageSettings.HorizontalSegments) + SlicedImageSettings.Padding, SlicedImageSettings.FinalAtlasSize.y, "TempPanoramaSize");
                    SetResolution(GetCount() - 1);
                    #if MultiMapScreenshot
                        SlicedImageSettings.MatTexArray = new Texture2D[SlicedImageSettings.HorizontalSegments];
                        SlicedImageSettings.MeshTexArray = new Texture2D[SlicedImageSettings.HorizontalSegments];
                    #endif
                    SlicedImageSettings.TexArray = new Texture2D[SlicedImageSettings.HorizontalSegments];
                    SlicedImageSettings.PrevPanorama = true;
                    SlicedImageSettings.CameraList = CameraList;
                    RayTracingMaster.ImageIsModified = true;
                break;
                case(ImageGenType.LargeScreenShot):
                    Application.runInBackground = true;
                    RayTracingMaster.RayMaster.DoPanorama = false;
                    RayTracingMaster.RayMaster.DoChainedImages = true;
                    CurrentResIndex = GetResolution();
                    AddResolution(Mathf.CeilToInt((float)SlicedImageSettings.FinalAtlasSize.x / (float)SlicedImageSettings.HorizontalSegments) + SlicedImageSettings.Padding, SlicedImageSettings.FinalAtlasSize.y, "TempPanoramaSize");
                    SetResolution(GetCount() - 1);
                    SlicedImageSettings.TexArray = new Texture2D[SlicedImageSettings.HorizontalSegments];
                    #if MultiMapScreenshot
                        SlicedImageSettings.MatTexArray = new Texture2D[SlicedImageSettings.HorizontalSegments];
                        SlicedImageSettings.MeshTexArray = new Texture2D[SlicedImageSettings.HorizontalSegments];
                    #endif
                    SlicedImageSettings.PrevPanorama = true;
                    SlicedImageSettings.CameraList = CameraList;
                    RayTracingMaster.ImageIsModified = true;
                break;
                case(ImageGenType.TurnTable):
                    TurnTableSettings.CameraList = CameraList;
                    RayTracingMaster.ImageIsModified = true;
                break;
                case(ImageGenType.TimelineShooter):
                    TimelineSettings.CameraList = CameraList;
                    RayTracingMaster.ImageIsModified = true;
                break;
            }
        }

        public void LateUpdate() {
            switch(SelectedFunctionality) {
                default:
                break;
                case(ImageGenType.LargeScreenShot):
                case(ImageGenType.Panorama):
                    RayTracingMaster.RayMaster.DoPanorama = SelectedFunctionality == ImageGenType.Panorama;
                    RayTracingMaster.RayMaster.DoChainedImages = true;
                    if(!HasStarted) SlicedImageSettings.SetInitialSettings(CameraList);
                    StartCoroutine(SlicedImageSettings.RecordFrame());
                break;
                case(ImageGenType.TurnTable):
                    if(!HasStarted) TurnTableSettings.SetInitialSettings(CameraList);
                    StartCoroutine(TurnTableSettings.RecordFrame());
                break;
                case(ImageGenType.TimedScreenShot):
                    if(((RayTracingMaster.SampleCount % SamplesBetweenShots) == SamplesBetweenShots - 1 && !ResetSampCountAfterShot) || (RayTracingMaster.SampleCount >= SamplesBetweenShots && ResetSampCountAfterShot)) {
                        IncrementRenderCounter();
                        ScreenCapture.CaptureScreenshot(PlayerPrefs.GetString("ScreenShotPath") + "/" + System.DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ", " + RayTracingMaster.SampleCount + " Samples.png");
                        UnityEditor.AssetDatabase.Refresh();
                        if(ResetSampCountAfterShot) {
                            RayTracingMaster.SampleCount = 0;
                            RayTracingMaster.RayMaster.FramesSinceStart = 0;
                        }
                    }
                break;
                case(ImageGenType.TimelineShooter):
                    if(!HasStarted) TimelineSettings.SetInitialSettings(CameraList);
                    StartCoroutine(TimelineSettings.RecordFrame());
                break;

            }
            HasStarted = true;
        }


    }


    [CustomEditor(typeof(TTAdvancedImageGen))]
    public class TTAdvancedImageGenEditor : Editor
    {
        // Custom in-scene UI for when ExampleScript
        // component is selected.
        float ttt = 0;
        TTAdvancedImageGen t;
        int SelectedCam = -1;
        bool TurnTableShowAll = false;

        private void ConstructCameraList() {
            if(t.CameraList == null) t.CameraList = new List<TTAdvancedImageGen.CameraListData>();
        }
        Vector2 scrollPosition;
        private void DisplayCameraList() {
            GUIStyle TempStyle = new GUIStyle();
            TempStyle.fixedWidth = 120;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(450), GUILayout.Height(300));
                for(int i = 0; i < t.CameraList.Count; i++) {
                    var A = i;
                    GUILayout.BeginHorizontal();
                    if(t.SelectedFunctionality == TTAdvancedImageGen.ImageGenType.TurnTable || t.SelectedFunctionality == TTAdvancedImageGen.ImageGenType.TimelineShooter) if(GUILayout.Button("Select", GUILayout.Width(50))) {SelectedCam = A;}
                    TTAdvancedImageGen.CameraListData TempDat = t.CameraList[i];
                        TempDat.ActivateCam = EditorGUILayout.ToggleLeft("", TempDat.ActivateCam, GUILayout.MaxWidth(15));
                        TempDat.TargCam = EditorGUILayout.ObjectField(TempDat.TargCam, typeof(Camera), true, GUILayout.Width(150)) as Camera;
                        TempDat.CamSettings = EditorGUILayout.ObjectField(TempDat.CamSettings, typeof(TTSettings), true, GUILayout.Width(150)) as TTSettings;
                        if(t.SelectedFunctionality == TTAdvancedImageGen.ImageGenType.TimelineShooter) TempDat.OptionalDirector = EditorGUILayout.ObjectField(TempDat.OptionalDirector, typeof(UnityEngine.Playables.PlayableDirector), true, GUILayout.Width(150)) as UnityEngine.Playables.PlayableDirector;

                    #if MultiMapScreenshot
                        TempDat.SaveImgMap = EditorGUILayout.ToggleLeft("GenMaps", TempDat.SaveImgMap, GUILayout.MaxWidth(75));
                    #endif
                    t.CameraList[i] = TempDat;

                    if(GUILayout.Button("Delete", GUILayout.Width(50))) {
                        if(t.SelectedFunctionality == TTAdvancedImageGen.ImageGenType.TurnTable) {
                            t.TurnTableSettings.CamSettings.RemoveAt(A);
                        }
                        if(t.SelectedFunctionality == TTAdvancedImageGen.ImageGenType.TimelineShooter) {
                            t.TimelineSettings.CamSettings.RemoveAt(A);
                        }
                        t.CameraList.RemoveAt(A); 
                        i--;
                    }
                    GUILayout.EndHorizontal();
                }
            GUILayout.EndScrollView();
        }
        private void AddNewCam() {
            Camera TempCam = null;
            TTSettings TempSet = null;
            if(t.CameraList.Count != 0) {
                if(t.CameraList[t.CameraList.Count - 1].TargCam != null) TempCam = t.CameraList[t.CameraList.Count - 1].TargCam;
                if(t.CameraList[t.CameraList.Count - 1].CamSettings != null) TempSet = t.CameraList[t.CameraList.Count - 1].CamSettings;
            }
            TTAdvancedImageGen.CameraListData TempElement = new TTAdvancedImageGen.CameraListData();
            TempElement.TargCam = TempCam;
            TempElement.CamSettings = TempSet;
            TempElement.ActivateCam = true;
            // if(TempCam.TryGetComponent<UnityEngine.Playables.PlayableDirector>(out UnityEngine.Playables.PlayableDirector TempDir)) TempElement.OptionalDirector = TempDir;
            t.CameraList.Add(TempElement);
            if(t.SelectedFunctionality == TTAdvancedImageGen.ImageGenType.TimelineShooter) {
                t.TimelineSettings.CamSettings.Add(new TTAdvancedImageGen.TimelineShooterData.CamData() {
                    SamplesPerShot = 300,
                    StartFrame = 0,
                    EndFrame = 500,
                    TotalFrames = 500
                });
            }
            if(t.SelectedFunctionality == TTAdvancedImageGen.ImageGenType.TurnTable) {
                t.TurnTableSettings.CamSettings.Add(new TTAdvancedImageGen.TurnTableData.CamData() {
                    HorizontalResolution = 10,
                    Center = Vector3.one,
                    Distance = 1.0f,
                    Pitch = 0.0f,
                    MaxSamples = 64,
                    TimeBetweenSegments = 10.0f
                });
            }
        }
        private void DisplaySlicedSettings() {
            GUILayout.BeginVertical(GUILayout.Width(345));
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Final Atlas Size: ", GUILayout.Width(145));
                    GUILayout.Label("W:", GUILayout.Width(25));
                    if(t.SelectedFunctionality == TTAdvancedImageGen.ImageGenType.Panorama) {
                        EditorGUI.BeginChangeCheck();
                            t.SlicedImageSettings.FinalAtlasSize.x = EditorGUILayout.IntField(t.SlicedImageSettings.FinalAtlasSize.x, GUILayout.Width(100));
                        if(EditorGUI.EndChangeCheck()) {
                            t.SlicedImageSettings.FinalAtlasSize.y = t.SlicedImageSettings.FinalAtlasSize.x / 2;
                        }
                        GUILayout.Label("H:", GUILayout.Width(20));
                        EditorGUILayout.LabelField((t.SlicedImageSettings.FinalAtlasSize.x / 2).ToString(), GUILayout.Width(100));
                    } else {
                        t.SlicedImageSettings.FinalAtlasSize.x = EditorGUILayout.IntField(t.SlicedImageSettings.FinalAtlasSize.x, GUILayout.Width(100));
                        GUILayout.Label("H:", GUILayout.Width(20));
                        t.SlicedImageSettings.FinalAtlasSize.y = EditorGUILayout.IntField(t.SlicedImageSettings.FinalAtlasSize.y, GUILayout.Width(100));
                    }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Maximum Time Per Slice: ", GUILayout.Width(175));
                    t.SlicedImageSettings.TimeBetweenSegments = EditorGUILayout.FloatField(t.SlicedImageSettings.TimeBetweenSegments, GUILayout.Width(100));
                    GUILayout.Label("s", GUILayout.Width(20));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Maximum Samples Per Slice: ", GUILayout.Width(175));
                    t.SlicedImageSettings.MaxSamples = EditorGUILayout.IntField(t.SlicedImageSettings.MaxSamples, GUILayout.Width(100));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                    GUILayout.Label("Slice Padding: ", GUILayout.Width(175));
                    t.SlicedImageSettings.Padding = EditorGUILayout.IntField(t.SlicedImageSettings.Padding, GUILayout.Width(100));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Slice Segments: ", GUILayout.Width(175));
                    t.SlicedImageSettings.HorizontalSegments = EditorGUILayout.IntField(t.SlicedImageSettings.HorizontalSegments, GUILayout.Width(100));
                GUILayout.EndHorizontal();

                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                    GUILayout.Label("Final Slice Size: ", GUILayout.Width(145));
                    GUILayout.Label("W:", GUILayout.Width(25));
                    EditorGUILayout.LabelField((Mathf.CeilToInt((float)t.SlicedImageSettings.FinalAtlasSize.x / (float)t.SlicedImageSettings.HorizontalSegments) + t.SlicedImageSettings.Padding).ToString(), GUILayout.Width(100));
                    GUILayout.Label("H:", GUILayout.Width(20));
                    EditorGUILayout.LabelField((t.SlicedImageSettings.FinalAtlasSize.y).ToString(), GUILayout.Width(100));
                GUILayout.EndHorizontal();


            GUILayout.EndVertical();
        }


        private void DisplayTimelineSettings() {
            GUILayout.BeginVertical(GUILayout.Width(345));
                if(SelectedCam != -1 && SelectedCam < t.TimelineSettings.CamSettings.Count) {
                    TTAdvancedImageGen.TimelineShooterData.CamData TempDat = t.TimelineSettings.CamSettings[SelectedCam];
                    GUILayout.BeginHorizontal();
                        GUILayout.Label("Camera " + SelectedCam, GUILayout.Width(175));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                        GUILayout.Label("Samples Per Shot: ", GUILayout.Width(175));
                        TempDat.SamplesPerShot = EditorGUILayout.IntField(TempDat.SamplesPerShot, GUILayout.Width(100));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                        GUILayout.Label("Start Frame: ", GUILayout.Width(175));
                        TempDat.StartFrame = EditorGUILayout.IntField(TempDat.StartFrame, GUILayout.Width(100));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                        GUILayout.Label("End Frame: ", GUILayout.Width(175));
                        TempDat.EndFrame = EditorGUILayout.IntField(TempDat.EndFrame, GUILayout.Width(100));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                        GUILayout.Label("Total Frames: ", GUILayout.Width(175));
                        TempDat.TotalFrames = EditorGUILayout.IntField(TempDat.TotalFrames, GUILayout.Width(100));
                    GUILayout.EndHorizontal();
                    t.TimelineSettings.CamSettings[SelectedCam] = TempDat;
                } else {
                    EditorGUILayout.Space();
                }
            GUILayout.EndVertical();
        }


        private void DisplayTurnTableSettings() {
            GUILayout.BeginVertical(GUILayout.Width(345));
                if(SelectedCam != -1 && SelectedCam < t.TurnTableSettings.CamSettings.Count) {
                    TTAdvancedImageGen.TurnTableData.CamData TempDat = t.TurnTableSettings.CamSettings[SelectedCam];
                    GUILayout.BeginHorizontal();
                        GUILayout.Label("Camera " + SelectedCam, GUILayout.Width(175));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                        GUILayout.Label("Horizontal Resolution: ", GUILayout.Width(175));
                        TempDat.HorizontalResolution = EditorGUILayout.IntField(TempDat.HorizontalResolution, GUILayout.Width(100));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                        GUILayout.Label("Center: ", GUILayout.Width(175));
                        TempDat.Center = EditorGUILayout.Vector3Field("",TempDat.Center, GUILayout.Width(100));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                        GUILayout.Label("Distance: ", GUILayout.Width(175));
                        TempDat.Distance = EditorGUILayout.FloatField(TempDat.Distance, GUILayout.Width(100));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                        GUILayout.Label("Pitch: ", GUILayout.Width(175));
                        TempDat.Pitch = EditorGUILayout.FloatField(TempDat.Pitch, GUILayout.Width(100));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                        GUILayout.Label("Maximum Time Per Turn: ", GUILayout.Width(175));
                        TempDat.TimeBetweenSegments = EditorGUILayout.FloatField(TempDat.TimeBetweenSegments, GUILayout.Width(100));
                        GUILayout.Label("s", GUILayout.Width(20));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                        GUILayout.Label("Maximum Samples Per Turn: ", GUILayout.Width(175));
                        TempDat.MaxSamples = EditorGUILayout.IntField(TempDat.MaxSamples, GUILayout.Width(100));
                    GUILayout.EndHorizontal();
                    t.TurnTableSettings.CamSettings[SelectedCam] = TempDat;
                } else {
                    EditorGUILayout.Space();
                }
            GUILayout.EndVertical();
        }

        public override void OnInspectorGUI() {
            var t1 = (targets);
            t =  t1[0] as TTAdvancedImageGen;
            if(t.SelectedFunctionality == TTAdvancedImageGen.ImageGenType.NULL) {
                EditorGUI.BeginChangeCheck();
                    t.SelectedFunctionality = (TTAdvancedImageGen.ImageGenType)EditorGUILayout.EnumPopup("Functionality: ", t.SelectedFunctionality);
                if(EditorGUI.EndChangeCheck()) {
                    switch(t.SelectedFunctionality) {
                        default:
                        break;
                        case(TTAdvancedImageGen.ImageGenType.Panorama):
                        case(TTAdvancedImageGen.ImageGenType.LargeScreenShot):
                        case(TTAdvancedImageGen.ImageGenType.TurnTable):
                            ConstructCameraList();
                        break;
                        case(TTAdvancedImageGen.ImageGenType.TimedScreenShot):
                            ConstructCameraList();
                            AddNewCam();
                        break;
                        case(TTAdvancedImageGen.ImageGenType.TimelineShooter):
                            ConstructCameraList();
                        break;
                    }                    
                    t.Init();
                }

            } else {
                GUILayout.Label("Selected Function: " + t.SelectedFunctionality.ToString());
                switch(t.SelectedFunctionality) {
                    default:
                    break;
                    case(TTAdvancedImageGen.ImageGenType.Panorama):
                    case(TTAdvancedImageGen.ImageGenType.LargeScreenShot):
                        if(GUILayout.Button("Add Camera")) AddNewCam();
                        GUILayout.BeginHorizontal();
                            DisplaySlicedSettings();
                            DisplayCameraList();
                        GUILayout.EndHorizontal();
                    break;
                    case(TTAdvancedImageGen.ImageGenType.TurnTable):
                        if(GUILayout.Button("Add Camera")) AddNewCam();
                        TurnTableShowAll = EditorGUILayout.ToggleLeft("Show All Sets", TurnTableShowAll, GUILayout.MaxWidth(135));
                        t.TurnTableSettings.Running = EditorGUILayout.ToggleLeft("Running", t.TurnTableSettings.Running, GUILayout.MaxWidth(135));
                        GUILayout.BeginHorizontal();
                            DisplayTurnTableSettings();
                            DisplayCameraList();
                        GUILayout.EndHorizontal();
                    break;
                    case(TTAdvancedImageGen.ImageGenType.TimedScreenShot):
                        GUILayout.BeginHorizontal();
                            GUILayout.BeginVertical(GUILayout.Width(345));
                                GUILayout.BeginHorizontal();
                                    GUILayout.Label("Samples Between Shots: ", GUILayout.Width(175));
                                    t.SamplesBetweenShots = EditorGUILayout.IntField(t.SamplesBetweenShots, GUILayout.Width(100));
                                GUILayout.EndHorizontal();
                                GUILayout.BeginHorizontal();
                                    t.ResetSampCountAfterShot = EditorGUILayout.ToggleLeft("Reset Accumulation After Shot", t.ResetSampCountAfterShot, GUILayout.MaxWidth(135));
                                GUILayout.EndHorizontal();
                            GUILayout.EndVertical();
                        DisplayCameraList();
                        GUILayout.EndHorizontal();
                    break;
                    case(TTAdvancedImageGen.ImageGenType.TimelineShooter):
                        if(GUILayout.Button("Add Camera")) AddNewCam();
                        GUILayout.BeginHorizontal();
                            DisplayTimelineSettings();
                            DisplayCameraList();
                        GUILayout.EndHorizontal();
                    break;
                }
            }
        }

        Vector3 GetTangent(float RadsAlong) {
            return (new Vector3(Mathf.Sin(Mathf.Deg2Rad * RadsAlong), 0, Mathf.Cos(Mathf.Deg2Rad * RadsAlong))).normalized;
        }

        Vector3 GetToVector(float Yaw, float Pitch) {
            return new Vector3(Mathf.Cos(Mathf.Deg2Rad * Yaw) * Mathf.Cos(Mathf.Deg2Rad * Pitch), Mathf.Sin(Mathf.Deg2Rad * Pitch), Mathf.Sin(Mathf.Deg2Rad * Yaw) * Mathf.Cos(Mathf.Deg2Rad * Pitch));
        }
        public void OnSceneGUI()
        {
            var t = target as TTAdvancedImageGen;
            if(t.SelectedFunctionality == TTAdvancedImageGen.ImageGenType.TurnTable) {
                var tr = t.transform;
                var pos = tr.position;
                float ArcLength = 5.0f;
                if(!t.TurnTableSettings.Running) {
                    ttt += Time.deltaTime;
                    EditorGUI.BeginChangeCheck();
                    if(t.TurnTableSettings.CamSettings != null) {
                        int StartIndex = 0;
                        int EndIndex = t.TurnTableSettings.CamSettings.Count;
                        if(!TurnTableShowAll) {
                            if(SelectedCam != -1 && SelectedCam < t.TurnTableSettings.CamSettings.Count) {
                                StartIndex = SelectedCam;
                                EndIndex = SelectedCam + 1;
                            }
                        }
                        for(int i = StartIndex; i < EndIndex; i++) {
                            if(t.CameraList[i].TargCam != null) {
                                TTAdvancedImageGen.TurnTableData.CamData TempVar = t.TurnTableSettings.CamSettings[i];
                                TempVar.Center = Handles.PositionHandle(TempVar.Center, Quaternion.identity);
                                float yaw = 0;

                                Vector3 Pos2 = TempVar.Center + GetToVector(yaw, TempVar.Pitch) * TempVar.Distance;
                                t.CameraList[i].TargCam.transform.position = Pos2;


                                int HorizSegments = TempVar.HorizontalResolution;
                                Vector3 Pos3 = new Vector3(TempVar.Center.x, Pos2.y, TempVar.Center.z);
                                    // Handles.DrawLine(Pos2, Pos2 + GetTangent(yaw), 0.01f);
                                for(int i2 = 0; i2 < HorizSegments; i2++) {
                                    Handles.DrawWireArc(Pos3, Vector3.up, GetTangent(90 + 360.0f / (float)HorizSegments * (float)i2 - (ArcLength / 2.0f)), ArcLength, Vector3.Distance(Pos3, Pos2));
                                    Vector3 ToVector = GetToVector(90 + 360.0f / (float)HorizSegments * (float)i2, TempVar.Pitch);
                                    t.CameraList[i].TargCam.transform.forward = ToVector;
                                    Vector3 Pos4 = TempVar.Center + ToVector * TempVar.Distance;
                                    Handles.ConeHandleCap(0, Pos4 - ToVector * 0.15f, t.CameraList[i].TargCam.transform.rotation, 0.2f,  EventType.Repaint);
                                    Handles.DrawDottedLine(TempVar.Center, Pos4, 0.01f);
                                    // Handles.ConeHandleCap(0, TempVar.Center - t.CameraList[i].TargCam.transform.forward * 0.1f, t.CameraList[i].TargCam.transform.rotation, 0.2f,  EventType.Repaint);
                                }

                                t.CameraList[i].TargCam.transform.forward = (TempVar.Center - Pos2).normalized;
                                t.TurnTableSettings.CamSettings[i] = TempVar;
                            }
                        }
                    }
                    if (EditorGUI.EndChangeCheck()) {}
                }
            }
        }
    }
}
#endif