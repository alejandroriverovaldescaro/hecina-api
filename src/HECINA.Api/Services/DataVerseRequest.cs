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
            
            // URL encode the userNameIdentifier
            var encodedUserIdentifier = Uri.EscapeDataString(userNameIdentifier);
            var query = $"{_config.ApiEndpoint}/api/data/v9.2/contacts?$filter=adx_identity_username eq '{encodedUserIdentifier}'&$select=contactid,firstname,lastname,emailaddress1,new_szvidnumber";
            
            // Create request message with headers to avoid race conditions on shared HttpClient
            using var request = new HttpRequestMessage(HttpMethod.Get, query);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            var response = await _httpClient.SendAsync(request);
            
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
