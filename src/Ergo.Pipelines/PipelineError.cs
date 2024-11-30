using Ergo.Shared.Interfaces;

namespace Ergo.Pipelines;
public record PipelineError(IPipeline Step, Exception Exception) : IException;
