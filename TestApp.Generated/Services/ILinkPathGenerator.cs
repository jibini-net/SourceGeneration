namespace TestApp.Services;

public interface ILinkPathGenerator
{
    string GenerateActionPath(params string[] pieces);
}
