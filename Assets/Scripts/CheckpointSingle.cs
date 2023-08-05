using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointSingle : MonoBehaviour
{
    private PathManager pathManager;
    [SerializeField] public GameObject body;

    public bool crossed { get; set; }

    private void Awake()
    {
        //this.body.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<CarAgent>(out CarAgent agent))
        {
            if (!this.crossed)  //Para que não cruze 2x
            {
                this.crossed = true;
                this.body.SetActive(false);
                this.pathManager.WhenPlayerCrossCheckpoint();
            }
        }
    }

    public void SetPathManager(PathManager pathManager)
    {
        this.pathManager = pathManager;
    }

    public void Deactivate()
    {
        this.crossed = false;
        this.body.SetActive(false);
    }

    public void Activate()
    {
        this.crossed = false;
        this.body.SetActive(true);
    }
}
