using AIVS.Data;

using AIVS.Models.Configuration;
using AIVS.Services.Implementations;
using AIVS.Services.Interface;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

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
builder.Services.Configure<AttributeStorageSettings>(builder.Configuration.GetSection("AttributeStorage"));
builder.Services.Configure<ValuerPhotoStorageSettings>(builder.Configuration.GetSection("ValuerPhotoStorage"));

builder.Services.AddDbContext<UserManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("UserManagementConnection")));

builder.Services.AddDbContext<AttributesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AttributesConnection")));

// ─────────────────────────────────────────────
// Windows Authentication
// ─────────────────────────────────────────────
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();


// ─────────────────────────────────────────────
// User Management Service
// ─────────────────────────────────────────────
builder.Services.AddScoped<IUserManagementService, UserManagementService>();



// ─────────────────────────────────────────────
// Sector Inbox Service
// ─────────────────────────────────────────────
builder.Services.AddScoped<ISectorInboxService, SectorInboxService>();

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<IValuerInboxService, ValuerInboxService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// ─────────────────────────────────────────────
// Authorisation policies
// ─────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;

    options.AddPolicy(aivsUsersPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy(valuerOnlyPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy(sectorManagerOnlyPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy(managementPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy(sectorInboxPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy(allSectorAccessPolicy, policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

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