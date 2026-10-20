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

    // interstitial with an AF_ analytics event, logged only when the ad can actually be requested
    public void ShowInter(string analyticsEvent)
    {
        if (adsManager == null)
            return;
        if (InterAllowed())
            AnalyticsEvents.Log(analyticsEvent);
        adsManager.ShowInterstitialWithLoading();
    }

    public void ShowRewarded(System.Action onReward, bool canShowInterOnFallback = false)
    {
        if (adsManager == null)
            return;
        adsManager.ShowRewardedWithLoading(onReward, canShowInterOnFallback);
    }

    // rewarded ad with analytics: "<placement>_Watch" on request, "<placement>_Claim" when the reward is given
    public void ShowRewarded(string analyticsPlacement, System.Action onReward, bool canShowInterOnFallback = false)
    {
        if (adsManager == null)
            return;

        AnalyticsEvents.Log(analyticsPlacement + "_Watch");
        System.Action claim = () =>
        {
            AnalyticsEvents.Log(analyticsPlacement + "_Claim");
            onReward?.Invoke();
        };
        adsManager.ShowRewardedWithLoading(claim, canShowInterOnFallback);
    }

    public void TryShowInter(bool enabled)
    {
        if (!enabled || adsManager == null)
            return;
        adsManager.ShowInterstitialWithLoading();
    }

    void TryShowInter(bool enabled, string analyticsEvent)
    {
        if (!enabled || adsManager == null)
            return;
        ShowInter(analyticsEvent);
    }

    // same early-outs as AdsManager.ShowInterstitialWithLoading, so AF_ events match real ad requests
    static bool InterAllowed()
    {
        return PlayerPrefs.GetInt("RemoveAds", 0) != 1 && PlayerPrefs.GetInt("RemoveAdsTimer", 0) != 1;
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
        TryShowInter(adsManager.modeselectionad, "AF_LevelSelection_ModeInter_Show");
    }

    public void ShowLevelSelectInter()
    {
        if (adsManager == null)
            return;
        TryShowInter(adsManager.levelselectad, "AF_LevelSelection_LevelStartInter_Show");
    }

    public void ShowPauseButtonInter()
    {
        if (adsManager == null)
            return;
        TryShowInter(adsManager.pausebuttonad, "AF_GamePlay_PauseInter_Show");
    }

    public void ShowRemoveAdsPanelInter()
    {
        if (adsManager == null)
            return;
        TryShowInter(adsManager.removeadspanelad, "AF_ModeSelection_RemoveAdsInter_Show");
    }

    public void ShowFirstMissionCompleteInter()
    {
        if (adsManager == null)
            return;
        TryShowInter(adsManager.firstmissioncompletead, "AF_LevelComplete_Inter_Show");
    }
}
