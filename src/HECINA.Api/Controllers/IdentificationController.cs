using HECINA.Api.Repositories;
using HECINA.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HECINA.Api.Controllers;

/// <summary>
/// Controller for handling identification-based operations with security validation
/// </summary>
[ApiController]
[Route("api/[controller]")]
// Note: No [Authorize] attribute here as we perform custom JWT validation in the action method
public class IdentificationController : ControllerBase
{
    private const string BearerScheme = "Bearer ";
    
    private readonly IMedicalExpensesRepository _repository;
    private readonly IJwtHandlerService _jwtHandlerService;
    private readonly IDataVerseRequest _dataVerseRequest;
    private readonly ILogger<IdentificationController> _logger;

    public IdentificationController(
        IMedicalExpensesRepository repository,
        IJwtHandlerService jwtHandlerService,
        IDataVerseRequest dataVerseRequest,
        ILogger<IdentificationController> logger)
    {
        _repository = repository;
        _jwtHandlerService = jwtHandlerService;
        _dataVerseRequest = dataVerseRequest;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves medical expenses by identification number with security validation
    /// </summary>
    /// <param name="identificationNumber">The SZV identification number to query</param>
    /// <param name="skipToken">Optional pagination token</param>
    /// <param name="top">Number of records to return (default: 10)</param>
    /// <returns>Medical expenses if authorized, 403 if unauthorized, 500 on error</returns>
    /// <remarks>
    /// This endpoint enforces end-to-end security:
    /// 1. Validates JWT token from Authorization header using Azure AD B2C
    /// 2. Extracts the userNameIdentifier (sub claim) from the validated token
    /// 3. Fetches the associated Contact from Dataverse
    /// 4. Compares Contact.SZVIdNumber with the requested identificationNumber
    /// 5. Returns 403 Forbidden if they don't match
    /// 6. Proceeds with data retrieval if they match
    /// </remarks>
    [HttpGet("{identificationNumber}")]
    public async Task<IActionResult> GetByIdentificationNumber(
        string identificationNumber, 
        [FromQuery] string? skipToken, 
        [FromQuery] int top = 10,
        [FromHeader(Name = "Authorization")] string? authHeader = null)
    {
        try
        {
            _logger.LogInformation("GetByIdentificationNumber API called");

            // Step 1: Extract JWT token from Authorization header
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith(BearerScheme, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Missing or invalid Authorization header");
                return Unauthorized(new { message = "Missing or invalid Authorization header" });
            }

            var token = authHeader.Substring(BearerScheme.Length).Trim();

            // Step 2: Validate JWT token using Azure AD B2C settings
            var principal = await _jwtHandlerService.ValidateTokenAsync(token);
            if (principal == null)
            {
                _logger.LogWarning("JWT token validation failed");
                return Unauthorized(new { message = "Invalid or expired JWT token" });
            }

            // Step 3: Extract userNameIdentifier (sub claim) from validated token
            var userNameIdentifier = _jwtHandlerService.GetUserNameIdentifier(principal);
            if (string.IsNullOrEmpty(userNameIdentifier))
            {
                _logger.LogWarning("UserNameIdentifier (sub claim) not found in JWT token");
                return Unauthorized(new { message = "User identifier not found in token" });
            }

            _logger.LogInformation("JWT validated successfully. UserNameIdentifier: {UserNameIdentifier}", userNameIdentifier);

            // Step 4: Fetch user session and associated Contact from Dataverse
            var userSession = await _dataVerseRequest.GetUserSessionFromDataVerseAsync(userNameIdentifier);
            if (userSession == null)
            {
                _logger.LogWarning("User session not found in Dataverse for: {UserNameIdentifier}", userNameIdentifier);
                return StatusCode(500, new { message = "Failed to retrieve user information from Dataverse" });
            }

            if (userSession.Contact == null)
            {
                _logger.LogWarning("Contact information not found for user: {UserNameIdentifier}", userNameIdentifier);
                return StatusCode(500, new { message = "Contact information not available" });
            }

            _logger.LogInformation("Contact retrieved from Dataverse for user: {UserNameIdentifier}", userNameIdentifier);

            // Step 5: Compare Contact.SZVIdNumber with requested identificationNumber
            // Use case-sensitive comparison (Ordinal) for identification numbers to ensure exact match
            // SZV identification numbers are standardized and case-sensitive
            if (!string.Equals(userSession.Contact.SZVIdNumber, identificationNumber, StringComparison.Ordinal))
            {
                // Log authorization failure without exposing sensitive ID values
                _logger.LogWarning(
                    "Authorization failed: User's SZVIdNumber does not match requested identificationNumber for user: {UserNameIdentifier}",
                    userNameIdentifier);
                
                return StatusCode(403, new 
                { 
                    message = "Access denied. You are not authorized to access data for this identification number.",
                    detail = "The identification number in the request does not match your registered SZV ID."
                });
            }

            _logger.LogInformation("Authorization successful for user: {UserNameIdentifier}. Proceeding with data retrieval.", userNameIdentifier);

            // Step 6: Authorization passed - proceed with retrieving medical expenses
            var expenses = await _repository.GetExpensesByPersonAsync(identificationNumber, skipToken, top);
            
            _logger.LogInformation("Successfully retrieved {Count} medical expenses for user: {UserNameIdentifier}", 
                expenses?.Count() ?? 0, userNameIdentifier);
            
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetByIdentificationNumber");
            return StatusCode(500, new { message = "An error occurred while processing your request" });
        }
    }
}
