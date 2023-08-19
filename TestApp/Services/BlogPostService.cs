using Generated;

namespace TestApp.Services;

public class BlogPostService : BlogPost.IBackendService
{
    private readonly BlogPost.Repository repo;

    public BlogPostService(BlogPost.Repository repo)
    {
        this.repo = repo;
    }

    public async Task<BlogPost.WithComments> Get(int bpID)
    {
        return await repo.BlogPost_GetWithComments(bpID);
    }

    public async Task<List<BlogPost>> GetByUser(int suID)
    {
        return await repo.BlogPost_GetByUserID(suID);
    }

    public async Task<BlogPost> MakePost(string bpContent)
    {
        return await repo.BlogPost_Create(1, bpContent);
    }
}