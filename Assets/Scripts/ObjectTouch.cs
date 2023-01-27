using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectTouch : MonoBehaviour
{
    
    [SerializeField]
    private float minTimeForTrigger = 0;
    [SerializeField]
    private float MaxTimeForTrigger=2;

    private float timeInide = 0;

    private void OnCollisionEnter(Collision collision)
    {
        timeInide = 0;
    }
    private void OnCollisionStay(Collision collision)
    {
        timeInide += Time.deltaTime;
    }

    private void OnCollisionExit(Collision collision)
    {
        if(timeInide<MaxTimeForTrigger && timeInide> minTimeForTrigger)
        {
            print("trigger");
        }
    }
}
