namespace TestApp.Services;

public interface IResourceInliner
{
    Task<string> PreLoad(string path, bool external = false);
}
