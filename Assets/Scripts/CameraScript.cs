using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 30f;

    float rotationX = 0;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public Camera userCamera;

    void Update()
    {
        // Mouse-based movement
        HandleMouseMovement();

        // Keyboard-based movement
        HandleKeyboardMovement();
    }

    void HandleMouseMovement()
    {
        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        userCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
    }
    
    void HandleKeyboardMovement()
    {
        // Get input from the keyboard
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Calculate the movement direction
        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

        // Move the camera in the calculated direction
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime);

        // Optionally, you can limit the camera's movement to a specific height
        // transform.position = new Vector3(transform.position.x, Mathf.Clamp(transform.position.y, minY, maxY), transform.position.z);
    }
}
