using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CARS;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;

    public DoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    /// <summary>Get all available doctors</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllDoctors()
    {
        var result = await _doctorService.GetAllDoctorsAsync();
        return Ok(result);
    }

    /// <summary>Get doctor by ID</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDoctorById(Guid id)
    {
        var result = await _doctorService.GetDoctorByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Create a new doctor (Admin only)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorDto dto)
    {
        var result = await _doctorService.CreateDoctorAsync(dto);
        if (!result.Success) return BadRequest(result);
        return CreatedAtAction(nameof(GetDoctorById), new { id = result.Data?.Id }, result);
    }

    /// <summary>Update doctor profile (Admin or Doctor)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> UpdateDoctor(Guid id, [FromBody] DoctorDto dto)
    {
        var result = await _doctorService.UpdateDoctorAsync(id, dto);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>Soft-delete a doctor (Admin only)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDoctor(Guid id)
    {
        var result = await _doctorService.DeleteDoctorAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
