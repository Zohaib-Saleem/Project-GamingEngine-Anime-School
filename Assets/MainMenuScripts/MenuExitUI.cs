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
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.ConfirmExitGame();
    }

    void OnNo()
    {
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.CloseExitPanel();
    }
}
