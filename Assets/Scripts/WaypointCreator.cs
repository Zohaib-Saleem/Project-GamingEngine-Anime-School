using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[ExecuteAlways]
public class WaypointCreator : MonoBehaviour
{
    [HideInInspector]
    public List<Transform> waypoints = new List<Transform>();

    [Header("Gizmo Settings")]
    public Color waypointColor = Color.yellow;
    public Color lineColor = Color.green;
    public float waypointSize = 0.5f;

    [Header("Editor Placement Mode")]
    public bool clickToPlace = false;

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        // Draw each waypoint and the lines between them
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;

            Gizmos.color = waypointColor;
            Gizmos.DrawSphere(waypoints[i].position, waypointSize);

            if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
            {
                Gizmos.color = lineColor;
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(WaypointCreator))]
public class WaypointCreatorEditor : Editor
{
    private WaypointCreator creator;

    private void OnEnable()
    {
        creator = (WaypointCreator)target;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(10);

        if (GUILayout.Button("➕ Add Waypoint"))
        {
            CreateWaypoint(creator.transform.position);
        }

        if (GUILayout.Button("🧹 Clear All Waypoints"))
        {
            ClearWaypoints();
        }

        EditorGUILayout.HelpBox(
            "Enable 'Click To Place' to create waypoints by left-clicking in the Scene view.\n" +
            "Right-click to move or rotate the camera.",
            MessageType.Info);
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!creator.clickToPlace) return;

        Event e = Event.current;
        if (e.button == 1) return; // allow right-click camera control

        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                CreateWaypoint(hit.point);
            }
            else
            {
                Plane ground = new Plane(Vector3.up, Vector3.zero);
                if (ground.Raycast(ray, out float dist))
                    CreateWaypoint(ray.GetPoint(dist));
            }

            e.Use();
        }
    }

    private void CreateWaypoint(Vector3 position)
    {
        GameObject newPoint = new GameObject("Waypoint " + creator.waypoints.Count);
        newPoint.transform.parent = creator.transform;
        newPoint.transform.position = position;

        creator.waypoints.Add(newPoint.transform);
        EditorUtility.SetDirty(creator);
        Undo.RegisterCreatedObjectUndo(newPoint, "Create Waypoint");
    }

    private void ClearWaypoints()
    {
        for (int i = creator.waypoints.Count - 1; i >= 0; i--)
        {
            if (creator.waypoints[i] != null)
                Undo.DestroyObjectImmediate(creator.waypoints[i].gameObject);
        }
        creator.waypoints.Clear();
        EditorUtility.SetDirty(creator);
    }
}
#endif
