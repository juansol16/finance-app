# MiGestor Fiscal — Development Plan

This document describes the complete implementation plan for the MiGestor Fiscal application. It is designed to be used by any developer or LLM to continue building the project from any phase.

## Context

- **User**: Software engineer working remotely for a US company, paid in USD→MXN
- **Tax Regime**: RESICO (Régimen Simplificado de Confianza) — Mexico
- **Goal**: Track income, expenses, deductible expenses, invoices, and tax obligations
- **Secondary Goal**: Portfolio piece for US job market (bilingual, polished UI)

## Tech Stack

- **Backend**: ASP.NET Core (.NET 10), RESTful API
- **Frontend**: Angular 21, Tailwind CSS + Ripple UI
- **Database**: PostgreSQL 16 + Entity Framework Core (Npgsql)
- **Auth**: JWT with BCrypt password hashing
- **File Storage**: Google Cloud Storage (signed URLs)
- **i18n**: ngx-translate (en/es)
- **Testing**: xUnit + Moq (backend), Vitest (frontend)
- **Deploy**: Dokploy with docker-compose (personal server)
- **API Docs**: Swagger/OpenAPI

## Data Model

### Entities

```
User
├── Id: Guid (PK)
├── Email: string (unique, required)
├── PasswordHash: string (required)
├── FullName: string (required)
├── PreferredLanguage: string ("es" | "en", default "es")
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

Income
├── Id: Guid (PK)
├── UserId: Guid (FK → User, required)
├── Type: IncomeType enum (Nomina = 0, Honorarios = 1)
├── Source: string (employer or client name, required)
├── Date: DateOnly (required)
├── AmountMXN: decimal (required)
├── ExchangeRate: decimal? (nullable, only for Nomina)
├── AmountUSD: decimal? (nullable, only for Nomina)
├── Description: string?
├── InvoicePdfUrl: string? (GCS URL)
├── InvoiceXmlUrl: string? (GCS URL)
├── XmlMetadata: string? (JSON string with parsed CFDI data)
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

CreditCard
├── Id: Guid (PK)
├── UserId: Guid (FK → User, required)
├── Bank: string (required)
├── Nickname: string (required)
├── LastFourDigits: string (4 chars, required)
├── IsActive: bool (default true)
└── CreatedAt: DateTime

Expense (General/Non-deductible)
├── Id: Guid (PK)
├── UserId: Guid (FK → User, required)
├── Category: ExpenseCategory enum (PagoTarjeta=0, Transferencia=1, PagoCoche=2, RetiroEfectivo=3, Honorarios=4, Otro=5)
├── CreditCardId: Guid? (FK → CreditCard, nullable, only when Category=PagoTarjeta)
├── Date: DateOnly (required)
├── AmountMXN: decimal (required)
├── Description: string?
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

TaxableExpense (Deductible)
├── Id: Guid (PK)
├── UserId: Guid (FK → User, required)
├── Category: TaxableExpenseCategory enum (Luz=0, Internet=1, Celular=2, Equipo=3, Software=4, Otro=5)
├── CreditCardId: Guid? (FK → CreditCard, nullable)
├── ExpenseId: Guid? (FK → Expense, nullable, optional link to card payment)
├── Date: DateOnly (required)
├── AmountMXN: decimal (required)
├── Description: string?
├── Vendor: string (e.g., "CFE", "Telmex")
├── InvoicePdfUrl: string? (GCS URL)
├── InvoiceXmlUrl: string? (GCS URL)
├── XmlMetadata: string? (JSON string with parsed CFDI data)
├── CreatedAt: DateTime
└── UpdatedAt: DateTime

TaxPayment
├── Id: Guid (PK)
├── UserId: Guid (FK → User, required)
├── PeriodMonth: int (1-12, required)
├── PeriodYear: int (required)
├── AmountDue: decimal (required)
├── DueDate: DateOnly (required)
├── Status: TaxPaymentStatus enum (Pendiente=0, Pagado=1)
├── PaymentDate: DateOnly? (nullable)
├── DeterminationPdfUrl: string? (GCS URL, accountant's determination)
├── PaymentReceiptUrl: string? (GCS URL, proof of payment)
├── CreatedAt: DateTime
└── UpdatedAt: DateTime
```

### Relationships
- User → Income: one-to-many
- User → CreditCard: one-to-many
- User → Expense: one-to-many
- User → TaxableExpense: one-to-many
- User → TaxPayment: one-to-many
- CreditCard → Expense: one-to-many (optional)
- CreditCard → TaxableExpense: one-to-many (optional)
- Expense → TaxableExpense: one-to-many (optional link)

### Design Decisions
- **Expense vs TaxableExpense as separate tables**: They represent different fiscal concepts. A credit card payment of $15,000 MXN may contain $3,000 in deductible expenses and $12,000 in personal spending. The optional FK allows linking without forcing it.
- **XmlMetadata as JSON string**: CFDI XMLs have many fields. We store parsed data as JSON for flexibility without creating dozens of columns. PostgreSQL handles JSON well with `jsonb`.

## RESICO Tax Calculation

RESICO applies a flat rate directly on **invoiced income** (no deductions subtracted). Monthly rates (2024):

| Monthly Income (MXN) | ISR Rate |
|-----------------------|----------|
| Up to $25,000.00 | 1.00% |
| $25,000.01 – $50,000.00 | 1.10% |
| $50,000.01 – $83,333.33 | 1.50% |
| $83,333.34 – $208,333.33 | 2.00% |
| Over $208,333.33 | 2.50% |

```csharp
public static decimal CalculateResicoISR(decimal monthlyIncome)
{
    return monthlyIncome switch
    {
        <= 25_000.00m   => monthlyIncome * 0.0100m,
        <= 50_000.00m   => monthlyIncome * 0.0110m,
        <= 83_333.33m   => monthlyIncome * 0.0150m,
        <= 208_333.33m  => monthlyIncome * 0.0200m,
        _               => monthlyIncome * 0.0250m
    };
}
```

> Note: Rates may change annually. Store rate tables in configuration or database for easy updates.

---

## Phase 0: Project Setup

### Status: COMPLETED

### 0.1 Clean up VS template
- [x] Delete `WeatherForecast.cs`
- [x] Delete `Controllers/WeatherForecastController.cs`
- [x] Clean `Program.cs` (remove weather endpoint)
- [x] Clean Angular app component (remove weather table)

### 0.2 Backend packages (NuGet)
Install in `Cuintable.Server.csproj`:
```
Npgsql.EntityFrameworkCore.PostgreSQL
Microsoft.AspNetCore.Authentication.JwtBearer
BCrypt.Net-Next
FluentValidation.AspNetCore
Swashbuckle.AspNetCore
```

### 0.3 Domain entities
Create in `Cuintable.Server/Models/`:
- `User.cs`
- `Income.cs` + `IncomeType.cs` (enum)
- `Expense.cs` + `ExpenseCategory.cs` (enum)
- `TaxableExpense.cs` + `TaxableExpenseCategory.cs` (enum)
- `CreditCard.cs`
- `TaxPayment.cs` + `TaxPaymentStatus.cs` (enum)

### 0.4 Database context
Create `Cuintable.Server/Data/AppDbContext.cs` with all DbSets, relationships, and constraints.

### 0.5 JWT Authentication
- Configure JWT in `Program.cs`
- Create `AuthController` with `POST /api/auth/register` and `POST /api/auth/login`
- Create `DTOs/Auth/RegisterRequest.cs`, `LoginRequest.cs`, `AuthResponse.cs`
- Store JWT secret in user-secrets (dev) / env vars (prod)

### 0.6 Frontend setup
- Install Tailwind CSS + Ripple UI
- Install and configure `@ngx-translate/core` + `@ngx-translate/http-loader`
- Create `assets/i18n/en.json` and `assets/i18n/es.json`
- Implement language toggle component
- Implement dark mode toggle

### 0.7 Docker Compose for Dokploy
Create `docker-compose.yml` with:
- `app` service: ASP.NET Core + Angular (uses existing Dockerfile)
- `db` service: PostgreSQL 16 Alpine
- Named volume for DB persistence
- Environment variables for connection string and JWT

---

## Phase 1: Income Module

### Status: COMPLETED

### Backend
- `IncomesController` with full CRUD: `GET /api/incomes`, `GET /api/incomes/{id}`, `POST /api/incomes`, `PUT /api/incomes/{id}`, `DELETE /api/incomes/{id}`
- `POST /api/incomes/{id}/upload` — upload invoice files (PDF, XML, ZIP) to GCS
- `IncomesService` with business logic
- `CreateIncomeRequest`, `UpdateIncomeRequest`, `IncomeResponse` DTOs
- FluentValidation validators
- If XML uploaded, parse CFDI and store metadata
- Unit tests for service layer

### Frontend
- Income list page with table/card view
- Create/Edit income modal/form
- Conditional fields: show ExchangeRate + AmountUSD only when Type=Nomina
- File upload (drag & drop) for invoices
- Display parsed XML metadata if available

### GCS Setup (first time)
- Create GCS bucket with private access
- Create service account with `Storage Object Admin` role
- Download JSON key file
- Create `IFileStorageService` interface and `GcsFileStorageService` implementation
- Generate signed URLs for read access (15-min expiry)

---

## Phase 2: Expense Module

### Status: COMPLETED

### Backend
- `CreditCardsController` with CRUD: `GET`, `POST`, `PUT`, `DELETE`
- `ExpensesController` with CRUD
- When category is `PagoTarjeta`, validate that `CreditCardId` is provided
- DTOs and FluentValidation
- Unit tests

### Frontend
- Credit card management page (list + add/edit form)
- Expense list page
- Expense form with category dropdown
- When `PagoTarjeta` is selected, show credit card selector dropdown
- Filter expenses by date range and category

---

## Phase 3: Taxable Expense Module

### Status: COMPLETED

### Backend
- `TaxableExpensesController` with CRUD
- File upload endpoint for PDF/XML/ZIP invoices to GCS
- `CfdiParserService` — parse CFDI XML files using `System.Xml.Linq`
  - Extract: `Rfc` (issuer), `Total`, `UUID` (folio fiscal), `FechaTimbrado`, `Concepto`
  - Store as JSON in `XmlMetadata`
- Optional linking: validate `ExpenseId` points to a valid expense of same user
- Unit tests including XML parsing

### XML Parsing Details
CFDI structure (key namespaces):
```xml
<cfdi:Comprobante xmlns:cfdi="http://www.sat.gob.mx/cfd/4" Total="1500.00">
  <cfdi:Emisor Rfc="AAA010101AAA" Nombre="EMPRESA SA" />
  <cfdi:Conceptos>
    <cfdi:Concepto Descripcion="Servicio de internet" Importe="1293.10" />
  </cfdi:Conceptos>
  <cfdi:Complemento>
    <tfd:TimbreFiscalDigital UUID="XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX" FechaTimbrado="2024-01-15T10:30:00" />
  </cfdi:Complemento>
</cfdi:Comprobante>
```

### Frontend
- Taxable expense list with filters
- Form with file upload (PDF/XML/ZIP)
- After XML upload: show parsed metadata preview before saving
- Optional dropdown to link to a credit card payment (shows expenses of type PagoTarjeta from same month)
- Display vendor, category, amount, and invoice status

---

## Phase 4: Tax Dashboard (Fiscal Brain)

### Status: COMPLETED

### Backend
- `TaxCalculationController`
  - `GET /api/tax/summary?month=X&year=Y` — returns:
    - Total income (Nomina + Honorarios)
    - Total deductible expenses
    - Estimated ISR (RESICO calculation)
    - Effective tax rate
  - `GET /api/tax/annual-summary?year=Y` — 12-month breakdown
- `ResicoTaxService` — implements RESICO tax table logic
- Tax rate configuration (stored in `appsettings.json` or DB table `TaxRate`)
- Unit tests for tax calculations

### Frontend
- Dashboard page (landing page after login)
- Summary cards: Total Income, Total Expenses, Total Deductible, Estimated ISR
- Monthly bar chart: Income vs Expenses (use `ngx-charts` or `chart.js` with `ng2-charts`)
- Annual ISR table with monthly breakdown
- Quick-access links to recent entries

---

## Phase 5: SAT Payment Tracking

### Status: COMPLETED

### Backend
- `TaxPaymentsController` with CRUD
- `POST /api/tax-payments/{id}/determination` — upload accountant's determination PDF
- `POST /api/tax-payments/{id}/receipt` — upload payment proof
- `PUT /api/tax-payments/{id}/mark-paid` — update status to Pagado with payment date
- Endpoint to get overdue/pending payments
- Unit tests

### Frontend
- Tax payments list with status badges (Pendiente = yellow, Pagado = green)
- Visual alert for payments approaching due date (< 5 days)
- Create payment record form (month, year, amount, due date)
- Upload determination PDF
- Mark as paid flow: confirm, set date, upload receipt
- Filter by year and status

---

## Phase 6: Frontend Polish

### Status: COMPLETED

### Layout & Navigation
- Sidebar navigation (responsive, collapsible on mobile)
- Top bar with: user name, language toggle, dark mode toggle, logout
- Breadcrumbs on inner pages

### Auth Flow
- Login page with email/password
- Register page
- AuthGuard on all routes except login/register
- JwtInterceptor: attach token to all API requests
- Handle 401 responses: redirect to login
- Token refresh logic (optional, can use long-lived tokens initially)

### UX Improvements
- Loading spinners/skeletons on data fetch
- Toast notifications for CRUD operations (success/error)
- Confirmation dialogs for delete operations
- Form validation with inline error messages
- Empty states with helpful messaging
- Responsive design across all pages

### i18n Completeness
- Translate all UI strings in `en.json` and `es.json`
- Date/currency formatting per locale
- Language preference saved to user profile

---

## Phase 7: Seed Data & Deployment

### Status: PENDING

### Seed Data
Create `DbSeeder` class that populates:
- 1 demo user (`demo@migestor.com` / `Demo123!`)
- 2 credit cards (Visa and Mastercard)
- 6 months of income data:
  - Monthly salary (Nomina): ~$45,000 MXN with USD exchange rate
  - 2-3 freelance payments (Honorarios): $5,000-$15,000 MXN
- 6 months of general expenses:
  - Monthly credit card payments
  - Car payments, transfers, etc.
- 6 months of deductible expenses:
  - Monthly: electricity, internet, phone
  - Occasional: computer equipment, software licenses
- 6 months of tax payments:
  - Mix of Pendiente and Pagado statuses
  - Realistic ISR amounts based on RESICO rates
- Sample invoice files (dummy PDFs) uploaded to GCS

### Deployment (Dokploy)
1. Push repo to GitHub/GitLab
2. In Dokploy: create new project → Compose → point to repo
3. Set environment variables in Dokploy UI:
   - `ConnectionStrings__DefaultConnection`
   - `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`
   - `GCS__BucketName`, `GCS__CredentialsJson` (or mount key file)
4. Configure domain and SSL in Dokploy (auto via Traefik)
5. Deploy — Dokploy builds and runs `docker-compose.yml`

### README Finalization
- Add screenshots of the running app
- Add architecture diagram image
- Add badges (build status, .NET version, Angular version)
- Document all API endpoints
- Add Contributing section

---

## API Endpoint Summary

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Create new user |
| POST | `/api/auth/login` | Login, returns JWT |
| GET | `/api/incomes` | List user incomes |
| POST | `/api/incomes` | Create income |
| PUT | `/api/incomes/{id}` | Update income |
| DELETE | `/api/incomes/{id}` | Delete income |
| POST | `/api/incomes/{id}/upload` | Upload invoice files |
| GET | `/api/credit-cards` | List user credit cards |
| POST | `/api/credit-cards` | Create credit card |
| PUT | `/api/credit-cards/{id}` | Update credit card |
| DELETE | `/api/credit-cards/{id}` | Delete credit card |
| GET | `/api/expenses` | List user expenses |
| POST | `/api/expenses` | Create expense |
| PUT | `/api/expenses/{id}` | Update expense |
| DELETE | `/api/expenses/{id}` | Delete expense |
| GET | `/api/taxable-expenses` | List deductible expenses |
| POST | `/api/taxable-expenses` | Create deductible expense |
| PUT | `/api/taxable-expenses/{id}` | Update deductible expense |
| DELETE | `/api/taxable-expenses/{id}` | Delete deductible expense |
| POST | `/api/taxable-expenses/{id}/upload` | Upload invoice files |
| GET | `/api/tax/summary` | Monthly tax summary |
| GET | `/api/tax/annual-summary` | Annual tax breakdown |
| GET | `/api/tax-payments` | List tax payments |
| POST | `/api/tax-payments` | Create tax payment record |
| PUT | `/api/tax-payments/{id}` | Update tax payment |
| PUT | `/api/tax-payments/{id}/mark-paid` | Mark as paid |
| POST | `/api/tax-payments/{id}/determination` | Upload determination PDF |
| POST | `/api/tax-payments/{id}/receipt` | Upload payment receipt |

---

## File Structure Convention

### Backend (C#)
```
Cuintable.Server/
├── Controllers/          # API controllers (thin, delegate to services)
├── Models/               # Domain entities and enums
├── Data/                 # AppDbContext, migrations, seeder
├── Services/             # Business logic interfaces + implementations
├── DTOs/                 # Request/Response objects per feature
│   ├── Auth/
│   ├── Incomes/
│   ├── Expenses/
│   ├── TaxableExpenses/
│   ├── CreditCards/
│   └── TaxPayments/
├── Validators/           # FluentValidation validators
├── Middleware/            # Custom middleware (error handling, etc.)
├── Configuration/        # Extension methods for service registration
└── Program.cs            # App entry point and DI configuration
```

### Frontend (Angular)
```
cuintable.client/src/app/
├── core/                 # Singletons: guards, interceptors, auth service
├── shared/               # Reusable: components, pipes, directives
├── features/             # Feature modules (lazy-loaded)
│   ├── auth/             # Login, register
│   ├── dashboard/        # Main dashboard
│   ├── incomes/          # Income CRUD
│   ├── expenses/         # Expense CRUD
│   ├── taxable-expenses/ # Deductible expense CRUD
│   ├── credit-cards/     # Credit card management
│   └── tax-payments/     # SAT payment tracking
├── layouts/              # Shell layout (sidebar + topbar)
└── i18n/                 # Translation files
```

---

## Notes for LLMs Continuing This Work

1. **Always check completed phases** — Review the status markers above before starting work. Completed code is in the repo.
2. **Follow existing patterns** — Check how previous phases implemented controllers, services, DTOs, and validators before adding new ones.
3. **Test everything** — Each backend service should have corresponding xUnit tests.
4. **Bilingual** — Every UI string must be in both `en.json` and `es.json`.
5. **The project uses NgModule** (not standalone components) — the template was created with `--no-standalone`.
6. **PostgreSQL** — All EF Core migrations target Npgsql. Run `dotnet ef migrations add <Name>` and `dotnet ef database update`.
7. **User context** — All queries must filter by the authenticated user's ID. Never return data belonging to other users.
8. **File uploads** — Use the `IFileStorageService` abstraction. In dev, you can implement a local file storage fallback.
9. **RESICO rates** — May change annually. The rates in the codebase are for 2024. Verify before using in production.
10. **Dokploy deployment** — The `docker-compose.yml` at root is the deployment target. Environment variables are set in Dokploy's UI, not committed to the repo.
