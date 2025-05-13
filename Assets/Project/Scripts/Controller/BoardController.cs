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
    public Dictionary<(int x, int y), Dictionary<(DestroyWallDirection, ColorType), int>> WallCoorInfoDic => wallCoorInfoDic;

    private WallFactory wallFactory;
    private BoardFactory boardFactory;
    private BlockGroupFactory blockGroupFactory;
    
    private GameObject boardParent;
    private GameObject playingBlockParent;
    public int boardWidth;
    public int boardHeight;

    private readonly float blockDistance = 0.79f;

    private int nowStageIndex = 0;
    
    private IObjectFactory objectFactory;

    private void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;
        objectFactory = new ObjectFactory();
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

        ResetData();

        CreateFactories();
        await InitializeBoard(stageIdx);
        await InitializeBlocks(stageIdx);

        CreateMaskingTemp();
    }

    private void ResetData()
    {
        boardBlockDic = new Dictionary<(int x, int y), BoardBlockObject>();
        CheckBlockGroupDic = new Dictionary<int, List<BoardBlockObject>>();
        boardParent = new GameObject("BoardParent");
        boardParent.transform.SetParent(transform);
    }

    private void CreateFactories()
    {
        wallFactory = new WallFactory(objectFactory, wallPrefabs, wallMaterials, boardParent.transform);
        boardFactory = new BoardFactory(boardBlockPrefab, blockDistance, boardParent.transform, wallCoorInfoDic);
        blockGroupFactory = new BlockGroupFactory(blockGroupPrefab, blockPrefab, testBlockMaterials, blockDistance, boardWidth, boardHeight, boardBlockDic);
    }

    private async Task InitializeBoard(int stageIdx)
    {
        await wallFactory.CreateWalls(stageDatas[stageIdx]);
        wallCoorInfoDic = wallFactory.WallCoordinateInfoDic;

        await boardFactory.CreateBoard(stageDatas[stageIdx]);
        boardBlockDic = boardFactory.BoardBlockDic;
        CheckBlockGroupDic = boardFactory.CheckBlockGroupDic;
        boardWidth = boardFactory.BoardWidth;
        boardHeight = boardFactory.BoardHeight;
        walls = wallFactory.Walls;
    }

    private async Task InitializeBlocks(int stageIdx)
    {
        await blockGroupFactory.CreateBlockGroups(stageDatas[stageIdx]);
        playingBlockParent = blockGroupFactory.PlayingBlockParent.gameObject;
    }
    
    public void DestroyBlockGroup(BlockObject block)
    {
        if (boardBlockDic.TryGetValue(((int)block.x, (int)block.y), out var boardBlock))
        {
            if (boardBlock.checkGroupIdx.Count > 0)
            {
                int groupIdx = boardBlock.checkGroupIdx[0];
                if (CheckBlockGroupDic.TryGetValue(groupIdx, out var blocks))
                {
                    foreach (var b in blocks)
                    {
                        Destroy(b.playingBlock.gameObject);
                        b.playingBlock = null;
                    }

                    CheckBlockGroupDic.Remove(groupIdx);

                    Instantiate(destroyParticlePrefab, boardBlock.transform.position, Quaternion.identity);
                }
            }
        }
    }

    public void GoToPreviousLevel()
    {
        if (nowStageIndex == 0) return;

        Destroy(boardParent);
        boardParent = null;
        Destroy(playingBlockParent.gameObject);
        playingBlockParent = null;
        Init(--nowStageIndex);

        StartCoroutine(AdjustCameraPosition());
    }

    public void GotoNextLevel()
    {
        if (nowStageIndex == stageDatas.Length - 1) return;

        Destroy(boardParent);
        boardParent = null;
        Destroy(playingBlockParent.gameObject);
        playingBlockParent = null;
        Init(++nowStageIndex);

        StartCoroutine(AdjustCameraPosition());
    }

    private IEnumerator AdjustCameraPosition()
    {
        yield return null;

        Vector3 camTr = Camera.main.transform.position;
        Camera.main.transform.position = new Vector3(1.5f + 0.5f * (boardWidth - 4), camTr.y, camTr.z);
    }
}