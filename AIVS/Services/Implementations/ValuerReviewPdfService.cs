using AIVS.Data;
using AIVS.Models.Configuration;
using AIVS.Models.ViewModels.UserManagement;
using AIVS.Services.Interface;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;
namespace AIVS.Services.Implementations
{
    public class ValuerReviewPdfService : IValuerReviewPdfService
    {
        private readonly AttributesDbContext _context;
        private readonly AttributeStorageSettings _storageSettings;

        public ValuerReviewPdfService(
            AttributesDbContext context,
            IOptions<AttributeStorageSettings> storageSettings)
        {
            _context = context;
            _storageSettings = storageSettings.Value;
        }

        public async Task<string> GenerateReviewedFormPdfAsync(
            long reviewId,
            AivsCurrentUserVm currentUser)
        {
            var review = await _context.AttrValuerReviews
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == reviewId);

            if (review == null)
                throw new InvalidOperationException("Valuer review could not be found.");

            var item = await _context.AttrPropertyInfo
                .AsNoTracking()
                .Include(x => x.PropertyDetails)
                .FirstOrDefaultAsync(x => x.Attr_ID == review.Attr_ID);

            if (item == null)
                throw new InvalidOperationException("Attribute submission could not be found.");

            var sections = await _context.AttrValuerReviewSections
                .AsNoTracking()
                .Where(x => x.ReviewId == review.Id)
                .OrderBy(x => x.Id)
                .ToListAsync();

            var attrNo = item.Attr_No ?? $"ATTR-{item.Attr_ID}";
            var folder = Path.Combine(
                _storageSettings.PhysicalRootPath,
                attrNo,
                "Valuer Review");

            Directory.CreateDirectory(folder);

            var fileName = $"{SafeFileName(attrNo)}_Reviewed_Form_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var fullPath = Path.Combine(folder, fileName);

            var generatedAt = DateTime.Now;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("AIVS - Attribute Inspection & Verification System")
                            .Bold()
                            .FontSize(14);

                        col.Item().Text("Valuer Reviewed Attribute Form")
                            .FontSize(11)
                            .FontColor(Colors.Grey.Darken2);

                        col.Item().PaddingTop(5).LineHorizontal(1);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text("Submission Details")
                            .Bold()
                            .FontSize(12);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                            });

                            AddCell(table, "Reference No", true);
                            AddCell(table, attrNo);
                            AddCell(table, "Status", true);
                            AddCell(table, item.Attr_Status);

                            AddCell(table, "Property Type", true);
                            AddCell(table, item.Property_Type);
                            AddCell(table, "Sector", true);
                            AddCell(table, item.RoutedSector ?? item.Sector);

                            AddCell(table, "Premise ID", true);
                            AddCell(table, item.Premise_id);
                            AddCell(table, "Valuation Key", true);
                            AddCell(table, item.Valuation_Key);

                            AddCell(table, "Property Description", true);
                            table.Cell().ColumnSpan(3).Element(CellStyle).Text(item.Property_Desc ?? "-");
                        });

                        col.Item().Text("Original Client Submission")
                            .Bold()
                            .FontSize(12);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                            });

                            AddCell(table, "Submitted By", true);
                            AddCell(table, item.SubmittedByName);
                            AddCell(table, "Submitted Email", true);
                            AddCell(table, item.SubmittedByEmail);

                            AddCell(table, "Submitted Date", true);
                            AddCell(table, item.SubmissionDateTime.ToString("yyyy-MM-dd HH:mm"));
                            AddCell(table, "Evidence Count", true);
                            AddCell(table, item.Evidence_Count.ToString() ?? "0");

                            AddCell(table, "Client Comment", true);
                            table.Cell().ColumnSpan(3).Element(CellStyle).Text(item.ClientComment ?? "-");
                        });

                        col.Item().PageBreak();

                        col.Item().Text("Valuer Section Review")
                            .Bold()
                            .FontSize(12);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(3);
                            });

                            AddHeader(table, "Section");
                            AddHeader(table, "Decision");
                            AddHeader(table, "Correction");
                            AddHeader(table, "Inspection");
                            AddHeader(table, "Comment");

                            foreach (var section in sections)
                            {
                                AddCell(table, section.SectionName);
                                AddCell(table, section.SectionDecision);
                                AddCell(table, section.RequiresCorrection ? "Yes" : "No");
                                AddCell(table, section.RequiresInspection ? "Yes" : "No");
                                AddCell(table, section.SectionComment);
                            }
                        });

                        col.Item().Text("Final Valuer Decision")
                            .Bold()
                            .FontSize(12);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(3);
                            });

                            AddCell(table, "Final Decision", true);
                            AddCell(table, review.FinalDecision);

                            AddCell(table, "Final Comment", true);
                            AddCell(table, review.FinalComment);

                            AddCell(table, "Review Status", true);
                            AddCell(table, review.ReviewStatus);

                            AddCell(table, "Ready for OVVIO", true);
                            AddCell(table, review.ReadyForOvvioExtract ? "Yes" : "No");

                            AddCell(table, "Reviewed By", true);
                            AddCell(table, currentUser.FullName);

                            AddCell(table, "Reviewed Date", true);
                            AddCell(table, generatedAt.ToString("yyyy-MM-dd HH:mm"));
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generated by AIVS on ");
                        text.Span(generatedAt.ToString("yyyy-MM-dd HH:mm")).SemiBold();
                        text.Span(" | ");
                        text.Span(attrNo).SemiBold();
                    });
                });
            })
            .GeneratePdf(fullPath);

            return fullPath;
        }

        private static void AddHeader(TableDescriptor table, string text)
        {
            table.Cell()
                .Background(Colors.Grey.Lighten2)
                .Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Padding(5)
                .Text(text)
                .Bold();
        }

        private static void AddCell(TableDescriptor table, string? text, bool bold = false)
        {
            var cell = table.Cell()
                .Element(CellStyle)
                .Text(string.IsNullOrWhiteSpace(text) ? "-" : text.Trim());

            if (bold)
                cell.Bold();
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5);
        }

        private static string SafeFileName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Reviewed_Form";

            var invalidChars = Path.GetInvalidFileNameChars();

            var cleaned = new string(value
                .Where(ch => !invalidChars.Contains(ch))
                .ToArray());

            return string.IsNullOrWhiteSpace(cleaned)
                ? "Reviewed_Form"
                : cleaned;
        }
    }
}