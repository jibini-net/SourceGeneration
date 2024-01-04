namespace SourceGenerator.Grammar;

using System.Text;
using System.Text.RegularExpressions;

using static Token;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public partial class TopLevelGrammar
{
    public static void MatchModel(TokenStream stream, string modelName)
    {
        Program.AppendLine("public class {0}",
            modelName);
        Program.AppendLine("{{");

        while (stream.Next > 0)
        {
            switch (stream.Next)
            {
                case (int)LCurly:
                    var cSharp = MatchCSharp(stream);
                    Program.AppendLine(cSharp.Replace("{", "{{").Replace("}", "}}"));
                    break;

                case (int)Schema:
                    var schema = SchemaGrammar.Match(stream);
                    SchemaGrammar.Write(schema);
                    break;

                case (int)Partial:
                    var partial = PartialGrammar.Match(stream, modelName);
                    PartialGrammar.Write(partial);
                    break;
                    
                case (int)Repo:
                    var repo = RepoGrammar.Match(stream);
                    RepoGrammar.Write(repo);
                    break;
                    
                case (int)Service:
                    var services = ServiceGrammar.Match(stream, modelName);
                    ServiceGrammar.WriteServiceInterface(services);
                    ServiceGrammar.WriteDbService(services);
                    ServiceGrammar.WriteApiService(services);
                    break;
                    
                default:
                    throw new Exception($"Invalid token '{stream.Text}' for top-level");
            }
        }

        Program.AppendLine("}}");
    }

    public static void MatchView(TokenStream stream, string modelName)
    {
        Program.AppendLine("using Microsoft.AspNetCore.Mvc;");
        
        Program.AppendLine("public abstract class {0}Base : {0}Base.IView",
            modelName);
        Program.AppendLine("{{");
        Program.AppendLine("    // Extend and fully implement all actions in a subclass");
        
        Program.AppendLine("    private readonly IServiceProvider sp;");
        Program.AppendLine("    public {0}Base(IServiceProvider sp)\n    {{",
            modelName);
        Program.AppendLine("        this.sp = sp;");
        Program.AppendLine("    }}");

        var renderBuilder = new StringBuilder();
        void buildDom(string expr)
        {
            renderBuilder.AppendLine($"        await writer.WriteAsync({expr});");
        }
        void buildLogic(string stmt)
        {
            renderBuilder.AppendLine($"        {stmt}");
        }

        buildDom($"$\"<!--_open-{modelName}({{indexByTag}})-->\"");

        ServiceGrammar.Dto actions = new()
        {
            Actions = new()
        };
        while (stream.Next > 0)
        {
            switch (stream.Next)
            {
                case (int)LCurly:
                    var cSharp = MatchCSharp(stream);
                    Program.AppendLine(cSharp
                        .Replace("{", "{{")
                        .Replace("}", "}}"));
                    break;

                case (int)State:
                    var schema = SchemaGrammar.Match(stream);
                    SchemaGrammar.Write(schema, accessLevel: "internal");
                    SchemaGrammar.WriteStateDump(schema, modelName);
                    break;

                case (int)Interface:
                    actions = ServiceGrammar.Match(stream, modelName);
                    ServiceGrammar.WriteViewInterface(actions, modelName);
                    break;

                default:
                    var domElement = HtmlNodeGrammar.Match(stream);
                    HtmlNodeGrammar.Write(domElement, buildDom, buildLogic);
                    break;
            }
        }

        buildDom($"$\"<!--_close-{modelName}({{indexByTag}})-->\"");

        Program.AppendLine("    public async Task<string> RenderAsync(StateDump state, int indexByTag = 0)\n    {{");
        Program.AppendLine("        var build = new System.Text.StringBuilder();");
        Program.AppendLine("        var tagCounts = new Dictionary<string, int>();");
        Program.AppendLine("        state.Tag = \"{0}\";",
            modelName);
        Program.AppendLine("        state.State = GetState();");
        Program.AppendLine("        using var writer = new StringWriter(build);");

        Program.Append(renderBuilder
            .ToString()
            .Replace("{", "{{")
            .Replace("}", "}}"));

        Program.AppendLine("        state.Children = tagCounts\n" +
                           "            .SelectMany((kv) => state.Children.Where((it) => it.Tag == kv.Key).Take(kv.Value + 1))\n" +
                           "            .ToList();");

        Program.AppendLine("        return build.ToString();");
        Program.AppendLine("    }}");

        Program.AppendLine("}}");

        Program.AppendLine("[Controller]\n[Route(\"/view/{0}\")]",
            modelName);
        Program.AppendLine("public class {0}ViewController : ControllerBase",
            modelName);
        Program.AppendLine("{{");

        Program.AppendLine("    private readonly {0}Base.IView component;",
            modelName);
        Program.AppendLine("    public {0}ViewController({0}Base.IView component)\n    {{",
            modelName);
        Program.AppendLine("        this.component = component;");
        Program.AppendLine("    }}");

        Program.AppendLine("    [HttpPost(\"\")]");
        Program.AppendLine("    public async Task<IActionResult> Index([FromBody] StateDump state = null)\n    {{");
        Program.AppendLine("        var html = await component.RenderPageAsync(state);");
        Program.AppendLine("        return Content(html, \"text/html\");");
        Program.AppendLine("    }}");

        foreach (var action in actions.Actions)
        {
            Program.AppendLine("    [HttpPost(\"{0}\")]",
                action.Name);

            string attr((string type, string name) it) => $"{it.type} {it.name}";
            var attrs = action.Params.Select(attr);
            var attributes = string.Join(", ", attrs.Prepend(""));

            Program.AppendLine("    public async Task<IActionResult> {0}([FromBody] TagRenderRequest render{1})\n    {{",
                action.Name,
                attributes);
            Program.AppendLine("        var html = await component.RenderComponentAsync(render.State, render.Path, async (it) =>");
            Program.AppendLine("        {{");

            Program.AppendLine("            {0}it.{1}({2});",
                (action.ReturnType == "Task" || action.ReturnType.StartsWith("Task<"))
                    ? "await "
                    : "",
                action.Name,
                string.Join(", ", action.Params.Select((it) => it.name)));

            Program.AppendLine("            await Task.CompletedTask;");
            Program.AppendLine("        }});");
            Program.AppendLine("        return Content(html, \"text/html\");");
            Program.AppendLine("    }}");
        }

        Program.AppendLine("}}");
    }

    public static string MatchCSharp(TokenStream stream)
    {
        // "{"
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception("Wrap verbatim C# code in curly braces");
        }
        var bracketDepth = 1;

        // {C# expression} "}"
        int start = stream.Offset, length = 0;
        var escaped = false;
        for (;
            start + length < stream.Source.Length && bracketDepth > 0;
            length++)
        {
            switch (stream.Source[start + length])
            {
                case '{':
                    bracketDepth += escaped ? 0 : 1;
                    continue;
                case '}':
                    bracketDepth -= escaped ? 0 : 1;
                    continue;
                case '\\':
                    escaped = !escaped;
                    continue;
                default:
                    escaped = false;
                    continue;
            }
        }
        stream.Seek(start + length);
        return NonPrecededBackslash()
            .Replace(stream.Source.Substring(start, length - 1), "")
            // This doesn't catch "\\" which becomes "\"
            .Replace("\\\\", "\\");
    }

    [GeneratedRegex("(?<!\\\\)\\\\")]
    private static partial Regex NonPrecededBackslash();
}
