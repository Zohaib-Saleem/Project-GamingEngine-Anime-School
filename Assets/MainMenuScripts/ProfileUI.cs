using UnityEngine;
using UnityEngine.UI;

public class ProfileUI : MonoBehaviour
{
    public static ProfileUI instance;

    public InputField nameInput;
    public InputField ageInput;
    public Button saveButton;
    public Image avatarPreview;

    [Tooltip("Used when user saves without typing a name.")]
    public string defaultName = "PLAYER";

    void Awake()
    {
        instance = this;

        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(OnSave);
        }
    }

    void OnEnable()
    {
        LoadFields();
    }

    public void LoadFields()
    {
        if (nameInput != null)
            nameInput.text = ProfileData.Name;
        if (ageInput != null)
            ageInput.text = ProfileData.Age;

        AvatarData.ApplyTo(avatarPreview);
    }

    void OnSave()
    {
        string name = nameInput != null ? nameInput.text : string.Empty;
        string age = ageInput != null ? ageInput.text : string.Empty;

        name = name != null ? name.Trim() : string.Empty;
        age = age != null ? age.Trim() : string.Empty;

        if (string.IsNullOrEmpty(name))
            name = string.IsNullOrEmpty(defaultName) ? "PLAYER" : defaultName;

        if (nameInput != null)
            nameInput.text = name;

        ProfileData.Save(name, age);

        if (MainMenuManager.instance != null)
            MainMenuManager.instance.OnProfileSaved();
        else
            gameObject.SetActive(false);
    }
}
