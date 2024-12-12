using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
namespace TrueTrace {
    public class TTTimedScreenShotter : MonoBehaviour
    {
        public int SamplesBetweenShots = 1000;
        public bool ResetSampCountAfterShot = false;

        void Update() {
            if(((RayTracingMaster.SampleCount % SamplesBetweenShots) == SamplesBetweenShots - 1 && !ResetSampCountAfterShot) || (RayTracingMaster.SampleCount >= SamplesBetweenShots && ResetSampCountAfterShot)) {
                ScreenCapture.CaptureScreenshot(PlayerPrefs.GetString("ScreenShotPath") + "/" + System.DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ", " + RayTracingMaster.SampleCount + " Samples.png");
                UnityEditor.AssetDatabase.Refresh();
                if(ResetSampCountAfterShot) {
                    RayTracingMaster.SampleCount = 0;
                    GameObject.Find("Scene").GetComponent<RayTracingMaster>().FramesSinceStart = 0;
                }
            }
        }
    }
}
#endif