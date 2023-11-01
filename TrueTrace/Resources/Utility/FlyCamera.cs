using UnityEngine;
using System.Collections;
     
namespace TrueTrace {
    public class FlyCamera : MonoBehaviour {
     
        /*
        Writen by Windexglow 11-13-10.  Use it, edit it, steal it I don't care.  
        Converted to C# 27-02-13 - no credit wanted.
        Simple flycam I made, since I couldn't find any others made public.  
        Made simple to use (drag and drop, done) for regular keyboard layout  
        wasd : basic movement
        shift : Makes camera accelerate
        space : Moves camera on X and Z axis only.  So camera doesn't gain any height*/
         
         //I made a small change so that I can turn off movement by pressing t
        float MainSpeed = 1.0f; //regular speed
        float ShiftAdd = 25.0f; //multiplied by how long shift is held.  Basically running
        float MaxSpeedPressingShift = 1000.0f; //Maximum speed when holding shift
        public float CameraSensitivity = 2.5f; //How sensitive it with mouse
        private Vector3 LastMousePosition = new Vector3(255, 255, 255); //kind of in the middle of the screen, rather than at the top (play)
        private float TotalRun = 1.0f;
        private bool StopMovement = true;
        private bool IsPressingT = false;
        void Update () {
            bool PressedT = Input.GetKey(KeyCode.T);
            if(PressedT && !IsPressingT) {
                if(!StopMovement) {
                    Cursor.lockState = CursorLockMode.None;
                    StopMovement = true;
                } else  {
                    Cursor.lockState = CursorLockMode.Locked;
                    StopMovement = false;   
                }
            }
            if(Input.GetMouseButtonDown(0)) {
                Cursor.lockState = CursorLockMode.Locked;
                StopMovement = false;
            }
            if(PressedT) {
                IsPressingT = true;
            } else {IsPressingT = false;}



            LastMousePosition = new Vector3(-Input.GetAxisRaw("Mouse Y") * CameraSensitivity, Input.GetAxisRaw("Mouse X") * CameraSensitivity, 0 );
            LastMousePosition = new Vector3(transform.eulerAngles.x + LastMousePosition.x , transform.eulerAngles.y + LastMousePosition.y, 0);
            if(LastMousePosition.x < 280) {
                if(LastMousePosition.x < 95) {
                    LastMousePosition.x = Mathf.Min(LastMousePosition.x, 88);
                } else {
                    LastMousePosition.x = Mathf.Max(LastMousePosition.x, 273);
                }
            }
            if(!StopMovement) {
                    transform.eulerAngles = LastMousePosition;
               
                Vector3 p = GetBaseInput();
                if (Input.GetKey (KeyCode.LeftShift)){
                    TotalRun += Time.deltaTime;
                    p  = p * TotalRun * ShiftAdd;
                    p.x = Mathf.Clamp(p.x, -MaxSpeedPressingShift, MaxSpeedPressingShift);
                    p.y = Mathf.Clamp(p.y, -MaxSpeedPressingShift, MaxSpeedPressingShift);
                    p.z = Mathf.Clamp(p.z, -MaxSpeedPressingShift, MaxSpeedPressingShift);
                }
                else{
                    TotalRun = Mathf.Clamp(TotalRun * 0.5f, 1f, 1000f);
                    p = p * MainSpeed;
                }
           
                p = p * Time.deltaTime;
               Vector3 newPosition = transform.position;
                if (Input.GetKey(KeyCode.Space)){ //If player wants to move on X and Z axis only
                    transform.Translate(p);
                    newPosition.x = transform.position.x;
                    newPosition.z = transform.position.z;
                    transform.position = newPosition;
                }
                else{
                    transform.Translate(p);
                }
            }
        }
         
        private Vector3 GetBaseInput() { //returns the basic values, if it's 0 than it's not active.
            Vector3 Velocity = new Vector3();
            if (Input.GetKey (KeyCode.W)){
                Velocity += new Vector3(0, 0 , 1);
            }
            if (Input.GetKey (KeyCode.S)){
                Velocity += new Vector3(0, 0, -1);
            }
            if (Input.GetKey (KeyCode.A)){
                Velocity += new Vector3(-1, 0, 0);
            }
            if (Input.GetKey (KeyCode.D)){
                Velocity += new Vector3(1, 0, 0);
            }
            return Velocity;
        }
    }
}