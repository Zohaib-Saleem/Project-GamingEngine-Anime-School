using UnityEngine;
using UnityEngine.UI;

public class RemoveAdsUI : MonoBehaviour
{
    public static RemoveAdsUI instance;

    public Button closeButton;
    public Button buyButton;

    void Awake()
    {
        instance = this;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnClose);
        }
        // Do not clear buyButton listeners — wire IAP on that button in Inspector.
        // On purchase success, call OnRemoveAdsInAppPurchased.
    }

    void OnClose()
    {
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.OnRemoveAdsPanelClosed();
        else
            gameObject.SetActive(false);
    }

    /// <summary>
    /// Pass this to Remove Ads IAP OnPurchaseComplete / success callback.
    /// Sets RemoveAds, hides banners, closes panel (no interstitial), and never shows the panel again.
    /// </summary>
    public void OnRemoveAdsInAppPurchased()
    {
        PlayerPrefs.SetInt("RemoveAds", 1);
        PlayerPrefs.Save();

        if (AdsManager.Instance != null)
            AdsManager.Instance.RemoveADs();
        else if (adsController.instance != null && adsController.instance.adsManager != null)
            adsController.instance.adsManager.RemoveADs();

        if (MainMenuManager.instance != null)
            MainMenuManager.instance.OnRemoveAdsPurchased();
        else
            gameObject.SetActive(false);
    }
}
