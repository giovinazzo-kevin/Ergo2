using Ergo.Lang.Ast;
using Ergo.Pipelines;
using Ergo.Pipelines.Analysis;
using Ergo.Pipelines.Compiler;
using Ergo.Pipelines.IO;
using Ergo.Shared.Types;
using System.Runtime.ExceptionServices;

namespace Ergo.UnitTests;

public class PipelineTests
{
    [Fact]
    public void Test()
    {
        var pipeline = Pipeline
            .WithStep(Steps.LoadSource, new() { SaveToPath = @".\ergo" })
            .WithStep(Steps.Analyze)
            .WithStep(Steps.Compile, new() { SaveToPath = @".\ergo\bin" });
        var result = pipeline.Run(new LoadSource.File("pipeline_tests.ergo", ":- module(_, [])."));
        result.EnsureSuccess();
        result = pipeline.Run((__string)"pipeline_tests");
        result.EnsureSuccess();
    }
}