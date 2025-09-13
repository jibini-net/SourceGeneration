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
    public async Task<List<Fsa>> Build(string word, int accept, Func<int, Fsa, Fsa, Task> cb, Action<Fsa> kill)
    {
        var (end, frontier) = await _ParseOR(word, 0, cb, kill);

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

        return frontier;
    }

    protected async Task _AddTransition(char on, Fsa to, Func<int, Fsa, Fsa, Task> cb, int index)
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

        await cb.Invoke(index, this, to);
    }

    protected async Task<(int end, List<Fsa> frontier)> _ParsePARENS(string word, int start, Func<int, Fsa, Fsa, Task> cb, Action<Fsa> kill)
    {
        // Revert to top of parsing hierarchy
        var (end, frontier) = await _ParseOR(word, start + 1, cb, kill);

        // Combine possible set of states down to one with epsilon
        frontier = frontier.MergeFrontier();

        if (end >= word.Length || word[end] != ')')
        {
            throw new ApplicationException($"Expected ')' at offset {end}");
        }
        end++;

        return (end, frontier);
    }

    protected async Task<(int end, List<Fsa> frontier)> _ParseOR(string word, int start, Func<int, Fsa, Fsa, Task> cb, Action<Fsa> kill)
    {
        // "- 1" to counteract the initial "++end"
        var end = start - 1;
        List<Fsa> frontier = [];

        do
        {
            List<Fsa> _frontier;
            (end, _frontier) = await _ParseSERIES(word, ++end, cb, kill);

            frontier.AddRange(_frontier);
        } while (end < word.Length && word[end] == '|');

        return (end, frontier);
    }

    protected async Task<(int end, List<Fsa> frontier)> _ParseSERIES(string word, int start, Func<int, Fsa, Fsa, Task> cb, Action<Fsa> kill, bool escaped = false)
    {
        if (start >= word.Length
            || (!escaped && (word[start] == ')' || word[start] == '|')))
        {
            return (start, [this]);
        }

        var (end, frontier) = await _ParsePLUS(word, start, cb, kill, escaped: escaped);

        (end, frontier) = await frontier.Single()._ParseSERIES(word, end, cb, kill);

        return (end, frontier);
    }

    protected async Task<(int end, List<Fsa> frontier)> _ParsePLUS(string word, int start, Func<int, Fsa, Fsa, Task> cb, Action<Fsa> kill, bool escaped = false)
    {
        int end;
        List<Fsa> frontier;
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
        (end, frontier) = await epsState._ParseLETTER(word, start, cb, kill, escaped: escaped);

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
                    (start, end, frontier) = await _EXT_ParsePLUS_Bounded(word, start, end, cb, kill, escaped: escaped);
                    kill(epsState);
                    return (end, frontier);

                case '?':
                    (start, end, frontier) = await _EXT_ParsePLUS_Optional(word, start, end, cb, kill, escaped: escaped);
                    kill(epsState);
                    return (end, frontier);

                case '*':
                    (start, end, frontier) = await _EXT_ParsePLUS_Star(word, start, end, cb, kill, escaped: escaped);
                    kill(epsState);
                    return (end, frontier);
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
                await _AddTransition(k, v, cb, end);
            }
        }
        kill(epsState);

        return (end, frontier);

    infinite_loop_detected:
        throw new ApplicationException($"Cannot use '+', '{{}}', or '*' (at offset {end}) on the empty string");
    }

    protected async Task<(int end, List<Fsa> frontier)> _ParseLETTER(string word, int start, Func<int, Fsa, Fsa, Task> cb, Action<Fsa> kill, bool escaped = false)
    {
        var letter = word[start];

        int end;
        List<Fsa> frontier;

        switch (letter)
        {
            case '\\' when !escaped:
                (end, frontier) = await _ParseSERIES(word, start + 1, cb, kill, escaped: true);
                return (end, frontier);

            case '(' when !escaped:
                (end, frontier) = await _ParsePARENS(word, start, cb, kill);
                return (end, frontier);

            case '[' when !escaped:
                (end, frontier) = await _EXT_ParseRANGE(word, start, cb);
                return (end, frontier);
        }

        var newState = new Fsa(letter);
        await _AddTransition(letter, newState, cb, start);

        end = start + 1;
        frontier = [newState];

        return (end, frontier);
    }
}
