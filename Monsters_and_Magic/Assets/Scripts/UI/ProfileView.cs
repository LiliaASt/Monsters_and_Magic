using TMPro;
using UnityEngine;

public class ProfileView : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;

    public string PlayerName => nameInput.text.Trim();

    public void SetName(string value)
    {
        nameInput.text = value;
    }
}
