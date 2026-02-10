using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MimeKit;
using Neura.Core.Settings;

namespace Neura.Services.Services;

public class EmailService(IOptions<MailSettings> mailSettings, ILogger<EmailService> logger) : IEmailSender
{
    private readonly ILogger<EmailService> _logger = logger;
    private readonly MailSettings _mailSettings = mailSettings.Value;

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var message = new MimeMessage
        {
            Subject = subject
        };

        message.From.Add(new MailboxAddress(_mailSettings.DisplayName ?? "Neura Support", _mailSettings.Mail));
        message.To.Add(MailboxAddress.Parse(email));

        var builder = new BodyBuilder
        {
            HtmlBody = htmlMessage
        };

        message.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();

        try
        {
            await smtp.ConnectAsync(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);

            await smtp.AuthenticateAsync(_mailSettings.Mail, _mailSettings.Password);

            await smtp.SendAsync(message);

            _logger.LogInformation("Email sent successfully to {email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {email}", email);
        }
        finally
        {
            await smtp.DisconnectAsync(true);
        }
    }
}