using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Image_Fader : MonoBehaviour
{
    public Image targetImage;
    public float defaultFadeTime = 0.5f;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        // Start hidden
        targetImage.enabled = false;
        SetAlpha(0f);
    }

    public void FadeIn()
    {
        FadeIn(defaultFadeTime);
    }

    public void FadeOut()
    {
        FadeOut(defaultFadeTime);
    }

    public void FadeIn(float duration)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        gameObject.SetActive(true);
        if (targetImage == null)
            targetImage = GetComponent<Image>();
        targetImage.enabled = true;

        fadeRoutine = StartCoroutine(FadeRoutine(1f, duration, false));
    }

    public void FadeOut(float duration)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (targetImage.color.a <= 0.01f)
        {
            HideImmediate();
            return;
        }

        targetImage.enabled = true;
        fadeRoutine = StartCoroutine(FadeRoutine(0f, duration, true));
    }

    public void HideImmediate()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = null;
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        SetAlpha(0f);
        if (targetImage != null)
            targetImage.enabled = false;
        gameObject.SetActive(false);
    }

    public void SetAlpha(float alpha)
    {
        Color c = targetImage.color;
        c.a = Mathf.Clamp01(alpha);
        targetImage.color = c;
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, bool disableAfter)
    {
        Color color = targetImage.color;
        float startAlpha = color.a;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            color.a = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            targetImage.color = color;

            yield return null;
        }

        color.a = targetAlpha;
        targetImage.color = color;

        if (disableAfter && Mathf.Approximately(targetAlpha, 0f))
            targetImage.enabled = false;

        fadeRoutine = null;
    }
}