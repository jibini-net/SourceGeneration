using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SourceGenerator;

/// <summary>
/// Implements a naive Finite State Automaton which supports nondeterminism
/// through epsilon transitions.Each state of the machine has a mapping of next
/// states, keyed against characters, but also a set of epsilon transitions
/// which can be reached with no character actions.
/// 
/// The longest valid match from any merged FSA will be returned. In the case of
/// ambiguous tokens, the token of highest precedence (lowest ID) will match.
/// </summary>
[Serializable]
public partial class Fsa
{
    /// <summary>
    /// Debug value indicating the character used to arrive in this state. Only
    /// used while building the initial NFA.
    /// </summary>
    [JsonIgnore]
    public char Letter { get; private set; } = '\0';

    // Enables (de-)serialization
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Field for JSON serialization only")]
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

    /// <summary>
    /// Set of transitions for particular letters; if all transitions are put
    /// here, the FSA will be deterministic.
    /// </summary>
    [JsonIgnore]
    public Dictionary<char, Fsa> Next { get; private set; } = [];

    // Enables (de-)serialization
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Field for JSON serialization only")]
    public List<string> a
    {
        get => Accepts.Select((it) => it.ToString()).ToList();
        set
        {
            Accepts = value.Select(int.Parse).ToList();
        }
    }

    /// <summary>
    /// IDs of tokens which are accepted if this state is reached during a match.
    /// </summary>
    [JsonIgnore]
    public List<int> Accepts { get; private set; } = [];

    /// <summary>
    /// States which can be reached by taking no action, and are reached if the
    /// parent state ("this") is reached.
    /// </summary>
    [JsonIgnore]
    public List<Fsa> Epsilon { get; private set; } = [];

    public Fsa()
    {
    }

    public Fsa(char letter)
    {
        Letter = letter;
    }

    /// <summary>
    /// Finds all states accessible from this state without consuming any
    /// characters, and also any states recursively accessible thereunder.
    /// </summary>
    protected IEnumerable<Fsa> EpsilonClosure()
    {
        return new[] { this }
            .Concat(Epsilon)
            .Concat(Epsilon.SelectMany((it) => it.EpsilonClosure()));
    }

    /// <summary>
    /// Single- or zero-element collection of reachable deterministic states.
    /// </summary>
    protected IEnumerable<Fsa> AdjacentSet(char c)
    {
        if (Next.TryGetValue(c, out var _v))
        {
            yield return _v;
        }
        yield break;
    }

    /// <summary>
    /// Traverses the entire network to find a distinct flattened node list.
    /// </summary>
    private List<Fsa> Flat
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
            return [.. visited];
        }
    }
}
