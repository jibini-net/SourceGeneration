namespace SourceGenerator.Grammar;

using static Token;
using static ClassType;

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

    public static Dto Match(TokenStream stream, string modelName, Dictionary<string, List<FieldGrammar.Dto>> splats, bool inherit = true)
    {
        var result = new Dto()
        {
            Fields = new()
        };

        // "partial" {type name} "{"
        Program.StartSpan(TopLevel);
        stream.Poll();
        Program.EndSpan();
        _ = stream.Next;
        Program.StartSpan(TypeName);
        if (stream.Poll() != (int)Ident)
        {
            throw new Exception($"Expected partial model class name");
        }
        Program.EndSpan();
        result.SuperClass = inherit ? modelName : "";
        result.Name = stream.Text;
        Program.StartSpan(TopLevel);
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception($"Expected left curly");
        }
        Program.EndSpan();

        while (stream.Next != (int)RCurly)
        {
            // {type} {name} ["=" "{" {initial value} "}"]
            var field = FieldGrammar.Match(stream);
            result.Fields.Add(field);

            // ","
            Program.StartSpan(TopLevel);
            if (stream.Next != (int)RCurly && stream.Poll() != (int)Comma)
            {
                throw new Exception("Expected comma or '}'");
            }
            Program.EndSpan();
        }

        // "}"
        Program.StartSpan(TopLevel);
        stream.Poll();
        Program.EndSpan();

        //TODO Enforce 'schema' occurs before 'partial'
        splats[$"{modelName}.{result.Name}"] = result.Fields
            .Union(splats.GetValueOrDefault(result.SuperClass, new()))
            .ToList();
        return result;
    }

    public static void Write(Dto dto)
    {
        Program.AppendLine("    typedef struct {0}", dto.Name);
        if (!string.IsNullOrEmpty(dto.SuperClass))
        {
            Program.AppendLine($"    // Discarded superclass '{dto.SuperClass}'");
        }
        Program.AppendLine("    }}");
        Program.AppendLine("    {{");

        foreach (var field in dto.Fields)
        {
            Program.AppendLine("        public {0} {1} {{ get; set; }}",
               field.TypeName,
               field.Name);
            if (!string.IsNullOrEmpty(field.Initial))
            {
                Program.AppendLine("            = {0};", field.Initial);
            }
        }
        
        Program.AppendLine("    }} {0}_t", dto.Name);
    }
}
