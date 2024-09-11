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
        [System.Serializable]
        public struct CamData {
            public Camera Cam;
            public int HorizontalResolution;
            public int VerticalResolution;
            public float TimeBetweenSegments;
            public int MaxSamples;
            public Vector3 Center;
            public float Distance;
            public float Pitch;
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
                if (RayMaster.FramesSinceStart >= CamSettings[CurrentCamera].MaxSamples || waitedTime >= CamSettings[CurrentCamera].TimeBetweenSegments) {
                    waitedTime = 0;
                    if(!System.IO.Directory.Exists(Application.dataPath.Replace("/Assets", "") + "/TurnTables")) {
                        System.IO.Directory.CreateDirectory(Application.dataPath.Replace("/Assets", "") + "/TurnTables");
                    }
                    ScreenCapture.CaptureScreenshot(Application.dataPath.Replace("/Assets", "") + "/TurnTables/" + CamSettings[CurrentCamera].Cam.gameObject.name + "_" + CurrentSegment + ".png");
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
                            EditorApplication.isPlaying = false;
                        }

                    }

                }
        }
        public void LateUpdate()
        {
            StartCoroutine(RecordFrame());
        }

    }

    [CustomEditor(typeof(TurnTableScript))]
    public class TurnTableScriptEditor : Editor
    {
        // Custom in-scene UI for when ExampleScript
        // component is selected.
        public void OnSceneGUI()
        {
            var t = target as TurnTableScript;
            var tr = t.transform;
            var pos = tr.position;
            if(!t.Running) {
                EditorGUI.BeginChangeCheck();
                if(t.CamSettings != null) {
                    for(int i = 0; i < t.CamSettings.Length; i++) {
                        if(t.CamSettings[i].Cam != null) {
                            t.CamSettings[i].Center = Handles.PositionHandle(t.CamSettings[i].Center, Quaternion.identity);
                            Vector3 Pos2 = t.CamSettings[i].Center + new Vector3(0, Mathf.Sin(Mathf.Deg2Rad * t.CamSettings[i].Pitch), Mathf.Cos(Mathf.Deg2Rad * t.CamSettings[i].Pitch)) * t.CamSettings[i].Distance;
                            t.CamSettings[i].Cam.transform.position = Pos2;
                            t.CamSettings[i].Cam.transform.forward = (t.CamSettings[i].Center - Pos2).normalized;
                            Handles.ConeHandleCap(0, t.CamSettings[i].Center - t.CamSettings[i].Cam.transform.forward * 0.1f, t.CamSettings[i].Cam.transform.rotation, 0.2f,  EventType.Repaint);

                            Handles.DrawDottedLine(Pos2, t.CamSettings[i].Center, 0.01f);
                        }
                    }
                }
                if (EditorGUI.EndChangeCheck()) {}
            }
        }
    }
}