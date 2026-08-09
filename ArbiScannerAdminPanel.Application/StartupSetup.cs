using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.Application.Services;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Infrastructure.DbContext;
using ArbiScannerAdminPanel.Infrastructure.Repositories;
using ArbiScannerAdminPanel.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
namespace ArbiScannerAdminPanel.Application
{
    public static class StartupSetup
    {
        public static void AddAdminDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            // Resolve the connection string inside the options delegate rather than taking an
            // already-read string: configuration sources added after the host builder is created
            // (notably WebApplicationFactory's in-memory overrides in the integration tests) are
            // only visible to reads that happen once the host is being built. Reading it at the
            // call site in Program.cs would bake in whatever appsettings.json happened to hold -
            // or nothing at all, in environments that ship no appsettings.json.
            services.AddDbContext<AdminPanelAppDbContext>(options =>
              options.UseNpgsql(configuration.GetConnectionString("AdminConnection")));
        }

        public static void AddServices(this IServiceCollection services) =>
            services.AddScoped<IAdminUsersRepository, AdminUsersRepository>()
            .AddScoped<IPaymentsRepository, PaymentsRepository>()
            .AddScoped<ISubscriptionsRepository, SubscriptionsRepository>()
            .AddScoped<IWebAppUserRepository, WebAppUserRepository>()
            .AddScoped<IKeycloakUserService, KeycloakUserService>()
            .AddScoped<IUsersService, UsersService>()
            .AddScoped<IOxaPayService, OxaPayService>()
            .AddScoped<IPaymentsService, PaymentsService>()
            .AddScoped<ISubscriptionService, SubscriptionService>();

        public static void AddJwtOptions(this IServiceCollection services, IConfiguration configuration) =>
            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // Pure resource-server config: no local Identity store or signing key —
        // Keycloak (arbiscanner-admin realm) issues tokens, this just validates them
        // against its published JWKS via Authority-based discovery.
        public static void AddAuthenticationJwt(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                    ?? throw new InvalidOperationException("JWT options not configured");

                options.Authority = jwtOptions.Authority;
                options.Audience = jwtOptions.Audience;
                // Only relaxed for local dev against a plain-HTTP Keycloak container
                // (see docker-compose.local.yml) — every real deployment keeps this true.
                options.RequireHttpsMetadata = !environment.IsDevelopment();
                // Without this, ASP.NET remaps short JWT claim names (e.g. "sub") to
                // long legacy URIs before TokenValidationParameters ever sees them, so
                // NameClaimType = "sub" below would silently never match anything.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Keycloak's user id (sub) is what every User.Identity?.Name call
                    // site in this codebase expects.
                    NameClaimType = "sub",
                    // Keycloak's default role claim shape is a nested realm_access.roles
                    // array, which [Authorize(Roles = ...)] can't read. The
                    // arbiscanner-admin realm's clients carry an extra flat "role" claim
                    // protocol mapper (see keycloak/realm-export/arbiscanner-admin-realm.json)
                    // that the JWT handler flattens into one Claim per role — pointing
                    // RoleClaimType at it is all that's needed, no ClaimsTransformation.
                    RoleClaimType = "role",
                };
            });
        }
    }
}
