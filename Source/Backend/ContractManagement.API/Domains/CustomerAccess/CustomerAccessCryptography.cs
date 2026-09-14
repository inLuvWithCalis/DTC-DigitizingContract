using System.Security.Cryptography;
using System.Text;
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

    public byte[] EncryptScalar(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(value);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using var algorithm = new AesGcm(_encryptionKey, tagSizeInBytes: 16);
        algorithm.Encrypt(nonce, plaintext, ciphertext, tag);

        return nonce
            .Concat(tag)
            .Concat(ciphertext)
            .ToArray();
    }

    public string DecryptScalar(byte[] envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (envelope.Length < 28)
        {
            throw new CryptographicException("Encrypted scalar envelope is invalid.");
        }

        var nonce = envelope[..12];
        var tag = envelope[12..28];
        var ciphertext = envelope[28..];
        var plaintext = new byte[ciphertext.Length];

        using var algorithm = new AesGcm(_encryptionKey, tagSizeInBytes: 16);
        algorithm.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
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
