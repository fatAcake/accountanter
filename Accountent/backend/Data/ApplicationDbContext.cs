using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Users> Users => Set<Users>();
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Counterparty> Counterparties => Set<Counterparty>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<TransactionLine> TransactionLines => Set<TransactionLine>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Users>(entity =>
            {
                entity.HasIndex(u => u.email).IsUnique();
                entity.Property(u => u.role).HasConversion<string>();
            });

            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasIndex(a => a.number).IsUnique();
                entity.Property(a => a.type).HasConversion<string>();
                entity.HasOne(a => a.parent)
                    .WithMany(a => a.children)
                    .HasForeignKey(a => a.parent_id)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Counterparty>(entity =>
            {
                entity.HasIndex(c => c.inn);
            });

            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasOne(t => t.debit_account)
                    .WithMany()
                    .HasForeignKey(t => t.debit_account_id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.credit_account)
                    .WithMany()
                    .HasForeignKey(t => t.credit_account_id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.counterparty)
                    .WithMany()
                    .HasForeignKey(t => t.counterparty_id)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<TransactionLine>(entity =>
            {
                entity.Property(l => l.side).HasConversion<string>();

                entity.HasOne(l => l.account)
                    .WithMany()
                    .HasForeignKey(l => l.account_id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(l => l.counterparty)
                    .WithMany()
                    .HasForeignKey(l => l.counterparty_id)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(l => l.transaction)
                    .WithMany(t => t.lines)
                    .HasForeignKey(l => l.transaction_id)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
