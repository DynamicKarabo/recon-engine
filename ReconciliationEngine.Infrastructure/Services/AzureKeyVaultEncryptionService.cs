using System.Security.Cryptography;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Microsoft.Extensions.Logging;
using ReconciliationEngine.Application.Interfaces;

namespace ReconciliationEngine.Infrastructure.Services;

public class AzureKeyVaultEncryptionService : IEncryptionService
{
    private readonly KeyClient _keyClient;
    private readonly ILogger<AzureKeyVaultEncryptionService> _logger;
    private readonly string _keyName;
    private byte[]? _cachedKey;
    private readonly SemaphoreSlim _keyRefreshLock = new(1, 1);

    public AzureKeyVaultEncryptionService(
        string keyVaultUrl,
        string keyName,
        ILogger<AzureKeyVaultEncryptionService> logger)
    {
        _keyName = keyName;
        _logger = logger;

        var credential = new DefaultAzureCredential();
        _keyClient = new KeyClient(new Uri(keyVaultUrl), credential);
    }

    public async Task InitializeAsync()
    {
        await RefreshKeyAsync();
    }

    private async Task RefreshKeyAsync()
    {
        await _keyRefreshLock.WaitAsync();
        try
        {
            var key = await _keyClient.GetKeyAsync(_keyName);
            
            var keyBytes = Encoding.UTF8.GetBytes(key.Value.Id.ToString());
            using var sha256 = SHA256.Create();
            _cachedKey = sha256.ComputeHash(keyBytes);

            _logger.LogInformation("Encryption key fetched and cached from Azure Key Vault");
        }
        finally
        {
            _keyRefreshLock.Release();
        }
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        if (_cachedKey == null)
            throw new InvalidOperationException("Encryption service not initialized. Call InitializeAsync first.");

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = EncryptAes(plainBytes, _cachedKey);
        return Convert.ToBase64String(encryptedBytes);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        if (_cachedKey == null)
            throw new InvalidOperationException("Encryption service not initialized. Call InitializeAsync first.");

        var cipherBytes = Convert.FromBase64String(cipherText);
        var decryptedBytes = DecryptAes(cipherBytes, _cachedKey);
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private byte[] EncryptAes(byte[] data, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);

        var result = new byte[aes.IV.Length + encrypted.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

        return result;
    }

    private byte[] DecryptAes(byte[] data, byte[] key)
    {
        var ivLength = 16;
        
        var iv = new byte[ivLength];
        var cipher = new byte[data.Length - ivLength];

        Buffer.BlockCopy(data, 0, iv, 0, ivLength);
        Buffer.BlockCopy(data, ivLength, cipher, 0, cipher.Length);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
    }
}
