using Ergo.Shared.Exceptions;

namespace Ergo.Compiler.Analysis.Exceptions;

public class AnalyzerException(AnalyzerError error, params object[] args) : ErgoException<AnalyzerError>(error, args);