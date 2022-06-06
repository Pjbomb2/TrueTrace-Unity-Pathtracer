using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CommonFunctions {

    public static class Extension {
    public static Vector3 mod(this Vector3 A, Vector3 B) {
        return new Vector3((A.x % B.x),(A.y % B.y),(A.z % B.z));
    }
    public static Vector3 mod(this Vector3 A, float B) {
        return new Vector3((A.x % B),(A.y % B),(A.z % B));
    }

    public static Vector3 abs(this Vector3 A) {
        return new Vector3(Mathf.Abs(A.x),Mathf.Abs(A.y),Mathf.Abs(A.z));
    }
    public static Vector3 inverse(this Vector3 A) {
        return new Vector3(1.0f / A.x,1.0f / A.y,1.0f / A.z);
    }
    public static Vector3 Floor(this Vector3 A) {
        return new Vector3(Mathf.Floor(A.x),Mathf.Floor(A.y),Mathf.Floor(A.z));
    }
    public static Vector3 Ceil(this Vector3 A) {
        return new Vector3(Mathf.Ceil(A.x),Mathf.Ceil(A.y),Mathf.Ceil(A.z));
    }
    public static Vector3 Pow(this Vector3 A, float B) {
        return new Vector3(Mathf.Pow(A.x, B),Mathf.Pow(A.y, B),Mathf.Pow(A.z, B));
    }
    public static Vector3 multi(this Vector3 A, Vector3 B) {
        return new Vector3(A.x*B.x,A.y*B.y,A.z*B.z);
    }
    public static Vector3 Div(this Vector3 A, Vector3 B) {
        return new Vector3(A.x/B.x,A.y/B.y,A.z/B.z);
    }
    public static Vector3 Div(this float A, Vector3 B) {
        return new Vector3(A/B.x,A/B.y,A/B.z);
    }

    }

}
