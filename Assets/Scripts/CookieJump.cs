using UnityEngine;

/// <summary>
/// 쿠키런 스타일 점프 + 슬라이딩 스크립트.
/// Rigidbody2D (Dynamic) + Collider2D 물리 충돌 기반.
/// Floor 태그가 붙은 바닥에 닿으면 점프 횟수 초기화.
/// 스페이스바로 점프, 2단 점프까지 가능.
/// 아래 방향키로 슬라이딩 (콜라이더 축소).
///
/// [필수 설정]
/// - Player: Rigidbody2D (Dynamic) + BoxCollider2D + Tag "Player"
/// - Floor:  Rigidbody2D (Static)  + Collider2D + Tag "Floor"
///
/// [사용법]
/// 1. 쿠키 GameObject에 이 스크립트 추가
/// 2. Inspector에서 jumpForce, slideKey 등 조절
/// </summary>
public class CookieJump : MonoBehaviour
{
    [Header("=== 점프 설정 ===")]
    [Tooltip("점프 힘 (Rigidbody에 가해지는 순간 힘)")]
    public float jumpForce = 14f;

    [Tooltip("최대 점프 횟수 (2 = 2단 점프)")]
    public int maxJumpCount = 2;

    [Header("=== 슬라이딩 설정 ===")]
    [Tooltip("슬라이딩 키")]
    public KeyCode slideKey = KeyCode.DownArrow;

    [Tooltip("슬라이딩 시 콜라이더 높이 배율 (0.5 = 절반)")]
    [Range(0.1f, 0.9f)]
    public float slideColliderRatio = 0.5f;

    [Header("=== 바닥 감지 설정 ===")]
    [Tooltip("바닥으로 인식할 태그 이름")]
    public string floorTag = "Floor";

    [Header("=== 상태 (읽기 전용) ===")]
    [SerializeField] private bool _isGrounded = false;
    [SerializeField] private bool _isSliding = false;
    [SerializeField] private int _currentJumpCount = 0;

    // ── 내부 변수 ──
    private Rigidbody2D _rb;
    private BoxCollider2D _col;
    private int _floorContactCount = 0;

    // 원래 콜라이더 정보 저장
    private Vector2 _originalColSize;
    private Vector2 _originalColOffset;

    // 원래 스프라이트 스케일 저장
    private Transform _spriteTransform;
    private Vector3 _originalSpriteScale;

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

        // BoxCollider2D 참조
        _col = GetComponent<BoxCollider2D>();
        if (_col != null)
        {
            _originalColSize = _col.size;
            _originalColOffset = _col.offset;
        }

        // 자식 스프라이트 참조 (시각적 슬라이딩용)
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            _spriteTransform = sr.transform;
            _originalSpriteScale = _spriteTransform.localScale;
        }
    }

    void Update()
    {
        HandleInput();
    }

    // ───────────────────────────────────────────
    //  입력 처리
    // ───────────────────────────────────────────

    /// <summary>
    /// 점프와 슬라이딩 입력을 처리한다.
    /// </summary>
    private void HandleInput()
    {
        // 점프 (슬라이딩 중에는 점프 불가)
        if (Input.GetKeyDown(KeyCode.Space) && !_isSliding)
        {
            TryJump();
        }

        // 슬라이딩 (바닥에 있을 때만)
        if (Input.GetKey(slideKey) && _isGrounded)
        {
            StartSlide();
        }
        else if (_isSliding)
        {
            EndSlide();
        }
    }

    private void TryJump()
    {
        if (_currentJumpCount < maxJumpCount)
        {
            // 기존 Y 속도를 리셋하고 점프 힘 적용
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
            _rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            _currentJumpCount++;
            _isGrounded = false;

            if (SoundManager.Instance != null)
                SoundManager.Instance.점프소리재생();
        }
    }

    // ───────────────────────────────────────────
    //  슬라이딩
    // ───────────────────────────────────────────

    /// <summary>
    /// 슬라이딩 시작: 콜라이더 축소 + 스프라이트 납작하게
    /// </summary>
    private void StartSlide()
    {
        if (_isSliding) return;
        _isSliding = true;

        if (SoundManager.Instance != null)
            SoundManager.Instance.슬라이딩소리재생();

        if (_col != null)
        {
            // 콜라이더 높이 줄이기 (하단 기준 유지)
            float newHeight = _originalColSize.y * slideColliderRatio;
            float heightDiff = _originalColSize.y - newHeight;

            _col.size = new Vector2(_originalColSize.x, newHeight);
            _col.offset = new Vector2(_originalColOffset.x, _originalColOffset.y - heightDiff * 0.5f);
        }

        // 스프라이트 시각적 납작하게
        if (_spriteTransform != null)
        {
            _spriteTransform.localScale = new Vector3(
                _originalSpriteScale.x,
                _originalSpriteScale.y * slideColliderRatio,
                _originalSpriteScale.z
            );
        }
    }

    /// <summary>
    /// 슬라이딩 종료: 콜라이더/스프라이트 원래대로 복구
    /// </summary>
    private void EndSlide()
    {
        if (!_isSliding) return;
        _isSliding = false;

        if (_col != null)
        {
            _col.size = _originalColSize;
            _col.offset = _originalColOffset;
        }

        if (_spriteTransform != null)
        {
            _spriteTransform.localScale = _originalSpriteScale;
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

            // 공중에서는 슬라이딩 해제
            if (!_isGrounded && _isSliding)
            {
                EndSlide();
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

    /// <summary>
    /// 슬라이딩 중인지 여부
    /// </summary>
    public bool IsSliding => _isSliding;
}
