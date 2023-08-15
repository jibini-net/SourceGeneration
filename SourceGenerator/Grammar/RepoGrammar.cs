namespace SourceGenerator.Grammar;

using static Token;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public class RepoGrammar
{
    public struct Dto
    {
        public List<ActionGrammar.Dto> Procs { get; set; }
    }

    public static Dto Match(TokenStream stream)
    {
        var result = new Dto()
        {
            Procs = new()
        };

        // "repo" "{"
        stream.Poll();
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception($"Expected left curly");
        }

        while (stream.Next != (int)RCurly)
        {
            // {dbo} "." {name} "(" {parameter list} ")" ["=>" {return type}]
            var proc = ActionGrammar.Match(stream);
            result.Procs.Add(proc);

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

    public static void Write(Dto dto)
    {
        Console.WriteLine("    public class Repository");
        Console.WriteLine("    {");
        Console.WriteLine("        //TODO Code to inject database service interface");

        foreach (var proc in dto.Procs)
        {
            Console.WriteLine("        public {0} {1}({2})",
                proc.ReturnType,
                proc.Name.Replace(".", "__"),
                string.Join(',', proc.Params.Select((it) => $"{it.type} {it.name}")));
            Console.WriteLine("        {");

            if (proc.ReturnType == "void")
            {
                Console.WriteLine("            //TODO Code to execute void-result proc" + (proc.IsJson ? " as json" : ""));
                Console.WriteLine("            //db.Execute" + (proc.IsJson ? "ForJson" : "") +  "(\"{0}\", new {{ ", proc.Name);
            }
            else
            {
                Console.WriteLine("            //TODO Code to read results from proc" + (proc.IsJson ? " as json" : ""));
                Console.WriteLine("            return default;");
                Console.WriteLine("            //return db.Execute" + (proc.IsJson ? "ForJson" : "") +  "<{0}>(\"{1}\", new {{ ",
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

            Console.WriteLine("        }");
        }

        Console.WriteLine("    }");
    }
}
