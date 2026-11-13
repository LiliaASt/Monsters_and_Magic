using UnityEngine;

/// <summary>
/// Single runtime driver for combat VFX prefabs. Effects are plain prefabs
/// referenced from attack/enemy configs; this component only owns lifetime:
/// one-shot effects self-destruct when their particles finish, looping
/// effects live until Stop() or until their parent is destroyed.
/// </summary>
[DisallowMultipleComponent]
public class VfxEffect : MonoBehaviour
{
    [Tooltip("Looping effects (flight trails, portals) stay alive until Stop() or parent destruction.")]
    public bool loop;

    private ParticleSystem[] particles;
    private float fixedLifetime = -1f;

    public static VfxEffect Spawn(GameObject prefab, Vector3 position, Quaternion rotation = default)
    {
        // default(Quaternion) is (0,0,0,0) - an INVALID quaternion that turns
        // every billboard into NaN and makes the effect invisible.
        if (rotation.x == 0f && rotation.y == 0f && rotation.z == 0f && rotation.w == 0f)
            rotation = Quaternion.identity;

        GameObject instance = Instantiate(prefab, position, rotation);
        VfxEffect vfx = instance.GetComponent<VfxEffect>();

        if (vfx == null)
            vfx = instance.AddComponent<VfxEffect>();

        // Hovl one-shot parts do not always play on awake - start them
        // explicitly so nothing spawns dead.
        foreach (ParticleSystem particle in instance.GetComponentsInChildren<ParticleSystem>(true))
            particle.Play();

        return vfx;
    }

    /// <summary>Restarts every particle system (line renderers drive themselves).</summary>
    public void Play()
    {
        foreach (ParticleSystem particle in particles)
            particle.Play();
    }

    /// <summary>Stops emission; live particles finish naturally, then the effect self-destructs.</summary>
    public void Stop()
    {
        foreach (ParticleSystem particle in particles)
            particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    /// <summary>Fixed lifetime for effects without particles (lightning arcs).</summary>
    public void PlayFor(float seconds)
    {
        Play();
        fixedLifetime = Mathf.Max(0.01f, seconds);
    }

    private void Awake()
    {
        particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void Update()
    {
        if (fixedLifetime > 0f)
        {
            fixedLifetime -= Time.deltaTime;
            if (fixedLifetime <= 0f)
                Destroy(gameObject);
            return;
        }

        if (loop)
            return;

        // One-shot: die as soon as every particle system has finished.
        // Effects without particles must use PlayFor() to get a lifetime.
        foreach (ParticleSystem particle in particles)
        {
            if (particle.IsAlive(true))
                return;
        }

        Destroy(gameObject);
    }
}
