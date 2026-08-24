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

        // Optional old popup flow stays disabled when in-panel reward buttons are set.
        if (showLevelRewardPopup && levelRewardPopup != null && rewardButtonsRoot == null)
            levelRewardPopup.ShowDoubleOrSkip(currentTotalReward, null);
    }

    public void OnClick2xReward()
    {
        if (rewardDecisionDone)
            return;

        if (adsController.instance != null && adsController.instance.adsManager != null)
        {
            adsController.instance.adsManager.ShowRewardedWithLoading(Grant2xReward, false);
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
        CurrencyManager.AddCoins(currentTotalReward);

        // Next buttons panel first, then interstitial
        ShowNextButtons();
        if (adsController.instance != null)
            adsController.instance.ShowInter();
    }

    void ShowNextButtons()
    {
        if (rewardButtonsRoot != null)
            rewardButtonsRoot.SetActive(false);
        if (nextButtonsRoot != null)
            nextButtonsRoot.SetActive(true);
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
    }
}
