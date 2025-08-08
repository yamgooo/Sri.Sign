# SRI Digital Signature Service

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![NuGet](https://img.shields.io/badge/nuget-v1.0.0-blue.svg)](https://www.nuget.org/packages/Yamgooo.SRI.Sign)

A professional .NET library for digital signature operations using XAdES signatures, specifically designed for SRI (Servicio de Rentas Internas) electronic invoicing in Ecuador.

## 🚀 Features

- **XAdES Digital Signatures**: Full support for XML Advanced Electronic Signatures
- **Certificate Management**: Flexible certificate loading from P12/PFX files
- **Async Operations**: High-performance asynchronous signature operations
- **Configuration Support**: Multiple configuration options (appsettings.json, code-based, dynamic)
- **Validation**: Built-in signature validation and certificate verification
- **Logging**: Comprehensive logging with structured logging support
- **Error Handling**: Robust error handling with detailed error messages
- **Performance Monitoring**: Built-in performance metrics and timing

## 📦 Installation

### NuGet Package
```bash
dotnet add package Yamgooo.SRI.Sign
```

### Manual Installation
```bash
git clone https://github.com/yourusername/sri-sign-service.git
cd sri-sign-service
dotnet build
```

## 🛠️ Quick Start

### 1. Basic Usage (Dynamic Configuration)

```csharp
using Yamgooo.SRI.Sign;

// Register the service
services.AddSriSignService();

// Use the service
var sriSignService = serviceProvider.GetRequiredService<ISriSignService>();

// Set certificate dynamically
sriSignService.SetDefaultCertificate("path/to/certificate.p12", "your-password");

// Sign XML content
var result = await sriSignService.SignAsync(xmlContent, accessKey);

if (result.Success)
{
    Console.WriteLine($"XML signed successfully in {result.ProcessingTimeMs}ms");
    var signedXml = result.SignedXml;
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}
```

### 2. Direct Certificate Usage

```csharp
// Sign with specific certificate (no configuration needed)
var result = await sriSignService.SignAsync(
    xmlContent, 
    accessKey, 
    "path/to/certificate.p12", 
    "your-password"
);
```

### 3. Configuration-based Usage

#### appsettings.json
```json
{
  "SriSign": {
    "CertificatePath": "Certificates/certificate.p12",
    "CertificatePassword": "your-secure-password"
  }
}
```

#### Program.cs
```csharp
// Register with configuration
services.AddSriSignService(configuration);

// Use the service
var sriSignService = serviceProvider.GetRequiredService<ISriSignService>();
var result = await sriSignService.SignAsync(xmlContent, accessKey);
```

### 4. Custom Configuration

```csharp
var config = new SriSignConfiguration
{
    CertificatePath = "path/to/certificate.p12",
    CertificatePassword = "your-password"
};

services.AddSriSignService(config);
```

## 📋 API Reference

### ISriSignService Interface

#### SignAsync Methods

```csharp
// Sign with default certificate configuration
Task<SignatureResult> SignAsync(string xmlContent, string accessKey);

// Sign with specific certificate
Task<SignatureResult> SignAsync(string xmlContent, string accessKey, string certificatePath, string password);
```

#### Validation

```csharp
// Validate a signed XML document
bool ValidateSignature(string signedXml);
```

#### Configuration

```csharp
// Set default certificate for subsequent operations
void SetDefaultCertificate(string certificatePath, string password);
```

### SignatureResult

```csharp
public class SignatureResult
{
    public bool Success { get; set; }
    public string SignedXml { get; set; }
    public string ErrorMessage { get; set; }
    public string AccessKey { get; set; }
    public DateTime SignatureTimestamp { get; set; }
    public long ProcessingTimeMs { get; set; }
}
```

## 🔧 Configuration Options

### Service Registration Methods

```csharp
// From appsettings.json
services.AddSriSignService(configuration, "SriSign");

// With custom configuration object
services.AddSriSignService(customConfig);

// With direct certificate parameters
services.AddSriSignService("certificate.p12", "password");

// Without configuration (for dynamic use)
services.AddSriSignService();
```

### Configuration Validation

The service automatically validates:
- Certificate file existence and accessibility
- Certificate file size (1KB - 10MB)
- Certificate password validity
- Certificate expiration
- Private key availability

## 📝 Examples

### Complete SRI Invoice Signing Example

```csharp
public class InvoiceSigningService
{
    private readonly ISriSignService _sriSignService;
    private readonly ILogger<InvoiceSigningService> _logger;

    public InvoiceSigningService(ISriSignService sriSignService, ILogger<InvoiceSigningService> logger)
    {
        _sriSignService = sriSignService;
        _logger = logger;
    }

    public async Task<SignatureResult> SignInvoiceAsync(string invoiceXml, string accessKey)
    {
        try
        {
            // Sign the invoice XML
            var result = await _sriSignService.SignAsync(invoiceXml, accessKey);
            
            if (result.Success)
            {
                _logger.LogInformation("Invoice signed successfully for access key: {AccessKey}", accessKey);
                
                // Validate the signature
                if (_sriSignService.ValidateSignature(result.SignedXml))
                {
                    _logger.LogInformation("Signature validation passed");
                }
                else
                {
                    _logger.LogWarning("Signature validation failed");
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing invoice");
            return SignatureResult.CreateFailure(ex.Message, accessKey);
        }
    }
}
```

### Multiple Certificate Management

```csharp
public class MultiTenantSigningService
{
    private readonly ISriSignService _sriSignService;

    public async Task<SignatureResult> SignForTenantAsync(string xmlContent, string accessKey, string tenantId)
    {
        // Get tenant-specific certificate
        var certificatePath = GetTenantCertificatePath(tenantId);
        var certificatePassword = GetTenantCertificatePassword(tenantId);

        // Sign with tenant-specific certificate
        return await _sriSignService.SignAsync(xmlContent, accessKey, certificatePath, certificatePassword);
    }
}
```

## 🔒 Security Considerations

- **Certificate Storage**: Store certificates securely and never commit them to source control
- **Password Management**: Use secure configuration providers (Azure Key Vault, AWS Secrets Manager, etc.)
- **File Permissions**: Ensure certificate files have appropriate access permissions
- **Network Security**: Use HTTPS for all network communications
- **Logging**: Be careful not to log sensitive information like certificate passwords

## 🧪 Testing

### Unit Testing Example

```csharp
[Test]
public async Task SignAsync_WithValidXml_ReturnsSuccess()
{
    // Arrange
    var mockLogger = new Mock<ILogger<SriSignService>>();
    var service = new SriSignService(mockLogger.Object);
    
    var xmlContent = "<test>content</test>";
    var accessKey = "test-access-key";
    var certificatePath = "test-cert.p12";
    var password = "test-password";

    // Act
    var result = await service.SignAsync(xmlContent, accessKey, certificatePath, password);

    // Assert
    Assert.IsNotNull(result);
    // Add more specific assertions based on your test certificate
}
```

## 🚀 Performance

The service is optimized for high-performance operations:

- **Async Operations**: All I/O operations are asynchronous
- **Memory Efficient**: Uses streaming for large XML documents
- **Caching**: Certificate loading is optimized
- **Metrics**: Built-in performance monitoring

Typical performance metrics:
- Small XML (< 1KB): ~50-100ms
- Medium XML (1-10KB): ~100-200ms
- Large XML (10-100KB): ~200-500ms

## 📦 Dependencies

- **.NET 9.0**: Target framework
- **FirmaXadesNet**: XAdES signature implementation
- **Microsoft.Extensions.Configuration**: Configuration support
- **Microsoft.Extensions.Logging**: Logging infrastructure
- **Microsoft.Extensions.Options**: Options pattern support



## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yamgooo/sri-sign-service/issues)
- **Documentation**: [Wiki](https://github.com/yamgooo)
- **Email**: erikportillapesantez@outlook.com

---

**Made with ❤️ for the Ecuadorian developer community**
