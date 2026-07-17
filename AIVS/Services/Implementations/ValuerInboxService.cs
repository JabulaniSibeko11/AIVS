using AIVS.Data;
using AIVS.Models.Attributes;
using AIVS.Models.Configuration;
using AIVS.Models.ViewModels.UserManagement;
using AIVS.Models.ViewModels.ValuerInbox;
using AIVS.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace AIVS.Services.Implementations
{
    public class ValuerInboxService : IValuerInboxService
    {
        private readonly AttributesDbContext _context;
        private readonly ILogger<ValuerInboxService> _logger;
        private readonly IEmailService _emailService;
        private readonly AttributeStorageSettings _storageSettings;
        private readonly INotificationService _notificationService;
        public ValuerInboxService(
            AttributesDbContext context,
            ILogger<ValuerInboxService> logger, IEmailService emailService, AttributeStorageSettings storageSettings, INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _storageSettings = storageSettings;
            _notificationService = notificationService;
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
    "InspectionExpired"
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
    "InspectionDetailsSent"
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
                .FirstOrDefaultAsync(x =>
                    x.Attr_ID == attrId &&
                    x.ReviewStatus == "InProgress");

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

            if (item.Attr_Status == "Claimed")
            {
                var oldStatus = item.Attr_Status;

                item.Attr_Status = "ValuerReview";
                item.UpdatedBy = currentUser.Username ?? currentUser.WindowsUsername;
                item.UpdatedDate = DateTime.Now;

                _context.AttrPropertyInfoAuditTrail.Add(new AttrPropertyInfoAuditTrail
                {
                    Attr_ID = item.Attr_ID,
                    Attr_No = item.Attr_No,
                    Action = "Valuer Review Started",
                    OldStatus = oldStatus,
                    NewStatus = "ValuerReview",
                    ActionByUserId = currentUser.Username ?? currentUser.WindowsUsername,
                    ActionByName = currentUser.FullName,
                    ActionRole = currentUser.Role ?? "Valuer",
                    Comment = "Valuer opened the review.",
                    ActionDateTime = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            return await BuildReviewPageAsync(review.Id);
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

        private async Task<ValuerReviewPageVm> BuildReviewPageAsync(long reviewId)
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

            var hasUnreviewedRequired = requiredSections.Any(x =>
                string.IsNullOrWhiteSpace(x.SectionDecision));

            var canSubmitToOvvio =
                !hasRequiredCorrections &&
                !hasInspectionRequired &&
                !hasUnreviewedRequired;

            var submitBlockReason = "";

            if (hasUnreviewedRequired)
            {
                submitBlockReason = "Some required sections have not been reviewed.";
            }
            else if (hasRequiredCorrections)
            {
                submitBlockReason = "Required sections still need correction.";
            }
            else if (hasInspectionRequired)
            {
                submitBlockReason = "A physical inspection is required.";
            }

            var submittedForm = await BuildSubmittedAttributeViewModelAsync(item.Attr_ID);

            var evidenceFiles = await BuildEvidenceFilesAsync(item.Attr_ID);

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
                FinalComment = review.FinalComment,
            };
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
            var common = new List<(string Code, string Name)>
            {
                ("PROPERTY_DETAILS", "Property Details"),
                ("VALUATION_DETAILS", "Valuation Details"),
                ("CONTACT_INFORMATION", "Contact Information"),
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
                .FirstOrDefaultAsync(x => x.Attr_ID == attrId);

            if (info?.PropertyDetails == null)
                return null;

            var property = info.PropertyDetails;
            var propertyDetailsId = property.Id;

            var valuation = property.ValuationDetails;
            var calculations = property.Calculations;

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

                ContactInfos = contacts.Select(c => new AttributeContactInfoVm
                {
                    ContactType = c.ContactType,
                    IsCompany = c.IsCompany,
                    CompanyName = c.CompanyName,
                    CompanyRegistrationNumber = c.CompanyRegistrationNumber,
                    FirstNames = c.FirstNames,
                    LastName = c.LastName,
                    PhysicalAddress = c.PhysicalAddress,
                    PostalAddress = c.PostalAddress,
                    Email = c.Email,
                    HomePhoneNo = c.HomePhoneNo,
                    WorkPhoneNo = c.WorkPhoneNo,
                    CellNo = c.CellNo
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

            if (vm.RequiresInspection || vm.SectionDecision == "Requires inspection")
            {
                review.RequiresInspection = true;
            }

            if (vm.RequiresCorrection || vm.SectionDecision == "Needs correction")
            {
                review.ReturnToClient = true;
            }

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
        "ReturnToClient",
        "Reject"
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

            var item = await _context.AttrPropertyInfo
                .Include(x => x.PropertyDetails)
                .FirstOrDefaultAsync(x => x.Attr_ID == vm.AttrId && x.IsActive == true);

            if (item == null)
                throw new InvalidOperationException("Attribute submission could not be found.");

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

            var inspectionRequiredExists = sections.Any(x =>
                x.RequiresInspection ||
                x.SectionDecision == "Requires inspection");

            var unreviewedRequiredExists = requiredSections.Any(x =>
                string.IsNullOrWhiteSpace(x.SectionDecision));

            if (vm.FinalDecision == "SubmitToOvvio")
            {
                if (unreviewedRequiredExists)
                    throw new InvalidOperationException("You cannot submit to OVVIO. Some required sections have not been reviewed.");

                if (requiredCorrectionExists)
                    throw new InvalidOperationException("You cannot submit to OVVIO. Required sections still need correction.");

                if (inspectionRequiredExists)
                    throw new InvalidOperationException("You cannot submit to OVVIO. A physical inspection is required.");
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
            review.ReviewerUserId = currentUser.UserId ?? 0;
            review.ReviewerName = currentUser.FullName;
            review.UpdatedBy = currentUserName;
            review.UpdatedDate = now;

            switch (vm.FinalDecision)
            {
                case "SubmitToOvvio":
                    review.ReviewStatus = "Completed";
                    review.ReadyForOvvioExtract = true;
                    review.ReturnToClient = false;
                    review.RequiresInspection = false;

                    item.Attr_Status = "ReadyForOvvioExtract";

                    item.Valuer = currentUser.FullName;
                    item.ValuerUserId = currentUserId;
                    item.ValuerComment = finalComment;
                    item.ValuerDecision = "Accepted";
                    item.RejectionReason = null;
                    item.ValuerDecisionDateTime = now;

                    item.ReadyForOvvioExtract = true;
                    item.OvvioExtractStatus = "Pending";
                    item.OvvioExtractBatchNo = null;
                    item.OvvioExtractDateTime = null;
                    item.OvvioExtractedBy = null;
                    item.OvvioExtractError = null;

                    item.Physical_Inspection_Required = false;

                    break;

                case "ReturnToClient":
                    review.ReviewStatus = "ReturnedToClient";
                    review.ReadyForOvvioExtract = false;
                    review.ReturnToClient = true;

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

                case "Reject":
                    review.ReviewStatus = "Rejected";
                    review.ReadyForOvvioExtract = false;
                    review.ReturnToClient = false;
                    review.RequiresInspection = false;

                    item.Attr_Status = "Rejected";

                    item.Valuer = currentUser.FullName;
                    item.ValuerUserId = currentUserId;
                    item.ValuerComment = finalComment;
                    item.ValuerDecision = "Rejected";
                    item.RejectionReason = finalComment;
                    item.ValuerDecisionDateTime = now;

                    item.ReadyForOvvioExtract = false;
                    item.OvvioExtractStatus = null;
                    item.OvvioExtractBatchNo = null;
                    item.OvvioExtractDateTime = null;
                    item.OvvioExtractedBy = null;
                    item.OvvioExtractError = null;

                    break;
            }

            item.UpdatedBy = currentUserName;
            item.UpdatedDate = now;

            var auditAction = vm.FinalDecision switch
            {
                "SubmitToOvvio" => "Submitted to OVVIO Extract",
                "ReturnToClient" => "Returned to Client",
                "Reject" => "Attribute Submission Rejected",
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

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            if (contact != null && !string.IsNullOrWhiteSpace(contact.Email))
            {
                var clientName = BuildClientName(contact);
                var attrNo = item.Attr_No ?? "-";
                var propertyDescription = item.Property_Desc;

                if (vm.FinalDecision == "SubmitToOvvio")
                {
                    await _emailService.SendAcceptedEmailAsync(
                        contact.Email,
                        clientName,
                        attrNo,
                        propertyDescription,
                        finalComment);
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
                else if (vm.FinalDecision == "ReturnToClient")
                {
                    await _emailService.SendReturnedToClientEmailAsync(
                        contact.Email,
                        clientName,
                        attrNo,
                        propertyDescription,
                        finalComment);

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
                else if (vm.FinalDecision == "Reject")
                {
                    await _emailService.SendRejectedEmailAsync(
                        contact.Email,
                        clientName,
                        attrNo,
                        propertyDescription,
                        finalComment);

                    await _notificationService.CreateNotificationAsync(
    currentUser.UserId,
    currentUser.Username ?? currentUser.WindowsUsername,
    currentUser.Role,
    "Attribute submission rejected",
    $"{item.Attr_No} has been rejected.",
    "Rejected",
    item.Attr_ID,
    item.Attr_No,
    null,
    currentUser.FullName);
                }
            }

        }
        private static HashSet<string> GetRequiredReviewSectionCodes(string? formType)
        {
            var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "PROPERTY_DETAILS",
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

            if (existingOpenRequest != null)
                throw new InvalidOperationException("There is already a pending physical inspection request for this submission.");

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

            var valuerDetails = await _context.AttrValuerInspectionDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.SapNumber == currentUser.SapNumber.Trim() &&
                    x.IsActive == true);

            if (valuerDetails == null)
                throw new InvalidOperationException("Your valuer inspection profile could not be found. Please create your valuer profile first.");

            var now = DateTime.Now;
            var pin = GenerateInspectionPin();

            request.InspectionPin = pin;
            request.InspectionPinGeneratedAt = now;
            request.ValuerDetailsSent = true;
            request.ValuerDetailsSentAt = now;
            request.ValuerDetailsSentByUserId = currentUser.UserId.Value;
            request.ValuerDetailsSentByName = currentUser.FullName;
            request.UpdatedBy = currentUser.Username ?? currentUser.WindowsUsername ?? currentUser.FullName;
            request.UpdatedDate = now;

            var oldStatus = property.Attr_Status;

            property.Attr_Status = "InspectionDetailsSent";
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
            await transaction.CommitAsync();
        }

        private static string GenerateInspectionPin()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            const int length = 6;

            return new string(Enumerable.Range(0, length)
                .Select(_ => chars[Random.Shared.Next(chars.Length)])
                .ToArray());
        }
    }
}