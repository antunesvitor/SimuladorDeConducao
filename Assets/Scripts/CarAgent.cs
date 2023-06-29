using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class CarAgent : Agent
{
    private float horizontalInput, verticalInput;
    private float currentSteerAngle, currentbreakForce;
    private bool isBreaking;
    private Rigidbody rBody;
    private List<GameObject> parkableStreets;

    // Settings
    [SerializeField] private float motorForce, breakForce, maxSteerAngle;

    // Wheel Colliders
    [SerializeField] private WheelCollider frontLeftWheelCollider, frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider, rearRightWheelCollider;

    // Wheels
    [SerializeField] private Transform frontLeftWheelTransform, frontRightWheelTransform;
    [SerializeField] private Transform rearLeftWheelTransform, rearRightWheelTransform;

    [SerializeField] private Transform Target;

    public void Start()
    {
        this.rBody = GetComponent<Rigidbody>();
        this.parkableStreets = GameObject.FindGameObjectsWithTag("ParkableStreet").ToList();     //Lista de todos os objetos
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

    public override void OnEpisodeBegin()
    {
        SetNewRandomCarLocation();
        SetNewRandomDestination();
        //Talvez setar uma nova posicão para o carro 
    }  

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(this.transform.localPosition);
        sensor.AddObservation(Target.localPosition);

        //Dar um jeito de add observacao da velocidade do carro
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        verticalInput = actions.ContinuousActions[0];
        horizontalInput = actions.ContinuousActions[1];
        isBreaking = actions.DiscreteActions[0] == 1;

        UpdateCar();

        //Finalizar episódio aqui

        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);

        if (distanceToTarget < 2.5f)
        {
            SetReward(1.0f);
            EndEpisode();
        }
        //Colocar um else if com condições de tombar o carro ou bater, subir na calçada etc
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        var discreteActionsOut = actionsOut.DiscreteActions;

        verticalInput = Input.GetAxis("Vertical");
        horizontalInput = Input.GetAxis("Horizontal");
        isBreaking = Input.GetKey(KeyCode.Space);

        //Debug.Log("verticalInput: " + verticalInput);
        //Debug.Log("horizontalInput: " + horizontalInput);
        //Debug.Log("isBreaking: " + isBreaking);

        continuousActionsOut[0] = verticalInput;
        continuousActionsOut[1] = horizontalInput;
        discreteActionsOut[0] = isBreaking ? 1 : 0;

        UpdateCar();
    }

    private void UpdateCar()
    {
        //Debug.Log("Atualizando carro");
        HandleMotor();
        HandleSteering();
        UpdateWheels();
    }

    private void SetNewRandomDestination()
    {
        //Seleciona as ruas próximas ao veículo
        GameObject[] nearbyParkableStreets = this.parkableStreets.Where(x => Vector3.Distance(x.transform.localPosition, this.transform.localPosition) < 20).ToArray();

        int randomIndex = UnityEngine.Random.Range(0, nearbyParkableStreets.Length);

        Transform randomlySelectedStreet = nearbyParkableStreets[randomIndex].transform;

        Vector3 randomStreetVector = new Vector3(randomlySelectedStreet.localPosition.x,
                                                    1.5f,
                                                    randomlySelectedStreet.localPosition.z);
        Target.localPosition = randomStreetVector;
    }

    private void SetNewRandomCarLocation()
    {
        int randomIndex = UnityEngine.Random.Range(0, parkableStreets.Count);
        GameObject randomSeletedStreetObj = this.parkableStreets[randomIndex];
        Transform randomlySelectedStreet = randomSeletedStreetObj.transform;
        Vector3 randomStreetVector = new Vector3(randomlySelectedStreet.localPosition.x,
                                                    .5f,
                                                    randomlySelectedStreet.localPosition.z);

        this.transform.localPosition = randomStreetVector;
        this.transform.rotation = randomlySelectedStreet.rotation;
        verticalInput = 0;
        horizontalInput = 0;
        isBreaking = false;
    }

}
