using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;
using System.Threading;

namespace TrueTrace {
    [System.Serializable][ExecuteInEditMode]
    public class InstancedObject : MonoBehaviour
    {
        private ParentObject PreviousInstance;
        public ParentObject InstanceParent;
        public int CompactedMeshData;
        public int ExistsInQue;
        public int QueInProgress;
        public int LightIndex = -1;
        bool PrevInstance = false;
        public void UpdateInstance()
        {
            if (PreviousInstance != null)
            {
                if (!PreviousInstance.Equals(InstanceParent))
                {
                    PreviousInstance.InstanceReferences--;
                    InstanceParent.InstanceReferences++;
                    PreviousInstance = InstanceParent;
                }
            }
            else
            {
                InstanceParent.InstanceReferences++;
                PreviousInstance = InstanceParent;
            }
        }
        private void OnEnable()
        {
            if(GetComponentInParent<InstancedManager>() != null) {
                DestroyImmediate(this);
                return;
            }
            InstancedManager.NeedsToReinit = true;
            // GameObject.Find("InstancedStorage").GetComponent<InstancedManager>().InitRelationships();
            if (gameObject.scene.isLoaded && Application.isPlaying)
            {
                if(PrevInstance && InstanceParent == null) {
                    Destroy(this);
                    return;
                }
                ExistsInQue = 3;
                this.transform.hasChanged = true;
                if(!AssetManager.Assets.InstanceRemoveQue.Contains(this)) {
                    if(QueInProgress == 2) {
                        AssetManager.Assets.InstanceUpdateQue.Remove(this);
                    }
                    if(!AssetManager.Assets.InstanceAddQue.Contains(this)) AssetManager.Assets.InstanceAddQue.Add(this);
                    QueInProgress = 3;
                    ExistsInQue = 3;
                }
                AssetManager.Assets.ParentCountHasChanged = true;
                // this.GetComponentInParent<AssetManager>().InstanceAddQue.Add(this);
                // this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
            }
        }

        private void OnDisable()
        {
            InstancedManager.NeedsToReinit = true;
            // GameObject.Find("InstancedStorage").GetComponent<InstancedManager>().InitRelationships();
            if (gameObject.scene.isLoaded && Application.isPlaying)
            {
                if(InstanceParent == null) {
                    Destroy(this);
                    return;
                }
                if(!AssetManager.Assets.InstanceRemoveQue.Contains(this)) {
                    QueInProgress = -1;
                    AssetManager.Assets.InstanceRemoveQue.Add(this);
                }
                AssetManager.Assets.ParentCountHasChanged = true;
            }
        }
        public void OnParentClear() {
            this.ExistsInQue = 3;
            if(AssetManager.Assets != null && AssetManager.Assets.InstanceUpdateQue != null) {
                AssetManager.Assets.InstanceUpdateQue.Add(this);
                AssetManager.Assets.ParentCountHasChanged = true;
                if(!AssetManager.Assets.InstanceAddQue.Contains(this)) AssetManager.Assets.InstanceAddQue.Add(this);
            }
        }
    }
}