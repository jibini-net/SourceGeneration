namespace SourceGenerator.Grammar;

using static Token;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public class ServiceGrammar
{
    public static void Match(TokenStream stream, string modelName)
    {
        // "service" "{"
        stream.Poll();
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception($"Expected left curly");
        }

        var actions = new List<RepoGrammar.ProcDto>();
        while (stream.Next != (int)RCurly)
        {
            // {action name} "(" {parameter list} ")" ["=>" {return type}]
            var proc = RepoGrammar.MatchProc(stream);
            if (proc.IsJson)
            {
                throw new Exception("JSON is not valid for service action");
            }
            actions.Add(proc);

            // ","
            if (stream.Next != (int)RCurly && stream.Poll() != (int)Comma)
            {
                throw new Exception("Expected comma or '}'");
            }
        }

        // "}"
        stream.Poll();

        WriteServiceInterface(actions);
        WriteDbService(actions);
        WriteApiService(actions, modelName);
    }

    public static void WriteServiceInterface(List<RepoGrammar.ProcDto> actions)
    {
        Console.WriteLine("    public interface IService");
        Console.WriteLine("    {");

        foreach (var proc in actions)
        {
            
            Console.WriteLine("        {0} {1}({2});",
                proc.ReturnType,
                proc.Name,
                string.Join(',', proc.Params.Select((it) => $"{it.type} {it.name}")));
        }

        Console.WriteLine("    }");
        Console.WriteLine("    public interface IBackendService : IService");
        Console.WriteLine("    {");
        Console.WriteLine("        // Implement and inject this interface as a separate service");
        Console.WriteLine("    }");
    }

    public static void WriteDbService(List<RepoGrammar.ProcDto> actions)
    {
        Console.WriteLine("    public class DbService : IService");
        Console.WriteLine("    {");
        Console.WriteLine("        //TODO Inject database wrapper service");
        Console.WriteLine("        private readonly IBackendService impl;");
        Console.WriteLine("        public DbService(IBackendService impl)");
        Console.WriteLine("        {");
        Console.WriteLine("            this.impl = impl;");
        Console.WriteLine("        }");

        foreach (var action in actions)
        {
            Console.WriteLine("        public {0} {1}({2})",
                action.ReturnType,
                action.Name,
                string.Join(',', action.Params.Select((it) => $"{it.type} {it.name}")));
            Console.WriteLine("        {");

            if (action.ReturnType == "void")
            {
                Console.WriteLine("            //TODO Code to execute via DB wrapper");
                Console.WriteLine("            /*wrapper.Execute(() => */impl.{0}(", action.Name);
            } else
            {
                Console.WriteLine("            //TODO Code to execute via DB wrapper");
                Console.WriteLine("            return /*wrapper.Execute<{0}>(() => */impl.{1}(",
                    action.ReturnType,
                    action.Name);
            }
            foreach (var par in action.Params.Select((it) => it.name))
            {
                if (par != action.Params.First().name)
                {
                    Console.WriteLine(",");
                }
                Console.Write("                  ");
                Console.Write(par);
            }
            Console.WriteLine("\n                  )/*)*/;");
            Console.WriteLine("        }");
        }

        Console.WriteLine("    }");
    }

    public static void WriteApiService(List<RepoGrammar.ProcDto> actions, string modelName)
    {
        Console.WriteLine("    public class ApiService : IService");
        Console.WriteLine("    {");
        Console.WriteLine("        //TODO Inject HTTP client service");
        Console.WriteLine("        public ApiService()");
        Console.WriteLine("        {");
        Console.WriteLine("        }");

        foreach (var action in actions)
        {
            Console.WriteLine("        public {0} {1}({2})",
                action.ReturnType,
                action.Name,
                string.Join(',', action.Params.Select((it) => $"{it.type} {it.name}")));
            Console.WriteLine("        {");

            if (action.ReturnType == "void")
            {
                Console.WriteLine("            //TODO Code to execute via API client");
                Console.WriteLine("            //api.Execute(\"{0}/{1}\", new {{",
                    modelName,
                    action.Name);
            } else
            {
                Console.WriteLine("            //TODO Code to execute via API client");
                Console.WriteLine("            return default;");
                Console.WriteLine("            //return api.Execute<{0}>(\"{1}/{2}\", new {{",
                    action.ReturnType,
                    modelName,
                    action.Name);
            }
            foreach (var par in action.Params.Select((it) => it.name))
            {
                if (par != action.Params.First().name)
                {
                    Console.WriteLine(",");
                }
                Console.Write("            //    ");
                Console.Write(par);
            }
            Console.WriteLine("\n            //});");
            Console.WriteLine("        }");
        }

        Console.WriteLine("    }");
    }
}
