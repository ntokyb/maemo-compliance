using MaemoCompliance.Domain.Common;

namespace MaemoCompliance.Application.Common;

public interface IDeploymentContext
{
    DeploymentMode Mode { get; }
    bool IsGovOnPrem => Mode == DeploymentMode.GovOnPrem;
    bool EngineModeEnabled { get; }
    bool AdminModeEnabled { get; }
}

