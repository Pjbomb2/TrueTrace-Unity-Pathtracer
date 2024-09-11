using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TranslationAnimation : MonoBehaviour
{
    public enum Options {X, Y, Z};
    public Options RotationAxis;
    public float Speed;
    public float Distance;
    private float TimeSinceStart = 0;
    private Vector3 InitialPosition;
    void Start() {
        InitialPosition = this.transform.position;
    }
    void Update()
    {
        this.transform.position = InitialPosition + 
        ((RotationAxis == Options.X ? Vector3.right : 
            (RotationAxis == Options.Y ? Vector3.up : Vector3.forward))
             * Distance * 4.0f * Mathf.Cos((TimeSinceStart += (Speed * Time.deltaTime))));
    }
}
