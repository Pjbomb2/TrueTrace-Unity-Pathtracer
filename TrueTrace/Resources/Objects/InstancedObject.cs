using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;
using System.Threading.Tasks;
using System.Threading;

namespace TrueTrace {
    [System.Serializable]
    public class InstancedObject : MonoBehaviour
    {
        private ParentObject PreviousInstance;
        public ParentObject InstanceParent;
        public int CompactedMeshData;
        public int ExistsInQue;
        public int QueInProgress;
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
            if (gameObject.scene.isLoaded)
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
                    AssetManager.Assets.InstanceAddQue.Add(this);
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
            if (gameObject.scene.isLoaded)
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
            AssetManager.Assets.InstanceUpdateQue.Add(this);
            this.ExistsInQue = 3;
            AssetManager.Assets.ParentCountHasChanged = true;
            AssetManager.Assets.InstanceAddQue.Add(this);
        }
    }
}