using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrueTrace {
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
    }
}