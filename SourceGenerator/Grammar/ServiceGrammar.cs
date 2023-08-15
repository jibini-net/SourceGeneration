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
        Console.WriteLine("    public interface IService");
        Console.WriteLine("    {");

        foreach (var proc in dto.Actions)
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

    public static void WriteDbService(Dto dto)
    {
        Console.WriteLine("    public class DbService : IService");
        Console.WriteLine("    {");
        Console.WriteLine("        //TODO Inject database wrapper service");
        Console.WriteLine("        private readonly IBackendService impl;");
        Console.WriteLine("        public DbService(IBackendService impl)");
        Console.WriteLine("        {");
        Console.WriteLine("            this.impl = impl;");
        Console.WriteLine("        }");

        foreach (var action in dto.Actions)
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

    public static void WriteApiService(Dto dto)
    {
        Console.WriteLine("    public class ApiService : IService");
        Console.WriteLine("    {");
        Console.WriteLine("        //TODO Inject HTTP client service");
        Console.WriteLine("        public ApiService()");
        Console.WriteLine("        {");
        Console.WriteLine("        }");

        foreach (var action in dto.Actions)
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
                    dto.ApiRoute,
                    action.Name);
            } else
            {
                Console.WriteLine("            //TODO Code to execute via API client");
                Console.WriteLine("            return default;");
                Console.WriteLine("            //return api.Execute<{0}>(\"{1}/{2}\", new {{",
                    action.ReturnType,
                    dto.ApiRoute,
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
