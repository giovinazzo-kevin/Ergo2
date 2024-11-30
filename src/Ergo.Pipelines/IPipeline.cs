using Ergo.Shared.Types;

namespace Ergo.Pipelines;

public interface IPipeline
{
    Type InterType { get; }
    Type OutputType { get; }
    Type EnvType { get; }
}

public interface IPipeline<TInput, TOutput, in TEnv> : IPipeline
{
    Type IPipeline.InterType => typeof(TInput);
    Type IPipeline.OutputType => typeof(TOutput);
    Type IPipeline.EnvType => typeof(TEnv);
    Result<TOutput, PipelineError> Run(TInput input, TEnv environment);
}
