using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text.Encodings.Web;

namespace RentalService.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendNewViewAppointmentNotificationAsync(string hostEmail, string hostName, string roomName, DateTime appointmentTime, string detailsUrl)
        {
            var subject = "Yêu cầu đặt lịch hẹn xem phòng mới - Trọ Tốt";
            var body = $@"<html><body style='font-family:sans-serif;background:#f8fafc;margin:0;padding:0;'>
    <div style='background:linear-gradient(90deg,#1b6ec2 0%,#0077cc 100%);padding:24px 0;text-align:center;'>
        <span style='font-size:2rem;color:#fff;font-weight:bold;letter-spacing:1px;'>
            Trọ Tốt
        </span>
    </div>
    <div style='padding:32px 16px 16px 16px;max-width:480px;margin:0 auto;background:#fff;border-radius:0 0 12px 12px;box-shadow:0 2px 8px rgba(0,0,0,0.04);'>
        <h2 style='color:#1b6ec2;'>Yêu cầu đặt lịch hẹn xem phòng mới</h2>
        <p>Xin chào {System.Net.WebUtility.HtmlEncode(hostName)},</p>
        <p>Bạn có một yêu cầu đặt lịch hẹn xem phòng mới:</p>
        <ul>
            <li><strong>Phòng:</strong> {System.Net.WebUtility.HtmlEncode(roomName)}</li>
            <li><strong>Thời gian hẹn:</strong> {appointmentTime:dd/MM/yyyy HH:mm}</li>
        </ul>
        <p style='text-align:center;margin:32px 0;'>
            <a href='{HtmlEncoder.Default.Encode(detailsUrl)}' style='background:#007bff;color:#fff;padding:12px 28px;text-decoration:none;border-radius:5px;font-size:1.1rem;font-weight:bold;display:inline-block;'>Xem chi tiết</a>
        </p>
        <hr style='margin:32px 0 16px 0;border:none;border-top:1px solid #eee;'>
        <p style='font-size:12px;color:#888;text-align:center;'>Trọ Tốt Team</p>
    </div>
</body></html>";
            
            SendEmail(hostEmail, subject, body);
        }

        public async Task SendViewAppointmentStatusUpdateAsync(string customerEmail, string customerName, string roomName, string status, string detailsUrl, string? hostContactInfo = null)
        {
            var subject = $"Cập nhật trạng thái lịch hẹn xem phòng - Trọ Tốt";
            var statusText = status switch
            {
                "Confirmed" => "đã được xác nhận",
                "Cancelled" => "đã bị hủy",
                _ => "đã được cập nhật"
            };
            
            var contactSection = "";
            if (status == "Confirmed" && !string.IsNullOrEmpty(hostContactInfo))
            {
                contactSection = $@"
        <h4 style='color:#1b6ec2;'>Thông tin liên hệ chủ nhà:</h4>
        <div style='background:#f8f9fa;padding:16px;border-radius:8px;margin:16px 0;'>{System.Net.WebUtility.HtmlEncode(hostContactInfo)}</div>";
            }

            var body = $@"<html><body style='font-family:sans-serif;background:#f8fafc;margin:0;padding:0;'>
    <div style='background:linear-gradient(90deg,#1b6ec2 0%,#0077cc 100%);padding:24px 0;text-align:center;'>
        <span style='font-size:2rem;color:#fff;font-weight:bold;letter-spacing:1px;'>
            Trọ Tốt
        </span>
    </div>
    <div style='padding:32px 16px 16px 16px;max-width:480px;margin:0 auto;background:#fff;border-radius:0 0 12px 12px;box-shadow:0 2px 8px rgba(0,0,0,0.04);'>
        <h2 style='color:#1b6ec2;'>Cập nhật lịch hẹn xem phòng</h2>
        <p>Xin chào {System.Net.WebUtility.HtmlEncode(customerName)},</p>
        <p>Lịch hẹn xem phòng <strong>{System.Net.WebUtility.HtmlEncode(roomName)}</strong> của bạn {statusText}.</p>
        {contactSection}
        <p style='text-align:center;margin:32px 0;'>
            <a href='{HtmlEncoder.Default.Encode(detailsUrl)}' style='background:#007bff;color:#fff;padding:12px 28px;text-decoration:none;border-radius:5px;font-size:1.1rem;font-weight:bold;display:inline-block;'>Xem chi tiết</a>
        </p>
        <hr style='margin:32px 0 16px 0;border:none;border-top:1px solid #eee;'>
        <p style='font-size:12px;color:#888;text-align:center;'>Trọ Tốt Team</p>
    </div>
</body></html>";
            
            SendEmail(customerEmail, subject, body);
        }

        public async Task SendNewBookingRequestNotificationAsync(string hostEmail, string hostName, string roomName, string customerName, string detailsUrl)
        {
            var subject = "Yêu cầu đặt phòng mới - Trọ Tốt";
            var body = $@"<html><body style='font-family:sans-serif;background:#f8fafc;margin:0;padding:0;'>
    <div style='background:linear-gradient(90deg,#1b6ec2 0%,#0077cc 100%);padding:24px 0;text-align:center;'>
        <span style='font-size:2rem;color:#fff;font-weight:bold;letter-spacing:1px;'>
            Trọ Tốt
        </span>
    </div>
    <div style='padding:32px 16px 16px 16px;max-width:480px;margin:0 auto;background:#fff;border-radius:0 0 12px 12px;box-shadow:0 2px 8px rgba(0,0,0,0.04);'>
        <h2 style='color:#1b6ec2;'>Yêu cầu đặt phòng mới</h2>
        <p>Xin chào {System.Net.WebUtility.HtmlEncode(hostName)},</p>
        <p>Bạn có một yêu cầu đặt phòng mới:</p>
        <ul>
            <li><strong>Phòng:</strong> {System.Net.WebUtility.HtmlEncode(roomName)}</li>
            <li><strong>Khách hàng:</strong> {System.Net.WebUtility.HtmlEncode(customerName)}</li>
        </ul>
        <p style='text-align:center;margin:32px 0;'>
            <a href='{HtmlEncoder.Default.Encode(detailsUrl)}' style='background:#007bff;color:#fff;padding:12px 28px;text-decoration:none;border-radius:5px;font-size:1.1rem;font-weight:bold;display:inline-block;'>Xem chi tiết</a>
        </p>
        <hr style='margin:32px 0 16px 0;border:none;border-top:1px solid #eee;'>
        <p style='font-size:12px;color:#888;text-align:center;'>Trọ Tốt Team</p>
    </div>
</body></html>";
            
            SendEmail(hostEmail, subject, body);
        }

        public async Task SendBookingRequestStatusUpdateAsync(string customerEmail, string customerName, string roomName, string status, string detailsUrl, string? hostContactInfo = null)
        {
            var subject = $"Cập nhật yêu cầu đặt phòng - Trọ Tốt";
            var statusText = status switch
            {
                "Approved" => "đã được chấp nhận",
                "Rejected" => "đã bị từ chối",
                "Cancelled" => "đã bị hủy",
                _ => "đã được cập nhật"
            };
            
            var contactSection = "";
            if (status == "Approved" && !string.IsNullOrEmpty(hostContactInfo))
            {
                contactSection = $@"
        <h4 style='color:#1b6ec2;'>Thông tin liên hệ chủ nhà:</h4>
        <div style='background:#f8f9fa;padding:16px;border-radius:8px;margin:16px 0;'>{System.Net.WebUtility.HtmlEncode(hostContactInfo)}</div>";
            }

            var body = $@"<html><body style='font-family:sans-serif;background:#f8fafc;margin:0;padding:0;'>
    <div style='background:linear-gradient(90deg,#1b6ec2 0%,#0077cc 100%);padding:24px 0;text-align:center;'>
        <span style='font-size:2rem;color:#fff;font-weight:bold;letter-spacing:1px;'>
            Trọ Tốt
        </span>
    </div>
    <div style='padding:32px 16px 16px 16px;max-width:480px;margin:0 auto;background:#fff;border-radius:0 0 12px 12px;box-shadow:0 2px 8px rgba(0,0,0,0.04);'>
        <h2 style='color:#1b6ec2;'>Cập nhật yêu cầu đặt phòng</h2>
        <p>Xin chào {System.Net.WebUtility.HtmlEncode(customerName)},</p>
        <p>Yêu cầu đặt phòng <strong>{System.Net.WebUtility.HtmlEncode(roomName)}</strong> của bạn {statusText}.</p>
        {contactSection}
        <p style='text-align:center;margin:32px 0;'>
            <a href='{HtmlEncoder.Default.Encode(detailsUrl)}' style='background:#007bff;color:#fff;padding:12px 28px;text-decoration:none;border-radius:5px;font-size:1.1rem;font-weight:bold;display:inline-block;'>Xem chi tiết</a>
        </p>
        <hr style='margin:32px 0 16px 0;border:none;border-top:1px solid #eee;'>
        <p style='font-size:12px;color:#888;text-align:center;'>Trọ Tốt Team</p>
    </div>
</body></html>";
            
            SendEmail(customerEmail, subject, body);
        }

        private void SendEmail(string to, string subject, string html)
        {
            Task.Run(() =>
            {
                try
                {
                    var host = Environment.GetEnvironmentVariable("SMTP_HOST");
                    var portStr = Environment.GetEnvironmentVariable("SMTP_PORT");
                    if (string.IsNullOrEmpty(portStr))
                    {
                        throw new InvalidOperationException("SMTP_PORT environment variable is not set.");
                    }
                    var port = int.Parse(portStr);
                    var user = Environment.GetEnvironmentVariable("SMTP_USER");
                    var pass = Environment.GetEnvironmentVariable("SMTP_PASS");
                    var from = Environment.GetEnvironmentVariable("SMTP_FROM");

                    // log SMTP configuration
                    Console.WriteLine($"SMTP Host: {host}, Port: {port}, From: {from}");
                    if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(from))
                    {
                        throw new InvalidOperationException("SMTP configuration is not set properly.");
                    }

                    var client = new SmtpClient(host, port)
                    {
                        Credentials = new NetworkCredential(user, pass),
                        EnableSsl = true
                    };

                    var mail = new MailMessage(from, to, subject, html)
                    {
                        IsBodyHtml = true
                    };
                    mail.Priority = MailPriority.High;
                    mail.Headers.Add("X-Priority", "1");
                    mail.Headers.Add("X-MSMail-Priority", "High");
                    mail.Headers.Add("Importance", "high");
                    mail.Headers.Add("Priority", "urgent");
                    mail.Headers.Add("X-Message-Flag", "urgent");
                    
                    // Add text alternative for better compatibility
                    var textBody = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
                    var plainView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain");
                    var htmlView = System.Net.Mail.AlternateView.CreateAlternateViewFromString(html, null, "text/html");
                    mail.AlternateViews.Add(plainView);
                    mail.AlternateViews.Add(htmlView);
                    mail.Body = textBody; // fallback

                    client.Send(mail);
                    Console.WriteLine("Email sent successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to send email:");
                    Console.WriteLine(ex);
                    // Optionally log ex.StackTrace or log it somewhere else
                }
            });
        }
    }
}
