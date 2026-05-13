using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using LMS.Application.Common.Email;
using LMS.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Text;

namespace LMS.Infrastructure.Services.Email;

public class GmailApiService : IEmailService
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _refreshToken;
    private readonly string _fromAddress;
    private readonly string _fromName;
    private readonly ILogger<GmailApiService> _logger;

    public GmailApiService(IConfiguration configuration, ILogger<GmailApiService> logger)
    {
        _clientId = configuration["Storage:GoogleClientId"] ?? throw new InvalidOperationException("Google Client ID is not configured.");
        _clientSecret = configuration["Storage:GoogleClientSecret"] ?? throw new InvalidOperationException("Google Client Secret is not configured.");
        _refreshToken = configuration["Storage:GoogleRefreshToken"] ?? throw new InvalidOperationException("Google Refresh Token is not configured.");
        _fromAddress = configuration["Smtp:FromAddress"] ?? throw new InvalidOperationException("From address is not configured.");
        _fromName = configuration["Smtp:FromName"] ?? "Leave Management System";
        _logger = logger;
    }

    public async Task SendEmailAsync(string toAddress, string subject, string body)
    {
        try
        {
            _logger.LogInformation("Attempting to send email to {ToAddress} via Gmail API (HTTP)", toAddress);

            var credential = new UserCredential(
                new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = _clientId,
                        ClientSecret = _clientSecret
                    }
                }),
                "user",
                new TokenResponse { RefreshToken = _refreshToken }
            );

            var service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Leave Management System"
            });

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_fromName, _fromAddress));
            mimeMessage.To.Add(MailboxAddress.Parse(toAddress));
            mimeMessage.Subject = subject;
            mimeMessage.Body = new TextPart("html") { Text = body };

            using var memoryStream = new MemoryStream();
            await mimeMessage.WriteToAsync(memoryStream);
            var rawMessage = Convert.ToBase64String(memoryStream.ToArray())
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");

            var message = new Message { Raw = rawMessage };
            await service.Users.Messages.Send(message, "me").ExecuteAsync();

            _logger.LogInformation("Email sent successfully to {ToAddress} via Gmail API", toAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[GMAIL_API_ERROR] Failed to send email to {ToAddress}. Error: {Message}", toAddress, ex.Message);
            throw;
        }
    }

    public Task SendForgotPasswordEmailAsync(string toAddress, string userName, string resetLink)
        => SendEmailAsync(toAddress, "Reset Your Password", EmailTemplates.GetForgotPasswordEmailTemplate(userName, resetLink));

    public Task SendInviteEmailAsync(string toAddress, string userName, string inviteLink)
        => SendEmailAsync(toAddress, "You're Invited!", EmailTemplates.GetInviteEmailTemplate(userName, inviteLink));

    public Task SendWelcomeEmailAsync(string toAddress, string adminName, string companyName, string loginLink)
        => SendEmailAsync(toAddress, $"Welcome to {companyName}!", EmailTemplates.GetWelcomeEmailTemplate(adminName, companyName, loginLink));
}
