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

        public static void AddAdminDbContext(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<AdminPanelAppDbContext>(options =>
              options.UseNpgsql(connectionString));
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
            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

            var jwtOptionsAtStartup = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
            if (string.IsNullOrWhiteSpace(jwtOptionsAtStartup.Issuer) ||
                string.IsNullOrWhiteSpace(jwtOptionsAtStartup.Audience) ||
                string.IsNullOrWhiteSpace(jwtOptionsAtStartup.SigningKey))
            {
                throw new InvalidOperationException("JWT settings are not configured. Please set Jwt:Issuer, Jwt:Audience, and Jwt:SigningKey.");
            }

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
