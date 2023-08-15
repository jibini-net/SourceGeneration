namespace SourceGenerator.Grammar;

using static Token;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public class PartialGrammar
{
    public struct Dto
    {
        public string SuperClass { get; set; }
        public string Name { get; set; }
        public List<FieldGrammar.Dto> Fields { get; set; }
    }

    public static Dto Match(TokenStream stream, string modelName)
    {
        var result = new Dto()
        {
            Fields = new()
        };

        // "partial" {type name} "{"
        stream.Poll();
        if (stream.Poll() != (int)Ident)
        {
            throw new Exception($"Expected partial model class name");
        }
        result.SuperClass = modelName;
        result.Name = stream.Text;
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception($"Expected left curly");
        }

        while (stream.Next != (int)RCurly)
        {
            // {type} {name} ["=" "{" {initial value} "}"]
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
        Console.WriteLine("    public partial class {0} : {1}",
            dto.Name,
            dto.SuperClass);
        Console.WriteLine("    {");

        foreach (var field in dto.Fields)
        {
            Console.WriteLine("        public {0} {1} {{ get; set; }}",
               field.TypeName,
               field.Name);
            if (!string.IsNullOrEmpty(field.Initial))
            {
                Console.WriteLine("            = {0};", field.Initial);
            }
        }
        
        Console.WriteLine("    }");
    }
}
