using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerCharController : MonoBehaviour
{

    CharacterController characterController;
    playerInputManager inputManager;
    Camera fpsCamera;

    [Header("Movement")]
    public float speed = 5f;
    private float desiredSpeed = 0f;
    [System.NonSerialized] public float currentSpeed = 0f;
    [SerializeField] private float sprintModifier = 1.5f;
    [SerializeField] private float crouchModifier = 0.5f;
    [SerializeField] private float gravity = -39.2f;
    [SerializeField] private float acceleration = 0.5f;
    [SerializeField] private float deceleration = 0.1f;
    private Vector3 velocity = new Vector3(0f, 0f, 0f);


    [Header("Jumping")]
    [SerializeField] private float jumpHeight = 1.5f;
    private float timeSinceLastJump = 0f;
    private bool canJump = true;

    [Header("Ground Check")]
    private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.1f;
    [SerializeField] private LayerMask groundMask;
    private bool isGrounded = true;





    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        inputManager = GetComponent<playerInputManager>();
        fpsCamera = GetComponentInChildren<Camera>();

        groundCheck = gameObject.transform.Find("GroundCheck");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        //Reset Speed

        //Run GroundCheck
        GroundCheck();

        //Movement input (x,z) plane
        float x = inputManager.GetMoveInput().x;
        float z = inputManager.GetMoveInput().z;
        Vector3 move = transform.right * x * 0.75f + transform.forward * z;

        if (x != 0 || z != 0)
        {
            desiredSpeed = speed;
            currentSpeed += speed * acceleration;
        }
        else
        {
            desiredSpeed = 0f;
            currentSpeed -= speed * deceleration;
        }
        //Handle Sprint and Crouch
        if (inputManager.GetSprintInputHeld() && !inputManager.GetAimInputHeld())
        {
            desiredSpeed *= sprintModifier;
        }
        else if (inputManager.GetCrouchInputHeld() && isGrounded)
        {
            desiredSpeed *= crouchModifier;
        }

        currentSpeed = Mathf.Clamp(currentSpeed, 0, desiredSpeed);

        //Apply movement to character controller
        characterController.Move(move * currentSpeed * Time.fixedDeltaTime);


        timeSinceLastJump += Time.fixedDeltaTime;
        if (inputManager.GetJumpInputHeld() && isGrounded && canJump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        //Make sure you have to lift space - Avoid bunny hopping



        //Add Gravity (y) plane
        velocity.y += gravity * Time.fixedDeltaTime;
        characterController.Move(velocity * Time.fixedDeltaTime);
    }


    void GroundCheck()
    {
        //Check if hit ground
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        //Check if the button is released and the player is on the ground.
        if (!isGrounded)
        {
            canJump = false;
        }
        else if (isGrounded && !inputManager.GetJumpInputHeld())
        {
            canJump = true;
        }
        //If on ground, stop downward velocity
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -0f;
        }
    }
}
