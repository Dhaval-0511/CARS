using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CARS;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class MedicalRecordsController : ControllerBase
{
    private readonly IMedicalRecordService _recordService;

    public MedicalRecordsController(IMedicalRecordService recordService)
    {
        _recordService = recordService;
    }

    /// <summary>Get current user's medical records (Patient sees own; Doctor sees their own uploads)</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Patient,Admin,Doctor")]
    public async Task<IActionResult> GetMyRecords()
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();
        var result = await _recordService.GetPatientRecordsAsync(userId);
        return Ok(result);
    }

    /// <summary>Get medical records for a specific patient (Admin only)</summary>
    [HttpGet("patient/{patientId:guid}")]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> GetPatientRecords(Guid patientId)
    {
        var result = await _recordService.GetPatientRecordsAsync(patientId);
        return Ok(result);
    }

    /// <summary>Upload a medical document (max 10MB)</summary>
    [HttpPost("upload")]
    [Authorize(Roles = "Patient,Admin,Doctor")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] Guid? appointmentId = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ApiResponse<object> { Success = false, Message = "No file provided" });

        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var result = await _recordService.UploadRecordAsync(userId, file, appointmentId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Download a medical record file</summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id)
    {
        var result = await _recordService.GetRecordFilePathAsync(id);
        if (!result.Success) return NotFound(result);

        var filePath = result.Data!;
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };

        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(bytes, contentType, result.Message); // Message = original file name
    }

    /// <summary>Soft-delete a medical record</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _recordService.DeleteRecordAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    private Guid GetUserId()
    {
        var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(val, out var id) ? id : Guid.Empty;
    }
}
