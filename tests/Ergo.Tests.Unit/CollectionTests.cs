using Ergo.Lang.Ast;
using Ergo.Runtime.WAM;

namespace Ergo.UnitTests;

public abstract class CollectionTests<TCollection> : Tests where TCollection : CollectionExpression
{
    protected abstract string Module { get; }
    protected abstract TCollection MakeCollection(Term[] elements, Term tail);
    protected abstract TCollection MakeEmpty();
    protected abstract string OpenDelim { get; }
    protected abstract string CloseDelim { get; }
    protected abstract Term EmptyElement { get; }

    private ErgoVM SetupVM()
    {
        var kb = Consult(Module);
        var vm = new ErgoVM();
        vm._QUERY = new Ergo.Compiler.Emission.Query(kb.Bytecode.AsQuery(), [], kb);
        return vm;
    }

    private Term Roundtrip(ErgoVM vm, Term term)
    {
        var w = vm.WriteHeapTerm(term); var a = vm.H; vm.Heap[vm.H++] = w;
        return vm.ReadHeapTerm(a);
    }

    private List<ErgoVM.Solution> Run(string query)
        => new ErgoVM().findall(CompileQuery(Consult(Module), query));

    [Fact] public void Parse_Empty() => Assert.Single(Run($"X = {OpenDelim}{CloseDelim}, write(X)"));
    [Fact] public void Parse_SingleElement() => Assert.Single(Run($"X = {OpenDelim}a{CloseDelim}, write(X)"));
    [Fact] public void Parse_MultipleElements() => Assert.Single(Run($"X = {OpenDelim}a, b, c{CloseDelim}, write(X)"));

    [Theory]
    [InlineData("a", "b", "c")]
    [InlineData("x")]
    [InlineData("p", "q", "r", "s")]
    public void Roundtrip_Ground(params string[] elems)
    {
        var vm = SetupVM();
        var read = Roundtrip(vm, MakeCollection(elems.Select(e => (Term)(__string)e).ToArray(), EmptyElement));
        Assert.IsType<TCollection>(read);
        Assert.Equal(elems.Length, ((TCollection)read).Contents.SkipLast(1).Count());
    }

    [Fact] public void Roundtrip_Empty() => Assert.Equal(EmptyElement.Expl, Roundtrip(SetupVM(), EmptyElement).Expl);
    [Fact] public void Unify_SameElements_Succeeds() => Assert.Single(Run($"{OpenDelim}a, b{CloseDelim} = {OpenDelim}a, b{CloseDelim}"));
    [Fact] public void Unify_Variable_Binds() => Assert.Single(Run($"X = {OpenDelim}a, b{CloseDelim}"));
    [Fact] public void Unify_DifferentElements_Fails() => Assert.Empty(Run($"{OpenDelim}a, b{CloseDelim} = {OpenDelim}a, c{CloseDelim}"));
}
