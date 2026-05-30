/*
    파일명: Assets/Navigation/ImmersalEditorGuard.cs
    역할: 에디터 플레이 모드에서 ImmersalSDK 오브젝트를 비활성화하여
          실제 AR 카메라 없이 발생하는 반복 에러를 방지합니다.
          (Could not acquire camera intrinsics — ImmersalSession 매 프레임 반복)
    부착 위치: ImmersalSDK 오브젝트
*/

using UnityEngine;

public class ImmersalEditorGuard : MonoBehaviour
{
    void Awake()
    {
#if UNITY_EDITOR
        // 에디터에는 실제 AR 카메라(XRCameraSubsystem)가 없으므로
        // ImmersalSession이 매 프레임 ARFoundationSupport.GetCameraData()를 호출해 에러 발생
        // ImmersalSDK 비활성화 후 ImmersalSession도 찾아서 함께 비활성화 — 순서 중요
        // ImmersalSession.Start()가 ImmersalSDK.Instance를 참조하기 전에 먼저 꺼야 NullRef 방지
        var session = FindObjectOfType<Immersal.XR.ImmersalSession>(true);
        if (session != null) session.gameObject.SetActive(false);
        gameObject.SetActive(false);
#endif
    }
}
