namespace SourceGenerator;

public partial class Fsa
{
    /// <summary>
    /// Accessible nodes from this one, ignoring epsilon transitions from here.
    /// </summary>
    protected Dictionary<char, List<Fsa>> memoizedClosures = new();

    /// <summary>
    /// Returns a cached or calculated list of states accessible from this one
    /// after applying the character transition. Only checks epsilon on children.
    /// </summary>
    protected List<Fsa> AccessibleMemoized(char c)
    {
        return memoizedClosures.TryGetValue(c, out var cached)
            ? cached
            : (memoizedClosures[c] = AdjacentSet(c)
                .SelectMany((it) => it.EpsilonClosure())
                .Distinct()
                .ToList());
    }

    /// <summary>
    /// Performs an expensive conversion between NFA and DFA which calculates the
    /// epsilon closures at all states for all characters in the alphabet. State
    /// is calculated and cached during runtime, which renders the FSA invalid if
    /// any of the structure is later modified.
    /// 
    /// Do not modify the NFA again after calling the conversion to DFA; the NFA
    /// would continue to function, but this method would not.
    /// </summary>
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
        var partitions = Flat
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
}
