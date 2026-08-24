using UnityEngine;
using UnityEngine.UI;

public class StoreCoinItem : MonoBehaviour
{
    public int coinAmount = 50000;
    public Text amountText;
    public Button claimButton;

    StoreUI owner;

    public void Bind(StoreUI store)
    {
        owner = store;
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
        if (owner != null)
            owner.ClaimCoinsPack(coinAmount);
    }
}
