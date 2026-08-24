using UnityEngine;
using UnityEngine.UI;

public class LevelSelectionUI : MonoBehaviour
{
    public static LevelSelectionUI instance;

    public LevelSelectionItem[] levelItems;
    public Button backButton;

    void Awake()
    {
        instance = this;
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBack);
        }
    }

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (levelItems == null)
            return;

        LevelMap.SetMenuLevelCount(levelItems.Length);

        int rewardUnlockIndex = LevelProgress.GetNextRewardUnlockIndex(levelItems.Length);
        int highestUnlocked = 0;

        for (int i = 0; i < levelItems.Length; i++)
        {
            if (levelItems[i] == null)
                continue;

            bool unlocked = LevelProgress.IsLevelUnlocked(i);
            bool completed = LevelProgress.IsLevelCompleted(i);
            bool showReward = i == rewardUnlockIndex;
            if (unlocked && i > highestUnlocked)
                highestUnlocked = i;

            levelItems[i].Setup(i, unlocked, completed, false, showReward);
        }

        if (levelItems.Length > 0 && levelItems[highestUnlocked] != null && LevelProgress.IsLevelUnlocked(highestUnlocked))
        {
            levelItems[highestUnlocked].Setup(
                highestUnlocked,
                true,
                LevelProgress.IsLevelCompleted(highestUnlocked),
                true,
                highestUnlocked == rewardUnlockIndex);
        }
    }

    void OnBack()
    {
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.BackFromLevelSelect();
    }
}
