using MaemoCompliance.Application.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace MaemoCompliance.Infrastructure.Security;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly ILogger<AesEncryptionService> _logger;

    public AesEncryptionService(IConfiguration configuration, ILogger<AesEncryptionService> logger)
    {
        _logger = logger;
        
        var encryptionKeyBase64 = configuration["Security:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(encryptionKeyBase64))
        {
            // Log error without exposing the key value
            _logger.LogError("Security:EncryptionKey is not configured. Encryption/decryption will fail.");
            throw new InvalidOperationException(
                "Security:EncryptionKey configuration is required. " +
                "Please provide a 32-byte (256-bit) key encoded as base64.");
        }

        try
        {
            _key = Convert.FromBase64String(encryptionKeyBase64);
            
            // Validate key length (AES-256 requires 32 bytes)
            if (_key.Length != 32)
            {
                // Log error without exposing key details
                _logger.LogError("Encryption key validation failed: key length is {KeyLength} bytes, expected 32 bytes.", _key.Length);
                throw new InvalidOperationException(
                    $"Encryption key must be exactly 32 bytes (256 bits) for AES-256. " +
                    $"Provided key is {_key.Length} bytes.");
            }
        }
        catch (FormatException ex)
        {
            // Log error without exposing the key value
            _logger.LogError(ex, "Failed to parse Security:EncryptionKey as base64.");
            throw new InvalidOperationException(
                "Security:EncryptionKey must be a valid base64-encoded string.", ex);
        }
    }

    public string Encrypt(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return plainText ?? string.Empty;
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var memoryStream = new MemoryStream();
            
            // Write IV first
            memoryStream.Write(aes.IV, 0, aes.IV.Length);
            
            // Then write encrypted data
            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
            using (var writer = new StreamWriter(cryptoStream, Encoding.UTF8))
            {
                writer.Write(plainText);
            }

            // Return IV + ciphertext as base64
            var encryptedBytes = memoryStream.ToArray();
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            // Log error without exposing the plaintext data
            _logger.LogError(ex, "Encryption operation failed. Data length: {DataLength} bytes.", plainText?.Length ?? 0);
            throw new InvalidOperationException("Failed to encrypt sensitive data.", ex);
        }
    }

    public string Decrypt(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return cipherText ?? string.Empty;
        }

        try
        {
            var encryptedBytes = Convert.FromBase64String(cipherText);
            
            // AES IV is 16 bytes
            if (encryptedBytes.Length < 16)
            {
                throw new CryptographicException("Invalid cipher text: too short to contain IV.");
            }

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Extract IV (first 16 bytes)
            var iv = new byte[16];
            Array.Copy(encryptedBytes, 0, iv, 0, 16);
            aes.IV = iv;

            // Extract ciphertext (remaining bytes)
            var ciphertextLength = encryptedBytes.Length - 16;
            var ciphertext = new byte[ciphertextLength];
            Array.Copy(encryptedBytes, 16, ciphertext, 0, ciphertextLength);

            using var decryptor = aes.CreateDecryptor();
            using var memoryStream = new MemoryStream(ciphertext);
            using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cryptoStream, Encoding.UTF8);

            return reader.ReadToEnd();
        }
        catch (FormatException ex)
        {
            // Log error without exposing cipher text
            _logger.LogError(ex, "Failed to parse cipher text as base64. Cipher text length: {CipherLength} bytes.", cipherText?.Length ?? 0);
            throw new CryptographicException("Invalid cipher text format.", ex);
        }
        catch (CryptographicException ex)
        {
            // Log error without exposing cipher text or key details
            _logger.LogError(ex, "Decryption failed - invalid key or corrupted cipher text. Cipher text length: {CipherLength} bytes.", cipherText?.Length ?? 0);
            throw;
        }
        catch (Exception ex)
        {
            // Log error without exposing cipher text
            _logger.LogError(ex, "Decryption failed unexpectedly. Cipher text length: {CipherLength} bytes.", cipherText?.Length ?? 0);
            throw new CryptographicException("Failed to decrypt sensitive data.", ex);
        }
    }
}

