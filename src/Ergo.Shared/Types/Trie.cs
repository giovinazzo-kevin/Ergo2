namespace Ergo.Shared.Types;

public class Trie<TToken, TValue>
    where TToken : notnull
{
    class Node
    {
        public Dictionary<TToken, Node> Children { get; } = [];
        public Maybe<TValue> Value { get; set; }
        public bool IsEndOfToken => Value.HasValue;
    }


    private readonly Node root = new();

    public TValue Insert(IEnumerable<TToken> word, TValue value)
    {
        var node = root;
        foreach (var c in word)
        {
            if (!node.Children.ContainsKey(c))
                node.Children[c] = new Node();
            node = node.Children[c];
        }
        node.Value = value;
        return value;
    }

    public Maybe<TValue> Match(IEnumerable<TToken> input)
    {
        var node = root;
        foreach (var c in input)
        {
            if (!node.Children.TryGetValue(c, out node))
                return default;
        }
        return node.Value;
    }
}