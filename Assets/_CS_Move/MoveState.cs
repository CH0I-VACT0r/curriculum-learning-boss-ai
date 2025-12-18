using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MoveState : MonoBehaviour
{
    public Rigidbody rb;
    private Animator animator;
    private AttackState attackState;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 180f;

    [HideInInspector] public float pendingMoveXInput = 0f;
    [HideInInspector] public float pendingForwardInput = 0f;
    [HideInInspector] public float pendingTurnInput = 0f;

    [HideInInspector] public Transform target;
    public Vector3 CurrentVelocity => rb.velocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.drag = 1f;
        rb.angularDrag = 1f;

        animator = GetComponent<Animator>();
        attackState = GetComponent<AttackState>();
    }

    private void FixedUpdate()
    {
        MovePhysics(pendingMoveXInput, pendingForwardInput, pendingTurnInput);

        if (attackState != null && !attackState.IsAttacking)
        {
            if (Mathf.Abs(pendingTurnInput) > 0.01f)
            {
                Quaternion targetRotation = transform.rotation * Quaternion.Euler(0, pendingTurnInput * rotationSpeed * Time.fixedDeltaTime, 0);
                rb.MoveRotation(targetRotation);
            }
        }
    }

    private void Update()
    {
        UpdateAnimator();
    }

    // 이동 처리
    private void MovePhysics(float moveX, float forward, float turn)
    {
        //Quaternion deltaRotation = Quaternion.Euler(0f, turn * rotationSpeed * Time.fixedDeltaTime, 0f);
        //rb.MoveRotation(rb.rotation * deltaRotation);

        Vector3 inputDirection = new Vector3(moveX, 0, forward).normalized;
        Vector3 worldMoveDirection = transform.TransformDirection(inputDirection);
        Vector3 targetVelocity = worldMoveDirection * moveSpeed;
        Vector3 force = (targetVelocity - rb.velocity);
        rb.AddForce(force, ForceMode.VelocityChange);
    }

    // 애니메이터 갱신
    private void UpdateAnimator()
    {
        if (animator == null) return;

        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);

        animator.SetFloat("MoveX", localVelocity.x / moveSpeed);
        animator.SetFloat("MoveZ", localVelocity.z / moveSpeed);
        animator.SetFloat("Turn", Mathf.Clamp(pendingTurnInput, -1f, 1f));
        animator.SetFloat("Speed", new Vector2(localVelocity.x, localVelocity.z).magnitude / moveSpeed);
    }

    public void ResetVelocity()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}