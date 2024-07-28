using System.Text.Json.Serialization;

namespace SourceGenerator;

/*
 * Implements a naive Finite State Automaton which supports nondeterminism
 * through epsilon transitions. Each state of the machine has a mapping of next
 * states, keyed against characters, but also a set of epsilon transitions
 * which can be reached with no character actions.
 * 
 * The longest valid match from any merged FSA will be returned. In the case of
 * ambiguous tokens, the token of highest precedence (lowest ID) will match.
 */
[Serializable]
public partial class Fsa
{
    /*
     * Debug value indicating the character used to arrive in this state.
     */
    [JsonIgnore]
    public char Letter { get; private set; } = '\0';

    public Fsa()
    {
    }

    public Fsa(char letter)
    {
        Letter = letter;
    }

    // Enables (de-)serialization
    public Dictionary<string, Fsa> n
    {
        get => Next.ToDictionary((it) => it.Key + "", (it) => it.Value);
        set
        {
            Next = value
                .Where((it) => it.Key.Length == 1)
                .ToDictionary((it) => it.Key.First(), (it) => it.Value);
        }
    }

    /*
     * Set of transitions for particular letters; if all transitions are put
     * here, the FSA will be deterministic.
     */
    [JsonIgnore]
    public Dictionary<char, Fsa> Next { get; private set; } = new();

    // Enables (de-)serialization
    public List<string> a
    {
        get => Accepts.Select((it) => it.ToString()).ToList();
        set
        {
            Accepts = value.Select(int.Parse).ToList();
        }
    }

    /*
     * IDs of tokens which are accepted if this state is reached during a match.
     */
    [JsonIgnore]
    public List<int> Accepts { get; private set; } = new();

    /*
     * States which can be reached by taking no action, and are reached if the
     * parent state ("this") is reached.
     */
    [JsonIgnore]
    public List<Fsa> Epsilon { get; private set; } = new();

    /*
     * Finds all states accessible from this state without consuming any
     * characters, and also any states recursively accessible thereunder.
     */
    protected IEnumerable<Fsa> EpsilonClosure()
    {
        return new[] { this }
            .Concat(Epsilon)
            .Concat(Epsilon.SelectMany((it) => it.EpsilonClosure()));
    }

    /*
     * Single- or zero-element list of reachable deterministic states.
     */
    protected IEnumerable<Fsa> AdjacentSet(char c)
    {
        if (Next.ContainsKey(c))
        {
            yield return Next[c];
        }
        yield break;
    }

    /*
     * Traverses the FSA in a breadth-first fashion, allowing vectorized
     * traversal of a frontier in case of nondeterministic automata.
     * 
     * A "frontier" refers to the set of nodes currently being visited. An
     * "epsilon closure" refers to nodes related to the frontier (and the
     * frontier itself) accessible without consuming any characters. Acceptance
     * states are achieved if any node on the frontier or any node in the
     * resulting epsilon closure has a token ID in its accept list.
     * 
     * Any reached accept state will update the "longest end" tracker, and the
     * last recorded longest match is returned on the first invalid state.
     */
    public (int accepted, string match) Search(string text, int startIndex)
    {
        // Used for deterministic paths
        var node = this;
        // Used once determinism ends
        List<Fsa> closure = null;// = EpsilonClosure().Distinct().ToList();
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

    /*
     * Accessible nodes from this one, ignoring epsilon transitions from here.
     */
    protected Dictionary<char, List<Fsa>> memoizedClosures = new();

    /*
     * Returns a cached or calculated list of states accessible from this one
     * after applying the character transition. Only checks epsilon on children.
     */
    protected List<Fsa> AccessibleMemoized(char c)
    {
        return memoizedClosures.TryGetValue(c, out var cached)
            ? cached
            : (memoizedClosures[c] = AdjacentSet(c)
                .SelectMany((it) => it.EpsilonClosure())
                .Distinct()
                .ToList());
    }

    /*
     * Performs an expensive conversion between NFA and DFA which calculates the
     * epsilon closures at all states for all characters in the alphabet. State
     * is calculated and cached during runtime, which renders the FSA invalid if
     * any of the structure is later modified.
     * 
     * Do not modify the NFA again after calling the conversion to DFA; the NFA
     * would continue to function, but this method would not.
     */
    public Fsa ConvertToDfa()
    {
        var result = new Fsa()
        {
            Letter = Letter,
            Accepts = new(Accepts)
        };
        var queue = new Queue<(Fsa node, List<Fsa> closure)>();
        // Visited set for cycles and already-deterministic nodes
        var replace = new Dictionary<HashSet<Fsa>, Fsa>(HashSet<Fsa>.CreateSetComparer());

        queue.Enqueue((result, EpsilonClosure().Distinct().ToList()));
        do
        {
            var (node, oldClosure) = queue.Dequeue();
            // Find all actions which can be taken from this state
            var alphabet = oldClosure.SelectMany((it) => it.Next.Keys).Distinct().ToList();

            // Find all nodes accessible from all discovered characters
            var closures = alphabet.ToDictionary(
                (c) => c,
                (c) => oldClosure.SelectMany((it) => it.AccessibleMemoized(c)).ToList());
            
            foreach (var (c, closure) in closures)
            {
                var withLetters = closure.Where((it) => it.Letter == c).ToHashSet();
                // Find an existing state for target nodes
                if (replace.TryGetValue(withLetters, out var cached))
                {
                    node.Next[c] = cached;
                    continue;
                }

                var created = new Fsa()
                {
                    Letter = c,
                    // Merged node will accept any tokens accepted by originals
                    Accepts = closure.SelectMany((it) => it.Accepts).Distinct().ToList()
                };
                node.Next[c] = created;
                replace[withLetters] = created;

                queue.Enqueue((created, closure.ToList()));
            }
        } while (queue.Count > 0);

        return result;
    }

    // https://codereview.stackexchange.com/questions/30332/proper-way-to-compare-two-dictionaries
    public class DictionaryComparer<TKey, TValue> : IEqualityComparer<IDictionary<TKey, TValue>>
    {
        public DictionaryComparer()
        {
        }

        public bool Equals(IDictionary<TKey, TValue> x, IDictionary<TKey, TValue> y)
        {
            if (x.Count != y.Count)
                return false;
            // Original code does not properly resolve hash collisions
            //return GetHashCode(x) == GetHashCode(y);
            return x.Count == y.Count
                && x.All((it) => y.TryGetValue(it.Key, out var _v) && it.Value as object == _v as object);
        }

        public int GetHashCode(IDictionary<TKey, TValue> obj)
        {
            int hash = 0;
            foreach (KeyValuePair<TKey, TValue> pair in obj)
            {
                int key = pair.Key.GetHashCode();
                int value = pair.Value != null ? pair.Value.GetHashCode() : 0;
                hash ^= ShiftAndWrap(key, 2) ^ value;
            }
            return hash;
        }

        private int ShiftAndWrap(int value, int positions)
        {
            positions &= 0x1F;
            uint number = BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
            uint wrapped = number >> (32 - positions);
            return BitConverter.ToInt32(BitConverter.GetBytes((number << positions) | wrapped), 0);
        }
    }

    /// <summary>
    /// Traverses the entire network to find a distinct flattened node list.
    /// Extremely expensive, so call sparingly.
    /// </summary>
    private List<Fsa> _flat
    {
        get
        {
            var visited = new HashSet<Fsa>() { this };
            void findChildren(Fsa it)
            {
                foreach (var n in it.Next.Values
                    .Concat(it.Epsilon)
                    .Where(visited.Add))
                {
                    findChildren(n);
                }
            }
            findChildren(this);
            return visited.ToList();
        }
    }

    /// <summary>
    /// Creates the minimal number of states and transitions which match the
    /// same tokens as the original DFA, also in linear time.
    /// 
    /// States are calculated by iterative paritioning of nodes, breaking out
    /// partitions whose members are found to be distinguishable by their
    /// outgoing transitions on any letter in the alphabet.
    /// </summary>
    public Fsa MinimizeDfa()
    {
        var remap = new Dictionary<Fsa, List<Fsa>>();
        // Initial partition is by accept versus non-accept states, and also the
        // token(s) which are accepted by accept states
        var partitions = _flat
            .GroupBy((it) => string.Join(',', it.Accepts.Distinct().Order()))
            .Select((it) => it.ToList())
            .ToList();
        foreach (var p in partitions)
        {
            foreach (var n in p)
            {
                remap[n] = p;
            }
        }

        // Continues until all partitions are indistinguishable internally
        var prevCount = 0;
        while (prevCount != partitions.Count)
        {
            prevCount = partitions.Count;

            for (var i = 0; i < prevCount; i++)
            {
                var part = partitions[i];
                // Next partitions are on any nodes in disagreement about which
                // other partitions result from transitions on alphabet
                var newParts = part
                    .GroupBy((p) => p.Next
                            .ToDictionary(
                                (it) => it.Key,
                                (it) => remap[it.Value]),
                        new DictionaryComparer<char, List<Fsa>>())
                    .ToList();
                // Partition members are (currently) indistinguishable
                if (newParts.Count() == 1)
                {
                    continue;
                }

                var partsRanges = newParts
                    .Select((it) => it.ToList())
                    .ToList();
                // Replace partition at index, append any additional partitions
                partitions[i] = partsRanges.First();
                partitions.AddRange(partsRanges.Skip(1));

                foreach (var p in partsRanges)
                {
                    foreach (var n in p)
                    {
                        remap[n] = p;
                    }
                }
            }
        }

        return RemapPartitions(partitions);
    }

    /// <summary>
    /// Reconstructs the minimal state graph for the DFA given a valid set of
    /// partitions. The members in each partition must be indistinguishable.
    /// </summary>
    private Fsa RemapPartitions(List<List<Fsa>> parts)
    {
        var partMap = parts
            // Create one replacement node for each partition
            .Select((p) => (p, repl: new Fsa()))
            // Relate all child nodes to replacement
            .SelectMany((it) => it.p.Select((n) => (n, it.repl)))
            // Index all replacement nodes selected above
            .ToDictionary((it) => it.n, (it) => it.repl);
        var visited = new Dictionary<Fsa, Fsa>();

        Fsa remapPartitions(Fsa it)
        {
            if (visited.TryGetValue(it, out var _n))
            {
                return _n;
            }
            var replace = partMap[it];
            visited[it] = replace;

            replace.Accepts = replace.Accepts.Union(it.Accepts).ToList();
            foreach (var (c, n) in it.Next)
            {
                var replaceNext = remapPartitions(n);
                if (replace.Next.TryGetValue(c, out var _v) && _v != replaceNext)
                {
                    throw new Exception("Disagreement between partitions rebuilding minimized states");
                }
                replace.Next[c] = replaceNext;
            }

            return replace;
        }

        return remapPartitions(this);
    }
}
