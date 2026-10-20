using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionItem : MonoBehaviour
{
    public int levelIndex;
    public Button playButton;
    public Button rewardUnlockButton;
    public GameObject lockedOverlay;
    public GameObject completedMark;
    public GameObject selectedHighlight;

    public void Setup(int index, bool unlocked, bool completed, bool selected, bool showRewardUnlock)
    {
        levelIndex = index;

        if (lockedOverlay != null)
            lockedOverlay.SetActive(!unlocked);
        if (completedMark != null)
            completedMark.SetActive(completed);
        if (selectedHighlight != null)
            selectedHighlight.SetActive(selected && unlocked);

        if (playButton != null)
        {
            playButton.interactable = unlocked;
            playButton.onClick.RemoveAllListeners();
            if (unlocked)
                playButton.onClick.AddListener(OnPlay);
        }

        if (rewardUnlockButton != null)
        {
            rewardUnlockButton.gameObject.SetActive(showRewardUnlock);
            rewardUnlockButton.onClick.RemoveAllListeners();
            if (showRewardUnlock)
                rewardUnlockButton.onClick.AddListener(OnRewardUnlock);
        }
    }

    void OnPlay()
    {
        AnalyticsEvents.Log("BF_LevelSelection_Level" + (levelIndex + 1).ToString("00") + "_Start");
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.LoadGameplay(levelIndex);
    }

    void OnRewardUnlock()
    {
        if (adsController.instance != null)
        {
            adsController.instance.ShowRewarded(
                "AF_LevelSelection_UnlockLevel" + (levelIndex + 1).ToString("00"),
                OnRewardSuccess,
                false);
            return;
        }

        OnRewardSuccess();
    }

    void OnRewardSuccess()
    {
        int total = LevelSelectionUI.instance != null && LevelSelectionUI.instance.levelItems != null
            ? LevelSelectionUI.instance.levelItems.Length
            : LevelMap.GetMenuLevelCount();

        LevelProgress.UnlockLevelFromReward(levelIndex, total);

        if (LevelSelectionUI.instance != null)
            LevelSelectionUI.instance.Refresh();
    }
}
