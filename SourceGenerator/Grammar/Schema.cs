namespace SourceGenerator.Grammar;

using static Token;

public class _Schema
{
    public static void Match(TokenStream stream)
    {
        // "schema" "{"
        stream.Poll();
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception($"Expected left curly");
        }

        while (stream.Next != (int)RCurly)
        {
            var field = MatchField(stream);
            Console.WriteLine($"public {field.TypeName} {field.Name} {'{'} get; set; {'}'}");

            // ","
            if (stream.Next != (int)RCurly && stream.Poll() != (int)Comma)
            {
                throw new Exception("Expected comma");
            }
        }

        // "}"
        stream.Poll();
    }

    public struct FieldDto
    {
        public string TypeName { get; set; }
        public string Name { get; set; }
    }

    public static FieldDto MatchField(TokenStream stream)
    {
        var result = new FieldDto();

        if (stream.Poll() != (int)Ident)
        {
            throw new Exception("Expected field type name");
        }
        result.TypeName = stream.Text;

        if (stream.Poll() != (int)Ident)
        {
            throw new Exception("Expected field member name");
        }
        result.Name = stream.Text;

        return result;
    }
}
