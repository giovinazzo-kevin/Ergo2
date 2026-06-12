using Ergo.Compiler.Analysis.Exceptions;
using Ergo.IO;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.Extensions;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Lang.Lexing;
using Ergo.Lang.Parsing;
using Ergo.Shared.Types;
using System.Diagnostics;

namespace Ergo.Compiler.Analysis;

public class Analyzer
{
    public readonly ModuleLocator ModuleLocator;
    public readonly LibraryLocator LibraryLocator;
    public readonly OperatorLookup Operators;
    public readonly string[] DefaultImports;

    public Analyzer(ModuleLocator? moduleLocator, LibraryLocator libraryLocator, OperatorLookup opLookup, params string[] defaultImports)
    {
        ModuleLocator = moduleLocator ?? ModuleLocator.Default;
        LibraryLocator = libraryLocator;
        Operators = opLookup;
        DefaultImports = defaultImports.Length > 0 ? defaultImports : ["stdlib"];
    }


    protected Module.Stage LoadStage_Link(Module module)
    {
        Trace.WriteLine($"{nameof(LoadStage_Link)}: {module.Name}");
        var libraries = LibraryLocator.Find(module.Name)
            .Select(type => Activator.CreateInstance(type, [module]))
            .Cast<Library>();
        module.Libraries.AddRange(libraries);
        foreach (var builtIns in module.Libraries
            .SelectMany(x => x.ExportedBuiltIns)
            .GroupBy(x => x.Signature)) {
            if (!module.Predicates.TryGetValue(builtIns.Key, out var pred))
                module.Predicates[builtIns.Key] = pred = new Predicate(module, builtIns.Key);
            pred.BuiltIns.AddRange(builtIns);
        }
        Operators.AddRange(module.Libraries.SelectMany(x => x.ExportedOperators).ToArray());
        return Module.Stage.Linked;
    }

    protected Module.Stage LoadStage_Import(Module module)
    {
        Trace.WriteLine($"{nameof(LoadStage_Import)}: {module.Name}");
        module.AbstractParsers.AddRange(
            module.Libraries
                .SelectMany(lib => lib.ExportedAbstractTerms)
                .Where(abs => abs.Parse != null)
                .Select(abs => (Ergo.Lang.Parsing.WellKnown.Delegates.Parse)abs.Parse!));
        return Module.Stage.Imported;
    }

    protected Module.Stage LoadStage_Open(CallGraph graph, Module module, ErgoFileStream stream)
    {
        Trace.WriteLine($"{nameof(LoadStage_Open)}: {module.Name}");
        var lexer = new Lexer(stream, Operators);
        module._parser = new Parser(lexer);
        if (module.AbstractParsers.Count > 0)
            foreach (var factory in module.AbstractParsers) {
                var production = factory(module._parser);
                if (production != null)
                    module._parser.AddAbstractParser(production);
            }
        return Module.Stage.Opened;
    }

    protected Module.Stage LoadStage_Preload(CallGraph graph, Module module)
    {
        Trace.WriteLine($"{nameof(LoadStage_Preload)}: {module.Name}");
        // Load default imports FIRST so their parsers are available for directive parsing
        module.Imports.AddRange(DefaultImports
            .Where(name => module.Name != name)
            .Where(name => !graph.Modules.TryGetValue(name, out var m) || m.LoadStage >= Module.Stage.Linked)
            .Select(name => graph.Modules.TryGetValue(name, out var m) ? m : LoadModule(graph, name)));
        // Inherit abstract term parsers from imports (transitive)
        var visited = new HashSet<__string>();
        void InheritParsers(Module m) {
            if (!visited.Add(m.Name)) return;
            foreach (var factory in m.AbstractParsers) {
                var production = factory(module._parser!);
                if (production != null)
                    module._parser!.AddAbstractParser(production);
            }
            foreach (var sub in m.Imports)
                InheritParsers(sub);
        }
        foreach (var import in module.Imports)
            InheritParsers(import);
        // NOW parse directives (list syntax available from imports)
        var directives = module._parser!.DirectiveDefinitions()
            .GetOr([]);
        if (directives.Length == 0)
            throw new AnalyzerException(AnalyzerError.Module0MustStartWithModuleDirective, module._parser.Lexer.File.Name);
        var declaration = directives[0];
        if (declaration.Functor != "module" || (declaration.Arity != 1 && declaration.Arity != 2))
            throw new AnalyzerException(AnalyzerError.Module0MustStartWithModuleDirective, module._parser.Lexer.File.Name);
        var resolvedDirectives = new List<(Lang.Ast.Directive Ast, Directive Node)>();
        foreach (var dir in directives) {
            var signature = dir.Arg.GetSignature().GetOrThrow().Unqualified;
            var resolved = graph.ResolveDirectives(signature, module).ToArray();
            if (resolved.Length == 0)
                throw new AnalyzerException(AnalyzerError.UnresolvedDirective0, signature);
            resolvedDirectives.AddRange(resolved.Select(r => (dir, r)));
        }
        foreach (var x in resolvedDirectives.OrderBy(x => x.Node.Precedence)) {
            var args = x.Ast.Arg.GetArguments();
            for (int i = 0; i < args.Length; i++)
                args[i] = args[i].Deref();
            x.Node.Execute(module, args.AsSpan());
        }
        return Module.Stage.Preloaded;
    }

    protected static Module.Stage LoadStage_Load(CallGraph graph, Module module)
    {
        Trace.WriteLine($"{nameof(LoadStage_Load)}: {module.Name}");
        var clauseDefs = module._parser!.ClauseOrFactDefinitions()
            .GetOr([]);
        module._parser.Context.ParseRoot.Print();
        var clauseDefsBySig = clauseDefs
            .ToLookup(c => c.Functor.GetSignature()
                .GetOrThrow(new AnalyzerException(AnalyzerError.Clause0HeadCanNotBeAVariable, c.Expl)));
        foreach (var group in clauseDefsBySig) {
            if (!module.Predicates.TryGetValue(group.Key, out var pred))
                module.Predicates[group.Key] = pred = new Predicate(module, group.Key);
        }
        foreach (var group in clauseDefsBySig) {
            var predicate = module.Predicates[group.Key];
            predicate.Clauses.AddRange(group
                .Select(clauseDef => (Def: clauseDef, Clause: new Clause(predicate, clauseDef.Args)))
                .Select(x => x.Clause
                    .WithGoals(x.Def.Goals.SelectMany(goalDef => ResolveGoals(graph, module, x.Clause, goalDef)))));
        }
        module._parser.Dispose();
        module._parser = null;
        return Module.Stage.Loaded;
    }

    protected static IEnumerable<Goal> ResolveQualifiedGoals(CallGraph graph, Module module, Clause clause, Signature signature, __string qualification, Term[] args)
    {
        if (!graph.Modules.TryGetValue(qualification, out var referencedModule))
            yield break;
        var isSelf = referencedModule.Name == module.Name;
        if (!isSelf && !signature.Module.HasValue && !referencedModule.Exports.Contains(signature))
            yield break;
        if (referencedModule.Dynamic.Contains(signature.Unqualified))
            yield return new DynamicGoal(clause, signature.Functor, args);
        else if (referencedModule.Predicates.TryGetValue(signature.Unqualified, out var callee))
            yield return new StaticGoal(clause, callee, args);
    }

    protected static IEnumerable<Goal> ResolveGoals(CallGraph graph, Module module, Clause clause, Term goalDef)
    {
        var args = goalDef.GetArguments();
        if (!goalDef.GetSignature().TryGetValue(out var signature)) {
            if (goalDef is Variable lateBound)
                return [new LateBoundGoal(clause, lateBound)];
            throw new NotSupportedException();
        }
        if (signature.Functor == Literals.Cut && signature.Arity.TryGetValue(out var cutArity) && cutArity == 0)
            return [new Cut(clause)];
        if (signature.Module.TryGetValue(out var qualification)) {
            if (!graph.Modules.TryGetValue(qualification, out var referencedModule))
                throw new AnalyzerException(AnalyzerError.UndefinedModule0, qualification);
            return ResolveQualifiedGoals(graph, module, clause, signature, qualification, args);
        }
        var resolved = graph.Modules.Keys
            .Except([module.Name])
            .Prepend(module.Name)
            .SelectMany(m => ResolveQualifiedGoals(graph, module, clause, signature, m, args))
            .ToList();
        if (resolved.Count == 0)
            throw new AnalyzerException(AnalyzerError.UndefinedPredicate0, signature);
        return resolved;
    }

    public Module LoadModule(CallGraph graph, Either<string, ErgoFileStream> either)
    {
        ErgoFileStream fs;
        string name;
        if (either is Case<string> { Value: var moduleName }) {
            name = moduleName;
            ModuleLocator.Index.Update();
            var fileInfo = ModuleLocator.Index.Find(moduleName).First();
            fs = ErgoFileStream.Open(fileInfo);
        } else {
            fs = either;
            name = Path.GetFileNameWithoutExtension(fs.Name);
        }
        if (!graph.Modules.TryGetValue(name, out var module))
            module = graph.Modules[name] = new(graph, name);
        if (module.LoadStage < Module.Stage.Linked)
            module.LoadStage = LoadStage_Link(module);
        if (module.LoadStage < Module.Stage.Imported)
            module.LoadStage = LoadStage_Import(module);
        if (module.LoadStage < Module.Stage.Opened)
            module.LoadStage = LoadStage_Open(graph, module, fs);
        if (module.LoadStage < Module.Stage.Preloaded) {
            module.LoadStage = Module.Stage.Preloaded;
            LoadStage_Preload(graph, module);
        }
        if (module.LoadStage < Module.Stage.Loaded) {
            module.LoadStage = Module.Stage.Loaded;
            LoadStage_Load(graph, module);
        }
        return module;
    }

    public CallGraph LoadModule(ErgoFileStream fs)
    {
        var moduleName = Path.GetFileNameWithoutExtension(fs.Name);
        var graph = new CallGraph(this, moduleName);
        LoadModule(graph, fs);
        return graph;
    }

    public CallGraph LoadModule(string name)
    {
        var graph = new CallGraph(this, name);
        LoadModule(graph, name);
        return graph;
    }
}
