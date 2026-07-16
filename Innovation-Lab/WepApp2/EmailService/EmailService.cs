// EmailServices/EmailService.cs
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace WepApp2.EmailService
{
    public interface IEmailService
    {
        Task SendPasswordResetEmail(string toEmail, string firstName, string resetLink);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendPasswordResetEmail(string toEmail, string firstName, string resetLink)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.EnableSsl = true;
                    client.Credentials = new NetworkCredential(username, password);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = "استعادة كلمة المرور - ادارة تقنيات معمل الابتكارات",
                        IsBodyHtml = true,
                        Body = GenerateEmailBody(firstName, resetLink)
                    };

                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("فشل إرسال البريد الإلكتروني", ex);
            }
        }

        private string GenerateEmailBody(string firstName, string resetLink)
        {
            return $@"
                <div style='direction: rtl; font-family: Tajawal, Arial, sans-serif; padding: 20px; background-color: #f5f5f5;'>
                    <div style='max-width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; padding: 30px; box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>
                        <div style='text-align: center; margin-bottom: 30px;'>
                            <h2 style='color: #2c3e50;'>اداة تقنيات معمل الابتكارات</h2>
                        </div>
                        
                        <h3 style='color: #34495e;'>مرحباً {firstName}</h3>
                        
                        <p style='color: #555; line-height: 1.8;'>
                            لقد طلبت استعادة كلمة المرور الخاصة بك. اضغط على الزر أدناه لإعادة تعيين كلمة المرور:
                        </p>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetLink}' style='display: inline-block; background-color: #0ea54a; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-size: 16px;'>
                                إعادة تعيين كلمة المرور
                            </a>
                        </div>
                        
                        <p style='color: #7f8c8d; text-align: center; font-size: 14px;'>
                            هذا الرابط صالح لمدة ساعة واحدة فقط
                        </p>
                        
                        <hr style='border: none; border-top: 1px solid #ecf0f1; margin: 30px 0;'>
                        
                        <p style='color: #95a5a6; font-size: 12px; text-align: center;'>
                            إذا لم تطلب إعادة تعيين كلمة المرور، يرجى تجاهل هذا البريد الإلكتروني.
                        </p>
                        
                        <p style='color: #95a5a6; font-size: 12px; text-align: center; margin-top: 20px;'>
                            أو انسخ والصق هذا الرابط في متصفحك:<br>
                            <span style='color: #3498db; word-break: break-all;'>{resetLink}</span>
                        </p>
                    </div>
 <!-- ذيل البريد -->
            <div style='background-color: #f8f9fa; padding: 30px 25px; text-align: center; border-top: 1px solid #e9ecef;'>
                <div style='font-size: 16px; color: #13b35e; margin-bottom: 15px; font-weight: 600;'>
                     شكرًا لاستخدامك نظام إدارة تقنيات معمل الابتكارات
                </div>
                <div style='font-size: 14px; color: #6c757d; line-height: 1.6;'>
                    إذا كنت تواجه أي مشكلة أو تحتاج إلى مساعدة، يرجى التواصل معنا
                    <br>
                    <strong>البريد الإلكتروني:</strong> support@innovations-lab.com
                    <br>
                    <strong>الهاتف:</strong> +966 11 123 4567
                </div>
            </div>
        </div>
                </div>
            ";
        }
    }
}