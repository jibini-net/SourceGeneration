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

    //TODO Escaping?
    public static Dto MatchStringSegment(TokenStream stream)
    {
        string escStr(string s) => s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "")
            .Replace("\n", "\\n\"\n            + \"");

        // "<\">"
        if (stream.Poll() != (int)LMultiLine)
        {
            throw new Exception("Expected '<\">'");
        }

        // {string content}
        var startIndex = stream.Offset;
        while (stream.Next != (int)RMultiLine)
        {
            stream.Seek(stream.Offset + 1);
            if (stream.Offset >= stream.Source.Length)
            {
                throw new Exception("Expected '</\">'");
            }
        }
        var length = stream.Offset - startIndex;
        var content = stream.Source.Substring(startIndex, length);

        // "</\">"
        stream.Poll();

        return new()
        {
            InnerContent = $"\"{escStr(content)}\""
        };
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

        if (result.Tag == "unsafe")
        {
            if (result.Attribs.Count > 0)
            {
                throw new Exception("'unsafe' has no available attributes");
            }
            if (result.Children.Count != 1 || result.Children.First().Children is not null)
            {
                throw new Exception("'unsafe' must have exactly one literal child");
            }
        }

        return result;
    }

    //TODO Improve
    public static void WriteSubComponent(Dto dto, Action<string> write)
    {
        var assignActions = dto.Attribs.Select((kv) => $"component.{kv.Key} = ({kv.Value});");
        var creationAction = $@"
            await ((Func<Task<string>>)(async () => {{
                var component = sp.GetService(typeof({dto.Tag}Base.IView)) as {dto.Tag}Base;
                {string.Join("\n                ", assignActions)}
                return await component.RenderAsync();
            }})).Invoke()
            ".Trim();
        write(creationAction);
    }

    //TODO Improve
    public static void WriteDomElement(Dto dto, Action<string> write)
    {
        string escStr(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var escAttr = ".ToString().Replace(\"\\\"\", \"&quot;\")";
        string attrib(string k, string v) => $" {escStr(k)}=\\\"\" + ({v}){escAttr} + \"\\\"";

        var drawTags = !string.IsNullOrEmpty(dto.Tag) && dto.Tag != "unsafe";
        if (drawTags)
        {
            var attribs = dto.Attribs.Select((kv) => attrib(kv.Key, kv.Value));
            write($"\"<{dto.Tag}{string.Join("", attribs)}>\"");
        }

        foreach (var child in dto.Children)
        {
            Write(child, write, unsafeHtml: dto.Tag == "unsafe");
        }

        if (drawTags)
        {
            write($"\"</{dto.Tag}>\"");
        }
    }

    //TODO Improve
    public static void WriteInnerContent(Dto dto, Action<string> write, bool unsafeHtml = false)
    {
        var htmlEnc = unsafeHtml
            ? ""
            : "System.Web.HttpUtility.HtmlEncode";

        write($"{htmlEnc}(({dto.InnerContent}).ToString())");
    }

    //TODO Improve
    public static void Write(Dto dto, Action<string> write, bool unsafeHtml = false)
    {
        if (dto.Children is null)
        {
            WriteInnerContent(dto, write, unsafeHtml);
        } else if (!string.IsNullOrEmpty(dto.Tag)
            // Sub-components must follow capitalized naming convention
            && dto.Tag[0] >= 'A'
            && dto.Tag[0] <= 'Z')
        {
            WriteSubComponent(dto, write);
        } else
        {
            WriteDomElement(dto, write);
        }
    }
}
