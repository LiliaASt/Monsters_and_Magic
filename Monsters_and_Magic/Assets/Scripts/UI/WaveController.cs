using UnityEngine;

public class WaveController : MonoBehaviour
{
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private WaveNotifier view;

    private void OnEnable()
    {
        waveManager.WaveStarted += HandleWaveStarted;
    }

    private void OnDisable()
    {
        waveManager.WaveStarted -= HandleWaveStarted;
    }

    private void HandleWaveStarted(WaveState wave)
    {
        view.ShowWave(wave.Number);
    }
}
