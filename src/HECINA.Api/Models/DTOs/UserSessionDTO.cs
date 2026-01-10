namespace HECINA.Api.Models.DTOs;

/// <summary>
/// Data Transfer Object for User Session information from Dataverse
/// </summary>
public class UserSessionDTO
{
    /// <summary>
    /// Session identifier
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// User name identifier from Azure AD B2C (sub claim)
    /// </summary>
    public string UserNameIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Contact information associated with this user session
    /// </summary>
    public ContactDTO Contact { get; set; } = new();

    /// <summary>
    /// Session creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Session last access timestamp
    /// </summary>
    public DateTime LastAccessedAt { get; set; }

    /// <summary>
    /// Indicates if the session is active
    /// </summary>
    public bool IsActive { get; set; }
}
