using System.IO;
using UnityEngine;

public class JsonFileStorage<T> : ILocalStorage<T> where T : class
{
    private readonly string path;
    private readonly string tempPath;

    public bool Exists => File.Exists(path);

    public JsonFileStorage(string fileName)
    {
        path = Path.Combine(Application.persistentDataPath, fileName);
        tempPath = path + ".tmp";
    }

    public T Load()
    {
        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<T>(json);
    }

    public void Save(T state)
    {
        string json = JsonUtility.ToJson(state, true);
        File.WriteAllText(tempPath, json);

        if (File.Exists(path))
        {
            string backup = path + ".bak";
            if (File.Exists(backup))
                File.Delete(backup);
            File.Replace(tempPath, path, backup);
        }
        else
        {
            File.Move(tempPath, path);
        }
    }
}
