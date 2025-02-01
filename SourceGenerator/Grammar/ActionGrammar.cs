namespace SourceGenerator.Grammar;

using static Token;
using static ClassType;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public class ActionGrammar
{
    public struct Dto
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public string SplatFrom { get; set; }
        public List<(string type, string name)> Params { get; set; }
        public bool IsJson { get; set; }
        public List<ApiDto> Api { get; set; }
    }

    public struct ApiDto
    {
        public string VersionName { get; set; }
        public string Summary { get; set; }
        public string Annotations { get; set; }
    }

    public static List<ApiDto> MatchApi(TokenStream stream)
    {
        List<ApiDto> result = [];

        // "api" "("
        Program.StartSpan(ClassType.Assign);
        stream.Poll();
        if (stream.Poll() != (int)LParen)
        {
            throw new ApplicationException("Expected left parens");
        }
        Program.EndSpan();

        while (stream.Next != (int)RParen)
        {
            // "{" {API version} "}" "{" {Description} "}"
            ApiDto entry = new()
            {
                VersionName = TopLevelGrammar.MatchCSharp(stream),
                Summary = TopLevelGrammar.MatchCSharp(stream)
            };
            // [ "{" {Annotations} "}" ]
            if (stream.Next == (int)LCurly)
            {
                entry.Annotations = TopLevelGrammar.MatchCSharp(stream);
            }

            // ","
            Program.StartSpan(ClassType.Assign);
            if (stream.Next != (int)RParen && stream.Poll() != (int)Comma)
            {
                throw new Exception("Expected annotations, comma, or ')'");
            }
            Program.EndSpan();

            result.Add(entry);
        }

        // ")"
        Program.StartSpan(ClassType.Assign);
        stream.Poll();
        Program.EndSpan();

        return result;
    }

    public static Dto Match(TokenStream stream, Dictionary<string, List<FieldGrammar.Dto>> splats)
    {
        var result = new Dto()
        {
            ReturnType = "void",
            Params = []
        };

        if (stream.Next == (int)Api)
        {
            // "api" "(" {descriptor} [, ...] ")"
            result.Api = MatchApi(stream);
        } else
        {
            result.Api = [];
        }

        // {SQL proc name} "("
        Program.StartSpan(Delimiter);
        if (stream.Poll() != (int)Ident)
        {
            throw new Exception("Expected procedure name for repo");
        }
        result.Name = stream.Text;
        if (stream.Poll() != (int)LParen)
        {
            throw new Exception("Expected left parens");
        }
        Program.EndSpan();

        // "..."
        if (stream.Next == (int)Splat)
        {
            Program.StartSpan(TypeName);
            stream.Poll();

            if (stream.Poll() != (int)Ident)
            {
                throw new Exception("Expected splat type identifier");
            }
            result.SplatFrom = stream.Text;
            Program.EndSpan();

            if (splats.TryGetValue(result.SplatFrom, out var splat))
            {
                result.Params = splat
                    .Select((it) => (it.TypeName, it.Name))
                    .ToList();
                goto skipArgs;
            } else
            {
                throw new Exception($"No splat type called '{result.SplatFrom}'");
            }
        }

        while (stream.Next != (int)RParen)
        {
            // {param type}
            string parType;
            if (stream.Next == (int)LCurly)
            {
                parType = TopLevelGrammar.MatchCSharp(stream);
            } else
            {
                Program.StartSpan(TypeName);
                if (stream.Poll() != (int)Ident)
                {
                    throw new Exception("Expected proc parameter type");
                } else
                {
                    parType = stream.Text;
                }
                Program.EndSpan();
            }

            // {param name}
            Program.StartSpan(ClassType.Assign);
            if (stream.Poll() != (int)Ident)
            {
                throw new Exception("Expected proc parameter name");
            }
            Program.EndSpan();
            result.Params.Add((parType, stream.Text));

            // ","
            Program.StartSpan(Delimiter);
            if (stream.Next != (int)RParen && stream.Poll() != (int)Comma)
            {
                throw new Exception("Expected comma or ')'");
            }
            Program.EndSpan();
        }

    skipArgs:
        // ")"
        Program.StartSpan(Delimiter);
        if (stream.Poll() != (int)RParen)
        {
            throw new Exception("Expected ')'");
        }
        Program.EndSpan();

        if (stream.Next == (int)Arrow)
        {
            Program.StartSpan(TypeName);
            // "=>" ["json"] {return type}
            stream.Poll();
            if (stream.Next == (int)Json)
            {
                result.IsJson = true;
                stream.Poll();
            }

            if (stream.Next == (int)LCurly)
            {
                Program.EndSpan();
                result.ReturnType = TopLevelGrammar.MatchCSharp(stream);
            } else if (stream.Poll() != (int)Ident)
            {
                throw new Exception("Expected proc return type name");
            } else
            {
                result.ReturnType = stream.Text;
                Program.EndSpan();
            }
        }

        return result;
    }
}
