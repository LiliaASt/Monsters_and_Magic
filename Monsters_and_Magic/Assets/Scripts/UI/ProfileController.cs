using UnityEngine;

[RequireComponent(typeof(ProfileView))]
public class ProfileController : MonoBehaviour
{
    private ProfileView view;

    private void Awake()
    {
        view = GetComponent<ProfileView>();
    }

    private void Start()
    {
        view.SetName(GameContext.Instance.Player.Name);
    }

    public void OnContinueClicked()
    {
        string playerName = view.PlayerName;

        if (playerName.Length == 0)
            return;

        GameContext.Instance.Player.SetName(playerName);
        GameContext.Instance.Save();
    }
}
