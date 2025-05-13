using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class BlockGroupFactory
{
    private readonly GameObject blockGroupPrefab;
    private readonly GameObject blockPrefab;
    private readonly Material[] testBlockMaterials;
    private readonly float blockDistance;
    private readonly int boardWidth;
    private readonly int boardHeight;
    private readonly Dictionary<(int x, int y), BoardBlockObject> boardBlockDic;

    public Transform PlayingBlockParent { get; private set; }

    public BlockGroupFactory(GameObject blockGroupPrefab, GameObject blockPrefab, Material[] testBlockMaterials,
        float blockDistance, int boardWidth, int boardHeight,
        Dictionary<(int x, int y), BoardBlockObject> boardBlockDic)
    {
        this.blockGroupPrefab = blockGroupPrefab;
        this.blockPrefab = blockPrefab;
        this.testBlockMaterials = testBlockMaterials;
        this.blockDistance = blockDistance;
        this.boardWidth = boardWidth;
        this.boardHeight = boardHeight;
        this.boardBlockDic = boardBlockDic;
    }

    public async Task CreateBlockGroups(StageData stageData)
    {
        PlayingBlockParent = new GameObject("PlayingBlockParent").transform;

        foreach (var pbData in stageData.playingBlocks)
        {
            GameObject blockGroupObject = Object.Instantiate(blockGroupPrefab, PlayingBlockParent);
            blockGroupObject.transform.position = new Vector3(
                pbData.center.x * blockDistance,
                0.33f,
                pbData.center.y * blockDistance
            );

            BlockDragHandler dragHandler = blockGroupObject.GetComponent<BlockDragHandler>();
            if (dragHandler != null) dragHandler.blocks = new List<BlockObject>();

            dragHandler.uniqueIndex = pbData.uniqueIndex;

            foreach (var gimmick in pbData.gimmicks)
            {
                if (System.Enum.TryParse(gimmick.gimmickType, out ObjectPropertiesEnum.BlockGimmickType gimmickType))
                {
                    dragHandler.gimmickType.Add(gimmickType);
                }
            }

            int minX = boardWidth, maxX = 0, minY = boardHeight, maxY = 0;

            foreach (var shape in pbData.shapes)
            {
                GameObject singleBlock = Object.Instantiate(blockPrefab, blockGroupObject.transform);
                singleBlock.transform.localPosition = new Vector3(
                    shape.offset.x * blockDistance,
                    0f,
                    shape.offset.y * blockDistance
                );

                dragHandler.blockOffsets.Add(new Vector2(shape.offset.x, shape.offset.y));

                var renderer = singleBlock.GetComponentInChildren<SkinnedMeshRenderer>();
                if (renderer != null && pbData.colorType >= 0)
                {
                    renderer.material = testBlockMaterials[(int)pbData.colorType];
                }

                if (singleBlock.TryGetComponent(out BlockObject blockObj))
                {
                    blockObj.colorType = pbData.colorType;
                    blockObj.x = pbData.center.x + shape.offset.x;
                    blockObj.y = pbData.center.y + shape.offset.y;
                    blockObj.offsetToCenter = new Vector2(shape.offset.x, shape.offset.y);

                    dragHandler.blocks.Add(blockObj);

                    if (boardBlockDic.TryGetValue(((int)blockObj.x, (int)blockObj.y), out var boardBlock))
                    {
                        boardBlock.playingBlock = blockObj;
                        blockObj.preBoardBlockObject = boardBlock;
                    }

                    // min/max 계산
                    if (minX > blockObj.x) minX = (int)blockObj.x;
                    if (minY > blockObj.y) minY = (int)blockObj.y;
                    if (maxX < blockObj.x) maxX = (int)blockObj.x;
                    if (maxY < blockObj.y) maxY = (int)blockObj.y;
                }
            }

            dragHandler.horizon = maxX - minX + 1;
            dragHandler.vertical = maxY - minY + 1;
        }

        await Task.Yield();
    }
}
