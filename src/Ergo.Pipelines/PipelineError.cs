namespace Ergo.Pipelines;
public record PipelineError(IPipeline Step, Exception Exception);
