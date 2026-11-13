using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.CloudSave;
using UnityEngine;

/// <summary>
/// Cloud sync of the single ProfileState via Unity Cloud Save. Local-first:
/// every failure keeps the local profile intact and marks it pending.
/// Domain models know nothing about this service.
/// </summary>
public class CloudSyncService : MonoBehaviour
{
    private const string CloudKey = "profile";

    [SerializeField] private float debounceSeconds = 3f;

    private ProfileService profile;
    private ConnectivityService connectivity;
    private float lastChangeTime = -999f;

    public bool IsSignedIn { get; private set; }
    public event Action<string> Status;

    public void Initialize(ProfileService profile, ConnectivityService connectivity)
    {
        this.profile = profile;
        this.connectivity = connectivity;
        connectivity.Changed += _ => MarkDirty();
        _ = SignInAndPull();
    }

    public void MarkDirty()
    {
        lastChangeTime = Time.time;
    }

    private async void Update()
    {
        if (lastChangeTime < 0f ||
            Time.time - lastChangeTime < debounceSeconds ||
            !IsSignedIn ||
            !connectivity.IsOnline)
            return;

        lastChangeTime = -999f;
        await Sync();
    }

    private async Task SignInAndPull()
    {
        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            IsSignedIn = true;
            Status?.Invoke("signed in");
            await PullRemote();
        }
        catch (Exception exception)
        {
            Status?.Invoke("offline: " + exception.Message);
        }
    }

    private async Task PullRemote()
    {
        var items = await CloudSaveService.Instance.Data.LoadAllAsync();

        if (!items.TryGetValue(CloudKey, out string cloudJson))
            return;

        ProfileState cloud = JsonUtility.FromJson<ProfileState>(cloudJson);

        if (cloud.sync.revision > profile.CurrentRevision)
        {
            profile.ReplaceState(cloud);
            Status?.Invoke("restored from cloud");
        }
    }

    private async Task Sync()
    {
        try
        {
            string json = profile.Serialize();
            var data = new System.Collections.Generic.Dictionary<string, object>
            {
                { CloudKey, json }
            };
            await CloudSaveService.Instance.Data.ForceSaveAsync(data);
            profile.MarkSynced();
            Status?.Invoke("synced");
        }
        catch (Exception exception)
        {
            Status?.Invoke("sync failed: " + exception.Message);
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && IsSignedIn && connectivity.IsOnline)
            _ = Sync();
    }
}
