using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using TFM.ApplePay.Configuration;
using TFM.ApplePay.Interfaces;
using TFM.ApplePay.Models.DecryptedToken;
using TFM.ApplePay.Models.PaymentToken;

namespace TFM.ApplePay.Services;

public class ApplePayTokenDecryptionService : IApplePayTokenDecryptionService
{
    private const string MerchantIdExtensionOid = "1.2.840.113635.100.6.32";

    private readonly ApplePayOptions _options;
    private readonly ILogger<ApplePayTokenDecryptionService> _logger;

    public ApplePayTokenDecryptionService(
        IOptions<ApplePayOptions> options,
        ILogger<ApplePayTokenDecryptionService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public DecryptedPaymentData DecryptToken(ApplePayPaymentToken token)
    {
        if (token.PaymentData.Version != "EC_v1")
            throw new NotSupportedException(
                $"Token version '{token.PaymentData.Version}' is not supported. Only EC_v1 is supported.");

        if (string.IsNullOrEmpty(token.PaymentData.Header.EphemeralPublicKey))
            throw new ArgumentException("EphemeralPublicKey is required for EC_v1 tokens.");

        // Step 1: Load the payment processing certificate
        using var cert = LoadPaymentProcessingCertificate();

        // Step 2: Get the ECDH private key directly from the certificate
        using var merchantEcdh = GetEcDiffieHellmanKey(cert);

        // Step 3: Parse the ephemeral public key (SPKI or raw EC point)
        byte[] ephemeralPublicKeyBytes = Convert.FromBase64String(token.PaymentData.Header.EphemeralPublicKey);
        using var ephemeralEcdh = ParseEphemeralPublicKey(ephemeralPublicKeyBytes);

        // Step 4: Perform ECDH key agreement to derive the shared secret
        byte[] sharedSecret = merchantEcdh.DeriveRawSecretAgreement(ephemeralEcdh.PublicKey);

        // Step 5: Extract the pre-computed merchant ID hash from the certificate
        byte[] merchantIdHash = ExtractMerchantIdHash(cert);

        // Step 6: Derive the symmetric key using NIST SP 800-56A Concatenation KDF
        byte[] symmetricKey = DeriveSymmetricKey(sharedSecret, merchantIdHash);

        // Step 7: Decrypt the payment data using AES-256-GCM
        byte[] encryptedData = Convert.FromBase64String(token.PaymentData.Data);
        byte[] decryptedBytes = DecryptAesGcm(symmetricKey, encryptedData);

        // Step 8: Parse the decrypted JSON
        string decryptedJson = Encoding.UTF8.GetString(decryptedBytes);
        _logger.LogDebug("Token decrypted successfully");

        return JsonSerializer.Deserialize<DecryptedPaymentData>(decryptedJson)
            ?? throw new InvalidOperationException("Decrypted token data was null.");
    }

    /// <summary>
    /// Parses an ephemeral public key that may be either a raw uncompressed EC point
    /// (65 bytes: 0x04 || X || Y) or a DER-encoded SubjectPublicKeyInfo structure
    /// (91 bytes for P-256). Apple provides X.509 SubjectPublicKeyInfo, but we handle
    /// both formats defensively.
    /// 
    /// Note: .NET's ECDiffieHellman.ImportSubjectPublicKeyInfo rejects SPKI with the
    /// generic ecPublicKey OID, so we extract the raw EC point manually in both cases.
    /// </summary>
    private static ECDiffieHellman ParseEphemeralPublicKey(byte[] keyBytes)
    {
        ECPoint q;

        if (keyBytes.Length == 65 && keyBytes[0] == 0x04)
        {
            q = new ECPoint
            {
                X = keyBytes[1..33],
                Y = keyBytes[33..65]
            };
        }
        else
        {
            // SubjectPublicKeyInfo DER structure for P-256:
            //   SEQUENCE
            //     SEQUENCE (AlgorithmIdentifier)
            //       OID ecPublicKey
            //       OID prime256v1
            //     BIT STRING
            //       0x00 (unused bits)
            //       0x04 || X (32 bytes) || Y (32 bytes)
            // The uncompressed point is always the trailing 65 bytes.

            if (keyBytes.Length < 65)
                throw new ArgumentException(
                    $"Ephemeral public key data is too short ({keyBytes.Length} bytes).");

            int offset = keyBytes.Length - 65;

            if (keyBytes[offset] != 0x04)
                throw new ArgumentException(
                    $"Could not locate uncompressed EC point (0x04) marker at expected " +
                    $"offset {offset}, found 0x{keyBytes[offset]:X2}.");

            q = new ECPoint
            {
                X = keyBytes[(offset + 1)..(offset + 33)],
                Y = keyBytes[(offset + 33)..(offset + 65)]
            };
        }

        return ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = q
        });
    }

    /// <summary>
    /// Gets an ECDiffieHellman instance from the certificate. Tries the native ECDH key
    /// first (Apple Pay payment processing certs use key agreement), then falls back to
    /// extracting from ECDSA via PKCS#8 export for certs that only expose a signing key type.
    /// </summary>
    private static ECDiffieHellman GetEcDiffieHellmanKey(X509Certificate2 cert)
    {
        var ecdh = cert.GetECDiffieHellmanPrivateKey();
        if (ecdh is not null)
            return ecdh;

        using var ecdsa = cert.GetECDsaPrivateKey();
        if (ecdsa is not null)
        {
            byte[] pkcs8 = ecdsa.ExportPkcs8PrivateKey();
            try
            {
                var ecdhFromEcdsa = ECDiffieHellman.Create();
                ecdhFromEcdsa.ImportPkcs8PrivateKey(pkcs8, out _);
                return ecdhFromEcdsa;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(pkcs8);
            }
        }

        throw new InvalidOperationException(
            "Payment processing certificate does not contain an EC private key. " +
            "Apple Pay EC_v1 requires an ECC (P-256) payment processing certificate.");
    }

    /// <summary>
    /// Extracts the merchant ID hash from the certificate's Apple merchant ID extension.
    /// Apple embeds a pre-computed hex-encoded SHA-256 hash of the merchant ID in the
    /// certificate as a UTF8String — this must be hex-decoded, not hashed again.
    /// </summary>
    private static byte[] ExtractMerchantIdHash(X509Certificate2 cert)
    {
        foreach (var ext in cert.Extensions)
        {
            if (ext.Oid?.Value != MerchantIdExtensionOid)
                continue;

            var reader = new AsnReader(ext.RawData, AsnEncodingRules.DER);
            string merchantIdHex = reader.ReadCharacterString(UniversalTagNumber.UTF8String);

            return Convert.FromHexString(merchantIdHex);
        }

        throw new InvalidOperationException(
            "Payment processing certificate does not contain the Apple merchant ID extension " +
            $"(OID {MerchantIdExtensionOid}).");
    }

    /// <summary>
    /// NIST SP 800-56A Section 5.8.1 — Single-pass Concatenation KDF.
    /// Only one iteration is needed since SHA-256 output (256 bits) matches the required key size.
    /// 
    /// key = SHA-256(counter || Z || algorithmID || partyUInfo || partyVInfo)
    /// 
    /// Apple's format uses a single-byte length prefix on algorithmID and no length
    /// prefixes on partyUInfo or partyVInfo.
    /// </summary>
    private static byte[] DeriveSymmetricKey(byte[] sharedSecret, byte[] merchantIdHash)
    {
        using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        sha256.AppendData([0x00, 0x00, 0x00, 0x01]);       // counter (4 bytes BE)
        sha256.AppendData(sharedSecret);                     // Z
        sha256.AppendData([0x0D]);                           // algorithmID length byte (13)
        sha256.AppendData("id-aes256-GCM"u8);               // algorithmID value
        sha256.AppendData("Apple"u8);                        // partyUInfo
        sha256.AppendData(merchantIdHash);                   // partyVInfo

        return sha256.GetHashAndReset();
    }

    /// <summary>
    /// Decrypt using AES-256-GCM with a 16-byte zero IV per Apple's spec.
    /// Uses BouncyCastle because Windows CNG restricts AesGcm to 12-byte nonces.
    /// </summary>
    private static byte[] DecryptAesGcm(byte[] key, byte[] encryptedData)
    {
        const int gcmTagSizeBits = 128;
        byte[] iv = new byte[16];

        var cipher = new GcmBlockCipher(new AesEngine());
        var parameters = new AeadParameters(
            new KeyParameter(key),
            gcmTagSizeBits,
            iv,
            null);
        cipher.Init(false, parameters);

        byte[] output = new byte[cipher.GetOutputSize(encryptedData.Length)];
        int len = cipher.ProcessBytes(encryptedData, 0, encryptedData.Length, output, 0);
        len += cipher.DoFinal(output, len);

        byte[] result = new byte[len];
        Array.Copy(output, result, len);
        return result;
    }

    private X509Certificate2 LoadPaymentProcessingCertificate()
    {
        if (!string.IsNullOrEmpty(_options.PaymentProcessingCertificateThumbprint))
            return LoadCertificateFromStore(_options.PaymentProcessingCertificateThumbprint);

        return new ManagedCertificate(
            _options.PaymentProcessingCertificatePath,
            _options.PaymentProcessingCertificatePassword).Certificate;
    }

    private static X509Certificate2 LoadCertificateFromStore(string thumbprint)
    {
        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);

        var certs = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
        if (certs.Count == 0)
            throw new InvalidOperationException(
                $"Certificate with thumbprint '{thumbprint}' not found in LocalMachine/My store.");

        return certs[0];
    }

    /// <summary>
    /// Loads a certificate entirely in memory without relying on OS-specific certificate
    /// stores or keychain access. Supports PFX/P12 bundles and PEM certificate + key file pairs.
    /// EphemeralKeySet ensures the private key never touches disk or OS keychain.
    /// </summary>
    private sealed class ManagedCertificate : IDisposable
    {
        private bool _disposed;

        public X509Certificate2 Certificate { get; }

        /// <summary>
        /// Load from a PFX/P12 or PEM file path.
        /// For PEM, pass the private key file path as <paramref name="password"/>
        /// prefixed with "keyfile:" (e.g. "keyfile:/path/to/key.pem").
        /// For PFX/P12, pass the decryption password directly.
        /// </summary>
        public ManagedCertificate(string path, string? password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            if (!File.Exists(path))
                throw new FileNotFoundException($"Certificate file not found: {path}");

            byte[] fileBytes = File.ReadAllBytes(path);

            Certificate = IsPemFile(path, fileBytes)
                ? LoadFromPem(fileBytes, password)
                : LoadFromPfx(fileBytes, password);
        }

        private static X509Certificate2 LoadFromPfx(byte[] pfxBytes, string? password)
        {
            return new X509Certificate2(
                pfxBytes,
                password,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
        }

        private static X509Certificate2 LoadFromPem(byte[] pemBytes, string? keyPathOrPem)
        {
            string certPem = Encoding.UTF8.GetString(pemBytes);

            if (!string.IsNullOrEmpty(keyPathOrPem) &&
                keyPathOrPem.StartsWith("keyfile:", StringComparison.OrdinalIgnoreCase))
            {
                string keyFilePath = keyPathOrPem["keyfile:".Length..];
                if (!File.Exists(keyFilePath))
                    throw new FileNotFoundException($"Private key file not found: {keyFilePath}");

                string keyPem = File.ReadAllText(keyFilePath);
                return CreateFromPemPair(certPem, keyPem);
            }

            if (ContainsPrivateKey(certPem))
                return CreateFromPemPair(certPem, certPem);

            throw new ArgumentException(
                "PEM file contains no private key. Provide the key file path via " +
                "the password parameter prefixed with 'keyfile:' (e.g. \"keyfile:/path/to/key.pem\").");
        }

        private static X509Certificate2 CreateFromPemPair(string certPem, string keyPem)
        {
            using var baseCert = X509Certificate2.CreateFromPem(certPem, keyPem);

            // Re-import via PFX with EphemeralKeySet to ensure the private key is
            // fully in-memory and consistent across all platforms.
            byte[] pfxBytes = baseCert.Export(X509ContentType.Pfx);
            try
            {
                return new X509Certificate2(
                    pfxBytes,
                    (string?)null,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.EphemeralKeySet);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(pfxBytes);
            }
        }

        private static bool IsPemFile(string path, byte[] fileBytes)
        {
            if (path.EndsWith(".pem", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".crt", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".key", StringComparison.OrdinalIgnoreCase))
                return true;

            if (fileBytes.Length < 11)
                return false;

            ReadOnlySpan<byte> span = fileBytes.AsSpan();
            int start = 0;

            if (span.Length >= 3 && span[0] == 0xEF && span[1] == 0xBB && span[2] == 0xBF)
                start = 3;

            while (start < span.Length && (span[start] == (byte)' ' || span[start] == (byte)'\t' ||
                                            span[start] == (byte)'\r' || span[start] == (byte)'\n'))
                start++;

            return span[start..].StartsWith("-----BEGIN "u8);
        }

        private static bool ContainsPrivateKey(string pem)
        {
            return pem.Contains("-----BEGIN PRIVATE KEY-----", StringComparison.Ordinal) ||
                   pem.Contains("-----BEGIN EC PRIVATE KEY-----", StringComparison.Ordinal) ||
                   pem.Contains("-----BEGIN ENCRYPTED PRIVATE KEY-----", StringComparison.Ordinal);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Certificate.Dispose();
        }
    }
}