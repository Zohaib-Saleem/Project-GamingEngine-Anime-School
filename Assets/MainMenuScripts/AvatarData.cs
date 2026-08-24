using UnityEngine;
using UnityEngine.UI;

public static class AvatarData
{
    const string IndexKey = "SelectedAvatarIndex";
    const string CompletedKey = "AvatarSelectionCompleted";

    public static Sprite[] Sprites;

    public static bool IsCompleted => PlayerPrefs.GetInt(CompletedKey, 0) == 1;

    public static int SelectedIndex => Mathf.Max(0, PlayerPrefs.GetInt(IndexKey, 0));

    public static void RegisterSprites(Sprite[] sprites)
    {
        if (sprites != null && sprites.Length > 0)
            Sprites = sprites;
    }

    public static void Save(int index)
    {
        if (Sprites != null && Sprites.Length > 0)
            index = Mathf.Clamp(index, 0, Sprites.Length - 1);
        else
            index = Mathf.Max(0, index);

        PlayerPrefs.SetInt(IndexKey, index);
        PlayerPrefs.SetInt(CompletedKey, 1);
        PlayerPrefs.Save();
    }

    public static Sprite GetSelectedSprite()
    {
        if (Sprites == null || Sprites.Length == 0)
            return null;

        int i = Mathf.Clamp(SelectedIndex, 0, Sprites.Length - 1);
        return Sprites[i];
    }

    public static void ApplyTo(Image image)
    {
        if (image == null)
            return;

        Sprite sprite = GetSelectedSprite();
        if (sprite != null)
            image.sprite = sprite;
    }
}
