using UnityEngine;

public class CameraFollowsPlayer : MonoBehaviour
{
    public Transform cameraPosition;
    
    void Update()
    {
        transform.position = cameraPosition.position;
    }
}
