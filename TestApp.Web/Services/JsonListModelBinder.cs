namespace TestApp.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;
using System.Text.Json.Serialization;
using TestApp.Extensions;

public class JsonListModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        await Task.CompletedTask;

        var type = bindingContext.ModelType.GenericTypeArguments.FirstOrDefault();
        if (type is null)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return;
        }

        // Interprets plain form submission arrays, where each array member is
        // submitted with the same field name to indicate multiple values
        //
        // May encounter ambiguity between identical nested and parent names
        if (!bindingContext.HttpContext.Request.HasJsonContentType())
        {
            if (!bindingContext.HttpContext.Request.Query
                .TryGetValue(bindingContext.ModelName, out var values))
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return;
            }

            var listType = typeof(JsonList<>).MakeGenericType(type);
            var add = listType.GetMethod("Add");
            var list = Activator.CreateInstance(listType);

            for (var i = 0; i < values.Count; i++)
            {
                if (type == typeof(string))
                {
                    add.Invoke(list, [values[i]]);
                    continue;
                }

                var item = values[i].FromJson(type);
                add.Invoke(list, [item]);
            }

            bindingContext.Result = ModelBindingResult.Success(list);
            return;
        }

        var json = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;
        var converterType = typeof(JsonListJsonConverter<>).MakeGenericType(type);
        // Case is still insensitive due to extension method later
        var options = new JsonSerializerOptions();
        options.Converters.Clear();
        options.Converters.Add(Activator.CreateInstance(converterType) as JsonConverter);

        var parsed = json.FromJson(bindingContext.ModelType, options);
        bindingContext.Result = ModelBindingResult.Success(parsed);
    }
}

public class JsonListJsonConverter<T> : JsonConverter<JsonList<T>>
{
    public override JsonList<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = JsonSerializer.Deserialize<List<T>>(ref reader, options);
        return new JsonList<T>(list);
    }

    public override void Write(Utf8JsonWriter writer, JsonList<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

[ModelBinder(BinderType = typeof(JsonListModelBinder))]
public class JsonList<T> : List<T>
{
    public JsonList() : base()
    {
    }

    public JsonList(List<T> items) : base(items)
    {
    }
}
