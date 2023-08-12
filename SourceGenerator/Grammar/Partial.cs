namespace SourceGenerator.Grammar;

using static Token;

public class _Partial
{
    public static void Match(TokenStream stream, string modelName)
    {
        // "partial" {identifier} "{"
        stream.Poll();
        if (stream.Poll() != (int)Ident)
        {
            throw new Exception($"Expected partial model class name");
        }
        Console.WriteLine("    public partial class {0} : {1}",
            stream.Text,
            modelName);
        Console.WriteLine("    {");
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception($"Expected left curly");
        }

        while (stream.Next != (int)RCurly)
        {
            var field = _Schema.MatchField(stream);
            Console.WriteLine("        public {0} {1} {{ get; set; }}",
                field.TypeName,
                field.Name);

            if (!string.IsNullOrEmpty(field.Initial))
            {
                Console.WriteLine("            = {0};", field.Initial);
            }

            // ","
            if (stream.Next != (int)RCurly && stream.Poll() != (int)Comma)
            {
                throw new Exception("Expected comma");
            }
        }

        // "}"
        stream.Poll();
        Console.WriteLine("    }");
    }
}
