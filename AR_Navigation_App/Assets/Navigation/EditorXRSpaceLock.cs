// 파일명: Assets/Navigation/EditorXRSpaceLock.cs
// 역할: 에디터 플레이 모드에서 Immersal SDK가 XRSpace Transform을 리셋하지 못하도록
//       씬 캘리브레이션 값으로 매 프레임 고정합니다.
//       실기기 빌드에서는 #if UNITY_EDITOR 로 전체 제거 — 런타임 오버헤드 없음.
// 사용법: AR Space, AR Space 2 게임오브젝트에 추가.
//         씬에서 Transform을 수정하면 OnValidate 가 값을 자동 갱신.

using UnityEngine;

public class EditorXRSpaceLock : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("에디터 전용 고정 Transform (씬 캘리브레이션 값과 일치시키세요)")]
    [SerializeField] private Vector3 lockedPosition;
    [SerializeField] private Vector3 lockedEuler;

    // ARNavigationController가 live Transform 대신 이 값으로 행렬을 직접 구성
    // → Immersal SDK가 Update()에서 Transform을 변경해도 타이밍 문제 없이 올바른 좌표 사용 가능
    public Vector3 LockedPosition => lockedPosition;
    public Vector3 LockedEuler    => lockedEuler;

    // 씬 편집 시 현재 Transform 값을 자동으로 캐싱
    void OnValidate()
    {
        lockedPosition = transform.position;
        lockedEuler    = transform.eulerAngles;
    }

    // LateUpdate: Immersal Update 이후 실행되어 리셋된 Transform을 복원
    void LateUpdate()
    {
        transform.position = lockedPosition;
        transform.rotation = Quaternion.Euler(lockedEuler);
    }
#endif
}
