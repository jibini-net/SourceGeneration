namespace SourceGenerator;

/*
 * Encapsulates a source file and also the current cursor state of the lexer-
 * parser. It supports a single token of lookahead, which allows polling a
 * "current" token into memory then peeking the "next" token off the top of the
 * token stream.
 */
public class TokenStream
{
    public Fsa Grammar { get; set; } = new();
    public string Source { get; set; } = "";
    /*
     * Cursor position can be reset, but take care to anchor to token starts, to
     * remain within source, and to clear the cached "next token."
     */
    public int Offset { get; private set; }

    /*
     * Seeks the source cursor to the specified offset, clearing the cached
     * token to ensure it gets re-evaluated.
     */
    public void Seek(int offset)
    {
        Offset = offset;
        nextToken = null;
    }

    /*
     * Allows inspection of the current source position in the debugger.
     */
    public string Remaining => Source[Offset..];

    // Cached "next token" value re-evaluated when peeking
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

    /*
     * String content matched by the previous token, which can be further
     * interpreted for literals or identifiers.
     */
    public string Text { get; private set; } = "";

    /*
     * Searches the grammar for a token matching the characters found at the
     * source's cursor offset and onwards. Whitespace will be ignored.
     */
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

    /*
     * Searches the grammar for a token matching the characters found at the
     * source's cursor offset and onwards. Whitespace will be ignored. After
     * calling, the matched length of text will be consumed from source (and the
     * cursor will move forward by that number of characters).
     */ 
    public int Poll()
    {
        var token = Next;
        nextToken = null;
        Offset += Text.Length;
        return token;
    }
}
