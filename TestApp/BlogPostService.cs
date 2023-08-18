using Generated;

namespace TestApp.Services;

public class BlogPostService : BlogPost.IBackendService
{
    private readonly BlogPost.Repository repo;

    public BlogPostService(BlogPost.Repository repo)
    {
        this.repo = repo;
    }

    public BlogPost.WithComments Get(int bpID)
    {
        return repo.BlogPost_GetWithComments(bpID);
    }

    public List<BlogPost> GetByUser(int suID)
    {
        return repo.BlogPost_GetByUserID(suID);
    }

    public BlogPost MakePost(string bpContent)
    {
        return repo.BlogPost_Create(1, bpContent);
    }
}