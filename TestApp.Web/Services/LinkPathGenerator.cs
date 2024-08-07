﻿namespace TestApp.Services;

using System.Text;

public class LinkPathGenerator : ILinkPathGenerator
{
    private IConfiguration config;
    private IHttpContextAccessor httpContext;
    public LinkPathGenerator(IConfiguration config, IHttpContextAccessor httpContext)
    {
        this.config = config;
        this.httpContext = httpContext;
    }

    public string GenerateActionPath(params string[] pieces)
    {
        var path = new StringBuilder();
        var preferredPath = config.GetValue<string>("LinkPath:PathBase");
        var preferredScheme = config.GetValue<string>("LinkPath:Scheme");

        if (string.IsNullOrEmpty(preferredPath))
        {
            var req = httpContext.HttpContext.Request;
            path.Append(string.IsNullOrEmpty(preferredScheme)
                ? req.Scheme
                : preferredScheme);
            path.Append("://");
            path.Append(req.Host);
            if (!string.IsNullOrEmpty(req.PathBase.ToString().Trim('/')))
            {
                path.Append('/');
                path.Append(req.PathBase.ToString().Trim('/'));
            }
        } else
        {
            path.Append(preferredPath.TrimEnd('/'));
        }

        foreach (var p in pieces)
        {
            path.Append('/');
            path.Append(p.Trim('/'));
        }

        return path.ToString();
    }

    public string GetNamed(string name)
    {
        return config.GetValue<string>($"Links:{name}");
    }
}
