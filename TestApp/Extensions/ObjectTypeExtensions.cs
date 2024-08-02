namespace TestApp.Extensions;

using System.Text.Json;

public static class ObjectTypeExtensions
{
    public static string ToJson(this object o)
    {
        return JsonSerializer.Serialize(o);
    }

    public static T FromJson<T>(this string json, JsonSerializerOptions options = null)
    {
        options ??= new();
        options.PropertyNameCaseInsensitive = true;
        return JsonSerializer.Deserialize<T>(json ?? "null", options);
    }

    public static object FromJson(this string json, Type t, JsonSerializerOptions options = null)
    {
        options ??= new();
        options.PropertyNameCaseInsensitive = true;
        return JsonSerializer.Deserialize(json ?? "null", t, options);
    }

    public static T ConvertTo<T>(this object o)
    {
        return o.ToJson().FromJson<T>();
    }
}
