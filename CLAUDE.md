# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MiGestor Fiscal (Cuintable) — full-stack personal finance and tax management app for the Mexican RESICO tax regime. Bilingual (EN/ES), dark mode, CFDI invoice parsing, USD→MXN exchange rate tracking.

## Tech Stack

- **Backend**: ASP.NET Core 10.0 (.NET 10), PostgreSQL 16+ with EF Core (Npgsql), JWT auth, FluentValidation
- **Frontend**: Angular 21 (NgModule pattern, NOT standalone), Tailwind CSS 4.x + DaisyUI 5.5, Chart.js, ngx-translate
- **Testing**: xUnit + Moq (backend), Vitest (frontend)
- **Deployment**: Docker + Docker Compose, targeting Dokploy

## Common Commands

```bash
# Run full app (backend + proxied frontend)
dotnet run --project Cuintable.Server

# Run frontend only (requires backend running)
cd cuintable.client && npm start

# Backend tests
dotnet test --project Cuintable.Server.Tests

# Frontend tests
cd cuintable.client && npm test

# EF Core migrations
cd Cuintable.Server
dotnet ef migrations add <MigrationName>
dotnet ef database update

# Docker build & run
docker compose up -d

# Format
cd cuintable.client && npx prettier --write "src/**/*.{ts,html,css}"
cd Cuintable.Server && dotnet format
```

**Dev URLs**: Backend API at `https://localhost:7071` (Swagger at `/swagger`), Frontend at `https://localhost:49822`

## Architecture

```
Cuintable.Server/           # ASP.NET Core API
  Controllers/              # 7 controllers (Auth, Incomes, Expenses, CreditCards, TaxableExpenses, Tax, TaxPayments)
  Models/                   # Domain entities (User, Income, CreditCard, Expense, TaxableExpense, TaxPayment)
  Services/                 # Business logic (interface + implementation pairs)
  DTOs/                     # Request/Response objects per feature
  Validators/               # FluentValidation validators
  Data/                     # AppDbContext + DbSeeder
  Program.cs                # DI setup, middleware config

Cuintable.Server.Tests/     # xUnit tests for services

cuintable.client/src/app/   # Angular SPA
  core/                     # Guards (AuthGuard), interceptors (JwtInterceptor), shared services
  features/                 # Lazy-loaded feature modules (auth, dashboard, incomes, expenses, tax-payments, etc.)
  layouts/                  # MainLayout with Sidebar + Topbar

cuintable.client/src/public/i18n/  # en.json, es.json translation files
```

### Key Patterns

- **Service layer**: All business logic lives in `Services/` behind interfaces (e.g., `IIncomeService` / `IncomeService`). Controllers are thin.
- **User scoping**: All data queries filter by authenticated user ID from JWT claims.
- **File storage abstraction**: `IFileStorageService` interface — currently `LocalFileStorageService`, planned migration to GCS.
- **CFDI XML parsing**: `CfdiParser.cs` extracts RFC, UUID, amounts from Mexican tax invoices; stores as `jsonb` in PostgreSQL.
- **RESICO tax engine**: `ResicoTaxService.cs` implements 2024 tiered ISR rates on gross income.
- **Automatic timestamps**: `AppDbContext` overrides `SaveChangesAsync` to set `CreatedAt`/`UpdatedAt`.
- **Angular modules**: Uses NgModule with lazy-loaded feature modules, NOT standalone components.

## Environment Setup

Copy `.env.example` to `.env` and configure: `DB_HOST`, `DB_NAME`, `DB_USER`, `DB_PASSWORD`, `JWT_KEY` (min 32 chars).

JWT config: Issuer = `MiGestorFiscal`, Audience = `MiGestorFiscalApp`.

Demo account for testing: `demo@migestor.com` / `Demo123!` (created by DbSeeder).

## Database

PostgreSQL with EF Core. Uses `jsonb` columns for CFDI metadata. Unique constraint on `TaxPayment(UserId, PeriodYear, PeriodMonth)`. All entities have a `UserId` foreign key.
