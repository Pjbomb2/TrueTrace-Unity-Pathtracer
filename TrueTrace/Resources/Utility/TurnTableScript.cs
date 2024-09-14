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
        float ttt = 0;

        Vector3 GetTangent(float RadsAlong) {
            return (new Vector3(Mathf.Sin(Mathf.Deg2Rad * RadsAlong), 0, Mathf.Cos(Mathf.Deg2Rad * RadsAlong))).normalized;
        }

        Vector3 GetToVector(float Yaw, float Pitch) {
            return new Vector3(Mathf.Cos(Mathf.Deg2Rad * Yaw) * Mathf.Cos(Mathf.Deg2Rad * Pitch), Mathf.Sin(Mathf.Deg2Rad * Pitch), Mathf.Sin(Mathf.Deg2Rad * Yaw) * Mathf.Cos(Mathf.Deg2Rad * Pitch));
        }
        public void OnSceneGUI()
        {
            var t = target as TurnTableScript;
            var tr = t.transform;
            var pos = tr.position;
            float ArcLength = 5.0f;
            if(!t.Running) {
                ttt += Time.deltaTime;
                EditorGUI.BeginChangeCheck();
                if(t.CamSettings != null) {
                    for(int i = 0; i < t.CamSettings.Length; i++) {
                        if(t.CamSettings[i].Cam != null) {
                            t.CamSettings[i].Center = Handles.PositionHandle(t.CamSettings[i].Center, Quaternion.identity);
                            float yaw = 0;

                            Vector3 Pos2 = t.CamSettings[i].Center + GetToVector(yaw, t.CamSettings[i].Pitch) * t.CamSettings[i].Distance;
                            t.CamSettings[i].Cam.transform.position = Pos2;


                            int HorizSegments = t.CamSettings[i].HorizontalResolution;
                            Vector3 Pos3 = new Vector3(t.CamSettings[i].Center.x, Pos2.y, t.CamSettings[i].Center.z);
                                // Handles.DrawLine(Pos2, Pos2 + GetTangent(yaw), 0.01f);
                            for(int i2 = 0; i2 < HorizSegments; i2++) {
                                Handles.DrawWireArc(Pos3, Vector3.up, GetTangent(90 + 360.0f / (float)HorizSegments * (float)i2 - (ArcLength / 2.0f)), ArcLength, Vector3.Distance(Pos3, Pos2));
                                Vector3 ToVector = GetToVector(90 + 360.0f / (float)HorizSegments * (float)i2, t.CamSettings[i].Pitch);
                                t.CamSettings[i].Cam.transform.forward = ToVector;
                                Vector3 Pos4 = t.CamSettings[i].Center + ToVector * t.CamSettings[i].Distance;
                                Handles.ConeHandleCap(0, Pos4 - ToVector * 0.15f, t.CamSettings[i].Cam.transform.rotation, 0.2f,  EventType.Repaint);
                                Handles.DrawDottedLine(t.CamSettings[i].Center, Pos4, 0.01f);
                                // Handles.ConeHandleCap(0, t.CamSettings[i].Center - t.CamSettings[i].Cam.transform.forward * 0.1f, t.CamSettings[i].Cam.transform.rotation, 0.2f,  EventType.Repaint);
                            }

                            t.CamSettings[i].Cam.transform.forward = (t.CamSettings[i].Center - Pos2).normalized;
                        }
                    }
                }
                if (EditorGUI.EndChangeCheck()) {}
            }
        }
    }
}
#endif