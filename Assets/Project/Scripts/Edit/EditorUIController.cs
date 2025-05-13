using TMPro;
using UnityEngine;

public class EditorUIController : MonoBehaviour
{
    public GameObject propertyPanel;
    public TMP_Dropdown colorDropdown;
    public TMP_InputField gimmickInput;

    private IEditableObject currentTarget;

    public void ShowProperties(IEditableObject target)
    {
        currentTarget = target;
        propertyPanel.SetActive(true);

        if (target is BlockEditorObject block)
        {
            colorDropdown.value = (int)block.colorType;
            gimmickInput.text = block.gimmickType;
        }
    }

    public void ApplyColor(int index)
    {
        if (currentTarget != null) currentTarget.UpdateColor((ColorType)index);
    }

    public void ApplyGimmick(string gimmick)
    {
        if (currentTarget != null) currentTarget.UpdateGimmick(gimmick);
    }
}
