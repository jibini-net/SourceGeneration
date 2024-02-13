namespace SourceGenerator.Grammar;

using static Token;
using static ClassType;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public class FieldGrammar
{
    public struct Dto
    {
        public string TypeName { get; set; }
        public string Name { get; set; }
        public string Initial { get; set; }
    }

    public static Dto Match(TokenStream stream)
    {
        var result = new Dto();

        // {type name}
        if (stream.Next == (int)LCurly)
        {
            result.TypeName = TopLevelGrammar.MatchCSharp(stream);
        } else
        {
            Program.StartSpan(TypeName);
            if (stream.Poll() != (int)Ident)
            {
                throw new Exception("Expected field type name");
            } else
            {
                result.TypeName = stream.Text;
            }
            Program.EndSpan();
        }

        // {field name}
        Program.StartSpan(ClassType.Assign);
        if (stream.Poll() != (int)Ident)
        {
            throw new Exception("Expected field member name");
        }
        result.Name = stream.Text;

        if (stream.Next == (int)Token.Assign)
        {
            // "=" "{" {C# expression} "}"
            stream.Poll();
            Program.EndSpan();
            result.Initial = TopLevelGrammar.MatchCSharp(stream);
        } else
        {
            Program.EndSpan();
        }

        return result;
    }
}
