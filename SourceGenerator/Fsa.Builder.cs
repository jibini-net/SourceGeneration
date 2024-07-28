namespace SourceGenerator;

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
        frontier = [this];
        // State which will be restored to when using "+" expression operator
        var restoreTo = this;
        var parensDepth = 0;
        var isEscaped = false;

        for (var regIndex = 0; regIndex < word.Length; regIndex++)
        {
            var c = word[regIndex];

            if (parensDepth > 0 || (c == ')' && !isEscaped))
            {
                // We are currently "within parentheses;" discard chars
                switch (c)
                {
                    case '(':
                        parensDepth++;
                        break;

                    case ')':
                        if (--parensDepth < 0) goto outer_break;
                        else break;
                }
                // Discard all characters, including last balanced ')'
                continue;
            }

            if (!isEscaped)
            {
                switch (c)
                {
                    case '\\':
                        isEscaped = true;
                        continue;

                    case '|':
                        {
                            var subExpr = new Fsa();
                            Epsilon.Add(subExpr);
                            subExpr.Build(word.Substring(regIndex + 1), accept, out var _frontier);
                            // Merge the "ORed" frontier with the parent frontier
                            frontier.AddRange(_frontier);
                        }
                        goto outer_break;

                    case '(':
                        {
                            // Enter parentheses discarding mode
                            parensDepth++;
                            var subExpr = restoreTo = new Fsa();
                            // Merge all states to parentheses using eps transitions
                            foreach (var state in frontier)
                            {
                                state.Epsilon.Add(subExpr);
                            }
                            subExpr.Build(word.Substring(regIndex + 1), 0, out frontier);
                        }
                        continue;

                    case '+':
                        foreach (var state in frontier)
                        {
                            state.Epsilon.Add(restoreTo);
                        }
                        continue;
                }
            }
            // Any non-escaping character resets to not being escaped
            isEscaped = false;

            // Always create a new node, which will likely become nondeterministic
            var useState = new Fsa(c);
            // Creates intermediate node to avoid infinite cyclic flow
            restoreTo = new Fsa();
            restoreTo.Next[c] = useState;

            foreach (var state in frontier)
            {
                if (state.Next.ContainsKey(c))
                {
                    // Tokens are nondeterministic via eps transitions
                    state.Epsilon.Add(restoreTo);
                } else
                {
                    state.Next[c] = useState;
                }
            }
            // After appending a char, frontier is always one merged branch
            frontier = [useState];
        }
    outer_break:

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
}
