using ArbiScannerAdminPanel.Application;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Infrastructure.DbContext;
using ArbiScannerWeb.Infrastructure.DbContext;
using ArbiScannerWeb.Infrastructure.Filters;
using ArbiScannerWeb.Infrastructure.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Enrichers.Span;
using StackExchange.Redis;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .Enrich.WithSpan());

    builder.Services.AddControllers(opts => opts.Filters.Add<ResultStatusCodeFilter>());
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
                .WithMethods("GET", "POST", "DELETE")
                .WithHeaders("Content-Type")
                .AllowCredentials();
        });
    });

    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.ConnectAsync(builder.Configuration["Redis:Endpoint"] ?? "localhost").GetAwaiter().GetResult());

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetSlidingWindowLimiter(GetClientIpAddress(httpContext), _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
    });

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("arbiscanner-admin", serviceVersion: "1.0.0"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(o =>
            {
                o.RecordException = true;
                o.Filter = ctx => ctx.Request.Path != "/metrics";
            })
            .AddHttpClientInstrumentation(o => o.RecordException = true)
            .AddEntityFrameworkCoreInstrumentation()
            .AddRedisInstrumentation()
            .AddOtlpExporter())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter());

    var app = builder.Build();

    app.UseOpenTelemetryPrometheusScrapingEndpoint();
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
    app.UseRateLimiter();

    app.MapControllers();
    app.MapFallbackToFile("/index.html");

    app.UseSpa(spa =>
    {
        spa.Options.SourcePath = "ClientApp";
    });

    using (var scope = app.Services.CreateScope())
    {
        await SeedDatabaseAsync(scope.ServiceProvider, builder.Configuration);
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

static string GetClientIpAddress(HttpContext context)
{
    var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (!string.IsNullOrEmpty(xForwardedFor))
        return xForwardedFor.Split(',')[0].Trim();

    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

static async Task SeedDatabaseAsync(IServiceProvider services, IConfiguration config)
{
    var dbContext = services.GetRequiredService<AdminPanelAppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    if (!await dbContext.Subscriptions.AnyAsync())
    {
        dbContext.Subscriptions.AddRange(
            new SubscriptionModel { Type = "Basic",    Price = 9.99m,  DurationInDays = 30  },
            new SubscriptionModel { Type = "Standard", Price = 19.99m, DurationInDays = 90  },
            new SubscriptionModel { Type = "Premium",  Price = 49.99m, DurationInDays = 365 }
        );
        await dbContext.SaveChangesAsync();
    }

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var roleName in new[] { "Administrator", "Manager" })
    {
        if (!await roleManager.RoleExistsAsync(roleName))
            await roleManager.CreateAsync(new IdentityRole(roleName));
    }

    if (!config.GetValue<bool>("Seed:Enabled"))
        return;

    var adminUserName = config["Seed:AdminUserName"];
    var adminPassword = config["Seed:AdminPassword"];

    if (string.IsNullOrWhiteSpace(adminUserName) || string.IsNullOrWhiteSpace(adminPassword))
        throw new InvalidOperationException("Seed is enabled, but Seed:AdminUserName or Seed:AdminPassword is missing.");

    var userManager = services.GetRequiredService<UserManager<AdminUserModel>>();

    await CreateUserIfMissingAsync(userManager, adminUserName, adminPassword, "Administrator");

    var managerUserName = config["Seed:ManagerUserName"];
    var managerPassword = config["Seed:ManagerPassword"];
    if (!string.IsNullOrWhiteSpace(managerUserName) && !string.IsNullOrWhiteSpace(managerPassword))
        await CreateUserIfMissingAsync(userManager, managerUserName, managerPassword, "Manager");
}

static async Task CreateUserIfMissingAsync(
    UserManager<AdminUserModel> userManager, string userName, string password, string role)
{
    if (await userManager.FindByNameAsync(userName) is not null)
        return;

    var user = new AdminUserModel { UserName = userName };
    var result = await userManager.CreateAsync(user, password);
    if (result.Succeeded)
        await userManager.AddToRoleAsync(user, role);
}
