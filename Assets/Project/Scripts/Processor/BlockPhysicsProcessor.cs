using UnityEngine;

public class BlockPhysicsProcessor : MonoBehaviour
{
    [SerializeField] private BlockDragController controller;

    private Rigidbody rb;
    private Vector3 targetPosition;
    
    private Vector3 lastCollisionNormal;
    private bool isColliding;
    private float lastCollisionTime;

    private readonly float collisionResetTime = 0.1f;
    private readonly float moveSpeed = 25f;
    private readonly float followSpeed = 30f;
    private readonly float maxSpeed = 20f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!controller.IsDragging()) return;

        Vector3 moveVector = targetPosition - transform.position;

        if (isColliding && Vector3.Distance(transform.position, targetPosition) > 0.5f)
        {
            if (Vector3.Dot(moveVector.normalized, lastCollisionNormal) > 0.1f)
                ResetCollisionState();
        }

        Vector3 velocity = isColliding
            ? Vector3.ProjectOnPlane(moveVector, lastCollisionNormal) * moveSpeed
            : moveVector * followSpeed;

        if (velocity.magnitude > maxSpeed)
            velocity = velocity.normalized * maxSpeed;

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, velocity, Time.fixedDeltaTime * 10f);

        if (isColliding && Time.time - lastCollisionTime > collisionResetTime)
            ResetCollisionState();
    }

    public void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
    }

    public void ActivatePhysics() => rb.isKinematic = false;
    public void DeactivatePhysics()
    {
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    private void OnCollisionEnter(Collision collision) => HandleCollision(collision);
    private void OnCollisionStay(Collision collision) => HandleCollision(collision);

    private void HandleCollision(Collision collision)
    {
        if (!controller.IsDragging()) return;

        if (collision.contactCount > 0 && collision.gameObject.layer != LayerMask.NameToLayer("Board"))
        {
            Vector3 normal = collision.contacts[0].normal;
            if (Vector3.Dot(normal, Vector3.up) < 0.8f)
            {
                isColliding = true;
                lastCollisionNormal = normal;
                lastCollisionTime = Time.time;
            }
        }
    }

    private void ResetCollisionState()
    {
        isColliding = false;
        lastCollisionNormal = Vector3.zero;
    }
}
