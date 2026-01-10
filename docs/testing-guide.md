# Testing Guide for Security Validation Workflow

## Overview

This document provides guidance for testing the security validation workflow components. While no automated test infrastructure exists yet, this guide offers test scenarios and hints for future test implementation.

## Unit Testing Recommendations

### JwtHandlerService Tests

#### Test Class Structure
```csharp
public class JwtHandlerServiceTests
{
    private Mock<IOptions<MicrosoftIdentityConfig>> _configMock;
    private Mock<ILogger<JwtHandlerService>> _loggerMock;
    private JwtHandlerService _service;

    [SetUp]
    public void Setup()
    {
        _configMock = new Mock<IOptions<MicrosoftIdentityConfig>>();
        _loggerMock = new Mock<ILogger<JwtHandlerService>>();
        
        var config = new MicrosoftIdentityConfig
        {
            Instance = "https://login.microsoftonline.com/",
            TenantId = "test-tenant-id",
            Audience = "test-audience",
            EndpointOpenIDConnectMetadataDocument = "https://login.microsoftonline.com/test-tenant-id/v2.0/.well-known/openid-configuration"
        };
        
        _configMock.Setup(x => x.Value).Returns(config);
        _service = new JwtHandlerService(_configMock.Object, _loggerMock.Object);
    }
}
```

#### Test Scenarios

1. **ValidateTokenAsync_WithValidToken_ReturnsClaimsPrincipal**
   - Setup: Create a valid JWT token with proper claims
   - Action: Call ValidateTokenAsync
   - Assert: Returns non-null ClaimsPrincipal with expected claims

2. **ValidateTokenAsync_WithExpiredToken_ReturnsNull**
   - Setup: Create an expired JWT token
   - Action: Call ValidateTokenAsync
   - Assert: Returns null and logs appropriate error

3. **ValidateTokenAsync_WithInvalidSignature_ReturnsNull**
   - Setup: Create a token with invalid signature
   - Action: Call ValidateTokenAsync
   - Assert: Returns null and logs security token validation failure

4. **ValidateTokenAsync_WithInvalidAudience_ReturnsNull**
   - Setup: Create a token with wrong audience
   - Action: Call ValidateTokenAsync
   - Assert: Returns null

5. **ValidateTokenAsync_WithInvalidIssuer_ReturnsNull**
   - Setup: Create a token with wrong issuer
   - Action: Call ValidateTokenAsync
   - Assert: Returns null

6. **GetUserNameIdentifier_WithValidPrincipal_ReturnsSubClaim**
   - Setup: Create ClaimsPrincipal with 'sub' claim
   - Action: Call GetUserNameIdentifier
   - Assert: Returns correct sub claim value

7. **GetUserNameIdentifier_WithoutSubClaim_ReturnsNull**
   - Setup: Create ClaimsPrincipal without 'sub' claim
   - Action: Call GetUserNameIdentifier
   - Assert: Returns null and logs warning

#### Required NuGet Packages for Testing
```xml
<PackageReference Include="NUnit" Version="3.14.0" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
```

### DataVerseRequest Tests

#### Test Class Structure
```csharp
public class DataVerseRequestTests
{
    private Mock<IOptions<DataVerseConfig>> _configMock;
    private Mock<ILogger<DataVerseRequest>> _loggerMock;
    private Mock<IHttpClientFactory> _httpClientFactoryMock;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private DataVerseRequest _service;

    [SetUp]
    public void Setup()
    {
        _configMock = new Mock<IOptions<DataVerseConfig>>();
        _loggerMock = new Mock<ILogger<DataVerseRequest>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        
        var config = new DataVerseConfig
        {
            ApiEndpoint = "https://test.crm.dynamics.com",
            ClientId = "test-client-id",
            ClientSecret = "test-secret",
            TenantId = "test-tenant-id",
            Resource = "https://test.crm.dynamics.com",
            TimeoutSeconds = 30
        };
        
        _configMock.Setup(x => x.Value).Returns(config);
        
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("DataVerseClient")).Returns(httpClient);
        
        _service = new DataVerseRequest(_configMock.Object, _loggerMock.Object, _httpClientFactoryMock.Object);
    }
}
```

#### Test Scenarios

1. **GetUserSessionFromDataVerseAsync_WithValidUser_ReturnsUserSession**
   - Setup: Mock successful token acquisition and Dataverse response
   - Action: Call GetUserSessionFromDataVerseAsync with valid identifier
   - Assert: Returns UserSessionDTO with populated Contact and SZVIdNumber

2. **GetUserSessionFromDataVerseAsync_WithNonExistentUser_ReturnsNull**
   - Setup: Mock empty Dataverse response
   - Action: Call GetUserSessionFromDataVerseAsync
   - Assert: Returns null and logs appropriate warning

3. **GetUserSessionFromDataVerseAsync_WithTokenFailure_ReturnsNull**
   - Setup: Mock failed token acquisition
   - Action: Call GetUserSessionFromDataVerseAsync
   - Assert: Returns null and logs error

4. **GetUserSessionFromDataVerseAsync_WithDataverseError_ReturnsNull**
   - Setup: Mock HTTP error response from Dataverse
   - Action: Call GetUserSessionFromDataVerseAsync
   - Assert: Returns null and logs error with status code

5. **GetUserSessionFromDataVerseAsync_WithTimeout_ReturnsNull**
   - Setup: Mock timeout exception
   - Action: Call GetUserSessionFromDataVerseAsync
   - Assert: Returns null and logs timeout error

6. **GetAccessTokenAsync_WithValidCredentials_ReturnsToken**
   - Setup: Mock successful token endpoint response
   - Action: Call GetAccessTokenAsync (through reflection or make it public for testing)
   - Assert: Returns valid access token

#### Mock Data Examples

**Dataverse Contact Response:**
```json
{
  "value": [
    {
      "contactid": "12345678-1234-1234-1234-123456789012",
      "firstname": "John",
      "lastname": "Doe",
      "emailaddress1": "john.doe@example.com",
      "new_szvidnumber": "123456789"
    }
  ]
}
```

**Token Response:**
```json
{
  "access_token": "eyJ0eXAiOiJKV1QiLCJhbGc...",
  "token_type": "Bearer",
  "expires_in": 3599
}
```

## Integration Testing Recommendations

### IdentificationController Integration Tests

#### Test Class Structure
```csharp
public class IdentificationControllerIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;

    [SetUp]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace real services with mocks
                    services.RemoveAll<IJwtHandlerService>();
                    services.RemoveAll<IDataVerseRequest>();
                    
                    services.AddScoped<IJwtHandlerService, MockJwtHandlerService>();
                    services.AddScoped<IDataVerseRequest, MockDataVerseRequest>();
                });
            });
        
        _client = _factory.CreateClient();
    }
}
```

#### Test Scenarios

1. **GetByIdentificationNumber_WithValidTokenAndMatchingSZVId_ReturnsOk**
   - Setup: Valid JWT token, matching SZVIdNumber in mock
   - Action: GET /api/identification/{identificationNumber}
   - Assert: 200 OK with medical expenses data

2. **GetByIdentificationNumber_WithValidTokenAndMismatchedSZVId_ReturnsForbidden**
   - Setup: Valid JWT token, different SZVIdNumber in mock
   - Action: GET /api/identification/{identificationNumber}
   - Assert: 403 Forbidden with appropriate error message

3. **GetByIdentificationNumber_WithInvalidToken_ReturnsUnauthorized**
   - Setup: Invalid or expired JWT token
   - Action: GET /api/identification/{identificationNumber}
   - Assert: 401 Unauthorized

4. **GetByIdentificationNumber_WithMissingToken_ReturnsUnauthorized**
   - Setup: No Authorization header
   - Action: GET /api/identification/{identificationNumber}
   - Assert: 401 Unauthorized

5. **GetByIdentificationNumber_WithDataverseFailure_ReturnsInternalServerError**
   - Setup: Mock Dataverse service to return null
   - Action: GET /api/identification/{identificationNumber}
   - Assert: 500 Internal Server Error

6. **GetByIdentificationNumber_WithPagination_ReturnsCorrectData**
   - Setup: Valid token and authorization
   - Action: GET /api/identification/{identificationNumber}?top=5&skipToken=abc
   - Assert: 200 OK with paginated results

#### Required NuGet Packages for Integration Testing
```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
<PackageReference Include="NUnit" Version="3.14.0" />
```

## Manual Testing Guide

### Prerequisites

1. Valid Azure AD B2C tenant with user flow configured
2. Test user account in Azure AD B2C
3. Dataverse environment with test contact data
4. Test contact linked to B2C user with known SZVIdNumber

### Manual Test Steps

#### Test 1: Successful Authorization Flow

1. Obtain JWT token from Azure AD B2C
   ```bash
   # Use your B2C login endpoint or Postman
   # Save the id_token or access_token
   ```

2. Make API request with matching SZVIdNumber
   ```bash
   curl -X GET "https://localhost:5000/api/identification/123456789?top=10" \
     -H "Authorization: Bearer YOUR_JWT_TOKEN"
   ```

3. Verify:
   - Response status: 200 OK
   - Response contains medical expenses data
   - Logs show successful authorization

#### Test 2: Authorization Failure (Mismatched SZVIdNumber)

1. Use same JWT token from Test 1
2. Request different identification number
   ```bash
   curl -X GET "https://localhost:5000/api/identification/987654321?top=10" \
     -H "Authorization: Bearer YOUR_JWT_TOKEN"
   ```

3. Verify:
   - Response status: 403 Forbidden
   - Response contains appropriate error message
   - Logs show authorization failure with details

#### Test 3: Invalid Token

1. Use expired or malformed token
   ```bash
   curl -X GET "https://localhost:5000/api/identification/123456789?top=10" \
     -H "Authorization: Bearer INVALID_TOKEN"
   ```

2. Verify:
   - Response status: 401 Unauthorized
   - Response contains token validation error
   - Logs show token validation failure

#### Test 4: Missing Token

1. Request without Authorization header
   ```bash
   curl -X GET "https://localhost:5000/api/identification/123456789?top=10"
   ```

2. Verify:
   - Response status: 401 Unauthorized
   - Response indicates missing authorization
   - Logs show missing header

### Test Data Setup

#### Dataverse Test Contact

Create test contact in Dataverse with:
```
- contactid: Generated GUID
- firstname: "Test"
- lastname: "User"
- emailaddress1: "test.user@example.com"
- new_szvidnumber: "123456789"
- adx_identity_username: "12345678-1234-1234-1234-123456789012" (matches B2C sub claim)
```

#### Azure AD B2C Test User

Create test user with:
```
- Email: test.user@example.com
- Sub claim: Must match adx_identity_username in Dataverse
```

## Performance Testing

### Load Testing Scenarios

1. **Concurrent Requests**
   - Test with 50-100 concurrent users
   - Each with valid JWT token
   - Measure response times and success rate

2. **Token Validation Performance**
   - Measure JWT validation overhead
   - Test with cached vs fresh OIDC metadata

3. **Dataverse Call Performance**
   - Measure time for Dataverse contact lookup
   - Test with different network latencies

### Performance Benchmarks

Recommended targets:
- JWT validation: < 100ms
- Dataverse lookup: < 500ms
- Total request time: < 1000ms
- Success rate: > 99.9%

## Security Testing

### Security Test Scenarios

1. **Token Replay Attack**
   - Test: Reuse expired token
   - Expected: Rejected with 401

2. **Token Manipulation**
   - Test: Modify token claims
   - Expected: Signature validation fails, 401

3. **SQL Injection in IdentificationNumber**
   - Test: Send SQL injection strings
   - Expected: Safely handled, no SQL errors

4. **Cross-User Data Access**
   - Test: Valid token but wrong SZVIdNumber
   - Expected: 403 Forbidden

5. **Brute Force Token Guessing**
   - Test: Multiple invalid tokens
   - Expected: All rejected, rate limiting (if implemented)

## Troubleshooting Test Failures

### Common Issues

1. **Token Validation Fails**
   - Check OpenID metadata endpoint is accessible
   - Verify tenant ID and audience match configuration
   - Ensure token is not expired

2. **Dataverse Connection Fails**
   - Verify Dataverse endpoint URL
   - Check client credentials are correct
   - Ensure network connectivity to Dataverse

3. **Authorization Check Fails Unexpectedly**
   - Verify contact exists in Dataverse
   - Check SZVIdNumber field name matches schema
   - Ensure UserNameIdentifier linking is correct

## Continuous Integration

### Recommended CI Pipeline

```yaml
steps:
  - name: Build
    run: dotnet build
  
  - name: Unit Tests
    run: dotnet test --filter Category=Unit
  
  - name: Integration Tests
    run: dotnet test --filter Category=Integration
    env:
      AzureAdB2C__ClientId: ${{ secrets.B2C_CLIENT_ID }}
      DataVerse__ApiEndpoint: ${{ secrets.DATAVERSE_ENDPOINT }}
  
  - name: Security Scan
    run: dotnet security-scan
```

### Test Coverage Goals

- Unit tests: > 80% code coverage
- Integration tests: All API endpoints
- Security tests: All authentication/authorization paths

## Future Test Automation

When implementing automated tests:

1. Create separate test project: `HECINA.Api.Tests`
2. Use dependency injection to inject mocks
3. Use TestServer for integration tests
4. Implement test data seeding for consistent results
5. Use test containers for isolated test environments
6. Add code coverage reporting
7. Integrate with CI/CD pipeline

## Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [NUnit Documentation](https://nunit.org/)
- [Moq Documentation](https://github.com/moq/moq4)
- [Microsoft Testing in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/test/)
- [Integration Testing Best Practices](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)
