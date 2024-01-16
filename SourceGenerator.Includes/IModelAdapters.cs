namespace Generated;

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
