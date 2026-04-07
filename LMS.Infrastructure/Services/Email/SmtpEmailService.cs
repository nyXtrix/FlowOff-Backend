using LMS.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace LMS.Infrastructure.Services.Email;

public class SmtpEmailService(IConfiguration configuration) : IEmailService
{
    private readonly string _smtpHost = configuration["Smtp:Host"] ?? throw new InvalidOperationException("Smtp:Host is not configured.");
    private readonly int _smtpPort = int.Parse(configuration["Smtp:Port"] ?? "587");
    private readonly string _smtpUser = configuration["Smtp:Username"] ?? throw new InvalidOperationException("Smtp:Username is not configured.");
    private readonly string _smtpPass = configuration["Smtp:Password"] ?? throw new InvalidOperationException("Smtp:Password is not configured.");
    private readonly string _fromAddress = configuration["Smtp:FromAddress"] ?? throw new InvalidOperationException("Smtp:FromAddress is not configured.");
    private readonly string _fromName = configuration["Smtp:FromName"] ?? "Leave Management System";

  
    public async Task SendEmailAsync(string toAddress, string subject, string body)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_fromName, _fromAddress));
        email.To.Add(MailboxAddress.Parse(toAddress));
        email.Subject = subject;
        email.Body = new TextPart("html") { Text = body };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_smtpUser, _smtpPass);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }

    public Task SendForgotPasswordEmailAsync(string toAddress, string userName, string resetLink)
        => SendEmailAsync(toAddress, "Reset Your Password", ForgotPasswordBody(userName, resetLink));

    public Task SendInviteEmailAsync(string toAddress, string userName, string inviteLink)
        => SendEmailAsync(toAddress, "You're Invited!", InviteUserBody(userName, inviteLink));

    public Task SendWelcomeEmailAsync(string toAddress, string adminName, string companyName, string loginLink)
        => SendEmailAsync(toAddress, $"Welcome to {companyName} &#127881;", WelcomeBody(adminName, companyName, loginLink));

    private static string ForgotPasswordBody(string userName, string resetLink) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;background:#f4f4f4;padding:30px;">
          <div style="max-width:520px;margin:auto;background:#fff;border-radius:8px;padding:32px;box-shadow:0 2px 8px rgba(0,0,0,0.08);">
            <h2 style="color:#2d2d2d;">Password Reset Request</h2>
            <p style="color:#555;">Hi <strong>{userName}</strong>,</p>
            <p style="color:#555;">We received a request to reset your password. Click the button below to set a new one. This link expires in <strong>24 hours</strong>.</p>
            <a href="{resetLink}" 
               style="display:inline-block;margin:20px 0;padding:12px 28px;background:#4f46e5;color:#fff;border-radius:6px;text-decoration:none;font-weight:bold;">
              Reset Password
            </a>
            <p style="color:#999;font-size:12px;">If you didn't request this, you can safely ignore this email. Your password won't change.</p>
          </div>
        </body>
        </html>
        """;

    private static string InviteUserBody(string userName, string inviteLink) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;background:#f4f4f4;padding:30px;">
          <div style="max-width:520px;margin:auto;background:#fff;border-radius:8px;padding:32px;box-shadow:0 2px 8px rgba(0,0,0,0.08);">
            <h2 style="color:#2d2d2d;">You're Invited! &#127881;</h2>
            <p style="color:#555;">Hi <strong>{userName}</strong>,</p>
            <p style="color:#555;">You've been invited to join the Leave Management System. Click the button below to set your password and activate your account.</p>
            <a href="{inviteLink}" 
               style="display:inline-block;margin:20px 0;padding:12px 28px;background:#059669;color:#fff;border-radius:6px;text-decoration:none;font-weight:bold;">
              Accept Invitation
            </a>
            <p style="color:#999;font-size:12px;">This invitation link will expire in 7 days.</p>
          </div>
        </body>
        </html>
        """;

    private static string WelcomeBody(string adminName, string companyName, string loginLink) => $"""
        <!DOCTYPE html>
        <html>
        <body style="font-family:Arial,sans-serif;background:#f4f4f4;padding:30px;">
          <div style="max-width:520px;margin:auto;background:#fff;border-radius:8px;padding:32px;box-shadow:0 2px 8px rgba(0,0,0,0.08);">
            <h2 style="color:#2d2d2d;">Welcome to {companyName} Leave Management System! &#128640;</h2>
            <p style="color:#555;">Hi <strong>{adminName}</strong>,</p>
            <p style="color:#555;">Your company <strong>{companyName}</strong> has been successfully registered on the Leave Management System. You're all set to start managing your team's leaves.</p>
            <a href="{loginLink}" 
               style="display:inline-block;margin:20px 0;padding:12px 28px;background:#4f46e5;color:#fff;border-radius:6px;text-decoration:none;font-weight:bold;">
              Go to Dashboard
            </a>
            <p style="color:#999;font-size:12px;">If you have any questions, feel free to reach out to our support team.</p>
          </div>
        </body>
        </html>
        """;
}
