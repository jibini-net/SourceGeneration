using Asp.Versioning;
using Generated;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.OpenApi.Models;
using System.IO.Compression;
using TestApp.Extensions;
using TestApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

builder.Services.AddResponseCompression((options) =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.SmallestSize;
});

builder.Services.AddControllersWithViews((config) =>
{
    config.ValueProviderFactories.Insert(0, new JsonBodyValueProviderFactory());
});
builder.Services.Configure<ApiBehaviorOptions>((config) =>
{
    config.SuppressInferBindingSourcesForParameters = true;
    config.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddSwaggerGen((options) =>
{
    static string generateNestedName(Type type)
    {
        if (type.DeclaringType is null)
        {
            return type.Name;
        } else
        {
            var parentName = generateNestedName(type.DeclaringType);
            return $"{parentName}.{type.Name}";
        }
    }
    options.CustomSchemaIds(generateNestedName);

    options.AddSecurityDefinition("Bearer", new()
    {
        In = ParameterLocation.Header,
        Description = "Access token (refresh token is in cookie)",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    options.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    options.SwaggerDoc("v1",
        new()
        {
            Title = "TestApp API",
            Version = "v1"
        });

    options.SwaggerDoc("v2",
        new()
        {
            Title = "TestApp API",
            Version = "v2"
        });

    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "TestApp.Web.xml"));
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "TestApp.Generated.xml"));
});

builder.Services
    .AddApiVersioning((options) =>
    {
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer((options) =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddScoped<IModelDbAdapter, ModelDbAdapter>();
builder.Services.AddScoped<IModelDbWrapper, ModelDbWrapper>();
builder.Services.AddScoped<ILinkPathGenerator, LinkPathGenerator>();
builder.Services.AddSingleton<IResourceInliner, ResourceInliner>();
builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddSingleton<IJwtAuthService, JwtAuthService>();

builder.Services.AddBackendServices();
builder.Services.AddViewServices();

var app = builder.Build();

app.UseSwagger((options) =>
{
    options.RouteTemplate = "api/docs/{documentName}/swagger.json";
});
app.UseSwaggerUI((options) =>
{
    var hosting = app.Services.GetService<IWebHostEnvironment>();
    options.RoutePrefix = "api/docs";
    options.SwaggerEndpoint("v1/swagger.json", "TestApp API v1");
    options.SwaggerEndpoint("v2/swagger.json", "TestApp API v2");
    options.EnableTryItOutByDefault();
});

app.UseHttpsRedirection();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
} else
{
    //TODO Server error pages
    //app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseResponseCompression();
}
app.MapControllers();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.Run();
