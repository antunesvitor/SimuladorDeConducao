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

    private float carDistanceToDestination;             //distância entre o carro e o destino no início do episódio
    private float currentCarDistanceToDestination;      //distância entre o carro e o destino a cada frame

    private float timeLimit = 20;                       //Tempo limite em segundos para atingir o objetivo
    private float elapsedTime = 0;                      //Tempo decorrido em segundos

    private CheckpointSingle nextCheckpoint;
    private Transform resetPosition;

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
    }


    #region :: ML-AGENTS METHODS ::
    public override void OnEpisodeBegin()
    {
        SetNewPath();

        //atualiza as novas variáveis de distância entre o agente e o destino
        this.carDistanceToDestination = Vector3.Distance(this.transform.localPosition, Target.transform.position);
        this.currentCarDistanceToDestination = this.carDistanceToDestination;

        this.elapsedTime = 0;
    }  

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(this.transform.localPosition);
        //sensor.AddObservation(Target.localPosition);
        sensor.AddObservation(this.nextCheckpoint?.transform.localPosition ?? Target.localPosition);

        //Dar um jeito de add observacao da velocidade do carro
        sensor.AddObservation(this.rBody.velocity.x);
        sensor.AddObservation(this.rBody.velocity.z);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        verticalInput = actions.ContinuousActions[0];
        horizontalInput = actions.ContinuousActions[1];
        isBreaking = actions.DiscreteActions[0] == 1;

        UpdateCar();

        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);

        this.elapsedTime += Time.deltaTime;
        //SetReward(- (Time.deltaTime * 0.1f) );

        if (CarIsUpsideDown())
        {
            SetReward(-5.0f);
            Debug.Log($"episode reward: {this.GetCumulativeReward()}");
            EndEpisode();
            //FixCarPosition();
        }

        if(this.elapsedTime > this.timeLimit)
        {
            SetReward(-2.0f);
            Debug.Log($"episode reward: {this.GetCumulativeReward()}");
            EndEpisode();
        }

        if (distanceToTarget < 2.5f)
        {
            this.elapsedTime = 0f;
            SetReward(5.0f);
            SetNewPath();

            Debug.Log($"episode reward: {this.GetCumulativeReward()}");
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        var discreteActionsOut = actionsOut.DiscreteActions;

        verticalInput = Input.GetAxis("Vertical");
        horizontalInput = Input.GetAxis("Horizontal");
        isBreaking = Input.GetKey(KeyCode.Space);

        continuousActionsOut[0] = verticalInput;
        continuousActionsOut[1] = horizontalInput;
        discreteActionsOut[0] = isBreaking ? 1 : 0;

        UpdateCar();

        if (CarIsUpsideDown())
        {
            FixCarPosition();
            //Debug.Log($"episode reward: {this.GetCumulativeReward()}")EndEpisode();
        }
    }

    #endregion

    #region :: CAR CONTROLL ::
    private void UpdateCar()
    {
        //Debug.Log("Atualizando carro");
        HandleMotor();
        HandleSteering();
        UpdateWheels();
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
    #endregion

    private void SetNewPath()
    {
        PathManager newPath = SpawnPointManager.Instance.GetNewPath();
      
        Transform newCarLocation = newPath.origin.transform;
        Transform newDestination = newPath.destiny.transform;

        this.transform.localPosition = newCarLocation.localPosition;
        this.transform.rotation = newCarLocation.rotation;

        this.resetPosition = newCarLocation;
        this.nextCheckpoint = newPath.GetFirstCheckpoint;
        Target.localPosition = newDestination.localPosition;

        ResetPhysics();
    }

    //Função que o SpawnpointManager chama informando que o checkpoint foi cruzado
    public void OnCheckedpoint(CheckpointSingle nextCheckpoint)
    {
        //if(nextCheckpoint != null) Debug.Log($"Checkpoint! {this.nextCheckpoint.name}");
        this.resetPosition = this.nextCheckpoint?.transform ?? this.resetPosition;
        this.nextCheckpoint = nextCheckpoint;

        //TODO: fazer com que as recompensas estejam organizadas em constantes em um código separado
        SetReward(2f);
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

    private void FixCarPosition()
    {
        Debug.Log("==>consertando posição");
        //Transform newPos = SpawnPointManager.Instance.GetNearbySpawnpoint(this.transform);
        this.transform.localPosition = this.resetPosition.localPosition;
        this.transform.rotation = this.resetPosition.rotation;
    }
}
