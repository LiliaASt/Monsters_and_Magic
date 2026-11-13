using UnityEngine;

[RequireComponent(typeof(HUDView))]
public class HUDController : MonoBehaviour
{
    private HUDView view;
    private RunManager runManager;
    private Run run;

    private void Awake()
    {
        view = GetComponent<HUDView>();
    }

    private void Start()
    {
        runManager = FindFirstObjectByType<RunManager>();
    }

    private void Update()
    {
        run ??= runManager.CurrentRun;

        if (run == null)
            return;

        view.SetTime(run.Elapsed);
        view.SetStats(run.Kills);
        view.SetLevel(run.Level);
        view.SetXpProgress(run.XpToNext > 0 ? (float)run.Xp / run.XpToNext : 0f);
    }
}
