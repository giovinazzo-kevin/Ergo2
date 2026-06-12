using Ergo.Shared.Types;

namespace Ergo.Pipelines;

public sealed class OrPipeline<TInput, TOutput>(
    Pipeline<TInput, TOutput> left,
    Pipeline<TInput, TOutput> right
) : Pipeline<TInput, TOutput>([new OrStep<TInput, TOutput>(left, right)], [default(Unit)!])
{
}

internal class OrStep<TInput, TOutput>(
    Pipeline<TInput, TOutput> left,
    Pipeline<TInput, TOutput> right
) : IPipeline<TInput, TOutput, Unit>
{
    public Result<TOutput, PipelineError> Run(TInput input, Unit _)
    {
        var result = left.Run(input);
        if (result is Success<TOutput>)
            return result;
        return right.Run(input);
    }
}
