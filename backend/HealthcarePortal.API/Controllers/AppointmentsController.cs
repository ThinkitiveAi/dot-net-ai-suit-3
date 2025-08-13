using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using HealthcarePortal.API.Data;
using HealthcarePortal.API.DTOs;
using HealthcarePortal.API.Models;
using HealthcarePortal.API.Services;

namespace HealthcarePortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly HealthcareDbContext _context;
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(HealthcareDbContext context, IAppointmentService appointmentService)
        {
            _context = context;
            _appointmentService = appointmentService;
        }

        [HttpGet("slots")]
        public async Task<ActionResult<List<AvailableSlotDto>>> GetAvailableSlots(int providerId, DateTime? date = null)
        {
            try
            {
                var targetDate = date ?? DateTime.Today;

                // Validate provider exists
                var provider = await _context.Providers.FindAsync(providerId);
                if (provider == null)
                {
                    return NotFound("Provider not found");
                }

                var slots = await _appointmentService.GetAvailableSlotsAsync(providerId, targetDate);
                return Ok(slots);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving available slots: {ex.Message}");
            }
        }

        [HttpPost("book")]
        public async Task<ActionResult<AppointmentResponseDto>> BookAppointment(BookAppointmentDto dto)
        {
            try
            {
                // Get current user info
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userType = User.FindFirst("UserType")?.Value;

                if (userIdClaim == null)
                {
                    return Unauthorized("User not authenticated");
                }

                // Validate appointment date is in the future
                if (dto.AppointmentDateTime <= DateTime.Now)
                {
                    return BadRequest("Appointment must be scheduled for a future date and time");
                }

                // Validate appointment is within business hours
                var appointmentTime = dto.AppointmentDateTime.TimeOfDay;
                if (appointmentTime < new TimeSpan(9, 0, 0) || appointmentTime >= new TimeSpan(17, 0, 0))
                {
                    return BadRequest("Appointments can only be scheduled between 9:00 AM and 5:00 PM");
                }

                // Validate appointment is on a weekday
                if (dto.AppointmentDateTime.DayOfWeek == DayOfWeek.Saturday ||
                    dto.AppointmentDateTime.DayOfWeek == DayOfWeek.Sunday)
                {
                    return BadRequest("Appointments can only be scheduled on weekdays");
                }

                // Verify the slot is still available
                if (!await _appointmentService.IsSlotAvailableAsync(dto.ProviderId, dto.AppointmentDateTime))
                {
                    return BadRequest("The selected time slot is no longer available");
                }

                // Verify patient and provider exist
                var patient = await _context.Patients.FindAsync(dto.PatientId);
                var provider = await _context.Providers.FindAsync(dto.ProviderId);

                if (patient == null)
                {
                    return BadRequest("Patient not found");
                }

                if (provider == null)
                {
                    return BadRequest("Provider not found");
                }

                // Ensure the authenticated user is the patient booking the appointment
                if (userType == "Patient" && int.Parse(userIdClaim) != dto.PatientId)
                {
                    return Forbid("You can only book appointments for yourself");
                }

                // Check for duplicate appointments (same patient, provider, and date)
                var existingAppointment = await _context.Appointments
                    .AnyAsync(a => a.PatientId == dto.PatientId &&
                                  a.ProviderId == dto.ProviderId &&
                                  a.AppointmentDateTime.Date == dto.AppointmentDateTime.Date &&
                                  a.Status == AppointmentStatus.Scheduled);

                if (existingAppointment)
                {
                    return BadRequest("You already have an appointment with this provider on the selected date");
                }

                var appointment = new Appointment
                {
                    PatientId = dto.PatientId,
                    ProviderId = dto.ProviderId,
                    AppointmentDateTime = dto.AppointmentDateTime,
                    Notes = dto.Notes?.Trim(),
                    Status = AppointmentStatus.Scheduled
                };

                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();

                // Load the appointment with related data
                await _context.Entry(appointment)
                    .Reference(a => a.Patient)
                    .LoadAsync();
                await _context.Entry(appointment)
                    .Reference(a => a.Provider)
                    .LoadAsync();

                var response = new AppointmentResponseDto
                {
                    Id = appointment.Id,
                    PatientId = appointment.PatientId,
                    PatientName = appointment.Patient.FullName,
                    PatientEmail = appointment.Patient.Email,
                    PatientPhone = appointment.Patient.PhoneNumber,
                    ProviderId = appointment.ProviderId,
                    ProviderName = appointment.Provider.FullName,
                    ProviderSpecialty = appointment.Provider.Specialty,
                    ProviderPhone = appointment.Provider.PhoneNumber,
                    ClinicAddress = appointment.Provider.ClinicAddress,
                    AppointmentDateTime = appointment.AppointmentDateTime,
                    Notes = appointment.Notes,
                    Status = appointment.Status.ToString(),
                    CreatedAt = appointment.CreatedAt
                };

                return CreatedAtAction(nameof(GetAppointmentById), new { id = appointment.Id }, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error booking appointment: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AppointmentResponseDto>> GetAppointmentById(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userType = User.FindFirst("UserType")?.Value;

                if (userIdClaim == null)
                {
                    return Unauthorized("User not authenticated");
                }

                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Provider)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (appointment == null)
                {
                    return NotFound("Appointment not found");
                }

                // Ensure user can only access their own appointments
                var userId = int.Parse(userIdClaim);
                if ((userType == "Patient" && appointment.PatientId != userId) ||
                    (userType == "Provider" && appointment.ProviderId != userId))
                {
                    return Forbid("You can only access your own appointments");
                }

                var response = new AppointmentResponseDto
                {
                    Id = appointment.Id,
                    PatientId = appointment.PatientId,
                    PatientName = appointment.Patient.FullName,
                    PatientEmail = appointment.Patient.Email,
                    PatientPhone = appointment.Patient.PhoneNumber,
                    ProviderId = appointment.ProviderId,
                    ProviderName = appointment.Provider.FullName,
                    ProviderSpecialty = appointment.Provider.Specialty,
                    ProviderPhone = appointment.Provider.PhoneNumber,
                    ClinicAddress = appointment.Provider.ClinicAddress,
                    AppointmentDateTime = appointment.AppointmentDateTime,
                    Notes = appointment.Notes,
                    Status = appointment.Status.ToString(),
                    CreatedAt = appointment.CreatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving appointment: {ex.Message}");
            }
        }

        [HttpGet("provider")]
        public async Task<ActionResult<List<AppointmentResponseDto>>> GetProviderAppointments(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? status = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userType = User.FindFirst("UserType")?.Value;

                if (userIdClaim == null || userType != "Provider")
                {
                    return Unauthorized("Only providers can access this endpoint");
                }

                var providerId = int.Parse(userIdClaim);

                var query = _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Provider)
                    .Where(a => a.ProviderId == providerId);

                // Apply date filters
                if (startDate.HasValue)
                {
                    query = query.Where(a => a.AppointmentDateTime.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.AppointmentDateTime.Date <= endDate.Value.Date);
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<AppointmentStatus>(status, true, out var statusEnum))
                {
                    query = query.Where(a => a.Status == statusEnum);
                }

                var appointments = await query
                    .OrderBy(a => a.AppointmentDateTime)
                    .Select(a => new AppointmentResponseDto
                    {
                        Id = a.Id,
                        PatientId = a.PatientId,
                        PatientName = a.Patient.FullName,
                        PatientEmail = a.Patient.Email,
                        PatientPhone = a.Patient.PhoneNumber,
                        ProviderId = a.ProviderId,
                        ProviderName = a.Provider.FullName,
                        ProviderSpecialty = a.Provider.Specialty,
                        ProviderPhone = a.Provider.PhoneNumber,
                        ClinicAddress = a.Provider.ClinicAddress,
                        AppointmentDateTime = a.AppointmentDateTime,
                        Notes = a.Notes,
                        Status = a.Status.ToString(),
                        CreatedAt = a.CreatedAt
                    })
                    .ToListAsync();

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving provider appointments: {ex.Message}");
            }
        }

        [HttpGet("patient")]
        public async Task<ActionResult<List<AppointmentResponseDto>>> GetPatientAppointments(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? status = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userType = User.FindFirst("UserType")?.Value;

                if (userIdClaim == null || userType != "Patient")
                {
                    return Unauthorized("Only patients can access this endpoint");
                }

                var patientId = int.Parse(userIdClaim);

                var query = _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Provider)
                    .Where(a => a.PatientId == patientId);

                // Apply date filters
                if (startDate.HasValue)
                {
                    query = query.Where(a => a.AppointmentDateTime.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.AppointmentDateTime.Date <= endDate.Value.Date);
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<AppointmentStatus>(status, true, out var statusEnum))
                {
                    query = query.Where(a => a.Status == statusEnum);
                }

                var appointments = await query
                    .OrderBy(a => a.AppointmentDateTime)
                    .Select(a => new AppointmentResponseDto
                    {
                        Id = a.Id,
                        PatientId = a.PatientId,
                        PatientName = a.Patient.FullName,
                        PatientEmail = a.Patient.Email,
                        PatientPhone = a.Patient.PhoneNumber,
                        ProviderId = a.ProviderId,
                        ProviderName = a.Provider.FullName,
                        ProviderSpecialty = a.Provider.Specialty,
                        ProviderPhone = a.Provider.PhoneNumber,
                        ClinicAddress = a.Provider.ClinicAddress,
                        AppointmentDateTime = a.AppointmentDateTime,
                        Notes = a.Notes,
                        Status = a.Status.ToString(),
                        CreatedAt = a.CreatedAt
                    })
                    .ToListAsync();

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving patient appointments: {ex.Message}");
            }
        }

        [HttpPut("{id}/status")]
        public async Task<ActionResult<AppointmentResponseDto>> UpdateAppointmentStatus(int id, [FromBody] UpdateAppointmentStatusDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userType = User.FindFirst("UserType")?.Value;

                if (userIdClaim == null)
                {
                    return Unauthorized("User not authenticated");
                }

                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Provider)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (appointment == null)
                {
                    return NotFound("Appointment not found");
                }

                var userId = int.Parse(userIdClaim);

                // Only providers can update appointment status
                if (userType != "Provider" || appointment.ProviderId != userId)
                {
                    return Forbid("Only the assigned provider can update appointment status");
                }

                // Validate status transition
                if (!IsValidStatusTransition(appointment.Status, dto.Status))
                {
                    return BadRequest($"Invalid status transition from {appointment.Status} to {dto.Status}");
                }

                appointment.Status = dto.Status;
                await _context.SaveChangesAsync();

                var response = new AppointmentResponseDto
                {
                    Id = appointment.Id,
                    PatientId = appointment.PatientId,
                    PatientName = appointment.Patient.FullName,
                    PatientEmail = appointment.Patient.Email,
                    PatientPhone = appointment.Patient.PhoneNumber,
                    ProviderId = appointment.ProviderId,
                    ProviderName = appointment.Provider.FullName,
                    ProviderSpecialty = appointment.Provider.Specialty,
                    ProviderPhone = appointment.Provider.PhoneNumber,
                    ClinicAddress = appointment.Provider.ClinicAddress,
                    AppointmentDateTime = appointment.AppointmentDateTime,
                    Notes = appointment.Notes,
                    Status = appointment.Status.ToString(),
                    CreatedAt = appointment.CreatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating appointment status: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> CancelAppointment(int id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userType = User.FindFirst("UserType")?.Value;

                if (userIdClaim == null)
                {
                    return Unauthorized("User not authenticated");
                }

                var appointment = await _context.Appointments.FindAsync(id);

                if (appointment == null)
                {
                    return NotFound("Appointment not found");
                }

                var userId = int.Parse(userIdClaim);

                // Patients can cancel their own appointments, providers can cancel appointments assigned to them
                if ((userType == "Patient" && appointment.PatientId != userId) ||
                    (userType == "Provider" && appointment.ProviderId != userId))
                {
                    return Forbid("You can only cancel your own appointments");
                }

                // Check if appointment can be cancelled (not in the past and not already completed)
                if (appointment.AppointmentDateTime <= DateTime.Now)
                {
                    return BadRequest("Cannot cancel past appointments");
                }

                if (appointment.Status == AppointmentStatus.Completed)
                {
                    return BadRequest("Cannot cancel completed appointments");
                }

                appointment.Status = AppointmentStatus.Cancelled;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Appointment cancelled successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error cancelling appointment: {ex.Message}");
            }
        }

        [HttpGet("upcoming")]
        public async Task<ActionResult<List<AppointmentResponseDto>>> GetUpcomingAppointments()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userType = User.FindFirst("UserType")?.Value;

                if (userIdClaim == null)
                {
                    return Unauthorized("User not authenticated");
                }

                var userId = int.Parse(userIdClaim);
                var currentDate = DateTime.Now;

                var query = _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Provider)
                    .Where(a => a.AppointmentDateTime > currentDate &&
                               a.Status == AppointmentStatus.Scheduled);

                if (userType == "Patient")
                {
                    query = query.Where(a => a.PatientId == userId);
                }
                else if (userType == "Provider")
                {
                    query = query.Where(a => a.ProviderId == userId);
                }
                else
                {
                    return Forbid("Invalid user type");
                }

                var appointments = await query
                    .OrderBy(a => a.AppointmentDateTime)
                    .Take(10) // Limit to next 10 appointments
                    .Select(a => new AppointmentResponseDto
                    {
                        Id = a.Id,
                        PatientId = a.PatientId,
                        PatientName = a.Patient.FullName,
                        PatientEmail = a.Patient.Email,
                        PatientPhone = a.Patient.PhoneNumber,
                        ProviderId = a.ProviderId,
                        ProviderName = a.Provider.FullName,
                        ProviderSpecialty = a.Provider.Specialty,
                        ProviderPhone = a.Provider.PhoneNumber,
                        ClinicAddress = a.Provider.ClinicAddress,
                        AppointmentDateTime = a.AppointmentDateTime,
                        Notes = a.Notes,
                        Status = a.Status.ToString(),
                        CreatedAt = a.CreatedAt
                    })
                    .ToListAsync();

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving upcoming appointments: {ex.Message}");
            }
        }

        private static bool IsValidStatusTransition(AppointmentStatus currentStatus, AppointmentStatus newStatus)
        {
            return currentStatus switch
            {
                AppointmentStatus.Scheduled => newStatus is AppointmentStatus.Completed or AppointmentStatus.Cancelled or AppointmentStatus.NoShow,
                AppointmentStatus.Completed => false, // Completed appointments cannot be changed
                AppointmentStatus.Cancelled => false, // Cancelled appointments cannot be changed
                AppointmentStatus.NoShow => newStatus == AppointmentStatus.Completed, // No-show can be marked as completed if patient shows up late
                _ => false
            };
        }
    }
}
