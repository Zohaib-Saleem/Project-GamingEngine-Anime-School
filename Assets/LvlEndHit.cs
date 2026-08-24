using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LvlEndHit : MonoBehaviour
{
    bool used;

    private void OnTriggerEnter(Collider other)
    {
        if (used)
            return;

        if (!other.CompareTag("Player") && !other.CompareTag("Cycle"))
            return;

        used = true;
        Level_Handler.instance.ChArrow.gameObject.SetActive(false);

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        if (Level_Handler.instance.levelNo == 1 && other.CompareTag("Cycle"))
        {
            Level_Handler.instance.AnimeSchoolManager.StopAndExitCycle();
            Invoke(nameof(GoForIt), 1.2f);
            return;
        }

        Level_Handler.instance.fader.FadeIn();
        Invoke(nameof(GoForIt), .5f);
        gameObject.SetActive(false);
    }

    public void GoForIt()
    {
        Level_Handler.instance.ReachCurrentTask();
        Level_Handler.instance.fader.FadeOut();
        gameObject.SetActive(false);
    }
}
