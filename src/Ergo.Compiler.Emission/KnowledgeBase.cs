using Ergo.IO;
using Ergo.Lang.Ast;
using Ergo.Lang.Lexing;
using System.Text;

namespace Ergo.Compiler.Emission;

public class KnowledgeBase
{
    const string BIN_HEADER = "ERGO_KB";
    internal int PC = 0;

    public readonly string Name;
    public readonly Dictionary<string, int> Labels = [];
    public readonly OperatorLookup Operators = new();
    public ReadOnlyMemory<byte> Memory { get; internal set; }
    internal KnowledgeBase(string name) { Name = name; }
    public int GetLabel(Analysis.Predicate pred)
        => Labels[PredicateLabel(pred)];
    public int SetLabel(Analysis.Predicate pred)
        => Labels[PredicateLabel(pred)] = PC;
    protected static string PredicateLabel(Analysis.Predicate predicate)
        => predicate.Signature.Expl;
    public int GetLabel(Analysis.Clause clause) 
        => Labels[ClauseLabel(clause)];
    public int SetLabel(Analysis.Clause clause)
        => Labels[ClauseLabel(clause)] = PC;
    protected static string ClauseLabel(Analysis.Clause clause)
        => clause.Parent.Signature.Expl + "_" + (clause.Parent.Clauses.IndexOf(clause) + 1);

    public static ErgoFileStream Serialize(KnowledgeBase kb)
    {
        var fs = ErgoFileStream.Create(null, $"{kb.Name}.kb");
        using var writer = new BinaryWriter(fs.Stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(BIN_HEADER);
        writer.Write(kb.Operators.Values.Count);
        foreach (var op in kb.Operators.Values)
        {
            writer.Write(op.Precedence);
            writer.Write((byte)op.Type_);
            writer.Write(op.Functors.Length);
            foreach (var fun in op.Functors)
                writer.Write((string)fun.Value);
        }
        writer.Write(kb.Memory.Length);
        writer.Write(kb.Memory.ToArray());
        foreach (var (key, value) in kb.Labels.OrderBy(x => x.Value))
        {
            writer.Write(key);
            writer.Write(value);
        }
        return fs;
    }

    public static KnowledgeBase Deserialize(ErgoFileStream fs)
    {
        using var reader = new BinaryReader(fs.Stream, Encoding.UTF8, leaveOpen: true);
        var bin_header = reader.ReadString();
        if (bin_header != BIN_HEADER)
            throw new NotSupportedException();
        var name = Path.GetFileNameWithoutExtension(fs.Name);
        var kb = new KnowledgeBase(name);
        var ops = new List<Operator>();
        var numOperators = reader.ReadInt32();
        for (int i = 0; i < numOperators; i++)
        {
            var precedence = reader.ReadInt32();
            var type = (Operator.Type)reader.ReadByte();
            var numFunctors = reader.ReadInt32();
            var functors = new Atom[numFunctors];
            for (int j = 0; j < numFunctors; j++)
                functors[j] = reader.ReadString();
            ops.Add(new(precedence, type, functors));
        }
        kb.Operators.AddRange(ops);
        kb.PC = reader.ReadInt32();
        kb.Memory = reader.ReadBytes(kb.PC);
        do
        {
            var key = reader.ReadString();
            var value = reader.ReadInt32();
            kb.Labels[key] = value;
        }
        while (reader.BaseStream.Position < reader.BaseStream.Length);
        return kb;
    }
}
