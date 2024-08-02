namespace SourceGenerator.Test;

public partial class TestFsa
{
    [Fact]
    public void CanMatchRANGELetters()
    {
        _CanMatch(
            patterns:
            [
                ("a[abcdef]+g", 1)
            ],
            input: "adefg",
            start: 0,
            expectToken: 1,
            expectText: "adefg"
            );

        _CanMatch(
            patterns:
            [
                ("a[a-cd-f]+g", 1)
            ],
            input: "adefg",
            start: 0,
            expectToken: 1,
            expectText: "adefg"
            );
    }

    [Fact]
    public void CanMakeRANGEOptional()
    {
        _CanMatch(
            patterns:
            [
                ("a([abcdef]+|)g", 1)
            ],
            input: "ag",
            start: 0,
            expectToken: 1,
            expectText: "ag"
            );

        _CanMatch(
            patterns:
            [
                ("a([abcdef]+|)", 1)
            ],
            input: "ag",
            start: 0,
            expectToken: 1,
            expectText: "a"
            );

        _CanMatch(
            patterns:
            [
                ("([abcdef]+|)", 1)
            ],
            input: "gh",
            start: 0,
            expectToken: 1,
            expectText: ""
            );
    }

    [Fact]
    public void CanEscapeRANGE()
    {
        _CanMatch(
            patterns:
            [
                ("a\\[abcdef]+g", 1)
            ],
            input: "a[abcdef]]g",
            start: 0,
            expectToken: 1,
            expectText: "a[abcdef]]g"
            );
    }

    [Fact]
    public void CanEscapeRANGEChar()
    {
        _CanMatch(
            patterns:
            [
                ("a[a\\-z\\]]+g", 1)
            ],
            input: "a]z-agz",
            start: 0,
            expectToken: 1,
            expectText: "a]z-ag"
            );

        _CanMatch(
            patterns:
            [
                ("a[\\-abcdef\\\\]+g", 1)
            ],
            input: "ade\\f-gz",
            start: 0,
            expectToken: 1,
            expectText: "ade\\f-g"
            );
    }

    [Fact]
    public void CanDetectRANGEUnexpectedDash()
    {
        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("[-abc]", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("[a-z-0]", 1);
        });
    }

    [Fact]
    public void CanDetectRANGEUnfinishedDash()
    {
        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("[A-Za-]", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("[A-Za-", 1);
        });
    }

    [Fact]
    public void CanDetectRANGEUnmatchedBracket()
    {
        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("[A-Za-z", 1);
        });
    }

    [Fact]
    public void CanEscapeBoundedPLUS()
    {
        _CanMatch(
            patterns:
            [
                ("0\\{1,3}1", 1)
            ],
            input: "0{1,3}12",
            start: 0,
            expectToken: 1,
            expectText: "0{1,3}1"
            );
    }

    [Fact]
    public void CanMatchBoundedPLUS()
    {
        _CanMatch(
            patterns:
            [
                ("[abc]{0,1}", 1)
            ],
            input: "defdef",
            start: 0,
            expectToken: 1,
            expectText: ""
            );

        _CanMatch(
            patterns:
            [
                ("[abc]{1,1}", 1)
            ],
            input: "abcabc",
            start: 0,
            expectToken: 1,
            expectText: "a"
            );

        _CanMatch(
            patterns:
            [
                ("[abc]{1,2}", 1)
            ],
            input: "abcabc",
            start: 0,
            expectToken: 1,
            expectText: "ab"
            );

        _CanMatch(
            patterns:
            [
                ("[abc]{1,3}", 1)
            ],
            input: "abcabc",
            start: 0,
            expectToken: 1,
            expectText: "abc"
            );
    }

    [Fact]
    public void CanMatchExactBoundedPLUS()
    {
        _CanMatch(
            patterns:
            [
                ("[abc]{6}", 1)
            ],
            input: "abcabc",
            start: 0,
            expectToken: 1,
            expectText: "abcabc"
            );

        _CanMatch(
            patterns:
            [
                ("[abc]{5}", 1)
            ],
            input: "abcabc",
            start: 0,
            expectToken: 1,
            expectText: "abcab"
            );

        _CanMatch(
            patterns:
            [
                ("[abc]{6}", 1)
            ],
            input: "abcab",
            start: 0,
            expectToken: 0,
            expectText: ""
            );
    }

    [Fact]
    public void CanMatchLowerBoundedPLUS()
    {
        _CanMatch(
            patterns:
            [
                ("a{2+}", 1)
            ],
            input: "aab",
            start: 0,
            expectToken: 1,
            expectText: "aa"
            );

        _CanMatch(
            patterns:
            [
                ("a{2+}", 1)
            ],
            input: "aaab",
            start: 0,
            expectToken: 1,
            expectText: "aaa"
            );

        _CanMatch(
            patterns:
            [
                ("a{2+}", 1)
            ],
            input: "aaaab",
            start: 0,
            expectToken: 1,
            expectText: "aaaa"
            );

        _CanMatch(
            patterns:
            [
                ("a{2+}", 1)
            ],
            input: "a",
            start: 0,
            expectToken: 0,
            expectText: ""
            );

        _CanMatch(
            patterns:
            [
                ("a{0+}", 1)
            ],
            input: "b",
            start: 0,
            expectToken: 1,
            expectText: ""
            );
    }

    [Fact]
    public void CanEscapeAndUseBoundedPLUS()
    {
        _CanMatch(
            patterns:
            [
                ("\\[abc\\]{1,3}", 1)
            ],
            input: "[abc]]]]",
            start: 0,
            expectToken: 1,
            expectText: "[abc]]]"
            );

        _CanMatch(
            patterns:
            [
                ("\\[abc\\]{2+}", 1)
            ],
            input: "[abc]]]]",
            start: 0,
            expectToken: 1,
            expectText: "[abc]]]]"
            );

        _CanMatch(
            patterns:
            [
                ("\\[{2+}abc\\]", 1)
            ],
            input: "[[[abc]",
            start: 0,
            expectToken: 1,
            expectText: "[[[abc]"
            );
    }

    [Fact]
    public void CanEscapeAndUseOptional()
    {
        _CanMatch(
            patterns:
            [
                ("\\[?abc\\]", 1)
            ],
            input: "abc]",
            start: 0,
            expectToken: 1,
            expectText: "abc]"
            );
    }

    [Fact]
    public void CanDetectRANGEBackwardsOrder()
    {
        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("[z-a]", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("[9-0]", 1);
        });
    }

    [Fact]
    public void CanDetectBoundedPLUSOnInfiniteLoop()
    {
        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(){1,3}", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(a|){1,3}", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("{1,3}", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("({1,3})", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(()){1,3}", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("a{1,3}{1,3}", 1);
        });
    }

    [Fact]
    public void CanDetectBoundedPLUSMissingBound()
    {
        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(abc){}", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(abc){,}", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(abc){1,}", 1);
        });
    }

    [Fact]
    public void CanDetectBoundedPLUSMissingBrace()
    {
        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(abc){1,", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(abc){", 1);
        });
    }

    [Fact]
    public void CanDetectBoundedPLUSBackwards()
    {
        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(abc){3,1}", 1);
        });
    }

    [Fact]
    public void CanDetectPLUSInfiniteLoopOnOptional()
    {
        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("((abc)?)+", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("((abc){0,1})+", 1);
        });
    }

    [Fact]
    public void CanMatchOptionalExpression()
    {
        _CanMatch(
            patterns:
            [
                ("a(bc)?d", 1)
            ],
            input: "ade",
            start: 0,
            expectToken: 1,
            expectText: "ad"
            );

        _CanMatch(
            patterns:
            [
                ("a(bc)?d", 1)
            ],
            input: "abcde",
            start: 0,
            expectToken: 1,
            expectText: "abcd"
            );

        _CanMatch(
            patterns:
            [
                ("a(bc)?d", 1)
            ],
            input: "abcbcde",
            start: 0,
            expectToken: 0,
            expectText: ""
            );
    }

    [Fact]
    public void CanMatchStarLoopExpression()
    {
        _CanMatch(
            patterns:
            [
                ("a(bc)*d", 1)
            ],
            input: "ade",
            start: 0,
            expectToken: 1,
            expectText: "ad"
            );

        _CanMatch(
            patterns:
            [
                ("a(bc)*d", 1)
            ],
            input: "abcde",
            start: 0,
            expectToken: 1,
            expectText: "abcd"
            );

        _CanMatch(
            patterns:
            [
                ("a(bc)*d", 1)
            ],
            input: "abcbcde",
            start: 0,
            expectToken: 1,
            expectText: "abcbcd"
            );

        _CanMatch(
            patterns:
            [
                ("a(bc)*d", 1)
            ],
            input: "abcbc",
            start: 0,
            expectToken: 0,
            expectText: ""
            );

        _CanMatch(
            patterns:
            [
                ("a\\(*b", 1)
            ],
            input: "a(((bc",
            start: 0,
            expectToken: 1,
            expectText: "a(((b"
            );
    }

    [Fact]
    public void CanDetectStarOnInfiniteLoop()
    {
        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("()*", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(a|)*", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("*", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(*)", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(())*", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("a**", 1);
        });

        Assert.Throws<ApplicationException>(() =>
        {
            var fsa = new Fsa();
            fsa.Build("(a*)*", 1);
        });
    }
}
