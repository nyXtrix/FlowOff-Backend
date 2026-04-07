using System.Threading.Tasks;

namespace LMS.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toAddress, string subject, string body);
    Task SendForgotPasswordEmailAsync(string toAddress, string userName, string resetLink);
    Task SendInviteEmailAsync(string toAddress, string userName, string inviteLink);
    Task SendWelcomeEmailAsync(string toAddress, string adminName, string companyName, string loginLink);
}
