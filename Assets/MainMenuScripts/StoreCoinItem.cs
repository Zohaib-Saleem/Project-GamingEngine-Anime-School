using UnityEngine;
using UnityEngine.UI;

public class StoreCoinItem : MonoBehaviour
{
    public int coinAmount = 50000;
    public Text amountText;
    public Button claimButton;

    StoreUI owner;
    int packIndex;

    public void Bind(StoreUI store, int index)
    {
        owner = store;
        packIndex = index;
        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaim);
        }

        if (amountText != null)
            amountText.text = coinAmount.ToString("N0");
    }

    void OnClaim()
    {
        AnalyticsEvents.Log("BF_Store_CoinPack" + (packIndex + 1) + "_Claim");
        if (owner != null)
            owner.ClaimCoinsPack(coinAmount);
    }
}
