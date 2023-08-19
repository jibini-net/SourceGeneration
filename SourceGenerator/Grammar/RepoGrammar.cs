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
        Program.AppendLine("    public class Repository");
        Program.AppendLine("    {{");
        Program.AppendLine("        private readonly IModelDbAdapter db;");
        Program.AppendLine("        public Repository(IModelDbAdapter db)");
        Program.AppendLine("        {{");
        Program.AppendLine("            this.db = db;");
        Program.AppendLine("        }}");

        foreach (var proc in dto.Procs)
        {
            Program.AppendLine("        public async {0} {1}({2})",
                proc.ReturnType == "void" ? "Task" : $"Task<{proc.ReturnType}>",
                proc.Name.Replace(".", "__"),
                string.Join(',', proc.Params.Select((it) => $"{it.type} {it.name}")));
            Program.AppendLine("        {{");

            if (proc.ReturnType == "void")
            {
                Program.AppendLine("            await db.Execute{0}Async(\"{1}\", new\n            {{",
                    proc.IsJson ? "ForJson" : "",
                    proc.Name);
            } else
            {
                Program.AppendLine("            return await db.Execute{0}Async<{1}>(\"{2}\", new\n            {{",
                    proc.IsJson ? "ForJson" : "",
                    proc.ReturnType,
                    proc.Name);
            }
            foreach (var par in proc.Params.Select((it) => it.name))
            {
                if (par != proc.Params.First().name)
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
}
