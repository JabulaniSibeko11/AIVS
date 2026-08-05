using AIVS.Data;
using AIVS.Models.Attributes;
using AIVS.Models.Configuration;
using AIVS.Models.ViewModels.UserManagement;
using AIVS.Models.ViewModels.ValuerInbox;
using AIVS.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AIVS.Services.Implementations
{
    public class ValuerInboxService : IValuerInboxService
    {
        private readonly AttributesDbContext _context;
        private readonly ILogger<ValuerInboxService> _logger;
        private readonly IEmailService _emailService;
        private readonly AttributeStorageSettings _storageSettings;
        private readonly INotificationService _notificationService;
        private readonly IValuerReviewPdfService _valuerReviewPdfService;
        private readonly IOptions<SectorManagerQaSettings> _sectorManagerQaSettings;
        public ValuerInboxService(
            AttributesDbContext context,
            ILogger<ValuerInboxService> logger, IEmailService emailService,
            IOptions<AttributeStorageSettings> storageSettings
            , INotificationService notificationService
            , IValuerReviewPdfService valuerReviewPdfService,
      IOptions<SectorManagerQaSettings> sectorManagerQaSettings)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _storageSettings = storageSettings.Value;
            _notificationService = notificationService;
            _valuerReviewPdfService = valuerReviewPdfService;
            _sectorManagerQaSettings = sectorManagerQaSettings;
        }

        public async Task<List<ValuerInboxItemVm>> GetMyInboxAsync(AivsCurrentUserVm currentUser)
        {
            if (currentUser.UserId == null)
                return new List<ValuerInboxItemVm>();

            var userIdText = currentUser.UserId.Value.ToString();

            var allowedStatuses = new[]
 {
    "Claimed",
    "ValuerReview",
    "InspectionRequired",
    "InspectionConfirmed",
    "InspectionDetailsSent",
    "InspectionExpired",
    "Resubmitted",
    "ReturnedToValuer"
};

            return await _context.AttrPropertyInfo
                .AsNoTracking()
                .Include(x => x.PropertyDetails)
                .Where(x =>
                    x.IsActive == true &&
                    x.Task_Assigned_To_UserId == userIdText &&
                    x.Attr_Status != null &&
                    allowedStatuses.Contains(x.Attr_Status))
                .OrderByDescending(x => x.Task_Assigned_DateTime)
                .Select(x => new ValuerInboxItemVm
                {
                    AttrId = x.Attr_ID,
                    AttrNo = x.Attr_No,
                    PropertyDescription = x.Property_Desc,
                    Township = x.PropertyDetails != null ? x.PropertyDetails.Township : null,
                    Sector = x.RoutedSector,
                    Status = x.Attr_Status,

                    AssignedDate = x.Task_Assigned_DateTime,
                    AssignedBy = x.Task_Assigner,
                    EvidenceCount = x.Evidence_Count,
                    ReviewStarted =
    x.Attr_Status == "ValuerReview" ||
    x.Attr_Status == "InspectionRequired" ||
    x.Attr_Status == "InspectionConfirmed" ||
    x.Attr_Status == "InspectionDetailsSent" ||
    x.Attr_Status == "InspectionExpired",

                    ReviewId = _context.AttrValuerReviews
                        .Where(r =>
                            r.Attr_ID == x.Attr_ID &&
                            (
                                r.ReviewStatus == "InProgress" ||
                                r.ReviewStatus == "InspectionRequired" ||
                                r.ReviewStatus == "InspectionConfirmed"
                            ))
                        .OrderByDescending(r => r.StartedAt)
                        .Select(r => (long?)r.Id)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }
        public async Task<ValuerReviewPageVm> OpenReviewAsync(long attrId, AivsCurrentUserVm currentUser)
        {
            if (currentUser.UserId == null)
                throw new InvalidOperationException("Your AIVS user could not be verified.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var userIdText = currentUser.UserId.Value.ToString();

            var allowedStatuses = new[]
  {
    "Claimed",
    "ValuerReview",
    "InspectionRequired",
    "InspectionConfirmed",
    "InspectionDetailsSent",
    "InspectionExpired",
    "Resubmitted",
    "ReturnedToValuer"
};

            var item = await _context.AttrPropertyInfo
    .Include(x => x.PropertyDetails)
    .FirstOrDefaultAsync(x =>
        x.Attr_ID == attrId &&
        x.IsActive == true &&
        allowedStatuses.Contains(x.Attr_Status!));

            if (item == null)
                throw new InvalidOperationException("The selected attribute submission was not found or is not available for review.");

            if (item.Task_Assigned_To_UserId != userIdText)
                throw new InvalidOperationException("This submission is not assigned to you.");

            var now = DateTime.Now;

            var assignment = await _context.AttrValuerAssignments
                .Where(x =>
                    x.Attr_ID == attrId &&
                    x.AssignmentStatus == "Active")
                .OrderByDescending(x => x.AssignedAt)
                .FirstOrDefaultAsync();

            var review = await _context.AttrValuerReviews
    .Where(x =>
        x.Attr_ID == attrId &&
        x.ReviewerUserId == currentUser.UserId.Value &&
        (
            x.ReviewStatus == "InProgress" ||
            x.ReviewStatus == "InspectionRequired" ||
            x.ReviewStatus == "InspectionConfirmed"
        ))
    .OrderByDescending(x => x.StartedAt)
    .FirstOrDefaultAsync();

            if (review == null)
            {
                review = new AttrValuerReview
                {
                    Attr_ID = item.Attr_ID,
                    Attr_No = item.Attr_No,
                    AssignmentId = assignment?.Id,

                    ReviewerUserId = currentUser.UserId.Value,
                    ReviewerUsername = currentUser.Username ?? currentUser.WindowsUsername,
                    ReviewerName = currentUser.FullName,
                    ReviewerEmail = currentUser.Email,
                    ReviewerRole = currentUser.Role,

                    ReviewStatus = "InProgress",
                    StartedAt = now,

                    CreatedBy = currentUser.Username ?? currentUser.WindowsUsername,
                    CreatedDate = now
                };

                _context.AttrValuerReviews.Add(review);
                await _context.SaveChangesAsync();

                await CreateDefaultReviewSectionsAsync(review, item, currentUser, now);
            }

            if (item.Attr_Status == "Claimed" ||
      item.Attr_Status == "Resubmitted" ||
      item.Attr_Status == "ReturnedToValuer")
            {
                var oldStatus = item.Attr_Status;

                item.Attr_Status = "ValuerReview";
                item.RevisionRequired = false;
                item.RevisionRequestedBy = null;
                item.RevisionRequestedDateTime = null;

                item.UpdatedBy = currentUser.Username ?? currentUser.WindowsUsername;
                item.UpdatedDate = now;

                var auditAction = oldStatus switch
                {
                    "Resubmitted" => "Resubmission Review Started",
                    "ReturnedToValuer" => "Sector Manager Return Review Started",
                    _ => "Review Started"
                };

                var auditComment = oldStatus switch
                {
                    "Resubmitted" => "Valuer opened the resubmitted attribute submission for review.",
                    "ReturnedToValuer" => "Valuer reopened the submission returned by the Sector Manager.",
                    _ => "Valuer opened the attribute submission for review."
                };

                _context.AttrPropertyInfoAuditTrail.Add(new AttrPropertyInfoAuditTrail
                {
                    Attr_ID = item.Attr_ID,
                    Attr_No = item.Attr_No,
                    Action = auditAction,
                    OldStatus = oldStatus,
                    NewStatus = "ValuerReview",
                    ActionByUserId = currentUser.UserId.Value.ToString(),
                    ActionByName = currentUser.FullName,
                    ActionRole = currentUser.Role ?? "Valuer",
                    Comment = auditComment,
                    ActionDateTime = now
                });

                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            await AcquireOrRefreshReviewLockAsync(review, currentUser);
            return await BuildReviewPageAsync(review.Id, currentUser);
        }

        private async Task CreateDefaultReviewSectionsAsync(
            AttrValuerReview review,
            AttrPropertyInfo item,
            AivsCurrentUserVm currentUser,
            DateTime now)
        {
            var sections = GetSectionsForFormType(item.Property_Type);

            foreach (var section in sections)
            {
                _context.AttrValuerReviewSections.Add(new AttrValuerReviewSection
                {
                    ReviewId = review.Id,
                    Attr_ID = item.Attr_ID,
                    Attr_No = item.Attr_No,

                    SectionCode = section.Code,
                    SectionName = section.Name,

                    SectionDecision = null,
                    SectionComment = null,

                    RequiresCorrection = false,
                    RequiresInspection = false,

                    CreatedBy = currentUser.Username ?? currentUser.WindowsUsername,
                    CreatedDate = now
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task<ValuerReviewPageVm> BuildReviewPageAsync(long reviewId, AivsCurrentUserVm currentUser)
        {
            var review = await _context.AttrValuerReviews
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == reviewId);

            if (review == null)
                throw new InvalidOperationException("Review could not be found.");

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
                .Select(x => new ValuerReviewSectionVm
                {
                    SectionId = x.Id,
                    SectionCode = x.SectionCode,
                    SectionName = x.SectionName,
                    SectionDecision = x.SectionDecision,
                    SectionComment = x.SectionComment,
                    RequiresCorrection = x.RequiresCorrection,
                    RequiresInspection = x.RequiresInspection
                })
                .ToListAsync();

            var requiredSectionCodes = GetRequiredReviewSectionCodes(item.Property_Type);

            var requiredSections = sections
                .Where(x => requiredSectionCodes.Contains(x.SectionCode))
                .ToList();

            var hasRequiredCorrections = requiredSections.Any(x =>
                x.RequiresCorrection ||
                x.SectionDecision == "Needs correction");

            var hasInspectionRequired = sections.Any(x =>
                x.RequiresInspection ||
                x.SectionDecision == "Requires inspection");

            // Decisions are now made once, at the bottom of the comparison page.
            // Individual section decisions are no longer required before OVVIO submission.
            var canSubmitToOvvio =
                !hasRequiredCorrections &&
                !hasInspectionRequired;

            var submitBlockReason = "";

            if (hasRequiredCorrections)
            {
                submitBlockReason = "Required fields are selected for correction.";
            }
            else if (hasInspectionRequired)
            {
                submitBlockReason = "A physical inspection is required.";
            }

            var submittedForm = await BuildSubmittedAttributeViewModelAsync(item.Attr_ID);

            var evidenceFiles = await BuildEvidenceFilesAsync(item.Attr_ID);
            var physicalInspectionEvidenceFiles = await BuildPhysicalInspectionEvidenceFilesAsync(item.Attr_ID);
            var comparisonSections = await BuildComparisonSectionsAsync(review.Id, submittedForm);

            var draft = currentUser.UserId == null ? null : await _context.AttrValuerReviewDrafts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ReviewId == review.Id && x.UserId == currentUser.UserId.Value);

            var activeLock = await _context.AttrValuerReviewLocks.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ReviewId == review.Id && x.IsActive && x.ExpiresAt > DateTime.Now);

            var auditTimeline = await _context.AttrPropertyInfoAuditTrail.AsNoTracking()
                .Where(x => x.Attr_ID == item.Attr_ID)
                .OrderByDescending(x => x.ActionDateTime)
                .Take(12)
                .Select(x => new AttributeAuditEventVm
                {
                    Action = x.Action,
                    Status = x.NewStatus,
                    ActionBy = x.ActionByName,
                    Comment = x.Comment,
                    ActionDateTime = x.ActionDateTime
                }).ToListAsync();

            var activeInspectionRequest = await _context.AttrInspectionRequests
                .AsNoTracking()
                .Include(x => x.Slots)
                .Where(x =>
                    x.Attr_ID == item.Attr_ID &&
                    (
                        x.Status == "PendingClientResponse" ||
                        x.Status == "Confirmed" ||
                        x.Status == "InspectionDetailsSent" ||
                        x.Status == "Expired"
                    ))
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new InspectionRequestVm
                {
                    Id = x.Id,
                    Status = x.Status,
                    ClientEmail = x.ClientEmail,
                    CreatedDate = x.CreatedDate,
                    ConfirmedDateTime = x.ConfirmedDateTime,
                    ValuerDetailsSent = x.ValuerDetailsSent,
                    ValuerDetailsSentAt = x.ValuerDetailsSentAt,
                    InspectionPin = x.InspectionPin,
                    Slots = x.Slots
                        .OrderBy(s => s.SlotNo)
                        .Select(s => new InspectionSlotVm
                        {
                            Id = s.Id,
                            SlotNo = s.SlotNo,
                            ProposedDateTime = s.ProposedDateTime,
                            SlotStatus = s.SlotStatus
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            return new ValuerReviewPageVm
            {
                AttrId = item.Attr_ID,
                AttrNo = item.Attr_No,
                ReviewId = review.Id,

                PropertyDescription = item.Property_Desc,
                Township = item.PropertyDetails != null ? item.PropertyDetails.Township : null,
                Sector = item.RoutedSector,

                Status = item.Attr_Status,
                FormType = item.Property_Type,
                EvidenceCount = item.Evidence_Count,

                ReviewStatus = review.ReviewStatus,
                StartedAt = review.StartedAt,

                SubmittedForm = submittedForm,

                Sections = sections,
                EvidenceFiles = evidenceFiles,

                PhysicalInspectionEvidenceFiles = physicalInspectionEvidenceFiles,
                ComparisonSections = comparisonSections,
                HasCityData = comparisonSections.Any(x => x.Fields.Any(f => f.HasCityValue)),
                ActiveTab = draft?.ActiveTab ?? "1",
                DifferencesOnly = draft?.DifferencesOnly ?? true,
                FinalComment = draft?.ValuerComment ?? review.FinalComment,
                IsLockedByAnotherUser = activeLock != null && currentUser.UserId != activeLock.UserId,
                LockedByName = activeLock?.UserName,
                LockExpiresAt = activeLock?.ExpiresAt,
                AuditTimeline = auditTimeline,

                ActiveInspectionRequest = activeInspectionRequest,

                CanSubmitToOvvio = canSubmitToOvvio,
                HasRequiredCorrections = hasRequiredCorrections,
                HasInspectionRequired = hasInspectionRequired,
                SubmitBlockReason = submitBlockReason,

                TotalSections = sections.Count,
                ReviewedSections = sections.Count(x => !string.IsNullOrWhiteSpace(x.SectionDecision)),
                CorrectionSections = sections.Count(x => x.RequiresCorrection || x.SectionDecision == "Needs correction"),
                InspectionSections = sections.Count(x => x.RequiresInspection || x.SectionDecision == "Requires inspection"),

                FinalDecision = review.FinalDecision,
                ValuerEvidencePath = item.ValuerEvidencePath,
            };
        }

        private async Task<List<AttributeComparisonSectionVm>> BuildComparisonSectionsAsync(long reviewId, AttributeSubmissionViewModel? submittedForm)
        {
            if (submittedForm == null)
                return new();

            var premiseId = submittedForm.PropertyDetails?.PremiseId?.Trim();
            var formType = submittedForm.FormType?.Trim();

            var cityRows = new List<AttrCityAttributeValue>();
            if (!string.IsNullOrWhiteSpace(premiseId))
            {
                var premiseKey = premiseId.ToUpperInvariant();
                var cityRowsForPremise = await _context.AttrCityAttributeValues
                    .AsNoTracking()
                    .Where(x => x.IsActive && x.PremiseId.Trim().ToUpper() == premiseKey)
                    .OrderBy(x => x.SectionCode)
                    .ThenBy(x => x.DisplayOrder)
                    .ThenBy(x => x.FieldLabel)
                    .ToListAsync();

                cityRows = cityRowsForPremise;
                if (cityRowsForPremise.Count > 0 && !string.IsNullOrWhiteSpace(formType))
                {
                    var normalisedFormType = NormalizeFormType(formType);
                    var matchingRows = cityRowsForPremise
                        .Where(x => string.IsNullOrWhiteSpace(x.FormType) ||
                                    NormalizeFormType(x.FormType) == normalisedFormType)
                        .ToList();

                    if (matchingRows.Count > 0)
                        cityRows = matchingRows;
                }
            }

            var clientValues = BuildClientValueLookup(submittedForm);
            var clientLabels = BuildClientFieldLabelLookup(submittedForm);

            var selectedKeys = await _context.AttrValuerReviewFieldCorrections.AsNoTracking()
                .Where(x => x.ReviewId == reviewId && x.IsActive)
                .Select(x => x.SectionCode + ":" + x.FieldCode)
                .ToListAsync();
            var selected = selectedKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Build from BOTH sources. Client-only keys remain visible while the City
            // table is empty; once the extract is loaded the same rows become a true
            // side-by-side comparison without changing the page structure.
            var cityByKey = cityRows
                .GroupBy(x => BuildComparisonKey(x.SectionCode, x.FieldCode), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.OrderBy(r => r.DisplayOrder).First(), StringComparer.OrdinalIgnoreCase);

            var allKeys = clientValues.Keys
                .Concat(cityByKey.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var allowedSections = GetComparisonSectionsForForm(submittedForm);
            allKeys = allKeys
     .Where(key =>
     {
         var sectionCode = key.Split(':', 2)[0];

         return
             !IsExcludedComparisonSection(sectionCode) &&
             allowedSections.Contains(sectionCode);
     })
     .ToList();

            return allKeys
                .GroupBy(key => key.Split(':', 2)[0], StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var sectionCode = NormalizeCode(group.Key);
                    var fields = group.Select((key, index) =>
                    {
                        var parts = key.Split(':', 2);
                        var fieldCode = parts.Length > 1 ? NormalizeCode(parts[1]) : string.Empty;
                        cityByKey.TryGetValue(key, out var cityRow);
                        clientValues.TryGetValue(key, out var clientValue);
                        clientLabels.TryGetValue(key, out var clientLabel);

                        var cityValue = CleanComparisonValue(cityRow?.FieldValue);
                        var cleanedClientValue = CleanComparisonValue(clientValue);
                        var hasCityValue = !string.IsNullOrWhiteSpace(cityValue);
                        var readOnly = fieldCode is "H_AREA" or "HAREA";

                        return new AttributeComparisonFieldVm
                        {
                            FieldCode = fieldCode,
                            FieldLabel = cityRow?.FieldLabel
                                         ?? clientLabel
                                         ?? HumaniseFieldCode(fieldCode),
                            CityValue = cityValue,
                            ClientValue = cleanedClientValue,
                            DisplayOrder = cityRow?.DisplayOrder ?? (index + 1) * 10,
                            IsReadOnly = readOnly,
                            HasCityValue = hasCityValue,
                            HasDifference = hasCityValue && !readOnly && !ComparisonValuesEqual(cityValue, cleanedClientValue),
                            IsSelectedForCorrection = selected.Contains(BuildComparisonKey(sectionCode, fieldCode))
                        };
                    })
                    // Do not create rows that are empty on both sides. Client-submitted
                    // zero/No values are retained because they are meaningful values.
                    .Where(x => !string.IsNullOrWhiteSpace(x.ClientValue) || x.HasCityValue)
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.FieldLabel)
                    .ToList();

                    return new AttributeComparisonSectionVm
                    {
                        SectionCode = sectionCode,
                        SectionName = GetComparisonSectionName(sectionCode),
                        TabNumber = IsPropertyOrValuationSection(sectionCode) ? 1 : 2,
                        DisplayOrder = GetComparisonSectionOrder(sectionCode),
                        Fields = fields
                    };
                })
                .Where(x => x.Fields.Count > 0)
                .OrderBy(x => x.TabNumber)
                .ThenBy(x => x.DisplayOrder)
                .ThenBy(x => x.SectionName)
                .ToList();
        }

        private static HashSet<string> GetComparisonSectionsForForm(
     AttributeSubmissionViewModel form)
        {
            var sections = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
    {
        "VALUATION_DETAILS",
        "ACCESS"
    };

            var type = NormalizeFormType(form.FormType);

            if (type == "BUSINESS")
            {
                sections.UnionWith(new[]
                {
            "BUSINESS_BUILDINGS",
            "BUSINESS_SECTIONS",
            "BUSINESS_GENERAL",
            "CALCULATIONS"
        });
            }
            else if (type == "DRC")
            {
                sections.UnionWith(new[]
                {
            "DRC_BUILDINGS",
            "DRC_IMPROVEMENTS",
            "DRC_VACANT_LAND",
            "DRC_MARKET_VALUE",
            "CALCULATIONS"
        });
            }
            else
            {
                sections.UnionWith(new[]
                {
            "PRIMARY_ATTRIBUTES",
            "SECONDARY_ATTRIBUTES",
            "CALCULATIONS"
        });
            }

            return sections;
        }

        private static Dictionary<string, string>
     BuildClientFieldLabelLookup(
         AttributeSubmissionViewModel form)
        {
            var labels = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            AddObjectLabels(
                labels,
                "VALUATION_DETAILS",
                form.ValuationDetails);

            AddObjectLabels(
                labels,
                "ACCESS",
                form.Access);

            AddObjectLabels(
                labels,
                "PRIMARY_ATTRIBUTES",
                form.PrimaryAttributes);

            AddObjectLabels(
                labels,
                "SECONDARY_ATTRIBUTES",
                form.SecondaryAttributes);

            AddObjectLabels(
                labels,
                "CALCULATIONS",
                form.Calculations);

            AddObjectLabels(
                labels,
                "BUSINESS_GENERAL",
                form.BusinessGeneral);

            AddObjectLabels(
                labels,
                "DRC_MARKET_VALUE",
                form.DrcMarketValueDemolition);

            AddListLabels(
                labels,
                "BUSINESS_BUILDINGS",
                "BUILDING",
                form.BusinessBuildings);

            AddListLabels(
                labels,
                "BUSINESS_SECTIONS",
                "SECTION",
                form.BusinessSections);

            AddListLabels(
                labels,
                "DRC_BUILDINGS",
                "BUILDING",
                form.DrcBuildings);

            AddListLabels(
                labels,
                "DRC_IMPROVEMENTS",
                "IMPROVEMENT",
                form.DrcImprovements);

            AddListLabels(
                labels,
                "DRC_VACANT_LAND",
                "LAND",
                form.DrcVacantLands);

            return labels;
        }
        private static void AddObjectLabels(IDictionary<string, string> labels, string sectionCode, object? source)
        {
            if (source == null) return;
            foreach (var property in source.GetType().GetProperties())
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0) continue;
                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string)) continue;
                labels[BuildComparisonKey(sectionCode, property.Name)] = HumaniseFieldCode(property.Name);
            }
        }

        private static void AddListLabels<T>(IDictionary<string, string> labels, string sectionCode, string rowPrefix, IEnumerable<T>? rows)
        {
            if (rows == null) return;
            var rowNumber = 0;
            foreach (var row in rows)
            {
                rowNumber++;
                if (row == null) continue;
                foreach (var property in row.GetType().GetProperties())
                {
                    if (!property.CanRead || property.GetIndexParameters().Length > 0) continue;
                    var fieldCode = $"{rowPrefix}_{rowNumber}_{NormalizeCode(property.Name)}";
                    labels[BuildComparisonKey(sectionCode, fieldCode)] = $"{HumaniseFieldCode(rowPrefix)} {rowNumber} - {HumaniseFieldCode(property.Name)}";
                }
            }
        }

        private static string HumaniseFieldCode(string? value)
        {
            var code = NormalizeCode(value);
            if (string.IsNullOrWhiteSpace(code)) return "Field";
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo
                .ToTitleCase(code.Replace('_', ' ').ToLowerInvariant())
                .Replace("Id", "ID")
                .Replace("Sg", "SG")
                .Replace("Tla", "TLA")
                .Replace("Gba", "GBA")
                .Replace("Nla", "NLA")
                .Replace("Drc", "DRC")
                .Replace("Sqm", "SQM");
        }

        private static Dictionary<string, string?>
    BuildClientValueLookup(
        AttributeSubmissionViewModel form)
        {
            var values = new Dictionary<string, string?>(
                StringComparer.OrdinalIgnoreCase);

            AddObjectValues(
                values,
                "VALUATION_DETAILS",
                form.ValuationDetails);

            AddObjectValues(
                values,
                "ACCESS",
                form.Access);

            AddObjectValues(
                values,
                "PRIMARY_ATTRIBUTES",
                form.PrimaryAttributes);

            AddObjectValues(
                values,
                "SECONDARY_ATTRIBUTES",
                form.SecondaryAttributes);

            AddObjectValues(
                values,
                "CALCULATIONS",
                form.Calculations);

            AddObjectValues(
                values,
                "BUSINESS_GENERAL",
                form.BusinessGeneral);

            AddObjectValues(
                values,
                "DRC_MARKET_VALUE",
                form.DrcMarketValueDemolition);

            AddListValues(
                values,
                "BUSINESS_BUILDINGS",
                "BUILDING",
                form.BusinessBuildings);

            AddListValues(
                values,
                "BUSINESS_SECTIONS",
                "SECTION",
                form.BusinessSections);

            AddListValues(
                values,
                "DRC_BUILDINGS",
                "BUILDING",
                form.DrcBuildings);

            AddListValues(
                values,
                "DRC_IMPROVEMENTS",
                "IMPROVEMENT",
                form.DrcImprovements);

            AddListValues(
                values,
                "DRC_VACANT_LAND",
                "LAND",
                form.DrcVacantLands);

            return values;
        }


        private static bool IsExcludedComparisonSection(
    string? sectionCode)
        {
            var code = NormalizeCode(sectionCode);

            return code is
                "PROPERTY" or
                "PROPERTY_DETAILS" or
                "CONTACT" or
                "CONTACT_INFO" or
                "CONTACT_INFORMATION";
        }

        private static void AddObjectValues(IDictionary<string, string?> values, string sectionCode, object? source)
        {
            if (source == null) return;
            foreach (var property in source.GetType().GetProperties())
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0) continue;
                var value = property.GetValue(source);
                if (value is System.Collections.IEnumerable && value is not string) continue;
                values[BuildComparisonKey(sectionCode, NormalizeCode(property.Name))] = ConvertComparisonValue(value);
            }
        }

        private static void AddListValues<T>(IDictionary<string, string?> values, string sectionCode, string rowPrefix, IEnumerable<T>? rows)
        {
            if (rows == null) return;
            var rowNumber = 0;
            foreach (var row in rows)
            {
                rowNumber++;
                if (row == null) continue;
                foreach (var property in row.GetType().GetProperties())
                {
                    if (!property.CanRead || property.GetIndexParameters().Length > 0) continue;
                    var fieldCode = $"{rowPrefix}_{rowNumber}_{NormalizeCode(property.Name)}";
                    values[BuildComparisonKey(sectionCode, fieldCode)] = ConvertComparisonValue(property.GetValue(row));
                }
            }
        }

        private static string BuildComparisonKey(string sectionCode, string fieldCode) => $"{NormalizeCode(sectionCode)}:{NormalizeCode(fieldCode)}";

        private static string NormalizeFormType(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var normalised = new string(value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());

            // Treat the common portal/City descriptions as the same logical form.
            return normalised switch
            {
                "RESIDENTIAL" or "STRESIDENTIAL" or "SECTIONALTITLERESIDENTIAL" => "RESIDENTIAL",
                "BUSINESS" or "BUSINESSCOMMERCIAL" or "COMMERCIAL" or "NONRES" or "NONRESIDENTIAL" => "BUSINESS",
                "DRC" or "DRCMETHOD" or "DEPRECIATEDREPLACEMENTCOST" => "DRC",
                _ => normalised
            };
        }
        private static string NormalizeCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var result = new System.Text.StringBuilder();
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                if (!char.IsLetterOrDigit(character))
                {
                    if (result.Length > 0 && result[^1] != '_') result.Append('_');
                    continue;
                }
                if (char.IsUpper(character) && i > 0 && char.IsLower(value[i - 1]) && result[^1] != '_') result.Append('_');
                result.Append(char.ToUpperInvariant(character));
            }
            return result.ToString().Trim('_');
        }

        private static string? ConvertComparisonValue(object? value) => value switch
        {
            null => null,
            bool b => b ? "Yes" : "No",
            DateTime d => d.ToString("yyyy-MM-dd"),
            decimal d => d.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture),
            double d => d.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture),
            float f => f.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
        };
        private static string? CleanComparisonValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        private static bool ComparisonValuesEqual(string? cityValue, string? clientValue)
        {
            var city = CleanComparisonValue(cityValue) ?? string.Empty;
            var client = CleanComparisonValue(clientValue) ?? string.Empty;
            if (decimal.TryParse(city, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cityNumber) &&
                decimal.TryParse(client, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var clientNumber))
                return cityNumber == clientNumber;
            return string.Equals(city, client, StringComparison.OrdinalIgnoreCase);
        }
        private static bool IsPropertyOrValuationSection(string code) => code is "PROPERTY" or "PROPERTY_DETAILS" or "VALUATION" or "VALUATION_DETAILS" or "ACCESS" or "CONTACT_INFO";
        private static int GetComparisonSectionOrder(string code) => code switch
        {
            "PROPERTY" or "PROPERTY_DETAILS" => 10,
            "VALUATION" or "VALUATION_DETAILS" => 20,
            "ACCESS" => 25,
            "CONTACT_INFO" => 27,
            "PRIMARY_ATTRIBUTES" => 30,
            "SECONDARY_ATTRIBUTES" => 40,
            "BUSINESS_BUILDINGS" => 50,
            "BUSINESS_SECTIONS" => 60,
            "BUSINESS_GENERAL" => 70,
            "DRC_BUILDINGS" => 80,
            "DRC_IMPROVEMENTS" => 90,
            "DRC_VACANT_LAND" => 100,
            "DRC_MARKET_VALUE" => 110,
            "CALCULATIONS" => 120,
            _ => 999
        };
        private static string GetComparisonSectionName(string code) => code switch
        {
            "PROPERTY" or "PROPERTY_DETAILS" => "Property Details",
            "VALUATION" or "VALUATION_DETAILS" => "Valuation Details",
            "ACCESS" => "Access",
            "CONTACT_INFO" => "Contact Information",
            "PRIMARY_ATTRIBUTES" => "Primary Attributes",
            "SECONDARY_ATTRIBUTES" => "Secondary Attributes",
            "BUSINESS_BUILDINGS" => "Business Buildings",
            "BUSINESS_SECTIONS" => "Business Sections",
            "BUSINESS_GENERAL" => "Business General",
            "DRC_BUILDINGS" => "DRC Buildings",
            "DRC_IMPROVEMENTS" => "DRC Improvements",
            "DRC_VACANT_LAND" => "DRC Vacant Land",
            "DRC_MARKET_VALUE" => "DRC Market Value and Demolition",
            "CALCULATIONS" => "Calculations",
            _ => code.Replace('_', ' ')
        };

        private async Task AcquireOrRefreshReviewLockAsync(AttrValuerReview review, AivsCurrentUserVm currentUser)
        {
            if (currentUser.UserId == null) return;
            var now = DateTime.Now;
            var reviewLock = await _context.AttrValuerReviewLocks.FirstOrDefaultAsync(x => x.ReviewId == review.Id);
            if (reviewLock == null)
            {
                _context.AttrValuerReviewLocks.Add(new AttrValuerReviewLock
                {
                    ReviewId = review.Id,
                    Attr_ID = review.Attr_ID,
                    UserId = currentUser.UserId.Value,
                    UserName = currentUser.FullName ?? currentUser.Username ?? "Valuer",
                    AcquiredAt = now,
                    LastActivityAt = now,
                    ExpiresAt = now.AddMinutes(20),
                    IsActive = true
                });
            }
            else if (!reviewLock.IsActive || reviewLock.ExpiresAt <= now || reviewLock.UserId == currentUser.UserId.Value)
            {
                reviewLock.UserId = currentUser.UserId.Value;
                reviewLock.UserName = currentUser.FullName ?? currentUser.Username ?? "Valuer";
                reviewLock.AcquiredAt = now; reviewLock.LastActivityAt = now;
                reviewLock.ExpiresAt = now.AddMinutes(20); reviewLock.IsActive = true;
            }
            await _context.SaveChangesAsync();
        }

        public async Task SaveDraftAsync(SaveReviewDraftVm vm, AivsCurrentUserVm currentUser)
        {
            if (currentUser.UserId == null) throw new InvalidOperationException("Your AIVS user could not be verified.");
            await EnsureCurrentAssignmentAsync(vm.AttrId, currentUser);
            var review = await _context.AttrValuerReviews.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == vm.ReviewId && x.Attr_ID == vm.AttrId && x.ReviewerUserId == currentUser.UserId.Value)
                ?? throw new InvalidOperationException("This review is not assigned to you.");
            var draft = await _context.AttrValuerReviewDrafts.FirstOrDefaultAsync(x => x.ReviewId == vm.ReviewId && x.UserId == currentUser.UserId.Value);
            if (draft == null)
            {
                draft = new AttrValuerReviewDraft { ReviewId = review.Id, Attr_ID = review.Attr_ID, UserId = currentUser.UserId.Value };
                _context.AttrValuerReviewDrafts.Add(draft);
            }
            draft.ActiveTab = vm.ActiveTab is "1" or "2" ? vm.ActiveTab : "1";
            draft.ValuerComment = vm.ValuerComment?.Trim();
            draft.DifferencesOnly = vm.DifferencesOnly;
            draft.SavedAt = DateTime.Now;
            await AcquireOrRefreshReviewLockAsync(review, currentUser);
            await _context.SaveChangesAsync();
        }

        public async Task SaveCorrectionFieldsAsync(SaveCorrectionFieldsVm vm, AivsCurrentUserVm currentUser)
        {
            if (currentUser.UserId == null) throw new InvalidOperationException("Your AIVS user could not be verified.");
            await EnsureCurrentAssignmentAsync(vm.AttrId, currentUser);
            var review = await _context.AttrValuerReviews.FirstOrDefaultAsync(x => x.Id == vm.ReviewId && x.Attr_ID == vm.AttrId && x.ReviewerUserId == currentUser.UserId.Value)
                ?? throw new InvalidOperationException("This review is not assigned to you.");
            var form = await BuildSubmittedAttributeViewModelAsync(vm.AttrId);
            var comparisons = await BuildComparisonSectionsAsync(vm.ReviewId, form);
            var allowed = comparisons.SelectMany(x => x.Fields.Where(f => f.HasDifference && !f.IsReadOnly)
                .Select(f => new { Section = x.SectionCode, Field = f })).ToDictionary(x => BuildComparisonKey(x.Section, x.Field.FieldCode));
            var requested = (vm.FieldKeys ?? new()).Select(x => x.Trim()).Where(allowed.ContainsKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existing = await _context.AttrValuerReviewFieldCorrections.Where(x => x.ReviewId == vm.ReviewId).ToListAsync();
            foreach (var row in existing) row.IsActive = requested.Contains(BuildComparisonKey(row.SectionCode, row.FieldCode));
            foreach (var key in requested.Where(x => existing.All(e => !string.Equals(BuildComparisonKey(e.SectionCode, e.FieldCode), x, StringComparison.OrdinalIgnoreCase))))
            {
                var value = allowed[key];
                _context.AttrValuerReviewFieldCorrections.Add(new AttrValuerReviewFieldCorrection
                {
                    ReviewId = review.Id,
                    Attr_ID = review.Attr_ID,
                    SectionCode = value.Section,
                    FieldCode = value.Field.FieldCode,
                    FieldLabel = value.Field.FieldLabel,
                    CityValue = value.Field.CityValue,
                    ClientValue = value.Field.ClientValue,
                    IsActive = true,
                    SelectedByUserId = currentUser.UserId.Value,
                    SelectedByName = currentUser.FullName,
                    SelectedAt = DateTime.Now
                });
            }
            await _context.SaveChangesAsync();
        }

        public async Task SaveQuickSectionDecisionAsync(QuickSectionDecisionVm vm, AivsCurrentUserVm currentUser)
        {
            var reviewSectionCode = vm.SectionCode switch
            {
                "PROPERTY" => "PROPERTY_DETAILS",
                "VALUATION" => "VALUATION_DETAILS",
                _ => vm.SectionCode
            };
            var section = await _context.AttrValuerReviewSections.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ReviewId == vm.ReviewId && x.Attr_ID == vm.AttrId && x.SectionCode == reviewSectionCode)
                ?? throw new InvalidOperationException("The review section could not be found.");
            var decision = vm.Decision switch
            {
                "Accepted" => "Accepted",
                "Needs correction" => "Needs correction",
                "Requires inspection" => "Requires inspection",
                _ => throw new InvalidOperationException("Invalid section decision.")
            };
            await SaveSectionReviewAsync(new SaveSectionReviewVm
            {
                ReviewId = vm.ReviewId,
                SectionId = section.Id,
                SectionDecision = decision,
                RequiresCorrection = decision == "Needs correction",
                RequiresInspection = decision == "Requires inspection",
                SectionComment = decision == "Accepted" ? "Accepted from comparison view." : "Decision selected from comparison view."
            }, currentUser);
        }
        private async Task<List<ValuerEvidenceFileVm>> BuildEvidenceFilesAsync(long attrId)
        {
            var rows = await _context.AttrFiles
                .AsNoTracking()
                .Where(x =>
                    x.Attr_ID == attrId &&
                    x.IsActive == true &&
                    x.IsDeleted == false)
                .OrderByDescending(x => x.UploadedDateTime)
                .ToListAsync();

            var files = new List<ValuerEvidenceFileVm>();

            foreach (var row in rows)
            {
                AddEvidenceFile(files, row, row.Files1, "Evidence 1");
                AddEvidenceFile(files, row, row.Files2, "Evidence 2");
                AddEvidenceFile(files, row, row.Files3, "Evidence 3");
                AddEvidenceFile(files, row, row.Files4, "Evidence 4");
                AddEvidenceFile(files, row, row.Files5, "Evidence 5");
                AddEvidenceFile(files, row, row.Files6, "Evidence 6");
                AddEvidenceFile(files, row, row.Files7, "Evidence 7");
                AddEvidenceFile(files, row, row.Files8, "Evidence 8");
                AddEvidenceFile(files, row, row.Files9, "Evidence 9");
                AddEvidenceFile(files, row, row.Files10, "Evidence 10");

                AddEvidenceFile(files, row, row.Rep_Letter, "Representative Letter");
                AddEvidenceFile(files, row, row.Acknowledgement_FileName, "Acknowledgement");
                AddEvidenceFile(files, row, row.Bulk_File_Name, "Bulk File");
            }

            return files;
        }
        private void AddEvidenceFile(
    List<ValuerEvidenceFileVm> files,
    AttrFiles row,
    string? fileName,
    string label)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            var fullPath = ResolveEvidenceFilePath(row, fileName);

            files.Add(new ValuerEvidenceFileVm
            {
                Id = row.ID,
                AttrFileId = row.ID,
                FileName = fileName,
                DisplayName = label,
                FilePath = fullPath,
                FileType = GetContentType(fileName),
                UploadedDateTime = row.UploadedDateTime,
                UploadedBy = row.UploadedByName ?? row.CreatedBy
            });
        }
        private string ResolveEvidenceFilePath(AttrFiles row, string fileName)
        {
            if (Path.IsPathRooted(fileName))
                return fileName;

            if (!string.IsNullOrWhiteSpace(row.RootFolder))
            {
                return Path.Combine(row.RootFolder, fileName);
            }

            var attrNo = row.Attr_No;

            if (string.IsNullOrWhiteSpace(attrNo))
                attrNo = row.Attr_Ref_Files;

            if (string.IsNullOrWhiteSpace(attrNo))
                attrNo = "Unknown";

            return Path.Combine(
                _storageSettings.PhysicalRootPath,
                attrNo,
                _storageSettings.EvidenceFolderName,
                fileName);
        }
        private static string GetContentType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();

            return ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }
        private static List<(string Code, string Name)> GetSectionsForFormType(string? formType)
        {
            // Property Details and Contact Information are identification/reference data.
            // Valuers do not review or make section decisions on them, so they are
            // intentionally excluded from the review section list.
            var common = new List<(string Code, string Name)>
            {
                ("VALUATION_DETAILS", "Valuation Details"),
                ("ACCESS_INFORMATION", "Access Information"),
                ("EVIDENCE", "Client Evidence"),
                ("DECLARATION", "Declaration")
            };

            formType = formType?.Trim();

            if (formType == "BusinessCommercial")
            {
                common.Insert(4, ("BUSINESS_BUILDINGS", "Business Buildings"));
                common.Insert(5, ("BUSINESS_SECTIONS", "Business Sections"));
                common.Insert(6, ("BUSINESS_GENERAL", "Business General"));
                common.Insert(7, ("CALCULATIONS", "Calculations"));

                return common;
            }

            if (formType == "DRCMethod")
            {
                common.Insert(4, ("DRC_BUILDINGS", "DRC Buildings"));
                common.Insert(5, ("DRC_IMPROVEMENTS", "DRC Improvements"));
                common.Insert(6, ("DRC_VACANT_LAND", "DRC Vacant Land"));
                common.Insert(7, ("DRC_MARKET_VALUE", "DRC Market Value / Demolition"));
                common.Insert(8, ("CALCULATIONS", "Calculations"));

                return common;
            }

            common.Insert(4, ("PRIMARY_ATTRIBUTES", "Primary Attributes"));
            common.Insert(5, ("SECONDARY_ATTRIBUTES", "Secondary Attributes"));
            common.Insert(6, ("CALCULATIONS", "Calculations"));

            return common;
        }
        private async Task<AttributeSubmissionViewModel?> BuildSubmittedAttributeViewModelAsync(long attrId)
        {
            var info = await _context.AttrPropertyInfo
                .AsNoTracking()
                .Include(x => x.PropertyDetails)
                    .ThenInclude(x => x!.ValuationDetails)
                .Include(x => x.PropertyDetails)
                    .ThenInclude(x => x!.Calculations)
                .Include(x => x.PropertyDetails)
                    .ThenInclude(x => x!.Access)
                .FirstOrDefaultAsync(x => x.Attr_ID == attrId);

            if (info?.PropertyDetails == null)
                return null;

            var property = info.PropertyDetails;
            var propertyDetailsId = property.Id;

            var valuation = property.ValuationDetails;
            var calculations = property.Calculations;
            var access = property.Access;

            var contacts = await _context.AttrContactInfo
                .AsNoTracking()
                .Where(x => x.PropertyDetailsId == propertyDetailsId)
                .ToListAsync();

            var primary = await _context.AttrPrimaryAttributes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PropertyDetailsId == propertyDetailsId);

            var secondary = await _context.AttrSecondaryAttributes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PropertyDetailsId == propertyDetailsId);

            var businessBuildings = await _context.AttrBusinessBuildings
                .AsNoTracking()
                .Where(x => x.PropertyDetailsId == propertyDetailsId)
                .ToListAsync();

            var businessSections = await _context.AttrBusinessSections
                .AsNoTracking()
                .Where(x => x.PropertyDetailsId == propertyDetailsId)
                .ToListAsync();

            var businessGeneral = await _context.AttrBusinessGeneral
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PropertyDetailsId == propertyDetailsId);

            var drcBuildings = await _context.AttrDrcBuildings
                .AsNoTracking()
                .Where(x => x.PropertyDetailsId == propertyDetailsId)
                .ToListAsync();

            var drcImprovements = await _context.AttrDrcImprovements
                .AsNoTracking()
                .Where(x => x.PropertyDetailsId == propertyDetailsId)
                .ToListAsync();

            var drcVacantLands = await _context.AttrDrcVacantLand
                .AsNoTracking()
                .Where(x => x.PropertyDetailsId == propertyDetailsId)
                .ToListAsync();

            var drcMarketValue = await _context.AttrDrcMarketValueDemolition
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PropertyDetailsId == propertyDetailsId);

            var declaration = await _context.AttrDeclarations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Attr_ID == attrId);

            return new AttributeSubmissionViewModel
            {
                AttrId = info.Attr_ID,
                AttrNo = info.Attr_No,
                FormType = property.FormType ?? info.Property_Type,
                ClientComment = info.ClientComment,

                PropertyDetails = new AttributePropertyDetailsVm
                {
                    PropertyId = info.Property_id,
                    PremiseId = info.Premise_id,
                    UnitKey = info.Unit_key,
                    ValuationKey = info.Valuation_Key,
                    Sector = info.Sector,
                    RollType = info.RollType,
                    RollDescription = info.RollDescription,

                    HArea = property.HArea,
                    CollectionBlock = property.CollectionBlock,
                    DataController = property.DataController,
                    DataCollector = property.DataCollector,
                    SGNumber = property.SGNumber,
                    Centroid = property.Centroid,
                    Erf = property.Erf,
                    Extent = property.Extent,
                    SectionalTitle = property.SectionalTitle,
                    LandUseFinancials = property.LandUseFinancials,
                    Municipality = property.Municipality,
                    Ward = property.Ward,
                    Township = property.Township,
                    Zoning = property.Zoning,
                    Sources = property.Sources,
                    Address = property.Address,
                    PropertyDesc = info.Property_Desc
                },

                ValuationDetails = new AttributeValuationDetailsVm
                {
                    ValuationCategoryOnRoll = valuation?.ValuationCategoryOnRoll,
                    ActualUse = valuation?.ActualUse,
                    IsMixedUse = valuation?.IsMixedUse ?? false,
                    AlternateUsages = valuation?.AlternateUsages,
                    OwnersTitleDeeds = valuation?.OwnersTitleDeeds,
                    OwnersFinancials = valuation?.OwnersFinancials
                },

                Access = new AttributeAccessVm
                {
                    AccessType = access?.AccessType,
                    PermissionStatus = access?.PermissionStatus,
                    Comments = access?.Comments
                },

                ContactInfos = contacts.Select(c => new AttributeContactInfoVm
                {
                    ContactType = c.ContactType,
                    IsCompany = c.IsCompany,
                    CompanyName = c.CompanyName,
                    CompanyRegistrationNumber = c.CompanyRegistrationNumber,
                    FirstNames = c.FirstNames,
                    LastName = c.LastName,
                    MaidenName = c.MaidenName,
                    IDNumber = c.IDNumber,
                    DateOfBirth = c.DateOfBirth,
                    Gender = c.Gender,
                    MaritalStatus = c.MaritalStatus,
                    Citizenship = c.Citizenship,
                    PhysicalAddress = c.PhysicalAddress,
                    PostalAddress = c.PostalAddress,
                    Email = c.Email,
                    HomePhoneNo = c.HomePhoneNo,
                    WorkPhoneNo = c.WorkPhoneNo,
                    CellNo = c.CellNo,
                    FaxNo = c.FaxNo,
                    Interviewed = c.Interviewed.HasValue ? (c.Interviewed.Value ? "Yes" : "No") : null,
                    Comments = c.Comments
                }).ToList(),

                PrimaryAttributes = new AttributePrimaryAttributesVm
                {
                    Tla1 = primary?.Tla1,
                    Tla2 = primary?.Tla2,
                    Tla3 = primary?.Tla3,
                    Garage = primary?.Garage,
                    CarportCp = primary?.CarportCp,
                    GrannyFlatGf = primary?.GrannyFlatGf,
                    StaffQuartersSq = primary?.StaffQuartersSq,
                    Storage = primary?.Storage,
                    AdjustmentFactor = primary?.AdjustmentFactor,
                    STMain = primary?.STMain
                },

                SecondaryAttributes = new AttributeSecondaryAttributesVm
                {
                    Storeys = secondary?.Storeys.ToString(),
                    Security = secondary?.Security,
                    Noise = secondary?.Noise,
                    Topography = secondary?.Topography,
                    Quality = secondary?.Quality,
                    Condition = secondary?.Condition,
                    SwimmingPool = secondary?.SwimmingPool,
                    TennisCourt = secondary?.TennisCourt,
                    STCondition = secondary?.STCondition.ToString(),
                    STFloor = secondary?.STFloor.ToString()
                },

                Calculations = new AttributeCalculationsVm
                {
                    //CalcUpdateTla = calculations?.CalcUpdateTla,
                    CalcUpdateTla = decimal.TryParse(calculations?.CalcUpdateTla, out var tla)
    ? tla
    : null,
                    Tla = calculations?.Tla,
                    // CalcUpdateWgba = calculations?.CalcUpdateWgba,
                    CalcUpdateWgba = decimal.TryParse(calculations?.CalcUpdateWgba, out var Wgba)
    ? Wgba
    : null,
                    AdjustedWgba = calculations?.AdjustedWgba,
                    TotalValueNonRes = calculations?.TotalValueNonRes,
                    TotalValueUnutilisedLand = calculations?.TotalValueUnutilisedLand,
                    DRCFinalValue = calculations?.DRCFinalValue,
                    CalculationStatus = calculations?.CalculationStatus
                },

                BusinessBuildings = businessBuildings.Select(b => new AttributeBusinessBuildingVm
                {
                    BuildingNr = b.BuildingNr,
                    Quality = b.Quality,
                    Condition = b.Condition,
                    YearBuilt = b.YearBuilt,
                    Storeys = b.Storeys,
                    GBA = b.GBA,
                    Depreciation = b.Depreciation,
                    Cost = b.Cost,
                    DRC = b.DRC
                }).ToList(),

                BusinessSections = businessSections.Select(s => new AttributeBusinessSectionVm
                {
                    BuildingNr = s.BuildingNr,
                    Usage = s.Usage,
                    MarketGroup = s.MarketGroup,
                    Quality = s.Quality,
                    GBA = s.GBA,
                    NLA = s.NLA,
                    CostRate = s.CostRate,
                    Cost = s.Cost,
                    Rental = s.Rental,
                    Vac = s.Vac,
                    Exp = s.Exp,
                    Cap = s.Cap,
                    Gross = s.Gross,
                    Normalised = s.Normalised,
                    Nett = s.Nett,
                    Value = s.Value
                }).ToList(),

                BusinessGeneral = new AttributeBusinessGeneralVm
                {
                    UnutilisedLandExtent = businessGeneral?.UnutilisedLandExtent,
                    UnutilisedLandRate = businessGeneral?.UnutilisedLandRate
                },

                DrcBuildings = drcBuildings.Select(b => new AttributeDrcBuildingVm
                {
                    BuildingDescription = b.BuildingDescription,
                    Quality = b.Quality,
                    GrossBuildingArea = b.GrossBuildingArea,
                    Condition = b.Condition,
                    DepreciationPercentage = b.DepreciationPercentage,
                    RatePerSQM = b.RatePerSQM,
                    DepreciatedRate = b.DepreciatedRate,
                    ReplacementCost = b.ReplacementCost
                }).ToList(),

                DrcImprovements = drcImprovements.Select(i => new AttributeDrcImprovementVm
                {
                    ImprovementDescription = i.ImprovementDescription,
                    Quality = i.Quality,
                    AreaUnit = i.AreaUnit,
                    Condition = i.Condition,
                    DepreciationPercentage = i.DepreciationPercentage,
                    RatePerSQM = i.RatePerSQM,
                    DepreciatedRate = i.DepreciatedRate,
                    ReplacementCost = i.ReplacementCost
                }).ToList(),

                DrcVacantLands = drcVacantLands.Select(v => new AttributeDrcVacantLandVm
                {
                    Region = v.Region,
                    MinRatePerSQM = v.MinRatePerSQM,
                    MidRatePerSQM = v.MidRatePerSQM,
                    MaxRatePerSQM = v.MaxRatePerSQM,
                    Area = v.Area,
                    Rate = v.Rate,
                    VacantLandCost = v.VacantLandCost
                }).ToList(),

                DrcMarketValueDemolition = new AttributeDrcMarketValueDemolitionVm
                {
                    DemolitionRate = drcMarketValue?.DemolitionRate,
                    MarketValue = drcMarketValue?.MarketValue,
                    MarketValueAfterDemolition = drcMarketValue?.MarketValueAfterDemolition
                },

                Declaration = new AttributeDeclarationVm
                {
                    DeclarationAccepted = declaration?.Declaration_Accepted ?? false,
                    DeclarationText = declaration?.Declaration_Text,
                    SignatureName = declaration?.Signature_Name,
                    SignaturePicture = declaration?.Signature_Picture
                }
            };
        }

        public async Task SaveSectionReviewAsync(
    SaveSectionReviewVm vm,
    AivsCurrentUserVm currentUser)
        {
            if (currentUser.UserId == null)
                throw new InvalidOperationException("Your AIVS user could not be verified.");

            if (vm.SectionId <= 0)
                throw new InvalidOperationException("Invalid review section.");

            if (string.IsNullOrWhiteSpace(vm.SectionDecision))
                throw new InvalidOperationException("Please select a section decision.");

            var allowedDecisions = new[]
            {
        "Accepted",
        "Needs correction",
        "Not applicable",
        "Requires inspection"
    };

            if (!allowedDecisions.Contains(vm.SectionDecision.Trim(), StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Invalid section decision selected.");

            var section = await _context.AttrValuerReviewSections
                .FirstOrDefaultAsync(x =>
                    x.Id == vm.SectionId &&
                    x.ReviewId == vm.ReviewId);

            if (section == null)
                throw new InvalidOperationException("Review section could not be found.");

            var review = await _context.AttrValuerReviews
                .FirstOrDefaultAsync(x => x.Id == section.ReviewId);

            if (review == null)
                throw new InvalidOperationException("Review could not be found.");

            if (review.ReviewerUserId != currentUser.UserId.Value)
                throw new InvalidOperationException("This review is not assigned to you.");

            await EnsureCurrentAssignmentAsync(section.Attr_ID, currentUser);

            var now = DateTime.Now;

            section.SectionDecision = vm.SectionDecision.Trim();
            section.SectionComment = vm.SectionComment?.Trim();
            section.RequiresCorrection = vm.RequiresCorrection;
            section.RequiresInspection = vm.RequiresInspection;
            section.ReviewedByUserId = currentUser.UserId.Value;
            section.ReviewedByName = currentUser.FullName;
            section.ReviewedAt = now;
            section.UpdatedBy = currentUser.Username ?? currentUser.WindowsUsername;
            section.UpdatedDate = now;

            review.UpdatedBy = currentUser.Username ?? currentUser.WindowsUsername;
            review.UpdatedDate = now;

            var reviewSections = await _context.AttrValuerReviewSections
                .Where(x => x.ReviewId == review.Id)
                .ToListAsync();

            review.RequiresInspection = reviewSections.Any(x =>
                x.RequiresInspection || x.SectionDecision == "Requires inspection");

            review.ReturnToClient = reviewSections.Any(x =>
                x.RequiresCorrection || x.SectionDecision == "Needs correction");

            await _context.SaveChangesAsync();
        }

        public async Task SubmitFinalReviewAsync(
       SubmitFinalReviewVm vm,
       AivsCurrentUserVm currentUser)
        {
            if (currentUser.UserId == null)
                throw new InvalidOperationException("Your AIVS user could not be verified.");

            if (string.IsNullOrWhiteSpace(vm.FinalDecision))
                throw new InvalidOperationException("Please select a final decision.");

            if (string.IsNullOrWhiteSpace(vm.FinalComment))
                throw new InvalidOperationException("Please enter a final comment.");

            var allowedDecisions = new[]
 {
    "SubmitToOvvio",
    "ReturnToClient"
};

            if (!allowedDecisions.Contains(vm.FinalDecision))
                throw new InvalidOperationException("Invalid final decision.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var review = await _context.AttrValuerReviews
                .FirstOrDefaultAsync(x => x.Id == vm.ReviewId && x.Attr_ID == vm.AttrId);

            if (review == null)
                throw new InvalidOperationException("Review could not be found.");

            if (review.ReviewerUserId != currentUser.UserId.Value)
                throw new InvalidOperationException("This review is not assigned to you.");

            await EnsureCurrentAssignmentAsync(vm.AttrId, currentUser);

            var item = await _context.AttrPropertyInfo
                .Include(x => x.PropertyDetails)
                .FirstOrDefaultAsync(x => x.Attr_ID == vm.AttrId && x.IsActive == true);

            if (item == null)
                throw new InvalidOperationException("Attribute submission could not be found.");

            if (string.Equals(item.Attr_Status, "ReadyForOvvioExtract", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This submission is already accepted and ready for OVVIO extract.");
            var contact = item.PropertyDetails == null
                ? null
                : await _context.AttrContactInfo
                    .AsNoTracking()
                    .Where(x => x.PropertyDetailsId == item.PropertyDetails.Id)
                    .OrderBy(x => x.Id)
                    .FirstOrDefaultAsync();

            var sections = await _context.AttrValuerReviewSections
                .Where(x => x.ReviewId == vm.ReviewId)
                .ToListAsync();

            var requiredSectionCodes = GetRequiredReviewSectionCodes(item.Property_Type);

            var requiredSections = sections
                .Where(x => requiredSectionCodes.Contains(x.SectionCode))
                .ToList();

            var requiredCorrectionExists = requiredSections.Any(x =>
                x.RequiresCorrection ||
                x.SectionDecision == "Needs correction");
            requiredCorrectionExists = requiredCorrectionExists || await _context.AttrValuerReviewFieldCorrections
                .AnyAsync(x => x.ReviewId == vm.ReviewId && x.IsActive);

            var inspectionRequiredExists = sections.Any(x =>
                x.RequiresInspection ||
                x.SectionDecision == "Requires inspection");

            if (vm.FinalDecision == "SubmitToOvvio")
            {
                if (requiredCorrectionExists)
                    throw new InvalidOperationException("You cannot submit to OVVIO. Required fields are still selected for correction.");

                if (inspectionRequiredExists)
                    throw new InvalidOperationException("You cannot submit to OVVIO. A physical inspection is required.");

                // The single bottom OVVIO action accepts every required section.
                // This replaces the removed Accept button on each section.
                foreach (var section in requiredSections)
                {
                    section.SectionDecision = "Accepted";
                    section.RequiresCorrection = false;
                    section.RequiresInspection = false;
                    section.UpdatedBy = currentUser.Username
                        ?? currentUser.WindowsUsername
                        ?? currentUser.FullName
                        ?? "AIVS";
                    section.UpdatedDate = DateTime.Now;
                }
            }

            var now = DateTime.Now;
            var oldStatus = item.Attr_Status;
            var finalComment = vm.FinalComment.Trim();

            var currentUserName = currentUser.Username
                ?? currentUser.WindowsUsername
                ?? currentUser.FullName
                ?? "AIVS";

            var currentUserId = currentUser.UserId.Value.ToString();

            review.FinalDecision = vm.FinalDecision;
            review.FinalComment = finalComment;
            review.CompletedAt = now;
            review.ReviewerUserId = currentUser.UserId.Value;
            review.ReviewerName = currentUser.FullName;
            review.UpdatedBy = currentUserName;
            review.UpdatedDate = now;

            switch (vm.FinalDecision)
            {
                case "SubmitToOvvio":
                    var returnedQa = await _context.AttrSectorManagerQaReviews
                        .AsNoTracking()
                        .Where(x =>
                            x.Attr_ID == item.Attr_ID &&
                            x.QaStatus == "ReturnedToValuer")
                        .OrderByDescending(x => x.QaCompletedAt)
                        .ThenByDescending(x => x.Id)
                        .FirstOrDefaultAsync();

                    var selectedForSectorQa = returnedQa != null || ShouldSelectForSectorManagerQa();

                    review.ReviewStatus = selectedForSectorQa
                        ? "SubmittedForSectorManagerQa"
                        : "Completed";

                    review.ReadyForOvvioExtract = !selectedForSectorQa;
                    review.ReturnToClient = false;
                    review.RequiresInspection = false;

                    item.Attr_Status = selectedForSectorQa
                        ? "SectorManagerQa"
                        : "ReadyForOvvioExtract";

                    item.Valuer = currentUser.FullName;
                    item.ValuerUserId = currentUserId;
                    item.ValuerComment = finalComment;
                    item.ValuerDecision = "Accepted";
                    item.RejectionReason = null;
                    item.ValuerDecisionDateTime = now;

                    item.ReadyForOvvioExtract = !selectedForSectorQa;
                    item.OvvioExtractStatus = selectedForSectorQa ? null : "Pending";
                    item.OvvioExtractBatchNo = null;
                    item.OvvioExtractDateTime = null;
                    item.OvvioExtractedBy = null;
                    item.OvvioExtractError = null;

                    item.Physical_Inspection_Required = false;

                    if (selectedForSectorQa)
                    {
                        var weekStart = StartOfWeek(now);
                        var weekEnd = weekStart.AddDays(6);

                        _context.AttrSectorManagerQaReviews.Add(new AttrSectorManagerQaReview
                        {
                            Attr_ID = item.Attr_ID,
                            Attr_No = item.Attr_No,
                            ValuerReviewId = review.Id,
                            Sector = item.RoutedSector,
                            QaWeekStartDate = weekStart.Date,
                            QaWeekEndDate = weekEnd.Date,
                            IsRandomlySelected = true,
                            SelectionReason = returnedQa == null
                                ? $"{Math.Clamp(_sectorManagerQaSettings.Value.WeeklySamplePercent, 0, 100)}% weekly random Sector Manager QA sample"
                                : "Resubmitted after Sector Manager QA return",
                            ValuerUserId = currentUser.UserId.Value,
                            ValuerName = currentUser.FullName,
                            ValuerSubmittedAt = now,
                            QaStatus = returnedQa?.SectorManagerUserId == null ? "Pending" : "InProgress",
                            SectorManagerUserId = returnedQa?.SectorManagerUserId,
                            SectorManagerUsername = returnedQa?.SectorManagerUsername,
                            SectorManagerName = returnedQa?.SectorManagerName,
                            SectorManagerEmail = returnedQa?.SectorManagerEmail,
                            QaStartedAt = returnedQa?.SectorManagerUserId == null ? null : now,
                            ReviewedPdfPathBeforeQa = item.ValuerEvidencePath,
                            CreatedBy = currentUserName,
                            CreatedDate = now
                        });
                    }

                    break;

                case "ReturnToClient":
                    review.ReviewStatus = "ReturnedToClient";
                    review.ReadyForOvvioExtract = false;
                    review.ReturnToClient = true;
                    review.RequiresInspection = false;

                    item.Attr_Status = "ReturnedToClient";

                    item.Valuer = currentUser.FullName;
                    item.ValuerUserId = currentUserId;
                    item.ValuerComment = finalComment;
                    item.ValuerDecision = "ReturnedToClient";
                    item.RejectionReason = null;
                    item.ValuerDecisionDateTime = now;

                    item.ReadyForOvvioExtract = false;
                    item.OvvioExtractStatus = null;
                    item.OvvioExtractBatchNo = null;
                    item.OvvioExtractDateTime = null;
                    item.OvvioExtractedBy = null;
                    item.OvvioExtractError = null;

                    item.RevisionRequired = true;
                    item.RevisionRequestedBy = currentUser.FullName;
                    item.RevisionRequestedDateTime = now;
                    item.RevisionReason = finalComment;
                    break;


            }

            item.UpdatedBy = currentUserName;
            item.UpdatedDate = now;

            var auditAction = vm.FinalDecision switch
            {
                "SubmitToOvvio" when item.Attr_Status == "SectorManagerQa"
                    => "Submitted for Sector Manager QA",

                "SubmitToOvvio" when item.Attr_Status == "ReadyForOvvioExtract"
                    => "Submitted to OVVIO Extract",

                "ReturnToClient"
                    => "Returned to Client",

                _ => "Final Review Decision"
            };

            _context.AttrPropertyInfoAuditTrail.Add(new AttrPropertyInfoAuditTrail
            {
                Attr_ID = item.Attr_ID,
                Attr_No = item.Attr_No,
                Action = auditAction,
                OldStatus = oldStatus,
                NewStatus = item.Attr_Status,
                ActionByUserId = currentUserId,
                ActionByName = currentUser.FullName,
                ActionRole = currentUser.Role ?? "Valuer",
                Comment = finalComment,
                ActionDateTime = now
            });

            var completedLock = await _context.AttrValuerReviewLocks
                .FirstOrDefaultAsync(x => x.ReviewId == review.Id);
            if (completedLock != null)
            {
                completedLock.IsActive = false;
                completedLock.LastActivityAt = now;
                completedLock.ExpiresAt = now;
            }

            // First save final decision so the reviewed PDF pulls the latest review/status values.
            await _context.SaveChangesAsync();

            var reviewedPdfPath = await _valuerReviewPdfService
     .GenerateReviewedFormPdfAsync(review.Id, currentUser);

            item.ValuerEvidencePath = reviewedPdfPath;
            item.UpdatedBy = currentUserName;
            item.UpdatedDate = DateTime.Now;

            var pendingQa = await _context.AttrSectorManagerQaReviews
                .FirstOrDefaultAsync(x =>
                    x.Attr_ID == item.Attr_ID &&
                    x.ValuerReviewId == review.Id &&
                    (x.QaStatus == "Pending" || x.QaStatus == "InProgress"));

            if (pendingQa != null)
            {
                pendingQa.ReviewedPdfPathBeforeQa = reviewedPdfPath;
                pendingQa.UpdatedBy = currentUserName;
                pendingQa.UpdatedDate = DateTime.Now;
            }

            _context.AttrPropertyInfoAuditTrail.Add(new AttrPropertyInfoAuditTrail
            {
                Attr_ID = item.Attr_ID,
                Attr_No = item.Attr_No,
                Action = "Reviewed Form PDF Generated",
                OldStatus = item.Attr_Status,
                NewStatus = item.Attr_Status,
                ActionByUserId = currentUserId,
                ActionByName = currentUser.FullName,
                ActionRole = currentUser.Role ?? "Valuer",
                Comment = $"Reviewed form PDF generated: {Path.GetFileName(reviewedPdfPath)}",
                ActionDateTime = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var clientName = contact == null ? "Client" : BuildClientName(contact);
            var attrNo = item.Attr_No ?? "-";
            var propertyDescription = item.Property_Desc;
            var finalStatus = item.Attr_Status;

            if (contact != null && !string.IsNullOrWhiteSpace(contact.Email))
            {
                if (vm.FinalDecision == "SubmitToOvvio" &&
                    string.Equals(finalStatus, "ReadyForOvvioExtract", StringComparison.OrdinalIgnoreCase))
                {
                    await _emailService.SendAcceptedEmailAsync(
                        contact.Email,
                        clientName,
                        attrNo,
                        propertyDescription,
                        finalComment);
                }
                else if (vm.FinalDecision == "ReturnToClient")
                {
                    await _emailService.SendReturnedToClientEmailAsync(
                        contact.Email,
                        clientName,
                        attrNo,
                        propertyDescription,
                        finalComment);
                }
            }

            if (vm.FinalDecision == "SubmitToOvvio" &&
                string.Equals(finalStatus, "ReadyForOvvioExtract", StringComparison.OrdinalIgnoreCase))
            {
                await _notificationService.CreateNotificationAsync(
                    currentUser.UserId,
                    currentUser.Username ?? currentUser.WindowsUsername,
                    currentUser.Role,
                    "Ready for OVVIO extract",
                    $"{item.Attr_No} has been marked as ready for OVVIO extract.",
                    "ReadyForOvvioExtract",
                    item.Attr_ID,
                    item.Attr_No,
                    null,
                    currentUser.FullName);
            }
            else if (vm.FinalDecision == "SubmitToOvvio" &&
                     string.Equals(finalStatus, "SectorManagerQa", StringComparison.OrdinalIgnoreCase))
            {
                await _notificationService.CreateNotificationAsync(
                    currentUser.UserId,
                    currentUser.Username ?? currentUser.WindowsUsername,
                    currentUser.Role,
                    "Selected for Sector Manager QA",
                    $"{item.Attr_No} has been selected for weekly Sector Manager QA before OVVIO.",
                    "SectorManagerQa",
                    item.Attr_ID,
                    item.Attr_No,
                    null,
                    currentUser.FullName);
            }
            else if (vm.FinalDecision == "ReturnToClient")
            {
                await _notificationService.CreateNotificationAsync(
                    currentUser.UserId,
                    currentUser.Username ?? currentUser.WindowsUsername,
                    currentUser.Role,
                    "Returned to client",
                    $"{item.Attr_No} has been returned to the client for correction.",
                    "ReturnedToClient",
                    item.Attr_ID,
                    item.Attr_No,
                    null,
                    currentUser.FullName);
            }
        }
        private static HashSet<string> GetRequiredReviewSectionCodes(
     string? formType)
        {
            var required = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
    {
        "VALUATION_DETAILS",
        "DECLARATION"
    };

            formType = formType?.Trim();

            if (formType == "BusinessCommercial")
            {
                required.Add("BUSINESS_BUILDINGS");
                required.Add("BUSINESS_SECTIONS");
                required.Add("BUSINESS_GENERAL");

                return required;
            }

            if (formType == "DRCMethod")
            {
                required.Add("DRC_BUILDINGS");
                required.Add("DRC_IMPROVEMENTS");
                required.Add("DRC_VACANT_LAND");

                return required;
            }

            required.Add("PRIMARY_ATTRIBUTES");
            required.Add("SECONDARY_ATTRIBUTES");

            return required;
        }
        public async Task ScheduleInspectionAsync(
    ScheduleInspectionVm vm,
    AivsCurrentUserVm currentUser)
        {
            if (currentUser.UserId == null)
                throw new InvalidOperationException("Your AIVS user could not be verified.");

            if (vm.Option1 == default || vm.Option2 == default || vm.Option3 == default)
                throw new InvalidOperationException("Please provide all three inspection date options.");

            var options = new[] { vm.Option1, vm.Option2, vm.Option3 };

            if (options.Any(x => x <= DateTime.Now))
                throw new InvalidOperationException("Inspection options must be future dates.");

            if (options.Distinct().Count() != 3)
                throw new InvalidOperationException("The three inspection options must be different.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var review = await _context.AttrValuerReviews
                .FirstOrDefaultAsync(x => x.Id == vm.ReviewId && x.Attr_ID == vm.AttrId);

            if (review == null)
                throw new InvalidOperationException("Review could not be found.");

            if (review.ReviewerUserId != currentUser.UserId.Value)
                throw new InvalidOperationException("This review is not assigned to you.");

            var item = await _context.AttrPropertyInfo
                .Include(x => x.PropertyDetails)
                .FirstOrDefaultAsync(x => x.Attr_ID == vm.AttrId && x.IsActive == true);

            if (item == null)
                throw new InvalidOperationException("Attribute submission could not be found.");

            await EnsureCurrentAssignmentAsync(vm.AttrId, currentUser);

            var existingOpenRequest = await _context.AttrInspectionRequests
      .FirstOrDefaultAsync(x =>
          x.Attr_ID == vm.AttrId &&
          (
              x.Status == "PendingClientResponse" ||
              x.Status == "Confirmed" ||
              x.Status == "InspectionDetailsSent"
          ));

            if (existingOpenRequest != null)
                throw new InvalidOperationException("There is already an active physical inspection request for this submission.");



            var contact = await _context.AttrContactInfo
                .AsNoTracking()
                .Where(x => x.PropertyDetailsId == item.PropertyDetails!.Id)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            if (contact == null || string.IsNullOrWhiteSpace(contact.Email))
                throw new InvalidOperationException("Client email could not be found. The physical inspection request cannot be sent.");


            if (string.IsNullOrWhiteSpace(contact.Email))
                throw new InvalidOperationException("Client email address could not be found.");

            var now = DateTime.Now;
            var oldStatus = item.Attr_Status;

            var request = new AttrInspectionRequest
            {
                Attr_ID = item.Attr_ID,
                Attr_No = item.Attr_No,
                ReviewId = review.Id,

                RequestedByUserId = currentUser.UserId.Value,
                RequestedByUsername = currentUser.Username ?? currentUser.WindowsUsername,
                RequestedByName = currentUser.FullName,
                RequestedByEmail = currentUser.Email,

                ClientName = BuildClientName(contact),
                ClientEmail = contact.Email,
                ClientCellNo = contact.CellNo,

                Status = "PendingClientResponse",
                RequestComment = vm.RequestComment?.Trim(),

                EmailToken = Guid.NewGuid(),
                EmailTokenExpiresAt = now.AddDays(14),

                CreatedBy = currentUser.Username ?? currentUser.WindowsUsername,
                CreatedDate = now
            };

            _context.AttrInspectionRequests.Add(request);
            await _context.SaveChangesAsync();

            for (var i = 0; i < options.Length; i++)
            {
                _context.AttrInspectionRequestSlots.Add(new AttrInspectionRequestSlot
                {
                    InspectionRequestId = request.Id,
                    Attr_ID = item.Attr_ID,
                    Attr_No = item.Attr_No,
                    SlotNo = i + 1,
                    ProposedDateTime = options[i],
                    SlotStatus = "Offered",
                    CreatedBy = currentUser.Username ?? currentUser.WindowsUsername,
                    CreatedDate = now
                });
            }

            review.RequiresInspection = true;
            review.ReviewStatus = "InspectionRequired";
            review.UpdatedBy = currentUser.Username ?? currentUser.WindowsUsername;
            review.UpdatedDate = now;

            item.Attr_Status = "InspectionRequired";
            item.UpdatedBy = currentUser.Username ?? currentUser.WindowsUsername;
            item.UpdatedDate = now;

            _context.AttrPropertyInfoAuditTrail.Add(new AttrPropertyInfoAuditTrail
            {
                Attr_ID = item.Attr_ID,
                Attr_No = item.Attr_No,
                Action = "Physical Inspection Requested",
                OldStatus = oldStatus,
                NewStatus = "InspectionRequired",
                ActionByUserId = currentUser.Username ?? currentUser.WindowsUsername,
                ActionByName = currentUser.FullName,
                ActionRole = currentUser.Role ?? "Valuer",
                Comment = vm.RequestComment,
                ActionDateTime = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();


            // Email sending will be added after this works.
            await _emailService.SendInspectionDateOptionsEmailAsync(
    contact.Email,
    BuildClientName(contact),
    item.Attr_No ?? "-",
    item.Property_Desc,
    options.OrderBy(x => x).ToList(),
    vm.RequestComment);

            await _notificationService.CreateNotificationAsync(
                currentUser.UserId,
                currentUser.Username ?? currentUser.WindowsUsername,
                currentUser.Role,
                "Inspection date options sent",
                $"Three inspection date options were sent to the client for {item.Attr_No}.",
                "InspectionDateOptionsSent",
                item.Attr_ID,
                item.Attr_No,
                request.Id,
                currentUser.FullName);

        }
        private static string BuildClientName(dynamic contact)
        {
            if (contact.IsCompany == true && !string.IsNullOrWhiteSpace(contact.CompanyName))
                return contact.CompanyName.Trim();

            return string.Join(" ",
                new[]
                {
            contact.FirstNames?.Trim(),
            contact.LastName?.Trim()
                }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        public async Task SendInspectionDetailsToClientAsync(
     long inspectionRequestId,
     AivsCurrentUserVm currentUser)
        {
            if (currentUser.UserId == null)
                throw new InvalidOperationException("Current AIVS user could not be resolved.");

            if (string.IsNullOrWhiteSpace(currentUser.SapNumber))
                throw new InvalidOperationException("Current user SAP number could not be resolved. Please check User Management mapping.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var request = await _context.AttrInspectionRequests
                .Include(x => x.Slots)
                .FirstOrDefaultAsync(x =>
                    x.Id == inspectionRequestId &&
                    x.Status == "Confirmed");

            if (request == null)
                throw new InvalidOperationException("Confirmed inspection request could not be found.");

            if (request.RequestedByUserId != currentUser.UserId.Value)
                throw new InvalidOperationException("You can only send inspection details for your own inspection requests.");

            if (request.ConfirmedDateTime == null)
                throw new InvalidOperationException("Inspection date has not been confirmed yet.");

            if (request.ValuerDetailsSent)
                throw new InvalidOperationException("Inspection details have already been sent to the client.");

            var property = await _context.AttrPropertyInfo
                .Include(x => x.PropertyDetails)
                .FirstOrDefaultAsync(x =>
                    x.Attr_ID == request.Attr_ID &&
                    x.IsActive == true);

            if (property == null)
                throw new InvalidOperationException("Attribute property could not be found.");

            await EnsureCurrentAssignmentAsync(property.Attr_ID, currentUser);

            if (property.PropertyDetails == null)
                throw new InvalidOperationException("Property details could not be found.");

            var contact = await _context.AttrContactInfo
                .AsNoTracking()
                .Where(x => x.PropertyDetailsId == property.PropertyDetails.Id)
                .OrderBy(x => x.Id)
                .FirstOrDefaultAsync();

            if (contact == null)
                throw new InvalidOperationException("Client contact details could not be found.");

            if (string.IsNullOrWhiteSpace(contact.Email))
                throw new InvalidOperationException("Client email address could not be found.");

            var sapNumber = currentUser.SapNumber.Trim();

            var valuerDetails = await _context.AttrValuerInspectionDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.SapNumber == sapNumber &&
                    x.IsActive == true);

            if (valuerDetails == null)
                throw new InvalidOperationException("Your valuer inspection profile could not be found. Please create your valuer profile first.");

            var now = DateTime.Now;
            var pin = GenerateInspectionPin();

            var pinValidFrom = request.ConfirmedDateTime.Value.AddMinutes(-30);
            var pinValidUntil = request.ConfirmedDateTime.Value.AddHours(2);

            request.Status = "InspectionDetailsSent";
            request.InspectionPin = pin;
            request.InspectionPinGeneratedAt = now;

            request.PinValidFrom = pinValidFrom;
            request.PinValidUntil = pinValidUntil;
            request.PinUsedAt = null;
            request.PinUsedByEmail = null;
            request.PinUsedIpAddress = null;
            request.PinUsedUserAgent = null;
            request.PinVerifiedAt = null;
            request.PinVerifiedByEmail = null;
            request.PinFailedAttempts = 0;

            request.ValuerDetailsSent = true;
            request.ValuerDetailsSentAt = now;
            request.ValuerDetailsSentByUserId = currentUser.UserId.Value;
            request.ValuerDetailsSentByName = currentUser.FullName;
            request.ValuerSapNumber = sapNumber;

            request.UpdatedBy = currentUser.Username ?? currentUser.WindowsUsername ?? currentUser.FullName;
            request.UpdatedDate = now;

            var oldStatus = property.Attr_Status;

            property.Attr_Status = "InspectionDetailsSent";
            property.Physical_Inspection_Status = "InspectionDetailsSent";
            property.Inspection_Valuer = valuerDetails.ValuerName;
            property.Inspection_ValuerUserId = currentUser.UserId.Value.ToString();
            property.Digital_Valuer_ID = pin;
            property.Digital_Valuer_ID_GeneratedDateTime = now;
            property.UpdatedBy = currentUser.Username ?? currentUser.WindowsUsername ?? currentUser.FullName;
            property.UpdatedDate = now;

            _context.AttrPropertyInfoAuditTrail.Add(new AttrPropertyInfoAuditTrail
            {
                Attr_ID = property.Attr_ID,
                Attr_No = property.Attr_No,
                Action = "Inspection Details Sent",
                OldStatus = oldStatus,
                NewStatus = "InspectionDetailsSent",
                ActionByUserId = currentUser.UserId.Value.ToString(),
                ActionByName = currentUser.FullName,
                ActionRole = currentUser.Role,
                Comment = "Inspection PIN and secure appointment instructions sent to client.",
                ActionDateTime = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            try
            {
                await _emailService.SendInspectionDetailsEmailAsync(
                    contact.Email,
                    BuildClientName(contact),
                    request.Attr_No ?? property.Attr_No ?? "-",
                    property.Property_Desc,
                    request.ConfirmedDateTime.Value,
                    pin,
                    valuerDetails.ValuerName,
                    valuerDetails.EmailAddress,
                    valuerDetails.CellNumber,
                    valuerDetails.VehicleRegistration,
                    valuerDetails.VehicleMake,
                    valuerDetails.VehicleColour,
                    valuerDetails.PhotoFileName);

                await _notificationService.CreateNotificationAsync(
                    currentUser.UserId,
                    currentUser.Username ?? currentUser.WindowsUsername,
                    currentUser.Role,
                    "Inspection details sent",
                    $"Inspection PIN and secure appointment instructions were sent to the client for {property.Attr_No}.",
                    "InspectionDetailsSent",
                    property.Attr_ID,
                    property.Attr_No,
                    request.Id,
                    currentUser.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Inspection details were saved, but email/notification failed for {AttrNo}, request {RequestId}",
                    property.Attr_No,
                    request.Id);
            }
        }

        private static string GenerateInspectionPin()
        {
            return Random.Shared.Next(1000, 10000).ToString();
        }
        private async Task<List<ValuerPhysicalInspectionEvidenceVm>> BuildPhysicalInspectionEvidenceFilesAsync(long attrId)
        {
            return await _context.AttrInspectionEvidence
                .AsNoTracking()
                .Where(x =>
                    x.Attr_ID == attrId &&
                    x.IsActive == true)
                .OrderByDescending(x => x.UploadedAt)
                .Select(x => new ValuerPhysicalInspectionEvidenceVm
                {
                    Id = x.Id,
                    AttrId = x.Attr_ID,
                    AttrNo = x.Attr_No,
                    InspectionRequestId = x.InspectionRequestId,
                    FileName = x.FileName,
                    FilePath = x.FilePath,
                    ContentType = x.ContentType,
                    FileSizeBytes = x.FileSizeBytes,
                    UploadedBySapNumber = x.UploadedBySapNumber,
                    UploadedByName = x.UploadedByName,
                    CaptureSource = x.CaptureSource,
                    EvidenceComment = x.EvidenceComment,
                    UploadedAt = x.UploadedAt
                })
                .ToListAsync();
        }

        private async Task EnsureCurrentAssignmentAsync(
            long attrId,
            AivsCurrentUserVm currentUser)
        {
            if (currentUser.UserId == null)
                throw new InvalidOperationException("Your AIVS user could not be verified.");

            var userId = currentUser.UserId.Value.ToString();
            var isAssigned = await _context.AttrPropertyInfo
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Attr_ID == attrId &&
                    x.IsActive == true &&
                    x.IsWithdrawn != true &&
                    x.Task_Assigned_To_UserId == userId);

            if (!isAssigned)
                throw new InvalidOperationException("This submission is no longer assigned to you. Return to the Review Inbox and refresh the page.");
        }

        private static string FriendlyStatus(string? status)
        {
            return status?.Trim() switch
            {
                "SectorInbox" => "Sector Inbox",
                "Claimed" => "Assigned to Valuer",
                "ValuerReview" => "Under Valuer Review",
                "ReturnedToClient" => "Returned to Client",
                "Resubmitted" => "Client Resubmitted",
                "ReturnedToValuer" => "Returned to Valuer",
                "SectorManagerQa" => "Sector Manager QA",
                "InspectionRequired" => "Inspection Required",
                "InspectionConfirmed" => "Inspection Date Confirmed",
                "InspectionDetailsSent" => "Inspection Details Sent",
                "InspectionCompleted" => "Inspection Completed",
                "InspectionExpired" => "Inspection Expired",
                "ReadyForOvvioExtract" => "Accepted / Ready for OVVIO",
                "OvvioExtracted" => "OVVIO Extracted",
                _ => string.IsNullOrWhiteSpace(status) ? "Unknown" : status
            };
        }

        private bool ShouldSelectForSectorManagerQa()
        {
            var settings = _sectorManagerQaSettings.Value;

            if (!settings.Enabled)
                return false;

            var samplePercent = Math.Clamp(settings.WeeklySamplePercent, 0, 100);

            if (samplePercent >= 100)
                return true;

            if (samplePercent <= 0)
                return false;

            return Random.Shared.Next(1, 101) <= samplePercent;
        }

        private static DateTime StartOfWeek(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.Date.AddDays(-1 * diff);
        }
    }
}
