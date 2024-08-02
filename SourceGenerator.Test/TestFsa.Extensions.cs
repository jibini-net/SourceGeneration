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
}
