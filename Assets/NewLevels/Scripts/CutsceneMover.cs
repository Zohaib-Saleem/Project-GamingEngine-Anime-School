using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Moves this transform through world-space keyframes in sync with a Timeline's time.
/// Used for cutscene actors (walking, with an in-place clip on a child Animator) and
/// camera dollies, so motion stays exact even when the cutscene is skipped.
/// </summary>
public class CutsceneMover : MonoBehaviour
{
    public PlayableDirector director;
    public float[] times;
    public Vector3[] positions;
    public Quaternion[] rotations;
    [Tooltip("Ease in/out between keys instead of linear motion.")]
    public bool smooth = true;
    [Tooltip("Yaw-only keys: lerp degrees on Y so the actor does not spin the long way.")]
    public bool yawDegrees = true;
    [Tooltip("Zero child local Yaw after apply so Timeline root-Q on the body cannot spin the actor.")]
    public bool lockChildLocalYaw = false;
    [Tooltip("Zero child local Position after apply so root-motion locomotion clips cannot skate/sink on top of CutsceneMover.")]
    public bool lockChildLocalPosition = false;
    [Tooltip("If set, this transform stays at target.position + followOffset and looks at target.position + followLookOffset.")]
    public Transform followTarget;
    public Vector3 followOffset;
    public Vector3 followLookOffset = new Vector3(0f, 1.32f, 0f);

    private void OnEnable()
    {
        if (director == null)
            director = GetComponentInParent<PlayableDirector>();
        Apply(director != null ? director.time : 0d);
    }

    private void Update()
    {
        if (director != null)
            Apply(director.time);
    }

    private void LateUpdate()
    {
        // Win over AnimationTrack root curves written the same frame.
        if (director != null)
            Apply(director.time);
    }

    public void Apply(double time)
    {
        if (followTarget != null)
        {
            transform.position = followTarget.position + followOffset;
            Vector3 aim = followTarget.position + followLookOffset;
            Vector3 delta = aim - transform.position;
            if (delta.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(delta);
            LockChildren();
            return;
        }

        if (times == null || times.Length == 0 || positions == null || positions.Length < times.Length)
            return;

        float t = (float)time;
        int last = times.Length - 1;
        int i = 0;
        while (i < last && t > times[i + 1])
            i++;

        float k = 0f;
        int j = i;
        if (t <= times[0])
            j = i = 0;
        else if (i < last)
        {
            j = i + 1;
            float span = times[j] - times[i];
            k = span > 0f ? Mathf.Clamp01((t - times[i]) / span) : 1f;
            if (smooth)
                k = Mathf.SmoothStep(0f, 1f, k);
        }

        transform.position = Vector3.Lerp(positions[i], positions[j], k);
        if (rotations != null && rotations.Length > j)
        {
            if (yawDegrees)
            {
                float y0 = rotations[i].eulerAngles.y;
                float y1 = rotations[j].eulerAngles.y;
                float y = Mathf.LerpAngle(y0, y1, k);
                transform.rotation = Quaternion.Euler(0f, y, 0f);
            }
            else
                transform.rotation = Quaternion.Slerp(rotations[i], rotations[j], k);
        }

        LockChildren();
    }

    void LockChildren()
    {
        if (!lockChildLocalYaw && !lockChildLocalPosition)
            return;
        for (int c = 0; c < transform.childCount; c++)
        {
            var ch = transform.GetChild(c);
            if (lockChildLocalYaw)
            {
                var e = ch.localEulerAngles;
                ch.localRotation = Quaternion.Euler(e.x, 0f, e.z);
            }
            if (lockChildLocalPosition)
                ch.localPosition = Vector3.zero;
        }
    }
}
