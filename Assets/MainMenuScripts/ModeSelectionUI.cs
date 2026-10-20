using UnityEngine;
using UnityEngine.UI;

public class ModeSelectionUI : MonoBehaviour
{
    public Button openWorldButton;
    public Button careerButton;
    public Button backButton;

    void Awake()
    {
        if (openWorldButton != null)
        {
            openWorldButton.onClick.RemoveAllListeners();
            openWorldButton.onClick.AddListener(OnOpenWorld);
        }
        if (careerButton != null)
            careerButton.interactable = false;
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBack);
        }
    }

    void OnOpenWorld()
    {
        AnalyticsEvents.Log("BF_ModeSelection_OpenWorld_Click");
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.OpenLevelSelect();
    }

    void OnBack()
    {
        AnalyticsEvents.Log("BF_ModeSelection_Back_Click");
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.BackFromModeSelect();
    }
}
