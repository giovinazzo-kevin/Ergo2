using Ergo.IO;
using Ergo.Shared.Types;

var ARGS = new Args(args);
var INPUT = ARGS.Get(0, Args.File);






class Args
{
    readonly string[] _positional;
    readonly HashSet<string> _flags;
    readonly HashSet<string> _named;

    public Args(string[] args)
    {
        for (var i = 0; i < args.Length; ++i)
        {
            args[i] = args[i].Trim();
            if (string.IsNullOrWhiteSpace(args[i].Replace("-", "")))
                continue;
            if (args[i].Length >= 1)
        }
    }

    public Maybe<string> Get(int index) => Get(index, s => s);
    public Maybe<T> Get<T>(int index, string name, Func<string, T> parse)
    {
        if (_named)
        if (index >= _positional.Length)
            return default;
        return parse(_positional[index]);
    }
    public static ErgoFileStream File(string path) =>
        ErgoFileStream.Open(new FileInfo(path));
}