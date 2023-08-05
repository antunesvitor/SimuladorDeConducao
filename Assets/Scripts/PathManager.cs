using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathManager : MonoBehaviour
{
    private List<CheckpointSingle> checkpointsList;

    public CheckpointSingle GetFirstCheckpoint { get { return this.checkpointsList[0]; } }

    private Transform _origin;
    private Transform _destiny;

    private int nextCheckpointSingleIndex;

    public int getNextCheckpointIndex { get { return this.nextCheckpointSingleIndex; } }

    public Transform origin { get { return this._origin; } }
    public Transform destiny { get { return this._destiny; } }

    private void Awake()
    {
        var pathCheckpoints = GetComponentsInChildren<CheckpointSingle>();
        this.checkpointsList = new List<CheckpointSingle>();
        foreach(CheckpointSingle checkpoint in pathCheckpoints)
        {
            checkpoint.SetPathManager(this);
            this.checkpointsList.Add(checkpoint);
            checkpoint.Deactivate();
        }

        this._origin = transform.Find("Origin");
        this._destiny = transform.Find("Destiny");
        this.nextCheckpointSingleIndex = 0;

        //Debug.Log("Awake() checkpointList size = " + this.checkpointsList.Count);
    }

    public void WhenPlayerCrossCheckpoint()
    {
        this.nextCheckpointSingleIndex++;
        try
        {
            SpawnPointManager.Instance.OnCheckedpoint(this.checkpointsList[nextCheckpointSingleIndex]);
        }
        catch (ArgumentOutOfRangeException)
        {
            //Atingiu todos os checkpoints
            this.checkpointsList.ForEach(x => x.Deactivate());
            SpawnPointManager.Instance.OnCheckedpoint(null);
        }
    }

    public void ActivateCheckpoints()
    {
        //Debug.Log("Escolhida a rota");

        //Debug.Log("Resetando path");
        //ResetPath();
        //Debug.Log("ActivateCheckpoints() checkpointList size = " + this.checkpointsList.Count);

        //Debug.Log("ativando rota");
        this.checkpointsList.ForEach(x => x.Activate());
    }

    public void ResetPath()
    {
        this.nextCheckpointSingleIndex = 0;
        this.checkpointsList.ForEach(x => x.Deactivate());
    }
}
