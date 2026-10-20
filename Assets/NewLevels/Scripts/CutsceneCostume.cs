using UnityEngine;

/// <summary>
/// Shows the cutscene copy of the player in the same outfit the player is wearing.
/// Order of <see cref="costumes"/> must match CostumeSelection.Costume on the player.
/// </summary>
public class CutsceneCostume : MonoBehaviour
{
    public GameObject[] costumes;

    private void OnEnable()
    {
        int index = PlayerPrefs.GetInt("Selectedcostume", 1);
        if (index < 1) index = 1;
        Apply(index);
    }

    public void Apply(int index)
    {
        if (costumes == null || costumes.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, costumes.Length - 1);
        for (int i = 0; i < costumes.Length; i++)
        {
            if (costumes[i] != null)
                costumes[i].SetActive(i == index);
        }
    }
}
