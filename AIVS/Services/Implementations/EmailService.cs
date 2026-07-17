using AIVS.Data;
using AIVS.Models.Attributes;
using AIVS.Models.Configuration;
using AIVS.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace AIVS.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;
        private readonly AttributesDbContext _context;
        public EmailService(
            IOptions<EmailSettings> settings,
            ILogger<EmailService> logger,
            AttributesDbContext context )
        {
            _settings = settings.Value;
            _logger = logger;
            _context = context;
        }

        public async Task SendSelfAssignmentEmailAsync(
            string toEmail,
            string fullName,
            string sector,
            int assignedCount,
            List<string> references)
        {
            var subject = $"AIVS - Attribute submissions assigned to your inbox - Sector {sector}";

            var body = BuildSelfAssignmentBody(
                fullName,
                sector,
                assignedCount,
                references);

            await SendHtmlEmailAsync(
                toEmail,
                subject,
                body,
                "self-assignment");
        }

        public async Task SendManagerAssignmentEmailAsync(
            string toEmail,
            string valuerFullName,
            string managerFullName,
            string sector,
            int assignedCount,
            List<string> references)
        {
            var subject = $"AIVS - Attribute submissions assigned to you - Sector {sector}";

            var body = BuildManagerAssignmentBody(
                valuerFullName,
                managerFullName,
                sector,
                assignedCount,
                references);

            await SendHtmlEmailAsync(
                toEmail,
                subject,
                body,
                "manager-assignment");
        }

        public async Task SendInspectionDateOptionsEmailAsync(
            string toEmail,
            string? clientName,
            string attrNo,
            string? propertyDescription,
            List<DateTime> proposedDates,
            string? requestComment)
        {
            var subject = ApplySubjectTemplate(
                _settings.Templates.InspectionDateOptionsSubject,
                "Property Attribute Inspection Date Selection - {AttrNo}",
                attrNo);

            var body = BuildInspectionDateOptionsBody(
                clientName,
                attrNo,
                propertyDescription,
                proposedDates,
                requestComment);

            await SendHtmlEmailAsync(
                toEmail,
                subject,
                body,
                "inspection-date-options", attrNo);
        }

        public async Task SendInspectionDetailsEmailAsync(
            string toEmail,
            string? clientName,
            string attrNo,
            string? propertyDescription,
            DateTime confirmedDateTime,
            string inspectionPin,
            string valuerName,
            string? valuerEmail,
            string? valuerCell,
            string? vehicleRegistration,
            string? vehicleMake,
            string? vehicleColour,
            string? photoFileName)
        {
            var subject = ApplySubjectTemplate(
                _settings.Templates.InspectionConfirmedSubject,
                "Confirmed Property Attribute Inspection - {AttrNo}",
                attrNo);

            var body = BuildInspectionDetailsBody(
                clientName,
                attrNo,
                propertyDescription,
                confirmedDateTime,
                inspectionPin,
                valuerName,
                valuerEmail,
                valuerCell,
                vehicleRegistration,
                vehicleMake,
                vehicleColour,
                photoFileName);

            var calendarInviteBytes = BuildInspectionCalendarInvite(
    toEmail,
    clientName,
    attrNo,
    propertyDescription,
    confirmedDateTime);

            await SendHtmlEmailAsync(
                toEmail,
                subject,
                body,
                "inspection-details",
                attrNo,
                null,
                calendarInviteBytes,
                $"COJ-Inspection-{SafeFileName(attrNo)}.ics");
        }

        private byte[] BuildInspectionCalendarInvite(
    string toEmail,
    string? clientName,
    string attrNo,
    string? propertyDescription,
    DateTime confirmedDateTime)
        {
            var start = confirmedDateTime;
            var end = confirmedDateTime.AddHours(1);

            var uid = $"aivs-inspection-{attrNo}-{confirmedDateTime:yyyyMMddHHmmss}@joburg.org.za";

            var title = $"COJ Property Attribute Inspection - {attrNo}";

            var description =
                "Your City of Johannesburg property attribute inspection has been confirmed.\\n\\n" +
                "For security, valuer details are not included in this calendar invite.\\n" +
                "Please log in to City Of Johannesburg Valuation Portal and enter your inspection PIN to view the authorised valuer and vehicle details.\\n\\n" +
                "Do not share your inspection PIN before the valuer arrives at the property.";

            var location = string.IsNullOrWhiteSpace(propertyDescription)
                ? "Property inspection location"
                : propertyDescription.Trim();

            var organizerEmail = string.IsNullOrWhiteSpace(_settings.FromEmail)
                ? "PropertyInfo@joburg.org.za"
                : _settings.FromEmail.Trim();

            var organizerName = string.IsNullOrWhiteSpace(_settings.FromName)
                ? "City of Johannesburg Valuation Services"
                : _settings.FromName.Trim();

            var attendeeName = string.IsNullOrWhiteSpace(clientName)
                ? "Client"
                : clientName.Trim();

            var ics = $@"BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//City of Johannesburg//AIVS//EN
CALSCALE:GREGORIAN
METHOD:REQUEST
BEGIN:VEVENT
UID:{EscapeIcs(uid)}
DTSTAMP:{DateTime.UtcNow:yyyyMMddTHHmmssZ}
DTSTART:{start.ToUniversalTime():yyyyMMddTHHmmssZ}
DTEND:{end.ToUniversalTime():yyyyMMddTHHmmssZ}
SUMMARY:{EscapeIcs(title)}
DESCRIPTION:{EscapeIcs(description)}
LOCATION:{EscapeIcs(location)}
ORGANIZER;CN={EscapeIcs(organizerName)}:MAILTO:{organizerEmail}
ATTENDEE;CN={EscapeIcs(attendeeName)};ROLE=REQ-PARTICIPANT;PARTSTAT=NEEDS-ACTION;RSVP=TRUE:MAILTO:{toEmail}
BEGIN:VALARM
TRIGGER:-PT24H
ACTION:DISPLAY
DESCRIPTION:Reminder: COJ property attribute inspection tomorrow.
END:VALARM
BEGIN:VALARM
TRIGGER:-PT1H
ACTION:DISPLAY
DESCRIPTION:Reminder: COJ property attribute inspection in one hour.
END:VALARM
END:VEVENT
END:VCALENDAR";

            return Encoding.UTF8.GetBytes(ics);
        }

        private static string EscapeIcs(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value
                .Replace(@"\", @"\\")
                .Replace(";", @"\;")
                .Replace(",", @"\,")
                .Replace("\r\n", @"\n")
                .Replace("\n", @"\n")
                .Trim();
        }

        private static string SafeFileName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "appointment";

            var invalid = Path.GetInvalidFileNameChars();

            var safe = new string(value
                .Where(ch => !invalid.Contains(ch))
                .ToArray());

            return string.IsNullOrWhiteSpace(safe)
                ? "appointment"
                : safe;
        }
        public async Task SendReturnedToClientEmailAsync(
            string toEmail,
            string? clientName,
            string attrNo,
            string? propertyDescription,
            string comment)
        {
            var subject = ApplySubjectTemplate(
                _settings.Templates.ReturnedToClientSubject,
                "Property Attribute Submission Returned for Update - {AttrNo}",
                attrNo);

            var body = BuildReturnedToClientBody(
                clientName,
                attrNo,
                propertyDescription,
                comment);

            await SendHtmlEmailAsync(
                toEmail,
                subject,
                body,
                "returned-to-client",
                attrNo);
        }

        public async Task SendRejectedEmailAsync(
            string toEmail,
            string? clientName,
            string attrNo,
            string? propertyDescription,
            string comment)
        {
            var subject = ApplySubjectTemplate(
                _settings.Templates.RejectedSubject,
                "Property Attribute Submission Outcome - {AttrNo}",
                attrNo);

            var body = BuildRejectedBody(
                clientName,
                attrNo,
                propertyDescription,
                comment);

            await SendHtmlEmailAsync(
                toEmail,
                subject,
                body,
                "rejected",
                attrNo);
        }

        public async Task SendAcceptedEmailAsync(
            string toEmail,
            string? clientName,
            string attrNo,
            string? propertyDescription,
            string comment)
        {
            var subject = ApplySubjectTemplate(
                _settings.Templates.AcceptedSubject,
                "Property Attribute Submission Accepted - {AttrNo}",
                attrNo);

            var body = BuildAcceptedBody(
                clientName,
                attrNo,
                propertyDescription,
                comment);

            await SendHtmlEmailAsync(
                toEmail,
                subject,
                body,
                "accepted",
                attrNo);
        }

        private async Task SendHtmlEmailAsync(
        string toEmail,
        string subject,
        string body,
        string emailType,
        string? attrNo = null,
        long? attrId = null,
        byte[]? calendarInviteBytes = null,
        string? calendarInviteFileName = null)
        {
            var originalToEmail = toEmail?.Trim();
            var actualToEmail = originalToEmail;

            if (!_settings.Enabled)
            {
                await SaveEmailLogAsync(new AivsEmailLog
                {
                    EmailType = emailType,
                    Attr_No = attrNo,
                    Attr_ID = attrId,
                    OriginalToEmail = originalToEmail,
                    ActualToEmail = actualToEmail,
                    Subject = subject,
                    BodyPreview = BuildBodyPreview(body),
                    IsTestMode = _settings.TestMode,
                    SendStatus = "Skipped",
                    ErrorMessage = "Email sending is disabled in EmailSettings.",
                    CreatedBy = "AIVS"
                });

                _logger.LogInformation(
                    "Email disabled. Skipped {EmailType} email to {Email}.",
                    emailType,
                    originalToEmail);

                return;
            }

            if (string.IsNullOrWhiteSpace(originalToEmail))
            {
                await SaveEmailLogAsync(new AivsEmailLog
                {
                    EmailType = emailType,
                    Attr_No = attrNo,
                    Attr_ID = attrId,
                    OriginalToEmail = originalToEmail,
                    ActualToEmail = actualToEmail,
                    Subject = subject,
                    BodyPreview = BuildBodyPreview(body),
                    IsTestMode = _settings.TestMode,
                    SendStatus = "Skipped",
                    ErrorMessage = "Recipient address is empty.",
                    CreatedBy = "AIVS"
                });

                _logger.LogWarning(
                    "Skipped {EmailType} email because recipient address is empty.",
                    emailType);

                return;
            }

            if (string.IsNullOrWhiteSpace(_settings.FromEmail))
            {
                await SaveEmailLogAsync(new AivsEmailLog
                {
                    EmailType = emailType,
                    Attr_No = attrNo,
                    Attr_ID = attrId,
                    OriginalToEmail = originalToEmail,
                    ActualToEmail = actualToEmail,
                    Subject = subject,
                    BodyPreview = BuildBodyPreview(body),
                    IsTestMode = _settings.TestMode,
                    SendStatus = "Skipped",
                    ErrorMessage = "FromEmail is not configured.",
                    CreatedBy = "AIVS"
                });

                _logger.LogWarning(
                    "Skipped {EmailType} email to {Email} because FromEmail is not configured.",
                    emailType,
                    originalToEmail);

                return;
            }

            if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
            {
                await SaveEmailLogAsync(new AivsEmailLog
                {
                    EmailType = emailType,
                    Attr_No = attrNo,
                    Attr_ID = attrId,
                    OriginalToEmail = originalToEmail,
                    ActualToEmail = actualToEmail,
                    Subject = subject,
                    BodyPreview = BuildBodyPreview(body),
                    IsTestMode = _settings.TestMode,
                    SendStatus = "Skipped",
                    ErrorMessage = "SmtpHost is not configured.",
                    CreatedBy = "AIVS"
                });

                _logger.LogWarning(
                    "Skipped {EmailType} email to {Email} because SmtpHost is not configured.",
                    emailType,
                    originalToEmail);

                return;
            }

            if (_settings.TestMode)
            {
                if (string.IsNullOrWhiteSpace(_settings.TestRecipient))
                {
                    await SaveEmailLogAsync(new AivsEmailLog
                    {
                        EmailType = emailType,
                        Attr_No = attrNo,
                        Attr_ID = attrId,
                        OriginalToEmail = originalToEmail,
                        ActualToEmail = null,
                        Subject = subject,
                        BodyPreview = BuildBodyPreview(body),
                        IsTestMode = true,
                        SendStatus = "Skipped",
                        ErrorMessage = "Email test mode is enabled but TestRecipient is empty.",
                        CreatedBy = "AIVS"
                    });

                    _logger.LogWarning(
                        "Email test mode is enabled but TestRecipient is empty. Skipped {EmailType} email intended for {Email}.",
                        emailType,
                        originalToEmail);

                    return;
                }

                actualToEmail = _settings.TestRecipient.Trim();

                body = $@"
<div style='background:#fff3cd;border:1px solid #ffeeba;padding:10px;margin-bottom:12px;font-weight:bold;color:#856404;'>
    TEST MODE: This email was originally intended for {WebUtility.HtmlEncode(originalToEmail)}.
</div>
{body}";
            }

            var ccEmails = !_settings.TestMode
                ? string.Join(";", _settings.DefaultCc.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
                : null;

            var bccEmails = !_settings.TestMode
                ? string.Join(";", _settings.DefaultBcc.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
                : null;

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(actualToEmail);

            if (calendarInviteBytes != null && calendarInviteBytes.Length > 0)
            {
                var fileName = string.IsNullOrWhiteSpace(calendarInviteFileName)
                    ? "inspection-appointment.ics"
                    : calendarInviteFileName;

                var calendarStream = new MemoryStream(calendarInviteBytes);

                var calendarAttachment = new Attachment(
                    calendarStream,
                    fileName,
                    "text/calendar");

                calendarAttachment.ContentType.Parameters.Add("method", "REQUEST");

                message.Attachments.Add(calendarAttachment);
            }
            if (!_settings.TestMode)
            {
                foreach (var cc in _settings.DefaultCc.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    message.CC.Add(cc.Trim());
                }

                foreach (var bcc in _settings.DefaultBcc.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    message.Bcc.Add(bcc.Trim());
                }
            }

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.UseSsl,
                UseDefaultCredentials = _settings.UseDefaultCredentials
            };

            if (!_settings.UseDefaultCredentials &&
                !string.IsNullOrWhiteSpace(_settings.Username))
            {
                client.Credentials = new NetworkCredential(
                    _settings.Username,
                    _settings.Password);
            }

            try
            {
                await client.SendMailAsync(message);

                await SaveEmailLogAsync(new AivsEmailLog
                {
                    EmailType = emailType,
                    Attr_No = attrNo,
                    Attr_ID = attrId,
                    OriginalToEmail = originalToEmail,
                    ActualToEmail = actualToEmail,
                    CcEmails = ccEmails,
                    BccEmails = bccEmails,
                    Subject = subject,
                    BodyPreview = BuildBodyPreview(body),
                    IsTestMode = _settings.TestMode,
                    SendStatus = "Sent",
                    CreatedBy = "AIVS",
                    SentDate = DateTime.Now
                });

                _logger.LogInformation(
                    "Sent {EmailType} email. Actual recipient: {ActualEmail}. Original recipient: {OriginalEmail}. TestMode: {TestMode}.",
                    emailType,
                    actualToEmail,
                    originalToEmail,
                    _settings.TestMode);
            }
            catch (Exception ex)
            {
                await SaveEmailLogAsync(new AivsEmailLog
                {
                    EmailType = emailType,
                    Attr_No = attrNo,
                    Attr_ID = attrId,
                    OriginalToEmail = originalToEmail,
                    ActualToEmail = actualToEmail,
                    CcEmails = ccEmails,
                    BccEmails = bccEmails,
                    Subject = subject,
                    BodyPreview = BuildBodyPreview(body),
                    IsTestMode = _settings.TestMode,
                    SendStatus = "Failed",
                    ErrorMessage = ex.InnerException == null
    ? ex.Message
    : $"{ex.Message} | Inner: {ex.InnerException.Message}",
                    CreatedBy = "AIVS"
                });

                _logger.LogError(
      ex,
      "Failed to send {EmailType} email. Actual recipient: {ActualEmail}. Original recipient: {OriginalEmail}. TestMode: {TestMode}. Error: {Error}",
      emailType,
      actualToEmail,
      originalToEmail,
      _settings.TestMode,
      ex.InnerException == null ? ex.Message : $"{ex.Message} | Inner: {ex.InnerException.Message}");
            }
        }
        private async Task SaveEmailLogAsync(AivsEmailLog log)
        {
            try
            {
                _context.AivsEmailLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to save AIVS email audit log for {EmailType}.",
                    log.EmailType);
            }
        }

        private static string BuildBodyPreview(string? htmlBody)
        {
            if (string.IsNullOrWhiteSpace(htmlBody))
                return string.Empty;

            var plainText = Regex.Replace(htmlBody, "<.*?>", " ");
            plainText = WebUtility.HtmlDecode(plainText);
            plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

            plainText = Regex.Replace(
                plainText,
                @"Inspection PIN:\s*[A-Z0-9]{6,12}",
                "Inspection PIN: ******",
                RegexOptions.IgnoreCase);

            plainText = Regex.Replace(
                plainText,
                @"PIN:\s*[A-Z0-9]{6,12}",
                "PIN: ******",
                RegexOptions.IgnoreCase);

            return plainText.Length > 1000
                ? plainText.Substring(0, 1000)
                : plainText;
        }
        private static string ApplySubjectTemplate(
            string? configuredTemplate,
            string fallbackTemplate,
            string attrNo)
        {
            var template = string.IsNullOrWhiteSpace(configuredTemplate)
                ? fallbackTemplate
                : configuredTemplate;

            return template.Replace("{AttrNo}", attrNo);
        }

        private static string BuildSelfAssignmentBody(
            string fullName,
            string sector,
            int assignedCount,
            List<string> references)
        {
            var refsHtml = BuildReferenceList(references);

            return WrapBody($@"
<p>Hi {H(fullName)},</p>

<p>
    Please note that you have assigned submitted attribute properties to your inbox
    for <strong>Sector {H(sector)}</strong>.
</p>

<p><strong>Total assigned:</strong> {assignedCount}</p>

<p><strong>Reference numbers:</strong></p>

<ul>
    {refsHtml}
</ul>

<p>You can now continue with the review from your Valuer Inbox.</p>");
        }

        private static string BuildManagerAssignmentBody(
            string valuerFullName,
            string managerFullName,
            string sector,
            int assignedCount,
            List<string> references)
        {
            var refsHtml = BuildReferenceList(references);

            return WrapBody($@"
<p>Hi {H(valuerFullName)},</p>

<p>
    Please note that <strong>{H(managerFullName)}</strong>
    has assigned submitted attribute properties to your Valuer Inbox
    for <strong>Sector {H(sector)}</strong>.
</p>

<p><strong>Total assigned:</strong> {assignedCount}</p>

<p><strong>Reference numbers:</strong></p>

<ul>
    {refsHtml}
</ul>

<p>Please continue with the review from your Valuer Inbox.</p>");
        }

        private static string BuildInspectionDateOptionsBody(
            string? clientName,
            string attrNo,
            string? propertyDescription,
            List<DateTime> proposedDates,
            string? requestComment)
        {
            var options = new StringBuilder();

            for (var i = 0; i < proposedDates.Count; i++)
            {
                options.Append($@"
<tr>
    <td style='padding:8px;border:1px solid #ddd;font-weight:700;'>Option {i + 1}</td>
    <td style='padding:8px;border:1px solid #ddd;'>{proposedDates[i]:yyyy-MM-dd HH:mm}</td>
</tr>");
            }

            var commentHtml = string.IsNullOrWhiteSpace(requestComment)
                ? ""
                : $@"
<p>
    <strong>Valuer Comment:</strong><br/>
    {H(requestComment)}
</p>";

            return WrapBody($@"
<p>Dear {H(NameOrClient(clientName))},</p>

<p>
    A physical inspection is required for your property attribute submission.
    Please log in to City Of Johannesburg Valuation Portal and select one of the proposed inspection dates.
</p>

<table style='border-collapse:collapse;width:100%;margin:12px 0;'>
    <tr>
        <td style='padding:8px;border:1px solid #ddd;font-weight:700;background:#f7f7f7;'>Reference Number</td>
        <td style='padding:8px;border:1px solid #ddd;'>{H(attrNo)}</td>
    </tr>
    <tr>
        <td style='padding:8px;border:1px solid #ddd;font-weight:700;background:#f7f7f7;'>Property</td>
        <td style='padding:8px;border:1px solid #ddd;'>{H(propertyDescription)}</td>
    </tr>
</table>

<p><strong>Proposed inspection dates:</strong></p>

<table style='border-collapse:collapse;width:100%;margin:12px 0;'>
    {options}
</table>

{commentHtml}

<p>
    Please respond from the City Of Johannesburg Valuation Portal under
    <strong>My Appointments with Valuer</strong>.
</p>");
        }

        private static string BuildInspectionDetailsBody(
    string? clientName,
    string attrNo,
    string? propertyDescription,
    DateTime confirmedDateTime,
    string inspectionPin,
    string valuerName,
    string? valuerEmail,
    string? valuerCell,
    string? vehicleRegistration,
    string? vehicleMake,
    string? vehicleColour,
    string? photoFileName)
        {
            return WrapBody($@"
<p>Dear {H(NameOrClient(clientName))},</p>

<p>
    Your physical inspection appointment has been confirmed.
</p>

<table style='border-collapse:collapse;width:100%;margin:12px 0;'>
    <tr>
        <td style='padding:8px;border:1px solid #ddd;font-weight:700;background:#f7f7f7;'>Reference Number</td>
        <td style='padding:8px;border:1px solid #ddd;'>{H(attrNo)}</td>
    </tr>
    <tr>
        <td style='padding:8px;border:1px solid #ddd;font-weight:700;background:#f7f7f7;'>Property</td>
        <td style='padding:8px;border:1px solid #ddd;'>{H(propertyDescription)}</td>
    </tr>
    <tr>
        <td style='padding:8px;border:1px solid #ddd;font-weight:700;background:#f7f7f7;'>Inspection Date and Time</td>
        <td style='padding:8px;border:1px solid #ddd;'>{confirmedDateTime:yyyy-MM-dd HH:mm}</td>
    </tr>
</table>

<div style='background:#fff6d6;border:1px solid #ead17a;border-left:5px solid #e6b000;padding:12px;margin:14px 0;font-weight:700;'>
    For your safety, valuer details will only be shown inside City Of Johannesburg Valuation Portal after you enter the inspection PIN below.
    Do not share this PIN before the valuer arrives at the property.
</div>

<p style='font-size:18px;'>
    <strong>Inspection PIN:</strong>
    <span style='background:#111;color:#fff;padding:6px 10px;border-radius:5px;font-weight:900;letter-spacing:2px;'>
        {H(inspectionPin)}
    </span>
</p>

<p>
    Please log in to City Of Johannesburg Valuation Portal and open <strong>My Appointments with Valuer</strong>.
    Enter the inspection PIN to view the valuer and vehicle details.
</p>

<p>
    Only allow the inspection to proceed if the person who arrives matches the details displayed in City Of Johannesburg Valuation Portal
    after PIN verification.
</p>");
        }

        private static string BuildReturnedToClientBody(
            string? clientName,
            string attrNo,
            string? propertyDescription,
            string comment)
        {
            return WrapBody($@"
<p>Dear {H(NameOrClient(clientName))},</p>

<p>
    Your property attribute submission has been returned for correction.
</p>

<table style='border-collapse:collapse;width:100%;margin:12px 0;'>
    <tr>
        <td style='padding:8px;border:1px solid #ddd;font-weight:700;background:#f7f7f7;'>Reference Number</td>
        <td style='padding:8px;border:1px solid #ddd;'>{H(attrNo)}</td>
    </tr>
    <tr>
        <td style='padding:8px;border:1px solid #ddd;font-weight:700;background:#f7f7f7;'>Property</td>
        <td style='padding:8px;border:1px solid #ddd;'>{H(propertyDescription)}</td>
    </tr>
</table>

<p>
    <strong>Valuer Comment:</strong><br/>
    {H(comment)}
</p>

<p>
    Please log in to City Of Johannesburg Valuation Portal, update the required information, and resubmit.
</p>");
        }

        private static string BuildRejectedBody(
            string? clientName,
            string attrNo,
            string? propertyDescription,
            string comment)
        {
            return WrapBody($@"
<p>Dear {H(NameOrClient(clientName))},</p>

<p>
    Your property attribute submission has been reviewed and rejected.
</p>

<table style='border-collapse:collapse;width:100%;margin:12px 0;'>
    <tr>
        <td style='padding:8px;border:1px solid #ddd;font-weight:700;background:#f7f7f7;'>Reference Number</td>
        <td style='padding:8px;border:1px solid #ddd;'>{H(attrNo)}</td>
    </tr>
    <tr>
        <td style='padding:8px;border:1px solid #ddd;font-weight:700;background:#f7f7f7;'>Property</td>
        <td style='padding:8px;border:1px solid #ddd;'>{H(propertyDescription)}</td>
    </tr>
</table>

<p>
    <strong>Reason:</strong><br/>
    {H(comment)}
</p>");
        }

        private static string BuildAcceptedBody(
            string? clientName,
            string attrNo,
            string? propertyDescription,
            string comment)
        {
            return WrapBody($@"
<p>Dear {H(NameOrClient(clientName))},</p>

<p>
    Your property attribute submission has been accepted and will continue to the next processing stage.
</p>

<table style='border-collapse:collapse;width:100%;margin:12px 0;'>
    <tr>
        <td style='padding:8px;border:1px solid #ddd;font-weight:700;background:#f7f7f7;'>Reference Number</td>
        <td style='padding:8px;border:1px solid #ddd;'>{H(attrNo)}</td>
    </tr>
    <tr>
        <td style='padding:8px;border:1px solid #ddd;font-weight:700;background:#f7f7f7;'>Property</td>
        <td style='padding:8px;border:1px solid #ddd;'>{H(propertyDescription)}</td>
    </tr>
</table>

<p>
    <strong>Comment:</strong><br/>
    {H(comment)}
</p>");
        }

        private static string BuildReferenceList(List<string> references)
        {
            var refsHtml = new StringBuilder();

            foreach (var reference in references)
            {
                refsHtml.Append($"<li>{H(reference)}</li>");
            }

            return refsHtml.ToString();
        }

        private static string WrapBody(string innerHtml)
        {
            return $@"
<html>
<body style='font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#222;line-height:1.5;'>
    {innerHtml}

    <p style='margin-top:20px;'>
        Regards,<br/>
        AIVS - Attribute Inspection & Verification System
    </p>
</body>
</html>";
        }

        private static string NameOrClient(string? clientName)
        {
            return string.IsNullOrWhiteSpace(clientName)
                ? "Client"
                : clientName.Trim();
        }

        private static string H(string? value)
        {
            return WebUtility.HtmlEncode(
                string.IsNullOrWhiteSpace(value)
                    ? "-"
                    : value.Trim());
        }
    }
}