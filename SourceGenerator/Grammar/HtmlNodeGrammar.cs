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
        var result = new Dto();

        switch (stream.Next)
        {
            case (int)LCurly:
                var cSharp = TopLevelGrammar.MatchCSharp(stream);
                result.InnerContent = cSharp;
                return result;

            case (int)Ident:
                // Handled below the switch statement
                break;

            case (int)LRfReduce:
                // "<>" [{fragment} ...] "</>"
                stream.Poll();
                result.Children = new();
                while (stream.Next != (int)RRfReduce)
                {
                    var child = Match(stream);
                    result.Children.Add(child);
                }
                stream.Poll();
                return result;

            case (int)LMultiLine:
                break;
        }

        // {tag} "("
        if (stream.Poll() != (int)Ident)
        {
            throw new Exception("Expected HTML tag identifier");
        }
        result.Tag = stream.Text;
        result.Attribs = new();
        result.Children = new();
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
            var child = Match(stream);
            result.Children.Add(child);
        }

        // ")"
        stream.Poll();

        return result;
    }

    public static void Write(Dto dto, Action<string> writeLine)
    {
        if (dto.Children is null)
        {
            writeLine($"System.Web.HttpUtility.HtmlEncode(({dto.InnerContent}).ToString())");
            return;
        }

        if (!string.IsNullOrEmpty(dto.Tag))
        {
            var esc = (string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var attribs = dto.Attribs
                .Select((it) => $" {esc(it.Key)}=\\\"\" + ({it.Value}).ToString().Replace(\"\\\"\", \"&quot;\") + \"\\\"");

            writeLine($"\"<{dto.Tag}{string.Join("", attribs)}>\"");
        }

        foreach (var child in dto.Children)
        {
            Write(child, writeLine);
        }

        if (!string.IsNullOrEmpty(dto.Tag))
        {
            writeLine($"\"</{dto.Tag}>\"");
        }
    }
}
