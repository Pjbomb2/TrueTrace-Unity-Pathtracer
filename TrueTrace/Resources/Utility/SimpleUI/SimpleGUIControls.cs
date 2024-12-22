using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;

namespace TrueTrace {
    public class SimpleGUIControls : MonoBehaviour
    {
        RayTracingMaster RayMaster;
        public void Start() {
            RayMaster = GameObject.Find("Scene").GetComponent<RayTracingMaster>();
            RayMaster.LocalTTSettings.DenoiserMethod = 1;
            RayMaster.LocalTTSettings.Accumulate = true;
            RayMaster.LocalTTSettings.ClayMode = false;
            RayMaster.LocalTTSettings.OIDNFrameCount = 50;
        }
        public void Exit() {
            Application.Quit();
        }
        public void ToggleClay(bool BoolIn) {
            RayMaster.LocalTTSettings.ClayMode = BoolIn;
        }
        public void ToggleASVGF(bool BoolIn) {
            RayMaster.LocalTTSettings.DenoiserMethod = BoolIn ? 1 : 0;
        }
        public void SetOIDNFrameCount(string StringIn) {
            int IntResult = 0;
            if(int.TryParse(StringIn, out IntResult)) {
                RayMaster.LocalTTSettings.OIDNFrameCount = IntResult;
            }
        }

    }
}