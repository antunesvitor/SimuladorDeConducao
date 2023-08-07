using UnityEngine;

public class SidewalkCollider : MonoBehaviour
{
    //private void OnTriggerEnter(Collider other)
    //{
    //    if(other.TryGetComponent<CarAgent>(out CarAgent agent))
    //    { 
    //        agent.OnSideWalkCollision();
    //    }
    //}

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.TryGetComponent<CarAgent>(out CarAgent agent))
        {
            agent.OnSideWalkCollision();
        }
    }
}
