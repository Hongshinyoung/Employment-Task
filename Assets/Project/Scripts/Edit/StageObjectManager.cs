using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class StageObjectManager : MonoBehaviour
{
    public enum EditMode { None, BoardBlock, Wall, PlayingBlock }
    public EditMode CurrentMode { get; private set; }
    public GameObject boardBlockPrefab, wallPrefab, playingBlockPrefab;
    public void SetMode(EditMode mode) => CurrentMode = mode;
    public bool IsPlacing => CurrentMode != EditMode.None;
    public Transform objectsRoot;

    public void HandlePlacement()
    {
        if (Input.GetMouseButtonDown(0) && IsPlacing)
        {
            Vector3 placePos = StageEditController.Instance.GetMouseWorldPosition();
            PlaceObjectAt(placePos);
        }
    }

    public void HandleSelection()
    {
        if (Input.GetMouseButtonDown(1)) // 오른쪽 클릭으로 선택
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var obj = hit.collider.GetComponent<IEditableObject>();
                if (obj != null) StageEditController.Instance.uiController.ShowProperties(obj);
            }
        }
    }

    public void PlaceObjectAt(Vector3 pos)
    {
        Vector3 snapped = new Vector3(Mathf.Round(pos.x / 0.79f) * 0.79f, 0, Mathf.Round(pos.z / 0.79f) * 0.79f);
        GameObject prefab = GetPrefabForMode(CurrentMode);

        if (prefab != null) Instantiate(prefab, snapped, Quaternion.identity, transform);
    }

    private GameObject GetPrefabForMode(EditMode mode) => mode switch
    {
        EditMode.BoardBlock => boardBlockPrefab,
        EditMode.Wall => wallPrefab,
        EditMode.PlayingBlock => playingBlockPrefab,
        _ => null
    };

    public List<PlayingBlockData> GetAllObjectsData()
    {
        var result = new List<PlayingBlockData>();

        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out PlayingBlockEditorObject playingBlock))
            {
                Vector2Int center = new Vector2Int(
                    Mathf.RoundToInt(child.position.x / 0.79f),
                    Mathf.RoundToInt(child.position.z / 0.79f)
                );

                var data = new PlayingBlockData
                {
                    center = center,
                    uniqueIndex = playingBlock.uniqueIndex,
                    colorType = playingBlock.colorType,
                    gimmicks = playingBlock.gimmicks,
                    shapes = new List<ShapeData>()
                };

                foreach (Transform shape in child)
                {
                    Vector2Int shapeOffset = new Vector2Int(
                        Mathf.RoundToInt(shape.localPosition.x / 0.79f),
                        Mathf.RoundToInt(shape.localPosition.z / 0.79f)
                    );
                    data.shapes.Add(new ShapeData { offset = shapeOffset });
                }

                result.Add(data);
            }
        }

        return result;
    }

    public void LoadFromData(StageData data)
    {
        Selection.activeObject = null;
        
        foreach (Transform child in transform)
        {
            GameObject.DestroyImmediate(child.gameObject);
        }

        foreach (var playingData in data.playingBlocks)
        {
            GameObject obj = Instantiate(playingBlockPrefab, transform);
            obj.transform.position = new Vector3(playingData.center.x * 0.79f, 0, playingData.center.y * 0.79f);

            var comp = obj.GetComponent<PlayingBlockEditorObject>();
            comp.uniqueIndex = playingData.uniqueIndex;
            comp.colorType = playingData.colorType;
            comp.gimmicks = playingData.gimmicks;
            comp.shapes = playingData.shapes;

            // 자식 블록 배치 (shapes 기준)
            foreach (var shape in playingData.shapes)
            {
                GameObject shapeBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shapeBlock.transform.SetParent(obj.transform);
                shapeBlock.transform.localPosition = new Vector3(shape.offset.x * 0.79f, 0, shape.offset.y * 0.79f);
            }

            comp.UpdateVisual();
        }
    }
}