using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommonVars;

namespace TrueTrace {
    public class TTInterface
    {
        public ParentObject InitializeObject(GameObject TargetObj) {
            //add raytracingobject and parentobject to targetobj
            return null;
        }

        public ParentObject InitializeGroupedObject(GameObject TargetObj) {
            //go through children and add raytracingobject to them, while adding parentobject to targetobj
            return null;
        }
    }
}
