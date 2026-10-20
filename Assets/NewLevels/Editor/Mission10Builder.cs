using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Builds Mission 10 "Sunset on the Court" into Levels/Level_9 (gameplay slot 9).
/// Continues from Mission 9: Rina said Haru is waiting at the court at sunset.
/// Story is plain: go outside → court → gift + confession.
/// Editor only.
/// </summary>
public static class Mission10Builder
{
    const int Slot = 9;
    const string AssetDir = "Assets/NewLevels/Mission10";
    const string ScenePath = "Assets/Scenes/GameScenes/Gameplay.unity";

    // Start in corridor where Mission 9 ended.
    static readonly Vector3 CorridorYuki = new Vector3(0.8f, 0.05f, 250.8f);
    static readonly Vector3 CorridorRina = new Vector3(-0.6f, 0.05f, 252.2f);
    static readonly Vector3 CorridorMei = new Vector3(1.6f, 0.05f, 252.0f);
    static readonly Vector3 ExitPoint = new Vector3(-0.4f, 0f, 255.5f);
    static readonly Vector3 OutsideGate = new Vector3(57.6f, 0f, 4.4f);

    // Match M8: court mesh top ~0.14–0.18; root ≈ 0.36 plants feet (0.58 floated ~25cm).
    const float CourtY = 0.36f;
    static readonly Vector3 CourtPoint = new Vector3(16.5f, 0f, -50.0f);
    static readonly Vector3 CourtHaru = new Vector3(22.5f, CourtY, -56.0f);
    static readonly Vector3 CourtYuki = new Vector3(14.2f, CourtY, -49.5f);
    // Side of the court — clearly watching, not standing with Yuki.
    static readonly Vector3 CourtBenchMei = new Vector3(8.2f, CourtY, -52.5f);
    static readonly Vector3 CourtBenchRina = new Vector3(9.0f, CourtY, -51.0f);

    static StringBuilder log;

    [MenuItem("Tools/Mission 10/Build")]
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
            level.name = "Level_9";

            var spawn = NewChild(level, "M10 Player Spawn", CorridorYuki, Quaternion.identity);
            var camSpawn = NewChild(level, "M10 Camera Spawn", CorridorYuki + new Vector3(0f, 1.65f, -3.2f), Quaternion.identity);
            var npcHolder = NewChild(level, "M10 Mission NPCs", Vector3.zero, Quaternion.identity);

            var t1 = MakeTaskPoint(template, level, "M10_01 Go Outside School", ExitPoint,
                new[] { CorridorYuki, new Vector3(0.5f, 0, 249.0f), new Vector3(-0.2f, 0, 252.0f), ExitPoint });
            t1.AddComponent<SchoolZoneTrigger>().action = SchoolZoneTrigger.ZoneAction.ExitSchool;

            var t2 = MakeTaskPoint(template, level, "M10_02 Meet Haru At Court", CourtPoint,
                new[]
                {
                    OutsideGate, new Vector3(54.6f, 0, 5.1f), new Vector3(36.9f, 0, 4.8f), new Vector3(27.1f, 0, 4.6f),
                    new Vector3(13.0f, 0, 5.6f), new Vector3(0.6f, 0, 7.6f), new Vector3(-2.6f, 0, 12.1f),
                    new Vector3(8.0f, 0, 0.0f), new Vector3(20.0f, 0, -20.0f), new Vector3(20.0f, 0, -40.0f), CourtPoint
                });
            t2.AddComponent<TaskPointHit>();

            var standCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/NPC/Animators/NPC_Stand.controller");
            var haruNpc = MakeCharacter("BoyStudent", npcHolder, "Haru (waiting at court)", CourtHaru, YawTo(CourtHaru, CourtYuki), standCtrl);
            AddMirror(npcHolder.gameObject, t2, haruNpc);

            var intro = BuildIntro(level);
            var sunset = BuildSunsetCutscene(level);

            Undo.RecordObject(lh, "Mission 10");
            var L = lh.levelSpawnPoints[Slot];
            L.Level = level.gameObject;
            L.playerSpawnPoint = spawn;
            L.CameraSp = camSpawn;
            L.Cutscene = intro;
            L.LeveltaskParticle = new[] { t1, t2 };
            L.TaskCutscenes = new GameObject[] { null, sunset.gameObject };
            L.LevelHints = new[]
            {
                "GO OUTSIDE THE SCHOOL BUILDING",
                "MEET HARU AT THE BASKETBALL COURT",
            };
            L.playerCarryItem = HandPropBuild.Player(lh, "Gift");
            L.showCarryAfterTask = 1;
            L.hideCarryOnReachTask = -1;
            L.showCarryAtStart = false;
            L.hasMathTest = false;
            L.mathTestSetIndex = 0;
            L.startInsideSchool = true;
            L.startInMyRoom = false;
            L.allowBike = false;
            L.RewardAmount = 2800;
            L.levelTime = 180f;
            EditorUtility.SetDirty(lh);
            EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
            AssetDatabase.SaveAssets();
            log.AppendLine("BUILD OK slot=" + Slot + " level=" + level.name);
        }
        catch (System.Exception e) { log.AppendLine("ERR " + e); }
        System.IO.File.WriteAllText(OutPath("m10_build.txt"), log.ToString());
    }

    public static void PreparePlay()
    {
        PlayerPrefs.SetInt("SelectedLevel", 9);
        PlayerPrefs.Save();
        System.IO.File.WriteAllText(OutPath("m10_play.txt"), "SelectedLevel=9");
    }

    [MenuItem("Tools/Mission 10/Save Scene")]
    public static void SaveScene()
    {
        var scene = EditorSceneManager.GetActiveScene();
        bool ok = scene.path == ScenePath && EditorSceneManager.SaveScene(scene);
        System.IO.File.WriteAllText(OutPath("m10_save.txt"), ok ? "SAVED" : "NOT SAVED (" + scene.path + ")");
    }

    // ---------------------------------------------------------------- cutscenes

    static PlayableDirector BuildIntro(Transform level)
    {
        // Right after Mission 9: Rina already told the truth. Now push Yuki to go.
        var cs = new Cutscene(level, "M10 Intro Cutscene", AssetDir + "/M10_Intro.playable", 14f, true);
        var yuki = cs.AddYuki("Yuki", CorridorYuki, YawTo(CorridorYuki, CorridorRina));
        var rina = cs.AddCharacter("GirlStudent", "Rina", CorridorRina, YawTo(CorridorRina, CorridorYuki));
        var mei = cs.AddCharacter("GirlStudent", "Mei", CorridorMei, YawTo(CorridorMei, CorridorYuki));

        cs.Move(yuki.mover, new[] { 0f, 14f }, new[] { CorridorYuki, CorridorYuki }, new[] { YawTo(CorridorYuki, CorridorRina), YawTo(CorridorYuki, CorridorRina) });
        cs.Move(rina.mover, new[] { 0f, 14f }, new[] { CorridorRina, CorridorRina }, new[] { YawTo(CorridorRina, CorridorYuki), YawTo(CorridorRina, CorridorYuki) });
        cs.Move(mei.mover, new[] { 0f, 14f }, new[] { CorridorMei, CorridorMei }, new[] { YawTo(CorridorMei, CorridorYuki), YawTo(CorridorMei, CorridorYuki) });

        cs.Anim(yuki, "FemaleIdle", 0f, 7.3f, true);
        cs.Anim(yuki, "male_talk_stand", 7.3f, 3.0f, true);
        cs.Anim(yuki, "FemaleIdle", 10.3f, 3.7f, true);
        cs.Anim(rina, "male_talk_stand", 0.2f, 3.4f, true);
        cs.Anim(rina, "FemaleIdle", 3.6f, 6.7f, true);
        cs.Anim(rina, "male_talk_stand", 10.3f, 3.7f, true);
        cs.Anim(mei, "FemaleIdle", 0f, 3.6f, true);
        cs.Anim(mei, "male_talk_stand", 3.6f, 3.7f, true);
        cs.Anim(mei, "FemaleIdle", 7.3f, 3.7f, true);
        cs.Anim(mei, "f02_greet_03", 11.0f, 3.0f, false);

        Vector3 mid = (CorridorYuki + CorridorRina) * 0.5f + Vector3.up * 1.35f;
        cs.Shot("Cam A Two Shot", 0f, 5.0f,
            mid + new Vector3(2.8f, 0.15f, 0.2f), mid + new Vector3(2.4f, 0.1f, 0.1f), mid);
        cs.Shot("Cam B Rina Close", 5.0f, 5.0f,
            OverShoulder(CorridorYuki, CorridorRina, 0.7f, -0.3f, 1.52f),
            OverShoulder(CorridorYuki, CorridorRina, 0.55f, -0.3f, 1.52f),
            CorridorRina + Vector3.up * 1.4f);
        cs.Shot("Cam C Yuki Go", 10.0f, 4.0f,
            OverShoulder(CorridorRina, CorridorYuki, 0.7f, 0.3f, 1.5f),
            OverShoulder(CorridorRina, CorridorYuki, 0.55f, 0.3f, 1.5f),
            CorridorYuki + Vector3.up * 1.35f);

        cs.Line(0.3f, 3.2f, "Rina", "Remember? Haru is waiting at the court. At sunset.", "M10_01_Rina");
        cs.Line(3.7f, 3.4f, "Mei", "Don't stand here blushing. Go meet him!", "M10_02_Mei");
        cs.Line(7.3f, 2.8f, "Yuki", "What if I misunderstood everything again?", "M10_03_Yuki");
        cs.Line(10.3f, 3.4f, "Rina", "You won't. Just go outside and run to the court.", "M10_04_Rina");

        cs.HideAmbient("SchoolNPCs/InsideSchool");
        return cs.Finish();
    }

    static PlayableDirector BuildSunsetCutscene(Transform level)
    {
        var cs = new Cutscene(level, "M10 Sunset Cutscene", AssetDir + "/M10_Sunset.playable", 22f, false);
        var yuki = cs.AddYuki("Yuki", CourtYuki, YawTo(CourtYuki, CourtHaru));
        var haru = cs.AddCharacter("BoyStudent", "Haru", CourtHaru, YawTo(CourtHaru, CourtYuki));
        var mei = cs.AddCharacter("GirlStudent", "Mei", CourtBenchMei, YawTo(CourtBenchMei, CourtYuki));
        var rina = cs.AddCharacter("GirlStudent", "Rina", CourtBenchRina, YawTo(CourtBenchRina, CourtHaru));

        Vector3 haruMeet = CourtYuki + new Vector3(1.15f, 0f, -0.35f);
        // ~9.8m walk @ ~1.5 m/s ≈ 6.5s; pin child local pos so walk root-motion cannot skate/sink.
        var yukiMove = cs.Move(yuki.mover, new[] { 0f, 22f }, new[] { CourtYuki, CourtYuki }, new[] { YawTo(CourtYuki, CourtHaru), YawTo(CourtYuki, haruMeet) });
        yukiMove.lockChildLocalPosition = true;
        var haruMove = cs.Move(haru.mover, new[] { 0f, 2.0f, 8.5f, 22f },
            new[] { CourtHaru, CourtHaru, haruMeet, haruMeet },
            new[] { YawTo(CourtHaru, CourtYuki), YawTo(CourtHaru, CourtYuki), YawTo(CourtHaru, CourtYuki), YawTo(haruMeet, CourtYuki) });
        haruMove.lockChildLocalPosition = true;
        var meiMove = cs.Move(mei.mover, new[] { 0f, 22f }, new[] { CourtBenchMei, CourtBenchMei }, new[] { YawTo(CourtBenchMei, CourtYuki), YawTo(CourtBenchMei, haruMeet) });
        meiMove.lockChildLocalPosition = true;
        var rinaMove = cs.Move(rina.mover, new[] { 0f, 22f }, new[] { CourtBenchRina, CourtBenchRina }, new[] { YawTo(CourtBenchRina, CourtHaru), YawTo(CourtBenchRina, haruMeet) });
        rinaMove.lockChildLocalPosition = true;

        cs.Anim(yuki, "male_talk_stand", 0.2f, 3.4f, true);
        cs.Anim(yuki, "FemaleIdle", 3.6f, 9.0f, true);
        cs.Anim(yuki, "male_talk_stand", 12.6f, 3.3f, true);
        cs.Anim(yuki, "FemaleIdle", 15.9f, 6.1f, true);

        cs.Anim(haru, "Idle", 0f, 2.0f, true);
        cs.Anim(haru, "walk_fwd", 2.0f, 6.5f, true);
        cs.Anim(haru, "male_talk_stand", 8.5f, 4.0f, true);
        cs.Anim(haru, "Idle", 12.5f, 3.5f, true);
        cs.Anim(haru, "male_talk_stand", 16.0f, 2.7f, true);
        cs.Anim(haru, "Idle", 18.7f, 3.3f, true);

        cs.Anim(mei, "FemaleIdle", 0f, 18.8f, true);
        cs.Anim(mei, "f02_greet_03", 18.8f, 1.6f, false);
        cs.Anim(mei, "FemaleIdle", 20.4f, 1.6f, true);
        cs.Anim(rina, "FemaleIdle", 0f, 20.4f, true);
        cs.Anim(rina, "Thoughtful Head Nod", 20.4f, 1.6f, false);

        cs.Hand(haru, 2.0f, 10.6f, "Gift");
        cs.Hand(yuki, 12.6f, 9.4f, "Gift");

        Vector3 mid = (CourtYuki + haruMeet) * 0.5f + Vector3.up * 1.25f;
        cs.Shot("Cam A Wide Court", 0f, 4.0f,
            new Vector3(10.5f, 2.4f, -42.0f), new Vector3(12.0f, 2.2f, -44.0f), mid);
        cs.Shot("Cam B Haru Walks", 4.0f, 4.5f,
            new Vector3(12.8f, 1.55f + CourtY, -46.5f), new Vector3(13.4f, 1.5f + CourtY, -47.2f), mid);
        cs.Shot("Cam C Gift Give", 8.5f, 5.0f,
            mid + new Vector3(2.2f, 0.25f, -1.6f), mid + new Vector3(1.9f, 0.2f, -1.3f), mid);
        cs.Shot("Cam D Haru Close", 13.5f, 3.5f,
            OverShoulder(CourtYuki, haruMeet, 0.7f, -0.3f, 1.52f + CourtY),
            OverShoulder(CourtYuki, haruMeet, 0.55f, -0.3f, 1.52f + CourtY),
            haruMeet + Vector3.up * 1.4f);
        // Side two-shot of Mei + Rina watching the couple (not an empty fence).
        Vector3 friendsMid = (CourtBenchMei + CourtBenchRina) * 0.5f + Vector3.up * 1.35f;
        cs.Shot("Cam E Friends Watch", 17.0f, 5.0f,
            friendsMid + new Vector3(-2.4f, 0.2f, -1.2f), friendsMid + new Vector3(-2.0f, 0.15f, -0.9f), friendsMid);

        cs.Line(0.3f, 3.2f, "Yuki", "Haru... You really waited for me.", "M10_05_Yuki");
        cs.Line(3.8f, 3.8f, "Haru", "Of course. I wanted to give you this myself.", "M10_06_Haru");
        cs.Line(8.8f, 3.6f, "Haru", "Rina helped me pick it. She's my cousin — not my girlfriend.", "M10_07_Haru");
        cs.Line(12.6f, 3.2f, "Yuki", "I thought... I was so wrong. I'm sorry.", "M10_08_Yuki");
        cs.Line(16.0f, 2.6f, "Haru", "It's okay. I like YOU, Yuki. Only you.", "M10_09_Haru");
        cs.Line(18.8f, 1.6f, "Mei", "Finally~!", "M10_10_Mei");
        cs.Line(20.4f, 1.5f, "Rina", "Told you. Sunset was perfect.", "M10_11_Rina");

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

        Transform level = lh.transform.Find("Level_9");
        if (level == null)
        {
            var go = new GameObject("Level_9");
            go.transform.SetParent(lh.transform, false);
            go.SetActive(false);
            level = go.transform;
            log.AppendLine("created Level_9");
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
            AssetDatabase.CreateFolder("Assets/NewLevels", "Mission10");
    }

    static string OutPath(string name) => System.IO.Path.Combine(System.IO.Path.GetTempPath(), name);

    [MenuItem("Tools/Mission 10/Render Preview Shots")]
    public static void RenderPreview()
    {
        var sb = new StringBuilder();
        try
        {
            var req = System.IO.File.ReadAllLines(OutPath("m10_preview_request.txt"));
            var lh = Object.FindObjectOfType<Level_Handler>(true);
            var am = lh.AnimeSchoolManager;
            var zone = Object.FindObjectOfType<SchoolZoneManager>(true);
            Transform level = lh.transform.Find("Level_9");

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
                string file = OutPath("m10_" + cutName.Replace(" ", "_") + "_" + time.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ".png");
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
        System.IO.File.WriteAllText(OutPath("m10_preview.txt"), sb.ToString());
    }
}
