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
        float mainSpeed = 1.0f; //regular speed
        float shiftAdd = 25.0f; //multiplied by how long shift is held.  Basically running
        float maxShift = 1000.0f; //Maximum speed when holdin gshift
        public float camSens = 2.5f; //How sensitive it with mouse
        private Vector3 lastMouse = new Vector3(255, 255, 255); //kind of in the middle of the screen, rather than at the top (play)
        private float totalRun= 1.0f;
        private bool StopMovement = true;
        private bool IsPressingT = false;
        void Update () {
            bool PressedT = Input.GetMouseButtonDown(1);
            // if(PressedT && !IsPressingT) {
            //     if(!StopMovement) {
            //         // Cursor.lockState = CursorLockMode.None;
            //         StopMovement = true;
            //     } else  {
            //         // Cursor.lockState = CursorLockMode.Locked;
            //         StopMovement = false;   
            //     }
            // }
            // if(Input.GetMouseButtonDown(1)) {
            //     // Cursor.lockState = CursorLockMode.Locked;
            //     StopMovement = false;
            // } else {
            //     StopMovement = true;

            // }
            // if(PressedT) {
            //     IsPressingT = true;
            // } else {IsPressingT = false;}
            StopMovement = !Input.GetMouseButton(1);



            lastMouse = new Vector3(-Input.GetAxisRaw("Mouse Y") * camSens, Input.GetAxisRaw("Mouse X") * camSens, 0 );
            lastMouse = new Vector3(transform.eulerAngles.x + lastMouse.x , transform.eulerAngles.y + lastMouse.y, 0);
            if(lastMouse.x < 280) {
                if(lastMouse.x < 95) {
                    lastMouse.x = Mathf.Min(lastMouse.x, 88);
                } else {
                    lastMouse.x = Mathf.Max(lastMouse.x, 273);
                }
            }
            if(!StopMovement) {
                    transform.eulerAngles = lastMouse;
               
                Vector3 p = GetBaseInput();
                if (Input.GetKey (KeyCode.LeftShift)){
                    totalRun += Time.deltaTime;
                    p  = p * totalRun * shiftAdd;
                    p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
                    p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
                    p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
                }
                else{
                    totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
                    p = p * mainSpeed;
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
            Vector3 p_Velocity = new Vector3();
            if (Input.GetKey (KeyCode.W)){
                p_Velocity += new Vector3(0, 0 , 1);
            }
            if (Input.GetKey (KeyCode.S)){
                p_Velocity += new Vector3(0, 0, -1);
            }
            if (Input.GetKey (KeyCode.A)){
                p_Velocity += new Vector3(-1, 0, 0);
            }
            if (Input.GetKey (KeyCode.D)){
                p_Velocity += new Vector3(1, 0, 0);
            }
            return p_Velocity;
        }
    }
}