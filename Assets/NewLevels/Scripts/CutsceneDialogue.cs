using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

/// <summary>
/// Cinematic overlay for a Timeline cutscene: uses the mission 1–5 speech-bubble
/// boxes already in the scene, plus a Skip button. Lines are timed against the
/// director so they stay in sync with the Timeline.
/// LateUpdate runs after Timeline so only the current speaker keeps a talk pose.
/// </summary>
[DefaultExecutionOrder(30000)]
public class CutsceneDialogue : MonoBehaviour
{
    [System.Serializable]
    public class Line
    {
        public float start;
        public float duration = 3f;
        public string speaker;
        [TextArea] public string text;
        public AudioClip voice;
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
    bool borrowedBubbles;
    bool useSceneBubbles;
    string currentSpeaker;
    AudioSource voiceSource;
    int voiceLine = -1;
    bool musicDucked;

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
        useSceneBubbles = MissionSpeechBubbles.Available;
        if (useSceneBubbles)
        {
            if (box != null)
                box.SetActive(false);
            if (topBar != null)
                topBar.gameObject.SetActive(false);
            if (bottomBar != null)
                bottomBar.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (director != null)
            director.stopped -= OnDirectorStopped;
        if (canvasRoot != null)
            canvasRoot.SetActive(false);
        ReleaseCameraListener();
        UnborrowBubbles();
    }

    private void OnDestroy()
    {
        if (director != null)
            director.stopped -= OnDirectorStopped;
        ReleaseCameraListener();
        UnborrowBubbles();
        if (canvasRoot != null)
            Destroy(canvasRoot);
    }

    void OnDirectorStopped(PlayableDirector stoppedDirector)
    {
        ReleaseCameraListener();
        UnborrowBubbles();
    }

    private void Update()
    {
        bool playing = director != null && director.state == PlayState.Playing;
        if (playing && !wasPlaying)
            OnPlayStarted();
        if (!playing && wasPlaying)
            UnborrowBubbles();
        wasPlaying = playing;

        if (canvasRoot.activeSelf != playing)
            canvasRoot.SetActive(playing);
        if (!playing)
        {
            currentSpeaker = null;
            StopVoice();
            SetMusicDuck(false);
            ReleaseCameraListener();
            return;
        }

        float t = (float)director.time;
        float dur = (float)director.duration;

        bool showBars = letterbox && !useSceneBubbles;
        if (topBar != null)
            topBar.gameObject.SetActive(showBars);
        if (bottomBar != null)
            bottomBar.gameObject.SetActive(showBars);

        float target = (showBars && t < dur - 0.5f) ? 1f : 0f;
        barAmount = Mathf.MoveTowards(barAmount, target, Time.unscaledDeltaTime * 3f);
        if (showBars)
        {
            float h = BarHeight * Mathf.SmoothStep(0f, 1f, barAmount);
            topBar.sizeDelta = new Vector2(0f, h);
            bottomBar.sizeDelta = new Vector2(0f, h);
        }

        if (skipButton != null)
            skipButton.SetActive(showSkip && t >= skipDelay && t < dur - 0.5f);

        Line current = CurrentLine(t);

        if (current == null || string.IsNullOrEmpty(current.text))
        {
            currentSpeaker = null;
            StopVoice();
            SetMusicDuck(false);
            if (!useSceneBubbles && box != null)
                box.SetActive(false);
            return;
        }

        currentSpeaker = current.speaker;
        SetMusicDuck(true);
        SyncVoice(current);

        int count = Mathf.Clamp(Mathf.FloorToInt((t - current.start) * TypeRate(current)), 0, current.text.Length);
        string typed = current.text.Substring(0, count);

        if (useSceneBubbles)
        {
            if (box != null)
                box.SetActive(false);
            return;
        }

        box.SetActive(true);
        speakerText.text = current.speaker;
        lineText.text = typed;
    }

    private void LateUpdate()
    {
        if (director == null || director.state != PlayState.Playing)
            return;
        ApplySpeakerTalkLock();
        if (useSceneBubbles)
        {
            Line current = CurrentLine((float)director.time);
            if (current != null && !string.IsNullOrEmpty(current.text) && !string.IsNullOrEmpty(currentSpeaker))
            {
                SyncVoice(current);
                int count = Mathf.Clamp(Mathf.FloorToInt(((float)director.time - current.start) * TypeRate(current)), 0, current.text.Length);
                string typed = current.text.Substring(0, count);
                MissionSpeechBubbles.Show(current.speaker, typed, SpeakerHead(current.speaker), PlayingCamera());
            }
            else
            {
                MissionSpeechBubbles.HideSoon();
            }
        }
    }

    Line CurrentLine(float t)
    {
        if (lines == null)
            return null;
        Line current = null;
        float bestStart = float.NegativeInfinity;
        for (int i = 0; i < lines.Length; i++)
        {
            Line l = lines[i];
            if (l == null || string.IsNullOrEmpty(l.text))
                continue;
            if (t >= l.start && t < l.start + l.duration && l.start >= bestStart)
            {
                current = l;
                bestStart = l.start;
            }
        }
        return current;
    }

    void ApplySpeakerTalkLock()
    {
        TimelineAsset timeline = director.playableAsset as TimelineAsset;
        if (timeline == null)
            return;

        float t = (float)director.time;
        bool hasLine = !string.IsNullOrEmpty(currentSpeaker);

        foreach (TrackAsset track in timeline.GetOutputTracks())
        {
            AnimationTrack animTrack = track as AnimationTrack;
            if (animTrack == null)
                continue;

            Animator anim = director.GetGenericBinding(animTrack) as Animator;
            if (anim == null || !anim.gameObject.activeInHierarchy)
                continue;

            TimelineClip active = null;
            foreach (TimelineClip clip in animTrack.GetClips())
            {
                if (t >= clip.start && t < clip.start + clip.duration)
                {
                    active = clip;
                    break;
                }
            }

            bool thisIsSpeaker = hasLine && NameMatchesSpeaker(anim.transform, currentSpeaker, director.transform);
            bool activeIsTalk = active != null && IsTalkClip(active.displayName);
            bool activeIsBusy = active != null && IsBusyClip(active.displayName);

            if (thisIsSpeaker && !activeIsBusy)
            {
                AnimationClip talk = GetNamedClip(animTrack, true) ?? ClipOf(active);
                SampleKeepRoot(anim, talk, t);
            }
            else if (hasLine && !thisIsSpeaker && activeIsTalk)
            {
                AnimationClip idle = GetNamedClip(animTrack, false);
                SampleKeepRoot(anim, idle, t);
            }
        }
    }

    static bool IsTalkClip(string name)
    {
        return !string.IsNullOrEmpty(name) && name.IndexOf("talk", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool IsBusyClip(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        string n = name.ToLowerInvariant();
        return n.Contains("walk") || n.Contains("jog") || n.Contains("sit") || n.Contains("run");
    }

    static AnimationClip ClipOf(TimelineClip clip)
    {
        if (clip == null)
            return null;
        AnimationPlayableAsset asset = clip.asset as AnimationPlayableAsset;
        return asset != null ? asset.clip : null;
    }

    static AnimationClip GetNamedClip(AnimationTrack track, bool talk)
    {
        foreach (TimelineClip clip in track.GetClips())
        {
            bool isTalk = IsTalkClip(clip.displayName);
            if (talk != isTalk)
                continue;
            if (!talk)
            {
                string n = clip.displayName == null ? "" : clip.displayName.ToLowerInvariant();
                if (!n.Contains("idle"))
                    continue;
            }
            AnimationClip c = ClipOf(clip);
            if (c != null)
                return c;
        }
        return null;
    }

    static bool NameMatchesSpeaker(Transform t, string speaker, Transform stopAt)
    {
        if (t == null || string.IsNullOrEmpty(speaker))
            return false;
        string s = speaker.ToLowerInvariant().Trim();
        while (t != null && t != stopAt)
        {
            string n = t.name.ToLowerInvariant();
            if (n.Contains(s) || (n.Length >= 4 && s.Contains(n)))
                return true;
            t = t.parent;
        }
        return false;
    }

    static void SampleKeepRoot(Animator anim, AnimationClip clip, float time)
    {
        if (anim == null || clip == null)
            return;
        Transform tr = anim.transform;
        Vector3 lp = tr.localPosition;
        Quaternion lr = tr.localRotation;
        float sample = clip.length > 0.01f ? time % clip.length : 0f;
        clip.SampleAnimation(anim.gameObject, sample);
        tr.localPosition = lp;
        tr.localRotation = lr;
    }

    Vector3 SpeakerHead(string speaker)
    {
        Transform t = FindSpeakerTransform(speaker);
        if (t == null)
            return Vector3.zero;
        Renderer[] rends = t.GetComponentsInChildren<Renderer>(true);
        Bounds b = new Bounds();
        bool any = false;
        if (rends != null)
        {
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null || !rends[i].enabled)
                    continue;
                if (!rends[i].gameObject.activeInHierarchy)
                    continue;
                if (!any)
                {
                    b = rends[i].bounds;
                    any = true;
                }
                else b.Encapsulate(rends[i].bounds);
            }
        }
        if (any)
            return new Vector3(b.center.x, b.max.y + 0.08f, b.center.z);
        return t.position + Vector3.up * 1.55f;
    }

    Transform FindSpeakerTransform(string speaker)
    {
        if (string.IsNullOrEmpty(speaker))
            return null;
        string s = speaker.ToLowerInvariant().Trim();
        Transform root = director != null ? director.transform : null;
        Transform best = null;
        int bestScore = 0;
        if (root != null)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                int score = SpeakerNameScore(child.name, s);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = child;
                }
            }
            if (best != null)
                return best;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                int score = SpeakerNameScore(all[i].name, s);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = all[i];
                }
            }
            if (best != null)
                return best;
        }
        return null;
    }

    static int SpeakerNameScore(string objectName, string speaker)
    {
        if (string.IsNullOrEmpty(objectName) || string.IsNullOrEmpty(speaker))
            return 0;
        string n = objectName.ToLowerInvariant().Trim();
        if (n == speaker)
            return 30;
        if (n.StartsWith(speaker + " ") || n.EndsWith(" " + speaker))
            return 20;
        if (speaker.Length >= 3 && n.Contains(speaker))
            return 10;
        return 0;
    }

    Camera PlayingCamera()
    {
        Camera best = null;
        float depth = float.NegativeInfinity;
        Camera[] cams = Camera.allCameras;
        for (int i = 0; i < cams.Length; i++)
        {
            Camera c = cams[i];
            if (c == null || !c.isActiveAndEnabled || c.targetTexture != null)
                continue;
            if (c.depth >= depth)
            {
                depth = c.depth;
                best = c;
            }
        }
        if (best != null)
            return best;
        if (director != null)
        {
            Camera[] nested = director.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < nested.Length; i++)
            {
                if (nested[i] != null && nested[i].isActiveAndEnabled)
                    return nested[i];
            }
        }
        return Camera.main;
    }

    void OnPlayStarted()
    {
        BorrowBubbles();
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

    void BorrowBubbles()
    {
        if (borrowedBubbles)
            return;
        useSceneBubbles = MissionSpeechBubbles.Available;
        if (useSceneBubbles)
            MissionSpeechBubbles.Retain();
        borrowedBubbles = true;
    }

    void UnborrowBubbles()
    {
        StopVoice();
        SetMusicDuck(false);
        if (!borrowedBubbles)
            return;
        MissionSpeechBubbles.Release();
        borrowedBubbles = false;
    }

    float TypeRate(Line line)
    {
        if (line == null || string.IsNullOrEmpty(line.text))
            return charsPerSecond;
        float dur = line.voice != null && line.voice.length > 0.08f ? line.voice.length : line.duration;
        dur = Mathf.Max(0.35f, dur * 0.92f);
        return line.text.Length / dur;
    }

    void SyncVoice(Line current)
    {
        int idx = -1;
        if (current != null && lines != null)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == current)
                {
                    idx = i;
                    break;
                }
            }
        }
        if (idx == voiceLine)
            return;
        voiceLine = idx;
        EnsureVoiceSource();
        voiceSource.Stop();
        if (idx < 0 || lines[idx] == null || lines[idx].voice == null)
            return;
        voiceSource.clip = lines[idx].voice;
        voiceSource.Play();
    }

    void StopVoice()
    {
        voiceLine = -1;
        if (voiceSource != null)
            voiceSource.Stop();
    }

    void SetMusicDuck(bool on)
    {
        if (musicDucked == on)
            return;
        musicDucked = on;
        if (on)
            UIClickSound.BeginDialogueDuck();
        else
            UIClickSound.EndDialogueDuck();
    }

    void EnsureVoiceSource()
    {
        if (voiceSource == null)
            voiceSource = GetComponent<AudioSource>();
        if (voiceSource == null)
            voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.spatialBlend = 0f;
        voiceSource.loop = false;
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

        AnalyticsEvents.Log("BF_GamePlay_Cutscene_Skip");
        StopVoice();
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
        kRt.anchorMin = kRt.anchorMax = new Vector2(1f, 0f);
        kRt.pivot = new Vector2(1f, 0f);
        kRt.sizeDelta = new Vector2(220f, 72f);
        kRt.anchoredPosition = new Vector2(-36f, 28f);
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

/// <summary>
/// Shared access to the Canvas/dialogues speech bubbles used by missions 1–5.
/// Restores their original text when cutscenes finish.
/// </summary>
public static class MissionSpeechBubbles
{
    class Box
    {
        public GameObject root;
        public RectTransform rect;
        public Text text;
        public Animator[] anims;
        public string originalText;
        public bool originalActive;
        public Vector2 originalAnchorMin;
        public Vector2 originalAnchorMax;
        public Vector2 originalPivot;
        public Vector2 originalPos;
        public Vector2 originalSize;
        public Vector3 originalScale;
    }

    static readonly List<Box> boxes = new List<Box>();
    static Canvas host;
    static int hostSort;
    static int retain;
    static bool collected;
    static int lockedIdx = -1;
    static string lockedSpeaker;
    static Vector2 lockedPos;
    static bool hasLockedPos;
    static bool lockedBelow;
    static float hideAfter = -1f;
    static string lastBody;

    public static bool Available
    {
        get
        {
            Collect();
            return boxes.Count > 0;
        }
    }

    public static void Retain()
    {
        Collect();
        if (retain == 0 && host != null)
        {
            hostSort = host.sortingOrder;
            host.sortingOrder = 250;
            FreezeBoxAnims();
        }
        retain++;
    }

    public static void Release()
    {
        if (retain <= 0)
            return;
        retain--;
        if (retain > 0)
            return;

        HideAll(true);
        Unlock();
        if (host != null)
            host.sortingOrder = hostSort;
    }

    public static void Show(string speaker, string typed)
    {
        Show(speaker, typed, Vector3.zero, null);
    }

    public static void Show(string speaker, string typed, Vector3 worldPos, Camera cam)
    {
        hideAfter = -1f;
        Collect();
        if (boxes.Count == 0)
            return;

        if (cam == null)
            cam = Camera.main;
        bool place = cam != null && host != null && worldPos.sqrMagnitude > 0.0001f;
        float vpX = 0.5f;
        if (place)
        {
            Vector3 vp = cam.WorldToViewportPoint(worldPos);
            if (vp.z < 0.05f)
                vp.x = 1f - vp.x;
            vpX = vp.x;
        }

        bool fresh = lockedIdx < 0 || lockedSpeaker != speaker;
        if (fresh)
        {
            lockedSpeaker = speaker;
            if (place)
                lockedIdx = vpX < 0.5f ? 0 : (boxes.Count >= 3 ? 2 : Mathf.Max(0, boxes.Count - 1));
            else
                lockedIdx = Pick(speaker);
            hasLockedPos = false;
            lockedBelow = false;
            lastBody = null;
        }
        int idx = Mathf.Clamp(lockedIdx, 0, boxes.Count - 1);

        for (int i = 0; i < boxes.Count; i++)
        {
            bool on = i == idx;
            if (on)
            {
                FreezeAnims(boxes[i]);
                if (!boxes[i].root.activeSelf)
                    boxes[i].root.SetActive(true);
                Vector3 keepScale = boxes[i].originalScale.sqrMagnitude > 0.01f ? boxes[i].originalScale : Vector3.one;
                if ((boxes[i].root.transform.localScale - keepScale).sqrMagnitude > 0.0001f)
                    boxes[i].root.transform.localScale = keepScale;
                string body = typed ?? "";
                if (!string.IsNullOrEmpty(speaker))
                    body = speaker + ": " + body;
                if (boxes[i].text != null && body != lastBody)
                {
                    boxes[i].text.text = body;
                    lastBody = body;
                }
                if (place)
                    PlaceNearSpeaker(boxes[i], cam, worldPos, vpX);
            }
            else if (boxes[i].root.activeSelf)
            {
                RestoreRect(boxes[i]);
                boxes[i].root.SetActive(false);
            }
        }
    }

    public static void HideSoon()
    {
        if (hideAfter < 0f)
            hideAfter = Time.unscaledTime + 0.18f;
        if (hideAfter >= 0f && Time.unscaledTime >= hideAfter)
            Hide();
    }

    static void PlaceNearSpeaker(Box box, Camera cam, Vector3 worldPos, float vpX)
    {
        if (box.rect == null || host == null)
            return;
        RectTransform hostRt = host.transform as RectTransform;
        if (hostRt == null)
            return;

        box.rect.anchorMin = box.rect.anchorMax = new Vector2(0.5f, 0.5f);
        box.rect.pivot = new Vector2(0.5f, 0f);
        if (box.originalSize.x > 80f)
            box.rect.sizeDelta = box.originalSize;

        if (hasLockedPos)
        {
            box.rect.anchoredPosition = lockedPos;
            return;
        }

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        Vector2 local;
        Camera eventCam = host.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(hostRt, screen, eventCam, out local))
            return;

        float boxW = box.originalSize.x > 80f ? box.originalSize.x : 280f;
        float boxH = box.originalSize.y > 60f ? box.originalSize.y : 110f;

        float nudgeX = vpX < 0.5f ? -70f : 70f;
        Vector2 above = local + new Vector2(nudgeX, 48f);
        Vector2 below = local + new Vector2(nudgeX, -boxH - 12f);

        float minX, maxX, minY, maxY;
        GetOnScreenLimits(hostRt, eventCam, boxW, boxH, out minX, out maxX, out minY, out maxY);

        lockedBelow = above.y > maxY;
        Vector2 desired = lockedBelow ? below : above;
        desired.x = Mathf.Clamp(desired.x, minX, maxX);
        desired.y = Mathf.Clamp(desired.y, minY, maxY);
        lockedPos = desired;
        hasLockedPos = true;
        box.rect.anchoredPosition = lockedPos;
    }

    static void GetOnScreenLimits(RectTransform hostRt, Camera eventCam, float boxW, float boxH, out float minX, out float maxX, out float minY, out float maxY)
    {
        // Top AdMob banners sit in screen pixels. Keep the whole bubble below that band
        // and above the skip button.
        float bannerPx = Mathf.Max(120f, Screen.height * 0.13f);
        float skipPx = Mathf.Max(96f, Screen.height * 0.10f);
        float pad = 18f;

        Vector2 topLeft;
        Vector2 botRight;
        bool okMin = RectTransformUtility.ScreenPointToLocalPointInRectangle(hostRt, new Vector2(pad, Screen.height - bannerPx), eventCam, out topLeft);
        bool okMax = RectTransformUtility.ScreenPointToLocalPointInRectangle(hostRt, new Vector2(Screen.width - pad, skipPx), eventCam, out botRight);
        if (okMin && okMax)
        {
            minX = Mathf.Min(topLeft.x, botRight.x) + boxW * 0.5f;
            maxX = Mathf.Max(topLeft.x, botRight.x) - boxW * 0.5f;
            minY = Mathf.Min(topLeft.y, botRight.y);
            maxY = Mathf.Max(topLeft.y, botRight.y) - boxH;
        }
        else
        {
            float halfW = hostRt.rect.width * 0.5f;
            float halfH = hostRt.rect.height * 0.5f;
            minX = -halfW + boxW * 0.5f + 20f;
            maxX = halfW - boxW * 0.5f - 20f;
            minY = -halfH + 110f;
            maxY = halfH - Mathf.Max(180f, hostRt.rect.height * 0.16f) - boxH;
        }

        if (maxX < minX)
        {
            float mid = (minX + maxX) * 0.5f;
            minX = maxX = mid;
        }
        if (maxY < minY)
        {
            float mid = (minY + maxY) * 0.5f;
            minY = maxY = mid;
        }
    }

    static void RestoreRect(Box box)
    {
        if (box.rect == null)
            return;
        box.rect.anchorMin = box.originalAnchorMin;
        box.rect.anchorMax = box.originalAnchorMax;
        box.rect.pivot = box.originalPivot;
        box.rect.anchoredPosition = box.originalPos;
        box.rect.sizeDelta = box.originalSize;
    }

    public static void Hide()
    {
        HideAll(false);
        Unlock();
    }

    static void Unlock()
    {
        lockedIdx = -1;
        lockedSpeaker = null;
        hasLockedPos = false;
        lockedBelow = false;
        hideAfter = -1f;
        lastBody = null;
    }

    static void FreezeBoxAnims()
    {
        for (int i = 0; i < boxes.Count; i++)
            FreezeAnims(boxes[i]);
    }

    static void FreezeAnims(Box box)
    {
        if (box == null || box.root == null)
            return;
        if (box.anims != null)
        {
            for (int i = 0; i < box.anims.Length; i++)
            {
                if (box.anims[i] != null)
                    box.anims[i].enabled = false;
            }
        }
        Animation[] legacy = box.root.GetComponentsInChildren<Animation>(true);
        for (int i = 0; i < legacy.Length; i++)
        {
            if (legacy[i] != null)
                legacy[i].enabled = false;
        }
    }

    static void HideAll(bool restoreText)
    {
        for (int i = 0; i < boxes.Count; i++)
        {
            Box b = boxes[i];
            if (b.root == null)
                continue;
            if (restoreText && b.text != null)
                b.text.text = b.originalText;
            if (restoreText && b.anims != null)
            {
                for (int a = 0; a < b.anims.Length; a++)
                {
                    if (b.anims[a] != null)
                        b.anims[a].enabled = true;
                }
            }
            else
                FreezeAnims(b);
            RestoreRect(b);
            b.root.SetActive(restoreText ? b.originalActive : false);
        }
    }

    static int Pick(string speaker)
    {
        if (boxes.Count == 0)
            return 0;
        string s = speaker == null ? "" : speaker.ToLowerInvariant();
        bool player = s.Contains("yuki") || s.Contains("you") || s.Contains("player");
        if (player)
            return 0;
        if (boxes.Count >= 3)
            return 2;
        if (boxes.Count >= 2)
            return 1;
        return 0;
    }

    static void Collect()
    {
        if (collected)
        {
            for (int i = boxes.Count - 1; i >= 0; i--)
            {
                if (boxes[i].root == null)
                    boxes.RemoveAt(i);
            }
            if (boxes.Count > 0)
                return;
            collected = false;
        }

        boxes.Clear();
        host = null;
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null || c.transform.parent != null || c.name != "Canvas")
                continue;
            host = c;
            break;
        }
        if (host == null)
            return;

        for (int i = 0; i < host.transform.childCount; i++)
        {
            Transform child = host.transform.GetChild(i);
            if (child == null || !child.name.StartsWith("dialogues"))
                continue;
            Text t = child.GetComponentInChildren<Text>(true);
            if (t == null)
                continue;
            RectTransform rt = child as RectTransform;
            ContentSizeFitter[] fitters = child.GetComponentsInChildren<ContentSizeFitter>(true);
            for (int f = 0; f < fitters.Length; f++)
            {
                if (fitters[f] != null)
                    fitters[f].enabled = false;
            }
            LayoutGroup[] layouts = child.GetComponentsInChildren<LayoutGroup>(true);
            for (int g = 0; g < layouts.Length; g++)
            {
                if (layouts[g] != null)
                    layouts[g].enabled = false;
            }
            Vector3 scale = child.localScale;
            if (scale.sqrMagnitude < 0.01f)
                scale = Vector3.one;
            boxes.Add(new Box
            {
                root = child.gameObject,
                rect = rt,
                text = t,
                anims = child.GetComponentsInChildren<Animator>(true),
                originalText = t.text,
                originalActive = child.gameObject.activeSelf,
                originalAnchorMin = rt != null ? rt.anchorMin : Vector2.zero,
                originalAnchorMax = rt != null ? rt.anchorMax : Vector2.one,
                originalPivot = rt != null ? rt.pivot : new Vector2(0.5f, 0.5f),
                originalPos = rt != null ? rt.anchoredPosition : Vector2.zero,
                originalSize = rt != null ? rt.sizeDelta : Vector2.zero,
                originalScale = scale
            });
        }
        collected = boxes.Count > 0;
    }
}
