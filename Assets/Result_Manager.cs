using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Result_Manager : MonoBehaviour
{
    public static Result_Manager instance;
    public GameObject LvlCmpltBar, LvlFailBar;
    public GameObject LevelCompletePanel, LevelFailPanel,BonusPanel,DoubleBonus;
    [Header("Level Complete Reward Flow")]
    public GameObject rewardButtonsRoot;
    public GameObject nextButtonsRoot;
    public Button x2RewardButton;
    public Button skip2xButton;
    public RewardPopupUI levelRewardPopup;
    public bool showLevelRewardPopup = true;
    public int timeBonusAmount = 500;
    [Header("Home Button")]
    [Tooltip("Off = home buttons on complete/fail panels are hidden for this build")]
    public bool showHomeButton = false;
    [Tooltip("Optional, if empty any child named \"Home\" under the panels is hidden")]
    public GameObject completeHomeButton, failHomeButton;
    public bool Completed, failed;
    public Text LevelReward, TimeReward, TotalReward;
    public int RewardAmount;
    public AnimeSchoolManager school_manager;
    int currentTotalReward;
    bool rewardDecisionDone;

    private void Awake()
    {
        instance = this;
        if (x2RewardButton != null)
        {
            x2RewardButton.onClick.RemoveAllListeners();
            x2RewardButton.onClick.AddListener(OnClick2xReward);
        }
        if (skip2xButton != null)
        {
            skip2xButton.onClick.RemoveAllListeners();
            skip2xButton.onClick.AddListener(OnClickSkip2x);
        }
    }

    public void LevelComplete()
    {
        if (Completed)
            return;

        Completed = true;
        CancelInvoke(nameof(GoForCompletion));
        if (Level_Handler.instance != null)
            Level_Handler.instance.LogMissionEvent("Complete");

        if (school_manager != null && school_manager.mainChUI != null)
            school_manager.mainChUI.SetActive(false);

        if (LevelCompletePanel != null)
            LevelCompletePanel.SetActive(false);
        if (LvlCmpltBar != null)
            LvlCmpltBar.SetActive(true);

        Invoke(nameof(GoForCompletion), 2.0f);
    }

    public void GoForCompletion()
    {
        if (!Completed)
            return;

        if (LvlCmpltBar != null)
            LvlCmpltBar.SetActive(false);
        if (LevelCompletePanel != null)
            LevelCompletePanel.SetActive(true);

        if (LevelReward != null)
            LevelReward.text = "" + RewardAmount.ToString();
        if (TimeReward != null)
            TimeReward.text = "" + timeBonusAmount;

        currentTotalReward = RewardAmount + timeBonusAmount;
        rewardDecisionDone = false;
        UpdateTotalRewardUI(currentTotalReward);

        // First show reward decision (2x / skip), hide next/restart/home.
        if (rewardButtonsRoot != null)
            rewardButtonsRoot.SetActive(true);
        if (nextButtonsRoot != null)
            nextButtonsRoot.SetActive(false);
        ApplyHomeButtonVisibility(LevelCompletePanel, completeHomeButton);

        // Optional old popup flow stays disabled when in-panel reward buttons are set.
        if (showLevelRewardPopup && levelRewardPopup != null && rewardButtonsRoot == null)
        {
            levelRewardPopup.analyticsScreen = "LevelComplete";
            levelRewardPopup.ShowDoubleOrSkip(currentTotalReward, null);
        }
    }

    public void OnClick2xReward()
    {
        if (rewardDecisionDone)
            return;

        if (adsController.instance != null && adsController.instance.adsManager != null)
        {
            adsController.instance.ShowRewarded("AF_LevelComplete_DoubleReward", Grant2xReward, false);
            return;
        }

        Grant2xReward();
    }

    void Grant2xReward()
    {
        if (rewardDecisionDone)
            return;

        rewardDecisionDone = true;
        currentTotalReward *= 2;
        UpdateTotalRewardUI(currentTotalReward);
        CurrencyManager.AddCoins(currentTotalReward);
        ShowNextButtons();
    }

    public void OnClickSkip2x()
    {
        if (rewardDecisionDone)
            return;

        rewardDecisionDone = true;
        AnalyticsEvents.Log("BF_LevelComplete_SkipReward_Click");
        CurrencyManager.AddCoins(currentTotalReward);

        // Next buttons panel first, then interstitial
        ShowNextButtons();
        if (adsController.instance != null)
        {
            if (IsFirstMissionFromArray())
                adsController.instance.ShowFirstMissionCompleteInter();
            else
                adsController.instance.ShowInter("AF_LevelComplete_Inter_Show");
        }
    }

    bool IsFirstMissionFromArray()
    {
        if (Level_Handler.instance != null)
            return Level_Handler.instance.selectionLevelNo == 0;
        return PlayerPrefs.GetInt("SelectedLevel", 0) == 0;
    }

    void ShowNextButtons()
    {
        if (rewardButtonsRoot != null)
            rewardButtonsRoot.SetActive(false);
        if (nextButtonsRoot != null)
            nextButtonsRoot.SetActive(true);
        ApplyHomeButtonVisibility(LevelCompletePanel, completeHomeButton);
    }

    void ApplyHomeButtonVisibility(GameObject panel, GameObject homeButton)
    {
        if (homeButton != null)
        {
            homeButton.SetActive(showHomeButton);
            return;
        }
        if (panel == null)
            return;

        // no button assigned, hide any button under the panel whose name contains "home"
        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
        {
            if (button.name.ToLower().Contains("home"))
                button.gameObject.SetActive(showHomeButton);
        }
    }

    void UpdateTotalRewardUI(int total)
    {
        if (TotalReward != null)
            TotalReward.text = "" + total.ToString();
    }

    public void HideCompleteUI()
    {
        CancelInvoke(nameof(GoForCompletion));
        CancelInvoke(nameof(GoForFail));

        Completed = false;
        failed = false;
        if (LvlCmpltBar != null)
            LvlCmpltBar.SetActive(false);
        if (LevelCompletePanel != null)
            LevelCompletePanel.SetActive(false);
        if (rewardButtonsRoot != null)
            rewardButtonsRoot.SetActive(false);
        if (nextButtonsRoot != null)
            nextButtonsRoot.SetActive(false);
        if (LvlFailBar != null)
            LvlFailBar.SetActive(false);
        if (LevelFailPanel != null)
            LevelFailPanel.SetActive(false);
        if (levelRewardPopup != null)
            levelRewardPopup.HideImmediate();
    }

    public void LevelFail()
    {
        if (failed)
            return;

        failed = true;
        CancelInvoke(nameof(GoForFail));
        if (Level_Handler.instance != null)
            Level_Handler.instance.LogMissionEvent("Fail");

        if (school_manager != null && school_manager.mainChUI != null)
            school_manager.mainChUI.SetActive(false);

        if (LevelFailPanel != null)
            LevelFailPanel.SetActive(false);
        if (LvlFailBar != null)
            LvlFailBar.SetActive(true);

        Invoke(nameof(GoForFail), 2.0f);
    }

    public void GoForFail()
    {
        if (!failed)
            return;

        if (LvlFailBar != null)
            LvlFailBar.SetActive(false);
        if (LevelFailPanel != null)
            LevelFailPanel.SetActive(true);
        ApplyHomeButtonVisibility(LevelFailPanel, failHomeButton);
    }
}
