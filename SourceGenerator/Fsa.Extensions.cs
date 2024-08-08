namespace SourceGenerator;

public partial class Fsa
{
    protected void _EXT_ParsePLUS_Bounded(string word, ref int start, ref int end, ref List<Fsa> frontier, bool escaped = false)
    {
        frontier = [];
        start -= escaped ? 1 : 0;

        var expr = word[start..end];
        int startNum, endNum;
        var canHaveMore = false;

        {
            var (token, match) = CommonMatcher.Search(word, ++end);
            if (token != (int)CommonMatch.Numbers)
            {
                goto expected_numeric;
            }
            startNum = int.Parse(match);

            end += match.Length;
        }

        if (end >= word.Length)
        {
            goto expected_operator;
        }
        switch (word[end])
        {
            case ',':
                {
                    var (token, match) = CommonMatcher.Search(word, ++end);
                    if (token != (int)CommonMatch.Numbers)
                    {
                        goto expected_numeric;
                    }
                    endNum = int.Parse(match);

                    end += match.Length;
                }
                if (endNum < startNum)
                {
                    throw new ApplicationException("Loop upper bound must be greater than or equal to lower bound");
                }

                break;

            case '+':
                end++;
                endNum = startNum;
                canHaveMore = true;
                break;

            case '}':
                endNum = startNum;
                break;

            default:
                goto expected_operator;
        }

        for (var c = startNum; c <= endNum; c++)
        {
            var builtExpr = string.Join("", Enumerable.Range(0, c).Select((_) => expr));
            _ParseSERIES(builtExpr, 0, out _, out var _frontier);

            frontier.AddRange(_frontier);
        }

        // Combine possible set of states down to one with epsilon
        frontier = frontier.MergeFrontier();

        if (canHaveMore)
        {
            frontier.Single()._ParsePARENS($"({expr}+|)", 0, out _, out frontier);
        }

        if (end >= word.Length || word[end] != '}')
        {
            throw new ApplicationException($"Expected '}}' at offset {end}");
        }
        end++;

        return;

    expected_operator:
        throw new ApplicationException($"Expected ',' or '+' at offset {end}");
    expected_numeric:
        throw new ApplicationException($"Expected numeric value at offset {end}");
    }

    protected void _EXT_ParsePLUS_Optional(string word, ref int start, ref int end, ref List<Fsa> frontier, bool escaped = false)
    {
        start -= escaped ? 1 : 0;
        var expr = word[start..end];

        _ParsePARENS($"({expr}|)", 0, out _, out frontier);
        
        end++;
    }

    protected void _EXT_ParsePLUS_Star(string word, ref int start, ref int end, ref List<Fsa> frontier, bool escaped = false)
    {
        start -= escaped ? 1 : 0;
        var expr = word[start..end];

        _ParsePARENS($"({expr}+|)", 0, out _, out frontier);

        end++;
    }

    protected void _EXT_ParseRANGE_Chars(string word, ref int end, ref List<Fsa> frontier)
    {
        var letter = word[end];

        var newState = new Fsa(letter);
        _AddTransition(letter, newState);

        frontier.Add(newState);

        if (end + 1 < word.Length && word[end + 1] == '-')
        {
            if (end + 2 >= word.Length || word[end + 2] == ']')
            {
                throw new ApplicationException($"Expected range end character at offset {end + 2}");
            }
            end += 2;

            var endLetter = word[end];
            if (endLetter <= letter)
            {
                throw new ApplicationException($"Range end (at offset {end}) must be greater than '{letter}'");
            }

            for (char c = (char)(letter + 1); c <= endLetter; c++)
            {
                var _newState = new Fsa(c);
                _AddTransition(c, _newState);

                frontier.Add(_newState);
            }
        }
    }

    protected void _EXT_ParseRANGE(string word, int start, out int end, out List<Fsa> frontier)
    {
        end = start + 1;
        frontier = [];

        for (var _escaped = false;
            end < word.Length && (_escaped || word[end] != ']');
            end++)
        {
            switch (word[end])
            {
                case '\\' when !_escaped:
                    _escaped = true;
                    continue;

                case '-' when !_escaped:
                    throw new ApplicationException($"Unexpected '-' at offset {end}");
            }

            _EXT_ParseRANGE_Chars(word, ref end, ref frontier);

            _escaped = false;
        }

        // Combine possible set of states down to one with epsilon
        frontier = frontier.MergeFrontier();

        if (end >= word.Length || word[end] != ']')
        {
            throw new ApplicationException($"Expected ']' at offset {end}");
        }
        end++;
    }
}
