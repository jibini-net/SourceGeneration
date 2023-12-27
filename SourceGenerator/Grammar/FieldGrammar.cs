namespace SourceGenerator.Grammar;

using static Token;

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
        } else if (stream.Poll() != (int)Ident)
        {
            throw new Exception("Expected field type name");
        } else
        {
            result.TypeName = stream.Text;
        }

        // {field name}
        if (stream.Poll() != (int)Ident)
        {
            throw new Exception("Expected field member name");
        }
        result.Name = stream.Text;

        if (stream.Next == (int)Assign)
        {
            // "=" "{" {C# expression} "}"
            stream.Poll();
            result.Initial = TopLevelGrammar.MatchCSharp(stream);
        }

        return result;
    }
}
