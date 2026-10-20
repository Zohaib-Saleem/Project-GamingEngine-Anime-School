using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Loads mission vocal clips and retimes cutscene lines, cameras, and movers
/// so each line lasts as long as its audio.
/// </summary>
public static class MissionVocals
{
    const string Folder = "Assets/Vocals/";
    const float Pad = 0.18f;
    const float Gap = 0.12f;
    const float Tail = 0.55f;

    public static AudioClip Load(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        string[] exts = { ".mp3", ".wav", ".ogg" };
        for (int i = 0; i < exts.Length; i++)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + id + exts[i]);
            if (clip != null)
                return clip;
        }
        return null;
    }

    public static void Bind(CutsceneDialogue.Line line, string voiceId, StringBuilder log)
    {
        if (line == null || string.IsNullOrEmpty(voiceId))
            return;
        AudioClip clip = Load(voiceId);
        if (clip == null)
        {
            if (log != null)
                log.AppendLine("MISSING VOICE " + voiceId);
            return;
        }
        line.voice = clip;
        if (clip.length > 0.08f)
            line.duration = clip.length + Pad;
        if (log != null)
            log.AppendLine("voice " + voiceId + " " + clip.length.ToString("0.00") + "s");
    }

    public static void BindClue(ClueTaskPoint.Line line, string voiceId, StringBuilder log)
    {
        if (line == null || string.IsNullOrEmpty(voiceId))
            return;
        AudioClip clip = Load(voiceId);
        if (clip == null)
        {
            if (log != null)
                log.AppendLine("MISSING VOICE " + voiceId);
            return;
        }
        line.voice = clip;
        if (log != null)
            log.AppendLine("voice " + voiceId + " " + clip.length.ToString("0.00") + "s");
    }

    public static void PackAndRemap(List<CutsceneDialogue.Line> lines, TimelineAsset timeline, PlayableDirector director, Transform root, StringBuilder log)
    {
        if (lines == null || lines.Count == 0 || timeline == null)
            return;

        float[] oldStarts = new float[lines.Count];
        for (int i = 0; i < lines.Count; i++)
            oldStarts[i] = lines[i].start;

        float oldDur = (float)timeline.fixedDuration;
        float prevEnd = 0f;
        for (int i = 0; i < lines.Count; i++)
        {
            float packed = i == 0 ? lines[i].start : prevEnd + Gap;
            lines[i].start = Mathf.Max(lines[i].start, packed);
            prevEnd = lines[i].start + Mathf.Max(0.4f, lines[i].duration);
        }

        float newDur = prevEnd + Tail;
        if (newDur < 2f)
            newDur = oldDur;

        List<float> oldKeys = new List<float> { 0f };
        List<float> newKeys = new List<float> { 0f };
        for (int i = 0; i < lines.Count; i++)
        {
            if (oldStarts[i] > oldKeys[oldKeys.Count - 1] + 0.001f)
            {
                oldKeys.Add(oldStarts[i]);
                newKeys.Add(lines[i].start);
            }
        }
        oldKeys.Add(Mathf.Max(oldDur, oldKeys[oldKeys.Count - 1] + 0.05f));
        newKeys.Add(newDur);

        timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
        timeline.fixedDuration = newDur;

        List<TimelineClip> clips = new List<TimelineClip>();
        List<float> clipOldStart = new List<float>();
        List<float> clipOldEnd = new List<float>();
        foreach (TrackAsset track in timeline.GetOutputTracks())
        {
            foreach (TimelineClip clip in track.GetClips())
            {
                clips.Add(clip);
                clipOldStart.Add((float)clip.start);
                clipOldEnd.Add((float)(clip.start + clip.duration));
            }
        }
        for (int i = 0; i < clips.Count; i++)
            clips[i].duration = 0.05;
        for (int i = 0; i < clips.Count; i++)
        {
            clips[i].start = Remap(clipOldStart[i], oldKeys, newKeys);
            clips[i].duration = Mathf.Max(0.05f, Remap(clipOldEnd[i], oldKeys, newKeys) - (float)clips[i].start);
        }

        if (root != null)
        {
            CutsceneMover[] movers = root.GetComponentsInChildren<CutsceneMover>(true);
            for (int m = 0; m < movers.Length; m++)
            {
                if (movers[m] == null || movers[m].times == null)
                    continue;
                for (int t = 0; t < movers[m].times.Length; t++)
                    movers[m].times[t] = Remap(movers[m].times[t], oldKeys, newKeys);
            }
        }

        if (director != null)
            director.extrapolationMode = DirectorWrapMode.None;

        if (log != null)
            log.AppendLine("vocals packed " + oldDur.ToString("0.00") + " -> " + newDur.ToString("0.00"));
    }

    static float Remap(float t, List<float> oldKeys, List<float> newKeys)
    {
        if (t <= oldKeys[0])
            return newKeys[0] + (t - oldKeys[0]);
        for (int i = 0; i < oldKeys.Count - 1; i++)
        {
            if (t <= oldKeys[i + 1] || i == oldKeys.Count - 2)
            {
                float span = oldKeys[i + 1] - oldKeys[i];
                float u = span > 0.0001f ? Mathf.Clamp01((t - oldKeys[i]) / span) : 0f;
                return newKeys[i] + u * (newKeys[i + 1] - newKeys[i]);
            }
        }
        return newKeys[newKeys.Count - 1];
    }
}
