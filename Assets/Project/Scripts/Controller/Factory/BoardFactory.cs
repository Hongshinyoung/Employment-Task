using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class BoardFactory
{
    private readonly GameObject boardBlockPrefab;
    private readonly float blockDistance;
    private readonly Transform boardParent;
    private readonly Dictionary<(int x, int y), Dictionary<(DestroyWallDirection, ColorType), int>> wallCoorInfoDic;

    public Dictionary<(int x, int y), BoardBlockObject> BoardBlockDic { get; private set; }
    public Dictionary<int, List<BoardBlockObject>> CheckBlockGroupDic { get; private set; }
    public int BoardWidth { get; private set; }
    public int BoardHeight { get; private set; }

    public BoardFactory(GameObject boardBlockPrefab, float blockDistance, Transform boardParent,
        Dictionary<(int x, int y), Dictionary<(DestroyWallDirection, ColorType), int>> wallCoorInfoDic)
    {
        this.boardBlockPrefab = boardBlockPrefab;
        this.blockDistance = blockDistance;
        this.boardParent = boardParent;
        this.wallCoorInfoDic = wallCoorInfoDic;
    }

    public async Task CreateBoard(StageData stageData)
    {
        BoardBlockDic = new Dictionary<(int x, int y), BoardBlockObject>();
        var standardBlockDic = new Dictionary<(int, bool), BoardBlockObject>();
        int standardBlockIndex = -1;

        // 보드 블록 생성 및 설정
        foreach (var data in stageData.boardBlocks)
        {
            GameObject blockObj = Object.Instantiate(boardBlockPrefab, boardParent);
            blockObj.transform.localPosition = new Vector3(data.x * blockDistance, 0, data.y * blockDistance);

            if (blockObj.TryGetComponent(out BoardBlockObject boardBlock))
            {
                boardBlock._ctrl = BoardController.Instance;
                boardBlock.x = data.x;
                boardBlock.y = data.y;

                if (wallCoorInfoDic != null && wallCoorInfoDic.ContainsKey((boardBlock.x, boardBlock.y)))
                {
                    foreach (var wallInfo in wallCoorInfoDic[(boardBlock.x, boardBlock.y)])
                    {
                        boardBlock.colorType.Add(wallInfo.Key.Item2);
                        boardBlock.len.Add(wallInfo.Value);

                        bool horizon = wallInfo.Key.Item1 == DestroyWallDirection.Up || wallInfo.Key.Item1 == DestroyWallDirection.Down;
                        boardBlock.isHorizon.Add(horizon);

                        standardBlockDic.Add((++standardBlockIndex, horizon), boardBlock);
                    }
                    boardBlock.isCheckBlock = true;
                }
                else
                {
                    boardBlock.isCheckBlock = false;
                }

                BoardBlockDic.Add((data.x, data.y), boardBlock);
            }
        }

        // standardBlockDic 기반 블록 연결 설정
        foreach (var kv in standardBlockDic)
        {
            BoardBlockObject boardBlockObject = kv.Value;
            for (int i = 0; i < boardBlockObject.colorType.Count; i++)
            {
                if (kv.Key.Item2) // 가로 방향
                {
                    for (int j = boardBlockObject.x + 1; j < boardBlockObject.x + boardBlockObject.len[i]; j++)
                    {
                        if (BoardBlockDic.TryGetValue((j, boardBlockObject.y), out var targetBlock))
                        {
                            targetBlock.colorType.Add(boardBlockObject.colorType[i]);
                            targetBlock.len.Add(boardBlockObject.len[i]);
                            targetBlock.isHorizon.Add(true);
                            targetBlock.isCheckBlock = true;
                        }
                    }
                }
                else // 세로 방향
                {
                    for (int k = boardBlockObject.y + 1; k < boardBlockObject.y + boardBlockObject.len[i]; k++)
                    {
                        if (BoardBlockDic.TryGetValue((boardBlockObject.x, k), out var targetBlock))
                        {
                            targetBlock.colorType.Add(boardBlockObject.colorType[i]);
                            targetBlock.len.Add(boardBlockObject.len[i]);
                            targetBlock.isHorizon.Add(false);
                            targetBlock.isCheckBlock = true;
                        }
                    }
                }
            }
        }

        // 3체크 블록 그룹 생성
        CheckBlockGroupDic = new Dictionary<int, List<BoardBlockObject>>();
        int checkBlockIndex = -1;

        foreach (var boardBlock in BoardBlockDic.Values)
        {
            for (int j = 0; j < boardBlock.colorType.Count; j++)
            {
                if (boardBlock.isCheckBlock && boardBlock.colorType[j] != ColorType.None)
                {
                    // 이 블록이 이미 그룹에 속해있는지 확인
                    if (boardBlock.checkGroupIdx.Count <= j)
                    {
                        bool isHorizon = boardBlock.isHorizon[j];
                        var neighborPos = isHorizon ? (boardBlock.x - 1, boardBlock.y) : (boardBlock.x, boardBlock.y - 1);

                        if (BoardBlockDic.TryGetValue(neighborPos, out var neighborBlock) &&
                            j < neighborBlock.colorType.Count &&
                            neighborBlock.colorType[j] == boardBlock.colorType[j] &&
                            neighborBlock.checkGroupIdx.Count > j)
                        {
                            int grpIdx = neighborBlock.checkGroupIdx[j];
                            CheckBlockGroupDic[grpIdx].Add(boardBlock);
                            boardBlock.checkGroupIdx.Add(grpIdx);
                        }
                        else
                        {
                            checkBlockIndex++;
                            CheckBlockGroupDic[checkBlockIndex] = new List<BoardBlockObject> { boardBlock };
                            boardBlock.checkGroupIdx.Add(checkBlockIndex);
                        }
                    }
                }
            }
        }

        // 보드 크기 계산
        BoardWidth = BoardBlockDic.Keys.Max(k => k.x);
        BoardHeight = BoardBlockDic.Keys.Max(k => k.y);

        await Task.Yield();
    }
}
