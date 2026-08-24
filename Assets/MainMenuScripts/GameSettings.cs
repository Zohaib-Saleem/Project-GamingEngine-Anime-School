using UnityEngine;
using UnityEngine.UI;

public static class GameSettings
{
    public const string VolumeKey = "GameVolume";
    public const string GfxKey = "GraphicQuality";

    /// <summary>Devices at or below this RAM (MB) never get shadows, even on High.</summary>
    public const int NoShadowsMaxRamMb = 4096;

    public static void Load(Slider soundSlider = null)
    {
        float volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        AudioListener.volume = volume;
        if (soundSlider != null)
            soundSlider.SetValueWithoutNotify(volume);

        ApplyGraphics(PlayerPrefs.GetInt(GfxKey, 1), null, null, null, false);
    }

    public static void SetVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
    }

    public static void SetGraphics(int quality, GameObject lowHighlight, GameObject mediumHighlight, GameObject highHighlight)
    {
        ApplyGraphics(quality, lowHighlight, mediumHighlight, highHighlight, true);
    }

    static void ApplyGraphics(int quality, GameObject lowHighlight, GameObject mediumHighlight, GameObject highHighlight, bool save)
    {
        quality = Mathf.Clamp(quality, 0, 2);
        int max = Mathf.Max(0, QualitySettings.names.Length - 1);
        int level = quality == 0 ? 0 : (quality == 2 ? max : Mathf.Clamp(max / 2, 0, max));
        QualitySettings.SetQualityLevel(level, true);

        // Low-RAM devices: always kill shadows after quality apply (High included)
        EnforceNoShadowsOnLowRam();

        if (lowHighlight != null)
            lowHighlight.SetActive(quality == 0);
        if (mediumHighlight != null)
            mediumHighlight.SetActive(quality == 1);
        if (highHighlight != null)
            highHighlight.SetActive(quality == 2);

        if (save)
        {
            PlayerPrefs.SetInt(GfxKey, quality);
            PlayerPrefs.Save();
        }
    }

    public static bool IsLowRamDevice()
    {
        return SystemInfo.systemMemorySize <= NoShadowsMaxRamMb;
    }

    public static void EnforceNoShadowsOnLowRam()
    {
        if (!IsLowRamDevice())
            return;

        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.shadowDistance = 0f;
    }
}
