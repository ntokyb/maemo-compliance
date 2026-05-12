namespace MaemoCompliance.Application.Common;

public interface IFeatureFlags
{
    bool BillingEnabled { get; }
    bool SelfServiceSignupEnabled { get; }
}

