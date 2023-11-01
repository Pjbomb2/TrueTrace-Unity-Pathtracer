using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;
using System.Threading;
#pragma warning disable 4014

namespace TrueTrace {
    [System.Serializable]
    public class InstancedManager : MonoBehaviour
    {
        [HideInInspector] public List<ParentObject> AddQueue;
        [HideInInspector] public List<ParentObject> RemoveQueue;
        [HideInInspector] public List<ParentObject> BuildQueue;
        [HideInInspector] public List<ParentObject> RenderQueue;
        [HideInInspector] public List<Task> CurrentlyActiveTasks;
        [HideInInspector] public bool ParentCountHasChanged;
        [HideInInspector] public bool NeedsToUpdateTextures;

        public void init()
        {
            AddQueue = new List<ParentObject>();
            RemoveQueue = new List<ParentObject>();
            BuildQueue = new List<ParentObject>();
            RenderQueue = new List<ParentObject>();
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
            AddQueue = new List<ParentObject>();
            RemoveQueue = new List<ParentObject>();
            RenderQueue = new List<ParentObject>();
            CurrentlyActiveTasks = new List<Task>();
            BuildQueue = new List<ParentObject>(GetComponentsInChildren<ParentObject>());
            RunningTasks = 0;
            for (int i = 0; i < BuildQueue.Count; i++)
            {
                var CurrentRep = i;
                BuildQueue[CurrentRep].LoadData();
                Task t1 = Task.Run(() => { BuildQueue[CurrentRep].BuildTotal(); RunningTasks--; });
                RunningTasks++;
                CurrentlyActiveTasks.Add(t1);
            }
        }

        public void BuildCombined()
        {
            init();
            List<ParentObject> TempQueue = new List<ParentObject>(GetComponentsInChildren<ParentObject>());
            for (int i = 0; i < TempQueue.Count; i++)
            {
                if (TempQueue[i].HasCompleted && !TempQueue[i].NeedsToUpdate) RenderQueue.Add(TempQueue[i]);
                else BuildQueue.Add(TempQueue[i]);
            }
            for (int i = 0; i < BuildQueue.Count; i++)
            {
                var CurrentRep = i;
                BuildQueue[CurrentRep].LoadData();
                CurrentlyActiveTasks.Add(Task.Run(() => BuildQueue[CurrentRep].BuildTotal()));
            }
        }

        public void UpdateRenderAndBuildQueues(ref bool NeedsToUpdate)
        {

            // UnityEngine.Profiling.Profiler.BeginSample("Instanced AddQueue");
            int AddQueueCount = AddQueue.Count - 1;
            for (int i = AddQueueCount; i >= 0; i--)
            {
                var CurrentRep = BuildQueue.Count;
                BuildQueue.Add(AddQueue[i]);
                AddQueue.RemoveAt(i);
                BuildQueue[CurrentRep].LoadData();
                CurrentlyActiveTasks.Add(Task.Run(() => BuildQueue[CurrentRep].BuildTotal()));
            }
            // UnityEngine.Profiling.Profiler.EndSample();
            // UnityEngine.Profiling.Profiler.BeginSample("Instanced RemoveQueue");
            int RemoveQueueCount = RemoveQueue.Count - 1;
            for (int i = RemoveQueueCount; i >= 0; i--)
            {
                if (RenderQueue.Contains(RemoveQueue[i]))
                    RenderQueue.Remove(RemoveQueue[i]);
                else if (BuildQueue.Contains(RemoveQueue[i]))
                {
                    CurrentlyActiveTasks.RemoveAt(BuildQueue.IndexOf(RemoveQueue[i]));
                    BuildQueue.Remove(RemoveQueue[i]);
                }
                NeedsToUpdate = true;
                RemoveQueue.RemoveAt(i);
            }
            // UnityEngine.Profiling.Profiler.EndSample();
            // UnityEngine.Profiling.Profiler.BeginSample("Instanced RenderQueue");
            int RenderQueueCount = RenderQueue.Count - 1;
            for (int i = RenderQueueCount; i >= 0; i--)
            {//Demotes from Render Que to Build Que in case mesh has changed
                if (RenderQueue[i].NeedsToUpdate)
                {
                    RenderQueue[i].ClearAll();
                    RenderQueue[i].LoadData();
                    BuildQueue.Add(RenderQueue[i]);
                    RenderQueue.RemoveAt(i);
                    var TempBuildQueueCount = BuildQueue.Count - 1;
                    CurrentlyActiveTasks.Add(Task.Run(() => BuildQueue[TempBuildQueueCount].BuildTotal()));
                    NeedsToUpdate = true;
                }
            }
            // UnityEngine.Profiling.Profiler.EndSample();
            // UnityEngine.Profiling.Profiler.BeginSample("Instanced BuildQueue");
            int BuildQueueCount = BuildQueue.Count - 1;
            for (int i = BuildQueueCount; i >= 0; i--)
            {//Promotes from Build Que to Render Que
                if (CurrentlyActiveTasks[i].IsFaulted) {
                    Debug.Log(CurrentlyActiveTasks[i].Exception + ", " + BuildQueue[i].Name);//Fuck, something fucked up
                    AddQueue.Add(BuildQueue[i]);
                    BuildQueue.RemoveAt(i);
                    CurrentlyActiveTasks.RemoveAt(i);
                } else if (CurrentlyActiveTasks[i].Status == TaskStatus.RanToCompletion) {
                    if (BuildQueue[i].AggTriangles == null || BuildQueue[i].AggNodes == null) {
                        AddQueue.Add(BuildQueue[i]);
                        BuildQueue.RemoveAt(i);
                        CurrentlyActiveTasks.RemoveAt(i);
                    } else {
                        BuildQueue[i].SetUpBuffers();
                        RenderQueue.Add(BuildQueue[i]);
                        BuildQueue.RemoveAt(i);
                        CurrentlyActiveTasks.RemoveAt(i);
                        NeedsToUpdate = true;
                    }
                }
            }
            // UnityEngine.Profiling.Profiler.EndSample();
        }
    }
}