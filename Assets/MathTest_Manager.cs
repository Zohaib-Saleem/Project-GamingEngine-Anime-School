using UnityEngine;
using UnityEngine.UI;

public class MathTest_Manager : MonoBehaviour
{
    public static MathTest_Manager instance;

    [System.Serializable]
    public class MathQuestionUI
    {
        [Tooltip("0 = first option is correct, 1 = second option is correct")]
        public int correctOption;
        public Toggle optionA;
        public Toggle optionB;
    }

    [System.Serializable]
    public class MathTestSet
    {
        [Tooltip("Shown in the panel Image (question sheet). Leave empty to keep whatever is already assigned.")]
        public Sprite questionSheet;
        [Tooltip("Per-question correct option: 0 = A, 1 = B. Length should match questions.")]
        public int[] correctOptions;
    }

    public GameObject testPanel;
    [Tooltip("Original quiz clipboard Image (Level 2). Leave this as the default sheet.")]
    public Image questionSheetImage;
    [Tooltip("Mission 7 quiz clipboard Image. Assign a second Image in the scene; it is enabled only for that test.")]
    public Image mission7SheetImage;
    public MathQuestionUI[] questions = new MathQuestionUI[5];
    [Tooltip("Index 0 = original Level 2 quiz. Index 1 = Mission 7 quiz, etc.")]
    public MathTestSet[] testSets;
    public Button submitButton;
    public Button autoFillButton;
    public Text passingMarksText;
    public int passScore = 3;

    [Header("Mobile Touch")]
    [Tooltip("Extra invisible tap area around each toggle (canvas units). Shrinks automatically so toggles never overlap.")]
    public float toggleTouchPadding = 40f;

    bool active;
    bool bound;
    bool autoFilled;
    int activeSetIndex;

    private void Awake()
    {
        instance = this;
        if (testPanel != null)
            testPanel.SetActive(false);

        if (questionSheetImage == null && testPanel != null)
        {
            var t = testPanel.transform.Find("Image");
            if (t != null)
                questionSheetImage = t.GetComponent<Image>();
        }

        if (mission7SheetImage != null)
            mission7SheetImage.gameObject.SetActive(false);

        if (passingMarksText != null)
            passingMarksText.text = "PASSING MARKS : " + passScore;

        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(SubmitTest);
            submitButton.interactable = false;
        }

        if (autoFillButton != null)
        {
            autoFillButton.onClick.RemoveAllListeners();
            autoFillButton.onClick.AddListener(OnAutoFillClicked);
        }
    }

    private void OnEnable()
    {
        instance = this;
    }

    private void Update()
    {
        if (!active)
            return;

        UpdateSubmitButton();
    }

    void BindAllToggles()
    {
        if (bound || questions == null)
            return;

        bound = true;
        SetupTogglesForTouch();

        for (int i = 0; i < questions.Length; i++)
        {
            int index = i;
            MathQuestionUI q = questions[index];
            if (q == null)
                continue;

            if (q.optionA != null)
            {
                q.optionA.onValueChanged.RemoveAllListeners();
                q.optionA.onValueChanged.AddListener(isOn =>
                {
                    if (isOn) OnOptionSelected(index, 0);
                });
            }

            if (q.optionB != null)
            {
                q.optionB.onValueChanged.RemoveAllListeners();
                q.optionB.onValueChanged.AddListener(isOn =>
                {
                    if (isOn) OnOptionSelected(index, 1);
                });
            }
        }
    }

    // small check boxes are easy to miss with a finger: give each toggle a bigger hit area
    // and centre the tick in its box
    void SetupTogglesForTouch()
    {
        var toggles = new System.Collections.Generic.List<Toggle>();
        foreach (MathQuestionUI q in questions)
        {
            if (q == null)
                continue;
            if (q.optionA != null) toggles.Add(q.optionA);
            if (q.optionB != null) toggles.Add(q.optionB);
        }
        if (toggles.Count == 0)
            return;

        Canvas.ForceUpdateCanvases();
        Canvas canvas = toggles[0].GetComponentInParent<Canvas>();
        Transform canvasRoot = canvas != null ? canvas.rootCanvas.transform : null;

        // a slightly bigger move is still a tap, so a shaky finger doesn't cancel the click
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem != null && Screen.dpi > 0)
            eventSystem.pixelDragThreshold = Mathf.Max(eventSystem.pixelDragThreshold, Mathf.RoundToInt(Screen.dpi * 0.08f));

        foreach (Toggle toggle in toggles)
        {
            FitCheckmark(toggle);
            if (canvasRoot != null)
                AddTouchArea(toggle, GetSafePadding(toggle, toggles, canvasRoot), canvasRoot);
        }
    }

    // padding is limited to half the gap to the nearest other toggle so tap areas never overlap
    float GetSafePadding(Toggle toggle, System.Collections.Generic.List<Toggle> toggles, Transform canvasRoot)
    {
        float padding = toggleTouchPadding;
        Bounds own = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRoot, toggle.transform);
        foreach (Toggle other in toggles)
        {
            if (other == toggle)
                continue;
            Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRoot, other.transform);
            float dx = Mathf.Max(0f, Mathf.Max(own.min.x - b.max.x, b.min.x - own.max.x));
            float dy = Mathf.Max(0f, Mathf.Max(own.min.y - b.max.y, b.min.y - own.max.y));
            padding = Mathf.Min(padding, Mathf.Max(dx, dy) * 0.5f - 1f);
        }
        return Mathf.Max(0f, padding);
    }

    void AddTouchArea(Toggle toggle, float canvasPadding, Transform canvasRoot)
    {
        Transform existing = toggle.transform.Find("TouchArea");
        GameObject area = existing != null ? existing.gameObject
            : new GameObject("TouchArea", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        // stay out of any layout group on the toggle
        area.GetComponent<LayoutElement>().ignoreLayout = true;
        RectTransform rect = (RectTransform)area.transform;
        rect.SetParent(toggle.transform, false);
        rect.SetAsFirstSibling();

        // convert canvas units to the toggle's local units
        float scale = toggle.transform.lossyScale.x;
        float localPadding = scale > 0f ? canvasPadding * canvasRoot.lossyScale.x / scale : canvasPadding;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-localPadding, -localPadding);
        rect.offsetMax = new Vector2(localPadding, localPadding);

        // invisible but still catches taps, which bubble up to the toggle
        Image image = area.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = true;
    }

    void FitCheckmark(Toggle toggle)
    {
        Graphic check = toggle.graphic;
        Graphic box = toggle.targetGraphic;
        if (check == null || box == null || check == box)
            return;

        box.raycastTarget = true;

        // keep the tick's current size, only move it to the centre of the box
        RectTransform checkRect = check.rectTransform;
        Vector2 size = checkRect.rect.size;
        if (checkRect.parent != box.rectTransform)
            checkRect.SetParent(box.rectTransform, true);

        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = size;
        checkRect.anchoredPosition = Vector2.zero;
    }

    void OnOptionSelected(int questionIndex, int option)
    {
        MathQuestionUI q = questions[questionIndex];
        if (option == 0)
        {
            if (q.optionB != null && q.optionB.isOn)
                q.optionB.SetIsOnWithoutNotify(false);
        }
        else
        {
            if (q.optionA != null && q.optionA.isOn)
                q.optionA.SetIsOnWithoutNotify(false);
        }

        UpdateSubmitButton();
    }

    public void StartTest()
    {
        int setIndex = 0;
        var lh = Level_Handler.instance;
        if (lh != null && lh.levelSpawnPoints != null
            && lh.levelNo >= 0 && lh.levelNo < lh.levelSpawnPoints.Length)
            setIndex = lh.levelSpawnPoints[lh.levelNo].mathTestSetIndex;

        StartTest(setIndex);
    }

    public void StartTest(int setIndex)
    {
        active = true;
        autoFilled = false;
        ApplyTestSet(setIndex);

        if (Level_Handler.instance != null)
            Level_Handler.instance.ShowMathTestView();

        ShowTestPanel();
        ResetAnswers();
        BindAllToggles();
        ResetAutoFillButton();
        UpdateSubmitButton();
    }

    void ApplyTestSet(int setIndex)
    {
        activeSetIndex = setIndex;
        MathTestSet set = null;
        if (testSets != null && testSets.Length > 0)
        {
            if (setIndex < 0 || setIndex >= testSets.Length)
                setIndex = 0;
            set = testSets[setIndex];
        }

        ApplySheetImages(setIndex, set);

        if (set == null || set.correctOptions == null || questions == null)
            return;

        int n = Mathf.Min(questions.Length, set.correctOptions.Length);
        for (int i = 0; i < n; i++)
        {
            if (questions[i] != null)
                questions[i].correctOption = set.correctOptions[i];
        }
    }

    void ApplySheetImages(int setIndex, MathTestSet set)
    {
        bool useMission7 = setIndex == 1 && mission7SheetImage != null;

        if (questionSheetImage != null)
            questionSheetImage.gameObject.SetActive(!useMission7);

        if (mission7SheetImage != null)
            mission7SheetImage.gameObject.SetActive(useMission7);

        Image activeSheet = useMission7 ? mission7SheetImage : questionSheetImage;
        if (activeSheet != null && set != null && set.questionSheet != null)
            activeSheet.sprite = set.questionSheet;
    }

    void ShowTestPanel()
    {
        gameObject.SetActive(true);

        if (testPanel == null)
        {
            Debug.LogWarning("MathTest_Manager: assign Test Panel in Inspector.");
            return;
        }

        Transform current = testPanel.transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);
            current = current.parent;
        }

        testPanel.SetActive(true);
    }

    void ResetAnswers()
    {
        if (questions == null)
            return;

        for (int i = 0; i < questions.Length; i++)
        {
            if (questions[i] == null)
                continue;
            if (questions[i].optionA != null)
                questions[i].optionA.SetIsOnWithoutNotify(false);
            if (questions[i].optionB != null)
                questions[i].optionB.SetIsOnWithoutNotify(false);
        }
    }

    void ResetAutoFillButton()
    {
        if (autoFillButton == null)
            return;

        autoFillButton.gameObject.SetActive(true);
        autoFillButton.interactable = true;
    }

    void OnAutoFillClicked()
    {
        if (!active || autoFilled)
            return;

        if (adsController.instance != null)
        {
            adsController.instance.ShowRewarded(ApplyAutoFill, false);
            return;
        }

        ApplyAutoFill();
    }

    void ApplyAutoFill()
    {
        if (!active)
            return;

        autoFilled = true;
        FillCorrectAnswers();

        if (autoFillButton != null)
        {
            autoFillButton.interactable = false;
            autoFillButton.gameObject.SetActive(false);
        }

        if (submitButton != null)
        {
            submitButton.gameObject.SetActive(true);
            submitButton.interactable = true;
        }
    }

    void FillCorrectAnswers()
    {
        if (questions == null)
            return;

        for (int i = 0; i < questions.Length; i++)
        {
            MathQuestionUI q = questions[i];
            if (q == null)
                continue;

            bool pickA = q.correctOption == 0;
            if (q.optionA != null)
                q.optionA.SetIsOnWithoutNotify(pickA);
            if (q.optionB != null)
                q.optionB.SetIsOnWithoutNotify(!pickA);
        }
    }

    void UpdateSubmitButton()
    {
        if (submitButton == null)
            return;

        bool canSubmit = active && AllQuestionsAnswered();
        submitButton.interactable = canSubmit;
        if (canSubmit)
            submitButton.gameObject.SetActive(true);
    }

    bool AllQuestionsAnswered()
    {
        if (questions == null || questions.Length == 0)
            return false;

        int configuredQuestions = 0;
        for (int i = 0; i < questions.Length; i++)
        {
            MathQuestionUI q = questions[i];
            if (q == null || (q.optionA == null && q.optionB == null))
                continue;

            configuredQuestions++;
            bool aOn = q.optionA != null && q.optionA.isOn;
            bool bOn = q.optionB != null && q.optionB.isOn;
            if (!aOn && !bOn)
                return false;
        }

        return configuredQuestions > 0;
    }

    public void SubmitTest()
    {
        if (!active || !AllQuestionsAnswered())
            return;

        int correctCount = 0;
        for (int i = 0; i < questions.Length; i++)
        {
            MathQuestionUI q = questions[i];
            bool aOn = q.optionA != null && q.optionA.isOn;
            int selected = aOn ? 0 : 1;
            if (selected == q.correctOption)
                correctCount++;
        }

        FinishTest(correctCount);
    }

    void FinishTest(int correctCount)
    {
        active = false;
        if (submitButton != null)
            submitButton.interactable = false;
        if (autoFillButton != null)
            autoFillButton.interactable = false;
        if (testPanel != null)
            testPanel.SetActive(false);

        if (correctCount >= passScore)
            Level_Handler.instance.OnMathTestPassed();
        else
            Level_Handler.instance.OnMathTestFailed();
    }
}
