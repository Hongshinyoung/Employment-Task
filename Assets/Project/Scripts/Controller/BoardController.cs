using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public partial class BoardController : MonoBehaviour
{
    public static BoardController Instance;
    
    [SerializeField] private StageData[] stageDatas;

    [SerializeField] private GameObject boardBlockPrefab;
    [SerializeField] private GameObject blockGroupPrefab; 
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private Material[] blockMaterials;
    [SerializeField] private Material[] testBlockMaterials;
    [SerializeField] private GameObject[] wallPrefabs;
    [SerializeField] private Material[] wallMaterials;
    [SerializeField] private Transform spawnerTr;
    [SerializeField] private Transform quadTr;
    [SerializeField] ParticleSystem destroyParticle;

    public ParticleSystem destroyParticlePrefab => destroyParticle;
    public List<SequentialCubeParticleSpawner> particleSpawners;
    public List<GameObject> walls = new List<GameObject>();

    private Dictionary<int, List<BoardBlockObject>> CheckBlockGroupDic { get; set; }
    private Dictionary<(int x, int y), BoardBlockObject> boardBlockDic;
    private Dictionary<(int, bool), BoardBlockObject> standardBlockDic = new Dictionary<(int, bool), BoardBlockObject>();
    private Dictionary<(int x, int y), Dictionary<(DestroyWallDirection, ColorType), int>> wallCoorInfoDic;

    private WallFactory wallFactory;
    private GameObject boardParent;
    private GameObject playingBlockParent;
    public int boardWidth;
    public int boardHeight;

    private readonly float blockDistance = 0.79f;

    private int nowStageIndex = 0;

    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        Init();
    }

    private async Task Init(int stageIdx = 0)
    {
        if (stageDatas == null)
        {
            Debug.LogError("StageData가 할당되지 않았습니다!");
            return;
        }

        boardBlockDic = new Dictionary<(int x, int y), BoardBlockObject>();
        CheckBlockGroupDic = new Dictionary<int, List<BoardBlockObject>>();

        boardParent = new GameObject("BoardParent");
        boardParent.transform.SetParent(transform);
        
        wallFactory = new WallFactory(wallPrefabs, wallMaterials, blockDistance, boardParent.transform);
        
        await wallFactory.CreateWalls(stageDatas[stageIdx]);

        wallCoorInfoDic = wallFactory.WallCoordinateInfoDic;
        walls = wallFactory.Walls;
        
        await CreateBoardAsync(stageIdx);

        await CreatePlayingBlocksAsync(stageIdx);

        CreateMaskingTemp();
    }

    private async Task CreateBoardAsync(int stageIdx = 0)
    {
        nowStageIndex = stageIdx;
        int standardBlockIndex = -1;
        
        // 보드 블록 생성
        for (int i = 0; i < stageDatas[stageIdx].boardBlocks.Count; i++)
        {
            BoardBlockData data = stageDatas[stageIdx].boardBlocks[i];

            GameObject blockObj = Instantiate(boardBlockPrefab, boardParent.transform);
            blockObj.transform.localPosition = new Vector3(
                data.x * blockDistance,
                0,
                data.y * blockDistance
            );

            if (blockObj.TryGetComponent(out BoardBlockObject boardBlock))
            {
                boardBlock._ctrl = this;
                boardBlock.x = data.x;
                boardBlock.y = data.y;
                
                if (wallCoorInfoDic.ContainsKey((boardBlock.x, boardBlock.y)))
                {
                    for (int k = 0; k < wallCoorInfoDic[(boardBlock.x, boardBlock.y)].Count; k++)
                    {
                        boardBlock.colorType.Add(wallCoorInfoDic[(boardBlock.x, boardBlock.y)].Keys.ElementAt(k).Item2);
                        boardBlock.len.Add(wallCoorInfoDic[(boardBlock.x, boardBlock.y)].Values.ElementAt(k));
                        
                        DestroyWallDirection dir = wallCoorInfoDic[(boardBlock.x, boardBlock.y)].Keys.ElementAt(k).Item1;
                        bool horizon = dir == DestroyWallDirection.Up || dir == DestroyWallDirection.Down;
                        boardBlock.isHorizon.Add(horizon);

                        standardBlockDic.Add((++standardBlockIndex, horizon), boardBlock);
                    }
                    boardBlock.isCheckBlock = true;
                }
                else
                {
                    boardBlock.isCheckBlock = false;
                }

                boardBlockDic.Add((data.x, data.y), boardBlock);
            }
            else
            {
                Debug.LogWarning("boardBlockPrefab에 BoardBlockObject 컴포넌트가 필요합니다!");
            }
        }

        // standardBlockDic에서 관련 위치의 블록들 설정
        foreach (var kv in standardBlockDic)
        {
            BoardBlockObject boardBlockObject = kv.Value;
            for (int i = 0; i < boardBlockObject.colorType.Count; i++)
            {
                if (kv.Key.Item2) // 가로 방향
                {
                    for (int j = boardBlockObject.x + 1; j < boardBlockObject.x + boardBlockObject.len[i]; j++)
                    {
                        if (boardBlockDic.TryGetValue((j, boardBlockObject.y), out BoardBlockObject targetBlock))
                        {
                            targetBlock.colorType.Add(boardBlockObject.colorType[i]);
                            targetBlock.len.Add(boardBlockObject.len[i]);
                            targetBlock.isHorizon.Add(kv.Key.Item2);
                            targetBlock.isCheckBlock = true;
                        }
                    }
                }
                else // 세로 방향
                {
                    for (int k = boardBlockObject.y + 1; k < boardBlockObject.y + boardBlockObject.len[i]; k++)
                    {
                        if (boardBlockDic.TryGetValue((boardBlockObject.x, k), out BoardBlockObject targetBlock))
                        {
                            targetBlock.colorType.Add(boardBlockObject.colorType[i]);
                            targetBlock.len.Add(boardBlockObject.len[i]);
                            targetBlock.isHorizon.Add(kv.Key.Item2);
                            targetBlock.isCheckBlock = true;
                        }
                    }
                }
            }
        }

        // 3체크 블록 그룹 생성
        int checkBlockIndex = -1;
        CheckBlockGroupDic = new Dictionary<int, List<BoardBlockObject>>();

        foreach (var blockPos in boardBlockDic.Keys)
        {
            BoardBlockObject boardBlock = boardBlockDic[blockPos];
            
            for (int j = 0; j < boardBlock.colorType.Count; j++)
            {
                if (boardBlock.isCheckBlock && boardBlock.colorType[j] != ColorType.None)
                {
                    // 이 블록이 이미 그룹에 속해있는지 확인
                    if (boardBlock.checkGroupIdx.Count <= j)
                    {
                        if (boardBlock.isHorizon[j])
                        {
                            // 왼쪽 블록 확인
                            (int x, int y) leftPos = (boardBlock.x - 1, boardBlock.y);
                            if (boardBlockDic.TryGetValue(leftPos, out BoardBlockObject leftBlock) &&
                                j < leftBlock.colorType.Count &&
                                leftBlock.colorType[j] == boardBlock.colorType[j] &&
                                leftBlock.checkGroupIdx.Count > j)
                            {
                                int grpIdx = leftBlock.checkGroupIdx[j];
                                CheckBlockGroupDic[grpIdx].Add(boardBlock);
                                boardBlock.checkGroupIdx.Add(grpIdx);
                            }
                            else
                            {
                                checkBlockIndex++;
                                CheckBlockGroupDic.Add(checkBlockIndex, new List<BoardBlockObject>());
                                CheckBlockGroupDic[checkBlockIndex].Add(boardBlock);
                                boardBlock.checkGroupIdx.Add(checkBlockIndex);
                            }
                        }
                        else
                        {
                            // 위쪽 블록 확인
                            (int x, int y) upPos = (boardBlock.x, boardBlock.y - 1);
                            if (boardBlockDic.TryGetValue(upPos, out BoardBlockObject upBlock) &&
                                j < upBlock.colorType.Count &&
                                upBlock.colorType[j] == boardBlock.colorType[j] &&
                                upBlock.checkGroupIdx.Count > j)
                            {
                                int grpIdx = upBlock.checkGroupIdx[j];
                                CheckBlockGroupDic[grpIdx].Add(boardBlock);
                                boardBlock.checkGroupIdx.Add(grpIdx);
                            }
                            else
                            {
                                checkBlockIndex++;
                                CheckBlockGroupDic.Add(checkBlockIndex, new List<BoardBlockObject>());
                                CheckBlockGroupDic[checkBlockIndex].Add(boardBlock);
                                boardBlock.checkGroupIdx.Add(checkBlockIndex);
                            }
                        }
                    }
                }
            }
        }

        await Task.Yield();
        
        boardWidth = boardBlockDic.Keys.Max(k => k.x);
        boardHeight = boardBlockDic.Keys.Max(k => k.y);
    }
    
     private async Task CreatePlayingBlocksAsync(int stageIdx = 0)
     {
         playingBlockParent = new GameObject("PlayingBlockParent");
         
         for (int i = 0; i < stageDatas[stageIdx].playingBlocks.Count; i++)
         {
             var pbData = stageDatas[stageIdx].playingBlocks[i];

             GameObject blockGroupObject = Instantiate(blockGroupPrefab, playingBlockParent.transform);
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
                 if (Enum.TryParse(gimmick.gimmickType, out ObjectPropertiesEnum.BlockGimmickType gimmickType))
                 {
                     dragHandler.gimmickType.Add(gimmickType);
                 }
             }
             
             int maxX = 0;
             int minX = boardWidth;
             int maxY = 0;
             int minY = boardHeight;
             foreach (var shape in pbData.shapes)
             {
                 GameObject singleBlock = Instantiate(blockPrefab, blockGroupObject.transform);
                 
                 singleBlock.transform.localPosition = new Vector3(
                     shape.offset.x * blockDistance,
                     0f,
                     shape.offset.y * blockDistance
                 );
                 dragHandler.blockOffsets.Add(new Vector2(shape.offset.x, shape.offset.y));

                 /*if (shape.colliderDirectionX > 0 && shape.colliderDirectionY > 0)
                 {
                     BoxCollider collider = dragHandler.AddComponent<BoxCollider>();
                     dragHandler.col = collider;

                     Vector3 localColCenter = singleBlock.transform.localPosition;
                     int x = shape.colliderDirectionX;
                     int y = shape.colliderDirectionY;
                     
                     collider.center = new Vector3
                         (x > 1 ? localColCenter.x + blockDistance * (x - 1)/ 2 : 0
                          ,0.2f, 
                          y > 1 ? localColCenter.z + blockDistance * (y - 1)/ 2 : 0);
                     collider.size = new Vector3(x * (blockDistance - 0.04f), 0.4f, y * (blockDistance - 0.04f));
                 }*/
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
                     
                     if (dragHandler != null)
                         dragHandler.blocks.Add(blockObj);
                     boardBlockDic[((int)blockObj.x, (int)blockObj.y)].playingBlock = blockObj;
                     blockObj.preBoardBlockObject = boardBlockDic[((int)blockObj.x, (int)blockObj.y)];
                     if(minX > blockObj.x) minX = (int)blockObj.x;
                     if(minY > blockObj.y) minY = (int)blockObj.y;
                     if(maxX < blockObj.x) maxX = (int)blockObj.x;
                     if(maxY < blockObj.y) maxY = (int)blockObj.y;
                 }
             }

             dragHandler.horizon = maxX - minX + 1;
             dragHandler.vertical = maxY - minY + 1;
         }

         await Task.Yield();
     }

    public void GoToPreviousLevel()
    {
        if (nowStageIndex == 0) return;

        Destroy(boardParent);
        Destroy(playingBlockParent.gameObject);
        Init(--nowStageIndex);
        
        StartCoroutine(Wait());
    }

    public void GotoNextLevel()
    {
        if (nowStageIndex == stageDatas.Length - 1) return;
        
        Destroy(boardParent);
        Destroy(playingBlockParent.gameObject);
        Init(++nowStageIndex);
        
        StartCoroutine(Wait());
    }

    IEnumerator Wait()
    {
        yield return null;
        
        Vector3 camTr = Camera.main.transform.position;
        Camera.main.transform.position = new Vector3(1.5f + 0.5f * (boardWidth - 4),camTr.y,camTr.z);
    } 
}