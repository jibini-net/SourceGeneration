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
            Procs = new()
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

    public static void Write(Dto _)
    {
        Program.AppendLine("    // Datalayer repositories are not supported");
    }
}
