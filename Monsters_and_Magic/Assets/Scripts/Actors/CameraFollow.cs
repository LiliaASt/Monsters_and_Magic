using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float maxOffset = 2f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 destination = target.position;
        destination.z = transform.position.z;

        transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, smoothTime);

        Vector3 toTarget = target.position - transform.position;
        toTarget.z = 0f;

        if (toTarget.magnitude > maxOffset)
        {
            Vector3 correction = toTarget - toTarget.normalized * maxOffset;
            transform.position += correction;
        }
    }
}
