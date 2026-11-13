using UnityEngine;

/// <summary>
/// Keeps a TrailRenderer's world orientation fixed. TrailRenderer stores its
/// vertices in local space, so a rotating parent (a homing projectile) would
/// swing the whole tail rigidly behind it. Counter-rotating to identity keeps
/// old points in world space, letting the tail bend along the real path.
/// </summary>
[RequireComponent(typeof(TrailRenderer))]
public class WorldSpaceTrail : MonoBehaviour
{
    private void LateUpdate()
    {
        transform.rotation = Quaternion.identity;
    }
}
