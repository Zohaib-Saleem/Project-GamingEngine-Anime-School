using UnityEngine;

public class SchoolZoneTrigger : MonoBehaviour
{
    public enum ZoneAction
    {
        EnterSchool,
        ExitSchool
    }

    public ZoneAction action = ZoneAction.EnterSchool;
    bool used;

    private void OnEnable()
    {
        used = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (used)
            return;

        if (!other.CompareTag("Player") && !other.CompareTag("Cycle"))
            return;

        used = true;

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        float delay = 0.5f;
        if (other.CompareTag("Cycle"))
        {
            Level_Handler.instance.AnimeSchoolManager.StopAndExitCycle();
            delay = 1.2f;
        }

        Image_Fader fade = SchoolZoneManager.instance.fader;
        if (fade == null)
            fade = Level_Handler.instance.fader;
        if (fade != null)
            fade.FadeIn();

        Invoke(nameof(GoForIt), delay);
    }

    void GoForIt()
    {
        SchoolZoneManager zone = SchoolZoneManager.instance;

        if (action == ZoneAction.EnterSchool)
            zone.EnterSchool();
        else
            zone.ExitSchool();

        if (zone.advanceTask)
            Level_Handler.instance.ReachCurrentTask();

        Image_Fader fade = zone.fader;
        if (fade == null)
            fade = Level_Handler.instance.fader;
        if (fade != null)
            fade.FadeOut();

        gameObject.SetActive(false);
    }
}
