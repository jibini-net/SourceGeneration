namespace SourceGenerator;

public partial class Fsa
{
    public enum CommonMatch
    {
        Numbers = 1,
        Letters,
        Whitespace
    }

    private static Fsa _CommonMatcher;
    public static Fsa CommonMatcher
    {
        get
        {
            if (_CommonMatcher is null)
            {
                var commonMatcher = new Fsa();
                commonMatcher.Build("[0-9]+", (int)CommonMatch.Numbers);
                commonMatcher.Build("[a-zA-Z]+", (int)CommonMatch.Letters);
                commonMatcher.Build("[ \n\r\t]+", (int)CommonMatch.Whitespace);

                _CommonMatcher = commonMatcher.ConvertToDfa().MinimizeDfa();
            }
            return _CommonMatcher;
        }
    }
}
