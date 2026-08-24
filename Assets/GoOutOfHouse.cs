using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoOutOfHouse : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Level_Handler.instance.fader.FadeIn();
            Invoke(nameof(GoForIt), .5f);
            
            this.gameObject.SetActive(false);
        }
    }
    public void GoForIt()
    {
        Level_Handler.instance.SpawnPlayerOutsideHouse();
        Level_Handler.instance.ReachCurrentTask();
        Level_Handler.instance.fader.FadeOut();
    }
}
