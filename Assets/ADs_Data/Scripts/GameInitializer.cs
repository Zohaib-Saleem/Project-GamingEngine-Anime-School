using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitializer : MonoBehaviour
{
    public int NextSceneIndex = 1;
    // Start is called before the first frame update
    void Start()
    {
        Invoke(nameof(LoadScene), 10f);
    }

    void LoadScene()
    {
        SceneManager.LoadSceneAsync(NextSceneIndex);
    }
}
