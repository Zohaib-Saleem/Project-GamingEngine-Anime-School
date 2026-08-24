using UnityEngine;

/// <summary>
/// Enable this GameObject only when device RAM is at least <see cref="minRamMb"/>.
/// Put on the Grass folder under env in Gameplay.
/// </summary>
public class DeviceMemoryGate : MonoBehaviour
{
    [Tooltip("Minimum device RAM in MB. 8192 = 8 GB.")]
    public int minRamMb = 8192;

    [Tooltip("If true, object stays on when RAM is enough. If false, inverted.")]
    public bool enableWhenEnoughRam = true;

#if UNITY_EDITOR
    [Tooltip("Editor often has lots of RAM — force this state while testing in Editor.")]
    public bool forceStateInEditor;
    public bool editorForcedActive = true;
#endif

    void Awake()
    {
        Apply();
    }

    public void Apply()
    {
        bool enough = SystemInfo.systemMemorySize >= minRamMb;

#if UNITY_EDITOR
        if (forceStateInEditor)
            enough = editorForcedActive;
#endif

        bool shouldBeActive = enableWhenEnoughRam ? enough : !enough;
        if (gameObject.activeSelf != shouldBeActive)
            gameObject.SetActive(shouldBeActive);
    }
}
