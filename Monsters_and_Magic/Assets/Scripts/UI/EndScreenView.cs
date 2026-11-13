using System.Text;
using TMPro;
using UnityEngine;

public class EndScreenView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statsText;

    public void Show(RunCompletion completion)
    {
        RunResult result = completion.Result;
        int minutes = Mathf.FloorToInt(result.Elapsed / 60f);
        int seconds = Mathf.FloorToInt(result.Elapsed % 60f);

        var text = new StringBuilder();

        text.AppendLine(result.State == RunState.Won ? "Победа" : "Поражение");
        text.AppendLine($"Время: {minutes:00}:{seconds:00}");
        text.AppendLine($"Убийства: {result.Kills}");
        text.AppendLine($"XP забега: {result.Xp}");

        if (completion.Rewards.Items.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Ресурсы:");

            foreach (ResourceAmount reward in completion.Rewards.Items)
                text.AppendLine($"{reward.resource.displayName}: +{reward.amount}");
        }

        statsText.text = text.ToString().TrimEnd();
    }
}
