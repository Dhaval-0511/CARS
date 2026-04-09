using Microsoft.EntityFrameworkCore;

namespace CARS;

public class MedicalRecordService : IMedicalRecordService
{
    private readonly CarsDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MedicalRecordService> _logger;
    private static readonly string[] AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg", ".docx", ".txt"];

    public MedicalRecordService(CarsDbContext context, IWebHostEnvironment env, ILogger<MedicalRecordService> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    public async Task<ApiResponse<List<MedicalRecordDto>>> GetPatientRecordsAsync(Guid patientId)
    {
        try
        {
            var records = await _context.MedicalRecords
                .Where(r => r.PatientId == patientId)
                .Select(r => new MedicalRecordDto
                {
                    Id = r.Id,
                    PatientId = r.PatientId,
                    FileName = r.FileName,
                    FileType = r.FileType,
                    FileSizeKB = r.FileSizeKB,
                    UploadedAt = r.UploadedAt,
                    DownloadUrl = $"/api/v1/medicalrecords/{r.Id}/download"
                })
                .ToListAsync();

            return new ApiResponse<List<MedicalRecordDto>> { Success = true, Message = "Records retrieved successfully", Data = records };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving records for patient {PatientId}", patientId);
            return new ApiResponse<List<MedicalRecordDto>> { Success = false, Message = "Failed to retrieve records", Errors = [ex.Message] };
        }
    }

    public async Task<ApiResponse<MedicalRecordDto>> UploadRecordAsync(Guid patientId, IFormFile file, Guid? appointmentId)
    {
        try
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return new ApiResponse<MedicalRecordDto> { Success = false, Message = $"File type '{ext}' not allowed" };

            var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath);

            var uniqueName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsPath, uniqueName);

            await using (var stream = File.Create(filePath))
                await file.CopyToAsync(stream);

            var record = new MedicalRecord
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                AppointmentId = appointmentId,
                FileName = file.FileName,
                FilePath = uniqueName,
                FileType = ext,
                FileSizeKB = file.Length / 1024,
                UploadedAt = DateTime.UtcNow
            };

            _context.MedicalRecords.Add(record);
            await _context.SaveChangesAsync();

            _logger.LogInformation("File uploaded for patient {PatientId}: {FileName}", patientId, file.FileName);
            return new ApiResponse<MedicalRecordDto>
            {
                Success = true,
                Message = "File uploaded successfully",
                Data = new MedicalRecordDto
                {
                    Id = record.Id,
                    PatientId = record.PatientId,
                    FileName = record.FileName,
                    FileType = record.FileType,
                    FileSizeKB = record.FileSizeKB,
                    UploadedAt = record.UploadedAt,
                    DownloadUrl = $"/api/v1/medicalrecords/{record.Id}/download"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for patient {PatientId}", patientId);
            return new ApiResponse<MedicalRecordDto> { Success = false, Message = "Upload failed", Errors = [ex.Message] };
        }
    }

    public async Task<ApiResponse<string>> GetRecordFilePathAsync(Guid id)
    {
        try
        {
            var record = await _context.MedicalRecords.FindAsync(id);
            if (record == null)
                return new ApiResponse<string> { Success = false, Message = "Record not found" };

            var fullPath = Path.Combine(_env.WebRootPath, "uploads", record.FilePath);
            if (!File.Exists(fullPath))
                return new ApiResponse<string> { Success = false, Message = "File not found on disk" };

            return new ApiResponse<string> { Success = true, Data = fullPath, Message = record.FileName };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file path for record {Id}", id);
            return new ApiResponse<string> { Success = false, Message = "Failed to get file path", Errors = [ex.Message] };
        }
    }

    public async Task<ApiResponse<bool>> DeleteRecordAsync(Guid id)
    {
        try
        {
            var record = await _context.MedicalRecords.FindAsync(id);
            if (record == null)
                return new ApiResponse<bool> { Success = false, Message = "Record not found" };

            // Soft delete
            record.IsDeleted = true;
            record.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Medical record soft-deleted: {Id}", id);
            return new ApiResponse<bool> { Success = true, Message = "Record deleted successfully", Data = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting record {Id}", id);
            return new ApiResponse<bool> { Success = false, Message = "Failed to delete record", Errors = [ex.Message] };
        }
    }
}
