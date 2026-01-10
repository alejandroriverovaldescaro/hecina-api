# Security and Validation Workflow Documentation

## Overview

This document describes the end-to-end security and validation workflow implemented for the HECINA API. The workflow ensures that users can only access medical expense data associated with their own SZV identification number.

## Architecture

### Components

1. **JwtHandlerService** - Validates JWT tokens from Azure AD B2C
2. **DataVerseRequest** - Fetches user information from Microsoft Dataverse
3. **IdentificationController** - Implements the authorization logic

### Workflow Sequence

```
1. Client sends request with JWT token in Authorization header
   ↓
2. IdentificationController extracts and validates JWT token using JwtHandlerService
   ↓
3. Extract userNameIdentifier (sub claim) from validated token
   ↓
4. Fetch Contact from Dataverse using DataVerseRequest.GetUserSessionFromDataVerseAsync()
   ↓
5. Compare Contact.SZVIdNumber with requested identificationNumber parameter
   ↓
6. If match → Proceed with data retrieval
   If mismatch → Return 403 Forbidden
```

## Configuration

### Azure AD B2C Configuration

Add the following section to `appsettings.json`:

```json
{
  "AzureAdB2C": {
    "Instance": "https://login.microsoftonline.com/",
    "ClientId": "YOUR_CLIENT_ID_HERE",
    "Audience": "YOUR_AUDIENCE_HERE",
    "TenantId": "YOUR_TENANT_ID_HERE",
    "Domain": "yourtenant.onmicrosoft.com",
    "SignUpSignInPolicyId": "B2C_1_susi",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "EndpointOpenIDConnectMetadataDocument": "https://login.microsoftonline.com/YOUR_TENANT_ID_HERE/v2.0/.well-known/openid-configuration"
  }
}
```

**Configuration Fields:**
- `Instance` - Azure AD B2C instance URL
- `ClientId` - Application (client) ID from Azure AD B2C app registration
- `Audience` - Expected audience in the JWT token (typically same as ClientId)
- `TenantId` - Azure AD B2C Tenant ID (GUID)
- `Domain` - Your B2C tenant domain
- `SignUpSignInPolicyId` - User flow name (e.g., B2C_1_susi)
- `SignedOutCallbackPath` - Redirect path after sign out
- `EndpointOpenIDConnectMetadataDocument` - OpenID Connect metadata endpoint URL

### Dataverse Configuration

Add the following section to `appsettings.json`:

```json
{
  "DataVerse": {
    "ApiEndpoint": "https://your-org.crm.dynamics.com",
    "ClientId": "YOUR_DATAVERSE_CLIENT_ID_HERE",
    "ClientSecret": "YOUR_DATAVERSE_CLIENT_SECRET_HERE",
    "TenantId": "YOUR_DATAVERSE_TENANT_ID_HERE",
    "Resource": "https://your-org.crm.dynamics.com",
    "TimeoutSeconds": 30
  }
}
```

**Configuration Fields:**
- `ApiEndpoint` - Dataverse organization URL
- `ClientId` - Application ID registered for Dataverse access
- `ClientSecret` - Application secret for authentication
- `TenantId` - Azure AD Tenant ID where Dataverse app is registered
- `Resource` - Resource URL for token acquisition (same as ApiEndpoint)
- `TimeoutSeconds` - HTTP request timeout in seconds

## API Endpoint

### GET /api/identification/{identificationNumber}

Retrieves medical expenses for a specific identification number with security validation.

**Authorization:** Required - JWT Bearer token from Azure AD B2C

**Parameters:**
- `identificationNumber` (path) - The SZV identification number
- `skipToken` (query, optional) - Pagination token
- `top` (query, optional) - Number of records to return (default: 10)

**Request Example:**
```http
GET /api/identification/123456789?top=10
Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6...
```

**Success Response (200 OK):**
```json
[
  {
    "id": "2024-01-15-PROV001",
    "personsIdentificationNumber": "123456789",
    "expenseDate": "2024-01-15T00:00:00Z",
    "providerCode": "PROV001",
    "providerName": "Medical Center",
    "careType": "Consultation",
    "details": [...]
  }
]
```

**Error Responses:**

- **401 Unauthorized** - Invalid or missing JWT token
  ```json
  {
    "message": "Invalid or expired JWT token"
  }
  ```

- **403 Forbidden** - SZVIdNumber mismatch
  ```json
  {
    "message": "Access denied. You are not authorized to access data for this identification number.",
    "detail": "The identification number in the request does not match your registered SZV ID."
  }
  ```

- **500 Internal Server Error** - Dataverse or system error
  ```json
  {
    "message": "Failed to retrieve user information from Dataverse"
  }
  ```

## Security Features

### JWT Token Validation

The `JwtHandlerService` performs the following validations:

1. **Signature Validation** - Verifies token is signed by Azure AD B2C using public keys from OpenID Connect metadata endpoint
2. **Issuer Validation** - Ensures token is issued by the configured Azure AD B2C tenant
3. **Audience Validation** - Verifies token is intended for this API
4. **Lifetime Validation** - Checks token expiration with 5-minute clock skew tolerance
5. **Algorithm Validation** - Ensures token uses RS256 algorithm

### Authorization Check

The authorization flow validates:

1. **User Authentication** - JWT token is valid and not expired
2. **User Identity** - User identifier (sub claim) exists in Dataverse
3. **Data Ownership** - User's SZVIdNumber matches the requested identificationNumber

### Error Handling

All operations include comprehensive error handling:
- JWT validation failures are logged and return 401 Unauthorized
- Dataverse connection failures are logged and return 500 Internal Server Error
- Authorization failures are logged with details and return 403 Forbidden
- All errors include correlation IDs for tracking

## Data Models

### ContactDTO

Represents a contact from Dataverse:

```csharp
public class ContactDTO
{
    public string ContactId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string SZVIdNumber { get; set; }  // Key field for authorization
    public string UserNameIdentifier { get; set; }
}
```

### UserSessionDTO

Represents a user session with associated contact:

```csharp
public class UserSessionDTO
{
    public string SessionId { get; set; }
    public string UserNameIdentifier { get; set; }
    public ContactDTO Contact { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public bool IsActive { get; set; }
}
```

## Development Setup

### Prerequisites

1. Azure AD B2C tenant with configured user flow
2. Registered application in Azure AD B2C
3. Microsoft Dataverse environment
4. Registered application for Dataverse access with appropriate permissions

### Local Development

1. Update `appsettings.Development.json` with your Azure AD B2C and Dataverse settings
2. Ensure the Dataverse application has permissions to read contact information
3. Test with a valid JWT token obtained from Azure AD B2C

### Testing with Postman

1. Obtain a JWT token from Azure AD B2C:
   - Use your B2C user flow endpoint
   - Authenticate with test credentials
   - Copy the `id_token` from the response

2. Make request to the API:
   ```http
   GET https://localhost:5000/api/identification/123456789
   Authorization: Bearer YOUR_JWT_TOKEN
   ```

## Dataverse Schema Requirements

The implementation expects the following fields in the Dataverse `contact` entity:

- `contactid` - Unique identifier (GUID)
- `firstname` - Contact first name
- `lastname` - Contact last name
- `emailaddress1` - Primary email address
- `new_szvidnumber` - Custom field for SZV identification number
- `adx_identity_username` - Link to Azure AD B2C user identifier

**Note:** Field names may need adjustment based on your Dataverse schema. Update the query in `DataVerseRequest.GetUserSessionFromDataVerseAsync()` accordingly.

## Logging

The implementation includes comprehensive logging at key points:

- JWT token validation attempts
- Dataverse API calls
- Authorization checks
- Error conditions

All logs include correlation IDs for distributed tracing.

## Security Considerations

1. **Never commit real credentials** - All configuration values in version control should be placeholders
2. **Use secure configuration** - In production, store secrets in Azure Key Vault or environment variables
3. **HTTPS only** - Always use HTTPS in production to protect JWT tokens in transit
4. **Token expiration** - Configure appropriate token lifetimes in Azure AD B2C
5. **Regular key rotation** - Rotate Dataverse client secrets regularly
6. **Audit logging** - Monitor and audit all authorization failures

## Troubleshooting

### Common Issues

**Issue:** "Invalid or expired JWT token"
- **Solution:** Verify token is not expired, ensure correct B2C configuration, check OpenID metadata endpoint is accessible

**Issue:** "Failed to retrieve user information from Dataverse"
- **Solution:** Verify Dataverse configuration, check application permissions, ensure contact exists with matching username

**Issue:** "Access denied" (403)
- **Solution:** Verify the user's SZVIdNumber in Dataverse matches the requested identificationNumber

### Debug Mode

Enable detailed logging by updating `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "HECINA.Api.Services": "Debug",
      "HECINA.Api.Controllers": "Debug"
    }
  }
}
```

## Maintenance

### Updating Azure AD B2C Configuration

If B2C tenant or application changes:
1. Update `appsettings.json` with new values
2. Update `EndpointOpenIDConnectMetadataDocument` URL
3. Restart the application
4. Test with new token

### Updating Dataverse Integration

If Dataverse schema changes:
1. Update field names in `DataVerseRequest.GetUserSessionFromDataVerseAsync()`
2. Update `ContactDTO` if new fields are needed
3. Test thoroughly with sample data

## Future Enhancements

Potential improvements for future development:

1. **Caching** - Cache user sessions to reduce Dataverse calls
2. **Rate Limiting** - Implement rate limiting per user
3. **Additional Claims** - Support additional authorization claims beyond SZVIdNumber
4. **Multi-factor Authentication** - Enforce MFA requirements
5. **Audit Trail** - Store access logs in database for compliance
