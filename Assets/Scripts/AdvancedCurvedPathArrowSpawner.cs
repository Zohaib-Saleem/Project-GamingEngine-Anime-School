using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class AdvancedCurvedPathArrowSpawner : MonoBehaviour
{
    [Header("Waypoints (Minimum 4 for proper curve)")]
    public Transform[] waypoints;

    [Header("Arrow Settings")]
    public GameObject arrowPrefab;
    public float spacing = 2f; // Distance between arrows on the path
   
    public bool fadeArrows = true; // Enable fading of arrows at a distance

    [Header("Arrow Appearance Settings")]
    public float fadeStartDistance = 10f; // Distance where fading begins
    public float fadeEndDistance = 20f;
    public float DesiredView;// Distance where fading completes
    [Header("Curve Settings")]
    [Range(0f, 1f)]
    public float curveTension = 0.5f;
    private GameObject[] spawnedArrows;

    void Start()
    {
        if (waypoints.Length < 2)
        {
            Debug.LogWarning("At least 2 waypoints are required.");
            return;
        }

        StartCoroutine(SpawnCurvedArrowsGradually());
    }

    void Update()
    {
        if (Camera.main != null)
        {
            Vector3 cameraPos = Camera.main.transform.position;
            if (spawnedArrows == null) return;


            foreach (var arrow in spawnedArrows)
            {
                if (arrow == null) continue;
                float distance = Vector3.Distance(cameraPos, arrow.transform.position);
                bool shouldBeActive = distance <= fadeEndDistance + DesiredView;
                if (arrow.activeSelf != shouldBeActive)
                {
                    arrow.SetActive(shouldBeActive);
                }
                if (arrow != null)
                {
                    UpdateArrowAppearance(arrow.transform.GetChild(0).gameObject);
                    //UpdateArrowAppearance(arrow.transform.GetChild(1).gameObject);
                }
            }
        }
        
    }

    //void SpawnCurvedArrows()
    //{
    //    // Extrapolate points for full curve coverage
    //    List<Vector3> controlPoints = new List<Vector3>();

    //    Vector3 firstDir = (waypoints[1].position - waypoints[0].position).normalized;
    //    Vector3 lastDir = (waypoints[waypoints.Length - 1].position - waypoints[waypoints.Length - 2].position).normalized;

    //    Vector3 preStart = waypoints[0].position - firstDir * spacing;
    //    Vector3 postEnd = waypoints[waypoints.Length - 1].position + lastDir * spacing;

    //    controlPoints.Add(preStart);
    //    controlPoints.AddRange(waypoints.Select(wp => wp.position));
    //    controlPoints.Add(postEnd);

    //    List<GameObject> arrows = new List<GameObject>();

    //    for (int i = 0; i < controlPoints.Count - 3; i++)
    //    {
    //        Vector3 p0 = controlPoints[i];
    //        Vector3 p1 = controlPoints[i + 1];
    //        Vector3 p2 = controlPoints[i + 2];
    //        Vector3 p3 = controlPoints[i + 3];

    //        float tStep = 0.01f; // sampling resolution
    //        float distanceAccumulator = 0f;
    //        Vector3 lastPos = GetCatmullRomPosition(0f, p0, p1, p2, p3);

    //        for (float t = tStep; t <= 1f; t += tStep)
    //        {
    //            Vector3 currentPos = GetCatmullRomPosition(t, p0, p1, p2, p3);
    //            float segmentDistance = Vector3.Distance(lastPos, currentPos);
    //            distanceAccumulator += segmentDistance;

    //            if (distanceAccumulator >= spacing)
    //            {
    //                Vector3 forward = GetCatmullRomTangent(t, p0, p1, p2, p3);
    //                Quaternion rotation = Quaternion.LookRotation(forward);

    //                GameObject arrow = Instantiate(arrowPrefab, currentPos + Vector3.up * 0.1f, rotation, transform);
    //                arrows.Add(arrow);

    //                distanceAccumulator = 0f;
    //                lastPos = currentPos; // reset from new arrow
    //            }
    //            else
    //            {
    //                lastPos = currentPos;
    //            }
    //        }
    //    }

    //    spawnedArrows = arrows.ToArray();
    //}
    IEnumerator SpawnCurvedArrowsGradually()
    {
        List<Vector3> controlPoints = new List<Vector3>();

        Vector3 firstDir = (waypoints[1].position - waypoints[0].position).normalized;
        Vector3 lastDir = (waypoints[waypoints.Length - 1].position - waypoints[waypoints.Length - 2].position).normalized;

        Vector3 preStart = waypoints[0].position - firstDir * spacing;
        Vector3 postEnd = waypoints[waypoints.Length - 1].position + lastDir * spacing;

        controlPoints.Add(preStart);
        controlPoints.AddRange(waypoints.Select(wp => wp.position));
        controlPoints.Add(postEnd);

        List<GameObject> arrows = new List<GameObject>();

        for (int i = 0; i < controlPoints.Count - 3; i++)
        {
            Vector3 p0 = controlPoints[i];
            Vector3 p1 = controlPoints[i + 1];
            Vector3 p2 = controlPoints[i + 2];
            Vector3 p3 = controlPoints[i + 3];

            float tStep = 0.01f;
            float distanceAccumulator = 0f;
            Vector3 lastPos = GetCatmullRomPosition(0f, p0, p1, p2, p3);

            for (float t = tStep; t <= 1f; t += tStep)
            {
                Vector3 currentPos = GetCatmullRomPosition(t, p0, p1, p2, p3);
                float segmentDistance = Vector3.Distance(lastPos, currentPos);
                distanceAccumulator += segmentDistance;

                if (distanceAccumulator >= spacing)
                {
                    Vector3 forward = GetCatmullRomTangent(t, p0, p1, p2, p3);
                    Quaternion rotation = Quaternion.LookRotation(forward);

                    GameObject arrow = Instantiate(arrowPrefab, currentPos + Vector3.up * 0.1f, rotation, transform);
                    arrows.Add(arrow);

                    distanceAccumulator = 0f;
                    lastPos = currentPos;

                    yield return null; // Spread out instantiation across frames
                }
                else
                {
                    lastPos = currentPos;
                }
            }
        }

        spawnedArrows = arrows.ToArray();
    }


    //void UpdateArrowAppearance(GameObject arrow)
    //{
    //    Vector3 position = arrow.transform.position;
    //    float distanceFromCamera = Vector3.Distance(Camera.main.transform.position, position);

    //    if (scaleArrowsWithDistance)
    //    {
    //        float scale = Mathf.Lerp(1f, 0.5f, distanceFromCamera / 20f);
    //        arrow.transform.localScale = Vector3.one * scale;
    //    }

    //    if (fadeArrows)
    //    {
    //        Renderer arrowRenderer = arrow.GetComponent<Renderer>();
    //        if (arrowRenderer != null)
    //        {
    //            float fadeFactor = Mathf.InverseLerp(fadeStartDistance, fadeEndDistance, distanceFromCamera);
    //            Color color = arrowRenderer.material.color;
    //            color.a = Mathf.Lerp(1f, 0f, fadeFactor);
    //            arrowRenderer.material.color = color;
    //        }
    //    }
    //}

    void UpdateArrowAppearance(GameObject arrow)
    {
        Vector3 position = arrow.transform.position;
        float distanceFromCamera = Vector3.Distance(Camera.main.transform.position, position);

        

        if (fadeArrows)
        {
            Renderer arrowRenderer = arrow.GetComponent<Renderer>();
            if (arrowRenderer != null && arrowRenderer.material.HasProperty("_Color"))
            {
                float fadeFactor = Mathf.InverseLerp(fadeStartDistance, fadeEndDistance, distanceFromCamera);
                Color color = arrowRenderer.material.color;
                color.a = Mathf.Lerp(1f, 0f, fadeFactor);
                arrowRenderer.material.color = color;

                // Optional: Force transparency mode if not working
                // arrowRenderer.material.SetFloat("_Mode", 3); // Uncomment if needed
            }
        }
    }



    Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float tension = curveTension; // curveTension should be between 0 and 1

        // Adjust tangents using tension
        Vector3 m0 = (1 - tension) * (p2 - p0) * 0.5f;
        Vector3 m1 = (1 - tension) * (p3 - p1) * 0.5f;

        float t2 = t * t;
        float t3 = t2 * t;

        return (2 * t3 - 3 * t2 + 1) * p1 +
               (t3 - 2 * t2 + t) * m0 +
               (-2 * t3 + 3 * t2) * p2 +
               (t3 - t2) * m1;
    }
    Vector3 GetCatmullRomTangent(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float tension = curveTension;

        Vector3 m0 = (1 - tension) * (p2 - p0) * 0.5f;
        Vector3 m1 = (1 - tension) * (p3 - p1) * 0.5f;

        float t2 = t * t;

        return ((6 * t2 - 6 * t) * p1 +
                (3 * t2 - 4 * t + 1) * m0 +
                (-6 * t2 + 6 * t) * p2 +
                (3 * t2 - 2 * t) * m1).normalized;
    }
    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = Color.yellow;
        foreach (var wp in waypoints)
        {
            if (wp != null)
                Gizmos.DrawSphere(wp.position, 0.3f);
        }

        List<Vector3> controlPoints = new List<Vector3>();

        if (waypoints.Length >= 2)
        {
            Vector3 firstDir = (waypoints[1].position - waypoints[0].position).normalized;
            Vector3 lastDir = (waypoints[waypoints.Length - 1].position - waypoints[waypoints.Length - 2].position).normalized;

            Vector3 preStart = waypoints[0].position - firstDir * spacing;
            Vector3 postEnd = waypoints[waypoints.Length - 1].position + lastDir * spacing;

            controlPoints.Add(preStart);
            controlPoints.AddRange(waypoints.Select(wp => wp.position));
            controlPoints.Add(postEnd);

            Gizmos.color = Color.cyan;
            Vector3 prevPos = GetCatmullRomPosition(0f, controlPoints[0], controlPoints[1], controlPoints[2], controlPoints[3]);

            for (int i = 0; i < controlPoints.Count - 3; i++)
            {
                for (float t = 0; t <= 1f; t += 0.02f)
                {
                    Vector3 pos = GetCatmullRomPosition(t, controlPoints[i], controlPoints[i + 1], controlPoints[i + 2], controlPoints[i + 3]);
                    Gizmos.DrawLine(prevPos, pos);
                    prevPos = pos;
                }
            }
        }
    }

}
