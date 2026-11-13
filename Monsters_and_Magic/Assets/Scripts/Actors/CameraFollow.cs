using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float maxOffset = 2f;

    [Tooltip("Vertical look offset for the tilted 2.5D camera: distance * tan(pitch). Keeps the hero at screen center.")]
    [SerializeField] private float lookHeightOffset = 0f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 destination = target.position;
        destination.y += lookHeightOffset;
        destination.z = transform.position.z;

        transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, smoothTime);

        // The leash must measure drift from the desired look point
        // (hero + tilt offset), not from the hero itself - otherwise it
        // cancels the offset and pushes the hero off-screen.
        Vector3 toDesired = destination - transform.position;
        toDesired.z = 0f;

        if (toDesired.magnitude > maxOffset)
        {
            Vector3 correction = toDesired - toDesired.normalized * maxOffset;
            transform.position += correction;
        }
    }
}
