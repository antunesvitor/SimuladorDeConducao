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
    //Controle do veículo
    private float horizontalInput, verticalInput;
    private float currentSteerAngle, currentbreakForce;
    private bool isBreaking;
    private Rigidbody rBody;

    private List<GameObject> parkableStreets;

    private float carDistanceToDestination;             //distância entre o carro e o destino no início do episódio
    private float currentCarDistanceToDestination;      //distância entre o carro e o destino a cada frame

    private float timeLimit = 20;                       //Tempo limite em segundos para atingir o objetivo
    private float elapsedTime = 0;                      //Tempo decorrido em segundos

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

        //atualiza as novas variáveis de distância entre o agente e o destino
        this.carDistanceToDestination = Vector3.Distance(this.transform.localPosition, Target.transform.position);
        this.currentCarDistanceToDestination = this.carDistanceToDestination;

        this.elapsedTime = 0;
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

        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.position);
        float travelledDistLastFrame = distanceToTarget * Time.deltaTime;
        this.currentCarDistanceToDestination -= travelledDistLastFrame;
        this.elapsedTime += Time.deltaTime;

        SetReward(travelledDistLastFrame);

        SetReward(-Time.deltaTime);

        if (CarIsUpsideDown())
        {
            SetReward(-1.0f);
            EndEpisode();
        }

        if(this.elapsedTime > this.timeLimit)
        {
            SetReward(-1.0f);
            EndEpisode();
        }

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
        //Debug.Log($"Car velocity x: {this.rBody.velocity.x} | z: {this.rBody.velocity.z}" );
        //Debug.Log("carDistanceToDestination: " + carDistanceToDestination);
        //Debug.Log("current distance: " + Vector3.Distance(this.transform.localPosition, Target.localPosition));

        continuousActionsOut[0] = verticalInput;
        continuousActionsOut[1] = horizontalInput;
        discreteActionsOut[0] = isBreaking ? 1 : 0;

        UpdateCar();

        if (CarIsUpsideDown())
            EndEpisode();
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
        GameObject[] nearbyParkableStreets = this.parkableStreets
                                              .Where(x => Vector3
                                                        .Distance(x.transform.localPosition,
                                                                  this.transform.localPosition) < 20)
                                              .ToArray();

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

        ResetPhysics();
    }

    //TODO: reescrever este método 
    private void ResetPhysics()
    {
        verticalInput = 0;
        horizontalInput = 0;
        isBreaking = false;

        this.rBody.velocity =
        this.rBody.angularVelocity =
        this.frontLeftWheelCollider.attachedRigidbody.velocity =
        this.rearLeftWheelCollider.attachedRigidbody.velocity =
        this.frontRightWheelCollider.attachedRigidbody.velocity =
        this.rearRightWheelCollider.attachedRigidbody.velocity =

        this.frontLeftWheelCollider.attachedRigidbody.angularVelocity =
        this.rearLeftWheelCollider.attachedRigidbody.angularVelocity =
        this.frontRightWheelCollider.attachedRigidbody.angularVelocity =
        this.rearRightWheelCollider.attachedRigidbody.angularVelocity = Vector3.zero;


        this.rBody.Sleep();
        this.frontLeftWheelCollider.attachedRigidbody.Sleep();
        this.rearLeftWheelCollider.attachedRigidbody.Sleep();
        this.frontRightWheelCollider.attachedRigidbody.Sleep();
        this.rearRightWheelCollider.attachedRigidbody.Sleep();
    }

    //Checa se o carro está de ponta cabeça
    private bool CarIsUpsideDown()
    {
        return Vector3.Dot(this.transform.up, Vector3.down) > 0;
    }
}
