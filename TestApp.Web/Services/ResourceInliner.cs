using System.Collections.Concurrent;

namespace TestApp.Services;

public class ResourceInliner : IResourceInliner
{

    public class CachedResource
    {
        public static TimeSpan LIFE = TimeSpan.FromHours(1);
        public string Content {  get; set; }
        public DateTime Loaded { get; set; } = DateTime.Now;
    }

    private ConcurrentDictionary<string, CachedResource> loadedInternal = new();
    private ConcurrentDictionary<string, CachedResource> loadedExternal = new();

    private async Task<string> LoadInternal(string path)
    {
        var fullPath = Path.Combine("wwwroot", path);
        var content = await File.ReadAllTextAsync(fullPath);
        loadedInternal[path] = new()
        {
            Content = content
        };
        return content;
    }

    private async Task<string> LoadExternal(string path)
    {
        using var client = new HttpClient();
        var content = await client.GetStringAsync(path);
        loadedExternal[path] = new()
        {
            Content = content
        };
        return content;
    }

    private async Task<string> GetOrLoad(string path,
        IDictionary<string, CachedResource> cache,
        Func<string, Task<string>> loader)
    {
        var cached = cache.TryGetValue(path, out var content);
        if (!cached || (DateTime.Now - content.Loaded) > CachedResource.LIFE)
        {
            try
            {
                return await loader(path);
            } catch (Exception)
            {
                if (cached)
                {
                    // Perhaps the file is temporarily unavailable;
                    // use the stale cached version
                    goto useCache;
                }
                throw;
            }
        }

    useCache:
        return content.Content;
    }

    public async Task<string> PreLoad(string path, bool external = false)
    {
        return await GetOrLoad(path,
            external ? loadedExternal : loadedInternal,
            external ? LoadExternal : LoadInternal);
    }
}
