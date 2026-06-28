using UnityEngine;
using UnityEngine.Rendering.Universal;

public class bling : MonoBehaviour
{
    [Header("크기 설정 (Scale)")]
    public float minScale = 0.5f;
    public float maxScale = 1.2f;

    [Header("투명도 설정 (Alpha)")]
    public float minAlpha = 0.3f;
    public float maxAlpha = 1.0f;

    [Header("변화 속도")]
    public float changeSpeed = 3f;

    [Header("낮/밤 페이드 속도")]
    [Tooltip("낮↔밤 전환 시 별이 나타나거나 사라지는 속도")]
    public float fadeSpeed = 2f;

    private float targetScale;
    private float targetAlpha;
    private SpriteRenderer spriteRenderer;
    private daynight daynightController;
    private float nightVisibility = 0f; // 0 = 낮(안보임), 1 = 밤(보임)

    // Light2D 제어용
    private Light2D[] lights;
    private float[] originalIntensities;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 자신과 자식에 있는 모든 Light2D 캐시
        lights = GetComponentsInChildren<Light2D>(true);
        originalIntensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
        {
            originalIntensities[i] = lights[i].intensity;
        }

        // 씬에서 daynight 컨트롤러 자동 검색
        daynightController = FindObjectOfType<daynight>();
        if (daynightController == null)
        {
            Debug.LogWarning("[bling] daynight 컨트롤러를 찾을 수 없습니다! 별이 항상 보입니다.");
        }

        // 초기 상태 설정: 밤이면 바로 보이고, 낮이면 안 보이게
        if (daynightController != null)
        {
            nightVisibility = daynightController.isNight ? 1f : 0f;
        }

        SetRandomTarget();

        // 초기 alpha 적용
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = nightVisibility > 0f ? targetAlpha * nightVisibility : 0f;
            spriteRenderer.color = c;
        }

        // 초기 Light2D intensity 적용
        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].intensity = originalIntensities[i] * nightVisibility;
        }
    }

    void Update()
    {
        // ── 낮/밤 상태에 따라 nightVisibility 부드럽게 전환 ──
        if (daynightController != null)
        {
            float targetVisibility = daynightController.isNight ? 1f : 0f;
            nightVisibility = Mathf.MoveTowards(nightVisibility, targetVisibility, Time.deltaTime * fadeSpeed);
        }
        else
        {
            nightVisibility = 1f; // daynight 없으면 항상 보임
        }

        // 1. 크기(Scale) 서서히 변경 — 밤 visibility에 비례
        float currentScale = transform.localScale.x;
        float effectiveTargetScale = targetScale * nightVisibility;
        float newScale = Mathf.Lerp(currentScale, effectiveTargetScale, Time.deltaTime * changeSpeed);
        transform.localScale = new Vector3(newScale, newScale, 1f);

        // 2. 반짝임(투명도, Alpha) — 밤에만 보임
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            float effectiveAlpha = targetAlpha * nightVisibility;
            c.a = Mathf.Lerp(c.a, effectiveAlpha, Time.deltaTime * changeSpeed);
            spriteRenderer.color = c;
        }

        // 3. Light2D intensity — 밤에만 보임
        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].intensity = Mathf.Lerp(lights[i].intensity,
                originalIntensities[i] * nightVisibility,
                Time.deltaTime * changeSpeed);
        }

        // 4. 현재 크기가 목표치에 거의 도달했다면, 새로운 랜덤 목표치를 설정
        if (Mathf.Abs(newScale - effectiveTargetScale) < 0.05f)
        {
            SetRandomTarget();
        }
    }

    void SetRandomTarget()
    {
        targetScale = Random.Range(minScale, maxScale);
        targetAlpha = Random.Range(minAlpha, maxAlpha);
    }
}


