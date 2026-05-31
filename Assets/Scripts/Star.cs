using UnityEngine;

/// <summary>
/// 별 트랩 컨트롤러.
/// Player가 Button Anchor(Trigger)에 닿으면,
/// Star 프리팹을 Start Anchor에 스폰하고
/// End Anchor까지 회전하며 가속 낙하/이동시킨다.
/// End Anchor에 도달하면 회전 및 이동이 정지한다.
///
/// [사용법 — 장애물 프리팹 내부 구성]
/// 
///  Obstacle (부모) ← 이 스크립트 추가
///   ├─ ButtonAnchor   ← Collider2D (Is Trigger ✓), 플레이어 접촉 감지
///   ├─ StartAnchor    ← 별 스폰/시작 위치 (빈 오브젝트)
///   └─ EndAnchor      ← 별 도착 위치 (빈 오브젝트)
///
/// 1. 부모 오브젝트에 이 스크립트 추가
/// 2. Inspector에서 Star Prefab + 3개 앵커 할당
/// 3. Play!
/// </summary>
public class Star : MonoBehaviour
{
    [Header("=== 프리팹 할당 ===")]
    [Tooltip("낙하할 별 프리팹 (SpriteRenderer + Collider2D 등)")]
    public GameObject starPrefab;

    [Header("=== 앵커 할당 ===")]
    [Tooltip("플레이어가 닿으면 별이 활성화되는 트리거")]
    public Transform buttonAnchor;

    [Tooltip("별 스폰/시작 위치")]
    public Transform startAnchor;

    [Tooltip("별 도착 위치")]
    public Transform endAnchor;

    [Header("=== 이동 설정 ===")]
    [Tooltip("이동 소요 시간 (초)")]
    public float moveDuration = 2.0f;

    [Tooltip("가속도 커브 (0→1, 기본: EaseIn 가속)")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("=== 회전 설정 ===")]
    [Tooltip("초당 회전 속도 (도)")]
    public float rotateSpeed = 360f;

    [Header("=== 플레이어 감지 ===")]
    [Tooltip("플레이어 태그")]
    public string playerTag = "Player";

    [Header("=== 상태 (읽기 전용) ===")]
    [SerializeField] private bool _isActivated = false;
    [SerializeField] private bool _isMoving = false;

    // ── 내부 변수 ──
    private float _moveTimer = 0f;
    private ButtonTriggerRelay _buttonRelay;
    private GameObject _starInstance; // 스폰된 별 인스턴스

    void Start()
    {
        // Button Anchor에 트리거 감지용 릴레이 추가
        if (buttonAnchor != null)
        {
            _buttonRelay = buttonAnchor.GetComponent<ButtonTriggerRelay>();
            if (_buttonRelay == null)
            {
                _buttonRelay = buttonAnchor.gameObject.AddComponent<ButtonTriggerRelay>();
            }
            _buttonRelay.Initialize(this, playerTag);
        }
        else
        {
            Debug.LogWarning("[Star] ButtonAnchor가 할당되지 않았습니다!");
        }
    }

    void Update()
    {
        if (!_isMoving || _starInstance == null) return;

        // ── 회전 ──
        _starInstance.transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

        // ── 이동 (가속 커브 적용) ──
        _moveTimer += Time.deltaTime;
        float t = Mathf.Clamp01(_moveTimer / moveDuration);

        // AnimationCurve로 가속도 적용
        float curveT = moveCurve.Evaluate(t);

        if (startAnchor != null && endAnchor != null)
        {
            _starInstance.transform.position = Vector3.Lerp(
                startAnchor.position, endAnchor.position, curveT);
        }

        // 도착 판정
        if (t >= 1f)
        {
            OnReachEnd();
        }
    }

    // ───────────────────────────────────────────
    //  활성화 / 정지
    // ───────────────────────────────────────────

    /// <summary>
    /// 버튼 트리거에 의해 별 이동 시작
    /// </summary>
    public void Activate()
    {
        if (_isActivated) return;

        _isActivated = true;
        _moveTimer = 0f;

        // Star 프리팹 스폰
        if (starPrefab != null && startAnchor != null)
        {
            _starInstance = Instantiate(starPrefab, startAnchor.position, Quaternion.identity, transform);
            _isMoving = true;
            Debug.Log("[Star] 별 스폰 및 이동 시작!");
        }
        else
        {
            Debug.LogWarning("[Star] starPrefab 또는 startAnchor가 할당되지 않았습니다!");
        }
    }

    /// <summary>
    /// End Anchor 도달 시 정지
    /// </summary>
    private void OnReachEnd()
    {
        _isMoving = false;

        // 최종 위치 고정
        if (_starInstance != null && endAnchor != null)
        {
            _starInstance.transform.position = endAnchor.position;
        }

        Debug.Log("[Star] 별 도착! 이동/회전 정지");
    }
}

/// <summary>
/// Button Anchor에 붙는 트리거 감지 릴레이.
/// 플레이어가 닿으면 Star.Activate()를 호출한다.
/// Star 스크립트가 런타임에 자동으로 추가한다.
/// </summary>
public class ButtonTriggerRelay : MonoBehaviour
{
    private Star _star;
    private string _playerTag;

    public void Initialize(Star star, string playerTag)
    {
        _star = star;
        _playerTag = playerTag;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_star == null) return;
        if (!other.CompareTag(_playerTag)) return;

        _star.Activate();
    }
}
