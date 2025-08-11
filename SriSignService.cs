using Yamgooo.SRI.Sign.Result;
using Yamgooo.SRI.Sign.Models;
using Microsoft.Extensions.Options;

namespace Yamgooo.SRI.Sign;

using FirmaXadesNet;
using FirmaXadesNet.Crypto;
using FirmaXadesNet.Signature.Parameters;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq;
using System.Diagnostics;

/// <summary>
/// Service for XML document signing operations using XAdES signatures.
/// </summary>
public class SriSignService : ISriSignService
{
    private readonly ILogger<SriSignService> _logger;
    private string? _defaultCertificatePath;
    private string? _defaultCertificatePassword;

    /// <summary>
    /// Initializes a new instance of the <see cref="SriSignService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public SriSignService(ILogger<SriSignService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("SriSignService initialized without configuration");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SriSignService"/> class with configuration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configuration">The configuration options.</param>
    public SriSignService(ILogger<SriSignService> logger, IOptions<SriSignConfiguration> configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var signConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        
        var config = signConfiguration.Value;
        _defaultCertificatePath = config.CertificatePath;
        _defaultCertificatePassword = config.CertificatePassword;
        
        _logger.LogInformation("SriSignService initialized with configuration from: {CertificatePath}", _defaultCertificatePath);
    }

    /// <summary>
    /// Sets the default certificate path and password for automatic loading.
    /// </summary>
    /// <param name="certificatePath">The default certificate path.</param>
    /// <param name="password">The default certificate password.</param>
    public void SetDefaultCertificate(string certificatePath, string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(certificatePath);
        ArgumentException.ThrowIfNullOrEmpty(password);

        _defaultCertificatePath = certificatePath;
        _defaultCertificatePassword = password;
        _logger.LogInformation("Default certificate configured: {CertificatePath}", certificatePath);
    }

    /// <summary>
    /// Signs XML content asynchronously using the default certificate configuration.
    /// </summary>
    /// <param name="xmlContent">The XML content to sign.</param>
    /// <returns>The signature result.</returns>
    public async Task<SignatureResult> SignAsync(string xmlContent)
    {
        ArgumentException.ThrowIfNullOrEmpty(xmlContent);

        if (string.IsNullOrEmpty(_defaultCertificatePath) || string.IsNullOrEmpty(_defaultCertificatePassword))
        {
            return SignatureResult.CreateFailure(
                "Default certificate not configured. Use SetDefaultCertificate() or the overload with certificate parameters.");
        }

        return await SignAsync(xmlContent, _defaultCertificatePath, _defaultCertificatePassword);
    }

    /// <summary>
    /// Signs XML content asynchronously with specific certificate.
    /// </summary>
    /// <param name="xmlContent">The XML content to sign.</param>
    /// <param name="certificatePath">The path to the certificate file.</param>
    /// <param name="password">The password for the certificate.</param>
    /// <returns>The signature result.</returns>
    public async Task<SignatureResult> SignAsync(string xmlContent, string certificatePath, string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(xmlContent);
        ArgumentException.ThrowIfNullOrEmpty(certificatePath);
        ArgumentException.ThrowIfNullOrEmpty(password);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Starting digital signature");

            if (!IsValidXml(xmlContent))
                return SignatureResult.CreateFailure("Invalid XML content");

            if (!File.Exists(certificatePath))
            {
                _logger.LogError("Certificate not found at: {CertificatePath}", certificatePath);
                return SignatureResult.CreateFailure($"Certificate not found at: {certificatePath}");
            }

            var certificate = await LoadCertificateAsync(certificatePath, password);
            if (certificate == null)
                return SignatureResult.CreateFailure("Failed to load certificate");

            var xmlDoc = CreateXmlDocument(xmlContent);
            if (xmlDoc == null)
                return SignatureResult.CreateFailure("Failed to create XML document");

            _logger.LogInformation("Signing XML document");

            var signedXmlDoc = await Task.Run(() => SignXmlDocument(xmlDoc, certificate));

            stopwatch.Stop();
            var result = SignatureResult.CreateSuccess(signedXmlDoc.OuterXml, stopwatch.ElapsedMilliseconds);

            _logger.LogInformation("XML signed successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            if (!ValidateSignature(result.SignedXml))
            {
                _logger.LogWarning("Generated signature does not pass validation");
                return SignatureResult.CreateFailure("Generated signature is not valid");
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error in digital signature: {Message}", ex.Message);
            
            return SignatureResult.CreateFailure($"Error in digital signature: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a signed XML document.
    /// </summary>
    /// <param name="signedXml">The signed XML content.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public bool ValidateSignature(string signedXml)
    {
        ArgumentException.ThrowIfNullOrEmpty(signedXml);

        try
        {
            _logger.LogDebug("Validating digital signature");

            if (!IsValidXml(signedXml))
            {
                _logger.LogWarning("Invalid XML format for signature validation");
                return false;
            }

            var xmlDoc = XDocument.Parse(signedXml);
            
            var signatureElements = xmlDoc.Descendants()
                .Where(e => e.Name.LocalName.Contains("Signature") || 
                           e.Name.LocalName.Contains("SignedInfo") ||
                           e.Name.LocalName.Contains("SignatureValue"))
                .ToList();

            if (!signatureElements.Any())
            {
                _logger.LogWarning("No signature elements found in XML");
                return false;
            }

            var signatureValue = xmlDoc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "SignatureValue");

            if (signatureValue == null || string.IsNullOrEmpty(signatureValue.Value))
            {
                _logger.LogWarning("SignatureValue not found or empty in XML");
                return false;
            }

            _logger.LogInformation("Signature validation successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating signature: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Validates if the provided content is valid XML.
    /// </summary>
    /// <param name="xmlContent">The XML content to validate.</param>
    /// <returns>True if valid XML, false otherwise.</returns>
    private static bool IsValidXml(string xmlContent)
    {
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates an XmlDocument from XML content.
    /// </summary>
    /// <param name="xmlContent">The XML content.</param>
    /// <returns>The XmlDocument or null if failed.</returns>
    private static XmlDocument? CreateXmlDocument(string xmlContent)
    {
        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlContent);
            return xmlDoc;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Loads a certificate from a P12/PFX file asynchronously.
    /// </summary>
    /// <param name="certificatePath">The path to the certificate file.</param>
    /// <param name="password">The password for the certificate file.</param>
    /// <returns>The loaded certificate or null if failed.</returns>
    private async Task<X509Certificate2?> LoadCertificateAsync(string certificatePath, string password)
    {
        try
        {
            _logger.LogDebug("Loading certificate from: {CertificatePath}", certificatePath);
            
            return await Task.Run(() => new X509Certificate2(
                certificatePath, 
                password,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load certificate from: {CertificatePath}", certificatePath);
            return null;
        }
    }

    /// <summary>
    /// Signs an XML document using FirmaXadesNet.
    /// </summary>
    /// <param name="xmlDoc">The XML document to sign.</param>
    /// <param name="certificate">The certificate to use for signing.</param>
    /// <returns>The signed XML document.</returns>
    private XmlDocument SignXmlDocument(XmlDocument xmlDoc, X509Certificate2 certificate)
    {
        var xadesService = new XadesService();
        var parameters = CreateSignatureParameters();

        using (parameters.Signer = new Signer(certificate))
        {
            using var inputStream = new MemoryStream();
            xmlDoc.Save(inputStream);
            inputStream.Position = 0;

            var signatureDocument = xadesService.Sign(inputStream, parameters);
            return signatureDocument.Document;
        }
    }

    /// <summary>
    /// Creates signature parameters for XAdES signing.
    /// </summary>
    /// <returns>The configured signature parameters.</returns>
    private static SignatureParameters CreateSignatureParameters()
    {
        return new SignatureParameters
        {
            SignatureMethod = SignatureMethod.RSAwithSHA1,
            DigestMethod = DigestMethod.SHA1,
            SigningDate = DateTime.Now,
            SignaturePackaging = SignaturePackaging.ENVELOPED,
            SignatureCommitments = 
            {
                new SignatureCommitment(SignatureCommitmentType.ProofOfOrigin)
            }
        };
    }
}