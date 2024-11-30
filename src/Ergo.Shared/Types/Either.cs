namespace Ergo.Shared.Types;

public interface Case<T>
{
    T Value { get; }
}

internal sealed record Case1<T, _2, _3, _4, _5, _6, _7, _8, _9>(T Value) : Either<T, _2, _3, _4, _5, _6, _7, _8, _9>, Case<T>;
internal sealed record Case2<_1, T, _3, _4, _5, _6, _7, _8, _9>(T Value) : Either<_1, T, _3, _4, _5, _6, _7, _8, _9>, Case<T>;
internal sealed record Case3<_1, _2, T, _4, _5, _6, _7, _8, _9>(T Value) : Either<_1, _2, T, _4, _5, _6, _7, _8, _9>, Case<T>;
internal sealed record Case4<_1, _2, _3, T, _5, _6, _7, _8, _9>(T Value) : Either<_1, _2, _3, T, _5, _6, _7, _8, _9>, Case<T>;
internal sealed record Case5<_1, _2, _3, _4, T, _6, _7, _8, _9>(T Value) : Either<_1, _2, _3, _4, T, _6, _7, _8, _9>, Case<T>;
internal sealed record Case6<_1, _2, _3, _4, _5, T, _7, _8, _9>(T Value) : Either<_1, _2, _3, _4, _5, T, _7, _8, _9>, Case<T>;
internal sealed record Case7<_1, _2, _3, _4, _5, _6, T, _8, _9>(T Value) : Either<_1, _2, _3, _4, _5, _6, T, _8, _9>, Case<T>;
internal sealed record Case8<_1, _2, _3, _4, _5, _6, _7, T, _9>(T Value) : Either<_1, _2, _3, _4, _5, _6, _7, T, _9>, Case<T>;
internal sealed record Case9<_1, _2, _3, _4, _5, _6, _7, _8, T>(T Value) : Either<_1, _2, _3, _4, _5, _6, _7, _8, T>, Case<T>;

public record Either<T1, T2, T3, T4, T5, T6, T7, T8, T9> : Either<T1, T2, T3, T4, T5, T6, T7, T8>
{
    public static implicit operator T9(Either<T1, T2, T3, T4, T5, T6, T7, T8, T9> e) => e is Case<T9> { Value: var t } ? t : throw new InvalidOperationException();

    public static implicit operator Maybe<T9>(Either<T1, T2, T3, T4, T5, T6, T7, T8, T9> e) => e is Case<T9> { Value: var t } ? t : Maybe<T9>.None;

    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 e) => new Case1<T1, T2, T3, T4, T5, T6, T7, T8, T9>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T2 e) => new Case2<T1, T2, T3, T4, T5, T6, T7, T8, T9>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T3 e) => new Case3<T1, T2, T3, T4, T5, T6, T7, T8, T9>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T4 e) => new Case4<T1, T2, T3, T4, T5, T6, T7, T8, T9>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T5 e) => new Case5<T1, T2, T3, T4, T5, T6, T7, T8, T9>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T6 e) => new Case6<T1, T2, T3, T4, T5, T6, T7, T8, T9>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T7 e) => new Case7<T1, T2, T3, T4, T5, T6, T7, T8, T9>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T8 e) => new Case8<T1, T2, T3, T4, T5, T6, T7, T8, T9>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T9 e) => new Case9<T1, T2, T3, T4, T5, T6, T7, T8, T9>(e);
}

public record Either<T1, T2, T3, T4, T5, T6, T7, T8> : Either<T1, T2, T3, T4, T5, T6, T7>
{
    public static implicit operator T8(Either<T1, T2, T3, T4, T5, T6, T7, T8> e) => e is Case<T8> { Value: var t } ? t : throw new InvalidOperationException();
    public static implicit operator Maybe<T8>(Either<T1, T2, T3, T4, T5, T6, T7, T8> e) => e is Case<T8> { Value: var t } ? t : Maybe<T8>.None;

    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8>(T1 e) => new Case1<T1, T2, T3, T4, T5, T6, T7, T8, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8>(T2 e) => new Case2<T1, T2, T3, T4, T5, T6, T7, T8, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8>(T3 e) => new Case3<T1, T2, T3, T4, T5, T6, T7, T8, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8>(T4 e) => new Case4<T1, T2, T3, T4, T5, T6, T7, T8, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8>(T5 e) => new Case5<T1, T2, T3, T4, T5, T6, T7, T8, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8>(T6 e) => new Case6<T1, T2, T3, T4, T5, T6, T7, T8, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8>(T7 e) => new Case7<T1, T2, T3, T4, T5, T6, T7, T8, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7, T8>(T8 e) => new Case8<T1, T2, T3, T4, T5, T6, T7, T8, Unit>(e);
}
public record Either<T1, T2, T3, T4, T5, T6, T7> : Either<T1, T2, T3, T4, T5, T6>
{
    public static implicit operator T7(Either<T1, T2, T3, T4, T5, T6, T7> e) => e is Case<T7> { Value: var t } ? t : throw new InvalidOperationException();
    public static implicit operator Maybe<T7>(Either<T1, T2, T3, T4, T5, T6, T7> e) => e is Case<T7> { Value: var t } ? t : Maybe<T7>.None;

    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7>(T1 e) => new Case1<T1, T2, T3, T4, T5, T6, T7, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7>(T2 e) => new Case2<T1, T2, T3, T4, T5, T6, T7, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7>(T3 e) => new Case3<T1, T2, T3, T4, T5, T6, T7, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7>(T4 e) => new Case4<T1, T2, T3, T4, T5, T6, T7, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7>(T5 e) => new Case5<T1, T2, T3, T4, T5, T6, T7, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7>(T6 e) => new Case6<T1, T2, T3, T4, T5, T6, T7, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6, T7>(T7 e) => new Case7<T1, T2, T3, T4, T5, T6, T7, Unit, Unit>(e);
}
public record Either<T1, T2, T3, T4, T5, T6> : Either<T1, T2, T3, T4, T5>
{
    public static implicit operator T6(Either<T1, T2, T3, T4, T5, T6> e) => e is Case<T6> { Value: var t } ? t : throw new InvalidOperationException();
    public static implicit operator Maybe<T6>(Either<T1, T2, T3, T4, T5, T6> e) => e is Case<T6> { Value: var t } ? t : Maybe<T6>.None;

    public static implicit operator Either<T1, T2, T3, T4, T5, T6>(T1 e) => new Case1<T1, T2, T3, T4, T5, T6, Unit, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6>(T2 e) => new Case2<T1, T2, T3, T4, T5, T6, Unit, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6>(T3 e) => new Case3<T1, T2, T3, T4, T5, T6, Unit, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6>(T4 e) => new Case4<T1, T2, T3, T4, T5, T6, Unit, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6>(T5 e) => new Case5<T1, T2, T3, T4, T5, T6, Unit, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5, T6>(T6 e) => new Case6<T1, T2, T3, T4, T5, T6, Unit, Unit, Unit>(e);
}
public record Either<T1, T2, T3, T4, T5> : Either<T1, T2, T3, T4>
{
    public static implicit operator T5(Either<T1, T2, T3, T4, T5> e) => e is Case<T5> { Value: var t } ? t : throw new InvalidOperationException();
    public static implicit operator Maybe<T5>(Either<T1, T2, T3, T4, T5> e) => e is Case<T5> { Value: var t } ? t : Maybe<T5>.None;

    public static implicit operator Either<T1, T2, T3, T4, T5>(T1 e) => new Case1<T1, T2, T3, T4, T5, Unit, Unit, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5>(T2 e) => new Case2<T1, T2, T3, T4, T5, Unit, Unit, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5>(T3 e) => new Case3<T1, T2, T3, T4, T5, Unit, Unit, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5>(T4 e) => new Case4<T1, T2, T3, T4, T5, Unit, Unit, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4, T5>(T5 e) => new Case5<T1, T2, T3, T4, T5, Unit, Unit, Unit, Unit>(e);
}
public record Either<T1, T2, T3, T4> : Either<T1, T2, T3>
{
    public static implicit operator T4(Either<T1, T2, T3, T4> e) => e is Case<T4> { Value: var t } ? t : throw new InvalidOperationException();
    public static implicit operator Maybe<T4>(Either<T1, T2, T3, T4> e) => e is Case<T4> { Value: var t } ? t : Maybe<T4>.None;

    public static implicit operator Either<T1, T2, T3, T4>(T1 e) => new Case1<T1, T2, T3, T4, Unit, Unit, Unit, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4>(T2 e) => new Case2<T1, T2, T3, T4, Unit, Unit, Unit, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4>(T3 e) => new Case3<T1, T2, T3, T4, Unit, Unit, Unit, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3, T4>(T4 e) => new Case4<T1, T2, T3, T4, Unit, Unit, Unit, Unit, Unit>(e);
}
public record Either<T1, T2, T3> : Either<T1, T2>
{
    public static implicit operator T3(Either<T1, T2, T3> e) => e is Case<T3> { Value: var t } ? t : throw new InvalidOperationException();
    public static implicit operator Maybe<T3>(Either<T1, T2, T3> e) => e is Case<T3> { Value: var t } ? t : Maybe<T3>.None;

    public static implicit operator Either<T1, T2, T3>(T1 e) => new Case1<T1, T2, T3, Unit, Unit, Unit, Unit, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3>(T2 e) => new Case2<T1, T2, T3, Unit, Unit, Unit, Unit, Unit, Unit>(e);
    public static implicit operator Either<T1, T2, T3>(T3 e) => new Case3<T1, T2, T3, Unit, Unit, Unit, Unit, Unit, Unit>(e);
}
public record Either<T1, T2>
{
    public static implicit operator T1(Either<T1, T2> e) => e is Case<T1> { Value: var t } ? t : throw new InvalidOperationException();
    public static implicit operator Maybe<T1>(Either<T1, T2> e) => e is Case<T1> { Value: var t } ? t : Maybe<T1>.None;
    public static implicit operator T2(Either<T1, T2> e) => e is Case<T2> { Value: var t } ? t : throw new InvalidOperationException();
    public static implicit operator Maybe<T2>(Either<T1, T2> e) => e is Case<T2> { Value: var t } ? t : Maybe<T2>.None;

    public static implicit operator Either<T1, T2>(T1 e) => new Case1<T1, T2, Unit, Unit, Unit, Unit, Unit, Unit, Unit>(e);
    public static implicit operator Either<T1, T2>(T2 e) => new Case2<T1, T2, Unit, Unit, Unit, Unit, Unit, Unit, Unit>(e);
}