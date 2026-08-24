using System;
using UnityEngine;

public static class CurrencyManager
{
    const string CoinsKey = "PlayerCoins";
    const string InitializedKey = "PlayerCoinsInitialized";

    public static event Action<int> OnCoinsChanged;

    public static int Coins => PlayerPrefs.GetInt(CoinsKey, 0);

    public static void EnsureStartingBalance(int startingCoins)
    {
        if (PlayerPrefs.GetInt(InitializedKey, 0) == 1)
            return;

        PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, startingCoins));
        PlayerPrefs.SetInt(InitializedKey, 1);
        PlayerPrefs.Save();
        OnCoinsChanged?.Invoke(Coins);
    }

    public static void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        PlayerPrefs.SetInt(CoinsKey, Coins + amount);
        PlayerPrefs.Save();
        OnCoinsChanged?.Invoke(Coins);
    }

    public static bool SpendCoins(int amount)
    {
        if (amount <= 0 || Coins < amount)
            return false;

        PlayerPrefs.SetInt(CoinsKey, Coins - amount);
        PlayerPrefs.Save();
        OnCoinsChanged?.Invoke(Coins);
        return true;
    }
}
