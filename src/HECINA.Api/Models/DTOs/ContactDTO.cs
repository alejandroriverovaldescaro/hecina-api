namespace HECINA.Api.Models.DTOs;

/// <summary>
/// Data Transfer Object for Contact information from Dataverse
/// </summary>
public class ContactDTO
{
    /// <summary>
    /// Unique identifier for the contact
    /// </summary>
    public string ContactId { get; set; } = string.Empty;

    /// <summary>
    /// First name of the contact
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name of the contact
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the contact
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// SZV Identification Number - used for authorization validation
    /// </summary>
    public string SZVIdNumber { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD B2C user identifier (subject claim)
    /// </summary>
    public string UserNameIdentifier { get; set; } = string.Empty;
}
