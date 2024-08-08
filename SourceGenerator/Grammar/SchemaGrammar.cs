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
    }

    public static Dto Match(TokenStream stream, string modelName, Dictionary<string, List<FieldGrammar.Dto>> splats)
    {
        var result = new Dto()
        {
            Fields = []
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

    public static void Write(Dto dto, string accessLevel = "public")
    {
        foreach (var field in dto.Fields)
        {
            Program.AppendLine("    {0} {1} {2} {{ get; set; }}",
                accessLevel,
                field.TypeName,
                field.Name);
            if (!string.IsNullOrEmpty(field.Initial))
            {
                Program.AppendLine("        = {0};", field.Initial);
            }
        }
    }

    public static void WriteStateDump(Dto dto)
    {
        string member(string name) => $"            [\"{name}\"] = {name},";
        var members = dto.Fields.Select((it) => member(it.Name));

        Program.AppendLine("    public Dictionary<string, object> GetState()\n    {{");
        Program.AppendLine("        return new()\n        {{");
        Program.AppendLine(string.Join("\n", members));
        Program.AppendLine("        }};");
        Program.AppendLine("    }}");

        string typeOf(string name) => dto.Fields.FirstOrDefault((it) => it.Name == name).TypeName;
        string assign(string name) => $"        {{{{ {name} = state.TryGetValue(\"{name}\", out var _v) ? (_v is null ? default({typeOf(name)}) : _v.ParseIfNot<{typeOf(name)}>()) : {name}; }}}}";
        var assignments = dto.Fields.Select((it) => assign(it.Name));

        Program.AppendLine("    public void LoadState(Dictionary<string, object> state)\n    {{");
        Program.AppendLine(string.Join("\n", assignments));
        Program.AppendLine("    }}");
    }
}
