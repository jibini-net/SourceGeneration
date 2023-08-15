namespace SourceGenerator.Grammar;

using System.Text.RegularExpressions;

using static Token;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public partial class TopLevelGrammar
{
    public static void Match(TokenStream stream, string modelName)
    {
        while (stream.Next > 0)
        {
            switch (stream.Next)
            {
                case (int)LCurly:
                    Console.WriteLine(MatchCSharp(stream));
                    break;

                case (int)Schema:
                    SchemaGrammar.Match(stream, modelName);
                    break;

                case (int)Partial:
                    PartialGrammar.Match(stream, modelName);
                    break;
                    
                case (int)Repo:
                    RepoGrammar.Match(stream, modelName);
                    break;
                    
                case (int)Service:
                    ServiceGrammar.Match(stream, modelName);
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
