using UnityEngine;

/// <summary>
/// 쿠키런 스타일 점프 스크립트.
/// Rigidbody2D (Dynamic) + Collider2D 물리 충돌 기반.
/// Floor 태그가 붙은 바닥에 닿으면 점프 횟수 초기화.
/// 스페이스바로 점프, 2단 점프까지 가능.
///
/// [필수 설정]
/// - Player: Rigidbody2D (Dynamic) + Collider2D + Tag "Player"
/// - Floor:  Rigidbody2D (Static)  + Collider2D + Tag "Floor"
///
/// [사용법]
/// 1. 쿠키 GameObject에 이 스크립트 추가
/// 2. Inspector에서 jumpForce 등 조절
/// </summary>
public class CookieJump : MonoBehaviour
{
    [Header("=== 점프 설정 ===")]
    [Tooltip("점프 힘 (Rigidbody에 가해지는 순간 힘)")]
    public float jumpForce = 14f;

    [Tooltip("최대 점프 횟수 (2 = 2단 점프)")]
    public int maxJumpCount = 2;

    [Header("=== 바닥 감지 설정 ===")]
    [Tooltip("바닥으로 인식할 태그 이름")]
    public string floorTag = "Floor";

    [Header("=== 상태 (읽기 전용) ===")]
    [SerializeField] private bool _isGrounded = false;
    [SerializeField] private int _currentJumpCount = 0;

    // ── 내부 변수 ──
    private Rigidbody2D _rb;
    private int _floorContactCount = 0; // 현재 닿아있는 Floor 수

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (_rb == null)
        {
            Debug.LogError("[CookieJump] Rigidbody2D가 없습니다! 추가해주세요.");
            return;
        }

        // Dynamic 타입 확인
        if (_rb.bodyType != RigidbodyType2D.Dynamic)
        {
            Debug.LogWarning("[CookieJump] Rigidbody2D를 Dynamic으로 설정합니다.");
            _rb.bodyType = RigidbodyType2D.Dynamic;
        }

        // 회전 방지 (쿠키가 굴러가지 않도록)
        _rb.freezeRotation = true;
    }

    void Update()
    {
        HandleInput();
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
            // 기존 Y 속도를 리셋하고 점프 힘 적용
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
            _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            _currentJumpCount++;
            _isGrounded = false;
        }
    }

    // ───────────────────────────────────────────
    //  충돌 감지 (Floor 태그 기반)
    // ───────────────────────────────────────────

    /// <summary>
    /// Floor에 닿는 순간 → 점프 횟수 초기화, 착지 상태
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(floorTag))
        {
            _floorContactCount++;
            _isGrounded = true;
            _currentJumpCount = 0;
        }
    }

    /// <summary>
    /// Floor에 계속 닿아있는 동안 착지 상태 유지
    /// </summary>
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(floorTag))
        {
            _isGrounded = true;
        }
    }

    /// <summary>
    /// Floor에서 떨어지면 → 공중 상태
    /// </summary>
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(floorTag))
        {
            _floorContactCount--;
            if (_floorContactCount <= 0)
            {
                _floorContactCount = 0;
                _isGrounded = false;
            }
        }
    }

    // ───────────────────────────────────────────
    //  외부 API
    // ───────────────────────────────────────────

    /// <summary>
    /// 바닥에 있는지 여부
    /// </summary>
    public bool IsGrounded => _isGrounded;
}
