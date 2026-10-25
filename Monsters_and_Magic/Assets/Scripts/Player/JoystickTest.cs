using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Ссылка на джойстик")]
    public Joystick joystick;

    private float deadZone = 0.01f;

    private void Update()
    {
        if (joystick == null)
        {
            Debug.LogWarning("[PlayerController] Джойстик не назначен в Inspector!");
            return;
        }

        Vector2 direction = joystick.Direction;

        if (direction.magnitude > deadZone)
        {
            Debug.Log($"[PlayerController] Направление: X={direction.x:F2}, Y={direction.y:F2}");
        }
    }
}