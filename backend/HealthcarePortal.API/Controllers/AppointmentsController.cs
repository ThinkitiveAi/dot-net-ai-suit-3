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
            var targetDate = date ?? DateTime.Today;
            var slots = await _appointmentService.GetAvailableSlotsAsync(providerId, targetDate);
            return Ok(slots);
        }
        
        [HttpPost("book")]
        public async Task<ActionResult<AppointmentResponseDto>> BookAppointment(BookAppointmentDto dto)
        {
            // Verify the slot is still available
            if (!await _appointmentService.IsSlotAvailableAsync(dto.ProviderId, dto.AppointmentDateTime))
            {
                return BadRequest("The selected time slot is no longer available");
            }
            
            // Verify patient and provider exist
            var patient = await _context.Patients.FindAsync(dto.PatientId);
            var provider = await _context.Providers.FindAsync(dto.ProviderId);
            
            if (patient == null || provider == null)
            {
                return BadRequest("Invalid patient or provider ID");
            }
            
            var appointment = new Appointment
            {
                PatientId = dto.PatientId,
                ProviderId = dto.ProviderId,
                AppointmentDateTime = dto.AppointmentDateTime,
                Notes = dto.Notes,
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
                ProviderId = appointment.ProviderId,
                ProviderName = appointment.Provider.FullName,
                AppointmentDateTime = appointment.AppointmentDateTime,
                Notes = appointment.Notes,
                Status = appointment.Status.ToString(),
                CreatedAt = appointment.CreatedAt
            };
            
            return Ok(response);
        }
        
        [HttpGet("provider")]
        public async Task<ActionResult<List<AppointmentResponseDto>>> GetProviderAppointments()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userType = User.FindFirst("UserType")?.Value;
            
            if (userIdClaim == null || userType != "Provider")
            {
                return Unauthorized("Only providers can access this endpoint");
            }
            
            var providerId = int.Parse(userIdClaim);
            
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Provider)
                .Where(a => a.ProviderId == providerId)
                .OrderBy(a => a.AppointmentDateTime)
                .Select(a => new AppointmentResponseDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient.FullName,
                    ProviderId = a.ProviderId,
                    ProviderName = a.Provider.FullName,
                    AppointmentDateTime = a.AppointmentDateTime,
                    Notes = a.Notes,
                    Status = a.Status.ToString(),
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();
            
            return Ok(appointments);
        }
        
        [HttpGet("patient")]
        public async Task<ActionResult<List<AppointmentResponseDto>>> GetPatientAppointments()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userType = User.FindFirst("UserType")?.Value;
            
            if (userIdClaim == null || userType != "Patient")
            {
                return Unauthorized("Only patients can access this endpoint");
            }
            
            var patientId = int.Parse(userIdClaim);
            
            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Provider)
                .Where(a => a.PatientId == patientId)
                .OrderBy(a => a.AppointmentDateTime)
                .Select(a => new AppointmentResponseDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    PatientName = a.Patient.FullName,
                    ProviderId = a.ProviderId,
                    ProviderName = a.Provider.FullName,
                    AppointmentDateTime = a.AppointmentDateTime,
                    Notes = a.Notes,
                    Status = a.Status.ToString(),
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();
            
            return Ok(appointments);
        }
    }
}
