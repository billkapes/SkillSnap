using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SkillSnap;
using SkillSnap.Models;
using SkillSnap.Client.Services;
using SkillSnap.Client.Pages;
using SkillSnap.Components;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<SkillSnapContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<SkillSnapContext>();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Configure Jwt:Key with a signing key of at least 32 bytes.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SkillSnap";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SkillSnap";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpClient>(serviceProvider =>
{
    var httpContext = serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext
        ?? throw new InvalidOperationException("An active HTTP request is required.");

    return new HttpClient
    {
        BaseAddress = new Uri($"{httpContext.Request.Scheme}://{httpContext.Request.Host}/")
    };
});
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<SkillService>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins("https://localhost:5001")
    .AllowAnyMethod()
    .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    var adminEmail = app.Configuration["DevelopmentAdmin:Email"];
    var adminPassword = app.Configuration["DevelopmentAdmin:Password"];

    if (string.IsNullOrWhiteSpace(adminEmail) != string.IsNullOrWhiteSpace(adminPassword))
    {
        throw new InvalidOperationException(
            "Configure both DevelopmentAdmin:Email and DevelopmentAdmin:Password to seed a test admin.");
    }

    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create Admin role: {string.Join(", ", roleResult.Errors.Select(error => error.Description))}");
            }
        }

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail };
            var userResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!userResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create development admin: {string.Join(", ", userResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var assignmentResult = await userManager.AddToRoleAsync(adminUser, "Admin");
            if (!assignmentResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not assign Admin role: {string.Join(", ", assignmentResult.Errors.Select(error => error.Description))}");
            }
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseCors("AllowClient");

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers().DisableAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(SkillSnap.Client._Imports).Assembly);

app.Run();
