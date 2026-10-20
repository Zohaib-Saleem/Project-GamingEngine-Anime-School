using UnityEngine;
using UnityEngine.UI;

public class MenuExitUI : MonoBehaviour
{
    public Button yesButton;
    public Button noButton;

    void Awake()
    {
        if (yesButton != null)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(OnYes);
        }
        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(OnNo);
        }
    }

    void OnYes()
    {
        AnalyticsEvents.Log("BF_MainMenu_ExitYes_Click");
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.ConfirmExitGame();
    }

    void OnNo()
    {
        AnalyticsEvents.Log("BF_MainMenu_ExitNo_Click");
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.CloseExitPanel();
    }
}
