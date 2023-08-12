namespace SourceGenerator.Grammar;

using static Token;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public class _Repo
{
    public static void Match(TokenStream stream, string _/*modelName*/)
    {
        // "repo" "{"
        stream.Poll();
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception($"Expected left curly");
        }
        Console.WriteLine("    public class Repository");
        Console.WriteLine("    {");
        Console.WriteLine("        //TODO Code to inject database service interface");

        while (stream.Next != (int)RCurly)
        {
            // {dbo} "." {name} "(" {parameter list} ")" ["=>" {return type}]
            var proc = MatchProc(stream);
            Console.WriteLine("        public {0} {1}({2})",
                proc.ReturnType,
                proc.Name.Replace(".", "__"),
                string.Join(',', proc.Params.Select((it) => $"{it.type} {it.name}")));
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

            Console.WriteLine("        }");

            // ","
            if (stream.Next != (int)RCurly && stream.Poll() != (int)Comma)
            {
                throw new Exception("Expected comma or '}'");
            }
        }

        // "}"
        stream.Poll();
        Console.WriteLine("    }");
    }

    public struct ProcDto
    {
        public string Name { get; set; }
        public string ReturnType { get; set; }
        public List<(string type, string name)> Params { get; set; }
    }

    public static ProcDto MatchProc(TokenStream stream)
    {
        var result = new ProcDto()
        {
            ReturnType = "void",
            Params = new()
        };

        // {SQL proc name} "("
        if (stream.Poll() != (int)Ident)
        {
            throw new Exception("Expected procedure name for repo");
        }
        result.Name = stream.Text;
        if (stream.Poll() != (int)LParen)
        {
            throw new Exception("Expected left parens");
        }

        // "..."
        if (stream.Next == (int)Splat)
        {
            throw new NotImplementedException("TODO Model field splat is not implemented");
        }

        while (stream.Next != (int)RParen)
        {
            // {param type}
            if (stream.Poll() != (int)Ident)
            {
                throw new Exception("Expected proc parameter type");
            }
            var parType = stream.Text;

            // {param name}
            if (stream.Poll() != (int)Ident)
            {
                throw new Exception("Expected proc parameter name");
            }
            result.Params.Add((parType, stream.Text));

            // ","
            if (stream.Next != (int)RParen && stream.Poll() != (int)Comma)
            {
                throw new Exception("Expected comma or ')'");
            }
        }

        // ")"
        stream.Poll();

        if (stream.Next == (int)Arrow)
        {
            // "=>" {return type}
            stream.Poll();
            if (stream.Poll() != (int)Ident)
            {
                throw new Exception("Expected proc return type name");
            }
            result.ReturnType = stream.Text;
        }

        return result;
    }
}
