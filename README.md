# HECINA API

A modern .NET 8 Web API for managing medical expenses, designed for on-premises IIS deployment with a cloud-ready architecture.

## Features

- **RESTful API** for medical expense management
- **Dual Authentication**: Basic Authentication and JWT Bearer tokens
- **High Performance**: Dapper-based data access with SQL Server
- **Correlation ID Tracking**: Built-in distributed tracing support
- **Swagger/OpenAPI**: Interactive API documentation
- **IIS Ready**: Optimized for on-premises deployment

## Quick Start

### Prerequisites

- .NET 8 SDK
- SQL Server 2012 or later

### Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/alejandroriverovaldescaro/hecina-api.git
   cd hecina-api
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Update the connection string in `src/HECINA.Api/appsettings.Development.json`

4. Build the solution:
   ```bash
   dotnet build
   ```

5. Run the API:
   ```bash
   cd src/HECINA.Api
   dotnet run
   ```

6. Access Swagger UI at `http://localhost:5000/swagger`

## Project Structure

```
├── src/
│   └── HECINA.Api/
│       ├── Controllers/         # API endpoints
│       ├── Domain/             # Domain models
│       ├── Repositories/       # Data access layer
│       ├── Infrastructure/     # Database configuration
│       ├── Authentication/     # Authentication handlers
│       ├── Middleware/         # Custom middleware
│       └── Extensions/         # Service extensions
├── docs/
│   └── api-modernization.md   # Architecture documentation
└── HECINA.Api.sln             # Solution file
```

## API Endpoints

### Medical Expenses

- `GET /api/medicalexpenses` - Get all medical expenses
- `GET /api/medicalexpenses/{id}` - Get expense by ID

**Note**: All endpoints require authentication.

## Authentication

### Basic Authentication

Add the `Authorization` header with Base64-encoded credentials:

```
Authorization: Basic YWRtaW46cGFzc3dvcmQxMjM=
```

### JWT Authentication

To use JWT authentication, update `appsettings.json`:

```json
{
  "Authentication": {
    "Scheme": "JWT"
  }
}
```

## Configuration

Key configuration sections in `appsettings.json`:

- **Database**: Connection string and timeout settings
- **Authentication**: Authentication scheme and credentials
- **Logging**: Log levels and providers

**⚠️ Security Note**: The `appsettings.json` file contains placeholder/sample credentials for development purposes only. In production:
- Store sensitive values in environment variables, Azure Key Vault, or secure configuration providers
- Never commit real credentials to source control
- Rotate secrets regularly

See [API Modernization Guide](docs/api-modernization.md) for detailed configuration options.

## Deployment

### IIS Deployment

1. Publish the application:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. Create an IIS application pool (No Managed Code)

3. Deploy published files to IIS

4. Update connection strings for production environment

For detailed deployment instructions, see [API Modernization Guide](docs/api-modernization.md).

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License.