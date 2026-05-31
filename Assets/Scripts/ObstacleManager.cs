using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 쿠키런 스타일 장애물 매니저.
/// GroundTile의 자식 "Point" 오브젝트 위치에 장애물을 소환한다.
/// 장애물은 타일의 자식으로 붙어서 타일과 함께 이동/삭제된다.
/// 
/// - 장애물은 min~max 랜덤 주기로 소환
/// - GroundTile 자식 "Point" Transform 위치에 스폰
/// - 장애물 배열에서 랜덤으로 선택하여 스폰
/// - 공통 태그를 자동 적용
///
/// [사용법]
/// 1. 빈 GameObject에 이 스크립트 추가
/// 2. TileManager 레퍼런스 할당
/// 3. GroundTile 프리팹 안에 "Point" 이름의 빈 자식 오브젝트 배치
/// 4. obstacles 배열에 장애물 이름/프리팹 등록
/// 5. Play!
/// </summary>
public class ObstacleManager : MonoBehaviour
{
    [Header("=== TileManager 참조 ===")]
    [Tooltip("타일 매니저 (스크롤 속도를 동기화하기 위해 필요)")]
    public TileManager tileManager;

    [Header("=== 장애물 배열 ===")]
    [Tooltip("소환할 장애물 목록")]
    public ObstacleEntry[] obstacles;

    [Header("=== 소환 설정 ===")]
    [Tooltip("장애물 소환 최소 간격 (초)")]
    public float spawnIntervalMin = 1.5f;

    [Tooltip("장애물 소환 최대 간격 (초)")]
    public float spawnIntervalMax = 4.0f;

    [Header("=== 태그 설정 ===")]
    [Tooltip("장애물에 적용할 공통 태그")]
    public string obstacleTag = "Obstacle";

    [Header("=== Point 설정 ===")]
    [Tooltip("GroundTile 자식 중 소환 위치로 사용할 오브젝트 이름")]
    public string pointName = "Point";

    // ── 내부 변수 ──
    private float _spawnTimer;
    private float _nextSpawnTime;
    private List<GameObject> _activeObstacles = new List<GameObject>();

    void Start()
    {
        if (tileManager == null)
        {
            Debug.LogError("[ObstacleManager] TileManager가 할당되지 않았습니다!");
        }

        // 첫 소환 타이머 설정
        ResetSpawnTimer();
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        // 소환 타이머
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= _nextSpawnTime)
        {
            SpawnObstacle();
            ResetSpawnTimer();
        }

        // 장애물 이동 및 삭제
        ScrollAndCleanup();
    }

    // ───────────────────────────────────────────
    //  타일 Point 탐색
    // ───────────────────────────────────────────

    /// <summary>
    /// 화면 오른쪽 끝에서 가장 가까운 GroundTile의 자식 "Point"를 찾는다.
    /// </summary>
    private Transform FindRightmostPoint()
    {
        if (tileManager == null) return null;

        Camera cam = Camera.main;
        float camRightEdge = cam.transform.position.x + cam.orthographicSize * cam.aspect;

        Transform bestPoint = null;
        float bestX = float.MinValue;

        // TileManager의 자식 타일들을 순회
        foreach (Transform tile in tileManager.transform)
        {
            // 프리뷰 타일 제외
            if (tile.name.StartsWith("[Preview]")) continue;

            // "Point" 자식 찾기
            Transform point = tile.Find(pointName);
            if (point == null) continue;

            // 화면 오른쪽 근처 또는 밖에 있는 타일 중 가장 오른쪽 것 선택
            float tileX = tile.position.x;
            if (tileX > bestX && tileX >= camRightEdge - 2f)
            {
                bestX = tileX;
                bestPoint = point;
            }
        }

        return bestPoint;
    }

    // ───────────────────────────────────────────
    //  소환
    // ───────────────────────────────────────────

    /// <summary>
    /// 소환 타이머를 리셋하고 다음 소환 시간을 랜덤으로 설정한다.
    /// </summary>
    private void ResetSpawnTimer()
    {
        _spawnTimer = 0f;
        _nextSpawnTime = Random.Range(spawnIntervalMin, spawnIntervalMax);
    }

    /// <summary>
    /// 장애물을 랜덤으로 하나 선택하여 GroundTile의 Point 위치에 소환한다.
    /// 장애물은 타일의 자식으로 붙어서 타일과 함께 이동한다.
    /// </summary>
    private void SpawnObstacle()
    {
        if (obstacles == null || obstacles.Length == 0)
        {
            Debug.LogWarning("[ObstacleManager] 장애물 배열이 비어있습니다!");
            return;
        }

        // GroundTile의 Point 찾기
        Transform spawnPoint = FindRightmostPoint();
        if (spawnPoint == null)
        {
            Debug.LogWarning($"[ObstacleManager] '{pointName}' 오브젝트를 찾을 수 없습니다!");
            return;
        }

        // 활성화된 장애물만 필터링
        System.Collections.Generic.List<ObstacleEntry> activeList = new System.Collections.Generic.List<ObstacleEntry>();
        for (int i = 0; i < obstacles.Length; i++)
        {
            if (obstacles[i].enabled && obstacles[i].prefab != null)
                activeList.Add(obstacles[i]);
        }

        if (activeList.Count == 0)
        {
            Debug.LogWarning("[ObstacleManager] 활성화된 장애물이 없습니다!");
            return;
        }

        // 랜덤 장애물 선택
        ObstacleEntry entry = activeList[Random.Range(0, activeList.Count)];

        if (entry.prefab == null)
        {
            return;
        }

        // Point의 좌표만 가져와서 생성 (스케일 영향 없이 ObstacleManager 자식으로)
        GameObject obstacle = Instantiate(entry.prefab, spawnPoint.position, Quaternion.identity, transform);
        obstacle.name = $"Obstacle_{entry.name}";

        // 태그 적용
        try
        {
            obstacle.tag = obstacleTag;
        }
        catch
        {
            Debug.LogWarning($"[ObstacleManager] '{obstacleTag}' 태그가 존재하지 않습니다! " +
                           "Edit → Project Settings → Tags and Layers에서 추가해주세요.");
        }

        _activeObstacles.Add(obstacle);
    }

    // ───────────────────────────────────────────
    //  이동 & 삭제
    // ───────────────────────────────────────────

    /// <summary>
    /// 장애물을 타일과 동일한 속도로 왼쪽으로 이동시키고,
    /// 화면 밖으로 나간 장애물은 삭제한다.
    /// </summary>
    private void ScrollAndCleanup()
    {
        if (tileManager == null) return;

        Camera cam = Camera.main;
        float camLeftEdge = cam.transform.position.x - cam.orthographicSize * cam.aspect;
        float destroyX = camLeftEdge - 3f;

        float delta = tileManager.scrollSpeed * Time.deltaTime;

        for (int i = _activeObstacles.Count - 1; i >= 0; i--)
        {
            GameObject obs = _activeObstacles[i];

            if (obs == null)
            {
                _activeObstacles.RemoveAt(i);
                continue;
            }

            // 타일과 같은 속도로 왼쪽 이동
            obs.transform.position += Vector3.left * delta;

            // 화면 밖 삭제
            if (obs.transform.position.x < destroyX)
            {
                Destroy(obs);
                _activeObstacles.RemoveAt(i);
            }
        }
    }
}

/// <summary>
/// 장애물 배열의 개별 항목.
/// Inspector에서 이름과 프리팹을 설정한다.
/// </summary>
[System.Serializable]
public class ObstacleEntry
{
    [Tooltip("활성화 여부")]
    public bool enabled = true;

    [Tooltip("장애물 이름 (구분용)")]
    public string name;

    [Tooltip("장애물 프리팹")]
    public GameObject prefab;
}
