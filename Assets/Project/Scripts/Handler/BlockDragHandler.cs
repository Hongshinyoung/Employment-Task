using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BlockDragHandler : MonoBehaviour
{
    [SerializeField] private BlockDragController controller;
    [SerializeField] private BlockPhysicsProcessor physicsProcessor;

    public int horizon = 1;
    public int vertical = 1;
    public int uniqueIndex;
    public List<ObjectPropertiesEnum.BlockGimmickType> gimmickType;
    public List<BlockObject> blocks = new List<BlockObject>();
    public List<Vector2> blockOffsets = new List<Vector2>();
    public bool Enabled = true;

    private Outline outline;
    public Collider col { get; set; }

    private void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        outline = gameObject.AddComponent<Outline>();
        outline.OutlineMode = Outline.Mode.OutlineAll;
        outline.OutlineColor = Color.yellow;
        outline.OutlineWidth = 2f;
        outline.enabled = false;
    }

    private void Update()
    {
        if (controller.IsDragging())
        {
            SetBlockPosition(false);
        }
    }

    public void OnDragStart()
    {
        outline.enabled = true;
        physicsProcessor.ActivatePhysics();
    }

    public void OnDragEnd()
    {
        outline.enabled = false;
        physicsProcessor.DeactivatePhysics();
        SetBlockPosition(true);
    }

    public void SetTargetPosition(Vector3 position)
    {
        physicsProcessor.SetTargetPosition(position);
    }

    public Vector3 GetCenterX()
    {
        if (blocks.Count == 0) return Vector3.zero;

        float minX = float.MaxValue;
        float maxX = float.MinValue;

        foreach (var block in blocks)
        {
            float blockX = block.transform.position.x;
            if (blockX < minX) minX = blockX;
            if (blockX > maxX) maxX = blockX;
        }

        return new Vector3((minX + maxX) / 2f, transform.position.y, 0);
    }

    public Vector3 GetCenterZ()
    {
        if (blocks.Count == 0) return Vector3.zero;

        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        foreach (var block in blocks)
        {
            float blockZ = block.transform.position.z;
            if (blockZ < minZ) minZ = blockZ;
            if (blockZ > maxZ) maxZ = blockZ;
        }

        return new Vector3(transform.position.x, transform.position.y, (minZ + maxZ) / 2f);
    }
    
    private void SetBlockPosition(bool snapToGrid)
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit))
        {
            Vector3 boardPos = hit.transform.position;
            Vector2 centerPos = new Vector2(
                Mathf.Round(boardPos.x / 0.79f),
                Mathf.Round(boardPos.z / 0.79f)
            );

            if (snapToGrid)
            {
                transform.position = new Vector3(centerPos.x * 0.79f, transform.position.y, centerPos.y * 0.79f);
            }

            if (hit.collider.TryGetComponent(out BoardBlockObject boardBlock))
            {
                foreach (var block in blocks)
                {
                    block.SetCoordinate(centerPos);
                    boardBlock.CheckAdjacentBlock(block, transform.position);
                    block.CheckBelowBoardBlock(transform.position);
                }
            }
        }
    }

    public void DestroyMove(Vector3 pos, ParticleSystem particle)
    {
        ClearPreboardBlockObjects();

        transform.DOMove(pos, 1f).SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                Destroy(particle.gameObject);
                Destroy(gameObject);
            });
    }

    private void ClearPreboardBlockObjects()
    {
        foreach (var b in blocks)
        {
            if (b.preBoardBlockObject != null)
                b.preBoardBlockObject.playingBlock = null;
        }
    }

    public void ReleaseInput()
    {
        if (col != null) col.enabled = false;
        physicsProcessor.DeactivatePhysics();
        outline.enabled = false;
    }

    private void OnDisable() => transform.DOKill(true);
    private void OnDestroy() => transform.DOKill(true);
}
