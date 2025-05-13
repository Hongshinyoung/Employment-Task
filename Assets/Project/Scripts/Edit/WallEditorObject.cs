using UnityEngine;

public class WallEditorObject : MonoBehaviour, IEditableObject
{
    public ObjectPropertiesEnum.WallDirection wallDirection;
    public int length;
    public ColorType colorType;
    public WallGimmickType gimmickType;

    public void UpdateColor(ColorType newColor) { colorType = newColor; UpdateVisual(); }
    public void UpdateGimmick(string gimmick) { gimmickType = (WallGimmickType)System.Enum.Parse(typeof(WallGimmickType), gimmick); UpdateVisual(); }

    public void UpdateVisual()
    {
        // Renderer 색상 및 방향 반영 등
    }
}