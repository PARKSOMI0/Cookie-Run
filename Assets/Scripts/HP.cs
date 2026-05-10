using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 쿠키의 체력(HP) 관리 스크립트.
/// UI Slider와 동기화하며, 1초에 1씩 자동 감소한다.
///
/// [사용법]
/// 1. 쿠키 GameObject에 이 스크립트 추가
/// 2. Canvas > Slider를 만들어서 hpSlider에 할당
/// 3. Play!
/// </summary>
public class HP : MonoBehaviour
{
    [Header("=== UI 할당 ===")]
    [Tooltip("체력 표시용 UI Slider")]
    public Slider hpSlider;

    [Header("=== 체력 설정 ===")]
    [Tooltip("최대 체력")]
    public float maxHP = 100f;

    [Tooltip("초당 감소량")]
    public float drainPerSecond = 1f;

    [Header("=== 상태 (읽기 전용) ===")]
    [SerializeField] private float _currentHP;

    /// <summary>
    /// 현재 체력 (외부 읽기용)
    /// </summary>
    public float CurrentHP => _currentHP;

    /// <summary>
    /// 사망 여부
    /// </summary>
    public bool IsDead => _currentHP <= 0f;

    void Start()
    {
        _currentHP = maxHP;

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = maxHP;
            hpSlider.value = _currentHP;
        }
    }

    void Update()
    {
        if (IsDead) return;

        // 1초에 drainPerSecond만큼 감소
        _currentHP -= drainPerSecond * Time.deltaTime;
        _currentHP = Mathf.Clamp(_currentHP, 0f, maxHP);

        // 슬라이더 동기화
        SyncSlider();

        if (IsDead)
        {
            OnDeath();
        }
    }

    // ───────────────────────────────────────────
    //  슬라이더 동기화
    // ───────────────────────────────────────────

    private void SyncSlider()
    {
        if (hpSlider != null)
        {
            hpSlider.value = _currentHP;
        }
    }

    // ───────────────────────────────────────────
    //  외부 API
    // ───────────────────────────────────────────

    /// <summary>
    /// 데미지를 받는다.
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        _currentHP -= amount;
        _currentHP = Mathf.Clamp(_currentHP, 0f, maxHP);
        SyncSlider();

        if (IsDead)
        {
            OnDeath();
        }
    }

    /// <summary>
    /// 체력을 회복한다.
    /// </summary>
    public void Heal(float amount)
    {
        if (IsDead) return;

        _currentHP += amount;
        _currentHP = Mathf.Clamp(_currentHP, 0f, maxHP);
        SyncSlider();
    }

    /// <summary>
    /// 체력을 최대로 회복한다.
    /// </summary>
    public void FullHeal()
    {
        _currentHP = maxHP;
        SyncSlider();
    }

    // ───────────────────────────────────────────
    //  사망 처리
    // ───────────────────────────────────────────

    private void OnDeath()
    {
        Debug.Log($"[HP] {gameObject.name} 사망!");
        // TODO: 게임 오버 로직 연결
    }
}
