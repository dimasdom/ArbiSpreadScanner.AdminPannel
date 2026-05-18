using ArbiScannerAdminPanel.Application;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Infrastructure.DbContext;
using ArbiScannerAdminPanel.API.Middleware;
using ArbiScannerWeb.Infrastructure.DbContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddAdminDbContext(builder.Configuration.GetConnectionString("AdminConnection")!);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")!));
builder.Services.AddServices();
builder.Services.AddIdentity();
builder.Services.AddAuthenticationJwt(builder.Configuration, builder.Environment);
builder.Services.AddHttpContextAccessor();

var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:Endpoint"] ?? "localhost"));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors("AllowAll");
app.UseDefaultFiles();
app.MapStaticAssets();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("/index.html");

app.UseSpa(spa =>
{
    spa.Options.SourcePath = "ClientApp";
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AdminPanelAppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    if (!await dbContext.Subscriptions.AnyAsync())
    {
        var subscriptions = new List<SubscriptionModel>
        {
            new SubscriptionModel { Type = "Basic", Price = 9.99m, DurationInDays = 30 },
            new SubscriptionModel { Type = "Standard", Price = 19.99m, DurationInDays = 90 },
            new SubscriptionModel { Type = "Premium", Price = 49.99m, DurationInDays = 365 }
        };
        dbContext.Subscriptions.AddRange(subscriptions);
        await dbContext.SaveChangesAsync();
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AdminUserModel>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var roleName in new[] { "Administrator", "Manager" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var isSeedEnabled = builder.Configuration.GetValue<bool>("Seed:Enabled");
    if (isSeedEnabled)
    {
        var adminUserName = builder.Configuration["Seed:AdminUserName"];
        var adminPassword = builder.Configuration["Seed:AdminPassword"];
        var managerUserName = builder.Configuration["Seed:ManagerUserName"];
        var managerPassword = builder.Configuration["Seed:ManagerPassword"];

        if (string.IsNullOrWhiteSpace(adminUserName) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException("Seed is enabled, but Seed:AdminUserName or Seed:AdminPassword is missing.");
        }

        var existingAdmin = await userManager.FindByNameAsync(adminUserName);
        if (existingAdmin == null)
        {
            var adminUser = new AdminUserModel { UserName = adminUserName };
            var adminResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (adminResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Administrator");
            }
        }

        if (!string.IsNullOrWhiteSpace(managerUserName) && !string.IsNullOrWhiteSpace(managerPassword))
        {
            var existingManager = await userManager.FindByNameAsync(managerUserName);
            if (existingManager == null)
            {
                var managerUser = new AdminUserModel { UserName = managerUserName };
                var managerResult = await userManager.CreateAsync(managerUser, managerPassword);
                if (managerResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(managerUser, "Manager");
                }
            }
        }
    }
}

await app.RunAsync();
