using UnityEngine;
using UnityEngine.UI;

public class DailyRewardItem : MonoBehaviour
{
    public int dayIndex;
    public Text rewardText;
    public Text statusText;
    public Button collectButton;
    public GameObject collectedState;
    public GameObject availableState;
    public GameObject lockedState;
    public GameObject highlight;

    [Header("Popup Animation")]
    public float pulseSpeed = 2f;
    public float pulseMin = 0.92f;
    public float pulseMax = 1.08f;

    DailyRewardUI owner;
    bool animating;
    RectTransform rt;

    public void Bind(DailyRewardUI panel)
    {
        owner = panel;
        if (collectButton != null)
        {
            collectButton.onClick.RemoveAllListeners();
            collectButton.onClick.AddListener(OnCollect);
        }
    }

    public void Refresh(int rewardAmount)
    {
        if (rewardText != null)
            rewardText.text = rewardAmount.ToString();

        DailyDayState state = DailyRewardData.GetDayState(dayIndex);

        if (collectedState != null)
            collectedState.SetActive(state == DailyDayState.Collected);
        if (availableState != null)
            availableState.SetActive(state == DailyDayState.Available);
        if (lockedState != null)
            lockedState.SetActive(state == DailyDayState.Locked);
        if (highlight != null)
            highlight.SetActive(state == DailyDayState.Available);

        if (collectButton != null)
            collectButton.interactable = state == DailyDayState.Available;

        if (statusText != null)
        {
            if (state == DailyDayState.Collected)
                statusText.text = "COLLECTED";
            else if (state == DailyDayState.Available)
                statusText.text = "COLLECT";
            else
            {
                int daysLeft = DailyRewardData.GetDaysLeft(dayIndex);
                statusText.text = daysLeft == 1 ? "1 DAY LEFT" : daysLeft + " DAYS LEFT";
            }
        }

        SetPulse(state == DailyDayState.Available);
    }

    void SetPulse(bool active)
    {
        animating = active;
        if (!animating)
        {
            transform.localScale = Vector3.one;
        }
        else
        {
            if (rt == null)
                rt = GetComponent<RectTransform>();
        }
    }

    void Update()
    {
        if (!animating)
            return;

        float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float s = Mathf.Lerp(pulseMin, pulseMax, t);
        transform.localScale = new Vector3(s, s, 1f);
    }

    void OnCollect()
    {
        if (owner != null)
            owner.TryClaimDay(dayIndex);
    }
}
