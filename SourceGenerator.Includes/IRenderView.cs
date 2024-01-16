using System.Text.Json;
using System.Web;

namespace Generated;

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

public static class RenderViewExtensions
{
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
                parentType = _view
                    .GetType()
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

    public static int GetNextIndexByTag(this Dictionary<string, int> tagCounts, string tag)
    {
        return tagCounts[tag] = (tagCounts.GetValueOrDefault(tag, -1) + 1);
    }
}
