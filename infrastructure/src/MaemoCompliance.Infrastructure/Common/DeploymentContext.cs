using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Common;
using Microsoft.Extensions.Configuration;

namespace MaemoCompliance.Infrastructure.Common;

public class DeploymentContext : IDeploymentContext
{
    public DeploymentMode Mode { get; }
    public bool EngineModeEnabled { get; }
    public bool AdminModeEnabled { get; }

    public DeploymentContext(IConfiguration configuration)
    {
        var modeString = configuration["Deployment:Mode"];
        
        if (string.IsNullOrWhiteSpace(modeString))
        {
            Mode = DeploymentMode.SaaS;
        }
        else
        {
            // Try to parse the mode string
            if (Enum.TryParse<DeploymentMode>(modeString, ignoreCase: true, out var parsedMode))
            {
                Mode = parsedMode;
            }
            else
            {
                // Invalid value, default to SaaS
                Mode = DeploymentMode.SaaS;
            }
        }

        // Read EngineMode:Enabled from configuration, default to true
        // This allows Maemo to run as a headless Compliance Engine without UI-specific behavior
        var engineModeEnabled = configuration["Deployment:EngineMode:Enabled"];
        if (string.IsNullOrWhiteSpace(engineModeEnabled))
        {
            EngineModeEnabled = true; // Default to enabled
        }
        else
        {
            EngineModeEnabled = bool.TryParse(engineModeEnabled, out var enabled) && enabled;
        }

        // Read AdminMode:Enabled from configuration, default to true
        // This controls whether /admin/v1 endpoints are available (for Codist staff)
        var adminModeEnabled = configuration["Deployment:AdminMode:Enabled"];
        if (string.IsNullOrWhiteSpace(adminModeEnabled))
        {
            AdminModeEnabled = true; // Default to enabled
        }
        else
        {
            AdminModeEnabled = bool.TryParse(adminModeEnabled, out var enabled) && enabled;
        }
    }
}

