using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using KikiNgao.SimpleBikeControl;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu instance;

    public GameObject pausePanel;
    public GameObject fakeLoading;
    public Slider soundSlider;
    public Button resumeButton;
    public Button restartButton;
    public Button homeButton;
    public Button gfxLowButton;
    public Button gfxMediumButton;
    public Button gfxHighButton;
    public GameObject gfxLowHighlight;
    public GameObject gfxMediumHighlight;
    public GameObject gfxHighHighlight;
    public float fakeLoadTime = 2f;
    public string mainMenuSceneName = "MainMenu";

    bool paused;
    public bool IsPaused => paused;

    private void Awake()
    {
        instance = this;
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (fakeLoading != null)
            fakeLoading.SetActive(false);

        BindButtons();
        LoadSettings();
    }

    void BindButtons()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(() =>
            {
                AnalyticsEvents.Log("BF_GamePlay_Resume_Click");
                Resume();
            });
        }
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() =>
            {
                AnalyticsEvents.Log("BF_GamePlay_Restart_Click");
                Restart();
            });
        }
        if (homeButton != null)
        {
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(() =>
            {
                AnalyticsEvents.Log("BF_GamePlay_Home_Click");
                Home();
            });
        }
        if (gfxLowButton != null)
        {
            gfxLowButton.onClick.RemoveAllListeners();
            gfxLowButton.onClick.AddListener(() =>
            {
                AnalyticsEvents.Log("BF_GamePlay_GfxLow_Select");
                GameSettings.SetGraphics(0, gfxLowHighlight, gfxMediumHighlight, gfxHighHighlight);
            });
        }
        if (gfxMediumButton != null)
        {
            gfxMediumButton.onClick.RemoveAllListeners();
            gfxMediumButton.onClick.AddListener(() =>
            {
                AnalyticsEvents.Log("BF_GamePlay_GfxMedium_Select");
                GameSettings.SetGraphics(1, gfxLowHighlight, gfxMediumHighlight, gfxHighHighlight);
            });
        }
        if (gfxHighButton != null)
        {
            gfxHighButton.onClick.RemoveAllListeners();
            gfxHighButton.onClick.AddListener(() =>
            {
                AnalyticsEvents.Log("BF_GamePlay_GfxHigh_Select");
                GameSettings.SetGraphics(2, gfxLowHighlight, gfxMediumHighlight, gfxHighHighlight);
            });
        }
        if (soundSlider != null)
        {
            soundSlider.onValueChanged.RemoveAllListeners();
            soundSlider.onValueChanged.AddListener(GameSettings.SetVolume);
        }
    }

    void LoadSettings()
    {
        GameSettings.Load(soundSlider);
        int gfx = PlayerPrefs.GetInt(GameSettings.GfxKey, 1);
        GameSettings.SetGraphics(gfx, gfxLowHighlight, gfxMediumHighlight, gfxHighHighlight);
    }

    public void Pause()
    {
        // Already paused — just keep panel visible
        if (paused)
        {
            if (pausePanel != null)
                pausePanel.SetActive(true);
            Time.timeScale = 0f;
            return;
        }

        // Scene fake-loading in progress — don't open pause
        if (fakeLoading != null && fakeLoading.activeSelf)
            return;

        AnalyticsEvents.Log("BF_GamePlay_Pause_Click");
        paused = true;
        if (pausePanel != null)
            pausePanel.SetActive(true);

        // Show interstitial while timeScale is still 1 so DOTween loading can finish,
        // then freeze gameplay. Pause panel stays on top after.
        StartCoroutine(PauseAdThenFreeze());
    }

    IEnumerator PauseAdThenFreeze()
    {
        if (adsController.instance != null)
            adsController.instance.ShowPauseButtonInter();

        // Let ad loading coroutine/tween start with normal timeScale
        yield return new WaitForSecondsRealtime(0.15f);

        if (!paused)
            yield break;

        Time.timeScale = 0f;
        AudioListener.pause = false;
        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void Resume()
    {
        paused = false;
        Time.timeScale = 1f;
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    public void Restart()
    {
        int current = PlayerPrefs.GetInt("SelectedLevel", 0);
        if (Level_Handler.instance != null)
            current = Level_Handler.instance.selectionLevelNo;
        ReloadScene(current);
    }

    public void Home()
    {
        StopAllCoroutines();
        StartCoroutine(HomeRoutine());
    }

    public void ReloadFromFirstLevel()
    {
        ReloadScene(0);
    }

    public void ReloadScene(int startLevel)
    {
        StopAllCoroutines();
        StartCoroutine(ReloadRoutine(startLevel));
    }

    IEnumerator ReloadRoutine(int startLevel)
    {
        paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (Result_Manager.instance != null)
            Result_Manager.instance.HideCompleteUI();

        PlayerPrefs.SetInt("SelectedLevel", Mathf.Max(0, startLevel));
        PlayerPrefs.Save();

        if (fakeLoading != null)
            fakeLoading.SetActive(true);

        yield return new WaitForSecondsRealtime(Mathf.Max(0.35f, fakeLoadTime));

        if (GameManager.Instance != null)
            Destroy(GameManager.Instance.gameObject);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator HomeRoutine()
    {
        paused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (Result_Manager.instance != null)
            Result_Manager.instance.HideCompleteUI();

        if (fakeLoading != null)
            fakeLoading.SetActive(true);

        yield return new WaitForSecondsRealtime(Mathf.Max(0.35f, fakeLoadTime));

        if (GameManager.Instance != null)
            Destroy(GameManager.Instance.gameObject);

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
