using System;
using UnityEngine;

public class BlockDragController : MonoBehaviour, IDragStateProvider
{
    [SerializeField] private BlockDragHandler handler;
    public BlockDragHandler Handler => handler;
    private Camera mainCamera;
    private Vector3 offset;
    private float zDistanceToCamera;
    private bool isDragging;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!handler.enabled) return;

        if (isDragging)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            handler.SetTargetPosition(mouseWorldPos + offset);
        }
    }
    
    private void OnMouseDown()
    {
        if (!handler.Enabled) return;

        isDragging = true;
        zDistanceToCamera = Vector3.Distance(transform.position, mainCamera.transform.position);
        offset = transform.position - GetMouseWorldPosition();
        
        handler.OnDragStart();
    }

    private void OnMouseUp()
    {
        if(!handler.enabled) return;
        
        isDragging = false;
        handler.OnDragEnd();
    }
    
    public bool IsDragging() => isDragging;

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = zDistanceToCamera;
        return mainCamera.ScreenToWorldPoint(mouseScreenPosition);
    }
}
