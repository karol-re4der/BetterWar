using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    //Final values
    [Header("Final Values")]
    public Vector3 distanceCameraShift = new Vector3();
    public Vector3 horizontalCameraShift = new Vector3();
    public Vector3 verticalCameraShift = new Vector3();
    public Vector3 angularCameraRotation = new Vector3();
    public Vector3 horizontalDragRotation = new Vector3();
    public Vector3 verticalDragRotation = new Vector3();

    //Debug values
    [Header("Runtime Values")]
    public float horizontalInput = 0;
    public float verticalInput = 0;
    public float angularInput = 0;
    public float distanceInput = 0;
    public Vector3 dragInput = new Vector3();
    public Vector3 dragPivot = new Vector3();
    public float deltaTime = 0;
    public bool dragActive = false;
    public float speedModifier = 0f;

    //Settings
    [Header("Settings")]
    public int sideScrollAreaSize = 10;
    public float bottomScrollAreaSize = 0.3f; //in percentage of total screen height
    public float cameraHeightMin = 1;
    public float cameraHeightMax = 10;
    public float cameraHeightSensitivityFalloff = 10; //speed boost at max height, linear
    public float distanceSensitivity = 20;
    public float directionalSensitivity = 25;
    public float angularSensitivity = 300;
    public float dragSensitivity = 300;
    public float scrollSmoothing = 10;
    public bool invertAngular = false;
    public bool invertDistance = false;
    public bool invertDrag = false;
    public float speedBoostStrength = 2;
    public bool mouseScrollEnabled = true;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        deltaTime = Time.deltaTime;

        //Set strength mod
        speedModifier = Mathf.Max(1f, ((transform.position.y - cameraHeightMin) / (cameraHeightMax - cameraHeightMin)) * cameraHeightSensitivityFalloff);


        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            speedModifier *= speedBoostStrength;
        }

        //Get inputs
        horizontalInput = Input.GetAxis("Horizontal") * directionalSensitivity * deltaTime * speedModifier;
        verticalInput = Input.GetAxis("Vertical") * directionalSensitivity * deltaTime * speedModifier;
        angularInput = (invertAngular ? -1 : 1) * Input.GetAxis("Angular") * angularSensitivity * deltaTime * speedModifier;

        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            distanceInput = (invertDistance ? -1 : 1) * Input.GetAxis("Mouse ScrollWheel") * distanceSensitivity * deltaTime * speedModifier;
        }
        else if (distanceInput > 0)
        {
            distanceInput -= scrollSmoothing * deltaTime * speedModifier;
            if (distanceInput < 0)
            {
                distanceInput = 0;
            }
        }
        else if (distanceInput < 0)
        {
            distanceInput += scrollSmoothing * deltaTime * speedModifier;
            if (distanceInput > 0)
            {
                distanceInput = 0;
            }
        }

        if (Input.GetMouseButton(2))
        {
            if (dragActive)
            {
                //Get input
                dragInput =  (invertDrag ? -1 : 1) * (dragPivot - Input.mousePosition) * deltaTime * dragSensitivity;
            }

            //save new pos
            dragPivot = Input.mousePosition;
            dragActive = true;
        }
        else
        {
            dragInput = Vector3.zero;
            dragActive = false;
        }

        //Add mouse on screen side input
        if (mouseScrollEnabled)
        {
            Vector2 mousePos = Input.mousePosition;
            if (mousePos.x < sideScrollAreaSize)
            {
                if (mousePos.y < Screen.height * bottomScrollAreaSize)
                {
                    horizontalInput -= directionalSensitivity * deltaTime * speedModifier /2;
                }
                else
                {
                    angularInput -= angularSensitivity * deltaTime * speedModifier / 2;
                }
            }
            else if (mousePos.x > Screen.width - sideScrollAreaSize)
            {
                if (mousePos.y < Screen.height * bottomScrollAreaSize)
                {
                    horizontalInput += directionalSensitivity * deltaTime * speedModifier / 2;

                }
                else
                {
                    angularInput += angularSensitivity * deltaTime * speedModifier / 2;
                }
            }

            if (mousePos.y < sideScrollAreaSize)
            {
                verticalInput -= directionalSensitivity * deltaTime * speedModifier;
            }
            else if (mousePos.y > Screen.height-sideScrollAreaSize)
            {
                verticalInput += directionalSensitivity * deltaTime * speedModifier;
            }
        }

        //Compute shifts
        horizontalCameraShift = new Vector3(horizontalInput, 0, 0);
        verticalCameraShift = new Vector3(transform.forward.x, 0, transform.forward.z) * verticalInput;
        angularCameraRotation = new Vector3(0, angularInput, 0);
        verticalDragRotation = new Vector3(-dragInput.y, 0, 0);
        horizontalDragRotation = new Vector3(0, dragInput.x, 0);
        distanceCameraShift = distanceInput * Vector3.forward;

        //Shift
        transform.Translate(horizontalCameraShift);
        transform.position += verticalCameraShift;
        transform.Rotate(angularCameraRotation,Space.World);
        transform.Rotate(verticalDragRotation, Space.Self);
        transform.Rotate(horizontalDragRotation, Space.World);
        transform.Translate(distanceCameraShift);

        //Enforce constraints
        if (transform.position.y < cameraHeightMin)
        {
            transform.position = new Vector3(transform.position.x, cameraHeightMin, transform.position.z);
            distanceInput = 0;
        }
        else if (transform.position.y > cameraHeightMax)
        {
            transform.position = new Vector3(transform.position.x, cameraHeightMax, transform.position.z);
            distanceInput = 0;
        }
    }
}
