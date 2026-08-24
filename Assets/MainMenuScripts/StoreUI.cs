using UnityEngine;
using UnityEngine.UI;

public class StoreUI : MonoBehaviour
{
    public static StoreUI instance;

    [Header("Tabs")]
    public Button coinsTabButton;
    public Button videosTabButton;
    public GameObject coinsTabHighlight;
    public GameObject videosTabHighlight;
    public GameObject coinsContent;
    public GameObject videosContent;

    [Header("Close")]
    public Button closeButton;

    [Header("Coins Packs")]
    public StoreCoinItem[] coinItems;

    [Header("Videos")]
    public Button[] videoWatchButtons;
    public int[] videoRewards = { 200, 600, 1500, 2500, 5500 };

    [Header("Reward Popup")]
    public RewardPopupUI rewardPopup;

    void Awake()
    {
        instance = this;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
        if (coinsTabButton != null)
        {
            coinsTabButton.onClick.RemoveAllListeners();
            coinsTabButton.onClick.AddListener(ShowCoinsTab);
        }
        if (videosTabButton != null)
        {
            videosTabButton.onClick.RemoveAllListeners();
            videosTabButton.onClick.AddListener(ShowVideosTab);
        }

        BindCoinItems();
        BindVideoButtons();
    }

    void OnEnable()
    {
        int total = videoWatchButtons != null ? videoWatchButtons.Length : 0;
        StoreVideoData.PrepareForSession(total);
        ShowVideosTab();
    }

    void BindCoinItems()
    {
        if (coinItems == null)
            return;

        for (int i = 0; i < coinItems.Length; i++)
        {
            if (coinItems[i] != null)
                coinItems[i].Bind(this);
        }
    }

    void BindVideoButtons()
    {
        if (videoWatchButtons == null)
            return;

        for (int i = 0; i < videoWatchButtons.Length; i++)
        {
            if (videoWatchButtons[i] == null)
                continue;

            int index = i;
            videoWatchButtons[i].onClick.RemoveAllListeners();
            videoWatchButtons[i].onClick.AddListener(() => WatchVideo(index));
        }
    }

    public void ShowCoinsTab()
    {
        SetTab(true);
    }

    public void ShowVideosTab()
    {
        SetTab(false);
        RefreshVideos();
    }

    void SetTab(bool coins)
    {
        if (coinsContent != null)
            coinsContent.SetActive(coins);
        if (videosContent != null)
            videosContent.SetActive(!coins);
        if (coinsTabHighlight != null)
            coinsTabHighlight.SetActive(coins);
        if (videosTabHighlight != null)
            videosTabHighlight.SetActive(!coins);
    }

    public void RefreshVideos()
    {
        if (videoWatchButtons == null)
            return;

        int total = videoWatchButtons.Length;
        int unlocked = StoreVideoData.GetNextUnlockedIndex();
        bool cycleDone = StoreVideoData.AllCompleted(total);

        for (int i = 0; i < total; i++)
        {
            if (videoWatchButtons[i] != null)
                videoWatchButtons[i].interactable = !cycleDone && i == unlocked;
        }
    }

    public void WatchVideo(int index)
    {
        int total = videoWatchButtons != null ? videoWatchButtons.Length : 0;
        if (!StoreVideoData.IsUnlocked(index, total))
            return;

        int reward = GetVideoReward(index);

        if (adsController.instance != null && adsController.instance.adsManager != null)
        {
            adsController.instance.adsManager.ShowRewardedWithLoading(
                () => OnStoreVideoWatched(reward, total),
                false);
            return;
        }

        OnStoreVideoWatched(reward, total);
    }

    void OnStoreVideoWatched(int reward, int total)
    {
        if (rewardPopup != null)
        {
            rewardPopup.ShowDoubleOrSkip(reward, () =>
            {
                StoreVideoData.CompleteCurrent(total);
                RefreshVideos();
            });
            return;
        }

        CurrencyManager.AddCoins(reward);
        StoreVideoData.CompleteCurrent(total);
        RefreshVideos();
    }

    public void ClaimCoinsPack(int amount)
    {
        if (amount <= 0)
            return;

        if (rewardPopup != null)
        {
            rewardPopup.ShowDoubleOrSkip(amount, null);
            return;
        }

        CurrencyManager.AddCoins(amount);
    }

    int GetVideoReward(int index)
    {
        if (videoRewards != null && index >= 0 && index < videoRewards.Length)
            return videoRewards[index];
        return 200;
    }

    public void Close()
    {
        gameObject.SetActive(false);
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.OnStoreClosed();
    }
}
