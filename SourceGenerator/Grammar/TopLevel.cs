namespace SourceGenerator.Grammar;

using static Token;

public class _TopLevel
{
    public static void Match(TokenStream stream)
    {
        for (var token = stream.Next;
            token > 0;
            stream.Poll(), token = stream.Next)
        {
            switch (stream.Next)
            {
                case (int)Schema:
                    _Schema.Match(stream);
                    break;
                    /*
                case (int)Partial:
                    _Partial.Match(stream);
                    break;

                case (int)Repo:
                    _Repo.Match(stream);
                    break;

                case (int)Service:
                    _Service.Match(stream);
                    break;
                    */
                default:
                    throw new Exception($"Invalid token '{stream.Text}' for top-level");
            }
        }
    }
}
