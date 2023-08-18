using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SourceGenerator.VsAdapter
{
    [Generator]
    public class ModelSourceGenerator : ISourceGenerator
    {
#if DEBUG
        public static readonly string BuildMode = "Debug";
#else
        public static readonly string BuildMode = "Release";
#endif
        public static readonly string DotNetVersion = "net7.0";
        public static string ToolPath => $"Tools/SourceGenerator/{BuildMode}/{DotNetVersion}";
        public static string CallingPath = "";

        public const string INCLUDES = "" +
            "public interface IModelDbAdapter\n{\n" +
            "    void Execute(string procName, object args);\n" +
            "    T Execute<T>(string procName, object args);\n" +
            "    T ExecuteForJson<T>(string procName, object args);\n" +
            "}\n" +
            "public interface IModelApiAdapter\n{\n" +
            "    void Execute(string path, object args);\n" +
            "    T Execute<T>(string path, object args);\n" +
            "}\n" +
            "public interface IModelDbWrapper\n{\n" +
            "    void Execute(Action impl);\n" +
            "    T Execute<T>(Func<T> impl);\n" +
            "}\n" ;

        public static async Task<MemoryStream> ExecuteProcess(AdditionalText file)
        {
            var processInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = Path.Combine(CallingPath, ToolPath, "SourceGenerator.exe"),
                Arguments = $"{'"'}{file.Path}{'"'}",
                WorkingDirectory = Path.Combine(CallingPath, ToolPath)
            };

            using (var process = Process.Start(processInfo))
            {
                var source = new MemoryStream();
                await process.StandardOutput.BaseStream.CopyToAsync(source);
                source.Position = 0;

                process.WaitForExit();
                switch (process.ExitCode)
                {
                    case 0:
                        return source;

                    default:
                        source.Dispose();
                        var error = process.StandardError.ReadToEnd();
                        throw new Exception(error);
                }
            }
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (string.IsNullOrEmpty(CallingPath))
            {
                CallingPath = context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir", out var result)
                    ? result
                    : throw new Exception("Cannot determine calling path");
            }

            var files = context.AdditionalFiles.Where((it) => it.Path.ToLowerInvariant().EndsWith(".model")).ToList();
            var completed = new SemaphoreSlim(0, files.Count);
            var sources = new BlockingCollection<(string file, MemoryStream source)>();

            foreach (var file in files)
            {
                async Task detachAsync()
                {
                    try
                    {
                        var name = Path.GetFileNameWithoutExtension(file.Path);
                        var source = await ExecuteProcess(file);

                        sources.Add(($"{name}.Model.g.cs", source));
                    } catch (Exception ex)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "SG001",
                                "",
                                ex.Message,
                                "",
                                DiagnosticSeverity.Error,
                                true),
                            null));
                    } finally
                    {
                        completed.Release();
                    }
                }
                _ = detachAsync();
            }
            for (var i = 0; i < files.Count; i ++)
            {
                completed.Wait();
            }

            // Add all sources from main thread for safety
            context.AddSource("_Includes.g.cs", INCLUDES);
            //TODO context.AddSource("_ServiceCollection.g.c", GenerateServiceCollection());
            foreach (var source in sources)
            {
                var sourceText = SourceText.From(source.source, Encoding.UTF8, canBeEmbedded: true);
                context.AddSource(source.file, sourceText);
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}