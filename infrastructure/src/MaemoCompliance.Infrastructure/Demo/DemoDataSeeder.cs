using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Demo;
using MaemoCompliance.Domain.Audits;
using MaemoCompliance.Domain.Documents;
using MaemoCompliance.Domain.Ncrs;
using MaemoCompliance.Domain.Risks;
using MaemoCompliance.Domain.Tenants;
using MaemoCompliance.Domain.Users;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MaemoCompliance.Infrastructure.Demo;

/// <summary>
/// Implementation of demo data seeder - creates "Demo Manufacturing Co." tenant with sample data.
/// </summary>
public class DemoDataSeeder : IDemoDataSeeder
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private const string DemoTenantName = "Demo Manufacturing Co.";
    private const string DemoTenantDomain = "demo.manufacturing.co";
    private const string DemoAdminEmail = "admin@demo.manufacturing.co";

    public DemoDataSeeder(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var defaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // CRITICAL: Always check for default tenant ID first (for dev mode compatibility)
        // This ensures the tenant matches TenantMiddleware default tenant ID
        var demoTenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == defaultTenantId, cancellationToken);

        if (demoTenant != null)
        {
            // Default tenant exists - check if we need to seed child entities
            var hasDocuments = await _context.Documents.AnyAsync(d => d.TenantId == demoTenant.Id, cancellationToken);
            if (hasDocuments)
            {
                // Demo data already seeded
                return;
            }
        }

        // Create demo tenant with default tenant ID if it doesn't exist
        // This matches the TenantMiddleware default tenant ID in dev mode
        if (demoTenant == null)
        {
            var modulesEnabled = new[] { "Documents", "NCR", "Risks", "Audits", "Engine", "Webhooks" };
            
            demoTenant = new Tenant
            {
                Id = defaultTenantId, // Use default tenant ID for dev mode compatibility
                Name = DemoTenantName,
                Domain = DemoTenantDomain,
                AdminEmail = DemoAdminEmail,
                IsActive = true,
                Edition = "Standard",
                Plan = "Pilot",
                LicenseExpiryDate = now.AddMonths(3), // License expires in 3 months
                ModulesEnabledJson = JsonSerializer.Serialize(modulesEnabled),
                CreatedAt = now,
                CreatedBy = "System"
            };

            _context.Tenants.Add(demoTenant);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Update existing demo tenant with licensing info if missing
            if (string.IsNullOrWhiteSpace(demoTenant.Edition) || string.IsNullOrWhiteSpace(demoTenant.ModulesEnabledJson))
            {
                // Need to reload with tracking to update
                var trackedTenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.Id == demoTenant.Id, cancellationToken);
                
                if (trackedTenant != null)
                {
                    if (string.IsNullOrWhiteSpace(trackedTenant.Edition))
                    {
                        trackedTenant.Edition = "Standard";
                    }
                    if (string.IsNullOrWhiteSpace(trackedTenant.Plan))
                    {
                        trackedTenant.Plan = "Pilot";
                    }
                    if (!trackedTenant.LicenseExpiryDate.HasValue)
                    {
                        trackedTenant.LicenseExpiryDate = now.AddMonths(3);
                    }
                    if (string.IsNullOrWhiteSpace(trackedTenant.ModulesEnabledJson))
                    {
                        var modulesEnabled = new[] { "Documents", "NCR", "Risks", "Audits", "Engine", "Webhooks" };
                        trackedTenant.ModulesEnabledJson = JsonSerializer.Serialize(modulesEnabled);
                    }
                    
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
        }

        var tenantId = demoTenant.Id;
        var demoUserId = "demo-user-001"; // Placeholder user ID

        // Create demo documents - SHEQ/ISO focused
        var documents = new List<Document>
        {
            new Document
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = "ISO 9001 Quality Manual",
                Category = "Quality",
                Department = "Quality Assurance",
                OwnerUserId = demoUserId,
                ReviewDate = now.AddMonths(6),
                Status = DocumentStatus.Active,
                Version = 1,
                IsCurrentVersion = true,
                FilePlanSeries = "HR",
                FilePlanSubSeries = "Employee Records",
                FilePlanItem = "ER-01",
                CreatedAt = now.AddDays(-30),
                CreatedBy = demoUserId
            },
            new Document
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = "SHEQ Policy",
                Category = "Safety",
                Department = "Health & Safety",
                OwnerUserId = demoUserId,
                ReviewDate = now.AddMonths(12),
                Status = DocumentStatus.Active,
                Version = 2,
                IsCurrentVersion = true,
                FilePlanSeries = "HR",
                FilePlanSubSeries = "Employee Records",
                FilePlanItem = "ER-02",
                CreatedAt = now.AddDays(-15),
                CreatedBy = demoUserId
            },
            new Document
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = "Incident Reporting Procedure",
                Category = "Safety",
                Department = "Health & Safety",
                OwnerUserId = demoUserId,
                ReviewDate = now.AddMonths(3),
                Status = DocumentStatus.Active,
                Version = 1,
                IsCurrentVersion = true,
                CreatedAt = now.AddDays(-10),
                CreatedBy = demoUserId
            },
            new Document
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = "Risk Management Procedure",
                Category = "Compliance",
                Department = "Quality Assurance",
                OwnerUserId = demoUserId,
                ReviewDate = now.AddMonths(6),
                Status = DocumentStatus.Active,
                Version = 1,
                IsCurrentVersion = true,
                CreatedAt = now.AddDays(-5),
                CreatedBy = demoUserId
            },
            new Document
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = "Internal Audit Procedure",
                Category = "Quality",
                Department = "Quality Assurance",
                OwnerUserId = demoUserId,
                ReviewDate = now.AddMonths(12),
                Status = DocumentStatus.Active,
                Version = 1,
                IsCurrentVersion = true,
                CreatedAt = now.AddDays(-20),
                CreatedBy = demoUserId
            }
        };

        _context.Documents.AddRange(documents);
        await _context.SaveChangesAsync(cancellationToken);

        // Create demo NCRs
        var ncrs = new List<Ncr>
        {
            new Ncr
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = "Non-conforming material in production line A",
                Description = "Discovered batch of materials that do not meet quality specifications. Investigation required.",
                Department = "Production",
                OwnerUserId = demoUserId,
                Severity = NcrSeverity.High,
                Status = NcrStatus.Open,
                DueDate = now.AddDays(7),
                Category = NcrCategory.Product,
                CreatedAt = now.AddDays(-2),
                CreatedBy = demoUserId
            },
            new Ncr
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = "Documentation not updated after process change",
                Description = "Process was modified but documentation was not updated accordingly.",
                Department = "Quality Assurance",
                OwnerUserId = demoUserId,
                Severity = NcrSeverity.Medium,
                Status = NcrStatus.InProgress,
                DueDate = now.AddDays(14),
                Category = NcrCategory.Process,
                RootCause = "Lack of change control process",
                CorrectiveAction = "Implement change control procedure",
                CreatedAt = now.AddDays(-10),
                CreatedBy = demoUserId
            },
            new Ncr
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = "Calibration certificate expired",
                Description = "Equipment calibration certificate has expired. Equipment must be recalibrated before use.",
                Department = "Maintenance",
                OwnerUserId = demoUserId,
                Severity = NcrSeverity.Medium,
                Status = NcrStatus.Closed,
                DueDate = now.AddDays(-5),
                ClosedAt = now.AddDays(-1),
                Category = NcrCategory.System,
                RootCause = "Calibration tracking system not properly maintained",
                CorrectiveAction = "Implemented automated calibration reminders",
                CreatedAt = now.AddDays(-20),
                CreatedBy = demoUserId
            },
            new Ncr
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = "Safety training overdue",
                Description = "Several employees have not completed required safety training.",
                Department = "Health & Safety",
                OwnerUserId = demoUserId,
                Severity = NcrSeverity.Low,
                Status = NcrStatus.Open,
                DueDate = now.AddDays(30),
                Category = NcrCategory.Safety,
                CreatedAt = now.AddDays(-5),
                CreatedBy = demoUserId
            }
        };

        _context.Ncrs.AddRange(ncrs);
        await _context.SaveChangesAsync(cancellationToken);

        // Create demo risks
        var risks = new List<Risk>
        {
            new Risk
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = "Supply chain disruption risk",
                Description = "Risk of supply chain disruption due to single-source suppliers.",
                Category = RiskCategory.Operational,
                Cause = "Over-reliance on single suppliers",
                Consequences = "Production delays, increased costs",
                InherentLikelihood = 3,
                InherentImpact = 4,
                InherentScore = 12,
                ExistingControls = "Diversification strategy in progress",
                ResidualLikelihood = 2,
                ResidualImpact = 3,
                ResidualScore = 6,
                OwnerUserId = demoUserId,
                Status = RiskStatus.Analysed,
                CreatedAt = now.AddDays(-15),
                CreatedBy = demoUserId
            },
            new Risk
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = "Data breach risk",
                Description = "Risk of unauthorized access to sensitive customer data.",
                Category = RiskCategory.InformationSecurity,
                Cause = "Insufficient cybersecurity measures",
                Consequences = "Regulatory fines, reputation damage, customer loss",
                InherentLikelihood = 2,
                InherentImpact = 5,
                InherentScore = 10,
                ExistingControls = "Firewall, encryption, access controls",
                ResidualLikelihood = 1,
                ResidualImpact = 3,
                ResidualScore = 3,
                OwnerUserId = demoUserId,
                Status = RiskStatus.Mitigated,
                CreatedAt = now.AddDays(-30),
                CreatedBy = demoUserId
            },
            new Risk
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Title = "Regulatory compliance risk",
                Description = "Risk of non-compliance with industry regulations.",
                Category = RiskCategory.Compliance,
                Cause = "Rapidly changing regulations",
                Consequences = "Fines, operational restrictions",
                InherentLikelihood = 3,
                InherentImpact = 4,
                InherentScore = 12,
                ExistingControls = "Regular compliance audits, legal review",
                ResidualLikelihood = 2,
                ResidualImpact = 2,
                ResidualScore = 4,
                OwnerUserId = demoUserId,
                Status = RiskStatus.Identified,
                CreatedAt = now.AddDays(-7),
                CreatedBy = demoUserId
            }
        };

        _context.Risks.AddRange(risks);
        await _context.SaveChangesAsync(cancellationToken);

        // Link some risks to NCRs
        var ncrRiskLinks = new List<NcrRiskLink>
        {
            new NcrRiskLink
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                NcrId = ncrs[0].Id, // High severity NCR
                RiskId = risks[0].Id, // Supply chain risk
                CreatedAt = now,
                CreatedBy = demoUserId
            },
            new NcrRiskLink
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                NcrId = ncrs[1].Id, // Process NCR
                RiskId = risks[2].Id, // Compliance risk
                CreatedAt = now,
                CreatedBy = demoUserId
            }
        };

        _context.NcrRiskLinks.AddRange(ncrRiskLinks);
        await _context.SaveChangesAsync(cancellationToken);

        // Create demo consultant user (required for audit templates)
        // Note: In a real scenario, this would be a proper user account
        var demoConsultantId = Guid.NewGuid();
        var consultantUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == "consultant@demo.maemo.com", cancellationToken);

        if (consultantUser == null)
        {
            consultantUser = new User
            {
                Id = demoConsultantId,
                Email = "consultant@demo.maemo.com",
                FullName = "Demo Consultant",
                Role = UserRole.Consultant,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "System"
            };

            _context.Users.Add(consultantUser);
            await _context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            demoConsultantId = consultantUser.Id;
        }

        // Create demo audit template - Internal SHEQ Audit
        var auditTemplate = await _context.AuditTemplates
            .FirstOrDefaultAsync(t => t.Name == "Internal SHEQ Audit – Production", cancellationToken);

        if (auditTemplate == null)
        {
            auditTemplate = new AuditTemplate
            {
                Id = Guid.NewGuid(),
                ConsultantUserId = demoConsultantId,
                Name = "Internal SHEQ Audit – Production",
                Description = "Comprehensive SHEQ audit template for production areas covering safety, health, environment, and quality",
                CreatedAt = now,
                CreatedBy = demoConsultantId.ToString()
            };

            _context.AuditTemplates.Add(auditTemplate);
            await _context.SaveChangesAsync(cancellationToken);

            // Create audit questions - 8 questions for comprehensive demo
            var auditQuestions = new List<AuditQuestion>
            {
                new AuditQuestion
                {
                    Id = Guid.NewGuid(),
                    AuditTemplateId = auditTemplate.Id,
                    Category = "Documentation",
                    QuestionText = "Are all quality procedures documented and up to date?",
                    MaxScore = 5,
                    CreatedAt = now,
                    CreatedBy = demoConsultantId.ToString()
                },
                new AuditQuestion
                {
                    Id = Guid.NewGuid(),
                    AuditTemplateId = auditTemplate.Id,
                    Category = "Training",
                    QuestionText = "Have all employees completed required safety training?",
                    MaxScore = 5,
                    CreatedAt = now,
                    CreatedBy = demoConsultantId.ToString()
                },
                new AuditQuestion
                {
                    Id = Guid.NewGuid(),
                    AuditTemplateId = auditTemplate.Id,
                    Category = "Compliance",
                    QuestionText = "Is the organization compliant with ISO 9001:2015 requirements?",
                    MaxScore = 5,
                    CreatedAt = now,
                    CreatedBy = demoConsultantId.ToString()
                },
                new AuditQuestion
                {
                    Id = Guid.NewGuid(),
                    AuditTemplateId = auditTemplate.Id,
                    Category = "Safety",
                    QuestionText = "Are all safety equipment and PPE properly maintained and available?",
                    MaxScore = 5,
                    CreatedAt = now,
                    CreatedBy = demoConsultantId.ToString()
                },
                new AuditQuestion
                {
                    Id = Guid.NewGuid(),
                    AuditTemplateId = auditTemplate.Id,
                    Category = "Environmental",
                    QuestionText = "Are environmental controls and waste management procedures being followed?",
                    MaxScore = 5,
                    CreatedAt = now,
                    CreatedBy = demoConsultantId.ToString()
                },
                new AuditQuestion
                {
                    Id = Guid.NewGuid(),
                    AuditTemplateId = auditTemplate.Id,
                    Category = "Process",
                    QuestionText = "Are production processes operating within defined parameters?",
                    MaxScore = 5,
                    CreatedAt = now,
                    CreatedBy = demoConsultantId.ToString()
                },
                new AuditQuestion
                {
                    Id = Guid.NewGuid(),
                    AuditTemplateId = auditTemplate.Id,
                    Category = "Incident Management",
                    QuestionText = "Are incidents being reported and investigated according to procedure?",
                    MaxScore = 5,
                    CreatedAt = now,
                    CreatedBy = demoConsultantId.ToString()
                },
                new AuditQuestion
                {
                    Id = Guid.NewGuid(),
                    AuditTemplateId = auditTemplate.Id,
                    Category = "Risk Management",
                    QuestionText = "Are risks identified, assessed, and controls implemented?",
                    MaxScore = 5,
                    CreatedAt = now,
                    CreatedBy = demoConsultantId.ToString()
                }
            };

            _context.AuditQuestions.AddRange(auditQuestions);
            await _context.SaveChangesAsync(cancellationToken);

            // Create audit run for demo tenant - marked as completed
            var auditRun = new AuditRun
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AuditTemplateId = auditTemplate.Id,
                StartedAt = now.AddDays(-7),
                CompletedAt = now.AddDays(-2), // Completed 2 days ago
                AuditorUserId = demoUserId,
                CreatedAt = now.AddDays(-7),
                CreatedBy = demoUserId
            };

            _context.AuditRuns.Add(auditRun);
            await _context.SaveChangesAsync(cancellationToken);

            // Create audit answers - at least 5 answers for comprehensive demo
            var auditAnswers = new List<AuditAnswer>
            {
                new AuditAnswer
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    AuditRunId = auditRun.Id,
                    AuditQuestionId = auditQuestions[0].Id, // Documentation
                    Score = 4,
                    Comment = "Most procedures are documented, but some need updates",
                    CreatedAt = now.AddDays(-5),
                    CreatedBy = demoUserId
                },
                new AuditAnswer
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    AuditRunId = auditRun.Id,
                    AuditQuestionId = auditQuestions[1].Id, // Training
                    Score = 3,
                    Comment = "Training records show some gaps",
                    CreatedAt = now.AddDays(-5),
                    CreatedBy = demoUserId
                },
                new AuditAnswer
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    AuditRunId = auditRun.Id,
                    AuditQuestionId = auditQuestions[2].Id, // Compliance
                    Score = 4,
                    Comment = "Generally compliant with minor non-conformances",
                    CreatedAt = now.AddDays(-5),
                    CreatedBy = demoUserId
                },
                new AuditAnswer
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    AuditRunId = auditRun.Id,
                    AuditQuestionId = auditQuestions[3].Id, // Safety
                    Score = 5,
                    Comment = "All safety equipment properly maintained and available",
                    CreatedAt = now.AddDays(-5),
                    CreatedBy = demoUserId
                },
                new AuditAnswer
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    AuditRunId = auditRun.Id,
                    AuditQuestionId = auditQuestions[4].Id, // Environmental
                    Score = 4,
                    Comment = "Environmental controls in place, minor improvements needed",
                    CreatedAt = now.AddDays(-5),
                    CreatedBy = demoUserId
                }
            };

            _context.AuditAnswers.AddRange(auditAnswers);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<DemoSeedResult> SeedDemoWithOutcomeAsync(CancellationToken cancellationToken = default)
    {
        var defaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var demoTenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == defaultTenantId, cancellationToken);

        if (demoTenant != null &&
            await _context.Documents.AnyAsync(d => d.TenantId == defaultTenantId, cancellationToken))
        {
            return new DemoSeedResult
            {
                WasAlreadySeeded = true,
                TenantId = demoTenant.Id,
                AdminEmail = demoTenant.AdminEmail
            };
        }

        await SeedAsync(cancellationToken);

        var after = await _context.Tenants
            .AsNoTracking()
            .FirstAsync(t => t.Id == defaultTenantId, cancellationToken);

        return new DemoSeedResult
        {
            WasAlreadySeeded = false,
            TenantId = after.Id,
            AdminEmail = after.AdminEmail
        };
    }
}

