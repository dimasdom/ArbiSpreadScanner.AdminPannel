using ArbiScannerAdminPanel.Abstractions.Interfaces.Services;
using ArbiScannerAdminPanel.Abstractions.Interfaces.Repositories;
using ArbiScannerAdminPanel.Domain.Models;
using ArbiScannerAdminPanel.Application.Services;
using ArbiScannerAdminPanel.Infrastructure.DbContext;
using ArbiScannerAdminPanel.Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ArbiScannerAdminPanel.Application
{
    public static class StartupSetup
    {
        private const string AccessTokenCookieName = "adminpanel.access_token";

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
            .AddScoped<IAdminRefreshTokenRepository, AdminRefreshTokenRepository>()
            .AddScoped<IAccountService, AccountService>()
            .AddScoped<IUsersService, UsersService>()
            .AddScoped<IOxaPayService, OxaPayService>()
            .AddScoped<IPaymentsService, PaymentsService>()
            .AddScoped<ISubscriptionService, SubscriptionService>();


        public static void AddIdentity(this IServiceCollection services)
        {
            services.AddDataProtection(); 
            services.AddIdentityCore<AdminUserModel>(config =>
            {
                config.Password.RequireNonAlphanumeric = true;
                config.Password.RequiredLength = 8;
                config.Password.RequireUppercase = true;
                config.SignIn.RequireConfirmedPhoneNumber = false;
                config.SignIn.RequireConfirmedEmail = false;
            })
            .AddRoles<IdentityRole>() 
            .AddEntityFrameworkStores<AdminPanelAppDbContext>()
            .AddSignInManager<SignInManager<AdminUserModel>>()
            .AddUserManager<UserManager<AdminUserModel>>()
            .AddDefaultTokenProviders();
        }
        public static void AddAuthenticationJwt(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            // Bind and validate through the options pipeline instead of reading the section here:
            // ValidateOnStart still fails the host fast on a misconfigured deployment, but it runs
            // while the host starts - after every configuration source is in place - so it doesn't
            // reject a configuration that is only complete once the host is built (which is the
            // case under WebApplicationFactory, where the test overrides land at build time).
            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(JwtOptions.SectionName))
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.Issuer)
                        && !string.IsNullOrWhiteSpace(options.Audience)
                        && !string.IsNullOrWhiteSpace(options.SigningKey),
                    "JWT settings are not configured. Please set Jwt:Issuer, Jwt:Audience, and Jwt:SigningKey.")
                .ValidateOnStart();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Read the JWT section here, inside the configure delegate, rather than
                // capturing it from the outer eager read above: this delegate only runs once
                // the options factory resolves JwtBearerOptions (after the host has finished
                // building), so it sees the final merged configuration - including any
                // overrides applied by WebApplicationFactory in integration tests. Reading it
                // eagerly at the outer scope would bake in configuration snapshotted before
                // those overrides are applied, causing the signing key used to validate a
                // token to silently differ from the one used to issue it.
                var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

                options.RequireHttpsMetadata = !string.Equals(environment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtOptions.SigningKey)),
                    ValidateIssuerSigningKey = true,
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrWhiteSpace(context.Token) &&
                            context.Request.Cookies.TryGetValue(AccessTokenCookieName, out var token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        }
    }
}
