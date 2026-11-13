using UnityEngine;

/// <summary>
/// Presentation-only: listens to the run's level-up event and plays a golden
/// vortex aura around the hero. The actor stays clean of VFX concerns -
/// RunManager only wires the run in, exactly like health bars and damage
/// popups.
/// </summary>
public class LevelUpPresenter : MonoBehaviour
{
    [SerializeField] private GameObject effectPrefab;

    private Run run;

    public void Bind(Run value)
    {
        if (run != null)
            run.LevelUp -= HandleLevelUp;

        run = value;
        run.LevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (run != null)
            run.LevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int level)
    {
        if (effectPrefab == null)
            return;

        // Aura-style: parented to the hero so the emitter follows him;
        // world-space simulation keeps the spiral vertical regardless of
        // hero movement. VfxEffect (loop = false) removes it after ~1s.
        GameObject aura = Instantiate(
            effectPrefab, transform.position, Quaternion.identity);
        aura.transform.SetParent(transform, false);
        aura.transform.localPosition = Vector3.zero;
    }
}
