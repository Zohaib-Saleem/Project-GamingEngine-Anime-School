using UnityEngine;
using UnityEngine.UI;

public class MainMenuHomeUI : MonoBehaviour
{
    public static MainMenuHomeUI instance;

    public Button startButton;
    public Button settingsButton;
    public Button exitButton;
    public Button dailyRewardButton;
    public Button profileButton;
    public Button storeButton;
    public Button storePlusButton;
    public Button watchVideoButton;
    public int watchVideoReward = 500;
    public RewardPopupUI rewardPopup;
    public Text profileNameText;
    public Image profileAvatarImage;

    void Awake()
    {
        instance = this;

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStart);
        }
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OnSettings);
        }
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(OnExit);
        }
        if (dailyRewardButton != null)
        {
            dailyRewardButton.onClick.RemoveAllListeners();
            dailyRewardButton.onClick.AddListener(OnDailyReward);
        }
        if (profileButton != null)
        {
            profileButton.onClick.RemoveAllListeners();
            profileButton.onClick.AddListener(OnProfile);
        }
        if (storeButton != null)
        {
            storeButton.onClick.RemoveAllListeners();
            storeButton.onClick.AddListener(OnStore);
        }
        if (storePlusButton != null)
        {
            storePlusButton.onClick.RemoveAllListeners();
            storePlusButton.onClick.AddListener(OnStore);
        }
        if (watchVideoButton != null)
        {
            watchVideoButton.onClick.RemoveAllListeners();
            watchVideoButton.onClick.AddListener(OnWatchVideo);
        }
    }

    void OnEnable()
    {
        RefreshProfileDisplay();
    }

    public void RefreshProfileDisplay()
    {
        RefreshProfileName();
        AvatarData.ApplyTo(profileAvatarImage);
    }

    public void RefreshProfileName()
    {
        if (profileNameText == null)
            return;

        string name = ProfileData.Name;
        profileNameText.text = string.IsNullOrEmpty(name) ? "PLAYER" : name;
    }

    void OnStart()
    {
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.OpenModeSelect();
    }

    void OnSettings()
    {
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.OpenSettings();
    }

    void OnExit()
    {
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.OpenExitPanel();
    }

    void OnDailyReward()
    {
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.OpenDailyReward();
    }

    void OnProfile()
    {
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.OpenProfile();
    }

    void OnStore()
    {
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.OpenStore();
    }

    void OnWatchVideo()
    {
        // Placeholder until ad SDK — direct reward panel (same as store/daily).
        int reward = Mathf.Max(0, watchVideoReward);
        if (reward <= 0)
            return;

        if (rewardPopup != null)
        {
            rewardPopup.ShowDoubleOrSkip(reward, null);
            return;
        }

        CurrencyManager.AddCoins(reward);
    }
}
