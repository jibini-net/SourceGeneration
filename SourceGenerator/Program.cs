//#define GENERATE_FSA_FILE

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

    public static int ThreadId => Environment.CurrentManagedThreadId;
    
    private static readonly ConcurrentDictionary<int, StringBuilder> sourceBuilders = new();
    private static StringBuilder SourceBuilder => sourceBuilders[ThreadId];

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
        SourceBuilder.Append(string.Format(source, args));
    }

    public static void AppendLine(string source, params object[] args)
    {
        SourceBuilder.AppendLine(string.Format(source, args));
    }

    private static void InitializeFsa()
    {
        var startTime = DateTime.Now;
#if GENERATE_FSA_FILE
        var nfa = new Fsa();

        nfa.Build("schema", (int)Schema);
        nfa.Build("partial", (int)Partial);
        nfa.Build("repo", (int)Repo);
        nfa.Build("service", (int)Service);
        nfa.Build("json", (int)Json);
        nfa.Build("state", (int)State);
        nfa.Build("interface", (int)Interface);
        nfa.Build("dto", (int)Dto);
        nfa.Build("api", (int)Api);
        nfa.Build("[a-zA-Z_]([a-zA-Z0-9_\\<\\>\\[\\]\\.\\?]+)?", (int)Ident);
        nfa.Build("\\{", (int)LCurly);
        nfa.Build("\\}", (int)RCurly);
        nfa.Build("\\(", (int)LParen);
        nfa.Build("\\)", (int)RParen);
        nfa.Build("\\,", (int)Comma);
        nfa.Build("\\.\\.\\.", (int)Splat);
        nfa.Build("\\=", (int)Assign);
        nfa.Build("\\=\\>", (int)Arrow);
        nfa.Build("\\<\\>", (int)LRfReduce);
        nfa.Build("\\<\\/\\>", (int)RRfReduce);
        nfa.Build("\\<\\\"\\>", (int)LMultiLine);
        nfa.Build("\\<\\/\\\"\\>", (int)RMultiLine);
        nfa.Build("\\|", (int)Bar);
        nfa.Build("[ \n\r\t]+", 9999);
#endif

        var consoleLine = new TrackedConsoleLine();
#if GENERATE_FSA_FILE
        consoleLine.Write($"CREATED NFA IN {(DateTime.Now - startTime).TotalMilliseconds}ms", color: ConsoleColor.Cyan);
#endif
        startTime = DateTime.Now;

#if GENERATE_FSA_FILE
        Dfa = nfa.ConvertToDfa().MinimizeDfa();
#else
        Dfa = """
              initial 0 nodes 
              n 0 tab 115 1 112 18 114 25 106 29 105 33 100 42 97 45 li 65 to 90 95 98 to 99 101 to 104 107 to 111 113 116 to 122 e 7 123 48 125 49 40 50 41 51 44 52 46 53 61 56 60 58 124 66 li 9 to 10 13 32 e 67 
              n 1 acc 1000 tab 99 2 101 8 116 14 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 98 100 102 to 115 117 to 122 e 7 
              n 2 acc 1000 tab 104 3 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 103 105 to 122 e 7 
              n 3 acc 1000 tab 101 4 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 100 102 to 122 e 7 
              n 4 acc 1000 tab 109 5 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 108 110 to 122 e 7 
              n 5 acc 1000 tab 97 6 li 46 48 to 57 60 62 to 63 65 to 91 93 95 98 to 122 e 7 
              n 6 acc 1 1000 tab li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 122 e 7 
              n 7 acc 1000 tab li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 122 e 7 
              n 8 acc 1000 tab 114 9 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 113 115 to 122 e 7 
              n 9 acc 1000 tab 118 10 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 117 119 to 122 e 7 
              n 10 acc 1000 tab 105 11 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 104 106 to 122 e 7 
              n 11 acc 1000 tab 99 12 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 98 100 to 122 e 7 
              n 12 acc 1000 tab 101 13 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 100 102 to 122 e 7 
              n 13 acc 4 1000 tab li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 122 e 7 
              n 14 acc 1000 tab 97 15 li 46 48 to 57 60 62 to 63 65 to 91 93 95 98 to 122 e 7 
              n 15 acc 1000 tab 116 16 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 115 117 to 122 e 7 
              n 16 acc 1000 tab 101 17 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 100 102 to 122 e 7 
              n 17 acc 6 1000 tab li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 122 e 7 
              n 18 acc 1000 tab 97 19 li 46 48 to 57 60 62 to 63 65 to 91 93 95 98 to 122 e 7 
              n 19 acc 1000 tab 114 20 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 113 115 to 122 e 7 
              n 20 acc 1000 tab 116 21 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 115 117 to 122 e 7 
              n 21 acc 1000 tab 105 22 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 104 106 to 122 e 7 
              n 22 acc 1000 tab 97 23 li 46 48 to 57 60 62 to 63 65 to 91 93 95 98 to 122 e 7 
              n 23 acc 1000 tab 108 24 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 107 109 to 122 e 7 
              n 24 acc 2 1000 tab li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 122 e 7 
              n 25 acc 1000 tab 101 26 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 100 102 to 122 e 7 
              n 26 acc 1000 tab 112 27 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 111 113 to 122 e 7 
              n 27 acc 1000 tab 111 28 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 110 112 to 122 e 7 
              n 28 acc 3 1000 tab li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 122 e 7 
              n 29 acc 1000 tab 115 30 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 114 116 to 122 e 7 
              n 30 acc 1000 tab 111 31 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 110 112 to 122 e 7 
              n 31 acc 1000 tab 110 32 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 109 111 to 122 e 7 
              n 32 acc 5 1000 tab li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 122 e 7 
              n 33 acc 1000 tab 110 34 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 109 111 to 122 e 7 
              n 34 acc 1000 tab 116 35 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 115 117 to 122 e 7 
              n 35 acc 1000 tab 101 36 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 100 102 to 122 e 7 
              n 36 acc 1000 tab 114 37 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 113 115 to 122 e 7 
              n 37 acc 1000 tab 102 38 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 101 103 to 122 e 7 
              n 38 acc 1000 tab 97 39 li 46 48 to 57 60 62 to 63 65 to 91 93 95 98 to 122 e 7 
              n 39 acc 1000 tab 99 40 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 98 100 to 122 e 7 
              n 40 acc 1000 tab 101 41 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 100 102 to 122 e 7 
              n 41 acc 7 1000 tab li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 122 e 7 
              n 42 acc 1000 tab 116 43 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 115 117 to 122 e 7 
              n 43 acc 1000 tab 111 44 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 110 112 to 122 e 7 
              n 44 acc 8 1000 tab li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 122 e 7 
              n 45 acc 1000 tab 112 46 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 111 113 to 122 e 7 
              n 46 acc 1000 tab 105 47 li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 104 106 to 122 e 7 
              n 47 acc 9 1000 tab li 46 48 to 57 60 62 to 63 65 to 91 93 95 97 to 122 e 7 
              n 48 acc 1001 
              n 49 acc 1002 
              n 50 acc 1003 
              n 51 acc 1004 
              n 52 acc 1005 
              n 53 tab 46 54 
              n 54 tab 46 55 
              n 55 acc 1006 
              n 56 acc 1007 tab 62 57 
              n 57 acc 1008 
              n 58 tab 62 59 47 60 34 64 
              n 59 acc 1009 
              n 60 tab 62 61 34 62 
              n 61 acc 1010 
              n 62 tab 62 63 
              n 63 acc 1012 
              n 64 tab 62 65 
              n 65 acc 1011 
              n 66 acc 1013 
              n 67 acc 9999 tab li 9 to 10 13 32 e 67 
              """.ParseFsa();
#endif

#if GENERATE_FSA_FILE
        consoleLine.Write($" [] CREATED DFA IN {(DateTime.Now - startTime).TotalMilliseconds}ms", color: ConsoleColor.Cyan);

        _ = Task.Run(async () =>
        {
            try
            {
                using var fsaOutput = File.Open("grammar.fsa", FileMode.Create);
                await Dfa.WriteTo(fsaOutput);
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        });
#else
        consoleLine.Write($"LOADED DFA IN {(DateTime.Now - startTime).TotalMilliseconds}ms", color: ConsoleColor.Cyan);
#endif
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
        consoleLine.Write($" [] GENERATED IN {millis}ms", ConsoleColor.Green);

        return sourceBuilders.Remove(ThreadId, out var _v)
            ? _v.ToString()
            : throw new Exception("String builder missing from dictionary");
    }

    private static readonly ConcurrentDictionary<int, (TokenStream source, List<MatchSpan> spanList)> spanLists = new();
    private static (TokenStream source, List<MatchSpan> spanList) SourceSpanList => spanLists[ThreadId];

    public static void StartSpan(ClassType classification, int? index = null)
    {
        if (!spanLists.ContainsKey(ThreadId))
        {
            return;
        }
        var (source, spanList) = SourceSpanList;
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
        var (source, spanList) = SourceSpanList;
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

        spanLists[ThreadId] = (source, []);

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

            var (_, spanList) = SourceSpanList;
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
        consoleLine.Write($" [] HIGHLIGHTED IN {millis}ms", ConsoleColor.Magenta);

        return spanLists.Remove(ThreadId, out var _v)
            ? JsonSerializer.Serialize(_v.spanList, MatchSpanJsonContext.Default.ListMatchSpan)
            : throw new Exception("Match spans missing from dictionary");
    }
}
