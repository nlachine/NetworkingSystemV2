using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerInputManager : MonoBehaviour
{
    [Header("Mouse Options")]
    [Range(1.0f, 50.0f)] public float mouseSensitivity = 5.0f;
    public bool InvertYAxis = false;

    bool v_FireInputWasHeld;
    bool v_GrenadeInputWasHeld;


    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    //Called after everything in the update. Once per frame
    private void LateUpdate()
    {
        v_FireInputWasHeld = GetFireInputHeld();
    }

    public bool CanProcessInput()
    {
        return Cursor.lockState == CursorLockMode.Locked;
    }

    #region MOVEMENT
    //Get the input of movement (Horizontal/Vertical)
    public Vector3 GetMoveInput()
    {
        if (CanProcessInput())
        {
            Vector3 move = new Vector3(Input.GetAxisRaw(inputConstants.c_AxisNameHorizontal), 0f, Input.GetAxisRaw(inputConstants.c_AxisNameVertical));

            // constrain move input to a maximum magnitude of 1, otherwise diagonal movement might exceed the max move speed defined
            move = Vector3.ClampMagnitude(move, 1);

            return move;
        }

        return Vector3.zero;
    }
    #endregion

    #region LOOK

    //Look - Mouse X
    public float GetLookInputsHorizontal()
    {
        return GetMouseOrStickLookAxis(inputConstants.c_MouseAxisNameHorizontal);
    }

    //Look - Mouse Y
    public float GetLookInputsVertical()
    {
        return GetMouseOrStickLookAxis(inputConstants.c_MouseAxisNameVertical);
    }

    //Adjust Look for sensitivity
    float GetMouseOrStickLookAxis(string mouseInputName)
    {
        if (CanProcessInput())
        {
            // Check if this look input is coming from the mouse
            float i = Input.GetAxisRaw(mouseInputName);

            // handle inverting vertical input
            if (InvertYAxis)
                i *= -1f;

            // apply sensitivity multiplier
            i *= mouseSensitivity * 10f;

            return i;
        }
        return 0f;
    }

    public float GetMouseSensitivity()
    {
        return mouseSensitivity;
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }

    public bool GetInvertYAxis()
    {
        return InvertYAxis;
    }
    #endregion

    #region JUMP
    //Space pressed down
    public bool GetJumpInputDown()
    {
        if (CanProcessInput())
        {
            return Input.GetButtonDown(inputConstants.c_ButtonNameJump);
        }
        return false;
    }

    public bool GetJumpInputUp()
    {
        if (CanProcessInput())
        {
            return Input.GetButtonUp(inputConstants.c_ButtonNameJump);
        }
        return false;
    }

    public bool GetJumpInputHeld()
    {
        if (CanProcessInput())
        {
            return Input.GetButton(inputConstants.c_ButtonNameJump);
        }
        return false;
    }
    #endregion

    #region FIRE
    //Left Click
    public bool GetFireInputDown()
    {
        return GetFireInputHeld() && !v_FireInputWasHeld;
    }

    //Left Click Released
    public bool GetFireInputReleased()
    {
        return !GetFireInputHeld() && v_FireInputWasHeld;
    }

    //Left Click Held
    public bool GetFireInputHeld()
    {
        if (CanProcessInput())
        {
            return Input.GetButton(inputConstants.c_ButtonNameFire);
        }
        return false;
    }
    #endregion

    #region AIM
    //Right Click Held
    public bool GetAimInputHeld()
    {
        if (CanProcessInput())
        {
            bool i = Input.GetButton(inputConstants.c_ButtonNameAim);
            return i;
        }
        return false;
    }
    #endregion

    #region SPRINT
    //Shift being held
    public bool GetSprintInputHeld()
    {
        if (CanProcessInput())
        {
            return Input.GetButton(inputConstants.c_ButtonNameSprint);
        }
        return false;
    }

    public bool GetSprintInputDown()
    {
        if (CanProcessInput())
        {
            return Input.GetButtonDown(inputConstants.c_ButtonNameSprint);
        }
        return false;
    }

    public bool GetSprintInputUp()
    {
        if (CanProcessInput())
        {
            return Input.GetButtonUp(inputConstants.c_ButtonNameSprint);
        }
        return false;
    }
    #endregion

    #region CROUCH
    //Left Ctrl down
    public bool GetCrouchInputDown()
    {
        if (CanProcessInput())
        {
            return Input.GetButtonDown(inputConstants.c_ButtonNameCrouch);
        }
        return false;
    }

    //Left Ctrl up
    public bool GetCrouchInputReleased()
    {
        if (CanProcessInput())
        {
            return Input.GetButtonUp(inputConstants.c_ButtonNameCrouch);
        }
        return false;
    }

    //Left Ctrl Held
    public bool GetCrouchInputHeld()
    {
        if (CanProcessInput())
        {
            return Input.GetButton(inputConstants.c_ButtonNameCrouch);
        }
        return false;
    }
    #endregion

    #region GRENADE
    //Key 'G' down
    public bool GetGrenadeInputDown()
    {
        if (CanProcessInput())
        {
            return Input.GetButtonDown(inputConstants.c_ButtonNameGrenade);
        }
        return false;
    }

    //Key 'G' up
    public bool GetGrenadeInputReleased()
    {
        if (CanProcessInput())
        {
            return Input.GetButtonUp(inputConstants.c_ButtonNameGrenade);
        }
        return false;
    }

    //Key 'G' Held
    public bool GetGrenadeInputHeld()
    {
        if (CanProcessInput())
        {
            return Input.GetButton(inputConstants.c_ButtonNameGrenade);
        }
        return false;
    }
    #endregion

    #region RELOAD
    //Key 'R' down
    public bool GetReloadInputDown()
    {
        if (CanProcessInput())
        {
            return Input.GetButtonDown(inputConstants.c_ButtonNameReload);
        }
        return false;
    }
    #endregion

    #region INTERACT
    //Key 'E' down
    public bool GetInteractInputDown()
    {
        if (CanProcessInput())
        {
            return Input.GetButtonDown(inputConstants.c_ButtonNameInteract);
        }
        return false;
    }
    #endregion

    #region SWITCH WEAPON
    //Check Scroll Wheel value
    public int GetSwitchWeaponInput()
    {
        if (CanProcessInput())
        {
            string axisName = inputConstants.c_ButtonNameSwitchWeapon;

            if (Input.GetAxis(axisName) > 0f) //Forward
                return -1;
            else if (Input.GetAxis(axisName) < 0f) // Backwards
                return 1;
        }

        return 0;
    }

    //Weapon Index
    public int GetSelectWeaponInput()
    {
        if (CanProcessInput())
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                return 1;
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                return 2;
            else if (Input.GetKeyDown(KeyCode.Alpha3))
                return 3;
            else if (Input.GetKeyDown(KeyCode.Alpha4))
                return 4;
            else if (Input.GetKeyDown(KeyCode.Alpha5))
                return 5;
            else if (Input.GetKeyDown(KeyCode.Alpha6))
                return 6;
            else
                return 0;
        }
        return 0;
    }
    #endregion

}
