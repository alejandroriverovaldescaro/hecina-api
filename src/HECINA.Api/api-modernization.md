# HECINA API Modernization

## Overview

This document describes the modernization strategy for the HECINA API, which is being migrated to .NET 8 with a focus on cloud-ready architecture while maintaining compatibility with on-premises IIS deployment.

## Architecture

### Technology Stack

- **Framework**: .NET 8 Web API
- **Data Access**: Dapper (lightweight ORM)
- **Database**: SQL Server 2012+ compatible
- **Authentication**: Basic Authentication / JWT Bearer tokens
- **Hosting**: IIS (on-premises) with cloud migration path

### Project Structure

```
HECINA.Api/
├── Controllers/          # API endpoints
├── Domain/              # Domain models
├── Repositories/        # Data access layer
├── Infrastructure/      # Database and external service configurations
├── Authentication/      # Authentication handlers
├── Middleware/          # Custom middleware components
└── Extensions/          # Service registration extensions
```

## Key Features

### 1. Minimal Hosting Model

The API uses ASP.NET Core's minimal hosting model for simplified configuration and improved performance.

### 2. Authentication

Two authentication schemes are supported:

- **Basic Authentication**: Simple username/password authentication for internal systems
- **JWT Bearer**: Token-based authentication for modern client applications

Configuration is managed through `appsettings.json` with the `Authentication:Scheme` setting.

### 3. Data Access

- Uses Dapper for high-performance data access
- SQL queries are compatible with SQL Server 2012+
- Connection pooling and parameterized queries for security and performance
- Repository pattern for clean separation of concerns

### 4. Correlation ID Middleware

Every request is assigned a unique correlation ID for distributed tracing and debugging. The correlation ID is:
- Generated if not provided by the client
- Returned in response headers
- Logged with every log entry

### 5. Swagger/OpenAPI

Integrated Swagger UI for API documentation and testing, available in development environment.

## Configuration

### Database Connection

Configure the database connection string in `appsettings.json`:

```json
{
  "Database": {
    "ConnectionString": "Server=localhost;Database=HecinaDb;...",
    "CommandTimeout": 30
  }
}
```

### Authentication

Basic Authentication example:

```json
{
  "Authentication": {
    "Scheme": "Basic",
    "Basic": {
      "Username": "admin",
      "Password": "password123"
    }
  }
}
```

JWT Authentication example:

```json
{
  "Authentication": {
    "Scheme": "JWT",
    "Jwt": {
      "SecretKey": "YourSecretKey",
      "Issuer": "HECINA.Api",
      "Audience": "HECINA.Api.Client"
    }
  }
}
```

## Deployment

### IIS Deployment

1. Publish the application: `dotnet publish -c Release`
2. Create an IIS application pool targeting "No Managed Code"
3. Deploy the published files to the IIS website directory
4. Ensure the application pool identity has access to the database
5. Configure appropriate connection strings in `appsettings.json`

### Requirements

- .NET 8 Runtime
- IIS with ASP.NET Core Module (ANCM)
- SQL Server 2012 or later

## API Endpoints

### Medical Expenses

- `GET /api/medicalexpenses` - Get all medical expenses with details
- `GET /api/medicalexpenses/{id}` - Get a specific medical expense by ID

All endpoints require authentication.

## Development

### Prerequisites

- .NET 8 SDK
- SQL Server (local or remote)
- Visual Studio 2022 or VS Code

### Running Locally

1. Update connection string in `appsettings.Development.json`
2. Run `dotnet restore`
3. Run `dotnet build`
4. Run `dotnet run`
5. Navigate to `http://localhost:5000/swagger` for API documentation

## Security Considerations

- Always use HTTPS in production
- Store sensitive configuration in environment variables or Azure Key Vault
- Rotate authentication secrets regularly
- Implement rate limiting for production deployments
- Use parameterized queries (handled by Dapper) to prevent SQL injection

## Future Enhancements

- Health check endpoints
- Distributed caching with Redis
- API versioning
- Rate limiting middleware
- Integration with Azure Application Insights
- Container support (Docker)
