# Servicio de Firma Digital SRI

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![NuGet](https://img.shields.io/badge/nuget-v1.0.0-blue.svg)](https://www.nuget.org/packages/Yamgooo.SRI.Sign)

Una biblioteca profesional de .NET para operaciones de firma digital usando firmas XAdES, específicamente diseñada para facturación electrónica del SRI (Servicio de Rentas Internas) en Ecuador.

También disponible en inglés: [README.md](README.md)

## 🚀 Características

- **Firmas Digitales XAdES**: Soporte completo para Firmas Electrónicas Avanzadas XML
- **Gestión de Certificados**: Carga flexible de certificados desde archivos P12/PFX
- **Operaciones Asíncronas**: Operaciones de firma asíncronas de alto rendimiento
- **Soporte de Configuración**: Múltiples opciones de configuración (appsettings.json, basada en código, dinámica)
- **Validación**: Validación de firma integrada y verificación de certificados
- **Registro de Eventos**: Registro completo con soporte para registro estructurado
- **Manejo de Errores**: Manejo robusto de errores con mensajes detallados
- **Monitoreo de Rendimiento**: Métricas de rendimiento integradas y temporización

## 📦 Instalación

### Paquete NuGet
```bash
dotnet add package Yamgooo.SRI.Sign
```

### Instalación Manual
```bash
git clone https://github.com/yourusername/sri-sign-service.git
cd sri-sign-service
dotnet build
```

## 🛠️ Inicio Rápido

### 1. Uso Básico (Configuración Dinámica)

```csharp
using Yamgooo.SRI.Sign;

// Registrar el servicio
services.AddSriSignService();

// Usar el servicio
var sriSignService = serviceProvider.GetRequiredService<ISriSignService>();

// Establecer certificado dinámicamente
sriSignService.SetDefaultCertificate("ruta/al/certificado.p12", "tu-contraseña");

// Firmar contenido XML
var result = await sriSignService.SignAsync(xmlContent, accessKey);

if (result.Success)
{
    Console.WriteLine($"XML firmado exitosamente en {result.ProcessingTimeMs}ms");
    var signedXml = result.SignedXml;
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}
```

### 2. Uso Directo de Certificado

```csharp
// Firmar con certificado específico (sin configuración necesaria)
var result = await sriSignService.SignAsync(
    xmlContent, 
    accessKey, 
    "ruta/al/certificado.p12", 
    "tu-contraseña"
);
```

### 3. Uso Basado en Configuración

#### appsettings.json
```json
{
  "SriSign": {
    "CertificatePath": "Certificados/certificado.p12",
    "CertificatePassword": "tu-contraseña-segura"
  }
}
```

#### Program.cs
```csharp
// Registrar con configuración
services.AddSriSignService(configuration);

// Usar el servicio
var sriSignService = serviceProvider.GetRequiredService<ISriSignService>();
var result = await sriSignService.SignAsync(xmlContent, accessKey);
```

### 4. Configuración Personalizada

```csharp
var config = new SriSignConfiguration
{
    CertificatePath = "ruta/al/certificado.p12",
    CertificatePassword = "tu-contraseña"
};

services.AddSriSignService(config);
```

## 📋 Referencia de API

### Interfaz ISriSignService

#### Métodos SignAsync

```csharp
// Firmar con configuración de certificado por defecto
Task<SignatureResult> SignAsync(string xmlContent, string accessKey);

// Firmar con certificado específico
Task<SignatureResult> SignAsync(string xmlContent, string accessKey, string certificatePath, string password);
```

#### Validación

```csharp
// Validar un documento XML firmado
bool ValidateSignature(string signedXml);
```

#### Configuración

```csharp
// Establecer certificado por defecto para operaciones posteriores
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

## 🔧 Opciones de Configuración

### Métodos de Registro de Servicio

```csharp
// Desde appsettings.json
services.AddSriSignService(configuration, "SriSign");

// Con objeto de configuración personalizado
services.AddSriSignService(customConfig);

// Con parámetros de certificado directos
services.AddSriSignService("certificado.p12", "contraseña");

// Sin configuración (para uso dinámico)
services.AddSriSignService();
```

### Validación de Configuración

El servicio valida automáticamente:
- Existencia y accesibilidad del archivo de certificado
- Tamaño del archivo de certificado (1KB - 10MB)
- Validez de la contraseña del certificado
- Expiración del certificado
- Disponibilidad de la clave privada

## 📝 Ejemplos

### Ejemplo Completo de Firma de Factura SRI

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
            // Firmar el XML de la factura
            var result = await _sriSignService.SignAsync(invoiceXml, accessKey);
            
            if (result.Success)
            {
                _logger.LogInformation("Factura firmada exitosamente para clave de acceso: {AccessKey}", accessKey);
                
                // Validar la firma
                if (_sriSignService.ValidateSignature(result.SignedXml))
                {
                    _logger.LogInformation("Validación de firma exitosa");
                }
                else
                {
                    _logger.LogWarning("Validación de firma fallida");
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al firmar factura");
            return SignatureResult.CreateFailure(ex.Message, accessKey);
        }
    }
}
```

### Gestión de Múltiples Certificados

```csharp
public class MultiTenantSigningService
{
    private readonly ISriSignService _sriSignService;

    public async Task<SignatureResult> SignForTenantAsync(string xmlContent, string accessKey, string tenantId)
    {
        // Obtener certificado específico del inquilino
        var certificatePath = GetTenantCertificatePath(tenantId);
        var certificatePassword = GetTenantCertificatePassword(tenantId);

        // Firmar con certificado específico del inquilino
        return await _sriSignService.SignAsync(xmlContent, accessKey, certificatePath, certificatePassword);
    }
}
```

## 🔒 Consideraciones de Seguridad

- **Almacenamiento de Certificados**: Almacena certificados de forma segura y nunca los incluyas en el control de versiones
- **Gestión de Contraseñas**: Usa proveedores de configuración seguros (Azure Key Vault, AWS Secrets Manager, etc.)
- **Permisos de Archivo**: Asegúrate de que los archivos de certificado tengan permisos de acceso apropiados
- **Seguridad de Red**: Usa HTTPS para todas las comunicaciones de red
- **Registro de Eventos**: Ten cuidado de no registrar información sensible como contraseñas de certificados

## 🧪 Pruebas

### Ejemplo de Prueba Unitaria

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
    // Agregar más aserciones específicas basadas en tu certificado de prueba
}
```

## 🚀 Rendimiento

El servicio está optimizado para operaciones de alto rendimiento:

- **Operaciones Asíncronas**: Todas las operaciones de I/O son asíncronas
- **Eficiencia de Memoria**: Usa streaming para documentos XML grandes
- **Caché**: La carga de certificados está optimizada
- **Métricas**: Monitoreo de rendimiento integrado

Métricas de rendimiento típicas:
- XML pequeño (< 1KB): ~50-100ms
- XML mediano (1-10KB): ~100-200ms
- XML grande (10-100KB): ~200-500ms

## 📦 Dependencias

- **.NET 9.0**: Framework objetivo
- **FirmaXadesNet**: Implementación de firma XAdES
- **Microsoft.Extensions.Configuration**: Soporte de configuración
- **Microsoft.Extensions.Logging**: Infraestructura de registro
- **Microsoft.Extensions.Options**: Soporte del patrón de opciones

## 🤝 Contribuir

1. Haz un fork del repositorio
2. Crea una rama de características (`git checkout -b feature/caracteristica-increible`)
3. Confirma tus cambios (`git commit -m 'Agregar alguna característica increíble'`)
4. Sube a la rama (`git push origin feature/caracteristica-increible`)
5. Abre una Pull Request

## 📄 Licencia

Este proyecto está licenciado bajo la Licencia MIT - ver el archivo [LICENSE](LICENSE) para más detalles.

## 📞 Soporte

- **Problemas**: [GitHub Issues](https://github.com/yamgooo/sri-sign-service/issues)
- **Documentación**: [Wiki](https://github.com/yamgooo)
- **Email**: erikportillapesantez@outlook.com

---

**Hecho con ❤️ para la comunidad de desarrolladores ecuatorianos**
