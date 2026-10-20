using UnityEngine;
using UnityEngine.UI;

public class DailyRewardUI : MonoBehaviour
{
    public static DailyRewardUI instance;

    public DailyRewardItem[] dayItems;
    public DailyRewardItem daySevenItem;
    public int[] rewardAmounts = { 200, 400, 600, 800, 1000, 1200, 3000 };
    public Button closeButton;
    public Button closeButton2;
    public Text rewardStatusText;
    public RewardPopupUI claimPopup;

    void Awake()
    {
        instance = this;

        DailyRewardItem[] allItems = GetAllItems();
        for (int i = 0; i < allItems.Length; i++)
        {
            if (allItems[i] != null)
                allItems[i].Bind(this);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
        if (closeButton2 != null)
        {
            closeButton2.onClick.RemoveAllListeners();
            closeButton2.onClick.AddListener(Close);
        }
    }

    void OnEnable()
    {
        Refresh();
    }

    void Update()
    {
        if (rewardStatusText == null)
            return;

        if (DailyRewardData.CanClaimToday())
        {
            rewardStatusText.text = "REWARD AVAILABLE!";
        }
        else
        {
            System.TimeSpan remaining = DailyRewardData.TimeUntilNextReward();
            rewardStatusText.text = "NEXT REWARD IN : " +
                remaining.Hours.ToString("00") + ":" +
                remaining.Minutes.ToString("00") + ":" +
                remaining.Seconds.ToString("00");
        }
    }

    public void Refresh()
    {
        DailyRewardItem[] allItems = GetAllItems();
        for (int i = 0; i < allItems.Length; i++)
        {
            if (allItems[i] == null)
                continue;

            int amount = GetRewardAmount(i);
            allItems[i].Refresh(amount);
        }

        UpdateCloseButtonsState();
    }

    public void TryClaimDay(int dayIndex)
    {
        if (DailyRewardData.GetDayState(dayIndex) != DailyDayState.Available)
            return;

        // Use day slot amount directly — more reliable than claim-state lookup.
        int amount = GetRewardAmount(dayIndex);
        if (amount <= 0)
            amount = DailyRewardData.GetClaimableAmount(rewardAmounts);
        if (amount <= 0)
            return;

        DailyRewardData.CommitClaim(rewardAmounts);
        AnalyticsEvents.Log("BF_MainMenu_DailyRewardDay" + (dayIndex + 1) + "_Claim");
        Refresh();

        if (claimPopup != null)
        {
            claimPopup.analyticsScreen = "DailyReward";
            claimPopup.ShowDoubleOrSkipDailyReward(amount, null);
            return;
        }

        CurrencyManager.AddCoins(amount);
    }

    public void Close()
    {
        if (DailyRewardData.CanClaimToday())
            return;

        AnalyticsEvents.Log("BF_MainMenu_DailyReward_Close");
        gameObject.SetActive(false);
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.OnDailyRewardClosed();
    }

    void UpdateCloseButtonsState()
    {
        bool canClose = !DailyRewardData.CanClaimToday();
        if (closeButton != null)
            closeButton.gameObject.SetActive(canClose);
        if (closeButton2 != null)
            closeButton2.gameObject.SetActive(canClose);
    }

    DailyRewardItem[] GetAllItems()
    {
        if (daySevenItem != null && (dayItems == null || dayItems.Length < 7))
        {
            DailyRewardItem[] merged = new DailyRewardItem[7];
            for (int i = 0; i < 6 && dayItems != null && i < dayItems.Length; i++)
                merged[i] = dayItems[i];
            merged[6] = daySevenItem;
            return merged;
        }

        return dayItems;
    }

    int GetRewardAmount(int index)
    {
        if (rewardAmounts == null || rewardAmounts.Length == 0)
            rewardAmounts = DailyRewardData.DefaultRewards;

        index = Mathf.Clamp(index, 0, rewardAmounts.Length - 1);
        return rewardAmounts[index];
    }
}
