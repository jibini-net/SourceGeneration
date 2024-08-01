namespace SourceGenerator;

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
    /// is provided and any logical "ORs" or ambiguous tokens.
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
    /// is provided and any logical "ORs" or ambiguous tokens.
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

    protected void _ParsePLUS_Bounded(string word, ref int start, ref int end, ref List<Fsa> frontier)
    {
        throw new NotImplementedException("Bounded loops ('{}')");
    }

    protected void _ParsePLUS(string word, int start, out int end, out List<Fsa> frontier, bool escaped = false)
    {
        if (!escaped && (word[start] == '+' || word[start] == '{'))
        {
            end = start;
            goto infinite_loop_detected;
        }

        var epsState = new Fsa();
        epsState._ParsePARENS(word, start, out end, out frontier, escaped: escaped);

        if (end < word.Length && (word[end] == '+' || word[end] == '{'))
        {
            // Detect any paths from this state directly to the frontier--
            // creating a cycle here would cause infinite loops
            if (frontier.Contains(epsState) || frontier.Intersect(epsState.EpsilonClosure()).Any())
            {
                goto infinite_loop_detected;
            }

            if (word[end] == '{')
            {
                _ParsePLUS_Bounded(word, ref start, ref end, ref frontier);

                return;
            }

            end++;

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
        throw new ApplicationException($"Cannot use '+' or '{{}}' (at offset {end}) on the empty string");
    }

    protected void _ParsePARENS(string word, int start, out int end, out List<Fsa> frontier, bool escaped = false)
    {
        if (!escaped && word[start] == '(')
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
        } else
        {
            _ParseRANGE(word, start, out end, out frontier, escaped: escaped);
        }
    }

    protected void _ParseRANGE_Chars(string word, ref int start, ref int end, ref List<Fsa> frontier)
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

    protected void _ParseRANGE(string word, int start, out int end, out List<Fsa> frontier, bool escaped = false)
    {
        if (!escaped && word[start] == '[')
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

                _ParseRANGE_Chars(word, ref start, ref end, ref frontier);

                _escaped = false;
            }

            // Combine possible set of states down to one with epsilon
            frontier = frontier.MergeFrontier();

            if (end >= word.Length || word[end] != ']')
            {
                throw new ApplicationException($"Expected ']' at offset {end}");
            }
            end++;
        } else
        {
            _ParseLETTER(word, start, out end, out frontier, escaped: escaped);
        }
    }

    protected void _ParseLETTER(string word, int start, out int end, out List<Fsa> frontier, bool escaped = false)
    {
        var letter = word[start];

        if (!escaped && letter == '\\')
        {
            _ParseSERIES(word, start + 1, out end, out frontier, escaped: true);

            return;
        }

        var newState = new Fsa(letter);
        _AddTransition(letter, newState);

        end = start + 1;
        frontier = [newState];
    }
}
