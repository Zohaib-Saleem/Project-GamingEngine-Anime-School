using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger_handler : MonoBehaviour
{
    public AnimeSchoolManager _manager;
    void Start()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Cycle"))
        {
            _manager.EnterRideBtn.SetActive(true);
            _manager.TriggeredCycle = other.gameObject;
            _manager.AssignCh();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Cycle"))
        {
            _manager.EnterRideBtn.SetActive(false);
        }
    }
}
