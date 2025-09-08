#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
 
namespace TrueTrace {
    [InitializeOnLoad]
    public class RenderingPipelineDefines
    {
        enum PipelineType
        {
            Unsupported,
            BuiltInPipeline,
            UniversalPipeline,
            HDPipeline
        }
     
        static RenderingPipelineDefines()
        {
            UpdateDefines();
        }

        static void SetGlobalDefines(string DefineToSet, bool SetValue) {
            string globalDefinesPath = TTPathFinder.GetGlobalDefinesPath();

            if(System.IO.File.Exists(globalDefinesPath)) {
                string[] GlobalDefines = System.IO.File.ReadAllLines(globalDefinesPath);
                int Index = -1;
                for(int i = 0; i < GlobalDefines.Length; i++) {
                    if(GlobalDefines[i].Equals("//END OF DEFINES")) break;
                    string TempString = GlobalDefines[i].Replace("#define ", "");
                    TempString = TempString.Replace("// ", "");
                    if(TempString.Equals(DefineToSet)) {
                        Index = i;
                        break;
                    }
                }
                if(Index == -1) {
                    Debug.Log("Cant find define \"" + DefineToSet + "\"");
                    return;
                }
                GlobalDefines[Index] = GlobalDefines[Index].Replace("// ", "");
                if(!SetValue) GlobalDefines[Index] = "// " + GlobalDefines[Index];

                System.IO.File.WriteAllLines(globalDefinesPath, GlobalDefines);
                AssetDatabase.Refresh();
            } else {Debug.Log("No GlobalDefinesFile");}
        }
     
        /// <summary>
        /// Update the unity pipeline defines for URP
        /// </summary>
        static void UpdateDefines()
        {
            var pipeline = GetPipeline();
     
            if (pipeline == PipelineType.UniversalPipeline)
            {
                AddDefine("UNITY_PIPELINE_URP");
            }
            else
            {
                RemoveDefine("UNITY_PIPELINE_URP");
            }
            if (pipeline == PipelineType.HDPipeline)
            {
                AddDefine("UNITY_PIPELINE_HDRP");
                SetGlobalDefines("HDRP", true);
            }
            else
            {
                RemoveDefine("UNITY_PIPELINE_HDRP");
                SetGlobalDefines("HDRP", false);
            }
        }
     
     
        /// <summary>
        /// Returns the type of renderpipeline that is currently running
        /// </summary>
        /// <returns></returns>
        static PipelineType GetPipeline()
        {
    #if UNITY_2019_1_OR_NEWER
        #if UNITY_6000_0_OR_NEWER
            if (GraphicsSettings.defaultRenderPipeline != null)
        #else
            if (GraphicsSettings.renderPipelineAsset != null)
        #endif
            {
                // SRP
        #if UNITY_6000_0_OR_NEWER
                var srpType = GraphicsSettings.defaultRenderPipeline.GetType().ToString();
        #else
                var srpType = GraphicsSettings.renderPipelineAsset.GetType().ToString();
        #endif
                if (srpType.Contains("HDRenderPipelineAsset"))
                {
                    return PipelineType.HDPipeline;
                }
                else if (srpType.Contains("UniversalRenderPipelineAsset") || srpType.Contains("LightweightRenderPipelineAsset"))
                {
                    return PipelineType.UniversalPipeline;
                }
                else return PipelineType.Unsupported;
            }
    #elif UNITY_2017_1_OR_NEWER
            if (GraphicsSettings.renderPipelineAsset != null) {
                // SRP not supported before 2019
                return PipelineType.Unsupported;
            }
    #endif
            // no SRP
            return PipelineType.BuiltInPipeline;
        }
     
        /// <summary>
        /// Add a custom define
        /// </summary>
        /// <param name="define"></param>
        /// <param name="buildTargetGroup"></param>
        static void AddDefine(string define)
        {
            var definesList = GetDefines();
            if (!definesList.Contains(define))
            {
                definesList.Add(define);
                SetDefines(definesList);
            }
        }
     
        /// <summary>
        /// Remove a custom define
        /// </summary>
        /// <param name="_define"></param>
        /// <param name="_buildTargetGroup"></param>
        public static void RemoveDefine(string define)
        {
            var definesList = GetDefines();
            if (definesList.Contains(define))
            {
                definesList.Remove(define);
                SetDefines(definesList);
            }
        }
     
        public static List<string> GetDefines()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            return defines.Split(';').ToList();
        }
     
        public static void SetDefines(List<string> definesList)
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
            var defines = string.Join(";", definesList.ToArray());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
        }
    }
}
#endif