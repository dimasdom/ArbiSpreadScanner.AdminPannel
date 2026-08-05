using ArbiScannerAdminPanel.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ArbiScannerAdminPanel.Infrastructure.DbContext
{
    public class AdminPanelAppDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public AdminPanelAppDbContext(DbContextOptions<AdminPanelAppDbContext> options) : base(options)
        {
        }
        public DbSet<PaymentModel> Payments { get; set; }
        public DbSet<SubscriptionModel> Subscriptions { get; set; }
        public DbSet<UserSubscriptionModel> UserSubscriptions { get; set; }
        public DbSet<UserSubscriptionPayment> UserSubscriptionPayments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<PaymentModel>()
                .HasKey(p => p.Id);

            builder.Entity<PaymentModel>()
                .Property(p => p.UserId)
                .IsRequired();

            builder.Entity<PaymentModel>()
                .HasIndex(p => p.UserId)
                .HasDatabaseName("IX_Payments_UserId");

            builder.Entity<PaymentModel>()
                .HasIndex(p => p.TransactionId)
                .HasDatabaseName("IX_Payments_TransactionId");

            builder.Entity<SubscriptionModel>()
                .HasKey(s => s.Id);

            builder.Entity<UserSubscriptionModel>()
                .HasKey(u => u.Id);

            builder.Entity<UserSubscriptionModel>()
                .Property(u => u.UserId)
                .IsRequired();

            builder.Entity<UserSubscriptionModel>()
                .HasIndex(u => new { u.UserId, u.EndDate })
                .HasDatabaseName("IX_UserSubscriptions_UserId_EndDate");

            builder.Entity<UserSubscriptionModel>()
                .HasOne(u => u.Subscription)
                .WithMany()
                .HasForeignKey(u => u.SubscriptionId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<UserSubscriptionPayment>()
                .HasKey(u => u.Id);

            builder.Entity<UserSubscriptionPayment>()
                .HasIndex(u => u.UserId)
                .HasDatabaseName("IX_UserSubscriptionPayments_UserId");

            builder.Entity<UserSubscriptionPayment>()
                .HasOne(u => u.Subscription)
                .WithMany()
                .HasForeignKey(u => u.SubscriptionId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<UserSubscriptionPayment>()
                .HasOne(u => u.Payment)
                .WithMany()
                .HasForeignKey(u => u.PaymentId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
