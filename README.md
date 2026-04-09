# 🏥 CARS - Clinic Appointment & Record System

A production-ready .NET 10 Web API for healthcare appointment management with JWT authentication, role-based authorization, and a modern frontend interface.

## 📋 Features

- ✅ **Authentication & Authorization**: JWT-based auth with role-based access control
- ✅ **User Roles**: Admin, Doctor, Patient with specific permissions
- ✅ **Appointment Management**: Book, view, update, and cancel appointments
- ✅ **Doctor Profiles**: Specialization, bio, availability management
- ✅ **Soft Delete**: Maintain data integrity with soft delete pattern
- ✅ **API Documentation**: Interactive Swagger/OpenAPI documentation
- ✅ **Modern Frontend**: Responsive HTML/CSS/JavaScript interface
- ✅ **Clean Architecture**: Separated layers (Domain, Application, Infrastructure, API)
- ✅ **FluentValidation**: Robust input validation
- ✅ **Entity Framework Core**: Code-first database approach

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core Web API (.NET 10)
- **ORM**: Entity Framework Core 10
- **Database**: SQL Server (LocalDB for development)
- **Authentication**: JWT Bearer Tokens
- **Validation**: FluentValidation
- **API Documentation**: Swagger/Swashbuckle
- **Password Hashing**: BCrypt.Net
- **Frontend**: HTML5, CSS3, Vanilla JavaScript

## 📁 Project Structure

```
CARS/
├── CARS.Domain/              # Domain entities and enums
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── DoctorProfile.cs
│   │   ├── Appointment.cs
│   │   ├── AvailabilitySlot.cs
│   │   └── MedicalRecord.cs
│   └── Enums/
│       ├── UserRole.cs
│       ├── AppointmentStatus.cs
│       └── DayOfWeekEnum.cs
│
├── CARS.Application/         # Business logic and DTOs
│   ├── DTOs/
│   ├── Interfaces/
│   ├── Services/
│   └── Validators/
│
├── CARS.Infrastructure/      # Data access layer
│   ├── Data/
│   │   └── CarsDbContext.cs
│   └── Repositories/
│
└── CARS.API/                 # Web API and presentation
    ├── Controllers/
    ├── Services/
    ├── wwwroot/              # Frontend files
    │   ├── index.html
    │   ├── css/
    │   └── js/
    ├── Program.cs
    └── appsettings.json
```

## 🚀 Getting Started

### Prerequisites

- .NET 10 SDK installed
- SQL Server or SQL Server Express
- Visual Studio 2022 or VS Code
- Postman (optional, for API testing)

### Installation Steps

1. **Clone or extract the project**
   ```bash
   cd CARS
   ```

2. **Update Connection String**
   
   Open `CARS.API/appsettings.json` and update the connection string if needed:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CarsDB;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
   }
   ```

   For SQL Server Express, use:
   ```
   Server=.\\SQLEXPRESS;Database=CarsDB;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true
   ```

3. **Restore NuGet Packages**
   ```bash
   dotnet restore
   ```

4. **Apply Database Migrations**
   ```bash
   cd CARS.Infrastructure
   dotnet ef migrations add InitialCreate --startup-project ../CARS.API
   dotnet ef database update --startup-project ../CARS.API
   ```

5. **Run the Application**
   ```bash
   cd ../CARS.API
   dotnet run
   ```

6. **Access the Application**
   - API: `https://localhost:7001` or `http://localhost:5000`
   - Swagger UI: `https://localhost:7001` (opens automatically)
   - Frontend: `https://localhost:7001/index.html`

## 📱 Using the Frontend

1. Open your browser and navigate to `https://localhost:7001/index.html`

2. **Demo Credentials** (seeded automatically):
   - **Admin**: admin@cars.com / Admin@123
   - **Doctor**: dr.sarah@cars.com / Doctor@123
   - **Patient**: john.doe@example.com / Patient@123

3. **Register a New Patient**:
   - Click "Register" tab
   - Fill in the form (password must have uppercase, lowercase, digit, and special character)
   - Submit to create account and auto-login

4. **Book an Appointment**:
   - Login as a patient
   - Click "Book New Appointment"
   - Select doctor, date/time, and add notes
   - Submit to book

## 🔐 API Endpoints

### Authentication
- `POST /api/v1/auth/register` - Register new patient
- `POST /api/v1/auth/login` - Login and get JWT token

### Doctors
- `GET /api/v1/doctors` - Get all available doctors
- `GET /api/v1/doctors/{id}` - Get doctor by ID
- `POST /api/v1/doctors` - Create doctor (Admin only)
- `PUT /api/v1/doctors/{id}` - Update doctor profile

### Appointments
- `GET /api/v1/appointments` - Get all appointments (Admin only)
- `GET /api/v1/appointments/my` - Get current user's appointments
- `GET /api/v1/appointments/{id}` - Get appointment by ID
- `POST /api/v1/appointments` - Book new appointment (Patient only)
- `PATCH /api/v1/appointments/{id}/status` - Update status (Doctor only)
- `PATCH /api/v1/appointments/{id}/cancel` - Cancel appointment

## 🧪 Testing with Swagger

1. Navigate to `https://localhost:7001`
2. Click on "Authorize" button (🔓 icon)
3. Login using `/api/v1/auth/login` endpoint
4. Copy the `token` from response
5. Enter: `Bearer {your-token-here}`
6. Click "Authorize"
7. Now you can test protected endpoints

## 📊 Database Schema

### Users Table
- Id (PK, GUID)
- FullName, Email (unique), PasswordHash
- Role (Admin/Doctor/Patient)
- PhoneNumber
- IsDeleted, DeletedAt, CreatedAt

### DoctorProfiles Table
- Id (PK, GUID)
- UserId (FK to Users)
- Specialization, Bio
- IsAvailable

### Appointments Table
- Id (PK, GUID)
- PatientId (FK to Users)
- DoctorId (FK to DoctorProfiles)
- AppointmentDate, Status
- Notes, IsDeleted, CreatedAt

### AvailabilitySlots Table
- Id (PK, GUID)
- DoctorId (FK to DoctorProfiles)
- DayOfWeek, StartTime, EndTime

### MedicalRecords Table
- Id (PK, GUID)
- PatientId (FK to Users)
- AppointmentId (FK to Appointments, nullable)
- FileName, FilePath, FileType, FileSizeKB
- IsDeleted, UploadedAt

## 🔧 Configuration

### JWT Settings (appsettings.json)
```json
"JwtSettings": {
  "SecretKey": "YourSuperSecretKeyForJWTTokenGeneration12345",
  "Issuer": "CarsAPI",
  "Audience": "CarsClient",
  "ExpiryInHours": 24
}
```

**⚠️ Important**: Change the `SecretKey` in production!

## 🐛 Troubleshooting

### Database Connection Issues
- Ensure SQL Server is running
- Check connection string in appsettings.json
- Verify SQL Server instance name

### Migration Errors
```bash
# Remove all migrations and start fresh
dotnet ef migrations remove --startup-project ../CARS.API
dotnet ef migrations add InitialCreate --startup-project ../CARS.API
dotnet ef database update --startup-project ../CARS.API
```

### CORS Issues
If frontend can't connect to API:
- Check that CORS is enabled in Program.cs
- Verify API is running on correct port
- Update `API_BASE_URL` in `wwwroot/js/app.js`

### Port Already in Use
Change the port in `CARS.API/Properties/launchSettings.json`

## 📝 Future Enhancements

- [ ] Medical records file upload/download
- [ ] Email notifications for appointments
- [ ] Doctor availability scheduling
- [ ] Patient medical history timeline
- [ ] Admin dashboard with analytics
- [ ] Real-time notifications (SignalR)
- [ ] Multi-language support
- [ ] Mobile app (Xamarin/MAUI)

## 👥 User Roles & Permissions

| Feature | Admin | Doctor | Patient |
|---------|-------|--------|---------|
| View all users | ✅ | ❌ | ❌ |
| Create doctors | ✅ | ❌ | ❌ |
| View all appointments | ✅ | ❌ | ❌ |
| Book appointment | ❌ | ❌ | ✅ |
| Update appointment status | ❌ | ✅ | ❌ |
| Cancel own appointment | ❌ | ❌ | ✅ |
| View own appointments | ✅ | ✅ | ✅ |

## 📄 License

This project is created for educational purposes.

## 🤝 Contributing

This is a college/portfolio project. Feel free to fork and modify for your own use!

## 📧 Support

For issues or questions, please check the code comments or create an issue in the repository.

---

**Made with ❤️ for healthcare digitization**
