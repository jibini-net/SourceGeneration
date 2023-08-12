namespace SourceGenerator;

public class TokenStream
{
    public Fsa Grammar { get; set; } = new();
    public string Source { get; set; } = "";
    public int Offset { get; set; }

    private int? nextToken;
    public int Next
    {
        get
        {
            Peek();
            return nextToken.Value;
        }
    }

    public string Text { get; private set; } = "";

    public int Peek()
    {
        if (nextToken is not null)
        {
            return nextToken.Value;
        }
        if (Offset >= Source.Length)
        {
            return (nextToken = -1).Value;
        }

        var (accepted, match) = Grammar.Search(Source, Offset);
        Text = match;
        return (nextToken = accepted).Value;
    }

    public int Poll()
    {
        var token = Peek();
        nextToken = null;
        Offset += Text.Length;
        return token;
    }
}
