using UnityEngine;

public static class StoreVideoData
{
    const string NextIndexKey = "StoreVideo_NextIndex";

    public static int GetNextUnlockedIndex()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(NextIndexKey, 0));
    }

    public static bool IsUnlocked(int index, int totalVideos)
    {
        if (totalVideos <= 0)
            return false;
        if (AllCompleted(totalVideos))
            return false;
        return index == GetNextUnlockedIndex();
    }

    public static bool IsUnlocked(int index)
    {
        return index == GetNextUnlockedIndex();
    }

    public static void CompleteCurrent(int totalVideos)
    {
        if (totalVideos <= 0)
            return;

        int next = GetNextUnlockedIndex() + 1;
        if (next >= totalVideos)
            next = totalVideos; // all finished this cycle

        PlayerPrefs.SetInt(NextIndexKey, next);
        PlayerPrefs.Save();
    }

    public static bool AllCompleted(int totalVideos)
    {
        return totalVideos > 0 && GetNextUnlockedIndex() >= totalVideos;
    }

    public static void ResetCycle()
    {
        PlayerPrefs.SetInt(NextIndexKey, 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Call when store/scene opens again. If last cycle finished, unlock Video 1 again.
    /// </summary>
    public static void PrepareForSession(int totalVideos)
    {
        if (AllCompleted(totalVideos))
            ResetCycle();
    }
}
