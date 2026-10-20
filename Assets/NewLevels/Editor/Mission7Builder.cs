using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Builds Mission 7 "Study Date" into Levels/Level_6 (gameplay slot 6).
/// Starts at the cafe where Mission 6 ended. Re-running rebuilds from scratch.
/// Editor only.
/// </summary>
public static class Mission7Builder
{
    const int Slot = 6;
    const string AssetDir = "Assets/NewLevels/Mission7";
    const string ScenePath = "Assets/Scenes/GameScenes/Gameplay.unity";

    // Cafe patio (open area — not under the roof; same counter spot M8 uses).
    static readonly Vector3 CafeYuki = new Vector3(4.5f, 0.02f, 61.0f);
    static readonly Vector3 CafeHaru = new Vector3(6.0f, 0.02f, 62.3f);
    static readonly Vector3 DrinkPoint = new Vector3(4.50f, 0f, 60.80f);
    static readonly Vector3 VendorPos = new Vector3(6.00f, 0.05f, 61.80f);
    static readonly Vector3 DrinksYuki = new Vector3(4.50f, 0.05f, 60.80f);

    // School
    static readonly Vector3 EnterPoint = new Vector3(57.6f, 0f, 4.4f);
    static readonly Vector3 InsideSpawn = new Vector3(-0.4f, 0f, 253.6f);
    static readonly Vector3 ClassroomPoint = new Vector3(1.4f, 0f, 246.0f);

    // Front-row seats nearest the door. Students face +z (the teacher).
    // maleSit parks hips ~0.11 behind / ~0.66 above the root — aim hips at chair (5) ~z=241.50.
    // hip ≈ root + (0, 0.66, -0.11); chair (5) center z≈241.50.
    static readonly Vector3 HaruSeat = new Vector3(3.22f, 0.02f, 241.50f);
    static readonly Vector3 YukiSeat = new Vector3(4.41f, -0.06f, 241.62f);
    static readonly Vector3 ClassEntry = new Vector3(3.20f, 0.05f, 245.55f);
    static readonly Vector3 MeiPeek = new Vector3(1.75f, 0.05f, 246.20f);

    static StringBuilder log;

    // ---------------------------------------------------------------- entry points

    [MenuItem("Tools/Mission 7/Build")]
    public static void Build()
    {
        log = new StringBuilder();
        try
        {
            EnsureFolder();
            var lh = Object.FindObjectOfType<Level_Handler>(true);
            var template = lh.transform.Find("Level_3/School Gate particle (1)").gameObject;
            Transform level = EnsureLevel(lh);

            for (int i = level.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(level.GetChild(i).gameObject);
            level.name = "Level_6";

            var spawn = NewChild(level, "M7 Player Spawn", CafeYuki, Quaternion.identity);
            var camSpawn = NewChild(level, "M7 Camera Spawn", CafeYuki + new Vector3(0f, 1.65f, -3.4f), Quaternion.identity);
            var npcHolder = NewChild(level, "M7 Mission NPCs", Vector3.zero, Quaternion.identity);

            var t1 = MakeTaskPoint(template, level, "M7_01 Buy Drinks", DrinkPoint,
                new[] { CafeYuki, new Vector3(4.5f, 0, 59.6f), DrinkPoint });
            t1.AddComponent<TaskPointHit>();

            var t2 = MakeTaskPoint(template, level, "M7_02 Go Inside School", EnterPoint,
                new[]
                {
                    DrinkPoint, new Vector3(1.0f, 0, 59.0f), new Vector3(-1.4f, 0, 55.1f), new Vector3(-2.3f, 0, 43.9f),
                    new Vector3(-2.2f, 0, 25.6f), new Vector3(-2.6f, 0, 12.1f), new Vector3(0.6f, 0, 7.6f),
                    new Vector3(13.0f, 0, 5.6f), new Vector3(27.1f, 0, 4.6f), new Vector3(36.9f, 0, 4.8f),
                    new Vector3(54.6f, 0, 5.1f), EnterPoint
                });
            t2.AddComponent<SchoolZoneTrigger>().action = SchoolZoneTrigger.ZoneAction.EnterSchool;

            var t3 = MakeTaskPoint(template, level, "M7_03 Go To Classroom", ClassroomPoint,
                new[] { InsideSpawn, new Vector3(-0.2f, 0, 249.0f), ClassroomPoint });
            t3.AddComponent<TaskPointHit>();

            var standCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/NPC/Animators/NPC_Stand.controller");
            var sitCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/NPC/Animators/NPC_SitMale.controller");
            if (sitCtrl == null) sitCtrl = standCtrl;

            var vendor = MakeCharacter("GirlStudent", npcHolder, "Cafe Staff (waiting)", VendorPos, YawTo(VendorPos, DrinkPoint), standCtrl);
            var haruNpc = MakeCharacter("BoyStudent", npcHolder, "Haru (waiting in class)", HaruSeat, 0f, sitCtrl);
            AddMirror(npcHolder.gameObject, t1, vendor);
            AddMirror(npcHolder.gameObject, t3, haruNpc);

            var intro = BuildIntro(level);
            var drinks = BuildDrinksCutscene(level, npcHolder.gameObject);
            var study = BuildStudyCutscene(level);

            Undo.RecordObject(lh, "Mission 7");
            var L = lh.levelSpawnPoints[Slot];
            L.Level = level.gameObject;
            L.playerSpawnPoint = spawn;
            L.CameraSp = camSpawn;
            L.Cutscene = intro;
            L.LeveltaskParticle = new[] { t1, t2, t3 };
            L.TaskCutscenes = new GameObject[] { drinks.gameObject, null, study.gameObject };
            L.LevelHints = new[]
            {
                "BUY TWO DRINKS AT THE CAFE",
                "GO INSIDE THE SCHOOL BUILDING",
                "GO TO THE CLASSROOM",
            };
            L.playerCarryItem = null;
            L.showCarryAfterTask = -1;
            L.hideCarryOnReachTask = -1;
            L.showCarryAtStart = false;
            L.hasMathTest = true;
            L.mathTestSetIndex = 1; // Mission 7 question sheet (not the Level 2 quiz)
            L.startInsideSchool = false;
            L.startInMyRoom = false;
            L.allowBike = false;
            L.RewardAmount = 2500;
            L.levelTime = 200f;
            EditorUtility.SetDirty(lh);
            EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
            AssetDatabase.SaveAssets();
            log.AppendLine("BUILD OK slot=" + Slot + " level=" + level.name);
        }
        catch (System.Exception e) { log.AppendLine("ERR " + e); }
        System.IO.File.WriteAllText(OutPath("m7_build.txt"), log.ToString());
    }

    public static void PreparePlay()
    {
        PlayerPrefs.SetInt("SelectedLevel", 6);
        PlayerPrefs.Save();
        System.IO.File.WriteAllText(OutPath("m7_play.txt"), "SelectedLevel=6");
    }

    public static void ProbeSit()
    {
        var sb = new StringBuilder();
        try
        {
            var lh = Object.FindObjectOfType<Level_Handler>(true);
            var zone = Object.FindObjectOfType<SchoolZoneManager>(true);
            var level = lh.transform.Find("Level_6");
            level.gameObject.SetActive(true);
            lh.AnimeSchoolManager.env.SetActive(true);
            zone.Interior.SetActive(true);
            zone.Exterior.SetActive(false);
            var cut = level.Find("M7 Study Cutscene");
            cut.gameObject.SetActive(true);
            var dir = cut.GetComponent<PlayableDirector>();
            foreach (var cc in cut.GetComponentsInChildren<CutsceneCostume>(true)) cc.Apply(1);
            dir.RebuildGraph();
            dir.time = 5.0;
            dir.Evaluate();
            foreach (var m in cut.GetComponentsInChildren<CutsceneMover>(true)) m.Apply(5.0);
            var yuki = cut.Find("Yuki");
            sb.AppendLine("Yuki root=" + yuki.position.ToString("F3") + " yaw=" + yuki.eulerAngles.y.ToString("F1"));
            foreach (var t in yuki.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.IndexOf("Hips", System.StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("Pelvis", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    sb.AppendLine("  hip " + t.name + " " + t.position.ToString("F3"));
            }
            sb.AppendLine("target chair (4) ~ (4.41, 0.45-0.60, 241.50) desk z~242.04");
            sb.AppendLine("Haru root=" + cut.Find("Haru").position.ToString("F3"));
        }
        catch (System.Exception e) { sb.AppendLine("ERR " + e); }
        System.IO.File.WriteAllText(OutPath("m7_sit.txt"), sb.ToString());
    }

    [MenuItem("Tools/Mission 7/Save Scene")]
    public static void SaveScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        bool ok = scene.path == ScenePath && EditorSceneManager.SaveScene(scene);
        System.IO.File.WriteAllText(OutPath("m7_save.txt"), ok ? "SAVED" : "NOT SAVED (" + scene.path + ")");
    }

    [MenuItem("Tools/Mission 7/List Clips")]
    public static void ListClips()
    {
        var sb = new StringBuilder();
        foreach (var n in new[] { "femaleSit", "maleSit", "VendorIdle", "eating", "FemaleIdle", "Idle", "IP_Mvm_Walk_Loop", "IP_Mvm_Jog_Loop", "male_talk_stand", "Thoughtful Head Nod", "f02_greet_03" })
        {
            var c = FindClip(n);
            sb.AppendLine(n + " -> " + (c ? c.name + " human=" + c.humanMotion + " len=" + c.length.ToString("F2") : "MISSING"));
        }
        foreach (var path in new[] {
            "Assets/Animations/femaleSit.fbx", "Assets/Animations/maleSit.fbx",
            "Assets/Animations/f02@f02_sit_01.fbx", "Assets/Animations/m01@sit_01.fbx" })
        {
            sb.AppendLine("FILE " + path);
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var c = a as AnimationClip;
                if (c != null) sb.AppendLine("  clip '" + c.name + "' human=" + c.humanMotion + " len=" + c.length.ToString("F2") + " legacy=" + c.legacy);
            }
        }
        System.IO.File.WriteAllText(OutPath("m7_clips.txt"), sb.ToString());
    }

    // ---------------------------------------------------------------- cutscenes

    static PlayableDirector BuildIntro(Transform level)
    {
        var cs = new Cutscene(level, "M7 Intro Cutscene", AssetDir + "/M7_Intro.playable", 16f, true);
        float yukiYaw = YawTo(CafeYuki, CafeHaru);
        var yuki = cs.AddYuki("Yuki", CafeYuki, yukiYaw);
        var haru = cs.AddCharacter("BoyStudent", "Haru", CafeHaru, YawTo(CafeHaru, CafeYuki));

        cs.Move(yuki.mover, new[] { 0f, 9.6f, 11.0f, 16f },
            new[] { CafeYuki, CafeYuki, CafeYuki, CafeYuki },
            new[] { yukiYaw, yukiYaw, 165f, 165f });
        Vector3 haruLeave = new Vector3(2.4f, 0.02f, 56.8f);
        float haruLeaveYaw = YawTo(CafeHaru, haruLeave);
        var haruMove = cs.Move(haru.mover, new[] { 0f, 9.6f, 16f },
            new[] { CafeHaru, CafeHaru, haruLeave },
            new[] { YawTo(CafeHaru, CafeYuki), haruLeaveYaw, haruLeaveYaw });
        haruMove.smooth = false;
        haruMove.lockChildLocalYaw = true;
        haruMove.lockChildLocalPosition = true;

        cs.Anim(yuki, "FemaleIdle", 0f, 3.4f, true);
        cs.Anim(yuki, "male_talk_stand", 3.4f, 3.2f, true);
        cs.Anim(yuki, "FemaleIdle", 6.6f, 4.8f, true);
        cs.Anim(yuki, "f02_greet_03", 11.4f, 3.2f, false);
        cs.Anim(yuki, "FemaleIdle", 14.6f, 1.4f, true);

        cs.Anim(haru, "male_talk_stand", 0f, 3.4f, true);
        cs.Anim(haru, "Idle", 3.4f, 3.2f, true);
        cs.Anim(haru, "male_talk_stand", 6.6f, 3.0f, true);
        // Female runner walk looks wrong on BoyStudent — use the male 3rdPerson walk.
        cs.Anim(haru, "walk_fwd", 9.6f, 6.4f, true);

        Vector3 haruHead = CafeHaru + Vector3.up * 1.6f, yukiHead = CafeYuki + Vector3.up * 1.42f;
        Vector3 cafeMid = (haruHead + yukiHead) * 0.5f - Vector3.up * 0.25f;
        cs.Shot("Cam A Two Shot", 0f, 3.4f, new Vector3(3.20f, 1.55f, 62.40f), new Vector3(3.40f, 1.50f, 62.05f), cafeMid);
        cs.Shot("Cam B Yuki Close", 3.4f, 3.2f, OverShoulder(CafeHaru, CafeYuki, 0.7f, -0.35f, 1.62f), OverShoulder(CafeHaru, CafeYuki, 0.55f, -0.35f, 1.62f), yukiHead);
        cs.Shot("Cam C Haru Close", 6.6f, 3.2f, OverShoulder(CafeYuki, CafeHaru, 0.7f, 0.35f, 1.6f), OverShoulder(CafeYuki, CafeHaru, 0.55f, 0.35f, 1.6f), haruHead);
        Vector3 haruMid = (CafeHaru + haruLeave) * 0.5f + Vector3.up * 1.45f;
        cs.Shot("Cam D Haru Leaves", 9.8f, 2.6f,
            new Vector3(3.15f, 1.58f, 62.55f), new Vector3(3.35f, 1.52f, 61.35f),
            cafeMid, (yukiHead * 0.45f + haruMid * 0.55f));
        cs.Shot("Cam E Yuki Date", 12.4f, 3.6f,
            OverShoulder(CafeHaru, CafeYuki, 0.85f, -0.25f, 1.55f),
            OverShoulder(CafeHaru, CafeYuki, 0.70f, -0.20f, 1.52f),
            yukiHead);

        cs.Line(0.3f, 3.0f, "Haru", "It's too noisy here. Let's study in the classroom.", "M7_01_Haru");
        cs.Line(3.5f, 3.0f, "Yuki", "Okay... drinks first. My treat.", "M7_02_Yuki");
        cs.Line(6.7f, 3.0f, "Haru", "I'll save us seats. Don't take too long!", "M7_03_Haru");
        cs.Line(9.9f, 2.2f, "Haru", "See you there!", "M7_04_Haru");
        cs.Line(12.4f, 3.4f, "Yuki", "...A study date. With him.", "M7_05_Yuki");

        log.AppendLine("LEAVE SHOTS SPLIT camD=Haru walk camE=Yuki date");
        cs.HideAmbient("SchoolNPCs/OutsideSchool");
        return cs.Finish();
    }

    static PlayableDirector BuildDrinksCutscene(Transform level, GameObject missionNpcs)
    {
        // Patio counter buy — same open-air framing as M8 cafe (not under the roof).
        var cs = new Cutscene(level, "M7 Drinks Cutscene", AssetDir + "/M7_Drinks.playable", 16f, false);
        Vector3 yukiPos = DrinksYuki;
        Vector3 staffPos = VendorPos;
        float yukiYaw = YawTo(yukiPos, staffPos);
        float staffYaw = YawTo(staffPos, yukiPos);

        var yuki = cs.AddYuki("Yuki", yukiPos, yukiYaw);
        var staff = cs.AddCharacter("GirlStudent", "Cafe Staff", staffPos, staffYaw);

        var yukiMove = cs.Move(yuki.mover, new[] { 0f, 16f }, new[] { yukiPos, yukiPos }, new[] { yukiYaw, yukiYaw });
        yukiMove.lockChildLocalPosition = true;
        var staffMove = cs.Move(staff.mover, new[] { 0f, 16f }, new[] { staffPos, staffPos }, new[] { staffYaw, staffYaw });
        staffMove.lockChildLocalPosition = true;

        cs.Anim(yuki, "male_talk_stand", 0.2f, 3.3f, true);
        cs.Anim(yuki, "FemaleIdle", 3.5f, 4.0f, true);
        cs.Anim(yuki, "male_talk_stand", 7.5f, 3.5f, true);
        cs.Anim(yuki, "FemaleIdle", 11.0f, 3.0f, true);
        cs.Anim(yuki, "f02_greet_03", 14.0f, 2.0f, false);

        cs.Anim(staff, "FemaleIdle", 0f, 3.5f, true);
        cs.Anim(staff, "male_talk_stand", 3.5f, 4.0f, true);
        cs.Anim(staff, "FemaleIdle", 7.5f, 3.6f, true);
        cs.Anim(staff, "male_talk_stand", 11.1f, 2.5f, true);
        cs.Anim(staff, "f02_greet_03", 13.6f, 2.4f, false);

        Vector3 mid = (yukiPos + staffPos) * 0.5f + Vector3.up * 1.35f;
        Vector3 staffHead = staffPos + Vector3.up * 1.45f;
        Vector3 yukiHead = yukiPos + Vector3.up * 1.40f;
        // Same patio framing family as M8 cafe — readable faces, outdoor light, no roof clip.
        cs.Shot("Cam A Patio Two Shot", 0f, 3.5f,
            new Vector3(3.10f, 1.55f, 62.40f), new Vector3(3.35f, 1.50f, 62.10f), mid);
        cs.Shot("Cam B Yuki Order", 3.5f, 4.0f,
            new Vector3(3.80f, 1.52f, 62.30f), new Vector3(3.95f, 1.48f, 62.05f), yukiHead);
        cs.Shot("Cam C Staff Reply", 7.5f, 3.5f,
            new Vector3(3.55f, 1.52f, 61.10f), new Vector3(3.70f, 1.48f, 61.25f), staffHead);
        cs.Shot("Cam D Two Teas Hand Off", 11.0f, 5.0f,
            new Vector3(3.40f, 1.50f, 62.70f), new Vector3(3.60f, 1.45f, 62.35f), mid);

        cs.HideTargets(missionNpcs);
        cs.Line(0.3f, 3.0f, "Yuki", "Two iced teas, please.", "M7_06_Yuki");
        cs.Line(3.6f, 3.6f, "Staff", "Study fuel? Coming right up.", "M7_07_Staff");
        cs.Line(7.6f, 3.2f, "Yuki", "Haru's waiting in the classroom. Don't make me run.", "M7_08_Yuki");
        cs.Line(11.2f, 2.6f, "Staff", "Here — two cups. Don't spill them!", "M7_09_Staff");
        cs.Line(14.0f, 1.8f, "Yuki", "Thanks. Classroom time.", "M7_10_Yuki");

        cs.HideAmbient("SchoolNPCs/OutsideSchool");
        return cs.Finish();
    }

    static PlayableDirector BuildStudyCutscene(Transform level)
    {
        var cs = new Cutscene(level, "M7 Study Cutscene", AssetDir + "/M7_Study.playable", 24f, false);
        var yuki = cs.AddYuki("Yuki", ClassEntry, YawTo(ClassEntry, YukiSeat));
        var haru = cs.AddCharacter("BoyStudent", "Haru", HaruSeat, 0f);
        var mei = cs.AddCharacter("GirlStudent", "Mei", MeiPeek, YawTo(MeiPeek, YukiSeat), false);
        cs.Show(mei.mover.gameObject, 17.2f, 6.8f, "Mei Visible");

        // Face the walk the whole way, stop + idle, then turn to the teacher and sit.
        float walkYaw = YawTo(ClassEntry, YukiSeat);
        Vector3 yukiStand = new Vector3(YukiSeat.x, 0.02f, YukiSeat.z);
        var yukiMove = cs.Move(yuki.mover, new[] { 0f, 3.1f, 3.7f, 4.2f },
            new[] { ClassEntry, yukiStand, yukiStand, YukiSeat },
            new[] { walkYaw, walkYaw, 0f, 0f });
        yukiMove.smooth = false;
        yukiMove.lockChildLocalYaw = true;

        cs.Anim(yuki, "IP_Mvm_Walk_Loop", 0f, 3.1f, true);
        cs.Anim(yuki, "FemaleIdle", 3.1f, 0.6f, true);
        cs.Anim(yuki, SitClip("f02_sit_01", "FemaleIdle"), 3.9f, 20.1f, true);

        // sit_01 (m01 pack) clips through these chairs; NPC_SitMale uses maleSit "Take 001".
        var maleSit = FindClipAt("Assets/Animations/maleSit.fbx");
        if (maleSit != null) cs.AnimClip(haru, maleSit, 0f, 24f, true);
        else cs.Anim(haru, SitClip("sit_01", "Idle"), 0f, 24f, true);

        cs.Anim(mei, "male_talk_stand", 17.2f, 4.2f, true);
        cs.Anim(mei, "f02_greet_03", 21.4f, 2.6f, false);

        Vector3 yukiHead = YukiSeat + Vector3.up * 1.18f;
        Vector3 haruHead = HaruSeat + Vector3.up * 1.22f;
        Vector3 meiHead = MeiPeek + Vector3.up * 1.45f;
        Vector3 sitMid = (YukiSeat + HaruSeat) * 0.5f + Vector3.up * 1.05f;

        cs.Shot("Cam A Walk In", 0f, 3.6f, new Vector3(6.20f, 1.58f, 245.85f), new Vector3(5.75f, 1.50f, 244.55f), new Vector3(3.90f, 1.08f, 243.15f));
        cs.Shot("Cam B Two Shot", 3.6f, 3.8f, new Vector3(5.85f, 1.45f, 243.55f), new Vector3(5.6f, 1.4f, 243.35f), sitMid);
        cs.Shot("Cam C Yuki Close", 7.4f, 3.8f, new Vector3(4.55f, 1.22f, 243.20f), new Vector3(4.50f, 1.20f, 243.05f), yukiHead);
        cs.Shot("Cam D Haru Close", 11.2f, 4.2f, new Vector3(3.15f, 1.25f, 243.25f), new Vector3(3.18f, 1.22f, 243.05f), haruHead);
        cs.Shot("Cam E Yuki Close", 15.4f, 1.8f, new Vector3(4.58f, 1.22f, 243.15f), new Vector3(4.52f, 1.20f, 243.00f), yukiHead);
        cs.Shot("Cam F Mei Peek", 17.2f, 3.4f, new Vector3(1.05f, 1.50f, 247.85f), new Vector3(1.35f, 1.48f, 247.35f), meiHead + new Vector3(0.4f, -0.1f, -0.2f));
        cs.Shot("Cam G Two Shot", 20.6f, 3.4f, new Vector3(5.7f, 1.42f, 243.4f), new Vector3(5.5f, 1.38f, 243.2f), sitMid);

        cs.Hand(yuki, 3.7f, 20.3f, "Book", "Notebook");
        cs.Hand(haru, 0f, 24f, "Book", "Notebook");

        cs.Line(0.4f, 3.0f, "Haru", "You made it. I, uh, saved the seat next to me.", "M7_11_Haru");
        cs.Line(3.7f, 3.5f, "Yuki", "I got us iced tea. So... what's the problem?", "M7_12_Yuki");
        cs.Line(7.5f, 3.6f, "Haru", "This one. I keep getting the same answer, and it's always wrong.", "M7_13_Haru");
        cs.Line(11.3f, 4.0f, "Yuki", "You're dropping the minus. Look — like this.", "M7_14_Yuki");
        cs.Line(15.5f, 1.6f, "Haru", "Oh. ...Oh!", "M7_15_Haru");
        cs.Line(17.3f, 3.2f, "Mei", "Ooh~ a study DATE. Don't mind me!", "M7_16_Mei");
        cs.Line(20.7f, 1.5f, "Yuki", "MEI!", "M7_17_Yuki");
        cs.Line(22.3f, 1.6f, "Haru", "Could you quiz me? Like a real test.", "M7_18_Haru");

        cs.HideAmbient("SchoolNPCs/InsideSchool");
        return cs.Finish();
    }

    static string SitClip(string preferred, string fallback)
    {
        if (FindClip(preferred) != null) return preferred;
        log.AppendLine("sit fallback " + preferred + " -> " + fallback);
        return fallback;
    }

    // ---------------------------------------------------------------- cutscene helper

    class Actor
    {
        public Transform mover;
        public List<Animator> animators = new List<Animator>();
    }

    class Cutscene
    {
        readonly GameObject root;
        readonly PlayableDirector director;
        readonly TimelineAsset timeline;
        readonly CutsceneDialogue dialogue;
        readonly List<CutsceneDialogue.Line> lines = new List<CutsceneDialogue.Line>();
        readonly float duration;
        readonly Dictionary<Animator, AnimationTrack> tracks = new Dictionary<Animator, AnimationTrack>();

        public Cutscene(Transform parent, string name, string assetPath, float duration, bool startActive)
        {
            this.duration = duration;
            root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.SetActive(startActive);

            AssetDatabase.DeleteAsset(assetPath);
            timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, assetPath);
            timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            timeline.fixedDuration = duration;

            director = root.AddComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;

            var listener = NewChild(root.transform, "Cutscene Audio Listener", Vector3.zero, Quaternion.identity).gameObject.AddComponent<AudioListener>();
            listener.enabled = false;
            dialogue = root.AddComponent<CutsceneDialogue>();
            dialogue.director = director;
            dialogue.cameraListener = listener;
        }

        public Actor AddYuki(string name, Vector3 pos, float yaw)
        {
            var actor = new Actor();
            actor.mover = NewChild(root.transform, name, pos, Quaternion.Euler(0, yaw, 0));
            var player = Object.FindObjectOfType<Level_Handler>(true).AnimeSchoolManager.mainCh;
            var sel = player.GetComponentInChildren<CostumeSelection>(true);
            var costumes = new List<GameObject>();
            foreach (var src in sel.Costume)
            {
                var copy = Object.Instantiate(src, actor.mover);
                copy.name = src.name;
                copy.transform.localPosition = Vector3.zero;
                copy.transform.localRotation = Quaternion.identity;
                copy.transform.localScale = src.transform.lossyScale;
                foreach (var c in copy.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                var anim = copy.GetComponent<Animator>();
                if (anim == null) anim = copy.AddComponent<Animator>();
                if (anim.avatar == null) anim.avatar = sel.avatar[costumes.Count];
                Prepare(anim);
                actor.animators.Add(anim);
                costumes.Add(copy);
            }
            actor.mover.gameObject.AddComponent<CutsceneCostume>().costumes = costumes.ToArray();
            HandProp.HideAll(actor.mover);
            ShowOnlyDuringCutscene(actor);
            return actor;
        }

        void ShowOnlyDuringCutscene(Actor actor)
        {
            actor.mover.gameObject.SetActive(false);
            Show(actor.mover.gameObject, 0f, duration, actor.mover.name + " Visible");
        }

        public Actor AddCharacter(string prefabName, string name, Vector3 pos, float yaw, bool showWholeTime = true)
        {
            var actor = new Actor();
            actor.mover = NewChild(root.transform, name, pos, Quaternion.Euler(0, yaw, 0));
            var body = MakeCharacter(prefabName, actor.mover, prefabName, pos, yaw, null);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            actor.animators.Add(body.GetComponent<Animator>());
            HandProp.HideAll(actor.mover);
            if (showWholeTime) ShowOnlyDuringCutscene(actor);
            else actor.mover.gameObject.SetActive(false);
            return actor;
        }

        static void Prepare(Animator anim)
        {
            anim.runtimeAnimatorController = null;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        public void Anim(Actor actor, string clipName, float start, float length, bool loop)
        {
            var clip = FindClip(clipName);
            if (clip == null) { log.AppendLine("MISSING CLIP " + clipName); return; }
            AnimClip(actor, clip, start, length, loop);
        }

        public void AnimClip(Actor actor, AnimationClip clip, float start, float length, bool loop)
        {
            if (clip == null) { log.AppendLine("MISSING CLIP asset"); return; }
            foreach (var anim in actor.animators)
            {
                if (!tracks.TryGetValue(anim, out var track))
                {
                    track = timeline.CreateTrack<AnimationTrack>(null, actor.mover.name + " / " + anim.name);
                    // Keep the actor root where CutsceneMover put it — scene offsets shove sit poses into chairs.
                    track.trackOffset = TrackOffset.ApplyTransformOffsets;
                    director.SetGenericBinding(track, anim);
                    tracks[anim] = track;
                }
                var tc = track.CreateClip(clip);
                tc.start = start;
                tc.duration = length;
                tc.displayName = clip.name;
                var asset = (AnimationPlayableAsset)tc.asset;
                asset.loop = loop ? AnimationPlayableAsset.LoopMode.On : AnimationPlayableAsset.LoopMode.Off;
                asset.removeStartOffset = true;
            }
        }

        public CutsceneMover Move(Transform target, float[] times, Vector3[] positions, float[] yaws)
        {
            var m = target.gameObject.AddComponent<CutsceneMover>();
            m.director = director;
            m.times = times;
            m.positions = positions;
            m.rotations = yaws.Select(y => Quaternion.Euler(0, y, 0)).ToArray();
            return m;
        }

        public void Shot(string name, float start, float length, Vector3 from, Vector3 to, Vector3 lookAt)
        {
            Shot(name, start, length, from, to, lookAt, lookAt);
        }

        public void Shot(string name, float start, float length, Vector3 from, Vector3 to, Vector3 lookFrom, Vector3 lookTo)
        {
            var cam = NewChild(root.transform, name, from, Quaternion.LookRotation(lookFrom - from)).gameObject;
            var c = cam.AddComponent<Camera>();
            c.fieldOfView = 45f;
            c.nearClipPlane = 0.05f;
            c.depth = 20;
            cam.SetActive(false);
            var m = cam.AddComponent<CutsceneMover>();
            m.director = director;
            m.times = new[] { start, start + length };
            m.positions = new[] { from, to };
            m.rotations = new[] { Quaternion.LookRotation(lookFrom - from), Quaternion.LookRotation(lookTo - to) };
            m.smooth = false;
            m.yawDegrees = false;
            Show(cam, start, length, name);
        }

        public GameObject Prop(string name, Vector3 pos, float yaw, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos + Vector3.up * 0.012f;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = new Vector3(0.21f, 0.024f, 0.29f);
            go.GetComponent<Renderer>().sharedMaterial = SolidMaterial("Notebook", color);
            go.SetActive(false);
            return go;
        }

        public GameObject Cup(string name, Vector3 pos, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.08f, 0.085f, 0.08f);
            go.GetComponent<Renderer>().sharedMaterial = SolidMaterial("Cup", color);
            go.SetActive(false);
            return go;
        }

        public void Hand(Actor actor, float start, float length, params string[] names)
        {
            if (actor == null || actor.mover == null)
                return;
            GameObject[] gos = HandPropBuild.EnsureAll(actor.mover, log, names);
            for (int i = 0; i < gos.Length; i++)
                Show(gos[i], start, length, actor.mover.name + " " + gos[i].name + " " + i);
        }

        public void Show(GameObject go, float start, float length, string trackName = null)
        {
            var track = timeline.CreateTrack<ActivationTrack>(null, trackName ?? go.name);
            track.postPlaybackState = ActivationTrack.PostPlaybackState.Inactive;
            var clip = track.CreateDefaultClip();
            clip.start = start;
            clip.duration = length;
            clip.displayName = trackName ?? go.name;
            director.SetGenericBinding(track, go);
        }

        public void Line(float start, float length, string speaker, string text, string voiceId = null)
        {
            var line = new CutsceneDialogue.Line { start = start, duration = length, speaker = speaker, text = text };
            MissionVocals.Bind(line, voiceId, log);
            lines.Add(line);
        }

        public void HideAmbient(string path)
        {
            var target = GameObject.Find(path.Split('/')[0])?.transform.Find(path.Substring(path.IndexOf('/') + 1));
            if (target == null) { log.AppendLine("ambient not found " + path); return; }
            HideTargets(target.gameObject, "Hide Ambient NPCs");
        }

        public void HideTargets(GameObject target, string trackName = null)
        {
            if (target == null) return;
            var holder = NewChild(root.transform, trackName ?? ("Hide " + target.name), Vector3.zero, Quaternion.identity).gameObject;
            holder.AddComponent<CutsceneVisibilityOverride>().targets = new[] { target };
            holder.SetActive(false);
            Show(holder, 0f, duration, trackName ?? ("Hide " + target.name));
        }

        public PlayableDirector Finish()
        {
            MissionVocals.PackAndRemap(lines, timeline, director, root.transform, log);
            dialogue.lines = lines.ToArray();
            EditorUtility.SetDirty(timeline);
            log.AppendLine("cutscene " + root.name + " tracks=" + timeline.outputTrackCount + " lines=" + lines.Count);
            return director;
        }
    }

    // ---------------------------------------------------------------- building blocks

    static Transform EnsureLevel(Level_Handler lh)
    {
        var so = new SerializedObject(lh);
        var prop = so.FindProperty("levelSpawnPoints");
        if (prop.arraySize <= Slot)
        {
            prop.arraySize = Slot + 1;
            so.ApplyModifiedProperties();
            log.AppendLine("expanded slots to " + (Slot + 1));
        }

        Transform level = lh.transform.Find("Level_6");
        if (level == null)
        {
            var go = new GameObject("Level_6");
            go.transform.SetParent(lh.transform, false);
            go.SetActive(false);
            level = go.transform;
            log.AppendLine("created Level_6");
        }
        return level;
    }

    static GameObject MakeTaskPoint(GameObject template, Transform parent, string name, Vector3 pos, Vector3[] path)
    {
        var go = Object.Instantiate(template, parent);
        go.name = name;
        go.SetActive(false);
        go.transform.position = pos;
        foreach (var z in go.GetComponents<SchoolZoneTrigger>()) Object.DestroyImmediate(z);

        var spawner = go.GetComponentInChildren<AdvancedCurvedPathArrowSpawner>(true);
        var pathTr = spawner.transform;
        for (int i = pathTr.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(pathTr.GetChild(i).gameObject);
        pathTr.position = pos;
        var wps = new Transform[path.Length];
        for (int i = 0; i < path.Length; i++)
            wps[i] = NewChild(pathTr, "Waypoint " + i, path[i], Quaternion.identity);
        spawner.waypoints = wps;
        return go;
    }

    static GameObject MakeCharacter(string prefabName, Transform parent, string name, Vector3 pos, float yaw, RuntimeAnimatorController ctrl)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/new-acc/school-life--ch/" + prefabName + ".prefab");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = name;
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        var anim = go.GetComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return go;
    }

    static void AddMirror(GameObject host, GameObject source, GameObject target)
    {
        var m = host.AddComponent<MirrorActive>();
        m.source = source;
        m.target = target;
        target.SetActive(false);
    }

    static ClueTaskPoint.Line Clue(string speaker, string text) => new ClueTaskPoint.Line { speaker = speaker, text = text };

    static Transform NewChild(Transform parent, string name, Vector3 pos, Quaternion rot)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.position = pos;
        t.rotation = rot;
        return t;
    }

    static float YawTo(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;
        return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
    }

    static Vector3 OverShoulder(Vector3 from, Vector3 to, float back, float side, float height)
    {
        Vector3 d = to - from; d.y = 0; d.Normalize();
        Vector3 right = new Vector3(d.z, 0, -d.x);
        Vector3 p = from - d * back + right * side;
        p.y = height;
        return p;
    }

    static readonly Dictionary<string, AnimationClip> clipCache = new Dictionary<string, AnimationClip>();

    static AnimationClip FindClip(string name)
    {
        if (clipCache.TryGetValue(name, out var cached) && cached != null) return cached;
        foreach (var guid in AssetDatabase.FindAssets(name + " t:AnimationClip"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var clip = a as AnimationClip;
                if (clip != null && clip.name == name && !clip.name.StartsWith("__preview__"))
                {
                    clipCache[name] = clip;
                    return clip;
                }
            }
        }
        return null;
    }

    static AnimationClip FindClipAt(string assetPath)
    {
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            var clip = a as AnimationClip;
            if (clip != null && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        return null;
    }

    static Material SolidMaterial(string prefix, Color color)
    {
        string path = AssetDir + "/" + prefix + "_" + ColorUtility.ToHtmlStringRGB(color) + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard")) { color = color };
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(AssetDir))
            AssetDatabase.CreateFolder("Assets/NewLevels", "Mission7");
    }

    static string OutPath(string name) => System.IO.Path.Combine(System.IO.Path.GetTempPath(), name);

    static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }

    // ---------------------------------------------------------------- preview

    /// <summary>Each line of m7_view_request.txt: name|fromX,fromY,fromZ|lookX,lookY,lookZ|in/out|fov</summary>
    public static void RenderViews()
    {
        var sb = new StringBuilder();
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        Vector3 V(string s) { var p = s.Split(','); return new Vector3(float.Parse(p[0], inv), float.Parse(p[1], inv), float.Parse(p[2], inv)); }
        try
        {
            var lh = Object.FindObjectOfType<Level_Handler>(true);
            var zone = Object.FindObjectOfType<SchoolZoneManager>(true);
            var env = lh.AnimeSchoolManager.env;
            bool envWas = env.activeSelf, intWas = zone.Interior.activeSelf, extWas = zone.Exterior.activeSelf;
            env.SetActive(true);
            var level = lh.transform.Find("Level_6");
            bool levelWas = level && level.gameObject.activeSelf;
            if (level) level.gameObject.SetActive(true);
            GameObject drink = level ? level.Find("M7_01 Buy Drinks")?.gameObject : null;
            bool drinkWas = drink && drink.activeSelf;
            if (drink)
            {
                drink.SetActive(true);
                foreach (var ps in drink.GetComponentsInChildren<ParticleSystem>(true))
                    ps.Simulate(1.2f, true, true);
            }
            var camGo = new GameObject("M7 View Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            foreach (var line in System.IO.File.ReadAllLines(OutPath("m7_view_request.txt")))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var p = line.Split('|');
                zone.Interior.SetActive(p[3] == "in");
                zone.Exterior.SetActive(p[3] != "in");
                Vector3 from = V(p[1]), look = V(p[2]);
                camGo.transform.SetPositionAndRotation(from, Quaternion.LookRotation(look - from));
                cam.fieldOfView = p.Length > 4 ? float.Parse(p[4], inv) : 50f;
                var rt = new RenderTexture(960, 540, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                tex.Apply();
                cam.targetTexture = null;
                RenderTexture.active = null;
                string file = OutPath("m7_view_" + p[0] + ".png");
                System.IO.File.WriteAllBytes(file, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(rt);
                sb.AppendLine(file);
            }
            Object.DestroyImmediate(camGo);
            if (drink) drink.SetActive(drinkWas);
            if (level) level.gameObject.SetActive(levelWas);
            env.SetActive(envWas); zone.Interior.SetActive(intWas); zone.Exterior.SetActive(extWas);
        }
        catch (System.Exception e) { sb.AppendLine("ERR " + e); }
        System.IO.File.WriteAllText(OutPath("m7_view.txt"), sb.ToString());
    }

    [MenuItem("Tools/Mission 7/Render Preview Shots")]
    public static void RenderPreview()
    {
        var sb = new StringBuilder();
        try
        {
            var req = System.IO.File.ReadAllLines(OutPath("m7_preview_request.txt"));
            var lh = Object.FindObjectOfType<Level_Handler>(true);
            var am = lh.AnimeSchoolManager;
            var zone = Object.FindObjectOfType<SchoolZoneManager>(true);
            Transform level = lh.transform.Find("Level_6");

            bool levelWas = level.gameObject.activeSelf, envWas = am.env.activeSelf, intWas = zone.Interior.activeSelf, extWas = zone.Exterior.activeSelf;
            level.gameObject.SetActive(true);
            am.env.SetActive(true);
            var npcs = GameObject.Find("SchoolNPCs");
            bool npcsWas = npcs != null && npcs.activeSelf;
            if (npcs) npcs.SetActive(false);

            foreach (var line in req)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split('|');
                string cutName = parts[0];
                float time = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                bool inside = parts[2] == "in";
                zone.Interior.SetActive(inside);
                zone.Exterior.SetActive(!inside);

                var cut = level.Find(cutName);
                cut.gameObject.SetActive(true);
                var dir = cut.GetComponent<PlayableDirector>();
                foreach (var cc in cut.GetComponentsInChildren<CutsceneCostume>(true)) cc.Apply(1);
                dir.RebuildGraph();
                dir.time = time;
                dir.Evaluate();
                dir.Evaluate();
                foreach (var m in cut.GetComponentsInChildren<CutsceneMover>(true)) m.Apply(time);
                foreach (var h in cut.GetComponentsInChildren<CutsceneHandItem>(true)) h.Snap();

                var cam = cut.GetComponentsInChildren<Camera>(false).FirstOrDefault(c => c.gameObject.activeInHierarchy);
                if (cam == null) { sb.AppendLine(line + " -> no active camera"); continue; }
                var rt = new RenderTexture(960, 540, 24);
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
                tex.Apply();
                cam.targetTexture = null;
                RenderTexture.active = null;
                string file = OutPath("m7_" + cutName.Replace(" ", "_") + "_" + time.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ".png");
                System.IO.File.WriteAllBytes(file, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(rt);
                sb.AppendLine(line + " -> " + cam.name + " -> " + file);
            }

            zone.Interior.SetActive(intWas);
            zone.Exterior.SetActive(extWas);
            am.env.SetActive(envWas);
            level.gameObject.SetActive(levelWas);
            if (npcs) npcs.SetActive(npcsWas);
        }
        catch (System.Exception e) { sb.AppendLine("ERR " + e); }
        System.IO.File.WriteAllText(OutPath("m7_preview.txt"), sb.ToString());
    }
}
