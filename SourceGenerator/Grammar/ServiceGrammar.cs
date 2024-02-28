namespace SourceGenerator.Grammar;

using static Token;
using static ClassType;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public class ServiceGrammar
{
    public struct Dto
    {
        public string ModelName { get; set; }
        public List<ActionGrammar.Dto> Actions { get; set; }
    }

    public static Dto Match(TokenStream stream, string modelName, Dictionary<string, List<FieldGrammar.Dto>> splats)
    {
        var result = new Dto()
        {
            ModelName = modelName,
            Actions = new()
        };

        // "service" "{"
        Program.StartSpan(TopLevel);
        stream.Poll();
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception($"Expected left curly");
        }
        Program.EndSpan();

        while (stream.Next != (int)RCurly)
        {
            // {action name} "(" {parameter list} ")" ["=>" {return type}]
            var action = ActionGrammar.Match(stream, splats);
            if (action.IsJson)
            {
                throw new Exception("JSON is not valid for service action");
            }
            result.Actions.Add(action);

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

        return result;
    }

    public static void WriteServiceInterface(Dto _)
    {
        Program.AppendLine("    // Datalayer services are not supported");
    }

    public static void WriteDbService(Dto _)
    {
        Program.AppendLine("    // Datalayer backend services are not supported");
    }

    public static void WriteApiService(Dto _)
    {
        Program.AppendLine("    // Datalayer frontend services are not supported");
    }

    public static void WriteViewInterface(Dto _)
    {
        Program.AppendLine("    // View action interfaces are not supported");
    }

    public static void WriteViewRenderer(string renderContent, string modelName)
    {
        Program.AppendLine("    void _{0}_render({0}_t *state, dom_build_t *writer)\n    {{", modelName);

        Program.Append(renderContent
            .Replace("{", "{{")
            .Replace("}", "}}"));

        Program.AppendLine("    }}");

        Program.AppendLine("    char *{0}_render({0}_t *state)\n    {{", modelName);
        Program.AppendLine("        dom_build_t writer = (dom_build_t){{0}};");
        Program.AppendLine("        _{0}_render(state, &writer);", modelName);
        Program.AppendLine("        char *result = writer_tostr(&writer);");
        Program.AppendLine("        writer_free(&writer);");
        Program.AppendLine("        return result;");
        Program.AppendLine("    }}");
    }

    public static void WriteViewController(Dto _)
    {
        Program.AppendLine("// View action controllers are not supported");
    }
}
