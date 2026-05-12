# Maemo Solution Architecture Audit Report
**Date:** 2025-11-19  
**Auditor:** Senior Software Architect  
**Scope:** Complete repository structure analysis and recommendations

---

## Executive Summary

This audit examines the Maemo solution structure, identifying architectural patterns, dependencies, separation of concerns, and areas for improvement. The analysis covers backend (.NET), frontend (Angular), infrastructure, deployment, and CI/CD components.

**Key Findings:**
- ✅ Clean Architecture foundation is present but needs refinement
- ⚠️ Mixed responsibilities in several areas
- ⚠️ No clear separation between Engine, Portal, and Admin surfaces
- ⚠️ Frontend is monolithic (single Angular app)
- ⚠️ Some infrastructure concerns leak into Application layer
- ✅ Good multi-tenancy foundation
- ✅ Engine API surface is well-structured

---

## A. Current State Folder Map

### Root Structure
```
maemo/
├── MaemoCompliance.Api/                    # ASP.NET Core Web API (monolithic)
├── MaemoCompliance.Application/            # Application layer (CQRS + Engine facades)
├── MaemoCompliance.Domain/                 # Domain entities and value objects
├── MaemoCompliance.Infrastructure/         # Infrastructure implementations
├── MaemoCompliance.Workers/                # Background worker services
├── MaemoCompliance.UnitTests/              # Unit test project
├── MaemoCompliance.IntegrationTests/       # Integration test project
├── MaemoCompliance.Engine.Client/          # C# SDK client library
├── MaemoCompliance.Engine.Sample/          # SDK sample application
├── frontend/                     # Angular application (monolithic)
├── deploy/                       # Deployment scripts
├── docker-compose.yml            # Docker Compose for development
├── docker-compose.govonprem.yml  # Docker Compose for GovOnPrem
└── MaemoCompliance.sln                     # Solution file
```

### Detailed Backend Structure

#### MaemoCompliance.Api/
```
MaemoCompliance.Api/
├── Authentication/               # ApiKeyAuthenticationHandler
├── Endpoints/                    # Portal endpoints (/api/*)
│   ├── AuditLogEndpoints.cs
│   ├── AuditsEndpoints.cs
│   ├── BillingEndpoints.cs
│   ├── ConsultantsEndpoints.cs
│   ├── DashboardEndpoints.cs
│   ├── DocumentsEndpoints.cs
│   ├── NcrsEndpoints.cs
│   ├── RisksEndpoints.cs
│   └── TenantsEndpoints.cs
├── Engine/                       # Engine endpoints (/engine/v1/*)
│   └── EngineV1Endpoints.cs     # ⚠️ Single large file (1140+ lines)
├── Middleware/                   # HTTP middleware
│   ├── HealthCheckSecurityMiddleware.cs
│   ├── SecurityHeadersMiddleware.cs
│   └── TenantMiddleware.cs
├── Program.cs                    # Startup configuration
├── ProgramClass.cs               # ⚠️ Duplicate/unused?
├── appsettings.*.json            # Configuration files
└── Dockerfile
```

**Issues:**
- ⚠️ Single `EngineV1Endpoints.cs` file is too large (1140+ lines)
- ⚠️ No separation between Portal (`/api/*`) and Admin (`/admin/*`) endpoints
- ⚠️ All endpoints mixed in same project
- ⚠️ `ProgramClass.cs` appears unused/duplicate

#### MaemoCompliance.Application/
```
MaemoCompliance.Application/
├── AuditLog/                     # Audit logging feature
├── Audits/                       # Audit management feature
│   ├── Commands/
│   ├── Dtos/
│   └── Queries/
├── Billing/                      # Billing abstraction
│   └── IBillingProvider.cs
├── Common/                       # Shared interfaces
│   ├── IApplicationDbContext.cs
│   ├── IAuditLogger.cs
│   ├── ICurrentUserService.cs
│   ├── IDateTimeProvider.cs
│   ├── IDeploymentContext.cs
│   ├── IEncryptionService.cs
│   ├── IFeatureFlags.cs
│   ├── IFileStorageProvider.cs
│   ├── IFileStorageService.cs   # ⚠️ Legacy interface
│   ├── IGraphService.cs
│   └── ITenantProvider.cs
├── Consultants/                  # Consultant feature
├── Dashboard/                    # Dashboard feature
├── Documents/                    # Document feature
├── Engine/                       # Engine facades
│   ├── DocumentsEngine.cs
│   ├── NcrEngine.cs
│   ├── RiskEngine.cs
│   ├── AuditEngine.cs
│   ├── ConsultantEngine.cs
│   └── TenantEngine.cs
├── Ncrs/                         # NCR feature
├── Risks/                        # Risk feature
├── Security/                      # Security abstractions
├── Tenants/                      # Tenant management
├── Webhooks/                     # Webhook abstractions
└── DependencyInjection.cs
```

**Issues:**
- ⚠️ `IFileStorageService` and `IFileStorageProvider` - duplicate abstraction
- ⚠️ `Engine/` folder mixes with feature folders - unclear separation
- ⚠️ No clear distinction between Portal and Admin use cases
- ✅ Good CQRS pattern with Commands/Queries separation

#### MaemoCompliance.Domain/
```
MaemoCompliance.Domain/
├── AuditLog/
├── Audits/
├── Common/                       # BaseEntity, TenantOwnedEntity, DeploymentMode
├── Documents/
├── Ncrs/
├── Risks/
├── Security/                     # ApiKey entity
├── Tenants/
├── Users/                        # User, UserRole, ConsultantTenantLink
└── Webhooks/
```

**Status:** ✅ Well-structured, pure domain entities

#### MaemoCompliance.Infrastructure/
```
MaemoCompliance.Infrastructure/
├── AuditLog/                     # AuditLogger implementation
├── Billing/                      # PayFastBillingProvider
├── Common/                       # Infrastructure services
│   ├── CurrentUserService.cs
│   ├── DeploymentContext.cs
│   ├── FeatureFlags.cs
│   └── SystemDateTimeProvider.cs
├── Configurations/               # EF Core configurations
│   └── [14 configuration files]
├── DependencyInjection.cs
├── FileStorage/                  # LocalFileStorageService
├── Graph/                        # Microsoft Graph integration
├── HealthChecks/
├── Logging/                      # Serilog configuration
├── Migrations/                   # EF Core migrations
├── MultiTenancy/                 # TenantContext, TenantProvider
├── Persistence/                  # DbContext and factory
│   ├── Configurations/          # ⚠️ Duplicate with Configurations/
│   └── Mappings/                 # ⚠️ Duplicate with Configurations/
├── Security/                     # Encryption, ApiKeyService
├── Storage/                      # FileStorageProvider implementations
│   ├── LocalFileStorageProvider.cs
│   └── SharePointFileStorageProvider.cs
└── Webhooks/                     # WebhookDispatcher, WebhookSubscriptionService
```

**Issues:**
- ⚠️ `Configurations/` and `Persistence/Configurations/` and `Persistence/Mappings/` - duplicate locations
- ⚠️ `FileStorage/` vs `Storage/` - unclear separation
- ⚠️ `DeploymentContext` in Infrastructure but used in Application layer interfaces

#### MaemoCompliance.Workers/
```
MaemoCompliance.Workers/
├── Services/
│   ├── ComplianceJobsWorker.cs
│   └── HeartbeatWorker.cs
├── Program.cs
└── Worker.cs
```

**Status:** ✅ Simple, focused structure

### Frontend Structure

#### frontend/
```
frontend/
├── src/
│   ├── app/
│   │   ├── components/          # Shared components
│   │   │   ├── dashboard/
│   │   │   ├── layout/
│   │   │   ├── login/
│   │   │   └── tenant-selector/
│   │   ├── features/            # Feature modules
│   │   │   ├── consultant/      # Consultant portal features
│   │   │   ├── documents/       # Document management
│   │   │   ├── ncrs/            # NCR management
│   │   │   ├── risks/           # Risk management
│   │   │   └── tenant-admin/    # Tenant administration
│   │   ├── guards/              # Route guards
│   │   ├── interceptors/         # HTTP interceptors
│   │   ├── models/               # TypeScript models
│   │   └── services/            # API service clients
│   ├── environments/             # Environment configs
│   └── styles.scss
├── angular.json
├── package.json
└── Dockerfile
```

**Issues:**
- ⚠️ Single Angular app serves both Portal and Consultant views
- ⚠️ No clear separation for Admin Console
- ⚠️ All features mixed together
- ⚠️ Services call `/api/*` endpoints directly (should use `/engine/v1` for external consumption)

### Deployment & Infrastructure

```
deploy/
└── govonprem/
    ├── backup-db.ps1/.sh
    ├── backup-files.ps1/.sh
    ├── restore-db.ps1/.sh
    └── restore-files.ps1/.sh

docker-compose.yml                # Development stack
docker-compose.govonprem.yml      # GovOnPrem stack
```

**Status:** ✅ Basic deployment scripts present

---

## B. Issues & Risks

### 🔴 Critical Issues

1. **No Admin Console Separation**
   - All endpoints in single `MaemoCompliance.Api` project
   - No `/admin/v1` route group
   - Admin functionality mixed with Portal endpoints
   - **Risk:** Cannot deploy Admin Console separately

2. **Monolithic Frontend**
   - Single Angular app for Portal, Consultant, and Admin
   - **Risk:** Cannot deploy frontends independently
   - **Risk:** Larger bundle sizes
   - **Risk:** Shared dependencies cause coupling

3. **Engine Endpoints in Single File**
   - `EngineV1Endpoints.cs` is 1140+ lines
   - **Risk:** Hard to maintain
   - **Risk:** Merge conflicts
   - **Risk:** Difficult to test

4. **Duplicate Configuration Locations**
   - `Infrastructure/Configurations/` vs `Infrastructure/Persistence/Configurations/` vs `Infrastructure/Persistence/Mappings/`
   - **Risk:** Confusion about where to add new configurations
   - **Risk:** Inconsistent patterns

5. **Infrastructure Leaking into Application**
   - `DeploymentContext` implementation in Infrastructure but interface in Application
   - **Risk:** Application layer depends on Infrastructure concepts
   - **Risk:** Hard to test Application layer in isolation

### 🟡 Medium Issues

6. **Duplicate File Storage Abstractions**
   - `IFileStorageService` (legacy) and `IFileStorageProvider` (new)
   - **Risk:** Confusion about which to use
   - **Risk:** Technical debt

7. **No Clear Feature Boundaries**
   - Features organized by domain (Documents, NCRs, Risks) but Engine facades mixed in
   - **Risk:** Unclear where to add new features
   - **Risk:** Cross-cutting concerns not isolated

8. **Frontend Services Call Portal Endpoints**
   - Services use `/api/*` instead of `/engine/v1/*`
   - **Risk:** Tight coupling to Portal API
   - **Risk:** Cannot easily switch to external API

9. **No Shared Library for DTOs**
   - DTOs duplicated between Application and Engine.Client
   - **Risk:** Inconsistency
   - **Risk:** Maintenance burden

10. **Worker Services Not Modular**
    - Single Workers project for all background jobs
    - **Risk:** Cannot scale workers independently
    - **Risk:** Deployment coupling

### 🟢 Low Priority Issues

11. **Unused Files**
    - `MaemoCompliance.Api/ProgramClass.cs` appears unused
    - **Risk:** Confusion

12. **No CI/CD Pipeline Files Visible**
    - No `.github/workflows/` or `.azure-pipelines/` visible
    - **Risk:** Cannot verify CI/CD structure

13. **Documentation Scattered**
    - Multiple markdown files at root
    - **Risk:** Hard to find documentation

---

## C. Recommended Folder Architecture (v2)

### Proposed Structure

```
maemo/
├── src/
│   ├── MaemoCompliance.Api/                        # Main API Gateway
│   │   ├── Engine/                       # Engine API surface
│   │   │   ├── Documents/
│   │   │   ├── Ncrs/
│   │   │   ├── Risks/
│   │   │   ├── Audits/
│   │   │   ├── Consultants/
│   │   │   ├── Tenants/
│   │   │   └── Webhooks/
│   │   ├── Portal/                       # Portal API surface
│   │   │   ├── Documents/
│   │   │   ├── Dashboard/
│   │   │   └── ...
│   │   ├── Admin/                        # Admin API surface
│   │   │   ├── Tenants/
│   │   │   ├── Users/
│   │   │   ├── Billing/
│   │   │   └── System/
│   │   ├── Shared/                       # Shared API components
│   │   │   ├── Authentication/
│   │   │   ├── Middleware/
│   │   │   └── HealthChecks/
│   │   └── Program.cs
│   │
│   ├── MaemoCompliance.Application/               # Application layer
│   │   ├── Engine/                       # Engine facades (API-first)
│   │   │   ├── Documents/
│   │   │   ├── Ncrs/
│   │   │   ├── Risks/
│   │   │   ├── Audits/
│   │   │   ├── Consultants/
│   │   │   └── Tenants/
│   │   ├── Portal/                       # Portal use cases
│   │   │   ├── Documents/
│   │   │   ├── Dashboard/
│   │   │   └── ...
│   │   ├── Admin/                        # Admin use cases
│   │   │   ├── Tenants/
│   │   │   ├── Users/
│   │   │   └── Billing/
│   │   ├── Common/                       # Shared abstractions
│   │   │   ├── Interfaces/
│   │   │   └── Dtos/
│   │   └── DependencyInjection.cs
│   │
│   ├── MaemoCompliance.Domain/                     # Domain layer (unchanged)
│   │   ├── [Domain entities]
│   │   └── Common/
│   │
│   ├── MaemoCompliance.Infrastructure/             # Infrastructure layer
│   │   ├── Persistence/                   # Database
│   │   │   ├── Configurations/           # EF Core configs (consolidated)
│   │   │   ├── Migrations/
│   │   │   └── MaemoComplianceDbContext.cs
│   │   ├── Storage/                       # File storage (consolidated)
│   │   │   ├── LocalFileStorageProvider.cs
│   │   │   └── SharePointFileStorageProvider.cs
│   │   ├── Security/
│   │   ├── MultiTenancy/
│   │   ├── Logging/
│   │   ├── HealthChecks/
│   │   ├── External/                     # External integrations
│   │   │   ├── Graph/
│   │   │   └── Billing/
│   │   └── DependencyInjection.cs
│   │
│   ├── MaemoCompliance.Workers/                    # Background workers
│   │   ├── ComplianceJobs/
│   │   │   └── ComplianceJobsWorker.cs
│   │   ├── Heartbeat/
│   │   │   └── HeartbeatWorker.cs
│   │   └── Program.cs
│   │
│   ├── MaemoCompliance.Shared/                     # NEW: Shared library
│   │   ├── Dtos/                         # Shared DTOs
│   │   ├── Contracts/                    # API contracts
│   │   └── Constants/
│   │
│   └── MaemoCompliance.Engine.Client/              # SDK (references Shared)
│       ├── Clients/
│       └── Models/                       # References MaemoCompliance.Shared.Dtos
│
├── frontend/
│   ├── portal/                           # NEW: Portal Angular app
│   │   ├── src/
│   │   │   ├── app/
│   │   │   │   ├── features/
│   │   │   │   │   ├── documents/
│   │   │   │   │   ├── ncrs/
│   │   │   │   │   ├── risks/
│   │   │   │   │   └── dashboard/
│   │   │   │   └── services/             # Calls /engine/v1/*
│   │   │   └── environments/
│   │   └── angular.json
│   │
│   ├── consultant/                       # NEW: Consultant Angular app
│   │   ├── src/
│   │   │   ├── app/
│   │   │   │   ├── features/
│   │   │   │   │   ├── audit-templates/
│   │   │   │   │   ├── audit-runs/
│   │   │   │   │   ├── clients/
│   │   │   │   │   └── dashboard/
│   │   │   │   └── services/             # Calls /engine/v1/*
│   │   │   └── environments/
│   │   └── angular.json
│   │
│   └── admin/                            # NEW: Admin Console Angular app
│       ├── src/
│       │   ├── app/
│       │   │   ├── features/
│       │   │   │   ├── tenants/
│       │   │   │   ├── users/
│       │   │   │   ├── billing/
│       │   │   │   └── system/
│       │   │   └── services/             # Calls /admin/v1/*
│       │   └── environments/
│       └── angular.json
│
├── tests/
│   ├── MaemoCompliance.UnitTests/                  # Unit tests
│   ├── MaemoCompliance.IntegrationTests/           # Integration tests
│   └── Maemo.E2ETests/                   # NEW: E2E tests
│
├── deploy/
│   ├── docker/
│   │   ├── api.Dockerfile
│   │   ├── workers.Dockerfile
│   │   ├── portal.Dockerfile
│   │   ├── consultant.Dockerfile
│   │   └── admin.Dockerfile
│   ├── kubernetes/                       # NEW: K8s manifests
│   │   ├── api/
│   │   ├── workers/
│   │   ├── portal/
│   │   ├── consultant/
│   │   └── admin/
│   └── scripts/
│       ├── govonprem/
│       └── saas/
│
├── docs/                                 # NEW: Consolidated documentation
│   ├── architecture/
│   ├── deployment/
│   └── api/
│
├── docker-compose.yml                    # Development stack
├── docker-compose.govonprem.yml          # GovOnPrem stack
└── MaemoCompliance.sln
```

### Key Architectural Principles

1. **API Surface Separation**
   - `/engine/v1/*` - External API (versioned, stable)
   - `/api/*` - Portal API (internal, may change)
   - `/admin/v1/*` - Admin API (internal, privileged)

2. **Frontend Separation**
   - Three independent Angular apps
   - Each can be deployed separately
   - Each has its own release cycle

3. **Feature Organization**
   - Features organized by API surface (Engine/Portal/Admin)
   - Clear boundaries
   - Shared code in `Common/`

4. **Infrastructure Consolidation**
   - Single location for EF Core configurations
   - Single location for storage providers
   - External integrations grouped

5. **Shared Library**
   - `MaemoCompliance.Shared` for DTOs and contracts
   - Used by Application, API, and SDK
   - Single source of truth

---

## D. Migration Plan (Step-by-Step)

### Phase 1: Foundation (Week 1-2)

1. **Create MaemoCompliance.Shared Project**
   - Create new class library
   - Move common DTOs from Application
   - Update references

2. **Consolidate Infrastructure Configurations**
   - Move all EF Core configs to `Infrastructure/Persistence/Configurations/`
   - Remove duplicates
   - Update references

3. **Consolidate Storage Providers**
   - Move all storage code to `Infrastructure/Storage/`
   - Remove `FileStorage/` folder
   - Update references

4. **Split Engine Endpoints**
   - Create `MaemoCompliance.Api/Engine/Documents/`, `Ncrs/`, etc.
   - Move endpoint methods to separate files
   - Update `EngineV1Endpoints.cs` to orchestrate

### Phase 2: API Separation (Week 3-4)

5. **Create Portal Endpoints Structure**
   - Create `MaemoCompliance.Api/Portal/` folder
   - Move existing `/api/*` endpoints
   - Organize by feature

6. **Create Admin Endpoints Structure**
   - Create `MaemoCompliance.Api/Admin/` folder
   - Identify admin endpoints (Tenants, Users, Billing)
   - Create `/admin/v1/*` route group
   - Implement admin endpoints

7. **Refactor Application Layer**
   - Create `Application/Portal/` and `Application/Admin/` folders
   - Move use cases to appropriate folders
   - Keep Engine facades in `Application/Engine/`

### Phase 3: Frontend Separation (Week 5-6)

8. **Extract Portal Frontend**
   - Create `frontend/portal/` Angular app
   - Move Portal features
   - Update services to use `/engine/v1/*`
   - Test independently

9. **Extract Consultant Frontend**
   - Create `frontend/consultant/` Angular app
   - Move Consultant features
   - Update services to use `/engine/v1/*`
   - Test independently

10. **Create Admin Frontend**
    - Create `frontend/admin/` Angular app
    - Implement Admin features
    - Use `/admin/v1/*` endpoints
    - Test independently

### Phase 4: Workers & Testing (Week 7-8)

11. **Modularize Workers**
    - Create feature folders in Workers
    - Separate concerns
    - Enable independent scaling

12. **Update Tests**
    - Update unit tests for new structure
    - Update integration tests
    - Create E2E test project

### Phase 5: Deployment & Documentation (Week 9-10)

13. **Update Dockerfiles**
    - Create separate Dockerfiles for each frontend
    - Update docker-compose files
    - Test deployments

14. **Create Kubernetes Manifests**
    - Create K8s manifests for each service
    - Enable independent scaling
    - Test deployments

15. **Consolidate Documentation**
    - Move docs to `docs/` folder
    - Create architecture documentation
    - Update README files

---

## E. Refactoring Priority List

### 🔴 High Priority (Do First)

1. **Split EngineV1Endpoints.cs**
   - **Impact:** High maintainability improvement
   - **Effort:** Low (2-4 hours)
   - **Risk:** Low

2. **Consolidate Infrastructure Configurations**
   - **Impact:** Medium (reduces confusion)
   - **Effort:** Low (1-2 hours)
   - **Risk:** Low

3. **Create MaemoCompliance.Shared Project**
   - **Impact:** High (enables SDK consistency)
   - **Effort:** Medium (4-6 hours)
   - **Risk:** Medium (requires updating references)

4. **Create Admin API Surface**
   - **Impact:** High (enables Admin Console)
   - **Effort:** Medium (8-12 hours)
   - **Risk:** Medium

### 🟡 Medium Priority (Do Next)

5. **Separate Portal and Admin Endpoints**
   - **Impact:** Medium (better organization)
   - **Effort:** Medium (6-8 hours)
   - **Risk:** Low

6. **Refactor Application Layer by Surface**
   - **Impact:** Medium (clearer boundaries)
   - **Effort:** Medium (8-10 hours)
   - **Risk:** Medium

7. **Extract Portal Frontend**
   - **Impact:** High (independent deployment)
   - **Effort:** High (16-20 hours)
   - **Risk:** Medium

8. **Extract Consultant Frontend**
   - **Impact:** High (independent deployment)
   - **Effort:** High (16-20 hours)
   - **Risk:** Medium

### 🟢 Low Priority (Do Later)

9. **Create Admin Frontend**
   - **Impact:** Medium (completes separation)
   - **Effort:** High (20-24 hours)
   - **Risk:** Low

10. **Modularize Workers**
    - **Impact:** Low (scaling optimization)
    - **Effort:** Medium (6-8 hours)
    - **Risk:** Low

11. **Remove Legacy Interfaces**
    - **Impact:** Low (cleanup)
    - **Effort:** Low (2-4 hours)
    - **Risk:** Low

12. **Consolidate Documentation**
    - **Impact:** Low (better organization)
    - **Effort:** Low (2-4 hours)
    - **Risk:** None

---

## Additional Recommendations

### Dependency Management

**Current Dependency Graph:**
```
MaemoCompliance.Api
  ├── MaemoCompliance.Application
  │     └── MaemoCompliance.Domain
  └── MaemoCompliance.Infrastructure
        ├── MaemoCompliance.Application
        └── MaemoCompliance.Domain

MaemoCompliance.Workers
  ├── MaemoCompliance.Application
  └── MaemoCompliance.Infrastructure

MaemoCompliance.Engine.Client
  └── (standalone, references MaemoCompliance.Shared)
```

**Recommended:**
- ✅ Keep current dependency flow (Domain ← Application ← Infrastructure ← Api)
- ✅ Add MaemoCompliance.Shared as shared dependency
- ✅ Engine.Client should reference MaemoCompliance.Shared, not Application

### Testing Strategy

**Current:**
- Unit tests project exists
- Integration tests project exists

**Recommended:**
- Add E2E tests for each frontend
- Add contract tests for Engine API
- Add performance tests for critical paths

### CI/CD Strategy

**Recommended Structure:**
```
.github/workflows/
├── ci-api.yml              # Build & test API
├── ci-portal.yml           # Build & test Portal frontend
├── ci-consultant.yml       # Build & test Consultant frontend
├── ci-admin.yml            # Build & test Admin frontend
├── ci-workers.yml          # Build & test Workers
├── ci-sdk.yml              # Build & test SDK
└── deploy.yml              # Deployment pipeline
```

### Deployment Modes

**Current:**
- Development (docker-compose)
- GovOnPrem (docker-compose.govonprem.yml)

**Recommended:**
- SaaS (Kubernetes)
- Consultant (Kubernetes with branding)
- Enterprise (Kubernetes with custom config)
- GovOnPrem (Docker Compose or K8s)

---

## Conclusion

The Maemo solution has a solid Clean Architecture foundation but needs refinement to support:
- ✅ Engine-first API consumption
- ✅ Separate Portal, Consultant, and Admin frontends
- ✅ Independent deployment and scaling
- ✅ Clear separation of concerns

The recommended migration plan prioritizes high-impact, low-risk changes first, followed by structural improvements that enable independent deployment and scaling.

**Estimated Total Effort:** 10-12 weeks for complete migration  
**Recommended Approach:** Incremental migration, one phase at a time  
**Risk Level:** Medium (managed through phased approach)

---

**Report Generated:** 2025-11-19  
**Next Review:** After Phase 1 completion

