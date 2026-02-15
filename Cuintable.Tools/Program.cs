using Cuintable.Server.Data;
using Cuintable.Server.Models;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

Env.TraversePath().Load();

var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "cuintable";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "cuintable";

if (string.IsNullOrEmpty(dbPassword))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: DB_PASSWORD no encontrada. Asegurate de tener un archivo .env configurado.");
    Console.ResetColor();
    return 1;
}

var connectionString = $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPassword}";

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(connectionString)
    .Options;

using var db = new AppDbContext(options);

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════╗");
Console.WriteLine("║   MiGestor Fiscal - Crear cuenta Owner   ║");
Console.WriteLine("╚══════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

// Full Name
Console.Write("Nombre completo: ");
var fullName = Console.ReadLine()?.Trim();
if (string.IsNullOrWhiteSpace(fullName))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: El nombre no puede estar vacio.");
    Console.ResetColor();
    return 1;
}

// Email
Console.Write("Email: ");
var email = Console.ReadLine()?.Trim()?.ToLowerInvariant();
if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: Email invalido.");
    Console.ResetColor();
    return 1;
}

// Check if email already exists
if (await db.Users.AnyAsync(u => u.Email == email))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Error: El email '{email}' ya esta registrado.");
    Console.ResetColor();
    return 1;
}

// Password
Console.Write("Password (min 6 caracteres): ");
var password = Console.ReadLine()?.Trim();
if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Error: El password debe tener al menos 6 caracteres.");
    Console.ResetColor();
    return 1;
}

// Tenant name
Console.Write($"Nombre del tenant [{fullName}]: ");
var tenantName = Console.ReadLine()?.Trim();
if (string.IsNullOrWhiteSpace(tenantName))
{
    tenantName = fullName;
}

// Language
Console.Write("Idioma preferido (es/en) [es]: ");
var lang = Console.ReadLine()?.Trim()?.ToLowerInvariant();
if (string.IsNullOrWhiteSpace(lang) || (lang != "es" && lang != "en"))
{
    lang = "es";
}

// Confirm
Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("=== Resumen ===");
Console.ResetColor();
Console.WriteLine($"  Nombre:   {fullName}");
Console.WriteLine($"  Email:    {email}");
Console.WriteLine($"  Tenant:   {tenantName}");
Console.WriteLine($"  Rol:      Owner");
Console.WriteLine($"  Idioma:   {lang}");
Console.WriteLine();
Console.Write("Crear cuenta? (s/n): ");
var confirm = Console.ReadLine()?.Trim()?.ToLowerInvariant();
if (confirm != "s" && confirm != "si" && confirm != "y" && confirm != "yes")
{
    Console.WriteLine("Operacion cancelada.");
    return 0;
}

// Create tenant + user
var tenant = new Tenant
{
    Id = Guid.NewGuid(),
    Name = tenantName
};

var user = new User
{
    Id = Guid.NewGuid(),
    TenantId = tenant.Id,
    Role = UserRole.Owner,
    Email = email,
    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
    FullName = fullName,
    PreferredLanguage = lang
};

db.Tenants.Add(tenant);
db.Users.Add(user);
await db.SaveChangesAsync();

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Cuenta Owner creada exitosamente!");
Console.ResetColor();
Console.WriteLine($"  Tenant ID: {tenant.Id}");
Console.WriteLine($"  User ID:   {user.Id}");
Console.WriteLine($"  Email:     {user.Email}");
Console.WriteLine();
Console.WriteLine("Ya puedes hacer login con estas credenciales.");

return 0;
