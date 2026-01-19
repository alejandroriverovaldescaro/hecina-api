using HECINA.Api.Models.DTOs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HECINA.Api.Models.DTOs;

/// <summary>
/// Data Transfer Object for Contact information from Dataverse
/// </summary>
public class ContactDTO
{
    [JsonPropertyName("contactid")]
    public Guid? ContactId { get; set; }

    [JsonPropertyName("firstname")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastname")]
    public string? LastName { get; set; }

    [JsonPropertyName("emailaddress1")]
    public string? Email { get; set; }

    [JsonPropertyName("uszv_szvidnumber")]
    public string? SZVIdNumber { get; set; }
}

 