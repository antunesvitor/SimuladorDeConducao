using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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

    private CheckpointSingle nextCheckpoint;
    private Transform resetPosition;
    private float stepPunishment { get { return Constants.FEEDBACK_MAXSTEPS_REACHED_PER_STEP / this.MaxStep; } }
    private int currentPathCheckoutPointsCount;
    private float singleCheckpointReward;
    private int collisionsCount = 0;
    private int currentCheckpointsCount = 0;

    public bool isTesting = false;

    // Settings
    [SerializeField] private float motorForce, breakForce, maxSteerAngle;

    // Wheel Colliders
    [SerializeField] private WheelCollider frontLeftWheelCollider, frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider, rearRightWheelCollider;

    // Wheels
    [SerializeField] private Transform frontLeftWheelTransform, frontRightWheelTransform;
    [SerializeField] private Transform rearLeftWheelTransform, rearRightWheelTransform;

    [SerializeField] private Transform Target;
    [SerializeField] private SpawnPointManager SpawnPointManager;

    public void Start()
    {
        this.rBody = GetComponent<Rigidbody>();
        // Debug.Log($"Step Punishment: {stepPunishment}");
    }

    #region :: ML-AGENTS METHODS ::
    public override void OnEpisodeBegin()
    {
        SetNewPath();
        //Aqui se define uma recompensa para cada checkpoint atingido com base na quantidade de checkpoints que há naquele Path
        //Então se um trajeto tiver mais checkpoints, o agente recebe menos recompensa POR cada um deles
        this.singleCheckpointReward = Constants.FEEDBACK_MAX_CHECKPOINT_REACHED / this.currentPathCheckoutPointsCount;
        // Debug.Log($"singleCheckpointReward: {singleCheckpointReward}");
        //Reseta contador de colisões
        this.collisionsCount = 0;
        this.currentCheckpointsCount = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
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

        //Aplicado uma pequena punição para que o agente entenda que não possa ficar parado
        ApplyReward(stepPunishment);

        if (CarIsUpsideDown())
        {
            SetReward(Constants.FEEDBACK_CAR_UPSIDE_DOWN);
            // Debug.Log($"episode reward: {Constants.FEEDBACK_CAR_UPSIDE_DOWN}");
            EndEpisode();
        }

        if (distanceToTarget < 2.5f)
        {
            float collisionsPenaty = this.collisionsCount * Constants.FEEDBACK_COLLISION_SIDEWALK;
            float checkpointsReward = this.currentCheckpointsCount * this.singleCheckpointReward;

            float finalReward = Constants.FEEDBACK_DESTINATION_REACHED + checkpointsReward;

            //limitado para que não exceda 1
            finalReward = finalReward > 1 ? 1 : finalReward;

            //limitado para que não exceda nem -1 nem 1
            finalReward = Mathf.Clamp(finalReward + collisionsPenaty, -1, 1);

            Debug.Log($"finalreward: {finalReward}");
            SetReward(finalReward);
            EndEpisode();
        }

        //Caso este seja o último step e o agente não atingiu o objetivo
        if (this.StepCount == this.MaxStep)
        {
            SetReward(Constants.FEEDBACK_MAXSTEPS_REACHED);

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
        }
    }

    #endregion

    #region :: CAR CONTROLL ::
    private void UpdateCar()
    {
        //// Debug.Log("Atualizando carro");
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
        PathManager newPath = this.SpawnPointManager.GetNewPath(!isTesting);

        Transform newCarLocation = newPath.origin.transform;
        Transform newDestination = newPath.destiny.transform;

        this.transform.localPosition = newCarLocation.localPosition;
        this.transform.rotation = newCarLocation.rotation;

        this.resetPosition = newCarLocation;
        this.nextCheckpoint = newPath.GetFirstCheckpoint;
        this.currentPathCheckoutPointsCount = newPath.checkpointsCount;

        Target.localPosition = new Vector3(newDestination.localPosition.x, 1, newDestination.localPosition.z);

        ResetPhysics();
    }

    //Função que o SpawnpointManager chama informando que o checkpoint foi cruzado
    public void OnCheckedpoint(CheckpointSingle nextCheckpoint)
    {
        this.resetPosition = this.nextCheckpoint?.transform ?? this.resetPosition;
        this.nextCheckpoint = nextCheckpoint;

        //incrementa o número de checkpoints
        this.currentCheckpointsCount++;

        ApplyReward(singleCheckpointReward);
    }

    public void OnSideWalkCollision()
    {
        // // Debug.Log($"Colidiu com calçada! -{Constants.FEEDBACK_COLLISION_SIDEWALK} pts | atual: {this.GetCumulativeReward()}");

        this.collisionsCount++;

        ApplyReward(Constants.FEEDBACK_COLLISION_SIDEWALK);
    }

    public void OnWallCollsion()
    {
        // // Debug.Log($"Colidiu com parede! -{Constants.FEEDBACK_COLLISION_WALL} pts | atual: {this.GetCumulativeReward()}");
        this.collisionsCount++;

        ApplyReward(Constants.FEEDBACK_COLLISION_WALL);
    }


    // Aplica a recompensa ou punição mas garante que o acumulado permaneça dentro de [-1,1]
    private void ApplyReward(float rewardToApply)
    {
        float newCumulativeReward = this.GetCumulativeReward() + rewardToApply;
        // Debug.Log("Recompensa a aplicar: " + rewardToApply);
        if (newCumulativeReward < -1){
            SetReward(-1);
            EndEpisode();
        }
        else if (newCumulativeReward > 1){
            SetReward(1);
            Debug.LogError("ISTO NÃO DEVE SER ACESSADO NUNCA");
        }
        else
            AddReward(rewardToApply);
    }

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

    //Checa se o carro est� de ponta cabe�a
    private bool CarIsUpsideDown()
    {
        return Vector3.Dot(this.transform.up, Vector3.down) > 0;
    }

    private void FixCarPosition()
    {
        //Transform newPos = SpawnPointManager.Instance.GetNearbySpawnpoint(this.transform);
        this.transform.localPosition = this.resetPosition.localPosition;
        this.transform.rotation = this.resetPosition.rotation;
    }
}
