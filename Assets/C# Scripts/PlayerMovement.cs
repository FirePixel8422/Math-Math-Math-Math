using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour { 
    [Header("Movement")]
    public bool canMove;
    public float moveSpeed;
    public float sprintSpeed;
    public float groundDrag;
    public float jumpForce;

    [Header("Ground Check")]
    public bool onGround;

    [Header("Misc")]
    public Transform orientation;
    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;
    Rigidbody rb;

    public bool isSprinting;
    public float lastWPressTime;
    public float doubleTapTime;
    private void Start()
    {
        canMove = true;
        Time.timeScale = 1f;
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    

    private void Update()
    {
        if (canMove)
        {
            MovePlayer();
        }
        MyInput();
        SpeedControl();

        if (onGround)
        {
            rb.drag = groundDrag;
        }
        else
        {
            rb.drag = 0;
        }

        CheckSprinting();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetButtonDown("Jump") && onGround && canMove)
        {
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
        Vector3 targetVelocity = moveDirection.normalized * currentSpeed;
        targetVelocity.y = rb.velocity.y;

        if (horizontalInput == 0 && verticalInput == 0)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
        else
        {
            rb.velocity = targetVelocity;
        }
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (flatVel.magnitude > (isSprinting ? sprintSpeed : moveSpeed))
        {
            Vector3 limitedVel = flatVel.normalized * (isSprinting ? sprintSpeed : moveSpeed);
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }


    void OnCollisionEnter(Collision collision)
    {
        onGround = true;
    }
    private void CheckSprinting()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (Time.time - lastWPressTime < doubleTapTime)
            {
                isSprinting = true;
            }
            lastWPressTime = Time.time;
        }

        if (Input.GetKeyUp(KeyCode.W))
        {
            isSprinting = false;
        }
    }
}