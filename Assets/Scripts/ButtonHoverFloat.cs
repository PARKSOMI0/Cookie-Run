using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverFloat : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    public float hoverScaleMultiplier = 1.15f;
    public float scaleSpeed = 10f;

    [Header("Float Settings")]
    public float floatAmplitude = 10f; // 얼마나 높이/낮게 둥둥 떠다닐지
    public float floatSpeed = 2f;      // 둥둥 떠다니는 속도

    private Vector3 originalScale;
    private Vector3 targetScale;
    
    private Vector2 originalAnchoredPosition;
    private RectTransform rectTransform;
    
    private float floatTimer;
    private float randomOffset;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalAnchoredPosition = rectTransform.anchoredPosition;
        }
        else
        {
            // Canvas UI가 아닐 경우를 대비한 Transform 위치 백업 (혹시 몰라서 추가)
            originalAnchoredPosition = transform.localPosition;
        }

        originalScale = transform.localScale;
        targetScale = originalScale;

        // 버튼마다 각자 다른 타이밍에 떠다니도록 랜덤 오프셋 적용
        randomOffset = Random.Range(0f, 10f);
    }

    void Update()
    {
        // 1. 마우스 호버 시 크기 변경 부드럽게 (Scale Interpolation)
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * scaleSpeed);

        // 2. 둥둥 떠다니는 애니메이션 (Floating Animation)
        floatTimer += Time.deltaTime * floatSpeed;
        float wave = Mathf.Sin(floatTimer + randomOffset) * floatAmplitude;

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, originalAnchoredPosition.y + wave);
        }
        else
        {
            // Canvas UI가 아닌 2D Sprite 버튼일 경우
            transform.localPosition = new Vector3(transform.localPosition.x, originalAnchoredPosition.y + wave, transform.localPosition.z);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 마우스가 버튼에 올라갔을 때 크기를 키움
        targetScale = originalScale * hoverScaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 마우스가 버튼에서 벗어났을 때 원래 크기로 복구
        targetScale = originalScale;
    }
}
