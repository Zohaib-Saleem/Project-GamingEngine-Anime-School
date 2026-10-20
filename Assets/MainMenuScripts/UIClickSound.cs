using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum BgMusicType
{
    None = 0,
    MainMenu = 1,
    Gameplay = 2,
    Cutscene = 3
}

/// <summary>
/// Persistent UI SFX + background music. Assign clips in Inspector (change anytime).
/// Music uses 2 AudioSources for cheap crossfade (no extra allocations per switch).
/// </summary>
public class UIClickSound : MonoBehaviour
{
    public static UIClickSound Instance { get; private set; }

    [Header("Click sound")]
    public AudioClip clickClip;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Reward panel sound")]
    [Tooltip("Played when a reward popup appears (daily / watch video / store).")]
    public AudioClip rewardClip;
    [Range(0f, 1f)] public float rewardVolume = 1f;

    [Header("Background Music")]
    public AudioClip mainMenuMusic;
    public AudioClip gameplayMusic;
    public AudioClip cutsceneMusic;
    [Range(0f, 1f)] public float musicVolume = 0.65f;
    [Min(0.05f)] public float musicFadeSeconds = 0.8f;
    public bool autoPlayByScene = true;
    public string mainMenuSceneName = "MainMenu";
    public string gameplaySceneName = "Gameplay";

    [Header("Auto hook buttons")]
    public bool autoHookButtons = true;

    AudioSource sfxSource;
    AudioSource musicA;
    AudioSource musicB;
    AudioSource musicActive;
    BgMusicType currentMusic = BgMusicType.None;
    Coroutine musicRoutine;
    readonly HashSet<int> hookedButtonIds = new HashSet<int>();
    Coroutine hookRoutine;
    int dialogueDuckCount;
    float musicGain;
    float musicGainTarget;
    float duckReleaseAt = -1f;
    const float DialogueMusicVolume = 0.1f;
    const float DuckFadeSeconds = 0.18f;
    const float DuckHoldSeconds = 0.22f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetupAudioSources();
    }

    void SetupAudioSources()
    {
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.ignoreListenerPause = true;

        musicA = CreateMusicSource("MusicA");
        musicB = CreateMusicSource("MusicB");
        musicActive = musicA;
        musicGain = musicVolume;
        musicGainTarget = musicVolume;
    }

    AudioSource CreateMusicSource(string childName)
    {
        Transform existing = transform.Find(childName);
        GameObject go = existing != null ? existing.gameObject : new GameObject(childName);
        if (existing == null)
            go.transform.SetParent(transform, false);

        AudioSource src = go.GetComponent<AudioSource>();
        if (src == null)
            src = go.AddComponent<AudioSource>();

        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f;
        src.volume = 0f;
        src.ignoreListenerPause = false;
        return src;
    }

    void Start()
    {
        ScheduleHook();
        if (autoPlayByScene)
            PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        hookedButtonIds.Clear();
        ScheduleHook();

        if (autoPlayByScene)
            PlayMusicForScene(scene.name);
    }

    void PlayMusicForScene(string sceneName)
    {
        if (sceneName == mainMenuSceneName)
            PlayBgMusic(BgMusicType.MainMenu);
        else if (sceneName == gameplaySceneName)
            PlayBgMusic(BgMusicType.Gameplay);
    }

    // -------------------------------------------------------------------------
    // SFX
    // -------------------------------------------------------------------------

    void ScheduleHook()
    {
        if (!autoHookButtons)
            return;

        if (hookRoutine != null)
            StopCoroutine(hookRoutine);
        hookRoutine = StartCoroutine(HookAfterUiBind());
    }

    IEnumerator HookAfterUiBind()
    {
        yield return null;
        HookAllButtonsInScene();
        yield return null;
        HookAllButtonsInScene();
        hookRoutine = null;
    }

    void Update()
    {
        TickDialogueDuck();
        if (!autoHookButtons || Time.frameCount % 30 != 0)
            return;

        HookAllButtonsInScene();
    }

    public static void BeginDialogueDuck()
    {
        if (Instance != null)
            Instance.PushDialogueDuck();
    }

    public static void EndDialogueDuck()
    {
        if (Instance != null)
            Instance.PopDialogueDuck();
    }

    void PushDialogueDuck()
    {
        dialogueDuckCount++;
        duckReleaseAt = -1f;
        musicGainTarget = DialogueMusicVolume;
    }

    void PopDialogueDuck()
    {
        dialogueDuckCount = Mathf.Max(0, dialogueDuckCount - 1);
        if (dialogueDuckCount == 0)
            duckReleaseAt = Time.unscaledTime + DuckHoldSeconds;
    }

    void TickDialogueDuck()
    {
        if (dialogueDuckCount == 0 && duckReleaseAt >= 0f && Time.unscaledTime >= duckReleaseAt)
        {
            musicGainTarget = musicVolume;
            duckReleaseAt = -1f;
        }

        float step = Time.unscaledDeltaTime / Mathf.Max(0.05f, DuckFadeSeconds);
        musicGain = Mathf.MoveTowards(musicGain, musicGainTarget, step);
        if (musicRoutine != null)
            return;
        if (musicActive != null && musicActive.isPlaying)
            musicActive.volume = musicGain;
    }

    public void PlayClick()
    {
        if (clickClip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clickClip, volume);
    }

    public void PlayReward()
    {
        if (rewardClip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(rewardClip, rewardVolume);
    }

    public static void Play()
    {
        if (Instance != null)
            Instance.PlayClick();
    }

    public static void PlayRewardSound()
    {
        if (Instance != null)
            Instance.PlayReward();
    }

    public void RegisterButton(Button button)
    {
        if (button == null)
            return;

        int id = button.GetInstanceID();
        if (button.GetComponent<UIClickSoundRelay>() != null)
        {
            hookedButtonIds.Add(id);
            return;
        }

        button.gameObject.AddComponent<UIClickSoundRelay>();
        hookedButtonIds.Add(id);
    }

    public void HookAllButtonsInScene()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
            RegisterButton(buttons[i]);
    }

    // -------------------------------------------------------------------------
    // Background music (fade in / out / crossfade)
    // -------------------------------------------------------------------------

    public static void PlayMusic(BgMusicType type)
    {
        if (Instance != null)
            Instance.PlayBgMusic(type);
    }

    public static void StopMusic()
    {
        if (Instance != null)
            Instance.StopBgMusic();
    }

    public void PlayBgMusic(BgMusicType type)
    {
        if (type == BgMusicType.None)
        {
            StopBgMusic();
            return;
        }

        AudioClip clip = GetClip(type);
        if (clip == null)
        {
            StopBgMusic();
            return;
        }

        // Already playing this track — keep it
        if (currentMusic == type && musicActive != null && musicActive.isPlaying && musicActive.clip == clip)
            return;

        if (musicRoutine != null)
            StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(CrossfadeTo(type, clip));
    }

    public void StopBgMusic()
    {
        if (musicRoutine != null)
            StopCoroutine(musicRoutine);
        musicRoutine = StartCoroutine(FadeOutAll());
        currentMusic = BgMusicType.None;
    }

    AudioClip GetClip(BgMusicType type)
    {
        switch (type)
        {
            case BgMusicType.MainMenu: return mainMenuMusic;
            case BgMusicType.Gameplay: return gameplayMusic;
            case BgMusicType.Cutscene: return cutsceneMusic;
            default: return null;
        }
    }

    IEnumerator CrossfadeTo(BgMusicType type, AudioClip clip)
    {
        AudioSource incoming = musicActive == musicA ? musicB : musicA;
        AudioSource outgoing = musicActive;

        incoming.clip = clip;
        incoming.volume = 0f;
        if (!incoming.isPlaying)
            incoming.Play();

        float fade = Mathf.Max(0.05f, musicFadeSeconds);
        float t = 0f;
        float outStart = outgoing != null && outgoing.isPlaying ? outgoing.volume : 0f;

        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fade);
            incoming.volume = musicGainTarget * p;
            if (outgoing != null && outgoing.isPlaying)
                outgoing.volume = outStart * (1f - p);
            yield return null;
        }

        incoming.volume = musicGainTarget;
        musicGain = musicGainTarget;
        if (outgoing != null)
        {
            outgoing.Stop();
            outgoing.clip = null;
            outgoing.volume = 0f;
        }

        musicActive = incoming;
        currentMusic = type;
        musicRoutine = null;
    }

    IEnumerator FadeOutAll()
    {
        float fade = Mathf.Max(0.05f, musicFadeSeconds);
        float a0 = musicA != null ? musicA.volume : 0f;
        float b0 = musicB != null ? musicB.volume : 0f;
        float t = 0f;

        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            float p = 1f - Mathf.Clamp01(t / fade);
            if (musicA != null)
                musicA.volume = a0 * p;
            if (musicB != null)
                musicB.volume = b0 * p;
            yield return null;
        }

        if (musicA != null)
        {
            musicA.Stop();
            musicA.clip = null;
            musicA.volume = 0f;
        }
        if (musicB != null)
        {
            musicB.Stop();
            musicB.clip = null;
            musicB.volume = 0f;
        }

        musicRoutine = null;
    }
}
