using UnityEngine;

/// <summary>
/// 쿠키런 스타일 점프 스크립트.
/// Rigidbody2D가 Kinematic이므로 물리 힘 대신 직접 위치를 제어한다.
/// 스페이스바로 점프, 2단 점프까지 가능.
/// 
/// [사용법]
/// 1. 쿠키 GameObject에 이 스크립트 추가
/// 2. Rigidbody2D → Body Type = Kinematic 확인
/// 3. Inspector에서 jumpForce, gravity, groundY 등 조절
/// </summary>
public class CookieJump : MonoBehaviour
{
    [Header("=== 점프 설정 ===")]
    [Tooltip("점프 시 위로 올라가는 힘 (초기 속도)")]
    public float jumpForce = 12f;

    [Tooltip("중력 가속도 (양수 값, 아래로 당김)")]
    public float gravity = 30f;

    [Tooltip("최대 점프 횟수 (2 = 2단 점프)")]
    public int maxJumpCount = 2;

    [Header("=== 바닥 설정 ===")]
    [Tooltip("바닥 Y 좌표 (이 아래로 떨어지지 않음)")]
    public float groundY = -3f;

    [Tooltip("바닥 판정을 위한 오차 범위")]
    public float groundThreshold = 0.05f;

    [Header("=== 상태 (읽기 전용) ===")]
    [SerializeField] private bool _isGrounded = true;
    [SerializeField] private int _currentJumpCount = 0;

    // ── 내부 변수 ──
    private float _velocityY = 0f;
    private Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (_rb == null)
        {
            Debug.LogError("[CookieJump] Rigidbody2D가 없습니다! 추가해주세요.");
            return;
        }

        if (!_rb.isKinematic)
        {
            Debug.LogWarning("[CookieJump] Rigidbody2D를 Kinematic으로 설정합니다.");
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    void Start()
    {
        // 시작 위치를 groundY로 초기화 (필요 시)
        if (transform.position.y < groundY)
        {
            transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        }
        groundY = transform.position.y;
    }

    void Update()
    {
        HandleInput();
        ApplyGravity();
        CheckGround();
    }

    // ───────────────────────────────────────────
    //  입력 처리
    // ───────────────────────────────────────────

    /// <summary>
    /// 스페이스바 입력 시 점프를 실행한다.
    /// </summary>
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryJump();
        }
    }

    /// <summary>
    /// 점프 횟수가 남아있으면 점프한다.
    /// </summary>
    private void TryJump()
    {
        if (_currentJumpCount < maxJumpCount)
        {
            _velocityY = jumpForce;
            _currentJumpCount++;
            _isGrounded = false;
        }
    }

    // ───────────────────────────────────────────
    //  물리 시뮬레이션 (Kinematic 직접 제어)
    // ───────────────────────────────────────────

    /// <summary>
    /// 매 프레임 중력을 적용하고 위치를 갱신한다.
    /// </summary>
    private void ApplyGravity()
    {
        if (_isGrounded && _velocityY <= 0f) return;

        // 중력 적용
        _velocityY -= gravity * Time.deltaTime;

        // 위치 갱신
        Vector3 pos = transform.position;
        pos.y += _velocityY * Time.deltaTime;

        // 바닥 클램프
        if (pos.y <= groundY)
        {
            pos.y = groundY;
            _velocityY = 0f;
        }

        transform.position = pos;
    }

    /// <summary>
    /// 바닥에 닿았는지 확인하고 점프 횟수를 초기화한다.
    /// </summary>
    private void CheckGround()
    {
        if (transform.position.y <= groundY + groundThreshold && _velocityY <= 0f)
        {
            _isGrounded = true;
            _currentJumpCount = 0;
        }
        else
        {
            _isGrounded = false;
        }
    }

    // ───────────────────────────────────────────
    //  외부 API
    // ───────────────────────────────────────────

    /// <summary>
    /// 바닥에 있는지 여부
    /// </summary>
    public bool IsGrounded => _isGrounded;

    /// <summary>
    /// 외부에서 바닥 Y 좌표를 동적으로 변경할 때 사용
    /// </summary>
    public void SetGroundY(float newGroundY)
    {
        groundY = newGroundY;
    }
}
