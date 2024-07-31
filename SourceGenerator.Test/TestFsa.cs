namespace SourceGenerator.Test;

public class TestFsa
{
    private void _CanMatch(
        List<(string, int)> patterns,
        string input, int start,
        int expectToken, string expectText)
    {
        var fsa = new Fsa();
        foreach (var (pattern, token) in patterns)
        {
            fsa.Build(pattern, token);
        }

        {
            var (a, m) = fsa.Search(input, start);
            Assert.Equal(expectToken, a);
            Assert.Equal(expectText, m);
        }

        fsa = fsa.ConvertToDfa();
        {
            var (a, m) = fsa.Search(input, start);
            Assert.Equal(expectToken, a);
            Assert.Equal(expectText, m);
        }

        fsa = fsa.MinimizeDfa();
        {
            var (a, m) = fsa.Search(input, start);
            Assert.Equal(expectToken, a);
            Assert.Equal(expectText, m);
        }
    }

    [Fact]
    public void CanMatchWholePhrase()
    {
        _CanMatch(
            patterns:
            [
                ("abc", 1)
            ],
            input: "abc",
            start: 0,
            expectToken: 1,
            expectText: "abc"
            );
    }

    [Fact]
    public void CanMatchPartialPhrase()
    {
        _CanMatch(
            patterns:
            [
                ("abc", 1)
            ],
            input: "abcd",
            start: 0,
            expectToken: 1,
            expectText: "abc"
            );
    }

    [Fact]
    public void MatchesLongestPhrase()
    {
        _CanMatch(
            patterns:
            [
                ("abc", 1),
                ("abcd", 2)
            ],
            input: "abcd",
            start: 0,
            expectToken: 2,
            expectText: "abcd"
            );
    }

    [Fact]
    public void MatchesLowestPrecedence()
    {
        _CanMatch(
            patterns:
            [
                ("(a|b|c|d)+", 1),
                ("abcd", 2)
            ],
            input: "abcd",
            start: 0,
            expectToken: 1,
            expectText: "abcd"
            );
    }

    [Fact]
    public void MatchesLowestPrecedenceFlipped()
    {
        _CanMatch(
            patterns:
            [
                ("(a|b|c|d)+", 2),
                ("abcd", 1)
            ],
            input: "abcd",
            start: 0,
            expectToken: 1,
            expectText: "abcd"
            );
    }

    [Fact]
    public void CanMatchRemainderWithStartOffset()
    {
        _CanMatch(
            patterns:
            [
                ("bcd", 1)
            ],
            input: "abcd",
            start: 1,
            expectToken: 1,
            expectText: "bcd"
            );
    }

    [Fact]
    public void CanMatchPartialWithStartOffset()
    {
        _CanMatch(
            patterns:
            [
                ("bcd", 1)
            ],
            input: "abcd",
            start: 1,
            expectToken: 1,
            expectText: "bcd"
            );
    }

    [Fact]
    public void DfaIsDeterministic()
    {
        var fsa = new Fsa();
        fsa.Build("(a|b|c|d)+|abcd|abxx|abxy", 1);

        fsa = fsa.ConvertToDfa();

        Assert.DoesNotContain(fsa.Flat, (it) => it.Epsilon.Count > 0);
    }

    [Fact]
    public void SERIESMarksAccept()
    {
        var fsa = new Fsa();
        fsa.Build("abc", 1);

        Assert.Single(fsa.Flat, (it) => it.Accepts.Count > 0 && it.Letter == 'c');
    }

    [Fact]
    public void ORMarksAccept()
    {
        var fsa = new Fsa();
        fsa.Build("abc|abx", 1);

        Assert.Single(fsa.Flat, (it) => it.Accepts.Count > 0 && it.Letter == 'c');
        Assert.Single(fsa.Flat, (it) => it.Accepts.Count > 0 && it.Letter == 'x');

        fsa = fsa.ConvertToDfa();

        Assert.Single(fsa.Flat, (it) => it.Accepts.Count > 0 && it.Letter == 'c');
        Assert.Single(fsa.Flat, (it) => it.Accepts.Count > 0 && it.Letter == 'x');

        fsa = fsa.MinimizeDfa();

        Assert.Single(fsa.Flat, (it) => it.Accepts.Count > 0);
    }

    [Fact]
    public void PARENSMarksAccept()
    {
        var fsa = new Fsa();
        fsa.Build("(abc|abx)", 1);

        Assert.Single(fsa.Flat, (it) => it.Accepts.Count > 0 && it.Letter == '\0');

        fsa = fsa.ConvertToDfa();

        Assert.Single(fsa.Flat, (it) => it.Accepts.Count > 0 && it.Letter == 'c');
        Assert.Single(fsa.Flat, (it) => it.Accepts.Count > 0 && it.Letter == 'x');

        fsa = fsa.MinimizeDfa();

        Assert.Single(fsa.Flat, (it) => it.Accepts.Count > 0);
    }

    [Fact]
    public void CanMatchBasicPLUS()
    {
        _CanMatch(
            patterns:
            [
                ("abc+", 1)
            ],
            input: "abcccd",
            start: 0,
            expectToken: 1,
            expectText: "abccc"
            );
    }

    [Fact]
    public void CanMatchPLUSAfterPARENS()
    {
        _CanMatch(
            patterns:
            [
                ("(abc)+", 1)
            ],
            input: "abcabc",
            start: 0,
            expectToken: 1,
            expectText: "abcabc"
            );
    }

    [Fact]
    public void CanMatchPartialPLUSAfterPARENS()
    {
        _CanMatch(
            patterns:
            [
                ("(abc)+", 1)
            ],
            input: "abcab",
            start: 0,
            expectToken: 1,
            expectText: "abc"
            );
    }

    [Fact]
    public void CanEndWithOR()
    {
        _CanMatch(
            patterns:
            [
                ("0|", 1)
            ],
            input: "1",
            start: 0,
            expectToken: 1,
            expectText: ""
            );

        _CanMatch(
            patterns:
            [
                ("0|", 1)
            ],
            input: "0",
            start: 0,
            expectToken: 1,
            expectText: "0"
            );
    }

    [Fact]
    public void CanMatchWithOptionalPLUS()
    {
        _CanMatch(
            patterns:
            [
                ("0(.0+|)", 1)
            ],
            input: "0.0001",
            start: 0,
            expectToken: 1,
            expectText: "0.000"
            );
    }

    [Fact]
    public void CanMatchWithoutOptionalPLUS()
    {
        _CanMatch(
            patterns:
            [
                ("0(.0+|)", 1)
            ],
            input: "0.1",
            start: 0,
            expectToken: 1,
            expectText: "0"
            );
    }

    [Fact]
    public void PLUSBoundedByOR()
    {
        _CanMatch(
            patterns:
            [
                ("0+|1+", 1)
            ],
            input: "000111",
            start: 0,
            expectToken: 1,
            expectText: "000"
            );
    }

    [Fact]
    public void PLUSBoundedByORAndPARENS()
    {
        _CanMatch(
            patterns:
            [
                ("0+(a+|b+)", 1)
            ],
            input: "000aaabbb",
            start: 0,
            expectToken: 1,
            expectText: "000aaa"
            );
    }

    [Fact]
    public void PLUSRejectedByORAndPARENS()
    {
        _CanMatch(
            patterns:
            [
                ("0+(a+|b+)1+", 1)
            ],
            input: "000aaabbb111",
            start: 0,
            expectToken: 0,
            expectText: ""
            );
    }

    [Fact]
    public void CanMatchEpsilonOnReject()
    {
        _CanMatch(
            patterns:
            [
                ("0+(a+|b+)1+", 1),
                ("", 2)
            ],
            input: "000aaabbb111",
            start: 0,
            expectToken: 2,
            expectText: ""
            );
    }

    [Fact]
    public void CanEscapePARENS()
    {
        _CanMatch(
            patterns:
            [
                ("0+\\(a+|b+\\)1+", 1)
            ],
            input: "0(ab)1",
            start: 0,
            expectToken: 1,
            expectText: "0(a"
            );
    }

    [Fact]
    public void CanMixEscapedPARENS()
    {
        _CanMatch(
            patterns:
            [
                ("0+(\\(a+|b+\\))+1+", 1)
            ],
            input: "0(aaabbb)123",
            start: 0,
            expectToken: 1,
            expectText: "0(aaabbb)1"
            );
    }

    [Fact]
    public void CanEscapePLUS()
    {
        _CanMatch(
            patterns:
            [
                ("0\\+1", 1)
            ],
            input: "0+1+2",
            start: 0,
            expectToken: 1,
            expectText: "0+1"
            );
    }

    [Fact]
    public void CanEscapeAndUsePLUS()
    {
        _CanMatch(
            patterns:
            [
                ("0\\++1", 1)
            ],
            input: "0+++1+++2",
            start: 0,
            expectToken: 1,
            expectText: "0+++1"
            );
    }

    [Fact]
    public void CanEscapePLUSAndUsePARENS()
    {
        _CanMatch(
            patterns:
            [
                ("(0\\+1)\\+", 1)
            ],
            input: "0+1+2",
            start: 0,
            expectToken: 1,
            expectText: "0+1+"
            );
    }

    [Fact]
    public void CanEscapeOR()
    {
        _CanMatch(
            patterns:
            [
                ("\\|0\\|1\\|2\\|", 1)
            ],
            input: "|0|1|2|3|",
            start: 0,
            expectToken: 1,
            expectText: "|0|1|2|"
            );
    }

    [Fact]
    public void CanEscapeORAndUsePARENS()
    {
        _CanMatch(
            patterns:
            [
                ("(\\|0\\|1\\|2\\|)", 1)
            ],
            input: "|0|1|2|3|",
            start: 0,
            expectToken: 1,
            expectText: "|0|1|2|"
            );
    }

    [Fact]
    public void CanMixEscapedOR()
    {
        _CanMatch(
            patterns:
            [
                ("(\\|0\\||\\|1\\|)2\\|", 1)
            ],
            input: "|0|2|3|",
            start: 0,
            expectToken: 1,
            expectText: "|0|2|"
            );
    }

    [Fact]
    public void CanDetectUnmatchedPARENS()
    {
        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(a|b|c", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("a|b|c)", 1);
        });
    }

    [Fact]
    public void CanDetectPLUSOnInfiniteLoop()
    {
        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("()+", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(a|)+", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("+", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(+)", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(())+", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("a++", 1);
        });
    }
}
