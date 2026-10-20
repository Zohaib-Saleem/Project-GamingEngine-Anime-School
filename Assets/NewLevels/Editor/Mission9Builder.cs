using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Builds Mission 9 "The Misunderstanding" into Levels/Level_8 (gameplay slot 8).
/// Starts outdoors near the court/parking where Mission 8 ended.
/// Story is written plainly so the player always knows what Yuki thinks and what is true.
/// Editor only.
/// </summary>
public static class Mission9Builder
{
    const int Slot = 8;
    const string AssetDir = "Assets/NewLevels/Mission9";
    const string ScenePath = "Assets/Scenes/GameScenes/Gameplay.unity";

    // Start near parking cars between court and school road (Mission 8 ended at court).
    static readonly Vector3 ParkYuki = new Vector3(24.0f, 0.05f, -22.0f);
    static readonly Vector3 ParkHaru = new Vector3(28.5f, 0.05f, -26.5f);
    static readonly Vector3 ParkRina = new Vector3(30.2f, 0.05f, -25.0f);
    static readonly Vector3 ParkSpawn = new Vector3(21.2f, 0.05f, -18.5f); // after intro: near parking, path to gate clear

    static readonly Vector3 GateMei = new Vector3(54.0f, 0.05f, 5.5f);
    static readonly Vector3 GatePoint = new Vector3(57.6f, 0f, 4.4f);
    static readonly Vector3 EnterPoint = new Vector3(57.6f, 0f, 4.4f);

    static readonly Vector3 InsideSpawn = new Vector3(-0.4f, 0f, 253.6f);
    // Aisle next to Haru's desk (not on top of furniture)
    static readonly Vector3 HaruDesk = new Vector3(3.22f, 0.05f, 241.50f);
    static readonly Vector3 DeskStand = new Vector3(4.20f, 0.05f, 242.80f);
    static readonly Vector3 DeskPoint = new Vector3(4.0f, 0f, 243.0f);
    // Open hallway outside classroom (same corridor M8 uses toward exit)
    static readonly Vector3 CorridorYuki = new Vector3(0.8f, 0.05f, 250.8f);
    static readonly Vector3 CorridorRina = new Vector3(-0.6f, 0.05f, 252.2f);
    static readonly Vector3 CorridorPoint = new Vector3(0.2f, 0f, 251.4f);

    static StringBuilder log;

    [MenuItem("Tools/Mission 9/Build")]
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
            level.name = "Level_8";

            var spawn = NewChild(level, "M9 Player Spawn", ParkSpawn, Quaternion.identity);
            var camSpawn = NewChild(level, "M9 Camera Spawn", ParkSpawn + new Vector3(0f, 1.65f, -3.2f), Quaternion.identity);
            var npcHolder = NewChild(level, "M9 Mission NPCs", Vector3.zero, Quaternion.identity);

            // 1) Find Mei — she helps Yuki calm down and think
            var t1 = MakeTaskPoint(template, level, "M9_01 Find Mei At Gate", GatePoint,
                new[]
                {
                    ParkSpawn, new Vector3(30f, 0, -10f), new Vector3(40f, 0, -2f), new Vector3(48f, 0, 2f), GatePoint
                });
            t1.AddComponent<TaskPointHit>();

            // 2) Enter school to investigate the truth
            var t2 = MakeTaskPoint(template, level, "M9_02 Go Inside School", EnterPoint,
                new[] { GatePoint, EnterPoint });
            t2.AddComponent<SchoolZoneTrigger>().action = SchoolZoneTrigger.ZoneAction.EnterSchool;

            // 3) Read Haru's desk note
            var t3 = MakeTaskPoint(template, level, "M9_03 Check Haru Desk", DeskPoint,
                new[] { InsideSpawn, new Vector3(-0.2f, 0, 249.0f), new Vector3(1.5f, 0, 246.0f), DeskPoint });
            t3.AddComponent<TaskPointHit>();

            // 4) Ask Rina directly
            var t4 = MakeTaskPoint(template, level, "M9_04 Find Rina In Corridor", CorridorPoint,
                new[] { DeskPoint, new Vector3(1.5f, 0, 246.0f), new Vector3(0.5f, 0, 249.0f), CorridorPoint });
            t4.AddComponent<TaskPointHit>();

            var standCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/NPC/Animators/NPC_Stand.controller");
            var meiNpc = MakeCharacter("GirlStudent", npcHolder, "Mei (waiting at gate)", GateMei, YawTo(GateMei, GatePoint), standCtrl);
            var rinaNpc = MakeCharacter("GirlStudent", npcHolder, "Rina (waiting in corridor)", CorridorRina, YawTo(CorridorRina, CorridorYuki), standCtrl);
            AddMirror(npcHolder.gameObject, t1, meiNpc);
            AddMirror(npcHolder.gameObject, t4, rinaNpc);

            var intro = BuildIntro(level);
            var meiCut = BuildMeiCutscene(level);
            var noteCut = BuildNoteCutscene(level);
            var rinaCut = BuildRinaCutscene(level);

            Undo.RecordObject(lh, "Mission 9");
            var L = lh.levelSpawnPoints[Slot];
            L.Level = level.gameObject;
            L.playerSpawnPoint = spawn;
            L.CameraSp = camSpawn;
            L.Cutscene = intro;
            L.LeveltaskParticle = new[] { t1, t2, t3, t4 };
            L.TaskCutscenes = new GameObject[] { meiCut.gameObject, null, noteCut.gameObject, rinaCut.gameObject };
            L.LevelHints = new[]
            {
                "FIND MEI AT THE SCHOOL GATE",
                "GO INSIDE THE SCHOOL BUILDING",
                "READ THE NOTE ON HARU'S DESK",
                "ASK RINA WHAT IS GOING ON",
            };
            L.playerCarryItem = null;
            L.showCarryAfterTask = -1;
            L.hideCarryOnReachTask = -1;
            L.showCarryAtStart = false;
            L.hasMathTest = false;
            L.mathTestSetIndex = 0;
            L.startInsideSchool = false;
            L.startInMyRoom = false;
            L.allowBike = false;
            L.RewardAmount = 2600;
            L.levelTime = 220f;
            EditorUtility.SetDirty(lh);
            EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
            AssetDatabase.SaveAssets();
            log.AppendLine("BUILD OK slot=" + Slot + " level=" + level.name);
        }
        catch (System.Exception e) { log.AppendLine("ERR " + e); }
        System.IO.File.WriteAllText(OutPath("m9_build.txt"), log.ToString());
    }

    public static void PreparePlay()
    {
        PlayerPrefs.SetInt("SelectedLevel", 8);
        PlayerPrefs.Save();
        System.IO.File.WriteAllText(OutPath("m9_play.txt"), "SelectedLevel=8");
    }

    [MenuItem("Tools/Mission 9/Save Scene")]
    public static void SaveScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        bool ok = scene.path == ScenePath && EditorSceneManager.SaveScene(scene);
        System.IO.File.WriteAllText(OutPath("m9_save.txt"), ok ? "SAVED" : "NOT SAVED (" + scene.path + ")");
    }

    // ---------------------------------------------------------------- cutscenes (plain dialogue)

    static PlayableDirector BuildIntro(Transform level)
    {
        // Player sees Haru + Rina together and thinks they are a couple.
        var cs = new Cutscene(level, "M9 Intro Cutscene", AssetDir + "/M9_Intro.playable", 16f, true);
        var yuki = cs.AddYuki("Yuki", ParkYuki, YawTo(ParkYuki, ParkHaru));
        var haru = cs.AddCharacter("BoyStudent", "Haru", ParkHaru, YawTo(ParkHaru, ParkRina));
        var rina = cs.AddCharacter("GirlStudent", "Rina", ParkRina, YawTo(ParkRina, ParkHaru));

        // Stay put for the hurt close-up, then take a few steps toward the gate.
        // In-place walk + mover (not walk_fwd) so root motion cannot skate her.
        Vector3 yukiLeave = ParkYuki + new Vector3(-1.6f, 0f, 2.2f);
        float faceCouple = YawTo(ParkYuki, ParkHaru);
        float faceLeave = YawTo(ParkYuki, yukiLeave);
        var yukiMove = cs.Move(yuki.mover, new[] { 0f, 13.0f, 15.5f, 16f },
            new[] { ParkYuki, ParkYuki, yukiLeave, yukiLeave },
            new[] { faceCouple, faceCouple, faceLeave, faceLeave });
        yukiMove.lockChildLocalPosition = true;
        var haruMove = cs.Move(haru.mover, new[] { 0f, 16f }, new[] { ParkHaru, ParkHaru }, new[] { YawTo(ParkHaru, ParkRina), YawTo(ParkHaru, ParkRina) });
        haruMove.lockChildLocalPosition = true;
        var rinaMove = cs.Move(rina.mover, new[] { 0f, 16f }, new[] { ParkRina, ParkRina }, new[] { YawTo(ParkRina, ParkHaru), YawTo(ParkRina, ParkHaru) });
        rinaMove.lockChildLocalPosition = true;

        cs.Anim(yuki, "male_talk_stand", 0.2f, 3.6f, true);
        cs.Anim(yuki, "FemaleIdle", 3.8f, 4.2f, true);
        cs.Anim(yuki, "male_talk_stand", 8.0f, 5.0f, true);
        cs.Anim(yuki, "IP_Mvm_Walk_Loop", 13.0f, 3.0f, true);
        cs.Anim(haru, "Idle", 0f, 16f, true);
        cs.Anim(rina, "FemaleIdle", 0f, 4.0f, true);
        cs.Anim(rina, "male_talk_stand", 4.0f, 3.8f, true);
        cs.Anim(rina, "FemaleIdle", 7.8f, 8.2f, true);

        cs.Hand(haru, 5.5f, 5.5f, "Gift");

        Vector3 coupleMid = (ParkHaru + ParkRina) * 0.5f + Vector3.up * 1.3f;
        // Keep parking asphalt / cars in frame — never aim only at sky/mountains.
        cs.Shot("Cam A Watch From Behind", 0f, 4.0f,
            ParkYuki + new Vector3(-2.0f, 1.65f, 1.8f), ParkYuki + new Vector3(-1.6f, 1.55f, 1.4f), coupleMid);
        cs.Shot("Cam B Couple Talk", 4.0f, 4.5f,
            coupleMid + new Vector3(2.4f, 0.35f, -1.8f), coupleMid + new Vector3(2.0f, 0.25f, -1.4f), coupleMid);
        cs.Shot("Cam C Gift Hide", 8.5f, 2.5f,
            ParkHaru + new Vector3(-1.6f, 1.45f, -1.0f), ParkHaru + new Vector3(-1.35f, 1.4f, -0.75f), ParkHaru + new Vector3(-0.15f, 1.05f, 0.05f));
        // Face close-up, then a short follow as she turns and walks off.
        Vector3 faceDir = ParkHaru - ParkYuki; faceDir.y = 0f; faceDir.Normalize();
        Vector3 faceRight = new Vector3(faceDir.z, 0f, -faceDir.x);
        Vector3 faceCamA = ParkYuki + faceDir * 1.45f + faceRight * 0.55f; faceCamA.y = 1.58f;
        Vector3 faceCamB = ParkYuki + faceDir * 1.30f + faceRight * 0.45f; faceCamB.y = 1.52f;
        cs.Shot("Cam D Yuki Hurt", 11.0f, 2.6f, faceCamA, faceCamB, ParkYuki + new Vector3(0f, 1.32f, 0f));
        Vector3 leaveDir = yukiLeave - ParkYuki; leaveDir.y = 0f; leaveDir.Normalize();
        Vector3 leaveRight = new Vector3(leaveDir.z, 0f, -leaveDir.x);
        Vector3 leaveOff = leaveDir * 1.85f + leaveRight * 0.42f;
        leaveOff.y = 1.50f;
        cs.ShotFollow("Cam E Yuki Leaves", 13.6f, 2.4f, yuki.mover, leaveOff, Vector3.up * 1.32f);

        // Very clear lines — player must understand Yuki's wrong conclusion.
        cs.Line(0.3f, 3.5f, "Yuki", "Wait... Haru is with Rina again?", "M9_01_Yuki");
        cs.Line(4.0f, 3.8f, "Rina", "Haha! You're so nervous about this, Haru.", "M9_02_Rina");
        cs.Line(8.0f, 2.8f, "Yuki", "He hid something behind his back... a gift?", "M9_03_Yuki");
        cs.Line(11.0f, 2.5f, "Yuki", "So that's how it is. He likes her.", "M9_04_Yuki");
        cs.Line(13.6f, 2.2f, "Yuki", "I can't watch this. I have to leave.", "M9_05_Yuki");

        cs.HideAmbient("SchoolNPCs/OutsideSchool");
        return cs.Finish();
    }

    static PlayableDirector BuildMeiCutscene(Transform level)
    {
        var cs = new Cutscene(level, "M9 Mei Cutscene", AssetDir + "/M9_Mei.playable", 14f, false);
        var yuki = cs.AddYuki("Yuki", GatePoint + new Vector3(-1.2f, 0.05f, 0.4f), YawTo(GatePoint, GateMei));
        var mei = cs.AddCharacter("GirlStudent", "Mei", GateMei, YawTo(GateMei, GatePoint));

        Vector3 yukiPos = GatePoint + new Vector3(-1.2f, 0.05f, 0.4f);
        cs.Move(yuki.mover, new[] { 0f, 14f }, new[] { yukiPos, yukiPos }, new[] { YawTo(yukiPos, GateMei), YawTo(yukiPos, GateMei) });
        cs.Move(mei.mover, new[] { 0f, 14f }, new[] { GateMei, GateMei }, new[] { YawTo(GateMei, yukiPos), YawTo(GateMei, yukiPos) });

        cs.Anim(yuki, "FemaleIdle", 0f, 3.5f, true);
        cs.Anim(yuki, "male_talk_stand", 3.5f, 3.9f, true);
        cs.Anim(yuki, "FemaleIdle", 7.4f, 6.6f, true);
        cs.Anim(mei, "male_talk_stand", 0.2f, 3.3f, true);
        cs.Anim(mei, "FemaleIdle", 3.5f, 3.9f, true);
        cs.Anim(mei, "male_talk_stand", 7.4f, 3.6f, true);
        cs.Anim(mei, "f02_greet_03", 11.0f, 3.0f, false);

        Vector3 mid = (yukiPos + GateMei) * 0.5f + Vector3.up * 1.35f;
        cs.Shot("Cam A Two Shot", 0f, 5.0f, mid + new Vector3(2.6f, 0.2f, -1.8f), mid + new Vector3(2.2f, 0.15f, -1.5f), mid);
        cs.Shot("Cam B Mei Close", 5.0f, 5.0f, yukiPos + new Vector3(0.3f, 1.5f, -0.2f) + (GateMei - yukiPos).normalized * -0.2f,
            OverShoulder(yukiPos, GateMei, 0.7f, -0.25f, 1.52f), GateMei + Vector3.up * 1.4f);
        cs.Shot("Cam C Yuki Close", 10.0f, 4.0f, OverShoulder(GateMei, yukiPos, 0.7f, 0.25f, 1.5f), OverShoulder(GateMei, yukiPos, 0.55f, 0.25f, 1.5f), yukiPos + Vector3.up * 1.35f);

        cs.Line(0.3f, 3.2f, "Mei", "Yuki? You look upset. What happened?", "M9_06_Mei");
        cs.Line(3.6f, 3.6f, "Yuki", "Haru was with Rina. He even hid a gift. I think he likes her.", "M9_07_Yuki");
        cs.Line(7.4f, 3.4f, "Mei", "Don't guess. Guessing will only hurt you.", "M9_08_Mei");
        cs.Line(11.0f, 2.8f, "Mei", "Go find the truth. Check his desk. Ask Rina yourself.", "M9_09_Mei");

        cs.HideAmbient("SchoolNPCs/OutsideSchool");
        return cs.Finish();
    }

    static PlayableDirector BuildNoteCutscene(Transform level)
    {
        var cs = new Cutscene(level, "M9 Note Cutscene", AssetDir + "/M9_Note.playable", 14f, false);
        float yaw = YawTo(DeskStand, HaruDesk);
        var yuki = cs.AddYuki("Yuki", DeskStand, yaw);

        cs.Move(yuki.mover, new[] { 0f, 14f }, new[] { DeskStand, DeskStand }, new[] { yaw, yaw });
        cs.Anim(yuki, "FemaleIdle", 0f, 14f, true);

        // Same desk paper already visible in Cam A — keep it there for Cam B push-in.
        Vector3 notePos = new Vector3(4.55f, 1.00f, 241.95f);
        var note = cs.Prop("Haru Note", notePos, -25f, new Color(1f, 0.96f, 0.82f));
        note.transform.localScale = new Vector3(0.30f, 0.02f, 0.38f);
        cs.Show(note, 0f, 14f);

        // Aisle side two-shot — Yuki readable, note on desk visible.
        cs.Shot("Cam A Desk Wide", 0f, 4.0f,
            new Vector3(6.10f, 1.58f, 242.40f), new Vector3(5.70f, 1.52f, 242.70f), DeskStand + Vector3.up * 1.2f);
        // Push in from Cam A toward the note (same side — never through Yuki).
        cs.Shot("Cam B Note Close", 4.0f, 6.0f,
            new Vector3(5.55f, 1.42f, 241.55f), new Vector3(5.25f, 1.32f, 241.70f), notePos);
        cs.Shot("Cam C Yuki Realize", 10.0f, 4.0f,
            new Vector3(5.40f, 1.52f, 241.60f), new Vector3(5.10f, 1.48f, 241.90f), DeskStand + Vector3.up * 1.35f);

        // Note text spoken clearly so player understands the truth.
        cs.Line(0.3f, 3.2f, "Yuki", "This is Haru's desk... There's a note.", "M9_10_Yuki");
        cs.Line(3.7f, 5.0f, "Note", "\"Rina, thanks for helping me pick a gift. I hope YUKI likes it.\"", "M9_11_Note");
        cs.Line(9.0f, 2.4f, "Yuki", "A gift... for me?", "M9_12_Yuki");
        cs.Line(11.5f, 2.3f, "Yuki", "Then I was wrong. I need to ask Rina.", "M9_13_Yuki");

        cs.HideAmbient("SchoolNPCs/InsideSchool");
        return cs.Finish();
    }

    static PlayableDirector BuildRinaCutscene(Transform level)
    {
        var cs = new Cutscene(level, "M9 Rina Cutscene", AssetDir + "/M9_Rina.playable", 16f, false);
        Vector3 yukiPos = CorridorYuki;
        var yuki = cs.AddYuki("Yuki", yukiPos, YawTo(yukiPos, CorridorRina));
        var rina = cs.AddCharacter("GirlStudent", "Rina", CorridorRina, YawTo(CorridorRina, yukiPos));

        cs.Move(yuki.mover, new[] { 0f, 16f }, new[] { yukiPos, yukiPos }, new[] { YawTo(yukiPos, CorridorRina), YawTo(yukiPos, CorridorRina) });
        cs.Move(rina.mover, new[] { 0f, 16f }, new[] { CorridorRina, CorridorRina }, new[] { YawTo(CorridorRina, yukiPos), YawTo(CorridorRina, yukiPos) });

        cs.Anim(yuki, "male_talk_stand", 0.2f, 3.2f, true);
        cs.Anim(yuki, "FemaleIdle", 3.4f, 8.1f, true);
        cs.Anim(yuki, "male_talk_stand", 11.5f, 2.4f, true);
        cs.Anim(yuki, "FemaleIdle", 13.9f, 2.1f, true);
        cs.Anim(rina, "FemaleIdle", 0f, 3.4f, true);
        cs.Anim(rina, "male_talk_stand", 3.4f, 8.1f, true);
        cs.Anim(rina, "FemaleIdle", 11.5f, 2.4f, true);
        cs.Anim(rina, "male_talk_stand", 13.9f, 2.1f, true);

        Vector3 mid = (yukiPos + CorridorRina) * 0.5f + Vector3.up * 1.35f;
        // Side two-shot along the hallway (not into a wall).
        cs.Shot("Cam A Two Shot", 0f, 5.0f,
            mid + new Vector3(2.8f, 0.15f, 0.2f), mid + new Vector3(2.4f, 0.1f, 0.1f), mid);
        cs.Shot("Cam B Rina Close", 5.0f, 6.0f, OverShoulder(yukiPos, CorridorRina, 0.7f, -0.3f, 1.52f), OverShoulder(yukiPos, CorridorRina, 0.55f, -0.3f, 1.52f), CorridorRina + Vector3.up * 1.4f);
        cs.Shot("Cam C Yuki Relief", 11.0f, 5.0f, OverShoulder(CorridorRina, yukiPos, 0.7f, 0.3f, 1.5f), OverShoulder(CorridorRina, yukiPos, 0.55f, 0.3f, 1.5f), yukiPos + Vector3.up * 1.35f);

        cs.Line(0.3f, 3.0f, "Yuki", "Rina... Are you and Haru... together?", "M9_14_Yuki");
        cs.Line(3.5f, 4.2f, "Rina", "Together? No. I'm his cousin, silly.", "M9_15_Rina");
        cs.Line(7.8f, 3.6f, "Rina", "He asked me to help choose a gift for YOU.", "M9_16_Rina");
        cs.Line(11.6f, 2.2f, "Yuki", "For me... Truly?", "M9_17_Yuki");
        cs.Line(13.9f, 1.9f, "Rina", "Yes. He's waiting at the court. At sunset.", "M9_18_Rina");

        cs.HideAmbient("SchoolNPCs/InsideSchool");
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

        public CutsceneMover ShotFollow(string name, float start, float length, Transform target, Vector3 offset, Vector3 lookOffset)
        {
            Vector3 pos = target.position + offset;
            Vector3 look = target.position + lookOffset;
            var cam = NewChild(root.transform, name, pos, Quaternion.LookRotation(look - pos)).gameObject;
            var c = cam.AddComponent<Camera>();
            c.fieldOfView = 45f;
            c.nearClipPlane = 0.05f;
            c.depth = 20;
            cam.SetActive(false);
            var m = cam.AddComponent<CutsceneMover>();
            m.director = director;
            m.followTarget = target;
            m.followOffset = offset;
            m.followLookOffset = lookOffset;
            m.yawDegrees = false;
            m.times = new[] { start, start + length };
            m.positions = new[] { pos, pos };
            Show(cam, start, length, name);
            return m;
        }

        public GameObject Prop(string name, Vector3 pos, float yaw, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = new Vector3(0.18f, 0.04f, 0.24f);
            go.GetComponent<Renderer>().sharedMaterial = SolidMaterial("Note", color);
            go.SetActive(false);
            return go;
        }

        public GameObject GiftBox(string name, Vector3 pos, float yaw, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);

            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.transform.SetParent(go.transform, false);
            box.transform.localPosition = Vector3.zero;
            box.transform.localScale = new Vector3(0.16f, 0.12f, 0.16f);
            box.GetComponent<Renderer>().sharedMaterial = SolidMaterial("Gift", color);

            var ribbon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(ribbon.GetComponent<Collider>());
            ribbon.transform.SetParent(go.transform, false);
            ribbon.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            ribbon.transform.localScale = new Vector3(0.18f, 0.03f, 0.04f);
            ribbon.GetComponent<Renderer>().sharedMaterial = SolidMaterial("GiftRibbon", new Color(1f, 0.85f, 0.2f));

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

        Transform level = lh.transform.Find("Level_8");
        if (level == null)
        {
            var go = new GameObject("Level_8");
            go.transform.SetParent(lh.transform, false);
            go.SetActive(false);
            level = go.transform;
            log.AppendLine("created Level_8");
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
            AssetDatabase.CreateFolder("Assets/NewLevels", "Mission9");
    }

    static string OutPath(string name) => System.IO.Path.Combine(System.IO.Path.GetTempPath(), name);

    [MenuItem("Tools/Mission 9/Render Preview Shots")]
    public static void RenderPreview()
    {
        var sb = new StringBuilder();
        try
        {
            var req = System.IO.File.ReadAllLines(OutPath("m9_preview_request.txt"));
            var lh = Object.FindObjectOfType<Level_Handler>(true);
            var am = lh.AnimeSchoolManager;
            var zone = Object.FindObjectOfType<SchoolZoneManager>(true);
            Transform level = lh.transform.Find("Level_8");

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
                string file = OutPath("m9_" + cutName.Replace(" ", "_") + "_" + time.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ".png");
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
        System.IO.File.WriteAllText(OutPath("m9_preview.txt"), sb.ToString());
    }
}
