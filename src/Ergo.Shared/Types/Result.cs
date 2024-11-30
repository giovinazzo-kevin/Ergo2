using Ergo.Shared.Interfaces;
using System.Runtime.ExceptionServices;

namespace Ergo.Shared.Types;

public abstract record Result<T, Err>
{
    public static implicit operator Result<T, Err>(T result) =>  new Success<T, Err>(result);
    public static implicit operator Result<T, Err>(Err result) =>  new Error<T, Err>(result);

    public void EnsureSuccess()
    {
        if (this is Error<IException> { Value.Exception: var ex })
            ExceptionDispatchInfo.Capture(ex).Throw();
        if (this is Error)
            throw new InvalidOperationException(ToString());
    }
}

public interface Success;
public interface Success<out T> : Success
{
    T Value { get; }
}

public interface Error;
public interface Error<out T> : Error
{
    T Value { get; }
}

internal sealed record Success<T, _>(T Value) : Result<T, _>, Success<T>;
internal sealed record Error<_, T>(T Value) : Result<_, T>, Error<T>;
