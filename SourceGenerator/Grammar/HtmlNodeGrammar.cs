namespace SourceGenerator.Grammar;

using static Token;
using static ClassType;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public class HtmlNodeGrammar
{
    public class Dto
    {
        public string Tag { get; set; }
        public string Alias { get; set; }
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
        } else
        {
            Program.StartSpan(ClassType.Assign);
            if (stream.Poll() != (int)Ident)
            {
                throw new Exception("Expected name for HTML attribute");
            } else
            {
                name = stream.Text;
            }
            Program.EndSpan();
        }

        // "=" "{" {C# code} "}"
        Program.StartSpan(ClassType.Assign);
        if (stream.Poll() != (int)Token.Assign)
        {
            throw new Exception("Expected '='");
        }
        Program.EndSpan();
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
        Program.StartSpan(Delimiter3);
        if (stream.Poll() != (int)LRfReduce)
        {
            throw new Exception("Expected '<>'");
        }
        Program.EndSpan();

        // [{fragment} ...]
        while (stream.Next != (int)RRfReduce)
        {
            var child = Match(stream);
            result.Children.Add(child);
        }

        // "</>"
        Program.StartSpan(Delimiter3);
        stream.Poll();
        Program.EndSpan();

        return result;
    }

    //TODO Escaping?
    public static Dto MatchStringSegment(TokenStream stream)
    {
        string escStr(string s) => s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "")
            .Replace("\n", "\\\\n\");\n        writer_append(writer, \"");

        // "<\">"
        Program.StartSpan(Delimiter3);
        if (stream.Poll() != (int)LMultiLine)
        {
            throw new Exception("Expected '<\">'");
        }
        Program.EndSpan();

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
        Program.StartSpan(Delimiter3);
        stream.Poll();
        Program.EndSpan();

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

        var delimStart = stream.Offset;
        // {tag} [{alias}]
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
        var special = SpecialTags.TryGetValue(result.Tag ?? "", out var specialKv);
        Program.StartSpan((special || (result.Tag[0] >= 'A' && result.Tag[0] <= 'Z'))
            ? Delimiter2
            : Delimiter, delimStart);
        Program.EndSpan();
        if (stream.Next == (int)Ident)
        {
            // "{alias name}"
            Program.StartSpan(ClassType.Assign);
            stream.Poll();
            Program.EndSpan();

            if (result.Tag[0] < 'A' || result.Tag[0] > 'Z')
            {
                throw new Exception("Aliases can only be applied to components");
            }
            result.Alias = stream.Text;
        }

        Program.StartSpan((special || (result.Tag[0] >= 'A' && result.Tag[0] <= 'Z'))
            ? Delimiter2
            : Delimiter);
        // "("
        if (stream.Poll() != (int)LParen)
        {
            throw new Exception("Expected left parens");
        }
        Program.EndSpan();

        // ["|" {name} "=" {value} ["," ...] "|"]
        if (stream.Next == (int)Bar)
        {
            Program.StartSpan(Delimiter2);
            // "|"
            stream.Poll();
            Program.EndSpan();

            while (stream.Next != (int)Bar)
            {
                var (name, value) = MatchAttribute(stream);
                if (result.Attribs.ContainsKey(name))
                {
                    throw new Exception($"Duplicate attribute named '{name}'");
                }
                result.Attribs[name] = value;

                Program.StartSpan(Delimiter2);
                // ","
                if (stream.Next != (int)Bar && stream.Poll() != (int)Comma)
                {
                    throw new Exception("Expected comma or '|'");
                }
                Program.EndSpan();
            }

            Program.StartSpan(Delimiter2);
            // "|"
            stream.Poll();
            Program.EndSpan();
        }

        // [{fragment} ...]
        while (stream.Next != (int)RParen)
        {
            var child = Match(stream);
            result.Children.Add(child);
        }

        // ")"
        Program.StartSpan((special || (result.Tag[0] >= 'A' && result.Tag[0] <= 'Z'))
            ? Delimiter2
            : Delimiter);
        stream.Poll();
        Program.EndSpan();

        if (special)
        {
            var (validator, _) = specialKv;
            validator(result);
        }

        return result;
    }

    public delegate void SpecialValidator(Dto dto);
    public delegate void SpecialRenderBuilder(Dto dto, Action<string> buildDom, Action<string> buildLogic);

    public static SpecialValidator LoopValidator(string opName, string argName)
    {
        return (dto) =>
        {
            if (dto.Attribs.Count > 0)
            {
                throw new Exception($"'{opName}' has no available attributes");
            }
            if (dto.Children.Count != 2
                || dto.Children.First().Children is not null)
            {
                throw new Exception($"Usage: '{opName}({{{argName}}} {{content}})'");
            }
        };
    }

    public static SpecialRenderBuilder LoopRenderBuilder(string opName)
    {
        return (dto, buildDom, buildLogic) =>
        {
            buildLogic($"{opName} ({dto.Children.First().InnerContent}) {{");
            Write(dto.Children[1], buildDom, buildLogic);
            buildLogic("}");
        };
    }

    public static Dictionary<string, (SpecialValidator, SpecialRenderBuilder)> SpecialTags = new()
    {
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

        ["while"] = (
            LoopValidator("while", "predicate"),
            LoopRenderBuilder("while")),

        ["for"] = (
            LoopValidator("for", "header"),
            LoopRenderBuilder("for")),

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
                buildLogic($"//CORE");
                buildLogic($"//await Children[{dto.Children.First().InnerContent}](writer);");
            }),

        /*
        ["parent"] = (
            (dto) =>
            {
                if (dto.Attribs.Count > 0)
                {
                    throw new Exception("'parent' has no available attributes");
                }
                if (dto.Children.Count != 2
                    || dto.Children[0].Children is not null
                    || dto.Children[1].Children is not null)
                {
                    throw new Exception("Usage: 'parent({tag} {name})'");
                }
            },
            (dto, buildDom, buildLogic) =>
            {
                buildLogic($"var {dto.Children[1].InnerContent} = state.FindParent(\"{dto.Children[0].InnerContent}\");");
            }),
        */

        ["br"] = (
            (dto) =>
            {
                if (dto.Children.Any() || dto.Attribs.Any())
                {
                    throw new Exception("Breaks do not have parameters nor accept children");
                }
            },
            (dto, buildDom, buildLogic) =>
            {
                buildDom("\"<br>\"");
            }
        ),

        /*
        ["using"] = (
            (dto) =>
            {
                if (dto.Attribs.Count > 0)
                {
                    throw new Exception("'using' has no available attributes");
                }
                if (dto.Children.Count != 1
                    || dto.Children.First().Children is not null)
                {
                    throw new Exception("Usage: 'using({namespace})'");
                }
            },
            (dto, buildDom, buildLogic) =>
            {
                throw new NotImplementedException("TODO Namespaces are not implemented");
            }
        ),

        ["inject"] = (
            (dto) =>
            {
                if (dto.Attribs.Count > 0)
                {
                    throw new Exception("'inject' has no available attributes");
                }
                if (dto.Children.Count != 2
                    || dto.Children[0].Children is not null
                    || dto.Children[1].Children is not null)
                {
                    throw new Exception("Usage: 'inject({type} {name})'");
                }
            },
            (dto, buildDom, buildLogic) =>
            {
                buildLogic($"var {dto.Children[1].InnerContent} = sp.GetRequiredService<{dto.Children[0].InnerContent}>();");
            }
        ),
        */
    };

    //TODO Improve
    public static void WriteSubComponent(Dto dto, Action<string> buildDom, Action<string> buildLogic)
    {
        var aliased = !string.IsNullOrEmpty(dto.Alias);
        //if (aliased)
        //{
        //    buildLogic($"StateDump {dto.Alias};");
        //}

        buildLogic("{\n");

        foreach (var (child, i) in dto.Children.Select((it, i) => (it, i)))
        {
            //CORE
            var unique = Guid.NewGuid().ToString().Replace("-", "_");
            buildLogic($"goto skip_{unique};");

            buildLogic($"{{//_child_{i}(StringWriter writer, ...) {{");

            Write(child, buildDom, buildLogic);

            buildLogic("}");

            buildLogic($"skip_{unique}:");
        }

        var assignActions = dto.Attribs.Select((kv) => $"component.{kv.Key} = ({kv.Value});");
        var creationAction = $@"
            ({{
                {dto.Tag}_t component = {{}};
                _RZ_MEMSET(&component, 0, sizeof({dto.Tag}_t));
                {string.Join("\n                ", assignActions)}
                //CORE
                //component.Children.AddRange(new RenderDelegate[] {{
                //    {string.Join(", ", dto.Children.Select((_, i) => $"_child_{i}"))}
                //}});
                _{dto.Tag}_render(&component, writer);
                """";
            }})

            ".Trim();
        buildDom(creationAction);

        buildLogic("\n        }");
    }

    //TODO Improve
    public static void WriteDomElement(Dto dto, Action<string> buildDom, Action<string> buildLogic)
    {
        string escStr(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        string attrib(string k, string v) => $"writer_append(writer, \" {escStr(k)}=\\\"\");writer_append(writer, {v});writer_append(writer, (\"\\\"";

        var drawTags = !string.IsNullOrEmpty(dto.Tag);
        if (drawTags)
        {
            var attribs = dto.Attribs.Select((kv) => attrib(kv.Key, kv.Value));
            buildDom($"(\"<{dto.Tag}{(dto.Attribs.Count > 0 ? "\"));" : "")}{string.Join("", attribs)}>\")");
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
        buildDom(dto.InnerContent);
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
