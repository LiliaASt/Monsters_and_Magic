using UnityEngine;

/// <summary>
/// Survivor.io-style character presentation: the 3D model always stands
/// upright facing the camera and flips left/right with movement; no physics
/// rotation is involved. Adds a light procedural run bob and arm swing.
/// </summary>
public class CharacterFacing : MonoBehaviour
{
    [SerializeField] private Transform model;
    [SerializeField] private Transform leftArm;
    [SerializeField] private Transform rightArm;
    [SerializeField] private float flipSpeed = 720f;
    [SerializeField] private float bobFrequency = 9f;
    [SerializeField] private float bobAmplitude = 0.07f;

    private Rigidbody2D body;
    private float yaw = 180f;
    private float phase;
    private Vector3 modelPosition;

    private void Awake()
    {
        body = GetComponentInParent<Rigidbody2D>();

        if (model != null)
            modelPosition = model.localPosition;
    }

    private void Update()
    {
        if (body == null || model == null)
            return;

        Vector2 velocity = body.linearVelocity;

        if (Mathf.Abs(velocity.x) > 0.15f)
            yaw = velocity.x > 0f ? 0f : 180f;

        // Flip around the model's own vertical axis (world Z, which points
        // at the camera): the character stays upright and keeps facing the
        // camera while turning left/right.
        Vector3 angles = model.localEulerAngles;
        angles.z = Mathf.MoveTowardsAngle(angles.z, yaw, flipSpeed * Time.deltaTime);
        model.localEulerAngles = angles;

        float speed = velocity.magnitude;

        if (speed > 0.15f)
            phase += Time.deltaTime * bobFrequency * Mathf.Min(speed, 6f) * 0.5f;

        float bob = Mathf.Sin(phase) * bobAmplitude * Mathf.Clamp01(speed / 3f);
        model.localPosition = modelPosition + new Vector3(0f, 0f, bob);

        if (leftArm != null)
            leftArm.localRotation = Quaternion.Euler(Mathf.Sin(phase) * 35f, 0f, 0f);

        if (rightArm != null)
            rightArm.localRotation = Quaternion.Euler(Mathf.Sin(phase + Mathf.PI) * 35f, 0f, 0f);
    }
}
