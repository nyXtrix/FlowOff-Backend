using LMS.Application.Common.Email;
using LMS.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace LMS.Infrastructure.Services.Email;

public class SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly string _smtpHost = configuration["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host is not configured.");
    private readonly int _smtpPort = int.Parse(configuration["Smtp:Port"] ?? "587");
    private readonly string _smtpUser = configuration["Smtp:Username"] ?? throw new InvalidOperationException("Smtp:Username is not configured.");
    private readonly string _smtpPass = configuration["Smtp:Password"] ?? throw new InvalidOperationException("Smtp:Password is not configured.");
    private readonly string _fromAddress = configuration["Smtp:FromAddress"] ?? throw new InvalidOperationException("Smtp:FromAddress is not configured.");
    private readonly string _fromName = configuration["Smtp:FromName"] ?? "Leave Management System";

    public async Task SendEmailAsync(string toAddress, string subject, string body)
    {
        logger.LogInformation("Attempting to send email to {ToAddress} via {Host}:{Port}", toAddress, _smtpHost, _smtpPort);

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_fromName, _fromAddress));
        email.To.Add(MailboxAddress.Parse(toAddress));
        email.Subject = subject;
        email.Body = new TextPart("html") { Text = body };

        using var smtp = new SmtpClient();
        smtp.Timeout = 20000; // Increased to 20s

        try
        {
            logger.LogDebug("Connecting to SMTP server...");
            await smtp.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.Auto);
            
            logger.LogDebug("Authenticating...");
            await smtp.AuthenticateAsync(_smtpUser, _smtpPass);
            
            logger.LogDebug("Sending message...");
            await smtp.SendAsync(email);
            
            await smtp.DisconnectAsync(true);
            logger.LogInformation("Email sent successfully to {ToAddress}", toAddress);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SMTP_CRITICAL_ERROR] Failed to send email to {ToAddress} via {Host}:{Port}. Error: {Message}", toAddress, _smtpHost, _smtpPort, ex.Message);
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

