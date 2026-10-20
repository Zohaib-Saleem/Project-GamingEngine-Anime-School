using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Editor helper: locate a designer-placed hand prop, or copy one onto a cutscene actor's right hand.
/// </summary>
public static class HandPropBuild
{
    public static GameObject Player(Level_Handler lh, params string[] names)
    {
        Transform player = lh != null && lh.AnimeSchoolManager != null && lh.AnimeSchoolManager.mainCh != null
            ? lh.AnimeSchoolManager.mainCh.transform
            : null;
        List<GameObject> all = HandProp.FindAll(player, names);
        for (int i = 0; i < all.Count; i++)
            all[i].SetActive(false);
        return all.Count > 0 ? all[0] : null;
    }

    public static GameObject Ensure(Transform dest, StringBuilder log, params string[] names)
    {
        if (dest == null)
            return null;

        GameObject existing = HandProp.Find(dest, names);
        if (existing != null)
        {
            existing.SetActive(false);
            return existing;
        }

        GameObject src = null;
        Level_Handler lh = Object.FindObjectOfType<Level_Handler>(true);
        if (lh != null && lh.AnimeSchoolManager != null && lh.AnimeSchoolManager.mainCh != null)
            src = HandProp.Find(lh.AnimeSchoolManager.mainCh.transform, names);
        if (src == null)
            src = HandProp.FindInScene(names);
        if (src == null)
        {
            if (log != null)
                log.AppendLine("HAND PROP MISSING " + string.Join("|", names) + " on " + dest.name);
            return null;
        }

        Transform hand = CutsceneHandItem.FindHand(dest, true);
        if (hand == null)
            hand = dest;
        GameObject copy = Object.Instantiate(src, hand);
        copy.name = src.name;
        copy.transform.localPosition = src.transform.localPosition;
        copy.transform.localRotation = src.transform.localRotation;
        copy.transform.localScale = src.transform.localScale;
        copy.SetActive(false);
        if (log != null)
            log.AppendLine("HAND PROP COPY " + copy.name + " -> " + dest.name + "/" + hand.name);
        return copy;
    }

    public static GameObject[] EnsureAll(Transform dest, StringBuilder log, params string[] names)
    {
        List<GameObject> found = HandProp.FindAll(dest, names);
        if (found.Count > 0)
        {
            for (int i = 0; i < found.Count; i++)
                found[i].SetActive(false);
            return found.ToArray();
        }

        GameObject one = Ensure(dest, log, names);
        return one != null ? new[] { one } : System.Array.Empty<GameObject>();
    }
}
