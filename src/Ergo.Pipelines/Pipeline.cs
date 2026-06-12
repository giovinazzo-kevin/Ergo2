using Ergo.Compiler.Emission;
using Ergo.Pipelines.Compiler;
using Ergo.Shared.Types;
using System.Reflection;

namespace Ergo.Pipelines;

public sealed class Pipeline
{
    public static readonly Pipeline<SourceInput, KnowledgeBase> Consult =
        WithStep(Steps.Consult);

    public static readonly CompileQuery CompileQuery = Ergo.Pipelines.Compiler.CompileQuery.Instance;

    public static Pipeline<SourceInput, KnowledgeBase> Compile =>
        WithStep(Steps.LoadSource)
            .WithStep(Steps.Analyze)
            .WithStep(Steps.Compile, new Compiler.Compile.Env { SaveToPath = "./bin/" });

    public static Pipeline<TInput, TOutput> WithStep<TInput, TOutput, TEnv>(IPipeline<TInput, TOutput, TEnv> firstStep, TEnv? env = default)
        where TEnv : class, new()
        => new([firstStep], [env ?? new()]);
}

public class Pipeline<TInput, TOutput>(IPipeline[] steps, object[] envs)
    : IPipeline<TInput, TOutput, Unit>
{
    public Pipeline<TInput, TOutput> Or(Pipeline<TInput, TOutput> fallback)
        => new OrPipeline<TInput, TOutput>(this, fallback);

    private readonly (MethodInfo ExecuteStep, PropertyInfo GetResult)[] MethodTable = steps
        .Select((step, i) => {
            var executeStep = typeof(IPipeline<,,>)
                .MakeGenericType(step.InterType, step.OutputType, step.EnvType)
                .GetMethod(nameof(IPipeline<Unit, Unit, Unit>.Run), BindingFlags.Instance | BindingFlags.Public);
            var success = typeof(Success<>)
                .MakeGenericType(step.OutputType);
            var getResult = success
                .GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
            return (executeStep!, getResult!);
        })
        .ToArray();

    public Pipeline<TInput, TNext> WithStep<TNext, TEnv>(IPipeline<TOutput, TNext, TEnv> next, TEnv? env = default)
        where TEnv : class, new()
        => new([.. steps, next], [.. envs, env ?? new()]);

    Result<TOutput, PipelineError> IPipeline<TInput, TOutput, Unit>.Run(TInput input, Unit _)
    {
        var result = new object[1] { input! };
        var data = (object)input!;
        for (int i = 0; i < steps.Length; i++) {
            ref var step = ref steps[i];
            try {
                data = MethodTable[i].ExecuteStep
                    .Invoke(step, [result[0], envs[i]])!;
                if (data is Error<PipelineError> { Value: var error })
                    return error;
                result[0] = MethodTable[i]!.GetResult
                    .GetValue(data)!;
            }
            catch (TargetInvocationException ex) {
                return new PipelineError(step, ex.InnerException ?? ex);
            }
        }
        return ((Success<TOutput>)data!).Value;
    }

    public virtual Result<TOutput, PipelineError> Run(TInput input) => ((IPipeline<TInput, TOutput, Unit>)this).Run(input, default);
}
