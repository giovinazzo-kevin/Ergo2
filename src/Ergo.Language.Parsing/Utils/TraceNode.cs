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

    public void Print(string indent = "", bool isLast = true, bool excludeFailures = true)
    {
        if (!Success && excludeFailures)
            return;
        Trace.WriteLine($"{indent}{(isLast ? "└── " : "├── ")}{this}");
        var visible = Children.Where(c => c.Success || !excludeFailures).ToList();
        for (int i = 0; i < visible.Count; i++)
            visible[i].Print(indent + (isLast ? "    " : "│   "), i == visible.Count - 1, excludeFailures);
    }
    public override string ToString()
    {
        return $"{Status} {Method}: {Value?.Replace("\n", "")}";
    }

}
