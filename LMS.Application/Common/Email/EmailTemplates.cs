namespace LMS.Application.Common.Email;

public static class EmailTemplates
{
    private const string PrimaryColor = "#4F46E5"; 
    private const string SecondaryColor = "#6366F1"; 
    private const string DarkBg = "#0F172A"; 

    public static string GetLeadInquirySetupTemplate(string name, string setupLink)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <style>
                body {{ font-family: 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 0; }}
                .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06); }}
                .header {{ background: linear-gradient(135deg, {PrimaryColor} 0%, {SecondaryColor} 100%); padding: 40px 20px; text-align: center; color: white; }}
                .header h1 {{ margin: 0; font-size: 28px; font-weight: 700; letter-spacing: -0.025em; }}
                .content {{ padding: 40px 30px; line-height: 1.6; color: #334155; }}
                .content h2 {{ color: #1e293b; font-size: 20px; margin-top: 0; }}
                .btn-container {{ text-align: center; margin: 35px 0; }}
                .btn {{ background-color: {PrimaryColor}; color: white !important; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; display: inline-block; transition: background-color 0.2s; }}
                .footer {{ background-color: #f1f5f9; padding: 20px; text-align: center; color: #64748b; font-size: 13px; }}
                .highlight {{ color: {PrimaryColor}; font-weight: 600; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>Leave Management System</h1>
                </div>
                <div class='content'>
                    <h2>Welcome to the next generation of Leave Management, {name}!</h2>
                    <p>Thank you for choosing our <span class='highlight'>Leave Management System</span>. Your request for a personal instance has been approved.</p>
                    <p>Click the button below to complete your registration and start managing your team's absences.</p>
                    
                    <div class='btn-container'>
                        <a href='{setupLink}' class='btn'>Launch My Instance</a>
                    </div>
                    
                    <p>If the button doesn't work, copy and paste this link into your browser:</p>
                    <p style='word-break: break-all; font-size: 12px; color: #94a3b8;'>{setupLink}</p>
                </div>
                <div class='footer'>
                    &copy; {System.DateTime.Now.Year} Leave Management System. All rights reserved.<br>
                    You are receiving this because you requested a setup link for your organization.
                </div>
            </div>
        </body>
        </html>";
    }

    public static string GetWelcomeEmailTemplate(string name, string companyName, string loginLink)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <style>
                body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 0; }}
                .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1); }}
                .header {{ background: linear-gradient(135deg, #059669 0%, #10b981 100%); padding: 60px 20px; text-align: center; color: white; }}
                .header h1 {{ margin: 0; font-size: 32px; font-weight: 800; }}
                .content {{ padding: 40px 30px; line-height: 1.6; color: #334155; }}
                .btn-container {{ text-align: center; margin: 35px 0; }}
                .btn {{ background-color: #059669; color: white !important; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; display: inline-block; }}
                .footer {{ background-color: #f1f5f9; padding: 20px; text-align: center; color: #64748b; font-size: 13px; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1>Welcome Aboard! 🚀</h1>
                </div>
                <div class='content'>
                    <h2>Hi {name},</h2>
                    <p>Congratulations! Your organization <strong style='color: #059669;'>{companyName}</strong> is now live on our Leave Management System.</p>
                    <p>Your administrator account is active and ready. Start by logging in to configure your workspace and invite your team.</p>
                    
                    <div class='btn-container'>
                        <a href='{loginLink}' class='btn'>Go to Dashboard</a>
                    </div>
                </div>
                <div class='footer'>
                    &copy; {System.DateTime.Now.Year} Leave Management System.
                </div>
            </div>
        </body>
        </html>";
    }

    public static string GetInviteEmailTemplate(string name, string inviteLink)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <style>
                body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f8fafc; margin: 0; }}
                .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 12px; overflow: hidden; border: 1px solid #e2e8f0; }}
                .header {{ background-color: #f8fafc; padding: 30px; border-bottom: 1px solid #e2e8f0; text-align: center; }}
                .content {{ padding: 40px 30px; line-height: 1.6; color: #475569; }}
                .btn-container {{ text-align: center; margin: 30px 0; }}
                .btn {{ background-color: #1e293b; color: white !important; padding: 12px 28px; border-radius: 6px; text-decoration: none; font-weight: 600; display: inline-block; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <div style='font-size: 20px; font-weight: 700; color: #1e293b;'>Invitation Request</div>
                </div>
                <div class='content'>
                    <p>Hello {name},</p>
                    <p>You have been invited to join your team's Leave Management System.</p>
                    <p>To accept this invitation and set up your account password, please click the button below:</p>
                    <div class='btn-container'>
                        <a href='{inviteLink}' class='btn'>Setup My Account</a>
                    </div>
                    <p style='font-size: 14px; color: #94a3b8;'>This link will expire in 24 hours.</p>
                </div>
            </div>
        </body>
        </html>";
    }

    public static string GetForgotPasswordEmailTemplate(string name, string resetLink)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <style>
                body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #fef2f2; margin: 0; }}
                .container {{ max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(220, 38, 38, 0.1); }}
                .header {{ background-color: #dc2626; padding: 30px; text-align: center; color: white; }}
                .content {{ padding: 40px 30px; line-height: 1.6; color: #334155; }}
                .btn-container {{ text-align: center; margin: 30px 0; }}
                .btn {{ background-color: #dc2626; color: white !important; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; display: inline-block; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1 style='margin: 0; font-size: 24px;'>Password Reset Request</h1>
                </div>
                <div class='content'>
                    <p>Hi {name},</p>
                    <p>We received a request to reset your password for your account.</p>
                    <p>If you made this request, click the button below to choose a new password:</p>
                    <div class='btn-container'>
                        <a href='{resetLink}' class='btn'>Reset Password</a>
                    </div>
                    <p>If you didn't request this, you can safely ignore this email.</p>
                </div>
            </div>
        </body>
        </html>";
    }
}
