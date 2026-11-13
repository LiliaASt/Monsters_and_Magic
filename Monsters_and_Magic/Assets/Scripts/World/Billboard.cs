using UnityEngine;

/// <summary>
/// Keeps a flat sprite facing the tilted 2.5D camera every frame, so
/// characters read as upright while the world around them is volumetric.
/// </summary>
public class Billboard : MonoBehaviour
{
    [Tooltip("Child transform that faces the camera. Flat children like the ground shadow stay untouched.")]
    [SerializeField] private Transform visual;

    [Tooltip("Random tilt for prop variety, added on top of the facing yaw.")]
    [SerializeField] private float tiltDegrees;

    private Camera mainCamera;

    public Transform Visual => visual;

    public void SetTilt(float degrees)
    {
        tiltDegrees = degrees;
    }

    public void SetVisual(Transform value)
    {
        visual = value;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        Transform target = visual != null ? visual : transform;

        // Yaw comes from the ROOT transform only (2D physics rotation for
        // projectiles; zero for flip-based characters and props) - never
        // from the visual itself, otherwise tilt accumulates every frame
        // and objects spin forever.
        float yaw = transform.eulerAngles.z;
        float pitch = mainCamera.transform.eulerAngles.x;

        target.rotation = Quaternion.Euler(pitch, 0f, yaw + tiltDegrees);
    }
}
