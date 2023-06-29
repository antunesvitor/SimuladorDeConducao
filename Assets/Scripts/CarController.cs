using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    private float horizontalInput, verticalInput;
    private float currentSteerAngle, currentbreakForce;
    private bool isBreaking;
    private GameObject[] parkableStreets;

    // Settings
    [SerializeField] private float motorForce, breakForce, maxSteerAngle;

    // Wheel Colliders
    [SerializeField] private WheelCollider frontLeftWheelCollider, frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider, rearRightWheelCollider;

    // Wheels
    [SerializeField] private Transform frontLeftWheelTransform, frontRightWheelTransform;
    [SerializeField] private Transform rearLeftWheelTransform, rearRightWheelTransform;

    [SerializeField] private Transform Target;

    private void Start()
    {
        this.parkableStreets = GameObject.FindGameObjectsWithTag("ParkableStreet");     //Lista de todos os objetos 
        int randomIndex = UnityEngine.Random.Range(0, parkableStreets.Length);

        Debug.Log("Random array index is: " + randomIndex);
        Transform randomlySelectedStreet = this.parkableStreets[randomIndex].transform;

        Vector3 randomStreetVector = new Vector3(randomlySelectedStreet.localPosition.x,
                                                    1.5f,
                                                    randomlySelectedStreet.localPosition.z);
        Target.localPosition = randomStreetVector;
    }

    private void FixedUpdate()
    {
        GetInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();

        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);

        //Debug.Log("Distancia ao alvo = " + distanceToTarget);
    }


    //Isso deverá ser transformado em Heuristic
    private void GetInput()
    {
        // Steering Input
        horizontalInput = Input.GetAxis("Horizontal");
        //Debug.Log("HI: " + horizontalInput.ToString());

        // Acceleration Input
        verticalInput = Input.GetAxis("Vertical");
        //Debug.Log("VI: " + verticalInput.ToString());

        // Breaking Input
        isBreaking = Input.GetKey(KeyCode.Space);
    }

    private void HandleMotor()
    {
        frontLeftWheelCollider.motorTorque = verticalInput * motorForce;
        frontRightWheelCollider.motorTorque = verticalInput * motorForce;
        currentbreakForce = isBreaking ? breakForce : 0f;
        ApplyBreaking();
    }

    private void ApplyBreaking()
    {
        frontRightWheelCollider.brakeTorque = currentbreakForce;
        frontLeftWheelCollider.brakeTorque = currentbreakForce;
        rearLeftWheelCollider.brakeTorque = currentbreakForce;
        rearRightWheelCollider.brakeTorque = currentbreakForce;
    }

    private void HandleSteering()
    {
        currentSteerAngle = maxSteerAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;
    }

    private void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }
}