# MiGestor Fiscal

A full-stack web application for managing personal finances and tax obligations under Mexico's **RESICO** (Simplified Tax Regime for Individuals). Built as both a real-world tool and a portfolio piece demonstrating modern software engineering practices.

## Purpose

As a software engineer working remotely for a US company and receiving income in both USD and MXN, managing Mexican tax obligations can be complex. MiGestor Fiscal solves this by providing:

- **Income Tracking** — Log salary payments (USD→MXN with exchange rates) and freelance honorarium payments (MXN)
- **Expense Management** — Track general expenses (credit card payments, transfers, cash withdrawals) and tax-deductible expenses separately
- **Invoice Management** — Upload and store PDF/XML invoices (CFDI) with automatic XML parsing to extract RFC, UUID, amounts
- **Tax Calculation** — Automatic ISR estimation using RESICO tax tables
- **SAT Payment Tracking** — Manage monthly tax payment obligations with status tracking and document uploads
- **Credit Card Registry** — Register credit cards and link card payments to deductible expenses

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Backend** | ASP.NET Core (.NET 10) — RESTful API |
| **Frontend** | Angular 21 with TypeScript |
| **UI Framework** | Tailwind CSS + Ripple UI |
| **Database** | PostgreSQL with Entity Framework Core |
| **Authentication** | JWT (JSON Web Tokens) |
| **File Storage** | Google Cloud Storage |
| **Internationalization** | ngx-translate (English / Spanish) |
| **Testing** | xUnit + Moq (backend), Vitest (frontend) |
| **Deployment** | Dokploy (Docker Compose on personal server) |
| **API Docs** | Swagger / OpenAPI |

## Architecture

```
┌─────────────────┐       HTTPS/JSON        ┌──────────────────────┐
│   Angular SPA   │ ◄─────────────────────► │  ASP.NET Core 10 API │
│   (Ripple UI)   │                          │                      │
│                 │                          │ Controllers          │
│ Components      │                          │ Services             │
│ Services        │                          │ Repositories         │
│ Guards          │                          │ JWT Auth Middleware   │
│ Interceptors    │                          │ XML Parser (CFDI)    │
│ i18n            │                          │ FluentValidation     │
└─────────────────┘                          └──────────┬───────────┘
                                                        │
                                          ┌─────────────┼─────────────┐
                                          │             │             │
                                    ┌─────▼─────┐ ┌────▼────┐ ┌─────▼──────┐
                                    │ PostgreSQL │ │  GCS    │ │ Exchange   │
                                    │            │ │ Bucket  │ │ Rate API   │
                                    └────────────┘ └─────────┘ └────────────┘
```

## Project Structure

```
Cuintable/
├── Cuintable.Server/              # ASP.NET Core Web API
│   ├── Controllers/               # API endpoints
│   ├── Models/                    # Domain entities
│   ├── Data/                      # DbContext, migrations
│   ├── Services/                  # Business logic
│   ├── DTOs/                      # Request/Response objects
│   ├── Middleware/                 # Custom middleware
│   └── Program.cs                 # App configuration
├── cuintable.client/              # Angular SPA
│   └── src/app/
│       ├── core/                  # Guards, interceptors, auth
│       ├── shared/                # Shared components, pipes
│       ├── features/              # Feature modules
│       │   ├── dashboard/
│       │   ├── incomes/
│       │   ├── expenses/
│       │   ├── taxable-expenses/
│       │   ├── credit-cards/
│       │   └── tax-payments/
│       └── i18n/                  # en.json, es.json
├── docker-compose.yml             # Dokploy deployment
├── DEVELOPMENT_PLAN.md            # Detailed implementation phases
└── README.md
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) and npm
- [PostgreSQL 16+](https://www.postgresql.org/) (or use Docker)
- [Angular CLI](https://angular.dev/) (`npm install -g @angular/cli`)

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-username/cuintable.git
   cd cuintable
   ```

2. **Set up the database**
   ```bash
   # Using Docker for PostgreSQL
   docker run -d --name cuintable-db \
     -e POSTGRES_DB=cuintable \
     -e POSTGRES_USER=cuintable \
     -e POSTGRES_PASSWORD=dev_password \
     -p 5432:5432 postgres:16-alpine
   ```

3. **Configure the backend**
   ```bash
   cd Cuintable.Server
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=cuintable;Username=cuintable;Password=dev_password"
   dotnet user-secrets set "Jwt:Key" "your-secret-key-at-least-32-characters-long"
   dotnet ef database update
   ```

4. **Install frontend dependencies**
   ```bash
   cd cuintable.client
   npm install
   ```

5. **Run the application**
   ```bash
   # From the root directory
   dotnet run --project Cuintable.Server
   ```
   The backend starts on `https://localhost:7071` and proxies the Angular dev server.

### Docker Compose (Production-like)

```bash
docker compose up -d
```

Access the app at `http://localhost:8080`.

## Environment Variables

For production deployment (e.g., Dokploy, Portainer), configure the following variables:

| Variable | Description | Example |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment mode | `Production` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=db;Database=my_db;Username=usr;Password=pwd` |
| `Jwt__Key` | JWT signing key (>32 chars) | `super_secret_key...` |
| `Jwt__Issuer` | Token issuer | `MiGestorFiscal` |
| `Jwt__Audience` | Token audience | `MiGestorFiscalApp` |
| `GoogleCloudStorage__BucketName` | GCS Bucket Name | `my-bucket-name` |
| `GoogleCloudStorage__CredentialsJson` | GCS Service Account JSON | `{"type": "service_account", ...}` |

## Demo Account

A pre-loaded demo account is available for exploring the application:

| | |
|---|---|
| **Email** | `demo@migestor.com` |
| **Password** | `Demo123!` |

The demo account includes 6 months of sample data: income records, expenses, deductible expenses with invoices, and tax payment history.

## Key Features

- **Bilingual UI** — Full English/Spanish support with one-click language toggle
- **Dark Mode** — System-aware dark/light theme toggle
- **CFDI XML Parsing** — Automatic extraction of invoice metadata (RFC, UUID, amounts)
- **RESICO Tax Calculator** — Monthly ISR estimation based on official tax tables
- **Responsive Design** — Mobile-friendly interface built with Ripple UI
- **Secure File Storage** — Signed URLs for invoice and receipt access
- **RESTful API** — Fully documented with Swagger/OpenAPI

## License

MIT
