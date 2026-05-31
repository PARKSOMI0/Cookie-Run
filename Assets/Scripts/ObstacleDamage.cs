using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 장애물 피격 처리 스크립트.
/// Player가 Obstacle 태그의 Trigger에 닿으면:
///   1. HP 감소
///   2. 일정 시간 속도 감소
///   3. 피격 패널 Fade In/Out
///
/// [사용법]
/// 1. Player GameObject에 이 스크립트 추가
/// 2. Inspector에서 HP, TileManager, 피격 패널 할당
/// 3. Obstacle 프리팹의 Collider2D → Is Trigger 체크 확인
/// </summary>
public class ObstacleDamage : MonoBehaviour
{
    [Header("=== 참조 할당 ===")]
    [Tooltip("Player의 HP 스크립트")]
    public HP playerHP;

    [Tooltip("속도 감소를 적용할 TileManager")]
    public TileManager tileManager;

    [Header("=== 데미지 설정 ===")]
    [Tooltip("장애물에 닿을 때 감소할 체력")]
    public float damage = 10f;

    [Tooltip("피격 후 속도 감소 배율 (0.5 = 50% 속도)")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.5f;

    [Tooltip("속도 감소 지속 시간 (초)")]
    public float slowDuration = 1.5f;

    [Header("=== 피격 판정 ===")]
    [Tooltip("장애물 태그 이름")]
    public string obstacleTag = "Obstacle";

    [Tooltip("피격 무적 시간 (초) — 연속 피격 방지")]
    public float invincibleDuration = 1.0f;

    [Header("=== 피격 이펙트 ===")]
    [Tooltip("피격 시 표시할 UI 패널 (Image 컴포넌트 필요)")]
    public Image hitPanel;

    [Tooltip("피격 패널 색상")]
    public Color hitColor = new Color(1f, 0f, 0f, 0.4f);

    [Tooltip("Fade In 시간 (초)")]
    public float fadeInDuration = 0.1f;

    [Tooltip("Fade Out 시간 (초)")]
    public float fadeOutDuration = 0.4f;

    [Header("=== 카메라 펀치 ===")]
    [Tooltip("카메라 펀치 강도")]
    public float punchIntensity = 0.3f;

    [Tooltip("카메라 펀치 지속 시간 (초)")]
    public float punchDuration = 0.15f;

    // ── 내부 변수 ──
    private bool _isInvincible = false;
    private bool _isSlowed = false;
    private float _originalSpeed;
    private Coroutine _slowCoroutine;
    private Coroutine _fadeCoroutine;
    private Coroutine _punchCoroutine;

    void Start()
    {
        // 자동 참조 찾기
        if (playerHP == null)
            playerHP = GetComponent<HP>();

        if (tileManager == null)
            tileManager = FindFirstObjectByType<TileManager>();

        // 피격 패널 초기 숨김
        if (hitPanel != null)
        {
            Color c = hitPanel.color;
            c.a = 0f;
            hitPanel.color = c;
        }

        // 원래 속도 저장
        if (tileManager != null)
            _originalSpeed = tileManager.scrollSpeed;
    }

    // ───────────────────────────────────────────
    //  충돌 감지 (Trigger + Collision 모두 처리)
    // ───────────────────────────────────────────

    /// <summary>
    /// Trigger 충돌 시 피격
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(obstacleTag)) return;
        ProcessHit();
    }

    /// <summary>
    /// 일반 충돌 시 피격
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag(obstacleTag)) return;
        ProcessHit();
    }

    /// <summary>
    /// 공통 피격 처리
    /// </summary>
    private void ProcessHit()
    {
        if (_isInvincible) return;

        ApplyDamage();
        ApplySlow();
        PlayHitEffect();
        CameraPunch();
        StartCoroutine(InvincibleTimer());

        if (SoundManager.Instance != null)
            SoundManager.Instance.피격소리재생();
    }

    // ───────────────────────────────────────────
    //  피격 처리
    // ───────────────────────────────────────────

    /// <summary>
    /// HP 감소
    /// </summary>
    private void ApplyDamage()
    {
        if (playerHP != null)
        {
            playerHP.TakeDamage(damage);
            Debug.Log($"[ObstacleDamage] 피격! 데미지: {damage}, 남은 HP: {playerHP.CurrentHP}");
        }
    }

    /// <summary>
    /// 일정 시간 동안 스크롤 속도 감소
    /// </summary>
    private void ApplySlow()
    {
        if (tileManager == null) return;

        // 이미 슬로우 중이면 리셋
        if (_slowCoroutine != null)
        {
            StopCoroutine(_slowCoroutine);
            tileManager.scrollSpeed = _originalSpeed;
        }

        _slowCoroutine = StartCoroutine(SlowRoutine());
    }

    /// <summary>
    /// 속도 감소 코루틴
    /// </summary>
    private IEnumerator SlowRoutine()
    {
        _isSlowed = true;

        // 현재 속도 기준으로 감소 (이미 슬로우가 걸려있을 수 있으므로 원래 속도 기준)
        _originalSpeed = _isSlowed ? _originalSpeed : tileManager.scrollSpeed;
        tileManager.scrollSpeed = _originalSpeed * slowMultiplier;

        yield return new WaitForSeconds(slowDuration);

        // 속도 복구
        tileManager.scrollSpeed = _originalSpeed;
        _isSlowed = false;
        _slowCoroutine = null;
    }

    /// <summary>
    /// 무적 시간 코루틴
    /// </summary>
    private IEnumerator InvincibleTimer()
    {
        _isInvincible = true;

        // 깜빡임 이펙트 (선택사항)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
        {
            float elapsed = 0f;
            while (elapsed < invincibleDuration)
            {
                sr.enabled = !sr.enabled;
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            sr.enabled = true;
        }
        else
        {
            yield return new WaitForSeconds(invincibleDuration);
        }

        _isInvincible = false;
    }

    // ───────────────────────────────────────────
    //  피격 패널 Fade 이펙트
    // ───────────────────────────────────────────

    /// <summary>
    /// 피격 패널 Fade In → Fade Out
    /// </summary>
    private void PlayHitEffect()
    {
        if (hitPanel == null) return;

        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeRoutine());
    }

    /// <summary>
    /// Fade In → Fade Out 코루틴
    /// </summary>
    private IEnumerator FadeRoutine()
    {
        // ── Fade In ──
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            SetPanelAlpha(Mathf.Lerp(0f, hitColor.a, t));
            yield return null;
        }
        SetPanelAlpha(hitColor.a);

        // ── Fade Out ──
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            SetPanelAlpha(Mathf.Lerp(hitColor.a, 0f, t));
            yield return null;
        }
        SetPanelAlpha(0f);

        _fadeCoroutine = null;
    }

    /// <summary>
    /// 패널 알파값 설정
    /// </summary>
    private void SetPanelAlpha(float alpha)
    {
        if (hitPanel == null) return;

        Color c = hitColor;
        c.a = alpha;
        hitPanel.color = c;
    }

    // ───────────────────────────────────────────
    //  카메라 펀치
    // ───────────────────────────────────────────

    /// <summary>
    /// 카메라를 순간적으로 흔들어 피격감을 준다.
    /// </summary>
    private void CameraPunch()
    {
        if (_punchCoroutine != null)
            StopCoroutine(_punchCoroutine);

        _punchCoroutine = StartCoroutine(PunchRoutine());
    }

    /// <summary>
    /// 카메라 펀치 코루틴: 랜덤 방향으로 톡 밀었다가 원래 위치로 복귀
    /// </summary>
    private IEnumerator PunchRoutine()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 originalPos = cam.transform.position;

        // 랜덤 방향으로 펀치
        Vector2 punchDir = Random.insideUnitCircle.normalized;
        Vector3 punchOffset = new Vector3(punchDir.x, punchDir.y, 0f) * punchIntensity;

        cam.transform.position = originalPos + punchOffset;

        // 부드럽게 원래 위치로 복귀
        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / punchDuration);
            // EaseOut 커브로 자연스럽게
            float ease = 1f - (1f - t) * (1f - t);
            cam.transform.position = Vector3.Lerp(originalPos + punchOffset, originalPos, ease);
            yield return null;
        }

        cam.transform.position = originalPos;
        _punchCoroutine = null;
    }
}
