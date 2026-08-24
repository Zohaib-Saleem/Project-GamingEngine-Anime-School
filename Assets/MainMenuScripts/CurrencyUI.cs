using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drop this on any cash bar. It always shows the latest total coins.
/// You can place multiple copies (main menu, store, etc.) — all stay in sync.
/// </summary>
public class CurrencyUI : MonoBehaviour
{
    [Tooltip("Leave empty to auto-find Text on this object or children.")]
    public Text coinText;

    public string prefix = "$ ";
    public bool useThousandsSeparator = true;

    [Tooltip("Only applied once on first ever launch.")]
    public int startingCoins = 10000;

    void Awake()
    {
        if (coinText == null)
            coinText = GetComponent<Text>();
        if (coinText == null)
            coinText = GetComponentInChildren<Text>(true);

        CurrencyManager.EnsureStartingBalance(startingCoins);
    }

    void OnEnable()
    {
        CurrencyManager.OnCoinsChanged += Refresh;
        Refresh(CurrencyManager.Coins);
    }

    void OnDisable()
    {
        CurrencyManager.OnCoinsChanged -= Refresh;
    }

    void Refresh(int amount)
    {
        if (coinText == null)
            return;

        string number = useThousandsSeparator ? amount.ToString("N0") : amount.ToString();
        coinText.text = prefix + number;
    }
}
