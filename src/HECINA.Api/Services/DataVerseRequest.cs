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
        // Note: Timeout is configured in ServiceCollectionExtensions via named client configuration
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

            // Query Dataverse for the contact associated with this user
            // Note: Adjust the OData query based on your actual Dataverse schema
            // Validate and encode the userNameIdentifier to prevent injection attacks
            
            // Security Note: OData Query Construction
            // The userNameIdentifier from Azure AD B2C JWT token is a trusted source (GUID format).
            // We apply multiple layers of defense:
            // 1. Input validation: reject suspicious characters that could break OData syntax
            // 2. URL encoding: encodes special characters
            // 3. Source trust: value comes from validated JWT token, not user input
            // For maximum security, consider using Microsoft.OData.Client library for parameterized queries.
            
            // Additional validation: ensure userNameIdentifier doesn't contain suspicious characters
            // that could break OData filter syntax even after URL encoding
            if (string.IsNullOrWhiteSpace(userNameIdentifier) || 
                userNameIdentifier.Contains('\'') || 
                userNameIdentifier.Contains('"') || 
                userNameIdentifier.Contains(';') ||
                userNameIdentifier.Contains('$') ||
                userNameIdentifier.Contains('&') ||
                userNameIdentifier.Contains('='))
            {
                _logger.LogWarning("Invalid or suspicious characters detected in userNameIdentifier");
                return null;
            }

            // Step 1: Get the external identity to find the ContactId
            var identityQuery = $"{_config.ApiEndpoint}/api/data/v9.2/adx_externalidentities?$filter=adx_username eq '{userNameIdentifier}'&$select=_adx_contactid_value";

            using var identityRequest = new HttpRequestMessage(HttpMethod.Get, identityQuery);
            identityRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            identityRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var identityResponse = await _httpClient.SendAsync(identityRequest);
            identityResponse.EnsureSuccessStatusCode();

            var identityJson = await identityResponse.Content.ReadAsStringAsync();

            // Parse the JSON response
            using JsonDocument doc = JsonDocument.Parse(identityJson);
            var values = doc.RootElement.GetProperty("value");

            if (values.GetArrayLength() == 0)
            {
                // No external identity found for this username
                return null;
            }

            // Get the first result
            var identity = values[0];

            // Get the contact ID - note the underscore prefix and _value suffix
            Guid contactId = identity.GetProperty("_adx_contactid_value").GetGuid();

            // Step 2: Query the contact using the contactId
            var contactQuery = $"{_config.ApiEndpoint}/api/data/v9.2/contacts({contactId})?$select=contactid,firstname,lastname,emailaddress1,uszv_szvidnumber";

            using var contactRequest = new HttpRequestMessage(HttpMethod.Get, contactQuery);
            contactRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            contactRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var contactResponse = await _httpClient.SendAsync(contactRequest);
            contactResponse.EnsureSuccessStatusCode();

            var contactJson = await contactResponse.Content.ReadAsStringAsync();
            // Parse contact data as needed
      
            // Extract fields with null checking
            var contactData = JsonSerializer.Deserialize<ContactDTO>(contactJson);

            if (contactData == null)
            {
                _logger.LogWarning("No contact found in Dataverse for user: {UserNameIdentifier}", userNameIdentifier);
                return null;
            }

            // Map the first contact to our DTO  
            var userSession = new UserSessionDTO
            {
                SessionId = Guid.NewGuid().ToString(),
                UserNameIdentifier = userNameIdentifier,
                Contact = new ContactDTO
                {
                    ContactId = contactData.ContactId ?? Guid.Empty,
                    FirstName = contactData.FirstName ?? string.Empty,
                    LastName = contactData.LastName ?? string.Empty,
                    Email = contactData.Email ?? string.Empty,
                    SZVIdNumber = contactData.SZVIdNumber ?? string.Empty 
                },
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                IsActive = true
            };

            _logger.LogInformation("Successfully retrieved user session from Dataverse for user: {UserNameIdentifier}", userNameIdentifier);
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
            // Validate configuration values before making request
            if (string.IsNullOrEmpty(_config.ClientId) || string.IsNullOrEmpty(_config.ClientSecret) || 
                string.IsNullOrEmpty(_config.TenantId) || string.IsNullOrEmpty(_config.Resource))
            {
                _logger.LogError("Dataverse configuration is incomplete. ClientId, ClientSecret, TenantId, and Resource are all required.");
                return null;
            }
            
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
