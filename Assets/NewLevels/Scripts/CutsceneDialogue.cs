using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

/// <summary>
/// Cinematic overlay for a Timeline cutscene: letterbox bars, typewriter dialogue
/// box (speaker + line) and a Skip button. Lines are timed against the director's
/// time, so they stay in sync with the Timeline and can be edited in the Inspector.
/// UI is built at runtime, no prefab needed.
/// </summary>
public class CutsceneDialogue : MonoBehaviour
{
    [System.Serializable]
    public class Line
    {
        public float start;
        public float duration = 3f;
        public string speaker;
        [TextArea] public string text;
    }

    public PlayableDirector director;
    public Line[] lines;
    public bool letterbox = true;
    public bool showSkip = true;
    [Tooltip("Seconds before the Skip button appears.")]
    public float skipDelay = 1f;
    public float charsPerSecond = 45f;
    public Color speakerColor = new Color(1f, 0.62f, 0.78f);
    [Tooltip("Optional camera listener, enabled only when no other listener is active.")]
    public AudioListener cameraListener;

    GameObject canvasRoot;
    RectTransform topBar, bottomBar;
    GameObject box;
    Text speakerText, lineText;
    GameObject skipButton;
    float barAmount;
    bool wasPlaying;
    bool ownsCameraListener;

    const float BarHeight = 110f;

    private void OnEnable()
    {
        if (director == null)
            director = GetComponent<PlayableDirector>();
        if (director != null)
        {
            director.stopped -= OnDirectorStopped;
            director.stopped += OnDirectorStopped;
        }
        BuildUI();
        canvasRoot.SetActive(false);
        wasPlaying = false;
        barAmount = 0f;
    }

    private void OnDisable()
    {
        if (director != null)
            director.stopped -= OnDirectorStopped;
        if (canvasRoot != null)
            canvasRoot.SetActive(false);
        ReleaseCameraListener();
    }

    private void OnDestroy()
    {
        if (director != null)
            director.stopped -= OnDirectorStopped;
        ReleaseCameraListener();
        if (canvasRoot != null)
            Destroy(canvasRoot);
    }

    void OnDirectorStopped(PlayableDirector stoppedDirector)
    {
        ReleaseCameraListener();
    }

    private void Update()
    {
        bool playing = director != null && director.state == PlayState.Playing;
        if (playing && !wasPlaying)
            OnPlayStarted();
        wasPlaying = playing;

        if (canvasRoot.activeSelf != playing)
            canvasRoot.SetActive(playing);
        if (!playing)
        {
            ReleaseCameraListener();
            return;
        }

        float t = (float)director.time;
        float dur = (float)director.duration;

        // Bars slide in at the start and out over the last half second
        float target = (letterbox && t < dur - 0.5f) ? 1f : 0f;
        barAmount = Mathf.MoveTowards(barAmount, target, Time.unscaledDeltaTime * 3f);
        float h = BarHeight * Mathf.SmoothStep(0f, 1f, barAmount);
        topBar.sizeDelta = new Vector2(0f, h);
        bottomBar.sizeDelta = new Vector2(0f, h);

        if (skipButton != null)
            skipButton.SetActive(showSkip && t >= skipDelay && t < dur - 0.5f);

        Line current = null;
        if (lines != null)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                Line l = lines[i];
                if (l != null && t >= l.start && t < l.start + l.duration)
                    current = l;
            }
        }

        if (current == null || string.IsNullOrEmpty(current.text))
        {
            box.SetActive(false);
            return;
        }

        box.SetActive(true);
        speakerText.text = current.speaker;
        int count = Mathf.Clamp(Mathf.FloorToInt((t - current.start) * charsPerSecond), 0, current.text.Length);
        lineText.text = current.text.Substring(0, count);
    }

    void OnPlayStarted()
    {
        if (cameraListener == null)
            return;

        bool otherActive = false;
        AudioListener[] all = FindObjectsOfType<AudioListener>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != cameraListener && all[i].isActiveAndEnabled)
                otherActive = true;
        }
        ownsCameraListener = !otherActive;
        cameraListener.enabled = ownsCameraListener;
    }

    void ReleaseCameraListener()
    {
        if (!ownsCameraListener || cameraListener == null)
            return;

        cameraListener.enabled = false;
        ownsCameraListener = false;
    }

    public void Skip()
    {
        if (director == null || director.state != PlayState.Playing)
            return;

        // Jump to the end so final poses/activations apply, then stop.
        // Level_Handler listens to director.stopped and continues the level.
        director.time = director.duration;
        director.Evaluate();
        director.Stop();
    }

    Font GetFont()
    {
        if (Level_Handler.instance != null && Level_Handler.instance.HintText != null && Level_Handler.instance.HintText.font != null)
            return Level_Handler.instance.HintText.font;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void BuildUI()
    {
        if (canvasRoot != null)
            return;

        Font font = GetFont();

        canvasRoot = new GameObject("CutsceneDialogueUI");
        Canvas canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRoot.AddComponent<GraphicRaycaster>();

        topBar = MakeBar("TopBar", 1f);
        bottomBar = MakeBar("BottomBar", 0f);

        box = new GameObject("DialogueBox", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(canvasRoot.transform, false);
        RectTransform boxRt = (RectTransform)box.transform;
        boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0f);
        boxRt.pivot = new Vector2(0.5f, 0f);
        boxRt.sizeDelta = new Vector2(1500f, 230f);
        boxRt.anchoredPosition = new Vector2(0f, 130f);
        box.GetComponent<Image>().color = new Color(0.06f, 0.05f, 0.12f, 0.85f);

        speakerText = MakeText(box.transform, "Speaker", font, 42, FontStyle.Bold, speakerColor, TextAnchor.UpperLeft);
        RectTransform sRt = speakerText.rectTransform;
        sRt.anchorMin = new Vector2(0f, 1f);
        sRt.anchorMax = new Vector2(1f, 1f);
        sRt.pivot = new Vector2(0f, 1f);
        sRt.sizeDelta = new Vector2(-60f, 56f);
        sRt.anchoredPosition = new Vector2(30f, -14f);

        lineText = MakeText(box.transform, "Line", font, 44, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
        RectTransform lRt = lineText.rectTransform;
        lRt.anchorMin = new Vector2(0f, 0f);
        lRt.anchorMax = new Vector2(1f, 1f);
        lRt.offsetMin = new Vector2(30f, 18f);
        lRt.offsetMax = new Vector2(-30f, -74f);

        skipButton = new GameObject("SkipButton", typeof(RectTransform), typeof(Image), typeof(Button));
        skipButton.transform.SetParent(canvasRoot.transform, false);
        RectTransform kRt = (RectTransform)skipButton.transform;
        kRt.anchorMin = kRt.anchorMax = new Vector2(1f, 1f);
        kRt.pivot = new Vector2(1f, 1f);
        kRt.sizeDelta = new Vector2(220f, 80f);
        kRt.anchoredPosition = new Vector2(-30f, -130f);
        skipButton.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        skipButton.GetComponent<Button>().onClick.AddListener(Skip);
        Text skipLabel = MakeText(skipButton.transform, "Label", font, 38, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        skipLabel.text = "SKIP >>";
        RectTransform skRt = skipLabel.rectTransform;
        skRt.anchorMin = Vector2.zero;
        skRt.anchorMax = Vector2.one;
        skRt.offsetMin = skRt.offsetMax = Vector2.zero;
        skipButton.SetActive(false);

        box.SetActive(false);
    }

    RectTransform MakeBar(string name, float anchorY)
    {
        GameObject bar = new GameObject(name, typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(canvasRoot.transform, false);
        RectTransform rt = (RectTransform)bar.transform;
        rt.anchorMin = new Vector2(0f, anchorY);
        rt.anchorMax = new Vector2(1f, anchorY);
        rt.pivot = new Vector2(0.5f, anchorY);
        rt.sizeDelta = Vector2.zero;
        Image img = bar.GetComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        return rt;
    }

    Text MakeText(Transform parent, string name, Font font, int size, FontStyle style, Color color, TextAnchor align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
        return t;
    }
}
