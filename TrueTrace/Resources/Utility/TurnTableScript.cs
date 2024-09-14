using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
 using UnityEngine.Profiling;
using System.Reflection;

namespace TrueTrace {
    public class TurnTableScript : MonoBehaviour
    {
        RayTracingMaster RayMaster;
        private int CurrentSegment = 0;
        private int CurrentCamera = 0;
        private float waitedTime = 0;
        private bool PrevImage = false;
        public bool Running = false;
        public float TimeBetweenSegments = 10.0f;
        public int MaxSamples = 64;
        [System.Serializable]
        public struct CamData {
            public Camera Cam;
            public int HorizontalResolution;
            public int VerticalResolution;
            public Vector3 Center;
            public float Distance;
            [Range(-89.9f, 89.9f)]public float Pitch;
        }
        public CamData[] CamSettings;

        void Start() {
            RayMaster = GameObject.Find("Scene").GetComponent<RayTracingMaster>();
            Application.runInBackground = true;
            Running = false;
            Init();
        }

        public void Init() {
            RayMaster.SampleCount = 0;
            RayMaster.FramesSinceStart = 0;          
            RayMaster._currentSample = 0;
            PrevImage = true;
            CurrentCamera = 0;
            if(!(CamSettings == null || CamSettings.Length == 0)) {
                if(CamSettings[0].Cam != null) CamSettings[0].Cam.gameObject.SetActive(true);
                for(int i = 1; i < CamSettings.Length; i++) if(CamSettings[i].Cam != null) CamSettings[i].Cam.gameObject.SetActive(false);
            }

        }

        IEnumerator RecordFrame()
        {
            yield return new WaitForEndOfFrame();
                if(PrevImage) {
                    PrevImage = false;
                    RayMaster.SampleCount = 0;
                    RayMaster.FramesSinceStart = 0;                    
                }
                waitedTime += Time.deltaTime;
                if(!Running) {
                    Running = true;
                }
                if (RayMaster.FramesSinceStart >= MaxSamples || waitedTime >= TimeBetweenSegments) {
                    waitedTime = 0;
                    if(!System.IO.Directory.Exists(Application.dataPath.Replace("/Assets", "") + "/TurnTables")) {
                        System.IO.Directory.CreateDirectory(Application.dataPath.Replace("/Assets", "") + "/TurnTables");
                    }
                    if(!System.IO.Directory.Exists(Application.dataPath.Replace("/Assets", "") + "/TurnTables/" + CamSettings[CurrentCamera].Cam.gameObject.name.Replace(" ", ""))) {
                        System.IO.Directory.CreateDirectory(Application.dataPath.Replace("/Assets", "") + "/TurnTables/" + CamSettings[CurrentCamera].Cam.gameObject.name.Replace(" ", ""));
                    }
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
                    ScreenCapture.CaptureScreenshot(Application.dataPath.Replace("/Assets", "") + "/TurnTables/" + CamSettings[CurrentCamera].Cam.gameObject.name.Replace(" ", "") + "/" + CamSettings[CurrentCamera].Cam.gameObject.name + "." + SegmentNumber + ".png");
                    CurrentSegment++;
                    CamSettings[CurrentCamera].Cam.gameObject.transform.RotateAround(CamSettings[CurrentCamera].Center, Vector3.up, (360.0f / (float)CamSettings[CurrentCamera].HorizontalResolution));
                    RayMaster.SampleCount = 0;
                    RayMaster.FramesSinceStart = 0;
                    RayMaster._currentSample = 0;
                    PrevImage = true;
                    if(CurrentSegment == CamSettings[CurrentCamera].HorizontalResolution) {
                        CurrentSegment = 0;
                        waitedTime = 0;
                        CurrentCamera++;
                        if(CurrentCamera < CamSettings.Length) {
                            CamSettings[CurrentCamera - 1].Cam.gameObject.SetActive(false);
                            CamSettings[CurrentCamera].Cam.gameObject.SetActive(true);
                            RayMaster.TossCamera(CamSettings[CurrentCamera].Cam);
                        } else {                            
                            Application.runInBackground = false;
#if UNITY_EDITOR
                            EditorApplication.isPlaying = false;
#endif
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