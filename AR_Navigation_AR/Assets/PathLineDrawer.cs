using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PathLineDrawer : MonoBehaviour
{
    public Transform[] pathPoints;
    public Transform user;

    private LineRenderer line;

    void Start()
    {
        line = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (pathPoints == null || pathPoints.Length == 0) return;
        if (user == null) return;

        int closestIndex = FindClosestNode();

        int count = pathPoints.Length - closestIndex + 1;
        line.positionCount = count;

        Vector3 userPos = user.position;
        userPos.y = 0f;

        line.SetPosition(0, userPos);

        int index = 1;

        for (int i = closestIndex; i < pathPoints.Length; i++)
        {
            Vector3 nodePos = pathPoints[i].position;
            nodePos.y = 0f;

            line.SetPosition(index, nodePos);
            index++;
        }
    }

    int FindClosestNode()
    {
        float min = Mathf.Infinity;
        int idx = 0;

        for (int i = 0; i < pathPoints.Length; i++)
        {
            Vector3 userPos = user.position;
            Vector3 nodePos = pathPoints[i].position;

            userPos.y = 0f;
            nodePos.y = 0f;

            float d = Vector3.Distance(userPos, nodePos);

            if (d < min)
            {
                min = d;
                idx = i;
            }
        }

        return idx;
    }
}