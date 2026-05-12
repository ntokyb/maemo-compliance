using Maemo.Application.Common;

namespace Maemo.Application.Tenants;

/// <summary>
/// Encrypts and decrypts tenant-held secrets using the same rules as other M365 fields.
/// </summary>
public static class TenantSecretProtector
{
    public static string ProtectSecret(string plainText, IDeploymentContext deploymentContext, IEncryptionService? encryptionService)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        if (deploymentContext.IsGovOnPrem)
        {
            if (encryptionService == null)
            {
                throw new InvalidOperationException(
                    "Encryption service is required for GovOnPrem mode. Configure Security:EncryptionKey.");
            }

            return encryptionService.Encrypt(plainText);
        }

        if (encryptionService != null)
        {
            return encryptionService.Encrypt(plainText);
        }

        throw new InvalidOperationException(
            "Security:EncryptionKey must be configured to store SharePoint client secrets.");
    }

    public static string? UnprotectSecret(string? stored, IDeploymentContext deploymentContext, IEncryptionService? encryptionService)
    {
        if (string.IsNullOrEmpty(stored))
        {
            return stored;
        }

        if (deploymentContext.IsGovOnPrem)
        {
            if (encryptionService == null)
            {
                throw new InvalidOperationException(
                    "Encryption service is required for GovOnPrem mode. Configure Security:EncryptionKey.");
            }

            return encryptionService.Decrypt(stored);
        }

        if (encryptionService != null)
        {
            try
            {
                return encryptionService.Decrypt(stored);
            }
            catch
            {
                // Legacy plaintext in SaaS DB
                return stored;
            }
        }

        return stored;
    }
}
