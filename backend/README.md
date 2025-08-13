# Healthcare Portal Backend (.NET Core API)

A comprehensive .NET Core Web API backend for a healthcare portal with provider/patient management and appointment scheduling system.

## Features

- **Provider Management**: Registration, login, and profile management for healthcare providers
- **Patient Management**: Registration, login, and profile management for patients
- **JWT Authentication**: Secure token-based authentication and authorization
- **Appointment Scheduling System (APS)**: 
  - View available time slots
  - Book appointments
  - Manage provider schedules
- **Role-based Access Control**: Separate access levels for providers and patients
- **Entity Framework Core**: Database operations with SQL Server
- **RESTful API Design**: Clean and consistent API endpoints

## Technology Stack

- **.NET 8.0**: Latest .NET framework
- **Entity Framework Core**: ORM for database operations
- **SQL Server**: Database (LocalDB for development)
- **JWT Bearer Authentication**: Secure authentication
- **BCrypt**: Password hashing
- **Swagger/OpenAPI**: API documentation

## Project Structure

```
HealthcarePortal.API/
├── Controllers/           # API controllers
│   ├── ProvidersController.cs
│   ├── PatientsController.cs
│   └── AppointmentsController.cs
├── Models/               # Entity models
│   ├── Provider.cs
│   ├── Patient.cs
│   └── Appointment.cs
├── DTOs/                 # Data Transfer Objects
│   ├── AuthDTOs.cs
│   └── AppointmentDTOs.cs
├── Data/                 # Database context
│   └── HealthcareDbContext.cs
├── Services/             # Business logic services
│   ├── AuthService.cs
│   └── AppointmentService.cs
├── Program.cs            # Application entry point
└── appsettings.json      # Configuration
```

## Setup Instructions

### Prerequisites

- .NET 8.0 SDK
- SQL Server or SQL Server LocalDB
- Visual Studio 2022 or VS Code

### Installation

1. **Navigate to the backend directory**:
   ```bash
   cd backend/HealthcarePortal.API
   ```

2. **Restore NuGet packages**:
   ```bash
   dotnet restore
   ```

3. **Update the connection string** in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=HealthcarePortalDb;Trusted_Connection=true;MultipleActiveResultSets=true"
     }
   }
   ```

4. **Create and apply database migrations**:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

5. **Run the application**:
   ```bash
   dotnet run
   ```

The API will be available at `https://localhost:7000` and `http://localhost:5000`.

### API Documentation

Once the application is running, you can access the Swagger documentation at:
- `https://localhost:7000/swagger`

## API Endpoints

### Authentication Endpoints

#### Provider Endpoints
- `POST /api/providers/register` - Register a new provider
- `POST /api/providers/login` - Provider login
- `GET /api/providers` - Get all providers (JWT protected)

#### Patient Endpoints
- `POST /api/patients/register` - Register a new patient
- `POST /api/patients/login` - Patient login
- `GET /api/patients` - Get all patients (JWT protected)

### Appointment Endpoints
- `GET /api/appointments/slots?providerId={id}&date={date}` - Get available slots
- `POST /api/appointments/book` - Book an appointment
- `GET /api/appointments/provider` - Get provider's appointments (Provider only)
- `GET /api/appointments/patient` - Get patient's appointments (Patient only)

## Authentication

The API uses JWT Bearer token authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## Database Schema

### Provider Table
- Id (Primary Key)
- FullName
- Email (Unique)
- PasswordHash
- Specialty
- PhoneNumber
- ClinicAddress
- CreatedAt

### Patient Table
- Id (Primary Key)
- FullName
- Email (Unique)
- PasswordHash
- Age
- Gender
- PhoneNumber
- CreatedAt

### Appointment Table
- Id (Primary Key)
- PatientId (Foreign Key)
- ProviderId (Foreign Key)
- AppointmentDateTime
- Notes
- Status (Scheduled, Completed, Cancelled, NoShow)
- CreatedAt

## Configuration

### JWT Settings
Update the JWT configuration in `appsettings.json`:

```json
{
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-that-is-at-least-32-characters-long",
    "Issuer": "HealthcarePortal",
    "Audience": "HealthcarePortalUsers",
    "ExpirationInMinutes": 60
  }
}
```

### CORS Configuration
The API is configured to allow requests from `http://localhost:3000` (React frontend). Update the CORS policy in `Program.cs` if needed.

## Development

### Adding New Migrations
When you modify the models, create a new migration:

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

### Running Tests
```bash
dotnet test
```

## Security Features

- Password hashing using BCrypt
- JWT token-based authentication
- Role-based authorization
- Input validation and sanitization
- CORS protection
- SQL injection prevention through Entity Framework

## Error Handling

The API includes comprehensive error handling with appropriate HTTP status codes and error messages.

## Deployment

For production deployment:

1. Update connection strings for production database
2. Set secure JWT secret key
3. Configure HTTPS
4. Set up proper logging
5. Configure CORS for production frontend URL

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License.
