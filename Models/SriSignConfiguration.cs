// -------------------------------------------------------------
// By: Erik Portilla
// Date: 2025-08-07
// -------------------------------------------------------------

namespace Yamgooo.SRI.Sign.Models;

/// <summary>
/// Configuration for the SRI digital signature service
/// </summary>
public class SriSignConfiguration
{
    /// <summary>
    /// Path to the certificate file (.p12 or .pfx)
    /// </summary>
    public string CertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// Certificate password (consider using secure configuration)
    /// </summary>
    public string CertificatePassword { get; set; } = string.Empty;

    /// <summary>
    /// Validates that the configuration is valid
    /// </summary>
    /// <returns>True if the configuration is valid, false otherwise</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(CertificatePath) && 
               !string.IsNullOrWhiteSpace(CertificatePassword) &&
               File.Exists(CertificatePath) &&
               IsValidCertificateFile();
    }

    /// <summary>
    /// Gets an error message if the configuration is not valid
    /// </summary>
    /// <returns>Error message or null if valid</returns>
    public string? GetValidationError()
    {
        if (string.IsNullOrWhiteSpace(CertificatePath))
            return "Certificate path cannot be empty or whitespace";

        if (string.IsNullOrWhiteSpace(CertificatePassword))
            return "Certificate password cannot be empty or whitespace";

        if (!File.Exists(CertificatePath))
            return $"Certificate file does not exist at path: {CertificatePath}";

        if (!IsValidCertificateFile())
            return $"Certificate file is not accessible or invalid: {CertificatePath}";

        return null;
    }

    /// <summary>
    /// Validates the certificate file accessibility and format
    /// </summary>
    private bool IsValidCertificateFile()
    {
        try
        {
            if (!File.Exists(CertificatePath))
                return false;

            var fileInfo = new FileInfo(CertificatePath);
            
            // Check if file is accessible
            using var stream = File.OpenRead(CertificatePath);

            // Check if file has reasonable size (between 1KB and 10MB)
            return fileInfo.Length is >= 1024 and <= 10 * 1024 * 1024;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates the certificate file and attempts to load it
    /// </summary>
    public bool IsValidCertificate()
    {
        try
        {
            if (!IsValid())
                return false;

            // Try to load the certificate to validate it
            using var certificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(CertificatePath, CertificatePassword);
            
            // Check if certificate is not expired
            if (certificate.NotAfter < DateTime.Now)
                return false;

            // Check if certificate has private key
            if (!certificate.HasPrivateKey)
                return false;

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a deep copy of the configuration
    /// </summary>
    /// <returns>A new instance with the same values</returns>
    public SriSignConfiguration Clone()
    {
        return new SriSignConfiguration
        {
            CertificatePath = this.CertificatePath,
            CertificatePassword = this.CertificatePassword
        };
    }
}
