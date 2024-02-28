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
            Program.AppendLine("typedef struct {0}",
                modelName);
            Program.AppendLine("{{");
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
                    var schema = SchemaGrammar.Match(stream, modelName, splats);
                    if (!meta)
                    {
                        SchemaGrammar.Write(schema);
                    }
                    break;

                case (int)Partial:
                    var partial = PartialGrammar.Match(stream, modelName, splats);
                    if (!meta)
                    {
                        PartialGrammar.Write(partial);
                    }
                    break;

                case (int)Dto:
                    var dto = PartialGrammar.Match(stream, modelName, splats, inherit: false);
                    if (!meta)
                    {
                        PartialGrammar.Write(dto);
                    }
                    break;

                case (int)Repo:
                    var repo = RepoGrammar.Match(stream, splats);
                    if (!meta)
                    {
                        RepoGrammar.Write(repo);
                    }
                    break;

                case (int)Service:
                    var services = ServiceGrammar.Match(stream, modelName, splats);
                    if (!meta)
                    {
                        ServiceGrammar.WriteServiceInterface(services);
                        ServiceGrammar.WriteDbService(services);
                        ServiceGrammar.WriteApiService(services);
                    }
                    break;
                    
                default:
                    throw new Exception($"Invalid token '{stream.Text}' for top-level");
            }
        }

        if (!meta)
        {
            Program.AppendLine("}} {0}_t;", modelName);
        }
    }

    public static void MatchView(TokenStream stream, string modelName, bool meta = false)
    {
        Dictionary<string, List<FieldGrammar.Dto>> splats = new();
        if (!meta)
        {
            Program.AppendLine("typedef struct {0}// : {0}Base.IView",
                modelName);
            Program.AppendLine("{{");
        }

        var renderBuilder = new StringBuilder();
        void buildDom(string expr)
        {
            renderBuilder.AppendLine($"//        await writer.WriteAsync({expr});");
        }
        void buildLogic(string stmt)
        {
            renderBuilder.AppendLine($"//        {stmt}");
        }

        if (!meta)
        {
            buildDom($"$\"<!--{modelName}-->\"");
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
                        //TODO Make protected
                        SchemaGrammar.Write(schema, accessLevel: "public");
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
            buildDom($"$\"<!--/{modelName}-->\"");

            ServiceGrammar.WriteViewRenderer(renderBuilder.ToString(), modelName);
            ServiceGrammar.WriteViewController(actions);
        }

        Program.AppendLine("}} {0}_t;", modelName);
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
