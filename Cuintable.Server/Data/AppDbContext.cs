using Cuintable.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Cuintable.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Income> Incomes => Set<Income>();
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<TaxableExpense> TaxableExpenses => Set<TaxableExpense>();
    public DbSet<TaxPayment> TaxPayments => Set<TaxPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).IsRequired().HasMaxLength(256);
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            e.Property(u => u.PreferredLanguage).HasMaxLength(5).HasDefaultValue("es");
        });

        // Income
        modelBuilder.Entity<Income>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Source).IsRequired().HasMaxLength(200);
            e.Property(i => i.AmountMXN).HasPrecision(18, 2);
            e.Property(i => i.ExchangeRate).HasPrecision(18, 6);
            e.Property(i => i.AmountUSD).HasPrecision(18, 2);
            e.Property(i => i.XmlMetadata).HasColumnType("jsonb");

            e.HasOne(i => i.User)
                .WithMany(u => u.Incomes)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CreditCard
        modelBuilder.Entity<CreditCard>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Bank).IsRequired().HasMaxLength(100);
            e.Property(c => c.Nickname).IsRequired().HasMaxLength(100);
            e.Property(c => c.LastFourDigits).IsRequired().HasMaxLength(4).IsFixedLength();

            e.HasOne(c => c.User)
                .WithMany(u => u.CreditCards)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Expense
        modelBuilder.Entity<Expense>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AmountMXN).HasPrecision(18, 2);

            e.HasOne(x => x.User)
                .WithMany(u => u.Expenses)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.CreditCard)
                .WithMany(c => c.Expenses)
                .HasForeignKey(x => x.CreditCardId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // TaxableExpense
        modelBuilder.Entity<TaxableExpense>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.AmountMXN).HasPrecision(18, 2);
            e.Property(t => t.Vendor).IsRequired().HasMaxLength(200);
            e.Property(t => t.XmlMetadata).HasColumnType("jsonb");

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
        });

        // TaxPayment
        modelBuilder.Entity<TaxPayment>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.AmountDue).HasPrecision(18, 2);
            e.Property(p => p.PeriodMonth).IsRequired();
            e.Property(p => p.PeriodYear).IsRequired();

            e.HasOne(p => p.User)
                .WithMany(u => u.TaxPayments)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(p => new { p.UserId, p.PeriodYear, p.PeriodMonth }).IsUnique();
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
        }
    }
}
