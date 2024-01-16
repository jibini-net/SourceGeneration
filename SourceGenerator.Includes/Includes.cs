﻿namespace Generated;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

public interface IModelDbAdapter
{
    Task ExecuteAsync(string procName, object args);
    Task<T> ExecuteAsync<T>(string procName, object args);
    Task<T> ExecuteForJsonAsync<T>(string procName, object args);
}

public interface IModelApiAdapter
{
    Task ExecuteAsync(string path, object args);
    Task<T> ExecuteAsync<T>(string path, object args);
}

public interface IModelDbWrapper
{
    Task ExecuteAsync(Func<Task> impl);
    Task<T> ExecuteAsync<T>(Func<Task<T>> impl);
}

public interface IRenderView
{
    Task<string> RenderAsync(StateDump state, int indexByTag = 0);
    Dictionary<string, object> GetState();
    void LoadState(Dictionary<string, object> state);
}

public class TagIndex
{
    public string Tag { get; set; }
    public int IndexByTag { get; set; }
    public bool Dependent { get; set; }
}

public class TagRenderRequest
{
    public StateDump State { get; set; }
    public List<TagIndex> Path { get; set; }
    public JsonElement Pars { get; set; }
}

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

public static class Extensions
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

    public static int GetNextIndexByTag(this Dictionary<string, int> tagCounts, string tag)
    {
        return tagCounts[tag] = (tagCounts.GetValueOrDefault(tag, -1) + 1);
    }

    public static async Task<string> RenderPageAsync(this IRenderView view, StateDump state = null)
    {
        state ??= new();

        view.LoadState(state.State);
        var html = await view.RenderAsync(state);

        var stateJson = JsonSerializer.Serialize(state);
        var stateComment = $"<!--{HttpUtility.HtmlEncode(stateJson).Replace("&quot;", "\"")}-->";
        return html + stateComment;
    }

    public static async Task<string> RenderComponentAsync<T>(this T _view, IServiceProvider sp, StateDump state, List<TagIndex> target, Func<T, Task> config)
        where T : IRenderView
    {
        state ??= new();
        IRenderView view = _view;

        var subState = state;
        Type parentType = null;
        StateDump parentState = null;

        foreach (var step in target)
        {
            if (step.Dependent)
            {
                parentState = subState;
                parentType = typeof(Extensions)
                    .Assembly
                    .GetType($"Generated.{parentState.Tag}Base")
                    .GetNestedType("IView");
            }
            subState = subState.GetOrAddChild(step.Tag, step.IndexByTag);
        }

        _view.LoadState(subState.State);
        await config(_view);
        subState.State = _view.GetState();

        if (parentType is not null)
        {
            view = sp.GetService(parentType) as IRenderView;
            subState = parentState;
        }
        var html = await view.RenderAsync(subState);

        var stateJson = JsonSerializer.Serialize(state);
        var stateComment = $"<!--{HttpUtility.HtmlEncode(stateJson).Replace("&quot;", "\"")}-->";
        return html + stateComment;
    }
}
