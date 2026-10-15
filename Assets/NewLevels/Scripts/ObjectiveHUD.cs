using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mission progress shown while the player is free to move: a "STEP x/N" pill with
/// the distance to the current task, and a "STEP COMPLETE" pop when a step is done.
/// Works with the existing task arrow, HUD Navigation markers and curved arrow paths.
/// Reads everything from Level_Handler, so it works for every level.
/// UI is built at runtime, no prefab needed.
/// </summary>
public class ObjectiveHUD : MonoBehaviour
{
    public Sprite tickSprite;
    public Color accent = new Color(1f, 0.45f, 0.7f);

    GameObject canvasRoot;
    GameObject stepPill;
    Text stepText, distanceText;
    GameObject completePop;
    float completeTimer;
    int lastTaskIndex = -1;

    const float PopTime = 1.3f;

    private void Awake()
    {
        BuildUI();
        stepPill.SetActive(false);
    }

    private void OnDestroy()
    {
        if (canvasRoot != null)
            Destroy(canvasRoot);
    }

    private void LateUpdate()
    {
        Level_Handler lh = Level_Handler.instance;
        if (lh == null)
            return;

        int idx = lh.CurrentTaskIndex;
        if (lastTaskIndex >= 0 && idx > lastTaskIndex)
            ShowComplete();
        lastTaskIndex = idx;
        UpdateCompletePop();

        Transform target = lh.CurrentTarget;
        Transform player = GetPlayer(lh);
        bool guiding = lh.IsGuiding && !ClueSpeech.IsShowing && target != null && target.gameObject.activeInHierarchy && player != null;
        if (stepPill.activeSelf != guiding)
            stepPill.SetActive(guiding);
        if (!guiding)
            return;

        Vector3 a = player.position, b = target.position;
        a.y = b.y = 0f;
        stepText.text = "STEP " + (idx + 1) + "/" + lh.TaskCount;
        distanceText.text = Mathf.CeilToInt(Vector3.Distance(a, b)) + " m";
    }

    static Transform GetPlayer(Level_Handler lh)
    {
        AnimeSchoolManager m = lh.AnimeSchoolManager;
        if (m == null)
            return null;
        if (m.mainCh != null && m.mainCh.activeInHierarchy)
            return m.mainCh.transform;
        if (m.TriggeredCycle != null && m.TriggeredCycle.activeInHierarchy)
            return m.TriggeredCycle.transform;
        return null;
    }

    void ShowComplete()
    {
        completeTimer = PopTime;
        completePop.SetActive(true);
    }

    void UpdateCompletePop()
    {
        if (completeTimer <= 0f)
            return;
        completeTimer -= Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(completeTimer / PopTime);
        completePop.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.15f, Mathf.Sin(t * Mathf.PI));
        if (completeTimer <= 0f)
            completePop.SetActive(false);
    }

    Font GetFont()
    {
        if (Level_Handler.instance != null && Level_Handler.instance.HintText != null && Level_Handler.instance.HintText.font != null)
            return Level_Handler.instance.HintText.font;
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void BuildUI()
    {
        Font font = GetFont();

        canvasRoot = new GameObject("ObjectiveHUD_UI", typeof(RectTransform));
        Canvas canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300; // under CutsceneDialogue (400)
        CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // STEP pill, top centre
        stepPill = MakeImage(canvasRoot.transform, "StepPill", new Color(0.06f, 0.05f, 0.12f, 0.8f));
        RectTransform pRt = (RectTransform)stepPill.transform;
        pRt.anchorMin = pRt.anchorMax = new Vector2(0.5f, 1f);
        pRt.pivot = new Vector2(0.5f, 1f);
        pRt.sizeDelta = new Vector2(360f, 70f);
        pRt.anchoredPosition = new Vector2(0f, -24f);
        GameObject stripe = MakeImage(stepPill.transform, "Accent", accent);
        RectTransform aRt = (RectTransform)stripe.transform;
        aRt.anchorMin = new Vector2(0f, 0f);
        aRt.anchorMax = new Vector2(0f, 1f);
        aRt.pivot = new Vector2(0f, 0.5f);
        aRt.sizeDelta = new Vector2(10f, 0f);
        stepText = MakeText(stepPill.transform, "Step", font, 36, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        Stretch(stepText.rectTransform, 30f, 0f, -140f, 0f);
        distanceText = MakeText(stepPill.transform, "Distance", font, 34, FontStyle.Bold, accent, TextAnchor.MiddleRight);
        Stretch(distanceText.rectTransform, 200f, 0f, -22f, 0f);

        // STEP COMPLETE pop
        completePop = MakeImage(canvasRoot.transform, "StepComplete", new Color(0.06f, 0.05f, 0.12f, 0.85f));
        RectTransform cRt = (RectTransform)completePop.transform;
        cRt.anchorMin = cRt.anchorMax = new Vector2(0.5f, 1f);
        cRt.pivot = new Vector2(0.5f, 1f);
        cRt.sizeDelta = new Vector2(460f, 84f);
        cRt.anchoredPosition = new Vector2(0f, -110f);
        float textLeft = 24f;
        if (tickSprite != null)
        {
            GameObject tick = MakeImage(completePop.transform, "Tick", Color.white);
            Image ti = tick.GetComponent<Image>();
            ti.sprite = tickSprite;
            ti.preserveAspect = true;
            RectTransform tkRt = (RectTransform)tick.transform;
            tkRt.anchorMin = tkRt.anchorMax = new Vector2(0f, 0.5f);
            tkRt.pivot = new Vector2(0f, 0.5f);
            tkRt.sizeDelta = new Vector2(60f, 60f);
            tkRt.anchoredPosition = new Vector2(20f, 0f);
            textLeft = 90f;
        }
        Text ct = MakeText(completePop.transform, "Label", font, 38, FontStyle.Bold, new Color(0.55f, 1f, 0.6f), TextAnchor.MiddleCenter);
        ct.text = "STEP COMPLETE";
        Stretch(ct.rectTransform, textLeft, 0f, -20f, 0f);
        completePop.SetActive(false);
    }

    static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(right, top);
    }

    static GameObject MakeImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    static Text MakeText(Transform parent, string name, Font font, int size, FontStyle style, Color color, TextAnchor align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
        return t;
    }
}
