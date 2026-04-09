using Microsoft.EntityFrameworkCore;

namespace CARS;

public class DoctorService : IDoctorService
{
    private readonly CarsDbContext _context;
    private readonly ICacheService _cache;
    private readonly ILogger<DoctorService> _logger;

    public DoctorService(CarsDbContext context, ICacheService cache, ILogger<DoctorService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ApiResponse<List<DoctorDto>>> GetAllDoctorsAsync()
    {
        try
        {
            const string cacheKey = "doctors:all";
            var cached = await _cache.GetAsync<List<DoctorDto>>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Doctors list served from cache");
                return new ApiResponse<List<DoctorDto>> { Success = true, Message = "Doctors retrieved from cache", Data = cached };
            }

            var doctors = await _context.DoctorProfiles
                .Include(d => d.User)
                .Where(d => d.IsAvailable)
                .Select(d => new DoctorDto
                {
                    Id = d.Id,
                    FullName = d.User.FullName,
                    Email = d.User.Email,
                    Specialization = d.Specialization,
                    Bio = d.Bio,
                    IsAvailable = d.IsAvailable
                })
                .ToListAsync();

            await _cache.SetAsync(cacheKey, doctors, TimeSpan.FromMinutes(5));
            return new ApiResponse<List<DoctorDto>> { Success = true, Message = "Doctors retrieved successfully", Data = doctors };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctors");
            return new ApiResponse<List<DoctorDto>> { Success = false, Message = "Failed to retrieve doctors", Errors = [ex.Message] };
        }
    }

    public async Task<ApiResponse<DoctorDto>> GetDoctorByIdAsync(Guid id)
    {
        try
        {
            var doctor = await _context.DoctorProfiles
                .Include(d => d.User)
                .Where(d => d.Id == id)
                .Select(d => new DoctorDto
                {
                    Id = d.Id,
                    FullName = d.User.FullName,
                    Email = d.User.Email,
                    Specialization = d.Specialization,
                    Bio = d.Bio,
                    IsAvailable = d.IsAvailable
                })
                .FirstOrDefaultAsync();

            if (doctor == null)
                return new ApiResponse<DoctorDto> { Success = false, Message = "Doctor not found" };

            return new ApiResponse<DoctorDto> { Success = true, Message = "Doctor retrieved successfully", Data = doctor };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctor {Id}", id);
            return new ApiResponse<DoctorDto> { Success = false, Message = "Failed to retrieve doctor", Errors = [ex.Message] };
        }
    }

    public async Task<ApiResponse<DoctorDto>> CreateDoctorAsync(CreateDoctorDto dto)
    {
        try
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return new ApiResponse<DoctorDto> { Success = false, Message = "User with this email already exists" };

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = UserRole.Doctor,
                PhoneNumber = dto.PhoneNumber,
                CreatedAt = DateTime.UtcNow
            };

            var doctorProfile = new DoctorProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Specialization = dto.Specialization,
                Bio = dto.Bio,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            _context.DoctorProfiles.Add(doctorProfile);
            await _context.SaveChangesAsync();
            await _cache.RemoveAsync("doctors:all");

            _logger.LogInformation("Doctor created: {Email}", dto.Email);
            return new ApiResponse<DoctorDto>
            {
                Success = true,
                Message = "Doctor created successfully",
                Data = new DoctorDto
                {
                    Id = doctorProfile.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    Specialization = doctorProfile.Specialization,
                    Bio = doctorProfile.Bio,
                    IsAvailable = doctorProfile.IsAvailable
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating doctor");
            return new ApiResponse<DoctorDto> { Success = false, Message = "Failed to create doctor", Errors = [ex.Message] };
        }
    }

    public async Task<ApiResponse<DoctorDto>> UpdateDoctorAsync(Guid id, DoctorDto dto)
    {
        try
        {
            var doctor = await _context.DoctorProfiles.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
            if (doctor == null)
                return new ApiResponse<DoctorDto> { Success = false, Message = "Doctor not found" };

            doctor.Specialization = dto.Specialization;
            doctor.Bio = dto.Bio;
            doctor.IsAvailable = dto.IsAvailable;

            await _context.SaveChangesAsync();
            await _cache.RemoveAsync("doctors:all");

            _logger.LogInformation("Doctor updated: {Id}", id);
            return await GetDoctorByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating doctor {Id}", id);
            return new ApiResponse<DoctorDto> { Success = false, Message = "Failed to update doctor", Errors = [ex.Message] };
        }
    }

    public async Task<ApiResponse<bool>> DeleteDoctorAsync(Guid id)
    {
        try
        {
            var doctor = await _context.DoctorProfiles.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
            if (doctor == null)
                return new ApiResponse<bool> { Success = false, Message = "Doctor not found" };

            // Soft delete
            doctor.User.IsDeleted = true;
            doctor.User.DeletedAt = DateTime.UtcNow;
            doctor.IsAvailable = false;

            await _context.SaveChangesAsync();
            await _cache.RemoveAsync("doctors:all");

            _logger.LogInformation("Doctor soft-deleted: {Id}", id);
            return new ApiResponse<bool> { Success = true, Message = "Doctor deleted successfully", Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting doctor {Id}", id);
            return new ApiResponse<bool> { Success = false, Message = "Failed to delete doctor", Errors = [ex.Message] };
        }
    }
}
