using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finds hand props the designer parented under a character (Water Bottle, Gift, Book).
/// Cutscenes and Level_Handler only enable/disable these objects.
/// </summary>
public static class HandProp
{
    public static readonly string[] AllNames = { "Water Bottle", "Gift", "Gift Box", "Book", "Notebook" };

    public static GameObject Find(Transform root, params string[] names)
    {
        List<GameObject> all = FindAll(root, names);
        return all.Count > 0 ? all[0] : null;
    }

    public static List<GameObject> FindAll(Transform root, params string[] names)
    {
        var list = new List<GameObject>();
        if (root == null || names == null || names.Length == 0)
            return list;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
                continue;
            for (int n = 0; n < names.Length; n++)
            {
                if (string.IsNullOrEmpty(names[n]) || all[i].name != names[n])
                    continue;
                if (IsUnderHand(all[i]))
                    list.Add(all[i].gameObject);
                break;
            }
        }
        return list;
    }

    public static void SetNamed(Transform root, bool visible, params string[] names)
    {
        List<GameObject> all = FindAll(root, names);
        for (int i = 0; i < all.Count; i++)
            all[i].SetActive(visible);
    }

    public static GameObject FindInScene(params string[] names)
    {
        Transform[] all = Object.FindObjectsOfType<Transform>(true);
        for (int n = 0; n < names.Length; n++)
        {
            if (string.IsNullOrEmpty(names[n]))
                continue;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == names[n] && IsUnderHand(all[i]))
                    return all[i].gameObject;
            }
        }
        return null;
    }

    public static bool IsUnderHand(Transform t)
    {
        Transform p = t != null ? t.parent : null;
        while (p != null)
        {
            string n = p.name.Replace(" ", "").ToLowerInvariant();
            if (n.Contains("righthand") || n.Contains("lefthand") || n.Contains("hand_r") || n.Contains("hand_l"))
                return true;
            p = p.parent;
        }
        return false;
    }

    public static void HideAll(Transform root)
    {
        HideNamed(root, AllNames);
    }

    public static void HideNamed(Transform root, params string[] names)
    {
        if (root == null || names == null)
            return;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
                continue;
            for (int n = 0; n < names.Length; n++)
            {
                if (all[i].name == names[n])
                {
                    all[i].gameObject.SetActive(false);
                    break;
                }
            }
        }
    }
}
