using UnityEngine;

/// <summary>
/// Presentation-only subscriber: binds to a Health instance and spawns a
/// DamagePopup for every hit - the actor stays clean of UI concerns, exactly
/// like WorldHealthBarController. Must live in its own file: Unity only
/// creates a serializable script reference for a MonoBehaviour matching the
/// file name.
/// </summary>
public class DamagePopupPresenter : MonoBehaviour
{
    private Health health;

    public void Bind(Health value)
    {
        if (health != null)
            health.Damaged -= HandleDamaged;

        health = value;
        health.Damaged += HandleDamaged;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.Damaged -= HandleDamaged;
    }

    private void HandleDamaged(float amount)
    {
        DamagePopup.Spawn(transform.position, amount);
    }
}
