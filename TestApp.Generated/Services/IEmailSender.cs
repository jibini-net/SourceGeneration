namespace TestApp.Services;

public interface IEmailSender
{
    Task SendEmailAsync(string address, string subject, string html);
}
