using UnityEngine;

/// <summary>
/// Maps Level Select slot (e.g. 15) → Gameplay mission slot (e.g. 5).
/// Select 0 → Gameplay 0 (first mission only here).
/// Select 1..4 → Gameplay 1..4.
/// Select 5+ → cycles Gameplay 1..4 (never Gameplay 0 again).
/// </summary>
public static class LevelMap
{
    const string MenuCountKey = "MenuLevelCount";

    public static void SetMenuLevelCount(int count)
    {
        PlayerPrefs.SetInt(MenuCountKey, Mathf.Max(1, count));
        PlayerPrefs.Save();
    }

    public static int GetMenuLevelCount(int fallback = 15)
    {
        return Mathf.Max(1, PlayerPrefs.GetInt(MenuCountKey, fallback));
    }

    public static int ToGameplayIndex(int selectionIndex, int gameplayCount)
    {
        if (gameplayCount <= 0)
            return 0;

        selectionIndex = Mathf.Max(0, selectionIndex);

        // First N select slots match gameplay 0..N-1 in order
        if (selectionIndex < gameplayCount)
            return selectionIndex;

        // Beyond that: only recycle gameplay 1..N-1 (skip first)
        if (gameplayCount == 1)
            return 0;

        int recycleCount = gameplayCount - 1;
        return ((selectionIndex - gameplayCount) % recycleCount) + 1;
    }
}
