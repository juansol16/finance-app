using Cuintable.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Cuintable.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Income> Incomes => Set<Income>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<TaxableExpense> TaxableExpenses => Set<TaxableExpense>();
    public DbSet<TaxPayment> TaxPayments => Set<TaxPayment>();
    public DbSet<CardStatement> CardStatements => Set<CardStatement>();
    public DbSet<StatementTransaction> StatementTransactions => Set<StatementTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tenant
        modelBuilder.Entity<Tenant>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).IsRequired().HasMaxLength(200);
        });

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            e.Property(u => u.PreferredLanguage).HasMaxLength(5).HasDefaultValue("es");
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);

            e.HasOne(u => u.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(u => u.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Income
        modelBuilder.Entity<Income>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Source).IsRequired().HasMaxLength(200);
            e.Property(i => i.AmountMXN).HasPrecision(18, 2);
            e.Property(i => i.ExchangeRate).HasPrecision(18, 6);
            e.Property(i => i.AmountUSD).HasPrecision(18, 2);
            e.Property(i => i.HonorarioMXN).HasPrecision(18, 2);
            e.Property(i => i.IvaMXN).HasPrecision(18, 2);
            e.Property(i => i.SubtotalMXN).HasPrecision(18, 2);
            e.Property(i => i.IsrWithheldMXN).HasPrecision(18, 2);
            e.Property(i => i.IvaWithheldMXN).HasPrecision(18, 2);
            e.Property(i => i.TakeHomePayUSD).HasPrecision(18, 2);
            e.Property(i => i.XmlMetadata).HasColumnType("jsonb");

            e.HasOne(i => i.Tenant)
                .WithMany()
                .HasForeignKey(i => i.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(i => i.User)
                .WithMany(u => u.Incomes)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(i => i.TenantId);
        });

        // CreditCard
        modelBuilder.Entity<CreditCard>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Bank).IsRequired().HasMaxLength(100);
            e.Property(c => c.Nickname).IsRequired().HasMaxLength(100);
            e.Property(c => c.LastFourDigits).IsRequired().HasMaxLength(4).IsFixedLength();

            e.HasOne(c => c.Tenant)
                .WithMany()
                .HasForeignKey(c => c.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.User)
                .WithMany(u => u.CreditCards)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(c => c.TenantId);
        });

        // Expense
        modelBuilder.Entity<Expense>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AmountMXN).HasPrecision(18, 2);

            e.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.User)
                .WithMany(u => u.Expenses)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.CreditCard)
                .WithMany(c => c.Expenses)
                .HasForeignKey(x => x.CreditCardId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.TenantId);
        });

        // TaxableExpense
        modelBuilder.Entity<TaxableExpense>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.AmountMXN).HasPrecision(18, 2);
            e.Property(t => t.IvaMXN).HasPrecision(18, 2);
            e.Property(t => t.ValidationComment).HasMaxLength(500);
            e.Property(t => t.Vendor).IsRequired().HasMaxLength(200);
            e.Property(t => t.XmlMetadata).HasColumnType("jsonb");

            e.HasOne(t => t.Tenant)
                .WithMany()
                .HasForeignKey(t => t.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.User)
                .WithMany(u => u.TaxableExpenses)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.CreditCard)
                .WithMany(c => c.TaxableExpenses)
                .HasForeignKey(t => t.CreditCardId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(t => t.Expense)
                .WithMany(x => x.TaxableExpenses)
                .HasForeignKey(t => t.ExpenseId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(t => t.TenantId);
        });

        // CardStatement
        modelBuilder.Entity<CardStatement>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.BankName).HasMaxLength(100);
            e.Property(s => s.CardLastFour).HasMaxLength(4);
            e.Property(s => s.PdfUrl).IsRequired();
            e.Property(s => s.PreviousBalance).HasPrecision(18, 2);
            e.Property(s => s.TotalPayments).HasPrecision(18, 2);
            e.Property(s => s.TotalCharges).HasPrecision(18, 2);
            e.Property(s => s.InterestCharged).HasPrecision(18, 2);
            e.Property(s => s.FeesCharged).HasPrecision(18, 2);
            e.Property(s => s.NewBalance).HasPrecision(18, 2);
            e.Property(s => s.MinimumPayment).HasPrecision(18, 2);
            e.Property(s => s.NoInterestPayment).HasPrecision(18, 2);
            e.Property(s => s.CreditLimit).HasPrecision(18, 2);
            e.Property(s => s.AvailableCredit).HasPrecision(18, 2);
            e.Property(s => s.RawExtractionJson).HasColumnType("jsonb");
            e.Property(s => s.AdviceJson).HasColumnType("jsonb");

            e.HasOne(s => s.Tenant)
                .WithMany()
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.CreditCard)
                .WithMany()
                .HasForeignKey(s => s.CreditCardId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(s => s.TenantId);
            e.HasIndex(s => new { s.TenantId, s.PeriodYear, s.PeriodMonth });
        });

        // StatementTransaction
        modelBuilder.Entity<StatementTransaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.RawDescription).IsRequired();
            e.Property(t => t.Merchant).IsRequired().HasMaxLength(200);
            e.Property(t => t.AmountMXN).HasPrecision(18, 2);
            e.Property(t => t.SuspiciousReason).HasMaxLength(200);

            e.HasOne(t => t.Tenant)
                .WithMany()
                .HasForeignKey(t => t.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.Statement)
                .WithMany(s => s.Transactions)
                .HasForeignKey(t => t.StatementId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.MatchedExpense)
                .WithMany()
                .HasForeignKey(t => t.MatchedExpenseId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(t => t.TenantId);
            e.HasIndex(t => t.StatementId);
        });

        // TaxPayment
        modelBuilder.Entity<TaxPayment>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.AmountDue).HasPrecision(18, 2);
            e.Property(p => p.PeriodMonth).IsRequired();
            e.Property(p => p.PeriodYear).IsRequired();

            e.HasOne(p => p.Tenant)
                .WithMany()
                .HasForeignKey(p => p.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.User)
                .WithMany(u => u.TaxPayments)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(p => new { p.TenantId, p.PeriodYear, p.PeriodMonth }).IsUnique();
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Income income) income.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is Expense expense) expense.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is TaxableExpense taxable) taxable.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is TaxPayment payment) payment.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is User user) user.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is CardStatement statement) statement.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is StatementTransaction transaction) transaction.UpdatedAt = DateTime.UtcNow;
        }
    }
}
