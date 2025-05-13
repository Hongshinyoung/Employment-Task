using System.Collections.Generic;
using UnityEngine;

public class PlayingBlockEditorObject : MonoBehaviour, IEditableObject
{
    public int uniqueIndex;
    public ColorType colorType;
    public List<ShapeData> shapes = new();
    public List<GimmickData> gimmicks = new();

    public void UpdateColor(ColorType newColor) { colorType = newColor; UpdateVisual(); }
    public void UpdateGimmick(string gimmick) { /* Ignore or Extend */ }

    public void UpdateVisual()
    {
        // 색상 반영, 자식 Shape 배치 등
    }
}