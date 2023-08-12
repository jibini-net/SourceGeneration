namespace SourceGenerator.Grammar;

using System.Text.RegularExpressions;
using static Token;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public class _TopLevel
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
                    _Schema.Match(stream, modelName);
                    break;

                case (int)Partial:
                    _Partial.Match(stream, modelName);
                    break;
                    
                case (int)Repo:
                    _Repo.Match(stream, modelName);
                    break;
                    /*
                case (int)Service:
                    _Service.Match(stream);
                    break;
                    */
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
        return Regex.Replace(stream.Source.Substring(start, length - 1),
            // Any character--mainly "{" and "}"-- can be escaped with "\"
            "(?<!\\\\)\\\\",
            "")
            // This doesn't catch "\\" which becomes "\"
            .Replace("\\\\", "\\");
    }
}
