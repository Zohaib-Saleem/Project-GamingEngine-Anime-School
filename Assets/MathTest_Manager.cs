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

    public GameObject testPanel;
    public MathQuestionUI[] questions = new MathQuestionUI[5];
    public Button submitButton;
    public Button autoFillButton;
    public Text passingMarksText;
    public int passScore = 3;

    bool active;
    bool bound;
    bool autoFilled;

    private void Awake()
    {
        instance = this;
        if (testPanel != null)
            testPanel.SetActive(false);

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
        active = true;
        autoFilled = false;

        if (Level_Handler.instance != null)
            Level_Handler.instance.ShowMathTestView();

        ShowTestPanel();
        ResetAnswers();
        BindAllToggles();
        ResetAutoFillButton();
        UpdateSubmitButton();
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
