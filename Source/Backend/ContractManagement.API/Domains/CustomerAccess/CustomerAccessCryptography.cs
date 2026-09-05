using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace ContractManagement.API.Domains.CustomerAccess;

public sealed class CustomerAccessCryptography
{
    private readonly byte[] _hashKey;
    private readonly byte[] _encryptionKey;

    public CustomerAccessCryptography(IOptions<CustomerOtpOptions> options)
    {
        var value = options.Value;
        _hashKey = ReadKey(value.HashKey, 32);
        _encryptionKey = ReadKey(value.EncryptionKey, 32);
    }

    public string HashSecret(string secret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        return Convert.ToHexString(HMACSHA256.HashData(
            _hashKey,
            Encoding.UTF8.GetBytes(secret)));
    }

    public string CreateToken() =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public string CreatePublicChallengeId() => CreateToken();

    public string CreateOtp() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public string EncryptDeliveryPayload(CustomerOtpDeliveryMessage message)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(message);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using var algorithm = new AesGcm(_encryptionKey, tagSizeInBytes: 16);
        algorithm.Encrypt(nonce, plaintext, ciphertext, tag);

        return Convert.ToBase64String(nonce
            .Concat(tag)
            .Concat(ciphertext)
            .ToArray());
    }

    public CustomerOtpDeliveryMessage DecryptDeliveryPayload(string payload)
    {
        var bytes = Convert.FromBase64String(payload);
        if (bytes.Length < 29)
        {
            throw new CryptographicException("OTP delivery payload is invalid.");
        }

        var nonce = bytes[..12];
        var tag = bytes[12..28];
        var ciphertext = bytes[28..];
        var plaintext = new byte[ciphertext.Length];

        using var algorithm = new AesGcm(_encryptionKey, tagSizeInBytes: 16);
        algorithm.Decrypt(nonce, ciphertext, tag, plaintext);
        var decoded = Encoding.UTF8.GetString(plaintext);
        if (decoded.StartsWith('{'))
        {
            return JsonSerializer.Deserialize<CustomerOtpDeliveryMessage>(decoded)
                ?? throw new CryptographicException("OTP delivery payload is invalid.");
        }

        // Drain pre-SMTP outbox rows using the legacy phone/OTP payload format.
        var parts = decoded.Split('\n', 2);
        if (parts.Length != 2)
        {
            throw new CryptographicException("OTP delivery payload is invalid.");
        }

        return new CustomerOtpDeliveryMessage(parts[0], parts[1]);
    }

    private static byte[] ReadKey(string? configuredKey, int length)
    {
        if (!string.IsNullOrWhiteSpace(configuredKey))
        {
            var key = Convert.FromBase64String(configuredKey);
            if (key.Length == length)
            {
                return key;
            }
        }

        // Development and test keys are process-local; production validates config at startup.
        return RandomNumberGenerator.GetBytes(length);
    }
}
