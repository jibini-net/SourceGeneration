namespace SourceGenerator.Grammar;

using static Token;

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

    public static Dto Match(TokenStream stream, string modelName)
    {
        var result = new Dto()
        {
            ModelName = modelName,
            Actions = new()
        };

        // "service" "{"
        stream.Poll();
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception($"Expected left curly");
        }

        while (stream.Next != (int)RCurly)
        {
            // {action name} "(" {parameter list} ")" ["=>" {return type}]
            var action = ActionGrammar.Match(stream);
            if (action.IsJson)
            {
                throw new Exception("JSON is not valid for service action");
            }
            result.Actions.Add(action);

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

    public static void WriteServiceInterface(Dto dto)
    {
        Program.AppendLine("    public interface IService");
        Program.AppendLine("    {{");

        foreach (var action in dto.Actions)
        {
            Program.AppendLine("        {0} {1}({2});",
                action.ReturnType == "void" ? "Task" : $"Task<{action.ReturnType}>",
                action.Name,
                string.Join(',', action.Params.Select((it) => $"{it.type} {it.name}")));
        }

        Program.AppendLine("    }}");
        Program.AppendLine("    public interface IBackendService : IService");
        Program.AppendLine("    {{");
        Program.AppendLine("        // Implement and inject this interface as a separate service");
        Program.AppendLine("    }}");
    }

    public static void WriteDbService(Dto dto)
    {
        Program.AppendLine("    public class DbService : IService");
        Program.AppendLine("    {{");
        Program.AppendLine("        private readonly IModelDbWrapper wrapper;");
        Program.AppendLine("        private readonly IBackendService impl;");
        Program.AppendLine("        public DbService(IModelDbWrapper wrapper, IBackendService impl)");
        Program.AppendLine("        {{");
        Program.AppendLine("            this.wrapper = wrapper;");
        Program.AppendLine("            this.impl = impl;");
        Program.AppendLine("        }}");

        foreach (var action in dto.Actions)
        {
            Program.AppendLine("        public async {0} {1}({2})",
                action.ReturnType == "void" ? "Task" : $"Task<{action.ReturnType}>",
                action.Name,
                string.Join(',', action.Params.Select((it) => $"{it.type} {it.name}")));
            Program.AppendLine("        {{");

            if (action.ReturnType == "void")
            {
                Program.AppendLine("            await wrapper.ExecuteAsync(async () => await impl.{0}(", action.Name);
            } else
            {
                Program.AppendLine("            return await wrapper.ExecuteAsync<{0}>(async () => await impl.{1}(",
                    action.ReturnType,
                    action.Name);
            }
            foreach (var par in action.Params.Select((it) => it.name))
            {
                if (par != action.Params.First().name)
                {
                    Program.AppendLine(",");
                }
                Program.Append("                ");
                Program.Append(par);
            }
            Program.AppendLine("\n                ));");
            Program.AppendLine("        }}");
        }

        Program.AppendLine("    }}");
    }

    public static void WriteApiService(Dto dto)
    {
        Program.AppendLine("    public class ApiService : IService");
        Program.AppendLine("    {{");
        Program.AppendLine("        private readonly IModelApiAdapter api;");
        Program.AppendLine("        public ApiService(IModelApiAdapter api)");
        Program.AppendLine("        {{");
        Program.AppendLine("            this.api = api;");
        Program.AppendLine("        }}");

        foreach (var action in dto.Actions)
        {
            Program.AppendLine("        public async {0} {1}({2})",
                action.ReturnType == "void" ? "Task" : $"Task<{action.ReturnType}>",
                action.Name,
                string.Join(',', action.Params.Select((it) => $"{it.type} {it.name}")));
            Program.AppendLine("        {{");

            if (action.ReturnType == "void")
            {
                Program.AppendLine("            await api.ExecuteAsync(\"{0}/{1}\", new\n            {{",
                    dto.ModelName,
                    action.Name);
            } else
            {
                Program.AppendLine("            return await api.ExecuteAsync<{0}>(\"{1}/{2}\", new\n            {{",
                    action.ReturnType,
                    dto.ModelName,
                    action.Name);
            }
            foreach (var par in action.Params.Select((it) => it.name))
            {
                if (par != action.Params.First().name)
                {
                    Program.AppendLine(",");
                }
                Program.Append("                ");
                Program.Append(par);
            }
            Program.AppendLine("\n            }});");
            Program.AppendLine("        }}");
        }

        Program.AppendLine("    }}");
    }

    public static void WriteViewInterface(Dto dto)
    {
        Program.AppendLine("    public interface IView : IRenderView\n    {{");

        foreach (var action in dto.Actions)
        {
            Program.AppendLine("        {0} {1}({2});",
                action.ReturnType,
                action.Name,
                string.Join(',', action.Params.Select((it) => $"{it.type} {it.name}")));
        }

        Program.AppendLine("    }}");

        foreach (var action in dto.Actions)
        {
            Program.AppendLine("    public abstract {0} {1}({2});",
                action.ReturnType,
                action.Name,
                string.Join(',', action.Params.Select((it) => $"{it.type} {it.name}")));
        }

        if (dto.Actions.Count == 0)
        {
            Program.AppendLine("    public class Default : {0}Base\n    {{",
                dto.ModelName);
            Program.AppendLine("        public Default(IServiceProvider sp) : base(sp)\n        {{\n        }}");
            Program.AppendLine("    }}");
        }
    }

    public static void WriteViewRenderer(string renderContent, string modelName)
    {
        Program.AppendLine("    public async Task<string> RenderAsync(StateDump state, int indexByTag = 0)\n    {{");
        Program.AppendLine("        var build = new StringBuilder();");
        Program.AppendLine("        var tagCounts = new Dictionary<string, int>();");
        Program.AppendLine("        state.Tag = \"{0}\";",
            modelName);
        Program.AppendLine("        state.State = GetState();");
        Program.AppendLine("        using var writer = new StringWriter(build);");

        Program.Append(renderContent
            .Replace("{", "{{")
            .Replace("}", "}}"));

        Program.AppendLine("        state.Trim(tagCounts);");

        Program.AppendLine("        return build.ToString();");
        Program.AppendLine("    }}");

        Program.AppendLine("}}");
    }

    public static void WriteViewController(Dto dto)
    {
        Program.AppendLine("[Controller]\n[Route(\"/view/{0}\")]",
            dto.ModelName);
        Program.AppendLine("public class {0}ViewController : ControllerBase",
            dto.ModelName);
        Program.AppendLine("{{");

        Program.AppendLine("    private readonly {0}Base.IView component;",
            dto.ModelName);
        Program.AppendLine("    private readonly IServiceProvider sp;");
        Program.AppendLine("    public {0}ViewController({0}Base.IView component, IServiceProvider sp)\n    {{",
            dto.ModelName);
        Program.AppendLine("        this.component = component;");
        Program.AppendLine("        this.sp = sp;");
        Program.AppendLine("    }}");

        Program.AppendLine("    [HttpPost(\"\")]");
        Program.AppendLine("    public async Task<IActionResult> Index()\n    {{");
        Program.AppendLine("        var state = await JsonSerializer.DeserializeAsync<StateDump>(Request.Body);");
        Program.AppendLine("        var html = await component.RenderPageAsync(state);");
        Program.AppendLine("        return Content(html, \"text/html\");");
        Program.AppendLine("    }}");

        foreach (var action in dto.Actions)
        {
            string attr((string type, string name) it) => $"public {it.type} {it.name} {{ get; set; }}";
            var attrs = action.Params.Select(attr);
            var attributes = string.Join("\n        ", attrs);

            Program.AppendLine("    public class _{0}_Params\n    {{",
                action.Name);
            Program.AppendLine("        {0}",
                attributes);
            Program.AppendLine("    }}");

            Program.AppendLine("    [HttpPost(\"{0}\")]",
                action.Name);
            Program.AppendLine("    public async Task<IActionResult> {0}()\n    {{",
                action.Name);
            Program.AppendLine("        var render = await JsonSerializer.DeserializeAsync<TagRenderRequest>(Request.Body);");
            Program.AppendLine("        var pars = JsonSerializer.Deserialize<_{0}_Params>(render.Pars);",
                action.Name);
            Program.AppendLine("        var html = await component.RenderComponentAsync(sp, render.State, render.Path, async (it) =>");
            Program.AppendLine("        {{");

            Program.AppendLine("            {0}it.{1}({2});",
                (action.ReturnType == "Task" || action.ReturnType.StartsWith("Task<"))
                    ? "await "
                    : "",
                action.Name,
                string.Join(", ", action.Params.Select((it) => $"pars.{it.name}")));

            Program.AppendLine("            await Task.CompletedTask;");
            Program.AppendLine("        }});");
            Program.AppendLine("        return Content(html, \"text/html\");");
            Program.AppendLine("    }}");
        }

        Program.AppendLine("}}");
    }
}
