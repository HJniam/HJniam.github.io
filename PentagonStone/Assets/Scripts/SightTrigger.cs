using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SightTrigger : MonoBehaviour
{
    public bool sightTrigger = false;
    public int playerCount = 0;


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            sightTrigger = true;
            playerCount++;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            playerCount--;
            if (playerCount == 0)
                sightTrigger = false;
        }
    }
}
