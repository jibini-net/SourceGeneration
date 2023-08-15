namespace SourceGenerator.Grammar;

using static Token;

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
        public List<(string type, string name)> Params { get; set; }
        public bool IsJson { get; set; }
    }

    public static Dto Match(TokenStream stream)
    {
        var result = new Dto()
        {
            ReturnType = "void",
            Params = new()
        };

        // {SQL proc name} "("
        if (stream.Poll() != (int)Ident)
        {
            throw new Exception("Expected procedure name for repo");
        }
        result.Name = stream.Text;
        if (stream.Poll() != (int)LParen)
        {
            throw new Exception("Expected left parens");
        }

        // "..."
        if (stream.Next == (int)Splat)
        {
            throw new NotImplementedException("TODO Model field splat is not implemented");
        }

        while (stream.Next != (int)RParen)
        {
            // {param type}
            string parType;
            if (stream.Next == (int)LCurly)
            {
                parType = TopLevelGrammar.MatchCSharp(stream);
            } else if (stream.Poll() != (int)Ident)
            {
                throw new Exception("Expected proc parameter type");
            } else
            {
                parType = stream.Text;
            }

            // {param name}
            if (stream.Poll() != (int)Ident)
            {
                throw new Exception("Expected proc parameter name");
            }
            result.Params.Add((parType, stream.Text));

            // ","
            if (stream.Next != (int)RParen && stream.Poll() != (int)Comma)
            {
                throw new Exception("Expected comma or ')'");
            }
        }

        // ")"
        stream.Poll();

        if (stream.Next == (int)Arrow)
        {
            // "=>" ["json"] {return type}
            stream.Poll();
            if (stream.Next == (int)Json)
            {
                result.IsJson = true;
                stream.Poll();
            }

            if (stream.Next == (int)LCurly)
            {
                result.ReturnType = TopLevelGrammar.MatchCSharp(stream);
            } else if (stream.Poll() != (int)Ident)
            {
                throw new Exception("Expected proc return type name");
            } else
            {
                result.ReturnType = stream.Text;
            }
        }

        return result;
    }
}
