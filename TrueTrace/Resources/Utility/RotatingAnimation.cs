using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingAnimation : MonoBehaviour
{
    public enum Options {X, Y, Z};
    public Options RotationAxis = Options.Y;
    public float Speed;
    public bool TransformAware;
    void Update()
    {
        if(TransformAware) this.transform.Rotate((RotationAxis == Options.Z ? Vector3.forward : (RotationAxis == Options.Y ? Vector3.up : Vector3.right)) * (Speed * Time.deltaTime));
        else this.transform.Rotate((RotationAxis == Options.Z ? new Vector3(0,0,1) : (RotationAxis == Options.Y ? new Vector3(0,1,0) : new Vector3(1,0,0))) * (Speed * Time.deltaTime), Space.World);
    }
}
