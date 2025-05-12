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
    private BoardFactory boardFactory;
    
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

        boardFactory = new BoardFactory(boardBlockPrefab, blockDistance, boardParent.transform, wallCoorInfoDic);

        await boardFactory.CreateBoard(stageDatas[stageIdx]);

        boardBlockDic = boardFactory.BoardBlockDic;
        CheckBlockGroupDic = boardFactory.CheckBlockGroupDic;
        boardWidth = boardFactory.BoardWidth;
        boardHeight = boardFactory.BoardHeight;
        
        await CreatePlayingBlocksAsync(stageIdx);

        CreateMaskingTemp();
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