using Ergo.Shared.Extensions;

namespace Ergo.Shared.Exceptions;
public abstract class ErgoException<TError> : Exception
    where TError : Enum
{
    protected ErgoException(TError error, params object[] args) : base(GetMessage(error, args)) { }
    static string GetMessage(TError error, params object[] args) => string.Format(error.GetDescription(), args);
}
