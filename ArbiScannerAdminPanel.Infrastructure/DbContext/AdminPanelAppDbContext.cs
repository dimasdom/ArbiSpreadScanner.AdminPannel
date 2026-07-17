using ArbiScannerAdminPanel.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ArbiScannerAdminPanel.Infrastructure.DbContext
{
    public class AdminPanelAppDbContext : IdentityDbContext<AdminUserModel>
    {
        public AdminPanelAppDbContext(DbContextOptions<AdminPanelAppDbContext> options) : base(options)
        {
        }
        public DbSet<PaymentModel> Payments { get; set; }
        public DbSet<SubscriptionModel> Subscriptions { get; set; }
        public DbSet<UserSubscriptionModel> UserSubscriptions { get; set; }
        public DbSet<UserSubscriptionPayment> UserSubscriptionPayments { get; set; }
        public DbSet<AdminRefreshTokenModel> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PaymentModel>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<PaymentModel>()
                .Property(p => p.UserId)
                .IsRequired();

            modelBuilder.Entity<PaymentModel>()
                .HasIndex(p => p.UserId)
                .HasDatabaseName("IX_Payments_UserId");

            modelBuilder.Entity<PaymentModel>()
                .HasIndex(p => p.TransactionId)
                .HasDatabaseName("IX_Payments_TransactionId");

            modelBuilder.Entity<SubscriptionModel>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<UserSubscriptionModel>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<UserSubscriptionModel>()
                .Property(u => u.UserId)
                .IsRequired();

            modelBuilder.Entity<UserSubscriptionModel>()
                .HasIndex(u => new { u.UserId, u.EndDate })
                .HasDatabaseName("IX_UserSubscriptions_UserId_EndDate");

            modelBuilder.Entity<UserSubscriptionModel>()
                .HasOne(u => u.Subscription)
                .WithMany()
                .HasForeignKey(u => u.SubscriptionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserSubscriptionPayment>()
                .HasKey(u => u.Id);

            modelBuilder.Entity<UserSubscriptionPayment>()
                .HasIndex(u => u.UserId)
                .HasDatabaseName("IX_UserSubscriptionPayments_UserId");

            modelBuilder.Entity<UserSubscriptionPayment>()
                .HasOne(u => u.Subscription)
                .WithMany()
                .HasForeignKey(u => u.SubscriptionId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserSubscriptionPayment>()
                .HasOne(u => u.Payment)
                .WithMany()
                .HasForeignKey(u => u.PaymentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<AdminRefreshTokenModel>()
                .HasKey(r => r.Id);

            modelBuilder.Entity<AdminRefreshTokenModel>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdminRefreshTokenModel>()
                .HasIndex(r => r.TokenHash);

            modelBuilder.Entity<AdminRefreshTokenModel>()
                .HasIndex(r => r.UserId);

            modelBuilder.Entity<AdminRefreshTokenModel>()
                .HasOne(r => r.ReplacedByToken)
                .WithMany()
                .HasForeignKey(r => r.ReplacedByTokenId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
