namespace SourceGenerator.Grammar;

using System.Text;
using System.Text.RegularExpressions;

using static Token;
using static ClassType;

/*
 * Implementation of source generation and semantic evaluation. The parser
 * operates top-down using recursive descent.
 */
public partial class TopLevelGrammar
{
    public static void MatchModel(TokenStream stream, string modelName, bool meta = false)
    {
        Dictionary<string, List<FieldGrammar.Dto>> splats = new();
        if (!meta)
        {
            Program.AppendLine("// Generated datalayers are not supported");
        }

        while (stream.Next > 0)
        {
            switch (stream.Next)
            {
                case (int)LCurly:
                    var cSharp = MatchCSharp(stream);
                    if (!meta)
                    {
                        Program.AppendLine(cSharp
                            .Replace("{", "{{")
                            .Replace("}", "}}"));
                    }
                    break;

                case (int)Schema:
                    _ = SchemaGrammar.Match(stream, modelName, splats);
                    break;

                case (int)Partial:
                    _ = PartialGrammar.Match(stream, modelName, splats);
                    break;

                case (int)Dto:
                    _ = PartialGrammar.Match(stream, modelName, splats, inherit: false);
                    break;

                case (int)Repo:
                    _ = RepoGrammar.Match(stream, splats);
                    break;

                case (int)Service:
                    _ = ServiceGrammar.Match(stream, modelName, splats);
                    break;
                    
                default:
                    throw new Exception($"Invalid token '{stream.Text}' for top-level");
            }
        }
    }

    public static void MatchView(TokenStream stream, string modelName, bool meta = false)
    {
        Dictionary<string, List<FieldGrammar.Dto>> splats = new();
        if (!meta)
        {
            Program.AppendLine("#include \"writer.h\"");
            Program.AppendLine("#include \"components.h\"");
            Program.AppendLine("//class {0} : {0}Base.IView",
                modelName);
            Program.AppendLine("//{{");
        }

        var renderBuilder = new StringBuilder();
        void buildDom(string expr)
        {
            renderBuilder.AppendLine($"        writer_append(writer, {expr});");
        }
        void buildLogic(string stmt)
        {
            renderBuilder.AppendLine($"        {stmt}");
        }

        if (!meta)
        {
            buildDom($"\"<!--{modelName}-->\"");
        }

        ServiceGrammar.Dto actions = new()
        {
            Actions = new()
        };
        while (stream.Next > 0)
        {
            switch (stream.Next)
            {
                case (int)LCurly:
                    var cSharp = MatchCSharp(stream);
                    if (!meta)
                    {
                        Program.AppendLine(cSharp
                            .Replace("{", "{{")
                            .Replace("}", "}}"));
                    }
                    break;

                case (int)State:
                    var schema = SchemaGrammar.Match(stream, modelName, splats);
                    if (!meta)
                    {
                        SchemaGrammar.Write(schema);
                        SchemaGrammar.WriteStateDump(schema, modelName);
                    }
                    break;

                case (int)Dto:
                    var dto = PartialGrammar.Match(stream, modelName, splats, inherit: false);
                    if (!meta)
                    {
                        PartialGrammar.Write(dto);
                    }
                    break;

                case (int)Interface:
                    actions = ServiceGrammar.Match(stream, modelName, splats);
                    if (!meta)
                    {
                        ServiceGrammar.WriteViewInterface(actions);
                    }
                    break;

                default:
                    var domElement = HtmlNodeGrammar.Match(stream);
                    if (!meta)
                    {
                        HtmlNodeGrammar.Write(domElement, buildDom, buildLogic);
                    }
                    break;
            }
        }

        if (!meta)
        {
            buildDom($"\"<!--/{modelName}-->\"");

            ServiceGrammar.WriteViewRenderer(renderBuilder.ToString(), modelName);
            ServiceGrammar.WriteViewController(actions);

            Program.AppendLine("//}}");
        }
    }

    public static string MatchCSharp(TokenStream stream)
    {
        // "{"
        Program.StartSpan(TopLevel);
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception("Wrap verbatim C# code in curly braces");
        }
        var bracketDepth = 1;

        // {C# expression} "}"
        int start = stream.Offset, length = 0;
        var escaped = false;
        for (;
            start + length < stream.Source.Length && bracketDepth > 0;
            length++)
        {
            switch (stream.Source[start + length])
            {
                case '{':
                    bracketDepth += escaped ? 0 : 1;
                    continue;
                case '}':
                    bracketDepth -= escaped ? 0 : 1;
                    continue;
                case '\\':
                    escaped = !escaped;
                    continue;
                default:
                    escaped = false;
                    continue;
            }
        }

        stream.Seek(start + length);
        Program.EndSpan();

        //TODO Fix issues with escaping curly braces in verbatim C#
        return NonPrecededBackslash()
            .Replace(stream.Source.Substring(start, length - 1), "")
            // This doesn't catch "\\" which becomes "\"
            .Replace("\\\\", "\\");
    }

    [GeneratedRegex("(?<!\\\\)\\\\")]
    private static partial Regex NonPrecededBackslash();
}
