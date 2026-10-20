using UnityEngine;
using UnityEngine.UI;

public class AvatarUI : MonoBehaviour
{
    public static AvatarUI instance;

    [Header("Avatars (same order)")]
    public Button[] avatarButtons;
    public GameObject[] selectionHighlights;
    public Sprite[] avatarSprites;

    [Header("Save")]
    public Button saveButton;

    int selectedIndex;

    void Awake()
    {
        instance = this;
        AvatarData.RegisterSprites(avatarSprites);

        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(OnSave);
        }

        BindAvatarButtons();
    }

    void OnEnable()
    {
        AvatarData.RegisterSprites(avatarSprites);
        selectedIndex = AvatarData.IsCompleted ? AvatarData.SelectedIndex : 0;
        if (avatarSprites != null && avatarSprites.Length > 0)
            selectedIndex = Mathf.Clamp(selectedIndex, 0, avatarSprites.Length - 1);
        RefreshSelection();
    }

    void BindAvatarButtons()
    {
        if (avatarButtons == null)
            return;

        for (int i = 0; i < avatarButtons.Length; i++)
        {
            if (avatarButtons[i] == null)
                continue;

            int index = i;
            avatarButtons[i].onClick.RemoveAllListeners();
            avatarButtons[i].onClick.AddListener(() =>
            {
                AnalyticsEvents.Log("BF_MainMenu_Avatar" + (index + 1) + "_Select");
                SelectAvatar(index);
            });
        }
    }

    public void SelectAvatar(int index)
    {
        selectedIndex = index;
        if (avatarSprites != null && avatarSprites.Length > 0)
            selectedIndex = Mathf.Clamp(selectedIndex, 0, avatarSprites.Length - 1);
        RefreshSelection();
    }

    void RefreshSelection()
    {
        if (selectionHighlights != null)
        {
            for (int i = 0; i < selectionHighlights.Length; i++)
            {
                if (selectionHighlights[i] != null)
                    selectionHighlights[i].SetActive(i == selectedIndex);
            }
        }
    }

    void OnSave()
    {
        AnalyticsEvents.Log("BF_MainMenu_Avatar_Save");
        AvatarData.RegisterSprites(avatarSprites);
        AvatarData.Save(selectedIndex);

        if (MainMenuManager.instance != null)
            MainMenuManager.instance.OnAvatarSaved();
        else
            gameObject.SetActive(false);
    }
}
