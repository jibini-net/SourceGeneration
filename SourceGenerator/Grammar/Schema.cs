namespace SourceGenerator.Grammar;

using System.Text.RegularExpressions;
using static Token;

public class _Schema
{
    public static void Match(TokenStream stream, string modelName)
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
        public string Initial { get; set; }
    }

    public static FieldDto MatchField(TokenStream stream)
    {
        var result = new FieldDto();

        // {type name}
        if (stream.Poll() != (int)Ident)
        {
            throw new Exception("Expected field type name");
        }
        result.TypeName = stream.Text;

        // {field name}
        if (stream.Poll() != (int)Ident)
        {
            throw new Exception("Expected field member name");
        }
        result.Name = stream.Text;

        if (stream.Next == (int)Assign)
        {
            // "="
            stream.Poll();
            result.Initial = _TopLevel.MatchCSharp(stream);
        }

        return result;
    }
}
