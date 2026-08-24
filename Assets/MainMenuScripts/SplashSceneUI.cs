using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SplashSceneUI : MonoBehaviour
{
    [Header("Next Scene")]
    public string mainMenuSceneName = "MainMenu";
    public float splashTime = 2.5f;
    public float fakeLoadTime = 1.5f;

    [Header("Optional UI")]
    public GameObject splashContent;
    public GameObject fakeLoading;
    public Slider loadingBar;
    public Text loadingText;

    void Start()
    {
        GameSettings.Load();

        if (fakeLoading != null)
            fakeLoading.SetActive(false);

        if (splashContent != null)
            splashContent.SetActive(true);

        StartCoroutine(SplashRoutine());
    }

    IEnumerator SplashRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.2f, splashTime));

        if (adsController.instance != null && adsController.instance.adsManager != null)
            adsController.instance.adsManager.ShowAdMobAppOpenAtStart();
        else if (AdsManager.Instance != null)
            AdsManager.Instance.ShowAdMobAppOpenAtStart();

        if (fakeLoading != null)
            fakeLoading.SetActive(true);

        float wait = Mathf.Max(0.2f, fakeLoadTime);
        float t = 0f;
        while (t < wait)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / wait);

            if (loadingBar != null)
                loadingBar.value = p;
            if (loadingText != null)
                loadingText.text = Mathf.RoundToInt(p * 100f) + "%";

            yield return null;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
