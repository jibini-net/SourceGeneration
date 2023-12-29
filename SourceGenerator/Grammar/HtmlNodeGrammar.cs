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

    public static (string name, string value) MatchAttribute(TokenStream stream)
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
        var value = TopLevelGrammar.MatchCSharp(stream);

        return (name, value);
    }

    public static Dto MatchFragmentReduce(TokenStream stream)
    {
        var result = new Dto()
        {
            Children = new()
        };

        // "<>"
        if (stream.Poll() != (int)LRfReduce)
        {
            throw new Exception("Expected '<>'");
        }

        // [{fragment} ...]
        while (stream.Next != (int)RRfReduce)
        {
            var child = Match(stream);
            result.Children.Add(child);
        }

        // "</>"
        stream.Poll();

        return result;
    }

    public static Dto MatchStringSegment(TokenStream stream)
    {
        throw new Exception("Not yet implemented");
    }

    public static Dto Match(TokenStream stream)
    {
        switch (stream.Next)
        {
            case (int)LCurly:
                var cSharp = TopLevelGrammar.MatchCSharp(stream);
                return new()
                {
                    InnerContent = cSharp
                };

            case (int)Ident:
                // Handled below the switch statement
                break;

            case (int)LRfReduce:
                // "<>" [{fragment} ...] "</>"
                return MatchFragmentReduce(stream);

            case (int)LMultiLine:
                // "<\">" [{string content}] "</\">"
                return MatchStringSegment(stream);
        }

        // {tag} "("
        if (stream.Poll() != (int)Ident)
        {
            throw new Exception("Expected HTML tag identifier");
        }
        var result = new Dto()
        {
            Tag = stream.Text,
            Attribs = new(),
            Children = new()
        };
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
                var (name, value) = MatchAttribute(stream);
                if (result.Attribs.ContainsKey(name))
                {
                    throw new Exception($"Duplicate attribute named '{name}'");
                }
                result.Attribs[name] = value;

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
        var htmlEnc = "System.Web.HttpUtility.HtmlEncode";
        string escStr(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var escAttr = ".ToString().Replace(\"\\\"\", \"&quot;\")";
        string attrib(string k, string v) => $" {escStr(k)}=\\\"\" + ({v}){escAttr} + \"\\\"";

        if (dto.Children is null)
        {
            writeLine($"{htmlEnc}(({dto.InnerContent}).ToString())");
            return;
        }

        if (!string.IsNullOrEmpty(dto.Tag))
        {
            var attribs = dto.Attribs.Select((kv) => attrib(kv.Key, kv.Value));
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
