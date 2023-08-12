namespace SourceGenerator.Grammar;

using static Token;

public class _Schema
{
    public static void Match(TokenStream stream, string modelName)
    {
        // "schema" "{"
        stream.Poll();
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception($"Expected left curly");
        }

        while (stream.Next != (int)RCurly)
        {
            var field = MatchField(stream);
            Console.WriteLine("    public {0} {1} {{ get; set; }}",
                field.TypeName.Replace("__model__", modelName),
                field.Name);

            if (stream.Next == (int)Assign)
            {
                var initial = MatchAssign(stream);
                Console.WriteLine("        = {0};",
                    initial.Replace("__model__", modelName));
            }

            // ","
            if (stream.Next != (int)RCurly && stream.Poll() != (int)Comma)
            {
                throw new Exception("Expected comma");
            }
        }

        // "}"
        stream.Poll();
    }

    public struct FieldDto
    {
        public string TypeName { get; set; }
        public string Name { get; set; }
    }

    public static FieldDto MatchField(TokenStream stream)
    {
        var result = new FieldDto();

        // {type name}
        if (stream.Poll() != (int)Ident)
        {
            throw new Exception("Expected field type name");
        }
        result.TypeName = stream.Text;

        // {field name}
        if (stream.Poll() != (int)Ident)
        {
            throw new Exception("Expected field member name");
        }
        result.Name = stream.Text;

        return result;
    }

    public static string MatchAssign(TokenStream stream)
    {
        // "=" "{"
        stream.Poll();
        if (stream.Poll() != (int)LCurly)
        {
            throw new Exception("Wrap initial values in curly brackets");
        }
        var bracketDepth = 1;

        // {C# expression} "}"
        int start = stream.Offset, length = 0;
        for (;
            start + length < stream.Source.Length && bracketDepth > 0;
            length++)
        {
            switch (stream.Source[start + length])
            {
                case '{':
                    bracketDepth++;
                    break;
                case '}':
                    bracketDepth--;
                    break;
            }
        }
        stream.Seek(start + length);
        return stream.Source.Substring(start, length - 1);
    }
}
