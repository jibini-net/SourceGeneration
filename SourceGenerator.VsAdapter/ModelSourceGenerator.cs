using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

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
        public static string ToolPath => $"Tools/SourceGenerator/{BuildMode}/{DotNetVersion}/SourceGenerator.exe";
        public static string CallingPath = "";

        public static string ExecuteProcess(GeneratorExecutionContext context)
        {
            var processInfo = new ProcessStartInfo()
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = Path.Combine(CallingPath, ToolPath),
                Arguments = $"{CallingPath}",
                
            };
            using (var process = Process.Start(processInfo))
            {
                // Source can overflow the buffer! Take it in pieces
                var output = new StringBuilder();
                for (var i = 0; !process.WaitForExit(10); i++)
                {
                    output.Append(process.StandardOutput.ReadToEnd());
                    // 10ms * 1,000 = 10,000ms = 10s
                    if (i > 1000)
                    {
                        throw new Exception("Source generator process timed out");
                    }
                }

                switch (process.ExitCode)
                {
                    case 0:
                        return output.ToString();

                    default:
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

            try
            {
                context.AddSource("GeneratedModels.g.cs", SourceText.From(ExecuteProcess(context), Encoding.UTF8));
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
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}