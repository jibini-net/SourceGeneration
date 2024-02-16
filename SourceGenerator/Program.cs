using SourceGenerator.Grammar;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace SourceGenerator;

using static Token;

internal class Program
{
#if DEBUG
    public const string BIND_INTERFACE = "0.0.0.0";
#else
    public const string BIND_INTERFACE = "127.0.0.1";
#endif
    public const int PORT = 58994;

    public static Fsa Dfa { get; private set; }

    public static int ThreadId => Thread.CurrentThread.ManagedThreadId;
    
    private static ConcurrentDictionary<int, StringBuilder> sourceBuilders = new();
    private static StringBuilder sourceBuilder => sourceBuilders[ThreadId];

    public static void Main(string[] _)
    {
        InitializeFsa();

        var ip = new IPEndPoint(IPAddress.Parse(BIND_INTERFACE), PORT);
        using var server = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        server.Bind(ip);
        server.Listen(PORT);

        var consoleLine = new TrackedConsoleLine();
        consoleLine.Write($"LISTENING ON PORT {PORT}", color: ConsoleColor.Cyan);
        while (true)
        {
            var client = server.Accept();

            new Thread(() => HandleClient(client)).Start();
        }
    }

    private static void HandleClient(Socket client)
    {
        var consoleLine = new TrackedConsoleLine();
        consoleLine.Write((client.RemoteEndPoint as IPEndPoint).Address.ToString(), color: ConsoleColor.Cyan);

        var recvBuffer = new byte[2048];
        var sourceText = new StringBuilder();

        using (client)
        {
            var unterminated = true;
            while (unterminated)
            {
                var readBytes = client.Receive(recvBuffer);
                if (readBytes == 0)
                {
                    break;
                }
                sourceText.Append(Encoding.UTF8.GetString(recvBuffer
                    .Take(readBytes)
                    .TakeWhile((it) => unterminated &= it != '\0')
                    .ToArray()));
            }

            var source = new TokenStream()
            {
                Grammar = Dfa,
                Source = sourceText.ToString()
            };

            try
            {
                if (source.Poll() != (int)Ident)
                {
                    throw new Exception("Provide compilation or action command");
                }
                var command = source.Text;
                if (source.Next != (int)LCurly)
                {
                    throw new Exception("Provide encoded name of source file");
                }
                var fileName = TopLevelGrammar.MatchCSharp(source);

                consoleLine.Write($" [] RECEIVED {fileName} AT {DateTime.Now:yyyy-MM-dd HH:mm:ss}", color: ConsoleColor.White);
                var output = command switch
                {
                    "generate" => Generate(fileName, source, consoleLine),
                    "highlight" => Highlight(fileName, source, consoleLine),
                    _ => throw new Exception("Invalid command")
                };

                client.Send(Encoding.UTF8.GetBytes(output));
            } catch (Exception ex)
            {
                var fullMessage = ex.InnerException is null
                    ? ex.Message
                    : $"{ex.Message} - {ex.InnerException.Message}";
                consoleLine.Write($" !! {fullMessage}", color: ConsoleColor.Red);
                client.Send(Encoding.UTF8.GetBytes(fullMessage));
            }
        }
    }

    public static void Append(string source, params object[] args)
    {
        sourceBuilder.Append(string.Format(source, args));
    }

    public static void AppendLine(string source, params object[] args)
    {
        sourceBuilder.AppendLine(string.Format(source, args));
    }

    private static void InitializeFsa()
    {
        var startTime = DateTime.Now;

        var letters = "a|b|c|d|e|f|g|h|i|j|k|l|m|n|o|p|q|r|s|t|u|v|w|x|y|z";
        var capLetters = letters.ToUpperInvariant();
        var numbers = "0|1|2|3|4|5|6|7|8|9";
        var cSharpType = "\\<|\\>|\\[|\\]|\\.|\\?";
        var word = $"({letters}|{capLetters}|_)+(|({letters}|{capLetters}|{numbers}|{cSharpType}|_)+)";

        var nfa = new Fsa();
        nfa.Build("schema", (int)Schema);
        nfa.Build("partial", (int)Partial);
        nfa.Build("repo", (int)Repo);
        nfa.Build("service", (int)Service);
        nfa.Build("json", (int)Json);
        nfa.Build("state", (int)State);
        nfa.Build("interface", (int)Interface);
        nfa.Build("dto", (int)Dto);
        nfa.Build(word, (int)Ident);
        nfa.Build("{", (int)LCurly);
        nfa.Build("}", (int)RCurly);
        nfa.Build("\\(", (int)LParen);
        nfa.Build("\\)", (int)RParen);
        nfa.Build(",", (int)Comma);
        nfa.Build("...", (int)Splat);
        nfa.Build("=", (int)Assign);
        nfa.Build("=>", (int)Arrow);
        nfa.Build("<>", (int)LRfReduce);
        nfa.Build("</>", (int)RRfReduce);
        nfa.Build("<\">", (int)LMultiLine);
        nfa.Build("</\">", (int)RMultiLine);
        nfa.Build("\\|", (int)Bar);
        nfa.Build("( |\n|\r|\t)+", 9999);

        var consoleLine = new TrackedConsoleLine();
        consoleLine.Write($"CREATED NFA IN {(DateTime.Now - startTime).TotalMilliseconds}ms", color: ConsoleColor.Cyan);
        startTime = DateTime.Now;

        Dfa = nfa
            .ConvertToDfa()
            .MinimizeDfa();

        consoleLine.Write($" [] CREATED DFA IN {(DateTime.Now - startTime).TotalMilliseconds}ms", color: ConsoleColor.Cyan);
    }

    public static string Generate(string fileName, TokenStream source, TrackedConsoleLine consoleLine)
    {
        sourceBuilders[ThreadId] = new();
        // Must remain at character zero of a successful result
        AppendLine("/* DO NOT EDIT THIS FILE */");

        var startTime = DateTime.Now;
        AppendLine($"// GENERATED FROM {fileName} AT {startTime:yyyy-MM-dd HH:mm:ss}");

        AppendLine("#nullable disable");
        AppendLine("namespace Generated;");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var modelName = Path.GetFileNameWithoutExtension(fileName);

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
            sourceBuilders.Remove(ThreadId, out var _);

            int lineNumber = 1, prevLine = 0;
            for (int i = 0; i <= source.Offset && i < source.Source.Length; i++)
            {
                if (source.Source[i] == '\n')
                {
                    lineNumber++;
                    prevLine = i;
                }
            }

            var lineChar = source.Offset - prevLine + 1;
            throw new Exception($"{fileName}:{lineNumber}:{lineChar}", ex);
        }

        var millis = (DateTime.Now - startTime).TotalMilliseconds;
        AppendLine($"// GENERATED IN {millis}ms");
        consoleLine.Write($" [] GENERATED IN {millis}ms", true, ConsoleColor.Green);

        return sourceBuilders.Remove(ThreadId, out var _v)
            ? _v.ToString()
            : throw new Exception("String builder missing from dictionary");
    }

    private static ConcurrentDictionary<int, (TokenStream source, List<MatchSpan> spanList)> spanLists = new();
    private static (TokenStream source, List<MatchSpan> spanList) sourceSpanList => spanLists[ThreadId];

    public static void StartSpan(ClassType classification, int? index = null)
    {
        if (!spanLists.ContainsKey(ThreadId))
        {
            return;
        }
        var (source, spanList) = sourceSpanList;
        index ??= source.Offset;

        var prev = spanList.LastOrDefault();
        if (prev is not null)
        {
            if (prev.l == -1)
            {
                prev.l = index.Value - prev.s;
            }

            //if (prev.s + prev.l < index.Value)
            //{
            //    spanList.Add(new()
            //    {
            //        c = ClassType.PlainText,
            //        s = prev.s + prev.l,
            //        l = index.Value - (prev.s + prev.l)
            //    });
            //}
        }

        spanList.Add(new()
        {
            c = classification,
            s = index.Value
        });
    }

    public static void EndSpan(int? index = null)
    {
        if (!spanLists.ContainsKey(ThreadId))
        {
            return;
        }
        var (source, spanList) = sourceSpanList;
        index ??= source.Offset;

        var prev = spanList.LastOrDefault();
        if (prev is not null && prev.l == -1)
        {
            prev.l = index.Value - prev.s;
        }
    }

    public static string Highlight(string fileName, TokenStream source, TrackedConsoleLine consoleLine)
    {
        var startTime = DateTime.Now;

        spanLists[ThreadId] = (source, new());

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var modelName = Path.GetFileNameWithoutExtension(fileName);

        try
        {
            _ = source.Next;
            var sourceStart = source.Offset;
            StartSpan(ClassType.PlainText, sourceStart);

            switch (ext)
            {
                case ".model":
                    TopLevelGrammar.MatchModel(source, modelName, meta: true);
                    break;
                case ".view":
                    TopLevelGrammar.MatchView(source, modelName, meta: true);
                    break;
            }

            EndSpan();

            var (_, spanList) = sourceSpanList;
            //var last = spanList.LastOrDefault();
            //if (last.s + last.l < source.Source.Length)
            //{
            //    spanList.Add(new()
            //    {
            //        c = ClassType.PlainText,
            //        s = source.Offset,
            //        l = source.Source.Length - (last.s + last.l)
            //    });
            //}

            spanList.ForEach((it) => it.s -= sourceStart);
            spanList.RemoveAll((it) => it.l == 0);
        } catch (Exception ex)
        {
            spanLists.Remove(ThreadId, out var _);

            int lineNumber = 1, prevLine = 0;
            for (int i = 0; i <= source.Offset && i < source.Source.Length; i++)
            {
                if (source.Source[i] == '\n')
                {
                    lineNumber++;
                    prevLine = i;
                }
            }

            var lineChar = source.Offset - prevLine + 1;
            throw new Exception($"{fileName}:{lineNumber}:{lineChar}", ex);
        }

        var millis = (DateTime.Now - startTime).TotalMilliseconds;
        consoleLine.Write($" [] HIGHLIGHTED IN {millis}ms", true, ConsoleColor.Magenta);

        return spanLists.Remove(ThreadId, out var _v)
            ? JsonSerializer.Serialize(_v.spanList)
            : throw new Exception("Match spans missing from dictionary");
    }
}
