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
        public string ApiRoute { get; set; }
        public List<ActionGrammar.Dto> Actions { get; set; }
    }

    public static Dto Match(TokenStream stream, string modelName)
    {
        var result = new Dto()
        {
            ApiRoute = modelName,
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
                    dto.ApiRoute,
                    action.Name);
            } else
            {
                Program.AppendLine("            return await api.ExecuteAsync<{0}>(\"{1}/{2}\", new\n            {{",
                    action.ReturnType,
                    dto.ApiRoute,
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
        Program.AppendLine("    public interface IView");
        Program.AppendLine("    {{");

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
    }
}
