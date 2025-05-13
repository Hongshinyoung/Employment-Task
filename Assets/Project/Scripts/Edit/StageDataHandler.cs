using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Project.Scripts.Data_Script;

public static class StageDataHandler
{
    public static void Save(StageData data, Transform objectsRoot)
    {
        data.boardBlocks.Clear();
        data.playingBlocks.Clear();
        data.Walls.Clear();

        foreach (Transform child in objectsRoot)
        {
            if (child.TryGetComponent(out BlockEditorObject block))
            {
                var newData = new BoardBlockData
                {
                    x = Mathf.RoundToInt(child.position.x / 0.79f),
                    y = Mathf.RoundToInt(child.position.z / 0.79f),
                    colorType = new List<ColorType> { block.colorType },
                    dataType = new List<int> { 0 } // 필요한 경우 확장
                };
                data.boardBlocks.Add(newData);
            }
            else if (child.TryGetComponent(out WallEditorObject wall))
            {
                var newData = new WallData
                {
                    x = Mathf.RoundToInt(child.position.x / 0.79f),
                    y = Mathf.RoundToInt(child.position.z / 0.79f),
                    WallDirection = wall.wallDirection,
                    length = wall.length,
                    wallColor = wall.colorType,
                    wallGimmickType = wall.gimmickType
                };
                data.Walls.Add(newData);
            }
            else if (child.TryGetComponent(out PlayingBlockEditorObject playingBlock))
            {
                var newData = new PlayingBlockData
                {
                    center = new Vector2Int(
                        Mathf.RoundToInt(child.position.x / 0.79f),
                        Mathf.RoundToInt(child.position.z / 0.79f)),
                    uniqueIndex = playingBlock.uniqueIndex,
                    colorType = playingBlock.colorType,
                    shapes = playingBlock.shapes,
                    gimmicks = playingBlock.gimmicks
                };
                data.playingBlocks.Add(newData);
            }
        }

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log($"StageData [{data.name}] 저장 완료.");
    }

    public static void Load(StageData data, Transform objectsRoot, GameObject boardPrefab, GameObject wallPrefab, GameObject playingPrefab)
    {
        Selection.activeObject = null;
        
        foreach (Transform child in objectsRoot)
        {
            GameObject.DestroyImmediate(child.gameObject);
        }

        foreach (var blockData in data.boardBlocks)
        {
            GameObject obj = Object.Instantiate(boardPrefab, objectsRoot);
            obj.transform.position = new Vector3(blockData.x * 0.79f, 0, blockData.y * 0.79f);

            var comp = obj.GetComponent<BlockEditorObject>();
            comp.UpdateColor(blockData.colorType[0]);
        }

        foreach (var wallData in data.Walls)
        {
            GameObject obj = Object.Instantiate(wallPrefab, objectsRoot);
            obj.transform.position = new Vector3(wallData.x * 0.79f, 0, wallData.y * 0.79f);

            var comp = obj.GetComponent<WallEditorObject>();
            comp.wallDirection = wallData.WallDirection;
            comp.length = wallData.length;
            comp.colorType = wallData.wallColor;
            comp.gimmickType = wallData.wallGimmickType;
            comp.UpdateVisual();
        }

        foreach (var playingData in data.playingBlocks)
        {
            GameObject obj = Object.Instantiate(playingPrefab, objectsRoot);
            obj.transform.position = new Vector3(playingData.center.x * 0.79f, 0, playingData.center.y * 0.79f);

            var comp = obj.GetComponent<PlayingBlockEditorObject>();
            comp.uniqueIndex = playingData.uniqueIndex;
            comp.colorType = playingData.colorType;
            comp.shapes = playingData.shapes;
            comp.gimmicks = playingData.gimmicks;
            comp.UpdateVisual();
        }

        Debug.Log($"StageData [{data.name}] 로드 완료.");
    }
}
