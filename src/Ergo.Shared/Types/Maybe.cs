﻿namespace Ergo.Shared.Types;

public static class Maybe
{
    public static Maybe<T> Some<T>(T some) => Maybe<T>.Some(some);
    public static Maybe<T> None<T>() => Maybe<T>.None;

    public static Maybe<T> FromTryGet<T>(Func<(bool, T)> tryGet)
    {
        var (success, value) = tryGet();
        if (success)
            return Some(value);
        return Maybe<T>.None;
    }
    public static Maybe<T> FromNullable<T>(T? nullable)
        where T : struct
    {
        if (nullable.HasValue)
            return Some(nullable.Value);
        return Maybe<T>.None;
    }
    public static Maybe<T> FromNullable<T>(T nullable)
        where T : class
    {
        if (nullable is not null)
            return Some(nullable);
        return Maybe<T>.None;
    }
    public static Maybe<T> Or<T>(params IEnumerable<Func<Maybe<T>>> parsers) 
        => parsers.Aggregate(Maybe<T>.None, (a, b) => a.Or(b));
}

public readonly struct Maybe<T>
{
    public readonly bool HasValue;
    private readonly T Value { get; }

    public Maybe<U> Map<U>(Func<T, Maybe<U>> some, Func<Maybe<U>>? none = null)
    {
        if (HasValue)
        {
            return some(Value);
        }

        if (none != null)
        {
            return none();
        }

        return Maybe<U>.None;
    }

    public Maybe<U> Cast<U>()
        => Select(x => (U)(object)x!);

    public Maybe<U> Select<U>(Func<T, U> some, Func<U>? none = null)
    {
        if (HasValue)
        {
            return some(Value);
        }

        if (none != null)
        {
            return none();
        }

        return Maybe<U>.None;
    }

    public U Reduce<U>(Func<T, U> some, Func<U> none)
    {
        if (HasValue)
        {
            return some(Value);
        }
        return none();
    }

    public Maybe<T> Where(Func<T, bool> cond)
    {
        if (HasValue && cond(Value))
            return this;
        return default;
    }

    public IEnumerable<T> AsEnumerable()
    {
        if (HasValue)
            yield return Value;
    }

    public bool TryGetValue(out T value)
    {
        if (HasValue) { value = Value; return true; }

        value = default!;
        return false;
    }
    public T GetOr(T other)
    {
        if (HasValue)
            return Value;
        return other;
    }
    public T GetOrLazy(Func<T> other)
    {
        if (HasValue)
            return Value;
        return other();
    }
    public Maybe<T> Or(Func<Maybe<T>> other)
    {
        if (HasValue)
            return this;
        return other();
    }

    public Maybe<T> And<U>(Func<Maybe<U>> other)
    {
        var this_ = this;
        return other().Map(_ => this_);
    }

    private static readonly InvalidOperationException InvalidOp = new();
    public T GetOrThrow()
    {
        if (HasValue)
            return Value;
        throw InvalidOp;
    }
    public T GetOrThrow(Exception ex)
    {
        if (HasValue)
            return Value;
        throw ex;
    }
    public T GetOrThrow(Func<Exception> ex)
    {
        if (HasValue)
            return Value;
        throw ex();
    }

    public Maybe<T> Do(Action<T>? some = null, Action? none = null) => Map<T>(v => { some?.Invoke(v); return v; }, () => { none?.Invoke(); return default; });
    public Maybe<T> Do(Action? some = null, Action? none = null) => Map<T>(v => { some?.Invoke(); return v; }, () => { none?.Invoke(); return default; });
    public Maybe<T> DoAlways(Action? always = null) => Do(_ => always?.Invoke(), always);
    public Maybe<T> DoWhenSome(Action? some = null) => Do(_ => some?.Invoke());
    public Maybe<T> DoWhenSome(Action<T>? some = null) => Do(some);
    public Maybe<T> DoWhenNone(Action? none = null) => Do(default(Action), none);
    public bool Check(Func<T, bool> cond) => HasValue && cond(Value);

    private Maybe(T value)
    {
        Value = value;
        HasValue = true;
    }

    public static readonly Maybe<T> None = default;
    public static Maybe<T> Some(T value) => new(value);

    public static implicit operator Maybe<T>(T a) => Maybe.Some(a);

    public override int GetHashCode()
    {
        if (!HasValue)
            return 1658;
        return Value!.GetHashCode();
    }

    public override string ToString() => HasValue ? $"Some {{{Value}}}" : "None";
}
