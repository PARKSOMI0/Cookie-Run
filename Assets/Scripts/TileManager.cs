using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 쿠키런 스타일 러닝게임의 맵 타일 매니저.
/// 플레이어가 달리면 앞에 타일을 계속 생성하고,
/// 지나간 타일은 삭제하여 무한 러닝 맵을 구현한다.
/// </summary>
public class TileManager : MonoBehaviour
{
    [Header("=== 타일 프리팹 ===")]
    [Tooltip("Ground Tile 프리팹 (GroundTile 컴포넌트가 붙어있어야 함)")]
    [SerializeField] private GameObject[] tilePrefabs;

    [Header("=== 맵 생성 설정 ===")]
    [Tooltip("화면에 유지할 최소 타일 개수")]
    [SerializeField] private int tilesAhead = 5;

    [Tooltip("플레이어 뒤에 유지할 타일 개수 (이보다 멀면 삭제)")]
    [SerializeField] private int tilesBehind = 2;

    [Header("=== 참조 ===")]
    [Tooltip("플레이어 Transform (타일 생성/삭제 기준)")]
    [SerializeField] private Transform player;

    [Header("=== 맵 스크롤 설정 ===")]
    [Tooltip("맵 스크롤 속도 (플레이어가 고정이고 맵이 움직이는 방식일 때 사용)")]
    [SerializeField] private float scrollSpeed = 8f;

    [Tooltip("true: 맵이 왼쪽으로 이동 (플레이어 고정) / false: 플레이어 기준으로 타일 생성")]
    [SerializeField] private bool useScrollMode = true;

    // 활성 타일 목록
    private readonly List<GameObject> activeTiles = new();

    // 다음 타일 스폰 위치 (X좌표)
    private float nextSpawnX = 0f;

    // 기본 타일 길이 (프리팹에서 가져옴)
    private float defaultTileLength = 20f;

    // ──────────────────────────────────────────────
    //  Unity 생명주기
    // ──────────────────────────────────────────────

    private void Start()
    {
        // 기본 타일 길이 미리 확인
        if (tilePrefabs != null && tilePrefabs.Length > 0)
        {
            var gt = tilePrefabs[0].GetComponent<GroundTile>();
            if (gt != null) defaultTileLength = gt.TileLength;
        }

        // 초기 타일 생성
        for (int i = 0; i < tilesAhead + tilesBehind; i++)
        {
            SpawnTile();
        }
    }

    private void Update()
    {
        if (useScrollMode)
        {
            // ── 스크롤 모드: 타일이 왼쪽으로 이동 ──
            ScrollTiles();
            EnsureTilesAhead_ScrollMode();
            RemovePassedTiles_ScrollMode();
        }
        else
        {
            // ── 플레이어 이동 모드: 플레이어 위치 기준 ──
            if (player == null) return;
            EnsureTilesAhead_PlayerMode();
            RemovePassedTiles_PlayerMode();
        }
    }

    // ──────────────────────────────────────────────
    //  타일 생성
    // ──────────────────────────────────────────────

    private void SpawnTile()
    {
        if (tilePrefabs == null || tilePrefabs.Length == 0)
        {
            Debug.LogWarning("[TileManager] 타일 프리팹이 설정되지 않았습니다!");
            return;
        }

        // 랜덤 타일 프리팹 선택
        GameObject prefab = tilePrefabs[UnityEngine.Random.Range(0, tilePrefabs.Length)];
        Vector3 spawnPos = new Vector3(nextSpawnX, 0f, 0f);

        GameObject tile = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
        activeTiles.Add(tile);

        // GroundTile 컴포넌트에서 타일 길이 가져오고 오브젝트 스폰
        GroundTile groundTile = tile.GetComponent<GroundTile>();
        float length = defaultTileLength;

        if (groundTile != null)
        {
            length = groundTile.TileLength;
            groundTile.SpawnObjects();
        }

        nextSpawnX += length;
    }

    // ──────────────────────────────────────────────
    //  스크롤 모드 (맵이 왼쪽으로 이동)
    // ──────────────────────────────────────────────

    private void ScrollTiles()
    {
        float delta = scrollSpeed * Time.deltaTime;

        // 모든 타일을 왼쪽으로 이동
        foreach (var tile in activeTiles)
        {
            if (tile != null)
                tile.transform.position += Vector3.left * delta;
        }

        // nextSpawnX도 같이 이동
        nextSpawnX -= delta;
    }

    private void EnsureTilesAhead_ScrollMode()
    {
        // 화면 오른쪽 끝 근처까지 타일이 있어야 함
        float screenRightEdge = Camera.main != null
            ? Camera.main.transform.position.x + Camera.main.orthographicSize * Camera.main.aspect + defaultTileLength
            : 20f;

        while (nextSpawnX < screenRightEdge + defaultTileLength * tilesAhead)
        {
            SpawnTile();
        }
    }

    private void RemovePassedTiles_ScrollMode()
    {
        float screenLeftEdge = Camera.main != null
            ? Camera.main.transform.position.x - Camera.main.orthographicSize * Camera.main.aspect
            : -15f;

        for (int i = activeTiles.Count - 1; i >= 0; i--)
        {
            if (activeTiles[i] == null)
            {
                activeTiles.RemoveAt(i);
                continue;
            }

            GroundTile gt = activeTiles[i].GetComponent<GroundTile>();
            float tileEnd = activeTiles[i].transform.position.x + (gt != null ? gt.TileLength : defaultTileLength);

            // 타일의 오른쪽 끝이 화면 왼쪽 밖으로 나갔으면 삭제
            if (tileEnd < screenLeftEdge - defaultTileLength)
            {
                if (gt != null) gt.ClearSpawnedObjects();
                Destroy(activeTiles[i]);
                activeTiles.RemoveAt(i);
            }
        }
    }

    // ──────────────────────────────────────────────
    //  플레이어 이동 모드
    // ──────────────────────────────────────────────

    private void EnsureTilesAhead_PlayerMode()
    {
        float playerX = player.position.x;
        float aheadDistance = defaultTileLength * tilesAhead;

        while (nextSpawnX < playerX + aheadDistance)
        {
            SpawnTile();
        }
    }

    private void RemovePassedTiles_PlayerMode()
    {
        float playerX = player.position.x;
        float behindDistance = defaultTileLength * tilesBehind;

        for (int i = activeTiles.Count - 1; i >= 0; i--)
        {
            if (activeTiles[i] == null)
            {
                activeTiles.RemoveAt(i);
                continue;
            }

            GroundTile gt = activeTiles[i].GetComponent<GroundTile>();
            float tileEnd = activeTiles[i].transform.position.x + (gt != null ? gt.TileLength : defaultTileLength);

            if (tileEnd < playerX - behindDistance)
            {
                if (gt != null) gt.ClearSpawnedObjects();
                Destroy(activeTiles[i]);
                activeTiles.RemoveAt(i);
            }
        }
    }
}
