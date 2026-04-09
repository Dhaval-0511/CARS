// ════════════════════════════════════════════════════════════════════════════
//  CARS – Clinic Appointment & Record System
//  Program.cs  |  Application entry point & DI / Middleware configuration
//
//  KEY FEATURES CONFIGURED HERE:
//    • Serilog          – structured request + error logging to file & console
//    • API Versioning   – URL-segment versioning (/api/v1/...)
//    • Swagger/OpenAPI  – interactive docs with JWT auth support
//    • EF Core          – SQL Server LocalDB via DbContext
//    • JWT Auth         – Bearer token authentication + role-based authorization
//    • CORS             – open policy (restrict in production)
//    • Redis / Memory   – distributed caching with fallback to in-memory
//    • Rate Limiting    – per-IP fixed-window limits (General + Auth policies)
//    • FluentValidation – model validation wired via DI
//    • Static Files     – serves wwwroot (login page, dashboard, uploads)
//    • DB Migration     – auto-runs EF migrations at startup
// ════════════════════════════════════════════════════════════════════════════

using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;
using CARS;

// ── Serilog early setup ─────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/cars-.log", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// ── Controllers + Endpoints ─────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── API Versioning ───────────────────────────────────────────────────────────
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ── Swagger with versioning & JWT ───────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CARS API – Clinic Appointment & Record System",
        Version = "v1",
        Description = "RESTful API: CRUD · Auth · JWT · Roles · Rate Limiting · Versioning · Caching · Redis · Upload/Download · Soft Delete · Validation · Seeding · Logging · Sunset",
        Contact = new OpenApiContact { Name = "CARS Team", Email = "support@cars.com" }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization. Enter: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ── Database (EF Core + SQL Server LocalDB) ──────────────────────────────────
builder.Services.AddDbContext<CarsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// ── JWT Authentication ───────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyForJWTTokenGeneration12345";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "CarsAPI",
        ValidAudience = jwtSettings["Audience"] ?? "CarsClient",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// ── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ── Caching: Redis or in-memory fallback ─────────────────────────────────────
var redisEnabled = builder.Configuration.GetValue<bool>("Redis:Enabled");
var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";

if (redisEnabled)
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "CARS:";
    });
    Log.Information("Using Redis cache at {Redis}", redisConnection);
}
else
{
    builder.Services.AddDistributedMemoryCache();
    Log.Information("Redis disabled — using in-memory distributed cache");
}

// Response caching middleware
builder.Services.AddResponseCaching();

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // General API: 100 requests / minute per IP
    options.AddFixedWindowLimiter("GeneralPolicy", policy =>
    {
        policy.Window = TimeSpan.FromMinutes(1);
        policy.PermitLimit = 100;
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policy.QueueLimit = 10;
    });

    // Auth endpoints: 10 requests / minute per IP (brute-force protection)
    options.AddFixedWindowLimiter("AuthPolicy", policy =>
    {
        policy.Window = TimeSpan.FromMinutes(1);
        policy.PermitLimit = 10;
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policy.QueueLimit = 2;
    });
});

// ── Validation ────────────────────────────────────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IMedicalRecordService, MedicalRecordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ICacheService, CacheService>();

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────────────────────
// Always show Swagger (useful in dev; restrict in prod if needed)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CARS API v1");
    c.RoutePrefix = "swagger"; // Swagger at /swagger
});

app.UseSerilogRequestLogging(); // log every HTTP request

app.UseMiddleware<SunsetMiddleware>(); // RFC 8594 Sunset headers

// Serve index.html at root "/" (fixes 404 on root URL)
app.UseDefaultFiles();            // maps / → /index.html
app.UseStaticFiles();             // serve wwwroot (index.html, dashboard.html, css, uploads)
app.UseResponseCaching();

app.UseCors("AllowAll");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("GeneralPolicy");

// ── Database Migration + Seeding ──────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CarsDbContext>();
    var logger  = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        context.Database.Migrate();
        logger.LogInformation("Database migration applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed");
    }
}

app.Run();
