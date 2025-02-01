using Generated;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using TestApp.Extensions;

namespace TestApp.Services;

public class ModelDbAdapter(
    IConfiguration config
    ) : IModelDbAdapter
{
    private async Task ExecuteUsingResultsAsync(string procName, object args, Action<SqlDataReader> consume, string connName = "DefaultConnection")
    {
        using var conn = new SqlConnection(config.GetConnectionString(connName));
        conn.Open();
        using var proc = conn.CreateCommand();
        proc.CommandText = procName;
        proc.CommandType = CommandType.StoredProcedure;

        foreach (var prop in args.GetType().GetProperties())
        {
            var propValue = prop.GetValue(args);
            proc.Parameters.AddWithValue(prop.Name, propValue ?? DBNull.Value);
        }
        
        consume(await proc.ExecuteReaderAsync());
    }

    public async Task ExecuteAsync(string procName, object args)
    {
        await ExecuteUsingResultsAsync(procName, args, (_) =>
        {
        });
    }

    private static object BuildRow(Type type, SqlDataReader reader)
    {
        var instance = type
            .GetConstructor([])
            .Invoke([]);
        foreach (var value in reader.GetColumnSchema())
        {
            var prop = type.GetProperty(value.ColumnName);
            if (prop is not null)
            {
                var propValue = reader[value.ColumnName];
                if (propValue == DBNull.Value)
                {
                    propValue = null;
                }
                prop.SetValue(instance, propValue);
            }
        }
        return instance;
    }

    private static bool IsSingle(Type type, out Type modelType)
    {
        modelType = type.GetGenericArguments().FirstOrDefault();
        var listType = typeof(List<>);
        if (modelType is not null)
        {
            listType = listType.MakeGenericType(modelType);
        }

        if (type.IsAssignableFrom(listType))
        {
            return false;
        } else
        {
            modelType = type;
            return true;
        }
    }

    public async Task<T> ExecuteAsync<T>(string procName, object args)
    {
        T _return = default;
        await ExecuteUsingResultsAsync(procName, args, (reader) =>
        {
            if (IsSingle(typeof(T), out var modelType))
            {
                _return = reader.Read()
                    ? (T)BuildRow(typeof(T), reader)
                    : default;
            } else
            {
                var listType = typeof(List<>).MakeGenericType(modelType);
                _return = (T)listType
                    .GetConstructor([])
                    .Invoke([]);

                while (reader.Read())
                {
                    var add = listType.GetMethod("Add", [modelType]);
                    add.Invoke(_return, [BuildRow(modelType, reader)]);
                }
            }
        });
        return _return;
    }

    public async Task<T> ExecuteForJsonAsync<T>(string procName, object args)
    {
        T _return = default;
        await ExecuteUsingResultsAsync(procName, args, (reader) =>
        {
            var jsonBuilder = new StringBuilder();
            while (reader.Read())
            {
                var json = reader[0];
                if (json == DBNull.Value)
                {
                    json = null;
                }
                jsonBuilder.Append(json ?? "");
            }
            if (jsonBuilder.Length == 0)
            {
                jsonBuilder.Append("[]");
            }

            if (IsSingle(typeof(T), out var modelType))
            {
                var listType = typeof(List<>).MakeGenericType(modelType);
                var list = jsonBuilder.ToString().FromJson(listType);

                var firstOrDefault = typeof(Enumerable)
                    .GetMethods()
                    .Where((it) => it.Name == "FirstOrDefault"
                        && it.GetGenericArguments().Length == 1
                        && it.GetParameters().Length == 1)
                    .Single()
                    .MakeGenericMethod(modelType);
                _return = (T)firstOrDefault.Invoke(null, [list]);
            } else
            {
                _return = jsonBuilder.ToString().FromJson<T>();
            }
        });
        return _return;
    }
}
