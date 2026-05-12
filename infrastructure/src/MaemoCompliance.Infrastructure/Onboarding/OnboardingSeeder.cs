using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Onboarding;
using MaemoCompliance.Application.Onboarding.Dtos;
using MaemoCompliance.Domain.Audits;
using MaemoCompliance.Domain.Documents;
using MaemoCompliance.Domain.Risks;
using MaemoCompliance.Domain.Tenants;
using MaemoCompliance.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Infrastructure.Onboarding;

/// <summary>
/// Implementation of onboarding seeder - seeds tenant-specific data based on onboarding selections.
/// </summary>
public class OnboardingSeeder : IOnboardingSeeder
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public OnboardingSeeder(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public Task SeedAsync(OnboardingRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var now = _dateTimeProvider.UtcNow;
        var userId = _currentUserService.UserId ?? "System";
        return SeedCoreAsync(tenantId, request, userId, now, cancellationToken);
    }

    public Task SeedForTenantAsync(Guid tenantId, OnboardingRequest request, CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        const string userId = "System";
        return SeedCoreAsync(tenantId, request, userId, now, cancellationToken);
    }

    private async Task SeedCoreAsync(
        Guid tenantId,
        OnboardingRequest request,
        string userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Seed document categories based on ISO standards
        await SeedDocumentCategoriesAsync(tenantId, request.IsoStandards, userId, now, cancellationToken);

        // Seed risk categories (standard set)
        await SeedRiskCategoriesAsync(tenantId, userId, now, cancellationToken);

        // Seed audit templates based on ISO standards
        await SeedAuditTemplatesAsync(tenantId, request.IsoStandards, userId, now, cancellationToken);

        // Seed departments based on industry and company size
        await SeedDepartmentsAsync(tenantId, request.Industry, request.CompanySize, userId, now, cancellationToken);

        // Configure default dashboard widgets
        await ConfigureDashboardWidgetsAsync(tenantId, request.IsoStandards, userId, now, cancellationToken);
    }

    private async Task SeedDocumentCategoriesAsync(
        Guid tenantId,
        List<string> isoStandards,
        string userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var categories = new HashSet<string>();

        // Base categories
        categories.Add("Policy");
        categories.Add("Procedure");
        categories.Add("Form");
        categories.Add("Record");
        categories.Add("BBBEE Certificate");

        // ISO-specific categories
        if (isoStandards.Contains("ISO 9001"))
        {
            categories.Add("Quality Manual");
            categories.Add("Quality Procedure");
            categories.Add("Quality Record");
        }

        if (isoStandards.Contains("ISO 14001"))
        {
            categories.Add("Environmental Policy");
            categories.Add("Environmental Procedure");
            categories.Add("Environmental Record");
        }

        if (isoStandards.Contains("ISO 45001"))
        {
            categories.Add("Safety Policy");
            categories.Add("Safety Procedure");
            categories.Add("Safety Record");
        }

        if (isoStandards.Contains("ISO 27001"))
        {
            categories.Add("Information Security Policy");
            categories.Add("Information Security Procedure");
            categories.Add("Information Security Record");
        }

        if (isoStandards.Contains("ISO 31000"))
        {
            categories.Add("Risk Management Policy");
            categories.Add("Risk Treatment Plan");
        }

        // Store categories in TenantSettings (using a JSON array)
        var existingCategories = await _context.TenantSettings
            .Where(ts => ts.TenantId == tenantId && ts.Key == "DocumentCategories")
            .FirstOrDefaultAsync(cancellationToken);

        if (existingCategories == null)
        {
            var categoriesJson = System.Text.Json.JsonSerializer.Serialize(categories.ToList());
            var setting = new TenantSettings
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Key = "DocumentCategories",
                Value = categoriesJson,
                Description = "Document categories configured during onboarding",
                CreatedAt = now,
                CreatedBy = userId
            };

            _context.TenantSettings.Add(setting);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedRiskCategoriesAsync(
        Guid tenantId,
        string userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Risk categories are enums, but we can store preferred ones in settings
        var riskCategories = new[] { "Operational", "Financial", "Compliance", "HealthSafety", "InformationSecurity" };

        var existing = await _context.TenantSettings
            .Where(ts => ts.TenantId == tenantId && ts.Key == "RiskCategories")
            .FirstOrDefaultAsync(cancellationToken);

        if (existing == null)
        {
            var categoriesJson = System.Text.Json.JsonSerializer.Serialize(riskCategories);
            var setting = new TenantSettings
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Key = "RiskCategories",
                Value = categoriesJson,
                Description = "Risk categories configured during onboarding",
                CreatedAt = now,
                CreatedBy = userId
            };

            _context.TenantSettings.Add(setting);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedAuditTemplatesAsync(
        Guid tenantId,
        List<string> isoStandards,
        string userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Get or create a consultant user for audit templates
        var consultantUser = await _context.Users
            .Where(u => u.Role == UserRole.Consultant)
            .FirstOrDefaultAsync(cancellationToken);

        if (consultantUser == null)
        {
            // Create a system consultant user
            consultantUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "system@maemo.com",
                FullName = "System Consultant",
                Role = UserRole.Consultant,
                IsActive = true,
                CreatedAt = now,
                CreatedBy = "System"
            };
            _context.Users.Add(consultantUser);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var consultantId = consultantUser.Id;

        // Create audit templates based on ISO standards
        var templates = new List<(string Name, string Description, List<(string Category, string Question)>)>();

        if (isoStandards.Contains("ISO 9001"))
        {
            templates.Add((
                "ISO 9001:2015 Quality Management System Audit",
                "Comprehensive audit template for ISO 9001:2015 Quality Management System",
                new List<(string, string)>
                {
                    ("Context", "Has the organization determined external and internal issues relevant to its purpose?"),
                    ("Leadership", "Has top management demonstrated leadership and commitment?"),
                    ("Planning", "Are quality objectives established and monitored?"),
                    ("Support", "Are resources provided and maintained?"),
                    ("Operation", "Are processes planned and controlled?"),
                    ("Performance", "Is performance evaluated and monitored?"),
                    ("Improvement", "Are opportunities for improvement identified and implemented?")
                }
            ));
        }

        if (isoStandards.Contains("ISO 14001"))
        {
            templates.Add((
                "ISO 14001:2015 Environmental Management System Audit",
                "Comprehensive audit template for ISO 14001:2015 Environmental Management System",
                new List<(string, string)>
                {
                    ("Context", "Has the organization determined environmental aspects and impacts?"),
                    ("Leadership", "Has environmental policy been established?"),
                    ("Planning", "Are environmental objectives and targets set?"),
                    ("Support", "Are environmental resources and competencies available?"),
                    ("Operation", "Are operational controls in place?"),
                    ("Performance", "Is environmental performance monitored?"),
                    ("Improvement", "Are environmental improvements implemented?")
                }
            ));
        }

        if (isoStandards.Contains("ISO 45001"))
        {
            templates.Add((
                "ISO 45001:2018 Occupational Health and Safety Audit",
                "Comprehensive audit template for ISO 45001:2018 Occupational Health and Safety Management System",
                new List<(string, string)>
                {
                    ("Context", "Has the organization determined OH&S hazards and risks?"),
                    ("Leadership", "Has OH&S policy been established?"),
                    ("Planning", "Are OH&S objectives and targets set?"),
                    ("Support", "Are OH&S resources and competencies available?"),
                    ("Operation", "Are operational controls and emergency preparedness in place?"),
                    ("Performance", "Is OH&S performance monitored and measured?"),
                    ("Improvement", "Are OH&S improvements implemented?")
                }
            ));
        }

        if (isoStandards.Contains("ISO 27001"))
        {
            templates.Add((
                "ISO 27001:2022 Information Security Management System Audit",
                "Comprehensive audit template for ISO 27001:2022 Information Security Management System",
                new List<(string, string)>
                {
                    ("Context", "Has the organization determined information security requirements?"),
                    ("Leadership", "Has information security policy been established?"),
                    ("Planning", "Are information security objectives set?"),
                    ("Support", "Are information security resources available?"),
                    ("Operation", "Are information security controls implemented?"),
                    ("Performance", "Is information security performance monitored?"),
                    ("Improvement", "Are information security improvements implemented?")
                }
            ));
        }

        // Create a generic SHEQ template if multiple standards selected
        if (isoStandards.Count > 1)
        {
            templates.Add((
                "Integrated SHEQ Management System Audit",
                "Comprehensive integrated audit template covering Safety, Health, Environment, and Quality",
                new List<(string, string)>
                {
                    ("Documentation", "Are all management system procedures documented and up to date?"),
                    ("Training", "Have all employees completed required training?"),
                    ("Compliance", "Is the organization compliant with applicable standards?"),
                    ("Safety", "Are all safety equipment and PPE properly maintained?"),
                    ("Environmental", "Are environmental controls and waste management procedures followed?"),
                    ("Process", "Are processes operating within defined parameters?"),
                    ("Incident Management", "Are incidents being reported and investigated?"),
                    ("Risk Management", "Are risks identified, assessed, and controls implemented?")
                }
            ));
        }

        foreach (var (name, description, questions) in templates)
        {
            var existingTemplate = await _context.AuditTemplates
                .Where(t => t.Name == name)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingTemplate == null)
            {
                var template = new AuditTemplate
                {
                    Id = Guid.NewGuid(),
                    ConsultantUserId = consultantId,
                    Name = name,
                    Description = description,
                    CreatedAt = now,
                    CreatedBy = consultantId.ToString()
                };

                _context.AuditTemplates.Add(template);
                await _context.SaveChangesAsync(cancellationToken);

                // Create audit questions
                var auditQuestions = questions.Select(q => new AuditQuestion
                {
                    Id = Guid.NewGuid(),
                    AuditTemplateId = template.Id,
                    Category = q.Category,
                    QuestionText = q.Question,
                    MaxScore = 5,
                    CreatedAt = now,
                    CreatedBy = consultantId.ToString()
                }).ToList();

                _context.AuditQuestions.AddRange(auditQuestions);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task SeedDepartmentsAsync(
        Guid tenantId,
        string industry,
        string companySize,
        string userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var departments = new List<string>();

        // Base departments
        departments.Add("Management");
        departments.Add("Quality Assurance");
        departments.Add("Human Resources");

        // Industry-specific departments
        switch (industry.ToLower())
        {
            case "manufacturing":
                departments.Add("Production");
                departments.Add("Maintenance");
                departments.Add("Health & Safety");
                departments.Add("Logistics");
                break;
            case "construction":
                departments.Add("Site Operations");
                departments.Add("Health & Safety");
                departments.Add("Project Management");
                break;
            case "healthcare":
                departments.Add("Clinical Services");
                departments.Add("Patient Care");
                departments.Add("Health & Safety");
                break;
            case "financial services":
                departments.Add("Operations");
                departments.Add("Compliance");
                departments.Add("Risk Management");
                break;
            case "government":
                departments.Add("Public Affairs");
                departments.Add("Compliance");
                departments.Add("Information Governance");
                break;
            case "technology":
                departments.Add("Development");
                departments.Add("IT Operations");
                departments.Add("Information Security");
                break;
            default:
                departments.Add("Operations");
                departments.Add("Health & Safety");
                break;
        }

        // Company size adjustments
        if (companySize.ToLower() == "small" && departments.Count > 5)
        {
            // For small companies, keep only essential departments
            departments = departments.Take(5).ToList();
        }

        // Create department entities
        foreach (var deptName in departments)
        {
            var existing = await _context.Departments
                .Where(d => d.TenantId == tenantId && d.Name == deptName)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing == null)
            {
                var department = new Department
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Name = deptName,
                    Description = $"Department for {deptName}",
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = userId
                };

                _context.Departments.Add(department);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ConfigureDashboardWidgetsAsync(
        Guid tenantId,
        List<string> isoStandards,
        string userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var widgets = new List<string> { "Documents", "NCRs", "Risks" };

        // Add audit widget if ISO standards include audit-related ones
        if (isoStandards.Any(s => s.Contains("9001") || s.Contains("14001") || s.Contains("45001") || s.Contains("27001")))
        {
            widgets.Add("Audits");
        }

        var widgetsJson = System.Text.Json.JsonSerializer.Serialize(widgets);

        var existing = await _context.TenantSettings
            .Where(ts => ts.TenantId == tenantId && ts.Key == "DashboardWidgets")
            .FirstOrDefaultAsync(cancellationToken);

        if (existing == null)
        {
            var setting = new TenantSettings
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Key = "DashboardWidgets",
                Value = widgetsJson,
                Description = "Default dashboard widgets configured during onboarding",
                CreatedAt = now,
                CreatedBy = userId
            };

            _context.TenantSettings.Add(setting);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

