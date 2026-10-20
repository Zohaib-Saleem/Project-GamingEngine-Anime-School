using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Builds Mission 8 "Sports Day Practice" into Levels/Level_7 (gameplay slot 7).
/// Starts in the classroom where Mission 7 ended. Re-running rebuilds from scratch.
/// Editor only.
/// </summary>
public static class Mission8Builder
{
    const int Slot = 7;
    const string AssetDir = "Assets/NewLevels/Mission8";
    const string ScenePath = "Assets/Scenes/GameScenes/Gameplay.unity";

    // Classroom (Mission 7 end) — both actors stand in the open aisle so Cam A can see them.
    static readonly Vector3 ClassSpawn = new Vector3(4.20f, 0.05f, 242.80f);
    static readonly Vector3 MeiIntro = new Vector3(2.70f, 0.05f, 244.40f);
    static readonly Vector3 ExitPoint = new Vector3(-0.4f, 0f, 255.5f);
    static readonly Vector3 OutsideGate = new Vector3(57.6f, 0f, 4.4f);

    // Cafe patio (open area — not under the roof, same spot M6 cafe used).
    static readonly Vector3 DrinkPoint = new Vector3(4.50f, 0f, 60.80f);
    static readonly Vector3 CafeYuki = new Vector3(4.50f, 0.05f, 60.80f);
    static readonly Vector3 VendorPos = new Vector3(6.00f, 0.05f, 61.80f);

    // Basketball court — renderer top ≈ 0.179. Idle soles clear at ~0.48, but walk stride dips feet lower.
    // Court mesh top ~0.14–0.18; jog_fwd dips feet ~4cm → root ≈ 0.36 keeps plant during jog.
    const float CourtY = 0.36f;
    static readonly Vector3 CourtPoint = new Vector3(16.5f, 0f, -50.0f);
    static readonly Vector3 CourtHaru = new Vector3(22.5f, CourtY, -56.0f);
    static readonly Vector3 CourtRina = new Vector3(18.8f, CourtY, -54.2f);
    static readonly Vector3 CourtYuki = new Vector3(14.2f, CourtY, -49.5f);
    static readonly Vector3 CourtBench = new Vector3(12.5f, CourtY, -47.8f);

    static StringBuilder log;

    [MenuItem("Tools/Mission 8/Build")]
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
            level.name = "Level_7";

            var spawn = NewChild(level, "M8 Player Spawn", ClassSpawn, Quaternion.identity);
            var camSpawn = NewChild(level, "M8 Camera Spawn", ClassSpawn + new Vector3(0f, 1.65f, -3.4f), Quaternion.identity);
            var npcHolder = NewChild(level, "M8 Mission NPCs", Vector3.zero, Quaternion.identity);

            var t1 = MakeTaskPoint(template, level, "M8_01 Go Outside School", ExitPoint,
                new[] { ClassSpawn, new Vector3(1.0f, 0, 248.5f), new Vector3(-0.2f, 0, 252.0f), ExitPoint });
            t1.AddComponent<SchoolZoneTrigger>().action = SchoolZoneTrigger.ZoneAction.ExitSchool;

            var t2 = MakeTaskPoint(template, level, "M8_02 Buy Water At Cafe", DrinkPoint,
                new[]
                {
                    OutsideGate, new Vector3(54.6f, 0, 5.1f), new Vector3(36.9f, 0, 4.8f), new Vector3(27.1f, 0, 4.6f),
                    new Vector3(13.0f, 0, 5.6f), new Vector3(0.6f, 0, 7.6f), new Vector3(-2.6f, 0, 12.1f),
                    new Vector3(-2.2f, 0, 25.6f), new Vector3(-2.3f, 0, 43.9f), new Vector3(-1.4f, 0, 55.1f),
                    new Vector3(1.0f, 0, 59.0f), DrinkPoint
                });
            t2.AddComponent<TaskPointHit>();

            var t3 = MakeTaskPoint(template, level, "M8_03 Take Water To Court", CourtPoint,
                new[]
                {
                    DrinkPoint, new Vector3(8.0f, 0, 40.0f), new Vector3(14.0f, 0, 18.0f), new Vector3(20.0f, 0, 2.0f),
                    new Vector3(24.0f, 0, -20.0f), new Vector3(20.0f, 0, -40.0f), CourtPoint
                });
            t3.AddComponent<TaskPointHit>();

            var standCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/NPC/Animators/NPC_Stand.controller");
            var vendor = MakeCharacter("GirlStudent", npcHolder, "Cafe Staff (waiting)", VendorPos, YawTo(VendorPos, DrinkPoint), standCtrl);
            var haruNpc = MakeCharacter("BoyStudent", npcHolder, "Haru (waiting at court)", CourtHaru, YawTo(CourtHaru, CourtYuki), standCtrl);
            AddMirror(npcHolder.gameObject, t2, vendor);
            AddMirror(npcHolder.gameObject, t3, haruNpc);

            var intro = BuildIntro(level);
            var cafe = BuildCafeCutscene(level);
            var court = BuildCourtCutscene(level);

            Undo.RecordObject(lh, "Mission 8");
            var L = lh.levelSpawnPoints[Slot];
            L.Level = level.gameObject;
            L.playerSpawnPoint = spawn;
            L.CameraSp = camSpawn;
            L.Cutscene = intro;
            L.LeveltaskParticle = new[] { t1, t2, t3 };
            L.TaskCutscenes = new GameObject[] { null, cafe.gameObject, court.gameObject };
            L.LevelHints = new[]
            {
                "GO OUTSIDE THE SCHOOL BUILDING",
                "BUY WATER AT THE CAFE",
                "TAKE THE WATER TO THE BASKETBALL COURT",
            };
            Transform leftover = level.Find("M8 Carry Water");
            if (leftover != null)
                Object.DestroyImmediate(leftover.gameObject);
            L.playerCarryItem = HandPropBuild.Player(lh, "Water Bottle");
            L.showCarryAfterTask = 1;
            L.hideCarryOnReachTask = 2;
            L.showCarryAtStart = false;
            L.hasMathTest = false;
            L.mathTestSetIndex = 0;
            L.startInsideSchool = true;
            L.startInMyRoom = false;
            L.allowBike = false;
            L.RewardAmount = 2400;
            L.levelTime = 200f;
            EditorUtility.SetDirty(lh);
            EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
            AssetDatabase.SaveAssets();
            log.AppendLine("BUILD OK slot=" + Slot + " level=" + level.name);
        }
        catch (System.Exception e) { log.AppendLine("ERR " + e); }
        System.IO.File.WriteAllText(OutPath("m8_build.txt"), log.ToString());
    }

    public static void PreparePlay()
    {
        PlayerPrefs.SetInt("SelectedLevel", 7);
        PlayerPrefs.Save();
        System.IO.File.WriteAllText(OutPath("m8_play.txt"), "SelectedLevel=7");
    }

    [MenuItem("Tools/Mission 8/Save Scene")]
    public static void SaveScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        bool ok = scene.path == ScenePath && EditorSceneManager.SaveScene(scene);
        System.IO.File.WriteAllText(OutPath("m8_save.txt"), ok ? "SAVED" : "NOT SAVED (" + scene.path + ")");
    }

    // ---------------------------------------------------------------- cutscenes

    static PlayableDirector BuildIntro(Transform level)
    {
        var cs = new Cutscene(level, "M8 Intro Cutscene", AssetDir + "/M8_Intro.playable", 12f, true);
        float yukiYaw = YawTo(ClassSpawn, MeiIntro);
        var yuki = cs.AddYuki("Yuki", ClassSpawn, yukiYaw);
        var mei = cs.AddCharacter("GirlStudent", "Mei", MeiIntro, YawTo(MeiIntro, ClassSpawn));

        cs.Move(yuki.mover, new[] { 0f, 12f }, new[] { ClassSpawn, ClassSpawn }, new[] { yukiYaw, yukiYaw });
        cs.Move(mei.mover, new[] { 0f, 12f }, new[] { MeiIntro, MeiIntro }, new[] { YawTo(MeiIntro, ClassSpawn), YawTo(MeiIntro, ClassSpawn) });

        cs.Anim(yuki, "FemaleIdle", 0f, 7.2f, true);
        cs.Anim(yuki, "male_talk_stand", 7.2f, 2.6f, true);
        cs.Anim(yuki, "FemaleIdle", 9.8f, 2.2f, true);
        cs.Anim(mei, "male_talk_stand", 0.2f, 6.9f, true);
        cs.Anim(mei, "FemaleIdle", 7.1f, 2.7f, true);
        cs.Anim(mei, "f02_greet_03", 9.8f, 2.2f, false);

        Vector3 mid = (ClassSpawn + MeiIntro) * 0.5f + Vector3.up * 1.35f;
        // Side two-shot from the desks — both faces readable, no doorway occlusion.
        cs.Shot("Cam A Two Shot", 0f, 5.5f,
            new Vector3(6.10f, 1.58f, 242.40f), new Vector3(5.70f, 1.52f, 242.70f), mid);
        cs.Shot("Cam B Mei Close", 5.5f, 3.5f,
            new Vector3(4.55f, 1.52f, 243.15f), new Vector3(4.25f, 1.48f, 243.45f), MeiIntro + Vector3.up * 1.38f);
        cs.Shot("Cam C Yuki Close", 9.0f, 3.0f,
            new Vector3(3.05f, 1.50f, 244.55f), new Vector3(3.25f, 1.48f, 244.20f), ClassSpawn + Vector3.up * 1.35f);

        cs.Line(0.3f, 3.2f, "Mei", "Sports day practice! Haru's running laps on the court.", "M8_01_Mei");
        cs.Line(3.6f, 3.4f, "Mei", "Bring him water. Perfect excuse to show up.", "M8_02_Mei");
        cs.Line(7.2f, 2.4f, "Yuki", "I'm not— okay. Fine. One bottle.", "M8_03_Yuki");
        cs.Line(9.8f, 2.0f, "Mei", "Go go go~ Don't let anyone beat you there!", "M8_04_Mei");

        cs.HideAmbient("SchoolNPCs/InsideSchool");
        return cs.Finish();
    }

    static PlayableDirector BuildCafeCutscene(Transform level)
    {
        // Eye-level patio shots (not under the cafe roof like the old gameplay camera).
        var cs = new Cutscene(level, "M8 Cafe Cutscene", AssetDir + "/M8_Cafe.playable", 14f, false);
        float yukiYaw = YawTo(CafeYuki, VendorPos);
        var yuki = cs.AddYuki("Yuki", CafeYuki, yukiYaw);
        var staff = cs.AddCharacter("GirlStudent", "Cafe Staff", VendorPos, YawTo(VendorPos, CafeYuki));

        var yukiMove = cs.Move(yuki.mover, new[] { 0f, 14f }, new[] { CafeYuki, CafeYuki }, new[] { yukiYaw, yukiYaw });
        yukiMove.lockChildLocalPosition = true;
        var staffMove = cs.Move(staff.mover, new[] { 0f, 14f }, new[] { VendorPos, VendorPos }, new[] { YawTo(VendorPos, CafeYuki), YawTo(VendorPos, CafeYuki) });
        staffMove.lockChildLocalPosition = true;

        cs.Anim(yuki, "male_talk_stand", 0.2f, 3.6f, true);
        cs.Anim(yuki, "FemaleIdle", 3.8f, 4.4f, true);
        cs.Anim(yuki, "male_talk_stand", 8.2f, 3.1f, true);
        cs.Anim(yuki, "FemaleIdle", 11.3f, 2.7f, true);

        cs.Anim(staff, "FemaleIdle", 0f, 4.0f, true);
        cs.Anim(staff, "male_talk_stand", 4.0f, 4.0f, true);
        cs.Anim(staff, "FemaleIdle", 8.0f, 3.4f, true);
        cs.Anim(staff, "f02_greet_03", 11.4f, 2.6f, false);

        Vector3 mid = (CafeYuki + VendorPos) * 0.5f + Vector3.up * 1.35f;
        Vector3 staffHead = VendorPos + Vector3.up * 1.45f;
        Vector3 yukiHead = CafeYuki + Vector3.up * 1.40f;
        // Medium patio angles — keep distance so hair doesn't fill the lens.
        cs.Shot("Cam A Patio Two Shot", 0f, 4.0f,
            new Vector3(3.10f, 1.55f, 62.40f), new Vector3(3.35f, 1.50f, 62.10f), mid);
        cs.Shot("Cam B Staff Close", 4.0f, 4.0f,
            new Vector3(3.55f, 1.52f, 61.10f), new Vector3(3.70f, 1.48f, 61.25f), staffHead);
        cs.Shot("Cam C Yuki Close", 8.0f, 3.5f,
            new Vector3(3.80f, 1.52f, 62.30f), new Vector3(3.95f, 1.48f, 62.05f), yukiHead);
        cs.Shot("Cam D Water Hand Off", 11.5f, 2.5f,
            new Vector3(3.40f, 1.50f, 62.70f), new Vector3(3.60f, 1.45f, 62.35f), mid);

        cs.Hand(yuki, 11.4f, 2.6f, "Water Bottle");

        cs.Line(0.3f, 3.4f, "Yuki", "One cold water, please. For... sports practice.", "M8_05_Yuki");
        cs.Line(4.0f, 3.8f, "Staff", "For Haru again? You're a good friend.", "M8_06_Staff");
        cs.Line(8.2f, 3.0f, "Yuki", "Just a friend! ...Okay, maybe hurry.", "M8_07_Yuki");
        cs.Line(11.4f, 2.4f, "Staff", "Here you go. Don't spill it on the way!", "M8_08_Staff");

        cs.HideAmbient("SchoolNPCs/OutsideSchool");
        return cs.Finish();
    }

    static PlayableDirector BuildCourtCutscene(Transform level)
    {
        var cs = new Cutscene(level, "M8 Court Cutscene", AssetDir + "/M8_Court.playable", 22f, false);
        var yuki = cs.AddYuki("Yuki", CourtYuki, YawTo(CourtYuki, CourtHaru));
        var haru = cs.AddCharacter("BoyStudent", "Haru", CourtHaru, YawTo(CourtHaru, CourtYuki));
        var rina = cs.AddCharacter("GirlStudent", "Rina", CourtRina, YawTo(CourtRina, CourtHaru));

        // Sports jog to Yuki — linear mover must match jog_fwd averageSpeed (3.700).
        // SmoothStep + mismatched speed caused skating; pin root-motion so feet stay planted.
        Vector3 haruStart = CourtHaru;
        Vector3 haruMeet = CourtYuki + new Vector3(1.55f, 0f, -0.55f); // natural talk gap
        float jogDist = Vector3.Distance(new Vector3(haruStart.x, 0f, haruStart.z), new Vector3(haruMeet.x, 0f, haruMeet.z));
        const float JogSpeed = 3.700f; // jog_fwd clip averageSpeed
        float jogDur = Mathf.Max(2.2f, jogDist / JogSpeed);
        // Ease out: last 0.55s decelerate with walk_fwd so the stop isn't a hard skate-halt.
        const float DecelSpeed = 1.557f;
        float decelDur = 0.55f;
        float cruiseDur = Mathf.Max(1.4f, jogDur - decelDur);
        float decelDist = DecelSpeed * decelDur;
        float cruiseDist = Mathf.Max(0.5f, jogDist - decelDist);
        cruiseDur = cruiseDist / JogSpeed;
        float tIdleEnd = 0.9f;
        float tDecel = tIdleEnd + cruiseDur;
        float tArrive = tDecel + decelDur;
        float faceYuki = YawTo(haruStart, haruMeet);
        Vector3 haruDecel = Vector3.Lerp(haruStart, haruMeet, cruiseDist / jogDist);

        var haruMove = cs.Move(haru.mover,
            new[] { 0f, tIdleEnd, tDecel, tArrive, 22f },
            new[] { haruStart, haruStart, haruDecel, haruMeet, haruMeet },
            new[] { faceYuki, faceYuki, faceYuki, faceYuki, faceYuki });
        haruMove.smooth = false;
        haruMove.lockChildLocalPosition = true;
        haruMove.lockChildLocalYaw = true;

        var yukiMove = cs.Move(yuki.mover, new[] { 0f, 22f }, new[] { CourtYuki, CourtYuki }, new[] { YawTo(CourtYuki, CourtHaru), YawTo(CourtYuki, haruMeet) });
        yukiMove.lockChildLocalPosition = true;
        var rinaMove = cs.Move(rina.mover, new[] { 0f, 22f }, new[] { CourtRina, CourtRina }, new[] { YawTo(CourtRina, CourtHaru), YawTo(CourtRina, haruMeet) });
        rinaMove.lockChildLocalPosition = true;

        float haruTalkStart = Mathf.Max(tArrive + 0.3f, 7.7f);
        float haruTalkDur = 6.4f;

        cs.Anim(haru, "Idle", 0f, tIdleEnd, true);
        cs.Anim(haru, "jog_fwd", tIdleEnd, cruiseDur, true);
        cs.Anim(haru, "walk_fwd", tDecel, decelDur, true);
        cs.Anim(haru, "Idle", tArrive, Mathf.Max(0.2f, haruTalkStart - tArrive), true);
        cs.Anim(haru, "male_talk_stand", haruTalkStart, haruTalkDur, true);
        cs.Anim(haru, "Idle", haruTalkStart + haruTalkDur, Mathf.Max(0.2f, 22f - (haruTalkStart + haruTalkDur)), true);

        cs.Anim(yuki, "FemaleIdle", 0f, 4.2f, true);
        cs.Anim(yuki, "male_talk_stand", 4.2f, 3.4f, true);
        cs.Anim(yuki, "FemaleIdle", 7.6f, 10.0f, true);
        cs.Anim(yuki, "male_talk_stand", 17.6f, 2.2f, true);
        cs.Anim(yuki, "FemaleIdle", 19.8f, 2.2f, true);

        cs.Anim(rina, "male_talk_stand", 0.2f, 3.4f, true);
        cs.Anim(rina, "FemaleIdle", 3.6f, 16.4f, true);
        cs.Anim(rina, "male_talk_stand", 20.0f, 1.8f, true);
        cs.Anim(rina, "FemaleIdle", 21.8f, 0.2f, true);

        Vector3 mid = (CourtYuki + haruMeet) * 0.5f + Vector3.up * 1.2f;
        // Sequential cams only — prior Max(4,tArrive-3) made B/C/D overlap when jog arrives ~3.65s.
        float tCamB = Mathf.Clamp(tArrive - 1.4f, 2.2f, 4.0f);
        float tCamC = Mathf.Max(tCamB + 1.0f, tArrive - 0.15f);
        float tCamD = Mathf.Max(tCamC + 1.25f, tArrive + 0.35f); // keep C ≥1.25s, still no overlap
        float tCamE = 18.0f;
        cs.Shot("Cam A Wide Court", 0f, tCamB,
            new Vector3(10.5f, 2.4f, -42.0f), new Vector3(12.0f, 2.2f, -44.0f), mid);
        cs.Shot("Cam B Rina Watch", tCamB, tCamC - tCamB,
            new Vector3(17.2f, 1.55f + CourtY, -51.5f), new Vector3(17.6f, 1.5f + CourtY, -52.2f), CourtRina + Vector3.up * 1.35f);
        cs.Shot("Cam C Haru Arrives", tCamC, tCamD - tCamC,
            new Vector3(12.8f, 1.55f + CourtY, -46.5f), new Vector3(13.4f, 1.5f + CourtY, -47.2f), mid);
        cs.Shot("Cam D Two Shot", tCamD, Mathf.Max(2.0f, tCamE - tCamD),
            new Vector3(11.5f, 1.6f + CourtY, -45.8f), new Vector3(12.0f, 1.55f + CourtY, -46.5f), mid);
        cs.Shot("Cam E Rina Reaction", tCamE, 4.0f,
            new Vector3(20.5f, 1.55f + CourtY, -51.0f), new Vector3(19.8f, 1.5f + CourtY, -51.8f), CourtRina + Vector3.up * 1.4f);

        cs.Line(0.4f, 3.2f, "Rina", "Took you long enough. I already brought him water.", "M8_09_Rina");
        cs.Line(4.2f, 3.4f, "Yuki", "...Oh. Right. Of course you did.", "M8_10_Yuki");
        cs.Line(haruTalkStart, 3.2f, "Haru", "Yuki! You came! I run faster when you're watching.", "M8_11_Haru");
        cs.Line(haruTalkStart + 3.4f, 2.8f, "Haru", "I'll take yours. It tastes better somehow.", "M8_12_Haru");
        cs.Line(17.6f, 2.2f, "Yuki", "That's— that's not how taste works.", "M8_13_Yuki");
        cs.Line(20.0f, 1.8f, "Rina", "...Interesting.", "M8_14_Rina");

        log.AppendLine("haru jog dist=" + jogDist.ToString("0.00") + " cruise=" + cruiseDur.ToString("0.00") + " decel=" + decelDur.ToString("0.00") + " arrive=" + tArrive.ToString("0.00"));

        cs.HideAmbient("SchoolNPCs/OutsideSchool");
        return cs.Finish();
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
            foreach (var anim in actor.animators)
            {
                if (!tracks.TryGetValue(anim, out var track))
                {
                    track = timeline.CreateTrack<AnimationTrack>(null, actor.mover.name + " / " + anim.name);
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
            m.smooth = false;
            m.lockChildLocalYaw = true;
            return m;
        }

        public void Shot(string name, float start, float length, Vector3 from, Vector3 to, Vector3 lookAt)
        {
            var cam = NewChild(root.transform, name, from, Quaternion.LookRotation(lookAt - from)).gameObject;
            var c = cam.AddComponent<Camera>();
            c.fieldOfView = 45f;
            c.nearClipPlane = 0.05f;
            c.depth = 20;
            cam.SetActive(false);
            var m = cam.AddComponent<CutsceneMover>();
            m.director = director;
            m.times = new[] { start, start + length };
            m.positions = new[] { from, to };
            m.rotations = new[] { Quaternion.LookRotation(lookAt - from), Quaternion.LookRotation(lookAt - to) };
            m.smooth = false;
            m.yawDegrees = false;
            Show(cam, start, length, name);
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
            var holder = NewChild(root.transform, "Hide Ambient NPCs", Vector3.zero, Quaternion.identity).gameObject;
            holder.AddComponent<CutsceneVisibilityOverride>().targets = new[] { target.gameObject };
            holder.SetActive(false);
            Show(holder, 0f, duration, "Hide Ambient NPCs");
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

        Transform level = lh.transform.Find("Level_7");
        if (level == null)
        {
            var go = new GameObject("Level_7");
            go.transform.SetParent(lh.transform, false);
            go.SetActive(false);
            level = go.transform;
            log.AppendLine("created Level_7");
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
        if (!AssetDatabase.IsValidFolder("Assets/NewLevels"))
            AssetDatabase.CreateFolder("Assets", "NewLevels");
        if (!AssetDatabase.IsValidFolder(AssetDir))
            AssetDatabase.CreateFolder("Assets/NewLevels", "Mission8");
    }

    static string OutPath(string name) => System.IO.Path.Combine(System.IO.Path.GetTempPath(), name);

    [MenuItem("Tools/Mission 8/Render Preview Shots")]
    public static void RenderPreview()
    {
        var sb = new StringBuilder();
        try
        {
            var req = System.IO.File.ReadAllLines(OutPath("m8_preview_request.txt"));
            var lh = Object.FindObjectOfType<Level_Handler>(true);
            var am = lh.AnimeSchoolManager;
            var zone = Object.FindObjectOfType<SchoolZoneManager>(true);
            Transform level = lh.transform.Find("Level_7");

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
                string file = OutPath("m8_" + cutName.Replace(" ", "_") + "_" + time.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ".png");
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
        System.IO.File.WriteAllText(OutPath("m8_preview.txt"), sb.ToString());
    }
}
