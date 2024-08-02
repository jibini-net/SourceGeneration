namespace TestApp.Services;

using MailKit.Net.Smtp;
using MimeKit;

public class EmailSender : IEmailSender
{
    private IConfiguration config;
    public EmailSender(IConfiguration config)
    {
        this.config = config;
    }

    public async Task SendEmailAsync(string address, string subject, string html)
    {
        var server = config["Smtp:Server"].ToString();
        var port = config.GetValue("Smtp:Port", 25);
        var useSsl = config.GetValue("Smtp:UseSsl", false);
        var from = config["Smtp:From"].ToString();
        var username = config["Smtp:Username"]?.ToString();
        var password = config["Smtp:Password"]?.ToString();

        using var client = new SmtpClient();
        await client.ConnectAsync(server, port, useSsl);
        if (!string.IsNullOrEmpty(username))
        {
            await client.AuthenticateAsync(username, password);
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(address));
        message.Subject = subject;

        var body = new BodyBuilder();
        body.HtmlBody = html;
        message.Body = body.ToMessageBody();

        await client.SendAsync(message);
    }
}
