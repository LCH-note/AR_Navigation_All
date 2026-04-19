using UnityEngine;
using TMPro;

public class DistanceUI : MonoBehaviour
{
    public Transform user;
    public Transform[] pathPoints;
    public TextMeshProUGUI distanceText;

    void Update()
    {
        if (user == null || pathPoints == null || pathPoints.Length == 0) return;

        Transform target = pathPoints[pathPoints.Length - 1];

        Vector3 userPos = user.position;
        Vector3 targetPos = target.position;

        userPos.y = 0f;
        targetPos.y = 0f;

        float distance = Vector3.Distance(userPos, targetPos);

        distanceText.text = "남은 거리: " + distance.ToString("F1") + " m";
    }
}