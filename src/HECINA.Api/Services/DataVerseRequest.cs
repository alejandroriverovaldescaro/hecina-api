using HECINA.Api.Models.Configurations;
using HECINA.Api.Models.DTOs;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HECINA.Api.Services;

/// <summary>
/// Service for interacting with Microsoft Dataverse
/// </summary>
public interface IDataVerseRequest
{
    /// <summary>
    /// Retrieves user session information from Dataverse based on the user name identifier
    /// </summary>
    /// <param name="userNameIdentifier">The user name identifier from Azure AD B2C (sub claim)</param>
    /// <returns>UserSessionDTO containing contact information, or null if not found</returns>
    Task<UserSessionDTO?> GetUserSessionFromDataVerseAsync(string userNameIdentifier);
}

public class DataVerseRequest : IDataVerseRequest
{
    private readonly DataVerseConfig _config;
    private readonly ILogger<DataVerseRequest> _logger;
    private readonly HttpClient _httpClient;

    public DataVerseRequest(
        IOptions<DataVerseConfig> config,
        ILogger<DataVerseRequest> logger,
        IHttpClientFactory httpClientFactory)
    {
        _config = config.Value;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("DataVerseClient");
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
    }

    /// <summary>
    /// Retrieves user session from Dataverse
    /// </summary>
    public async Task<UserSessionDTO?> GetUserSessionFromDataVerseAsync(string userNameIdentifier)
    {
        try
        {
            _logger.LogInformation("Fetching user session from Dataverse for user: {UserNameIdentifier}", userNameIdentifier);

            // Get access token for Dataverse
            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("Failed to obtain access token for Dataverse");
                return null;
            }

            // Set up the request to Dataverse
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Query Dataverse for the contact associated with this user
            // Note: Adjust the OData query based on your actual Dataverse schema
            // URL encode the userNameIdentifier to prevent injection attacks
            var encodedUserIdentifier = Uri.EscapeDataString(userNameIdentifier);
            var query = $"{_config.ApiEndpoint}/api/data/v9.2/contacts?$filter=adx_identity_username eq '{encodedUserIdentifier}'&$select=contactid,firstname,lastname,emailaddress1,new_szvidnumber";
            
            var response = await _httpClient.GetAsync(query);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Dataverse API request failed with status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var dataverseResponse = JsonSerializer.Deserialize<DataverseContactResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dataverseResponse?.Value == null || !dataverseResponse.Value.Any())
            {
                _logger.LogWarning("No contact found in Dataverse for user: {UserNameIdentifier}", userNameIdentifier);
                return null;
            }

            // Map the first contact to our DTO
            var contact = dataverseResponse.Value.First();
            var userSession = new UserSessionDTO
            {
                SessionId = Guid.NewGuid().ToString(),
                UserNameIdentifier = userNameIdentifier,
                Contact = new ContactDTO
                {
                    ContactId = contact.ContactId ?? string.Empty,
                    FirstName = contact.FirstName ?? string.Empty,
                    LastName = contact.LastName ?? string.Empty,
                    Email = contact.EmailAddress1 ?? string.Empty,
                    SZVIdNumber = contact.SZVIdNumber ?? string.Empty,
                    UserNameIdentifier = userNameIdentifier
                },
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                IsActive = true
            };

            _logger.LogInformation("Successfully retrieved user session from Dataverse. SZVIdNumber: {SZVIdNumber}", userSession.Contact.SZVIdNumber);
            return userSession;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user session from Dataverse for user: {UserNameIdentifier}", userNameIdentifier);
            return null;
        }
    }

    /// <summary>
    /// Obtains an access token for Dataverse using client credentials flow
    /// </summary>
    private async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            var tokenEndpoint = $"https://login.microsoftonline.com/{_config.TenantId}/oauth2/v2.0/token";
            
            var requestBody = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", _config.ClientId },
                { "client_secret", _config.ClientSecret },
                { "scope", $"{_config.Resource}/.default" },
                { "grant_type", "client_credentials" }
            });

            var response = await _httpClient.PostAsync(tokenEndpoint, requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to obtain access token. Status code: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return tokenResponse?.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obtaining access token for Dataverse");
            return null;
        }
    }

    // Internal classes for JSON deserialization
    private class DataverseContactResponse
    {
        public List<DataverseContact>? Value { get; set; }
    }

    private class DataverseContact
    {
        [JsonPropertyName("contactid")]
        public string? ContactId { get; set; }
        
        [JsonPropertyName("firstname")]
        public string? FirstName { get; set; }
        
        [JsonPropertyName("lastname")]
        public string? LastName { get; set; }
        
        [JsonPropertyName("emailaddress1")]
        public string? EmailAddress1 { get; set; }
        
        [JsonPropertyName("new_szvidnumber")]
        public string? SZVIdNumber { get; set; }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
        
        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
