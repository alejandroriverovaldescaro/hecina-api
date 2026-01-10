namespace HECINA.Api.Models.Configurations;

/// <summary>
/// Configuration for Azure AD B2C authentication
/// </summary>
public class MicrosoftIdentityConfig
{
    public const string SectionName = "AzureAdB2C";

    /// <summary>
    /// Azure AD B2C instance (e.g., https://login.microsoftonline.com/)
    /// </summary>
    public string Instance { get; set; } = string.Empty;

    /// <summary>
    /// The Application (client) ID from Azure AD B2C app registration
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The audience for the JWT token validation
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD B2C Tenant ID
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD B2C domain (e.g., yourtenant.onmicrosoft.com)
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Sign Up Sign In Policy ID (user flow name)
    /// </summary>
    public string SignUpSignInPolicyId { get; set; } = string.Empty;

    /// <summary>
    /// The path to redirect after sign out
    /// </summary>
    public string SignedOutCallbackPath { get; set; } = string.Empty;

    /// <summary>
    /// OpenID Connect metadata endpoint for token validation
    /// </summary>
    public string EndpointOpenIDConnectMetadataDocument { get; set; } = string.Empty;
}
