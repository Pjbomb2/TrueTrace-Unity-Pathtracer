using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; 

namespace TrueTrace {
    public class FlyCamera : MonoBehaviour {
        bool isPaused = false;
        bool isPaused2 = false;
        public bool UseAltScheme = false;
        void OnApplicationFocus(bool hasFocus)
        {
            isPaused = !hasFocus;
        }

        void OnApplicationPause(bool pauseStatus)
        {
            isPaused = pauseStatus;
        }
        /*
        Writen by Windexglow 11-13-10.  Use it, edit it, steal it I don't care.  
        Converted to C# 27-02-13 - no credit wanted.
        Simple flycam I made, since I couldn't find any others made public.  
        Made simple to use (drag and drop, done) for regular keyboard layout  
        wasd : basic movement
        shift : Makes camera accelerate
        space : Moves camera on X and Z axis only.  So camera doesn't gain any height*/
         
         //I made a small change so that I can turn off movement by pressing t
        public float mainSpeed = 1.0f; //regular speed
        public float shiftAdd = 25.0f; //multiplied by how long shift is held.  Basically running
        public float maxShift = 1000.0f; //Maximum speed when holdin gshift
        public float camSens = 2.5f; //How sensitive it with mouse
        private Vector3 lastMouse = new Vector3(255, 255, 255); //kind of in the middle of the screen, rather than at the top (play)
        private float totalRun= 1.0f;
        private bool StopMovement = true;
        public void Start() {
            lastMouse = transform.eulerAngles;
        }
        void Update () {
            bool PressedT = UseAltScheme || Mouse.current.rightButton.wasPressedThisFrame;
            StopMovement = !Mouse.current.rightButton.isPressed;

            if(Mouse.current.rightButton.isPressed) {
                if(!isPaused2) {
                    Vector2 delta = Mouse.current.delta.ReadValue();
                    lastMouse = new Vector3(-delta.y * camSens * 0.1f, delta.x * camSens * 0.1f, 0 );
                } else lastMouse = Vector3.zero;
                lastMouse = new Vector3(transform.eulerAngles.x + lastMouse.x , transform.eulerAngles.y + lastMouse.y, 0);
                if(lastMouse.x < 280) {
                    if(lastMouse.x < 95) {
                        lastMouse.x = Mathf.Min(lastMouse.x, 88);
                    } else {
                        lastMouse.x = Mathf.Max(lastMouse.x, 273);
                    }
                }
            }
            if(UseAltScheme || !StopMovement) {
                    transform.eulerAngles = lastMouse;
                Vector3 p = GetBaseInput();
                if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed){
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
                if (Keyboard.current.spaceKey.isPressed){ //If player wants to move on X and Z axis only
                    transform.Translate(p);
                    newPosition.x = transform.position.x;
                    newPosition.z = transform.position.z;
                    transform.position = newPosition;
                }
                else{
                    transform.Translate(p);
                }
            }
            isPaused2 = isPaused;
        }
         
        private Vector3 GetBaseInput() { //returns the basic values, if it's 0 than it's not active.
            var keyboard = Keyboard.current;
            Vector3 p_Velocity = Vector3.zero;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                p_Velocity += new Vector3(0, 0, 1);
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                p_Velocity += new Vector3(0, 0, -1);
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                p_Velocity += new Vector3(-1, 0, 0);
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                p_Velocity += new Vector3(1, 0, 0);
            if (keyboard.pageUpKey.isPressed || keyboard.eKey.isPressed)
                p_Velocity += new Vector3(0, 1, 0);
            if (keyboard.pageDownKey.isPressed || keyboard.qKey.isPressed)
                p_Velocity += new Vector3(0, -1, 0);
            return p_Velocity;
        }
    }
}