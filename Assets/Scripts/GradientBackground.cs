using UnityEngine;

/// <summary>
/// 카메라 전체 배경을 그라데이션으로 채우는 스크립트.
/// 아래쪽은 타일 색상, 위쪽은 하늘색으로 자연스럽게 전환.
/// 카메라 뷰 전체를 빈틈없이 덮는다.
///
/// [사용법]
/// 1. Main Camera 오브젝트에 이 스크립트를 직접 추가 (가장 간단!)
///    또는 빈 GameObject에 추가해도 됨
/// 2. Inspector에서 색상 조절
/// 3. 끝!
/// </summary>
[ExecuteAlways]
public class GradientBackground : MonoBehaviour
{
    [Header("=== 그라데이션 색상 ===")]
    [Tooltip("배경 하단 색상 (타일 색상과 동일하게)")]
    public Color bottomColor = new Color(0.784f, 0.847f, 0.498f); // 타일 배경의 연한 올리브색

    [Tooltip("배경 상단 색상 (하늘색)")]
    public Color topColor = new Color(0.529f, 0.808f, 0.980f);    // 밝은 하늘색

    [Header("=== 렌더링 설정 ===")]
    [Tooltip("Sorting Order (다른 모든 것보다 뒤에)")]
    public int sortingOrder = -100;

    // ── 내부 변수 ──
    private GameObject _bgObject;
    private SpriteRenderer _sr;
    private Texture2D _tex;
    private Sprite _sprite;
    private bool _needsRebuild = true;

    // 변경 감지
    private Color _cachedBottom;
    private Color _cachedTop;

    void OnEnable()
    {
        _needsRebuild = true;
    }

    void LateUpdate()
    {
        if (_needsRebuild || _cachedBottom != bottomColor || _cachedTop != topColor)
        {
            RebuildGradient();
            _needsRebuild = false;
        }

        FitToCamera();
    }

    void OnValidate()
    {
        _needsRebuild = true;
    }

    void OnDisable()
    {
        CleanUp();
    }

    void OnDestroy()
    {
        CleanUp();
    }

    /// <summary>
    /// 그라데이션 배경 생성
    /// </summary>
    private void RebuildGradient()
    {
        // 기존 리소스 정리
        CleanUp();

        // 배경 전용 자식 오브젝트 생성
        _bgObject = new GameObject("__GradientBG__");
        _bgObject.transform.SetParent(transform);
        _bgObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

        // SpriteRenderer 추가
        _sr = _bgObject.AddComponent<SpriteRenderer>();
        _sr.sortingOrder = sortingOrder;
        _sr.color = Color.white;

        // ── 그라데이션 텍스처 생성 (2px × 256px) ──
        int texW = 2;
        int texH = 256;
        _tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        _tex.filterMode = FilterMode.Bilinear;
        _tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < texH; y++)
        {
            float t = (float)y / (texH - 1);
            Color c = Color.Lerp(bottomColor, topColor, t);
            for (int x = 0; x < texW; x++)
            {
                _tex.SetPixel(x, y, c);
            }
        }
        _tex.Apply();

        // ── 스프라이트 생성 (100 PPU, 중앙 피벗) ──
        _sprite = Sprite.Create(
            _tex,
            new Rect(0, 0, texW, texH),
            new Vector2(0.5f, 0.5f),
            100f
        );

        _sr.sprite = _sprite;

        // 카메라 배경색도 상단 색상으로 맞춤 (빈틈 방지)
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.backgroundColor = topColor;
        }

        // 캐시
        _cachedBottom = bottomColor;
        _cachedTop = topColor;

        // 즉시 위치/크기 맞춤
        FitToCamera();
    }

    /// <summary>
    /// 카메라 뷰 전체를 꽉 채우도록 위치와 스케일을 조정한다.
    /// </summary>
    private void FitToCamera()
    {
        Camera cam = Camera.main;
        if (cam == null || _bgObject == null || _sr == null || _sr.sprite == null) return;

        // 카메라 뷰 크기 (월드 단위)
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;

        // 스프라이트 원본 크기 (월드 단위, 100 PPU 기준)
        float spriteW = _sr.sprite.bounds.size.x; // 2/100 = 0.02
        float spriteH = _sr.sprite.bounds.size.y; // 256/100 = 2.56

        // 카메라 전체를 덮도록 스케일 계산 (여유분 포함)
        float scaleX = (camWidth + 2f) / spriteW;
        float scaleY = (camHeight + 2f) / spriteH;

        _bgObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        // 카메라 중심에 배치, Z는 뒤쪽
        _bgObject.transform.position = new Vector3(
            cam.transform.position.x,
            cam.transform.position.y,
            cam.transform.position.z + 50f // 카메라 앞, 다른 오브젝트 뒤
        );
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    private void CleanUp()
    {
        if (_bgObject != null)
        {
            if (Application.isPlaying)
                Destroy(_bgObject);
            else
                DestroyImmediate(_bgObject);
            _bgObject = null;
        }

        if (_sprite != null)
        {
            if (Application.isPlaying)
                Destroy(_sprite);
            else
                DestroyImmediate(_sprite);
            _sprite = null;
        }

        if (_tex != null)
        {
            if (Application.isPlaying)
                Destroy(_tex);
            else
                DestroyImmediate(_tex);
            _tex = null;
        }

        _sr = null;

        // 혹시 남아있는 자식 오브젝트 정리
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (child.name == "__GradientBG__")
            {
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }
    }
}
