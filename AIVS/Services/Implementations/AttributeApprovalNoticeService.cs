using AIVS.Data;
using AIVS.Models.Attributes;
using AIVS.Models.Configuration;
using AIVS.Models.ViewModels.UserManagement;
using AIVS.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AIVS.Services.Implementations;

public class AttributeApprovalNoticeService : IAttributeApprovalNoticeService
{
    private readonly AttributesDbContext _context;
    private readonly AttributeStorageSettings _storage;

    public AttributeApprovalNoticeService(
        AttributesDbContext context,
        IOptions<AttributeStorageSettings> storage)
    {
        _context = context;
        _storage = storage.Value;
    }

    public async Task<string> GenerateAsync(
        AttrPropertyInfo item,
        string approvalComment,
        AivsCurrentUserVm approver)
    {
        var attrNo = item.Attr_No ?? $"ATTR-{item.Attr_ID}";
        var details = item.PropertyDetails ?? await _context.AttrPropertyDetails
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == item.Attr_PropertyDetailsId);

        var contact = details == null ? null : await _context.AttrContactInfo
            .AsNoTracking()
            .Where(x => x.PropertyDetailsId == details.Id)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();

        var clientName = contact == null
            ? item.SubmittedByName
            : contact.IsCompany && !string.IsNullOrWhiteSpace(contact.CompanyName)
                ? contact.CompanyName
                : string.Join(" ", new[] { contact.FirstNames, contact.LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

        var folder = Path.Combine(_storage.PhysicalRootPath, attrNo, "Approval Notice");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"{Safe(attrNo)}_Attribute_Approval_Notice_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        var approvedAt = DateTime.Now;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("CITY OF JOHANNESBURG").Bold().FontSize(16);
                    col.Item().Text("VALUATION SERVICES").Bold().FontSize(11);
                    col.Item().Text("ATTRIBUTE INSPECTION & VERIFICATION SYSTEM (AIVS)")
                        .FontSize(9).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(8).LineHorizontal(1);
                });

                page.Content().PaddingVertical(15).Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text("PROPERTY ATTRIBUTE SUBMISSION APPROVAL NOTICE")
                        .Bold().FontSize(14);

                    col.Item().Text($"Date: {approvedAt:dd MMMM yyyy}");
                    col.Item().Text($"Reference: {attrNo}").Bold();

                    col.Item().PaddingTop(8).Text($"Dear {(!string.IsNullOrWhiteSpace(clientName) ? clientName : "Client")},");

                    col.Item().Text(
                        "The City of Johannesburg has completed the review and quality assurance process for the property attribute submission referenced above. The submission has been approved for processing into the City's valuation attribute records.");

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2.4f);
                        });

                        AddRow(table, "Reference No.", attrNo);
                        AddRow(table, "Property Description", item.Property_Desc);
                        AddRow(table, "Township", details?.Township);
                        AddRow(table, "Premise ID", item.Premise_id);
                        AddRow(table, "Property Type", item.Property_Type);
                        AddRow(table, "Sector", item.RoutedSector ?? item.Sector);
                        AddRow(table, "Final QA Decision", "Approved");
                        AddRow(table, "Approved Date", approvedAt.ToString("dd MMMM yyyy HH:mm"));
                    });

                    col.Item().PaddingTop(5).Text("Final approval comment").Bold();
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8)
                        .Text(string.IsNullOrWhiteSpace(approvalComment) ? "Approved." : approvalComment);

                    col.Item().PaddingTop(5).Text(
                        "This approval confirms completion of the AIVS review workflow. The approved attribute information will be inserted into the OVVIO integration staging data for downstream processing.");

                    col.Item().PaddingTop(12).Text("Regards,").Bold();
                    col.Item().Text("City of Johannesburg Valuation Services");
                    col.Item().Text("valuationenquiries@joburg.org.za");
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("AIVS Approval Notice | ");
                    text.Span(attrNo).SemiBold();
                    text.Span(" | Generated ");
                    text.Span(approvedAt.ToString("yyyy-MM-dd HH:mm"));
                });
            });
        }).GeneratePdf(path);

        return path;
    }

    private static void AddRow(TableDescriptor table, string label, string? value)
    {
        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(label).SemiBold();
        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Text(value ?? "-");
    }

    private static string Safe(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Where(c => !invalid.Contains(c)).ToArray());
    }
}
