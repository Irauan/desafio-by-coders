# DesafioByCoders - CNAB Transaction Import System

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-20-DD0031?logo=angular)](https://angular.io/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

A modern web application for importing and managing CNAB (Centro Nacional de Automação Bancária) financial transaction files. Built with .NET 9, Angular 20, and PostgreSQL.

## 📋 Table of Contents

- [Features](#-features)
- [Architecture](#-architecture)
- [Technology Stack](#-technology-stack)
- [Quick Start with Docker Compose](#-quick-start-with-docker-compose)
- [API Documentation](#-api-documentation)
- [Consuming the API](#-consuming-the-api)
- [CNAB File Format](#-cnab-file-format)
- [Project Structure](#-project-structure)
- [Development](#-development)
- [Testing](#-testing)
- [License](#-license)

## ✨ Features

### Core Functionality

- ✅ **CNAB File Upload**: Web interface for uploading CNAB transaction files
- ✅ **Automatic Parsing**: Parses fixed-width CNAB 80-character format
- ✅ **Data Validation**: Comprehensive validation with detailed error messages
- ✅ **Duplicate Detection**: Prevents importing the same transactions multiple times
- ✅ **Store Management**: Automatic store creation from transaction data
- ✅ **Balance Calculation**: Real-time balance calculation per store

### Technical Features

- ✅ **REST API**: Versioned API with OpenAPI/Scalar documentation
- ✅ **Database Migrations**: Automatic schema management with EF Core
- ✅ **Health Checks**: Comprehensive health monitoring
- ✅ **Structured Logging**: Serilog with request/response logging
- ✅ **Exception Handling**: Global exception handling middleware
- ✅ **Docker Support**: Full Docker Compose production deployment
- ✅ **CORS Support**: Configurable CORS for Angular SPA
- ✅ **Indexed Queries**: Optimized database indexes for performance

### Quality Assurance

- ✅ **Unit Tests**: Comprehensive xUnit test coverage
- ✅ **Integration Tests**: Full API integration tests with Testcontainers
- ✅ **Test Coverage**: High code coverage with detailed reports
- ✅ **Clean Architecture**: VSA (Vertical Slice Architecture) pattern
- ✅ **SOLID Principles**: Well-structured, maintainable codebase

## 🏗️ Architecture

### System Architecture

The application is composed of the following components:

- **Angular SPA**
- **.NET API**
- **PostgreSQL**: Relational database

### Application Architecture

The application follows **Vertical Slice Architecture (VSA)** principles:

- **Features/Transactions**: Transaction import and management
- **Features/Stores**: Store listing and balance calculation
- **Middleware**: Exception handling, logging decorators
- **Handlers**: Command/Query pattern with decorators

> **📝 Why Vertical Slice Architecture?**
>
> This project uses VSA instead of traditional layered architecture (Controllers, Services, Repositories, etc.) to **maximize cohesion** and **minimize coupling** between related files.
>
> **Key Benefits:**
> - ✅ **High Cohesion**: All files related to a feature (handler, validator, repository, entity) are co-located in the same folder
> - ✅ **Easy Navigation**: Finding all code for "Transaction Import" means opening one folder, not jumping across multiple technical layers
> - ✅ **Business-Focused Structure**: Folders are named after **business concepts** (Transactions, Stores) rather than technical concerns (Services, Domain, Infrastructure)
> - ✅ **Change Isolation**: Modifying transaction import logic doesn't require touching files scattered across the solution
> - ✅ **Team Scalability**: Multiple developers can work on different features without merge conflicts
>
> **Why No Technical Folders?**
>
> Traditional architecture:
> ```
> ❌ Domain/Entities/Transaction.cs
> ❌ Services/TransactionService.cs
> ❌ Repositories/TransactionRepository.cs
> ❌ Controllers/TransactionsController.cs
> ```
>
> VSA approach:
> ```
> ✅ Features/Transactions/Transaction.cs
> ✅ Features/Transactions/Import/TransactionImportHandler.cs
> ✅ Features/Transactions/TransactionRepository.cs
> ✅ Features/Transactions/Import/TransactionImportController.cs
> ```
>
> Everything related to transactions lives in `Features/Transactions/`, making the codebase easier to understand and maintain. The folder structure reflects **what the system does** (business capabilities) rather than **how it's built** (technical
> layers).

## 🛠️ Technology Stack

### Backend (.NET API)

- **.NET 9.0**: Latest .NET framework
- **ASP.NET Core**: Web API framework
- **Entity Framework Core 9**: ORM for database access
- **PostgreSQL**: Relational database
- **Serilog**: Structured logging
- **Scrutor**: Dependency injection decorators
- **EFCore.BulkExtensions**: High-performance bulk operations
- **xUnit**: Unit testing framework
- **Testcontainers**: Integration testing with Docker

### Frontend (Angular SPA)

- **Angular 20**: Modern web framework
- **TypeScript**: Type-safe JavaScript
- **RxJS**: Reactive programming
- **Karma + Jasmine**: Testing framework
- **Custom CSS**: No framework dependencies

### Infrastructure

- **Docker**: Containerization
- **Docker Compose**: Multi-container orchestration
- **Nginx**: Web server and reverse proxy
- **GitHub Actions**: CI/CD (optional)

## 🚀 Quick Start with Docker Compose

### Prerequisites

- Docker Engine 20.10+
- Docker Compose 2.0+
- 2GB+ RAM available
- 5GB+ disk space

### Step 1: Clone the Repository

```bash
git clone <repository-url>
cd desafio-by-coders
```

### Step 2: Configure Environment

```bash
# Copy the example environment file
copy .env.example .env

# Edit .env and set your database password
# IMPORTANT: Change DB_PASSWORD in production!
```

### Step 3: Start the Application

```bash
# Build and start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Check service status
docker-compose ps
```

### Step 4: Access the Application

- **Web Application**: http://localhost
- **API Documentation (Scalar)**: http://localhost:8080/scalar/v1
- **API Base URL**: http://localhost:8080/api/v1
- **Health Check**: http://localhost:8080/health

### Step 5: Import CNAB File

1. Open http://localhost in your browser
2. Click "Select CNAB File" and choose the `CNAB.txt` file
3. Click "Import File"
4. View the import results and store balances

### Stop the Application

```bash
# Stop all services
docker-compose stop

# Stop and remove containers (keeps data)
docker-compose down

# Stop and remove everything including data (⚠️ DELETES DATA!)
docker-compose down -v
```

## 📚 API Documentation

The API is fully documented using **OpenAPI 3.0** and can be explored interactively using the **Scalar** interface.

### Access API Documentation

**Scalar Interactive Documentation**: http://localhost:8080/scalar/v1

The Scalar interface provides:

- ✅ Complete API reference
- ✅ Request/response examples
- ✅ Try-it-out functionality
- ✅ Schema definitions
- ✅ Authentication details

### API Versioning

The API uses URL versioning:

- **Current version**: `v1`
- **Base URL**: `/api/v1`

## 🔌 Consuming the API

### 1. Health Check

Check if the API is running and healthy:

```bash
# Simple health check
curl http://localhost:8080/health

# Detailed health check
curl http://localhost:8080/health/ready
```

**Response:**

```json
{
  "status": "Healthy",
  "results": {
    "postgresql": {
      "status": "Healthy"
    },
    "transactiondb_context": {
      "status": "Healthy"
    }
  }
}
```

### 2. Import CNAB File

Upload and import a CNAB transaction file:

```bash
curl -X POST http://localhost:8080/api/v1/transactions/import \
  -F "file=@CNAB.txt" \
  -H "Accept: application/json"
```

**Success Response (200 OK):**

```json
{
  "status": 200,
  "totalImportedLines": 21,
  "importedSummaryPerStores": [
    {
      "storeName": "BAR DO JOÃO",
      "imported": 8
    },
    {
      "storeName": "MERCADO DA MARIA",
      "imported": 13
    }
  ],
  "totalLinesDuplicate": 0
}
```

**Partial Success (207 Multi-Status):**

```json
{
  "results": [
    {
      "status": 207,
      "totalImportedLines": 18,
      "importedSummaryPerStores": [
        ...
      ],
      "totalLinesDuplicate": 2
    },
    {
      "status": 207,
      "totalInvalidLines": 3,
      "errors": [
        {
          "code": "CNAB_INVALID_DATE",
          "message": "Line 5: Invalid date '20190230'."
        }
      ]
    }
  ]
}
```

**Error Response (422 Unprocessable Entity):**

```json
{
  "status": 422,
  "totalInvalidLines": 3,
  "errors": [
    {
      "code": "CNAB_INVALID_LENGTH",
      "message": "Line 1: Line length invalid (79), expected >= 80."
    },
    {
      "code": "CNAB_INVALID_TYPE",
      "message": "Line 2: Invalid transaction type value 'X'."
    }
  ]
}
```

### 3. List Stores with Balances

Get all stores with their transaction balances:

```bash
curl http://localhost:8080/api/v1/stores
```

**Response (200 OK):**

```json
[
  {
    "name": "BAR DO JOÃO",
    "owner": "JOÃO SILVA",
    "balance": 152000.00
  },
  {
    "name": "MERCADO DA MARIA",
    "owner": "MARIA SANTOS",
    "balance": -3450.50
  }
]
```

### Using PowerShell (Windows)

```powershell
# Import CNAB file
$file = Get-Item "CNAB.txt"
$form = @{
    file = $file
}
Invoke-RestMethod -Uri "http://localhost:8080/api/v1/transactions/import" `
    -Method Post -Form $form

# List stores
Invoke-RestMethod -Uri "http://localhost:8080/api/v1/stores" -Method Get
```

### Using Postman

1. **Import Collection**: Import the OpenAPI specification from `/scalar/v1`
2. **Import CNAB**:
    - Method: `POST`
    - URL: `http://localhost:8080/api/v1/transactions/import`
    - Body: `form-data`
    - Key: `file` (type: File)
    - Value: Select `CNAB.txt`
3. **List Stores**:
    - Method: `GET`
    - URL: `http://localhost:8080/api/v1/stores`

### Error Handling

All API errors follow the **RFC 7807 Problem Details** standard:

```json
{
  "status": 500,
  "title": "An error occurred while processing your request",
  "type": "https://httpstatuses.com/500",
  "instance": "/api/v1/transactions/import",
  "detail": "An unexpected error occurred. Please try again later.",
  "traceId": "0HMVF8Q9QH123"
}
```

## 📄 CNAB File Format

### Fixed-Width Format Specification

The CNAB file uses a fixed-width format where each line represents one transaction with **80 characters** (not 81 as the sum below might suggest).

| Field            | Start | End | Size   | Description                                                   |
|------------------|-------|-----|--------|---------------------------------------------------------------|
| **Tipo**         | 1     | 1   | 1      | Transaction type (see types table below)                      |
| **Data**         | 2     | 9   | 8      | Transaction date (yyyyMMdd format)                            |
| **Valor**        | 10    | 19  | 10     | Transaction amount in cents (divide by 100 to get real value) |
| **CPF**          | 20    | 30  | 11     | Beneficiary CPF (Brazilian tax ID)                            |
| **Cartão**       | 31    | 42  | 12     | Card number used in transaction                               |
| **Hora**         | 43    | 48  | 6      | Time of occurrence (hhmmss, UTC-3 timezone)                   |
| **Dono da loja** | 49    | 62  | 14     | Store owner name                                              |
| **Nome loja**    | 63    | 80  | **18** | Store name                                                    |

> **⚠️ Important Note**: The original specification indicates the store name field ends at position 81 with 19 characters. However, the provided `CNAB.txt` file has **80 characters per line** with the store name field having **18 characters** (
> positions 63-80). This implementation follows the **actual file format (80 characters)** rather than the specification (81 characters).

### Example Line (80 characters)

```
3201903010000014200096206760174753****3153153453JOSE CARLOS    MERCEARIA 3 IRMÃOS
```

**Parsed as:**

- Type: `3` (Financiamento)
- Date: `20190301` (March 1, 2019)
- Amount: `0000014200` (142.00)
- CPF: `09620676017`
- Card: `4753****3153`
- Time: `153453` (15:34:53)
- Owner: `JOSE CARLOS   ` (14 chars, padded with spaces)
- Store: `MERCEARIA 3 IRMÃOS` (18 chars)

# Documentação sobre os tipos das transações

| Tipo | Descrição              | Natureza | Sinal |
|------|------------------------|----------|-------|
| 1    | Débito                 | Entrada  | +     |
| 2    | Boleto                 | Saída    | -     |
| 3    | Financiamento          | Saída    | -     |
| 4    | Crédito                | Entrada  | +     |
| 5    | Recebimento Empréstimo | Entrada  | +     |
| 6    | Vendas                 | Entrada  | +     |
| 7    | Recebimento TED        | Entrada  | +     |
| 8    | Recebimento DOC        | Entrada  | +     |
| 9    | Aluguel                | Saída    | -     |

### Validation Rules

The parser performs the following validations:

| Validation    | Error Code             | Description                               |
|---------------|------------------------|-------------------------------------------|
| Empty line    | `CNAB_EMPTY_LINE`      | Line is empty or contains only whitespace |
| Line length   | `CNAB_INVALID_LENGTH`  | Line must be exactly 80 characters        |
| Type format   | `CNAB_INVALID_TYPE`    | Type must be numeric                      |
| Type value    | `CNAB_UNKNOWN_TYPE`    | Type must be 1-9                          |
| Date format   | `CNAB_INVALID_DATE`    | Date must be valid yyyyMMdd               |
| Time format   | `CNAB_INVALID_TIME`    | Time must be valid hhmmss                 |
| Amount format | `CNAB_INVALID_AMOUNT`  | Amount must be numeric                    |
| Amount value  | `CNAB_NEGATIVE_AMOUNT` | Amount cannot be negative                 |

## 📁 Project Structure

```
desafio-by-coders/
├── DesafioByCoders.Api/              # .NET API project
│   ├── Features/                     # Vertical Slice Architecture
│   │   ├── Transactions/             # Transaction management
│   │   │   ├── Import/               # Import command & handler
│   │   │   ├── CnabParser/           # CNAB parsing strategies
│   │   │   ├── Transaction.cs        # Transaction entity
│   │   │   ├── TransactionRepository.cs
│   │   │   └── TransactionDbContext.cs
│   │   └── Stores/                   # Store management
│   │       └── List/                 # Store listing query
│   ├── Middleware/                   # Custom middleware
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Handlers/                     # Handler patterns
│   │   ├── IHandler.cs
│   │   └── LoggingHandlerDecorator.cs
│   ├── Migrations/                   # EF Core migrations
│   ├── Program.cs                    # Application startup
│   └── appsettings.json              # Configuration
│
├── DesafioByCoders.Ui.Web/           # Angular SPA project
│   ├── src/
│   │   ├── app/
│   │   │   ├── transactions/         # Transaction feature module
│   │   │   │   ├── transaction-import/
│   │   │   │   ├── transaction.service.ts
│   │   │   │   └── transaction.module.ts
│   │   │   └── stores/               # Store feature module
│   │   └── proxy.conf.js             # Dev proxy configuration
│   ├── nginx.conf                    # Production nginx config
│   └── Dockerfile                    # Angular Docker build
│
├── DesafioByCoders.Api.Tests.Units/  # Unit tests
├── DesafioByCoders.Api.Tests.Integrations/ # Integration tests
├── docker-compose.yml                # Docker Compose orchestration
├── .env.example                      # Environment variables template
└── README.md                         # This file
```

## 🔧 Development

### Prerequisites for Local Development

- .NET 9.0 SDK
- Node.js 20+
- PostgreSQL 16+ (or Docker)
- Visual Studio 2022 / VS Code / Rider

### Running with Aspire (Development)

```bash
# Install Aspire workload
dotnet workload install aspire

# Run the Aspire AppHost
cd DesafioByCoders.AppHost
dotnet run

# Access Aspire Dashboard
# http://localhost:15888
```

### Running API Locally

```bash
# Navigate to API project
cd DesafioByCoders.Api

# Set connection string
$env:ConnectionStrings__desafiobycoders="Host=localhost;Database=desafiobycoders;Username=postgres;Password=yourpassword"

# Run migrations
dotnet ef database update

# Run the API
dotnet run

# Access API
# http://localhost:5000
# http://localhost:5000/scalar/v1
```

### Running Angular Locally

```bash
# Navigate to Angular project
cd DesafioByCoders.Ui.Web

# Install dependencies
npm install

# Set API URL (for Aspire integration)
$env:services__desafiobycoders-api__http__0="http://localhost:5000"

# Run dev server
npm start

# Access application
# http://localhost:4200
```

### Database Migrations

```bash
# Create a new migration
dotnet ef migrations add "MigrationName" --project DesafioByCoders.Api

# Apply migrations
dotnet ef database update --project DesafioByCoders.Api

# Rollback migration
dotnet ef database update PreviousMigrationName --project DesafioByCoders.Api

# Remove last migration (if not applied)
dotnet ef migrations remove --project DesafioByCoders.Api
```

## 🧪 Testing

### Unit Tests

```bash
# Run all unit tests
dotnet test DesafioByCoders.Api.Tests.Units

# Run specific test class
dotnet test --filter "FullyQualifiedName~TransactionImportHandlerTests"

# Run with coverage
dotnet test DesafioByCoders.Api.Tests.Units /p:CollectCoverage=true
```

### Integration Tests

```bash
# Run all integration tests (requires Docker)
dotnet test DesafioByCoders.Api.Tests.Integrations

# Run specific feature tests
dotnet test --filter "FullyQualifiedName~TransactionImportControllerTests"

# Run with verbose output
dotnet test DesafioByCoders.Api.Tests.Integrations -v normal
```

### Angular Tests

```bash
# Navigate to Angular project
cd DesafioByCoders.Ui.Web

# Run tests (uses Edge browser)
npm test

# Run tests with coverage
npm run test:coverage

# Run tests in headless mode
npm run test:headless
```

### Test Coverage Summary

- **Unit Tests**: 69 tests covering handlers, repositories, parsers
- **Integration Tests**: 15+ tests with real PostgreSQL via Testcontainers
- **Angular Tests**: Component and service tests
- **Coverage**: 87% code coverage across all layers (excluding migration code), with tests focused on covering business logic

## 📖 Documentation

- **[Transaction Import Feature](TRANSACTION_IMPORT_FEATURE.md)**: Complete technical documentation for the CNAB import workflow
- **[Exception Handling](EXCEPTION_HANDLING_MIDDLEWARE.md)**: Middleware documentation
- **[CNAB Parser](CNAB_PARSER_REFACTORING.md)**: Parser strategy pattern
- **[Angular API Configuration](ANGULAR_API_URL_DOCKER.md)**: API URL setup

## 🚢 Production Deployment

### Environment Variables

Create a `.env` file based on `.env.example`:

```bash
# Database
DB_PASSWORD=YourSecurePasswordHere

# CORS (optional)
CORS_ORIGINS=https://yourdomain.com,https://www.yourdomain.com

# API URL (optional, defaults to http://api:8080)
API_URL=http://api:8080
```

### Deploy with Docker Compose

```bash
# Production deployment
docker-compose up -d

# Check logs
docker-compose logs -f

# Scale services (if needed)
docker-compose up -d --scale api=3
```

Made with ❤️ using .NET 9 and Angular 20