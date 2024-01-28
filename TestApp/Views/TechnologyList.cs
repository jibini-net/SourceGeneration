using Generated;

namespace TestApp.Views;

public class TechnologyList : TechnologyListBase
{
    public TechnologyList(IServiceProvider sp) : base(sp)
    {
        technologyList = new()
        {
            new()
            {
                Name = "ASP.NET Core (MVC/Blazor)",
                IconClass = "fa fa-globe",
                Description = "Interactive UI with server- and client-side code; production application template and framework design"
            },
            new()
            {
                Name = "ASP.NET Core (APIs)",
                IconClass = "fa fa-server",
                Description = "Secure and intuitive API design to allow 3rd party integration, with <a target='_blank' href='https://t.jibini.net/api/docs'>generated documentation</a>"
            },
            new()
            {
                Name = "Microsoft SQL Server",
                IconClass = "fa fa-database",
                Description = "Integrated database features, including stored procedures and JSON queries"
            },
            new()
            {
                Name = "Source Generation",
                IconClass = "oi oi-project",
                Description = "Custom <a target='_blank' href='https://github.com/jibini-net/SourceGeneration'>descriptor languages and datalayer generators</a> for accelerated development"
            },
            new()
            {
                Name = "Spring Boot",
                IconClass = "fa fa-leaf",
                Description = "Java and Kotlin backends using an O/RM, used in combination with React.js"
            },
            new()
            {
                Name = "React.js and Node.js",
                IconClass = "fab fa-js",
                Description = "JS and TypeScript frontends for pure JS stacks, or other APIs in .NET Core/Java"
            },
            new()
            {
                Name = "C and Low-level Code",
                IconClass = "fa fa-microchip",
                Description = "OS kernel and embedded development with a focus on memory safety and portability"
            },
            new()
            {
                Name = "State Machines and Parsers",
                IconClass = "fa fa-indent",
                Description = "Custom built language parsing frameworks with <a target='_blank' href='https://apps.jibini.net/Fsa'>custom lexer</a>/parser generators"
            },
            new()
            {
                Name = "OpenGL Graphics",
                IconClass = "fa fa-gamepad",
                Description = "Desktop and embedded graphics for <a target='_blank' href='https://github.com/jibini-net/CheckEngine'>small games</a> and <a target='_blank' href='https://github.com/jibini-net/Lemon'>detailed rendering</a> with shader pipelines"
            },
            new()
            {
                Name = "Windows Server",
                IconClass = "fab fa-microsoft",
                Description = "Deployment to on-premises servers and IaaS (Azure, AWS, HostWinds, and more)"
            },
            new()
            {
                Name = "Linux and UNIX-likes",
                IconClass = "fab fa-freebsd",
                Description = "System and cluster administration with heavily virtualized workloads, ZFS filesystem"
            },
            new()
            {
                Name = "Git and SCM",
                IconClass = "fab fa-git",
                Description = "Promoting general code cleanliness and trackability with point-in-time restore"
            }
        };
    }
}
