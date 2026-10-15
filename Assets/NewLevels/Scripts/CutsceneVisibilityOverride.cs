using UnityEngine;

/// <summary>
/// Temporarily hides ambient scene groups while a cutscene object is active,
/// then restores each group's previous state when the cutscene closes.
/// </summary>
public class CutsceneVisibilityOverride : MonoBehaviour
{
    public GameObject[] targets;

    bool[] previousStates;
    bool applied;

    void OnEnable()
    {
        if (targets == null)
            return;

        previousStates = new bool[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
                continue;

            previousStates[i] = targets[i].activeSelf;
            targets[i].SetActive(false);
        }

        applied = true;
    }

    void OnDisable()
    {
        Restore();
    }

    void OnDestroy()
    {
        Restore();
    }

    void Restore()
    {
        if (!applied || targets == null || previousStates == null)
            return;

        for (int i = 0; i < targets.Length && i < previousStates.Length; i++)
        {
            if (targets[i] != null)
                targets[i].SetActive(previousStates[i]);
        }

        applied = false;
    }
}
