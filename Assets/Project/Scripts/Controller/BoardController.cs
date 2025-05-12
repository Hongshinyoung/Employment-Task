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
    private BlockGroupFactory blockGroupFactory;
    
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
        
        blockGroupFactory = new BlockGroupFactory(blockGroupPrefab, blockPrefab, testBlockMaterials,
            blockDistance, boardWidth, boardHeight, boardBlockDic);

        await blockGroupFactory.CreateBlockGroups(stageDatas[stageIdx]);

        playingBlockParent = blockGroupFactory.PlayingBlockParent.gameObject;

        CreateMaskingTemp();
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