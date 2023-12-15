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
        [HideInInspector] public int ExistsInQue;
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
                this.GetComponentInParent<AssetManager>().InstanceAddQue.Add(this);
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
                if(!this.GetComponentInParent<AssetManager>().InstanceRemoveQue.Contains(this)) this.GetComponentInParent<AssetManager>().InstanceRemoveQue.Add(this);
                this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
            }
        }
        public void OnParentClear() {

            this.GetComponentInParent<AssetManager>().InstanceUpdateQue.Add(this);
            this.ExistsInQue = 3;
            this.GetComponentInParent<AssetManager>().ParentCountHasChanged = true;
            this.GetComponentInParent<AssetManager>().InstanceAddQue.Add(this);
        }
    }
}