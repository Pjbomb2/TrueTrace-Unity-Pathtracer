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
        [HideInInspector] public int CompactedMeshData;
        [HideInInspector] public int ExistsInQueue;

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
                if(InstanceParent == null) {
                    Destroy(this);
                    return;
                }
                ExistsInQueue = 3;
                this.transform.hasChanged = true;
                this.GetComponentInParent<AssetManager>().InstanceAddQueue.Add(this);
                this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
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
                if(!this.GetComponentInParent<AssetManager>().InstanceRemoveQueue.Contains(this)) this.GetComponentInParent<AssetManager>().InstanceRemoveQueue.Add(this);
                this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
            }
        }
        public void OnParentClear() {

            this.GetComponentInParent<AssetManager>().InstanceUpdateQueue.Add(this);
            this.ExistsInQueue = 3;
            this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
            this.GetComponentInParent<AssetManager>().InstanceAddQueue.Add(this);
        }
    }
}