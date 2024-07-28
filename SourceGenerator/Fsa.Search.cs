namespace SourceGenerator;

public partial class Fsa
{
    /// <summary>
    /// Traverses the FSA in a breadth-first fashion, allowing vectorized
    /// traversal of a frontier in case of nondeterministic automata.
    /// 
    /// A "frontier" refers to the set of nodes currently being visited. An
    /// "epsilon closure" refers to nodes related to the frontier (and the
    /// frontier itself) accessible without consuming any characters. Acceptance
    /// states are achieved if any node on the frontier or any node in the
    /// resulting epsilon closure has a token ID in its accept list.
    /// 
    /// Any reached accept state will update the "longest end" tracker, and the
    /// last recorded longest match is returned on the first invalid state.
    /// </summary>
    public (int accepted, string match) Search(string text, int startIndex)
    {
        // Used for deterministic paths
        var node = this;
        // Used once determinism ends
        List<Fsa> closure = null;
        int textIndex = startIndex, longestEnd = -1, match = 0;
        var nfaMode = false;

        for (;;)
        {
            if (!nfaMode && (node?.Epsilon?.Count ?? 0) > 0)
            {
                nfaMode = true;
                closure = node.EpsilonClosure().Distinct().ToList();
            }

            if (nfaMode)
            {
                // Any accept state in the frontier is a valid match
                var acceptState = closure.Where((it) => it.Accepts.Count > 0).FirstOrDefault();
                if (acceptState is not null)
                {
                    longestEnd = textIndex;
                    match = acceptState.Accepts.Min();
                }

                // "Invalid state" due to end of input or lack of next states
                if (textIndex >= text.Length || closure.Count == 0)
                {
                    break;
                }
            } else
            {
                // Any accept state in the frontier is a valid match
                if ((node?.Accepts?.Count ?? 0) > 0)
                {
                    longestEnd = textIndex;
                    match = node.Accepts.Min();
                }

                // "Invalid state" due to end of input or lack of next states
                if (textIndex >= text.Length || node is null)
                {
                    break;
                }
            }

            var c = text[textIndex++];
            if (nfaMode)
            {
                var frontier = closure.SelectMany((it) => it.AdjacentSet(c)).Distinct();
                closure = frontier.SelectMany((it) => it.EpsilonClosure()).Distinct().ToList();
            } else
            {
                node = node.Next.GetValueOrDefault(c);
            }
        }

        if (longestEnd == -1)
        {
            return (0, "");
        } else
        {
            return (match, text.Substring(startIndex, longestEnd - startIndex));
        }
    }
}
