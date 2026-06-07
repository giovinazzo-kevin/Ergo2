using Ergo.Shared.Types;

namespace Ergo.Pipelines;

public sealed class OrPipeline<TInput, TOutput>(
    Pipeline<TInput, TOutput> left,
    Pipeline<TInput, TOutput> right
) : Pipeline<TInput, TOutput>([], [])
{
    public override Result<TOutput, PipelineError> Run(TInput input)
    {
        var result = left.Run(input);
        if (result is Success<TOutput>)
            return result;
        return right.Run(input);
    }
}
