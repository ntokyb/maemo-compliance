namespace MaemoCompliance.Infrastructure.Email;

public class ResendOptions
{
    public const string SectionName = "Resend";

    /// <summary>Resend API key (<c>re_...</c>). Env: <c>Resend__ApiKey</c>.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Verified sender address (e.g. noreply@maemo.codist.co.za).</summary>
    public string FromAddress { get; set; } = string.Empty;
}
