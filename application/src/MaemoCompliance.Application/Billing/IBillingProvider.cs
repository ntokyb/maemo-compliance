namespace MaemoCompliance.Application.Billing;

public interface IBillingProvider
{
    Task<string> CreateSubscriptionAsync(Guid tenantId, string plan, string customerEmail);
    Task<bool> CancelSubscriptionAsync(Guid tenantId);
    Task<bool> HandleWebhookAsync(string payload, string signature);
}

