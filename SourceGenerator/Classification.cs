﻿namespace SourceGenerator;

using System.Text.Json.Serialization;

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
#pragma warning disable IDE1006 // Naming Styles
    // Start
    public int s { get; set; }
    // Length
    public int l { get; set; } = -1;
    // Classification
    public ClassType c { get; set; }
#pragma warning restore IDE1006 // Naming Styles
}

[JsonSerializable(typeof(MatchSpan), GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(List<MatchSpan>), GenerationMode = JsonSourceGenerationMode.Serialization)]
public partial class MatchSpanJsonContext : JsonSerializerContext
{
}
