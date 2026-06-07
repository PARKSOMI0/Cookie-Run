using UnityEngine;
using System.Collections;

public class RotateText : MonoBehaviour
{
    [Header("Settings")]
    public float rotationSpeed = 100f;
    public float delayBeforeAppear = 2f; // n초 후 나타남

    private bool shouldRotate = false;

    void Awake()
    {
        // 시작할 때 게임 오브젝트를 숨깁니다.
        // 자식 오브젝트를 포함하는 부모 컨테이너 등에 붙어있다면 렌더러만 끄는게 나을 수도 있지만,
        // 현재는 자기 자신을 회전시키는 스크립트이므로 Awake나 Start 대신 
        // Image나 CanvasRenderer를 끄는 방식을 사용할 수도 있습니다.
        // 하지만 SetActive를 끄면 코루틴이 동작하지 않으므로, 
        // 렌더러 컴포넌트를 끄는 것이 가장 안전합니다.
        
        // 어떤 UI든 숨길 수 있도록 CanvasRenderer를 사용합니다.
        CanvasRenderer cr = GetComponent<CanvasRenderer>();
        if (cr != null)
        {
            cr.cullTransparentMesh = true; // 투명하게 만들 수 있지만 완벽히 숨겨지지 않을 수 있음.
            cr.SetAlpha(0f);
        }

        // 딜레이 코루틴 시작
        StartCoroutine(AppearRoutine());
    }

    IEnumerator AppearRoutine()
    {
        // delayBeforeAppear 초만큼 대기
        yield return new WaitForSeconds(delayBeforeAppear);
        
        CanvasRenderer cr = GetComponent<CanvasRenderer>();
        if (cr != null)
        {
            cr.SetAlpha(1f);
        }

        // 회전 시작
        shouldRotate = true;
    }

    void Update()
    {
        if (shouldRotate)
        {
            // 정면을 바라보면서 회전 (Z축 기준 회전)
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
    }
}
