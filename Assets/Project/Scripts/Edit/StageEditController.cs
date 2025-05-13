using UnityEngine;

public class StageEditController : MonoBehaviour
{
    public static StageEditController Instance;

    public StageData currentStageData;
    public StageObjectManager objectManager;
    public EditorUIController uiController;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        objectManager.HandlePlacement();
        objectManager.HandleSelection();
    }

    public Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit)) return hit.point;
        return Vector3.zero;
    }

    public void SaveStage() => StageDataHandler.Save(currentStageData, objectManager.objectsRoot);
    public void LoadStage(StageData data) => objectManager.LoadFromData(data);
}
