namespace ProductService.Infrastructure.Messaging;

public sealed class ServiceBusSettings
{
    public const string SectionName = "AzureServiceBus";

    /// <summary>
    /// Full connection string from the Azure portal — Shared Access Policy → Primary Connection String.
    /// Store in Azure Key Vault or environment variable in production, never in appsettings.json.
    /// </summary>
    public string ConnectionString { get; init; } = string.Empty;
}
