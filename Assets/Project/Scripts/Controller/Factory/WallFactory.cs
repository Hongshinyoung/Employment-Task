using System.Collections.Generic;
using System.Threading.Tasks;
using Project.Scripts.Data_Script;
using UnityEngine;

public class WallFactory
{
    private readonly float blockDistance;
    private readonly Transform boardParent;
    private readonly Material[] wallMaterials;
    private readonly GameObject[] wallPrefabs;

    public WallFactory(GameObject[] wallPrefabs, Material[] wallMaterials, float blockDistance, Transform boardParent)
    {
        this.wallPrefabs = wallPrefabs;
        this.wallMaterials = wallMaterials;
        this.blockDistance = blockDistance;
        this.boardParent = boardParent;

        WallCoordinateInfoDic = new Dictionary<(int x, int y), Dictionary<(DestroyWallDirection, ColorType), int>>();
        Walls = new List<GameObject>();
    }

    public Dictionary<(int x, int y), Dictionary<(DestroyWallDirection, ColorType), int>> WallCoordinateInfoDic { get; }
    public List<GameObject> Walls { get; }

    public async Task CreateWalls(StageData stageData)
    {
        if (stageData == null || stageData.Walls == null)
        {
            Debug.LogError("StageData가 유효하지 않거나 Walls 데이터가 없습니다.");
            return;
        }

        GameObject wallsParent = new GameObject("CustomWallsParent");
        wallsParent.transform.SetParent(boardParent);

        foreach (var wallData in stageData.Walls)
        {
            var (position, rotation, destroyDirection, shouldAddWallInfo) = CalculateWallTransform(wallData);

            if (shouldAddWallInfo && wallData.wallColor != ColorType.None)
            {
                var pos = (wallData.x, wallData.y);
                var wallInfo = (destroyDirection, wallData.wallColor);

                if (!WallCoordinateInfoDic.ContainsKey(pos))
                    WallCoordinateInfoDic[pos] = new Dictionary<(DestroyWallDirection, ColorType), int>();
                WallCoordinateInfoDic[pos][wallInfo] = wallData.length;
            }

            // 길이에 따른 위치 조정 (수평/수직 벽만)
            AdjustWallPositionForLength(ref position, wallData);

            if (wallData.length - 1 >= 0 && wallData.length - 1 < wallPrefabs.Length)
            {
                var wallObj = Object.Instantiate(wallPrefabs[wallData.length - 1], wallsParent.transform);
                wallObj.transform.position = position;
                wallObj.transform.rotation = rotation;

                var wall = wallObj.GetComponent<WallObject>();
                wall.SetWall(wallMaterials[(int)wallData.wallColor], wallData.wallColor != ColorType.None);

                Walls.Add(wallObj);
            }
            else
            {
                Debug.LogError($"벽 프리팹 인덱스 오류: {wallData.length - 1}");
            }
        }

        await Task.Yield();
    }

    private (Vector3 position, Quaternion rotation, DestroyWallDirection destroyDirection, bool shouldAddWallInfo)
        CalculateWallTransform(WallData wallData)
    {
        var position = new Vector3(wallData.x * blockDistance, 0f, wallData.y * blockDistance);
        var rotation = Quaternion.identity;
        var destroyDirection = DestroyWallDirection.None;
        var shouldAddWallInfo = false;

        switch (wallData.WallDirection)
        {
            case ObjectPropertiesEnum.WallDirection.Single_Up:
                position.z += 0.5f;
                rotation = Quaternion.Euler(0f, 180f, 0f);
                destroyDirection = DestroyWallDirection.Up;
                shouldAddWallInfo = true;
                break;
            case ObjectPropertiesEnum.WallDirection.Single_Down:
                position.z -= 0.5f;
                rotation = Quaternion.identity;
                destroyDirection = DestroyWallDirection.Down;
                shouldAddWallInfo = true;
                break;
            case ObjectPropertiesEnum.WallDirection.Single_Left:
                position.x -= 0.5f;
                rotation = Quaternion.Euler(0f, 90f, 0f);
                destroyDirection = DestroyWallDirection.Left;
                shouldAddWallInfo = true;
                break;
            case ObjectPropertiesEnum.WallDirection.Single_Right:
                position.x += 0.5f;
                rotation = Quaternion.Euler(0f, -90f, 0f);
                destroyDirection = DestroyWallDirection.Right;
                shouldAddWallInfo = true;
                break;
            case ObjectPropertiesEnum.WallDirection.Left_Up:
                // 왼쪽 위 모서리
                position.x -= 0.5f;
                position.z += 0.5f;
                rotation = Quaternion.Euler(0f, 180f, 0f);
                break;

            case ObjectPropertiesEnum.WallDirection.Left_Down:
                // 왼쪽 아래 모서리
                position.x -= 0.5f;
                position.z -= 0.5f;
                rotation = Quaternion.identity;
                break;

            case ObjectPropertiesEnum.WallDirection.Right_Up:
                // 오른쪽 위 모서리
                position.x += 0.5f;
                position.z += 0.5f;
                rotation = Quaternion.Euler(0f, 270f, 0f);
                break;

            case ObjectPropertiesEnum.WallDirection.Right_Down:
                // 오른쪽 아래 모서리
                position.x += 0.5f;
                position.z -= 0.5f;
                rotation = Quaternion.Euler(0f, 0f, 0f);
                break;

            case ObjectPropertiesEnum.WallDirection.Open_Up:
                // 위쪽이 열린 벽
                position.z += 0.5f;
                rotation = Quaternion.Euler(0f, 180f, 0f);
                break;

            case ObjectPropertiesEnum.WallDirection.Open_Down:
                // 아래쪽이 열린 벽
                position.z -= 0.5f;
                rotation = Quaternion.identity;
                break;

            case ObjectPropertiesEnum.WallDirection.Open_Left:
                // 왼쪽이 열린 벽
                position.x -= 0.5f;
                rotation = Quaternion.Euler(0f, 90f, 0f);
                break;

            case ObjectPropertiesEnum.WallDirection.Open_Right:
                // 오른쪽이 열린 벽
                position.x += 0.5f;
                rotation = Quaternion.Euler(0f, -90f, 0f);
                break;
            default:
                Debug.LogWarning($"벽 방향 {wallData.WallDirection} 은 특별 처리 없음.");
                break;
        }

        return (position, rotation, destroyDirection, shouldAddWallInfo);
    }

    private void AdjustWallPositionForLength(ref Vector3 position, WallData wallData)
    {
        if (wallData.length > 1)
            switch (wallData.WallDirection)
            {
                case ObjectPropertiesEnum.WallDirection.Single_Up:
                case ObjectPropertiesEnum.WallDirection.Single_Down:
                case ObjectPropertiesEnum.WallDirection.Open_Up:
                case ObjectPropertiesEnum.WallDirection.Open_Down:
                    position.x += (wallData.length - 1) * blockDistance * 0.5f;
                    break;

                case ObjectPropertiesEnum.WallDirection.Single_Left:
                case ObjectPropertiesEnum.WallDirection.Single_Right:
                case ObjectPropertiesEnum.WallDirection.Open_Left:
                case ObjectPropertiesEnum.WallDirection.Open_Right:
                    position.z += (wallData.length - 1) * blockDistance * 0.5f;
                    break;
            }
    }
}