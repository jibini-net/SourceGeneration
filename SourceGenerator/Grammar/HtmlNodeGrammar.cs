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

        // "=" "{" {C# code} "}"
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

        if (SpecialTags.TryGetValue(result.Tag ?? "", out var kv))
        {
            var (validator, _) = kv;
            validator(result);
        }

        return result;
    }

    public delegate void SpecialValidator(Dto dto);
    public delegate void SpecialRenderBuilder(Dto dto, Action<string> buildDom, Action<string> buildLogic);

    public static Dictionary<string, (SpecialValidator, SpecialRenderBuilder)> SpecialTags = new()
    {
        ["unsafe"] = (
            (dto) =>
            {
                if (dto.Attribs.Count > 0)
                {
                    throw new Exception("'unsafe' has no available attributes");
                }
                if (dto.Children.Count != 1 || dto.Children.Single().Children is not null)
                {
                    throw new Exception("'unsafe' must have exactly one literal child");
                }
            },
            (dto, buildDom, buildLogic) =>
            {
                var child = dto.Children.Single();
                Write(child, buildDom, buildLogic, unsafeHtml: true);
            }),

        ["if"] = (
            (dto) =>
            {
                if (dto.Attribs.Count > 0)
                {
                    throw new Exception("'if' has no available attributes");
                }
                if (dto.Children.Count < 2
                    || dto.Children.Count > 3
                    || dto.Children.First().Children is not null)
                {
                    throw new Exception("Usage: 'if({predicate} {success} [else])'");
                }
            },
            (dto, buildDom, buildLogic) =>
            {
                buildLogic($"if ({dto.Children.First().InnerContent}) {{");
                Write(dto.Children[1], buildDom, buildLogic);
                buildLogic("}");
                if (dto.Children.Count >= 3)
                {
                    buildLogic("else {");
                    Write(dto.Children[2], buildDom, buildLogic);
                    buildLogic("}");
                }
            }),

        ["child"] = (
            (dto) =>
            {
                if (dto.Attribs.Count > 0)
                {
                    throw new Exception("'child' has no available attributes");
                }
                if (dto.Children.Count != 1
                    || dto.Children.First().Children is not null)
                {
                    throw new Exception("Usage: 'child({index})'");
                }
            },
            (dto, buildDom, buildLogic) =>
            {
                buildLogic($"await Children[{dto.Children.First().InnerContent}](state, writer);");
            }
        )
    };

    //TODO Improve
    public static void WriteSubComponent(Dto dto, Action<string> buildDom, Action<string> buildLogic)
    {
        buildLogic("{\n");

        foreach (var (child, i) in dto.Children.Select((it, i) => (it, i)))
        {
            buildLogic($"async Task _child_{i}(StateDump state, StringWriter writer) {{");

            Write(child, buildDom, buildLogic);

            buildLogic("await Task.CompletedTask;");
            buildLogic("}\n");
        }

        var assignActions = dto.Attribs.Select((kv) => $"component.{kv.Key} = ({kv.Value});");
        var creationAction = $@"
            await ((Func<Task<string>>)(async () => {{
                var indexByTag = (tagCounts[""{dto.Tag}""] = (tagCounts.GetValueOrDefault(""{dto.Tag}"", -1) + 1));
                var subState = state.GetOrAddChild(""{dto.Tag}"", indexByTag);
                var component = sp.GetService(typeof({dto.Tag}Base.IView)) as {dto.Tag}Base;
                component.LoadState(subState.State);
                {string.Join("\n                ", assignActions)}
                component.Children.AddRange(new Func<StateDump, StringWriter, Task>[] {{
                    {string.Join(", ", dto.Children.Select((_, i) => $"_child_{i}"))}
                }});
                return await component.RenderAsync(subState, indexByTag);
            }})).Invoke()

            ".Trim();
        buildDom(creationAction);

        buildLogic("\n        }");
    }

    //TODO Improve
    public static void WriteDomElement(Dto dto, Action<string> buildDom, Action<string> buildLogic)
    {
        string escStr(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var escAttr = ".ToString().Replace(\"\\\"\", \"&quot;\")";
        string attrib(string k, string v) => $" {escStr(k)}=\\\"\" + ({v}){escAttr} + \"\\\"";

        var drawTags = !string.IsNullOrEmpty(dto.Tag);
        if (drawTags)
        {
            var attribs = dto.Attribs.Select((kv) => attrib(kv.Key, kv.Value));
            buildDom($"\"<{dto.Tag}{string.Join("", attribs)}>\"");
        }

        foreach (var child in dto.Children)
        {
            Write(child, buildDom, buildLogic);
        }

        if (drawTags)
        {
            buildDom($"\"</{dto.Tag}>\"");
        }
    }

    //TODO Improve
    public static void WriteInnerContent(Dto dto, Action<string> buildDom, bool unsafeHtml = false)
    {
        var htmlEnc = unsafeHtml
            ? ""
            : "System.Web.HttpUtility.HtmlEncode";

        buildDom($"{htmlEnc}(({dto.InnerContent})?.ToString() ?? \"\")");
    }

    //TODO Improve
    public static void Write(Dto dto, Action<string> buildDom, Action<string> buildLogic, bool unsafeHtml = false)
    {
        if (dto.Children is null)
        {
            WriteInnerContent(dto, buildDom, unsafeHtml);
        } else if (!string.IsNullOrEmpty(dto.Tag)
            // Sub-components must follow capitalized naming convention
            && dto.Tag[0] >= 'A'
            && dto.Tag[0] <= 'Z')
        {
            WriteSubComponent(dto, buildDom, buildLogic);
        } else if (SpecialTags.TryGetValue(dto.Tag ?? "", out var kv))
        {
            var (_, builder) = kv;
            builder(dto, buildDom, buildLogic);
        } else
        {
            WriteDomElement(dto, buildDom, buildLogic);
        }
    }
}
