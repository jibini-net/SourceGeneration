namespace TestApp.Services;

public interface ILinkPathGenerator
{
    string GenerateActionPath(params string[] pieces);

    string GetNamed(string name);
}
