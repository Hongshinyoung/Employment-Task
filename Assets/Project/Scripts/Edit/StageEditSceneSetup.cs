using UnityEngine;
using UnityEngine.UI;

public class StageEditSceneSetup : MonoBehaviour
{
    public StageEditController editController;
    public StageObjectManager objectManager;
    public EditorUIController uiController;

    public Button boardBlockButton;
    public Button wallButton;
    public Button playingBlockButton;
    public Button saveButton;
    public Button loadButton;

    private void Start()
    {
        boardBlockButton.onClick.AddListener(() => objectManager.SetMode(StageObjectManager.EditMode.BoardBlock));
        wallButton.onClick.AddListener(() => objectManager.SetMode(StageObjectManager.EditMode.Wall));
        playingBlockButton.onClick.AddListener(() => objectManager.SetMode(StageObjectManager.EditMode.PlayingBlock));

        saveButton.onClick.AddListener(() => editController.SaveStage());
        loadButton.onClick.AddListener(() => editController.LoadStage(editController.currentStageData));
    }
}