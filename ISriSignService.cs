// -------------------------------------------------------------
// By: Erik Portilla
// Date: 2025-08-07
// -------------------------------------------------------------

using Yamgooo.SRI.Sign.Result;

namespace Yamgooo.SRI.Sign;

/// <summary>
/// Service interface for XML document signing operations.
/// </summary>
public interface ISriSignService
{
    /// <summary>
    /// Signs XML content asynchronously using the default certificate configuration.
    /// </summary>
    /// <param name="xmlContent">The XML content to sign.</param>
    /// <param name="accessKey">The access key for the document.</param>
    /// <returns>The signature result.</returns>
    Task<SignatureResult> SignAsync(string xmlContent, string accessKey);

    /// <summary>
    /// Signs XML content asynchronously with specific certificate.
    /// </summary>
    /// <param name="xmlContent">The XML content to sign.</param>
    /// <param name="accessKey">The access key for the document.</param>
    /// <param name="certificatePath">The path to the certificate file.</param>
    /// <param name="password">The password for the certificate.</param>
    /// <returns>The signature result.</returns>
    Task<SignatureResult> SignAsync(string xmlContent, string accessKey, string certificatePath, string password);

    /// <summary>
    /// Validates a signed XML document.
    /// </summary>
    /// <param name="signedXml">The signed XML content.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    bool ValidateSignature(string signedXml);

    /// <summary>
    /// Sets the default certificate path and password for automatic loading.
    /// </summary>
    /// <param name="certificatePath">The default certificate path.</param>
    /// <param name="password">The default certificate password.</param>
    void SetDefaultCertificate(string certificatePath, string password);
}
