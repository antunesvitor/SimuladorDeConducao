using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] GameObject body;

    private void Awake()
    {
        this.body.SetActive(false);
    }
}
