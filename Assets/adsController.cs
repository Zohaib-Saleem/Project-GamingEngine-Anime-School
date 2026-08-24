using UnityEngine;

public class adsController : MonoBehaviour
{
    public static adsController instance;

    public AdsManager adsManager;

    void Awake()
    {
        instance = this;

        if (adsManager == null)
            adsManager = FindObjectOfType<AdsManager>();
        ShowTopBanner();
    }

    void Start()
    {
        
    }

    public void ShowTopBanner()
    {
        if (adsManager != null)
            adsManager.ShowTopBannerWithMediation();
    }

    public void ShowMedBanner()
    {
        if (adsManager != null)
            adsManager.ShowMediumRect();
    }

    public void HideMedBanner()
    {
        if (adsManager != null)
            adsManager.HideMediumRect();
    }

    public void ShowInter()
    {
        if (adsManager != null)
            adsManager.ShowInterstitialWithLoading();
    }

    public void ShowRewarded(System.Action onReward, bool canShowInterOnFallback = false)
    {
        if (adsManager == null)
            return;
        adsManager.ShowRewardedWithLoading(onReward, canShowInterOnFallback);
    }

    public void TryShowInter(bool enabled)
    {
        if (!enabled || adsManager == null)
            return;
        adsManager.ShowInterstitialWithLoading();
    }

    public void ShowDailyRewardBackInter()
    {
        if (adsManager == null)
            return;
        TryShowInter(adsManager.dailyrewardbackad);
    }

    public void ShowBeforeMainMenuInter()
    {
        if (adsManager == null)
            return;
        TryShowInter(adsManager.beforemainmenu);
    }

    public void ShowMainMenuStartInter()
    {
        if (adsManager == null)
            return;
        //TryShowInter(adsManager.mainmenustartad);
    }

    public void ShowModeSelectionInter()
    {
        if (adsManager == null)
            return;
        TryShowInter(adsManager.modeselectionad);
    }

    public void ShowLevelSelectInter()
    {
        if (adsManager == null)
            return;
        TryShowInter(adsManager.levelselectad);
    }

    public void ShowPauseButtonInter()
    {
        if (adsManager == null)
            return;
        TryShowInter(adsManager.pausebuttonad);
    }

    public void ShowRemoveAdsPanelInter()
    {
        if (adsManager == null)
            return;
        TryShowInter(adsManager.removeadspanelad);
    }
}
