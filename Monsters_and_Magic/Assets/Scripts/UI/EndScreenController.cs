using UnityEngine;

[RequireComponent(typeof(EndScreenView))]
public class EndScreenController : MonoBehaviour
{
    private EndScreenView view;

    private void Awake()
    {
        view = GetComponent<EndScreenView>();
    }

    private void OnEnable()
    {
        RunCompletion completion = FindFirstObjectByType<RunManager>().Completion;
        view.Show(completion);
    }

    public void OnReturnToMenuClicked()
    {
        GameContext.Instance.Flow.OpenMenu();
    }
}
