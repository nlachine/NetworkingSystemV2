using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerLookV2 : MonoBehaviour
{
    playerInputManager inputManager;

    public static bool cursorLocked = true;
    private Transform fpsCam;
    [Header("Options")]
    [SerializeField] private float xSens = 1f;
    [SerializeField] private float ySens = 1f;
    [SerializeField] private float maxAngle = 89f;
    private float adjustY;
    private float adjustX;


    private Quaternion camCenter;
    void Start()
    {
        inputManager = GetComponent<playerInputManager>();
        fpsCam = GetComponentInChildren<Camera>().transform;
        camCenter = fpsCam.localRotation; //set rotation origin for cameras to camCenter
    }

    // Update is called once per frame
    void Update()
    {
        SetY();
        SetX();
        CursorLock();
    }

    void SetY()
    {
        
        if(inputManager.GetAimInputHeld())
            adjustY = ySens / 1.5f;
        else
            adjustY = ySens;
        
        float inputY = inputManager.GetLookInputsVertical() * adjustY * Time.deltaTime;

        Quaternion adj = Quaternion.AngleAxis(inputY, -Vector3.right);
        Quaternion delta = fpsCam.localRotation * adj;

        if(Quaternion.Angle(camCenter, delta) < maxAngle)
            fpsCam.localRotation = delta;
        else if(fpsCam.localRotation.eulerAngles.x < (360-maxAngle) && fpsCam.localRotation.eulerAngles.x > maxAngle)
            fpsCam.localRotation = Quaternion.Slerp(fpsCam.localRotation, Quaternion.Euler(maxAngle, 0, 0), Time.deltaTime * 4f);
    }

     void SetX()
    {
        if(inputManager.GetAimInputHeld())
            adjustX = xSens / 1.5f;
        else
            adjustX = xSens;
        float inputX = inputManager.GetLookInputsHorizontal() * adjustX * Time.deltaTime;


        Quaternion adj = Quaternion.AngleAxis(inputX, Vector3.up);
        Quaternion delta = transform.localRotation * adj;
        transform.localRotation = delta;
    }

    void CursorLock()
    {
        if(cursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if(Input.GetKeyDown(KeyCode.Escape))
                cursorLocked = false;               
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = true;

            if(Input.GetKeyDown(KeyCode.Escape))
                cursorLocked = true;                   
        }
    }
}
