namespace IDP.Infrastructure.Emails.Primitives;

internal static class EmailTemplates
{
    public const string MfaCodeHtml = @"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='UTF-8'>
        <title>Your Verification Code</title>
    </head>
    <body style='font-family: Arial, sans-serif; line-height: 1.6; margin: 0; padding: 20px; color: #333;'>
        <div style='max-width: 600px; margin: 0 auto;'>
            <h2 style='color: #2563eb; margin-bottom: 16px;'>Your Verification Code</h2>
            <p>Hi <%NAME%>,</p>
            <p>Use the following code to verify your identity:</p>
            <div style='font-size: 24px; font-weight: bold; color: #2563eb; margin: 20px 0; padding: 10px 0; letter-spacing: 2px;'>
                <%MFA_CODE%>
            </div>
            <p>This code expires in <strong style='color: #dc2626;'>10 minutes</strong>. Do not share it with anyone.</p>
            <div style='margin-top: 30px; font-size: 12px; color: #6b7280;'>
                <p>If you didn't request this, please ignore this email.</p>
                <p>© {YEAR} TokenIDP. All rights reserved.</p>
            </div>
        </div>
    </body>
    </html>";

    public const string MfaCodeSubject = "Your verification code";

    public const string PasswordResetHtml = @"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='UTF-8'>
        <title>Password Reset</title>
    </head>
    <body style='font-family: Arial, sans-serif; line-height: 1.6; margin: 0; padding: 20px; color: #333;'>
        <div style='max-width: 600px; margin: 0 auto;'>
            <h2 style='color: #2563eb; margin-bottom: 16px;'>Reset Your Password</h2>
            <p>A password reset request was received for <strong><%TENANT_NAME%></strong>.</p>
            <p>Use the link below to reset your password:</p>
            <p style='margin: 24px 0;'>
                <a href='<%RESET_LINK%>' style='background: #2563eb; color: #fff; text-decoration: none; padding: 10px 16px; border-radius: 6px; display: inline-block;'>
                    Reset Password
                </a>
            </p>
            <p>This link expires in <strong><%EXPIRY_MINUTES%> minutes</strong>.</p>
            <div style='margin-top: 30px; font-size: 12px; color: #6b7280;'>
                <p>If you didn't request this, please ignore this email.</p>
                <p>© {YEAR} TokenIDP. All rights reserved.</p>
            </div>
        </div>
    </body>
    </html>";

    public const string PasswordResetSubject = "Reset your password";

    public const string EmailConfirmationHtml = @"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='UTF-8'>
        <title>Confirm Your Email</title>
    </head>
    <body style='font-family: Arial, sans-serif; line-height: 1.6; margin: 0; padding: 20px; color: #333;'>
        <div style='max-width: 600px; margin: 0 auto;'>
            <h2 style='color: #2563eb; margin-bottom: 16px;'>Confirm Your Email Address</h2>
            <p>Welcome to <strong><%TENANT_NAME%></strong>.</p>
            <p>Please confirm your email address before signing in to your account.</p>
            <p style='margin: 24px 0;'>
                <a href='<%CONFIRM_LINK%>' style='background: #2563eb; color: #fff; text-decoration: none; padding: 10px 16px; border-radius: 6px; display: inline-block;'>
                    Confirm Email
                </a>
            </p>
            <p>This confirmation link expires in <strong><%EXPIRY_HOURS%> hours</strong>.</p>
            <div style='margin-top: 30px; font-size: 12px; color: #6b7280;'>
                <p>If you didn't create this account, please ignore this email.</p>
                <p>© {YEAR} TokenIDP. All rights reserved.</p>
            </div>
        </div>
    </body>
    </html>";

    public const string EmailConfirmationSubject = "Confirm your email";
}