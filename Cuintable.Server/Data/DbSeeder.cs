using Cuintable.Server.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Cuintable.Server.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Apply migrations if any
        await context.Database.MigrateAsync();

        // Check if demo user exists
        if (await context.Users.AnyAsync(u => u.Email == "demo@migestor.com"))
        {
            return; // Already seeded
        }

        // 1. Create Demo User
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "demo@migestor.com",
            FullName = "Demo User",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo123!"),
            PreferredLanguage = "es",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // 2. Create Credit Cards (Visa and Mastercard as required)
        var card1 = new CreditCard
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Bank = "BBVA",
            Nickname = "Visa Oro",
            LastFourDigits = "1234",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var card2 = new CreditCard
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Bank = "Citibanamex",
            Nickname = "Mastercard Platinum",
            LastFourDigits = "5678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.CreditCards.AddRange(card1, card2);
        await context.SaveChangesAsync();

        // Setup for data generation
        var random = new Random(42); // Fixed seed for reproducibility
        var cards = new[] { card1, card2 };
        var startDate = DateTime.Today.AddMonths(-6);

        // 3. Generate Incomes (Last 6 months)
        var incomes = new List<Income>();
        for (int i = 0; i < 6; i++)
        {
            var monthDate = startDate.AddMonths(i);
            var daysInMonth = DateTime.DaysInMonth(monthDate.Year, monthDate.Month);

            // Monthly Payroll (Nomina) - 15th and last day
            incomes.Add(CreateIncome(user.Id, IncomeType.Nomina, "Tech Corp Inc.", new DateOnly(monthDate.Year, monthDate.Month, 15), 45000m, 18.50m, 2432.43m));
            incomes.Add(CreateIncome(user.Id, IncomeType.Nomina, "Tech Corp Inc.", new DateOnly(monthDate.Year, monthDate.Month, daysInMonth), 45000m, 18.50m, 2432.43m));

            // Variable Freelance (Honorarios) - 1 or 2 per month
            int freelanceCount = random.Next(1, 3);
            for (int k = 0; k < freelanceCount; k++)
            {
                var day = random.Next(1, daysInMonth);
                var amount = random.Next(5000, 15000);
                incomes.Add(CreateIncome(user.Id, IncomeType.Honorarios, $"Cliente Freelance {k + 1}", new DateOnly(monthDate.Year, monthDate.Month, day), amount, null, null));
            }
        }
        context.Incomes.AddRange(incomes);

        // 4. Generate Expenses & Taxable Expenses
        var expenses = new List<Expense>();
        var taxableExpenses = new List<TaxableExpense>();

        for (int i = 0; i < 6; i++)
        {
            var monthDate = startDate.AddMonths(i);
            var daysInMonth = DateTime.DaysInMonth(monthDate.Year, monthDate.Month);

            // Monthly Fixed Deductibles
            taxableExpenses.Add(CreateTaxableExpense(user.Id, TaxableExpenseCategory.Luz, "CFE", new DateOnly(monthDate.Year, monthDate.Month, 5), 500 + random.Next(-50, 50)));
            taxableExpenses.Add(CreateTaxableExpense(user.Id, TaxableExpenseCategory.Internet, "Telmex", new DateOnly(monthDate.Year, monthDate.Month, 10), 389));
            taxableExpenses.Add(CreateTaxableExpense(user.Id, TaxableExpenseCategory.Celular, "Telcel", new DateOnly(monthDate.Year, monthDate.Month, 20), 499));

            // Occasional Equipment and Software (every other month)
            if (i % 2 == 0)
            {
                taxableExpenses.Add(CreateTaxableExpense(user.Id, TaxableExpenseCategory.Software, "Adobe Creative Cloud", new DateOnly(monthDate.Year, monthDate.Month, 15), 899));
            }
            if (i == 1)
            {
                taxableExpenses.Add(CreateTaxableExpense(user.Id, TaxableExpenseCategory.Equipo, "Amazon MX", new DateOnly(monthDate.Year, monthDate.Month, 12), 15499));
            }
            if (i == 4)
            {
                taxableExpenses.Add(CreateTaxableExpense(user.Id, TaxableExpenseCategory.Software, "JetBrains", new DateOnly(monthDate.Year, monthDate.Month, 8), 3200));
            }

            // Credit card payments (PagoTarjeta)
            int cardPaymentCount = random.Next(3, 7);
            for (int k = 0; k < cardPaymentCount; k++)
            {
                var date = new DateOnly(monthDate.Year, monthDate.Month, random.Next(1, daysInMonth));
                var amount = random.Next(200, 3000);
                var card = cards[random.Next(cards.Length)];

                expenses.Add(new Expense
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Category = ExpenseCategory.PagoTarjeta,
                    CreditCardId = card.Id,
                    Date = date,
                    AmountMXN = amount,
                    Description = $"Pago tarjeta {card.Nickname}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            // Transfers
            expenses.Add(new Expense
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Category = ExpenseCategory.Transferencia,
                Date = new DateOnly(monthDate.Year, monthDate.Month, random.Next(1, daysInMonth)),
                AmountMXN = random.Next(2000, 8000),
                Description = "Transferencia mensual",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            // Car payment (fixed monthly)
            expenses.Add(new Expense
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Category = ExpenseCategory.PagoCoche,
                Date = new DateOnly(monthDate.Year, monthDate.Month, 5),
                AmountMXN = 8500,
                Description = "Mensualidad auto",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            // Occasional cash withdrawal
            if (i % 3 == 0)
            {
                expenses.Add(new Expense
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Category = ExpenseCategory.RetiroEfectivo,
                    Date = new DateOnly(monthDate.Year, monthDate.Month, random.Next(1, daysInMonth)),
                    AmountMXN = random.Next(1000, 5000),
                    Description = "Retiro ATM",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        context.Expenses.AddRange(expenses);
        context.TaxableExpenses.AddRange(taxableExpenses);

        await context.SaveChangesAsync();

        // 5. Generate Tax Payments (Last 5 months closed, current month pending)
        var taxPayments = new List<TaxPayment>();

        for (int i = 0; i < 6; i++)
        {
            var periodDate = startDate.AddMonths(i);

            // Calculate total income for this period
            var monthlyIncome = incomes
                .Where(x => x.Date.Year == periodDate.Year && x.Date.Month == periodDate.Month)
                .Sum(x => x.AmountMXN);

            var amountDue = CalculateResicoISR(monthlyIncome);
            var dueDate = new DateOnly(periodDate.Year, periodDate.Month, 17).AddMonths(1);

            var taxPayment = new TaxPayment
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                PeriodMonth = periodDate.Month,
                PeriodYear = periodDate.Year,
                AmountDue = amountDue,
                DueDate = dueDate,
                Status = i < 5 ? TaxPaymentStatus.Pagado : TaxPaymentStatus.Pendiente,
                PaymentDate = i < 5 ? dueDate.AddDays(-2) : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            taxPayments.Add(taxPayment);
        }
        context.TaxPayments.AddRange(taxPayments);
        await context.SaveChangesAsync();
    }

    private static Income CreateIncome(Guid userId, IncomeType type, string source, DateOnly date, decimal amountMXN, decimal? rate, decimal? amountUSD)
    {
        return new Income
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Source = source,
            Date = date,
            AmountMXN = amountMXN,
            ExchangeRate = rate,
            AmountUSD = amountUSD,
            Description = "Ingreso generado automaticamente",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static TaxableExpense CreateTaxableExpense(Guid userId, TaxableExpenseCategory category, string vendor, DateOnly date, decimal amount)
    {
        return new TaxableExpense
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Category = category,
            Vendor = vendor,
            Date = date,
            AmountMXN = amount,
            Description = "Gasto deducible generado",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static decimal CalculateResicoISR(decimal income)
    {
        if (income <= 25000.00m) return income * 0.0100m;
        if (income <= 50000.00m) return income * 0.0110m;
        if (income <= 83333.33m) return income * 0.0150m;
        if (income <= 208333.33m) return income * 0.0200m;
        return income * 0.0250m;
    }
}
