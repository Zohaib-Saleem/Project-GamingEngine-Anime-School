using UnityEngine;

/// <summary>
/// Keeps a cutscene prop (cup, gift, notebook) glued to a character's hand.
/// Runs after Timeline/Animator so the prop follows the posed bone.
/// </summary>
[DefaultExecutionOrder(32000)]
public class CutsceneHandItem : MonoBehaviour
{
    public Transform target;
    public bool rightHand = true;
    public bool keepUpright = true;
    public Vector3 localPos = new Vector3(0.03f, 0.04f, 0.06f);
    public Vector3 localEuler = Vector3.zero;

    Transform hand;

    public static CutsceneHandItem Attach(GameObject prop, Transform actorRoot, bool rightHand)
    {
        if (prop == null || actorRoot == null)
            return null;
        CutsceneHandItem item = prop.GetComponent<CutsceneHandItem>();
        if (item == null)
            item = prop.AddComponent<CutsceneHandItem>();
        item.target = actorRoot;
        item.rightHand = rightHand;
        item.hand = null;
        return item;
    }

    public static CutsceneHandItem AttachCup(GameObject prop, Transform actorRoot, bool rightHand = true)
    {
        CutsceneHandItem item = Attach(prop, actorRoot, rightHand);
        if (item == null)
            return null;
        item.keepUpright = true;
        item.localPos = new Vector3(0.03f, 0.03f, 0.07f);
        item.localEuler = Vector3.zero;
        return item;
    }

    public static CutsceneHandItem AttachGift(GameObject prop, Transform actorRoot, bool rightHand = true)
    {
        CutsceneHandItem item = Attach(prop, actorRoot, rightHand);
        if (item == null)
            return null;
        item.keepUpright = true;
        item.localPos = new Vector3(0.04f, 0.05f, 0.08f);
        item.localEuler = Vector3.zero;
        return item;
    }

    public static CutsceneHandItem AttachNotebook(GameObject prop, Transform actorRoot, bool rightHand = true)
    {
        CutsceneHandItem item = Attach(prop, actorRoot, rightHand);
        if (item == null)
            return null;
        item.keepUpright = true;
        item.localPos = new Vector3(0.02f, 0.02f, 0.08f);
        item.localEuler = new Vector3(0f, 0f, 90f);
        return item;
    }

    private void OnEnable()
    {
        hand = null;
    }

    private void LateUpdate()
    {
        Snap();
    }

    public void Snap()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
            return;
        if (hand == null || !hand.gameObject.activeInHierarchy)
            hand = FindHand(target, rightHand);
        if (hand == null)
            return;

        transform.position = hand.TransformPoint(localPos);
        if (keepUpright)
        {
            Vector3 fwd = Vector3.ProjectOnPlane(hand.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.001f)
                fwd = Vector3.ProjectOnPlane(hand.right, Vector3.up);
            if (fwd.sqrMagnitude < 0.001f)
                fwd = Vector3.forward;
            transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up) * Quaternion.Euler(localEuler);
        }
        else
        {
            transform.rotation = hand.rotation * Quaternion.Euler(localEuler);
        }
    }

    public static Transform FindHand(Transform root, bool right)
    {
        if (root == null)
            return null;

        Animator[] anims = root.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < anims.Length; i++)
        {
            Animator anim = anims[i];
            if (anim == null)
                continue;
            if (anim.isHuman)
            {
                Transform bone = anim.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
                if (bone != null)
                    return bone;
            }
        }

        string want = right ? "righthand" : "lefthand";
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        Transform named = null;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
                continue;
            string n = all[i].name.Replace(" ", "").ToLowerInvariant();
            if (n != want && n != "mixamorig:" + want)
                continue;
            named = all[i];
            break;
        }
        return named;
    }
}
