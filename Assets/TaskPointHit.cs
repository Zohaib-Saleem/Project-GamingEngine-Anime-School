using UnityEngine;

public class TaskPointHit : MonoBehaviour
{
    bool used;

    private void OnTriggerEnter(Collider other)
    {
        if (used)
            return;

        if (!other.CompareTag("Player") && !other.CompareTag("Cycle"))
            return;

        used = true;

        Collider triggerCol = GetComponent<Collider>();
        if (triggerCol != null)
            triggerCol.enabled = false;

        Level_Handler.instance.ReachCurrentTask();
    }
}
