using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    public static SpawnPointManager Instance;
    SpawnPoint[] spawnPoints;

    [SerializeField] public float NewSpawnPointMaxRange = 10;
    [SerializeField] public float NewSpawnPointMinRange = 0;

    void Awake()
    {
        Instance = this;
        spawnPoints = GetComponentsInChildren<SpawnPoint>();
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
}
