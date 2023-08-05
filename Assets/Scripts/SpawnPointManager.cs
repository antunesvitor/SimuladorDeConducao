using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    public static SpawnPointManager Instance;
    SpawnPoint[] spawnPoints;
    List<PathManager> pathManagers;

    [SerializeField] public CarAgent Agent;
    [SerializeField] public float NewSpawnPointMaxRange = 10;
    [SerializeField] public float NewSpawnPointMinRange = 0;

    private int lastRandomIndex = -1;

    void Awake()
    {
        Instance = this;
        //spawnPoints = GetComponentsInChildren<SpawnPoint>();
        pathManagers = GetComponentsInChildren<PathManager>().ToList();
        Debug.Log($"SpawnPointManager Awake() pathManagers size: ${this.pathManagers.Count()}");
        Transform pathsTransform = transform.Find("Path");
    }

    public Transform GetSpawnPoint()
    {
        return this.spawnPoints[Random.Range(0, this.spawnPoints.Length)].transform;
    }

    //Retornar um outro spawnpoint dentro de um range distancia de um dado transform
    public Transform GetNearbySpawnpoint(Transform spawnpoint)
    {
        Transform[] nearbySpawnPoints = this.spawnPoints
                                                .Where(x => Vector3
                                                    .Distance(x.transform.localPosition, spawnpoint.localPosition) < NewSpawnPointMaxRange)
                                                .Select(x=>x.transform).ToArray();

        return nearbySpawnPoints[Random.Range(0, nearbySpawnPoints.Length)];
    }

    public PathManager GetNewPath()
    {
        //Reseta o caminho anterior antes de passar um novo
        if(this.lastRandomIndex >= 0)
        {
            PathManager previousPathGiven = this.pathManagers[this.lastRandomIndex];
            previousPathGiven.ResetPath();
        }

        int randomIndex = Random.Range(0, this.pathManagers.Count());
        PathManager randomPathM = this.pathManagers[randomIndex];
        this.lastRandomIndex = randomIndex;

        randomPathM.ActivateCheckpoints();
        Debug.Log("GetNewPath() newPath nextCheckpointIndex: " + randomPathM.getNextCheckpointIndex);
        return randomPathM;
    }

    public void OnCheckedpoint(CheckpointSingle checkpointSingle)
    {
        this.Agent.OnCheckedpoint(checkpointSingle);
    }
}
