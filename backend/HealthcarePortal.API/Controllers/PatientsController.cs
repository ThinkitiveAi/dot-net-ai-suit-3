using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthcarePortal.API.Data;
using HealthcarePortal.API.DTOs;
using HealthcarePortal.API.Models;
using HealthcarePortal.API.Services;

namespace HealthcarePortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly HealthcareDbContext _context;
        private readonly IAuthService _authService;
        
        public PatientsController(HealthcareDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }
        
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(PatientRegistrationDto dto)
        {
            // Check if email already exists
            if (await _context.Patients.AnyAsync(p => p.Email == dto.Email))
            {
                return BadRequest("Email already exists");
            }
            
            var patient = new Patient
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = _authService.HashPassword(dto.Password),
                Age = dto.Age,
                Gender = dto.Gender,
                PhoneNumber = dto.PhoneNumber
            };
            
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
            
            var token = _authService.GenerateJwtToken(patient.Id, patient.Email, "Patient", patient.FullName);
            
            return Ok(new AuthResponseDto
            {
                Token = token,
                UserType = "Patient",
                UserId = patient.Id,
                FullName = patient.FullName,
                Email = patient.Email
            });
        }
        
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Email == dto.Email);
            
            if (patient == null || !_authService.VerifyPassword(dto.Password, patient.PasswordHash))
            {
                return Unauthorized("Invalid credentials");
            }
            
            var token = _authService.GenerateJwtToken(patient.Id, patient.Email, "Patient", patient.FullName);
            
            return Ok(new AuthResponseDto
            {
                Token = token,
                UserType = "Patient",
                UserId = patient.Id,
                FullName = patient.FullName,
                Email = patient.Email
            });
        }
        
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<PatientDto>>> GetPatients()
        {
            var patients = await _context.Patients
                .Select(p => new PatientDto
                {
                    Id = p.Id,
                    FullName = p.FullName,
                    Email = p.Email,
                    Age = p.Age,
                    Gender = p.Gender,
                    PhoneNumber = p.PhoneNumber
                })
                .ToListAsync();
            
            return Ok(patients);
        }
    }
}
