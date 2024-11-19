using System.Text;

namespace Ergo.IO;

public record ErgoFileStream(Stream Stream, string Name)
{
    private readonly BinaryReader _utf8reader = new(Stream, Encoding.UTF8, true);
    public bool Eof => Stream.Position >= Stream.Length;
    public char ReadUTF8Char()
    {
        if (Stream.Position >= Stream.Length)
            throw new Exception("Error: Read beyond EOF");
        var numRead = Math.Min(4, (int)(Stream.Length - Stream.Position));
        var bytes = _utf8reader.ReadBytes(numRead);
        var chars = Encoding.UTF8.GetChars(bytes);
        if (chars.Length == 0)
            throw new Exception("Error: Invalid UTF8 char");
        var charLen = Encoding.UTF8.GetByteCount(new char[] { chars[0] });
        Stream.Position += charLen - numRead;
        return chars[0];
    }
    public void Save(string? newName = null)
    {
        var fi = new FileInfo(newName ?? Name);
        if (!(fi.Directory?.Exists ?? true))
            fi.Directory.Create();
        using var fs = fi.Open(FileMode.OpenOrCreate);
        var pos = Stream.Position;
        Stream.Seek(0, SeekOrigin.Begin);
        Stream.CopyTo(fs);
        Stream.Seek(pos, SeekOrigin.Begin);
    }
    public static ErgoFileStream Open(FileInfo fileInfo)
    {
        var fs = fileInfo.OpenRead();
        return new(fs, fileInfo.Name);
    }
    public static ErgoFileStream Open(FileStream stream)
    {
        return new(stream, stream.Name);
    }
    public static ErgoFileStream Create(string? contents, string fileName)
    {
        var ms = new MemoryStream();
        if (!string.IsNullOrEmpty(contents))
        {
            var sw = new StreamWriter(ms, leaveOpen: true);
            sw.Write(contents);
            sw.Dispose();
            ms.Seek(0, SeekOrigin.Begin);
        }
        return new(ms, fileName);
    }
}

