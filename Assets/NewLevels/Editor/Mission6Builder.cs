using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Builds Mission 6 "Wrong Notebook" into Levels/Level_5 (gameplay slot 5).
/// Re-running rebuilds it from scratch. Editor only.
/// </summary>
public static class Mission6Builder
{
    const int Slot = 5;
    const string AssetDir = "Assets/NewLevels/Mission6";
    const string ScenePath = "Assets/Scenes/GameScenes/Gameplay.unity";

    // Story positions (world space, school interior is around z 150..256, cafe around (4, 61)).
    static readonly Vector3 YukiStart = new Vector3(1.2f, 0.05f, 214.8f);
    static readonly Vector3 YukiBump = new Vector3(0.7f, 0.05f, 220.6f);
    static readonly Vector3 HaruStart = new Vector3(0.2f, 0.05f, 230.0f);
    static readonly Vector3 HaruBump = new Vector3(0.5f, 0.05f, 221.6f);
    static readonly Vector3 HaruSidestep = new Vector3(-0.5f, 0.05f, 220.6f);
    static readonly Vector3 HaruLab = new Vector3(1.1f, 0.05f, 214.2f);

    static readonly Vector3 ClassroomPoint = new Vector3(1.4f, 0f, 246.0f);
    static readonly Vector3 MeiPoint = new Vector3(-0.2f, 0f, 249.0f);
    static readonly Vector3 MeiPos = new Vector3(-1.2f, 0.05f, 250.8f);
    static readonly Vector3 ExitPoint = new Vector3(-0.4f, 0f, 255.5f);

    static readonly Vector3 CafePoint = new Vector3(4.3f, 0f, 61.3f);
    static readonly Vector3 CafeHaru = new Vector3(6.0f, 0.02f, 62.3f);
    static readonly Vector3 CafeYukiFrom = new Vector3(3.4f, 0.02f, 60.2f);
    static readonly Vector3 CafeYuki = new Vector3(4.5f, 0.02f, 61.0f);

    static StringBuilder log;

    // ---------------------------------------------------------------- entry points

    [MenuItem("Tools/Mission 6/Build")]
    public static void Build()
    {
        log = new StringBuilder();
        try
        {
            EnsureFolder();
            var lh = Object.FindObjectOfType<Level_Handler>(true);
            Transform level = lh.transform.Find("Level_5");
            var template = lh.transform.Find("Level_3/School Gate particle (1)").gameObject;

            for (int i = level.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(level.GetChild(i).gameObject);
            level.name = "Level_5";

            // Spawn after the intro: where Yuki stands at the end of it.
            var spawn = NewChild(level, "M6 Player Spawn", YukiBump, Quaternion.identity);
            var camSpawn = NewChild(level, "M6 Camera Spawn", YukiBump + new Vector3(0f, 1.65f, -3.4f), Quaternion.identity);

            var npcHolder = NewChild(level, "M6 Mission NPCs", Vector3.zero, Quaternion.identity);

            // Tasks
            var t1 = MakeTaskPoint(template, level, "M6_01 Go To Classroom", ClassroomPoint,
                new[] { YukiBump, new Vector3(0.4f, 0, 228f), new Vector3(0.3f, 0, 238.5f), new Vector3(0.6f, 0, 242.8f), ClassroomPoint });
            var clue = t1.AddComponent<ClueTaskPoint>();
            clue.lines = new[]
            {
                Clue("Yuki", "Okay, back to my drawing... wait. This cover is blue.", "M6_07_Yuki"),
                Clue("Yuki", "\"Haru Sato, Class 2-B\"... This isn't my notebook!", "M6_08_Yuki"),
                Clue("Yuki", "That means HE has mine. With all my doodles inside! I have to find him!", "M6_09_Yuki"),
            };

            var t2 = MakeTaskPoint(template, level, "M6_02 Find Mei", MeiPoint,
                new[] { ClassroomPoint, new Vector3(0.6f, 0, 247.6f), MeiPoint });
            t2.AddComponent<TaskPointHit>();

            var t3 = MakeTaskPoint(template, level, "M6_03 Go Outside", ExitPoint,
                new[] { MeiPoint, new Vector3(-0.3f, 0, 252.4f), ExitPoint });
            t3.AddComponent<SchoolZoneTrigger>().action = SchoolZoneTrigger.ZoneAction.ExitSchool;

            var t4 = MakeTaskPoint(template, level, "M6_04 Find Haru At Cafe", CafePoint,
                new[]
                {
                    new Vector3(54.6f, 0, 5.1f), new Vector3(36.9f, 0, 4.8f), new Vector3(27.1f, 0, 4.6f), new Vector3(13.0f, 0, 5.6f),
                    new Vector3(0.6f, 0, 7.6f), new Vector3(-2.6f, 0, 12.1f), new Vector3(-2.2f, 0, 25.6f), new Vector3(-2.3f, 0, 43.9f),
                    new Vector3(-1.4f, 0, 55.1f), new Vector3(1.0f, 0, 59.0f), CafePoint
                });
            t4.AddComponent<TaskPointHit>();

            // Gameplay NPCs that wait at their task point
            var standCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/NPC/Animators/NPC_Stand.controller");
            var meiNpc = MakeCharacter("GirlStudent", npcHolder, "Mei (waiting)", MeiPos, YawTo(MeiPos, MeiPoint), standCtrl);
            var haruNpc = MakeCharacter("BoyStudent", npcHolder, "Haru (waiting at cafe)", CafeHaru, YawTo(CafeHaru, new Vector3(1f, 0, 59f)), standCtrl);
            AddMirror(npcHolder.gameObject, t2, meiNpc);
            AddMirror(npcHolder.gameObject, t4, haruNpc);

            // Cutscenes
            var intro = BuildIntro(level);
            var meiCut = BuildMeiCutscene(level);
            var cafeCut = BuildCafeCutscene(level, haruNpc);

            // Level_Handler slot
            Undo.RecordObject(lh, "Mission 6");
            var L = lh.levelSpawnPoints[Slot];
            L.Level = level.gameObject;
            L.playerSpawnPoint = spawn;
            L.CameraSp = camSpawn;
            L.Cutscene = intro;
            L.LeveltaskParticle = new[] { t1, t2, t3, t4 };
            L.TaskCutscenes = new GameObject[] { null, meiCut.gameObject, null, cafeCut.gameObject };
            L.LevelHints = new[]
            {
                "GO TO YOUR CLASSROOM",
                "FIND MEI NEAR THE STAIRS",
                "GO OUTSIDE THE SCHOOL BUILDING",
                "FIND HARU AT THE CAFE",
            };
            L.playerCarryItem = HandPropBuild.Player(lh, "Book", "Notebook");
            L.showCarryAfterTask = -1;
            L.hideCarryOnReachTask = 3;
            L.showCarryAtStart = true;
            L.hasMathTest = false;
            L.startInsideSchool = true;
            L.startInMyRoom = false;
            L.allowBike = false;
            L.RewardAmount = 2000;
            L.levelTime = 180f;
            EditorUtility.SetDirty(lh);
            EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
            AssetDatabase.SaveAssets();
            log.AppendLine("BUILD OK");
        }
        catch (System.Exception e) { log.AppendLine("ERR " + e); }
        System.IO.File.WriteAllText(OutPath("m6_build.txt"), log.ToString());
    }

    [MenuItem("Tools/Mission 6/Save Scene")]
    public static void SaveScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        bool ok = scene.path == ScenePath && EditorSceneManager.SaveScene(scene);
        System.IO.File.WriteAllText(OutPath("m6_save.txt"), ok ? "SAVED" : "NOT SAVED (" + scene.path + ")");
    }

    public static void PreparePlay()
    {
        PlayerPrefs.SetInt("SelectedLevel", 5);
        PlayerPrefs.Save();
        System.IO.File.WriteAllText(OutPath("m6_play.txt"), "SelectedLevel=5");
    }

    public static void CapturePlayShot()
    {
        string name = "play";
        string req = OutPath("play_shot_name.txt");
        if (System.IO.File.Exists(req))
            name = System.IO.File.ReadAllText(req).Trim();
        var cam = Object.FindObjectsOfType<Camera>().Where(c => c.isActiveAndEnabled).OrderByDescending(c => c.depth).FirstOrDefault();
        var sb = new StringBuilder();
        if (cam == null) { System.IO.File.WriteAllText(OutPath("play_shot.txt"), "no camera"); return; }
        var rt = new RenderTexture(960, 540, 24);
        var prev = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
        tex.Apply();
        cam.targetTexture = prev;
        RenderTexture.active = null;
        string file = OutPath(name + ".png");
        System.IO.File.WriteAllBytes(file, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        sb.AppendLine(file + " cam=" + cam.name + " pos=" + cam.transform.position.ToString("F2"));
        System.IO.File.WriteAllText(OutPath("play_shot.txt"), sb.ToString());
    }

    // ---------------------------------------------------------------- cutscenes

    static PlayableDirector BuildIntro(Transform level)
    {
        var cs = new Cutscene(level, "M6 Intro Cutscene", AssetDir + "/M6_Intro.playable", 16f, true);
        var yuki = cs.AddYuki("Yuki", YukiStart, 0f);
        var haru = cs.AddCharacter("BoyStudent", "Haru", HaruStart, 180f);

        var yukiIntroMove = cs.Move(yuki.mover, new[] { 0f, 4.2f }, new[] { YukiStart, YukiBump }, new[] { 0f, 0f });
        yukiIntroMove.lockChildLocalPosition = true;
        var haruIntroMove = cs.Move(haru.mover, new[] { 0f, 4.2f, 12.2f, 16f },
            new[] { HaruStart, HaruBump, HaruBump, HaruLab },
            new[] { 180f, 180f, 180f, 180f });
        haruIntroMove.lockChildLocalPosition = true;
        haruIntroMove.lockChildLocalYaw = true;

        cs.Anim(yuki, "IP_Mvm_Walk_Loop", 0f, 4.2f, true);
        cs.Anim(yuki, "FemaleIdle", 4.2f, 4.1f, true);
        cs.Anim(yuki, "male_talk_stand", 8.3f, 2.5f, true);
        cs.Anim(yuki, "FemaleIdle", 10.8f, 2.6f, true);
        cs.Anim(yuki, "male_talk_stand", 13.4f, 2.6f, true);

        cs.Anim(haru, "walk_fwd", 0f, 4.2f, true);
        cs.Anim(haru, "Idle", 4.2f, 0.1f, true);
        cs.Anim(haru, "male_talk_stand", 4.3f, 4.0f, true);
        cs.Anim(haru, "Idle", 8.3f, 2.5f, true);
        cs.Anim(haru, "male_talk_stand", 10.8f, 1.4f, true);
        cs.Anim(haru, "walk_fwd", 12.2f, 3.8f, true);

        Vector3 meet = (YukiBump + HaruBump) * 0.5f + Vector3.up * 1.15f;
        // Corridor cameras stay on the open side of the hall. A camera parked on the door
        // hides Haru, so Yuki's lines play against an empty doorway.
        cs.Shot("Cam A Yuki Walk", 0f, 2.2f, new Vector3(-1.85f, 1.55f, 213.4f), new Vector3(-1.80f, 1.50f, 214.6f), new Vector3(1.05f, 1.25f, 216.4f));
        cs.Shot("Cam B Both Approach", 2.2f, 2.2f, new Vector3(-2.50f, 1.62f, 218.6f), new Vector3(-2.40f, 1.55f, 219.4f), new Vector3(0.65f, 1.15f, 221.3f));
        cs.Shot("Cam C Talk Two Shot", 4.4f, 8.8f, new Vector3(-2.35f, 1.48f, 221.05f), new Vector3(-2.20f, 1.42f, 220.90f), meet);
        cs.Shot("Cam D Haru Leaves", 13.2f, 2.8f, new Vector3(-2.25f, 1.58f, 223.4f), new Vector3(-2.10f, 1.52f, 222.2f), meet);

        cs.Hand(yuki, 0f, 16f, "Book", "Notebook");
        cs.Hand(haru, 0f, 12.2f, "Book", "Notebook");

        cs.Line(0.3f, 3.6f, "Yuki", "Science class is finally over... Let me finish my drawing.", "M6_01_Yuki");
        cs.Line(4.3f, 1.4f, "Haru", "Ah- sorry!", "M6_02_Haru");
        cs.Line(5.8f, 2.4f, "Haru", "Are you okay? I wasn't looking.", "M6_03_Haru");
        cs.Line(8.3f, 2.4f, "Yuki", "I-I'm fine! It was my fault!", "M6_04_Yuki");
        cs.Line(10.8f, 2.3f, "Haru", "Here's your notebook. I'm late for science club!", "M6_05_Haru");
        cs.Line(13.4f, 2.4f, "Yuki", "...He was kind of cute.", "M6_06_Yuki");

        log.AppendLine("HARU WALK_FWD corridor");
        cs.HideAmbient("SchoolNPCs/InsideSchool");
        return cs.Finish();
    }

    static PlayableDirector BuildMeiCutscene(Transform level)
    {
        var cs = new Cutscene(level, "M6 Mei Cutscene", AssetDir + "/M6_Mei.playable", 13f, false);
        float yukiYaw = YawTo(MeiPoint, MeiPos);
        var yuki = cs.AddYuki("Yuki", MeiPoint + Vector3.up * 0.05f, yukiYaw);
        var mei = cs.AddCharacter("GirlStudent", "Mei", MeiPos, YawTo(MeiPos, MeiPoint));

        cs.Anim(mei, "male_talk_stand", 0f, 3.3f, true);
        cs.Anim(mei, "FemaleIdle", 3.3f, 3.3f, true);
        cs.Anim(mei, "male_talk_stand", 6.6f, 4f, true);
        cs.Anim(mei, "Thoughtful Head Nod", 10.6f, 2.4f, false);
        cs.Anim(yuki, "FemaleIdle", 0f, 3.3f, true);
        cs.Anim(yuki, "male_talk_stand", 3.3f, 3.3f, true);
        cs.Anim(yuki, "FemaleIdle", 6.6f, 4f, true);
        cs.Anim(yuki, "male_talk_stand", 10.6f, 2.4f, true);

        Vector3 meiHead = MeiPos + Vector3.up * 1.45f, yukiHead = MeiPoint + Vector3.up * 1.4f;
        cs.Shot("Cam A Two Shot", 0f, 3.3f, new Vector3(1.4f, 1.5f, 251.6f), new Vector3(1.3f, 1.5f, 251.3f), (meiHead + yukiHead) * 0.5f - Vector3.up * 0.25f);
        cs.Shot("Cam B Yuki Close", 3.3f, 3.3f, OverShoulder(MeiPos, MeiPoint, 0.75f, 0.35f, 1.55f), OverShoulder(MeiPos, MeiPoint, 0.6f, 0.35f, 1.55f), yukiHead);
        cs.Shot("Cam C Mei Close", 6.6f, 4f, OverShoulder(MeiPoint, MeiPos, 0.75f, -0.35f, 1.55f), OverShoulder(MeiPoint, MeiPos, 0.6f, -0.35f, 1.55f), meiHead);
        cs.Shot("Cam D Two Shot", 10.6f, 2.4f, new Vector3(1.3f, 1.45f, 251.3f), new Vector3(1.2f, 1.45f, 251.0f), (meiHead + yukiHead) * 0.5f - Vector3.up * 0.25f);

        cs.Hand(yuki, 0f, 13f, "Book", "Notebook");

        cs.Line(0.3f, 3.0f, "Mei", "Yuki! Why is your face so red?", "M6_10_Mei");
        cs.Line(3.4f, 3.1f, "Yuki", "Mei, have you seen Haru Sato? I need to find him. Now!", "M6_11_Yuki");
        cs.Line(6.7f, 3.8f, "Mei", "Haru? He just left for the cafe... Ooh~ is this about a boy?", "M6_12_Mei");
        cs.Line(10.7f, 2.2f, "Yuki", "It's about a NOTEBOOK!", "M6_13_Yuki");

        cs.HideAmbient("SchoolNPCs/InsideSchool");
        return cs.Finish();
    }

    static PlayableDirector BuildCafeCutscene(Transform level, GameObject waitingHaru)
    {
        var cs = new Cutscene(level, "M6 Cafe Cutscene", AssetDir + "/M6_Cafe.playable", 23f, false);
        float yukiYaw = YawTo(CafeYuki, CafeHaru);
        float haruYaw = YawTo(CafeHaru, CafeYuki);
        var yuki = cs.AddYuki("Yuki", CafeYukiFrom, yukiYaw);
        var haru = cs.AddCharacter("BoyStudent", "Haru", CafeHaru, haruYaw);

        var yukiMove = cs.Move(yuki.mover, new[] { 0f, 1.8f, 23f }, new[] { CafeYukiFrom, CafeYuki, CafeYuki }, new[] { yukiYaw, yukiYaw, yukiYaw });
        yukiMove.lockChildLocalPosition = true;
        var haruMove = cs.Move(haru.mover, new[] { 0f, 23f }, new[] { CafeHaru, CafeHaru }, new[] { haruYaw, haruYaw });
        haruMove.lockChildLocalPosition = true;
        if (waitingHaru != null)
            cs.HideTarget(waitingHaru);
        cs.Anim(yuki, "IP_Mvm_Walk_Loop", 0f, 1.8f, true);
        cs.Anim(yuki, "male_talk_stand", 1.8f, 2.2f, true);
        cs.Anim(yuki, "FemaleIdle", 4f, 4f, true);
        cs.Anim(yuki, "male_talk_stand", 8f, 4f, true);
        cs.Anim(yuki, "FemaleIdle", 12f, 4f, true);
        cs.Anim(yuki, "male_talk_stand", 16f, 2.6f, true);
        cs.Anim(yuki, "FemaleIdle", 18.6f, 1f, true);
        cs.Anim(yuki, "f02_greet_03", 19.6f, 3.4f, false);

        cs.Anim(haru, "Idle", 0f, 4f, true);
        cs.Anim(haru, "male_talk_stand", 4f, 4f, true);
        cs.Anim(haru, "Idle", 8f, 4f, true);
        cs.Anim(haru, "male_talk_stand", 12f, 8f, true);
        cs.Anim(haru, "Idle", 20f, 3f, true);
        log.AppendLine("CAFE NO NOD");

        Vector3 haruHead = CafeHaru + Vector3.up * 1.6f, yukiHead = CafeYuki + Vector3.up * 1.42f;
        cs.Shot("Cam A Wide Cafe", 0f, 4f, new Vector3(3.15f, 1.55f, 62.35f), new Vector3(3.35f, 1.50f, 62.05f), (haruHead + yukiHead) * 0.5f - Vector3.up * 0.2f);
        cs.Shot("Cam B Haru Close", 4f, 4f, OverShoulder(CafeYuki, CafeHaru, 0.7f, 0.35f, 1.6f), OverShoulder(CafeYuki, CafeHaru, 0.55f, 0.35f, 1.6f), haruHead);
        cs.Shot("Cam C Yuki Close", 8f, 4f, OverShoulder(CafeHaru, CafeYuki, 0.7f, -0.35f, 1.62f), OverShoulder(CafeHaru, CafeYuki, 0.55f, -0.35f, 1.62f), yukiHead);
        cs.Shot("Cam D Haru Close", 12f, 4f, OverShoulder(CafeYuki, CafeHaru, 0.9f, 0.35f, 1.58f), OverShoulder(CafeYuki, CafeHaru, 0.75f, 0.32f, 1.55f), haruHead);
        cs.Shot("Cam E Two Shot", 16f, 7f, TwoShot(CafeYuki, CafeHaru, 2.7f, 1.5f), TwoShot(CafeYuki, CafeHaru, 2.4f, 1.45f), (haruHead + yukiHead) * 0.5f - Vector3.up * 0.35f);

        cs.Hand(yuki, 0f, 23f, "Book", "Notebook");
        cs.Hand(haru, 0f, 23f, "Book", "Notebook");

        cs.Line(0.3f, 3.5f, "Yuki", "Haru Sato! You have my notebook!", "M6_14_Yuki");
        cs.Line(4.1f, 3.8f, "Haru", "Ah, the girl from the corridor! So this one is yours...", "M6_15_Haru");
        cs.Line(8.1f, 3.8f, "Yuki", "D-did you... look inside?", "M6_16_Yuki");
        cs.Line(12.1f, 3.8f, "Haru", "Only one page. Your drawings are really good.", "M6_17_Haru");
        cs.Line(16.1f, 2.5f, "Yuki", "You saw THOSE?!", "M6_18_Yuki");
        cs.Line(18.7f, 2.4f, "Haru", "Hey... could you help me with math sometime?", "M6_19_Haru");
        cs.Line(21.2f, 1.8f, "Yuki", "...Okay!", "M6_20_Yuki");

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

        public Actor AddCharacter(string prefabName, string name, Vector3 pos, float yaw)
        {
            var actor = new Actor();
            actor.mover = NewChild(root.transform, name, pos, Quaternion.Euler(0, yaw, 0));
            var body = MakeCharacter(prefabName, actor.mover, prefabName, pos, yaw, null);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.identity;
            var anim = body.GetComponent<Animator>();
            if (anim != null)
                Prepare(anim);
            actor.animators.Add(anim);
            HandProp.HideAll(actor.mover);
            ShowOnlyDuringCutscene(actor);
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
                tc.displayName = clipName;
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

        public GameObject Prop(string name, Vector3 pos, float yaw, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos + Vector3.up * 0.012f;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = new Vector3(0.21f, 0.024f, 0.29f);
            go.GetComponent<Renderer>().sharedMaterial = NotebookMaterial(color);
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
            HideTarget(target.gameObject);
        }

        public void HideTarget(GameObject target)
        {
            if (target == null)
                return;
            var holder = NewChild(root.transform, "Hide " + target.name, Vector3.zero, Quaternion.identity).gameObject;
            holder.AddComponent<CutsceneVisibilityOverride>().targets = new[] { target };
            holder.SetActive(false);
            Show(holder, 0f, duration, "Hide " + target.name);
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

    static GameObject MakeTaskPoint(GameObject template, Transform parent, string name, Vector3 pos, Vector3[] path)
    {
        var go = Object.Instantiate(template, parent);
        go.name = name;
        go.SetActive(false);
        Vector3 delta = pos - go.transform.position;
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

    static ClueTaskPoint.Line Clue(string speaker, string text, string voiceId = null)
    {
        var line = new ClueTaskPoint.Line { speaker = speaker, text = text };
        MissionVocals.BindClue(line, voiceId, log);
        return line;
    }

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

    // Camera behind 'from' looking toward 'to' (over the shoulder of whoever stands at 'from').
    static Vector3 OverShoulder(Vector3 from, Vector3 to, float back, float side, float height)
    {
        Vector3 d = to - from; d.y = 0; d.Normalize();
        Vector3 right = new Vector3(d.z, 0, -d.x);
        Vector3 p = from - d * back + right * side;
        p.y = height;
        return p;
    }

    // Camera to the side of two people standing at a and b.
    static Vector3 TwoShot(Vector3 a, Vector3 b, float dist, float height)
    {
        Vector3 d = b - a; d.y = 0; d.Normalize();
        Vector3 left = new Vector3(-d.z, 0, d.x);
        Vector3 p = (a + b) * 0.5f + left * dist;
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

    static Material NotebookMaterial(Color color)
    {
        string path = AssetDir + "/Notebook_" + ColorUtility.ToHtmlStringRGB(color) + ".mat";
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
            AssetDatabase.CreateFolder("Assets/NewLevels", "Mission6");
    }

    static string OutPath(string name) => System.IO.Path.Combine(System.IO.Path.GetTempPath(), name);

    // ---------------------------------------------------------------- preview (for checking camera framing)

    /// <summary>Renders the active cutscene camera at the times in PreviewRequest.txt into PNGs in the temp folder.</summary>
    [MenuItem("Tools/Mission 6/Render Preview Shots")]
    public static void RenderPreview()
    {
        var sb = new StringBuilder();
        try
        {
            var req = System.IO.File.ReadAllLines(OutPath("m6_preview_request.txt"));
            var lh = Object.FindObjectOfType<Level_Handler>(true);
            var am = lh.AnimeSchoolManager;
            var zone = Object.FindObjectOfType<SchoolZoneManager>(true);
            Transform level = lh.transform.Find("Level_5");

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
                string file = OutPath("m6_" + cutName.Replace(" ", "_") + "_" + time.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ".png");
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
        System.IO.File.WriteAllText(OutPath("m6_preview.txt"), sb.ToString());
    }
}
