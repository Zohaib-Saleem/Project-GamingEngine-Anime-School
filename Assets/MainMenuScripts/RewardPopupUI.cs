using System;
using UnityEngine;
using UnityEngine.UI;

public class RewardPopupUI : MonoBehaviour
{
    public enum PopupMode
    {
        SimpleCollect,
        DoubleOrSkip
    }

    public GameObject root;
    public Text rewardAmountText;
    public Button collectButton;
    public Button doubleRewardButton;
    public Button noThanksButton;

    // screen name used in analytics events, set by whoever opens the popup
    [NonSerialized] public string analyticsScreen = "MainMenu";

    PopupMode mode = PopupMode.SimpleCollect;
    int sessionAmount;
    int pendingReward;
    bool rewardGranted;
    bool waitingForCollect;
    Action onClosed;

    void Awake()
    {
        if (root == null)
            root = gameObject;

        BindButtons();

        if (root != null && root != gameObject)
            root.SetActive(false);
        gameObject.SetActive(false);
    }

    void BindButtons()
    {
        if (collectButton != null)
        {
            collectButton.onClick.RemoveAllListeners();
            collectButton.onClick.AddListener(OnCollectClicked);
        }
        if (doubleRewardButton != null)
        {
            doubleRewardButton.onClick.RemoveAllListeners();
            doubleRewardButton.onClick.AddListener(OnDoubleClicked);
        }
        if (noThanksButton != null)
        {
            noThanksButton.onClick.RemoveAllListeners();
            noThanksButton.onClick.AddListener(OnNoThanksClicked);
        }
    }

    public void ShowSimpleCollect(int amount, Action closedCallback)
    {
        Open(PopupMode.SimpleCollect, amount, false, closedCallback);
    }

    public void ShowSimpleCollectWithoutGrant(int amount, Action closedCallback)
    {
        Open(PopupMode.SimpleCollect, amount, true, closedCallback);
    }

    public void ShowDoubleOrSkip(int amount, Action closedCallback)
    {
        Open(PopupMode.DoubleOrSkip, amount, false, closedCallback);
    }

    public void ShowDoubleOrSkipWithoutGrant(int amount, Action closedCallback)
    {
        Open(PopupMode.DoubleOrSkip, amount, true, closedCallback);
    }

    public void ShowDoubleOrSkipDailyReward(int amount, Action closedCallback)
    {
        Open(PopupMode.DoubleOrSkip, amount, false, closedCallback);
    }

    void Open(PopupMode popupMode, int amount, bool alreadyGranted, Action closedCallback)
    {
        mode = popupMode;
        sessionAmount = Mathf.Max(0, amount);
        pendingReward = sessionAmount;
        rewardGranted = alreadyGranted;
        waitingForCollect = mode == PopupMode.SimpleCollect;
        onClosed = closedCallback;

        if (root != null)
            root.SetActive(true);
        gameObject.SetActive(true);

        UIClickSound.PlayRewardSound();

        ApplyButtonState();
        SetAmountText(pendingReward);
    }

    void ApplyButtonState()
    {
        bool showChoice = mode == PopupMode.DoubleOrSkip && !waitingForCollect;
        bool showCollect = mode == PopupMode.SimpleCollect || waitingForCollect;

        if (doubleRewardButton != null)
            doubleRewardButton.gameObject.SetActive(showChoice);
        if (noThanksButton != null)
            noThanksButton.gameObject.SetActive(showChoice);
        if (collectButton != null)
            collectButton.gameObject.SetActive(showCollect);
    }

    void SetAmountText(int amount)
    {
        if (rewardAmountText != null)
            rewardAmountText.text = amount.ToString();
    }

    void GoToCollectStep(bool doubled)
    {
        int src = sessionAmount > 0 ? sessionAmount : pendingReward;
        pendingReward = doubled ? src * 2 : src;
        waitingForCollect = true;

        ApplyButtonState();
        SetAmountText(pendingReward);
    }

    public void OnDoubleClicked()
    {
        if (waitingForCollect || rewardGranted)
            return;

        if (adsController.instance != null && adsController.instance.adsManager != null)
        {
            adsController.instance.ShowRewarded("AF_" + analyticsScreen + "_DoubleReward", OnRewardedAdSuccess, false);
            return;
        }

        OnRewardedAdSuccess();
    }

    void OnRewardedAdSuccess()
    {
        if (waitingForCollect || rewardGranted)
            return;

        GoToCollectStep(true);
    }

    public void OnNoThanksClicked()
    {
        if (waitingForCollect || rewardGranted)
            return;

        AnalyticsEvents.Log("BF_" + analyticsScreen + "_NoThanks_Click");
        GoToCollectStep(false);
    }

    public void OnCollectClicked()
    {
        if (!waitingForCollect && mode == PopupMode.DoubleOrSkip)
            return;

        AnalyticsEvents.Log("BF_" + analyticsScreen + "_Collect_Click");

        if (!rewardGranted)
        {
            int payout = pendingReward > 0 ? pendingReward : sessionAmount;
            CurrencyManager.AddCoins(payout);
            rewardGranted = true;
        }

        Close();
    }

    public void HideImmediate()
    {
        pendingReward = 0;
        rewardGranted = false;
        waitingForCollect = false;
        onClosed = null;

        if (root != null && root != gameObject)
            root.SetActive(false);

        gameObject.SetActive(false);
    }

    void Close()
    {
        Action callback = onClosed;
        sessionAmount = 0;
        HideImmediate();
        callback?.Invoke();
    }
}
