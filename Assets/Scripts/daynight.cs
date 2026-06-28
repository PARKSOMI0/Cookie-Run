using UnityEngine;

public class daynight : MonoBehaviour
{
    [Header("낮/밤 유지 시간 설정 (초)")]
    [Tooltip("낮 상태가 유지되는 시간")]
    public float dayDuration = 10f;
    [Tooltip("밤 상태가 유지되는 시간")]
    public float nightDuration = 10f;

    [Tooltip("현재 상태 (테스트용으로 직접 체크해볼 수 있습니다)")]
    public bool isNight = false;

    [Header("낮 배경 그라데이션 (위/아래)")]
    public Color dayTopColor = new Color(0.53f, 0.81f, 0.92f, 1f);     // 밝은 하늘색
    public Color dayBottomColor = new Color(0.85f, 0.95f, 1f, 1f);     // 연한 하늘색

    [Header("밤 배경 그라데이션 (위/아래)")]
    public Color nightTopColor = new Color(0.02f, 0.02f, 0.12f, 1f);   // 짙은 남색
    public Color nightBottomColor = new Color(0.08f, 0.06f, 0.25f, 1f); // 어두운 보라

    [Header("낮↔밤 전환 시간 (초)")]
    public float transitionDuration = 2f;

    [Header("배경 오브젝트 (선택사항 - 비우면 자동 검색)")]
    [Tooltip("색상을 적용할 배경 SpriteRenderer를 직접 할당할 수 있습니다")]
    public SpriteRenderer targetSpriteRenderer;

    // 내부 변수
    private Texture2D gradientTexture;
    private float timer = 0f;
    private float transitionTimer = 0f;
    private bool isTransitioning = false;
    private Color prevTopColor, prevBottomColor;
    private Color lastAppliedTop, lastAppliedBottom;

    private const int TEX_HEIGHT = 32;

    void Start()
    {
        // SpriteRenderer 찾기: 직접 할당 → 자신 → 자식 순서
        if (targetSpriteRenderer == null)
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
        if (targetSpriteRenderer == null)
            targetSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (targetSpriteRenderer == null)
        {
            Debug.LogError("[daynight] SpriteRenderer를 찾을 수 없습니다! 배경 오브젝트를 할당해주세요.");
            return;
        }

        // 원본 스프라이트 크기 정보 저장
        Sprite origSprite = targetSpriteRenderer.sprite;
        Vector2 origBoundsSize = origSprite != null
            ? origSprite.bounds.size
            : new Vector2(20f, 12f);

        // ── 그라데이션 텍스처 생성 (1 x 32 픽셀) ──
        gradientTexture = new Texture2D(1, TEX_HEIGHT, TextureFormat.RGBA32, false);
        gradientTexture.filterMode = FilterMode.Bilinear;
        gradientTexture.wrapMode = TextureWrapMode.Clamp;

        // PPU를 계산하여 높이가 원본과 동일하게 맞춤
        // 스프라이트 높이 = texHeight / ppu = origBoundsSize.y  →  ppu = texHeight / origBoundsSize.y
        float ppu = TEX_HEIGHT / origBoundsSize.y;

        // 스프라이트 생성
        Sprite gradientSprite = Sprite.Create(
            gradientTexture,
            new Rect(0, 0, 1, TEX_HEIGHT),
            new Vector2(0.5f, 0.5f),
            ppu
        );

        // X 스케일 보정 (텍스처가 1픽셀 너비이므로 원본 너비에 맞춤)
        // 새 스프라이트 너비 = 1 / ppu, 원본 너비 = origBoundsSize.x
        float newWidth = 1f / ppu;
        float xScaleFactor = origBoundsSize.x / newWidth;

        Vector3 scale = targetSpriteRenderer.transform.localScale;
        scale.x *= xScaleFactor;
        targetSpriteRenderer.transform.localScale = scale;

        // 스프라이트 교체 (기본 Sprites 머테리얼을 그대로 유지 → 2D 정렬 시스템 정상 작동)
        targetSpriteRenderer.sprite = gradientSprite;
        targetSpriteRenderer.color = Color.white;

        // 커스텀 셰이더 머테리얼이 남아있을 경우 기본 스프라이트 셰이더로 복원
        if (targetSpriteRenderer.sharedMaterial != null &&
            targetSpriteRenderer.sharedMaterial.shader.name == "Custom/VerticalGradient")
        {
            Shader defaultShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (defaultShader == null)
                defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader != null)
                targetSpriteRenderer.material = new Material(defaultShader);
        }

        // 초기 색상 적용
        Color initTop = isNight ? nightTopColor : dayTopColor;
        Color initBottom = isNight ? nightBottomColor : dayBottomColor;
        UpdateGradientTexture(initTop, initBottom);

        prevTopColor = initTop;
        prevBottomColor = initBottom;
    }

    void Update()
    {
        if (gradientTexture == null) return;

        timer += Time.deltaTime;
        float currentCycleDuration = isNight ? nightDuration : dayDuration;

        // 낮/밤 전환 시점
        if (timer >= currentCycleDuration)
        {
            prevTopColor = isNight ? nightTopColor : dayTopColor;
            prevBottomColor = isNight ? nightBottomColor : dayBottomColor;

            isNight = !isNight;
            timer = 0f;
            transitionTimer = 0f;
            isTransitioning = true;
        }

        // 현재 목표 색상
        Color targetTop = isNight ? nightTopColor : dayTopColor;
        Color targetBottom = isNight ? nightBottomColor : dayBottomColor;

        Color finalTop, finalBottom;

        if (isTransitioning)
        {
            transitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(transitionTimer / transitionDuration);
            t = t * t * (3f - 2f * t); // SmoothStep

            finalTop = Color.Lerp(prevTopColor, targetTop, t);
            finalBottom = Color.Lerp(prevBottomColor, targetBottom, t);

            if (t >= 1f)
                isTransitioning = false;
        }
        else
        {
            finalTop = targetTop;
            finalBottom = targetBottom;
        }

        // 색상이 바뀌었을 때만 텍스처 업데이트 (성능 최적화)
        if (finalTop != lastAppliedTop || finalBottom != lastAppliedBottom)
        {
            UpdateGradientTexture(finalTop, finalBottom);
        }
    }

    /// <summary>
    /// 그라데이션 텍스처의 픽셀을 갱신합니다. (1 x 32 픽셀만 갱신하므로 매우 가벼움)
    /// </summary>
    private void UpdateGradientTexture(Color topColor, Color bottomColor)
    {
        topColor.a = 1f;
        bottomColor.a = 1f;

        for (int y = 0; y < TEX_HEIGHT; y++)
        {
            float t = (float)y / (TEX_HEIGHT - 1);
            gradientTexture.SetPixel(0, y, Color.Lerp(bottomColor, topColor, t));
        }
        gradientTexture.Apply();

        lastAppliedTop = topColor;
        lastAppliedBottom = bottomColor;
    }
}
