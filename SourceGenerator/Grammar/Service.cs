namespace SourceGenerator.Grammar;

using static Token;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public class _Service
{
    public static void Match(TokenStream stream, string modelName)
    {
        // "service" "{"
        stream.Poll();
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception($"Expected left curly");
        }

        var actions = new List<_Repo.ProcDto>();
        while (stream.Next != (int)RCurly)
        {
            // {action name} "(" {parameter list} ")" ["=>" {return type}]
            actions.Add(_Repo.MatchProc(stream));

            // ","
            if (stream.Next != (int)RCurly && stream.Poll() != (int)Comma)
            {
                throw new Exception("Expected comma or '}'");
            }
        }

        // "}"
        stream.Poll();

        WriteServices(actions, modelName);
    }

    public static void WriteServices(List<_Repo.ProcDto> actions, string modelName)
    {
        Console.WriteLine("    public interface IService");
        Console.WriteLine("    {");

        foreach (var proc in actions)
        {
            
            Console.WriteLine("        {0} {1}({2});",
                proc.ReturnType,
                proc.Name,
                string.Join(',', proc.Params.Select((it) => $"{it.type} {it.name}")));
            /*
            Console.WriteLine("        {");

            if (proc.ReturnType == "void")
            {
                Console.WriteLine("            //TODO Code to execute void-result proc");
                Console.WriteLine("            //db.Execute(\"{0}\", new {{ ", proc.Name);
            } else
            {
                Console.WriteLine("            //TODO Code to read results from proc");
                Console.WriteLine("            return default;");
                Console.WriteLine("            //return db.Execute<{0}>(\"{1}\", new {{ ",
                    proc.ReturnType,
                    proc.Name);
            }
            foreach (var par in proc.Params.Select((it) => it.name))
            {
                if (par != proc.Params.First().name)
                {
                    Console.WriteLine(",");
                }
                Console.Write("            //    ");
                Console.Write(par);
            }
            Console.WriteLine("\n            //});");
            */
        }

        Console.WriteLine("    }");
    }
}
