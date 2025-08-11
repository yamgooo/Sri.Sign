// -------------------------------------------------------------
// By: Erik Portilla
// Date: 2025-08-07
// -------------------------------------------------------------

namespace Yamgooo.SRI.Sign.Result;

/// <summary>
/// Result of a digital signature operation
/// </summary>
public class SignatureResult
{
    /// <summary>
    /// Indicates whether the signature operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The signed XML content
    /// </summary>
    public string SignedXml { get; set; } = string.Empty;

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the signature was created
    /// </summary>
    public DateTime SignatureTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Creates a successful signature result
    /// </summary>
    public static SignatureResult CreateSuccess(string signedXml, long processingTimeMs = 0)
    {
        return new SignatureResult
        {
            Success = true,
            SignedXml = signedXml,
            ProcessingTimeMs = processingTimeMs
        };
    }

    /// <summary>
    /// Creates a failed signature result
    /// </summary>
    public static SignatureResult CreateFailure(string errorMessage)
    {
        return new SignatureResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}