using UnityEngine;

public class ArrowFollower : MonoBehaviour
{
    public Transform[] pathPoints;
    public float rotateSpeed = 5f;
    public float moveSpeed = 3f;
    public float reachDistance = 0.8f;

    private int currentIndex = 0;
    private bool isFinished = false;

    void Start()
    {
        // 🔥 1. 카메라 위치에서 시작
        Transform cam = Camera.main.transform;

        Vector3 startPos = cam.position;
        startPos.y = 0f; // 바닥 고정

        transform.position = startPos;

        currentIndex = 0;
        isFinished = false;

        // 🔥 2. 첫 번째 노드 방향 바라보기
        if (pathPoints != null && pathPoints.Length > 0)
        {
            Vector3 dir = pathPoints[0].position - transform.position;
            dir.y = 0f;

            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }

    void Update()
    {
        if (pathPoints == null || pathPoints.Length == 0) return;
        if (isFinished) return;

        Transform target = pathPoints[currentIndex];

        Vector3 currentPos = transform.position;
        Vector3 targetPos = target.position;

        // 🔥 Y 고정
        currentPos.y = 0f;
        targetPos.y = 0f;

        Vector3 direction = targetPos - currentPos;

        // 🔥 방향 회전
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRotation,
                Time.deltaTime * rotateSpeed
            );
        }

        // 🔥 이동
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            moveSpeed * Time.deltaTime
        );

        // 🔥 도착 체크
        float distance = Vector3.Distance(currentPos, targetPos);

        if (distance < reachDistance)
        {
            if (currentIndex < pathPoints.Length - 1)
            {
                currentIndex++;
                Debug.Log("➡ 다음 노드: " + currentIndex);
            }
            else
            {
                isFinished = true;
                Debug.Log("🎉 목적지 도착!");
            }
        }
    }
}