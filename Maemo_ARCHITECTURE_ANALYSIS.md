# Maemo - Complete Architecture & Project Analysis

**Generated:** January 28, 2026  
**Purpose:** Structured breakdown for staging deployment readiness and developer onboarding

---

## 1. SYSTEM OVERVIEW

### What Is This System?

**Maemo** is a **multi-tenant ISO compliance document management system** for governance, risk, and compliance (GRC) operations. It helps organizations manage:

- **Document control** (versioning, approval, lifecycle)
- **Non-Conformance Reports (NCRs)** with root cause and corrective action tracking
- **Risk register** with inherent/residual scoring and NCR linking
- **Audit management** (templates, runs, evidence)
- **Consultant multi-client management** (aggregated dashboards, client switching)

Target users: Quality Managers, Compliance Officers, Consultants, System Administrators in SMEs and government organizations.

### Current State

| Aspect | Status |
|--------|--------|
| **Overall** | **In-progress / Near-MVP** (~87% complete per PROJECT_ANALYSIS.md) |
| **Backend** | Functional; dev auth bypass in place; Azure AD placeholders |
| **Frontend** | Portal + Admin Console working locally |
| **Docker** | Compose exists; **Dockerfile paths may be broken** (see §6) |
| **Tests** | Minimal (placeholder `UnitTest1`, few integration tests) |
| **Production Auth** | Azure AD not configured; requires real tenant/client IDs |

### Core Modules / Domains

| Module | Description | Location |
|--------|-------------|----------|
| **Documents** | Document CRUD, versioning, approval workflow | `Maemo.Api/Portal/DocumentsEndpoints.cs`, `Engine/Documents/` |
| **NCRs** | Non-conformance reports, status workflow, risk linking | `Maemo.Api/Portal/`, `Engine/Ncrs/` |
| **Risks** | Risk register, scoring, NCR links | `Maemo.Api/Portal/`, `Engine/Risks/` |
| **Audits** | Templates, runs, answers, evidence | `Maemo.Api/Portal/AuditsEndpoints.cs`, `Engine/Audits/` |
| **Tenants** | Multi-tenancy, provisioning, M365 connect | `Maemo.Api/Portal/TenantsEndpoints.cs`, `Admin/` |
| **Consultants** | Dashboard, clients, branding (partial) | `Maemo.Api/Portal/ConsultantsEndpoints.cs` |
| **Admin** | Tenant/user management, audit logs, billing (stub) | `Maemo.Api/Admin/` |
| **Engine API** | Versioned external API (`/engine/v1/*`) | `Maemo.Api/Engine/` |

---

## 2. TECHNOLOGY STACK

### Languages, Frameworks, Libraries

| Component | Technology | Version |
|-----------|------------|---------|
| **Backend** | .NET / ASP.NET Core | 8.0 |
| **API style** | Minimal APIs (endpoint-based) | — |
| **ORM** | Entity Framework Core | 8.0 |
| **Database** | PostgreSQL | 16 |
| **CQRS** | MediatR | 12.2.0 |
| **Validation** | FluentValidation | 11.3.0 |
| **Auth** | JWT Bearer + API Key | Microsoft.AspNetCore.Authentication.JwtBearer 8.0 |
| **Logging** | Serilog | 9.0.0 |
| **Swagger** | Swashbuckle | 6.6.2 |
| **Frontend** | Angular | ~20.2.1 |
| **Auth (FE)** | MSAL Angular | ^4.0.21 |
| **Build** | Angular CLI + Vite | — |

### Database

- **Type:** PostgreSQL 16
- **ORM:** Entity Framework Core 8.0
- **Schema:** Migrations in `infrastructure/src/Maemo.Infrastructure/Migrations/`
- **Migrations:** 15+ migrations from `InitialCreate` through `AddDocumentRetentionFields`, `AddOnboardingFieldsToTenant`, etc.
- **Strategy:** Auto-apply in Development; production should run `dotnet ef database update` or equivalent

### Infrastructure & Cloud

- **Containerization:** Docker (multi-stage builds)
- **Orchestration:** Docker Compose (`docker-compose.yml`, `docker-compose.govonprem.yml`)
- **File storage:** Local filesystem (GovOnPrem) or SharePoint Online (SaaS) via Microsoft Graph
- **Deployment modes:** SaaS (cloud) and GovOnPrem (on-premises)

### DevOps

- **CI:** GitHub Actions (`.github/workflows/ci-maemo.yml`)
  - Builds solution, runs unit + integration tests
  - Builds Docker images (API, Workers, Frontend)
  - Uses `package-lock.json` for npm cache (may need creation)
- **Dockerfiles:** `Maemo.Api/Dockerfile`, `workers/src/Maemo.Workers/Dockerfile`, `frontend/Dockerfile`
- **Note:** API/Workers Dockerfiles use paths like `Maemo.Application/`, `Maemo.Infrastructure/` at repo root; actual paths are `application/src/Maemo.Application/`, `infrastructure/src/Maemo.Infrastructure/` — **Docker builds may fail** (see §6)

### External APIs / Integrations

- **Azure AD / Entra ID:** JWT auth (placeholder config)
- **Microsoft Graph:** SharePoint, Teams, Mail (stubbed in `GraphService.cs`)
- **PayFast:** Billing provider interface; `PayFastBillingProvider` is a stub

---

## 3. ARCHITECTURE

### Architectural Pattern

**Modular monolith** with Clean Architecture and CQRS:

- **Domain** (`domain/src/Maemo.Domain`): Entities, enums, value objects
- **Application** (`application/src/Maemo.Application`): Commands, queries, handlers, DTOs
- **Infrastructure** (`infrastructure/src/Maemo.Infrastructure`): EF Core, file storage, Graph API
- **API** (`Maemo.Api`): Host; Portal, Engine, Admin endpoints
- **Workers** (`workers/src/Maemo.Workers`): Background jobs

Data flow: **HTTP → Endpoint → MediatR → Handler → DbContext → PostgreSQL**

### Folder Structure

```
maemo/
├── Maemo.Api/                    # Canonical API host (Portal + Engine + Admin)
│   ├── Portal/                   # /api/* (internal)
│   ├── Engine/                   # /engine/v1/* (external, versioned)
│   ├── Admin/                    # /admin/v1/* (platform admin)
│   └── Middleware/               # Tenant, security, logging
├── application/src/
│   ├── Maemo.Application/        # CQRS handlers, DTOs, validators
│   └── Maemo.Shared/             # Shared contracts
├── domain/src/Maemo.Domain/      # Domain entities
├── infrastructure/src/Maemo.Infrastructure/
│   ├── Persistence/              # EF Core, DbContext
│   ├── Graph/                    # Microsoft Graph (stubbed)
│   └── Demo/                     # DemoDataSeeder
├── workers/src/Maemo.Workers/    # Background workers
├── frontend/                     # Angular Portal (port 4200)
│   └── projects/admin-console/   # Admin Console (port 4300)
├── Maemo.Engine.Client/           # C# SDK for Engine API
├── Maemo.UnitTests/
├── Maemo.IntegrationTests/
└── engine/, portal/, admin/      # Legacy/alternate API projects (solution references)
```

### Data Flow

1. **Entry:** HTTP request to `Maemo.Api` (Portal `/api/*`, Engine `/engine/v1/*`, Admin `/admin/v1/*`)
2. **Middleware:** Tenant resolution, auth (JWT/API Key), logging
3. **Endpoint:** Minimal API handler → `ISender.Send(command/query)`
4. **Handler:** MediatR handler → `IApplicationDbContext` / repositories
5. **Persistence:** EF Core → PostgreSQL (tenant-scoped via query filters)

### Authentication & Authorization

| Mechanism | Use Case |
|-----------|----------|
| **JWT Bearer** | User sessions (Azure AD) |
| **API Key** (`X-Api-Key`) | Programmatic access |
| **Roles** | Admin, TenantAdmin, Consultant, User, PlatformAdmin |

**Dev mode:** Portal/Admin endpoints can bypass auth when `!app.Environment.IsDevelopment()` is false (see `TenantsEndpoints.cs`, `AdminV1Endpoints.cs`).

### API Design

| API | Style | Base Path | Auth |
|-----|-------|-----------|------|
| **Portal** | REST | `/api/*` | JWT (dev bypass) |
| **Engine** | REST | `/engine/v1/*` | JWT or API Key |
| **Admin** | REST | `/admin/v1/*` | Admin only (dev bypass) |

**Representative endpoints:**

- Documents: `GET/POST/PUT/DELETE /api/documents`, `/engine/v1/tenants/{id}/documents`
- NCRs: `GET/POST/PUT /api/ncrs`, `/engine/v1/tenants/{id}/ncrs`
- Risks: `GET/POST/PUT /api/risks`, `/engine/v1/tenants/{id}/risks`
- Audits: `GET/POST /api/audits/templates`, `/engine/v1/tenants/{id}/audits/...`
- Tenants: `GET/POST /api/tenants`, `POST /api/tenants/provision`
- Admin: `GET/POST /admin/v1/tenants`, `GET /admin/v1/logs/business`
- Demo: `GET /api/demo/tenant`, `POST /api/demo/seed`
- Health: `GET /api/health`

---

## 4. WHAT IS DONE

### Fully Implemented Features

- **Documents:** CRUD, versioning, upload/download, status, approval endpoints, BBBEE fields
- **NCRs:** CRUD, status workflow, severity, root cause, corrective action, risk linking
- **Risks:** CRUD, inherent/residual scoring, NCR linking
- **Audits:** Templates, questions, runs, answers, evidence upload
- **Multi-tenancy:** Tenant isolation, provisioning, demo seeder (`11111111-1111-1111-1111-111111111111`)
- **Consultants:** Dashboard, clients list, client linking (branding partial)
- **Admin:** Tenant CRUD, status, branding, business audit logs
- **Engine API:** Documents, NCRs, Risks, Audits, versioned
- **Background workers:** Heartbeat, compliance, document reminders, overdue NCRs
- **Security:** JWT + API Key, RBAC, audit logging, encryption service, security headers

### Tests / Validation

- **Unit tests:** `Maemo.UnitTests` — placeholder `UnitTest1` only
- **Integration tests:** `Maemo.IntegrationTests` — `DocumentsApiTests`, `MaemoApiFixture`; minimal coverage
- **Validation:** FluentValidation on commands
- **E2E:** None

### End-to-End vs Partial

| Flow | Status |
|------|--------|
| Document create → upload → view → version | ✅ E2E |
| NCR create → status update → link risk | ✅ E2E |
| Risk create → link NCR | ✅ E2E |
| Audit template → run → answers → evidence | ✅ E2E |
| Consultant dashboard → client switch | ⚠️ Backend ready; UI partial |
| Tenant provisioning → user creation | ⚠️ API + Admin UI; Azure AD not wired |
| Billing invoices | ❌ Placeholder only |
| SharePoint file ops | ⚠️ Upload only; Get/Delete stubbed |

---

## 5. WHAT IS LEFT TO DO

### Incomplete Files / TODOs / Stubs

| File | Issue |
|------|-------|
| `Maemo.Api/Portal/ConsultantsEndpoints.cs` L126 | Placeholder response for consultant clients |
| `portal/.../DocumentsEndpoints.cs` L621 | TODO: tenant admin role check |
| `Maemo.Application/.../GetDocumentAuditEvidenceQueryHandler.cs` L157 | TODO: Document-NCR links table |
| `Maemo.Workers/.../RecordsRetentionWorker.cs` L175 | Placeholder worker logging |
| `Maemo.Workers/.../DocumentDestructionWorker.cs` L225 | Placeholder worker logging |
| `Maemo.Api/Admin/AdminBillingEndpoints.cs` | Billing endpoints placeholder (invoices, issue) |
| `portal/.../AuditLogEndpoints.cs` L36 | TODO: system admin check |
| `Maemo.Api/Portal/AuditLogEndpoints.cs` L36 | Same |
| `UpdateTenantBrandingCommandHandler.cs` L39 | TODO: get ModifiedBy from user context |
| `UpdateAdminTenantStatusCommandHandler.cs` L38 | TODO: TenantStatus enum |
| `AdminTenantDetailDto.cs` L24 | TODO: AzureAdTenantId, SharePointSiteId |
| `GetAdminDashboardSummaryQueryHandler.cs` L32 | TODO: TenantStatus enum |
| `ProvisionTenantCommandHandler.cs` L186 | Placeholder admin user |
| `GraphService.cs` L105, 122, 140, 155 | SharePoint upload TODO; Teams/Mail/GetUserProfile stubs |
| `MaemoDbContextFactory.cs` L25 | Stub tenant provider for design-time |

### Missing Integrations / Broken Imports

- **Azure AD:** Placeholder `{tenantId}`, `{your-client-id}` in config
- **SharePoint:** `GetAsync`, `DeleteAsync` not implemented
- **Billing:** No real PayFast or invoice logic
- **CI npm cache:** Uses `package-lock.json`; may need `npm install` to generate

### Security / Config Gaps

- **Hardcoded values:** `Authentication.Authority`, `Audience` use placeholders
- **No `.env.example`:** Environment variables not documented in a single file
- **Dev auth bypass:** Intentional but must be disabled in production

### Missing Tests

- Unit tests for handlers, validators, services
- Integration tests for NCR, Risk, Audit, Tenant flows
- E2E tests for Portal and Admin
- Contract tests for Engine API

---

## 6. STAGING DEPLOYMENT READINESS

### Gaps Before Staging

1. **Dockerfile paths:** API and Workers Dockerfiles reference `Maemo.Application/`, `Maemo.Domain/`, `Maemo.Infrastructure/`, `Maemo.Workers/` at repo root. Actual paths: `application/src/Maemo.Application/`, `domain/src/Maemo.Domain/`, `infrastructure/src/Maemo.Infrastructure/`, `workers/src/Maemo.Workers/`. **Docker builds will fail** unless paths are fixed.
2. **Environment variables:** No `.env.example`; config scattered across `appsettings.*.json`, `environment.ts`
3. **Azure AD:** Real tenant/client IDs required for production-like auth
4. **Database migrations:** Strategy exists; ensure migrations run in staging (e.g. init container or startup)
5. **Seeding:** Demo seeder runs in Development; staging may need explicit seed or different strategy

### Docker / Docker Compose

- **docker-compose.yml:** Present; services: `db` (PostgreSQL 5433→5432), `api`, `workers`, `frontend`
- **Dockerfiles:** Present but path issues (see above)
- **Frontend Dockerfile:** In `frontend/`; builds Angular app

### Environment Variables

| Variable | Used In | Purpose |
|----------|---------|---------|
| `ConnectionStrings__MaemoDatabase` | API, Workers | PostgreSQL connection |
| `ASPNETCORE_ENVIRONMENT` | API, Workers | Development/Production |
| `Authentication__Authority` | API | Azure AD authority |
| `Authentication__Audience` | API | JWT audience |
| `Storage__LocalBasePath` | API | Local file storage (GovOnPrem) |
| `API_BASE_URL` | Frontend (Docker) | API base URL |
| `adminApiBaseUrl` | Admin Console | Admin API base |

**Recommendation:** Add `.env.example` with all required variables and descriptions.

### Database Migration Strategy

- EF Core migrations in `Maemo.Infrastructure/Migrations/`
- Auto-apply in Development (see `Program.cs` or startup)
- Staging/production: run `dotnet ef database update` or equivalent in init/startup

### Estimate: Developer Days to Staging

| Category | Days | Notes |
|----------|------|-------|
| **Dockerfile fixes** | 0.5–1 | Correct COPY paths for project structure |
| **Env documentation** | 0.5 | `.env.example`, update RUNBOOK |
| **Azure AD (staging)** | 1–2 | App registration, config |
| **Critical TODOs** | 1–2 | Billing stubs OK; fix consultant placeholder, audit log check |
| **Testing** | 2–3 | Basic integration tests for main flows |
| **CI verification** | 0.5 | Ensure npm cache, Docker builds pass |
| **Total** | **5–9 days** | Depends on Azure AD and test depth |

---

## 7. ONBOARDING GUIDE

### Clone and Run Locally

```powershell
git clone <repo-url>
cd maemo
```

### Dependencies (Order)

1. **.NET 8 SDK:** `dotnet --version`
2. **Node.js 18+:** `node --version`
3. **PostgreSQL 16** or Docker for DB only
4. **Angular CLI (optional):** `npm install -g @angular/cli`

### Environment Variables

- **API:** `Maemo.Api/appsettings.Development.json`
  - `ConnectionStrings:MaemoDatabase` — use port **5433** if using Docker DB (host port mapping)
  - `Authentication` — placeholders OK for dev; auth bypass in Development
- **Frontend:** `frontend/src/environments/environment.ts`, `environment.development.ts`
  - `apiBaseUrl`: `http://localhost:5000`
  - `adminApiBaseUrl` (Admin Console): `http://localhost:5000/admin/v1`
  - `azureAd` — placeholders for dev

### Run Commands

```powershell
# Option A: Docker Compose (all services)
docker compose up

# Option B: Manual
# Terminal 1: DB
docker compose up db   # or local PostgreSQL on 5432/5433

# Terminal 2: API
cd Maemo.Api
dotnet run             # http://localhost:5000

# Terminal 3: Portal
cd frontend
npm install --legacy-peer-deps
ng serve               # http://localhost:4200

# Terminal 4: Admin Console (optional)
cd frontend
ng serve admin-console --port 4300   # http://localhost:4300
```

### Run Tests

```powershell
# Backend
dotnet test Maemo.UnitTests/Maemo.UnitTests.csproj
dotnet test Maemo.IntegrationTests/Maemo.IntegrationTests.csproj

# Frontend
cd frontend && ng test
```

### Setup Gotchas

1. **Port 5432:** Docker Compose maps DB to **5433** to avoid conflict with local PostgreSQL.
2. **Demo tenant:** Call `POST /api/demo/seed` or restart API in Development to create tenant `11111111-1111-1111-1111-111111111111`.
3. **npm:** Use `npm install --legacy-peer-deps`; `.npmrc` has `legacy-peer-deps=true`.
4. **CORS:** Portal (4200) and Admin (4300) are allowed in `Program.cs`.
5. **IndexedDB:** Offline banner may need DB init; clear IndexedDB if "Database not initialized" appears.
6. **PWA icons:** Optional; add icons or remove from manifest if not using PWA.

---

## 8. HOW TO FULLY USE THE SYSTEM

### Main Entry Points

| Entry | URL | Purpose |
|-------|-----|---------|
| **Portal** | http://localhost:4200 | Tenant users: documents, NCRs, risks, audits |
| **Admin Console** | http://localhost:4300 | Platform admins: tenants, users, logs |
| **Swagger** | http://localhost:5000/swagger | Engine + Admin API (Portal excluded by design) |
| **Health** | http://localhost:5000/api/health | Liveness |

### User Journey (Portal)

1. **Login** (Azure AD or dev bypass)
2. **Select tenant** (if multiple)
3. **Dashboard** — summaries, recent activity
4. **Documents** — list, create, upload, version, approve
5. **NCRs** — create, update status, link risks
6. **Risks** — create, score, link NCRs
7. **Audits** — templates, runs, answers, evidence
8. **Tenant Admin** — settings, M365, billing (stub)
9. **Consultant** — dashboard, clients, switch client

### Admin Journey

1. **Login** (PlatformAdmin)
2. **Tenants** — create, update status, branding
3. **Users** — manage per tenant
4. **Logs** — business audit log viewer
5. **Billing** — placeholder (invoices, issue)

### Roles

| Role | Capabilities |
|------|--------------|
| **PlatformAdmin** | Full admin; tenant/user management |
| **Admin / TenantAdmin** | Tenant-scoped admin; settings, users |
| **Consultant** | Multi-client view; dashboard, client switch |
| **User** | Standard portal access |

---

## 9. RISKS & RECOMMENDATIONS

### Technical Risks

1. **Dockerfile paths:** Build failures until paths match repo structure.
2. **Azure AD:** Production auth blocked until real config.
3. **SharePoint:** Incomplete file ops may block SaaS file workflows.
4. **Test coverage:** Regressions likely without more tests.
5. **Dev auth bypass:** Must be disabled in production builds.

### Architectural Concerns

1. **Dual API surface:** Portal vs Engine — ensure consistent behavior and documentation.
2. **Consultant branding:** Domain/API incomplete; may need schema changes.
3. **Billing:** Stub only; real integration will need new design.

### Top 3 Priorities Before Staging

1. **Fix Dockerfile paths** so `docker compose up` and CI builds succeed.
2. **Add `.env.example`** and document all required environment variables.
3. **Wire Azure AD for staging** (or document a clear auth strategy for staging).

---

## APPENDIX: Key File Reference

| Purpose | Path |
|---------|------|
| API host | `Maemo.Api/Program.cs` |
| Portal routes | `Maemo.Api/Portal/*.cs` |
| Engine routes | `Maemo.Api/Engine/**/*.cs` |
| Admin routes | `Maemo.Api/Admin/*.cs` |
| DbContext | `Maemo.Infrastructure/Persistence/` |
| Migrations | `Maemo.Infrastructure/Migrations/` |
| Demo seeder | `Maemo.Infrastructure/Demo/DemoDataSeeder.cs` |
| Frontend config | `frontend/src/environments/` |
| Runbook | `RUNBOOK.md` |
| Project analysis | `PROJECT_ANALYSIS.md` |
| CI workflow | `.github/workflows/ci-maemo.yml` |
