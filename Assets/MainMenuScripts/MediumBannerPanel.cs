using UnityEngine;

/// <summary>
/// Attach on every panel that should keep the medium banner visible.
/// Calls adsController.ShowMedBanner / HideMedBanner.
/// Banner stays shown while ANY of these panels is active.
/// </summary>
public class MediumBannerPanel : MonoBehaviour
{
    static int activePanelCount;

    void OnEnable()
    {
        activePanelCount++;
        UpdateBanner();
    }

    void OnDisable()
    {
        activePanelCount = Mathf.Max(0, activePanelCount - 1);
        UpdateBanner();
    }

    static void UpdateBanner()
    {
        if (adsController.instance == null)
            return;

        if (activePanelCount > 0)
            adsController.instance.ShowMedBanner();
        else
            adsController.instance.ShowTopBanner();
    }
}
