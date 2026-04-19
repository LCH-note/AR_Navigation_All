using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ARGroundPlacement : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public Transform arrow;
    public float followSpeed = 10f;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Update()
    {
        if (raycastManager == null || arrow == null) return;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            Vector3 newPos = Vector3.Lerp(
                arrow.position,
                hitPose.position,
                Time.deltaTime * followSpeed
            );

            arrow.position = newPos;
        }
    }
}