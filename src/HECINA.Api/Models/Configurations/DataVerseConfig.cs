namespace HECINA.Api.Models.Configurations;

/// <summary>
/// Configuration for Dataverse integration
/// </summary>
public class DataVerseConfig
{
    public const string SectionName = "DataVerse";

    /// <summary>
    /// Dataverse API endpoint URL
    /// </summary>
    public string ApiEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Client ID for Dataverse authentication
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret for Dataverse authentication
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Tenant ID for Dataverse authentication
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Resource URL for Dataverse
    /// </summary>
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Timeout for Dataverse API calls in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
