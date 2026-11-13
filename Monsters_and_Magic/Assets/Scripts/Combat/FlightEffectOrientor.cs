using UnityEngine;

/// <summary>
/// Keeps stretched billboard particles aligned with the carrier's flight
/// direction. Billboards ignore transform rotation, so the orientation is
/// applied through startRotation for every newly emitted particle.
/// Attach to a projectile's flight effect root.
/// </summary>
public class FlightEffectOrientor : MonoBehaviour
{
    private ParticleSystem[] particles;

    private void Awake()
    {
        particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void LateUpdate()
    {
        float angle = transform.parent != null
            ? transform.parent.rotation.eulerAngles.z
            : 0f;

        foreach (ParticleSystem particle in particles)
        {
            ParticleSystem.MainModule main = particle.main;
            main.startRotation = angle * Mathf.Deg2Rad;
        }
    }
}
