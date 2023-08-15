namespace SourceGenerator.Grammar;

using static Token;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public class SchemaGrammar
{
    public static void Match(TokenStream stream, string _/*modelName*/)
    {
        // "schema" "{"
        stream.Poll();
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception($"Expected left curly");
        }

        while (stream.Next != (int)RCurly)
        {
            // {type} {name} ["=" "{" {C# expression} "}"]
            var field = MatchField(stream);
            Console.WriteLine("    public {0} {1} {{ get; set; }}",
                field.TypeName,
                field.Name);
            if (!string.IsNullOrEmpty(field.Initial))
            {
                Console.WriteLine("        = {0};", field.Initial);
            }

            // ","
            if (stream.Next != (int)RCurly && stream.Poll() != (int)Comma)
            {
                throw new Exception("Expected comma or '}'");
            }
        }

        // "}"
        stream.Poll();
    }

    public struct FieldDto
    {
        public string TypeName { get; set; }
        public string Name { get; set; }
        public string Initial { get; set; }
    }

    public static FieldDto MatchField(TokenStream stream)
    {
        var result = new FieldDto();

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
