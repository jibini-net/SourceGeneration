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
            if (nextToken is not null)
            {
                return nextToken.Value;
            }
            return Peek();
        }
    }

    public string Text { get; private set; } = "";

    public int Peek()
    {
        if (Offset >= Source.Length)
        {
            Text = "";
            return (nextToken = -1).Value;
        }
        var (accepted, match) = Grammar.Search(Source, Offset);
        Text = match;
        nextToken = accepted;

        // Discard whitespace/comments
        if (accepted == 9999)
        {
            Poll();
            return Peek();
        }
        return accepted;
    }

    public int Poll()
    {
        var token = Next;
        nextToken = null;
        Offset += Text.Length;
        return token;
    }
}
