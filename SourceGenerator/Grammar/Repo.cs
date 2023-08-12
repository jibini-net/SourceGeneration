namespace SourceGenerator.Grammar;

using System.Text;

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
            // {SQL proc name} "("
            if (stream.Poll() != (int)Ident)
            {
                throw new Exception("Expected procedure name for repo");
            }
            var procName = stream.Text;
            if (stream.Poll() != (int)LParen)
            {
                throw new Exception("Expected left parens");
            }

            // "..."
            if (stream.Next == (int)Splat)
            {
                throw new NotImplementedException("Model field splat is not implemented");
            }

            var pars = new StringBuilder();
            var returnType = "void";
            var parNames = new List<string>();

            while (stream.Next != (int)RParen)
            {
                // {param type}
                if (stream.Poll() != (int)Ident)
                {
                    throw new Exception("Expected proc parameter type");
                }
                pars.Append($"{stream.Text} ");

                // {param name}
                if (stream.Poll() != (int)Ident)
                {
                    throw new Exception("Expected proc parameter name");
                }
                pars.Append($"{stream.Text},");
                parNames.Add(stream.Text);

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
                returnType = stream.Text;
            }

            Console.WriteLine("        public {0} {1}({2})",
                returnType,
                procName.Replace(".", "__"),
                pars.ToString().TrimEnd(','));
            Console.WriteLine("        {");

            if (returnType == "void")
            {
                Console.WriteLine("            //TODO Code to execute void-result proc");
                Console.WriteLine("            //db.Execute(\"{0}\", new {{ ",
                    procName);
            } else
            {
                Console.WriteLine("            //TODO Code to read results from proc");
                Console.WriteLine("            return default;");
                Console.WriteLine("            //return db.Execute<{0}>(\"{1}\", new {{ ",
                    returnType,
                    procName);
            }
            foreach (var par in parNames)
            {
                if (par != parNames.First())
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
}
