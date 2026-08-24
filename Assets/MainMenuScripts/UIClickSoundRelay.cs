using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Plays UI click sound on pointer click. Not cleared by Button.onClick.RemoveAllListeners().
/// </summary>
[DisallowMultipleComponent]
public class UIClickSoundRelay : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;

        // Skip if button exists and is not interactable
        Button button = GetComponent<Button>();
        if (button != null && (!button.IsInteractable() || !button.enabled))
            return;

        UIClickSound.Play();
    }
}
