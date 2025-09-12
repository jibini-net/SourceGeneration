namespace Jibini.CommonUtil.DataStructures;

public static partial class FsaExtensions
{
    public static List<Fsa> MergeFrontier(this List<Fsa> frontier)
    {
        var merge = new Fsa();
        foreach (var state in frontier)
        {
            state.Epsilon.Add(merge);
        }
        return [merge];
    }
}

public partial class Fsa
{
    /// <summary>
    /// Creates new nodes in the FSA to match the provided word. The resulting
    /// machine is likely nondeterministic, depending on which regular expression
    /// is provided and any logical "ORs" or ambiguous tokens.<br />
    /// 
    /// Paths are not reused nor optimized at this stage. If a letter is already
    /// in the "next" list of a state, it is added as an epsilon transition.
    /// </summary>
    public void Build(string word, int accept, out List<Fsa> frontier)
    {
        _ParseOR(word, 0, out var end, out frontier);

        if (end < word.Length)
        {
            throw new ApplicationException($"Unexpected '{word[end]}' at offset {end}");
        }

        if (accept > 0)
        {
            foreach (var state in frontier)
            {
                // If there are already elements, tokens may be ambiguous
                state.Accepts.Add(accept);
            }
        }
    }

    /// <summary>
    /// Creates new nodes in the FSA to match the provided word. The resulting
    /// machine is likely nondeterministic, depending on which regular expression
    /// is provided and any logical "ORs" or ambiguous tokens.<br />
    /// 
    /// Paths are not reused nor optimized at this stage. If a letter is already
    /// in the "next" list of a state, it is added as an epsilon transition.
    /// </summary>
    public void Build(string word, int accept)
    {
        Build(word, accept, out var _);
    }

    protected void _AddTransition(char on, Fsa to)
    {
        if (Next.ContainsKey(on))
        {
            // Become nondeterministic on second occurrence of letter
            var epsState = new Fsa();
            epsState.Next[on] = to;

            Epsilon.Add(epsState);
        } else
        {
            Next[on] = to;
        }
    }

    protected void _ParsePARENS(string word, int start, out int end, out List<Fsa> frontier)
    {
        // Revert to top of parsing hierarchy
        _ParseOR(word, start + 1, out end, out frontier);

        // Combine possible set of states down to one with epsilon
        frontier = frontier.MergeFrontier();

        if (end >= word.Length || word[end] != ')')
        {
            throw new ApplicationException($"Expected ')' at offset {end}");
        }
        end++;
    }

    protected void _ParseOR(string word, int start, out int end, out List<Fsa> frontier)
    {
        // "- 1" to counteract the initial "++end"
        end = start - 1;
        frontier = [];

        do
        {
            _ParseSERIES(word, ++end, out end, out var _frontier);

            frontier.AddRange(_frontier);
        } while (end < word.Length && word[end] == '|');
    }

    protected void _ParseSERIES(string word, int start, out int end, out List<Fsa> frontier, bool escaped = false)
    {
        if (start >= word.Length
            || (!escaped && (word[start] == ')' || word[start] == '|')))
        {
            end = start;
            frontier = [this];

            return;
        }

        _ParsePLUS(word, start, out end, out frontier, escaped: escaped);

        frontier.Single()._ParseSERIES(word, end, out end, out frontier);
    }

    protected void _ParsePLUS(string word, int start, out int end, out List<Fsa> frontier, bool escaped = false)
    {
        if (!escaped && (word[start] == '+' || word[start] == '{' || word[start] == '*'))
        {
            end = start;
            goto infinite_loop_detected;
        }
        if (!escaped && word[start] == '?')
        {
            end = start;
            throw new ApplicationException($"Expected optional expression before offset {end}");
        }

        var epsState = new Fsa();
        epsState._ParseLETTER(word, start, out end, out frontier, escaped: escaped);

        if (end < word.Length
            && (word[end] == '+' || word[end] == '{' || word[end] == '?' || word[end] == '*'))
        {
            if (word[end] != '?'
                // Detect any paths from this state directly to the frontier--
                // creating a cycle here would cause infinite loops
                && (frontier.Contains(epsState) || frontier.Intersect(epsState.EpsilonClosure()).Any()))
            {
                goto infinite_loop_detected;
            }

            switch (word[end])
            {
                case '+':
                    end++;
                    break;

                case '{':
                    _EXT_ParsePLUS_Bounded(word, ref start, ref end, ref frontier, escaped: escaped);
                    return;

                case '?':
                    _EXT_ParsePLUS_Optional(word, ref start, ref end, ref frontier, escaped: escaped);
                    return;

                case '*':
                    _EXT_ParsePLUS_Star(word, ref start, ref end, ref frontier, escaped: escaped);
                    return;
            }

            // Keep loop as sub-state to avoid unintended transitions
            Epsilon.Add(epsState);
            foreach (var state in frontier)
            {
                state.Epsilon.Add(epsState);
            }
        } else
        {
            // No looping to consider; merge in sub-state's transitions
            Epsilon.AddRange(epsState.Epsilon);
            foreach (var (k, v) in epsState.Next)
            {
                _AddTransition(k, v);
            }
        }

        return;

    infinite_loop_detected:
        throw new ApplicationException($"Cannot use '+', '{{}}', or '*' (at offset {end}) on the empty string");
    }

    protected void _ParseLETTER(string word, int start, out int end, out List<Fsa> frontier, bool escaped = false)
    {
        var letter = word[start];

        switch (letter)
        {
            case '\\' when !escaped:
                _ParseSERIES(word, start + 1, out end, out frontier, escaped: true);
                return;

            case '(' when !escaped:
                _ParsePARENS(word, start, out end, out frontier);
                return;

            case '[' when !escaped:
                _EXT_ParseRANGE(word, start, out end, out frontier);
                return;
        }

        var newState = new Fsa(letter);
        _AddTransition(letter, newState);

        end = start + 1;
        frontier = [newState];
    }
}
