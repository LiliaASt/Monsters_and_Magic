using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Online availability probe. internetReachability is only a hint; actual
/// availability is confirmed by a lightweight request. Gameplay never
/// depends on this - it only gates optional cloud sync.
/// </summary>
public class ConnectivityService : MonoBehaviour
{
    [SerializeField] private string probeUrl = "https://unity.com";
    [SerializeField] private float checkInterval = 15f;

    public bool IsOnline { get; private set; }
    public event Action<bool> Changed;

    private void Start()
    {
        StartCoroutine(CheckLoop());
    }

    private IEnumerator CheckLoop()
    {
        while (true)
        {
            yield return Check();
            yield return new WaitForSeconds(checkInterval);
        }
    }

    private IEnumerator Check()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            SetOnline(false);
            yield break;
        }

        using UnityWebRequest request = UnityWebRequest.Head(probeUrl);
        request.timeout = 5;
        yield return request.SendWebRequest();
        SetOnline(request.result == UnityWebRequest.Result.Success);
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
            StartCoroutine(Check());
    }

    private void SetOnline(bool value)
    {
        if (IsOnline == value)
            return;

        IsOnline = value;
        Changed?.Invoke(value);
    }
}
