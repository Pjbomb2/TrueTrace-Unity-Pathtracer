using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 using UnityEngine.UIElements;
using CommonVars;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

#if UNITY_EDITOR
    #if UNITY_2021
    #else
    using UnityEditor.UIElements;
     using UnityEditor;
    namespace TrueTrace {


    public class SavePopup2 : PopupWindowContent
    {
        string PresetName = "Null";
        RayTracingObject ThisOBJ;
        int SaveIndex;

        public SavePopup2(RayTracingObject ThisOBJ, int SaveIndex) {
            this.ThisOBJ = ThisOBJ;
            this.SaveIndex = SaveIndex;
        }
        public override Vector2 GetWindowSize()
        {
            return new Vector2(460, 50);
        }

        public override void OnGUI(Rect rect) {
            // Debug.Log("ONINSPECTORGUI");

            PresetName = GUILayout.TextField(PresetName, 32);
            
            if(GUILayout.Button("Save Preset")) {
                RayObjs PresetRays;
                int CopyIndex = -1;
                UnityEditor.AssetDatabase.Refresh();
                using (var A = new StringReader(Resources.Load<TextAsset>("Utility/MaterialPresets").text)) {
                    var serializer = new XmlSerializer(typeof(RayObjs));
                    PresetRays = serializer.Deserialize(A) as RayObjs;
                    int RayReadCount = PresetRays.RayObj.Count;
                    for(int i = 0; i < RayReadCount; i++) {
                        if(PresetRays.RayObj[i].MatName.Equals(PresetName)) {
                            CopyIndex = i;
                            break;
                        }
                    }
                }
                RayObjectDatas TempRay = new RayObjectDatas() {
                    ID = 0,
                    MatName = PresetName,
                    OptionID = (int)ThisOBJ.MaterialOptions[SaveIndex],
                    TransCol = ThisOBJ.TransmissionColor[SaveIndex],
                    BaseCol = ThisOBJ.BaseColor[SaveIndex],
                    MetRemap = ThisOBJ.MetallicRemap[SaveIndex],
                    RoughRemap = ThisOBJ.RoughnessRemap[SaveIndex],
                    Emiss = ThisOBJ.emmission[SaveIndex],
                    EmissCol = ThisOBJ.EmissionColor[SaveIndex],
                    Rough = ThisOBJ.Roughness[SaveIndex],
                    IOR = ThisOBJ.IOR[SaveIndex],
                    Met = ThisOBJ.Metallic[SaveIndex],
                    SpecTint = ThisOBJ.SpecularTint[SaveIndex],
                    Sheen = ThisOBJ.Sheen[SaveIndex],
                    SheenTint = ThisOBJ.SheenTint[SaveIndex],
                    Clearcoat = ThisOBJ.ClearCoat[SaveIndex],
                    ClearcoatGloss = ThisOBJ.ClearCoatGloss[SaveIndex],
                    Anisotropic = ThisOBJ.Anisotropic[SaveIndex],
                    Flatness = ThisOBJ.Flatness[SaveIndex],
                    DiffTrans = ThisOBJ.DiffTrans[SaveIndex],
                    SpecTrans = ThisOBJ.SpecTrans[SaveIndex],
                    FollowMat = ThisOBJ.FollowMaterial[SaveIndex],
                    ScatterDist = ThisOBJ.ScatterDist[SaveIndex],
                    Spec = ThisOBJ.Specular[SaveIndex],
                    AlphaCutoff = ThisOBJ.AlphaCutoff[SaveIndex],
                    NormStrength = ThisOBJ.NormalStrength[SaveIndex],
                    Hue = ThisOBJ.Hue[SaveIndex],
                    Brightness = ThisOBJ.Brightness[SaveIndex],
                    Contrast = ThisOBJ.Contrast[SaveIndex],
                    Saturation = ThisOBJ.Saturation[SaveIndex],
                    BlendColor = ThisOBJ.BlendColor[SaveIndex],
                    BlendFactor = ThisOBJ.BlendFactor[SaveIndex],
                    MainTexScaleOffset = ThisOBJ.MainTexScaleOffset[SaveIndex],
                    SecondaryTextureScale = ThisOBJ.SecondaryTextureScale[SaveIndex],
                    Rotation = ThisOBJ.Rotation[SaveIndex],
                    Flags = ThisOBJ.Flags[SaveIndex]
                };
                if(CopyIndex != -1) PresetRays.RayObj[CopyIndex] = TempRay;
                else PresetRays.RayObj.Add(TempRay);

                using(StreamWriter writer = new StreamWriter(Application.dataPath + "/TrueTrace/Resources/Utility/MaterialPresets.xml")) {
                    var serializer = new XmlSerializer(typeof(RayObjs));
                    serializer.Serialize(writer.BaseStream, PresetRays);
                    UnityEditor.AssetDatabase.Refresh();
                }
                this.editorWindow.Close();
            }


        }
    }
    public class LoadPopup2 : PopupWindowContent
    {
        RayObjs PresetRays;
        Vector2 ScrollPosition;
        MasterMaterialEditor SourceWindow;
        public LoadPopup2(MasterMaterialEditor editor) {
            this.SourceWindow = editor;
        }
        private void CallEditorFunction(RayObjectDatas RayObj) {
            if(SourceWindow != null) {
                SourceWindow.LoadFunction(RayObj);
            }
        }
        public override Vector2 GetWindowSize()
        {
            return new Vector2(460, 250);
        }

        public override void OnGUI(Rect rect) {
            UnityEditor.AssetDatabase.Refresh();
            using (var A = new StringReader(Resources.Load<TextAsset>("Utility/MaterialPresets").text)) {
                var serializer = new XmlSerializer(typeof(RayObjs));
                PresetRays = serializer.Deserialize(A) as RayObjs;
            }
            int PresetLength = PresetRays.RayObj.Count;
            ScrollPosition = GUILayout.BeginScrollView(ScrollPosition, GUILayout.Width(460), GUILayout.Height(250));
            for(int i = 0; i < PresetLength; i++) {
                GUILayout.BeginHorizontal();
                    if(GUILayout.Button(PresetRays.RayObj[i].MatName)) {CallEditorFunction(PresetRays.RayObj[i]); this.editorWindow.Close();}
                    if(GUILayout.Button("Delete")) {
                        PresetRays.RayObj.RemoveAt(i);
                        using(StreamWriter writer = new StreamWriter(Application.dataPath + "/TrueTrace/Resources/Utility/MaterialPresets.xml")) {
                            var serializer = new XmlSerializer(typeof(RayObjs));
                            serializer.Serialize(writer.BaseStream, PresetRays);
                            UnityEditor.AssetDatabase.Refresh();
                        }
                        OnGUI(new Rect(0,0,100,10));
                    }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
    }



       public class MasterMaterialEditor : EditorWindow {
            [MenuItem("TrueTrace/Master Material Editor")]
            public static void ShowWindow() {
                GetWindow<MasterMaterialEditor>("Material Editor");
            }
            int Selected2;
            RayTracingObject TempRTO2;
            public void LoadFunction(RayObjectDatas RayObj) {
                TempRTO2.MaterialOptions[Selected2] = (RayTracingObject.Options)RayObj.OptionID;
                TempRTO2.TransmissionColor[Selected2] = RayObj.TransCol;
                TempRTO2.BaseColor[Selected2] = RayObj.BaseCol;
                TempRTO2.MetallicRemap[Selected2] = RayObj.MetRemap;
                TempRTO2.RoughnessRemap[Selected2] = RayObj.RoughRemap;
                TempRTO2.emmission[Selected2] = RayObj.Emiss;
                TempRTO2.EmissionColor[Selected2] = RayObj.EmissCol;
                TempRTO2.Roughness[Selected2] = RayObj.Rough;
                TempRTO2.IOR[Selected2] = RayObj.IOR;
                TempRTO2.Metallic[Selected2] = RayObj.Met;
                TempRTO2.SpecularTint[Selected2] = RayObj.SpecTint;
                TempRTO2.Sheen[Selected2] = RayObj.Sheen;
                TempRTO2.SheenTint[Selected2] = RayObj.SheenTint;
                TempRTO2.ClearCoat[Selected2] = RayObj.Clearcoat;
                TempRTO2.ClearCoatGloss[Selected2] = RayObj.ClearcoatGloss;
                TempRTO2.Anisotropic[Selected2] = RayObj.Anisotropic;
                TempRTO2.Flatness[Selected2] = RayObj.Flatness;
                TempRTO2.DiffTrans[Selected2] = RayObj.DiffTrans;
                TempRTO2.SpecTrans[Selected2] = RayObj.SpecTrans;
                TempRTO2.FollowMaterial[Selected2] = RayObj.FollowMat;
                TempRTO2.ScatterDist[Selected2] = RayObj.ScatterDist;
                TempRTO2.Specular[Selected2] = RayObj.Spec;
                TempRTO2.AlphaCutoff[Selected2] = RayObj.AlphaCutoff;
                TempRTO2.NormalStrength[Selected2] = RayObj.NormStrength;
                TempRTO2.Hue[Selected2] = RayObj.Hue;
                TempRTO2.Brightness[Selected2] = RayObj.Brightness;
                TempRTO2.Contrast[Selected2] = RayObj.Contrast;
                TempRTO2.Saturation[Selected2] = RayObj.Saturation;
                TempRTO2.BlendColor[Selected2] = RayObj.BlendColor;
                TempRTO2.BlendFactor[Selected2] = RayObj.BlendFactor;
                TempRTO2.MainTexScaleOffset[Selected2] = RayObj.MainTexScaleOffset;
                TempRTO2.SecondaryTextureScale[Selected2] = RayObj.SecondaryTextureScale;
                TempRTO2.Rotation[Selected2] = RayObj.Rotation;
                TempRTO2.Flags[Selected2] = RayObj.Flags;
                TempRTO2.CallMaterialEdited(true);
            }



            private List<TreeViewItemData<string>> TraverseChildren(Transform t, ref int id, int depth) {
                List<TreeViewItemData<string>> TempList = new List<TreeViewItemData<string>>();
                if(depth > 50) {Debug.Log("MAX DEPTH EXCEEDED"); return TempList;}
                int TransCount = t.childCount;

                RayTracingObject RTO = t.gameObject.GetComponent<RayTracingObject>();
                if(RTO != null) {
                    for(int i = 0; i < RTO.BaseColor.Length; i++) {
                        TempList.Add(new TreeViewItemData<string>(id++, "MAT: " + RTO.Names[i]));
                    }
                }

                for(int i = 0; i < TransCount; i++) {
                    var Obj = t.GetChild(i).gameObject;
                    if(!Obj.activeInHierarchy) continue;
                    RTO = Obj.GetComponent<RayTracingObject>();
                    if(RTO != null) {
                        TempList.Add(new TreeViewItemData<string>(id++, "RTO: " + Obj.name, TraverseChildren(Obj.transform, ref id, depth + 1)));
                    } else {
                        ParentObject PO = Obj.GetComponent<ParentObject>();
                        if(PO != null) {
                            TempList.Add(new TreeViewItemData<string>(id++, "PO: " + Obj.name, TraverseChildren(Obj.transform, ref id, depth + 1)));
                        } else {
                            TempList.Add(new TreeViewItemData<string>(id++, "EMPTY: " + Obj.name, TraverseChildren(Obj.transform, ref id, depth + 1)));
                        }
                    }
                }
                return TempList;
            }

            private void Rebuild() {

            }

            private GameObject Traverse(string[] Names, int Index, Transform t) {
                string TargetName = (((Names[++Index].Replace("EMPTY: ", "")).Replace("PO: ", "")).Replace("RTO: ", "")).Replace("MAT: ", "");
                if(Index == Names.Length - 1) return t.gameObject;
                for(int i = 0; i < t.childCount; i++) {
                    if(t.GetChild(i).gameObject.name.Equals(TargetName)) return Traverse(Names, Index, t.GetChild(i));
                }
                return null;
            }
            private CommonVars.RayObjectDatas Ray;

            private void Copy(RayTracingObject OBJtoWrite, int Index) {
                Ray = new CommonVars.RayObjectDatas() {
                    OptionID = (int)OBJtoWrite.MaterialOptions[Index],
                    TransCol = OBJtoWrite.TransmissionColor[Index],
                    BaseCol = OBJtoWrite.BaseColor[Index],
                    MetRemap = OBJtoWrite.MetallicRemap[Index],
                    RoughRemap = OBJtoWrite.RoughnessRemap[Index],
                    Emiss = OBJtoWrite.emmission[Index],
                    EmissCol = OBJtoWrite.EmissionColor[Index],
                    Rough = OBJtoWrite.Roughness[Index],
                    IOR = OBJtoWrite.IOR[Index],
                    Met = OBJtoWrite.Metallic[Index],
                    SpecTint = OBJtoWrite.SpecularTint[Index],
                    Sheen = OBJtoWrite.Sheen[Index],
                    SheenTint = OBJtoWrite.SheenTint[Index],
                    Clearcoat = OBJtoWrite.ClearCoat[Index],
                    ClearcoatGloss = OBJtoWrite.ClearCoatGloss[Index],
                    Anisotropic = OBJtoWrite.Anisotropic[Index],
                    Flatness = OBJtoWrite.Flatness[Index],
                    DiffTrans = OBJtoWrite.DiffTrans[Index],
                    SpecTrans = OBJtoWrite.SpecTrans[Index],
                    FollowMat = OBJtoWrite.FollowMaterial[Index],
                    ScatterDist = OBJtoWrite.ScatterDist[Index],
                    Spec = OBJtoWrite.Specular[Index],
                    AlphaCutoff = OBJtoWrite.AlphaCutoff[Index],
                    NormStrength = OBJtoWrite.NormalStrength[Index],
                    Hue = OBJtoWrite.Hue[Index],
                    Brightness = OBJtoWrite.Brightness[Index],
                    Contrast = OBJtoWrite.Contrast[Index],
                    Saturation = OBJtoWrite.Saturation[Index],
                    BlendColor = OBJtoWrite.BlendColor[Index],
                    BlendFactor = OBJtoWrite.BlendFactor[Index],
                    MainTexScaleOffset = OBJtoWrite.MainTexScaleOffset[Index],
                    SecondaryTextureScale = OBJtoWrite.SecondaryTextureScale[Index],
                    Rotation = OBJtoWrite.Rotation[Index],
                    Flags = OBJtoWrite.Flags[Index]
                };
            }

            private void Paste(RayTracingObject TempRTO, int NameIndex) {
                TempRTO.MaterialOptions[NameIndex] = (RayTracingObject.Options)Ray.OptionID;
                TempRTO.TransmissionColor[NameIndex] = Ray.TransCol;
                TempRTO.BaseColor[NameIndex] = Ray.BaseCol;
                TempRTO.MetallicRemap[NameIndex] = Ray.MetRemap;
                TempRTO.RoughnessRemap[NameIndex] = Ray.RoughRemap;
                TempRTO.emmission[NameIndex] = Ray.Emiss;
                TempRTO.EmissionColor[NameIndex] = Ray.EmissCol;
                TempRTO.Roughness[NameIndex] = Ray.Rough;
                TempRTO.IOR[NameIndex] = Ray.IOR;
                TempRTO.Metallic[NameIndex] = Ray.Met;
                TempRTO.SpecularTint[NameIndex] = Ray.SpecTint;
                TempRTO.Sheen[NameIndex] = Ray.Sheen;
                TempRTO.SheenTint[NameIndex] = Ray.SheenTint;
                TempRTO.ClearCoat[NameIndex] = Ray.Clearcoat;
                TempRTO.ClearCoatGloss[NameIndex] = Ray.ClearcoatGloss;
                TempRTO.Anisotropic[NameIndex] = Ray.Anisotropic;
                TempRTO.Flatness[NameIndex] = Ray.Flatness;
                TempRTO.DiffTrans[NameIndex] = Ray.DiffTrans;
                TempRTO.SpecTrans[NameIndex] = Ray.SpecTrans;
                TempRTO.FollowMaterial[NameIndex] = Ray.FollowMat;
                TempRTO.ScatterDist[NameIndex] = Ray.ScatterDist;
                TempRTO.Specular[NameIndex] = Ray.Spec;
                TempRTO.AlphaCutoff[NameIndex] = Ray.AlphaCutoff;
                TempRTO.NormalStrength[NameIndex] = Ray.NormStrength;
                TempRTO.Hue[NameIndex] = Ray.Hue;
                TempRTO.Brightness[NameIndex] = Ray.Brightness;
                TempRTO.Contrast[NameIndex] = Ray.Contrast;
                TempRTO.Saturation[NameIndex] = Ray.Saturation;
                TempRTO.BlendColor[NameIndex] = Ray.BlendColor;
                TempRTO.BlendFactor[NameIndex] = Ray.BlendFactor;
                TempRTO.MainTexScaleOffset[NameIndex] = Ray.MainTexScaleOffset;
                TempRTO.SecondaryTextureScale[NameIndex] = Ray.SecondaryTextureScale;
                TempRTO.Rotation[NameIndex] = Ray.Rotation;
                TempRTO.Flags[NameIndex] = Ray.Flags;

                TempRTO.CallMaterialEdited(true);
            }

            private void Propogate(RayTracingObject TempRTO, int NameIndex) {
                    RayTracingObject[] Objects = GameObject.FindObjectsOfType<RayTracingObject>();
                    string Name = TempRTO.Names[NameIndex];
                    foreach(var Obj in Objects) {
                        for(int i = 0; i < Obj.MaterialOptions.Length; i++) {
                            if(Obj.Names[i].Equals(Name)) {
                                Obj.MaterialOptions[i] = TempRTO.MaterialOptions[NameIndex];
                                Obj.BaseColor[i] = TempRTO.BaseColor[NameIndex];
                                Obj.TransmissionColor[i] = TempRTO.TransmissionColor[NameIndex];
                                Obj.emmission[i] = TempRTO.emmission[NameIndex];
                                Obj.EmissionColor[i] = TempRTO.EmissionColor[NameIndex];
                                Obj.Roughness[i] = TempRTO.Roughness[NameIndex];
                                Obj.RoughnessRemap[i] = TempRTO.RoughnessRemap[NameIndex];
                                Obj.MetallicRemap[i] = TempRTO.MetallicRemap[NameIndex];
                                Obj.IOR[i] = TempRTO.IOR[NameIndex];
                                Obj.Metallic[i] = TempRTO.Metallic[NameIndex];
                                Obj.SpecularTint[i] = TempRTO.SpecularTint[NameIndex];
                                Obj.Sheen[i] = TempRTO.Sheen[NameIndex];
                                Obj.SheenTint[i] = TempRTO.SheenTint[NameIndex];
                                Obj.ClearCoat[i] = TempRTO.ClearCoat[NameIndex];
                                Obj.ClearCoatGloss[i] = TempRTO.ClearCoatGloss[NameIndex];
                                Obj.Anisotropic[i] = TempRTO.Anisotropic[NameIndex];
                                Obj.Flatness[i] = TempRTO.Flatness[NameIndex];
                                Obj.DiffTrans[i] = TempRTO.DiffTrans[NameIndex];
                                Obj.SpecTrans[i] = TempRTO.SpecTrans[NameIndex];
                                Obj.FollowMaterial[i] = TempRTO.FollowMaterial[NameIndex];
                                Obj.ScatterDist[i] = TempRTO.ScatterDist[NameIndex];
                                Obj.Specular[i] = TempRTO.Specular[NameIndex];
                                Obj.AlphaCutoff[i] = TempRTO.AlphaCutoff[NameIndex];
                                Obj.BlendColor[i] = TempRTO.BlendColor[NameIndex];
                                Obj.BlendFactor[i] = TempRTO.BlendFactor[NameIndex];
                                Obj.MainTexScaleOffset[i] = TempRTO.MainTexScaleOffset[NameIndex];
                                Obj.SecondaryTextureScale[i] = TempRTO.SecondaryTextureScale[NameIndex];
                                Obj.Rotation[i] = TempRTO.Rotation[NameIndex];
                                Obj.Flags[i] = TempRTO.Flags[NameIndex];
                                Obj.CallMaterialEdited(true);
                            }
                        }
                    }
                    TempRTO.CallMaterialEdited(true);
            }


            public void CreateGUI() {
                TwoPaneSplitView NewView = new TwoPaneSplitView(1, position.width / 2, TwoPaneSplitViewOrientation.Horizontal);

                TreeView MatHierarchyView = new TreeView();

                GameObject obj = GameObject.Find("Scene");
                int ID = 0;
                List<TreeViewItemData<string>> Roots = TraverseChildren(obj.transform, ref ID, 0);

                MatHierarchyView.SetRootItems(Roots);

                NewView.Add(MatHierarchyView);
                
                VisualElement TempWindow = new VisualElement();

                MatHierarchyView.selectionChanged += (evt => {

                    string MatName = MatHierarchyView.GetItemDataForIndex<string>(MatHierarchyView.selectedIndex);
                    if(MatName.Contains("MAT: ")) {
                        List<string> MatParentList = new List<string>();
                        int Reps = 0;
                        int ParentID = MatHierarchyView.GetIdForIndex(MatHierarchyView.selectedIndex);
                        while(Reps < 10 && ParentID != -1) {
                            Reps++;
                            MatParentList.Add(MatHierarchyView.GetItemDataForId<string>(ParentID));
                            ParentID = MatHierarchyView.viewController.GetParentId(ParentID);
                        }
                        MatParentList.Reverse();
                        MatParentList.Insert(0, "Scene");
                        string RTOName = MatHierarchyView.GetItemDataForId<string>(MatHierarchyView.GetParentIdForIndex(MatHierarchyView.selectedIndex));
                        MatName = MatName.Replace("MAT: ", "");
                        int Index = 0;
                        GameObject RTGO = Traverse(MatParentList.ToArray(), Index, obj.transform);
                        if(RTGO != null) {
                            RayTracingObject RTO = RTGO.GetComponent<RayTracingObject>();
                            if(RTO != null) {                            
                                TempWindow.Clear();
                                int Selected = (new List<string>(RTO.Names)).IndexOf(MatName);
                                if(Selected == -1) {for(int i = 0; i < RTO.Names.Length; i++) {Debug.Log(RTO.Names[i]);} Debug.Log("DESIRED: " + MatName); Debug.Log("NO NAME"); Rebuild();}
                                List<string> MatTypeOptions = new List<string>(System.Enum.GetNames(typeof(RayTracingObject.Options)));
                                PopupField<string> MatTypePopup = new PopupField<string>("<b>Material Type</b>");
                                MatTypePopup.choices = MatTypeOptions;
                                MatTypePopup.index = (int)RTO.MaterialOptions[Selected];
                                MatTypePopup.RegisterValueChangedCallback(evt => {RTO.MaterialOptions[Selected] = (RayTracingObject.Options)MatTypePopup.index;RTO.CallMaterialEdited(true);});

                                var SO = new SerializedObject(RTO);

                                ColorField ColField = new ColorField("Base Color") {value = new Color(RTO.BaseColor[Selected].x, RTO.BaseColor[Selected].y, RTO.BaseColor[Selected].z, 1)};
                                ColField.RegisterValueChangedCallback(evt => {RTO.BaseColor[Selected] = new Vector3(evt.newValue.r, evt.newValue.g, evt.newValue.b);RTO.CallMaterialEdited(true);});

                                FloatField EmissField = new FloatField("Emission") {value = RTO.emmission[Selected]};
                                EmissField.RegisterValueChangedCallback(evt => {RTO.emmission[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});

                                ColorField EmissColField = new ColorField("Emission Color") {value = new Color(RTO.EmissionColor[Selected].x, RTO.EmissionColor[Selected].y, RTO.EmissionColor[Selected].z, 1)};
                                EmissColField.RegisterValueChangedCallback(evt => {RTO.EmissionColor[Selected] = new Vector3(evt.newValue.r, evt.newValue.g, evt.newValue.b);RTO.CallMaterialEdited(true);});
                                VisualElement EmissionBar = new VisualElement();
                                EmissionBar.style.flexDirection = FlexDirection.Row;
                                EmissionBar.style.width = 450;
                                EmissionBar.style.height = 20;
                                    Toggle EmissMask = new Toggle("Is Emission Map: ") {value = RTO.Flags[Selected].GetFlag(CommonFunctions.Flags.IsEmissionMask)};
                                    EmissMask.RegisterValueChangedCallback(evt => {RTO.Flags[Selected] = CommonFunctions.SetFlagVar(RTO.Flags[Selected], CommonFunctions.Flags.IsEmissionMask, evt.newValue);RTO.CallMaterialEdited(true);});

                                    Toggle EmissBase = new Toggle("Base Color Is Map: ") {value = RTO.Flags[Selected].GetFlag(CommonFunctions.Flags.BaseIsMap)};
                                    EmissBase.RegisterValueChangedCallback(evt => {RTO.Flags[Selected] = CommonFunctions.SetFlagVar(RTO.Flags[Selected], CommonFunctions.Flags.BaseIsMap, evt.newValue);RTO.CallMaterialEdited(true);});

                                    Toggle EmissReplace = new Toggle("Replace Base Color: ") {value = RTO.Flags[Selected].GetFlag(CommonFunctions.Flags.ReplaceBase)};
                                    EmissReplace.RegisterValueChangedCallback(evt => {RTO.Flags[Selected] = CommonFunctions.SetFlagVar(RTO.Flags[Selected], CommonFunctions.Flags.ReplaceBase, evt.newValue);RTO.CallMaterialEdited(true);});
                                EmissionBar.Add(EmissMask);
                                EmissionBar.Add(EmissBase);
                                EmissionBar.Add(EmissReplace);

                                VisualElement MiscBar1 = new VisualElement();
                                MiscBar1.style.flexDirection = FlexDirection.Row;
                                MiscBar1.style.width = 450;
                                MiscBar1.style.height = 20;

                                Slider RoughnessSlider = new Slider() {label = RTO.Flags[Selected].GetFlag(CommonFunctions.Flags.UseSmoothness) ? "Smoothness: " : "Roughness: ", value = RTO.Flags[Selected].GetFlag(CommonFunctions.Flags.UseSmoothness) ? (1.0f - RTO.Roughness[Selected]) : RTO.Roughness[Selected], highValue = 1.0f, lowValue = 0};
                                RoughnessSlider.RegisterValueChangedCallback(evt => {RTO.Roughness[Selected] = (RTO.Flags[Selected].GetFlag(CommonFunctions.Flags.UseSmoothness) ? (1.0f - evt.newValue) : evt.newValue); RTO.CallMaterialEdited(true);});
                                RoughnessSlider.style.width = 350;
                                RoughnessSlider.showInputField = true;
                                    Toggle IsSmoothness = new Toggle("Use Smoothness: ") {value = RTO.Flags[Selected].GetFlag(CommonFunctions.Flags.UseSmoothness)};
                                    IsSmoothness.RegisterValueChangedCallback(evt => {RTO.Flags[Selected] = CommonFunctions.SetFlagVar(RTO.Flags[Selected], CommonFunctions.Flags.UseSmoothness, evt.newValue); if(evt.newValue) RoughnessSlider.label = "Smoothness: "; else RoughnessSlider.label = "Roughness";RTO.CallMaterialEdited(true); RTO.Roughness[Selected] = 1.0f - RTO.Roughness[Selected];});

                                    Toggle InvertSmoothTex = new Toggle("Invert Roughness Tex: ") {value = RTO.Flags[Selected].GetFlag(CommonFunctions.Flags.InvertSmoothnessTexture)};
                                    InvertSmoothTex.RegisterValueChangedCallback(evt => {RTO.Flags[Selected] = CommonFunctions.SetFlagVar(RTO.Flags[Selected], CommonFunctions.Flags.InvertSmoothnessTexture, evt.newValue);RTO.CallMaterialEdited(true);});

                                    Toggle ShadowCaster = new Toggle("Dont Cast Shadows: ") {value = RTO.Flags[Selected].GetFlag(CommonFunctions.Flags.ShadowCaster)};
                                    ShadowCaster.RegisterValueChangedCallback(evt => {RTO.Flags[Selected] = CommonFunctions.SetFlagVar(RTO.Flags[Selected], CommonFunctions.Flags.ShadowCaster, evt.newValue);RTO.CallMaterialEdited(true);});
                                MiscBar1.Add(ShadowCaster);
                                MiscBar1.Add(IsSmoothness);
                                MiscBar1.Add(InvertSmoothTex);


                                VisualElement MiscBar2 = new VisualElement();
                                MiscBar2.style.flexDirection = FlexDirection.Row;
                                MiscBar2.style.width = 450;
                                MiscBar2.style.height = 20;
                                    Toggle Invisible = new Toggle("Invisible: ") {value = RTO.Flags[Selected].GetFlag(CommonFunctions.Flags.Invisible)};
                                    Invisible.RegisterValueChangedCallback(evt => {RTO.Flags[Selected] = CommonFunctions.SetFlagVar(RTO.Flags[Selected], CommonFunctions.Flags.Invisible, evt.newValue);RTO.CallMaterialEdited(true);});

                                    // Toggle IsBackground = new Toggle("Is Background: ") {value = RTO.Flags[Selected].GetFlag(CommonFunctions.Flags.IsBackground)};
                                    // IsBackground.RegisterValueChangedCallback(evt => {RTO.Flags[Selected] = CommonFunctions.SetFlagVar(RTO.Flags[Selected], CommonFunctions.Flags.IsBackground, evt.newValue);RTO.CallMaterialEdited(true);});

                                    // Toggle BackgroundBleed = new Toggle("Sky Passes Through Background: ") {value = RTO.Flags[Selected].GetFlag(CommonFunctions.Flags.BackgroundBleed)};
                                    // BackgroundBleed.RegisterValueChangedCallback(evt => {RTO.Flags[Selected] = CommonFunctions.SetFlagVar(RTO.Flags[Selected], CommonFunctions.Flags.BackgroundBleed, evt.newValue);RTO.CallMaterialEdited(true);});
                                MiscBar2.Add(Invisible);
                                // MiscBar2.Add(IsBackground);
                                // MiscBar2.Add(BackgroundBleed);


                                MinMaxSlider RoughnessMinMax = new MinMaxSlider() {value = RTO.RoughnessRemap[Selected], label = "Roughness Remap: ", highLimit = 1, lowLimit = 0};
                                RoughnessMinMax.RegisterValueChangedCallback(evt => {RTO.RoughnessRemap[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                RoughnessMinMax.style.width = 350;

                                Slider MetallicSlider = new Slider() {label = "Metallic: ", value = RTO.Metallic[Selected], highValue = 1.0f, lowValue = 0};
                                MetallicSlider.RegisterValueChangedCallback(evt => {RTO.Metallic[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                MetallicSlider.style.width = 350;
                                MetallicSlider.showInputField = true;

                                MinMaxSlider MetallicMinMax = new MinMaxSlider() {value = RTO.MetallicRemap[Selected], label = "Metallic Remap: ", highLimit = 1, lowLimit = 0};
                                MetallicMinMax.RegisterValueChangedCallback(evt => {RTO.MetallicRemap[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                MetallicMinMax.style.width = 350;

                                Slider IORSlider = new Slider() {label = "Index of Refraction: ", value = RTO.IOR[Selected], highValue = 6.0f, lowValue = 1.0f};
                                IORSlider.RegisterValueChangedCallback(evt => {RTO.IOR[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                IORSlider.style.width = 350;
                                IORSlider.showInputField = true;

                                Slider SpecularSlider = new Slider() {label = "Specular: ", value = RTO.Specular[Selected], highValue = 1.0f, lowValue = 0.0f};
                                SpecularSlider.RegisterValueChangedCallback(evt => {RTO.Specular[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                SpecularSlider.style.width = 350;
                                SpecularSlider.showInputField = true;

                                Slider SpecTintSlider = new Slider() {label = "Specular Tint: ", value = RTO.SpecularTint[Selected], highValue = 1.0f, lowValue = 0.0f};
                                SpecTintSlider.RegisterValueChangedCallback(evt => {RTO.SpecularTint[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                SpecTintSlider.style.width = 350;
                                SpecTintSlider.showInputField = true;

                                Slider SheenSlider = new Slider() {label = "Sheen: ", value = RTO.Sheen[Selected], highValue = 10.0f, lowValue = 0.0f};
                                SheenSlider.RegisterValueChangedCallback(evt => {RTO.Sheen[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                SheenSlider.style.width = 350;
                                SheenSlider.showInputField = true;

                                Slider SheenTintSlider = new Slider() {label = "Sheen Tint: ", value = RTO.SheenTint[Selected], highValue = 1.0f, lowValue = 0.0f};
                                SheenTintSlider.RegisterValueChangedCallback(evt => {RTO.SheenTint[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                SheenTintSlider.style.width = 350;
                                SheenTintSlider.showInputField = true;

                                Slider ClearCoatSlider = new Slider() {label = "ClearCoat: ", value = RTO.ClearCoat[Selected], highValue = 1.0f, lowValue = 0.0f};
                                ClearCoatSlider.RegisterValueChangedCallback(evt => {RTO.ClearCoat[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                ClearCoatSlider.style.width = 350;
                                ClearCoatSlider.showInputField = true;

                                Slider ClearCoatGlossSlider = new Slider() {label = "ClearCoat Gloss: ", value = RTO.ClearCoatGloss[Selected], highValue = 1.0f, lowValue = 0.0f};
                                ClearCoatGlossSlider.RegisterValueChangedCallback(evt => {RTO.ClearCoatGloss[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                ClearCoatGlossSlider.style.width = 350;
                                ClearCoatGlossSlider.showInputField = true;

                                Slider AnisotropicSlider = new Slider() {label = "Anisotropic: ", value = RTO.Anisotropic[Selected], highValue = 1.0f, lowValue = 0.0f};
                                AnisotropicSlider.RegisterValueChangedCallback(evt => {RTO.Anisotropic[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                AnisotropicSlider.style.width = 350;
                                AnisotropicSlider.showInputField = true;

                                Slider SpecTransSlider = new Slider() {label = "SpecTrans(Glass): ", value = RTO.SpecTrans[Selected], highValue = 1.0f, lowValue = 0.0f};
                                SpecTransSlider.RegisterValueChangedCallback(evt => {RTO.SpecTrans[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                SpecTransSlider.style.width = 350;
                                SpecTransSlider.showInputField = true;

                                Toggle ThinToggle = new Toggle() {label = "Thin: ", value = RTO.Flags[Selected].GetFlag(CommonFunctions.Flags.Thin)};
                                ThinToggle.RegisterValueChangedCallback(evt => {RTO.Flags[Selected] = CommonFunctions.SetFlagVar(RTO.Flags[Selected], CommonFunctions.Flags.Thin, evt.newValue);RTO.CallMaterialEdited(true);});

                                Slider DiffTransSlider = new Slider() {label = "DiffTrans: ", value = RTO.DiffTrans[Selected], highValue = 1.0f, lowValue = 0.0f};
                                DiffTransSlider.RegisterValueChangedCallback(evt => {RTO.DiffTrans[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                DiffTransSlider.style.width = 350;
                                DiffTransSlider.showInputField = true;

                                Vector3Field TransmissionColor = new Vector3Field() {label = "Transmission Color: ", value = RTO.TransmissionColor[Selected]};
                                TransmissionColor.RegisterValueChangedCallback(evt => {RTO.TransmissionColor[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                TransmissionColor.style.width = 350;

                                Slider FlatnessSlider = new Slider() {label = "Flatness: ", value = RTO.Flatness[Selected], highValue = 1.0f, lowValue = 0.0f};
                                FlatnessSlider.RegisterValueChangedCallback(evt => {RTO.Flatness[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                FlatnessSlider.style.width = 350;
                                FlatnessSlider.showInputField = true;

                                Slider ScatterDistSlider = new Slider() {label = "ScatterDist: ", value = RTO.ScatterDist[Selected], highValue = 5.0f, lowValue = 0.0f};
                                ScatterDistSlider.RegisterValueChangedCallback(evt => {RTO.ScatterDist[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                ScatterDistSlider.style.width = 350;
                                ScatterDistSlider.showInputField = true;

                                Slider AlphaCutoffSlider = new Slider() {label = "AlphaCutoff: ", value = RTO.AlphaCutoff[Selected], highValue = 1.0f, lowValue = 0.01f};
                                AlphaCutoffSlider.RegisterValueChangedCallback(evt => {RTO.AlphaCutoff[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                AlphaCutoffSlider.style.width = 350;
                                AlphaCutoffSlider.showInputField = true;

                                Slider NormalStrengthSlider = new Slider() {label = "NormalStrength: ", value = RTO.NormalStrength[Selected], highValue = 5.0f, lowValue = 0.0f};
                                NormalStrengthSlider.RegisterValueChangedCallback(evt => {RTO.NormalStrength[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                NormalStrengthSlider.style.width = 350;
                                NormalStrengthSlider.showInputField = true;

                                Toggle FollowMaterial = new Toggle() {label = "FollowMaterial: ", value = RTO.FollowMaterial[Selected]};
                                FollowMaterial.RegisterValueChangedCallback(evt => {RTO.FollowMaterial[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});

                                Slider HueSlider = new Slider() {label = "Hue: ", value = RTO.Hue[Selected], highValue = 1.0f, lowValue = 0.0f};
                                HueSlider.RegisterValueChangedCallback(evt => {RTO.Hue[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                HueSlider.style.width = 350;
                                HueSlider.showInputField = true;

                                Slider BrightnessSlider = new Slider() {label = "Brightness: ", value = RTO.Brightness[Selected], highValue = 5.0f, lowValue = 0.0f};
                                BrightnessSlider.RegisterValueChangedCallback(evt => {RTO.Brightness[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                BrightnessSlider.style.width = 350;
                                BrightnessSlider.showInputField = true;

                                Slider SaturationSlider = new Slider() {label = "Saturation: ", value = RTO.Saturation[Selected], highValue = 2.0f, lowValue = 0.0f};
                                SaturationSlider.RegisterValueChangedCallback(evt => {RTO.Saturation[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                SaturationSlider.style.width = 350;
                                SaturationSlider.showInputField = true;

                                Slider ContrastSlider = new Slider() {label = "Contrast: ", value = RTO.Contrast[Selected], highValue = 2.0f, lowValue = 0.0f};
                                ContrastSlider.RegisterValueChangedCallback(evt => {RTO.Contrast[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                ContrastSlider.style.width = 350;
                                ContrastSlider.showInputField = true;

                                ColorField BlendColField = new ColorField("Blend Color") {value = new Color(RTO.BlendColor[Selected].x, RTO.BlendColor[Selected].y, RTO.BlendColor[Selected].z, 1)};
                                BlendColField.RegisterValueChangedCallback(evt => {RTO.BlendColor[Selected] = new Vector3(evt.newValue.r, evt.newValue.g, evt.newValue.b);RTO.CallMaterialEdited(true);});

                                Slider BlendSlider = new Slider() {label = "Blend Factor: ", value = RTO.BlendFactor[Selected], highValue = 1.0f, lowValue = 0.0f};
                                BlendSlider.RegisterValueChangedCallback(evt => {RTO.BlendFactor[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                BlendSlider.style.width = 350;
                                BlendSlider.showInputField = true;

                                Vector4Field MainTexField = new Vector4Field() {label = "MainTex Scale/Offset: ", value = RTO.MainTexScaleOffset[Selected]};
                                MainTexField.RegisterValueChangedCallback(evt => {RTO.MainTexScaleOffset[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                MainTexField.style.width = 350;

                                Vector2Field SecondaryTexField = new Vector2Field() {label = "SecondaryTex Scale/Offset: ", value = RTO.SecondaryTextureScale[Selected]};
                                SecondaryTexField.RegisterValueChangedCallback(evt => {RTO.SecondaryTextureScale[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                SecondaryTexField.style.width = 350;

                                Slider RotationSlider = new Slider() {label = "Texture Rotation: ", value = RTO.Rotation[Selected], highValue = 1.0f, lowValue = 0.0f};
                                RotationSlider.RegisterValueChangedCallback(evt => {RTO.Rotation[Selected] = evt.newValue;RTO.CallMaterialEdited(true);});
                                RotationSlider.style.width = 350;
                                RotationSlider.showInputField = true;

                                Button SavePresetButton = new Button(() => UnityEditor.PopupWindow.Show(new Rect(0,0,10,10), new SavePopup2(RTO, Selected))) {text = "Save Material Preset"};
                                Button LoadPresetButton = new Button(() => {TempRTO2 = RTO; Selected2 = Selected; UnityEditor.PopupWindow.Show(new Rect(0,0,100,10), new LoadPopup2(this));}){text = "Load Material Preset"};

                                TempWindow.Add(new Label(RTO.name));
                                TempWindow.Add(SavePresetButton);
                                TempWindow.Add(LoadPresetButton);
                                TempWindow.Add(MatTypePopup);
                                TempWindow.Add(ColField);
                                TempWindow.Add(EmissField);
                                TempWindow.Add(EmissColField);
                                TempWindow.Add(EmissionBar);
                                TempWindow.Add(MiscBar1);
                                TempWindow.Add(MiscBar2);
                                TempWindow.Add(MetallicSlider);
                                TempWindow.Add(MetallicMinMax);
                                TempWindow.Add(RoughnessSlider);
                                TempWindow.Add(RoughnessMinMax);

                                TempWindow.Add(IORSlider);
                                TempWindow.Add(SpecularSlider);
                                TempWindow.Add(SpecTintSlider);
                                TempWindow.Add(SheenSlider);
                                TempWindow.Add(SheenTintSlider);
                                TempWindow.Add(ClearCoatSlider);
                                TempWindow.Add(ClearCoatGlossSlider);
                                TempWindow.Add(AnisotropicSlider);
                                TempWindow.Add(SpecTransSlider);
                                TempWindow.Add(ThinToggle);
                                TempWindow.Add(DiffTransSlider);
                                TempWindow.Add(TransmissionColor);
                                TempWindow.Add(FlatnessSlider);
                                TempWindow.Add(ScatterDistSlider);
                                TempWindow.Add(AlphaCutoffSlider);
                                TempWindow.Add(NormalStrengthSlider);

                                TempWindow.Add(HueSlider);
                                TempWindow.Add(BrightnessSlider);
                                TempWindow.Add(SaturationSlider);
                                TempWindow.Add(ContrastSlider);

                                TempWindow.Add(BlendColField);
                                TempWindow.Add(BlendSlider);

                                TempWindow.Add(MainTexField);
                                TempWindow.Add(SecondaryTexField);
                                TempWindow.Add(RotationSlider);



                                Button CopyButton = new Button(() => Copy(RTO, Selected)) {text = "Copy"};
                                Button PasteButton = new Button(() => Paste(RTO, Selected)){text = "Paste"};
                                Button PropogateButton = new Button(() => Propogate(RTO, Selected)){text = "Propogate Materials"};
                                SO.ApplyModifiedProperties();   
                                TempWindow.Add(CopyButton);
                                TempWindow.Add(PasteButton);
                                TempWindow.Add(PropogateButton);



                            } else {Debug.Log("NO RTO"); Rebuild();}
                        } else {Debug.Log("NO RTGO"); Rebuild();}
                    } else {Debug.Log("NO MAT"); Rebuild();}

                });


                Label TempLabel = new Label("EEEE");
                TempWindow.Add(TempLabel);
                NewView.Add(TempWindow);

                rootVisualElement.Add(NewView);



            }



        }
    }
    #endif
#endif