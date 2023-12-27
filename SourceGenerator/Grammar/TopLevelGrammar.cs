namespace SourceGenerator.Grammar;

using System.Text.RegularExpressions;

using static Token;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public partial class TopLevelGrammar
{
    public static void MatchModel(TokenStream stream, string modelName)
    {
        while (stream.Next > 0)
        {
            switch (stream.Next)
            {
                case (int)LCurly:
                    var cSharp = MatchCSharp(stream);
                    Program.AppendLine(cSharp.Replace("{", "{{").Replace("}", "}}"));
                    break;

                case (int)Schema:
                    var schema = SchemaGrammar.Match(stream);
                    SchemaGrammar.Write(schema);
                    break;

                case (int)Partial:
                    var partial = PartialGrammar.Match(stream, modelName);
                    PartialGrammar.Write(partial);
                    break;
                    
                case (int)Repo:
                    var repo = RepoGrammar.Match(stream);
                    RepoGrammar.Write(repo);
                    break;
                    
                case (int)Service:
                    var services = ServiceGrammar.Match(stream, modelName);
                    ServiceGrammar.WriteServiceInterface(services);
                    ServiceGrammar.WriteDbService(services);
                    ServiceGrammar.WriteApiService(services);
                    break;
                    
                default:
                    throw new Exception($"Invalid token '{stream.Text}' for top-level");
            }
        }
    }

    public static void MatchView(TokenStream stream, string modelName)
    {
        while (stream.Next > 0)
        {
            switch (stream.Next)
            {
                case (int)LCurly:
                    var cSharp = MatchCSharp(stream);
                    Program.AppendLine(cSharp.Replace("{", "{{").Replace("}", "}}"));
                    break;

                case (int)State:
                    var schema = SchemaGrammar.Match(stream);
                    SchemaGrammar.Write(schema, accessLevel: "private");
                    break;

                case (int)Interface:
                    //var inter = InterfaceGrammar.Match(stream, modelName);
                    //InterfaceGrammar.Write(inter);
                    // "interface" "{" "}"
                    stream.Poll();
                    if (stream.Poll() != (int)LCurly)
                    {
                        throw new Exception("Expected left curly");
                    }
                    if (stream.Poll() != (int)RCurly)
                    {
                        throw new Exception("Expected right curly");
                    }
                    break;

                default:
                    throw new Exception($"Invalid token '{stream.Text}' for top-level");
            }
        }
    }

    public static string MatchCSharp(TokenStream stream)
    {
        // "{"
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception("Wrap C# code in curly brackets");
        }
        var bracketDepth = 1;

        // {C# expression} "}"
        int start = stream.Offset, length = 0;
        var escaped = false;
        for (;
            start + length < stream.Source.Length && bracketDepth > 0;
            length++)
        {
            switch (stream.Source[start + length])
            {
                case '{':
                    bracketDepth += escaped ? 0 : 1;
                    continue;
                case '}':
                    bracketDepth -= escaped ? 0 : 1;
                    continue;
                case '\\':
                    escaped = !escaped;
                    continue;
                default:
                    escaped = false;
                    continue;
            }
        }
        stream.Seek(start + length);
        return NonPrecededBackslash()
            .Replace(stream.Source.Substring(start, length - 1), "")
            // This doesn't catch "\\" which becomes "\"
            .Replace("\\\\", "\\");
    }

    [GeneratedRegex("(?<!\\\\)\\\\")]
    private static partial Regex NonPrecededBackslash();
}
