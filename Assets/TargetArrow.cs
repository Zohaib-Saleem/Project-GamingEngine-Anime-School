using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetArrow : MonoBehaviour
{
    [Header("References")]
    public Transform target;

    [Header("Settings")]
    public bool followTarget = true;
    public float rotateSpeed = 10f;

    void LateUpdate()
    {
        if (!followTarget || target == null)
            return;

        // Direction to target (Y only)
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotateSpeed * Time.deltaTime
        );
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void StartFollowing()
    {
        followTarget = true;
    }

    public void StopFollowing()
    {
        followTarget = false;
    }
}