namespace Ergo.Shared.Types;

public class Tx<TState>(TState state, Action<TState>? rollback = null, Action? commit = null, Action? always = null) : IDisposable
{
    private bool _disposed = false;
    private bool _commit = false;
    public readonly TState State = state;
    public void Commit()
    {
        if (!_commit)
        {
            _commit = true;
            commit?.Invoke();
        }
    }
    public void Rollback()
    {
        _commit = false;
        Dispose();
    }
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            if (!_commit)
                rollback?.Invoke(State);
            always?.Invoke();
        }
    }
}