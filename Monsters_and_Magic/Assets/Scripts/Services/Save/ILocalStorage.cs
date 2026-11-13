public interface ILocalStorage<T> where T : class
{
    bool Exists { get; }

    T Load();
    void Save(T state);
}
