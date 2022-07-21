using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;
using System.Threading;

public class InstancedManager : MonoBehaviour
{
    
	public List<ParentObject> AddQue;
	public List<ParentObject> RemoveQue;
	public List<ParentObject> BuildQue;
	public List<ParentObject> RenderQue;
	public List<Task> CurrentlyActiveTasks;
	public bool ParentCountHasChanged;
	public bool NeedsToUpdateTextures;

	public void init() {
		AddQue = new List<ParentObject>();
		RemoveQue = new List<ParentObject>();
		BuildQue = new List<ParentObject>();
		RenderQue = new List<ParentObject>();
		CurrentlyActiveTasks = new List<Task>();
	}

    public void ClearAll() {//My attempt at clearing memory
        ParentObject[] ChildrenObjects = this.GetComponentsInChildren<ParentObject>();
        foreach(ParentObject obj in ChildrenObjects)
            obj.ClearAll();
	}

    public void EditorBuild() {
        init();
        AddQue = new List<ParentObject>();
        RemoveQue = new List<ParentObject>();
        RenderQue = new List<ParentObject>();
        CurrentlyActiveTasks = new List<Task>();
        BuildQue = new List<ParentObject>(GetComponentsInChildren<ParentObject>());
        for(int i = 0; i < BuildQue.Count; i++) {
            var CurrentRep = i;
            BuildQue[CurrentRep].LoadData();
            Task t1 = Task.Run(() => BuildQue[CurrentRep].BuildTotal());
            CurrentlyActiveTasks.Add(t1);
        }
    }

    public void BuildCombined() {
        init();
        List<ParentObject> TempQue = new List<ParentObject>(GetComponentsInChildren<ParentObject>());
        for(int i = 0; i < TempQue.Count; i++) {
            if(TempQue[i].HasCompleted && !TempQue[i].NeedsToUpdate)
                RenderQue.Add(TempQue[i]);
            else
                BuildQue.Add(TempQue[i]);
        }
        for(int i = 0; i < BuildQue.Count; i++) {
            var CurrentRep = i;
            BuildQue[CurrentRep].LoadData();
            CurrentlyActiveTasks.Add(Task.Run(() => BuildQue[CurrentRep].BuildTotal()));
        }
    }

    public void UpdateRenderAndBuildQues(ref bool NeedsToUpdate) {

        int AddQueCount = AddQue.Count - 1;
        int RemoveQueCount = RemoveQue.Count - 1;
        for(int i = AddQueCount; i >= 0; i--) {
            var CurrentRep = BuildQue.Count;
            BuildQue.Add(AddQue[i]);
            AddQue.RemoveAt(i);
            BuildQue[CurrentRep].LoadData();
            CurrentlyActiveTasks.Add(Task.Run(() => BuildQue[CurrentRep].BuildTotal()));
        }
        for(int i = RemoveQueCount; i >= 0; i--) {
            if(RenderQue.Contains(RemoveQue[i]))
                RenderQue.Remove(RemoveQue[i]);
            else if(BuildQue.Contains(RemoveQue[i])) {
                CurrentlyActiveTasks.RemoveAt(BuildQue.IndexOf(RemoveQue[i]));
                BuildQue.Remove(RemoveQue[i]);
            }
            NeedsToUpdate = true;
            RemoveQue.RemoveAt(i); 
        }
        int RenderQueCount = RenderQue.Count - 1;
        for(int i = RenderQueCount; i >= 0; i--) {//Demotes from Render Que to Build Que in case mesh has changed
            if(RenderQue[i].NeedsToUpdate) {
                RenderQue[i].ClearAll();
                RenderQue[i].LoadData();
                BuildQue.Add(RenderQue[i]);
                RenderQue.RemoveAt(i);
                var TempBuildQueCount = BuildQue.Count - 1;
                CurrentlyActiveTasks.Add(Task.Run(() => BuildQue[TempBuildQueCount].BuildTotal()));
                NeedsToUpdate = true;
            }
        }
        int BuildQueCount = BuildQue.Count - 1;
        for(int i = BuildQueCount; i >= 0; i--) {//Promotes from Build Que to Render Que
            if(CurrentlyActiveTasks[i].IsFaulted) Debug.Log(CurrentlyActiveTasks[i].Exception + ", " + BuildQue[i].Name);//Fuck, something fucked up
            if(CurrentlyActiveTasks[i].Status == TaskStatus.RanToCompletion) {
                BuildQue[i].SetUpBuffers();
                RenderQue.Add(BuildQue[i]);
                BuildQue.RemoveAt(i);
                CurrentlyActiveTasks.RemoveAt(i);
                NeedsToUpdate = true;
            }
        }
    }






}
