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
        Dictionary<string, List<FieldGrammar.Dto>> splats = [];
        if (!meta)
        {
            Program.AppendLine("using Asp.Versioning;");
            Program.AppendLine("using Microsoft.AspNetCore.Authorization;");
            Program.AppendLine("using Microsoft.AspNetCore.Mvc;");
            Program.AppendLine("using Microsoft.AspNetCore.Http;");

            Program.AppendLine("public partial class {0}",
                modelName);
            Program.AppendLine("{{");
        }

        ServiceGrammar.Dto services = new()
        {
            Actions = []
        };
        while (stream.Next > 0)
        {
            switch (stream.Next)
            {
                case (int)LCurly:
                    var cSharp = MatchCSharp(stream);
                    if (!meta)
                    {
                        Program.AppendLine("{0}", cSharp);
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
                    services = ServiceGrammar.Match(stream, modelName, splats);
                    break;
                    
                default:
                    throw new Exception($"Invalid token '{stream.Text}' for top-level");
            }
        }

        if (!meta)
        {
            ServiceGrammar.WriteServiceInterface(services);
            ServiceGrammar.WriteDbService(services);
            ServiceGrammar.WriteApiService(services);

            Program.AppendLine("}}");

            //TODO Support emitting multiple files
            ServiceGrammar.WriteApiControllers(services);
        }
    }

    public static void MatchView(TokenStream stream, string modelName, bool meta = false)
    {
        Dictionary<string, List<FieldGrammar.Dto>> splats = [];
        if (!meta)
        {
            Program.AppendLine("using Microsoft.AspNetCore.Mvc;");
            Program.AppendLine("using System.Text;");
            Program.AppendLine("using System.Text.Json;");
            Program.AppendLine("using System.Web;");
            Program.AppendLine("using Microsoft.Extensions.DependencyInjection;");

            Program.AppendLine("public abstract class {0}Base : {0}Base.IView",
                modelName);
            Program.AppendLine("{{");
            Program.AppendLine("    // Extend and fully implement all actions in a subclass");

            Program.AppendLine("    private readonly IServiceProvider sp;");
            Program.AppendLine("    public readonly List<RenderDelegate> Children = new();");
            Program.AppendLine("    public {0}Base(IServiceProvider sp)\n    {{",
                modelName);
            Program.AppendLine("        this.sp = sp;");
            Program.AppendLine("    }}");
        }

        var renderBuilder = new StringBuilder();
        void buildDom(string expr)
        {
            renderBuilder.AppendLine($"        await writer.WriteAsync({expr});");
        }
        void buildLogic(string stmt)
        {
            renderBuilder.AppendLine($"        {stmt}");
        }

        if (!meta)
        {
            buildDom($"$\"<!--_{{((Children.Count > 0) ? '!' : null)}}open-{modelName}({{indexByTag}})-->\"");
        }

        ServiceGrammar.Dto actions = new()
        {
            Actions = []
        };
        while (stream.Next > 0)
        {
            switch (stream.Next)
            {
                case (int)LCurly:
                    var cSharp = MatchCSharp(stream);
                    if (!meta)
                    {
                        Program.AppendLine("{0}", cSharp);
                    }
                    break;

                case (int)State:
                    var schema = SchemaGrammar.Match(stream, modelName, splats);
                    if (!meta)
                    {
                        //TODO Make protected
                        SchemaGrammar.Write(schema, accessLevel: "public");
                        SchemaGrammar.WriteStateDump(schema);
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
            buildDom($"$\"<!--_{{((Children.Count > 0) ? '!' : null)}}close-{modelName}({{indexByTag}})-->\"");

            ServiceGrammar.WriteViewRenderer(renderBuilder.ToString(), modelName);

            Program.AppendLine("}}");

            //TODO Support emitting multiple files
            ServiceGrammar.WriteViewController(actions);
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
