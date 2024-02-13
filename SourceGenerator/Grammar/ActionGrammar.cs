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
        Program.StartSpan(Delimeter);
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
            throw new NotImplementedException("TODO Model field splat is not implemented");
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
            Program.StartSpan(Delimeter);
            if (stream.Next != (int)RParen && stream.Poll() != (int)Comma)
            {
                throw new Exception("Expected comma or ')'");
            }
            Program.EndSpan();
        }

        // ")"
        Program.StartSpan(Delimeter);
        stream.Poll();
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
