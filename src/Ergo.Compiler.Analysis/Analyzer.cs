using Ergo.Compiler.Analysis.Exceptions;
using Ergo.IO;
using Ergo.Lang.Ast;
using Ergo.Lang.Ast.Extensions;
using Ergo.Lang.Ast.WellKnown;
using Ergo.Lang.Lexing;
using Ergo.Lang.Parsing;
using Ergo.Shared.Types;
using System.Xml.Linq;

namespace Ergo.Compiler.Analysis;

public class Analyzer
{
    public readonly ModuleLocator ModuleLocator;
    public readonly LibraryLocator LibraryLocator;
    public readonly OperatorLookup Operators;
    public readonly Module? DefaultImport;

    public Analyzer(ModuleLocator? moduleLocator, LibraryLocator libraryLocator, OperatorLookup opLookup, string defaultImport = "prologue")
    {
        ModuleLocator = moduleLocator ?? ModuleLocator.Default;
        LibraryLocator = libraryLocator;
        Operators = opLookup;
        if(!string.IsNullOrEmpty(defaultImport))
        {
            var stream = ModuleLocator.Index.Find(defaultImport).Single();
            var ergoStream = ErgoFileStream.Open(stream);
            DefaultImport = LoadModule(ergoStream).Modules[defaultImport];
        }
    }


    protected Module.Stage LoadStage_Link(Module module)
    {
        var libraries = LibraryLocator.Find(module.Name)
            .Select(type => Activator.CreateInstance(type, [module]))
            .Cast<Library>();
        module.Libraries.AddRange(libraries);
        foreach (var builtIns in module.Libraries
            .SelectMany(x => x.ExportedBuiltIns)
            .GroupBy(x => x.Signature))
        {
            if (!module.Predicates.TryGetValue(builtIns.Key, out var pred))
                module.Predicates[builtIns.Key] = pred = new Predicate(module, builtIns.Key);
            pred.BuiltIns.AddRange(builtIns);
        }
        return Module.Stage.Linked;
    }

    protected Module.Stage LoadStage_Open(CallGraph graph, Module module, ErgoFileStream stream)
    {
        var lexer = new Lexer(stream, Operators);
        module._parser = new Parser(lexer);
        return Module.Stage.Opened;
    }

    protected Module.Stage LoadStage_Preload(CallGraph graph, Module module)
    {
        var directives = module._parser!.DirectiveDefinitions()
            .GetOr([]);
        if (directives.Length == 0)
            throw new AnalyzerException(AnalyzerError.Module0MustStartWithModuleDirective, module._parser.Lexer.File.Name);
        var declaration = directives[0];
        if (declaration.Functor != "module" || declaration.Arity != 2)
            throw new AnalyzerException(AnalyzerError.Module0MustStartWithModuleDirective, module._parser.Lexer.File.Name);
        if (DefaultImport != null)
            module.Imports.Add(DefaultImport);
        var resolvedDirectives = new List<(Lang.Ast.Directive Ast, Directive Node)>();
        foreach (var dir in directives)
        {
            var signature = dir.Arg.GetSignature().GetOrThrow().Unqualified;
            var resolved = graph.ResolveDirectives(signature, module).ToArray();
            if (resolved.Length == 0)
                throw new AnalyzerException(AnalyzerError.UnresolvedDirective0, signature);
            resolvedDirectives.AddRange(resolved.Select(r => (dir, r)));
        }
        foreach (var x in resolvedDirectives.OrderBy(x => x.Node.Precedence))
        {
            var args = x.Ast.Arg.GetArguments();
            for (int i = 0; i < args.Length; i++)
                args[i] = args[i].Deref();
            x.Node.Execute(module, args.AsSpan());
        }
        return Module.Stage.Preloaded;
    }

    protected static Module.Stage LoadStage_Load(CallGraph graph, Module module)
    {
        var clauseDefs = module._parser!.ClauseOrFactDefinitions()
            .GetOr([]);
        var clauseDefsBySig = clauseDefs
            .ToLookup(c => c.Functor.GetSignature()
                .GetOrThrow(new AnalyzerException(AnalyzerError.Clause0HeadCanNotBeAVariable, c.Expl)));
        foreach (var group in clauseDefsBySig)
        {
            if (!module.Predicates.TryGetValue(group.Key, out var pred))
                module.Predicates[group.Key] = pred = new Predicate(module, group.Key);
        }
        foreach (var group in clauseDefsBySig)
        {
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

    protected static List<Goal> ResolveQualifiedGoals(CallGraph graph, Module module, Clause clause, Signature signature, __string qualification, Term[] args)
    {
        if (!graph.Modules.TryGetValue(qualification, out var referencedModule))
            return [];
        if (signature.Functor == Literals.Cut && signature.Arity == 0)
            return [new Cut(clause)];
        if (!signature.Module.HasValue && !referencedModule.Exports.Contains(signature))
            return [];
        var list = new List<Goal>();
        if (referencedModule.Dynamic.Contains(signature.Unqualified))
            list.Add(new DynamicGoal(clause, signature.Functor, args));
        else if (!referencedModule.Predicates.TryGetValue(signature.Unqualified, out var callee))
            throw new AnalyzerException(AnalyzerError.UndefinedPredicate0, signature);
        else
            list.Add(new StaticGoal(clause, callee, args));
        return list;
    }

    protected static List<Goal> ResolveGoals(CallGraph graph, Module module, Clause clause, Term goalDef)
    {
        var args = goalDef.GetArguments();
        if (!goalDef.GetSignature().TryGetValue(out var signature))
        {
            if (goalDef is Variable lateBound)
                return [new LateBoundGoal(clause, lateBound)];
            throw new NotSupportedException();
        }
        if (signature.Module.TryGetValue(out var qualification))
        {
            if (!graph.Modules.TryGetValue(qualification, out var referencedModule))
                throw new AnalyzerException(AnalyzerError.UndefinedModule0, qualification);
            return ResolveQualifiedGoals(graph, module, clause, signature, qualification, args);
        }
        return graph.Modules.Keys
            .Except([module.Name])
            .Prepend(module.Name)
            .SelectMany(m => ResolveQualifiedGoals(graph, module, clause, signature, m, args))
            .ToList();
    }
    
    public Module LoadModule(CallGraph graph, Either<string, ErgoFileStream> either)
    {
        ErgoFileStream fs;
        if (either is Case<string> { Value: var moduleName })
        {
            ModuleLocator.Index.Update();
            var fileInfo = ModuleLocator.Index.Find(moduleName).First();
            fs = ErgoFileStream.Open(fileInfo);
        }
        else fs = either;
        if (!graph.Modules.TryGetValue(graph.Root, out var module))
            module = graph.Modules[graph.Root] = new(graph, graph.Root);
        if (module.LoadStage < Module.Stage.Linked)
            module.LoadStage = LoadStage_Link(module);
        if (module.LoadStage < Module.Stage.Opened)
            module.LoadStage = LoadStage_Open(graph, module, fs);
        if (module.LoadStage < Module.Stage.Preloaded)
            module.LoadStage = LoadStage_Preload(graph, module);
        if (module.LoadStage < Module.Stage.Loaded)
            module.LoadStage = LoadStage_Load(graph, module);
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
