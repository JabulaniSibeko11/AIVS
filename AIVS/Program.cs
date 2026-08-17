using AIVS.Data;

using AIVS.Models.Configuration;
using AIVS.Services.Implementations;
using AIVS.Services.Interface;
using AIVS.Security;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
// ─────────────────────────────────────────────
// Read roles and policy names from appsettings
// ─────────────────────────────────────────────
var rolesSection = builder.Configuration.GetSection("AivsRoles");
var policiesSection = builder.Configuration.GetSection("AivsPolicyNames");

var allUsersRoles = rolesSection.GetSection("AllUsers").Get<string[]>() ?? Array.Empty<string>();
var managementRoles = rolesSection.GetSection("Management").Get<string[]>() ?? Array.Empty<string>();
var sectorInboxRoles = rolesSection.GetSection("SectorInboxUsers").Get<string[]>() ?? Array.Empty<string>();
var allSectorAccessRoles = rolesSection.GetSection("AllSectorAccess").Get<string[]>() ?? Array.Empty<string>();

var valuerRole = rolesSection["Valuer"] ?? "VALUER";
var sectorManagerRole = rolesSection["SectorManager"] ?? "SECTOR MANAGER";

var aivsUsersPolicy = policiesSection["AivsUsers"] ?? "AivsUsers";
var valuerOnlyPolicy = policiesSection["ValuerOnly"] ?? "ValuerOnly";
var sectorManagerOnlyPolicy = policiesSection["SectorManagerOnly"] ?? "SectorManagerOnly";
var managementPolicy = policiesSection["Management"] ?? "AivsManagement";
var sectorInboxPolicy = policiesSection["SectorInbox"] ?? "SectorInbox";
var allSectorAccessPolicy = policiesSection["AllSectorAccess"] ?? "AllSectorAccess";


builder.Services.Configure<AivsSettings>(builder.Configuration.GetSection("AivsSettings"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

builder.Services.Configure<GenesisPortalSettings>(
    builder.Configuration.GetSection("GenesisPortalSettings"));
builder.Services.Configure<AttributeStorageSettings>(builder.Configuration.GetSection("AttributeStorage"));
builder.Services.Configure<ValuerPhotoStorageSettings>(builder.Configuration.GetSection("ValuerPhotoStorage"));

builder.Services.AddDbContext<UserManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UserManagementConnection")));

builder.Services.AddDbContext<AttributesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AttributesConnection")));
builder.Services.Configure<SectorManagerQaSettings>(
    builder.Configuration.GetSection("SectorManagerQaSettings"));
builder.Services.Configure<DemoQaSettings>(
    builder.Configuration.GetSection("DemoQaSettings"));

// ─────────────────────────────────────────────
// Windows Authentication
// ─────────────────────────────────────────────
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();


// ─────────────────────────────────────────────
// User Management Service
// ─────────────────────────────────────────────
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IAivsRoleAccessService, AivsRoleAccessService>();
builder.Services.AddScoped<IAuthorizationHandler, AivsPermissionHandler>();



// ─────────────────────────────────────────────
// Sector Inbox Service
// ─────────────────────────────────────────────
builder.Services.AddScoped<ISectorInboxService, SectorInboxService>();

builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IProcessorFileService, ProcessorFileService>();

builder.Services.AddScoped<IValuerInboxService, ValuerInboxService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IHomeDashboardService, HomeDashboardService>();
builder.Services.AddScoped<IStatsExtractService, StatsExtractService>();
builder.Services.AddScoped<IValuerReviewPdfService, ValuerReviewPdfService>();
builder.Services.AddScoped<ISectorManagerQaService, SectorManagerQaService>();
builder.Services.AddScoped<IAttributeApprovalNoticeService, AttributeApprovalNoticeService>();
builder.Services.AddScoped<IOvvioAttributeService, OvvioAttributeService>();
// ─────────────────────────────────────────────
// Authorisation policies
// ─────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;

    // Permission-based policies used by the controllers.
    AddPermissionPolicy(options, AivsPolicyNames.AccessAivs, AivsPermission.AccessAivs);
    AddPermissionPolicy(options, AivsPolicyNames.ViewSectorInbox, AivsPermission.ViewSectorInbox);
    AddPermissionPolicy(options, AivsPolicyNames.SelfAssign, AivsPermission.SelfAssign);
    AddPermissionPolicy(options, AivsPolicyNames.AssignWork, AivsPermission.AssignWork);
    AddPermissionPolicy(options, AivsPolicyNames.ReviewSubmission, AivsPermission.ReviewSubmission);
    AddPermissionPolicy(options, AivsPolicyNames.SectorManagerQa, AivsPermission.PerformSectorManagerQa);
    AddPermissionPolicy(options, AivsPolicyNames.SeniorManagerQa, AivsPermission.PerformSeniorManagerQa);
    AddPermissionPolicy(options, AivsPolicyNames.ViewSectorStatistics, AivsPermission.ViewSectorStatistics);
    AddPermissionPolicy(options, AivsPolicyNames.ViewExecutiveStatistics, AivsPermission.ViewExecutiveStatistics);
    AddPermissionPolicy(options, AivsPolicyNames.ExportStatistics, AivsPermission.ExportStatistics);
    AddPermissionPolicy(options, AivsPolicyNames.ViewAllSectors, AivsPermission.ViewAllSectors);
    AddPermissionPolicy(options, AivsPolicyNames.AdministerAivs, AivsPermission.AdministerAivs);

    // Keep the older configuration-based policy names available while views or
    // controllers are migrated to the permission-based policy names above.
    AddAuthenticatedPolicy(options, aivsUsersPolicy);
    AddAuthenticatedPolicy(options, valuerOnlyPolicy);
    AddAuthenticatedPolicy(options, sectorManagerOnlyPolicy);
    AddAuthenticatedPolicy(options, managementPolicy);
    AddAuthenticatedPolicy(options, sectorInboxPolicy);
    AddAuthenticatedPolicy(options, allSectorAccessPolicy);
});

static void AddPermissionPolicy(
    AuthorizationOptions options,
    string policyName,
    AivsPermission permission)
{
    options.AddPolicy(policyName, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new AivsPermissionRequirement(permission));
    });
}

static void AddAuthenticatedPolicy(AuthorizationOptions options, string policyName)
{
    // Avoid attempting to register a duplicate policy name.
    if (string.IsNullOrWhiteSpace(policyName))
    {
        return;
    }

    options.AddPolicy(policyName, policy => policy.RequireAuthenticatedUser());
}

var app = builder.Build();

// ─────────────────────────────────────────────
// HTTP pipeline
// ─────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

// IMPORTANT: authentication must come before authorisation
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

app.Run();