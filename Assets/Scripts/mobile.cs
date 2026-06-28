using UnityEngine;
using UnityEngine.UI;

public class mobile : MonoBehaviour
{
    [Header("=== 모바일 UI 설정 ===")]
    [Tooltip("점프 버튼 UI")]
    public Button jumpButton;

    [Tooltip("제어할 쿠키 점프 스크립트")]
    public CookieJump cookieJump;

    void Start()
    {
        // 버튼 클릭 이벤트에 OnJumpButtonClicked 메서드 연결
        if (jumpButton != null)
        {
            jumpButton.onClick.AddListener(OnJumpButtonClicked);
        }
        else
        {
            Debug.LogWarning("[mobile] 점프 버튼이 할당되지 않았습니다. 인스펙터에서 할당해주세요.");
        }

        // cookieJump가 할당되지 않았다면 씬에서 찾기
        if (cookieJump == null)
        {
            cookieJump = FindFirstObjectByType<CookieJump>();
        }
    }

    public void OnJumpButtonClicked()
    {
        if (cookieJump != null)
        {
            cookieJump.Jump(); // 쿠키 점프 실행
        }
        else
        {
            Debug.LogWarning("[mobile] CookieJump 스크립트를 찾을 수 없어 점프할 수 없습니다.");
        }
    }
}
