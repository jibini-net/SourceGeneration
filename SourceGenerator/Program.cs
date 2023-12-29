﻿using SourceGenerator.Grammar;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace SourceGenerator;

using static Token;

internal class Program
{
    private static readonly StringBuilder sourceBuilder = new();

    public static void Append(string source, params object[] args)
    {
        sourceBuilder.Append(string.Format(source, args));
    }

    public static void AppendLine(string source, params object[] args)
    {
        sourceBuilder.AppendLine(string.Format(source, args));
    }

    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: SourceGenerator.exe {ModelFile}");
            Process.GetCurrentProcess().Kill();
        }
        var sourceFile = args[0];
        Console.OutputEncoding = Encoding.UTF8;
        AppendLine("/* DO NOT EDIT THIS FILE */");

        var initTime = DateTime.Now;
        var binary = new BinaryFormatter();
        Fsa dfa;
        if (File.Exists("GrammarDfaCache.bin"))
        {
            using var inStream = File.OpenRead("GrammarDfaCache.bin");
#pragma warning disable SYSLIB0011
            dfa = (Fsa)binary.Deserialize(inStream);
#pragma warning restore SYSLIB0011

            AppendLine($"// DFA RESTORED IN {(DateTime.Now - initTime).TotalMilliseconds}ms");
        } else
        {
            var letters = "a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|y|z";
            var capLetters = letters.ToUpperInvariant();
            var numbers = "0|1|2|3|4|5|6|7|8|9";
            var cSharpType = "\\<|\\>|\\[|\\]|\\.|\\?";
            var word = $"({letters}|{capLetters}|_)+(|({letters}|{capLetters}|{numbers}|{cSharpType}|_)+)";

            var nfa = new Fsa();
            nfa.Build("schema",     (int)Schema);
            nfa.Build("partial",    (int)Partial);
            nfa.Build("repo",       (int)Repo);
            nfa.Build("service",    (int)Service);
            nfa.Build("json",       (int)Json);
            nfa.Build("state",      (int)State);
            nfa.Build("interface",  (int)Interface);
            nfa.Build(word,         (int)Ident);
            nfa.Build("{",          (int)LCurly);
            nfa.Build("}",          (int)RCurly);
            nfa.Build("\\(",        (int)LParen);
            nfa.Build("\\)",        (int)RParen);
            nfa.Build(",",          (int)Comma);
            nfa.Build("...",        (int)Splat);
            nfa.Build("=",          (int)Assign);
            nfa.Build("=>",         (int)Arrow);
            nfa.Build("<>",         (int)LRfReduce);
            nfa.Build("</>",        (int)RRfReduce);
            nfa.Build("<\">",       (int)LMultiLine);
            nfa.Build("</\">",      (int)RMultiLine);
            nfa.Build("\\|",        (int)Bar);
            nfa.Build("( |\n|\r|\t)+", 9999);

            AppendLine($"// INITIAL NFA TRAINED AT {(DateTime.Now - initTime).TotalMilliseconds}ms");

            // Significant performance improvement after initial training
            dfa = nfa.ConvertToDfa();
            AppendLine($"// DFA CREATED AT {(DateTime.Now - initTime).TotalMilliseconds}ms");
            dfa = dfa.MinimizeDfa();
            AppendLine($"// DFA MINIMIZED AT {(DateTime.Now - initTime).TotalMilliseconds}ms");
            
            // In case multiple processes are compiling cache
            var tempFile = $"GrammarDfaCache-{Guid.NewGuid()}.bin";
            using (var outStream = File.OpenWrite(tempFile))
            {
#pragma warning disable SYSLIB0011
                binary.Serialize(outStream, dfa);
#pragma warning restore SYSLIB0011
            }

            // Try to place the cache in the correct place; ignore errors
            try
            {
                File.Move(tempFile, "GrammarDfaCache.bin");
            } catch (Exception)
            {
                File.Delete(tempFile);
            }

            AppendLine($"// DFA COMPLETE IN {(DateTime.Now - initTime).TotalMilliseconds}ms");
        }

        var startTime = DateTime.Now;
        AppendLine($"// GENERATED FROM '{sourceFile}' AT {startTime:yyyy-MM-dd HH:mm:ss}");
        
        AppendLine("#nullable disable");
        AppendLine("namespace Generated;");

        if (!File.Exists(sourceFile))
        {
            throw new Exception($"Could not find '{sourceFile}'");
        }
        var source = new TokenStream()
        {
            Source = File.ReadAllText(sourceFile),
            Grammar = dfa
        };

        var ext = Path.GetExtension(sourceFile).ToLowerInvariant();
        var modelName = Path.GetFileNameWithoutExtension(sourceFile);

        try
        {
            switch (ext)
            {
                case ".model":
                    TopLevelGrammar.MatchModel(source, modelName);
                    break;
                case ".view":
                    TopLevelGrammar.MatchView(source, modelName);
                    break;
            }
        } catch (Exception ex)
        {
            int lineNumber = 1, prevLine = 0;
            for (int i = 0; i <= source.Offset && i < source.Source.Length; i++)
            {
                if (source.Source[i] == '\n')
                {
                    lineNumber++;
                    prevLine = i;
                }
            }

            Console.Error.WriteLine("{0}:{1}:{2} - {3}",
                sourceFile,
                lineNumber,
                source.Offset - prevLine + 1,
                ex.Message);
            Process.GetCurrentProcess().Kill();
        }

        AppendLine($"// GENERATED IN {(DateTime.Now - startTime).TotalMilliseconds}ms");
        Console.Write(sourceBuilder.ToString());
    }
}
