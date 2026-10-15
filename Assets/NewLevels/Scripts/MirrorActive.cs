using UnityEngine;

/// <summary>
/// Keeps <see cref="target"/> active only while <see cref="source"/> is active.
/// Used so mission NPCs appear with their task point and vanish when the
/// task cutscene (which has its own copies of them) starts.
/// </summary>
public class MirrorActive : MonoBehaviour
{
    public GameObject source;
    public GameObject target;

    private void LateUpdate()
    {
        if (source == null || target == null)
            return;

        bool on = source.activeInHierarchy;
        if (target.activeSelf != on)
            target.SetActive(on);
    }
}
