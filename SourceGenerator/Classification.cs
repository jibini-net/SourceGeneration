using System.Text.Json.Serialization;

namespace SourceGenerator;

public enum ClassType
{
    PlainText = 1,
    TopLevel,
    Delimiter,
    Delimiter2,
    Delimiter3,
    Assign,
    TypeName
}

public class MatchSpan
{
    // Start
    public int s { get; set; }
    // Length
    public int l { get; set; } = -1;
    // Classification
    public ClassType c { get; set; }
}

[JsonSerializable(typeof(MatchSpan), GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(List<MatchSpan>), GenerationMode = JsonSourceGenerationMode.Serialization)]
public partial class MatchSpanJsonContext : JsonSerializerContext
{
}
