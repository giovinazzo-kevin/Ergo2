using Ergo.IO;
using Ergo.Lang.Ast;
using Ergo.Shared.Types;
using System.Text;

namespace Ergo.Pipelines.IO;

public sealed class LoadSource : IPipeline<Either<__string, FileInfo, FileStream, LoadSource.File>, ErgoFileStream, LoadSource.Env>
{
    public class Env
    {
        /// <summary>
        /// When creating a file, also writes the file to disk at the given path. 
        /// Otherwise, the file is only kept in memory.
        /// </summary>
        public string? SaveToPath { get; set; } = null;
        public ModuleLocator ModuleLocator { get; set; } = ModuleLocator.Default;
    }
    public readonly record struct File(string Name, string Contents);

    internal static readonly LoadSource Instance = new ();
    private LoadSource() { }

    public Result<ErgoFileStream, PipelineError> Run(Either<__string, FileInfo, FileStream, File> input, Env env)
    {
        return input switch
        {
            Case<File> { Value: var file } => Create(file, env),
            Case<FileInfo> { Value: var file } => Open(file, env),
            Case<FileStream> { Value: var stream } => Open(stream, env),
            Case<__string> { Value: var name } => Locate(name, env),
            _ => default!,
        };
    }

    static ErgoFileStream Locate(__string input, Env env)
    {;
        env.ModuleLocator.Index.Update();
        var fileInfo = env.ModuleLocator.Index.Find(input).First();
        return Open(fileInfo, env);
    }

    static ErgoFileStream Create(File input, Env env)
    {
        if (env.SaveToPath is not null)
        {
            var directoryInfo = new DirectoryInfo(env.SaveToPath);
            if (!directoryInfo.Exists)
                directoryInfo.Create();
            System.IO.File.WriteAllText(Path.Combine(directoryInfo.FullName, input.Name), input.Contents, new UTF8Encoding());
        }
        return ErgoFileStream.Create(input.Contents, input.Name);
    }

    static ErgoFileStream Open(FileInfo input, Env env)
    {
        if (!input.Exists)
            throw new FileNotFoundException(input.FullName);
        return ErgoFileStream.Open(input);
    }

    static ErgoFileStream Open(FileStream input, Env env)
    {
        return ErgoFileStream.Open(input);
    }
}
