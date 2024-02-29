namespace SourceGenerator.Grammar;

using static Token;
using static ClassType;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public class SchemaGrammar
{
    public struct Dto
    {
        public List<FieldGrammar.Dto> Fields { get; set; }
        public string ModelName { get; set; }
    }

    public static Dto Match(TokenStream stream, string modelName, Dictionary<string, List<FieldGrammar.Dto>> splats)
    {
        var result = new Dto()
        {
            Fields = new(),
            ModelName = modelName
        };

        // "schema" "{"
        Program.StartSpan(TopLevel);
        stream.Poll();
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception($"Expected left curly");
        }
        Program.EndSpan();

        while (stream.Next != (int)RCurly)
        {
            // {type} {name} ["=" "{" {C# expression} "}"]
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

        splats[modelName] = result.Fields;
        return result;
    }

    public static void Write(Dto dto)
    {
        Program.AppendLine("typedef struct {0}\n{{", dto.ModelName);

        foreach (var field in dto.Fields)
        {
            Program.AppendLine("    {0} {1}",
                field.TypeName,
                field.Name);
            if (!string.IsNullOrEmpty(field.Initial))
            {
                Program.AppendLine("        = {0}", field.Initial);
            }
            Program.AppendLine(";");
        }

        Program.AppendLine("}} {0}_t;", dto.ModelName);

        Program.AppendLine("void _{0}_render({0}_t *state, writer_t *writer);", dto.ModelName);
        Program.AppendLine("char *{0}_render({0}_t *state);", dto.ModelName);

        Program.AppendLine("4c27e626-5404-40f4-bba1-adcc5a721701");
        Program.AppendLine("#include \"{0}.view.g.h\"\n", dto.ModelName);
    }

    public static void WriteStateDump(Dto _, string __)
    {
        Program.AppendLine("    // State dumps and restorations are not supported");
    }
}
