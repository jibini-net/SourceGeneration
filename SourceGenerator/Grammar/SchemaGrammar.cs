namespace SourceGenerator.Grammar;

using static Token;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public class SchemaGrammar
{
    public struct Dto
    {
        public List<FieldGrammar.Dto> Fields { get; set; }
    }

    public static Dto Match(TokenStream stream)
    {
        var result = new Dto()
        {
            Fields = new()
        };

        // "schema" "{"
        stream.Poll();
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception($"Expected left curly");
        }

        while (stream.Next != (int)RCurly)
        {
            // {type} {name} ["=" "{" {C# expression} "}"]
            var field = FieldGrammar.Match(stream);
            result.Fields.Add(field);

            // ","
            if (stream.Next != (int)RCurly && stream.Poll() != (int)Comma)
            {
                throw new Exception("Expected comma or '}'");
            }
        }

        // "}"
        stream.Poll();

        return result;
    }

    public static void Write(Dto dto)
    {
        foreach (var field in dto.Fields)
        {
            Console.WriteLine("    public {0} {1} {{ get; set; }}",
                field.TypeName,
                field.Name);
            if (!string.IsNullOrEmpty(field.Initial))
            {
                Console.WriteLine("        = {0};", field.Initial);
            }
        }
    }
}
