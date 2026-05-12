using MaemoCompliance.Application.Common;
using Microsoft.Extensions.Configuration;

namespace MaemoCompliance.Infrastructure.Common;

public class FeatureFlags : IFeatureFlags
{
    public bool BillingEnabled { get; }
    public bool SelfServiceSignupEnabled { get; }

    public FeatureFlags(IConfiguration configuration)
    {
        // Default to true for SaaS, false for GovOnPrem
        // Can be overridden via configuration
        BillingEnabled = configuration.GetValue<bool?>("Features:BillingEnabled") 
            ?? configuration.GetValue<bool>("Features:BillingEnabled", true);
        
        SelfServiceSignupEnabled = configuration.GetValue<bool?>("Features:SelfServiceSignupEnabled") 
            ?? configuration.GetValue<bool>("Features:SelfServiceSignupEnabled", true);
    }
}

