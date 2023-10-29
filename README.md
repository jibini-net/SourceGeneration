# SourceGeneration
Descriptor language for .NET model source code generation

Several branches exist as submodules of other projects. This allows custom language features if a project calls for them, or for translation to languages other than C#.

Included is a test Solution to demonstrate creating several individual pieces of boilerplate source:
 - database repository
 - DTO model
 - partial classes
 - backend service interfaces
 - frontend HTTP client

from a simple input syntax.

### Brief Example

An example descriptor `Model/BlogPost.model`
```c
schema {
    int bpID,
    int bpUserID,
    string bpContent = {""}
}

partial WithComments {
    List<string> user_comments = {new()}
}

repo {
    BlogPost_GetByID(int bpID)
        => BlogPost,

    BlogPost_GetByUserID(int suID)
        => List<BlogPost>,

    BlogPost_GetWithComments(int bpID)
        => json BlogPost.WithComments,

    BlogPost_Create(int bpSiteUserID, string bpContent)
        => BlogPost
}

service {
    MakePost(string bpContent)
        => BlogPost,

    GetByUser(int suID)
        => List<BlogPost.WithComments>
}
```

concisely describes a generated source

```csharp
/* DO NOT EDIT THIS FILE */
// DFA RESTORED IN 44.1527ms
// GENERATED FROM 'D:\...\Models\BlogPost.model' AT 2023-08-17 21:52:12
#nullable disable
namespace Generated;
public class BlogPost
{
    public int bpID { get; set; }
    public int bpUserID { get; set; }
    public string bpContent { get; set; }
        = "";
    public partial class WithComments : BlogPost
    {
        public List<string> user_comments { get; set; }
            = new();
    }
    public class Repository
    {
        private readonly IModelDbAdapter db;
        public Repository(IModelDbAdapter db)
        {
            this.db = db;
        }
        public BlogPost BlogPost_GetByID(int bpID)
        {
            return db.Execute<BlogPost>("BlogPost_GetByID", new
            {
                bpID
            });
        }
        public List<BlogPost> BlogPost_GetByUserID(int suID)
        {
            return db.Execute<List<BlogPost>>("BlogPost_GetByUserID", new
            {
                suID
            });
        }
        public BlogPost.WithComments BlogPost_GetWithComments(int bpID)
        {
            return db.ExecuteForJson<BlogPost.WithComments>("BlogPost_GetWithComments", new
            {
                bpID
            });
        }
        public BlogPost BlogPost_Create(int bpSiteUserID,string bpContent)
        {
            return db.Execute<BlogPost>("BlogPost_Create", new
            {
                bpSiteUserID,
                bpContent
            });
        }
    }
    public interface IService
    {
        BlogPost MakePost(string bpContent);
        List<BlogPost.WithComments> GetByUser(int suID);
    }
    public interface IBackendService : IService
    {
        // Implement and inject this interface as a separate service
    }
    public class DbService : IService
    {
        private readonly IModelDbWrapper wrapper;
        private readonly IBackendService impl;
        public DbService(IModelDbWrapper wrapper, IBackendService impl)
        {
            this.wrapper = wrapper;
            this.impl = impl;
        }
        public BlogPost MakePost(string bpContent)
        {
            return wrapper.Execute<BlogPost>(() => impl.MakePost(
                  bpContent
                  ));
        }
        public List<BlogPost.WithComments> GetByUser(int suID)
        {
            return wrapper.Execute<List<BlogPost.WithComments>>(() => impl.GetByUser(
                  suID
                  ));
        }
    }
    public class ApiService : IService
    {
        private readonly IModelApiAdapter api;
        public ApiService(IModelApiAdapter api)
        {
            this.api = api;
        }
        public BlogPost MakePost(string bpContent)
        {
            return api.Execute<BlogPost>("BlogPost/MakePost", new
            {
                bpContent
            });
        }
        public List<BlogPost.WithComments> GetByUser(int suID)
        {
            return api.Execute<List<BlogPost.WithComments>>("BlogPost/GetByUser", new
            {
                suID
            });
        }
    }
}
// GENERATED IN 29.7579ms
```
