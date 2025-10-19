using System;
using System.IO;
using UnityEngine;

namespace TrueTrace
{
    public static class TTPathFinder
    {
        private static string baseResourcePath;
        public static string CurrentTargetFile;

        public static string GetResourcePath()
        {
            if (!string.IsNullOrEmpty(baseResourcePath))
                return NormalizePath(baseResourcePath);

            // Use more precise search parameters to reduce overhead and potential errors
            var searchPattern = "Resources";
            var directories = Directory.GetDirectories(Application.dataPath, searchPattern, SearchOption.AllDirectories);

            foreach (var dir in directories)
            {
                string normalizedDir = NormalizePath(dir);
                
                if (normalizedDir.EndsWith("/TrueTrace/Resources"))
                {
                    baseResourcePath = normalizedDir;
                    return normalizedDir;
                }
            }

            Debug.LogError("Resource path not found.");  // Use Error logging for better visibility
            throw new Exception("Resource path not found.");
        }
        
        private static string NormalizePath(string path)
        {
            // Always use forward slashes to prevent platform-specific issues
            return Path.Combine(path).Replace('\\', '/');
        }

        // Ensure all paths returned from these methods are normalized
        public static string GetSaveFilePath() => NormalizePath(Path.Combine(GetResourcePath(), "Utility/SaveFile.xml"));
        public static string GetGlobalDefinesPath() => NormalizePath(Path.Combine(GetResourcePath(), "GlobalDefines.cginc"));
        public static string GetMaterialPresetsPath() => NormalizePath(Path.Combine(GetResourcePath(), "Utility/MaterialPresets/"));
        public static string GetMaterialMappingsPath() => NormalizePath(Path.Combine(GetResourcePath(), "Utility/MaterialMappings.xml"));
    }
}