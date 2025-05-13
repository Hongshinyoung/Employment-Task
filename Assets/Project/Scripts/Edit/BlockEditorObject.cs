using UnityEngine;

public class BlockEditorObject : MonoBehaviour, IEditableObject
{
    public ColorType colorType;
    public string gimmickType;

    private void OnMouseDrag()
    {
        Vector3 newPos = StageEditController.Instance.GetMouseWorldPosition();
        newPos.x = Mathf.Round(newPos.x / 0.79f) * 0.79f;
        newPos.z = Mathf.Round(newPos.z / 0.79f) * 0.79f;
        newPos.y = 0f;
        transform.position = newPos;
    }

    public void UpdateColor(ColorType newColor)
    {
        colorType = newColor;
        GetComponent<Renderer>().material.color = GetColor(newColor); // 예시
    }

    public void UpdateGimmick(string newGimmick)
    {
        gimmickType = newGimmick;
    }

    private Color GetColor(ColorType color) => color switch
    {
        ColorType.Red => Color.red,
        ColorType.Blue => Color.blue,
        _ => Color.white
    };
}
