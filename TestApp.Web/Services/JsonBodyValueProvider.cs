using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json.Nodes;

namespace TestApp.Services;

public class JsonBodyValueProvider(
    JsonObject json
    ) : IValueProvider
{
    public bool ContainsPrefix(string prefix)
    {
        return true;
    }

    public ValueProviderResult GetValue(string key)
    {
        var split = key.Split(".");
        JsonNode pointer = json;

        foreach (var item in split)
        {
            if (pointer is not JsonObject
                || !pointer.AsObject().TryGetPropertyValue(item, out pointer))
            {
                // Requested node is not a valid leaf or doesn't exist
                return ValueProviderResult.None;
            }

            if (pointer is JsonArray)
            {
                // Cannot be fully handled by the default ASP.NET Core pipeline,
                // but emitting the full JSON should allow custom model binding
                var arrayJson = pointer.ToJsonString();
                return new(new(arrayJson));
            }
        }

        return new(new(pointer?.ToString()));
    }
}

public class JsonBodyValueProviderFactory : IValueProviderFactory
{
    public async Task CreateValueProviderAsync(ValueProviderFactoryContext context)
    {
        if (!context.ActionContext.HttpContext.Request.HasJsonContentType())
        {
            return;
        }

        context.ActionContext.HttpContext.Request.EnableBuffering();
        var json = await context.ActionContext.HttpContext.Request.ReadFromJsonAsync<JsonObject>();
        context.ActionContext.HttpContext.Request.Body.Position = 0;
        
        context.ValueProviders.Insert(0, new JsonBodyValueProvider(json));
    }
}
