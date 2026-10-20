using System.Collections;
using KikiNgao.SimpleBikeControl;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager instance;

    public enum MenuScreen
    {
        Home,
        ModeSelect,
        LevelSelect,
        Settings,
        Exit
    }

    [Header("Panels")]
    public GameObject homePanel;
    public GameObject modeSelectPanel;
    public GameObject levelSelectPanel;
    public GameObject settingsPanel;
    public GameObject exitPanel;
    public GameObject dailyRewardPanel;
    public GameObject profilePanel;
    public GameObject avatarPanel;
    public GameObject storePanel;
    public GameObject removeAdsPanel;
    public GameObject fakeLoading;

    [Header("Daily Reward")]
    public bool showDailyRewardOnStart = true;

    [Header("Scenes")]
    public string gameplaySceneName = "Gameplay";
    public float fakeLoadTime = 2f;

    MenuScreen currentScreen = MenuScreen.Home;
    MenuScreen settingsReturnScreen = MenuScreen.Home;
    Coroutine transitionRoutine;
    bool showInterAfterFirstProfile;
    bool pendingRemoveAdsOnModeSelect;
    bool pendingMainMenuStartInter;
    bool pendingModeSelectionInter;
    bool screenLogged;

    void Awake()
    {
        instance = this;
        if (fakeLoading != null)
            fakeLoading.SetActive(false);
        if (exitPanel != null)
            exitPanel.SetActive(false);
        if (dailyRewardPanel != null)
            dailyRewardPanel.SetActive(false);
        if (profilePanel != null)
            profilePanel.SetActive(false);
        if (avatarPanel != null)
            avatarPanel.SetActive(false);
        if (storePanel != null)
            storePanel.SetActive(false);
        if (removeAdsPanel != null)
            removeAdsPanel.SetActive(false);
        GameSettings.Load();
    }

    void Start()
    {
        ShowScreenImmediate(MenuScreen.Home);

        if (showDailyRewardOnStart && dailyRewardPanel != null)
            OpenDailyReward();
        else
            ContinueFirstTimeFlow();
    }

    public void OpenDailyReward()
    {
        if (dailyRewardPanel != null)
        {
            AnalyticsEvents.Log("GF_3_DailyReward_Open");
            dailyRewardPanel.SetActive(true);
            if (DailyRewardUI.instance != null)
                DailyRewardUI.instance.Refresh();
        }
    }

    public void OpenDailyRewardImmediate()
    {
        ShowScreenImmediate(MenuScreen.Home);
        OpenDailyReward();
    }

    public void OnDailyRewardClosed()
    {
        if (dailyRewardPanel != null)
            dailyRewardPanel.SetActive(false);

        bool firstFlow = !AvatarData.IsCompleted || !ProfileData.IsCompleted;

        if (firstFlow)
        {
            // Panel first, then interstitial
            ContinueFirstTimeFlow();

            // First flow: ad after daily only when BOTH bools are true
            if (CanShowDailyRewardBackAd() && CanShowBeforeMainMenuAd())
                ShowInterNow("AF_MainMenu_DailyRewardInter_Show");
        }
        else
        {
            // Returning flow: only dailyrewardbackad
            if (CanShowDailyRewardBackAd())
                ShowInterNow("AF_MainMenu_DailyRewardInter_Show");
        }
    }

    void ContinueFirstTimeFlow()
    {
        // First play: Avatar (once) → Profile (once) → optional before-main-menu ad
        if (!AvatarData.IsCompleted)
        {
            OpenAvatar();
            return;
        }

        if (!ProfileData.IsCompleted)
        {
            showInterAfterFirstProfile = true;
            OpenProfile();
            return;
        }
    }

    public void OpenAvatar()
    {
        if (avatarPanel != null)
        {
            AnalyticsEvents.Log("GF_4_AvatarSelection_Open");
            avatarPanel.SetActive(true);
        }
    }

    public void OnAvatarSaved()
    {
        if (avatarPanel != null)
            avatarPanel.SetActive(false);

        if (MainMenuHomeUI.instance != null)
            MainMenuHomeUI.instance.RefreshProfileDisplay();

        if (!ProfileData.IsCompleted)
        {
            showInterAfterFirstProfile = true;
            OpenProfile();
        }
    }

    public void OpenProfile()
    {
        if (profilePanel != null)
        {
            AnalyticsEvents.Log("GF_5_Profile_Open");
            profilePanel.SetActive(true);
            if (ProfileUI.instance != null)
                ProfileUI.instance.LoadFields();
        }
    }

    public void OnProfileSaved()
    {
        if (profilePanel != null)
            profilePanel.SetActive(false);

        if (MainMenuHomeUI.instance != null)
            MainMenuHomeUI.instance.RefreshProfileDisplay();

        // First flow end: ad after profile if either bool allows entry ad
        // - only dailyrewardbackad true → ad here
        // - both true → ad here (beforemainmenu) as well as earlier after daily
        if (showInterAfterFirstProfile)
        {
            showInterAfterFirstProfile = false;
            if (CanShowDailyRewardBackAd() || CanShowBeforeMainMenuAd())
                ShowInterNow("AF_MainMenu_ProfileInter_Show");
        }
    }

    bool CanShowDailyRewardBackAd()
    {
        return adsController.instance != null
            && adsController.instance.adsManager != null
            && adsController.instance.adsManager.dailyrewardbackad;
    }

    bool CanShowBeforeMainMenuAd()
    {
        return adsController.instance != null
            && adsController.instance.adsManager != null
            && adsController.instance.adsManager.beforemainmenu;
    }

    void ShowInterNow(string analyticsEvent)
    {
        if (adsController.instance != null)
            adsController.instance.ShowInter(analyticsEvent);
    }

    public void OpenStore()
    {
        if (storePanel != null)
        {
            storePanel.SetActive(true);
            if (StoreUI.instance != null)
                StoreUI.instance.RefreshVideos();
        }
    }

    public void OnStoreClosed()
    {
        if (storePanel != null)
            storePanel.SetActive(false);
    }

    public void OpenHome()
    {
        TransitionTo(MenuScreen.Home);
    }

    public void OpenModeSelect()
    {
        // After loading, Mode Select opens then Remove Ads panel (if ads not removed).
        pendingRemoveAdsOnModeSelect = PlayerPrefs.GetInt("RemoveAds", 0) != 1;
        pendingMainMenuStartInter = true;
        TransitionTo(MenuScreen.ModeSelect);
    }

    public void OpenLevelSelect()
    {
        pendingModeSelectionInter = true;
        TransitionTo(MenuScreen.LevelSelect);
    }

    public void OpenSettings()
    {
        settingsReturnScreen = currentScreen;
        ShowScreenImmediate(MenuScreen.Settings);
    }

    public void CloseSettings()
    {
        ShowScreenImmediate(settingsReturnScreen);
    }

    public void OpenExitPanel()
    {
        if (exitPanel != null)
            exitPanel.SetActive(true);
    }

    public void CloseExitPanel()
    {
        if (exitPanel != null)
            exitPanel.SetActive(false);
    }

    public void ConfirmExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void BackFromLevelSelect()
    {
        pendingRemoveAdsOnModeSelect = false;
        pendingMainMenuStartInter = false;
        TransitionTo(MenuScreen.ModeSelect);
    }

    public void BackFromModeSelect()
    {
        if (removeAdsPanel != null)
            removeAdsPanel.SetActive(false);
        pendingMainMenuStartInter = false;
        TransitionTo(MenuScreen.Home);
    }

    public void OpenRemoveAdsPanel()
    {
        if (PlayerPrefs.GetInt("RemoveAds", 0) == 1)
            return;

        if (removeAdsPanel != null)
        {
            AnalyticsEvents.Log("BF_ModeSelection_RemoveAdsPanel_Open");
            removeAdsPanel.SetActive(true);
        }
    }

    public void OnRemoveAdsPanelClosed()
    {
        if (removeAdsPanel != null)
            removeAdsPanel.SetActive(false);

        if (adsController.instance != null)
            adsController.instance.ShowRemoveAdsPanelInter();
    }

    /// <summary>
    /// After Remove Ads IAP success — close panel without interstitial and skip future auto-open.
    /// </summary>
    public void OnRemoveAdsPurchased()
    {
        pendingRemoveAdsOnModeSelect = false;

        if (removeAdsPanel != null)
            removeAdsPanel.SetActive(false);
    }

    public void LoadGameplay(int levelIndex)
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(LoadGameplayRoutine(levelIndex));
    }

    void TransitionTo(MenuScreen target)
    {
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionRoutine(target));
    }

    IEnumerator TransitionRoutine(MenuScreen target)
    {
        if (fakeLoading != null)
            fakeLoading.SetActive(true);

        yield return new WaitForSecondsRealtime(Mathf.Max(0.35f, fakeLoadTime));

        // Panel / screen first
        ShowScreenImmediate(target);

        if (fakeLoading != null)
            fakeLoading.SetActive(false);

        if (target == MenuScreen.ModeSelect && pendingRemoveAdsOnModeSelect)
        {
            pendingRemoveAdsOnModeSelect = false;
            OpenRemoveAdsPanel();
        }

        // Then interstitial (after panel is open) — only for forward navigation
        if (adsController.instance != null)
        {
            if (target == MenuScreen.ModeSelect && pendingMainMenuStartInter)
            {
                pendingMainMenuStartInter = false;
                adsController.instance.ShowMainMenuStartInter();
            }
            else if (target == MenuScreen.LevelSelect && pendingModeSelectionInter)
            {
                pendingModeSelectionInter = false;
                adsController.instance.ShowModeSelectionInter();
            }
        }

        transitionRoutine = null;
    }

    IEnumerator LoadGameplayRoutine(int levelIndex)
    {
        // Loading panel first, then interstitial
        if (fakeLoading != null)
            fakeLoading.SetActive(true);

        if (adsController.instance != null)
            adsController.instance.ShowLevelSelectInter();

        PlayerPrefs.SetInt("SelectedLevel", Mathf.Max(0, levelIndex));
        PlayerPrefs.Save();

        yield return new WaitForSecondsRealtime(Mathf.Max(0.35f, fakeLoadTime));

        if (GameManager.Instance != null)
            Destroy(GameManager.Instance.gameObject);

        SceneManager.LoadScene(gameplaySceneName);
    }

    void ShowScreenImmediate(MenuScreen screen)
    {
        if (!screenLogged || screen != currentScreen)
            LogScreenOpen(screen);
        screenLogged = true;
        currentScreen = screen;
        if (exitPanel != null)
            exitPanel.SetActive(false);

        SetPanel(homePanel, screen == MenuScreen.Home);
        SetPanel(modeSelectPanel, screen == MenuScreen.ModeSelect);
        SetPanel(levelSelectPanel, screen == MenuScreen.LevelSelect);
        SetPanel(settingsPanel, screen == MenuScreen.Settings);

        if (screen == MenuScreen.LevelSelect && LevelSelectionUI.instance != null)
            LevelSelectionUI.instance.Refresh();
    }

    static void LogScreenOpen(MenuScreen screen)
    {
        switch (screen)
        {
            case MenuScreen.Home: AnalyticsEvents.Log("GF_2_MainMenu_Open"); break;
            case MenuScreen.ModeSelect: AnalyticsEvents.Log("GF_6_ModeSelection_Open"); break;
            case MenuScreen.LevelSelect: AnalyticsEvents.Log("GF_7_LevelSelection_Open"); break;
        }
    }

    static void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
}
