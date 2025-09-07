namespace SourceGenerator.Test;

public class TestFsaExtensions
{
    [Fact]
    public async Task CanSerializeFsa()
    {
        var fsa = new Fsa();

        fsa.Build("abc", 1);
        fsa.Build("123", 2);

        fsa = fsa.ConvertToDfa().MinimizeDfa();

        _ = await fsa.WriteToString();
    }

    [Fact]
    public void CanDeserializeFsa()
    {
        var fsaString =
            """
            initial 0
            nodes
                n 0 tab
                    97  1
                    49  4
                n 1 tab
                    98  2
                n 2 tab
                    99  3
                n 3 acc 1
                n 4 tab
                    50  5
                n 5 tab
                    51  6
                n 6 acc 2
            """;
            
        var fsa = fsaString.ParseFsa();

        Assert.Equal(1, fsa.Search("abcdef", 0).accepted);
        Assert.Equal("abc", fsa.Search("abcdef", 0).match);

        Assert.Equal(2, fsa.Search("123456", 0).accepted);
        Assert.Equal("123", fsa.Search("123456", 0).match);
    }

    [Fact]
    public async Task CanSerializeAndDeserializeNfa()
    {
        string fsaString;
        {
            var fsa = new Fsa();

            fsa.Build("abc", 1);
            fsa.Build("abd", 2);

            fsaString = await fsa.WriteToString();
        }

        {
            var fsa = fsaString.ParseFsa();

            Assert.Equal(1, fsa.Search("abcdef", 0).accepted);
            Assert.Equal("abc", fsa.Search("abcdef", 0).match);

            Assert.Equal(2, fsa.Search("abddef", 0).accepted);
            Assert.Equal("abd", fsa.Search("abddef", 0).match);
        }
    }

    [Fact]
    public void CanDeserializeFsaWithCharList()
    {
        var fsaString =
            """
            initial 0
            nodes
                n 0 tab
                    li 97 98 99 e   1
                n 1 acc 1
            """;
        var fsa = fsaString.ParseFsa();

        Assert.Equal(1, fsa.Search("abcdef", 0).accepted);
        Assert.Equal("a", fsa.Search("abcdef", 0).match);
        Assert.Equal("b", fsa.Search("abcdef", 1).match);
        Assert.Equal("c", fsa.Search("abcdef", 2).match);
    }

    [Fact]
    public void CanDeserializeFsaWithCharListRange()
    {
        var fsaString =
            """
            initial 0
            nodes
                n 0 tab
                    li 97 to 99 100 e   1
                    li 101 to 102 e     2
                n 1 acc 1
                n 2 acc 2
            """;
        var fsa = fsaString.ParseFsa();

        Assert.Equal(1, fsa.Search("abcdef", 0).accepted);
        Assert.Equal("a", fsa.Search("abcdef", 0).match);
        Assert.Equal(1, fsa.Search("abcdef", 1).accepted);
        Assert.Equal("b", fsa.Search("abcdef", 1).match);
        Assert.Equal(1, fsa.Search("abcdef", 2).accepted);
        Assert.Equal("c", fsa.Search("abcdef", 2).match);
        Assert.Equal(1, fsa.Search("abcdef", 3).accepted);
        Assert.Equal("d", fsa.Search("abcdef", 3).match);

        Assert.Equal(2, fsa.Search("abcdef", 4).accepted);
        Assert.Equal("e", fsa.Search("abcdef", 4).match);
        Assert.Equal(2, fsa.Search("abcdef", 5).accepted);
        Assert.Equal("f", fsa.Search("abcdef", 5).match);
    }
}
