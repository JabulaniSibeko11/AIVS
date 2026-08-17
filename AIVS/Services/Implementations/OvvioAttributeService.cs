using AIVS.Data;
using AIVS.Models.Attributes;
using AIVS.Models.ViewModels.UserManagement;
using AIVS.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AIVS.Services.Implementations;

public class OvvioAttributeService : IOvvioAttributeService
{
    private readonly AttributesDbContext _context;

    public OvvioAttributeService(AttributesDbContext context)
    {
        _context = context;
    }

    public async Task<AttrOvvioApprovedAttribute> InsertApprovedSubmissionAsync(
        AttrPropertyInfo item,
        string approvalComment,
        string? approvalNoticePath,
        AivsCurrentUserVm currentUser)
    {
        var detailsId = item.Attr_PropertyDetailsId;
        if (detailsId == null)
            throw new InvalidOperationException("Property details are missing; the approved attribute data cannot be inserted into OVVIO staging.");

        var details = await _context.AttrPropertyDetails.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == detailsId.Value);

        var payload = new
        {
            Property = details,
            Valuation = await _context.AttrValuationDetails.AsNoTracking().FirstOrDefaultAsync(x => x.PropertyDetailsId == detailsId.Value),
            Primary = await _context.AttrPrimaryAttributes.AsNoTracking().FirstOrDefaultAsync(x => x.PropertyDetailsId == detailsId.Value),
            Secondary = await _context.AttrSecondaryAttributes.AsNoTracking().FirstOrDefaultAsync(x => x.PropertyDetailsId == detailsId.Value),
            Access = await _context.AttrAccess.AsNoTracking().FirstOrDefaultAsync(x => x.PropertyDetailsId == detailsId.Value),
            Calculations = await _context.AttrCalculations.AsNoTracking().FirstOrDefaultAsync(x => x.PropertyDetailsId == detailsId.Value),
            BusinessGeneral = await _context.AttrBusinessGeneral.AsNoTracking().FirstOrDefaultAsync(x => x.PropertyDetailsId == detailsId.Value),
            BusinessBuildings = await _context.AttrBusinessBuildings.AsNoTracking().Where(x => x.PropertyDetailsId == detailsId.Value).ToListAsync(),
            BusinessSections = await _context.AttrBusinessSections.AsNoTracking().Where(x => x.PropertyDetailsId == detailsId.Value).ToListAsync(),
            DrcBuildings = await _context.AttrDrcBuildings.AsNoTracking().Where(x => x.PropertyDetailsId == detailsId.Value).ToListAsync(),
            DrcImprovements = await _context.AttrDrcImprovements.AsNoTracking().Where(x => x.PropertyDetailsId == detailsId.Value).ToListAsync(),
            DrcVacantLand = await _context.AttrDrcVacantLand.AsNoTracking().Where(x => x.PropertyDetailsId == detailsId.Value).ToListAsync()
        };

        var existing = await _context.AttrOvvioApprovedAttributes
            .FirstOrDefaultAsync(x => x.Attr_ID == item.Attr_ID);

        var row = existing ?? new AttrOvvioApprovedAttribute { Attr_ID = item.Attr_ID };
        row.Attr_No = item.Attr_No;
        row.PremiseId = item.Premise_id;
        row.ValuationKey = item.Valuation_Key;
        row.PropertyDescription = item.Property_Desc;
        row.Township = details?.Township;
        row.Sector = item.RoutedSector ?? item.Sector;
        row.PropertyType = item.Property_Type;
        row.ApprovedAttributeJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
        row.ExportStatus = "Inserted";
        row.ApprovedAt = DateTime.Now;
        row.ApprovedBy = currentUser.FullName;
        row.ApprovedByUserId = currentUser.UserId?.ToString();
        row.ApprovalComment = approvalComment;
        row.ApprovalNoticePath = approvalNoticePath;
        row.CreatedBy = currentUser.Username ?? currentUser.WindowsUsername ?? currentUser.FullName ?? "AIVS";

        if (existing == null)
            _context.AttrOvvioApprovedAttributes.Add(row);

        await _context.SaveChangesAsync();
        return row;
    }
}
