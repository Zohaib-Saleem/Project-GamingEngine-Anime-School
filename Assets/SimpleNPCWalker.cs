using UnityEngine;

public class SimpleNPCWalker : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float walkSpeed = 1.2f;
    public float waitTime = 1.5f;
    public Animator animator;
    public string speedParam = "Speed";
    public bool lookAtMoveDirection = true;

    Vector3 start;
    Vector3 end;
    bool toB = true;
    float wait;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        start = pointA != null ? pointA.position : transform.position;
        end = pointB != null ? pointB.position : transform.position + transform.forward * 4f;
        transform.position = start;
    }

    private void Update()
    {
        if (wait > 0f)
        {
            wait -= Time.deltaTime;
            SetSpeed(0f);
            return;
        }

        Vector3 target = toB ? end : start;
        Vector3 pos = transform.position;
        pos.y = target.y;
        Vector3 next = Vector3.MoveTowards(pos, target, walkSpeed * Time.deltaTime);

        Vector3 delta = next - transform.position;
        delta.y = 0f;
        if (lookAtMoveDirection && delta.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(delta);

        transform.position = new Vector3(next.x, transform.position.y, next.z);
        SetSpeed(1f);

        Vector3 flat = transform.position;
        flat.y = target.y;
        if ((flat - target).sqrMagnitude < 0.04f)
        {
            toB = !toB;
            wait = waitTime;
            SetSpeed(0f);
        }
    }

    void SetSpeed(float value)
    {
        if (animator != null && !string.IsNullOrEmpty(speedParam))
            animator.SetFloat(speedParam, value);
    }
}
