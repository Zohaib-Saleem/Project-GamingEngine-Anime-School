using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Task point that needs no Timeline: on reach the player pauses (joystick hidden),
/// a short speech box plays (speaker + typed lines), then the level moves on.
/// Use instead of TaskPointHit on a LeveltaskParticle object.
/// </summary>
public class ClueTaskPoint : MonoBehaviour
{
    [Serializable]
    public class Line
    {
        public string speaker;
        [TextArea] public string text;
    }

    public Line[] lines;
    public float secondsPerLine = 3.4f;
    public float charsPerSecond = 45f;
    public Color speakerColor = new Color(1f, 0.62f, 0.78f);
    [Tooltip("Optional prop picked up at this point (hidden on reach).")]
    public GameObject pickupProp;

    bool used;

    private void OnEnable()
    {
        used = false;
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = true;
        if (pickupProp != null)
            pickupProp.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (used)
            return;
        if (!other.CompareTag("Player") && !other.CompareTag("Cycle"))
            return;

        used = true;
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;
        if (pickupProp != null)
            pickupProp.SetActive(false);

        Level_Handler lh = Level_Handler.instance;
        if (lines == null || lines.Length == 0)
        {
            lh.ReachCurrentTask();
            return;
        }

        SetPlayerInput(lh, false);
        ClueSpeech.Show(lines, secondsPerLine, charsPerSecond, speakerColor, () =>
        {
            SetPlayerInput(lh, true);
            lh.ReachCurrentTask();
        });
    }

    static void SetPlayerInput(Level_Handler lh, bool on)
    {
        if (lh == null || lh.AnimeSchoolManager == null)
            return;
        if (lh.AnimeSchoolManager.mainChUI != null)
            lh.AnimeSchoolManager.mainChUI.SetActive(on);
        if (lh.ChArrow != null)
            lh.ChArrow.gameObject.SetActive(on);
        if (on)
            lh.ActivateHint();
        else
            lh.DeActiveHint();
    }
}

/// <summary>Runtime speech box used by ClueTaskPoint. Lives on its own object so it survives the task point being disabled.</summary>
public class ClueSpeech : MonoBehaviour
{
    static ClueSpeech current;
    public static bool IsShowing => current != null;

    ClueTaskPoint.Line[] lines;
    float perLine, cps;
    int index;
    float timer;
    Action onDone;
    Text speakerText, lineText;

    public static void Show(ClueTaskPoint.Line[] lines, float perLine, float cps, Color speakerColor, Action onDone)
    {
        if (current != null)
            Destroy(current.gameObject);

        GameObject go = new GameObject("ClueSpeech");
        current = go.AddComponent<ClueSpeech>();
        current.lines = lines;
        current.perLine = Mathf.Max(1f, perLine);
        current.cps = Mathf.Max(5f, cps);
        current.onDone = onDone;
        current.Build(speakerColor);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        ClueTaskPoint.Line l = lines[index];

        // Tap after the line is fully typed skips to the next one.
        bool typed = timer * cps >= l.text.Length;
        if (timer >= perLine || (typed && timer > 0.6f && Input.GetMouseButtonDown(0)))
        {
            timer = 0f;
            index++;
            if (index >= lines.Length)
            {
                Finish();
                return;
            }
            l = lines[index];
        }

        speakerText.text = l.speaker;
        lineText.text = l.text.Substring(0, Mathf.Min(l.text.Length, Mathf.FloorToInt(timer * cps)));
    }

    void Finish()
    {
        Action done = onDone;
        onDone = null;
        Destroy(gameObject);
        current = null;
        if (done != null)
            done();
    }

    private void OnDestroy()
    {
        if (current == this)
            current = null;
    }

    void Build(Color speakerColor)
    {
        Font font = Level_Handler.instance != null && Level_Handler.instance.HintText != null && Level_Handler.instance.HintText.font != null
            ? Level_Handler.instance.HintText.font
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 350;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Same look as the CutsceneDialogue box.
        GameObject box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(transform, false);
        RectTransform bRt = (RectTransform)box.transform;
        bRt.anchorMin = bRt.anchorMax = new Vector2(0.5f, 0f);
        bRt.pivot = new Vector2(0.5f, 0f);
        bRt.sizeDelta = new Vector2(1500f, 230f);
        bRt.anchoredPosition = new Vector2(0f, 130f);
        Image bg = box.GetComponent<Image>();
        bg.color = new Color(0.06f, 0.05f, 0.12f, 0.85f);
        bg.raycastTarget = false;

        speakerText = MakeText(box.transform, font, 42, FontStyle.Bold, speakerColor);
        RectTransform sRt = speakerText.rectTransform;
        sRt.anchorMin = new Vector2(0f, 1f);
        sRt.anchorMax = new Vector2(1f, 1f);
        sRt.pivot = new Vector2(0f, 1f);
        sRt.sizeDelta = new Vector2(-60f, 56f);
        sRt.anchoredPosition = new Vector2(30f, -14f);

        lineText = MakeText(box.transform, font, 44, FontStyle.Normal, Color.white);
        RectTransform lRt = lineText.rectTransform;
        lRt.anchorMin = Vector2.zero;
        lRt.anchorMax = Vector2.one;
        lRt.offsetMin = new Vector2(30f, 18f);
        lRt.offsetMax = new Vector2(-30f, -74f);
    }

    static Text MakeText(Transform parent, Font font, int size, FontStyle style, Color color)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.UpperLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);
        return t;
    }
}
