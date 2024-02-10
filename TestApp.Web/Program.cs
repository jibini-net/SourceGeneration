using Generated;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using TestApp.Extensions;
using TestApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

builder.Services.AddResponseCompression((config) =>
{
    config.EnableForHttps = true;
    config.Providers.Add<BrotliCompressionProvider>();
    config.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.SmallestSize;
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

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.UseResponseCompression();
app.MapControllers();
app.UseStaticFiles();

app.Run();
