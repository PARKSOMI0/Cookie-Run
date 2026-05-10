using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 쿠키런 스타일 러닝게임의 타일 매니저.
/// 타일 프리팹을 촘촘하게 너비에 맞춰 이어 붙이고,
/// 그라운드 프리팹에서 Y축 오프셋을 가져와 위치를 잡는다.
/// 
/// ★ Edit 모드에서도 타일 프리뷰가 보인다.
///   Inspector 값 변경 시 자동으로 프리뷰가 갱신된다.
/// 
/// [사용법]
/// 1. 빈 GameObject에 이 스크립트를 추가
/// 2. tilePrefab  — 반복 생성할 타일 프리팹 할당
/// 3. groundPrefab — Y축 오프셋 참조용 그라운드 프리팹 할당
/// 4. Play!
/// </summary>
[ExecuteInEditMode]
public class TileManager : MonoBehaviour
{
    [Header("=== 프리팹 할당 ===")]
    [Tooltip("반복 생성할 타일 프리팹")]
    public GameObject tilePrefab;

    [Tooltip("Y축 오프셋 참조용 그라운드 프리팹 (SpriteRenderer 필요)")]
    public GameObject groundPrefab;

    [Header("=== 타일 설정 ===")]
    [Tooltip("화면에 유지할 타일 수 (앞쪽 버퍼 포함)")]
    [Range(5, 30)]
    public int maxTileCount = 15;

    [Tooltip("맵 스크롤 속도 (Units/sec)")]
    public float scrollSpeed = 5f;

    [Header("=== 디버그 ===")]
    [Tooltip("타일 너비를 수동 지정 (0이면 SpriteRenderer에서 자동 계산)")]
    public float overrideTileWidth = 0f;

    [Tooltip("Edit 모드 프리뷰 활성화")]
    public bool showPreview = true;

    // ── 내부 변수 ──
    private float _tileWidth;       // 타일 하나의 너비
    private float _groundYOffset;   // 그라운드 프리팹에서 가져온 Y 오프셋
    private float _nextSpawnX;      // 다음 타일 생성 위치
    private Queue<GameObject> _activeTiles = new Queue<GameObject>();

    // Edit 모드 프리뷰용
    private List<GameObject> _previewTiles = new List<GameObject>();
    private int _lastPreviewCount = -1;
    private float _lastPreviewWidth = -1f;
    private float _lastPreviewYOffset = float.NaN;
    private GameObject _lastTilePrefab;
    private GameObject _lastGroundPrefab;

    // ───────────────────────────────────────────
    //  라이프사이클
    // ───────────────────────────────────────────

    void Start()
    {
        if (Application.isPlaying)
        {
            // 플레이 모드 진입 시 프리뷰 정리
            ClearPreviewTiles();

            CalculateTileWidth();
            CalculateGroundYOffset();
            SpawnInitialTiles();
        }
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            // 플레이 모드: 스크롤 + 재활용
            ScrollTiles();
            RecycleAndSpawn();
        }
        else
        {
            // Edit 모드: 프리뷰만 갱신
            if (showPreview)
            {
                RefreshPreviewIfNeeded();
            }
            else
            {
                ClearPreviewTiles();
            }
        }
    }

    void OnDestroy()
    {
        // 스크립트 제거 시 프리뷰 정리
        if (!Application.isPlaying)
        {
            ClearPreviewTiles();
        }
    }

    void OnDisable()
    {
        if (!Application.isPlaying)
        {
            ClearPreviewTiles();
        }
    }

    void OnValidate()
    {
        // Inspector 값 변경 시 프리뷰 강제 갱신
        if (!Application.isPlaying)
        {
            _lastPreviewCount = -1; // 강제 갱신 트리거
        }
    }

    // ───────────────────────────────────────────
    //  Edit 모드 프리뷰
    // ───────────────────────────────────────────

    /// <summary>
    /// Inspector 값이 바뀌었을 때만 프리뷰를 다시 생성한다.
    /// </summary>
    private void RefreshPreviewIfNeeded()
    {
        if (tilePrefab == null) 
        {
            ClearPreviewTiles();
            return;
        }

        CalculateTileWidth();
        CalculateGroundYOffset();

        // 값이 변하지 않았으면 스킵
        bool changed = _lastPreviewCount != maxTileCount
                     || !Mathf.Approximately(_lastPreviewWidth, _tileWidth)
                     || !Mathf.Approximately(_lastPreviewYOffset, _groundYOffset)
                     || _lastTilePrefab != tilePrefab
                     || _lastGroundPrefab != groundPrefab;

        if (!changed) return;

        RebuildPreview();
    }

    /// <summary>
    /// 프리뷰 타일을 전부 삭제 후 다시 생성한다.
    /// </summary>
    private void RebuildPreview()
    {
        ClearPreviewTiles();

        if (tilePrefab == null || _tileWidth <= 0f) return;

        // Scene 카메라 또는 Main 카메라 기준
        Camera cam = Camera.main;
        float startX;

        if (cam != null && cam.orthographic)
        {
            startX = cam.transform.position.x - cam.orthographicSize * cam.aspect;
        }
        else
        {
            // 카메라가 없으면 이 오브젝트 위치 기준
            startX = transform.position.x;
        }

        float spawnX = startX;

        for (int i = 0; i < maxTileCount; i++)
        {
            float posX = spawnX + _tileWidth * 0.5f;
            Vector3 pos = new Vector3(posX, _groundYOffset, 0f);

#if UNITY_EDITOR
            GameObject tile = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(tilePrefab, transform);
            tile.transform.position = pos;
            tile.transform.rotation = Quaternion.identity;
#else
            GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
#endif
            tile.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            tile.name = $"[Preview] Tile_{i}";
            _previewTiles.Add(tile);

            spawnX += _tileWidth;
        }

        // 캐시 갱신
        _lastPreviewCount = maxTileCount;
        _lastPreviewWidth = _tileWidth;
        _lastPreviewYOffset = _groundYOffset;
        _lastTilePrefab = tilePrefab;
        _lastGroundPrefab = groundPrefab;
    }

    /// <summary>
    /// 프리뷰 타일을 모두 삭제한다.
    /// </summary>
    private void ClearPreviewTiles()
    {
        foreach (GameObject tile in _previewTiles)
        {
            if (tile != null)
            {
                DestroyImmediate(tile);
            }
        }
        _previewTiles.Clear();

        // 혹시 남아있는 자식 프리뷰도 정리
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (child.name.StartsWith("[Preview]"))
            {
                DestroyImmediate(child);
            }
        }

        _lastPreviewCount = -1;
    }

    // ───────────────────────────────────────────
    //  초기화
    // ───────────────────────────────────────────

    /// <summary>
    /// 타일 프리팹의 SpriteRenderer bounds에서 너비를 계산한다.
    /// overrideTileWidth > 0 이면 수동 값을 사용한다.
    /// </summary>
    private void CalculateTileWidth()
    {
        if (overrideTileWidth > 0f)
        {
            _tileWidth = overrideTileWidth;
            return;
        }

        if (tilePrefab == null)
        {
            Debug.LogError("[TileManager] tilePrefab이 할당되지 않았습니다!");
            return;
        }

        // SpriteRenderer에서 가져오기
        SpriteRenderer sr = tilePrefab.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = tilePrefab.GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
        {
            _tileWidth = sr.bounds.size.x;
        }
        else
        {
            // Renderer fallback
            Renderer rend = tilePrefab.GetComponent<Renderer>();
            if (rend == null)
                rend = tilePrefab.GetComponentInChildren<Renderer>();

            if (rend != null)
            {
                _tileWidth = rend.bounds.size.x;
            }
            else
            {
                Debug.LogWarning("[TileManager] 타일 프리팹에 Renderer가 없어 기본 너비 1을 사용합니다.");
                _tileWidth = 1f;
            }
        }
    }

    /// <summary>
    /// 그라운드 프리팹의 Transform.position.y를 Y 오프셋으로 사용한다.
    /// </summary>
    private void CalculateGroundYOffset()
    {
        if (groundPrefab == null)
        {
            _groundYOffset = 0f;
            return;
        }

        // 그라운드 프리팹의 Y 위치를 오프셋으로 사용
        _groundYOffset = groundPrefab.transform.position.y;
    }

    /// <summary>
    /// 시작 시 화면을 채울 만큼 타일을 촘촘하게 생성한다.
    /// </summary>
    private void SpawnInitialTiles()
    {
        // 카메라 왼쪽 끝에서 시작
        Camera cam = Camera.main;
        float camLeftEdge = cam.transform.position.x - cam.orthographicSize * cam.aspect;

        _nextSpawnX = camLeftEdge;

        for (int i = 0; i < maxTileCount; i++)
        {
            SpawnTile();
        }
    }

    // ───────────────────────────────────────────
    //  타일 생성 / 재활용 (플레이 모드 전용)
    // ───────────────────────────────────────────

    /// <summary>
    /// 타일 하나를 _nextSpawnX 위치에 생성하고, 큐에 등록한다.
    /// 타일 Pivot이 중앙이라고 가정하고 절반 너비만큼 오프셋 적용.
    /// </summary>
    private void SpawnTile()
    {
        // 중앙 피벗 기준: 스프라이트의 왼쪽 끝이 _nextSpawnX에 오도록
        float spawnX = _nextSpawnX + _tileWidth * 0.5f;

        Vector3 pos = new Vector3(spawnX, _groundYOffset, 0f);
        GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
        _activeTiles.Enqueue(tile);

        // 다음 타일 위치는 현재 타일의 오른쪽 끝
        _nextSpawnX += _tileWidth;
    }

    /// <summary>
    /// 모든 활성 타일을 왼쪽으로 스크롤한다 (러닝게임 방식).
    /// </summary>
    private void ScrollTiles()
    {
        float delta = scrollSpeed * Time.deltaTime;
        _nextSpawnX -= delta;

        foreach (GameObject tile in _activeTiles)
        {
            if (tile != null)
            {
                tile.transform.position += Vector3.left * delta;
            }
        }
    }

    /// <summary>
    /// 화면 왼쪽 밖으로 나간 타일을 삭제하고 오른쪽에 새 타일을 생성한다.
    /// </summary>
    private void RecycleAndSpawn()
    {
        if (_activeTiles.Count == 0) return;

        Camera cam = Camera.main;
        float camLeftEdge = cam.transform.position.x - cam.orthographicSize * cam.aspect;
        float destroyThreshold = camLeftEdge - _tileWidth;

        // 큐 앞쪽(가장 왼쪽) 타일이 화면 밖이면 제거
        while (_activeTiles.Count > 0)
        {
            GameObject oldest = _activeTiles.Peek();

            if (oldest == null)
            {
                _activeTiles.Dequeue();
                continue;
            }

            if (oldest.transform.position.x < destroyThreshold)
            {
                _activeTiles.Dequeue();
                Destroy(oldest);

                // 오른쪽에 새 타일 추가
                SpawnTile();
            }
            else
            {
                break;
            }
        }
    }
}
