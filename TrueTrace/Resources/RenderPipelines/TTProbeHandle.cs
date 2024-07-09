using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TrueTrace {
    public class TTProbeHandle : MonoBehaviour
    {
        LightProbes LocalProbes;
        void Start()
        {
            LocalProbes = this.GetComponent<LightProbes>();
        }

    }
}