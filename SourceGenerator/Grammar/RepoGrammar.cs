namespace SourceGenerator.Grammar;

using static Token;
using static ClassType;

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

    public static Dto Match(TokenStream stream, Dictionary<string, List<FieldGrammar.Dto>> splats)
    {
        var result = new Dto()
        {
            Procs = []
        };

        // "repo" "{"
        Program.StartSpan(TopLevel);
        stream.Poll();
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception($"Expected left curly");
        }
        Program.EndSpan();

        while (stream.Next != (int)RCurly)
        {
            // {dbo} "." {name} "(" {parameter list} ")" ["=>" {return type}]
            var proc = ActionGrammar.Match(stream, splats);
            if (proc.Api.Count > 0)
            {
                throw new Exception("API descriptors are not valid for stored procedures");
            }
            result.Procs.Add(proc);

            // ","
            Program.StartSpan(TopLevel);
            if (stream.Next != (int)RCurly && stream.Poll() != (int)Comma)
            {
                throw new Exception("Expected comma or '}'");
            }
            Program.EndSpan();
        }

        // "}"
        Program.StartSpan(TopLevel);
        stream.Poll();
        Program.EndSpan();

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
            var args = string.IsNullOrEmpty(proc.SplatFrom)
                ? string.Join(',', proc.Params.Select((it) => $"{it.type} {it.name}"))
                : $"{proc.SplatFrom} splat";
            Program.AppendLine("        public async {0} {1}({2})",
                proc.ReturnType == "void" ? "Task" : $"Task<{proc.ReturnType}>",
                proc.Name.Replace(".", "__"),
                args);
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
                if (!string.IsNullOrEmpty(proc.SplatFrom))
                {
                    Program.Append("splat.");
                }
                Program.Append(par);
            }
            Program.AppendLine("\n            }});");

            Program.AppendLine("        }}");
        }

        Program.AppendLine("    }}");
    }
}
