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
    public class ProvidersController : ControllerBase
    {
        private readonly HealthcareDbContext _context;
        private readonly IAuthService _authService;

        public ProvidersController(HealthcareDbContext context, IAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(ProviderRegistrationDto dto)
        {
            // Check if email already exists
            if (await _context.Providers.AnyAsync(p => p.Email == dto.Email))
            {
                return BadRequest("Email already exists");
            }

            var provider = new Provider
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = _authService.HashPassword(dto.Password),
                Specialty = dto.Specialty,
                PhoneNumber = dto.PhoneNumber,
                ClinicAddress = dto.ClinicAddress
            };

            _context.Providers.Add(provider);
            await _context.SaveChangesAsync();

            var token = _authService.GenerateJwtToken(provider.Id, provider.Email, "Provider", provider.FullName);

            return Ok(new AuthResponseDto
            {
                Token = token,
                UserType = "Provider",
                UserId = provider.Id,
                FullName = provider.FullName,
                Email = provider.Email
            });
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.Email == dto.Email);

            if (provider == null || !_authService.VerifyPassword(dto.Password, provider.PasswordHash))
            {
                return Unauthorized("Invalid credentials");
            }

            var token = _authService.GenerateJwtToken(provider.Id, provider.Email, "Provider", provider.FullName);

            return Ok(new AuthResponseDto
            {
                Token = token,
                UserType = "Provider",
                UserId = provider.Id,
                FullName = provider.FullName,
                Email = provider.Email
            });
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<List<ProviderDto>>> GetProviders()
        {
            var providers = await _context.Providers
                .Select(p => new ProviderDto
                {
                    Id = p.Id,
                    FullName = p.FullName,
                    Email = p.Email,
                    Specialty = p.Specialty,
                    PhoneNumber = p.PhoneNumber,
                    ClinicAddress = p.ClinicAddress
                })
                .ToListAsync();

            return Ok(providers);
        }
    }
}
