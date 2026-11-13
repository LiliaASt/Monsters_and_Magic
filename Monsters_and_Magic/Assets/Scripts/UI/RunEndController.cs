using UnityEngine;

public class RunEndController : MonoBehaviour
{
    public void OpenMenu()
    {
        GameContext.Instance.Flow.OpenMenu();
    }
}
