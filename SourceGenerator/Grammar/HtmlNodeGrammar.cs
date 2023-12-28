namespace SourceGenerator.Grammar;

using static Token;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public class HtmlNodeGrammar
{
    public class Dto
    {
        public string Tag { get; set; }
        public Dictionary<string, string> Attribs { get; set; }
        public List<Dto> Children { get; set; }
        public string InnerContent { get; set; }
    }

    public static Dto Match(TokenStream stream)
    {
        // {tag} "("
        var result = new Dto()
        {
            Tag = stream.Text,
            Attribs = new(),
            Children = new()
        };
        stream.Poll();
        if (stream.Poll() != (int)LParen)
        {
            throw new Exception("Expected left parens");
        }

        // ["|" {name} "=" {value} ["," ...] "|"]
        if (stream.Next == (int)Bar)
        {
            // "|"
            stream.Poll();

            while (stream.Next != (int)Bar)
            {
                // {attrib name}
                string name;
                if (stream.Next == (int)LCurly)
                {
                    name = TopLevelGrammar.MatchCSharp(stream);
                } else if (stream.Poll() != (int)Ident)
                {
                    throw new Exception("Expected name for HTML attribute");
                } else
                {
                    name = stream.Text;
                }

                // "=" "{" {C# code} "}
                if (stream.Poll() != (int)Assign)
                {
                    throw new Exception("Expected '='");
                }
                if (result.Attribs.ContainsKey(name))
                {
                    throw new Exception($"Duplicate attribute named '{name}'");
                }
                result.Attribs[name] = TopLevelGrammar.MatchCSharp(stream);

                // ","
                if (stream.Next != (int)Bar && stream.Poll() != (int)Comma)
                {
                    throw new Exception("Expected comma or '|'");
                }
            }

            // "|"
            stream.Poll();
        }

        // [{fragment} ...]
        while (stream.Next != (int)RParen)
        {
            switch (stream.Next)
            {
                case (int)Ident:
                    result.Children.Add(Match(stream));
                    break;

                case (int)LCurly:
                    var cSharp = TopLevelGrammar.MatchCSharp(stream);
                    result.Children.Add(new()
                    {
                        InnerContent = cSharp
                    });
                    break;

                default:
                    throw new Exception($"Unexpected token '{stream.Text}' for HTML element");
            }
        }

        // ")"
        stream.Poll();

        return result;
    }

    public static void Write(Dto dto, Action<string> writeLine)
    {
        var esc = (string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var attribs = dto.Attribs
            .Select((it) => $" {esc(it.Key)}=\\\"\" + ({it.Value}).ToString().Replace(\"\\\"\", \"&quot;\") + \"\\\"");
        
        writeLine($"\"<{dto.Tag}{string.Join("", attribs)}>\"");

        foreach (var child in dto.Children)
        {
            if (!string.IsNullOrEmpty(child.Tag))
            {
                Write(child, writeLine);
            } else
            {
                writeLine($"System.Web.HttpUtility.HtmlEncode(({child.InnerContent}).ToString())");
            }
        }

        writeLine($"\"</{dto.Tag}>\"");
    }
}
