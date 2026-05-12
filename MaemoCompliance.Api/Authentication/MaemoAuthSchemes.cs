namespace MaemoCompliance.Api.Authentication;

/// <summary>Named authentication schemes for dual JWT (local symmetric + Azure AD) and API keys.</summary>
public static class MaemoAuthSchemes
{
    public const string SmartJwt = "SmartJwt";
    public const string LocalJwt = "LocalJwt";
    public const string AzureAd = "AzureAD";
    public const string ApiKey = "ApiKey";
}
