using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CostumeSelection : MonoBehaviour
{
    public GameObject[] Costume;
    public Avatar[] avatar;
    int selectedCostume_index;
    public Animator animator;
    void Start()
    {
        
    }
    private void OnEnable()
    {
        int x = PlayerPrefs.GetInt("Selectedcostume");
        SelectCostume(x);
    }
    public void SelectCostume(int index)
    {
        for (int i = 0; i < Costume.Length; i++)
        {
            Costume[i].SetActive(false);
        }
        Costume[index].SetActive(true);
        animator.avatar = avatar[index];
        PlayerPrefs.SetInt("Selectedcostume", index);
    }
    public void BuyCostume()
    {
        PlayerPrefs.SetInt("Selectedcostume",selectedCostume_index);
        SelectCostume(selectedCostume_index);
    }
}
