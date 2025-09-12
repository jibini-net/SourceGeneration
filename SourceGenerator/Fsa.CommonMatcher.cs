namespace Jibini.CommonUtil.DataStructures;

public partial class Fsa
{
    public enum CommonMatch
    {
        Numbers = 1,
        Letters,
        Whitespace = 9999
    }

    private static Fsa _CommonMatcher;
    public static Fsa CommonMatcher
    {
        get
        {
            if (_CommonMatcher is null)
            {
                var commonMatcher = new Fsa();
                commonMatcher.Build("[0-9]+", (int)CommonMatch.Numbers, (_) => Task.CompletedTask).Wait();
                commonMatcher.Build("[a-zA-Z]+", (int)CommonMatch.Letters, (_) => Task.CompletedTask).Wait();
                commonMatcher.Build("[ \n\r\t\v\f]+", (int)CommonMatch.Whitespace, (_) => Task.CompletedTask).Wait();

                _CommonMatcher = commonMatcher.ConvertToDfa((_, _) => Task.CompletedTask).Result.MinimizeDfa((_, _, _) => Task.CompletedTask).Result;
            }
            return _CommonMatcher;
        }
    }
}
