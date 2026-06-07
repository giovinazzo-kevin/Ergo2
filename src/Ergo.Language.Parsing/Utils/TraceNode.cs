using System.Diagnostics;

namespace Ergo.Lang.Parsing;

public class TraceNode
{
    public string? Method { get; set; }
    public string? Value { get; set; }
    public bool Success { get; set; } = true;
    public char Status => (Success ? '✔' : '✖');
    public List<TraceNode> Children { get; } = [];
    public TraceNode? Parent { get; set; }

    public void Print(string indent = "", bool isLast = true, bool excludeFailures = true, int depth = 0)
    {
        // Print if success OR we are at the top level (depth 0 or 1) even if failure
        if (!Success && excludeFailures && depth > 1)
            return;

#if PARSER_TRACE
        Trace.WriteLine($"{indent}{(isLast ? "└── " : "├── ")}{this}");
#endif
        var visible = Children
            .Where(c => c.Success || !excludeFailures || depth < 1) // allow failed children at top level
            .ToList();

        for (int i = 0; i < visible.Count; i++)
            visible[i].Print(indent + (isLast ? "    " : "│   "), i == visible.Count - 1, excludeFailures, depth + 1);
    }

    public override string ToString()
    {
        return $"{Status} {Method}: {Value?.Replace("\n", "")}";
    }

}
