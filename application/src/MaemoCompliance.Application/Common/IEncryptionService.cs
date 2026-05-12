namespace MaemoCompliance.Application.Common;

/// <summary>
/// Service for encrypting and decrypting sensitive data at rest.
/// Used primarily in GovOnPrem mode to protect sensitive tenant fields.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts plain text and returns base64-encoded cipher text.
    /// </summary>
    /// <param name="plainText">The text to encrypt. If null or empty, returns null.</param>
    /// <returns>Base64-encoded cipher text (IV + encrypted data), or null if input is null/empty.</returns>
    /// <exception cref="InvalidOperationException">Thrown if encryption key is not configured.</exception>
    string Encrypt(string? plainText);

    /// <summary>
    /// Decrypts base64-encoded cipher text and returns plain text.
    /// </summary>
    /// <param name="cipherText">The base64-encoded cipher text to decrypt. If null or empty, returns null.</param>
    /// <returns>Decrypted plain text, or null if input is null/empty.</returns>
    /// <exception cref="InvalidOperationException">Thrown if encryption key is not configured.</exception>
    /// <exception cref="CryptographicException">Thrown if decryption fails (invalid cipher text or key).</exception>
    string Decrypt(string? cipherText);
}

