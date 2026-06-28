using UnityEngine;
using UnityEngine.UI;

public class daynight : MonoBehaviour
{
    [Header("낮/밤 유지 시간 설정 (초)")]
    public float dayDuration = 10f;
    public float nightDuration = 10f;
    
    [Header("낮/밤 전환 속도 (초)")]
    public float transitionSpeed = 1.5f;

    [Header("화면 크기 맞춤")]
    [Tooltip("체크하면 모바일 기기의 화면 비율에 상관없이 배경이 화면에 꽉 차도록 자동 조절됩니다.")]
    public bool autoFitToScreen = true;

    [Header("=== 색상 모드 설정 ===")]
    [Tooltip("체크하면 위아래 그라데이션을 사용하고, 해제하면 단일 색상을 사용합니다.")]
    public bool useGradient = true;

    [Header("[단일 색상 설정] (useGradient = false)")]
    public Color dayColor = new Color(0.4f, 0.7f, 1f);
    public Color nightColor = new Color(0.05f, 0.05f, 0.15f);

    [Header("[그라데이션 설정] (useGradient = true)")]
    public Color dayTopColor = new Color(0.53f, 0.81f, 0.92f, 1f);
    public Color dayBottomColor = new Color(0.85f, 0.95f, 1f, 1f);
    public Color nightTopColor = new Color(0.02f, 0.02f, 0.12f, 1f);
    public Color nightBottomColor = new Color(0.08f, 0.06f, 0.25f, 1f);

    [Tooltip("현재 상태 (테스트용)")]
    public bool isNight = false;

    // 내부 변수
    private SpriteRenderer[] spriteRenderers;
    private Image uiImage;
    private Camera mainCamera;
    
    private float timer = 0f;
    private float transitionProgress = 0f;

    // 그라데이션 전용 변수
    private Texture2D gradientTexture;
    private const int TEX_HEIGHT = 32;

    void Start()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        uiImage = GetComponent<Image>();

        if ((spriteRenderers == null || spriteRenderers.Length == 0) && uiImage == null)
        {
            mainCamera = Camera.main;
        }

        // 그라데이션 텍스처 초기화
        if (useGradient && spriteRenderers != null && spriteRenderers.Length > 0)
        {
            InitializeGradient();
        }

        if (autoFitToScreen)
        {
            FitToScreen();
        }
    }

    private void InitializeGradient()
    {
        // 첫 번째 SpriteRenderer를 기준으로 그라데이션 텍스처 생성
        SpriteRenderer targetSR = spriteRenderers[0];
        Sprite origSprite = targetSR.sprite;
        
        Vector2 origBoundsSize = origSprite != null ? origSprite.bounds.size : new Vector2(20f, 12f);
        
        // ★ WebGL Infinity 에러 방지 (최소값 보정)
        origBoundsSize.x = Mathf.Max(origBoundsSize.x, 0.01f);
        origBoundsSize.y = Mathf.Max(origBoundsSize.y, 0.01f);

        gradientTexture = new Texture2D(1, TEX_HEIGHT, TextureFormat.RGBA32, false);
        gradientTexture.filterMode = FilterMode.Bilinear;
        gradientTexture.wrapMode = TextureWrapMode.Clamp;

        float ppu = TEX_HEIGHT / origBoundsSize.y;
        
        Sprite gradientSprite = Sprite.Create(
            gradientTexture,
            new Rect(0, 0, 1, TEX_HEIGHT),
            new Vector2(0.5f, 0.5f),
            ppu
        );

        float newWidth = 1f / ppu;
        float xScaleFactor = origBoundsSize.x / newWidth;
        if (float.IsInfinity(xScaleFactor) || float.IsNaN(xScaleFactor)) xScaleFactor = 1f;

        Vector3 scale = targetSR.transform.localScale;
        scale.x *= xScaleFactor;
        targetSR.transform.localScale = scale;

        targetSR.sprite = gradientSprite;
        targetSR.color = Color.white;

        // 다른 모든 SpriteRenderer도 동일하게 텍스처 적용
        for (int i = 1; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].sprite = gradientSprite;
            spriteRenderers[i].color = Color.white;
            spriteRenderers[i].transform.localScale = targetSR.transform.localScale;
        }
    }

    void Update()
    {
        if (autoFitToScreen)
        {
            FitToScreen(); 
        }

        timer += Time.deltaTime;
        float currentCycleDuration = isNight ? nightDuration : dayDuration;
        if (timer >= currentCycleDuration)
        {
            isNight = !isNight;
            timer = 0f;
        }

        float targetProgress = isNight ? 1f : 0f;
        transitionProgress = Mathf.Lerp(transitionProgress, targetProgress, Time.deltaTime * transitionSpeed);

        if (useGradient && gradientTexture != null)
        {
            // 그라데이션 색상 업데이트
            Color currentTop = Color.Lerp(dayTopColor, nightTopColor, transitionProgress);
            Color currentBottom = Color.Lerp(dayBottomColor, nightBottomColor, transitionProgress);
            UpdateGradientTexture(currentTop, currentBottom);
        }
        else
        {
            // 단일 색상 업데이트
            Color currentColor = Color.Lerp(dayColor, nightColor, transitionProgress);
            ApplySolidColor(currentColor);
        }
    }

    private void UpdateGradientTexture(Color top, Color bottom)
    {
        for (int y = 0; y < TEX_HEIGHT; y++)
        {
            float t = (float)y / (TEX_HEIGHT - 1);
            gradientTexture.SetPixel(0, y, Color.Lerp(bottom, top, t));
        }
        gradientTexture.Apply();
    }

    private void ApplySolidColor(Color color)
    {
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            foreach (var sr in spriteRenderers)
                if (sr != null) sr.color = color;
        }
        else if (uiImage != null)
        {
            uiImage.color = color;
        }
        else if (mainCamera != null)
        {
            mainCamera.backgroundColor = color;
        }
    }

    void FitToScreen()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        if (uiImage != null)
        {
            RectTransform rt = uiImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return;
        }

        if (spriteRenderers != null && spriteRenderers.Length > 0 && cam.orthographic)
        {
            transform.localScale = Vector3.one; 

            float camHeight = cam.orthographicSize * 2f;
            float camWidth = camHeight * cam.aspect;

            Bounds bounds = new Bounds(spriteRenderers[0].transform.position, Vector3.zero);
            foreach (var sr in spriteRenderers)
            {
                if (sr.sprite != null)
                    bounds.Encapsulate(sr.bounds);
            }

            float spriteW = bounds.size.x;
            float spriteH = bounds.size.y;
            if (spriteW <= 0.01f || spriteH <= 0.01f) return;

            float scaleX = camWidth / spriteW;
            float scaleY = camHeight / spriteH;
            
            // 비율을 유지하면서 넉넉하게 덮음
            float scale = Mathf.Max(scaleX, scaleY) * 1.05f;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
