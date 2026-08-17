using System.Net;
using System.Text;
using AIVS.Data;
using AIVS.Models.Attributes;
using AIVS.Models.Configuration;
using AIVS.Models.ViewModels.UserManagement;
using AIVS.Models.ViewModels.ValuerInbox;
using AIVS.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AIVS.Services.Implementations;

public sealed class ProcessorFileService : IProcessorFileService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".xlsx", ".xls", ".csv", ".png", ".jpg", ".jpeg"
    };

    private const long MaxFileSize = 20L * 1024L * 1024L;
    private readonly AttributesDbContext _context;
    private readonly AttributeStorageSettings _storage;

    public ProcessorFileService(AttributesDbContext context, IOptions<AttributeStorageSettings> storage)
    {
        _context = context;
        _storage = storage.Value;
    }

    public async Task<List<ProcessorEvidenceFileVm>> GetEvidenceAsync(long attrId)
    {
        return await _context.AttrProcessorEvidence
            .AsNoTracking()
            .Where(x => x.Attr_ID == attrId && x.IsActive)
            .OrderByDescending(x => x.UploadedAt)
            .Select(x => new ProcessorEvidenceFileVm
            {
                Id = x.Id,
                FileName = x.FileName,
                ContentType = x.ContentType,
                FileSizeBytes = x.FileSizeBytes,
                EvidenceComment = x.EvidenceComment,
                EvidenceStage = x.EvidenceStage,
                UploadedByName = x.UploadedByName,
                UploadedByRole = x.UploadedByRole,
                UploadedAt = x.UploadedAt
            })
            .ToListAsync();
    }

    public async Task UploadEvidenceAsync(long attrId, IEnumerable<IFormFile> files, string? comment, string stage, AivsCurrentUserVm currentUser)
    {
        if (currentUser.UserId == null)
            throw new InvalidOperationException("Your AIVS user could not be verified.");

        var item = await _context.AttrPropertyInfo
            .FirstOrDefaultAsync(x => x.Attr_ID == attrId && x.IsActive == true)
            ?? throw new InvalidOperationException("Attribute submission could not be found.");

        var uploadFiles = files?.Where(x => x != null && x.Length > 0).ToList() ?? new();
        if (uploadFiles.Count == 0)
            throw new InvalidOperationException("Please select at least one processor evidence file.");
        if (uploadFiles.Count > 10)
            throw new InvalidOperationException("A maximum of 10 evidence files can be uploaded at a time.");

        var folder = GetProcessorEvidenceFolder(item.Attr_No ?? $"ATTR-{attrId}");
        Directory.CreateDirectory(folder);

        foreach (var file in uploadFiles)
        {
            var originalName = Path.GetFileName(file.FileName);
            var ext = Path.GetExtension(originalName);

            if (string.IsNullOrWhiteSpace(originalName) || !AllowedExtensions.Contains(ext))
                throw new InvalidOperationException($"{originalName}: only PDF, Excel, CSV, PNG and JPG evidence files are allowed.");
            if (file.Length > MaxFileSize)
                throw new InvalidOperationException($"{originalName}: file exceeds the 20 MB limit.");

            var fullPath = Path.Combine(folder, originalName);
            if (File.Exists(fullPath))
                throw new InvalidOperationException($"{originalName}: a processor evidence file with the same original filename already exists.");

            await using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await file.CopyToAsync(stream);

            _context.AttrProcessorEvidence.Add(new AttrProcessorEvidence
            {
                Attr_ID = item.Attr_ID,
                Attr_No = item.Attr_No,
                FileName = originalName,
                FilePath = fullPath,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? GetContentType(originalName) : file.ContentType,
                FileSizeBytes = file.Length,
                EvidenceComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
                EvidenceStage = string.IsNullOrWhiteSpace(stage) ? "ProcessorReview" : stage.Trim(),
                UploadedByUserId = currentUser.UserId,
                UploadedByName = currentUser.FullName,
                UploadedByRole = currentUser.Role,
                UploadedAt = DateTime.Now,
                IsActive = true
            });
        }

        _context.AttrPropertyInfoAuditTrail.Add(new AttrPropertyInfoAuditTrail
        {
            Attr_ID = item.Attr_ID,
            Attr_No = item.Attr_No,
            Action = "Processor Evidence Uploaded",
            OldStatus = item.Attr_Status,
            NewStatus = item.Attr_Status,
            ActionByUserId = currentUser.UserId.Value.ToString(),
            ActionByName = currentUser.FullName,
            ActionRole = currentUser.Role,
            Comment = $"{uploadFiles.Count} processor evidence file(s) uploaded at {stage}." +
                      (string.IsNullOrWhiteSpace(comment) ? string.Empty : $" Comment: {comment.Trim()}"),
            ActionDateTime = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }

    public async Task<(string Path, string FileName, string ContentType)?> GetEvidenceFileAsync(long evidenceId)
    {
        var row = await _context.AttrProcessorEvidence.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == evidenceId && x.IsActive);
        if (row == null) return null;

        var path = row.FilePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            var attrNo = string.IsNullOrWhiteSpace(row.Attr_No) ? $"ATTR-{row.Attr_ID}" : row.Attr_No;
            var currentPath = Path.Combine(GetProcessorEvidenceFolder(attrNo), row.FileName);
            if (File.Exists(currentPath))
            {
                path = currentPath;
            }
            else
            {
                // Backward compatibility with evidence uploaded before the folder was renamed to Evidence.
                var legacyPath = Path.Combine(_storage.PhysicalRootPath, attrNo, _storage.ProcessorFolderName, "Processor Evidence", row.FileName);
                if (!File.Exists(legacyPath)) return null;
                path = legacyPath;
            }
        }

        return (path, row.FileName, string.IsNullOrWhiteSpace(row.ContentType) ? GetContentType(row.FileName) : row.ContentType);
    }

    public async Task<string?> SaveClientEmailCopyAsync(
        string attrNo,
        string emailType,
        string fromEmail,
        string fromName,
        string originalToEmail,
        string actualToEmail,
        string? ccEmails,
        string? bccEmails,
        string subject,
        string htmlBody,
        bool isTestMode,
        byte[]? calendarInviteBytes = null,
        string? calendarInviteFileName = null,
        byte[]? attachmentBytes = null,
        string? attachmentFileName = null,
        string? attachmentContentType = null)
    {
        if (string.IsNullOrWhiteSpace(attrNo)) return null;

        var folder = GetClientEmailFolder(attrNo);
        Directory.CreateDirectory(folder);

        var safeType = SafeFilePart(emailType);
        var fileName = $"{DateTime.Now:yyyyMMdd_HHmmssfff}_{safeType}.eml";
        var path = Path.Combine(folder, fileName);

        var boundary = "----AIVS-" + Guid.NewGuid().ToString("N");
        var hasAttachments = (calendarInviteBytes?.Length ?? 0) > 0 || (attachmentBytes?.Length ?? 0) > 0;
        var sb = new StringBuilder();
        sb.AppendLine($"From: {EncodeHeader(fromName)} <{fromEmail}>");
        sb.AppendLine($"To: {actualToEmail}");
        if (!string.IsNullOrWhiteSpace(ccEmails)) sb.AppendLine($"Cc: {ccEmails}");
        if (!string.IsNullOrWhiteSpace(bccEmails)) sb.AppendLine($"Bcc: {bccEmails}");
        sb.AppendLine($"Subject: {EncodeHeader(subject)}");
        sb.AppendLine($"Date: {DateTimeOffset.Now:R}");
        sb.AppendLine("MIME-Version: 1.0");
        sb.AppendLine($"X-AIVS-Attribute: {attrNo}");
        sb.AppendLine($"X-AIVS-Email-Type: {emailType}");
        sb.AppendLine($"X-AIVS-Original-To: {originalToEmail}");
        sb.AppendLine($"X-AIVS-Test-Mode: {isTestMode}");

        if (!hasAttachments)
        {
            sb.AppendLine("Content-Type: text/html; charset=utf-8");
            sb.AppendLine("Content-Transfer-Encoding: base64");
            sb.AppendLine();
            sb.AppendLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(htmlBody), Base64FormattingOptions.InsertLineBreaks));
        }
        else
        {
            sb.AppendLine($"Content-Type: multipart/mixed; boundary=\"{boundary}\"");
            sb.AppendLine();
            AppendHtmlPart(sb, boundary, htmlBody);
            if (calendarInviteBytes is { Length: > 0 })
                AppendAttachment(sb, boundary, calendarInviteBytes, calendarInviteFileName ?? "inspection-appointment.ics", "text/calendar");
            if (attachmentBytes is { Length: > 0 })
                AppendAttachment(sb, boundary, attachmentBytes, attachmentFileName ?? "attachment", attachmentContentType ?? "application/octet-stream");
            sb.AppendLine($"--{boundary}--");
        }

        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    private string GetProcessorEvidenceFolder(string attrNo) =>
        Path.Combine(_storage.PhysicalRootPath, attrNo, _storage.ProcessorFolderName,
            string.IsNullOrWhiteSpace(_storage.ProcessorEvidenceFolderName) ? "Evidence" : _storage.ProcessorEvidenceFolderName);
    private string GetClientEmailFolder(string attrNo) => Path.Combine(_storage.PhysicalRootPath, attrNo, _storage.ProcessorFolderName, _storage.ProcessorEmailFolderName);

    private static void AppendHtmlPart(StringBuilder sb, string boundary, string html)
    {
        sb.AppendLine($"--{boundary}");
        sb.AppendLine("Content-Type: text/html; charset=utf-8");
        sb.AppendLine("Content-Transfer-Encoding: base64");
        sb.AppendLine();
        sb.AppendLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(html), Base64FormattingOptions.InsertLineBreaks));
    }

    private static void AppendAttachment(StringBuilder sb, string boundary, byte[] bytes, string fileName, string contentType)
    {
        sb.AppendLine($"--{boundary}");
        sb.AppendLine($"Content-Type: {contentType}; name=\"{fileName.Replace("\"", "") }\"");
        sb.AppendLine("Content-Transfer-Encoding: base64");
        sb.AppendLine($"Content-Disposition: attachment; filename=\"{fileName.Replace("\"", "") }\"");
        sb.AppendLine();
        sb.AppendLine(Convert.ToBase64String(bytes, Base64FormattingOptions.InsertLineBreaks));
    }

    private static string SafeFilePart(string? value)
    {
        var source = string.IsNullOrWhiteSpace(value) ? "client-email" : value;
        return string.Concat(source.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) || char.IsWhiteSpace(ch) ? '-' : ch));
    }
    private static string EncodeHeader(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Replace("\r", " ").Replace("\n", " ");
    private static string GetContentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".xls" => "application/vnd.ms-excel",
        ".csv" => "text/csv",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "application/octet-stream"
    };
}
