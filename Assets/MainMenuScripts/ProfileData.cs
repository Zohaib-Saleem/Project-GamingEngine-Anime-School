using UnityEngine;

public static class ProfileData
{
    const string NameKey = "PlayerProfileName";
    const string AgeKey = "PlayerProfileAge";
    const string CompletedKey = "PlayerProfileCompleted";

    public static bool IsCompleted => PlayerPrefs.GetInt(CompletedKey, 0) == 1;

    public static string Name => PlayerPrefs.GetString(NameKey, string.Empty);

    public static string Age => PlayerPrefs.GetString(AgeKey, string.Empty);

    public static void Save(string name, string age)
    {
        PlayerPrefs.SetString(NameKey, name != null ? name.Trim() : string.Empty);
        PlayerPrefs.SetString(AgeKey, age != null ? age.Trim() : string.Empty);
        PlayerPrefs.SetInt(CompletedKey, 1);
        PlayerPrefs.Save();
    }
}
