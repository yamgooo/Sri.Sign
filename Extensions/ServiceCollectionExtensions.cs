// -------------------------------------------------------------
// By: Erik Portilla
// Date: 2025-01-27
// -------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Yamgooo.SRI.Sign.Models;

namespace Yamgooo.SRI.Sign.Extensions;

/// <summary>
/// Extensions for configuring the SRI digital signature service
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SRI digital signature service with configuration from appsettings.json
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="sectionName">Section name in appsettings.json (default: "SriSign")</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddSriSignService(
        this IServiceCollection services, 
        IConfiguration configuration, 
        string sectionName = "SriSign")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        var configSection = configuration.GetSection(sectionName);
        if (!configSection.Exists())
        {
            throw new ArgumentException($"Configuration section '{sectionName}' not found in appsettings.json");
        }

        var certificatePath = configSection["CertificatePath"];
        var certificatePassword = configSection["CertificatePassword"];

        if (string.IsNullOrWhiteSpace(certificatePath) || string.IsNullOrWhiteSpace(certificatePassword))
        {
            throw new ArgumentException($"CertificatePath and CertificatePassword must be configured in section '{sectionName}'");
        }

        services.Configure<SriSignConfiguration>(options =>
        {
            options.CertificatePath = certificatePath;
            options.CertificatePassword = certificatePassword;
        });

        // Register the service
        services.AddScoped<ISriSignService, SriSignService>();

        return services;
    }

    /// <summary>
    /// Registers the SRI digital signature service with custom configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Custom configuration</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddSriSignService(
        this IServiceCollection services, 
        SriSignConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        if (!configuration.IsValid())
        {
            var error = configuration.GetValidationError();
            throw new ArgumentException($"Invalid SRI Sign configuration: {error}");
        }

        services.Configure<SriSignConfiguration>(options =>
        {
            options.CertificatePath = configuration.CertificatePath;
            options.CertificatePassword = configuration.CertificatePassword;
        });

        // Register the service
        services.AddScoped<ISriSignService, SriSignService>();

        return services;
    }

    /// <summary>
    /// Registers the SRI digital signature service with minimal configuration (certificate only)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="certificatePath">Certificate path</param>
    /// <param name="certificatePassword">Certificate password</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddSriSignService(
        this IServiceCollection services, 
        string certificatePath, 
        string certificatePassword)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePassword);

        var configuration = new SriSignConfiguration
        {
            CertificatePath = certificatePath,
            CertificatePassword = certificatePassword
        };

        return services.AddSriSignService(configuration);
    }

    /// <summary>
    /// Registers the SRI digital signature service without configuration (for dynamic use)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddSriSignService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the service without configuration for dynamic use
        services.AddScoped<ISriSignService, SriSignService>();

        return services;
    }
}
