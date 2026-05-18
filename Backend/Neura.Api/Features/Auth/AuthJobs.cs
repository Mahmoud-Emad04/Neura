using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Neura.Core.Entities;
using Neura.Services.Helpers;

namespace Neura.Api.Features.Auth;

public class AuthJobs(IEmailSender emailSender)
{
    public async Task SendConfirmationEmail(string email, string firstName, string userId, string code)
    {
        var emailBody = EmailBodyBuilder.GenerateEmailBody("EmailConfirmation",
            new Dictionary<string, string>
            {
                { "{{name}}", firstName },
                { "{{action_url}}", $"https://neuralearning.netlify.app/auth/verify-email?userId={userId}&code={code}" }
            }
        );
        await emailSender.SendEmailAsync(email, "✅ Neura: Email Confirmation", emailBody);
    }

    public async Task SendResetPasswordEmail(string email, string firstName, string code)
    {
        var emailBody = EmailBodyBuilder.GenerateEmailBody("ForgetPassword",
            new Dictionary<string, string>
            {
                { "{{name}}", firstName },
                { "{{action_url}}", $"https://neuralearning.netlify.app/auth/reset-password?email={email}&code={code}" }
            }
        );
        await emailSender.SendEmailAsync(email, "✅ Neura: Change Password", emailBody);
    }
}
