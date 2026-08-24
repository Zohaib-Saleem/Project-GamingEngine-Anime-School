using UnityEngine;

public static class LevelProgress
{
    const string AllCompleteKey = "AllLevelsCompletedOnce";
    const string HighestUnlockedKey = "HighestUnlockedLevel";
    const string LevelCompletePrefix = "LevelComplete_";

    public static int GetHighestUnlocked()
    {
        return PlayerPrefs.GetInt(HighestUnlockedKey, 0);
    }

    public static bool IsLevelUnlocked(int index)
    {
        if (index <= 0)
            return true;

        if (PlayerPrefs.GetInt(AllCompleteKey, 0) == 1)
            return true;

        return index <= GetHighestUnlocked();
    }

    public static bool IsLevelCompleted(int index)
    {
        return PlayerPrefs.GetInt(LevelCompletePrefix + index, 0) == 1;
    }

    public static bool AllLevelsCompletedOnce()
    {
        return PlayerPrefs.GetInt(AllCompleteKey, 0) == 1;
    }

    /// <summary>
    /// Only this locked level shows the rewarded unlock button
    /// (e.g. if 3rd level is playable → returns 4th level index).
    /// </summary>
    public static int GetNextRewardUnlockIndex(int totalLevels)
    {
        if (totalLevels <= 1 || AllLevelsCompletedOnce())
            return -1;

        int next = GetHighestUnlocked() + 1;
        if (next <= 0)
            next = 1;

        if (next >= totalLevels)
            return -1;

        if (IsLevelUnlocked(next))
            return -1;

        return next;
    }

    public static void UnlockLevelFromReward(int index, int totalLevels)
    {
        if (index <= 0 || index >= totalLevels)
            return;

        if (IsLevelUnlocked(index))
            return;

        int highest = GetHighestUnlocked();
        if (index > highest)
            PlayerPrefs.SetInt(HighestUnlockedKey, index);

        if (AreAllLevelsComplete(totalLevels))
            PlayerPrefs.SetInt(AllCompleteKey, 1);

        PlayerPrefs.Save();
    }

    public static void MarkLevelComplete(int completedIndex, int totalLevels)
    {
        PlayerPrefs.SetInt(LevelCompletePrefix + completedIndex, 1);

        int highest = GetHighestUnlocked();
        int nextUnlock = completedIndex + 1;
        if (nextUnlock > highest && nextUnlock < totalLevels)
            PlayerPrefs.SetInt(HighestUnlockedKey, nextUnlock);

        if (AreAllLevelsComplete(totalLevels))
            PlayerPrefs.SetInt(AllCompleteKey, 1);

        PlayerPrefs.Save();
    }

    static bool AreAllLevelsComplete(int totalLevels)
    {
        for (int i = 0; i < totalLevels; i++)
        {
            if (!IsLevelCompleted(i))
                return false;
        }
        return totalLevels > 0;
    }
}
