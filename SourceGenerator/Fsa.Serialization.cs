using System.Text;

namespace SourceGenerator;

public static partial class FsaExtensions
{
    public static async Task WriteTo(this Fsa fsa, Stream stream)
    {
        var nodes = fsa.Flat.Select((it, i) => (node: it, id: i)).ToList();
        var nodeToId = nodes.ToDictionary((it) => it.node, (it) => it.id);

        using var writer = new StreamWriter(stream, leaveOpen: true);

        await writer.WriteAsync($"initial {nodeToId[fsa]} nodes ");

        foreach (var (node, id) in nodes)
        {
            await writer.WriteAsync($"\nn {id} ");

            if (node.Accepts.Count > 0)
            {
                await writer.WriteAsync("acc ");
                foreach (var acc in node.Accepts)
                {
                    await writer.WriteAsync($"{acc} ");
                }
            }

            if (node.Next.Count > 0)
            {
                await writer.WriteAsync("tab ");
                foreach (var grouping in node.Next.GroupBy((it) => nodeToId[it.Value]).Select((it) => it.OrderBy((it) => it.Key).ToList()))
                {
                    if (grouping.Count == 1)
                    {
                        await writer.WriteAsync($"{(int)grouping.Single().Key} {nodeToId[grouping.Single().Value]} ");
                    } else
                    {
                        await writer.WriteAsync("li ");

                        for (var remaining = grouping as IEnumerable<KeyValuePair<char, Fsa>>;
                            remaining?.FirstOrDefault().Value is not null;)
                        {
                            var prev = -1;
                            var sequential = remaining.TakeWhile((it) => prev == -1 | prev + 1 == (prev = it.Key)).ToList();

                            await writer.WriteAsync($"{(int)sequential.First().Key} ");
                            if (sequential.Count > 1)
                            {
                                await writer.WriteAsync($"to {(int)sequential.Last().Key} ");
                            }

                            remaining = remaining.Skip(sequential.Count);
                        }

                        await writer.WriteAsync("e ");

                        await writer.WriteAsync($"{nodeToId[grouping.First().Value]} ");
                    }
                }
            }

            if (node.Epsilon.Count > 0)
            {
                await writer.WriteAsync("eps ");
                foreach (var eps in node.Epsilon)
                {
                    await writer.WriteAsync($"{nodeToId[eps]} ");
                }
            }
        }

        await writer.FlushAsync();
    }

    public static async Task<string> WriteToString(this Fsa fsa)
    {
        using var stream = new MemoryStream();
        await fsa.WriteTo(stream);

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static Fsa ParseFsa(this string fsaString)
    {
        Dictionary<int, Fsa> nodes = null;
        var initial = -1;

        var stream = new TokenStream()
        {
            Source = fsaString,
            Grammar = Fsa.CommonMatcher
        };

        Fsa getOrAddState(int index)
        {
            return nodes.TryGetValue(index, out var _v) ? _v : (nodes[index] = new());
        }

        int parseIndexValue()
        {
            if (stream.Poll() != (int)Fsa.CommonMatch.Numbers)
            {
                throw new ArgumentException($"Expected index value (near {stream.Offset})", nameof(fsaString));
            }
            return int.TryParse(stream.Text, out var _v) ? _v : throw new FormatException($"Invalid number format");
        }

        IEnumerable<int> parseListItems()
        {
            // "li"
            stream.Poll();
            
            while (stream.Next == (int)Fsa.CommonMatch.Numbers)
            {
                // {number}
                var number = parseIndexValue();
                yield return number;

                if (stream.Next == (int)Fsa.CommonMatch.Letters && stream.Text == "to")
                {
                    // "to"
                    stream.Poll();

                    // {end number}
                    var endRange = parseIndexValue();
                    for (var i = number + 1; i <= endRange; i++)
                    {
                        yield return i;
                    }
                }
            }

            // "e"
            if (stream.Poll() != (int)Fsa.CommonMatch.Letters || stream.Text != "e")
            {
                throw new ArgumentException($"Expected 'e' (near {stream.Offset})", nameof(fsaString));
            }
        }

        IEnumerable<int> parseSingleOrListItems()
        {
            if (stream.Next == (int)Fsa.CommonMatch.Letters && stream.Text == "li")
            {
                return parseListItems();
            } else
            {
                return [parseIndexValue()];
            }
        }

        void parseInitial()
        {
            if (initial != -1)
            {
                throw new ArgumentException($"Duplicate 'initial' command (near {stream.Offset})", nameof(fsaString));
            }
            // "initial"
            stream.Poll();

            initial = parseIndexValue();
        }

        void parseAccepts(Fsa node)
        {
            // "acc"
            stream.Poll();

            while (stream.Next == (int)Fsa.CommonMatch.Numbers)
            {
                // {accept token}
                var token = parseIndexValue();
                node.Accepts.Add(token);
            }
        }

        void parseTransitionTable(Fsa node)
        {
            // "tab"
            stream.Poll();

            while (stream.Next == (int)Fsa.CommonMatch.Numbers
                || (stream.Next == (int)Fsa.CommonMatch.Letters && stream.Text == "li"))
            {
                // {character} | "li" {character} ... "e"
                var cInts = parseSingleOrListItems().ToList();
                if (cInts.Any((it) => it > char.MaxValue))
                {
                    throw new ArgumentException("Character out of range", nameof(fsaString));
                }

                // {to index}
                var index = parseIndexValue();

                foreach (var c in cInts)
                {
                    node.Next[(char)c] = getOrAddState(index);
                }
            }
        }

        void parseEpsilon(Fsa node)
        {
            // "eps"
            stream.Poll();

            while (stream.Next == (int)Fsa.CommonMatch.Numbers)
            {
                // {to index}
                var index = parseIndexValue();
                node.Epsilon.Add(getOrAddState(index));
            }
        }

        bool parseNodeDetail(Fsa node)
        {
            switch (stream.Next)
            {
                case (int)Fsa.CommonMatch.Letters when stream.Text == "acc":
                    parseAccepts(node);
                    return true;
                case (int)Fsa.CommonMatch.Letters when stream.Text == "tab":
                    parseTransitionTable(node);
                    return true;
                case (int)Fsa.CommonMatch.Letters when stream.Text == "eps":
                    parseEpsilon(node);
                    return true;

                default:
                    return false;
            }
        }

        void parseNode()
        {
            // "n"
            stream.Poll();

            var nodeIndex = parseIndexValue();
            var node = getOrAddState(nodeIndex);

            while(parseNodeDetail(node));
        }

        void parseNodes()
        {
            if (nodes is not null)
            {
                throw new ArgumentException($"Duplicate 'nodes' command (near {stream.Offset})", nameof(fsaString));
            }
            // "nodes"
            stream.Poll();

            nodes = [];

            while (stream.Next == (int)Fsa.CommonMatch.Letters && stream.Text == "n")
            {
                parseNode();
            }
        }

        while (stream.Next > 0)
        {
            if (stream.Next != (int)Fsa.CommonMatch.Letters)
            {
                throw new ArgumentException($"Expected command name (near {stream.Offset})", nameof(fsaString));
            }

            switch (stream.Text)
            {
                case "initial":
                    parseInitial();
                    break;
                case "nodes":
                    parseNodes();
                    break;

                default:
                    throw new ArgumentException($"Invalid command name (near {stream.Offset})", nameof(fsaString));
            }
        }

        if (initial == -1)
        {
            throw new ArgumentException("Expected an initial state identifier", nameof(fsaString));
        }
        if (nodes is null)
        {
            throw new ArgumentException("Expected a node list", nameof(fsaString));
        }
        if (!nodes.TryGetValue(initial, out Fsa fsa))
        {
            throw new ArgumentException("Invalid initial index", nameof(fsaString));
        }
        return fsa;
    }
}
