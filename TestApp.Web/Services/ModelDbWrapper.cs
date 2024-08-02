namespace TestApp.Services;

using Generated;

public class ModelDbWrapper : IModelDbWrapper
{
    public async Task ExecuteAsync(Func<Task> impl)
    {
        //TODO
        await impl();
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> impl)
    {
        //TODO
        return await impl();
    }
}
