using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using KikiNgao.SimpleBikeControl;

public class Level_Handler : MonoBehaviour
{
    public static Level_Handler instance;
    [System.Serializable]
    public class LevelSpawnPoint
    {
        public GameObject Level;
        public Transform playerSpawnPoint;
        public Transform CameraSp;
        public PlayableDirector Cutscene;
        public GameObject[] LeveltaskParticle;
        [Tooltip("Same index as LeveltaskParticle. Drag cutscene parent object (with PlayableDirector on it). Leave empty for no cutscene.")]
        public GameObject[] TaskCutscenes;
        [Tooltip("If true, after last task/cutscene a math test opens instead of instant complete.")]
        public bool hasMathTest;
        [Tooltip("Which MathTest_Manager.testSets entry to show (0 = original quiz, 1 = Mission 7, …).")]
        public int mathTestSetIndex;
        [Tooltip("Turn on school interior at mission start (for indoor missions like lab).")]
        public bool startInsideSchool;
        [Tooltip("Start this mission in the bedroom environment. Level 0 always starts there for backwards compatibility.")]
        public bool startInMyRoom;
        [Tooltip("Keep the bicycle available for this mission even when it is after the original bike level.")]
        public bool allowBike;
        public int RewardAmount;
        public float levelTime;
        [Tooltip("Same index as LeveltaskParticle. Shown in the single hint text.")]
        [TextArea] public string[] LevelHints;
        [Tooltip("Optional item held by the player (assign a bottle/prop). Enabled after a task cutscene, hidden when a later task is reached.")]
        public GameObject playerCarryItem;
        [Tooltip("Task index whose cutscene, when it finishes, enables playerCarryItem. -1 = never.")]
        public int showCarryAfterTask = -1;
        [Tooltip("Task index that hides playerCarryItem when the player reaches it. -1 = never.")]
        public int hideCarryOnReachTask = -1;
        [Tooltip("If true, playerCarryItem is visible as soon as the mission starts (after the intro).")]
        public bool showCarryAtStart;
    }

    public LevelSpawnPoint[] levelSpawnPoints;
    public TargetArrow ChArrow;
    public AnimeSchoolManager AnimeSchoolManager;
    public Image_Fader fader;
    public GameObject LevelLoading,MyRoom,LevelTimer;
    public GameObject HintPanel;
    public Text HintText;
    public Text levelTimetxt;
    public int levelNo,taskparticle_index=0;
    [HideInInspector] public int selectionLevelNo;
    PlayableDirector playingCutscene;
    bool gameplayStarted;
    bool isTaskCutscene;
    bool taskBusy;
    GameObject activeTaskCutsceneObject;
    Coroutine taskCutsceneWatch;

    LevelSpawnPoint CurrentLevel => levelSpawnPoints[levelNo];

    // Read by ObjectiveHUD / TaskPathGuide.
    public int CurrentTaskIndex => taskparticle_index;
    public int TaskCount => GetTaskCount();
    public bool IsGuiding => gameplayStarted && !taskBusy && !isTaskCutscene;
    public Transform CurrentTarget
    {
        get
        {
            GameObject point = GetTaskPoint(taskparticle_index);
            return point != null ? point.transform : null;
        }
    }

    private void Awake()
    {
        instance=this;
        selectionLevelNo = Mathf.Max(0, PlayerPrefs.GetInt("SelectedLevel", 0));
        int gameplayCount = levelSpawnPoints != null ? levelSpawnPoints.Length : 1;
        levelNo = LevelMap.ToGameplayIndex(selectionLevelNo, gameplayCount);
        SetCutsceneMode();
    }
    private void Start()
    {
        StartLevelCutScene();
    }
    private void OnDestroy()
    {
        StopPlayingCutscene();
    }
    public void FadeIn()
    {
        fader.FadeIn();
    }
    public void Fadeout()
    {
        fader.FadeOut();
    }
    void SetCutsceneMode()
    {
        Control_Handle(false);
        if (ChArrow != null)
            ChArrow.gameObject.SetActive(false);
        DeActiveHint();
        if (AnimeSchoolManager == null)
            return;
        if (AnimeSchoolManager.cycleCam != null)
            AnimeSchoolManager.cycleCam.SetActive(false);
        if (AnimeSchoolManager.CycleUi != null)
            AnimeSchoolManager.CycleUi.SetActive(false);
        if (AnimeSchoolManager.EnterRideBtn != null)
            AnimeSchoolManager.EnterRideBtn.SetActive(false);
    }
    public void Control_Handle(bool active)
    {
        if (AnimeSchoolManager == null)
            return;

        if(active)
        {
            Debug.Log("ActiveControl");
            AnimeSchoolManager.mainCh.SetActive(true);
            AnimeSchoolManager.mainChCamera.SetActive(true);
            AnimeSchoolManager.mainChUI.SetActive(true);
            SetCutsceneChromeVisible(true);
        }
        else
        {
            Debug.Log("DeactiveControl");
            AnimeSchoolManager.mainCh.SetActive(false);
            AnimeSchoolManager.mainChCamera.SetActive(false);
            AnimeSchoolManager.mainChUI.SetActive(false);
            SetCutsceneChromeVisible(false);
        }
    }

    void SetCutsceneChromeVisible(bool visible)
    {
        if (AnimeSchoolManager == null || AnimeSchoolManager.mainChUI == null)
            return;
        Transform panel = AnimeSchoolManager.mainChUI.transform.parent;
        if (panel == null)
            return;

        Transform pause = panel.Find("PauseBtn");
        if (pause != null)
            pause.gameObject.SetActive(visible);

        for (int i = 0; i < panel.childCount; i++)
        {
            Transform child = panel.GetChild(i);
            if (child != null && child.name.StartsWith("CASHBar"))
                child.gameObject.SetActive(visible);
        }
    }
    public void ActiveLevelLoading()
    {
        LevelLoading.SetActive(true);
    }
    public void StartLevelCutScene()
    {
        gameplayStarted = false;
        isTaskCutscene = false;
        taskBusy = false;
        SetCutsceneMode();

        bool useMyRoom = levelNo == 0 || CurrentLevel.startInMyRoom;
        if (useMyRoom)
        {
            MyRoom.SetActive(true);
            AnimeSchoolManager.Sun.SetActive(false);
        }
        else
        {
            if (MyRoom != null)
                MyRoom.SetActive(false);
            AnimeSchoolManager.Sun.SetActive(true);
        }

        for (int i = 0; i < levelSpawnPoints.Length; i++)
        {
            if (levelSpawnPoints[i].Level != null)
                levelSpawnPoints[i].Level.SetActive(i == levelNo);
        }

        Result_Manager.instance.RewardAmount = CurrentLevel.RewardAmount;

        if (levelNo >= 2 && !CurrentLevel.allowBike)
            AnimeSchoolManager.DisableBike();

        // Outdoor missions must force Exterior on — otherwise only skybox shows during the intro cutscene.
        if (SchoolZoneManager.instance != null)
        {
            if (CurrentLevel.startInsideSchool)
                SchoolZoneManager.instance.ShowInterior();
            else if (!useMyRoom)
                SchoolZoneManager.instance.ShowExterior();
        }

        if (levelNo >= 2)
        {
            CostumeSelection costumes = AnimeSchoolManager != null
                ? AnimeSchoolManager.mainCh.GetComponentInChildren<CostumeSelection>(true)
                : null;
            if (costumes != null && PlayerPrefs.GetInt("Selectedcostume", 0) < 1)
                costumes.SelectCostume(1);
        }

        PlayLevelCutscene();
        if (CurrentLevel.Cutscene == null)
            SpawnPlayerInlevel();
    }
    void PlayLevelCutscene()
    {
        StopPlayingCutscene();

        PlayableDirector cutscene = CurrentLevel.Cutscene;
        if (cutscene == null)
            return;

        isTaskCutscene = false;
        playingCutscene = cutscene;
        playingCutscene.stopped += OnCutsceneStopped;
        playingCutscene.time = 0;
        playingCutscene.Play();
        UIClickSound.PlayMusic(BgMusicType.Cutscene);
    }
    void PlayTaskCutscene(GameObject cutsceneObject)
    {
        StopPlayingCutscene();
        SetCutsceneMode();

        if (cutsceneObject == null)
        {
            AdvanceToNextTaskOrComplete();
            return;
        }

        activeTaskCutsceneObject = cutsceneObject;
        activeTaskCutsceneObject.SetActive(true);

        PlayableDirector director = cutsceneObject.GetComponent<PlayableDirector>();
        if (director == null)
            director = cutsceneObject.GetComponentInChildren<PlayableDirector>();

        if (director == null)
        {
            DisableActiveTaskCutsceneObject();
            AdvanceToNextTaskOrComplete();
            return;
        }

        isTaskCutscene = true;
        playingCutscene = director;
        playingCutscene.extrapolationMode = DirectorWrapMode.None;
        playingCutscene.stopped += OnCutsceneStopped;
        playingCutscene.time = 0;
        playingCutscene.Play();
        UIClickSound.PlayMusic(BgMusicType.Cutscene);
        if (taskCutsceneWatch != null)
            StopCoroutine(taskCutsceneWatch);
        taskCutsceneWatch = StartCoroutine(WatchTaskCutscene(director));
    }
    IEnumerator WatchTaskCutscene(PlayableDirector director)
    {
        yield return null;
        float limit = director != null ? (float)director.duration + 0.75f : 2f;
        if (limit < 1f)
            limit = 8f;

        float elapsed = 0f;
        while (isTaskCutscene && director != null && elapsed < limit)
        {
            bool objectOff = !director.gameObject.activeInHierarchy;
            bool finished = director.duration > 0 && director.time >= director.duration - 0.03f;
            bool stopped = director.state != PlayState.Playing;
            if (objectOff || finished || stopped)
                break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (isTaskCutscene)
            FinishTaskCutscene();
    }
    void FinishTaskCutscene()
    {
        if (!isTaskCutscene)
            return;

        isTaskCutscene = false;
        if (taskCutsceneWatch != null)
        {
            StopCoroutine(taskCutsceneWatch);
            taskCutsceneWatch = null;
        }

        Time.timeScale = 1f;
        if (playingCutscene != null)
        {
            playingCutscene.stopped -= OnCutsceneStopped;
            playingCutscene = null;
        }

        bool lastTask = taskparticle_index >= GetTaskCount() - 1;
        DisableCutsceneCameras(activeTaskCutsceneObject);
        DisableActiveTaskCutsceneObject();
        if (fader != null)
            fader.HideImmediate();
        EnablePlayerCamera();
        UIClickSound.PlayMusic(BgMusicType.Gameplay);
        if (CurrentLevel.showCarryAfterTask == taskparticle_index)
            SetCarryItemVisible(true);

        if (lastTask && !CurrentLevel.hasMathTest)
            CompleteIt();
        else
            AdvanceToNextTaskOrComplete();
    }
    void StopPlayingCutscene()
    {
        if (playingCutscene == null)
            return;

        playingCutscene.stopped -= OnCutsceneStopped;
        PlayableDirector director = playingCutscene;
        playingCutscene = null;
        director.Stop();
        DisableActiveTaskCutsceneObject();
    }
    void OnCutsceneStopped(PlayableDirector director)
    {
        director.stopped -= OnCutsceneStopped;
        if (playingCutscene == director)
            playingCutscene = null;

        Time.timeScale = 1f;

        if (isTaskCutscene)
        {
            FinishTaskCutscene();
            return;
        }

        UIClickSound.PlayMusic(BgMusicType.Gameplay);
        SpawnPlayerInlevel();
    }
    public void SpawnPlayerInlevel()
    {
        if (gameplayStarted)
            return;

        gameplayStarted = true;
        StopPlayingCutscene();
        UIClickSound.PlayMusic(BgMusicType.Gameplay);

        Transform playerSpawn = CurrentLevel.playerSpawnPoint;
        Transform cameraSpawn = CurrentLevel.CameraSp;
        SetupTaskPoints();
        AnimeSchoolManager.mainCh.transform.position= playerSpawn.position;
        AnimeSchoolManager.mainCh.transform.rotation= playerSpawn.rotation;
        Control_Handle(true);
        AnimeSchoolManager.mainChCamera.transform.position= cameraSpawn.position;
        AnimeSchoolManager.mainChCamera.GetComponent<ThirdPersonOrbitCamBasic>().SnapBehindTarget();
        AnimeSchoolManager.FlyBtn.gameObject.SetActive(false);
        ActivateHint();
    }
    void SetupTaskPoints()
    {
        taskparticle_index = 0;
        taskBusy = false;
        DisableAllTaskCutscenes();
        int count = GetTaskCount();
        for (int i = 0; i < count; i++)
        {
            GameObject point = GetTaskPoint(i);
            if (point != null)
                point.SetActive(i == 0);
        }

        EnableCurrentTaskArrow();
        HideAllCarryItems();
        if (CurrentLevel.showCarryAtStart)
            SetCarryItemVisible(true);
    }

    Transform GetPlayerTransform()
    {
        return AnimeSchoolManager != null && AnimeSchoolManager.mainCh != null
            ? AnimeSchoolManager.mainCh.transform
            : null;
    }

    void HideAllCarryItems()
    {
        Transform player = GetPlayerTransform();
        if (player != null)
            HandProp.HideAll(player);
        if (levelSpawnPoints == null)
            return;
        for (int i = 0; i < levelSpawnPoints.Length; i++)
        {
            if (levelSpawnPoints[i] != null && levelSpawnPoints[i].playerCarryItem != null)
                levelSpawnPoints[i].playerCarryItem.SetActive(false);
        }
    }

    void SetCarryItemVisible(bool visible)
    {
        if (CurrentLevel.playerCarryItem == null)
            return;
        Transform player = GetPlayerTransform();
        if (player != null)
            HandProp.SetNamed(player, visible, CurrentLevel.playerCarryItem.name);
        else
            CurrentLevel.playerCarryItem.SetActive(visible);
    }

    int GetTaskCount()
    {
        if (CurrentLevel.LeveltaskParticle == null)
            return 0;
        return CurrentLevel.LeveltaskParticle.Length;
    }
    public GameObject GetTaskPoint(int index)
    {
        if (CurrentLevel.LeveltaskParticle == null || index < 0 || index >= CurrentLevel.LeveltaskParticle.Length)
            return null;
        return CurrentLevel.LeveltaskParticle[index];
    }
    GameObject GetTaskCutsceneObject(int index)
    {
        if (CurrentLevel.TaskCutscenes == null || index < 0 || index >= CurrentLevel.TaskCutscenes.Length)
            return null;
        return CurrentLevel.TaskCutscenes[index];
    }
    void DisableCutsceneCameras(GameObject cutsceneRoot)
    {
        if (cutsceneRoot == null)
            return;

        Camera[] cams = cutsceneRoot.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cams.Length; i++)
            cams[i].enabled = false;
    }
    void EnablePlayerCamera()
    {
        if (AnimeSchoolManager == null || AnimeSchoolManager.mainChCamera == null)
            return;

        GameObject camObj = AnimeSchoolManager.mainChCamera;
        Transform t = camObj.transform;
        while (t != null)
        {
            t.gameObject.SetActive(true);
            t = t.parent;
        }
        camObj.SetActive(true);

        Camera[] cams = camObj.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cams.Length; i++)
        {
            cams[i].enabled = true;
            cams[i].depth = 50;
        }

        AudioListener listener = camObj.GetComponentInChildren<AudioListener>(true);
        if (listener != null)
            listener.enabled = true;
    }
    void DisableActiveTaskCutsceneObject()
    {
        if (activeTaskCutsceneObject == null)
            return;

        DisableCutsceneCameras(activeTaskCutsceneObject);
        activeTaskCutsceneObject.SetActive(false);
        activeTaskCutsceneObject = null;
    }
    void DisableAllTaskCutscenes()
    {
        if (CurrentLevel.TaskCutscenes == null)
            return;

        for (int i = 0; i < CurrentLevel.TaskCutscenes.Length; i++)
        {
            if (CurrentLevel.TaskCutscenes[i] != null)
                CurrentLevel.TaskCutscenes[i].SetActive(false);
        }
        activeTaskCutsceneObject = null;
    }
    void EnableCurrentTaskArrow()
    {
        GameObject point = GetTaskPoint(taskparticle_index);
        if (point == null)
            return;

        point.SetActive(true);
        if (ChArrow == null)
            return;

        ChArrow.gameObject.SetActive(true);
        ChArrow.SetTarget(point.transform);
    }
    bool IsBikeObject(GameObject point)
    {
        if (point == null || AnimeSchoolManager == null)
            return false;

        if (point.GetComponent<SimpleBike>() != null)
            return true;

        if (AnimeSchoolManager.Cycle != null &&
            (point == AnimeSchoolManager.Cycle || AnimeSchoolManager.Cycle.transform.IsChildOf(point.transform)))
            return true;

        if (AnimeSchoolManager.TriggeredCycle != null &&
            (point == AnimeSchoolManager.TriggeredCycle || AnimeSchoolManager.TriggeredCycle.transform.IsChildOf(point.transform)))
            return true;

        return false;
    }
    bool IsRidingCycle()
    {
        if (AnimeSchoolManager == null || AnimeSchoolManager.TriggeredCycle == null)
            return false;

        SimpleBike bike = AnimeSchoolManager.TriggeredCycle.GetComponent<SimpleBike>();
        if (bike == null || !bike.enabled || !AnimeSchoolManager.TriggeredCycle.activeInHierarchy)
            return false;

        return AnimeSchoolManager.CycleCharacter == null || AnimeSchoolManager.CycleCharacter.activeInHierarchy;
    }
    public void ReachCurrentTask()
    {
        if (taskBusy)
            return;

        taskBusy = true;
        if (CurrentLevel.hideCarryOnReachTask == taskparticle_index)
            SetCarryItemVisible(false);

        GameObject current = GetTaskPoint(taskparticle_index);
        if (current != null && !IsBikeObject(current))
            current.SetActive(false);

        DeActiveHint();

        GameObject taskCutscene = GetTaskCutsceneObject(taskparticle_index);
        if (taskCutscene != null && !IsRidingCycle())
        {
            PlayTaskCutscene(taskCutscene);
            return;
        }

        AdvanceToNextTaskOrComplete();
    }
    void AdvanceToNextTaskOrComplete()
    {
        taskparticle_index++;

        if (taskparticle_index >= GetTaskCount())
        {
            if (CurrentLevel.hasMathTest)
                StartMathTest();
            else
                CompleteAfterLastTask();
            return;
        }

        taskBusy = false;
        if (!IsRidingCycle())
            RestoreWalkAfterTaskCutscene();
        EnableCurrentTaskArrow();
        ActivateHint();
    }
    void StartMathTest()
    {
        if (ChArrow != null)
            ChArrow.gameObject.SetActive(false);
        DeActiveHint();

        ShowMathTestView();

        if (MathTest_Manager.instance != null)
            MathTest_Manager.instance.StartTest();
        else
        {
            Debug.LogWarning("MathTest_Manager missing in scene.");
            CompleteIt();
        }
    }
    public void ShowMathTestView()
    {
        if (AnimeSchoolManager == null)
            return;

        // Keep walk body off during quiz, but camera on so scene stays visible behind UI
        if (AnimeSchoolManager.mainCh != null)
            AnimeSchoolManager.mainCh.SetActive(false);
        if (AnimeSchoolManager.mainChUI != null)
            AnimeSchoolManager.mainChUI.SetActive(false);
        if (AnimeSchoolManager.mainChCamera != null)
            AnimeSchoolManager.mainChCamera.SetActive(true);
        if (AnimeSchoolManager.cycleCam != null)
            AnimeSchoolManager.cycleCam.SetActive(false);
        if (AnimeSchoolManager.CycleUi != null)
            AnimeSchoolManager.CycleUi.SetActive(false);
    }
    public void OnMathTestPassed()
    {
        CompleteIt();
    }
    public void OnMathTestFailed()
    {
        Result_Manager.instance.LevelFail();
    }
    void RestoreWalkAfterTaskCutscene()
    {
        Control_Handle(true);
        if (ChArrow != null)
            ChArrow.gameObject.SetActive(true);
        if (AnimeSchoolManager.FlyBtn != null)
            AnimeSchoolManager.FlyBtn.gameObject.SetActive(false);
        ThirdPersonOrbitCamBasic cam = AnimeSchoolManager.mainChCamera.GetComponent<ThirdPersonOrbitCamBasic>();
        if (cam != null)
            cam.SnapBehindTarget();
    }
    public void ActivateHint()
    {
        if (HintText == null)
            return;

        string[] hints = CurrentLevel.LevelHints;
        if (hints == null || taskparticle_index < 0 || taskparticle_index >= hints.Length)
        {
            DeActiveHint();
            return;
        }

        string msg = hints[taskparticle_index];
        if (string.IsNullOrEmpty(msg))
        {
            DeActiveHint();
            return;
        }

        HintText.text = msg;
        if (HintPanel != null)
            HintPanel.SetActive(true);
        else
            HintText.gameObject.SetActive(true);
    }
    public void DeActiveHint()
    {
        if (HintPanel != null)
            HintPanel.SetActive(false);
        else if (HintText != null)
            HintText.gameObject.SetActive(false);
    }
    public void SpawnPlayerOutsideHouse()
    {
        AnimeSchoolManager.SpawnOutSidePlayerHouse();
        MyRoom.SetActive(false);
        AnimeSchoolManager.Sun.SetActive(true);
    }
    void CompleteAfterLastTask()
    {
        PrepareResultScreen();
        if (fader != null)
            fader.HideImmediate();
        CompleteIt();
    }
    public void PrepareResultScreen()
    {
        Time.timeScale = 1f;
        if (ChArrow != null)
            ChArrow.gameObject.SetActive(false);
        DeActiveHint();

        if (AnimeSchoolManager == null)
            return;

        if (AnimeSchoolManager.mainCh != null)
            AnimeSchoolManager.mainCh.SetActive(false);
        if (AnimeSchoolManager.mainChUI != null)
            AnimeSchoolManager.mainChUI.SetActive(false);
        EnablePlayerCamera();
        if (AnimeSchoolManager.cycleCam != null)
            AnimeSchoolManager.cycleCam.SetActive(false);
        if (AnimeSchoolManager.CycleUi != null)
            AnimeSchoolManager.CycleUi.SetActive(false);
        if (fader != null)
            fader.HideImmediate();
    }
    public void CompleteIt()
    {
        Time.timeScale = 1f;
        PrepareResultScreen();
        if (fader != null)
            fader.HideImmediate();
        LevelComplete();
    }
    public void LevelComplete()
    {
        PrepareResultScreen();
        if (fader != null)
            fader.HideImmediate();

        int menuTotal = LevelMap.GetMenuLevelCount(15);
        // Mark this select slot complete, but do NOT advance SelectedLevel yet.
        // Restart must replay the same level; NextLevel advances.
        LevelProgress.MarkLevelComplete(selectionLevelNo, menuTotal);
        PlayerPrefs.SetInt("SelectedLevel", selectionLevelNo);
        PlayerPrefs.Save();

        Result_Manager.instance.LevelComplete();
        DeActiveHint();
    }
    public void NextLevel()
    {
        fader.FadeIn();
        Invoke(nameof(GoNextLevel), 0.5f);
    }

    void GoNextLevel()
    {
        Result_Manager.instance.HideCompleteUI();

        if (levelNo >= 0 && levelNo < levelSpawnPoints.Length)
            DeActiveHint();

        taskparticle_index = 0;
        taskBusy = false;

        selectionLevelNo = Mathf.Max(0, PlayerPrefs.GetInt("SelectedLevel", 0));
        int menuTotal = LevelMap.GetMenuLevelCount(15);
        int gameplayCount = levelSpawnPoints != null ? levelSpawnPoints.Length : 1;

        // Advance to next select slot only on Next
        selectionLevelNo++;
        PlayerPrefs.SetInt("SelectedLevel", selectionLevelNo);
        PlayerPrefs.Save();

        if (selectionLevelNo >= menuTotal)
        {
            if (PauseMenu.instance != null)
                PauseMenu.instance.ReloadFromFirstLevel();
            else
            {
                PlayerPrefs.SetInt("SelectedLevel", 0);
                selectionLevelNo = 0;
                levelNo = LevelMap.ToGameplayIndex(0, gameplayCount);
                fader.FadeOut();
            }
            return;
        }

        // After first 5 select levels: always full scene restart, then load mapped mission.
        if (selectionLevelNo >= gameplayCount)
        {
            if (PauseMenu.instance != null)
            {
                PauseMenu.instance.ReloadScene(selectionLevelNo);
                return;
            }

            PlayerPrefs.SetInt("SelectedLevel", selectionLevelNo);
            PlayerPrefs.Save();
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            return;
        }

        levelNo = LevelMap.ToGameplayIndex(selectionLevelNo, gameplayCount);
        StartLevelCutScene();
        if (levelSpawnPoints[levelNo].Cutscene == null)
            SpawnPlayerInlevel();
        fader.FadeOut();
    }
    public void Restartlevel()
    {
        if (PauseMenu.instance != null)
            PauseMenu.instance.Restart();
    }
    public void Home()
    {
        if (PauseMenu.instance != null)
            PauseMenu.instance.Home();
    }
}
