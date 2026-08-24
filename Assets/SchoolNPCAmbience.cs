using UnityEngine;

public class SchoolNPCAmbience : MonoBehaviour
{
    public static SchoolNPCAmbience instance;

    [Tooltip("Outside NPCs. Disabled when player is inside the school.")]
    public GameObject outdoorNPCs;
    [Tooltip("Inside NPCs. Disabled when player is outside.")]
    public GameObject indoorNPCs;

    bool inside;

    private void Awake()
    {
        instance = this;
        ShowOutdoor();
    }

    public void ShowOutdoor()
    {
        inside = false;
        SetGroup(outdoorNPCs, true);
        SetGroup(indoorNPCs, false);
    }

    public void ShowIndoor()
    {
        inside = true;
        SetGroup(outdoorNPCs, false);
        SetGroup(indoorNPCs, true);
    }

    public void Refresh()
    {
        if (inside)
            ShowIndoor();
        else
            ShowOutdoor();
    }

    void SetGroup(GameObject group, bool active)
    {
        if (group != null && group.activeSelf != active)
            group.SetActive(active);
    }
}
