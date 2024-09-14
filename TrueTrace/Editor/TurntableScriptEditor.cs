using UnityEditor;
using UnityEngine;

namespace TrueTrace
{
    [CustomEditor(typeof(TurnTableScript))]
    public class TurnTableScriptEditor : Editor
    {
        // Custom in-scene UI for when ExampleScript
        // component is selected.
        private float ttt;

        private Vector3 GetTangent(float RadsAlong)
        {
            return new Vector3(Mathf.Sin(Mathf.Deg2Rad * RadsAlong), 0, Mathf.Cos(Mathf.Deg2Rad * RadsAlong))
                .normalized;
        }

        private Vector3 GetToVector(float Yaw, float Pitch)
        {
            return new Vector3(Mathf.Cos(Mathf.Deg2Rad * Yaw) * Mathf.Cos(Mathf.Deg2Rad * Pitch),
                Mathf.Sin(Mathf.Deg2Rad * Pitch), Mathf.Sin(Mathf.Deg2Rad * Yaw) * Mathf.Cos(Mathf.Deg2Rad * Pitch));
        }

        public void OnSceneGUI()
        {
            var t = target as TurnTableScript;
            var tr = t.transform;
            var pos = tr.position;
            var ArcLength = 5.0f;
            if (!t.Running)
            {
                ttt += Time.deltaTime;
                EditorGUI.BeginChangeCheck();
                if (t.CamSettings != null)
                    for (var i = 0; i < t.CamSettings.Length; i++)
                        if (t.CamSettings[i].Cam != null)
                        {
                            t.CamSettings[i].Center =
                                Handles.PositionHandle(t.CamSettings[i].Center, Quaternion.identity);
                            float yaw = 0;

                            var Pos2 = t.CamSettings[i].Center +
                                       GetToVector(yaw, t.CamSettings[i].Pitch) * t.CamSettings[i].Distance;
                            t.CamSettings[i].Cam.transform.position = Pos2;


                            var HorizSegments = t.CamSettings[i].HorizontalResolution;
                            var Pos3 = new Vector3(t.CamSettings[i].Center.x, Pos2.y, t.CamSettings[i].Center.z);
                            // Handles.DrawLine(Pos2, Pos2 + GetTangent(yaw), 0.01f);
                            for (var i2 = 0; i2 < HorizSegments; i2++)
                            {
                                Handles.DrawWireArc(Pos3, Vector3.up,
                                    GetTangent(90 + 360.0f / HorizSegments * i2 - ArcLength / 2.0f), ArcLength,
                                    Vector3.Distance(Pos3, Pos2));
                                var ToVector = GetToVector(90 + 360.0f / HorizSegments * i2, t.CamSettings[i].Pitch);
                                t.CamSettings[i].Cam.transform.forward = ToVector;
                                var Pos4 = t.CamSettings[i].Center + ToVector * t.CamSettings[i].Distance;
                                Handles.ConeHandleCap(0, Pos4 - ToVector * 0.15f,
                                    t.CamSettings[i].Cam.transform.rotation,
                                    0.2f, EventType.Repaint);
                                Handles.DrawDottedLine(t.CamSettings[i].Center, Pos4, 0.01f);
                                // Handles.ConeHandleCap(0, t.CamSettings[i].Center - t.CamSettings[i].Cam.transform.forward * 0.1f, t.CamSettings[i].Cam.transform.rotation, 0.2f,  EventType.Repaint);
                            }

                            t.CamSettings[i].Cam.transform.forward = (t.CamSettings[i].Center - Pos2).normalized;
                        }

                if (EditorGUI.EndChangeCheck())
                {
                }
            }
        }
    }
}