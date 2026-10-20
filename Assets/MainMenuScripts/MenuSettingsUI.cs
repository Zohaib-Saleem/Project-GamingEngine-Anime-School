using UnityEngine;
using UnityEngine.UI;

public class MenuSettingsUI : MonoBehaviour
{
    public Slider soundSlider;
    public Button saveButton;
    public Button gfxLowButton;
    public Button gfxMediumButton;
    public Button gfxHighButton;
    public GameObject gfxLowHighlight;
    public GameObject gfxMediumHighlight;
    public GameObject gfxHighHighlight;

    void Awake()
    {
        GameSettings.Load(soundSlider);

        if (soundSlider != null)
        {
            soundSlider.onValueChanged.RemoveAllListeners();
            soundSlider.onValueChanged.AddListener(GameSettings.SetVolume);
        }
        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(SaveAndClose);
        }
        if (gfxLowButton != null)
        {
            gfxLowButton.onClick.RemoveAllListeners();
            gfxLowButton.onClick.AddListener(() =>
            {
                AnalyticsEvents.Log("BF_Settings_GfxLow_Select");
                GameSettings.SetGraphics(0, gfxLowHighlight, gfxMediumHighlight, gfxHighHighlight);
            });
        }
        if (gfxMediumButton != null)
        {
            gfxMediumButton.onClick.RemoveAllListeners();
            gfxMediumButton.onClick.AddListener(() =>
            {
                AnalyticsEvents.Log("BF_Settings_GfxMedium_Select");
                GameSettings.SetGraphics(1, gfxLowHighlight, gfxMediumHighlight, gfxHighHighlight);
            });
        }
        if (gfxHighButton != null)
        {
            gfxHighButton.onClick.RemoveAllListeners();
            gfxHighButton.onClick.AddListener(() =>
            {
                AnalyticsEvents.Log("BF_Settings_GfxHigh_Select");
                GameSettings.SetGraphics(2, gfxLowHighlight, gfxMediumHighlight, gfxHighHighlight);
            });
        }
    }

    void OnEnable()
    {
        GameSettings.Load(soundSlider);
        int gfx = PlayerPrefs.GetInt(GameSettings.GfxKey, 1);
        GameSettings.SetGraphics(gfx, gfxLowHighlight, gfxMediumHighlight, gfxHighHighlight);
    }

    void SaveAndClose()
    {
        AnalyticsEvents.Log("BF_Settings_Save_Click");
        PlayerPrefs.Save();
        if (MainMenuManager.instance != null)
            MainMenuManager.instance.CloseSettings();
    }
}
