using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrueTrace {//This is for external interaction with your scripts.  If there is anything you want me to add here, to let you control easily, please let me know on discord, twitter, or github.
    public static class TTInterface
    {
        public static void SetTTSettings(string SettingsName) {
            RayTracingMaster.RayMaster.LocalTTSettingsName = SettingsName;
            RayTracingMaster.RayMaster.LoadTT();
        }
        public static void SetTTSettings(TTSettings Settings) {
            if(RayTracingMaster.RayMaster != null) {
                RayTracingMaster.RayMaster.LocalTTSettings = Settings;
                RayTracingMaster.RayMaster.LocalTTSettingsName = Settings.name;
            }
        }
        public static void CallUpdatedTextureMappings(RayTracingObject TargetMat) {
            TargetMat.CallTilingScrolled();
        }
        public static void CallUpdatedTextureMappings(GameObject TargetMat) {
            if(TargetMat.TryGetComponent<RayTracingObject>(out RayTracingObject Targ)) {
                Targ.CallTilingScrolled();
            }
        }
        public static void SetTTEnabled(bool TTEnable) {
            if(RayTracingMaster.RayMaster != null) {
                RayTracingMaster.RayMaster.RunTrueTrace = TTEnable;
            }
        }
    }
}