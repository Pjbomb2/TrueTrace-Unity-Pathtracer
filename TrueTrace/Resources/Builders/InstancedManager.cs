using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;
using System.Threading;
#pragma warning disable 4014

namespace TrueTrace {
    [System.Serializable][ExecuteInEditMode]
    public class InstancedManager : MonoBehaviour
    {
        [HideInInspector] public List<ParentObject> AddQue;
        [HideInInspector] public List<ParentObject> RemoveQue;
        [HideInInspector] public List<ParentObject> BuildQue;
        [HideInInspector] public List<ParentObject> RenderQue;
        [HideInInspector] public List<ParentObject> TempQue;
        [HideInInspector] public List<Task> CurrentlyActiveTasks;
        [HideInInspector] public bool ParentCountHasChanged;
        [HideInInspector] public bool NeedsToUpdateTextures;
        [HideInInspector] public static bool NeedsToReinit = false; 
        public struct InstanceData {
            public List<InstancedObject> InstanceTargets;
            public Mesh LocalMesh;
            public int SubMeshCount;
            public RenderParams LocalRendp;
            public InstanceTransformData[] InstTransfArray;
        }
        Dictionary<ParentObject, InstanceData> InstanceIndexes;
        public struct InstanceTransformData {
            public Matrix4x4 objectToWorld;
            public Matrix4x4 prevObjectToWorld;
        }

        public void InitRelationships() {
            NeedsToReinit = false;
            TempQue = new List<ParentObject>(this.GetComponentsInChildren<ParentObject>(false));
            InstanceIndexes = new Dictionary<ParentObject, InstanceData>();
            InstancedObject[] InstanceQues = GameObject.FindObjectsOfType<InstancedObject>(false);
            int InstCount = InstanceQues.Length;
            List<InstancedObject> TempObject2 = new List<InstancedObject>();
            for(int i = 0; i < InstCount; i++) {
                if(InstanceQues[i].InstanceParent != null) TempObject2.Add(InstanceQues[i]);
            }
            InstanceQues = TempObject2.ToArray();
            InstCount = InstanceQues.Length;
            for(int i = 0; i < InstCount; i++) {
                if (InstanceIndexes.TryGetValue(InstanceQues[i].InstanceParent, out InstanceData ExistingList)) {
                    ExistingList.InstanceTargets.Add(InstanceQues[i]);
                } else {
                    InstanceData TempInst = new InstanceData();
                    TempInst.InstanceTargets = new List<InstancedObject>();
                    TempInst.InstanceTargets.Add(InstanceQues[i]);
                    if(InstanceQues[i].InstanceParent.gameObject.TryGetComponent<MeshFilter>(out MeshFilter TempFilter)) TempInst.LocalMesh = TempFilter.sharedMesh;
                    InstanceQues[i].InstanceParent.gameObject.TryGetComponent<MeshRenderer>(out MeshRenderer TempRend);
                    TempInst.SubMeshCount = TempInst.LocalMesh.subMeshCount;
                    TempInst.LocalRendp = new RenderParams();
                    // for(int i2 = 0; i2 < TempInst.SubMeshCount; i2++) {
                        TempRend.sharedMaterials[0].enableInstancing = true;
                        TempInst.LocalRendp = new RenderParams(TempRend.sharedMaterials[0]);
                        TempInst.LocalRendp.motionVectorMode = MotionVectorGenerationMode.Object;
                        TempInst.LocalRendp.worldBounds = new Bounds(Vector3.zero, new Vector3(999999,999999,999999));
                    // }
                    InstanceIndexes.Add(InstanceQues[i].InstanceParent, TempInst);
                }
            }
            InstCount = TempQue.Count;
            for(int i = 0; i < InstCount; i++) {
                if (InstanceIndexes.TryGetValue(TempQue[i], out InstanceData ExistingList)) {
                    ExistingList.InstTransfArray = new InstanceTransformData[ExistingList.InstanceTargets.Count];
                    InstanceIndexes[TempQue[i]] = ExistingList;
                }
            }

        }
        public bool DoRendering = true;
        public void Update() {
            RenderInstances();
        }

        public void RenderInstances() {
            if(DoRendering) {
                if(InstanceIndexes == null || TempQue == null || TempQue.Count == 0 || NeedsToReinit) InitRelationships();
                int Coun1 = TempQue.Count;
                for(int i = 0; i < Coun1; i++) {
                    if (InstanceIndexes.TryGetValue(TempQue[i], out InstanceData ExistingList)) {
                        int Coun2 = ExistingList.InstanceTargets.Count;
                        Bounds TempBounds = ExistingList.LocalMesh.bounds;
                        Matrix4x4 TempIden;
                        if(ExistingList.InstTransfArray == null) {
                            NeedsToReinit = true;
                            continue;
                        }
                        for(int i2 = 0; i2 < Coun2; i2++) {
                            TempIden = Matrix4x4.identity;
                            ExistingList.InstTransfArray[i2].prevObjectToWorld = ExistingList.InstTransfArray[i2].objectToWorld;
                            if(TempQue[i].RenderImposters) ExistingList.InstTransfArray[i2].objectToWorld.SetTRS(ExistingList.InstanceTargets[i2].transform.position +  Vector3.Scale(TempBounds.center, ExistingList.InstanceTargets[i2].transform.lossyScale), ExistingList.InstanceTargets[i2].transform.rotation, Vector3.Scale(ExistingList.InstanceTargets[i2].transform.lossyScale, TempBounds.extents * 2.0f));
                            else ExistingList.InstTransfArray[i2].objectToWorld = ExistingList.InstanceTargets[i2].transform.localToWorldMatrix;
                        }

                        for(int i2 = 0; i2 < ExistingList.SubMeshCount; i2++) {try {
                            int Count = ExistingList.InstTransfArray.Length;
                            int Offset = 0;
                            int Batches = Mathf.CeilToInt((float)Count / 511.0f);
                            for(int i3 = 0; i3 < Batches; i3++) {
                                Graphics.RenderMeshInstanced(ExistingList.LocalRendp, TempQue[i].RenderImposters ? Resources.GetBuiltinResource<Mesh>("Cube.fbx") : ExistingList.LocalMesh, i2, ExistingList.InstTransfArray, Mathf.Min(Count - Offset, 511), Offset);
                                Offset += 511;
                            }

                        } catch(System.Exception E) {}}
                    }
                }
            }
        }

        public void init()
        {
            AddQue = new List<ParentObject>();
            RemoveQue = new List<ParentObject>();
            BuildQue = new List<ParentObject>();
            RenderQue = new List<ParentObject>();
            CurrentlyActiveTasks = new List<Task>();
        }

        public void ClearAll()
        {//My attempt at clearing memory
            ParentObject[] ChildrenObjects = this.GetComponentsInChildren<ParentObject>();
            foreach (ParentObject obj in ChildrenObjects)
                obj.ClearAll();
        }
        public int RunningTasks;
        public void EditorBuild()
        {
            init();
            AddQue = new List<ParentObject>();
            RemoveQue = new List<ParentObject>();
            RenderQue = new List<ParentObject>();
            CurrentlyActiveTasks = new List<Task>();
            BuildQue = new List<ParentObject>(GetComponentsInChildren<ParentObject>());
            RunningTasks = 0;
            for (int i = 0; i < BuildQue.Count; i++)
            {
                var CurrentRep = i;
                BuildQue[CurrentRep].LoadData();
                Task t1 = Task.Run(() => { BuildQue[CurrentRep].BuildTotal(); RunningTasks--; });
                RunningTasks++;
                CurrentlyActiveTasks.Add(t1);
            }
        }

        public void BuildCombined()
        {
            init();
            List<ParentObject> TempQue = new List<ParentObject>(GetComponentsInChildren<ParentObject>());
            for (int i = 0; i < TempQue.Count; i++)
            {
                if (TempQue[i].HasCompleted && !TempQue[i].NeedsToUpdate) RenderQue.Add(TempQue[i]);
                else BuildQue.Add(TempQue[i]);
            }
            for (int i = 0; i < BuildQue.Count; i++)
            {
                var CurrentRep = i;
                BuildQue[CurrentRep].LoadData();
                CurrentlyActiveTasks.Add(Task.Run(() => BuildQue[CurrentRep].BuildTotal()));
            }
        }

        public void UpdateRenderAndBuildQues(ref bool NeedsToUpdate)
        {

            // UnityEngine.Profiling.Profiler.BeginSample("Instaced RemoveQue");
            int RemoveQueCount = RemoveQue.Count - 1;
            for (int i = RemoveQueCount; i >= 0; i--)
            {
                if (RenderQue.Contains(RemoveQue[i]))
                    RenderQue.Remove(RemoveQue[i]);
                else if (BuildQue.Contains(RemoveQue[i]))
                {
                    CurrentlyActiveTasks.RemoveAt(BuildQue.IndexOf(RemoveQue[i]));
                    BuildQue.Remove(RemoveQue[i]);
                }
                NeedsToUpdate = true;
                RemoveQue.RemoveAt(i);
            }
            // UnityEngine.Profiling.Profiler.EndSample();
            // UnityEngine.Profiling.Profiler.BeginSample("Instaced AddQue");
            int AddQueCount = AddQue.Count - 1;
            for (int i = AddQueCount; i >= 0; i--)
            {
                var CurrentRep = BuildQue.Count;
                if(AddQue[i].enabled) {
                    BuildQue.Add(AddQue[i]);
                    BuildQue[CurrentRep].LoadData();
                    CurrentlyActiveTasks.Add(Task.Run(() => BuildQue[CurrentRep].BuildTotal()));
                }
                AddQue.RemoveAt(i);
            }
            // UnityEngine.Profiling.Profiler.EndSample();
            // UnityEngine.Profiling.Profiler.BeginSample("Instaced BuildQue");
            int BuildQueCount = BuildQue.Count - 1;
            for (int i = BuildQueCount; i >= 0; i--)
            {//Promotes from Build Que to Render Que
                if (CurrentlyActiveTasks[i].IsFaulted) {
                    Debug.Log(CurrentlyActiveTasks[i].Exception + ", " + BuildQue[i].Name);//Fuck, something fucked up
                    AddQue.Add(BuildQue[i]);
                    BuildQue.RemoveAt(i);
                    CurrentlyActiveTasks.RemoveAt(i);
                } else if (CurrentlyActiveTasks[i].Status == TaskStatus.RanToCompletion) {
                    if (BuildQue[i].AggTriangles == null || BuildQue[i].AggNodes == null) {
                        AddQue.Add(BuildQue[i]);
                        BuildQue.RemoveAt(i);
                        CurrentlyActiveTasks.RemoveAt(i);
                    } else {
                        BuildQue[i].SetUpBuffers();
                        RenderQue.Add(BuildQue[i]);
                        BuildQue.RemoveAt(i);
                        CurrentlyActiveTasks.RemoveAt(i);
                        NeedsToUpdate = true;
                    }
                }
            }
            // UnityEngine.Profiling.Profiler.EndSample();
            // UnityEngine.Profiling.Profiler.BeginSample("Instaced RenderQue");
            int RenderQueCount = RenderQue.Count - 1;
            for (int i = RenderQueCount; i >= 0; i--)
            {//Demotes from Render Que to Build Que in case mesh has changed
                if (RenderQue[i].NeedsToUpdate)
                {
                    RenderQue[i].ClearAll();
                    RenderQue[i].LoadData();
                    BuildQue.Add(RenderQue[i]);
                    RenderQue.RemoveAt(i);
                    var TempBuildQueCount = BuildQue.Count - 1;
                    CurrentlyActiveTasks.Add(Task.Run(() => BuildQue[TempBuildQueCount].BuildTotal()));
                    NeedsToUpdate = true;
                }
            }
            // UnityEngine.Profiling.Profiler.EndSample();
        }
    }
}