using System.Text.Json;
using System.Text.Json.Serialization;

namespace Generated;

//TODO Key field
public class StateDump
{
    [JsonIgnore]
    public StateDump Parent { get; private set; }
    public string Tag { get; set; }
    public Dictionary<string, object> State { get; set; } = new();
    public List<StateDump> Children { get; set; } = new();

    public StateDump GetOrAddChild(string tag, int indexByTag)
    {
        var result = Children
            .Where((it) => it.Tag == tag)
            .Skip(indexByTag)
            .FirstOrDefault();
        if (result is null)
        {
            result = new()
            {
                Tag = tag
            };
            Children.Add(result);
        }
        result.Parent = this;
        return result;
    }

    public StateDump FindParent(string tag)
    {
        for (var temp = Parent;
            temp is not null;
            temp = temp.Parent)
        {
            if (temp.Tag == tag)
            {
                return temp;
            }
        }
        return null;
    }

    public T Get<T>(string name)
    {
        return (T)(State[name] = State[name].ParseIfNot<T>());
    }
}

public static class StateDumpExtensions
{
    public static T ParseIfNot<T>(this object o)
    {
        if (o is null)
        {
            return default;
        }
        if (o is JsonElement json && typeof(T) != typeof(JsonElement))
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        return (T)o;
    }
}
