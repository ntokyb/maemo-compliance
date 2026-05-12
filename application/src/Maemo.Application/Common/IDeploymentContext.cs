using Maemo.Domain.Common;

namespace Maemo.Application.Common;

public interface IDeploymentContext
{
    DeploymentMode Mode { get; }
    bool IsGovOnPrem => Mode == DeploymentMode.GovOnPrem;
    bool EngineModeEnabled { get; }
    bool AdminModeEnabled { get; }
}

