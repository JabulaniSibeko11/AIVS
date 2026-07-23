using AIVS.Data;
using AIVS.Models.Attributes;
using AIVS.Models.Configuration;
using AIVS.Models.ViewModels.SectorManager;
using AIVS.Models.ViewModels.UserManagement;
using AIVS.Models.ViewModels.ValuerInbox;
using AIVS.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AIVS.Services.Implementations
{
    public class SectorManagerQaService : ISectorManagerQaService
    {
        private readonly AttributesDbContext _context;
        private readonly ILogger<SectorManagerQaService> _logger;
        private readonly IValuerReviewPdfService _valuerReviewPdfService;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly AttributeStorageSettings _storageSettings;

        public SectorManagerQaService(
            AttributesDbContext context,
            ILogger<SectorManagerQaService> logger,
            IValuerReviewPdfService valuerReviewPdfService,
            INotificationService notificationService,
            IEmailService emailService,
            IOptions<AttributeStorageSettings> storageSettings)
        {
            _context = context;
            _logger = logger;
            _valuerReviewPdfService = valuerReviewPdfService;
            _notificationService = notificationService;
            _emailService = emailService;
            _storageSettings = storageSettings.Value;
        }

        public async Task<List<SectorManagerQaInboxItemVm>> GetInboxAsync(AivsCurrentUserVm currentUser)
        {
            if (currentUser.UserId == null)
                return new List<SectorManagerQaInboxItemVm>();

            var sector = currentUser.Sector?.Trim();

            var query =
                from qa in _context.AttrSectorManagerQaReviews.AsNoTracking()
                join item in _context.AttrPropertyInfo.AsNoTracking()
                    on qa.Attr_ID equals item.Attr_ID
                join details in _context.AttrPropertyDetails.AsNoTracking()
                    on item.Attr_ID equals details.Id into detailsJoin
                from details in detailsJoin.DefaultIfEmpty()
                where item.IsActive == true
                      && item.Attr_Status == "SectorManagerQa"
                      && qa.QaStatus == "Pending"
                select new
                {
                    Qa = qa,
                    Item = item,
                    Details = details
                };

            if (!IsExecutiveOrAdmin(currentUser) && !string.IsNullOrWhiteSpace(sector))
            {
                query = query.Where(x => x.Qa.Sector == sector || x.Item.RoutedSector == sector);
            }

            return await query
                .OrderBy(x => x.Qa.ValuerSubmittedAt)
                .Select(x => new SectorManagerQaInboxItemVm
                {
                    QaId = x.Qa.Id,
                    AttrId = x.Item.Attr_ID,
                    AttrNo = x.Item.Attr_No,
                    ValuerReviewId = x.Qa.ValuerReviewId,
                    PropertyDescription = x.Item.Property_Desc,
                    Township = x.Details != null ? x.Details.Township : null,
                    Sector = x.Item.RoutedSector,
                    ValuerName = x.Qa.ValuerName,
                    ValuerSubmittedAt = x.Qa.ValuerSubmittedAt,
                    QaStatus = x.Qa.QaStatus,
                    SelectionReason = x.Qa.SelectionReason,
                    QaWeekStartDate = x.Qa.QaWeekStartDate,
                    QaWeekEndDate = x.Qa.QaWeekEndDate
                })
                .ToListAsync();
        }

        public async Task<SectorManagerQaDetailsVm> GetDetailsAsync(long qaId, AivsCurrentUserVm currentUser)
        {
            if (currentUser.UserId == null)
                throw new InvalidOperationException("Your AIVS user could not be verified.");

            var qa = await _context.AttrSectorManagerQaReviews
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == qaId);

            if (qa == null)
                throw new InvalidOperationException("Sector Manager QA record could not be found.");

            var item = await _context.AttrPropertyInfo
                .AsNoTracking()
                .Include(x => x.PropertyDetails)
                .FirstOrDefaultAsync(x =>
                    x.Attr_ID == qa.Attr_ID &&
                    x.IsActive == true);

            if (item == null)
                throw new InvalidOperationException("Attribute submission could not be found.");

            EnsureSectorAccess(item.RoutedSector, currentUser);

            if (qa.ValuerReviewId == null)
                throw new InvalidOperationException("Valuer review could not be found for this QA record.");

            var review = await _context.AttrValuerReviews
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == qa.ValuerReviewId.Value);

            if (review == null)
                throw new InvalidOperationException("Valuer review could not be found.");

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

            var submittedForm = await BuildSubmittedAttributeViewModelAsync(item.Attr_ID);
            var evidenceFiles = await BuildEvidenceFilesAsync(item.Attr_ID);
            var physicalEvidenceFiles = await BuildPhysicalInspectionEvidenceFilesAsync(item.Attr_ID);

            return new SectorManagerQaDetailsVm
            {
                QaId = qa.Id,
                AttrId = item.Attr_ID,
                AttrNo = item.Attr_No,
                ValuerReviewId = qa.ValuerReviewId,
                PropertyDescription = item.Property_Desc,
                Township = item.PropertyDetails?.Township,
                Sector = item.RoutedSector,
                CurrentStatus = item.Attr_Status,
                QaStatus = qa.QaStatus,
                SelectionReason = qa.SelectionReason,
                QaWeekStartDate = qa.QaWeekStartDate,
                QaWeekEndDate = qa.QaWeekEndDate,
                ValuerName = qa.ValuerName ?? review.ReviewerName,
                ValuerSubmittedAt = qa.ValuerSubmittedAt,
                ValuerFinalDecision = review.FinalDecision,
                ValuerFinalComment = review.FinalComment,
                ReviewedPdfPathBeforeQa = qa.ReviewedPdfPathBeforeQa,
                ReviewedPdfPathAfterQa = qa.ReviewedPdfPathAfterQa,
                SubmittedForm = submittedForm,
                Sections = sections,
                EvidenceFiles = evidenceFiles,
                PhysicalInspectionEvidenceFiles = physicalEvidenceFiles
            };
        }

        public async Task ApproveToOvvioAsync(SectorManagerQaDecisionVm vm, AivsCurrentUserVm currentUser)
        {
            if (currentUser.UserId == null)
                throw new InvalidOperationException("Your AIVS user could not be verified.");

            if (string.IsNullOrWhiteSpace(vm.Comment))
                throw new InvalidOperationException("Please enter the Sector Manager QA approval comment.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var qa = await _context.AttrSectorManagerQaReviews
                .FirstOrDefaultAsync(x => x.Id == vm.QaId && x.Attr_ID == vm.AttrId);

            if (qa == null)
                throw new InvalidOperationException("Sector Manager QA record could not be found.");

            if (!string.Equals(qa.QaStatus, "Pending", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(qa.QaStatus, "InProgress", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This QA record has already been completed.");

            var item = await _context.AttrPropertyInfo
                .Include(x => x.PropertyDetails)
                .FirstOrDefaultAsync(x =>
                    x.Attr_ID == vm.AttrId &&
                    x.IsActive == true);

            if (item == null)
                throw new InvalidOperationException("Attribute submission could not be found.");

            EnsureSectorAccess(item.RoutedSector, currentUser);

            if (!string.Equals(item.Attr_Status, "SectorManagerQa", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This submission is not waiting for Sector Manager QA.");

            if (qa.ValuerReviewId == null)
                throw new InvalidOperationException("Valuer review could not be found for this QA record.");

            var review = await _context.AttrValuerReviews
                .FirstOrDefaultAsync(x => x.Id == qa.ValuerReviewId.Value);

            if (review == null)
                throw new InvalidOperationException("Valuer review could not be found.");

            var now = DateTime.Now;
            var comment = vm.Comment.Trim();
            var oldStatus = item.Attr_Status;
            var currentUserName = CurrentUserName(currentUser);
            var currentUserId = currentUser.UserId.Value.ToString();

            qa.QaStatus = "Approved";
            qa.QaDecision = "ApproveToOvvio";
            qa.QaComment = comment;
            qa.QaStartedAt ??= now;
            qa.QaCompletedAt = now;
            qa.SectorManagerUserId = currentUser.UserId.Value;
            qa.SectorManagerUsername = currentUser.Username ?? currentUser.WindowsUsername;
            qa.SectorManagerName = currentUser.FullName;
            qa.SectorManagerEmail = currentUser.Email;
            qa.UpdatedBy = currentUserName;
            qa.UpdatedDate = now;

            review.ReviewStatus = "Completed";
            review.ReadyForOvvioExtract = true;
            review.ReturnToClient = false;
            review.RequiresInspection = false;
            review.UpdatedBy = currentUserName;
            review.UpdatedDate = now;

            item.Attr_Status = "ReadyForOvvioExtract";
            item.ReadyForOvvioExtract = true;
            item.OvvioExtractStatus = "Pending";
            item.OvvioExtractBatchNo = null;
            item.OvvioExtractDateTime = null;
            item.OvvioExtractedBy = null;
            item.OvvioExtractError = null;

            item.SectorManagerQaDecision = "ApproveToOvvio";
            item.SectorManagerQaComment = comment;
            item.SectorManagerQaBy = currentUser.FullName;
            item.SectorManagerQaUserId = currentUserId;
            item.SectorManagerQaDateTime = now;

            item.UpdatedBy = currentUserName;
            item.UpdatedDate = now;

            _context.AttrPropertyInfoAuditTrail.Add(new AttrPropertyInfoAuditTrail
            {
                Attr_ID = item.Attr_ID,
                Attr_No = item.Attr_No,
                Action = "Sector Manager Approved for OVVIO",
                OldStatus = oldStatus,
                NewStatus = "ReadyForOvvioExtract",
                ActionByUserId = currentUserId,
                ActionByName = currentUser.FullName,
                ActionRole = currentUser.Role ?? "Sector Manager",
                Comment = comment,
                ActionDateTime = now
            });

            await _context.SaveChangesAsync();

            var reviewedPdfPath = await _valuerReviewPdfService
                .GenerateReviewedFormPdfAsync(review.Id, currentUser);

            item.ValuerEvidencePath = reviewedPdfPath;
            qa.ReviewedPdfPathAfterQa = reviewedPdfPath;
            item.UpdatedBy = currentUserName;
            item.UpdatedDate = DateTime.Now;
            qa.UpdatedBy = currentUserName;
            qa.UpdatedDate = DateTime.Now;

            _context.AttrPropertyInfoAuditTrail.Add(new AttrPropertyInfoAuditTrail
            {
                Attr_ID = item.Attr_ID,
                Attr_No = item.Attr_No,
                Action = "Reviewed PDF Updated After Sector Manager QA",
                OldStatus = item.Attr_Status,
                NewStatus = item.Attr_Status,
                ActionByUserId = currentUserId,
                ActionByName = currentUser.FullName,
                ActionRole = currentUser.Role ?? "Sector Manager",
                Comment = $"Reviewed PDF updated after Sector Manager QA: {Path.GetFileName(reviewedPdfPath)}",
                ActionDateTime = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await NotifyAndEmailAfterSectorApprovalAsync(item, comment, currentUser);
        }

        public async Task ReturnToValuerAsync(SectorManagerQaDecisionVm vm, AivsCurrentUserVm currentUser)
        {
            if (currentUser.UserId == null)
                throw new InvalidOperationException("Your AIVS user could not be verified.");

            if (string.IsNullOrWhiteSpace(vm.Comment))
                throw new InvalidOperationException("Please enter the reason for returning this review to the valuer.");

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var qa = await _context.AttrSectorManagerQaReviews
                .FirstOrDefaultAsync(x => x.Id == vm.QaId && x.Attr_ID == vm.AttrId);

            if (qa == null)
                throw new InvalidOperationException("Sector Manager QA record could not be found.");

            if (!string.Equals(qa.QaStatus, "Pending", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(qa.QaStatus, "InProgress", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This QA record has already been completed.");

            var item = await _context.AttrPropertyInfo
                .FirstOrDefaultAsync(x =>
                    x.Attr_ID == vm.AttrId &&
                    x.IsActive == true);

            if (item == null)
                throw new InvalidOperationException("Attribute submission could not be found.");

            EnsureSectorAccess(item.RoutedSector, currentUser);

            if (!string.Equals(item.Attr_Status, "SectorManagerQa", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This submission is not waiting for Sector Manager QA.");

            if (qa.ValuerReviewId == null)
                throw new InvalidOperationException("Valuer review could not be found for this QA record.");

            var review = await _context.AttrValuerReviews
                .FirstOrDefaultAsync(x => x.Id == qa.ValuerReviewId.Value);

            if (review == null)
                throw new InvalidOperationException("Valuer review could not be found.");

            var now = DateTime.Now;
            var comment = vm.Comment.Trim();
            var oldStatus = item.Attr_Status;
            var currentUserName = CurrentUserName(currentUser);
            var currentUserId = currentUser.UserId.Value.ToString();

            qa.QaStatus = "ReturnedToValuer";
            qa.QaDecision = "ReturnToValuer";
            qa.QaComment = comment;
            qa.QaStartedAt ??= now;
            qa.QaCompletedAt = now;
            qa.SectorManagerUserId = currentUser.UserId.Value;
            qa.SectorManagerUsername = currentUser.Username ?? currentUser.WindowsUsername;
            qa.SectorManagerName = currentUser.FullName;
            qa.SectorManagerEmail = currentUser.Email;
            qa.UpdatedBy = currentUserName;
            qa.UpdatedDate = now;

            review.ReviewStatus = "ReturnedToValuer";
            review.ReadyForOvvioExtract = false;
            review.ReturnToClient = false;
            review.RequiresInspection = false;
            review.CompletedAt = null;
            review.UpdatedBy = currentUserName;
            review.UpdatedDate = now;

            item.Attr_Status = "ReturnedToValuer";
            item.ReadyForOvvioExtract = false;
            item.OvvioExtractStatus = null;
            item.OvvioExtractBatchNo = null;
            item.OvvioExtractDateTime = null;
            item.OvvioExtractedBy = null;
            item.OvvioExtractError = null;

            item.SectorManagerQaDecision = "ReturnToValuer";
            item.SectorManagerQaComment = comment;
            item.SectorManagerQaBy = currentUser.FullName;
            item.SectorManagerQaUserId = currentUserId;
            item.SectorManagerQaDateTime = now;

            item.UpdatedBy = currentUserName;
            item.UpdatedDate = now;

            _context.AttrPropertyInfoAuditTrail.Add(new AttrPropertyInfoAuditTrail
            {
                Attr_ID = item.Attr_ID,
                Attr_No = item.Attr_No,
                Action = "Sector Manager Returned to Valuer",
                OldStatus = oldStatus,
                NewStatus = "ReturnedToValuer",
                ActionByUserId = currentUserId,
                ActionByName = currentUser.FullName,
                ActionRole = currentUser.Role ?? "Sector Manager",
                Comment = comment,
                ActionDateTime = now
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _notificationService.CreateNotificationAsync(
                item.Task_Assigned_To_UserId == null ? null : int.TryParse(item.Task_Assigned_To_UserId, out var valuerUserId) ? valuerUserId : null,
                item.Task_Assigned_To,
                "Valuer",
                "Returned by Sector Manager",
                $"{item.Attr_No} was returned by the Sector Manager for rework.",
                "ReturnedToValuer",
                item.Attr_ID,
                item.Attr_No,
                qa.Id,
                currentUser.FullName);
        }

        private async Task NotifyAndEmailAfterSectorApprovalAsync(
            AttrPropertyInfo item,
            string comment,
            AivsCurrentUserVm currentUser)
        {
            try
            {
                var contact = item.PropertyDetails == null
                    ? null
                    : await _context.AttrContactInfo
                        .AsNoTracking()
                        .Where(x => x.PropertyDetailsId == item.PropertyDetails.Id)
                        .OrderBy(x => x.Id)
                        .FirstOrDefaultAsync();

                if (contact != null && !string.IsNullOrWhiteSpace(contact.Email))
                {
                    await _emailService.SendAcceptedEmailAsync(
                        contact.Email,
                        BuildClientName(contact),
                        item.Attr_No ?? "-",
                        item.Property_Desc,
                        comment);
                }

                await _notificationService.CreateNotificationAsync(
                    currentUser.UserId,
                    currentUser.Username ?? currentUser.WindowsUsername,
                    currentUser.Role,
                    "Sector Manager approved OVVIO extract",
                    $"{item.Attr_No} was approved by the Sector Manager and is ready for OVVIO extract.",
                    "ReadyForOvvioExtract",
                    item.Attr_ID,
                    item.Attr_No,
                    null,
                    currentUser.FullName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Sector Manager QA approval completed, but email/notification failed for {AttrNo}",
                    item.Attr_No);
            }
        }

        private void EnsureSectorAccess(string? itemSector, AivsCurrentUserVm currentUser)
        {
            if (IsExecutiveOrAdmin(currentUser))
                return;

            var userSector = currentUser.Sector?.Trim();

            if (string.IsNullOrWhiteSpace(userSector))
                throw new InvalidOperationException("Your sector could not be resolved from User Management.");

            if (!string.Equals(userSector, itemSector?.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This QA record does not belong to your sector.");
        }

        private static bool IsExecutiveOrAdmin(AivsCurrentUserVm currentUser)
        {
            var role = NormalizeRole(currentUser.Role);

            return role == "EXECUTIVE" ||
                   role == "SYSTEM ADMIN" ||
                   role == "VALUATION ADMIN" ||
                   role == "ADMIN" ||
                   role == "ADMINISTRATOR" ||
                   role == "IT MANAGER";
        }

        private static string NormalizeRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return string.Empty;

            return role
                .Replace('\u00A0', ' ')
                .Replace("\t", " ")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim()
                .ToUpperInvariant();
        }

        private static string CurrentUserName(AivsCurrentUserVm currentUser)
        {
            return currentUser.Username
                   ?? currentUser.WindowsUsername
                   ?? currentUser.FullName
                   ?? "AIVS";
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
                return Path.Combine(row.RootFolder, fileName);

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
                    CalcUpdateTla = decimal.TryParse(calculations?.CalcUpdateTla, out var tla) ? tla : null,
                    Tla = calculations?.Tla,
                    CalcUpdateWgba = decimal.TryParse(calculations?.CalcUpdateWgba, out var wgba) ? wgba : null,
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
    }
}