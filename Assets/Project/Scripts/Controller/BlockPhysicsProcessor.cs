using UnityEngine;

public class BlockPhysicsProcessor : MonoBehaviour
{
    [SerializeField] private BlockDragController dragController;

    private Rigidbody rb;
    private Vector3 targetPosition;

    private Vector3 lastCollisionNormal;
    private bool isColliding;
    private float lastCollisionTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!dragController.IsDragging()) return;

        Vector3 moveVector = targetPosition - transform.position;

        if (isColliding && Vector3.Distance(transform.position, targetPosition) > 0.5f)
        {
            if (Vector3.Dot(moveVector.normalized, lastCollisionNormal) > 0.1f)
                ResetCollisionState();
        }

        Vector3 velocity = isColliding
            ? Vector3.ProjectOnPlane(moveVector, lastCollisionNormal) * Constants.MoveSpeed
            : moveVector * Constants.FollowSpeed;

        if (velocity.magnitude > Constants.MaxSpeed)
            velocity = velocity.normalized * Constants.MaxSpeed;

        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, velocity, Time.fixedDeltaTime * 10f);

        if (isColliding && Time.time - lastCollisionTime > Constants.CollisionResetTime)
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
        if (!dragController.IsDragging()) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            Vector3 wallPosition = collision.transform.position;
            int wallX = Mathf.RoundToInt((wallPosition.x - Constants.BlockDistance * 0.5f) / Constants.BlockDistance);
            int wallY = Mathf.RoundToInt((wallPosition.z - Constants.BlockDistance * 0.5f) / Constants.BlockDistance);

            if (BoardController.Instance.WallCoorInfoDic.TryGetValue((wallX, wallY), out var wallInfo))
            {
                foreach (var keyValue in wallInfo)
                {
                    ColorType wallColor = keyValue.Key.Item2;
                    if (dragController.Handler != null && dragController.Handler.blocks != null)
                    {
                        foreach (var block in dragController.Handler.blocks)
                        {
                            if (block.colorType == wallColor)
                            {
                                BoardController.Instance.DestroyBlockGroup(block);
                                return;
                            }
                        }
                    }
                }
            }
        }
    }

    private void ResetCollisionState()
    {
        isColliding = false;
        lastCollisionNormal = Vector3.zero;
    }
}