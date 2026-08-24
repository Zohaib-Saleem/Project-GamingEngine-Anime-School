using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Costume_Trigger : MonoBehaviour
{
    bool used;

    private void OnTriggerEnter(Collider other)
    {
        if (used)
            return;

        if (!other.CompareTag("Player"))
            return;

        CostumeSelection costume = other.GetComponentInParent<CostumeSelection>();
        if (costume == null)
            return;

        used = true;

        Collider triggerCol = GetComponent<Collider>();
        if (triggerCol != null)
            triggerCol.enabled = false;

        costume.SelectCostume(1);
        Level_Handler.instance.ReachCurrentTask();
        this.gameObject.SetActive(false);
    }
}
