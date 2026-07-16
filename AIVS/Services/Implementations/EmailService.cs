using AIVS.Models.Configuration;
using AIVS.Services.Interface;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace AIVS.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> settings,
            ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendSelfAssignmentEmailAsync(
            string toEmail,
            string fullName,
            string sector,
            int assignedCount,
            List<string> references)
        {
            if (!_settings.Enabled)
                return;

            if (string.IsNullOrWhiteSpace(toEmail))
                return;

            var subject = $"AIVS - Attribute submissions assigned to your inbox - Sector {sector}";

            var body = BuildSelfAssignmentBody(fullName, sector, assignedCount, references);

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            foreach (var cc in _settings.DefaultCc.Where(x => !string.IsNullOrWhiteSpace(x)))
                message.CC.Add(cc);

            foreach (var bcc in _settings.DefaultBcc.Where(x => !string.IsNullOrWhiteSpace(x)))
                message.Bcc.Add(bcc);

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.UseSsl,
                UseDefaultCredentials = _settings.UseDefaultCredentials
            };

            if (!_settings.UseDefaultCredentials &&
                !string.IsNullOrWhiteSpace(_settings.Username))
            {
                client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
            }

            try
            {
                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send AIVS self-assignment email to {Email}", toEmail);
            }
        }

        private static string BuildSelfAssignmentBody(
            string fullName,
            string sector,
            int assignedCount,
            List<string> references)
        {
            var refsHtml = new StringBuilder();

            foreach (var reference in references)
            {
                refsHtml.Append($"<li>{WebUtility.HtmlEncode(reference)}</li>");
            }

            return $@"
<html>
<body style='font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#222;'>
    <p>Hi {WebUtility.HtmlEncode(fullName)},</p>

    <p>
        Please note that you have assigned submitted attribute properties to your inbox
        for <strong>Sector {WebUtility.HtmlEncode(sector)}</strong>.
    </p>

    <p>
        <strong>Total assigned:</strong> {assignedCount}
    </p>

    <p><strong>Reference numbers:</strong></p>

    <ul>
        {refsHtml}
    </ul>

    <p>
        You can now continue with the review from your Valuer Inbox.
    </p>

    <p>
        Regards,<br/>
        AIVS - Attribute Inspection & Verification System
    </p>
</body>
</html>";
        }
        public async Task SendManagerAssignmentEmailAsync(
    string toEmail,
    string valuerFullName,
    string managerFullName,
    string sector,
    int assignedCount,
    List<string> references)
        {
            if (!_settings.Enabled)
                return;

            if (string.IsNullOrWhiteSpace(toEmail))
                return;

            var subject = $"AIVS - Attribute submissions assigned to you - Sector {sector}";

            var body = BuildManagerAssignmentBody(
                valuerFullName,
                managerFullName,
                sector,
                assignedCount,
                references);

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            foreach (var cc in _settings.DefaultCc.Where(x => !string.IsNullOrWhiteSpace(x)))
                message.CC.Add(cc);

            foreach (var bcc in _settings.DefaultBcc.Where(x => !string.IsNullOrWhiteSpace(x)))
                message.Bcc.Add(bcc);

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.UseSsl,
                UseDefaultCredentials = _settings.UseDefaultCredentials
            };

            if (!_settings.UseDefaultCredentials &&
                !string.IsNullOrWhiteSpace(_settings.Username))
            {
                client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
            }

            try
            {
                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send AIVS manager assignment email to {Email}", toEmail);
            }
        }
        private static string BuildManagerAssignmentBody(
    string valuerFullName,
    string managerFullName,
    string sector,
    int assignedCount,
    List<string> references)
        {
            var refsHtml = new StringBuilder();

            foreach (var reference in references)
            {
                refsHtml.Append($"<li>{WebUtility.HtmlEncode(reference)}</li>");
            }

            return $@"
<html>
<body style='font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#222;'>
    <p>Hi {WebUtility.HtmlEncode(valuerFullName)},</p>

    <p>
        Please note that <strong>{WebUtility.HtmlEncode(managerFullName)}</strong>
        has assigned submitted attribute properties to your Valuer Inbox
        for <strong>Sector {WebUtility.HtmlEncode(sector)}</strong>.
    </p>

    <p>
        <strong>Total assigned:</strong> {assignedCount}
    </p>

    <p><strong>Reference numbers:</strong></p>

    <ul>
        {refsHtml}
    </ul>

    <p>
        Please continue with the review from your Valuer Inbox.
    </p>

    <p>
        Regards,<br/>
        AIVS - Attribute Inspection & Verification System
    </p>
</body>
</html>";
        }
    }
}