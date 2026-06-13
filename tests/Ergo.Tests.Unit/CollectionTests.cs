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

    private Lang.Ast.Term Roundtrip(ErgoVM vm, Lang.Ast.Term term)
    {
        var word = vm.WriteHeapTerm(term);
        var addr = vm.H;
        vm.Heap[vm.H++] = word;
        return vm.ReadHeapTerm(addr);
    }

    private List<ErgoVM.Solution> Run(string query)
    {
        var kb = Consult(Module);
        var vm = new ErgoVM();
        var q = CompileQuery(kb, query);
        var solutions = new List<ErgoVM.Solution>();
        vm.SolutionEmitted += _ => solutions.Add(vm.MaterializeSolution());
        vm.Run(q);
        return solutions;
    }

    #region Parse
    [Fact]
    public void Parse_Empty()
    {
        var solutions = Run($"X = {OpenDelim}{CloseDelim}, write(X)");
        Assert.Single(solutions);
    }

    [Fact]
    public void Parse_SingleElement()
    {
        var solutions = Run($"X = {OpenDelim}a{CloseDelim}, write(X)");
        Assert.Single(solutions);
    }

    [Fact]
    public void Parse_MultipleElements()
    {
        var solutions = Run($"X = {OpenDelim}a, b, c{CloseDelim}, write(X)");
        Assert.Single(solutions);
    }
    #endregion

    #region Roundtrip
    [Theory]
    [InlineData("a", "b", "c")]
    [InlineData("x")]
    [InlineData("p", "q", "r", "s")]
    public void Roundtrip_Ground(params string[] elems)
    {
        var vm = SetupVM();
        var astElems = elems.Select(e => (Lang.Ast.Term)(__string)e).ToArray();
        var col = MakeCollection(astElems, EmptyElement);
        var read = Roundtrip(vm, col);
        Assert.IsType<TCollection>(read);
        var readCol = (TCollection)read;
        var head = readCol.Contents.SkipLast(1).ToArray();
        Assert.Equal(elems.Length, head.Length);
    }

    [Fact]
    public void Roundtrip_Empty()
    {
        var vm = SetupVM();
        var read = Roundtrip(vm, EmptyElement);
        Assert.Equal(EmptyElement.Expl, read.Expl);
    }
    #endregion

    #region Unification
    [Fact]
    public void Unify_SameElements_Succeeds()
    {
        var solutions = Run($"{OpenDelim}a, b{CloseDelim} = {OpenDelim}a, b{CloseDelim}");
        Assert.Single(solutions);
    }

    [Fact]
    public void Unify_Variable_Binds()
    {
        var solutions = Run($"X = {OpenDelim}a, b{CloseDelim}");
        Assert.Single(solutions);
    }

    [Fact]
    public void Unify_DifferentElements_Fails()
    {
        var solutions = Run($"{OpenDelim}a, b{CloseDelim} = {OpenDelim}a, c{CloseDelim}");
        Assert.Empty(solutions);
    }
    #endregion
}
