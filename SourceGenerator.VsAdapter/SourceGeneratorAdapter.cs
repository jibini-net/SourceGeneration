using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SourceGenerator.VsAdapter
{
    [Generator]
    public class SourceGeneratorAdapter : ISourceGenerator
    {
#if DEBUG
        public static readonly string BuildMode = "Debug";
#else
        public static readonly string BuildMode = "Release";
#endif
        public static readonly string DotNetVersion = "net7.0";
        public static string ToolPath => $"Tools/SourceGenerator/{BuildMode}/{DotNetVersion}";
        public static string CallingPath = "";

        internal string ReadEmbeddedResource(string fileName)
        {
            var assy = GetType().Assembly;
            var streamName = $"{assy.GetName().Name}.{fileName}";

            using (var file = assy.GetManifestResourceStream(streamName))
            using (var reader = new StreamReader(file))
            {
                return reader.ReadToEnd();
            }
        }

        private string _INCLUDES;
        public string INCLUDES => _INCLUDES is null
            ? _INCLUDES = ReadEmbeddedResource("_Includes.txt")
            : _INCLUDES;

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

            var findExts = new[] { ".model", ".view" }.ToImmutableHashSet();
            var files = context.AdditionalFiles
                .Where((it) => findExts.Contains(
                    Path.GetExtension(it.Path).ToLowerInvariant()))
                .ToList();
            var completed = new SemaphoreSlim(0, files.Count);
            var sources = new BlockingCollection<(string file, MemoryStream source)>();

            foreach (var file in files)
            {
                async Task detachAsync()
                {
                    try
                    {
                        var name = Path.GetFileName(file.Path);
                        var source = await ExecuteProcess(file);

                        sources.Add(($"{name}.g.cs", source));
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
            context.AddSource("_ServiceCollection.g.cs", GenerateServiceCollection(files));
            context.AddSource("_ViewCollection.g.cs", GenerateViewCollection(files));
            foreach (var source in sources)
            {
                var sourceText = SourceText.From(source.source, Encoding.UTF8, canBeEmbedded: true);
                context.AddSource(source.file, sourceText);
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        private static string GenerateServiceCollection(List<AdditionalText> files)
        {
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("namespace Generated;");
            sourceBuilder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            foreach (var file in files.Where((it) => Path.GetExtension(it.Path).ToLowerInvariant() == ".model"))
            {
                var name = Path.GetFileNameWithoutExtension(file.Path);
                sourceBuilder.AppendFormat("public static class {0}Extensions\n{{\n", name);

                sourceBuilder.AppendFormat("    public static void Add{0}Backend<T>(this IServiceCollection services)\n", name);
                sourceBuilder.AppendFormat("        where T : class, {0}.IBackendService\n    {{\n", name);
                sourceBuilder.AppendFormat("        services.AddScoped<{0}.Repository>();\n", name);
                sourceBuilder.AppendFormat("        services.AddScoped<{0}.IBackendService, T>();\n", name);
                sourceBuilder.AppendFormat("        services.AddScoped<{0}.DbService>();\n", name);
                sourceBuilder.AppendFormat("        services.AddScoped<{0}.IService>((sp) => sp.GetRequiredService<{1}.DbService>());\n",
                    name,
                    name);
                sourceBuilder.AppendLine("    }");

                sourceBuilder.AppendFormat("    public static void Add{0}Frontend(this IServiceCollection services)\n    {{\n", name);
                sourceBuilder.AppendFormat("        services.AddScoped<{0}.ApiService>();\n", name);
                sourceBuilder.AppendFormat("        services.AddScoped<{0}.IService>((sp) => sp.GetRequiredService<{1}.ApiService>());\n",
                    name,
                    name);
                sourceBuilder.AppendLine("    }");

                sourceBuilder.AppendLine("}");
            }
            return sourceBuilder.ToString();
        }

        private static string GenerateViewCollection(List<AdditionalText> files)
        {
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("namespace Generated;");
            sourceBuilder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            foreach (var file in files.Where((it) => Path.GetExtension(it.Path).ToLowerInvariant() == ".view"))
            {
                var name = Path.GetFileNameWithoutExtension(file.Path);
                sourceBuilder.AppendFormat("public static class {0}Extensions\n{{\n", name);

                sourceBuilder.AppendFormat("    public static void Add{0}View<T>(this IServiceCollection services)\n", name);
                sourceBuilder.AppendFormat("        where T : class, {0}Base.IView\n    {{\n", name);
                sourceBuilder.AppendFormat("        services.AddScoped<T>();\n", name);
                sourceBuilder.AppendFormat("        services.AddScoped<{0}Base.IView>((sp) => sp.GetRequiredService<T>());\n",
                    name);
                sourceBuilder.AppendLine("    }");

                sourceBuilder.AppendLine("}");
            }
            return sourceBuilder.ToString();
        }
    }
}