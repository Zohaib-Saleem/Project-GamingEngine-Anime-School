using System;
using System.Globalization;
using UnityEngine;

public static class DailyRewardData
{
    const string LastClaimKey = "DailyReward_LastClaimDate";
    const string NextDayKey = "DailyReward_NextDayIndex";

    public static readonly int[] DefaultRewards = { 200, 400, 600, 800, 1000, 1200, 3000 };

    public static void NormalizeStreak()
    {
        if (!TryGetLastClaimDate(out DateTime lastClaim))
            return;

        int gap = (DateTime.Today - lastClaim.Date).Days;
        if (gap > 1)
        {
            PlayerPrefs.SetInt(NextDayKey, 0);
            PlayerPrefs.Save();
        }
    }

    public static bool CanClaimToday()
    {
        NormalizeStreak();

        if (!TryGetLastClaimDate(out DateTime lastClaim))
            return true;

        return lastClaim.Date != DateTime.Today;
    }

    public static int GetNextDayIndex()
    {
        NormalizeStreak();
        return Mathf.Clamp(PlayerPrefs.GetInt(NextDayKey, 0), 0, DefaultRewards.Length - 1);
    }

    public static DailyDayState GetDayState(int dayIndex)
    {
        int next = GetNextDayIndex();
        bool canClaim = CanClaimToday();

        if (dayIndex < next)
            return DailyDayState.Collected;

        if (dayIndex == next && canClaim)
            return DailyDayState.Available;

        return DailyDayState.Locked;
    }

    public static int GetDaysLeft(int dayIndex)
    {
        int next = GetNextDayIndex();
        bool canClaim = CanClaimToday();
        int wait = dayIndex - next;
        if (!canClaim)
            wait += 1;
        return Mathf.Max(0, wait);
    }

    public static int GetClaimableAmount(int[] rewards)
    {
        if (!CanClaimToday())
            return 0;

        if (rewards == null || rewards.Length == 0)
            rewards = DefaultRewards;

        int day = GetNextDayIndex();
        return rewards[Mathf.Clamp(day, 0, rewards.Length - 1)];
    }

    public static int CommitClaim(int[] rewards)
    {
        if (!CanClaimToday())
            return 0;

        int amount = GetClaimableAmount(rewards);
        if (amount <= 0)
            return 0;

        PlayerPrefs.SetString(LastClaimKey, DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        PlayerPrefs.SetInt(NextDayKey, (GetNextDayIndex() + 1) % rewards.Length);
        PlayerPrefs.Save();
        return amount;
    }

    public static int Claim(int[] rewards)
    {
        int amount = CommitClaim(rewards);
        if (amount > 0)
            CurrencyManager.AddCoins(amount);
        return amount;
    }

    public static TimeSpan TimeUntilNextReward()
    {
        DateTime nextMidnight = DateTime.Today.AddDays(1);
        TimeSpan remaining = nextMidnight - DateTime.Now;
        if (remaining.TotalSeconds < 0)
            remaining = TimeSpan.Zero;
        return remaining;
    }

    static bool TryGetLastClaimDate(out DateTime date)
    {
        string raw = PlayerPrefs.GetString(LastClaimKey, string.Empty);
        if (string.IsNullOrEmpty(raw))
        {
            date = default;
            return false;
        }

        return DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}

public enum DailyDayState
{
    Collected,
    Available,
    Locked
}
